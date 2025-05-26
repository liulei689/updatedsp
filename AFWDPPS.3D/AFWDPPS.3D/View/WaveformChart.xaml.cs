using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Axis = LiveCharts.Wpf.Axis;
using Separator = LiveCharts.Wpf.Separator;
using SeriesCollection = LiveCharts.SeriesCollection;

namespace WpfApp3D
{
    /// <summary>
    /// ComboBox.xaml 的交互逻辑
    /// </summary>
    public partial class WaveformChart
    {
        private DispatcherTimer uiSyncTimer;
        // 使用字典存储波形和其对应的复选框状态
        private Dictionary<int, LineSeries> seriesDictionary = new Dictionary<int, LineSeries>();
        public WaveformChart()
        {
            InitializeComponent();
            WindowState = WindowState.Normal;
            InitializeChart();
            Loaded += WaveformChart_Loaded;
        }

        private void WaveformChart_Loaded(object sender, RoutedEventArgs e)
        {
            // 动态设置 CheckBox 的 Content 和 Foreground
            foreach (LineSeries series in cartesianChart.Series)
            {
                string checkBoxName = series.Tag.ToString();
                CheckBox checkBox = FindName(checkBoxName) as CheckBox;
                if (checkBox != null)
                {
                    checkBox.Content = series.Title;
                    checkBox.Foreground = series.Stroke;
                }
            }
        }

        private int Xcont = 20;
        private void InitializeLines()
        {
            cartesianChart.Series = new SeriesCollection
    {
        // 横滚角度系列
        new LineSeries
        {
            Title = "船体横滚角度",
            Values = new ChartValues<double>(),
            PointGeometry = DefaultGeometries.Circle,
            PointGeometrySize = 7,
            Fill = Brushes.Transparent,
            Stroke = Brushes.DeepSkyBlue, // 保留深天蓝色
            StrokeThickness = 2,
            Tag = "l1"
        },
        new LineSeries
        {
            Title = "声呐横滚角度",
            Values = new ChartValues<double>(),
            PointGeometry = DefaultGeometries.Square,
            PointGeometrySize = 7,
            Fill = Brushes.Transparent,
            Stroke = Brushes.HotPink, // 保留热粉红色
            StrokeThickness = 2,
                        Tag = "l2"

        },
        new LineSeries
        {
            Title = "电机横滚动作角度",
            Values = new ChartValues<double>(),
            PointGeometry = DefaultGeometries.Diamond, // 改为菱形标记
            PointGeometrySize = 7,
            Fill = Brushes.Transparent,
            Stroke = Brushes.ForestGreen, // 更改颜色为森林绿
            StrokeThickness = 2,
                        Tag = "l3"

        },
        
        // 俯仰角度系列
        new LineSeries
        {
            Title = "船体俯仰角度",
            Values = new ChartValues<double>(),
            PointGeometry = DefaultGeometries.Triangle, // 改为三角形标记
            PointGeometrySize = 7,
            Fill = Brushes.Transparent,
            Stroke = Brushes.Red, // 保留红色
            StrokeThickness = 2,
                        Tag = "l4"

        },
        new LineSeries
        {
            Title = "声呐俯仰角度",
            Values = new ChartValues<double>(),
            PointGeometry = DefaultGeometries.Square,
            PointGeometrySize = 7,
            Fill = Brushes.Transparent,
            Stroke = Brushes.Orange, // 更改颜色为橙色
            StrokeThickness = 2,
                        Tag = "l5"

        },
        new LineSeries
        {
            Title = "电机俯仰动作角度",
            Values = new ChartValues<double>(),
            PointGeometry = DefaultGeometries.Diamond, // 改为菱形标记
            PointGeometrySize = 7,
            Fill = Brushes.Transparent,
            Stroke = Brushes.DarkViolet, // 更改颜色为深紫罗兰色
            StrokeThickness = 2,
                        Tag = "l6"

        }
    };
        }

        private void InitializeChart()
        {
            InitializeLines();
            lineSeries1 = cartesianChart.Series[0] as LineSeries;
            lineSeries2 = cartesianChart.Series[1] as LineSeries;
            lineSeries3 = cartesianChart.Series[2] as LineSeries;
            lineSeries4 = cartesianChart.Series[3] as LineSeries;
            lineSeries5 = cartesianChart.Series[4] as LineSeries;
            lineSeries6 = cartesianChart.Series[5] as LineSeries;
            // 设置图例位置为左上角
            cartesianChart.LegendLocation = LegendLocation.Top;
            // 设置x轴和y轴的范围（可选，根据需要调整）  
            cartesianChart.AxisX.Add(new Axis
            {
                Separator = new Separator { IsEnabled = true }, //网格线
                DisableAnimations = true,
                MinValue = 0, // 设置x轴最小值  
                MaxValue = Xcont, // 设置x轴最大值（假设我们想要显示100个数据点）  
                LabelFormatter = x => x.ToString("N0"), // 格式化x轴标签  
            });
            cartesianChart.AxisY.Add(new Axis
            {
                Separator = new Separator { IsEnabled = false },//网格线
                DisableAnimations = true,
                MinValue = -20, // 设置y轴最小值  
                MaxValue = 20, // 设置y轴最大值（假设波形在-5到5之间波动）  
                LabelFormatter = value => value.ToString("F2") // 格式化y轴标签  
            });
        }

