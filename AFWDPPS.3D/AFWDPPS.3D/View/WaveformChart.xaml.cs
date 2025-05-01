using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfApp3D
{
    /// <summary>
    /// ComboBox.xaml 的交互逻辑
    /// </summary>
    public partial class WaveformChart
    {
        private DispatcherTimer uiSyncTimer;
        public WaveformChart()
        {

            InitializeComponent();

            WindowState = WindowState.Normal;

            // 初始化 DispatcherTimer
            //uiSyncTimer = new DispatcherTimer();
            //uiSyncTimer.Tick += OnUITimerTick;
            //uiSyncTimer.Interval = TimeSpan.FromMilliseconds(10);
            //uiSyncTimer.Start();
            InitializeChart();
        }



        private void InitializeChart()
        {
            cartesianChart.Series = new SeriesCollection
    {
        new LineSeries
        {
            Title = "船体俯仰角度",
            Values = new ChartValues<double>(),
            PointGeometry = DefaultGeometries.Circle,
            PointGeometrySize = 10,
            Fill = Brushes.Transparent,
            Stroke = Brushes.DeepSkyBlue,
            StrokeThickness = 2
        },
        new LineSeries
        {
            Title = "声呐俯仰角度",
            Values = new ChartValues<double>(),
            PointGeometry = DefaultGeometries.Square,
            PointGeometrySize = 10,
            Fill = Brushes.Transparent,
            Stroke = Brushes.HotPink,
            StrokeThickness = 2
        }
    };

            lineSeries1 = cartesianChart.Series[0] as LineSeries;
            lineSeries2 = cartesianChart.Series[1] as LineSeries;
            // 设置图例位置为左上角
            cartesianChart.LegendLocation = LegendLocation.Top;
            // 设置x轴和y轴的范围（可选，根据需要调整）  
            cartesianChart.AxisX.Add(new Axis
            {
                Separator = new Separator { IsEnabled = false }, //网格线
                DisableAnimations = true,
                MinValue = 0, // 设置x轴最小值  
                MaxValue = 100, // 设置x轴最大值（假设我们想要显示100个数据点）  
                LabelFormatter = x => x.ToString("N0") // 格式化x轴标签  
            });
            cartesianChart.AxisY.Add(new Axis
            {
                Separator = new Separator { IsEnabled = false },//网格线
                DisableAnimations = true,
                MinValue = -45, // 设置y轴最小值  
                MaxValue = 45, // 设置y轴最大值（假设波形在-5到5之间波动）  
                LabelFormatter = value => value.ToString("F2") // 格式化y轴标签  
            });
        }

        private Random random = new Random();
        private SeriesCollection seriesCollection;
        private LineSeries lineSeries1;
        private LineSeries lineSeries2;

        public void OnUITimerTick(double x, double x1)
        {

            Dispatcher.Invoke(() =>
            {
                // 生成新的数据点并添加到系列中
                lineSeries1.Values.Add((double)x);
                // 移除旧的数据点以保持图表中数据点的数量稳定  
                if (lineSeries1.Values.Count > 100)
                {
                    lineSeries1.Values.RemoveAt(0);
                    cartesianChart.AxisX[0].MaxValue = lineSeries1.Values.Count; // 更新x轴最大值以匹配数据点数量  
                    cartesianChart.AxisX[0].MinValue = lineSeries1.Values.Count - 99; // 更新x轴最小值以匹配数据点数量（保持100个数据点的窗口）  
                }
                lineSeries2.Values.Add((double)x1);
                // 移除旧的数据点以保持图表中数据点的数量稳定  
                if (lineSeries2.Values.Count > 100)
                {
                    lineSeries2.Values.RemoveAt(0);
                    cartesianChart.AxisX[0].MaxValue = lineSeries2.Values.Count; // 更新x轴最大值以匹配数据点数量  
                    cartesianChart.AxisX[0].MinValue = lineSeries2.Values.Count - 99; // 更新x轴最小值以匹配数据点数量（保持100个数据点的窗口）  
                }
                // 刷新图表以显示更新后的数据  
                cartesianChart.InvalidateVisual();







            });
        }
    }
}





