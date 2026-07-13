using System.ComponentModel;
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
        Closing += OnMainWindowClosing;
    }

    private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnMainWindowLoaded;
        OpenProjectBrowserDialog();
    }

    private void OnMainWindowClosing(object sender, CancelEventArgs e)
    {
        Closing -= OnMainWindowClosing;
        Project.Current?.Unload();
    }

    private async void OpenProjectBrowserDialog()
    {
        var dialog = new ProjectBrowserDialog();
        if (Application.Current?.ApplicationLifetime is 
            IClassicDesktopStyleApplicationLifetime desktop)
        {
            var projectCreated = await dialog.ShowDialog<bool>(desktop.MainWindow);

            if (!projectCreated || dialog.DataContext == null)
            {
                desktop.Shutdown();
            }
            else
            {
                Project.Current?.Unload();
                DataContext = dialog.DataContext;
                Console.WriteLine($"[DEBUG] MainWindow DataContext: {DataContext}");
            }
        }
}

    // Adding this tells the compiler we expect the XAML to provide the logic
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
