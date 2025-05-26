namespace AFWDPPS.DB
{
    internal class Program
    {
        static void Main2(string[] args)
        {

            for (int i = 0; i < 1000; i++)
            {
                稳定平台数据 dd = new 稳定平台数据();
                dd.船横滚角度 = i;
                var res = WDPT.Add(dd).Result;
            }
        }
    }
}
