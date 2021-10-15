using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace RoslynDiscord
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            DiscordConfiguration config = new DiscordConfiguration
            {
                Token = Environment.GetEnvironmentVariable("ROSLYN_TOKEN"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.GuildMessages | DiscordIntents.DirectMessages | DiscordIntents.Guilds
            };

            DiscordClient client = new DiscordClient(config);
            var commandsNext = client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { "!" }
            });
            commandsNext.RegisterCommands<CompileCommands>();

            await client.ConnectAsync();

            await Task.Delay(-1);
        }
    }
}
