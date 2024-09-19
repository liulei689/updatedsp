using System;
using System.Collections.Generic;
using System.IO;

namespace LL2024.Algorithms.UpdateDSP
{
    public class GeneralAlgorithms : IGeneralAlgorithms
    {
        const int APPHEAD_LENGTH = 124;

        public int LoadBinFile(byte[] BinFileData, string FilePath)
        {
            if (File.Exists(FilePath))
            {
                FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
                int len = (int)fs.Length;
                if (len < APPHEAD_LENGTH)
                {
                    throw new Exception($"固件不合法，低于最小限制{APPHEAD_LENGTH}BIT，加载固件失败。");
                }
                for (int i = 0; i <= len; i++)
                {
                    BinFileData[i] = (byte)fs.ReadByte();
                }
                fs.Close();
                return len;
            }
            throw new Exception("读取失败！\n错误原因：不存在此文件");

        }
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
        private byte DLE = 0x55, STX = 0x02, ETX = 0x03;//包头包尾数值
        //设置帧头帧尾
        public void SetDLE_STX_ETX(byte dle = 0x55, byte stx = 0x02, byte etx = 0x03)
        {
            DLE = dle;
            STX = stx;
            ETX = etx;
        }
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

        public byte[] SetSendData(byte[] databuf, int datalength)
        {
            //包头
            List<byte> sendlist = new List<byte>
            {
                DLE,
                STX
            };
            //转义字节
            for (int i = 0; i < datalength; i++)
            {
                if (databuf[i] == DLE)
                {
                    sendlist.Add(databuf[i]);
                }
                sendlist.Add(databuf[i]);
            }
            // 校验字节
            byte[] Check = DSP28335.CheckSum(databuf, datalength);
            if (Check[0] == DLE)
            { sendlist.Add(Check[0]); }
            sendlist.Add(Check[0]);
            if (Check[1] == DLE)
            { sendlist.Add(Check[1]); }
            sendlist.Add(Check[1]);
            //包尾
            sendlist.Add(DLE);
            sendlist.Add(ETX);
            byte[] SendPack = new byte[sendlist.Count];
            sendlist.CopyTo(SendPack);
            return SendPack;
        }

        // 计算给定数据（data）的CRC，长度为len  
        public ushort GetCRC16(byte[] data, int start, int len)
        {
            ushort crc = 0; // 初始CRC值 
            for (int i = start; i < len; i++)
            {
                // CRC更新算法  
                crc = (ushort)((crc >> 8) ^ CC_CRCTAB[(crc ^ data[i]) & 0xFF]);
            }
            return crc;
        }

        public byte[] GetCRC16Bits(byte[] data, int start, int len)
        {
            ushort value = GetCRC16(data, start, len);
            byte[] byteArray = new byte[2];
            // 将ushort的高字节和低字节分别存储到byte数组的相应位置  
            byteArray[0] = (byte)(value >> 8); // 右移8位得到高字节  
            byteArray[1] = (byte)(value & 0xFF); // 与0xFF进行位与操作得到低字节  
            return byteArray;
        }

        private readonly ushort[] CC_CRCTAB =
        {
            0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50a5, 0x60c6, 0x70e7,
            0x8108, 0x9129, 0xa14a, 0xb16b, 0xc18c, 0xd1ad, 0xe1ce, 0xf1ef,
            0x1231, 0x0210, 0x3273, 0x2252, 0x52b5, 0x4294, 0x72f7, 0x62d6,
            0x9339, 0x8318, 0xb37b, 0xa35a, 0xd3bd, 0xc39c, 0xf3ff, 0xe3de,
            0x2462, 0x3443, 0x0420, 0x1401, 0x64e6, 0x74c7, 0x44a4, 0x5485,
            0xa56a, 0xb54b, 0x8528, 0x9509, 0xe5ee, 0xf5cf, 0xc5ac, 0xd58d,
            0x3653, 0x2672, 0x1611, 0x0630, 0x76d7, 0x66f6, 0x5695, 0x46b4,
            0xb75b, 0xa77a, 0x9719, 0x8738, 0xf7df, 0xe7fe, 0xd79d, 0xc7bc,
            0x48c4, 0x58e5, 0x6886, 0x78a7, 0x0840, 0x1861, 0x2802, 0x3823,
            0xc9cc, 0xd9ed, 0xe98e, 0xf9af, 0x8948, 0x9969, 0xa90a, 0xb92b,
            0x5af5, 0x4ad4, 0x7ab7, 0x6a96, 0x1a71, 0x0a50, 0x3a33, 0x2a12,
            0xdbfd, 0xcbdc, 0xfbbf, 0xeb9e, 0x9b79, 0x8b58, 0xbb3b, 0xab1a,
            0x6ca6, 0x7c87, 0x4ce4, 0x5cc5, 0x2c22, 0x3c03, 0x0c60, 0x1c41,
            0xedae, 0xfd8f, 0xcdec, 0xddcd, 0xad2a, 0xbd0b, 0x8d68, 0x9d49,
            0x7e97, 0x6eb6, 0x5ed5, 0x4ef4, 0x3e13, 0x2e32, 0x1e51, 0x0e70,
            0xff9f, 0xefbe, 0xdfdd, 0xcffc, 0xbf1b, 0xaf3a, 0x9f59, 0x8f78,
            0x9188, 0x81a9, 0xb1ca, 0xa1eb, 0xd10c, 0xc12d, 0xf14e, 0xe16f,
            0x1080, 0x00a1, 0x30c2, 0x20e3, 0x5004, 0x4025, 0x7046, 0x6067,
            0x83b9, 0x9398, 0xa3fb, 0xb3da, 0xc33d, 0xd31c, 0xe37f, 0xf35e,
            0x02b1, 0x1290, 0x22f3, 0x32d2, 0x4235, 0x5214, 0x6277, 0x7256,
            0xb5ea, 0xa5cb, 0x95a8, 0x8589, 0xf56e, 0xe54f, 0xd52c, 0xc50d,
            0x34e2, 0x24c3, 0x14a0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405,
            0xa7db, 0xb7fa, 0x8799, 0x97b8, 0xe75f, 0xf77e, 0xc71d, 0xd73c,
            0x26d3, 0x36f2, 0x0691, 0x16b0, 0x6657, 0x7676, 0x4615, 0x5634,
            0xd94c, 0xc96d, 0xf90e, 0xe92f, 0x99c8, 0x89e9, 0xb98a, 0xa9ab,
            0x5844, 0x4865, 0x7806, 0x6827, 0x18c0, 0x08e1, 0x3882, 0x28a3,
            0xcb7d, 0xdb5c, 0xeb3f, 0xfb1e, 0x8bf9, 0x9bd8, 0xabbb, 0xbb9a,
            0x4a75, 0x5a54, 0x6a37, 0x7a16, 0x0af1, 0x1ad0, 0x2ab3, 0x3a92,
            0xfd2e, 0xed0f, 0xdd6c, 0xcd4d, 0xbdaa, 0xad8b, 0x9de8, 0x8dc9,
            0x7c26, 0x6c07, 0x5c64, 0x4c45, 0x3ca2, 0x2c83, 0x1ce0, 0x0cc1,
            0xef1f, 0xff3e, 0xcf5d, 0xdf7c, 0xaf9b, 0xbfba, 0x8fd9, 0x9ff8,
            0x6e17, 0x7e36, 0x4e55, 0x5e74, 0x2e93, 0x3eb2, 0x0ed1, 0x1ef0
        };



    }
}
