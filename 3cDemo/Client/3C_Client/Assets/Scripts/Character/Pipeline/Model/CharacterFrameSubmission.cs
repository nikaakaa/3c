using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;

namespace ThirdPersonAction
{
    public enum CharacterFrameSubmissionSource
    {
        None = 0,
        CharacterRuntimeGraph = 1
    }

    public enum CharacterFrameRequestProviderId
    {
        None = 0,
        External = 1,
        Dodge = 2,
        TurnBack = 3,
        Attack = 4,
        Jump = 5
    }

    public readonly struct CharacterFrameRequestSubmission
    {
        public CharacterFrameRequestSubmission(
            CharacterFrameRequestProviderId providerId,
            CharacterInputRequestFact requestFact,
            ActionInterruptRequest interruptRequest,
            ActionInterruptContext interruptContext,
            int sourceOrder)
        {
            ProviderId = providerId;
            RequestFact = requestFact;
            InterruptRequest = interruptRequest;
            InterruptContext = interruptContext;
            SourceOrder = sourceOrder < 0 ? 0 : sourceOrder;
        }

        public CharacterFrameRequestProviderId ProviderId { get; }
        public CharacterInputRequestFact RequestFact { get; }
        public ActionInterruptRequest InterruptRequest { get; }
        public ActionInterruptContext InterruptContext { get; }
        public int SourceOrder { get; }
        public bool HasRequest => ProviderId != CharacterFrameRequestProviderId.None && RequestFact.HasRequest;
    }

    public readonly struct CharacterFrameRequestSubmissionSet
    {
        public CharacterFrameRequestSubmissionSet(
            CharacterFrameRequestSubmission first,
            CharacterFrameRequestSubmission second,
            int count)
            : this(first, second, default, default, count)
        {
        }

        public CharacterFrameRequestSubmissionSet(
            CharacterFrameRequestSubmission first,
            CharacterFrameRequestSubmission second,
            CharacterFrameRequestSubmission third,
            CharacterFrameRequestSubmission fourth,
            int count)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
            Count = count < 0 ? 0 : count > 4 ? 4 : count;
        }

        public CharacterFrameRequestSubmission First { get; }
        public CharacterFrameRequestSubmission Second { get; }
        public CharacterFrameRequestSubmission Third { get; }
        public CharacterFrameRequestSubmission Fourth { get; }
        public int Count { get; }
        public bool HasAny => Count > 0;

        public static CharacterFrameRequestSubmissionSet Empty => default;

