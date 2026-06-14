using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonAnimation;
using ThirdPersonCamera;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ThirdPersonSimulation.Tests
{
    public sealed class FullBodyRollbackReplayTests
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
        public void FullBodyRollbackSimulationReplaysMoveRunAndDodgeToSameSnapshot()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
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
        public void FullBodyRollbackSimulationReplaysTurnBackEntryLocalProfileToSameSnapshot()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            RunLocomotionAnimationConfigSO config = ScriptableObject.CreateInstance<RunLocomotionAnimationConfigSO>();
            LocomotionMotionProfileSO profile = ScriptableObject.CreateInstance<LocomotionMotionProfileSO>();
            CharacterConfigSO characterConfig = null;
            StepLocomotionPlaybackProgress playback = new StepLocomotionPlaybackProgress(
                BasicMovementPhase.TurnBack,
                TurnBackMotionPolicy.DefaultAliasKey,
                0.2f,
                0.1f);

            try
            {
                profile.SetBakedData(
                    BasicMovementPhase.TurnBack,
                    BasicMovementGait.Run,
                    TurnBackMotionPolicy.DefaultAliasKey,
                    1f,
                    AnimationCurve.Constant(0f, 1f, 0f),
                    AnimationCurve.Linear(0f, 0f, 1f, 1.2f),
                    AnimationCurve.Linear(0f, 0f, 1f, 180f),
                    TurnBackMotionPolicy.DefaultAliasKey,
                    string.Empty);
                config.SetMotionProfileBindings(new LocomotionPhaseMotionProfileBinding(
                    BasicMovementPhase.TurnBack,
                    BasicMovementGait.Run,
                    TurnBackMotionPolicy.DefaultAliasKey,
                    profile));

                AnimationMotionFakeDriver motionDriver = new AnimationMotionFakeDriver(fixture.Root.transform);
                characterConfig = CreateCharacterConfig(LoadConfiguredStateMachineDefinitionAsset(), LoadConfiguredCharacterConfigAsset().Movement, config);
                fixture.Locomotion.CharacterConfig = characterConfig;
                fixture.Locomotion.SetMotionExecutor(motionDriver);
                fixture.Locomotion.SetAnimationPlaybackProgressSource(playback);
                CharacterSimulationSnapshot restoreSnapshot = CreateTurnBackSnapshot(new SimulationTick(10), 0.2f);
                fixture.Simulation.Restore(in restoreSnapshot);

                PredictionInputHistory inputHistory = new PredictionInputHistory(16);
                PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(16);
                CharacterSimulationSnapshot start = fixture.Simulation.CaptureSnapshot(new SimulationTick(10));
                snapshotHistory.Write(in start);

                for (int tick = 11; tick <= 14; tick++)
                {
                    PredictionInputFrame frame = Input(tick, Vector2.down, true);
                    inputHistory.Write(frame);
                    fixture.Simulation.Advance(in frame);
                    CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(frame.Tick);
                    snapshotHistory.Write(in snapshot);
                }

                playback.Set(BasicMovementPhase.TurnBack, TurnBackMotionPolicy.DefaultAliasKey, 0.95f);
                fixture.Root.transform.SetPositionAndRotation(new Vector3(20f, 0f, 20f), Quaternion.Euler(0f, 90f, 0f));

                LocalRollbackSynctestRunner runner = new LocalRollbackSynctestRunner(inputHistory, snapshotHistory, fixture.Simulation);
                LocalRollbackSynctestResult result = runner.Run(
                    new SimulationTick(10),
                    new SimulationTick(14),
                    new SimulationTick(10),
                    CharacterSimulationSnapshotTolerance.Default);

                AssertSynctestSuccess(in result);
                Assert.False(result.FirstMismatch.HasMismatch);
                Assert.True(result.Comparison.Matches);
                Assert.AreEqual(BasicMovementPhase.TurnBack, SnapshotHistoryLatest(snapshotHistory, new SimulationTick(14)).LocomotionPhase);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(characterConfig);
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void FullBodyRollbackSimulationAdvanceUsesFullBodyActionController()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            PredictionInputFrame frame = Input(7, Vector2.up, true, true);

            fixture.Simulation.Advance(in frame);
            CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(frame.Tick);

            Assert.AreEqual(CharacterStateIds.Dodge, fixture.FullBody.CurrentStateSnapshot.ActiveState);
            Assert.AreEqual(CharacterStateVariant.Directional, fixture.FullBody.CurrentStateSnapshot.Variant);
            Assert.True(snapshot.FullBodyRestoreState.Snapshot.ActiveState.IsValid);
            Assert.AreEqual(CharacterStateIds.Dodge, snapshot.FullBodyRestoreState.Snapshot.ActiveState);
            Assert.AreEqual(7, snapshot.RuntimeBlackboard.Action.SourceStep);
            Assert.True(fixture.ActionPresenter.PresentCount > 0);
        }

        [Test]
        public void FullBodyActionTickAdapterRunsPipelineWithSimulationTickAsSourceStep()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            UnityInputSystemRequestBufferAdapter requestAdapter = fixture.Root.AddComponent<UnityInputSystemRequestBufferAdapter>();
            FullBodyActionTickAdapter adapter = fixture.Root.AddComponent<FullBodyActionTickAdapter>();
            SimulationTickContext context = new SimulationTickContext(
                new SimulationTick(21),
                SimulationTickRate.Default,
                SimulationTickRole.Client);

            fixture.InputSource.Input = new BasicLocomotionInputSnapshot(
                SimulationTickRate.Default.FixedDeltaSecondsFloat,
                Vector2.up,
                Vector2.zero,
                true);
            requestAdapter.BufferComponent = fixture.InputBuffer;
            fixture.InputBuffer.SetStep(21);
            fixture.InputBuffer.Buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 21, 4);
            adapter.FullBodyActionController = fixture.FullBody;
            adapter.RequestBufferAdapter = requestAdapter;
            RunFullBodyAdapterTick(adapter, in context);

            CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(new SimulationTick(21));

            Assert.AreEqual(21, fixture.InputBuffer.CurrentStep);
            Assert.AreEqual(CharacterStateIds.Dodge, fixture.FullBody.CurrentStateSnapshot.ActiveState);
            Assert.AreEqual(21, snapshot.RuntimeBlackboard.Action.SourceStep);
            Assert.AreEqual(21, snapshot.RuntimeBlackboard.Animation.SourceStep);
            Assert.True(adapter.LastFrameResult.Success);
            Assert.True(adapter.LastFrameResult.BasicMovementExecuted || adapter.LastFrameResult.ActionMovementExecuted);
            Assert.True(adapter.LastFrameResult.AnimationFactsWritten);
            Assert.True(adapter.LastFrameResult.SnapshotEventsReady);
        }

        [Test]
        public void FullBodyFramePipelineWritesBufferedInputBeforeGameplayDecision()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            FullBodyFramePipeline pipeline = new FullBodyFramePipeline();
            PredictionInputFrame frame = Input(31, Vector2.up, true, true);
            FullBodyFrameInput input = FullBodyFrameInput.FromPredictionInputFrame(
                in frame,
                SimulationTickRate.Default.FixedDeltaSecondsFloat);
            FullBodyFrameContext context = pipeline.BeginFrame(in input);

            Assert.False(fixture.InputBuffer.Buffer.TryPeek(InputRequestKind.Dodge, 31, out _));

            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.ReadInput, ref context, out _);
            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.UpdateInputBuffer, ref context, out _);

            Assert.True(fixture.InputBuffer.Buffer.TryPeek(InputRequestKind.Dodge, 31, out _));

            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.GameplayDecision, ref context, out _);

            Assert.True(context.InputRequest.HasRequest);
            Assert.AreEqual(InputRequestKind.Dodge, context.InputRequest.RequestKind);
            Assert.AreEqual(CharacterStateIds.Dodge, context.StateFrame.Snapshot.ActiveState);
        }

        [Test]
        public void FullBodyFramePipelineExecutesMotionOnlyInExecuteMotionPhase()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            FullBodyFramePipeline pipeline = new FullBodyFramePipeline();
            FullBodyFrameInput input = FullBodyFrameInput.FromLocomotionInput(
                32,
                new BasicLocomotionInputSnapshot(
                    SimulationTickRate.Default.FixedDeltaSecondsFloat,
                    Vector2.up,
                    Vector2.zero));
            FullBodyFrameContext context = pipeline.BeginFrame(in input);

            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.ReadInput, ref context, out _);
            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.UpdateInputBuffer, ref context, out _);
            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.GameplayDecision, ref context, out _);
            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.BuildMotion, ref context, out _);

            Assert.AreEqual(0, fixture.Driver.TotalMoveCount);

            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.ExecuteMotion, ref context, out _);

            Assert.AreEqual(1, fixture.Driver.TotalMoveCount);

            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.PresentationBridge, ref context, out _);
            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.WriteSnapshotAndEvents, ref context, out _);

            Assert.AreEqual(1, fixture.Driver.TotalMoveCount);
        }

        [Test]
        public void FullBodyFramePipelinePresentsAnimationAfterMotionExecution()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            FullBodyFramePipeline pipeline = new FullBodyFramePipeline();
            PredictionInputFrame frame = Input(33, Vector2.up, true, true);
            FullBodyFrameInput input = FullBodyFrameInput.FromPredictionInputFrame(
                in frame,
                SimulationTickRate.Default.FixedDeltaSecondsFloat);
            FullBodyFrameContext context = pipeline.BeginFrame(in input);

            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.ReadInput, ref context, out _);
            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.UpdateInputBuffer, ref context, out _);
            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.GameplayDecision, ref context, out _);
            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.BuildMotion, ref context, out _);

            Assert.AreEqual(0, fixture.ActionPresenter.PresentCount);

            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.ExecuteMotion, ref context, out _);

            Assert.AreEqual(1, fixture.Driver.ActionMoveCount);
            Assert.AreEqual(0, fixture.ActionPresenter.PresentCount);

            pipeline.RunPhase(fixture.FullBody, SimulationTickPhase.PresentationBridge, ref context, out _);

            Assert.AreEqual(1, fixture.ActionPresenter.PresentCount);
            Assert.True(context.AnimationFactsWritten);
        }

        [Test]
        public void FullBodyActionRequestGateLeavesAttackRequestForFutureComboChange()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Attack, InputButtonKind.Attack, 34, 4);
            FullBodyActionRequestGateInput input = new FullBodyActionRequestGateInput(
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
                default,
                DodgeActionConfig.Default,
                0,
                Array.Empty<ActionInterruptPolicy>());

            FullBodyActionRequestGateResult result = FullBodyActionRequestGate.Evaluate(in input);

            Assert.False(result.Accepted);
            Assert.False(result.Request.HasRequest);
            Assert.True(buffer.TryPeek(InputRequestKind.Attack, 34, out _));
        }

        [Test]
        public void FullBodyCompatibleTickMatchesPhasePipelineForMove()
        {
            FullBodyFrameInput input = FullBodyFrameInput.FromLocomotionInput(
                35,
                new BasicLocomotionInputSnapshot(
                    SimulationTickRate.Default.FixedDeltaSecondsFloat,
                    Vector2.up,
                    Vector2.zero));

            AssertCompatibleTickMatchesPhasePipeline(in input);
        }

        [Test]
        public void FullBodyCompatibleTickMatchesPhasePipelineForDirectionalDodge()
        {
            PredictionInputFrame frame = Input(36, Vector2.up, true, true);
            FullBodyFrameInput input = FullBodyFrameInput.FromPredictionInputFrame(
                in frame,
                SimulationTickRate.Default.FixedDeltaSecondsFloat);

            AssertCompatibleTickMatchesPhasePipeline(in input);
        }

        [Test]
        public void FullBodyCompatibleTickMatchesPhasePipelineForBackstepDodge()
        {
            PredictionInputFrame frame = Input(37, Vector2.zero, false, true);
            FullBodyFrameInput input = FullBodyFrameInput.FromPredictionInputFrame(
                in frame,
                SimulationTickRate.Default.FixedDeltaSecondsFloat);

            AssertCompatibleTickMatchesPhasePipeline(in input);
        }

        [Test]
        public void FullBodyCompatibleTickWithoutTickDriverExecutesMotionOnce()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            FullBodyFrameInput input = FullBodyFrameInput.FromLocomotionInput(
                38,
                new BasicLocomotionInputSnapshot(
                    SimulationTickRate.Default.FixedDeltaSecondsFloat,
                    Vector2.up,
                    Vector2.zero));

            Assert.True(fixture.FullBody.Tick(in input));

            Assert.AreEqual(1, fixture.Driver.TotalMoveCount);
            Assert.True(fixture.FullBody.LastFramePipelineResult.Success);
        }

        [Test]
        public void FullBodyActionTickAdapterRegisterDisablesFullBodyAutoUpdate()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            UnitySimulationTickDriver tickDriver = fixture.Root.AddComponent<UnitySimulationTickDriver>();
            FullBodyActionTickAdapter adapter = fixture.Root.AddComponent<FullBodyActionTickAdapter>();

            fixture.FullBody.AutoUpdate = true;
            adapter.TickDriver = tickDriver;
            adapter.FullBodyActionController = fixture.FullBody;

            Assert.True(adapter.Register());
            Assert.False(fixture.FullBody.AutoUpdate);

            adapter.Unregister();
            Assert.True(fixture.FullBody.AutoUpdate);
        }

        [Test]
        public void FullBodyActionTickAdapterIgnoresUnregisteredRetiredLocomotionTickAdapter()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            UnitySimulationTickDriver tickDriver = fixture.Root.AddComponent<UnitySimulationTickDriver>();
            LocomotionTickAdapter locomotionAdapter = fixture.Root.AddComponent<LocomotionTickAdapter>();
            FullBodyActionTickAdapter fullBodyAdapter = fixture.Root.AddComponent<FullBodyActionTickAdapter>();

            fixture.InputSource.Input = new BasicLocomotionInputSnapshot(
                SimulationTickRate.Default.FixedDeltaSecondsFloat,
                Vector2.up,
                Vector2.zero,
                true);
            locomotionAdapter.TickDriver = tickDriver;
            locomotionAdapter.LocomotionController = fixture.Locomotion;
            fullBodyAdapter.TickDriver = tickDriver;
            fullBodyAdapter.FullBodyActionController = fixture.FullBody;

            LogAssert.Expect(LogType.Error, new Regex(".*locomotion-tick-adapter-retired.*"));
            Assert.False(locomotionAdapter.Register());

            Assert.False(locomotionAdapter.IsRegistered);
            Assert.True(fullBodyAdapter.Register());
            Assert.True(fullBodyAdapter.IsRegistered);

            fullBodyAdapter.Unregister();
            locomotionAdapter.Unregister();
        }

        [Test]
        public void FullBodyRollbackSimulationRestoreRestoresFullBodyStateAndInputBuffer()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            PredictionInputFrame frame = Input(5, Vector2.up, true, true);
            fixture.Simulation.Advance(in frame);
            CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(frame.Tick);

            PredictionInputFrame later = Input(6, Vector2.zero);
            fixture.Simulation.Advance(in later);
            fixture.InputBuffer.Clear();
            fixture.InputBuffer.SetStep(20);

            fixture.Simulation.Restore(in snapshot);

            Assert.AreEqual(5, fixture.InputBuffer.CurrentStep);
            Assert.AreEqual(snapshot.FullBodyRestoreState.Snapshot.ActiveState, fixture.FullBody.CurrentStateSnapshot.ActiveState);
            Assert.AreEqual(snapshot.FullBodyRestoreState.Snapshot.StateTime, fixture.FullBody.CurrentStateSnapshot.StateTime, 0.0001f);
            Assert.False(fixture.InputBuffer.Buffer.TryPeek(InputRequestKind.Dodge, 5, out _));
            Assert.AreEqual(snapshot.InputBufferRestoreState.Buffer.Requests.Count, fixture.InputBuffer.Buffer.Count);
        }

        [Test]
        public void FullBodyRollbackSimulationRestoreRestoresActionVariantAndDirection()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            PredictionInputFrame frame = Input(5, Vector2.right, true, true);
            fixture.Simulation.Advance(in frame);
            CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(frame.Tick);
            Vector3 expectedDirection = snapshot.FullBodyRestoreState.StateMachine.ActionWorldDirection;

            Assert.AreEqual(CharacterStateVariant.Directional, snapshot.FullBodyRestoreState.Snapshot.Variant);
            Assert.AreEqual(Vector3.right, expectedDirection);

            FullBodyActionRestoreState divergent = CreateActionRestoreState(
                CharacterStateVariant.Backstep,
                Vector3.back,
                0.5f);
            Assert.True(fixture.FullBody.Restore(in divergent));
            Assert.AreEqual(CharacterStateVariant.Backstep, fixture.FullBody.CurrentStateSnapshot.Variant);

            fixture.Simulation.Restore(in snapshot);
            FullBodyActionRestoreState restored = fixture.FullBody.CaptureRestoreState();

            Assert.AreEqual(CharacterStateVariant.Directional, fixture.FullBody.CurrentStateSnapshot.Variant);
            Assert.AreEqual(CharacterStateVariant.Directional, restored.Snapshot.Variant);
            Assert.AreEqual(expectedDirection, restored.StateMachine.ActionWorldDirection);
        }

        [Test]
        public void FullBodyRollbackSimulationRestoreRestoresActionAnimationPlayback()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
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
        public void FullBodyRollbackSimulationRestorePassesLocomotionGaitToPlayback()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            ManualLocomotionPlaybackProgress playback = new ManualLocomotionPlaybackProgress(
                BasicMovementPhase.TurnBack,
                TurnBackMotionPolicy.DefaultAliasKey,
                0.9f);
            fixture.Locomotion.SetAnimationPlaybackProgressSource(playback);
            CharacterSimulationSnapshot snapshot = CreateTurnBackSnapshot(new SimulationTick(12), 0.35f);

            fixture.Simulation.Restore(in snapshot);

            Assert.AreEqual(BasicMovementGait.Run, playback.LastRestoreGait);
            Assert.AreEqual(0.35f, playback.CurrentPlaybackProgress.NormalizedTime, 0.0001f);
            Assert.AreEqual(BasicMovementPhase.TurnBack, playback.CurrentPlaybackProgress.Phase);
            Assert.AreEqual(TurnBackMotionPolicy.DefaultAliasKey, playback.CurrentPlaybackProgress.AliasKey);
        }

        [Test]
        public void MotionExecutorRollbackStateRoundTripsThroughLocomotionSnapshot()
        {
            GameObject gameObject = new GameObject("rollback-motion-executor-state-test");
            gameObject.SetActive(false);

            try
            {
                PlayerLocomotionController controller = AddConfiguredLocomotionController(gameObject);
                RollbackStateFakeMotionExecutor executor = new RollbackStateFakeMotionExecutor();
                controller.SetMotionExecutor(executor);
                executor.RestoreRollbackState(new MotionExecutorRollbackState(5f, Vector3.right, -3f));

                CharacterSimulationSnapshot snapshot = controller.CaptureSimulationSnapshot(new SimulationTick(4));
                executor.RestoreRollbackState(MotionExecutorRollbackState.Empty);

                Assert.True(controller.RestoreSimulationSnapshot(in snapshot));
                MotionExecutorRollbackState restored = executor.CaptureRollbackState();

                Assert.AreEqual(5f, restored.CurrentSpeed, 0.0001f);
                Assert.AreEqual(Vector3.right, restored.LastWorldDirection);
                Assert.AreEqual(-3f, restored.VerticalVelocity, 0.0001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
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
        public void DebugRunnerAcceptsFullBodyRollbackSimulationBehaviour()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            PredictionInputHistoryTickRecorder inputRecorder = fixture.Root.AddComponent<PredictionInputHistoryTickRecorder>();
            LocomotionSnapshotHistoryRecorder snapshotRecorder = fixture.Root.AddComponent<LocomotionSnapshotHistoryRecorder>();
            LocalRollbackSynctestDebugRunner debugRunner = fixture.Root.AddComponent<LocalRollbackSynctestDebugRunner>();

            debugRunner.InputRecorder = inputRecorder;
            debugRunner.SnapshotRecorder = snapshotRecorder;
            debugRunner.SimulationBehaviour = fixture.Simulation;
            debugRunner.RunOnKeyDown = false;

            Assert.AreSame(fixture.Simulation, debugRunner.Simulation);
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
                    Vector3.zero, false, false, false, false),
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
                    Vector3.zero, false, false, false, false),
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
                    Vector3.zero, false, false, false, false),
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
                .WithFullBodyState(FullBodyActionRestoreState.Inactive, InputRequestBufferComponentRestoreState.Empty);

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
        public void WithFullBodyStatePreservesCameraBasis()
        {
            RollbackCameraBasisState basis = new RollbackCameraBasisState(Vector3.right, Vector3.back, 120f);
            CharacterSimulationSnapshot snapshot = new CharacterSimulationSnapshot(
                SimulationTick.Zero,
                Vector3.zero,
                0f,
                new CharacterStateMachineRestoreState(
                    CharacterStateMachineSnapshot.Inactive,
                    Vector3.zero, false, false, false, false),
                false,
                BasicMovementGait.Walk,
                Vector3.zero,
                BasicMovementPhase.Idle,
                BasicMovementGait.Walk,
                string.Empty,
                0f)
                .WithCameraBasis(in basis);

            CharacterSimulationSnapshot enriched = snapshot.WithFullBodyState(
                FullBodyActionRestoreState.Inactive,
                InputRequestBufferComponentRestoreState.Empty);

            Assert.AreEqual(120f, enriched.CameraBasisState.Yaw, 0.0001f);
        }

        [Test]
        public void FullBodyRollbackSimulationRestoresRollbackCameraBasisWithoutMutatingCameraController()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            fixture.Camera.ResetState(45f, 0f);
            fixture.Simulation.Advance(Input(1, Vector2.up));

            CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(new SimulationTick(1));

            Assert.AreEqual(45f, snapshot.CameraBasisState.Yaw, 0.0001f);
            Assert.True(snapshot.FullBodyRestoreState.Snapshot.ActiveState.IsValid);

            fixture.Camera.ResetState(180f, 0f);
            fixture.Simulation.Advance(Input(2, Vector2.zero));

            fixture.Simulation.Restore(in snapshot);
            CharacterSimulationSnapshot restored = fixture.Simulation.CaptureSnapshot(new SimulationTick(1));

            Assert.AreEqual(snapshot.CameraBasisState.Yaw, restored.CameraBasisState.Yaw, 0.0001f);
            Assert.AreEqual(snapshot.Position, restored.Position);
            Assert.AreEqual(180f, fixture.Camera.Yaw, 0.0001f);
            AssertPlanarForward(45f, fixture.FullBody.LocomotionController.RollbackCameraBasisProvider.CameraPlanarForward);
        }

        [Test]
        public void FullBodyRollbackSimulationResolvesCameraBasisWhenReferenceIsMissing()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            fixture.FullBody.LocomotionController.CameraController = null;
            fixture.FullBody.LocomotionController.RollbackCameraBasisProvider.Override(
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
                fixture.FullBody.LocomotionController.CurrentWorldDirection),
                Is.LessThan(0.0001f));

            fixture.Camera.ResetState(0f, 0f);
            fixture.Simulation.Restore(in snapshot);

            Assert.AreEqual(0f, fixture.Camera.Yaw, 0.0001f);
            AssertPlanarForward(280f, fixture.FullBody.LocomotionController.RollbackCameraBasisProvider.CameraPlanarForward);
        }

        [Test]
        public void FullBodyRollbackSimulationReplaysCameraLookRelativeMovement()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
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
        public void FullBodyRollbackSimulationUsesInputCameraBasisForReplayMovement()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
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
        public void FullBodyRollbackReplayDoesNotMoveLocalCameraTargets()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
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
        public void FullBodyRollbackSnapshotUsesLatestLocomotionFramePhase()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();

            for (int tick = 1; tick <= 8; tick++)
                fixture.Simulation.Advance(Input(tick, Vector2.up));
            fixture.Simulation.Advance(Input(9, Vector2.zero));

            CharacterSimulationSnapshot snapshot = fixture.Simulation.CaptureSnapshot(new SimulationTick(9));

            Assert.AreEqual(BasicMovementPhase.MoveStop, snapshot.LocomotionPhase);
            Assert.AreEqual(BasicMovementPhase.MoveStop, snapshot.RuntimeBlackboard.Animation.LocomotionProgress.Phase);
            Assert.AreEqual(snapshot.RuntimeBlackboard.Animation.LocomotionProgress.AliasKey, snapshot.AnimationKey);
        }

        [Test]
        public void FullBodyRollbackRestorePreservesLocomotionAnimationPhaseDuringAction()
        {
            using FullBodyRollbackFixture fixture = FullBodyRollbackFixture.Create();
            fixture.Locomotion.SetAnimationPlaybackProgressSource(new ManualLocomotionPlaybackProgress(
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
        public void LocomotionRestoreSeedsAnimationMotionPlaybackWindow()
        {
            GameObject root = new GameObject("locomotion-animation-restore-window-test");
            RunLocomotionAnimationConfigSO config = ScriptableObject.CreateInstance<RunLocomotionAnimationConfigSO>();
            LocomotionMotionProfileSO profile = ScriptableObject.CreateInstance<LocomotionMotionProfileSO>();
            CharacterConfigSO characterConfig = null;

            try
            {
                PlayerLocomotionController controller = AddConfiguredLocomotionController(root);
                AnimationMotionFakeDriver driver = new AnimationMotionFakeDriver(root.transform);
                ManualLocomotionPlaybackProgress playback = new ManualLocomotionPlaybackProgress(
                    BasicMovementPhase.MoveLoop,
                    "RunLoop",
                    0.4f);

                profile.SetBakedData(
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    "RunLoop",
                    1f,
                    AnimationCurve.Constant(0f, 1f, 0f),
                    AnimationCurve.Linear(0f, 0f, 1f, 10f),
                    AnimationCurve.Linear(0f, 0f, 1f, 90f),
                    "RunLoop",
                    string.Empty);
                config.SetMotionProfileBindings(new LocomotionPhaseMotionProfileBinding(
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    "RunLoop",
                    profile));

                characterConfig = CreateCharacterConfig(LoadConfiguredStateMachineDefinitionAsset(), LoadConfiguredCharacterConfigAsset().Movement, config);
                controller.CharacterConfig = characterConfig;
                controller.SetMotionExecutor(driver);
                controller.SetAnimationPlaybackProgressSource(playback);
                CharacterSimulationSnapshot snapshot = CreateMoveLoopSnapshot(new SimulationTick(2), "RunLoop", 0.4f);
                CharacterStateMachineRunner runner = new CharacterStateMachineRunner(LoadConfiguredStateMachineDefinitionAsset().ToDefinition());
                Assert.True(runner.Restore(snapshot.FullBodyRestoreState.StateMachine));

                root.transform.SetPositionAndRotation(new Vector3(30f, 0f, 30f), Quaternion.Euler(0f, 180f, 0f));
                playback.Set(BasicMovementPhase.MoveLoop, "RunLoop", 0.9f);

                Assert.True(controller.RestoreSimulationSnapshot(in snapshot));
                playback.Set(BasicMovementPhase.MoveLoop, "RunLoop", 0.6f);
                Assert.True(controller.TryEvaluateWithStateMachine(
                    new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero, true),
                    runner,
                    CharacterInputRequestFact.None(InputRequestKind.Dodge),
                    3,
                    out BasicLocomotionFrame frame,
                    out _));

                MovementCommand command = frame.Command;
                Assert.True(command.HasAnimationMotion);
                Assert.AreEqual("RunLoop", command.AnimationMotionSourceAliasKey);
                Assert.AreEqual(2f, command.AnimationLocalPlanarDelta.z, 0.001f);
                Assert.AreEqual(18f, command.AnimationYawDelta, 0.01f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(characterConfig);
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(config);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FullBodyRestoreStateDoesNotStoreUnityObjects()
        {
            var unityObjectProperties = typeof(FullBodyActionRestoreState)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => typeof(UnityEngine.Object).IsAssignableFrom(property.PropertyType))
                .Select(property => property.Name)
                .ToArray();

            Assert.IsEmpty(unityObjectProperties);
        }

        [Test]
        public void FullBodyActionRestoreStateSeparatesGameplayAndDiagnosticState()
        {
            CharacterStateMachineRestoreState stateMachine = new CharacterStateMachineRestoreState(
                new CharacterStateMachineSnapshot(
                    CharacterStateIds.Dodge,
                    0.25f,
                    CharacterStateVariant.Directional,
                    "Action/Dodge",
                    Array.Empty<CharacterStateTag>()),
                Vector3.forward,
                true,
                true,
                false,
                true);
            FullBodyActionGameplayRestoreState gameplay = new FullBodyActionGameplayRestoreState(stateMachine);
            FullBodyActionDiagnosticRestoreState diagnostic = new FullBodyActionDiagnosticRestoreState(
                "FullBody/Action/Dodge",
                "pending",
                "last-full",
                "last-pending",
                "FullBody/Locomotion/MoveLoop",
                BasicMovementPhase.MoveLoop,
                false);

            FullBodyActionRestoreState restoreState = new FullBodyActionRestoreState(gameplay, diagnostic);

            Assert.AreEqual(CharacterStateIds.Dodge, restoreState.Gameplay.Snapshot.ActiveState);
            Assert.AreEqual(Vector3.forward, restoreState.Gameplay.StateMachine.ActionWorldDirection);
            Assert.AreEqual("FullBody/Action/Dodge", restoreState.Diagnostic.DebugFullBodyStatePath);
            Assert.AreEqual("last-full", restoreState.Diagnostic.LastLoggedFullBodyPath);
            Assert.False(restoreState.Diagnostic.LoggedInitialLocomotionState);
            Assert.AreEqual(restoreState.Gameplay.Snapshot.ActiveState, restoreState.Snapshot.ActiveState);
        }

        [Test]
        public void SnapshotComparisonIgnoresFullBodyDiagnosticRestoreState()
        {
            CharacterStateMachineRestoreState stateMachine = new CharacterStateMachineRestoreState(
                new CharacterStateMachineSnapshot(
                    CharacterStateIds.Dodge,
                    0.25f,
                    CharacterStateVariant.Directional,
                    string.Empty,
                    Array.Empty<CharacterStateTag>()),
                Vector3.forward,
                true,
                true,
                false,
                true);
            FullBodyActionGameplayRestoreState gameplay = new FullBodyActionGameplayRestoreState(stateMachine);
            CharacterSimulationSnapshot expected = SnapshotWithFullBody(
                new FullBodyActionRestoreState(
                    gameplay,
                    new FullBodyActionDiagnosticRestoreState(
                        "debug-a",
                        "pending-a",
                        "full-a",
                        "transition-a",
                        "locomotion-a",
                        BasicMovementPhase.MoveLoop,
                        true)));
            CharacterSimulationSnapshot actual = SnapshotWithFullBody(
                new FullBodyActionRestoreState(
                    gameplay,
                    new FullBodyActionDiagnosticRestoreState(
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
        public void FullBodyRollbackSimulationDoesNotCaptureOrRestoreRealCameraState()
        {
            string sourcePath = Path.Combine(Application.dataPath, "Scripts/Simulation/Rollback/FullBodyRollbackSimulation.cs");
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
        public void FullBodyRollbackCoreDoesNotReferenceForbiddenIntegrationTypes()
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

        static CharacterSimulationSnapshot SnapshotWithFullBody(FullBodyActionRestoreState fullBodyRestoreState)
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
                0f).WithFullBodyState(fullBodyRestoreState, InputRequestBufferComponentRestoreState.Empty);
        }

        static FullBodyActionRestoreState CreateActionRestoreState(
            CharacterStateVariant variant,
            Vector3 actionWorldDirection,
            float stateTime)
        {
            CharacterStateMachineSnapshot snapshot = new CharacterStateMachineSnapshot(
                CharacterStateIds.Dodge,
                stateTime,
                variant,
                "FullBody/Action/Dodge",
                Array.Empty<CharacterStateTag>());
            CharacterStateMachineRestoreState state = new CharacterStateMachineRestoreState(
                snapshot,
                actionWorldDirection,
                true,
                false,
                false,
                false);
            return new FullBodyActionRestoreState(
                new FullBodyActionGameplayRestoreState(state),
                FullBodyActionDiagnosticRestoreState.Empty);
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
                "FullBody/Locomotion/TurnBack",
                Array.Empty<CharacterStateTag>());
            CharacterStateMachineRestoreState state = new CharacterStateMachineRestoreState(
                stateSnapshot,
                Vector3.zero,
                Vector3.back,
                Vector3.forward,
                true,
                false,
                false,
                false);
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
            FullBodyActionRestoreState fullBodyState = new FullBodyActionRestoreState(
                new FullBodyActionGameplayRestoreState(state),
                FullBodyActionDiagnosticRestoreState.Empty);

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
                .WithFullBodyState(in fullBodyState, InputRequestBufferComponentRestoreState.Empty)
                .WithMotionExecutorState(new MotionExecutorRollbackState(0f, Vector3.back, 0f, Vector3.zero, 0f, true));
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
                "FullBody/Locomotion/MoveLoop",
                Array.Empty<CharacterStateTag>());
            CharacterStateMachineRestoreState state = new CharacterStateMachineRestoreState(
                stateSnapshot,
                Vector3.zero,
                false,
                false,
                false,
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
            FullBodyActionRestoreState fullBodyState = new FullBodyActionRestoreState(
                new FullBodyActionGameplayRestoreState(state),
                FullBodyActionDiagnosticRestoreState.Empty);

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
                .WithFullBodyState(in fullBodyState, InputRequestBufferComponentRestoreState.Empty)
                .WithMotionExecutorState(new MotionExecutorRollbackState(0f, Vector3.forward, 0f, Vector3.zero, 0f, true));
        }

        static CharacterSimulationSnapshot SnapshotHistoryLatest(PredictionSnapshotHistory history, SimulationTick tick)
        {
            Assert.True(history.TryGet(tick, out CharacterSimulationSnapshot snapshot));
            return snapshot;
        }

        static void RunFullBodyAdapterTick(FullBodyActionTickAdapter adapter, in SimulationTickContext context)
        {
            adapter.Tick(SimulationTickPhase.ReadInput, in context);
            adapter.Tick(SimulationTickPhase.UpdateInputBuffer, in context);
            adapter.Tick(SimulationTickPhase.GameplayDecision, in context);
            adapter.Tick(SimulationTickPhase.BuildMotion, in context);
            adapter.Tick(SimulationTickPhase.ExecuteMotion, in context);
            adapter.Tick(SimulationTickPhase.PresentationBridge, in context);
            adapter.Tick(SimulationTickPhase.WriteSnapshotAndEvents, in context);
        }

        static void AssertCompatibleTickMatchesPhasePipeline(in FullBodyFrameInput input)
        {
            using FullBodyRollbackFixture compatible = FullBodyRollbackFixture.Create();
            using FullBodyRollbackFixture phased = FullBodyRollbackFixture.Create();
            FullBodyFramePipeline pipeline = new FullBodyFramePipeline();
            FullBodyFrameContext context = pipeline.BeginFrame(in input);

            Assert.True(compatible.FullBody.Tick(in input));
            pipeline.RunPhase(phased.FullBody, SimulationTickPhase.ReadInput, ref context, out _);
            pipeline.RunPhase(phased.FullBody, SimulationTickPhase.UpdateInputBuffer, ref context, out _);
            pipeline.RunPhase(phased.FullBody, SimulationTickPhase.GameplayDecision, ref context, out _);
            pipeline.RunPhase(phased.FullBody, SimulationTickPhase.BuildMotion, ref context, out _);
            pipeline.RunPhase(phased.FullBody, SimulationTickPhase.ExecuteMotion, ref context, out _);
            pipeline.RunPhase(phased.FullBody, SimulationTickPhase.PresentationBridge, ref context, out _);
            pipeline.RunPhase(phased.FullBody, SimulationTickPhase.WriteSnapshotAndEvents, ref context, out FullBodyFrameResult phaseResult);

            FullBodyFrameResult compatibleResult = compatible.FullBody.LastFramePipelineResult;
            Assert.True(phaseResult.Success);
            Assert.AreEqual(phaseResult.StateFrame.Snapshot.ActiveState, compatible.FullBody.CurrentStateSnapshot.ActiveState);
            Assert.AreEqual(phaseResult.StateFrame.Snapshot.Variant, compatible.FullBody.CurrentStateSnapshot.Variant);
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

        sealed class FullBodyRollbackFixture : IDisposable
        {
            FullBodyRollbackFixture(
                GameObject root,
                FullBodyRollbackSimulation simulation,
                PlayerFullBodyActionController fullBody,
                PlayerLocomotionController locomotion,
                ThirdPersonCameraController camera,
                InputRequestBufferComponent inputBuffer,
                FakeLocomotionInputSource inputSource,
                FakeFullBodyDriver driver,
                FakeActionAnimationPresenter actionPresenter,
                ScriptableObject[] assets)
            {
                Root = root;
                Simulation = simulation;
                FullBody = fullBody;
                Locomotion = locomotion;
                Camera = camera;
                InputBuffer = inputBuffer;
                InputSource = inputSource;
                Driver = driver;
                ActionPresenter = actionPresenter;
                Assets = assets;
            }

            public GameObject Root { get; }
            public FullBodyRollbackSimulation Simulation { get; }
            public PlayerFullBodyActionController FullBody { get; }
            public PlayerLocomotionController Locomotion { get; }
            public ThirdPersonCameraController Camera { get; }
            public InputRequestBufferComponent InputBuffer { get; }
            public FakeLocomotionInputSource InputSource { get; }
            public FakeFullBodyDriver Driver { get; }
            public FakeActionAnimationPresenter ActionPresenter { get; }
            ScriptableObject[] Assets { get; }

            public static FullBodyRollbackFixture Create()
            {
                GameObject root = new GameObject("fullbody-rollback-fixture");
                root.SetActive(false);

                InputRequestBufferComponent inputBuffer = root.AddComponent<InputRequestBufferComponent>();
                FakeLocomotionInputSource inputSource = root.AddComponent<FakeLocomotionInputSource>();
                PlayerLocomotionController locomotion = AddConfiguredLocomotionController(root);
                ThirdPersonCameraController camera = root.AddComponent<ThirdPersonCameraController>();
                FakeFullBodyDriver driver = root.AddComponent<FakeFullBodyDriver>();
                FakeActionAnimationPresenter actionPresenter = root.AddComponent<FakeActionAnimationPresenter>();
                PlayerFullBodyActionController fullBody = root.AddComponent<PlayerFullBodyActionController>();
                FullBodyRollbackSimulation simulation = root.AddComponent<FullBodyRollbackSimulation>();
                ActionInterruptPolicySetSO policySet = CreatePolicySet();
                DodgeActionConfigSO dodgeConfig = ScriptableObject.CreateInstance<DodgeActionConfigSO>();

                camera.AutoTick = false;
                camera.DebugLog = false;
                camera.ResetState(0f, 0f);
                locomotion.AutoUpdate = false;
                locomotion.CameraController = camera;
                locomotion.SetMotionExecutor(driver);
                locomotion.SetInputSource(inputSource);
                fullBody.AutoUpdate = false;
                fullBody.CharacterConfig = locomotion.CharacterConfig;
                fullBody.LocomotionController = locomotion;
                fullBody.InputBufferComponent = inputBuffer;
                fullBody.ActionMovementExecutorBehaviour = driver;
                fullBody.FacingProviderBehaviour = driver;
                fullBody.AnimationPresenterBehaviour = actionPresenter;
                fullBody.InterruptPolicySet = policySet;
                fullBody.DodgeActionConfigAsset = dodgeConfig;
                simulation.FullBodyActionController = fullBody;
                simulation.LocomotionController = locomotion;
                simulation.InputBufferComponent = inputBuffer;

                return new FullBodyRollbackFixture(
                    root,
                    simulation,
                    fullBody,
                    locomotion,
                    camera,
                    inputBuffer,
                    inputSource,
                    driver,
                    actionPresenter,
                    new ScriptableObject[] { policySet, dodgeConfig });
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
        }

        static PlayerLocomotionController AddConfiguredLocomotionController(GameObject gameObject)
        {
            PlayerLocomotionController controller = gameObject.AddComponent<PlayerLocomotionController>();
            controller.CharacterConfig = LoadConfiguredCharacterConfigAsset();
            return controller;
        }

        static CharacterConfigSO LoadConfiguredCharacterConfigAsset()
        {
            CharacterConfigSO asset = AssetDatabase.LoadAssetAtPath<CharacterConfigSO>(
                "Assets/Configs/3C/CharacterConfig.asset");
            Assert.NotNull(asset);
            Assert.NotNull(asset.StateMachine);
            Assert.NotNull(asset.Movement);
            return asset;
        }

        static CharacterStateMachineDefinitionSO LoadConfiguredStateMachineDefinitionAsset()
        {
            CharacterStateMachineDefinitionSO asset = AssetDatabase.LoadAssetAtPath<CharacterStateMachineDefinitionSO>(
                "Assets/Configs/3C/Statemachine/DefaultCharacterStateMachine.asset");
            Assert.NotNull(asset);
            return asset;
        }

        static CharacterConfigSO CreateCharacterConfig(
            CharacterStateMachineDefinitionSO stateMachine,
            BasicMovementConfigSO movement,
            RunLocomotionAnimationConfigSO locomotionAnimation)
        {
            CharacterConfigSO config = ScriptableObject.CreateInstance<CharacterConfigSO>();
            SetPrivateField(config, "stateMachine", stateMachine);
            SetPrivateField(config, "movement", movement);
            SetPrivateField(config, "locomotionAnimation", locomotionAnimation);
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

        sealed class FakeFullBodyDriver : MonoBehaviour, IBasicLocomotionMotionExecutor, IActionMovementExecutor, IFacingDirectionProvider, IMotionExecutorRollbackStateProvider
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

        static void AssertPlanarForward(float yaw, Vector3 actual)
        {
            Vector3 expected = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            expected.y = 0f;
            expected.Normalize();
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(0.0001f));
        }

        sealed class FakeActionAnimationPresenter : MonoBehaviour, IActionAnimationPresenter, IActionAnimationPlaybackProgressController
        {
            public ActionAnimationKey CurrentKey { get; private set; }
            public float CurrentNormalizedTime { get; private set; }
            public bool HasValidPlayback { get; private set; }
            public ActionAnimationPlaybackProgress CurrentPlaybackProgress =>
                HasValidPlayback
                    ? new ActionAnimationPlaybackProgress(CurrentKey, CurrentNormalizedTime, true, false)
                    : ActionAnimationPlaybackProgress.Invalid;
            public string CurrentAnimationName { get; private set; } = string.Empty;
            public int PresentCount { get; private set; }

            public bool Present(in CharacterStateAnimationRequest request)
            {
                if (!request.HasKey)
                    return false;

                CurrentKey = request.Key;
                CurrentNormalizedTime = 0f;
                HasValidPlayback = true;
                CurrentAnimationName = request.Key.Value;
                PresentCount++;
                return true;
            }

            public void Clear()
            {
                CurrentKey = default;
                CurrentNormalizedTime = 0f;
                HasValidPlayback = false;
                CurrentAnimationName = string.Empty;
            }

            public bool RestorePlaybackProgress(in ActionAnimationPlaybackProgress progress, string animationName)
            {
                if (!progress.HasValidPlayback)
                {
                    Clear();
                    return true;
                }

                CurrentKey = progress.Key;
                CurrentNormalizedTime = progress.NormalizedTime;
                HasValidPlayback = true;
                CurrentAnimationName = animationName;
                return true;
            }
        }
    }
}
