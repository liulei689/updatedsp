using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
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
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] str_Usb_Serial;
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
        public MoliDevice()
        {
            InitializeComponent();
            comboBox_DevIndex.SelectedIndex = 0;
            comboBox_CANIndex.SelectedIndex = 0;
            _autoScaleTimer.Stop();
            _autoScaleTimer.Tick += _autoScaleTimer_Tick;
            Closed += MoliDevice_Closed;
            timer12 = new DispatcherTimer();
            timer12.Interval = TimeSpan.FromMilliseconds(10);
            timer12.Tick += Timer12_Tick; ;
            timer12.Stop();
            #region  串口信息稳定设备
            botelv1.ItemsSource = new string[] { "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };

            botelv1.SelectedIndex = 5;
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
            this.serialPort2.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort2_DataReceived);
            #endregion
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

                            if (tmpByte == 0xAA)
                            {
                                // tmpHEAD1 = tmpByte;
                                //切换协议解析状态
                                G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD2;
                                G_btList_RecBuf.Add(tmpByte);
                            }
                            break;
                        case (int)enum_ComStatus.COM_STATUS_HEAD2:
                            if (tmpByte == 0x55)
                            {
                                //切换协议解析状态
                                G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_DATA;
                                G_btList_RecBuf.Add(tmpByte);
                            }
                            break;
                        case (int)enum_ComStatus.COM_STATUS_DATA:
                            G_btList_RecBuf.Add(tmpByte);

                            //数据接收完成后的有效性判断
                            if (G_btList_RecBuf.Count == 12)  //包接收完成
                            {
                                byte[] Rbuffer = G_btList_RecBuf.ToArray();
                                var yaw = MoliDj.RawToAngle(Rbuffer[6]);
                                Dome1.Dome1Instance.pitchdianji = yaw;

                                //string hexString = BitConverter.ToString(Rbuffer).Replace("-", " ").ToUpper();
                                //// 使用BitConverter将字节数组转换为float
                                //pitch = BitConverter.ToSingle(Rbuffer, 19);
                                //yaw = BitConverter.ToSingle(Rbuffer, 23);
                                //pitch *= 57.3; //滚转
                                //yaw *= 57.3;
                                // UpdateYaw();
                                //UpdatePitch();
                                G_btList_RecBuf.Clear();
                                //切换协议解析状态
                                G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD1;
                            }

                            //数据包长度超限检查
                            if (G_btList_RecBuf.Count >= 56)
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
        private void Timer12_Tick(object sender, EventArgs e)
        {
            if (Dome1.Dome1Instance.generator == null) return;

            // 生成振动信号（正弦波）
            double vibrationPitch = Dome1.Dome1Instance.generator.GenerateNextValue();
            double vibrationYaw = Dome1.Dome1Instance.generator.GenerateNextValue(); // 假设俯
            Dome1.Dome1Instance.pitch = vibrationYaw;
            //if (BoXing.Instance != null)
            //    BoXing.Instance.SetBoXing(new double[6] { vibrationPitch, 0, 0, 0, 0, 0 });
            byte[] data = MoliDj.BuildFrame(vibrationPitch);
            sendData(data, data.Length);

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
                    serialPort2.Parity = Parity.None;
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

    }
}
