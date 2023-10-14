using Discord.WebSocket;
using Discord;
using ProjectTemplate.Objects;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Threading;
using Spectre.Console;
using System.IO;
using Newtonsoft.Json;
using Discord.Rest;
using System.Linq;
using CountingBot;

namespace ProjectTemplate
{
    public class Program
    {
        public static Dictionary<string, Preset> presetsJSON;
        public static DiscordSocketClient? client;
        public static ChatBuilder chatBuilder;
        public static DateTime runtime;
        public static Preset? currentPreset;
        public static bool maintenance = false;
        private static Timer? _timer = null;
        private static Commands cmds = null;
        private static Events events = null;
        private static JSONPresetBuilder jsonPresetBuilder = null;

        // Settings

        public static string name = "Template"; // Name used in the menu
        public static string color = "blue"; // Color used in the menu      For all colors check https://spectreconsole.net/appendix/colors
        public static List<string> options = new List<string>() // Options for bots/presets, to get do "PresetObject:GetValueOfOption("option name")", to see more check the wiki on github
        {
            "developer"
        };
        public static string[] jsonFiles = new string[] // JSON Files that will be created for every preset    DO NOT INCLUDE .JSON
        {
            "servers"
        };

        public static Game status = new Game("with the template", ActivityType.Playing); // Used to display the bot status
        public static bool enableStatus = true; // Displays the status for EVERY preset/bot, if u disable it u have to set the status with code yourself (for example use preset options)

        public static GatewayIntents gatewayIntents = GatewayIntents.All;

        static async Task Main(string[] args)
        {
            Console.Clear();
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(path: "logs.txt", shared: true)
            .CreateLogger();
            DiscordMenuBuilder builder = new DiscordMenuBuilder();
            builder.Create(name, color, options);
            await builder.StartMenu();
            await Task.Delay(-1);
        }

        public static async Task Run()
        {
            Console.Clear();
            runtime = DateTime.Now;
            DiscordSocketConfig config = new();
            config.AlwaysDownloadUsers = true;
            config.MessageCacheSize = 100;
            config.GatewayIntents = gatewayIntents;
            cmds = new Commands();
            jsonPresetBuilder = new JSONPresetBuilder(jsonFiles);
            client = new DiscordSocketClient(config);
            client.Log += LogAsync;
            client.Connected += Connected;
            client.SlashCommandExecuted += cmds.Handle;
            events = new Events();
            chatBuilder = new ChatBuilder();
            await client.LoginAsync(TokenType.Bot, currentPreset.token);
            await client.StartAsync();
            StartTerminal();
            await Task.Delay(-1);
        }

