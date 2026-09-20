using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace ThirdPersonSimulation
{
    public sealed class CanonicalWriter : IDisposable
    {
        readonly MemoryStream m_Stream;
        readonly bool m_OwnsStream;

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
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            m_Stream.Write(buffer);
        }

        public void WriteUInt32(uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            m_Stream.Write(buffer);
        }

        public void WriteUInt16(ushort value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            m_Stream.Write(buffer);
        }

        public void WriteInt64(long value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            m_Stream.Write(buffer);
        }

        public void WriteUInt64(ulong value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            m_Stream.Write(buffer);
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
            m_Stream.Write(value);
        }

        public long BeginLengthPrefixedBlock()
        {
            long position = m_Stream.Position;
            WriteInt32(0);
            return position;
        }

        public void EndLengthPrefixedBlock(long prefixPosition)
        {
            long end = m_Stream.Position;
            if (prefixPosition < 0 || prefixPosition > end - sizeof(int))
                throw new ArgumentOutOfRangeException(nameof(prefixPosition));
            int length = checked((int)(end - prefixPosition - sizeof(int)));
            m_Stream.Position = prefixPosition;
            try
            {
                WriteInt32(length);
            }
            finally
            {
                m_Stream.Position = end;
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

        public bool ContentEquals(ReadOnlySpan<byte> value)
        {
            if (m_Stream.Length != value.Length)
                return false;
            long position = m_Stream.Position;
            try
            {
                m_Stream.Position = 0;
                Span<byte> buffer = stackalloc byte[256];
                int offset = 0;
                while (offset < value.Length)
                {
                    int count = Math.Min(buffer.Length, value.Length - offset);
                    int read = m_Stream.Read(buffer.Slice(0, count));
                    if (read == 0 || !buffer.Slice(0, read).SequenceEqual(value.Slice(offset, read)))
                        return false;
                    offset += read;
                }
                return true;
            }
            finally
            {
                m_Stream.Position = position;
            }
        }

        public StableHash ComputeHash()
        {
            if (m_Stream.TryGetBuffer(out ArraySegment<byte> buffer))
            {
                return SimulationCanonicalPayloadHash.Compute(new ArraySegment<byte>(
                    buffer.Array,
                    buffer.Offset,
                    checked((int)m_Stream.Length)));
            }
            return SimulationCanonicalPayloadHash.Compute(ToArray());
        }

        public void Dispose()
        {
            if (m_OwnsStream)
                m_Stream.Dispose();
        }

    }

    public sealed class CanonicalReader
    {
        readonly byte[] m_Bytes;
        readonly int m_End;
        int m_Offset;

        public CanonicalReader(byte[] bytes)
            : this(new ArraySegment<byte>(bytes ?? throw new ArgumentNullException(nameof(bytes))))
        {
        }

        public CanonicalReader(ArraySegment<byte> bytes)
        {
            m_Bytes = bytes.Array ?? throw new ArgumentNullException(nameof(bytes));
            m_Offset = bytes.Offset;
            m_End = bytes.Offset + bytes.Count;
        }

        public int Remaining => m_End - m_Offset;
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
        public ArraySegment<byte> ReadBytesSegment()
        {
            int length = ReadLength();
            Require(length);
            var value = new ArraySegment<byte>(m_Bytes, m_Offset, length);
            m_Offset += length;
            return value;
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
