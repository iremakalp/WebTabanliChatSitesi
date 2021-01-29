using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace WebApplication6.Helpers
{
    /// <summary>
    /// Parse message for emoji macros
    /// </summary>
    public class BasicEmojis
    {
        public static string ParseEmojis(string content)
        {
            content = content.Replace(":)", Img("emoji1.png"));
            content = content.Replace(":P", Img("emoji2.png"));
            content = content.Replace(":O", Img("emoji3.png"));
            content = content.Replace(":-)", Img("emoji4.png"));
            content = content.Replace("B|", Img("emoji5.png"));
            content = content.Replace("<3", Img("emoji7.png"));

            return content;
        }

        private static string Img(string imageName)
        {
            return ("<img class=\"emoji\" src=\"/Content/emojis/" + imageName + "\">");
        }
    }
}
