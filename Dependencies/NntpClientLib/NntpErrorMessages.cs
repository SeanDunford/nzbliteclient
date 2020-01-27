using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dependencies.NntpClientLib
{
    public class NntpErrorMessages
    {
        public const string ERROR_1 = "Failed to setup session after connection to NNTP server.";
        public const string ERROR_2 = "GROUP command failed.";
        public const string ERROR_3 = "Incorrect direction command, it must be LAST or NEXT.";
        public const string ERROR_4 = "Failed to move the selected article cursor in the current group.";
        public const string ERROR_5 = "Failed to get statistics for current group.";
        public const string ERROR_6 = "Can not post to group.";
        public const string ERROR_7 = "Article failed to post to group.";
        public const string ERROR_8 = "Slave command failed.";
        public const string ERROR_9 = "Message Id must begin with < and end with >.";
        public const string ERROR_10 = "Message Id must begin with < and end with > and contain a message.";
        public const string ERROR_11 = "Client is not connected to server.";
        public const string ERROR_12 = "NNTP command failed, unexpected response code.";
        public const string ERROR_13 = "Response input string should contain at least 3 fields, possibly 4.";

        public const string ERROR_30 = "Required password was not accepted.";
        public const string ERROR_31 = "Unable to authenticate.";

        public const string ERROR_41 = "No previous response was received.";
        public const string ERROR_42 = "Response code is missing.";
        public const string ERROR_43 = "The minimum size of the buffer must be positive.";
        public const string ERROR_44 = "Cannot read stream.";
        public const string ERROR_45 = "Can not read from underlying stream.";
        public const string ERROR_46 = "Unrecognized date format requested, 'R' is the only accepted format argument.";
        public const string ERROR_47 = "String is not a valid date time.";
        public const string ERROR_48 = "Response input string should contain 2 or 3 fields separated by spaces.";

    }
}