        private Random random = new Random();
        private SeriesCollection seriesCollection;
        private LineSeries lineSeries1;
        private LineSeries lineSeries2;
        private LineSeries lineSeries3;
        private LineSeries lineSeries4;
        private LineSeries lineSeries5;
        private LineSeries lineSeries6;
        public void OnUITimerTick(double x1, double x2, double x3, double x4, double x5, double x6)
        {
            Dispatcher.Invoke(() =>
            {
                AddPointsToChart(lineSeries1, x1);
                AddPointsToChart(lineSeries2, x2);
                AddPointsToChart(lineSeries3, x3);
                AddPointsToChart(lineSeries4, x4);
                AddPointsToChart(lineSeries5, x5);
                AddPointsToChart(lineSeries6, x6);
                cartesianChart.InvalidateVisual();
            });
        }

        private void AddPointsToChart(LineSeries lineSeries, double x)
        {
            if (st.Content.ToString() == "暂停")
            {
                // 生成新的数据点并添加到系列中
                lineSeries.Values.Add((double)x);
                // 移除旧的数据点以保持图表中数据点的数量稳定  
                if (lineSeries.Values.Count > Xcont)
                {
                    lineSeries.Values.RemoveAt(0);
                    cartesianChart.AxisX[0].MaxValue = lineSeries.Values.Count; // 更新x轴最大值以匹配数据点数量  
                    cartesianChart.AxisX[0].MinValue = lineSeries.Values.Count - (Xcont - 1); // 更新x轴最小值以匹配数据点数量（保持100个数据点的窗口）  
                }
            }
        }

        private void Wave_Checked(object sender, RoutedEventArgs e)
        {
            if (cartesianChart == null) return;
            if (sender is CheckBox checkBox)
            {
                string name = checkBox.Name;
                switch (name)
                {
                    case "l1":
                        if (!cartesianChart.Series.Contains(lineSeries1))
                        {
                            cartesianChart.Series.Add(lineSeries1);
                        }
                        break;
                    case "l2":
                        if (!cartesianChart.Series.Contains(lineSeries2))
                        {
                            cartesianChart.Series.Add(lineSeries2);
                        }
                        break;
                    case "l3":
                        if (!cartesianChart.Series.Contains(lineSeries3))
                        {
                            cartesianChart.Series.Add(lineSeries3);
                        }
                        break;
                    case "l4":
                        if (!cartesianChart.Series.Contains(lineSeries4))
                        {
                            cartesianChart.Series.Add(lineSeries4);
                        }
                        break;
                    case "l5":
                        if (!cartesianChart.Series.Contains(lineSeries5))
                        {
                            cartesianChart.Series.Add(lineSeries5);
                        }
                        break;
                    case "l6":
                        if (!cartesianChart.Series.Contains(lineSeries6))
                        {
                            cartesianChart.Series.Add(lineSeries6);
                        }
                        break;
                }
            }
        }

