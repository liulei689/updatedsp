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
        #endregion
        public UpdateDspNormal()
        {
            InitializeComponent();
            this.DataContext = App.Current.Services.GetService<DescriptionViewModel>();
            botelv.ItemsSource = new string[]{ "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };
            botelv.SelectedIndex = 5;
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
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
                        //serialPort2.Close();    //关闭串口
                        //OpenCloseCom.Text = "Open";

                        //comboBox_ComNum.Enabled = true;
                        //comboBox_BaundRate.Enabled = true;

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
                    serialPort2.PortName = comname;
                    serialPort2.BaudRate = Convert.ToInt32(botelv.SelectedItem);
                    serialPort2.StopBits = StopBits.One;
                    serialPort2.Parity = Parity.None;
                    serialPort2.DataBits = 8;
                    serialPort2.Open();//打开串口
                    //OpenCloseCom.Text = "Close";
                    ////创建数据处理线程
                    //RecDataDeal = new Thread(new ThreadStart(ProtocolParsing));
                    //RecDataDeal.IsBackground = true;
                    //RecDataDeal.Start();

                }
            }
            catch (Exception ex)
            {
                Message.Error(ex.Message);
                serialPort2.Close();    //关闭串口
                comlist.IsEnabled = true;
                botelv.IsEnabled = true;
                //RecDataDeal.Abort();
            }
            openclosecom.IsChecked = serialPort2.IsOpen;
            if(serialPort2.IsOpen)
            Message.Success(comlist.SelectedItem as string+"连接成功！");
            else
            Message.Error(comlist.SelectedItem as string + "连接失败！");

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
                rx.IsEnabled =true;
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
        private void Timer_Tick(object sender, EventArgs e)
        {
            #region 串口识别
            if (comlist.ItemsSource == null || !Common.Common.SearchPort().SequenceEqual(comlist.ItemsSource as IList<string>)) 
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

        private async void comlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comlist.SelectedItem != null)
                OpenCloseCom();

        }
    }
}
