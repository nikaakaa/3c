using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonAnimation;
using ThirdPersonCamera;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonDiagnostics;
using ThirdPersonMovement;
using ThirdPersonPresentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonSimulation.Tests
{
    public sealed class LocalRollbackSynctestFoundationTests
    {
        [Test]
        public void PredictionInputFrameClampsAxesAndBuildsLocomotionInput()
        {
            PredictionInputFrame frame = new PredictionInputFrame(
                new SimulationTick(4),
                new Vector2(2f, 0f),
                new Vector2(float.NaN, -5f),
                true,
                new PredictionButtonFrame(true, true, false),
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None);

            Assert.AreEqual(new SimulationTick(4), frame.Tick);
            Assert.That(frame.Move.magnitude, Is.LessThanOrEqualTo(1.0001f));
            Assert.AreEqual(0f, frame.Look.x);
            Assert.AreEqual(-1f, frame.Look.y);
            Assert.True(frame.Dodge.Pressed);

            BasicLocomotionInputSnapshot input = frame.ToLocomotionInput(0.016f);
            Assert.AreEqual(frame.Move, input.Move);
            Assert.AreEqual(frame.Look, input.Look);
            Assert.True(input.RunHeld);
        }

        [Test]
        public void PredictionInputFrameStoresOptionalCameraBasisFact()
        {
            RollbackCameraBasisState basis = new RollbackCameraBasisState(Vector3.right, Vector3.back, 90f);
            PredictionInputFrame frame = new PredictionInputFrame(
                new SimulationTick(5),
                Vector2.right,
                Vector2.zero,
                false,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                basis);

            Assert.True(frame.HasCameraBasis);
            Assert.AreEqual(90f, frame.CameraBasisState.Yaw, 0.0001f);
            Assert.AreEqual(Vector3.right, frame.CameraBasisState.PlanarForward);

            PredictionInputFrame legacy = Input(6, Vector2.up);
            Assert.False(legacy.HasCameraBasis);
            Assert.AreEqual(0f, legacy.CameraBasisState.Yaw, 0.0001f);
        }

        [Test]
        public void PredictionInputHistoryWritesOverridesReadsAndTrims()
        {
            PredictionInputHistory history = new PredictionInputHistory(3);

            history.Write(Input(1, Vector2.right));
            history.Write(Input(2, Vector2.up));
            history.Write(Input(3, Vector2.left));
            history.Write(Input(2, Vector2.down, true));

            Assert.True(history.TryGet(new SimulationTick(2), out PredictionInputFrame overwritten));
            Assert.AreEqual(Vector2.down, overwritten.Move);
            Assert.True(overwritten.RunHeld);

            var range = new System.Collections.Generic.List<PredictionInputFrame>();
            PredictionHistoryQueryResult result = history.TryReadRange(new SimulationTick(1), new SimulationTick(3), range);
            Assert.True(result.Success);
            Assert.AreEqual(3, range.Count);

            history.Write(Input(4, Vector2.zero));
            Assert.False(history.TryGet(new SimulationTick(1), out _));

            history.TrimConfirmedBefore(new SimulationTick(4));
            Assert.False(history.TryGet(new SimulationTick(3), out _));
            Assert.True(history.TryGet(new SimulationTick(4), out _));
        }

        [Test]
        public void PredictionInputHistoryReportsMissingRangeTick()
        {
            PredictionInputHistory history = new PredictionInputHistory(4);
            history.Write(Input(1, Vector2.right));
            history.Write(Input(3, Vector2.left));

            var range = new System.Collections.Generic.List<PredictionInputFrame>();
            PredictionHistoryQueryResult result = history.TryReadRange(new SimulationTick(1), new SimulationTick(3), range);

            Assert.False(result.Success);
            Assert.AreEqual(new SimulationTick(2), result.Tick);
            StringAssert.Contains("missing", result.Reason);
        }

        [Test]
        public void PredictionSnapshotHistoryWritesOverridesLatestAndTrims()
        {
            PredictionSnapshotHistory history = new PredictionSnapshotHistory(2);

            history.Write(Snapshot(1, Vector3.right));
            history.Write(Snapshot(2, Vector3.up));
            history.Write(Snapshot(2, Vector3.left));

            Assert.True(history.TryGet(new SimulationTick(2), out CharacterSimulationSnapshot overwritten));
            Assert.AreEqual(Vector3.left, overwritten.Position);

            history.Write(Snapshot(3, Vector3.forward));
            Assert.False(history.TryGet(new SimulationTick(1), out _));
            Assert.True(history.TryGetLatestRecoverableTick(out SimulationTick latest));
            Assert.AreEqual(new SimulationTick(3), latest);

            history.TrimConfirmedBefore(new SimulationTick(3));
            Assert.False(history.TryGet(new SimulationTick(2), out _));
            Assert.True(history.TryGet(new SimulationTick(3), out _));
        }

        [Test]
        public void SnapshotComparisonReportsDifferingFields()
        {
            CharacterSimulationSnapshot expected = Snapshot(4, new Vector3(1f, 0f, 0f));
            CharacterSimulationSnapshot actual = Snapshot(4, new Vector3(1.5f, 0f, 0f));

            CharacterSimulationSnapshotComparison comparison = CharacterSimulationSnapshotComparer.Compare(
                in expected,
                in actual,
                new CharacterSimulationSnapshotTolerance(0.01f, 0.01f, 0.0001f, 0.0001f));

            Assert.False(comparison.Matches);
            CollectionAssert.Contains(comparison.Differences.ToArray(), "position");
        }

        [Test]
        public void SnapshotComparisonReportsBlackboardDifferences()
        {
            CharacterSimulationSnapshot expected = Snapshot(4, Vector3.zero);
            CharacterRuntimeBlackboard blackboard = new CharacterRuntimeBlackboard();
            blackboard.WriteLocomotionFacts(new CharacterRuntimeLocomotionFacts(
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                BasicMovementGait.Run,
                false,
                BasicMovementGait.Walk,
                true,
                Vector3.forward,
                true,
                1f,
                4));
            CharacterSimulationSnapshot actual = new CharacterSimulationSnapshot(
                expected.Tick,
                expected.Position,
                expected.Yaw,
                expected.StateMachineRestoreState,
                expected.RunLatchActive,
                expected.LastMovingGait,
                expected.CurrentWorldDirection,
                expected.LocomotionPhase,
                expected.LocomotionGait,
                expected.AnimationKey,
                expected.AnimationNormalizedTime,
                blackboard.CaptureRestoreState());

            CharacterSimulationSnapshotComparison comparison = CharacterSimulationSnapshotComparer.Compare(
                in expected,
                in actual,
                CharacterSimulationSnapshotTolerance.Default);

            Assert.False(comparison.Matches);
            CollectionAssert.Contains(comparison.Differences.ToArray(), "blackboard.locomotion.phase");
            CollectionAssert.DoesNotContain(comparison.Differences.ToArray(), "blackboard.debug.lastWriter");
        }

        [Test]
        public void SnapshotComparisonClassifiesVisualLocomotionPlaybackAsPresentationDrift()
        {
            CharacterSimulationSnapshot expected = SnapshotWithAnimation(
                4,
                BasicMovementPhase.MoveLoop,
                "WalkLoop",
                0f);
            CharacterSimulationSnapshot actual = SnapshotWithAnimation(
                4,
                BasicMovementPhase.MoveLoop,
                "WalkLoop",
                0.111111f);

            CharacterSimulationSnapshotComparison comparison = CharacterSimulationSnapshotComparer.Compare(
                in expected,
                in actual,
                CharacterSimulationSnapshotTolerance.Default);

            Assert.True(comparison.Matches, string.Join(", ", comparison.Differences));
            CollectionAssert.Contains(comparison.PresentationDifferences.ToArray(), "animationNormalizedTime");
            CollectionAssert.Contains(comparison.PresentationDifferences.ToArray(), "blackboard.animation.locomotionNormalizedTime");
            CollectionAssert.DoesNotContain(comparison.Differences.ToArray(), "animationNormalizedTime");
        }

        [Test]
        public void SnapshotComparisonKeepsTurnBackPlaybackStrict()
        {
            CharacterSimulationSnapshot expected = SnapshotWithAnimation(
                4,
                BasicMovementPhase.TurnBack,
                "Locomotion.Turn.Back",
                0.267606f);
            CharacterSimulationSnapshot actual = SnapshotWithAnimation(
                4,
                BasicMovementPhase.TurnBack,
                "Locomotion.Turn.Back",
                0f);

            CharacterSimulationSnapshotComparison comparison = CharacterSimulationSnapshotComparer.Compare(
                in expected,
                in actual,
                CharacterSimulationSnapshotTolerance.Default);

            Assert.False(comparison.Matches);
            CollectionAssert.Contains(comparison.Differences.ToArray(), "animationNormalizedTime");
            CollectionAssert.Contains(comparison.Differences.ToArray(), "blackboard.animation.locomotionNormalizedTime");
        }

        [Test]
        public void SnapshotComparisonClassifiesActionPlaybackAsPresentationDrift()
        {
            CharacterSimulationSnapshot expected = SnapshotWithAnimation(
                4,
                BasicMovementPhase.MoveLoop,
                "WalkLoop",
                0.5f,
                ActionAnimationKeys.DodgeDirectional,
                0.2f,
                true);
            CharacterSimulationSnapshot actual = SnapshotWithAnimation(
                4,
                BasicMovementPhase.MoveLoop,
                "WalkLoop",
                0.5f,
                ActionAnimationKeys.DodgeDirectional,
                0.31f,
                true);

            CharacterSimulationSnapshotComparison comparison = CharacterSimulationSnapshotComparer.Compare(
                in expected,
                in actual,
                CharacterSimulationSnapshotTolerance.Default);

            Assert.True(comparison.Matches, string.Join(", ", comparison.Differences));
            CollectionAssert.Contains(comparison.PresentationDifferences.ToArray(), "blackboard.animation.actionNormalizedTime");
            CollectionAssert.DoesNotContain(comparison.Differences.ToArray(), "blackboard.animation.actionNormalizedTime");
        }

        [Test]
        public void RollbackScopeResolverClassifiesTurnBackAsProfileDrivenStrict()
        {
            PredictionRollbackAuthorityPolicy policy = PredictionRollbackScopeResolver.ResolveLocomotionPlayback(
                BasicMovementPhase.TurnBack,
                "Locomotion.Turn.Back");

            Assert.AreEqual(AnimationPlaybackAuthority.ProfileDriven, policy.AnimationAuthority);
            Assert.AreEqual(RollbackMotionAuthority.AnimationProfile, policy.MotionAuthority);
            Assert.AreEqual(RollbackCompareScope.StrictGameplay, policy.CompareScope);
        }

        [Test]
        public void RollbackScopeResolverClassifiesMoveLoopAsVisualPresentation()
        {
            PredictionRollbackAuthorityPolicy policy = PredictionRollbackScopeResolver.ResolveLocomotionPlayback(
                BasicMovementPhase.MoveLoop,
                "WalkLoop");

            Assert.AreEqual(AnimationPlaybackAuthority.VisualOnly, policy.AnimationAuthority);
            Assert.AreEqual(RollbackMotionAuthority.KinematicInput, policy.MotionAuthority);
            Assert.AreEqual(RollbackCompareScope.PresentationDrift, policy.CompareScope);
        }

        [Test]
        public void RollbackScopeResolverClassifiesActionPlaybackAsPresentation()
        {
            PredictionRollbackAuthorityPolicy policy = PredictionRollbackScopeResolver.ResolveActionPlayback(
                ActionAnimationKeys.DodgeDirectional);

            Assert.AreEqual(AnimationPlaybackAuthority.VisualOnly, policy.AnimationAuthority);
            Assert.AreEqual(RollbackMotionAuthority.StateTimeline, policy.MotionAuthority);
            Assert.AreEqual(RollbackCompareScope.PresentationDrift, policy.CompareScope);
        }

        [Test]
        public void RollbackScopeResolverClassifiesGameplayFactsAsStrict()
        {
            PredictionRollbackAuthorityPolicy root = PredictionRollbackScopeResolver.ResolveRootPose();
            PredictionRollbackAuthorityPolicy locomotion = PredictionRollbackScopeResolver.ResolveLocomotionFacts();
            PredictionRollbackAuthorityPolicy action = PredictionRollbackScopeResolver.ResolveActionFacts();

            Assert.AreEqual(RollbackCompareScope.StrictGameplay, root.CompareScope);
            Assert.AreEqual(RollbackMotionAuthority.MotionExecutor, root.MotionAuthority);
            Assert.AreEqual(RollbackCompareScope.StrictGameplay, locomotion.CompareScope);
            Assert.AreEqual(RollbackCompareScope.StrictGameplay, action.CompareScope);
            Assert.AreEqual(RollbackMotionAuthority.StateTimeline, action.MotionAuthority);
        }

        [Test]
        public void SnapshotComparisonDoesNotIgnoreStrictActionMotionResult()
        {
            CharacterRuntimeActionFacts expectedAction = new CharacterRuntimeActionFacts(
                true,
                ActionStateIds.Dodge,
                false,
                false,
                true,
                Vector3.forward,
                1.1f,
                true,
                12);
            CharacterRuntimeActionFacts actualAction = new CharacterRuntimeActionFacts(
                true,
                ActionStateIds.Dodge,
                true,
                false,
                true,
                Vector3.right,
                0.7f,
                false,
                12);
            CharacterSimulationSnapshot expected = SnapshotWithActionFacts(expectedAction);
            CharacterSimulationSnapshot actual = SnapshotWithActionFacts(actualAction);

            CharacterSimulationSnapshotComparison comparison = CharacterSimulationSnapshotComparer.Compare(
                in expected,
                in actual,
                CharacterSimulationSnapshotTolerance.Default);

            Assert.False(comparison.Matches);
            Assert.That(comparison.Differences, Does.Contain("blackboard.action.completed"));
            Assert.That(comparison.Differences, Does.Contain("blackboard.action.worldDirection"));
            Assert.That(comparison.Differences, Does.Contain("blackboard.action.planarDistance"));
            Assert.That(comparison.Differences, Does.Contain("blackboard.action.rotateToDirection"));
            Assert.IsEmpty(comparison.PresentationDifferences);
        }

        [Test]
        public void RollbackScopeResolverDoesNotReferencePresentationRuntimeObjects()
        {
            string root = Path.Combine(
                Application.dataPath,
                "Scripts",
                "Simulation",
                "Rollback");
            string source = string.Join(
                "\n",
                File.ReadAllText(Path.Combine(root, "PredictionRollbackAuthorityPolicy.cs"), System.Text.Encoding.UTF8),
                File.ReadAllText(Path.Combine(root, "PredictionRollbackScopeResolver.cs"), System.Text.Encoding.UTF8));

            Assert.That(source, Does.Not.Contain("AnimancerState"));
            Assert.That(source, Does.Not.Contain("AnimationClip"));
            Assert.That(source, Does.Not.Contain("TransitionAsset"));
            Assert.That(source, Does.Not.Contain("UnityEngine.Object"));
        }

        [Test]
        public void SnapshotComparisonIgnoresBlackboardDiagnosticSourceSteps()
        {
            CharacterRuntimeBlackboard expectedBlackboard = new CharacterRuntimeBlackboard();
            expectedBlackboard.WriteLocomotionFacts(new CharacterRuntimeLocomotionFacts(
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                BasicMovementGait.Run,
                false,
                BasicMovementGait.Walk,
                true,
                Vector3.forward,
                true,
                1f,
                1));
            CharacterRuntimeBlackboard actualBlackboard = new CharacterRuntimeBlackboard();
            actualBlackboard.WriteLocomotionFacts(new CharacterRuntimeLocomotionFacts(
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                BasicMovementGait.Run,
                false,
                BasicMovementGait.Walk,
                true,
                Vector3.forward,
                true,
                1f,
                99));
            CharacterSimulationSnapshot expected = new CharacterSimulationSnapshot(
                new SimulationTick(4),
                Vector3.zero,
                0f,
                new CharacterStateMachineRestoreState(
                    CharacterStateMachineSnapshot.Inactive,
                    Vector3.zero,
                    false),
                true,
                BasicMovementGait.Run,
                Vector3.forward,
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                "RunLoop",
                0.5f,
                expectedBlackboard.CaptureRestoreState());
            CharacterSimulationSnapshot actual = new CharacterSimulationSnapshot(
                expected.Tick,
                expected.Position,
                expected.Yaw,
                expected.StateMachineRestoreState,
                expected.RunLatchActive,
                expected.LastMovingGait,
                expected.CurrentWorldDirection,
                expected.LocomotionPhase,
                expected.LocomotionGait,
                expected.AnimationKey,
                expected.AnimationNormalizedTime,
                actualBlackboard.CaptureRestoreState());

            CharacterSimulationSnapshotComparison comparison = CharacterSimulationSnapshotComparer.Compare(
                in expected,
                in actual,
                CharacterSimulationSnapshotTolerance.Default);

            Assert.True(comparison.Matches, string.Join(", ", comparison.Differences));
        }

        [Test]
        public void FullBodyActionLifecycleRestoreKeepsActionFactsForNextTick()
        {
            FullBodyActionRuntimeModule original = new FullBodyActionRuntimeModule();
            Assert.True(original.Rebuild(LoadConfiguredStateMachineDefinitionAsset(), false));
            CharacterResolvedAction action = ResolvedDodgeAction(CharacterStateVariant.Directional, Vector3.forward, 1);
            ActionLifecycleFrame entered = original.TickActionLifecycle(in action, 0.1f, 1);
            FullBodyActionRestoreState restoreState = original.CaptureRestoreState();

            FullBodyActionRuntimeModule restored = new FullBodyActionRuntimeModule();
            Assert.True(restored.Rebuild(LoadConfiguredStateMachineDefinitionAsset(), false));
            Assert.True(restored.Restore(in restoreState));

            CharacterResolvedAction none = default;
            ActionLifecycleFrame originalNext = original.TickActionLifecycle(in none, 0.1f, 2);
            ActionLifecycleFrame restoredNext = restored.TickActionLifecycle(in none, 0.1f, 2);

            Assert.AreEqual(ActionStateIds.Dodge, entered.ActionState);
            Assert.True(entered.HasAnimationRequest);
            Assert.AreEqual(originalNext.ActionState, restoredNext.ActionState);
            Assert.AreEqual(originalNext.MotionSpec.StateTime, restoredNext.MotionSpec.StateTime, 0.0001f);
            Assert.AreEqual(
                originalNext.MotionSpec.LockedWorldDirection,
                restoredNext.MotionSpec.LockedWorldDirection);
            Assert.AreEqual(
                originalNext.AnimationRequest.ActionPlaybackIntent,
                restoredNext.AnimationRequest.ActionPlaybackIntent);
        }

        static CharacterStateMachineDefinition LoadConfiguredStateMachineDefinition()
        {
            return LoadConfiguredStateMachineDefinitionAsset().ToDefinition();
        }

        static CharacterStateMachineDefinitionSO LoadConfiguredStateMachineDefinitionAsset()
        {
            CharacterStateMachineDefinitionSO asset = AssetDatabase.LoadAssetAtPath<CharacterStateMachineDefinitionSO>(
                "Assets/Configs/3C/StateMachine/Locomotion/Corin/CorinLocomotionStateGraph.asset");
            Assert.NotNull(asset);
            return asset;
        }

        static CharacterFrameRuntimeController AddConfiguredRuntimeController(GameObject gameObject)
        {
            CharacterFrameRuntimeController controller = gameObject.AddComponent<CharacterFrameRuntimeController>();
            controller.CharacterConfig = LoadConfiguredCharacterConfigAsset();
            controller.AutoUpdate = false;
            return controller;
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

        [Test]
        public void CharacterFrameRuntimeControllerRestoresSimulationSnapshot()
        {
            GameObject gameObject = new GameObject("rollback-locomotion-snapshot-test");
            gameObject.SetActive(false);

            try
            {
                CharacterFrameRuntimeController controller = AddConfiguredRuntimeController(gameObject);
                gameObject.transform.SetPositionAndRotation(new Vector3(2f, 0f, 3f), Quaternion.Euler(0f, 45f, 0f));
                controller.LocomotionModule.SetRunLatchActive(true);

                CharacterSimulationSnapshot snapshot = controller.CaptureSimulationSnapshot(new SimulationTick(8));
                gameObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                controller.LocomotionModule.SetRunLatchActive(false);

                Assert.True(controller.RestoreSimulationSnapshot(in snapshot));
                CharacterSimulationSnapshot restored = controller.CaptureSimulationSnapshot(new SimulationTick(8));

                Assert.AreEqual(snapshot.Position, restored.Position);
                Assert.AreEqual(snapshot.Yaw, restored.Yaw, 0.0001f);
                Assert.AreEqual(snapshot.StateMachine.ActiveState, restored.StateMachine.ActiveState);
                Assert.True(restored.RunLatchActive);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CharacterFrameRuntimeControllerRestoresRuntimeBlackboardSnapshotIdempotently()
        {
            GameObject gameObject = new GameObject("rollback-blackboard-snapshot-test");
            gameObject.SetActive(false);

            try
            {
                CharacterFrameRuntimeController controller = AddConfiguredRuntimeController(gameObject);
                controller.LocomotionModule.WriteAnimationFacts(new CharacterRuntimeAnimationFacts(
                    new AnimationPhasePlaybackProgress(BasicMovementPhase.MoveLoop, "RunLoop", 0.6f, true, false),
                    "RunLoop",
                    ActionAnimationKeys.DodgeDirectional,
                    0.2f,
                    true,
                    true,
                    "Dodge Directional",
                    9));

                CharacterSimulationSnapshot snapshot = controller.CaptureSimulationSnapshot(new SimulationTick(9));
                controller.LocomotionModule.WriteAnimationFacts(CharacterRuntimeAnimationFacts.Default);

                Assert.True(controller.RestoreSimulationSnapshot(in snapshot));
                CharacterSimulationSnapshot restoredOnce = controller.CaptureSimulationSnapshot(new SimulationTick(9));
                Assert.True(controller.RestoreSimulationSnapshot(in snapshot));
                CharacterSimulationSnapshot restoredTwice = controller.CaptureSimulationSnapshot(new SimulationTick(9));

                CharacterSimulationSnapshotComparison firstComparison = CharacterSimulationSnapshotComparer.Compare(
                    in snapshot,
                    in restoredOnce,
                    CharacterSimulationSnapshotTolerance.Default);
                CharacterSimulationSnapshotComparison secondComparison = CharacterSimulationSnapshotComparer.Compare(
                    in restoredOnce,
                    in restoredTwice,
                    CharacterSimulationSnapshotTolerance.Default);

                Assert.True(firstComparison.Matches, string.Join(", ", firstComparison.Differences));
                Assert.True(secondComparison.Matches, string.Join(", ", secondComparison.Differences));
                Assert.AreEqual("Animation", restoredTwice.RuntimeBlackboard.Debug.LastWriter);
                Assert.AreEqual(ActionAnimationKeys.DodgeDirectional, restoredTwice.RuntimeBlackboard.Animation.ActionKey);
                Assert.True(restoredTwice.RuntimeBlackboard.Animation.ActionIsEnded);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SnapshotRecorderWritesDuringSnapshotPhaseWithoutMovingRoot()
        {
            GameObject gameObject = new GameObject("rollback-snapshot-recorder-test");
            gameObject.SetActive(false);

            try
            {
                UnitySimulationTickDriver driver = gameObject.AddComponent<UnitySimulationTickDriver>();
                CharacterFrameRuntimeController controller = AddConfiguredRuntimeController(gameObject);
                LocomotionSnapshotHistoryRecorder recorder = gameObject.AddComponent<LocomotionSnapshotHistoryRecorder>();
                recorder.TickDriver = driver;
                recorder.RuntimeController = controller;
                Assert.True(recorder.Register());

                gameObject.transform.position = new Vector3(4f, 0f, 5f);
                driver.Runner.Run(new SimulationTickContext(new SimulationTick(12), SimulationTickRate.Default, SimulationTickRole.Client));

                Assert.True(recorder.History.TryGet(new SimulationTick(12), out CharacterSimulationSnapshot snapshot));
                Assert.AreEqual(new Vector3(4f, 0f, 5f), snapshot.Position);
                Assert.AreEqual(new Vector3(4f, 0f, 5f), gameObject.transform.position);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void InputRecorderWritesPredictionFrameDuringReadInputPhase()
        {
            GameObject gameObject = new GameObject("rollback-input-recorder-test");

            try
            {
                UnitySimulationTickDriver driver = gameObject.AddComponent<UnitySimulationTickDriver>();
                FakePredictionSource source = gameObject.AddComponent<FakePredictionSource>();
                PredictionInputHistoryTickRecorder recorder = gameObject.AddComponent<PredictionInputHistoryTickRecorder>();
                recorder.TickDriver = driver;
                recorder.InputSourceBehaviour = source;
                Assert.True(recorder.Register());

                driver.Runner.Run(new SimulationTickContext(new SimulationTick(6), SimulationTickRate.Default, SimulationTickRole.Client));

                Assert.True(recorder.History.TryGet(new SimulationTick(6), out PredictionInputFrame frame));
                Assert.AreEqual(Vector2.right, frame.Move);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void LocalSynctestReplaysSameInputsToSameSnapshot()
        {
            PredictionInputHistory inputHistory = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(8);
            FakeRollbackSimulation simulation = new FakeRollbackSimulation();

            snapshotHistory.Write(simulation.CaptureSnapshot(new SimulationTick(0)));
            for (int tick = 1; tick <= 3; tick++)
            {
                PredictionInputFrame input = Input(tick, Vector2.right);
                inputHistory.Write(input);
                simulation.Advance(input);
                CharacterSimulationSnapshot snapshot = simulation.CaptureSnapshot(new SimulationTick(tick));
                snapshotHistory.Write(in snapshot);
            }

            LocalRollbackSynctestRunner runner = new LocalRollbackSynctestRunner(inputHistory, snapshotHistory, simulation);
            LocalRollbackSynctestResult result = runner.Run(
                new SimulationTick(0),
                new SimulationTick(3),
                new SimulationTick(0),
                CharacterSimulationSnapshotTolerance.Default);

            Assert.True(result.Success, result.FailureReason);
        }

        [Test]
        public void LocalSynctestResultReportsFirstReplayMismatch()
        {
            PredictionInputHistory inputHistory = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(8);
            FakeRollbackSimulation simulation = new FakeRollbackSimulation();

            snapshotHistory.Write(Snapshot(0, Vector3.zero));
            inputHistory.Write(Input(1, Vector2.right));
            inputHistory.Write(Input(2, Vector2.right));
            snapshotHistory.Write(Snapshot(1, Vector3.right));
            snapshotHistory.Write(Snapshot(2, Vector3.right));

            LocalRollbackSynctestRunner runner = new LocalRollbackSynctestRunner(inputHistory, snapshotHistory, simulation);
            LocalRollbackSynctestResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(2),
                SimulationTick.Zero,
                CharacterSimulationSnapshotTolerance.Default);

            Assert.False(result.Success);
            Assert.True(result.FirstMismatch.HasMismatch);
            Assert.AreEqual(LocalRollbackSynctestMismatchStage.Replay, result.FirstMismatch.Stage);
            Assert.AreEqual(new SimulationTick(2), result.FirstMismatch.Tick);
            Assert.True(result.FirstMismatch.HasInput);
            Assert.AreEqual(Vector2.right, result.FirstMismatch.Input.Move);
            CollectionAssert.Contains(result.FirstMismatch.Comparison.Differences.ToArray(), "position");
        }

        [Test]
        public void LocalSynctestPassesWhenReplayOnlyHasPresentationDrift()
        {
            PredictionInputHistory inputHistory = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(8);
            PresentationDriftRollbackSimulation simulation = new PresentationDriftRollbackSimulation();

            snapshotHistory.Write(SnapshotWithAnimation(0, BasicMovementPhase.MoveLoop, "WalkLoop", 0f));
            inputHistory.Write(Input(1, Vector2.zero));
            snapshotHistory.Write(SnapshotWithAnimation(1, BasicMovementPhase.MoveLoop, "WalkLoop", 0f));

            LocalRollbackSynctestRunner runner = new LocalRollbackSynctestRunner(inputHistory, snapshotHistory, simulation);
            LocalRollbackSynctestResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(1),
                SimulationTick.Zero,
                CharacterSimulationSnapshotTolerance.Default);

            Assert.True(result.Success, result.FailureReason);
            Assert.False(result.FirstMismatch.HasMismatch);
            Assert.True(result.FirstMismatch.HasPresentationDrift);
            CollectionAssert.Contains(result.FirstMismatch.Comparison.PresentationDifferences.ToArray(), "animationNormalizedTime");
        }

        [Test]
        public void LocalSynctestStrictMismatchOverridesEarlierPresentationDrift()
        {
            PredictionInputHistory inputHistory = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(8);
            PresentationThenStrictMismatchRollbackSimulation simulation = new PresentationThenStrictMismatchRollbackSimulation();

            snapshotHistory.Write(SnapshotWithAnimation(0, Vector3.zero, BasicMovementPhase.MoveLoop, "WalkLoop", 0f));
            inputHistory.Write(Input(1, Vector2.zero));
            snapshotHistory.Write(SnapshotWithAnimation(1, Vector3.zero, BasicMovementPhase.MoveLoop, "WalkLoop", 0f));
            inputHistory.Write(Input(2, Vector2.right));
            snapshotHistory.Write(SnapshotWithAnimation(2, Vector3.right, BasicMovementPhase.MoveLoop, "WalkLoop", 0f));

            LocalRollbackSynctestRunner runner = new LocalRollbackSynctestRunner(inputHistory, snapshotHistory, simulation);
            LocalRollbackSynctestResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(2),
                SimulationTick.Zero,
                CharacterSimulationSnapshotTolerance.Default);

            Assert.False(result.Success);
            Assert.True(result.FirstMismatch.HasMismatch);
            Assert.True(result.FirstMismatch.HasPresentationDrift);
            Assert.AreEqual(new SimulationTick(2), result.FirstMismatch.Tick);
            CollectionAssert.Contains(result.FirstMismatch.Comparison.Differences.ToArray(), "position");
            CollectionAssert.Contains(result.FirstMismatch.Comparison.PresentationDifferences.ToArray(), "animationNormalizedTime");
        }

        [Test]
        public void LocalSynctestFailsWhenFirstMismatchConvergesByEnd()
        {
            PredictionInputHistory inputHistory = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(8);
            TransientMismatchRollbackSimulation simulation = new TransientMismatchRollbackSimulation();

            snapshotHistory.Write(Snapshot(0, Vector3.zero));
            inputHistory.Write(Input(1, Vector2.right));
            inputHistory.Write(Input(2, Vector2.left));
            snapshotHistory.Write(Snapshot(1, Vector3.right));
            snapshotHistory.Write(Snapshot(2, Vector3.zero));

            LocalRollbackSynctestRunner runner = new LocalRollbackSynctestRunner(inputHistory, snapshotHistory, simulation);
            LocalRollbackSynctestResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(2),
                SimulationTick.Zero,
                CharacterSimulationSnapshotTolerance.Default);

            Assert.False(result.Success);
            Assert.True(result.Comparison.Matches);
            Assert.AreEqual("first mismatch", result.FailureReason);
            Assert.True(result.FirstMismatch.HasMismatch);
            Assert.AreEqual(LocalRollbackSynctestMismatchStage.Replay, result.FirstMismatch.Stage);
            Assert.AreEqual(new SimulationTick(1), result.FirstMismatch.Tick);
        }

        [Test]
        public void LocalSynctestFailsWhenRestoreDoesNotRoundTrip()
        {
            PredictionInputHistory inputHistory = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(8);
            BrokenRestoreRollbackSimulation simulation = new BrokenRestoreRollbackSimulation(Vector3.right);

            snapshotHistory.Write(Snapshot(0, Vector3.zero));
            inputHistory.Write(Input(1, Vector2.zero));
            snapshotHistory.Write(Snapshot(1, Vector3.right));

            LocalRollbackSynctestRunner runner = new LocalRollbackSynctestRunner(inputHistory, snapshotHistory, simulation);
            LocalRollbackSynctestResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(1),
                SimulationTick.Zero,
                CharacterSimulationSnapshotTolerance.Default);

            Assert.False(result.Success);
            Assert.True(result.FirstMismatch.HasMismatch);
            Assert.AreEqual(LocalRollbackSynctestMismatchStage.Restore, result.FirstMismatch.Stage);
            Assert.False(result.FirstMismatch.HasInput);
        }

        [Test]
        public void LocalSynctestReportsFinalMismatchWithoutIntermediateSnapshot()
        {
            PredictionInputHistory inputHistory = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(8);
            FakeRollbackSimulation simulation = new FakeRollbackSimulation();

            snapshotHistory.Write(Snapshot(0, Vector3.zero));
            inputHistory.Write(Input(1, Vector2.right));
            inputHistory.Write(Input(2, Vector2.right));
            snapshotHistory.Write(Snapshot(2, Vector3.left));

            LocalRollbackSynctestRunner runner = new LocalRollbackSynctestRunner(inputHistory, snapshotHistory, simulation);
            LocalRollbackSynctestResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(2),
                SimulationTick.Zero,
                CharacterSimulationSnapshotTolerance.Default);

            Assert.False(result.Success);
            Assert.True(result.FirstMismatch.HasMismatch);
            Assert.AreEqual(new SimulationTick(2), result.FirstMismatch.Tick);
            Assert.False(result.Comparison.Matches);
            Assert.AreEqual("first mismatch and snapshot mismatch", result.FailureReason);
            CollectionAssert.Contains(result.Comparison.Differences.ToArray(), "position");
        }

        [Test]
        public void SoakInputGeneratorIsDeterministicBySeed()
        {
            PredictionInputFrame a = LocalRollbackSoakInputGenerator.GenerateFrame(17, new SimulationTick(8));
            PredictionInputFrame b = LocalRollbackSoakInputGenerator.GenerateFrame(17, new SimulationTick(8));
            PredictionInputFrame c = LocalRollbackSoakInputGenerator.GenerateFrame(18, new SimulationTick(8));

            Assert.AreEqual(a.Move, b.Move);
            Assert.AreEqual(a.Look, b.Look);
            Assert.AreEqual(a.RunHeld, b.RunHeld);
            Assert.AreEqual(a.Dodge.Pressed, b.Dodge.Pressed);
            Assert.True(a.Move != c.Move || a.Look != c.Look || a.RunHeld != c.RunHeld || a.Dodge.Pressed != c.Dodge.Pressed);
        }

        [Test]
        public void LocalRollbackSoakRunnerPassesAcrossGeneratedWindows()
        {
            PredictionInputHistory inputHistory = new PredictionInputHistory(32);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(32);
            FakeRollbackSimulation simulation = new FakeRollbackSimulation();
            LocalRollbackSoakInputConfig inputConfig = new LocalRollbackSoakInputConfig(41, 12);
            LocalRollbackSoakConfig config = new LocalRollbackSoakConfig(41, 12, 4, true);

            LocalRollbackSoakInputGenerator.Populate(in inputConfig, inputHistory, snapshotHistory, simulation);
            LocalRollbackSoakRunner runner = new LocalRollbackSoakRunner(inputHistory, snapshotHistory, simulation);
            LocalRollbackSoakResult result = runner.Run(in config, CharacterSimulationSnapshotTolerance.Default);

            Assert.True(result.Success, result.FailureReason);
            Assert.AreEqual(9, result.CheckedWindows);
        }

        [Test]
        public void LocalRollbackSoakRunnerStopsOnFirstMismatch()
        {
            PredictionInputHistory inputHistory = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(8);
            FakeRollbackSimulation simulation = new FakeRollbackSimulation();
            LocalRollbackSoakConfig config = new LocalRollbackSoakConfig(7, 3, 2, true);

            snapshotHistory.Write(Snapshot(0, Vector3.zero));
            inputHistory.Write(Input(1, Vector2.right));
            inputHistory.Write(Input(2, Vector2.right));
            inputHistory.Write(Input(3, Vector2.right));
            snapshotHistory.Write(Snapshot(1, Vector3.right));
            snapshotHistory.Write(Snapshot(2, Vector3.right));
            snapshotHistory.Write(Snapshot(3, Vector3.right));

            LocalRollbackSoakRunner runner = new LocalRollbackSoakRunner(inputHistory, snapshotHistory, simulation);
            LocalRollbackSoakResult result = runner.Run(in config, CharacterSimulationSnapshotTolerance.Default);

            Assert.False(result.Success);
            Assert.AreEqual(1, result.CheckedWindows);
            Assert.AreEqual(new SimulationTick(0), result.FirstFailure.RestoreTick);
            Assert.AreEqual(new SimulationTick(2), result.FirstFailure.EndTick);
            Assert.True(result.FirstFailure.FirstMismatch.HasMismatch);
            CollectionAssert.Contains(result.FirstFailure.Comparison.Differences.ToArray(), "position");
        }

        [Test]
        public void LocalRollbackSoakRunnerKeepsFirstMismatchWhenContinuing()
        {
            PredictionInputHistory inputHistory = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(8);
            FakeRollbackSimulation simulation = new FakeRollbackSimulation();
            LocalRollbackSoakConfig config = new LocalRollbackSoakConfig(7, 3, 1, false);

            snapshotHistory.Write(Snapshot(0, Vector3.zero));
            inputHistory.Write(Input(1, Vector2.right));
            inputHistory.Write(Input(2, Vector2.right));
            inputHistory.Write(Input(3, Vector2.right));
            snapshotHistory.Write(Snapshot(1, Vector3.zero));
            snapshotHistory.Write(Snapshot(2, Vector3.zero));
            snapshotHistory.Write(Snapshot(3, Vector3.zero));

            LocalRollbackSoakRunner runner = new LocalRollbackSoakRunner(inputHistory, snapshotHistory, simulation);
            LocalRollbackSoakResult result = runner.Run(in config, CharacterSimulationSnapshotTolerance.Default);

            Assert.False(result.Success);
            Assert.AreEqual(3, result.CheckedWindows);
            Assert.AreEqual(new SimulationTick(0), result.FirstFailure.RestoreTick);
            Assert.AreEqual(new SimulationTick(1), result.FirstFailure.EndTick);
        }

        [Test]
        public void LocalRollbackSoakRunnerFailsWhenWindowFirstMismatchConvergesAtEnd()
        {
            PredictionInputHistory inputHistory = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(8);
            TransientMismatchRollbackSimulation simulation = new TransientMismatchRollbackSimulation();
            LocalRollbackSoakConfig config = new LocalRollbackSoakConfig(7, 2, 2, true);

            snapshotHistory.Write(Snapshot(0, Vector3.zero));
            inputHistory.Write(Input(1, Vector2.right));
            inputHistory.Write(Input(2, Vector2.left));
            snapshotHistory.Write(Snapshot(1, Vector3.right));
            snapshotHistory.Write(Snapshot(2, Vector3.zero));

            LocalRollbackSoakRunner runner = new LocalRollbackSoakRunner(inputHistory, snapshotHistory, simulation);
            LocalRollbackSoakResult result = runner.Run(in config, CharacterSimulationSnapshotTolerance.Default);

            Assert.False(result.Success);
            Assert.AreEqual(1, result.CheckedWindows);
            Assert.True(result.FirstFailure.Comparison.Matches);
            Assert.True(result.FirstFailure.FirstMismatch.HasMismatch);
            Assert.AreEqual("first mismatch", result.FirstFailure.FailureReason);
        }

        [Test]
        public void LocalRollbackSoakRunnerPassesAndRecordsPresentationDrift()
        {
            PredictionInputHistory inputHistory = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(8);
            PresentationDriftRollbackSimulation simulation = new PresentationDriftRollbackSimulation();
            LocalRollbackSoakConfig config = new LocalRollbackSoakConfig(7, 2, 1, true);

            snapshotHistory.Write(SnapshotWithAnimation(0, BasicMovementPhase.MoveLoop, "WalkLoop", 0f));
            inputHistory.Write(Input(1, Vector2.zero));
            snapshotHistory.Write(SnapshotWithAnimation(1, BasicMovementPhase.MoveLoop, "WalkLoop", 0f));
            inputHistory.Write(Input(2, Vector2.zero));
            snapshotHistory.Write(SnapshotWithAnimation(2, BasicMovementPhase.MoveLoop, "WalkLoop", 0f));

            LocalRollbackSoakRunner runner = new LocalRollbackSoakRunner(inputHistory, snapshotHistory, simulation);
            LocalRollbackSoakResult result = runner.Run(in config, CharacterSimulationSnapshotTolerance.Default);

            Assert.True(result.Success, result.FailureReason);
            Assert.True(result.HasPresentationDrift);
            Assert.True(result.FirstPresentationDrift.FirstMismatch.HasPresentationDrift);
            CollectionAssert.Contains(
                result.FirstPresentationDrift.FirstMismatch.Comparison.PresentationDifferences.ToArray(),
                "animationNormalizedTime");
        }

        [Test]
        public void SoakDebugRunnerLogsResultAndRestoresHiddenReplayState()
        {
            GameObject gameObject = new GameObject("rollback-soak-debug-runner-test");
            GameObject visualObject = new GameObject("rollback-soak-debug-visual-test");
            GameObject cameraObject = new GameObject("rollback-soak-debug-camera-test");
            GameObject cameraFollowObject = new GameObject("rollback-soak-debug-camera-follow-test");
            GameObject cameraAimObject = new GameObject("rollback-soak-debug-camera-aim-test");
            gameObject.SetActive(false);

            try
            {
                PresentationTransformInterpolator presentation = gameObject.AddComponent<PresentationTransformInterpolator>();
                SamplingFakeRollbackSimulation simulation = gameObject.AddComponent<SamplingFakeRollbackSimulation>();
                LocalRollbackSoakDebugRunner debugRunner = gameObject.AddComponent<LocalRollbackSoakDebugRunner>();
                ThirdPersonCameraController camera = cameraObject.AddComponent<ThirdPersonCameraController>();

                presentation.Source = gameObject.transform;
                presentation.VisualTarget = visualObject.transform;
                presentation.SnapDistance = 20f;
                camera.FollowAnchorSource = visualObject.transform;
                camera.CameraFollowTarget = cameraFollowObject.transform;
                camera.CameraAimTarget = cameraAimObject.transform;
                camera.AutoTick = false;
                camera.DebugLog = false;
                camera.Sensitivity = new Vector2(10f, 10f);
                camera.ResetState(25f, 3f);
                simulation.Configure(gameObject.transform, presentation);
                debugRunner.SimulationBehaviour = simulation;
                debugRunner.PresentationInterpolator = presentation;
                debugRunner.CameraController = camera;
                debugRunner.Seed = 13;
                debugRunner.TickCount = 6;
                debugRunner.RollbackFrames = 2;
                debugRunner.RunOnKeyDown = false;

                gameObject.transform.position = new Vector3(2f, 0f, 0f);
                presentation.ResetSamples();
                presentation.CaptureSourceSample();
                gameObject.transform.position = new Vector3(3f, 0f, 0f);
                presentation.CaptureSourceSample();
                presentation.UpdateVisualTarget();
                camera.Resolve();
                Vector3 sourceBefore = gameObject.transform.position;
                Vector3 visualBefore = visualObject.transform.position;
                Vector3 cameraFollowBefore = cameraFollowObject.transform.position;
                float cameraYawBefore = camera.Yaw;
                var events = new System.Collections.Generic.List<RuntimeDiagnosticLogEvent>();

                using (RuntimeDiagnosticLog.Capture(events.Add))
                    Assert.True(debugRunner.RunSoak(), debugRunner.LastResult.FailureReason);

                RuntimeDiagnosticLogEvent resultEvent = events.First(e => e.Message == "rollback-soak-result");
                Assert.AreEqual(sourceBefore, gameObject.transform.position);
                Assert.AreEqual(visualBefore, visualObject.transform.position);
                Assert.AreEqual(cameraFollowBefore, cameraFollowObject.transform.position);
                Assert.AreEqual(cameraYawBefore, camera.Yaw, 0.0001f);
                Assert.True(debugRunner.HasResult);
                Assert.True(debugRunner.LastResult.Success);
                StringAssert.Contains("ROLLBACK_SOAK_RESULT", resultEvent.Context);
                StringAssert.Contains("result=PASS", resultEvent.Context);
                StringAssert.Contains("seed=13", resultEvent.Context);
                StringAssert.Contains("sourceRestored=True", resultEvent.Context);
                StringAssert.Contains("visualRestored=True", resultEvent.Context);
                StringAssert.Contains("cameraLocalOnly=True", resultEvent.Context);
                StringAssert.Contains("visualChecked=True", resultEvent.Context);
                Assert.False(events.Any(e => e.Message == "rollback-soak-first-mismatch"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraAimObject);
                UnityEngine.Object.DestroyImmediate(cameraFollowObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(visualObject);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RollbackDebugRigPrefabWiresLocalRollbackTools()
        {
            string prefabPath = Path.Combine(Application.dataPath, "Prefabs", "Simulation", "RollbackDebugRig.prefab");
            string prefab = File.ReadAllText(prefabPath, System.Text.Encoding.UTF8);
            string inputRecorderFileId = "8801000000000000003";
            string snapshotRecorderFileId = "8801000000000000004";
            string simulationFileId = "8801000000000000005";

            StringAssert.Contains("guid: 76dd67150d57413e9cf41bca3e79f6ef", prefab);
            StringAssert.Contains("guid: d43c4b24d029331458e709b1b3b82db2", prefab);
            StringAssert.Contains("guid: f79063608d784da787c3554c8d0eda2d", prefab);
            StringAssert.Contains($"inputRecorder: {{fileID: {inputRecorderFileId}}}", prefab);
            StringAssert.Contains($"snapshotRecorder: {{fileID: {snapshotRecorderFileId}}}", prefab);
            StringAssert.Contains($"simulationBehaviour: {{fileID: {simulationFileId}}}", prefab);
            StringAssert.Contains("triggerKey: 287", prefab);
            StringAssert.Contains("triggerKey: 288", prefab);
            StringAssert.Contains("triggerKey: 289", prefab);
            StringAssert.Contains("applyReplayResultToScene: 0", prefab);

            string runnerPath = Path.Combine(Application.dataPath, "Scripts", "Simulation", "Rollback", "LocalRollbackSoakDebugRunner.cs");
            string runner = File.ReadAllText(runnerPath, System.Text.Encoding.UTF8);
            StringAssert.Contains("ROLLBACK_SOAK_RESULT", runner);
            StringAssert.Contains("ROLLBACK_SOAK_FIRST_MISMATCH", runner);
        }

        [Test]
        public void DebugRunnerRestoresHistoricalTickAndReplaysRecordedInputs()
        {
            GameObject gameObject = new GameObject("rollback-debug-runner-test");
            gameObject.SetActive(false);

            try
            {
                PredictionInputHistoryTickRecorder inputRecorder = gameObject.AddComponent<PredictionInputHistoryTickRecorder>();
                LocomotionSnapshotHistoryRecorder snapshotRecorder = gameObject.AddComponent<LocomotionSnapshotHistoryRecorder>();
                SamplingFakeRollbackSimulation simulation = gameObject.AddComponent<SamplingFakeRollbackSimulation>();
                LocalRollbackSynctestDebugRunner debugRunner = gameObject.AddComponent<LocalRollbackSynctestDebugRunner>();
                simulation.Configure(gameObject.transform, null);
                debugRunner.InputRecorder = inputRecorder;
                debugRunner.SnapshotRecorder = snapshotRecorder;
                debugRunner.SimulationBehaviour = simulation;
                debugRunner.RollbackFrames = 2;
                debugRunner.RunOnKeyDown = false;

                snapshotRecorder.History.Write(simulation.CaptureSnapshot(SimulationTick.Zero));
                for (int tick = 1; tick <= 3; tick++)
                {
                    PredictionInputFrame input = Input(tick, Vector2.right);
                    inputRecorder.History.Write(input);
                    simulation.Advance(in input);
                    CharacterSimulationSnapshot snapshot = simulation.CaptureSnapshot(new SimulationTick(tick));
                    snapshotRecorder.History.Write(in snapshot);
                }

                Assert.True(
                    debugRunner.RunDebugSynctest(),
                    $"{debugRunner.LastResult.FailureReason}: {string.Join(", ", debugRunner.LastResult.Comparison.Differences)}");
                Assert.True(debugRunner.HasResult);
                Assert.True(debugRunner.LastResult.Success);
                Assert.AreEqual(new SimulationTick(1), debugRunner.LastResult.RestoreTick);
                Assert.AreEqual(new SimulationTick(3), debugRunner.LastResult.EndTick);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void DebugRunnerRestoresLiveSnapshotAfterFailedReplay()
        {
            GameObject gameObject = new GameObject("rollback-debug-runner-restore-test");
            gameObject.SetActive(false);

            try
            {
                PredictionInputHistoryTickRecorder inputRecorder = gameObject.AddComponent<PredictionInputHistoryTickRecorder>();
                LocomotionSnapshotHistoryRecorder snapshotRecorder = gameObject.AddComponent<LocomotionSnapshotHistoryRecorder>();
                SamplingFakeRollbackSimulation simulation = gameObject.AddComponent<SamplingFakeRollbackSimulation>();
                LocalRollbackSynctestDebugRunner debugRunner = gameObject.AddComponent<LocalRollbackSynctestDebugRunner>();
                simulation.Configure(gameObject.transform, null);
                debugRunner.InputRecorder = inputRecorder;
                debugRunner.SnapshotRecorder = snapshotRecorder;
                debugRunner.SimulationBehaviour = simulation;
                debugRunner.RollbackFrames = 2;
                debugRunner.RunOnKeyDown = false;

                snapshotRecorder.History.Write(simulation.CaptureSnapshot(SimulationTick.Zero));
                for (int tick = 1; tick <= 3; tick++)
                {
                    PredictionInputFrame input = Input(tick, Vector2.right);
                    inputRecorder.History.Write(input);
                    simulation.Advance(in input);
                    CharacterSimulationSnapshot snapshot = simulation.CaptureSnapshot(new SimulationTick(tick));
                    snapshotRecorder.History.Write(in snapshot);
                }

                Assert.True(snapshotRecorder.History.TryGet(new SimulationTick(3), out CharacterSimulationSnapshot liveSnapshot));
                gameObject.transform.position += new Vector3(10f, 0f, 0f);
                inputRecorder.History.Write(Input(3, Vector2.left));

                Assert.False(debugRunner.RunDebugSynctest());
                Assert.True(debugRunner.HasResult);
                Assert.False(debugRunner.LastResult.Success);
                Assert.AreEqual(liveSnapshot.Position, gameObject.transform.position);
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(liveSnapshot.Yaw, gameObject.transform.eulerAngles.y)), Is.LessThan(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void DebugRunnerRestoresPresentationSamplesAfterHiddenReplay()
        {
            GameObject gameObject = new GameObject("rollback-debug-runner-presentation-restore-test");
            GameObject visualObject = new GameObject("rollback-debug-runner-presentation-visual-test");
            GameObject driverObject = new GameObject("rollback-debug-runner-presentation-driver-test");
            GameObject cameraObject = new GameObject("rollback-debug-runner-presentation-camera-test");
            GameObject cameraFollowObject = new GameObject("rollback-debug-runner-camera-follow-test");
            GameObject cameraAimObject = new GameObject("rollback-debug-runner-camera-aim-test");
            gameObject.SetActive(false);

            try
            {
                UnitySimulationTickDriver driver = driverObject.AddComponent<UnitySimulationTickDriver>();
                PredictionInputHistoryTickRecorder inputRecorder = gameObject.AddComponent<PredictionInputHistoryTickRecorder>();
                LocomotionSnapshotHistoryRecorder snapshotRecorder = gameObject.AddComponent<LocomotionSnapshotHistoryRecorder>();
                PresentationTransformInterpolator presentation = gameObject.AddComponent<PresentationTransformInterpolator>();
                SamplingFakeRollbackSimulation simulation = gameObject.AddComponent<SamplingFakeRollbackSimulation>();
                LocalRollbackSynctestDebugRunner debugRunner = gameObject.AddComponent<LocalRollbackSynctestDebugRunner>();
                ThirdPersonCameraController camera = cameraObject.AddComponent<ThirdPersonCameraController>();

                presentation.Source = gameObject.transform;
                presentation.VisualTarget = visualObject.transform;
                presentation.TickDriver = driver;
                presentation.SnapDistance = 20f;
                camera.FollowAnchorSource = visualObject.transform;
                camera.CameraFollowTarget = cameraFollowObject.transform;
                camera.CameraAimTarget = cameraAimObject.transform;
                camera.AutoTick = false;
                camera.DebugLog = false;
                camera.Sensitivity = new Vector2(10f, 10f);
                camera.ResetState(30f, 5f);
                simulation.Configure(gameObject.transform, presentation);
                debugRunner.InputRecorder = inputRecorder;
                debugRunner.SnapshotRecorder = snapshotRecorder;
                debugRunner.SimulationBehaviour = simulation;
                debugRunner.PresentationInterpolator = presentation;
                debugRunner.CameraController = camera;
                debugRunner.RollbackFrames = 2;
                debugRunner.RunOnKeyDown = false;

                snapshotRecorder.History.Write(Snapshot(0, Vector3.zero));
                for (int tick = 1; tick <= 3; tick++)
                {
                    PredictionInputFrame input = Input(tick, Vector2.right);
                    inputRecorder.History.Write(input);
                    gameObject.transform.position = new Vector3(tick, 0f, 0f);
                    CharacterSimulationSnapshot snapshot = Snapshot(tick, gameObject.transform.position);
                    snapshotRecorder.History.Write(in snapshot);
                }

                driver.ResetDriver(SimulationTick.Zero);
                driver.Advance(SimulationTickRate.Default.FixedDeltaSecondsFloat * 0.5f);
                gameObject.transform.position = new Vector3(2f, 0f, 0f);
                presentation.ResetSamples();
                presentation.CaptureSourceSample();
                gameObject.transform.position = new Vector3(3f, 0f, 0f);
                presentation.CaptureSourceSample();
                presentation.UpdateVisualTarget();
                camera.Resolve();
                Vector3 visualBefore = visualObject.transform.position;
                Vector3 cameraFollowBefore = cameraFollowObject.transform.position;
                Vector3 cameraAimBefore = cameraAimObject.transform.position;
                float cameraYawBefore = camera.Yaw;
                float cameraPitchBefore = camera.Pitch;

                inputRecorder.History.Write(Input(2, Vector2.left, false, Vector2.right));
                var events = new System.Collections.Generic.List<RuntimeDiagnosticLogEvent>();

                using (RuntimeDiagnosticLog.Capture(events.Add))
                    Assert.False(debugRunner.RunDebugSynctest());
                presentation.UpdateVisualTarget();
                RuntimeDiagnosticLogEvent timingProbe = events.First(e => e.Message == "rollback-timing-probe");

                Assert.AreEqual(new Vector3(3f, 0f, 0f), gameObject.transform.position);
                Assert.AreEqual(visualBefore, visualObject.transform.position);
                Assert.AreEqual(cameraFollowBefore, cameraFollowObject.transform.position);
                Assert.AreEqual(cameraAimBefore, cameraAimObject.transform.position);
                Assert.AreEqual(cameraYawBefore, camera.Yaw, 0.0001f);
                Assert.AreEqual(cameraPitchBefore, camera.Pitch, 0.0001f);
                Assert.True(presentation.HasCurrentTickPose);
                Assert.True(presentation.HasPreviousTickPose);
                Assert.AreEqual("Simulation.rollback-timing-probe", timingProbe.ChannelKey);
                StringAssert.Contains("ROLLBACK_TIMING_PROBE", timingProbe.Context);
                StringAssert.Contains("result=FAIL", timingProbe.Context);
                StringAssert.Contains("presentationState=True", timingProbe.Context);
                StringAssert.Contains("cameraState=local-only", timingProbe.Context);
                StringAssert.Contains("hasCameraFinal=True", timingProbe.Context);
                StringAssert.Contains("camYawStart=30.000/5.000", timingProbe.Context);
                StringAssert.Contains("camYawReplay=30.000/5.000", timingProbe.Context);
                StringAssert.Contains("camYawFinal=30.000/5.000", timingProbe.Context);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraAimObject);
                UnityEngine.Object.DestroyImmediate(cameraFollowObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(driverObject);
                UnityEngine.Object.DestroyImmediate(visualObject);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void DebugRunnerLogsFirstMismatchDetailsAfterFailedReplay()
        {
            GameObject gameObject = new GameObject("rollback-debug-runner-first-mismatch-test");
            gameObject.SetActive(false);

            try
            {
                PredictionInputHistoryTickRecorder inputRecorder = gameObject.AddComponent<PredictionInputHistoryTickRecorder>();
                LocomotionSnapshotHistoryRecorder snapshotRecorder = gameObject.AddComponent<LocomotionSnapshotHistoryRecorder>();
                SamplingFakeRollbackSimulation simulation = gameObject.AddComponent<SamplingFakeRollbackSimulation>();
                LocalRollbackSynctestDebugRunner debugRunner = gameObject.AddComponent<LocalRollbackSynctestDebugRunner>();
                simulation.Configure(gameObject.transform, null);
                debugRunner.InputRecorder = inputRecorder;
                debugRunner.SnapshotRecorder = snapshotRecorder;
                debugRunner.SimulationBehaviour = simulation;
                debugRunner.RollbackFrames = 2;
                debugRunner.RunOnKeyDown = false;

                snapshotRecorder.History.Write(simulation.CaptureSnapshot(SimulationTick.Zero));
                for (int tick = 1; tick <= 3; tick++)
                {
                    PredictionInputFrame input = Input(tick, Vector2.right);
                    inputRecorder.History.Write(input);
                    simulation.Advance(in input);
                    CharacterSimulationSnapshot snapshot = simulation.CaptureSnapshot(new SimulationTick(tick));
                    snapshotRecorder.History.Write(in snapshot);
                }

                inputRecorder.History.Write(Input(2, Vector2.left));
                var events = new System.Collections.Generic.List<RuntimeDiagnosticLogEvent>();

                using (RuntimeDiagnosticLog.Capture(events.Add))
                    Assert.False(debugRunner.RunDebugSynctest());

                RuntimeDiagnosticLogEvent detail = events.First(e => e.Message == "synctest-first-mismatch");
                Assert.AreEqual(RuntimeDiagnosticLogCategory.Simulation, detail.Category);
                StringAssert.Contains("stage=Replay", detail.Context);
                StringAssert.Contains("restore=1", detail.Context);
                StringAssert.Contains("end=3", detail.Context);
                StringAssert.Contains("inputMove=(-1.000,0.000)", detail.Context);
                StringAssert.Contains("expected={", detail.Context);
                StringAssert.Contains("actual={", detail.Context);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void LogFormatterOutputsScopeGroupsForPassFailAndFirstDrift()
        {
            CharacterSimulationSnapshot expected = SnapshotWithAnimation(1, BasicMovementPhase.MoveLoop, "WalkLoop", 0f);
            CharacterSimulationSnapshot actual = SnapshotWithAnimation(1, BasicMovementPhase.MoveLoop, "WalkLoop", 0.111111f);
            CharacterSimulationSnapshotComparison driftComparison =
                new CharacterSimulationSnapshotComparison(Array.Empty<string>(), new[] { "animationNormalizedTime" });
            LocalRollbackSynctestFirstMismatch firstDrift = new LocalRollbackSynctestFirstMismatch(
                LocalRollbackSynctestMismatchStage.Replay,
                new SimulationTick(1),
                true,
                Input(1, Vector2.zero),
                expected,
                actual,
                driftComparison);
            LocalRollbackSynctestResult passResult = new LocalRollbackSynctestResult(
                true,
                SimulationTick.Zero,
                new SimulationTick(1),
                SimulationTick.Zero,
                string.Empty,
                driftComparison,
                in firstDrift);

            string pass = LocalRollbackSynctestLogFormatter.FormatPass(in passResult);
            string firstPresentation = LocalRollbackSynctestLogFormatter.FormatFirstMismatch(in passResult);

            StringAssert.Contains("[rollback-synctest] PASS", pass);
            StringAssert.Contains("presentationDifferences=animationNormalizedTime", pass);
            StringAssert.Contains("firstPresentationDifferences=animationNormalizedTime", pass);
            StringAssert.Contains("[rollback-synctest] first-presentation-drift", firstPresentation);

            CharacterSimulationSnapshotComparison failureComparison =
                new CharacterSimulationSnapshotComparison(new[] { "position" }, new[] { "blackboard.animation.actionNormalizedTime" });
            LocalRollbackSynctestFirstMismatch firstMismatch = new LocalRollbackSynctestFirstMismatch(
                LocalRollbackSynctestMismatchStage.Replay,
                new SimulationTick(2),
                true,
                Input(2, Vector2.right),
                expected,
                SnapshotWithAnimation(2, Vector3.right, BasicMovementPhase.MoveLoop, "WalkLoop", 0.222222f),
                failureComparison);
            LocalRollbackSynctestResult failResult = new LocalRollbackSynctestResult(
                false,
                SimulationTick.Zero,
                new SimulationTick(2),
                SimulationTick.Zero,
                "first mismatch and snapshot mismatch",
                failureComparison,
                in firstMismatch);

            string fail = LocalRollbackSynctestLogFormatter.FormatFail(in failResult);
            string firstStrict = LocalRollbackSynctestLogFormatter.FormatFirstMismatch(in failResult);

            StringAssert.Contains("[rollback-synctest] FAIL", fail);
            StringAssert.Contains("firstDifferences=position", fail);
            StringAssert.Contains("firstPresentationDifferences=blackboard.animation.actionNormalizedTime", fail);
            StringAssert.Contains("differences=position", fail);
            StringAssert.Contains("presentationDifferences=blackboard.animation.actionNormalizedTime", fail);
            StringAssert.Contains("[rollback-synctest] first-mismatch", firstStrict);
        }

        [Test]
        public void DebugRunnerCanApplyReplayResultWithPresentationCorrection()
        {
            GameObject gameObject = new GameObject("rollback-debug-runner-visible-test");
            GameObject visualObject = new GameObject("rollback-debug-runner-visual-test");
            gameObject.SetActive(false);

            try
            {
                PredictionInputHistoryTickRecorder inputRecorder = gameObject.AddComponent<PredictionInputHistoryTickRecorder>();
                LocomotionSnapshotHistoryRecorder snapshotRecorder = gameObject.AddComponent<LocomotionSnapshotHistoryRecorder>();
                PresentationTransformInterpolator presentation = gameObject.AddComponent<PresentationTransformInterpolator>();
                SamplingFakeRollbackSimulation simulation = gameObject.AddComponent<SamplingFakeRollbackSimulation>();
                LocalRollbackSynctestDebugRunner debugRunner = gameObject.AddComponent<LocalRollbackSynctestDebugRunner>();
                presentation.Source = gameObject.transform;
                presentation.VisualTarget = visualObject.transform;
                simulation.Configure(gameObject.transform, presentation);
                debugRunner.InputRecorder = inputRecorder;
                debugRunner.SnapshotRecorder = snapshotRecorder;
                debugRunner.SimulationBehaviour = simulation;
                debugRunner.PresentationInterpolator = presentation;
                debugRunner.RollbackFrames = 2;
                debugRunner.RunOnKeyDown = false;
                debugRunner.ApplyReplayResultToScene = true;
                debugRunner.VisualCorrectionSeconds = 1f;

                snapshotRecorder.History.Write(simulation.CaptureSnapshot(SimulationTick.Zero));
                for (int tick = 1; tick <= 3; tick++)
                {
                    PredictionInputFrame input = Input(tick, Vector2.right);
                    inputRecorder.History.Write(input);
                    simulation.Advance(in input);
                    CharacterSimulationSnapshot snapshot = simulation.CaptureSnapshot(new SimulationTick(tick));
                    snapshotRecorder.History.Write(in snapshot);
                }

                CharacterSimulationSnapshot liveSnapshot = simulation.CaptureSnapshot(new SimulationTick(3));
                visualObject.transform.SetPositionAndRotation(liveSnapshot.Position, Quaternion.Euler(0f, liveSnapshot.Yaw, 0f));
                inputRecorder.History.Write(Input(3, Vector2.left));

                Assert.False(debugRunner.RunDebugSynctest());
                Vector3 replayPosition = gameObject.transform.position;

                Assert.That(Vector3.Distance(liveSnapshot.Position, replayPosition), Is.GreaterThan(0.001f));
                Assert.AreEqual(liveSnapshot.Position, visualObject.transform.position);
                Assert.True(presentation.IsCorrectionActive);

                presentation.AdvanceCorrection(0.5f);
                presentation.UpdateVisualTarget();

                Assert.That(Vector3.Distance(liveSnapshot.Position, visualObject.transform.position), Is.GreaterThan(0.001f));
                Assert.That(Vector3.Distance(replayPosition, visualObject.transform.position), Is.GreaterThan(0.001f));

                presentation.AdvanceCorrection(0.5f);
                presentation.UpdateVisualTarget();

                Assert.False(presentation.IsCorrectionActive);
                Assert.AreEqual(replayPosition, gameObject.transform.position);
                Assert.AreEqual(replayPosition, visualObject.transform.position);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(visualObject);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void LocalSynctestStopsWhenInputOrSnapshotIsMissing()
        {
            PredictionInputHistory inputHistory = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(8);
            FakeRollbackSimulation simulation = new FakeRollbackSimulation();
            snapshotHistory.Write(simulation.CaptureSnapshot(new SimulationTick(0)));
            snapshotHistory.Write(simulation.CaptureSnapshot(new SimulationTick(2)));

            LocalRollbackSynctestRunner runner = new LocalRollbackSynctestRunner(inputHistory, snapshotHistory, simulation);
            LocalRollbackSynctestResult missingInput = runner.Run(
                new SimulationTick(0),
                new SimulationTick(2),
                new SimulationTick(0),
                CharacterSimulationSnapshotTolerance.Default);
            LocalRollbackSynctestResult missingSnapshot = runner.Run(
                new SimulationTick(0),
                new SimulationTick(2),
                new SimulationTick(1),
                CharacterSimulationSnapshotTolerance.Default);

            Assert.False(missingInput.Success);
            StringAssert.Contains("missing input", missingInput.FailureReason);
            Assert.False(missingSnapshot.Success);
            StringAssert.Contains("missing snapshot", missingSnapshot.FailureReason);
        }

        [Test]
        public void RollbackCoreDoesNotReferenceForbiddenRuntimeObjects()
        {
            string root = Path.Combine(Application.dataPath, "Scripts/Simulation/Rollback");
            string[] coreFiles =
            {
                "LocalRollbackSynctestRunner.cs",
                "PredictionInputHistory.cs",
                "PredictionSnapshotHistory.cs",
                "PredictionHistoryQueryResult.cs",
                "PredictionInputFrame.cs",
                "PredictionButtonFrame.cs",
                "PredictionRollbackAuthorityPolicy.cs",
                "PredictionRollbackScopeResolver.cs",
                "CharacterSimulationSnapshotComparer.cs"
            };
            string combined = string.Join("\n", coreFiles.Select(file => File.ReadAllText(Path.Combine(root, file))));

            Assert.That(combined, Does.Not.Contain("Animancer"));
            Assert.That(combined, Does.Not.Contain("Cinemachine"));
            Assert.That(combined, Does.Not.Contain("ThirdPersonCamera"));
            Assert.That(combined, Does.Not.Contain("ThirdPersonPresentation"));
            Assert.That(combined, Does.Not.Contain("UnityEngine.InputSystem"));
            Assert.That(combined, Does.Not.Contain("CharacterController"));
            Assert.That(combined, Does.Not.Contain("BasicLocomotionPipeline"));
        }

        [Test]
        public void SnapshotModelDoesNotStoreUnityObjects()
        {
            var unityObjectFields = typeof(CharacterSimulationSnapshot)
                .GetProperties()
                .Where(property => typeof(UnityEngine.Object).IsAssignableFrom(property.PropertyType))
                .Select(property => property.Name)
                .ToArray();

            Assert.IsEmpty(unityObjectFields);

            string snapshotPath = Path.Combine(Application.dataPath, "Scripts/Simulation/Rollback/CharacterSimulationSnapshot.cs");
            string snapshotSource = File.ReadAllText(snapshotPath);
            Assert.That(snapshotSource, Does.Not.Contain("Animator"));
            Assert.That(snapshotSource, Does.Not.Contain("AnimationClip"));
            Assert.That(snapshotSource, Does.Not.Contain("InputAction"));
            Assert.That(snapshotSource, Does.Not.Contain("Animancer"));
            Assert.That(snapshotSource, Does.Not.Contain("Cinemachine"));
            Assert.That(snapshotSource, Does.Not.Contain("ThirdPersonCamera"));
            Assert.That(snapshotSource, Does.Not.Contain("ThirdPersonPresentation"));
            Assert.That(snapshotSource, Does.Not.Contain("PresentationDebugRestoreState"));
            Assert.That(snapshotSource, Does.Not.Contain("UnityEngine.Object"));
        }

        static PredictionInputFrame Input(int tick, Vector2 move, bool runHeld = false)
        {
            return Input(tick, move, runHeld, Vector2.zero);
        }

        static PredictionInputFrame Input(int tick, Vector2 move, bool runHeld, Vector2 look)
        {
            return new PredictionInputFrame(
                new SimulationTick(tick),
                move,
                look,
                runHeld,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None);
        }

        static CharacterSimulationSnapshot Snapshot(int tick, Vector3 position)
        {
            return new CharacterSimulationSnapshot(
                new SimulationTick(tick),
                position,
                0f,
                new CharacterStateMachineSnapshot(
                    CharacterStateIds.Idle,
                    0f,
                    CharacterStateVariant.None,
                    string.Empty,
                    Array.Empty<CharacterStateTag>()),
                false,
                BasicMovementGait.Walk,
                Vector3.zero,
                BasicMovementPhase.Idle,
                BasicMovementGait.Walk,
                string.Empty,
                0f);
        }

        static CharacterSimulationSnapshot SnapshotWithActionFacts(CharacterRuntimeActionFacts actionFacts)
        {
            CharacterRuntimeBlackboard blackboard = new CharacterRuntimeBlackboard();
            blackboard.WriteActionFacts(in actionFacts);
            CharacterStateMachineSnapshot state = new CharacterStateMachineSnapshot(
                CharacterStateIds.Dodge,
                0.1f,
                CharacterStateVariant.Directional,
                string.Empty,
                Array.Empty<CharacterStateTag>());

            return new CharacterSimulationSnapshot(
                new SimulationTick(actionFacts.SourceStep),
                Vector3.zero,
                0f,
                new CharacterStateMachineRestoreState(
                    state,
                    actionFacts.WorldDirection,
                    false),
                false,
                BasicMovementGait.Walk,
                Vector3.zero,
                BasicMovementPhase.Idle,
                BasicMovementGait.Walk,
                string.Empty,
                0f,
                blackboard.CaptureRestoreState());
        }

        static CharacterSimulationSnapshot SnapshotWithAnimation(
            int tick,
            BasicMovementPhase phase,
            string aliasKey,
            float normalizedTime,
            ActionAnimationKey actionKey = default,
            float actionNormalizedTime = 0f,
            bool actionHasPlayback = false)
        {
            return SnapshotWithAnimation(
                tick,
                Vector3.zero,
                phase,
                aliasKey,
                normalizedTime,
                actionKey,
                actionNormalizedTime,
                actionHasPlayback);
        }

        static CharacterSimulationSnapshot SnapshotWithAnimation(
            int tick,
            Vector3 position,
            BasicMovementPhase phase,
            string aliasKey,
            float normalizedTime,
            ActionAnimationKey actionKey = default,
            float actionNormalizedTime = 0f,
            bool actionHasPlayback = false)
        {
            CharacterRuntimeBlackboard blackboard = new CharacterRuntimeBlackboard();
            blackboard.WriteAnimationFacts(new CharacterRuntimeAnimationFacts(
                new AnimationPhasePlaybackProgress(
                    phase,
                    aliasKey,
                    normalizedTime,
                    !string.IsNullOrWhiteSpace(aliasKey),
                    false),
                aliasKey,
                new ActionAnimationPlaybackProgress(
                    actionKey,
                    actionNormalizedTime,
                    actionHasPlayback,
                    false,
                    actionHasPlayback ? new ActionAnimationPlaybackIntent(tick + 1) : ActionAnimationPlaybackIntent.Invalid),
                actionHasPlayback ? actionKey.Value : string.Empty,
                tick));

            return new CharacterSimulationSnapshot(
                new SimulationTick(tick),
                position,
                0f,
                new CharacterStateMachineRestoreState(
                    new CharacterStateMachineSnapshot(
                        CharacterStateIds.Idle,
                        0f,
                        CharacterStateVariant.None,
                        string.Empty,
                        Array.Empty<CharacterStateTag>()),
                    Vector3.zero,
                    false),
                false,
                BasicMovementGait.Walk,
                Vector3.zero,
                phase,
                BasicMovementGait.Walk,
                aliasKey,
                normalizedTime,
                blackboard.CaptureRestoreState());
        }

        static CharacterStateMachineContext Context(
            bool move,
            float deltaTime,
            CharacterInputRequestFact request = default)
        {
            Vector2 moveInput = move ? Vector2.up : Vector2.zero;
            MovementInputIntent intent = MovementInputIntent.FromRaw(moveInput, 0.1f, false);
            Vector3 worldDirection = move ? Vector3.forward : Vector3.zero;
            CharacterInputRequestFact resolvedRequest = request.HasRequest
                ? request
                : CharacterInputRequestFact.None(ThirdPersonInput.InputRequestKind.Dodge);

            return new CharacterStateMachineContext(
                deltaTime,
                1,
                intent,
                worldDirection,
                BasicMovementPhaseFacts.FromTiming(BasicMovementPhase.MoveLoop, 1f, BasicMovementSettings.FromConfig(null)),
                resolvedRequest);
        }

        static CharacterInputRequestFact DodgeRequest(CharacterStateVariant variant, Vector3 direction)
        {
            return new CharacterInputRequestFact(
                true,
                ThirdPersonInput.InputRequestKind.Dodge,
                1,
                4,
                30,
                variant,
                direction);
        }

        static CharacterResolvedAction ResolvedDodgeAction(
            CharacterStateVariant variant,
            Vector3 direction,
            int sourceStep)
        {
            CharacterActionRequest request = new CharacterActionRequest(
                CharacterFrameRequestProviderId.Dodge,
                ActionRequestType.Dodge,
                ThirdPersonInput.InputRequestKind.Dodge,
                sourceStep,
                sourceStep + 5,
                30,
                0,
                variant,
                direction);
            CharacterInputRequestFact requestFact = new CharacterInputRequestFact(
                true,
                ThirdPersonInput.InputRequestKind.Dodge,
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
                variant == CharacterStateVariant.Backstep ? ActionAnimationKeys.DodgeBackstep : ActionAnimationKeys.DodgeDirectional,
                motionSpec);
        }

        static ActionMotionResolveResult ResolveActionMotion(in CharacterStateMachineFrame frame, float deltaTime)
        {
            CharacterActionCatalogSO actionCatalog = LoadConfiguredCharacterConfigAsset().ActionCatalog;
            CharacterActionCatalog catalog = actionCatalog != null ? actionCatalog.ToCatalog() : CharacterActionCatalog.Empty;
            DodgeActionTuning dodgeTuning = default;
            bool hasDodgeAction = catalog.TryGetDodgeDefinition(out CharacterActionDefinition definition) &&
                                  definition.TryGetDodgeTuning(out dodgeTuning);
            ActionMotionSpec spec = DodgeActionMotionSpecAdapter.Resolve(
                frame.ActionMotionSpec,
                hasDodgeAction,
                in dodgeTuning);
            ActionMotionResolveInput input = new ActionMotionResolveInput(
                spec,
                deltaTime,
                frame.TimelineFacts,
                CharacterRuntimeActionFacts.Default,
                true);
            return ActionMotionResolver.Resolve(in input);
        }

        sealed class FakeMotionExecutor : IBasicLocomotionMotionExecutor
        {
            public float CurrentSpeed { get; private set; }
            public Vector3 LastWorldDirection { get; private set; }

            public void ExecuteBasicMovement(in MovementCommand command)
            {
                CurrentSpeed = command.SuppressInputPlanarMovement ? 0f : command.PlanarSpeed;
                LastWorldDirection = command.WorldDirection;
            }
        }

        sealed class MovingFakeMotionExecutor : IBasicLocomotionMotionExecutor
        {
            readonly Transform owner;

            public MovingFakeMotionExecutor(Transform owner)
            {
                this.owner = owner;
            }

            public float CurrentSpeed { get; private set; }
            public Vector3 LastWorldDirection { get; private set; }

            public void ExecuteBasicMovement(in MovementCommand command)
            {
                Vector3 inputDisplacement = command.SuppressInputPlanarMovement
                    ? Vector3.zero
                    : command.WorldDirection * command.PlanarSpeed * command.DeltaTime;
                CurrentSpeed = command.DeltaTime > 0f ? inputDisplacement.magnitude / command.DeltaTime : 0f;
                LastWorldDirection = command.WorldDirection;

                if (owner == null)
                    return;

                owner.position += inputDisplacement;
                if (command.DesiredFacing.sqrMagnitude > 0.000001f)
                    owner.rotation = Quaternion.LookRotation(command.DesiredFacing, Vector3.up);
            }
        }

        sealed class FakePredictionSource : MonoBehaviour, IPredictionInputFrameSource
        {
            public bool TryReadPredictionInput(in SimulationTickContext context, out PredictionInputFrame frame)
            {
                frame = Input(context.TickValue, Vector2.right);
                return true;
            }
        }

        sealed class FakeRollbackSimulation : ILocalRollbackSynctestSimulation
        {
            Vector3 position;

            public CharacterSimulationSnapshot CaptureSnapshot(SimulationTick tick)
            {
                return Snapshot(tick.Value, position);
            }

            public void Restore(in CharacterSimulationSnapshot snapshot)
            {
                position = snapshot.Position;
            }

            public void Advance(in PredictionInputFrame input)
            {
                position += new Vector3(input.Move.x, 0f, input.Move.y);
            }
        }

        sealed class PresentationDriftRollbackSimulation : ILocalRollbackSynctestSimulation
        {
            float normalizedTime;

            public CharacterSimulationSnapshot CaptureSnapshot(SimulationTick tick)
            {
                return SnapshotWithAnimation(tick.Value, BasicMovementPhase.MoveLoop, "WalkLoop", normalizedTime);
            }

            public void Restore(in CharacterSimulationSnapshot snapshot)
            {
                normalizedTime = snapshot.AnimationNormalizedTime;
            }

            public void Advance(in PredictionInputFrame input)
            {
                normalizedTime = 0.111111f;
            }
        }

        sealed class PresentationThenStrictMismatchRollbackSimulation : ILocalRollbackSynctestSimulation
        {
            Vector3 position;
            float normalizedTime;

            public CharacterSimulationSnapshot CaptureSnapshot(SimulationTick tick)
            {
                return SnapshotWithAnimation(tick.Value, position, BasicMovementPhase.MoveLoop, "WalkLoop", normalizedTime);
            }

            public void Restore(in CharacterSimulationSnapshot snapshot)
            {
                position = snapshot.Position;
                normalizedTime = snapshot.AnimationNormalizedTime;
            }

            public void Advance(in PredictionInputFrame input)
            {
                normalizedTime = 0.111111f;
            }
        }

        sealed class TransientMismatchRollbackSimulation : ILocalRollbackSynctestSimulation
        {
            Vector3 position;

            public CharacterSimulationSnapshot CaptureSnapshot(SimulationTick tick)
            {
                return Snapshot(tick.Value, position);
            }

            public void Restore(in CharacterSimulationSnapshot snapshot)
            {
                position = snapshot.Position;
            }

            public void Advance(in PredictionInputFrame input)
            {
                if (input.Tick.Value == 1 || input.Tick.Value == 2)
                {
                    position = Vector3.zero;
                    return;
                }

                position += new Vector3(input.Move.x, 0f, input.Move.y);
            }
        }

        sealed class BrokenRestoreRollbackSimulation : ILocalRollbackSynctestSimulation
        {
            Vector3 position;

            public BrokenRestoreRollbackSimulation(Vector3 initialPosition)
            {
                position = initialPosition;
            }

            public CharacterSimulationSnapshot CaptureSnapshot(SimulationTick tick)
            {
                return Snapshot(tick.Value, position);
            }

            public void Restore(in CharacterSimulationSnapshot snapshot)
            {
            }

            public void Advance(in PredictionInputFrame input)
            {
                position += new Vector3(input.Move.x, 0f, input.Move.y);
            }
        }

        sealed class SamplingFakeRollbackSimulation : MonoBehaviour, ILocalRollbackSynctestSimulation
        {
            Transform owner;
            PresentationTransformInterpolator presentation;

            public void Configure(
                Transform ownerTransform,
                PresentationTransformInterpolator presentationInterpolator)
            {
                owner = ownerTransform;
                presentation = presentationInterpolator;
            }

            public CharacterSimulationSnapshot CaptureSnapshot(SimulationTick tick)
            {
                return Snapshot(tick.Value, owner != null ? owner.position : Vector3.zero);
            }

            public void Restore(in CharacterSimulationSnapshot snapshot)
            {
                if (owner != null)
                    owner.SetPositionAndRotation(snapshot.Position, Quaternion.Euler(0f, snapshot.Yaw, 0f));
            }

            public void Advance(in PredictionInputFrame input)
            {
                if (owner == null)
                    return;

                owner.position += new Vector3(input.Move.x, 0f, input.Move.y);
                presentation?.CaptureSourceSample();
                presentation?.UpdateVisualTarget();
            }
        }
    }
}

