using System.IO;
using System.Text;
using System.Collections.Generic;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterBehavior;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor.Character.Behavior
{
    public sealed class CharacterBehaviorSubmissionEntryTests
    {
        const string DefaultDefinitionPath = "Assets/Configs/3C/Behavior/DefaultCharacterBehaviorRuntimeDefinition.asset";
        const string CorinConfigPath = "Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset";

        [Test]
        public void DefaultBehaviorDefinitionUsesFixedProductionLeafOrder()
        {
            CharacterBehaviorRuntimeDefinitionSO asset =
                AssetDatabase.LoadAssetAtPath<CharacterBehaviorRuntimeDefinitionSO>(DefaultDefinitionPath);

            Assert.NotNull(asset);
            CharacterBehaviorRuntimeDefinition definition = asset.ToDefinition();
            Assert.True(definition.IsValid, definition.Diagnostic);
            Assert.True(definition.HasRequiredProductionOrder);
            Assert.AreEqual(CharacterBehaviorSourceKind.Locomotion, definition.GetLeafAt(0));
            Assert.AreEqual(CharacterBehaviorSourceKind.CommittedAction, definition.GetLeafAt(1));
        }

        [Test]
        public void CorinCharacterConfigReferencesFormalBehaviorDefinition()
        {
            CharacterConfigSO config = AssetDatabase.LoadAssetAtPath<CharacterConfigSO>(CorinConfigPath);

            Assert.NotNull(config);
            Assert.NotNull(config.BehaviorRuntimeDefinition);
            Assert.True(config.BehaviorRuntimeDefinition.ToDefinition().IsValid);
        }

        [Test]
        public void RuntimeDefinitionRejectsMissingFormalDefinition()
        {
            CharacterBehaviorRuntimeDefinition definition =
                CharacterBehaviorRuntimeDefinition.Invalid("behavior-entry-definition-missing");
            CharacterBehaviorSubmissionRunner runner = new CharacterBehaviorSubmissionRunner(definition);
            CharacterFrameInput input = CharacterFrameInput.FromLocomotionInput(17, default);
            CharacterFrameContext context = new CharacterFrameContext(input);

            bool success = runner.TrySubmitFrameRequests(null, ref context);

            Assert.False(success);
            Assert.AreEqual("behavior-entry-definition-missing", context.FailureReason);
        }

        [Test]
        public void RuntimeDefinitionRejectsUnsupportedLeafShape()
        {
            CharacterBehaviorRuntimeDefinition definition = new CharacterBehaviorRuntimeDefinition(
                new CharacterBehaviorSourceId("behavior.root"),
                new[] { CharacterBehaviorSourceKind.Locomotion, CharacterBehaviorSourceKind.Locomotion });

            Assert.False(definition.IsValid);
            Assert.AreEqual("behavior-entry-committed-action-leaf-order-invalid", definition.Diagnostic);
        }

        [Test]
        public void RequestPassRunsLocomotionBeforeCommittedAction()
        {
            List<string> calls = new List<string>();
            CharacterBehaviorSubmissionRunner runner = CreateRecordingRunner(calls, true, true);
            CharacterFrameContext context = new CharacterFrameContext(
                CharacterFrameInput.FromLocomotionInput(21, default));

            bool success = runner.TrySubmitFrameRequests(null, ref context);

            Assert.True(success);
            CollectionAssert.AreEqual(new[] { "locomotion-request", "action-request" }, calls);
            Assert.AreEqual(CharacterBehaviorSourceKind.Locomotion, runner.LastRequestTrace.SourceAt(0));
            Assert.AreEqual(CharacterBehaviorSourceKind.CommittedAction, runner.LastRequestTrace.SourceAt(1));
        }

        [Test]
        public void OutputPassRunsLocomotionBeforeCommittedAction()
        {
            List<string> calls = new List<string>();
            CharacterBehaviorSubmissionRunner runner = CreateRecordingRunner(calls, true, true);
            CharacterFrameContext context = new CharacterFrameContext(
                CharacterFrameInput.FromLocomotionInput(22, default));

            bool success = runner.TrySubmitFrameOutput(null, ref context, out _);

            Assert.False(success);
            Assert.AreEqual("behavior-required-output-missing", context.FailureReason);
            CollectionAssert.AreEqual(new[] { "locomotion-output", "action-output" }, calls);
            Assert.AreEqual(CharacterBehaviorSourceKind.Locomotion, runner.LastOutputTrace.SourceAt(0));
            Assert.AreEqual(CharacterBehaviorSourceKind.CommittedAction, runner.LastOutputTrace.SourceAt(1));
        }

        [Test]
        public void MissingRegisteredLeafFailsExplicitly()
        {
            CharacterBehaviorRuntimeDefinition definition = CreateDefaultDefinition();
            CharacterBehaviorSubmissionRunner runner = new CharacterBehaviorSubmissionRunner(
                definition,
                new ICharacterBehaviorSubmissionLeaf[]
                {
                    new RecordingLeaf(CharacterBehaviorSourceKind.Locomotion, new List<string>(), true, true)
                },
                new CharacterBehaviorSubmissionComposer());
            CharacterFrameContext context = new CharacterFrameContext(
                CharacterFrameInput.FromLocomotionInput(23, default));

            bool success = runner.TrySubmitFrameRequests(null, ref context);

            Assert.False(success);
            Assert.AreEqual("behavior-entry-leaf-unsupported:CommittedAction", context.FailureReason);
        }

        [Test]
        public void ComposerReportsMissingRequiredOutput()
        {
            CharacterBehaviorSubmissionComposer composer = new CharacterBehaviorSubmissionComposer();
            CharacterFrameContext context = new CharacterFrameContext(
                CharacterFrameInput.FromLocomotionInput(24, default));

            bool success = composer.TryCompose(
                new CharacterBehaviorSubmissionSet(),
                in context,
                out _,
                out string diagnostic);

            Assert.False(success);
            Assert.AreEqual("behavior-required-output-missing", diagnostic);
        }

        [Test]
        public void ComposerMergesLocomotionRequiredOutputWithOptionalCommittedActionOutput()
        {
            const int step = 31;
            CharacterBehaviorSubmissionComposer composer = new CharacterBehaviorSubmissionComposer();
            CharacterBehaviorSubmissionSet submissions = new CharacterBehaviorSubmissionSet();
            submissions.Add(CreateLocomotionOutput(step, true, true));
            submissions.Add(CreateCommittedActionOutput(step, false));
            CharacterFrameContext context = new CharacterFrameContext(
                CharacterFrameInput.FromLocomotionInput(step, new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero, true)));

            bool success = composer.TryCompose(
                submissions,
                in context,
                out CharacterFrameSubmission submission,
                out string diagnostic);

            Assert.True(success, diagnostic);
            Assert.True(submission.StateFrame.ExecuteBasicMovement);
            Assert.True(submission.StateFrame.PresentLocomotionAnimation);
            Assert.True(submission.ActionMotionResult.HasSpec);
            Assert.True(submission.ActionMotionResult.HasActionMovement);
            Assert.True(submission.ActionOutput.HasCommittedActionBranchOutcome);
            Assert.AreEqual(ActionAnimationKeys.DodgeDirectional, submission.ActionOutput.AnimationRequest.Key);
            Assert.True(submission.ArbitrationInput.OccupancyClaim.ClaimsFullBody);
            Assert.True(submission.ArbitrationInput.LocomotionCandidate.HasMotionCandidate);
            Assert.True(submission.ArbitrationInput.LocomotionCandidate.HasAnimationCandidate);
            Assert.True(submission.ArbitrationInput.CommittedActionCandidate.HasMotionCandidate);
            Assert.True(submission.ArbitrationInput.CommittedActionCandidate.HasAnimationCandidate);
            CharacterFramePlan plan = CharacterFramePlan.PassThrough(in submission);
            Assert.True(plan.HasPlan);
            Assert.AreEqual(CharacterBodyDomain.CommittedAction, plan.BaseSlotOwner);
            Assert.True(plan.SuppressesLocomotionMotion);
            Assert.True(plan.SuppressesLocomotionAnimation);
        }

        [Test]
        public void ComposerRejectsRequiredCommittedActionOutput()
        {
            const int step = 32;
            CharacterBehaviorSubmissionComposer composer = new CharacterBehaviorSubmissionComposer();
            CharacterBehaviorSubmissionSet submissions = new CharacterBehaviorSubmissionSet();
            submissions.Add(CreateLocomotionOutput(step, true, true));
            submissions.Add(CreateCommittedActionOutput(step, true));
            CharacterFrameContext context = new CharacterFrameContext(
                CharacterFrameInput.FromLocomotionInput(step, default));

            bool success = composer.TryCompose(
                submissions,
                in context,
                out _,
                out string diagnostic);

            Assert.False(success);
            Assert.AreEqual("behavior-required-output-unsupported:CommittedAction", diagnostic);
        }

        [Test]
        public void RuntimeCoreDefaultHostUsesBehaviorSubmissionRunner()
        {
            string runtimeCore = File.ReadAllText(
                Path.Combine(Application.dataPath, "Scripts/Character/Pipeline/Runtime/CharacterRuntimeCore.cs"),
                Encoding.UTF8);

            Assert.That(runtimeCore, Does.Contain("CharacterBehaviorSubmissionRunner"));
            Assert.That(runtimeCore, Does.Contain("TryResolveBehaviorRuntimeDefinition"));
            Assert.That(runtimeCore, Does.Not.Contain("CharacterFrameSubmitterChain.CreateDefault()"));
            Assert.That(runtimeCore, Does.Not.Contain("CharacterFrameSubmitterGraph"));
        }

        [Test]
        public void BehaviorEntryKeepsSingleExistingSubmitterDelegates()
        {
            string leafs = File.ReadAllText(
                Path.Combine(Application.dataPath, "Scripts/Character/Behavior/Runtime/CharacterBehaviorSubmissionLeafs.cs"),
                Encoding.UTF8);

            Assert.That(leafs, Does.Contain("new LocomotionFrameSubmitter()"));
            Assert.That(leafs, Does.Contain("new CommittedActionFrameSubmitter()"));
            Assert.That(leafs, Does.Not.Contain("new CharacterFrameSubmitterChain"));
            Assert.That(leafs, Does.Not.Contain("new CharacterFrameRuntimeHost"));
            Assert.That(leafs, Does.Not.Contain("MotionExecutor."));
            Assert.That(leafs, Does.Not.Contain("AnimationPresenter."));
            Assert.That(leafs, Does.Not.Contain("WriteBlackboard"));
        }

        [Test]
        public void ComposerUsesExistingFrameOutputComposerOnly()
        {
            string composer = File.ReadAllText(
                Path.Combine(Application.dataPath, "Scripts/Character/Behavior/Runtime/CharacterBehaviorSubmissionComposer.cs"),
                Encoding.UTF8);

            Assert.That(composer, Does.Contain("CharacterFrameOutputComposer"));
            Assert.That(composer, Does.Not.Contain("new DefaultBodyArbiter"));
            Assert.That(composer, Does.Not.Contain("MotionExecutor"));
            Assert.That(composer, Does.Not.Contain("AnimationPresenter"));
            Assert.That(composer, Does.Not.Contain("WriteBlackboard"));
        }

        static CharacterBehaviorSubmissionRunner CreateRecordingRunner(
            List<string> calls,
            bool locomotionSuccess,
            bool actionSuccess)
        {
            return new CharacterBehaviorSubmissionRunner(
                CreateDefaultDefinition(),
                new ICharacterBehaviorSubmissionLeaf[]
                {
                    new RecordingLeaf(CharacterBehaviorSourceKind.Locomotion, calls, locomotionSuccess, locomotionSuccess),
                    new RecordingLeaf(CharacterBehaviorSourceKind.CommittedAction, calls, actionSuccess, actionSuccess)
                },
                new CharacterBehaviorSubmissionComposer());
        }

        static CharacterBehaviorRuntimeDefinition CreateDefaultDefinition()
        {
            return new CharacterBehaviorRuntimeDefinition(
                new CharacterBehaviorSourceId("behavior.root"),
                new[]
                {
                    CharacterBehaviorSourceKind.Locomotion,
                    CharacterBehaviorSourceKind.CommittedAction
                });
        }

        static BehaviorOutputSubmission CreateLocomotionOutput(
            int step,
            bool executeBasicMovement,
            bool presentLocomotionAnimation)
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
                new MovementCommand(
                    Vector3.forward,
                    4f,
                    720f,
                    0.1f,
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    BasicMovementMotionFacts.None(BasicMovementPhase.MoveLoop)));
            CharacterStateMachineSnapshot snapshot = new CharacterStateMachineSnapshot(
                CharacterStateIds.MoveLoop,
                0.1f,
                CharacterStateVariant.None,
                string.Empty,
                new[] { CharacterStateTag.Character, CharacterStateTag.Locomotion, CharacterStateTag.Movement });
            CharacterStateMachineFrame stateFrame = new CharacterStateMachineFrame(
                snapshot,
                executeBasicMovement,
                presentLocomotionAnimation,
                false,
                InputRequestKind.Dodge,
                false,
                false,
                ActionMotionSpec.None(step),
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
            CharacterBehaviorSubmissionSource source = CharacterBehaviorSubmissionSource.Create(
                "locomotion.output",
                CharacterBehaviorSourceKind.Locomotion,
                CharacterBehaviorEvaluationPass.OutputPass,
                step,
                0);

            return new BehaviorOutputSubmission(
                source,
                decisionFrame,
                stateDecision,
                locomotionFrame,
                stateFrame,
                ActionMotionResolveResult.None(step),
                CharacterFrameActionOutputSubmission.None(step),
                CharacterFrameArbitrationInput.None(step),
                LocomotionPreemptionFact.None,
                true);
        }

        static BehaviorOutputSubmission CreateCommittedActionOutput(int step, bool required)
        {
            ActionMotionSpec motionSpec = new ActionMotionSpec(
                ActionStateIds.Dodge,
                CharacterStateIds.Dodge,
                CharacterStateVariant.Directional,
                0.35f,
                4f,
                true,
                true,
                Vector3.forward,
                0.1f,
                step);
            ActionMotionResolveResult motionResult = new ActionMotionResolveResult(
                motionSpec,
                default,
                true,
                false,
                true,
                step,
                string.Empty);
            ActionTimelineOutcome timelineOutcome = new ActionTimelineOutcome(
                1,
                step,
                ActionAnimationKeys.DodgeDirectional,
                true,
                motionSpec,
                true,
                new[] { "window.action.dodge" },
                new[] { new ActionCueRequest("cue.dodge.flash", 1, step) });
            CommittedActionBranchOutcome branchOutcome = new CommittedActionBranchOutcome(
                timelineOutcome,
                CharacterFrameCandidateOutput.CommittedAction(true, true, step),
                BodyOccupancyClaim.CommittedActionFullBody(step),
                step,
                new CommittedActionNodeId("timeline.dodge.directional"),
                string.Empty);
            CharacterStateAnimationRequest animationRequest = new CharacterStateAnimationRequest(
                CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeDirectional.Value, "Dodge Directional"),
                step,
                new ActionAnimationPlaybackIntent(step));
            CharacterFrameActionOutputSubmission actionOutput = new CharacterFrameActionOutputSubmission(
                animationRequest,
                true,
                true,
                InputRequestKind.Dodge,
                false,
                step,
                branchOutcome);
            CharacterFrameArbitrationInput arbitrationInput = new CharacterFrameArbitrationInput(
                BodyOccupancyClaim.CommittedActionFullBody(step),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.Locomotion, step),
                CharacterFrameCandidateOutput.CommittedAction(true, true, step),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.UpperBody, step),
                step);
            CharacterBehaviorSubmissionSource source = CharacterBehaviorSubmissionSource.Create(
                "committed-action.output",
                CharacterBehaviorSourceKind.CommittedAction,
                CharacterBehaviorEvaluationPass.OutputPass,
                step,
                1);

            return new BehaviorOutputSubmission(
                source,
                default,
                default,
                default,
                default,
                motionResult,
                actionOutput,
                arbitrationInput,
                LocomotionPreemptionFact.None,
                required);
        }

        sealed class RecordingLeaf : ICharacterBehaviorSubmissionLeaf
        {
            readonly List<string> calls;
            readonly bool requestSuccess;
            readonly bool outputSuccess;

            public RecordingLeaf(
                CharacterBehaviorSourceKind sourceKind,
                List<string> calls,
                bool requestSuccess,
                bool outputSuccess)
            {
                SourceKind = sourceKind;
                this.calls = calls;
                this.requestSuccess = requestSuccess;
                this.outputSuccess = outputSuccess;
            }

            public CharacterBehaviorSourceKind SourceKind { get; }

            public bool TryRunRequestPass(
                ICharacterFrameRuntimePort runtime,
                ref CharacterFrameContext context,
                CharacterBehaviorSubmissionSet submissions,
                CharacterBehaviorSubmissionTrace trace)
            {
                calls.Add(SourceKind == CharacterBehaviorSourceKind.Locomotion
                    ? "locomotion-request"
                    : "action-request");
                trace.Add(CharacterBehaviorEvaluationPass.RequestPass, SourceKind);
                return requestSuccess;
            }

            public bool TryRunOutputPass(
                ICharacterFrameRuntimePort runtime,
                ref CharacterFrameContext context,
                CharacterBehaviorSubmissionSet submissions,
                CharacterBehaviorSubmissionTrace trace)
            {
                calls.Add(SourceKind == CharacterBehaviorSourceKind.Locomotion
                    ? "locomotion-output"
                    : "action-output");
                trace.Add(CharacterBehaviorEvaluationPass.OutputPass, SourceKind);
                return outputSuccess;
            }
        }
    }
}
