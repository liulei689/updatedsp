using System;

namespace WpfApp3D.Models
{
    public class TransformManager
    {
        // 存储mu俯仰和滚转的当前值和更新时间
        private double currentMupitch = 0;
        private double currentMuyaw = 0;
        private static bool lastUpdateTime = false;
        private static DateTime startwatch = DateTime.Now;
        private const int DelayMilliseconds = 5000; // mu数据延迟传入延时时间（毫秒）

        /// <summary>
        /// 接收来自平台和mu俯仰和滚转参数，模拟电机控制并返回处理后的角度。
        /// </summary>
        /// <param name="pitch">平台俯仰角度。</param>
        /// <param name="yaw">平台滚转角度。</param>
        /// <param name="mupitch">mu俯仰角度。</param>
        /// <param name="muyaw">mu滚转角度。</param>
        /// <returns>处理后的俯仰和滚转角度。</returns>
        public (double pitch, double yaw) CalculateTransform(double pitch, double yaw, double mupitch, double muyaw)
        {
            #region mu数据延迟传入
            if (!lastUpdateTime)
            {
                lastUpdateTime = true;
                startwatch = DateTime.Now;
            }
            // 检查是否应该更新mu角度值
            if ((DateTime.Now - startwatch).TotalMilliseconds >= DelayMilliseconds)
            {
                currentMupitch = mupitch;
                currentMuyaw = muyaw;
            }
            else
            {
                currentMupitch = 0;
                currentMuyaw = 0;
            }
            #endregion
            // 使用当前的mu角度值进行计算
            pitch = pitch + currentMupitch;
            yaw = yaw + currentMuyaw;

            // 返回处理后的参数
            return (pitch, yaw);
        }
    }

}
