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

        public string GetVersionToString(byte byte1, byte byte2)
        {
            return byte1.ToString("X2").Insert(1, ".") + "." + byte2.ToString("X2").Insert(1, ".");
        }
    }
}
