using System;
using System.IO;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonSimulation;
using UnityEditor;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    public sealed class CharacterMotionMatchingExpectedArtifactIdentity
    {
        readonly MotionMatchingClipDependencyIdentity[] m_Dependencies;

        public CharacterMotionMatchingExpectedArtifactIdentity(
            int artifactSchemaVersion,
            string algorithmVersion,
            CharacterMotionMatchingDatabaseId databaseId,
            int databaseRevision,
            CharacterMotionMatchingFeatureSchemaId featureSchemaId,
            int featureSchemaRevision,
            string rigId,
            string rigRevision,
            MotionMatchingClipDependencyIdentity[] dependencies,
            StableHash orderedDependencyHash)
        {
            if (artifactSchemaVersion <= 0 || string.IsNullOrWhiteSpace(algorithmVersion) || !databaseId.IsValid || databaseRevision <= 0 ||
                !featureSchemaId.IsValid || featureSchemaRevision <= 0 || string.IsNullOrWhiteSpace(rigId) || string.IsNullOrWhiteSpace(rigRevision) ||
                dependencies == null || dependencies.Length == 0 || !orderedDependencyHash.IsValid)
                throw new ArgumentException("Expected Motion Matching Artifact identity is incomplete.");
            m_Dependencies = (MotionMatchingClipDependencyIdentity[])dependencies.Clone();
            for (int i = 1; i < m_Dependencies.Length; i++)
            {
                if (m_Dependencies[i - 1].SourceClipId.CompareTo(m_Dependencies[i].SourceClipId) >= 0)
                    throw new ArgumentException("Expected Motion Matching Clip dependencies are not in strict SourceClipId order.", nameof(dependencies));
            }
            ArtifactSchemaVersion = artifactSchemaVersion;
            AlgorithmVersion = algorithmVersion;
            DatabaseId = databaseId;
            DatabaseRevision = databaseRevision;
            FeatureSchemaId = featureSchemaId;
            FeatureSchemaRevision = featureSchemaRevision;
            RigId = rigId;
            RigRevision = rigRevision;
            OrderedDependencyHash = orderedDependencyHash;
        }

        public int ArtifactSchemaVersion { get; }
        public string AlgorithmVersion { get; }
        public CharacterMotionMatchingDatabaseId DatabaseId { get; }
        public int DatabaseRevision { get; }
        public CharacterMotionMatchingFeatureSchemaId FeatureSchemaId { get; }
        public int FeatureSchemaRevision { get; }
        public string RigId { get; }
        public string RigRevision { get; }
        public int DependencyCount => m_Dependencies.Length;
        public StableHash OrderedDependencyHash { get; }
        public MotionMatchingClipDependencyIdentity GetDependency(int index) => m_Dependencies[index];

        public bool Matches(CharacterMotionMatchingDatabaseArtifactIdentity actual)
        {
            if (actual == null || ArtifactSchemaVersion != actual.ArtifactSchemaVersion || DatabaseRevision != actual.DatabaseRevision ||
                FeatureSchemaRevision != actual.FeatureSchemaRevision || DependencyCount != actual.ClipDependencyCount ||
                !DatabaseId.Equals(actual.DatabaseId) || !FeatureSchemaId.Equals(actual.FeatureSchemaId) ||
                !string.Equals(AlgorithmVersion, actual.AnalysisAlgorithmVersion, StringComparison.Ordinal) ||
                !string.Equals(RigId, actual.RigId, StringComparison.Ordinal) || !string.Equals(RigRevision, actual.RigRevision, StringComparison.Ordinal) ||
                !OrderedDependencyHash.Equals(actual.OrderedClipDependencyHash))
                return false;
            for (int i = 0; i < DependencyCount; i++)
            {
                MotionMatchingClipDependencyIdentity expected = GetDependency(i);
                MotionMatchingClipDependencyIdentity value = actual.GetClipDependency(i);
                if (!expected.SourceSetId.Equals(value.SourceSetId) || expected.SourceSetRevision != value.SourceSetRevision ||
                    !expected.SourceClipId.Equals(value.SourceClipId) || !string.Equals(expected.AssetGuid, value.AssetGuid, StringComparison.Ordinal) ||
                    expected.LocalFileId != value.LocalFileId || !string.Equals(expected.ImportDependencyHash, value.ImportDependencyHash, StringComparison.Ordinal) ||
                    !string.Equals(expected.SamplingRigSignature, value.SamplingRigSignature, StringComparison.Ordinal) ||
                    !expected.MotionRootBoneId.Equals(value.MotionRootBoneId) || !expected.FootArtifactHash.Equals(value.FootArtifactHash))
                    return false;
            }
            return true;
        }
    }

    public static class CharacterMotionMatchingDatabaseArtifactStore
    {
        public static string RootPath => Path.GetFullPath(Path.Combine("Library", "CharacterSimulation", "Analysis", "MotionMatching"));

        public static string GetPath(CharacterMotionMatchingDatabaseDefinition database)
        {
            if (!database)
                throw new ArgumentNullException(nameof(database));
            string assetPath = AssetDatabase.GetAssetPath(database);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException("Motion Matching Database Definition must be a persisted asset.");
            return Path.Combine(RootPath, guid + ".mmdb");
        }

        public static CharacterMotionMatchingArtifactInspection Inspect(
            CharacterMotionMatchingDatabaseDefinition database,
            CharacterMotionMatchingExpectedArtifactIdentity expected)
        {
            if (expected == null)
                throw new ArgumentNullException(nameof(expected));
            string path = GetPath(database);
            if (!File.Exists(path))
                return new CharacterMotionMatchingArtifactInspection(CharacterMotionMatchingArtifactStatus.Missing, path, null, string.Empty);
            try
            {
                CharacterMotionMatchingDatabaseArtifact artifact = CharacterMotionMatchingDatabaseArtifactCodec.Read(File.ReadAllBytes(path));
                if (!expected.Matches(artifact.Identity))
                    return new CharacterMotionMatchingArtifactInspection(CharacterMotionMatchingArtifactStatus.Stale, path, artifact, "Artifact input identity no longer matches current authoring or dependencies.");
                return new CharacterMotionMatchingArtifactInspection(CharacterMotionMatchingArtifactStatus.Ready, path, artifact, string.Empty);
            }
            catch (Exception exception)
            {
                return new CharacterMotionMatchingArtifactInspection(CharacterMotionMatchingArtifactStatus.Invalid, path, null, exception.Message);
            }
        }

        public static CharacterMotionMatchingDatabaseArtifact Publish(
            CharacterMotionMatchingDatabaseDefinition database,
            CharacterMotionMatchingDatabaseArtifact artifact,
            string candidatePath)
        {
            if (!database)
                throw new ArgumentNullException(nameof(database));
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            if (string.IsNullOrWhiteSpace(candidatePath))
                throw new ArgumentException("Motion Matching Artifact candidate path is missing.", nameof(candidatePath));
            database.RequireValid();
            if (!artifact.Identity.DatabaseId.Equals(database.DatabaseId) || artifact.Identity.DatabaseRevision != database.Revision ||
                !artifact.SearchDomainId.Equals(database.SearchDomainId) || artifact.SampleRate != database.SampleRate ||
                !artifact.Identity.FeatureSchemaId.Equals(database.FeatureSchema.FeatureSchemaId) ||
                artifact.Identity.FeatureSchemaRevision != database.FeatureSchema.Revision ||
                !string.Equals(artifact.Identity.RigId, database.TargetRig.RigId, StringComparison.Ordinal) ||
                !string.Equals(artifact.Identity.RigRevision, database.TargetRig.Revision, StringComparison.Ordinal))
                throw new InvalidOperationException("Motion Matching Artifact identity does not match the target Database Definition.");
            string path = GetPath(database);
            string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Motion Matching Artifact path has no directory.");
            Directory.CreateDirectory(directory);
            byte[] bytes = CharacterMotionMatchingDatabaseArtifactCodec.Write(artifact);
            try
            {
                File.WriteAllBytes(candidatePath, bytes);
                CharacterMotionMatchingDatabaseArtifact verified = CharacterMotionMatchingDatabaseArtifactCodec.Read(File.ReadAllBytes(candidatePath));
                if (!verified.Identity.EqualsExact(artifact.Identity))
                    throw new InvalidDataException("Motion Matching Artifact candidate does not round-trip exactly.");
                if (File.Exists(path))
                    File.Replace(candidatePath, path, null);
                else
                    File.Move(candidatePath, path);
                CharacterMotionMatchingDatabaseArtifact published = CharacterMotionMatchingDatabaseArtifactCodec.Read(File.ReadAllBytes(path));
                if (!published.Identity.EqualsExact(artifact.Identity))
                    throw new InvalidDataException("Published Motion Matching Artifact differs from its verified candidate.");
                return published;
            }
            finally
            {
                if (File.Exists(candidatePath))
                    File.Delete(candidatePath);
            }
        }
    }
}
