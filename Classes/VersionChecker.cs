using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;

namespace Classes
{
    class VersionChecker
    {
        private const string LOGNAME = "[VERSIONCHECKER]";
        private const string VERSION_URL = "https://www.nzblite.com/nzbliteclient.version";

        public static bool Check()
        {
            try
            {
                Logger.Info(LOGNAME, "Checking for NzbLiteClient update");
                Version currentVersion = Utilities.CurrentVersion();
                using (WebClientCustom wc = new WebClientCustom())
                {
                    wc.TimeoutMs = 10000;
                    wc.Headers["user-agent"] = Utilities.USERAGENT;
                    string res = wc.DownloadString(VERSION_URL);
                    if (string.IsNullOrEmpty(res) == false)
                    {
                        Version tmp = Version.Parse(res);
                        if (tmp > currentVersion)
                        {
                            Logger.Info(LOGNAME, "New NzbLiteClient version detected: " + tmp + " (current: " + currentVersion + "). Please visit https://www.nzblite.com to download the latest version.");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                Logger.Warn(LOGNAME, "Error checking new version");
            }
            return false;
        }
    }
}
