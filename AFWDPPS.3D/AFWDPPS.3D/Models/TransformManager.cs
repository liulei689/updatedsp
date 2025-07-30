using System;

namespace WpfApp3D.Models
{
    public class TransformManager : BaseControlManager
    {
        // 存储mu俯仰和滚转的当前值和更新时间
        private double currentMupitch = 0;
        private double currentMuyaw = 0;
        private static bool lastUpdateTime = false;
        private static DateTime startwatch = DateTime.Now;
        private const int DelayMilliseconds = 5000; // mu数据延迟传入延时时间（毫秒）
        private AlgorithmModule algorithm = new AlgorithmModule();
        private ControlAlgorithmType currentAlgorithmType = ControlAlgorithmType.PID; // 默认使用PID算法

        public void SetAlgorithmType(ControlAlgorithmType type)
        {
            this.currentAlgorithmType = type;
        }

        public override (double djpitch, double djyaw, double speedPitch, double speedYaw) Step2_ControlMotorAlgorithm(double mupitch, double muyaw)
        {
            #region 模拟mu需要一定时间初始化，数据延迟传入
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

            algorithm.AlgorithmType = this.currentAlgorithmType;
            var (controlPitch, controlYaw, speedPitch, speedYaw) = algorithm.FilterAndControl(currentMupitch, currentMuyaw);
            // 可以在这里处理或记录speedPitch和speedYaw，如果需要
            return (controlPitch, controlYaw, speedPitch, speedYaw);
        }
    }
}
