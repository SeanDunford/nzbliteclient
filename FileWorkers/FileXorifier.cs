using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Classes;

namespace FileWorkers
{
    class FileXorifier
    {
        private const string LOGNAME = "[FILEXORIFIER]";

        ///// <summary>
        ///// Fonction qui permet de copier un fichier en async
        ///// </summary>
        //public static bool CopyAndXorFile(string sourceFile, string destinationFile, Guid metafileId)
        //{
        //    try
        //    {
        //        byte[] encKey = metafileId.ToByteArray();
        //        using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, Utilities.BUFFER_SIZE))
        //        {
        //            using (BinaryWriter bw = new BinaryWriter(new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, Utilities.BUFFER_SIZE)))
        //            {
        //                byte[] buf = new byte[Utilities.BUFFER_SIZE];
        //                int nbRead;
        //                while ((nbRead = sourceStream.Read(buf, 0, buf.Length)) > 0)
        //                {
        //                    Xorify(ref buf, nbRead, encKey);
        //                    bw.Write(buf, 0, nbRead);
        //                }
        //            }
        //        }
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(LOGNAME, ex.Message, ex);
        //    }
        //    return false;            
        //}

        public static byte[] GenerateXorKey(Guid guidForEnc)
        {
            try
            {
                byte[] b = guidForEnc.ToByteArray();
                List<byte> ret = new List<byte>(Utilities.ARTICLE_SIZE);
                while (ret.Count < Utilities.ARTICLE_SIZE)
                {
                    ret.AddRange(b);
                }
                return ret.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return null;
        }

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
    }
}
