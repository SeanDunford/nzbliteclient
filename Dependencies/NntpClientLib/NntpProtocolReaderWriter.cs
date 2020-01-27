using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.Security;
using Dependencies.NntpClientLib;
using System.Security.Cryptography.X509Certificates;

namespace NntpClientLib
{
    internal class NntpProtocolReaderWriter : IDisposable
    {
        public static bool DEBUG = false; 
        public bool SSL = false;
        public const string NEWLINE = "\r\n";

        private TcpClient m_connection;
        
        private Stream m_network;

        private StreamWriter m_writer;
        private NntpStreamReader m_reader;

        private System.Text.Encoding m_enc = Rfc977NntpClient.DefaultEncoding;
        internal Encoding DefaultTextEncoding
        {
            get { return m_enc; }
        }

        private Boolean CertValidationCallback(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;    //HACK: this should be worked out better, right now we accept all SSL Certs.
        }

        internal NntpProtocolReaderWriter(TcpClient connection, bool SSL, string authdomain)
        {
            this.SSL = SSL;
            m_connection = connection;

            //m_connection.ReceiveBufferSize = 6000;

			// ssl needs longer to connect but then its kinda ok after it has done its stuff

            if (!SSL)
            {
                m_network = (NetworkStream)m_connection.GetStream();
            }
            else
            {
				//m_network = new SslStream(m_connection.GetStream(), false, CertValidationCallback);
                m_network = new SslStream(m_connection.GetStream(), true, CertValidationCallback);
                ((SslStream)m_network).AuthenticateAsClient (authdomain);
            }
            m_writer = new StreamWriter(m_network, DefaultTextEncoding);
            m_writer.NewLine = NEWLINE;
            m_writer.AutoFlush = true;
            m_reader = new NntpStreamReader(m_network);
        }

        internal string ReadLine()
        {
            string s = m_reader.ReadLine();
            if (DEBUG)
            {
                Console.WriteLine(">> "+ s);
            }
            return s;
        }

        internal string ReadResponse()
        {
            m_lastResponse = m_reader.ReadLine();
            if (DEBUG)
            {
                Console.WriteLine("< " + m_lastResponse);
            }
            return m_lastResponse;
        }

        private string m_lastResponse;
        internal string LastResponse
        {
            get { return m_lastResponse; }
        }

        internal int LastResponseCode
        {
            get
            {
                if (string.IsNullOrEmpty(m_lastResponse))
                {
                    throw new InvalidOperationException(NntpErrorMessages.ERROR_41);
                }
                if (m_lastResponse.Length > 2)
                {
                    return Convert.ToInt32(m_lastResponse.Substring(0, 3), System.Globalization.CultureInfo.InvariantCulture);
                }
                throw new InvalidOperationException(NntpErrorMessages.ERROR_42);
            }
        }

        private string m_lastCommand;
        internal string LastCommand
        {
            get { return m_lastCommand; }
        }

        internal void WriteCommand(string line)
        {
            if (DEBUG)
            {
                Console.WriteLine("> " + line);
            }
            m_lastCommand = line;
            m_writer.WriteLine(line);
        }

        internal void WriteLine(string line)
        {
            if (DEBUG)
            {
                Console.WriteLine("> " + line);
            }
			m_writer.WriteLine(line);
            m_writer.Flush();
        }

        internal void Write(string line)
        {
            if (DEBUG)
            {
                Console.WriteLine("> " + line);
            }
            m_writer.Write(line);
            m_writer.Flush();
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (m_connection == null)
            {
                return;
            }
            try
            {
                m_writer.Close();
            }
            catch { }
            m_writer = null;

            try
            {
                m_reader.Close();
            }
            catch { }
            m_reader = null;

            if (m_connection != null)
            {
                try
                {
                    m_connection.GetStream().Close();
                }
                catch { }
            }
        }

        #endregion
    }
}

