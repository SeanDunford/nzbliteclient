using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Classes
{
    public class Renamer
    {
        private const string LOGNAME = "[RENAMER]";
        public static string Rename(string s)
        {
            try
            {
                if (string.IsNullOrEmpty(s))
                {
                    return "";
                }
                s = ReplaceKeywords(s);
                string[] tmp = s.Split(".");
                for (int i = 0; i < tmp.Length; i++)
                {
                    tmp[i] = Capitalize(tmp[i]);
                }
                return string.Join(".", tmp);
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return s;
        }
        private static string ReplaceKeywords(string s)
        {
            try
            {
                if (string.IsNullOrEmpty(s))
                {
                    return "";
                }
                s = s.ToLower();
                s = s.Replace("french", "FRENCH");
                s = s.Replace("dvdrip", "DVDRiP");
                s = s.Replace("bluray", "BluRay");
                s = s.Replace("multi", "MULTi");
                s = s.Replace("webdl", "WebDL");
                s = s.Replace("web-dl", "WebDL");
                s = s.Replace("hdlight", "HDLight");
                s = s.Replace("webrip", "WEBRiP");
                s = s.Replace("dts", "DTS");
                s = s.Replace("ac3", "AC3");
                s = s.Replace("internal", "iNTERNAL");
                s = s.Replace(" ", ".");
                s = s.Replace("'", ".");
                s = s.Replace("&", "And");
                s = s.Replace("..........", ".");
                s = s.Replace(".........", ".");
                s = s.Replace("........", ".");
                s = s.Replace(".......", ".");
                s = s.Replace("......", ".");
                s = s.Replace(".....", ".");
                s = s.Replace("....", ".");
                s = s.Replace("...", ".");
                s = s.Replace("..", ".");
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return s;
        }

        private static string Capitalize(string s)
        {
            try
            {
                if (string.IsNullOrEmpty(s))
                {
                    return "";
                }
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s);
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return s;
        }
    }
}
