using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

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

        static ConnectionState HandleCommandPacket(Connection connection)
        {
            using (Command command = connection.ReceiveCommandPacket())
            {
                return command.Execute(connection);
            }
        }

        static void WorkerThread(object parameter)
        {
            using (Connection connection = (Connection)parameter)
            {
                connection.InitializeConnection();

                for (; ; )
                {
                    ConnectionState connectionState;
                    string errorMessage = "";

                    if (Debugger.IsAttached)
                    {
                        connectionState = HandleCommandPacket(connection);
                    }
                    else
                    {
                        try
                        {
                            connectionState = HandleCommandPacket(connection);
                        }
                        catch (Exception exception)
                        {
                            connectionState = ConnectionState.ERROR;
                            errorMessage = exception.ToString();
                        }
                    }

                    switch (connectionState)
                    {
                        case ConnectionState.CONTINUE:
                            break;
                        case ConnectionState.ERROR:
                            Log.WriteLine(errorMessage);
                            break;
                        case ConnectionState.EXIT:
                            return;
                    }

                    connection.SendResult();
                }
            }
        }

        static void Main(string[] args)
        {
            const string CONNECTION_STRING = "Driver={SQL Server Native Client 11.0};Server=.\\SQLEXPRESS;UID=max;PWD=W0zixege";
            Log.SetVerbosity(Log.Verbosity.ALL);
            Socket socket = OpenSocket(FXMY_DEFAULT_PORT);

            for (; ; )
            {
                Socket newSocket = socket.Accept();
                Connection connection = new Connection(newSocket, CONNECTION_STRING);

                Thread thread = new Thread(new ParameterizedThreadStart(WorkerThread));
                thread.Start(connection);
            }
        }
    }
}
