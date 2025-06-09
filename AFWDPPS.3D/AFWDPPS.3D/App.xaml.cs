using System.Windows;
using WpfApp3D.Models;

namespace WpfApp3D
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        //
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AsyncLogger.Initialize(); // 初始化异步日志记录器

            // 在这里可以添加其他启动逻辑

            // 其他启动逻辑
        }
    }
}
