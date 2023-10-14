using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ProjectTemplate.Objects;
using Serilog;
using ProjectTemplate;

namespace CountingBot
{
    internal class JSONPresetBuilder
    {

        public string[] jsonFiles;
        public DirectoryInfo jsonDirectory;

        public JSONPresetBuilder(string[] jsonFiles)
        {
            jsonDirectory = new DirectoryInfo(Directory.GetCurrentDirectory()).GetDirectories().ToList().Find(x => x.Name == "JSON");
            if (jsonDirectory == null) 
            {
                Log.Information("JSON directory not found, creating...");
                try
                {
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "JSON"));
                    Log.Information("JSON directory successfully created!");
                }
                catch(Exception ex)
                {
                    Log.Information("JSON directory has failed to create");
                }
            } else
            {
                Log.Information("JSON directory found!");
            }
            this.jsonFiles = jsonFiles;
        }

        public async Task Create(Preset preset)
        {
            Log.Information("Creating JSON files for the current preset...");
            DirectoryInfo directory = jsonDirectory.GetDirectories().ToList().Find(x => x.Name == preset.name);
            if (directory == null) directory = jsonDirectory.CreateSubdirectory(preset.name);
            foreach (var file in jsonFiles)
            {
                if(directory.GetFiles().ToList().Find(x=>x.Name==$"{file}.json") == null)
                {
                    Log.Information($"{file}.json not found! Creating...");
                    try
                    {
                        File.Create(Path.Combine(directory.FullName, $"{file}.json"));
                        Log.Information($"{file}.json successfully created!");
                    }
                    catch (Exception ex)
                    {
                        Log.Information($"{file}.json has failed to create");
                    }
                }
                else
                {
                    Log.Information($"{file}.json was found!");
                }
            }
        }

        /// <summary>
        /// Get a path to a JSON file in a preset
        /// </summary>
        /// <param name="preset">The preset you want to get the json file from</param>
        /// <param name="fileName">The JSON file name (Do not include the extension .json)</param>
        /// <returns>A string that includes the path to the file; If preset or fileName is null, the function will return null. Returns string</returns>
        public static string GetPathToJSON(Preset preset, string fileName)
        {
            if (preset == null || fileName == null || fileName == "") return null;
            return $"{Directory.GetCurrentDirectory()}/JSON/{Program.currentPreset.name}/{fileName}.json";
        }
    }
}
