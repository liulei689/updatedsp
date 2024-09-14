using LL2024.Algorithms.UpdateDSP;
using System.Collections.Generic;

namespace LL.Algorithms.UpdateDSP
{
    public class GeneralAlgorithms : IGeneralAlgorithms
    {
        public (ushort, ushort) GetBinCheckAAndCheckB(byte[] BinFileData, int BinFileLen)
        {
            ushort Bin_CheckA = 0, Bin_CheckB = 0;
            BinFileData[0] = 0;
            BinFileData[1] = 0;
            BinFileData[2] = 0;
            BinFileData[3] = 0;
            for (int i = 0; i < BinFileLen; i++)
            {
                Bin_CheckA += BinFileData[i];
                Bin_CheckB += Bin_CheckA;
            }
            return (Bin_CheckA, Bin_CheckB);
        }

        public byte[] CheckSum(byte[] databuf, int datalength, byte CHECKA = 0, byte CHECKB = 0)
        {
            byte CHECK_A = 0, CHECK_B = 0;
            for (int i = 0; i < datalength; i++)
            {
                CHECK_A += databuf[i];
                CHECK_B += CHECK_A;
            }
            return new byte[3] { CHECK_A, CHECK_B, (CHECK_A == CHECKA && CHECK_B == CHECKB) ? (byte)1 : (byte)0 };
        }

        public string GetVersionToString(byte byte1, byte byte2)
        {
            return byte1.ToString("X2").Insert(1, ".") + "." + byte2.ToString("X2").Insert(1, ".");
        }

        public string GetCommAckResult(byte code)
        {
            string str;
            switch (code)
            {
                case 0x00:
                    str = "成功应答";
                    break;
                case 0x01:
                    str = "扇区擦除错误";
                    break;
                case 0x02:
                    str = "扇区写入错误";
                    break;
                case 0x03:
                    str = "固件数据校验码错误，请尝试重新加载固件";
                    break;
                case 0x04:
                    str = "数据包校验失败，请尝试重新加载固件";
                    break;
                case 0x05:
                    str = "固件数据写入成功";
                    break;
                case 0x06:
                    str = "超出FLASH容量范围";
                    break;
                case 0x07:
                    str = "Boot串码不符错误,请尝试重新加载固件";
                    break;
                case 0x08:
                    str = "扇区擦除成功";
                    break;
                case 0xFF:
                    str = "非法数据包，,请尝试重新加载固件";
                    break;
                default:
                    str = "应答无法解析";
                    break;
            }
            return str;
        }

        private volatile int protocol_sign = 0;
        private const int protocol_sign_startDLE = 0;
        private const int protocol_sign_STX = 1;
        private const int protocol_sign_endDLE = 2;
        private const int protocol_sign_ETX = 3;
        const byte DLE = 0x55, STX = 0x02, ETX = 0x03;//包头包尾数值

        public void ClearRecListCache()
        {
            if (reclist != null)
                reclist.Clear();
        }

        List<byte> reclist = new List<byte>();
        List<byte> reclist_rec = new List<byte>();
        public List<byte> SerialDataReceiver(byte data)
        {
            reclist_rec.Clear();
            switch (protocol_sign)
            {
                // 找到数据包开始标志DLE
                case protocol_sign_startDLE:
                    if (data == DLE)
                    {
                        reclist.Clear();
                        protocol_sign = protocol_sign_STX;
                        reclist.Add(data);
                    }
                    break;
                // 找到数据包开始标志STX
                case protocol_sign_STX:
                    if (data == STX)
                    {
                        protocol_sign = protocol_sign_endDLE;
                        reclist.Add(data);
                    }
                    else if (data == DLE)
                    {
                        reclist.Clear();
                        reclist.Add(data);
                    }
                    else
                    {
                        protocol_sign = protocol_sign_startDLE;
                    }
                    break;
                // 找到数据包结束标志DLE
                case protocol_sign_endDLE:
                    reclist.Add(data);
                    if (data == DLE)
                    {
                        protocol_sign = protocol_sign_ETX;
                    }
                    break;
                // 找到数据包结束标志ETX
                case protocol_sign_ETX:
                    if (data == ETX)
                    {
                        reclist.Add(data);
                        // DLE+STX+<data stream>+CHECKA+CHECKB+DLE+ETX
                        if (reclist.Count >= 7 && reclist.Count <= 2048)
                        {
                            // 将数据内部DLE DLE转换为DLE  其实没啥用
                            for (int j = 2; j < reclist.Count - 2; j++)
                            {
                                if (reclist[j] == DLE && (j + 1) < (reclist.Count - 2) && reclist[j + 1] == DLE)
                                {
                                    reclist.RemoveAt(j);
                                }
                            }
                            reclist_rec.AddRange(reclist);
                            // 恢复默认值
                            protocol_sign = protocol_sign_startDLE;
                        }
                    }
                    else if (data == DLE)
                    {
                        // DLE+DLE为数据中出现DLE的转义 特么的下位机如果有0x55会给两个0x55，防止数据中出现0x55 0x03认为完整帧了，在这里其实已经去掉其中一个0x55
                        //数据帧连续两个0x55 只取一个  只有0x55 0x03这种情况才认为出来，其他无论多少一个0x55 后面跟一个0x03 只取一个0x55 ，0x03正常取出不来。严谨逻辑
                        protocol_sign = protocol_sign_endDLE;
                    }

                    else
                    {
                        // DLE后跟的既不是ETX也不是DLE，数据包出错
                        protocol_sign = protocol_sign_startDLE;
                    }
                    break;
                default:
                    protocol_sign = protocol_sign_startDLE;
                    break;
            }
            // 数据过长，丢弃数据
            if (reclist.Count >= 2048)
            {
                reclist.Clear();
                protocol_sign = protocol_sign_startDLE;
            }
            return reclist_rec;
        }

        public byte[] ValidatePacket(List<byte> data, byte ChannelID)
        {
            // 通道地址不对，不处理
            int PackLength = data.Count;
            byte[] DataArray = new byte[PackLength - 6];
            reclist.CopyTo(2, DataArray, 0, PackLength - 6);
            byte isRight = DSP28335.CheckSum(DataArray, PackLength - 6, reclist[PackLength - 4], reclist[PackLength - 3])[2];
            if (isRight == 1 && DataArray[0] == ChannelID)
                return DataArray;
            else return null;
        }
    }
}
