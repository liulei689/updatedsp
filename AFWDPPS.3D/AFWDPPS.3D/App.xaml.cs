using AFWDPPS.DB;
using System;
using System.Windows;
using WpfApp3D.Models;

namespace WpfApp3D
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 注册全局异常处理事件
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                base.OnStartup(e);

                // 初始化组件 - 这些地方容易抛出依赖缺失异常
                AsyncLogger.Initialize(); // 初始化异步日志记录器
                FistDbManager.Run();     // 数据库初始化

                // 其他启动逻辑
            }
            catch (Exception ex)
            {
                // 处理启动过程中的异常
                HandleException("应用程序启动失败", ex.InnerException);

                HandleException("应用程序启动失败", ex);

            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // 处理启动过程中的异常
            HandleException("UI线程内部异常", e.Exception.InnerException);

            HandleException("UI线程外部异常", e.Exception);
            e.Handled = true; // 阻止应用程序崩溃
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException("非UI线程未处理异常", e.ExceptionObject as Exception);
        }

        private void HandleException(string context, Exception ex)
        {
            // 记录异常日志
            string errorMessage = $"{context}: {ex.Message}\n\n堆栈跟踪:\n{ex.StackTrace}";

            // 显示错误对话框
            MessageBox.Show(errorMessage, "应用程序错误", MessageBoxButton.OK, MessageBoxImage.Error);

            // 可以选择记录到文件或发送到服务器
        }
    }

}
