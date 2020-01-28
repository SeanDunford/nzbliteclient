using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Classes;
using Usenet;

namespace Classes
{
    class DownloadLink
    {
        private const string LOGNAME = "[DOWNLOADLINK]";
        private const byte VERSION = 3;
        private const string GZIP_HEADER = "1F8B080000000000000A";

        #region Properties
        public Guid Id;
        public string Group;
        public string Poster;
        public uint PostDate;
        public bool Encrypted;
        public string Name;
        public string Checksum;
        public Dictionary<string, DownloadLinkFileInfo> DicoOfPassNumberPerExtension; //key: fileExtension - value: DownloadLinkFileInfo
        #endregion

        public DownloadLink(Guid id, string name, string checksum, string group, string poster, uint postDate, bool encrypted, Dictionary<string, DownloadLinkFileInfo> dicoOfPassNumberPerExtension)
        {
            try
            {
                Id = id;
                Name = name;
                Checksum = checksum;
                Group = group;
                Poster = poster;
                PostDate = postDate;
                Encrypted = encrypted;
                DicoOfPassNumberPerExtension = dicoOfPassNumberPerExtension;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }

        public static string ToString(DownloadLink dl)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        bw.Write(VERSION);
                        bw.Write(dl.Id.ToByteArray());
                        StringWrite(bw, dl.Name);
                        bw.Write(Utilities.HexToBytes(dl.Checksum));
                        StringWrite(bw, dl.Group);
                        StringWrite(bw, dl.Poster);
                        bw.Write(dl.PostDate);
                        bw.Write(dl.Encrypted ? (byte)1 : (byte)0);
                        bw.Write((ushort)dl.DicoOfPassNumberPerExtension.Count);
                        foreach (KeyValuePair<string, DownloadLinkFileInfo> kvp in dl.DicoOfPassNumberPerExtension)
                        {
                            StringWrite(bw, kvp.Key);
                            bw.Write(kvp.Value.Size);
                            bw.Write((int)kvp.Value.ListOfPassNumber.Count);
                            foreach (byte b in kvp.Value.ListOfPassNumber)
                            {
                                bw.Write(b);
                            }
                        }
                        return Utilities.BytesToHex(Zipper.Zip(ms.ToArray())).Substring(GZIP_HEADER.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return null;
        }

        public static DownloadLink Parse(string str)
        {
            try
            {
                byte tmpVersion;
                Guid tmpId;
                string tmpName;
                string tmpChecksum;
                string tmpGroup;
                string tmpPoster;
                uint tmpPostDate;
                bool tmpEncrypted;
                Dictionary<string, DownloadLinkFileInfo> tmpDicoOfPassNumberPerExtension = new Dictionary<string, DownloadLinkFileInfo>();

                byte[] b = Utilities.HexToBytes(GZIP_HEADER + str);
                b = Zipper.Unzip(b);
                using (BinaryReader br = new BinaryReader(new MemoryStream(b)))
                {
                    tmpVersion = br.ReadByte(); //atm not used
                    if (tmpVersion == 3)
                    {
                        tmpId = new Guid(br.ReadBytes(16));
                        tmpName = StringRead(br);
                        tmpChecksum = Utilities.BytesToHex(br.ReadBytes(16));
                        tmpGroup = StringRead(br);
                        tmpPoster = StringRead(br);
                        tmpPostDate = br.ReadUInt32();
                        tmpEncrypted = br.ReadBoolean();
                        ushort nbExt = br.ReadUInt16();

                        for (ushort i = 0; i < nbExt; i++)
                        {
                            string key = StringRead(br);
                            long filesize = br.ReadInt64();
                            int nbChunks = br.ReadInt32();
                            DownloadLinkFileInfo dlfi = new DownloadLinkFileInfo() { Size = filesize, ListOfPassNumber = new List<byte>(nbChunks) };
                            for (ushort j = 0; j < nbChunks; j++)
                            {
                                dlfi.ListOfPassNumber.Add(br.ReadByte());
                            }
                            tmpDicoOfPassNumberPerExtension[key] = dlfi;
                        }
                        return new DownloadLink(tmpId, tmpName, tmpChecksum, tmpGroup, tmpPoster, tmpPostDate, tmpEncrypted, tmpDicoOfPassNumberPerExtension);
                    }
                    else if (tmpVersion <= 2) //COMPATIBILITY ONLY
                    {
                        tmpId = new Guid(br.ReadBytes(16));
                        tmpName = br.ReadString();
                        tmpChecksum = br.ReadString();
                        tmpGroup = br.ReadString();
                        tmpPoster = br.ReadString();
                        tmpPostDate = br.ReadUInt32();
                        tmpEncrypted = br.ReadBoolean();
                        ushort nbExt = br.ReadUInt16();

                        for (ushort i = 0; i < nbExt; i++)
                        {
                            string key = br.ReadString();
                            long filesize = br.ReadInt64();
                            int nbChunks = 0;
                            if (tmpVersion == 1)
                            {
                                nbChunks = br.ReadUInt16();
                            }
                            else if (tmpVersion == 2)
                            {
                                nbChunks = br.ReadInt32();
                            }
                            DownloadLinkFileInfo dlfi = new DownloadLinkFileInfo() { Size = filesize, ListOfPassNumber = new List<byte>(nbChunks) };
                            for (ushort j = 0; j < nbChunks; j++)
                            {
                                dlfi.ListOfPassNumber.Add(br.ReadByte());
                            }
                            tmpDicoOfPassNumberPerExtension[key] = dlfi;
                        }
                        return new DownloadLink(tmpId, tmpName, tmpChecksum, tmpGroup, tmpPoster, tmpPostDate, tmpEncrypted, tmpDicoOfPassNumberPerExtension);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return null;
        }

        public bool ToNzb(string outputDir)
        {
            try
            {
                const string EXT_NZB = ".nzb";
                string destFile = Path.Combine(outputDir, Name + EXT_NZB);
                using (StreamWriter sw = new StreamWriter(destFile, false, Encoding.UTF8))
                {
                    sw.NewLine = "\r\n";
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf - 8\"?>");
                    sw.WriteLine("<!DOCTYPE nzb PUBLIC \" -//newzBin//DTD NZB 1.1//EN\" \"http://www.newzbin.com/DTD/nzb/nzb-1.1.dtd\">");
                    sw.WriteLine("<nzb xmlns=\"http://www.newzbin.com/DTD/2003/nzb\">");

                    foreach (KeyValuePair<string, DownloadLinkFileInfo> kvp in DicoOfPassNumberPerExtension)
                    {
                        string filename = Id.ToString() + kvp.Key;
                        string msgSubject = UsenetChunk.GenerateChunkSubject(Id, 0, kvp.Value.ListOfPassNumber.Count);
                        sw.WriteLine("<file poster=\"" + HttpUtility.HtmlEncode(Poster) + "\" date=\"" + PostDate + "\" subject=\"" + HttpUtility.HtmlEncode(msgSubject) + "\">");
                        sw.WriteLine("<groups>");
                        sw.WriteLine("<group>" + Group + "</group>");
                        sw.WriteLine("</groups>");
                        sw.WriteLine("<segments>");
                        for (int i = 0; i < kvp.Value.ListOfPassNumber.Count; i++)
                        {
                            string msgId = UsenetServer.IdToMessageId(UsenetChunk.GenerateChunkId(Id, i, kvp.Value.ListOfPassNumber[i]));
                            long size = Utilities.ARTICLE_SIZE;
                            if (i == kvp.Value.ListOfPassNumber.Count - 1)
                            {
                                size = kvp.Value.Size % Utilities.ARTICLE_SIZE;
                            }
                            sw.WriteLine("<segment bytes=\"" + size + "\" number=\"" + (i + 1) + "\">" + msgId + "</segment>");
                        }
                        sw.WriteLine("</segments>");
                        sw.WriteLine("</file>");
                    }
                    sw.WriteLine("</nzb>");
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        private static void StringWrite(BinaryWriter bw, string value)
        {
            byte[] b = Utilities.UTF8.GetBytes(value);
            if (b.Length > ushort.MaxValue)
            {
                b = b.Take(ushort.MaxValue).ToArray();
            }
            bw.Write((ushort)b.Length);
            bw.Write(b);
        }

        private static string StringRead(BinaryReader br)
        {
            ushort len = br.ReadUInt16();
            byte[] b = br.ReadBytes(len);
            return Utilities.UTF8.GetString(b);
        }
    }

    public class DownloadLinkFileInfo
    {
        public long Size;
        public List<byte> ListOfPassNumber;
    }
}

