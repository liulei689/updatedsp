using AFWDPPS.DB;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WpfApp3D.View
{
    /// <summary>
    /// MoliDevice.xaml 的交互逻辑 
    /// </summary>
    public partial class MoliDevice : Window
    {
        #region 创芯科技can接口
        /*------------兼容ZLG的数据类型---------------------------------*/

        //1.ZLGCAN系列接口卡信息的数据类型。
        //public struct VCI_BOARD_INFO 
        //{ 
        //    public UInt16 hw_Version;
        //    public UInt16 fw_Version;
        //    public UInt16 dr_Version;
        //    public UInt16 in_Version;
        //    public UInt16 irq_Num;
        //    public byte   can_Num;
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst=20)] public byte []str_Serial_Num;
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        //    public byte[] str_hw_Type;
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        //    public byte[] Reserved;
        //}

        //以下为简易定义与调用方式，在项目属性->生成->勾选使用不安全代码即可
        unsafe public struct VCI_BOARD_INFO//使用不安全代码
        {
            public UInt16 hw_Version;
            public UInt16 fw_Version;
            public UInt16 dr_Version;
            public UInt16 in_Version;
            public UInt16 irq_Num;
            public byte can_Num;

            public fixed byte str_Serial_Num[20];
            public fixed byte str_hw_Type[40];
            public fixed byte Reserved[8];
        }

        /////////////////////////////////////////////////////
        //2.定义CAN信息帧的数据类型。
        unsafe public struct VCI_CAN_OBJ  //使用不安全代码
        {
            public uint ID;
            public uint TimeStamp;        //时间标识
            public byte TimeFlag;         //是否使用时间标识
            public byte SendType;         //发送标志。保留，未用
            public byte RemoteFlag;       //是否是远程帧
            public byte ExternFlag;       //是否是扩展帧
            public byte DataLen;          //数据长度
            public fixed byte Data[8];    //数据
            public fixed byte Reserved[3];//保留位

        }

        //3.定义初始化CAN的数据类型
        public struct VCI_INIT_CONFIG
        {
            public UInt32 AccCode;
            public UInt32 AccMask;
            public UInt32 Reserved;
            public byte Filter;   //0或1接收所有帧。2标准帧滤波，3是扩展帧滤波。
            public byte Timing0;  //波特率参数，具体配置，请查看二次开发库函数说明书。
            public byte Timing1;
            public byte Mode;     //模式，0表示正常模式，1表示只听模式,2自测模式
        }

        /*------------其他数据结构描述---------------------------------*/
        //4.USB-CAN总线适配器板卡信息的数据类型1，该类型为VCI_FindUsbDevice函数的返回参数。
        public struct VCI_BOARD_INFO1
        {
            public UInt16 hw_Version;
            public UInt16 fw_Version;
            public UInt16 dr_Version;
            public UInt16 in_Version;
            public UInt16 irq_Num;
            public byte can_Num;
            public byte Reserved;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public byte[] str_Serial_Num;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] str_hw_Type;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] str_Usb_Serial;
        }

        /*------------数据结构描述完成---------------------------------*/

        public struct CHGDESIPANDPORT
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public byte[] szpwd;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] szdesip;
            public Int32 desport;

            public void Init()
            {
                szpwd = new byte[10];
                szdesip = new byte[20];
            }
        }
        const int DEV_USBCAN = 3;
        const int DEV_USBCAN2 = 4;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DeviceType"></param>
        /// <param name="DeviceInd"></param>
        /// <param name="Reserved"></param>
        /// <returns></returns>
        /*------------兼容ZLG的函数描述---------------------------------*/
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_OpenDevice(UInt32 DeviceType, UInt32 DeviceInd, UInt32 Reserved);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_CloseDevice(UInt32 DeviceType, UInt32 DeviceInd);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_InitCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_INIT_CONFIG pInitConfig);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_ReadBoardInfo(UInt32 DeviceType, UInt32 DeviceInd, ref VCI_BOARD_INFO pInfo);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_GetReceiveNum(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_ClearBuffer(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_StartCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_ResetCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_Transmit(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_CAN_OBJ pSend, UInt32 Len);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_Receive(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_CAN_OBJ pReceive, UInt32 Len, Int32 WaitTime);

        /*------------其他函数描述---------------------------------*/

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_ConnectDevice(UInt32 DevType, UInt32 DevIndex);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_UsbDeviceReset(UInt32 DevType, UInt32 DevIndex, UInt32 Reserved);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_FindUsbDevice2(ref VCI_BOARD_INFO pInfo);
        /*------------函数描述结束---------------------------------*/

        static UInt32 m_devtype = 4;//USBCAN2

        UInt32 m_bOpen = 0;
        UInt32 m_devind = 0;
        UInt32 m_canind = 0;

        VCI_CAN_OBJ[] m_recobj = new VCI_CAN_OBJ[1000];

        UInt32[] m_arrdevtype = new UInt32[20];
        #endregion
        private readonly DispatcherTimer _autoScaleTimer =
new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1) };   // 2 Hz
        private readonly DispatcherTimer timer12 =
