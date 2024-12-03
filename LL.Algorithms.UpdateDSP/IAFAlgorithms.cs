using System.Collections.Generic;

namespace LL2024.Algorithms.UpdateDSP
{
    public interface IAFAlgorithms
    {
        List<byte> GetRecBufData_422(byte[] bt_RecBuf, byte devideid);
        byte CalculateChecksum(byte[] dataFrame, bool flag = false);
        bool CheckChecksum(byte[] dataFrame);
    }
}
