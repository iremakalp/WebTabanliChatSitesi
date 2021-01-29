
$(function () {

    var chatHub = $.connection.chatHub;
    $.connection.hub.start().done(function () {
        console.log("TooChat");
        model.roomList();
        model.userList();

        setTimeout(function () {
            if (model.chatRooms().length > 0) {
                model.joinedRoom(model.chatRooms()[0].name())
                model.joinRoom();
            }
        }, 250);
    });

    // Client Operations

    chatHub.client.newMessage = function (messageView) {
        var isMine = messageView.From === model.myName();
        var message = new ChatMessage(messageView.Content,
            messageView.Timestamp,
            messageView.From,
            isMine);
        model.chatMessages.push(message);
        $(".chat-body").animate({ scrollTop: $(".chat-body")[0].scrollHeight }, 1000);
    };

    chatHub.client.getProfileInfo = function (displayName) {
        model.myName(displayName);
    };

    chatHub.client.addUser = function (user) {
        model.userAdded(new ChatUser(user.Username,
            user.DisplayName,
            user.CurrentRoom));
    };

    chatHub.client.removeUser = function (user) {
        model.userRemoved(user.Username);
    };


    $('ul#room-list').on('click', 'a', function () {
        var roomName = $(this).text();
        model.joinedRoom(roomName);
        model.joinRoom();
        model.chatMessages.removeAll();
        $("input#iRoom").val(roomName);
        $('#room-list a').removeClass('active');
        $(this).addClass('active');
    });

    chatHub.client.addChatRoom = function (room) {
        model.roomAdded(new ChatRoom(room.Id, room.Name));
    };

    chatHub.client.removeChatRoom = function (room) {
        model.roomDeleted(room.Id);
    };


    chatHub.client.onRoomDeleted = function (message) {
        model.serverInfoMessage(message);
        $("#errorAlert").removeClass("hidden").show().delay(5000).fadeOut(500);

        if (model.chatRooms().length == 0) {
            model.joinedRoom("");
        }
        else {
            // Join to the first room in list
            $("ul#room-list li a")[0].click();
        }
    };
    chatHub.client.onError = function (message) {
        model.serverInfoMessage(message);

        $("#errorAlert").removeClass("hidden").show().delay(5000).fadeOut(500);
    };


    var Model = function () {
        var self = this;
        self.message = ko.observable("");
        self.chatRooms = ko.observableArray([]);
        self.chatUsers = ko.observableArray([]);
        self.chatMessages = ko.observableArray([]);
        self.joinedRoom = ko.observable("");
        self.serverInfoMessage = ko.observable("");
        self.myName = ko.observable("");
        self.onEnter = function (d, e) {
            if (e.keyCode === 13) {
                self.sendNewMessage();
            }
            return true;
        }
        self.filter = ko.observable("");
        self.filteredChatUsers = ko.computed(function () {
            if (!self.filter()) {
                return self.chatUsers();
            } else {
                return ko.utils.arrayFilter(self.chatUsers(), function (user) {
                    var displayName = user.displayName().toLowerCase();
                    return displayName.includes(self.filter().toLowerCase());
                });
            }
        });

    };

    Model.prototype = {

        // Server Operations
        sendNewMessage: function () {
            var self = this;
            chatHub.server.send(self.joinedRoom(), self.message());
            self.message("");
        },

        joinRoom: function () {
            var self = this;
            chatHub.server.join(self.joinedRoom()).done(function () {
                self.userList();
                self.messageHistory();
            });
        },

        roomList: function () {
            var self = this;
            chatHub.server.getRooms().done(function (result) {
                self.chatRooms.removeAll();
                for (var i = 0; i < result.length; i++) {
                    self.chatRooms.push(new ChatRoom(result[i].Id, result[i].Name));
                }
            });
        },

        userList: function () {
            var self = this;
            chatHub.server.getUsers(self.joinedRoom()).done(function (result) {
                self.chatUsers.removeAll();
                for (var i = 0; i < result.length; i++) {
                    self.chatUsers.push(new ChatUser(result[i].Username,
                        result[i].DisplayName,
                        result[i].CurrentRoom))
                }
            });

        },

        createRoom: function () {
            var name = $("#roomName").val();
            chatHub.server.createRoom(name);
        },

        deleteRoom: function () {
            var self = this;
            chatHub.server.deleteRoom(self.joinedRoom());
        },


        messageHistory: function () {
            var self = this;
            chatHub.server.getMessageHistory(self.joinedRoom()).done(function (result) {
                self.chatMessages.removeAll();
                for (var i = 0; i < result.length; i++) {
                    var isMine = result[i].From == self.myName();
                    self.chatMessages.push(new ChatMessage(result[i].Content,
                                                     result[i].Timestamp,
                                                     result[i].From,
                                                     isMine))
                }

                $(".chat-body").animate({ scrollTop: $(".chat-body")[0].scrollHeight }, 1000);

            });
        },


        roomAdded: function (room) {
            var self = this;
            self.chatRooms.push(room);
        },

        roomDeleted: function(id){
            var self = this;
            var temp;
            ko.utils.arrayForEach(self.chatRooms(), function (room) {
                if (room.roomId() == id)
                    temp = room;
            });
            self.chatRooms.remove(temp);
        },

        userAdded: function (user) {
            var self = this;
            self.chatUsers.push(user)
        },

        userRemoved: function (id) {
            var self = this;
            var temp;
            ko.utils.arrayForEach(self.chatUsers(), function (user) {
                if (user.userName() == id)
                    temp = user;
            });
            self.chatUsers.remove(temp);
        },

    };

    function ChatRoom(roomId, name) {
        var self = this;
        self.roomId = ko.observable(roomId);
        self.name = ko.observable(name);
    }

    function ChatUser(userName, displayName, currentRoom) {
        var self = this;
        self.userName = ko.observable(userName);
        self.displayName = ko.observable(displayName);
        self.currentRoom = ko.observable(currentRoom);
    }

    function ChatMessage(content, timestamp, from, isMine) {
        var self = this;
        self.content = ko.observable(content);
        self.timestamp = ko.observable(timestamp);
        self.from = ko.observable(from);
        self.isMine = ko.observable(isMine);
    }

    var model = new Model();
    ko.applyBindings(model);

});