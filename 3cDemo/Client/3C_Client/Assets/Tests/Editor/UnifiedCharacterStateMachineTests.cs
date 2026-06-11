using System.IO;
using System.Linq;
using Animancer;
using Animancer.TransitionLibraries;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonAnimation;
using ThirdPersonCamera;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class UnifiedCharacterStateMachineTests
    {
        [Test]
        public void DefaultStateIsFullBodyLocomotionIdle()
        {
            CharacterStateMachineRunner runner = CreateRunner();

            Assert.AreEqual(CharacterStateIds.Idle, runner.Snapshot.ActiveState);
            Assert.AreEqual("FullBody/Locomotion/Idle", runner.Snapshot.ActivePath);
            Assert.AreEqual(BasicMovementPhase.Idle, runner.Snapshot.LocomotionPhase);
        }

        [Test]
        public void DefaultTreeContainsLocomotionAndDodgeStates()
        {
            CharacterStateMachineDefinition definition = CharacterStateMachineDefinition.CreateDefault();
            string[] ids = definition.Nodes.Select(node => node.StateId.Value).ToArray();

            CollectionAssert.Contains(ids, "FullBody/Locomotion/Idle");
            CollectionAssert.Contains(ids, "FullBody/Locomotion/MoveStart");
            CollectionAssert.Contains(ids, "FullBody/Locomotion/MoveLoop");
            CollectionAssert.Contains(ids, "FullBody/Locomotion/MoveStop");
            CollectionAssert.Contains(ids, "FullBody/Action/Dodge");
        }

        [Test]
        public void MovementInputEntersMoveStart()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            CharacterStateMachineFrame frame = runner.Tick(Context(move: true));

            Assert.AreEqual(CharacterStateIds.MoveStart, frame.Snapshot.ActiveState);
            Assert.True(frame.ExecuteBasicMovement);
        }

        [Test]
        public void MoveStartCanExitToMoveLoop()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true));

            CharacterStateMachineFrame frame = runner.Tick(Context(move: true, canExit: true));

            Assert.AreEqual(CharacterStateIds.MoveLoop, frame.Snapshot.ActiveState);
            Assert.AreEqual(BasicMovementPhase.MoveLoop, frame.LocomotionPhase);
        }

        [Test]
        public void NoMoveInputEntersMoveStopAndThenIdle()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true));
            runner.Tick(Context(move: true, canExit: true));

            CharacterStateMachineFrame stop = runner.Tick(Context(move: false));
            CharacterStateMachineFrame idle = runner.Tick(Context(move: false, canExit: true));

            Assert.AreEqual(CharacterStateIds.MoveStop, stop.Snapshot.ActiveState);
            Assert.AreEqual(CharacterStateIds.Idle, idle.Snapshot.ActiveState);
            Assert.True(idle.ResetRunLatch);
        }

        [Test]
        public void MoveStopCanReEnterMoveStart()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true));
            runner.Tick(Context(move: true, canExit: true));
            runner.Tick(Context(move: false));

            CharacterStateMachineFrame frame = runner.Tick(Context(move: true));

            Assert.AreEqual(CharacterStateIds.MoveStart, frame.Snapshot.ActiveState);
        }

        [Test]
        public void MoveInputAndDodgeRequestEnterDirectionalDodge()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: true,
                request: DodgeRequest(CharacterStateVariant.Directional, Vector3.forward)));

            Assert.AreEqual(CharacterStateIds.Dodge, frame.Snapshot.ActiveState);
            Assert.AreEqual(CharacterStateVariant.Directional, frame.Snapshot.Variant);
            Assert.True(frame.ConsumeInputRequest);
            Assert.True(frame.HasActionMovement);
            Assert.False(frame.ExecuteBasicMovement);
            Assert.False(frame.PresentLocomotionAnimation);
            Assert.AreEqual(4f * 0.1f / 0.35f, frame.ActionMovementCommand.PlanarDistance, 0.0001f);
        }

        [Test]
        public void NoMoveInputAndDodgeRequestEnterBackstepDodge()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: false,
                request: DodgeRequest(CharacterStateVariant.Backstep, Vector3.back)));

            Assert.AreEqual(CharacterStateIds.Dodge, frame.Snapshot.ActiveState);
            Assert.AreEqual(CharacterStateVariant.Backstep, frame.Snapshot.Variant);
            Assert.True(frame.HasActionMovement);
            Assert.AreEqual(3f * 0.1f / 0.35f, frame.ActionMovementCommand.PlanarDistance, 0.0001f);
        }

        [Test]
        public void DirectionalDodgeCompletionWritesRunLatch()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true, deltaTime: 0.1f, request: DodgeRequest(CharacterStateVariant.Directional, Vector3.forward)));

            CharacterStateMachineFrame frame = runner.Tick(Context(move: true, deltaTime: 0.25f));

            Assert.AreEqual(CharacterStateIds.MoveLoop, frame.Snapshot.ActiveState);
            Assert.True(frame.SetRunLatch);
        }

        [Test]
        public void DirectionalDodgeRunLoopReleaseKeepsRunGaitForMoveStop()
        {
            GameObject gameObject = new GameObject("locomotion-run-end-test");

            try
            {
                PlayerLocomotionController controller = gameObject.AddComponent<PlayerLocomotionController>();
                CharacterStateMachineRunner runner = CreateRunner();
                BasicLocomotionFrame locomotionFrame;
                CharacterStateMachineFrame stateFrame;

                Assert.True(controller.TryEvaluateWithStateMachine(
                    new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero),
                    runner,
                    DodgeRequest(CharacterStateVariant.Directional, Vector3.forward),
                    1,
                    out locomotionFrame,
                    out stateFrame));
                Assert.AreEqual(CharacterStateIds.Dodge, stateFrame.Snapshot.ActiveState);

                Assert.True(controller.TryEvaluateWithStateMachine(
                    new BasicLocomotionInputSnapshot(0.25f, Vector2.up, Vector2.zero),
                    runner,
                    CharacterInputRequestFact.None(InputRequestKind.Dodge),
                    2,
                    out locomotionFrame,
                    out stateFrame));
                Assert.AreEqual(CharacterStateIds.MoveLoop, stateFrame.Snapshot.ActiveState);
                Assert.True(stateFrame.SetRunLatch);

                Assert.True(controller.TryEvaluateWithStateMachine(
                    new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero),
                    runner,
                    CharacterInputRequestFact.None(InputRequestKind.Dodge),
                    3,
                    out locomotionFrame,
                    out stateFrame));
                Assert.AreEqual(CharacterStateIds.MoveLoop, stateFrame.Snapshot.ActiveState);
                Assert.AreEqual(BasicMovementGait.Run, locomotionFrame.Command.Gait);

                Assert.True(controller.TryEvaluateWithStateMachine(
                    new BasicLocomotionInputSnapshot(0.1f, Vector2.zero, Vector2.zero),
                    runner,
                    CharacterInputRequestFact.None(InputRequestKind.Dodge),
                    4,
                    out locomotionFrame,
                    out stateFrame));
                Assert.AreEqual(CharacterStateIds.MoveStop, stateFrame.Snapshot.ActiveState);
                Assert.AreEqual(BasicMovementGait.Run, locomotionFrame.Command.Gait);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void BackstepDodgeCompletionDoesNotWriteRunLatch()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: false, deltaTime: 0.1f, request: DodgeRequest(CharacterStateVariant.Backstep, Vector3.back)));

            CharacterStateMachineFrame frame = runner.Tick(Context(move: false, deltaTime: 0.25f));

            Assert.AreEqual(CharacterStateIds.Idle, frame.Snapshot.ActiveState);
            Assert.False(frame.SetRunLatch);
        }

        [Test]
        public void DodgeExitsToMoveLoopOrIdleAfterDuration()
        {
            CharacterStateMachineRunner directional = CreateRunner();
            directional.Tick(Context(move: true, deltaTime: 0.35f, request: DodgeRequest(CharacterStateVariant.Directional, Vector3.forward)));
            CharacterStateMachineFrame moveLoop = directional.Tick(Context(move: true));

            CharacterStateMachineRunner backstep = CreateRunner();
            backstep.Tick(Context(move: false, deltaTime: 0.35f, request: DodgeRequest(CharacterStateVariant.Backstep, Vector3.back)));
            CharacterStateMachineFrame idle = backstep.Tick(Context(move: false));

            Assert.AreEqual(CharacterStateIds.MoveLoop, moveLoop.Snapshot.ActiveState);
            Assert.AreEqual(CharacterStateIds.Idle, idle.Snapshot.ActiveState);
        }

        [Test]
        public void DodgeOutputsActionAnimationFromStateVariant()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            CharacterStateMachineFrame directional = runner.Tick(Context(
                move: true,
                request: DodgeRequest(CharacterStateVariant.Directional, Vector3.forward)));

            Assert.True(directional.HasAnimationRequest);
            Assert.AreEqual(ActionAnimationKeys.DodgeDirectional, directional.AnimationRequest.Key);
            Assert.AreEqual("Action.Dodge.Directional", directional.AnimationRequest.Binding.TransitionLibraryKey);
        }

        [Test]
        public void FullBodyActionControllerClearsActionAnimationWhenReturningToLocomotion()
        {
            GameObject gameObject = new GameObject("fullbody-action-clear-test");

            try
            {
                PlayerLocomotionController locomotion = gameObject.AddComponent<PlayerLocomotionController>();
                PlayerFullBodyActionController fullBody = gameObject.AddComponent<PlayerFullBodyActionController>();
                InputRequestBufferComponent inputBuffer = gameObject.AddComponent<InputRequestBufferComponent>();
                TestFacingDirectionProviderComponent facing = gameObject.AddComponent<TestFacingDirectionProviderComponent>();
                TestActionAnimationPresenter actionPresenter = gameObject.AddComponent<TestActionAnimationPresenter>();

                locomotion.AutoUpdate = false;
                fullBody.AutoUpdate = false;
                fullBody.LocomotionController = locomotion;
                fullBody.InputBufferComponent = inputBuffer;
                fullBody.FacingProviderBehaviour = facing;
                fullBody.AnimationPresenterBehaviour = actionPresenter;

                inputBuffer.SetStep(1);
                inputBuffer.Buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 1, 4);

                Assert.True(fullBody.Tick(new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero)));
                Assert.AreEqual(CharacterStateIds.Dodge, fullBody.CurrentStateSnapshot.ActiveState);
                Assert.AreEqual(1, actionPresenter.PresentCount);
                Assert.AreEqual(0, actionPresenter.ClearCount);

                Assert.True(fullBody.Tick(new BasicLocomotionInputSnapshot(0.25f, Vector2.up, Vector2.zero)));
                Assert.AreEqual(CharacterStateIds.MoveLoop, fullBody.CurrentStateSnapshot.ActiveState);
                Assert.AreEqual(1, actionPresenter.ClearCount);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void BackstepDodgeReturnToIdleReplaysLocomotionAfterActionInterrupt()
        {
            CreateAnimationPresenterRig(
                out GameObject gameObject,
                out BasicLocomotionAnimancerPresenter locomotionPresenter,
                out ActionAnimationAnimancerPresenter actionPresenter,
                out AnimancerComponent animancer,
                out AnimationClip idleClip,
                out AnimationClip runClip,
                out AnimationClip dodgeClip);

            try
            {
                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.Idle, BasicMovementGait.Walk, false, 0f, Vector3.zero, 0f));
                Assert.AreSame(idleClip, animancer.Graph.Layers[0].CurrentState.MainObject);

                actionPresenter.Present(new CharacterStateAnimationRequest(CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeBackstep.Value, "Dodge Backstep"), 1));
                Assert.AreSame(dodgeClip, animancer.Graph.Layers[0].CurrentState.MainObject);

                actionPresenter.Clear();
                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.Idle, BasicMovementGait.Walk, false, 0f, Vector3.zero, 0f));

                Assert.AreSame(idleClip, animancer.Graph.Layers[0].CurrentState.MainObject);
                Assert.False(actionPresenter.HasValidPlayback);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(runClip);
                Object.DestroyImmediate(dodgeClip);
            }
        }

        [Test]
        public void DirectionalDodgeReturnToRunLoopReplaysLocomotionAfterActionInterrupt()
        {
            CreateAnimationPresenterRig(
                out GameObject gameObject,
                out BasicLocomotionAnimancerPresenter locomotionPresenter,
                out ActionAnimationAnimancerPresenter actionPresenter,
                out AnimancerComponent animancer,
                out AnimationClip idleClip,
                out AnimationClip runClip,
                out AnimationClip dodgeClip);

            try
            {
                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.MoveLoop, BasicMovementGait.Run, true, 1f, Vector3.forward, 5f));
                Assert.AreSame(runClip, animancer.Graph.Layers[0].CurrentState.MainObject);

                actionPresenter.Present(new CharacterStateAnimationRequest(CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeDirectional.Value, "Dodge Directional"), 1));
                Assert.AreSame(dodgeClip, animancer.Graph.Layers[0].CurrentState.MainObject);

                actionPresenter.Clear();
                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.MoveLoop, BasicMovementGait.Run, true, 1f, Vector3.forward, 5f));

                Assert.AreSame(runClip, animancer.Graph.Layers[0].CurrentState.MainObject);
                Assert.False(actionPresenter.HasValidPlayback);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(runClip);
                Object.DestroyImmediate(dodgeClip);
            }
        }

        [Test]
        public void MissingActionMovementVariantAnimationFailsValidation()
        {
            CharacterStateNodeDefinition dodge = new CharacterStateNodeDefinition(
                CharacterStateIds.Dodge,
                CharacterStateIds.Action,
                "Dodge",
                new[] { CharacterStateTag.FullBody, CharacterStateTag.Action, CharacterStateTag.Dodge },
                CharacterStateOutputDefinition.ActionMovement(
                    InputRequestKind.Dodge,
                    new CharacterActionMovementDefinition(CharacterStateVariant.Directional, 0.35f, 4f, true, true),
                    new CharacterActionMovementDefinition(CharacterStateVariant.Backstep, 0.35f, 3f, false, false)),
                default,
                new[]
                {
                    new CharacterStateVariantDefinition(CharacterStateVariant.Directional, default),
                    new CharacterStateVariantDefinition(CharacterStateVariant.Backstep, CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeBackstep.Value, "Backstep"))
                });
            CharacterStateMachineDefinition definition = new CharacterStateMachineDefinition(
                CharacterStateIds.Idle,
                CharacterStateMachineDefinition.CreateDefault().Nodes.Where(node => node.StateId != CharacterStateIds.Dodge).Concat(new[] { dodge }).ToArray(),
                CharacterStateMachineDefinition.CreateDefault().Transitions.ToArray());

            CharacterStateMachineValidationResult validation = definition.Validate();

            Assert.True(validation.HasErrors);
            Assert.That(validation.DescribeErrors(), Does.Contain("Directional"));
        }

        [Test]
        public void GenericActionMovementNodeCanPassValidationWithoutDodgeStateId()
        {
            CharacterStateNodeDefinition roll = new CharacterStateNodeDefinition(
                new CharacterStateId("FullBody/Action/Roll"),
                CharacterStateIds.Action,
                "Roll",
                new[] { CharacterStateTag.FullBody, CharacterStateTag.Action },
                CharacterStateOutputDefinition.ActionMovement(
                    InputRequestKind.Dodge,
                    new CharacterActionMovementDefinition(CharacterStateVariant.None, 0.25f, 2f, true, false)),
                CharacterStateAnimationBinding.FromLibraryKey("Action.Roll", "Roll"));
            CharacterStateMachineDefinition defaults = CharacterStateMachineDefinition.CreateDefault();
            CharacterStateMachineDefinition definition = new CharacterStateMachineDefinition(
                CharacterStateIds.Idle,
                defaults.Nodes.Concat(new[] { roll }).ToArray(),
                defaults.Transitions.ToArray());

            CharacterStateMachineValidationResult validation = definition.Validate();

            Assert.False(validation.HasErrors, validation.DescribeErrors());
        }

        [Test]
        public void FullBodyActionInputRequestBuilderBuildsDirectionalDodgeFact()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 2, 4);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero);

            CharacterInputRequestFact fact = FullBodyActionInputRequestBuilder.BuildDodgeRequestFact(
                buffer,
                2,
                in input,
                in settings,
                false,
                new TestCameraBasisProvider(Vector3.forward, Vector3.right),
                new TestFacingDirectionProvider(Vector3.forward),
                DodgeActionConfig.Default);

            Assert.True(fact.HasRequest);
            Assert.AreEqual(CharacterStateVariant.Directional, fact.Variant);
            Assert.AreEqual(DodgeActionConfig.Default.Priority, fact.Priority);
            Assert.AreEqual(Vector3.forward, fact.WorldDirection);
        }

        [Test]
        public void FullBodyActionInputRequestBuilderBuildsBackstepDodgeFact()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 3, 4);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.zero, Vector2.zero);

            CharacterInputRequestFact fact = FullBodyActionInputRequestBuilder.BuildDodgeRequestFact(
                buffer,
                3,
                in input,
                in settings,
                false,
                new TestCameraBasisProvider(Vector3.forward, Vector3.right),
                new TestFacingDirectionProvider(Vector3.forward),
                DodgeActionConfig.Default);

            Assert.True(fact.HasRequest);
            Assert.AreEqual(CharacterStateVariant.Backstep, fact.Variant);
            Assert.AreEqual(Vector3.back, fact.WorldDirection);
        }

        [Test]
        public void RunnerAndEvaluatorDoNotReferenceForbiddenRuntimeObjects()
        {
            string root = Path.Combine(Application.dataPath, "Scripts/Character/StateMachine/Solver");
            string combined = string.Join("\n", new[]
            {
                Path.Combine(root, "CharacterStateMachineRunner.cs"),
                Path.Combine(root, "CharacterStateTransitionEvaluator.cs")
            }.Select(File.ReadAllText));

            Assert.That(combined, Does.Not.Contain("Animancer"));
            Assert.That(combined, Does.Not.Contain("CharacterController"));
            Assert.That(combined, Does.Not.Contain("UnityEngine.InputSystem"));
            Assert.That(combined, Does.Not.Contain("Cinemachine"));
            Assert.That(combined, Does.Not.Contain("DodgeActionConfig"));
            Assert.That(combined, Does.Not.Contain("CharacterStateIds.Dodge"));
        }

        [Test]
        public void RuntimeCodeNoLongerReferencesOldSplitPathTypes()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string combined = string.Join("\n", Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

            Assert.That(combined, Does.Not.Contain("BasicLocomotionStateMachine"));
            Assert.That(combined, Does.Not.Contain("LocomotionStateGraphConfigSO"));
            Assert.That(combined, Does.Not.Contain("FullBodyHfsmStateTreeBuilder"));
            Assert.That(combined, Does.Not.Contain("FullBodyHfsmStateTreeDriver"));
            Assert.That(combined, Does.Not.Contain("DodgeActionRuntime"));
            Assert.That(combined, Does.Not.Contain("DodgeFullBodyActionModule"));
            Assert.That(combined, Does.Not.Contain("FullBodyActionSetSO"));
            Assert.That(combined, Does.Not.Contain("FullBodyActionAnimationSetSO"));
            Assert.That(combined, Does.Not.Contain("ActionAnimationProfileSO"));
        }

        static CharacterStateMachineRunner CreateRunner()
        {
            return new CharacterStateMachineRunner(CharacterStateMachineDefinition.CreateDefault());
        }

        static CharacterStateMachineContext Context(
            bool move,
            float deltaTime = 0.1f,
            bool canExit = false,
            CharacterInputRequestFact request = default)
        {
            Vector2 moveInput = move ? Vector2.up : Vector2.zero;
            MovementInputIntent intent = MovementInputIntent.FromRaw(moveInput, 0.1f, false);
            Vector3 worldDirection = move ? Vector3.forward : Vector3.zero;
            CharacterInputRequestFact resolvedRequest = request.HasRequest ? request : CharacterInputRequestFact.None(InputRequestKind.Dodge);
            return new CharacterStateMachineContext(
                deltaTime,
                1,
                intent,
                worldDirection,
                new BasicMovementPhaseFacts(canExit),
                resolvedRequest);
        }

        static CharacterInputRequestFact DodgeRequest(CharacterStateVariant variant, Vector3 direction)
        {
            return new CharacterInputRequestFact(
                true,
                InputRequestKind.Dodge,
                1,
                4,
                DodgeActionConfig.Default.Priority,
                variant,
                direction);
        }

        static void CreateAnimationPresenterRig(
            out GameObject gameObject,
            out BasicLocomotionAnimancerPresenter locomotionPresenter,
            out ActionAnimationAnimancerPresenter actionPresenter,
            out AnimancerComponent animancer,
            out AnimationClip idleClip,
            out AnimationClip runClip,
            out AnimationClip dodgeClip)
        {
            gameObject = new GameObject("animation-presenter-rig");
            gameObject.AddComponent<Animator>();
            animancer = gameObject.AddComponent<AnimancerComponent>();
            locomotionPresenter = gameObject.AddComponent<BasicLocomotionAnimancerPresenter>();
            actionPresenter = gameObject.AddComponent<ActionAnimationAnimancerPresenter>();
            idleClip = CreateClip("Idle");
            runClip = CreateClip("RunLoop");
            dodgeClip = CreateClip("Action.Dodge");

            TransitionLibrary library = new TransitionLibrary();
            library.AddTransition(StringReference.Get("Idle"), CreateClipTransition(idleClip));
            library.AddTransition(StringReference.Get("RunLoop"), CreateClipTransition(runClip));
            library.AddTransition(StringReference.Get(ActionAnimationKeys.DodgeBackstep.Value), CreateClipTransition(dodgeClip));
            library.AddTransition(StringReference.Get(ActionAnimationKeys.DodgeDirectional.Value), CreateClipTransition(dodgeClip));
            animancer.Graph.Transitions = library;
        }

        static AnimationClip CreateClip(string name)
        {
            AnimationClip clip = new AnimationClip { name = name };
            clip.legacy = false;
            return clip;
        }

        static ClipTransition CreateClipTransition(AnimationClip clip)
        {
            return new ClipTransition
            {
                Clip = clip,
                FadeDuration = 0.08f
            };
        }

        sealed class TestCameraBasisProvider : ICameraMovementBasisProvider
        {
            public TestCameraBasisProvider(Vector3 forward, Vector3 right)
            {
                CameraPlanarForward = forward;
                CameraPlanarRight = right;
            }

            public Vector3 CameraPlanarForward { get; }
            public Vector3 CameraPlanarRight { get; }
        }

        sealed class TestFacingDirectionProvider : IFacingDirectionProvider
        {
            public TestFacingDirectionProvider(Vector3 facingForward)
            {
                FacingForward = facingForward;
            }

            public Vector3 FacingForward { get; }
        }

        sealed class TestFacingDirectionProviderComponent : MonoBehaviour, IFacingDirectionProvider
        {
            public Vector3 FacingForward => Vector3.forward;
        }

        sealed class TestActionAnimationPresenter : MonoBehaviour, IActionAnimationPresenter
        {
            public int PresentCount { get; private set; }
            public int ClearCount { get; private set; }
            public ActionAnimationKey CurrentKey { get; private set; }
            public float CurrentNormalizedTime => 0f;
            public bool HasValidPlayback => CurrentKey.IsValid;
            public string CurrentAnimationName => CurrentKey.Value;

            public bool Present(in CharacterStateAnimationRequest request)
            {
                PresentCount++;
                CurrentKey = request.Key;
                return true;
            }

            public void Clear()
            {
                ClearCount++;
                CurrentKey = default;
            }
        }
    }
}
