using System;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AFWDPP.Views
{
    /// <summary>
    /// MU 模块：独立页面，管理串口 B。
    /// 连接 MU 设备（FC41FD 协议），解析出船姿后写入 BusState。
    /// WP 端 80ms 定时器读 BusState 拼帧下发。
    /// </summary>
    public partial class MU : UserControl, IDisposable
    {
        private readonly MuPort _muPort = new MuPort();

        private int _frameCount;

        private DateTime _lastTime = DateTime.MinValue;

        private readonly DispatcherTimer _aliveTimer;

        private string _lastPortName;

        private int _lastBaudRate = 115200;

        // 设备身份识别 + WMI 插拔监听（替代轮询）
        private readonly SerialPortWatcher _watcher = new SerialPortWatcher();

        // UI 上显示的固定端口名（拔掉时不变，插上自动连）
        private string _displayPortName;

        // 用户曾经手动打开过串口（用于触发自动重连）
        private bool _wasConnectedByUser;

        public MU()
        {
            InitializeComponent();

            // 波特率下拉（参考 SP/FC/ZZ 风格）
            botelv.ItemsSource = new string[] { "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };
            botelv.SelectedIndex = 8;  // 默认 921600

            // 加载端口列表
            LoadComList();

            // 订阅串口回调
            _muPort.OnAttitude = OnAttitudeParsed;
            _muPort.OnFrame = OnMuFrame;

            // WMI 插拔事件（后台线程触发，要切到 UI 线程）
            _watcher.OnRemoved = port => Dispatcher.BeginInvoke(() => OnWatcherRemoved(port));
            _watcher.OnArrived = port => Dispatcher.BeginInvoke(() => OnWatcherArrived(port));

            // UI 初始化
            statusLabel.Text = "未连接";
            pitchLabel.Text = "—";
            yawLabel.Text = "—";
            frameCountLabel.Text = "0";
            lastUpdateLabel.Text = "—";

            // 离线策略初始提示（用 hex 颜色，避免依赖未导出的资源 key）
            offlinePolicyHint.Text = "当前：MU 离线时船姿字段填 0";
            offlinePolicyHint.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFDB3340"));

            // 每 500ms 刷新一次 MuAlive 状态显示 + 自动同步串口列表（参考 WP 风格）
            _aliveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _aliveTimer.Tick += AliveTimer_Tick;
            _aliveTimer.Start();
        }

        /// <summary>
        /// 加载当前可用的串口列表（参考 WP 风格，用 Common.Common.SearchPort）
        /// </summary>
        private void LoadComList()
        {
            try
            {
                var ports = Common.Common.SearchPort().Distinct().OrderBy(p => p).ToArray();
                comlist.ItemsSource = ports;
                if (ports.Length > 0) comlist.SelectedIndex = 0;
            }
            catch { }
        }

        /// <summary>
        /// 端口号变化时（无需操作，保留供扩展）。
        /// </summary>
        private void comlist_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        /// <summary>
        /// 手动刷新串口列表（插拔串口后立即看到新端口）。
        /// </summary>
        private void refreshComList_Click(object sender, RoutedEventArgs e)
        {
            LoadComList();
            // 顺便刷新一次状态显示
            AliveTimer_Tick(null, null);
        }

        /// <summary>
        /// 清空接收日志（发送 + 接受）。
        /// RunDataListViewer 没有公开 Clear 方法，直接访问内部 ListView。
        /// </summary>
        private void clearLogs_Click(object sender, RoutedEventArgs e)
        {
            ClearRunDataList(txlog);
            ClearRunDataList(rxlog);
        }

        /// <summary>
        /// 显示开关：控制 MU 串口收到帧时是否写入接受列表。
        /// 关闭后 MuPort 只更新 BusState.ShipAttitude，不调用 rxlog.AddOne，节约性能。
        /// </summary>
        private void rxtxshow_Click(object sender, RoutedEventArgs e)
        {
            // 通知 MuPort 改变刷新策略
            _muPort.EnableLogToUi = rxtxshow.IsChecked == true;
        }

        /// <summary>
        /// 清空 RunDataListViewer 内部 ListView 的所有项。
        /// </summary>
        private static void ClearRunDataList(导引头上位机程序.Views.UserControls.RunDataListViewer viewer)
        {
            if (viewer == null) return;
            // RunDataListViewer 内部 ListView 名叫 rtbLog，通过 VisualTreeHelper 找到
            var listView = FindVisualChild<System.Windows.Controls.ListView>(viewer);
            if (listView != null) listView.Items.Clear();
        }

        /// <summary>
        /// 沿视觉树向下查找指定类型的子元素（递归查找第一个匹配）。
        /// </summary>
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        private void AliveTimer_Tick(object sender, EventArgs e)
        {
            // WMI 已处理插拔自动重连，这里只负责：
            // 1) MuAlive 在线/离线状态显示
            // 2) 偶尔刷新一次 comlist（启动后第一次 + 兜底）

            if (!_muPort.IsOpen)
            {
                // 串口未打开：保持等待提示（只在没有手动操作时覆盖）
                if (!string.IsNullOrEmpty(_displayPortName) && _wasConnectedByUser)
                {
                    statusLabel.Text = $"等待重连 {_displayPortName}…";
                }
                else if (string.IsNullOrEmpty(_displayPortName))
                {
                    statusLabel.Text = "串口未打开";
                }
                return;
            }

            bool alive = (DateTime.Now - BusState.LastMuTime).TotalSeconds < BusState.MU_TIMEOUT_SECONDS;
            BusState.MuAlive = alive;

            if (alive)
            {
                double sec = (DateTime.Now - BusState.LastMuTime).TotalSeconds;
                statusLabel.Text = $"在线 ({sec:F1}s)";
            }
            else
            {
                statusLabel.Text = "离线（超时）";
            }
        }

        /// <summary>
        /// 打开/关闭串口按钮事件。
        /// </summary>
        private void openclosecom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_muPort.IsOpen)
                {
                    _muPort.Close();
                    _wasConnectedByUser = false;  // 主动关闭，不自动重连
                    _watcher.StopWatchers();
                    _displayPortName = null;
                    openclosecom.IsChecked = false;
                    statusLabel.Text = "已关闭";
                    rx.IsEnabled = false;
                    tx.IsEnabled = false;
                    comlist.IsEnabled = true;
                    botelv.IsEnabled = true;
                    return;
                }

                string port = comlist.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(port))
                {
                    MessageBox.Show("请先选择端口号");
                    openclosecom.IsChecked = false;
                    return;
                }
                int baud = int.Parse(botelv.SelectedItem.ToString());

                _muPort.Open(port, baud);

                _lastPortName = port;
                _lastBaudRate = baud;
                _displayPortName = port;
                _wasConnectedByUser = true;  // 用户主动连接成功 → 启用自动重连

                // 启动 WMI 插拔监听：记住设备身份（VID/PID），拔掉后按它重连
                string pnp = SerialPortWatcher.QueryPnpDeviceId(port);
                string vidPid = SerialPortWatcher.ExtractVidPid(pnp);
                if (!string.IsNullOrEmpty(vidPid))
                {
                    _watcher.StartWatchingForReconnect(port, vidPid);
                }

                _frameCount = 0;
                BusState.LastMuTime = DateTime.MinValue;
                BusState.MuAlive = false;

                openclosecom.IsChecked = true;
                rx.IsEnabled = true;
                tx.IsEnabled = true;
                comlist.IsEnabled = false;
                botelv.IsEnabled = false;
                statusLabel.Text = $"已连接 {port} @ {baud}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开失败: {ex.Message}");
                _muPort.Close();
                _watcher.StopWatchers();
                openclosecom.IsChecked = false;
                comlist.IsEnabled = true;
                botelv.IsEnabled = true;
                statusLabel.Text = "打开失败";
            }
        }

        /// <summary>
        /// WMI 检测到目标设备拔出。关掉串口，显示保持原端口名（用户感知不到）。
        /// </summary>
        private void OnWatcherRemoved(string port)
        {
            // 串口可能是 WMI 抢在我们之前检测到，MuPort 内部 IsOpen 还没更新 → 强制关闭
            try { _muPort.Close(); } catch { }
            _muPort.IsOpenProperty = false;  // 保险，确保 IsOpen 立刻返回 false
            openclosecom.IsChecked = false;
            rx.IsEnabled = false;
            tx.IsEnabled = false;
            comlist.IsEnabled = true;
            botelv.IsEnabled = true;
            BusState.MuAlive = false;
            // 显示端口名保持不变（_displayPortName 不动）
            statusLabel.Text = !string.IsNullOrEmpty(_displayPortName)
                ? $"等待重连 {_displayPortName}…"
                : "串口已拔出";
        }

        /// <summary>
        /// WMI 检测到目标设备插入（按 VID/PID 匹配同一台设备）。自动重连。
        /// </summary>
        private void OnWatcherArrived(string port)
        {
            // port 是新出现的端口名，可能跟原来的 _displayPortName 不一样（COM3 → COM5）
            // 但用户界面看到的名字保持不变，内部用新名字去开
            try
            {
                _muPort.Open(port, _lastBaudRate);
                _lastPortName = port;  // 内部记录新名字

                // UI 上的 comlist 选中项同步成新名字
                if (comlist.ItemsSource is string[] arr && arr.Contains(port))
                {
                    comlist.SelectedItem = port;
                }

                _frameCount = 0;
                BusState.LastMuTime = DateTime.MinValue;
                BusState.MuAlive = false;

                openclosecom.IsChecked = true;
                rx.IsEnabled = true;
                tx.IsEnabled = true;
                comlist.IsEnabled = false;
                botelv.IsEnabled = false;
                statusLabel.Text = $"已自动重连 {port} @ {_lastBaudRate}";
            }
            catch
            {
                statusLabel.Text = $"重连失败，请检查 {port}";
            }
        }

        /// <summary>
        /// MU 解析出船姿后回调（后台线程触发）。
        /// 解析后写入 BusState，并切到 UI 线程刷新显示。
        /// </summary>
        private void OnAttitudeParsed(byte h3, byte l3, byte h4, byte l4)
        {
            // 先写入全局共享（任何后台线程单字节赋值是原子的）
            BusState.ShipAttitude[0] = h3;
            BusState.ShipAttitude[1] = l3;
            BusState.ShipAttitude[2] = h4;
            BusState.ShipAttitude[3] = l4;
            BusState.LastMuTime = DateTime.Now;
            BusState.MuAlive = true;

            Dispatcher.BeginInvoke(() =>
            {
                short sPitch = (short)((h3 << 8) | l3);
                short sYaw = (short)((h4 << 8) | l4);

                _frameCount++;
                _lastTime = DateTime.Now;

                pitchLabel.Text = $"{sPitch / 1000.0,8:F3}°";
                yawLabel.Text   = $"{sYaw   / 1000.0,8:F3}°";
                frameCountLabel.Text = _frameCount.ToString().PadLeft(8);
                lastUpdateLabel.Text = _lastTime.ToString("HH:mm:ss.fff");
            });
        }

        /// <summary>
        /// MU 收到完整一帧后的 hex 回调（后台线程触发）
        /// </summary>
        private void OnMuFrame(string hex)
        {
            Dispatcher.BeginInvoke(() =>
            {
                rxlog.AddOne(hex, "收←◆");
            });
        }

        /// <summary>
        /// MU 离线时船姿字段填充策略开关
        /// 勾选 = 填 0（默认）；不勾选 = 保持上一组值
        /// </summary>
        private void offlinePolicy_Click(object sender, RoutedEventArgs e)
        {
            if (offlinePolicy.IsChecked == true)
            {
                BusState.OfflineFillZero = true;
                offlinePolicyHint.Text = "当前：MU 离线时船姿字段填 0";
                // 红色（与项目 HCDangerColor #FFDB3340 一致）
                offlinePolicyHint.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFDB3340"));
            }
            else
            {
                BusState.OfflineFillZero = false;
                offlinePolicyHint.Text = "当前：MU 离线时船姿字段保持上一组值";
                // 黄色（与项目 HCWarningColor #FFE9AF20 一致）
                offlinePolicyHint.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFE9AF20"));
            }
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            _aliveTimer?.Stop();
            _muPort?.Dispose();
        }
    }
}
