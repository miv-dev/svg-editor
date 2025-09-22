using Microsoft.UI.Xaml;
using svg_editor.Services;
using svg_editor.Views.Pages;

namespace svg_editor;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        RootGrid.DataContext = App.GetService<AppState>();

        RootFrame.Navigate(typeof(HomePage));

    }
   
}
