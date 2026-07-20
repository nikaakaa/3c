using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public enum CharacterTargetProgramArtifactStatus
    {
        Missing,
        Current,
        Stale,
        Invalid,
        UnsupportedVersion
    }

    public sealed class CharacterTargetProgramArtifactResult
    {
        public CharacterTargetProgramArtifactResult(
            CharacterTargetProgramArtifactStatus status,
            string path,
            LoadedCharacterTargetProgramArtifact artifact,
            string message)
        {
            Status = status;
            Path = path ?? string.Empty;
            Artifact = artifact;
            Message = message ?? string.Empty;
        }

        public CharacterTargetProgramArtifactStatus Status { get; }
        public string Path { get; }
        public LoadedCharacterTargetProgramArtifact Artifact { get; }
        public string Message { get; }
        public bool IsCurrent => Status == CharacterTargetProgramArtifactStatus.Current && Artifact != null;
    }

    public sealed class CharacterTargetProgramArtifactPublishTransaction : IDisposable
    {
        readonly string m_TemporaryPath;
        readonly string m_DestinationPath;
        readonly string m_BackupPath;
        readonly string[] m_ObsoletePaths;
        readonly bool m_HadDestination;
        readonly LoadedCharacterTargetProgramArtifact m_StagedArtifact;
        bool m_Committed;
        bool m_Completed;

        internal CharacterTargetProgramArtifactPublishTransaction(
            string temporaryPath,
            string destinationPath,
            string backupPath,
            string[] obsoletePaths,
            bool hadDestination,
            LoadedCharacterTargetProgramArtifact stagedArtifact)
        {
            m_TemporaryPath = temporaryPath;
            m_DestinationPath = destinationPath;
            m_BackupPath = backupPath;
            m_ObsoletePaths = obsoletePaths ?? Array.Empty<string>();
            m_HadDestination = hadDestination;
            m_StagedArtifact = stagedArtifact;
        }

        public string DestinationPath => m_DestinationPath;
        public LoadedCharacterTargetProgramArtifact StagedArtifact => m_StagedArtifact;

        public LoadedCharacterTargetProgramArtifact Commit()
        {
            RequireOpen();
            if (m_Committed)
                throw new InvalidOperationException("Target Program artifact transaction is already committed.");
            if (m_HadDestination)
                File.Replace(m_TemporaryPath, m_DestinationPath, m_BackupPath);
            else
                File.Move(m_TemporaryPath, m_DestinationPath);
            m_Committed = true;
            LoadedCharacterTargetProgramArtifact published = CharacterTargetProgramArtifactLoader.Inspect(
                m_StagedArtifact.Descriptor.DefinitionGuid,
                File.ReadAllBytes(m_DestinationPath));
            if (!published.Descriptor.Equals(m_StagedArtifact.Descriptor) ||
                !BytesEqual(published.CopyCanonicalBytes(), m_StagedArtifact.CopyCanonicalBytes()))
                throw new InvalidDataException("Published Target Program artifact differs from the verified staged bytes.");
            return published;
        }

        public void Complete()
        {
            RequireOpen();
            if (!m_Committed)
                throw new InvalidOperationException("Target Program artifact transaction has not committed its staged bytes.");
            for (int i = 0; i < m_ObsoletePaths.Length; i++)
            {
                if (File.Exists(m_ObsoletePaths[i]))
                    File.Delete(m_ObsoletePaths[i]);
            }
            if (File.Exists(m_BackupPath))
                File.Delete(m_BackupPath);
            m_Completed = true;
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
                        throw new InvalidOperationException("Target Program artifact backup is missing during rollback.");
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
                throw new ObjectDisposedException(nameof(CharacterTargetProgramArtifactPublishTransaction));
        }

        static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }
    }

    public static class CharacterTargetProgramArtifactStore
    {
        public static string GetPath(string definitionGuid, NumericProfileId numericProfileId, TargetAbiVersion abiVersion)
        {
            CharacterTargetProgramArtifactLoader.RequireDefinitionGuid(definitionGuid);
            string profile = RequireProfilePathSegment(numericProfileId.Value);
            if (!abiVersion.IsValid)
                throw new ArgumentException("Target ABI version is required.", nameof(abiVersion));
            return Path.GetFullPath(Path.Combine(
                "Library",
                "CharacterSimulation",
                "Programs",
                definitionGuid,
                $"{profile}-abi{abiVersion.Value}.csim"));
        }

        public static CharacterTargetProgramArtifactPublishTransaction Stage(
            string definitionGuid,
            CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            string path = GetPath(
                definitionGuid,
                program.Manifest.NumericProfile.Id,
                program.Manifest.NumericProfile.AbiVersion);
            string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Target Program artifact directory is unavailable.");
            Directory.CreateDirectory(directory);
            string profile = RequireProfilePathSegment(program.Manifest.NumericProfile.Id.Value);
            string[] candidates = Directory.GetFiles(directory, $"{profile}-abi*.csim", SearchOption.TopDirectoryOnly);
            var obsolete = new List<string>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (!string.Equals(Path.GetFullPath(candidates[i]), path, StringComparison.OrdinalIgnoreCase))
                    obsolete.Add(candidates[i]);
            }
            string token = Guid.NewGuid().ToString("N");
            string temporaryPath = Path.Combine(directory, $"{Path.GetFileName(path)}.{token}.tmp");
            string backupPath = Path.Combine(directory, $"{Path.GetFileName(path)}.{token}.bak");
            try
            {
                byte[] canonicalBytes = CharacterSimulationProgramCodec.WriteArtifact(program);
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(canonicalBytes, 0, canonicalBytes.Length);
                    stream.Flush(true);
                }
                CharacterTargetProgramArtifactExpectation expectation = CreateExpectation(definitionGuid, program);
                LoadedCharacterTargetProgramArtifact staged = CharacterTargetProgramArtifactLoader.LoadFile(temporaryPath, expectation);
                return new CharacterTargetProgramArtifactPublishTransaction(
                    temporaryPath,
                    path,
                    backupPath,
                    obsolete.ToArray(),
                    File.Exists(path),
                    staged);
            }
            catch
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                throw;
            }
        }

        public static CharacterTargetProgramArtifactResult LoadCurrent(
            string definitionGuid,
            CharacterTargetProgramArtifactExpectation expectation)
        {
            string path = GetPath(
                definitionGuid,
                expectation.Program.NumericProfile.Id,
                expectation.Program.NumericProfile.AbiVersion);
            if (!File.Exists(path))
                return new CharacterTargetProgramArtifactResult(CharacterTargetProgramArtifactStatus.Missing, path, null, "Target Program artifact is missing.");
            try
            {
                LoadedCharacterTargetProgramArtifact artifact = CharacterTargetProgramArtifactLoader.Inspect(definitionGuid, File.ReadAllBytes(path));
                if (!Matches(artifact.Descriptor, expectation))
                    return new CharacterTargetProgramArtifactResult(CharacterTargetProgramArtifactStatus.Stale, path, artifact, "Target Program artifact identity does not match the current build expectation.");
                return new CharacterTargetProgramArtifactResult(CharacterTargetProgramArtifactStatus.Current, path, artifact, string.Empty);
            }
            catch (CharacterSimulationProgramArtifactVersionException exception)
            {
                return new CharacterTargetProgramArtifactResult(CharacterTargetProgramArtifactStatus.UnsupportedVersion, path, null, exception.Message);
            }
            catch (Exception exception)
            {
                return new CharacterTargetProgramArtifactResult(CharacterTargetProgramArtifactStatus.Invalid, path, null, exception.Message);
            }
        }

        public static CharacterTargetProgramArtifactResult Inspect(
            string definitionGuid,
            NumericProfileId numericProfileId,
            TargetAbiVersion abiVersion)
        {
            string path = GetPath(definitionGuid, numericProfileId, abiVersion);
            if (!File.Exists(path))
                return new CharacterTargetProgramArtifactResult(CharacterTargetProgramArtifactStatus.Missing, path, null, "Target Program artifact is missing.");
            try
            {
                LoadedCharacterTargetProgramArtifact artifact = CharacterTargetProgramArtifactLoader.Inspect(definitionGuid, File.ReadAllBytes(path));
                return new CharacterTargetProgramArtifactResult(CharacterTargetProgramArtifactStatus.Current, path, artifact, string.Empty);
            }
            catch (CharacterSimulationProgramArtifactVersionException exception)
            {
                return new CharacterTargetProgramArtifactResult(CharacterTargetProgramArtifactStatus.UnsupportedVersion, path, null, exception.Message);
            }
            catch (Exception exception)
            {
                return new CharacterTargetProgramArtifactResult(CharacterTargetProgramArtifactStatus.Invalid, path, null, exception.Message);
            }
        }

        public static CharacterTargetProgramArtifactExpectation CreateExpectation(
            string definitionGuid,
            CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            return new CharacterTargetProgramArtifactExpectation(
                definitionGuid,
                program.Manifest.ProgramId,
                program.Manifest.CompilerVersion,
                program.Manifest.OperationSetVersion,
                program.Manifest.SourceRevision,
                program.Manifest.SemanticHash,
                program.Manifest.NumericProfile,
                program.ProgramHash,
                program.LayoutHash,
                program.Manifest.Capabilities.RequiredWorldCapabilities);
        }

        static bool Matches(
            CharacterTargetProgramArtifactDescriptor descriptor,
            CharacterTargetProgramArtifactExpectation expectation)
        {
            ProgramLoadExpectation program = expectation.Program;
            return string.Equals(descriptor.DefinitionGuid, expectation.DefinitionGuid, StringComparison.Ordinal) &&
                   descriptor.ProgramId.Equals(expectation.ProgramId) &&
                   string.Equals(descriptor.CompilerVersion, program.CompilerVersion, StringComparison.Ordinal) &&
                   descriptor.OperationSetVersion.Equals(program.OperationSetVersion) &&
                   descriptor.SourceRevision.Equals(program.SourceRevision) &&
                   descriptor.SemanticHash.Equals(program.SemanticHash) &&
                   descriptor.NumericProfileId.Equals(program.NumericProfile.Id) &&
                   descriptor.TargetAbiVersion.Equals(program.NumericProfile.AbiVersion) &&
                   descriptor.ProgramHash.Equals(expectation.ProgramHash) &&
                   descriptor.LayoutHash.Equals(expectation.LayoutHash) &&
                   descriptor.RequiredWorldCapabilities == expectation.RequiredWorldCapabilities;
        }

        static string RequireProfilePathSegment(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Numeric Profile identity is required.", nameof(value));
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (!((character >= 'a' && character <= 'z') ||
                      (character >= '0' && character <= '9') ||
                      character == '-' || character == '_' || character == '.'))
                    throw new ArgumentException("Numeric Profile identity is not a safe artifact path segment.", nameof(value));
            }
            return value;
        }
    }
}
