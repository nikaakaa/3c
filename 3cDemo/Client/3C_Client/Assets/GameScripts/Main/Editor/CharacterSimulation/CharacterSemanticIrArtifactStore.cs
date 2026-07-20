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
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            string path = GetPath(definitionGuid);
            string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Semantic IR cache directory is unavailable.");
            Directory.CreateDirectory(directory);
            string temporaryPath = Path.Combine(directory, $"{definitionGuid}.{Guid.NewGuid():N}.tmp");
            try
            {
                byte[] bytes = artifact.ToArray();
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                SemanticIrLoadExpectation expectation = CreateExpectation(artifact.Header);
                ValidatedSemanticIrArtifact verified = CharacterGameplaySemanticIrCodec.ReadValidatedArtifact(File.ReadAllBytes(temporaryPath), expectation);
                if (File.Exists(path))
                    File.Replace(temporaryPath, path, null);
                else
                    File.Move(temporaryPath, path);
                return CharacterGameplaySemanticIrCodec.ReadValidatedArtifact(File.ReadAllBytes(path), expectation);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
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
