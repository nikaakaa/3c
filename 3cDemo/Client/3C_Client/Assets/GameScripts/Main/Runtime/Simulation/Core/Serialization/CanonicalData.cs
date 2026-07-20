using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ThirdPersonSimulation
{
    public sealed class CanonicalWriter : IDisposable
    {
        readonly MemoryStream m_Stream;
        readonly bool m_OwnsStream;
        readonly byte[] m_PrimitiveBuffer = new byte[8];

        public CanonicalWriter()
        {
            m_Stream = new MemoryStream();
            m_OwnsStream = true;
        }

        public CanonicalWriter(MemoryStream stream)
        {
            m_Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public long Length => m_Stream.Length;
        public void WriteByte(byte value) => m_Stream.WriteByte(value);
        public void WriteBoolean(bool value) => WriteByte(value ? (byte)1 : (byte)0);

        public void WriteInt32(int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(m_PrimitiveBuffer.AsSpan(0, 4), value);
            m_Stream.Write(m_PrimitiveBuffer, 0, 4);
        }

        public void WriteUInt32(uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(m_PrimitiveBuffer.AsSpan(0, 4), value);
            m_Stream.Write(m_PrimitiveBuffer, 0, 4);
        }

        public void WriteUInt16(ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(m_PrimitiveBuffer.AsSpan(0, 2), value);
            m_Stream.Write(m_PrimitiveBuffer, 0, 2);
        }

        public void WriteInt64(long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(m_PrimitiveBuffer, value);
            m_Stream.Write(m_PrimitiveBuffer, 0, 8);
        }

        public void WriteUInt64(ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(m_PrimitiveBuffer, value);
            m_Stream.Write(m_PrimitiveBuffer, 0, 8);
        }

        public void WriteDouble(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            WriteInt64(BitConverter.DoubleToInt64Bits(value == 0d ? 0d : value));
        }

        public void WriteString(string value)
        {
            value ??= string.Empty;
            int byteCount = Encoding.UTF8.GetByteCount(value);
            WriteInt32(byteCount);
            if (byteCount == 0)
                return;
            byte[] rented = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                int written = Encoding.UTF8.GetBytes(value, 0, value.Length, rented, 0);
                m_Stream.Write(rented, 0, written);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        public void WriteBytes(byte[] value)
        {
            byte[] bytes = value ?? Array.Empty<byte>();
            WriteInt32(bytes.Length);
            m_Stream.Write(bytes, 0, bytes.Length);
        }

        public void WriteBytes(ReadOnlySpan<byte> value)
        {
            WriteInt32(value.Length);
            if (value.Length == 0)
                return;
            byte[] rented = ArrayPool<byte>.Shared.Rent(value.Length);
            try
            {
                value.CopyTo(rented);
                m_Stream.Write(rented, 0, value.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        public void WriteRawBytes(byte[] value, int offset, int count)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (offset < 0 || count < 0 || offset > value.Length - count)
                throw new ArgumentOutOfRangeException();
            m_Stream.Write(value, offset, count);
        }

        public byte[] ToArray() => m_Stream.ToArray();
        public StableHash ComputeHash()
        {
            using SHA256 sha = SHA256.Create();
            if (m_Stream.TryGetBuffer(out ArraySegment<byte> buffer))
            {
                return new StableHash(ToHex(sha.ComputeHash(
                    buffer.Array,
                    buffer.Offset,
                    checked((int)m_Stream.Length))));
            }
            return new StableHash(ToHex(sha.ComputeHash(ToArray())));
        }

        public void Dispose()
        {
            if (m_OwnsStream)
                m_Stream.Dispose();
        }

        static string ToHex(byte[] bytes)
        {
            var chars = new char[bytes.Length * 2];
            const string hex = "0123456789abcdef";
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = hex[bytes[i] >> 4];
                chars[i * 2 + 1] = hex[bytes[i] & 15];
            }
            return new string(chars);
        }
    }

    public sealed class CanonicalReader
    {
        readonly byte[] m_Bytes;
        int m_Offset;

        public CanonicalReader(byte[] bytes)
        {
            m_Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        }

        public int Remaining => m_Bytes.Length - m_Offset;
        public byte ReadByte()
        {
            Require(1);
            return m_Bytes[m_Offset++];
        }
        public bool ReadBoolean()
        {
            byte value = ReadByte();
            if (value > 1)
                throw new InvalidDataException("Canonical boolean is invalid.");
            return value == 1;
        }
        public int ReadInt32() { Require(4); int value = BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(m_Bytes, m_Offset, 4)); m_Offset += 4; return value; }
        public ushort ReadUInt16() { Require(2); ushort value = BinaryPrimitives.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(m_Bytes, m_Offset, 2)); m_Offset += 2; return value; }
        public uint ReadUInt32() { Require(4); uint value = BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(m_Bytes, m_Offset, 4)); m_Offset += 4; return value; }
        public long ReadInt64() { Require(8); long value = BinaryPrimitives.ReadInt64LittleEndian(new ReadOnlySpan<byte>(m_Bytes, m_Offset, 8)); m_Offset += 8; return value; }
        public ulong ReadUInt64() { Require(8); ulong value = BinaryPrimitives.ReadUInt64LittleEndian(new ReadOnlySpan<byte>(m_Bytes, m_Offset, 8)); m_Offset += 8; return value; }
        public double ReadDouble()
        {
            double value = BitConverter.Int64BitsToDouble(ReadInt64());
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidDataException("Canonical double is not finite.");
            return value == 0d ? 0d : value;
        }
        public string ReadString()
        {
            int length = ReadLength();
            Require(length);
            string value = Encoding.UTF8.GetString(m_Bytes, m_Offset, length);
            m_Offset += length;
            return value;
        }
        public byte[] ReadBytes()
        {
            int length = ReadLength();
            return ReadRawBytes(length);
        }
        public byte[] ReadRawBytes(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            Require(length);
            var value = new byte[length];
            Buffer.BlockCopy(m_Bytes, m_Offset, value, 0, length);
            m_Offset += length;
            return value;
        }
        public void RequireComplete()
        {
            if (Remaining != 0)
                throw new InvalidDataException($"Canonical payload has {Remaining} trailing bytes.");
        }

        int ReadLength()
        {
            int value = ReadInt32();
            if (value < 0)
                throw new InvalidDataException("Canonical length is negative.");
            return value;
        }

        void Require(int count)
        {
            if (count < 0 || count > Remaining)
                throw new EndOfStreamException();
        }
    }

}
