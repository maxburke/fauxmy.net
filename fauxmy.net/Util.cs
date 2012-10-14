using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fxmy.net
{
    class ShitBrokeException : Exception
    {
        public ShitBrokeException(string message)
            : base(message)
        {
        }
    }

    class Util
    {
        public static void Verify(bool condition)
        {
            if (!condition)
                throw new ShitBrokeException("Verification failed!");
        }
    }
}
