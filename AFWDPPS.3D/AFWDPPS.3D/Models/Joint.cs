using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WpfApp3D.Models
{
    /// <summary>
    /// 连接点
    /// </summary>
    public class Joint
    {
        /// <summary>
        /// 三维模型
        /// </summary>
        public Model3D model = null;
        /// <summary>
        /// 三维模型
        /// </summary>
        public ModelUIElement3D modelUie3D = null;
        /// <summary>
        /// 当前角度
        /// </summary>
        public double angle = 0;
        /// <summary>
        /// 最小角度
        /// </summary>
        public double angleMin = -180;
        /// <summary>
        /// 最大角度
        /// </summary>
        public double angleMax = 180;
        /// <summary>
        /// 旋转中心点X坐标值
        /// </summary>
        public double rotPointX = 0;
        /// <summary>
        /// 旋转中心点Y坐标值
        /// </summary>
        public double rotPointY = 0;
        /// <summary>
        /// 旋转中心点Z坐标值
        /// </summary>
        public double rotPointZ = 0;
        /// <summary>
        /// 围绕X轴旋转
        /// </summary>
        public double rotAxisX = 0;
        /// <summary>
        /// 围绕Y轴旋转
        /// </summary>
        public double rotAxisY = 0;
        /// <summary>
        /// 围绕Z轴旋转
        /// </summary>
        public double rotAxisZ = 0;

        /// <summary>
        /// 三维模型
        /// </summary>
        /// <param name="pModel"></param>
        public Joint(Model3D pModel=null, ModelUIElement3D uieModel=null)
        {
            model = pModel;
            modelUie3D = uieModel;
        }
         

    }
}
