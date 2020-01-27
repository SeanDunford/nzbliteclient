using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using log4net;

namespace Classes
{
    public static class Logger
    {

        private static readonly string LOG_CONFIG_FILE = "log4net.config";

        private static ILog _log;

        private static ILog GetLogger(Type type)
        {
            return LogManager.GetLogger(type);
        }

        public static void Debug(string logname, string message)
        {
            _log.Debug(logname + ' ' + message);
        }

        public static void Info(string logname, string message)
        {
            _log.Info(logname + ' ' + message);
        }

        public static void Warn(string logname, string message)
        {
            _log.Warn(logname + ' ' + message);
        }

        public static void Error(string logname, string message, Exception ex)
        {
            _log.Error(logname + ' ' + message, ex);
        }

        public static void SetLog4NetConfiguration()
        {
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead(Path.Combine(Utilities.ExecutableFolder, LOG_CONFIG_FILE)));

            var repo = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));
            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);
            _log = GetLogger(typeof(Logger));
        }
    }
}
