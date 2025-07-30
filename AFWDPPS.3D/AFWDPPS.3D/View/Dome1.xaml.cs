using HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using WpfApp3D.Models;

namespace WpfApp3D
{
    public partial class Dome1 : UserControl
    {
        #region 3D模型构建
        private double yaw = 0; // 方向角度
        private double pitch = 0; // 俯仰角度
        private DispatcherTimer timer; // 定时器
        private DispatcherTimer timer11; // 定时器
        private bool isTimerRunning = false; // 定时器是否运行
        private BoxVisual3D boxModel; // 3D模型引用
        private BoxVisual3D boxModel1; // 3D模型引用
        public System.IO.Ports.SerialPort serialPort2;
        public System.IO.Ports.SerialPort serialPort3;

        public GeometryModel3D springModel;
        private ModelVisual3D springVisual;
        SimplifiedSineWaveGenerator generator;
        public void InintZXB()
        {
            if (double.TryParse(fuzhi.Text, out double amplitude) &&
                double.TryParse(pinlv.Text, out double frequency))
            {
                // 创建或更新正弦波生成器
                if (generator == null)
                {
                    generator = new SimplifiedSineWaveGenerator(amplitude, frequency);
                }
                else
                {
                    generator.SetAmplitude(amplitude);
                    generator.SetFrequency(frequency);
                    generator.Reset(); // 重置生成器以从头开始生成数据
                }
            }
        }
        public Dome1()
        {
            InitializeComponent();
            // ProtocolParser.Run();
            // 动态添加 BoxVisual3D
            boxModel1 = new BoxVisual3D
            {
                Center = new Point3D(0, 0, 0),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f9e2b8")), // 使用自定义颜色
                Length = 20, // 调整尺寸
                Width = 15,  // 调整尺寸
                Height = 1.8 // 调整尺寸
            };
            boxModel = new BoxVisual3D
            {
                Center = new Point3D(0, 0, 20),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f9e2b8")), // 使用自定义颜色
                Length = 40, // 调整尺寸
                Width = 30,  // 调整尺寸
                Height = 1.8 // 调整尺寸
            };
            viewport.Items.Add(boxModel1); // 添加到 HelixViewport3D

            viewport.Items.Add(boxModel); // 添加到 HelixViewport3D
                                          // 创建圆柱的顶点和三角形索引
                                          // 定义弹簧的参数
                                          // 定义弹簧的参数
            Point3D startPoint = boxModel.Center; // 起点
            Point3D endPoint = boxModel1.Center; // 终点
            double radius = 0.5; // 弹簧的半径
            int turns = 20; // 弹簧的圈数

            // 创建弹簧几何形状
            var springMesh = CreateSpringGeometry(endPoint, startPoint, radius, turns);
            // 创建几何模型
            springModel = new GeometryModel3D
            {
                Geometry = springMesh,
                Material = MaterialHelper.CreateMaterial(Brushes.Silver), // 设置材质
                Transform = new TranslateTransform3D(startPoint.X, startPoint.Y, startPoint.Z)
            };
            springVisual = new ModelVisual3D { Content = springModel };
            // 添加到视图
            viewport.Items.Add(springVisual);

            // 初始化角度为0°
            UpdateYaw();
            UpdatePitch();
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
            this.serialPort2.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort1_DataReceived);
            #endregion
            #region  串口平台
            botelv2.ItemsSource = new string[] { "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };

            botelv2.SelectedIndex = 8;
            if (comlist2.ItemsSource == null || !ports.SequenceEqual(comlist2.ItemsSource as IList<string>))
            {
                comlist2.ItemsSource = SerialPort.GetPortNames();
            }
            if (comlist2.SelectedItem == null && comlist2.Items.Count > 0)
            {
                comlist2.SelectedIndex = comlist2.Items.Count - 1;
            }
            this.serialPort3 = new System.IO.Ports.SerialPort();
            serialPort3.RtsEnable = true;
            this.serialPort3.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort2_DataReceived);
            #endregion

