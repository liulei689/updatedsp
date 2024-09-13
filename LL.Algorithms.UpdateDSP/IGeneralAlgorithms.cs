namespace LL.Algorithms.UpdateDSP
{
    public interface IGeneralAlgorithms
    {
        (ushort, ushort) GetBinCheckAAndCheckB(byte[] BinFileData, int BinFileLen);
        string GetVersionToString(byte byte1, byte byte2);
        string GetCommAckResult(byte code);

    }
}
