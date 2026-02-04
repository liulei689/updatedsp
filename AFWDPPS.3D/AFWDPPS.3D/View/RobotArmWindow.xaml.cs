using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using ScottPlot;

namespace WpfApp3D.View
{
  public class Joint
    {
        public Model3D model = null;
        public double angle = 0;
        public double angleMin = -180;
        public double angleMax = 180;
        public int rotPointX = 0;
        public int rotPointY = 0;
        public int rotPointZ = 0;
        public int rotAxisX = 0;
        public int rotAxisY = 0;
        public int rotAxisZ = 0;
        public MotorSimulator Motor { get; set; }

        public Joint(Model3D pModel)
        {
            model = pModel;
            Motor = new MotorSimulator();
        }
    }

    /// <summary>
    /// Interaction logic for RobotArmWindow.xaml
    /// </summary>
    public partial class RobotArmWindow : Window
    {
        public static RobotArmWindow Instance;

        private bool _motorInitialized;
        private double? _prevAngleDeg;
        private DateTime _prevAngleTime;

        //provides functionality to 3d models
        Model3DGroup RA = new Model3DGroup(); //RoboticArm 3d group
        Model3D geom = null; //Debug sphere to check in which point the joint is rotatin

        public List<Joint> joints = null;

        bool switchingJoint = false;
        bool isAnimating = false;

        System.Windows.Media.Color oldColor = System.Windows.Media.Colors.White;
        GeometryModel3D oldSelectedModel = null;
        string basePath = "";
        ModelVisual3D visual;
        double LearningRate = 0.01;
        double SamplingDistance = 0.15;
        double DistanceThreshold = 20;
        //provides render to model3d objects
        ModelVisual3D RoboticArm = new ModelVisual3D();
        Transform3DGroup F1;
        Transform3DGroup F2;
        Transform3DGroup F3;
        Transform3DGroup F4;
        Transform3DGroup F5;
        Transform3DGroup F6;
        RotateTransform3D R;
        TranslateTransform3D T;
        Vector3D reachingPoint;
        int movements = 10;
        System.Windows.Forms.Timer timer1;

        DispatcherTimer j4j5SineTimer;
        DateTime j4j5SineStart;
        double j4SineBaseAngle;
        double j5SineBaseAngle;
        List<string> modelSearchPaths = new List<string>();
        // Set to TimeSpan.Zero (or negative) to run indefinitely.
        TimeSpan j4j5SineDuration = TimeSpan.Zero;
        TimeSpan j4j5SinePeriod = TimeSpan.FromSeconds(4);
        double j4SineAmplitudeDeg = 25;
        double j5SineAmplitudeDeg = 15;
        bool j4j5SineEnabled = false;
        double motorTime = 0.25; // 电机模拟时间，从sin(π/2)开始

        // Waveform data
        private const int DataPoints = 1000;
        private double[] currentData = new double[DataPoints];
        private double[] angleData = new double[DataPoints];
        private double[] gyroData = new double[DataPoints];
        private ScottPlot.Plottables.Signal currentSignal;
        private ScottPlot.Plottables.Signal angleSignal;
        private ScottPlot.Plottables.Signal gyroSignal;
        DispatcherTimer updateTimer;

#if IRB6700
        //directroy of all stl files
        private const string MODEL_PATH1 = "IRB6700-MH3_245-300_IRC5_rev02_LINK01_CAD.stl";
        private const string MODEL_PATH2 = "IRB6700-MH3_245-300_IRC5_rev00_LINK02_CAD.stl";
        private const string MODEL_PATH3 = "IRB6700-MH3_245-300_IRC5_rev02_LINK03_CAD.stl";
        private const string MODEL_PATH4 = "IRB6700-MH3_245-300_IRC5_rev01_LINK04_CAD.stl";
        private const string MODEL_PATH5 = "IRB6700-MH3_245-300_IRC5_rev01_LINK05_CAD.stl";
        private const string MODEL_PATH6 = "IRB6700-MH3_245-300_IRC5_rev01_LINK06_CAD.stl";
        private const string MODEL_PATH7 = "IRB6700-MH3_245-300_IRC5_rev02_LINK01_CABLE.stl";
        private const string MODEL_PATH8 = "IRB6700-MH3_245-300_IRC5_rev02_LINK01m_CABLE.stl";
        private const string MODEL_PATH9 = "IRB6700-MH3_245-300_IRC5_rev00_LINK02_CABLE.stl";
        private const string MODEL_PATH10 = "IRB6700-MH3_245-300_IRC5_rev00_LINK02m_CABLE.stl";
        private const string MODEL_PATH11 = "IRB6700-MH3_245-300_IRC5_rev00_LINK03a_CABLE.stl";
        private const string MODEL_PATH12 = "IRB6700-MH3_245-300_IRC5_rev00_LINK03b_CABLE.stl";
        private const string MODEL_PATH13 = "IRB6700-MH3_245-300_IRC5_rev02_LINK03m_CABLE.stl";
        private const string MODEL_PATH14 = "IRB6700-MH3_245-300_IRC5_rev01_LINK04_CABLE.stl";
        private const string MODEL_PATH15 = "IRB6700-MH3_245-300_IRC5_rev00_ROD_CAD.stl";
        private const string MODEL_PATH16 = "IRB6700-MH3_245-300_IRC5_rev00_LOGO1_CAD.stl";
        private const string MODEL_PATH17 = "IRB6700-MH3_245-300_IRC5_rev00_LOGO2_CAD.stl";
        private const string MODEL_PATH18 = "IRB6700-MH3_245-300_IRC5_rev00_LOGO3_CAD.stl";
        private const string MODEL_PATH19 = "IRB6700-MH3_245-300_IRC5_rev02_BASE_CAD.stl";
        private const string MODEL_PATH20 = "IRB6700-MH3_245-300_IRC5_rev00_CYLINDER_CAD.stl";
#else

