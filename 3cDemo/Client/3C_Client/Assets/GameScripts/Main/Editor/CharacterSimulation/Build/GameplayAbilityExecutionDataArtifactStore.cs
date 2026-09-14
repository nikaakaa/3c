using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class GameplayAbilityExecutionDataArtifactPaths
    {
        public static string GetPath(
            string abilityGuid,
            NumericProfileId numericProfileId,
            TargetAbiVersion abiVersion)
        {
            RequireDefinitionGuid(abilityGuid);
            if (!numericProfileId.IsValid)
                throw new ArgumentException("Numeric Profile identity is required.", nameof(numericProfileId));
            if (!abiVersion.IsValid)
                throw new ArgumentException("Target ABI version is required.", nameof(abiVersion));
            return Path.GetFullPath(Path.Combine(
                "Library",
                "CharacterSimulation",
                "Abilities",
                abilityGuid,
                $"{numericProfileId.Value}-abi{abiVersion.Value}.ability"));
        }

        static void RequireDefinitionGuid(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
                throw new ArgumentException("Ability definition GUID is invalid.", nameof(value));
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!((c >= '0' && c <= '9') ||
                      (c >= 'a' && c <= 'f') ||
                      (c >= 'A' && c <= 'F')))
                    throw new ArgumentException("Ability definition GUID is invalid.", nameof(value));
            }
        }
    }

    public sealed class GameplayAbilityExecutionDataArtifactPublishTransaction<TData> : IDisposable
        where TData : class
    {
        readonly string m_TemporaryPath;
        readonly string m_DestinationPath;
        readonly string m_BackupPath;
        readonly string[] m_ObsoletePaths;
        readonly bool m_HadDestination;
        readonly byte[] m_CanonicalBytes;
        readonly Func<byte[], TData> m_Read;
        readonly TData m_StagedData;
        bool m_Committed;
        bool m_Completed;

        internal GameplayAbilityExecutionDataArtifactPublishTransaction(
            string temporaryPath,
            string destinationPath,
            string backupPath,
            string[] obsoletePaths,
            bool hadDestination,
            byte[] canonicalBytes,
            Func<byte[], TData> read,
            TData stagedData)
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
            m_StagedData = stagedData ?? throw new ArgumentNullException(nameof(stagedData));
        }

        public string DestinationPath => m_DestinationPath;
        public TData StagedData => m_StagedData;

        public TData Commit()
        {
            RequireOpen();
            if (m_Committed)
                throw new InvalidOperationException("Gameplay Ability execution data artifact transaction is already committed.");
            if (m_HadDestination)
                File.Replace(m_TemporaryPath, m_DestinationPath, m_BackupPath);
            else
                File.Move(m_TemporaryPath, m_DestinationPath);
            m_Committed = true;
            byte[] publishedBytes = File.ReadAllBytes(m_DestinationPath);
            if (!BytesEqual(publishedBytes, m_CanonicalBytes))
                throw new InvalidDataException("Published Gameplay Ability execution data differs from the verified staged bytes.");
            return m_Read(publishedBytes);
        }

        public void Complete()
        {
            RequireOpen();
            if (!m_Committed)
                throw new InvalidOperationException("Gameplay Ability execution data artifact transaction has not committed its bytes.");
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
                        throw new InvalidOperationException("Gameplay Ability execution data artifact backup is missing during rollback.");
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
                throw new ObjectDisposedException(nameof(GameplayAbilityExecutionDataArtifactPublishTransaction<TData>));
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

    public static class GameplayAbilityFloat32ExecutionDataArtifactStore
    {
        public static string GetPath(string abilityGuid) =>
            GameplayAbilityExecutionDataArtifactPaths.GetPath(
                abilityGuid,
                Float32SimulationNumericProfile.Value.Id,
                Float32SimulationNumericProfile.Value.AbiVersion);

        public static GameplayAbilityExecutionDataArtifactPublishTransaction<Float32GameplayAbilityExecutionData> Stage(
            string abilityGuid,
            Float32GameplayAbilityExecutionData data)
        {
            RequireAbility(abilityGuid, data);
            string path = GameplayAbilityExecutionDataArtifactPaths.GetPath(
                abilityGuid,
                data.NumericProfile.Id,
                data.NumericProfile.AbiVersion);
            byte[] canonicalBytes = Float32GameplayAbilityExecutionDataCodec.WriteArtifact(data);
            Float32GameplayAbilityExecutionDataLoadExpectation expectation = Expectation(
                abilityGuid,
                data,
                Float32GameplayAbilityExecutionDataCodec.ComputeCanonicalBytesHash(canonicalBytes));
            return Stage(
                path,
                canonicalBytes,
                bytes => Float32GameplayAbilityExecutionDataCodec.ReadArtifact(bytes, expectation));
        }

        public static Float32GameplayAbilityExecutionData Write(
            string abilityGuid,
            Float32GameplayAbilityExecutionData data)
        {
            using GameplayAbilityExecutionDataArtifactPublishTransaction<Float32GameplayAbilityExecutionData> transaction =
                Stage(abilityGuid, data);
            Float32GameplayAbilityExecutionData published = transaction.Commit();
            transaction.Complete();
            return published;
        }

        static GameplayAbilityExecutionDataArtifactPublishTransaction<Float32GameplayAbilityExecutionData> Stage(
            string path,
            byte[] canonicalBytes,
            Func<byte[], Float32GameplayAbilityExecutionData> read)
        {
            string directory = Path.GetDirectoryName(path) ??
                throw new InvalidOperationException("Gameplay Ability execution data artifact directory is unavailable.");
            Directory.CreateDirectory(directory);
            string profile = Float32SimulationNumericProfile.Value.Id.Value;
            string[] candidates = Directory.GetFiles(directory, $"{profile}-abi*.ability", SearchOption.TopDirectoryOnly);
            var obsolete = new List<string>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
                if (!string.Equals(Path.GetFullPath(candidates[i]), path, StringComparison.OrdinalIgnoreCase))
                    obsolete.Add(candidates[i]);
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
                Float32GameplayAbilityExecutionData staged = read(File.ReadAllBytes(temporaryPath));
                return new GameplayAbilityExecutionDataArtifactPublishTransaction<Float32GameplayAbilityExecutionData>(
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

        static Float32GameplayAbilityExecutionDataLoadExpectation Expectation(
            string abilityGuid,
            Float32GameplayAbilityExecutionData data,
            StableHash canonicalBytesHash) =>
            new Float32GameplayAbilityExecutionDataLoadExpectation(
                abilityGuid,
                data.AbilityId.Value,
                data.CompilerVersion,
                data.OperationSetVersion.Value,
                data.SourceRevision.Value,
                data.SemanticHash.ToString(),
                data.NumericProfile.Id.Value,
                data.NumericProfile.AbiVersion.Value,
                data.ExecutionIdentity,
                data.ContentHash.ToString(),
                data.StateSchemaHash.ToString(),
                canonicalBytesHash.ToString(),
                data.Root);

        static void RequireAbility(string abilityGuid, Float32GameplayAbilityExecutionData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.Root.IsAbility || !string.Equals(data.Root.RootIdentity, abilityGuid, StringComparison.Ordinal))
                throw new InvalidOperationException("Float32 Gameplay Ability execution data root identity is invalid.");
        }
    }

    public static class GameplayAbilityFixedExecutionDataArtifactStore
    {
        public static string GetPath(string abilityGuid) =>
            GameplayAbilityExecutionDataArtifactPaths.GetPath(
                abilityGuid,
                FixedSimulationNumericProfile.Value.Id,
                FixedSimulationNumericProfile.Value.AbiVersion);

        public static GameplayAbilityExecutionDataArtifactPublishTransaction<ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionData> Stage(
            string abilityGuid,
            ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionData data)
        {
            RequireAbility(abilityGuid, data);
            string path = GameplayAbilityExecutionDataArtifactPaths.GetPath(
                abilityGuid,
                data.NumericProfile.Id,
                data.NumericProfile.AbiVersion);
            byte[] canonicalBytes = ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionDataCodec.WriteArtifact(data);
            ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionDataLoadExpectation expectation = Expectation(
                abilityGuid,
                data,
                ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionDataCodec.ComputeCanonicalBytesHash(canonicalBytes));
            return Stage(
                path,
                canonicalBytes,
                bytes => ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionDataCodec.ReadArtifact(bytes, expectation));
        }

        public static ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionData Write(
            string abilityGuid,
            ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionData data)
        {
            using GameplayAbilityExecutionDataArtifactPublishTransaction<ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionData> transaction =
                Stage(abilityGuid, data);
            ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionData published = transaction.Commit();
            transaction.Complete();
            return published;
        }

        static GameplayAbilityExecutionDataArtifactPublishTransaction<ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionData> Stage(
            string path,
            byte[] canonicalBytes,
            Func<byte[], ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionData> read)
        {
            string directory = Path.GetDirectoryName(path) ??
                throw new InvalidOperationException("Gameplay Ability execution data artifact directory is unavailable.");
            Directory.CreateDirectory(directory);
            string profile = FixedSimulationNumericProfile.Value.Id.Value;
            string[] candidates = Directory.GetFiles(directory, $"{profile}-abi*.ability", SearchOption.TopDirectoryOnly);
            var obsolete = new List<string>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
                if (!string.Equals(Path.GetFullPath(candidates[i]), path, StringComparison.OrdinalIgnoreCase))
                    obsolete.Add(candidates[i]);
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
                ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionData staged = read(File.ReadAllBytes(temporaryPath));
                return new GameplayAbilityExecutionDataArtifactPublishTransaction<ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionData>(
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

        static ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionDataLoadExpectation Expectation(
            string abilityGuid,
            ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionData data,
            StableHash canonicalBytesHash) =>
            new ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionDataLoadExpectation(
                abilityGuid,
                data.AbilityId.Value,
                data.CompilerVersion,
                data.OperationSetVersion.Value,
                data.SourceRevision.Value,
                data.SemanticHash.ToString(),
                data.NumericProfile.Id.Value,
                data.NumericProfile.AbiVersion.Value,
                data.ExecutionIdentity,
                data.ContentHash.ToString(),
                data.StateSchemaHash.ToString(),
                canonicalBytesHash.ToString(),
                data.Root);

        static void RequireAbility(
            string abilityGuid,
            ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.Root.IsAbility || !string.Equals(data.Root.RootIdentity, abilityGuid, StringComparison.Ordinal))
                throw new InvalidOperationException("Fixed Gameplay Ability execution data root identity is invalid.");
        }
    }
}
