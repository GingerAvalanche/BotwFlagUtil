using Avalonia;
using Avalonia.Styling;
using System.Text.Json;
using ReactiveUI;
using System;
using System.IO;

namespace BotwFlagUtil.ViewModels
{
    public enum Theme
    {
        Dark,
        Light
    }

    public struct AppSettings
    {
        public Theme Theme { get; set; }
    }

    public class SettingsViewModel : ViewModelBase
    {
        // Static Variables/Fields
        private static readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "botw_tools", "flag_settings.json");

        // Instance Variables/Fields
        private readonly Settings settings;
        private AppSettings appSettings;
        public string GameDir { get => settings.gameDir; set => this.RaiseAndSetIfChanged(ref settings.gameDir, value); }
        public string GameDirNx { get => settings.gameDirNx; set => this.RaiseAndSetIfChanged(ref settings.gameDirNx, value); }
        public string UpdateDir { get => settings.updateDir; set => this.RaiseAndSetIfChanged(ref settings.updateDir, value); }
        public string DlcDir { get => settings.dlcDir; set => this.RaiseAndSetIfChanged(ref settings.dlcDir, value); }
        public string DlcDirNx { get => settings.dlcDirNx; set => this.RaiseAndSetIfChanged(ref settings.dlcDirNx, value); }
        public bool LightTheme { get => appSettings.Theme == Theme.Light; }
        public bool DarkTheme { get => appSettings.Theme == Theme.Dark; }

        public SettingsViewModel()
        {
            settings = Settings.Load();
            if (File.Exists(settingsPath))
            {
                appSettings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsPath));
            }
            else
            {
                appSettings = default;
            }
        }

        public void OnThemeSelected(int themeNum)
        {
            appSettings.Theme = (Theme)themeNum;
            ThemeVariant theme = themeNum == 0 ? ThemeVariant.Dark: ThemeVariant.Light;
            Application.Current!.RequestedThemeVariant = theme;
        }

        public void SaveSettings()
        {
            settings.Save();
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(appSettings, Helpers.jsOpt));
        }
    }
}
