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

    private void createButton_Click(object? sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as NewProject;
        var projectPath = viewModel.CreateProject(templateList.SelectedItem as ProjectTemplate);
        bool dialogResult = false;
        var win = TopLevel.GetTopLevel(this) as Window;
        if(!string.IsNullOrEmpty(projectPath)) dialogResult = true;

        win.Close(dialogResult);
    }
}