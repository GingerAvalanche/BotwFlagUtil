using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BotwFlagUtil.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BotwFlagUtil.Views;

public partial class MainWindow : Window
{
    readonly WindowIcon icon;
    public MainWindow()
    {
        InitializeComponent();

        MainWindowViewModel vm = new();
        DataContext = vm;

        using (var streamReader = AssetLoader.Open(new("avares://BotwFlagUtil/Assets/icon.png")))
        {
            icon = new(streamReader);
        }

        //FlagSelector.DataTemplates.Add(new FlagNameDataTemplate(vm));
        FlagSelector.ItemTemplate = new FlagNameDataTemplate(vm);

        // Filters
        Blue.Click += Filter_Click;
        Green.Click += Filter_Click;
        Yellow.Click += Filter_Click;
        Red.Click += Filter_Click;
        Manual.Click += Filter_Click;
        Automatic.Click += Filter_Click;

        About.Click += About_Click;
        Exit.Click += Exit_Click;
        Export.Click += Export_Click;
        Help.Click += Help_Click;
        Import.Click += Import_Click;
        Open.Click += Open_ClickAsync;
        Save.Click += Save_Click;
        Settings.Click += Settings_Click;

        Confirm.Click += Confirm_Click;
    }

    private async void Confirm_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!((MainWindowViewModel)DataContext!).Confirm())
            {
                await MessageBoxManager.GetMessageBoxStandard(
                    new MessageBoxStandardParams()
                    {
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentTitle = "Can't Confirm",
                        ContentMessage = "Could not confirm this flag. One of the properties does not contain a valid value!",
                        WindowIcon = icon,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    }).ShowWindowDialogAsync(this);
            }
        }
        catch (Exception ex)
        {
            await ErrorDialog(ex);
        }
    }

    private void Filter_Click(object? sender, RoutedEventArgs e)
    {
        MainWindowViewModel vm = (MainWindowViewModel)DataContext!;
        MenuItem item = (MenuItem)sender!;
        switch (item.Name)
        {
            case "Blue":
                vm.FilterBlue = item.IsChecked;
                break;
            case "Green":
                vm.FilterGreen = item.IsChecked;
                break;
            case "Yellow":
                vm.FilterYellow = item.IsChecked;
                break;
            case "Red":
                vm.FilterRed = item.IsChecked;
                break;
            case "Manual":
                vm.FilterMan = item.IsChecked;
                break;
            case "Automatic":
                vm.FilterAuto = item.IsChecked;
                break;
        }
        vm.UpdateFilter();
    }

    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            string message;
            using (var streamReader = new StreamReader(AssetLoader.Open(new("avares://BotwFlagUtil/Assets/about.md"))))
            {
                message = await streamReader.ReadToEndAsync();
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
        catch (Exception ex)
        {
            await ErrorDialog(ex);
        }
    }

    private async void Exit_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (((MainWindowViewModel)DataContext!).NeedsSave &&
                await DiscardConfirmationDialogue() == ButtonResult.No)
            {
                return;
            }
            if (Application.Current!.ApplicationLifetime is
                IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                desktopLifetime.Shutdown();
            }
        }
        catch (Exception ex)
        {
            await ErrorDialog(ex);
        }
    }

    private void Export_Click(object? sender, RoutedEventArgs e)
    {
        ((MainWindowViewModel)DataContext!).Export();
    }

    private void Help_Click(object? sender, RoutedEventArgs e)
    {
        Image img = new()
        {
            Source = new Bitmap($"{AppContext.BaseDirectory}/Assets/help.png")
        };
        Window helpWindow = new()
        {
            Title = "Help",
            Icon = icon,
            Content = img,
            //SizeToContent = SizeToContent.WidthAndHeight,
            Height = 725,
            Width = 1422,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        helpWindow.Show(this);
    }

    private void Import_Click(object? sender, RoutedEventArgs e)
    {
        ((MainWindowViewModel)DataContext!).Import();
    }

    private async void Open_ClickAsync(object? sender, RoutedEventArgs e)
    {
        try
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
        catch (Exception ex)
        {
            await ErrorDialog(ex);
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        MainWindowViewModel vm = (MainWindowViewModel)DataContext!;
        vm.Save();
    }

    private async void Settings_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            await new SettingsWindow().ShowDialog(this);
        }
        catch (Exception ex)
        {
            await ErrorDialog(ex);
        }
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

    private async Task ErrorDialog(Exception ex)
    {
        await MessageBoxManager.GetMessageBoxStandard(
            new()
            {
                ButtonDefinitions = ButtonEnum.Ok,
                ContentTitle = "Error",
                ContentMessage = $"{ex.Message}\n{ex.StackTrace ?? ""}",
                MaxHeight = 800,
                Width = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            }).ShowWindowDialogAsync(this);
    }

    private class FlagNameDataTemplate(MainWindowViewModel vm) : IDataTemplate
    {
        public Control Build(object? param)
        {
            string flagName = (string)param!;
            var control = new TextBlock()
            {
                Background = vm.BgColors[flagName],
                Foreground = vm.FgColors[flagName],
                FontWeight = FontWeight.Bold,
                Padding = new Thickness(12),
                Text = flagName
            };
            return control;
        }

        public bool Match(object? data)
        {
            return data is string flagName && vm.BgColors.ContainsKey(flagName);
        }
    }
}