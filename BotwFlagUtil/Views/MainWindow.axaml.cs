using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform;
using BotwFlagUtil.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System;
using System.IO;

namespace BotwFlagUtil.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent(true);

        DataContext = new MainWindowViewModel();

        About.Click += About_Click;
        Exit.Click += Exit_Click;
        Export.Click += Export_Click;
        Import.Click += Import_Click;
        Open.Click += Open_ClickAsync;
        Save.Click += Save_Click;
        Settings.Click += Settings_Click;
    }

    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        string message;
        WindowIcon icon;
        using (var streamReader = new StreamReader(AssetLoader.Open(new("avares://BotwFlagUtil/Assets/about.md"))))
        {
            message = streamReader.ReadToEnd();
        }
        using (var streamReader = AssetLoader.Open(new("avares://BotwFlagUtil/Assets/icon.png")))
        {
            icon = new(streamReader);
        }
        await MessageBoxManager.GetMessageBoxStandard(
            new MessageBoxStandardParams()
            {
                ButtonDefinitions = ButtonEnum.Ok,
                ContentTitle = "About",
                ContentMessage = message,
                Markdown = true,
                MaxHeight = 800,
                Width = 500,
                WindowIcon = icon,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            }).ShowWindowDialogAsync(this);
    }

    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        if ((DataContext as MainWindowViewModel)!.NeedsSave)
        {
            var result = MessageBoxManager.GetMessageBoxStandard(
                new MessageBoxStandardParams()
                {
                    ButtonDefinitions = ButtonEnum.YesNo,
                    ContentTitle = "Discard Changes",
                    ContentMessage = "You are about to discard unsaved changes. Continue?",
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                }).ShowWindowDialogAsync(this);
            if (result.Result == ButtonResult.No)
            {
                return;
            }
        }
        if (Avalonia.Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.Shutdown();
        }
    }

    private void Export_Click(object? sender, RoutedEventArgs e)
    {
        (DataContext as MainWindowViewModel)!.Export();
    }

    private void Import_Click(object? sender, RoutedEventArgs e)
    {
        (DataContext as MainWindowViewModel)!.Import();
    }

    private async void Open_ClickAsync(object? sender, RoutedEventArgs e)
    {
        if ((DataContext as MainWindowViewModel)!.NeedsSave)
        {
            var result = MessageBoxManager.GetMessageBoxStandard(
                new MessageBoxStandardParams()
                {
                    ButtonDefinitions = ButtonEnum.YesNo,
                    ContentTitle = "Discard Changes",
                    ContentMessage = "You are about to discard unsaved changes. Continue?",
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                }).ShowWindowDialogAsync(this);
            if (result.Result == ButtonResult.No)
            {
                return;
            }
        }
        var selection = await StorageProvider.OpenFolderPickerAsync(
                new()
                {
                    Title = "Select the root folder of your mod",
                    AllowMultiple = false,
                }
            );
        if (selection.Count != 1)
        {
            return;
        }
        string folder = Uri.UnescapeDataString(selection[0].Path.AbsolutePath);
        if (!string.IsNullOrEmpty(folder))
        {
            (DataContext as MainWindowViewModel)!.OpenMod(folder);
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        (DataContext as MainWindowViewModel)!.Save();
    }

    private async void Settings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await new SettingsWindow().ShowDialog(this);
    }
}