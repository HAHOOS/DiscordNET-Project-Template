using Discord.WebSocket;
using Discord;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ProjectTemplate.Objects;

namespace ProjectTemplate
{
    public class DiscordMenuBuilder
    {

        string name;
        string textColor;
        List<string> settings;
        bool created;

        public DiscordMenuBuilder() {
        
        }
        
        public void Create(string name, string textColor, List<string> settings)
        {
            this.name = name;
            this.textColor = textColor;
            this.settings = settings;
            created = true;
        }

        public async Task EditPreset(Preset preset, string option)
        {
            var newPreset = preset;
            Console.Write($"{Environment.NewLine}Type the new value: ");
            string v = Console.ReadLine();
            Console.WriteLine("Changing the value...");
            newPreset.ChangeOption(option, v);
            Program.presetsJSON.Remove(preset.name);
            Program.presetsJSON[newPreset.name] = newPreset;
            Console.WriteLine("Changed the value");
            string jsonS = System.Text.Json.JsonSerializer.Serialize(Program.presetsJSON);
            File.WriteAllText("presets.json", jsonS);
        }
        
        public async Task ShowPresetActions(Preset preset)
        {
            Console.WriteLine("Currently selected: " + preset.name);
            var paOpt = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
.PageSize(10)
 .Title($"[{textColor}] {name} | Select an action[/]")
 .HighlightStyle(new Style().Foreground(Spectre.Console.Color.Black).Background(Spectre.Console.Color.White).Decoration(Decoration.Bold))
.AddChoices("Edit", "Delete", "Preview", "Test Functionality", "Back"));
            if (paOpt == "Preview")
            {
                Console.WriteLine(
                    $@"[MAIN]
 Name: {preset.name}
 Token: [RESTRICTED]
[SETTINGS]"
                    );
                foreach(var item in preset.settings)
                {
                    Console.WriteLine(" "+item.name + ": " + item.value);
                }
                Console.ReadKey();
                ShowPresetActions(preset);
                return;
            }
            else if (paOpt == "Test Functionality")
            {
                AnsiConsole.Write(new Markup("[lime]Testing the token...[/]"));
                DiscordSocketClient c = new DiscordSocketClient();
                await c.LoginAsync(TokenType.Bot, preset.token);
                if (c.LoginState != LoginState.LoggedIn)
                {
                    AnsiConsole.Write(new Markup($"{Environment.NewLine}[red]Token test failed[/]"));
                }
                else
                {
                    AnsiConsole.Write(new Markup($"{Environment.NewLine}[lime]Token test success[/]"));
                }
                Console.ReadKey();
                ShowPresetActions(preset);
                return;
            }
            else if (paOpt == "Delete")
            {
                AnsiConsole.Write(new Markup($"{Environment.NewLine}[red]Are you sure you want to delete this preset? This action is permament.  Y/N[/]"));
                char a = Console.ReadKey().KeyChar;
                if (a.ToString().ToLower() == "y")
                {
                    AnsiConsole.Write(new Markup($"{Environment.NewLine}[red]Removing....[/]"));
                    Program.presetsJSON.Remove(preset.name);
                    string jsonS = System.Text.Json.JsonSerializer.Serialize(Program.presetsJSON);
                    File.WriteAllText("presets.json", jsonS);
                    AnsiConsole.Write(new Markup($"{Environment.NewLine}[lime]Successfully removed the preset[/]"));
                    Console.ReadKey();
                    ShowPresetActions(preset);
                    return;
                }
                else
                {
                    AnsiConsole.Write(new Markup($"{Environment.NewLine}[lime]Cancelled.[/]"));
                    Console.ReadKey();
                    ShowPresetActions(preset);
                    return;
                }
            }
            else if (paOpt == "Back")
            {
                OptionSelected("Presets");
                return;
            }
            else if (paOpt == "Edit")
            {
                var eOpt = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
.PageSize(10)
 .Title($"[{textColor}] {name} | Select an option to edit[/]")
 .HighlightStyle(new Style().Foreground(Spectre.Console.Color.Black).Background(Spectre.Console.Color.White).Decoration(Decoration.Bold))
.AddChoices(settings));
                await EditPreset(preset, eOpt);
                Console.ReadKey();
                ShowPresetActions(preset);
            }
        }

        public async Task OptionSelected(string option)
        {
            if (option == "Exit") Environment.Exit(0);
            if (option == "Start")
            {
                #region Function
                if (Program.presetsJSON.Count <= 0)
                {
                    Log.Error("Presets are empty, please create a new one in the option 'Presets'");
                    Console.ReadKey();
                    StartMenu();
                    return;
                }
                var list = Program.presetsJSON.Keys.ToList();
                list.Add("Cancel");
                var sOpt = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
        .PageSize(10)
         .Title($"[{textColor}] {name} | Select a preset[/]")
         .MoreChoicesText("[grey](Move up and down to reveal more presets)[/]")
         .HighlightStyle(new Style().Foreground(Spectre.Console.Color.Black).Background(Spectre.Console.Color.White).Decoration(Decoration.Bold))
        .AddChoices(list));
                if (sOpt == "Cancel")
                {
                    StartMenu();
                    return;
                }
                Preset selected = Program.presetsJSON.ToList().Find(x => x.Key == sOpt).Value;
                if (selected == null)
                {
                    Log.Error("Unable to retrieve the preset, please try again");
                    StartMenu();
                    return;
                }
                Program.currentPreset = selected;
                await Program.Run();
                #endregion
            }
            if (option == "Presets")
            {
                #region Function
                var list = Program.presetsJSON.Keys.ToList();
                list.Add("CREATE NEW");
                list.Add("Cancel");
                var pOpt = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
        .PageSize(10)
         .Title($"[{textColor}] {name} | Select a preset/option[/]")
         .MoreChoicesText("[grey](Move up and down to reveal more presets/options)[/]")
         .HighlightStyle(new Style().Foreground(Spectre.Console.Color.Black).Background(Spectre.Console.Color.White).Decoration(Decoration.Bold))
        .AddChoices(list));
                if (pOpt == "CREATE NEW")
                {
                    Console.Write($"{Environment.NewLine}What is the name: ");
                    string title = Console.ReadLine();
                    Console.Write($"{Environment.NewLine}What is the token: ");
                    var token = Console.ReadLine();
                    /*
                    ConsoleKey key;
                    do
                    {
                        var keyInfo = Console.ReadKey(intercept: true);
                        key = keyInfo.Key;

                        if (key == ConsoleKey.Backspace && token.Length > 0)
                        {
                            Console.Write("\b \b");
                            token = token[0..^1];
                        }
                        else if (!char.IsControl(keyInfo.KeyChar))
                        {
                            Console.Write("*");
                            token += keyInfo.KeyChar;
                        }
                    } while (key != ConsoleKey.Enter);
                    */
                    Console.Write($"{Environment.NewLine}[SETTINGS] ");
                    var settings = new List<Setting>();
                    foreach (string name in this.settings)
                    {
                        Console.Write($"{Environment.NewLine}What is the " + name + ": ");
                        object value = Console.ReadLine();
                        settings.Add(
                            new Setting()
                            {
                                name = name,
                                value = value
                            }
                            );


                    };
                    Console.WriteLine("Creating a new preset....");
                    Program.presetsJSON.Add(title, new Preset()
                    {
                        name = title,
                        token = token,
                        settings = settings
                    }
                        );
                    string jsonS = System.Text.Json.JsonSerializer.Serialize(Program.presetsJSON);
                    File.WriteAllText("presets.json", jsonS);
                    Console.WriteLine("Successfully created a new preset!");
                    Console.ReadKey();
                    OptionSelected("Presets");
                    return;

                }
                else if (pOpt == "Cancel")
                {
                    StartMenu();
                    return;
                }
                else
                {
                    Preset selected = Program.presetsJSON.ToList().Find(x => x.Key == pOpt).Value;
                    if (selected == null) { Log.Error("Unable to retrieve the preset, please try again");
                        Console.ReadKey();
                        StartMenu();
                        return;
                    }
                    ShowPresetActions(selected);
                }
                #endregion
            }
        }

        public async Task StartMenu()
        {
            if (!created)
            {
                Log.Error("Could not start a menu that wasn't created properly, u must run :Create() function first and inpout arguments");
                return;
            }
            Log.Information("Loading presets.json.... (if nothing will show after 10 seconds, it means that the file does not exist)");
            string presetsJson = File.ReadAllText("presets.json");
            try
            {
                var json = JsonConvert.DeserializeObject<Dictionary<string, Preset>>(presetsJson);
                Program.presetsJSON = json;
                Log.Information("Loaded presets.json successfully!");
            }
            catch (Exception e)
            {
                Log.Error($"presets.json has thrown an error while loading. \nError: {e.Message}");
            }

            var opt = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
        .PageSize(10)
         .Title($"[{textColor}] {name} | Select an option[/]")
         .HighlightStyle(new Style().Foreground(Spectre.Console.Color.Black).Background(Spectre.Console.Color.White).Decoration(Decoration.Bold))
        .AddChoices(new[] {
           "Start", "Presets", "Exit"
        }));
            OptionSelected(opt);
            await Task.Delay(-1);
        }
    }
}
