using System.Collections.Generic;

namespace LL.Algorithms.UpdateDSP
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

    }
}
