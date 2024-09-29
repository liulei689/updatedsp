using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Reflection;

namespace UpdateDSP.Common
{
    public static class Common
    {
        public static IList<string> SearchPort()
        {
            return [.. SerialPort.GetPortNames()];
        }
        public static string GetPackageVersion(string assemblyName)
        {
            try
            {
                var assembly = Assembly.Load(assemblyName);
                // 假设开发者使用了AssemblyFileVersion或AssemblyInformationalVersion  
                var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
                if (!string.IsNullOrEmpty(fileVersion))
                    return fileVersion;

                var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                return informationalVersion ?? "Unknown Version";
            }
            catch (Exception ex)
            {
                return $"Error getting version for {assemblyName}: {ex.Message}";
            }
        }
    }
}
