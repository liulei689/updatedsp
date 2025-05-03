namespace WpfApp3D.Models
{
    public abstract class BaseControlManager
    {
        // 构造函数
        public BaseControlManager()
        {
            // 初始化逻辑（如果有需要）
        }
        DataDelay dataDelay = new DataDelay(1); // 设置延迟为1毫秒

        // 基础实现：1. 模拟mu读取平台数据
        /// <summary>
        /// mu读取平台数据
        /// </summary>
        /// <param name="pitch">平台俯仰角度</param>
        /// <param name="yaw">平台横滚角度</param>
        /// <returns>模拟mu读到数据</returns>
        public virtual (double mupitch, double muyaw) Step1_ReadPlatformData(double pitch, double yaw)
        {
            dataDelay.InputData(pitch, yaw);
            // 尝试获取延迟后的数据
            var delayedData = dataDelay.GetDelayedData();
            return (delayedData.Pitch, delayedData.Yaw);


        }

        /// <summary>
        /// 通过平台数据控制电机算法
        /// </summary>
        /// <param name="mupitch">mu读到俯仰角度</param>
        /// <param name="muyaw">mu读到横滚角度</param>
        /// <returns>djpitch：俯仰电机转的角度 djyaw：横滚电机转的角度</returns>        
        public virtual (double djpitch, double djyaw) Step2_ControlMotorAlgorithm(double mupitch, double muyaw)
        {
            return (mupitch, muyaw);
            // 基础实现逻辑
        }

        // 基础实现：3. 电机受控反馈模拟
        /// <summary>
        /// 电机受控反馈模拟
        /// </summary>
        /// <param name="djpitch">电机俯仰角度指令</param>
        /// <param name="djyaw">电机横滚角度指令</param>
        /// <returns>djpitc_back：俯仰电机转的角度反馈 djyaw_back：横滚电机转的角度反馈</returns>   
        public virtual (double djpitc_back, double djyaw_back) Step3_SimulateMotorFeedback(double djpitch, double djyaw)
        {
            return (djpitch, djyaw);
        }

        // 基础实现：4. 受电机控制后稳定平台角度反馈 
        /// <summary>
        /// 受电机控制后稳定平台角度反馈
        /// </summary>
        /// <param name="djpitc_back">受电机控制后稳定平台角度反馈</param>
        /// <param name="djyaw_back">受电机控制后稳定平台角度反馈</param>
        /// <returns>pitc_back：声呐平台的俯仰角度反馈 yaw_back：声呐平台的横滚角度反馈</returns>
        public virtual (double pitc_back, double yaw_back) Step4_FeedbackStablePlatformAngle(double pitc, double yaw, double djpitc_back, double djyaw_back)
        {
            return (pitc - djpitc_back, yaw - djyaw_back);
        }
    }
}
