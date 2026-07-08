using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace AFWDPP.Views
{
    /// <summary>
    /// 串口设备身份识别 + WMI 插拔事件监听。
    ///
    /// 设计要点：
    /// 1) 串口名（COM3/COM4）会变，但设备的 VID/PID 不变。
    ///    通过 WMI Win32_SerialPort 的 PNPDeviceID 提取 VID/PID，记住"设备身份"。
    /// 2) 拔掉/插上后通过 WMI __InstanceDeletionEvent / __InstanceCreationEvent 立即响应，
    ///    不用轮询 SerialPort.GetPortNames()。
    /// 3) 插上时按 VID/PID 匹配 → 找到新端口名 → 自动重连。
    /// </summary>
    public class SerialPortWatcher : IDisposable
    {
        /// <summary>
        /// 从 WMI PNPDeviceID 提取的设备身份，例如 "VID_1A86&PID_7523"。
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// 当前连接的端口名（如 "COM3"），由 WMI 事件刷新。
        /// </summary>
        public string CurrentPortName { get; private set; }

        /// <summary>
        /// 拔掉事件：参数 = 消失的端口名。
        /// </summary>
        public Action<string> OnRemoved;

        /// <summary>
        /// 插上事件：参数 = 新出现的端口名。
        /// </summary>
        public Action<string> OnArrived;

        private ManagementEventWatcher _arrivedWatcher;
        private ManagementEventWatcher _removedWatcher;
        private bool _disposed;

        /// <summary>
        /// 把 WMI PNPDeviceID 归一化成 VID_xxxx&PID_xxxx 形式。
        /// 例：USB\VID_1A86&PID_7523\5&2F8A4F1F&0&2  →  VID_1A86&PID_7523
        /// </summary>
        public static string ExtractVidPid(string pnpDeviceId)
        {
            if (string.IsNullOrEmpty(pnpDeviceId)) return null;
            var m = Regex.Match(pnpDeviceId, @"VID_([0-9A-Fa-f]{4}).*?PID_([0-9A-Fa-f]{4})");
            if (!m.Success) return null;
            return $"VID_{m.Groups[1].Value.ToUpper()}&PID_{m.Groups[2].Value.ToUpper()}";
        }

        /// <summary>
        /// 通过端口名查 WMI，返回其 PNPDeviceID。
        /// 例：COM3 → USB\VID_1A86&PID_7523\...
        /// </summary>
        public static string QueryPnpDeviceId(string portName)
        {
            if (string.IsNullOrEmpty(portName)) return null;
            try
            {
                string query = $"SELECT PNPDeviceID FROM Win32_SerialPort WHERE DeviceID = '{portName.Replace("'", "''")}'";
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        try { return mo["PNPDeviceID"]?.ToString(); }
                        finally { mo.Dispose(); }
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 拔掉时记录设备身份（VID/PID），启动 WMI 监听，等待插上后匹配。
        /// </summary>
        public void StartWatchingForReconnect(string portName, string vidPid)
        {
            DeviceId = vidPid;
            CurrentPortName = portName;

            StopWatchers();

            // 监听串口拔出
            var qRemove = new WqlEventQuery(
                "SELECT * FROM __InstanceDeletionEvent WITHIN 1 " +
                "WHERE TargetInstance ISA 'Win32_SerialPort'");
            _removedWatcher = new ManagementEventWatcher(qRemove);
            _removedWatcher.EventArrived += RemovedEventArrived;
            _removedWatcher.Start();

            // 监听串口插入
            var qArr = new WqlEventQuery(
                "SELECT * FROM __InstanceCreationEvent WITHIN 1 " +
                "WHERE TargetInstance ISA 'Win32_SerialPort'");
            _arrivedWatcher = new ManagementEventWatcher(qArr);
            _arrivedWatcher.EventArrived += ArrivedEventArrived;
            _arrivedWatcher.Start();
        }

        /// <summary>
        /// 只停止监听（不释放资源，方便后续重启）。
        /// </summary>
        public void StopWatchers()
        {
            try { _removedWatcher?.Stop(); _removedWatcher?.Dispose(); } catch { }
            try { _arrivedWatcher?.Stop(); _arrivedWatcher?.Dispose(); } catch { }
            _removedWatcher = null;
            _arrivedWatcher = null;
        }

        private void RemovedEventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var mo = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                string port = mo["DeviceID"]?.ToString();
                if (port == CurrentPortName)
                {
                    CurrentPortName = null;
                    OnRemoved?.Invoke(port);
                }
            }
            catch { }
        }

        private void ArrivedEventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var mo = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                string port = mo["DeviceID"]?.ToString();
                string pnp = mo["PNPDeviceID"]?.ToString();
                string vidPid = ExtractVidPid(pnp);

                // 没记录过设备身份 → 不处理
                if (string.IsNullOrEmpty(DeviceId)) return;

                // 按 VID/PID 匹配 → 是同一台设备
                if (!string.IsNullOrEmpty(vidPid) &&
                    string.Equals(vidPid, DeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    CurrentPortName = port;
                    OnArrived?.Invoke(port);
                }
            }
            catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopWatchers();
        }
    }
}