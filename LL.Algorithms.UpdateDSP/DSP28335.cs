using System;
using System.Collections.Generic;

namespace LL2024.Algorithms.UpdateDSP
{
    public static class DSP28335
    {
        public static readonly IGeneralAlgorithms _instance = new GeneralAlgorithms();
        public static readonly IQFXHAlgorithms _afxhinstance = new QFXHAlgorithms();

        /// <summary>
        /// 加载固件
        /// </summary>
        /// <param name="BinFileData">待载入缓冲区</param>
        /// <param name="FilePath">固件路径</param>
        /// <returns></returns>
        public static int LoadBinFile(byte[] BinFileData, string FilePath)
        {
            return _instance.LoadBinFile(BinFileData, FilePath);
        }
        /// <summary>
        /// 固件完整性校验，固件所有字节生成双校验和，CheckA固件数据校验，CheckB为带CheckA的固件校验和
        /// 上位机下发固件前生成，BOOTLOAD完全接收后自己生成，校验不过，升级失败，报固件数据校验码错误
        /// 错误响应码：0x03
        /// </summary>
        /// <param name="BinFileData">固件字节</param>
        /// <param name="BinFileLen">固件长度</param>
        /// <returns>CheckA,CheckB</returns>
        public static (ushort, ushort) GetBinCheckAAndCheckB(byte[] BinFileData, int BinFileLen)
        {
            return _instance.GetBinCheckAAndCheckB(BinFileData, BinFileLen);
        }

        /// <summary>
        /// 通过HEX获取固件版本号
        /// </summary>
        /// <param name="byte1">字节1</param>
        /// <param name="byte2">字节2</param>
        /// <param name="head">版本头</param>
        /// <returns></returns>
        public static string GetVersionToString(byte byte1, byte byte2, string head = "V")
        {
            return head + _instance.GetVersionToString(byte1, byte2);
        }

        /// <summary>
        /// 初始化固件信息中CheckA和CheckB
        /// 固件信息前部分字节保留，下发前写入校验和与长度，0~1校验双字节CheckA和0~2双字节CheckB 4~7
        /// </summary>
        /// <param name="BinFileData">固件字节</param>
        /// <param name="Bin_CheckA">校验A</param>
        /// <param name="Bin_CheckB">校验B</param>
        public static void SetHexCheckAB(byte[] BinFileData, ushort Bin_CheckA, ushort Bin_CheckB)
        {
            BinFileData[0] = (byte)(Bin_CheckA >> 8);
            BinFileData[1] = (byte)(Bin_CheckA >> 0);
            BinFileData[2] = (byte)(Bin_CheckB >> 8);
            BinFileData[3] = (byte)(Bin_CheckB >> 0);
        }

        /// <summary>
        /// 初始化固件信息中代码长度 写入32字节长度
        /// </summary>
        /// <param name="BinFileData">固件字节</param>
        /// <param name="BinFileLen">固件长度</param>
        public static void SetHexLength(byte[] BinFileData, int BinFileLen)
        {
            BinFileData[4] = (byte)(BinFileLen >> 24);
            BinFileData[5] = (byte)(BinFileLen >> 16);
            BinFileData[6] = (byte)(BinFileLen >> 8);
            BinFileData[7] = (byte)(BinFileLen >> 0);
        }

        /// <summary>
        /// 准备握手数据包
        /// </summary>
        /// <param name="ChannelID"></param>
        /// <param name="BinFileLen"></param>
        /// <param name="Bin_CheckA"></param>
        /// <param name="Bin_CheckB"></param>
        /// <returns></returns>
        public static byte[] SetHandshakePacket(byte ChannelID, int BinFileLen, ushort Bin_CheckA, ushort Bin_CheckB)
        {
            byte[] buf = new byte[20];
            int i = 0;
            buf[i++] = ChannelID;
            buf[i++] = 0x81;
            // 数据总长度
            buf[i++] = (byte)(BinFileLen >> 24);
            buf[i++] = (byte)(BinFileLen >> 16);
            buf[i++] = (byte)(BinFileLen >> 8);
            buf[i++] = (byte)(BinFileLen >> 0);
            // BIN文件校验码
            buf[i++] = (byte)(Bin_CheckA >> 8);
            buf[i++] = (byte)(Bin_CheckA >> 0);
            buf[i++] = (byte)(Bin_CheckB >> 8);
            buf[i++] = (byte)(Bin_CheckB >> 0);
            // Flash操作码 都是一样的吗？
            buf[i++] = 0xA5;
            buf[i++] = 0xF1;
            return SetSendData(buf, i);
        }

