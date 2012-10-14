using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace fxmy.net
{
    class NetworkBufferReader
    {
        Stream mStream;

        public NetworkBufferReader(Stream stream)
        {
            mStream = stream;
        }

        byte ReadByte()
        {
            int b = mStream.ReadByte();
            if (b == -1)
                throw new IndexOutOfRangeException("End of stream reached!");

            return (byte)b;
        }

        public byte ReadUint8()
        {
            return ReadByte();
        }

        public ushort ReadUint16()
        {
            byte b0 = ReadByte();
            byte b1 = ReadByte();

            return (ushort)(((ushort)b0) | ((ushort)b1 << 8));
        }

        public uint ReadUint32()
        {
            byte b0 = ReadByte();
            byte b1 = ReadByte();
            byte b2 = ReadByte();
            byte b3 = ReadByte();

            return ((uint)b0) | ((uint)b1 << 8) | ((uint)b2 << 16) | ((uint)b3 << 24);
        }

        public ulong ReadLcb()
        {
            byte b = ReadByte();

            switch (b)
            {
                case 254:
                    {
                        byte b0 = ReadByte();
                        byte b1 = ReadByte();
                        byte b2 = ReadByte();
                        byte b3 = ReadByte();
                        byte b4 = ReadByte();
                        byte b5 = ReadByte();
                        byte b6 = ReadByte();
                        byte b7 = ReadByte();
                        return ((ulong)b0)
                            | ((ulong)b1 << 8)
                            | ((ulong)b2 << 16)
                            | ((ulong)b3 << 24)
                            | ((ulong)b4 << 32)
                            | ((ulong)b5 << 40)
                            | ((ulong)b6 << 48)
                            | ((ulong)b7 << 56);
                    }
                case 253:
                    {
                        byte b0 = ReadByte();
                        byte b1 = ReadByte();
                        byte b2 = ReadByte();

                        return ((ulong)b0)
                            | ((ulong)b1 << 8)
                            | ((ulong)b2 << 16);
                    }
                case 252:
                    {
                        byte b0 = ReadByte();
                        byte b1 = ReadByte();

                        return ((ulong)b0)
                            | ((ulong)b1 << 8);
                    }
                case 251:
                    /*
                     * 251 signifies a NULL column value and will never be seen here.
                     */
                    throw new ConnectionException("Error: Received a NULL column value in a place that should never see a NULL column value.");
                default:
                    return (ulong)ReadByte();
            }
        }

        public string ReadLcs()
        {
            int length = (int)ReadLcb();
            byte[] bytes = new byte[length];
            mStream.Read(bytes, 0, length);

            return UTF8Encoding.UTF8.GetString(bytes);
        }

        public string ReadString()
        {
            List<byte> byteList = new List<byte>();

            for (; ; )
            {
                int b = mStream.ReadByte();
                if (b == 0 || b == -1)
                    break;

                byteList.Add((byte)b);
            }

            return UTF8Encoding.UTF8.GetString(byteList.ToArray());
        }

        public void SkipLcs()
        {
            int length = (int)ReadLcb();
            mStream.Seek(length, SeekOrigin.Current);
        }

        public void SkipString()
        {
            for (; ; )
            {
                int b = mStream.ReadByte();
                if (b == 0 || b == -1)
                    return;
            }
        }

        public void SkipBytes(int bytes)
        {
            mStream.Seek(bytes, SeekOrigin.Current);
        }
    }
}
