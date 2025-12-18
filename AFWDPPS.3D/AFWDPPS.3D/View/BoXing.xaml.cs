using ScottPlot;
using System;
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
    public partial class BoXing : Window
    {
        private readonly int channelCount = 12;
        private int pointCount = 1000;
        private double[][] datas;
        private ScottPlot.Plottables.Signal[] signals;
        private ScottPlot.DataGenerators.RandomWalker[] walkers;
        private readonly string[] channelNames = { "船体横滚", "声呐横滚", "电机横滚动作", "船体俯仰", "声呐俯仰", "电机俯仰动作", "陀螺横摇角速度", "陀螺俯仰角速度", "陀螺横摇角速度积分", "陀螺俯仰角速度积分", "横摇电流", "俯仰电流" };
        private ScottPlot.Plottables.Crosshair CH;
        private readonly DispatcherTimer _autoScaleTimer =
    new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1000) };   // 2 Hz
        public static BoXing Instance { get; set; }
        public BoXing()
        {
            InitializeComponent();
            walkers = new ScottPlot.DataGenerators.RandomWalker[channelCount];
            datas = new double[channelCount][];
            signals = new ScottPlot.Plottables.Signal[channelCount];

            walkers[0] = new ScottPlot.DataGenerators.RandomWalker();
            walkers[1] = new ScottPlot.DataGenerators.RandomWalker();
            walkers[2] = new ScottPlot.DataGenerators.RandomWalker();
            walkers[3] = new ScottPlot.DataGenerators.RandomWalker();
            walkers[4] = new ScottPlot.DataGenerators.RandomWalker();
            walkers[5] = new ScottPlot.DataGenerators.RandomWalker();
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
            if (AutoScaleToggle.IsChecked.Value)
                WpfPlot1.Plot.Axes.AutoScale();
        }

        private void BoXing_Loaded(object sender, RoutedEventArgs e)
        {
            InitDataAndPlot();
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

        private void InitDataAndPlot()
        {
            if (WpfPlot1 == null) return;
            // 清空旧曲线
            WpfPlot1.Plot.Clear();
            datas = new double[channelCount][];
            signals = new ScottPlot.Plottables.Signal[channelCount];
            walkers = new ScottPlot.DataGenerators.RandomWalker[channelCount];
            var palette = new ScottPlot.Palettes.Category10();
            for (int i = 0; i < channelCount; i++)
            {
                datas[i] = new double[pointCount];
                //walkers[i] = new ScottPlot.DataGenerators.RandomWalker(i);
                //for (int j = 0; j < pointCount; j++)
                //    datas[i][j] = walkers[i].Next();
                signals[i] = WpfPlot1.Plot.Add.Signal(datas[i]);
                signals[i].LegendText = channelNames[i];
                signals[i].Color = palette.GetColor(i); // 这里获取颜色
                signals[i].IsVisible = GetCheckBox(i)?.IsChecked == true;
            }
            // ⬇⬇ 让横坐标完整显示所有点
            // WpfPlot1.Plot.Axes.SetLimitsX(0, pointCount - 1);


            WpfPlot1.Plot.Legend.FontName = "微软雅黑";
            WpfPlot1.Plot.ShowLegend();
            CH = WpfPlot1.Plot.Add.Crosshair(0, 0);
            CH.TextColor = Colors.White;
            CH.TextBackgroundColor = CH.HorizontalLine.Color;

            WpfPlot1.Refresh();
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
            if (PointCountCombo.SelectedItem is ComboBoxItem item && int.TryParse(item.Content.ToString(), out int newCount))
            {
                if (newCount != pointCount)
                {
                    pointCount = newCount;
                    InitDataAndPlot();
                }
            }
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
    }

}
