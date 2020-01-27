using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Classes;

namespace Usenet
{
    class UsenetUploader
    {
        private const string LOGNAME = "[USENETUPLOADER]";

        private static ConcurrentQueue<UsenetChunk> _queueOfChunks = new ConcurrentQueue<UsenetChunk>();
        private static int _remainingChunks = 0;
        private static string _poster;
        private static byte[] _encKey;

        public static bool IsFinished()
        {
            return (_remainingChunks == 0);
        }

        public static bool AddChunk(UsenetChunk chunk)
        {
            try
            {
                _queueOfChunks.Enqueue(chunk);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        public static bool Run(string poster, byte[] encKey)
        {
            try
            {
                _remainingChunks = _queueOfChunks.Count;
                _poster = poster;
                _encKey = encKey;
                for (int i = 0; i < UsenetConns.ListOfConns.Count; i++)
                {
                    Task t = new Task(Process, i);
                    t.Start();
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        private static void Process(object connNumber)
        {
            try
            {
                UsenetServer us = UsenetConns.ListOfConns[(int)connNumber];
                while (_queueOfChunks.IsEmpty == false)
                {
                    try
                    {
                        UsenetChunk chunk = null;
                        if (_queueOfChunks.TryDequeue(out chunk))
                        {
                            chunk.SetDataFromBr(_encKey);
                            chunk.SetSubject();
                            bool isUploaded = false;
                            for (byte passNumber = 0; passNumber < UsenetServer.MAX_PASS; passNumber++)
                            {
                                chunk.SetId(passNumber);
                                isUploaded = us.Upload(chunk, _poster);
                                if (isUploaded == true)
                                {
                                    break;
                                }
                            }
                            if (isUploaded == false)
                            {
                                chunk.SetId(UsenetServer.MAX_PASS);
                                Logger.Warn(LOGNAME, "Cannot upload chunk " + chunk.Fi.Name + " (#" + chunk.ChunkNumber + ")");
                            }
                            chunk.DataRaz();//free memory
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(LOGNAME, ex.Message, ex);
                    }
                    Interlocked.Decrement(ref _remainingChunks);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
        }
    }
}
