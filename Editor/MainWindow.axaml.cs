using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LumenX;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // Adding this tells the compiler we expect the XAML to provide the logic
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
