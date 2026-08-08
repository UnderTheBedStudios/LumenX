using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LumenX.Editors;

partial class ProjectLayoutView : UserControl
{
    public ProjectLayoutView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}