using System.Collections.Generic;

namespace LL2024.Algorithms.UpdateDSP
{
    public interface IGeneralAlgorithms
    {
        int LoadBinFile(byte[] BinFileData, string FilePath);
        (ushort, ushort) GetBinCheckAAndCheckB(byte[] BinFileData, int BinFileLen);
        byte[] CheckSum(byte[] databuf, int datalength, byte CHECKA = 0, byte CHECKB = 0);
        string GetVersionToString(byte byte1, byte byte2);
        string GetCommAckResult(byte code);
        List<byte> SerialDataReceiver(byte data);
        byte[] ValidatePacket(List<byte> data, byte ChannelID);
        void ClearRecListCache();
        byte[] SetSendData(byte[] databuf, int datalength);
        void SetDLE_STX_ETX(byte dle = 0x55, byte stx = 0x02, byte etx = 0x03);
        ushort GetCRC16(byte[] data, int start, int len);
        byte[] GetCRC16Bits(byte[] data, int start, int len);
        /// <summary>
        /// 检查crc 默认crc是传入长度的后两字节
        /// </summary>
        /// <param name="data"></param>
        /// <param name="start"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        bool CheckCRC16Bits(byte[] data, int start, int len);
        /// <summary>
        /// 校验和1 字节异或的结果
        /// </summary>
        /// <param name="btAry_Data"></param>
        /// <param name="int_CheckSumIndex"></param>
        /// <returns></returns>
        byte CheckSum_BytesXorResult(byte[] btAry_Data, int start, int end);
        /// <summary>
        /// 校验和2 0减字节之和
        /// </summary>
        /// <param name="btAry_Data"></param>
        /// <param name="int_CheckSumIndex"></param>
        /// <returns></returns>
        byte CheckSum_ZeroMinusBytesSum(byte[] btAry_Data, int start, int end);

        /// <summary>
        /// 正常校验逻辑 帧最后两位分别是校验和1 校验和2
        /// </summary>
        /// <param name="btAry_Data"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        bool CheckSumNomarl(byte[] btAry_Data);
        /// <summary>
        /// 正常校验生成逻辑 帧最后两位分别是校验和1 校验和2
        /// </summary>
        /// <param name="btAry_Data"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        void GetSumNomarl(byte[] btAry_Data);

    }
}
