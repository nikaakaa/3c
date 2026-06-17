using System.IO;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class CharacterFrameArbitrationTests
    {
        [Test]
        public void BodyArbiterSuppressesLocomotionWhenFullBodyClaimWins()
        {
            CharacterFrameArbitrationInput input = new CharacterFrameArbitrationInput(
                BodyOccupancyClaim.FullBodyAction(12),
                CharacterFrameCandidateOutput.Locomotion(true, true, 12),
                CharacterFrameCandidateOutput.FullBodyAction(true, true, 12),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.UpperBody, 12),
                12);

            BodyOccupancyDecision decision = DefaultBodyArbiter.Instance.Decide(in input);

            Assert.True(decision.FullBodyClaimAccepted);
            Assert.AreEqual(CharacterBodyDomain.FullBodyAction, decision.BaseLayerOwner);
            Assert.True(decision.SuppressLocomotionMotion);
            Assert.True(decision.SuppressLocomotionAnimation);
            Assert.False(decision.AllowUpperBody);
        }

        [Test]
        public void BodyArbiterAllowsLocomotionWithoutFullBodyClaim()
        {
            CharacterFrameArbitrationInput input = new CharacterFrameArbitrationInput(
                BodyOccupancyClaim.None(13),
                CharacterFrameCandidateOutput.Locomotion(true, true, 13),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.FullBodyAction, 13),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.UpperBody, 13),
                13);

            BodyOccupancyDecision decision = DefaultBodyArbiter.Instance.Decide(in input);

            Assert.False(decision.FullBodyClaimAccepted);
            Assert.AreEqual(CharacterBodyDomain.Locomotion, decision.BaseLayerOwner);
            Assert.False(decision.SuppressLocomotionMotion);
            Assert.False(decision.SuppressLocomotionAnimation);
        }

        [Test]
        public void UpperBodyClaimDoesNotImplicitlySuppressBaseLocomotion()
        {
            CharacterFrameArbitrationInput input = new CharacterFrameArbitrationInput(
                BodyOccupancyClaim.UpperBody(14),
                CharacterFrameCandidateOutput.Locomotion(true, true, 14),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.FullBodyAction, 14),
                CharacterFrameCandidateOutput.UpperBody(false, true, 14),
                14);

            BodyOccupancyDecision decision = DefaultBodyArbiter.Instance.Decide(in input);

            Assert.AreEqual(CharacterBodyDomain.Locomotion, decision.BaseLayerOwner);
            Assert.AreEqual(CharacterBodyDomain.UpperBody, decision.UpperBodyOwner);
            Assert.True(decision.AllowUpperBody);
            Assert.False(decision.SuppressLocomotionMotion);
            Assert.False(decision.SuppressLocomotionAnimation);
        }

        [Test]
        public void OutputComposerConsumesPlanBeforeOutput()
        {
            CharacterFrameSubmission submission = CreateSubmission(true, true, true, 15);
            CharacterFrameOutputComposer composer = new CharacterFrameOutputComposer();

            CharacterFramePlan plan = composer.CreatePlan(in submission);
            CharacterFrameOutput output = composer.Compose(in submission, in plan);

            Assert.True(output.Plan.OccupancyDecision.FullBodyClaimAccepted);
            Assert.AreEqual(CharacterBodyDomain.FullBodyAction, output.Plan.BaseLayerOwner);
            Assert.False(output.Movement.ExecuteBasicMovement);
            Assert.True(output.Movement.ExecuteActionMovement);
            Assert.False(output.Animation.PresentLocomotionAnimation);
        }

        [Test]
        public void CharacterFramePipelinePhaseOrderStaysFormal()
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    SimulationTickPhase.ReadInput,
                    SimulationTickPhase.UpdateInputBuffer,
                    SimulationTickPhase.GameplayDecision,
                    SimulationTickPhase.BuildMotion,
                    SimulationTickPhase.ExecuteMotion,
                    SimulationTickPhase.PresentationBridge,
                    SimulationTickPhase.WriteSnapshotAndEvents
                },
                SimulationTickPhaseOrder.Phases);
        }

        [Test]
        public void BodyClaimPolicyResolvesDodgeFullBodyClaim()
        {
            BodyClaimPolicy policy = new BodyClaimPolicy(new[]
            {
                new BodyClaimPolicyDefinition(
                    ActionStateIds.Dodge.Value,
                    BodyOccupancyKind.FullBody,
                    CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation)
            });

            bool resolved = policy.TryResolveClaim(ActionStateIds.Dodge, 21, out BodyOccupancyClaim claim);

            Assert.True(resolved);
            Assert.True(claim.ClaimsFullBody);
            Assert.AreEqual(CharacterBodyDomain.FullBodyAction, claim.Domain);
            Assert.AreEqual(CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation, claim.Channels);
        }

        [Test]
        public void ActionLifecycleFrameBuildsDodgeMotionAndAnimationFromResolvedAction()
        {
            CharacterActionRequest request = new CharacterActionRequest(
                CharacterFrameRequestProviderId.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                31,
                36,
                30,
                0,
                CharacterStateVariant.Directional,
                Vector3.forward);
            CharacterInputRequestFact requestFact = new CharacterInputRequestFact(
                true,
                InputRequestKind.Dodge,
                31,
                36,
                30,
                CharacterStateVariant.Directional,
                Vector3.forward);
            ActionInterruptRequest interruptRequest = new ActionInterruptRequest(
                31,
                ActionRequestType.Dodge,
                ActionStateIds.Dodge,
                30,
                0,
                31,
                36);
            ActionMotionSpec motionSpec = new ActionMotionSpec(
                ActionStateIds.Dodge,
                CharacterStateIds.Dodge,
                CharacterStateVariant.Directional,
                0.35f,
                4f,
                true,
                false,
                Vector3.forward,
                0f,
                31);
            CharacterResolvedAction resolvedAction = new CharacterResolvedAction(
                CharacterFrameRequestProviderId.Dodge,
                request,
                requestFact,
                interruptRequest,
                new ActionInterruptContext(ActionStateIds.None, 0f, 0, 31),
                ActionAnimationKeys.DodgeDirectional,
                motionSpec);

            ActionLifecycleFrame frame = ActionLifecycleFrame.FromResolvedAction(
                in resolvedAction,
                0.1f,
                true,
                false,
                31,
                new ActionAnimationPlaybackIntent(9));

            Assert.True(frame.HasAction);
            Assert.True(frame.StartedThisFrame);
            Assert.AreEqual(ActionStateIds.Dodge, frame.ActionState);
            Assert.AreEqual(0.1f, frame.MotionSpec.StateTime);
            Assert.True(frame.HasAnimationRequest);
            Assert.True(frame.AnimationRequest.IsActionAnimation);
            Assert.AreEqual(ActionAnimationKeys.DodgeDirectional, frame.AnimationRequest.Key);
            Assert.AreEqual(new ActionAnimationPlaybackIntent(9), frame.AnimationRequest.ActionPlaybackIntent);
        }

        [Test]
        public void ActionLifecycleReusesPlaybackIntentWhileActionActive()
        {
            FullBodyActionRuntimeModule module = new FullBodyActionRuntimeModule();
            CharacterResolvedAction action = CreateResolvedDodgeAction(CharacterStateVariant.Directional, ActionAnimationKeys.DodgeDirectional, 31);

            ActionLifecycleFrame first = module.TickActionLifecycle(in action, 0.016f, 31);
            CharacterResolvedAction none = default;
            ActionLifecycleFrame next = module.TickActionLifecycle(in none, 0.016f, 32);

            Assert.True(first.AnimationRequest.HasActionPlaybackIntent);
            Assert.AreEqual(first.AnimationRequest.ActionPlaybackIntent, next.AnimationRequest.ActionPlaybackIntent);
            Assert.AreEqual(32, next.AnimationRequest.SourceStep);
        }

        [Test]
        public void ActionLifecycleChangesPlaybackIntentForNewAcceptedDirectionalDodge()
        {
            FullBodyActionRuntimeModule module = new FullBodyActionRuntimeModule();
            CharacterResolvedAction firstAction = CreateResolvedDodgeAction(CharacterStateVariant.Directional, ActionAnimationKeys.DodgeDirectional, 31);
            CharacterResolvedAction secondAction = CreateResolvedDodgeAction(CharacterStateVariant.Directional, ActionAnimationKeys.DodgeDirectional, 40);

            ActionLifecycleFrame first = module.TickActionLifecycle(in firstAction, 0.016f, 31);
            ActionLifecycleFrame second = module.TickActionLifecycle(in secondAction, 0.016f, 40);

            Assert.AreEqual(ActionAnimationKeys.DodgeDirectional, first.AnimationRequest.Key);
            Assert.AreEqual(ActionAnimationKeys.DodgeDirectional, second.AnimationRequest.Key);
            Assert.AreNotEqual(first.AnimationRequest.ActionPlaybackIntent, second.AnimationRequest.ActionPlaybackIntent);
        }

        [Test]
        public void ActionLifecycleChangesPlaybackIntentForNewAcceptedBackstepDodge()
        {
            FullBodyActionRuntimeModule module = new FullBodyActionRuntimeModule();
            CharacterResolvedAction firstAction = CreateResolvedDodgeAction(CharacterStateVariant.Backstep, ActionAnimationKeys.DodgeBackstep, 31);
            CharacterResolvedAction secondAction = CreateResolvedDodgeAction(CharacterStateVariant.Backstep, ActionAnimationKeys.DodgeBackstep, 40);

            ActionLifecycleFrame first = module.TickActionLifecycle(in firstAction, 0.016f, 31);
            ActionLifecycleFrame second = module.TickActionLifecycle(in secondAction, 0.016f, 40);

            Assert.AreEqual(ActionAnimationKeys.DodgeBackstep, first.AnimationRequest.Key);
            Assert.AreEqual(ActionAnimationKeys.DodgeBackstep, second.AnimationRequest.Key);
            Assert.AreNotEqual(first.AnimationRequest.ActionPlaybackIntent, second.AnimationRequest.ActionPlaybackIntent);
        }

        [Test]
        public void ActionLifecycleRestoreStateCarriesPlaybackIntent()
        {
            CharacterResolvedAction action = CreateResolvedDodgeAction(CharacterStateVariant.Directional, ActionAnimationKeys.DodgeDirectional, 31);
            ActionLifecycleRestoreState restoreState = new ActionLifecycleRestoreState(
                true,
                action,
                0.2f,
                false,
                new ActionAnimationPlaybackIntent(7),
                8);

            Assert.True(restoreState.HasActiveAction);
            Assert.AreEqual(new ActionAnimationPlaybackIntent(7), restoreState.ActivePlaybackIntent);
            Assert.AreEqual(8, restoreState.NextPlaybackIntentValue);
        }

        [Test]
        public void SubmissionWithoutExplicitArbitrationDoesNotInferClaim()
        {
            CharacterFrameSubmission submission = CreateSubmissionWithArbitration(
                true,
                true,
                true,
                22,
                CharacterFrameArbitrationInput.None(22));
            CharacterFramePlan plan = new CharacterFrameOutputComposer().CreatePlan(in submission);

            Assert.False(plan.OccupancyDecision.FullBodyClaimAccepted);
            Assert.AreEqual(CharacterBodyDomain.None, plan.BaseLayerOwner);
        }

        [Test]
        public void CharacterRuntimeCoreCanRunWithoutGameObject()
        {
            CharacterRuntimeCore core = new CharacterRuntimeCore();
            BasicLocomotionInputSnapshot locomotionInput = new BasicLocomotionInputSnapshot(0.016f, Vector2.zero, Vector2.zero, false);
            CharacterFrameInput frameInput = CharacterFrameInput.FromLocomotionInput(1, in locomotionInput);

            Assert.NotNull(core.RuntimePort);
            Assert.False(core.Tick(in frameInput));
            CharacterRuntimeCoreRestoreState restoreState = core.CaptureRestoreState();
            Assert.True(core.Restore(in restoreState));
        }

        static CharacterFrameSubmission CreateSubmission(
            bool executeBasicMovement,
            bool presentLocomotionAnimation,
            bool hasActionMovement,
            int step)
        {
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero, true);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            MovementInputIntent intent = MovementInputIntent.FromRaw(input.Move, settings.InputDeadZone, input.RunHeld);
            LocomotionDecisionFacts facts = new LocomotionDecisionFacts(
                intent,
                BasicMovementGait.Run,
                BasicMovementPhaseFacts.None,
                new LocomotionSpatialFacts(Vector3.forward, Vector3.forward, Vector3.forward, Vector3.right),
                LocomotionTurnBackIntent.None);
            LocomotionDecisionFrame decisionFrame = new LocomotionDecisionFrame(
                input,
                settings,
                intent,
                facts,
                BasicMovementGait.Run);
            BasicLocomotionFrame locomotionFrame = new BasicLocomotionFrame(
                input,
                settings,
                intent,
                Vector3.forward,
                BasicMovementPhase.MoveLoop,
                new MovementCommand(Vector3.forward, 4f, 720f, 0.1f, BasicMovementPhase.MoveLoop, BasicMovementGait.Run, BasicMovementMotionFacts.None(BasicMovementPhase.MoveLoop)));
            ActionMotionSpec actionSpec = hasActionMovement
                ? new ActionMotionSpec(
                    ActionStateIds.Dodge,
                    CharacterStateIds.Dodge,
                    CharacterStateVariant.Directional,
                    0.35f,
                    4f,
                    true,
                    false,
                    Vector3.forward,
                    0.1f,
                    step)
                : ActionMotionSpec.None(step);
            CharacterStateMachineSnapshot snapshot = new CharacterStateMachineSnapshot(
                hasActionMovement ? CharacterStateIds.Dodge : CharacterStateIds.MoveLoop,
                0.1f,
                hasActionMovement ? CharacterStateVariant.Directional : CharacterStateVariant.None,
                string.Empty,
                hasActionMovement
                    ? new[] { CharacterStateTag.FullBody, CharacterStateTag.Action, CharacterStateTag.Dodge }
                    : new[] { CharacterStateTag.FullBody, CharacterStateTag.Locomotion, CharacterStateTag.Movement });
            CharacterStateMachineFrame stateFrame = new CharacterStateMachineFrame(
                snapshot,
                executeBasicMovement,
                presentLocomotionAnimation,
                false,
                InputRequestKind.Dodge,
                false,
                false,
                actionSpec,
                default,
                false,
                CharacterStatePayload.Empty);
            LocomotionStateDecisionFrame stateDecision = new LocomotionStateDecisionFrame(
                decisionFrame,
                stateFrame,
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                intent,
                BasicMovementPhaseFacts.None,
                facts,
                CharacterRuntimeBlackboardSnapshot.Empty,
                false);
            ActionMotionResolveResult actionResult = hasActionMovement
                ? new ActionMotionResolveResult(actionSpec, default, true, false, false, step, "test")
                : ActionMotionResolveResult.None(step);

            CharacterFrameArbitrationInput arbitrationInput = new CharacterFrameArbitrationInput(
                hasActionMovement ? BodyOccupancyClaim.FullBodyAction(step) : BodyOccupancyClaim.None(step),
                CharacterFrameCandidateOutput.Locomotion(executeBasicMovement, presentLocomotionAnimation, step),
                CharacterFrameCandidateOutput.FullBodyAction(hasActionMovement, hasActionMovement, step),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.UpperBody, step),
                step);
            return CreateSubmissionWithArbitration(
                executeBasicMovement,
                presentLocomotionAnimation,
                hasActionMovement,
                step,
                arbitrationInput);
        }

        static CharacterFrameSubmission CreateSubmissionWithArbitration(
            bool executeBasicMovement,
            bool presentLocomotionAnimation,
            bool hasActionMovement,
            int step,
            CharacterFrameArbitrationInput arbitrationInput)
        {
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero, true);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            MovementInputIntent intent = MovementInputIntent.FromRaw(input.Move, settings.InputDeadZone, input.RunHeld);
            LocomotionDecisionFacts facts = new LocomotionDecisionFacts(
                intent,
                BasicMovementGait.Run,
                BasicMovementPhaseFacts.None,
                new LocomotionSpatialFacts(Vector3.forward, Vector3.forward, Vector3.forward, Vector3.right),
                LocomotionTurnBackIntent.None);
            LocomotionDecisionFrame decisionFrame = new LocomotionDecisionFrame(
                input,
                settings,
                intent,
                facts,
                BasicMovementGait.Run);
            BasicLocomotionFrame locomotionFrame = new BasicLocomotionFrame(
                input,
                settings,
                intent,
                Vector3.forward,
                BasicMovementPhase.MoveLoop,
                new MovementCommand(Vector3.forward, 4f, 720f, 0.1f, BasicMovementPhase.MoveLoop, BasicMovementGait.Run, BasicMovementMotionFacts.None(BasicMovementPhase.MoveLoop)));
            ActionMotionSpec actionSpec = hasActionMovement
                ? new ActionMotionSpec(
                    ActionStateIds.Dodge,
                    CharacterStateIds.Dodge,
                    CharacterStateVariant.Directional,
                    0.35f,
                    4f,
                    true,
                    false,
                    Vector3.forward,
                    0.1f,
                    step)
                : ActionMotionSpec.None(step);
            CharacterStateMachineSnapshot snapshot = new CharacterStateMachineSnapshot(
                hasActionMovement ? CharacterStateIds.Dodge : CharacterStateIds.MoveLoop,
                0.1f,
                hasActionMovement ? CharacterStateVariant.Directional : CharacterStateVariant.None,
                string.Empty,
                hasActionMovement
                    ? new[] { CharacterStateTag.FullBody, CharacterStateTag.Action, CharacterStateTag.Dodge }
                    : new[] { CharacterStateTag.FullBody, CharacterStateTag.Locomotion, CharacterStateTag.Movement });
            CharacterStateMachineFrame stateFrame = new CharacterStateMachineFrame(
                snapshot,
                executeBasicMovement,
                presentLocomotionAnimation,
                false,
                InputRequestKind.Dodge,
                false,
                false,
                actionSpec,
                default,
                false,
                CharacterStatePayload.Empty);
            LocomotionStateDecisionFrame stateDecision = new LocomotionStateDecisionFrame(
                decisionFrame,
                stateFrame,
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                intent,
                BasicMovementPhaseFacts.None,
                facts,
                CharacterRuntimeBlackboardSnapshot.Empty,
                false);
            ActionMotionResolveResult actionResult = hasActionMovement
                ? new ActionMotionResolveResult(actionSpec, default, true, false, false, step, "test")
                : ActionMotionResolveResult.None(step);

            return new CharacterFrameSubmission(
                CharacterFrameSubmissionSource.CharacterRuntimeGraph,
                step,
                decisionFrame,
                stateDecision,
                locomotionFrame,
                stateFrame,
                actionResult,
                CharacterInputRequestFact.None(InputRequestKind.Dodge),
                ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoRequest),
                StateTimelineFactsTrace.None,
                CharacterStateMachineSnapshot.Inactive,
                false,
                arbitrationInput);
        }

        static CharacterResolvedAction CreateResolvedDodgeAction(
            CharacterStateVariant variant,
            ActionAnimationKey animationKey,
            int sourceStep)
        {
            Vector3 direction = variant == CharacterStateVariant.Backstep ? Vector3.back : Vector3.forward;
            CharacterActionRequest request = new CharacterActionRequest(
                CharacterFrameRequestProviderId.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                sourceStep,
                sourceStep + 5,
                30,
                0,
                variant,
                direction);
            CharacterInputRequestFact requestFact = new CharacterInputRequestFact(
                true,
                InputRequestKind.Dodge,
                sourceStep,
                sourceStep + 5,
                30,
                variant,
                direction);
            ActionMotionSpec motionSpec = new ActionMotionSpec(
                ActionStateIds.Dodge,
                CharacterStateIds.Dodge,
                variant,
                0.35f,
                variant == CharacterStateVariant.Backstep ? 2.5f : 4f,
                variant != CharacterStateVariant.Backstep,
                false,
                direction,
                0f,
                sourceStep);
            return new CharacterResolvedAction(
                CharacterFrameRequestProviderId.Dodge,
                request,
                requestFact,
                new ActionInterruptRequest(sourceStep, ActionRequestType.Dodge, ActionStateIds.Dodge, 30, 0, sourceStep, sourceStep + 5),
                new ActionInterruptContext(ActionStateIds.None, 0f, 0, sourceStep),
                animationKey,
                motionSpec);
        }

    }
}
