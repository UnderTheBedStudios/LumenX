using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace LumenX.GameProject;

public partial class NewProjectView : UserControl
{
    private Button exitButton => this.FindControl<Button>("ExitButton");

    public NewProjectView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        if (sender == exitButton)
        {
            // Close the parent window
            if (Application.Current?.ApplicationLifetime is 
                IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        }
    }
}