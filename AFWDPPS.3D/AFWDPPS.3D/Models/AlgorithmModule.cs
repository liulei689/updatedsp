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
        // 参数记忆：每种算法的参数单独保存
        private static double PID_Kp = 2.0, PID_Ki = 0.05, PID_Kd = 0.1;
        private static double LADRC_wo = 10.0, LADRC_wc = 5.0, LADRC_b0 = 1.0;
        private static double SMC_lambda = 2.0, SMC_eta = 2.0;

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
        private double Kp = PID_Kp;
        private double Ki = PID_Ki;
        private double Kd = PID_Kd;
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
        private double smc_lambda = SMC_lambda;
        private double smc_eta = SMC_eta;

        // LADRC参数
        private double ladrc_wo = LADRC_wo; // 观测器带宽
        private double ladrc_wc = LADRC_wc; // 控制器带宽
        private double ladrc_b0 = LADRC_b0; // 系统增益
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

        // 评价指标统计字段
        private double mseSum = 0;
        private int mseCount = 0;
        private double maxOvershoot = 0;
        private double controlEnergy = 0;

        // 算法切换
        public ControlAlgorithmType AlgorithmType
        {
            get => _algorithmType;
            set
            {
                if (_algorithmType != value)
                {
                    // 切换前保存当前参数
                    SaveCurrentParams(_algorithmType);
                    // 切换后恢复参数
                    RestoreParams(value);
                    _algorithmType = value;
                }
            }
        }
        private ControlAlgorithmType _algorithmType = ControlAlgorithmType.PID;

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

        // 重置评价指标
        public void ResetMetrics()
        {
            mseSum = 0;
            mseCount = 0;
            maxOvershoot = 0;
            controlEnergy = 0;
        }

        // 获取当前评价分数（可自定义加权）
        public double GetCurrentScore()
        {
            double mse = mseCount > 0 ? mseSum / mseCount : 0;
            return mse + 0.01 * controlEnergy + maxOvershoot;
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

            // 评价指标统计
            double target = 0; // 目标值为0
            double error = filteredPitch - target;
            mseSum += error * error;
            mseCount++;
            maxOvershoot = Math.Max(maxOvershoot, Math.Abs(filteredPitch));
            controlEnergy += controlPitch * controlPitch + controlYaw * controlYaw;

            return (controlPitch, controlYaw, speedPitch, speedYaw);
        }
        public void SetPIDParam(string name, double value)
        {
            switch (name)
            {
                case "Kp": Kp = value; break;
                case "Ki": Ki = value; break;
                case "Kd": Kd = value; break;
            }
        }
        public void SetLADRCParam(string name, double value)
        {
            switch (name)
            {
                case "wo": ladrc_wo = value; break;
                case "wc": ladrc_wc = value; break;
                case "b0": ladrc_b0 = value; break;
            }
        }
        public void SetSMCParam(string name, double value)
        {
            switch (name)
            {
                case "lambda": smc_lambda = value; break;
                case "eta": smc_eta = value; break;
            }
        }
        private void SaveCurrentParams(ControlAlgorithmType type)
        {
            switch (type)
            {
                case ControlAlgorithmType.PID:
                    PID_Kp = Kp; PID_Ki = Ki; PID_Kd = Kd;
                    break;
                case ControlAlgorithmType.LADRC:
                    LADRC_wo = ladrc_wo; LADRC_wc = ladrc_wc; LADRC_b0 = ladrc_b0;
                    break;
                case ControlAlgorithmType.SMC:
                    SMC_lambda = smc_lambda; SMC_eta = smc_eta;
                    break;
            }
        }
        private void RestoreParams(ControlAlgorithmType type)
        {
            switch (type)
            {
                case ControlAlgorithmType.PID:
                    Kp = PID_Kp; Ki = PID_Ki; Kd = PID_Kd;
                    break;
                case ControlAlgorithmType.LADRC:
                    ladrc_wo = LADRC_wo; ladrc_wc = LADRC_wc; ladrc_b0 = LADRC_b0;
                    break;
                case ControlAlgorithmType.SMC:
                    smc_lambda = SMC_lambda; smc_eta = SMC_eta;
                    break;
            }
        }
    }
}
