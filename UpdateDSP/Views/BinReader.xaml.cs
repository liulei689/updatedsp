using Rubyer;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace UpdateDSP.Views
{
    /// <summary>
    /// MenuBar.xaml 的交互逻辑
    /// </summary>
    public partial class BinReader : RubyerWindow
    {
        byte[] _bytes;
        int len;
        public BinReader(byte[] bytes, int BinFileLen)
        {
            _bytes = bytes;
            len = BinFileLen;
            InitializeComponent();

            DataContext = this;
            Loaded += BinReader_Loaded;
        }

        private void BinReader_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TestText.Text = ByteArrayToHexWithAlignedLines(_bytes, 39);
                });
            });

        }

        public string ByteArrayToHexWithAlignedLines(byte[] bytes, int bytesPerLine)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 3 - 1); // 每个字节2个十六进制字符加1个空格（除了行尾），但最后一行不需要额外的空格  
            int address = 0;
            hex.Append(address++.ToString("D8"));
            for (int i = 0; i < len; i++)
            {
                hex.AppendFormat(" {0:x2}", bytes[i]); // 注意前面的空格  
                // 每行达到限制后换行  
                if ((i + 1) % bytesPerLine == 0)
                {
                    hex.AppendLine();
                    hex.Append(address++.ToString("D8"));
                }
            }

            // 去除最后一行末尾可能多余的空格（实际上在这个逻辑下不是必需的，因为换行是在循环中完成的）  
            // 但如果需要更精确的控制，可以添加一些逻辑来处理特殊情况，比如字节总数不是bytesPerLine的倍数  

            return hex.ToString().ToUpper();
        }
        public static readonly DependencyProperty IsItalicProperty =
            DependencyProperty.Register("IsItalic", typeof(bool), typeof(BinReader), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool IsItalic
        {
            get { return (bool)GetValue(IsItalicProperty); }
            set { SetValue(IsItalicProperty, value); }
        }


        public static readonly DependencyProperty IsUnderlineProperty =
            DependencyProperty.Register("IsUnderline", typeof(bool), typeof(BinReader), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool IsUnderline
        {
            get { return (bool)GetValue(IsUnderlineProperty); }
            set { SetValue(IsUnderlineProperty, value); }
        }


        public static readonly DependencyProperty IsBoldProperty =
            DependencyProperty.Register("IsBold", typeof(bool), typeof(BinReader), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool IsBold
        {
            get { return (bool)GetValue(IsBoldProperty); }
            set { SetValue(IsBoldProperty, value); }
        }


        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var radio = sender as RadioButton;
            switch (radio.Tag.ToString())
            {
                case "left":
                    TestText.HorizontalContentAlignment = HorizontalAlignment.Left;
                    break;
                case "right":
                    TestText.HorizontalContentAlignment = HorizontalAlignment.Right;
                    break;
                case "center":
                    TestText.HorizontalContentAlignment = HorizontalAlignment.Center;
                    break;
            }
        }

        private void ItalicToggle_Checked(object sender, RoutedEventArgs e)
        {
            TestText.FontStyle = FontStyles.Italic;
        }

        private void ItalicToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            TestText.FontStyle = FontStyles.Normal;
        }

        private void UnderlineToggle_Checked(object sender, RoutedEventArgs e)
        {
            TestText.TextDecorations = TextDecorations.Underline;
        }

        private void UnderlineToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            TestText.TextDecorations = null;
        }

        private void BoldToggle_Checked(object sender, RoutedEventArgs e)
        {
            TestText.FontWeight = FontWeights.Bold;
        }

        private void BoldToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            TestText.FontWeight = FontWeights.Normal;
        }
    }
}
