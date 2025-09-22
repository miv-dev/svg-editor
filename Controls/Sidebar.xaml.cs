using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using svg_editor.Models;
using svg_editor.Services;
using svg_editor.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace svg_editor.Controls;

public sealed partial class Sidebar : UserControl
{
    private readonly SvgStore _store;

    private readonly MainViewModel _vm;
    public Sidebar()
    {
        InitializeComponent();

        _store = App.GetService<SvgStore>();

        _vm = new MainViewModel(_store);

        DataContext = _vm;
    }

   

    private void ShapesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var shape = ShapesList.SelectedItem as Shape;
        _vm.Store.SetSelected(shape?.Id);
    }
}
