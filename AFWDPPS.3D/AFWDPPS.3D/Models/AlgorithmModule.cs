using System;

namespace WpfApp3D.Models
{
    public enum ControlAlgorithmType
    {
        PID,
        LADRC,
        SMC
    }

    public class AlgorithmModule
    {
        // 卡尔曼滤波参数
        private double kalmanPitchEstimate = 0;
        private double kalmanPitchErrorCov = 1;
        private double kalmanPitchQ = 0.01; // 过程噪声
        private double kalmanPitchR = 0.1;  // 测量噪声

        private double kalmanYawEstimate = 0;
        private double kalmanYawErrorCov = 1;
        private double kalmanYawQ = 0.01;
        private double kalmanYawR = 0.1;

        // PID控制参数
        private double Kp = 2.0;
        private double Ki = 0.05;
        private double Kd = 0.1;
        private double prevPitchError = 0;
        private double prevYawError = 0;
        private double pitchIntegral = 0;
        private double yawIntegral = 0;

        // LADRC参数
        private double ladrc_Kp = 2.0;
        private double ladrc_Kd = 1.0;
        private double ladrc_Ko = 20.0;
        private double pitchESO = 0;
        private double yawESO = 0;

        // SMC参数
        private double smc_lambda = 2.0;
        private double smc_eta = 2.0;

        // LADRC参数
        private double ladrc_wo = 10.0; // 观测器带宽
        private double ladrc_wc = 5.0; // 控制器带宽
        private double ladrc_b0 = 1.0; // 系统增益
        private double dt = 0.01; // 假设时间步，单位秒，根据实际定时器调整
        private double lastControlPitch = 0;
        private double lastControlYaw = 0;

        // Pitch ESO states
        private double pitch_z1 = 0;
        private double pitch_z2 = 0;
        private double pitch_z3 = 0;

        // Yaw ESO states
        private double yaw_z1 = 0;
        private double yaw_z2 = 0;
        private double yaw_z3 = 0;

        // 算法切换
        public ControlAlgorithmType AlgorithmType { get; set; } = ControlAlgorithmType.PID;

        private double KalmanFilter(ref double estimate, ref double errorCov, double Q, double R, double measurement)
        {
            // 预测
            double predEstimate = estimate;
            double predErrorCov = errorCov + Q;
            // 更新
            double K = predErrorCov / (predErrorCov + R);
            estimate = predEstimate + K * (measurement - predEstimate);
            errorCov = (1 - K) * predErrorCov;
            return estimate;
        }

        public (double controlPitch, double controlYaw, double speedPitch, double speedYaw) FilterAndControl(double inputPitch, double inputYaw)
        {
            // 卡尔曼滤波
            double filteredPitch = KalmanFilter(ref kalmanPitchEstimate, ref kalmanPitchErrorCov, kalmanPitchQ, kalmanPitchR, inputPitch);
            double filteredYaw = KalmanFilter(ref kalmanYawEstimate, ref kalmanYawErrorCov, kalmanYawQ, kalmanYawR, inputYaw);

            double controlPitch = 0, controlYaw = 0, speedPitch = 0, speedYaw = 0;
            switch (AlgorithmType)
            {
                case ControlAlgorithmType.PID:
                    {
                        double pitchError = 0 - filteredPitch;
                        double yawError = 0 - filteredYaw;
                        pitchIntegral += pitchError;
                        yawIntegral += yawError;
                        double pitchDerivative = pitchError - prevPitchError;
                        double yawDerivative = yawError - prevYawError;
                        controlPitch = Kp * pitchError + Ki * pitchIntegral + Kd * pitchDerivative;
                        controlYaw = Kp * yawError + Ki * yawIntegral + Kd * yawDerivative;
                        prevPitchError = pitchError;
                        prevYawError = yawError;
                        speedPitch = pitchDerivative;
                        speedYaw = yawDerivative;
                        break;
                    }
                case ControlAlgorithmType.LADRC:
                    {
                        // Pitch LADRC
                        double beta1 = 3 * ladrc_wo;
                        double beta2 = 3 * ladrc_wo * ladrc_wo;
                        double beta3 = ladrc_wo * ladrc_wo * ladrc_wo;
                        double e_pitch = pitch_z1 - filteredPitch;
                        pitch_z1 += dt * (pitch_z2 - beta1 * e_pitch + ladrc_b0 * lastControlPitch);
                        pitch_z2 += dt * (pitch_z3 - beta2 * e_pitch);
                        pitch_z3 += dt * (-beta3 * e_pitch);
                        double u0_pitch = ladrc_wc * ladrc_wc * (0 - pitch_z1) + 2 * ladrc_wc * (0 - pitch_z2);
                        controlPitch = (u0_pitch - pitch_z3) / ladrc_b0;
                        speedPitch = pitch_z2;
                        lastControlPitch = controlPitch;

                        // Yaw LADRC
                        double e_yaw = yaw_z1 - filteredYaw;
                        yaw_z1 += dt * (yaw_z2 - beta1 * e_yaw + ladrc_b0 * lastControlYaw);
                        yaw_z2 += dt * (yaw_z3 - beta2 * e_yaw);
                        yaw_z3 += dt * (-beta3 * e_yaw);
                        double u0_yaw = ladrc_wc * ladrc_wc * (0 - yaw_z1) + 2 * ladrc_wc * (0 - yaw_z2);
                        controlYaw = (u0_yaw - yaw_z3) / ladrc_b0;
                        speedYaw = yaw_z2;
                        lastControlYaw = controlYaw;
                        break;
                    }
                case ControlAlgorithmType.SMC:
                    {
                        double pitchError = 0 - filteredPitch;
                        double yawError = 0 - filteredYaw;
                        double s_pitch = smc_lambda * pitchError + prevPitchError;
                        double s_yaw = smc_lambda * yawError + prevYawError;
                        controlPitch = -smc_lambda * pitchError - smc_eta * Math.Sign(s_pitch);
                        controlYaw = -smc_lambda * yawError - smc_eta * Math.Sign(s_yaw);
                        speedPitch = -smc_lambda * pitchError;
                        speedYaw = -smc_lambda * yawError;
                        prevPitchError = pitchError;
                        prevYawError = yawError;
                        break;
                    }
            }
            return (controlPitch, controlYaw, speedPitch, speedYaw);
        }
    }
}

// Remove any declarations after the class closing brace.