using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynDiscord
{
    public class CompileContext
    {
        public DiscordClient Client { get; }
        public DiscordMessage Message { get; }
        public DiscordChannel Channel => Message.Channel;
        public DiscordGuild Guild => Channel.Guild;
        public DiscordUser User => Message.Author;
        public DiscordMember Member { get; }

        public CompileContext(DiscordClient client, DiscordMessage message, DiscordMember member)
        {
            Client = client;
            Message = message;
            Member = member;
        }

        public StringBuilder Output { get; } = new StringBuilder();
    }
}
