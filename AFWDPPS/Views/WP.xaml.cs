using AFWDPP.Common;
using HandyControl.Data;
using LL2024.Algorithms.UpdateDSP;
using Rubyer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using static AFWDPP.Common.Common;
using Application = System.Windows.Application;

namespace AFWDPP.Views
{
    public static class Logger
    {
        public static int TestCount = 0; // 静态变量用于保存当前的TestCount
        private static string logDirectory;

        public static void Initialize(string pth)
        {
            logDirectory = pth;
            EnsureLogFolderExists(pth);
            LoadLastTestCount();
        }

        private static void EnsureLogFolderExists(string pth)
        {
            if (!Directory.Exists(pth))
            {
                Directory.CreateDirectory(pth);
            }
        }

        private static void LoadLastTestCount()
        {
            // 获取日志目录下的所有文件
            var files = Directory.GetFiles(logDirectory, "采集数据*.txt")
                                 .Select(f => new FileInfo(f))
                                 .OrderByDescending(f => f.CreationTime)
                                 .ToList();

            if (files.Any())
            {
                // 假设文件名格式为 "采集数据X_yyyy-MM-dd.txt"，其中X是TestCount
                foreach (var file in files)
                {
                    if (int.TryParse(Path.GetFileNameWithoutExtension(file.Name).Replace("采集数据", "").Split('_')[0], out int count))
                    {
                        TestCount = count;
                        break;
                    }
                }
            }

            // 确保TestCount从最后一个已有的编号开始
            TestCount++;
        }
        public static void WriteLog(string message, string logDirectory)
        {
            EnsureLogFolderExists(logDirectory);
            // 生成日志文件名 (例如: 采集数据1_2025-01-20.txt)
            string logFileName = $"采集数据{TestCount}_{DateTime.Now:yyyyMMdd}.txt";
            string logFilePath = Path.Combine(logDirectory, logFileName);

            // 使用 File.AppendAllText 写入日志信息
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},{message}{Environment.NewLine}";
                File.AppendAllText(logFilePath, logEntry);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to write log: {ex.Message}");
            }

        }
    }
    /// <summary>
    /// FC.xaml 的交互逻辑
    /// </summary>
    public partial class WP : UserControl, IDisposable
    {
        #region 全局变量
        public System.IO.Ports.SerialPort serialPort2;


        public Thread RecDataDeal;
        DispatcherTimer timerhandshake;
        DispatcherTimer timer;
        #endregion
        public WP()
        {
            InitializeComponent();

            DSP28335.SetDLE_STX_ETX();
            // this.DataContext = App.Current.Services.GetService<DescriptionViewModel>();
            botelv.ItemsSource = new string[] { "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };

            botelv.SelectedIndex = 5;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
            //握手定时器
            timerhandshake = new DispatcherTimer();
            timerhandshake.Interval = TimeSpan.FromMilliseconds(80);
            timerhandshake.IsEnabled = true;
            timerhandshake.Tick += timerhandshake_Tick;
            var ports = Common.Common.SearchPort();
            if (comlist.ItemsSource == null || !ports.SequenceEqual(comlist.ItemsSource as IList<string>))
            {
                comlist.ItemsSource = Common.Common.SearchPort();
            }
            if (comlist.SelectedItem == null && comlist.Items.Count > 0)
            {
                comlist.SelectedIndex = comlist.Items.Count - 1;
            }

            this.serialPort2 = new System.IO.Ports.SerialPort();
            serialPort2.RtsEnable = true;
            //this.serialPort2.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort1_DataReceived);
            Loaded += FC_Loaded;

            var moduleGroups = Mbslist.GroupBy(m => m.模块).ToList();
            // 创建一个字典来快速查找每个模块下的功能
            moduleFunctions = moduleGroups.ToDictionary(
               g => g.Key,
               g => g.Select(m => m.功能).ToList()
           );
            var moduleNames = moduleGroups.Select(g => g.Key).ToList();
        }

        Dictionary<string, List<string>> moduleFunctions;
        private void FC_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Initialize(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "发送数据"));
            Logger.Initialize(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "接受数据"));
        }

        byte[] SendCacheToZhangPengFeiB = new byte[7];
        byte[] SendCacheToZhangPengFeiC = new byte[56]; //模拟MU数据

        byte da = 0;
        private void timerhandshake_Tick(object sender, EventArgs e)
        {
            //#region 模拟MU数据发送
            //SendCacheToZhangPengFeiC[0] = 0x7F;
            //SendCacheToZhangPengFeiC[1] = 0x80;
            //SendCacheToZhangPengFeiC[2] = 0;
            //SendCacheToZhangPengFeiC[3] = 0xC1;
            //if (da > 255) da = 0;
            //SendCacheToZhangPengFeiC[53] = da++;
            //sendData(SendCacheToZhangPengFeiC, 56);

            //#endregion

            if (sendermodel.IsChecked == true)
            {
                SendCacheToZhangPengFeiB[0] = 0xA5;

                SendCacheToZhangPengFeiB[1] = 0x02;

                // Step 1: Multiply by 1000 and cast to short.
                short angleValue = (short)(x1.Value * 1000);

                // Step 2: Extract high and low bytes.
                SendCacheToZhangPengFeiB[2] = (byte)((angleValue >> 8) & 0xFF); // High byte
                SendCacheToZhangPengFeiB[3] = (byte)(angleValue & 0xFF); // Low byte

                short angleValue1 = (short)(y1.Value * 1000);

                // Step 2: Extract high and low bytes.
                SendCacheToZhangPengFeiB[4] = (byte)((angleValue1 >> 8) & 0xFF); // High byte
                SendCacheToZhangPengFeiB[5] = (byte)(angleValue1 & 0xFF); // Low byte
                SendCacheToZhangPengFeiB[6] = GetSum(SendCacheToZhangPengFeiB);
                sendData(SendCacheToZhangPengFeiB, 7);
            }
        }

        public byte GetSum(byte[] data)
        {
            byte sum = 0;

            for (int i = 0; i < data.Length - 1; i++)
            {
                sum += data[i];
            }
            return (byte)(sum & 0xFF);
        }
        byte[] testdata1 = new byte[83];

        public IEnumerable<T> FindChildrenOfType<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfType in FindChildrenOfType<T>(child))
                    {
                        yield return childOfType;
                    }
                }
            }
        }
        public void GetTextBoxes(DependencyObject parent)
        {
            var textBoxes = FindChildrenOfType<TextBox>(parent);
            foreach (TextBox textBox in textBoxes)
            {
                testdata1.ToByte(textBox);
            }
        }

        private const byte HEAD1 = 0x78;
        /// <summary>
        /// 通讯数据接收状态机标志
        /// </summary>
        private int G_int_ComStatus = 0;
        private List<byte> G_btList_RecBuf = new List<byte>();
        private List<byte> G_btList_RecBuf_R = new List<byte>();
        private int G_int_RecBufLen = 0;
        private enum enum_ComStatus
        {
            COM_STATUS_HEAD1 = 0,
            COM_STATUS_HEAD2,
            COM_STATUS_DEVICE_ID,
            COM_STATUS_DEVICE_FC1,
            COM_STATUS_DEVICE_FC2,
            COM_STATUS_LEN,
            COM_STATUS_DATA
        }

        int COUNTS = 0;
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            int bytesToRead = sp.BytesToRead;
            byte[] buffer = new byte[bytesToRead];

            // 读取数据到缓冲区  
            int nbrDataRead = sp.Read(buffer, 0, bytesToRead);
            G_btList_RecBuf_R.Clear();
            foreach (byte tmpByte in buffer)
            {
                switch (G_int_ComStatus)
                {
                    case (int)enum_ComStatus.COM_STATUS_HEAD1:
                        G_btList_RecBuf.Clear();

                        if (tmpByte == 0xA5)
                        {
                            // tmpHEAD1 = tmpByte;
                            //切换协议解析状态
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD2;
                            G_btList_RecBuf.Add(tmpByte);
                        }
                        break;
                    case (int)enum_ComStatus.COM_STATUS_HEAD2:
                        if (tmpByte == 0x00 || tmpByte == 0x01 || tmpByte == 0x02 || tmpByte == 0x03 || tmpByte == 0x04 || tmpByte == 0x05)
                        {
                            //切换协议解析状态
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_DATA;
                            G_btList_RecBuf.Add(tmpByte);
                        }
                        break;
                    case (int)enum_ComStatus.COM_STATUS_DATA:
                        G_btList_RecBuf.Add(tmpByte);

                        //数据接收完成后的有效性判断
                        if (G_btList_RecBuf.Count == 7)  //包接收完成
                        {
                            byte[] Rbuffer = G_btList_RecBuf.ToArray();
                            string hexString = BitConverter.ToString(Rbuffer).Replace("-", " ").ToUpper();
                            // 使用BitConverter将字节数组转换为float
                            Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                IDC_EDIT_CHECKA_0_0.Content = headcount++;
                                if (!rx.IsEnabled)
                                    rx.IsEnabled = true;

                                if (Rbuffer.Length == 7 && Rbuffer[0] == 0xA5 && GetSum(Rbuffer) == Rbuffer[6])
                                {
                                    if (COUNTS++ > 10)
                                    {
                                        if (rxtxshow.IsChecked == true)
                                            rxlog.AddOne(hexString, "收←◆");
                                        x2.Content = ParseAngleFromBytes(Rbuffer[2], Rbuffer[3]);
                                        y2.Content = ParseAngleFromBytes(Rbuffer[4], Rbuffer[5]);
                                        string LogFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "接受数据");

                                        COUNTS = 0;
                                        Logger.WriteLog(hexString + "," + x2.Content + "," + y2.Content, LogFolderPath);
                                    }
                                }
                                //else
                                //{
                                //    if (rxtxshow.IsChecked == true)
                                //        rxlog.AddOne("校验和或者帧头、长度错误", "收←◆");
                                //}

                            });
                            G_btList_RecBuf.Clear();
                            //检查校验和字节
                            //if ((DSP28335.CheckSumNomarl(G_btList_RecBuf.ToArray())))
                            //{
                            //    G_btList_RecBuf_R.AddRange(G_btList_RecBuf);
                            //}

                            //else
                            //{
                            //    G_btList_RecBuf.Clear();
                            //    //string str_ErrorInfo = "“";
                            //    //foreach (byte tmpbt in G_btList_RecBuf)
                            //    //{
                            //    //    str_ErrorInfo += tmpbt.ToString("X2") + " ";
                            //    //}
                            //    //str_ErrorInfo += "”帧校验和错误！";

                            //}

                            //切换协议解析状态
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD1;
                        }

                        //数据包长度超限检查
                        if (G_btList_RecBuf.Count >= 7)
                        {
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD1;

                            //str_ErrorInfo += "“";
                            //for (int i = 0; i < 6; i++)
                            //{
                            //    str_ErrorInfo += G_btList_RecBuf[i].ToString("X2") + " ";
                            //}
                            //str_ErrorInfo += "......”该帧数据长度超限！";

                            //清空相关缓存
                            G_btList_RecBuf.Clear();
                        }
                        break;

                    default:
                        G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD1;
                        break;
                }
            }

            //
            if (COUNTS++ > 10)
            {
                COUNTS = 0;
                if (nbrDataRead == 0)
                    return;


            }
        }
        public float ParseAngleFromBytes(byte highByte, byte lowByte)
        {
            // Combine the bytes into a short. This preserves the sign bit.
            short angleValue = (short)((highByte << 8) | lowByte);

            // Convert back to a floating-point number and divide by 1000.
            return (float)angleValue / 1000;
        }
        private int headcount = 0;

        bool istoendd = false;
        void GetSPsum(byte[] data, int length)
        {
            int i = 0;
            byte result = 0;
            for (i = 0; i < length - 1; i++)
            {
                result += data[i];
            }
            result &= 0x00FF;
            data[length - 1] = result;
        }
        #region 串口打开关闭
        bool UpdateFlag = false;
        bool IsStartOneTest = false;

        //打开关闭串口
        private async void OpenCloseCom()
        {
            try
            {
                //根据当前串口属性来判断是否打开
                if (serialPort2 != null && serialPort2.IsOpen)
                {
                    if (UpdateFlag == true)
                    {
                        if (await MessageBoxR.Warning("正在进行固件升级，关闭串口会导致固件升级失败，是否要关闭？", button: MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            //// 停止固件升级
                            UpdateFlag = false;
                            serialPort2.DataReceived -= serialPort1_DataReceived;
                            ////串口已经处于打开状态
                            serialPort2.Close();    //关闭串口

                            comlist.IsEnabled = true;
                            botelv.IsEnabled = true;

                        }
                        else
                        {
                            openclosecom.IsChecked = true;
                            return;
                        }
                    }
                    else
                    {     // 解绑事件处理程序
                        serialPort2.DataReceived -= serialPort1_DataReceived;
                        ////串口已经处于打开状态
                        serialPort2.Close();    //关闭串口


                        comlist.IsEnabled = true;
                        botelv.IsEnabled = true;

                    }
                }
                else
                {
                    //串口已经处于关闭状态，则设置好串口属性后打开
                    comlist.IsEnabled = false;
                    botelv.IsEnabled = false;

                    ////配置串口
                    string comname = "";
                    if ((comlist.SelectedItem as string).Contains("("))
                        comname = (comlist.SelectedItem as string).Split('(')[1].Replace(")", "");
                    if (comname.Contains("->"))
                        comname = comname.Split('-')[0];
                    if (comname == "")
                        comname = comlist.SelectedItem as string;
                    serialPort2.PortName = comname;
                    serialPort2.BaudRate = Convert.ToInt32(botelv.SelectedItem);
                    serialPort2.StopBits = StopBits.One;
                    serialPort2.Parity = Parity.None;
                    serialPort2.DataBits = 8;
                    serialPort2.DataReceived -= serialPort1_DataReceived;
                    serialPort2.DataReceived += serialPort1_DataReceived;
                    serialPort2.Open();//打开串口
                    notifytimes = 0;
                    ////创建数据处理线程

                }
            }
            catch (Exception ex)
            {
                Message.Error(ex.Message);
                serialPort2.Close();    //关闭串口
                comlist.IsEnabled = true;
                botelv.IsEnabled = true;
                openclosecom.IsChecked = false;
                return;
                //RecDataDeal.Abort();
            }
            if (serialPort2 == null)
            {
                openclosecom.IsChecked = false;
                //Message.Warning(comlist.SelectedItem as string + "已断开连接！");
                return;
            }

            openclosecom.IsChecked = serialPort2.IsOpen;
            if (serialPort2.IsOpen)
            {
                // Message.Success(comlist.SelectedItem as string + "连接成功！");
            }
            else
            {
                // Logger.TestCount++;
                // Message.Warning(comlist.SelectedItem as string + "已断开连接！");
            }

        }
        int count = 0;
        List<byte> dl = new List<byte>();
        /// <summary>
        /// 数据处理线程函数
        /// </summary>
        public void ProtocolParsing()
        {

        }
        bool issend = false;
        public int needFlashTime = 0;
        public int ComfirTimes = 3;

        /// <summary>
        /// 获取通用回复结果
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public string GetCommAckResult(byte code)
        {
            string str = DSP28335.GetCommAckResult(code);
            if (code == 0x05)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    serialPort2.Close();    //关闭串口
                    comlist.IsEnabled = true;
                    botelv.IsEnabled = true;
                    openclosecom.IsChecked = false;
                    Message.Success(str + "！串口已关闭！");
                });
            }
            return str;
        }

        /// <summary>
        /// 发送二进制数据包
        /// </summary>
        /// <param name="packorder"></param>
        #endregion
        #region 串口读取数据
        /// <summary>
        /// 串口读取数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        Queue<byte> RecDataQueue = new Queue<byte>();//接收队列，用于数据处理
        int notifytimes = 0;

        #endregion
        private void openclosecom_Click(object sender, RoutedEventArgs e)
        {
            OpenCloseCom();
        }

        /// <summary>
        /// 对数据进行分包,并启动升级
        /// </summary>
        /// <param name="data"></param>
        /// <param name="datalen"></param>
        /// <returns></returns>

        bool istoend = false;
        int ccc = 0;
        /// <summary>
        /// 打包并发送数据
        /// </summary>
        /// <param name="databuf"></param>
        /// <param name="datalength"></param>
        private void sendData(byte[] databuf, int datalength)
        {
            if (!serialPort2.IsOpen) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                tx.IsEnabled = true;
                if (rxtxshow.IsChecked == true)
                {
                    // 将字节数组转换为十六进制字符串  
                    string hexString = BitConverter.ToString(databuf, 0, datalength).Replace("-", " ").ToUpper();
                    txlog.AddOne(hexString, "发→◇");
                }

            });

            //});
            try
            {
                serialPort2.Write(databuf, 0, datalength);
                string LogFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "发送数据");
                string hexString = BitConverter.ToString(databuf, 0, datalength).Replace("-", " ").ToUpper();
                Logger.WriteLog(hexString + "," + x1.Value + "," + y1.Value, LogFolderPath);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {

                    Message.Error(ex.Message);
                });
            }
            Thread.Sleep(1);
        }


        int number = 0;
        double pres = 0;

        int timeout = 0;
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (timeout++ > 5)
            {
                timeout = 0;
                tx.IsEnabled = false;
                rx.IsEnabled = false;
            }
            #region 串口识别
            var ports = Common.Common.SearchPort().Distinct().ToList();
            if (comlist.ItemsSource == null || !ports.SequenceEqual(comlist.ItemsSource as IList<string>))
            {
                bool isopen = false;
                if ((comlist.ItemsSource as IList<string>).Count < ports.Count)
                    isopen = true;
                if (!isopen)
                    comlist.SelectionChanged -= comlist_SelectionChanged;
                comlist.ItemsSource = ports;
                if (comlist.Items.Count > 0)
                {
                    comlist.SelectedIndex = comlist.Items.Count - 1;
                }
                if (!isopen)
                    comlist.SelectionChanged += comlist_SelectionChanged;

            }
            if (comlist.SelectedItem == null && comlist.Items.Count > 0)
            {
                comlist.SelectedIndex = comlist.Items.Count - 1;
            }
            #endregion
            timenow.Text = DateTime.Now.ToString("yyyy年MM月dd日 dddd tt hh:mm:ss", CultureInfo.CreateSpecificCulture("zh-CN")); ;
        }

        private int isfirst = 0;
        private bool disposedValue;

        private void comlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isfirst < 1)
            {
                isfirst++;
            }
            else
            {
                //if (comlist.SelectedItem != null)
                //   OpenCloseCom();
            }
        }
        private void ReleaseSerialPort()
        {
            if (serialPort2?.IsOpen == true)
            {
                serialPort2.Close();
            }
            serialPort2?.Dispose();
            serialPort2 = null;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // 创建一个新的进程启动信息对象  
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                // 设置要启动的程序（cmd.exe）  
                FileName = "cmd.exe",
                // 设置要执行的命令，注意这里没有/c参数，因为我们想要看到cmd窗口  
                Arguments = $"/k \"mode {comlist.SelectedValue}\"", // 使用/k参数保持cmd窗口打开  
            };

            // 启动进程执行命令  
            try
            {
                // 创建一个新的进程  
                using (Process process = new Process { StartInfo = startInfo })
                {
                    // 启动进程  
                    process.Start();
                }
            }
            catch (Exception ex)
            {
                Message.Error(ex.Message);
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.XuanFu();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }
                if (timer != null)
                    timer.Stop();
                if (timerhandshake != null)
                    timerhandshake.Stop();
                if (RecDataDeal != null)
                    RecDataDeal.Abort();
                ReleaseSerialPort();
                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~WP()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {

        }

        private void yuanshishuju_Click(object sender, RoutedEventArgs e)
        {
            var data = sender as Button;
            if (data != null)
            {
                if (data.Content.ToString() == "原始数据")
                {
                    data.Content = "解析数据";
                    Common.Common.IsShowSource = true;
                }
                else
                {
                    data.Content = "原始数据";
                    Common.Common.IsShowSource = false;

                }
            }
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void comboBoxFrameType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        public class Module
        {
            public string 序号 { get; set; }
            public string 模块 { get; set; }
            public string 功能 { get; set; }
            public string 方向 { get; set; }
            public string 报头 { get; set; }
            public string 设备 { get; set; }
            public string 功能字节1 { get; set; }
            public string 功能字节2 { get; set; }
            // 如果需要，可以添加更多功能字节字段，例如功能字节2等
            public string 数据长度 { get; set; }
            public string 数据 { get; set; } // 由于数据字段可能包含多个字节，因此使用byte数组存储
            public string 校验 { get; set; } // 校验字段也可能包含多个字节，因此使用byte数组存储（这里仅作为示例，实际校验可能需要根据特定算法计算）
            public string 报尾 { get; set; }
            public string 备注 { get; set; }
            // 可以根据需要添加更多属性或方法
        }
        List<Module> Mbslist = new List<Module>();

        // 将HEX字符串转换为byte数组
        public static byte[] HexStringToByteArray(string hexString)
        {
            // 确保输入字符串长度为偶数
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException("无效的hex长度.");
            }

            // 初始化byte数组，长度为hexString长度的一半
            int byteCount = hexString.Length / 2;
            byte[] byteArray = new byte[byteCount];

            // 遍历hexString，每两个字符转换为一个byte
            for (int i = 0; i < byteCount; i++)
            {
                // 获取当前位置的两个字符
                string hexChar = hexString.Substring(i * 2, 2);
                // 将两个字符转换为byte并存储到byteArray中
                byteArray[i] = Convert.ToByte(hexChar, 16);
            }

            return byteArray;
        }

        byte[] SendCache = new byte[100];

        private void SetCacheByModel(Module data)
        {
            Array.Clear(SendCache, 0, SendCache.Length);
            SendCache[0] = data.报头.ToByte();
            SendCache[1] = data.设备.ToByte();
            SendCache[2] = data.功能字节1.ToByte();
            SendCache[3] = data.功能字节2.ToByte();
            SendCache[4] = data.数据长度.ToByte(); //7+长度等于帧总长度
            byte len = SendCache[4];
            SendCache[5 + len] = 0; //校验
            SendCache[6 + len] = data.报尾.ToByte();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            SendCacheToZhangPengFeiB[0] = 0xA5;
            if (searchtime.SelectedIndex == 0)
                SendCacheToZhangPengFeiB[1] = 0x02;
            else if (searchtime.SelectedIndex == 1)
                SendCacheToZhangPengFeiB[1] = 0x01;
            else if (searchtime.SelectedIndex == 2)
                SendCacheToZhangPengFeiB[1] = 0x04;
            else if (searchtime.SelectedIndex == 3)
                SendCacheToZhangPengFeiB[1] = 0x03;
            // Step 1: Multiply by 1000 and cast to short.
            short angleValue = (short)(x1.Value * 1000);

            // Step 2: Extract high and low bytes.
            SendCacheToZhangPengFeiB[2] = (byte)((angleValue >> 8) & 0xFF); // High byte
            SendCacheToZhangPengFeiB[3] = (byte)(angleValue & 0xFF); // Low byte

            short angleValue1 = (short)(y1.Value * 1000);

            // Step 2: Extract high and low bytes.
            SendCacheToZhangPengFeiB[4] = (byte)((angleValue1 >> 8) & 0xFF); // High byte
            SendCacheToZhangPengFeiB[5] = (byte)(angleValue1 & 0xFF); // Low byte



            SendCacheToZhangPengFeiB[6] = GetSum(SendCacheToZhangPengFeiB);
            for (int i = 0; i < 5; i++)
            {
                sendData(SendCacheToZhangPengFeiB, 7);
                await Task.Delay(80);
            }
        }

        private void ShowNotify()
        {
            HandyControl.Controls.NotifyIcon.ShowBalloonTip("上位机", "上位机", NotifyIconInfoType.Info, "NotifyIconToken");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            SP._SP.ShowBusByMS(SendCache);

        }
        private async void Button_Click_5(object sender, RoutedEventArgs e)
        {

        }

        private void Button_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            sendermodel.IsChecked = true;
        }

        private void Button_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            sendermodel.IsChecked = false;
        }

        private void searchtime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (searchtime.SelectedIndex == 0)
                {
                    if (cmd != null)
                        cmd.Visibility = Visibility.Visible;
                    //if (l1 != null)
                    //    l1.Visibility = Visibility.Visible;
                    //if (l2 != null)
                    //    l2.Visibility = Visibility.Visible;
                    //if (l3 != null)
                    //    l3.Visibility = Visibility.Visible;
                    //if (l4 != null)
                    //    l4.Visibility = Visibility.Visible;

                    if (l5 != null)
                        l5.Visibility = Visibility.Collapsed;
                    if (l6 != null)
                        l6.Visibility = Visibility.Collapsed;

                }
                else if (searchtime.SelectedIndex == 1)
                {
                    cmd.Visibility = Visibility.Visible;

                    l1.Visibility = Visibility.Collapsed;
                    l2.Visibility = Visibility.Collapsed;
                    l3.Visibility = Visibility.Collapsed;
                    l4.Visibility = Visibility.Collapsed;

                    if (l5 != null)
                        l5.Visibility = Visibility.Collapsed;
                    if (l6 != null)
                        l6.Visibility = Visibility.Collapsed;

                }
                else if (searchtime.SelectedIndex == 2)
                {
                    cmd.Visibility = Visibility.Visible;

                    l1.Visibility = Visibility.Collapsed;
                    l2.Visibility = Visibility.Collapsed;
                    l3.Visibility = Visibility.Collapsed;
                    l4.Visibility = Visibility.Collapsed;

                    if (l5 != null)
                        l5.Visibility = Visibility.Collapsed;
                    if (l6 != null)
                        l6.Visibility = Visibility.Collapsed;

                }
                else if (searchtime.SelectedIndex == 3)
                {
                    cmd.Visibility = Visibility.Collapsed;

                    l1.Visibility = Visibility.Collapsed;
                    l2.Visibility = Visibility.Collapsed;
                    l3.Visibility = Visibility.Collapsed;
                    l4.Visibility = Visibility.Collapsed;

                    if (l5 != null)
                        l5.Visibility = Visibility.Visible;
                    if (l6 != null)
                        l6.Visibility = Visibility.Visible;

                }
            }
            catch { }
        }

        private async void l5_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
            {
                if (btn.Name == "l5")
                {
                    for (int i = 0; i < 5; i++)
                    {
                        sendduoji(0);
                        await Task.Delay(50);
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        sendduoji(1);
                        await Task.Delay(50);
                    }
                }
                else if (btn.Name == "l6")
                {
                    for (int i = 0; i < 5; i++)
                    {
                        sendduoji(0);
                        await Task.Delay(50);
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        sendduoji(2);
                        await Task.Delay(50);
                    }
                }

            }
        }

        private void sendduoji(byte type)
        {
            SendCacheToZhangPengFeiB[0] = 0xA5;
            SendCacheToZhangPengFeiB[1] = 0x03;
            SendCacheToZhangPengFeiB[2] = type; // High  if(type==0)
            SendCacheToZhangPengFeiB[3] = 0; // Low byte
            SendCacheToZhangPengFeiB[4] = 0; // High byte
            SendCacheToZhangPengFeiB[5] = 0; // Low byte
            SendCacheToZhangPengFeiB[6] = GetSum(SendCacheToZhangPengFeiB);
            sendData(SendCacheToZhangPengFeiB, 7);
        }
    }

}
