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
using System.Windows.Threading;

namespace AFWDPP.Views
{
    /// <summary>
    /// FC.xaml 的交互逻辑
    /// </summary>
    public partial class SP : UserControl, IDisposable
    {
        #region 全局变量
        public System.IO.Ports.SerialPort serialPort2;
        public static byte ChannelID;
        private const int BINDATA_PACK_LEN = 512;
        int BinPackNum;//包个数
        int BinPackOrder;//第BinPackOrder个包

        volatile int ProgState;//程序状态
        const int PROGSTATE_UPDATE_IDEL = 0;
        const int PROGSTATE_UPDATE_START = 1;
        const int PROGSTATE_UPDATE_LOAD = 2;
        const int PROGSTATE_UPDATE_FINAL = 3;


        const byte PROTOCOL_CMD_COMACK = 0x02;
        const byte PROTOCOL_CMD_STARTUPDATE = 0x81;
        const byte PROTOCOL_CMD_BINDATA = 0x82;

        int BinFileLen;

        bool UpdateFlag;
        const int CIPHER_LOCAL_START = 30;
        const int DATA_LOCAL_START = 62;
        byte[] BinFileData = new byte[2 * 1024 * 1024];
        ushort Bin_CheckA, Bin_CheckB;
        ushort[] Data = new ushort[32];
        byte[] Ciphers = new byte[16];

        public Thread RecDataDeal;
        DispatcherTimer timerhandshake;
        DispatcherTimer timer;
        #endregion
        public SP()
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
            timerhandshake.Interval = TimeSpan.FromMilliseconds(200);
            // timerhandshake.IsEnabled = false;
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

        }
        byte[] testdata1 = new byte[83];
        byte[] d1 = { 0x05, 0x06, 0x00, 0x0d, 0x00, 0x01, 0xD8, 0x4D };
        byte[] d2 = { 0x05, 0x06, 0x00, 0x0E, 0x00, 0x05, 0x29, 0x8E };
        byte[] d3 = { 0x05, 0x03, 0xA0, 0x00, 0x00, 0x00, 0x66, 0x4E };
        byte[] d4 = { 0x07, 0x03, 0x41, 0x3C, 0x0B, 0x00, 0x21 };
        byte[] d5 = { 0x07, 0x03, 0x41, 0x4F, 0x0B, 0x00, 0x21 };
        byte[] d6 = { 0x07, 0x03, 0x34, 0x00, 0x04, 0x00, 0x21 };
        byte[] c1 = { 0x05, 0x06, 0x00, 0x0d, 0x00, 0x01, 0xD8, 0x4D };

        byte[] c2 = { 0x05, 0x06, 0x00, 0x0E, 0x00, 0x05, 0x29, 0x8E };
        byte[] c3 = { 0x05, 0x03, 0x2B, 0x35, 0x35, 0x2C, 0x30, 0x2C, 0x30, 0x2C, 0x30, 0x2C, 0x4F, 0x4B, 0x2C, 0x35, 0x39, 0x39, 0x35, 0x34, 0x2C, 0x30, 0x2C, 0x30, 0x2C, 0x30, 0x2C, 0x30, 0x2C, 0x30, 0x2C, 0x30, 0x2C, 0x30, 0x2C, 0x30, 0x2C, 0x30, 0x2C, 0x30, 0x2C, 0x32, 0x34, 0x36, 0x30, 0x39, 0x37, 0x25 };
        byte[] c4 = { 0x07, 0x03, 0x16, 0xff, 0xf1, 0xff, 0xf2, 0xff, 0xf3, 0xff, 0xf4, 0xff, 0xf5, 0xff, 0xf6, 0xff, 0xf7, 0xff, 0xf8, 0xff, 0xf9, 0xff, 0xfa, 0xff, 0xfb, 0x37, 0x25 };
        byte[] c5 = { 0x07, 0x03, 0x16, 0xff, 0xf1, 0xff, 0xf2, 0xff, 0xf3, 0xff, 0xf4, 0xff, 0xf5, 0xff, 0xf6, 0xff, 0xf7, 0xff, 0xf8, 0xff, 0xf9, 0xff, 0xfa, 0xff, 0xfb, 0x37, 0x25 };
        byte[] c6 = { 0x07, 0x03, 0x08, 0xff, 0xf1, 0xff, 0xf2, 0xff, 0xf3, 0xff, 0xf4, 0x37, 0x25 };
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            int bytesToRead = sp.BytesToRead;
            byte[] buffer = new byte[bytesToRead];

