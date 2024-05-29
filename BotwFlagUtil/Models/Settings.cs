using SharpYaml.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotwFlagUtil
{
    internal class Settings(string gameDir, string updateDir, string dlcDir, string gameDirNx, string dlcDirNx)
    {
        private static readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "botw_tools", "settings.json");
        public bool WiiU = true;
        [JsonInclude]
        public string gameDir = gameDir;
        [JsonInclude]
        public string gameDirNx = gameDirNx;
        [JsonInclude]
        public string updateDir = updateDir;
        [JsonInclude]
        public string dlcDir = dlcDir;
        [JsonInclude]
        public string dlcDirNx = dlcDirNx;

        public static Settings Load()
        {
            Settings value;
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (File.Exists(settingsPath))
            {
                value = JsonSerializer.Deserialize<Settings>(
                    File.ReadAllText(settingsPath)
                )!;
                if (Validate(value))
                {
                    return value;
                }
            }
            else if (File.Exists(Path.Combine(appdata, "bcml", "settings.json")))
            {
                Dictionary<string, dynamic> bcmlSettings =
                    JsonSerializer.Deserialize<Dictionary<string, dynamic>>(
                        File.ReadAllText(Path.Combine(appdata, "bcml", "settings.json"))
                    )!;
                value = new(
                    bcmlSettings["game_dir"],
                    bcmlSettings["update_dir"],
                    bcmlSettings["dlc_dir"],
                    bcmlSettings["game_dir_nx"],
                    bcmlSettings["dlc_dir_nx"]
                );
            }
            else
            {
                value = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            if (!Directory.Exists(Directory.GetParent(settingsPath)!.FullName))
            {
                Directory.CreateDirectory(Directory.GetParent(settingsPath)!.FullName);
            }
            File.WriteAllText(
                settingsPath,
                JsonSerializer.Serialize(value, Helpers.jsOpt)
            );
            return value;
        }

        public void Save()
        {
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(this, Helpers.jsOpt));
        }

        public static bool Validate(Settings value)
        {
            if (value == null || (value.gameDir == string.Empty && value.gameDirNx == string.Empty))
            {
                Console.WriteLine("Must have game dumps for at least one of the consoles.");
                return false;
            }
            if (value.gameDir != string.Empty)
            {
                if (!File.Exists(Path.Combine(value.gameDir, "Pack", "Dungeon000.pack")))
                {
                    Console.WriteLine("WiiU game dump failed to validate.");
                    return false;
                }
                if (!File.Exists(Path.Combine(value.updateDir, "Actor", "Pack", "FldObj_MountainSnow_A_M_02.sbactorpack")))
                {
                    Console.WriteLine(Path.Combine(value.updateDir, "Actor", "Pack", "FldObj_MountainSnow_A_M_02.sbactorpack"));
                    Console.WriteLine("WiiU update dump failed to validate.");
                    return false;
                }
                if (value.dlcDir != string.Empty && !File.Exists(Path.Combine(value.dlcDir, "Pack", "AocMainField.pack")))
                {
                    Console.WriteLine("WiiU DLC dump failed to validate.");
                    return false;
                }
            }
            if (value.gameDirNx != string.Empty)
            {
                if (!File.Exists(Path.Combine(value.gameDirNx, "Pack", "Dungeon000.pack")))
                {
                    Console.WriteLine("Switch game dump failed to validate.");
                    return false;
                }
                if (value.dlcDirNx != string.Empty && !File.Exists(Path.Combine(value.dlcDirNx, "Pack", "AocMainField.pack")))
                {
                    Console.WriteLine("Switch DLC dump failed to validate.");
                    return false;
                }
            }

            return true;
        }

        public static bool ValidateGameDir(string path) => File.Exists(Path.Combine(path, "Pack", "Dungeon000.pack"));
        public static bool ValidateUpdateDir(string path) => File.Exists(Path.Combine(path, "Actor", "Pack", "FldObj_MountainSnow_A_M_02.sbactorpack"));
        public static bool ValidateDlcDir(string path) => File.Exists(Path.Combine(path, "Pack", "AocMainField.pack"));
        public static bool ValidateGameDirNx(string path) => ValidateGameDir(path) & ValidateUpdateDir(path);
        public static bool ValidateDlcDirNx(string path) => ValidateDlcDir(path);
    }
}
