using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebApplication6.Helpers;
using WebApplication6.Models;
using WebApplication6.Models.ViewModels;
using AutoMapper;
namespace WebApplication6.Mappings
{
    public class MessageProfile : Profile
    {
        public MessageProfile()
        {
            CreateMap<Message, MessageViewModel>()
                .ForMember(dst => dst.From, opt => opt.MapFrom(x => x.FromUser.DisplayName))
                .ForMember(dst => dst.To, opt => opt.MapFrom(x => x.ToRoom.Name))
                .ForMember(dst => dst.Content, opt => opt.MapFrom(x => BasicEmojis.ParseEmojis(x.Content)))
                .ForMember(dst => dst.Timestamp, opt => opt.MapFrom(x => new DateTime(long.Parse(x.Timestamp)).ToLongTimeString()));

            CreateMap<MessageViewModel, Message>();
        }
    }
}