using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonAnimation;
using ThirdPersonCamera;
using ThirdPersonCharacterBehavior;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ThirdPersonSimulation.Tests
{
    public sealed class CharacterFrameRollbackReplayTests
    {
        [Test]
        public void PredictionInputFrameReplayWritesPressedRequestsToInputBuffer()
        {
            GameObject gameObject = new GameObject("rollback-input-buffer-replay-test");
            gameObject.SetActive(false);

            try
            {
                InputRequestBufferComponent buffer = gameObject.AddComponent<InputRequestBufferComponent>();
                PredictionInputFrame frame = new PredictionInputFrame(
                    new SimulationTick(12),
                    Vector2.zero,
                    Vector2.zero,
                    false,
                    new PredictionButtonFrame(true, true, false),
                    new PredictionButtonFrame(false, true, false),
                    new PredictionButtonFrame(false, false, true),
                    new PredictionButtonFrame(true, true, false));

                PredictionInputFrameInputBufferReplay.WriteToInputBuffer(in frame, buffer);

                Assert.AreEqual(12, buffer.CurrentStep);
                Assert.AreEqual(2, buffer.Buffer.Count);
                Assert.True(buffer.Buffer.TryPeek(InputRequestKind.Dodge, 12, out BufferedInputRequest dodge));
                Assert.True(buffer.Buffer.TryPeek(InputRequestKind.Interact, 12, out BufferedInputRequest interact));
                Assert.False(buffer.Buffer.TryPeek(InputRequestKind.Attack, 12, out _));
                Assert.False(buffer.Buffer.TryPeek(InputRequestKind.Jump, 12, out _));
                Assert.AreEqual(12, dodge.OriginStep);
                Assert.AreEqual(12, interact.OriginStep);

                buffer.Clear();
                PredictionInputFrame pressedActions = new PredictionInputFrame(
                    new SimulationTick(13),
                    Vector2.zero,
                    Vector2.zero,
                    false,
                    new PredictionButtonFrame(false, true, false),
                    new PredictionButtonFrame(true, true, false),
                    new PredictionButtonFrame(true, true, false),
                    new PredictionButtonFrame(true, true, false));

                PredictionInputFrameInputBufferReplay.WriteToInputBuffer(in pressedActions, buffer);

                Assert.AreEqual(13, buffer.CurrentStep);
                Assert.AreEqual(3, buffer.Buffer.Count);
                Assert.False(buffer.Buffer.TryPeek(InputRequestKind.Dodge, 13, out _));
                Assert.True(buffer.Buffer.TryPeek(InputRequestKind.Attack, 13, out _));
                Assert.True(buffer.Buffer.TryPeek(InputRequestKind.Jump, 13, out _));
                Assert.True(buffer.Buffer.TryPeek(InputRequestKind.Interact, 13, out _));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PredictionInputFrameReplayTrimsExpiredRequestsBeforeTick()
        {
            GameObject gameObject = new GameObject("rollback-input-buffer-expiry-test");
            gameObject.SetActive(false);

            try
            {
                InputRequestBufferComponent buffer = gameObject.AddComponent<InputRequestBufferComponent>();
                buffer.SetStep(1);
                buffer.Buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 1, 0);

                PredictionInputFrame frame = Input(3, Vector2.zero);
                PredictionInputFrameInputBufferReplay.WriteToInputBuffer(in frame, buffer);

                Assert.AreEqual(3, buffer.CurrentStep);
                Assert.AreEqual(0, buffer.Buffer.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void InputRequestBufferComponentRestoreKeepsCurrentStepAndConsumedRequests()
        {
            GameObject gameObject = new GameObject("rollback-input-buffer-restore-test");
            gameObject.SetActive(false);

            try
            {
                InputRequestBufferComponent buffer = gameObject.AddComponent<InputRequestBufferComponent>();
                buffer.SetStep(2);
                buffer.AddButtonState(InputButtonKind.Dodge, new InputButtonState(true, true, false));
                buffer.AddButtonState(InputButtonKind.Attack, new InputButtonState(true, true, false));
                Assert.True(buffer.Buffer.TryConsume(InputRequestKind.Dodge, 2, out _));

                InputRequestBufferComponentRestoreState restoreState = buffer.CaptureRestoreState();
                buffer.Clear();
                buffer.SetStep(20);

                buffer.Restore(in restoreState);

                Assert.AreEqual(2, buffer.CurrentStep);
                Assert.AreEqual(2, buffer.Buffer.Count);
                Assert.False(buffer.Buffer.TryPeek(InputRequestKind.Dodge, 2, out _));
                Assert.True(buffer.Buffer.TryPeek(InputRequestKind.Attack, 2, out _));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void InputRequestBufferComponentRestoreTrimsExpiredRequests()
        {
            InputRequestBufferRestoreState bufferState = new InputRequestBufferRestoreState(new[]
            {
                new BufferedInputRequestRestoreState(InputRequestKind.Dodge, InputButtonKind.Dodge, 1, 2, false)
            });
            InputRequestBufferComponentRestoreState restoreState = new InputRequestBufferComponentRestoreState(5, bufferState);
            GameObject gameObject = new GameObject("rollback-input-buffer-restore-expired-test");
            gameObject.SetActive(false);

            try
            {
                InputRequestBufferComponent buffer = gameObject.AddComponent<InputRequestBufferComponent>();

                buffer.Restore(in restoreState);

                Assert.AreEqual(5, buffer.CurrentStep);
                Assert.AreEqual(0, buffer.Buffer.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CharacterFrameRollbackSimulationReplaysMoveRunAndDodgeToSameSnapshot()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            PredictionInputHistory inputHistory = new PredictionInputHistory(16);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(16);

            CharacterSimulationSnapshot initial = fixture.Simulation.CaptureSnapshot(SimulationTick.Zero);
            snapshotHistory.Write(in initial);

            PredictionInputFrame[] frames =
            {
                Input(1, Vector2.up),
                Input(2, Vector2.up, true),
                Input(3, Vector2.up, true, true),
                Input(4, Vector2.up, true),
                Input(5, Vector2.up, true),
                Input(6, Vector2.up, true)
            };

            for (int i = 0; i < frames.Length; i++)
            {
                PredictionInputFrame frame = frames[i];
                inputHistory.Write(frame);
                fixture.Simulation.Advance(in frame);
                CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(frame.Tick);
                snapshotHistory.Write(in snapshot);
            }

            LocalRollbackSynctestRunner runner = new LocalRollbackSynctestRunner(inputHistory, snapshotHistory, fixture.Simulation);
            LocalRollbackSynctestResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(6),
                SimulationTick.Zero,
                CharacterSimulationSnapshotTolerance.Default);

            AssertSynctestSuccess(in result);
        }
        [Test]
        public void CharacterFrameRollbackSimulationAdvanceUsesCharacterFrameRuntimeController()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            PredictionInputFrame frame = Input(7, Vector2.up, true, true);

            fixture.Simulation.Advance(in frame);
            CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(frame.Tick);

            CharacterStateId activeState = fixture.RuntimeController.CommittedActionModule.CurrentStateSnapshot.ActiveState;
            Assert.That(activeState.Value, Does.StartWith("Locomotion."));
            Assert.AreEqual(snapshot.CommittedActionRestoreState.Gameplay.Snapshot.ActiveState, activeState);
            Assert.True(snapshot.CommittedActionRestoreState.Gameplay.ActionLifecycle.HasActiveAction);
            Assert.AreEqual(ActionStateIds.Dodge, snapshot.CommittedActionRestoreState.Gameplay.ActionLifecycle.ActiveAction.MotionSpec.ActionState);
            Assert.AreEqual(7, snapshot.RuntimeBlackboard.Action.SourceStep);
            Assert.True(fixture.ActionPresenter.PresentCount > 0);
        }

        [Test]
        public void CharacterFramePipelineWritesBufferedInputBeforeGameplayDecision()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            CharacterFramePipelineHost pipeline = CreateFramePipelineHost();
            PredictionInputFrame frame = Input(31, Vector2.up, true, true);
            CharacterFrameInput input = CharacterFrameInput.FromPredictionInputFrame(
                in frame,
                SimulationTickRate.Default.FixedDeltaSecondsFloat);
            CharacterFrameContext context = pipeline.BeginFrame(in input);

            Assert.False(fixture.InputBuffer.Buffer.TryPeek(InputRequestKind.Dodge, 31, out _));

            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.ReadInput, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.UpdateInputBuffer, ref context, out _);

            Assert.True(fixture.InputBuffer.Buffer.TryPeek(InputRequestKind.Dodge, 31, out _));

            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.GameplayDecision, ref context, out _);

            Assert.True(context.InputRequest.HasRequest);
            Assert.AreEqual(InputRequestKind.Dodge, context.InputRequest.RequestKind);
            Assert.True(context.RequestSubmissions.HasAny);
            Assert.AreEqual(ActionStateIds.Dodge, context.ResolvedAction.MotionSpec.ActionState);
            Assert.AreNotEqual(CharacterStateIds.Dodge, context.StateFrame.Snapshot.ActiveState);
        }

        [Test]
        public void CharacterFramePipelineRunsThroughRuntimePortOrder()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            CharacterFramePipelineHost pipeline = CreateFramePipelineHost();
            RecordingCharacterFrameRuntimePort runtime = new RecordingCharacterFrameRuntimePort(fixture.RuntimeController.RuntimePort);
            PredictionInputFrame frame = Input(37, Vector2.up, true, true);
            CharacterFrameInput input = CharacterFrameInput.FromPredictionInputFrame(
                in frame,
                SimulationTickRate.Default.FixedDeltaSecondsFloat);

            bool success = pipeline.Tick(runtime, in input, out CharacterFrameResult result);

            Assert.True(success);
            Assert.True(result.Success);
            Assert.Less(runtime.IndexOf("WriteBufferedInputFacts"), runtime.IndexOf("PrepareFrameRuntimeAdapters"));
            Assert.Less(runtime.IndexOf("PrepareFrameRuntimeAdapters"), runtime.IndexOf("SetLastFrameOutputs"));
            Assert.Less(runtime.IndexOf("SetLastFrameOutputs"), runtime.IndexOf("ExecuteFrameMotion"));
            Assert.Less(runtime.IndexOf("ExecuteFrameMotion"), runtime.IndexOf("PresentFrameAnimation"));
            Assert.Less(runtime.IndexOf("PresentFrameAnimation"), runtime.IndexOf("UpdateStateSnapshot"));
            Assert.Less(runtime.IndexOf("UpdateStateSnapshot"), runtime.IndexOf("LogDiagnosticTickSnapshots"));
        }

        [Test]
        public void CharacterFramePipelineRuntimePortKeepsMotionAndPresentationSeparate()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            CharacterFramePipelineHost pipeline = CreateFramePipelineHost();
            RecordingCharacterFrameRuntimePort runtime = new RecordingCharacterFrameRuntimePort(fixture.RuntimeController.RuntimePort);
            PredictionInputFrame frame = Input(38, Vector2.up, true, true);
            CharacterFrameInput input = CharacterFrameInput.FromPredictionInputFrame(
                in frame,
                SimulationTickRate.Default.FixedDeltaSecondsFloat);
            CharacterFrameContext context = pipeline.BeginFrame(in input);

            pipeline.RunPhase(runtime, SimulationTickPhase.ReadInput, ref context, out _);
            pipeline.RunPhase(runtime, SimulationTickPhase.UpdateInputBuffer, ref context, out _);
            pipeline.RunPhase(runtime, SimulationTickPhase.GameplayDecision, ref context, out _);
            pipeline.RunPhase(runtime, SimulationTickPhase.BuildMotion, ref context, out _);

            Assert.AreEqual(-1, runtime.IndexOf("ExecuteFrameMotion"));
            Assert.AreEqual(-1, runtime.IndexOf("PresentFrameAnimation"));

            pipeline.RunPhase(runtime, SimulationTickPhase.ExecuteMotion, ref context, out _);

            Assert.That(runtime.IndexOf("ExecuteFrameMotion"), Is.GreaterThanOrEqualTo(0));
            Assert.AreEqual(-1, runtime.IndexOf("PresentFrameAnimation"));

            pipeline.RunPhase(runtime, SimulationTickPhase.PresentationBridge, ref context, out _);

            Assert.That(runtime.IndexOf("PresentFrameAnimation"), Is.GreaterThan(runtime.IndexOf("ExecuteFrameMotion")));
            Assert.That(runtime.IndexOf("WriteStateFrameActionFacts"), Is.GreaterThan(runtime.IndexOf("PresentFrameAnimation")));
            Assert.That(runtime.IndexOf("UpdateStateSnapshot"), Is.GreaterThan(runtime.IndexOf("WriteStateFrameActionFacts")));
        }

        [Test]
        public void CharacterFramePipelineUsesSameCurrentTimelineFactsForSubmissionArbiterAndRunner()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            CharacterFramePipelineHost pipeline = CreateFramePipelineHost();
            PredictionInputFrame frame = Input(34, Vector2.up, true, true);
            CharacterFrameInput input = CharacterFrameInput.FromPredictionInputFrame(
                in frame,
                SimulationTickRate.Default.FixedDeltaSecondsFloat);
            CharacterFrameContext context = pipeline.BeginFrame(in input);

            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.ReadInput, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.UpdateInputBuffer, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.GameplayDecision, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.BuildMotion, ref context, out _);

            Assert.AreEqual(StateTimelineFactsSource.Current, context.CurrentTimelineFactsTrace.Source);
            Assert.AreEqual(context.Step, context.CurrentTimelineFactsTrace.SourceStep);
            Assert.AreEqual(
                context.CurrentTimelineFactsTrace.FactsId,
                context.StateFrame.CurrentTimelineFactsTrace.FactsId);
            Assert.AreEqual(
                context.CurrentTimelineFactsTrace.SourceStep,
                context.StateFrame.CurrentTimelineFactsTrace.SourceStep);
            Assert.AreEqual(
                context.CurrentTimelineFactsTrace.FactsId,
                context.FrameSubmission.CurrentTimelineFactsTrace.FactsId);
            Assert.AreEqual(StateTimelineFactsSource.Projected, context.StateFrame.ProjectedTimelineFactsTrace.Source);
        }

        [Test]
        public void CharacterFrameRollbackSimulationReplaysTimelineFactsSequenceDeterministically()
        {
            PredictionInputFrame[] inputs =
            {
                Input(1, Vector2.up),
                Input(2, Vector2.up, true),
                Input(3, Vector2.up, true, true),
                Input(4, Vector2.down, true),
                Input(5, Vector2.down, true)
            };

            TimelineTraceFrame[] first = RunTimelineTraceSequence(inputs);
            TimelineTraceFrame[] second = RunTimelineTraceSequence(inputs);

            Assert.AreEqual(first.Length, second.Length);
            Assert.That(first.Any(frame => frame.Current.Source == StateTimelineFactsSource.Current), Is.True);
            Assert.That(first.Any(frame => frame.Projected.Source == StateTimelineFactsSource.Projected), Is.True);
            Assert.That(first.Any(frame => frame.Target.Source == StateTimelineFactsSource.Target), Is.True);

            for (int i = 0; i < first.Length; i++)
                AssertTimelineTraceFrameEqual(first[i], second[i], i);
        }

        [Test]
        public void CharacterFramePipelineExecutesMotionOnlyInExecuteMotionPhase()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            CharacterFramePipelineHost pipeline = CreateFramePipelineHost();
            CharacterFrameInput input = CharacterFrameInput.FromLocomotionInput(
                32,
                new BasicLocomotionInputSnapshot(
                    SimulationTickRate.Default.FixedDeltaSecondsFloat,
                    Vector2.up,
                    Vector2.zero));
            CharacterFrameContext context = pipeline.BeginFrame(in input);

            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.ReadInput, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.UpdateInputBuffer, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.GameplayDecision, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.BuildMotion, ref context, out _);

            Assert.AreEqual(0, fixture.Driver.TotalMoveCount);

            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.ExecuteMotion, ref context, out _);

            Assert.AreEqual(1, fixture.Driver.TotalMoveCount);

            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.PresentationBridge, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.WriteSnapshotAndEvents, ref context, out _);

            Assert.AreEqual(1, fixture.Driver.TotalMoveCount);
        }

        [Test]
        public void CharacterFramePipelineResolvesActionMotionSpecBeforeExecution()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            CharacterFramePipelineHost pipeline = CreateFramePipelineHost();
            PredictionInputFrame frame = Input(35, Vector2.up, true, true);
            CharacterFrameInput input = CharacterFrameInput.FromPredictionInputFrame(
                in frame,
                SimulationTickRate.Default.FixedDeltaSecondsFloat);
            CharacterFrameContext context = pipeline.BeginFrame(in input);

            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.ReadInput, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.UpdateInputBuffer, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.GameplayDecision, ref context, out _);

            Assert.True(context.ResolvedAction.MotionSpec.HasSpec);
            Assert.False(context.StateFrame.ActionMotionSpec.HasSpec);
            Assert.False(context.StateFrame.HasActionMovement);

            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.BuildMotion, ref context, out CharacterFrameResult buildResult);

            Assert.True(context.ActionMotionResult.HasActionMovement);
            Assert.True(context.FrameSubmission.HasFrameOutput);
            Assert.AreEqual(context.ActionMotionResult.MovementCommand.PlanarDistance, buildResult.ActionMotionResult.MovementCommand.PlanarDistance, 0.0001f);
            Assert.AreEqual(0, fixture.Driver.TotalMoveCount);

            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.ExecuteMotion, ref context, out _);

            Assert.AreEqual(1, fixture.Driver.ActionMoveCount);
            Assert.AreEqual(context.ActionMotionResult.MovementCommand.WorldDirection, fixture.Driver.LastWorldDirection);
        }

        [Test]
        public void CharacterFramePipelineWritesActionFactsFromResolverResult()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            CharacterFramePipelineHost pipeline = CreateFramePipelineHost();
            PredictionInputFrame frame = Input(36, Vector2.up, true, true);
            CharacterFrameInput input = CharacterFrameInput.FromPredictionInputFrame(
                in frame,
                SimulationTickRate.Default.FixedDeltaSecondsFloat);
            CharacterFrameContext context = pipeline.BeginFrame(in input);

            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.ReadInput, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.UpdateInputBuffer, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.GameplayDecision, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.BuildMotion, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.ExecuteMotion, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.PresentationBridge, ref context, out _);

            CharacterRuntimeActionFacts facts = fixture.RuntimeController.LocomotionModule.RuntimeBlackboardSnapshot.Action;

            Assert.True(facts.Active);
            Assert.AreEqual(context.ActionMotionResult.HasActionMovement, facts.HasMovement);
            Assert.AreEqual(context.ActionMotionResult.ActionCompleted, facts.Completed);
            Assert.AreEqual(context.ActionMotionResult.MovementCommand.PlanarDistance, facts.PlanarDistance, 0.0001f);
            Assert.AreEqual(context.ActionMotionResult.MovementCommand.WorldDirection, facts.WorldDirection);
            Assert.AreEqual(context.ActionMotionResult.SourceStep, facts.SourceStep);
        }

        [Test]
        public void CharacterFramePipelinePresentsAnimationAfterMotionExecution()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            CharacterFramePipelineHost pipeline = CreateFramePipelineHost();
            PredictionInputFrame frame = Input(33, Vector2.up, true, true);
            CharacterFrameInput input = CharacterFrameInput.FromPredictionInputFrame(
                in frame,
                SimulationTickRate.Default.FixedDeltaSecondsFloat);
            CharacterFrameContext context = pipeline.BeginFrame(in input);

            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.ReadInput, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.UpdateInputBuffer, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.GameplayDecision, ref context, out _);
            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.BuildMotion, ref context, out _);

            Assert.AreEqual(0, fixture.ActionPresenter.PresentCount);

            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.ExecuteMotion, ref context, out _);

            Assert.AreEqual(1, fixture.Driver.ActionMoveCount);
            Assert.AreEqual(0, fixture.ActionPresenter.PresentCount);

            pipeline.RunPhase(fixture.RuntimeController.RuntimePort, SimulationTickPhase.PresentationBridge, ref context, out _);

            Assert.AreEqual(1, fixture.ActionPresenter.PresentCount);
            Assert.True(context.AnimationFactsWritten);
        }

        [Test]
        public void CharacterActionRequestSubmissionArbiterLeavesAttackRequestForFutureComboChange()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Attack, InputButtonKind.Attack, 34, 4);
            CharacterActionRequestSubmissionInput input = new CharacterActionRequestSubmissionInput(
                buffer,
                34,
                CharacterStateMachineSnapshot.Inactive,
                new BasicLocomotionInputSnapshot(
                    SimulationTickRate.Default.FixedDeltaSecondsFloat,
                    Vector2.zero,
                    Vector2.zero),
                false,
                default,
                default,
                false,
                CharacterActionCatalog.Empty,
                0,
                Array.Empty<ActionInterruptPolicy>());

            CharacterActionRequestSubmissionResult result = CharacterActionRequestSubmissionArbiter.Evaluate(in input);

            Assert.False(result.Accepted);
            Assert.False(result.Request.HasRequest);
            Assert.True(buffer.TryPeek(InputRequestKind.Attack, 34, out _));
        }

        [Test]
        public void CharacterActionRequestSubmissionArbiterAcceptsExternalRequestThroughSameCollection()
        {
            CharacterInputRequestFact requestFact = new CharacterInputRequestFact(
                true,
                InputRequestKind.Dodge,
                40,
                44,
                50,
                CharacterStateVariant.Directional,
                Vector3.forward);
            ActionInterruptRequest interruptRequest = new ActionInterruptRequest(
                40,
                ActionRequestType.Dodge,
                ActionStateIds.Dodge,
                50,
                0,
                40,
                44);
            ActionInterruptContext interruptContext = new ActionInterruptContext(
                ActionStateIds.None,
                0f,
                0,
                40);
            CharacterFrameExternalRequestSubmission external = new CharacterFrameExternalRequestSubmission(
                new CharacterFrameRequestSubmission(
                    CharacterFrameRequestProviderId.External,
                    requestFact,
                    interruptRequest,
                    interruptContext,
                    0));
            CharacterActionRequestSubmissionInput input = new CharacterActionRequestSubmissionInput(
                new InputRequestBuffer(),
                40,
                CharacterStateMachineSnapshot.Inactive,
                new BasicLocomotionInputSnapshot(
                    SimulationTickRate.Default.FixedDeltaSecondsFloat,
                    Vector2.zero,
                    Vector2.zero),
                false,
                default,
                default,
                false,
                CharacterActionCatalog.Empty,
                0,
                new[] { new ActionInterruptPolicy(ActionStateIds.None, ActionStateIds.Dodge, 0) },
                external);

            CharacterActionRequestSubmissionResult result = CharacterActionRequestSubmissionArbiter.Evaluate(in input);

            Assert.True(result.Accepted);
            Assert.AreEqual(InputRequestKind.Dodge, result.Request.RequestKind);
            Assert.AreEqual(1, result.RequestSubmissions.Count);
            Assert.AreEqual(CharacterFrameRequestProviderId.External, result.RequestSubmissions.First.ProviderId);
        }

        [Test]
        public void CharacterFrameCompatibleTickMatchesPhasePipelineForMove()
        {
            CharacterFrameInput input = CharacterFrameInput.FromLocomotionInput(
                35,
                new BasicLocomotionInputSnapshot(
                    SimulationTickRate.Default.FixedDeltaSecondsFloat,
                    Vector2.up,
                    Vector2.zero));

            AssertCompatibleTickMatchesPhasePipeline(in input);
        }

        [Test]
        public void CharacterFrameCompatibleTickMatchesPhasePipelineForDirectionalDodge()
        {
            PredictionInputFrame frame = Input(36, Vector2.up, true, true);
            CharacterFrameInput input = CharacterFrameInput.FromPredictionInputFrame(
                in frame,
                SimulationTickRate.Default.FixedDeltaSecondsFloat);

            AssertCompatibleTickMatchesPhasePipeline(in input);
        }

        [Test]
        public void CharacterFrameCompatibleTickMatchesPhasePipelineForBackstepDodge()
        {
            PredictionInputFrame frame = Input(37, Vector2.zero, false, true);
            CharacterFrameInput input = CharacterFrameInput.FromPredictionInputFrame(
                in frame,
                SimulationTickRate.Default.FixedDeltaSecondsFloat);

            AssertCompatibleTickMatchesPhasePipeline(in input);
        }

        [Test]
        public void CharacterFrameCompatibleTickWithoutTickDriverExecutesMotionOnce()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            CharacterFrameInput input = CharacterFrameInput.FromLocomotionInput(
                38,
                new BasicLocomotionInputSnapshot(
                    SimulationTickRate.Default.FixedDeltaSecondsFloat,
                    Vector2.up,
                    Vector2.zero));

            Assert.True(fixture.RuntimeController.Tick(in input));

            Assert.AreEqual(1, fixture.Driver.TotalMoveCount);
            Assert.True(fixture.RuntimeController.LastFramePipelineResult.Success);
        }

        [Test]
        public void CharacterFrameRuntimeTickAdapterRegisterDisablesRuntimeControllerAutoUpdate()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            UnitySimulationTickDriver tickDriver = fixture.Root.AddComponent<UnitySimulationTickDriver>();
            CharacterFrameRuntimeTickAdapter adapter = fixture.Root.AddComponent<CharacterFrameRuntimeTickAdapter>();

            fixture.RuntimeController.AutoUpdate = true;
            adapter.TickDriver = tickDriver;
            adapter.RuntimeController = fixture.RuntimeController;

            Assert.True(adapter.Register());
            Assert.False(fixture.RuntimeController.AutoUpdate);

            adapter.Unregister();
            Assert.True(fixture.RuntimeController.AutoUpdate);
        }

        [Test]
        public void CharacterFrameRollbackSimulationRestoreRestoresCharacterStateAndInputBuffer()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            PredictionInputFrame frame = Input(5, Vector2.up, true, true);
            fixture.Simulation.Advance(in frame);
            CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(frame.Tick);

            PredictionInputFrame later = Input(6, Vector2.zero);
            fixture.Simulation.Advance(in later);
            fixture.InputBuffer.Clear();
            fixture.InputBuffer.SetStep(20);

            fixture.Simulation.Restore(in snapshot);

            Assert.AreEqual(5, fixture.InputBuffer.CurrentStep);
            Assert.AreEqual(snapshot.CommittedActionRestoreState.Snapshot.ActiveState, fixture.RuntimeController.CommittedActionModule.CurrentStateSnapshot.ActiveState);
            Assert.AreEqual(snapshot.CommittedActionRestoreState.Snapshot.StateTime, fixture.RuntimeController.CommittedActionModule.CurrentStateSnapshot.StateTime, 0.0001f);
            Assert.False(fixture.InputBuffer.Buffer.TryPeek(InputRequestKind.Dodge, 5, out _));
            Assert.AreEqual(snapshot.InputBufferRestoreState.Buffer.Requests.Count, fixture.InputBuffer.Buffer.Count);
        }

        [Test]
        public void CharacterFrameRollbackSimulationRestoreRestoresActionVariantAndDirection()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            PredictionInputFrame frame = Input(5, Vector2.right, true, true);
            fixture.Simulation.Advance(in frame);
            CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(frame.Tick);
            ActionMotionSpec expectedSpec = snapshot.CommittedActionRestoreState.Gameplay.ActionLifecycle.ActiveAction.MotionSpec;
            Vector3 expectedDirection = expectedSpec.LockedWorldDirection;

            Assert.AreEqual(CharacterStateVariant.Directional, expectedSpec.Variant);
            Assert.AreEqual(Vector3.right, expectedDirection);

            CommittedActionRestoreState divergent = CreateActionRestoreState(
                CharacterStateVariant.Backstep,
                Vector3.back,
                0.5f);
            Assert.True(fixture.RuntimeController.CommittedActionModule.Restore(in divergent));
            CommittedActionRestoreState divergentCaptured = fixture.RuntimeController.CommittedActionModule.CaptureRestoreState();
            Assert.AreEqual(CharacterStateVariant.Backstep, divergentCaptured.Gameplay.ActionLifecycle.ActiveAction.MotionSpec.Variant);

            fixture.Simulation.Restore(in snapshot);
            CommittedActionRestoreState restored = fixture.RuntimeController.CommittedActionModule.CaptureRestoreState();
            ActionMotionSpec restoredSpec = restored.Gameplay.ActionLifecycle.ActiveAction.MotionSpec;

            Assert.AreEqual(CharacterStateVariant.Directional, restoredSpec.Variant);
            Assert.AreEqual(expectedDirection, restoredSpec.LockedWorldDirection);
        }

        [Test]
        public void CharacterFrameRollbackSimulationRestoreRestoresActionAnimationPlayback()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            PredictionInputFrame frame = Input(5, Vector2.up, true, true);
            fixture.Simulation.Advance(in frame);
            CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(frame.Tick);

            fixture.ActionPresenter.Clear();

            fixture.Simulation.Restore(in snapshot);

            Assert.AreEqual(snapshot.RuntimeBlackboard.Animation.ActionKey, fixture.ActionPresenter.CurrentKey);
            Assert.AreEqual(snapshot.RuntimeBlackboard.Animation.ActionNormalizedTime, fixture.ActionPresenter.CurrentNormalizedTime, 0.0001f);
            Assert.AreEqual(snapshot.RuntimeBlackboard.Animation.ActionHasValidPlayback, fixture.ActionPresenter.HasValidPlayback);
        }

        [Test]
        public void CharacterFrameRollbackSimulationRestorePassesLocomotionGaitToPlayback()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            ManualLocomotionPlaybackProgress playback = new ManualLocomotionPlaybackProgress(
                BasicMovementPhase.TurnBack,
                TurnBackMotionPolicy.DefaultAliasKey,
                0.9f);
            fixture.LocomotionPresenter.SetPlayback(playback);
            CharacterSimulationSnapshot snapshot = CreateTurnBackSnapshot(new SimulationTick(12), 0.35f);

            fixture.Simulation.Restore(in snapshot);

            Assert.AreEqual(BasicMovementGait.Run, playback.LastRestoreGait);
            Assert.AreEqual(0.35f, playback.CurrentPlaybackProgress.NormalizedTime, 0.0001f);
            Assert.AreEqual(BasicMovementPhase.TurnBack, playback.CurrentPlaybackProgress.Phase);
            Assert.AreEqual(TurnBackMotionPolicy.DefaultAliasKey, playback.CurrentPlaybackProgress.AliasKey);
        }

        [Test]
        public void CharacterFrameRollbackSimulationRestoreKeepsTurnBackPreviousMotionPlaybackWindowForReplay()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            StepLocomotionPlaybackProgress playback = new StepLocomotionPlaybackProgress(
                BasicMovementPhase.TurnBack,
                TurnBackMotionPolicy.DefaultAliasKey,
                0.9f,
                0.05f);
            fixture.LocomotionPresenter.SetPlayback(playback);
            CharacterSimulationSnapshot snapshot = CreateTurnBackSnapshot(new SimulationTick(12), 0.35f)
                .WithLocomotionRuntimeState(CreatePreviousMotionPlaybackRuntime(
                    BasicMovementPhase.TurnBack,
                    TurnBackMotionPolicy.DefaultAliasKey,
                    0.25f,
                    true));

            fixture.Simulation.Restore(in snapshot);
            LocomotionRuntimeRollbackState restoredState = fixture.RuntimeController.LocomotionModule.CaptureRollbackState();

            Assert.True(restoredState.HasPreviousMotionPlaybackProgress);
            Assert.AreEqual(0.25f, restoredState.PreviousMotionPlaybackProgress.NormalizedTime, 0.0001f);
            Assert.AreEqual(0.35f, playback.CurrentPlaybackProgress.NormalizedTime, 0.0001f);

            fixture.Simulation.Advance(Input(13, Vector2.down, true));
            CharacterSimulationSnapshot replayed = fixture.Simulation.CaptureSnapshot(new SimulationTick(13));

            Assert.AreEqual(0.4f, replayed.RuntimeBlackboard.Animation.LocomotionProgress.NormalizedTime, 0.0001f);
            Assert.True(replayed.LocomotionRuntimeState.HasPreviousMotionPlaybackProgress);
            Assert.AreEqual(0.4f, replayed.LocomotionRuntimeState.PreviousMotionPlaybackProgress.NormalizedTime, 0.0001f);
        }

        [Test]
        public void CharacterFrameRollbackSimulationRestoreKeepsMissingPreviousMotionPlaybackWindowMissing()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            StepLocomotionPlaybackProgress playback = new StepLocomotionPlaybackProgress(
                BasicMovementPhase.TurnBack,
                TurnBackMotionPolicy.DefaultAliasKey,
                0.9f,
                0.05f);
            fixture.LocomotionPresenter.SetPlayback(playback);
            CharacterSimulationSnapshot snapshot = CreateTurnBackSnapshot(new SimulationTick(12), 0.35f)
                .WithLocomotionRuntimeState(CreatePreviousMotionPlaybackRuntime(
                    BasicMovementPhase.TurnBack,
                    TurnBackMotionPolicy.DefaultAliasKey,
                    0f,
                    false));

            fixture.Simulation.Restore(in snapshot);
            LocomotionRuntimeRollbackState restoredState = fixture.RuntimeController.LocomotionModule.CaptureRollbackState();

            Assert.False(restoredState.HasPreviousMotionPlaybackProgress);
            Assert.False(restoredState.PreviousMotionPlaybackProgress.HasValidPlayback);
            Assert.AreEqual(0.35f, playback.CurrentPlaybackProgress.NormalizedTime, 0.0001f);
        }

        [Test]
        public void MotionExecutorRollbackStateRoundTripsThroughLocomotionSnapshot()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            MotionExecutorRollbackState initial = new MotionExecutorRollbackState(
                5f,
                Vector3.right,
                0f,
                new Vector3(2f, 0f, 3f),
                45f,
                true);

            fixture.Driver.RestoreRollbackState(in initial);
            CharacterSimulationSnapshot snapshot = fixture.RuntimeController.CaptureSimulationSnapshot(new SimulationTick(4));
            fixture.Driver.RestoreRollbackState(MotionExecutorRollbackState.Empty);

            Assert.True(fixture.RuntimeController.RestoreSimulationSnapshot(in snapshot));
            MotionExecutorRollbackState restored = fixture.Driver.CaptureRollbackState();

            Assert.AreEqual(5f, restored.CurrentSpeed, 0.0001f);
            Assert.AreEqual(Vector3.right, restored.LastWorldDirection);
            Assert.AreEqual(initial.RootPosition, restored.RootPosition);
            Assert.AreEqual(initial.RootYaw, restored.RootYaw, 0.0001f);
        }

        [Test]
        public void SnapshotComparerReportsMotionRootPoseDifferences()
        {
            CharacterSimulationSnapshot expected = SnapshotWithMotionRoot(new Vector3(1f, 0f, 2f), 45f);
            CharacterSimulationSnapshot actual = SnapshotWithMotionRoot(new Vector3(1.5f, 0f, 2f), 60f);

            CharacterSimulationSnapshotComparison comparison = CharacterSimulationSnapshotComparer.Compare(
                in expected,
                in actual,
                CharacterSimulationSnapshotTolerance.Default);

            Assert.False(comparison.Matches);
            CollectionAssert.Contains(comparison.Differences, "motionExecutor.rootPosition");
            CollectionAssert.Contains(comparison.Differences, "motionExecutor.rootYaw");
        }

        [Test]
        public void DebugRunnerAcceptsCharacterFrameRollbackSimulationBehaviour()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            GameObject debugRig = new GameObject("rollback-debug-rig-test");
            try
            {
                PredictionInputHistoryTickRecorder inputRecorder = debugRig.AddComponent<PredictionInputHistoryTickRecorder>();
                LocomotionSnapshotHistoryRecorder snapshotRecorder = debugRig.AddComponent<LocomotionSnapshotHistoryRecorder>();
                LocalRollbackSynctestDebugRunner debugRunner = debugRig.AddComponent<LocalRollbackSynctestDebugRunner>();

                debugRunner.InputRecorder = inputRecorder;
                debugRunner.SnapshotRecorder = snapshotRecorder;
                debugRunner.SimulationBehaviour = fixture.Simulation;
                debugRunner.RunOnKeyDown = false;

                Assert.AreNotSame(fixture.Root, debugRig);
                Assert.AreSame(fixture.Simulation, debugRunner.Simulation);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(debugRig);
            }
        }

        [Test]
        public void CameraBasisYawDefaultIsZero()
        {
            CharacterSimulationSnapshot snapshot = new CharacterSimulationSnapshot(
                SimulationTick.Zero,
                Vector3.zero,
                0f,
                new CharacterStateMachineRestoreState(
                    CharacterStateMachineSnapshot.Inactive,
                    Vector3.zero, false),
                false,
                BasicMovementGait.Walk,
                Vector3.zero,
                BasicMovementPhase.Idle,
                BasicMovementGait.Walk,
                string.Empty,
                0f);

            Assert.AreEqual(0f, snapshot.CameraBasisState.Yaw, 0.0001f);
        }

        [Test]
        public void WithCameraBasisRoundTrip()
        {
            CharacterSimulationSnapshot snapshot = new CharacterSimulationSnapshot(
                SimulationTick.Zero,
                Vector3.zero,
                0f,
                new CharacterStateMachineRestoreState(
                    CharacterStateMachineSnapshot.Inactive,
                    Vector3.zero, false),
                false,
                BasicMovementGait.Walk,
                Vector3.zero,
                BasicMovementPhase.Idle,
                BasicMovementGait.Walk,
                string.Empty,
                0f);

            RollbackCameraBasisState basis = new RollbackCameraBasisState(
                Quaternion.Euler(0f, 45f, 0f) * Vector3.forward,
                Quaternion.Euler(0f, 45f, 0f) * Vector3.right,
                45f);
            CharacterSimulationSnapshot withCam = snapshot.WithCameraBasis(in basis);
            Assert.AreEqual(45f, withCam.CameraBasisState.Yaw, 0.0001f);
            Assert.AreEqual(snapshot.Position, withCam.Position);
            Assert.AreEqual(snapshot.Yaw, withCam.Yaw);
        }

        [Test]
        public void SnapshotPreservesRollbackCameraBasisAndLocomotionRuntimeState()
        {
            CharacterSimulationSnapshot snapshot = new CharacterSimulationSnapshot(
                SimulationTick.Zero,
                Vector3.zero,
                0f,
                new CharacterStateMachineRestoreState(
                    CharacterStateMachineSnapshot.Inactive,
                    Vector3.zero, false),
                true,
                BasicMovementGait.Run,
                Vector3.forward,
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                "RunLoop",
                0.5f);
            RollbackCameraBasisState basis = new RollbackCameraBasisState(Vector3.left, Vector3.back, 270f);
            LocomotionRuntimeRollbackState runtime = new LocomotionRuntimeRollbackState(
                MovementInputIntent.FromRaw(Vector2.up, 0f, true),
                Vector3.right,
                new AnimationPhasePlaybackProgress(BasicMovementPhase.MoveLoop, "RunLoop", 0.25f, true, false),
                true,
                true,
                BasicMovementGait.Run,
                new LocomotionTurnBackIntent(true, 10, 12, 180f, 120f, Vector3.back, Vector3.forward));
            MotionExecutorRollbackState motionState = new MotionExecutorRollbackState(3.5f, Vector3.right, -2f);

            CharacterSimulationSnapshot enriched = snapshot
                .WithCameraBasis(in basis)
                .WithLocomotionRuntimeState(in runtime)
                .WithMotionExecutorState(in motionState)
                .WithCommittedActionState(CommittedActionRestoreState.Inactive, InputRequestBufferComponentRestoreState.Empty);

            Assert.AreEqual(270f, enriched.CameraBasisState.Yaw, 0.0001f);
            Assert.AreEqual(Vector3.left, enriched.CameraBasisState.PlanarForward);
            Assert.True(enriched.LocomotionRuntimeState.HasPreviousMotionPlaybackProgress);
            Assert.AreEqual(0.25f, enriched.LocomotionRuntimeState.PreviousMotionPlaybackProgress.NormalizedTime, 0.0001f);
            Assert.AreEqual(BasicMovementGait.Run, enriched.LocomotionRuntimeState.ActiveMoveStopGait);
            Assert.True(enriched.LocomotionRuntimeState.PendingTurnBackIntent.IsValidAt(11));
            Assert.AreEqual(3.5f, enriched.MotionExecutorState.CurrentSpeed, 0.0001f);
            Assert.AreEqual(Vector3.right, enriched.MotionExecutorState.LastWorldDirection);
            Assert.AreEqual(-2f, enriched.MotionExecutorState.VerticalVelocity, 0.0001f);
        }

        [Test]
        public void WithCommittedActionStatePreservesCameraBasis()
        {
            RollbackCameraBasisState basis = new RollbackCameraBasisState(Vector3.right, Vector3.back, 120f);
            CharacterSimulationSnapshot snapshot = new CharacterSimulationSnapshot(
                SimulationTick.Zero,
                Vector3.zero,
                0f,
                new CharacterStateMachineRestoreState(
                    CharacterStateMachineSnapshot.Inactive,
                    Vector3.zero, false),
                false,
                BasicMovementGait.Walk,
                Vector3.zero,
                BasicMovementPhase.Idle,
                BasicMovementGait.Walk,
                string.Empty,
                0f)
                .WithCameraBasis(in basis);

            CharacterSimulationSnapshot enriched = snapshot.WithCommittedActionState(
                CommittedActionRestoreState.Inactive,
                InputRequestBufferComponentRestoreState.Empty);

            Assert.AreEqual(120f, enriched.CameraBasisState.Yaw, 0.0001f);
        }

        [Test]
        public void CharacterFrameRollbackSimulationRestoresRollbackCameraBasisWithoutMutatingCameraController()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            fixture.Camera.ResetState(45f, 0f);
            fixture.Simulation.Advance(Input(1, Vector2.up));

            CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(new SimulationTick(1));

            Assert.AreEqual(45f, snapshot.CameraBasisState.Yaw, 0.0001f);
            Assert.True(snapshot.CommittedActionRestoreState.Snapshot.ActiveState.IsValid);

            fixture.Camera.ResetState(180f, 0f);
            fixture.Simulation.Advance(Input(2, Vector2.zero));

            fixture.Simulation.Restore(in snapshot);
            CharacterSimulationSnapshot restored = fixture.Simulation.CaptureSnapshot(new SimulationTick(1));

            Assert.AreEqual(snapshot.CameraBasisState.Yaw, restored.CameraBasisState.Yaw, 0.0001f);
            Assert.AreEqual(snapshot.Position, restored.Position);
            Assert.AreEqual(180f, fixture.Camera.Yaw, 0.0001f);
            AssertPlanarForward(45f, fixture.RuntimeController.RollbackCameraBasisProvider.CameraPlanarForward);
        }

        [Test]
        public void CharacterFrameRollbackSimulationResolvesCameraBasisWhenReferenceIsMissing()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            fixture.RuntimeController.RollbackCameraBasisProvider.Override(
                new RollbackCameraBasisState(
                    Quaternion.Euler(0f, 280f, 0f) * Vector3.forward,
                    Quaternion.Euler(0f, 280f, 0f) * Vector3.right,
                    280f));
            fixture.Camera.ResetState(280f, 0f);
            fixture.Simulation.Advance(Input(1, Vector2.up));

            CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(new SimulationTick(1));

            Assert.AreEqual(280f, snapshot.CameraBasisState.Yaw, 0.0001f);
            Assert.That(Vector3.Distance(
                Quaternion.Euler(0f, 280f, 0f) * Vector3.forward,
                fixture.RuntimeController.LocomotionModule.CurrentWorldDirection),
                Is.LessThan(0.0001f));

            fixture.Camera.ResetState(0f, 0f);
            fixture.Simulation.Restore(in snapshot);

            Assert.AreEqual(0f, fixture.Camera.Yaw, 0.0001f);
            AssertPlanarForward(280f, fixture.RuntimeController.RollbackCameraBasisProvider.CameraPlanarForward);
        }

        [Test]
        public void CharacterFrameRollbackSimulationReplaysCameraLookRelativeMovement()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            PredictionInputHistory inputHistory = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(8);

            fixture.Camera.ResetState(90f, 0f);
            snapshotHistory.Write(fixture.Simulation.CaptureSnapshot(SimulationTick.Zero));

            PredictionInputFrame[] frames =
            {
                Input(1, Vector2.up, Vector2.right * 12f),
                Input(2, Vector2.up, Vector2.right * -4f),
                Input(3, Vector2.up, Vector2.zero)
            };

            for (int i = 0; i < frames.Length; i++)
            {
                inputHistory.Write(frames[i]);
                fixture.Simulation.Advance(in frames[i]);
                CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(frames[i].Tick);
                snapshotHistory.Write(in snapshot);
            }

            fixture.Camera.ResetState(270f, 0f);
            LocalRollbackSynctestRunner runner = new LocalRollbackSynctestRunner(inputHistory, snapshotHistory, fixture.Simulation);
            LocalRollbackSynctestResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(3),
                SimulationTick.Zero,
                CharacterSimulationSnapshotTolerance.Default);

            AssertSynctestSuccess(in result);
        }

        [Test]
        public void CharacterFrameRollbackSimulationUsesInputCameraBasisForReplayMovement()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            PredictionInputHistory inputHistory = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(8);

            fixture.Camera.ResetState(0f, 0f);
            CharacterSimulationSnapshot initial = fixture.Simulation.CaptureSnapshot(SimulationTick.Zero);
            snapshotHistory.Write(in initial);

            RollbackCameraBasisState basis = new RollbackCameraBasisState(
                Quaternion.Euler(0f, 35f, 0f) * Vector3.forward,
                Quaternion.Euler(0f, 35f, 0f) * Vector3.right,
                35f);
            PredictionInputFrame frame = Input(1, Vector2.right).WithCameraBasis(in basis);
            inputHistory.Write(in frame);
            fixture.Simulation.Advance(in frame);
            CharacterSimulationSnapshot expected = fixture.Simulation.CaptureSnapshot(frame.Tick);
            snapshotHistory.Write(in expected);

            fixture.Camera.ResetState(180f, 0f);
            LocalRollbackSynctestRunner runner = new LocalRollbackSynctestRunner(inputHistory, snapshotHistory, fixture.Simulation);
            LocalRollbackSynctestResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(1),
                SimulationTick.Zero,
                CharacterSimulationSnapshotTolerance.Default);

            AssertSynctestSuccess(in result);
            Assert.AreEqual(35f, expected.CameraBasisState.Yaw, 0.0001f);
        }

        [Test]
        public void CharacterFrameRollbackReplayDoesNotMoveLocalCameraTargets()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            GameObject followTarget = new GameObject("rollback-local-camera-follow-test");
            GameObject aimTarget = new GameObject("rollback-local-camera-aim-test");

            try
            {
                fixture.Camera.CameraFollowTarget = followTarget.transform;
                fixture.Camera.CameraAimTarget = aimTarget.transform;
                fixture.Camera.FollowAnchorSource = fixture.Root.transform;
                fixture.Root.transform.position = new Vector3(2f, 0f, 3f);
                fixture.Camera.Resolve();
                Vector3 followBefore = followTarget.transform.position;
                Vector3 aimBefore = aimTarget.transform.position;

                CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(SimulationTick.Zero);
                fixture.Root.transform.position = new Vector3(20f, 0f, 30f);
                fixture.Simulation.Restore(in snapshot);
                fixture.Simulation.Advance(Input(1, Vector2.up));

                Assert.AreEqual(followBefore, followTarget.transform.position);
                Assert.AreEqual(aimBefore, aimTarget.transform.position);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(aimTarget);
                UnityEngine.Object.DestroyImmediate(followTarget);
            }
        }

        [Test]
        public void CharacterFrameRollbackSnapshotUsesLatestLocomotionFramePhase()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();

            for (int tick = 1; tick <= 8; tick++)
                fixture.Simulation.Advance(Input(tick, Vector2.up));
            fixture.Simulation.Advance(Input(9, Vector2.zero));

            CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(new SimulationTick(9));

            Assert.AreEqual(BasicMovementPhase.MoveStop, snapshot.LocomotionPhase);
            Assert.AreEqual(BasicMovementPhase.MoveStop, snapshot.RuntimeBlackboard.Animation.LocomotionProgress.Phase);
            Assert.AreEqual(snapshot.RuntimeBlackboard.Animation.LocomotionProgress.AliasKey, snapshot.AnimationKey);
        }

        [Test]
        public void CharacterFrameRollbackRestorePreservesLocomotionAnimationPhaseDuringAction()
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            fixture.LocomotionPresenter.SetPlayback(new ManualLocomotionPlaybackProgress(
                BasicMovementPhase.MoveLoop,
                "WalkLoop",
                0.5f));

            fixture.Simulation.Advance(Input(1, Vector2.up));
            fixture.Simulation.Advance(Input(2, Vector2.up, dodgePressed: true));
            CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(new SimulationTick(2));

            Assert.True(snapshot.RuntimeBlackboard.Action.Active);
            Assert.AreEqual(BasicMovementPhase.MoveLoop, snapshot.RuntimeBlackboard.Animation.LocomotionProgress.Phase);

            fixture.Simulation.Advance(Input(3, Vector2.up));
            fixture.Simulation.Restore(in snapshot);
            fixture.Simulation.Advance(Input(3, Vector2.up));
            CharacterSimulationSnapshot replayed = fixture.Simulation.CaptureSnapshot(new SimulationTick(3));

            Assert.AreEqual(BasicMovementPhase.MoveLoop, replayed.RuntimeBlackboard.Animation.LocomotionProgress.Phase);
            Assert.AreEqual(snapshot.RuntimeBlackboard.Animation.LocomotionProgress.AliasKey, replayed.AnimationKey);
        }

        [Test]
        public void CommittedActionRestoreStateDoesNotStoreUnityObjects()
        {
            var unityObjectProperties = typeof(CommittedActionRestoreState)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => typeof(UnityEngine.Object).IsAssignableFrom(property.PropertyType))
                .Select(property => property.Name)
                .ToArray();

            Assert.IsEmpty(unityObjectProperties);
        }

        [Test]
        public void CommittedActionRestoreStateSeparatesGameplayAndDiagnosticState()
        {
            CharacterStateMachineRestoreState stateMachine = new CharacterStateMachineRestoreState(
                new CharacterStateMachineSnapshot(
                    CharacterStateIds.MoveLoop,
                    0.25f,
                    CharacterStateVariant.None,
                    "Locomotion/MoveLoop",
                    Array.Empty<CharacterStateTag>()),
                Vector3.forward,
                true);
            CharacterInputRequestFact requestFact = new CharacterInputRequestFact(
                true,
                InputRequestKind.Dodge,
                10,
                12,
                30,
                CharacterStateVariant.Directional,
                Vector3.forward);
            CharacterActionRequest request = new CharacterActionRequest(
                CharacterFrameRequestProviderId.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                10,
                12,
                30,
                0,
                CharacterStateVariant.Directional,
                Vector3.forward);
            ActionMotionSpec motionSpec = new ActionMotionSpec(
                ActionStateIds.Dodge,
                CharacterStateIds.MoveLoop,
                CharacterStateVariant.Directional,
                0.35f,
                4f,
                true,
                true,
                Vector3.forward,
                0.25f,
                10);
            CharacterResolvedAction resolvedAction = new CharacterResolvedAction(
                CharacterFrameRequestProviderId.Dodge,
                request,
                requestFact,
                new ActionInterruptRequest(3, ActionRequestType.Dodge, ActionStateIds.Dodge, 30, 10),
                new ActionInterruptContext(ActionStateIds.None, 0f, 0, 10),
                ActionAnimationKeys.DodgeDirectional,
                motionSpec);
            CommittedActionGameplayRestoreState gameplay = new CommittedActionGameplayRestoreState(
                stateMachine,
                new ActionLifecycleRestoreState(true, resolvedAction, 0.25f, false, new ActionAnimationPlaybackIntent(1), 1));
            CommittedActionDiagnosticRestoreState diagnostic = new CommittedActionDiagnosticRestoreState(
                "Action.Dodge",
                "pending",
                "last-full",
                "last-pending",
                "Locomotion.MoveLoop",
                BasicMovementPhase.MoveLoop,
                false);

            CommittedActionRestoreState restoreState = new CommittedActionRestoreState(gameplay, diagnostic);

            Assert.AreEqual(CharacterStateIds.MoveLoop, restoreState.Gameplay.Snapshot.ActiveState);
            Assert.True(restoreState.Gameplay.ActionLifecycle.HasActiveAction);
            Assert.AreEqual(ActionStateIds.Dodge, restoreState.Gameplay.ActionLifecycle.ActiveAction.MotionSpec.ActionState);
            Assert.AreEqual(Vector3.forward, restoreState.Gameplay.StateMachine.ActionWorldDirection);
            Assert.AreEqual("Action.Dodge", restoreState.Diagnostic.DebugStatePath);
            Assert.AreEqual("last-full", restoreState.Diagnostic.LastLoggedStatePath);
            Assert.False(restoreState.Diagnostic.LoggedInitialLocomotionState);
            Assert.AreEqual(CharacterStateIds.MoveLoop, restoreState.Snapshot.ActiveState);
        }

        [Test]
        public void SnapshotComparisonIgnoresCharacterFrameDiagnosticRestoreState()
        {
            CharacterStateMachineRestoreState stateMachine = new CharacterStateMachineRestoreState(
                new CharacterStateMachineSnapshot(
                    CharacterStateIds.Dodge,
                    0.25f,
                    CharacterStateVariant.Directional,
                    string.Empty,
                    Array.Empty<CharacterStateTag>()),
                Vector3.forward,
                true);
            CommittedActionGameplayRestoreState gameplay = new CommittedActionGameplayRestoreState(stateMachine);
            CharacterSimulationSnapshot expected = SnapshotWithCommittedAction(
                new CommittedActionRestoreState(
                    gameplay,
                    new CommittedActionDiagnosticRestoreState(
                        "debug-a",
                        "pending-a",
                        "full-a",
                        "transition-a",
                        "locomotion-a",
                        BasicMovementPhase.MoveLoop,
                        true)));
            CharacterSimulationSnapshot actual = SnapshotWithCommittedAction(
                new CommittedActionRestoreState(
                    gameplay,
                    new CommittedActionDiagnosticRestoreState(
                        "debug-b",
                        "pending-b",
                        "full-b",
                        "transition-b",
                        "locomotion-b",
                        BasicMovementPhase.MoveStop,
                        false)));

            CharacterSimulationSnapshotComparison comparison = CharacterSimulationSnapshotComparer.Compare(
                in expected,
                in actual,
                CharacterSimulationSnapshotTolerance.Default);

            Assert.True(comparison.Matches, string.Join(", ", comparison.Differences));
        }

        [Test]
        public void CharacterFrameRollbackSimulationDoesNotCaptureOrRestoreRealCameraState()
        {
            string sourcePath = Path.Combine(Application.dataPath, "Scripts/Simulation/Rollback/CharacterFrameRollbackSimulation.cs");
            string source = File.ReadAllText(sourcePath);

            Assert.That(source, Does.Not.Contain("ThirdPersonCamera"));
            Assert.That(source, Does.Not.Contain("CameraController"));
            Assert.That(source, Does.Not.Contain("Camera.main"));
            Assert.That(source, Does.Not.Contain("FreeLook"));
            Assert.That(source, Does.Not.Contain("ResetState"));
            Assert.That(source, Does.Not.Contain("CaptureRollbackState"));
            Assert.That(source, Does.Not.Contain("RestoreRollbackState"));
        }

        [Test]
        public void CharacterFrameRollbackCoreDoesNotReferenceForbiddenIntegrationTypes()
        {
            string root = Path.Combine(Application.dataPath, "Scripts/Simulation/Rollback");
            string combined = string.Join("\n", Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

            Assert.That(combined, Does.Not.Contain("Fantasy"));
            Assert.That(combined, Does.Not.Contain(".proto"));
            Assert.That(combined, Does.Not.Contain("Animancer"));
            Assert.That(combined, Does.Not.Contain("Cinemachine"));
            Assert.That(combined, Does.Not.Contain("UnityEngine.InputSystem"));
            Assert.That(combined, Does.Not.Contain("CharacterController"));
            Assert.That(combined, Does.Not.Contain("BasicLocomotionPipeline"));
        }

        static PredictionInputFrame Input(int tick, Vector2 move, bool runHeld = false, bool dodgePressed = false)
        {
            return Input(tick, move, Vector2.zero, runHeld, dodgePressed);
        }

        static PredictionInputFrame Input(int tick, Vector2 move, Vector2 look, bool runHeld = false, bool dodgePressed = false)
        {
            return new PredictionInputFrame(
                new SimulationTick(tick),
                move,
                look,
                runHeld,
                dodgePressed ? new PredictionButtonFrame(true, true, false) : PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None);
        }

        static CharacterSimulationSnapshot SnapshotWithMotionRoot(Vector3 rootPosition, float rootYaw)
        {
            return new CharacterSimulationSnapshot(
                SimulationTick.Zero,
                rootPosition,
                rootYaw,
                CharacterStateMachineSnapshot.Inactive,
                false,
                BasicMovementGait.Walk,
                Vector3.zero,
                BasicMovementPhase.Idle,
                BasicMovementGait.Walk,
                string.Empty,
                0f).WithMotionExecutorState(new MotionExecutorRollbackState(
                    0f,
                    Vector3.zero,
                    0f,
                    rootPosition,
                    rootYaw,
                    true));
        }

        static CharacterSimulationSnapshot SnapshotWithCommittedAction(CommittedActionRestoreState committedActionRestoreState)
        {
            return new CharacterSimulationSnapshot(
                SimulationTick.Zero,
                Vector3.zero,
                0f,
                CharacterStateMachineSnapshot.Inactive,
                false,
                BasicMovementGait.Walk,
                Vector3.zero,
                BasicMovementPhase.Idle,
                BasicMovementGait.Walk,
                string.Empty,
                0f).WithCommittedActionState(committedActionRestoreState, InputRequestBufferComponentRestoreState.Empty);
        }

        static CommittedActionRestoreState CreateActionRestoreState(
            CharacterStateVariant variant,
            Vector3 actionWorldDirection,
            float stateTime)
        {
            DodgeActionVariant actionVariant = variant == CharacterStateVariant.Backstep
                ? DodgeActionVariant.Backstep
                : DodgeActionVariant.Directional;
            DodgeActionTuning config = TestDodgeTuning();
            CharacterStateMachineSnapshot snapshot = new CharacterStateMachineSnapshot(
                CharacterStateIds.MoveLoop,
                stateTime,
                CharacterStateVariant.None,
                "Locomotion/MoveLoop",
                Array.Empty<CharacterStateTag>());
            CharacterStateMachineRestoreState state = new CharacterStateMachineRestoreState(
                snapshot,
                actionWorldDirection,
                true);
            CharacterInputRequestFact requestFact = new CharacterInputRequestFact(
                true,
                InputRequestKind.Dodge,
                5,
                9,
                config.Priority,
                variant,
                actionWorldDirection);
            CharacterActionRequest request = new CharacterActionRequest(
                CharacterFrameRequestProviderId.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                5,
                9,
                config.Priority,
                0,
                variant,
                actionWorldDirection);
            ActionMotionSpec motionSpec = new ActionMotionSpec(
                ActionStateIds.Dodge,
                CharacterStateIds.MoveLoop,
                variant,
                config.ResolveDuration(actionVariant),
                config.ResolveDistance(actionVariant),
                config.ShouldRotateToDirection(actionVariant),
                false,
                actionWorldDirection,
                stateTime,
                5);
            CharacterResolvedAction resolvedAction = new CharacterResolvedAction(
                CharacterFrameRequestProviderId.Dodge,
                request,
                requestFact,
                new ActionInterruptRequest(0, ActionRequestType.Dodge, ActionStateIds.Dodge, config.Priority, 5),
                new ActionInterruptContext(ActionStateIds.None, 0f, 0, 5),
                DodgeActionPlanner.ResolveAnimationKey(actionVariant),
                motionSpec);
            return new CommittedActionRestoreState(
                new CommittedActionGameplayRestoreState(
                    state,
                    new ActionLifecycleRestoreState(true, resolvedAction, stateTime, false, new ActionAnimationPlaybackIntent(1), 1)),
                CommittedActionDiagnosticRestoreState.Empty);
        }

        static void AssertSynctestSuccess(in LocalRollbackSynctestResult result)
        {
            if (result.Success)
                return;

            string message = result.FirstMismatch.HasMismatch
                ? LocalRollbackSynctestLogFormatter.FormatFirstMismatch(in result)
                : LocalRollbackSynctestLogFormatter.FormatFail(in result);
            Assert.Fail(message);
        }

        static CharacterSimulationSnapshot CreateTurnBackSnapshot(SimulationTick tick, float normalizedTime)
        {
            AnimationPhasePlaybackProgress progress = new AnimationPhasePlaybackProgress(
                BasicMovementPhase.TurnBack,
                TurnBackMotionPolicy.DefaultAliasKey,
                normalizedTime,
                true,
                false);
            CharacterStateMachineSnapshot stateSnapshot = new CharacterStateMachineSnapshot(
                CharacterStateIds.TurnBack,
                normalizedTime,
                CharacterStateVariant.None,
                "Locomotion.TurnBack",
                Array.Empty<CharacterStateTag>());
            CharacterStateMachineRestoreState state = new CharacterStateMachineRestoreState(
                stateSnapshot,
                Vector3.zero,
                Vector3.back,
                Vector3.forward,
                true);
            CharacterRuntimeBlackboardSnapshot blackboard = new CharacterRuntimeBlackboardSnapshot(
                new CharacterRuntimeLocomotionFacts(
                    BasicMovementPhase.TurnBack,
                    BasicMovementGait.Run,
                    BasicMovementGait.Run,
                    false,
                    BasicMovementGait.Run,
                    true,
                    Vector3.back,
                    true,
                    1f,
                    tick.Value),
                CharacterRuntimeActionFacts.Default,
                new CharacterRuntimeAnimationFacts(
                    progress,
                    TurnBackMotionPolicy.DefaultAliasKey,
                    ActionAnimationPlaybackProgress.Invalid,
                    string.Empty,
                    tick.Value),
                CharacterRuntimeDebugFacts.Default);
            LocomotionRuntimeRollbackState locomotionRuntime = new LocomotionRuntimeRollbackState(
                MovementInputIntent.FromRaw(Vector2.down, 0f, true),
                Vector3.back,
                progress,
                true,
                false,
                BasicMovementGait.Run,
                LocomotionTurnBackIntent.None);
            CommittedActionRestoreState committedActionState = new CommittedActionRestoreState(
                new CommittedActionGameplayRestoreState(state),
                CommittedActionDiagnosticRestoreState.Empty);

            return new CharacterSimulationSnapshot(
                tick,
                Vector3.zero,
                0f,
                state,
                true,
                BasicMovementGait.Run,
                Vector3.back,
                BasicMovementPhase.TurnBack,
                BasicMovementGait.Run,
                TurnBackMotionPolicy.DefaultAliasKey,
                normalizedTime,
                new CharacterRuntimeBlackboardRestoreState(blackboard))
                .WithLocomotionRuntimeState(in locomotionRuntime)
                .WithCommittedActionState(in committedActionState, InputRequestBufferComponentRestoreState.Empty)
                .WithMotionExecutorState(new MotionExecutorRollbackState(0f, Vector3.back, 0f, Vector3.zero, 0f, true));
        }

        static LocomotionRuntimeRollbackState CreatePreviousMotionPlaybackRuntime(
            BasicMovementPhase phase,
            string aliasKey,
            float normalizedTime,
            bool hasPreviousPlayback)
        {
            return new LocomotionRuntimeRollbackState(
                MovementInputIntent.FromRaw(Vector2.down, 0f, true),
                Vector3.back,
                new AnimationPhasePlaybackProgress(
                    phase,
                    aliasKey,
                    normalizedTime,
                    hasPreviousPlayback,
                    false),
                hasPreviousPlayback,
                false,
                BasicMovementGait.Run,
                LocomotionTurnBackIntent.None);
        }

        static CharacterSimulationSnapshot CreateMoveLoopSnapshot(SimulationTick tick, string aliasKey, float normalizedTime)
        {
            AnimationPhasePlaybackProgress progress = new AnimationPhasePlaybackProgress(
                BasicMovementPhase.MoveLoop,
                aliasKey,
                normalizedTime,
                true,
                false);
            CharacterStateMachineSnapshot stateSnapshot = new CharacterStateMachineSnapshot(
                CharacterStateIds.MoveLoop,
                normalizedTime,
                CharacterStateVariant.None,
                "Locomotion.MoveLoop",
                Array.Empty<CharacterStateTag>());
            CharacterStateMachineRestoreState state = new CharacterStateMachineRestoreState(
                stateSnapshot,
                Vector3.zero,
                false);
            CharacterRuntimeBlackboardSnapshot blackboard = new CharacterRuntimeBlackboardSnapshot(
                new CharacterRuntimeLocomotionFacts(
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    BasicMovementGait.Run,
                    false,
                    BasicMovementGait.Run,
                    true,
                    Vector3.forward,
                    true,
                    1f,
                    tick.Value),
                CharacterRuntimeActionFacts.Default,
                new CharacterRuntimeAnimationFacts(
                    progress,
                    aliasKey,
                    ActionAnimationPlaybackProgress.Invalid,
                    string.Empty,
                    tick.Value),
                CharacterRuntimeDebugFacts.Default);
            LocomotionRuntimeRollbackState locomotionRuntime = new LocomotionRuntimeRollbackState(
                MovementInputIntent.FromRaw(Vector2.up, 0f, true),
                Vector3.forward,
                progress,
                true,
                false,
                BasicMovementGait.Run,
                LocomotionTurnBackIntent.None);
            CommittedActionRestoreState committedActionState = new CommittedActionRestoreState(
                new CommittedActionGameplayRestoreState(state),
                CommittedActionDiagnosticRestoreState.Empty);

            return new CharacterSimulationSnapshot(
                tick,
                Vector3.zero,
                0f,
                state,
                true,
                BasicMovementGait.Run,
                Vector3.forward,
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                aliasKey,
                normalizedTime,
                new CharacterRuntimeBlackboardRestoreState(blackboard))
                .WithLocomotionRuntimeState(in locomotionRuntime)
                .WithCommittedActionState(in committedActionState, InputRequestBufferComponentRestoreState.Empty)
                .WithMotionExecutorState(new MotionExecutorRollbackState(0f, Vector3.forward, 0f, Vector3.zero, 0f, true));
        }

        static CharacterSimulationSnapshot SnapshotHistoryLatest(PredictionSnapshotHistory history, SimulationTick tick)
        {
            Assert.True(history.TryGet(tick, out CharacterSimulationSnapshot snapshot));
            return snapshot;
        }

        static TimelineTraceFrame[] RunTimelineTraceSequence(PredictionInputFrame[] inputs)
        {
            using CharacterFrameRollbackFixture fixture = CharacterFrameRollbackFixture.Create();
            TimelineTraceFrame[] traces = new TimelineTraceFrame[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                PredictionInputFrame input = inputs[i];
                fixture.Simulation.Advance(in input);
                traces[i] = TimelineTraceFrame.From(fixture.RuntimeController.LastFramePipelineResult);
            }

            return traces;
        }

        static void AssertTimelineTraceFrameEqual(TimelineTraceFrame expected, TimelineTraceFrame actual, int index)
        {
            AssertTimelineTraceEqual(expected.Current, actual.Current, $"frame {index} current");
            AssertTimelineTraceEqual(expected.Projected, actual.Projected, $"frame {index} projected");
            AssertTimelineTraceEqual(expected.Target, actual.Target, $"frame {index} target");
        }

        static void AssertTimelineTraceEqual(TimelineTraceSnapshot expected, TimelineTraceSnapshot actual, string label)
        {
            Assert.AreEqual(expected.Source, actual.Source, $"{label} source");
            Assert.AreEqual(expected.SourceStep, actual.SourceStep, $"{label} source step");
            Assert.AreEqual(expected.RequestType, actual.RequestType, $"{label} request");
            Assert.AreEqual(expected.FactsId, actual.FactsId, $"{label} facts id");
            Assert.AreEqual(expected.StateId, actual.StateId, $"{label} state");
            Assert.AreEqual(expected.NormalizedTime, actual.NormalizedTime, 0.0001f, $"{label} normalized");
            Assert.AreEqual(expected.ElapsedSeconds, actual.ElapsedSeconds, 0.0001f, $"{label} elapsed");
            Assert.AreEqual(expected.MotionWindowActive, actual.MotionWindowActive, $"{label} motion");
            Assert.AreEqual(expected.InputLockWindowActive, actual.InputLockWindowActive, $"{label} input lock");
            Assert.AreEqual(expected.InterruptWindowActive, actual.InterruptWindowActive, $"{label} interrupt");
            Assert.AreEqual(expected.ExitWindowActive, actual.ExitWindowActive, $"{label} exit");
            Assert.AreEqual(expected.Priority, actual.Priority, $"{label} priority");
            Assert.AreEqual(expected.Resistance, actual.Resistance, $"{label} resistance");
            Assert.AreEqual(expected.MinPriority, actual.MinPriority, $"{label} min priority");
            Assert.AreEqual(expected.Force, actual.Force, $"{label} force");
            Assert.AreEqual(expected.ActiveWindowIds, actual.ActiveWindowIds, $"{label} active windows");
            Assert.AreEqual(expected.RequestWindowIds, actual.RequestWindowIds, $"{label} request windows");
            Assert.AreEqual(expected.ActiveFactIds, actual.ActiveFactIds, $"{label} active facts");
            Assert.AreEqual(expected.RequestFactIds, actual.RequestFactIds, $"{label} request facts");
        }

        static CharacterFramePipelineHost CreateFramePipelineHost()
        {
            CharacterBehaviorSubmissionRunner runner = new CharacterBehaviorSubmissionRunner(CreateBehaviorRuntimeDefinition());
            return new CharacterFramePipelineHost(runner, runner);
        }

        static CharacterBehaviorRuntimeDefinition CreateBehaviorRuntimeDefinition()
        {
            return new CharacterBehaviorRuntimeDefinition(
                new CharacterBehaviorSourceId("behavior.root"),
                new[]
                {
                    CharacterBehaviorSourceKind.Locomotion,
                    CharacterBehaviorSourceKind.CommittedAction
                });
        }

        static void AssertCompatibleTickMatchesPhasePipeline(in CharacterFrameInput input)
        {
            using CharacterFrameRollbackFixture compatible = CharacterFrameRollbackFixture.Create();
            using CharacterFrameRollbackFixture phased = CharacterFrameRollbackFixture.Create();
            CharacterFrameContext context = phased.RuntimeController.BeginFrame(in input);

            Assert.True(compatible.RuntimeController.Tick(in input));
            phased.RuntimeController.RunPhase(SimulationTickPhase.ReadInput, ref context, out _);
            phased.RuntimeController.RunPhase(SimulationTickPhase.UpdateInputBuffer, ref context, out _);
            phased.RuntimeController.RunPhase(SimulationTickPhase.GameplayDecision, ref context, out _);
            phased.RuntimeController.RunPhase(SimulationTickPhase.BuildMotion, ref context, out _);
            phased.RuntimeController.RunPhase(SimulationTickPhase.ExecuteMotion, ref context, out _);
            phased.RuntimeController.RunPhase(SimulationTickPhase.PresentationBridge, ref context, out _);
            phased.RuntimeController.RunPhase(SimulationTickPhase.WriteSnapshotAndEvents, ref context, out CharacterFrameResult phaseResult);

            CharacterFrameResult compatibleResult = compatible.RuntimeController.LastFramePipelineResult;
            Assert.True(phaseResult.Success);
            Assert.AreEqual(phaseResult.StateFrame.Snapshot.ActiveState, compatible.RuntimeController.CommittedActionModule.CurrentStateSnapshot.ActiveState);
            Assert.AreEqual(phaseResult.StateFrame.Snapshot.Variant, compatible.RuntimeController.CommittedActionModule.CurrentStateSnapshot.Variant);
            Assert.AreEqual(phaseResult.StateFrame.Owner.Kind, compatibleResult.StateFrame.Owner.Kind);
            Assert.AreEqual(phaseResult.InputRequest.RequestKind, compatibleResult.InputRequest.RequestKind);
            Assert.AreEqual(phaseResult.InputRequest.HasRequest, compatibleResult.InputRequest.HasRequest);
            Assert.AreEqual(phaseResult.InputRequestConsumed, compatibleResult.InputRequestConsumed);
            Assert.AreEqual(phaseResult.BasicMovementExecuted, compatibleResult.BasicMovementExecuted);
            Assert.AreEqual(phaseResult.ActionMovementExecuted, compatibleResult.ActionMovementExecuted);
            Assert.AreEqual(phaseResult.ActionAnimationPresented, compatibleResult.ActionAnimationPresented);
            Assert.AreEqual(phaseResult.LocomotionAnimationPresented, compatibleResult.LocomotionAnimationPresented);
            Assert.AreEqual(phaseResult.AnimationFactsWritten, compatibleResult.AnimationFactsWritten);
            Assert.AreEqual(phaseResult.SnapshotEventsReady, compatibleResult.SnapshotEventsReady);
        }

        readonly struct TimelineTraceFrame
        {
            public TimelineTraceFrame(
                TimelineTraceSnapshot current,
                TimelineTraceSnapshot projected,
                TimelineTraceSnapshot target)
            {
                Current = current;
                Projected = projected;
                Target = target;
            }

            public TimelineTraceSnapshot Current { get; }
            public TimelineTraceSnapshot Projected { get; }
            public TimelineTraceSnapshot Target { get; }

            public static TimelineTraceFrame From(in CharacterFrameResult result)
            {
                return new TimelineTraceFrame(
                    TimelineTraceSnapshot.From(result.CurrentTimelineFactsTrace),
                    TimelineTraceSnapshot.From(result.StateFrame.ProjectedTimelineFactsTrace),
                    TimelineTraceSnapshot.From(result.StateFrame.TargetTimelineFactsTrace));
            }
        }

        readonly struct TimelineTraceSnapshot
        {
            public TimelineTraceSnapshot(
                StateTimelineFactsSource source,
                int sourceStep,
                ActionRequestType requestType,
                string factsId,
                string stateId,
                float normalizedTime,
                float elapsedSeconds,
                bool motionWindowActive,
                bool inputLockWindowActive,
                bool interruptWindowActive,
                bool exitWindowActive,
                int priority,
                int resistance,
                int minPriority,
                bool force,
                string activeWindowIds,
                string requestWindowIds,
                string activeFactIds,
                string requestFactIds)
            {
                Source = source;
                SourceStep = sourceStep;
                RequestType = requestType;
                FactsId = factsId;
                StateId = stateId;
                NormalizedTime = normalizedTime;
                ElapsedSeconds = elapsedSeconds;
                MotionWindowActive = motionWindowActive;
                InputLockWindowActive = inputLockWindowActive;
                InterruptWindowActive = interruptWindowActive;
                ExitWindowActive = exitWindowActive;
                Priority = priority;
                Resistance = resistance;
                MinPriority = minPriority;
                Force = force;
                ActiveWindowIds = activeWindowIds;
                RequestWindowIds = requestWindowIds;
                ActiveFactIds = activeFactIds;
                RequestFactIds = requestFactIds;
            }

            public StateTimelineFactsSource Source { get; }
            public int SourceStep { get; }
            public ActionRequestType RequestType { get; }
            public string FactsId { get; }
            public string StateId { get; }
            public float NormalizedTime { get; }
            public float ElapsedSeconds { get; }
            public bool MotionWindowActive { get; }
            public bool InputLockWindowActive { get; }
            public bool InterruptWindowActive { get; }
            public bool ExitWindowActive { get; }
            public int Priority { get; }
            public int Resistance { get; }
            public int MinPriority { get; }
            public bool Force { get; }
            public string ActiveWindowIds { get; }
            public string RequestWindowIds { get; }
            public string ActiveFactIds { get; }
            public string RequestFactIds { get; }

            public static TimelineTraceSnapshot From(StateTimelineFactsTrace trace)
            {
                StateTimelineWindowFacts facts = trace.Facts;
                return new TimelineTraceSnapshot(
                    trace.Source,
                    trace.SourceStep,
                    trace.RequestType,
                    trace.FactsId,
                    facts.StateId.Value,
                    facts.NormalizedTime,
                    facts.ElapsedSeconds,
                    facts.MotionWindowActive,
                    facts.InputLockWindowActive,
                    facts.InterruptWindowActive,
                    facts.ExitWindowActive,
                    facts.Priority,
                    facts.Resistance,
                    facts.MinPriority,
                    facts.Force,
                    facts.ActiveWindowIds,
                    facts.RequestWindowIds,
                    facts.ActiveFactIds,
                    facts.RequestFactIds);
            }
        }

        sealed class RecordingCharacterFrameRuntimePort : ICharacterFrameRuntimePort
        {
            readonly ICharacterFrameRuntimePort inner;
            readonly List<string> calls = new List<string>();

            public RecordingCharacterFrameRuntimePort(ICharacterFrameRuntimePort inner)
            {
                this.inner = inner;
            }

            public ILocomotionFrameRuntimePort LocomotionFrameRuntime => inner.LocomotionFrameRuntime;
            public CharacterStateMachineRunner StateMachine => inner.StateMachine;
            public CharacterStateMachineSnapshot CurrentStateSnapshot => inner.CurrentStateSnapshot;
            public InputRequestBuffer InputRequestBuffer => inner.InputRequestBuffer;
            public string ActiveFrameStatePath => inner.ActiveFrameStatePath;

            public int IndexOf(string call)
            {
                return calls.IndexOf(call);
            }

            public bool WriteBufferedInputFacts(in CharacterFrameInput input)
            {
                calls.Add("WriteBufferedInputFacts");
                return inner.WriteBufferedInputFacts(in input);
            }

            public bool PrepareFrameRuntimeAdapters()
            {
                calls.Add("PrepareFrameRuntimeAdapters");
                return inner.PrepareFrameRuntimeAdapters();
            }

            public bool TryResolveActionCatalog(out CharacterActionCatalog catalog)
            {
                return inner.TryResolveActionCatalog(out catalog);
            }

            public bool TryResolveBodyClaimPolicy(out BodyClaimPolicy policy)
            {
                return inner.TryResolveBodyClaimPolicy(out policy);
            }

            public ActionLifecycleFrame TickActionLifecycle(
                in CharacterResolvedAction acceptedAction,
                in CharacterActionCatalog actionCatalog,
                float deltaTime,
                int step)
            {
                calls.Add("TickActionLifecycle");
                return inner.TickActionLifecycle(in acceptedAction, in actionCatalog, deltaTime, step);
            }

            public void CompleteActionLifecycle(in ActionMotionResolveResult result, bool requireAnimationEnded)
            {
                calls.Add("CompleteActionLifecycle");
                inner.CompleteActionLifecycle(in result, requireAnimationEnded);
            }

            public int ResolveCurrentActionResistance()
            {
                return inner.ResolveCurrentActionResistance();
            }

            public IReadOnlyList<ActionInterruptPolicy> ResolveInterruptPolicies()
            {
                return inner.ResolveInterruptPolicies();
            }

            public void SetLastFrameOutputs(
                in BasicLocomotionFrame locomotionFrame,
                in CharacterStateMachineFrame stateFrame,
                in ActionMotionResolveResult actionMotionResult)
            {
                calls.Add("SetLastFrameOutputs");
                inner.SetLastFrameOutputs(in locomotionFrame, in stateFrame, in actionMotionResult);
            }

            public bool ConsumeFrameInputRequest(in CharacterFrameInputConsumeSubmission inputConsume)
            {
                calls.Add("ConsumeFrameInputRequest");
                return inner.ConsumeFrameInputRequest(in inputConsume);
            }

            public void ExecuteFrameMotion(
                in CharacterFrameMovementSubmission movement,
                out bool actionMovementExecuted,
                out bool basicMovementExecuted)
            {
                calls.Add("ExecuteFrameMotion");
                inner.ExecuteFrameMotion(
                    in movement,
                    out actionMovementExecuted,
                    out basicMovementExecuted);
            }

            public void PresentFrameAnimation(
                in CharacterFrameAnimationSubmission animation,
                in BasicLocomotionFrame locomotionFrame,
                out bool actionAnimationPresented,
                out bool locomotionAnimationPresented)
            {
                calls.Add("PresentFrameAnimation");
                inner.PresentFrameAnimation(
                    in animation,
                    in locomotionFrame,
                    out actionAnimationPresented,
                    out locomotionAnimationPresented);
            }

            public void WriteStateFrameActionFacts(
                in CharacterStateMachineFrame stateFrame,
                in ActionMotionResolveResult actionMotionResult,
                bool exitedToLocomotion,
                int step)
            {
                calls.Add("WriteStateFrameActionFacts");
                inner.WriteStateFrameActionFacts(in stateFrame, in actionMotionResult, exitedToLocomotion, step);
            }

            public void WriteLocomotionPreemptionFact(in LocomotionPreemptionFact fact)
            {
                calls.Add("WriteLocomotionPreemptionFact");
                inner.WriteLocomotionPreemptionFact(in fact);
            }

            public void UpdateStateSnapshot(in CharacterStateMachineFrame stateFrame, int step)
            {
                calls.Add("UpdateStateSnapshot");
                inner.UpdateStateSnapshot(in stateFrame, step);
            }

            public void WriteAnimationRuntimeFacts(int step)
            {
                calls.Add("WriteAnimationRuntimeFacts");
                inner.WriteAnimationRuntimeFacts(step);
            }

            public void CompleteLocomotionTick()
            {
                calls.Add("CompleteLocomotionTick");
                inner.CompleteLocomotionTick();
            }

            public void LogDiagnosticTickSnapshots(int step)
            {
                calls.Add("LogDiagnosticTickSnapshots");
                inner.LogDiagnosticTickSnapshots(step);
            }
        }

        sealed class CharacterFrameRollbackFixture : IDisposable
        {
            CharacterFrameRollbackFixture(
                GameObject root,
                CharacterFrameRollbackSimulation simulation,
                CharacterFrameRuntimeController runtimeController,
                ThirdPersonCameraController camera,
                InputRequestBufferComponent inputBuffer,
                FakeLocomotionInputSource inputSource,
                FakeCharacterMotionDriver driver,
                FakeLocomotionAnimationPresenter locomotionPresenter,
                FakeActionAnimationPresenter actionPresenter,
                ScriptableObject[] assets)
            {
                Root = root;
                Simulation = simulation;
                RuntimeController = runtimeController;
                Camera = camera;
                InputBuffer = inputBuffer;
                InputSource = inputSource;
                Driver = driver;
                LocomotionPresenter = locomotionPresenter;
                ActionPresenter = actionPresenter;
                Assets = assets;
            }

            public GameObject Root { get; }
            public CharacterFrameRollbackSimulation Simulation { get; }
            public CharacterFrameRuntimeController RuntimeController { get; }
            public ThirdPersonCameraController Camera { get; }
            public InputRequestBufferComponent InputBuffer { get; }
            public FakeLocomotionInputSource InputSource { get; }
            public FakeCharacterMotionDriver Driver { get; }
            public FakeLocomotionAnimationPresenter LocomotionPresenter { get; }
            public FakeActionAnimationPresenter ActionPresenter { get; }
            ScriptableObject[] Assets { get; }

            public static CharacterFrameRollbackFixture Create()
            {
                GameObject root = new GameObject("character-frame-rollback-fixture");
                root.SetActive(false);

                InputRequestBufferComponent inputBuffer = root.AddComponent<InputRequestBufferComponent>();
                FakeLocomotionInputSource inputSource = root.AddComponent<FakeLocomotionInputSource>();
                ThirdPersonCameraController camera = root.AddComponent<ThirdPersonCameraController>();
                FakeCharacterMotionDriver driver = root.AddComponent<FakeCharacterMotionDriver>();
                FakeLocomotionAnimationPresenter locomotionPresenter = root.AddComponent<FakeLocomotionAnimationPresenter>();
                FakeActionAnimationPresenter actionPresenter = root.AddComponent<FakeActionAnimationPresenter>();
                CharacterFrameRuntimeController runtimeController = root.AddComponent<CharacterFrameRuntimeController>();
                CharacterFrameRollbackSimulation simulation = root.AddComponent<CharacterFrameRollbackSimulation>();
                ActionInterruptPolicySetSO policySet = CreatePolicySet();
                CharacterActionDefinitionSO actionDefinition = CreateDodgeDefinition(TestDodgeTuning());
                CharacterActionCatalogSO actionCatalog = CreateCatalog(actionDefinition);
                CharacterConfigSO baseConfig = LoadConfiguredCharacterConfigAsset();
                CharacterConfigSO characterConfig = CreateCharacterConfig(
                    baseConfig.StateMachine,
                    baseConfig.Movement,
                    baseConfig.LocomotionAnimation,
                    policySet,
                    actionCatalog);

                camera.AutoTick = false;
                camera.DebugLog = false;
                camera.ResetState(0f, 0f);
                runtimeController.AutoUpdate = false;
                runtimeController.CharacterConfig = characterConfig;
                runtimeController.InputBufferComponent = inputBuffer;
                runtimeController.InputSourceBehaviour = inputSource;
                runtimeController.MotionExecutorBehaviour = driver;
                runtimeController.FacingProviderBehaviour = driver;
                runtimeController.CameraController = camera;
                runtimeController.LocomotionPresenterBehaviour = locomotionPresenter;
                runtimeController.ActionMovementExecutorBehaviour = driver;
                runtimeController.AnimationPresenterBehaviour = actionPresenter;
                simulation.RuntimeController = runtimeController;
                simulation.InputBufferComponent = inputBuffer;

                return new CharacterFrameRollbackFixture(
                    root,
                    simulation,
                    runtimeController,
                    camera,
                    inputBuffer,
                    inputSource,
                    driver,
                    locomotionPresenter,
                    actionPresenter,
                    new ScriptableObject[] { policySet, actionDefinition, actionCatalog, characterConfig });
            }

            public void Dispose()
            {
                for (int i = 0; i < Assets.Length; i++)
                    UnityEngine.Object.DestroyImmediate(Assets[i]);
                UnityEngine.Object.DestroyImmediate(Root);
            }

            static ActionInterruptPolicySetSO CreatePolicySet()
            {
                ActionInterruptPolicySetSO policySet = ScriptableObject.CreateInstance<ActionInterruptPolicySetSO>();
                ActionInterruptPolicyDefinition[] policies =
                {
                    new ActionInterruptPolicyDefinition(ActionStateIds.None.Value, ActionStateIds.Dodge.Value, 0),
                    new ActionInterruptPolicyDefinition(ActionStateIds.Dodge.Value, ActionStateIds.Dodge.Value, 0),
                    new ActionInterruptPolicyDefinition(CharacterStateIds.MoveStart.Value, ActionStateIds.Dodge.Value, 0),
                    new ActionInterruptPolicyDefinition(CharacterStateIds.MoveLoop.Value, ActionStateIds.Dodge.Value, 0),
                    new ActionInterruptPolicyDefinition(CharacterStateIds.MoveStop.Value, ActionStateIds.Dodge.Value, 0),
                new ActionInterruptPolicyDefinition(CharacterStateIds.MoveStart.Value, CharacterStateIds.TurnBack.Value, 20),
                new ActionInterruptPolicyDefinition(CharacterStateIds.MoveLoop.Value, CharacterStateIds.TurnBack.Value, 20)
            };
                FieldInfo field = typeof(ActionInterruptPolicySetSO).GetField("policies", BindingFlags.Instance | BindingFlags.NonPublic);
                field.SetValue(policySet, policies);
                return policySet;
            }

            static CharacterActionCatalogSO CreateCatalog(CharacterActionDefinitionSO definition)
            {
                CharacterActionCatalogSO catalog = ScriptableObject.CreateInstance<CharacterActionCatalogSO>();
                SetPrivateField(catalog, "definitions", new[] { definition });
                return catalog;
            }

            static CharacterActionDefinitionSO CreateDodgeDefinition(in DodgeActionTuning config)
            {
                CharacterActionDefinitionSO definition = ScriptableObject.CreateInstance<CharacterActionDefinitionSO>();
                SetPrivateField(definition, "actionStateId", ActionStateIds.Dodge.Value);
                SetPrivateField(definition, "requestType", ActionRequestType.Dodge);
                SetPrivateField(definition, "sourceInputKind", InputRequestKind.Dodge);
                SetPrivateField(definition, "motionSourceStateId", CharacterStateIds.Dodge.Value);
                SetPrivateField(definition, "priority", config.Priority);
                SetPrivateField(definition, "resistance", config.Resistance);
                SetPrivateField(definition, "directionalDodge", new DodgeActionVariantAuthoring(
                    DodgeActionVariant.Directional,
                    config.DirectionalDuration,
                    config.DirectionalDistance,
                    config.DirectionalRotateToDirection,
                    ActionAnimationKeys.DodgeDirectional.Value));
                SetPrivateField(definition, "backstepDodge", new DodgeActionVariantAuthoring(
                    DodgeActionVariant.Backstep,
                    config.BackstepDuration,
                    config.BackstepDistance,
                    config.BackstepRotateToDirection,
                    ActionAnimationKeys.DodgeBackstep.Value));
                SetPrivateField(definition, "committedActionBranch", CreateDodgeCommittedBranch(config));
                return definition;
            }

            static CommittedActionBranchAuthoring CreateDodgeCommittedBranch(in DodgeActionTuning config)
            {
                return new CommittedActionBranchAuthoring(
                    1,
                    true,
                    "action.dodge",
                    "selector.dodge",
                    BodyOccupancyKind.FullBody,
                    CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                    new[]
                    {
                        CommittedActionBranchNodeAuthoring.Selector(
                            "selector.dodge",
                            new[] { "condition.directional", "condition.backstep" },
                            Vector2.zero),
                        CommittedActionBranchNodeAuthoring.ConditionNode(
                            "condition.directional",
                            new CommittedActionBranchConditionAuthoring(
                                CommittedActionConditionKind.ActionVariantEquals,
                                CharacterStateVariant.Directional,
                                false),
                            new[] { "timeline.dodge.directional" },
                            new Vector2(1f, 0f)),
                        CommittedActionBranchNodeAuthoring.ConditionNode(
                            "condition.backstep",
                            new CommittedActionBranchConditionAuthoring(
                                CommittedActionConditionKind.ActionVariantEquals,
                                CharacterStateVariant.Backstep,
                                false),
                            new[] { "timeline.dodge.backstep" },
                            new Vector2(1f, 1f)),
                        CommittedActionBranchNodeAuthoring.TimelineNode(
                            "timeline.dodge.directional",
                            CreateDodgeTimeline(
                                "action.dodge.directional",
                                "timeline.dodge.directional",
                                CharacterStateVariant.Directional,
                                ActionAnimationKeys.DodgeDirectional.Value,
                                config.DirectionalDuration,
                                config.DirectionalDistance,
                                config.DirectionalRotateToDirection,
                                true),
                            new Vector2(2f, 0f)),
                        CommittedActionBranchNodeAuthoring.TimelineNode(
                            "timeline.dodge.backstep",
                            CreateDodgeTimeline(
                                "action.dodge.backstep",
                                "timeline.dodge.backstep",
                                CharacterStateVariant.Backstep,
                                ActionAnimationKeys.DodgeBackstep.Value,
                                config.BackstepDuration,
                                config.BackstepDistance,
                                config.BackstepRotateToDirection,
                                false),
                            new Vector2(2f, 1f))
                    });
            }

            static CommittedActionBranchTimelineAuthoring CreateDodgeTimeline(
                string branchId,
                string timelineNodeId,
                CharacterStateVariant variant,
                string animationKey,
                float duration,
                float distance,
                bool rotateToDirection,
                bool setRunLatch)
            {
                return new CommittedActionBranchTimelineAuthoring(
                    true,
                    branchId,
                    timelineNodeId,
                    duration,
                    BodyOccupancyKind.FullBody,
                    CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                    new[]
                    {
                        new ActionTimelineTrackAuthoring(
                            ActionTimelineTrackKind.Animation,
                            new[]
                            {
                                new ActionTimelineClipAuthoring(
                                    ActionTimelineClipKind.AnimationKey,
                                    0,
                                    duration,
                                    ActionTimelineClipPayloadAuthoring.Animation(animationKey))
                            }),
                        new ActionTimelineTrackAuthoring(
                            ActionTimelineTrackKind.Motion,
                            new[]
                            {
                                new ActionTimelineClipAuthoring(
                                    ActionTimelineClipKind.Motion,
                                    0,
                                    duration,
                                    ActionTimelineClipPayloadAuthoring.Motion(
                                        CharacterStateIds.Dodge.Value,
                                        variant,
                                        duration,
                                        distance,
                                        rotateToDirection,
                                        setRunLatch))
                            })
                    });
            }
        }

        static DodgeActionTuning TestDodgeTuning()
        {
            return new DodgeActionTuning(0.35f, 4f, 0.35f, 3f, 30, 20, true, false);
        }

        static CharacterConfigSO LoadConfiguredCharacterConfigAsset()
        {
            CharacterConfigSO asset = AssetDatabase.LoadAssetAtPath<CharacterConfigSO>(
                "Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset");
            Assert.NotNull(asset);
            Assert.NotNull(asset.StateMachine);
            Assert.NotNull(asset.Movement);
            return asset;
        }

        static CharacterStateMachineDefinitionSO LoadConfiguredStateMachineDefinitionAsset()
        {
            CharacterStateMachineDefinitionSO asset = AssetDatabase.LoadAssetAtPath<CharacterStateMachineDefinitionSO>(
                "Assets/Configs/3C/StateMachine/Locomotion/Corin/CorinLocomotionStateGraph.asset");
            Assert.NotNull(asset);
            return asset;
        }

        static CharacterConfigSO CreateCharacterConfig(
            CharacterStateMachineDefinitionSO stateMachine,
            BasicMovementConfigSO movement,
            RunLocomotionAnimationConfigSO locomotionAnimation)
        {
            CharacterConfigSO baseConfig = LoadConfiguredCharacterConfigAsset();
            return CreateCharacterConfig(
                stateMachine,
                movement,
                locomotionAnimation,
                baseConfig.ActionInterruptPolicy,
                baseConfig.ActionCatalog);
        }

        static CharacterConfigSO CreateCharacterConfig(
            CharacterStateMachineDefinitionSO stateMachine,
            BasicMovementConfigSO movement,
            RunLocomotionAnimationConfigSO locomotionAnimation,
            ActionInterruptPolicySetSO policySet,
            CharacterActionCatalogSO actionCatalog)
        {
            CharacterConfigSO config = ScriptableObject.CreateInstance<CharacterConfigSO>();
            CharacterConfigSO baseConfig = LoadConfiguredCharacterConfigAsset();
            SetPrivateField(config, "stateMachine", stateMachine);
            SetPrivateField(config, "movement", movement);
            SetPrivateField(config, "locomotionAnimation", locomotionAnimation);
            SetPrivateField(config, "actionInterruptPolicy", policySet);
            SetPrivateField(config, "bodyClaimPolicy", baseConfig.BodyClaimPolicy);
            SetPrivateField(config, "actionCatalog", actionCatalog);
            SetPrivateField(config, "behaviorRuntimeDefinition", baseConfig.BehaviorRuntimeDefinition);
            SetPrivateField(config, "inputActions", baseConfig.InputActions);
            SetPrivateField(config, "moveAction", baseConfig.MoveAction);
            SetPrivateField(config, "runAction", baseConfig.RunAction);
            SetPrivateField(config, "lookAction", baseConfig.LookAction);
            SetPrivateField(config, "cameraConfig", baseConfig.CameraConfig);
            return config;
        }

        static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(target, value);
        }

        sealed class FakeLocomotionInputSource : MonoBehaviour, IBasicLocomotionInputSource
        {
            public BasicLocomotionInputSnapshot Input { get; set; } =
                new BasicLocomotionInputSnapshot(SimulationTickRate.Default.FixedDeltaSecondsFloat, Vector2.zero, Vector2.zero);

            public bool InputEnabled { get; private set; }
            public int ReadCount { get; private set; }

            public BasicLocomotionInputSnapshot ReadInput(float deltaTime)
            {
                ReadCount++;
                return new BasicLocomotionInputSnapshot(deltaTime, Input.Move, Input.Look, Input.RunHeld);
            }

            public void SetInputEnabled(bool enabled)
            {
                InputEnabled = enabled;
            }
        }

        sealed class FakeCharacterMotionDriver : MonoBehaviour, IBasicLocomotionMotionExecutor, IActionMovementExecutor, IFacingDirectionProvider, IMotionExecutorRollbackStateProvider
        {
            public float CurrentSpeed { get; private set; }
            public Vector3 LastWorldDirection { get; private set; }
            public int BasicMoveCount { get; private set; }
            public int ActionMoveCount { get; private set; }
            public int TotalMoveCount => BasicMoveCount + ActionMoveCount;
            public Vector3 FacingForward => transform.forward;

            public void ExecuteBasicMovement(in MovementCommand command)
            {
                BasicMoveCount++;
                Vector3 inputDisplacement = command.SuppressInputPlanarMovement
                    ? Vector3.zero
                    : command.WorldDirection * command.PlanarSpeed * command.DeltaTime;
                CurrentSpeed = command.DeltaTime > 0f ? inputDisplacement.magnitude / command.DeltaTime : 0f;
                LastWorldDirection = command.WorldDirection;
                transform.position += inputDisplacement;
                ApplyFacing(command.DesiredFacing);
            }

            public void ExecuteActionMovement(in ActionMovementCommand command)
            {
                ActionMoveCount++;
                CurrentSpeed = command.DeltaTime > 0f ? command.PlanarDistance / command.DeltaTime : 0f;
                LastWorldDirection = command.WorldDirection;
                transform.position += command.WorldDirection * command.PlanarDistance;
                if (command.RotateToDirection)
                    ApplyFacing(command.WorldDirection);
            }

            public MotionExecutorRollbackState CaptureRollbackState()
            {
                return new MotionExecutorRollbackState(
                    CurrentSpeed,
                    LastWorldDirection,
                    0f,
                    transform.position,
                    transform.eulerAngles.y,
                    true);
            }

            public void RestoreRollbackState(in MotionExecutorRollbackState state)
            {
                CurrentSpeed = state.CurrentSpeed;
                LastWorldDirection = state.LastWorldDirection;
                if (state.HasRootPose)
                    transform.SetPositionAndRotation(state.RootPosition, Quaternion.Euler(0f, state.RootYaw, 0f));
            }

            void ApplyFacing(Vector3 direction)
            {
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.000001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        sealed class AnimationMotionFakeDriver : IBasicLocomotionMotionExecutor, IMotionExecutorRollbackStateProvider
        {
            readonly Transform root;

            public AnimationMotionFakeDriver(Transform root)
            {
                this.root = root;
            }

            public float CurrentSpeed { get; private set; }
            public Vector3 LastWorldDirection { get; private set; }

            public void ExecuteBasicMovement(in MovementCommand command)
            {
                LastWorldDirection = command.WorldDirection;
                if (root == null)
                    return;

                Vector3 inputDisplacement = command.HasMovement && !command.SuppressInputPlanarMovement
                    ? command.WorldDirection * command.PlanarSpeed * command.DeltaTime
                    : Vector3.zero;
                Vector3 animationWorldDelta = ResolveAnimationWorldDelta(in command);
                root.position += inputDisplacement;
                if (command.HasAnimationMotion)
                {
                    root.rotation = root.rotation * Quaternion.Euler(0f, command.AnimationYawDelta, 0f);
                    root.position += animationWorldDelta;
                }
                if (!command.HasAnimationMotion && !command.SuppressInputRotation && command.DesiredFacing.sqrMagnitude > 0.000001f)
                    root.rotation = Quaternion.LookRotation(command.DesiredFacing, Vector3.up);

                CurrentSpeed = command.DeltaTime > 0f
                    ? (inputDisplacement + animationWorldDelta).magnitude / command.DeltaTime
                    : 0f;
            }

            public MotionExecutorRollbackState CaptureRollbackState()
            {
                return new MotionExecutorRollbackState(
                    CurrentSpeed,
                    LastWorldDirection,
                    0f,
                    root != null ? root.position : Vector3.zero,
                    root != null ? root.eulerAngles.y : 0f,
                    root != null);
            }

            public void RestoreRollbackState(in MotionExecutorRollbackState state)
            {
                CurrentSpeed = state.CurrentSpeed;
                LastWorldDirection = state.LastWorldDirection;
                if (root != null && state.HasRootPose)
                    root.SetPositionAndRotation(state.RootPosition, Quaternion.Euler(0f, state.RootYaw, 0f));
            }

            Vector3 ResolveAnimationWorldDelta(in MovementCommand command)
            {
                if (!command.HasAnimationMotion)
                    return Vector3.zero;

                Vector3 localDelta = command.AnimationLocalPlanarDelta;
                localDelta.y = 0f;
                switch (command.AnimationPlanarDeltaSpace)
                {
                    case BasicMovementPlanarDeltaSpace.World:
                        return localDelta;
                    case BasicMovementPlanarDeltaSpace.EntryLocal:
                        return ResolveEntryLocalWorldDelta(localDelta, command.AnimationPlanarBasisForward);
                    default:
                        return root != null ? root.TransformDirection(localDelta) : localDelta;
                }
            }

            static Vector3 ResolveEntryLocalWorldDelta(Vector3 localDelta, Vector3 entryPlanarBasisForward)
            {
                entryPlanarBasisForward.y = 0f;
                if (entryPlanarBasisForward.sqrMagnitude <= 0.000001f)
                    return Vector3.zero;

                Vector3 forward = entryPlanarBasisForward.normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                return right * localDelta.x + forward * localDelta.z;
            }
        }

        sealed class RollbackStateFakeMotionExecutor : IBasicLocomotionMotionExecutor, IMotionExecutorRollbackStateProvider
        {
            MotionExecutorRollbackState state = MotionExecutorRollbackState.Empty;

            public float CurrentSpeed => state.CurrentSpeed;
            public Vector3 LastWorldDirection => state.LastWorldDirection;

            public void ExecuteBasicMovement(in MovementCommand command)
            {
                state = new MotionExecutorRollbackState(command.PlanarSpeed, command.WorldDirection, state.VerticalVelocity);
            }

            public MotionExecutorRollbackState CaptureRollbackState()
            {
                return state;
            }

            public void RestoreRollbackState(in MotionExecutorRollbackState state)
            {
                this.state = state;
            }
        }

        sealed class ManualLocomotionPlaybackProgress : ILocomotionAnimationPlaybackProgressController
        {
            AnimationPhasePlaybackProgress progress;

            public ManualLocomotionPlaybackProgress(BasicMovementPhase phase, string aliasKey, float normalizedTime)
            {
                LastRestoreGait = BasicMovementGait.Walk;
                Set(phase, aliasKey, normalizedTime);
            }

            public AnimationPhasePlaybackProgress CurrentPlaybackProgress => progress;
            public BasicMovementGait LastRestoreGait { get; private set; }

            public void Set(BasicMovementPhase phase, string aliasKey, float normalizedTime)
            {
                progress = new AnimationPhasePlaybackProgress(phase, aliasKey, normalizedTime, true, normalizedTime >= 1f);
            }

            public bool RestorePlaybackProgress(in AnimationPhasePlaybackProgress restoredProgress)
            {
                progress = restoredProgress;
                return true;
            }

            public bool RestorePlaybackProgress(in AnimationPhasePlaybackProgress restoredProgress, BasicMovementGait gait)
            {
                LastRestoreGait = gait;
                progress = restoredProgress;
                return true;
            }

            public AnimationPhasePlaybackProgress AdvancePlayback(float deltaTime)
            {
                return progress;
            }
        }

        sealed class StepLocomotionPlaybackProgress : ILocomotionAnimationPlaybackProgressController
        {
            AnimationPhasePlaybackProgress progress;
            readonly float step;

            public StepLocomotionPlaybackProgress(BasicMovementPhase phase, string aliasKey, float normalizedTime, float step)
            {
                this.step = Mathf.Max(0f, step);
                LastRestoreGait = BasicMovementGait.Walk;
                Set(phase, aliasKey, normalizedTime);
            }

            public AnimationPhasePlaybackProgress CurrentPlaybackProgress => progress;
            public BasicMovementGait LastRestoreGait { get; private set; }

            public void Set(BasicMovementPhase phase, string aliasKey, float normalizedTime)
            {
                progress = new AnimationPhasePlaybackProgress(phase, aliasKey, normalizedTime, true, normalizedTime >= 1f);
            }

            public bool RestorePlaybackProgress(in AnimationPhasePlaybackProgress restoredProgress)
            {
                progress = restoredProgress;
                return true;
            }

            public bool RestorePlaybackProgress(in AnimationPhasePlaybackProgress restoredProgress, BasicMovementGait gait)
            {
                LastRestoreGait = gait;
                progress = restoredProgress;
                return true;
            }

            public AnimationPhasePlaybackProgress AdvancePlayback(float deltaTime)
            {
                float next = Mathf.Clamp01(progress.NormalizedTime + step);
                progress = new AnimationPhasePlaybackProgress(
                    progress.Phase,
                    progress.AliasKey,
                    next,
                    progress.HasValidPlayback,
                    next >= 1f);
                return progress;
            }
        }

        sealed class FakeLocomotionAnimationPresenter : MonoBehaviour, ILocomotionAnimationPresenter
        {
            ILocomotionAnimationPlaybackProgressController playback;
            AnimationPhasePlaybackProgress progress = AnimationPhasePlaybackProgress.Invalid(BasicMovementPhase.Idle);

            public AnimationPhasePlaybackProgress CurrentPlaybackProgress =>
                playback != null ? playback.CurrentPlaybackProgress : progress;
            public string CurrentAnimationName { get; private set; } = string.Empty;
            public BasicMovementGait LastRestoreGait { get; private set; } = BasicMovementGait.Walk;

            public void SetPlayback(ILocomotionAnimationPlaybackProgressController source)
            {
                playback = source;
                progress = source != null ? source.CurrentPlaybackProgress : AnimationPhasePlaybackProgress.Invalid(BasicMovementPhase.Idle);
                CurrentAnimationName = progress.AliasKey;
            }

            public void Present(in MovementAnimationContext context)
            {
                if (playback != null)
                    return;

                string aliasKey = LocomotionAnimationAliasResolver.ResolveAliasKey(context.AnimationConfig, in context);
                progress = new AnimationPhasePlaybackProgress(context.Phase, aliasKey, 0f, true, false);
                CurrentAnimationName = aliasKey;
            }

            public bool RestorePlaybackProgress(in AnimationPhasePlaybackProgress restoredProgress)
            {
                progress = restoredProgress;
                CurrentAnimationName = restoredProgress.AliasKey;
                if (playback != null)
                    return playback.RestorePlaybackProgress(in restoredProgress);

                return true;
            }

            public bool RestorePlaybackProgress(in AnimationPhasePlaybackProgress restoredProgress, BasicMovementGait gait)
            {
                LastRestoreGait = gait;
                progress = restoredProgress;
                CurrentAnimationName = restoredProgress.AliasKey;
                if (playback != null)
                    return playback.RestorePlaybackProgress(in restoredProgress, gait);

                return true;
            }

            public AnimationPhasePlaybackProgress AdvancePlayback(float deltaTime)
            {
                if (playback != null)
                {
                    progress = playback.AdvancePlayback(deltaTime);
                    CurrentAnimationName = progress.AliasKey;
                    return progress;
                }

                return progress;
            }
        }

        static void AssertPlanarForward(float yaw, Vector3 actual)
        {
            Vector3 expected = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            expected.y = 0f;
            expected.Normalize();
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(0.0001f));
        }

        sealed class FakeActionAnimationPresenter : MonoBehaviour, IActionAnimationPresenter, IActionAnimationPlaybackProgressController, ICharacterAnimationOutputPresenter
        {
            public ActionAnimationKey CurrentKey { get; private set; }
            public ActionAnimationPlaybackIntent PlaybackIntent { get; private set; }
            public float CurrentNormalizedTime { get; private set; }
            public bool HasValidPlayback { get; private set; }
            public ActionAnimationPlaybackProgress CurrentPlaybackProgress =>
                HasValidPlayback
                    ? new ActionAnimationPlaybackProgress(CurrentKey, CurrentNormalizedTime, true, false, PlaybackIntent)
                    : ActionAnimationPlaybackProgress.Invalid;
            public string CurrentAnimationName { get; private set; } = string.Empty;
            public int PresentCount { get; private set; }
            public int SourceStep { get; private set; }
            public CharacterAnimationPlaybackSnapshot CurrentSnapshot => new CharacterAnimationPlaybackSnapshot(
                HasValidPlayback ? CharacterAnimationPlaybackDomain.Action : CharacterAnimationPlaybackDomain.None,
                CurrentAnimationName,
                AnimationPhasePlaybackProgress.Invalid(BasicMovementPhase.Idle),
                string.Empty,
                CurrentPlaybackProgress,
                CurrentAnimationName,
                SourceStep);

            public void PresentLocomotion(in MovementAnimationContext context)
            {
            }

            public bool PresentAction(in CharacterStateAnimationRequest request)
            {
                return Present(in request);
            }

            public void ClearActionPlayback()
            {
                Clear();
            }

            public bool Present(in CharacterStateAnimationRequest request)
            {
                if (!request.HasKey)
                    return false;

                CurrentKey = request.Key;
                PlaybackIntent = request.ActionPlaybackIntent;
                CurrentNormalizedTime = 0f;
                HasValidPlayback = true;
                CurrentAnimationName = request.Key.Value;
                SourceStep = request.SourceStep;
                PresentCount++;
                return true;
            }

            public void Clear()
            {
                CurrentKey = default;
                PlaybackIntent = ActionAnimationPlaybackIntent.Invalid;
                CurrentNormalizedTime = 0f;
                HasValidPlayback = false;
                CurrentAnimationName = string.Empty;
                SourceStep = 0;
            }

            public bool RestorePlaybackProgress(in ActionAnimationPlaybackProgress progress, string animationName)
            {
                if (!progress.HasValidPlayback)
                {
                    Clear();
                    return true;
                }

                CurrentKey = progress.Key;
                PlaybackIntent = progress.PlaybackIntent;
                CurrentNormalizedTime = progress.NormalizedTime;
                HasValidPlayback = true;
                CurrentAnimationName = animationName;
                return true;
            }
        }
    }
}



