using LL.Algorithms.UpdateDSP;
using System.Collections.Generic;

namespace LL2024.Algorithms.UpdateDSP
{
    public static class DSP28335
    {
        public static readonly IGeneralAlgorithms _instance = new GeneralAlgorithms();

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
    }
}
