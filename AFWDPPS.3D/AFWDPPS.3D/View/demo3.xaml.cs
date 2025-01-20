using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace WpfApp3D
{
    /// <summary>
    /// demo3.xaml 的交互逻辑
    /// </summary>
    public partial class demo3 : UserControl
    {

        public ObservableCollection<Visual3D> hv3DObj { get; set; }


        double length = 500, width = 300, height = 18;

        List<ModelUIElement3D> models = new List<ModelUIElement3D>();

        public demo3()
        {
            InitializeComponent();
            //this.hv3DObj = new ObservableCollection<Visual3D>();

            //hv3DObj.Add(new DefaultLights()); //光源
            //Brush brush = new SolidColorBrush(Colors.Yellow);
            //hv3DObj.Add(new BoxVisual3D { Center = new Point3D(length, width, height), Fill = brush, Length = length, Width = width, Height = height });
            viewport.Visibility = Visibility.Collapsed;
            htVp.Visibility = Visibility.Collapsed;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //CreatePlane();

            //HelixToolkitTest();
            CreateGround();

            IntiTransform();
        }


        private Point3D GetCenterPoint(int x, int y, int z)
        {
            return new Point3D(x / 2, y / 2, z / 2);
        }

        void HelixToolkitTest()
        {
            //模型为透视模式

            ModelUIElement3D modelUIElement3D = new ModelUIElement3D();
            MeshBuilder meshBuilder = new MeshBuilder();


            meshBuilder.AddBox(new Point3D(0, 0, 0), 50, 30, 1.8);//矩形


            Point3D pointCyl = new Point3D(50, 30, 0);
            meshBuilder.AddCylinder(pointCyl, pointCyl + new Vector3D(50, 30, 1), 6, 2, true, true);//圆柱  起点，终点 直径 表面细分

            GeometryModel3D geom = new GeometryModel3D();
            geom.Geometry = meshBuilder.ToMesh(true);

            geom.Material = MaterialHelper.CreateMaterial(ColorHelper.HexToColor("#f9e2b8"));//Colors.Orange

            modelUIElement3D.Model = geom;

            htVp.Children.Add(modelUIElement3D);


            //
            //圆柱  起点，终点 直径 表面细分
            //meshBuilder.AddCylinder(new Point3D(0,15,0),new Point3D(0,15,10),6,10);
            //ModelUIElement3D modelUIElement3D2 = new ModelUIElement3D();
            //MeshBuilder meshBuilder2 = new MeshBuilder();
            ////meshBuilder2.AddSphere(new Point3D(0,0,0),3,20); //圆
            //meshBuilder2.AddCylinder(new Point3D(0, 15, 0), new Point3D(0, 15, 10), 6, 10);
            //GeometryModel3D geom2 = new GeometryModel3D();
            //geom2.Material = MaterialHelper.CreateMaterial(Brushes.Red,null,null,1);
            //geom2.BackMaterial = MaterialHelper.CreateMaterial(Brushes.Green, null, null, 1);
            //modelUIElement3D2.Model = geom2;

            //htVp.Children.Add(modelUIElement3D2);


            htVp.Visibility = Visibility.Visible;
        }

        #region 机械臂

        void CreateGround()
        {
            ModelUIElement3D elemet = new ModelUIElement3D();

            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = MaterialHelper.CreateMaterial(Brushes.LightGray, null, null, 1);
            geometry.BackMaterial = MaterialHelper.CreateMaterial(Brushes.LightGray, null, null, 1);

            //地平面
            var builder = new MeshBuilder(false,false);
            builder.AddQuad(new Point3D(100, 100, 0), new Point3D(-100, 100, 0), new Point3D(-100, -100, 0), new Point3D(100, -100, 0));
            
            geometry.Geometry = builder.ToMesh(true);
            elemet.Model = geometry;

            htVp.Children.Add(elemet);

            CreateBase();
        }


        void CreateBase()
        {
            ModelUIElement3D elemet = new ModelUIElement3D();
            GeometryModel3D geom = new GeometryModel3D();
            geom.Material = MaterialHelper.CreateMaterial(Brushes.Gray, null, null, 1);

            var builder = new MeshBuilder(false, false);

            Point3D point3D = new Point3D(0, 0, 0);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, 1), 10, 40, true, true); //第一个底坐

            point3D = new Point3D(0, 0, 1);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, 1), 8, 40, true, true); //第二个底坐

            geom.Geometry = builder.ToMesh(true);
            elemet.Model = geom;

            htVp.Children.Add(elemet);
            htVp.Visibility = Visibility.Visible;

            CreteAxis1();
            CreteAxis2();
            CreteAxis3();
            CreteAxis4();
        }


        void CreteAxis1()
        {
            ModelUIElement3D element1 = new ModelUIElement3D();
            GeometryModel3D geom = new GeometryModel3D();
            geom.Material = MaterialHelper.CreateMaterial(Brushes.Silver, null, null, 1);

            var builder = new MeshBuilder(false, false);

            Point3D point3D = new Point3D(0, 0, 2);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, 18), 8, 40, true, true); //第一个轴直柱

            point3D = new Point3D(0, 0, 11);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 10, 0), 8, 40, true, true); //第一个轴平申

            geom.Geometry = builder.ToMesh(true);
            element1.Model = geom;

            models.Add(element1);
            htVp.Children.Add(element1);
            htVp.Visibility = Visibility.Visible;
        }

        ModelUIElement3D element2 = new ModelUIElement3D();
        void CreteAxis2()
        {
            GeometryModel3D geom = new GeometryModel3D();
            geom.Material = MaterialHelper.CreateMaterial(Brushes.AliceBlue, null, null, 1);

            var builder = new MeshBuilder(false, false);

            Point3D point3D = new Point3D(0, 10, 11);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 18, 0), 8, 40, true, true);

            point3D = new Point3D(0, 19, 11);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, 10), 8, 40, true, true);

            point3D = new Point3D(0, 19, 21);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, 0.5), 6, 40, true, true);

            point3D = new Point3D(0, 19, 21.5);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, 35), 5.5, 40, true, true);

            point3D = new Point3D(0, 19, 46.5);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, 0.5), 6, 40, true, true);

            point3D = new Point3D(0, 19, 47);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, 10), 8, 40, true, true);

            point3D = new Point3D(0, 8, 57);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 20, 0), 8, 40, true, true);

            geom.Geometry = builder.ToMesh(true);
            element2.Model = geom;

            models.Add(element2);
            htVp.Children.Add(element2);
            htVp.Visibility = Visibility.Visible;
        }

        ModelUIElement3D element3 = new ModelUIElement3D();
        void CreteAxis3()
        {
            GeometryModel3D geom = new GeometryModel3D();
            geom.Material = MaterialHelper.CreateMaterial(Brushes.Silver, null, null, 1);

            var builder = new MeshBuilder(false, false);
            //第一节
            Point3D point3D = new Point3D(0, 8, 57);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, -18, 0), 8, 40, true, true);
            //第二节
            point3D = new Point3D(0, -1, 57);
            builder.AddCylinder(point3D, point3D + new Vector3D(8, 0, 0), 8, 40, true, true);
           
            //第三节 小节口
            point3D = new Point3D(8, -1, 57);//中心点
            builder.AddCylinder(point3D, point3D + new Vector3D(0.5, 0, 0), 6, 40, true, true);
            //第四节
            point3D = new Point3D(8.5, -1, 57);
            builder.AddCylinder(point3D, point3D + new Vector3D(40, 0, 0), 5.5, 40, true, true);
            
            //第五节
            point3D = new Point3D(48.5, -7, 57);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 15, 0), 5.5, 40, true, true);


            geom.Geometry = builder.ToMesh(true);
            element3.Model = geom;

            models.Add(element3);
            htVp.Children.Add(element3);
            htVp.Visibility = Visibility.Visible;
        }

        void CreteAxis4()
        {
            ModelUIElement3D element = new ModelUIElement3D();
            GeometryModel3D geom = new GeometryModel3D();
            geom.Material = MaterialHelper.CreateMaterial(Brushes.LightSteelBlue, null, null, 1);

            var builder = new MeshBuilder(false, false);
            //第一节
            Point3D point3D = new Point3D(48.5, 8, 57);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 14, 0), 5.5, 40, true, true);
            //第二节
            point3D = new Point3D(48.5, 16, 57);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, -8), 5.5, 40, true, true);

            geom.Geometry = builder.ToMesh(true);
            element.Model = geom;

            models.Add(element);
            htVp.Children.Add(element);
            htVp.Visibility = Visibility.Visible;

            CreteAxis5();
        }

        void CreteAxis5()
        {
            ModelUIElement3D element = new ModelUIElement3D();
            GeometryModel3D geom = new GeometryModel3D();
            geom.Material = MaterialHelper.CreateMaterial(Brushes.Silver, null, null, 1);

            var builder = new MeshBuilder(false, false);
            //第一节
            Point3D point3D = new Point3D(48.5, 16, 49);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, -8), 5.5, 40, true, true);
            //第二节 X Z
            point3D = new Point3D(41.5, 16, 41);
            builder.AddCylinder(point3D, point3D + new Vector3D(14, 0, 0), 5.5, 40, true, true);

            geom.Geometry = builder.ToMesh(true);
            element.Model = geom;

            models.Add(element);
            htVp.Children.Add(element);
            htVp.Visibility = Visibility.Visible;

            CreteAxis6();
        }

        void CreteAxis6()
        {
            ModelUIElement3D element = new ModelUIElement3D();
            GeometryModel3D geom = new GeometryModel3D();
            geom.Material = MaterialHelper.CreateMaterial(Brushes.Red, null, null, 1);

            var builder = new MeshBuilder(false, false);
            //第一节
            Point3D point3D = new Point3D(55.5, 16, 41);
            builder.AddCylinder(point3D, point3D + new Vector3D(1.5, 0, 0), 5, 40, true, true);
            //第二节 X Z
            point3D = new Point3D(57, 16, 41);//中心点
            Point3D p0 = new Point3D(57, 14, 41);
            Point3D p1 = new Point3D(57, 18, 41);
            Point3D p2 = new Point3D(57, 16, 30);
            //for(int i = 0; i < 100; i++)
            //{
            //    p0 = p0 + new Vector3D(0.01, 0, 0);
            //    p1 = p1 + new Vector3D(0.01, 0, 0);
            //    p2 = p2 + new Vector3D(0.01, 0, 0);
            //    builder.AddTriangle(p2, p1, p0);
            //}
            //Point uv0 = new Point(2, 14);
            //Point uv1 = new Point(2, 18);
            //Point uv2 = new Point(2, 16);
            //builder.AddTriangle(p2, p1, p0);
            //builder.AddTriangle(p2, p1, p0, uv0, uv1, uv2);
            //builder.AddCubeFace(point3D, new Vector3D(5, 10, 20), new Vector3D(2, 0, 0), 10, 15, 25);


            builder.AddCone(point3D, point3D+new Vector3D(0, 0, -10), 1, true, 40);

            geom.Geometry = builder.ToMesh(true);
            element.Model = geom;

            models.Add(element);
            htVp.Children.Add(element);
            htVp.Visibility = Visibility.Visible;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int index = int.Parse((sender as Slider).Tag.ToString());

            (trans[index - 1].Rotation as AxisAngleRotation3D).Angle = (float)e.NewValue;


            //原点Z轴旋转
            //RotateTransform3D transform1 = new RotateTransform3D()
            //{
            //    //按点（0，0，0）旋转，对应的角度为 e.NewValue ，对应的轴是 Vector3D(0,0,1) Z轴
            //    Rotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), e.NewValue),
            //    CenterX = 0,
            //    CenterY = 0,
            //    CenterZ = 0,
            //};


            ////元素旋转
            //for (int i = index-1; i < models.Count; i++)
            //{
            //    models[i].Transform = transform1;
            //}


            //RotateTransform3D transform2 = new RotateTransform3D()
            //{
            //    //按点（0，0，0）旋转，对应的角度为 e.NewValue ，对应的轴是 Vector3D(0,0,1) Z轴
            //    Rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), e.NewValue),
            //    CenterX = 0,
            //    CenterY = 0,
            //    CenterZ = 0,
            //};
        }

        /// <summary>
        /// 旋转对象
        /// </summary>
        RotateTransform3D IntiTransform(double x, double y, double z, double rotation)
        {
            //轴的起始点 CenterX CenterY CenterZ

            RotateTransform3D transform2 = new RotateTransform3D()
            {
                //按点（0，0，0）旋转，对应的角度为 e.NewValue ，对应的轴是 Vector3D(0,0,1) Z轴
                Rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), rotation),
                CenterX = x,
                CenterY = y,
                CenterZ = z,
            };

            return transform2;
        }

        List<RotateTransform3D> trans = new List<RotateTransform3D>();
        void IntiTransform()
        {
            #region 第一轴旋转
            //轴的起始点 CenterX CenterY CenterZ  第一轴旋转
            RotateTransform3D transform1 = new RotateTransform3D()
            {
                //按点（0，0，2）旋转，对应的角度为 e.NewValue ，对应的轴是 Vector3D(0,0,1) Z轴
                Rotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0),
                CenterX = 0,
                CenterY = 0,
                CenterZ = 2,
            };
            models[0].Transform = transform1;
            trans.Add(transform1);

            for (int i = 1; i < models.Count; i++)
            {
                var group = new Transform3DGroup();//使用组是对每个轴进行联动，保证整体旋转一致性
                group.Children.Add(transform1);
                models[i].Transform = group;
            }
            #endregion

            #region 第二轴
            RotateTransform3D transform2 = new RotateTransform3D()
            {
                //按点（0，0，0）旋转，对应的角度为 e.NewValue ，对应的轴是 Vector3D(0,0,1) Y轴
                Rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0),
                CenterX = 0,
                CenterY = 0,
                CenterZ = 11,
            };

            for (int i = 1; i < models.Count; i++)
            {
                (models[i].Transform as Transform3DGroup).Children.Insert(0, transform2);//使用Insert：
                //(models[i].Transform as Transform3DGroup).Children.Add(transform2);
            }

            trans.Add(transform2);
            #endregion

            #region 第三轴
            RotateTransform3D transform3 = new RotateTransform3D()
            {
                //按点（0，0，0）旋转，对应的角度为 e.NewValue ，对应的轴是 Vector3D(0,0,1) Y轴
                Rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0),
                CenterX = 0,
                CenterY = 0,
                CenterZ = 57,
            };

            for (int i = 2; i < models.Count; i++)
            {
                (models[i].Transform as Transform3DGroup).Children.Insert(0, transform3);
                //(models[i].Transform as Transform3DGroup).Children.Add(transform3);
            }

            trans.Add(transform3);
            #endregion

            #region 第四轴
            RotateTransform3D transform4 = new RotateTransform3D()
            {
                //按点（0，0，0）旋转，对应的角度为 e.NewValue ，对应的轴是 Vector3D(0,0,1) Y轴  (48.5, 8, 57);
                Rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0),
                CenterX = 48.5,
                CenterY = 8,
                CenterZ = 57,
            };

            for (int i = 3; i < models.Count; i++)
            {
                (models[i].Transform as Transform3DGroup).Children.Insert(0, transform4); 
            }

            trans.Add(transform4);
            #endregion

            #region 第五轴
            RotateTransform3D transform5 = new RotateTransform3D()
            {
                //按点（0，0，0）旋转，对应的角度为 e.NewValue ，对应的轴是 Vector3D(0,0,1) Z轴  (48.5, 16, 49)
                Rotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0),
                CenterX = 48.50,
                CenterY = 16,
                CenterZ = 49,
            };

            for (int i = 4; i < models.Count; i++)
            {
                (models[i].Transform as Transform3DGroup).Children.Insert(0, transform5);
            }

            trans.Add(transform5);
            #endregion

            #region 第六轴
            transform5 = new RotateTransform3D()
            {
                //按点（0，0，0）旋转，对应的角度为 e.NewValue ，对应的轴是 Vector3D(0,0,1) X轴  (55.5, 16, 41)
                Rotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0),
                CenterX = 55,
                CenterY = 16,
                CenterZ = 41,
            };

            for (int i = 5; i < models.Count; i++)
            {
                (models[i].Transform as Transform3DGroup).Children.Insert(0, transform5);
            }

            trans.Add(transform5);
            #endregion

        }

        #endregion

        void CreatePlane()
        {

            ModelVisual3D modelVisual3D = new ModelVisual3D();
            GeometryModel3D geometry = new GeometryModel3D();
            geometry.Material = new DiffuseMaterial() { Brush = Brushes.Yellow };
            geometry.Geometry = new MeshGeometry3D()
            {
                Positions = new Point3DCollection()
                {
                    new Point3D(2.5,0,0.5),
                    new Point3D(-2.5,0,0.5),
                    new Point3D(2.5,2,0.5),
                    new Point3D(-2.5,2,0.5)
                },
                TriangleIndices = new Int32Collection(new[] { 0, 2, 1, 1, 2, 3 }),
            };

            modelVisual3D.Content = geometry;


            AxisAngleRotation3D axisAngle = new AxisAngleRotation3D();//旋转
            axisAngle.Angle = 45;
            axisAngle.Axis = new Vector3D(0, 0, 1);
            Point3D point3D = new Point3D(0, 0, 0);

            RotateTransform3D rotationTransform = new RotateTransform3D(axisAngle, point3D);
            modelVisual3D.Transform = rotationTransform;

            viewport.Children.Add(modelVisual3D);

            viewport.Visibility = Visibility.Visible;
        }
    }
}
