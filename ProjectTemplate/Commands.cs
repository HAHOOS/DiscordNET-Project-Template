using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectTemplate
{
    internal class Commands
    {

        public async Task Create()
        {
            var commands = new List<ApplicationCommandProperties>();
            #region Slash Commands
            var pingCmd = new SlashCommandBuilder()
                .WithName("ping")
                .WithDescription("Ping pong!");
            #endregion
            Log.Information("Creating slash commands...");
            try
            {
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
                await Program.client.BulkOverwriteGlobalApplicationCommandsAsync(commands.ToArray());
                Log.Information("Successfully created slash commands!");
            }
            catch (HttpException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Log.Error("An unexpected error occured while creating slash commands:\n" + json + Environment.NewLine + "Line: " + exception.Source);
            }
        }

        internal async Task Handle(SocketSlashCommand cmd)
        {
            if (Program.maintenance && cmd.User.Id.ToString() == (string)Program.currentPreset.GetValueOfOption("developer"))
            {
                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle("Maintenance")
                    .WithDescription("The bot is currently in maintenance mode which lets only developers use the functions, try again later!")
                    .WithCurrentTimestamp()
                    .WithColor(Discord.Color.Red);
                await cmd.RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }
            switch (cmd.CommandName)
            {
                case "ping":
                    await PingCommand(cmd);
                    break;
            }
        }

        private async Task PingCommand(SocketSlashCommand cmd)
        {
            await cmd.RespondAsync("Pong!");
        }
    }
}
