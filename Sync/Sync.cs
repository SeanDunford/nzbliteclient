using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Classes;
using Database;

namespace Sync
{
    class Sync
    {
        private const string LOGNAME = "[SYNC]";
        private const string POST_OK = "OK";

        public static bool Synchronize(DbFile dbf)
        {
            try
            {
                string apiKey = Settings.Settings.Current.ApiKey;
                string apiUrl = Settings.Settings.Current.ApiSyncUrl;
                if (string.IsNullOrEmpty(apiKey))
                {
                    Logger.Warn(LOGNAME, "Cannot sync because apiKey is missing");
                    return false;
                }
                if (string.IsNullOrEmpty(apiUrl))
                {
                    Logger.Warn(LOGNAME, "Cannot sync because apiUrl is missing");
                    return false;
                }

                SyncItem si = new SyncItem()
                {
                    Key = apiKey,
                    Name = dbf.Name,
                    Size = dbf.Size,
                    Checksum = dbf.Checksum,
                    DownloadLink = dbf.DownloadLink,
                    Tag = dbf.Tag,
                    Category = dbf.Category,
                    Lang = dbf.Lang,
                    EncryptionMode = dbf.EncryptionMode
                };

                string json = Serializer.Serialize(si);

                using (WebClientCustom wc = new WebClientCustom())
                {
                    wc.TimeoutMs = 30000;
                    wc.Headers["user-agent"] = Utilities.USERAGENT;
                    string res = wc.UploadString(apiUrl, json);
                    if (string.IsNullOrEmpty(res) == false && res == POST_OK)
                    {
                        Db.FileSave(dbf, DbFile.State.SYNC);
                        Db.Save();
                        Logger.Info(LOGNAME, "[" + dbf.Id + "] Synced");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        public static bool SynchronizeAll()
        {
            try
            {
                List<DbFile> filesToSync = Db.FilesToSync();
                if (filesToSync == null || filesToSync.Count == 0)
                {
                    Logger.Info(LOGNAME, "No file to sync");
                    return true;
                }

                int total = filesToSync.Count;
                int success = 0;

                foreach (DbFile dbf in filesToSync)
                {
                    if (Synchronize(dbf) == true)
                    {
                        success += 1;
                    }
                }
                Logger.Info(LOGNAME, success + " files synced (total: " + total + " - " + (int)((success / total) * 100) + "%)");
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
