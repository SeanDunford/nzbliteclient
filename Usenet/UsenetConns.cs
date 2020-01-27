using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Classes;
using ProxyLib.Proxy;

namespace Usenet
{
    public class UsenetConns
    {
        #region Properties
        private const string LOGNAME = "[USENETCONNS]";
        public static List<UsenetServer> ListOfConns = new List<UsenetServer>();
        #endregion

        #region Start / Stop
        public static bool Start()
        {
            try
            {
                for (int i = 0; i < Settings.Settings.Current.UsenetSlots; i++)
                {
                    IProxyClient proxyClient = GetProxy();
                    if (i == 0 && proxyClient != null)
                    {
                        Logger.Info(LOGNAME, "Proxy enabled (" + Settings.Settings.Current.ProxyType + "): " + Settings.Settings.Current.ProxyServer + ":" + Settings.Settings.Current.ProxyPort);
                    }
                    UsenetServer us = new UsenetServer(Settings.Settings.Current.UsenetServer, Settings.Settings.Current.UsenetPort, Settings.Settings.Current.UsenetUseSSL, Settings.Settings.Current.UsenetUsername, Settings.Settings.Current.UsenetPassword, Settings.Settings.Current.UsenetNewsgroup, proxyClient);
                    ListOfConns.Add(us);
                }
                Logger.Info(LOGNAME, "UsenetConns loaded");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        public static bool Stop()
        {
            try
            {
                foreach (UsenetServer us in ListOfConns)
                {
                    us.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }

        private static IProxyClient GetProxy()
        {
            IProxyClient proxyClient = null;
            if (Settings.Settings.Current.ProxyEnabled == true && string.IsNullOrEmpty(Settings.Settings.Current.ProxyType) == false && string.IsNullOrEmpty(Settings.Settings.Current.ProxyServer) == false && Settings.Settings.Current.ProxyPort > 0)
            {
                if (Settings.Settings.Current.ProxyType.ToLower() == "socks5")
                {
                    proxyClient = new Socks5ProxyClient(Settings.Settings.Current.ProxyServer, Settings.Settings.Current.ProxyPort, Settings.Settings.Current.ProxyUsername, Settings.Settings.Current.ProxyPassword);
                }
                else if (Settings.Settings.Current.ProxyType.ToLower() == "socks4")
                {
                    proxyClient = new Socks4ProxyClient(Settings.Settings.Current.ProxyServer, Settings.Settings.Current.ProxyPort, Settings.Settings.Current.ProxyUsername);
                }
                else if (Settings.Settings.Current.ProxyType.ToLower() == "http")
                {
                    proxyClient = new HttpProxyClient(Settings.Settings.Current.ProxyServer, Settings.Settings.Current.ProxyPort, Settings.Settings.Current.ProxyUsername, Settings.Settings.Current.ProxyPassword);
                }
                if (proxyClient != null)
                {
                    //Setup timeouts
                    proxyClient.ReceiveTimeout = (int)TimeSpan.FromSeconds(600).TotalMilliseconds;
                    proxyClient.SendTimeout = (int)TimeSpan.FromSeconds(600).TotalMilliseconds;
                }
            }
            return proxyClient;
        }
        #endregion
    }
}
