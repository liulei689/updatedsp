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
using System.Windows.Shapes;

namespace WpfApp3D
{
    /// <summary>
    /// SixDrill.xaml 的交互逻辑
    /// </summary>
    public partial class SixDrill : UserControl
    {
        public SixDrill()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //string[] files = System.IO.Directory.GetFiles("./STL-Moduls");
            string[] files = System.IO.Directory.GetFiles("./STL-Moduls");

            // 这个对象是由HelixToolKit工具包提供
            ModelImporter importer = new ModelImporter();
            foreach (string file in files)
            {
                // 加载模型  *.stl   *.obj（贴图材质）   生成一个Group的对象（模型结构）
                Model3DGroup group = importer.Load(file);// 1个子项

                var geo = (group.Children[0] as GeometryModel3D);

                //加载每个模型的时候都会设置这个！！！
                // 设置模型的正面材质
                //geo.Material = MaterialHelper.CreateMaterial(new SolidColorBrush(Color.FromArgb(20, 50, 50, 50)));
                geo.Material = MaterialHelper.CreateMaterial(Brushes.LightGray);
                // 设置模型的背面材质
                //geo.BackMaterial = MaterialHelper.CreateMaterial(new SolidColorBrush(Color.FromArgb(20, 50, 50, 50)));
                geo.BackMaterial = MaterialHelper.CreateMaterial(Brushes.Gray);

                if (file.IndexOf("压板") > -1)
                {
                    geo.Material = MaterialHelper.CreateMaterial(Brushes.Red);
                }
                if (file.IndexOf("夹爪定位杆") > -1)
                {
                    geo.Material = MaterialHelper.CreateMaterial(Brushes.Yellow);
                }
                if (file.IndexOf("下底板") > -1)
                {
                    geo.Material = MaterialHelper.CreateMaterial(Brushes.YellowGreen);
                }
                if (file.IndexOf("垂直鑽栓槽軸") > -1)
                {
                    geo.Material = MaterialHelper.CreateMaterial(Brushes.Blue);
                }
                if (file.IndexOf("迷宮防塵蓋21D") > -1)
                {
                    geo.Material = MaterialHelper.CreateMaterial(Brushes.LightGray);
                }
                //if (file.IndexOf("深沟球轴承") > -1)
                //{
                //    geo.Material = MaterialHelper.CreateMaterial(Brushes.Blue);
                //}

                // 将Group对象放到UI对象里
                ModelUIElement3D model_ui = new ModelUIElement3D();
                model_ui.Model = group;

                // 将UI对象添加到ViewPort3D中
                this.vp.Children.Add(model_ui);

                #region other
                /*
                if (file.IndexOf("装配体1-1 零件1") > -1)
                {
                    // 加载一个绿色的圆柱
                    MeshBuilder meshBuilder = new MeshBuilder();
                    // 圆柱起点、圆柱终点、圆柱的半径
                    double soc = new Random().Next(5, 65);
                    meshBuilder.AddCylinder(new Point3D(0, 0, 0), new Point3D(0, soc, 0), 9.5);

                    GeometryModel3D model = new GeometryModel3D();
                    model.Geometry = meshBuilder.ToMesh();

                    if (soc > 40)
                    {
                        model.Material = MaterialHelper.CreateMaterial(Colors.Green);
                        model.BackMaterial = MaterialHelper.CreateMaterial(Colors.Green);
                    }
                    else if (soc > 20)
                    {
                        model.Material = MaterialHelper.CreateMaterial(Colors.Orange);
                        model.BackMaterial = MaterialHelper.CreateMaterial(Colors.Orange);
                    }
                    else
                    {
                        model.Material = MaterialHelper.CreateMaterial(Colors.Red);
                        model.BackMaterial = MaterialHelper.CreateMaterial(Colors.Red);
                    }

                    ModelUIElement3D model_temp = new ModelUIElement3D();
                    model_temp.Model = model;


                    // 根据电芯模型的位置，进行绿色圆柱的位置调整
                    Rect3D rect3D = group.Bounds;
                    model_temp.Transform = new TranslateTransform3D(rect3D.X + 8.9, rect3D.Y, rect3D.Z + 8.9);

                    this.vp.Children.Add(model_temp);
                }
                */
                #endregion

            }

        }
    }
}
