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
            Loaded += FC_Loaded;
        }
        Dictionary<string, List<string>> moduleFunctions;
        private void FC_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            var moduleGroups = Mbslist.GroupBy(m => m.模块).ToList();
            // 创建一个字典来快速查找每个模块下的功能
            moduleFunctions = moduleGroups.ToDictionary(
               g => g.Key,
               g => g.Select(m => m.功能).ToList()
           );
            var moduleNames = moduleGroups.Select(g => g.Key).ToList();
            IDC_EDIT_FC_1.ItemsSource = moduleNames;
            IDC_EDIT_FC_1.SelectedIndex = 0;
        }

        byte HEARTBEAT = 0;
        Module md = null;
        private bool useSetCacheByModel = true;  // 标志变量，用于控制交替执行

        private void timerhandshake_Tick(object sender, EventArgs e)
        {
            if (md == null)
            {
                md = Mbslist.FindLast(o => o.功能 == "心跳  握手");
            }

            if (useSetCacheByModel)
            {
                if (headhe.IsChecked == true)
                    SetCacheByModel(md);
            }
            else
            {
                IDC_EDIT_FC_2_SelectionChanged(null, null);
                var res = GetComboBoxSelectedValues();
                for (int i = 0; i < res.Length; i++)
                {
                    SendCache[5 + i] = res[i];
                }
            }

            // 切换标志变量的状态
            useSetCacheByModel = !useSetCacheByModel;

            // 计算校验和并发送数据
            DSP28335.CalculateChecksum(SendCache);
            //SendCache[SendCache[4] + 7 - 2] =  SendCache.CalculateChecksum();
            sendData(SendCache, 7 + SendCache[4]);
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

                        if (tmpByte == HEAD1)
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
                        else if (tmpByte == HEAD1)  //此处代码起到保护帧头1的下一个字节不被本函数丢掉
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
                        if (G_btList_RecBuf.Count == G_int_RecBufLen && G_btList_RecBuf[G_int_RecBufLen - 1] == 0x79)  //包接收完成
                        {
                            //检查校验和字节
                            if (DSP28335.CheckChecksum(G_btList_RecBuf.ToArray()))
                            {
                                var data = G_btList_RecBuf.ToArray();
                                string hexString = BitConverter.ToString(data).Replace("-", " ").ToUpper();

                                // 使用BitConverter将字节数组转换为float
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (data[2] == 0xA0 && data[3] == 0x00 && data[4] == 0x02 && data.Length == 9) //心跳帧
                                    {
                                        IDC_EDIT_CHECKA_0.Content = headcount++;
                                        IDC_EDIT_CHECKA_1.Content = DSP28335.GetVersionToString(data[5], data[4]);
                                    }
                                    if (data[2] == 0xF0 && data[3] == 0x01 && data[4] == 0x22) //光电数据
                                    {
                                        IDC_EDIT_CHECKA_13.Content = headcount2++;
                                    }
                                    if (data[2] == 0xF0 && data[3] == 0x02 && data[4] == 0x0B) //光电信息
                                    {
                                        IDC_EDIT_CHECKA_14.Content = headcount3++;
                                    }
                                    if (data[2] == 0xF0 && data[3] == 0x03 && data[4] == 0x07) //故障码
                                    {
                                        IDC_EDIT_CHECKA_31.Content = headcount4++;
                                    }
                                    if (data[2] == 0xF0 && data[3] == 0x06) //识别物体
                                    {
                                        IDC_EDIT_CHECKA_32.Content = headcount5++;
                                    }
                                    if (data[2] == 0x70 && data[3] == 0x02) //漂移
                                    {
                                        IDC_EDIT_CHECKB_0.Content = headcount6++;
                                    }
                                    if (data[2] == 0x70 && data[3] == 0xA0) //读取保存
                                    {
                                        IDC_EDIT_CHECKB_2.Content = headcount7++;
                                    }
                                    if (!rx.IsEnabled)
                                        rx.IsEnabled = true;

                                    rxlog.AddOne(hexString, "收←◆");

                                });
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
        private int headcount2 = 0;
        private int headcount3 = 0;
        private int headcount4 = 0;
        private int headcount5 = 0;

        private int headcount6 = 0;
        private int headcount7 = 0;
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
                string hexString = BitConverter.ToString(databuf, 0, datalength).Replace("-", " ").ToUpper();

                txlog.AddOne(hexString, "发→◇");

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
        List<Module> Mbslist = new List<Module>();

        public void LoadData()
        {
            Mbslist.Clear();
            Mbslist.Add(new Module { 序号 = "1", 模块 = "可见光控制", 功能 = "透雾", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x13", 功能字节2 = "0x01", 数据长度 = "0x01", 数据 = "0x00关      0x01开", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "2", 模块 = "可见光控制", 功能 = "变焦", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x10", 功能字节2 = "0x01", 数据长度 = "0x01", 数据 = "0x01（变焦+）  0x02（变焦-）  0x00（变焦停） 0x03（小步进+）0x04（小步进-）0x05（大步进+）0x06（大步进-）", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "3", 模块 = "可见光控制", 功能 = "可见光电子放大", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x14", 功能字节2 = "0x01", 数据长度 = "0x01", 数据 = "放大数值", 校验 = "校验", 报尾 = "0x59", 备注 = "放大数值范围10-40 单位是0.1倍也就是1-4倍" });
            Mbslist.Add(new Module { 序号 = "4", 模块 = "可见光控制", 功能 = "调焦", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x10", 功能字节2 = "0x02", 数据长度 = "0x01", 数据 = "0x00（调焦停）0x01（调焦+）  0x02（调焦-）   0x03（小步进+）0x04（小步进-）0x05（大步进+）0x06（大步进-）", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "5", 模块 = "可见光控制", 功能 = "设置焦位", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x11", 功能字节2 = "0x01", 数据长度 = "0x01", 数据 = "0xXX", 校验 = "校验", 报尾 = "0x59", 备注 = "走到设定焦位点位置" });
            Mbslist.Add(new Module { 序号 = "6", 模块 = "可见光控制", 功能 = "焦位变化", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x11", 功能字节2 = "0x02", 数据长度 = "0x01", 数据 = "0x01焦位+               0x02焦位-", 校验 = "校验", 报尾 = "0x59", 备注 = "走到设定焦位点位置" });
            Mbslist.Add(new Module { 序号 = "7", 模块 = "可见光控制", 功能 = "自动对焦(单次)", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x12", 功能字节2 = "0x01", 数据长度 = "0x01", 数据 = "0x01", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "8", 模块 = "红外控制", 功能 = "变焦", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x20", 功能字节2 = "0x01", 数据长度 = "0x01", 数据 = "0x01（变焦+）  0x02（变焦-）  0x00（变焦停） 0x03（小步进+）0x04（小步进-）0x05（大步进+）0x06（大步进-）", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "9", 模块 = "红外控制", 功能 = "极性  选择", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x23", 功能字节2 = "0x01", 数据长度 = "0x01", 数据 = "0x00白热  0x01黑热", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "10", 模块 = "红外控制", 功能 = "图像  校正", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x23", 功能字节2 = "0x02", 数据长度 = "0x01", 数据 = "0x00挡板  0x01背景", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "11", 模块 = "红外控制", 功能 = "红外电子放大", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x24", 功能字节2 = "0x01", 数据长度 = "0x01", 数据 = "放大数值", 校验 = "校验", 报尾 = "0x59", 备注 = "放大数值范围10-40 单位是0.1倍也就是1-4倍" });
            Mbslist.Add(new Module { 序号 = "12", 模块 = "红外控制", 功能 = "调焦", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x20", 功能字节2 = "0x02", 数据长度 = "0x01", 数据 = "0x01（调焦+）  0x02（调焦-）  0x00（调焦停） 0x03（小步进+）0x04（小步进-）0x05（大步进+）0x06（大步进-）", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "13", 模块 = "红外控制", 功能 = "自动齐焦（单次）", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x22", 功能字节2 = "0x01", 数据长度 = "0x01", 数据 = "0x01", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "14", 模块 = "红外控制", 功能 = "设置焦位", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x21", 功能字节2 = "0x01", 数据长度 = "0x01", 数据 = "0xXX", 校验 = "校验", 报尾 = "0x59", 备注 = "走到设定焦位点位置" });
            Mbslist.Add(new Module { 序号 = "15", 模块 = "红外控制", 功能 = "焦位变化", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x21", 功能字节2 = "0x02", 数据长度 = "0x01", 数据 = "0x01焦位+               0x02焦位-", 校验 = "校验", 报尾 = "0x59", 备注 = "走到设定焦位点位置" });
            Mbslist.Add(new Module { 序号 = "16", 模块 = "红外控制", 功能 = "自动对焦(单次)", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x12", 功能字节2 = "0x01", 数据长度 = "0x01", 数据 = "0x01", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "17", 模块 = "红外控制", 功能 = "自动校正设置", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x23", 功能字节2 = "0x03", 数据长度 = "0x01", 数据 = "0x00(关闭) 0x01(开启)", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "18", 模块 = "红外控制", 功能 = "图像增强", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x23", 功能字节2 = "0x04", 数据长度 = "0x01", 数据 = "0x00(关闭) 0x01(开启)", 校验 = "校验", 报尾 = "0x59", 备注 = "默认开启" });
            Mbslist.Add(new Module { 序号 = "19", 模块 = "激光控制", 功能 = "测距  开关", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x30", 功能字节2 = "0x00", 数据长度 = "0x01", 数据 = "0x00连续停止    0x01连续开始   0x02单次测距", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "20", 模块 = "激光控制", 功能 = "测距  设置", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x30", 功能字节2 = "0x01", 数据长度 = "0x01", 数据 = "0xXX频率数，Hz", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "21", 模块 = "跟踪控制", 功能 = "视频  切换", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x40", 功能字节2 = "0x00", 数据长度 = "0x01", 数据 = "0x00 可见光  0x01 红外  0x02 可见光1", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "22", 模块 = "跟踪控制", 功能 = "波门引导控制", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x40", 功能字节2 = "0x01", 数据长度 = "0x05", 数据 = "D5：0x00锁定；0x01中心引导；0x02坐标引导；0x03编号引导；0x04识别引导,D6：中心引导:引导X坐标对中心的偏差int16_t高八位；坐标引导、识别引导：X坐标uint16_t高八位；编号引导:引导编号；,D7：中心引导:引导X坐标对中心的偏差int16_t低八位；坐标引导、识别引导：X坐标uint16_t低八位；,D8：中心引导:引导Y坐标对中心的偏差int16_t高八位；坐标引导、识别引导：Y坐标uint16_t高八位；,D9：中心引导:引导Y坐标对中心的偏差int16_t低八位；坐标引导、识别引导：Y坐标uint16_t低八位；,D9：中心引导:引导Y坐标对中心的偏差int16_t低八位；坐标引导、识别引导：Y坐标uint16_t低八位；", 校验 = "校验", 报尾 = "0x59", 备注 = "D3:0x01时只进行图像跟踪0xA1时不但进行图像跟踪，同时会激活伺服跟踪" });
            Mbslist.Add(new Module { 序号 = "23", 模块 = "跟踪控制", 功能 = "跟踪  方式", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x40", 功能字节2 = "0x02", 数据长度 = "0x01", 数据 = "0x00质心, 0x01相关", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "24", 模块 = "跟踪控制", 功能 = "质心跟踪目标特性", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x40", 功能字节2 = "0x03", 数据长度 = "0x01", 数据 = "0x00黑目标, 0x01白目标", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "25", 模块 = "跟踪控制", 功能 = "识别  开关", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x40", 功能字节2 = "0x04", 数据长度 = "0x01", 数据 = "0x00识别关     0x01识别开", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "26", 模块 = "跟踪控制", 功能 = "波门大小设置", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x40", 功能字节2 = "0x0A", 数据长度 = "0x05", 数据 = "D5：0x00精确设置；0x01快捷设置；,D6：精确设置:宽度uint16_t高8位；快捷设置:0到9共10个档位；,D7：精确设置:宽度uint16低8位；快捷设置:比例设置0x00 1:1， 0x01 16:9， 0x02 9:16；,D8：精确设置:高度uint16_t高8位；,D9：精确设置:高度uint16_t低8位；", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "27", 模块 = "跟踪控制", 功能 = "波门移动", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x40", 功能字节2 = "0x0B", 数据长度 = "0x02", 数据 = "D5：0x00无动作，0x01上，0x02下， 0x03左，0x04右；,D6：移动的像素数量0~10；", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "28", 模块 = "伺服控制", 功能 = "伺服上下电", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x50", 功能字节2 = "0x00", 数据长度 = "0x01", 数据 = "0x00下电，,0x01上电", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "29", 模块 = "伺服控制", 功能 = "模式设置", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x50", 功能字节2 = "0x01", 数据长度 = "0x01", 数据 = "0x00手动模式，,0x01跟踪模式，,0x02目指模式，,0x03扇扫模式，,0x04归零模式", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "30", 模块 = "伺服控制", 功能 = "伺服手动(百分比)", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x50", 功能字节2 = "0x02", 数据长度 = "0x08", 数据 = "D5~D8：float数据手动方位速度；,D9~D12：float数据手动俯仰速度；", 校验 = "校验", 报尾 = "0x59", 备注 = "方位俯仰速度均为百分比,输入范围-100~100" });
            Mbslist.Add(new Module { 序号 = "31", 模块 = "伺服控制", 功能 = "伺服手动(绝对值)", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x50", 功能字节2 = "0xA2", 数据长度 = "0x08", 数据 = "D5~D8：float数据手动方位速度；,D9~D12：float数据手动俯仰速度；", 校验 = "校验", 报尾 = "0x59", 备注 = "方位俯仰速度均为绝对值,输入范围为-100~100" });
            Mbslist.Add(new Module { 序号 = "32", 模块 = "伺服控制", 功能 = "目指设置", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x50", 功能字节2 = "0x03", 数据长度 = "0x0C", 数据 = "D5~D8：float数据方位目指；,D9~D12：float数据俯仰目指；,D13~D16：float数据距离目指；", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "33", 模块 = "伺服控制", 功能 = "扇扫设置", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x50", 功能字节2 = "0x04", 数据长度 = "0x0D", 数据 = "D5：0x00停止， 0x01方位扇扫开始，0xF1设置方位扇扫,D6~D9：float数据最小位置,D10~D13：float数据最大位置,D14~D17：float数据扇扫速度", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "34", 模块 = "伺服控制", 功能 = "自动跟踪模式", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x51", 功能字节2 = "0x10", 数据长度 = "0x01", 数据 = "D5：0x00关     0x01开", 校验 = "校验", 报尾 = "0x59", 备注 = "如无跟踪目标则自动开启识别后，进行跟踪" });
            Mbslist.Add(new Module { 序号 = "35", 模块 = "伺服控制", 功能 = "零位修正", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x70", 功能字节2 = "0x00", 数据长度 = "0x06", 数据 = "D5：0x00清零， 0x01方位，0x02俯仰；,D6：0x00绝对值， 0x01变化值；,D7~D10：float数据（-360~360度）", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "36", 模块 = "伺服控制", 功能 = "陀螺漂移手动修正", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x70", 功能字节2 = "0x01", 数据长度 = "0x05", 数据 = "D5：0x00清零，0x01方位，0x02俯仰；,D6~D9：陀螺漂移修正值float数据（0~3°/s）", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "37", 模块 = "伺服控制", 功能 = "漂移自动修正", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x70", 功能字节2 = "0x02", 数据长度 = "0x01", 数据 = "0x00强行结束,,0x01开始自动校漂,,0xA0退出校漂模式,,0xA1校漂模式", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "38", 模块 = "伺服控制", 功能 = "自动漂移反馈", 方向 = "设备反馈", 报头 = "0x78", 设备 = "0xEA", 功能字节1 = "0x70", 功能字节2 = "0x02", 数据长度 = "0x01", 数据 = "0x00未开始自动校漂，,0x01自动校漂中，,0x02自动校漂完毕，,0x11伺服未上电，,0x12图像跟踪不正常，,0x13不在跟踪模式下，,0xA0退出校漂模式，,0xA1校漂模式", 校验 = "校验", 报尾 = "0x79", 备注 = null });
            Mbslist.Add(new Module { 序号 = "39", 模块 = "伺服控制", 功能 = "读取保存参数", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0x70", 功能字节2 = "0xA0", 数据长度 = "0x01", 数据 = "0x00读取，,0x01保存", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "40", 模块 = "伺服控制", 功能 = "读取保存参数反馈", 方向 = "设备反馈", 报头 = "0x78", 设备 = "0xEA", 功能字节1 = "0x70", 功能字节2 = "0xA0", 数据长度 = "0x01", 数据 = "0x00读取异常，,0x01读取成功,,0x10保存异常,,0x11保存成功", 校验 = "校验", 报尾 = "0x79", 备注 = null });
            Mbslist.Add(new Module { 序号 = "41", 模块 = "其他报文", 功能 = "心跳  握手", 方向 = "设备接收", 报头 = "0x58", 设备 = "0xEA", 功能字节1 = "0xA0", 功能字节2 = "0x00", 数据长度 = "0x01", 数据 = "0x00", 校验 = "校验", 报尾 = "0x59", 备注 = null });
            Mbslist.Add(new Module { 序号 = "42", 模块 = "其他报文", 功能 = "心跳  反馈", 方向 = "设备反馈", 报头 = "0x78", 设备 = "0xEA", 功能字节1 = "0xA0", 功能字节2 = "0x00", 数据长度 = "0x02", 数据 = "D5：版本号整数(2位)；,D6：版本号小数(2位)", 校验 = "校验", 报尾 = "0x79", 备注 = "心跳反馈数据为软件版本号" });
            Mbslist.Add(new Module { 序号 = "43", 模块 = "反馈      报文", 功能 = "光电  数据", 方向 = "设备反馈", 报头 = "0x78", 设备 = "0xEA", 功能字节1 = "0xF0", 功能字节2 = "0x01", 数据长度 = "0x22", 数据 = "D5~D8: 方位角float数据；,D9~D12：俯仰角float数据；,D13~D16：陀螺方位角速度float数据；,D17~D20：陀螺俯仰角速度float数据；,D21~D24：测角方位角速度float数据；,D25~D28：测角俯仰角速度float数据；,D29：测偏量有效标志；0x00无效，0x01有效；,D30~D31：int16_t左右偏差(单位0.1个像素),D32~D33：int16_t高低偏差(单位0.1个像素),D34：激光数据有效标志；0x00无效，0x01有效；,D35~D38：目标距离float数据；", 校验 = "校验", 报尾 = "0x79", 备注 = "持续反馈，频率可调，默认反馈频率 为：50Hz" });
            Mbslist.Add(new Module { 序号 = "44", 模块 = "反馈      报文", 功能 = "光电  信息", 方向 = "设备反馈", 报头 = "0x78", 设备 = "0xEA", 功能字节1 = "0xF0", 功能字节2 = "0x02", 数据长度 = "0x0B", 数据 = "D5~D6：水平视场uint16_t(单位0.1度)；,D7~D8：垂直视场uint16_t(单位0.1度)；,D9：电子变倍uint8_t(单位0.1)；,D10：可见光模块状态字；,D11：红外模块状态字；,D12：激光测距模块状态字；,D13：跟踪模块状态字；,D14：伺服模块状态字；,D15：其他功能状态字；", 校验 = "校验", 报尾 = "0x79", 备注 = "持续反馈，频率可调，默认反馈频率 为：10Hz" });
            Mbslist.Add(new Module { 序号 = "45", 模块 = "反馈      报文", 功能 = "故障码", 方向 = "设备反馈", 报头 = "0x78", 设备 = "0xEA", 功能字节1 = "0xF0", 功能字节2 = "0x03", 数据长度 = "0x07", 数据 = "D5：可见光故障字反馈；,D6：红外故障字反馈；,D7：激光故障字反馈；,D8：跟踪模块故障字反馈；,D9：伺服模块故障字反馈；,D10：陀螺故障字反馈；,D11：参数装调反馈；", 校验 = "校验", 报尾 = "0x79", 备注 = "持续反馈，频率可调，默认反馈频率 为：10Hz" });
            Mbslist.Add(new Module { 序号 = "46", 模块 = "反馈      报文", 功能 = "识别  物体", 方向 = "设备反馈", 报头 = "0x78", 设备 = "0xEA", 功能字节1 = "0xF0", 功能字节2 = "0x06", 数据长度 = "11*N", 数据 = "D(5+11×(N-1))：目标编号；,D(6+11×(N-1))：目标类型；,D(7+11×(N-1))：置信度；,D(8+11×(N-1))~ D(9+11×(N-1))：目标左上角的x坐标uint16_t；,D(10+11×(N-1))~ D(11+11×(N-1))：目标左上角的y坐标uint16_t；,D(12+11×(N-1))~ D(13+11×(N-1))：目标宽度uint16_t；,D(14+11×(N-1))~ D(15+11×(N-1))：目标高度uint16_t；", 校验 = "校验", 报尾 = "0x79", 备注 = "N为识别到的目标数，从1开始。" });
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

        private void IDC_EDIT_FC_1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (moduleFunctions.ContainsKey(IDC_EDIT_FC_1.SelectedValue.ToString()))
            {
                IDC_EDIT_FC_2.ItemsSource = null;
                IDC_EDIT_FC_2.ItemsSource = moduleFunctions[IDC_EDIT_FC_1.SelectedValue.ToString()];
                IDC_EDIT_FC_2.SelectedIndex = 0;
            }
            else
            {
                IDC_EDIT_FC_2.ItemsSource = null;
            }
        }
        byte[] SendCache = new byte[100];
        private void IDC_EDIT_FC_2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IDC_EDIT_FC_2.SelectedValue == null || IDC_EDIT_FC_1.SelectedValue == null)
                return;
            var data = Mbslist.FindLast(o => o.模块 == IDC_EDIT_FC_1.SelectedValue.ToString() && o.功能 == IDC_EDIT_FC_2.SelectedValue.ToString());
            if (sender != null)
            {
                //IDC_EDIT_FC_3.Content = data.方向;
                IDC_EDIT_FC_4.Content = data.备注;
                AddComboBoxes(data.数据长度.ToByte());
                IDC_EDIT_FC_6.Content = data.数据;
            }
            SetCacheByModel(data);
        }
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

        private void AddComboBoxes(byte counts)
        {
            // 清除之前添加的 ComboBox
            IDC_EDIT_FC_5.Children.Clear();
            // 定义 ComboBox 的数据源
            List<string> items = Enumerable.Range(1, counts).Select(i => $"0x{i:X2}").ToList();
            items.Add("0x00"); // 在列表末尾添加 0x00
                               // 动态添加 ComboBox
            for (int i = 0; i < counts; i++) // 假设你要添加 5 个 ComboBox
            {
                ComboBox comboBox = new ComboBox
                {
                    SelectedIndex = 0, // 默认选中第一个项
                    IsEditable = true,
                    ItemsSource = items,
                    Tag = $"{i}", // 设置 Tag 属性以区分不同的 ComboBox
                    Width = 100 // 你可以根据需要设置宽度
                };

                IDC_EDIT_FC_5.Children.Add(comboBox);
            }
        }


        private byte[] GetComboBoxSelectedValues()
        {
            if (IDC_EDIT_FC_5.Children.Count == 0) return [];
            var selectedValues = new byte[IDC_EDIT_FC_5.Children.Count];
            foreach (var child in IDC_EDIT_FC_5.Children)
            {
                if (child is ComboBox comboBox)
                {
                    if (int.TryParse(comboBox.Tag.ToString(), out int tag))

                        if (comboBox.Text != null)
                        {
                            if (comboBox.Text.Length == 4 && comboBox.Text.Contains("0x"))
                                selectedValues[tag] = comboBox.Text.ToByte();
                        }
                }
            }

            return selectedValues;
        }
    }

}
