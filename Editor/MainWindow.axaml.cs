using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LumenX.GameProject;

namespace LumenX;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnMainWindowLoaded;
    }

    private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnMainWindowLoaded;
        OpenProjectBrowserDialog();
    }

    private async void OpenProjectBrowserDialog()
    {
        var dialog = new ProjectBrowserDialog();
        if (Application.Current?.ApplicationLifetime is 
            IClassicDesktopStyleApplicationLifetime desktop)
        {
            await dialog.ShowDialog(desktop.MainWindow);

            desktop.Shutdown();
        }
    }

    // Adding this tells the compiler we expect the XAML to provide the logic
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
