using NntpClientLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Classes;
using yEnc;
using ProxyLib.Proxy;

namespace Usenet
{
    public class UsenetServer
    {
        private const string LOGNAME = "[USENETSERVER]";
        public const byte MAX_PASS = 10;

        Rfc977NntpClientWithExtensions client;

        string username;
        string password;
        string serverAddress;
        bool useSSL;
        ushort port;
        string newsgroup;
        IProxyClient proxyClient;
        //string postFromUser;

        public UsenetServer(string server, ushort port, bool useSSL, string username, string password, string newsgroup, IProxyClient proxyClient) //string postFromUser
        {
            this.username = username;
            this.password = password;
            this.serverAddress = server;
            this.useSSL = useSSL;
            this.port = port;
            this.newsgroup = newsgroup;
            this.proxyClient = proxyClient;
            //this.postFromUser = postFromUser;
        }

        public bool Connected
        {
            get
            {
                if (client == null)
                    return false;

                // polling?

                return client.Connected;
            }
        }

        public bool Connect()
        {
            try
            {
                client = ConnectUsenet();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool Close()
        {
            try
            {
                if (client != null && client.Connected)
                {
                    client.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public byte[] DownloadSubject(string id)
        {
            if (CheckConnexion() == false)
            {
                return null;
            }
            try
            {
                var str = client.RetrieveArticleHeader(IdToMessageId(id))["Subject"][0];
                return Utilities.UTF8.GetBytes(str);
            }
            catch (Exception ex)
            {
                if (client.LastNntpResponse.StartsWith("430 "))
                {
                    return null;
                }
                throw ex;
            }
        }

        public byte[] Download(UsenetChunk chunk)
        {
            if (CheckConnexion() == false)
            {
                return null;
            }

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Download(client.RetrieveArticleBody(IdToMessageId(chunk.Id)), ms);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                //if (nntpClient.LastNntpResponse.StartsWith("430 No Such Article"))
                //{
                //    return null;
                //}
            }
            return null;
        }


        private void Download(IEnumerable<string> body, Stream outputStream)
        {
            byte[] buffer = new byte[4096];
            using (MemoryStream tmpStream = new MemoryStream())
            {
                tmpStream.Position = 0;
                StreamWriter sw = new StreamWriter(tmpStream, System.Text.Encoding.GetEncoding("ISO-8859-1"));
                sw.NewLine = Rfc977NntpClient.NEWLINE;
#if YENC_HEADER_FOOTER
				List<string> lines = new List<string>(body);

				if (lines.Count < 2) throw new InvalidDataException("Not enough data in article");
				if (!lines[0].StartsWith("=ybegin")) throw new InvalidDataException("Must start with =ybegin");
				if (!lines[lines.Count - 1].StartsWith("=yend")) throw new InvalidDataException("Must end with =yend");

				lines.RemoveAt(0);
				lines.RemoveAt(lines.Count - 1);

				foreach (var line in lines)
				{
					sw.WriteLine(line);
				}
#else
                foreach (var line in body)
                {
                    sw.WriteLine(line);
                }
#endif

                sw.Flush();
                tmpStream.Position = 0;

                YEncDecoder yencDecoder = new YEncDecoder();

                using (CryptoStream yencStream = new CryptoStream(tmpStream, yencDecoder, CryptoStreamMode.Read))
                {
                    int read = 0;
                    while ((read = yencStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        outputStream.Write(buffer, 0, read);
                    }
                }
            }
        }

        public IEnumerable<string> Upload(int size, Stream inputStream)
        {
            var yencEncoder = new YEncEncoder();
            var buffer = new byte[4096];

            using (MemoryStream tmpStream = new MemoryStream((int)(inputStream.Length)))
            {
                using (CryptoStream yencStream = new CryptoStream(tmpStream, yencEncoder, CryptoStreamMode.Write))
                {
                    int toRead = size;

                    inputStream.Position = 0;

                    for (int i = 0; i < toRead / buffer.Length; i++)
                    {
                        inputStream.Read(buffer, 0, buffer.Length);
                        yencStream.Write(buffer, 0, buffer.Length);
                    }
                    if (toRead % buffer.Length != 0)
                    {
                        inputStream.Read(buffer, 0, toRead % buffer.Length);
                        yencStream.Write(buffer, 0, toRead % buffer.Length);
                    }

                    tmpStream.Position = 0;
                    StreamReader sr = new StreamReader(tmpStream, System.Text.Encoding.GetEncoding("ISO-8859-1"));

#if YENC_HEADER_FOOTER
					yield return $"=ybegin line=128 size={size} name={Cryptography.GetRandomBytes(8).ToHexString()}.blob";
#endif
                    while (sr.Peek() >= 0)
                    {
                        yield return sr.ReadLine();
                    }
#if YENC_HEADER_FOOTER
					yield return $"=yend size={size}";
#endif
                }
            }
        }

        public IEnumerable<string> Upload(byte[] buffer)
        {
            var yencEncoder = new YEncEncoder();

            using (MemoryStream tmpStream = new MemoryStream(buffer.Length))
            {
                using (CryptoStream yencStream = new CryptoStream(tmpStream, yencEncoder, CryptoStreamMode.Write))
                {
                    yencStream.Write(buffer, 0, buffer.Length);

                    tmpStream.Position = 0;
                    StreamReader sr = new StreamReader(tmpStream, System.Text.Encoding.GetEncoding("ISO-8859-1"));

#if YENC_HEADER_FOOTER
					yield return $"=ybegin line=128 size={size} name={Cryptography.GetRandomBytes(8).ToHexString()}.blob";
#endif
                    while (sr.Peek() >= 0)
                    {
                        yield return sr.ReadLine();
                    }
#if YENC_HEADER_FOOTER
					yield return $"=yend size={size}";
#endif
                }
            }
        }

        public Rfc977NntpClientWithExtensions ConnectUsenet()
        {
            Rfc977NntpClientWithExtensions tmpClient = new Rfc977NntpClientWithExtensions();
            tmpClient.ConnectionTimeout = 30000;
            tmpClient.Connect(serverAddress, port, useSSL, proxyClient);
            if (username != null && password != null)
            {
                tmpClient.AuthenticateUser(username, password);
            }
            var newsgroup = this.newsgroup;
            tmpClient.SelectNewsgroup(newsgroup);
            return tmpClient;
        }

        public ArticleHeadersDictionary CreateHeader(string chunkId, string subject, string posterEmail)
        {
            string messageId = IdToMessageId(chunkId);
            string postFromUser = posterEmail;
            var headers = new ArticleHeadersDictionary();

            headers.AddHeader("From", postFromUser);
            headers.AddHeader("Subject", subject);
            headers.AddHeader("Newsgroups", this.newsgroup);
            //headers.AddHeader("Date", new NntpDateTime(DateTime.Now).ToString());
            headers.AddHeader("User-Agent", Utilities.USERAGENT);
            headers.AddHeader("Message-ID", messageId);

            return headers;
        }

        #region Custom Functions
        //Fonction qui verifie qu'on est bien connecté
        public bool CheckConnexion()
        {
            if (Connected == false)
            {
                if (Connect() == false)
                {
                    Logger.Info(LOGNAME, "Cannot connect to usenet");
                    return false;
                }
            }
            return true;
        }

        //Fonction qui verifie si un article est present sur le reseau
        public bool CheckArticle(string chunkId)
        {
            if (CheckConnexion() == false)
            {
                return false;
            }
            string messageId = IdToMessageId(chunkId);
            Rfc977NntpClientWithExtensions nntpClient = client;

            try
            {
                ArticleHeadersDictionary dico = nntpClient.RetrieveArticleHeader(messageId);
                if (dico.ContainsKey("Message-ID"))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                string k = ex.Message;
            }

            return false;
        }

        public bool Upload(UsenetChunk chunk, string posterEmail)
        {
            if (CheckConnexion() == false)
            {
                return false;
            }

            ArticleHeadersDictionary headers = CreateHeader(chunk.Id, chunk.Subject, posterEmail);

            Rfc977NntpClientWithExtensions nntpClient = client;

            var rawData = chunk.Data;

            try
            {
                try
                {
                    nntpClient.PostArticle(new ArticleHeadersDictionaryEnumerator(headers), Upload(rawData).ToList());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    if (nntpClient.LastNntpResponse.StartsWith("441 "))
                    {
                        return false;
                    }
                    //throw ex;
                }

                //if (!nntpClient.LastNntpResponse.StartsWith("240 ") && nntpClient.LastNntpResponse.Split('<')[1].Split('>')[0] == null)
                //{
                //    throw new Exception(nntpClient.LastNntpResponse);
                //}
                if (nntpClient.LastNntpResponse.StartsWith("240 "))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                //nothing todo
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return false;
        }

        public static string IdToMessageId(string chunkId)
        {
            //var res = $"<{chunkId.Substring(0, chunkId.Length - 32)}@{chunkId.Substring(chunkId.Length - 32) }.local>";
            var res = $"<{chunkId.Substring(0, chunkId.Length - 16)}@{chunkId.Substring(chunkId.Length - 48)}.local>";
            return res;
        }
        #endregion
    }
}
