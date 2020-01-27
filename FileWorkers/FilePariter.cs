using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Classes;

namespace FileWorkers
{
    public class FilePariter
    {

        #region Constantes and Properties
        private const string LOGNAME = "[FILEPARITER]";
        public const string EXT_PAR2 = ".par2";
        private const int TIMEOUT_MS = 3 * 3600 * 1000;
        private const int REDUNDANCY_PERCENT_DEFAULT = 15;
        private const int REDUNDANCY_PERCENT_MIN = 5;
        private const int MIN_SIZE = 1024 * 1000 * 5;
        private static string ExePar2 = LinuxExePar2;
        private const string LinuxExePar2 = "/usr/bin/par2";
        private const string LinuxExePar22 = "/usr/local/bin/par2";
        private static int Redundancy = REDUNDANCY_PERCENT_DEFAULT;
        private static int Threads = 0;
        #endregion

        //Fonction qui verifie les prerequis pour l'utilisation de par2
        public static bool Load(string parPath, int parThreads, int parRedundancy)
        {
            try
            {
                if (string.IsNullOrEmpty(parPath) == false)
                {
                    ExePar2 = parPath;
                }
                if (File.Exists(ExePar2) == false && Utilities.IsWindowsPlatform() == true)
                {
                    ExePar2 = Path.Combine(Utilities.ExecutableFolder, "par2.exe");
                }
                else if (File.Exists(ExePar2) == false)
                {
                    if (File.Exists(LinuxExePar2))
                    {
                        ExePar2 = LinuxExePar2;
                    }
                    else if (File.Exists(LinuxExePar22))
                    {
                        ExePar2 = LinuxExePar22;
                    }
                }      
                if (parThreads > 0)
                {
                    Threads = parThreads;
                }
                if (parRedundancy >= REDUNDANCY_PERCENT_MIN)
                {
                    Redundancy = parRedundancy;
                }
                if (File.Exists(ExePar2))
                {
                    Logger.Info(LOGNAME, "Pariter loaded (redundancy: " + Redundancy.ToString() + "% - threads: " + (Threads == 0 ? "all" : Threads.ToString()) + " - path: "+ ExePar2 +")");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        //Fonction qui crée les par2
        public static bool Create(DirectoryInfo workingDir, FileInfo filePath, string outputFilePar)
        {
            try
            {
                if (filePath.Exists == false)
                {
                    return false;
                }
                if (filePath.Length < MIN_SIZE) //pas de fichier de parite pour les petits fichiers
                {
                    return true;
                }
                int exitCode = -1;
                int blocksize = Utilities.ARTICLE_SIZE;
                string argThread = (Threads > 0 ? "-t"+Threads.ToString() : "");
                string args = string.Format(@"create -s{0} -r{1} {4} ""{3}"" ""{2}""", blocksize, Settings.Settings.Current.ParRedundancy, filePath.FullName, outputFilePar, argThread);

                if (ProcessWrapper.Run(workingDir.FullName, ExePar2, args, TIMEOUT_MS, out exitCode) == true)
                {
                    if (exitCode == 0)
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

        //Fonction qui repare une archive
        public static bool Repair(DirectoryInfo workingDir)
        {
            try
            {
                int exitCode = -1;
                FileInfo par2File = workingDir.GetFiles("*.par2").FirstOrDefault();
                if (par2File == null)
                {
                    return false;
                }
                string argThread = (Threads > 0 ? "-t" + Threads.ToString() : "");
                string args = string.Format(@"repair {1} ""{0}""", par2File.FullName, argThread);
                if (ProcessWrapper.Run(workingDir.FullName, ExePar2, args, TIMEOUT_MS, out exitCode) == true)
                {
                    if (exitCode == 0)
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


        //Fonction qui supprime tous les fichiers par du repertoire
        public static bool RemoveAllParFiles(DirectoryInfo workingDir)
        {
            try
            {
                FileInfo[] filesToDelete = workingDir.GetFiles("*." + EXT_PAR2);
                foreach (FileInfo f in filesToDelete)
                {
                    Utilities.FileDelete(f.FullName);
                }
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
