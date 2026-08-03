using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics.Editor;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation.Fixed;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public readonly struct ActionAnimationNumericTargetView
    {
        public ActionAnimationNumericTargetView(
            string profileId,
            int abiVersion)
        {
            ProfileId = profileId?.Trim() ?? string.Empty;
            AbiVersion = abiVersion;
        }

        public string ProfileId { get; }
        public int AbiVersion { get; }
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ProfileId) &&
            AbiVersion > 0;
        public override string ToString() =>
            IsValid ? $"{ProfileId} / ABI {AbiVersion}" : "unresolved";
    }

    public sealed class ActionAnimationWorkspaceLiveView
    {
        readonly PoseInertializationSnapshot[] m_Inertializations;

        public ActionAnimationWorkspaceLiveView(
            RuntimeDebugTargetInfo target,
            ActionAnimationNumericTargetView numericTarget,
            ActionAnimationPlaybackLifecycleSnapshot playback,
            ActionPresentationTimeSnapshot time,
            AnimationSlotRuntimeSnapshot slot,
            AnimationBlendStackSnapshot stack,
            bool hasStack,
            int inertializationCount,
            AnimationPresentationRuntimeSnapshot posePlan)
        {
            Target = target;
            NumericTarget = numericTarget;
            Playback = playback ??
                throw new ArgumentNullException(nameof(playback));
            Time = time;
            Slot = slot;
            Stack = stack;
            HasStack = hasStack;
            InertializationCount = inertializationCount;
            m_Inertializations =
                CopyInertializations(posePlan.Inertializations);
            ProjectionRevision = posePlan.ProjectionRevision;
            PoseGraphId = posePlan.PoseGraphId;
            PoseGraphRevision = posePlan.PoseGraphRevision;
            PosePlanHash = posePlan.PosePlanHash;
            PresentationFrame = posePlan.CompletionIdentity;
            FinalAvailability = posePlan.FinalAvailability;
            FinalInvalidReason = posePlan.FinalInvalidReason;
            FinalAppliedAt = posePlan.FinalAppliedAt;
            FinalContinuityIdentity = posePlan.ContinuityIdentity;
            FinalContributionCount = posePlan.FinalContributions.Count;
        }

        public RuntimeDebugTargetInfo Target { get; }
        public ActionAnimationNumericTargetView NumericTarget { get; }
        public ActionAnimationPlaybackLifecycleSnapshot Playback { get; }
        public ActionPresentationTimeSnapshot Time { get; }
        public AnimationSlotRuntimeSnapshot Slot { get; }
        public AnimationBlendStackSnapshot Stack { get; }
        public bool HasStack { get; }
        public int InertializationCount { get; }
        public IReadOnlyList<PoseInertializationSnapshot>
            Inertializations => m_Inertializations;
        public string ProjectionRevision { get; }
        public string PoseGraphId { get; }
        public string PoseGraphRevision { get; }
        public string PosePlanHash { get; }
        public ulong PresentationFrame { get; }
        public AnimationPoseAvailability FinalAvailability { get; }
        public AnimationPoseNativeInvalidReason FinalInvalidReason { get; }
        public ulong FinalAppliedAt { get; }
        public ulong FinalContinuityIdentity { get; }
        public int FinalContributionCount { get; }

        static PoseInertializationSnapshot[] CopyInertializations(
            AnimationReadOnlyBuffer<PoseInertializationSnapshot> source)
        {
            var result =
                new PoseInertializationSnapshot[source.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = source[i];
            return result;
        }
    }

    public sealed class ActionAnimationWorkspacePreviewView
    {
        readonly PoseInertializationSnapshot[] m_Inertializations;

        public ActionAnimationWorkspacePreviewView(
            ActionAnimationPlaybackLifecycleSnapshot playback,
            ActionPresentationTimeSnapshot time,
            AnimationSlotRuntimeSnapshot slot,
            AnimationBlendStackSnapshot stack,
            bool hasStack,
            int inertializationCount,
            AnimationPresentationRuntimeSnapshot posePlan,
            CharacterPosePlanStageSnapshot stages)
        {
            Playback = playback ??
                throw new ArgumentNullException(nameof(playback));
            Time = time;
            Slot = slot;
            Stack = stack;
            HasStack = hasStack;
            InertializationCount = inertializationCount;
            m_Inertializations =
                CopyInertializations(posePlan.Inertializations);
            PosePlan = posePlan;
            Stages = stages;
        }

        public ActionAnimationPlaybackLifecycleSnapshot Playback { get; }
        public ActionPresentationTimeSnapshot Time { get; }
        public AnimationSlotRuntimeSnapshot Slot { get; }
        public AnimationBlendStackSnapshot Stack { get; }
        public bool HasStack { get; }
        public int InertializationCount { get; }
        public IReadOnlyList<PoseInertializationSnapshot>
            Inertializations => m_Inertializations;
        public AnimationPresentationRuntimeSnapshot PosePlan { get; }
        public CharacterPosePlanStageSnapshot Stages { get; }

        static PoseInertializationSnapshot[] CopyInertializations(
            AnimationReadOnlyBuffer<PoseInertializationSnapshot> source)
        {
            var result =
                new PoseInertializationSnapshot[source.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = source[i];
            return result;
        }
    }

    public static class ActionAnimationAuthoringWorkspaceLiveProjection
    {
        static readonly Dictionary<string, ActionAnimationNumericTargetView>
            s_NumericTargets =
                new Dictionary<string, ActionAnimationNumericTargetView>(
                    StringComparer.Ordinal);

        public static bool TryResolve(
            ActionAnimationWorkspaceResolution resolution,
            RuntimeDebugViewModel runtimeView,
            out ActionAnimationWorkspaceLiveView live,
            out string failure)
        {
            live = null;
            failure = string.Empty;
            if (resolution?.Producer == null ||
                resolution.Slot == null ||
                resolution.RuntimeDebug == null ||
                resolution.Definition == null)
            {
                failure = "Workspace typed session 尚未闭合。";
                return false;
            }
            if (runtimeView == null || !runtimeView.Attached)
            {
                failure = "没有连接 Runtime Debug target。";
                return false;
            }
            if (!runtimeView.Valid)
            {
                failure = string.IsNullOrWhiteSpace(runtimeView.Error)
                    ? "Runtime Debug View 无效。"
                    : runtimeView.Error;
                return false;
            }
            if (!string.Equals(
                    runtimeView.Target.Revision.SourceRevision,
                    resolution.RuntimeDebug.SourceRevision,
                    StringComparison.Ordinal))
            {
                failure =
                    $"Trace source revision stale: runtime={runtimeView.Target.Revision.SourceRevision}, authoring={resolution.RuntimeDebug.SourceRevision}.";
                return false;
            }
            if (!AnimationPresentationRuntimeTargetRegistry.TryGet(
                    runtimeView.Target.CharacterRuntimeId,
                    out AnimationPresentationRuntimeTarget target))
            {
                failure =
                    "当前 Runtime Debug target 没有正式 Animation Presentation diagnostics target。";
                return false;
            }
            if (!string.Equals(
                    target.ProjectionRevision,
                    resolution.RuntimeDebug.ProjectionRevision,
                    StringComparison.Ordinal))
            {
                failure =
                    $"Trace projection revision stale: runtime={target.ProjectionRevision}, authoring={resolution.RuntimeDebug.ProjectionRevision}.";
                return false;
            }
            if (!target.TryGetDebugView(
                    out AnimationPresentationDebugView debug))
            {
                failure =
                    "Animation Presentation 尚未发布完整 committed debug frame。";
                return false;
            }

            if (!TryResolveAnimation(
                    resolution,
                    debug,
                    out ActionAnimationPlaybackLifecycleSnapshot playback,
                    out ActionPresentationTimeSnapshot time,
                    out AnimationSlotRuntimeSnapshot slot,
                    out AnimationBlendStackSnapshot stack,
                    out bool hasStack,
                    out AnimationPresentationRuntimeSnapshot posePlan,
                    out failure))
                return false;
            ActionAnimationNumericTargetView numericTarget =
                ResolveNumericTarget(
                    resolution.Definition,
                    runtimeView.Target.Revision);
            if (!numericTarget.IsValid)
            {
                failure =
                    $"Runtime Program '{runtimeView.Target.Revision}' 无法精确映射到 Definition 的 Float32 或 Fixed Program。";
                return false;
            }
            live = new ActionAnimationWorkspaceLiveView(
                runtimeView.Target,
                numericTarget,
                playback,
                time,
                slot,
                stack,
                hasStack,
                posePlan.Inertializations.Count,
                posePlan);
            return true;
        }

        public static bool TryResolvePreview(
            ActionAnimationWorkspaceResolution resolution,
            AnimationPresentationDebugView debug,
            CharacterPosePlanStageSnapshot stages,
            out ActionAnimationWorkspacePreviewView preview,
            out string failure)
        {
            preview = null;
            failure = string.Empty;
            if (resolution?.Producer == null ||
                resolution.Slot == null ||
                resolution.PreviewTarget?.IsReady != true)
            {
                failure = "Workspace Preview typed session 尚未闭合。";
                return false;
            }
            if (debug == null)
            {
                failure = "正式 Timeline Preview 尚未发布 Animation Debug View。";
                return false;
            }
            AnimationPresentationRuntimeSnapshot posePlan =
                debug.PosePlan;
            if (!string.Equals(
                    posePlan.ProjectionRevision,
                    resolution.PreviewTarget.Projection.ProjectionRevision,
                    StringComparison.Ordinal))
            {
                failure =
                    $"Preview projection stale: preview={posePlan.ProjectionRevision}, authoring={resolution.PreviewTarget.Projection.ProjectionRevision}.";
                return false;
            }
            if (!string.Equals(
                    posePlan.PoseGraphId,
                    resolution.Slot.Graph.GraphId.ToString(),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    posePlan.PoseGraphRevision,
                    resolution.Slot.Graph.ContentRevision,
                    StringComparison.Ordinal))
            {
                failure =
                    $"Preview Pose Graph stale: preview={posePlan.PoseGraphId}@{posePlan.PoseGraphRevision}, authoring={resolution.Slot.Graph.GraphId}@{resolution.Slot.Graph.ContentRevision}.";
                return false;
            }
            if (!stages.IsValid ||
                !string.Equals(
                    stages.PoseGraphId,
                    posePlan.PoseGraphId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    stages.PosePlanHash,
                    posePlan.PosePlanHash,
                    StringComparison.Ordinal))
            {
                failure = "Preview Pose Plan stage 与 committed Animation frame 不匹配。";
                return false;
            }
            if (!TryResolveAnimation(
                    resolution,
                    debug,
                    out ActionAnimationPlaybackLifecycleSnapshot playback,
                    out ActionPresentationTimeSnapshot time,
                    out AnimationSlotRuntimeSnapshot slot,
                    out AnimationBlendStackSnapshot stack,
                    out bool hasStack,
                    out posePlan,
                    out failure))
                return false;
            preview = new ActionAnimationWorkspacePreviewView(
                playback,
                time,
                slot,
                stack,
                hasStack,
                posePlan.Inertializations.Count,
                posePlan,
                stages);
            return true;
        }

        static bool TryResolveAnimation(
            ActionAnimationWorkspaceResolution resolution,
            AnimationPresentationDebugView debug,
            out ActionAnimationPlaybackLifecycleSnapshot playback,
            out ActionPresentationTimeSnapshot time,
            out AnimationSlotRuntimeSnapshot slot,
            out AnimationBlendStackSnapshot stack,
            out bool hasStack,
            out AnimationPresentationRuntimeSnapshot posePlan,
            out string failure)
        {
            playback = null;
            time = default;
            slot = default;
            stack = default;
            hasStack = false;
            posePlan = default;
            failure = string.Empty;
            ActionAnimationPlaybackLifecycleSnapshot[] playbacks =
                debug.ActionPlaybacks
                    .Where(value =>
                        value != null &&
                        value.PlaybackId.ProducerId.Equals(
                            resolution.Producer.ProducerId))
                    .OrderByDescending(value =>
                        value.LatestCommandSequence)
                    .ToArray();
            if (playbacks.Length == 0)
            {
                failure =
                    $"Trace 中没有 producer '{resolution.Producer.ProducerId}' 的 Action playback。";
                return false;
            }
            if (playbacks.Length > 1 &&
                playbacks[0].LatestCommandSequence ==
                playbacks[1].LatestCommandSequence)
            {
                failure =
                    $"Trace 中 producer '{resolution.Producer.ProducerId}' 的最新 playback 不唯一。";
                return false;
            }
            playback = playbacks[0];
            ActionAnimationPlaybackLifecycleSnapshot selectedPlayback =
                playback;
            ActionPresentationTimeSnapshot[] times =
                debug.ActionTimes
                    .Where(value =>
                        value.PlaybackId.Equals(
                            selectedPlayback.PlaybackId))
                    .ToArray();
            if (times.Length != 1)
            {
                failure =
                    $"Playback '{playback.PlaybackId}' 的三层时间快照数量为 {times.Length}。";
                return false;
            }
            time = times[0];
            posePlan = debug.PosePlan;
            AnimationSlotRuntimeSnapshot[] slots =
                Copy(posePlan.AnimationSlots)
                    .Where(value =>
                        value.SlotId.Equals(
                            resolution.Slot.SlotId))
                    .ToArray();
            if (slots.Length != 1)
            {
                failure =
                    $"Pose Plan 中 Slot '{resolution.Slot.SlotId}' 的 Runtime route 数量为 {slots.Length}。";
                return false;
            }
            slot = slots[0];
            AnimationBlendStackSnapshot[] stacks =
                Copy(posePlan.Stacks)
                    .Where(value =>
                        value.PoseNodeId.Equals(
                            resolution.Slot.Node.NodeId))
                    .ToArray();
            if (stacks.Length > 1)
            {
                failure =
                    $"Slot node '{resolution.Slot.Node.NodeId}' 的 Blend Stack 快照不唯一。";
                return false;
            }
            hasStack = stacks.Length == 1;
            stack = hasStack ? stacks[0] : default;
            return true;
        }

        static ActionAnimationNumericTargetView ResolveNumericTarget(
            ActionAnimationDefinitionContext definition,
            BTSMTL.Diagnostics.RuntimeProgramRevision revision)
        {
            string key =
                $"{definition.AssetGuid}/{revision.ProgramId}/{revision.SourceRevision}/{revision.ProgramHash}";
            if (s_NumericTargets.TryGetValue(
                    key,
                    out ActionAnimationNumericTargetView cached))
                return cached;

            CharacterPipelineDefinition value =
                definition.Definition;
            if (value.SimulationProgram &&
                string.Equals(
                    value.SimulationProgram.ProgramId,
                    revision.ProgramId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    value.SimulationProgram.SourceRevision,
                    revision.SourceRevision,
                    StringComparison.Ordinal) &&
                string.Equals(
                    value.SimulationProgram.ProgramHash,
                    revision.ProgramHash,
                    StringComparison.Ordinal))
            {
                cached = new ActionAnimationNumericTargetView(
                    value.SimulationProgram.NumericProfileId,
                    value.SimulationProgram.TargetAbiVersion);
                s_NumericTargets[key] = cached;
                return cached;
            }

            FixedCharacterSimulationProgramAsset[] fixedMatches =
                AssetDatabase
                    .FindAssets(
                        "t:FixedCharacterSimulationProgramAsset")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(
                        AssetDatabase.LoadAssetAtPath<
                            FixedCharacterSimulationProgramAsset>)
                    .Where(candidate =>
                        candidate &&
                        string.Equals(
                            candidate.DefinitionGuid,
                            definition.AssetGuid,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            candidate.ProgramId,
                            revision.ProgramId,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            candidate.SourceRevision,
                            revision.SourceRevision,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            candidate.ProgramHash,
                            revision.ProgramHash,
                            StringComparison.Ordinal))
                    .ToArray();
            cached = fixedMatches.Length == 1
                ? new ActionAnimationNumericTargetView(
                    FixedSimulationNumericProfile.Value.Id.Value,
                    FixedSimulationNumericProfile.Value.AbiVersion.Value)
                : default;
            s_NumericTargets[key] = cached;
            return cached;
        }

        static T[] Copy<T>(AnimationReadOnlyBuffer<T> source)
        {
            var result = new T[source.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = source[i];
            return result;
        }
    }
}
