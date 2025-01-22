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
        public System.IO.Ports.SerialPort serialPort2;
        public Dome1()
        {
            InitializeComponent();

            // 动态添加 BoxVisual3D
            boxModel = new BoxVisual3D
            {
                Center = new Point3D(0, 0, 0),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f9e2b8")), // 使用自定义颜色
                Length = 50, // 调整尺寸
                Width = 30,  // 调整尺寸
                Height = 1.8 // 调整尺寸
            };
            viewport.Items.Add(boxModel); // 添加到 HelixViewport3D

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
            timer11.Interval = TimeSpan.FromMilliseconds(50); // 每500毫秒更新一次
            timer11.Tick += Timer_Tick11;
            timer11.Stop();
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
                timer.Interval = TimeSpan.FromMilliseconds(50); // 每500毫秒更新一次
                timer.Tick += Timer_Tick;
                timer.Start();
                isTimerRunning = true;
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
        // 定时器事件
        private void Timer_Tick(object sender, EventArgs e)
        {
            Random random = new Random();
            yaw += random.Next(-2, 5); // 随机生成 -5 到 5 的角度变化量
            pitch += random.Next(-2, 5); // 随机生成 -5 到 5 的角度变化量

            // 限制角度范围
            yaw = Clamp(yaw, -10, 10);
            pitch = Clamp(pitch, -10, 10);

            UpdateYaw();
            UpdatePitch();

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

        // 更新3D模型的旋转状态
        private void UpdateTransform()
        {
            var yawRotation = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), yaw));
            var pitchRotation = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), pitch));
            boxModel.Transform = new Transform3DGroup { Children = { yawRotation, pitchRotation } };
            var d = dssd.Position;
            var s = dssd.LookDirection;
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