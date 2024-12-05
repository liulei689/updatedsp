
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows.Controls;

namespace AFWDPP.Common
{
    public static class Common
    {
        public static void FloatStringToBytes(this byte[] bytes, string floatvalue, int startindex)
        {
            if (int.TryParse(floatvalue, out int re3))
            {
                var data3 = FloatToLittleEndianBytes(re3);
                bytes[startindex] = data3[0];
                bytes[startindex + 1] = data3[1];
                bytes[startindex + 2] = data3[2];
                bytes[startindex + 3] = data3[3];
            }
        }
        public static void String2ToBytes(this byte[] bytes, string value, int startindex)
        {
            if (int.TryParse(value, out int re2))
            {
                var data2 = IntToTwoByteArrayLittleEndian(re2);
                bytes[startindex] = data2[0];
                bytes[startindex + 1] = data2[1];
            }
        }

        public static void ToByte(this byte[] bytes, TextBox textbox)
        {
            var res = textbox.Tag;

            var name = textbox.Name.ToString().ToUpper();
            name = name.Replace("IDC_EDIT_FC_", "");
            string[] index = name.Split('_');
            int[] intArray = new int[index.Length];
            int validIndex = 0; // 用于记录有效转换的索引

            foreach (string str in index)
            {
                if (int.TryParse(str, out int result))
                {
                    intArray[validIndex++] = result;
                }
                else
                    intArray[validIndex++] = -1;
            }
            if (intArray.Length == 1 && intArray[0] >= 0) //hex
                bytes[intArray[0]] = textbox.Text.ToByte();
            if (intArray.Length == 2 && intArray[0] >= 0 && intArray[1] >= 0)
            {
                if (intArray[1] - intArray[0] == 1) //双字节
                {
                    if (int.TryParse(textbox.Text, out int re2))
                    {
                        var data2 = IntToTwoByteArrayLittleEndian(re2);
                        bytes[intArray[0]] = data2[0];
                        bytes[intArray[1]] = data2[1];
                    }
                }
                if (intArray[1] - intArray[0] == 3) //四字节
                {
                    if (int.TryParse(textbox.Text, out int re3))
                    {
                        var data3 = FloatToLittleEndianBytes(re3);
                        bytes[intArray[0]] = data3[0];
                        bytes[intArray[0] + 1] = data3[1];
                        bytes[intArray[0] + 2] = data3[2];
                        bytes[intArray[0] + 3] = data3[3];
                    }
                }
            }


        }
        public static byte[] FloatToLittleEndianBytes(float value)
        {
            // 使用BitConverter将float转换为字节数组（默认是大端序）
            byte[] bytes = BitConverter.GetBytes(value);

            // 检查系统是否使用大端序（通常Windows是小端序，但最好检查一下）
            if (BitConverter.IsLittleEndian)
            {
                // 如果系统已经是小端序，则不需要转换
                return bytes;
            }
            else
            {
                // 如果系统是大端序，则需要反转字节数组
                Array.Reverse(bytes);
                return bytes;
            }
        }
        public static byte[] IntToTwoByteArrayLittleEndian(int value)
        {
            // 由于int通常是4个字节，我们需要确保只取最低的2个字节
            // 我们可以通过与0x00FF进行位与操作来获取最低字节，然后通过右移8位来获取次低字节

            byte lowByte = (byte)(value & 0x00FF);
            byte highByte = (byte)((value >> 8) & 0x00FF);

            // 返回一个包含这两个字节的数组，顺序为小端字节序
            return new byte[] { lowByte, highByte };
        }
        public static byte ToByte(this string hexString)
        {
            // 如果字符串以"0x"开头，则去掉它
            if (hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hexString = hexString.Substring(2);
            }
            if (hexString.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
            {
                hexString = hexString.Substring(2);
            }
            hexString = hexString.Replace("h", "").Replace("H", ""); // 去掉h后缀
            hexString = hexString.Trim();
            // 如果字符串长度不是2，则抛出异常（这里假设输入总是有效的两位十六进制数）
            if (hexString.Length != 2)
            {
                return 0;
            }
            // 检查字符串是否只包含有效的十六进制字符
            foreach (char c in hexString)
            {
                if (!char.IsDigit(c) && !(char.IsLower(c) && (c >= 'a' && c <= 'f')) && !(char.IsUpper(c) && (c >= 'A' && c <= 'F')))
                {
                    return 0; // 发现非法字符
                }
            }
            // 将十六进制字符串转换为字节
            return Convert.ToByte(hexString, 16);
        }
        public enum FrameType : byte
        {
            控制数据帧 = 0x13,    // 控制数据帧（表5）
            目标参数装订帧 = 0x14, // 目标参数装订帧（表6）
            图像模板装订帧 = 0x15   // 图像模板装订帧（表7）
        }


        public static bool IsShowSource = false; //是否显示原始值
        public static string GetGateStatus65(this byte input)
        {
            if (IsShowSource) return "0x" + input.ToString("X2");
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
        public static string GetGateStatus6(this byte input)
        {
            if (IsShowSource) return "0x" + input.ToString("X2");

            switch (input)
            {
                case 0x13:
                    return "宽视场";
                case 0x15:
                    return "窄视场";
                default:
                    return "无效";
            }
        }
        public static string GetGateStatus3(this byte input)
        {
            if (IsShowSource) return "0x" + input.ToString("X2");

            switch (input)
            {
                case 0x13:
                    return "自检正常";
                case 0x15:
                    return "自检异常";
                default:
                    return "无效";
            }
        }
        public static string GetGateStatus7(this byte input)
        {
            if (IsShowSource) return "0x" + input.ToString("X2");

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
            if (IsShowSource) return "0x" + input.ToString("X2");
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
            if (IsShowSource) return "0x" + input.ToString("X2");

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
            if (IsShowSource) return "0x" + input.ToString("X2");

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
