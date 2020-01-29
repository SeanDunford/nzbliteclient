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
        public const byte VERSION = 4;

        #region Properties
        public Guid Id;
        public byte Version;
        public uint PostDate;
        public Utilities.EncryptionMode EncryptionMode;
        public string Name;
        public string Checksum;
        public Dictionary<string, DownloadLinkFileInfo> DicoOfPassNumberPerExtension; //key: fileExtension - value: DownloadLinkFileInfo
        #endregion

        public DownloadLink(byte version, Guid id, string name, string checksum, uint postDate, Utilities.EncryptionMode encryptionMode, Dictionary<string, DownloadLinkFileInfo> dicoOfPassNumberPerExtension)
        {
            try
            {
                Version = version;
                Id = id;
                Name = name;
                Checksum = checksum;
                PostDate = postDate;
                EncryptionMode = encryptionMode;
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
                        bw.Write(dl.Version);
                        bw.Write(dl.Id.ToByteArray());
                        StringWrite(bw, dl.Name);
                        bw.Write(Utilities.HexToBytes(dl.Checksum));
                        bw.Write(dl.PostDate);
                        bw.Write((byte)dl.EncryptionMode);
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
                        return Utilities.BytesToHex(Zipper.Zip(ms.ToArray()));
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
                uint tmpPostDate;
                Utilities.EncryptionMode tmpEncryption;
                Dictionary<string, DownloadLinkFileInfo> tmpDicoOfPassNumberPerExtension = new Dictionary<string, DownloadLinkFileInfo>();

                byte[] b = Utilities.HexToBytes(str);
                b = Zipper.Unzip(b);
                using (BinaryReader br = new BinaryReader(new MemoryStream(b)))
                {
                    tmpVersion = br.ReadByte();
                    tmpId = new Guid(br.ReadBytes(16));
                    tmpName = StringRead(br);
                    tmpChecksum = Utilities.BytesToHex(br.ReadBytes(16));
                    tmpPostDate = br.ReadUInt32();
                    tmpEncryption = (Utilities.EncryptionMode)br.ReadByte();
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
                    return new DownloadLink(tmpVersion, tmpId, tmpName, tmpChecksum, tmpPostDate, tmpEncryption, tmpDicoOfPassNumberPerExtension);
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
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    sw.WriteLine("<!DOCTYPE nzb PUBLIC \" -//newzBin//DTD NZB 1.1//EN\" \"http://www.newzbin.com/DTD/nzb/nzb-1.1.dtd\">");
                    sw.WriteLine("<nzb xmlns=\"http://www.newzbin.com/DTD/2003/nzb\">");

                    foreach (KeyValuePair<string, DownloadLinkFileInfo> kvp in DicoOfPassNumberPerExtension)
                    {
                        string filename = Id.ToString() + kvp.Key;
                        string msgSubject = UsenetChunk.GenerateChunkSubject(Id, Utilities.EXT_RAW, 0, kvp.Value.ListOfPassNumber.Count);
                        sw.WriteLine("\t<file poster=\"" + Utilities.USERAGENT + "\" date=\"" + PostDate + "\" subject=\"" + HttpUtility.HtmlEncode(msgSubject) + "\">");
                        sw.WriteLine("\t\t<groups>");
                        sw.WriteLine("\t\t\t<group>alt.binary.backup</group>");
                        sw.WriteLine("\t\t</groups>");
                        sw.WriteLine("\t\t<segments>");
                        for (int i = 0; i < kvp.Value.ListOfPassNumber.Count; i++)
                        {
                            string msgId = UsenetServer.IdToMessageId(UsenetChunk.GenerateChunkId(Id, kvp.Key, i, kvp.Value.ListOfPassNumber[i]));
                            msgId = msgId.Substring(1, msgId.Length - 2);
                            long size = Utilities.ARTICLE_SIZE;
                            if (i == kvp.Value.ListOfPassNumber.Count - 1)
                            {
                                size = kvp.Value.Size % Utilities.ARTICLE_SIZE;
                            }
                            sw.WriteLine("\t\t\t<segment bytes=\"" + size + "\" number=\"" + (i + 1) + "\">" + msgId + "</segment>");
                        }
                        sw.WriteLine("\t\t</segments>");
                        sw.WriteLine("\t</file>");
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
            if (string.IsNullOrEmpty(value))
            {
                bw.Write((ushort)0);
                return;
            }
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
            if (len == 0)
            {
                return string.Empty;
            }
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

