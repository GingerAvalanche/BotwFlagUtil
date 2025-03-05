using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotwFlagUtil.Models
{
    internal class Settings(string gameDir, string updateDir, string dlcDir, string gameDirNx, string dlcDirNx)
    {
        private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "botw_tools", "settings.json");
        public bool WiiU = true;
        [JsonInclude]
        public string GameDir = gameDir;
        [JsonInclude]
        public string GameDirNx = gameDirNx;
        [JsonInclude]
        public string UpdateDir = updateDir;
        [JsonInclude]
        public string DlcDir = dlcDir;
        [JsonInclude]
        public string DlcDirNx = dlcDirNx;

        public static Settings Load()
        {
            Settings value;
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (File.Exists(SettingsPath))
            {
                value = JsonSerializer.Deserialize<Settings>(
                    File.ReadAllText(SettingsPath)
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
                    bcmlSettings["game_dir"] as string ?? "",
                    bcmlSettings["update_dir"] as string ?? "",
                    bcmlSettings["dlc_dir"] as string ?? "",
                    bcmlSettings["game_dir_nx"] as string ?? "",
                    bcmlSettings["dlc_dir_nx"] as string ?? ""
                );
            }
            else
            {
                value = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            if (!Directory.Exists(Directory.GetParent(SettingsPath)!.FullName))
            {
                Directory.CreateDirectory(Directory.GetParent(SettingsPath)!.FullName);
            }
            File.WriteAllText(
                SettingsPath,
                JsonSerializer.Serialize(value, Helpers.JsOpt)
            );
            return value;
        }

        public void Save()
        {
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, Helpers.JsOpt));
        }

        public static bool Validate(Settings value)
        {
            if (value is null or { GameDir: "", GameDirNx: "" })
            {
                return false;
            }
            if (value.GameDir != string.Empty)
            {
                if (!ValidateGameDir(value.GameDir))
                {
                    return false;
                }
                if (!ValidateUpdateDir(value.UpdateDir))
                {
                    return false;
                }
                if (value.DlcDir != string.Empty && !ValidateDlcDir(value.DlcDir))
                {
                    return false;
                }
            }

            if (value.GameDirNx == string.Empty) return true;
            if (!ValidateGameDirNx(value.GameDirNx))
            {
                return false;
            }
            return value.DlcDirNx == string.Empty || ValidateDlcDirNx(value.DlcDirNx);
        }

        public static bool ValidateGameDir(string path) => File.Exists(Path.Combine(path, "Pack", "Dungeon000.pack"));
        public static bool ValidateUpdateDir(string path) => File.Exists(Path.Combine(path, "Actor", "Pack", "FldObj_MountainSnow_A_M_02.sbactorpack"));
        public static bool ValidateDlcDir(string path) => File.Exists(Path.Combine(path, "Pack", "AocMainField.pack"));
        public static bool ValidateGameDirNx(string path) => ValidateGameDir(path) & ValidateUpdateDir(path);
        public static bool ValidateDlcDirNx(string path) => ValidateDlcDir(path);
    }
}
