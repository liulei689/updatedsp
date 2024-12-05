using AFWDPP.Common;
using LL2024.Algorithms.UpdateDSP;
using Rubyer;
using System;
using System.Windows.Controls;

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
                    statues.Content = data2.Length;
                    DSP28335.CalculateChecksum(data2);
                    IDC_EDIT_FC_2.Text = "";
                    for (int i = 0; i < data2.Length; i++)
                    {
                        IDC_EDIT_FC_2.Text += data2[i].ToString("X2") + " ";
                    }
                }
                catch (Exception ex)
                {
                    Message.Error(ex);
                }
            }

        }
    }
}
