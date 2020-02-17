using System;
using System.Diagnostics;
using System.IO;
using Classes;
using Database;
using FileWorkers;
using Settings;
using Usenet;

namespace NzbLiteClient
{
    class Program
    {
        private const string LOGNAME = "[PROGRAM]";
        private const string SEP = "*********************************";

        static void Main(string[] args)
        {
            if (Utilities.IsWindowsPlatform())
            {
                Console.Title = "NzbLiteClient v" + Utilities.CurrentVersion().ToString();
            }
            if (args == null || args.Length == 0)
            {
                Console.WriteLine(" _   _     _     _      _ _        _____ _ _            _   ");
                Console.WriteLine("| \\ | |   | |   | |    (_) |      / ____| (_)          | |  ");
                Console.WriteLine("|  \\| |___| |__ | |     _| |_ ___| |    | |_  ___ _ __ | |_ ");
                Console.WriteLine("| . ` |_  / '_ \\| |    | | __/ _ \\ |    | | |/ _ \\ '_ \\| __|");
                Console.WriteLine("| |\\  |/ /| |_) | |____| | ||  __/ |____| | |  __/ | | | |_ ");
                Console.WriteLine("|_| \\_/___|_.__/|______|_|\\__\\___|\\_____|_|_|\\___|_| |_|\\__| (v" + Utilities.CurrentVersion().ToString() + ")");
            }
            Utilities.EnsureDirectories();
            Utilities.InitiateSSLTrust();
            Logger.SetLog4NetConfiguration();
            Console.WriteLine(SEP);
            bool isInitOk = Init();
            Console.WriteLine(SEP);
            if (isInitOk == false)
            {
                return;
            }
            if (args == null || args.Length == 0)
            {
                if (VersionChecker.Check() == true)
                {
                    if (Utilities.IsWindowsPlatform())
                    {
                        Console.Title += " - NEW VERSION AVAILABLE !";
                    }
                }
                Menu();
                Console.ReadLine();
                Process.GetCurrentProcess().Kill();
            }
            else
            {
                string cmd = args[0];
                string param = string.Empty;
                string outputDir = Utilities.FolderDownload;
                if (args.Length > 1)
                {
                    param = args[1];
                }
                if (args.Length > 2)
                {
                    if (Directory.Exists(args[2]))
                    {
                        outputDir = args[2];
                    }
                    else
                    {
                        Logger.Error(LOGNAME, "Invalid output directory", null);
                        return;
                    }
                }
                switch (cmd.ToLower())
                {
                    case "-b":
                        ModeBackup();
                        break;

                    case "-r":
                        if (Settings.Settings.Current.Folders == null)
                        {
                            Logger.Error(LOGNAME, "You have to define at least 1 SettingFolder", null);
                            return;
                        }
                        SettingFolder sf = null;
                        for (int i = 0; i < Settings.Settings.Current.Folders.Count; i++)
                        {
                            if (Settings.Settings.Current.Folders[i].Path == param)
                            {

                                sf = Settings.Settings.Current.Folders[i];
                                break;
                            }
                        }
                        ModeRestore(sf);
                        break;

                    case "-u":
                        ModeUpload(param, Crypto.EncryptionMode.NONE, outputDir);
                        break;

                    case "-d":
                        ModeDownload(param, outputDir);
                        break;

                    case "-s":
                        ModeSync();
                        break;

                    case "-c":
                        ModeConvert(param, outputDir);
                        break;

                    default:
                        Logger.Error(LOGNAME, "Invalid commandline", null);
                        break;
                }
            }
        }

