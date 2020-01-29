using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Classes;
using Database;
using Usenet;

namespace FileWorkers
{
    class FileDownloader
    {
        #region Properties
        private const string LOGNAME = "[FILEDOWNLOADER]";
        private const int WAIT = 15 * 1000;
        private const int MINIMAL_VERSION_SUPPORTED = 4;
        private static bool _isStarted = false;
        private static Task _taskDownload = new Task(Download);
        #endregion

        #region Start/Stop
        public static bool Start()
        {
            try
            {
                _isStarted = true;
                _taskDownload.Start();
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

        #region Download
        private static void Download()
        {
            try
            {
                while (_isStarted)
                {
                    try
                    {
                        DbFile dbf = Db.FileToDownload();
                        if (dbf == null)
                        {
                            Task.Delay(WAIT).Wait();
                            continue;
                        }
                        DownloadLink dl = DownloadLink.Parse(dbf.DownloadLink);
                        if (dl == null)
                        {
                            Logger.Warn(LOGNAME, "Cannot parse DownloadLink: " + dbf.Id);
                            continue;
                        }

                        //ne pas faire de savoir pr ne pas ecraser le fichier Database
                        if (DownloadProcess(dl, Utilities.FolderDownload) == true)
                        {
                            //todo a revoir
                        }
                        else
                        {
                            //todo a revoir
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

        public static bool DownloadProcess(DownloadLink dl, string outputdir)
        {
            try
            {
                if (dl.Version < MINIMAL_VERSION_SUPPORTED)
                {
                    Logger.Warn(LOGNAME, "This NzbLiteClient version doesn't support NzbLite format < " + MINIMAL_VERSION_SUPPORTED);
                    return false;
                }
                DirectoryInfo workingDir = new DirectoryInfo(Utilities.FolderTemp);
                //On clean le workingdir
                FileInfo[] filesToRemove = workingDir.GetFiles();
                foreach (FileInfo fileToRemove in filesToRemove)
                {
                    Utilities.FileDelete(fileToRemove.FullName);
                }

                Stopwatch perfTotal = new Stopwatch();
                Stopwatch perfPar = new Stopwatch();
                Stopwatch perfDownload = new Stopwatch();

                Guid usenetId = dl.Id;
                byte[] encKey = FileXorifier.GenerateXorKey(usenetId);
                string usenetIdStr = usenetId.ToString();

                Logger.Info(LOGNAME, "Start " + usenetIdStr);


                //1- Preparing Chunks for downloading Raw
                Logger.Info(LOGNAME, "Chunks " + usenetIdStr);

                long totalSize = 0;
                DownloadLinkFileInfo dlfi = dl.DicoOfPassNumberPerExtension[Utilities.EXT_RAW];
                FileInfo fi = new FileInfo(Path.Combine(Utilities.FolderTemp, usenetIdStr));
                string filepath = fi.FullName;

                BinaryWriter bw = new BinaryWriter(new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write, Utilities.BUFFER_SIZE));
                bw.BaseStream.SetLength(dlfi.Size);
                totalSize += dlfi.Size;
                int skippedChunks = 0;
                for (int i = 0; i < dlfi.ListOfPassNumber.Count; i++)
                {
                    if (dlfi.ListOfPassNumber[i] < UsenetServer.MAX_PASS)
                    {
                        UsenetChunk chunk = new UsenetChunk(bw, new FileInfo(filepath).Name, usenetId, Utilities.EXT_RAW, i, dlfi.ListOfPassNumber[i], dlfi.ListOfPassNumber.Count, dl.EncryptionMode); ;
                        UsenetDownloader.AddChunk(chunk);
                    }
                    else
                    {
                        skippedChunks += 1;
                    }
                }

                //2- Run Download Tasks
                Logger.Info(LOGNAME, "Download starts " + usenetIdStr);
                perfDownload.Start();
                UsenetDownloader.Run(encKey, dl.Version);

                //3- Checking for end
                while (UsenetDownloader.IsFinished() == false)
                {
                    Task.Delay(50).Wait();
                }
                perfDownload.Stop();
                bw.Close();

                //4- Checksum for rawFile
                bool success = false;
                string checksum = Checksum.Calculate(fi);
                if (dl.Checksum == checksum) //File is OK
                {
                    success = true;
                }
                else
                {
                    //Downloading Parity Files
                    List<BinaryWriter> listOfBws = new List<BinaryWriter>();

                    foreach (KeyValuePair<string, DownloadLinkFileInfo> kvp in dl.DicoOfPassNumberPerExtension)
                    {
                        string extension = kvp.Key;
                        if (extension == Utilities.EXT_RAW)
                        {
                            continue;
                        }
                        filepath = fi.FullName + extension;

                        bw = new BinaryWriter(new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write, Utilities.BUFFER_SIZE));
                        bw.BaseStream.SetLength(kvp.Value.Size);
                        listOfBws.Add(bw);
                        totalSize += kvp.Value.Size;
                        int nbChunks = kvp.Value.ListOfPassNumber.Count;
                        for (int i = 0; i < nbChunks; i++)
                        {
                            if (kvp.Value.ListOfPassNumber[i] < UsenetServer.MAX_PASS)
                            {
                                UsenetChunk chunk = new UsenetChunk(bw, new FileInfo(filepath).Name, usenetId, extension, i, kvp.Value.ListOfPassNumber[i], nbChunks, dl.EncryptionMode);
                                UsenetDownloader.AddChunk(chunk);
                            }
                        }
                    }

                    //Run Download Tasks
                    Logger.Info(LOGNAME, "Download Par starts " + usenetIdStr);
                    perfDownload.Start();
                    UsenetDownloader.Run(encKey, dl.Version);

                    //Checking for end
                    while (UsenetDownloader.IsFinished() == false)
                    {
                        Task.Delay(50).Wait();
                    }
                    perfDownload.Stop();

                    //Free writers
                    foreach (BinaryWriter bww in listOfBws)
                    {
                        bww.Close();
                    }

                    //trying to repair file with PAR
                    perfPar.Start();
                    Logger.Info(LOGNAME, "Par2 " + usenetIdStr);
                    if (FilePariter.Repair(workingDir) == true)
                    {
                        checksum = Checksum.Calculate(fi);
                        if (dl.Checksum == checksum)
                        {
                            success = true;
                        }
                    }
                    perfPar.Stop();
                }

                //5- Moving to dest
                if (success == true)
                {
                    string destFilepath = outputdir;
                    string[] tmp = dl.Name.Split("/");
                    for (int i = 0; i < tmp.Length; i++)
                    {
                        destFilepath = Path.Combine(destFilepath, tmp[i]);
                        if (i < tmp.Length - 1)
                        {
                            Utilities.EnsureDirectory(destFilepath);
                        }
                        else
                        {
                            Utilities.FileDelete(destFilepath);
                        }
                    }
                    fi.MoveTo(destFilepath);
                }

                ////6- Process Finished
                //FileInfo[] filesToDelete = workingDir.GetFiles();
                //foreach (FileInfo fileToDelete in filesToDelete)
                //{
                //    Utilities.FileDelete(fileToDelete.FullName);
                //}
                perfTotal.Stop();

                if (success == true)
                {
                    Logger.Info(LOGNAME, "Success " + dl.Id + " (name: " + dl.Name + " - par: " + (long)(perfPar.ElapsedMilliseconds / 1000) + "s - down: " + (long)(perfDownload.ElapsedMilliseconds / 1000) + "s - speed: " + Utilities.ConvertSizeToHumanReadable(totalSize / (perfDownload.ElapsedMilliseconds / 1000)) + "b/s)");
                }
                else
                {
                    Logger.Info(LOGNAME, "Fail downloading " + dl.Id + " (name: " + dl.Name + ")");
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        public static void DownloadSingleFile(DownloadLink dl, string outputDir)
        {
            try
            {
                if (DownloadProcess(dl, outputDir) == false)
                {
                    Logger.Warn(LOGNAME, "File has not been downloaded");
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
