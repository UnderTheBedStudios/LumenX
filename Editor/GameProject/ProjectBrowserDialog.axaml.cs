using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace LumenX.GameProject;

public partial class ProjectBrowserDialog : Window
{
    // Declare named controls as fields
    private ToggleButton openProjectButton => this.FindControl<ToggleButton>("OpenProjectButton");
    private ToggleButton createProjectButton => this.FindControl<ToggleButton>("CreateProjectButton");
    private StackPanel contentPanel => this.FindControl<StackPanel>("ContentPanel");

    public ProjectBrowserDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender == openProjectButton)
        {
            if (createProjectButton.IsChecked == true)
            {
                createProjectButton.IsChecked = false;
                contentPanel.Margin = new Thickness(0);
            }
            openProjectButton.IsChecked = true;
        }
        else if (sender == createProjectButton)
        {
            if (openProjectButton.IsChecked == true)
            {
                openProjectButton.IsChecked = false;
                contentPanel.Margin = new Thickness(-800, 0, 0, 0);
            }
            createProjectButton.IsChecked = true;
        }
    }
}