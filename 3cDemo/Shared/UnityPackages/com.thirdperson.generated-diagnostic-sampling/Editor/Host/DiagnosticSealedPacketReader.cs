using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ThirdPerson.GeneratedDiagnosticSampling.Host
{
    public sealed class DiagnosticSealedPacketReader : IDisposable
    {
        const string Magic = "generated-diagnostic-packet/1";
        readonly FileStream m_Stream;
        readonly BinaryReader m_Reader;

        public DiagnosticSealedPacketReader(
            DiagnosticSealedPacketArtifact artifact,
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticPacketLayout expectedLayout)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            if (capability == null)
                throw new ArgumentNullException(nameof(capability));
            Layout = expectedLayout ?? throw new ArgumentNullException(nameof(expectedLayout));
            m_Stream = new FileStream(
                artifact.Path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            try
            {
                if (m_Stream.Length != artifact.Size)
                    throw new InvalidDataException("Diagnostic packet size does not match the manifest.");
                string sha256 = ComputeSha256(m_Stream);
                if (!string.Equals(sha256, artifact.Sha256, StringComparison.Ordinal))
                    throw new InvalidDataException("Diagnostic packet hash does not match the manifest.");
                m_Stream.Position = 0;
                m_Reader = new BinaryReader(m_Stream, new UTF8Encoding(false), true);
                string magic = m_Reader.ReadString();
                if (!string.Equals(magic, Magic, StringComparison.Ordinal))
                    throw new InvalidDataException("Diagnostic packet magic is invalid.");
                CapabilityId = m_Reader.ReadString();
                CapabilityRevision = m_Reader.ReadInt32();
                ProgramId = m_Reader.ReadString();
                SchemaIdentity = m_Reader.ReadString();
                GeneratedProgramHash = m_Reader.ReadString();
                LineageTypeIdentity = m_Reader.ReadString();
                RequireHeader(capability);
                RequireLayout(Layout, true);
            }
            catch
            {
                m_Reader?.Dispose();
                m_Stream.Dispose();
                throw;
            }
        }

        public string CapabilityId { get; }
        public int CapabilityRevision { get; }
        public string ProgramId { get; }
        public string SchemaIdentity { get; }
        public string GeneratedProgramHash { get; }
        public string LineageTypeIdentity { get; }
        public DiagnosticPacketLayout Layout { get; }

        public IEnumerable<DiagnosticCapturePacket> ReadAll()
        {
            ulong previousSequence = 0;
            while (m_Stream.Position < m_Stream.Length)
            {
                DiagnosticCapturePacket packet = ReadPacket();
                if (!string.Equals(
                        packet.SampleKey.Lineage.TypeIdentity,
                        LineageTypeIdentity,
                        StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Diagnostic packet lineage does not match the manifest.");
                }
                if (packet.SampleKey.Sequence <= previousSequence)
                    throw new InvalidDataException("Diagnostic packet sequence is not strictly increasing.");
                previousSequence = packet.SampleKey.Sequence;
                yield return packet;
            }
        }

        public void Dispose()
        {
            m_Reader.Dispose();
            m_Stream.Dispose();
        }

        DiagnosticCapturePacket ReadPacket()
        {
            var packet = new DiagnosticCapturePacket(Layout);
            ulong sequence = m_Reader.ReadUInt64();
            string lineageTypeIdentity = m_Reader.ReadString();
            ulong lineageHigh = m_Reader.ReadUInt64();
            ulong lineageLow = m_Reader.ReadUInt64();
            var lineage = new DiagnosticLineageKey(
                lineageTypeIdentity,
                lineageHigh,
                lineageLow);
            var sampleKey = new DiagnosticSampleKey(sequence, lineage);
            packet.Begin(sampleKey);
            for (int i = 0; i < packet.BooleanValues.Length; i++) packet.BooleanValues[i] = m_Reader.ReadBoolean();
            for (int i = 0; i < packet.Int32Values.Length; i++) packet.Int32Values[i] = m_Reader.ReadInt32();
            for (int i = 0; i < packet.UInt32Values.Length; i++) packet.UInt32Values[i] = m_Reader.ReadUInt32();
            for (int i = 0; i < packet.Int64Values.Length; i++) packet.Int64Values[i] = m_Reader.ReadInt64();
            for (int i = 0; i < packet.UInt64Values.Length; i++) packet.UInt64Values[i] = m_Reader.ReadUInt64();
            for (int i = 0; i < packet.Float32Values.Length; i++) packet.Float32Values[i] = m_Reader.ReadSingle();
            for (int i = 0; i < packet.Float64Values.Length; i++) packet.Float64Values[i] = m_Reader.ReadDouble();
            for (int i = 0; i < packet.IdentityValues.Length; i++) packet.IdentityValues[i] = m_Reader.ReadString();
            for (int i = 0; i < packet.Vector2Values.Length; i++) packet.Vector2Values[i] = new DiagnosticVector2(m_Reader.ReadSingle(), m_Reader.ReadSingle());
            for (int i = 0; i < packet.Vector3Values.Length; i++) packet.Vector3Values[i] = new DiagnosticVector3(m_Reader.ReadSingle(), m_Reader.ReadSingle(), m_Reader.ReadSingle());
            for (int i = 0; i < packet.Vector4Values.Length; i++) packet.Vector4Values[i] = new DiagnosticVector4(m_Reader.ReadSingle(), m_Reader.ReadSingle(), m_Reader.ReadSingle(), m_Reader.ReadSingle());
            for (int i = 0; i < packet.QuaternionValues.Length; i++) packet.QuaternionValues[i] = new DiagnosticQuaternion(m_Reader.ReadSingle(), m_Reader.ReadSingle(), m_Reader.ReadSingle(), m_Reader.ReadSingle());
            for (int tableIndex = 0; tableIndex < packet.Tables.Length; tableIndex++)
            {
                DiagnosticTablePacket table = packet.Tables[tableIndex];
                int count = m_Reader.ReadInt32();
                table.Begin(count, sampleKey);
                for (int rowIndex = 0; rowIndex < count; rowIndex++)
                    ReadValues(table.Row(rowIndex));
            }
            return packet;
        }

        void RequireHeader(DiagnosticCapabilityBuildDescriptor capability)
        {
            if (!string.Equals(CapabilityId, capability.CapabilityId, StringComparison.Ordinal) ||
                CapabilityRevision != capability.CapabilityRevision ||
                !string.Equals(ProgramId, capability.ProgramId, StringComparison.Ordinal) ||
                !string.Equals(SchemaIdentity, capability.SchemaIdentity, StringComparison.Ordinal) ||
                !string.Equals(
                    GeneratedProgramHash,
                    capability.GeneratedProgramHash,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    LineageTypeIdentity,
                    capability.LineageTypeIdentity,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException("Diagnostic packet identity does not match the manifest.");
            }
        }

        void RequireLayout(DiagnosticPacketLayout expected, bool readTables)
        {
            string identity = m_Reader.ReadString();
            int booleanCount = m_Reader.ReadInt32();
            int int32Count = m_Reader.ReadInt32();
            int uint32Count = m_Reader.ReadInt32();
            int int64Count = m_Reader.ReadInt32();
            int uint64Count = m_Reader.ReadInt32();
            int float32Count = m_Reader.ReadInt32();
            int float64Count = m_Reader.ReadInt32();
            int identityCount = m_Reader.ReadInt32();
            int vector2Count = m_Reader.ReadInt32();
            int vector3Count = m_Reader.ReadInt32();
            int vector4Count = m_Reader.ReadInt32();
            int quaternionCount = m_Reader.ReadInt32();
            if (!string.Equals(identity, expected.Identity, StringComparison.Ordinal) ||
                booleanCount != expected.BooleanCount ||
                int32Count != expected.Int32Count ||
                uint32Count != expected.UInt32Count ||
                int64Count != expected.Int64Count ||
                uint64Count != expected.UInt64Count ||
                float32Count != expected.Float32Count ||
                float64Count != expected.Float64Count ||
                identityCount != expected.IdentityCount ||
                vector2Count != expected.Vector2Count ||
                vector3Count != expected.Vector3Count ||
                vector4Count != expected.Vector4Count ||
                quaternionCount != expected.QuaternionCount)
            {
                throw new InvalidDataException("Diagnostic packet layout does not match the schema.");
            }
            if (readTables)
            {
                int tableCount = m_Reader.ReadInt32();
                if (tableCount != expected.Tables.Count)
                    throw new InvalidDataException("Diagnostic packet table count does not match the schema.");
                for (int i = 0; i < tableCount; i++)
                {
                    string tableId = m_Reader.ReadString();
                    int capacity = m_Reader.ReadInt32();
                    DiagnosticTableLayout table = expected.Tables[i];
                    if (!string.Equals(tableId, table.Id, StringComparison.Ordinal) ||
                        capacity != table.Capacity)
                    {
                        throw new InvalidDataException("Diagnostic packet table layout does not match the schema.");
                    }
                    RequireLayout(table.RowLayout, false);
                }
            }
            else if (expected.Tables.Count != 0)
            {
                throw new InvalidDataException("Nested diagnostic tables are not supported.");
            }
        }

        void ReadValues(DiagnosticCapturePacket packet)
        {
            for (int i = 0; i < packet.BooleanValues.Length; i++) packet.BooleanValues[i] = m_Reader.ReadBoolean();
            for (int i = 0; i < packet.Int32Values.Length; i++) packet.Int32Values[i] = m_Reader.ReadInt32();
            for (int i = 0; i < packet.UInt32Values.Length; i++) packet.UInt32Values[i] = m_Reader.ReadUInt32();
            for (int i = 0; i < packet.Int64Values.Length; i++) packet.Int64Values[i] = m_Reader.ReadInt64();
            for (int i = 0; i < packet.UInt64Values.Length; i++) packet.UInt64Values[i] = m_Reader.ReadUInt64();
            for (int i = 0; i < packet.Float32Values.Length; i++) packet.Float32Values[i] = m_Reader.ReadSingle();
            for (int i = 0; i < packet.Float64Values.Length; i++) packet.Float64Values[i] = m_Reader.ReadDouble();
            for (int i = 0; i < packet.IdentityValues.Length; i++) packet.IdentityValues[i] = m_Reader.ReadString();
            for (int i = 0; i < packet.Vector2Values.Length; i++) packet.Vector2Values[i] = new DiagnosticVector2(m_Reader.ReadSingle(), m_Reader.ReadSingle());
            for (int i = 0; i < packet.Vector3Values.Length; i++) packet.Vector3Values[i] = new DiagnosticVector3(m_Reader.ReadSingle(), m_Reader.ReadSingle(), m_Reader.ReadSingle());
            for (int i = 0; i < packet.Vector4Values.Length; i++) packet.Vector4Values[i] = new DiagnosticVector4(m_Reader.ReadSingle(), m_Reader.ReadSingle(), m_Reader.ReadSingle(), m_Reader.ReadSingle());
            for (int i = 0; i < packet.QuaternionValues.Length; i++) packet.QuaternionValues[i] = new DiagnosticQuaternion(m_Reader.ReadSingle(), m_Reader.ReadSingle(), m_Reader.ReadSingle(), m_Reader.ReadSingle());
        }

        static string ComputeSha256(Stream stream)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash)
                    builder.Append(value.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
