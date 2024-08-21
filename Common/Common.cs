using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UpdateDSP.Common
{
   public static class Common
    {
        public static IList<string> SearchPort()
        {
            int i = 0;
            int j = 0;
            int m = 0;
            IList<string> infoListstatic = new List<string>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                i++;
                if (i > 10000)
                {
                    i = 0;
                }
                j = 0;
                foreach (string portName in SerialPort.GetPortNames())
                {
                    j++;
                }
                if (m != j)
                {

                    IList<string> infoList = new List<string>();
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PnPEntity where Name like '%(COM%'"))
                    {
                        var hardInfos = searcher.Get();
                        m = 0;
                        foreach (var hardInfo in hardInfos)
                        {
                            if (hardInfo.Properties["Name"].Value != null)
                            {
                                string deviceName = hardInfo.Properties["Name"].Value.ToString();
                                int startIndex = deviceName.IndexOf("(");
                                int endIndex = deviceName.IndexOf(")");
                                string key = deviceName.Substring(startIndex + 1, deviceName.Length - startIndex - 2);
                                string name = deviceName.Substring(0, startIndex - 1);
                                //Console.WriteLine("key:" + key + ",name:" + name + ",deviceName:" + deviceName);
                                infoList.Add(name + "(" + key + ")");
                                m++;
                            }
                        }
                    }
                    if (!infoListstatic.SequenceEqual(infoList))
                    {
                        //Application.Current.Dispatcher.Invoke(() =>
                        //{
                        //    comlist.ItemsSource = infoList;
                        //    comlist.SelectedIndex = 0;
                        //});
                        infoListstatic = infoList;

                    }
                    if (infoListstatic.Count() == 0)
                        return infoListstatic;
                }
                return infoListstatic;
            }
            return infoListstatic;
        }
    }
}