        private const string MODEL_PATH1 = "IRB4600_20kg-250_LINK1_CAD_rev04.stl";
        private const string MODEL_PATH2 = "IRB4600_20kg-250_LINK2_CAD_rev04.stl";
        private const string MODEL_PATH3 = "IRB4600_20kg-250_LINK3_CAD_rev005.stl";
        private const string MODEL_PATH4 = "IRB4600_20kg-250_LINK4_CAD_rev04.stl";
        private const string MODEL_PATH5 = "IRB4600_20kg-250_LINK5_CAD_rev04.stl";
        private const string MODEL_PATH6 = "IRB4600_20kg-250_LINK6_CAD_rev04.stl";
        private const string MODEL_PATH7 = "IRB4600_20kg-250_LINK3_CAD_rev04.stl";
        private const string MODEL_PATH8 = "IRB4600_20kg-250_CABLES_LINK1_rev03.stl";
        private const string MODEL_PATH9 = "IRB4600_20kg-250_CABLES_LINK2_rev03.stl";
        private const string MODEL_PATH10 = "IRB4600_20kg-250_CABLES_LINK3_rev03.stl";
        private const string MODEL_PATH11 = "IRB4600_20kg-250_BASE_CAD_rev04.stl";
#endif


        public RobotArmWindow() : this(null)
        {
        }

        public RobotArmWindow(string modelsBasePath)
        {
            InitializeComponent();
            Instance = this;
            basePath = ResolveModelsBasePath(modelsBasePath);
            List<string> modelsNames = new List<string>();
            modelsNames.Add(MODEL_PATH1);
            modelsNames.Add(MODEL_PATH2);
            modelsNames.Add(MODEL_PATH3);
            modelsNames.Add(MODEL_PATH4);
            modelsNames.Add(MODEL_PATH5);
            modelsNames.Add(MODEL_PATH6);
            modelsNames.Add(MODEL_PATH7);
            modelsNames.Add(MODEL_PATH8);
            modelsNames.Add(MODEL_PATH9);
            modelsNames.Add(MODEL_PATH10);
            modelsNames.Add(MODEL_PATH11);//Until here for the 4600
#if IRB6700

            modelsNames.Add(MODEL_PATH12);
            modelsNames.Add(MODEL_PATH13);
            modelsNames.Add(MODEL_PATH14);
            modelsNames.Add(MODEL_PATH15);
            modelsNames.Add(MODEL_PATH16);
            modelsNames.Add(MODEL_PATH17);
            modelsNames.Add(MODEL_PATH18);
            modelsNames.Add(MODEL_PATH19);
            modelsNames.Add(MODEL_PATH20);
#endif
            RoboticArm.Content = Initialize_Environment(modelsNames);

            /** Debug sphere to check in which point the joint is rotating**/
            var position = new Point3D(0, 0, 0);
            var sphereMesh = CreateSphereMesh(position, 50, 15, 15);
            geom = new GeometryModel3D(sphereMesh, Materials.Brown);
            visual = new ModelVisual3D();
            visual.Content = geom;

            viewPort3d.RotateGesture = new MouseGesture(MouseAction.RightClick);
            viewPort3d.PanGesture = new MouseGesture(MouseAction.LeftClick);
            viewPort3d.Children.Add(visual);
            viewPort3d.Children.Add(RoboticArm);
            viewPort3d.Camera.LookDirection = new Vector3D(-0.9997, -0.034, 0); // 正对j4 j5中心
            viewPort3d.Camera.UpDirection = new Vector3D(0, 0, 1);
            viewPort3d.Camera.Position = new Point3D(3408, -100, 2125); // 再拉远，向右移一点

            double[] angles = { joints[0].angle, joints[1].angle, joints[2].angle, joints[3].angle, joints[4].angle, joints[5].angle };
            ForwardKinematics(angles);

            InitializePlots();
        }

        private void InitializePlots()
        {
            // Initialize data with zeros
            for (int i = 0; i < DataPoints; i++)
            {
                currentData[i] = 0;
                angleData[i] = 0;
                gyroData[i] = 0;
            }
            // Add signals
            currentSignal = currentPlot.Plot.Add.Signal(currentData);
            currentSignal.LegendText = "Current (A)";
            currentSignal.Color = ScottPlot.Color.FromHex("#0000FF");
            angleSignal = anglePlot.Plot.Add.Signal(angleData);
            angleSignal.LegendText = "Angle (°)";
            angleSignal.Color = ScottPlot.Color.FromHex("#00FF00");
            gyroSignal = gyroPlot.Plot.Add.Signal(gyroData);
            gyroSignal.LegendText = "Gyro Ω (°/s)";
            gyroSignal.Color = ScottPlot.Color.FromHex("#FF0000");
            // Set axis labels
            currentPlot.Plot.Axes.Title.Label.Text = "Current";
            anglePlot.Plot.Axes.Title.Label.Text = "Angle";
            gyroPlot.Plot.Axes.Title.Label.Text = "Gyro Angular Velocity";
            // Set fixed Y-axis limits
            currentPlot.Plot.Axes.SetLimitsY(-1.5, 1.5);
            anglePlot.Plot.Axes.SetLimitsY(-180, 180);
            gyroPlot.Plot.Axes.SetLimitsY(-500, 500);
            // Refresh
            currentPlot.Refresh();
            anglePlot.Refresh();
            gyroPlot.Refresh();
        }

