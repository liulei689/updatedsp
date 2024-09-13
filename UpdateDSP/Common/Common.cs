using System.Collections.Generic;
using System.IO.Ports;

namespace UpdateDSP.Common
{
    public static class Common
    {
        public static IList<string> SearchPort()
        {
            return [.. SerialPort.GetPortNames()];
        }
    }
}
