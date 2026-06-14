using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonAction
{
    public enum FullBodyFramePipelineStep
    {
        None = 0,
        ReadInput = 1,
        UpdateInputBuffer = 2,
        GameplayDecision = 3,
        BuildMotion = 4,
        ExecuteMotion = 5,
        PresentationBridge = 6,
        WriteSnapshotAndEvents = 7,
        Completed = 8,
        Failed = 9
    }

    public readonly struct FullBodyFrameInput
    {
        public FullBodyFrameInput(
            int step,
            BasicLocomotionInputSnapshot locomotionInput,
            bool hasBufferedButtonFacts,
            PredictionButtonFrame dodge,
            PredictionButtonFrame attack,
            PredictionButtonFrame jump,
            PredictionButtonFrame interact)
        {
            Step = step < 0 ? 0 : step;
            LocomotionInput = SanitizeInput(in locomotionInput);
            HasBufferedButtonFacts = hasBufferedButtonFacts;
            Dodge = dodge;
            Attack = attack;
            Jump = jump;
            Interact = interact;
        }

        public int Step { get; }
        public BasicLocomotionInputSnapshot LocomotionInput { get; }
        public float DeltaTime => LocomotionInput.DeltaTime;
        public bool HasBufferedButtonFacts { get; }
        public PredictionButtonFrame Dodge { get; }
        public PredictionButtonFrame Attack { get; }
        public PredictionButtonFrame Jump { get; }
        public PredictionButtonFrame Interact { get; }

        public static FullBodyFrameInput FromLocomotionInput(int step, in BasicLocomotionInputSnapshot input)
        {
            return new FullBodyFrameInput(
                step,
                input,
                false,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None);
        }

        public static FullBodyFrameInput FromPredictionInputFrame(in PredictionInputFrame frame, float deltaTime)
        {
            return new FullBodyFrameInput(
                frame.Tick.Value,
                frame.ToLocomotionInput(SanitizeDelta(deltaTime)),
                true,
                frame.Dodge,
                frame.Attack,
                frame.Jump,
                frame.Interact);
        }

        static BasicLocomotionInputSnapshot SanitizeInput(in BasicLocomotionInputSnapshot input)
        {
            return new BasicLocomotionInputSnapshot(
                SanitizeDelta(input.DeltaTime),
                SanitizeVector(input.Move),
                SanitizeVector(input.Look),
                input.RunHeld);
        }

        static float SanitizeDelta(float deltaTime)
        {
            return float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f ? 0f : deltaTime;
        }

        static Vector2 SanitizeVector(Vector2 value)
        {
            value.x = SanitizeAxis(value.x);
            value.y = SanitizeAxis(value.y);
            return value;
        }

        static float SanitizeAxis(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }
    }

    public struct FullBodyFrameContext
    {
        public FullBodyFrameContext(FullBodyFrameInput input)
        {
            Input = input;
            CurrentStep = FullBodyFramePipelineStep.None;
            LocomotionDecision = default;
            StateDecision = default;
            LocomotionFrame = default;
            StateFrame = default;
            InputRequest = CharacterInputRequestFact.None(InputRequestKind.Dodge);
            ActionDecision = ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoRequest);
            PreviousStateSnapshot = CharacterStateMachineSnapshot.Inactive;
            ExitedToLocomotion = false;
            InputRequestConsumed = false;
            ActionMovementExecuted = false;
            BasicMovementExecuted = false;
            ActionAnimationPresented = false;
            LocomotionAnimationPresented = false;
            AnimationFactsWritten = false;
            SnapshotEventsReady = false;
            Success = false;
            FailureReason = string.Empty;
        }

        public FullBodyFrameInput Input { get; private set; }
        public FullBodyFramePipelineStep CurrentStep { get; private set; }
        public LocomotionDecisionFrame LocomotionDecision { get; private set; }
        public LocomotionStateDecisionFrame StateDecision { get; private set; }
        public BasicLocomotionFrame LocomotionFrame { get; private set; }
        public CharacterStateMachineFrame StateFrame { get; private set; }
        public CharacterInputRequestFact InputRequest { get; private set; }
        public ActionInterruptDecision ActionDecision { get; private set; }
        public CharacterStateMachineSnapshot PreviousStateSnapshot { get; private set; }
        public bool ExitedToLocomotion { get; private set; }
        public bool InputRequestConsumed { get; private set; }
        public bool ActionMovementExecuted { get; private set; }
        public bool BasicMovementExecuted { get; private set; }
        public bool ActionAnimationPresented { get; private set; }
        public bool LocomotionAnimationPresented { get; private set; }
        public bool AnimationFactsWritten { get; private set; }
        public bool SnapshotEventsReady { get; private set; }
        public bool Success { get; private set; }
        public string FailureReason { get; private set; }
        public int Step => Input.Step;

        internal void MarkStep(FullBodyFramePipelineStep step)
        {
            CurrentStep = step;
        }

        internal void SetLocomotionDecision(in LocomotionDecisionFrame decision)
        {
            LocomotionDecision = decision;
        }

        internal void SetInputRequest(in CharacterInputRequestFact request, in ActionInterruptDecision decision)
        {
            InputRequest = request;
            ActionDecision = decision;
        }

        internal void SetStateDecision(in LocomotionStateDecisionFrame decision, in CharacterStateMachineSnapshot previousSnapshot)
        {
            StateDecision = decision;
            StateFrame = decision.StateFrame;
            PreviousStateSnapshot = previousSnapshot;
            ExitedToLocomotion = previousSnapshot.Owner.IsAction && !decision.StateFrame.Owner.IsAction;
        }

        internal void SetLocomotionFrame(in BasicLocomotionFrame frame, in CharacterStateMachineFrame stateFrame)
        {
            LocomotionFrame = frame;
            StateFrame = stateFrame;
        }

        internal void MarkInputRequestConsumed()
        {
            InputRequestConsumed = true;
        }

        internal void MarkMotionExecuted(bool actionMovementExecuted, bool basicMovementExecuted)
        {
            ActionMovementExecuted = actionMovementExecuted;
            BasicMovementExecuted = basicMovementExecuted;
        }

        internal void MarkPresentation(bool actionAnimationPresented, bool locomotionAnimationPresented, bool animationFactsWritten)
        {
            ActionAnimationPresented = actionAnimationPresented;
            LocomotionAnimationPresented = locomotionAnimationPresented;
            AnimationFactsWritten = animationFactsWritten;
        }

        internal void MarkSnapshotEventsReady()
        {
            SnapshotEventsReady = true;
        }

        internal void MarkCompleted()
        {
            CurrentStep = FullBodyFramePipelineStep.Completed;
            Success = true;
            FailureReason = string.Empty;
        }

        internal void MarkFailed(string reason)
        {
            CurrentStep = FullBodyFramePipelineStep.Failed;
            Success = false;
            FailureReason = string.IsNullOrEmpty(reason) ? "unknown" : reason;
        }
    }

    public readonly struct FullBodyFrameResult
    {
        public FullBodyFrameResult(in FullBodyFrameContext context)
        {
            Success = context.Success;
            Step = context.Step;
            CompletedStep = context.CurrentStep;
            LocomotionFrame = context.LocomotionFrame;
            StateFrame = context.StateFrame;
            InputRequest = context.InputRequest;
            InputRequestConsumed = context.InputRequestConsumed;
            ActionMovementExecuted = context.ActionMovementExecuted;
            BasicMovementExecuted = context.BasicMovementExecuted;
            ActionAnimationPresented = context.ActionAnimationPresented;
            LocomotionAnimationPresented = context.LocomotionAnimationPresented;
            AnimationFactsWritten = context.AnimationFactsWritten;
            SnapshotEventsReady = context.SnapshotEventsReady;
            FailureReason = context.FailureReason;
            DiagnosticSummary = BuildDiagnosticSummary(in context);
        }

        public bool Success { get; }
        public int Step { get; }
        public FullBodyFramePipelineStep CompletedStep { get; }
        public BasicLocomotionFrame LocomotionFrame { get; }
        public CharacterStateMachineFrame StateFrame { get; }
        public CharacterInputRequestFact InputRequest { get; }
        public bool InputRequestConsumed { get; }
        public bool ActionMovementExecuted { get; }
        public bool BasicMovementExecuted { get; }
        public bool ActionAnimationPresented { get; }
        public bool LocomotionAnimationPresented { get; }
        public bool AnimationFactsWritten { get; }
        public bool SnapshotEventsReady { get; }
        public string FailureReason { get; }
        public string DiagnosticSummary { get; }

        static string BuildDiagnosticSummary(in FullBodyFrameContext context)
        {
            return
                $"step={context.Step} phase={context.CurrentStep} success={context.Success} " +
                $"request={context.InputRequest.RequestKind} hasRequest={context.InputRequest.HasRequest} consumed={context.InputRequestConsumed} " +
                $"owner={context.StateFrame.Owner.Kind} action={context.StateFrame.ActionState.Value} " +
                $"motionAction={context.ActionMovementExecuted} motionBasic={context.BasicMovementExecuted} " +
                $"presentAction={context.ActionAnimationPresented} presentLocomotion={context.LocomotionAnimationPresented} animationFacts={context.AnimationFactsWritten}";
        }
    }
}