        /// <summary>
        /// 发送每包固件
        /// </summary>
        /// <param name="packorder">包序号</param>
        /// <returns></returns>
        public static byte[] SendPackBinData(byte[] BinFileData, byte ChannelID, int packorder, int datalength, int BINDATA_PACK_LEN)
        {

            byte[] buf = new byte[BINDATA_PACK_LEN + 4];
            int tmp, len;

            // 包长度
            if (datalength >= ((packorder + 1) * BINDATA_PACK_LEN))
            {
                len = BINDATA_PACK_LEN;
                tmp = packorder * BINDATA_PACK_LEN;
            }
            else if (datalength >= (packorder * BINDATA_PACK_LEN) && datalength < ((packorder + 1) * BINDATA_PACK_LEN))
            {
                len = datalength - (packorder * BINDATA_PACK_LEN);
                tmp = packorder * BINDATA_PACK_LEN;
            }
            else
            {
                len = 0;
                tmp = 0;
            }
            buf[0] = ChannelID;
            buf[1] = 0x82;
            buf[2] = (byte)(len >> 8);
            buf[3] = (byte)(len >> 0);
            Array.Copy(BinFileData, tmp, buf, 4, len);
            return SetSendData(buf, len + 4);
        }

        /// <summary>
        /// 最后发送数据包
        /// </summary>
        /// <param name="databuf"></param>
        /// <param name="datalength"></param>
        public static byte[] SetSendData(byte[] databuf, int datalength)
        {
            return _instance.SetSendData(databuf, datalength);
        }
        /// <summary>
        /// 通过状态码获取状态文本信息
        /// </summary>
        /// <param name="code">状态码</param>
        /// <returns></returns>
        public static string GetCommAckResult(byte code)
        {
            return _instance.GetCommAckResult(code);
        }

        /// <summary>
        /// 计算或校验校验和
        /// </summary>
        /// <param name="databuf">待生成数据</param>
        /// <param name="datalength">长度</param>
        /// <param name="Sum">生成的校验和</param>
        /// <param name="CHECKA">待校验A</param>
        /// <param name="CHECKB">待校验B</param>
        /// <returns></returns>
        public static byte[] CheckSum(byte[] databuf, int datalength, byte CHECKA = 0, byte CHECKB = 0)
        {
            return _instance.CheckSum(databuf, datalength, CHECKA, CHECKB);
        }

        /// <summary>
        /// 串口完整帧数据识别，单字节传入通过帧头，帧尾，转义帧方式,取到完整帧
        /// </summary>
        /// <param name="data">接受的单字节</param>
        public static List<byte> SerialPacketReceiver(byte data)
        {
            return _instance.SerialDataReceiver(data);
        }

        /// <summary>
        /// 解析校验帧
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] ValidatePacket(List<byte> data, byte ChannelID)
        {
            return _instance.ValidatePacket(data, ChannelID);
        }

        /// <summary>
        /// 清除接受缓存（可选）一般初始化或停止接受时清空一次即可，清除缓存中干扰数据
        /// </summary>
        public static void ClearRecListCache()
        {
            _instance.ClearRecListCache();
        }

        /// <summary>
        /// 识别包类型 业务包识别
        /// </summary>
        /// <param name="RecData"></param>
        /// <returns></returns>
        public static bool IdentifyPacket(byte[] RecData)
        {
            return RecData.Length >= 0x80 && RecData[0] == 0xAA && RecData[1] == 0x55 && RecData[3] == 0x80;
        }

        //帧头帧尾设置
        public static void SetDLE_STX_ETX(byte dle = 0x55, byte stx = 0x02, byte etx = 0x03)
        {
            _instance.SetDLE_STX_ETX(dle, stx, etx);
        }

        /// <summary>
        /// 获取固件包数量
        /// </summary>
        /// <param name="BinFileLen"></param>
        /// <param name="BINDATA_PACK_LEN"></param>
        /// <returns></returns>
        public static int GetBinPackNum(int BinFileLen, int BINDATA_PACK_LEN)
        {
            return BinFileLen / BINDATA_PACK_LEN + 1;
        }

