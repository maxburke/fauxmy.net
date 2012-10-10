using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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
    }
}
