using System;
using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;

namespace ThirdPersonAction
{
    public sealed class FullBodyRuntimePortAdapter : ICharacterFrameRuntimePort
    {
        readonly PlayerFullBodyActionController controller;

        public FullBodyRuntimePortAdapter(PlayerFullBodyActionController controller)
        {
            this.controller = controller;
        }

        public ILocomotionFrameRuntimePort LocomotionFrameRuntime =>
            controller != null ? controller.LocomotionController : null;

        public CharacterStateMachineRunner StateMachine =>
            controller != null ? controller.StateMachine : null;

        public CharacterStateMachineSnapshot CurrentStateSnapshot =>
            controller != null ? controller.CurrentStateSnapshot : CharacterStateMachineSnapshot.Inactive;

        public InputRequestBuffer InputRequestBuffer =>
            controller != null && controller.InputBufferComponent != null ? controller.InputBufferComponent.Buffer : null;

        public string ActiveFullBodyStatePath =>
            controller != null ? controller.ActiveFullBodyStatePath : string.Empty;

        FullBodyOutputRuntime OutputRuntime =>
            controller != null ? controller.OutputRuntime : null;

        public bool PrepareFrameRuntimeAdapters()
        {
            return controller != null && controller.PrepareFramePipelineAdapters();
        }

        public bool TryResolveDodgeActionConfig(out DodgeActionConfig config)
        {
            if (controller != null)
                return controller.TryResolveDodgeActionConfig(out config);

            config = default;
            return false;
        }

        public int ResolveCurrentActionResistance()
        {
            return controller != null ? controller.ResolveCurrentActionResistance() : 0;
        }

        public IReadOnlyList<ActionInterruptPolicy> ResolveInterruptPolicies()
        {
            return controller != null ? controller.ResolveInterruptPoliciesForPipeline() : Array.Empty<ActionInterruptPolicy>();
        }

        public bool WriteBufferedInputFacts(in CharacterFrameInput input)
        {
            InputRequestBufferComponent buffer = controller != null ? controller.InputBufferComponent : null;
            if (buffer == null)
                return false;

            buffer.SetStep(input.Step);
            buffer.AddButtonState(InputButtonKind.Dodge, ToInputButtonState(input.Dodge));
            buffer.AddButtonState(InputButtonKind.Attack, ToInputButtonState(input.Attack));
            buffer.AddButtonState(InputButtonKind.Jump, ToInputButtonState(input.Jump));
            buffer.AddButtonState(InputButtonKind.Interact, ToInputButtonState(input.Interact));
            return true;
        }

        public void SetLastFrameOutputs(
            in BasicLocomotionFrame locomotionFrame,
            in CharacterStateMachineFrame stateFrame,
            in ActionMotionResolveResult actionMotionResult)
        {
            OutputRuntime?.SetLastFrameOutputs(in locomotionFrame, in stateFrame, in actionMotionResult);
        }

        public bool ConsumeStateFrameInputRequest(in CharacterStateMachineFrame stateFrame, int step)
        {
            FullBodyOutputRuntime outputRuntime = OutputRuntime;
            return outputRuntime != null && outputRuntime.ConsumeStateFrameInputRequest(in stateFrame, step);
        }

        public void ExecuteStateFrameMotion(
            in CharacterStateMachineFrame stateFrame,
            in BasicLocomotionFrame locomotionFrame,
            in ActionMotionResolveResult actionMotionResult,
            out bool actionMovementExecuted,
            out bool basicMovementExecuted)
        {
            FullBodyOutputRuntime outputRuntime = OutputRuntime;
            if (outputRuntime != null)
            {
                outputRuntime.ExecuteStateFrameMotion(
                    in stateFrame,
                    in locomotionFrame,
                    in actionMotionResult,
                    out actionMovementExecuted,
                    out basicMovementExecuted);
                return;
            }

            actionMovementExecuted = false;
            basicMovementExecuted = false;
        }

        public void PresentStateFrameAnimation(
            in CharacterStateMachineFrame stateFrame,
            in BasicLocomotionFrame locomotionFrame,
            bool exitedToLocomotion,
            out bool actionAnimationPresented,
            out bool locomotionAnimationPresented)
        {
            FullBodyOutputRuntime outputRuntime = OutputRuntime;
            if (outputRuntime != null)
            {
                outputRuntime.PresentStateFrameAnimation(
                    in stateFrame,
                    in locomotionFrame,
                    exitedToLocomotion,
                    out actionAnimationPresented,
                    out locomotionAnimationPresented);
                return;
            }

            actionAnimationPresented = false;
            locomotionAnimationPresented = false;
        }

        public void WriteStateFrameActionFacts(
            in CharacterStateMachineFrame stateFrame,
            in ActionMotionResolveResult actionMotionResult,
            bool exitedToLocomotion,
            int step)
        {
            OutputRuntime?.WriteStateFrameActionFacts(in stateFrame, in actionMotionResult, exitedToLocomotion, step);
        }

        public void UpdateStateSnapshot(in CharacterStateMachineFrame stateFrame, int step)
        {
            OutputRuntime?.UpdateStateSnapshot(in stateFrame, step);
        }

        public void WriteAnimationRuntimeFacts(int step)
        {
            OutputRuntime?.WriteAnimationRuntimeFacts(step);
        }

        public void CompleteLocomotionTick()
        {
            OutputRuntime?.CompleteLocomotionTick();
        }

        public void LogDiagnosticTickSnapshots(int step)
        {
            OutputRuntime?.LogDiagnosticTickSnapshots(step);
        }

        static InputButtonState ToInputButtonState(PredictionButtonFrame frame)
        {
            return new InputButtonState(frame.Pressed, frame.Held, frame.Released);
        }
    }
}