            timer11 = new DispatcherTimer();
            timer11.Interval = TimeSpan.FromMilliseconds(1000); // 每500毫秒更新一次
            timer11.Tick += Timer_Tick11;
            timer11.Stop();
            ToggleTimer_Click(null, null);
            Loaded += Dome1_Loaded;
        }

        private void Dome1_Loaded(object sender, RoutedEventArgs e)
        {
            InintZXB();
        }

        private MeshGeometry3D CreateSpringGeometry(Point3D startPoint, Point3D endPoint, double radius, int turns)
        {
            var meshBuilder = new MeshBuilder();
            double length = (endPoint - startPoint).Length; // 弹簧的总长度
            double step = length / turns / 360.0; // 每度的步长

            // 计算螺旋线上的点
            for (double t = 0; t <= 360 * turns; t += 1)
            {
                double angle = t * Math.PI / 180.0; // 将角度转换为弧度
                double z = startPoint.Z + (t / 360.0 / turns) * length; // 当前点的 Z 坐标
                double x = startPoint.X + radius * Math.Cos(angle);
                double y = startPoint.Y + radius * Math.Sin(angle);

                meshBuilder.AddSphere(new Point3D(x, y, z), radius / 2, 8); // 添加一个小球作为弹簧的“线”
            }

            return meshBuilder.ToMesh();
        }
        #endregion
        #region 动作模拟
        //定时器模拟船体晃动
        private void Timer_Tick11(object sender, EventArgs e)
        {
            // 如果提取的值为空，停止定时器
            if (extractedValues.Count == 0)
            {
                timer11.Stop();
                return;
            }

            // 获取当前索引对应的值
            var (pitch1, yaw1) = extractedValues[currentIndex];
            yaw = yaw1;
            pitch = pitch1;
            // 更新yaw和pitch
            UpdateYaw();
            UpdatePitch();

            // 更新索引，如果到达最后一条，重新从头开始
            currentIndex = (currentIndex + 1) % extractedValues.Count;

        }


        // 增加方向角度
        private void YawIncrease_Click(object sender, RoutedEventArgs e)
        {
            yaw += 1;
            UpdateYaw();
        }

        // 减少方向角度
        private void YawDecrease_Click(object sender, RoutedEventArgs e)
        {
            yaw -= 1;
            UpdateYaw();
        }

        // 增加俯仰角度
        private void PitchIncrease_Click(object sender, RoutedEventArgs e)
        {
            pitch += 1;
            UpdatePitch();
        }

        // 减少俯仰角度
        private void PitchDecrease_Click(object sender, RoutedEventArgs e)
        {
            pitch -= 1;
            UpdatePitch();
        }

        // 启动/停止定时器
        private void ToggleTimer_Click(object sender, RoutedEventArgs e)
        {
            if (!isTimerRunning)
            {
                timer11.Stop();
                // 启动定时器
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(100); // 每500毫秒更新一次
                timer.Tick += Timer_Tick;
                timer.Start();
                isTimerRunning = true;
                if (sender != null)
                    ((Button)sender).Content = "停止定时器";
            }
            else
            {
                // 停止定时器
                timer.Stop();
                isTimerRunning = false;
                ((Button)sender).Content = "启动定时器";
            }
        }
        private int currentIndex = 0; // 当前索引
        private bool issuiji = false;
        List<(double x, double y)> dataList;

