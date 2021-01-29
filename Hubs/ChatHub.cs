using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using Microsoft.AspNet.SignalR;
using WebApplication6.Models;
using WebApplication6.Models.ViewModels;

namespace WebApplication6.Hubs
{
    [Authorize]

    public class ChatHub : Hub
    {
       
     
       // kullanıcı bilgileri
        public readonly static List<UserViewModel> _Connections = new List<UserViewModel>();

        //chat odası bilgileri
        private readonly static List<RoomViewModel> _Rooms = new List<RoomViewModel>();

        //SignalR bağlantılarını uygulama kullanıcılarıyla eşleme.
        private readonly static Dictionary<string, string> _ConnectionsMap = new Dictionary<string, string>();
       


        public void Send(string roomName, string message)
        {
            if (message.StartsWith("/private"))
                SendPrivate(message);
            else
                SendToRoom(roomName, message);
        }

        public void SendPrivate(string message)
        {
            // message format: /private(receiverName) Lorem ipsum...
            string[] split = message.Split(')');
            string receiver = split[0].Split('(')[1];
            string userId;
            if (_ConnectionsMap.TryGetValue(receiver, out userId))
            {
                // Who is the sender;
                var sender = _Connections.Where(u => u.Username == IdentityName).First();

                message = Regex.Replace(message, @"\/private\(.*?\)", string.Empty).Trim();

                // Build the message
                MessageViewModel messageViewModel = new MessageViewModel()
                {
                    From = sender.DisplayName,
                    To = "",
                    Content = Regex.Replace(message, @"(?i)<(?!img|a|/a|/img).*?>", String.Empty),
                    Timestamp = DateTime.Now.ToLongTimeString()
                };

                // Send the message
                Clients.Client(userId).newMessage(messageViewModel);
                Clients.Caller.newMessage(messageViewModel);
            }
        }



        public void SendToRoom(string roomName, string message)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    var user = db.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();
                    var room = db.Rooms.Where(r => r.Name == roomName).FirstOrDefault();

                    // Create and save message in database
                    Message msg = new Message()
                    {
                        Content = Regex.Replace(message, @"(?i)<(?!img|a|/a|/img).*?>", String.Empty),
                        Timestamp = DateTime.Now.Ticks.ToString(),
                        FromUser = user,
                        ToRoom = room
                    };
                    db.Messages.Add(msg);
                    db.SaveChanges();

