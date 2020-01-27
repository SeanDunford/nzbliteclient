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

        #region Upload Properties
        public BinaryReader Br;
        public byte PassNumber;
        #endregion

        #region Download Properties
        public BinaryWriter Bw;
        #endregion

        #region Properties
        public FileInfo Fi;
        public int ChunkNumber;
        public int TotalChunks;
        public string Id;
        public string Subject;
        public byte[] Data;
        public bool Encrypted;
        #endregion

        #region Constructors
        //Constructor for upload
        public UsenetChunk(BinaryReader br, FileInfo fi, int chunkNumber, int totalChunks, bool encrypted)
        {
            Br = br;
            Fi = fi;
            ChunkNumber = chunkNumber;
            TotalChunks = totalChunks;
            Encrypted = encrypted;
        }

        //Constructor for download
        public UsenetChunk(BinaryWriter bw, FileInfo fi, int chunkNumber, byte passNumber, int totalChunks, bool encrypted)
        {
            Bw = bw;
            Fi = fi;
            ChunkNumber = chunkNumber;
            TotalChunks = totalChunks;
            PassNumber = passNumber;
            Encrypted = encrypted;
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
                if (Encrypted == true)
                {
                    FileXorifier.Xorify(ref Data, Data.Length, encKey);
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
                if (Encrypted == true)
                {
                    FileXorifier.Xorify(ref Data, Data.Length, encKey);
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
                Subject = GenerateChunkSubject(Fi.Name, ChunkNumber, TotalChunks);
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
                Id = GenerateChunkId(Fi.Name, ChunkNumber, PassNumber);
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        public static string GenerateChunkId(string filename, long chunkNumber, int passNumber)
        {
            try
            {
                string[] arr = { filename, chunkNumber.ToString(), passNumber.ToString() };
                return Utilities.GenerateHash(string.Join("|||", arr));
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return null;
        }

        public static string GenerateChunkSubject(string filename, long chunkNumber, long totalChunks)
        {
            try
            {
                //format: [01/10] - "JWR1574809494ip3UC191127MUN.part1.rar" yEnc (1/358)
                //string[] arr = { filename, chunkExt };
                return "[01/01] - \"" + Utilities.GenerateHash(filename) + "\" yEnc (" + (chunkNumber + 1) + "/" + totalChunks + ")";
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return null;
        }
    }
}
