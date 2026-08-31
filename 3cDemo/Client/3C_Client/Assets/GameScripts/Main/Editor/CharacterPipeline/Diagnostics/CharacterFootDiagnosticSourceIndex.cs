using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootDiagnosticSourceRange
    {
        public int frame;
        public string side;
        public long offset;
        public int length;
        public string sha256;
    }

    internal sealed class CharacterFootDiagnosticSourceIndex
    {
        public string family;
        public string file;
        public long bytes;
        public string[] columns;
        public List<CharacterFootDiagnosticSourceRange> ranges = new List<CharacterFootDiagnosticSourceRange>();
    }

    internal sealed class CharacterFootDiagnosticSourceReader : IDisposable
    {
        static readonly UTF8Encoding s_Utf8 = new UTF8Encoding(false, true);
        readonly FileStream m_Stream;
        readonly byte[] m_Buffer = new byte[65536];
        readonly CharacterFootDiagnosticSourceIndex m_Index;
        int m_Start;
        int m_End;
        long m_Position;
        long m_LineOffset;
        byte[] m_LineBytes;
        CharacterFootDiagnosticSourceRange m_Range;
        IncrementalHash m_RangeHash;

        internal CharacterFootDiagnosticSourceReader(string path, string family)
        {
            m_Stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                65536, FileOptions.SequentialScan);
            m_Index = new CharacterFootDiagnosticSourceIndex
            {
                family = family,
                file = Path.GetFullPath(path),
                bytes = m_Stream.Length
            };
        }

        internal string ReadLine()
        {
            m_LineOffset = m_Position;
            using var line = new MemoryStream();
            while (true)
            {
                if (m_Start == m_End)
                {
                    m_End = m_Stream.Read(m_Buffer, 0, m_Buffer.Length);
                    m_Start = 0;
                    if (m_End == 0)
                        break;
                }
                int end = Array.IndexOf(m_Buffer, (byte)'\n', m_Start, m_End - m_Start);
                int count = end >= 0 ? end - m_Start + 1 : m_End - m_Start;
                line.Write(m_Buffer, m_Start, count);
                m_Start += count;
                m_Position += count;
                if (end >= 0)
                    break;
            }
            if (line.Length == 0)
                return null;
            m_LineBytes = line.ToArray();
            int length = m_LineBytes.Length;
            if (length > 0 && m_LineBytes[length - 1] == '\n') length--;
            if (length > 0 && m_LineBytes[length - 1] == '\r') length--;
            int start = m_LineOffset == 0 && length >= 3 && m_LineBytes[0] == 0xef &&
                m_LineBytes[1] == 0xbb && m_LineBytes[2] == 0xbf ? 3 : 0;
            return s_Utf8.GetString(m_LineBytes, start, length - start);
        }

        internal void SetColumns(string[] columns) => m_Index.columns = columns;

        internal void Include(int frame, string side)
        {
            if (m_Range == null || m_Range.frame != frame || m_Range.side != side ||
                m_Range.offset + m_Range.length != m_LineOffset)
            {
                EndRange();
                m_Range = new CharacterFootDiagnosticSourceRange
                {
                    frame = frame,
                    side = side,
                    offset = m_LineOffset
                };
                m_RangeHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            }
            m_Range.length = checked(m_Range.length + m_LineBytes.Length);
            m_RangeHash.AppendData(m_LineBytes);
        }

        internal CharacterFootDiagnosticSourceIndex Complete()
        {
            EndRange();
            return m_Index;
        }

        void EndRange()
        {
            if (m_Range == null)
                return;
            m_Range.sha256 = BitConverter.ToString(m_RangeHash.GetHashAndReset())
                .Replace("-", string.Empty).ToLowerInvariant();
            m_Index.ranges.Add(m_Range);
            m_RangeHash.Dispose();
            m_RangeHash = null;
            m_Range = null;
        }

        public void Dispose()
        {
            m_RangeHash?.Dispose();
            m_Stream.Dispose();
        }
    }
}
