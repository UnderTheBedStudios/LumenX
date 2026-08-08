using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LumenX.Editors;

partial class LevelEditorView : UserControl
{
    public LevelEditorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}