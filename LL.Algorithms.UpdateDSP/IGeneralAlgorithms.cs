namespace LL.Algorithms.UpdateDSP
{
    public interface IGeneralAlgorithms
    {
        (ushort, ushort) GetBinCheckAAndCheckB(byte[] BinFileData, int BinFileLen);
        byte[] CheckSum(byte[] databuf, int datalength, byte CHECKA = 0, byte CHECKB = 0);
        string GetVersionToString(byte byte1, byte byte2);
        string GetCommAckResult(byte code);

    }
}
