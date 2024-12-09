using AFWDPP.Common;
using HandyControl.Data;
using LL2024.Algorithms.UpdateDSP;
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
    public partial class ZZ : UserControl, IDisposable
    {
        #region 全局变量
        public System.IO.Ports.SerialPort serialPort2;


        public Thread RecDataDeal;
        DispatcherTimer timerhandshake;
        DispatcherTimer timer;
        public static ZZ _ZZ;
        #endregion
        public ZZ()
        {
            InitializeComponent();

            DSP28335.SetDLE_STX_ETX();
            // this.DataContext = App.Current.Services.GetService<DescriptionViewModel>();
            botelv.ItemsSource = new string[] { "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };

            botelv.SelectedIndex = 6;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
            //握手定时器
            timerhandshake = new DispatcherTimer();
            timerhandshake.Interval = TimeSpan.FromMilliseconds(100);
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
            Loaded += ZZ_Loaded;
            _ZZ = this;
        }
        Dictionary<string, List<string>> moduleFunctions;
        private void ZZ_Loaded(object sender, RoutedEventArgs e)
        {

        }

        byte HEARTBEAT = 0;
        Module md = null;
        private bool useSetCacheByModel = true;  // 标志变量，用于控制交替执行
        private byte heda = 0;
        private void timerhandshake_Tick(object sender, EventArgs e)
        {
            if (sendermodel.IsChecked == true)
            {
                Button_Click(null, null);
            }
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

        public void ShowBusByMS(byte[] data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (data[2] == 0x13 && data[3] == 0x01 && data[4] == 0x01)  //1
                {
                    IDC_EDIT_CHECKA_1.Content = countshead[0]++;
                    IDC_EDIT_CHECKA_1_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x10 && data[3] == 0x01 && data[4] == 0x01) //2
                {
                    IDC_EDIT_CHECKA_2.Content = countshead[1]++;
                    IDC_EDIT_CHECKA_2_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x14 && data[3] == 0x01 && data[4] == 0x01) //3
                {
                    IDC_EDIT_CHECKA_3.Content = countshead[2]++;
                    IDC_EDIT_CHECKA_3_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x10 && data[3] == 0x02 && data[4] == 0x01) //4
                {
                    IDC_EDIT_CHECKA_4.Content = countshead[3]++;
                    IDC_EDIT_CHECKA_4_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x11 && data[3] == 0x01 && data[4] == 0x01) //5
                {
                    IDC_EDIT_CHECKA_5.Content = countshead[4]++;
                    IDC_EDIT_CHECKA_5_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x11 && data[3] == 0x02 && data[4] == 0x01) //6
                {
                    IDC_EDIT_CHECKA_6.Content = countshead[5]++;
                    IDC_EDIT_CHECKA_6_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x12 && data[3] == 0x01 && data[4] == 0x01) //7
                {
                    IDC_EDIT_CHECKA_7.Content = countshead[6]++;
                    IDC_EDIT_CHECKA_7_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x30 && data[3] == 0x00 && data[4] == 0x01) //8
                {
                    IDC_EDIT_CHECKA_8.Content = countshead[7]++;
                    IDC_EDIT_CHECKA_8_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x30 && data[3] == 0x01 && data[4] == 0x01) //9
                {
                    IDC_EDIT_CHECKA_9.Content = countshead[8]++;
                    IDC_EDIT_CHECKA_9_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x40 && data[3] == 0x00 && data[4] == 0x01) //10
                {
                    IDC_EDIT_CHECKA_10.Content = countshead[9]++;
                    IDC_EDIT_CHECKA_10_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x40 && data[3] == 0x01 && data[4] == 0x05) //11
                {
                    IDC_EDIT_CHECKA_11.Content = countshead[10]++;
                    IDC_EDIT_CHECKA_11_3.Content = data[3].ToString("X2");
                    IDC_EDIT_CHECKA_11_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x40 && data[3] == 0x02 && data[4] == 0x01) //12
                {
                    IDC_EDIT_CHECKA_12.Content = countshead[11]++;
                    IDC_EDIT_CHECKA_12_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x40 && data[3] == 0x03 && data[4] == 0x01) //13
                {
                    IDC_EDIT_CHECKA_13.Content = countshead[12]++;
                    IDC_EDIT_CHECKA_13_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x40 && data[3] == 0x04 && data[4] == 0x01) //14
                {
                    IDC_EDIT_CHECKA_14.Content = countshead[13]++;
                    IDC_EDIT_CHECKA_14_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x40 && data[3] == 0x0A && data[4] == 0x05) //15
                {
                    IDC_EDIT_CHECKA_15.Content = countshead[14]++;
                    IDC_EDIT_CHECKA_15_5.Content = data[5].ToString("X2");
                }
                if (data[2] == 0x40 && data[3] == 0x0B && data[4] == 0x02) //16
                {
                    IDC_EDIT_CHECKA_16.Content = countshead[15]++;
                    IDC_EDIT_CHECKA_16_5.Content = data[5].ToString("X2");
                }
                var hexString = BitConverter.ToString(data).Replace("-", " ").ToUpper();
                if (!rx.IsEnabled)
                    rx.IsEnabled = true;
                if (rxtxshow.IsChecked == true)
                    rxlog.AddOne(hexString, "收←◆");

            });

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
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            int bytesToRead = sp.BytesToRead;
            byte[] buffer = new byte[bytesToRead];

            // 读取数据到缓冲区  
            int nbrDataRead = sp.Read(buffer, 0, bytesToRead);
            if (nbrDataRead == 0)
                return;


            G_btList_RecBuf_R.Clear();
            foreach (byte tmpByte in buffer)
            {
                switch (G_int_ComStatus)
                {
                    case (int)enum_ComStatus.COM_STATUS_HEAD1:
                        G_btList_RecBuf.Clear();

                        if (tmpByte == 0x58)
                        {
                            // tmpHEAD1 = tmpByte;
                            //切换协议解析状态
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD2;
                            G_btList_RecBuf.Add(tmpByte);
                        }
                        break;

                    case (int)enum_ComStatus.COM_STATUS_HEAD2:
                        if (tmpByte == 0xEA)
                        {
                            //切换协议解析状态
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_DEVICE_FC1;
                            G_btList_RecBuf.Add(tmpByte);
                        }
                        else if (tmpByte == 0x58)  //此处代码起到保护帧头1的下一个字节不被本函数丢掉
                        {
                            G_btList_RecBuf.Clear();
                            G_btList_RecBuf.Add(tmpByte);
                        }
                        else
                        {
                            //切换协议解析状态
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD1;
                        }
                        break;

                    //case (int)enum_ComStatus.COM_STATUS_DEVICE_ID: //设备ID
                    //    G_btList_RecBuf.Add(tmpByte); //测试上位机不过滤设备ID
                    //    G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_DEVICE_FC1;
                    //    break;
                    case (int)enum_ComStatus.COM_STATUS_DEVICE_FC1: //设备功能字节1
                        G_btList_RecBuf.Add(tmpByte);
                        G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_DEVICE_FC2;
                        break;
                    case (int)enum_ComStatus.COM_STATUS_DEVICE_FC2: //设备功能字节2
                        G_btList_RecBuf.Add(tmpByte);
                        G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_LEN;
                        break;
                    case (int)enum_ComStatus.COM_STATUS_LEN://获取数据包长度
                        G_btList_RecBuf.Add(tmpByte);
                        G_int_RecBufLen = tmpByte + 7;
                        G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_DATA;
                        break;

                    case (int)enum_ComStatus.COM_STATUS_DATA:
                        G_btList_RecBuf.Add(tmpByte);
                        //数据接收完成后的有效性判断
                        if (G_btList_RecBuf.Count == G_int_RecBufLen && G_btList_RecBuf[G_int_RecBufLen - 1] == 0x59)  //包接收完成
                        {
                            //检查校验和字节
                            if (DSP28335.CheckChecksum(G_btList_RecBuf.ToArray()))
                            {
                                var data = G_btList_RecBuf.ToArray();
                                ShowBusByMS(data);
                                // 使用BitConverter将字节数组转换为float
                            }
                            else
                            {
                                G_btList_RecBuf.Clear();
                                //string str_ErrorInfo = "“";
                                //foreach (byte tmpbt in G_btList_RecBuf)
                                //{
                                //    str_ErrorInfo += tmpbt.ToString("X2") + " ";
                                //}
                                //str_ErrorInfo += "”帧校验和错误！";

                            }

                            //切换协议解析状态
                            G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD1;

                        }

                        //数据包长度超限检查
                        if (G_btList_RecBuf.Count >= 512)
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



            //  var data = DSP28335.GetRecBufData_422(buffer, 0xEA);
            //  if (data == null || data.Count == 0) return;
            //if (buffer.Length > 4 && buffer.Length == buffer[4] + 7)
            //{
            //    var gres = buffer.CalculateChecksum();
            //    var res = buffer[buffer[4] + 7 - 2];
            //    if (gres == res)
            //    {


            //  }
            // }
        }
        private int headcount = 0;
        int[] countshead = new int[20];
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
                if (RecDataDeal != null)
                    RecDataDeal.Abort();
                ReleaseSerialPort();
                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~ZZ()
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

        private void ShowNotify()
        {
            HandyControl.Controls.NotifyIcon.ShowBalloonTip("上位机", "上位机", NotifyIconInfoType.Info, "NotifyIconToken");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (IDC_EDIT_FC_31.Text.ToByte() > 10) return;

            byte[] buffer3 = new byte[128];
            buffer3[0] = 0x78;
            buffer3[1] = 0xEA;
            buffer3[2] = 0xF0;
            buffer3[3] = 0x06;

            buffer3.String2ToBytes(IDC_EDIT_FC_6_7.Text, 6);
            buffer3.String2ToBytes(IDC_EDIT_FC_8_9.Text, 8);
            buffer3.String2ToBytes(IDC_EDIT_FC_10_11.Text, 10);
            buffer3.String2ToBytes(IDC_EDIT_FC_12_13.Text, 12);
            buffer3[14] = IDC_EDIT_FC_14.Text.ToByte();
            buffer3[15] = IDC_EDIT_FC_15.Text.ToByte();
            buffer3[16] = IDC_EDIT_FC_16.Text.ToByte();
            buffer3[17] = IDC_EDIT_FC_17.Text.ToByte();
            buffer3[18] = IDC_EDIT_FC_18.Text.ToByte();
            buffer3[19] = IDC_EDIT_FC_19.Text.ToByte();
            buffer3[20] = IDC_EDIT_FC_20.Text.ToByte();
            buffer3[21] = IDC_EDIT_FC_21.Text.ToByte();
            buffer3.String2ToBytes(IDC_EDIT_FC_22_23.Text, 22);
            buffer3.String2ToBytes(IDC_EDIT_FC_24_25.Text, 24);
            buffer3[26] = IDC_EDIT_FC_26.Text.ToByte();
            buffer3.FloatStringToBytes(IDC_EDIT_FC_27_30.Text, 27);
            buffer3[31] = IDC_EDIT_FC_31.Text.ToByte();
            int len = 34 + buffer3[31] * 11;
            buffer3[4] = (byte)(len - 7);

            for (int i = 0; i < buffer3[31] * 11; i++)
            {
                buffer3[32 + i] = (byte)i;
            }

            if (heda > 255) heda = 0;
            buffer3[len - 2] = heda++;
            Application.Current.Dispatcher.Invoke(() =>
            {
                IDC_EDIT_FC_5.Text = buffer3[37].ToString();
            });
            DSP28335.CalculateChecksum(buffer3);
            buffer3[len - 1] = 0x79;

            sendData(buffer3, len);
        }
    }

}
