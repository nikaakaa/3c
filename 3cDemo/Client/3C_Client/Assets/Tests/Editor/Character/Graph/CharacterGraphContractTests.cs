using NUnit.Framework;
using System;
using ThirdPersonAction;
using ThirdPersonCharacterGraph;
using ThirdPersonCharacterStateMachine;
using UnityEngine;

namespace Tests.Editor.Character.Graph
{
    public sealed class CharacterGraphContractTests
    {
        [Test]
        public void EmptyCharacterGraphHasTypedEmptyBranches()
        {
            CharacterGraphDefinition graph = CharacterGraphDefinition.Empty;

            Assert.False(graph.HasAnyBranch);
            Assert.False(graph.Locomotion.IsDefined);
            Assert.False(graph.Action.IsDefined);
            Assert.False(graph.UpperBody.IsDefined);
            Assert.False(graph.Cue.IsDefined);
        }

        [Test]
        public void CharacterGraphCarriesFormalBranchDefinitions()
        {
            ActionTimelineDefinition timeline = new ActionTimelineDefinition(
                ActionStateIds.Dodge,
                12,
                new[] { new ActionTimelineTrackDefinition(ActionTimelineTrackKind.Cue, System.Array.Empty<ActionTimelineClipDefinition>()) });
            ActionBranchDefinition action = ActionBranchDefinition.Define(
                "action",
                ActionStateIds.Dodge,
                ActionNodeDefinition.Timeline("dodge-timeline", timeline),
                BodyOccupancyClaim.FullBodyAction(7));
            CharacterGraphDefinition graph = new CharacterGraphDefinition(
                LocomotionBranchDefinition.Define("locomotion"),
                action,
                UpperBodyBranchDefinition.Define("upper-body", false),
                CueBranchDefinition.Define("cue", false));

            Assert.True(graph.HasAnyBranch);
            Assert.True(graph.Locomotion.CanEvaluate);
            Assert.True(graph.Action.CanEvaluate);
            Assert.False(graph.UpperBody.CanEvaluate);
            Assert.False(graph.Cue.CanEvaluate);
        }

        [Test]
        public void BranchDefinitionsRoundTripThroughSerializedForm()
        {
            CharacterGraphBranchDefinition branch =
                CharacterGraphBranchDefinition.Define(CharacterGraphBranchKind.Locomotion, "locomotion", false);
            string json = JsonUtility.ToJson(branch.ToSerializedForm());
            CharacterGraphBranchSerializedForm form =
                JsonUtility.FromJson<CharacterGraphBranchSerializedForm>(json);
            LocomotionBranchDefinition restored = new LocomotionBranchDefinition(form.ToDefinition());

            Assert.True(restored.IsDefined);
            Assert.False(restored.CanEvaluate);
            Assert.AreEqual("locomotion", restored.Branch.BranchId.Value);
        }

        [Test]
        public void ExecutionNodeTreeAllowsSingleParentParallelBranches()
        {
            CharacterExecutionNodeId parallelId = new CharacterExecutionNodeId("parallel");
            CharacterExecutionNodeId locomotionId = new CharacterExecutionNodeId("locomotion");
            CharacterExecutionNodeId actionId = new CharacterExecutionNodeId("action");
            CharacterExecutionNodeTree tree = new CharacterExecutionNodeTree(
                new CharacterExecutionNodeId("root"),
                new[]
                {
                    CharacterExecutionNodeDefinition.Root("root", parallelId),
                    CharacterExecutionNodeDefinition.Parallel("parallel", locomotionId, actionId),
                    CharacterExecutionNodeDefinition.Branch("locomotion", CharacterGraphBranchKind.Locomotion),
                    CharacterExecutionNodeDefinition.Branch("action", CharacterGraphBranchKind.Action)
                });

            CharacterExecutionNodeTreeValidationResult result = CharacterExecutionNodeTreeValidator.Validate(tree);

            Assert.False(result.HasErrors, string.Join(",", result.Errors));
        }

        [Test]
        public void EmptyExecutionNodeTreeAndGraphStateAreValidEmptyContracts()
        {
            CharacterExecutionNodeTreeValidationResult result =
                CharacterExecutionNodeTreeValidator.Validate(CharacterExecutionNodeTree.Empty);
            CharacterGraphState state = CharacterGraphState.Empty;

            Assert.False(result.HasErrors);
            Assert.AreEqual(0, state.NodeStates.Count);
        }

        [Test]
        public void ExecutionNodeEvaluationCarriesInputDownAndOutputUp()
        {
            CharacterExecutionNodeDefinition node =
                CharacterExecutionNodeDefinition.Branch("locomotion", CharacterGraphBranchKind.Locomotion);
            CharacterGraphInput graphInput = new CharacterGraphInput(
                12,
                0.02f,
                CharacterRuntimeBlackboardSnapshot.Empty);
            CharacterExecutionNodeEvaluationInput input = new CharacterExecutionNodeEvaluationInput(
                node,
                graphInput,
                CharacterGraphState.Empty);
            ICharacterExecutionNodeEvaluator evaluator = new EchoLocomotionEvaluator();

            CharacterExecutionNodeEvaluationResult result = evaluator.Evaluate(in input);

            Assert.AreEqual(12, result.SourceStep);
            Assert.True(result.FrameResult.LocomotionCandidate.HasAnyCandidate);
            Assert.False(result.HasForeignStateWrites);
        }