new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(10) };   // 2 Hz
        public System.IO.Ports.SerialPort serialPort2;
        public static double CurrentRoll = 0; // 静态变量存储电流值
        public MoliDevice()
        {
            InitializeComponent();
            comboBox_DevIndex.SelectedIndex = 0;
            comboBox_CANIndex.SelectedIndex = 0;
            _autoScaleTimer.Stop();
            _autoScaleTimer.Tick += _autoScaleTimer_Tick;
            Closed += MoliDevice_Closed;
            timer12 = new DispatcherTimer();
            timer12.Interval = TimeSpan.FromMilliseconds(1);
            timer12.Tick += Timer12_Tick;
            timer12.Start();
            #region  串口信息稳定设备
            botelv1.ItemsSource = new string[] { "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };

            botelv1.SelectedIndex = 8;
            var ports = SerialPort.GetPortNames();
            if (comlist1.ItemsSource == null || !ports.SequenceEqual(comlist1.ItemsSource as IList<string>))
            {
                comlist1.ItemsSource = SerialPort.GetPortNames();
            }
            if (comlist1.SelectedItem == null && comlist1.Items.Count > 0)
            {
                comlist1.SelectedIndex = comlist1.Items.Count - 1;
            }
            this.serialPort2 = new System.IO.Ports.SerialPort();
            serialPort2.RtsEnable = true;
            serialPort2.Parity = Parity.Even;
            this.serialPort2.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort2_DataReceived);
            #endregion
            CmbOption.SelectedIndex = 0; // 默认选中第一个选项
        }
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
        public bool CheckSum_ZeroMinusBytesSum(byte[] btAry_Data)
        {
            int m_int_CheckSum1 = 0;
            for (int index = 0; index < btAry_Data.Length - 1; index++)
            {
                m_int_CheckSum1 += btAry_Data[index];
            }
            byte data = (byte)(0 - m_int_CheckSum1);
            if (data == btAry_Data[btAry_Data.Length - 1])
                return true;
            return false;
        }
        private void serialPort2_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            try
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

                            if (tmpByte == 0xAA || tmpByte == 0xEB)
                            {
                                // tmpHEAD1 = tmpByte;
                                //切换协议解析状态
                                G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD2;
                                G_btList_RecBuf.Add(tmpByte);
                            }
                            break;
                        case (int)enum_ComStatus.COM_STATUS_HEAD2:
                            if (tmpByte == 0x55 || tmpByte == 0x90)
                            {
                                //切换协议解析状态
                                G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_DATA;
                                G_btList_RecBuf.Add(tmpByte);
                            }
                            break;
                        case (int)enum_ComStatus.COM_STATUS_DATA:
                            G_btList_RecBuf.Add(tmpByte);

                            //数据接收完成后的有效性判断
                            if (G_btList_RecBuf.Count == 29 && G_btList_RecBuf[0] == 0xAA && G_btList_RecBuf[1] == 0x55 && CheckSum_ZeroMinusBytesSum(G_btList_RecBuf.ToArray()))  //包接收完成
                            {
                                byte[] Rbuffer = G_btList_RecBuf.ToArray();


                                var yaw = MoliDj.RawToAngle(Rbuffer[6]);
                                Dome1.Dome1Instance.pitchdianji = yaw;

                                //string hexString = BitConverter.ToString(Rbuffer).Replace("-", " ").ToUpper();
                                //// 使用BitConverter将字节数组转换为float
                                ///
                                double[] floats = new double[16];
                                floats[0] = BitConverter.ToSingle(Rbuffer, 4);
                                floats[1] = BitConverter.ToSingle(Rbuffer, 8);
                                floats[8] = BitConverter.ToSingle(Rbuffer, 12);
                                floats[9] = BitConverter.ToSingle(Rbuffer, 16);
                                roll = floats[2] = BitConverter.ToSingle(Rbuffer, 20);
                                floats[3] = BitConverter.ToSingle(Rbuffer, 24);
                                floats[4] = sendjiaodu;
                                Task.Run(() =>
                                {
                                    new 船体姿态数据陀螺() { 原始数据 = Rbuffer.ToString(), 接受时间 = DateTime.Now, 船俯仰角度 = floats[8], 船横滚角度 = floats[3], x = floats[1], y = floats[9] }.AddTLData();
                                });



                                if (DebugBoXing.Instance != null)
                                    DebugBoXing.Instance.SetBoXing(floats);

                                //pitch *= 57.3; //滚转
                                //yaw *= 57.3;
                                // UpdateYaw();
                                //UpdatePitch();

                                // 更新静态电流值
                                CurrentRoll = roll;

                                // 直接用串口数据更新电机
                         
                              

                    

                                G_btList_RecBuf.Clear();
                                //切换协议解析状态
                                G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD1;
                            }
                            if (G_btList_RecBuf.Count == 19 && G_btList_RecBuf[0] == 0xEB && G_btList_RecBuf[1] == 0x90)
                            {
                                byte[] Rbuffer = G_btList_RecBuf.ToArray();
                                if (Rbuffer[0] == 0xEB) //包头1
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        TbP1.Text = BitConverter.ToSingle(Rbuffer, 6).ToString();
                                        TbI1.Text = BitConverter.ToSingle(Rbuffer, 10).ToString();
                                        TbD1.Text = BitConverter.ToSingle(Rbuffer, 14).ToString();
                                    });
                                    G_btList_RecBuf.Clear();
                                    //切换协议解析状态
                                    G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD1;
                                }
                            }

                            //数据包长度超限检查
                            if (G_btList_RecBuf.Count >= 256)
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
            }
            catch { }

        }

        public double datas = 0;
        public double roll = 0;
        static public double feedbackAngle = 0;
        static public double sendjiaodu = 0;
        private void Timer12_Tick(object sender, EventArgs e)
        {
            if (RobotArmWindow.Instance == null) return;
            // 更新电机状态
            RobotArmWindow.Instance.joints[3].Motor.Update(0.01);

            // 发送电机反馈数据：角度和陀螺仪角速度 (定时器驱动)
            byte[] feedbackFrame = BuildFeedbackFrame(RobotArmWindow.Instance.joints[3].angle, RobotArmWindow.Instance.joints[3].Motor.GyroOmega);
            sendData(feedbackFrame, feedbackFrame.Length);

            // 更新波形：只传入 current，RobotArmWindow 内部会由 motor 模拟计算 angle 与 gyro
            RobotArmWindow.Instance?.UpdateWaveforms(CurrentRoll);
        }
        private void sendData(byte[] databuf, int datalength)
        {
            if (serialPort2 == null && !serialPort2.IsOpen) return;

            //});
            try
            {
                serialPort2.Write(databuf, 0, datalength);

            }
            catch (Exception ex)
            {

            }
            Thread.Sleep(1);
        }
        private void MoliDevice_Closed(object sender, EventArgs e)
        {
            if (m_bOpen == 1)
            {
                VCI_CloseDevice(m_devtype, m_devind);
            }
            _autoScaleTimer?.Stop();
            timer12?.Stop();
        }

        unsafe private void _autoScaleTimer_Tick(object sender, EventArgs e)
        {
            UInt32 res = new UInt32();

            res = VCI_Receive(m_devtype, m_devind, m_canind, ref m_recobj[0], 1000, 100);

            /////////////////////////////////////
            //IntPtr[] ptArray = new IntPtr[1];
            //ptArray[0] = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VCI_CAN_OBJ)) * 50);
            //IntPtr pt = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)) * 1);

            //Marshal.Copy(ptArray, 0, pt, 1);


            //res = VCI_Receive(m_devtype, m_devind, m_canind, pt, 50/*50*/, 100);
            ////////////////////////////////////////////////////////
            if (res == 0xFFFFFFFF) res = 0;//当设备未初始化时，返回0xFFFFFFFF，不进行列表显示。
            String str = "";
            for (UInt32 i = 0; i < res; i++)
            {
                //VCI_CAN_OBJ obj = (VCI_CAN_OBJ)Marshal.PtrToStructure((IntPtr)((UInt32)pt + i * Marshal.SizeOf(typeof(VCI_CAN_OBJ))), typeof(VCI_CAN_OBJ));

                str = "接收到数据: ";
                str += "  帧ID:0x" + System.Convert.ToString(m_recobj[i].ID, 16);
                str += "  帧格式:";
                if (m_recobj[i].RemoteFlag == 0)
                    str += "数据帧 ";
                else
                    str += "远程帧 ";
                if (m_recobj[i].ExternFlag == 0)
                    str += "标准帧 ";
                else
                    str += "扩展帧 ";

                //////////////////////////////////////////
                if (m_recobj[i].RemoteFlag == 0)
                {
                    str += "数据: ";
                    byte len = (byte)(m_recobj[i].DataLen % 9);
                    byte j = 0;
                    fixed (VCI_CAN_OBJ* m_recobj1 = &m_recobj[i])
                    {
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[0], 16);
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[1], 16);
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[2], 16);
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[3], 16);
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[4], 16);
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[5], 16);
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[6], 16);
                        if (j++ < len)
                            str += " " + System.Convert.ToString(m_recobj1->Data[7], 16);
                    }
                }

                LvCanMsgs.Items.Add(str);
                LvCanMsgs.SelectedIndex = LvCanMsgs.Items.Count - 1;
            }
        }

        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (m_bOpen == 1)
            {
                VCI_CloseDevice(m_devtype, m_devind);
                m_bOpen = 0;
            }
            else
            {
                m_devtype = 4; //usbcan2

                m_devind = (UInt32)comboBox_DevIndex.SelectedIndex;
                m_canind = (UInt32)comboBox_CANIndex.SelectedIndex;
                if (VCI_OpenDevice(m_devtype, m_devind, 0) == 0)
                {
                    System.Windows.MessageBox.Show("打开设备失败,请检查设备类型和设备索引号是否正确");
                    return;
                }

                m_bOpen = 1;
                VCI_INIT_CONFIG config = new VCI_INIT_CONFIG();
                config.AccCode = System.Convert.ToUInt32("0x00000000", 16);
                config.AccMask = System.Convert.ToUInt32("0xFFFFFFFF", 16);
                config.Timing0 = System.Convert.ToByte("0x00", 16);
                config.Timing1 = System.Convert.ToByte("0x1C", 16);
                config.Filter = (Byte)(0 + 1); //接收所有类型
                config.Mode = (Byte)2;//还回测试模式
                VCI_InitCAN(m_devtype, m_devind, m_canind, ref config);
            }
            buttonConnect.Content = m_bOpen == 1 ? "断开CAN" : "连接CAN";
            if (m_bOpen == 0) _autoScaleTimer.Stop();
            else _autoScaleTimer.Start();
        }

        private void BtnStartCan_Click(object sender, RoutedEventArgs e)
        {
            if (m_bOpen == 0)
                return;
            VCI_StartCAN(m_devtype, m_devind, m_canind);
        }

        private void BtnResetCan_Click(object sender, RoutedEventArgs e)
        {
            if (m_bOpen == 0)
                return;
            VCI_ResetCAN(m_devtype, m_devind, m_canind);
        }

        unsafe private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (m_bOpen == 0)
                return;

            VCI_CAN_OBJ sendobj = new VCI_CAN_OBJ();
            //sendobj.Init();
            sendobj.RemoteFlag = 0;
            sendobj.ExternFlag = 0;
            sendobj.ID = 0x0123;
            int len = (textBox_Data.Text.Length + 1) / 3;
            sendobj.DataLen = System.Convert.ToByte(len);
            String strdata = textBox_Data.Text;
            int i = -1;
            if (i++ < len - 1)
                sendobj.Data[0] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
            if (i++ < len - 1)
                sendobj.Data[1] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
            if (i++ < len - 1)
                sendobj.Data[2] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
            if (i++ < len - 1)
                sendobj.Data[3] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
            if (i++ < len - 1)
                sendobj.Data[4] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
            if (i++ < len - 1)
                sendobj.Data[5] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
            if (i++ < len - 1)
                sendobj.Data[6] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);
            if (i++ < len - 1)
                sendobj.Data[7] = System.Convert.ToByte("0x" + strdata.Substring(i * 3, 2), 16);

            if (VCI_Transmit(m_devtype, m_devind, m_canind, ref sendobj, 1) == 0)
            {
                System.Windows.MessageBox.Show("发送失败");
            }
        }

        private void BtnOpenSerial_Click(object sender, RoutedEventArgs e)
        {
            OpenCloseCom1();
        }
        private void OpenCloseCom1()
        {
            try
            {
                //根据当前串口属性来判断是否打开
                if (serialPort2.IsOpen)
                {
                    ////串口已经处于打开状态
                    serialPort2.Close();    //关闭串口
                    comlist1.IsEnabled = true;
                    botelv1.IsEnabled = true;
                    openclosecom1.Content = "打开稳定平台串口";
                    timer12.Stop();
                }
                else
                {
                    //串口已经处于关闭状态，则设置好串口属性后打开
                    comlist1.IsEnabled = false;
                    botelv1.IsEnabled = false;

                    ////配置串口
                    string comname = "";
                    if ((comlist1.SelectedItem as string).Contains("("))
                        comname = (comlist1.SelectedItem as string).Split('(')[1].Replace(")", "");
                    if (comname.Contains("->"))
                        comname = comname.Split('-')[0];
                    if (comname == "")
                        comname = comlist1.SelectedItem as string;
                    serialPort2.PortName = comname;
                    serialPort2.BaudRate = Convert.ToInt32(botelv1.SelectedItem);
                    serialPort2.StopBits = StopBits.One;
                    serialPort2.Parity = Parity.Even;
                    serialPort2.DataBits = 8;
                    serialPort2.Open();//打开串口
                    openclosecom1.Content = "关闭稳定平台串口";
                    timer12.Start();

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                serialPort2.Close();    //关闭串口
                comlist1.IsEnabled = true;
                botelv1.IsEnabled = true;
                //openclosecom1.IsChecked = false;
                openclosecom1.Content = "打开稳定平台串口";
                timer12.Stop();
                return;
                //RecDataDeal.Abort();
            }
            //openclosecom1.IsChecked = serialPort2.IsOpen;
            if (serialPort2.IsOpen)
            {
            }
            else
            {

            }

        }
        bool isread = true; bool sendcmdt = false;
        private async void BtnRead_Click(object sender, RoutedEventArgs e)
        {
            {

                if ((sender as Button).Name == "BtnRead")
                { isread = true; }
                else
                    isread = false;
                for (int i = 0; i < 5; i++)
                {
                    SendCmd();
                    await Task.Delay(50);
                }

            }
        }
        private void SendCmd()
        {


            // 帧总长 18 字节
            byte[] frame = new byte[19];

            // 0~1 帧头
            frame[0] = 0xEB;
            frame[1] = 0x90;

            // 2 帧计数（这里简单写 0，实际可自增）
            frame[2] = 0;

            // 3 数据包长度
            frame[3] = 19;

            // 4 命令字：PID 查询/修改指令固定 0x01
            frame[4] = 0x01;

            // 5 更改命令字：Bit0=P, Bit1=I, Bit2=D
            byte updateFlag = 0;
            updateFlag |= 0x01;
            updateFlag |= 0x02;
            updateFlag |= 0x04;
            frame[5] = updateFlag;
            if (isread) frame[5] = 0;
            // 6~9  P(float) 小端
            FloatStringToBytes(frame, TbP.Text, 6);

            // 10~13 I(float) 小端
            FloatStringToBytes(frame, TbI.Text, 10);

            // 14~17 D(float) 小端
            FloatStringToBytes(frame, TbD.Text, 14);

            // 18 校验和：累加 0~17 字节后取反+1，再取低 8 位
            byte sum = 0;
            for (int idx = 0; idx < frame[3]; idx++)
                sum += frame[idx];
            frame[18] = (byte)(((~sum) + 1) & 0xFF);
            sendData(frame, frame.Length);

        }

        public byte[] FloatToLittleEndianBytes(float value)
        {
            // 使用BitConverter将float转换为字节数组（默认是大端序）
            byte[] bytes = BitConverter.GetBytes(value);

            // 检查系统是否使用大端序（通常Windows是小端序，但最好检查一下）
            //if (BitConverter.IsLittleEndian)
            //{
            //    // 如果系统已经是小端序，则不需要转换
            //    return bytes;
            //}
            //else
            //{
            // 如果系统是大端序，则需要反转字节数组
            Array.Reverse(bytes);
            return bytes;
            //}
        }
        public void FloatStringToBytes(byte[] bytes, string floatvalue, int startindex)
        {
            if (float.TryParse(floatvalue, out float re3))
            {
                var data3 = FloatToLittleEndianBytes(re3);
                bytes[startindex] = data3[2];
                bytes[startindex + 1] = data3[3];
                bytes[startindex + 2] = data3[0];
                bytes[startindex + 3] = data3[1];
            }
        }

        public string GetGateStatusHex(byte[] input, int start, int end = 0)
        {
            // Null or empty check for input array
            if (input == null || input.Length == 0)
                return "";

            // Validate start and end parameters
            if (end != 0 && (start < 0 || end >= input.Length || start > end))
                return "";
            // Use StringBuilder for efficient string concatenation
            var hexBuilder = new StringBuilder();
            if (end - start == 3)
            {
                byte[] buff = new byte[4];
                buff[1] = input[start];
                buff[0] = input[start + 1];
                buff[3] = input[start + 2];
                buff[2] = input[start + 3];
                var data = BitConverter.ToSingle(buff, 0);
                hexBuilder.Append(data + "(");

            }
            if (end - start == 1)
            {
                var data = BitConverter.ToInt16(input, start);
                //byte[] buff = new byte[4];
                //buff[1] = input[start];
                //buff[0] = input[start + 1];
                //buff[2] = input[start];
                //buff[3] = input[start + 1];
                //var data = BitConverter.ToSingle(buff, 0);
                hexBuilder.Append(data + "(");
            }
            else if (end == 0)
            {
                hexBuilder.Append(input[start] + "(0x" + input[start].ToString("X2"));
            }
            for (int i = start; i <= end; i++)
            {
                hexBuilder.Append(input[i].ToString("X2"));
                if (i < end) // Avoid adding space after the last element
                    hexBuilder.Append(" ");
            }

            return hexBuilder.ToString() + ")";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (DebugBoXing.Instance == null)
                DebugBoXing.Instance = new DebugBoXing();
            DebugBoXing.Instance.Show();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var win = new RobotArmWindow(); 
            win.Show();
        }
        private static byte _txCounter = 0;

        // Build feedback frame using AA55 protocol with two floats: angle and gyro
        public byte[] BuildFeedbackFrame(double angle, double gyro)
        {
            byte[] buf = new byte[31]; // Extended to include two floats (8 bytes) + checksum

            /* 固定头 */
            buf[0] = 0xAA;          // 包头1
            buf[1] = 0x55;          // 包头2
            buf[2] = _txCounter;    // 帧计数
            buf[3] = 0x1E;          // 包长度 (30 bytes data + 1 checksum)

            /* 载荷初值 */
            buf[4] = 0x01;          // 机位号：俯仰电机
            buf[5] = 0x00;          // 反馈帧计数
            buf[6] = 0x00;          // 状态字：电机正常
            buf[7] = 0x00;          // 速度（rpm×10）→ 0 rpm
            buf[8] = 0x00;          // 力矩（N·m×10）→ 0 N·m

            /* 3 路电压 0 V */
            buf[9] = 0x00; buf[10] = 0x00;   // A线
            buf[11] = 0x00; buf[12] = 0x00;   // B线
            buf[13] = 0x00; buf[14] = 0x00;   // C线

            /* 3 路电流 0 A */
            buf[15] = 0x00; buf[16] = 0x00;   // A相
            buf[17] = 0x00; buf[18] = 0x00;   // B相
            buf[19] = 0x00; buf[20] = 0x00;   // C相

            /* 角度原始值 0 */
            buf[21] = 0x00; // Keeping as 0, since we're adding floats separately

            /* 添加两个float: angle and gyro */
            FloatStringToBytes(buf, angle.ToString(), 22); // 22-25: angle as float
            FloatStringToBytes(buf, gyro.ToString(), 26);  // 26-29: gyro as float

            /* 校验和：0~29 累加 → 取反+1 */
            byte sum = 0;
            for (int i = 0; i < 30; i++) sum += buf[i];
            buf[30] = (byte)(((~sum) + 1) & 0xFF);
            _txCounter++;   // 帧计数循环
            return buf;
        }
    }








}
