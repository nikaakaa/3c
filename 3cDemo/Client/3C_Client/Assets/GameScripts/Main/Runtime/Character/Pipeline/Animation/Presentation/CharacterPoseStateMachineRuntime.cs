using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal enum CharacterPoseStateMachineBlendMode : byte
    {
        Single = 1,
        Standard = 2,
        Inertialization = 3
    }

    internal readonly struct CharacterPoseStateMachineNativeControl
    {
        internal CharacterPoseStateMachineNativeControl(
            int sourcePoseValueIndex,
            int targetPoseValueIndex,
            int sourceStateIndex,
            int targetStateIndex,
            float elapsedSeconds,
            float durationSeconds,
            int curveIndex,
            int blendProfileIndex,
            CharacterPoseStateMachineBlendMode blendMode,
            ulong generation)
        {
            if (sourcePoseValueIndex < 0 || targetPoseValueIndex < 0 ||
                sourceStateIndex < 0 || targetStateIndex < 0 ||
                !float.IsFinite(elapsedSeconds) || elapsedSeconds < 0f ||
                !float.IsFinite(durationSeconds) || durationSeconds < 0f ||
                (blendMode == CharacterPoseStateMachineBlendMode.Standard && curveIndex < 0) ||
                (blendMode == CharacterPoseStateMachineBlendMode.Standard &&
                 durationSeconds > 0f && blendProfileIndex < 0) ||
                (byte)blendMode < (byte)CharacterPoseStateMachineBlendMode.Single ||
                (byte)blendMode > (byte)CharacterPoseStateMachineBlendMode.Inertialization ||
                generation == 0)
            {
                throw new ArgumentException("Pose StateMachine native control is invalid.");
            }
            SourcePoseValueIndex = sourcePoseValueIndex;
            TargetPoseValueIndex = targetPoseValueIndex;
            SourceStateIndex = sourceStateIndex;
            TargetStateIndex = targetStateIndex;
            ElapsedSeconds = elapsedSeconds;
            DurationSeconds = durationSeconds;
            CurveIndex = curveIndex;
            BlendProfileIndex = blendProfileIndex;
            BlendMode = blendMode;
            Generation = generation;
        }

        internal int SourcePoseValueIndex { get; }
        internal int TargetPoseValueIndex { get; }
        internal int SourceStateIndex { get; }
        internal int TargetStateIndex { get; }
        internal float ElapsedSeconds { get; }
        internal float DurationSeconds { get; }
        internal int CurveIndex { get; }
        internal int BlendProfileIndex { get; }
        internal CharacterPoseStateMachineBlendMode BlendMode { get; }
        internal ulong Generation { get; }
    }

    internal interface ICharacterPoseStateSourceRuntime
    {
        void SetRelevant(
            PoseStateSourceProviderPlan provider,
            bool relevant,
            PoseSourceProviderDemandKind demandKind);
        void Reset(PoseStateSourceProviderPlan provider);
        PoseSourceProviderStatus GetStatus(PoseStateSourceProviderPlan provider);
        float GetRemainingTime(PoseStateSourceProviderPlan provider);
        bool TrySynchronize(
            CharacterPoseStateSourceSyncPlan plan,
            bool establishRelation,
            out double targetEffectiveTime);
        void ClearSynchronization(CharacterPoseStateSourceSyncPlan plan);
    }

    internal sealed class CharacterPoseStateMachineRuntime
    {
        [Flags]
        enum PageField : uint
        {
            None = 0,
            ActiveStateIndex = 1u << 0,
            BlendSourceStateIndex = 1u << 1,
            BlendTargetStateIndex = 1u << 2,
            TimeInState = 1u << 3,
            BlendElapsed = 1u << 4,
            BlendDuration = 1u << 5,
            FactGeneration = 1u << 6,
            SelectionGeneration = 1u << 7,
            ControlGeneration = 1u << 8,
            ActiveTransition = 1u << 9,
            EvaluatedTransitionId = 1u << 10,
            PendingInertializationRequest = 1u << 11,
            CaptureCompletion = 1u << 12,
            ReleaseCompletion = 1u << 13,
            Initialized = 1u << 14,
            HasPendingCapture = 1u << 15,
            HasPendingRelease = 1u << 16,
            HasTransitionRuleResult = 1u << 17,
            TransitionRuleResult = 1u << 18,
            CanPublishPose = 1u << 19,
            HasPendingTarget = 1u << 20,
            PendingTargetTransition = 1u << 21,
            FrameFailure = 1u << 22
        }

        sealed class Page
        {
            internal int ActiveStateIndex = -1;
            internal int BlendSourceStateIndex = -1;
            internal int BlendTargetStateIndex = -1;
            internal float TimeInState;
            internal float BlendElapsed;
            internal float BlendDuration;
            internal ulong FactGeneration;
            internal ulong SelectionGeneration = 1;
            internal ulong ControlGeneration = 1;
            internal CharacterPoseStateTransitionDescriptor ActiveTransition;
            internal PoseStateTransitionId EvaluatedTransitionId;
            internal PoseInertializationRequest PendingInertializationRequest;
            internal TransitionCompletionFact CaptureCompletion;
            internal TransitionCompletionFact ReleaseCompletion;
            internal bool Initialized;
            internal bool HasPendingCapture;
            internal bool HasPendingRelease;
            internal bool HasTransitionRuleResult;
            internal bool TransitionRuleResult;
            internal bool CanPublishPose;
            internal bool HasPendingTarget;
            internal CharacterPoseStateTransitionDescriptor PendingTargetTransition;
            internal PresentationFrameFailure FrameFailure;
        }

        readonly CharacterPoseStateMachineDescriptor m_Descriptor;
        readonly CompiledTransitionRoutingPlan m_RoutingPlan;
        readonly TransitionRoutingWorkspace m_RoutingWorkspace = new TransitionRoutingWorkspace();
        Page m_Committed = new Page();
        Page m_Pending = new Page();

        readonly CharacterPoseTransitionRuleValue[] m_RuleValues;
        readonly int[][] m_TransitionsBySource;
        readonly float[] m_TransitionDurations;
        readonly float[] m_TransitionCompletionDurations;

        ulong m_NextSelectionGeneration = 2;
        ulong m_NextControlGeneration = 2;
        ulong m_RoutingFrame;
        PageField m_DirtyFields;
        bool m_FrameOpen;

        int m_ActiveStateIndex
        {
            get => Read(PageField.ActiveStateIndex, m_Committed.ActiveStateIndex, m_Pending.ActiveStateIndex);
            set => Write(PageField.ActiveStateIndex, ref m_Committed.ActiveStateIndex, ref m_Pending.ActiveStateIndex, value);
        }

        int m_BlendSourceStateIndex
        {
            get => Read(PageField.BlendSourceStateIndex, m_Committed.BlendSourceStateIndex, m_Pending.BlendSourceStateIndex);
            set => Write(PageField.BlendSourceStateIndex, ref m_Committed.BlendSourceStateIndex, ref m_Pending.BlendSourceStateIndex, value);
        }

        int m_BlendTargetStateIndex
        {
            get => Read(PageField.BlendTargetStateIndex, m_Committed.BlendTargetStateIndex, m_Pending.BlendTargetStateIndex);
            set => Write(PageField.BlendTargetStateIndex, ref m_Committed.BlendTargetStateIndex, ref m_Pending.BlendTargetStateIndex, value);
        }

        float m_TimeInState
        {
            get => Read(PageField.TimeInState, m_Committed.TimeInState, m_Pending.TimeInState);
            set => Write(PageField.TimeInState, ref m_Committed.TimeInState, ref m_Pending.TimeInState, value);
        }

        float m_BlendElapsed
        {
            get => Read(PageField.BlendElapsed, m_Committed.BlendElapsed, m_Pending.BlendElapsed);
            set => Write(PageField.BlendElapsed, ref m_Committed.BlendElapsed, ref m_Pending.BlendElapsed, value);
        }

        float m_BlendDuration
        {
            get => Read(PageField.BlendDuration, m_Committed.BlendDuration, m_Pending.BlendDuration);
            set => Write(PageField.BlendDuration, ref m_Committed.BlendDuration, ref m_Pending.BlendDuration, value);
        }

        ulong m_FactGeneration
        {
            get => Read(PageField.FactGeneration, m_Committed.FactGeneration, m_Pending.FactGeneration);
            set => Write(PageField.FactGeneration, ref m_Committed.FactGeneration, ref m_Pending.FactGeneration, value);
        }

        ulong m_SelectionGeneration
        {
            get => Read(PageField.SelectionGeneration, m_Committed.SelectionGeneration, m_Pending.SelectionGeneration);
            set => Write(PageField.SelectionGeneration, ref m_Committed.SelectionGeneration, ref m_Pending.SelectionGeneration, value);
        }

        ulong m_ControlGeneration
        {
            get => Read(PageField.ControlGeneration, m_Committed.ControlGeneration, m_Pending.ControlGeneration);
            set => Write(PageField.ControlGeneration, ref m_Committed.ControlGeneration, ref m_Pending.ControlGeneration, value);
        }

        CharacterPoseStateTransitionDescriptor m_ActiveTransition
        {
            get => Read(PageField.ActiveTransition, m_Committed.ActiveTransition, m_Pending.ActiveTransition);
            set => Write(PageField.ActiveTransition, ref m_Committed.ActiveTransition, ref m_Pending.ActiveTransition, value);
        }

        PoseStateTransitionId m_EvaluatedTransitionId
        {
            get => Read(PageField.EvaluatedTransitionId, m_Committed.EvaluatedTransitionId, m_Pending.EvaluatedTransitionId);
            set => Write(PageField.EvaluatedTransitionId, ref m_Committed.EvaluatedTransitionId, ref m_Pending.EvaluatedTransitionId, value);
        }

        PoseInertializationRequest m_PendingInertializationRequest
        {
            get => Read(PageField.PendingInertializationRequest, m_Committed.PendingInertializationRequest, m_Pending.PendingInertializationRequest);
            set => Write(PageField.PendingInertializationRequest, ref m_Committed.PendingInertializationRequest, ref m_Pending.PendingInertializationRequest, value);
        }

        TransitionCompletionFact m_CaptureCompletion
        {
            get => Read(PageField.CaptureCompletion, m_Committed.CaptureCompletion, m_Pending.CaptureCompletion);
            set => Write(PageField.CaptureCompletion, ref m_Committed.CaptureCompletion, ref m_Pending.CaptureCompletion, value);
        }

        TransitionCompletionFact m_ReleaseCompletion
        {
            get => Read(PageField.ReleaseCompletion, m_Committed.ReleaseCompletion, m_Pending.ReleaseCompletion);
            set => Write(PageField.ReleaseCompletion, ref m_Committed.ReleaseCompletion, ref m_Pending.ReleaseCompletion, value);
        }

        bool m_Initialized
        {
            get => Read(PageField.Initialized, m_Committed.Initialized, m_Pending.Initialized);
            set => Write(PageField.Initialized, ref m_Committed.Initialized, ref m_Pending.Initialized, value);
        }

        bool m_HasPendingCapture
        {
            get => Read(PageField.HasPendingCapture, m_Committed.HasPendingCapture, m_Pending.HasPendingCapture);
            set => Write(PageField.HasPendingCapture, ref m_Committed.HasPendingCapture, ref m_Pending.HasPendingCapture, value);
        }

        bool m_HasPendingRelease
        {
            get => Read(PageField.HasPendingRelease, m_Committed.HasPendingRelease, m_Pending.HasPendingRelease);
            set => Write(PageField.HasPendingRelease, ref m_Committed.HasPendingRelease, ref m_Pending.HasPendingRelease, value);
        }

        bool m_HasTransitionRuleResult
        {
            get => Read(PageField.HasTransitionRuleResult, m_Committed.HasTransitionRuleResult, m_Pending.HasTransitionRuleResult);
            set => Write(PageField.HasTransitionRuleResult, ref m_Committed.HasTransitionRuleResult, ref m_Pending.HasTransitionRuleResult, value);
        }

        bool m_TransitionRuleResult
        {
            get => Read(PageField.TransitionRuleResult, m_Committed.TransitionRuleResult, m_Pending.TransitionRuleResult);
            set => Write(PageField.TransitionRuleResult, ref m_Committed.TransitionRuleResult, ref m_Pending.TransitionRuleResult, value);
        }

        bool m_CanPublishPose
        {
            get => Read(PageField.CanPublishPose, m_Committed.CanPublishPose, m_Pending.CanPublishPose);
            set => Write(PageField.CanPublishPose, ref m_Committed.CanPublishPose, ref m_Pending.CanPublishPose, value);
        }

        bool m_HasPendingTarget
        {
            get => Read(PageField.HasPendingTarget, m_Committed.HasPendingTarget, m_Pending.HasPendingTarget);
            set => Write(PageField.HasPendingTarget, ref m_Committed.HasPendingTarget, ref m_Pending.HasPendingTarget, value);
        }

        CharacterPoseStateTransitionDescriptor m_PendingTargetTransition
        {
            get => Read(PageField.PendingTargetTransition, m_Committed.PendingTargetTransition, m_Pending.PendingTargetTransition);
            set => Write(PageField.PendingTargetTransition, ref m_Committed.PendingTargetTransition, ref m_Pending.PendingTargetTransition, value);
        }

        PresentationFrameFailure m_FrameFailure
        {
            get => Read(PageField.FrameFailure, m_Committed.FrameFailure, m_Pending.FrameFailure);
            set => Write(PageField.FrameFailure, ref m_Committed.FrameFailure, ref m_Pending.FrameFailure, value);
        }

        internal CharacterPoseStateMachineRuntime(CharacterPoseStateMachineDescriptor descriptor)
        {
            m_Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            descriptor.RequireValid();
            m_RoutingPlan = descriptor.LoadRoutingPlan();
            int ruleCapacity = descriptor.Transitions.Count == 0
                ? 1
                : descriptor.Transitions.Max(value => value.Rule.Operations.Count);
            m_RuleValues = new CharacterPoseTransitionRuleValue[ruleCapacity];
            m_TransitionDurations = new float[descriptor.Transitions.Count];
            m_TransitionCompletionDurations = new float[descriptor.Transitions.Count];
            for (int i = 0; i < descriptor.Transitions.Count; i++)
            {
                m_TransitionDurations[i] = descriptor.Transitions[i].DurationSeconds;
                m_TransitionCompletionDurations[i] =
                    descriptor.Transitions[i].CompletionDurationSeconds;
            }
            m_TransitionsBySource = new int[descriptor.States.Count][];
            for (int state = 0; state < m_TransitionsBySource.Length; state++)
            {
                m_TransitionsBySource[state] = descriptor.Transitions
                    .Where(value => value.SourceStateIndex == state)
                    .OrderBy(value => value.Priority)
                    .ThenBy(value => value.TransitionId)
                    .Select(value => value.Index)
                    .ToArray();
            }
        }

        internal int Index => m_Descriptor.Index;
        internal int ActiveStateIndex => m_ActiveStateIndex;
        internal int TargetStateIndex => m_BlendTargetStateIndex;
        internal PoseStateId ActiveStateId =>
            (uint)m_ActiveStateIndex < (uint)m_Descriptor.States.Count
                ? m_Descriptor.States[m_ActiveStateIndex].StateId
                : default;
        internal PoseStateId TargetStateId =>
            (uint)m_BlendTargetStateIndex < (uint)m_Descriptor.States.Count
                ? m_Descriptor.States[m_BlendTargetStateIndex].StateId
                : default;
        internal PoseStateTransitionId ActiveTransitionId =>
            m_ActiveTransition?.TransitionId ?? default;
        internal PoseStateTransitionId EvaluatedTransitionId =>
            m_EvaluatedTransitionId;
        internal bool HasTransitionRuleResult =>
            m_HasTransitionRuleResult;
        internal bool TransitionRuleResult =>
            m_TransitionRuleResult;
        internal float TimeInState => m_TimeInState;
        internal PoseInertializationRequest PendingInertializationRequest => m_PendingInertializationRequest;
        internal bool HasPendingInertializationRequest => m_PendingInertializationRequest.IsValid;
        internal bool CanPublishPose => m_CanPublishPose;
        internal bool HasPendingTarget => m_HasPendingTarget;
        internal PresentationFrameFailure FrameFailure => m_FrameFailure;
        internal bool HasActiveTransition => m_ActiveTransition != null;

        internal string ApplyTuning(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block)
        {
            if (layout == null || block == null)
                return "Pose StateMachine tuning payload is missing.";
            string ownerId = $"pose-state-machine:{m_Descriptor.StateMachineId.Value}";
            for (int i = 0; i < layout.Entries.Count; i++)
            {
                CharacterPoseTuningLayoutEntry entry = layout.Entries[i];
                if (!string.Equals(entry.OwnerId, ownerId, StringComparison.Ordinal) ||
                    entry.Interaction != CharacterPoseTuningInteractionPolicy.TunableDefault)
                    continue;
                CharacterPoseTuningValue value = block.GetValue(entry);
                const string transitionPrefix = "/transition:";
                int transitionStart = entry.FieldId.IndexOf(
                    transitionPrefix,
                    ownerId.Length,
                    StringComparison.Ordinal);
                if (transitionStart < 0)
                    continue;
                transitionStart += transitionPrefix.Length;
                int separator = entry.FieldId.IndexOf(
                    '/',
                    transitionStart);
                if (separator <= transitionStart)
                    return $"Pose StateMachine tuning field '{entry.FieldId}' has no transition identity.";
                string transitionId = entry.FieldId.Substring(
                    transitionStart,
                    separator - transitionStart);
                int transitionIndex = -1;
                for (int transition = 0;
                     transition < m_Descriptor.Transitions.Count;
                     transition++)
                {
                    if (string.Equals(
                            m_Descriptor.Transitions[transition].TransitionId.Value,
                            transitionId,
                            StringComparison.Ordinal))
                    {
                        transitionIndex = transition;
                        break;
                    }
                }
                if (transitionIndex < 0 || entry.ValueKind != CharacterPoseTuningValueKind.Float)
                    return $"Pose StateMachine tuning field '{entry.FieldId}' is not a valid transition field.";
                if (entry.FieldId.EndsWith("/duration", StringComparison.Ordinal))
                {
                    m_TransitionDurations[transitionIndex] = value.FloatValue;
                    float authoredDuration =
                        m_Descriptor.Transitions[transitionIndex].DurationSeconds;
                    float authoredCompletionDuration =
                        m_Descriptor.Transitions[transitionIndex].CompletionDurationSeconds;
                    m_TransitionCompletionDurations[transitionIndex] = authoredDuration > 0f
                        ? authoredCompletionDuration * value.FloatValue / authoredDuration
                        : value.FloatValue;
                }
                else if (entry.FieldId.EndsWith("/completion-duration", StringComparison.Ordinal))
                    m_TransitionCompletionDurations[transitionIndex] = value.FloatValue;
            }
            return string.Empty;
        }

        internal void BeginFrame()
        {
            if (m_FrameOpen)
                throw new InvalidOperationException("Pose StateMachine frame is already open.");
            m_RoutingWorkspace.BeginFrame();
            m_DirtyFields = PageField.None;
            m_FrameOpen = true;
        }

        internal void DiscardFrame()
        {
            if (!m_FrameOpen)
                return;
            m_RoutingWorkspace.DiscardFrame();
            m_DirtyFields = PageField.None;
            m_FrameOpen = false;
        }

        internal void CommitFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Pose StateMachine frame is not open.");
            m_RoutingWorkspace.CommitFrame();
            ApplyPending();
            m_DirtyFields = PageField.None;
            m_FrameOpen = false;
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
            Apply(PageField.ActiveStateIndex, ref m_Committed.ActiveStateIndex, m_Pending.ActiveStateIndex);
            Apply(PageField.BlendSourceStateIndex, ref m_Committed.BlendSourceStateIndex, m_Pending.BlendSourceStateIndex);
            Apply(PageField.BlendTargetStateIndex, ref m_Committed.BlendTargetStateIndex, m_Pending.BlendTargetStateIndex);
            Apply(PageField.TimeInState, ref m_Committed.TimeInState, m_Pending.TimeInState);
            Apply(PageField.BlendElapsed, ref m_Committed.BlendElapsed, m_Pending.BlendElapsed);
            Apply(PageField.BlendDuration, ref m_Committed.BlendDuration, m_Pending.BlendDuration);
            Apply(PageField.FactGeneration, ref m_Committed.FactGeneration, m_Pending.FactGeneration);
            Apply(PageField.SelectionGeneration, ref m_Committed.SelectionGeneration, m_Pending.SelectionGeneration);
            Apply(PageField.ControlGeneration, ref m_Committed.ControlGeneration, m_Pending.ControlGeneration);
            Apply(PageField.ActiveTransition, ref m_Committed.ActiveTransition, m_Pending.ActiveTransition);
            Apply(PageField.EvaluatedTransitionId, ref m_Committed.EvaluatedTransitionId, m_Pending.EvaluatedTransitionId);
            Apply(PageField.PendingInertializationRequest, ref m_Committed.PendingInertializationRequest, m_Pending.PendingInertializationRequest);
            Apply(PageField.CaptureCompletion, ref m_Committed.CaptureCompletion, m_Pending.CaptureCompletion);
            Apply(PageField.ReleaseCompletion, ref m_Committed.ReleaseCompletion, m_Pending.ReleaseCompletion);
            Apply(PageField.Initialized, ref m_Committed.Initialized, m_Pending.Initialized);
            Apply(PageField.HasPendingCapture, ref m_Committed.HasPendingCapture, m_Pending.HasPendingCapture);
            Apply(PageField.HasPendingRelease, ref m_Committed.HasPendingRelease, m_Pending.HasPendingRelease);
            Apply(PageField.HasTransitionRuleResult, ref m_Committed.HasTransitionRuleResult, m_Pending.HasTransitionRuleResult);
            Apply(PageField.TransitionRuleResult, ref m_Committed.TransitionRuleResult, m_Pending.TransitionRuleResult);
            Apply(PageField.CanPublishPose, ref m_Committed.CanPublishPose, m_Pending.CanPublishPose);
            Apply(PageField.HasPendingTarget, ref m_Committed.HasPendingTarget, m_Pending.HasPendingTarget);
            Apply(PageField.PendingTargetTransition, ref m_Committed.PendingTargetTransition, m_Pending.PendingTargetTransition);
            Apply(PageField.FrameFailure, ref m_Committed.FrameFailure, m_Pending.FrameFailure);
        }

        internal PoseStateMachineRuntimeSnapshot CreateSnapshot()
        {
            PoseStateId activeState = (uint)m_ActiveStateIndex < (uint)m_Descriptor.States.Count
                ? m_Descriptor.States[m_ActiveStateIndex].StateId
                : default;
            PoseStateId targetState = (uint)m_BlendTargetStateIndex < (uint)m_Descriptor.States.Count
                ? m_Descriptor.States[m_BlendTargetStateIndex].StateId
                : default;
            float progress = m_ActiveTransition != null &&
                             m_ActiveTransition.BlendLogic == AnimationTransitionBlendLogic.StandardBlend
                ? m_BlendDuration <= 0f
                    ? 1f
                    : Math.Clamp(m_BlendElapsed / m_BlendDuration, 0f, 1f)
                : 0f;
            return new PoseStateMachineRuntimeSnapshot(
                m_Descriptor.StateMachineId,
                m_Descriptor.NodeId,
                activeState,
                targetState,
                m_ActiveTransition?.TransitionId ?? default,
                m_EvaluatedTransitionId,
                m_HasTransitionRuleResult,
                m_TransitionRuleResult,
                m_TimeInState,
                progress,
                m_ActiveTransition?.BlendLogic ?? default,
                m_ActiveTransition?.BlendMode ?? default,
                m_ActiveTransition?.DurationSeconds ?? 0f,
                m_ActiveTransition != null ? m_BlendElapsed : 0f,
                m_ActiveTransition?.CurveIndex ?? -1,
                m_ActiveTransition?.BlendProfileIndex ?? -1,
                m_RoutingWorkspace.Snapshot);
        }

        internal void PrepareFrame(
            float deltaSeconds,
            in CharacterPresentationFactFrame facts,
            ICharacterPoseStateSourceRuntime sources)
        {
            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f || !facts.IsValid)
                throw new ArgumentException("Pose StateMachine frame input is invalid.");
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));
            if (!m_Initialized)
                Initialize(sources);
            if (m_FactGeneration != 0 && facts.BodyDiscontinuityGeneration != m_FactGeneration)
                Reset(sources, TransitionRoutingResetReason.OwnerGenerationChanged);
            m_FactGeneration = facts.BodyDiscontinuityGeneration;
            m_FrameFailure = default;
            m_EvaluatedTransitionId = default;
            m_HasTransitionRuleResult = false;
            m_TransitionRuleResult = false;
            m_CanPublishPose = EvaluateRequiredPose(sources);
            if (!m_CanPublishPose)
                return;
            m_TimeInState += deltaSeconds;
            if (m_ActiveTransition != null &&
                m_ActiveTransition.BlendLogic == AnimationTransitionBlendLogic.StandardBlend)
            {
                m_BlendElapsed = Math.Min(m_BlendElapsed + deltaSeconds, m_BlendDuration);
                if (m_BlendDuration <= 0f || m_BlendElapsed >= m_BlendDuration)
                    CompleteStandardBlend(sources);
            }
            PrepareTransitionDemand(in facts, sources);
        }

        internal void EvaluateTransitions(
            in CharacterPresentationFactFrame facts,
            ICharacterPoseStateSourceRuntime sources)
        {
            if (!m_Initialized)
                throw new InvalidOperationException("Pose StateMachine is not initialized.");
            m_FrameFailure = default;
            m_CanPublishPose = EvaluateRequiredPose(sources);
            if (!m_CanPublishPose)
                return;
            if (m_ActiveTransition != null &&
                m_HasPendingTarget &&
                m_PendingTargetTransition != null &&
                m_PendingTargetTransition.Index != m_ActiveTransition.Index &&
                TryBeginTransition(
                    m_PendingTargetTransition,
                    sources))
            {
                if (m_ActiveTransition != null && !m_HasPendingRelease)
                    UpdateSynchronization(sources);
                return;
            }
            CompleteRoutingHandshake(sources);
            if (m_ActiveTransition != null)
            {
                if (!m_HasPendingRelease)
                    UpdateSynchronization(sources);
                return;
            }
            if (!m_HasPendingTarget ||
                m_PendingTargetTransition == null)
            {
                SetStateRelevant(
                    m_ActiveStateIndex,
                    true,
                    PoseSourceProviderDemandKind.Active,
                    sources);
                return;
            }
            TryBeginTransition(
                m_PendingTargetTransition,
                sources);
        }

        internal CharacterPoseStateMachineNativeControl BuildNativeControl()
        {
            if (!m_CanPublishPose)
            {
                string detail = m_FrameFailure.IsValid
                    ? m_FrameFailure.Detail
                    : "Pose StateMachine required source is pending.";
                throw new InvalidOperationException(detail);
            }
            if (!m_Initialized || (uint)m_ActiveStateIndex >= (uint)m_Descriptor.States.Count)
                throw new InvalidOperationException("Pose StateMachine has no active state.");
            if (m_ActiveTransition != null &&
                m_ActiveTransition.BlendLogic == AnimationTransitionBlendLogic.StandardBlend)
            {
                return new CharacterPoseStateMachineNativeControl(
                    m_Descriptor.States[m_BlendSourceStateIndex].OutputPoseValueIndex,
                    m_Descriptor.States[m_BlendTargetStateIndex].OutputPoseValueIndex,
                    m_BlendSourceStateIndex,
                    m_BlendTargetStateIndex,
                    m_BlendElapsed,
                    m_TransitionDurations[m_ActiveTransition.Index],
                    m_ActiveTransition.CurveIndex,
                    m_ActiveTransition.BlendProfileIndex,
                    CharacterPoseStateMachineBlendMode.Standard,
                    m_ControlGeneration);
            }
            if (m_ActiveTransition != null &&
                m_ActiveTransition.BlendLogic == AnimationTransitionBlendLogic.Inertialization)
            {
                return new CharacterPoseStateMachineNativeControl(
                    m_Descriptor.States[m_BlendSourceStateIndex].OutputPoseValueIndex,
                    m_Descriptor.States[m_BlendTargetStateIndex].OutputPoseValueIndex,
                    m_BlendSourceStateIndex,
                    m_BlendTargetStateIndex,
                    0f,
                    m_TransitionDurations[m_ActiveTransition.Index],
                    m_ActiveTransition.CurveIndex,
                    m_ActiveTransition.BlendProfileIndex,
                    CharacterPoseStateMachineBlendMode.Inertialization,
                    m_ControlGeneration);
            }
            int output = m_Descriptor.States[m_ActiveStateIndex].OutputPoseValueIndex;
            return new CharacterPoseStateMachineNativeControl(
                output,
                output,
                m_ActiveStateIndex,
                m_ActiveStateIndex,
                0f,
                0f,
                -1,
                -1,
                CharacterPoseStateMachineBlendMode.Single,
                m_ControlGeneration);
        }

        internal void NotifyNativeFrameCompleted(
            in PoseInertializationNativeState state,
            ulong completionIdentity)
        {
            if (!m_PendingInertializationRequest.IsValid ||
                state.OutputCompletionIdentity != completionIdentity ||
                state.LastEventIdentity != m_ControlGeneration)
            {
                return;
            }
            bool succeeded = state.RuntimeState != PoseInertializationRuntimeState.Invalid;
            PoseInertializationRequest request = m_PendingInertializationRequest;
            if (m_HasPendingCapture)
            {
                SubmitCaptureCompletion(in request, succeeded);
                return;
            }
            if (m_HasPendingRelease &&
                !m_ReleaseCompletion.IsPresent &&
                IsInertializationTerminal(state.RuntimeState))
                SubmitReleaseCompletion(in request, succeeded);
        }

        static bool IsInertializationTerminal(
            PoseInertializationRuntimeState state) =>
            state == PoseInertializationRuntimeState.Anchor ||
            state == PoseInertializationRuntimeState.HardCut ||
            state == PoseInertializationRuntimeState.Complete ||
            state == PoseInertializationRuntimeState.Reset ||
            state == PoseInertializationRuntimeState.Invalid;

        internal void SubmitCaptureCompletion(
            in PoseInertializationRequest request,
            bool succeeded)
        {
            if (!m_HasPendingCapture ||
                !Matches(m_PendingInertializationRequest, request))
                throw new InvalidOperationException("Pose StateMachine capture completion does not match its pending request.");
            m_CaptureCompletion = new TransitionCompletionFact(
                true,
                request.RequestEventId,
                request.RequestGeneration,
                succeeded);
            m_HasPendingCapture = false;
        }

        internal void SubmitReleaseCompletion(
            in PoseInertializationRequest request,
            bool succeeded)
        {
            if (!m_HasPendingRelease ||
                m_ReleaseCompletion.IsPresent ||
                !Matches(m_PendingInertializationRequest, request))
                throw new InvalidOperationException("Pose StateMachine release completion does not match its pending request.");
            m_ReleaseCompletion = new TransitionCompletionFact(
                true,
                request.RequestEventId,
                request.RequestGeneration,
                succeeded);
        }

        internal void Reset(
            ICharacterPoseStateSourceRuntime sources,
            TransitionRoutingResetReason resetReason)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));
            for (int state = 0; state < m_Descriptor.States.Count; state++)
                SetStateRelevant(
                    state,
                    false,
                    default,
                    sources);
            if (m_ActiveTransition?.SourceSync != null)
                sources.ClearSynchronization(m_ActiveTransition.SourceSync);
            m_ActiveStateIndex = m_Descriptor.EntryStateIndex;
            m_BlendSourceStateIndex = -1;
            m_BlendTargetStateIndex = -1;
            m_TimeInState = 0f;
            m_BlendElapsed = 0f;
            m_BlendDuration = 0f;
            m_ActiveTransition = null;
            m_EvaluatedTransitionId = default;
            m_PendingInertializationRequest = default;
            m_CaptureCompletion = default;
            m_ReleaseCompletion = default;
            m_HasPendingCapture = false;
            m_HasPendingRelease = false;
            m_HasTransitionRuleResult = false;
            m_TransitionRuleResult = false;
            m_CanPublishPose = false;
            m_HasPendingTarget = false;
            m_PendingTargetTransition = null;
            m_FrameFailure = default;
            m_SelectionGeneration =
                AllocateSelectionGeneration();
            m_ControlGeneration =
                AllocateControlGeneration();
            ResetStateOnEntryIfNeeded(m_ActiveStateIndex, sources);
            SetStateRelevant(
                m_ActiveStateIndex,
                true,
                PoseSourceProviderDemandKind.Entry,
                sources);
            TransitionRoutingFrameOutput routingReset = SubmitRouting(
                m_ActiveStateIndex,
                m_ActiveStateIndex,
                true,
                default,
                default,
                resetReason);
            if (routingReset.IsInvalid)
                throw new InvalidOperationException(
                    $"Pose StateMachine reset routing failed: {routingReset.ReasonCode}: {routingReset.Reason}");
            m_Initialized = true;
            m_CanPublishPose = false;
        }

        void Initialize(ICharacterPoseStateSourceRuntime sources)
        {
            m_ActiveStateIndex = m_Descriptor.EntryStateIndex;
            ResetStateOnEntryIfNeeded(m_ActiveStateIndex, sources);
            SetStateRelevant(
                m_ActiveStateIndex,
                true,
                PoseSourceProviderDemandKind.Entry,
                sources);
            m_Initialized = true;
            m_TimeInState = 0f;
            m_ControlGeneration =
                AllocateControlGeneration();
            TransitionRoutingFrameOutput routingInitialization = SubmitRouting(
                m_ActiveStateIndex,
                m_ActiveStateIndex,
                true,
                default,
                default,
                TransitionRoutingResetReason.Explicit);
            if (routingInitialization.IsInvalid)
                throw new InvalidOperationException(
                    $"Pose StateMachine initialization routing failed: {routingInitialization.ReasonCode}: {routingInitialization.Reason}");
        }

        CharacterPoseStateTransitionDescriptor SelectTransition(
            in CharacterPresentationFactFrame facts,
            ICharacterPoseStateSourceRuntime sources)
        {
            float remainingTime = GetStateRemainingTime(m_ActiveStateIndex, sources);
            int[] candidates = m_TransitionsBySource[m_ActiveStateIndex];
            for (int i = 0; i < candidates.Length; i++)
            {
                CharacterPoseStateTransitionDescriptor transition = m_Descriptor.Transitions[candidates[i]];
                bool result = CharacterPoseTransitionRuleRuntime.Evaluate(
                        transition.Rule,
                        in facts,
                        m_TimeInState,
                        remainingTime,
                        m_RuleValues);
                m_EvaluatedTransitionId = transition.TransitionId;
                m_HasTransitionRuleResult = true;
                m_TransitionRuleResult = result;
                if (result)
                {
                    return transition;
                }
            }
            return null;
        }

        void PrepareTransitionDemand(
            in CharacterPresentationFactFrame facts,
            ICharacterPoseStateSourceRuntime sources)
        {
            CharacterPoseStateTransitionDescriptor selected =
                SelectTransition(in facts, sources);
            if (m_ActiveTransition != null &&
                selected != null &&
                selected.Index == m_ActiveTransition.Index)
            {
                ClearPendingTarget(sources);
                return;
            }
            if (selected == null)
            {
                ClearPendingTarget(sources);
                SetStateRelevant(
                    m_ActiveStateIndex,
                    true,
                    PoseSourceProviderDemandKind.Active,
                    sources);
                return;
            }
            bool firstDemand =
                m_PendingTargetTransition == null ||
                m_PendingTargetTransition.Index != selected.Index;
            if (m_PendingTargetTransition != null &&
                m_PendingTargetTransition.Index != selected.Index)
            {
                ClearPendingTarget(sources);
            }
            if (firstDemand)
                ResetStateOnEntryIfNeeded(selected.TargetStateIndex, sources);
            SetStateRelevant(
                m_ActiveStateIndex,
                true,
                PoseSourceProviderDemandKind.TransitionSource,
                sources);
            SetStateRelevant(
                selected.TargetStateIndex,
                true,
                PoseSourceProviderDemandKind.TransitionTarget,
                sources);
            m_HasPendingTarget = true;
            m_PendingTargetTransition = selected;
        }

        bool TryBeginTransition(
            CharacterPoseStateTransitionDescriptor transition,
            ICharacterPoseStateSourceRuntime sources)
        {
            int sourceState = m_ActiveStateIndex;
            int targetState = transition.TargetStateIndex;
            CharacterPoseStateTransitionDescriptor replacedTransition =
                m_ActiveTransition;
            int replacedSourceState = m_BlendSourceStateIndex;
            int replacedTargetState = m_BlendTargetStateIndex;
            if (!m_HasPendingTarget ||
                m_PendingTargetTransition == null ||
                m_PendingTargetTransition.Index != transition.Index)
            {
                throw new InvalidOperationException(
                    $"Pose State transition '{transition.TransitionId}' was not prepared for the current frame.");
            }
            PoseSourceProviderStatus targetStatus = GetStateStatus(targetState, sources);
            if (targetStatus.Availability == PresentationPoseSourceAvailability.Pending)
                return false;
            if (targetStatus.Availability == PresentationPoseSourceAvailability.Invalid)
            {
                SetProviderFailure(
                    targetState,
                    targetStatus,
                    PresentationFrameFailureKind.ProviderInvalid);
                return false;
            }
            m_HasPendingTarget = false;
            m_PendingTargetTransition = null;
            if (transition.SourceSync.Mode == PoseStateSourceSyncMode.MarkerGroup &&
                !sources.TrySynchronize(transition.SourceSync, true, out _))
            {
                throw new InvalidOperationException(
                    $"Pose State transition '{transition.TransitionId}' source sync could not be established.");
            }

            if (replacedTransition != null)
            {
                sources.ClearSynchronization(replacedTransition.SourceSync);
                m_CaptureCompletion = default;
                m_ReleaseCompletion = default;
                m_HasPendingCapture = false;
                m_HasPendingRelease = false;
            }
            m_SelectionGeneration =
                AllocateSelectionGeneration();
            TransitionRoutingFrameOutput route = SubmitRouting(
                sourceState,
                targetState,
                true,
                default,
                default,
                TransitionRoutingResetReason.None);
            if (route.IsInvalid)
                throw new InvalidOperationException($"Pose State transition routing failed: {route.ReasonCode}: {route.Reason}");
            m_ControlGeneration =
                AllocateControlGeneration();
            m_TimeInState = 0f;
            ReleaseReplacedTransitionStates(
                replacedSourceState,
                replacedTargetState,
                sourceState,
                targetState,
                sources);

            if (route.HasStandardBlendCommand)
            {
                m_PendingInertializationRequest = default;
                m_ActiveTransition = transition;
                m_BlendSourceStateIndex = sourceState;
                m_BlendTargetStateIndex = targetState;
                m_ActiveStateIndex = targetState;
                m_BlendDuration = m_TransitionCompletionDurations[transition.Index];
                m_BlendElapsed = 0f;
                if (m_BlendDuration <= 0f)
                    CompleteStandardBlend(sources);
                return true;
            }
            if (!route.HasInertializationRequest)
                throw new InvalidOperationException("Pose State transition routing produced no executable decision.");
            m_ActiveTransition = transition;
            m_BlendSourceStateIndex = sourceState;
            m_BlendTargetStateIndex = targetState;
            m_ActiveStateIndex = targetState;
            m_PendingInertializationRequest = route.InertializationRequest;
            m_HasPendingCapture = route.CapturePermission;
            return true;
        }

        void ReleaseReplacedTransitionStates(
            int replacedSourceState,
            int replacedTargetState,
            int sourceState,
            int targetState,
            ICharacterPoseStateSourceRuntime sources)
        {
            if (replacedSourceState >= 0 &&
                replacedSourceState != sourceState &&
                replacedSourceState != targetState)
            {
                SetStateRelevant(
                    replacedSourceState,
                    false,
                    default,
                    sources);
            }
            if (replacedTargetState >= 0 &&
                replacedTargetState != replacedSourceState &&
                replacedTargetState != sourceState &&
                replacedTargetState != targetState)
            {
                SetStateRelevant(
                    replacedTargetState,
                    false,
                    default,
                    sources);
            }
        }

        void CompleteStandardBlend(ICharacterPoseStateSourceRuntime sources)
        {
            CharacterPoseStateTransitionDescriptor transition = m_ActiveTransition;
            if (transition == null)
                return;
            int sourceState = m_BlendSourceStateIndex;
            int targetState = m_BlendTargetStateIndex;
            m_ActiveStateIndex = targetState;
            SetStateRelevant(
                targetState,
                true,
                PoseSourceProviderDemandKind.Active,
                sources);
            SetStateRelevant(
                sourceState,
                false,
                default,
                sources);
            sources.ClearSynchronization(transition.SourceSync);
            m_ActiveTransition = null;
            m_BlendSourceStateIndex = -1;
            m_BlendTargetStateIndex = -1;
            m_BlendElapsed = 0f;
            m_BlendDuration = 0f;
            m_ControlGeneration =
                AllocateControlGeneration();
        }

        void CompleteRoutingHandshake(ICharacterPoseStateSourceRuntime sources)
        {
            if (m_CaptureCompletion.IsPresent)
            {
                TransitionRoutingFrameOutput capture = SubmitRouting(
                    m_BlendSourceStateIndex,
                    m_BlendTargetStateIndex,
                    true,
                    m_CaptureCompletion,
                    default,
                    TransitionRoutingResetReason.None);
                m_CaptureCompletion = default;
                if (capture.IsInvalid || !capture.ReleasePermission)
                    throw new InvalidOperationException($"Pose State inertialization capture failed: {capture.ReasonCode}: {capture.Reason}");
                SetStateRelevant(
                    m_BlendTargetStateIndex,
                    true,
                    PoseSourceProviderDemandKind.Active,
                    sources);
                SetStateRelevant(
                    m_BlendSourceStateIndex,
                    false,
                    default,
                    sources);
                sources.ClearSynchronization(m_ActiveTransition.SourceSync);
                m_HasPendingRelease = true;
                return;
            }
            if (!m_HasPendingRelease || !m_ReleaseCompletion.IsPresent)
                return;
            TransitionRoutingFrameOutput release = SubmitRouting(
                m_BlendTargetStateIndex,
                m_BlendTargetStateIndex,
                true,
                default,
                m_ReleaseCompletion,
                TransitionRoutingResetReason.None);
            if (release.IsInvalid)
                throw new InvalidOperationException($"Pose State inertialization release failed: {release.ReasonCode}: {release.Reason}");
            m_ReleaseCompletion = default;
            m_HasPendingRelease = false;
            m_PendingInertializationRequest = default;
            m_ActiveTransition = null;
            m_BlendSourceStateIndex = -1;
            m_BlendTargetStateIndex = -1;
            m_ControlGeneration =
                AllocateControlGeneration();
        }

        void UpdateSynchronization(ICharacterPoseStateSourceRuntime sources)
        {
            if (m_ActiveTransition.SourceSync.Mode == PoseStateSourceSyncMode.MarkerGroup &&
                !sources.TrySynchronize(m_ActiveTransition.SourceSync, false, out _))
            {
                throw new InvalidOperationException(
                    $"Pose State transition '{m_ActiveTransition.TransitionId}' source sync update failed.");
            }
        }

        TransitionRoutingFrameOutput SubmitRouting(
            int sourceState,
            int targetState,
            bool targetReady,
            TransitionCompletionFact capture,
            TransitionCompletionFact release,
            TransitionRoutingResetReason resetReason)
        {
            var input = new TransitionRoutingFrameInput(
                m_RoutingPlan.PlanId,
                new TransitionFrameId(++m_RoutingFrame),
                new TransitionRouteOwnerId($"pose-state-machine/{m_Descriptor.StateMachineId}"),
                Endpoint(sourceState),
                Endpoint(targetState),
                new TransitionSelectionGeneration(m_SelectionGeneration),
                targetReady,
                true,
                capture,
                release,
                resetReason);
            return TransitionRoutingRuntime.Step(m_RoutingPlan, m_RoutingWorkspace, in input);
        }

        TransitionEndpointId Endpoint(int stateIndex) =>
            new TransitionEndpointId(
                $"pose-state/{m_Descriptor.StateMachineId}/{m_Descriptor.States[stateIndex].StateId}");

        void SetStateRelevant(
            int stateIndex,
            bool relevant,
            PoseSourceProviderDemandKind demandKind,
            ICharacterPoseStateSourceRuntime sources)
        {
            IReadOnlyList<PoseStateSourceProviderPlan> usages = m_Descriptor.States[stateIndex].SourceProviders;
            for (int i = 0; i < usages.Count; i++)
                sources.SetRelevant(
                    usages[i],
                    relevant,
                    demandKind);
        }

        void ResetStateSources(int stateIndex, ICharacterPoseStateSourceRuntime sources)
        {
            IReadOnlyList<PoseStateSourceProviderPlan> usages = m_Descriptor.States[stateIndex].SourceProviders;
            for (int i = 0; i < usages.Count; i++)
                sources.Reset(usages[i]);
        }

        void ResetStateOnEntryIfNeeded(int stateIndex, ICharacterPoseStateSourceRuntime sources)
        {
            if (m_Descriptor.States[stateIndex].AlwaysResetOnEntry)
                ResetStateSources(stateIndex, sources);
        }

        bool EvaluateRequiredState(
            int stateIndex,
            ICharacterPoseStateSourceRuntime sources)
        {
            PoseSourceProviderStatus status = GetStateStatus(stateIndex, sources);
            if (status.Availability == PresentationPoseSourceAvailability.Ready)
                return true;
            SetProviderFailure(
                stateIndex,
                status,
                status.Availability == PresentationPoseSourceAvailability.Pending
                    ? PresentationFrameFailureKind.RequiredProviderPending
                    : PresentationFrameFailureKind.ProviderInvalid);
            return false;
        }

        bool EvaluateRequiredPose(
            ICharacterPoseStateSourceRuntime sources)
        {
            if (m_ActiveTransition == null ||
                m_ActiveTransition.BlendLogic != AnimationTransitionBlendLogic.StandardBlend)
            {
                return EvaluateRequiredState(
                    m_ActiveStateIndex,
                    sources);
            }
            if (m_BlendSourceStateIndex < 0 || m_BlendTargetStateIndex < 0)
                throw new InvalidOperationException("Active Standard Blend has an invalid State pair.");
            bool sourceReady = EvaluateRequiredState(
                m_BlendSourceStateIndex,
                sources);
            bool targetReady = EvaluateRequiredState(
                m_BlendTargetStateIndex,
                sources);
            return sourceReady && targetReady;
        }

        PoseSourceProviderStatus GetStateStatus(
            int stateIndex,
            ICharacterPoseStateSourceRuntime sources)
        {
            IReadOnlyList<PoseStateSourceProviderPlan> usages = m_Descriptor.States[stateIndex].SourceProviders;
            PoseSourceProviderStatus pending = default;
            for (int i = 0; i < usages.Count; i++)
            {
                PoseSourceProviderStatus status = sources.GetStatus(usages[i]);
                if (!status.IsValid || status.ProviderId != usages[i].ProviderId)
                {
                    throw new InvalidOperationException(
                        $"Pose provider '{usages[i].ProviderId}' returned an invalid status.");
                }
                if (status.Availability == PresentationPoseSourceAvailability.Invalid)
                    return status;
                if (status.Availability == PresentationPoseSourceAvailability.Pending)
                    pending = status;
            }
            if (pending.IsValid)
                return pending;
            if (usages.Count == 0)
                throw new InvalidOperationException(
                    $"Pose State '{m_Descriptor.States[stateIndex].StateId}' has no source provider.");
            return PoseSourceProviderStatus.Ready(usages[0].ProviderId);
        }

        void SetProviderFailure(
            int stateIndex,
            PoseSourceProviderStatus status,
            PresentationFrameFailureKind kind)
        {
            m_CanPublishPose = false;
            string detail = status.Availability == PresentationPoseSourceAvailability.Pending
                ? $"Pose State '{m_Descriptor.States[stateIndex].StateId}' provider '{status.ProviderId}' is pending."
                : $"Pose State '{m_Descriptor.States[stateIndex].StateId}' provider '{status.ProviderId}' is invalid: {status.FailureReason}.";
            m_FrameFailure = new PresentationFrameFailure(
                kind,
                $"pose-state-machine/{m_Descriptor.StateMachineId}/{m_Descriptor.States[stateIndex].StateId}",
                detail);
        }

        void ClearPendingTarget(ICharacterPoseStateSourceRuntime sources)
        {
            if (!m_HasPendingTarget || m_PendingTargetTransition == null)
                return;
            int targetState = m_PendingTargetTransition.TargetStateIndex;
            if (targetState != m_ActiveStateIndex)
                SetStateRelevant(
                    targetState,
                    false,
                    default,
                    sources);
            SetStateRelevant(
                m_ActiveStateIndex,
                true,
                PoseSourceProviderDemandKind.Active,
                sources);
            m_HasPendingTarget = false;
            m_PendingTargetTransition = null;
        }

        float GetStateRemainingTime(int stateIndex, ICharacterPoseStateSourceRuntime sources)
        {
            float remaining = float.PositiveInfinity;
            IReadOnlyList<PoseStateSourceProviderPlan> usages = m_Descriptor.States[stateIndex].SourceProviders;
            for (int i = 0; i < usages.Count; i++)
                remaining = Math.Min(remaining, sources.GetRemainingTime(usages[i]));
            return float.IsPositiveInfinity(remaining) ? float.MaxValue : remaining;
        }

        ulong AllocateSelectionGeneration()
        {
            if (m_NextSelectionGeneration ==
                ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    "Pose StateMachine selection generation was exhausted.");
            }
            return m_NextSelectionGeneration++;
        }

        ulong AllocateControlGeneration()
        {
            if (m_NextControlGeneration ==
                ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    "Pose StateMachine control generation was exhausted.");
            }
            return m_NextControlGeneration++;
        }

        static bool Matches(
            in PoseInertializationRequest expected,
            in PoseInertializationRequest actual) =>
            expected.RequestEventId == actual.RequestEventId &&
            expected.RequestGeneration == actual.RequestGeneration;
    }

    internal readonly struct CharacterPoseTransitionRuleValue
    {
        internal CharacterPoseTransitionRuleValue(
            PoseTransitionRuleValueKind kind,
            bool boolValue,
            float floatValue,
            int enumValue,
            string identityValue)
        {
            Kind = kind;
            BoolValue = boolValue;
            FloatValue = floatValue;
            EnumValue = enumValue;
            IdentityValue = identityValue ?? string.Empty;
        }

        internal PoseTransitionRuleValueKind Kind { get; }
        internal bool BoolValue { get; }
        internal float FloatValue { get; }
        internal int EnumValue { get; }
        internal string IdentityValue { get; }
    }

    internal static class CharacterPoseTransitionRuleRuntime
    {
        internal static bool Evaluate(
            CharacterPoseTransitionRuleProgram program,
            in CharacterPresentationFactFrame facts,
            float timeInState,
            float statePoseRemainingTime,
            CharacterPoseTransitionRuleValue[] values)
        {
            program.RequireValid();
            if (!facts.IsValid || !float.IsFinite(timeInState) || timeInState < 0f ||
                !float.IsFinite(statePoseRemainingTime) || statePoseRemainingTime < 0f ||
                values == null || values.Length < program.Operations.Count)
            {
                throw new ArgumentException("Pose Transition Rule frame input is invalid.");
            }
            for (int i = 0; i < program.Operations.Count; i++)
            {
                CharacterPoseTransitionRuleCompiledOperation operation = program.Operations[i];
                values[i] = operation.Code switch
                {
                    PoseTransitionRuleOperationCode.ReadFact => ReadFact(operation, in facts),
                    PoseTransitionRuleOperationCode.BoolLiteral => Bool(operation.BoolLiteral),
                    PoseTransitionRuleOperationCode.FloatLiteral => Float(operation.FloatLiteral),
                    PoseTransitionRuleOperationCode.EnumLiteral => Enum(operation.EnumLiteral),
                    PoseTransitionRuleOperationCode.IdentityLiteral => Identity(operation.IdentityLiteral),
                    PoseTransitionRuleOperationCode.Not => Bool(!RequireBool(values, operation.InputA)),
                    PoseTransitionRuleOperationCode.And => Bool(
                        RequireBool(values, operation.InputA) && RequireBool(values, operation.InputB)),
                    PoseTransitionRuleOperationCode.Or => Bool(
                        RequireBool(values, operation.InputA) || RequireBool(values, operation.InputB)),
                    PoseTransitionRuleOperationCode.Equal => Bool(Equal(values, operation.InputA, operation.InputB)),
                    PoseTransitionRuleOperationCode.NotEqual => Bool(!Equal(values, operation.InputA, operation.InputB)),
                    PoseTransitionRuleOperationCode.Greater => Bool(
                        RequireFloat(values, operation.InputA) > RequireFloat(values, operation.InputB)),
                    PoseTransitionRuleOperationCode.GreaterOrEqual => Bool(
                        RequireFloat(values, operation.InputA) >= RequireFloat(values, operation.InputB)),
                    PoseTransitionRuleOperationCode.Less => Bool(
                        RequireFloat(values, operation.InputA) < RequireFloat(values, operation.InputB)),
                    PoseTransitionRuleOperationCode.LessOrEqual => Bool(
                        RequireFloat(values, operation.InputA) <= RequireFloat(values, operation.InputB)),
                    PoseTransitionRuleOperationCode.TimeInState => Float(timeInState),
                    PoseTransitionRuleOperationCode.StatePoseRemainingTime => Float(statePoseRemainingTime),
                    _ => throw new InvalidOperationException(
                        $"Pose Transition Rule operation '{operation.Code}' is unsupported.")
                };
            }
            return RequireBool(values, program.OutputOperationIndex);
        }

        static CharacterPoseTransitionRuleValue ReadFact(
            CharacterPoseTransitionRuleCompiledOperation operation,
            in CharacterPresentationFactFrame facts)
        {
            CharacterPresentationFactValue value = facts.Require(operation.FactId);
            return value.Kind switch
            {
                PresentationFactValueKind.Bool => Bool(value.BoolValue),
                PresentationFactValueKind.Float => Float(value.FloatValue),
                PresentationFactValueKind.Enum => Enum(value.EnumValue),
                PresentationFactValueKind.Identity => Identity(value.IdentityValue),
                _ => throw new InvalidOperationException(
                    $"Presentation Fact '{operation.FactId}' cannot be read by a Pose Transition Rule.")
            };
        }

        static bool Equal(CharacterPoseTransitionRuleValue[] values, int a, int b)
        {
            CharacterPoseTransitionRuleValue left = Require(values, a);
            CharacterPoseTransitionRuleValue right = Require(values, b);
            if (left.Kind != right.Kind)
                throw new InvalidOperationException("Pose Transition Rule equality operands have different types.");
            return left.Kind switch
            {
                PoseTransitionRuleValueKind.Bool => left.BoolValue == right.BoolValue,
                PoseTransitionRuleValueKind.Float => left.FloatValue == right.FloatValue,
                PoseTransitionRuleValueKind.Enum => left.EnumValue == right.EnumValue,
                PoseTransitionRuleValueKind.Identity => string.Equals(
                    left.IdentityValue,
                    right.IdentityValue,
                    StringComparison.Ordinal),
                _ => false
            };
        }

        static CharacterPoseTransitionRuleValue Require(CharacterPoseTransitionRuleValue[] values, int index)
        {
            if ((uint)index >= (uint)values.Length)
                throw new InvalidOperationException("Pose Transition Rule input index is invalid.");
            return values[index];
        }

        static bool RequireBool(CharacterPoseTransitionRuleValue[] values, int index)
        {
            CharacterPoseTransitionRuleValue value = Require(values, index);
            if (value.Kind != PoseTransitionRuleValueKind.Bool)
                throw new InvalidOperationException("Pose Transition Rule input is not Bool.");
            return value.BoolValue;
        }

        static float RequireFloat(CharacterPoseTransitionRuleValue[] values, int index)
        {
            CharacterPoseTransitionRuleValue value = Require(values, index);
            if (value.Kind != PoseTransitionRuleValueKind.Float)
                throw new InvalidOperationException("Pose Transition Rule input is not Float.");
            return value.FloatValue;
        }

        static CharacterPoseTransitionRuleValue Bool(bool value) =>
            new CharacterPoseTransitionRuleValue(PoseTransitionRuleValueKind.Bool, value, 0f, 0, string.Empty);

        static CharacterPoseTransitionRuleValue Float(float value) =>
            new CharacterPoseTransitionRuleValue(PoseTransitionRuleValueKind.Float, false, value, 0, string.Empty);

        static CharacterPoseTransitionRuleValue Enum(int value) =>
            new CharacterPoseTransitionRuleValue(PoseTransitionRuleValueKind.Enum, false, 0f, value, string.Empty);

        static CharacterPoseTransitionRuleValue Identity(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Pose Transition Rule identity value is missing.");
            return new CharacterPoseTransitionRuleValue(
                PoseTransitionRuleValueKind.Identity,
                false,
                0f,
                0,
                value);
        }
    }
}
