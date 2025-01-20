using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using System.Xml.Linq;
using WpfApp3D.Models;
using static System.Net.WebRequestMethods;

namespace WpfApp3D
{
    /// <summary>
    /// MonitorView.xaml 的交互逻辑
    /// </summary>
    public partial class MonitorView : UserControl
    { 
        List<Visual3D> modelsTL = new List<Visual3D>();//上左钻 
        List<Visual3D> modelsTR = new List<Visual3D>();//上右钻
        List<Visual3D> modelsBL = new List<Visual3D>();//下左钻 

        List<ToolInfo> toolTL = new List<ToolInfo>();
        List<ToolInfo> toolTR = new List<ToolInfo>();
        List<ToolInfo> toolBL = new List<ToolInfo>(); 

        List<ToolInfo> toolON = new List<ToolInfo>();

        double topDrillBaseZ = 40;
        double bottomDrillBaseZ = -20;
        double toolONZ = 10;

        double topLDrillOffZ = 0,topRDrillOffZ = 0,bottomLDrillOffZ = 0;//钻包当前偏移Z
         
        int tlBaseNo = 151;
        int trBaseNo = 51;
        int blBaseNo = 1;

        double topLYBase = -20;
        double topRYBase = 10;

        double bottomlYBase = -20;

        public MonitorView()
        {
            InitializeComponent();
            //上左
            int tlNo = tlBaseNo;
            for (int i = 0; i < 6; i++)
            {
                toolTL.Add(new ToolInfo() { PointX = i * 5, PointY = 0, PointZ = topDrillBaseZ, ToolName = $"{tlNo++}" });
            }
            for (int i = 0; i < 7; i++)
            {
                toolTL.Add(new ToolInfo() { PointX = 30, PointY = -1 * i * 5 - 1, PointZ = topDrillBaseZ, ToolName = $"{tlNo++}" });
            }
            toolTL.Add(new ToolInfo() { PointX = 15, PointY = -37, PointZ = topDrillBaseZ, ToolType = "main", ToolName = "181" });

            //上右
            int trNo = trBaseNo;
            for (int i = 0; i < 6; i++)
            {
                toolTR.Add(new ToolInfo() { PointX = i * 5, PointY = 10, PointZ = topDrillBaseZ, ToolName = $"{trNo++}" });
            }
            for (int i = 0; i < 6; i++)
            {
                toolTR.Add(new ToolInfo() { PointX = 30, PointY = 10 + i * 5, PointZ = topDrillBaseZ, ToolName = $"{trNo++}" });
            }
            toolTR.Add(new ToolInfo() { PointX = 23, PointY = 17, PointZ = topDrillBaseZ, ToolType = "main", ToolName = "80" });


            //下
            int blNo = blBaseNo;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    toolBL.Add(new ToolInfo() { PointX = i * 5, PointY = j * 5, PointZ = bottomDrillBaseZ, ToolName = $"{blNo++}" });
                }
            }
            toolBL.Add(new ToolInfo() { PointX = 30, PointY = 0, PointZ = bottomDrillBaseZ, ToolType = "main", ToolName = "30" });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
        }

        void Init()
        {
            for (int i = 0; i < toolTL.Count; i++)
            {
                toolTL[i].PointY += topLYBase;
                CreteDrill3D(toolTL[i], modelsTL);
            }
            for (int i = 0; i < toolTR.Count; i++)
            {
                toolTR[i].PointY += topRYBase;
                CreteDrill3D(toolTR[i], modelsTR);
            }

            for (int i = 0; i < toolBL.Count; i++)
            {
                toolBL[i].PointY += topLYBase;
                CreteDrill3D(toolBL[i], modelsBL);
            }

            //IntiTransform();
        }

        void CreteDrill3D(ToolInfo toolInfo, List<Visual3D> visual3Ds)
        {
            double x = toolInfo.PointX;
            double y = toolInfo.PointY;
            double z = toolInfo.PointZ;
            double secondZ = -5;
            double lenght1 = -10;
            double offzTxt = 1;
            int tNo = 0;
            int.TryParse(toolInfo.ToolName, out tNo);

            if (tNo < 50)//下钻
            {
                lenght1 = 10;
                offzTxt = -2;
                secondZ = -secondZ;
            }
            else if (tNo < 150)//上右钻包
            {
            }
            else//上左钻包 
            {
            }

            ModelUIElement3D element1 = new ModelUIElement3D();
            GeometryModel3D geom = new GeometryModel3D();
            if (toolInfo.ToolType == "main")
            {
                geom.Material = MaterialHelper.CreateMaterial(Brushes.Red, null, null, 1);
            }
            else
            {
                geom.Material = MaterialHelper.CreateMaterial(Brushes.AliceBlue, null, null, 1);
            }

            var builder = new MeshBuilder(false, false);

            Point3D point3D = new Point3D(x, y, z);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, lenght1), 2, 40, true, true);

            point3D = new Point3D(x, y, z + lenght1);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, secondZ), 1, 40, true, true);

            geom.Geometry = builder.ToMesh(true);
            element1.Model = geom;
            element1.SetName(toolInfo.ToolName);
            element1.Transform = new TranslateTransform3D(0, 0, 0);

            visual3Ds.Add(element1);

            TextVisual3D txt3D = CreateTxt(x, y, z + offzTxt, toolInfo.ToolName);
            txt3D.Transform = new TranslateTransform3D(0, 0, 0);
            visual3Ds.Add(txt3D);

            htVp.Children.Add(txt3D);
            htVp.Children.Add(element1);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            for (int i = 0; i < modelsTL.Count; i++)
            {
                (modelsTL[i].Transform as TranslateTransform3D).OffsetY = (double)e.NewValue;
            }
        }
        private void SliderZ1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            topLDrillOffZ = (double)e.NewValue;
            drillZMove(modelsTL, (double)e.NewValue);
        }
        private void SliderZ2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            topRDrillOffZ = (double)e.NewValue;
            drillZMove(modelsTR, (double)e.NewValue);            
        }

        private void SliderZ3_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            bottomLDrillOffZ = (double)e.NewValue;
            drillZMove(modelsBL, (double)e.NewValue,"BOTTOM");
        }

        void drillZMove(List<Visual3D> visual3Ds,double offSetZ,string drillType="TOP")
        {
            for (int i = 0; i < visual3Ds.Count; i++)
            {
                double offZ = 0;
                var tNo = visual3Ds[i].GetName();
                ToolInfo toolInfo = toolON.Where(w => w.ToolName == tNo).FirstOrDefault();
                if (toolInfo != null)
                {
                    offZ = drillType=="TOP"? - toolONZ: toolONZ;//toolInfo.OffSetZ;
                }
                 
                (visual3Ds[i].Transform as TranslateTransform3D).OffsetZ = offZ + offSetZ;
            }
        }

        private void SliderTR_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            for (int i = 0; i < modelsTR.Count; i++)
            {
                (modelsTR[i].Transform as TranslateTransform3D).OffsetY = (double)e.NewValue;
            }
        }
        private void SliderBL_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            for (int i = 0; i < modelsBL.Count; i++)
            {
                (modelsBL[i].Transform as TranslateTransform3D).OffsetY = (double)e.NewValue;
            }
        }


        private void SliderX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
        }
        private void SliderU_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
        }
        private void SliderA_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
        }


        string dirZ;
        private void ToolUpZ_Click(object sender, RoutedEventArgs e)
        {
            dirZ = "ON";
            ZMove(TName.Text);
        }

        private void ToolDownZ_Click(object sender, RoutedEventArgs e)
        {
            dirZ = "OFF";
            int tNo = 0;
            int.TryParse(TName.Text, out tNo);
            if (tNo == 0)
            {
                if (toolON.Count > 0)
                {
                    while (toolON.Count > 0)
                    {
                        ZMove(toolON[0].ToolName);
                    }
                }
            }
            else
            {
                ZMove(tNo.ToString());
            }
        }

        TextVisual3D CreateTxt(double x, double y, double z, string txt)
        {

            TextVisual3D txt3D = new TextVisual3D()
            {
                Text = txt,
                Height = 2,
                Foreground = Brushes.Black,
                Background = Brushes.Transparent,
                //BorderBrush = Brushes.Red,                
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextDirection = new Vector3D(0, 1, 0),
                Position = new Point3D(x, y, z),
                //UpDirection=new Vector3D(-1, 0, 0),//平躺
            };

            return txt3D;
        }

        void ZMove(string toolName)
        {
            if (toolName == "0") { return; }

            double offX = 0, offY = 0, offZ = 0;

            List<Visual3D> models = new List<Visual3D>();
            int tNo = int.Parse(toolName);
            double z = toolONZ;
            string drillType = "TOP";

            if (string.IsNullOrWhiteSpace(dirZ)) { dirZ = "OFF"; }

            TranslateTransform3D transform3D;
            if (tNo < trBaseNo)//下钻trNo
            {
                drillType = "BOTTOM";
                z = dirZ == "OFF" ? -z : z;
                models = modelsBL;
                offZ = bottomLDrillOffZ;
            }
            else if (tNo < tlBaseNo)//上右钻包
            {
                z = dirZ == "OFF" ? z : -z;
                models = modelsTR;
                offZ = topRDrillOffZ;
            }
            else
            {//上左钻包 
                z = dirZ == "OFF" ? z : -z;
                models = modelsTL;
                offZ = topLDrillOffZ;
            }
            Visual3D visual3D = null;
            for (int i = 0; i < models.Count; i++)  
            {
                var txt = models[i].GetName();
                if (txt == toolName)
                {
                    visual3D = models[i];
                    offX = (models[i].Transform as TranslateTransform3D).OffsetX;
                    offY = (models[i].Transform as TranslateTransform3D).OffsetY;
                    //offZ = (models[i].Transform as TranslateTransform3D).OffsetZ;
                    break;
                }
            }

            if (visual3D != null)
            {
                ToolInfo toolInfo = toolON.Where(w => w.ToolName == toolName).FirstOrDefault();
                if (dirZ == "OFF") //收刀
                {
                    if (toolInfo != null)
                    {
                        transform3D = new TranslateTransform3D(offX, offY, offZ);
                        visual3D.Transform = transform3D;// group;

                        toolON.Remove(toolInfo);
                    }
                }
                else //出刀
                { 
                    if (toolInfo == null)
                    {
                        transform3D = new TranslateTransform3D(offX, offY, offZ + z);
                        visual3D.Transform = transform3D;// group;
                        toolInfo = new ToolInfo() { ToolName = toolName, OffSetZ = offZ + z };
                        toolON.Add(toolInfo);
                    }
                }
                txtUseingTool.Text = string.Join(",", toolON.Select(w => w.ToolName).ToList());
            }
        }

        /****************************************************/


        #region createAxis
        /// <summary>
        /// 上钻包1垂直刀
        /// </summary>
        /// <param name="toolInfo"></param>
        /// <returns></returns>
        ModelUIElement3D CreteAxis1(ToolInfo toolInfo)
        {
            double x = toolInfo.PointX;
            double y = toolInfo.PointY;
            double z = toolInfo.PointZ;
            double lenght1 = -10;

            ModelUIElement3D element1 = new ModelUIElement3D();
            GeometryModel3D geom = new GeometryModel3D();
            if (toolInfo.ToolType == "main")
            {
                geom.Material = MaterialHelper.CreateMaterial(Brushes.Red, null, null, 1);
            }
            else
            {
                geom.Material = MaterialHelper.CreateMaterial(Brushes.AliceBlue, null, null, 1);
            }

            var builder = new MeshBuilder(false, false);

            Point3D point3D = new Point3D(x, y, z);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, lenght1), 2, 40, true, true);

            point3D = new Point3D(x, y, z + lenght1);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, -5), 1, 40, true, true);

            geom.Geometry = builder.ToMesh(true);
            element1.Model = geom;
            element1.SetName(toolInfo.ToolName);
            element1.Transform = new TranslateTransform3D(0, 0, 0);
            modelsTL.Add(element1);

            TextVisual3D txt3D = CreateTxt(x, y, z + 1, toolInfo.ToolName);
            htVp.Children.Add(txt3D);
            modelsTL.Add(txt3D);
            return element1;
        }

        /// <summary>
        /// 上钻包2垂直刀
        /// </summary>
        /// <param name="toolInfo"></param>
        /// <returns></returns>
        ModelUIElement3D CreteAxis2(ToolInfo toolInfo)
        {
            double x = toolInfo.PointX;
            double y = toolInfo.PointY;
            double z = toolInfo.PointZ;
            double lenght1 = -10;

            ModelUIElement3D element1 = new ModelUIElement3D();
            GeometryModel3D geom = new GeometryModel3D();
            if (toolInfo.ToolType == "main")
            {
                geom.Material = MaterialHelper.CreateMaterial(Brushes.Red, null, null, 1);
            }
            else
            {
                geom.Material = MaterialHelper.CreateMaterial(Brushes.AliceBlue, null, null, 1);
            }

            var builder = new MeshBuilder(false, false);

            Point3D point3D = new Point3D(x, y, z);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, lenght1), 2, 40, true, true);

            point3D = new Point3D(x, y, z + lenght1);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, -5), 1, 40, true, true);

            geom.Geometry = builder.ToMesh(true);
            element1.Model = geom;
            element1.SetName(toolInfo.ToolName);
            element1.Transform = new TranslateTransform3D(0, 0, 0);

            modelsTR.Add(element1); 

            TextVisual3D txt3D = CreateTxt(x, y, z + 1, toolInfo.ToolName);
            htVp.Children.Add(txt3D);
            modelsTR.Add(txt3D);

            return element1;
        }

        /// <summary>
        /// 下钻包垂直刀
        /// </summary>
        /// <param name="toolInfo"></param>
        /// <returns></returns>
        ModelUIElement3D CreteAxis3(ToolInfo toolInfo)
        {
            double x = toolInfo.PointX;
            double y = toolInfo.PointY;
            double z = toolInfo.PointZ;
            double lenght1 = 10;

            ModelUIElement3D element1 = new ModelUIElement3D();
            GeometryModel3D geom = new GeometryModel3D();
            if (toolInfo.ToolType == "main")
            {
                geom.Material = MaterialHelper.CreateMaterial(Brushes.Blue, null, null, 1);
            }
            else
            {
                geom.Material = MaterialHelper.CreateMaterial(Brushes.LightYellow, null, null, 1);
            }

            var builder = new MeshBuilder(false, false);

            Point3D point3D = new Point3D(x, y, z);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, lenght1), 2, 40, true, true);

            point3D = new Point3D(x, y, z + lenght1);
            builder.AddCylinder(point3D, point3D + new Vector3D(0, 0, 5), 1, 40, true, true);

            geom.Geometry = builder.ToMesh(true);
            element1.Model = geom;
            element1.SetName(toolInfo.ToolName);
            element1.Transform = new TranslateTransform3D(0, 0, 0);
            modelsBL.Add(element1);

            TextVisual3D txt3D = CreateTxt(x, y, z - 2, toolInfo.ToolName);
            htVp.Children.Add(txt3D);
            modelsBL.Add(txt3D);

            return element1;
        }
        #endregion


        void IntiTransform()
        {
            #region 第TL轴旋转
            TranslateTransform3D transform3D = new TranslateTransform3D(0, 0, 0);

            for (int i = 0; i < modelsTL.Count; i++)
            {
                modelsTL[i].Transform = new TranslateTransform3D(0, 0, 0);// group;  
            }
            #endregion

            #region 第TR轴旋转
            //transform3D = new TranslateTransform3D(0,0,0);
            //modelsTR[0].Transform = transform3D;
            //transTR.Add(transform3D);

            //for (int i = 1; i < modelsTL.Count; i++)
            //{
            //    var group = new Transform3DGroup();//使用组是对每个轴进行联动，保证整体旋转一致性
            //    group.Children.Add(transform3D);
            //    modelsTR[i].Transform = group;
            //}

            #endregion

            #region 第BL轴旋转

            for (int i = 0; i < modelsBL.Count; i++)
            {
                modelsBL[i].Transform = new TranslateTransform3D(0, 0, 0);// group; 
            }

            #endregion
        }

    }
}
