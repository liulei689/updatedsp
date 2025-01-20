using HelixToolkit.Wpf;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp3D
{
    /// <summary>
    /// Plane3D.xaml 的交互逻辑
    /// </summary>
    public partial class Plane3D : UserControl
    {
        public Plane3D()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HelixToolkitTest();
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


        //模型类型：
        // 矩形   （板材 、 槽）
        // 圆     （孔）

    }
}
