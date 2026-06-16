using System.IO;
using Animancer;
using Animancer.TransitionLibraries;
using NUnit.Framework;
using ThirdPersonAnimation;
using ThirdPersonCharacterStateMachine;
using ThirdPersonMovement;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class LocomotionFootPhaseMatchingTests
    {
        [Test]
        public void SamplerSamplesLeftPlantByNormalizedTime()
        {
            LocomotionFootPhaseProfileSO profile = CreateRunLoopProfile();
            try
            {
                LocomotionFootPhaseSample sample = LocomotionFootPhaseSampler.Sample(
                    profile,
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    "RunLoop",
                    0.1f,
                    7);

                Assert.True(sample.IsValid);
                Assert.AreEqual(LocomotionFootPhase.LeftPlant, sample.FootPhase);
                Assert.AreEqual(7, sample.SourceStep);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SamplerSamplesRightPlantByNormalizedTime()
        {
            LocomotionFootPhaseProfileSO profile = CreateRunLoopProfile();
            try
            {
                LocomotionFootPhaseSample sample = LocomotionFootPhaseSampler.Sample(
                    profile,
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    "RunLoop",
                    0.5f);

                Assert.True(sample.IsValid);
                Assert.AreEqual(LocomotionFootPhase.RightPlant, sample.FootPhase);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SamplerWrapsLoopNormalizedTime()
        {
            LocomotionFootPhaseProfileSO profile = CreateRunLoopProfile();
            try
            {
                LocomotionFootPhaseSample sample = LocomotionFootPhaseSampler.Sample(
                    profile,
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    "RunLoop",
                    1.1f);

                Assert.True(sample.IsValid);
                Assert.AreEqual(0.1f, sample.NormalizedTime, 0.0001f);
                Assert.AreEqual(LocomotionFootPhase.LeftPlant, sample.FootPhase);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void SamplerReturnsInvalidWhenProfileDisabled()
        {
            LocomotionFootPhaseProfileSO profile = CreateRunLoopProfile(false);
            try
            {
                LocomotionFootPhaseSample sample = LocomotionFootPhaseSampler.Sample(
                    profile,
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    "RunLoop",
                    0.5f);

                Assert.False(sample.IsValid);
                Assert.AreEqual(LocomotionFootPhase.Unknown, sample.FootPhase);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void MatcherMatchesRightPlantExitToRunLoopMarker()
        {
            LocomotionFootPhaseProfileSO profile = CreateRunLoopProfile();
            try
            {
                LocomotionFootPhaseMatchResult result = LocomotionFootPhaseMatcher.Match(
                    CreateMatchRequest(LocomotionFootPhase.RightPlant),
                    profile);

                Assert.True(result.IsValid);
                Assert.AreEqual(LocomotionFootPhase.RightPlant, result.MatchedPhase);
                Assert.AreEqual(0.5f, result.StartNormalizedTime, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void MatcherMatchesLeftPlantExitToRunLoopMarker()
        {
            LocomotionFootPhaseProfileSO profile = CreateRunLoopProfile();
            try
            {
                LocomotionFootPhaseMatchResult result = LocomotionFootPhaseMatcher.Match(
                    CreateMatchRequest(LocomotionFootPhase.LeftPlant),
                    profile);

                Assert.True(result.IsValid);
                Assert.AreEqual(LocomotionFootPhase.LeftPlant, result.MatchedPhase);
                Assert.AreEqual(0f, result.StartNormalizedTime, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void MatcherReturnsInvalidWhenTargetMarkerMissing()
        {
            LocomotionFootPhaseProfileSO profile = ScriptableObject.CreateInstance<LocomotionFootPhaseProfileSO>();
            profile.SetProfileData(
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                "RunLoop",
                true,
                true,
                new LocomotionFootPhaseMarker(LocomotionFootPhase.LeftPlant, 0f));

            try
            {
                LocomotionFootPhaseMatchResult result = LocomotionFootPhaseMatcher.Match(
                    CreateMatchRequest(LocomotionFootPhase.RightPlant),
                    profile);

                Assert.False(result.IsValid);
                Assert.AreEqual("target-marker-missing", result.Reason);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void BlackboardFootPhaseFactsDefaultToInvalid()
        {
            CharacterRuntimeAnimationFacts facts = CharacterRuntimeAnimationFacts.Default;

            Assert.False(facts.CurrentLocomotionFootPhase.IsValid);
            Assert.False(facts.LastLocomotionExitFootPhase.IsValid);
            Assert.AreEqual(LocomotionFootPhase.Unknown, facts.CurrentLocomotionFootPhase.FootPhase);
            Assert.AreEqual(LocomotionFootPhase.Unknown, facts.LastLocomotionExitFootPhase.FootPhase);
        }

        [Test]
        public void BlackboardSnapshotRestorePreservesFootPhaseFacts()
        {
            CharacterRuntimeBlackboard blackboard = new CharacterRuntimeBlackboard();
            LocomotionFootPhaseSample current = new LocomotionFootPhaseSample(
                true,
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                "RunLoop",
                0.5f,
                LocomotionFootPhase.RightPlant,
                11);
            LocomotionFootPhaseSample exit = new LocomotionFootPhaseSample(
                true,
                BasicMovementPhase.TurnBack,
                BasicMovementGait.Run,
                "Locomotion.Turn.Back",
                0.92f,
                LocomotionFootPhase.RightPlant,
                10);

            blackboard.WriteAnimationFacts(new CharacterRuntimeAnimationFacts(
                new AnimationPhasePlaybackProgress(BasicMovementPhase.MoveLoop, "RunLoop", 0.5f, true, false),
                "RunLoop",
                ActionAnimationPlaybackProgress.Invalid,
                string.Empty,
                current,
                exit,
                11));
            CharacterRuntimeBlackboardRestoreState restoreState = blackboard.CaptureRestoreState();

            blackboard.Reset();
            blackboard.Restore(in restoreState);

            CharacterRuntimeAnimationFacts restored = blackboard.Snapshot.Animation;
            Assert.True(restored.CurrentLocomotionFootPhase.IsValid);
            Assert.True(restored.LastLocomotionExitFootPhase.IsValid);
            Assert.AreEqual(LocomotionFootPhase.RightPlant, restored.CurrentLocomotionFootPhase.FootPhase);
            Assert.AreEqual(0.92f, restored.LastLocomotionExitFootPhase.NormalizedTime, 0.0001f);
        }

        [Test]
        public void PresenterAppliesRunLoopStartOverrideOnNewPlayback()
        {
            CreatePresenterRig(out GameObject gameObject, out BasicLocomotionAnimancerPresenter presenter, out AnimancerComponent animancer, out AnimationClip runClip);
            try
            {
                MovementAnimationContext context = CreateRunLoopContext(
                    new LocomotionFootPhaseMatchResult(true, LocomotionFootPhase.RightPlant, 0.5f, "RunLoop", "matched"),
                    true);

                presenter.Present(in context);

                AnimancerState state = animancer.Graph.Layers[0].CurrentState;
                Assert.AreSame(runClip, state.MainObject);
                Assert.AreEqual(0.5f, state.NormalizedTime, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(runClip);
            }
        }

        [Test]
        public void PresenterDoesNotRepeatRunLoopStartOverrideOnSamePlayback()
        {
            CreatePresenterRig(out GameObject gameObject, out BasicLocomotionAnimancerPresenter presenter, out AnimancerComponent animancer, out AnimationClip runClip);
            try
            {
                MovementAnimationContext context = CreateRunLoopContext(
                    new LocomotionFootPhaseMatchResult(true, LocomotionFootPhase.RightPlant, 0.5f, "RunLoop", "matched"),
                    true);

                presenter.Present(in context);
                AnimancerState state = animancer.Graph.Layers[0].CurrentState;
                state.NormalizedTime = 0.75f;

                presenter.Present(in context);

                Assert.AreSame(runClip, animancer.Graph.Layers[0].CurrentState.MainObject);
                Assert.AreEqual(0.75f, animancer.Graph.Layers[0].CurrentState.NormalizedTime, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(runClip);
            }
        }

        [Test]
        public void SamplerKeepsPureDataBoundary()
        {
            string sampler = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Animation/Solver/LocomotionFootPhaseSampler.cs"));

            Assert.That(sampler, Does.Not.Contain("Animancer"));
            Assert.That(sampler, Does.Not.Contain("AnimationClip"));
            Assert.That(sampler, Does.Not.Contain("Transform"));
            Assert.That(sampler, Does.Not.Contain("CharacterController"));
            Assert.That(sampler, Does.Not.Contain("InputAction"));
        }

        [Test]
        public void ConfigValidationReportsMissingTurnBackProfile()
        {
            RunLocomotionAnimationConfigSO config = ScriptableObject.CreateInstance<RunLocomotionAnimationConfigSO>();
            LocomotionFootPhaseProfileSO runLoop = CreateRunLoopProfile();
            try
            {
                config.SetFootPhaseProfileBindings(new LocomotionPhaseFootPhaseProfileBinding(
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    "RunLoop",
                    runLoop));

                RunLocomotionAnimationConfigValidationResult result = config.Validate();

                Assert.True(result.HasErrors);
                Assert.That(result.DescribeErrors(), Does.Contain("TurnBack foot phase profile is missing."));
            }
            finally
            {
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(runLoop);
            }
        }

        [Test]
        public void ConfigValidationReportsMissingRunLoopProfile()
        {
            RunLocomotionAnimationConfigSO config = ScriptableObject.CreateInstance<RunLocomotionAnimationConfigSO>();
            LocomotionFootPhaseProfileSO turnBack = CreateTurnBackProfile();
            try
            {
                config.SetFootPhaseProfileBindings(new LocomotionPhaseFootPhaseProfileBinding(
                    BasicMovementPhase.TurnBack,
                    BasicMovementGait.Run,
                    "Locomotion.Turn.Back",
                    turnBack));

                RunLocomotionAnimationConfigValidationResult result = config.Validate();

                Assert.True(result.HasErrors);
                Assert.That(result.DescribeErrors(), Does.Contain("RunLoop (MoveLoop + Run) foot phase profile is missing."));
            }
            finally
            {
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(turnBack);
            }
        }

        [Test]
        public void SharedTurnBackAliasKeepsFallbackGaitForFootPhaseProfile()
        {
            RunLocomotionAnimationConfigSO config = ScriptableObject.CreateInstance<RunLocomotionAnimationConfigSO>();
            LocomotionFootPhaseProfileSO turnBack = CreateTurnBackProfile();
            try
            {
                config.SetFootPhaseProfileBindings(new LocomotionPhaseFootPhaseProfileBinding(
                    BasicMovementPhase.TurnBack,
                    BasicMovementGait.Run,
                    "Locomotion.Turn.Back",
                    turnBack));

                BasicMovementGait gait = LocomotionAnimationAliasResolver.ResolveGaitForAlias(
                    config,
                    BasicMovementPhase.TurnBack,
                    "Locomotion.Turn.Back",
                    BasicMovementGait.Run);
                LocomotionFootPhaseProfileSO resolved = config.ResolveFootPhaseProfile(
                    BasicMovementPhase.TurnBack,
                    gait,
                    "Locomotion.Turn.Back");

                Assert.AreEqual(BasicMovementGait.Run, gait);
                Assert.AreSame(turnBack, resolved);
            }
            finally
            {
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(turnBack);
            }
        }

        [Test]
        public void CorinConfigResolvesStartAndLoopFootPhaseProfiles()
        {
            RunLocomotionAnimationConfigSO config = AssetDatabase.LoadAssetAtPath<RunLocomotionAnimationConfigSO>(
                "Assets/Configs/3C/Animation/Corin/Locomotion/CorinLocomotionAnimationConfig.asset");

            Assert.NotNull(config);
            AssertResolvesProfile(config, BasicMovementPhase.MoveStart, BasicMovementGait.Walk, "WalkStart");
            AssertResolvesProfile(config, BasicMovementPhase.MoveLoop, BasicMovementGait.Walk, "WalkLoop");
            AssertResolvesProfile(config, BasicMovementPhase.MoveStart, BasicMovementGait.Run, "RunStart");
            AssertResolvesProfile(config, BasicMovementPhase.MoveLoop, BasicMovementGait.Run, "RunLoop");
        }

        static LocomotionFootPhaseMatchRequest CreateMatchRequest(LocomotionFootPhase phase)
        {
            return new LocomotionFootPhaseMatchRequest(
                new LocomotionFootPhaseSample(
                    true,
                    BasicMovementPhase.TurnBack,
                    BasicMovementGait.Run,
                    "Locomotion.Turn.Back",
                    0.92f,
                    phase,
                    4),
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                "RunLoop");
        }

        static MovementAnimationContext CreateRunLoopContext(
            LocomotionFootPhaseMatchResult result,
            bool hasRequest)
        {
            return new MovementAnimationContext(
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                true,
                1f,
                Vector3.forward,
                5f,
                default,
                false,
                result,
                hasRequest);
        }

        static void AssertResolvesProfile(
            RunLocomotionAnimationConfigSO config,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey)
        {
            LocomotionFootPhaseProfileSO profile = config.ResolveFootPhaseProfile(phase, gait, aliasKey);

            Assert.NotNull(profile, $"{phase}/{gait}/{aliasKey}");
            Assert.True(profile.EnablePhaseMatching);
            Assert.AreEqual(phase, profile.Phase);
            Assert.AreEqual(gait, profile.Gait);
            Assert.AreEqual(aliasKey, profile.AliasKey);
            Assert.GreaterOrEqual(profile.Markers.Length, 2);
        }

        static LocomotionFootPhaseProfileSO CreateRunLoopProfile(bool enabled = true)
        {
            LocomotionFootPhaseProfileSO profile = ScriptableObject.CreateInstance<LocomotionFootPhaseProfileSO>();
            profile.SetProfileData(
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                "RunLoop",
                enabled,
                true,
                new LocomotionFootPhaseMarker(LocomotionFootPhase.LeftPlant, 0f),
                new LocomotionFootPhaseMarker(LocomotionFootPhase.RightPlant, 0.5f));
            return profile;
        }

        static LocomotionFootPhaseProfileSO CreateTurnBackProfile()
        {
            LocomotionFootPhaseProfileSO profile = ScriptableObject.CreateInstance<LocomotionFootPhaseProfileSO>();
            profile.SetProfileData(
                BasicMovementPhase.TurnBack,
                BasicMovementGait.Run,
                "Locomotion.Turn.Back",
                true,
                false,
                new LocomotionFootPhaseMarker(LocomotionFootPhase.LeftPlant, 0f),
                new LocomotionFootPhaseMarker(LocomotionFootPhase.RightPlant, 0.92f));
            return profile;
        }

        static void CreatePresenterRig(
            out GameObject gameObject,
            out BasicLocomotionAnimancerPresenter presenter,
            out AnimancerComponent animancer,
            out AnimationClip runClip)
        {
            gameObject = new GameObject("foot-phase-presenter-rig");
            gameObject.AddComponent<Animator>();
            animancer = gameObject.AddComponent<AnimancerComponent>();
            presenter = gameObject.AddComponent<BasicLocomotionAnimancerPresenter>();
            AnimatorRootMotionController.Resolve(animancer);
            runClip = new AnimationClip { name = "RunLoop" };
            runClip.legacy = false;

            TransitionLibrary library = new TransitionLibrary();
            library.AddTransition(StringReference.Get("RunLoop"), CreateClipTransition(runClip));
            animancer.Graph.Transitions = library;
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

