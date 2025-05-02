using System;

namespace WpfApp3D.Models
{

    /// <summary>
    /// 正玄波生成器
    /// </summary>
    public class SimplifiedSineWaveGenerator
    {
        private double amplitude;  // 信号的幅值
        private double frequency;  // 信号的频率
        private double phase;      // 相位偏移
        private double currentTime; // 当前时间
        private double timeStep;   // 时间步长，用于控制数据生成的间隔

        // 构造函数
        public SimplifiedSineWaveGenerator(double amplitude, double frequency, double timeStep = 0.01)
        {
            this.amplitude = amplitude;
            this.frequency = frequency;
            this.phase = 0;
            this.currentTime = 0;
            this.timeStep = timeStep; // 默认时间步长为 0.01 秒
        }

        // 生成下一个正弦波值
        public double GenerateNextValue()
        {
            double value = amplitude * Math.Sin(2 * Math.PI * frequency * currentTime + phase);
            currentTime += timeStep; // 使用默认时间步长递增时间
            return value;
        }

        // 设置幅值
        public void SetAmplitude(double amplitude)
        {
            this.amplitude = amplitude;
        }

        // 设置频率
        public void SetFrequency(double frequency)
        {
            this.frequency = frequency;
        }

        // 设置相位偏移
        public void SetPhase(double phase)
        {
            this.phase = phase;
        }

        // 设置时间步长
        public void SetTimeStep(double timeStep)
        {
            this.timeStep = timeStep;
        }

        // 重置生成器，从头开始生成数据
        public void Reset()
        {
            currentTime = 0;
        }
    }
}
