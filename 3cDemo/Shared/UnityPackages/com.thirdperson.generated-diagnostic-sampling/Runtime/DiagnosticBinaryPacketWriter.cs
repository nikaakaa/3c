using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ThirdPerson.GeneratedDiagnosticSampling
{
    public sealed class DiagnosticBinaryPacketWriter : IDiagnosticPacketWriter
    {
        const string Magic = "generated-diagnostic-packet/1";
        readonly string m_FinalPath;
        readonly string m_StagingPath;
        FileStream m_Stream;
        BinaryWriter m_Writer;

        public DiagnosticBinaryPacketWriter(string path)
        {
            m_FinalPath = DiagnosticIdentity.RequireText(path, nameof(path));
            m_StagingPath = m_FinalPath + ".staging";
        }

        public DiagnosticSealedPacketArtifact Artifact { get; private set; }

        public void Begin(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticPacketLayout layout)
        {
            if (m_Writer != null)
                throw new InvalidOperationException("Diagnostic writer is already open.");
            string directory = Path.GetDirectoryName(Path.GetFullPath(m_FinalPath));
            Directory.CreateDirectory(directory);
            if (File.Exists(m_FinalPath) || File.Exists(m_StagingPath))
                throw new IOException("Diagnostic packet output already exists.");
            m_Stream = new FileStream(
                m_StagingPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read);
            m_Writer = new BinaryWriter(m_Stream, new UTF8Encoding(false), true);
            m_Writer.Write(Magic);
            m_Writer.Write(capability.CapabilityId);
            m_Writer.Write(capability.CapabilityRevision);
            m_Writer.Write(capability.ProgramId);
            m_Writer.Write(capability.SchemaIdentity);
            m_Writer.Write(capability.GeneratedProgramHash);
            m_Writer.Write(capability.LineageTypeIdentity);
            m_Writer.Write(layout.Identity);
            m_Writer.Write(layout.BooleanCount);
            m_Writer.Write(layout.Int32Count);
            m_Writer.Write(layout.UInt32Count);
            m_Writer.Write(layout.Int64Count);
            m_Writer.Write(layout.UInt64Count);
            m_Writer.Write(layout.Float32Count);
            m_Writer.Write(layout.Float64Count);
            m_Writer.Write(layout.IdentityCount);
            m_Writer.Write(layout.Vector2Count);
            m_Writer.Write(layout.Vector3Count);
            m_Writer.Write(layout.Vector4Count);
            m_Writer.Write(layout.QuaternionCount);
            m_Writer.Write(layout.Tables.Count);
            foreach (DiagnosticTableLayout table in layout.Tables)
            {
                m_Writer.Write(table.Id);
                m_Writer.Write(table.Capacity);
                WriteLayout(table.RowLayout);
            }
        }

        public void Write(DiagnosticCapturePacket packet)
        {
            if (m_Writer == null)
                throw new InvalidOperationException("Diagnostic writer is not open.");
            m_Writer.Write(packet.SampleKey.Sequence);
            m_Writer.Write(packet.SampleKey.Lineage.TypeIdentity);
            m_Writer.Write(packet.SampleKey.Lineage.ValueHigh);
            m_Writer.Write(packet.SampleKey.Lineage.ValueLow);
            foreach (bool value in packet.BooleanValues) m_Writer.Write(value);
            foreach (int value in packet.Int32Values) m_Writer.Write(value);
            foreach (uint value in packet.UInt32Values) m_Writer.Write(value);
            foreach (long value in packet.Int64Values) m_Writer.Write(value);
            foreach (ulong value in packet.UInt64Values) m_Writer.Write(value);
            foreach (float value in packet.Float32Values) m_Writer.Write(value);
            foreach (double value in packet.Float64Values) m_Writer.Write(value);
            foreach (string value in packet.IdentityValues) m_Writer.Write(value ?? string.Empty);
            foreach (DiagnosticVector2 value in packet.Vector2Values)
            {
                m_Writer.Write(value.X);
                m_Writer.Write(value.Y);
            }
            foreach (DiagnosticVector3 value in packet.Vector3Values)
            {
                m_Writer.Write(value.X);
                m_Writer.Write(value.Y);
                m_Writer.Write(value.Z);
            }
            foreach (DiagnosticVector4 value in packet.Vector4Values)
            {
                m_Writer.Write(value.X);
                m_Writer.Write(value.Y);
                m_Writer.Write(value.Z);
                m_Writer.Write(value.W);
            }
            foreach (DiagnosticQuaternion value in packet.QuaternionValues)
            {
                m_Writer.Write(value.X);
                m_Writer.Write(value.Y);
                m_Writer.Write(value.Z);
                m_Writer.Write(value.W);
            }
            foreach (DiagnosticTablePacket table in packet.Tables)
            {
                m_Writer.Write(table.Count);
                for (int i = 0; i < table.Count; i++)
                    WriteValues(table.Row(i));
            }
        }

        public void Complete()
        {
            if (m_Writer == null)
                throw new InvalidOperationException("Diagnostic writer is not open.");
            m_Writer.Flush();
            m_Stream.Flush(true);
            Dispose();
            string hash = ComputeHash(m_StagingPath);
            long size = new FileInfo(m_StagingPath).Length;
            File.Move(m_StagingPath, m_FinalPath);
            Artifact = new DiagnosticSealedPacketArtifact(m_FinalPath, size, hash);
        }

        public void Fault(in DiagnosticCaptureFailure failure)
        {
            if (m_Writer == null)
                return;
            m_Writer.Flush();
            Dispose();
        }

        public void Dispose()
        {
            m_Writer?.Dispose();
            m_Stream?.Dispose();
            m_Writer = null;
            m_Stream = null;
        }

        void WriteLayout(DiagnosticPacketLayout layout)
        {
            m_Writer.Write(layout.Identity);
            m_Writer.Write(layout.BooleanCount);
            m_Writer.Write(layout.Int32Count);
            m_Writer.Write(layout.UInt32Count);
            m_Writer.Write(layout.Int64Count);
            m_Writer.Write(layout.UInt64Count);
            m_Writer.Write(layout.Float32Count);
            m_Writer.Write(layout.Float64Count);
            m_Writer.Write(layout.IdentityCount);
            m_Writer.Write(layout.Vector2Count);
            m_Writer.Write(layout.Vector3Count);
            m_Writer.Write(layout.Vector4Count);
            m_Writer.Write(layout.QuaternionCount);
            if (layout.Tables.Count != 0)
                throw new InvalidOperationException("Nested diagnostic tables are not supported.");
        }

        void WriteValues(DiagnosticCapturePacket packet)
        {
            foreach (bool value in packet.BooleanValues) m_Writer.Write(value);
            foreach (int value in packet.Int32Values) m_Writer.Write(value);
            foreach (uint value in packet.UInt32Values) m_Writer.Write(value);
            foreach (long value in packet.Int64Values) m_Writer.Write(value);
            foreach (ulong value in packet.UInt64Values) m_Writer.Write(value);
            foreach (float value in packet.Float32Values) m_Writer.Write(value);
            foreach (double value in packet.Float64Values) m_Writer.Write(value);
            foreach (string value in packet.IdentityValues) m_Writer.Write(value ?? string.Empty);
            foreach (DiagnosticVector2 value in packet.Vector2Values)
            {
                m_Writer.Write(value.X);
                m_Writer.Write(value.Y);
            }
            foreach (DiagnosticVector3 value in packet.Vector3Values)
            {
                m_Writer.Write(value.X);
                m_Writer.Write(value.Y);
                m_Writer.Write(value.Z);
            }
            foreach (DiagnosticVector4 value in packet.Vector4Values)
            {
                m_Writer.Write(value.X);
                m_Writer.Write(value.Y);
                m_Writer.Write(value.Z);
                m_Writer.Write(value.W);
            }
            foreach (DiagnosticQuaternion value in packet.QuaternionValues)
            {
                m_Writer.Write(value.X);
                m_Writer.Write(value.Y);
                m_Writer.Write(value.Z);
                m_Writer.Write(value.W);
            }
        }

        static string ComputeHash(string path)
        {
            using (FileStream stream = File.OpenRead(path))
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

    public sealed class DiagnosticSealedPacketArtifact
    {
        public DiagnosticSealedPacketArtifact(string path, long size, string sha256)
        {
            Path = DiagnosticIdentity.RequireText(path, nameof(path));
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));
            Size = size;
            Sha256 = DiagnosticIdentity.RequireId(sha256, nameof(sha256));
        }

        public string Path { get; }
        public long Size { get; }
        public string Sha256 { get; }
    }
}
