using System;
using System.IO;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public enum CharacterSemanticIrCacheStatus
    {
        Missing,
        Current,
        Stale,
        Invalid,
        UnsupportedVersion
    }

    public sealed class CharacterSemanticIrCacheResult
    {
        public CharacterSemanticIrCacheResult(
            CharacterSemanticIrCacheStatus status,
            string path,
            CharacterGameplaySemanticIrArtifactHeader header,
            ValidatedSemanticIrArtifact artifact,
            string message)
        {
            Status = status;
            Path = path ?? string.Empty;
            Header = header;
            Artifact = artifact;
            Message = message ?? string.Empty;
        }

        public CharacterSemanticIrCacheStatus Status { get; }
        public string Path { get; }
        public CharacterGameplaySemanticIrArtifactHeader Header { get; }
        public ValidatedSemanticIrArtifact Artifact { get; }
        public string Message { get; }
        public bool IsCurrent => Status == CharacterSemanticIrCacheStatus.Current && Artifact != null;
    }

    public sealed class CharacterSemanticIrArtifactPublishTransaction : IDisposable
    {
        readonly string m_TemporaryPath;
        readonly string m_DestinationPath;
        readonly string m_BackupPath;
        readonly bool m_HadDestination;
        readonly byte[] m_CanonicalBytes;
        readonly ValidatedSemanticIrArtifact m_StagedArtifact;
        bool m_Committed;
        bool m_Completed;

        internal CharacterSemanticIrArtifactPublishTransaction(
            string temporaryPath,
            string destinationPath,
            string backupPath,
            bool hadDestination,
            byte[] canonicalBytes,
            ValidatedSemanticIrArtifact stagedArtifact)
        {
            m_TemporaryPath = temporaryPath;
            m_DestinationPath = destinationPath;
            m_BackupPath = backupPath;
            m_HadDestination = hadDestination;
            m_CanonicalBytes = canonicalBytes ?? throw new ArgumentNullException(nameof(canonicalBytes));
            m_StagedArtifact = stagedArtifact ?? throw new ArgumentNullException(nameof(stagedArtifact));
        }

        public ValidatedSemanticIrArtifact StagedArtifact => m_StagedArtifact;

        public ValidatedSemanticIrArtifact Commit()
        {
            RequireOpen();
            if (m_Committed)
                throw new InvalidOperationException("Semantic IR artifact transaction is already committed.");
            if (m_HadDestination)
                File.Replace(m_TemporaryPath, m_DestinationPath, m_BackupPath);
            else
                File.Move(m_TemporaryPath, m_DestinationPath);
            m_Committed = true;
            byte[] publishedBytes = File.ReadAllBytes(m_DestinationPath);
            if (!BytesEqual(publishedBytes, m_CanonicalBytes))
                throw new InvalidDataException("Published Semantic IR artifact differs from the verified staged bytes.");
            return CharacterGameplaySemanticIrCodec.ReadValidatedArtifact(
                publishedBytes,
                CharacterSemanticIrArtifactStore.CreateExpectation(m_StagedArtifact.Header));
        }

        public void Complete()
        {
            RequireOpen();
            if (!m_Committed)
                throw new InvalidOperationException("Semantic IR artifact transaction has not committed its staged bytes.");
            m_Completed = true;
            TryDelete(m_BackupPath);
        }

        public void Rollback()
        {
            if (m_Completed)
                return;
            if (m_Committed)
            {
                if (m_HadDestination)
                {
                    if (!File.Exists(m_BackupPath))
                        throw new InvalidOperationException("Semantic IR artifact backup is missing during rollback.");
                    if (File.Exists(m_DestinationPath))
                        File.Replace(m_BackupPath, m_DestinationPath, null);
                    else
                        File.Move(m_BackupPath, m_DestinationPath);
                }
                else if (File.Exists(m_DestinationPath))
                {
                    File.Delete(m_DestinationPath);
                }
            }
            if (File.Exists(m_TemporaryPath))
                File.Delete(m_TemporaryPath);
            if (File.Exists(m_BackupPath))
                File.Delete(m_BackupPath);
            m_Completed = true;
        }

        public void Dispose()
        {
            if (!m_Completed)
                Rollback();
        }

        void RequireOpen()
        {
            if (m_Completed)
                throw new ObjectDisposedException(nameof(CharacterSemanticIrArtifactPublishTransaction));
        }

        static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }

        static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public static class CharacterSemanticIrArtifactStore
    {
        public static string GetPath(string definitionGuid)
        {
            RequireGuid(definitionGuid);
            return Path.GetFullPath(Path.Combine(
                "Library",
                "CharacterSimulation",
                "SemanticIr",
                $"{definitionGuid}.csir"));
        }

        public static ValidatedSemanticIrArtifact Write(string definitionGuid, ValidatedSemanticIrArtifact artifact)
        {
            using CharacterSemanticIrArtifactPublishTransaction transaction = Stage(definitionGuid, artifact);
            ValidatedSemanticIrArtifact published = transaction.Commit();
            transaction.Complete();
            return published;
        }

        public static CharacterSemanticIrArtifactPublishTransaction Stage(
            string definitionGuid,
            ValidatedSemanticIrArtifact artifact)
        {
            RequireGuid(definitionGuid);
            ValidatedSemanticIrArtifact verified = RoundTrip(artifact);
            byte[] bytes = verified.ToArray();
            string path = GetPath(definitionGuid);
            string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Semantic IR cache directory is unavailable.");
            Directory.CreateDirectory(directory);
            string token = Guid.NewGuid().ToString("N");
            string temporaryPath = Path.Combine(directory, $"{definitionGuid}.{token}.tmp");
            string backupPath = Path.Combine(directory, $"{definitionGuid}.{token}.bak");
            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                byte[] stagedBytes = File.ReadAllBytes(temporaryPath);
                ValidatedSemanticIrArtifact staged = CharacterGameplaySemanticIrCodec.ReadValidatedArtifact(
                    stagedBytes,
                    CreateExpectation(verified.Header));
                return new CharacterSemanticIrArtifactPublishTransaction(
                    temporaryPath,
                    path,
                    backupPath,
                    File.Exists(path),
                    stagedBytes,
                    staged);
            }
            catch
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
                throw;
            }
        }

        internal static ValidatedSemanticIrArtifact RoundTrip(ValidatedSemanticIrArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            return CharacterGameplaySemanticIrCodec.ReadValidatedArtifact(
                artifact.ToArray(),
                CreateExpectation(artifact.Header));
        }

        public static CharacterSemanticIrCacheResult LoadCurrent(string definitionGuid, SemanticIrLoadExpectation expectation)
        {
            string path = GetPath(definitionGuid);
            if (!File.Exists(path))
                return new CharacterSemanticIrCacheResult(CharacterSemanticIrCacheStatus.Missing, path, null, null, "Semantic IR cache is missing.");
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                CharacterGameplaySemanticIrArtifactHeader header = CharacterGameplaySemanticIrCodec.ReadArtifactHeader(bytes);
                if (!Matches(header, expectation))
                    return new CharacterSemanticIrCacheResult(CharacterSemanticIrCacheStatus.Stale, path, header, null, "Semantic IR cache identity does not match the current build expectation.");
                ValidatedSemanticIrArtifact artifact = CharacterGameplaySemanticIrCodec.ReadValidatedArtifact(bytes, expectation);
                return new CharacterSemanticIrCacheResult(CharacterSemanticIrCacheStatus.Current, path, artifact.Header, artifact, string.Empty);
            }
            catch (SemanticIrArtifactVersionException exception)
            {
                return new CharacterSemanticIrCacheResult(CharacterSemanticIrCacheStatus.UnsupportedVersion, path, null, null, exception.Message);
            }
            catch (Exception exception)
            {
                return new CharacterSemanticIrCacheResult(CharacterSemanticIrCacheStatus.Invalid, path, null, null, exception.Message);
            }
        }

        public static CharacterSemanticIrCacheResult Inspect(string definitionGuid)
        {
            string path = GetPath(definitionGuid);
            if (!File.Exists(path))
                return new CharacterSemanticIrCacheResult(CharacterSemanticIrCacheStatus.Missing, path, null, null, "Semantic IR cache is missing.");
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                CharacterGameplaySemanticIrArtifactHeader header = CharacterGameplaySemanticIrCodec.ReadArtifactHeader(bytes);
                ValidatedSemanticIrArtifact artifact = CharacterGameplaySemanticIrCodec.ReadValidatedArtifact(bytes);
                return new CharacterSemanticIrCacheResult(CharacterSemanticIrCacheStatus.Current, path, header, artifact, string.Empty);
            }
            catch (SemanticIrArtifactVersionException exception)
            {
                return new CharacterSemanticIrCacheResult(CharacterSemanticIrCacheStatus.UnsupportedVersion, path, null, null, exception.Message);
            }
            catch (Exception exception)
            {
                return new CharacterSemanticIrCacheResult(CharacterSemanticIrCacheStatus.Invalid, path, null, null, exception.Message);
            }
        }

        public static SemanticIrLoadExpectation CreateExpectation(CharacterGameplaySemanticIrArtifactHeader header)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));
            return new SemanticIrLoadExpectation(
                header.ProgramId,
                header.CompilerVersion,
                header.OperationSetVersion,
                header.TickRate,
                header.SourceRevision,
                header.SemanticHash);
        }

        static bool Matches(CharacterGameplaySemanticIrArtifactHeader header, SemanticIrLoadExpectation expectation)
        {
            return header.ProgramId.Equals(expectation.ProgramId) &&
                   string.Equals(header.CompilerVersion, expectation.CompilerVersion, StringComparison.Ordinal) &&
                   header.OperationSetVersion.Equals(expectation.OperationSetVersion) &&
                   header.TickRate == expectation.TickRate &&
                   header.SourceRevision.Equals(expectation.SourceRevision) &&
                   header.SemanticHash.Equals(expectation.SemanticHash);
        }

        static void RequireGuid(string definitionGuid)
        {
            if (string.IsNullOrEmpty(definitionGuid) || definitionGuid.Length != 32)
                throw new ArgumentException("Definition GUID must contain 32 lowercase hexadecimal characters.", nameof(definitionGuid));
            for (int i = 0; i < definitionGuid.Length; i++)
            {
                char value = definitionGuid[i];
                if (!((value >= '0' && value <= '9') || (value >= 'a' && value <= 'f')))
                    throw new ArgumentException("Definition GUID must contain 32 lowercase hexadecimal characters.", nameof(definitionGuid));
            }
        }
    }
}
