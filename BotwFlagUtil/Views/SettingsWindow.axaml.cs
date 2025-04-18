using Avalonia.Controls;
using Avalonia.Interactivity;
using BotwFlagUtil.ViewModels;
using System;
using System.Threading.Tasks;
using BotwFlagUtil.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace BotwFlagUtil.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            DataContext = new SettingsViewModel();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            InitializeComponent();

            GameDir.TextChanged += DumpBox_TextChanged;
            UpdateDir.TextChanged += DumpBox_TextChanged;
            DlcDir.TextChanged += DumpBox_TextChanged;
            GameDirNx.TextChanged += DumpBox_TextChanged;
            DlcDirNx.TextChanged += DumpBox_TextChanged;
            GameBrowse.Click += BrowseButton_Click;
            UpdateBrowse.Click += BrowseButton_Click;
            DlcBrowse.Click += BrowseButton_Click;
            GameNxBrowse.Click += BrowseButton_Click;
            DlcNxBrowse.Click += BrowseButton_Click;
            SaveButton.Click += SaveButton_Click;
            CancelButton.Click += CancelButton_Click;

            Width = 600;
            Height = 250;
        }

        private void DumpBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (e.Source is not TextBox box) return;
            bool valid;
            switch (box.Name)
            {
                case "GameDir":
                    valid = Settings.ValidateGameDir(box.Text!);
                    GameDirValid.IsVisible = valid;
                    GameDirInvalid.IsVisible = !valid;
                    break;
                case "UpdateDir":
                    valid = Settings.ValidateUpdateDir(box.Text!);
                    UpdateDirValid.IsVisible = valid;
                    UpdateDirInvalid.IsVisible = !valid;
                    break;
                case "DlcDir":
                    valid = Settings.ValidateDlcDir(box.Text!);
                    DlcDirValid.IsVisible = valid;
                    DlcDirInvalid.IsVisible = !valid;
                    break;
                case "GameDirNx":
                    valid = Settings.ValidateGameDirNx(box.Text!);
                    GameDirNxValid.IsVisible = valid;
                    GameDirNxInvalid.IsVisible = !valid;
                    break;
                case "DlcDirNx":
                    valid = Settings.ValidateDlcDirNx(box.Text!);
                    DlcDirNxValid.IsVisible = valid;
                    DlcDirNxInvalid.IsVisible = !valid;
                    break;
            }
        }

        private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var selection = await StorageProvider.OpenFolderPickerAsync(
                    new()
                    {
                        Title = "Select the root folder of your dump",
                        AllowMultiple = false,
                    }
                );
                if (selection.Count != 1)
                {
                    return;
                }
                string path = Uri.UnescapeDataString(selection[0].Path.AbsolutePath);
                if (sender is Button button)
                {
                    switch (button.Name)
                    {
                        case "GameBrowse":
                            GameDir.Text = path;
                            break;
                        case "UpdateBrowse":
                            UpdateDir.Text = path;
                            break;
                        case "DlcBrowse":
                            DlcDir.Text = path;
                            break;
                        case "GameNxBrowse":
                            GameDirNx.Text = path;
                            break;
                        case "DlcNxBrowse":
                            DlcDirNx.Text = path;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                await ErrorDialog(ex);
            }
        }

        private void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            ((SettingsViewModel)DataContext!).SaveSettings();
            Close();
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
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
    }
}
