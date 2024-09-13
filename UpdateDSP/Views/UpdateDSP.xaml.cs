using LL2024.Algorithms.UpdateDSP;
using Rubyer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace UpdateDSP.Views
{
    /// <summary>
    /// UpdateDspNormal.xaml 的交互逻辑
    /// </summary>
    public partial class UpdateDspNormal : UserControl, IDisposable
    {
        #region 全局变量
        public System.IO.Ports.SerialPort serialPort2;
        const byte DLE = 0x55, STX = 0x02, ETX = 0x03;//包头包尾数值
        public static byte ChannelID;
        //通信协议解析
        private volatile int protocol_sign = 0;
        private const int protocol_sign_startDLE = 0;
        private const int protocol_sign_STX = 1;
        private const int protocol_sign_endDLE = 2;
        private const int protocol_sign_ETX = 3;


        private const int BINDATA_PACK_LEN = 512;


        int BinPackNum;//包个数
        int BinPackOrder;//第BinPackOrder个包

        volatile int ProgState;//程序状态
        const int PROGSTATE_UPDATE_IDEL = 0;
        const int PROGSTATE_UPDATE_START = 1;
        const int PROGSTATE_UPDATE_LOAD = 2;
        const int PROGSTATE_UPDATE_FINAL = 3;


        const byte PROTOCOL_CMD_MCUINFO = 0x04;
        const byte PROTOCOL_CMD_COMACK = 0x02;
        const byte PROTOCOL_CMD_STARTUPDATE = 0x81;
        const byte PROTOCOL_CMD_BINDATA = 0x82;
        const byte PROTOCOL_CMD_DEVINFO = 0x84;

        int BinFileLen;
        int DataLen;
        bool UpdateFlag;
        const int APPHEAD_LENGTH = 124;
        const int CIPHER_LOCAL_START = 30;
        const int DATA_LOCAL_START = 62;
        byte[] BinFileData = new byte[2 * 1024 * 1024];
        ushort Bin_CheckA, Bin_CheckB;
        ushort[] Data = new ushort[32];
        byte[] Ciphers = new byte[16];
        //byte[] pData = new byte[1024];

        public Thread RecDataDeal;
        DispatcherTimer timerhandshake;
        DispatcherTimer timer;
        #endregion
        public UpdateDspNormal()
        {
            InitializeComponent();
            // this.DataContext = App.Current.Services.GetService<DescriptionViewModel>();
            botelv.ItemsSource = new string[] { "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };
            chanleid.ItemsSource = new int[] { 0, 1 };
            chanleid.SelectedIndex = 0;
            botelv.SelectedIndex = 1;
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
            //this.serialPort2.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort1_DataReceived);
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

        /// <summary>
        /// 数据处理线程函数
        /// </summary>
        public void ProtocolParsing()
        {

            List<byte> reclist = new List<byte>();
            byte data;
            while (serialPort2.IsOpen)
            {
                byte[] RecData = new byte[serialPort2.BytesToRead];
                serialPort2.Read(RecData, 0, RecData.Length);
                if (RecData.Length >= 0x80 && RecData[0] == 0xAA && RecData[1] == 0x55 && RecData[3] == 0x80)
                {
                    if (notifytimes == 0)
                    {
                        notifytimes = 1;
                        AddTextToLog("识别到设备不在BOOTLOAD模式下，请点击上方固件升级，再重新上下电设备!");
                    }

                }
                else
                {
                    foreach (byte tmpInt in RecData)
                    {
                        RecDataQueue.Enqueue(tmpInt); //放入Queue 给Deal线程备用
                    }
                }
                //try
                //{
                if (RecDataQueue.Count < 1)
                { Thread.Sleep(10); }
                while (RecDataQueue.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        rx.IsEnabled = true;
                    });
                    data = RecDataQueue.Dequeue();
                    switch (protocol_sign)
                    {
                        // 找到数据包开始标志DLE
                        case protocol_sign_startDLE:
                            if (data == DLE)
                            {
                                reclist.Clear();
                                protocol_sign = protocol_sign_STX;
                                reclist.Add(data);
                            }
                            break;
                        // 找到数据包开始标志STX
                        case protocol_sign_STX:
                            if (data == STX)
                            {
                                protocol_sign = protocol_sign_endDLE;
                                reclist.Add(data);
                            }
                            else if (data == DLE)
                            {
                                reclist.Clear();
                                reclist.Add(data);
                            }
                            else
                            {
                                protocol_sign = protocol_sign_startDLE;
                            }
                            break;
                        // 找到数据包结束标志DLE
                        case protocol_sign_endDLE:
                            reclist.Add(data);
                            if (data == DLE)
                            {
                                protocol_sign = protocol_sign_ETX;
                            }
                            break;
                        // 找到数据包结束标志ETX
                        case protocol_sign_ETX:
                            if (data == ETX)
                            {
                                reclist.Add(data);
                                // DLE+STX+<data stream>+CHECKA+CHECKB+DLE+ETX
                                if (reclist.Count >= 7 && reclist.Count <= 2048)
                                {
                                    DisDataToDlg(reclist, reclist.Count);
                                    // 将数据内部DLE DLE转换为DLE  其实没啥用
                                    for (int j = 2; j < reclist.Count - 2; j++)
                                    {
                                        if (reclist[j] == DLE && (j + 1) < (reclist.Count - 2) && reclist[j + 1] == DLE)
                                        {
                                            reclist.RemoveAt(j);
                                        }
                                    }
                                    // 通道地址不对，不处理
                                    int PackLength = reclist.Count;
                                    byte[] DataArray = new byte[PackLength - 6];
                                    reclist.CopyTo(2, DataArray, 0, PackLength - 6);

                                    byte isRight = DSP28335.CheckSum(DataArray, PackLength - 6, reclist[PackLength - 4], reclist[PackLength - 3])[2];
                                    if (DataArray[0] == ChannelID && isRight == 1)
                                    {
                                        Implement(DataArray);
                                    }
                                    // 恢复默认值
                                    protocol_sign = protocol_sign_startDLE;
                                }
                            }
                            else if (data == DLE)
                            {
                                // DLE+DLE为数据中出现DLE的转义 特么的下位机如果有0x55会给两个0x55，防止数据中出现0x55 0x03认为完整帧了，在这里其实已经去掉其中一个0x55
                                //数据帧连续两个0x55 只取一个  只有0x55 0x03这种情况才认为出来，其他无论多少一个0x55 后面跟一个0x03 只取一个0x55 ，0x03正常取出不来。严谨逻辑
                                protocol_sign = protocol_sign_endDLE;
                            }

                            else
                            {
                                // DLE后跟的既不是ETX也不是DLE，数据包出错
                                protocol_sign = protocol_sign_startDLE;
                            }
                            break;
                        default:
                            protocol_sign = protocol_sign_startDLE;
                            break;
                    }
                    // 数据过长，丢弃数据
                    if (reclist.Count >= 2048)
                    {
                        reclist.Clear();
                        protocol_sign = protocol_sign_startDLE;
                    }
                }
                //}
                //catch (Exception ex)
                //{ Message.Error(ex.Message, "提示"); }
            }
            Thread.Sleep(500);
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
                        if (updateprogress.Value <= updateprogress.Maximum)
                            updateprogress.Value = pres + BinPackOrder * 0.73;
                        MainWindow.Instance.SetTitle("升级进度" + ((updateprogress.Value * 100) / updateprogress.Maximum).ToString("F2") + "%");
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
                            updateprogress.Value = updateprogress.Maximum;
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
            byte[] buf = new byte[BINDATA_PACK_LEN + 4];
            int tmp, len;

            // 包长度
            if (DataLen >= ((packorder + 1) * BINDATA_PACK_LEN))
            {
                len = BINDATA_PACK_LEN;
                tmp = packorder * BINDATA_PACK_LEN;
            }
            else if (DataLen >= (packorder * BINDATA_PACK_LEN) && DataLen < ((packorder + 1) * BINDATA_PACK_LEN))
            {
                len = DataLen - (packorder * BINDATA_PACK_LEN);
                tmp = packorder * BINDATA_PACK_LEN;
            }
            else
            {
                len = 0;
                tmp = 0;
            }
            buf[0] = ChannelID;
            buf[1] = PROTOCOL_CMD_BINDATA;
            buf[2] = (byte)(len >> 8);
            buf[3] = (byte)(len >> 0);
            Array.Copy(BinFileData, tmp, buf, 4, len);
            sendData(buf, len + 4);
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

        public static bool IsAllZeros(byte[] array)
        {
            foreach (var item in array)
            {
                if (item != 0)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion
        private void openclosecom_Click(object sender, RoutedEventArgs e)
        {
            OpenCloseCom();
        }
        #region 加载固件
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Bin_CheckA = 0;
            Bin_CheckB = 0;
            BinFileData = new byte[2 * 1024 * 1024];
            if (sender == null && File.Exists(LoadFileName.Text))
            {
                FileStream fs = new FileStream(LoadFileName.Text, FileMode.Open, FileAccess.Read);
                BinFileLen = (int)fs.Length;
                if (BinFileLen < APPHEAD_LENGTH)
                {
                    UpdateFlag = false;
                    UpdateStop();
                    Message.Error("程序文件为空，取消固件升级。");
                }
                for (int i = 0; i <= BinFileLen; i++)
                {
                    BinFileData[i] = (byte)fs.ReadByte();
                }
                fs.Close();
                // 初始化CheckA和CheckB和代码长度
                // 加入校验码
                DSP28335.SetHexLength(BinFileData, BinFileLen);
                var (tempA, tempB) = DSP28335.GetBinCheckAAndCheckB(BinFileData, BinFileLen);
                Bin_CheckA = tempA; Bin_CheckB = tempB;
                DSP28335.SetHexCheckAB(BinFileData, Bin_CheckA, Bin_CheckB);

                // 显示文件信息
                version.Visibility = Visibility.Visible;
                IDC_EDIT_CHECKA.Content = Bin_CheckA.ToString("X4");
                IDC_EDIT_CHECKB.Content = Bin_CheckB.ToString("X4");
                IDC_EDIT_CODELENGTH.Content = BinFileLen.ToString() + "字节";
                // 软件版本号
                IDC_EDIT_SOFTVM.Text = DSP28335.GetVersionToString(BinFileData[24], BinFileData[25]);
                Message.Success("已重新载入固件，请重新开始固件升级流程，当前载入的固件版本：" + IDC_EDIT_SOFTVM.Text, 10000, true);
                AddTextToLog("已重新载入固件，请重新开始固件升级流程，当前载入的固件版本：" + IDC_EDIT_SOFTVM.Text);
                // 软件ID
                uint g_SoftId = (uint)(BinFileData[26] << 24) + (uint)(BinFileData[27] << 16) + (uint)(BinFileData[28] << 8) + (uint)(BinFileData[29]);
                IDC_EDIT_SOFTID.Content = g_SoftId.ToString("d");
                // 串码
                byte[] SoftSn = new byte[10];
                for (int i = 0; i < 8; i++)
                {
                    SoftSn[i] = BinFileData[8 + i * 2 + 1];
                }
                IDC_EDIT_SOFTSN.Content = Encoding.ASCII.GetString(SoftSn);
                var dataid = new string[32];
                for (int i = 0; i < 32; i++)
                {
                    Data[i] = (ushort)((ushort)(BinFileData[62 + i * 2] << 8) + BinFileData[DATA_LOCAL_START + i * 2 + 1]);
                    dataid[i] = Data[i].ToString("X4");
                }
                IDC_EDIT_DAT0.Content = "";
                IDC_EDIT_DAT8.Content = "";
                IDC_EDIT_DAT16.Content = "";
                IDC_EDIT_DAT24.Content = "";
                for (int i = 0; i < 32; i++)
                {
                    if (i >= 0 && i < 8)
                        IDC_EDIT_DAT0.Content += dataid[i] + " ";
                    if (i >= 8 && i < 16)
                        IDC_EDIT_DAT8.Content += dataid[i] + " ";
                    if (i >= 16 && i < 24)
                        IDC_EDIT_DAT16.Content += dataid[i] + " ";
                    if (i >= 24 && i < 32)
                        IDC_EDIT_DAT24.Content += dataid[i] + " ";
                }
                // CIPHER0~15
                string[] cipherid = new string[16];
                for (int i = 0; i < 16; i++)
                {
                    Ciphers[i] = BinFileData[CIPHER_LOCAL_START + i];
                    cipherid[i] = Ciphers[i].ToString("X2");
                }
                IDC_EDIT_CIPHER0.Content = "";
                IDC_EDIT_CIPHER8.Content = "";
                for (int i = 0; i < 16; i++)
                {
                    if (i >= 0 && i < 8)
                        IDC_EDIT_CIPHER0.Content += cipherid[i] + " ";
                    if (i >= 8 && i < 16)
                        IDC_EDIT_CIPHER8.Content += cipherid[i] + " ";
                }
            }
            else
            {
                var openFileDialog1 = new Microsoft.Win32.OpenFileDialog();
                openFileDialog1.Filter = "二进制文件|*.bin";
                openFileDialog1.Title = "Load File";

                if (openFileDialog1.ShowDialog() == true)
                {
                    //string filename = Path.GetFileName(openFileDialog1.FileName);//只取文件名
                    var filepath = openFileDialog1.FileName;//取全路径文件名
                    LoadFileName.Text = filepath;
                    if (!File.Exists(filepath))
                    {
                        Message.Error("\n\t读取失败！\n错误原因：可能不存在此文件");
                    }
                    else
                    {
                        FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
                        BinFileLen = (int)fs.Length;
                        if (BinFileLen < APPHEAD_LENGTH)
                        {
                            UpdateFlag = false;
                            UpdateStop();
                            Message.Error("程序文件为空，取消固件升级。");
                        }
                        for (int i = 0; i <= BinFileLen; i++)
                        {
                            BinFileData[i] = (byte)fs.ReadByte();
                        }
                        fs.Close();
                    }

                    // 初始化CheckA和CheckB和代码长度
                    DSP28335.SetHexLength(BinFileData, BinFileLen);
                    var (tempA, tempB) = DSP28335.GetBinCheckAAndCheckB(BinFileData, BinFileLen);
                    Bin_CheckA = tempA; Bin_CheckB = tempB;
                    DSP28335.SetHexCheckAB(BinFileData, Bin_CheckA, Bin_CheckB);
                    // 显示文件信息
                    version.Visibility = Visibility.Visible;
                    IDC_EDIT_CHECKA.Content = Bin_CheckA.ToString("X4");
                    IDC_EDIT_CHECKB.Content = Bin_CheckB.ToString("X4");
                    IDC_EDIT_CODELENGTH.Content = BinFileLen.ToString() + "字节";
                    // 软件版本号
                    IDC_EDIT_SOFTVM.Text = DSP28335.GetVersionToString(BinFileData[24], BinFileData[25]);
                    Message.Success("当前载入的固件版本：" + IDC_EDIT_SOFTVM.Text, 10000, true);
                    // 软件ID
                    uint g_SoftId = (uint)(BinFileData[26] << 24) + (uint)(BinFileData[27] << 16) + (uint)(BinFileData[28] << 8) + (uint)(BinFileData[29]);
                    IDC_EDIT_SOFTID.Content = g_SoftId.ToString("d");
                    // 串码
                    byte[] SoftSn = new byte[10];
                    for (int i = 0; i < 8; i++)
                    {
                        SoftSn[i] = BinFileData[8 + i * 2 + 1];
                    }
                    IDC_EDIT_SOFTSN.Content = Encoding.ASCII.GetString(SoftSn);
                    var dataid = new string[32];
                    for (int i = 0; i < 32; i++)
                    {
                        Data[i] = (ushort)((ushort)(BinFileData[62 + i * 2] << 8) + BinFileData[DATA_LOCAL_START + i * 2 + 1]);
                        dataid[i] = Data[i].ToString("X4");
                    }
                    IDC_EDIT_DAT0.Content = "";
                    IDC_EDIT_DAT8.Content = "";
                    IDC_EDIT_DAT16.Content = "";
                    IDC_EDIT_DAT24.Content = "";
                    for (int i = 0; i < 32; i++)
                    {
                        if (i >= 0 && i < 8)
                            IDC_EDIT_DAT0.Content += dataid[i] + " ";
                        if (i >= 8 && i < 16)
                            IDC_EDIT_DAT8.Content += dataid[i] + " ";
                        if (i >= 16 && i < 24)
                            IDC_EDIT_DAT16.Content += dataid[i] + " ";
                        if (i >= 24 && i < 32)
                            IDC_EDIT_DAT24.Content += dataid[i] + " ";
                    }
                    // CIPHER0~15
                    string[] cipherid = new string[16];
                    for (int i = 0; i < 16; i++)
                    {
                        Ciphers[i] = BinFileData[CIPHER_LOCAL_START + i];
                        cipherid[i] = Ciphers[i].ToString("X2");
                    }
                    IDC_EDIT_CIPHER0.Content = "";
                    IDC_EDIT_CIPHER8.Content = "";
                    for (int i = 0; i < 16; i++)
                    {
                        if (i >= 0 && i < 8)
                            IDC_EDIT_CIPHER0.Content += cipherid[i] + " ";
                        if (i >= 8 && i < 16)
                            IDC_EDIT_CIPHER8.Content += cipherid[i] + " ";
                    }
                }
            }

        }
        #endregion
        #region 开始固件升级
        private void StartToUpdate()
        {
            try
            {
                // 禁止再次点击加载文件
                //LoadFileButton.Enabled = false;
                // 启动固件更新
                string pathname = LoadFileName.Text;
                if (pathname.Length == 0)
                {
                    Message.Error("尚未加载BIN固件文件！");
                    return;
                }
                // 初始化固件升级功能,数据分包，每包512字节
                if (UpdateStart(BinFileData, BinFileLen) == false)
                {
                    Message.Error("初始化固件升级模块失败！");
                    return;
                }
                // 向文本框中添加文本
                Message.Success("固件升级启动，等待设备应答......\r\n");
                // 获取通道选择
                if (chanleid.SelectedItem != null)
                {
                    ChannelID = byte.Parse(chanleid.SelectedItem.ToString());
                }
                else
                {
                    Message.Error("请填写通道号");
                    return;
                }
            }
            catch (Exception ex)
            {
                Message.Error(ex.Message);
                return;
            }
            timerhandshake.Start();
            ButtonHelper.SetLoading(start, true);
            // 启动固件更新
            UpdateFlag = true;
            start.Content = "停止固件升级";
            start.Background = new SolidColorBrush(Colors.Red);
            updateprogress.Value = updateprogress.Minimum;
            issend = false;
            AddTextToLog("待设备响应握手,可能需重新上下电设备");

        }
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (start.Content.ToString() == "开始固件升级")
            {
                if (serialPort2.IsOpen)
                {
                    if (UpdateFlag == true)
                    {
                        Message.Warning("固件升级正在进行中，无须重复开始！");

                    }
                    else
                    {
                        StartToUpdate();
                    }
                }
                else
                {
                    if (await MessageBoxR.Warning("串口未打开或需重新打开，是否要打开串口并进行固件升级？", button: MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        OpenCloseCom();
                        StartToUpdate();
                    }
                    else
                    {
                        Message.Error("串口状态异常，无法进行固件升级！");
                    }
                }
            }
            else
            {

                // 正在升级时，提醒用户是否要退出升级
                if (await MessageBoxR.Warning("正在进行固件升级，停止升级会导致固件升级失败，是否要关闭？", button: MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // 文件加载按钮
                    timerhandshake.Stop();
                    // LoadFileButton.Enabled = true;
                    // 停止固件升级
                    UpdateFlag = false;
                    UpdateStop();
                    // TxDisplay.AppendText("固件升级功能强制退出！\r\n");
                    start.Content = "开始固件升级";
                    start.Background = new SolidColorBrush(Colors.Green);
                    ButtonHelper.SetLoading(start, false);
                }
            }
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
                start.Content = "开始固件升级";
                start.Background = new SolidColorBrush(Colors.Green);
                ButtonHelper.SetLoading(start, false);
                tx.Content = "发送";
            });
            // 停止固件更新
            UpdateFlag = false;
            //Array.Clear(pData, 0, pData.Length);
            DataLen = 0;
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

            // pData = data;
            DataLen = datalen;
            ProgState = PROGSTATE_UPDATE_START;

            // 数据包总数
            BinPackNum = DataLen / BINDATA_PACK_LEN;
            // 最后一包数据可以是0长度
            BinPackNum++;
            updateprogress.Maximum = BinPackNum;
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
            byte[] buf = new byte[20];
            int i = 0;
            buf[i++] = ChannelID;
            buf[i++] = 0x81;
            // 数据总长度
            buf[i++] = (byte)(BinFileLen >> 24);
            buf[i++] = (byte)(BinFileLen >> 16);
            buf[i++] = (byte)(BinFileLen >> 8);
            buf[i++] = (byte)(BinFileLen >> 0);
            // BIN文件校验码
            buf[i++] = (byte)(Bin_CheckA >> 8);
            buf[i++] = (byte)(Bin_CheckA >> 0);
            buf[i++] = (byte)(Bin_CheckB >> 8);
            buf[i++] = (byte)(Bin_CheckB >> 0);
            // Flash操作码 都是一样的吗？
            buf[i++] = 0xA5;
            buf[i++] = 0xF1;
            int datalength = i;
            Application.Current.Dispatcher.Invoke(() =>
            {
                tx.Content = "下发握手帧，等待设备回应...";
            });
            sendData(buf, datalength);

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
            //给数据包添加包头包尾完成打包
            List<byte> sendlist = new List<byte>();

            sendlist.Add(DLE);
            sendlist.Add(STX);
            for (int i = 0; i < datalength; i++)
            {
                if (databuf[i] == DLE)
                {
                    sendlist.Add(databuf[i]);
                }
                sendlist.Add(databuf[i]);
            }
            // 校验字节
            byte[] Check = DSP28335.CheckSum(databuf, datalength);
            if (Check[0] == DLE)
            { sendlist.Add(Check[0]); }
            sendlist.Add(Check[0]);
            if (Check[1] == DLE)
            { sendlist.Add(Check[1]); }
            sendlist.Add(Check[1]);
            sendlist.Add(DLE);
            sendlist.Add(ETX);
            byte[] SendPack = new byte[sendlist.Count];
            sendlist.CopyTo(SendPack);
            try
            {
                serialPort2.Write(SendPack, 0, sendlist.Count);

            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {

                    Message.Error(ex.Message);
                });
            }
            sendlist.Clear();
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
                rtbLog.AppendText(DateTime.Now.ToString("HH:mm:ss.fff") + ">>" + text + "\r\n");
                // 确保滚动到底部  
                rtbLog.ScrollToEnd();
            });
        }
        public void DisDataToDlg(List<byte> raw, int datalen)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string str, strraw;
                strraw = "";
                for (int i = 0; i < datalen; i++)
                {
                    str = string.Format("{0:X2} ", raw[i]);
                    strraw += str;
                }
                if (txlog.Text.Length > 10000)
                {
                    txlog.Text = "";
                }
                txlog.AppendText(DateTime.Now.ToString("HH:mm:ss.fff") + ">>" + strraw + "\r\n");

                // 确保滚动到底部  
                txlog.ScrollToEnd();
            });
        }
        double pres = 0;

        int timeout = 0;
        private void Timer_Tick(object sender, EventArgs e)
        {
            //if (UpdateFlag)
            //{
            //    if (woshoutimeout > 60*10) 
            //    {
            //        AddTextToLog("等待设备握手超时，已停止固件升级，请确保串口波特率正确,设备已重新上下电!");
            //        UpdateStop();
            //    }
            //    else
            //    {
            //        woshoutimeout++;
            //    }
            //}

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

            if (updateprogress.Value < 80 && issend)
            {
                updateprogress.Value++;
                pres = updateprogress.Value;
                if (updateprogress.Value > 70)
                {
                    AddTextToLog("擦除时间超时，已停止固件升级，请重新开始固件升级并上下电设备!");
                    Message.Error("擦除时间超时，已停止固件升级，请重新开始固件升级并上下电设备!");
                    UpdateStop();
                }
                else
                {
                    // 使用正则表达式匹配括号内的数字加“秒”  
                    string pattern = @"\（(\d+)秒\）";

                    // 替换匹配的文本  
                    int time = (needFlashTime - (int)updateprogress.Value) < 0 ? 0 : needFlashTime - (int)updateprogress.Value;
                    rtbLog.Text = Regex.Replace(rtbLog.Text, pattern, match => $"（{time}秒）");
                    MainWindow.Instance.SetTitle("擦除中还剩" + time + "秒");

                    rtbLog.ScrollToEnd();
                }
            }
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
                                                                    // 不隐藏cmd窗口（这是默认行为）  
                                                                    // CreateNoWindow = false, // 可以省略这行代码  
                                                                    // 使用系统的shell执行（这是默认行为）  
                                                                    // UseShellExecute = true, // 可以省略这行代码  
            };

            // 启动进程执行命令  
            try
            {
                // 创建一个新的进程  
                using (Process process = new Process { StartInfo = startInfo })
                {
                    // 启动进程  
                    process.Start();

                    // 等待进程退出（可选，如果你想要程序在cmd窗口关闭后继续执行）  
                    // process.WaitForExit(); // 如果你不调用这个，程序将会立即继续执行，不会等待cmd窗口关闭  
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
        ~UpdateDspNormal()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
