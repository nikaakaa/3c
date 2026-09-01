using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    internal readonly struct CharacterPoseActorCommittedDiagnosticsView
    {
        internal CharacterPoseActorCommittedDiagnosticsView(
            CharacterPoseActorCommittedDiagnosticsProjector.Page page)
        {
            m_Page = page ?? throw new ArgumentNullException(nameof(page));
            m_Identity = page.Identity;
            if (!IsValid)
            {
                throw new ArgumentException(
                    "Pose actor committed diagnostics are invalid.",
                    nameof(page));
            }
        }

        readonly CharacterPoseActorCommittedDiagnosticsProjector.Page m_Page;
        readonly ulong m_Identity;
        internal bool IsValid =>
            m_Page != null &&
            m_Identity != 0 &&
            m_Page.Identity == m_Identity &&
            m_Page.Result.IsCompleted;
        internal CharacterPoseProgramResult Result
        {
            get
            {
                RequireValid();
                return m_Page.Result;
            }
        }
        internal int StackCount
        {
            get
            {
                RequireValid();
                return m_Page.StackCount;
            }
        }
        internal int EntryCount
        {
            get
            {
                RequireValid();
                return m_Page.EntryCount;
            }
        }
        internal int AnimationSlotCount
        {
            get
            {
                RequireValid();
                return m_Page.AnimationSlotCount;
            }
        }
        internal int PoseStateMachineCount
        {
            get
            {
                RequireValid();
                return m_Page.PoseStateMachineCount;
            }
        }
        internal int InertializationCount
        {
            get
            {
                RequireValid();
                return m_Page.InertializationCount;
            }
        }
        internal int BlendSpacePlayerCount
        {
            get
            {
                RequireValid();
                return m_Page.BlendSpacePlayerCount;
            }
        }
        internal int BlendSpaceSampleCount
        {
            get
            {
                RequireValid();
                return m_Page.BlendSpaceSampleCount;
            }
        }
        internal int RootOrientationWarpCount
        {
            get
            {
                RequireValid();
                return m_Page.RootOrientationWarpCount;
            }
        }

        internal void CopyBlendStacks(
            AnimationBlendStackSnapshot[] stacks,
            AnimationBlendStackEntrySnapshot[] entries,
            float[] entryBoneWeights,
            float[] storedBoneWeights)
        {
            RequireValid();
            int entryBoneWeightCount =
                checked(m_Page.EntryCount * m_Page.BoneCount);
            int storedBoneWeightCount =
                checked(m_Page.StackCount * m_Page.BoneCount);
            if (stacks == null || stacks.Length < m_Page.StackCount ||
                entries == null || entries.Length < m_Page.EntryCount ||
                entryBoneWeights == null ||
                entryBoneWeights.Length < entryBoneWeightCount ||
                storedBoneWeights == null ||
                storedBoneWeights.Length < storedBoneWeightCount)
            {
                throw new ArgumentException(
                    "Pose actor Blend Stack diagnostics destination is invalid.");
            }
            Array.Copy(m_Page.Stacks, 0, stacks, 0, m_Page.StackCount);
            Array.Copy(m_Page.Entries, 0, entries, 0, m_Page.EntryCount);
            Array.Copy(
                m_Page.EntryBoneWeights,
                0,
                entryBoneWeights,
                0,
                entryBoneWeightCount);
            Array.Copy(
                m_Page.StoredBoneWeights,
                0,
                storedBoneWeights,
                0,
                storedBoneWeightCount);
        }

        internal void CopyAnimationSlots(
            AnimationSlotRuntimeSnapshot[] animationSlots)
        {
            RequireValid();
            if (animationSlots == null ||
                animationSlots.Length < m_Page.AnimationSlotCount)
            {
                throw new ArgumentException(
                    "Pose actor Animation Slot diagnostics destination is invalid.");
            }
            Array.Copy(
                m_Page.AnimationSlots,
                0,
                animationSlots,
                0,
                m_Page.AnimationSlotCount);
        }

        internal void CopyPoseStateMachines(
            PoseStateMachineRuntimeSnapshot[] stateMachines)
        {
            RequireValid();
            if (stateMachines == null ||
                stateMachines.Length < m_Page.PoseStateMachineCount)
            {
                throw new ArgumentException(
                    "Pose actor StateMachine diagnostics destination is invalid.");
            }
            Array.Copy(
                m_Page.PoseStateMachines,
                0,
                stateMachines,
                0,
                m_Page.PoseStateMachineCount);
        }

        internal void CopyInertializations(
            PoseInertializationSnapshot[] inertializations,
            Vector3[] positionResiduals,
            Vector3[] rotationResiduals,
            Vector3[] scaleResiduals,
            float[] boneEnvelopes)
        {
            RequireValid();
            int boneValueCount =
                checked(m_Page.InertializationCount * m_Page.BoneCount);
            if (inertializations == null ||
                inertializations.Length < m_Page.InertializationCount ||
                positionResiduals == null ||
                positionResiduals.Length < boneValueCount ||
                rotationResiduals == null ||
                rotationResiduals.Length < boneValueCount ||
                scaleResiduals == null ||
                scaleResiduals.Length < boneValueCount ||
                boneEnvelopes == null ||
                boneEnvelopes.Length < boneValueCount)
            {
                throw new ArgumentException(
                    "Pose actor Inertialization diagnostics destination is invalid.");
            }
            Array.Copy(
                m_Page.Inertializations,
                0,
                inertializations,
                0,
                m_Page.InertializationCount);
            Array.Copy(
                m_Page.InertialPositionResiduals,
                0,
                positionResiduals,
                0,
                boneValueCount);
            Array.Copy(
                m_Page.InertialRotationResiduals,
                0,
                rotationResiduals,
                0,
                boneValueCount);
            Array.Copy(
                m_Page.InertialScaleResiduals,
                0,
                scaleResiduals,
                0,
                boneValueCount);
            Array.Copy(
                m_Page.InertialBoneEnvelopes,
                0,
                boneEnvelopes,
                0,
                boneValueCount);
        }

        internal AnimationFootStepObservationRuntimeSnapshot
            ResolveFootStepObservation(
            AnimationPoseSourceId sourceId,
            float sourceWeight)
        {
            RequireValid();
            if (!sourceId.IsValid)
                return default;
            for (int i = 0;
                 i < m_Page.ClipFootObservationCount;
                 i++)
            {
                AnimationFootStepObservationRuntimeSnapshot observation =
                    m_Page.ClipFootObservations[i];
                if (observation.SourceId.Equals(sourceId))
                {
                    return observation.WithSourceWeight(sourceWeight);
                }
            }
            return default;
        }

        internal void CopyBlendSpaces(
            AnimationBlendSpacePlayerRuntimeSnapshot[] players,
            AnimationBlendSpaceSampleRuntimeSnapshot[] samples)
        {
            RequireValid();
            if (players == null ||
                players.Length < m_Page.BlendSpacePlayerCount ||
                samples == null ||
                samples.Length < m_Page.BlendSpaceSampleCount)
            {
                throw new ArgumentException(
                    "Pose actor Blend Space diagnostics destination is invalid.");
            }
            Array.Copy(
                m_Page.BlendSpacePlayers,
                0,
                players,
                0,
                m_Page.BlendSpacePlayerCount);
            Array.Copy(
                m_Page.BlendSpaceSamples,
                0,
                samples,
                0,
                m_Page.BlendSpaceSampleCount);
        }

        internal RootOrientationWarpRuntimeSnapshot GetRootOrientationWarp(
            int index)
        {
            RequireValid();
            if ((uint)index >= (uint)m_Page.RootOrientationWarpCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Page.RootOrientationWarps[index];
        }

        void RequireValid()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException(
                    "Pose actor committed diagnostics lease is stale.");
            }
        }
    }

    internal sealed class CharacterPoseActorCommittedDiagnosticsProjector
    {
        internal sealed class Page
        {
            internal Page(
                CharacterPresentationProjection projection,
                in AnimationPoseNativeAggregateLayout layout)
            {
                CharacterPresentationPosePlan program =
                    projection.PosePlan;
                int entryCapacity = 0;
                for (int i = 0; i < program.BlendNodes.Count; i++)
                {
                    entryCapacity = checked(
                        entryCapacity +
                        program.BlendNodes[i]
                            .StackPolicy.MaxActiveSourceEntries);
                }
                BoneCount = layout.BoneCount;
                Stacks =
                    new AnimationBlendStackSnapshot[
                        program.BlendNodes.Count];
                Entries =
                    new AnimationBlendStackEntrySnapshot[entryCapacity];
                EntryBoneWeights =
                    new float[checked(entryCapacity * BoneCount)];
                StoredBoneWeights =
                    new float[checked(Stacks.Length * BoneCount)];
                AnimationSlots =
                    new AnimationSlotRuntimeSnapshot[
                        program.AnimationSlots.Count];
                PoseStateMachines =
                    new PoseStateMachineRuntimeSnapshot[
                        program.StateMachines.Count];
                Inertializations =
                    new PoseInertializationSnapshot[
                        program.Inertializations.Count];
                int inertialBoneValueCount = checked(
                    program.Inertializations.Count * BoneCount);
                InertialPositionResiduals =
                    new Vector3[inertialBoneValueCount];
                InertialRotationResiduals =
                    new Vector3[inertialBoneValueCount];
                InertialScaleResiduals =
                    new Vector3[inertialBoneValueCount];
                InertialBoneEnvelopes =
                    new float[inertialBoneValueCount];
                ClipFootObservations =
                    new AnimationFootStepObservationRuntimeSnapshot[
                        program.ClipPlayers.Count];
                BlendSpacePlayers =
                    new AnimationBlendSpacePlayerRuntimeSnapshot[
                        projection.BlendSpacePlayers.Count];
                int blendSpaceSampleCapacity = 0;
                for (int i = 0;
                     i < projection.BlendSpacePlayers.Count;
                     i++)
                {
                    int planIndex =
                        projection.BlendSpacePlayers[i]
                            .BlendSpacePlanIndex;
                    blendSpaceSampleCapacity = checked(
                        blendSpaceSampleCapacity +
                        projection.BlendSpaces[planIndex]
                            .Samples.Count);
                }
                BlendSpaceSamples =
                    new AnimationBlendSpaceSampleRuntimeSnapshot[
                        blendSpaceSampleCapacity];
                RootOrientationWarps =
                    new RootOrientationWarpRuntimeSnapshot[
                        program.RootOrientationWarps.Count];
            }

            internal ulong Identity;
            internal CharacterPoseProgramResult Result;
            internal int StackCount;
            internal int EntryCount;
            internal int AnimationSlotCount;
            internal int PoseStateMachineCount;
            internal int InertializationCount;
            internal int ClipFootObservationCount;
            internal int BlendSpacePlayerCount;
            internal int BlendSpaceSampleCount;
            internal int RootOrientationWarpCount;
            internal readonly int BoneCount;
            internal readonly AnimationBlendStackSnapshot[] Stacks;
            internal readonly AnimationBlendStackEntrySnapshot[] Entries;
            internal readonly float[] EntryBoneWeights;
            internal readonly float[] StoredBoneWeights;
            internal readonly AnimationSlotRuntimeSnapshot[] AnimationSlots;
            internal readonly PoseStateMachineRuntimeSnapshot[]
                PoseStateMachines;
            internal readonly PoseInertializationSnapshot[] Inertializations;
            internal readonly Vector3[] InertialPositionResiduals;
            internal readonly Vector3[] InertialRotationResiduals;
            internal readonly Vector3[] InertialScaleResiduals;
            internal readonly float[] InertialBoneEnvelopes;
            internal readonly AnimationFootStepObservationRuntimeSnapshot[]
                ClipFootObservations;
            internal readonly AnimationBlendSpacePlayerRuntimeSnapshot[]
                BlendSpacePlayers;
            internal readonly AnimationBlendSpaceSampleRuntimeSnapshot[]
                BlendSpaceSamples;
            internal readonly RootOrientationWarpRuntimeSnapshot[]
                RootOrientationWarps;
        }

        readonly Page m_Page;
        readonly CharacterPresentationPosePlan m_Program;
        ulong m_NextIdentity = 1;

        internal CharacterPoseActorCommittedDiagnosticsProjector(
            CharacterPresentationProjection projection,
            in AnimationPoseNativeAggregateLayout layout)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            CharacterPresentationPosePlan program =
                projection.PosePlan;
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            program.RequireValid();
            layout.RequireValid();
            m_Program = program;
            m_Page = new Page(projection, in layout);
        }

        internal void BeginFrame()
        {
            m_Page.Identity = 0;
            m_Page.Result = default;
            m_Page.StackCount = 0;
            m_Page.EntryCount = 0;
            m_Page.AnimationSlotCount = 0;
            m_Page.PoseStateMachineCount = 0;
            m_Page.InertializationCount = 0;
            m_Page.ClipFootObservationCount = 0;
            m_Page.BlendSpacePlayerCount = 0;
            m_Page.BlendSpaceSampleCount = 0;
            m_Page.RootOrientationWarpCount = 0;
        }

        internal CharacterPoseActorCommittedDiagnosticsView Capture(
            in CharacterPoseProgramResult result,
            IReadOnlyList<AnimationBlendStackRuntime> stacks,
            IReadOnlyList<CharacterAnimationTransitionRouteRuntime> routes,
            IReadOnlyList<CharacterPoseStateMachineRuntime> stateMachines,
            PoseInertializationNativeProgram inertializations,
            IReadOnlyList<AnimationClipPlayerRuntime> clipPlayers,
            IReadOnlyList<AnimationBlendSpacePlayerRuntime>
                blendSpacePlayers,
            IReadOnlyList<RootOrientationWarpRuntime> rootOrientationWarps,
            AnimationPresentationDiagnosticsInterest interest)
        {
            if (!result.IsCompleted || m_Page.Identity != 0)
            {
                throw new InvalidOperationException(
                    "Pose actor committed diagnostics request is invalid.");
            }
            m_Page.Result = result;
            if ((interest &
                 (AnimationPresentationDiagnosticsInterest.LiveState |
                  AnimationPresentationDiagnosticsInterest.Capture)) != 0)
            {
                if (stacks == null ||
                    stacks.Count != m_Page.Stacks.Length ||
                    routes == null || routes.Count != stacks.Count ||
                    stateMachines == null ||
                    stateMachines.Count !=
                    m_Page.PoseStateMachines.Length ||
                    inertializations == null ||
                    clipPlayers == null ||
                    clipPlayers.Count !=
                    m_Page.ClipFootObservations.Length ||
                    blendSpacePlayers == null ||
                    blendSpacePlayers.Count !=
                    m_Page.BlendSpacePlayers.Length ||
                    rootOrientationWarps == null ||
                    rootOrientationWarps.Count !=
                    m_Page.RootOrientationWarps.Length)
                {
                    throw new InvalidOperationException(
                        "Pose actor diagnostics layout is inconsistent.");
                }
                int entryOffset = 0;
                for (int stackIndex = 0;
                     stackIndex < stacks.Count;
                     stackIndex++)
                {
                    AnimationBlendStackRuntime stack = stacks[stackIndex];
                    stack.CopyDiagnostics(
                        stackIndex,
                        m_Page.Stacks,
                        m_Page.Entries,
                        entryOffset,
                        m_Page.EntryBoneWeights,
                        m_Page.StoredBoneWeights);
                    entryOffset = checked(entryOffset + stack.EntryCount);
                }
                m_Page.StackCount = stacks.Count;
                m_Page.EntryCount = entryOffset;
                Array.Clear(
                    m_Page.AnimationSlots,
                    0,
                    m_Page.AnimationSlots.Length);
                int animationSlotCount = 0;
                for (int routeIndex = 0;
                     routeIndex < routes.Count;
                     routeIndex++)
                {
                    CharacterAnimationTransitionRouteRuntime route =
                        routes[routeIndex];
                    if (!route.IsAnimationSlot)
                        continue;
                    int slotIndex = route.AnimationSlotIndex;
                    if ((uint)slotIndex >=
                            (uint)m_Page.AnimationSlots.Length ||
                        m_Page.Stacks[routeIndex].PoseNodeId !=
                        route.NodeId)
                    {
                        throw new InvalidOperationException(
                            "Animation Slot diagnostics layout is inconsistent.");
                    }
                    m_Page.AnimationSlots[slotIndex] =
                        route.CreateSlotSnapshot(
                            in m_Page.Stacks[routeIndex]);
                    animationSlotCount++;
                }
                if (animationSlotCount !=
                    m_Page.AnimationSlots.Length)
                {
                    throw new InvalidOperationException(
                        "Animation Slot diagnostics coverage is incomplete.");
                }
                m_Page.AnimationSlotCount = animationSlotCount;
                for (int i = 0; i < stateMachines.Count; i++)
                {
                    m_Page.PoseStateMachines[i] =
                        stateMachines[i].CreateSnapshot();
                }
                m_Page.PoseStateMachineCount = stateMachines.Count;
                CaptureInertializations(inertializations);
                for (int i = 0; i < rootOrientationWarps.Count; i++)
                {
                    m_Page.RootOrientationWarps[i] =
                        rootOrientationWarps[i]
                            .CreateDiagnosticsSnapshot();
                }
                m_Page.RootOrientationWarpCount =
                    rootOrientationWarps.Count;
                int clipFootObservationCount = 0;
                for (int i = 0; i < clipPlayers.Count; i++)
                {
                    AnimationClipPlayerRuntime player = clipPlayers[i];
                    if (!player.IsRelevant || !player.HasCompletedFrame)
                        continue;
                    m_Page.ClipFootObservations[
                            clipFootObservationCount++] =
                        player.CreateFootStepObservationSnapshot(0f);
                }
                m_Page.ClipFootObservationCount =
                    clipFootObservationCount;
                int blendSpaceSampleCount = 0;
                int blendSpacePlayerCount = 0;
                for (int i = 0; i < blendSpacePlayers.Count; i++)
                {
                    AnimationBlendSpacePlayerRuntime player =
                        blendSpacePlayers[i];
                    if (!player.IsRelevant || !player.HasCompletedFrame)
                        continue;
                    m_Page.BlendSpacePlayers[
                            blendSpacePlayerCount++] =
                        player.CreateDiagnosticsSnapshot(
                            m_Page.BlendSpaceSamples,
                            ref blendSpaceSampleCount);
                }
                m_Page.BlendSpacePlayerCount =
                    blendSpacePlayerCount;
                m_Page.BlendSpaceSampleCount =
                    blendSpaceSampleCount;
            }
            m_Page.Identity = m_NextIdentity++;
            return new CharacterPoseActorCommittedDiagnosticsView(m_Page);
        }

        void CaptureInertializations(
            PoseInertializationNativeProgram program)
        {
            if (program.SlotNodeOffset != m_Program.Inertializations.Count ||
                program.Nodes.Length !=
                checked(
                    m_Program.Inertializations.Count +
                    m_Program.AnimationSlots.Count) ||
                program.BoneCount != m_Page.BoneCount)
            {
                throw new InvalidOperationException(
                    "Pose Inertialization diagnostics layout is inconsistent.");
            }
            for (int nodeIndex = 0;
                 nodeIndex < m_Program.Inertializations.Count;
                 nodeIndex++)
            {
                CharacterPresentationInertializationDescriptor descriptor =
                    m_Program.Inertializations[nodeIndex];
                PoseInertializationNativeState state =
                    program.States[nodeIndex];
                PoseInertializationMode mode = default;
                float duration = 0f;
                int sourceEndpointIndex = -1;
                int targetEndpointIndex = -1;
                int curveIndex = -1;
                int profileIndex = -1;
                if ((uint)state.ActiveRuleIndex <
                        (uint)program.Rules.Length &&
                    state.RuntimeState != 0 &&
                    state.RuntimeState !=
                    PoseInertializationRuntimeState.Reset &&
                    state.RuntimeState !=
                    PoseInertializationRuntimeState.Invalid)
                {
                    PoseInertializationNativeRule rule =
                        program.Rules[state.ActiveRuleIndex];
                    mode = rule.Mode;
                    duration = state.ActiveDurationSeconds;
                    sourceEndpointIndex = rule.SourceEndpointIndex;
                    targetEndpointIndex = rule.TargetEndpointIndex;
                    PoseInertializationNativeNode node =
                        program.Nodes[nodeIndex];
                    int descriptorRuleIndex =
                        state.ActiveRuleIndex - node.RuleOffset;
                    if ((uint)descriptorRuleIndex >=
                        (uint)descriptor.Rules.Count)
                    {
                        throw new InvalidOperationException(
                            "Pose Inertialization diagnostic rule layout is inconsistent.");
                    }
                    CharacterPresentationInertializationRuleDescriptor
                        descriptorRule =
                            descriptor.Rules[descriptorRuleIndex];
                    curveIndex = descriptorRule.CurveIndex;
                    profileIndex = descriptorRule.ProfileIndex;
                }
                m_Page.Inertializations[nodeIndex] =
                    new PoseInertializationSnapshot(
                        descriptor.NodeId,
                        descriptor.TemporalOwnerKind,
                        descriptor.InputOwnerNodeId,
                        descriptor.InputOwnerIndex,
                        state.RuntimeState,
                        state.LastEventIdentity,
                        state.LastReason,
                        state.LastResetReason,
                        state.LastResetSequence,
                        descriptor.PolicyId,
                        descriptor.PolicyRevision,
                        sourceEndpointIndex,
                        targetEndpointIndex,
                        curveIndex,
                        profileIndex,
                        state.PreviousEndpoint.IsValid
                            ? state.PreviousEndpoint.ToManaged()
                            : default,
                        state.CurrentEndpoint.IsValid
                            ? state.CurrentEndpoint.ToManaged()
                            : default,
                        state.PreviousContinuityIdentity,
                        state.CurrentContinuityIdentity,
                        mode,
                        state.ElapsedSeconds,
                        duration,
                        state.AccumulatorGeneration,
                        state.HistoryCompletionIdentity,
                        state.OutputCompletionIdentity);
                int offset = nodeIndex * m_Page.BoneCount;
                for (int boneIndex = 0;
                     boneIndex < m_Page.BoneCount;
                     boneIndex++)
                {
                    int index = offset + boneIndex;
                    m_Page.InertialPositionResiduals[index] =
                        program.PositionResiduals[index];
                    m_Page.InertialRotationResiduals[index] =
                        program.RotationResiduals[index];
                    m_Page.InertialScaleResiduals[index] =
                        program.ScaleResiduals[index];
                    m_Page.InertialBoneEnvelopes[index] =
                        program.GetBoneEnvelope(nodeIndex, boneIndex);
                }
            }
            m_Page.InertializationCount =
                m_Program.Inertializations.Count;
        }

        internal void Reset()
        {
            BeginFrame();
            Array.Clear(m_Page.Stacks, 0, m_Page.Stacks.Length);
            Array.Clear(m_Page.Entries, 0, m_Page.Entries.Length);
            Array.Clear(
                m_Page.EntryBoneWeights,
                0,
                m_Page.EntryBoneWeights.Length);
            Array.Clear(
                m_Page.StoredBoneWeights,
                0,
                m_Page.StoredBoneWeights.Length);
            Array.Clear(
                m_Page.AnimationSlots,
                0,
                m_Page.AnimationSlots.Length);
            Array.Clear(
                m_Page.PoseStateMachines,
                0,
                m_Page.PoseStateMachines.Length);
            Array.Clear(
                m_Page.Inertializations,
                0,
                m_Page.Inertializations.Length);
            Array.Clear(
                m_Page.InertialPositionResiduals,
                0,
                m_Page.InertialPositionResiduals.Length);
            Array.Clear(
                m_Page.InertialRotationResiduals,
                0,
                m_Page.InertialRotationResiduals.Length);
            Array.Clear(
                m_Page.InertialScaleResiduals,
                0,
                m_Page.InertialScaleResiduals.Length);
            Array.Clear(
                m_Page.InertialBoneEnvelopes,
                0,
                m_Page.InertialBoneEnvelopes.Length);
            Array.Clear(
                m_Page.ClipFootObservations,
                0,
                m_Page.ClipFootObservations.Length);
            Array.Clear(
                m_Page.BlendSpacePlayers,
                0,
                m_Page.BlendSpacePlayers.Length);
            Array.Clear(
                m_Page.BlendSpaceSamples,
                0,
                m_Page.BlendSpaceSamples.Length);
            Array.Clear(
                m_Page.RootOrientationWarps,
                0,
                m_Page.RootOrientationWarps.Length);
        }
    }

    internal sealed class AnimationPresentationRuntimeSnapshotPublisher : IDisposable
    {
        const int InterestOwnerCapacity = 16;
        const AnimationPresentationDiagnosticsInterest ExplicitOwnerMask =
            AnimationPresentationDiagnosticsInterest.LiveState |
            AnimationPresentationDiagnosticsInterest.Capture |
            AnimationPresentationDiagnosticsInterest.OperationDetail |
            AnimationPresentationDiagnosticsInterest.FinalPoseDetail;

        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterPresentationPosePlan m_Program;
        readonly Page[] m_Pages;
        readonly Guid[] m_InterestOwnerIds = new Guid[InterestOwnerCapacity];
        readonly AnimationPresentationDiagnosticsInterest[] m_OwnerInterests =
            new AnimationPresentationDiagnosticsInterest[InterestOwnerCapacity];
        readonly int[] m_OwnerPoseWatchCounts = new int[InterestOwnerCapacity];
        readonly AnimationPoseWatchIdentity[] m_OwnerPoseWatches =
            new AnimationPoseWatchIdentity[checked(InterestOwnerCapacity * AnimationPoseWatchCapacity.PerWindow)];
        readonly AnimationPoseWatchIdentity[] m_MergedPoseWatchInterests =
            new AnimationPoseWatchIdentity[AnimationPoseWatchCapacity.PerTarget];
        readonly AnimationPoseWatchIdentity[] m_PoseWatchMergeScratch =
            new AnimationPoseWatchIdentity[AnimationPoseWatchCapacity.PerTarget];
        AnimationPresentationDiagnosticsInterest m_Interest;
        int m_MergedPoseWatchInterestCount;
        int m_ActivePageIndex = -1;
        int m_PendingPageIndex = -1;
        ulong m_PendingCompletionIdentity;
        ulong m_NoInterestSkipCount;
        AnimationPresentationRuntimeSnapshot m_Current;
        bool m_Disposed;

        internal AnimationPresentationRuntimeSnapshotPublisher(
            CharacterPresentationProjection projection,
            in AnimationPoseNativeAggregateLayout layout,
            int physicalSourceCapacity)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Program = projection.PosePlan ?? throw new ArgumentException("Animation Pose Program is missing.", nameof(projection));
            m_Program.RequireValid();
            if (m_Program.FullBodyIks.Count != 1)
                throw new ArgumentException("FullBodyIK diagnostics solver layout is inconsistent.", nameof(projection));
            layout.RequireValid();
            if (physicalSourceCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(physicalSourceCapacity));
            int entryCapacity = 0;
            for (int i = 0; i < projection.PosePlan.BlendNodes.Count; i++)
                entryCapacity = checked(entryCapacity + projection.PosePlan.BlendNodes[i].StackPolicy.MaxActiveSourceEntries);
            int blendSpacePlayerCapacity = projection.BlendSpacePlayers.Count;
            int blendSpaceSampleCapacity = 0;
            for (int i = 0; i < projection.BlendSpacePlayers.Count; i++)
            {
                int planIndex =
                    projection.BlendSpacePlayers[i].BlendSpacePlanIndex;
                blendSpaceSampleCapacity = checked(
                    blendSpaceSampleCapacity +
                    projection.BlendSpaces[planIndex].Samples.Count);
            }
            m_Pages = new[]
            {
                new Page(m_Program, projection.Rig, layout, entryCapacity, physicalSourceCapacity, blendSpacePlayerCapacity, blendSpaceSampleCapacity, projection.LinkedPose.Groups.Count),
                new Page(m_Program, projection.Rig, layout, entryCapacity, physicalSourceCapacity, blendSpacePlayerCapacity, blendSpaceSampleCapacity, projection.LinkedPose.Groups.Count)
            };
        }

        internal AnimationPresentationRuntimeSnapshot Current
        {
            get
            {
                RequireAlive();
                return m_Current;
            }
        }

        internal bool HasCurrent
        {
            get
            {
                RequireAlive();
                return m_ActivePageIndex >= 0;
            }
        }

        internal AnimationPresentationDiagnosticsInterest Interest
        {
            get
            {
                RequireAlive();
                return m_Interest;
            }
        }

        internal bool HasInterest =>
            Interest != AnimationPresentationDiagnosticsInterest.None;

        internal ulong NoInterestSkipCount
        {
            get
            {
                RequireAlive();
                return m_NoInterestSkipCount;
            }
        }

        internal bool HasPendingFrame
        {
            get
            {
                RequireAlive();
                return m_PendingPageIndex >= 0;
            }
        }

        internal void RecordNoInterestSkip()
        {
            RequireAlive();
            if (m_NoInterestSkipCount != ulong.MaxValue)
                m_NoInterestSkipCount++;
        }

        internal void BeginFrame(
            in CharacterPoseFrameExecutionResult executionResult,
            in CharacterPoseSourceCommittedDiagnosticsView sourceDiagnostics,
            in CharacterPoseProgramCommittedDiagnosticsView programDiagnostics,
            in CharacterLinkedPoseCommittedDiagnosticsView
                linkedPoseDiagnostics,
            in CharacterPoseActorCommittedDiagnosticsView actorDiagnostics,
            in CharacterPoseConstraintCommittedDiagnosticsView
                constraintDiagnostics,
            in CharacterFinalPoseCommittedDiagnosticsView
                publicationDiagnostics,
            AnimationPresentationDiagnosticsInterest interest)
        {
            RequireAlive();
            RequireValidFrameInterest(interest);
            ComposedAnimationPoseFrame finalFrame =
                publicationDiagnostics.Frame;
            if (!executionResult.IsPublished ||
                !sourceDiagnostics.IsValid ||
                sourceDiagnostics.Result.Lineage !=
                executionResult.Lineage ||
                !programDiagnostics.IsValid ||
                programDiagnostics.Result.Lineage !=
                executionResult.Lineage ||
                !linkedPoseDiagnostics.IsValid ||
                linkedPoseDiagnostics.Result.Lineage !=
                executionResult.Lineage ||
                !actorDiagnostics.IsValid ||
                actorDiagnostics.Result.Lineage !=
                executionResult.Lineage ||
                !constraintDiagnostics.IsValid ||
                constraintDiagnostics.Result.Lineage !=
                executionResult.Lineage ||
                !publicationDiagnostics.IsValid ||
                publicationDiagnostics.Result.Lineage !=
                executionResult.Lineage ||
                executionResult.Lineage.CompletionIdentity !=
                finalFrame.CompletionIdentity)
                throw new ArgumentException("Animation runtime diagnostics frame inputs are inconsistent.");
            AnimationPhysicalBoneWriteDiagnostics physicalWrite =
                publicationDiagnostics.PhysicalWrite;
            if (m_PendingPageIndex >= 0)
                throw new InvalidOperationException("Animation runtime diagnostics has an unpublished frame.");

            int pageIndex = m_ActivePageIndex == 0 ? 1 : 0;
            Page page = m_Pages[pageIndex];
            page.Lease.Invalidate();
            page.ClearCounts();
            page.CompletionIdentity =
                executionResult.Lineage.CompletionIdentity;
            page.Interest = interest;

            if (RequiresBasicState(interest))
            {
                CopySourceReleases(page, in sourceDiagnostics);
                CopyBlendStacks(page, in actorDiagnostics);
                CopyPoseStateMachines(
                    page,
                    in actorDiagnostics,
                    in programDiagnostics);
                CopyRootOrientationWarps(page, in actorDiagnostics);
                CopyInertializations(page, in actorDiagnostics);
                CopySlotContributions(
                    page,
                    in programDiagnostics,
                    in sourceDiagnostics);
                CopyFootPlacement(
                    page,
                    in constraintDiagnostics,
                    in physicalWrite);
            }
            if (RequiresOperationDetail(interest))
                CopyOperations(
                    page,
                    in programDiagnostics,
                    in sourceDiagnostics);
            CopyLinkedPose(
                page,
                in programDiagnostics,
                in linkedPoseDiagnostics);
            if ((interest & AnimationPresentationDiagnosticsInterest.PoseWatch) != 0)
                CopyPoseWatches(
                    page,
                    in programDiagnostics,
                    in sourceDiagnostics,
                    in constraintDiagnostics);
            CopyFinalSummary(
                page,
                in executionResult,
                in finalFrame);
            if (RequiresFinalPoseDetail(interest))
                CopyFinalDetail(page, in finalFrame);
            if (RequiresBasicState(interest))
            {
                CopyBlendSpaces(page, in actorDiagnostics);
                page.FootStepObservation =
                    ResolveFootStepObservation(
                        page,
                        in actorDiagnostics);
            }
            m_PendingPageIndex = pageIndex;
            m_PendingCompletionIdentity =
                executionResult.Lineage.CompletionIdentity;
        }

        void CopyLinkedPose(
            Page page,
            in CharacterPoseProgramCommittedDiagnosticsView
                programDiagnostics,
            in CharacterLinkedPoseCommittedDiagnosticsView
                linkedPoseDiagnostics)
        {
            if (linkedPoseDiagnostics.GroupCount !=
                    page.LinkedPoseGroups.Length ||
                m_Program.LinkedPoseCalls.Count != page.LinkedPoseEntries.Length)
            {
                throw new InvalidOperationException("Linked Pose diagnostics layout is inconsistent.");
            }
            for (int groupIndex = 0;
                 groupIndex < linkedPoseDiagnostics.GroupCount;
                 groupIndex++)
            {
                page.LinkedPoseGroups[groupIndex] =
                    linkedPoseDiagnostics.GetGroup(groupIndex);
            }
            page.LinkedPoseGroupCount = linkedPoseDiagnostics.GroupCount;

            for (int callIndex = 0; callIndex < m_Program.LinkedPoseCalls.Count; callIndex++)
            {
                CharacterLinkedPoseCallPlanDescriptor call = m_Program.LinkedPoseCalls[callIndex];
                CharacterLinkedPoseRuntimeGroupSnapshot group = RequireLinkedPoseGroup(page, call.GroupId);
                CharacterLinkedPoseEntryFragmentPlanDescriptor fragment =
                    RequireLinkedPoseFragment(call, group.ImplementationId);
                CharacterPresentationPoseOperation callOperation = RequireLinkedPoseCallOperation(call.Index);
                CharacterPoseOperationCompletion completion =
                    programDiagnostics.GetOperationCompletion(
                        callOperation.Index);
                ulong completionIdentity = completion.CompletionIdentity;
                bool completed = completion.Matches(
                    programDiagnostics.Result.Lineage.CompletionIdentity);
                for (int operationIndex = fragment.OperationStart;
                     completed && operationIndex < fragment.OperationStart + fragment.OperationCount;
                     operationIndex++)
                {
                    completed = programDiagnostics.GetOperationCompletion(
                        operationIndex).Matches(
                        programDiagnostics.Result.Lineage.CompletionIdentity);
                }

                page.LinkedPoseEntries[callIndex] = new AnimationLinkedPoseEntryRuntimeSnapshot(
                    call.GroupId,
                    call.InterfaceId,
                    call.InterfaceSignature,
                    call.EntryId,
                    call.NodeId,
                    group.ImplementationId,
                    group.Generation,
                    group.StateReset,
                    fragment.Index,
                    fragment.OperationStart,
                    fragment.OperationCount,
                    fragment.StageStart,
                    fragment.StageCount,
                    fragment.SourceIndices.Count,
                    completionIdentity,
                    completed);
            }
            page.LinkedPoseEntryCount = m_Program.LinkedPoseCalls.Count;
        }

        static CharacterLinkedPoseRuntimeGroupSnapshot RequireLinkedPoseGroup(
            Page page,
            LinkedPoseGroupId groupId)
        {
            for (int i = 0; i < page.LinkedPoseGroupCount; i++)
            {
                CharacterLinkedPoseRuntimeGroupSnapshot group = page.LinkedPoseGroups[i];
                if (group.GroupId == groupId)
                    return group;
            }
            throw new InvalidOperationException($"Linked Pose diagnostics Group '{groupId}' is absent.");
        }

        CharacterLinkedPoseEntryFragmentPlanDescriptor RequireLinkedPoseFragment(
            CharacterLinkedPoseCallPlanDescriptor call,
            LinkedPoseImplementationId implementationId)
        {
            for (int i = 0; i < call.FragmentIndices.Count; i++)
            {
                CharacterLinkedPoseEntryFragmentPlanDescriptor fragment =
                    m_Program.LinkedPoseFragments[call.FragmentIndices[i]];
                if (fragment.ImplementationId == implementationId)
                    return fragment;
            }
            throw new InvalidOperationException(
                $"Linked Pose Call '{call.NodeId}' has no fragment for Implementation '{implementationId}'.");
        }

        CharacterPresentationPoseOperation RequireLinkedPoseCallOperation(int callIndex)
        {
            for (int i = 0; i < m_Program.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = m_Program.Operations[i];
                if (operation.Code == CharacterPoseOperationCode.LinkedPoseCall &&
                    operation.LinkedPoseCallIndex == callIndex)
                {
                    return operation;
                }
            }
            throw new InvalidOperationException($"Linked Pose Call #{callIndex} has no root operation.");
        }

        internal void DiscardPendingFrame()
        {
            RequireAlive();
            if (m_PendingPageIndex < 0)
                return;
            Page page = m_Pages[m_PendingPageIndex];
            page.Lease.Invalidate();
            page.ClearCounts();
            m_PendingPageIndex = -1;
            m_PendingCompletionIdentity = 0;
        }

        static void CopyBlendStacks(
            Page page,
            in CharacterPoseActorCommittedDiagnosticsView actorDiagnostics)
        {
            if (actorDiagnostics.StackCount > page.Stacks.Length ||
                actorDiagnostics.EntryCount > page.Entries.Length ||
                actorDiagnostics.AnimationSlotCount !=
                page.AnimationSlots.Length)
            {
                throw new InvalidOperationException(
                    "Pose actor Blend Stack diagnostics coverage is incomplete.");
            }
            actorDiagnostics.CopyBlendStacks(
                page.Stacks,
                page.Entries,
                page.EntryBoneWeights,
                page.StoredBoneWeights);
            actorDiagnostics.CopyAnimationSlots(page.AnimationSlots);
            page.StackCount = actorDiagnostics.StackCount;
            page.EntryCount = actorDiagnostics.EntryCount;
            page.AnimationSlotCount = actorDiagnostics.AnimationSlotCount;
        }

        static void CopyPoseStateMachines(
            Page page,
            in CharacterPoseActorCommittedDiagnosticsView actorDiagnostics,
            in CharacterPoseProgramCommittedDiagnosticsView programDiagnostics)
        {
            if (actorDiagnostics.PoseStateMachineCount !=
                page.PoseStateMachines.Length)
                throw new InvalidOperationException("Pose StateMachine diagnostics coverage is incomplete.");
            actorDiagnostics.CopyPoseStateMachines(page.PoseStateMachines);
            for (int i = 0;
                 i < actorDiagnostics.PoseStateMachineCount;
                 i++)
            {
                for (int bone = 0; bone < page.PoseBoneCount; bone++)
                {
                    page.PoseStateMachineBoneWeights[
                        i * page.PoseBoneCount + bone] =
                        programDiagnostics.GetStateMachineBoneWeight(i, bone);
                }
            }
            page.PoseStateMachineCount =
                actorDiagnostics.PoseStateMachineCount;
        }

        static void CopyRootOrientationWarps(
            Page page,
            in CharacterPoseActorCommittedDiagnosticsView actorDiagnostics)
        {
            if (actorDiagnostics.RootOrientationWarpCount !=
                page.RootOrientationWarps.Length)
                throw new InvalidOperationException("Root Orientation Warp diagnostics coverage is incomplete.");
            for (int i = 0;
                 i < actorDiagnostics.RootOrientationWarpCount;
                 i++)
            {
                page.RootOrientationWarps[i] =
                    actorDiagnostics.GetRootOrientationWarp(i);
            }
            page.RootOrientationWarpCount =
                actorDiagnostics.RootOrientationWarpCount;
        }

        static void CopyBlendSpaces(
            Page page,
            in CharacterPoseActorCommittedDiagnosticsView actorDiagnostics)
        {
            if (actorDiagnostics.BlendSpacePlayerCount >
                    page.BlendSpacePlayers.Length ||
                actorDiagnostics.BlendSpaceSampleCount >
                    page.BlendSpaceSamples.Length)
            {
                throw new InvalidOperationException(
                    "Animation Blend Space diagnostics fixed capacity was exceeded.");
            }
            actorDiagnostics.CopyBlendSpaces(
                page.BlendSpacePlayers,
                page.BlendSpaceSamples);
            for (int i = 0;
                 i < actorDiagnostics.BlendSpacePlayerCount;
                 i++)
            {
                AnimationBlendSpacePlayerRuntimeSnapshot player =
                    page.BlendSpacePlayers[i];
                for (int operationIndex = 0;
                     operationIndex < page.OperationCount;
                     operationIndex++)
                {
                    AnimationPoseOperationSnapshot operation =
                        page.Operations[operationIndex];
                    if (operation.Code !=
                            CharacterPoseOperationCode.BlendSpacePlayer ||
                        !operation.NodeId.Equals(player.NodeId))
                        continue;
                    player = player.WithPoseResult(
                        operation.Availability,
                        operation.InvalidReason);
                    break;
                }
                page.BlendSpacePlayers[i] = player;
            }
            Array.Clear(
                page.BlendSpacePlayers,
                actorDiagnostics.BlendSpacePlayerCount,
                page.BlendSpacePlayers.Length -
                actorDiagnostics.BlendSpacePlayerCount);
            Array.Clear(
                page.BlendSpaceSamples,
                actorDiagnostics.BlendSpaceSampleCount,
                page.BlendSpaceSamples.Length -
                actorDiagnostics.BlendSpaceSampleCount);
            page.BlendSpacePlayerCount =
                actorDiagnostics.BlendSpacePlayerCount;
            page.BlendSpaceSampleCount =
                actorDiagnostics.BlendSpaceSampleCount;
        }

        static void CopySourceReleases(
            Page page,
            in CharacterPoseSourceCommittedDiagnosticsView sourceDiagnostics)
        {
            if (sourceDiagnostics.ReleaseCount > page.Releases.Length)
            {
                throw new InvalidOperationException(
                    "Animation diagnostics release capacity was exceeded.");
            }
            sourceDiagnostics.CopyReleases(page.Releases);
            Array.Clear(
                page.Releases,
                sourceDiagnostics.ReleaseCount,
                page.Releases.Length - sourceDiagnostics.ReleaseCount);
            page.ReleaseCount = sourceDiagnostics.ReleaseCount;
        }

        internal AnimationPresentationRuntimeSnapshot Publish()
        {
            RequireAlive();
            if (m_PendingPageIndex < 0 || m_PendingCompletionIdentity == 0)
                throw new InvalidOperationException("Animation runtime diagnostics has no completed native frame.");
            Page page = m_Pages[m_PendingPageIndex];
            page.Lease.BeginWrite(m_PendingCompletionIdentity);
            m_Current = page.CreateSnapshot(m_Projection, m_Program, m_PendingCompletionIdentity);
            m_ActivePageIndex = m_PendingPageIndex;
            m_PendingPageIndex = -1;
            m_PendingCompletionIdentity = 0;
            return m_Current;
        }

        static AnimationFootStepObservationRuntimeSnapshot ResolveFootStepObservation(
            Page page,
            in CharacterPoseActorCommittedDiagnosticsView actorDiagnostics)
        {
            AnimationPoseSourceId sourceId = default;
            float sourceWeight = -1f;
            for (int i = 0; i < page.FinalContributionCount; i++)
            {
                AnimationPoseSourceContribution contribution = page.FinalContributions[i];
                if (contribution.Kind != AnimationPoseContributionKind.Live ||
                    contribution.Weight <= sourceWeight)
                {
                    continue;
                }
                sourceId = contribution.SourceId;
                sourceWeight = contribution.Weight;
            }
            if (!sourceId.IsValid)
                return default;
            return actorDiagnostics.ResolveFootStepObservation(
                sourceId,
                sourceWeight);
        }

        internal void Invalidate()
        {
            if (m_Disposed)
                return;
            for (int i = 0; i < m_Pages.Length; i++)
                m_Pages[i].Lease.Invalidate();
            m_Current = default;
            m_ActivePageIndex = -1;
            m_PendingPageIndex = -1;
            m_PendingCompletionIdentity = 0;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Invalidate();
            m_Disposed = true;
        }

        internal AnimationPresentationDiagnosticsInterest ResolveFrameInterest(
            AnimationPresentationDiagnosticsInterest transientInterest)
        {
            RequireAlive();
            if ((transientInterest & ~ExplicitOwnerMask) != 0)
                throw new ArgumentOutOfRangeException(nameof(transientInterest));
            return m_Interest | transientInterest;
        }

        internal void SetDiagnosticsInterest(
            Guid ownerId,
            AnimationPresentationDiagnosticsInterest interest)
        {
            RequireAlive();
            RequireInterestMutationAvailable();
            if (ownerId == Guid.Empty)
                throw new ArgumentException("Animation diagnostics owner identity is missing.", nameof(ownerId));
            if ((interest & ~ExplicitOwnerMask) != 0)
                throw new ArgumentOutOfRangeException(nameof(interest));
            if (interest == AnimationPresentationDiagnosticsInterest.None)
            {
                RemoveDiagnosticsInterest(ownerId);
                return;
            }
            int ownerIndex = FindOwner(ownerId);
            if (ownerIndex < 0)
            {
                ownerIndex = RequireFreeOwner();
                m_InterestOwnerIds[ownerIndex] = ownerId;
            }
            if (m_OwnerInterests[ownerIndex] == interest)
                return;
            m_OwnerInterests[ownerIndex] = interest;
            RebuildInterest(true);
        }

        internal void RemoveDiagnosticsInterest(Guid ownerId)
        {
            if (m_Disposed || ownerId == Guid.Empty)
                return;
            RequireInterestMutationAvailable();
            int ownerIndex = FindOwner(ownerId);
            if (ownerIndex < 0 || m_OwnerInterests[ownerIndex] == AnimationPresentationDiagnosticsInterest.None)
                return;
            m_OwnerInterests[ownerIndex] = AnimationPresentationDiagnosticsInterest.None;
            ReleaseOwnerIfEmpty(ownerIndex);
            RebuildInterest(true);
        }

        internal void SetPoseWatchInterests(Guid ownerId, IReadOnlyList<AnimationPoseWatchIdentity> interests)
        {
            RequireAlive();
            RequireInterestMutationAvailable();
            if (ownerId == Guid.Empty)
                throw new ArgumentException("Pose Watch owner identity is missing.", nameof(ownerId));
            int count = interests?.Count ?? 0;
            if (count > AnimationPoseWatchCapacity.PerWindow)
                throw new InvalidOperationException($"Pose Watch window capacity exceeded: {count}/{AnimationPoseWatchCapacity.PerWindow}.");
            if (count == 0)
            {
                RemovePoseWatchInterests(ownerId);
                return;
            }
            for (int i = 0; i < count; i++)
            {
                AnimationPoseWatchIdentity interest = interests[i];
                if (!interest.IsValid)
                    throw new ArgumentException("Pose Watch interests contain an invalid identity.", nameof(interests));
                for (int duplicateIndex = 0; duplicateIndex < i; duplicateIndex++)
                {
                    if (interest.Equals(interests[duplicateIndex]))
                        throw new ArgumentException("Pose Watch interests contain a duplicate identity.", nameof(interests));
                }
            }
            int ownerIndex = FindOwner(ownerId);
            if (ownerIndex < 0)
                ownerIndex = RequireFreeOwner();
            int mergedCount = BuildMergedPoseWatchScratch(ownerIndex, interests, count);
            int ownerOffset = checked(ownerIndex * AnimationPoseWatchCapacity.PerWindow);
            Array.Clear(m_OwnerPoseWatches, ownerOffset, AnimationPoseWatchCapacity.PerWindow);
            for (int i = 0; i < count; i++)
                m_OwnerPoseWatches[ownerOffset + i] = interests[i];
            m_InterestOwnerIds[ownerIndex] = ownerId;
            m_OwnerPoseWatchCounts[ownerIndex] = count;
            CommitMergedPoseWatchScratch(mergedCount);
            RebuildInterest(true);
        }

        internal void RemovePoseWatchInterests(Guid ownerId)
        {
            if (m_Disposed || ownerId == Guid.Empty)
                return;
            RequireInterestMutationAvailable();
            int ownerIndex = FindOwner(ownerId);
            if (ownerIndex < 0 || m_OwnerPoseWatchCounts[ownerIndex] == 0)
                return;
            int ownerOffset = checked(ownerIndex * AnimationPoseWatchCapacity.PerWindow);
            Array.Clear(m_OwnerPoseWatches, ownerOffset, AnimationPoseWatchCapacity.PerWindow);
            m_OwnerPoseWatchCounts[ownerIndex] = 0;
            ReleaseOwnerIfEmpty(ownerIndex);
            int mergedCount = BuildMergedPoseWatchScratch(-1, null, 0);
            CommitMergedPoseWatchScratch(mergedCount);
            RebuildInterest(true);
        }

        int BuildMergedPoseWatchScratch(
            int replacementOwnerIndex,
            IReadOnlyList<AnimationPoseWatchIdentity> replacement,
            int replacementCount)
        {
            Array.Clear(m_PoseWatchMergeScratch, 0, m_PoseWatchMergeScratch.Length);
            int mergedCount = 0;
            for (int ownerIndex = 0; ownerIndex < InterestOwnerCapacity; ownerIndex++)
            {
                int count = ownerIndex == replacementOwnerIndex
                    ? replacementCount
                    : m_OwnerPoseWatchCounts[ownerIndex];
                int ownerOffset = checked(ownerIndex * AnimationPoseWatchCapacity.PerWindow);
                for (int watchIndex = 0; watchIndex < count; watchIndex++)
                {
                    AnimationPoseWatchIdentity candidate = ownerIndex == replacementOwnerIndex
                        ? replacement[watchIndex]
                        : m_OwnerPoseWatches[ownerOffset + watchIndex];
                    bool duplicate = false;
                    for (int mergedIndex = 0; mergedIndex < mergedCount; mergedIndex++)
                    {
                        if (!m_PoseWatchMergeScratch[mergedIndex].Equals(candidate))
                            continue;
                        duplicate = true;
                        break;
                    }
                    if (duplicate)
                        continue;
                    if (mergedCount >= AnimationPoseWatchCapacity.PerTarget)
                        throw new InvalidOperationException($"Pose Watch target capacity exceeded: more than {AnimationPoseWatchCapacity.PerTarget} unique interests.");
                    int insertionIndex = mergedCount;
                    while (insertionIndex > 0 && ComparePoseWatch(
                               candidate,
                               m_PoseWatchMergeScratch[insertionIndex - 1]) < 0)
                    {
                        m_PoseWatchMergeScratch[insertionIndex] =
                            m_PoseWatchMergeScratch[insertionIndex - 1];
                        insertionIndex--;
                    }
                    m_PoseWatchMergeScratch[insertionIndex] = candidate;
                    mergedCount++;
                }
            }
            return mergedCount;
        }

        void CommitMergedPoseWatchScratch(int count)
        {
            Array.Clear(m_MergedPoseWatchInterests, 0, m_MergedPoseWatchInterests.Length);
            Array.Copy(m_PoseWatchMergeScratch, m_MergedPoseWatchInterests, count);
            m_MergedPoseWatchInterestCount = count;
        }

        static int ComparePoseWatch(
            AnimationPoseWatchIdentity left,
            AnimationPoseWatchIdentity right)
        {
            int comparison = string.Compare(left.GraphId, right.GraphId, StringComparison.Ordinal);
            if (comparison != 0)
                return comparison;
            comparison = string.Compare(left.GraphRevision, right.GraphRevision, StringComparison.Ordinal);
            if (comparison != 0)
                return comparison;
            comparison = string.Compare(left.NodeId.Value, right.NodeId.Value, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(left.CallSite, right.CallSite, StringComparison.Ordinal);
        }

        int FindOwner(Guid ownerId)
        {
            for (int i = 0; i < m_InterestOwnerIds.Length; i++)
            {
                if (m_InterestOwnerIds[i] == ownerId)
                    return i;
            }
            return -1;
        }

        int RequireFreeOwner()
        {
            for (int i = 0; i < m_InterestOwnerIds.Length; i++)
            {
                if (m_InterestOwnerIds[i] == Guid.Empty)
                    return i;
            }
            throw new InvalidOperationException($"Animation diagnostics owner capacity exceeded: {InterestOwnerCapacity}.");
        }

        void ReleaseOwnerIfEmpty(int ownerIndex)
        {
            if (m_OwnerInterests[ownerIndex] == AnimationPresentationDiagnosticsInterest.None &&
                m_OwnerPoseWatchCounts[ownerIndex] == 0)
                m_InterestOwnerIds[ownerIndex] = Guid.Empty;
        }

        void RebuildInterest(bool invalidateCurrent)
        {
            AnimationPresentationDiagnosticsInterest interest = AnimationPresentationDiagnosticsInterest.None;
            for (int i = 0; i < InterestOwnerCapacity; i++)
            {
                interest |= m_OwnerInterests[i];
                if (m_OwnerPoseWatchCounts[i] > 0)
                    interest |= AnimationPresentationDiagnosticsInterest.PoseWatch;
            }
            bool changed = interest != m_Interest;
            m_Interest = interest;
            if (invalidateCurrent && (changed || m_ActivePageIndex >= 0))
                Invalidate();
        }

        void RequireInterestMutationAvailable()
        {
            if (m_PendingPageIndex >= 0)
                throw new InvalidOperationException("Animation diagnostics interest cannot change while a committed frame copy is pending publication.");
        }

        static void CopyInertializations(
            Page page,
            in CharacterPoseActorCommittedDiagnosticsView actorDiagnostics)
        {
            if (actorDiagnostics.InertializationCount !=
                page.Inertializations.Length)
            {
                throw new InvalidOperationException(
                    "Pose Inertialization diagnostics coverage is incomplete.");
            }
            actorDiagnostics.CopyInertializations(
                page.Inertializations,
                page.InertialPositionResiduals,
                page.InertialRotationResiduals,
                page.InertialScaleResiduals,
                page.InertialBoneEnvelopes);
            page.InertializationCount =
                actorDiagnostics.InertializationCount;
        }

        void CopySlotContributions(
            Page page,
            in CharacterPoseProgramCommittedDiagnosticsView programDiagnostics,
            in CharacterPoseSourceCommittedDiagnosticsView sourceDiagnostics)
        {
            int destinationIndex = 0;
            for (int slotIndex = 0;
                 slotIndex < programDiagnostics.PlayerCount;
                 slotIndex++)
            {
                AnimationPlayerPoseNativeRange range =
                    programDiagnostics.GetSlotRange(slotIndex);
                int count =
                    programDiagnostics.GetSlotContributionCount(slotIndex);
                if (count < 0 || count > range.ContributionCapacity)
                    throw new InvalidOperationException($"Pose Slot #{slotIndex} contribution count is invalid.");
                for (int i = 0; i < count; i++)
                {
                    int flatContributionIndex =
                        range.ContributionOffset + i;
                    AnimationPrimitivePoseContribution primitive =
                        programDiagnostics.GetSlotContribution(
                            flatContributionIndex);
                    page.SlotContributions[destinationIndex] =
                        ConvertContribution(
                            primitive,
                            in sourceDiagnostics);
                    for (int boneIndex = 0;
                         boneIndex < programDiagnostics.BoneCount;
                         boneIndex++)
                    {
                        page.SlotContributionBoneWeights[
                            destinationIndex * programDiagnostics.BoneCount +
                            boneIndex] =
                            programDiagnostics.GetSlotContributionBoneWeight(
                                flatContributionIndex,
                                boneIndex);
                    }
                    destinationIndex++;
                }
            }
            page.SlotContributionCount = destinationIndex;
        }

        void CopyOperations(
            Page page,
            in CharacterPoseProgramCommittedDiagnosticsView programDiagnostics,
            in CharacterPoseSourceCommittedDiagnosticsView sourceDiagnostics)
        {
            int contributionOffset = 0;
            int operationCount = 0;
            for (int i = 0; i < m_Program.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = m_Program.Operations[i];
                if (operation.OutputValueIndex < 0)
                    continue;
                CharacterPresentationPoseSourceMapEntry source = m_Program.SourceMap[i];
                int valueIndex = operation.OutputValueIndex;
                int contributionCount =
                    programDiagnostics.GetValueContributionCount(valueIndex);
                if (contributionCount < 0 ||
                    contributionCount >
                    programDiagnostics.PoseValueContributionStride)
                    throw new InvalidOperationException($"Animation Pose operation #{i} contribution count is invalid.");
                page.Operations[operationCount++] = new AnimationPoseOperationSnapshot(
                    i,
                    source.GraphId,
                    source.NodeId,
                    source.CallSite,
                    operation.Code,
                    programDiagnostics.GetValueAvailability(valueIndex),
                    programDiagnostics.GetValueInvalidReason(valueIndex),
                    programDiagnostics.GetValueOutputWeight(valueIndex),
                    programDiagnostics.GetValueContinuityIdentity(valueIndex),
                    programDiagnostics.GetOperationCompletion(
                        operation.Index).CompletionIdentity,
                    contributionOffset,
                    contributionCount);
                for (int contributionIndex = 0; contributionIndex < contributionCount; contributionIndex++)
                {
                    int destinationIndex = contributionOffset + contributionIndex;
                    page.OperationContributions[destinationIndex] = ConvertContribution(
                        programDiagnostics.GetValueContribution(
                            valueIndex,
                            contributionIndex),
                        in sourceDiagnostics);
                    for (int boneIndex = 0;
                         boneIndex < programDiagnostics.BoneCount;
                         boneIndex++)
                    {
                        page.OperationContributionBoneWeights[
                            destinationIndex * programDiagnostics.BoneCount +
                            boneIndex] =
                            programDiagnostics.GetValueContributionBoneWeight(
                                valueIndex,
                                contributionIndex,
                                boneIndex);
                    }
                }
                contributionOffset = checked(contributionOffset + contributionCount);
            }
            page.OperationCount = operationCount;
            page.OperationContributionCount = contributionOffset;
        }

        void CopyPoseWatches(
            Page page,
            in CharacterPoseProgramCommittedDiagnosticsView programDiagnostics,
            in CharacterPoseSourceCommittedDiagnosticsView sourceDiagnostics,
            in CharacterPoseConstraintCommittedDiagnosticsView
                constraintDiagnostics)
        {
            CharacterFootLandingPredictionDiagnostics footLandingPrediction =
                constraintDiagnostics.FootLandingPrediction;
            int boneCount = programDiagnostics.BoneCount;
            int stride =
                programDiagnostics.PoseValueContributionStride;
            ulong frameCompletion =
                programDiagnostics.Result.Lineage.CompletionIdentity;
            for (int watchIndex = 0; watchIndex < m_MergedPoseWatchInterestCount; watchIndex++)
            {
                AnimationPoseWatchIdentity identity = m_MergedPoseWatchInterests[watchIndex];
                int poseOffset = watchIndex * boneCount;
                int contributionOffset = watchIndex * stride;
                int goalOffset = watchIndex * CharacterFullBodyIkGoalSetHeader.MaximumGoalCount;
                int effectorOffset = watchIndex * CharacterFullBodyIkGoalSetHeader.MaximumGoalCount;
                int limbOffset = watchIndex * 4;
                Array.Clear(page.PoseWatchLocalPoses, poseOffset, boneCount);
                Array.Clear(page.PoseWatchComponentPoses, poseOffset, boneCount);
                Array.Clear(page.PoseWatchContributions, contributionOffset, stride);
                page.PoseWatchFootLandingPredictions[watchIndex] = default;
                page.PoseWatchFullBodyIkSolvers[watchIndex] = default;
                Array.Clear(
                    page.PoseWatchFullBodyIkGoals,
                    goalOffset,
                    CharacterFullBodyIkGoalSetHeader.MaximumGoalCount);
                Array.Clear(
                    page.PoseWatchFullBodyIkEffectors,
                    effectorOffset,
                    CharacterFullBodyIkGoalSetHeader.MaximumGoalCount);
                Array.Clear(page.PoseWatchFullBodyIkLimbs, limbOffset, 4);
                if (!TryResolvePoseWatchOperation(identity, out CharacterPresentationPoseOperation operation))
                {
                    page.PoseWatches[watchIndex] = CreateUnavailableWatch(
                        identity,
                        -1,
                        poseOffset,
                        boneCount,
                        contributionOffset,
                        AnimationPoseWatchAvailability.Invalid,
                        default,
                        0);
                    continue;
                }
                if (!MatchesPoseWatchGraphRevision(identity, operation))
                {
                    page.PoseWatches[watchIndex] = CreateUnavailableWatch(
                        identity,
                        -1,
                        poseOffset,
                        boneCount,
                        contributionOffset,
                        AnimationPoseWatchAvailability.Stale,
                        default,
                        0);
                    continue;
                }
                AnimationLinkedPoseEntryRuntimeSnapshot linkedPoseEntry =
                    FindLinkedPoseEntry(page, operation);
                ulong completion = programDiagnostics.GetOperationCompletion(
                    operation.Index).CompletionIdentity;
                if (operation.OutputFullBodyIkGoalContributionValueIndex >= 0)
                {
                    int contributionValueIndex =
                        operation.OutputFullBodyIkGoalContributionValueIndex;
                    AnimationFullBodyIkGoalContributionSnapshot contribution = default;
                    AnimationPoseWatchAvailability goalAvailability =
                        completion != frameCompletion
                            ? AnimationPoseWatchAvailability.NotCompleted
                            : AnimationPoseWatchAvailability.Invalid;
                    if ((uint)contributionValueIndex <
                        (uint)constraintDiagnostics.FullBodyIkGoalContributionCount)
                    {
                        CharacterFullBodyIkGoalContributionHeader header =
                            constraintDiagnostics.GetGoalContribution(
                                contributionValueIndex);
                        if (header.IsValid &&
                            header.CompletionIdentity == completion &&
                            header.ProducerOperationIndex == operation.Index &&
                            header.GoalOffset <=
                            constraintDiagnostics.FullBodyIkContributionGoalCount -
                            header.GoalCount)
                        {
                            contribution =
                                new AnimationFullBodyIkGoalContributionSnapshot(
                                    in header,
                                    goalOffset);
                            if (header.Availability ==
                                CharacterFullBodyIkGoalContributionAvailability.Ready)
                            {
                                for (int goalIndex = 0;
                                     goalIndex < header.GoalCount;
                                     goalIndex++)
                                {
                                    page.PoseWatchFullBodyIkGoals[
                                            goalOffset + goalIndex] =
                                        constraintDiagnostics.GetContributionGoal(
                                            header.GoalOffset + goalIndex);
                                }
                                goalAvailability =
                                    AnimationPoseWatchAvailability.Targets;
                            }
                            else
                            {
                                goalAvailability =
                                    AnimationPoseWatchAvailability
                                        .WorldContextUnavailable;
                            }
                        }
                    }
                    if (operation.Code == CharacterPoseOperationCode.FootPlacement &&
                        footLandingPrediction.IsCompleted &&
                        footLandingPrediction.CompletionIdentity == completion &&
                        footLandingPrediction.FrameSequence ==
                        contribution.FrameSequence)
                    {
                        page.PoseWatchFootLandingPredictions[watchIndex] =
                            footLandingPrediction;
                    }
                    page.PoseWatches[watchIndex] = new AnimationPoseWatchSnapshot(
                        identity,
                        operation.Index,
                        operation.Code,
                        FindStageIndex(operation.Index),
                        operation.ExecutionDomain,
                        CharacterPoseSpace.None,
                        linkedPoseEntry,
                        contribution,
                        default,
                        poseOffset,
                        boneCount,
                        contributionOffset,
                        0,
                        goalAvailability,
                        goalAvailability == AnimationPoseWatchAvailability.Invalid
                            ? programDiagnostics.PoseGraphInvalidReason
                            : AnimationPoseNativeInvalidReason.None,
                        operation.Weight,
                        0,
                        completion);
                    continue;
                }
                if (operation.OutputFullBodyIkGoalSetValueIndex >= 0)
                {
                    int goalSetIndex = operation.OutputFullBodyIkGoalSetValueIndex;
                    AnimationFullBodyIkGoalSetSnapshot goalSet = default;
                    AnimationPoseWatchAvailability goalAvailability =
                        completion != frameCompletion
                            ? AnimationPoseWatchAvailability.NotCompleted
                            : AnimationPoseWatchAvailability.Invalid;
                    if ((uint)goalSetIndex <
                            (uint)m_Program.FullBodyIkGoalSetWorkspaceCount &&
                        operation.Code ==
                        CharacterPoseOperationCode.FullBodyIkGoalAssembler)
                    {
                        CharacterFullBodyIkGoalSetHeader header =
                            constraintDiagnostics.GoalSet;
                        if (header.IsValid &&
                            header.CompletionIdentity == completion &&
                            header.ProducerOperationIndex == operation.Index)
                        {
                            goalSet = new AnimationFullBodyIkGoalSetSnapshot(
                                in header,
                                goalOffset);
                            for (int goalIndex = 0;
                                 goalIndex < header.GoalCount;
                                 goalIndex++)
                            {
                                page.PoseWatchFullBodyIkGoals[goalOffset + goalIndex] =
                                    constraintDiagnostics.GetGoal(
                                        goalIndex);
                            }
                            goalAvailability = AnimationPoseWatchAvailability.Targets;
                        }
                    }
                    if (operation.Code == CharacterPoseOperationCode.FootPlacement &&
                        footLandingPrediction.IsCompleted &&
                        footLandingPrediction.CompletionIdentity == completion &&
                        footLandingPrediction.FrameSequence == goalSet.FrameSequence)
                    {
                        page.PoseWatchFootLandingPredictions[watchIndex] =
                            footLandingPrediction;
                    }
                    page.PoseWatches[watchIndex] = new AnimationPoseWatchSnapshot(
                        identity,
                        operation.Index,
                        operation.Code,
                        FindStageIndex(operation.Index),
                        operation.ExecutionDomain,
                        CharacterPoseSpace.None,
                        linkedPoseEntry,
                        default,
                        goalSet,
                        poseOffset,
                        boneCount,
                        contributionOffset,
                        0,
                        goalAvailability,
                        goalAvailability == AnimationPoseWatchAvailability.Invalid
                            ? programDiagnostics.PoseGraphInvalidReason
                            : AnimationPoseNativeInvalidReason.None,
                        operation.Weight,
                        0,
                        completion);
                    continue;
                }
                int valueIndex = operation.OutputValueIndex;
                AnimationPoseAvailability availability =
                    programDiagnostics.GetValueAvailability(valueIndex);
                AnimationPoseNativeInvalidReason invalidReason =
                    programDiagnostics.GetValueInvalidReason(valueIndex);
                AnimationPoseWatchAvailability watchAvailability =
                    completion != frameCompletion
                    ? AnimationPoseWatchAvailability.NotCompleted
                    : availability == AnimationPoseAvailability.Pose
                        ? AnimationPoseWatchAvailability.Pose
                        : availability == AnimationPoseAvailability.NoPose
                            ? AnimationPoseWatchAvailability.NoPose
                            : AnimationPoseWatchAvailability.Invalid;
                int contributionCount = 0;
                if (watchAvailability == AnimationPoseWatchAvailability.Pose)
                {
                    for (int boneIndex = 0; boneIndex < boneCount; boneIndex++)
                    {
                        page.PoseWatchLocalPoses[poseOffset + boneIndex] =
                            programDiagnostics.GetValuePose(
                                valueIndex,
                                boneIndex);
                    }
                    for (int boneIndex = 0; boneIndex < boneCount; boneIndex++)
                    {
                        AnimationLocalBonePose stored = page.PoseWatchLocalPoses[poseOffset + boneIndex];
                        CharacterComponentBonePose component;
                        if (operation.OutputPoseSpace == CharacterPoseSpace.Component)
                        {
                            component = new CharacterComponentBonePose(stored.Position, stored.Rotation, stored.Scale);
                        }
                        else if (!CharacterPoseConstraintMath.TryCreateComponent(
                                     stored,
                                     page.PoseBones[boneIndex].ParentPoseBoneIndex,
                                     page.PoseWatchComponentPoses,
                                     poseOffset,
                                     out component))
                        {
                            throw new InvalidOperationException(
                                $"Pose Watch operation #{operation.Index} component Bone #{boneIndex} is invalid.");
                        }
                        page.PoseWatchComponentPoses[poseOffset + boneIndex] = component;
                    }
                    contributionCount =
                        programDiagnostics.GetValueContributionCount(valueIndex);
                    if (contributionCount < 0 || contributionCount > stride)
                        throw new InvalidOperationException($"Pose Watch operation #{operation.Index} contribution count is invalid.");
                    for (int contributionIndex = 0; contributionIndex < contributionCount; contributionIndex++)
                    {
                        page.PoseWatchContributions[contributionOffset + contributionIndex] = ConvertContribution(
                            programDiagnostics.GetValueContribution(
                                valueIndex,
                                contributionIndex),
                            in sourceDiagnostics);
                    }
                }
                if (operation.Code == CharacterPoseOperationCode.FullBodyIK &&
                    operation.FullBodyIkIndex == 0)
                {
                    CharacterFullBodyIkSolverDiagnostics diagnostics =
                        constraintDiagnostics.Solver;
                    if (diagnostics.IsCompleted &&
                        diagnostics.InputCompletionIdentity == completion)
                    {
                        page.PoseWatchFullBodyIkSolvers[watchIndex] = diagnostics;
                        for (int effectorIndex = 0;
                             effectorIndex <
                             constraintDiagnostics.SolverEffectorCount;
                             effectorIndex++)
                        {
                            page.PoseWatchFullBodyIkEffectors[effectorOffset + effectorIndex] =
                                constraintDiagnostics.GetSolverEffector(
                                    effectorIndex);
                        }
                        for (int limbIndex = 0;
                             limbIndex < constraintDiagnostics.SolverLimbCount;
                             limbIndex++)
                        {
                            page.PoseWatchFullBodyIkLimbs[limbOffset + limbIndex] =
                                constraintDiagnostics.GetSolverLimb(
                                    limbIndex);
                        }
                    }
                }
                page.PoseWatches[watchIndex] = new AnimationPoseWatchSnapshot(
                    identity,
                    operation.Index,
                    operation.Code,
                    FindStageIndex(operation.Index),
                    operation.ExecutionDomain,
                    operation.OutputPoseSpace,
                    linkedPoseEntry,
                    default,
                    default,
                    poseOffset,
                    boneCount,
                    contributionOffset,
                    contributionCount,
                    watchAvailability,
                    invalidReason,
                    programDiagnostics.GetValueOutputWeight(valueIndex),
                    programDiagnostics.GetValueContinuityIdentity(valueIndex),
                    completion);
            }
            page.PoseWatchCount = m_MergedPoseWatchInterestCount;
        }

        void CopyFootPlacement(
            Page page,
            in CharacterPoseConstraintCommittedDiagnosticsView
                constraintDiagnostics,
            in AnimationPhysicalBoneWriteDiagnostics physicalWrite)
        {
            CharacterFootLandingPredictionDiagnostics footLandingPrediction =
                constraintDiagnostics.FootLandingPrediction;
            if (!footLandingPrediction.IsCompleted ||
                footLandingPrediction.CompletionIdentity != page.CompletionIdentity)
                return;
            CharacterFullBodyIkSolverDiagnostics solverDiagnostics = default;
            CharacterFullBodyIkEffectorDiagnostics pelvis = default;
            CharacterFullBodyIkEffectorDiagnostics leftFoot = default;
            CharacterFullBodyIkEffectorDiagnostics rightFoot = default;
            CharacterFullBodyIkLimbDiagnostics leftLeg = default;
            CharacterFullBodyIkLimbDiagnostics rightLeg = default;
            CharacterFullBodyIkSolverDiagnostics candidate =
                constraintDiagnostics.Solver;
            if (candidate.IsCompleted &&
                candidate.InputCompletionIdentity == page.CompletionIdentity &&
                candidate.FrameSequence == footLandingPrediction.FrameSequence)
            {
                bool containsFoot = false;
                for (int effectorIndex = 0;
                     effectorIndex < constraintDiagnostics.SolverEffectorCount;
                     effectorIndex++)
                {
                    CharacterFullBodyIkEffectorDiagnostics effector =
                        constraintDiagnostics.GetSolverEffector(
                            effectorIndex);
                    if (effector.Slot == CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation)
                    {
                        pelvis = effector;
                        containsFoot = true;
                    }
                    else if (effector.Slot == CharacterFullBodyIkEffectorSlot.LeftFoot)
                    {
                        leftFoot = effector;
                        containsFoot = true;
                    }
                    else if (effector.Slot == CharacterFullBodyIkEffectorSlot.RightFoot)
                    {
                        rightFoot = effector;
                        containsFoot = true;
                    }
                }
                if (containsFoot)
                {
                    for (int limbIndex = 0;
                         limbIndex < constraintDiagnostics.SolverLimbCount;
                         limbIndex++)
                    {
                        CharacterFullBodyIkLimbDiagnostics limb =
                            constraintDiagnostics.GetSolverLimb(limbIndex);
                        if (limb.Limb == CharacterFullBodyIkLimbSlot.LeftLeg)
                            leftLeg = limb;
                        else if (limb.Limb == CharacterFullBodyIkLimbSlot.RightLeg)
                            rightLeg = limb;
                    }
                    solverDiagnostics = candidate;
                }
            }
            page.FootPlacement.LandingPrediction = footLandingPrediction;
            page.FootPlacement.Solver = solverDiagnostics;
            page.FootPlacement.Pelvis = pelvis;
            page.FootPlacement.LeftFoot = leftFoot;
            page.FootPlacement.RightFoot = rightFoot;
            page.FootPlacement.LeftLeg = leftLeg;
            page.FootPlacement.RightLeg = rightLeg;
            page.FootPlacement.PhysicalWrite =
                physicalWrite.IsAvailable &&
                physicalWrite.CompletionIdentity == page.CompletionIdentity
                    ? physicalWrite
                    : default;
        }

        bool TryResolvePoseWatchOperation(
            AnimationPoseWatchIdentity identity,
            out CharacterPresentationPoseOperation resolved)
        {
            resolved = default;
            bool found = false;
            for (int i = 0; i < m_Program.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = m_Program.Operations[i];
                CharacterPresentationPoseSourceMapEntry source = m_Program.SourceMap[i];
                if ((operation.OutputValueIndex < 0 &&
                     operation.OutputFullBodyIkGoalContributionValueIndex < 0 &&
                     operation.OutputFullBodyIkGoalSetValueIndex < 0) ||
                    !string.Equals(source.GraphId, identity.GraphId, StringComparison.Ordinal) ||
                    !source.NodeId.Equals(identity.NodeId) ||
                    !string.Equals(source.CallSite, identity.CallSite, StringComparison.Ordinal))
                {
                    continue;
                }
                if (found)
                    throw new InvalidOperationException($"Pose Watch identity '{identity}' resolves to multiple compiled operations.");
                resolved = operation;
                found = true;
            }
            return found;
        }

        bool MatchesPoseWatchGraphRevision(
            AnimationPoseWatchIdentity identity,
            CharacterPresentationPoseOperation operation)
        {
            if (operation.LinkedPoseFragmentIndex >= 0)
            {
                CharacterLinkedPoseEntryFragmentPlanDescriptor fragment =
                    m_Program.LinkedPoseFragments[operation.LinkedPoseFragmentIndex];
                return string.Equals(identity.GraphRevision, fragment.GraphRevision, StringComparison.Ordinal);
            }
            return string.Equals(identity.GraphRevision, m_Program.ContentRevision, StringComparison.Ordinal);
        }

        static AnimationLinkedPoseEntryRuntimeSnapshot FindLinkedPoseEntry(
            Page page,
            CharacterPresentationPoseOperation operation)
        {
            if ((uint)operation.LinkedPoseCallIndex < (uint)page.LinkedPoseEntryCount)
                return page.LinkedPoseEntries[operation.LinkedPoseCallIndex];
            for (int i = 0; i < page.LinkedPoseEntryCount; i++)
            {
                AnimationLinkedPoseEntryRuntimeSnapshot entry = page.LinkedPoseEntries[i];
                if (operation.Index >= entry.OperationStart &&
                    operation.Index < entry.OperationStart + entry.OperationCount)
                {
                    return entry;
                }
            }
            return default;
        }

        int FindStageIndex(int operationIndex)
        {
            for (int i = 0; i < m_Program.Stages.Count; i++)
            {
                CharacterPresentationPoseStage stage = m_Program.Stages[i];
                if (operationIndex >= stage.OperationStart &&
                    operationIndex < stage.OperationStart + stage.OperationCount)
                {
                    return stage.Index;
                }
            }
            throw new InvalidOperationException(
                $"Pose Watch operation #{operationIndex} is not owned by a compiled stage.");
        }

        static AnimationPoseWatchSnapshot CreateUnavailableWatch(
            AnimationPoseWatchIdentity identity,
            int operationIndex,
            int poseOffset,
            int boneCount,
            int contributionOffset,
            AnimationPoseWatchAvailability availability,
            AnimationPoseNativeInvalidReason invalidReason,
            ulong completionIdentity) =>
            new AnimationPoseWatchSnapshot(
                identity,
                operationIndex,
                default,
                -1,
                default,
                CharacterPoseSpace.None,
                default,
                default,
                default,
                poseOffset,
                boneCount,
                contributionOffset,
                0,
                availability,
                invalidReason,
                0f,
                0,
                completionIdentity);

        static void CopyFinalSummary(
            Page page,
            in CharacterPoseFrameExecutionResult executionResult,
            in ComposedAnimationPoseFrame finalFrame)
        {
            CharacterPoseProgramResult program = executionResult.Program;
            CharacterFinalPosePublicationResult publication =
                executionResult.Publication;
            page.FinalAvailability = publication.Availability;
            page.FinalInvalidReason =
                program.GraphInvalidReason !=
                AnimationPoseNativeInvalidReason.None
                    ? program.GraphInvalidReason
                    : program.OutputInvalidReason;
            page.InvalidOperationIndex = program.InvalidOperationIndex;
            page.PoseGraphCompletedAt =
                executionResult.Lineage.CompletionIdentity;
            page.FinalAppliedAt = publication.AppliedCompletionIdentity;
            page.ContinuityIdentity = finalFrame.ContinuityIdentity;
            page.HasFootFeatures = finalFrame.HasFootFeatures;
            AnimationFootFeatureSample left = finalFrame.LeftFootFeatures;
            AnimationFootFeatureSample right = finalFrame.RightFootFeatures;
            page.LeftFootSteps = page.HasFootFeatures
                ? new AnimationBiomechanicalStepReadPage(
                    in left,
                    global::ThirdPersonCharacter.Pipeline.Presentation.CharacterFootSide.Left)
                : default;
            page.RightFootSteps = page.HasFootFeatures
                ? new AnimationBiomechanicalStepReadPage(
                    in right,
                    global::ThirdPersonCharacter.Pipeline.Presentation.CharacterFootSide.Right)
                : default;
        }

        void CopyFinalDetail(
            Page page,
            in ComposedAnimationPoseFrame finalFrame)
        {
            AnimationReadOnlyBuffer<float> parameters =
                finalFrame.PoseParameters;
            AnimationReadOnlyBuffer<byte> parameterAvailability =
                finalFrame.PoseParameterAvailability;
            AnimationReadOnlyBuffer<AnimationPoseSourceContribution>
                contributions = finalFrame.Contributions;
            int contributionCount = contributions.Count;
            if (parameters.Count != m_Program.Parameters.Count ||
                parameterAvailability.Count != parameters.Count ||
                finalFrame.PoseBoneCount != page.BoneIds.Length ||
                contributionCount <= 0 ||
                contributionCount > page.FinalContributions.Length)
                throw new InvalidOperationException("Final Animation Pose contribution count is invalid.");
            for (int i = 0; i < m_Program.Parameters.Count; i++)
            {
                page.Parameters[i] = new AnimationPoseParameterSnapshot(
                    m_Program.Parameters[i].ParameterId,
                    parameters[i],
                    parameterAvailability[i] != 0);
            }
            for (int i = 0; i < contributionCount; i++)
            {
                page.FinalContributions[i] = contributions[i];
                for (int boneIndex = 0; boneIndex < page.BoneIds.Length; boneIndex++)
                {
                    page.FinalContributionBoneWeights[i * page.BoneIds.Length + boneIndex] =
                        finalFrame.GetContributionBoneWeight(i, boneIndex);
                }
            }
            page.ParameterCount = m_Program.Parameters.Count;
            page.FinalContributionCount = contributionCount;
        }

        AnimationPoseSourceContribution ConvertContribution(
            AnimationPrimitivePoseContribution primitive,
            in CharacterPoseSourceCommittedDiagnosticsView sourceDiagnostics)
        {
            AnimationPoseSourceId sourceId = default;
            PoseNodeId playerNodeId =
                RequirePlayerNodeId(primitive.PhysicalPlayerIndex);
            if (primitive.Kind == AnimationPoseContributionKind.Live)
            {
                var physical = new AnimationPhysicalSourceIdentity(
                    new AnimationPhysicalSourceIndex(primitive.PhysicalSourceIndex),
                    primitive.PhysicalSourceGeneration);
                CharacterPoseSourceCommittedIdentity source =
                    sourceDiagnostics.Resolve(physical);
                sourceId = source.SourceId;
                if (source.PoseNodeId != playerNodeId)
                    throw new InvalidOperationException("Animation diagnostic contribution Player identity is inconsistent.");
                if (source.SourceOwnerIndex != primitive.SourceOwnerIndex)
                    throw new InvalidOperationException("Animation diagnostic contribution producer identity is inconsistent.");
            }
            return new AnimationPoseSourceContribution(
                playerNodeId,
                primitive.Kind,
                sourceId,
                primitive.SourceOwnerIndex,
                primitive.ContributionContinuityIdentity,
                primitive.Weight,
                primitive.LeftFootWeight,
                primitive.RightFootWeight);
        }

        PoseNodeId RequirePlayerNodeId(int playerIndex)
        {
            PoseNodeId result = default;
            for (int i = 0; i < m_Program.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation =
                    m_Program.Operations[i];
                if (operation.PlayerIndex != playerIndex ||
                    operation.Code !=
                        CharacterPoseOperationCode.SelectedPosePlayer &&
                    operation.Code != CharacterPoseOperationCode.BlendStack &&
                    operation.Code !=
                        CharacterPoseOperationCode.BlendSpacePlayer &&
                    operation.Code != CharacterPoseOperationCode.ClipPlayer &&
                    operation.Code != CharacterPoseOperationCode.AnimationSlot)
                {
                    continue;
                }
                if (!operation.NodeId.IsValid || result.IsValid)
                {
                    throw new InvalidOperationException(
                        "Animation diagnostic Player identity is ambiguous.");
                }
                result = operation.NodeId;
            }
            if (!result.IsValid)
            {
                throw new InvalidOperationException(
                    $"Animation diagnostic Player #{playerIndex} is missing.");
            }
            return result;
        }

        static bool RequiresBasicState(AnimationPresentationDiagnosticsInterest interest) =>
            (interest & (AnimationPresentationDiagnosticsInterest.LiveState |
                         AnimationPresentationDiagnosticsInterest.Capture)) != 0;

        static bool RequiresOperationDetail(AnimationPresentationDiagnosticsInterest interest) =>
            (interest & (AnimationPresentationDiagnosticsInterest.Capture |
                         AnimationPresentationDiagnosticsInterest.OperationDetail)) != 0;

        static bool RequiresFinalPoseDetail(AnimationPresentationDiagnosticsInterest interest) =>
            (interest & (AnimationPresentationDiagnosticsInterest.Capture |
                         AnimationPresentationDiagnosticsInterest.FinalPoseDetail)) != 0;

        static void RequireValidFrameInterest(AnimationPresentationDiagnosticsInterest interest)
        {
            const AnimationPresentationDiagnosticsInterest all =
                ExplicitOwnerMask |
                AnimationPresentationDiagnosticsInterest.PoseWatch;
            if (interest == AnimationPresentationDiagnosticsInterest.None ||
                (interest & ~all) != 0)
                throw new ArgumentOutOfRangeException(nameof(interest));
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationPresentationRuntimeSnapshotPublisher));
        }

        sealed class Page
        {
            internal Page(
                CharacterPresentationPosePlan program,
                CharacterAnimationRigPayload rig,
                AnimationPoseNativeAggregateLayout layout,
                int entryCapacity,
                int releaseCapacity,
                int blendSpacePlayerCapacity,
                int blendSpaceSampleCapacity,
                int linkedPoseGroupCapacity)
            {
                Lease = new FinalAnimationPoseFramePageLease();
                Stacks = new AnimationBlendStackSnapshot[layout.PlayerCount];
                Inertializations = new PoseInertializationSnapshot[program.Inertializations.Count];
                Entries = new AnimationBlendStackEntrySnapshot[entryCapacity];
                Operations = new AnimationPoseOperationSnapshot[program.Operations.Count];
                Parameters = new AnimationPoseParameterSnapshot[program.Parameters.Count];
                BlendSpacePlayers = new AnimationBlendSpacePlayerRuntimeSnapshot[blendSpacePlayerCapacity];
                BlendSpaceSamples = new AnimationBlendSpaceSampleRuntimeSnapshot[blendSpaceSampleCapacity];
                SlotContributions = new AnimationPoseSourceContribution[layout.TotalPlayerContributionCapacity];
                OperationContributions = new AnimationPoseSourceContribution[
                    checked(program.Operations.Count * layout.PoseValueContributionStride)];
                FinalContributions = new AnimationPoseSourceContribution[layout.PoseValueContributionStride];
                Releases = new AnimationReleasedPoseSourceSnapshot[releaseCapacity];
                AnimationSlots = new AnimationSlotRuntimeSnapshot[program.AnimationSlots.Count];
                PoseStateMachines = new PoseStateMachineRuntimeSnapshot[program.StateMachines.Count];
                PoseStateMachineBoneWeights = new float[
                    checked(program.StateMachines.Count * layout.BoneCount)];
                RootOrientationWarps = new RootOrientationWarpRuntimeSnapshot[program.RootOrientationWarps.Count];
                LinkedPoseGroups = new CharacterLinkedPoseRuntimeGroupSnapshot[linkedPoseGroupCapacity];
                LinkedPoseEntries = new AnimationLinkedPoseEntryRuntimeSnapshot[program.LinkedPoseCalls.Count];
                PoseWatches = new AnimationPoseWatchSnapshot[AnimationPoseWatchCapacity.PerTarget];
                PoseWatchFullBodyIkGoals = new CharacterFullBodyIkGoal[
                    checked(AnimationPoseWatchCapacity.PerTarget * CharacterFullBodyIkGoalSetHeader.MaximumGoalCount)];
                PoseWatchFootLandingPredictions =
                    new CharacterFootLandingPredictionDiagnostics[AnimationPoseWatchCapacity.PerTarget];
                PoseWatchFullBodyIkSolvers =
                    new CharacterFullBodyIkSolverDiagnostics[AnimationPoseWatchCapacity.PerTarget];
                PoseWatchFullBodyIkEffectors = new CharacterFullBodyIkEffectorDiagnostics[
                    checked(AnimationPoseWatchCapacity.PerTarget * CharacterFullBodyIkGoalSetHeader.MaximumGoalCount)];
                PoseWatchFullBodyIkLimbs = new CharacterFullBodyIkLimbDiagnostics[
                    checked(AnimationPoseWatchCapacity.PerTarget * 4)];
                PoseWatchLocalPoses = new AnimationLocalBonePose[checked(AnimationPoseWatchCapacity.PerTarget * layout.BoneCount)];
                PoseWatchComponentPoses = new CharacterComponentBonePose[checked(AnimationPoseWatchCapacity.PerTarget * layout.BoneCount)];
                PoseWatchContributions = new AnimationPoseSourceContribution[
                    checked(AnimationPoseWatchCapacity.PerTarget * layout.PoseValueContributionStride)];
                BoneIds = new AnimationBoneId[layout.BoneCount];
                PoseBones = new AnimationPoseBoneSnapshot[layout.BoneCount];
                EntryBoneWeights = new float[checked(entryCapacity * layout.BoneCount)];
                StoredBoneWeights = new float[checked(layout.PlayerCount * layout.BoneCount)];
                InertialPositionResiduals = new Vector3[checked(program.Inertializations.Count * layout.BoneCount)];
                InertialRotationResiduals = new Vector3[checked(program.Inertializations.Count * layout.BoneCount)];
                InertialScaleResiduals = new Vector3[checked(program.Inertializations.Count * layout.BoneCount)];
                InertialBoneEnvelopes = new float[checked(program.Inertializations.Count * layout.BoneCount)];
                SlotContributionBoneWeights = new float[checked(layout.TotalPlayerContributionCapacity * layout.BoneCount)];
                OperationContributionBoneWeights = new float[
                    checked(program.Operations.Count * layout.PoseValueContributionStride * layout.BoneCount)];
                FinalContributionBoneWeights = new float[checked(layout.PoseValueContributionStride * layout.BoneCount)];
                FootPlacement = new AnimationFootPlacementRuntimeSnapshotPage();
                PhysicalBoneCount = rig.PhysicalBoneCount;
                VirtualBoneCount = rig.VirtualBoneCount;
                PoseBoneCount = rig.PoseBoneCount;
                for (int i = 0; i < BoneIds.Length; i++)
                {
                    BoneIds[i] = rig.GetPoseBoneId(i);
                    CharacterPoseBoneKind kind = rig.GetPoseBoneKind(i);
                    AnimationBoneId sourceBoneId = default;
                    AnimationBoneId targetBoneId = default;
                    if (kind == CharacterPoseBoneKind.Virtual)
                    {
                        CharacterAnimationVirtualBonePayload virtualBone =
                            rig.VirtualBones[i - rig.PhysicalBoneCount];
                        sourceBoneId = rig.PhysicalBones[virtualBone.SourcePhysicalBoneIndex].BoneId;
                        targetBoneId = rig.PhysicalBones[virtualBone.TargetPhysicalBoneIndex].BoneId;
                    }
                    PoseBones[i] = new AnimationPoseBoneSnapshot(
                        BoneIds[i],
                        kind,
                        rig.GetPoseParentIndex(i),
                        sourceBoneId,
                        targetBoneId);
                }
            }

            internal readonly FinalAnimationPoseFramePageLease Lease;
            internal readonly AnimationBlendStackSnapshot[] Stacks;
            internal readonly PoseInertializationSnapshot[] Inertializations;
            internal readonly AnimationBlendStackEntrySnapshot[] Entries;
            internal readonly AnimationPoseOperationSnapshot[] Operations;
            internal readonly AnimationPoseParameterSnapshot[] Parameters;
            internal readonly AnimationBlendSpacePlayerRuntimeSnapshot[] BlendSpacePlayers;
            internal readonly AnimationBlendSpaceSampleRuntimeSnapshot[] BlendSpaceSamples;
            internal readonly AnimationPoseSourceContribution[] SlotContributions;
            internal readonly AnimationPoseSourceContribution[] OperationContributions;
            internal readonly AnimationPoseSourceContribution[] FinalContributions;
            internal readonly AnimationReleasedPoseSourceSnapshot[] Releases;
            internal readonly AnimationSlotRuntimeSnapshot[] AnimationSlots;
            internal readonly PoseStateMachineRuntimeSnapshot[] PoseStateMachines;
            internal readonly float[] PoseStateMachineBoneWeights;
            internal readonly RootOrientationWarpRuntimeSnapshot[] RootOrientationWarps;
            internal readonly CharacterLinkedPoseRuntimeGroupSnapshot[] LinkedPoseGroups;
            internal readonly AnimationLinkedPoseEntryRuntimeSnapshot[] LinkedPoseEntries;
            internal readonly AnimationPoseWatchSnapshot[] PoseWatches;
            internal readonly CharacterFullBodyIkGoal[] PoseWatchFullBodyIkGoals;
            internal readonly CharacterFootLandingPredictionDiagnostics[] PoseWatchFootLandingPredictions;
            internal readonly CharacterFullBodyIkSolverDiagnostics[] PoseWatchFullBodyIkSolvers;
            internal readonly CharacterFullBodyIkEffectorDiagnostics[] PoseWatchFullBodyIkEffectors;
            internal readonly CharacterFullBodyIkLimbDiagnostics[] PoseWatchFullBodyIkLimbs;
            internal readonly AnimationLocalBonePose[] PoseWatchLocalPoses;
            internal readonly CharacterComponentBonePose[] PoseWatchComponentPoses;
            internal readonly AnimationPoseSourceContribution[] PoseWatchContributions;
            internal readonly AnimationBoneId[] BoneIds;
            internal readonly AnimationPoseBoneSnapshot[] PoseBones;
            internal readonly float[] EntryBoneWeights;
            internal readonly float[] StoredBoneWeights;
            internal readonly Vector3[] InertialPositionResiduals;
            internal readonly Vector3[] InertialRotationResiduals;
            internal readonly Vector3[] InertialScaleResiduals;
            internal readonly float[] InertialBoneEnvelopes;
            internal readonly float[] SlotContributionBoneWeights;
            internal readonly float[] OperationContributionBoneWeights;
            internal readonly float[] FinalContributionBoneWeights;
            internal readonly int PhysicalBoneCount;
            internal readonly int VirtualBoneCount;
            internal readonly int PoseBoneCount;
            internal AnimationPresentationDiagnosticsInterest Interest;
            internal int StackCount;
            internal int InertializationCount;
            internal int EntryCount;
            internal int OperationCount;
            internal int ParameterCount;
            internal int BlendSpacePlayerCount;
            internal int BlendSpaceSampleCount;
            internal int SlotContributionCount;
            internal int OperationContributionCount;
            internal int FinalContributionCount;
            internal int ReleaseCount;
            internal int AnimationSlotCount;
            internal int PoseStateMachineCount;
            internal int RootOrientationWarpCount;
            internal int LinkedPoseGroupCount;
            internal int LinkedPoseEntryCount;
            internal int PoseWatchCount;
            internal ulong CompletionIdentity;
            internal AnimationPoseAvailability FinalAvailability;
            internal AnimationPoseNativeInvalidReason FinalInvalidReason;
            internal int InvalidOperationIndex;
            internal ulong PoseGraphCompletedAt;
            internal ulong FinalAppliedAt;
            internal ulong ContinuityIdentity;
            internal AnimationBiomechanicalStepReadPage LeftFootSteps;
            internal AnimationBiomechanicalStepReadPage RightFootSteps;
            internal bool HasFootFeatures;
            internal AnimationFootStepObservationRuntimeSnapshot FootStepObservation;
            internal readonly AnimationFootPlacementRuntimeSnapshotPage FootPlacement;

            internal void ClearCounts()
            {
                StackCount = 0;
                InertializationCount = 0;
                EntryCount = 0;
                OperationCount = 0;
                ParameterCount = 0;
                BlendSpacePlayerCount = 0;
                BlendSpaceSampleCount = 0;
                SlotContributionCount = 0;
                OperationContributionCount = 0;
                FinalContributionCount = 0;
                ReleaseCount = 0;
                AnimationSlotCount = 0;
                PoseStateMachineCount = 0;
                RootOrientationWarpCount = 0;
                LinkedPoseGroupCount = 0;
                LinkedPoseEntryCount = 0;
                PoseWatchCount = 0;
                FootStepObservation = default;
                FootPlacement.Clear();
            }

            internal AnimationPresentationRuntimeSnapshot CreateSnapshot(
                CharacterPresentationProjection projection,
                CharacterPresentationPosePlan program,
                ulong leaseIdentity)
            {
                return new AnimationPresentationRuntimeSnapshot(
                    projection.ProjectionRevision,
                    projection.Rig.RigId,
                    projection.Rig.RigRevision,
                    program.PoseGraphId,
                    program.ContentRevision,
                    program.PlanHash,
                    CompletionIdentity,
                    FinalAvailability,
                    FinalInvalidReason,
                    InvalidOperationIndex,
                    PoseGraphCompletedAt,
                    FinalAppliedAt,
                    ContinuityIdentity,
                    LeftFootSteps,
                    RightFootSteps,
                    HasFootFeatures,
                    FootStepObservation,
                    new AnimationFootPlacementRuntimeSnapshot(
                        FootPlacement,
                        Lease,
                        leaseIdentity),
                    PhysicalBoneCount,
                    VirtualBoneCount,
                    PoseBoneCount,
                    Lease,
                    leaseIdentity,
                    Stacks,
                    StackCount,
                    Inertializations,
                    InertializationCount,
                    Entries,
                    EntryCount,
                    Operations,
                    OperationCount,
                    Parameters,
                    ParameterCount,
                    BlendSpacePlayers,
                    BlendSpacePlayerCount,
                    BlendSpaceSamples,
                    BlendSpaceSampleCount,
                    SlotContributions,
                    SlotContributionCount,
                    OperationContributions,
                    OperationContributionCount,
                    FinalContributions,
                    FinalContributionCount,
                    Releases,
                    ReleaseCount,
                    AnimationSlots,
                    AnimationSlotCount,
                    PoseStateMachines,
                    PoseStateMachineCount,
                    RootOrientationWarps,
                    RootOrientationWarpCount,
                    LinkedPoseGroups,
                    LinkedPoseGroupCount,
                    LinkedPoseEntries,
                    LinkedPoseEntryCount,
                    PoseWatches,
                    PoseWatchCount,
                    PoseWatchFullBodyIkGoals,
                    PoseWatchFootLandingPredictions,
                    PoseWatchFullBodyIkSolvers,
                    PoseWatchFullBodyIkEffectors,
                    PoseWatchFullBodyIkLimbs,
                    PoseWatchLocalPoses,
                    PoseWatchComponentPoses,
                    PoseWatchContributions,
                    BoneIds,
                    PoseBones,
                    EntryBoneWeights,
                    StoredBoneWeights,
                    InertialPositionResiduals,
                    InertialRotationResiduals,
                    InertialScaleResiduals,
                    InertialBoneEnvelopes,
                    PoseStateMachineBoneWeights,
                    SlotContributionBoneWeights,
                    OperationContributionBoneWeights,
                    FinalContributionBoneWeights);
            }
        }
    }
}
