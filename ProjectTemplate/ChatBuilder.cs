using Discord;
using Discord.WebSocket;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProjectTemplate
{
    public class ChatBuilder
    {

        DiscordSocketClient client = Program.client;
        public bool _return = false;

        IMessageChannel sChannel;

        public enum ChatType
        {
            DMs,
            Chat
        }


        public ChatBuilder() { 
        
        }

        public async Task AwaitMessage(IMessageChannel chnl, ulong guild)
        {
            
            string content = Console.ReadLine();
            if (content.ToLower() == "/leave")
            {
                _return = true;
            }
            else
            {
                if (content.StartsWith("/@"))
                {
                    var strings = content.Split(" ");
                    string id = strings[0].Replace("/@", "");
                    content = content.Replace(strings[0] + " ", "");
                    await chnl.SendMessageAsync(content, messageReference: new MessageReference(ulong.Parse(id), chnl.Id, guild));
                }
                else
                {
                    await chnl.SendMessageAsync(content);
                }
            }
            if(_return == false) await AwaitMessage(chnl, guild);
        }

        public async Task AwaitMessage(IDMChannel chnl)
        {

            string content = Console.ReadLine();
            if (content.ToLower() == "/leave")
            {
                _return = true;
            }
            else
            {
                if (content.StartsWith("/@"))
                {
                    var strings = content.Split(" ");
                    string id = strings[0].Replace("/@", "");
                    content = content.Replace(strings[0] + " ", "");
                    await chnl.SendMessageAsync(content, messageReference: new MessageReference(ulong.Parse(id), chnl.Id));
                }else if (content.ToLower() == "/close")
                {
                    await chnl.CloseAsync();
                    _return = true;
                } 
                else
                {
                    await chnl.SendMessageAsync(content);
                }
            }
            if (_return == false) await AwaitMessage(chnl);
        }

        public async Task ChannelSelected(SocketGuild s, string c)
        {
            ITextChannel channel = (ITextChannel)s.Channels.ToList().Find(x => x.Id == ulong.Parse(c.Split(" | ")[1]));
            if (channel != null)
            {
                var messages = await channel.GetMessagesAsync().FlattenAsync<IMessage>();
                messages = messages.Reverse();
                foreach(var msg in messages)
                {
                    var author = (IGuildUser)msg.Author;
                    AnsiConsole.MarkupLine($"[rgb(255,0,0)]{author.Username} ({author.Id})[/] | Message ID: {msg.Id}");
                    if (msg.Content == "" || msg.Content == null)
                    {
                        if (msg.Embeds.Count > 0)
                        {
                            AnsiConsole.MarkupLine("[grey] Embed placed here [/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[grey] "+msg.Content+" [/]");
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[grey] " + msg.Content + " [/]");
                    }
                }
                sChannel = channel;
                client.MessageReceived += MessageReceived;
                await AwaitMessage(channel, s.Id);
                sChannel = null;
                client.MessageReceived -= MessageReceived;
                AnsiConsole.Clear();
                await ServerSelected(s.Name, true);
            }
        }

        private Task MessageReceived(SocketMessage msg)
        {
            if (msg.Channel.Id != sChannel.Id) return Task.CompletedTask;
            var author = (IGuildUser)msg.Author;
            AnsiConsole.MarkupLine($"[rgb(255,0,0)]{author.Username} ({author.Id})[/] | Message ID: {msg.Id}");
            if (msg.Content == "" || msg.Content == null)
            {
                if (msg.Embeds.Count > 0)
                {
                    AnsiConsole.MarkupLine("[grey] Embed placed here [/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[grey] " + msg.Content + " [/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[grey] " + msg.Content + " [/]");
            }
            return Task.CompletedTask;
        }

        public async Task ServerSelected(string s, bool writeRule)
        {
            var server = client.CurrentUser.MutualGuilds.ToList().Find(x => x.Name == s);
            List<string> channels = new List<string>();
            foreach (var c in server.Channels)
            {
                if (c.GetChannelType() == ChannelType.Text)
                {
                    channels.Add(c.Name + " | " + c.Id);
                }
            }
            channels.Add("CANCEL");
            if (writeRule) AnsiConsole.Write(new Rule($"[{Program.color}] Discord Chat Menu [/]"));
            var opt = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title("Select a channel")
        .PageSize(10)
         .HighlightStyle(new Style().Foreground(Spectre.Console.Color.Black).Background(Spectre.Console.Color.White).Decoration(Decoration.Bold))
        .AddChoices(channels));
            if (opt == "CANCEL")
            {
                AnsiConsole.Clear();
                await Start(ChatType.Chat);
            }
            else
            {
                await ChannelSelected(server, opt);
            }
        }

        public async Task DMSelected(IDMChannel chnl)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[{Program.color}] DMS with @{chnl.Recipient.Username} [/]"));
            var messages = await chnl.GetMessagesAsync().FlattenAsync<IMessage>();
            messages = messages.Reverse();
            foreach (var msg in messages)
            {
                var author = msg.Author;
                AnsiConsole.MarkupLine($"[rgb(255,0,0)]{author.Username} ({author.Id})[/] | Message ID: {msg.Id}");
                if (msg.Content == "" || msg.Content == null)
                {
                    if (msg.Embeds.Count > 0)
                    {
                        AnsiConsole.MarkupLine("[grey] Embed placed here [/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[grey] " + msg.Content + " [/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[grey] " + msg.Content + " [/]");
                }
            }
            if(messages.Count() <= 0)
            {
                AnsiConsole.MarkupLine("[red] There are no DMs with this person [/]");
            }
            _return = false;
            sChannel = chnl;
            client.MessageReceived += MessageReceived;
            await AwaitMessage(chnl);
            sChannel = null;
            client.MessageReceived -= MessageReceived;
            AnsiConsole.Clear();
            await Start(ChatType.DMs);
        }

        public async Task Start(ChatType type)
        {
            if (type == ChatType.Chat)
            {
                List<string> servers = new List<string>();
                foreach (var s in client.CurrentUser.MutualGuilds)
                {
                    servers.Add(s.Name);
                }
                servers.Add("CANCEL");
                AnsiConsole.Write(new Rule($"[{Program.color}] Discord Chat Menu [/]"));
                var opt = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .Title("Select a server")
            .PageSize(10)
             .HighlightStyle(new Style().Foreground(Spectre.Console.Color.Black).Background(Spectre.Console.Color.White).Decoration(Decoration.Bold))
            .AddChoices(servers));
                if (opt == "CANCEL")
                {
                    AnsiConsole.Clear();
                    return;
                }
                else
                {
                    await ServerSelected(opt, false);
                }
            }else if(type == ChatType.DMs)
            {
                List<string> dms = new List<string>();
                var allDms = await client.GetDMChannelsAsync();
                var listAllDms = allDms.ToList();
                foreach (var dm in allDms)
                {
                    dms.Add("@" + dm.Recipient.Username + " | " + dm.Id);
                }
                dms.Add("NEW RECIPIENT");
                dms.Add("CANCEL");
                AnsiConsole.Write(new Rule($"[{Program.color}] Discord DMs Menu [/]"));
                var opt = AnsiConsole.Prompt(
               new SelectionPrompt<string>()
               .Title("Select a DM Channel")
           .PageSize(10)
            .HighlightStyle(new Style().Foreground(Spectre.Console.Color.Black).Background(Spectre.Console.Color.White).Decoration(Decoration.Bold))
           .AddChoices(dms));
                if (opt == "CANCEL")
                {
                    AnsiConsole.Clear();
                    return;
                }
                else if (opt == "NEW RECIPIENT") {
                    ulong name = AnsiConsole.Ask<ulong>("Who is the [green]recipient[/]? (Enter their ID, not Name)");
                    var user = await client.GetUserAsync(name);
                    var chnl = await user.CreateDMChannelAsync();
                    await DMSelected(chnl);
                } else
                {
                    List<string> split = opt.Split(" | ").ToList();
                    IDMChannel channel = listAllDms.Find(x => x.Id == ulong.Parse(split[1]));
                    await DMSelected(channel);
                }
            }
        }
    }
}
