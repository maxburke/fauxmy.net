using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace fxmy.net.test
{
    class TestAttribute : Attribute
    {
        public string TestCaseName { get; private set; }

        public TestAttribute(string testCaseName)
        {
            TestCaseName = testCaseName;
        }
    }

    public delegate bool TestDelegate();

    class Program
    {
        static void Main(string[] args)
        {
            Type mainType = typeof(fxmy.net.test.Program);
            Assembly assembly = mainType.Assembly;

            Type[] assemblyTypes = assembly.GetTypes();

            int numPassed = 0;
            int totalTests = 0;

            foreach (Type type in assemblyTypes)
            {
                foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.NonPublic))
                {
                    object[] attributes = methodInfo.GetCustomAttributes(typeof(TestAttribute), false);

                    foreach (object attribute in attributes)
                    {
                        TestAttribute testAttribute = (TestAttribute)attribute;
                        TestDelegate testDelegate = (TestDelegate)Delegate.CreateDelegate(typeof(TestDelegate), methodInfo);

                        ++totalTests;

                        try
                        {
                            bool success = testDelegate();
                            Console.WriteLine("{0} - {1}", success ? "PASSED" : "FAILED", testAttribute.TestCaseName);

                            numPassed += success ? 1 : 0;
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine("FAILED - {0} - Exception thrown:", testAttribute.TestCaseName);
                            Console.WriteLine("{0}", exception.ToString());
                        }
                    }
                }
            }

            Console.WriteLine("----------------");
            Console.WriteLine("{0} / {1} Passed", numPassed, totalTests);

            if (numPassed != totalTests)
                System.Diagnostics.Debugger.Break(); 
        }
    }
}
