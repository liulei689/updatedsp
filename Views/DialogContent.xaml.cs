using Rubyer;
using Rubyer.Commons;
using UpdateDSP.Consts;
using UpdateDSP.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UpdateDSP.Views
{
    /// <summary>
    /// DialogContent.xaml 的交互逻辑
    /// </summary>
    public partial class DialogContent : UserControl
    {
        public DialogContent()
        {
            InitializeComponent();
            DataContext = new DialogContentViewModel();
        }
    }
}
