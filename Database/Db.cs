using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Classes;
using Settings;

namespace Database
{
    public class Db
    {
        #region Properties
        private const string LOGNAME = "[DB]";
        private static ConcurrentDictionary<string, DbFile> _dicoOfFiles = new ConcurrentDictionary<string, DbFile>(StringComparer.OrdinalIgnoreCase);
        private static bool _needToSave = false;
        private static Task _taskSave = new Task(TaskSave);
        private static bool _taskSaveStarted = false;
        #endregion

        #region Start
        public static bool Load(SettingFolder settingFolder, bool startTaskSave)
        {
            try
            {
                if (File.Exists(Utilities.FileDb))
                {
                    List<DbFile> listOfFiles = Serializer.Deserialize<List<DbFile>>(Utilities.FileDb);
                    if (listOfFiles != null)
                    {
                        foreach (DbFile dbf in listOfFiles)
                        {
                            if (dbf != null)
                            {
                                if (settingFolder != null)
                                {
                                    if (dbf.Id.StartsWith(settingFolder.Path))
                                    {
                                        if (dbf.Status == DbFile.State.UPLOADED || dbf.Status == DbFile.State.SYNC)
                                        {
                                            dbf.Status = DbFile.State.QUEUED;
                                            _dicoOfFiles[dbf.Id] = dbf;
                                        }
                                    }                                    
                                }
                                else
                                {
                                    if (dbf.Status == DbFile.State.ERROR || dbf.Status == DbFile.State.MISSING)
                                    {
                                        dbf.Status = DbFile.State.QUEUED;
                                    }
                                    _dicoOfFiles[dbf.Id] = dbf;
                                }
                            }
                        }
                    }
                }

                if (startTaskSave && _taskSaveStarted == false)
                {
                    _taskSave.Start();
                }                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        private async static void TaskSave()
        {
            _taskSaveStarted = true;
            while (true)
            {
                try
                {
                    if (_needToSave == false)
                    {
                        await Task.Delay(250);
                        continue;
                    }
                    _needToSave = false;
                    Save();
                }
                catch (Exception ex)
                {
                    Logger.Error(LOGNAME, ex.Message, ex);
                }
            }
            _taskSaveStarted = false;
        }

        public static bool Save()
        {
            try
            {
                string backup = Utilities.FileDb + DateTime.UtcNow.ToString("yyyyMMdd HHmmss") + Utilities.EXT_BACKUP;
                if (File.Exists(Utilities.FileDb))
                {
                    File.Move(Utilities.FileDb, backup);
                }

                List<DbFile> listOfFiles = _dicoOfFiles.Values.ToList();
                if (Serializer.Serialize(listOfFiles, Utilities.FileDb) == true)
                {
                    File.Delete(backup);
                }
                _needToSave = false;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }
        #endregion

        #region Files
        /// <summary>
        /// List Files not yet sync
        /// </summary>
        public static List<DbFile> FilesToSync()
        {
            List<DbFile> listOfDbf = null;
            try
            {
                listOfDbf = (from x in _dicoOfFiles.Values where x.Status == DbFile.State.UPLOADED select x).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return listOfDbf;
        }
        #endregion

        #region File
        /// <summary>
        /// Create DbFile
        /// </summary>
        public static bool FileSave(DbFile dbf, DbFile.State newStatus)
        {
            try
            {
                dbf.Status = newStatus;
                _dicoOfFiles[dbf.Id] = dbf;
                _needToSave = true;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        /// <summary>
        /// Get DbFile from its id
        /// </summary>
        public static DbFile FileGet(string fileId)
        {
            try
            {
                DbFile dbf = null;
                if (_dicoOfFiles.TryGetValue(fileId, out dbf))
                {
                    return dbf;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return null;
        }

        /// <summary>
        /// Delete DbFile
        /// </summary>
        public static bool FileDelete(DbFile dbf)
        {
            try
            {
                DbFile dbfOut;
                if (_dicoOfFiles.TryRemove(dbf.Id, out dbfOut))
                {
                    _needToSave = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        /// <summary>
        /// Get DbFile to upload
        /// </summary>
        public static DbFile FileToUpload()
        {
            try
            {
                DbFile dbf = (from x in _dicoOfFiles.Values.ToList() where x.Status == DbFile.State.QUEUED && string.IsNullOrEmpty(x.DownloadLink) orderby Guid.NewGuid() select x).Take(1).FirstOrDefault();
                return dbf;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return null;
        }

        /// <summary>
        /// Get DbFile to download
        /// </summary>
        public static DbFile FileToDownload()
        {
            try
            {
                DbFile dbf = (from x in _dicoOfFiles.Values.ToList() where x.Status == DbFile.State.QUEUED && string.IsNullOrEmpty(x.DownloadLink) == false orderby x.Id ascending select x).Take(1).FirstOrDefault();
                return dbf;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return null;
        }

        #endregion

        #region Clean
        public static bool Clean()
        {
            try
            {
                ConcurrentDictionary<string, DbFile> cleanDico = new ConcurrentDictionary<string, DbFile>(StringComparer.OrdinalIgnoreCase);
                foreach (DbFile dbf in _dicoOfFiles.Values)
                {
                    if (string.IsNullOrEmpty(dbf.DownloadLink) == false)
                    {
                        cleanDico[dbf.Id] = dbf;
                    }
                }
                _dicoOfFiles = cleanDico;
                Save();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }
        #endregion
    }
}
