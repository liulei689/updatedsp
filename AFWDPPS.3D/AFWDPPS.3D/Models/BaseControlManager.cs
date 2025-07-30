namespace WpfApp3D.Models
{
    public abstract class BaseControlManager
    {
        // 构造函数
        public BaseControlManager()
        {
            // 初始化逻辑，如果需要
        }
        DataDelay dataDelay = new DataDelay(1); // 延迟为1秒
        protected MotorSimulator motorSimulator = new MotorSimulator();

        // 步骤实现：1. 模拟mu读取平台数据
        /// <summary>
        /// mu读取平台数据
        /// </summary>
        /// <param name="pitch">平台俯仰角度</param>
        /// <param name="yaw">平台滚转角度</param>
        /// <returns>模拟mu读取数据</returns>
        public virtual (double mupitch, double muyaw) Step1_ReadPlatformData(double pitch, double yaw)
        {
            dataDelay.InputData(pitch, yaw);
            // 可以获取延迟后的数据
            var delayedData = dataDelay.GetDelayedData();
            return (delayedData.Pitch, delayedData.Yaw);
        }

        /// <summary>
        /// 通过平台数据控制电机算法
        /// </summary>
        /// <param name="mupitch">mu读取俯仰角度</param>
        /// <param name="muyaw">mu读取滚转角度</param>
        /// <returns>djpitch电机俯仰转动角度 djyaw电机滚转转动角度 speedPitch俯仰角速度 speedYaw滚转角速度</returns>        
        public virtual (double djpitch, double djyaw, double speedPitch, double speedYaw) Step2_ControlMotorAlgorithm(double mupitch, double muyaw)
        {
            return (mupitch, muyaw, 0, 0);
            // 实际实现逻辑
        }

        // 步骤实现：3. 电机受控反馈模拟
        /// <summary>
        /// 电机受控反馈模拟
        /// </summary>
        /// <param name="djpitch">电机俯仰角度指令</param>
        /// <param name="djyaw">电机滚转角度指令</param>
        /// <returns>djpitc_back电机俯仰转动的角度反馈 djyaw_back电机滚转转动的角度反馈</returns>   
        public virtual (double djpitc_back, double djyaw_back) Step3_SimulateMotorFeedback(double djpitch, double djyaw, double speedPitch, double speedYaw)
        {
            return motorSimulator.SimulateFeedback(djpitch, djyaw, speedPitch, speedYaw);
        }

        // 步骤实现：4. 受电机控制后稳定平台角度反馈 
        /// <summary>
        /// 受电机控制后稳定平台角度反馈
        /// </summary>
        /// <param name="djpitc_back">受电机控制后稳定平台角度反馈</param>
        /// <param name="djyaw_back">受电机控制后稳定平台角度反馈</param>
        /// <returns>pitc_back反馈稳定平台的俯仰角度反馈 yaw_back反馈稳定平台的滚转角度反馈</returns>
        public virtual (double pitc_back, double yaw_back) Step4_FeedbackStablePlatformAngle(double pitc, double yaw, double djpitc_back, double djyaw_back)
        {
            return (pitc - djpitc_back, yaw - djyaw_back);
        }
    }
}
