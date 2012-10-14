using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace fxmy.net
{
    class Status
    {
        public int NativeError { get; private set; }
        public byte Header { get; private set; }
        public ushort StatusCode { get; private set; }
        public string SqlState { get; private set; }
        public string Message { get; private set; }

        public const int ERROR_NO_DATA = -100;
        public const int OK = 0;
        public const int ERROR_UNKNOWN_OBJECT = 208;
        public const int ERROR_DEFAULT = 208;
        public const byte ERROR = 0xFF;

        static Status[] mStatusArray = new Status[] {
            new Status(OK, 0, 0, "", "OK"),
            new Status(ERROR_NO_DATA, ERROR, 0, "", "NO DATA"),
            new Status(102, ERROR, 1149, "42000", "ERROR"),
            new Status(109, ERROR, 1136, "21S01", "ERROR"),
            new Status(110, ERROR, 1136, "21S01", "ERROR"),
            new Status(120, ERROR, 1136, "21S01", "ERROR"),
            new Status(121, ERROR, 1136, "21S01", "ERROR"),
            new Status(132, ERROR, 1308, "42000", "ERROR"),
            new Status(134, ERROR, 1331, "42000", "ERROR"),
            new Status(137, ERROR, 1327, "42000", "ERROR"),
            new Status(139, ERROR, 1067, "42000", "ERROR"),  // VERIFY 
            new Status(144, ERROR, 1056, "42000", "ERROR"),  // VERIFY 
            // 145 possibly requires special treatment 
            new Status(148, ERROR, 1149, "42000", "ERROR"),
            new Status(156, ERROR, 1149, "42000", "ERROR"),
            new Status(178, ERROR, 1313, "42000", "ERROR"),
            new Status(183, ERROR, 1425, "42000", "ERROR"),
            new Status(189, ERROR, 1318, "42000", "ERROR"),
            new Status(193, ERROR, 1071, "42000", "ERROR"),  // VERIFY 
            new Status(201, ERROR, 1107, "HY000", "ERROR"), 
            new Status(207, ERROR, 1054, "42S22", "ERROR"),
            new Status(208, ERROR, 1146, "42S02", "ERROR"),
            new Status(214, ERROR, 1108, "HY000", "ERROR"),
            new Status(216, ERROR, 1583, "42000", "ERROR"),  // VERIFY  
            new Status(5701, 0, 0, "", ""),                  // Changed database context. 
            new Status(5703, 0, 0, "", ""),                  // Changed language setting. 
        };

        Status(int nativeError, byte header, ushort statusCode, string sqlState, string message)
        {
            NativeError = nativeError;
            Header = header;
            StatusCode = statusCode;
            SqlState = sqlState;
            Message = message;
        }

        public static Status GetStatus(OdbcError error)
        {
            Status status = GetStatus(error.NativeError);
            return status;
        }

        public static Status GetStatus(int nativeError)
        {
            for (int i = 0; i < mStatusArray.Length; ++i)
            {
                if (mStatusArray[i].NativeError == nativeError)
                    return mStatusArray[i];
            }

            throw new ConnectionException(string.Format("Unknown error code {0}", nativeError));
        }
    }
}
