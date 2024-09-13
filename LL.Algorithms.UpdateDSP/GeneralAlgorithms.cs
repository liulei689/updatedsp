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
    }
}
