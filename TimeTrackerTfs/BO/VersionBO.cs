using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace TimeTrackerTfs.BO
{
    public static class VersionBO
    {
        public static string VersionFile { get; set; }
        private static bool checkVersion()
        {
            if (string.IsNullOrEmpty(VersionFile))
                return false;
            string file = VersionFile + @"\version.txt";
            using (var sr = File.OpenText(file))
            {
                string newVer = sr.ReadToEnd();
                return CurrVersion != newVer;
            }
        }

        public static string CurrVersion
        {
            get
            {
                try
                {
                    System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                    return fvi.FileVersion;
                }
                catch
                {
                    return ("Not Found.");
                }
            }
        }

        public static bool HasNewVersion
        {
            get
            {
                try
                {
                    return checkVersion();
                }
                catch { return false; }
            }
        }
    }
}
