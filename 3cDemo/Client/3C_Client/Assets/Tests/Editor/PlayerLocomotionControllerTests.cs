using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Animancer;
using NUnit.Framework;
using ThirdPersonAnimation;
using ThirdPersonCamera;
using ThirdPersonMovement;
using ThirdPersonSimulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThirdPersonMovement.Tests
{
    public sealed class PlayerLocomotionControllerTests
    {
        static readonly BasicMovementSettings Settings = new BasicMovementSettings(4f, 0.1f, 720f, 0.08f, 0.08f);

        [Test]
        public void StateMachineStartsInIdle()
        {
            BasicLocomotionStateMachine stateMachine = new BasicLocomotionStateMachine();

            Assert.AreEqual(BasicMovementPhase.Idle, stateMachine.Phase);
            Assert.AreEqual(0f, stateMachine.PhaseTime);
            Assert.True(stateMachine.UsesDefaultGraph);
        }

        [Test]
        public void DefaultStateGraphValidatorHasNoErrors()
        {
            LocomotionStateGraphDefinition definition = LocomotionStateGraphDefinition.CreateDefault();

            LocomotionStateGraphValidationResult result = LocomotionStateGraphValidator.Validate(definition);

            Assert.False(result.HasErrors, string.Join("\n", result.Errors));
            Assert.AreEqual(BasicMovementPhase.Idle, definition.InitialState);
            Assert.AreEqual(4, definition.EnabledStates.Length);
            Assert.AreEqual(6, definition.Transitions.Length);
        }

        [Test]
        public void StateGraphValidatorFindsMissingInitialState()
        {
            LocomotionStateGraphDefinition definition = new LocomotionStateGraphDefinition(
                BasicMovementPhase.MoveLoop,
                new[] { BasicMovementPhase.Idle },
                LocomotionStateGraphTransitionConfig.CreateDefaultTransitions());

            LocomotionStateGraphValidationResult result = LocomotionStateGraphValidator.Validate(definition);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("Initial state"));
        }

        [Test]
        public void StateGraphValidatorFindsMissingTransitionTarget()
        {
            LocomotionStateGraphDefinition definition = new LocomotionStateGraphDefinition(
                BasicMovementPhase.Idle,
                new[] { BasicMovementPhase.Idle },
                new[]
                {
                    new LocomotionStateGraphTransitionConfig(
                        BasicMovementPhase.Idle,
                        BasicMovementPhase.MoveStart,
                        0,
                        LocomotionStateGraphCondition.HasMoveIntent)
                });

            LocomotionStateGraphValidationResult result = LocomotionStateGraphValidator.Validate(definition);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("target state"));
        }

        [Test]
        public void StateGraphValidatorFindsDuplicateTransition()
        {
            LocomotionStateGraphDefinition definition = new LocomotionStateGraphDefinition(
                BasicMovementPhase.Idle,
                LocomotionStateGraphDefinition.CreateDefaultStates(),
                new[]
                {
                    new LocomotionStateGraphTransitionConfig(BasicMovementPhase.Idle, BasicMovementPhase.MoveStart, 1, LocomotionStateGraphCondition.HasMoveIntent),
                    new LocomotionStateGraphTransitionConfig(BasicMovementPhase.Idle, BasicMovementPhase.MoveStart, 2, LocomotionStateGraphCondition.HasMoveIntent)
                });

            LocomotionStateGraphValidationResult result = LocomotionStateGraphValidator.Validate(definition);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("Duplicate transition"));
        }

        [Test]
        public void StateGraphValidatorFindsPriorityConflict()
        {
            LocomotionStateGraphDefinition definition = new LocomotionStateGraphDefinition(
                BasicMovementPhase.Idle,
                LocomotionStateGraphDefinition.CreateDefaultStates(),
                new[]
                {
                    new LocomotionStateGraphTransitionConfig(BasicMovementPhase.Idle, BasicMovementPhase.MoveStart, 1, LocomotionStateGraphCondition.HasMoveIntent),
                    new LocomotionStateGraphTransitionConfig(BasicMovementPhase.Idle, BasicMovementPhase.MoveStop, 1, LocomotionStateGraphCondition.Always)
                });

            LocomotionStateGraphValidationResult result = LocomotionStateGraphValidator.Validate(definition);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("Priority conflict"));
        }

        [Test]
        public void StateGraphValidatorWarnsAboutUnreachableState()
        {
            LocomotionStateGraphDefinition definition = new LocomotionStateGraphDefinition(
                BasicMovementPhase.Idle,
                LocomotionStateGraphDefinition.CreateDefaultStates(),
                new[]
                {
                    new LocomotionStateGraphTransitionConfig(BasicMovementPhase.Idle, BasicMovementPhase.MoveStart, 1, LocomotionStateGraphCondition.HasMoveIntent)
                });

            LocomotionStateGraphValidationResult result = LocomotionStateGraphValidator.Validate(definition);

            Assert.That(result.Warnings, Is.Not.Empty);
            Assert.That(string.Join("\n", result.Warnings), Does.Contain(nameof(BasicMovementPhase.MoveLoop)));
        }

        [Test]
        public void StateMachineMovesFromIdleToMoveStart()
        {
            BasicLocomotionStateMachine stateMachine = new BasicLocomotionStateMachine();

            BasicMovementPhase phase = stateMachine.Tick(true, 0.02f, Settings);

            Assert.AreEqual(BasicMovementPhase.MoveStart, phase);
            Assert.AreEqual(0f, stateMachine.PhaseTime);
        }

        [Test]
        public void StateMachineHonorsMoveStartMinimumTime()
        {
            BasicLocomotionStateMachine stateMachine = new BasicLocomotionStateMachine();

            stateMachine.Tick(true, 0.02f, Settings);
            stateMachine.Tick(true, 0.04f, Settings);

            Assert.AreEqual(BasicMovementPhase.MoveStart, stateMachine.Phase);

            stateMachine.Tick(true, 0.04f, Settings);

            Assert.AreEqual(BasicMovementPhase.MoveLoop, stateMachine.Phase);
        }

        [Test]
        public void StateMachineCanUseConfiguredGraph()
        {
            LocomotionStateGraphDefinition definition = LocomotionStateGraphDefinition.CreateDefault();
            BasicLocomotionStateMachine stateMachine = new BasicLocomotionStateMachine(definition);

            BasicMovementPhase phase = stateMachine.Tick(true, 0.02f, Settings);

            Assert.AreEqual(BasicMovementPhase.MoveStart, phase);
            Assert.False(stateMachine.UsesDefaultGraph);
        }

        [Test]
        public void StateMachineCanStopFromMoveStart()
        {
            BasicLocomotionStateMachine stateMachine = new BasicLocomotionStateMachine();

            stateMachine.Tick(true, 0.02f, Settings);
            stateMachine.Tick(false, 0.02f, Settings);

            Assert.AreEqual(BasicMovementPhase.MoveStop, stateMachine.Phase);
        }

        [Test]
        public void StateMachineCanStopFromMoveLoop()
        {
            BasicLocomotionStateMachine stateMachine = new BasicLocomotionStateMachine();

            stateMachine.Tick(true, 0.02f, Settings);
            stateMachine.Tick(true, 0.08f, Settings);
            stateMachine.Tick(false, 0.02f, Settings);

            Assert.AreEqual(BasicMovementPhase.MoveStop, stateMachine.Phase);
        }

        [Test]
        public void StateMachineHonorsMoveStopMinimumTime()
        {
            BasicLocomotionStateMachine stateMachine = new BasicLocomotionStateMachine();

            stateMachine.Tick(true, 0.02f, Settings);
            stateMachine.Tick(false, 0.02f, Settings);
            stateMachine.Tick(false, 0.04f, Settings);

            Assert.AreEqual(BasicMovementPhase.MoveStop, stateMachine.Phase);

            stateMachine.Tick(false, 0.04f, Settings);

            Assert.AreEqual(BasicMovementPhase.Idle, stateMachine.Phase);
        }

        [Test]
        public void StateMachineUsesMoveStopExitDuration()
        {
            BasicLocomotionStateMachine stateMachine = new BasicLocomotionStateMachine();
            BasicMovementSettings settings = Settings.WithMoveStopExitDuration(0.2f);

            stateMachine.Tick(true, 0.02f, settings);
            stateMachine.Tick(false, 0.02f, settings);
            stateMachine.Tick(false, 0.08f, settings);

            Assert.AreEqual(BasicMovementPhase.MoveStop, stateMachine.Phase);

            stateMachine.Tick(false, 0.12f, settings);

            Assert.AreEqual(BasicMovementPhase.Idle, stateMachine.Phase);
        }

        [Test]
        public void StateMachineCanRestartFromMoveStop()
        {
            BasicLocomotionStateMachine stateMachine = new BasicLocomotionStateMachine();

            stateMachine.Tick(true, 0.02f, Settings);
            stateMachine.Tick(false, 0.02f, Settings);
            stateMachine.Tick(true, 0.02f, Settings);

            Assert.AreEqual(BasicMovementPhase.MoveStart, stateMachine.Phase);
        }

        [Test]
        public void StateMachineResetReturnsToIdleAndClearsTime()
        {
            BasicLocomotionStateMachine stateMachine = new BasicLocomotionStateMachine();
            stateMachine.Tick(true, 0.02f, Settings);
            stateMachine.Tick(true, 0.04f, Settings);

            stateMachine.Reset();

            Assert.AreEqual(BasicMovementPhase.Idle, stateMachine.Phase);
            Assert.AreEqual(0f, stateMachine.PhaseTime);
        }

        [Test]
        public void StateMachineExposesActivePath()
        {
            BasicLocomotionStateMachine stateMachine = new BasicLocomotionStateMachine();

            Assert.That(stateMachine.ActivePath, Does.Contain(nameof(BasicMovementPhase.Idle)));
        }

        [Test]
        public void StateMachineClampsNegativeDeltaTime()
        {
            BasicLocomotionStateMachine stateMachine = new BasicLocomotionStateMachine();

            stateMachine.Tick(true, 0.02f, Settings);
            stateMachine.Tick(true, -1f, Settings);

            Assert.AreEqual(0f, stateMachine.PhaseTime);
        }

        [Test]
        public void StateMachineFixedDeltaKeepsMinimumTimeSemantics()
        {
            BasicLocomotionStateMachine stateMachine = new BasicLocomotionStateMachine(LocomotionStateGraphDefinition.CreateDefault());

            stateMachine.Tick(true, 0.02f, Settings);
            stateMachine.Tick(true, 0.02f, Settings);
            stateMachine.Tick(true, 0.02f, Settings);

            Assert.AreEqual(BasicMovementPhase.MoveStart, stateMachine.Phase);

            stateMachine.Tick(true, 0.02f, Settings);
            stateMachine.Tick(true, 0.02f, Settings);

            Assert.AreEqual(BasicMovementPhase.MoveLoop, stateMachine.Phase);
        }

        [Test]
        public void AnimationSetResolvesDefaultKeys()
        {
            LocomotionAnimationSetSO animationSet = ScriptableObject.CreateInstance<LocomotionAnimationSetSO>();
            try
            {
                Assert.AreEqual(StringReference.Get("Idle"), animationSet.Resolve(BasicMovementPhase.Idle, LocomotionAnimationGait.Walk, LocomotionAnimationGait.Run));
                Assert.AreEqual(StringReference.Get("WalkStart"), animationSet.Resolve(BasicMovementPhase.MoveStart, LocomotionAnimationGait.Walk, LocomotionAnimationGait.Run));
                Assert.AreEqual(StringReference.Get("RunStart"), animationSet.Resolve(BasicMovementPhase.MoveStart, LocomotionAnimationGait.Run, LocomotionAnimationGait.Run));
                Assert.AreEqual(StringReference.Get("WalkLoop"), animationSet.Resolve(BasicMovementPhase.MoveLoop, LocomotionAnimationGait.Walk, LocomotionAnimationGait.Run));
                Assert.AreEqual(StringReference.Get("RunLoop"), animationSet.Resolve(BasicMovementPhase.MoveLoop, LocomotionAnimationGait.Run, LocomotionAnimationGait.Run));
                Assert.AreEqual(StringReference.Get("WalkEnd"), animationSet.Resolve(BasicMovementPhase.MoveStop, LocomotionAnimationGait.Walk, LocomotionAnimationGait.Walk));
                Assert.AreEqual(StringReference.Get("RunEnd"), animationSet.Resolve(BasicMovementPhase.MoveStop, LocomotionAnimationGait.Walk, LocomotionAnimationGait.Run));
            }
            finally
            {
                Object.DestroyImmediate(animationSet);
            }
        }

        [Test]
        public void AnimationSetResolvesDefaultEntries()
        {
            LocomotionAnimationSetSO animationSet = ScriptableObject.CreateInstance<LocomotionAnimationSetSO>();
            try
            {
                Assert.AreEqual("Idle", animationSet.ResolveEntry(BasicMovementPhase.Idle, LocomotionAnimationGait.Walk, LocomotionAnimationGait.Run).Key);
                Assert.AreEqual("WalkStart", animationSet.ResolveEntry(BasicMovementPhase.MoveStart, LocomotionAnimationGait.Walk, LocomotionAnimationGait.Run).Key);
                Assert.AreEqual("RunStart", animationSet.ResolveEntry(BasicMovementPhase.MoveStart, LocomotionAnimationGait.Run, LocomotionAnimationGait.Run).Key);
                Assert.AreEqual("WalkLoop", animationSet.ResolveEntry(BasicMovementPhase.MoveLoop, LocomotionAnimationGait.Walk, LocomotionAnimationGait.Run).Key);
                Assert.AreEqual("RunLoop", animationSet.ResolveEntry(BasicMovementPhase.MoveLoop, LocomotionAnimationGait.Run, LocomotionAnimationGait.Run).Key);
                Assert.AreEqual("WalkEnd", animationSet.ResolveEntry(BasicMovementPhase.MoveStop, LocomotionAnimationGait.Walk, LocomotionAnimationGait.Walk).Key);
                Assert.AreEqual("RunEnd", animationSet.ResolveEntry(BasicMovementPhase.MoveStop, LocomotionAnimationGait.Walk, LocomotionAnimationGait.Run).Key);
            }
            finally
            {
                Object.DestroyImmediate(animationSet);
            }
        }

        [Test]
        public void AnimationSetRunEndExitDurationOverrideWins()
        {
            LocomotionAnimationSetSO animationSet = ScriptableObject.CreateInstance<LocomotionAnimationSetSO>();
            try
            {
                SetAnimationEntry(animationSet, "runEnd", new LocomotionAnimationEntry(
                    "RunEnd",
                    exitDurationMode: LocomotionAnimationExitDurationMode.Manual,
                    exitDurationOverride: 0.31f));

                LocomotionAnimationTiming timing = animationSet.ResolveTiming(
                    BasicMovementPhase.MoveStop,
                    LocomotionAnimationGait.Run,
                    LocomotionAnimationGait.Run,
                    0.08f);

                Assert.AreEqual(0.31f, timing.ExitDuration, 0.000001f);
            }
            finally
            {
                Object.DestroyImmediate(animationSet);
            }
        }

        [Test]
        public void AnimationSetWalkEndExitDurationOverrideWins()
        {
            LocomotionAnimationSetSO animationSet = ScriptableObject.CreateInstance<LocomotionAnimationSetSO>();
            try
            {
                SetAnimationEntry(animationSet, "walkEnd", new LocomotionAnimationEntry(
                    "WalkEnd",
                    exitDurationMode: LocomotionAnimationExitDurationMode.Manual,
                    exitDurationOverride: 0.19f));

                LocomotionAnimationTiming timing = animationSet.ResolveTiming(
                    BasicMovementPhase.MoveStop,
                    LocomotionAnimationGait.Walk,
                    LocomotionAnimationGait.Walk,
                    0.08f);

                Assert.AreEqual(0.19f, timing.ExitDuration, 0.000001f);
            }
            finally
            {
                Object.DestroyImmediate(animationSet);
            }
        }

        [Test]
        public void AnimationSetUsesFallbackExitDuration()
        {
            LocomotionAnimationSetSO animationSet = ScriptableObject.CreateInstance<LocomotionAnimationSetSO>();
            try
            {
                LocomotionAnimationTiming timing = animationSet.ResolveTiming(
                    BasicMovementPhase.MoveStop,
                    LocomotionAnimationGait.Run,
                    LocomotionAnimationGait.Run,
                    0.42f);

                Assert.AreEqual(0.42f, timing.ExitDuration, 0.000001f);
            }
            finally
            {
                Object.DestroyImmediate(animationSet);
            }
        }

        [Test]
        public void AnimationSetValidationReportsMissingRunEnd()
        {
            LocomotionAnimationSetSO animationSet = ScriptableObject.CreateInstance<LocomotionAnimationSetSO>();
            try
            {
                SetAnimationEntry(animationSet, "runEnd", new LocomotionAnimationEntry(string.Empty));

                LocomotionAnimationSetValidationResult result = animationSet.Validate();

                Assert.True(result.HasErrors);
                Assert.That(string.Join("\n", result.Errors), Does.Contain("MoveStop + Run"));
            }
            finally
            {
                Object.DestroyImmediate(animationSet);
            }
        }

        [Test]
        public void AnimationSetValidationReportsMissingManualStopExitDuration()
        {
            LocomotionAnimationSetSO animationSet = ScriptableObject.CreateInstance<LocomotionAnimationSetSO>();
            try
            {
                SetAnimationEntry(animationSet, "runEnd", new LocomotionAnimationEntry(
                    "RunEnd",
                    exitDurationMode: LocomotionAnimationExitDurationMode.Manual,
                    exitDurationOverride: -1f));

                LocomotionAnimationSetValidationResult result = animationSet.Validate();

                Assert.True(result.HasErrors);
                Assert.That(string.Join("\n", result.Errors), Does.Contain("MoveStop exit duration"));
            }
            finally
            {
                Object.DestroyImmediate(animationSet);
            }
        }

        [Test]
        public void RunEndMappingDoesNotChangeLogicPhase()
        {
            LocomotionAnimationSetSO animationSet = ScriptableObject.CreateInstance<LocomotionAnimationSetSO>();
            try
            {
                MovementAnimationContext context = new MovementAnimationContext(
                    BasicMovementPhase.MoveStop,
                    false,
                    0f,
                    Vector3.forward,
                    0f);

                StringReference key = animationSet.Resolve(context.Phase, LocomotionAnimationGait.Walk, LocomotionAnimationGait.Run);

                Assert.AreEqual(StringReference.Get("RunEnd"), key);
                Assert.AreEqual(BasicMovementPhase.MoveStop, context.Phase);
            }
            finally
            {
                Object.DestroyImmediate(animationSet);
            }
        }

        [Test]
        public void PipelineOutputsCommandAndAnimationPhase()
        {
            BasicLocomotionPipeline pipeline = new BasicLocomotionPipeline();
            BasicLocomotionStateMachine stateMachine = new BasicLocomotionStateMachine();
            TestCameraBasis cameraBasis = new TestCameraBasis(Vector3.forward, Vector3.right);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.02f, Vector2.up, Vector2.zero);

            BasicLocomotionFrame frame = pipeline.Tick(in input, in Settings, cameraBasis, stateMachine);

            Assert.AreEqual(BasicMovementPhase.MoveStart, frame.Phase);
            Assert.AreEqual(BasicMovementPhase.MoveStart, frame.Command.Phase);
            Assert.AreEqual(Vector3.forward, frame.WorldDirection);
            Assert.AreEqual(Settings.MaxPlanarSpeed, frame.Command.PlanarSpeed);

            MovementAnimationContext animationContext = new MovementAnimationContext(
                frame.Phase,
                frame.Intent.HasMoveIntent,
                frame.Intent.Strength,
                frame.WorldDirection,
                frame.Command.PlanarSpeed);

            Assert.AreEqual(frame.Phase, animationContext.Phase);
        }

        [Test]
        public void ControllerUsesRunEndExitDurationForMoveStop()
        {
            GameObject gameObject = new GameObject("locomotion-run-end-duration-test");
            gameObject.SetActive(false);
            LocomotionAnimationSetSO animationSet = null;

            try
            {
                PlayerLocomotionController controller = gameObject.AddComponent<PlayerLocomotionController>();
                BasicLocomotionAnimancerPresenter presenter = gameObject.AddComponent<BasicLocomotionAnimancerPresenter>();
                animationSet = ScriptableObject.CreateInstance<LocomotionAnimationSetSO>();
                SetAnimationEntry(animationSet, "runEnd", new LocomotionAnimationEntry(
                    "RunEnd",
                    exitDurationMode: LocomotionAnimationExitDurationMode.Manual,
                    exitDurationOverride: 0.2f));

                controller.LocomotionPresenter = presenter;
                presenter.AnimationSet = animationSet;
                controller.SetInputSource(new FakeInputSource(new BasicLocomotionInputSnapshot(0.02f, Vector2.zero, Vector2.zero)));
                controller.SetMotionExecutor(new FakeMotionExecutor());
                DisableCameraDebugLog(controller);

                gameObject.SetActive(true);
                controller.Tick(new BasicLocomotionInputSnapshot(0.02f, Vector2.up, Vector2.zero));
                controller.Tick(new BasicLocomotionInputSnapshot(0.02f, Vector2.zero, Vector2.zero));
                controller.Tick(new BasicLocomotionInputSnapshot(0.08f, Vector2.zero, Vector2.zero));

                Assert.AreEqual(BasicMovementPhase.MoveStop, controller.CurrentPhase);

                controller.Tick(new BasicLocomotionInputSnapshot(0.12f, Vector2.zero, Vector2.zero));

                Assert.AreEqual(BasicMovementPhase.Idle, controller.CurrentPhase);
            }
            finally
            {
                if (animationSet != null)
                    Object.DestroyImmediate(animationSet);

                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ControllerUsesWalkEndExitDurationForMoveStop()
        {
            GameObject gameObject = new GameObject("locomotion-walk-end-duration-test");
            gameObject.SetActive(false);
            LocomotionAnimationSetSO animationSet = null;

            try
            {
                PlayerLocomotionController controller = gameObject.AddComponent<PlayerLocomotionController>();
                BasicLocomotionAnimancerPresenter presenter = gameObject.AddComponent<BasicLocomotionAnimancerPresenter>();
                animationSet = ScriptableObject.CreateInstance<LocomotionAnimationSetSO>();
                SetAnimationEntry(animationSet, "walkEnd", new LocomotionAnimationEntry(
                    "WalkEnd",
                    exitDurationMode: LocomotionAnimationExitDurationMode.Manual,
                    exitDurationOverride: 0.18f));

                controller.LocomotionPresenter = presenter;
                presenter.AnimationSet = animationSet;
                controller.SetInputSource(new FakeInputSource(new BasicLocomotionInputSnapshot(0.02f, Vector2.zero, Vector2.zero)));
                controller.SetMotionExecutor(new FakeMotionExecutor());
                DisableCameraDebugLog(controller);

                gameObject.SetActive(true);
                controller.Tick(new BasicLocomotionInputSnapshot(0.02f, new Vector2(0f, 0.3f), Vector2.zero));
                controller.Tick(new BasicLocomotionInputSnapshot(0.02f, Vector2.zero, Vector2.zero));
                controller.Tick(new BasicLocomotionInputSnapshot(0.08f, Vector2.zero, Vector2.zero));

                Assert.AreEqual(BasicMovementPhase.MoveStop, controller.CurrentPhase);

                controller.Tick(new BasicLocomotionInputSnapshot(0.1f, Vector2.zero, Vector2.zero));

                Assert.AreEqual(BasicMovementPhase.Idle, controller.CurrentPhase);
            }
            finally
            {
                if (animationSet != null)
                    Object.DestroyImmediate(animationSet);

                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ControllerRestartsFromMoveStopBeforeExitDuration()
        {
            GameObject gameObject = new GameObject("locomotion-stop-restart-duration-test");
            gameObject.SetActive(false);
            LocomotionAnimationSetSO animationSet = null;

            try
            {
                PlayerLocomotionController controller = gameObject.AddComponent<PlayerLocomotionController>();
                BasicLocomotionAnimancerPresenter presenter = gameObject.AddComponent<BasicLocomotionAnimancerPresenter>();
                animationSet = ScriptableObject.CreateInstance<LocomotionAnimationSetSO>();
                SetAnimationEntry(animationSet, "runEnd", new LocomotionAnimationEntry(
                    "RunEnd",
                    exitDurationMode: LocomotionAnimationExitDurationMode.Manual,
                    exitDurationOverride: 0.5f));

                controller.LocomotionPresenter = presenter;
                presenter.AnimationSet = animationSet;
                controller.SetInputSource(new FakeInputSource(new BasicLocomotionInputSnapshot(0.02f, Vector2.zero, Vector2.zero)));
                controller.SetMotionExecutor(new FakeMotionExecutor());
                DisableCameraDebugLog(controller);

                gameObject.SetActive(true);
                controller.Tick(new BasicLocomotionInputSnapshot(0.02f, Vector2.up, Vector2.zero));
                controller.Tick(new BasicLocomotionInputSnapshot(0.02f, Vector2.zero, Vector2.zero));
                controller.Tick(new BasicLocomotionInputSnapshot(0.02f, Vector2.up, Vector2.zero));

                Assert.AreEqual(BasicMovementPhase.MoveStart, controller.CurrentPhase);
            }
            finally
            {
                if (animationSet != null)
                    Object.DestroyImmediate(animationSet);

                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ControllerCanBeDrivenByFakeInputSourceAndMotionExecutor()
        {
            GameObject gameObject = new GameObject("locomotion-test");
            gameObject.SetActive(false);

            try
            {
                PlayerLocomotionController controller = gameObject.AddComponent<PlayerLocomotionController>();
                FakeInputSource inputSource = new FakeInputSource(new BasicLocomotionInputSnapshot(0.02f, Vector2.up, Vector2.zero));
                FakeMotionExecutor motionExecutor = new FakeMotionExecutor();

                controller.SetInputSource(inputSource);
                controller.SetMotionExecutor(motionExecutor);
                typeof(PlayerLocomotionController)
                    .GetField("debugCameraLog", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(controller, false);

                gameObject.SetActive(true);
                typeof(PlayerLocomotionController)
                    .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(controller, null);

                Assert.AreEqual(1, inputSource.ReadCount);
                Assert.AreEqual(1, motionExecutor.ExecuteCount);
                Assert.AreEqual(BasicMovementPhase.MoveStart, controller.CurrentPhase);
                Assert.AreEqual(BasicMovementPhase.MoveStart, motionExecutor.LastCommand.Phase);
                Assert.That(motionExecutor.LastCommand.PlanarSpeed, Is.GreaterThan(0f));
                Assert.True(controller.AutoUpdate);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ControllerUsesDefaultStateGraphWhenConfigMissing()
        {
            GameObject gameObject = new GameObject("locomotion-default-graph-test");
            gameObject.SetActive(false);

            try
            {
                PlayerLocomotionController controller = gameObject.AddComponent<PlayerLocomotionController>();
                controller.SetInputSource(new FakeInputSource(new BasicLocomotionInputSnapshot(0.02f, Vector2.up, Vector2.zero)));
                controller.SetMotionExecutor(new FakeMotionExecutor());
                DisableCameraDebugLog(controller);

                gameObject.SetActive(true);
                controller.Tick(new BasicLocomotionInputSnapshot(0.02f, Vector2.up, Vector2.zero));

                Assert.True(controller.UsesDefaultStateGraph);
                Assert.AreEqual(BasicMovementPhase.MoveStart, controller.CurrentPhase);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ControllerAutoUpdateCanBeDisabled()
        {
            GameObject gameObject = new GameObject("locomotion-auto-update-test");
            gameObject.SetActive(false);

            try
            {
                PlayerLocomotionController controller = gameObject.AddComponent<PlayerLocomotionController>();
                FakeInputSource inputSource = new FakeInputSource(new BasicLocomotionInputSnapshot(0.02f, Vector2.up, Vector2.zero));
                FakeMotionExecutor motionExecutor = new FakeMotionExecutor();

                controller.SetInputSource(inputSource);
                controller.SetMotionExecutor(motionExecutor);
                controller.AutoUpdate = false;
                DisableCameraDebugLog(controller);

                gameObject.SetActive(true);
                InvokeUpdate(controller);

                Assert.AreEqual(0, inputSource.ReadCount);
                Assert.AreEqual(0, motionExecutor.ExecuteCount);
                Assert.False(controller.AutoUpdate);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TickFromInputSourceUsesProvidedDelta()
        {
            GameObject gameObject = new GameObject("locomotion-fixed-delta-test");
            gameObject.SetActive(false);

            try
            {
                PlayerLocomotionController controller = gameObject.AddComponent<PlayerLocomotionController>();
                FakeInputSource inputSource = new FakeInputSource(new BasicLocomotionInputSnapshot(0.02f, Vector2.up, Vector2.zero));
                FakeMotionExecutor motionExecutor = new FakeMotionExecutor();

                controller.SetInputSource(inputSource);
                controller.SetMotionExecutor(motionExecutor);
                DisableCameraDebugLog(controller);

                gameObject.SetActive(true);
                Assert.True(controller.TickFromInputSource(1f / 30f));

                Assert.AreEqual(1, inputSource.ReadCount);
                Assert.AreEqual(1f / 30f, inputSource.LastDeltaTime, 0.000001f);
                Assert.AreEqual(1, motionExecutor.ExecuteCount);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void LocomotionTickAdapterRegistersToExecuteMotionAndDisablesAutoUpdate()
        {
            GameObject gameObject = new GameObject("locomotion-adapter-test");
            gameObject.SetActive(false);

            try
            {
                UnitySimulationTickDriver driver = gameObject.AddComponent<UnitySimulationTickDriver>();
                PlayerLocomotionController controller = gameObject.AddComponent<PlayerLocomotionController>();
                LocomotionTickAdapter adapter = gameObject.AddComponent<LocomotionTickAdapter>();
                FakeInputSource inputSource = new FakeInputSource(new BasicLocomotionInputSnapshot(0.02f, Vector2.up, Vector2.zero));
                FakeMotionExecutor motionExecutor = new FakeMotionExecutor();

                controller.SetInputSource(inputSource);
                controller.SetMotionExecutor(motionExecutor);
                DisableCameraDebugLog(controller);
                adapter.TickDriver = driver;
                adapter.LocomotionController = controller;

                gameObject.SetActive(true);
                Assert.True(adapter.Register());

                Assert.True(adapter.IsRegistered);
                Assert.False(controller.AutoUpdate);

                driver.Runner.Run(new SimulationTickContext(new SimulationTick(2), new SimulationTickRate(30), SimulationTickRole.Client));

                Assert.AreEqual(1, inputSource.ReadCount);
                Assert.AreEqual(1f / 30f, inputSource.LastDeltaTime, 0.000001f);
                Assert.AreEqual(1, motionExecutor.ExecuteCount);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void LocomotionTickAdapterUnregistersOnDisable()
        {
            GameObject gameObject = new GameObject("locomotion-adapter-unregister-test");
            gameObject.SetActive(false);

            try
            {
                UnitySimulationTickDriver driver = gameObject.AddComponent<UnitySimulationTickDriver>();
                PlayerLocomotionController controller = gameObject.AddComponent<PlayerLocomotionController>();
                LocomotionTickAdapter adapter = gameObject.AddComponent<LocomotionTickAdapter>();
                FakeInputSource inputSource = new FakeInputSource(new BasicLocomotionInputSnapshot(0.02f, Vector2.up, Vector2.zero));
                FakeMotionExecutor motionExecutor = new FakeMotionExecutor();

                controller.SetInputSource(inputSource);
                controller.SetMotionExecutor(motionExecutor);
                DisableCameraDebugLog(controller);
                adapter.TickDriver = driver;
                adapter.LocomotionController = controller;

                gameObject.SetActive(true);
                Assert.True(adapter.Register());
                adapter.Unregister();
                driver.Runner.Run(new SimulationTickContext(new SimulationTick(3), SimulationTickRate.Default, SimulationTickRole.Client));

                Assert.False(adapter.IsRegistered);
                Assert.AreEqual(0, inputSource.ReadCount);
                Assert.AreEqual(0, motionExecutor.ExecuteCount);
                Assert.True(controller.AutoUpdate);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void LocomotionTickAdapterAvoidsFrameUpdateDoubleDrive()
        {
            GameObject gameObject = new GameObject("locomotion-no-double-drive-test");
            gameObject.SetActive(false);

            try
            {
                UnitySimulationTickDriver driver = gameObject.AddComponent<UnitySimulationTickDriver>();
                PlayerLocomotionController controller = gameObject.AddComponent<PlayerLocomotionController>();
                LocomotionTickAdapter adapter = gameObject.AddComponent<LocomotionTickAdapter>();
                FakeInputSource inputSource = new FakeInputSource(new BasicLocomotionInputSnapshot(0.02f, Vector2.up, Vector2.zero));
                FakeMotionExecutor motionExecutor = new FakeMotionExecutor();

                controller.SetInputSource(inputSource);
                controller.SetMotionExecutor(motionExecutor);
                DisableCameraDebugLog(controller);
                adapter.TickDriver = driver;
                adapter.LocomotionController = controller;

                gameObject.SetActive(true);
                Assert.True(adapter.Register());
                InvokeUpdate(controller);
                driver.Runner.Run(new SimulationTickContext(new SimulationTick(4), SimulationTickRate.Default, SimulationTickRole.Client));

                Assert.AreEqual(1, inputSource.ReadCount);
                Assert.AreEqual(1, motionExecutor.ExecuteCount);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void UnitySimulationTickDriverCanDriveLocomotionForEachEmittedTick()
        {
            GameObject gameObject = new GameObject("locomotion-driver-multi-tick-test");
            gameObject.SetActive(false);

            try
            {
                UnitySimulationTickDriver driver = gameObject.AddComponent<UnitySimulationTickDriver>();
                PlayerLocomotionController controller = gameObject.AddComponent<PlayerLocomotionController>();
                LocomotionTickAdapter adapter = gameObject.AddComponent<LocomotionTickAdapter>();
                FakeInputSource inputSource = new FakeInputSource(new BasicLocomotionInputSnapshot(0.02f, Vector2.up, Vector2.zero));
                FakeMotionExecutor motionExecutor = new FakeMotionExecutor();

                controller.SetInputSource(inputSource);
                controller.SetMotionExecutor(motionExecutor);
                DisableCameraDebugLog(controller);
                adapter.TickDriver = driver;
                adapter.LocomotionController = controller;

                gameObject.SetActive(true);
                Assert.True(adapter.Register());
                int emitted = driver.Advance(3f / 60f);

                Assert.AreEqual(3, emitted);
                Assert.AreEqual(3, inputSource.ReadCount);
                Assert.AreEqual(3, motionExecutor.ExecuteCount);
                Assert.AreEqual(1f / 60f, inputSource.LastDeltaTime, 0.000001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SandboxLocomotionAdapterReferencesSceneTickDriver()
        {
            string scenePath = Path.Combine(Application.dataPath, "Scenes", "Sandbox.unity");
            string sceneYaml = File.ReadAllText(scenePath);

            string driverId = FindMonoBehaviourIdByScriptGuid(sceneYaml, "2e94c947e6094f1f8a6e0a68f3edb197");
            string adapterBody = FindMonoBehaviourBodyByScriptGuid(sceneYaml, "0cfcb781fc314bfda705d6f8277c97c5");
            string controllerBody = FindMonoBehaviourBodyByScriptGuid(sceneYaml, "d9147fe7289b8de4091089d2785daa85");

            Assert.NotNull(driverId);
            Assert.NotNull(adapterBody);
            Assert.NotNull(controllerBody);
            Assert.AreEqual(driverId, FindFileId(adapterBody, "tickDriver"));
            Assert.AreNotEqual("0", FindFileId(adapterBody, "tickDriver"));
            StringAssert.Contains("autoUpdate: 0", controllerBody);
            StringAssert.Contains("runAutomatically: 1", FindMonoBehaviourBodyByScriptGuid(sceneYaml, "2e94c947e6094f1f8a6e0a68f3edb197"));
        }

        [Test]
        public void UnityInputSystemLocomotionInputSourceResolvesActionsFromInputAsset()
        {
            InputActionAsset inputActions = ScriptableObject.CreateInstance<InputActionAsset>();
            GameObject gameObject = new GameObject("input-source-test");

            try
            {
                InputActionMap playerMap = inputActions.AddActionMap("Player");
                InputAction move = playerMap.AddAction("Move", InputActionType.Value, expectedControlType: "Vector2");
                InputAction look = playerMap.AddAction("Look", InputActionType.Value, expectedControlType: "Vector2");

                UnityInputSystemLocomotionInputSource inputSource = gameObject.AddComponent<UnityInputSystemLocomotionInputSource>();
                inputSource.InputActions = inputActions;
                inputSource.ActionMapName = "Player";
                inputSource.MoveActionName = "Move";
                inputSource.LookActionName = "Look";

                inputSource.SetInputEnabled(true);

                Assert.AreSame(move, inputSource.MoveAction);
                Assert.AreSame(look, inputSource.LookAction);
                Assert.True(move.enabled);
                Assert.True(look.enabled);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(inputActions);
            }
        }

        [Test]
        public void SandboxLocomotionInputSourceUsesProjectInputAsset()
        {
            string scenePath = Path.Combine(Application.dataPath, "Scenes", "Sandbox.unity");
            string sceneYaml = File.ReadAllText(scenePath);
            string inputSourceBody = FindMonoBehaviourBodyByScriptGuid(sceneYaml, "64115ba33e5c5e54f8911c4e9e0fa9b9");

            Assert.NotNull(inputSourceBody);
            StringAssert.Contains("inputActions: {fileID: -944628639613478452, guid: ea97b05a2d14465449e741dc51ff44fe", inputSourceBody);
            StringAssert.Contains("actionMapName: Player", inputSourceBody);
            StringAssert.Contains("moveActionName: Move", inputSourceBody);
            StringAssert.Contains("lookActionName: Look", inputSourceBody);
            Assert.False(inputSourceBody.Contains("moveAction:"), inputSourceBody);
            Assert.False(inputSourceBody.Contains("lookAction:"), inputSourceBody);
        }

        [Test]
        public void ControllerDoesNotReferenceInputSystemTypes()
        {
            string source = typeof(PlayerLocomotionController).FullName;
            FieldInfo[] fields = typeof(PlayerLocomotionController).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < fields.Length; i++)
            {
                string typeName = fields[i].FieldType.FullName;
                Assert.False(typeName.Contains("InputActionReference"), $"{source}.{fields[i].Name} references InputActionReference");
                Assert.False(typeName.StartsWith("UnityEngine.InputSystem"), $"{source}.{fields[i].Name} references Unity Input System");
            }
        }

        [Test]
        public void StateGraphCoreDoesNotReferencePresentationInputMotionOrTickTypes()
        {
            AssertTypeFieldsDoNotReference(
                typeof(BasicLocomotionStateMachine),
                "Animancer",
                "CharacterController",
                "KinematicCharacterMotor",
                "Camera",
                "Cinemachine",
                "InputAction",
                "SimulationTickRunner",
                "UnitySimulationTickDriver",
                "SimulationTickAccumulator");

            AssertTypeFieldsDoNotReference(
                typeof(LocomotionStateGraphBuilder),
                "Animancer",
                "CharacterController",
                "KinematicCharacterMotor",
                "Camera",
                "Cinemachine",
                "InputAction",
                "SimulationTickRunner",
                "UnitySimulationTickDriver",
                "SimulationTickAccumulator");
        }

        [Test]
        public void PresenterDoesNotReferenceStateGraphBuilderOrMotionExecutor()
        {
            AssertTypeFieldsDoNotReference(
                typeof(BasicLocomotionAnimancerPresenter),
                "LocomotionStateGraphBuilder",
                "IBasicLocomotionMotionExecutor",
                "CharacterMotionDriver",
                "CharacterControllerBasicMotionExecutor");
        }

        [Test]
        public void TickDriverDoesNotReferenceLocomotionTypes()
        {
            FieldInfo[] fields = typeof(UnitySimulationTickDriver).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < fields.Length; i++)
            {
                string typeName = fields[i].FieldType.FullName;
                Assert.False(typeName.Contains("ThirdPersonMovement"), $"UnitySimulationTickDriver.{fields[i].Name} references Locomotion");
                Assert.False(typeName.Contains("PlayerLocomotionController"), $"UnitySimulationTickDriver.{fields[i].Name} references PlayerLocomotionController");
            }
        }

        [Test]
        public void PipelineDoesNotHoldConcreteMotionDriver()
        {
            FieldInfo[] fields = typeof(BasicLocomotionPipeline).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < fields.Length; i++)
                Assert.AreNotEqual(typeof(CharacterMotionDriver), fields[i].FieldType);
        }

        sealed class TestCameraBasis : ICameraMovementBasisProvider
        {
            public TestCameraBasis(Vector3 forward, Vector3 right)
            {
                CameraPlanarForward = forward;
                CameraPlanarRight = right;
            }

            public Vector3 CameraPlanarForward { get; }
            public Vector3 CameraPlanarRight { get; }
        }

        sealed class FakeInputSource : IBasicLocomotionInputSource
        {
            readonly BasicLocomotionInputSnapshot snapshot;

            public FakeInputSource(BasicLocomotionInputSnapshot snapshot)
            {
                this.snapshot = snapshot;
            }

            public int ReadCount { get; private set; }
            public bool Enabled { get; private set; }
            public float LastDeltaTime { get; private set; }

            public BasicLocomotionInputSnapshot ReadInput(float deltaTime)
            {
                ReadCount++;
                LastDeltaTime = deltaTime;
                return snapshot;
            }

            public void SetInputEnabled(bool enabled)
            {
                Enabled = enabled;
            }
        }

        sealed class FakeMotionExecutor : IBasicLocomotionMotionExecutor
        {
            public float CurrentSpeed => LastCommand.PlanarSpeed;
            public Vector3 LastWorldDirection => LastCommand.WorldDirection;
            public int ExecuteCount { get; private set; }
            public MovementCommand LastCommand { get; private set; }

            public void ExecuteBasicMovement(in MovementCommand command)
            {
                ExecuteCount++;
                LastCommand = command;
            }
        }

        static void DisableCameraDebugLog(PlayerLocomotionController controller)
        {
            typeof(PlayerLocomotionController)
                .GetField("debugCameraLog", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, false);
        }

        static void InvokeUpdate(PlayerLocomotionController controller)
        {
            typeof(PlayerLocomotionController)
                .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
        }

        static void SetAnimationEntry(LocomotionAnimationSetSO animationSet, string fieldName, LocomotionAnimationEntry entry)
        {
            typeof(LocomotionAnimationSetSO)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(animationSet, entry);
        }

        static string FindMonoBehaviourIdByScriptGuid(string yaml, string scriptGuid)
        {
            MatchCollection matches = Regex.Matches(yaml, @"--- !u!114 &(?<id>\d+)\r?\nMonoBehaviour:(?<body>.*?)(?=\r?\n--- !u!|\z)", RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                if (match.Groups["body"].Value.Contains($"guid: {scriptGuid}"))
                    return match.Groups["id"].Value;
            }

            return null;
        }

        static string FindMonoBehaviourBodyByScriptGuid(string yaml, string scriptGuid)
        {
            MatchCollection matches = Regex.Matches(yaml, @"--- !u!114 &(?<id>\d+)\r?\nMonoBehaviour:(?<body>.*?)(?=\r?\n--- !u!|\z)", RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                if (match.Groups["body"].Value.Contains($"guid: {scriptGuid}"))
                    return match.Groups["body"].Value;
            }

            return null;
        }

        static string FindFileId(string yamlBlock, string fieldName)
        {
            Match match = Regex.Match(yamlBlock, $@"{fieldName}: \{{fileID: (?<id>-?\d+)\}}");
            return match.Success ? match.Groups["id"].Value : null;
        }

        static void AssertTypeFieldsDoNotReference(System.Type type, params string[] forbiddenNames)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < fields.Length; i++)
            {
                string typeName = fields[i].FieldType.FullName ?? fields[i].FieldType.Name;
                for (int j = 0; j < forbiddenNames.Length; j++)
                    Assert.False(typeName.Contains(forbiddenNames[j]), $"{type.FullName}.{fields[i].Name} references {forbiddenNames[j]}");
            }
        }
    }
}
