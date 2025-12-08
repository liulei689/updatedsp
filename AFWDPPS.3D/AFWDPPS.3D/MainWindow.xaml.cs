using AFWDPPS.DB;
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
            this.DataContext = new MainViewModel();
            this.Closing += (s, e) =>
            {
                FistDbManager.CloseDb();
            };

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
