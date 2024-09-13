using Microsoft.Extensions.DependencyInjection;
using Rubyer;
using Rubyer.Enums;
using System;
using System.Diagnostics;
using System.Windows;
using UpdateDSP.ViewModels;

namespace UpdateDSP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RubyerWindow
    {
        public static MainWindow Instance { get; private set; }
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = App.Current.Services.GetRequiredService<MainViewModel>();

            Loaded += MainWindow_Loaded;
            ThemeManager.ThemeModeChanged += OnThemeModeChanged;
            Instance = this;
            this.StateChanged += MainWindow_StateChanged;
        }
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // 检查窗口是否从最小化状态恢复  
            if (this.WindowState == WindowState.Normal)
            {
                // 这里执行窗口从最小化恢复正常的逻辑  
                this.Topmost = false;
                this.ShowInTaskbar = true;
            }
        }
        public void SetTitle(string title)
        {
            if (Topmost && !ShowInTaskbar && WindowState == WindowState.Minimized)
            {
                Title = title;
            }
        }
        public void XuanFu()
        {
            this.Topmost = true;
            this.ShowInTaskbar = false;
            //图标显示在托盘区
            this.WindowState = WindowState.Minimized;
        }

        private void OnThemeModeChanged(object sender, ThemeModeChangedArgs e)
        {
            //if (e.IsDarkMode)
            //{
            //    this.TitleBackground = (Brush)Application.Current.Resources["Dark"];
            //}
            //else
            //{
            //    this.TitleBackground = (Brush)Application.Current.Resources["Primary"];
            //}
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            controlSlider.Value = ((CornerRadius)App.Current.Resources["AllControlCornerRadius"]).TopLeft;
            contrainerSlider.Value = ((CornerRadius)App.Current.Resources["AllContainerCornerRadius"]).TopLeft;
            ThemeManager.SwitchThemeMode(ThemeMode.System);
            darkMode.IsChecked = ThemeManager.GetIsAppDarkMode();
            int hour = DateTime.Now.Hour;
            if (hour >= 22 || hour < 6)
            {
                darkMode.IsChecked = true;
                ThemeManager.SwitchThemeMode(darkMode.IsChecked ? ThemeMode.Dark : ThemeMode.Light);
                // 晚上22点（含）到凌晨6点（不含）之间
            }
            else
            {
            }
            // 上述之外的时间段
        }

        private void controlSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ThemeManager.SwitchControlCornerRadius(e.NewValue);
        }

        private void contrainerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ThemeManager.SwitchContainerCornerRadius(e.NewValue);
        }

        private void BlackSwitch_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.SwitchThemeMode(darkMode.IsChecked ? ThemeMode.Dark : ThemeMode.Light);
        }

        protected override void OnClosed(EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RestartApplication();
        }
        private void RestartApplication()
        {
            // 获取当前可执行文件的路径  
            string exePath = Process.GetCurrentProcess().MainModule.FileName;

            // 启动新进程  
            ProcessStartInfo startInfo = new ProcessStartInfo(exePath);
            // 如果需要，可以在这里添加启动参数  
            // startInfo.Arguments = "your_arguments_here";  

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                // 处理启动失败的情况，例如文件被锁定  
                Message.Error($"重启失败: {ex.Message}");
                return;
            }

            // 退出当前进程  
            Environment.Exit(0);
        }
    }
}