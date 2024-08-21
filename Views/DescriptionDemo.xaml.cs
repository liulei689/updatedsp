using Microsoft.Extensions.DependencyInjection;
using UpdateDSP.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO.Ports;
using System.Threading;
using Rubyer;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices.ComTypes;
using static ICSharpCode.AvalonEdit.Document.TextDocumentWeakEventManager;




namespace UpdateDSP.Views
{
    /// <summary>
    /// UpdateDspNormal.xaml 的交互逻辑
    /// </summary>
    public partial class UpdateDspNormal : UserControl
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
        byte[] pData = new byte[1024];

        Thread RecDataDeal;
        DispatcherTimer timerhandshake;
        #endregion
        public UpdateDspNormal()
        {
            InitializeComponent();
            this.DataContext = App.Current.Services.GetService<DescriptionViewModel>();
            botelv.ItemsSource = new string[]{ "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };
            chanleid.ItemsSource = new int[] {0,1};
            chanleid.SelectedIndex = 0;
            botelv.SelectedIndex = 5;
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
            //握手定时器
             timerhandshake = new DispatcherTimer();
            timerhandshake.Interval = TimeSpan.FromMilliseconds(200);
            timerhandshake.IsEnabled = false;
            timerhandshake.Tick += timerhandshake_Tick;
           

            this.serialPort2 = new System.IO.Ports.SerialPort();
            serialPort2.RtsEnable = true;
            this.serialPort2.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort1_DataReceived);
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
                       
                        if (await MessageBoxR.Warning("正在进行固件升级，关闭串口会导致固件升级失败，是否要关闭？", button: MessageBoxButton.YesNo) ==MessageBoxResult.Yes)
                        {
                            //// 文件加载按钮
                            //UpdateButton.Text = "Start";
                            //LoadFileButton.Enabled = true;
                            //// 停止固件升级
                            //UpdateFlag = false;
                            //UpdateStop();
                            //TxDisplay.AppendText("固件升级功能强制退出！\r\n");
                            ////串口已经处于打开状态
                            //serialPort2.Close();    //关闭串口
                            //OpenCloseCom.Text = "Open";

                            //comboBox_ComNum.Enabled = true;
                            //comboBox_BaundRate.Enabled = true;

                            RecDataDeal.Abort();
                        }
                        else return;
                    }
                    else
                    {
                        ////串口已经处于打开状态
                        serialPort2.Close();    //关闭串口

                        comlist.IsEnabled = true;
                        botelv.IsEnabled = true;

                       // RecDataDeal.Abort();
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
                    serialPort2.PortName = comname;
                    serialPort2.BaudRate = Convert.ToInt32(botelv.SelectedItem);
                    serialPort2.StopBits = StopBits.One;
                    serialPort2.Parity = Parity.None;
                    serialPort2.DataBits = 8;
                    serialPort2.Open();//打开串口

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
                return;
                //RecDataDeal.Abort();
            }
            openclosecom.IsChecked = serialPort2.IsOpen;
            if(serialPort2.IsOpen)
            Message.Success(comlist.SelectedItem as string+"连接成功！");
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
                //try
                //{
                    if (RecDataQueue.Count < 1)
                    { Thread.Sleep(10); }
                    while (RecDataQueue.Count > 0)
                    {
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
                                       // DisDataToDlg(reclist, reclist.Count);
                                        // 将数据内部DLE DLE转换为DLE
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
                                        byte[] checksum = new byte[2];
                                        checksum = CheckSum(DataArray, PackLength - 6);

                                        if (DataArray[0] == 0x00 && checksum[0] == reclist[PackLength - 4] && checksum[1] == reclist[PackLength - 3])
                                        {
                                            Implement(DataArray);
                                        }
                                        // 恢复默认值
                                        protocol_sign = protocol_sign_startDLE;
                                    }
                                }
                                else if (data == DLE)
                                {
                                    // DLE+DLE为数据中出现DLE的转义
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
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            timerhandshake.IsEnabled = false;
                            timerhandshake.Stop();
                        });
                       
                        if (DataBuf[3] == 0)
                        {
                            //TxDisplay.AppendText("握手成功，开始发送第一包数据\r\n");
                            //TxDisplay.AppendText("MCU擦除FLASH成功，开始下发数据.....\r\n");
                            //TxDisplay.Focus();
                            //TxDisplay.Select(RxDisplay.TextLength, 0);
                            //TxDisplay.ScrollToCaret();
                            // 下发第一包数据
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Message.Success("握手成功，开始发送第一包数据");
                                Message.Success("MCU擦除FLASH成功，开始下发数据");
                            });
                            SendPackBinData(BinPackOrder);
                            ProgState = PROGSTATE_UPDATE_LOAD;
                        }
                        else
                        {
                            str = GetCommAckResult(DataBuf[3]);
                            str += "，退出固件升级。\r\n";
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Message.Success(str);
                            });
                            //TxDisplay.AppendText(str);
                            //TxDisplay.Focus();
                            //TxDisplay.Select(RxDisplay.TextLength, 0);
                            //TxDisplay.ScrollToCaret();
                            //UpdateStop();
                        }
                    }
                    break;
                case PROGSTATE_UPDATE_LOAD:
                    if (!(cmd == PROTOCOL_CMD_COMACK && DataBuf[2] == PROTOCOL_CMD_BINDATA))
                    {
                        break;
                    }
                    // 获得包序号
                    str = string.Format("收到{0:d}/{1:d}包应答结果：{2:d}。\r\n", BinPackOrder + 1, BinPackNum, DataBuf[3]);
                    //TxDisplay.AppendText(str);
                    //TxDisplay.Focus();
                    //TxDisplay.Select(RxDisplay.TextLength, 0);
                    //TxDisplay.ScrollToCaret();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Message.Success(str);
                    });
                    if (DataBuf[3] != 0)
                    {
                        str = GetCommAckResult(DataBuf[3]);
                        str += "，退出固件升级。\r\n";
                        //TxDisplay.AppendText(str);
                        //TxDisplay.Focus();
                        //TxDisplay.Select(RxDisplay.TextLength, 0);
                        //TxDisplay.ScrollToCaret();
                        //UpdateStop();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Message.Success(str);
                        });
                        break;
                    }
                    // 下发下一包数据
                    BinPackOrder = (BinPackOrder + 1) % BinPackNum;
                    // 设置进度条
                    //setPos(BinPackOrder);
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
                        break;
                    }

                    str = string.Format("收到{0:d}/{1:d}包应答结果：{2:d}。\r\n", BinPackOrder + 1, BinPackNum, DataBuf[3]);
                    //TxDisplay.AppendText(str);
                    //TxDisplay.Focus();
                    //TxDisplay.Select(RxDisplay.TextLength, 0);
                    //TxDisplay.ScrollToCaret();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Message.Success(str);
                    });
                    if (DataBuf[3] == 5)
                    {
                        //progressBar1.Value = progressBar1.Maximum;
                    }
                    str = GetCommAckResult(DataBuf[3]);
                    str += "，退出固件升级。\r\n";
                    //TxDisplay.AppendText(str);
                    //TxDisplay.Focus();
                    //TxDisplay.Select(RxDisplay.TextLength, 0);
                    //TxDisplay.ScrollToCaret();

                    //UpdateStop();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Message.Success(str);
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
            string str;
            switch (code)
            {
                case 0x00:
                    str = "成功应答";
                    break;
                case 0x01:
                    str = "扇区擦除错误";
                    break;
                case 0x02:
                    str = "扇区写入错误";
                    break;
                case 0x03:
                    str = "固件数据校验码错误";
                    break;
                case 0x04:
                    str = "数据包校验失败";
                    break;
                case 0x05:
                    str = "固件数据写入成功";
                    break;
                case 0x06:
                    str = "超出FLASH容量范围";
                    break;
                case 0x07:
                    str = "Boot串吗不符错误";
                    break;
                case 0x08:
                    str = "扇区擦除成功";
                    break;
                case 0xFF:
                    str = "非法数据包";
                    break;
                default:
                    str = "应答无法解析";
                    break;
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
            Array.Copy(pData, tmp, buf, 4, len);
            sendData(buf, len + 4);
        }
        #endregion
        #region 串口读取数据
        /// <summary>
        /// 串口读取数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        Queue<byte> RecDataQueue = new Queue<byte>();//接收队列，用于数据处理
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (UpdateFlag == true)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    rx.IsEnabled = true;
                });
            }
            Thread.Sleep(50);
            int n = serialPort2.BytesToRead;//接收缓冲区中数据的字节数
            byte[] RecData = new byte[serialPort2.BytesToRead];
            serialPort2.Read(RecData, 0, RecData.Length);
            foreach (byte tmpInt in RecData)
            {
                RecDataQueue.Enqueue(tmpInt); //放入Queue 给Deal线程备用
            }
        }
        #endregion
        private void openclosecom_Click(object sender, RoutedEventArgs e)
        {
            OpenCloseCom();
        }
        #region 加载固件
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            openFileDialog1.Filter = "二进制文件|*.bin";
            openFileDialog1.Title = "Load File";

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //string filename = Path.GetFileName(openFileDialog1.FileName);//只取文件名
                string filepath = openFileDialog1.FileName;//取全路径文件名
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
                        // UpdateStop();
                        Message.Error("程序文件为空，取消固件升级。");
                    }
                    for (int i = 0; i <= BinFileLen; i++)
                    {
                        BinFileData[i] = (byte)fs.ReadByte();
                    }
                    fs.Close();
                }
            }
        }
        #endregion
        #region 开始固件升级
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
                        try
                        {
                            // 禁止再次点击加载文件
                            //LoadFileButton.Enabled = false;
                            // 启动固件更新
                            string pathname = LoadFileName.Text;
                            if (pathname.Length == 0)
                            {
                                Message.Error("尚未加载程序文件！");
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
                        // 启动固件更新
                        UpdateFlag = true;
                        start.Content = "停止固件升级";
                        start.Background = new SolidColorBrush(Colors.Red);
                        //timerhandshake.Enabled = true;
                        //timerTx.Enabled = true;
                        //timerRx.Enabled = true;
                        //progressBar1.Value = progressBar1.Minimum;
                    }
                }
                else
                {
                    Message.Error("串口未打开，无法进行固件升级！");
                }
            }
            else {

                // 正在升级时，提醒用户是否要退出升级
                if (await MessageBoxR.Warning("正在进行固件升级，关闭串口会导致固件升级失败，是否要关闭？", button: MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // 文件加载按钮
                    timerhandshake.Stop();
                    // LoadFileButton.Enabled = true;
                    // 停止固件升级
                    UpdateFlag = false;
                    //  UpdateStop();
                    // TxDisplay.AppendText("固件升级功能强制退出！\r\n");
                    start.Content = "开始固件升级";
                    start.Background = new SolidColorBrush(Colors.Green);
                }
            }
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
            pData = data;
            DataLen = datalen;
            ProgState = PROGSTATE_UPDATE_START;

            // 数据包总数
            BinPackNum = DataLen / BINDATA_PACK_LEN;
            // 最后一包数据可以是0长度
            BinPackNum++;
            //progressBar1.Maximum = BinPackNum;
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
                timerhandshake.IsEnabled = false;
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
            sendData(buf, datalength);
           
        }
        /// <summary>
        /// 打包并发送数据
        /// </summary>
        /// <param name="databuf"></param>
        /// <param name="datalength"></param>
        private void sendData(byte[] databuf, int datalength)
        {
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
            byte[] Check = CheckSum(databuf, datalength);
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
                if (UpdateFlag == true)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        tx.IsEnabled = true;
                    });
                }
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
        /// <summary>
        /// 计算校验和
        /// </summary>
        /// <param name="databuf"></param>
        /// <param name="datalength"></param>
        /// <returns></returns>
        private byte[] CheckSum(byte[] databuf, int datalength)
        {
            byte CHECK_A = 0, CHECK_B = 0;
            for (int i = 0; i < datalength; i++)
            {
                CHECK_A += databuf[i];
                CHECK_B += CHECK_A;
            }
            byte[] checksum = new byte[2] { CHECK_A, CHECK_B };
            return checksum;
        }
        #endregion
        private async void Timer_Tick(object sender, EventArgs e)
        {
            #region 串口识别
            var ports = await Task.Run(() => Common.Common.SearchPort());
            if (comlist.ItemsSource == null || !ports.SequenceEqual(comlist.ItemsSource as IList<string>)) 
            {
                comlist.ItemsSource = Common.Common.SearchPort();
            }
            if (comlist.SelectedItem==null && comlist.Items.Count > 0)
            {
                comlist.SelectedIndex = comlist.Items.Count - 1;
            }
            #endregion

            var now = DateTime.Now;
            hour.Angle = (now.Hour - 12) / 12.0 * 360;
            minutes.Angle = now.Minute / 60.0 * 360;
            second.Angle = now.Second / 60.0 * 360;
            if (updateprogress.Value > 99)
                updateprogress.Value = 0;
            updateprogress.Value += 1;
        }


        private void TextBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
        private int isfirst =0;
        private  void comlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isfirst < 1)
            {
                isfirst++;
            }
            else {
                if (comlist.SelectedItem != null)
                    OpenCloseCom();
            }

        }
    }
}
