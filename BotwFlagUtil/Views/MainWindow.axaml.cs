using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using BotwFlagUtil.Enums;
using BotwFlagUtil.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BotwFlagUtil.Views;

public partial class MainWindow : Window
{
    readonly WindowIcon icon;
    public MainWindow()
    {
        InitializeComponent(true);

        MainWindowViewModel vm = new();
        DataContext = vm;

        using (var streamReader = AssetLoader.Open(new("avares://BotwFlagUtil/Assets/icon.png")))
        {
            icon = new(streamReader);
        }

        FlagSelector.DataTemplates.Add(new FlagNameDataTemplate(vm));

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
        using (var streamReader = new StreamReader(AssetLoader.Open(new("avares://BotwFlagUtil/Assets/about.md"))))
        {
            message = streamReader.ReadToEnd();
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

    private async void Exit_Click(object? sender, RoutedEventArgs e)
    {
        if (((MainWindowViewModel)DataContext!).NeedsSave &&
            await DiscardConfirmationDialogue() == ButtonResult.No)
        {
            return;
        }
        if (Avalonia.Application.Current!.ApplicationLifetime is
            IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.Shutdown();
        }
    }

    private void Export_Click(object? sender, RoutedEventArgs e)
    {
        ((MainWindowViewModel)DataContext!).Export();
    }

    private void Import_Click(object? sender, RoutedEventArgs e)
    {
        ((MainWindowViewModel)DataContext!).Import();
    }

    private async void Open_ClickAsync(object? sender, RoutedEventArgs e)
    {
        MainWindowViewModel vm = (MainWindowViewModel)DataContext!;
        if (vm.NeedsSave && await DiscardConfirmationDialogue() == ButtonResult.No)
        {
            return;
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
            vm.OpenMod(folder);
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        MainWindowViewModel vm = (MainWindowViewModel)DataContext!;
        vm.Save();
    }

    private async void Settings_Click(object? sender, RoutedEventArgs e)
    {
        await new SettingsWindow().ShowDialog(this);
    }

    private async Task<ButtonResult> DiscardConfirmationDialogue()
    {
        return await MessageBoxManager.GetMessageBoxStandard(
            new MessageBoxStandardParams()
            {
                ButtonDefinitions = ButtonEnum.YesNo,
                ContentTitle = "Discard Changes",
                ContentMessage = "You are about to discard unsaved changes. Continue?",
                WindowIcon = icon,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            }).ShowWindowDialogAsync(this);
    }

    private class FlagNameDataTemplate(MainWindowViewModel vm) : IDataTemplate
    {
        private readonly Dictionary<string, GeneratorConfidence> confidences = vm.confidences;
        private readonly Dictionary<string, bool> confirmeds = vm.confirmeds;

        public Control Build(object? param)
        {
            string flagName = (string)param!;
            var control = new TextBlock()
            {
                Background = GetBackgroundColor(flagName),
                Foreground = GetForegroundColor(flagName),
                FontWeight = FontWeight.Bold,
                Text = flagName
            };
            return control;
        }

        public bool Match(object? data)
        {
            return data is string flagName && confidences.ContainsKey(flagName);
        }

        public IImmutableSolidColorBrush GetBackgroundColor(string flagName)
        {
            return confirmeds[flagName] ? Brushes.PaleGreen : Brushes.Transparent;
        }

        public IImmutableSolidColorBrush GetForegroundColor(string flagName)
        {
            return confidences[flagName] switch
            {
                GeneratorConfidence.Bad => Brushes.Red,
                GeneratorConfidence.Mediocre => Brushes.Yellow,
                GeneratorConfidence.Good => Brushes.Green,
                GeneratorConfidence.Definite => Brushes.Blue,
                _ => Brushes.Violet,
            };
        }
    }
}