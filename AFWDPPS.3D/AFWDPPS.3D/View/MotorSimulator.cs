using System;

namespace WpfApp3D.View
{
    /// <summary>
    /// 无刷力矩电机模拟器
    /// 基于附件中的特性描述实现
    /// </summary>
    public class MotorSimulator
    {
        // 电机参数
        private const double J = 0.00045; // 转动惯量 kg·m?
        private const double B = 0.05;   // 阻尼系数 Nms (增加阻尼让震荡快)
        private const double K = 0.08;     // 转矩常数 Nm/A (调整为慢速)
        private const double R0 = 0.1;    // 绕组电阻 ohm
        private const double Alpha = 0.004; // 温度系数 /°C
        private const double Cth = 10.0;  // 热容量 J/°C
        private const double H = 0.1;     // 冷却系数 W/°C

        // 约束
        private const double I_max = 1.0; // 最大电流 A
        private const double Te_max = 0.5; // 最大转矩 Nm
        private const double Omega_max = 100.0; // 最大转速 rad/s
        private const double DeltaT_max = 100.0; // 最大温升 °C

        // 状态变量
        public double Omega { get; private set; } // 转速 rad/s
        public double Theta { get; private set; } // 位置 rad
        public double DeltaT { get; private set; } // 温升 °C
        public double Angle { get; private set; } // 角度 °
        public double GyroOmega { get; private set; } // 陀螺仪角速度 rad/s

        // 输入
        public double I_ctrl { get; set; } // 控制电流 A
        public double TL { get; set; } // 负载转矩 Nm
        public double T_amb { get; set; } // 环境温度 °C

        // 输出
        public double Te { get; private set; } // 电磁转矩 Nm
        public double Ia { get; private set; } // 三相电流a A
        public double Ib { get; private set; } // 三相电流b A
        public double Ic { get; private set; } // 三相电流c A

        // 随机数生成器，用于添加噪声
        private Random random = new Random();

        public MotorSimulator()
        {
            // 初始化状态
            Omega = 0.0;
            Theta = 0.0;
            DeltaT = 0.0;
            Angle = 0.0;
            GyroOmega = 0.0;

            // 默认输入
            I_ctrl = 0.0;
            TL = 0.0;
            T_amb = 25.0; // 室温
        }

        /// <summary>
        /// 更新电机状态，时间步长dt秒
        /// </summary>
        /// <param name="dt">时间步长 s</param>
        public void Update(double dt)
        {
            // 约束输入
            I_ctrl = Math.Max(-I_max, Math.Min(I_max, I_ctrl));

            // 计算电磁转矩 (简化线性模型，考虑温度影响)
            double R = R0 * (1 + Alpha * DeltaT);
            Te = K * I_ctrl * (1 - DeltaT / DeltaT_max); // 温度影响转矩
            Te = Math.Max(-Te_max, Math.Min(Te_max, Te));

            // 简化三相电流 (假设平衡三相)
            double I_phase = Math.Abs(I_ctrl) / Math.Sqrt(3);
            Ia = I_phase;
            Ib = I_phase;
            Ic = I_phase;

            // 更新转速
            double dOmega = dt / J * (Te - TL - B * Omega);
            Omega += dOmega;
            Omega = Math.Max(-Omega_max, Math.Min(Omega_max, Omega));

            // 更新位置
            Theta += dt * Omega;
            Theta = Theta % (2 * Math.PI); // 防止Theta累积太大

            // 更新角度 (度)
            Angle = Theta * (180.0 / Math.PI);
            if (Angle > 180) Angle -= 360;
            if (Angle < -180) Angle += 360;

            // 陀螺仪角速度 (添加小噪声模拟)
            double noise = (random.NextDouble() - 0.5) * 0.01; // ±0.005 rad/s噪声
            GyroOmega = Omega + noise;

            // 计算损耗 (铜损为主)
            double P_loss = (Ia * Ia + Ib * Ib + Ic * Ic) * R;

            // 冷却
            double Q_cool = H * DeltaT;

            // 更新温升
            double dDeltaT = dt / Cth * (P_loss - Q_cool);
            DeltaT += dDeltaT;
            DeltaT = Math.Max(0, Math.Min(DeltaT_max, DeltaT));
        }

        /// <summary>
        /// 重置电机状态
        /// </summary>
        public void Reset()
        {
            Omega = 0.0;
            Theta = 0.0;
            DeltaT = 0.0;
            Angle = 0.0;
            GyroOmega = 0.0;
        }
    }
}