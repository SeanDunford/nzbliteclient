using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Classes;

namespace Settings
{
    public class SettingFolder
    {
        private const string FILE_NAMING_RULE_PARENTNAME = "PARENTNAME";
        private const string FILE_NAMING_RULE_FILENAME = "FILENAME";
        private const string FILE_NAMING_RULE_FULLPATH = "FULLPATH";

        public enum FileNamingRuleEnum
        {
            ERROR = -1,
            FILENAME = 0,
            PARENTNAME = 1,
            FULLPATH = 2
        }

        public string Path;
        public string FileNamingRule;
        public string Tag;
        public string Category;
        public string Lang; //2Letters language
        public Utilities.EncryptionMode EncryptionMode = Utilities.EncryptionMode.NONE;

        public FileNamingRuleEnum GetFileNamingRule()
        {
            if (string.IsNullOrEmpty(FileNamingRule))
            {
                return FileNamingRuleEnum.ERROR;
            }
            switch (FileNamingRule.ToUpper())
            {
                case FILE_NAMING_RULE_PARENTNAME: return FileNamingRuleEnum.PARENTNAME;
                case FILE_NAMING_RULE_FILENAME: return FileNamingRuleEnum.FILENAME;
                case FILE_NAMING_RULE_FULLPATH: return FileNamingRuleEnum.FULLPATH;
            }
            return FileNamingRuleEnum.ERROR;
        }
    }
}
