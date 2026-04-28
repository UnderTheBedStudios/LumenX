using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace LumenX.GameProject;

public partial class ProjectBrowserDialog : Window
{
    public ProjectBrowserDialog()
    {
        InitializeComponent();
    }
    
    // Adding this tells the compiler we expect the XAML to provide the logic
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