        private void Wave_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                foreach (var series in cartesianChart.Series)
                {
                    if (series is LineSeries lineSeries && lineSeries.Tag?.ToString() == checkBox.Name.ToString())
                    {
                        if (cartesianChart.Series.Contains(lineSeries))
                        {
                            cartesianChart.Series.Remove(lineSeries);
                        }
                        break;
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null && btn.Content != null && btn.Content.ToString() == "只显示横滚")
            {
                l1.IsChecked = true;
                l2.IsChecked = true;
                l3.IsChecked = true;
                l4.IsChecked = false;
                l5.IsChecked = false;
                l6.IsChecked = false;
            }
            else if (btn != null && btn.Content != null && btn.Content.ToString() == "只显示俯仰")
            {
                l1.IsChecked = false;
                l2.IsChecked = false;
                l3.IsChecked = false;
                l4.IsChecked = true;
                l5.IsChecked = true;
                l6.IsChecked = true;
            }
            else if (btn != null && btn.Content != null && btn.Content.ToString() == "全部显示")
            {
                l1.IsChecked = true;
                l2.IsChecked = true;
                l3.IsChecked = true;
                l4.IsChecked = true;
                l5.IsChecked = true;
                l6.IsChecked = true;
            }
            else if (btn != null && btn.Content != null && btn.Content.ToString() == "全部隐藏")
            {
                l1.IsChecked = false;
                l2.IsChecked = false;
                l3.IsChecked = false;
                l4.IsChecked = false;
                l5.IsChecked = false;
                l6.IsChecked = false;
            }

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cartesianChart == null) return;
            if (comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var res = comboBox.SelectedItem as ComboBoxItem;
                int.TryParse(res.Content.ToString(), out Xcont);
                cartesianChart.AxisX[0].MaxValue = Xcont;

            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null && btn.Content != null)
            {
                if (btn.Content.ToString() == "暂停")
                {
                    st.Content = "开始";
                }
                else if (btn.Content.ToString() == "开始")
                {
                    st.Content = "暂停";
                }
            }
        }
        public string Serid = "";
        private string serid = "";
        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Serid = GenerateSerial(serid);
            serid = Serid;
            // 模拟一个长时间运行的任务
            for (int i = 0; i <= 100; i++)
            {
                await Task.Delay(100);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    progressBar.Value = i;
                    progressText.Text = $"{i}%";
                });
            }
            Serid = "";
            //Program.Main1();
            // WpfApp3Ds.Program.Main();
        }

        #region
        #region 序列号
        public static string GenerateSerial(string previousSerial = null)
        {
            // 获取当前日期
            DateTime currentDate = DateTime.Now;
            string currentDateString = currentDate.ToString("yyyyMMdd");

            if (string.IsNullOrEmpty(previousSerial))
            {
                // 如果没有提供前一个流水号，则返回当天的第一个序号
                return $"{currentDateString}001";
            }

            // 解析前一个流水号
            if (!TryParseSerial(previousSerial, out DateTime serialDate, out int serialNumber))
            {
                throw new ArgumentException("无效的流水号格式", nameof(previousSerial));
            }

            // 检查是否是同一天
            if (serialDate.Date == currentDate.Date)
            {
                // 同一天则序号加1
                serialNumber++;
                if (serialNumber > 999)
                {
                    throw new InvalidOperationException("序号超出最大值999");
                }
                return $"{serialDate:yyyyMMdd}{serialNumber:D3}";
            }
            else
            {
                // 不是同一天，则返回当天的第一个序号
                return $"{currentDateString}001";
            }
        }
        private static bool TryParseSerial(string serial, out DateTime date, out int number)
        {
            date = default;
            number = default;

            // 确保长度正确
            if (serial.Length != 11)
            {
                return false;
            }

            // 提取日期和序号部分
            string dateString = serial.Substring(0, 8);
            string numberString = serial.Substring(8, 3);

            // 尝试解析日期
            if (!DateTime.TryParseExact(dateString, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out date))
            {
                return false;
            }

            // 尝试解析序号
            if (!int.TryParse(numberString, out number) || number < 1 || number > 999)
            {
                return false;
            }

            return true;
        }
        #endregion
        //public class ChineseFontResolver : IFontResolver
        //{
        //    public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic)
        //    {
        //        string fontPath = @"C:\Windows\Fonts\simhei.ttf";
        //        return new FontResolverInfo(fontPath);
        //    }

        //    public byte[] GetFont(string faceName)
        //    {
        //        return File.ReadAllBytes(@"C:\Windows\Fonts\simhei.ttf");
        //    }
        //}

        //public class Program
        //{
        //    public static void Main1()
        //    {
        //        GlobalFontSettings.FontResolver = new ChineseFontResolver();

        //        PdfDocument document = new PdfDocument();
        //        document.Info.Title = "安防稳定平台数据分析报告";

        //        PdfPage page = document.AddPage();
        //        page.Orientation = PageOrientation.Portrait;
        //        page.Size = PageSize.A4;

        //        XGraphics gfx = XGraphics.FromPdfPage(page);

        //        XFont titleFont = new XFont("SimHei", 24);
        //        XSolidBrush titleBrush = new XSolidBrush(XColor.FromKnownColor(XKnownColor.Black));

        //        // 绘制标题
        //        gfx.DrawString("安防稳定平台数据分析报告", titleFont, titleBrush,
        //            new XRect(0, -300, page.Width.Point, page.Height.Point - 50), XStringFormats.Center);

        //        XFont tableFont = new XFont("SimHei", 12);
        //        XSolidBrush tableBrush = new XSolidBrush(XColor.FromKnownColor(XKnownColor.Black));

        //        string[,] tableData = new string[12, 3]
        //        {
        //    { "报告生成日期", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"),  DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")},
        //    { "报告流水号", GenerateRandomSerialNumber(), "RH20250523001" },
        //    { "船体平均滚转偏移角度", "0.51°", "0.32°" },
        //    { "船体平均俯仰偏移角度", "0.83°", "0.21°"},
        //    { "声呐平均滚转偏移角度", "0.42°", "0.13°"},
        //    { "声呐平均俯仰偏移角度", "0.31°", "0.63°" },
        //    { "船体最大滚转偏移角度", "2.1°", "1.21°"},
        //    { "船体最大滚转偏移角度", "1.7°", "1.11°"},
        //    { "声呐最大俯仰偏移角度", "2.3°", "1.27°"},
        //    { "声呐最大俯仰偏移角度", "1.3°", "0.21°"},
        //     { "声呐滚转精度", "1.3°", "1.27°"},
        //    { "声呐俯仰精度", "1.3°", "1.21°"}
        //        };

        //        double[] columnWidths = { 150, 150, 150 };

        //        double tableX = 40;
        //        double tableY = 120;
        //        double tableRowHeight = 25;

        //        // 绘制表头
        //        for (int col = 0; col < 3; col++)
        //        {
        //            double cellWidth = columnWidths[col];
        //            double cellHeight = tableRowHeight;

        //            XRect cellRect = new XRect(tableX + col * cellWidth, tableY, cellWidth, cellHeight);
        //            gfx.DrawRectangle(XPens.Black, cellRect);

        //            string headerText = col == 0 ? "检测项" :
        //                      col == 1 ? "近1分钟" :
        //                      col == 2 ? "近5分钟" :
        //                      col == 3 ? "指标" :
        //                      string.Empty; // 默认情况处理
        //            gfx.DrawString(headerText, tableFont, tableBrush, cellRect, XStringFormats.Center);
        //        }

        //        // 绘制表格内容
        //        for (int row = 0; row < tableData.GetLength(0); row++)
        //        {
        //            tableY += tableRowHeight;

        //            for (int col = 0; col < tableData.GetLength(1); col++)
        //            {
        //                double cellWidth = columnWidths[col];
        //                double cellHeight = tableRowHeight;

        //                XRect cellRect = new XRect(tableX + col * cellWidth, tableY, cellWidth, cellHeight);
        //                gfx.DrawRectangle(XPens.Black, cellRect);

        //                string cellText = tableData[row, col];
        //                gfx.DrawString(cellText, tableFont, tableBrush, cellRect, XStringFormats.Center);
        //            }
        //        }

        //        // 添加多个水印
        //        AddWatermark(gfx, page);
        //        // 生成包含时间戳的文件名
        //        string filename = $"安防稳定平台数据分析报告{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.pdf"; // 你可以根据需求
        //        document.Save(filename);
        //        // 使用默认浏览器打开文件
        //        Process.Start(new ProcessStartInfo(filename) { UseShellExecute = true });
        //    }

        //    static void AddWatermark(XGraphics gfx, PdfPage page)
        //    {
        //        XFont watermarkFont = new XFont("SimHei", 36);
        //        XSolidBrush watermarkBrush = new XSolidBrush(XColor.FromArgb(20, 0, 0, 255)); // 设置水印颜色为更浅的灰色

        //        string watermarkText = "安防稳定平台";
        //        double textWidth = gfx.MeasureString(watermarkText, watermarkFont).Width;
        //        double textHeight = gfx.MeasureString(watermarkText, watermarkFont).Height;

        //        double angle = -45; // 设置水印旋转角度

        //        gfx.Save();
        //        gfx.RotateTransform(angle); // 先旋转画布，使文本沿对角线排列

        //        double startX = -page.Width.Point / 2; // 从页面中心偏移开始绘制，确保覆盖整个页面
        //        double startY = -page.Height.Point / 2;

        //        double stepX = textWidth * 1.5; // 增大水平步长，避免水印重叠
        //        double stepY = textHeight * 1.5; // 增大垂直步长，避免水印重叠

        //        int rows = (int)((page.Height.Point * 1.5) / stepY) + 2;
        //        int cols = (int)((page.Width.Point * 1.5) / stepX) + 2;

        //        for (int i = 0; i < rows; i++)
        //        {
        //            for (int j = 0; j < cols; j++)
        //            {
        //                double x = startX + j * stepX;
        //                double y = startY + i * stepY;

        //                // 确保水印文本在页面范围内
        //                if (x + textWidth / Math.Sqrt(2) < page.Width.Point && y + textHeight / Math.Sqrt(2) < page.Height.Point)
        //                {
        //                    gfx.DrawString(watermarkText, watermarkFont, watermarkBrush,
        //                        new XRect(x, y, textWidth, textHeight), XStringFormats.Center);
        //                }
        //            }
        //        }

        //        gfx.Restore();
        //    }

        //    static string GenerateRandomSerialNumber()
        //    {
        //        Random random = new Random();
        //        string serialNumber = "RH" + DateTime.Now.ToString("yyyyMMdd") + random.Next(1000, 9999).ToString("D4");
        //        return serialNumber;
        //    }
        //}
        #endregion
    }

}





