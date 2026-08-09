using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

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
        AddHandler(PointerPressedEvent, (s, e) =>
        {
            if (VisualRoot is Window w)
                w.Title = $"HIT: {e.Source}";
        }, RoutingStrategies.Tunnel);
    }
}