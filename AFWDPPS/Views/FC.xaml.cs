using AFWDPP.Common;

using HandyControl.Data;
using LL2024.Algorithms.UpdateDSP;
using MiniExcelLibs;
using Rubyer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using static AFWDPP.Common.Common;
using Application = System.Windows.Application;

namespace AFWDPP.Views
{
    /// <summary>
    /// FC.xaml 的交互逻辑
    /// </summary>
    public partial class FC : UserControl, IDisposable
    {
        #region 全局变量
        public System.IO.Ports.SerialPort serialPort2;


        public Thread RecDataDeal;
        DispatcherTimer timerhandshake;
        DispatcherTimer timer;
        #endregion
        public FC()
        {
            InitializeComponent();

            DSP28335.SetDLE_STX_ETX();
            // this.DataContext = App.Current.Services.GetService<DescriptionViewModel>();
            botelv.ItemsSource = new string[] { "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };

            botelv.SelectedIndex = 7;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
            //握手定时器
            timerhandshake = new DispatcherTimer();
            timerhandshake.Interval = TimeSpan.FromMilliseconds(20);
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
            this.serialPort2.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort1_DataReceived);
            Loaded += FC_Loaded;
        }

        private void FC_Loaded(object sender, RoutedEventArgs e)
        {
          //  List<Module> infoList = MiniExcel.Query<Module>("C:\\Users\\liu\\Desktop\\222222222222222.xlsx").ToList();
        }

        byte HEARTBEAT = 0;
        private void timerhandshake_Tick(object sender, EventArgs e)
        {

            //testdata1.ToByte(IDC_EDIT_FC_0);
            //testdata1.ToByte(IDC_EDIT_FC_1);
            //testdata1.ToByte(IDC_EDIT_FC_2);
            //testdata1.ToByte(IDC_EDIT_FC_17_18);

            if (HEARTBEAT > 255)
            {
                HEARTBEAT = 0;
            }
            testdata1[81] = HEARTBEAT++;
            GetSPsum(testdata1, testdata1.Length);
            sendData(testdata1, testdata1.Length);
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
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            int bytesToRead = sp.BytesToRead;
            byte[] buffer = new byte[bytesToRead];

            // 读取数据到缓冲区  
            int nbrDataRead = sp.Read(buffer, 0, bytesToRead);
            if (nbrDataRead == 0)
                return;

            if (Common.Common.CheckSPsum(buffer) && buffer.Length == 73)
            {
                string hexString = BitConverter.ToString(buffer).Replace("-", " ").ToUpper();

                // 使用BitConverter将字节数组转换为float
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!rx.IsEnabled)
                        rx.IsEnabled = true;

                    rxlog.AddOne(hexString, "收←◆");

                });
            }
        }
        bool istoendd = false;
        public static (byte Hx, byte Lx) ConvertAngleToBytes(short angle)
        {
            // 假设 X 轴和 Y 轴的最大正值分别为 20.5° 和 30.5°，对应的指令值为 20500 和 30500
            // 但由于我们只关心绝对值，并且知道要乘以 1000，所以这里直接使用 20500 和 30500 的最大值 30500 来判断是否需要处理溢出（尽管在这个特定例子中不会溢出）
            // 实际上，由于我们分别处理 X 轴和 Y 轴，应该为每个轴设置不同的限制，但这里为了简化，我们假设输入是合法的

            // 将角度乘以 1000（注意：这里假设输入的角度已经在允许范围内）
            short commandValue = (short)(angle);

            // 对于 X 轴，范围应该是 -20500 到 20500
            // 对于 Y 轴，范围应该是 -30500 到 30500
            // 但由于我们在这个方法中不区分轴，只是进行转换，所以这里不进行检查
            // 如果需要区分轴并进行检查，可以在调用此方法之前或在方法内部添加额外的逻辑

            // 处理负数（转换为补码，即二进制的反码加一）
            //if (commandValue < 0)
            //{
            //    commandValue = (short)~commandValue; // 反码计算
            //}

            // 注意：这里我们假设转换后的值不会超过一个字节的范围（对于高字节来说是不可能的，因为我们是将整数分为两个字节）
            // 但实际上，由于我们已经将角度乘以了 1000，所以转换后的值可能会超过一个字节（0-255）的范围
            // 因此，我们正确地将其分为高字节和低字节

            // 将整数拆分为高字节和低字节
            byte Hx = (byte)((commandValue >> 8) & 0xFF); // 取高8位
            byte Lx = (byte)(commandValue & 0xFF);        // 取低8位

            return (Hx, Lx);
        }
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
        //打开关闭串口
        private async void OpenCloseCom()
        {
            try
            {
                //根据当前串口属性来判断是否打开
                if (serialPort2.IsOpen)
                {
                    if (UpdateFlag == true)
                    {
                        if (await MessageBoxR.Warning("正在进行固件升级，关闭串口会导致固件升级失败，是否要关闭？", button: MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            //// 停止固件升级
                            UpdateFlag = false;


                            ////串口已经处于打开状态
                            serialPort2.Close();    //关闭串口
                            comlist.IsEnabled = true;
                            botelv.IsEnabled = true;
                            RecDataDeal.Abort();
                        }
                        else
                        {
                            openclosecom.IsChecked = true;
                            return;
                        }
                    }
                    else
                    {
                        ////串口已经处于打开状态
                        serialPort2.Close();    //关闭串口
                        comlist.IsEnabled = true;
                        botelv.IsEnabled = true;
                        RecDataDeal.Abort();
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
                    serialPort2.Open();//打开串口
                    notifytimes = 0;
                    ////创建数据处理线程
                    RecDataDeal = new Thread(new ThreadStart(ProtocolParsing));
                    RecDataDeal.IsBackground = true;
                    RecDataDeal.Start();

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
            openclosecom.IsChecked = serialPort2.IsOpen;
            if (serialPort2.IsOpen)
                Message.Success(comlist.SelectedItem as string + "连接成功！");
            else
                Message.Warning(comlist.SelectedItem as string + "已断开连接！");

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
            HandyControl.Controls.NotifyIcon.ShowBalloonTip("上位机", "上位机", NotifyIconInfoType.Info, "NotifyIconToken");

        }



        /// <summary>
        /// 对数据进行分包,并启动升级
        /// </summary>
        /// <param name="data"></param>
        /// <param name="datalen"></param>
        /// <returns></returns>


        bool istoend = false;
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

                // 将字节数组转换为十六进制字符串  
                string hexString = BitConverter.ToString(databuf).Replace("-", " ").ToUpper();

                txlog.AddOne(hexString, "发→◇");

            });

            //});
            try
            {
                serialPort2.Write(databuf, 0, databuf.Length);

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
                if (comlist.SelectedItem != null)
                    OpenCloseCom();
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
                if (RecDataDeal != null)
                    RecDataDeal.Abort();
                ReleaseSerialPort();
                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~FC()
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
    }
}
