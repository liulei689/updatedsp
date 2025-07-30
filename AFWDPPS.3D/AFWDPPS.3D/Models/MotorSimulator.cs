using System;
using System.Collections.Generic;

namespace WpfApp3D.Models
{
    public class MotorSimulator
    {
        // 电机参数
        private double R = 1.0;         // 电阻 (Ω)
        private double L = 0.5;         // 电感 (H)
        private double K_e = 0.01;      // 反电动势常数 (V.s/rad)
        private double K_t = 0.01;      // 扭矩常数 (Nm/A)
        private double J = 0.01;        // 转动惯量 (kg.m^2)
        private double b = 0.001;       // 阻尼系数 (Nm.s/rad)
        private double max_voltage = 24;  // 最大电压 (V)
        private double max_current = 10;  // 最大电流 (A)

        // 状态变量（针对俯仰和偏航分别维护）
        private double i_pitch = 0.0;   // 俯仰电流 (A)
        private double omega_pitch = 0.0;  // 俯仰角速度 (rad/s)
        private double theta_pitch = 0.0;  // 俯仰角位移 (rad)
        private double i_yaw = 0.0;     // 偏航电流 (A)
        private double omega_yaw = 0.0;    // 偏航角速度 (rad/s)
        private double theta_yaw = 0.0;    // 偏航角位移 (rad)

        private Random random = new Random();

        // 非线性反电动势模型
        private double NonlinearEmf(double omega)
        {
            return K_e * Math.Tanh(omega);
        }

        // 电机的电气部分：电流与电压的关系
        private double ElectricalDynamics(double V, double i, double omega)
        {
            double E_b = NonlinearEmf(omega);
            return (V - R * i - E_b) / L;
        }

        // 电机的机械部分：转矩与角速度的关系
        private double MechanicalDynamics(double i, double omega, double torque_load)
        {
            double torque_motor = K_t * i;
            return (torque_motor - b * omega - torque_load) / J;
        }

        public (double, double) SimulateFeedback(double targetPitch, double targetYaw, double speedPitch, double speedYaw)
        {
            //return (targetPitch, targetYaw);
            double dt = 0.001; // 时间步长
            double T_total = 0.05; // 模拟总时间（秒），可调整
            int time_steps = (int)(T_total / dt);

            // 假设targetPitch和targetYaw为目标位置，speed为期望速度
            // 这里简化：将控制输入映射为电压V，假设负载扭矩为0
            // 对于实际应用，可能需要根据误差计算V
            double V_pitch = speedPitch * 10; // 示例映射：速度转换为电压
            double V_yaw = speedYaw * 10;
            double torque_load = 0.0;

            for (int t = 0; t < time_steps; t++)
            {
                // 更新俯仰
                double di_dt_pitch = ElectricalDynamics(V_pitch, i_pitch, omega_pitch);
                double domega_dt_pitch = MechanicalDynamics(i_pitch, omega_pitch, torque_load);
                i_pitch += di_dt_pitch * dt;
                omega_pitch += domega_dt_pitch * dt;
                theta_pitch += omega_pitch * dt;

                // 更新偏航
                double di_dt_yaw = ElectricalDynamics(V_yaw, i_yaw, omega_yaw);
                double domega_dt_yaw = MechanicalDynamics(i_yaw, omega_yaw, torque_load);
                i_yaw += di_dt_yaw * dt;
                omega_yaw += domega_dt_yaw * dt;
                theta_yaw += omega_yaw * dt;

                // 限制电压和电流
                V_pitch = Math.Max(-max_voltage, Math.Min(max_voltage, V_pitch));
                i_pitch = Math.Max(-max_current, Math.Min(max_current, i_pitch));
                V_yaw = Math.Max(-max_voltage, Math.Min(max_voltage, V_yaw));
                i_yaw = Math.Max(-max_current, Math.Min(max_current, i_yaw));
            }

            // 添加噪声
            double noisePitch = (random.NextDouble() * 2 - 1) * 0.01;
            double noiseYaw = (random.NextDouble() * 2 - 1) * 0.01;
            return (theta_pitch + noisePitch, theta_yaw + noiseYaw);
        }
    }
}