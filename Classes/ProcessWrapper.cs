using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Classes
{
    public class ProcessWrapper
    {
        private const string LOGNAME = "[PROCESSWRAPPER]";

        #region Run
        /// <summary>
        /// Run an external process
        /// </summary>
        public static bool Run(string workingDir, string exeLocation, string args, int timeoutMs, out int exitCode)
        {
            try
            {
                using (Process proc = new Process())
                {
                    proc.StartInfo.Arguments = args;
                    proc.StartInfo.WorkingDirectory = workingDir;
                    proc.StartInfo.FileName = exeLocation;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.Start();
                    proc.WaitForExit(timeoutMs);
                    exitCode = proc.ExitCode;
                }
                return true;
            }
            catch (Exception ex)
            {
                exitCode = -1;
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }



        #endregion
    }
}
