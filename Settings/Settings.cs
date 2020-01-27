using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Classes;

namespace Settings
{
    public class Settings
    {
        private const string LOGNAME = "[SETTINGS]";
        public static Settings Current;

        #region Sync
        public string ApiKey;
        public string ApiSyncUrl;
        public bool ApiSyncAuto;
        #endregion

        #region Usenet
        public string UsenetNewsgroup;
        public string UsenetUsername;
        public string UsenetPassword;
        public string UsenetServer;
        public bool UsenetUseSSL;
        public ushort UsenetPort;
        public ushort UsenetSlots;
        #endregion

        #region General
        public bool CleanNames;
        public string[] Filters = { };
        public long FileMinSize = 102400000;
        public bool RemoveMissing = false;
        public int PercentSuccess = 90;
        public List<SettingFolder> Folders;
        #endregion

        #region Parity
        public int ParRedundancy = 10;
        public int ParThreads = 0;
        public string ParPath;
        #endregion

        #region Proxy
        public bool ProxyEnabled;
        public string ProxyType;
        public string ProxyServer;
        public ushort ProxyPort;
        public string ProxyUsername;
        public string ProxyPassword;
        #endregion

        public static bool Load()
        {
            try
            {
                if (File.Exists(Utilities.FileSettings) == false)
                {
                    throw new Exception(Utilities.FileSettings + " not found");
                }

                Settings settings = Serializer.Deserialize<Settings>(Utilities.FileSettings);
                if (settings == null)
                {
                    throw new Exception(Utilities.FileSettings + " empty");
                }
                foreach (SettingFolder sf in settings.Folders)
                {
                    if (sf.GetFileNamingRule() == SettingFolder.FileNamingRuleEnum.ERROR)
                    {
                        return false;
                    }
                }
                Current = settings;
                Logger.Info(LOGNAME, "Settings loaded");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }
    }


}
