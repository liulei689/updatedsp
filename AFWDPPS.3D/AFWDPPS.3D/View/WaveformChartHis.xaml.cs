using AFWDPPS.DB;
using ScottPlot;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace WpfApp3D.View
{
    /// <summary>
    /// BoXing.xaml 的交互逻辑
    /// </summary>
    public partial class WaveformChartHis : Window
    {
        private readonly int channelCount = 12;
        private int displayPointCount = 50000;
        private int maxRows = 200000;
        private double[][] datas;
        private DateTime[] times;
        private ScottPlot.Plottables.Signal[] signals;
        private ScottPlot.DataGenerators.RandomWalker[] walkers;
        private readonly string[] channelNames =
        {
            "船体横滚",
            "声呐横滚",
            "电机横滚动作",
            "船体俯仰",
            "声呐俯仰",
            "电机俯仰动作",
            "陀螺横摇角速度",
            "陀螺俯仰角速度",
            "陀螺横摇角速度积分",
            "陀螺俯仰角速度积分",
            "横摇电流",
            "俯仰电流",
        };
        private ScottPlot.Plottables.Crosshair CH;
        private readonly DispatcherTimer _autoScaleTimer =
    new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1000) };   // 2 Hz
        public static BoXing Instance { get; set; }
        private string _dbFilePath;
        private readonly bool _loadLatestOnStartup;

        public WaveformChartHis(bool loadLatestOnStartup = false)
        {
            InitializeComponent();
            _loadLatestOnStartup = loadLatestOnStartup;
            walkers = new ScottPlot.DataGenerators.RandomWalker[channelCount];
            datas = new double[channelCount][];
            times = Array.Empty<DateTime>();
            signals = new ScottPlot.Plottables.Signal[channelCount];

            for (int i = 0; i < channelCount; i++)
                walkers[i] = new ScottPlot.DataGenerators.RandomWalker();
            Loaded += BoXing_Loaded;
            MouseMove += DisplayScaling_MouseMove;
            _autoScaleTimer.Start();
            _autoScaleTimer.Tick += _autoScaleTimer_Tick; ;
            Closed += BoXing_Closed;
        }

        private void BoXing_Closed(object sender, EventArgs e)
        {
            Instance = null;
            _autoScaleTimer?.Stop();

        }

        private void _autoScaleTimer_Tick(object sender, EventArgs e)
        {
            //if (AutoScaleToggle.IsChecked.Value)
            //   WpfPlot1.Plot.Axes.AutoScale();
        }

        private void BoXing_Loaded(object sender, RoutedEventArgs e)
        {
            InitDefaults();
            if (_loadLatestOnStartup)
            {
                var dbPath = FistDbManager.TryGetLatestDbPath();
                if (!string.IsNullOrWhiteSpace(dbPath))
                {
                    _dbFilePath = dbPath;
                    DbPathText.Text = _dbFilePath;
                    SetLastMinutes(10);
                    _ = LoadAndPlotAsync(latestOnly: true);
                    return;
                }
            }

            // fallback: ask user to select a DB
            SelectDbAndLoad();
        }

        private void InitDefaults()
        {
            if (PointCountCombo.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out var n) && n > 0)
                displayPointCount = n;

            if (int.TryParse(MaxRowsText.Text, out var r) && r > 0)
                maxRows = r;
        }
        int countd = 300;
        public void SetBoXing(double[] data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < channelCount; i++)
                {
                    Array.Copy(datas[i], 1, datas[i], 0, datas[i].Length - 1);
                    datas[i][datas[i].Length - 1] = data[i];
                }

                WpfPlot1.Refresh();
            });
        }

        private void SelectDbAndLoad()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "SQLite数据库|*.db;*.sqlite;*.sqlite3|所有文件|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == true)
            {
                _dbFilePath = dialog.FileName;
                DbPathText.Text = _dbFilePath;
                _ = LoadAndPlotAsync(latestOnly: false);
            }
        }

        private (DateTime? Start, DateTime? End) ReadTimeRange()
        {
            DateTime? start = null;
            DateTime? end = null;

            if (StartDatePicker.SelectedDate.HasValue)
            {
                var date = StartDatePicker.SelectedDate.Value.Date;
                if (TimeSpan.TryParse(StartTimeText.Text, out var ts))
                    start = date.Add(ts);
                else
                    start = date;
            }

            if (EndDatePicker.SelectedDate.HasValue)
            {
                var date = EndDatePicker.SelectedDate.Value.Date;
                if (TimeSpan.TryParse(EndTimeText.Text, out var ts))
                    end = date.Add(ts);
                else
                    end = date.AddDays(1).AddMilliseconds(-1);
            }

            return (start, end);
        }

        private static int ClampPositiveInt(string text, int fallback)
        {
            if (int.TryParse(text, out var n) && n > 0)
                return n;
            return fallback;
        }

        private async Task LoadAndPlotAsync(bool latestOnly)
        {
            if (WpfPlot1 == null) return;
            if (string.IsNullOrWhiteSpace(_dbFilePath))
            {
                StatusText.Text = "请先选择DB文件。";
                return;
            }

            displayPointCount = ClampPositiveInt((PointCountCombo.SelectedItem as ComboBoxItem)?.Content?.ToString(), displayPointCount);
            maxRows = ClampPositiveInt(MaxRowsText.Text, maxRows);

            var range = ReadTimeRange();
            DateTime? start = range.Start;
            DateTime? end = range.End;

            StatusText.Text = "正在查询数据...";

            var res = await FistDbManager.QuerySonarData(
                dbFilePath: _dbFilePath,
                startTime: start,
                endTime: end,
                maxRows: maxRows,
                latestFirst: latestOnly);

            if (res == null || res.Count == 0)
            {
                WpfPlot1.Plot.Clear();
                WpfPlot1.Refresh();
                StatusText.Text = "未查询到数据。";
                return;
            }

            // Downsample if needed
            int step = 1;
            if (displayPointCount > 0 && res.Count > displayPointCount)
                step = (int)Math.Ceiling(res.Count / (double)displayPointCount);

            var sampled = (step <= 1) ? res : res.Where((_, idx) => idx % step == 0).ToList();
            times = sampled.Select(x => x.接受时间).ToArray();

            double[][] resultArray = new double[channelCount][];
            resultArray[0] = sampled.Select(item => item.船横滚角度).ToArray();
            resultArray[1] = sampled.Select(item => item.声呐横滚角度).ToArray();
            resultArray[2] = sampled.Select(item => item.横滚电机动作角度).ToArray();
            resultArray[3] = sampled.Select(item => item.船俯仰角度).ToArray();
            resultArray[4] = sampled.Select(item => item.声呐俯仰角度).ToArray();
            resultArray[5] = sampled.Select(item => item.俯仰电机动作角度).ToArray();
            resultArray[6] = sampled.Select(item => item.陀螺横摇角速度).ToArray();
            resultArray[7] = sampled.Select(item => item.陀螺俯仰角速度).ToArray();
            resultArray[8] = sampled.Select(item => item.陀螺横摇角速度积分).ToArray();
            resultArray[9] = sampled.Select(item => item.陀螺俯仰角速度积分).ToArray();
            resultArray[10] = sampled.Select(item => item.横摇电流).ToArray();
            resultArray[11] = sampled.Select(item => item.俯仰电流).ToArray();

            // UI plot
            WpfPlot1.Plot.Clear();
            datas = new double[channelCount][];
            signals = new ScottPlot.Plottables.Signal[channelCount];
            walkers = new ScottPlot.DataGenerators.RandomWalker[channelCount];

            var palette = new ScottPlot.Palettes.Category10();
            for (int i = 0; i < channelCount; i++)
            {
                datas[i] = resultArray[i];
                signals[i] = WpfPlot1.Plot.Add.Signal(datas[i]);
                signals[i].LegendText = channelNames[i];
                WpfPlot1.Plot.Legend.FontSize = 40;
                signals[i].Color = palette.GetColor(i);
                signals[i].IsVisible = GetCheckBox(i)?.IsChecked == true;
            }

            WpfPlot1.Plot.Axes.SetLimitsX(0, sampled.Count);
            var plt = WpfPlot1.Plot;
            var yAxis = plt.Axes.GetYAxes().FirstOrDefault();
            if (yAxis != null)
            {
                yAxis.TickLabelStyle = new ScottPlot.LabelStyle
                {
                    FontSize = 40,
                    Italic = false,
                };
            }
            WpfPlot1.Plot.Legend.FontName = "微软雅黑";
            WpfPlot1.Plot.ShowLegend();
            CH = WpfPlot1.Plot.Add.Crosshair(0, 0);
            CH.TextColor = Colors.Red;
            CH.TextBackgroundColor = Colors.White;
            CH.HorizontalLine.Color = Colors.Black;
            CH.VerticalLine.Color = Colors.Black;
            WpfPlot1.Refresh();

            StatusText.Text = $"原始 {res.Count} 行，显示 {sampled.Count} 点 (step={step})";
        }

        private void ChannelCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < channelCount; i++)
            {
                if (signals != null && signals.Length > i)
                    signals[i].IsVisible = GetCheckBox(i)?.IsChecked == true;
            }
            if (WpfPlot1 != null)
                WpfPlot1.Refresh();
        }

        private void PointCountCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PointCountCombo.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out int newCount) && newCount > 0)
                displayPointCount = newCount;
        }

        private CheckBox GetCheckBox(int idx)
        {
            switch (idx)
            {
                case 0: return cb1;
                case 1: return cb2;
                case 2: return cb3;
                case 3: return cb4;
                case 4: return cb5;
                case 5: return cb6;
                case 6: return cb7;
                case 7: return cb8;
                case 8: return cb9;
                case 9: return cb10;
                case 10: return cb11;
                case 11: return cb12;
                default: return null;
            }
        }

        private async void DisplayScaling_MouseMove(object sender, MouseEventArgs e)
        {
            if (WpfPlot1 == null) return;
            if (CH == null) return;
            // 1. 立刻更新十字线（轻量）
            var p = e.GetPosition(WpfPlot1);
            var px = new ScottPlot.Pixel(p.X * WpfPlot1.DisplayScale, p.Y * WpfPlot1.DisplayScale);
            var coord = WpfPlot1.Plot.GetCoordinates(px);
            CH.Position = coord;

            // 2. 把重活放到线程池
            await Task.Run(() =>
            {
                int idx = (int)Math.Round(coord.X);
                var sb = new System.Text.StringBuilder();
                sb.Append($"当前[X:{coord.X:F0} Y:{coord.Y:F3}]");
                if (times != null && idx >= 0 && idx < times.Length)
                    sb.Append($"  时间:{times[idx]:yyyy-MM-dd HH:mm:ss.fff}");
                for (int i = 0; i < channelCount; i++)
                {
                    if (idx >= 0 && idx < datas[i].Length)
                        sb.Append($"  {channelNames[i]}:{datas[i][idx]:F3}");
                }
                return sb.ToString();
            }).ContinueWith(t =>
            {
                // 3. 回到 UI 线程更新标题
                if (!t.IsFaulted && IsLoaded)
                    Dispatcher.Invoke(() => this.Title = t.Result);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void AutoScaleToggle_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void SelectDb_Click(object sender, RoutedEventArgs e)
        {
            SelectDbAndLoad();
        }

        private void Query_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadAndPlotAsync(latestOnly: false);
        }

        private void SetLastMinutes(int minutes)
        {
            var now = DateTime.Now;
            var start = now.AddMinutes(-minutes);

            StartDatePicker.SelectedDate = start.Date;
            EndDatePicker.SelectedDate = now.Date;
            StartTimeText.Text = start.ToString("HH:mm:ss");
            EndTimeText.Text = now.ToString("HH:mm:ss");
        }

        private void Last10Min_Click(object sender, RoutedEventArgs e)
        {
            SetLastMinutes(10);
            _ = LoadAndPlotAsync(latestOnly: true);
        }

        private void Last1Hour_Click(object sender, RoutedEventArgs e)
        {
            SetLastMinutes(60);
            _ = LoadAndPlotAsync(latestOnly: true);
        }
    }

}
