using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace LumenX.GameProject;

public partial class OpenProjectView : UserControl
{
    private Button exitButton => this.FindControl<Button>("ExitButton");
    private ListBox projectListBox => this.FindControl<ListBox>("projectList");

    public OpenProjectView()
    {
        InitializeComponent();
        projectListBox.DoubleTapped += projectList_DoubleTapped;
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

    private void openProjectButton_Click(object? sender, RoutedEventArgs e)
    {
        OpenSelectedProject();
    }

    private void projectList_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        OpenSelectedProject();
    }

    private void OpenSelectedProject()
    {
        Console.WriteLine($"[DEBUG] projectListBox is null: {projectListBox == null}");

        var project = OpenProject.Open(projectListBox.SelectedItem as ProjectData);
        bool dialogResult = false;
        var win = TopLevel.GetTopLevel(this) as Window;

        if (win == null)
        {
            System.Diagnostics.Debug.WriteLine("TopLevel is not a Window");
            return;
        }

        if (project != null)
        {
            dialogResult = true;
            win.DataContext = project;
        }

        win.Close(dialogResult);
    }
}