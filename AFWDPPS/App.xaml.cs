using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using AFWDPP.ViewModels;

namespace AFWDPP
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Services = ConfigureServices();
            ShowSplashScreen();
        }

        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);



            new MainWindow().Show();
        }

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<MainViewModel>();
            return services.BuildServiceProvider();
        }
        void ShowSplashScreen()
        {
            var splashScreen = new SplashScreen("../logo.ico");
            splashScreen.Show(true);
        }
    }
}