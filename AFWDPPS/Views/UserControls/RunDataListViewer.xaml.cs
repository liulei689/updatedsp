using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace 导引头上位机程序.Views.UserControls
{
    /// <summary>
    /// RunDataListViewer.xaml 的交互逻辑
    /// </summary>
    public partial class RunDataListViewer : UserControl
    {
        public RunDataListViewer()
        {
            InitializeComponent();
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            string tempFilePath = System.IO.Path.GetTempFileName();
            StringBuilder allText = new StringBuilder();

            foreach (ListViewItem item in rtbLog.Items)
            {
                // 检查 Content 是否为 TextBlock
                if (item.Content is TextBlock textBlock)
                {
                    // 将 TextBlock 的文本添加到 StringBuilder 中
                    allText.AppendLine(textBlock.Text);
                }
                // 注意：如果 Content 不是 TextBlock，这里将不会处理它。
                // 根据你的具体需求，你可能需要添加额外的逻辑来处理其他类型的 Content。
            }

            // 现在 allText 包含了 ListView 中所有 TextBlock 的文本内容
            string result = allText.ToString();
            for (int i = 0; i < rtbLog.Items.Count; i++)
                System.IO.File.WriteAllText(tempFilePath, result);
            Process.Start("notepad.exe", tempFilePath);
        }
        private bool issxcheck = true;
        private bool istoend = false;
        public void AddOne(string hexString, string otherstring)
        {
            string strs = issxcheck ? "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "]" + otherstring : "";

            //WrapPanel stackPanel = new WrapPanel
            //{
            //    Orientation = Orientation.Horizontal,
            //    Margin = new Thickness(0)
            //};

            //// 创建时间戳TextBlock  
            //TextBlock timestampTextBlock = new TextBlock
            //{
            //    TextWrapping = TextWrapping.Wrap, // 设置文本自动换行  
            //    Text = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "]>>>",
            //    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#24D1DF")),
            //    Margin = new Thickness(0, 0, 10, 0) // 在时间戳和内容之间添加一些间距  

            //};
            //stackPanel.Children.Add(timestampTextBlock);

            // 创建内容TextBlock  
            TextBlock contentTextBlock = new TextBlock
            {
                Text = strs + hexString,
                // Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap // 设置文本自动换行  

            };
            // stackPanel.Children.Add(contentTextBlock);

            // 创建一个ListViewItem并将StackPanel设置为其内容  
            ListViewItem listViewItem = new ListViewItem
            {
                Content = contentTextBlock
            };
            listViewItem.Width = rtbLog.ActualWidth - 5;
            // 将ListViewItem插入到ListView的Items集合中的最前面
            rtbLog.Items.Insert(0, listViewItem);

            // 如果项数超过最大值，移除最下面的一项（现在是最旧的一项）
            if (rtbLog.Items.Count > maxcount)
            {
                rtbLog.Items.RemoveAt(rtbLog.Items.Count - 1);
            }

            // 可选：滚动到顶部显示最新添加的项
            //rtbLog.ScrollIntoView(listViewItem);
        }

        private void MenuItem_Checked(object sender, RoutedEventArgs e)
        {
            issxcheck = true;
        }

        private void MenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            issxcheck = false;
        }

        private int maxcount = 50;
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            rtbLog.Items.Clear();
            maxcount = 50;
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            rtbLog.Items.Clear();
            maxcount = 100;
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            rtbLog.Items.Clear();
            maxcount = 500;
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            rtbLog.Items.Clear();
            maxcount = 1000;
        }

        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            rtbLog.Items.Clear();
        }
    }
}
