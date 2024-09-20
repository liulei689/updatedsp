namespace LL2024.Algorithms.UpdateDSP
{
    public class QFXHAlgorithms : IQFXHAlgorithms
    {

        byte SendCount = 0;
        public void SetHexHead(byte[] BinFileData)
        {
            BinFileData[0] = 0xAA;
            BinFileData[1] = 0X55;
            if (SendCount < 255)
            {
                SendCount++;
            }
            else
            {
                SendCount = 0;
            }
            BinFileData[2] = SendCount;
            BinFileData[3] = 0x80;
        }
    }
}