        [Test]
        public void ExecutionNodeStateWritesMustBelongToOwnerNode()
        {
            CharacterExecutionNodeId owner = new CharacterExecutionNodeId("owner");
            CharacterExecutionNodeId foreign = new CharacterExecutionNodeId("foreign");
            CharacterExecutionNodeEvaluationResult result = new CharacterExecutionNodeEvaluationResult(
                CharacterGraphFrameResult.Empty(9),
                new[]
                {
                    new CharacterExecutionNodeStateWrite(owner, new CharacterGraphNodeState(owner, 9)),
                    new CharacterExecutionNodeStateWrite(owner, new CharacterGraphNodeState(foreign, 9))
                },
                Array.Empty<string>(),
                9);

            Assert.True(result.HasForeignStateWrites);
        }

        [Test]
        public void ExecutionNodeTreeRejectsSharedRuntimeNode()
        {
            CharacterExecutionNodeId childId = new CharacterExecutionNodeId("shared-action");
            CharacterExecutionNodeId leftId = new CharacterExecutionNodeId("left");
            CharacterExecutionNodeId rightId = new CharacterExecutionNodeId("right");
            CharacterExecutionNodeTree tree = new CharacterExecutionNodeTree(
                new CharacterExecutionNodeId("root"),
                new[]
                {
                    CharacterExecutionNodeDefinition.Root("root", leftId, rightId),
                    CharacterExecutionNodeDefinition.Parallel("left", childId),
                    CharacterExecutionNodeDefinition.Parallel("right", childId),
                    CharacterExecutionNodeDefinition.Branch("shared-action", CharacterGraphBranchKind.Action)
                });

            CharacterExecutionNodeTreeValidationResult result = CharacterExecutionNodeTreeValidator.Validate(tree);

            Assert.True(result.HasErrors);
            CollectionAssert.Contains(result.Errors, "node-multiple-parents:shared-action");
        }

        [Test]
        public void GraphFrameResultBuildsPipelineArbitrationInput()
        {
            ActionBranchOutcome action = new ActionBranchOutcome(
                ActionTimelineOutcome.None(0, 9),
                CharacterFrameCandidateOutput.FullBodyAction(true, true, 9),
                BodyOccupancyClaim.FullBodyAction(9),
                9);
            CharacterGraphFrameResult result = new CharacterGraphFrameResult(
                CharacterFrameCandidateOutput.Locomotion(true, true, 9),
                action,
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.UpperBody, 9),
                CueOutcome.None(9),
                action.BodyClaim,
                System.Array.Empty<string>(),
                9);

            CharacterFrameArbitrationInput input = result.ToArbitrationInput();

            Assert.True(input.OccupancyClaim.ClaimsFullBody);
            Assert.True(input.LocomotionCandidate.HasAnyCandidate);
            Assert.True(input.FullBodyActionCandidate.HasAnyCandidate);
            Assert.False(input.UpperBodyCandidate.HasAnyCandidate);
        }

        [Test]
        public void UnimplementedBranchesReturnEmptyCandidatesWithDiagnostics()
        {
            LocomotionBranchOutcome locomotion = LocomotionBranchOutcome.Empty(3);
            UpperBodyBranchOutcome upperBody = UpperBodyBranchOutcome.Empty(3);
            CueBranchOutcome cue = CueBranchOutcome.Empty(3);

            Assert.False(locomotion.HasOutput);
            Assert.False(upperBody.HasOutput);
            Assert.False(cue.HasOutput);
            CollectionAssert.Contains(locomotion.Diagnostics.Messages, "branch-unimplemented:Locomotion");
            CollectionAssert.Contains(upperBody.Diagnostics.Messages, "branch-unimplemented:UpperBody");
            CollectionAssert.Contains(cue.Diagnostics.Messages, "branch-unimplemented:Cue");
        }

        [Test]
        public void BranchClaimDescriptorsExpressChannelsNotGameplayOwners()
        {
            CharacterBranchClaimDescriptor fullBody = CharacterBranchClaimDescriptor.FullBodyAction(5);
            CharacterBranchClaimDescriptor upperBody = CharacterBranchClaimDescriptor.UpperBody(5);
            CharacterBranchClaimDescriptor lowerBody = CharacterBranchClaimDescriptor.LowerBodyLocomotion(5);

            Assert.AreEqual(CharacterGraphBranchKind.Action, fullBody.BranchKind);
            Assert.AreEqual(CharacterBranchClaimChannel.FullBody, fullBody.Channel);
            Assert.AreEqual(CharacterGraphBranchKind.UpperBody, upperBody.BranchKind);
            Assert.AreEqual(CharacterBranchClaimChannel.UpperBody, upperBody.Channel);
            Assert.AreEqual(CharacterGraphBranchKind.Locomotion, lowerBody.BranchKind);
            Assert.AreEqual(CharacterBranchClaimChannel.LowerBody, lowerBody.Channel);
        }

        sealed class EchoLocomotionEvaluator : ICharacterExecutionNodeEvaluator
        {
            public CharacterExecutionNodeEvaluationResult Evaluate(in CharacterExecutionNodeEvaluationInput input)
            {
                CharacterGraphFrameResult frameResult = new CharacterGraphFrameResult(
                    CharacterFrameCandidateOutput.Locomotion(true, false, input.SourceStep),
                    ActionBranchOutcome.None(input.SourceStep),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.UpperBody, input.SourceStep),
                CueOutcome.None(input.SourceStep),
                BodyOccupancyClaim.None(input.SourceStep),
                Array.Empty<string>(),
                input.SourceStep);
            CharacterExecutionNodeStateWrite stateWrite = new CharacterExecutionNodeStateWrite(
                    input.Node.Id,
                    new CharacterGraphNodeState(input.Node.Id, input.SourceStep));

                return new CharacterExecutionNodeEvaluationResult(
                    frameResult,
                    new[] { stateWrite },
                    Array.Empty<string>(),
                    input.SourceStep);
            }
        }
    }
}
