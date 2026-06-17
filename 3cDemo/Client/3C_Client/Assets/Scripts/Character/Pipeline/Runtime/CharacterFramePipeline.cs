using System;
using ThirdPersonCharacterStateMachine;
using ThirdPersonMovement;
using ThirdPersonSimulation;

namespace ThirdPersonAction
{
    public sealed class CharacterFramePipeline
    {
        readonly ICharacterFrameRequestSubmitter requestSubmitter;
        readonly ICharacterFrameOutputSubmitter outputSubmitter;
        readonly CharacterFrameOutputComposer outputComposer = new CharacterFrameOutputComposer();
        readonly CharacterFrameOutputApplier outputApplier = new CharacterFrameOutputApplier();

        public CharacterFramePipeline(
            ICharacterFrameRequestSubmitter requestSubmitter,
            ICharacterFrameOutputSubmitter outputSubmitter)
        {
            this.requestSubmitter = requestSubmitter ?? throw new ArgumentNullException(nameof(requestSubmitter));
            this.outputSubmitter = outputSubmitter ?? throw new ArgumentNullException(nameof(outputSubmitter));
        }

        public bool Tick(
            ICharacterFrameRuntimePort runtime,
            in CharacterFrameInput input,
            out CharacterFrameResult result)
        {
            CharacterFrameContext context = BeginFrame(in input);
            result = default;
            foreach (SimulationTickPhase phase in SimulationTickPhaseOrder.Phases)
                RunPhase(runtime, phase, ref context, out result);

            return result.Success;
        }

        public CharacterFrameContext BeginFrame(in CharacterFrameInput input)
        {
            return new CharacterFrameContext(input);
        }

        public bool RunPhase(
            ICharacterFrameRuntimePort runtime,
            SimulationTickPhase phase,
            ref CharacterFrameContext context,
            out CharacterFrameResult result)
        {
            if (context.CurrentStep == CharacterFramePipelineStep.Failed)
            {
                result = new CharacterFrameResult(in context);
                return false;
            }

            switch (phase)
            {
                case SimulationTickPhase.ReadInput:
                    context.MarkStep(CharacterFramePipelineStep.ReadInput);
                    break;
                case SimulationTickPhase.UpdateInputBuffer:
                    RunUpdateInputBuffer(runtime, ref context);
                    break;
                case SimulationTickPhase.GameplayDecision:
                    RunGameplayDecision(runtime, ref context);
                    break;
                case SimulationTickPhase.BuildMotion:
                    RunBuildMotion(runtime, ref context);
                    break;
                case SimulationTickPhase.ExecuteMotion:
                    RunExecuteMotion(runtime, ref context);
                    break;
                case SimulationTickPhase.PresentationBridge:
                    RunPresentationBridge(runtime, ref context);
                    break;
                case SimulationTickPhase.WriteSnapshotAndEvents:
                    RunWriteSnapshotAndEvents(runtime, ref context);
                    break;
            }

            result = new CharacterFrameResult(in context);
            return result.Success || context.CurrentStep != CharacterFramePipelineStep.Failed;
        }

        void RunUpdateInputBuffer(ICharacterFrameRuntimePort runtime, ref CharacterFrameContext context)
        {
            context.MarkStep(CharacterFramePipelineStep.UpdateInputBuffer);
            if (runtime == null)
            {
                context.MarkFailed("runtime-missing");
                return;
            }

            CharacterFrameInput input = context.Input;
            if (input.HasBufferedButtonFacts)
                runtime.WriteBufferedInputFacts(in input);
        }

        void RunGameplayDecision(ICharacterFrameRuntimePort runtime, ref CharacterFrameContext context)
        {
            context.MarkStep(CharacterFramePipelineStep.GameplayDecision);
            requestSubmitter.TrySubmitFrameRequests(runtime, ref context);
        }

        void RunBuildMotion(ICharacterFrameRuntimePort runtime, ref CharacterFrameContext context)
        {
            context.MarkStep(CharacterFramePipelineStep.BuildMotion);
            if (!outputSubmitter.TrySubmitFrameOutput(runtime, ref context, out CharacterFrameSubmission submission))
                return;

            CharacterFramePlan plan = outputComposer.CreatePlan(in submission);
            CharacterFrameOutput output = outputComposer.Compose(in submission, in plan);
            context.SetOutput(in output);
            outputApplier.ApplyFrameCache(runtime, in output);
        }

        void RunExecuteMotion(ICharacterFrameRuntimePort runtime, ref CharacterFrameContext context)
        {
            context.MarkStep(CharacterFramePipelineStep.ExecuteMotion);
            if (runtime == null)
            {
                context.MarkFailed("runtime-missing");
                return;
            }

            outputApplier.ApplyMotion(runtime, ref context);
        }

