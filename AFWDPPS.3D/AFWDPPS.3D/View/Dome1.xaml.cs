using HelixToolkit.Wpf;
using System;
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
        private bool isTimerRunning = false; // 定时器是否运行
        private BoxVisual3D boxModel; // 3D模型引用

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

        // 定时器事件
        private void Timer_Tick(object sender, EventArgs e)
        {
            Random random = new Random();
            yaw += random.Next(-5, 6); // 随机生成 -5 到 5 的角度变化量
            pitch += random.Next(-5, 6); // 随机生成 -5 到 5 的角度变化量

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
    }
}