using System;
using System.Collections.Generic;
using BTSMTL.EventGraphs;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeActionSlotSource : IDisposable
    {
        struct SampleCache
        {
            internal AnimationPoseSourceId SourceId;
            internal ulong ActionInstanceId;
            internal CharacterActionAnimationSourcePlan Plan;
            internal ActionProjectedSample Sample;
            internal ulong RequestSequence;
            internal ulong CommandSequence;
            internal int OwnerIndex;
        }

        readonly PoseNodeId m_NodeId;
        readonly AnimationSlotId m_SlotId;
        readonly IActionAnimationPlaybackFrameSource m_Playback;
        readonly AnimationChannelId m_ChannelId;
        readonly CharacterPoseNativeSourceResourceCatalog m_Catalog;
        readonly CharacterAnimationInputContract m_InputContract;
        readonly EventGraphVariableBinding[] m_ParameterBindings;
        readonly AnimationPoseRequestWorkspace m_Workspace;
        readonly AnimationResolvedPoseSourceSample[] m_Resolved;
        readonly bool[] m_Prepared;
        readonly int[] m_PushOrder;
        SampleCache[] m_Committed;
        SampleCache[] m_Pending;
        ulong m_CommittedRequestSequence;
        ulong m_PendingRequestSequence;
        bool m_FrameOpen;
        bool m_Disposed;

        internal CharacterPoseNativeActionSlotSource(
            PoseNodeId nodeId,
            AnimationSlotId slotId,
            AnimationChannelId channelId,
            IActionAnimationPlaybackFrameSource playback,
            int capacity,
            CharacterPoseNativeSourceResourceCatalog catalog,
            CharacterAnimationInputContract inputContract,
            CharacterAnimationVariableContract variableContract,
            int footPlacementWeightParameterIndex)
        {
            if (!nodeId.IsValid || !slotId.IsValid || !channelId.IsValid || capacity <= 0)
                throw new ArgumentException(
                    "Action Slot source configuration is invalid.");
            m_NodeId = nodeId;
            m_SlotId = slotId;
            m_Playback = playback ?? throw new ArgumentNullException(nameof(playback));
            m_ChannelId = channelId;
            m_Catalog = catalog ??
                throw new ArgumentNullException(nameof(catalog));
            m_InputContract = inputContract ??
                throw new ArgumentNullException(nameof(inputContract));
            m_ParameterBindings = new EventGraphVariableBinding[inputContract.Parameters.Count];
            for (int i = 0; i < m_ParameterBindings.Length; i++)
            {
                CharacterPoseParameterDeclaration parameter = inputContract.Parameters[i];
                if (parameter.Usage != CharacterPoseParameterUsage.AnimatedProperty &&
                    parameter.ValueType != PoseParameterValueType.Vector3 &&
                    parameter.ValueType != PoseParameterValueType.Quaternion)
                    m_ParameterBindings[i] = variableContract.Bind(parameter.ParameterId.Value);
            }
            m_Workspace = new AnimationPoseRequestWorkspace(
                new AnimationPoseRequestWorkspaceLayout(
                    capacity,
                    1,
                    inputContract.Parameters.Count,
                    footPlacementWeightParameterIndex));
            m_Committed = new SampleCache[capacity];
            m_Pending = new SampleCache[capacity];
            m_Resolved = new AnimationResolvedPoseSourceSample[capacity];
            m_Prepared = new bool[capacity];
            m_PushOrder = new int[capacity];
            for (int i = 0; i < capacity; i++)
                m_Resolved[i] = new AnimationResolvedPoseSourceSample();
        }

        internal void BeginFrame(
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage,
            AnimationBlendStackRuntime stack)
        {
            RequireAlive();
            if (m_FrameOpen || !input.IsValid || !lineage.IsValid ||
                stack == null || stack.PoseNodeId != m_NodeId ||
                stack.AnimationChannelId != m_ChannelId)
            {
                throw new InvalidOperationException(
                    $"Action Slot source '{m_NodeId}' frame is invalid.");
            }
            Array.Copy(m_Committed, m_Pending, m_Committed.Length);
            Array.Clear(m_Prepared, 0, m_Prepared.Length);
            m_PendingRequestSequence = m_CommittedRequestSequence;
            m_Workspace.BeginFrame(lineage.CompletionIdentity);
            m_FrameOpen = true;
            try
            {
                Prune(stack);
                ApplyFrames(in input, stack);
                PrepareRetained(in input, stack);
                Prune(stack);
                ReportUsage(stack, lineage.CompletionIdentity);
            }
            catch
            {
                DiscardFrame();
                throw;
            }
        }

        internal AnimationResolvedPoseSourceSample RequireSample(
            PoseNodeId nodeId,
            AnimationPoseSourceId sourceId)
        {
            RequireAlive();
            int index = FindSource(sourceId);
            if (!m_FrameOpen || nodeId != m_NodeId || index < 0 ||
                !m_Prepared[index] || !m_Resolved[index].IsValid)
            {
                throw new InvalidOperationException(
                    $"Action Slot source '{m_NodeId}' has no sample for '{sourceId}'.");
            }
            return m_Resolved[index];
        }

        internal void CommitFrame()
        {
            RequireAlive();
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"Action Slot source '{m_NodeId}' frame is not open.");
            Swap(ref m_Committed, ref m_Pending);
            m_CommittedRequestSequence = m_PendingRequestSequence;
            m_Workspace.Reset();
            m_FrameOpen = false;
        }

        internal void DiscardFrame()
        {
            if (m_Disposed || !m_FrameOpen)
                return;
            m_Workspace.Reset();
            Array.Clear(m_Prepared, 0, m_Prepared.Length);
            m_FrameOpen = false;
        }

        internal void Reset()
        {
            RequireAlive();
            if (m_FrameOpen)
                DiscardFrame();
            Array.Clear(m_Committed, 0, m_Committed.Length);
            Array.Clear(m_Pending, 0, m_Pending.Length);
            Array.Clear(m_Prepared, 0, m_Prepared.Length);
            m_CommittedRequestSequence = 0;
            m_PendingRequestSequence = 0;
            m_Workspace.Reset();
        }

        void ApplyFrames(
            in CharacterPoseNativeFrameInput input,
            AnimationBlendStackRuntime stack)
        {
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> frames = m_Playback.Frames;
            int count = 0;
            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i].AnimationChannelId != m_ChannelId)
                    continue;
                if (count == m_PushOrder.Length)
                    throw new InvalidOperationException($"Action Slot '{m_NodeId}' frame capacity was exceeded.");
                int insert = count;
                while (insert > 0 && frames[m_PushOrder[insert - 1]].LatestCommandSequence > frames[i].LatestCommandSequence)
                {
                    m_PushOrder[insert] = m_PushOrder[insert - 1];
                    insert--;
                }
                m_PushOrder[insert] = i;
                count++;
            }
            int targetIndex = -1;
            bool currentSourceEnded = false;
            for (int i = 0; i < count; i++)
            {
                ActionAnimationPlaybackLifecycleFrame frame = frames[m_PushOrder[i]];
                AnimationPoseSourceId sourceId = SourceId(in frame);
                if (frame.EndReason != ActionPlaybackEndReason.None)
                {
                    currentSourceEnded |= stack.IsCurrentSource(sourceId);
                    continue;
                }
                if (frame.FirstSampleReadiness != ActionFirstSampleReadiness.Ready)
                    continue;
                if (!frame.ProjectedSample.IsValid)
                    throw new InvalidOperationException($"Action Slot '{m_NodeId}' requires a Timeline projected sample.");
                int index = FindSource(sourceId);
                if (index < 0)
                {
                    index = Allocate();
                    m_Pending[index] = new SampleCache
                    {
                        SourceId = sourceId,
                        ActionInstanceId = frame.ActionInstanceId,
                        OwnerIndex = stack.RequireSourceOwnerIndex(frame.ProgramProducerId),
                        Plan = m_Catalog.RequireActionPlan(frame.ProjectedSample.AuthoringClipIdentity)
                    };
                }
                ref SampleCache cache = ref m_Pending[index];
                targetIndex = index;
                if (cache.Plan.AuthoringClipIdentity != frame.ProjectedSample.AuthoringClipIdentity)
                    throw new InvalidOperationException($"Timeline producer '{frame.ProgramProducerId}' changed Clip within one selection.");
                if (cache.CommandSequence == frame.LatestCommandSequence)
                    continue;
                cache.CommandSequence = frame.LatestCommandSequence;
                cache.Sample = frame.ProjectedSample;
                cache.RequestSequence = NextRequestSequence();
                Materialize(index, in input);
            }
            if (targetIndex >= 0)
            {
                Materialize(targetIndex, in input);
                AnimationPoseSampleRequest request = m_Resolved[targetIndex].Request;
                stack.PushPoseRequest(in request,
                    stack.RequireTransitionTo(request.SourceOwnerIndex, AnimationBlendTransitionEndpointKind.SourceOwner), false);
            }
            else if (currentSourceEnded)
            {
                stack.PushSourcePose(NextRequestSequence(),
                    stack.RequireTransitionTo(-1, AnimationBlendTransitionEndpointKind.SourcePose), false);
            }
        }

        int Allocate()
        {
            for (int i = 0; i < m_Pending.Length; i++)
                if (!m_Pending[i].SourceId.IsValid)
                    return i;
            throw new InvalidOperationException($"Action Slot source '{m_NodeId}' capacity was exceeded.");
        }

        void ReportUsage(AnimationBlendStackRuntime stack, ulong completionIdentity)
        {
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> frames = m_Playback.Frames;
            for (int i = 0; i < frames.Count; i++)
            {
                ActionAnimationPlaybackLifecycleFrame frame = frames[i];
                if (frame.AnimationChannelId != m_ChannelId)
                    continue;
                ActionSlotSourceUsageKind? usage = null;
                for (int entryIndex = 0; entryIndex < stack.EntryCount; entryIndex++)
                {
                    AnimationBlendEntryState entry = stack.GetEntryState(entryIndex);
                    if (entry.IsSourcePose || !entry.SourceId.PlaybackId.Equals(frame.PlaybackId))
                        continue;
                    usage = stack.IsCurrentSource(entry.SourceId)
                        ? ActionSlotSourceUsageKind.Sample
                        : ActionSlotSourceUsageKind.OutgoingHandoff;
                    if (usage == ActionSlotSourceUsageKind.Sample)
                        break;
                }
                m_Playback.ReportSlotUsage(in frame, m_SlotId, usage, completionIdentity);
            }
        }

        static AnimationPoseSourceId SourceId(in ActionAnimationPlaybackLifecycleFrame frame) =>
            new AnimationPoseSourceId(frame.PlaybackId, AnimationPoseSourceKind.Timeline,
                new AnimationPoseSelectionGeneration(frame.SourcePoseContinuityIdentity), frame.ActionInstanceId);

        void PrepareRetained(
            in CharacterPoseNativeFrameInput input,
            AnimationBlendStackRuntime stack)
        {
            for (int i = 0; i < stack.EntryCount; i++)
            {
                AnimationBlendEntryState entry = stack.GetEntryState(i);
                if (entry.IsSourcePose ||
                    entry.SourceId.SourceKind !=
                    AnimationPoseSourceKind.Timeline)
                    continue;
                int index = FindSource(entry.SourceId);
                if (index < 0 || !m_Pending[index].Sample.IsValid)
                    throw new InvalidOperationException(
                        $"Action Slot source '{m_NodeId}' lost '{entry.SourceId}'.");
                if (!m_Prepared[index])
                    Materialize(index, in input);
            }
        }

        void Materialize(
            int index,
            in CharacterPoseNativeFrameInput input)
        {
            if (m_Prepared[index])
                return;
            ref SampleCache state = ref m_Pending[index];
            if (!state.SourceId.IsValid || !state.Sample.IsValid ||
                state.Plan == null || state.RequestSequence == 0)
                throw new InvalidOperationException(
                    $"Action Slot source '{m_NodeId}' cannot materialize source #{index}.");
            AnimationPoseRequestWorkspaceRow row =
                m_Workspace.PrepareRow(state.SourceId);
            row.Clips[row.ClipOffset] =
                state.Plan.CreateSample(in state.Sample);
            CharacterAnimationPoseInputFrame parameters =
                input.ParameterFrame;
            for (int i = 0; i < row.ParameterCount; i++)
            {
                CharacterPoseParameterDeclaration declaration =
                    m_InputContract.Parameters[i];
                if (declaration.ValueType == PoseParameterValueType.Vector3 ||
                    declaration.ValueType == PoseParameterValueType.Quaternion)
                    continue;
                row.PoseParameters[row.ParameterOffset + i] =
                    declaration.Usage == CharacterPoseParameterUsage.AnimatedProperty
                        ? declaration.DefaultValue
                        : ReadParameter(parameters.RequireValue(m_ParameterBindings[i]), declaration);
                row.PoseParameterAvailability[row.ParameterOffset + i] = 1;
            }
            PresentationPoseSampleTime time = state.Sample.Time;
            var request = new AnimationPoseSampleRequest(
                state.SourceId,
                state.ActionInstanceId,
                state.RequestSequence,
                state.OwnerIndex,
                time.SampleTime,
                time.ContinuousTime,
                time.Cycle,
                time.Loop,
                time.TimeScale,
                new AnimationReadOnlyBuffer<ClipSamplePlan>(
                    row.Clips,
                    row.ClipOffset,
                    1,
                    m_Workspace,
                    row.LeaseGeneration),
                new PresentationParameterPageId(state.RequestSequence),
                new AnimationReadOnlyBuffer<float>(
                    row.PoseParameters,
                    row.ParameterOffset,
                    row.ParameterCount,
                    m_Workspace,
                    row.LeaseGeneration),
                new AnimationReadOnlyBuffer<byte>(
                    row.PoseParameterAvailability,
                    row.ParameterOffset,
                    row.ParameterCount,
                    m_Workspace,
                    row.LeaseGeneration));
            AnimationFootFeatureSample left = default;
            AnimationFootFeatureSample right = default;
            m_Resolved[index].Set(
                request,
                in left,
                in right,
                false);
            m_Prepared[index] = true;
        }

        void Prune(AnimationBlendStackRuntime stack)
        {
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> frames = m_Playback.Frames;
            for (int i = 0; i < m_Pending.Length; i++)
            {
                AnimationPoseSourceId sourceId = m_Pending[i].SourceId;
                if (!sourceId.IsValid || stack.ContainsSource(sourceId))
                    continue;
                bool selected = false;
                for (int frameIndex = 0; frameIndex < frames.Count; frameIndex++)
                {
                    ActionAnimationPlaybackLifecycleFrame frame = frames[frameIndex];
                    if (frame.EndReason == ActionPlaybackEndReason.None && SourceId(in frame).Equals(sourceId))
                    {
                        selected = true;
                        break;
                    }
                }
                if (!selected)
                {
                    m_Pending[i] = default;
                    m_Prepared[i] = false;
                }
            }
        }

        int FindSource(AnimationPoseSourceId sourceId)
        {
            for (int i = 0; i < m_Pending.Length; i++)
                if (m_Pending[i].SourceId.Equals(sourceId))
                    return i;
            return -1;
        }

        ulong NextRequestSequence()
        {
            m_PendingRequestSequence++;
            if (m_PendingRequestSequence == 0)
                throw new InvalidOperationException(
                    "Action Slot request sequence was exhausted.");
            return m_PendingRequestSequence;
        }

        static float ReadParameter(
            in EventGraphValue value,
            CharacterPoseParameterDeclaration declaration)
        {
            return declaration.ValueType switch
            {
                PoseParameterValueType.Float
                    when value.Kind == EventGraphValueKind.Float32 =>
                    value.Float32Value,
                PoseParameterValueType.Int
                    when value.Kind == EventGraphValueKind.Int32 =>
                    value.Int32Value,
                PoseParameterValueType.Bool
                    when value.Kind == EventGraphValueKind.Bool =>
                    value.BoolValue ? 1f : 0f,
                _ => throw new InvalidOperationException(
                    $"Pose parameter '{declaration.ParameterId}' value kind is inconsistent.")
            };
        }

        static void Swap<T>(ref T left, ref T right)
        {
            T value = left;
            left = right;
            right = value;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeActionSlotSource));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            if (m_FrameOpen)
                DiscardFrame();
            m_Workspace.Dispose();
            m_Disposed = true;
        }
    }
}
