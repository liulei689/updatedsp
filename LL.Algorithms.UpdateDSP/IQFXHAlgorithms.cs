using System.Collections.Generic;

namespace LL2024.Algorithms.UpdateDSP
{
    public interface IQFXHAlgorithms
    {
        void SetHexHead(byte[] BinFileData);
        List<byte> GetRecBufData_422(params byte[] bt_RecBuf);
    }
}
