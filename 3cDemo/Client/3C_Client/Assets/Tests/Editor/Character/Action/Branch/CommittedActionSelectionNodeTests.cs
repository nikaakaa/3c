using System.IO;
using System.Text;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace Tests.Editor.Character.Action.Branch
{
    public sealed class CommittedActionSelectionNodeTests
    {
        [Test]
        public void SingleTimelineRootRemainsCompatible()
        {
            ActionTimelineDefinition timeline = Timeline(ActionAnimationKeys.DodgeDirectional, "cue.directional");
            CommittedActionBranchDefinition branch = CommittedActionBranchDefinition.Define(
                "dodge",
                ActionStateIds.Dodge,
                CommittedActionNodeDefinition.Timeline("timeline.directional", timeline),
                BodyOccupancyClaim.CommittedActionFullBody(10));

            CommittedActionBranchOutcome outcome = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(branch, 0, 10));

            Assert.True(outcome.HasOutcome);
            Assert.AreEqual("timeline.directional", outcome.SelectedNodeId.Value);
            Assert.AreEqual("cue.directional", outcome.TimelineOutcome.CueRequests[0].CueId);
        }

        [Test]
        public void ActionVariantConditionSelectsDirectionalTimeline()
        {
            CommittedActionBranchDefinition branch = SelectorBranch();
            CommittedActionBranchEvaluationContext context = Context(CharacterStateVariant.Directional);

            CommittedActionBranchOutcome outcome = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(branch, 0, 11, context));

            Assert.True(outcome.HasOutcome);
            Assert.AreEqual("timeline.directional", outcome.SelectedNodeId.Value);
            Assert.AreEqual(ActionAnimationKeys.DodgeDirectional, outcome.TimelineOutcome.AnimationKey);
            Assert.AreEqual("cue.directional", outcome.TimelineOutcome.CueRequests[0].CueId);
        }

        [Test]
        public void ActionVariantConditionSelectsBackstepTimeline()
        {
            CommittedActionBranchDefinition branch = SelectorBranch();
            CommittedActionBranchEvaluationContext context = Context(CharacterStateVariant.Backstep);

            CommittedActionBranchOutcome outcome = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(branch, 0, 12, context));

            Assert.True(outcome.HasOutcome);
            Assert.AreEqual("timeline.backstep", outcome.SelectedNodeId.Value);
            Assert.AreEqual(ActionAnimationKeys.DodgeBackstep, outcome.TimelineOutcome.AnimationKey);
            Assert.AreEqual("cue.backstep", outcome.TimelineOutcome.CueRequests[0].CueId);
        }

        [Test]
        public void FirstPassingChildWins()
        {
            CommittedActionNodeDefinition first = CommittedActionNodeDefinition.ConditionNode(
                "condition.first",
                CommittedActionConditionDefinition.HasMoveIntent(true),
                new CommittedActionNodeId("timeline.first"));
            CommittedActionNodeDefinition second = CommittedActionNodeDefinition.ConditionNode(
                "condition.second",
                CommittedActionConditionDefinition.HasMoveIntent(true),
                new CommittedActionNodeId("timeline.second"));
            CommittedActionBranchDefinition branch = CommittedActionBranchDefinition.Define(
                "dodge",
                ActionStateIds.Dodge,
                CommittedActionNodeDefinition.Selector(
                    "selector.dodge",
                    first.NodeId,
                    second.NodeId),
                BodyOccupancyClaim.CommittedActionFullBody(13),
                new[]
                {
                    first,
                    second,
                    CommittedActionNodeDefinition.Timeline(
                        "timeline.first",
                        Timeline(ActionAnimationKeys.DodgeDirectional, "cue.first")),
                    CommittedActionNodeDefinition.Timeline(
                        "timeline.second",
                        Timeline(ActionAnimationKeys.DodgeBackstep, "cue.second"))
                });
            CommittedActionBranchEvaluationContext context = new CommittedActionBranchEvaluationContext(
                13,
                default,
                CharacterInputRequestFact.None(InputRequestKind.Dodge),
                new MovementInputIntent(Vector2.up, Vector2.up, 1f, true),
                default);

            CommittedActionBranchOutcome outcome = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(branch, 0, 13, context));

            Assert.True(outcome.HasOutcome);
            Assert.AreEqual("timeline.first", outcome.SelectedNodeId.Value);
            Assert.AreEqual("cue.first", outcome.TimelineOutcome.CueRequests[0].CueId);
        }

        [Test]
        public void SelectorWithoutPassingChildReportsDiagnosticWithoutFallback()
        {
            CommittedActionBranchDefinition branch = SelectorBranch();
            CommittedActionBranchEvaluationContext context = Context(CharacterStateVariant.None);

            CommittedActionBranchOutcome outcome = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(branch, 0, 14, context));

            Assert.False(outcome.HasOutcome);
            Assert.True(outcome.HasDiagnostic);
            Assert.AreEqual("committed-action-selector-no-match:selector.dodge", outcome.Diagnostic);
            Assert.False(outcome.TimelineOutcome.HasCue);
        }

        [Test]
        public void UnselectedTimelineDoesNotEmitCueOrAnimation()
        {
            CommittedActionBranchDefinition branch = SelectorBranch();
            CommittedActionBranchEvaluationContext context = Context(CharacterStateVariant.Directional);

            CommittedActionBranchOutcome outcome = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(branch, 0, 15, context));

            Assert.True(outcome.TimelineOutcome.HasCue);
            Assert.AreEqual(1, outcome.TimelineOutcome.CueRequests.Count);
            Assert.AreEqual("cue.directional", outcome.TimelineOutcome.CueRequests[0].CueId);
            Assert.AreNotEqual(ActionAnimationKeys.DodgeBackstep, outcome.TimelineOutcome.AnimationKey);
        }

        [Test]
        public void ConditionEvaluatorIsPureTrueFalse()
        {
            CommittedActionBranchEvaluationContext directional = Context(CharacterStateVariant.Directional);
            CommittedActionBranchEvaluationContext backstep = Context(CharacterStateVariant.Backstep);

            Assert.True(CommittedActionConditionEvaluator.Evaluate(
                CommittedActionConditionDefinition.ActionVariant(CharacterStateVariant.Directional),
                in directional));
            Assert.False(CommittedActionConditionEvaluator.Evaluate(
                CommittedActionConditionDefinition.ActionVariant(CharacterStateVariant.Directional),
                in backstep));
        }

        [Test]
        public void ConditionEvaluatorSupportsFormalFactKinds()
        {
            TimelineFactId cancelFact = TimelineFactIds.CancelableToDodge;
            CommittedActionBranchEvaluationContext context = FormalContext(
                20,
                new CommittedActionRequestFactSet(new[]
                {
                    CommittedActionRequestFact.HeldFact(InputRequestKind.Dodge, 20),
                    CommittedActionRequestFact.ReleasedFact(InputRequestKind.TurnBack, 20)
                }),
                new ActionFactSet(new[] { cancelFact }),
                new CommittedActionNodeId("timeline.active"),
                12,
                12,
                CharacterStateVariant.Backstep);

            Assert.True(CommittedActionConditionEvaluator.Evaluate(CommittedActionConditionDefinition.Always(), in context));
            Assert.True(CommittedActionConditionEvaluator.Evaluate(CommittedActionConditionDefinition.RequestHeld(InputRequestKind.Dodge), in context));
            Assert.True(CommittedActionConditionEvaluator.Evaluate(CommittedActionConditionDefinition.RequestReleased(InputRequestKind.TurnBack), in context));
            Assert.True(CommittedActionConditionEvaluator.Evaluate(CommittedActionConditionDefinition.RequiredFactActive(cancelFact.Value), in context));
            Assert.True(CommittedActionConditionEvaluator.Evaluate(CommittedActionConditionDefinition.TimelineComplete(), in context));
            Assert.True(CommittedActionConditionEvaluator.Evaluate(CommittedActionConditionDefinition.ActionVariant(CharacterStateVariant.Backstep), in context));
            Assert.False(CommittedActionConditionEvaluator.Evaluate(CommittedActionConditionDefinition.ActionVariant(CharacterStateVariant.Directional), in context));
        }

        [Test]
        public void RequestReleaseSuppressesHeldOnlyOnReleaseTick()
        {
            CommittedActionRequestFactSet facts = new CommittedActionRequestFactSet(new[]
            {
                CommittedActionRequestFact.HeldFact(InputRequestKind.Dodge, 30),
                CommittedActionRequestFact.ReleasedFact(InputRequestKind.Dodge, 30)
            });
            CommittedActionBranchEvaluationContext releaseTick = FormalContext(30, facts);
            CommittedActionBranchEvaluationContext nextTick = FormalContext(31, facts);

            Assert.False(CommittedActionConditionEvaluator.Evaluate(
                CommittedActionConditionDefinition.RequestHeld(InputRequestKind.Dodge),
                in releaseTick));
            Assert.True(CommittedActionConditionEvaluator.Evaluate(
                CommittedActionConditionDefinition.RequestReleased(InputRequestKind.Dodge),
                in releaseTick));
            Assert.True(CommittedActionConditionEvaluator.Evaluate(
                CommittedActionConditionDefinition.RequestHeld(InputRequestKind.Dodge),
                in nextTick));
            Assert.False(CommittedActionConditionEvaluator.Evaluate(
                CommittedActionConditionDefinition.RequestReleased(InputRequestKind.Dodge),
                in nextTick));
        }

        [Test]
        public void TimelineCompleteChildSelectsNextTimelineFromTickZero()
        {
            ActionTimelineDefinition start = Timeline(ActionAnimationKeys.DodgeDirectional, "cue.start");
            ActionTimelineDefinition loop = Timeline(ActionAnimationKeys.DodgeBackstep, "cue.loop");
            CommittedActionNodeDefinition startNode = CommittedActionNodeDefinition.Timeline(
                "timeline.start",
                start,
                new CommittedActionNodeId("condition.complete"));
            CommittedActionNodeDefinition complete = CommittedActionNodeDefinition.ConditionNode(
                "condition.complete",
                CommittedActionConditionDefinition.TimelineComplete(),
                new CommittedActionNodeId("timeline.loop"));
            CommittedActionBranchDefinition branch = CommittedActionBranchDefinition.Define(
                "test.hold",
                ActionStateIds.Dodge,
                startNode,
                BodyOccupancyClaim.CommittedActionFullBody(40),
                new[]
                {
                    complete,
                    CommittedActionNodeDefinition.Timeline("timeline.loop", loop)
                });
            CommittedActionBranchEvaluationContext context = FormalContext(
                40,
                CommittedActionRequestFactSet.Empty,
                ActionFactSet.Empty,
                new CommittedActionNodeId("timeline.start"),
                12,
                12,
                CharacterStateVariant.None);

            CommittedActionBranchOutcome outcome = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(branch, 12, 40, context));

            Assert.True(outcome.HasOutcome);
            Assert.AreEqual("timeline.loop", outcome.SelectedNodeId.Value);
            Assert.AreEqual("cue.loop", outcome.TimelineOutcome.CueRequests[0].CueId);
            Assert.AreEqual(0, outcome.TimelineOutcome.LocalTick);
        }

        [Test]
        public void SelectionRuntimeKeepsStaticBoundary()
        {
            string root = Path.Combine(Application.dataPath, "Scripts/Character/Action/Branch");
            string source = File.ReadAllText(Path.Combine(root, "Solver/CommittedActionBranchEvaluator.cs"), Encoding.UTF8) +
                            File.ReadAllText(Path.Combine(root, "Model/CommittedActionBranchDefinition.cs"), Encoding.UTF8) +
                            File.ReadAllText(Path.Combine(root, "Model/CommittedActionBranchOutcome.cs"), Encoding.UTF8);

            Assert.That(source, Does.Not.Contain("MonoBehaviour"));
            Assert.That(source, Does.Not.Contain("Transform"));
            Assert.That(source, Does.Not.Contain("Animator"));
            Assert.That(source, Does.Not.Contain("InputAction"));
            Assert.That(source, Does.Not.Contain("GraphView"));
            Assert.That(source, Does.Contain("CharacterRuntimeBlackboardSnapshot"));
            Assert.That(source, Does.Not.Contain("new CharacterRuntimeBlackboard"));
            Assert.That(source, Does.Not.Contain("WriteBlackboard"));
        }

        static CommittedActionBranchDefinition SelectorBranch()
        {
            CommittedActionNodeDefinition directionalCondition = CommittedActionNodeDefinition.ConditionNode(
                "condition.directional",
                CommittedActionConditionDefinition.ActionVariant(CharacterStateVariant.Directional),
                new CommittedActionNodeId("timeline.directional"));
            CommittedActionNodeDefinition backstepCondition = CommittedActionNodeDefinition.ConditionNode(
                "condition.backstep",
                CommittedActionConditionDefinition.ActionVariant(CharacterStateVariant.Backstep),
                new CommittedActionNodeId("timeline.backstep"));

            return CommittedActionBranchDefinition.Define(
                "dodge",
                ActionStateIds.Dodge,
                CommittedActionNodeDefinition.Selector(
                    "selector.dodge",
                    directionalCondition.NodeId,
                    backstepCondition.NodeId),
                BodyOccupancyClaim.CommittedActionFullBody(1),
                new[]
                {
                    directionalCondition,
                    backstepCondition,
                    CommittedActionNodeDefinition.Timeline(
                        "timeline.directional",
                        Timeline(ActionAnimationKeys.DodgeDirectional, "cue.directional")),
                    CommittedActionNodeDefinition.Timeline(
                        "timeline.backstep",
                        Timeline(ActionAnimationKeys.DodgeBackstep, "cue.backstep"))
                });
        }

        static CommittedActionBranchEvaluationContext Context(CharacterStateVariant variant)
        {
            CharacterInputRequestFact request = new CharacterInputRequestFact(
                variant != CharacterStateVariant.None,
                InputRequestKind.Dodge,
                1,
                3,
                10,
                variant,
                variant == CharacterStateVariant.Backstep ? Vector3.back : Vector3.forward);
            return new CommittedActionBranchEvaluationContext(1, default, request, default, default);
        }

        static CommittedActionBranchEvaluationContext FormalContext(
            int sourceStep,
            CommittedActionRequestFactSet requestFacts = default,
            ActionFactSet activeFacts = default,
            CommittedActionNodeId activeTimelineNodeId = default,
            int actionLocalTick = 0,
            int runtimeTimelineDurationTicks = 0,
            CharacterStateVariant variant = CharacterStateVariant.None)
        {
            CharacterInputRequestFact request = new CharacterInputRequestFact(
                variant != CharacterStateVariant.None,
                InputRequestKind.Dodge,
                sourceStep,
                sourceStep + 2,
                10,
                variant,
                Vector3.forward);
            return new CommittedActionBranchEvaluationContext(
                sourceStep,
                default,
                request,
                default,
                default,
                CharacterRuntimeBlackboardSnapshot.Empty,
                activeTimelineNodeId,
                actionLocalTick,
                runtimeTimelineDurationTicks,
                requestFacts.Facts == null ? CommittedActionRequestFactSet.Empty : requestFacts,
                activeFacts.ActiveFacts == null ? ActionFactSet.Empty : activeFacts,
                StateTimelineWindowFacts.None(default),
                1);
        }

        static ActionTimelineDefinition Timeline(ActionAnimationKey key, string cueId)
        {
            return new ActionTimelineDefinition(
                ActionStateIds.Dodge,
                12,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.AnimationKey,
                                0,
                                12,
                                ActionTimelineClipPayload.Animation(key))
                        }),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Motion,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.Motion,
                                0,
                                12,
                                ActionTimelineClipPayload.Motion(MotionSpec(key)))
                        }),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Cue,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.Cue,
                                0,
                                0,
                                ActionTimelineClipPayload.Cue(cueId))
                        })
                });
        }

        static ActionMotionSpec MotionSpec(ActionAnimationKey key)
        {
            bool backstep = key == ActionAnimationKeys.DodgeBackstep;
            return new ActionMotionSpec(
                ActionStateIds.Dodge,
                CharacterStateIds.Dodge,
                backstep ? CharacterStateVariant.Backstep : CharacterStateVariant.Directional,
                0.35f,
                backstep ? 2.5f : 4f,
                !backstep,
                false,
                backstep ? Vector3.back : Vector3.forward,
                0f,
                0);
        }
    }
}
