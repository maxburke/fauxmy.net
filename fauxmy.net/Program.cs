using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace fxmy.net
{
    class Fauxmy
    {
        const int FXMY_DEFAULT_PORT = 3306;
        const int FXMY_DEFAULT_LISTEN_BACKLOG = 50;

        static Socket OpenSocket(int port)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress address = IPAddress.Any;
            IPEndPoint endPoint = new IPEndPoint(address, port);
            socket.Bind(endPoint);
            socket.Listen(FXMY_DEFAULT_LISTEN_BACKLOG);

            return socket;
        }

        static void Main(string[] args)
        {
            Log.SetVerbosity(Log.Verbosity.ALL);
            Socket socket = OpenSocket(FXMY_DEFAULT_PORT);

            for (; ; )
            {
                Socket newConnection = socket.Accept();

            }
        }
    }
}
