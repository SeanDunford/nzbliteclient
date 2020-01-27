using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Classes;

namespace Classes
{
    class Zipper
    {
        private const string LOGNAME = "[ZIPPER]";

        //Fonction qui zip array de bytes et retourne un array de bytes 
        public static byte[] Zip(byte[] b)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (GZipStream gz = new GZipStream(ms, CompressionLevel.Optimal))
                    {
                        gz.Write(b, 0, b.Length);
                    }
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return null;
        }

        //Fonction qui zip array de bytes et retourne un array de bytes 
        public static byte[] Unzip(byte[] b)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(b))
                {
                    using (GZipStream gz = new GZipStream(ms, CompressionMode.Decompress))
                    {
                        using (MemoryStream ms2 = new MemoryStream())
                        {
                            gz.CopyTo(ms2);
                            return ms2.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return null;
        }
    }
}
