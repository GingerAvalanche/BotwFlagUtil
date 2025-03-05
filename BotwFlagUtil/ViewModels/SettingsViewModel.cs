﻿using Avalonia;
using Avalonia.Styling;
using System.Text.Json;
using ReactiveUI;
using System;
using System.IO;
using BotwFlagUtil.Models;

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
        private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "botw_tools", "flag_settings.json");

        // Instance Variables/Fields
        private readonly Settings settings = Settings.Load();
        private AppSettings appSettings = File.Exists(SettingsPath) ?
            JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) : default;

        public string GameDir
        {
            get => settings.GameDir;
            set => this.RaiseAndSetIfChanged(ref settings.GameDir, value);
        }

        public string GameDirNx
        {
            get => settings.GameDirNx;
            set => this.RaiseAndSetIfChanged(ref settings.GameDirNx, value);
        }

        public string UpdateDir
        {
            get => settings.UpdateDir;
            set => this.RaiseAndSetIfChanged(ref settings.UpdateDir, value);
        }

        public string DlcDir
        {
            get => settings.DlcDir;
            set => this.RaiseAndSetIfChanged(ref settings.DlcDir, value);
        }

        public string DlcDirNx
        {
            get => settings.DlcDirNx;
            set => this.RaiseAndSetIfChanged(ref settings.DlcDirNx, value);
        }
        
        public bool LightTheme => appSettings.Theme == Theme.Light;
        public bool DarkTheme => appSettings.Theme == Theme.Dark;

        public void OnThemeSelected(int themeNum)
        {
            appSettings.Theme = (Theme)themeNum;
            ThemeVariant theme = themeNum == 0 ? ThemeVariant.Dark: ThemeVariant.Light;
            Application.Current!.RequestedThemeVariant = theme;
        }

        public void SaveSettings()
        {
            settings.Save();
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(appSettings, Helpers.JsOpt));
        }
    }
}
