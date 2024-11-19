using System.Collections.Generic;
using System.IO.Ports;

namespace AFWDPP.Common
{
    public static class Common
    {
        public static string GetGateStatus(this byte input)
        {
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
                    return "输入值超出范围";
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
