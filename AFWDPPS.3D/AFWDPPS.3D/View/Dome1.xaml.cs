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

namespace WpfApp3D
{
    public partial class Dome1 : UserControl
    {
        private double yaw = 0; // 方向角度
        private double pitch = 0; // 俯仰角度
        private DispatcherTimer timer; // 定时器
        private DispatcherTimer timer11; // 定时器
        private bool isTimerRunning = false; // 定时器是否运行
        private BoxVisual3D boxModel; // 3D模型引用
        private BoxVisual3D boxModel1; // 3D模型引用
        public System.IO.Ports.SerialPort serialPort2;
        public GeometryModel3D springModel;
        public Dome1()
        {
            InitializeComponent();

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

            // 添加到视图
            viewport.Items.Add(new ModelVisual3D { Content = springModel });

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
        // 自定义 Clamp 方法
        private double Clamp(double value, double min, double max)
        {
            return value < min ? min : (value > max ? max : value);
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
           (0,0),
(0.0471,0.4707),
(0.094,0.9395),
(0.1405,1.4047),
(0.1864,1.8642),
(0.2316,2.3165),
(0.276,2.7596),
(0.3192,3.1918),
(0.3611,3.6115),
(0.4017,4.0169),
(0.4406,4.4065),
(0.4779,4.7787),
(0.5132,5.132),
(0.5465,5.4651),
(0.5777,5.7767),
(0.6066,6.0655),
(0.633,6.3304),
(0.657,6.5703),
(0.6784,6.7844),
(0.6972,6.9717),
(0.7131,7.1314),
(0.7263,7.2631),
(0.7366,7.3662),
(0.744,7.4402),
(0.7485,7.4848),
(0.75,7.5),
(0.7486,7.4856),
(0.7442,7.4417),
(0.7368,7.3684),
(0.7266,7.2661),
(0.7135,7.1351),
(0.6976,6.976),
(0.6789,6.7895),
(0.6576,6.5761),
(0.6337,6.3368),
(0.6073,6.0725),
(0.5784,5.7843),
(0.5473,5.4733),
(0.5141,5.1407),
(0.4788,4.7879),
(0.4416,4.4161),
(0.4027,4.027),
(0.3622,3.6219),
(0.3203,3.2026),
(0.2771,2.7707),
(0.2328,2.3278),
(0.1876,1.8758),
(0.1416,1.4164),
(0.0951,0.9514),
(0.0483,0.4826),
(0.0012,0.0119),
(-0.0459,-0.4588),
(-0.0928,-0.9277),
(-0.1393,-1.3929),
(-0.1853,-1.8527),
(-0.2305,-2.3051),
(-0.2748,-2.7485),
(-0.3181,-3.181),
(-0.3601,-3.601),
(-0.4007,-4.0068),
(-0.4397,-4.3968),
(-0.4769,-4.7694),
(-0.5123,-5.1233),
(-0.5457,-5.457),
(-0.5769,-5.7691),
(-0.6058,-6.0585),
(-0.6324,-6.324),
(-0.6565,-6.5646),
(-0.6779,-6.7793),
(-0.6967,-6.9672),
(-0.7128,-7.1277),
(-0.726,-7.2601),
(-0.7364,-7.3639),
(-0.7439,-7.4387),
(-0.7484,-7.4841),
(-0.75,-7.5),
(-0.7486,-7.4863),
(-0.7443,-7.4431),
(-0.7371,-7.3706),
(-0.7269,-7.269),
(-0.7139,-7.1388),
(-0.698,-6.9804),
(-0.6795,-6.7945),
(-0.6582,-6.5818),
(-0.6343,-6.3432),
(-0.608,-6.0795),
(-0.5792,-5.7919),
(-0.5481,-5.4815),
(-0.5149,-5.1494),
(-0.4797,-4.797),
(-0.4426,-4.4258),
(-0.4037,-4.037),
(-0.3632,-3.6324),
(-0.3213,-3.2134),
(-0.2782,-2.7818),
(-0.2339,-2.3392),
(-0.1887,-1.8874),
(-0.1428,-1.4281),
(-0.0963,-0.9632),
(-0.0495,-0.4945),
(-0.0024,-0.0239)
        };
        }

        // 定时器事件
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
            pitch = dataList[currentIndex].y;
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
            Point3D startPoint = box1.Center;
            startPoint.Offset(yaw, yaw, 0);
            Point3D endPoint = box2.Center;

            // 计算弹簧的方向和长度
            Vector3D direction = endPoint - startPoint;
            direction.Normalize();

            // 生成一个小的随机偏移量
            Random rand = new Random();
            double randomOffset = (rand.NextDouble() - 0.5) * 0.2; // 随机偏移量，范围在 -0.05 到 0.05 之间

            // 创建一个平移变换，让弹簧稍微动一下
            Vector3D moveVector = direction * randomOffset;

            // 创建平移变换
            TranslateTransform3D translateTransform = new TranslateTransform3D(moveVector.X, moveVector.Y, moveVector.Z);

            // 应用变换
            springModel.Transform = translateTransform;
        }
        // 更新3D模型的旋转状态
        private void UpdateTransform()
        {
            var yawRotation = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), yaw));
            var pitchRotation = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), pitch));
            boxModel.Transform = new Transform3DGroup { Children = { yawRotation, pitchRotation } };
            var yawRotation1 = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), yaw * 0.1));
            var pitchRotation1 = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), pitch * 0.1));
            boxModel1.Transform = new Transform3DGroup { Children = { yawRotation1, pitchRotation1 } };
            UpdateSpringGeometry(boxModel, boxModel1, springModel);
            x1.Text = (yaw * 0.1).ToString();
            y1.Text = (pitch * 0.1).ToString();
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
    }
}