        private void UpdateMotorsAndWaveforms(double dt)
        {
            foreach (var joint in joints) joint.Motor.Update(dt);
            // Set joint angles based on motor simulation
            joints[3].Motor.TL = 0.0; // No load torque for j4
            joints[3].angle = Math.Max(joints[3].angleMin, Math.Min(joints[3].angleMax, joints[3].Motor.Angle));
            // joints[4].angle is set in the sine timer
            // Add data for joint 3 (j4)
            var motor = joints[3].Motor;
            Array.Copy(currentData, 1, currentData, 0, DataPoints - 1);
            currentData[DataPoints - 1] = motor.Ia;
            Array.Copy(angleData, 1, angleData, 0, DataPoints - 1);
            angleData[DataPoints - 1] = motor.Angle;
            Array.Copy(gyroData, 1, gyroData, 0, DataPoints - 1);
            gyroData[DataPoints - 1] = motor.GyroOmega;
            // Update text values
            currentValue.Text = motor.Ia.ToString("F2") + " A";
            angleValue.Text = motor.Angle.ToString("F2") + " °";
            gyroValue.Text = motor.GyroOmega.ToString("F2") + " rad/s";
            // Refresh plots
            // currentPlot.Plot.Axes.AutoScale();
            currentPlot.Plot.Axes.SetLimitsY(-10, 10);
            anglePlot.Plot.Axes.SetLimitsY(-180, 180);
            gyroPlot.Plot.Axes.SetLimitsY(-1, 1);
            // Refresh
            currentPlot.Refresh();
            anglePlot.Refresh();
            gyroPlot.Refresh();
        }

        // UpdateWaveforms now accepts only current. Angle and gyro are produced by the motor simulator
        public void UpdateWaveforms(double current)
        {
            if (joints == null || joints.Count < 4) return; // Safety check

            // Ensure a deterministic start state so the first shown angle is 0.
            if (!_motorInitialized)
            {
                joints[3].Motor.Reset();
                _motorInitialized = true;
            }

            // Drive motor simulator for joint 3 with the incoming current
            joints[3].Motor.I_ctrl = current;
            joints[3].Motor.Update(0.016); // simulate 16ms step

            var motor = joints[3].Motor;
            double angle = motor.Angle;
            // Derive gyro directly from angle delta so that integrating gyro reproduces angle.
            // This keeps the angle/gyro waveforms strongly linked even when angle wraps at [-180, 180].
            double gyro;
            var now = DateTime.UtcNow;
            if (!_prevAngleDeg.HasValue)
            {
                _prevAngleDeg = angle;
                _prevAngleTime = now;
                gyro = 0;
            }
            else
            {
                double dt = (now - _prevAngleTime).TotalSeconds;
                if (dt <= 1e-6) dt = 0.016;

                double dAngle = angle - _prevAngleDeg.Value;
                // unwrap across -180/180 boundary
                if (dAngle > 180) dAngle -= 360;
                else if (dAngle < -180) dAngle += 360;

                gyro = dAngle / dt; // deg/s
                _prevAngleDeg = angle;
                _prevAngleTime = now;
            }

            // Update joint angle for visualization
            joints[3].angle = Math.Max(joints[3].angleMin, Math.Min(joints[3].angleMax, angle));

            // Shift and append waveform data
            Array.Copy(currentData, 1, currentData, 0, DataPoints - 1);
            currentData[DataPoints - 1] = motor.I_ctrl;
            Array.Copy(angleData, 1, angleData, 0, DataPoints - 1);
            angleData[DataPoints - 1] = angle;
            Array.Copy(gyroData, 1, gyroData, 0, DataPoints - 1);
            gyroData[DataPoints - 1] = gyro;

            currentValue.Text = motor.I_ctrl.ToString("F4") + " A";
            angleValue.Text = angle.ToString("F4") + " °";
            gyroValue.Text = gyro.ToString("F4") + " °/s";

            currentPlot.Refresh();
            anglePlot.Refresh();
            gyroPlot.Refresh();

            execute_fk();
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            char sep = Path.DirectorySeparatorChar;
            return path.EndsWith(sep.ToString()) ? path : path + sep;
        }

