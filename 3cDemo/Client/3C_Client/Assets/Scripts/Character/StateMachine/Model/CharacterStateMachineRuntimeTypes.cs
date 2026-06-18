using System;
using ThirdPersonAction;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonCharacterStateMachine
{
    public enum StateTimelineFactsSource
    {
        None = 0,
        Current = 1,
        Projected = 2,
        Target = 3
    }

    public readonly struct StateTimelineFactsTrace
    {
        readonly string factsId;

        public StateTimelineFactsTrace(
            StateTimelineFactsSource source,
            StateTimelineWindowFacts facts,
            int sourceStep,
            ActionRequestType requestType = ActionRequestType.None)
        {
            Source = source;
            Facts = facts;
            SourceStep = Mathf.Max(0, sourceStep);
            RequestType = requestType;
            factsId = BuildFactsId(source, facts, SourceStep);
        }

        public StateTimelineFactsSource Source { get; }
        public StateTimelineWindowFacts Facts { get; }
        public int SourceStep { get; }
        public ActionRequestType RequestType { get; }
        public string FactsId => factsId ?? string.Empty;
        public bool HasFacts => Source != StateTimelineFactsSource.None && Facts.StateId.IsValid;

        public static StateTimelineFactsTrace None => default;

        public static StateTimelineFactsTrace Current(
            StateTimelineWindowFacts facts,
            int sourceStep,
            ActionRequestType requestType = ActionRequestType.None)
        {
            return new StateTimelineFactsTrace(StateTimelineFactsSource.Current, facts, sourceStep, requestType);
        }

        public static StateTimelineFactsTrace Projected(
            StateTimelineWindowFacts facts,
            int sourceStep,
            ActionRequestType requestType = ActionRequestType.None)
        {
            return new StateTimelineFactsTrace(StateTimelineFactsSource.Projected, facts, sourceStep, requestType);
        }

        public static StateTimelineFactsTrace Target(
            StateTimelineWindowFacts facts,
            int sourceStep,
            ActionRequestType requestType = ActionRequestType.None)
        {
            return new StateTimelineFactsTrace(StateTimelineFactsSource.Target, facts, sourceStep, requestType);
        }

        static string BuildFactsId(StateTimelineFactsSource source, StateTimelineWindowFacts facts, int sourceStep)
        {
            if (source == StateTimelineFactsSource.None)
                return string.Empty;

            return $"{source}:{sourceStep}:{facts.StateId.Value}:{facts.ElapsedSeconds:F6}:{facts.NormalizedTime:F6}:{facts.ActiveWindowIds}:{facts.RequestWindowIds}:{facts.ActiveFactIds}:{facts.RequestFactIds}";
        }
    }

    public readonly struct CharacterStateMachineContext
    {
        public CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            MovementInputIntent moveIntent,
            Vector3 worldMoveDirection,
            BasicMovementPhaseFacts phaseFacts,
            CharacterInputRequestFact inputRequest)
            : this(
                deltaTime,
                currentStep,
                moveIntent,
                worldMoveDirection,
                Vector3.forward,
                phaseFacts,
                inputRequest,
                CharacterRuntimeBlackboardSnapshot.Empty,
                StateTimelineWindowFacts.None(default))
        {
        }

        public CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            in LocomotionDecisionFacts locomotionFacts,
            CharacterInputRequestFact inputRequest)
            : this(
                deltaTime,
                currentStep,
                in locomotionFacts,
                inputRequest,
                CharacterRuntimeBlackboardSnapshot.Empty,
                StateTimelineWindowFacts.None(default))
        {
        }

        public CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            in LocomotionDecisionFacts locomotionFacts,
            CharacterInputRequestFact inputRequest,
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard)
            : this(
                deltaTime,
                currentStep,
                in locomotionFacts,
                inputRequest,
                runtimeBlackboard,
                StateTimelineWindowFacts.None(default))
        {
        }

        public CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            in LocomotionDecisionFacts locomotionFacts,
            CharacterInputRequestFact inputRequest,
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard,
            StateTimelineWindowFacts timelineFacts)
            : this(
                deltaTime,
                currentStep,
                in locomotionFacts,
                inputRequest,
                runtimeBlackboard,
                StateTimelineFactsTrace.Current(timelineFacts, currentStep),
                StateTimelineFactsTrace.Current(timelineFacts, currentStep),
                StateTimelineFactsTrace.None,
                StateTimelineFactsTrace.None)
        {
        }

        CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            in LocomotionDecisionFacts locomotionFacts,
            CharacterInputRequestFact inputRequest,
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard,
            StateTimelineFactsTrace timelineFactsTrace,
            StateTimelineFactsTrace currentTimelineFactsTrace,
            StateTimelineFactsTrace projectedTimelineFactsTrace,
            StateTimelineFactsTrace targetTimelineFactsTrace)
        {
            DeltaTime = Mathf.Max(0f, deltaTime);
            CurrentStep = Mathf.Max(0, currentStep);
            LocomotionFacts = locomotionFacts;
            MoveIntent = locomotionFacts.MoveIntent;
            WorldMoveDirection = NormalizePlanarOrZero(locomotionFacts.SpatialFacts.WorldMoveDirection);
            FacingForward = NormalizePlanarOrZero(locomotionFacts.SpatialFacts.FacingForward);
            PhaseFacts = locomotionFacts.PhaseFacts;
            InputRequest = inputRequest;
            RuntimeBlackboard = runtimeBlackboard;
            TimelineFactsTrace = timelineFactsTrace;
            CurrentTimelineFactsTrace = currentTimelineFactsTrace;
            ProjectedTimelineFactsTrace = projectedTimelineFactsTrace;
            TargetTimelineFactsTrace = targetTimelineFactsTrace;
        }

        public CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            MovementInputIntent moveIntent,
            Vector3 worldMoveDirection,
            BasicMovementPhaseFacts phaseFacts,
            CharacterInputRequestFact inputRequest,
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard)
            : this(
                deltaTime,
                currentStep,
                moveIntent,
                worldMoveDirection,
                Vector3.forward,
                phaseFacts,
                inputRequest,
                runtimeBlackboard,
                StateTimelineWindowFacts.None(default))
        {
        }

        public CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            MovementInputIntent moveIntent,
            Vector3 worldMoveDirection,
            Vector3 facingForward,
            BasicMovementPhaseFacts phaseFacts,
            CharacterInputRequestFact inputRequest,
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard)
            : this(
                deltaTime,
                currentStep,
                moveIntent,
                worldMoveDirection,
                facingForward,
                phaseFacts,
                inputRequest,
                runtimeBlackboard,
                StateTimelineWindowFacts.None(default))
        {
        }

        public CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            MovementInputIntent moveIntent,
            Vector3 worldMoveDirection,
            Vector3 facingForward,
            BasicMovementPhaseFacts phaseFacts,
            CharacterInputRequestFact inputRequest,
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard,
            StateTimelineWindowFacts timelineFacts)
        {
            DeltaTime = Mathf.Max(0f, deltaTime);
            CurrentStep = Mathf.Max(0, currentStep);
            MoveIntent = moveIntent;
            WorldMoveDirection = NormalizePlanarOrZero(worldMoveDirection);
            FacingForward = NormalizePlanarOrZero(facingForward);
            PhaseFacts = phaseFacts;
            InputRequest = inputRequest;
            RuntimeBlackboard = runtimeBlackboard;
            LocomotionFacts = new LocomotionDecisionFacts(
                MoveIntent,
                MoveIntent.HasMoveIntent ? MoveIntent.Gait : BasicMovementGait.Walk,
                PhaseFacts,
                new LocomotionSpatialFacts(WorldMoveDirection, FacingForward, Vector3.zero, Vector3.zero),
                LocomotionTurnBackIntent.None);
            TimelineFactsTrace = StateTimelineFactsTrace.Current(timelineFacts, CurrentStep);
            CurrentTimelineFactsTrace = TimelineFactsTrace;
            ProjectedTimelineFactsTrace = StateTimelineFactsTrace.None;
            TargetTimelineFactsTrace = StateTimelineFactsTrace.None;
        }

        public float DeltaTime { get; }
        public int CurrentStep { get; }
        public LocomotionDecisionFacts LocomotionFacts { get; }
        public MovementInputIntent MoveIntent { get; }
        public Vector3 WorldMoveDirection { get; }
        public Vector3 FacingForward { get; }
        public BasicMovementPhaseFacts PhaseFacts { get; }
        public CharacterInputRequestFact InputRequest { get; }
        public CharacterRuntimeBlackboardSnapshot RuntimeBlackboard { get; }
        public StateTimelineFactsTrace TimelineFactsTrace { get; }
        public StateTimelineFactsTrace CurrentTimelineFactsTrace { get; }
        public StateTimelineFactsTrace ProjectedTimelineFactsTrace { get; }
        public StateTimelineFactsTrace TargetTimelineFactsTrace { get; }
        public StateTimelineWindowFacts TimelineFacts => TimelineFactsTrace.Facts;
        public StateTimelineWindowFacts CurrentTimelineFacts => CurrentTimelineFactsTrace.Facts;
        public StateTimelineWindowFacts ProjectedTimelineFacts => ProjectedTimelineFactsTrace.Facts;
        public StateTimelineWindowFacts TargetTimelineFacts => TargetTimelineFactsTrace.Facts;
        public bool HasMoveIntent => MoveIntent.HasMoveIntent;
        public bool StateCanExit => PhaseFacts.PhaseCanExit;

        public CharacterStateMachineContext WithTimelineFacts(StateTimelineWindowFacts timelineFacts)
        {
            return WithCurrentTimelineFacts(timelineFacts);
        }

        public CharacterStateMachineContext WithCurrentTimelineFacts(StateTimelineWindowFacts timelineFacts)
        {
            StateTimelineFactsTrace trace = StateTimelineFactsTrace.Current(timelineFacts, CurrentStep);
            return Rebuild(trace, trace, ProjectedTimelineFactsTrace, TargetTimelineFactsTrace);
        }

        public CharacterStateMachineContext WithProjectedTimelineFacts(
            StateTimelineWindowFacts timelineFacts,
            ActionRequestType requestType = ActionRequestType.None)
        {
            StateTimelineFactsTrace trace = StateTimelineFactsTrace.Projected(timelineFacts, CurrentStep, requestType);
            return Rebuild(trace, CurrentTimelineFactsTrace, trace, TargetTimelineFactsTrace);
        }

        public CharacterStateMachineContext WithTargetTimelineFacts(
            StateTimelineWindowFacts timelineFacts,
            ActionRequestType requestType = ActionRequestType.None)
        {
            StateTimelineFactsTrace trace = StateTimelineFactsTrace.Target(timelineFacts, CurrentStep, requestType);
            return Rebuild(trace, CurrentTimelineFactsTrace, ProjectedTimelineFactsTrace, trace);
        }

        CharacterStateMachineContext Rebuild(
            StateTimelineFactsTrace timelineFactsTrace,
            StateTimelineFactsTrace currentTimelineFactsTrace,
            StateTimelineFactsTrace projectedTimelineFactsTrace,
            StateTimelineFactsTrace targetTimelineFactsTrace)
        {
            LocomotionDecisionFacts locomotionFacts = LocomotionFacts;
            return new CharacterStateMachineContext(
                DeltaTime,
                CurrentStep,
                in locomotionFacts,
                InputRequest,
                RuntimeBlackboard,
                timelineFactsTrace,
                currentTimelineFactsTrace,
                projectedTimelineFactsTrace,
                targetTimelineFactsTrace);
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }

    public readonly struct CharacterStateMachineSnapshot
    {
        public CharacterStateMachineSnapshot(
            CharacterStateId activeState,
            float stateTime,
            CharacterStateVariant variant,
            string pendingTransitionPath,
            CharacterStateTag[] tags)
        {
            ActiveState = activeState;
            ActivePath = activeState.Value;
            StateTime = Mathf.Max(0f, stateTime);
            Variant = variant;
            PendingTransitionPath = pendingTransitionPath ?? string.Empty;
            Tags = new IReadOnlyListWrapper<CharacterStateTag>(tags);
        }

        public CharacterStateId ActiveState { get; }
        public string ActivePath { get; }
        public float StateTime { get; }
        public CharacterStateVariant Variant { get; }
        public string PendingTransitionPath { get; }
        public IReadOnlyListWrapper<CharacterStateTag> Tags { get; }
        public bool HasPendingTransition => !string.IsNullOrEmpty(PendingTransitionPath);

        public static CharacterStateMachineSnapshot Inactive => new CharacterStateMachineSnapshot(
            default,
            0f,
            CharacterStateVariant.None,
            string.Empty,
            System.Array.Empty<CharacterStateTag>());
    }

    public readonly struct CharacterStateDomainView
    {
        CharacterStateDomainView(
            CharacterStateMachineSnapshot snapshot,
            CharacterStateOwner owner,
            ActionStateId actionState,
            BasicMovementPhase locomotionPhase,
            bool isAction,
            bool isLocomotion)
        {
            Snapshot = snapshot;
            Owner = owner;
            ActionState = actionState;
            LocomotionPhase = locomotionPhase;
            IsAction = isAction;
            IsLocomotion = isLocomotion;
        }

        public CharacterStateMachineSnapshot Snapshot { get; }
        public CharacterStateOwner Owner { get; }
        public ActionStateId ActionState { get; }
        public BasicMovementPhase LocomotionPhase { get; }
        public bool IsAction { get; }
        public bool IsLocomotion { get; }

        public static CharacterStateDomainView FromSnapshot(in CharacterStateMachineSnapshot snapshot)
        {
            return FromSnapshotAndNode(in snapshot, null);
        }

        public static CharacterStateDomainView FromSnapshotAndMetadata(
            in CharacterStateMachineSnapshot snapshot,
            in CharacterStateNodeMetadata metadata)
        {
            if (!snapshot.ActiveState.IsValid || !metadata.NodeId.IsValid)
            {
                return new CharacterStateDomainView(
                    snapshot,
                    CharacterStateOwner.None,
                    ActionStateIds.None,
                    BasicMovementPhase.Idle,
                    false,
                    false);
            }

            bool isAction =
                metadata.IsActionCapabilityState ||
                (metadata.ActionState.IsValid && metadata.ActionState != ActionStateIds.None);
            bool isLocomotion = metadata.IsLocomotionPlaybackState;
            ActionStateId actionState = isAction ? metadata.ActionState : ActionStateIds.None;
            CharacterStateOwner owner = isAction ? CharacterStateOwner.Action(actionState) : CharacterStateOwner.None;

            return new CharacterStateDomainView(
                snapshot,
                owner,
                actionState,
                metadata.LocomotionPhase,
                isAction,
                isLocomotion);
        }

        public static CharacterStateDomainView FromSnapshotAndNode(
            in CharacterStateMachineSnapshot snapshot,
            CharacterStateNodeDefinition node)
        {
            if (!snapshot.ActiveState.IsValid)
            {
                return new CharacterStateDomainView(
                    snapshot,
                    CharacterStateOwner.None,
                    ActionStateIds.None,
                    BasicMovementPhase.Idle,
                    false,
                    false);
            }

            bool isAction = HasTag(in snapshot, CharacterStateTag.Action) ||
                            IsKnownActionState(snapshot.ActiveState) ||
                            (node != null && node.IsActionCapabilityState);
            bool isLocomotion = HasTag(in snapshot, CharacterStateTag.Locomotion) ||
                                IsKnownLocomotionState(snapshot.ActiveState) ||
                                (node != null && node.IsLocomotionPlaybackState);
            ActionStateId actionState = isAction ? ResolveActionState(snapshot.ActiveState, snapshot.ActivePath) : ActionStateIds.None;
            CharacterStateOwner owner = isAction ? CharacterStateOwner.Action(actionState) : CharacterStateOwner.None;
            BasicMovementPhase locomotionPhase = ResolveLocomotionPhase(snapshot.ActiveState, node);

            return new CharacterStateDomainView(
                snapshot,
                owner,
                actionState,
                locomotionPhase,
                isAction,
                isLocomotion);
        }

        static bool HasTag(in CharacterStateMachineSnapshot snapshot, CharacterStateTag tag)
        {
            for (int i = 0; i < snapshot.Tags.Count; i++)
            {
                if (snapshot.Tags[i] == tag)
                    return true;
            }

            return false;
        }

        static BasicMovementPhase ResolveLocomotionPhase(
            CharacterStateId activeState,
            CharacterStateNodeDefinition node)
        {
            if (node != null &&
                node.TryGetModule(CharacterStateModuleType.LocomotionPhase, out CharacterStateModuleDefinition module))
            {
                return module.LocomotionPhase;
            }

            if (activeState == CharacterStateIds.MoveStart)
                return BasicMovementPhase.MoveStart;
            if (activeState == CharacterStateIds.MoveLoop)
                return BasicMovementPhase.MoveLoop;
            if (activeState == CharacterStateIds.MoveStop)
                return BasicMovementPhase.MoveStop;
            if (activeState == CharacterStateIds.TurnBack)
                return BasicMovementPhase.TurnBack;
            return BasicMovementPhase.Idle;
        }

        static bool IsKnownActionState(CharacterStateId activeState)
        {
            return activeState == CharacterStateIds.Dodge;
        }

        static bool IsKnownLocomotionState(CharacterStateId activeState)
        {
            return activeState == CharacterStateIds.Idle ||
                   activeState == CharacterStateIds.MoveStart ||
                   activeState == CharacterStateIds.MoveLoop ||
                   activeState == CharacterStateIds.MoveStop ||
                   activeState == CharacterStateIds.TurnBack;
        }

        static string LastPathSegment(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            int index = path.LastIndexOf('/');
            return index >= 0 && index < path.Length - 1 ? path.Substring(index + 1) : path;
        }

        static ActionStateId ResolveActionState(CharacterStateId activeState, string activePath)
        {
            string segment = LastPathSegment(activePath);
            if (string.IsNullOrWhiteSpace(segment))
                segment = LastPathSegment(activeState.Value);

            if (segment.StartsWith("Action.", System.StringComparison.Ordinal))
                return new ActionStateId(segment);

            return string.IsNullOrWhiteSpace(segment)
                ? ActionStateIds.None
                : new ActionStateId("Action." + segment);
        }
    }

    public readonly struct CharacterStatePayload
    {
        public CharacterStatePayload(
            Vector3 primaryWorldDirection,
            Vector3 secondaryWorldDirection,
            Vector3 entryBasisForward)
        {
            PrimaryWorldDirection = NormalizePlanarOrZero(primaryWorldDirection);
            SecondaryWorldDirection = NormalizePlanarOrZero(secondaryWorldDirection);
            EntryBasisForward = NormalizePlanarOrZero(entryBasisForward);
        }

        public Vector3 PrimaryWorldDirection { get; }
        public Vector3 SecondaryWorldDirection { get; }
        public Vector3 EntryBasisForward { get; }
        public bool HasPrimaryWorldDirection => PrimaryWorldDirection.sqrMagnitude > 0.000001f;
        public bool HasSecondaryWorldDirection => SecondaryWorldDirection.sqrMagnitude > 0.000001f;
        public bool HasEntryBasisForward => EntryBasisForward.sqrMagnitude > 0.000001f;

        public static CharacterStatePayload Empty => default;

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }

    public readonly struct CharacterStateMachineRestoreState
    {
        public CharacterStateMachineRestoreState(
            CharacterStateMachineSnapshot snapshot,
            Vector3 actionWorldDirection,
            bool animationRequestedForState)
            : this(
                snapshot,
                new CharacterStatePayload(actionWorldDirection, Vector3.zero, Vector3.zero),
                animationRequestedForState)
        {
        }

        public CharacterStateMachineRestoreState(
            CharacterStateMachineSnapshot snapshot,
            CharacterStatePayload statePayload,
            bool animationRequestedForState)
        {
            Snapshot = snapshot;
            StatePayload = statePayload;
            AnimationRequestedForState = animationRequestedForState;
        }

        public CharacterStateMachineRestoreState(
            CharacterStateMachineSnapshot snapshot,
            Vector3 actionWorldDirection,
            Vector3 turnBackWorldDirection,
            bool animationRequestedForState)
            : this(
                snapshot,
                actionWorldDirection,
                turnBackWorldDirection,
                Vector3.zero,
                animationRequestedForState)
        {
        }

        public CharacterStateMachineRestoreState(
            CharacterStateMachineSnapshot snapshot,
            Vector3 actionWorldDirection,
            Vector3 turnBackWorldDirection,
            Vector3 turnBackEntryBasisForward,
            bool animationRequestedForState)
        {
            Snapshot = snapshot;
            StatePayload = new CharacterStatePayload(actionWorldDirection, turnBackWorldDirection, turnBackEntryBasisForward);
            AnimationRequestedForState = animationRequestedForState;
        }

        public CharacterStateMachineSnapshot Snapshot { get; }
        public CharacterStatePayload StatePayload { get; }
        public Vector3 ActionWorldDirection => StatePayload.PrimaryWorldDirection;
        public Vector3 TurnBackWorldDirection => StatePayload.SecondaryWorldDirection;
        public Vector3 TurnBackEntryBasisForward => StatePayload.EntryBasisForward;
        public bool AnimationRequestedForState { get; }
    }

    public readonly struct ActionAnimationPlaybackIntent : IEquatable<ActionAnimationPlaybackIntent>
    {
        public ActionAnimationPlaybackIntent(int value)
        {
            Value = value < 0 ? 0 : value;
        }

        public int Value { get; }
        public bool IsValid => Value > 0;

        public bool Equals(ActionAnimationPlaybackIntent other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ActionAnimationPlaybackIntent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static ActionAnimationPlaybackIntent Invalid => default;

        public static bool operator ==(ActionAnimationPlaybackIntent left, ActionAnimationPlaybackIntent right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActionAnimationPlaybackIntent left, ActionAnimationPlaybackIntent right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct CharacterStateAnimationRequest
    {
        public CharacterStateAnimationRequest(CharacterStateAnimationBinding binding, int sourceStep)
            : this(binding, CharacterStatePlaybackFactSource.Action, sourceStep)
        {
        }

        public CharacterStateAnimationRequest(
            CharacterStateAnimationBinding binding,
            int sourceStep,
            ActionAnimationPlaybackIntent actionPlaybackIntent)
            : this(binding, CharacterStatePlaybackFactSource.Action, sourceStep, actionPlaybackIntent)
        {
        }

        public CharacterStateAnimationRequest(
            CharacterStateAnimationBinding binding,
            CharacterStatePlaybackFactSource playbackFactSource,
            int sourceStep)
            : this(binding, playbackFactSource, sourceStep, ActionAnimationPlaybackIntent.Invalid)
        {
        }

        public CharacterStateAnimationRequest(
            CharacterStateAnimationBinding binding,
            CharacterStatePlaybackFactSource playbackFactSource,
            int sourceStep,
            ActionAnimationPlaybackIntent actionPlaybackIntent)
        {
            Binding = binding;
            PlaybackFactSource = playbackFactSource;
            SourceStep = Mathf.Max(0, sourceStep);
            ActionPlaybackIntent = actionPlaybackIntent;
        }

        public CharacterStateAnimationBinding Binding { get; }
        public CharacterStatePlaybackFactSource PlaybackFactSource { get; }
        public ActionAnimationKey Key => Binding.Key;
        public int SourceStep { get; }
        public ActionAnimationPlaybackIntent ActionPlaybackIntent { get; }
        public bool HasKey => Binding.HasKey;
        public bool HasActionPlaybackIntent => ActionPlaybackIntent.IsValid;
        public string TimelineBindingKey => Binding.TimelineBindingKey;
        public bool IsActionAnimation => PlaybackFactSource == CharacterStatePlaybackFactSource.Action;
        public bool IsLocomotionAnimation => PlaybackFactSource == CharacterStatePlaybackFactSource.Locomotion;
    }

    public readonly struct CharacterStateMachineFrame
    {
        public CharacterStateMachineFrame(
            CharacterStateMachineSnapshot snapshot,
            bool executeBasicMovement,
            bool presentLocomotionAnimation,
            bool consumeInputRequest,
            InputRequestKind consumedRequestKind,
            bool setRunLatch,
            bool resetRunLatch,
            ActionMotionSpec actionMotionSpec,
            CharacterStateAnimationRequest animationRequest,
            bool hasAnimationRequest,
            CharacterStatePayload statePayload,
            TurnBackMotionPolicy turnBackMotionPolicy = default,
            bool hasTurnBackMotionPolicy = false,
            Vector3 turnBackWorldDirection = default,
            Vector3 turnBackEntryBasisForward = default,
            StateTimelineWindowFacts timelineFacts = default,
            CharacterStateTransitionConditionTrace[] conditionTraces = null,
            StateTimelineFactsTrace currentTimelineFactsTrace = default,
            StateTimelineFactsTrace projectedTimelineFactsTrace = default,
            StateTimelineFactsTrace targetTimelineFactsTrace = default)
        {
            Snapshot = snapshot;
            ExecuteBasicMovement = executeBasicMovement;
            PresentLocomotionAnimation = presentLocomotionAnimation;
            ConsumeInputRequest = consumeInputRequest;
            ConsumedRequestKind = consumedRequestKind;
            SetRunLatch = setRunLatch;
            ResetRunLatch = resetRunLatch;
            ActionMotionSpec = actionMotionSpec;
            ActionMovementCommand = default;
            HasActionMovement = false;
            ActionCompleted = false;
            AnimationRequest = animationRequest;
            HasAnimationRequest = hasAnimationRequest;
            StatePayload = statePayload;
            TurnBackMotionPolicy = turnBackMotionPolicy;
            HasTurnBackMotionPolicy = hasTurnBackMotionPolicy && turnBackMotionPolicy.IsEnabled;
            TurnBackWorldDirection = NormalizePlanarOrZero(turnBackWorldDirection);
            TurnBackEntryBasisForward = NormalizePlanarOrZero(turnBackEntryBasisForward);
            TimelineFacts = timelineFacts;
            ConditionTraces = new IReadOnlyListWrapper<CharacterStateTransitionConditionTrace>(
                conditionTraces ?? System.Array.Empty<CharacterStateTransitionConditionTrace>());
            CurrentTimelineFactsTrace = currentTimelineFactsTrace;
            ProjectedTimelineFactsTrace = projectedTimelineFactsTrace;
            TargetTimelineFactsTrace = targetTimelineFactsTrace;
        }

        public CharacterStateMachineSnapshot Snapshot { get; }
        public CharacterStateDomainView StateView
        {
            get
            {
                CharacterStateMachineSnapshot snapshot = Snapshot;
                return CharacterStateDomainView.FromSnapshot(in snapshot);
            }
        }
        public BasicMovementPhase LocomotionPhase => StateView.LocomotionPhase;
        public CharacterStateOwner Owner => StateView.Owner;
        public ActionStateId ActionState => StateView.ActionState;
        public bool ExecuteBasicMovement { get; }
        public bool PresentLocomotionAnimation { get; }
        public bool ConsumeInputRequest { get; }
        public InputRequestKind ConsumedRequestKind { get; }
        public bool SetRunLatch { get; }
        public bool ResetRunLatch { get; }
        public ActionMotionSpec ActionMotionSpec { get; }
        public ActionMovementCommand ActionMovementCommand { get; }
        public bool HasActionMovement { get; }
        public bool ActionCompleted { get; }
        public CharacterStateAnimationRequest AnimationRequest { get; }
        public bool HasAnimationRequest { get; }
        public CharacterStatePayload StatePayload { get; }
        public TurnBackMotionPolicy TurnBackMotionPolicy { get; }
        public bool HasTurnBackMotionPolicy { get; }
        public Vector3 TurnBackWorldDirection { get; }
        public Vector3 TurnBackEntryBasisForward { get; }
        public StateTimelineWindowFacts TimelineFacts { get; }
        public IReadOnlyListWrapper<CharacterStateTransitionConditionTrace> ConditionTraces { get; }
        public StateTimelineFactsTrace CurrentTimelineFactsTrace { get; }
        public StateTimelineFactsTrace ProjectedTimelineFactsTrace { get; }
        public StateTimelineFactsTrace TargetTimelineFactsTrace { get; }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            if (sqrMagnitude <= 0.000001f)
                return Vector3.zero;

            return value / Mathf.Sqrt(sqrMagnitude);
        }
    }
}