        // 定时器事件
        double avgYaw = 0;
        double avgPitch = 0;
        private async void Timer_Tick(object sender, EventArgs e)
        {
            if (generator == null) return;

            // 生成振动信号（正弦波）
            double vibrationPitch = generator.GenerateNextValue();
            double vibrationYaw = generator.GenerateNextValue(); // 假设俯仰和滚转使用相同生成器，或创建另一个

            // Step 1: 读取平台数据（添加振动）
            pitch = vibrationPitch;
            yaw = vibrationYaw;

            var (mupitch, muyaw) = datacontrl.Step1_ReadPlatformData(pitch, yaw);

            // Step 2: 控制算法
            var (djpitch, djyaw, speedPitch, speedYaw) = datacontrl.Step2_ControlMotorAlgorithm(mupitch, muyaw);

            // Step 3: 模拟电机反馈（简单身份模拟，或添加延迟）
            var (djpitc_back, djyaw_back) = datacontrl.Step3_SimulateMotorFeedback(djpitch, djyaw, speedPitch, speedYaw);

            // Step 4: 反馈稳定平台角度（闭环）
            var (pitc_back, yaw_back) = datacontrl.Step4_FeedbackStablePlatformAngle(pitch, yaw, djpitc_back, djyaw_back);

            // 更新用于显示的角度
            pitch1 = pitc_back;
            yaw1 = yaw_back;
            pitchdianji = djpitc_back;
            yawdianji = djyaw_back;

            // 记录数据
            AFWDPPS.DB.稳定平台数据 data = new AFWDPPS.DB.稳定平台数据();
            if (WaveformChart != null)
                data.流水号 = WaveformChart.Serid;
            data.船横滚角度 = pitch;
            data.声呐横滚角度 = pitch1;
            data.横滚电机动作角度 = pitchdianji;
            data.船俯仰角度 = yaw;
            data.声呐俯仰角度 = yaw1;
            data.俯仰电机动作角度 = yawdianji;
            data.时间 = DateTime.Now;
            if (WaveformChart != null)
            {
                AsyncLogger.Log(data);
            }
            if (WaveformChart != null)
                WaveformChart.OnUITimerTick(pitch, pitch1, pitchdianji, yaw, yaw1, yawdianji);

            // 更新3D模型
            if (ishide.SelectedIndex == 1)
                UpdateTransform();
        }

        // 更新方向角度
        private void UpdateYaw()
        {
            yawTextBox.Text = yaw.ToString("F2");
            UpdateTransform();
        }

        // 更新俯仰角度
        private void UpdatePitch()
        {
            pitchTextBox.Text = pitch.ToString("F2");
            UpdateTransform();
        }
        private void UpdateSpringGeometry(BoxVisual3D box1, BoxVisual3D box2, GeometryModel3D springModel)
        {
            // 弹簧新的起点和终点在这里动态调整
            Point3D newCenter1 = box1.Center;
            newCenter1.Offset(-pitch * 0.7, -yaw * 0.0, 0);
            Point3D newCenter2 = box2.Center;
            newCenter2.Offset(0, 0, 0);
            // 计算木板新的中心点

            // 计算弹簧新的起点和终点
            Point3D springStart = newCenter2;
            Point3D springEnd = newCenter1;

            // 计算弹簧的方向和长度
            Vector3D direction = springEnd - springStart;
            double length = direction.Length;

            // 创建变换组
            Transform3DGroup transformGroup = new Transform3DGroup();

            // 平移变换，将弹簧移动到起点
            TranslateTransform3D translateTransform = new TranslateTransform3D(springStart.X, springStart.Y, springStart.Z);
            transformGroup.Children.Add(translateTransform);

            // 旋转变换，使弹簧沿着方向向量对齐
            AxisAngleRotation3D rotation = new AxisAngleRotation3D(direction, 0);
            RotateTransform3D rotateTransform = new RotateTransform3D(rotation);
            transformGroup.Children.Add(rotateTransform);

            // 缩放变换，调整弹簧的长度
            ScaleTransform3D scaleTransform = new ScaleTransform3D(1, 1, length / springModel.Geometry.Bounds.SizeZ);
            transformGroup.Children.Add(scaleTransform);

            // 应用变换到弹簧模型
            springModel.Transform = transformGroup;
        }
        // 更新3D模型的旋转状态
        TransformManager datacontrl = new TransformManager();

