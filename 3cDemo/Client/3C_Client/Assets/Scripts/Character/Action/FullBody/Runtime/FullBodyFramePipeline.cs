using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;

namespace ThirdPersonAction
{
    public sealed class FullBodyFramePipeline
    {
        public bool Tick(
            PlayerFullBodyActionController controller,
            in FullBodyFrameInput input,
            out FullBodyFrameResult result)
        {
            FullBodyFrameContext context = BeginFrame(in input);
            RunPhase(controller, SimulationTickPhase.ReadInput, ref context, out _);
            RunPhase(controller, SimulationTickPhase.UpdateInputBuffer, ref context, out _);
            RunPhase(controller, SimulationTickPhase.GameplayDecision, ref context, out _);
            RunPhase(controller, SimulationTickPhase.BuildMotion, ref context, out _);
            RunPhase(controller, SimulationTickPhase.ExecuteMotion, ref context, out _);
            RunPhase(controller, SimulationTickPhase.PresentationBridge, ref context, out _);
            RunPhase(controller, SimulationTickPhase.WriteSnapshotAndEvents, ref context, out result);
            return result.Success;
        }

        public FullBodyFrameContext BeginFrame(in FullBodyFrameInput input)
        {
            return new FullBodyFrameContext(input);
        }

        public bool RunPhase(
            PlayerFullBodyActionController controller,
            SimulationTickPhase phase,
            ref FullBodyFrameContext context,
            out FullBodyFrameResult result)
        {
            if (context.CurrentStep == FullBodyFramePipelineStep.Failed)
            {
                result = new FullBodyFrameResult(in context);
                return false;
            }

            switch (phase)
            {
                case SimulationTickPhase.ReadInput:
                    context.MarkStep(FullBodyFramePipelineStep.ReadInput);
                    break;
                case SimulationTickPhase.UpdateInputBuffer:
                    RunUpdateInputBuffer(controller, ref context);
                    break;
                case SimulationTickPhase.GameplayDecision:
                    RunGameplayDecision(controller, ref context);
                    break;
                case SimulationTickPhase.BuildMotion:
                    RunBuildMotion(controller, ref context);
                    break;
                case SimulationTickPhase.ExecuteMotion:
                    RunExecuteMotion(controller, ref context);
                    break;
                case SimulationTickPhase.PresentationBridge:
                    RunPresentationBridge(controller, ref context);
                    break;
                case SimulationTickPhase.WriteSnapshotAndEvents:
                    RunWriteSnapshotAndEvents(controller, ref context);
                    break;
            }

            result = new FullBodyFrameResult(in context);
            return result.Success || context.CurrentStep != FullBodyFramePipelineStep.Failed;
        }

        void RunUpdateInputBuffer(PlayerFullBodyActionController controller, ref FullBodyFrameContext context)
        {
            context.MarkStep(FullBodyFramePipelineStep.UpdateInputBuffer);
            if (controller == null)
            {
                context.MarkFailed("controller-missing");
                return;
            }

            FullBodyFrameInput input = context.Input;
            if (input.HasBufferedButtonFacts)
                WriteBufferedInputFacts(controller.InputBufferComponent, in input);
        }

        void RunGameplayDecision(PlayerFullBodyActionController controller, ref FullBodyFrameContext context)
        {
            context.MarkStep(FullBodyFramePipelineStep.GameplayDecision);
            if (controller == null || !controller.PrepareFramePipelineAdapters())
            {
                context.MarkFailed("controller-not-ready");
                return;
            }

            PlayerLocomotionController locomotion = controller.LocomotionController;
            CharacterStateMachineRunner stateMachine = controller.StateMachine;
            if (locomotion == null || stateMachine == null)
            {
                context.MarkFailed("state-machine-or-locomotion-missing");
                return;
            }

            FullBodyFrameInput input = context.Input;
            BasicLocomotionInputSnapshot locomotionInput = input.LocomotionInput;
            if (!locomotion.TryPrepareDecisionFrame(
                    in locomotionInput,
                    stateMachine,
                    context.Step,
                    out LocomotionDecisionFrame locomotionDecision))
            {
                context.MarkFailed("locomotion-facts-not-ready");
                return;
            }

            context.SetLocomotionDecision(in locomotionDecision);
            FullBodyActionRequestGateResult gateResult = ResolveActionRequest(controller, in context, in locomotionDecision);
            context.SetInputRequest(gateResult.Request, gateResult.Decision);
            CharacterInputRequestFact inputRequest = gateResult.Request;
            CharacterStateMachineSnapshot previousSnapshot = controller.CurrentStateSnapshot;
            if (!locomotion.TryEvaluatePreparedGameplayDecision(
                    in locomotionDecision,
                    stateMachine,
                    in inputRequest,
                    context.Step,
                    out LocomotionStateDecisionFrame stateDecision))
            {
                context.MarkFailed("state-decision-failed");
                return;
            }

            context.SetStateDecision(in stateDecision, in previousSnapshot);
        }

