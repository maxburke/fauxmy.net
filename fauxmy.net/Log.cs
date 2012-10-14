using System;
using System.IO;
using System.Data.Odbc;

namespace fxmy.net
{
    class Log
    {
        static Verbosity gVerbosity;
        static TextWriter gLogStream = Console.Error;

        public enum Verbosity
        {
            CRITICAL,
            ERROR,
            WARNING,
            INFO,
            ALL
        }

        public static void SetVerbosity(Verbosity verbosityLevel)
        {
            gVerbosity = verbosityLevel;
        }

        public static void SetLogFile(string logFileName)
        {
            gLogStream = new StreamWriter(logFileName);
        }

        public static void LogErrors(System.Data.Odbc.OdbcException exception)
        {
            OdbcErrorCollection errors = exception.Errors;

            for (int i = 0; i < errors.Count; ++i)
                WriteLine("[{0}] [{1}] {2}", errors[i].NativeError, errors[i].SQLState, errors[i].Message);
        }

        public static void WriteLine(string format, params object[] args)
        {
            string logString = string.Format(format, args);
            gLogStream.WriteLine("[fxmy] {0}", logString);
        }
    }
}
