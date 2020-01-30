using System;
using System.Collections.Generic;
using System.Text;
using Classes;

namespace Database
{
    public class DbFile
    {
        public enum State
        {
            ERROR = -1, //processing error
            QUEUED = 0, //queued for upload
            MISSING = 1, //file not found
            UPLOADED = 2, //file uploaded
            SYNC = 3 //file sync to directory
        }
        public string Id; //filepath.tolower
        public string Name;
        public DateTime DateLastWriteTime;
        public long Size;
        public string Checksum;
        public string DownloadLink;
        public State Status; //Sync to directory
        public string Tag;
        public string Category;
        public string Lang;
        public Crypto.EncryptionMode EncryptionMode = Crypto.EncryptionMode.NONE;
    }
}
