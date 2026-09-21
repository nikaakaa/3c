using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace ThirdPersonSimulation
{
    public sealed class CanonicalWriter : IDisposable
    {
        const int InitialCapacity = 256;

        byte[] m_Buffer;
        int m_Position;
        int m_Length;
        readonly bool m_Bounded;

        public CanonicalWriter()
        {
            m_Buffer = new byte[InitialCapacity];
        }

        public CanonicalWriter(byte[] buffer)
        {
            m_Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            m_Bounded = true;
        }

        public long Length => m_Length;

        public long Position
        {
            get => m_Position;
            set
            {
                if (value < 0 || value > m_Length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                m_Position = checked((int)value);
            }
        }

        public void Reset()
        {
            m_Position = 0;
            m_Length = 0;
        }

        public void WriteByte(byte value)
        {
            EnsureCapacity(1);
            m_Buffer[m_Position++] = value;
            TrackLength();
        }

        public void WriteBoolean(bool value) => WriteByte(value ? (byte)1 : (byte)0);

        public void WriteInt32(int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            WriteRaw(buffer);
        }

        public void WriteUInt32(uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            WriteRaw(buffer);
        }

        public void WriteUInt16(ushort value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            WriteRaw(buffer);
        }

        public void WriteInt64(long value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            WriteRaw(buffer);
        }

        public void WriteUInt64(ulong value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            WriteRaw(buffer);
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
            const int characterCapacity = 256;
            Span<byte> buffer = stackalloc byte[characterCapacity * 3];
            int offset = 0;
            while (offset < value.Length)
            {
                int count = Math.Min(characterCapacity, value.Length - offset);
                if (offset + count < value.Length &&
                    char.IsHighSurrogate(value[offset + count - 1]) &&
                    char.IsLowSurrogate(value[offset + count]))
                    count--;
                int written = Encoding.UTF8.GetBytes(value.AsSpan(offset, count), buffer);
                WriteRaw(buffer.Slice(0, written));
                offset += count;
            }
        }

        public void WriteBytes(byte[] value)
        {
            byte[] bytes = value ?? Array.Empty<byte>();
            WriteInt32(bytes.Length);
            WriteRaw(bytes);
        }

        public void WriteEventId(EventId value)
        {
            WriteInt32(value.IsValid ? 64 : 0);
            if (!value.IsValid)
                return;
            Span<char> characters = stackalloc char[64];
            Span<byte> bytes = stackalloc byte[64];
            value.Format(characters);
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)characters[i];
            WriteRaw(bytes);
        }

        public void WriteBytes(ReadOnlySpan<byte> value)
        {
            WriteInt32(value.Length);
            WriteRaw(value);
        }

        public long BeginLengthPrefixedBlock()
        {
            long position = m_Position;
            WriteInt32(0);
            return position;
        }

        public void EndLengthPrefixedBlock(long prefixPosition)
        {
            long end = m_Position;
            if (prefixPosition < 0 || prefixPosition > end - sizeof(int))
                throw new ArgumentOutOfRangeException(nameof(prefixPosition));
            int length = checked((int)(end - prefixPosition - sizeof(int)));
            Position = prefixPosition;
            WriteInt32(length);
            Position = end;
        }

        public void WriteRawBytes(byte[] value, int offset, int count)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (offset < 0 || count < 0 || offset > value.Length - count)
                throw new ArgumentOutOfRangeException();
            WriteRaw(value.AsSpan(offset, count));
        }

        public void WriteRawBytes(ReadOnlySpan<byte> value) => WriteRaw(value);

        public ReadOnlySpan<byte> WrittenSpan => m_Buffer.AsSpan(0, m_Length);

        public byte[] ToArray()
        {
            var result = new byte[m_Length];
            Buffer.BlockCopy(m_Buffer, 0, result, 0, m_Length);
            return result;
        }

        public bool ContentEquals(ReadOnlySpan<byte> value)
        {
            if (m_Length != value.Length)
                return false;
            return m_Buffer.AsSpan(0, m_Length).SequenceEqual(value);
        }

        public StableHash ComputeHash()
        {
            return SimulationCanonicalPayloadHash.Compute(new ArraySegment<byte>(m_Buffer, 0, m_Length));
        }

        public void Dispose()
        {
        }

        void WriteRaw(ReadOnlySpan<byte> value)
        {
            if (value.Length == 0)
                return;
            EnsureCapacity(value.Length);
            value.CopyTo(m_Buffer.AsSpan(m_Position));
            m_Position += value.Length;
            TrackLength();
        }

        void EnsureCapacity(int count)
        {
            if (checked(m_Position + count) <= m_Buffer.Length)
                return;
            if (m_Bounded)
                throw new InvalidOperationException("Bounded canonical writer capacity is exhausted.");
            int next = m_Buffer.Length;
            while (next < m_Position + count)
                next = checked(next * 2);
            Array.Resize(ref m_Buffer, next);
        }

        void TrackLength()
        {
            if (m_Position > m_Length)
                m_Length = m_Position;
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

        public ArraySegment<byte> ReadUtf8Segment()
        {
            int length = ReadLength();
            Require(length);
            var value = new ArraySegment<byte>(m_Bytes, m_Offset, length);
            m_Offset += length;
            return value;
        }

        public EventId ReadEventId()
        {
            int length = ReadLength();
            if (length == 0)
                return default;
            if (length != 64)
                throw new InvalidDataException("Canonical event identity must contain 64 hexadecimal characters.");
            Require(length);
            Span<char> characters = stackalloc char[64];
            for (int i = 0; i < characters.Length; i++)
                characters[i] = (char)m_Bytes[m_Offset + i];
            EventId value = EventId.Parse(characters);
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
            return ReadRawBytesSegment(ReadLength());
        }
        public ArraySegment<byte> ReadRawBytesSegment(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
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
            if (length == 0)
                return Array.Empty<byte>();
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
