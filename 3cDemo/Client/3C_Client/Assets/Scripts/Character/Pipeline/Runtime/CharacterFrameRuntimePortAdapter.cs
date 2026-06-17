using System;
using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;

namespace ThirdPersonAction
{
    public sealed class CharacterFrameRuntimePortAdapter : ICharacterFrameRuntimePort
    {
        readonly CharacterRuntimeCore core;

        public CharacterFrameRuntimePortAdapter(CharacterRuntimeCore core)
        {
            this.core = core;
        }

        FullBodyOutputRuntime OutputRuntime =>
            core != null ? core.ActionOutputRuntime : null;

        public ILocomotionFrameRuntimePort LocomotionFrameRuntime =>
            core != null ? core.LocomotionFrameRuntime : null;

        public CharacterStateMachineRunner StateMachine =>
            core != null ? core.StateMachine : null;

        public CharacterStateMachineSnapshot CurrentStateSnapshot =>
            core != null ? core.CurrentStateSnapshot : CharacterStateMachineSnapshot.Inactive;

        public InputRequestBuffer InputRequestBuffer =>
            core != null ? core.InputRequestBuffer : null;

        public string ActiveFrameStatePath =>
            core != null ? core.ActiveFrameStatePath : string.Empty;

        public bool PrepareFrameRuntimeAdapters()
        {
            return core != null && core.PrepareFrameRuntimeAdapters();
        }

        public bool TryResolveActionCatalog(out CharacterActionCatalog catalog)
        {
            if (core != null)
                return core.TryResolveActionCatalog(out catalog);

            catalog = CharacterActionCatalog.Empty;
            return false;
        }

        public bool TryResolveBodyClaimPolicy(out BodyClaimPolicy policy)
        {
            if (core != null)
                return core.TryResolveBodyClaimPolicy(out policy);

            policy = BodyClaimPolicy.Empty;
            return false;
        }

        public ActionLifecycleFrame TickActionLifecycle(
            in CharacterResolvedAction acceptedAction,
            in CharacterActionCatalog actionCatalog,
            float deltaTime,
            int step)
        {
            return core != null
                ? core.TickActionLifecycle(in acceptedAction, in actionCatalog, deltaTime, step)
                : ActionLifecycleFrame.None(step);
        }

        public void CompleteActionLifecycle(in ActionMotionResolveResult result, bool requireAnimationEnded)
        {
            core?.CompleteActionLifecycle(in result, requireAnimationEnded);
        }

        public int ResolveCurrentActionResistance()
        {
            return core != null ? core.ResolveCurrentActionResistance() : 0;
        }

        public IReadOnlyList<ActionInterruptPolicy> ResolveInterruptPolicies()
        {
            return core != null
                ? core.ResolveInterruptPolicies()
                : Array.Empty<ActionInterruptPolicy>();
        }

        public bool WriteBufferedInputFacts(in CharacterFrameInput input)
        {
            return core != null && core.WriteBufferedInputFacts(in input);
        }

        public void SetLastFrameOutputs(
            in BasicLocomotionFrame locomotionFrame,
            in CharacterStateMachineFrame stateFrame,
            in ActionMotionResolveResult actionMotionResult)
        {
            OutputRuntime?.SetLastFrameOutputs(in locomotionFrame, in stateFrame, in actionMotionResult);
        }

        public bool ConsumeFrameInputRequest(in CharacterFrameInputConsumeSubmission inputConsume)
        {
            FullBodyOutputRuntime outputRuntime = OutputRuntime;
            return outputRuntime != null && outputRuntime.ConsumeFrameInputRequest(in inputConsume);
        }

        public void ExecuteFrameMotion(
            in CharacterFrameMovementSubmission movement,
            out bool actionMovementExecuted,
            out bool basicMovementExecuted)
        {
            FullBodyOutputRuntime outputRuntime = OutputRuntime;
            if (outputRuntime != null)
            {
                outputRuntime.ExecuteFrameMotion(
                    in movement,
                    out actionMovementExecuted,
                    out basicMovementExecuted);
                return;
            }

            actionMovementExecuted = false;
            basicMovementExecuted = false;
        }

        public void PresentFrameAnimation(
            in CharacterFrameAnimationSubmission animation,
            in BasicLocomotionFrame locomotionFrame,
            out bool actionAnimationPresented,
            out bool locomotionAnimationPresented)
        {
            FullBodyOutputRuntime outputRuntime = OutputRuntime;
            if (outputRuntime != null)
            {
                outputRuntime.PresentFrameAnimation(
                    in animation,
                    in locomotionFrame,
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

        public void WriteLocomotionPreemptionFact(in LocomotionPreemptionFact fact)
        {
            OutputRuntime?.WriteLocomotionPreemptionFact(in fact);
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
    }
}