        void RunPresentationBridge(ICharacterFrameRuntimePort runtime, ref CharacterFrameContext context)
        {
            context.MarkStep(CharacterFramePipelineStep.PresentationBridge);
            if (runtime == null)
            {
                context.MarkFailed("runtime-missing");
                return;
            }

            outputApplier.ApplyPresentationAndFacts(runtime, ref context);
        }

        void RunWriteSnapshotAndEvents(ICharacterFrameRuntimePort runtime, ref CharacterFrameContext context)
        {
            context.MarkStep(CharacterFramePipelineStep.WriteSnapshotAndEvents);
            context.MarkSnapshotEventsReady();
            FullBodyDiagnostics.LogPipelineSnapshot(
                runtime != null ? runtime.ActiveFrameStatePath : string.Empty,
                context.Step,
                new CharacterFrameResult(in context).DiagnosticSummary);
            context.MarkCompleted();
        }
    }

    public sealed class CharacterFrameOutputComposer
    {
        readonly IBodyArbiter bodyArbiter;

        public CharacterFrameOutputComposer()
            : this(DefaultBodyArbiter.Instance)
        {
        }

        public CharacterFrameOutputComposer(IBodyArbiter bodyArbiter)
        {
            this.bodyArbiter = bodyArbiter ?? throw new ArgumentNullException(nameof(bodyArbiter));
        }

        public CharacterFramePlan CreatePlan(in CharacterFrameSubmission submission)
        {
            return bodyArbiter.CreatePlan(in submission);
        }

        public CharacterFrameOutput Compose(
            in CharacterFrameSubmission submission,
            in CharacterFramePlan plan)
        {
            return new CharacterFrameOutput(submission, plan);
        }

        public CharacterFrameOutput Compose(in CharacterFramePlan plan)
        {
            return new CharacterFrameOutput(plan);
        }
    }

    public sealed class CharacterFrameOutputApplier
    {
        public void ApplyFrameCache(ICharacterFrameOutputRuntimePort runtime, in CharacterFrameOutput output)
        {
            if (runtime == null || !output.HasSubmission)
                return;

            CharacterFrameMovementSubmission movement = output.Movement;
            CharacterFrameRuntimeFactsSubmission runtimeFacts = output.RuntimeFacts;
            runtime.SetLastFrameOutputs(
                movement.LocomotionFrame,
                runtimeFacts.StateFrame,
                movement.ActionMotionResult);
        }

        public void ApplyMotion(ICharacterFrameOutputRuntimePort runtime, ref CharacterFrameContext context)
        {
            CharacterFrameOutput output = context.Output;
            if (!output.HasSubmission)
            {
                context.MarkFailed("output-missing");
                return;
            }

            CharacterFrameRuntimeFactsSubmission runtimeFacts = output.RuntimeFacts;
            CharacterFrameInputConsumeSubmission inputConsume = output.InputConsume;
            if (inputConsume.HasInputConsume && runtime.ConsumeFrameInputRequest(in inputConsume))
                context.MarkInputRequestConsumed();

            CharacterFrameMovementSubmission movement = output.Movement;
            runtime.ExecuteFrameMotion(
                in movement,
                out bool actionMovementExecuted,
                out bool basicMovementExecuted);
            context.MarkMotionExecuted(actionMovementExecuted, basicMovementExecuted);
        }

        public void ApplyPresentationAndFacts(ICharacterFrameOutputRuntimePort runtime, ref CharacterFrameContext context)
        {
            CharacterFrameOutput output = context.Output;
            if (!output.HasSubmission)
            {
                context.MarkFailed("output-missing");
                return;
            }

            CharacterFrameRuntimeFactsSubmission runtimeFacts = output.RuntimeFacts;
            CharacterFrameAnimationSubmission animation = output.Animation;
            CharacterFrameMovementSubmission movement = output.Movement;
            CharacterStateMachineFrame stateFrame = runtimeFacts.StateFrame;
            BasicLocomotionFrame locomotionFrame = movement.LocomotionFrame;
            runtime.PresentFrameAnimation(
                in animation,
                in locomotionFrame,
                out bool actionAnimationPresented,
                out bool locomotionAnimationPresented);
            runtime.WriteStateFrameActionFacts(
                in stateFrame,
                runtimeFacts.ActionMotionResult,
                runtimeFacts.ExitedToLocomotion,
                runtimeFacts.Step);
            if (runtimeFacts.WriteLocomotionPreemption)
            {
                LocomotionPreemptionFact locomotionPreemption = runtimeFacts.LocomotionPreemption;
                runtime.WriteLocomotionPreemptionFact(in locomotionPreemption);
            }
            runtime.UpdateStateSnapshot(in stateFrame, runtimeFacts.Step);
            runtime.WriteAnimationRuntimeFacts(runtimeFacts.Step);
            runtime.CompleteLocomotionTick();
            runtime.LogDiagnosticTickSnapshots(runtimeFacts.Step);
            context.MarkPresentation(actionAnimationPresented, locomotionAnimationPresented, true);
        }
    }
}
