using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal enum CharacterAnimationSlotNativeTransitionMode : byte
    {
        None = 0,
        StandardBlend = 1,
        Inertialization = 2
    }

    internal readonly struct CharacterAnimationSlotNativeControl
    {
        internal CharacterAnimationSlotNativeControl(
            ulong generation,
            CharacterAnimationSlotNativeTransitionMode mode,
            int sourceProducerIndex,
            int targetProducerIndex)
        {
            if (generation == 0 ||
                (byte)mode > (byte)CharacterAnimationSlotNativeTransitionMode.Inertialization ||
                sourceProducerIndex < -1 || targetProducerIndex < -1)
            {
                throw new ArgumentException("Animation Slot native control is invalid.");
            }
            Generation = generation;
            Mode = mode;
            SourceProducerIndex = sourceProducerIndex;
            TargetProducerIndex = targetProducerIndex;
        }

        internal ulong Generation { get; }
        internal CharacterAnimationSlotNativeTransitionMode Mode { get; }
        internal int SourceProducerIndex { get; }
        internal int TargetProducerIndex { get; }
    }

    internal sealed class CharacterAnimationTransitionRouteRuntime
    {
        [Flags]
        enum PageField : ushort
        {
            None = 0,
            SelectionGeneration = 1 << 0,
            NativeGeneration = 1 << 1,
            LastPresentationRequestSequence = 1 << 2,
            CurrentEndpointKind = 1 << 3,
            CurrentSourceId = 1 << 4,
            ActiveRequest = 1 << 5,
            HasActiveRequest = 1 << 6,
            ReleasePermission = 1 << 7,
            PendingReleaseCompletion = 1 << 8,
            NativeControl = 1 << 9
        }

        sealed class Page
        {
            internal ulong SelectionGeneration = 1;
            internal ulong NativeGeneration = 1;
            internal ulong LastPresentationRequestSequence;
            internal AnimationBlendTransitionEndpointKind
                CurrentEndpointKind;
            internal AnimationPoseSourceId CurrentSourceId;
            internal PoseInertializationRequest ActiveRequest;
            internal bool HasActiveRequest;
            internal bool ReleasePermission;
            internal bool PendingReleaseCompletion;
            internal CharacterAnimationSlotNativeControl
                NativeControl;
        }

        readonly struct RouteExecution
        {
            internal RouteExecution(AnimationBlendTransitionPayload transition, bool executeAsHardCut)
            {
                Transition = transition ?? throw new ArgumentNullException(nameof(transition));
                ExecuteAsHardCut = executeAsHardCut;
            }

            internal AnimationBlendTransitionPayload Transition { get; }
            internal bool ExecuteAsHardCut { get; }
        }

        readonly AnimationBlendNodePayload m_BlendNode;
        readonly CharacterAnimationSlotDescriptor m_Slot;
        readonly CompiledTransitionRoutingPlan m_Plan;
        readonly TransitionRoutingWorkspace m_Workspace = new TransitionRoutingWorkspace();
        readonly Page m_Committed = new Page();
        readonly Page m_Pending = new Page();
        readonly Dictionary<int, TransitionEndpointId> m_SourceOwnerEndpoints = new Dictionary<int, TransitionEndpointId>();
        readonly Dictionary<TransitionRuleId, AnimationBlendTransitionPayload> m_TransitionsByRule =
            new Dictionary<TransitionRuleId, AnimationBlendTransitionPayload>();
        readonly TransitionRouteOwnerId m_OwnerId;
        readonly int m_AnimationSlotIndex;

        ulong m_FrameId;
        ulong m_NextSelectionGeneration = 2;
        ulong m_NextNativeGeneration = 2;
        PageField m_DirtyFields;
        bool m_FrameOpen;

        ulong m_SelectionGeneration
        {
            get => Read(PageField.SelectionGeneration, m_Committed.SelectionGeneration, m_Pending.SelectionGeneration);
            set => Write(PageField.SelectionGeneration, ref m_Committed.SelectionGeneration, ref m_Pending.SelectionGeneration, value);
        }

        ulong m_NativeGeneration
        {
            get => Read(PageField.NativeGeneration, m_Committed.NativeGeneration, m_Pending.NativeGeneration);
            set => Write(PageField.NativeGeneration, ref m_Committed.NativeGeneration, ref m_Pending.NativeGeneration, value);
        }

        ulong m_LastPresentationRequestSequence
        {
            get => Read(PageField.LastPresentationRequestSequence, m_Committed.LastPresentationRequestSequence, m_Pending.LastPresentationRequestSequence);
            set => Write(PageField.LastPresentationRequestSequence, ref m_Committed.LastPresentationRequestSequence, ref m_Pending.LastPresentationRequestSequence, value);
        }

        AnimationBlendTransitionEndpointKind m_CurrentEndpointKind
        {
            get => Read(PageField.CurrentEndpointKind, m_Committed.CurrentEndpointKind, m_Pending.CurrentEndpointKind);
            set => Write(PageField.CurrentEndpointKind, ref m_Committed.CurrentEndpointKind, ref m_Pending.CurrentEndpointKind, value);
        }

        AnimationPoseSourceId m_CurrentSourceId
        {
            get => Read(PageField.CurrentSourceId, m_Committed.CurrentSourceId, m_Pending.CurrentSourceId);
            set => Write(PageField.CurrentSourceId, ref m_Committed.CurrentSourceId, ref m_Pending.CurrentSourceId, value);
        }

        PoseInertializationRequest m_ActiveRequest
        {
            get => Read(PageField.ActiveRequest, m_Committed.ActiveRequest, m_Pending.ActiveRequest);
            set => Write(PageField.ActiveRequest, ref m_Committed.ActiveRequest, ref m_Pending.ActiveRequest, value);
        }

        bool m_HasActiveRequest
        {
            get => Read(PageField.HasActiveRequest, m_Committed.HasActiveRequest, m_Pending.HasActiveRequest);
            set => Write(PageField.HasActiveRequest, ref m_Committed.HasActiveRequest, ref m_Pending.HasActiveRequest, value);
        }

        bool m_ReleasePermission
        {
            get => Read(PageField.ReleasePermission, m_Committed.ReleasePermission, m_Pending.ReleasePermission);
            set => Write(PageField.ReleasePermission, ref m_Committed.ReleasePermission, ref m_Pending.ReleasePermission, value);
        }

        bool m_PendingReleaseCompletion
        {
            get => Read(PageField.PendingReleaseCompletion, m_Committed.PendingReleaseCompletion, m_Pending.PendingReleaseCompletion);
            set => Write(PageField.PendingReleaseCompletion, ref m_Committed.PendingReleaseCompletion, ref m_Pending.PendingReleaseCompletion, value);
        }

        CharacterAnimationSlotNativeControl m_NativeControl
        {
            get => Read(PageField.NativeControl, m_Committed.NativeControl, m_Pending.NativeControl);
            set => Write(PageField.NativeControl, ref m_Committed.NativeControl, ref m_Pending.NativeControl, value);
        }

        internal CharacterAnimationTransitionRouteRuntime(
            AnimationBlendNodePayload blendNode,
            CharacterAnimationSlotDescriptor slot)
        {
            m_BlendNode = blendNode ?? throw new ArgumentNullException(nameof(blendNode));
            m_Slot = slot;
            if (slot != null)
            {
                slot.RequireValid();
                if (slot.NodeId != blendNode.NodeId)
                    throw new ArgumentException("Animation Slot route owner does not match its Blend payload.");
                m_AnimationSlotIndex = slot.Index;
                m_OwnerId = slot.RoutingOwnerId;
                m_Plan = LoadSlotPlan(
                    slot,
                    blendNode,
                    m_SourceOwnerEndpoints,
                    m_TransitionsByRule);
                if (!string.Equals(m_Plan.PlanId.ToString(), slot.RoutingPlanId, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Animation Slot '{slot.NodeId}' Routing Plan identity is inconsistent.");
            }
            else
            {
                m_AnimationSlotIndex = -1;
                m_OwnerId = new TransitionRouteOwnerId($"animation-blend/{blendNode.NodeId}");
                m_Plan = LoadBlendPlan(
                    blendNode,
                    m_SourceOwnerEndpoints,
                    m_TransitionsByRule);
            }
            m_NativeControl = new CharacterAnimationSlotNativeControl(
                m_NativeGeneration,
                CharacterAnimationSlotNativeTransitionMode.None,
                -1,
                -1);
            Reset();
        }

        internal PoseNodeId NodeId => m_BlendNode.NodeId;
        internal bool IsAnimationSlot => m_Slot != null;
        internal AnimationSlotId SlotId =>
            m_Slot?.SlotId ?? default;
        internal int AnimationSlotIndex => m_AnimationSlotIndex;
        internal CharacterAnimationSlotNativeControl NativeControl => m_NativeControl;
        internal TransitionRoutingRuntimeSnapshot Snapshot => m_Workspace.Snapshot;
        internal bool HasOpenFrame => m_FrameOpen;

        internal void BeginFrame()
        {
            if (m_FrameOpen)
                throw new InvalidOperationException("Animation transition route frame is already open.");
            m_Workspace.BeginFrame();
            m_DirtyFields = PageField.None;
            m_FrameOpen = true;
        }

        internal void DiscardFrame()
        {
            if (!m_FrameOpen)
                return;
            m_Workspace.DiscardFrame();
            m_DirtyFields = PageField.None;
            m_FrameOpen = false;
        }

        internal void CommitFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Animation transition route frame is not open.");
            m_Workspace.CommitFrame();
            ApplyPending();
            m_DirtyFields = PageField.None;
            m_FrameOpen = false;
        }

        internal AnimationSlotRuntimeSnapshot CreateSlotSnapshot(
            in AnimationBlendStackSnapshot stack)
        {
            if (!IsAnimationSlot || stack.PoseNodeId != NodeId ||
                stack.AnimationChannelId != m_Slot.AnimationChannelId)
            {
                throw new InvalidOperationException("Animation Slot diagnostics source is inconsistent.");
            }
            AnimationSlotTransitionExecution execution = m_NativeControl.Mode switch
            {
                CharacterAnimationSlotNativeTransitionMode.None => AnimationSlotTransitionExecution.None,
                CharacterAnimationSlotNativeTransitionMode.StandardBlend =>
                    AnimationSlotTransitionExecution.StandardBlend,
                CharacterAnimationSlotNativeTransitionMode.Inertialization =>
                    AnimationSlotTransitionExecution.Inertialization,
                _ => throw new InvalidOperationException("Animation Slot transition mode is invalid.")
            };
            return new AnimationSlotRuntimeSnapshot(
                m_Slot.SlotId,
                NodeId,
                m_Slot.AnimationChannelId,
                m_CurrentEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner,
                m_CurrentSourceId,
                m_Slot.SourceUsage.KeepSourcePoseUpdating,
                m_Slot.SourceUsage.SourcePoseValueIndex,
                stack.Availability,
                stack.OutputWeight,
                execution,
                m_ReleasePermission,
                m_PendingReleaseCompletion,
                m_Workspace.Snapshot);
        }

        internal void PushSelection(
            AnimationBlendStackRuntime stack,
            in AnimationPoseSampleRequest selection)
        {
            if (stack == null || stack.PoseNodeId != NodeId || !selection.IsValid)
                throw new ArgumentException("Animation transition selection is invalid.");
            bool sameSelection =
                m_CurrentEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner &&
                m_CurrentSourceId.Equals(selection.SourceId);
            if (sameSelection)
            {
                ContinueRequestSequence(selection.PresentationRequestSequence);
                return;
            }
            RequireChangedRequestSequence(selection.PresentationRequestSequence);
            AdvanceSelectionGeneration();
            stack.GetCurrentRoutingEndpoint(
                out int sourceOwnerIndex,
                out AnimationBlendTransitionEndpointKind sourceEndpointKind);
            RouteExecution execution = Route(
                sourceOwnerIndex,
                sourceEndpointKind,
                selection.SourceOwnerIndex,
                AnimationBlendTransitionEndpointKind.SourceOwner);
            stack.PushPoseRequest(in selection, execution.Transition, execution.ExecuteAsHardCut);
            m_LastPresentationRequestSequence = selection.PresentationRequestSequence;
            m_CurrentEndpointKind = AnimationBlendTransitionEndpointKind.SourceOwner;
            m_CurrentSourceId = selection.SourceId;
        }

        internal void PushSourcePose(
            AnimationBlendStackRuntime stack,
            ulong presentationRequestSequence)
        {
            if (!IsAnimationSlot ||
                stack == null ||
                stack.PoseNodeId != NodeId ||
                presentationRequestSequence == 0)
                throw new ArgumentException(
                    "Animation transition Source Pose selection is invalid.");
            if (m_CurrentEndpointKind == AnimationBlendTransitionEndpointKind.SourcePose)
            {
                ContinueRequestSequence(presentationRequestSequence);
                return;
            }
            RequireChangedRequestSequence(presentationRequestSequence);
            AdvanceSelectionGeneration();
            stack.GetCurrentRoutingEndpoint(
                out int sourceOwnerIndex,
                out AnimationBlendTransitionEndpointKind sourceEndpointKind);
            RouteExecution execution = Route(
                sourceOwnerIndex,
                sourceEndpointKind,
                -1,
                AnimationBlendTransitionEndpointKind.SourcePose);
            stack.PushSourcePose(
                presentationRequestSequence,
                execution.Transition,
                execution.ExecuteAsHardCut);
            m_LastPresentationRequestSequence = presentationRequestSequence;
            m_CurrentEndpointKind = AnimationBlendTransitionEndpointKind.SourcePose;
            m_CurrentSourceId = default;
        }

        internal bool CanReleaseSources => !m_HasActiveRequest || m_ReleasePermission;

        internal void NotifyNativeFrameCompleted(
            PoseInertializationNativeProgram inertialization,
            ulong completionIdentity)
        {
            if (!IsAnimationSlot || !m_HasActiveRequest || m_ReleasePermission)
                return;
            PoseInertializationNativeState state = inertialization.GetAnimationSlotState(m_AnimationSlotIndex);
            if (state.OutputCompletionIdentity != completionIdentity ||
                state.LastEventIdentity != m_NativeControl.Generation ||
                state.RuntimeState == PoseInertializationRuntimeState.Invalid)
            {
                return;
            }
            TransitionRoutingFrameOutput output = Step(
                m_ActiveRequest.TargetEndpoint,
                m_ActiveRequest.TargetEndpoint,
                new TransitionCompletionFact(
                    true,
                    m_ActiveRequest.RequestEventId,
                    m_ActiveRequest.RequestGeneration,
                    true),
                TransitionCompletionFact.None);
            if (output.IsInvalid || !output.ReleasePermission)
                throw new InvalidOperationException(
                    $"Animation Slot '{NodeId}' capture completion failed: [{output.ReasonCode}] {output.Reason}");
            m_ReleasePermission = true;
            if (m_ActiveRequest.SourceEndpoint.IsSourcePose)
                m_PendingReleaseCompletion = true;
        }

        internal void NotifySourcesReleased()
        {
            if (!m_HasActiveRequest || !m_ReleasePermission)
                return;
            m_PendingReleaseCompletion = true;
        }

        internal void FlushReleaseCompletion()
        {
            if (!m_PendingReleaseCompletion)
                return;
            TransitionRoutingFrameOutput output = Step(
                m_ActiveRequest.TargetEndpoint,
                m_ActiveRequest.TargetEndpoint,
                TransitionCompletionFact.None,
                new TransitionCompletionFact(
                    true,
                    m_ActiveRequest.RequestEventId,
                    m_ActiveRequest.RequestGeneration,
                    true));
            if (output.IsInvalid ||
                output.CompletionOutcome != TransitionRoutingCompletionOutcome.ReleaseCompleted)
            {
                throw new InvalidOperationException(
                    $"Animation Slot '{NodeId}' release completion failed: [{output.ReasonCode}] {output.Reason}");
            }
            m_PendingReleaseCompletion = false;
            m_ReleasePermission = false;
            m_HasActiveRequest = false;
            m_ActiveRequest = default;
        }

        internal void Reset()
        {
            AdvanceSelectionGeneration();
            AnimationBlendTransitionEndpointKind resetEndpointKind =
                IsAnimationSlot
                    ? AnimationBlendTransitionEndpointKind.SourcePose
                    : AnimationBlendTransitionEndpointKind.NoPose;
            TransitionEndpointId resetEndpoint =
                ResolveEndpoint(-1, resetEndpointKind);
            TransitionRoutingFrameOutput output = TransitionRoutingRuntime.Step(
                m_Plan,
                m_Workspace,
                new TransitionRoutingFrameInput(
                    m_Plan.PlanId,
                    NextFrameId(),
                    m_OwnerId,
                    resetEndpoint,
                    resetEndpoint,
                    new TransitionSelectionGeneration(m_SelectionGeneration),
                    true,
                    true,
                    TransitionCompletionFact.None,
                    TransitionCompletionFact.None,
                    TransitionRoutingResetReason.Explicit));
            if (output.IsInvalid)
                throw new InvalidOperationException(
                    $"Animation transition owner '{NodeId}' reset failed: [{output.ReasonCode}] {output.Reason}");
            m_CurrentEndpointKind = resetEndpointKind;
            m_CurrentSourceId = default;
            m_ActiveRequest = default;
            m_HasActiveRequest = false;
            m_ReleasePermission = false;
            m_PendingReleaseCompletion = false;
            m_LastPresentationRequestSequence = 0;
            m_NativeGeneration =
                AllocateNativeGeneration();
            m_NativeControl = new CharacterAnimationSlotNativeControl(
                m_NativeGeneration,
                CharacterAnimationSlotNativeTransitionMode.None,
                -1,
                -1);
        }

        RouteExecution Route(
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind sourceEndpointKind,
            int targetOwnerIndex,
            AnimationBlendTransitionEndpointKind targetEndpointKind)
        {
            TransitionEndpointId source =
                ResolveEndpoint(sourceOwnerIndex, sourceEndpointKind);
            TransitionEndpointId target =
                ResolveEndpoint(targetOwnerIndex, targetEndpointKind);
            TransitionRoutingFrameOutput output = Step(
                source,
                target,
                TransitionCompletionFact.None,
                TransitionCompletionFact.None);
            if (output.IsInvalid)
                throw new InvalidOperationException(
                    $"Animation transition owner '{NodeId}' route failed: [{output.ReasonCode}] {output.Reason}");
            if (!m_TransitionsByRule.TryGetValue(output.ActiveRuleId, out AnimationBlendTransitionPayload transition))
            {
                throw new InvalidOperationException(
                    $"Animation transition owner '{NodeId}' did not resolve its compiled exact rule.");
            }

            if (output.HasStandardBlendCommand)
            {
                if (output.StandardBlendCommand.DurationSeconds != transition.DurationSeconds ||
                    !string.Equals(
                        output.StandardBlendCommand.BlendCurveId.Value,
                        $"curve/{transition.CurveIndex}",
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        output.StandardBlendCommand.BlendProfileId.Value,
                        $"profile/{transition.BlendProfileIndex}",
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Animation transition owner '{NodeId}' Standard Blend command is inconsistent.");
                }
                m_ActiveRequest = default;
                m_HasActiveRequest = false;
                m_ReleasePermission = false;
                m_PendingReleaseCompletion = false;
                PublishNativeControl(
                    CharacterAnimationSlotNativeTransitionMode.StandardBlend,
                    sourceOwnerIndex,
                    targetOwnerIndex);
                return new RouteExecution(transition, false);
            }
            if (output.HasInertializationRequest && output.CapturePermission)
            {
                m_ActiveRequest = output.InertializationRequest;
                m_HasActiveRequest = true;
                m_ReleasePermission = false;
                m_PendingReleaseCompletion = false;
                PublishNativeControl(
                    CharacterAnimationSlotNativeTransitionMode.Inertialization,
                    sourceOwnerIndex,
                    targetOwnerIndex);
                return new RouteExecution(transition, true);
            }
            throw new InvalidOperationException(
                $"Animation transition owner '{NodeId}' produced unsupported route '{output.RouteDecision}'.");
        }

        TransitionRoutingFrameOutput Step(
            TransitionEndpointId source,
            TransitionEndpointId target,
            TransitionCompletionFact captureCompletion,
            TransitionCompletionFact releaseCompletion) =>
            TransitionRoutingRuntime.Step(
                m_Plan,
                m_Workspace,
                new TransitionRoutingFrameInput(
                    m_Plan.PlanId,
                    NextFrameId(),
                    m_OwnerId,
                    source,
                    target,
                    new TransitionSelectionGeneration(m_SelectionGeneration),
                    true,
                    true,
                    captureCompletion,
                    releaseCompletion,
                    TransitionRoutingResetReason.None));

        void PublishNativeControl(
            CharacterAnimationSlotNativeTransitionMode mode,
            int sourceOwnerIndex,
            int targetOwnerIndex)
        {
            if (!IsAnimationSlot)
                return;
            m_NativeGeneration =
                AllocateNativeGeneration();
            m_NativeControl = new CharacterAnimationSlotNativeControl(
                m_NativeGeneration,
                mode,
                sourceOwnerIndex,
                targetOwnerIndex);
        }

        TransitionEndpointId ResolveEndpoint(
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind endpointKind)
        {
            if ((byte)endpointKind < (byte)AnimationBlendTransitionEndpointKind.SourceOwner ||
                (byte)endpointKind > (byte)AnimationBlendTransitionEndpointKind.NoPose)
            {
                throw new InvalidOperationException(
                    $"Animation transition owner '{NodeId}' endpoint kind is invalid.");
            }
            if (endpointKind == AnimationBlendTransitionEndpointKind.SourcePose)
            {
                if (sourceOwnerIndex != -1 || !IsAnimationSlot)
                    throw new InvalidOperationException($"Animation transition owner '{NodeId}' Source Pose endpoint is invalid.");
                return TransitionEndpointId.SourcePose;
            }
            if (endpointKind == AnimationBlendTransitionEndpointKind.NoPose)
            {
                if (sourceOwnerIndex != -1 || IsAnimationSlot)
                    throw new InvalidOperationException($"Animation transition owner '{NodeId}' No Pose endpoint is invalid.");
                return TransitionEndpointId.NoPose;
            }
            if (!m_SourceOwnerEndpoints.TryGetValue(sourceOwnerIndex, out TransitionEndpointId endpoint))
                throw new InvalidOperationException(
                    $"Animation transition owner '{NodeId}' references unknown source owner '{sourceOwnerIndex}'.");
            return endpoint;
        }

        void AdvanceSelectionGeneration()
        {
            if (m_NextSelectionGeneration ==
                ulong.MaxValue)
                throw new InvalidOperationException($"Animation transition owner '{NodeId}' selection generation was exhausted.");
            m_SelectionGeneration =
                m_NextSelectionGeneration++;
        }

        ulong AllocateNativeGeneration()
        {
            if (m_NextNativeGeneration ==
                ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Animation Slot '{NodeId}' native generation was exhausted.");
            }
            return m_NextNativeGeneration++;
        }

        void RequireChangedRequestSequence(ulong presentationRequestSequence)
        {
            if (presentationRequestSequence == 0 ||
                presentationRequestSequence <= m_LastPresentationRequestSequence)
            {
                throw new InvalidOperationException(
                    $"Animation transition owner '{NodeId}' target request sequence is stale.");
            }
        }

        void ContinueRequestSequence(ulong presentationRequestSequence)
        {
            if (presentationRequestSequence == 0 ||
                presentationRequestSequence < m_LastPresentationRequestSequence)
            {
                throw new InvalidOperationException(
                    $"Animation transition owner '{NodeId}' continuation request sequence is stale.");
            }
            m_LastPresentationRequestSequence = presentationRequestSequence;
        }

        TransitionFrameId NextFrameId()
        {
            if (m_FrameId == ulong.MaxValue)
                throw new InvalidOperationException($"Animation transition owner '{NodeId}' frame identity was exhausted.");
            return new TransitionFrameId(++m_FrameId);
        }

        T Read<T>(PageField field, T committed, T pending) =>
            m_FrameOpen && (m_DirtyFields & field) != 0
                ? pending
                : committed;

        void Write<T>(PageField field, ref T committed, ref T pending, T value)
        {
            if (!m_FrameOpen)
            {
                committed = value;
                return;
            }
            pending = value;
            m_DirtyFields |= field;
        }

        void Apply<T>(PageField field, ref T committed, T pending)
        {
            if ((m_DirtyFields & field) != 0)
                committed = pending;
        }

        void ApplyPending()
        {
            Apply(PageField.SelectionGeneration, ref m_Committed.SelectionGeneration, m_Pending.SelectionGeneration);
            Apply(PageField.NativeGeneration, ref m_Committed.NativeGeneration, m_Pending.NativeGeneration);
            Apply(PageField.LastPresentationRequestSequence, ref m_Committed.LastPresentationRequestSequence, m_Pending.LastPresentationRequestSequence);
            Apply(PageField.CurrentEndpointKind, ref m_Committed.CurrentEndpointKind, m_Pending.CurrentEndpointKind);
            Apply(PageField.CurrentSourceId, ref m_Committed.CurrentSourceId, m_Pending.CurrentSourceId);
            Apply(PageField.ActiveRequest, ref m_Committed.ActiveRequest, m_Pending.ActiveRequest);
            Apply(PageField.HasActiveRequest, ref m_Committed.HasActiveRequest, m_Pending.HasActiveRequest);
            Apply(PageField.ReleasePermission, ref m_Committed.ReleasePermission, m_Pending.ReleasePermission);
            Apply(PageField.PendingReleaseCompletion, ref m_Committed.PendingReleaseCompletion, m_Pending.PendingReleaseCompletion);
            Apply(PageField.NativeControl, ref m_Committed.NativeControl, m_Pending.NativeControl);
        }

        static CompiledTransitionRoutingPlan LoadSlotPlan(
            CharacterAnimationSlotDescriptor slot,
            AnimationBlendNodePayload blendNode,
            Dictionary<int, TransitionEndpointId> sourceOwnerEndpoints,
            Dictionary<TransitionRuleId, AnimationBlendTransitionPayload> transitionsByRule)
        {
            var endpointDescriptors = new Dictionary<TransitionEndpointId, CharacterAnimationSlotEndpointDescriptor>();
            for (int i = 0; i < slot.Endpoints.Count; i++)
            {
                CharacterAnimationSlotEndpointDescriptor endpoint = slot.Endpoints[i];
                endpointDescriptors.Add(endpoint.EndpointId, endpoint);
                if (!endpoint.SourcePose)
                    sourceOwnerEndpoints.Add(endpoint.ProgramProducerIndex, endpoint.EndpointId);
            }
            CompiledTransitionRoutingPlan plan =
                slot.LoadRoutingPlan();
            if (plan.Endpoints.Count != slot.Endpoints.Count ||
                plan.Rules.Count != slot.RequestRoutes.Count)
            {
                throw new InvalidOperationException(
                    $"Animation Slot '{slot.NodeId}' compiled Routing Plan shape is inconsistent.");
            }
            for (int i = 0; i < plan.Endpoints.Count; i++)
            {
                if (!endpointDescriptors.ContainsKey(
                        plan.Endpoints[i]))
                {
                    throw new InvalidOperationException(
                        $"Animation Slot '{slot.NodeId}' compiled Routing Plan contains an unknown endpoint.");
                }
            }
            for (int i = 0; i < slot.RequestRoutes.Count; i++)
            {
                CharacterAnimationSlotRequestRouteDescriptor route = slot.RequestRoutes[i];
                CharacterAnimationSlotEndpointDescriptor source = endpointDescriptors[route.SourceEndpointId];
                CharacterAnimationSlotEndpointDescriptor target = endpointDescriptors[route.TargetEndpointId];
                AnimationBlendTransitionPayload transition = blendNode.RequireTransition(
                    source.ProgramProducerIndex,
                    source.SourcePose
                        ? AnimationBlendTransitionEndpointKind.SourcePose
                        : AnimationBlendTransitionEndpointKind.SourceOwner,
                    target.ProgramProducerIndex,
                    target.SourcePose
                        ? AnimationBlendTransitionEndpointKind.SourcePose
                        : AnimationBlendTransitionEndpointKind.SourceOwner);
                transitionsByRule.Add(route.RuleId, transition);
                if (!plan.TryGetRule(
                        route.SourceEndpointId,
                        route.TargetEndpointId,
                        out AnimationTransitionRule compiled) ||
                    compiled.RuleId != route.RuleId ||
                    compiled.BlendLogic != route.BlendLogic ||
                    compiled.DurationSeconds !=
                        route.DurationSeconds ||
                    !compiled.BlendCurveId.Equals(
                        new TransitionBlendCurveId(
                            $"curve/{route.CurveIndex}")) ||
                    !compiled.BlendProfileId.Equals(
                        new TransitionBlendProfileId(
                            $"profile/{route.BlendProfileIndex}")))
                {
                    throw new InvalidOperationException(
                        $"Animation Slot '{slot.NodeId}' route '{route.RuleId}' does not match its compiled Routing Plan.");
                }
            }
            return plan;
        }

        static CompiledTransitionRoutingPlan LoadBlendPlan(
            AnimationBlendNodePayload blendNode,
            Dictionary<int, TransitionEndpointId> sourceOwnerEndpoints,
            Dictionary<TransitionRuleId, AnimationBlendTransitionPayload> transitionsByRule)
        {
            var identities = new Dictionary<int, string>();
            for (int i = 0; i < blendNode.Transitions.Count; i++)
            {
                AnimationBlendTransitionPayload transition = blendNode.Transitions[i];
                if (transition.SourceEndpointKind != AnimationBlendTransitionEndpointKind.SourceOwner &&
                    transition.SourceEndpointKind != AnimationBlendTransitionEndpointKind.NoPose ||
                    transition.TargetEndpointKind != AnimationBlendTransitionEndpointKind.SourceOwner)
                {
                    throw new InvalidOperationException(
                        $"Animation Blend Stack '{blendNode.NodeId}' contains an endpoint outside its state-local route contract.");
                }
                CollectIdentity(
                    transition.SourceOwnerIndex,
                    transition.SourceEndpointKind,
                    transition.SourceOwnerIdentity,
                    identities);
                CollectIdentity(
                    transition.TargetOwnerIndex,
                    transition.TargetEndpointKind,
                    transition.TargetOwnerIdentity,
                    identities);
            }
            foreach (KeyValuePair<int, string> owner in identities.OrderBy(value => value.Value, StringComparer.Ordinal))
            {
                var endpoint = new TransitionEndpointId(
                    $"animation-blend/{blendNode.NodeId}/owner/{owner.Value}");
                sourceOwnerEndpoints.Add(owner.Key, endpoint);
            }
            CompiledTransitionRoutingPlan plan =
                blendNode.LoadRoutingPlan();
            for (int i = 0; i < blendNode.Transitions.Count; i++)
            {
                AnimationBlendTransitionPayload transition = blendNode.Transitions[i];
                TransitionEndpointId source = ResolveBlendEndpoint(
                    transition.SourceOwnerIndex,
                    transition.SourceEndpointKind,
                    sourceOwnerEndpoints);
                TransitionEndpointId target = ResolveBlendEndpoint(
                    transition.TargetOwnerIndex,
                    transition.TargetEndpointKind,
                    sourceOwnerEndpoints);
                var ruleId = new TransitionRuleId(
                    $"animation-blend/{blendNode.NodeId}/route/{StableHash.Compute(source.Value, target.Value)}");
                transitionsByRule.Add(ruleId, transition);
                if (!plan.TryGetRule(
                        source,
                        target,
                        out AnimationTransitionRule compiled) ||
                    compiled.RuleId != ruleId ||
                    compiled.BlendLogic != transition.BlendLogic ||
                    compiled.DurationSeconds !=
                        transition.DurationSeconds ||
                    !compiled.BlendCurveId.Equals(
                        new TransitionBlendCurveId(
                            $"curve/{transition.CurveIndex}")) ||
                    !compiled.BlendProfileId.Equals(
                        new TransitionBlendProfileId(
                            $"profile/{transition.BlendProfileIndex}")))
                {
                    throw new InvalidOperationException(
                        $"Animation Blend Stack '{blendNode.NodeId}' route '{ruleId}' does not match its compiled Routing Plan.");
                }
            }
            return plan;
        }

        static void CollectIdentity(
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind endpointKind,
            string sourceOwnerIdentity,
            Dictionary<int, string> identities)
        {
            if (endpointKind != AnimationBlendTransitionEndpointKind.SourceOwner)
                return;
            if (sourceOwnerIndex < 0 || string.IsNullOrWhiteSpace(sourceOwnerIdentity))
                throw new InvalidOperationException("Animation transition source owner endpoint is invalid.");
            if (identities.TryGetValue(sourceOwnerIndex, out string existing) &&
                !string.Equals(existing, sourceOwnerIdentity, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Animation transition source owner index '{sourceOwnerIndex}' resolves to multiple identities.");
            }
            identities[sourceOwnerIndex] = sourceOwnerIdentity;
        }

        static TransitionEndpointId ResolveBlendEndpoint(
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind endpointKind,
            IReadOnlyDictionary<int, TransitionEndpointId> sourceOwnerEndpoints) =>
            endpointKind switch
            {
                AnimationBlendTransitionEndpointKind.SourceOwner =>
                    sourceOwnerEndpoints[sourceOwnerIndex],
                AnimationBlendTransitionEndpointKind.SourcePose =>
                    TransitionEndpointId.SourcePose,
                AnimationBlendTransitionEndpointKind.NoPose =>
                    TransitionEndpointId.NoPose,
                _ => throw new InvalidOperationException(
                    "Animation Blend transition endpoint kind is invalid.")
            };
    }
}
