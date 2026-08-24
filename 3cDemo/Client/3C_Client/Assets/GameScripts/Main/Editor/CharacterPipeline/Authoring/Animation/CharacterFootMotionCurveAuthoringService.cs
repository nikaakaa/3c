using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterFootMotionCurveCandidate
    {
        readonly Dictionary<string, AnimationCurve> m_Curves;

        internal CharacterFootMotionCurveCandidate(
            AnimationClip clip,
            CharacterFootPlacementAnalysisSource source,
            AnimationFootContactSchedule schedule,
            CharacterAnimationClipContentIdentity clipIdentity,
            AnimationFootAnalysisArtifact artifact,
            Dictionary<string, AnimationCurve> curves)
        {
            Clip = clip ? clip : throw new ArgumentNullException(nameof(clip));
            Source = source ? source : throw new ArgumentNullException(nameof(source));
            Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            ClipAssetGuid = clipIdentity.AssetGuid;
            ClipLocalFileId = clipIdentity.LocalFileId;
            DependencyBaseline = clipIdentity.FullDependencyHash;
            AnalysisInputHash = clipIdentity.AnalysisInputHash;
            RegisteredCurveHash = clipIdentity.RegisteredCurveHash;
            ArtifactIdentityHash = artifact?.Identity.IdentityHash ??
                                   throw new ArgumentNullException(nameof(artifact));
            ArtifactContentHash = artifact.ContentHash;
            MotionReferenceClipAssetGuid = artifact.Identity.MotionReferenceClipAssetGuid;
            MotionReferenceClipAnalysisInputHash = artifact.Identity.MotionReferenceClipAnalysisInputHash;
            m_Curves = curves?.ToDictionary(
                pair => pair.Key,
                pair => Copy(pair.Value),
                StringComparer.Ordinal) ?? throw new ArgumentNullException(nameof(curves));
            RequireComplete();
        }

        public AnimationClip Clip { get; }
        public CharacterFootPlacementAnalysisSource Source { get; }
        public AnimationFootContactSchedule Schedule { get; }
        public string ClipAssetGuid { get; }
        public long ClipLocalFileId { get; }
        public string DependencyBaseline { get; }
        public string AnalysisInputHash { get; }
        public string RegisteredCurveHash { get; }
        public StableHash ArtifactIdentityHash { get; }
        public StableHash ArtifactContentHash { get; }
        public string MotionReferenceClipAssetGuid { get; }
        public string MotionReferenceClipAnalysisInputHash { get; }
        public IReadOnlyDictionary<string, AnimationCurve> Curves => m_Curves;

        internal Dictionary<string, AnimationCurve> CopyCurves() =>
            m_Curves.ToDictionary(pair => pair.Key, pair => Copy(pair.Value), StringComparer.Ordinal);

        void RequireComplete()
        {
            if (m_Curves.Count != CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels.Count)
                throw new InvalidOperationException("Foot Motion candidate does not contain 22 curves.");
            for (int i = 0; i < CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels.Count; i++)
            {
                string channelId = CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels[i].ChannelId;
                if (!m_Curves.TryGetValue(channelId, out AnimationCurve curve))
                    throw new InvalidOperationException($"Foot Motion candidate is missing '{channelId}'.");
                CharacterAnimationClipRegisteredCurveCatalog.Validate(Clip, channelId, curve);
            }
        }

        static AnimationCurve Copy(AnimationCurve source) =>
            new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
    }

    public static class CharacterFootMotionCurveAuthoringService
    {
        public static CharacterFootMotionCurveCandidate BuildCandidate(
            AnimationClip clip,
            CharacterFootPlacementAnalysisSource source,
            AnimationFootContactSchedule schedule = null)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            if (!source)
                throw new ArgumentNullException(nameof(source));
            AnimationFootContactSchedule resolvedSchedule = schedule ?? AnimationFootContactSchedule.Inferred;
            AnimationFootAnalysisArtifactIdentity expected =
                AnimationFootAnalysisArtifactBuilder.GetExpectedIdentity(clip, source, resolvedSchedule);
            AnimationFootAnalysisArtifactInspection inspection =
                AnimationFootAnalysisArtifactStore.Inspect(expected);
            if (inspection.Status != AnimationFootAnalysisArtifactStatus.Ready || inspection.Artifact == null)
                throw new InvalidOperationException(
                    $"Foot Motion candidate requires a Ready artifact: {inspection.Status}; {inspection.Error}");
            AnimationFootMotionDataDescriptor data = inspection.Artifact.MotionData;
            if (!data.Left.CanBuildCurves || !data.Right.CanBuildCurves)
                throw new InvalidOperationException(
                    $"Foot Motion data is incomplete: Left={data.Left.Diagnostic}; Right={data.Right.Diagnostic}");
            CharacterAnimationClipContentIdentity identity =
                CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
            Dictionary<string, AnimationCurve> curves = BuildCurves(data);
            return new CharacterFootMotionCurveCandidate(
                clip,
                source,
                resolvedSchedule,
                identity,
                inspection.Artifact,
                curves);
        }

        public static void Apply(CharacterFootMotionCurveCandidate candidate)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));
            CharacterAnimationClipContentIdentity current =
                CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(candidate.Clip);
            if (!string.Equals(current.AssetGuid, candidate.ClipAssetGuid, StringComparison.Ordinal) ||
                current.LocalFileId != candidate.ClipLocalFileId ||
                !string.Equals(current.FullDependencyHash, candidate.DependencyBaseline, StringComparison.Ordinal) ||
                !string.Equals(current.AnalysisInputHash, candidate.AnalysisInputHash, StringComparison.Ordinal) ||
                !string.Equals(current.RegisteredCurveHash, candidate.RegisteredCurveHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot Motion candidate is stale because the AnimationClip changed.");
            AnimationFootAnalysisArtifactIdentity expected =
                AnimationFootAnalysisArtifactBuilder.GetExpectedIdentity(
                    candidate.Clip,
                    candidate.Source,
                    candidate.Schedule);
            AnimationFootAnalysisArtifactInspection inspection =
                AnimationFootAnalysisArtifactStore.Inspect(expected);
            if (inspection.Status != AnimationFootAnalysisArtifactStatus.Ready || inspection.Artifact == null ||
                !inspection.Artifact.Identity.IdentityHash.Equals(candidate.ArtifactIdentityHash) ||
                !inspection.Artifact.ContentHash.Equals(candidate.ArtifactContentHash))
                throw new InvalidOperationException("Foot Motion candidate is stale because its analysis artifact changed.");
            Dictionary<string, AnimationCurve> curves = candidate.CopyCurves();
            for (int i = 0; i < CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels.Count; i++)
            {
                string channelId = CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels[i].ChannelId;
                CharacterAnimationClipRegisteredCurveCatalog.Validate(candidate.Clip, channelId, curves[channelId]);
            }
            Undo.RecordObject(candidate.Clip, "Apply Foot Motion Curves");
            CharacterAnimationClipRegisteredCurveCatalog.ReplaceFootMotionGroup(candidate.Clip, curves);
        }

        public static void ValidateApplied(CharacterFootMotionCurveCandidate candidate)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));
            for (int i = 0; i < CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels.Count; i++)
            {
                string channelId = CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels[i].ChannelId;
                AnimationCurve actual = CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                    candidate.Clip,
                    channelId);
                AnimationCurve expected = candidate.Curves[channelId];
                if (!CurvesEqual(actual, expected))
                    throw new InvalidOperationException(
                        $"AnimationClip '{candidate.Clip.name}' applied Curve '{channelId}' differs from its Artifact candidate.");
            }
        }

        internal static bool CurvesEqual(AnimationCurve left, AnimationCurve right)
        {
            if (left == null || right == null || left.length != right.length ||
                left.preWrapMode != right.preWrapMode || left.postWrapMode != right.postWrapMode)
                return false;
            Keyframe[] leftKeys = left.keys;
            Keyframe[] rightKeys = right.keys;
            for (int i = 0; i < leftKeys.Length; i++)
            {
                if (BitConverter.SingleToInt32Bits(leftKeys[i].time) != BitConverter.SingleToInt32Bits(rightKeys[i].time) ||
                    BitConverter.SingleToInt32Bits(leftKeys[i].value) != BitConverter.SingleToInt32Bits(rightKeys[i].value) ||
                    BitConverter.SingleToInt32Bits(leftKeys[i].inTangent) != BitConverter.SingleToInt32Bits(rightKeys[i].inTangent) ||
                    BitConverter.SingleToInt32Bits(leftKeys[i].outTangent) != BitConverter.SingleToInt32Bits(rightKeys[i].outTangent) ||
                    BitConverter.SingleToInt32Bits(leftKeys[i].inWeight) != BitConverter.SingleToInt32Bits(rightKeys[i].inWeight) ||
                    BitConverter.SingleToInt32Bits(leftKeys[i].outWeight) != BitConverter.SingleToInt32Bits(rightKeys[i].outWeight) ||
                    leftKeys[i].weightedMode != rightKeys[i].weightedMode)
                    return false;
            }
            return true;
        }

        static Dictionary<string, AnimationCurve> BuildCurves(AnimationFootMotionDataDescriptor data)
        {
            var result = new Dictionary<string, AnimationCurve>(StringComparer.Ordinal);
            AddFoot(result, data.Left, true);
            AddFoot(result, data.Right, false);
            return result;
        }

        static void AddFoot(
            IDictionary<string, AnimationCurve> destination,
            AnimationFootMotionFootPage foot,
            bool left)
        {
            destination.Add(Channel(left, 0), StepTimeCurve(foot));
            destination.Add(Channel(left, 1), ConstantCurve(foot, value => value.Step.Distance));
            destination.Add(Channel(left, 2), LinearCurve(foot, value => value.Step.HeightAbovePath));
            destination.Add(Channel(left, 3), LinearCurve(foot, value => value.Filter.ToeHeight));
            destination.Add(Channel(left, 4), LinearCurve(foot, value => value.Filter.ToeSpeed));
            destination.Add(Channel(left, 5), LinearCurve(foot, value => value.Filter.PositionError));
            destination.Add(Channel(left, 6), LinearCurve(foot, value => value.Filter.RotationError));
            destination.Add(Channel(left, 7), LinearCurve(foot, value => value.Filter.Contact));
            destination.Add(Channel(left, 8), ConstantCurve(foot, value => (float)value.Constraint.LockMode));
            destination.Add(Channel(left, 9), LinearCurve(foot, value => value.Constraint.LockWeight));
            destination.Add(Channel(left, 10), LinearCurve(foot, value => value.Constraint.Support));
        }

        static string Channel(bool left, int index)
        {
            string[] channels = left
                ? new[]
                {
                    CharacterAnimationClipRegisteredCurveChannels.LeftStepTime,
                    CharacterAnimationClipRegisteredCurveChannels.LeftStepDistance,
                    CharacterAnimationClipRegisteredCurveChannels.LeftFootHeight,
                    CharacterAnimationClipRegisteredCurveChannels.LeftToeHeight,
                    CharacterAnimationClipRegisteredCurveChannels.LeftToeSpeed,
                    CharacterAnimationClipRegisteredCurveChannels.LeftPositionError,
                    CharacterAnimationClipRegisteredCurveChannels.LeftRotationError,
                    CharacterAnimationClipRegisteredCurveChannels.LeftContact,
                    CharacterAnimationClipRegisteredCurveChannels.LeftLockMode,
                    CharacterAnimationClipRegisteredCurveChannels.LeftLockWeight,
                    CharacterAnimationClipRegisteredCurveChannels.LeftSupport
                }
                : new[]
                {
                    CharacterAnimationClipRegisteredCurveChannels.RightStepTime,
                    CharacterAnimationClipRegisteredCurveChannels.RightStepDistance,
                    CharacterAnimationClipRegisteredCurveChannels.RightFootHeight,
                    CharacterAnimationClipRegisteredCurveChannels.RightToeHeight,
                    CharacterAnimationClipRegisteredCurveChannels.RightToeSpeed,
                    CharacterAnimationClipRegisteredCurveChannels.RightPositionError,
                    CharacterAnimationClipRegisteredCurveChannels.RightRotationError,
                    CharacterAnimationClipRegisteredCurveChannels.RightContact,
                    CharacterAnimationClipRegisteredCurveChannels.RightLockMode,
                    CharacterAnimationClipRegisteredCurveChannels.RightLockWeight,
                    CharacterAnimationClipRegisteredCurveChannels.RightSupport
                };
            return channels[index];
        }

        static AnimationCurve StepTimeCurve(AnimationFootMotionFootPage foot)
        {
            AnimationCurve curve = LinearCurve(foot, value => value.Step.TimeSeconds);
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (keys[i + 1].value <= keys[i].value + 0.000001f)
                    continue;
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyLeftTangentMode(curve, i + 1, AnimationUtility.TangentMode.Constant);
            }
            return curve;
        }

        static AnimationCurve LinearCurve(
            AnimationFootMotionFootPage foot,
            Func<AnimationFootMotionDerivedSample, float> select)
        {
            var keys = new Keyframe[foot.Samples.Count];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = new Keyframe(foot.Samples[i].TimeSeconds, select(foot.Samples[i]));
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            return curve;
        }

        static AnimationCurve ConstantCurve(
            AnimationFootMotionFootPage foot,
            Func<AnimationFootMotionDerivedSample, float> select)
        {
            var keys = new Keyframe[foot.Samples.Count];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = new Keyframe(foot.Samples[i].TimeSeconds, select(foot.Samples[i]));
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            }
            return curve;
        }
    }

    public enum CharacterFootMotionBakeState : byte
    {
        Empty = 1,
        Same = 2,
        Different = 3,
        Partial = 4
    }

    public enum CharacterFootMotionBakeChannelDiffKind : byte
    {
        Missing = 1,
        Changed = 2
    }

    public readonly struct CharacterFootMotionBakeChannelDiff
    {
        public CharacterFootMotionBakeChannelDiff(
            string channelId,
            string propertyName,
            CharacterFootMotionBakeChannelDiffKind kind)
        {
            ChannelId = channelId ?? throw new ArgumentNullException(nameof(channelId));
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            Kind = kind;
        }

        public string ChannelId { get; }
        public string PropertyName { get; }
        public CharacterFootMotionBakeChannelDiffKind Kind { get; }
    }

    public sealed class CharacterFootMotionBakePlan
    {
        readonly CharacterFootMotionBakeChannelDiff[] m_ChangedChannels;

        internal CharacterFootMotionBakePlan(
            CharacterFootPlacementAnalysisSource source,
            AnimationClip targetClip,
            AnimationClip motionReferenceClip,
            CharacterFootMotionCurveCandidate candidate,
            CharacterFootMotionBakeState state,
            CharacterFootMotionBakeChannelDiff[] changedChannels)
        {
            Source = source ? source : throw new ArgumentNullException(nameof(source));
            TargetClip = targetClip ? targetClip : throw new ArgumentNullException(nameof(targetClip));
            MotionReferenceClip = motionReferenceClip
                ? motionReferenceClip
                : throw new ArgumentNullException(nameof(motionReferenceClip));
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            State = state;
            m_ChangedChannels = changedChannels ?? throw new ArgumentNullException(nameof(changedChannels));
            SourceAssetPath = AssetDatabase.GetAssetPath(Source);
            TargetAssetPath = AssetDatabase.GetAssetPath(TargetClip);
            MotionReferenceAssetPath = AssetDatabase.GetAssetPath(MotionReferenceClip);
            PlanHash = StableHash.Compute(
                "character-foot-motion-bake-plan/1",
                Candidate.ClipAssetGuid,
                Candidate.ClipLocalFileId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Candidate.RegisteredCurveHash,
                Candidate.ArtifactIdentityHash.Value,
                Candidate.ArtifactContentHash.Value,
                State.ToString(),
                string.Join("|", m_ChangedChannels.Select(value =>
                    string.Concat(value.ChannelId, ":", value.Kind.ToString())))).Value;
        }

        public CharacterFootPlacementAnalysisSource Source { get; }
        public AnimationClip TargetClip { get; }
        public AnimationClip MotionReferenceClip { get; }
        public CharacterFootMotionCurveCandidate Candidate { get; }
        public CharacterFootMotionBakeState State { get; }
        public string SourceAssetPath { get; }
        public string TargetAssetPath { get; }
        public string MotionReferenceAssetPath { get; }
        public string PlanHash { get; }
        public string RegisteredCurveHash => Candidate.RegisteredCurveHash;
        public StableHash ArtifactIdentityHash => Candidate.ArtifactIdentityHash;
        public StableHash ArtifactContentHash => Candidate.ArtifactContentHash;
        public IReadOnlyList<CharacterFootMotionBakeChannelDiff> ChangedChannels => m_ChangedChannels;
        public bool RequiresReplace =>
            State is CharacterFootMotionBakeState.Different or CharacterFootMotionBakeState.Partial;
        public bool IsNoChange => State == CharacterFootMotionBakeState.Same;
    }

    public sealed class CharacterFootMotionBakeApplyResult
    {
        public CharacterFootMotionBakeApplyResult(
            CharacterFootMotionBakePlan plan,
            bool applied,
            string registeredCurveHash)
        {
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            Applied = applied;
            RegisteredCurveHash = registeredCurveHash ?? string.Empty;
        }

        public CharacterFootMotionBakePlan Plan { get; }
        public bool Applied { get; }
        public string RegisteredCurveHash { get; }
    }

    public static class CharacterFootMotionBakeService
    {
        public static CharacterFootMotionBakePlan Analyze(
            CharacterFootPlacementAnalysisSource source,
            AnimationClip targetClip)
        {
            RequireInput(source, targetClip);
            AnimationFootAnalysisArtifactBuilder.Build(targetClip, source);
            return BuildPlanFromReadyArtifact(source, targetClip);
        }

        public static CharacterFootMotionBakePlan BuildPlanFromReadyArtifact(
            CharacterFootPlacementAnalysisSource source,
            AnimationClip targetClip)
        {
            RequireInput(source, targetClip);
            CharacterFootMotionReference motionReference = source.RequireMotionReference(targetClip);
            CharacterFootMotionCurveCandidate candidate =
                CharacterFootMotionCurveAuthoringService.BuildCandidate(targetClip, source);
            var changed = new List<CharacterFootMotionBakeChannelDiff>(
                CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels.Count);
            int existingCount = 0;
            for (int i = 0; i < CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels.Count; i++)
            {
                CharacterAnimationClipRegisteredCurveDescriptor descriptor =
                    CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels[i];
                AnimationCurve current = AnimationUtility.GetEditorCurve(targetClip, descriptor.Binding);
                if (current == null)
                {
                    changed.Add(new CharacterFootMotionBakeChannelDiff(
                        descriptor.ChannelId,
                        descriptor.Binding.propertyName,
                        CharacterFootMotionBakeChannelDiffKind.Missing));
                    continue;
                }
                existingCount++;
                CharacterAnimationClipRegisteredCurveCatalog.Validate(
                    targetClip,
                    descriptor.ChannelId,
                    current);
                if (!CharacterFootMotionCurveAuthoringService.CurvesEqual(
                        current,
                        candidate.Curves[descriptor.ChannelId]))
                {
                    changed.Add(new CharacterFootMotionBakeChannelDiff(
                        descriptor.ChannelId,
                        descriptor.Binding.propertyName,
                        CharacterFootMotionBakeChannelDiffKind.Changed));
                }
            }
            CharacterFootMotionBakeState state = existingCount == 0
                ? CharacterFootMotionBakeState.Empty
                : existingCount != CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels.Count
                    ? CharacterFootMotionBakeState.Partial
                    : changed.Count == 0
                        ? CharacterFootMotionBakeState.Same
                        : CharacterFootMotionBakeState.Different;
            return new CharacterFootMotionBakePlan(
                source,
                targetClip,
                motionReference.MotionReference,
                candidate,
                state,
                changed.ToArray());
        }

        public static CharacterFootMotionBakeApplyResult Apply(
            CharacterFootMotionBakePlan analyzedPlan,
            string expectedPlanHash,
            bool replaceExisting)
        {
            if (analyzedPlan == null)
                throw new ArgumentNullException(nameof(analyzedPlan));
            if (string.IsNullOrWhiteSpace(expectedPlanHash) ||
                !string.Equals(expectedPlanHash, analyzedPlan.PlanHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot Motion Bake expected plan hash does not match the analyzed plan.");
            CharacterFootMotionBakePlan current = BuildPlanFromReadyArtifact(
                analyzedPlan.Source,
                analyzedPlan.TargetClip);
            if (!string.Equals(current.PlanHash, expectedPlanHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot Motion Bake plan is stale because its inputs, Artifact, or registered Curves changed.");
            if (current.IsNoChange)
            {
                return new CharacterFootMotionBakeApplyResult(
                    current,
                    false,
                    CharacterAnimationClipRegisteredCurveCatalog.ComputeRegisteredCurveHash(current.TargetClip));
            }
            if (current.RequiresReplace && !replaceExisting)
                throw new InvalidOperationException("Foot Motion Bake would replace existing Curve data and requires explicit replace confirmation.");
            CharacterFootMotionCurveAuthoringService.Apply(current.Candidate);
            CharacterFootMotionCurveAuthoringService.ValidateApplied(current.Candidate);
            AssetDatabase.SaveAssetIfDirty(current.TargetClip);
            CharacterFootMotionBakePlan applied = BuildPlanFromReadyArtifact(
                current.Source,
                current.TargetClip);
            if (!applied.IsNoChange)
                throw new InvalidOperationException("Foot Motion Bake did not converge to the analyzed Candidate.");
            return new CharacterFootMotionBakeApplyResult(
                applied,
                true,
                CharacterAnimationClipRegisteredCurveCatalog.ComputeRegisteredCurveHash(applied.TargetClip));
        }

        static void RequireInput(
            CharacterFootPlacementAnalysisSource source,
            AnimationClip targetClip)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            if (!targetClip)
                throw new ArgumentNullException(nameof(targetClip));
            source.RequireValid();
            _ = source.RequireMotionReference(targetClip);
            string targetPath = AssetDatabase.GetAssetPath(targetClip);
            if (string.IsNullOrEmpty(targetPath) ||
                !targetPath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase) ||
                AssetDatabase.LoadMainAssetAtPath(targetPath) != targetClip)
                throw new InvalidOperationException("Foot Motion Bake Target must be one persisted native AnimationClip.");
        }
    }
}
