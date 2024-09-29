using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace UpdateDSP.Views
{
    /// <summary>
    /// About.xaml 的交互逻辑
    /// </summary>
    public partial class About : UserControl
    {
        public About()
        {
            InitializeComponent();
            // 获取当前执行程序集的引用  
            Assembly currentAssembly = Assembly.GetExecutingAssembly();

            // 获取版本信息  
            Version version2 = currentAssembly.GetName().Version;
            mianver.Text = "V" + version2.ToString();
            version.Text = "V" + Common.Common.GetPackageVersion("LL2024.Algorithms.UpdateDSP"); ;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            //Hyperlink link = sender as Hyperlink;
            //string url = link.NavigateUri.AbsoluteUri;
            //Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
    }
}
