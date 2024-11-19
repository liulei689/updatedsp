using System.Collections.Generic;
using System.IO.Ports;

namespace AFWDPP.Common
{
    public static class Common
    {
        public static bool IsShowSource = false; //是否显示原始值
        public static string GetGateStatus65(this byte input)
        {
            if (IsShowSource) return input.ToString("X2");
            switch (input)
            {
                case 0:
                    return "默认";
                case 1:
                    return "波门变大";
                case 2:
                    return "波门缩小";
                case 3:
                    return "波门目标重选";
                default:
                    return "未知";
            }
        }

        public static string GetGateStatus7(this byte input)
        {
            if (IsShowSource) return input.ToString("X2");

            switch (input)
            {
                case 0x13:
                    return "不叠加";
                case 0x15:
                    return "叠加";
                default:
                    return "无效";
            }
        }

        // 扩展方法，用于获取目标跟踪状态的描述
        public static string GetGateStatus10(this byte input)
        {
            if (IsShowSource) return input.ToString("X2");
            switch (input)
            {
                case 0x13:
                    return "未捕获";
                case 0x15:
                    return "捕获";
                case 0x16:
                    return "跟踪";
                case 0x19:
                    return "人工引导";
                case 0x1A:
                    return "记忆跟踪";
                default:
                    return "未知";
            }
        }

        public static string GetGateStatus51(this byte input)
        {
            if (IsShowSource) return input.ToString("X2");

            switch (input)
            {
                case 0x11:
                    return "人员";
                case 0x12:
                    return "车辆";
                case 0x13:
                    return "工事";
                default:
                    return "无效";
            }
        }
        public static string GetGateStatus52(this byte input)
        {
            if (IsShowSource) return input.ToString("X2");

            switch (input)
            {
                case 0x11:
                    return "输出";
                case 0x12:
                    return "不输出";
                default:
                    return input.ToString("X2");
            }
        }
        public static bool CheckSPsum(byte[] data)
        {
            int i = 0;
            int result = 0;
            for (i = 0; i < data.Length - 1; i++)
            {
                result += data[i];
            }
            result &= 0x00FF;
            if (data[data.Length - 1] == result)
                return true;
            else
                return false;
        }
        public static IList<string> SearchPort()
        {
            return [.. SerialPort.GetPortNames()];
        }
    }
}
