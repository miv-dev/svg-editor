using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using svg_editor.Services;
using svg_editor.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace svg_editor
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {

        public static IHost Host { get; } = new HostBuilder().ConfigureServices(services =>
        {
            services.AddSingleton<AppState>();
            services.AddSingleton<SvgStore>();
            services.AddSingleton<CommandManager>();
            services.AddSingleton<MainViewModel>(sp => new MainViewModel(
                sp.GetRequiredService<SvgStore>(),
                sp.GetRequiredService<AppState>(),
                sp.GetRequiredService<CommandManager>()
            ));
        }).Build();


        public static T GetService<T>() where T : class
        => Host.Services.GetRequiredService<T>();


        public static Window? Window { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var state = GetService<AppState>();

            Window = new MainWindow();
            Window.Activate();
        }
    }
}
