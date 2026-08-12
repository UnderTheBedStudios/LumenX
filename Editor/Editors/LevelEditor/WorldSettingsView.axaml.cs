using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

using LumenX.GameProject;
using Avalonia.Input;
namespace LumenX.Editors;

partial class WorldSettingsView : UserControl
{
    

    public WorldSettingsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}