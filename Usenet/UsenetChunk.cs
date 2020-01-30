using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Classes;
using FileWorkers;

namespace Usenet
{
    public class UsenetChunk
    {
        private const string LOGNAME = "[USENETCHUNK]";
        private const char SEPARATOR = '|';

        #region Upload Properties
        public BinaryReader Br;
        public byte PassNumber;
        #endregion

        #region Download Properties
        public BinaryWriter Bw;
        #endregion

        #region Properties
        public string Filename;
        public Guid FileId;
        public string ChunkExt;
        public int ChunkNumber;
        public int TotalChunks;
        public string Id;
        public string Subject;
        public byte[] Data;
        public Crypto.EncryptionMode EncryptionMode;
        #endregion

        #region Constructors
        //Constructor for upload
        public UsenetChunk(BinaryReader br, string filename, Guid fileId,string chunkExt, int chunkNumber, int totalChunks, Crypto.EncryptionMode encryptionMode)
        {
            Br = br;
            Filename = filename;
            FileId = fileId;
            ChunkExt = chunkExt;
            ChunkNumber = chunkNumber;
            TotalChunks = totalChunks;
            EncryptionMode = encryptionMode;
        }

        //Constructor for download
        public UsenetChunk(BinaryWriter bw, string filename, Guid fileId, string chunkExt, int chunkNumber, byte passNumber, int totalChunks, Crypto.EncryptionMode encryptionMode)
        {
            Bw = bw;
            Filename = filename;
            FileId = fileId;
            ChunkExt = chunkExt;
            ChunkNumber = chunkNumber;
            TotalChunks = totalChunks;
            PassNumber = passNumber;
            EncryptionMode = encryptionMode;
        }
        #endregion

        public void SetDataFromBr(byte[] encKey)
        {
            try
            {
                lock (Br)
                {
                    Br.BaseStream.Position = ((long)(ChunkNumber) * Utilities.ARTICLE_SIZE);
                    Data = Br.ReadBytes(Utilities.ARTICLE_SIZE);
                }
                if (EncryptionMode == Crypto.EncryptionMode.XOR)
                {
                    Crypto.Xorify(ref Data, Data.Length, encKey);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }
        public void WriteDataToBw()
        {
            try
            {
                lock (Bw)
                {
                    Bw.BaseStream.Position = ((long)(ChunkNumber) * Utilities.ARTICLE_SIZE);
                    Bw.Write(Data);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        public void DataSet(byte[] rawData, byte[] encKey)
        {
            try
            {
                Data = rawData;
                if (EncryptionMode == Crypto.EncryptionMode.XOR)
                {
                    Crypto.Xorify(ref Data, Data.Length, encKey);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        public void DataRaz()
        {
            try
            {
                Data = null;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        public void SetSubject()
        {
            try
            {
                Subject = GenerateChunkSubject(FileId, ChunkExt, ChunkNumber, TotalChunks);
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        public void SetId(byte passNumber)
        {
            try
            {
                PassNumber = passNumber;
                Id = GenerateChunkId(FileId, ChunkExt, ChunkNumber, PassNumber);
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        public static string GenerateChunkId(Guid fileId, string chunkExt, long chunkNumber, int passNumber)
        {
            try
            {
                string[] arr = { fileId.ToString(), chunkExt, chunkNumber.ToString(), passNumber.ToString() };
                return Crypto.GenerateHash(string.Join(SEPARATOR, arr));
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return null;
        }

        public static string GenerateChunkSubject(Guid fileId, string chunkExt, long chunkNumber, long totalChunks)
        {
            try
            {
                //format: [01/10] - "JWR1574809494ip3UC191127MUN.part1.rar" yEnc (1/358)
                //string[] arr = { filename, chunkExt };
                return "[01/01] - \"" + Crypto.GenerateHash(fileId.ToString() + SEPARATOR + chunkExt) + "\" yEnc (" + (chunkNumber + 1) + "/" + totalChunks + ")";
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return null;
        }

        //#region Compatibility with NzbLite Format v2
        //public void CompatV2SetId(byte passNumber)
        //{
        //    try
        //    {
        //        PassNumber = passNumber;
        //        Id = CompatV2GenerateChunkId(Filename, ChunkNumber, PassNumber);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(LOGNAME, ex.Message, ex);
        //    }
        //}
        //public static string CompatV2GenerateChunkId(string filename, long chunkNumber, int passNumber)
        //{
        //    try
        //    {
        //        string[] arr = { filename, chunkNumber.ToString(), passNumber.ToString() };
        //        return Utilities.GenerateHash(string.Join(SEPARATOR, arr));
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(LOGNAME, ex.Message, ex);
        //    }
        //    return null;
        //}
        //#endregion
    }
}
