using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace fxmy.net
{
    class NetworkBufferWriter
    {
        List<byte> mByteBuffer = new List<byte>();

        const byte TWO_BYTE_HEADER = 252;
        const byte THREE_BYTE_HEADER = 253;
        const byte EIGHT_BYTE_HEADER = 254;

        public void WriteUint8(byte value)
        {
            mByteBuffer.Add(value);
        }

        public void WriteUint16(ushort value)
        {
            mByteBuffer.Add((byte)(value & 0xFF));
            mByteBuffer.Add((byte)((value >> 8) & 0xFF));
        }

        public void WriteUint32(uint value)
        {
            mByteBuffer.Add((byte)(value & 0xFF));
            mByteBuffer.Add((byte)((value >> 8) & 0xFF));
            mByteBuffer.Add((byte)((value >> 16) & 0xFF));
            mByteBuffer.Add((byte)((value >> 24) & 0xFF));
        }

        public void WriteLcb(ulong value)
        {
            if (value <= 250)
            {
                mByteBuffer.Add((byte)value);
            }
            else if (value <= ((1 << 16) - 1))
            {
                mByteBuffer.Add(TWO_BYTE_HEADER);
                mByteBuffer.Add((byte)(value & 0xFF));
                mByteBuffer.Add((byte)((value >> 8) & 0xFF));
            }
            else if (value <= ((1 << 24) - 1))
            {
                mByteBuffer.Add(THREE_BYTE_HEADER);
                mByteBuffer.Add((byte)(value & 0xFF));
                mByteBuffer.Add((byte)((value >> 8) & 0xFF));
                mByteBuffer.Add((byte)((value >> 16) & 0xFF));
            }
            else
            {
                mByteBuffer.Add(EIGHT_BYTE_HEADER);
                mByteBuffer.Add((byte)(value & 0xFF));
                mByteBuffer.Add((byte)((value >> 8) & 0xFF));
                mByteBuffer.Add((byte)((value >> 16) & 0xFF));
                mByteBuffer.Add((byte)((value >> 24) & 0xFF));
                mByteBuffer.Add((byte)((value >> 32) & 0xFF));
                mByteBuffer.Add((byte)((value >> 40) & 0xFF));
                mByteBuffer.Add((byte)((value >> 48) & 0xFF));
                mByteBuffer.Add((byte)((value >> 56) & 0xFF));
            }
        }

        public void WriteTerminatedString(string value)
        {
            WriteString(value);
            mByteBuffer.Add(0);
        }

        public void WriteString(string value)
        {
            byte[] stringBytes = UTF8Encoding.UTF8.GetBytes(value);
            mByteBuffer.AddRange(stringBytes);
        }

        public byte[] GetBuffer()
        {
            return mByteBuffer.ToArray();
        }
    }
}
