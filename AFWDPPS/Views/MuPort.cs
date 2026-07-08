using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace AFWDPP.Views
{
    /// <summary>
    /// MU 设备串口（串口 B）：接 MU 船姿设备。
    /// 协议：[FC][41][data 52B][FD]，固定 56 字节一帧。
    /// offset 19 = float pitch(弧度)，offset 23 = float yaw(弧度)。
    /// 解析后 ×57.3 转角度，再 ×1000 转 short 大端，写入 BusState.ShipAttitude[4]。
    /// </summary>
    public class MuPort : IDisposable
    {
        // 底层串口对象
        private SerialPort _sp;

        // 接收缓冲
        private readonly List<byte> _buf = new List<byte>();

        // 解析状态机：0=HEAD1, 1=HEAD2, 2=DATA
        private int _state;

        /// <summary>
        /// 解析到船姿后的回调：参数 (H3, L3, H4, L4)，已 ×1000。
        /// MU 后台线程触发。
        /// </summary>
        public Action<byte, byte, byte, byte> OnAttitude;

        /// <summary>
        /// 原始帧收到后的回调：参数为 hex 字符串。
        /// </summary>
        public Action<string> OnFrame;

        /// <summary>
        /// 是否把原始帧写入接受列表（rxtxshow 开关控制）。
        /// 关闭后只更新 BusState.ShipAttitude，不调 OnLog，节约 UI 性能。
        /// </summary>
        public bool EnableLogToUi = true;

        /// <summary>
        /// 串口是否打开。
        /// </summary>
        public bool IsOpen
        {
            get
            {
                try { return _sp != null && _sp.IsOpen; }
                catch { return false; }
            }
        }

        /// <summary>
        /// 强制覆盖 IsOpen 状态（用于 WMI 检测到拔出但底层串口状态还没刷新的情况）。
        /// 设为 false 后，下次 Open() 调用会自动恢复。
        /// </summary>
        public bool IsOpenProperty
        {
            get => IsOpen;
            set
            {
                if (!value && _sp != null)
                {
                    try { _sp.DataReceived -= OnRx; } catch { }
                    try { _sp.ErrorReceived -= OnError; } catch { }
                    try { if (_sp.IsOpen) _sp.Close(); } catch { }
                    try { _sp.Dispose(); } catch { }
                    _sp = null;
                    _state = 0;
                    _buf.Clear();
                }
            }
        }

        /// <summary>
        /// 打开串口。先确保关闭旧的，再创建新串口并订阅接收事件。
        /// </summary>
        /// <param name="portName">端口名（如 "COM3"）</param>
        /// <param name="baudRate">波特率</param>
        public void Open(string portName, int baudRate)
        {
            Close();

            _sp = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000,
                DtrEnable = true,
                RtsEnable = true
            };
            _sp.DataReceived += OnRx;
            _sp.ErrorReceived += OnError;
            _sp.Open();

            _state = 0;
            _buf.Clear();
        }

        /// <summary>
        /// 关闭串口。
        /// </summary>
        public void Close()
        {
            try
            {
                if (_sp != null)
                {
                    _sp.DataReceived -= OnRx;
                    _sp.ErrorReceived -= OnError;
                    if (_sp.IsOpen)
                    {
                        try { _sp.Close(); } catch { }
                    }
                    _sp.Dispose();
                }
            }
            catch { }
            _sp = null;
            _state = 0;
            _buf.Clear();
        }

        /// <summary>
        /// 串口数据到达事件（后台线程触发）。
        /// 状态机解析协议：HEAD1=FC → HEAD2=41 → DATA 收到 FD 收尾。
        /// </summary>
        private void OnRx(object sender, SerialDataReceivedEventArgs e)
        {
            if (!IsOpen) return;

            try
            {
                int n = _sp.BytesToRead;
                if (n == 0) return;
                byte[] tmp = new byte[n];
                int read = _sp.Read(tmp, 0, n);
                if (read == 0) return;

                for (int i = 0; i < read; i++)
                {
                    byte b = tmp[i];
                    if (_state == 0)
                    {
                        if (b == 0xFC) { _buf.Clear(); _buf.Add(b); _state = 1; }
                    }
                    else if (_state == 1)
                    {
                        if (b == 0x41) { _buf.Add(b); _state = 2; }
                        else _state = 0;
                    }
                    else
                    {
                        _buf.Add(b);
                        if (_buf.Count >= 56)
                        {
                            if (_buf[0] == 0xFC && _buf[1] == 0x41 && _buf[55] == 0xFD)
                            {
                                ParseFrame(_buf.ToArray());
                            }
                            _buf.Clear();
                            _state = 0;
                        }
                        else if (_buf.Count > 56)
                        {
                            _buf.Clear();
                            _state = 0;
                        }
                    }
                }
            }
            catch (TimeoutException) { }
            catch { Close(); }
        }

        private void OnError(object sender, SerialErrorReceivedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 解析完整的一帧 MU 数据（56 字节）。
        /// offset 19 = float pitch(滚转 弧度)，offset 23 = float yaw(横摇 弧度)。
        /// </summary>
        private void ParseFrame(byte[] f)
        {
            try
            {
                // 提取两个 float（弧度，小端）
                float pitch = BitConverter.ToSingle(f, 19);
                float yaw = BitConverter.ToSingle(f, 23);

                // 弧度 → 度
                pitch *= 57.3f;
                yaw *= 57.3f;

                // ×1000 转 short，大端拆分
                short sPitch = (short)(pitch * 1000);
                short sYaw = (short)(yaw * 1000);

                // 回调通知 BusState（MU 后台线程）— 永远执行（不依赖开关）
                OnAttitude?.Invoke(
                    (byte)(sPitch >> 8), (byte)(sPitch & 0xFF),
                    (byte)(sYaw >> 8), (byte)(sYaw & 0xFF)
                );

                // 原始 hex 回调 — 只在开启显示时执行（节约 UI 性能）
                if (EnableLogToUi)
                {
                    string hex = BitConverter.ToString(f).Replace("-", " ").ToUpper();
                    OnFrame?.Invoke(hex);
                }
            }
            catch { }
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose() => Close();
    }
}
