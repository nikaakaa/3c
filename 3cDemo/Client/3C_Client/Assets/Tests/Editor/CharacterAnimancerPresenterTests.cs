using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Animancer;
using Animancer.TransitionLibraries;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonAnimation;
using ThirdPersonCharacterStateMachine;
using ThirdPersonMovement;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class CharacterAnimancerPresenterTests
    {
        const string UnifiedPresenterGuid = "3f6c52e6a4dd4fe3a996c8b5df6f2201";
        const string BasicPresenterGuid = "5a5b6c35cff14f98994e3fef16d478f5";
        const string ActionPresenterGuid = "1fe0054a52138614aa2611b735361ea5";

        [Test]
        public void PresenterPlaysLocomotionAlias()
        {
            CreatePresenterRig(out GameObject gameObject, out CharacterAnimancerPresenter presenter);
            try
            {
                presenter.PresentLocomotion(new MovementAnimationContext(
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    true,
                    1f,
                    Vector3.forward,
                    4f));

                CharacterAnimationPlaybackSnapshot snapshot = presenter.CurrentSnapshot;
                Assert.AreEqual(CharacterAnimationPlaybackDomain.Locomotion, snapshot.ActiveDomain);
                Assert.AreEqual("RunLoop", snapshot.ActiveStableKey);
                Assert.AreEqual("RunLoop", snapshot.LocomotionProgress.AliasKey);
                Assert.AreEqual("RunLoop", snapshot.LocomotionAnimationName);
                Assert.True(snapshot.LocomotionProgress.HasValidPlayback);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PresenterPlaysActionKey()
        {
            CreatePresenterRig(out GameObject gameObject, out CharacterAnimancerPresenter presenter);
            try
            {
                CharacterStateAnimationRequest request = ActionRequest(ActionAnimationKeys.DodgeDirectional.Value, 12);

                Assert.True(presenter.PresentAction(in request));

                CharacterAnimationPlaybackSnapshot snapshot = presenter.CurrentSnapshot;
                Assert.AreEqual(CharacterAnimationPlaybackDomain.Action, snapshot.ActiveDomain);
                Assert.AreEqual(ActionAnimationKeys.DodgeDirectional.Value, snapshot.ActiveStableKey);
                Assert.AreEqual(new ActionAnimationKey(ActionAnimationKeys.DodgeDirectional.Value), snapshot.ActionProgress.Key);
                Assert.AreEqual("DodgeDirectional", snapshot.ActionAnimationName);
                Assert.True(snapshot.ActionProgress.HasValidPlayback);
                Assert.AreEqual(12, snapshot.SourceStep);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SameActionKeyAfterRestoreDoesNotRestart()
        {
            CreatePresenterRig(out GameObject gameObject, out CharacterAnimancerPresenter presenter);
            try
            {
                ActionAnimationKey key = new ActionAnimationKey(ActionAnimationKeys.DodgeDirectional.Value);
                Assert.True(presenter.RestorePlaybackProgress(new ActionAnimationPlaybackProgress(key, 0.42f, true, false, new ActionAnimationPlaybackIntent(1)), "DodgeDirectional"));

                CharacterStateAnimationRequest request = ActionRequest(ActionAnimationKeys.DodgeDirectional.Value, 14, 1);
                Assert.True(presenter.PresentAction(in request));

                Assert.AreEqual(0.42f, presenter.CurrentSnapshot.ActionProgress.NormalizedTime, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SameActionKeyWithNewPlaybackIntentRestarts()
        {
            CreatePresenterRig(out GameObject gameObject, out CharacterAnimancerPresenter presenter);
            try
            {
                ActionAnimationKey key = new ActionAnimationKey(ActionAnimationKeys.DodgeDirectional.Value);
                Assert.True(presenter.RestorePlaybackProgress(new ActionAnimationPlaybackProgress(key, 0.42f, true, false, new ActionAnimationPlaybackIntent(1)), "DodgeDirectional"));

                CharacterStateAnimationRequest request = ActionRequest(ActionAnimationKeys.DodgeDirectional.Value, 15, 2);
                Assert.True(presenter.PresentAction(in request));

                CharacterAnimationPlaybackSnapshot snapshot = presenter.CurrentSnapshot;
                Assert.AreEqual(0f, snapshot.ActionProgress.NormalizedTime, 0.0001f);
                Assert.AreEqual(new ActionAnimationPlaybackIntent(2), snapshot.ActionProgress.PlaybackIntent);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void LocomotionRestoreThenSameAliasDoesNotRestart()
        {
            CreatePresenterRig(out GameObject gameObject, out CharacterAnimancerPresenter presenter);
            try
            {
                AnimationPhasePlaybackProgress progress = new AnimationPhasePlaybackProgress(
                    BasicMovementPhase.MoveLoop,
                    "RunLoop",
                    0.37f,
                    true,
                    false);
                Assert.True(presenter.RestorePlaybackProgress(in progress, BasicMovementGait.Run));

                presenter.PresentLocomotion(new MovementAnimationContext(
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    true,
                    1f,
                    Vector3.forward,
                    4f));

                Assert.AreEqual(0.37f, presenter.CurrentSnapshot.LocomotionProgress.NormalizedTime, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ClearActionPlaybackClearsActionFactAndKeepsLocomotionFact()
        {
            CreatePresenterRig(out GameObject gameObject, out CharacterAnimancerPresenter presenter);
            try
            {
                presenter.PresentLocomotion(new MovementAnimationContext(
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    true,
                    1f,
                    Vector3.forward,
                    4f));
                CharacterStateAnimationRequest request = ActionRequest(ActionAnimationKeys.DodgeBackstep.Value, 21);
                presenter.PresentAction(in request);

                presenter.ClearActionPlayback();

                CharacterAnimationPlaybackSnapshot snapshot = presenter.CurrentSnapshot;
                Assert.AreEqual(CharacterAnimationPlaybackDomain.Locomotion, snapshot.ActiveDomain);
                Assert.False(snapshot.ActionProgress.HasValidPlayback);
                Assert.True(snapshot.LocomotionProgress.HasValidPlayback);
                Assert.AreEqual("RunLoop", snapshot.LocomotionProgress.AliasKey);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TurnBackRequestUsesPolicyStartNormalizedTime()
        {
            CreatePresenterRig(out GameObject gameObject, out CharacterAnimancerPresenter presenter);
            try
            {
                TurnBackMotionPolicy policy = new TurnBackMotionPolicy(
                    TurnBackMotionPolicy.DefaultAliasKey,
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    TurnBackMotionYawSource.BakedMotionProfile,
                    TurnBackMotionTranslationSource.BakedMotionProfile,
                    true,
                    true,
                    1f,
                    0.08f,
                    0.28f,
                    1f,
                    1f,
                    TurnBackMotionPolicy.DefaultBakedMotionProfileId);

                presenter.PresentLocomotion(new MovementAnimationContext(
                    BasicMovementPhase.TurnBack,
                    BasicMovementGait.Run,
                    true,
                    1f,
                    Vector3.back,
                    0f,
                    policy,
                    true));

                Assert.AreEqual(0.28f, presenter.CurrentSnapshot.LocomotionProgress.NormalizedTime, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FootPhaseStartOverrideIsAppliedToRunLoop()
        {
            CreatePresenterRig(out GameObject gameObject, out CharacterAnimancerPresenter presenter);
            try
            {
                LocomotionFootPhaseMatchResult match = new LocomotionFootPhaseMatchResult(
                    true,
                    LocomotionFootPhase.LeftPlant,
                    0.73f,
                    "RunLoop",
                    "test");

                presenter.PresentLocomotion(new MovementAnimationContext(
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    true,
                    1f,
                    Vector3.forward,
                    4f,
                    default,
                    false,
                    match,
                    true));

                Assert.AreEqual(0.73f, presenter.CurrentSnapshot.LocomotionProgress.NormalizedTime, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FormalRuntimeDoesNotReferenceOldPresentersAsEntryPoints()
        {
            string runtime = File.ReadAllText("Assets/Scripts/Character/Pipeline/Runtime/CharacterFrameRuntimeController.cs", Encoding.UTF8);

            Assert.That(runtime, Does.Contain("ILocomotionAnimationPresenter"));
            Assert.That(runtime, Does.Contain("ICharacterAnimationOutputPresenter"));
            Assert.That(runtime, Does.Not.Contain("IActionAnimationPresenter animationPresenter"));
        }

        [Test]
        public void UnifiedPresenterDoesNotOwnStateOrMotionSideEffects()
        {
            string source = File.ReadAllText("Assets/Scripts/Character/Animation/Runtime/CharacterAnimancerPresenter.cs", Encoding.UTF8);

            Assert.That(source, Does.Not.Contain("CharacterController.Move"));
            Assert.That(source, Does.Not.Contain(".position ="));
            Assert.That(source, Does.Not.Contain(".rotation ="));
            Assert.That(source, Does.Not.Contain("ExecuteBasicMovement"));
            Assert.That(source, Does.Not.Contain("ExecuteActionMovement"));
            Assert.That(source, Does.Not.Contain("CharacterStateMachineRunner"));
            Assert.That(source, Does.Not.Contain("ActionInterruptArbiter"));
            Assert.That(source, Does.Not.Contain("InputRequestBuffer"));
        }

        [Test]
        public void FormalPrefabsUseOnlyUnifiedAnimancerPresenter()
        {
            AssertFormalPrefabUsesOnlyUnifiedPresenter("Assets/Prefabs/Character/可琳.prefab");
            AssertFormalPrefabUsesOnlyUnifiedPresenter("Assets/Prefabs/Character/可琳_Humanoid.prefab");
        }

        [Test]
        public void HumanoidAssemblerBuildsUnifiedPresenterOnly()
        {
            string assembler = File.ReadAllText("Assets/Editor/CorinHumanoidPresentationAssembler.cs", Encoding.UTF8);

            Assert.That(assembler, Does.Contain("AddComponent<CharacterAnimancerPresenter>"));
            Assert.That(assembler, Does.Contain("LocomotionPresenterBehaviour = presenter"));
            Assert.That(assembler, Does.Contain("AnimationPresenterBehaviour = presenter"));
            Assert.That(assembler, Does.Not.Contain("LocomotionPresenter ="));
            Assert.That(assembler, Does.Not.Contain("Assets/Configs/3C/Animacer"));
            Assert.That(assembler, Does.Not.Contain("Pramater"));
        }

        [Test]
        public void SandboxSceneDoesNotOverrideOldDualPresenterPair()
        {
            string yaml = File.ReadAllText("Assets/Scenes/Sandbox.unity", Encoding.UTF8);

            bool hasBasic = yaml.Contains(BasicPresenterGuid);
            bool hasAction = yaml.Contains(ActionPresenterGuid);
            Assert.False(hasBasic && hasAction);
        }

        static void AssertFormalPrefabUsesOnlyUnifiedPresenter(string path)
        {
            string yaml = File.ReadAllText(path, Encoding.UTF8);
            Assert.AreEqual(1, Regex.Matches(yaml, UnifiedPresenterGuid).Count, path);
            Assert.Zero(Regex.Matches(yaml, BasicPresenterGuid).Count, path);
            Assert.Zero(Regex.Matches(yaml, ActionPresenterGuid).Count, path);
        }

        static CharacterStateAnimationRequest ActionRequest(string key, int sourceStep, int playbackIntent = 1)
        {
            return new CharacterStateAnimationRequest(
                CharacterStateAnimationBinding.FromKey(key, key),
                CharacterStatePlaybackFactSource.Action,
                sourceStep,
                new ActionAnimationPlaybackIntent(playbackIntent));
        }

        static void CreatePresenterRig(
            out GameObject gameObject,
            out CharacterAnimancerPresenter presenter)
        {
            gameObject = new GameObject("fullbody-animancer-presenter-rig");
            gameObject.AddComponent<Animator>();
            AnimancerComponent animancer = gameObject.AddComponent<AnimancerComponent>();
            presenter = gameObject.AddComponent<CharacterAnimancerPresenter>();
            AnimatorRootMotionController.Resolve(animancer);

            TransitionLibrary library = new TransitionLibrary();
            library.AddTransition(StringReference.Get("Idle"), CreateClipTransition(CreateClip("Idle")));
            library.AddTransition(StringReference.Get("RunLoop"), CreateClipTransition(CreateClip("RunLoop")));
            library.AddTransition(StringReference.Get("Locomotion.Turn.Back"), CreateClipTransition(CreateClip("TurnBack")));
            library.AddTransition(StringReference.Get(ActionAnimationKeys.DodgeDirectional.Value), CreateClipTransition(CreateClip("DodgeDirectional")));
            library.AddTransition(StringReference.Get(ActionAnimationKeys.DodgeBackstep.Value), CreateClipTransition(CreateClip("DodgeBackstep")));
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
    }
}
