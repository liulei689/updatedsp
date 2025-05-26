using System.Windows;
using WpfApp3D.ViewModels;

namespace WpfApp3D
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AFWDPPS.PDF.Program.Main();
            this.DataContext = new MainViewModel();


        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
