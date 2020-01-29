using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Classes;
using Database;
using Settings;

namespace FileWorkers
{
    public class FileScanner
    {
        #region Properties
        private const string LOGNAME = "[FILESCANNER]";
        private const int WAIT = 5 * 60 * 1000;
        private static bool _isStarted = false;
        private static Task _taskScan = new Task(Scan);
        #endregion

        #region Start/Stop
        public static bool Start()
        {
            try
            {
                _isStarted = true;
                _taskScan.Start();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        public static bool Stop()
        {
            try
            {
                _isStarted = false;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }
        #endregion

        #region Scan
        private static void Scan()
        {
            try
            {
                while (_isStarted)
                {
                    try
                    {
                        foreach (SettingFolder sf in Settings.Settings.Current.Folders)
                        {
                            DirectoryInfo di = new DirectoryInfo(sf.Path);
                            if (di.Exists)
                            {
                                ScanDirs(di, sf);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(LOGNAME, ex.Message, ex);
                    }
                    Task.Delay(WAIT).Wait();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        private static void ScanDirs(DirectoryInfo di, SettingFolder sf)
        {
            try
            {
                DirectoryInfo[] dirs = di.GetDirectories();
                ScanFiles(di, sf);
                foreach (DirectoryInfo diChild in dirs)
                {
                    ScanDirs(diChild, sf);
                }
            }
            catch (System.UnauthorizedAccessException ex)
            {
                //nothing to do
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);

            }
        }

        private static void ScanFiles(DirectoryInfo di, SettingFolder sf)
        {
            try
            {
                FileInfo[] files = di.GetFiles();
                foreach (FileInfo fi in files)
                {
                    if (fi.Length < Settings.Settings.Current.FileMinSize)
                    {
                        continue;
                    }
                    string fileId = fi.FullName;
                    if (string.IsNullOrEmpty(fileId))
                    {
                        continue;
                    }
                    DbFile dbf = Db.FileGet(fileId);

                    if (dbf == null) //file not in db
                    {
                        string name = GetFileName(fi, sf);
                        if (CheckFilters(name) == false)
                        {
                            continue;
                        }
                        dbf = new DbFile();
                        dbf.Id = fileId;
                        dbf.DateLastWriteTime = fi.LastWriteTimeUtc;
                        dbf.Name = name;
                        dbf.Tag = sf.Tag;
                        dbf.Category = sf.Category;
                        dbf.Lang = sf.Lang;
                        dbf.EncryptionMode = sf.EncryptionMode;
                        dbf.Checksum = null;
                        dbf.Size = fi.Length;
                        Db.FileSave(dbf, DbFile.State.QUEUED);
                        Logger.Info(LOGNAME, "Added: " + dbf.Name);
                    }
                    else if (dbf.DateLastWriteTime < fi.LastWriteTimeUtc)
                    {
                        dbf.DateLastWriteTime = fi.LastWriteTimeUtc;
                        dbf.Checksum = null;
                        dbf.Size = fi.Length;
                        dbf.DownloadLink = null;
                        Db.FileSave(dbf, DbFile.State.QUEUED);
                        Logger.Info(LOGNAME, "Updated: " + dbf.Name);
                    }
                }
            }
            catch (System.UnauthorizedAccessException ex)
            {
                //nothing to do
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        private static bool CheckFilters(string name)
        {
            try
            {
                if (Settings.Settings.Current.Filters == null || Settings.Settings.Current.Filters.Length == 0)
                {
                    return true;
                }
                foreach (string filter in Settings.Settings.Current.Filters)
                {
                    if (string.IsNullOrEmpty(filter))
                    {
                        continue;
                    }
                    if (Regex.Match(name, filter, RegexOptions.IgnoreCase).Success == true)
                    {
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

        private static string GetFileName(FileInfo fi, SettingFolder sf)
        {
            string ret = fi.Name;
            SettingFolder.FileNamingRuleEnum fnr = sf.GetFileNamingRule();
            if (fnr == SettingFolder.FileNamingRuleEnum.PARENTNAME)
            {
                ret = fi.Directory.Name + fi.Extension;
            }
            else if (fnr == SettingFolder.FileNamingRuleEnum.FULLPATH)
            {
                ret = fi.FullName.Split(sf.Path)[1].Replace("\\", "/");
            }

            if (Settings.Settings.Current.CleanNames == true)
            {
                ret = Renamer.Rename(ret);
            }
            return ret;
        }
        #endregion
    }
}
