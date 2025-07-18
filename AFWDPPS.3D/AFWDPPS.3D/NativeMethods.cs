using System.Runtime.InteropServices;

public static class NativeMethods
{
    [DllImport("AFWDPP.SF.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int add(int a, int b);
}