        private static bool Init()
        {
            try
            {
                if (Settings.Settings.Load() == false)
                {
                    Logger.Error(LOGNAME, "An error occurred while loading settings", null);
                    return false;
                }
                if (FilePariter.Load(Settings.Settings.Current.ParPath, Settings.Settings.Current.ParThreads, Settings.Settings.Current.ParRedundancy) == false)
                {
                    Logger.Error(LOGNAME, "An error occurred while loading par2", null);
                    return false;
                }
                if (UsenetConns.Start() == false)
                {
                    Logger.Error(LOGNAME, "An error occurred while starting UsenetConns", null);
                    return false;
                }
                if (Pokemon.Load() == false)
                {
                    Logger.Error(LOGNAME, "An error occurred while starting Pokemon", null);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        #region Modes
        private static void ModeBackup()
        {
            try
            {
                if (Db.Load(null, true) == false)
                {
                    Logger.Error(LOGNAME, "An error occurred while loading database", null);
                    return;
                }
                if (FileScanner.Start() == false)
                {
                    Logger.Error(LOGNAME, "An error occurred while starting filescanner", null);
                    return;
                }
                if (FileUploader.Start() == false)
                {
                    Logger.Error(LOGNAME, "An error occurred while starting fileuploader", null);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        private static void ModeRestore(SettingFolder sf)
        {
            try
            {
                if (sf == null)
                {
                    Logger.Error(LOGNAME, "An error occurred while retrieving SettingFolder", null);
                    return;
                }
                if (Db.Load(sf, false) == false)
                {
                    Logger.Error(LOGNAME, "An error occurred while loading database", null);
                    return;
                }
                if (FileDownloader.Start() == false)
                {
                    Logger.Error(LOGNAME, "An error occurred while starting filedownloader", null);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        private static void ModeUpload(string filepath, Crypto.EncryptionMode encryptionMode, string savepath)
        {
            try
            {
                DbFile dbf = FileUploader.UploadSingleFile(new FileInfo(filepath), encryptionMode);
                string nzblPath = filepath + Utilities.EXT_NZBL;
                if (string.IsNullOrEmpty(savepath) == false)
                {
                    if (Directory.Exists(savepath))
                    {
                        nzblPath = Path.Combine(savepath, new FileInfo(filepath).Name + Utilities.EXT_NZBL);
                    }
                    else
                    {
                        nzblPath = savepath;
                    }
                }
                if (dbf != null)
                {
                    File.WriteAllBytes(nzblPath, Utilities.HexToBytes(dbf.DownloadLink));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        private static void ModeDownload(string dlLink, string outputDir)
        {
            try
            {
                if (string.IsNullOrEmpty(outputDir))
                {
                    outputDir = Utilities.FolderDownload;
                }
                DownloadLink dl = DownloadLink.Parse(dlLink);
                if (dl == null)
                {
                    Console.WriteLine("Invalid Link");
                    return;
                }
                FileDownloader.DownloadSingleFile(dl, outputDir);
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        private static void ModeSync()
        {
            try
            {
                if (Db.Load(null, false) == false)
                {
                    Logger.Error(LOGNAME, "An error occurred while loading database", null);
                    return;
                }
                if (Sync.Sync.SynchronizeAll() == false)
                {
                    Logger.Error(LOGNAME, "An error occurred while syncing files", null);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        private static void ModeConvert(string dlLink, string outputDir)
        {
            try
            {
                DownloadLink dl = DownloadLink.Parse(dlLink);
                if (dl == null)
                {
                    Console.WriteLine("Invalid Link");
                    return;
                }
                if (dl.ToNzb(outputDir) == true)
                {
                    Console.WriteLine("Nzb successfully generated");
                }
                else
                {
                    Console.WriteLine("An error occured processing Nzb");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }
        #endregion

        private static void Menu()
        {
            bool loop = true;
            string dlLink = null;
            string outputDir = null;
            while (loop)
            {
                Console.WriteLine(SEP);
                Console.WriteLine("Available options: ");
                Console.WriteLine("[1] Backup");
                Console.WriteLine("[2] Restore");
                Console.WriteLine("[3] Upload single file");
                Console.WriteLine("[4] Download single file");
                Console.WriteLine("[5] Sync files to NzbLite.com");
                Console.WriteLine("[6] Convert Link to Nzb");
                Console.WriteLine("[7] Clean local datatabase");
                Console.WriteLine("Choose an option: ");
                string input = Console.ReadLine();
                Console.WriteLine(SEP);
                switch (input)
                {
                    case "1":
                        ModeBackup();
                        loop = false;
                        break;

                    case "2":
                        if (Settings.Settings.Current.Folders == null)
                        {
                            Logger.Error(LOGNAME, "You have to define at least 1 SettingFolder", null);
                            return;
                        }
                        Console.WriteLine("List of available folders to restore: ");
                        for (int i = 0; i < Settings.Settings.Current.Folders.Count; i++)
                        {
                            Console.WriteLine((i + 1) + " " + Settings.Settings.Current.Folders[i].Path);
                        }
                        Console.WriteLine("Select a folder to restore: ");
                        string v = Console.ReadLine();
                        int n = -1;
                        if (Int32.TryParse(v, out n) == false || n > Settings.Settings.Current.Folders.Count)
                        {
                            Logger.Error(LOGNAME, "Invalid input", null);
                            return;
                        }
                        SettingFolder sf = Settings.Settings.Current.Folders[n - 1];
                        ModeRestore(sf);
                        loop = false;
                        break;

                    case "3":
                        Console.WriteLine("Enter filepath to upload:");
                        string filepath = Console.ReadLine();
                        Console.WriteLine("Encrypt file with XOR (y/n):");
                        string encrypted = Console.ReadLine();
                        if (string.IsNullOrEmpty(filepath) || File.Exists(filepath) == false)
                        {
                            Console.WriteLine("File not found !");
                            continue;
                        }
                        Crypto.EncryptionMode encryptionMode = Crypto.EncryptionMode.NONE;
                        if (encrypted != null && encrypted.ToLower() == "y")
                        {
                            encryptionMode = Crypto.EncryptionMode.XOR;
                        }
                        Console.WriteLine("Enter .nzbl save path:");
                        string savepath = Console.ReadLine();
                        ModeUpload(filepath, encryptionMode, savepath);
                        break;

                    case "4":
                        Console.WriteLine("Enter Link:");
                        dlLink = Console.ReadLine();
                        Console.WriteLine("Enter outputDir (" + Utilities.FolderDownload + "):");
                        outputDir = Console.ReadLine();
                        if (string.IsNullOrEmpty(outputDir))
                        {
                            outputDir = Utilities.FolderDownload;
                        }
                        ModeDownload(dlLink, outputDir);
                        break;

                    case "5":
                        ModeSync();
                        break;

                    case "6":
                        Console.WriteLine("Enter Link:");
                        dlLink = Console.ReadLine();
                        Console.WriteLine("Enter outputDir (" + Utilities.FolderDownload + "):");
                        outputDir = Console.ReadLine();
                        if (string.IsNullOrEmpty(outputDir))
                        {
                            outputDir = Utilities.FolderDownload;
                        }
                        ModeConvert(dlLink, outputDir);
                        break;

                    case "7":
                        if (Db.Load(null, false) == false)
                        {
                            Logger.Error(LOGNAME, "An error occurred while loading database", null);
                            return;
                        }
                        if (Db.Clean() == false)
                        {
                            Logger.Error(LOGNAME, "An error occurred while cleaning database", null);
                            return;
                        }
                        Console.WriteLine("Database successfully cleaned");
                        break;
                }
            }

        }
    }
}
