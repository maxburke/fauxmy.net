using System;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Data.Odbc;

namespace fxmy.net
{
    class ConnectionException : Exception
    {
        public ConnectionException(string message)
            : base(message)
        {
        }
    }

    enum ConnectionState
    {
        CONTINUE,
        EXIT,
        ERROR
    }

    class Connection : IDisposable
    {
        Socket mSocket;
        int mPacketNumber;
        uint mClientFlags;
        uint mMaxPacketSize;
        byte mCharSet;
        string mConnectionString;
        ulong mAffectedRows;
        ulong mInsertId;

        public Status Status;
        public bool MultiStatements { get; set; }
        public OdbcConnection DatabaseConnection { get; private set; }

        static byte[] mHandshakePacket = new byte[] {
            0xa,                            /* protocol version */
            (byte)'5', (byte)'.', (byte)'0', (byte)'.', (byte)'0', 0,
                                            /* server version (null terminated) */
            1, 0, 0, 0,                     /* thread id */
            0, 0, 0, 0, 0, 0, 0, 0,         /* scramble buf */
            0,                              /* filler, always 0 */
            0, 0x82,                        /* server capabilities. the 2 (512, in
                                               little endian) means that this
                                               server supports the 4.1 protocol
                                               instead of the 4.0 protocol. */
                                            /* The 0x80 indicates that we support 4.1
                                               protocol authentication, enforcing
                                               that the password is sent to us as
                                               a 20-byte re-jiggered SHA1. */
            0,                              /* server language */
            0, 0,                           /* server status */
            0, 0,                           /* server capabilities, upper two bytes */
            8,                              /* length of the scramble */
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,   /* filler, always 0 */
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0,               /* plugin data, at least 12 bytes. */
            0                               /* second part of scramble */
        };

        enum ClientFlags
        {
            CLIENT_LONG_PASSWORD = 1 << 0,
            CLIENT_FOUND_ROWS = 1 << 1,
            CLIENT_LONG_FLAG = 1 << 2,
            CLIENT_CONNECT_WITH_DB = 1 << 3,
            CLIENT_NO_SCHEMA = 1 << 4,
            CLIENT_COMPRESS = 1 << 5,
            CLIENT_ODBC = 1 << 6,
            CLIENT_LOCAL_FILES = 1 << 7,
            CLIENT_IGNORE_SPACE = 1 << 8,
            CLIENT_PROTOCOL_41 = 1 << 9,
            CLIENT_INTERACTIVE = 1 << 10,
            CLIENT_SSL = 1 << 11,
            CLIENT_IGNORE_SIGPIPE = 1 << 12,
            CLIENT_TRANSACTIONS = 1 << 13,
            CLIENT_RESERVED = 1 << 14,
            CLIENT_SECURE_CONNECTION = 1 << 15,
            CLIENT_MULTI_STATEMENTS = 1 << 16,
            CLIENT_MULTI_RESULTS = 1 << 17,
        };

        public Connection(Socket socket, string connectionString)
        {
            mSocket = socket;
            mConnectionString = connectionString;
        }

        void SendHandshake()
        {
            Send(mHandshakePacket);
        }

        void ReceiveAuthPacket()
        {
            const int NUM_PADDING_BYTES = 23;

            byte[] authPacket = Receive();
            MemoryStream memoryStream = new MemoryStream(authPacket);
            NetworkBufferReader networkReader = new NetworkBufferReader(memoryStream);

            mClientFlags = networkReader.ReadUint32();
            mMaxPacketSize = networkReader.ReadUint32();
            mCharSet = networkReader.ReadUint8();

            networkReader.SkipBytes(NUM_PADDING_BYTES);

            networkReader.SkipString();
            networkReader.SkipLcs();

            if ((mClientFlags & (uint)ClientFlags.CLIENT_CONNECT_WITH_DB) != 0)
            {
                string databaseName = networkReader.ReadString();
                Connect(databaseName);
            }
        }

        void Send(byte[] buffer)
        {
            int packetNumber = mPacketNumber++;
            int numPackets = 0;
            int size = buffer.Length;

            for (int i = 0; i < size; i += (1 << 24) - 1)
            {
                ++numPackets;
            }

            if (numPackets > 255)
            {
                throw new ConnectionException("Result set too large.");
            }

            byte[] packetHeader = new byte[4];
            int offset = 0;

            for (int i = 0; i < numPackets; ++i)
            {
                int packetSize = Math.Min((1 << 24) - 1, size);
                size -= packetSize;
                packetHeader[0] = (byte)(packetSize & 0xFF);
                packetHeader[1] = (byte)((packetSize >> 8) & 0xFF);
                packetHeader[2] = (byte)((packetSize >> 16) & 0xFF);
                packetHeader[3] = (byte)packetNumber;

                mSocket.Send(packetHeader);
                mSocket.Send(buffer, offset, packetSize, SocketFlags.None);
                offset += packetSize;
            }
        }

        byte[] Receive()
        {
            byte[] packetHeader = new byte[4];
            int packetSize;
            int packetNumber;

            mSocket.Receive(packetHeader);
            packetSize = ((int)packetHeader[0])
                | ((int)(packetHeader[1]) << 8)
                | ((int)(packetHeader[2]) << 16);
            packetNumber = (int)packetHeader[3];
            mPacketNumber = packetNumber + 1;

            byte[] receiveBuffer = new byte[packetSize];
            mSocket.Receive(receiveBuffer);

            return receiveBuffer;
        }

        public void SendResult()
        {
            NetworkBufferWriter writer = new NetworkBufferWriter();

            if (Status.Header == Status.OK)
            {
                writer.WriteUint8(Status.Header);
                writer.WriteLcb(mAffectedRows);
                writer.WriteLcb(mInsertId);
                writer.WriteUint16(0);
                writer.WriteUint16(0);
                writer.WriteString(Status.Message);
            }
            else if (Status.Header == Status.ERROR)
            {
                writer.WriteUint8(Status.Header);
                writer.WriteUint16(Status.StatusCode);
                writer.WriteString("#");
                writer.WriteString(Status.SqlState);
                writer.WriteString(Status.Message);
            }
            else
            {
                // Need to return columns here.
                Debugger.Break();
            }

            Send(writer.GetBuffer());
            Status = null;
        }

        public Command ReceiveCommandPacket()
        {
            byte[] commandPacketData = Receive();
            return CommandPacket.CreateCommand(commandPacketData);
        }

        public void Dispose()
        {
            DatabaseConnection.Close();
            DatabaseConnection.Dispose();
            mSocket.Close();
        }

        public void Connect(string database)
        {
            DatabaseConnection = new OdbcConnection(mConnectionString);
            DatabaseConnection.Open();
            DatabaseConnection.ChangeDatabase(database);
        }

        public void InitializeConnection()
        {
            SendHandshake();
            ReceiveAuthPacket();

            Status = Status.GetStatus(Status.OK);
            SendResult();
        }
    }
}