        private static async Task Connected()
        {
            if (maintenance)
            {
                Log.Information("Starting maintenance...");
                maintenance = true;
                await client.SetStatusAsync(UserStatus.DoNotDisturb);
                await client.SetGameAsync("being under maintenance", type: ActivityType.Watching);
                Log.Information("Started maintenance mode!");
            }
            else
            {
                if (enableStatus)
                {
                    await client.SetGameAsync(status.Name, type: status.Type);
                    Log.Information("Bot's status set");
                }
            }
            Log.Debug("Starting creation of slash commands...");
            await cmds.Create();
            await jsonPresetBuilder.Create(currentPreset);
            _timer = new Timer(a =>
            {
                TimeSpan time = DateTime.Now - runtime;
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        }

        private static async Task LogAsync(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Error:
                    Log.Error(message.Exception, message.Message);
                    break;
                case LogSeverity.Debug:
                    Log.Debug(message.Exception, message.Message);
                    break;
                case LogSeverity.Warning:
                    Log.Warning(message.Exception, message.Message);
                    break;
                case LogSeverity.Critical:
                    Log.Error(message.Exception, message.Message);
                    break;
                case LogSeverity.Info:
                    Log.Information(message.Exception, message.Message);
                    break;
                case LogSeverity.Verbose:
                    Log.Verbose(message.Exception, message.Message);
                    break;
            }

            await Task.CompletedTask;
        }

        public async static void StartTerminal()
        {
            string cmd = Console.ReadLine();
            string[] args;
            try
            {
                args = cmd.Split(" ");
            }
            catch
            {
                Console.WriteLine("Something happened that made arguments variable to throw an error, please try again");
                StartTerminal();
                return;
            }
            if (chatBuilder._return) return;
            switch (args[0].ToLower())
            {
                case "stop":
                    Log.Information($"Stopping the bot...");
                    await client.StopAsync();
                    Log.Information($"Stopped the bot");
                    Log.Information($"Closing the application in 5 seconds...");
                    await Task.Delay(5000);
                    Environment.Exit(-1);
                    break;
                case "leaveserver":
                    Log.Information($"Leaving the server...");
                    var guild = client.GetGuild(ulong.Parse(args[1]));
                    await guild.LeaveAsync();
                    Log.Information($"Successfully left the server '{guild.Name}'");
                    StartTerminal();
                    break;
                case "servers":
                    Log.Information($"Found {client.CurrentUser.MutualGuilds.Count} servers:");
                    AnsiConsole.Write(new Rule($"[{color}] Servers [/]"));
                    foreach (var g in client.CurrentUser.MutualGuilds)
                    {
                        AnsiConsole.MarkupLine($"\n[{color}]{g.Name}[/] ({g.Id}): {g.MemberCount} members\n");
                    }
                    AnsiConsole.Write(new Rule());
                    StartTerminal();
                    break;
                case "lookup":
                    Log.Information("Getting guild information...");
                    var gu = client.GetGuild(ulong.Parse(args[1]));
                    string invite = null;
                    bool vanityInvite = false;
                    bool firstInvite = false;
                    bool createdInvite = false;
                    if (gu.CurrentUser.GuildPermissions.ManageGuild)
                    {
                        var invites = await gu.GetInvitesAsync();
                        if (invites != null && invites.Count > 0)
                        {
                            firstInvite = true;
                            invite = invites.First().Code;
                        }
                    }
                    if (invite == null)
                    {
                        if (gu.CurrentUser.GuildPermissions.ManageGuild)
                        {
                            var v = await gu.GetVanityInviteAsync();
                            if (v != null)
                            {
                                Log.Information("Invite found");
                                invite = v.Code;
                                vanityInvite = true;
                            }
                        }
                        else {

                            if (gu.CurrentUser.GuildPermissions.CreateInstantInvite)
                            {
                                var i = await gu.DefaultChannel.CreateInviteAsync(maxAge: 86400);
                                invite = i.Code;
                                Log.Information("Invite created");
                                createdInvite = true;
                            }
                        }
                    }
                    if (invite == null)
                    {
                        Log.Information("We were unable to retrieve/create invite");
                        invite = "Unable to retrieve invite";
                    }
                    else
                    {
                        invite = $"https://discord.gg/{invite}";
                    }
                    var users = gu.Users;
                    int totalUsers = users.Where(user => user.IsBot == false).Count();
                    int online = 0;
                    int offline = 0;
                    int bots = 0;
                    foreach (IGuildUser user in users)
                    {
                        if (user.IsBot) bots++;
                        else if (user.Status == UserStatus.Online) online++;
                        else if (user.Status == UserStatus.Offline) offline++;
                    }
                    AnsiConsole.Write(new Rule($"[{color}] Guild Info [/]"));
                    AnsiConsole.Write(new Markup($@"
ID: {gu.Id}
Name: {gu.Name}
Description: {gu.Description}
Owner: {gu.Owner.Username} ({gu.Owner.Id})
Created At: {gu.CreatedAt.ToString("g")}

Total Members: {totalUsers}
Online Members: {online}
Offline Members: {offline}
Bots: {bots}

Invite Link: {invite}
"));
                    if(invite == "Unable to retrieve invite")
                    {
                        AnsiConsole.Write(new Markup(
                            @"[red]We were unable to retrieve the invite due to these reasons:

  The bot does not have [bold]Manage Guild/Server[/] or [bold]Create Invite[/]
[/]"));
                    }
                    AnsiConsole.Write(new Rule());
                    StartTerminal();
                    break;
                case "help":
                    AnsiConsole.Write(new Rule($"[{color}] Help [/]"));
                    AnsiConsole.Write(new Markup($@"
[bold]Commands[/]
help - Display all available commands
maintenance - Toggle maintenance mode (more described in the wiki found on github)
activity (type) (text) - Change bot activity (Playing, watching etc.)
status (type) - Change bot status (Online, Do not disturb etc.)
stop - Stop the bot
leaveserver (id) - Leave a server
servers - List all servers the bot is in
lookup (id) - Lookup informations about a server
chat - Opens a menu where you can send messages in servers
dm - Opens a menu where you can answer DMs
"));
                    AnsiConsole.Write(new Rule());
                    StartTerminal();
                    break;
                case "maintenace":
                    if (maintenance != true)
                    {
                        Log.Information("Starting maintenance...");
                        maintenance = true;
                        await client.SetStatusAsync(UserStatus.DoNotDisturb);
                        await client.SetGameAsync("being under maintenance", type: ActivityType.Watching);
                        Log.Information("Started maintenance mode!");
                    }
                    else if (maintenance == true)
                    {
                        Log.Information("Disabling maintenance...");
                        maintenance = false;
                        await client.SetStatusAsync(UserStatus.Online);
                        await client.SetGameAsync("your servers!", type: ActivityType.Watching);
                        Log.Information("Disabled maintenance mode!");
                    }
                    StartTerminal();
                    break;
                case "activity":
                    string text = String.Empty;
                    ActivityType type;
                    switch (args[1].ToLower())
                    {
                        case "play":
                            type = ActivityType.Playing; break;
                        case "listen":
                            type = ActivityType.Listening; break;
                        case "watch":
                            type = ActivityType.Watching; break;
                        case "compete":
                            type = ActivityType.Competing; break;
                        case "custom":
                            type = ActivityType.CustomStatus; break;
                        default:
                            type = ActivityType.Playing; break;

                    }
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (i == 0 || i == 1) { }
                        else
                        {

                            text += args[i] + " ";
                        }
                    }
                    await client.SetGameAsync(text, type: type);

                    Log.Information($"Successfully changed the activity to '[${type.ToString()}] ${text}'");
                    StartTerminal();
                    break;
                case "status":
                    UserStatus status;
                    switch (args[1].ToLower())
                    {
                        case "dnd":
                            status = UserStatus.DoNotDisturb; break;
                        case "afk":
                            status = UserStatus.AFK; break;
                        case "online":
                            status = UserStatus.Online; break;
                        case "invisible":
                            status = UserStatus.Invisible; break;
                        case "idle":
                            status = UserStatus.Idle; break;
                        case "offline":
                            status = UserStatus.Offline; break;
                        default:
                            status = UserStatus.Online; break;

                    }
                    await client.SetStatusAsync(status);
                    Log.Information($"Successfully changed the activity to ${status.ToString()}");
                    StartTerminal();
                    break;
                case "chat":
                    await chatBuilder.Start(ChatBuilder.ChatType.Chat);
                    StartTerminal();
                    break;
                case "dm":
                    await chatBuilder.Start(ChatBuilder.ChatType.DMs);
                    StartTerminal();
                    break;
                default:
                    Log.Warning("No command found! Try using 'help' command to see available commands");
                    StartTerminal();
                    break;
            }
        }
    }
}