using YWDEVICE.Common;
using LL2024.Algorithms.UpdateDSP;
using Rubyer;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace 导引头上位机程序.Views
{
    public partial class Tools : RubyerWindow
    {
        public Tools()
        {
            InitializeComponent();
            // 检查剪贴板是否包含文本数据
            if (Clipboard.ContainsText())
            {
                // 从剪贴板获取文本数据
                string clipboardText = Clipboard.GetText();
                IDC_EDIT_FC_1.Text = clipboardText;
            }


        }

        private void SetHexCardNumbers(byte[] data, bool ispass)
        {
            Dispatcher.Invoke(() =>
            {
                // 获取WrapPanel的引用
                WrapPanel wrapPanel = this.HexContent.Children.OfType<WrapPanel>().FirstOrDefault();
                if (wrapPanel != null)
                {
                    // 清除WrapPanel上已有的所有控件
                    wrapPanel.Children.Clear();
                    // 动态生成Badge和Card控件
                    for (int i = 0; i < data.Length; i++) // 从2开始以避免与静态Badge重复
                    {
                        // 创建Card控件
                        Card card = new Card
                        {
                            Width = 30,
                            Height = 30,
                            Background = (Brush)this.FindResource("Primary"), // 使用资源字典中的PrimaryBrush
                            Content = data[i].ToString("x2").ToUpper(), // 自定义内容
                            Foreground = Brushes.White,
                            HorizontalContentAlignment = HorizontalAlignment.Center
                        };
                        if (data.Length - 2 == i)
                        {
                            if (data.Length < 8 || data == null || data.Length != data[4] + 7)
                            {
                                card.Width = 80;
                                card.Content = "无法生成校验";
                                card.Foreground = Brushes.Red;
                            }
                        }

                        // 创建Badge控件
                        Badge badge = new Badge
                        {
                            Margin = new Thickness(10),
                            Text = i.ToString()
                        };

                        // 将Card控件设置为Badge控件的内容
                        badge.Content = card;

                        // 将Badge控件添加到WrapPanel中
                        wrapPanel.Children.Add(badge);
                    }
                }
            });
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
                    var chekc = DSP28335.CheckChecksum(data2);
                    if (chekc)
                    {
                        IDC_EDIT_CHECKB_5.Content = "正确帧";
                        IDC_EDIT_CHECKB_5.Foreground = new SolidColorBrush(Colors.Blue);
                    }
                    else
                    {
                        if (data2.Length < 8 || data2 == null || data2.Length != data2[4] + 7)
                        {
                            IDC_EDIT_CHECKB_5.Content = "祯长度不合法，应为" + (data2[4] + 7);
                            IDC_EDIT_CHECKB_5.Foreground = new SolidColorBrush(Colors.Red);
                        }
                        else
                        {
                            IDC_EDIT_CHECKB_5.Content = "校验不通过";
                            IDC_EDIT_CHECKB_5.Foreground = new SolidColorBrush(Colors.Red);

                        }
                    }
                    IDC_EDIT_CHECKB_1.Content = data2.Length + "(0x" + data2.Length.ToString("X2") + ")"; ;
                    IDC_EDIT_CHECKB_2.Content = data2[4] + "(0x" + data2[4].ToString("X2") + ")";
                    DSP28335.CalculateChecksum(data2);
                    IDC_EDIT_CHECKB_3.Content = data2[data2.Length - 2] + "(0x" + data2[data2.Length - 2].ToString("X2") + ")";

                    Task.Run(() =>
                    {
                        SetHexCardNumbers(data2, chekc);
                    });
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
