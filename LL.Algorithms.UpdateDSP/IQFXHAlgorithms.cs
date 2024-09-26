using System.Collections.Generic;

namespace LL2024.Algorithms.UpdateDSP
{
    public interface IQFXHAlgorithms
    {
        void SetHexHead(byte[] data);
        List<byte> GetRecBufData_422(params byte[] bt_RecBuf);
        string GetQFXHCommAckResult(byte[] data);
        string GetQFXHCommAckResult(byte code);
    }
}
