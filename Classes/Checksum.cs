using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Classes
{
    public class Checksum
    {
        private const string LOGNAME = "[CHECKSUM]";
        private const int CHECKSUM_LEN = 32;
        private const int BUFFER_SIZE = 32 * 1024 * 1000;

        public static bool CheckChecksum(string chk)
        {
            try
            {
                if (!string.IsNullOrEmpty(chk))
                {
                    return chk.Length == CHECKSUM_LEN;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        public static string Calculate(string str)
        {
            string ret = null;
            try
            {
                using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
                {
                    byte[] hash = md5.ComputeHash(Utilities.UTF8.GetBytes(str));
                    ret = BitConverter.ToString(hash).Replace("-", "");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return ret;
        }

        public static string Calculate(ref byte[] buf)
        {
            string ret = null;
            try
            {
                using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
                {
                    byte[] hash = md5.ComputeHash(buf);
                    ret = BitConverter.ToString(hash).Replace("-", "");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return ret;
        }

        public static string Calculate(FileInfo fi)
        {
            string ret = null;
            try
            {
                using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE))
                {
                    using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
                    {
                        byte[] hash = md5.ComputeHash(fs);
                        ret = BitConverter.ToString(hash).Replace("-", "");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return ret;
        }
    }
}