            // 读取数据到缓冲区  
            int nbrDataRead = sp.Read(buffer, 0, bytesToRead);
            if (nbrDataRead == 0)
                return;

            //// 将字节数组转换为十六进制字符串  
            //if (buffer.SequenceEqual(d1))
            //{
            //    sendData(c1, c1.Length);

            //}
            //else if (buffer.SequenceEqual(d2))
            //{
            //    sendData(c2, c2.Length);

            //}
            //else if (buffer.SequenceEqual(d3))
            //{
            //    sendData(c3, c3.Length);

            //}
            //else if (buffer.SequenceEqual(d4))
            //{
            //    sendData(c4, c4.Length);

            //}
            //else if (buffer.SequenceEqual(d5))
            //{
            //    sendData(c5, c5.Length);

            //}
            //else if (buffer.SequenceEqual(d6))
            //{
            //    sendData(c6, c6.Length);

            //}
            testdata1[0] = 0xEB;
            testdata1[1] = 0x90;
            testdata1[3] = 0x13;
            byte[] buffer3 = new byte[7];
            buffer3[0] = 0xA5;
            buffer3[1] = 0x02;

            // 示例用法
            short xAxisAngle = 1500;  // X 轴示例角度
            short yAxisAngle = -2500; // Y 轴示例角度

            // 调用方法并获取结果
            var (Hx_X, Lx_X) = ConvertAngleToBytes(xAxisAngle);
            var (Hx_Y, Lx_Y) = ConvertAngleToBytes(yAxisAngle);

