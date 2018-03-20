using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace TimeTrackerTfs.BO
{
    public static class VersionBO
    {

        private static string versionFile = ConfigurationManager.AppSettings["checkVersion"];

        private static bool checkVersion()
        {
            try
            {
                using (var sr = File.OpenText(versionFile))
                {
                    string newVer = sr.ReadToEnd();
                    System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                    string accVersion = fvi.FileVersion;
                    return accVersion != newVer;
                }
            }
            catch { return false; };
        }

        public static bool HasNewVersion
        {
            get
            {
                return checkVersion();
            }
        }
    }
}
