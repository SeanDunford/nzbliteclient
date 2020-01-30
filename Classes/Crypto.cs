using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Classes
{
    public class Crypto
    {
        #region Constantes and Properties
        private const string LOGNAME = "[CRYPTO]";
        #endregion

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
                byte[] b = sha256Hash.ComputeHash(Utilities.UTF8.GetBytes(rawData));
                return Utilities.BytesToHex(b);
            }
        }

        public static byte[] GenerateEncryptionKey(Guid guidForEnc, EncryptionMode encMode)
        {
            try
            {
                byte[] b = guidForEnc.ToByteArray();
                if (encMode == EncryptionMode.XOR)
                {
                    List<byte> ret = new List<byte>(Utilities.ARTICLE_SIZE);
                    while (ret.Count < Utilities.ARTICLE_SIZE)
                    {
                        ret.AddRange(b);
                    }
                    return ret.ToArray();
                }
               
                return b;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return null;
        }


        #region Xor
        public static bool Xorify(ref byte[] buf, int bufLen, byte[] encryptionKey)
        {
            try
            {
                int encLen = encryptionKey.Length;
                for (int i = 0; i < bufLen; i++)
                {
                    buf[i] = (byte)(buf[i] ^ encryptionKey[i % encLen]);
                }
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