            buffer3[2] = Hx_X;
            buffer3[3] = Lx_X;
            buffer3[4] = Hx_Y;
            buffer3[5] = Lx_Y;
            GetSPsum(buffer3, 7);
            sendData(buffer3, 7);
            string hexString = BitConverter.ToString(buffer).Replace("-", " ").ToUpper();
            string strs = isrxcheck ? "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "]收←◆" : "";

            Application.Current.Dispatcher.Invoke(() =>
            {
                rx.IsEnabled = true;
                if (txlog.LineCount > 500)
                    txlog.Clear();
                txlog.AppendText(strs);

                txlog.AppendText(" " + hexString);
                txlog.AppendText("\r\n");
                // 确保滚动到底部  
                txlog.ScrollToEnd();
            });
        }
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
                            UpdateStop();
                            AddTextToLog("固件升级功能强制退出！\r\n");
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
        /// 对接收数据进行处理
        /// </summary>
        public void Implement(byte[] DataBuf)
        {
            //DataBuf[0]是ChannelID
            byte cmd = DataBuf[1];
            string str;

            // 一般命令处理
            if (cmd == 0x04)
            {
                // RecvDevInfo(DataBuf, DataBuf.Length);
            }
            // 固件升级
            switch (ProgState)
            {
                case PROGSTATE_UPDATE_IDEL:// 固件升级无效状态
                    break;
                case PROGSTATE_UPDATE_START:
                    if (cmd == PROTOCOL_CMD_COMACK && DataBuf[2] == PROTOCOL_CMD_STARTUPDATE)
                    {
                        if (DataBuf[3] == 0)
                        {
                            if (ComfirTimes-- <= 0)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    timerhandshake.Stop();
                                });
                                ComfirTimes = 3;
                                // 下发第一包数据
                                AddTextToLog("握手成功，等待发送第一包数据");
                                needFlashTime = new Random().Next(14, 26);
                                AddTextToLog("DSP擦除FLASH中，预估（15秒）....".Replace("15", needFlashTime.ToString()));
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    tx.Content = "握手已停止，等待设备准备完成后回应中...";
                                });
                                issend = true;
                                SendPackBinData(BinPackOrder);
                                ProgState = PROGSTATE_UPDATE_LOAD;
                            }
                        }
                        else
                        {
                            str = GetCommAckResult(DataBuf[3]);
                            str += "，退出固件升级";
                            AddTextToLog(str);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Message.Success(str);
                            });

                            UpdateStop();
                        }
                    }
                    break;
                case PROGSTATE_UPDATE_LOAD:
                    if (!(cmd == PROTOCOL_CMD_COMACK && DataBuf[2] == PROTOCOL_CMD_BINDATA))
                    {
                        str = GetCommAckResult(DataBuf[3]);
                        str += "，退出固件升级。";

                        UpdateStop();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            AddTextToLog(str);
                        });
                        break;
                    }
                    issend = false;
                    // 获得包序号
                    str = string.Format("收到{0:d}/{1:d}包应答结果：{2:d}。", BinPackOrder + 1, BinPackNum, DataBuf[3]);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AddTextToLog(str);
                    });
                    if (DataBuf[3] != 0)
                    {
                        str = GetCommAckResult(DataBuf[3]);
                        str += "，退出固件升级。";

                        UpdateStop();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            AddTextToLog(str);
                        });
                        break;
                    }
                    // 下发下一包数据
                    BinPackOrder = (BinPackOrder + 1) % BinPackNum;
                    // 设置进度条
                    Application.Current.Dispatcher.Invoke(() =>
                    {

                    });
                    // 判断是不是最后一包数据,是最后一包数据则等待报告文件校验字节
                    if ((BinPackOrder + 1) >= BinPackNum)
                    {
                        ProgState = PROGSTATE_UPDATE_FINAL;
                    }

                    SendPackBinData(BinPackOrder);
                    break;
                case PROGSTATE_UPDATE_FINAL:
                    if (!(cmd == PROTOCOL_CMD_COMACK && DataBuf[2] == PROTOCOL_CMD_BINDATA))
                    {
                        str = GetCommAckResult(DataBuf[3]);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MainWindow.Instance.SetTitle(str);
                        });
                        str += "，退出固件升级。";

                        UpdateStop();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            AddTextToLog(str);
                        });
                        break;
                    }

                    str = string.Format("固件包下发成功，收到{0:d}/{1:d}包应答结果：{2:d}。", BinPackOrder + 1, BinPackNum, DataBuf[3]);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AddTextToLog(str);
                    });
                    if (DataBuf[3] == 5)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            issend = false;
                        });
                    }
                    str = GetCommAckResult(DataBuf[3]);
                    str += "，退出固件升级。";
                    UpdateStop();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AddTextToLog(str);
                    });
                    ProgState = PROGSTATE_UPDATE_IDEL;
                    break;
                default:
                    break;
            }
        }
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
        public void SendPackBinData(int packorder)
        {
            var data = DSP28335.SendPackBinData(BinFileData, ChannelID, packorder, BinFileLen, BINDATA_PACK_LEN);
            sendData(data, data.Length);
            Application.Current.Dispatcher.Invoke(() =>
            {
                tx.Content = "下发固件包中...";
            });

        }
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
        #region 加载固件
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Bin_CheckA = 0;
                Bin_CheckB = 0;
                BinFileData = new byte[2 * 1024 * 1024];
                var openFileDialog1 = new Microsoft.Win32.OpenFileDialog();
                openFileDialog1.Filter = "二进制文件|*.bin";
                openFileDialog1.Title = "Load File";

                if (openFileDialog1.ShowDialog() == true)
                {
                    //string filename = Path.GetFileName(openFileDialog1.FileName);//只取文件名
                    var filepath = openFileDialog1.FileName;//取全路径文件名
                    BinFileLen = DSP28335.LoadBinFile(BinFileData, filepath);
                    // 初始化CheckA和CheckB和代码长度
                    DSP28335.SetHexLength(BinFileData, BinFileLen);
                    var (tempA, tempB) = DSP28335.GetBinCheckAAndCheckB(BinFileData, BinFileLen);
                    Bin_CheckA = tempA; Bin_CheckB = tempB;
                    DSP28335.SetHexCheckAB(BinFileData, Bin_CheckA, Bin_CheckB);
                    // 显示文件信息



                }
            }
            catch (Exception ex)
            {
                Message.Error(ex.Message, 10000, true);
                UpdateFlag = false;
                UpdateStop();
            }
        }
        #endregion
        #region 开始固件升级
        private void StartToUpdate()
        {


        }
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
        }
        /// <summary>
        /// 终止固件升级
        /// </summary>
        public void UpdateStop()
        {
            issend = false;
            BinPackOrder = 0;
            timerhandshake.Stop();
            //文件加载按钮
            Application.Current.Dispatcher.Invoke(() =>
            {

                tx.Content = "发送";
            });
            // 停止固件更新
            UpdateFlag = false;
            //Array.Clear(pData, 0, pData.Length);

            BinPackNum = 0;
            ProgState = PROGSTATE_UPDATE_IDEL;
        }
        /// <summary>
        /// 对数据进行分包,并启动升级
        /// </summary>
        /// <param name="data"></param>
        /// <param name="datalen"></param>
        /// <returns></returns>
        public bool UpdateStart(byte[] data, int datalen)
        {
            if (data == null)
            {
                return false;
            }
            ProgState = PROGSTATE_UPDATE_START;
            BinPackNum = DSP28335.GetBinPackNum(BinFileLen, BINDATA_PACK_LEN);
            return true;
        }

        /// <summary>
        /// 定时发送握手信号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerhandshake_Tick(object sender, EventArgs e)
        {
            if (serialPort2.IsOpen == false)
            {
                timerhandshake.Stop();
                return;
            }
            SendPackStart();
        }
        /// <summary>
        /// 发送握手数据包
        /// </summary>
        public void SendPackStart()
        {
            byte[] buf = DSP28335.SetHandshakePacket(ChannelID, BinFileLen, Bin_CheckA, Bin_CheckB);
            Application.Current.Dispatcher.Invoke(() =>
            {
                tx.Content = "下发握手帧，等待设备回应...";
            });
            sendData(buf, buf.Length);

        }
        /// <summary>
        /// 打包并发送数据
        /// </summary>
        /// <param name="databuf"></param>
        /// <param name="datalength"></param>
        private void sendData(byte[] databuf, int datalength)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                tx.IsEnabled = true;
            });
            // 将字节数组转换为十六进制字符串  
            string hexString = BitConverter.ToString(databuf).Replace("-", " ").ToUpper();
            string strs = issxcheck ? "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "]发→◇" : "";

            Application.Current.Dispatcher.Invoke(() =>
            {
                rtbLog.AppendText(strs);

                rtbLog.AppendText(" " + hexString);
                rtbLog.AppendText("\r\n");
                // 确保滚动到底部  
                rtbLog.ScrollToEnd();
            });
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
        #endregion
        private void AddTextToLog(string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 创建一个新的Paragraph来包含文本             
                if (rtbLog.Text.Length > 5000)
                {
                    rtbLog.Text = "";
                }
                string str = issxcheck ? DateTime.Now.ToString("HH:mm:ss.fff") : "";
                rtbLog.AppendText(str + ">>" + text + "\r\n");
                // 确保滚动到底部  
                rtbLog.ScrollToEnd();
            });
        }
        int number = 0;
        public void DisDataToDlg(byte raw)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string str;



                str = string.Format("{0:X2} ", raw);


                if (txlog.Text.Length > 10000)
                {
                    txlog.Text = "";
                }
                string strs = isrxcheck ? "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "]收←◆" : "";

                if (number == 0)
                    txlog.AppendText(strs);
                number++;
                txlog.AppendText(" " + str);
                if (number > 6)
                {
                    number = 0;
                    txlog.AppendText("\r\n");
                }
                // 确保滚动到底部  
                txlog.ScrollToEnd();
            });
        }
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
            //if (isfirst < 1)
            //{
            //    isfirst++;
            //}
            //else
            //{
            //    if (comlist.SelectedItem != null)
            //        OpenCloseCom();
            //}
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
        ~SP()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            var windows = new BinReader(BinFileData, BinFileLen);
            windows.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            windows.Show();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            string tempFilePath = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFilePath, rtbLog.Text);
            Process.Start("notepad.exe", tempFilePath);
        }
        private void MenuItem_Click2(object sender, RoutedEventArgs e)
        {
            string tempFilePath = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFilePath, txlog.Text);
            Process.Start("notepad.exe", tempFilePath);
        }
        bool isrxcheck = true;
        private void MenuItem_Checked(object sender, RoutedEventArgs e)
        {
            isrxcheck = true;
        }

        private void MenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            isrxcheck = false;
        }
        bool issxcheck = true;
        private void MenuItem_Checked2(object sender, RoutedEventArgs e)
        {
            issxcheck = true;
        }

        private void MenuItem_Unchecked2(object sender, RoutedEventArgs e)
        {
            issxcheck = false;
        }
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
