using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using svg_editor.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace svg_editor.Views.Controls
{
    public sealed partial class Sidebar : UserControl
    {
        private MainViewModel VM => (MainViewModel)DataContext!;

        public Sidebar()
        {
            InitializeComponent();
            DataContext = App.GetService<MainViewModel>();
        }

        public async Task SaveFileAsync(string svgContent)
        {
            // Открываем диалог сохранения
            WindowId windowId = App.Window.AppWindow.Id;

            var picker = new FileSavePicker(windowId)
            {
                SuggestedFileName = "scene",
                DefaultFileExtension = ".svg",
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            picker.FileTypeChoices.Add("SVG Files", new List<string> { ".svg" });


            // Ожидаем выбора файла
            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
               File.WriteAllText(file.Path, svgContent);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string svg = VM.GetSvg();

            _ = SaveFileAsync(svg);
        }

        public async Task OpenFile()
        {
            WindowId windowId = App.Window.AppWindow.Id;


            var picker = new FileOpenPicker(windowId);
            picker.FileTypeFilter.Add(".svg");
            picker.SuggestedStartLocation = PickerLocationId.Desktop;


            var file = await picker.PickSingleFileAsync();
            if (file is null) return;

            string xml = File.ReadAllText(file.Path);

            VM.ImportSvg(xml);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            
            _ = OpenFile();
        }
    }
}
