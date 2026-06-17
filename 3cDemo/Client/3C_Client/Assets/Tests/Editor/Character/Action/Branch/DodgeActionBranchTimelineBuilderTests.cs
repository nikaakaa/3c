using System.Reflection;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using UnityEngine;

namespace Tests.Editor.Character.Action.Branch
{
    public sealed class DodgeActionBranchTimelineBuilderTests
    {
        [Test]
        public void DirectionalDodgeBuilderCreatesEquivalentTimelineDefinition()
        {
            CharacterActionDefinition definition = CreateDodgeDefinition();

            bool built = DodgeActionBranchTimelineBuilder.TryBuild(
                in definition,
                DodgeActionVariant.Directional,
                0.02f,
                out ActionBranchDefinition branch);
            ActionTimelineDefinition timeline = branch.RootNode.TimelineNode.Timeline;
            ActionTimelineOutcome outcome = ActionTimelineEvaluator.Evaluate(
                new ActionTimelineEvaluationInput(timeline, 0, 21));

            Assert.True(built);
            Assert.True(branch.CanEvaluate);
            Assert.AreEqual(21, timeline.DurationFrames);
            Assert.True(outcome.HasAnimation);
            Assert.True(outcome.HasMotion);
            Assert.AreEqual(ActionAnimationKeys.DodgeDirectional, outcome.AnimationKey);
            Assert.AreEqual(0.42f, outcome.MotionSpec.Duration, 0.0001f);
            Assert.AreEqual(5.5f, outcome.MotionSpec.Distance, 0.0001f);
            Assert.True(outcome.MotionSpec.RotateToDirection);
            Assert.True(outcome.MotionSpec.SetRunLatchOnComplete);
        }

        [Test]
        public void BackstepDodgeBuilderCreatesEquivalentTimelineDefinition()
        {
            CharacterActionDefinition definition = CreateDodgeDefinition();

            bool built = DodgeActionBranchTimelineBuilder.TryBuild(
                in definition,
                DodgeActionVariant.Backstep,
                0.02f,
                out ActionBranchDefinition branch);
            ActionTimelineDefinition timeline = branch.RootNode.TimelineNode.Timeline;
            ActionTimelineOutcome outcome = ActionTimelineEvaluator.Evaluate(
                new ActionTimelineEvaluationInput(timeline, 0, 31));

            Assert.True(built);
            Assert.True(branch.CanEvaluate);
            Assert.AreEqual(31, timeline.DurationFrames);
            Assert.True(outcome.HasAnimation);
            Assert.True(outcome.HasMotion);
            Assert.AreEqual(ActionAnimationKeys.DodgeBackstep, outcome.AnimationKey);
            Assert.AreEqual(0.61f, outcome.MotionSpec.Duration, 0.0001f);
            Assert.AreEqual(2.75f, outcome.MotionSpec.Distance, 0.0001f);
            Assert.False(outcome.MotionSpec.RotateToDirection);
            Assert.False(outcome.MotionSpec.SetRunLatchOnComplete);
        }

        [Test]
        public void DodgeTimelineMotionMatchesExistingMotionSpecAdapter()
        {
            CharacterActionDefinition definition = CreateDodgeDefinition();
            Assert.True(definition.TryGetDodgeTuning(out DodgeActionTuning tuning));
            ActionMotionSpec baseSpec = new ActionMotionSpec(
                ActionStateIds.Dodge,
                CharacterStateIds.Dodge,
                CharacterStateVariant.Directional,
                0f,
                0f,
                false,
                true,
                Vector3.forward,
                0f,
                4);
            ActionMotionSpec adapted = DodgeActionMotionSpecAdapter.Resolve(baseSpec, true, in tuning);

            DodgeActionBranchTimelineBuilder.TryBuild(
                in definition,
                DodgeActionVariant.Directional,
                0.02f,
                out ActionBranchDefinition branch);
            ActionTimelineOutcome outcome = ActionTimelineEvaluator.Evaluate(
                new ActionTimelineEvaluationInput(branch.RootNode.TimelineNode.Timeline, 0, 4));

            Assert.AreEqual(adapted.Duration, outcome.MotionSpec.Duration, 0.0001f);
            Assert.AreEqual(adapted.Distance, outcome.MotionSpec.Distance, 0.0001f);
            Assert.AreEqual(adapted.RotateToDirection, outcome.MotionSpec.RotateToDirection);
            Assert.AreEqual(adapted.SetRunLatchOnComplete, outcome.MotionSpec.SetRunLatchOnComplete);
        }

