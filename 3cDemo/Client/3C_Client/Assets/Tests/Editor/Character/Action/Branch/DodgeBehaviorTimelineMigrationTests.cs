using System.IO;
using System.Text;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor.Character.Action.Branch
{
    public sealed class DodgeBehaviorTimelineMigrationTests
    {
        const string DodgeAssetPath = "Assets/Configs/3C/Action/Corin/Actions/Dodge/CorinDodgeActionDefinition.asset";

        [Test]
        public void CorinDodgeActionCompilesToSelectorWithDirectionalAndBackstepTimelines()
        {
            CharacterActionDefinitionSO asset = AssetDatabase.LoadAssetAtPath<CharacterActionDefinitionSO>(DodgeAssetPath);
            ActionTimelineCompileContext compileContext = CompileContext();

            Assert.NotNull(asset);
            CharacterActionCatalogValidationResult validation = asset.Validate(in compileContext);
            Assert.False(validation.HasErrors, validation.DescribeErrors());
            CharacterActionDefinition definition = asset.ToDefinition(in compileContext);

            Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));
            Assert.AreEqual(CommittedActionNodeKind.Root, branch.RootNode.Kind);
            Assert.AreEqual("branch.root.action.dodge", branch.RootNode.NodeId.Value);
            Assert.AreEqual("selector.dodge", branch.RootNode.ChildIds[0].Value);
            Assert.True(branch.TryGetNode(new CommittedActionNodeId("selector.dodge"), out CommittedActionNodeDefinition selector));
            Assert.AreEqual(CommittedActionNodeKind.Selector, selector.Kind);
            Assert.True(branch.TryGetNode(new CommittedActionNodeId("timeline.dodge.directional"), out CommittedActionNodeDefinition directional));
            Assert.True(branch.TryGetNode(new CommittedActionNodeId("timeline.dodge.backstep"), out CommittedActionNodeDefinition backstep));
            Assert.AreEqual(21, directional.TimelineNode.Timeline.DurationTicks);
            Assert.AreEqual(21, backstep.TimelineNode.Timeline.DurationTicks);
        }

        [Test]
        public void DirectionalDodgeRuntimeContentComesFromSelectedTimeline()
        {
            CharacterActionDefinition definition = LoadDodgeDefinition();
            Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));

            CommittedActionBranchOutcome outcome = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(
                    branch,
                    0,
                    31,
                    Context(CharacterStateVariant.Directional, Vector3.forward)));

            Assert.True(outcome.HasOutcome);
            Assert.AreEqual("timeline.dodge.directional", outcome.SelectedNodeId.Value);
            Assert.AreEqual(ActionAnimationKeys.DodgeDirectional, outcome.TimelineOutcome.AnimationKey);
            Assert.True(outcome.TimelineOutcome.HasMotion);
            Assert.AreEqual(0.35f, outcome.TimelineOutcome.MotionSpec.Duration, 0.0001f);
            Assert.AreEqual(4f, outcome.TimelineOutcome.MotionSpec.Distance, 0.0001f);
            Assert.True(outcome.TimelineOutcome.MotionSpec.RotateToDirection);
            Assert.True(outcome.TimelineOutcome.MotionSpec.SetRunLatchOnComplete);
        }

        [Test]
        public void BackstepDodgeRuntimeContentComesFromSelectedTimeline()
        {
            CharacterActionDefinition definition = LoadDodgeDefinition();
            Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));

            CommittedActionBranchOutcome outcome = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(
                    branch,
                    0,
                    32,
                    Context(CharacterStateVariant.Backstep, Vector3.back)));

            Assert.True(outcome.HasOutcome);
            Assert.AreEqual("timeline.dodge.backstep", outcome.SelectedNodeId.Value);
            Assert.AreEqual(ActionAnimationKeys.DodgeBackstep, outcome.TimelineOutcome.AnimationKey);
            Assert.True(outcome.TimelineOutcome.HasMotion);
            Assert.AreEqual(0.35f, outcome.TimelineOutcome.MotionSpec.Duration, 0.0001f);
            Assert.AreEqual(3f, outcome.TimelineOutcome.MotionSpec.Distance, 0.0001f);
            Assert.False(outcome.TimelineOutcome.MotionSpec.RotateToDirection);
            Assert.False(outcome.TimelineOutcome.MotionSpec.SetRunLatchOnComplete);
        }

        [Test]
        public void DodgeResolverNoLongerReadsOldVariantRuntimeAuthority()
        {
            string source = File.ReadAllText(
                Path.Combine(Application.dataPath, "Scripts/Character/Action/Solver/CharacterActionRequestResolution.cs"),
                Encoding.UTF8);

            Assert.That(source, Does.Not.Contain("TryGetDodgeVariant"));
            Assert.That(source, Does.Not.Contain("ResolveDuration"));
            Assert.That(source, Does.Not.Contain("ResolveDistance"));
            Assert.That(source, Does.Not.Contain("ShouldRotateToDirection"));
            Assert.That(source, Does.Not.Contain("variantDefinition"));
        }

        [Test]
        public void DodgeTimelineRuntimeKeepsUnifiedPipelineBoundary()
        {
            string root = Path.Combine(Application.dataPath, "Scripts/Character/Action");
            string source = File.ReadAllText(Path.Combine(root, "Solver/CharacterActionRequestResolution.cs"), Encoding.UTF8) +
                            File.ReadAllText(Path.Combine(root, "Branch/Solver/CommittedActionBranchEvaluator.cs"), Encoding.UTF8) +
                            File.ReadAllText(Path.Combine(root, "Timeline/Model/ActionTimelineDefinition.cs"), Encoding.UTF8);

            Assert.That(source, Does.Not.Contain("new CharacterFramePipeline"));
            Assert.That(source, Does.Not.Contain("IActionMovementExecutor"));
            Assert.That(source, Does.Not.Contain("ICharacterAnimationOutputPresenter"));
            Assert.That(source, Does.Not.Contain("Transform"));
        }

        static CharacterActionDefinition LoadDodgeDefinition()
        {
            CharacterActionDefinitionSO asset = AssetDatabase.LoadAssetAtPath<CharacterActionDefinitionSO>(DodgeAssetPath);
            Assert.NotNull(asset);
            ActionTimelineCompileContext compileContext = CompileContext();
            return asset.ToDefinition(in compileContext);
        }

        static ActionTimelineCompileContext CompileContext()
        {
            return new ActionTimelineCompileContext(1f / 60f);
        }

        static CommittedActionBranchEvaluationContext Context(CharacterStateVariant variant, Vector3 direction)
        {
            CharacterInputRequestFact request = new CharacterInputRequestFact(
                true,
                InputRequestKind.Dodge,
                1,
                4,
                30,
                variant,
                direction);
            return new CommittedActionBranchEvaluationContext(1, default, request, default, default);
        }
    }
}
