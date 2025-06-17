using System.Collections.Generic;

namespace LL2024.Algorithms.UpdateDSP
{
    public interface ICommon
    {
        List<byte> GetRecBufData_422(int HEAD1, int len, byte[] bt_RecBuf);
    }
}