        private string ResolveModelsBasePath(string preferred)
        {
            modelSearchPaths.Clear();

            if (!string.IsNullOrEmpty(preferred))
            {
                modelSearchPaths.Add(preferred);
                if (Directory.Exists(preferred))
                {
                    var test = Path.Combine(preferred, MODEL_PATH1);
                    if (File.Exists(test))
                        return EnsureTrailingSeparator(preferred);
                }
            }

            var candidates = new List<string>();
            try { candidates.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "3D_Models")); } catch { }
            try { candidates.Add(Path.Combine(Path.GetDirectoryName(typeof(RobotArmWindow).Assembly.Location), "3D_Models")); } catch { }
            try { candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "3D_Models")); } catch { }

            try
            {
                var dir = Path.GetDirectoryName(typeof(RobotArmWindow).Assembly.Location);
                for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
                {
                    candidates.Add(Path.Combine(dir, "3D_Models"));
                    var parent = Directory.GetParent(dir);
                    dir = parent != null ? parent.FullName : null;
                }
            }
            catch { }

            foreach (var c in candidates)
            {
                if (string.IsNullOrEmpty(c)) continue;
                if (!modelSearchPaths.Contains(c)) modelSearchPaths.Add(c);
                try
                {
                    if (Directory.Exists(c))
                    {
                        var test = Path.Combine(c, MODEL_PATH1);
                        if (File.Exists(test))
                            return EnsureTrailingSeparator(c);
                    }
                }
                catch { }
            }


            // Fallback to base directory guess; Initialize_Environment will throw a detailed error if missing
            var fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "3D_Models");
            if (!modelSearchPaths.Contains(fallback)) modelSearchPaths.Add(fallback);
            return EnsureTrailingSeparator(fallback);
        }

        private void StartDefaultJ4J5SineMotionIfEnabled()
        {
            if (!j4j5SineEnabled)
                return;

            // Avoid interfering with IK animation.
            if (timer1 != null && timer1.Enabled)
                return;

            if (j4j5SineTimer == null)
            {
                j4j5SineTimer = new DispatcherTimer(DispatcherPriority.Render);
                j4j5SineTimer.Interval = TimeSpan.FromMilliseconds(16);
                j4j5SineTimer.Tick += (_, __) => TickDefaultJ4J5SineMotion();
            }

            j4SineBaseAngle = joints != null && joints.Count > 3 ? joints[3].angle : 0;
            j5SineBaseAngle = joints != null && joints.Count > 4 ? joints[4].angle : 0;
            j4j5SineStart = DateTime.Now;
            j4j5SineTimer.Start();
        }

        private void StopDefaultJ4J5SineMotion()
        {
            if (j4j5SineTimer != null)
                j4j5SineTimer.Stop();
        }

        private void TickDefaultJ4J5SineMotion()
        {
            if (joints == null || joints.Count < 5)
            {
                StopDefaultJ4J5SineMotion();
                return;
            }

            var elapsed = DateTime.Now - j4j5SineStart;
            if (j4j5SineDuration > TimeSpan.Zero && elapsed > j4j5SineDuration)
            {
                StopDefaultJ4J5SineMotion();
                return;
            }

            double t = elapsed.TotalSeconds;
            double w = 2.0 * Math.PI / Math.Max(0.001, j4j5SinePeriod.TotalSeconds);

            double j4Target = j4SineBaseAngle + (j4SineAmplitudeDeg * Math.Sin(w * t));

            // Respect joint limits.
            j4Target = Clamp(j4Target, joints[3].angleMin, joints[3].angleMax);

            // Update sliders without triggering joint_ValueChanged.
            isAnimating = true;
            joints[3].Motor.I_ctrl = MoliDevice.CurrentRoll;
            isAnimating = false;

            // Update motors and waveforms synchronously
            UpdateMotorsAndWaveforms(0.016); // dt = 16ms

            execute_fk();
        }

        private Model3DGroup Initialize_Environment(List<string> modelsNames)
        {
            try
            {
                ModelImporter import = new ModelImporter();
                joints = new List<Joint>();

                foreach (string modelName in modelsNames)
                {
                    var materialGroup = new MaterialGroup();
                    System.Windows.Media.Color mainColor = System.Windows.Media.Colors.White;
                    EmissiveMaterial emissMat = new EmissiveMaterial(new SolidColorBrush(mainColor));
                    DiffuseMaterial diffMat = new DiffuseMaterial(new SolidColorBrush(mainColor));
                    SpecularMaterial specMat = new SpecularMaterial(new SolidColorBrush(mainColor), 200);
                    materialGroup.Children.Add(emissMat);
                    materialGroup.Children.Add(diffMat);
                    materialGroup.Children.Add(specMat);

                    var fullPath = System.IO.Path.Combine(basePath, modelName);
                    if (!System.IO.File.Exists(fullPath))
                    {
                        var msg = "未找到 3D 模型文件: " + fullPath +
                                  "\n请将 3D_Models 文件夹放在以下任一位置，或在构造 RobotArmWindow 时传入绝对路径：\n - " + string.Join("\n - ", modelSearchPaths.ToArray());
                        throw new FileNotFoundException(msg, fullPath);
                    }

                    var link = import.Load(fullPath);
                    GeometryModel3D model = link.Children[0] as GeometryModel3D;
                    model.Material = materialGroup;
                    model.BackMaterial = materialGroup;

                    joints.Add(new Joint(link));
                }

                RA.Children.Add(joints[0].model);
                RA.Children.Add(joints[1].model);
                RA.Children.Add(joints[2].model);
                RA.Children.Add(joints[3].model);
                RA.Children.Add(joints[4].model);
                RA.Children.Add(joints[5].model);
                RA.Children.Add(joints[6].model);
                RA.Children.Add(joints[7].model);
                RA.Children.Add(joints[8].model);
                RA.Children.Add(joints[9].model);
                RA.Children.Add(joints[10].model);
#if IRB6700
                RA.Children.Add(joints[11].model);
                RA.Children.Add(joints[12].model);
                RA.Children.Add(joints[13].model);
                RA.Children.Add(joints[14].model);
                RA.Children.Add(joints[15].model);
                RA.Children.Add(joints[16].model);
                RA.Children.Add(joints[17].model);
                RA.Children.Add(joints[18].model);
                RA.Children.Add(joints[19].model);
#endif

#if IRB6700
                System.Windows.Media.Color cableColor = System.Windows.Media.Colors.DarkSlateGray;
                changeModelColor(joints[6], cableColor);
                changeModelColor(joints[7], cableColor);
                changeModelColor(joints[8], cableColor);
                changeModelColor(joints[9], cableColor);
                changeModelColor(joints[10], cableColor);
                changeModelColor(joints[11], cableColor);
                changeModelColor(joints[12], cableColor);
                changeModelColor(joints[13], cableColor);

                changeModelColor(joints[14], System.Windows.Media.Colors.Gray);

                changeModelColor(joints[15], System.Windows.Media.Colors.Red);
                changeModelColor(joints[16], System.Windows.Media.Colors.Red);
                changeModelColor(joints[17], System.Windows.Media.Colors.Red);

                changeModelColor(joints[18], System.Windows.Media.Colors.Gray);
                changeModelColor(joints[19], System.Windows.Media.Colors.Gray);

                joints[0].angleMin = -180;
                joints[0].angleMax = 180;
                joints[0].rotAxisX = 0;
                joints[0].rotAxisY = 0;
                joints[0].rotAxisZ = 1;
                joints[0].rotPointX = 0;
                joints[0].rotPointY = 0;
                joints[0].rotPointZ = 0;

                joints[1].angleMin = -100;
                joints[1].angleMax = 60;
                joints[1].rotAxisX = 0;
                joints[1].rotAxisY = 1;
                joints[1].rotAxisZ = 0;
                joints[1].rotPointX = 348;
                joints[1].rotPointY = -243;
                joints[1].rotPointZ = 775;

                joints[2].angleMin = -90;
                joints[2].angleMax = 90;
                joints[2].rotAxisX = 0;
                joints[2].rotAxisY = 1;
                joints[2].rotAxisZ = 0;
                joints[2].rotPointX = 347;
                joints[2].rotPointY = -376;
                joints[2].rotPointZ = 1923;

                joints[3].angleMin = -180;
                joints[3].angleMax = 180;
                joints[3].rotAxisX = 1;
                joints[3].rotAxisY = 0;
                joints[3].rotAxisZ = 0;
                joints[3].rotPointX = 60;
                joints[3].rotPointY = 0;
                joints[3].rotPointZ = 2125;

                joints[4].angleMin = -115;
                joints[4].angleMax = 115;
                joints[4].rotAxisX = 0;
                joints[4].rotAxisY = 1;
                joints[4].rotAxisZ = 0;
                joints[4].rotPointX = 1815;
                joints[4].rotPointY = 0;
                joints[4].rotPointZ = 2125;

                joints[5].angleMin = -180;
                joints[5].angleMax = 180;
                joints[5].rotAxisX = 1;
                joints[5].rotAxisY = 0;
                joints[5].rotAxisZ = 0;
                joints[5].rotPointX = 2008;
                joints[5].rotPointY = 0;
                joints[5].rotPointZ = 2125;


#else
                changeModelColor(joints[6], System.Windows.Media.Colors.Red);
                changeModelColor(joints[7], System.Windows.Media.Colors.Black);
                changeModelColor(joints[8], System.Windows.Media.Colors.Black);
                changeModelColor(joints[9], System.Windows.Media.Colors.Black);
                changeModelColor(joints[10], System.Windows.Media.Colors.Gray);

                RA.Children.Add(joints[0].model);
                RA.Children.Add(joints[1].model);
                RA.Children.Add(joints[2].model);
                RA.Children.Add(joints[3].model);
                RA.Children.Add(joints[4].model);
                RA.Children.Add(joints[5].model);
                RA.Children.Add(joints[6].model);
                RA.Children.Add(joints[7].model);
                RA.Children.Add(joints[8].model);
                RA.Children.Add(joints[9].model);
                RA.Children.Add(joints[10].model);
                
                joints[0].angleMin = -180;
                joints[0].angleMax = 180;
                joints[0].rotAxisX = 0;
                joints[0].rotAxisY = 0;
                joints[0].rotAxisZ = 1;
                joints[0].rotPointX = 0;
                joints[0].rotPointY = 0;
                joints[0].rotPointZ = 0;

                joints[1].angleMin = -100;
                joints[1].angleMax = 60;
                joints[1].rotAxisX = 0;
                joints[1].rotAxisY = 1;
                joints[1].rotAxisZ = 0;
                joints[1].rotPointX = 175; 
                joints[1].rotPointY = -200;
                joints[1].rotPointZ = 500;

                joints[2].angleMin = -90;
                joints[2].angleMax = 90;
                joints[2].rotAxisX = 0;
                joints[2].rotAxisY = 1;
                joints[2].rotAxisZ = 0;
                joints[2].rotPointX = 190;
                joints[2].rotPointY = -700;
                joints[2].rotPointZ = 1595;

                joints[3].angleMin = -180;
                joints[3].angleMax = 180;
                joints[3].rotAxisX = 1;
                joints[3].rotAxisY = 0;
                joints[3].rotAxisZ = 0;
                joints[3].rotPointX = 400;
                joints[3].rotPointY = 0;
                joints[3].rotPointZ = 1765;

                joints[4].angleMin = -115;
                joints[4].angleMax = 115;
                joints[4].rotAxisX = 0;
                joints[4].rotAxisY = 1;
                joints[4].rotAxisZ = 0;
                joints[4].rotPointX = 1405;
                joints[4].rotPointY = 50;
                joints[4].rotPointZ = 1765;

                joints[5].angleMin = -180;
                joints[5].angleMax = 180;
                joints[5].rotAxisX = 1;
                joints[5].rotAxisY = 0;
                joints[5].rotAxisZ = 0;
                joints[5].rotPointX = 1405;
                joints[5].rotPointY = 0;
                joints[5].rotPointZ = 1765;
#endif
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception Error:" + e.StackTrace);
            }
            return RA;
        }

        private static MeshGeometry3D CreateSphereMesh(Point3D center, double radius, int thetaDiv, int phiDiv)
        {
            if (thetaDiv < 3)
                thetaDiv = 3;
            if (phiDiv < 2)
                phiDiv = 2;

            var mesh = new MeshGeometry3D();

            // Points
            for (int pi = 0; pi <= phiDiv; pi++)
            {
                double v = (double)pi / phiDiv;
                double phi = Math.PI * v; // 0..PI
                double y = Math.Cos(phi);
                double r = Math.Sin(phi);

                for (int ti = 0; ti <= thetaDiv; ti++)
                {

                    double u = (double)ti / thetaDiv;
                    double theta = 2.0 * Math.PI * u; // 0..2PI

                    double x = r * Math.Cos(theta);
                    double z = r * Math.Sin(theta);

                    var normal = new Vector3D(x, y, z);
                    normal.Normalize();

                    mesh.Normals.Add(normal);
                    mesh.TextureCoordinates.Add(new System.Windows.Point(u, v));
                    mesh.Positions.Add(new Point3D(
                        center.X + radius * x,
                        center.Y + radius * y,
                        center.Z + radius * z));
                }
            }

            int stride = thetaDiv + 1;
            for (int pi = 0; pi < phiDiv; pi++)
            {
                for (int ti = 0; ti < thetaDiv; ti++)
                {
                    int a = (pi * stride) + ti;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;

                    // two triangles: a-c-b and b-c-d (consistent winding)
                    mesh.TriangleIndices.Add(a);
                    mesh.TriangleIndices.Add(c);
                    mesh.TriangleIndices.Add(b);

                    mesh.TriangleIndices.Add(b);
                    mesh.TriangleIndices.Add(c);
                    mesh.TriangleIndices.Add(d);
                }
            }

            mesh.Freeze();
            return mesh;
        }

        public static T Clamp<T>(T value, T min, T max)
            where T : System.IComparable<T>
        {
            T result = value;
            if (value.CompareTo(max) > 0)
                result = max;
            if (value.CompareTo(min) < 0)
                result = min;
            return result;
        }

        // private void ReachingPoint_TextChanged(object sender, TextChangedEventArgs e)
        // {
        //     if (TbX == null || TbY == null || TbZ == null)
        //         return;

        //     double x, y, z;
        //     if (!double.TryParse(TbX.Text, out x)) return;
        //     if (!double.TryParse(TbY.Text, out y)) return;
        //     if (!double.TryParse(TbZ.Text, out z)) return;

        //     reachingPoint = new Vector3D(x, y, z);
        //     if (geom != null)
        //     {
        //         geom.Transform = new TranslateTransform3D(reachingPoint);
        //     }
        // }

        // private void jointSelector_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        // {
        //     changeSelectedJoint();
        // }

        // private void changeSelectedJoint()
        // {
        //     if (joints == null)
        //         return;

        //     int sel = ((int)jointSelector.Value) - 1;
        //     switchingJoint = true;
        //     unselectModel();
        //     if (sel < 0)
        //     {
        //         jointX.IsEnabled = false;
        //         jointY.IsEnabled = false;
        //         jointZ.IsEnabled = false;
        //         jointXAxis.IsEnabled = false;
        //         jointYAxis.IsEnabled = false;
        //         jointZAxis.IsEnabled = false;
        //     }
        //     else
        //     {
        //         if (!jointX.IsEnabled)
        //         {
        //             jointX.IsEnabled = true;
        //             jointY.IsEnabled = true;
        //             jointZ.IsEnabled = true;

        //             jointXAxis.IsEnabled = true;
        //             jointYAxis.IsEnabled = true;
        //             jointZAxis.IsEnabled = true;
        //         }
        //         jointX.Value = joints[sel].rotPointX;
        //         jointY.Value = joints[sel].rotPointY;
        //         jointZ.Value = joints[sel].rotPointZ;
        //         jointXAxis.IsChecked = joints[sel].rotAxisX == 1 ? true : false;
        //         jointYAxis.IsChecked = joints[sel].rotAxisY == 1 ? true : false;
        //         jointZAxis.IsChecked = joints[sel].rotAxisZ == 1 ? true : false;
        //         selectModel(joints[sel].model);
        //         updateSpherePosition();
        //     }
        //     switchingJoint = false;
        // }

        // private void rotationPointChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        // {
        //     if (switchingJoint)
        //         return;

        //     int sel = ((int)jointSelector.Value) - 1;
        //     joints[sel].rotPointX = (int)jointX.Value;
        //     joints[sel].rotPointY = (int)jointY.Value;
        //     joints[sel].rotPointZ = (int)jointZ.Value;
        //     updateSpherePosition();
        // }
        private void updateSpherePosition()
        {
            //int sel = ((int)jointSelector.Value) - 1;
            //if (sel < 0)
            //    return;

            //Transform3DGroup F = new Transform3DGroup();
            //F.Children.Add(new TranslateTransform3D(joints[sel].rotPointX, joints[sel].rotPointY, joints[sel].rotPointZ));
            //F.Children.Add(joints[sel].model.Transform);
            //geom.Transform = F;
        }

        // private void CheckBox_StateChanged(object sender, RoutedEventArgs e)
        // {
        //     if (switchingJoint)
        //         return;

        //     int sel = ((int)jointSelector.Value) - 1;
        //     joints[sel].rotAxisX = jointXAxis.IsChecked.Value ? 1 : 0;
        //     joints[sel].rotAxisY = jointYAxis.IsChecked.Value ? 1 : 0;
        //     joints[sel].rotAxisZ = jointZAxis.IsChecked.Value ? 1 : 0;
        // }


        /**
         * This methodes execute the FK (Forward Kinematics). It starts from the first joint, the base.
         * */
        private void execute_fk()
        {
            /** Debug sphere, it takes the x,y,z of the textBoxes and update its position
             * This is useful when using x,y,z in the "new Point3D(x,y,z)* when defining a new RotateTransform3D() to check where the joints is actually  rotating */
            double[] angles = { joints[0].angle, joints[1].angle, joints[2].angle, joints[3].angle, joints[4].angle, joints[5].angle };
            ForwardKinematics(angles);
            // updateSpherePosition(); // Removed as controls are removed
        }

        private System.Windows.Media.Color changeModelColor(Joint pJoint, System.Windows.Media.Color newColor)
        {
            Model3DGroup models = ((Model3DGroup)pJoint.model);
            return changeModelColor(models.Children[0] as GeometryModel3D, newColor);
        }

        private System.Windows.Media.Color changeModelColor(GeometryModel3D pModel, System.Windows.Media.Color newColor)
        {
            if (pModel == null)
                return oldColor;

            System.Windows.Media.Color previousColor = System.Windows.Media.Colors.Black;

            MaterialGroup mg = (MaterialGroup)pModel.Material;
            if (mg.Children.Count > 0)
            {
                try
                {
                    previousColor = ((EmissiveMaterial)mg.Children[0]).Color;
                    ((EmissiveMaterial)mg.Children[0]).Color = newColor;
                    ((DiffuseMaterial)mg.Children[1]).Color = newColor;

                }
                catch (Exception exc)
                {
                    previousColor = oldColor;
                }
            }

            return previousColor;
        }


        private void selectModel(Model3D pModel)
        {
            try
            {
                Model3DGroup models = ((Model3DGroup)pModel);
                oldSelectedModel = models.Children[0] as GeometryModel3D;
            }
            catch (Exception exc)
            {
                oldSelectedModel = (GeometryModel3D)pModel;
            }
            oldColor = changeModelColor(oldSelectedModel, System.Windows.Media.Color.FromRgb(255, 51, 51));
        }

        private void unselectModel()
        {
            changeModelColor(oldSelectedModel, oldColor);
        }

        private void ViewPort3D_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(viewPort3d);
            PointHitTestParameters hitParams = new PointHitTestParameters(mousePos);
            VisualTreeHelper.HitTest(viewPort3d, null, ResultCallback, hitParams);
        }

        private void ViewPort3D_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Perform the hit test on the mouse's position relative to the viewport.
            HitTestResult result = VisualTreeHelper.HitTest(viewPort3d, e.GetPosition(viewPort3d));
            RayMeshGeometry3DHitTestResult mesh_result = result as RayMeshGeometry3DHitTestResult;

            if (oldSelectedModel != null)
                unselectModel();

            if (mesh_result != null)
            {
                selectModel(mesh_result.ModelHit);
            }
        }

        public HitTestResultBehavior ResultCallback(HitTestResult result)
        {
            // Did we hit 3D?
            RayHitTestResult rayResult = result as RayHitTestResult;
            if (rayResult != null)
            {
                // Did we hit a MeshGeometry3D?
                RayMeshGeometry3DHitTestResult rayMeshResult = rayResult as RayMeshGeometry3DHitTestResult;
                geom.Transform = new TranslateTransform3D(new Vector3D(rayResult.PointHit.X, rayResult.PointHit.Y, rayResult.PointHit.Z));

                if (rayMeshResult != null)
                {
                    // Yes we did!
                }
            }

            return HitTestResultBehavior.Continue;
        }

        public void StartInverseKinematics(object sender, RoutedEventArgs e)
        {
            if (timer1.Enabled)
            {
                //button.Content = "Go to position";
                isAnimating = false;
                timer1.Stop();
                movements = 0;

                StartDefaultJ4J5SineMotionIfEnabled();
            }
            else
            {
                StopDefaultJ4J5SineMotion();
                geom.Transform = new TranslateTransform3D(reachingPoint);
                movements = 5000;
               // button.Content = "STOP";
                isAnimating = true;
                timer1.Start();
            }
        }

        public void timer1_Tick(object sender, EventArgs e)
        {
            double[] angles = { joints[0].angle, joints[1].angle, joints[2].angle, joints[3].angle, joints[4].angle, joints[5].angle };
            angles = InverseKinematics(reachingPoint, angles);
            joints[0].angle = angles[0];
            joints[1].angle = angles[1];
            joints[2].angle = angles[2];
            joints[3].angle = angles[3];
            joints[4].angle = angles[4];
            joints[5].angle = angles[5];

            if ((--movements) <= 0)
            {
               // button.Content = "Go to position";
                isAnimating = false;
                timer1.Stop();
            }
        }

        public double[] InverseKinematics(Vector3D target, double[] angles)
        {
            if (DistanceFromTarget(target, angles) < DistanceThreshold)
            {
                movements = 0;
                return angles;
            }

            double[] oldAngles = { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
            angles.CopyTo(oldAngles, 0);
            for (int i = 0; i <= 5; i++)
            {
                // Gradient descent
                // Update : Solution -= LearningRate * Gradient
                double gradient = PartialGradient(target, angles, i);
                angles[i] -= LearningRate * gradient;

                // Clamp
                angles[i] = Clamp(angles[i], joints[i].angleMin, joints[i].angleMax);

                // Early termination
                if (DistanceFromTarget(target, angles) < DistanceThreshold || checkAngles(oldAngles, angles))
                {
                    movements = 0;
                    return angles;
                }
            }

            return angles;
        }

        public bool checkAngles(double[] oldAngles, double[] angles)
        {
            for (int i = 0; i <= 5; i++)
            {
                if (oldAngles[i] != angles[i])
                    return false;
            }

            return true;
        }

        public double PartialGradient(Vector3D target, double[] angles, int i)
        {
            // Saves the angle,
            // it will be restored later
            double angle = angles[i];

            // Gradient : [F(x+SamplingDistance) - F(x)] / h
            double f_x = DistanceFromTarget(target, angles);

            angles[i] += SamplingDistance;
            double f_x_plus_d = DistanceFromTarget(target, angles);

            double gradient = (f_x_plus_d - f_x) / SamplingDistance;

            // Restores
            angles[i] = angle;

            return gradient;
        }


        public double DistanceFromTarget(Vector3D target, double[] angles)
        {
            Vector3D point = ForwardKinematics(angles);
            return Math.Sqrt(Math.Pow((point.X - target.X), 2.0) + Math.Pow((point.Y - target.Y), 2.0) + Math.Pow((point.Z - target.Z), 2.0));
        }


        public Vector3D ForwardKinematics(double[] angles)
        {
            //The base only has rotation and is always at the origin, so the only transform in the transformGroup is the rotation R
            F1 = new Transform3DGroup();
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[0].rotAxisX, joints[0].rotAxisY, joints[0].rotAxisZ), angles[0]), new Point3D(joints[0].rotPointX, joints[0].rotPointY, joints[0].rotPointZ));
            F1.Children.Add(R);

            //This moves the first joint attached to the base, it may translate and rotate. Since the joint are already in the right position (the .stl model也 store the joints position
            //in the virtual world when they were first created, so if you load all the .stl models of the joint they will be automatically positioned in the right locations)
            //so in all of these cases the first translation is always 0, I just left it for future purposes if something need to be moved
            //After that, the joint needs to rotate of a certain amount (given by the value in the slider), and the rotation must be executed on a specific point
            //After some testing it looks like the point 175, -200, 500 is the sweet spot to achieve the rotation intended for the joint
            //finally we also need to apply the transformation applied to the base 
            F2 = new Transform3DGroup();
            T = new TranslateTransform3D(0, 0, 0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[1].rotAxisX, joints[1].rotAxisY, joints[1].rotAxisZ), angles[1]), new Point3D(joints[1].rotPointX, joints[1].rotPointY, joints[1].rotPointZ));
            F2.Children.Add(T);
            F2.Children.Add(R);
            F2.Children.Add(F1);

            //The second joint is attached to the first one. As before I found the sweet spot after testing, and looks like is rotating just fine. No pre-translation as before
            //and again the previous transformation needs to be applied
            F3 = new Transform3DGroup();
            T = new TranslateTransform3D(0, 0, 0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[2].rotAxisX, joints[2].rotAxisY, joints[2].rotAxisZ), angles[2]), new Point3D(joints[2].rotPointX, joints[2].rotPointY, joints[2].rotPointZ));
            F3.Children.Add(T);
            F3.Children.Add(R);
            F3.Children.Add(F2);

            //as before
            F4 = new Transform3DGroup();
            T = new TranslateTransform3D(0, 0, 0); //1500, 650, 1650
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[3].rotAxisX, joints[3].rotAxisY, joints[3].rotAxisZ), angles[3]), new Point3D(joints[3].rotPointX, joints[3].rotPointY, joints[3].rotPointZ));
            F4.Children.Add(T);
            F4.Children.Add(R);
            F4.Children.Add(F3);

            //as before
            F5 = new Transform3DGroup();
            T = new TranslateTransform3D(0, 0, 0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[4].rotAxisX, joints[4].rotAxisY, joints[4].rotAxisZ), angles[4]), new Point3D(joints[4].rotPointX, joints[4].rotPointY, joints[4].rotPointZ));
            F5.Children.Add(T);
            F5.Children.Add(R);
            F5.Children.Add(F4);

            //NB: I was having a nightmare trying to understand why it was always rotating in a weird way... SO I realized that the order in which
            //you add the Children is actually VERY IMPORTANT in fact before I was applyting F and then T and R, but the previous transformation
            //Should always be applied as last (FORWARD Kinematics)
            F6 = new Transform3DGroup();
            T = new TranslateTransform3D(0, 0, 0);
            R = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(joints[5].rotAxisX, joints[5].rotAxisY, joints[5].rotAxisZ), angles[5]), new Point3D(joints[5].rotPointX, joints[5].rotPointY, joints[5].rotPointZ));
            F6.Children.Add(T);
            F6.Children.Add(R);
            F6.Children.Add(F5);


            joints[0].model.Transform = F1; //First joint
            joints[1].model.Transform = F2; //Second joint (the "biceps")
            joints[2].model.Transform = F3; //third joint (the "knee" or "elbow")
            joints[3].model.Transform = F4; //the "forearm"
            joints[4].model.Transform = F5; //the tool plate
            joints[5].model.Transform = F6;


            // Removed Tx, Ty, Tz updates as labels removed
#if IRB6700
            joints[6].model.Transform = F1;
            joints[7].model.Transform = F1;
            joints[19].model.Transform = F1;
            joints[14].model.Transform = F1;

            joints[8].model.Transform = F2;
            joints[9].model.Transform = F2;

            joints[10].model.Transform = F3;
            joints[11].model.Transform = F3;
            joints[12].model.Transform = F3;
            joints[16].model.Transform = F3;

            joints[13].model.Transform = F4;
            joints[17].model.Transform = F4;
#else
            joints[7].model.Transform = F1; //Cables

            joints[8].model.Transform = F2; //Cables

            joints[6].model.Transform = F3; //The ABB writing
            joints[9].model.Transform = F3; //Cables
#endif

            return new Vector3D(joints[5].model.Bounds.Location.X, joints[5].model.Bounds.Location.Y, joints[5].model.Bounds.Location.Z);
        }

        private void OpenWaveformWindow(object sender, RoutedEventArgs e)
        {
            var waveformWindow = new BoXingTl();
            waveformWindow.Show();
        }
    }
}