using System.IO;
using System.Linq;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonAnimation;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonMovement;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class FullBodyConfigAuthoringLayoutTests
    {
        const string CharacterConfigPath = "Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset";
        const string OldCharacterConfigPath = "Assets/Configs/3C/CharacterConfig.asset";
        const string StateMachinePath = "Assets/Configs/3C/StateMachine/FullBody/CorinFullBodyStateMachine.asset";
        const string DodgeActionPath = "Assets/Configs/3C/Action/FullBody/Dodge/CorinDodgeActionConfig.asset";
        const string RequestPolicyPath = "Assets/Configs/3C/Action/FullBody/RequestPolicy/CorinFullBodyStateRequestPolicySet.asset";
        const string LocomotionAnimationPath = "Assets/Configs/3C/Animation/Corin/Locomotion/CorinLocomotionAnimationConfig.asset";
        const string GenericAnimancerPath = "Assets/Configs/3C/Animation/Corin/Animancer/RigVariants/Generic/CorinGenericAnimancerTransitionLibrary.asset";
        const string HumanoidReferencePath = "Assets/Configs/3C/Animation/Corin/Animancer/Reference/Humanoid/CorinHumanoid_TransitionLib.asset";
        const string InputActionsPath = "Assets/Configs/3C/Input/CharacterInput.inputactions";

        [Test]
        public void CorinCharacterConfigIsFormalRootAndOldRootAssetIsGone()
        {
            CharacterConfigSO config = LoadRequired<CharacterConfigSO>(CharacterConfigPath);

            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Object>(OldCharacterConfigPath));
            Assert.NotNull(config.StateMachine);
            Assert.NotNull(config.Movement);
            Assert.NotNull(config.LocomotionAnimation);
            Assert.NotNull(config.FullBodyStateRequestPolicy);
            Assert.NotNull(config.DodgeAction);
            Assert.NotNull(config.FullBodyActionAnimation);
            Assert.NotNull(config.AnimancerRigVariant);
            Assert.NotNull(config.InputActions);
            Assert.NotNull(config.MoveAction);
            Assert.NotNull(config.RunAction);
            Assert.NotNull(config.LookAction);
            Assert.NotNull(config.CameraConfig);
        }

        [Test]
        public void FormalConfigAssetsUseApprovedPathsAndNames()
        {
            string[] paths =
            {
                CharacterConfigPath,
                StateMachinePath,
                DodgeActionPath,
                RequestPolicyPath,
                LocomotionAnimationPath,
                GenericAnimancerPath,
                InputActionsPath
            };

            foreach (string path in paths)
            {
                Assert.NotNull(AssetDatabase.LoadAssetAtPath<Object>(path), path);
                Assert.That(Path.GetFileNameWithoutExtension(path), Does.Not.StartWith("Default"));
                Assert.That(path, Does.Not.Contain("Animacer"));
                Assert.That(path, Does.Not.Contain("Pramater"));
                Assert.That(path, Does.Not.Contain("Statemachine"));
            }

            Assert.False(AssetDatabase.IsValidFolder("Assets/Configs/3C/Animacer"));
            Assert.True(HasImmediateDirectory("Assets/Configs/3C", "StateMachine"));
            Assert.False(HasImmediateDirectory("Assets/Configs/3C", "Statemachine"));
            Assert.False(AssetDatabase.IsValidFolder("Assets/Configs/3C/Animation/Locomotion"));
            Assert.False(AssetDatabase.IsValidFolder("Assets/Configs/3C/Animation/FullBody"));
            Assert.False(AssetDatabase.IsValidFolder("Assets/Configs/3C/Animation/Corin/FullBody"));
        }

        [Test]
        public void GenericAnimancerLibraryIsTheFormalRigVariant()
        {
            CharacterConfigSO config = LoadRequired<CharacterConfigSO>(CharacterConfigPath);

            Assert.AreEqual(GenericAnimancerPath, AssetDatabase.GetAssetPath(config.AnimancerRigVariant));
            Assert.AreEqual(GenericAnimancerPath, AssetDatabase.GetAssetPath(config.FullBodyActionAnimation));
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<Object>(HumanoidReferencePath));
            Assert.That(AssetDatabase.GetAssetPath(config.AnimancerRigVariant), Does.Not.Contain("/Reference/Humanoid/"));
        }

        [Test]
        public void RequestPolicyUsesFullBodyStateRequestScope()
        {
            ActionInterruptPolicySetSO policy = LoadRequired<ActionInterruptPolicySetSO>(RequestPolicyPath);

            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Object>(
                "Assets/Configs/3C/Action/DefaultDodgeInterruptPolicySet.asset"));
            Assert.That(policy.name, Does.Not.Contain("DodgeInterrupt"));
            Assert.True(policy.CompilePolicies().Any(item => item.TargetState.Value == CharacterStateIds.TurnBack.Value));
        }

        [Test]
        public void LocomotionConfigReferencesFormalMotionAndFootPhaseProfiles()
        {
            RunLocomotionAnimationConfigSO config = LoadRequired<RunLocomotionAnimationConfigSO>(LocomotionAnimationPath);

            foreach (LocomotionPhaseMotionProfileBinding binding in config.MotionProfiles)
                AssertApprovedProfilePath(binding.Profile);

            foreach (LocomotionPhaseFootPhaseProfileBinding binding in config.FootPhaseProfiles)
                AssertApprovedProfilePath(binding.Profile);
        }

        [Test]
        public void StateMachineDoesNotOwnDodgeMotionNumbers()
        {
            string yaml = ReadYaml(StateMachinePath);

            Assert.That(yaml, Does.Not.Contain("\n      output:"));
            Assert.That(yaml, Does.Not.Contain("duration: 0.35\n        distance: 4"));
            Assert.That(yaml, Does.Not.Contain("duration: 0.35\n        distance: 3"));
            Assert.That(yaml, Does.Contain("actionMovements:\n      - variant: 1\n        duration: 0\n        distance: 0"));
            Assert.That(yaml, Does.Contain("- variant: 2\n        duration: 0\n        distance: 0"));
        }

        [Test]
        public void DodgeMotionSpecAdapterUsesFormalActionConfigForStateMachinePlaceholders()
        {
            DodgeActionConfigSO actionConfig = LoadRequired<DodgeActionConfigSO>(DodgeActionPath);
            DodgeActionConfig dodgeConfig = actionConfig.ToConfig();
            ActionMotionSpec directionalSpec = PlaceholderDodgeSpec(CharacterStateVariant.Directional, Vector3.forward);
            ActionMotionSpec backstepSpec = PlaceholderDodgeSpec(CharacterStateVariant.Backstep, Vector3.back);
            directionalSpec = DodgeActionMotionSpecAdapter.Resolve(directionalSpec, true, in dodgeConfig);
            backstepSpec = DodgeActionMotionSpecAdapter.Resolve(backstepSpec, true, in dodgeConfig);

            ActionMotionResolveInput directionalInput = new ActionMotionResolveInput(
                directionalSpec,
                0.1f,
                StateTimelineWindowFacts.None(default),
                CharacterRuntimeActionFacts.Default);
            ActionMotionResolveInput backstepInput = new ActionMotionResolveInput(
                backstepSpec,
                0.1f,
                StateTimelineWindowFacts.None(default),
                CharacterRuntimeActionFacts.Default);
            ActionMotionResolveResult directional = ActionMotionResolver.Resolve(in directionalInput);
            ActionMotionResolveResult backstep = ActionMotionResolver.Resolve(in backstepInput);

            Assert.AreEqual(0.35f, dodgeConfig.DirectionalDuration, 0.0001f);
            Assert.AreEqual(4f, dodgeConfig.DirectionalDistance, 0.0001f);
            Assert.AreEqual(0.35f, dodgeConfig.BackstepDuration, 0.0001f);
            Assert.AreEqual(3f, dodgeConfig.BackstepDistance, 0.0001f);
            Assert.AreEqual(4f * 0.1f / 0.35f, directional.MovementCommand.PlanarDistance, 0.0001f);
            Assert.True(directional.MovementCommand.RotateToDirection);
            Assert.AreEqual(3f * 0.1f / 0.35f, backstep.MovementCommand.PlanarDistance, 0.0001f);
            Assert.False(backstep.MovementCommand.RotateToDirection);
        }

        [Test]
        public void DodgeMotionResolverDoesNotUseCodeDefaultWhenFormalActionConfigIsMissing()
        {
            ActionMotionSpec spec = PlaceholderDodgeSpec(CharacterStateVariant.Directional, Vector3.forward);
            ActionMotionResolveInput input = new ActionMotionResolveInput(
                spec,
                0.1f,
                StateTimelineWindowFacts.None(default),
                CharacterRuntimeActionFacts.Default);

            ActionMotionResolveResult result = ActionMotionResolver.Resolve(in input);

            Assert.False(result.HasActionMovement);
            Assert.AreEqual(0f, result.MovementCommand.PlanarDistance, 0.0001f);
        }

        static T LoadRequired<T>(string path)
            where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.NotNull(asset, path);
            return asset;
        }

        static void AssertApprovedProfilePath(Object profile)
        {
            Assert.NotNull(profile);
            string path = AssetDatabase.GetAssetPath(profile);
            Assert.That(path, Does.StartWith("Assets/Configs/3C/Animation/Corin/Locomotion/"));
            Assert.That(path, Does.Not.Contain("/Reference/"));
            Assert.That(path, Does.Not.Contain("DefaultRun"));
            Assert.That(path, Does.Not.Contain("TestTurnback"));
            Assert.That(path, Does.Not.Contain("turnback613"));
            Assert.That(path, Does.Not.Contain("testTurn"));
        }

        static string ReadYaml(string path)
        {
            return File.ReadAllText(path, System.Text.Encoding.UTF8).Replace("\r\n", "\n");
        }

        static bool HasImmediateDirectory(string parent, string expectedName)
        {
            foreach (string directory in Directory.GetDirectories(parent))
                if (string.Equals(Path.GetFileName(directory), expectedName, System.StringComparison.Ordinal))
                    return true;

            return false;
        }

        static ActionMotionSpec PlaceholderDodgeSpec(CharacterStateVariant variant, Vector3 direction)
        {
            return new ActionMotionSpec(
                ActionStateIds.Dodge,
                CharacterStateIds.Dodge,
                variant,
                0f,
                0f,
                false,
                variant == CharacterStateVariant.Directional,
                direction,
                0.1f,
                1);
        }
    }
}
