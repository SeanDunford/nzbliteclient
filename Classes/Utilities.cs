using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Settings;

namespace Classes
{
    public class Utilities
    {
        #region Constants
        private const string LOGNAME = "[UTILITIES]";
        public const int ARTICLE_SIZE = 700 * 1024;
        public const int BUFFER_SIZE = 32 * 1024 * 1024;
        public const string EXT_RAW = ".raw";
        public const string EXT_BACKUP = ".backup";
        public const string USERAGENT = "NzbLiteClient";
        #endregion

        #region Static Folders and Files
        public static string ExecutableFolder = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString();
        public static string AssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        public static string FolderLogs = Path.Combine(ExecutableFolder, "logs");
        public static string FolderTemp = Path.Combine(ExecutableFolder, "temp");
        public static string FolderDownload = Path.Combine(ExecutableFolder, "download");
        public static string FileDb = Path.Combine(ExecutableFolder, "database.db");
        public static string FileSettings = Path.Combine(ExecutableFolder, "config.json");
        #endregion

        #region Static Properties
        public static Encoding UTF8 = Encoding.UTF8;
        public static Random Rnd = new Random(Environment.TickCount);
        #endregion

        #region Converters
        /// <summary>
        /// Convert a byte size in human readable format
        /// </summary>
        public static string ConvertSizeToHumanReadable(double tmpSize)
        {
            string ret = null;
            try
            {
                string[] sizes = new string[5] { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                while (tmpSize >= 1024.0 && order < sizes.Length - 1)
                {
                    order++;
                    tmpSize = unchecked(tmpSize / 1024.0);
                }
                ret = $"{(long)Math.Round(tmpSize):0.##}{sizes[order]}";
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return ret;

        }
        #endregion

        #region Dirs And Files
        /// <summary>
        /// Ensure all directories exist
        /// </summary>
        public static void EnsureDirectories()
        {
            try
            {
                //EnsureDirectory(FolderParities);
                EnsureDirectory(FolderLogs);
                EnsureDirectory(FolderTemp);
                EnsureDirectory(FolderDownload);
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        /// <summary>
        /// Ensure a directory exists
        /// </summary>
        public static void EnsureDirectory(string dirPath)
        {
            try
            {
                if (Directory.Exists(dirPath))
                {
                    return;
                }
                Directory.CreateDirectory(dirPath);
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        /// <summary>
        /// Backup a file
        /// </summary>
        public static void FileBackup(string filePath)
        {
            try
            {
                string backup = filePath + ".backup";
                FileDelete(backup);
                if (File.Exists(filePath))
                {
                    File.Move(filePath, backup);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        /// <summary>
        /// Try to delete a file
        /// </summary>
        public static void FileDelete(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        /// <summary>
        /// Fonction qui permet de savoir si un repertoire est vide
        /// </summary>
        public static bool FolderIsEmpty(string path)
        {
            int fileCount = Directory.GetFiles(path).Length;
            if (fileCount > 0)
            {
                return false;
            }

            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                if (!FolderIsEmpty(dir))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        #region Encryption
        public enum EncryptionMode
        {
            NONE = 0,
            XOR = 1
        }
        public static string GenerateHash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] b = sha256Hash.ComputeHash(UTF8.GetBytes(rawData));
                return BytesToHex(b);
            }
        }
        #endregion

        #region Hex <-> Bytes
        /// <summary>
        /// Convert byte array to hex string
        /// </summary>
        public static string BytesToHex(byte[] b)
        {
            if (b == null || b.Length == 0)
            {
                return string.Empty;
            }
            return BitConverter.ToString(b).Replace("-", "");
        }

        /// <summary>
        /// Convert hex string to byte array
        /// </summary>
        public static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return null;
            }

            int bLen = unchecked(hex.Length / 2) - 1;
            byte[] b = new byte[bLen + 1];
            int num = b.Length - 1;
            for (int i = 0; i <= num; i++)
            {
                b[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return b;
        }

        #endregion

        #region UnixTime
        public static uint UnixTimestampFromDate(DateTime d)
        {
            return (uint)d.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }
        public static DateTime UnixTimestampToDate(uint secs)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(secs);
        }
        #endregion

        #region Helpers
        public static bool IsWindowsPlatform()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        public static Version CurrentVersion()
        {
            return Assembly.GetEntryAssembly().GetName().Version;
        }

        public static void InitiateSSLTrust()
        {
            try
            {
                //Change SSL checks so that all checks pass
                ServicePointManager.ServerCertificateValidationCallback =
                   new RemoteCertificateValidationCallback(
                        delegate
                        { return true; }
                    );
            }
            catch (Exception ex)
            {
                //nothing to do
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
        #endregion
    }
}