        public void SetAlgorithmType(ControlAlgorithmType type)
        {
            datacontrl.SetAlgorithmType(type);
        }
        private void UpdateTransform()
        {
            var yawRotation = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), pitch));
            var pitchRotation = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), yaw));
            boxModel.Transform = new Transform3DGroup { Children = { yawRotation, pitchRotation } };
            var (mupitch, muyaw) = datacontrl.Step1_ReadPlatformData(pitch, yaw);
            var (djpitch, djyaw, speedPitch, speedYaw) = datacontrl.Step2_ControlMotorAlgorithm(mupitch, muyaw);
            var (djpitc_back, djyaw_back) = datacontrl.Step3_SimulateMotorFeedback(djpitch, djyaw, speedPitch, speedYaw);
            var (pitc_back, yaw_back) = datacontrl.Step4_FeedbackStablePlatformAngle(pitch, yaw, djpitc_back, djyaw_back);
            var yawRotation1 = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), pitc_back));
            var pitchRotation1 = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), yaw_back));
            boxModel1.Transform = new Transform3DGroup { Children = { yawRotation1, pitchRotation1 } };
            UpdateSpringGeometry(boxModel, boxModel1, springModel);
            x1.Text = (pitc_back).ToString("F2");
            y1.Text = (yaw_back).ToString("F2");

        }
        #endregion
        #region 稳定平台串口操作
        private int G_int_ComStatus1 = 0;
        private List<byte> G_btList_RecBuf1 = new List<byte>();
        private List<byte> G_btList_RecBuf_R1 = new List<byte>();
        private int G_int_RecBufLen1 = 0;
        double pitch1 = 0;
        double pitchdianji = 0;
        double yawdianji = 0;

        double yaw1 = 0;
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
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

                G_btList_RecBuf_R1.Clear();
                foreach (byte tmpByte in buffer)
                {
                    switch (G_int_ComStatus1)
                    {
                        case (int)enum_ComStatus.COM_STATUS_HEAD1:
                            G_btList_RecBuf1.Clear();

                            if (tmpByte == 0xA5)
                            {
                                // tmpHEAD1 = tmpByte;
                                //切换协议解析状态
                                G_int_ComStatus1 = (int)enum_ComStatus.COM_STATUS_HEAD2;
                                G_btList_RecBuf1.Add(tmpByte);
                            }
                            break;
                        case (int)enum_ComStatus.COM_STATUS_HEAD2:
                            if (tmpByte == 0x01 || tmpByte == 0x02)
                            {
                                //切换协议解析状态
                                G_int_ComStatus1 = (int)enum_ComStatus.COM_STATUS_DATA;
                                G_btList_RecBuf1.Add(tmpByte);
                            }
                            break;
                        case (int)enum_ComStatus.COM_STATUS_DATA:
                            G_btList_RecBuf1.Add(tmpByte);

                            //数据接收完成后的有效性判断
                            if (G_btList_RecBuf1.Count == 7)  //包接收完成
                            {
                                byte[] Rbuffer = G_btList_RecBuf1.ToArray();
                                string hexString = BitConverter.ToString(Rbuffer).Replace("-", " ").ToUpper();
                                // 使用BitConverter将字节数组转换为float
                                //Application.Current.Dispatcher.BeginInvoke(() =>
                                //{


                                if (Rbuffer.Length == 7 && Rbuffer[0] == 0xA5 && GetSum(Rbuffer) == Rbuffer[6])
                                {
                                    //if (COUNTS++ > 10)
                                    //{
                                    //    if (rxtxshow.IsChecked == true)
                                    //        rxlog.AddOne(hexString, "收←◆");
                                    //Application.Current.Dispatcher.Invoke(() =>
                                    //{

                                    //    if (WaveformChart != null)
                                    //        WaveformChart.OnUITimerTick(pitch, yaw);

                                    //});
                                    pitchdianji = ParseAngleFromBytes(Rbuffer[2], Rbuffer[3]);
                                    yawdianji = ParseAngleFromBytes(Rbuffer[4], Rbuffer[5]);
                                    // Removed manual adjustments to let algorithm handle stable angles
                                    pitch1 = pitch - pitchdianji;
                                    if (pitch1 < 0) pitch1 += 1.5;
                                    else
                                        pitch1 -= 1.5;

                                    yaw1 = yaw - yawdianji;
                                    if (yaw1 < 0) yaw1 += 1.5;
                                    yaw1 -= 1.5;
                                    //}
                                }


                                //  });
                                G_btList_RecBuf1.Clear();
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
                                G_int_ComStatus1 = (int)enum_ComStatus.COM_STATUS_HEAD1;
                            }

                            //数据包长度超限检查
                            if (G_btList_RecBuf1.Count >= 7)
                            {
                                G_int_ComStatus1 = (int)enum_ComStatus.COM_STATUS_HEAD1;

                                //str_ErrorInfo += "“";
                                //for (int i = 0; i < 6; i++)
                                //{
                                //    str_ErrorInfo += G_btList_RecBuf[i].ToString("X2") + " ";
                                //}
                                //str_ErrorInfo += "......”该帧数据长度超限！";

                                //清空相关缓存
                                G_btList_RecBuf1.Clear();
                            }
                            break;

                        default:
                            G_int_ComStatus1 = (int)enum_ComStatus.COM_STATUS_HEAD1;
                            break;
                    }
                }
            }
            catch { }
        }
        public float ParseAngleFromBytes(byte highByte, byte lowByte)
        {
            // Combine the bytes into a short. This preserves the sign bit.
            short angleValue = (short)((highByte << 8) | lowByte);

            // Convert back to a floating-point number and divide by 1000.
            return (float)angleValue / 1000;
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

        private void openclosecom1_Click(object sender, RoutedEventArgs e)
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
                    timer11.Stop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                serialPort2.Close();    //关闭串口
                comlist1.IsEnabled = true;
                botelv1.IsEnabled = true;
                openclosecom1.IsChecked = false;
                openclosecom1.Content = "打开稳定平台串口";
                return;
                //RecDataDeal.Abort();
            }
            openclosecom1.IsChecked = serialPort2.IsOpen;
            if (serialPort2.IsOpen)
            {
            }
            else
            {

            }

        }
        List<(double, double)> extractedValues = null;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                Title = "选择TXT文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                extractedValues = ProcessTxtFile(filePath);


            }
        }

        private List<(double, double)> ProcessTxtFile(string filePath)
        {
            List<(double, double)> extractedValues = new List<(double, double)>();

            try
            {
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    var result = ExtractValues(line);
                    extractedValues.Add(result); // 将提取的值对添加到列表中
                }
                timer11.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取文件时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return extractedValues;
        }

        private (double, double) ExtractValues(string line)
        {
            // 按逗号分割字符串
            string[] parts = line.Split(',');

            double lastValue = 0;
            double secondLastValue = 0;

            // 检查是否有足够的部分
            if (parts.Length >= 2)
            {
                // 使用索引访问最后两个部分
                double.TryParse(parts[parts.Length - 1], out lastValue);
                double.TryParse(parts[parts.Length - 2], out secondLastValue);
            }

            // 返回两个double值作为元组
            return (secondLastValue, lastValue);
        }
        public WaveformChart WaveformChart { get; set; }
        public WaveformChartFY WaveformChartFY { get; set; }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            WaveformChart = new WaveformChart();
            WaveformChart.Show();
            //WaveformChartFY = new WaveformChartFY();
            //WaveformChartFY.Show();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            var WaveformChartHis = new WaveformChartHis();
            WaveformChartHis.Show();
        }
        /// <summary>
        /// 生成正玄波
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            InintZXB();
        }

        #endregion
        #region 稳定平台串口操作
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

                            if (tmpByte == 0xFC)
                            {
                                // tmpHEAD1 = tmpByte;
                                //切换协议解析状态
                                G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_HEAD2;
                                G_btList_RecBuf.Add(tmpByte);
                            }
                            break;
                        case (int)enum_ComStatus.COM_STATUS_HEAD2:
                            if (tmpByte == 0x41)
                            {
                                //切换协议解析状态
                                G_int_ComStatus = (int)enum_ComStatus.COM_STATUS_DATA;
                                G_btList_RecBuf.Add(tmpByte);
                            }
                            break;
                        case (int)enum_ComStatus.COM_STATUS_DATA:
                            G_btList_RecBuf.Add(tmpByte);

                            //数据接收完成后的有效性判断
                            if (G_btList_RecBuf.Count == 56 && G_btList_RecBuf[0] == 0xFC && G_btList_RecBuf[1] == 0x41 && G_btList_RecBuf[55] == 0xFD)  //包接收完成
                            {
                                byte[] Rbuffer = G_btList_RecBuf.ToArray();
                                string hexString = BitConverter.ToString(Rbuffer).Replace("-", " ").ToUpper();
                                // 使用BitConverter将字节数组转换为float
                                pitch = BitConverter.ToSingle(Rbuffer, 19);
                                yaw = BitConverter.ToSingle(Rbuffer, 23);
                                pitch *= 57.3; //滚转
                                yaw *= 57.3;

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
        int count1 = 0;

        private void openclosecom2_Click(object sender, RoutedEventArgs e)
        {
            OpenCloseCom2();
        }
        private void OpenCloseCom2()
        {
            try
            {
                //根据当前串口属性来判断是否打开
                if (serialPort3.IsOpen)
                {
                    ////串口已经处于打开状态
                    serialPort3.Close();    //关闭串口
                    comlist2.IsEnabled = true;
                    botelv2.IsEnabled = true;
                    openclosecom2.Content = "打开稳定平台串口";


                }
                else
                {
                    //串口已经处于关闭状态，则设置好串口属性后打开
                    comlist2.IsEnabled = false;
                    botelv2.IsEnabled = false;

                    ////配置串口
                    string comname = "";
                    if ((comlist2.SelectedItem as string).Contains("("))
                        comname = (comlist2.SelectedItem as string).Split('(')[1].Replace(")", "");
                    if (comname.Contains("->"))
                        comname = comname.Split('-')[0];
                    if (comname == "")
                        comname = comlist2.SelectedItem as string;
                    serialPort3.PortName = comname;
                    serialPort3.BaudRate = Convert.ToInt32(botelv2.SelectedItem);
                    serialPort3.StopBits = StopBits.One;
                    serialPort3.Parity = Parity.None;
                    serialPort3.DataBits = 8;
                    serialPort3.Open();//打开串口
                    openclosecom2.Content = "关闭稳定平台串口";
                    timer11.Stop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                serialPort3.Close();    //关闭串口
                comlist2.IsEnabled = true;
                botelv2.IsEnabled = true;
                openclosecom2.IsChecked = false;
                openclosecom2.Content = "打开稳定平台串口";
                return;
                //RecDataDeal.Abort();
            }
            openclosecom1.IsChecked = serialPort3.IsOpen;
            if (serialPort3.IsOpen)
            {
            }
            else
            {

            }

        }




        #endregion
    }
    #region 读本地日志文件生成动作
    public class ProtocolParser
    {
        public static void Run()
        {
            string filePath = "C:\\Users\\liu\\Documents\\WeChat Files\\wxid_7i8ckispir9a22\\FileStorage\\File\\2025-03\\实验1（10圈）.txt"; // 替换为你的txt文件路径

            try
            {
                string content = File.ReadAllText(filePath);
                ParseContent(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取文件时发生错误: {ex.Message}");
            }
        }

        public static void ParseContent(string content)
        {
            // 移除所有空格和回车换行符，只保留十六进制字符
            var cleanedContent = new string(content.Where(c => !char.IsWhiteSpace(c)).ToArray());

            byte[] buffer = new byte[16]; // 用于存储完整的数据帧
            int currentIndex = 0; // 当前处理的位置
            int frameIndex = 0; // 当前在帧中的位置

            while (currentIndex < cleanedContent.Length)
            {
                // 检查是否找到了起始字节（A5 CC）
                if (frameIndex == 0)
                {
                    if (currentIndex + 1 < cleanedContent.Length && cleanedContent[currentIndex] == 'A' && cleanedContent[currentIndex + 1] == '5')
                    {
                        buffer[frameIndex] = Convert.ToByte(cleanedContent.Substring(currentIndex, 2), 16);
                        currentIndex += 2;
                        frameIndex++;
                    }
                    else
                    {
                        currentIndex++;
                    }
                }
                else if (frameIndex == 1)
                {
                    if (currentIndex + 1 < cleanedContent.Length && cleanedContent[currentIndex] == 'C' && cleanedContent[currentIndex + 1] == 'C')
                    {
                        buffer[frameIndex] = Convert.ToByte(cleanedContent.Substring(currentIndex, 2), 16);
                        currentIndex += 2;
                        frameIndex++;
                    }
                    else
                    {
                        frameIndex = 0;
                        currentIndex++;
                    }
                }
                else
                {
                    if (currentIndex + 1 < cleanedContent.Length)
                    {
                        buffer[frameIndex] = Convert.ToByte(cleanedContent.Substring(currentIndex, 2), 16);
                        currentIndex += 2;
                        frameIndex++;

                        if (frameIndex >= 16)
                        {
                            byte[] dataFrame = new byte[16];
                            Array.Copy(buffer, dataFrame, 16);

                            if (IsValidData(dataFrame))
                            {
                                indss++;
                                ParseAndDisplayData(dataFrame);
                            }

                            frameIndex = 0;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        public static int indss = 0;
        public static bool IsValidData(byte[] data)
        {
            if (data.Length != 16)
                return false;

            if (data[0] != 0xA5 || data[1] != 0xCC)
                return false;

            if (data[2] != 0x20)
                return false;

            byte checksum = CalculateChecksum(data);
            if (checksum != data[15])
                return false;

            return true;
        }

        public static byte CalculateChecksum(byte[] data)
        {
            int sum = 0;
            for (int i = 2; i < 15; i++)
            {
                sum += data[i];
            }
            return (byte)(sum & 0xFF);
        }

        public static void ParseAndDisplayData(byte[] data)
        {
            string startBytes = BitConverter.ToString(data, 0, 2).Replace("-", " ");
            string selfCheck = Convert.ToString(data[2], 16).PadLeft(2, '0').ToUpper();
            double xAngularVelocity = ParseAngularVelocity(data, 3);
            double yAngularVelocity = ParseAngularVelocity(data, 6);
            double zAngularVelocity = ParseAngularVelocity(data, 9);
            double temperature = ParseTemperature(data, 12);
            string spareByte = Convert.ToString(data[14], 16).PadLeft(2, '0').ToUpper();
            string checksum = Convert.ToString(data[15], 16).PadLeft(2, '0').ToUpper();

            File.AppendAllLines("1.txt", new string[1] { $"{startBytes} {selfCheck} {xAngularVelocity} {yAngularVelocity} {zAngularVelocity} {temperature} {spareByte} {checksum}" });
        }

        private static double ParseAngularVelocity(byte[] data, int startIndex)
        {
            byte[] angularVelocityBytes = new byte[] { data[startIndex], data[startIndex + 1], data[startIndex + 2] };

            if ((angularVelocityBytes[2] & 0x80) == 0x80)
            {
                byte[] extendedBytes = new byte[] { angularVelocityBytes[0], angularVelocityBytes[1], angularVelocityBytes[2], 0xFF };
                int angularVelocityRaw = BitConverter.ToInt32(extendedBytes, 0);
                return angularVelocityRaw / 256.0;
            }
            else
            {
                byte[] extendedBytes = new byte[] { angularVelocityBytes[0], angularVelocityBytes[1], angularVelocityBytes[2], 0x00 };
                int angularVelocityRaw = BitConverter.ToInt32(extendedBytes, 0);
                return angularVelocityRaw / 256.0;
            }
        }

        private static double ParseTemperature(byte[] data, int startIndex)
        {
            byte[] temperatureBytes = new byte[] { data[startIndex + 1], data[startIndex] };
            short temperatureRaw = BitConverter.ToInt16(temperatureBytes, 0);
            return temperatureRaw / 256.0;
        }
    }
    #endregion
}