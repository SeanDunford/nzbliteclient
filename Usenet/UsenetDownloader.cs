using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Classes;

namespace Usenet
{
    class UsenetDownloader
    {
        private const string LOGNAME = "[USENETDOWNLOADER]";
        private const int MAX_RETRY = 3;
        private static ConcurrentQueue<UsenetChunk> _queueOfChunks = new ConcurrentQueue<UsenetChunk>();
        private static int _remainingChunks = 0;
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

        public static bool Run(byte[] encKey)
        {
            try
            {
                _remainingChunks = _queueOfChunks.Count;
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
                            chunk.SetId(chunk.PassNumber);
                            for (int i = 0; i < MAX_RETRY; i++)
                            {
                                byte[] rawdata = us.Download(chunk);
                                if (rawdata == null && i == MAX_RETRY - 1)
                                {
                                    Logger.Warn(LOGNAME, "Cannot download chunk " + chunk.Fi.Name + " (#" + chunk.ChunkNumber + ")");
                                }
                                else
                                {
                                    chunk.DataSet(rawdata, _encKey);
                                    chunk.WriteDataToBw();
                                }
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
