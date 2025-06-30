using AFWDPPS.DB;
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
    public partial class WaveformChartHis
    {
        private DispatcherTimer uiSyncTimer;
        // 使用字典存储波形和其对应的复选框状态
        private Dictionary<int, LineSeries> seriesDictionary = new Dictionary<int, LineSeries>();
        public WaveformChartHis()
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

        /// <summary>
        /// 到底部初始化图表
        /// </summary>
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
                if (cartesianChart.Series.Contains(lineSeries1))
                    AddPointsToChart(lineSeries1, x1);
                if (cartesianChart.Series.Contains(lineSeries2))
                    AddPointsToChart(lineSeries2, x2);
                if (cartesianChart.Series.Contains(lineSeries3))
                    AddPointsToChart(lineSeries3, x3);
                if (cartesianChart.Series.Contains(lineSeries4))
                    AddPointsToChart(lineSeries4, x4);
                if (cartesianChart.Series.Contains(lineSeries5))
                    AddPointsToChart(lineSeries5, x5);
                if (cartesianChart.Series.Contains(lineSeries6))
                    AddPointsToChart(lineSeries6, x6);
                cartesianChart.InvalidateVisual();
            });
        }

        /// <summary>
        /// 向图表中添加数据点
        /// </summary>
        /// <param name="lineSeries"></param>
        /// <param name="x"></param>
        private void AddPointsToChart(LineSeries lineSeries, double x)
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
            AFWDPPS.PDF.Program.Main();
        }

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

        private async void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private async void DatePicker_SelectedDateChanged_1(object sender, SelectionChangedEventArgs e)
        {

        }

        private async void starttime_SelectedTimeChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            if (starttime.SelectedDateTime.HasValue && endtime.SelectedDateTime.HasValue)
            {
                var res = await WDPT.GetListByTime(starttime.SelectedDateTime.Value, endtime.SelectedDateTime.Value);
                res.ForEach(item =>
                {
                    OnUITimerTick(item.船横滚角度, item.声呐横滚角度, item.横滚电机动作角度, item.船俯仰角度, item.声呐俯仰角度, item.俯仰电机动作角度);
                });
            }
        }

        private async void endtime_SelectedTimeChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            if (starttime.SelectedDateTime.HasValue && endtime.SelectedDateTime.HasValue)
            {
                var res = await WDPT.GetListByTime(starttime.SelectedDateTime.Value, endtime.SelectedDateTime.Value);
                res.ForEach(item =>
                {
                    OnUITimerTick(item.船横滚角度, item.声呐横滚角度, item.横滚电机动作角度, item.船俯仰角度, item.声呐俯仰角度, item.俯仰电机动作角度);
                });
            }
        }
    }

}





