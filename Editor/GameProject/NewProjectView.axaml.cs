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
    private ListBox templateListBox => this.FindControl<ListBox>("templateList");

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
        Console.WriteLine($"[DEBUG] DataContext type: {DataContext?.GetType().FullName ?? "NULL"}");
        Console.WriteLine($"[DEBUG] templateListBox is null: {templateListBox == null}");
        var viewModel = DataContext as NewProject;
        if (viewModel == null)
        {
            // This will tell you immediately if this is the problem
            System.Diagnostics.Debug.WriteLine($"DataContext is actually: {DataContext?.GetType().Name ?? "null"}");
            return;
        }

        var projectPath = viewModel.CreateProject(templateListBox.SelectedItem as ProjectTemplate);
        bool dialogResult = false;
        var win = TopLevel.GetTopLevel(this) as Window;

        if (win == null)
        {
            System.Diagnostics.Debug.WriteLine("TopLevel is not a Window");
            return;
        }

        if (!string.IsNullOrEmpty(projectPath))
        {
            dialogResult = true;
            var project = OpenProject.Open(new ProjectData()
            {
                ProjectName = viewModel.ProjectName,
                ProjectPath = viewModel.ProjectPath
            });
            win.DataContext = project;
        }

        win.Close(dialogResult);
    }
}