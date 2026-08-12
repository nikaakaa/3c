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
        public const int ArtifactSchemaVersion = 4;
        public const string AlgorithmVersion = "character-motion-matching-analysis/v2";

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
            if (!rigBinding || !rigBinding.Animator || rigBinding.PhysicalBones.Count != database.TargetRig.PhysicalBoneCount ||
                !string.Equals(rigBinding.RigId, database.TargetRig.RigId, StringComparison.Ordinal) ||
                !string.Equals(rigBinding.RigRevision, database.TargetRig.Revision, StringComparison.Ordinal))
                throw new InvalidOperationException("Sampling Rig animation binding does not match the Database Target Rig exact identity.");

            MotionMatchingFeatureSchemaPayload featureSchema = MotionMatchingAuthoringPayloadCompiler.CompileFeatureSchema(profile.FeatureSchema, profile.TrajectoryPolicy);
            MotionMatchingTrajectoryPolicyPayload trajectory = MotionMatchingAuthoringPayloadCompiler.CompileTrajectoryPolicy(profile.TrajectoryPolicy);
            MotionMatchingCostProfilePayload cost = MotionMatchingAuthoringPayloadCompiler.CompileCostProfile(profile.CostProfile, featureSchema);
            MotionMatchingSearchPolicyPayload search = MotionMatchingAuthoringPayloadCompiler.CompileSearchPolicy(profile.SearchPolicy);
            var clips = ResolveClips(database, analysisSource, rigBinding);
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
                sampleCount += Mathf.Max(1, Mathf.CeilToInt((source.EndTime - source.StartTime) * database.SampleRate));
            }
            if (sampleCount < search.TopK)
                throw new InvalidOperationException($"Database estimated sample count {sampleCount} is less than Search Policy TopK {search.TopK}.");
            if (sampleCount > search.MaximumAdmittedSampleCount)
                throw new InvalidOperationException($"Database estimated sample count {sampleCount} exceeds Search Policy maximum {search.MaximumAdmittedSampleCount}.");
            var coverage = new MotionMatchingCoverageBuildInput[database.CoverageRequirements.Count];
            for (int i = 0; i < coverage.Length; i++)
                coverage[i] = new MotionMatchingCoverageBuildInput(database.CoverageRequirements[i]);
            MotionMatchingClipDependencyIdentity[] dependencies = clips.Select(value => value.DependencyIdentity).ToArray();
            StableHash dependencyHash = StableHash.Compute(dependencies.Select(DependencyKey).ToArray());
            StableHash snapshot = ComputeInputSnapshot(profile, database, analysisSource, clips, dependencyHash);
            var expected = new CharacterMotionMatchingExpectedArtifactIdentity(
                ArtifactSchemaVersion, AlgorithmVersion, database.DatabaseId, database.Revision,
                profile.FeatureSchema.FeatureSchemaId, profile.FeatureSchema.Revision,
                database.TargetRig.RigId, database.TargetRig.Revision, dependencies, snapshot, dependencyHash);
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
            CharacterAnimationRigBinding rigBinding)
        {
            var values = new List<MotionMatchingResolvedClipBuildInput>();
            for (int sourceSetIndex = 0; sourceSetIndex < database.SourceSets.Count; sourceSetIndex++)
            {
                CharacterMotionMatchingSourceSet sourceSet = database.SourceSets[sourceSetIndex];
                for (int clipIndex = 0; clipIndex < sourceSet.SourceClips.Count; clipIndex++)
                {
                    CharacterMotionMatchingSourceClipEntry entry = sourceSet.SourceClips[clipIndex];
                    MotionMatchingSourceClipInspection sourceInspection = MotionMatchingSourceClipResolver.Inspect(
                        entry, sourceSet.SamplingCompatibilityMode);
                    if (!sourceInspection.HasFormalBuildPrerequisites)
                        throw new InvalidOperationException(
                            $"Clip '{entry.SourceClipId}' source inspection is {sourceInspection.Status}: {sourceInspection.Diagnostic}");
                    AnimationClip clip = sourceInspection.Clip;
                    string path = AssetDatabase.GetAssetPath(clip);
                    string dependencyHash = AssetDatabase.GetAssetDependencyHash(path).ToString();
                    string samplingSignature = RequireSamplingRigSignature(
                        sourceSet, database.TargetRig, rigBinding, sourceInspection);
                    AnimationFootAnalysisArtifactInspection footInspection = AnimationFootAnalysisArtifactBuilder.Inspect(clip, analysisSource);
                    if (footInspection.Status != AnimationFootAnalysisArtifactStatus.Ready || footInspection.Artifact == null)
                        throw new InvalidOperationException($"Clip '{entry.SourceClipId}' Foot Analysis Artifact is {footInspection.Status}; run explicit Build Source Set Foot Analysis first.");
                    values.Add(new MotionMatchingResolvedClipBuildInput(
                        0, sourceSet.SourceSetId, sourceSet.Revision, sourceSet.SamplingCompatibilityMode,
                        entry.SourceClipId, entry.AnimationClipAssetGuid, entry.AnimationClipLocalFileId,
                        dependencyHash, samplingSignature, sourceSet.MotionRootBoneId, clip, footInspection.Artifact));
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

        static string RequireSamplingRigSignature(
            CharacterMotionMatchingSourceSet sourceSet,
            CharacterAnimationRigDefinition targetRig,
            CharacterAnimationRigBinding rigBinding,
            MotionMatchingSourceClipInspection sourceInspection)
        {
            if (sourceSet.SamplingCompatibilityMode == MotionMatchingSamplingCompatibilityMode.HumanoidRetargeted)
                return RequireHumanoidSamplingRigSignature(targetRig, rigBinding, sourceInspection);
            if (sourceSet.SamplingCompatibilityMode == MotionMatchingSamplingCompatibilityMode.ExactGenericRig)
                return RequireGenericSamplingRigSignature(sourceSet, targetRig, rigBinding, sourceInspection);
            throw new InvalidOperationException("Motion Source Set has no explicit Sampling Compatibility Mode.");
        }

        static string RequireHumanoidSamplingRigSignature(
            CharacterAnimationRigDefinition targetRig,
            CharacterAnimationRigBinding rigBinding,
            MotionMatchingSourceClipInspection sourceInspection)
        {
            if (!sourceInspection.CompatibilityDeclared || !sourceInspection.SourceAvatarIdentityAvailable)
                throw new InvalidOperationException(
                    $"HumanoidRetargeted Clip '{sourceInspection.SourceClipId}' has no formally compatible source Avatar identity.");
            Animator animator = rigBinding.Animator;
            Avatar targetAvatar = animator.avatar;
            if (!animator.isHuman || !targetAvatar || !targetAvatar.isValid || !targetAvatar.isHuman)
                throw new InvalidOperationException("HumanoidRetargeted Target Sampling Rig has no valid Humanoid Avatar.");
            string targetAvatarIdentity = RequireStableAssetIdentity(targetAvatar, "Target Sampling Rig Avatar");
            return StableHash.Compute(
                "motion-matching-sampling-rig/humanoid/v1",
                ((byte)MotionMatchingSamplingCompatibilityMode.HumanoidRetargeted).ToString(CultureInfo.InvariantCulture),
                sourceInspection.SourceAvatarIdentity,
                targetAvatarIdentity,
                targetRig.RigId,
                targetRig.Revision).Value;
        }

        static string RequireGenericSamplingRigSignature(
            CharacterMotionMatchingSourceSet sourceSet,
            CharacterAnimationRigDefinition targetRig,
            CharacterAnimationRigBinding rigBinding,
            MotionMatchingSourceClipInspection sourceInspection)
        {
            if (!sourceInspection.CompatibilityDeclared || !sourceInspection.SourceHierarchyIdentityAvailable || sourceInspection.ModelImporter == null)
                throw new InvalidOperationException(
                    $"ExactGenericRig Clip '{sourceInspection.SourceClipId}' has no formal source root or hierarchy identity.");
            if (rigBinding.Animator.isHuman)
                throw new InvalidOperationException("ExactGenericRig Target Sampling Rig Animator is declared Humanoid.");

            string[] targetPaths = RequireTargetBonePaths(targetRig, rigBinding);
            int motionRootIndex = targetRig.RequirePhysicalBoneIndex(sourceSet.MotionRootBoneId);
            string targetRootIdentity = targetPaths[motionRootIndex];
            if (!string.Equals(sourceInspection.SourceRootIdentity, targetRootIdentity, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"ExactGenericRig Clip '{sourceInspection.SourceClipId}' motion root '{sourceInspection.SourceRootIdentity}' does not match Target Rig root '{targetRootIdentity}' for BoneId '{sourceSet.MotionRootBoneId}'.");

            string[] sourcePaths = sourceInspection.ModelImporter.transformPaths;
            if (sourcePaths == null || sourcePaths.Length != sourceInspection.SourceHierarchyPathCount)
                throw new InvalidOperationException(
                    $"ExactGenericRig Clip '{sourceInspection.SourceClipId}' source hierarchy changed after inspection.");
            sourcePaths = (string[])sourcePaths.Clone();
            string[] canonicalTargetPaths = (string[])targetPaths.Clone();
            Array.Sort(sourcePaths, StringComparer.Ordinal);
            Array.Sort(canonicalTargetPaths, StringComparer.Ordinal);
            if (sourcePaths.Length != canonicalTargetPaths.Length)
                throw new InvalidOperationException(
                    $"ExactGenericRig Clip '{sourceInspection.SourceClipId}' hierarchy path count {sourcePaths.Length} does not match Target Rig count {canonicalTargetPaths.Length}.");
            for (int i = 0; i < sourcePaths.Length; i++)
            {
                if (!string.Equals(sourcePaths[i], canonicalTargetPaths[i], StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"ExactGenericRig Clip '{sourceInspection.SourceClipId}' hierarchy path '{sourcePaths[i]}' does not match Target Rig path '{canonicalTargetPaths[i]}' at canonical index {i}.");
            }

            string targetHierarchyIdentity = ComputeTargetBoneHierarchyIdentity(
                targetRig, targetPaths, targetRootIdentity);
            return StableHash.Compute(
                "motion-matching-sampling-rig/exact-generic/v1",
                ((byte)MotionMatchingSamplingCompatibilityMode.ExactGenericRig).ToString(CultureInfo.InvariantCulture),
                sourceInspection.SourceRootIdentity,
                sourceInspection.SourceHierarchyIdentity,
                targetHierarchyIdentity).Value;
        }

        static string[] RequireTargetBonePaths(
            CharacterAnimationRigDefinition targetRig,
            CharacterAnimationRigBinding rigBinding)
        {
            targetRig.RequireValid();
            if (rigBinding.PhysicalBones.Count != targetRig.PhysicalBoneCount)
                throw new InvalidOperationException("Target Sampling Rig Bone binding count does not match the Target Rig definition.");
            var paths = new string[targetRig.PhysicalBoneCount];
            var transforms = new HashSet<Transform>();
            var uniquePaths = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < paths.Length; i++)
            {
                CharacterAnimationPhysicalBoneDefinition bone = targetRig.PhysicalBones[i];
                Transform transform = rigBinding.PhysicalBones[i];
                if (bone == null || !bone.BoneId.IsValid || bone.ParentIndex < -1 || bone.ParentIndex >= i)
                    throw new InvalidOperationException($"Target Rig Bone #{i} is invalid or not parent-first.");
                if (!transform || !transforms.Add(transform) ||
                    transform != rigBinding.Animator.transform && !transform.IsChildOf(rigBinding.Animator.transform))
                    throw new InvalidOperationException($"Target Sampling Rig Bone '{bone.BoneId}' is missing, duplicated, or outside the Animator hierarchy.");
                if (bone.ParentIndex >= 0 && transform.parent != rigBinding.PhysicalBones[bone.ParentIndex])
                    throw new InvalidOperationException($"Target Sampling Rig Bone '{bone.BoneId}' does not match parent-first Rig hierarchy.");
                string path = AnimationUtility.CalculateTransformPath(transform, rigBinding.Animator.transform);
                if (path == null || !uniquePaths.Add(path))
                    throw new InvalidOperationException($"Target Sampling Rig Bone '{bone.BoneId}' has a missing or duplicate transform path.");
                paths[i] = path;
            }
            return paths;
        }

        static string ComputeTargetBoneHierarchyIdentity(
            CharacterAnimationRigDefinition targetRig,
            string[] targetPaths,
            string targetRootIdentity)
        {
            var parts = new string[targetRig.PhysicalBoneCount + 4];
            parts[0] = "motion-matching-target-hierarchy/v1";
            parts[1] = targetRig.RigId;
            parts[2] = targetRig.Revision;
            parts[3] = targetRootIdentity;
            for (int i = 0; i < targetRig.PhysicalBoneCount; i++)
            {
                CharacterAnimationPhysicalBoneDefinition bone = targetRig.PhysicalBones[i];
                parts[i + 4] = FormattableString.Invariant(
                    $"{i}:{bone.BoneId.Value}:{bone.ParentIndex}:{targetPaths[i]}");
            }
            return StableHash.Compute(parts).Value;
        }

        static string RequireStableAssetIdentity(UnityEngine.Object value, string label)
        {
            if (!value || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long localId) ||
                string.IsNullOrEmpty(guid) || localId == 0)
                throw new InvalidOperationException($"{label} has no stable GUID/local file identity.");
            return guid + ":" + localId.ToString(CultureInfo.InvariantCulture);
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
                DependencyHash(database), DependencyHash(database.TargetRig), DependencyHash(analysisSource),
                DependencyHash(database.FeatureSchema),
                DependencyHash(profile.TrajectoryPolicy),
                DependencyHash(profile.CostProfile),
                DependencyHash(profile.SearchPolicy),
                dependencyHash.Value
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
