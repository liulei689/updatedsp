using AFWDPP.Common;
using LL2024.Algorithms.UpdateDSP;
using Rubyer;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace 导引头上位机程序.Views
{
    public partial class Tools : RubyerWindow
    {
        public Tools()
        {
            InitializeComponent();

        }

        private void IDC_EDIT_FC_1_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var data = sender as TextBox;
            if (data != null)
            {
                try
                {
                    if (data.Text.Length < 8) return;
                    var data2 = data.Text.HexStringToByteArray();
                    IDC_EDIT_CHECKB_1.Content = data2.Length + "(0x" + data2.Length.ToString("X2") + ")"; ;
                    IDC_EDIT_CHECKB_2.Content = data2[4] + "(0x" + data2[4].ToString("X2") + ")"; ;
                    DSP28335.CalculateChecksum(data2);
                    IDC_EDIT_FC_2.Text = "";
                    for (int i = 0; i < data2.Length; i++)
                    {
                        IDC_EDIT_FC_2.Text += data2[i].ToString("X2") + " ";
                    }
                    IDC_EDIT_CHECKB_3.Content = data2[data2.Length - 2] + "(0x" + data2[data2.Length - 2].ToString("X2") + ")";
                    if (IDC_EDIT_FC_1.Text.Trim() == IDC_EDIT_FC_2.Text.Trim())
                    {
                        IDC_EDIT_CHECKB_5.Content = "正确帧";
                        IDC_EDIT_CHECKB_5.Foreground = new SolidColorBrush(Colors.Blue);

                    }
                    else
                    {
                        IDC_EDIT_CHECKB_5.Content = "错误帧";
                        IDC_EDIT_CHECKB_5.Foreground = new SolidColorBrush(Colors.Red);
                    }
                }
                catch (Exception ex)
                {
                    IDC_EDIT_CHECKB_5.Content = ex;
                    IDC_EDIT_CHECKB_5.Foreground = new SolidColorBrush(Colors.Red);
                    //  Message.Error(ex);
                }
            }

        }
    }
}
