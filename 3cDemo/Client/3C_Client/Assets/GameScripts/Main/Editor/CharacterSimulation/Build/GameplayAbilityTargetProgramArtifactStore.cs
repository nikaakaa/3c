using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using FixedProgram = ThirdPersonSimulation.Fixed.CharacterSimulationProgram;
using FixedLoadedArtifact = ThirdPersonSimulation.Fixed.LoadedCharacterTargetProgramArtifact;
using FixedLoader = ThirdPersonSimulation.Fixed.CharacterTargetProgramArtifactLoader;
using Float32Program = ThirdPersonSimulation.CharacterSimulationProgram;
using Float32LoadedArtifact = ThirdPersonSimulation.LoadedCharacterTargetProgramArtifact;
using Float32Loader = ThirdPersonSimulation.CharacterTargetProgramArtifactLoader;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class GameplayAbilityTargetProgramArtifactPaths
    {
        public static string GetPath(
            string abilityGuid,
            NumericProfileId numericProfileId,
            TargetAbiVersion abiVersion)
        {
            Float32Loader.RequireDefinitionGuid(abilityGuid);
            if (!numericProfileId.IsValid)
                throw new ArgumentException("Numeric Profile identity is required.", nameof(numericProfileId));
            if (!abiVersion.IsValid)
                throw new ArgumentException("Target ABI version is required.", nameof(abiVersion));
            return Path.GetFullPath(Path.Combine(
                "Library",
                "CharacterSimulation",
                "Abilities",
                abilityGuid,
                $"{numericProfileId.Value}-abi{abiVersion.Value}.csim"));
        }
    }

    public sealed class GameplayAbilityTargetProgramArtifactPublishTransaction<TArtifact> : IDisposable
        where TArtifact : class
    {
        readonly string m_TemporaryPath;
        readonly string m_DestinationPath;
        readonly string m_BackupPath;
        readonly string[] m_ObsoletePaths;
        readonly bool m_HadDestination;
        readonly byte[] m_CanonicalBytes;
        readonly Func<byte[], TArtifact> m_Read;
        readonly TArtifact m_StagedArtifact;
        bool m_Committed;
        bool m_Completed;

        internal GameplayAbilityTargetProgramArtifactPublishTransaction(
            string temporaryPath,
            string destinationPath,
            string backupPath,
            string[] obsoletePaths,
            bool hadDestination,
            byte[] canonicalBytes,
            Func<byte[], TArtifact> read,
            TArtifact stagedArtifact)
        {
            m_TemporaryPath = temporaryPath;
            m_DestinationPath = destinationPath;
            m_BackupPath = backupPath;
            m_ObsoletePaths = obsoletePaths ?? Array.Empty<string>();
            m_HadDestination = hadDestination;
            m_CanonicalBytes = canonicalBytes == null
                ? throw new ArgumentNullException(nameof(canonicalBytes))
                : (byte[])canonicalBytes.Clone();
            m_Read = read ?? throw new ArgumentNullException(nameof(read));
            m_StagedArtifact = stagedArtifact ?? throw new ArgumentNullException(nameof(stagedArtifact));
        }

        public string DestinationPath => m_DestinationPath;
        public TArtifact StagedArtifact => m_StagedArtifact;

        public TArtifact Commit()
        {
            RequireOpen();
            if (m_Committed)
                throw new InvalidOperationException("Ability Target Program artifact transaction is already committed.");
            if (m_HadDestination)
                File.Replace(m_TemporaryPath, m_DestinationPath, m_BackupPath);
            else
                File.Move(m_TemporaryPath, m_DestinationPath);
            m_Committed = true;
            byte[] publishedBytes = File.ReadAllBytes(m_DestinationPath);
            if (!BytesEqual(publishedBytes, m_CanonicalBytes))
                throw new InvalidDataException("Published Ability Target Program artifact differs from the verified staged bytes.");
            return m_Read(publishedBytes);
        }

        public void Complete()
        {
            RequireOpen();
            if (!m_Committed)
                throw new InvalidOperationException("Ability Target Program artifact transaction has not committed its bytes.");
            m_Completed = true;
            for (int i = 0; i < m_ObsoletePaths.Length; i++)
                TryDelete(m_ObsoletePaths[i]);
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
                        throw new InvalidOperationException("Ability Target Program artifact backup is missing during rollback.");
                    if (File.Exists(m_DestinationPath))
                        File.Replace(m_BackupPath, m_DestinationPath, null);
                    else
                        File.Move(m_BackupPath, m_DestinationPath);
                }
                else if (File.Exists(m_DestinationPath))
                    File.Delete(m_DestinationPath);
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
                throw new ObjectDisposedException(nameof(GameplayAbilityTargetProgramArtifactPublishTransaction<TArtifact>));
        }

        static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
                if (left[i] != right[i])
                    return false;
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

    public static class GameplayAbilityFloat32TargetProgramArtifactStore
    {
        public static string GetPath(string abilityGuid) =>
            GameplayAbilityTargetProgramArtifactPaths.GetPath(
                abilityGuid,
                Float32SimulationNumericProfile.Value.Id,
                Float32SimulationNumericProfile.Value.AbiVersion);

        public static GameplayAbilityTargetProgramArtifactPublishTransaction<Float32LoadedArtifact> Stage(
            string abilityGuid,
            Float32Program program)
        {
            RequireAbilityRoot(abilityGuid, program);
            string path = GameplayAbilityTargetProgramArtifactPaths.GetPath(
                abilityGuid,
                program.Manifest.NumericProfile.Id,
                program.Manifest.NumericProfile.AbiVersion);
            return Stage(
                abilityGuid,
                path,
                program,
                ThirdPersonSimulation.CharacterSimulationProgramCodec.WriteArtifact,
                bytes => Float32Loader.Inspect(abilityGuid, bytes));
        }

        public static Float32LoadedArtifact Write(string abilityGuid, Float32Program program)
        {
            using GameplayAbilityTargetProgramArtifactPublishTransaction<Float32LoadedArtifact> transaction = Stage(abilityGuid, program);
            Float32LoadedArtifact published = transaction.Commit();
            transaction.Complete();
            return published;
        }

        public static Float32LoadedArtifact Load(string abilityGuid)
        {
            string path = GetPath(abilityGuid);
            if (!File.Exists(path))
                throw new FileNotFoundException("Ability Float32 Target Program artifact is missing.", path);
            Float32LoadedArtifact artifact = Float32Loader.Inspect(abilityGuid, File.ReadAllBytes(path));
            RequireAbilityRoot(abilityGuid, artifact.Program);
            return artifact;
        }

        static GameplayAbilityTargetProgramArtifactPublishTransaction<Float32LoadedArtifact> Stage(
            string abilityGuid,
            string path,
            Float32Program program,
            Func<Float32Program, byte[]> write,
            Func<byte[], Float32LoadedArtifact> read)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Ability Target artifact directory is unavailable.");
            Directory.CreateDirectory(directory);
            string profile = program.Manifest.NumericProfile.Id.Value;
            string[] candidates = Directory.GetFiles(directory, $"{profile}-abi*.csim", SearchOption.TopDirectoryOnly);
            var obsolete = new List<string>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
                if (!string.Equals(Path.GetFullPath(candidates[i]), path, StringComparison.OrdinalIgnoreCase))
                    obsolete.Add(candidates[i]);
            byte[] canonicalBytes = write(program);
            string token = Guid.NewGuid().ToString("N");
            string temporaryPath = Path.Combine(directory, $"{Path.GetFileName(path)}.{token}.tmp");
            string backupPath = Path.Combine(directory, $"{Path.GetFileName(path)}.{token}.bak");
            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(canonicalBytes, 0, canonicalBytes.Length);
                    stream.Flush(true);
                }
                Float32LoadedArtifact staged = read(File.ReadAllBytes(temporaryPath));
                return new GameplayAbilityTargetProgramArtifactPublishTransaction<Float32LoadedArtifact>(
                    temporaryPath,
                    path,
                    backupPath,
                    obsolete.ToArray(),
                    File.Exists(path),
                    canonicalBytes,
                    read,
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

        static void RequireAbilityRoot(string abilityGuid, Float32Program program)
        {
            Float32Loader.RequireDefinitionGuid(abilityGuid);
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (!program.Manifest.Root.IsAbility ||
                !string.Equals(program.Manifest.Root.RootIdentity, abilityGuid, StringComparison.Ordinal))
                throw new InvalidOperationException("Ability Float32 Target Program root identity is invalid.");
        }
    }

    public static class GameplayAbilityFixedTargetProgramArtifactStore
    {
        public static string GetPath(string abilityGuid) =>
            GameplayAbilityTargetProgramArtifactPaths.GetPath(
                abilityGuid,
                FixedSimulationNumericProfile.Value.Id,
                FixedSimulationNumericProfile.Value.AbiVersion);

        public static GameplayAbilityTargetProgramArtifactPublishTransaction<FixedLoadedArtifact> Stage(
            string abilityGuid,
            FixedProgram program)
        {
            RequireAbilityRoot(abilityGuid, program);
            string path = GameplayAbilityTargetProgramArtifactPaths.GetPath(
                abilityGuid,
                program.Manifest.NumericProfile.Id,
                program.Manifest.NumericProfile.AbiVersion);
            return Stage(abilityGuid, path, program);
        }

        public static FixedLoadedArtifact Write(string abilityGuid, FixedProgram program)
        {
            using GameplayAbilityTargetProgramArtifactPublishTransaction<FixedLoadedArtifact> transaction = Stage(abilityGuid, program);
            FixedLoadedArtifact published = transaction.Commit();
            transaction.Complete();
            return published;
        }

        public static FixedLoadedArtifact Load(string abilityGuid)
        {
            string path = GetPath(abilityGuid);
            if (!File.Exists(path))
                throw new FileNotFoundException("Ability Fixed Target Program artifact is missing.", path);
            FixedLoadedArtifact artifact = FixedLoader.Inspect(abilityGuid, File.ReadAllBytes(path));
            RequireAbilityRoot(abilityGuid, artifact.Program);
            return artifact;
        }

        static GameplayAbilityTargetProgramArtifactPublishTransaction<FixedLoadedArtifact> Stage(
            string abilityGuid,
            string path,
            FixedProgram program)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Ability Fixed Target artifact directory is unavailable.");
            Directory.CreateDirectory(directory);
            string profile = program.Manifest.NumericProfile.Id.Value;
            string[] candidates = Directory.GetFiles(directory, $"{profile}-abi*.csim", SearchOption.TopDirectoryOnly);
            var obsolete = new List<string>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
                if (!string.Equals(Path.GetFullPath(candidates[i]), path, StringComparison.OrdinalIgnoreCase))
                    obsolete.Add(candidates[i]);
            byte[] canonicalBytes = ThirdPersonSimulation.Fixed.CharacterSimulationProgramCodec.WriteArtifact(program);
            string token = Guid.NewGuid().ToString("N");
            string temporaryPath = Path.Combine(directory, $"{Path.GetFileName(path)}.{token}.tmp");
            string backupPath = Path.Combine(directory, $"{Path.GetFileName(path)}.{token}.bak");
            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(canonicalBytes, 0, canonicalBytes.Length);
                    stream.Flush(true);
                }
                FixedLoadedArtifact staged = FixedLoader.Inspect(abilityGuid, File.ReadAllBytes(temporaryPath));
                return new GameplayAbilityTargetProgramArtifactPublishTransaction<FixedLoadedArtifact>(
                    temporaryPath,
                    path,
                    backupPath,
                    obsolete.ToArray(),
                    File.Exists(path),
                    canonicalBytes,
                    bytes => FixedLoader.Inspect(abilityGuid, bytes),
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

        static void RequireAbilityRoot(string abilityGuid, FixedProgram program)
        {
            FixedLoader.RequireDefinitionGuid(abilityGuid);
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (!program.Manifest.Root.IsAbility ||
                !string.Equals(program.Manifest.Root.RootIdentity, abilityGuid, StringComparison.Ordinal))
                throw new InvalidOperationException("Ability Fixed Target Program root identity is invalid.");
        }
    }
}