        void RunBuildMotion(PlayerFullBodyActionController controller, ref FullBodyFrameContext context)
        {
            context.MarkStep(FullBodyFramePipelineStep.BuildMotion);
            if (controller == null || controller.LocomotionController == null || !context.StateDecision.HasStateFrame)
            {
                context.MarkFailed("motion-build-prerequisite-missing");
                return;
            }

            LocomotionStateDecisionFrame stateDecision = context.StateDecision;
            if (!controller.LocomotionController.TryBuildMotionFromStateDecision(
                    in stateDecision,
                    context.Step,
                    out BasicLocomotionFrame locomotionFrame,
                    out CharacterStateMachineFrame stateFrame))
            {
                context.MarkFailed("motion-build-failed");
                return;
            }

            context.SetLocomotionFrame(in locomotionFrame, in stateFrame);
            controller.SetLastFrameOutputsForPipeline(in locomotionFrame, in stateFrame);
        }

        void RunExecuteMotion(PlayerFullBodyActionController controller, ref FullBodyFrameContext context)
        {
            context.MarkStep(FullBodyFramePipelineStep.ExecuteMotion);
            if (controller == null)
            {
                context.MarkFailed("controller-missing");
                return;
            }

            CharacterStateMachineFrame stateFrame = context.StateFrame;
            if (controller.ConsumeStateFrameInputRequestForPipeline(in stateFrame, context.Step))
                context.MarkInputRequestConsumed();

            BasicLocomotionFrame locomotionFrame = context.LocomotionFrame;
            controller.ExecuteStateFrameMotionForPipeline(
                in stateFrame,
                in locomotionFrame,
                out bool actionMovementExecuted,
                out bool basicMovementExecuted);
            context.MarkMotionExecuted(actionMovementExecuted, basicMovementExecuted);
        }

        void RunPresentationBridge(PlayerFullBodyActionController controller, ref FullBodyFrameContext context)
        {
            context.MarkStep(FullBodyFramePipelineStep.PresentationBridge);
            if (controller == null)
            {
                context.MarkFailed("controller-missing");
                return;
            }

            CharacterStateMachineFrame stateFrame = context.StateFrame;
            BasicLocomotionFrame locomotionFrame = context.LocomotionFrame;
            controller.PresentStateFrameAnimationForPipeline(
                in stateFrame,
                in locomotionFrame,
                context.ExitedToLocomotion,
                out bool actionAnimationPresented,
                out bool locomotionAnimationPresented);
            controller.WriteStateFrameActionFactsForPipeline(
                in stateFrame,
                context.ExitedToLocomotion,
                context.Step);
            controller.UpdateStateSnapshotForPipeline(in stateFrame, context.Step);
            controller.WriteAnimationRuntimeFactsForPipeline(context.Step);
            controller.CompleteLocomotionTickForPipeline();
            controller.LogDiagnosticTickSnapshotsForPipeline(context.Step);
            context.MarkPresentation(actionAnimationPresented, locomotionAnimationPresented, true);
        }

        void RunWriteSnapshotAndEvents(PlayerFullBodyActionController controller, ref FullBodyFrameContext context)
        {
            context.MarkStep(FullBodyFramePipelineStep.WriteSnapshotAndEvents);
            context.MarkSnapshotEventsReady();
            FullBodyDiagnostics.LogPipelineSnapshot(
                controller != null ? controller.ActiveFullBodyStatePath : string.Empty,
                context.Step,
                new FullBodyFrameResult(in context).DiagnosticSummary);
            context.MarkCompleted();
        }

        FullBodyActionRequestGateResult ResolveActionRequest(
            PlayerFullBodyActionController controller,
            in FullBodyFrameContext context,
            in LocomotionDecisionFrame locomotionDecision)
        {
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard = controller.LocomotionController != null
                ? controller.LocomotionController.RuntimeBlackboardSnapshot
                : default;
            bool hasDodgeConfig = controller.TryResolveDodgeActionConfig(out DodgeActionConfig config);
            FullBodyPipelineActionRequestResolverInput input = new FullBodyPipelineActionRequestResolverInput(
                controller.InputBufferComponent != null ? controller.InputBufferComponent.Buffer : null,
                context.Step,
                context.Input.DeltaTime,
                controller.StateMachine,
                controller.CurrentStateSnapshot,
                context.Input.LocomotionInput,
                controller.LocomotionController != null && controller.LocomotionController.RunLatchActive,
                locomotionDecision.Facts,
                runtimeBlackboard,
                hasDodgeConfig,
                config,
                controller.ResolveCurrentActionResistance(),
                controller.ResolveInterruptPoliciesForPipeline());

            return FullBodyPipelineActionRequestResolver.Resolve(in input);
        }

        static void WriteBufferedInputFacts(InputRequestBufferComponent buffer, in FullBodyFrameInput input)
        {
            if (buffer == null)
                return;

            buffer.SetStep(input.Step);
            buffer.AddButtonState(InputButtonKind.Dodge, ToInputButtonState(input.Dodge));
            buffer.AddButtonState(InputButtonKind.Attack, ToInputButtonState(input.Attack));
            buffer.AddButtonState(InputButtonKind.Jump, ToInputButtonState(input.Jump));
            buffer.AddButtonState(InputButtonKind.Interact, ToInputButtonState(input.Interact));
        }

        static InputButtonState ToInputButtonState(PredictionButtonFrame frame)
        {
            return new InputButtonState(frame.Pressed, frame.Held, frame.Released);
        }

    }
}
