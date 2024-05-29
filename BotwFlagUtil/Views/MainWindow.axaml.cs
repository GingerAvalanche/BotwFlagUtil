using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using BotwFlagUtil.ViewModels;

namespace BotwFlagUtil.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent(true);

        DataContext = new MainWindowViewModel();

        Exit.Click += Exit_Click;
        Export.Click += Export_Click;
        Import.Click += Import_Click;
        Save.Click += Save_Click;
        //Settings.Click += Settings_Click;
    }

    private void Exit_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if ((DataContext as MainWindowViewModel)!.NeedsSave)
        {
            //MessageBoxResult result = MessageBox;
        }
        if (Avalonia.Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.Shutdown();
        }
    }

    private void Export_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as MainWindowViewModel)!.Export();
    }

    private void Import_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as MainWindowViewModel)!.Import();
    }

    private void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (DataContext as MainWindowViewModel)!.Save();
    }
}