                    // Broadcast the message
                    var messageViewModel = Mapper.Map<Message, MessageViewModel>(msg);
                    Clients.Group(roomName).newMessage(messageViewModel);
                }
            }
            catch (Exception)
            {
                Clients.Caller.onError("Message not send!");
            }
        }


        public void Join(string roomName)
        {
            try
            {
                var user = _Connections.Where(u => u.Username == IdentityName).FirstOrDefault();
                if (user != null && user.CurrentRoom != roomName)
                {
                  
                    if (!string.IsNullOrEmpty(user.CurrentRoom))
                        Clients.OthersInGroup(user.CurrentRoom).removeUser(user);

                                      Leave(user.CurrentRoom);
                    Groups.Add(Context.ConnectionId, roomName);
                    user.CurrentRoom = roomName;

                    Clients.OthersInGroup(roomName).addUser(user);
                }
            }
            catch (Exception ex)
            {
                Clients.Caller.onError("You failed to join the chat room!" + ex.Message);
            }
        }

        private void Leave(string roomName)
        {
            Groups.Remove(Context.ConnectionId, roomName);
        }


        public void CreateRoom(string roomName)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    // Accept: Letters, numbers and one space between words.
                    Match match = Regex.Match(roomName, @"^\w+( \w+)*$");
                    if (!match.Success)
                    {
                        Clients.Caller.onError("Geçersiz oda adı!Oda adı yalnızca harf ve rakamlardan oluşmalıdır.");
                    }
                    else if (roomName.Length < 5 || roomName.Length > 20)
                    {
                        Clients.Caller.onError("Oda adı 5-20 karakter arasında olmalıdır!");
                    }
                    else if (db.Rooms.Any(r => r.Name == roomName))
                    {
                        Clients.Caller.onError("Bu isimde başka bir sohbet odası var!");
                    }
                    else
                    {
                        // Create and save chat room in database
                        var user = db.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();
                        var room = new Room()
                        {
                            Name = roomName,
                            UserAccount = user
                        };
                        db.Rooms.Add(room);
                        db.SaveChanges();

                        if (room != null)
                        {
                            // Update room list
                            var roomViewModel = Mapper.Map<Room, RoomViewModel>(room);
                            _Rooms.Add(roomViewModel);
                            Clients.All.addChatRoom(roomViewModel);
                        }
                    }
                }//using
            }
            catch (Exception ex)
            {
                Clients.Caller.onError("Chat odası oluşturulamadı: " + ex.Message);
            }
        }


        public void DeleteRoom(string roomName)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    // Odayı veritabanından sil
                    var room = db.Rooms.Where(r => r.Name == roomName && r.UserAccount.UserName == IdentityName).FirstOrDefault();
                    db.Rooms.Remove(room);
                    db.SaveChanges();

                    // Odayı listeden sil
                    var roomViewModel = _Rooms.First<RoomViewModel>(r => r.Name == roomName);
                    _Rooms.Remove(roomViewModel);

                    // Kullancıyı lobbye yönlendir
                    Clients.Group(roomName).onRoomDeleted(string.Format("Oda silindi!", roomName));

                    // Tüm kullancııların oda listesini güncelle
                    Clients.All.removeChatRoom(roomViewModel);
                }
            }
            catch (Exception)
            {
                Clients.Caller.onError("Bu odayı silme yetkiniz yok!");
            }
        }

        public IEnumerable<MessageViewModel> GetMessageHistory(string roomName)
        {
            using (var db = new ApplicationDbContext())
            {
                var messageHistory = db.Messages.Where(m => m.ToRoom.Name == roomName)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(20)
                    .AsEnumerable()
                    .Reverse()
                    .ToList();
                return Mapper.Map<IEnumerable<Message>, IEnumerable<MessageViewModel>>(messageHistory);
            }
        }
        public IEnumerable<RoomViewModel> GetRooms()
        {
            using (var db = new ApplicationDbContext())
            {          
                if (_Rooms.Count == 0)
                {
                    foreach (var room in db.Rooms)
                    {
                        var roomViewModel = Mapper.Map<Room, RoomViewModel>(room);
                        _Rooms.Add(roomViewModel);
                    }
                }
            }
            return _Rooms.ToList();
        }

        public IEnumerable<UserViewModel> GetUsers(string roomName)
        {
            return _Connections.Where(u => u.CurrentRoom == roomName).ToList();
        }



     
        public override Task OnConnected()
        {
            using (var db = new ApplicationDbContext())
            {
                try
                {
                    var user = db.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();

                    var userViewModel = Mapper.Map<ApplicationUser, UserViewModel>(user);

                    userViewModel.CurrentRoom = "";

                    if (!_Connections.Any(u => u.Username == IdentityName))
                    {
                        _Connections.Add(userViewModel);
                        _ConnectionsMap.Add(IdentityName, Context.ConnectionId);
                    }

                    Clients.Caller.getProfileInfo(user.DisplayName);
                }
                catch (Exception ex)
                {
                    Clients.Caller.onError("Bağlantı:" + ex.Message);
                }
            }

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            try
            {
                var user = _Connections.Where(u => u.Username == IdentityName).First();
                _Connections.Remove(user);

                // kullanıcı lsitesinden kaldırıldı
                Clients.OthersInGroup(user.CurrentRoom).removeUser(user);

               // eşleşme kaldırıldı
                _ConnectionsMap.Remove(user.Username);
            }
            catch (Exception ex)
            {
                Clients.Caller.onError("Bağlantı kesildi: " + ex.Message);
            }

            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            var user = _Connections.Where(u => u.Username == IdentityName).FirstOrDefault();
            if (user != null)
                Clients.Caller.getProfileInfo(user.DisplayName);

            return base.OnReconnected();
        }
    

        private string IdentityName
        {
            get { return Context.User.Identity.Name; }
        }
    }
}

