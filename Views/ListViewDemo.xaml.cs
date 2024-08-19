using Microsoft.Extensions.DependencyInjection;
using UpdateDSP.ViewModels;
using System.Windows.Controls;

namespace UpdateDSP.Views
{
    /// <summary>
    /// ListViewDemo.xaml 的交互逻辑
    /// </summary>
    public partial class ListViewDemo : UserControl
    {
        public ListViewDemo()
        {
            InitializeComponent();

            this.DataContext = App.Current.Services.GetService<ListViewModel>();
        }
    }
}