        public CharacterFrameRequestSubmission GetAt(int index)
        {
            return index == 0 ? First : index == 1 ? Second : index == 2 ? Third : index == 3 ? Fourth : default;
        }
    }

    public readonly struct CharacterFrameExternalRequestSubmission
    {
        public CharacterFrameExternalRequestSubmission(CharacterFrameRequestSubmission submission)
        {
            Submission = submission;
        }

        public CharacterFrameRequestSubmission Submission { get; }
        public bool HasSubmission => Submission.HasRequest;

        public static CharacterFrameExternalRequestSubmission None => default;
    }

    public readonly struct CharacterFrameMovementSubmission
    {
        public CharacterFrameMovementSubmission(
            BasicLocomotionFrame locomotionFrame,
            ActionMotionResolveResult actionMotionResult,
            bool executeBasicMovement,
            bool executeActionMovement)
        {
            LocomotionFrame = locomotionFrame;
            ActionMotionResult = actionMotionResult;
            ExecuteBasicMovement = executeBasicMovement;
            ExecuteActionMovement = executeActionMovement;
        }

        public BasicLocomotionFrame LocomotionFrame { get; }
        public ActionMotionResolveResult ActionMotionResult { get; }
        public bool ExecuteBasicMovement { get; }
        public bool ExecuteActionMovement { get; }
        public bool HasMovement => ExecuteBasicMovement || ExecuteActionMovement;
    }

    public readonly struct CharacterFrameAnimationSubmission
    {
        public CharacterFrameAnimationSubmission(
            CharacterStateAnimationRequest animationRequest,
            bool hasAnimationRequest,
            bool presentLocomotionAnimation,
            bool exitedToLocomotion)
        {
            AnimationRequest = animationRequest;
            HasAnimationRequest = hasAnimationRequest;
            PresentLocomotionAnimation = presentLocomotionAnimation;
            ExitedToLocomotion = exitedToLocomotion;
        }

        public CharacterStateAnimationRequest AnimationRequest { get; }
        public bool HasAnimationRequest { get; }
        public bool PresentLocomotionAnimation { get; }
        public bool ExitedToLocomotion { get; }
        public bool HasAnimation => HasAnimationRequest || PresentLocomotionAnimation;
    }

    public readonly struct CharacterFrameInputConsumeSubmission
    {
        public CharacterFrameInputConsumeSubmission(
            CharacterInputRequestFact acceptedRequest,
            bool consumeInputRequest,
            InputRequestKind consumedRequestKind,
            int step)
        {
            AcceptedRequest = acceptedRequest;
            ConsumeInputRequest = consumeInputRequest;
            ConsumedRequestKind = consumedRequestKind;
            Step = step < 0 ? 0 : step;
        }

        public CharacterInputRequestFact AcceptedRequest { get; }
        public bool ConsumeInputRequest { get; }
        public InputRequestKind ConsumedRequestKind { get; }
        public int Step { get; }
        public bool HasInputConsume => ConsumeInputRequest;
    }

    public readonly struct CharacterFrameActionOutputSubmission
    {
        public CharacterFrameActionOutputSubmission(
            CharacterStateAnimationRequest animationRequest,
            bool hasAnimationRequest,
            bool consumeInputRequest,
            InputRequestKind consumedRequestKind,
            bool exitedToLocomotion,
            int step)
            : this(
                animationRequest,
                hasAnimationRequest,
                consumeInputRequest,
                consumedRequestKind,
                exitedToLocomotion,
                step,
                ActionBranchOutcome.None(step))
        {
        }

        public CharacterFrameActionOutputSubmission(
            CharacterStateAnimationRequest animationRequest,
            bool hasAnimationRequest,
            bool consumeInputRequest,
            InputRequestKind consumedRequestKind,
            bool exitedToLocomotion,
            int step,
            ActionBranchOutcome actionBranchOutcome)
        {
            int sanitizedStep = step < 0 ? 0 : step;
            AnimationRequest = animationRequest;
            HasAnimationRequest = hasAnimationRequest;
            ConsumeInputRequest = consumeInputRequest;
            ConsumedRequestKind = consumedRequestKind;
            ExitedToLocomotion = exitedToLocomotion;
            Step = sanitizedStep;
            ActionBranchOutcome = actionBranchOutcome.HasOutcome
                ? actionBranchOutcome
                : ActionBranchOutcome.None(sanitizedStep);
        }

        public CharacterStateAnimationRequest AnimationRequest { get; }
        public bool HasAnimationRequest { get; }
        public bool ConsumeInputRequest { get; }
        public InputRequestKind ConsumedRequestKind { get; }
        public bool ExitedToLocomotion { get; }
        public int Step { get; }
        public ActionBranchOutcome ActionBranchOutcome { get; }
        public ActionTimelineOutcome ActionTimelineOutcome => ActionBranchOutcome.TimelineOutcome;
        public bool HasActionBranchOutcome => ActionBranchOutcome.HasOutcome;

        public static CharacterFrameActionOutputSubmission None(int step)
        {
            return new CharacterFrameActionOutputSubmission(
                default,
                false,
                false,
                default,
                false,
                step);
        }
    }

    public readonly struct CharacterFrameRuntimeFactsSubmission
    {
        public CharacterFrameRuntimeFactsSubmission(
            CharacterStateMachineFrame stateFrame,
            ActionMotionResolveResult actionMotionResult,
            bool exitedToLocomotion,
            int step)
            : this(
                stateFrame,
                actionMotionResult,
                exitedToLocomotion,
                LocomotionPreemptionFact.None,
                step)
        {
        }

        public CharacterFrameRuntimeFactsSubmission(
            CharacterStateMachineFrame stateFrame,
            ActionMotionResolveResult actionMotionResult,
            bool exitedToLocomotion,
            LocomotionPreemptionFact locomotionPreemption,
            int step)
        {
            StateFrame = stateFrame;
            ActionMotionResult = actionMotionResult;
            ExitedToLocomotion = exitedToLocomotion;
            LocomotionPreemption = locomotionPreemption;
            Step = step < 0 ? 0 : step;
        }

        public CharacterStateMachineFrame StateFrame { get; }
        public ActionMotionResolveResult ActionMotionResult { get; }
        public bool ExitedToLocomotion { get; }
        public LocomotionPreemptionFact LocomotionPreemption { get; }
        public int Step { get; }
        public bool WriteActionFacts => StateFrame.Snapshot.ActiveState.IsValid || ActionMotionResult.HasSpec;
        public bool WriteAnimationFacts => StateFrame.Snapshot.ActiveState.IsValid;
        public bool WriteLocomotionPreemption => LocomotionPreemption.HasPreemption;
        public bool UpdateStateSnapshot => StateFrame.Snapshot.ActiveState.IsValid;
        public bool CompleteLocomotionTick => StateFrame.Snapshot.ActiveState.IsValid;
        public bool HasRuntimeFacts => WriteActionFacts || WriteAnimationFacts || WriteLocomotionPreemption || UpdateStateSnapshot || CompleteLocomotionTick;

        public CharacterFrameRuntimeFactsSubmission WithLocomotionPreemption(in LocomotionPreemptionFact fact)
        {
            return new CharacterFrameRuntimeFactsSubmission(
                StateFrame,
                ActionMotionResult,
                ExitedToLocomotion,
                fact,
                Step);
        }
    }

    public readonly struct CharacterFrameDiagnosticsSubmission
    {
        public CharacterFrameDiagnosticsSubmission(
            StateTimelineFactsTrace currentTimelineFactsTrace,
            StateTimelineFactsTrace projectedTimelineFactsTrace,
            StateTimelineFactsTrace targetTimelineFactsTrace,
            IReadOnlyListWrapper<CharacterStateTransitionConditionTrace> conditionTraces,
            string actionMotionDiagnosticSummary)
        {
            CurrentTimelineFactsTrace = currentTimelineFactsTrace;
            ProjectedTimelineFactsTrace = projectedTimelineFactsTrace;
            TargetTimelineFactsTrace = targetTimelineFactsTrace;
            ConditionTraces = conditionTraces;
            ActionMotionDiagnosticSummary = actionMotionDiagnosticSummary ?? string.Empty;
        }

        public StateTimelineFactsTrace CurrentTimelineFactsTrace { get; }
        public StateTimelineFactsTrace ProjectedTimelineFactsTrace { get; }
        public StateTimelineFactsTrace TargetTimelineFactsTrace { get; }
        public IReadOnlyListWrapper<CharacterStateTransitionConditionTrace> ConditionTraces { get; }
        public string ActionMotionDiagnosticSummary { get; }
        public bool HasDiagnostics =>
            CurrentTimelineFactsTrace.HasFacts ||
            ProjectedTimelineFactsTrace.HasFacts ||
            TargetTimelineFactsTrace.HasFacts ||
            !string.IsNullOrEmpty(ActionMotionDiagnosticSummary);
    }

    public readonly struct CharacterFrameSnapshotEventsSubmission
    {
        public CharacterFrameSnapshotEventsSubmission(int step, bool commitSnapshotEvents)
        {
            Step = step < 0 ? 0 : step;
            CommitSnapshotEvents = commitSnapshotEvents;
        }

        public int Step { get; }
        public bool CommitSnapshotEvents { get; }
        public bool HasSnapshotEvents => CommitSnapshotEvents;

        public static CharacterFrameSnapshotEventsSubmission None(int step)
        {
            return new CharacterFrameSnapshotEventsSubmission(step, false);
        }
    }

    public readonly struct CharacterFrameSubmission
    {
        public CharacterFrameSubmission(
            CharacterFrameSubmissionSource source,
            int step,
            LocomotionDecisionFrame locomotionDecision,
            LocomotionStateDecisionFrame stateDecision,
            BasicLocomotionFrame locomotionFrame,
            CharacterStateMachineFrame stateFrame,
            ActionMotionResolveResult actionMotionResult,
            CharacterInputRequestFact inputRequest,
            ActionInterruptDecision actionDecision,
            StateTimelineFactsTrace currentTimelineFactsTrace,
            CharacterStateMachineSnapshot previousStateSnapshot,
            bool exitedToLocomotion)
            : this(
                source,
                step,
                locomotionDecision,
                stateDecision,
                locomotionFrame,
                stateFrame,
                actionMotionResult,
                inputRequest,
                actionDecision,
                currentTimelineFactsTrace,
                previousStateSnapshot,
                exitedToLocomotion,
                CharacterFrameActionOutputSubmission.None(step),
                CharacterFrameArbitrationInput.None(step))
        {
        }

        public CharacterFrameSubmission(
            CharacterFrameSubmissionSource source,
            int step,
            LocomotionDecisionFrame locomotionDecision,
            LocomotionStateDecisionFrame stateDecision,
            BasicLocomotionFrame locomotionFrame,
            CharacterStateMachineFrame stateFrame,
            ActionMotionResolveResult actionMotionResult,
            CharacterInputRequestFact inputRequest,
            ActionInterruptDecision actionDecision,
            StateTimelineFactsTrace currentTimelineFactsTrace,
            CharacterStateMachineSnapshot previousStateSnapshot,
            bool exitedToLocomotion,
            CharacterFrameArbitrationInput arbitrationInput)
            : this(
                source,
                step,
                locomotionDecision,
                stateDecision,
                locomotionFrame,
                stateFrame,
                actionMotionResult,
                inputRequest,
                actionDecision,
                currentTimelineFactsTrace,
                previousStateSnapshot,
                exitedToLocomotion,
                CharacterFrameActionOutputSubmission.None(step),
                arbitrationInput)
        {
        }

        public CharacterFrameSubmission(
            CharacterFrameSubmissionSource source,
            int step,
            LocomotionDecisionFrame locomotionDecision,
            LocomotionStateDecisionFrame stateDecision,
            BasicLocomotionFrame locomotionFrame,
            CharacterStateMachineFrame stateFrame,
            ActionMotionResolveResult actionMotionResult,
            CharacterInputRequestFact inputRequest,
            ActionInterruptDecision actionDecision,
            StateTimelineFactsTrace currentTimelineFactsTrace,
            CharacterStateMachineSnapshot previousStateSnapshot,
            bool exitedToLocomotion,
            CharacterFrameActionOutputSubmission actionOutput,
            CharacterFrameArbitrationInput arbitrationInput)
            : this(
                source,
                step,
                locomotionDecision,
                stateDecision,
                locomotionFrame,
                stateFrame,
                actionMotionResult,
                inputRequest,
                actionDecision,
                currentTimelineFactsTrace,
                previousStateSnapshot,
                exitedToLocomotion,
                actionOutput,
                arbitrationInput,
                LocomotionPreemptionFact.None)
        {
        }

        public CharacterFrameSubmission(
            CharacterFrameSubmissionSource source,
            int step,
            LocomotionDecisionFrame locomotionDecision,
            LocomotionStateDecisionFrame stateDecision,
            BasicLocomotionFrame locomotionFrame,
            CharacterStateMachineFrame stateFrame,
            ActionMotionResolveResult actionMotionResult,
            CharacterInputRequestFact inputRequest,
            ActionInterruptDecision actionDecision,
            StateTimelineFactsTrace currentTimelineFactsTrace,
            CharacterStateMachineSnapshot previousStateSnapshot,
            bool exitedToLocomotion,
            CharacterFrameActionOutputSubmission actionOutput,
            CharacterFrameArbitrationInput arbitrationInput,
            LocomotionPreemptionFact locomotionPreemption)
        {
            Source = source;
            Step = step < 0 ? 0 : step;
            LocomotionDecision = locomotionDecision;
            StateDecision = stateDecision;
            LocomotionFrame = locomotionFrame;
            StateFrame = stateFrame;
            ActionMotionResult = actionMotionResult;
            InputRequest = inputRequest;
            ActionDecision = actionDecision;
            CurrentTimelineFactsTrace = currentTimelineFactsTrace;
            PreviousStateSnapshot = previousStateSnapshot;
            ExitedToLocomotion = exitedToLocomotion;
            ActionOutput = actionOutput;
            ArbitrationInput = arbitrationInput;
            LocomotionPreemption = locomotionPreemption;
        }

        public CharacterFrameSubmissionSource Source { get; }
        public int Step { get; }
        public LocomotionDecisionFrame LocomotionDecision { get; }
        public LocomotionStateDecisionFrame StateDecision { get; }
        public BasicLocomotionFrame LocomotionFrame { get; }
        public CharacterStateMachineFrame StateFrame { get; }
        public ActionMotionResolveResult ActionMotionResult { get; }
        public CharacterInputRequestFact InputRequest { get; }
        public ActionInterruptDecision ActionDecision { get; }
        public StateTimelineFactsTrace CurrentTimelineFactsTrace { get; }
        public StateTimelineWindowFacts CurrentTimelineFacts => CurrentTimelineFactsTrace.Facts;
        public CharacterStateMachineSnapshot PreviousStateSnapshot { get; }
        public bool ExitedToLocomotion { get; }
        public CharacterFrameActionOutputSubmission ActionOutput { get; }
        public CharacterFrameArbitrationInput ArbitrationInput { get; }
        public LocomotionPreemptionFact LocomotionPreemption { get; }
        public CharacterFrameMovementSubmission Movement => new CharacterFrameMovementSubmission(
            LocomotionFrame,
            ActionMotionResult,
            StateFrame.ExecuteBasicMovement,
            ActionMotionResult.HasActionMovement);
        public CharacterFrameAnimationSubmission Animation => new CharacterFrameAnimationSubmission(
            ActionOutput.HasAnimationRequest ? ActionOutput.AnimationRequest : StateFrame.AnimationRequest,
            ActionOutput.HasAnimationRequest || StateFrame.HasAnimationRequest,
            StateFrame.PresentLocomotionAnimation,
            ExitedToLocomotion || ActionOutput.ExitedToLocomotion);
        public CharacterFrameInputConsumeSubmission InputConsume => new CharacterFrameInputConsumeSubmission(
            InputRequest,
            ActionOutput.ConsumeInputRequest || StateFrame.ConsumeInputRequest,
            ActionOutput.ConsumeInputRequest ? ActionOutput.ConsumedRequestKind : StateFrame.ConsumedRequestKind,
            Step);
        public CharacterFrameRuntimeFactsSubmission RuntimeFacts => new CharacterFrameRuntimeFactsSubmission(
            StateFrame,
            ActionMotionResult,
            ExitedToLocomotion,
            Step);
        public CharacterFrameDiagnosticsSubmission Diagnostics => new CharacterFrameDiagnosticsSubmission(
            CurrentTimelineFactsTrace,
            StateFrame.ProjectedTimelineFactsTrace,
            StateFrame.TargetTimelineFactsTrace,
            StateFrame.ConditionTraces,
            ActionMotionResult.DiagnosticSummary);
        public CharacterFrameSnapshotEventsSubmission SnapshotEvents => new CharacterFrameSnapshotEventsSubmission(
            Step,
            HasFrameOutput);
        public bool HasStateFrame => StateDecision.HasStateFrame;
        public bool HasFrameOutput => Source != CharacterFrameSubmissionSource.None && HasStateFrame;

        public static CharacterFrameSubmission None(int step)
        {
            return new CharacterFrameSubmission(
                CharacterFrameSubmissionSource.None,
                step,
                default,
                default,
                default,
                default,
                ActionMotionResolveResult.None(step),
                CharacterInputRequestFact.None(InputRequestKind.Dodge),
                ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoRequest),
                StateTimelineFactsTrace.None,
                CharacterStateMachineSnapshot.Inactive,
                false);
        }
    }
}
