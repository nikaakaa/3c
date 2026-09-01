using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ThirdPerson.GeneratedDiagnosticSampling
{
    public sealed class DiagnosticSealedArtifact
    {
        public DiagnosticSealedArtifact(string path, long size, string sha256)
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

    public sealed class DiagnosticEncodedDocument
    {
        readonly byte[] m_Content;

        public DiagnosticEncodedDocument(byte[] content, string sha256)
        {
            if (content == null || content.Length == 0)
                throw new ArgumentException("Diagnostic document content is required.", nameof(content));
            m_Content = (byte[])content.Clone();
            Sha256 = DiagnosticIdentity.RequireId(sha256, nameof(sha256));
            string actual = DiagnosticArtifactIntegrity.ComputeSha256(m_Content);
            if (!string.Equals(actual, Sha256, StringComparison.Ordinal))
                throw new InvalidDataException("Diagnostic document hash is invalid.");
        }

        public int Length => m_Content.Length;
        public string Sha256 { get; }
        public byte[] CopyContent() => (byte[])m_Content.Clone();

        internal static DiagnosticEncodedDocument Create(byte[] content) =>
            new DiagnosticEncodedDocument(
                content,
                DiagnosticArtifactIntegrity.ComputeSha256(content));
    }

    public static class DiagnosticArtifactStore
    {
        public static DiagnosticSealedArtifact Seal(
            string path,
            DiagnosticEncodedDocument document)
        {
            path = DiagnosticIdentity.RequireText(path, nameof(path));
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            string finalPath = Path.GetFullPath(path);
            string stagingPath = finalPath + ".staging";
            string directory = Path.GetDirectoryName(finalPath);
            Directory.CreateDirectory(directory);
            if (File.Exists(finalPath) || File.Exists(stagingPath))
                throw new IOException("Diagnostic artifact output already exists.");
            byte[] content = document.CopyContent();
            using (var stream = new FileStream(
                stagingPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read))
            {
                stream.Write(content, 0, content.Length);
                stream.Flush(true);
            }
            File.Move(stagingPath, finalPath);
            return new DiagnosticSealedArtifact(finalPath, content.Length, document.Sha256);
        }

        public static DiagnosticEncodedDocument Open(DiagnosticSealedArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            byte[] content = File.ReadAllBytes(artifact.Path);
            if (content.LongLength != artifact.Size)
                throw new InvalidDataException("Diagnostic artifact size does not match the manifest.");
            return new DiagnosticEncodedDocument(content, artifact.Sha256);
        }

        public static void Require(DiagnosticSealedArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            using (var stream = new FileStream(
                artifact.Path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read))
            {
                if (stream.Length != artifact.Size)
                    throw new InvalidDataException("Diagnostic artifact size does not match the manifest.");
                string sha256 = DiagnosticArtifactIntegrity.ComputeSha256(stream);
                if (!string.Equals(sha256, artifact.Sha256, StringComparison.Ordinal))
                    throw new InvalidDataException("Diagnostic artifact hash does not match the manifest.");
            }
        }
    }

    public static class DiagnosticArtifactIntegrity
    {
        public static string ComputeSha256(byte[] content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            using (SHA256 sha = SHA256.Create())
                return Hex(sha.ComputeHash(content));
        }

        public static string ComputeSha256(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            using (SHA256 sha = SHA256.Create())
                return Hex(sha.ComputeHash(stream));
        }

        static string Hex(byte[] hash)
        {
            var builder = new StringBuilder(hash.Length * 2);
            foreach (byte value in hash)
                builder.Append(value.ToString("x2"));
            return builder.ToString();
        }
    }
}
