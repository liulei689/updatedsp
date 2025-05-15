
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AFWDPP.Common
{
    public static class Common
    {


        /*************************************************************
                       crc校验函数查表
        *************************************************************/
        static byte[] crc_table =
        { 0x00, 0x31, 0x62, 0x53, 0xC4, 0xF5, 0xA6, 0x97,
    0xB9, 0x88, 0xDB, 0xEA, 0x7D, 0x4C, 0x1F, 0x2E,
    0x43, 0x72, 0x21, 0x10, 0x87, 0xB6, 0xE5, 0xD4,
    0xFA, 0xCB, 0x98, 0xA9, 0x3E, 0x0F, 0x5C, 0x6D,
    0x86, 0xB7, 0xE4, 0xD5, 0x42, 0x73, 0x20, 0x11,
    0x3F, 0x0E, 0x5D, 0x6C, 0xFB, 0xCA, 0x99, 0xA8,
    0xC5, 0xF4, 0xA7, 0x96, 0x01, 0x30, 0x63, 0x52,
    0x7C, 0x4D, 0x1E, 0x2F, 0xB8, 0x89, 0xDA, 0xEB,
    0x3D, 0x0C, 0x5F, 0x6E, 0xF9, 0xC8, 0x9B, 0xAA,
    0x84, 0xB5, 0xE6, 0xD7, 0x40, 0x71, 0x22, 0x13,
    0x7E, 0x4F, 0x1C, 0x2D, 0xBA, 0x8B, 0xD8, 0xE9,
    0xC7, 0xF6, 0xA5, 0x94, 0x03, 0x32, 0x61, 0x50,
    0xBB, 0x8A, 0xD9, 0xE8, 0x7F, 0x4E, 0x1D, 0x2C,
    0x02, 0x33, 0x60, 0x51, 0xC6, 0xF7, 0xA4, 0x95,
    0xF8, 0xC9, 0x9A, 0xAB, 0x3C, 0x0D, 0x5E, 0x6F,
    0x41, 0x70, 0x23, 0x12, 0x85, 0xB4, 0xE7, 0xD6,
    0x7A, 0x4B, 0x18, 0x29, 0xBE, 0x8F, 0xDC, 0xED,
    0xC3, 0xF2, 0xA1, 0x90, 0x07, 0x36, 0x65, 0x54,
    0x39, 0x08, 0x5B, 0x6A, 0xFD, 0xCC, 0x9F, 0xAE,
    0x80, 0xB1, 0xE2, 0xD3, 0x44, 0x75, 0x26, 0x17,
    0xFC, 0xCD, 0x9E, 0xAF, 0x38, 0x09, 0x5A, 0x6B,
    0x45, 0x74, 0x27, 0x16, 0x81, 0xB0, 0xE3, 0xD2,
    0xBF, 0x8E, 0xDD, 0xEC, 0x7B, 0x4A, 0x19, 0x28,
    0x06, 0x37, 0x64, 0x55, 0xC2, 0xF3, 0xA0, 0x91,
    0x47, 0x76, 0x25, 0x14, 0x83, 0xB2, 0xE1, 0xD0,
    0xFE, 0xCF, 0x9C, 0xAD, 0x3A, 0x0B, 0x58, 0x69,
    0x04, 0x35, 0x66, 0x57, 0xC0, 0xF1, 0xA2, 0x93,
    0xBD, 0x8C, 0xDF, 0xEE, 0x79, 0x48, 0x1B, 0x2A,
    0xC1, 0xF0, 0xA3, 0x92, 0x05, 0x34, 0x67, 0x56,
    0x78, 0x49, 0x1A, 0x2B, 0xBC, 0x8D, 0xDE, 0xEF,
    0x82, 0xB3, 0xE0, 0xD1, 0x46, 0x77, 0x24, 0x15,
    0x3B, 0x0A, 0x59, 0x68, 0xFF, 0xCE, 0x9D, 0xAC };
        static byte g_crc = 0;
        static byte CRCChar(byte ch)
        {
            g_crc = crc_table[g_crc ^ ch];
            return g_crc;
        }
        /*****************CRC函数********************/
        public static byte CRCBuffer(byte[] buffer)
        {
            g_crc = 0x00;
            int i;
            int len = buffer.Length - 2;
            for (i = 0; i < len; i++)
            {
                CRCChar(buffer[i]);
            }
            return g_crc;
        }

        public static string GetGateStatusHex(this byte[] input, int start, int end = 0)
        {
            // Null or empty check for input array
            if (input == null || input.Length == 0)
                return "";

            // Validate start and end parameters
            if (end != 0 && (start < 0 || end >= input.Length || start > end))
                return "";
            // Use StringBuilder for efficient string concatenation
            var hexBuilder = new StringBuilder();
            if (end - start == 3)
            {
                byte[] buff = new byte[4];
                buff[1] = input[start];
                buff[0] = input[start + 1];
                buff[3] = input[start + 2];
                buff[2] = input[start + 3];
                var data = BitConverter.ToSingle(buff, 0);
                hexBuilder.Append(data + "(");

            }
            if (end - start == 1)
            {
                var data = BitConverter.ToInt16(input, start);
                //byte[] buff = new byte[4];
                //buff[1] = input[start];
                //buff[0] = input[start + 1];
                //buff[2] = input[start];
                //buff[3] = input[start + 1];
                //var data = BitConverter.ToSingle(buff, 0);
                hexBuilder.Append(data + "(");
            }
            else if (end == 0)
            {
                hexBuilder.Append(input[start] + "(0x" + input[start].ToString("X2"));
            }
            for (int i = start; i <= end; i++)
            {
                hexBuilder.Append(input[i].ToString("X2"));
                if (i < end) // Avoid adding space after the last element
                    hexBuilder.Append(" ");
            }

            return hexBuilder.ToString() + ")";
        }

        /// <summary>
        /// 将十六进制字符串转换为字节数组。
        /// </summary>
        /// <param name="hex">要转换的十六进制字符串。</param>
        /// <returns>表示十六进制字符串的字节数组。</returns>
        /// <exception cref="ArgumentNullException">当输入字符串为空时抛出。</exception>
        /// <exception cref="ArgumentException">当输入字符串长度不是偶数或包含非十六进制字符时抛出。</exception>
        public static byte[] HexStringToByteArray(this string hex)
        {
            if (hex == null)
                throw new ArgumentNullException(nameof(hex), "输入字符串不能为空。");

            // 移除字符串中的所有空白字符以确保灵活性。
            var cleanHex = new string(hex.Where(c => !char.IsWhiteSpace(c)).ToArray());

            // 检查清理后的字符串长度是否为偶数（因为每个字节由两个十六进制字符表示）。
            if (cleanHex.Length % 2 != 0)
                throw new ArgumentException("十六进制字符串必须包含偶数个字符。", nameof(hex));

            try
            {
                return Enumerable.Range(0, cleanHex.Length)
                                 .Where(x => x % 2 == 0)
                                 .Select(x => Convert.ToByte(cleanHex.Substring(x, 2), 16))
                                 .ToArray();
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            {
                throw new ArgumentException("十六进制字符串包含非十六进制字符。", nameof(hex), ex);
            }
        }

        public static void FloatStringToBytes(this byte[] bytes, string floatvalue, int startindex)
        {
            if (float.TryParse(floatvalue, out float re3))
            {
                var data3 = FloatToLittleEndianBytes(re3);
                bytes[startindex] = data3[2];
                bytes[startindex + 1] = data3[3];
                bytes[startindex + 2] = data3[0];
                bytes[startindex + 3] = data3[1];
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
            //if (BitConverter.IsLittleEndian)
            //{
            //    // 如果系统已经是小端序，则不需要转换
            //    return bytes;
            //}
            //else
            //{
            // 如果系统是大端序，则需要反转字节数组
            Array.Reverse(bytes);
            return bytes;
            //}
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

    public static class QueneWriteData
    {
        public static class AsyncLogger
        {
            private static readonly BlockingCollection<byte[]> _logQueue = new BlockingCollection<byte[]>();
            private static Task _workerTask;

            public static void Initialize()
            {
                // 启动后台工作者线程
                _workerTask = Task.Run(ProcessLogQueue);
            }

            public static void Add(byte[] logEntry)
            {
                try
                {
                    _logQueue.Add(logEntry); // 添加队列
                }
                catch
                {
                }
            }

            private static async Task ProcessLogQueue()
            {
                foreach (var logEntry in _logQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        //写入数据
                        //  await logEntry.ExecuteInsertLog();
                        await Task.Delay(50);
                    }
                    catch
                    {
                    }
                }
            }

            // 用于通知系统停止接收新包，并完成处理现有条目
            public static void Shutdown()
            {
                _logQueue.CompleteAdding();

                // 等待工作者线程处理完所有数据包
                try
                {
                    _workerTask.Wait();
                }
                catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
                {
                    // 处理取消的情况
                }
            }
        }
    }
}
