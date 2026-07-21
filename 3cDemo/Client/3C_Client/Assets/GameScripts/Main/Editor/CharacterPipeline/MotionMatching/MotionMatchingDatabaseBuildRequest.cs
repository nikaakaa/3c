using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    public sealed class MotionMatchingResolvedClipBuildInput
    {
        public MotionMatchingResolvedClipBuildInput(
            int clipBindingIndex,
            CharacterMotionMatchingSourceSetId sourceSetId,
            int sourceSetRevision,
            MotionMatchingSamplingCompatibilityMode compatibilityMode,
            CharacterMotionMatchingSourceClipId sourceClipId,
            string assetGuid,
            long localFileId,
            string dependencyHash,
            string samplingRigSignature,
            AnimationBoneId motionRootBoneId,
            AnimationClip clip,
            AnimationFootAnalysisArtifact footArtifact)
        {
            ClipBindingIndex = clipBindingIndex;
            SourceSetId = sourceSetId;
            SourceSetRevision = sourceSetRevision;
            CompatibilityMode = compatibilityMode;
            SourceClipId = sourceClipId;
            AssetGuid = assetGuid;
            LocalFileId = localFileId;
            DependencyHash = dependencyHash;
            SamplingRigSignature = samplingRigSignature;
            MotionRootBoneId = motionRootBoneId;
            Clip = clip;
            FootArtifact = footArtifact;
        }

        public int ClipBindingIndex { get; }
        public CharacterMotionMatchingSourceSetId SourceSetId { get; }
        public int SourceSetRevision { get; }
        public MotionMatchingSamplingCompatibilityMode CompatibilityMode { get; }
        public CharacterMotionMatchingSourceClipId SourceClipId { get; }
        public string AssetGuid { get; }
        public long LocalFileId { get; }
        public string DependencyHash { get; }
        public string SamplingRigSignature { get; }
        public AnimationBoneId MotionRootBoneId { get; }
        public AnimationClip Clip { get; }
        public AnimationFootAnalysisArtifact FootArtifact { get; }
        public MotionMatchingClipDependencyIdentity DependencyIdentity => new MotionMatchingClipDependencyIdentity(
            SourceSetId, SourceSetRevision, SourceClipId, AssetGuid, LocalFileId, DependencyHash,
            SamplingRigSignature, MotionRootBoneId, FootArtifact.ContentHash);
    }

    public readonly struct MotionMatchingSegmentBuildInput
    {
        public MotionMatchingSegmentBuildInput(
            CharacterMotionMatchingSegmentId segmentId,
            CharacterMotionMatchingSourceClipId sourceClipId,
            int clipBindingIndex,
            float startTime,
            float endTime,
            MotionMatchingSegmentLoopMode loopMode,
            bool canInitialize,
            bool canJumpInto,
            float entryExclusion,
            float exitExclusion,
            CharacterMotionMatchingSegmentId continuationTarget,
            bool terminal)
        {
            SegmentId = segmentId;
            SourceClipId = sourceClipId;
            ClipBindingIndex = clipBindingIndex;
            StartTime = startTime;
            EndTime = endTime;
            LoopMode = loopMode;
            CanInitialize = canInitialize;
            CanJumpInto = canJumpInto;
            EntryExclusion = entryExclusion;
            ExitExclusion = exitExclusion;
            ContinuationTarget = continuationTarget;
            Terminal = terminal;
        }
        public CharacterMotionMatchingSegmentId SegmentId { get; }
        public CharacterMotionMatchingSourceClipId SourceClipId { get; }
        public int ClipBindingIndex { get; }
        public float StartTime { get; }
        public float EndTime { get; }
        public MotionMatchingSegmentLoopMode LoopMode { get; }
        public bool CanInitialize { get; }
        public bool CanJumpInto { get; }
        public float EntryExclusion { get; }
        public float ExitExclusion { get; }
        public CharacterMotionMatchingSegmentId ContinuationTarget { get; }
        public bool Terminal { get; }
    }

    public sealed class MotionMatchingCoverageBuildInput
    {
        readonly MotionMatchingFootContactMask[] m_Contacts;
        public MotionMatchingCoverageBuildInput(CharacterMotionMatchingCoverageRequirement source)
        {
            source.RequireValid();
            RequirementId = source.RequirementId;
            MinimumSpeed = source.MinimumSpeed;
            MaximumSpeed = source.MaximumSpeed;
            MinimumFacingChangeDegrees = source.MinimumFacingChangeDegrees;
            MaximumFacingChangeDegrees = source.MaximumFacingChangeDegrees;
            RequireInitialization = source.RequireInitialization;
            MinimumPlanHorizon = source.MinimumPlanHorizon;
            m_Contacts = new MotionMatchingFootContactMask[source.RequiredContactCombinations.Count];
            for (int i = 0; i < m_Contacts.Length; i++)
                m_Contacts[i] = source.RequiredContactCombinations[i];
        }
        public string RequirementId { get; }
        public float MinimumSpeed { get; }
        public float MaximumSpeed { get; }
        public float MinimumFacingChangeDegrees { get; }
        public float MaximumFacingChangeDegrees { get; }
        public bool RequireInitialization { get; }
        public float MinimumPlanHorizon { get; }
        public int ContactCount => m_Contacts.Length;
        public MotionMatchingFootContactMask GetContact(int index) => m_Contacts[index];
    }

    public sealed class MotionMatchingDatabaseBuildRequest
    {
        readonly MotionMatchingResolvedClipBuildInput[] m_Clips;
        readonly MotionMatchingSegmentBuildInput[] m_Segments;
        readonly MotionMatchingCoverageBuildInput[] m_Coverage;

        public MotionMatchingDatabaseBuildRequest(
            CharacterMotionMatchingProfile profile,
            CharacterMotionMatchingDatabaseDefinition database,
            CharacterFootPlacementAnalysisSource analysisSource,
            GameObject samplingRigPrefab,
            CharacterAnimationRigBinding samplingRigBinding,
            MotionMatchingFeatureSchemaPayload featureSchema,
            MotionMatchingTrajectoryPolicyPayload trajectoryPolicy,
            MotionMatchingCostProfilePayload costProfile,
            MotionMatchingSearchPolicyPayload searchPolicy,
            MotionMatchingResolvedClipBuildInput[] clips,
            MotionMatchingSegmentBuildInput[] segments,
            MotionMatchingCoverageBuildInput[] coverage,
            CharacterMotionMatchingExpectedArtifactIdentity expectedIdentity,
            StableHash inputSnapshotHash,
            int estimatedSampleCount,
            long memoryUpperBoundBytes,
            string candidatePath)
        {
            Profile = profile;
            Database = database;
            AnalysisSource = analysisSource;
            SamplingRigPrefab = samplingRigPrefab;
            SamplingRigBinding = samplingRigBinding;
            FeatureSchema = featureSchema;
            TrajectoryPolicy = trajectoryPolicy;
            CostProfile = costProfile;
            SearchPolicy = searchPolicy;
            m_Clips = clips;
            m_Segments = segments;
            m_Coverage = coverage;
            ExpectedIdentity = expectedIdentity;
            InputSnapshotHash = inputSnapshotHash;
            EstimatedSampleCount = estimatedSampleCount;
            MemoryUpperBoundBytes = memoryUpperBoundBytes;
            CandidatePath = candidatePath;
        }

        public CharacterMotionMatchingProfile Profile { get; }
        public CharacterMotionMatchingDatabaseDefinition Database { get; }
        public CharacterFootPlacementAnalysisSource AnalysisSource { get; }
        public GameObject SamplingRigPrefab { get; }
        public CharacterAnimationRigBinding SamplingRigBinding { get; }
        public MotionMatchingFeatureSchemaPayload FeatureSchema { get; }
        public MotionMatchingTrajectoryPolicyPayload TrajectoryPolicy { get; }
        public MotionMatchingCostProfilePayload CostProfile { get; }
        public MotionMatchingSearchPolicyPayload SearchPolicy { get; }
        public CharacterMotionMatchingExpectedArtifactIdentity ExpectedIdentity { get; }
        public StableHash InputSnapshotHash { get; }
        public int EstimatedSampleCount { get; }
        public long MemoryUpperBoundBytes { get; }
        public string CandidatePath { get; }
        public int ClipCount => m_Clips.Length;
        public int SegmentCount => m_Segments.Length;
        public int CoverageCount => m_Coverage.Length;
        public MotionMatchingResolvedClipBuildInput GetClip(int index) => m_Clips[index];
        public MotionMatchingSegmentBuildInput GetSegment(int index) => m_Segments[index];
        public MotionMatchingCoverageBuildInput GetCoverage(int index) => m_Coverage[index];
    }

    public static class MotionMatchingDatabaseBuildRequestFactory
    {
        public const int ArtifactSchemaVersion = 1;
        public const string AlgorithmVersion = "character-motion-matching-analysis/v1";

        public static MotionMatchingDatabaseBuildRequest Create(
            CharacterMotionMatchingProfile profile,
            CharacterMotionMatchingDatabaseDefinition database,
            CharacterFootPlacementAnalysisSource analysisSource)
        {
            CharacterMotionMatchingAuthoringValidator.RequireDatabase(profile, database);
            if (!analysisSource)
                throw new ArgumentNullException(nameof(analysisSource));
            analysisSource.RequireValid();
            string rigPath = AssetDatabase.GUIDToAssetPath(analysisSource.SamplingRigAssetGuid);
            GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            if (!rigPrefab)
                throw new InvalidOperationException($"Sampling Rig GUID '{analysisSource.SamplingRigAssetGuid}' does not resolve to a Prefab.");
            CharacterAnimationRigBinding rigBinding = rigPrefab.GetComponentInChildren<CharacterAnimationRigBinding>(true);
            if (!rigBinding || !rigBinding.Animator || rigBinding.Bones.Count != database.TargetRig.Bones.Count ||
                !string.Equals(rigBinding.RigId, database.TargetRig.RigId, StringComparison.Ordinal) ||
                !string.Equals(rigBinding.RigRevision, database.TargetRig.Revision, StringComparison.Ordinal))
                throw new InvalidOperationException("Sampling Rig animation binding does not match the Database Target Rig exact identity.");

            MotionMatchingFeatureSchemaPayload featureSchema = MotionMatchingAuthoringPayloadCompiler.CompileFeatureSchema(profile.FeatureSchema, profile.TrajectoryPolicy);
            MotionMatchingTrajectoryPolicyPayload trajectory = MotionMatchingAuthoringPayloadCompiler.CompileTrajectoryPolicy(profile.TrajectoryPolicy);
            MotionMatchingCostProfilePayload cost = MotionMatchingAuthoringPayloadCompiler.CompileCostProfile(profile.CostProfile, featureSchema);
            MotionMatchingSearchPolicyPayload search = MotionMatchingAuthoringPayloadCompiler.CompileSearchPolicy(profile.SearchPolicy);
            string targetHierarchySignature = ComputeTargetHierarchySignature(rigBinding);
            var clips = ResolveClips(database, analysisSource, rigBinding, targetHierarchySignature);
            var clipIndex = clips.ToDictionary(value => value.SourceClipId, value => value.ClipBindingIndex);
            CharacterMotionMatchingSegmentDefinition[] orderedSegments = database.Segments.OrderBy(value => value.SegmentId).ToArray();
            var segments = new MotionMatchingSegmentBuildInput[orderedSegments.Length];
            int sampleCount = 0;
            for (int i = 0; i < segments.Length; i++)
            {
                CharacterMotionMatchingSegmentDefinition source = orderedSegments[i];
                MotionMatchingResolvedClipBuildInput clip = clips[clipIndex[source.SourceClipId]];
                if (source.EndTime > clip.Clip.length + 0.00001f)
                    throw new InvalidOperationException($"Segment '{source.SegmentId}' range exceeds Clip '{clip.Clip.name}' length.");
                segments[i] = new MotionMatchingSegmentBuildInput(
                    source.SegmentId, source.SourceClipId, clip.ClipBindingIndex, source.StartTime, source.EndTime,
                    source.LoopMode, source.CanInitialize, source.CanJumpInto, source.EntryExclusion, source.ExitExclusion,
                    source.ContinuationTargetSegmentId, source.Terminal);
                sampleCount += Mathf.CeilToInt((source.EndTime - source.StartTime) * database.SampleRate) + 1;
            }
            if (sampleCount > search.MaximumAdmittedSampleCount)
                throw new InvalidOperationException($"Database estimated sample count {sampleCount} exceeds Search Policy maximum {search.MaximumAdmittedSampleCount}.");
            var coverage = new MotionMatchingCoverageBuildInput[database.CoverageRequirements.Count];
            for (int i = 0; i < coverage.Length; i++)
                coverage[i] = new MotionMatchingCoverageBuildInput(database.CoverageRequirements[i]);
            MotionMatchingClipDependencyIdentity[] dependencies = clips.Select(value => value.DependencyIdentity).ToArray();
            StableHash dependencyHash = StableHash.Compute(dependencies.Select(DependencyKey).ToArray());
            var expected = new CharacterMotionMatchingExpectedArtifactIdentity(
                ArtifactSchemaVersion, AlgorithmVersion, database.DatabaseId, database.Revision,
                profile.FeatureSchema.FeatureSchemaId, profile.FeatureSchema.Revision,
                database.TargetRig.RigId, database.TargetRig.Revision, dependencies, dependencyHash);
            StableHash snapshot = ComputeInputSnapshot(profile, database, analysisSource, clips, dependencyHash);
            long memoryUpperBound = (long)sampleCount * featureSchema.DenseFeatureCount * sizeof(float) * 4L +
                                    (long)sampleCount * 1024L + (long)sampleCount * search.PlanSampleCount * sizeof(int);
            string finalPath = CharacterMotionMatchingDatabaseArtifactStore.GetPath(database);
            string candidatePath = finalPath + "." + Guid.NewGuid().ToString("N") + ".candidate";
            return new MotionMatchingDatabaseBuildRequest(
                profile, database, analysisSource, rigPrefab, rigBinding, featureSchema, trajectory, cost, search,
                clips, segments, coverage, expected, snapshot, sampleCount, memoryUpperBound, candidatePath);
        }

        static MotionMatchingResolvedClipBuildInput[] ResolveClips(
            CharacterMotionMatchingDatabaseDefinition database,
            CharacterFootPlacementAnalysisSource analysisSource,
            CharacterAnimationRigBinding rigBinding,
            string targetHierarchySignature)
        {
            var values = new List<MotionMatchingResolvedClipBuildInput>();
            for (int sourceSetIndex = 0; sourceSetIndex < database.SourceSets.Count; sourceSetIndex++)
            {
                CharacterMotionMatchingSourceSet sourceSet = database.SourceSets[sourceSetIndex];
                for (int clipIndex = 0; clipIndex < sourceSet.SourceClips.Count; clipIndex++)
                {
                    CharacterMotionMatchingSourceClipEntry entry = sourceSet.SourceClips[clipIndex];
                    AnimationClip clip = MotionMatchingSourceClipResolver.Resolve(entry);
                    string path = AssetDatabase.GetAssetPath(clip);
                    string dependencyHash = AssetDatabase.GetAssetDependencyHash(path).ToString();
                    RequireCompatibility(sourceSet.SamplingCompatibilityMode, clip, rigBinding, targetHierarchySignature);
                    string samplingSignature = StableHash.Compute(
                        sourceSet.SamplingCompatibilityMode.ToString(), targetHierarchySignature, dependencyHash).Value;
                    AnimationFootAnalysisArtifactInspection inspection = AnimationFootAnalysisArtifactBuilder.Inspect(clip, analysisSource);
                    if (inspection.Status != AnimationFootAnalysisArtifactStatus.Ready || inspection.Artifact == null)
                        throw new InvalidOperationException($"Clip '{entry.SourceClipId}' Foot Analysis Artifact is {inspection.Status}; run explicit Build Source Set Foot Analysis first.");
                    values.Add(new MotionMatchingResolvedClipBuildInput(
                        0, sourceSet.SourceSetId, sourceSet.Revision, sourceSet.SamplingCompatibilityMode,
                        entry.SourceClipId, entry.AnimationClipAssetGuid, entry.AnimationClipLocalFileId,
                        dependencyHash, samplingSignature, sourceSet.MotionRootBoneId, clip, inspection.Artifact));
                }
            }
            values.Sort((left, right) => left.SourceClipId.CompareTo(right.SourceClipId));
            var result = new MotionMatchingResolvedClipBuildInput[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                MotionMatchingResolvedClipBuildInput source = values[i];
                result[i] = new MotionMatchingResolvedClipBuildInput(
                    i, source.SourceSetId, source.SourceSetRevision, source.CompatibilityMode, source.SourceClipId,
                    source.AssetGuid, source.LocalFileId, source.DependencyHash, source.SamplingRigSignature,
                    source.MotionRootBoneId, source.Clip, source.FootArtifact);
            }
            return result;
        }

        static void RequireCompatibility(
            MotionMatchingSamplingCompatibilityMode mode,
            AnimationClip clip,
            CharacterAnimationRigBinding rigBinding,
            string targetHierarchySignature)
        {
            if (mode == MotionMatchingSamplingCompatibilityMode.HumanoidRetargeted)
            {
                if (!clip.humanMotion || !rigBinding.Animator.isHuman || !rigBinding.Animator.avatar || !rigBinding.Animator.avatar.isValid || !rigBinding.Animator.avatar.isHuman)
                    throw new InvalidOperationException($"HumanoidRetargeted Clip '{clip.name}' or Target Sampling Rig has no valid Humanoid Avatar.");
                return;
            }
            if (mode != MotionMatchingSamplingCompatibilityMode.ExactGenericRig)
                throw new InvalidOperationException("Motion Source Set has no explicit Sampling Compatibility Mode.");
            var paths = new HashSet<string>(AnimationUtility.GetCurveBindings(clip).Select(binding => binding.path), StringComparer.Ordinal);
            for (int i = 0; i < rigBinding.Bones.Count; i++)
            {
                string path = AnimationUtility.CalculateTransformPath(rigBinding.Bones[i], rigBinding.Animator.transform);
                if (!string.IsNullOrEmpty(path) && !paths.Contains(path))
                    throw new InvalidOperationException($"ExactGenericRig Clip '{clip.name}' does not contain required hierarchy path '{path}' from signature '{targetHierarchySignature}'.");
            }
        }

        static string ComputeTargetHierarchySignature(CharacterAnimationRigBinding binding)
        {
            var parts = new string[binding.Bones.Count + 2];
            parts[0] = binding.RigId;
            parts[1] = binding.RigRevision;
            for (int i = 0; i < binding.Bones.Count; i++)
                parts[i + 2] = AnimationUtility.CalculateTransformPath(binding.Bones[i], binding.Animator.transform);
            return StableHash.Compute(parts).Value;
        }

        static StableHash ComputeInputSnapshot(
            CharacterMotionMatchingProfile profile,
            CharacterMotionMatchingDatabaseDefinition database,
            CharacterFootPlacementAnalysisSource analysisSource,
            MotionMatchingResolvedClipBuildInput[] clips,
            StableHash dependencyHash)
        {
            var parts = new List<string>
            {
                DependencyHash(profile), DependencyHash(profile.FeatureSchema), DependencyHash(profile.TrajectoryPolicy),
                DependencyHash(profile.CostProfile), DependencyHash(profile.SearchPolicy), DependencyHash(database),
                DependencyHash(database.TargetRig), DependencyHash(analysisSource), dependencyHash.Value
            };
            for (int i = 0; i < clips.Length; i++)
                parts.Add(clips[i].DependencyHash);
            return StableHash.Compute(parts.ToArray());
        }

        static string DependencyHash(UnityEngine.Object value)
        {
            string path = AssetDatabase.GetAssetPath(value);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException($"Motion Matching build input '{value}' is not a persisted asset.");
            return AssetDatabase.GetAssetDependencyHash(path).ToString();
        }

        static string DependencyKey(MotionMatchingClipDependencyIdentity value) => string.Join("|",
            value.SourceSetId.Value,
            value.SourceSetRevision.ToString(CultureInfo.InvariantCulture),
            value.SourceClipId.Value,
            value.AssetGuid,
            value.LocalFileId.ToString(CultureInfo.InvariantCulture),
            value.ImportDependencyHash,
            value.SamplingRigSignature,
            value.MotionRootBoneId.Value,
            value.FootArtifactHash.Value);
    }
}