        /// <summary>
        /// 获取CRC
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="start">开始位置</param>
        /// <param name="len">计算长度</param>
        /// <returns></returns>
        public static ushort GetCRC16(byte[] data, int start, int len)
        {
            return _instance.GetCRC16(data, start, len);
        }

        /// <summary>
        /// 获取CRC
        /// </summary>
        /// <param name="start">开始位置</param>
        /// <param name="len">计算长度</param>
        /// <returns></returns>
        public static byte[] GetCRC16Bits(byte[] data, int start, int len)
        {
            return _instance.GetCRC16Bits(data, start, len);
        }

        /// <summary>
        /// 检查crc
        /// </summary>
        /// <param name="data"></param>
        /// <param name="start"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static bool CheckCRC16Bits(byte[] data, int start, int len) => _instance.CheckCRC16Bits(data, start, len);

        /// <summary>
        /// 设位
        /// </summary>
        /// <param name="array"></param>
        /// <param name="byteIndex"></param>
        /// <param name="bitIndex"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="IndexOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void SetBitAt(byte[] array, int byteIndex, int bitIndex, int value)
        {
            // 验证参数  
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (byteIndex < 0 || byteIndex >= array.Length)
                throw new IndexOutOfRangeException("byteIndex is out of range.");
            if (bitIndex < 0 || bitIndex >= 8)
                throw new IndexOutOfRangeException("bitIndex is out of range.");
            if (value != 0 && value != 1)
                throw new ArgumentException("value must be 0 or 1.", nameof(value));

            // 设置位  
            int mask = 1 << bitIndex;
            array[byteIndex] = (byte)(value == 1 ? (array[byteIndex] | mask) : (array[byteIndex] & ~mask));
        }

        /// <summary>
        /// 读位
        /// </summary>
        /// <param name="array"></param>
        /// <param name="byteIndex"></param>
        /// <param name="bitIndex"></param>
        /// <returns></returns>
        public static bool ReadBitAt(byte[] array, int byteIndex, int bitIndex)
        {
            // 检查索引是否在有效范围内  
            if (byteIndex >= 0 && byteIndex < array.Length && bitIndex >= 0 && bitIndex < 8)
            {
                // 创建一个掩码，其中只有bitIndex位被设置为1  
                byte mask = (byte)(1 << bitIndex);
                // 使用AND操作检查指定字节的bitIndex位是否被设置  
                // 如果结果为非零，则表示该位被设置（true），否则为false  
                return (array[byteIndex] & mask) != 0;
            }
            // 如果索引超出范围，则默认返回false（或可以选择抛出异常）  
            return false;
        }

        public static bool CheckSumNomarl(byte[] btAry_Data)
        {
            return _instance.CheckSumNomarl(btAry_Data);
        }

        public static void GetSumNomarl(byte[] btAry_Data)
        {
            _instance.GetSumNomarl(btAry_Data);
        }

        public static byte CheckSum_BytesXorResult(byte[] btAry_Data, int start, int end)
        {
            return _instance.CheckSum_BytesXorResult(btAry_Data, start, end);
        }

        public static byte CheckSum_ZeroMinusBytesSum(byte[] btAry_Data, int start, int end)
        {
            return _instance.CheckSum_ZeroMinusBytesSum(btAry_Data, start, end);
        }

        /// <summary>
        /// 6465握手帧
        /// </summary>
        /// <param name="BinFileData"></param>
        public static void SetQFXHHexHead(byte[] BinFileData)
        {
            _afxhinstance.SetHexHead(BinFileData);
            SetBitAt(BinFileData, 6, 3, 1); //加载模式
            SetBitAt(BinFileData, 7, 0, 0);//还未加载
            //  CSID
            BinFileData[8] = 0x11;
            BinFileData[9] = 0x22;
            BinFileData[10] = 0x33;
            BinFileData[11] = 0x44;
            byte[] CRC = GetCRC16Bits(BinFileData, 0, 12);
            BinFileData[12] = CRC[0];
            BinFileData[13] = CRC[1];
            GetSumNomarl(BinFileData);
        }

        /// <summary>
        /// 422多字节状态机式接受
        /// </summary>
        /// <param name="bt_RecBuf"></param>
        /// <returns></returns>
        public static List<byte> GetRecBufData_422(params byte[] bt_RecBuf)
        {
            return _afxhinstance.GetRecBufData_422(bt_RecBuf);
        }
    }
}
