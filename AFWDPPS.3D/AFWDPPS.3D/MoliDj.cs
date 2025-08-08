using System;

namespace WpfApp3D
{
    public class MoliDj
    {
        public MoliDj() { }
        private static byte _txCounter = 0;   // 帧计数循环

        /// <summary>
        /// 返回 23 字节完整帧，所有字段已按协议顺序赋初值
        /// </summary>
        public static byte[] BuildFrame()
        {
            byte[] buf = new byte[23];

            /* 固定头 */
            buf[0] = 0xAA;          // 包头1
            buf[1] = 0x55;          // 包头2
            buf[2] = _txCounter;    // 帧计数
            buf[3] = 0x16;          // 包长度

            /* 载荷初值 */
            buf[4] = 0x01;          // 机位号：俯仰电机
            buf[5] = 0x00;          // 反馈帧计数
            buf[6] = 0x00;          // 状态字：电机正常
            buf[7] = 0x00;          // 速度（rpm×10）→ 0 rpm
            buf[8] = 0x00;          // 力矩（N·m×10）→ 0 N·m

            /* 3 路电压 0 V */
            buf[9] = 0x00; buf[10] = 0x00;   // A线
            buf[11] = 0x00; buf[12] = 0x00;   // B线
            buf[13] = 0x00; buf[14] = 0x00;   // C线

            /* 3 路电流 0 A */
            buf[15] = 0x00; buf[16] = 0x00;   // A相
            buf[17] = 0x00; buf[18] = 0x00;   // B相
            buf[19] = 0x00; buf[20] = 0x00;   // C相

            /* 角度 0° → 原始值 0 */
            buf[21] = AngleToRaw((float)-2.1);
            var da = RawToAngle(buf[21]);
            /* 校验和：0~21 累加 → 取反+1 */
            byte sum = 0;
            for (int i = 0; i < 22; i++) sum += buf[i];
            buf[22] = (byte)(((~sum) + 1) & 0xFF);
            _txCounter++;   // 帧计数循环
            return buf;
        }
        /// <summary>
        /// 把真实角度 → 单字节原始值
        /// </summary>
        public static byte AngleToRaw(float degree)
        {
            // 限幅到 [-12.7 , 12.7]
            degree = Math.Max(-12.7f, Math.Min(12.7f, degree));

            int raw;
            if (degree < 0)
                raw = 128 + (int)Math.Round(-degree * 10);   // 128..255
            else
                raw = (int)Math.Round(degree * 10);          // 0..127

            return (byte)(raw & 0xFF);
        }

        /// <summary>
        /// 把单字节原始值 → 真实角度
        /// </summary>
        public static float RawToAngle(byte raw)
        {
            if (raw >= 128)
                return -(raw - 128) / 10.0f;   // 负角度
            else
                return raw / 10.0f;            // 正角度
        }
    }
}
