using System;
using System.Collections.Generic;
using System.Text;


namespace Sync
{
    //All keys have to be in lower
    class SyncItem
    {
        public string Key;
        public string Name;
        public long Size;
        public string Checksum;
        public string DownloadLink;
        public string Tag;
        public string Category;
        public string Lang;
        public Classes.Crypto.EncryptionMode EncryptionMode;
    }
}
