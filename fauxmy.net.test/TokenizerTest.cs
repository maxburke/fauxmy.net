using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using fxmy.net;

namespace fxmy.net.test
{
    class TokenizerTest
    {
        [TestAttribute("TokenizerTest")]
        static bool RunTest()
        {
            using (TextReader reader = new StreamReader(fxmy.net.Log.QUERY_LOG_FILE))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    Query query = new Query(line);

                    System.Diagnostics.Debugger.Break();
                }

                return true;
            }
        }
    }
}
