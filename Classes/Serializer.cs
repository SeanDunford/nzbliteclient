using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Classes
{
    public class Serializer
    {

        private const string LOGNAME = "[SERIALIZER]";
        private static JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        /// <summary>
        /// Serialize an object to a string
        /// </summary>
        public static string Serialize(object obj)
        {
            try
            {
                string str = JsonConvert.SerializeObject(obj, Formatting.Indented, serializerSettings);
                return str;
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return null;
        }

        /// <summary>
        /// Serialize an object to a file
        /// </summary>
        public static bool Serialize(object obj, string filepath)
        {
            try
            {
                string str = Serialize(obj);
                if (string.IsNullOrEmpty(str) == false)
                {
                    using (StreamWriter sw = new StreamWriter(filepath, false))
                    {
                        sw.Write(str);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }
            return false;
        }


        // <summary>
        // Unserialize an object from file
        // </summary>
        public static T Deserialize<T>(string filepath)
        {
            T ret = default(T);
            try
            {
                using (StreamReader sr = new StreamReader(filepath))
                {
                    string str = sr.ReadToEnd();
                    ret = JsonConvert.DeserializeObject<T>(str, serializerSettings);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOGNAME, ex.Message, ex);
            }

            return ret;
        }


    }
}
