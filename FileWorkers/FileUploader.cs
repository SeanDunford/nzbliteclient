using System;
using System.Threading.Tasks;
using Classes;
using Database;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Usenet;
using System.Linq;

namespace FileWorkers
{
    public class FileUploader
    {
        #region Properties
        private const string LOGNAME = "[FILEUPLOADER]";
        private const int WAIT = 15 * 1000;
        private static bool _isStarted = false;
        private static Task _taskUpload = new Task(Upload);
        #endregion

        #region Start/Stop
        public static bool Start()
        {
            try
            {
                _isStarted = true;
                _taskUpload.Start();
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

        #region Upload
        private static void Upload()
        {
            try
            {
                while (_isStarted)
                {
                    try
                    {
                        DbFile dbf = Db.FileToUpload();
                        if (dbf == null)
                        {
                            Task.Delay(WAIT).Wait();
                            continue;
                        }
                        if (File.Exists(dbf.Id) == false)
                        {
                            if (Settings.Settings.Current.RemoveMissing == true)
                            {
                                Db.FileDelete(dbf);
                            }
                            else
                            {
                                Db.FileSave(dbf, DbFile.State.MISSING);
                                Logger.Info(LOGNAME, "Missing: " + dbf.Id);
                            }
                            continue;
                        }

                        if (UploadProcess(dbf) == true)
                        {
                            if (Settings.Settings.Current.ApiSyncAuto == true)
                            {
                                Sync.Sync.Synchronize(dbf);
                            }
                            //Save Dbf
                            Db.FileSave(dbf, DbFile.State.UPLOADED);
                        }
                        else
                        {
                            Db.FileSave(dbf, DbFile.State.ERROR);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(LOGNAME, ex.Message, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        public static bool UploadProcess(DbFile dbf)
        {
            try
            {
                DirectoryInfo workingDir = new DirectoryInfo(Utilities.FolderTemp);
                //On clean le workingdir
                FileInfo[] filesToRemove = workingDir.GetFiles();
                foreach (FileInfo fileToRemove in filesToRemove)
                {
                    Utilities.FileDelete(fileToRemove.FullName);
                }

                Stopwatch perfTotal = new Stopwatch();
                Stopwatch perfPar = new Stopwatch();
                Stopwatch perfUpload = new Stopwatch();

                Logger.Info(LOGNAME, "[" + dbf.Id + "] Start");
                FileInfo fi = new FileInfo(dbf.Id);
                perfTotal.Start();

                Guid usenetId = Guid.NewGuid();
                byte[] encKey = FileXorifier.GenerateXorKey(usenetId);
                string usenetIdStr = usenetId.ToString();
                string posterEmail = Pokemon.GetEmail();
                string tempFilePath = Path.Combine(workingDir.FullName, usenetIdStr);

                //1- Copying file to temp
                fi.CopyTo(tempFilePath);

                //2- Checksum
                string checksum = Checksum.Calculate(new FileInfo(tempFilePath));
                dbf.Checksum = checksum;
                Logger.Info(LOGNAME, "[" + dbf.Id + "] Checksum: " + checksum);

                //3- Create Parity File
                perfPar.Start();
                Logger.Info(LOGNAME, "[" + dbf.Id + "] Par2");
                if (FilePariter.Create(workingDir, new FileInfo(tempFilePath), usenetIdStr + FilePariter.EXT_PAR2) == false)
                {
                    Logger.Info(LOGNAME, "Par2 error: " + dbf.Id);
                    return false;
                }
                perfPar.Stop();

                //4- Preparing Chunks
                Logger.Info(LOGNAME, "[" + dbf.Id + "] Chunks");
                List<BinaryReader> listOfBrs = new List<BinaryReader>();
                FileInfo[] listOfFilesToUpload = workingDir.GetFiles();
                long totalSize = (from x in listOfFilesToUpload select x.Length).Sum();
                Dictionary<string, long> dicoOfSizePerExtension = new Dictionary<string, long>();
                Dictionary<string, List<UsenetChunk>> dicoOfChunksPerExtension = new Dictionary<string, List<UsenetChunk>>(); //key: extension - value: ListOfChunks
                foreach (FileInfo fileToUpload in listOfFilesToUpload)
                {
                    string extension = fileToUpload.Name.Replace(usenetIdStr, "");
                    if (string.IsNullOrEmpty(extension))
                    {
                        extension = Utilities.EXT_RAW;
                    }
                    dicoOfSizePerExtension[extension] = fileToUpload.Length;
                    int nbChunks = (int)Math.Ceiling((double)fileToUpload.Length / Utilities.ARTICLE_SIZE);
                    BinaryReader br = new BinaryReader(new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, Utilities.BUFFER_SIZE));
                    listOfBrs.Add(br);
                    dicoOfChunksPerExtension[extension] = new List<UsenetChunk>(nbChunks);
                    for (int i = 0; i < nbChunks; i++)
                    {
                        UsenetChunk chunk = new UsenetChunk(br, fileToUpload.Name, usenetId, i, nbChunks, dbf.Encrypted);
                        dicoOfChunksPerExtension[extension].Add(chunk);
                        UsenetUploader.AddChunk(chunk);
                    }
                }

                //5- Run Upload Tasks
                Logger.Info(LOGNAME, "[" + dbf.Id + "] Upload");
                perfUpload.Start();
                UsenetUploader.Run(posterEmail, encKey);

                //6- Checking for end
                while (UsenetUploader.IsFinished() == false)
                {
                    Task.Delay(50).Wait();
                }
                perfUpload.Stop();

                //7- Process Finished
                foreach (BinaryReader br in listOfBrs)
                {
                    br.Close();
                }
                foreach (FileInfo fileToDelete in listOfFilesToUpload)
                {
                    Utilities.FileDelete(fileToDelete.FullName);
                }

                //8- Upload check
                List<UsenetChunk> mainChunkList = dicoOfChunksPerExtension[Utilities.EXT_RAW];
                int nbSuccess = (from x in mainChunkList where x.PassNumber != UsenetServer.MAX_PASS select x).Count();
                int percSuccess = (nbSuccess * 100) / mainChunkList.Count();
                bool uploadSuccess = true;
                if (percSuccess < Settings.Settings.Current.PercentSuccess)
                {
                    uploadSuccess = false;
                }

                if (uploadSuccess == true)
                {
                    //9- Generate DownloadLink
                    Dictionary<string, DownloadLinkFileInfo> dicoOfPassNumberPerExtension = new Dictionary<string, DownloadLinkFileInfo>(); //key: fileExtension|filesize - value: listOfPassNumber
                    foreach (KeyValuePair<string, List<UsenetChunk>> kvp in dicoOfChunksPerExtension)
                    {
                        string extension = kvp.Key;
                        DownloadLinkFileInfo dlfi = new DownloadLinkFileInfo() { Size = dicoOfSizePerExtension[kvp.Key], ListOfPassNumber = (from x in kvp.Value orderby x.ChunkNumber ascending select x.PassNumber).ToList() };
                        dicoOfPassNumberPerExtension[extension] = dlfi;
                    }

                    DownloadLink dl = new DownloadLink(usenetId, dbf.Name, dbf.Checksum, Settings.Settings.Current.UsenetNewsgroup, posterEmail, Utilities.UnixTimestampFromDate(DateTime.UtcNow), dbf.Encrypted, dicoOfPassNumberPerExtension); ;
                    dbf.DownloadLink = DownloadLink.ToString(dl);
                }

                perfTotal.Stop();
                Logger.Info(LOGNAME, "[" + dbf.Id + "] " + (uploadSuccess == true ? "Success" : "Fail") + " (chunks: " + percSuccess + "% - files: " + listOfFilesToUpload.Length + " - size: " + Utilities.ConvertSizeToHumanReadable(totalSize) + " - par: " + (long)(perfPar.ElapsedMilliseconds / 1000) + "s - up: " + (long)(perfUpload.ElapsedMilliseconds / 1000) + "s - speed: " + Utilities.ConvertSizeToHumanReadable(totalSize / (perfUpload.ElapsedMilliseconds / 1000)) + "b/s)");
                return uploadSuccess;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        public static void UploadSingleFile(FileInfo fi, bool encrypted)
        {
            try
            {
                DbFile dbf = new DbFile();
                dbf.Id = fi.FullName;
                dbf.DateLastWriteTime = fi.LastWriteTimeUtc;
                dbf.Name = fi.Name;
                dbf.Tag = null;
                dbf.Category = null;
                dbf.Encrypted = encrypted;
                dbf.Size = fi.Length;
                dbf.Checksum = Checksum.Calculate(fi);
                if (UploadProcess(dbf) == false)
                {
                    Logger.Warn(LOGNAME, "File has not been uploaded");
                }
                else
                {
                    Logger.Info(LOGNAME, "Link: " + dbf.DownloadLink);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }
        #endregion
    }
}
