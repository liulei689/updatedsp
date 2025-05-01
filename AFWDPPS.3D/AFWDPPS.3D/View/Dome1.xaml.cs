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
        public GeometryModel3D springModel;
        private ModelVisual3D springVisual;
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
            botelv.ItemsSource = new string[] { "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };

            botelv.SelectedIndex = 5;
            var ports = SerialPort.GetPortNames();
            if (comlist.ItemsSource == null || !ports.SequenceEqual(comlist.ItemsSource as IList<string>))
            {
                comlist.ItemsSource = SerialPort.GetPortNames();
            }
            if (comlist.SelectedItem == null && comlist.Items.Count > 0)
            {
                comlist.SelectedIndex = comlist.Items.Count - 1;
            }
            this.serialPort2 = new System.IO.Ports.SerialPort();
            serialPort2.RtsEnable = true;
            this.serialPort2.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort1_DataReceived);

            timer11 = new DispatcherTimer();
            timer11.Interval = TimeSpan.FromMilliseconds(1000); // 每500毫秒更新一次
            timer11.Tick += Timer_Tick11;
            timer11.Stop();
            data();
            ToggleTimer_Click(null, null);
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
                timer.Interval = TimeSpan.FromMilliseconds(10); // 每500毫秒更新一次
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
        private void data()
        {
            // 创建一个List来存储数据
            dataList = new List<(double x, double y)>
        {
        (12.6736,0),
        (12.1451,0.9414),
        (11.5687,1.8791),
        (10.9466,2.8093),
        (10.2814,3.7285),
        (9.5757,4.633),
        (8.8322,5.5192),
        (8.0539,6.3837),
        (7.2439,7.223),
        (6.4053,8.0338),
        (5.5414,8.8129),
        (4.6557,9.5573),
        (3.7516,10.264),
        (2.8328,10.9303),
        (1.9027,11.5534),
        (0.9652,12.131),
        (0.0239,12.6608),
        (-0.9175,13.1407),
        (-1.8553,13.5687),
        (-2.7858,13.9433),
        (-3.7054,14.2629),
        (-4.6103,14.5262),
        (-5.497,14.7323),
        (-6.362,14.8803),
        (-7.202,14.9697),
        (-8.0136,15),
        (-8.7936,14.9712),
        (-9.5389,14.8833),
        (-10.2466,14.7368),
        (-10.9139,14.5322),
        (-11.5382,14.2703),
        (-12.117,13.9521),
        (-12.648,13.5789),
        (-13.1291,13.1522),
        (-13.5585,12.6736),
        (-13.9345,12.1451),
        (-14.2555,11.5687),
        (-14.5203,10.9466),
        (-14.7278,10.2814),
        (-14.8773,9.5757),
        (-14.9681,8.8322),
        (-15,8.0539),
        (-14.9726,7.2439),
        (-14.8863,6.4053),
        (-14.7412,5.5414),
        (-14.5381,4.6557),
        (-14.2776,3.7516),
        (-13.9608,2.8328),
        (-13.589,1.9027),
        (-13.1637,0.9652),
        (-12.6864,0.0239),
        (-12.1591,-0.9175),
        (-11.5838,-1.8553),
        (-10.9629,-2.7858),
        (-10.2988,-3.7054),
        (-9.5941,-4.6103),
        (-8.8515,-5.497),
        (-8.0741,-6.362),
        (-7.2648,-7.202),
        (-6.4269,-8.0136),
        (-5.5636,-8.7936),
        (-4.6784,-9.5389),
        (-3.7748,-10.2466),
        (-2.8562,-10.9139),
        (-1.9264,-11.5382),
        (-0.9891,-12.117),
        (-0.0478,-12.648),
        (0,-13.1291),
        (0.9414,-13.5585),
        (1.8791,-13.9345),
        (2.8093,-14.2555),
        (3.7285,-14.5203),
        (4.633,-14.7278),
        (5.5192,-14.8773),
        (6.3837,-14.9681),
        (7.223,-15),
        (8.0338,-14.9726),
        (8.8129,-14.8863),
        (9.5573,-14.7412),
        (10.264,-14.5381),
        (10.9303,-14.2776),
        (11.5534,-13.9608),
        (12.131,-13.589),
        (12.6608,-13.1637),
        (13.1407,-12.6864),
        (13.5687,-12.1591),
        (13.9433,-11.5838),
        (14.2629,-10.9629),
        (14.5262,-10.2988),
        (14.7323,-9.5941),
        (14.8803,-8.8515),
        (14.9697,-8.0741),
        (15,-7.2648),
        (14.9712,-6.4269),
        (14.8833,-5.5636),
        (14.7368,-4.6784),
        (14.5322,-3.7748),
        (14.2703,-2.8562),
        (13.9521,-1.9264),
        (13.5789,-0.9891),
                };
        }

        // 定时器事件
        double avgYaw = 0;
        double avgPitch = 0;
        private void Timer_Tick(object sender, EventArgs e)
        {
            //Random random = new Random();
            //yaw += random.Next(-15, 15); // 随机生成 -5 到 5 的角度变化量
            //pitch += random.Next(-15, 15); // 随机生成 -5 到 5 的角度变化量

            //// 限制角度范围
            //yaw = Clamp(yaw, -15, 15);
            //pitch = Clamp(pitch, -15, 15);
            currentIndex = (currentIndex + 1) % dataList.Count;
            yawTextBox.Text = dataList[currentIndex].x.ToString("F2");
            pitchTextBox.Text = dataList[currentIndex].y.ToString("F2");
            yaw = dataList[currentIndex].x;
            if (currentIndex >= 3)
                avgYaw = (dataList[currentIndex].x + dataList[currentIndex - 1].x + dataList[currentIndex - 2].x) / 3;
            else
                avgYaw = (dataList[currentIndex].x + dataList[dataList.Count - currentIndex - 1].x + dataList[dataList.Count - currentIndex - 2].x) / 3;
            pitch = dataList[currentIndex].y;
            if (currentIndex >= 3)
                avgPitch = (dataList[currentIndex].y + dataList[currentIndex - 1].y + dataList[currentIndex - 2].y) / 3;
            else
                avgPitch = (dataList[currentIndex].y + dataList[dataList.Count - currentIndex - 1].y + dataList[dataList.Count - currentIndex - 2].y) / 3;
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
        private void UpdateTransform()
        {
            var yawRotation = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), pitch));
            var pitchRotation = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), yaw));
            boxModel.Transform = new Transform3DGroup { Children = { yawRotation, pitchRotation } };
            var res = new TransformManager().CalculateTransform(pitch, yaw, pitch, yaw);
            var yawRotation1 = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), res.pitch));
            var pitchRotation1 = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), res.yaw));
            boxModel1.Transform = new Transform3DGroup { Children = { yawRotation1, pitchRotation1 } };
            UpdateSpringGeometry(boxModel, boxModel1, springModel);
            x1.Text = (avgYaw).ToString("F2");
            y1.Text = (avgPitch).ToString("F2");
            if (WaveformChart != null)
                WaveformChart.OnUITimerTick(pitch, res.pitch);
        }
        #endregion
        #region 串口操作

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

                string hexString = BitConverter.ToString(buffer).Replace("-", " ").ToUpper();
                // 使用BitConverter将字节数组转换为float
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (buffer.Length == 7 && buffer[0] == 0xA5 && GetSum(buffer) == buffer[6])
                    {
                        pitch = ParseAngleFromBytes(buffer[2], buffer[3]);
                        yaw = ParseAngleFromBytes(buffer[4], buffer[5]);
                        // 限制角度范围
                        //yaw = Clamp(yaw, -10, 10);
                        //pitch = Clamp(pitch, -10, 10);

                        UpdateYaw();
                        UpdatePitch();
                    }
                    else
                    {

                    }

                });
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

        private void openclosecom_Click(object sender, RoutedEventArgs e)
        {
            OpenCloseCom();
        }
        private void OpenCloseCom()
        {
            try
            {
                //根据当前串口属性来判断是否打开
                if (serialPort2.IsOpen)
                {


                    ////串口已经处于打开状态
                    serialPort2.Close();    //关闭串口
                    comlist.IsEnabled = true;
                    botelv.IsEnabled = true;
                    openclosecom.Content = "打开串口";


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
                    openclosecom.Content = "关闭串口";
                    timer11.Stop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                serialPort2.Close();    //关闭串口
                comlist.IsEnabled = true;
                botelv.IsEnabled = true;
                openclosecom.IsChecked = false;
                openclosecom.Content = "打开串口";
                return;
                //RecDataDeal.Abort();
            }
            openclosecom.IsChecked = serialPort2.IsOpen;
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
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            WaveformChart = new WaveformChart();
            WaveformChart.Show();
        }
    }
    #endregion
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