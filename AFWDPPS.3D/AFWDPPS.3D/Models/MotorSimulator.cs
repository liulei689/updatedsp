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
        private double K_t = 0.1;      // 扭矩常数 (Nm/A)，增加以提高输出
        private double m_load = 5.0;    // 负载质量 (kg)
        private double g = 9.81;        // 重力加速度 (m/s^2)
        private double l_arm = 0.5;     // 假设臂长 (m)，可根据实际调整
        private double J = 0.01;        // 基础转动惯量 (kg.m^2)
        private double b = 0.001;       // 阻尼系数 (Nm.s/rad)
        private double max_voltage = 24;  // 最大电压 (V)
        private double max_current = 10;  // 最大电流 (A)
        private double rho_water = 1025.0;  // 海水密度 (kg/m^3)
        private double volume_load = 0.005; // 假设负载体积 (m^3)，可根据实际调整
        private double b_water = 0.1;       // 水阻尼系数 (Nm.s/rad)，可调整

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
            return (torque_motor - b * omega - b_water * omega - torque_load) / J;
        }

        public (double, double) SimulateFeedback(double targetPitch, double targetYaw, double speedPitch, double speedYaw)
        {

            return (targetPitch, targetYaw);
            double dt = 1.0; // 时间步长，修改为1

            double V_pitch = speedPitch * 100; // 示例映射：速度转换为电压，增加系数
            double V_yaw = speedYaw * 100;

            // 添加日志
            Console.WriteLine($"V_pitch: {V_pitch}, V_yaw: {V_yaw}");

            // 更新俯仰
            double di_dt_pitch = ElectricalDynamics(V_pitch, i_pitch, omega_pitch);
            double F_b = rho_water * g * volume_load;  // 浮力
            double effective_weight = m_load * g - F_b;
            double torque_load_pitch = effective_weight * l_arm * Math.Sin(theta_pitch);  // 更新俯仰负载扭矩考虑浮力
            double torque_load_yaw = 0.0;  // 假设偏航无重力扭矩
            double domega_dt_pitch = MechanicalDynamics(i_pitch, omega_pitch, torque_load_pitch);
            i_pitch += di_dt_pitch * dt;
            omega_pitch += domega_dt_pitch * dt;
            theta_pitch += omega_pitch * dt;

            // 更新偏航
            double di_dt_yaw = ElectricalDynamics(V_yaw, i_yaw, omega_yaw);
            double domega_dt_yaw = MechanicalDynamics(i_yaw, omega_yaw, torque_load_yaw);
            i_yaw += di_dt_yaw * dt;
            omega_yaw += domega_dt_yaw * dt;
            theta_yaw += omega_yaw * dt;

            // 限制电压和电流
            V_pitch = Math.Max(-max_voltage, Math.Min(max_voltage, V_pitch));
            i_pitch = Math.Max(-max_current, Math.Min(max_current, i_pitch));
            V_yaw = Math.Max(-max_voltage, Math.Min(max_voltage, V_yaw));
            i_yaw = Math.Max(-max_current, Math.Min(max_current, i_yaw));

            // 添加噪声
            double noisePitch = (random.NextDouble() * 2 - 1) * 0.01;
            double noiseYaw = (random.NextDouble() * 2 - 1) * 0.01;

            // 日志
            Console.WriteLine($"theta_pitch: {theta_pitch + noisePitch}, theta_yaw: {theta_yaw + noiseYaw}");

            return (theta_pitch + noisePitch, theta_yaw + noiseYaw);
        }
        public MotorSimulator()
        {
            // 在构造函数中更新转动惯量，包括负载
            J = J + m_load * l_arm * l_arm;
        }
    }
}