        [Test]
        public void DodgeTimelineKeepsCompletionRunLatchAndAnimationEndWaitBehavior()
        {
            CharacterActionDefinition directionalDefinition = CreateDefinitionWithBuiltBranch(DodgeActionVariant.Directional);
            CharacterActionDefinition backstepDefinition = CreateDefinitionWithBuiltBranch(DodgeActionVariant.Backstep);
            ActionLifecycleFrame directionalFrame = TickFrame(
                directionalDefinition,
                CharacterStateVariant.Directional,
                ActionAnimationKeys.DodgeDirectional);
            ActionLifecycleFrame backstepFrame = TickFrame(
                backstepDefinition,
                CharacterStateVariant.Backstep,
                ActionAnimationKeys.DodgeBackstep);
            ActionMotionResolveResult directionalCompleted = new ActionMotionResolveResult(
                directionalFrame.MotionSpec,
                default,
                false,
                true,
                directionalFrame.MotionSpec.SetRunLatchOnComplete,
                10,
                "complete");
            ActionMotionResolveResult backstepCompleted = new ActionMotionResolveResult(
                backstepFrame.MotionSpec,
                default,
                false,
                true,
                backstepFrame.MotionSpec.SetRunLatchOnComplete,
                11,
                "complete");

            Assert.True(directionalFrame.MotionSpec.SetRunLatchOnComplete);
            Assert.False(backstepFrame.MotionSpec.SetRunLatchOnComplete);
            Assert.False(RequiresAnimationEnd(in directionalFrame, in directionalCompleted));
            Assert.True(RequiresAnimationEnd(in backstepFrame, in backstepCompleted));
        }

        static CharacterActionDefinition CreateDodgeDefinition()
        {
            return new CharacterActionDefinition(
                ActionStateIds.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                CharacterStateIds.Dodge,
                33,
                44,
                new DodgeActionVariantDefinition(
                    DodgeActionVariant.Directional,
                    0.42f,
                    5.5f,
                    true,
                    ActionAnimationKeys.DodgeDirectional),
                new DodgeActionVariantDefinition(
                    DodgeActionVariant.Backstep,
                    0.61f,
                    2.75f,
                    false,
                    ActionAnimationKeys.DodgeBackstep));
        }

        static CharacterActionDefinition CreateDefinitionWithBuiltBranch(DodgeActionVariant variant)
        {
            CharacterActionDefinition definition = CreateDodgeDefinition();
            Assert.True(DodgeActionBranchTimelineBuilder.TryBuild(
                in definition,
                variant,
                0.02f,
                out ActionBranchDefinition branch));

            return new CharacterActionDefinition(
                ActionStateIds.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                CharacterStateIds.Dodge,
                definition.Priority,
                definition.Resistance,
                definition.DirectionalDodge,
                definition.BackstepDodge,
                branch);
        }

        static ActionLifecycleFrame TickFrame(
            CharacterActionDefinition definition,
            CharacterStateVariant variant,
            ActionAnimationKey animationKey)
        {
            FullBodyActionRuntimeModule module = new FullBodyActionRuntimeModule();
            CharacterActionCatalog catalog = new CharacterActionCatalog(new[] { definition });
            CharacterResolvedAction action = CreateResolvedAction(variant, animationKey);
            return module.TickActionLifecycle(in action, in catalog, 0.02f, 10);
        }

        static CharacterResolvedAction CreateResolvedAction(
            CharacterStateVariant variant,
            ActionAnimationKey animationKey)
        {
            CharacterActionRequest request = new CharacterActionRequest(
                CharacterFrameRequestProviderId.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                10,
                14,
                33,
                0,
                variant,
                Vector3.forward);
            CharacterInputRequestFact requestFact = new CharacterInputRequestFact(
                true,
                InputRequestKind.Dodge,
                10,
                14,
                33,
                variant,
                Vector3.forward);
            ActionInterruptRequest interruptRequest = new ActionInterruptRequest(
                10,
                ActionRequestType.Dodge,
                ActionStateIds.Dodge,
                33,
                0,
                10,
                14);
            ActionMotionSpec motionSpec = new ActionMotionSpec(
                ActionStateIds.Dodge,
                CharacterStateIds.Dodge,
                variant,
                0f,
                0f,
                false,
                variant == CharacterStateVariant.Directional,
                Vector3.forward,
                0f,
                10);

            return new CharacterResolvedAction(
                CharacterFrameRequestProviderId.Dodge,
                request,
                requestFact,
                interruptRequest,
                new ActionInterruptContext(ActionStateIds.None, 0f, 0, 10),
                animationKey,
                motionSpec);
        }

        static bool RequiresAnimationEnd(
            in ActionLifecycleFrame frame,
            in ActionMotionResolveResult result)
        {
            MethodInfo method = typeof(FullBodyActionFrameSubmitter).GetMethod(
                "RequiresActionAnimationEndBeforeLifecycleExit",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);
            object[] args = { frame, result, false };
            return (bool)method.Invoke(null, args);
        }
    }
}
