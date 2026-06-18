using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace Tests.Editor.Character.Action.Branch
{
    public sealed class CommittedActionBranchAuthoringTests
    {
        [Test]
        public void BranchAuthoringCompilesSelectorConditionTimelineOrder()
        {
            CommittedActionBranchAuthoring authoring = CreateMoveIntentSelectorBranch();
            CharacterActionCatalogValidationResult validation = new CharacterActionCatalogValidationResult();
            ActionTimelineCompileContext compileContext = CompileContext();

            authoring.ValidateInto(validation, "Test", ActionStateIds.Dodge, 7, in compileContext);
            CommittedActionBranchDefinition branch =
                authoring.ToCommittedActionBranchDefinition(ActionStateIds.Dodge, 7, in compileContext);
            CommittedActionBranchOutcome outcome = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(
                    branch,
                    0,
                    7,
                    MoveIntentContext()));

            Assert.False(validation.HasErrors, validation.DescribeErrors());
            Assert.True(branch.CanEvaluate);
            Assert.AreEqual(CommittedActionNodeKind.Root, branch.RootNode.Kind);
            Assert.AreEqual("branch.root.test", branch.RootNode.NodeId.Value);
            Assert.AreEqual("selector.test", branch.RootNode.ChildIds[0].Value);
            Assert.True(branch.TryGetNode(new CommittedActionNodeId("selector.test"), out CommittedActionNodeDefinition selector));
            Assert.AreEqual("condition.first", selector.ChildIds[0].Value);
            Assert.AreEqual("condition.second", selector.ChildIds[1].Value);
            Assert.True(outcome.HasOutcome);
            Assert.AreEqual("timeline.first", outcome.SelectedNodeId.Value);
            Assert.AreEqual("cue.first", outcome.TimelineOutcome.CueRequests[0].CueId);
        }

        [Test]
        public void BranchAuthoringValidatorReportsInvalidTopology()
        {
            ActionTimelineCompileContext compileContext = CompileContext();
            CharacterActionCatalogValidationResult missingRootValidation = new CharacterActionCatalogValidationResult();
            CharacterActionCatalogValidationResult duplicateValidation = new CharacterActionCatalogValidationResult();
            CharacterActionCatalogValidationResult danglingValidation = new CharacterActionCatalogValidationResult();
            CharacterActionCatalogValidationResult invalidConditionValidation = new CharacterActionCatalogValidationResult();
            CharacterActionCatalogValidationResult cycleValidation = new CharacterActionCatalogValidationResult();
            CharacterActionCatalogValidationResult emptyTimelineValidation = new CharacterActionCatalogValidationResult();

            CommittedActionBranchAuthoring missingRoot = new CommittedActionBranchAuthoring(
                1,
                true,
                "branch.missing-root",
                "selector.missing",
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion,
                new[]
                {
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.missing-root",
                        Timeline("timeline.missing-root", ActionAnimationKeys.DodgeDirectional, "cue.root"),
                        Vector2.zero)
                });
            CommittedActionBranchAuthoring duplicate = new CommittedActionBranchAuthoring(
                1,
                true,
                "branch.duplicate",
                "timeline.duplicate",
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion,
                new[]
                {
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.duplicate",
                        Timeline("timeline.duplicate", ActionAnimationKeys.DodgeDirectional, "cue.duplicate"),
                        Vector2.zero),
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.duplicate",
                        Timeline("timeline.duplicate.second", ActionAnimationKeys.DodgeBackstep, "cue.duplicate.second"),
                        Vector2.one)
                });
            CommittedActionBranchAuthoring dangling = new CommittedActionBranchAuthoring(
                1,
                true,
                "branch.dangling",
                "selector.dangling",
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion,
                new[]
                {
                    CommittedActionBranchNodeAuthoring.Selector(
                        "selector.dangling",
                        new[] { "timeline.missing" },
                        Vector2.zero)
                });
            CommittedActionBranchAuthoring invalidCondition = new CommittedActionBranchAuthoring(
                1,
                true,
                "branch.invalid-condition",
                "condition.invalid",
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion,
                new[]
                {
                    CommittedActionBranchNodeAuthoring.ConditionNode(
                        "condition.invalid",
                        new CommittedActionBranchConditionAuthoring(
                            CommittedActionConditionKind.ActionVariantEquals,
                            CharacterStateVariant.None,
                            false),
                        new[] { "timeline.invalid-condition" },
                        Vector2.zero),
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.invalid-condition",
                        Timeline("timeline.invalid-condition", ActionAnimationKeys.DodgeDirectional, "cue.invalid-condition"),
                        Vector2.one)
                });
            CommittedActionBranchAuthoring cycle = new CommittedActionBranchAuthoring(
                1,
                true,
                "branch.cycle",
                "selector.cycle",
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion,
                new[]
                {
                    CommittedActionBranchNodeAuthoring.Selector(
                        "selector.cycle",
                        new[] { "condition.cycle" },
                        Vector2.zero),
                    CommittedActionBranchNodeAuthoring.ConditionNode(
                        "condition.cycle",
                        new CommittedActionBranchConditionAuthoring(
                            CommittedActionConditionKind.HasMoveIntent,
                            CharacterStateVariant.None,
                            true),
                        new[] { "selector.cycle" },
                        Vector2.one)
                });
            CommittedActionBranchAuthoring emptyTimeline = new CommittedActionBranchAuthoring(
                1,
                true,
                "branch.empty",
                "timeline.empty",
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion,
                new[]
                {
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.empty",
                        default,
                        Vector2.zero)
                });

            missingRoot.ValidateInto(missingRootValidation, "MissingRoot", ActionStateIds.Dodge, 1, in compileContext);
            duplicate.ValidateInto(duplicateValidation, "Duplicate", ActionStateIds.Dodge, 1, in compileContext);
            dangling.ValidateInto(danglingValidation, "Dangling", ActionStateIds.Dodge, 1, in compileContext);
            invalidCondition.ValidateInto(invalidConditionValidation, "InvalidCondition", ActionStateIds.Dodge, 1, in compileContext);
            cycle.ValidateInto(cycleValidation, "Cycle", ActionStateIds.Dodge, 1, in compileContext);
            emptyTimeline.ValidateInto(emptyTimelineValidation, "Empty", ActionStateIds.Dodge, 1, in compileContext);

            Assert.That(missingRootValidation.Errors, Has.Some.Contains("root node is missing"));
            Assert.That(duplicateValidation.Errors, Has.Some.Contains("duplicate"));
            Assert.That(danglingValidation.Errors, Has.Some.Contains("child is missing"));
            Assert.That(invalidConditionValidation.Errors, Has.Some.Contains("expected variant is missing"));
            Assert.That(cycleValidation.Errors, Has.Some.Contains("has cycle"));
            Assert.That(emptyTimelineValidation.Errors, Has.Some.Contains("timeline is required"));
            Assert.False(missingRoot.ToCommittedActionBranchDefinition(ActionStateIds.Dodge, 1, in compileContext).CanEvaluate);
            Assert.False(duplicate.ToCommittedActionBranchDefinition(ActionStateIds.Dodge, 1, in compileContext).CanEvaluate);
            Assert.False(dangling.ToCommittedActionBranchDefinition(ActionStateIds.Dodge, 1, in compileContext).CanEvaluate);
            Assert.False(invalidCondition.ToCommittedActionBranchDefinition(ActionStateIds.Dodge, 1, in compileContext).CanEvaluate);
            Assert.False(cycle.ToCommittedActionBranchDefinition(ActionStateIds.Dodge, 1, in compileContext).CanEvaluate);
            Assert.False(emptyTimeline.ToCommittedActionBranchDefinition(ActionStateIds.Dodge, 1, in compileContext).CanEvaluate);
        }

        [Test]
        public void ConditionAuthoringCompilesFormalKinds()
        {
            TimelineFactId factId = TimelineFactIds.CancelableToDodge;

            Assert.AreEqual(CommittedActionConditionKind.Always,
                new CommittedActionBranchConditionAuthoring(CommittedActionConditionKind.Always, CharacterStateVariant.None, true).ToDefinition().Kind);
            Assert.AreEqual(InputRequestKind.Dodge,
                new CommittedActionBranchConditionAuthoring(CommittedActionConditionKind.RequestHeld, CharacterStateVariant.None, true, InputRequestKind.Dodge, string.Empty).ToDefinition().RequestKind);
            Assert.AreEqual(InputRequestKind.TurnBack,
                new CommittedActionBranchConditionAuthoring(CommittedActionConditionKind.RequestReleased, CharacterStateVariant.None, true, InputRequestKind.TurnBack, string.Empty).ToDefinition().RequestKind);
            Assert.AreEqual(factId,
                new CommittedActionBranchConditionAuthoring(CommittedActionConditionKind.RequiredFactActive, CharacterStateVariant.None, true, default, factId.Value).ToDefinition().RequiredFactId);
            Assert.AreEqual(CommittedActionConditionKind.TimelineComplete,
                new CommittedActionBranchConditionAuthoring(CommittedActionConditionKind.TimelineComplete, CharacterStateVariant.None, true).ToDefinition().Kind);
            Assert.True(new CommittedActionBranchConditionAuthoring(CommittedActionConditionKind.HasMoveIntent, CharacterStateVariant.None, true).ToDefinition().ExpectedBool);
            Assert.AreEqual(CharacterStateVariant.Backstep,
                new CommittedActionBranchConditionAuthoring(CommittedActionConditionKind.ActionVariantEquals, CharacterStateVariant.Backstep, false).ToDefinition().ExpectedVariant);
        }

        [Test]
        public void RequiredFactActiveValidatesAgainstTimelineFactContext()
        {
            ActionTimelineCompileContext compileContext = CompileContext();
            CharacterActionCatalogValidationResult valid = new CharacterActionCatalogValidationResult();
            CharacterActionCatalogValidationResult missing = new CharacterActionCatalogValidationResult();

            CreateRequiredFactBranch(TimelineFactIds.CancelableToDodge.Value)
                .ValidateInto(valid, "Valid", ActionStateIds.Dodge, 3, in compileContext);
            CreateRequiredFactBranch("Action.MissingFact")
                .ValidateInto(missing, "Missing", ActionStateIds.Dodge, 3, in compileContext);

            Assert.False(valid.HasErrors, valid.DescribeErrors());
            Assert.That(missing.Errors, Has.Some.Contains("missing from action fact context"));
        }

        [Test]
        public void ActionFactResolverReportsDuplicateAndConflict()
        {
            ActionFactCompileContext context = new ActionFactCompileContext(new[]
            {
                new ActionFactDeclaration(TimelineFactIds.CancelableToDodge, ActionFactSourceKind.TimelineWindow, true),
                new ActionFactDeclaration(TimelineFactIds.CancelableToDodge, ActionFactSourceKind.TimelineWindow, true),
                new ActionFactDeclaration(TimelineFactIds.CancelableToDodge, ActionFactSourceKind.Runtime, false)
            });

            ActionFactValidationResult result = ActionFactIdResolver.Validate(context);

            Assert.That(result.Warnings, Has.Some.Contains("duplicate"));
            Assert.That(result.Errors, Has.Some.Contains("conflicts"));
        }

        [Test]
        public void BranchAuthoringRuntimeBoundaryStaysPure()
        {
            string source = File.ReadAllText(
                Path.Combine(Application.dataPath, "Scripts/Character/Action/Timeline/Config/CommittedActionBranchTimelineAuthoring.cs"),
                Encoding.UTF8);

            Assert.That(source, Does.Not.Contain("UnityEditor"));
            Assert.That(source, Does.Not.Contain("GraphView"));
            Assert.That(source, Does.Not.Contain("TimelinePlayer"));
            Assert.That(source, Does.Not.Contain("PlayableGraph"));
            Assert.That(source, Does.Not.Contain("TreeRunner"));
            Assert.That(source, Does.Not.Contain("DodgeCommittedActionBranchAuthoring"));
        }

        static CommittedActionBranchAuthoring CreateMoveIntentSelectorBranch()
        {
            return new CommittedActionBranchAuthoring(
                1,
                true,
                "branch.test",
                "branch.root.test",
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                new[]
                {
                    CommittedActionBranchNodeAuthoring.Root(
                        "branch.root.test",
                        "selector.test",
                        Vector2.zero),
                    CommittedActionBranchNodeAuthoring.Selector(
                        "selector.test",
                        new[] { "condition.first", "condition.second" },
                        new Vector2(1f, 0f)),
                    CommittedActionBranchNodeAuthoring.ConditionNode(
                        "condition.first",
                        new CommittedActionBranchConditionAuthoring(
                            CommittedActionConditionKind.HasMoveIntent,
                            CharacterStateVariant.None,
                            true),
                        new[] { "timeline.first" },
                        new Vector2(1f, 0f)),
                    CommittedActionBranchNodeAuthoring.ConditionNode(
                        "condition.second",
                        new CommittedActionBranchConditionAuthoring(
                            CommittedActionConditionKind.HasMoveIntent,
                            CharacterStateVariant.None,
                            true),
                        new[] { "timeline.second" },
                        new Vector2(1f, 1f)),
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.first",
                        Timeline("timeline.first", ActionAnimationKeys.DodgeDirectional, "cue.first"),
                        new Vector2(2f, 0f)),
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.second",
                        Timeline("timeline.second", ActionAnimationKeys.DodgeBackstep, "cue.second"),
                        new Vector2(2f, 1f))
                });
        }

        static CommittedActionBranchAuthoring CreateRequiredFactBranch(string requiredFactId)
        {
            return new CommittedActionBranchAuthoring(
                1,
                true,
                "branch.fact",
                "branch.root.fact",
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                new[]
                {
                    CommittedActionBranchNodeAuthoring.Root(
                        "branch.root.fact",
                        "selector.fact",
                        Vector2.zero),
                    CommittedActionBranchNodeAuthoring.Selector(
                        "selector.fact",
                        new[] { "condition.fact" },
                        new Vector2(1f, 0f)),
                    CommittedActionBranchNodeAuthoring.ConditionNode(
                        "condition.fact",
                        new CommittedActionBranchConditionAuthoring(
                            CommittedActionConditionKind.RequiredFactActive,
                            CharacterStateVariant.None,
                            true,
                            default,
                            requiredFactId),
                        new[] { "timeline.fact" },
                        Vector2.one),
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.fact",
                        TimelineWithCancelFact("timeline.fact", TimelineFactIds.CancelableToDodge.Value),
                        new Vector2(2f, 0f))
                });
        }

        static CommittedActionBranchTimelineAuthoring Timeline(
            string timelineNodeId,
            ActionAnimationKey animationKey,
            string cueId)
        {
            return new CommittedActionBranchTimelineAuthoring(
                true,
                timelineNodeId,
                timelineNodeId,
                0.2f,
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                new[]
                {
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipAuthoring(
                                ActionTimelineClipKind.AnimationKey,
                                0f,
                                0.2f,
                                ActionTimelineClipPayloadAuthoring.Animation(animationKey.Value))
                        }),
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Cue,
                        new[]
                        {
                            new ActionTimelineClipAuthoring(
                                ActionTimelineClipKind.Cue,
                                0f,
                                0f,
                                ActionTimelineClipPayloadAuthoring.Cue(cueId))
                        })
                });
        }

        static CommittedActionBranchTimelineAuthoring TimelineWithCancelFact(
            string timelineNodeId,
            string factId)
        {
            return new CommittedActionBranchTimelineAuthoring(
                true,
                timelineNodeId,
                timelineNodeId,
                0.2f,
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                new[]
                {
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Cancel,
                        new[]
                        {
                            new ActionTimelineClipAuthoring(
                                ActionTimelineClipKind.CancelWindow,
                                0f,
                                0.2f,
                                ActionTimelineClipPayloadAuthoring.Fact(factId))
                        })
                });
        }

        static CommittedActionBranchEvaluationContext MoveIntentContext()
        {
            return new CommittedActionBranchEvaluationContext(
                7,
                default,
                CharacterInputRequestFact.None(InputRequestKind.Dodge),
                new MovementInputIntent(Vector2.up, Vector2.up, 1f, true),
                default);
        }

        static ActionTimelineCompileContext CompileContext()
        {
            return new ActionTimelineCompileContext(1f / 60f);
        }
    }

    public sealed class ConfigOnlyActionGoldenPathTests
    {
        const string TestHoldAction = "Action.TestHold";
        const string TestCounterAction = "Action.TestCounter";
        const string CounterFact = "window.test.counter.open";
        const string HoldStartKey = "Action.TestHold.Start";
        const string HoldLoopKey = "Action.TestHold.Loop";
        const string HoldEndKey = "Action.TestHold.End";
        const string CounterMainKey = "Action.TestCounter.Main";

        [Test]
        public void TestHoldDefinitionCompilesThroughFormalActionDefinition()
        {
            CharacterActionDefinitionSO hold = CreateActionDefinition(
                TestHoldAction,
                ActionRequestType.Custom,
                InputRequestKind.Interact,
                TestHoldBranch());
            CharacterActionCatalogValidationResult validation = hold.Validate(CompileContext());
            CharacterActionDefinition definition = hold.ToDefinition(CompileContext());
            CharacterActionCatalog catalog = new CharacterActionCatalog(new[] { definition });

            Assert.False(validation.HasErrors, validation.DescribeErrors());
            Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));
            Assert.True(catalog.TryGetDefinition(new ActionStateId(TestHoldAction), out CharacterActionDefinition fromCatalog));
            Assert.AreEqual(TestHoldAction, fromCatalog.ActionState.Value);
            Assert.AreEqual(CommittedActionNodeKind.Root, branch.RootNode.Kind);
            Assert.AreEqual("branch.root.testhold", branch.RootNode.NodeId.Value);
            Assert.AreEqual("timeline.testhold.start", branch.RootNode.ChildIds[0].Value);

            Object.DestroyImmediate(hold);
        }

        [Test]
        public void TestHoldStartLoopEndRunsThroughBranchConditions()
        {
            CharacterActionDefinitionSO hold = CreateActionDefinition(
                TestHoldAction,
                ActionRequestType.Custom,
                InputRequestKind.Interact,
                TestHoldBranch());
            CharacterActionDefinition definition = hold.ToDefinition(CompileContext());
            Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));

            CommittedActionBranchOutcome start = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(branch, 0, 1));
            CommittedActionBranchOutcome loopAfterStart = EvaluateActive(branch, "timeline.testhold.start", 12, 2, CommittedActionRequestFactSet.Empty);
            CommittedActionBranchOutcome loopHeld = EvaluateActive(branch, "timeline.testhold.loop", 0, 3, RequestFacts(true, false, 3));
            CommittedActionBranchOutcome endReleased = EvaluateActive(branch, "timeline.testhold.loop", 0, 4, RequestFacts(true, true, 4));

            Assert.AreEqual("timeline.testhold.start", start.SelectedNodeId.Value);
            Assert.AreEqual(HoldStartKey, start.TimelineOutcome.AnimationKey.Value);
            Assert.AreEqual("timeline.testhold.loop", loopAfterStart.SelectedNodeId.Value);
            Assert.AreEqual(HoldLoopKey, loopAfterStart.TimelineOutcome.AnimationKey.Value);
            Assert.AreEqual("timeline.testhold.loop", loopHeld.SelectedNodeId.Value);
            Assert.AreEqual("timeline.testhold.end", endReleased.SelectedNodeId.Value);
            Assert.AreEqual(HoldEndKey, endReleased.TimelineOutcome.AnimationKey.Value);

            Object.DestroyImmediate(hold);
        }

        [Test]
        public void TestHoldLoopOutputsCounterFactOnlyInsideWindow()
        {
            CharacterActionDefinitionSO hold = CreateActionDefinition(
                TestHoldAction,
                ActionRequestType.Custom,
                InputRequestKind.Interact,
                TestHoldBranch());
            CharacterActionDefinition definition = hold.ToDefinition(CompileContext());
            Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));

            CommittedActionBranchOutcome outside = EvaluateActive(branch, "timeline.testhold.loop", 0, 5, CommittedActionRequestFactSet.Empty);
            CommittedActionBranchOutcome inside = EvaluateActive(branch, "timeline.testhold.loop", 8, 6, CommittedActionRequestFactSet.Empty);

            CollectionAssert.DoesNotContain(outside.TimelineOutcome.ActiveWindowFactIds, CounterFact);
            CollectionAssert.Contains(inside.TimelineOutcome.ActiveWindowFactIds, CounterFact);

            Object.DestroyImmediate(hold);
        }

        [Test]
        public void TestCounterPolicyUsesMatrixAndArbiter()
        {
            CharacterActionDefinitionSO counter = CreateActionDefinition(
                TestCounterAction,
                ActionRequestType.Attack,
                InputRequestKind.Attack,
                TestCounterBranch());
            CharacterActionCatalogValidationResult validation = counter.Validate(CompileContext());
            CharacterActionDefinition definition = counter.ToDefinition(CompileContext());
            ActionTransitionPolicyMatrixDefinition matrix = new ActionTransitionPolicyMatrixDefinition(new[]
            {
                new ActionTransitionPolicyRowDefinition(
                    TestHoldAction,
                    TestCounterAction,
                    ActionRequestType.Attack,
                    CounterFact,
                    50)
            });
            var policies = ActionInterruptPolicySetCompiler.Compile(matrix, FactContext(CounterFact), out ActionInterruptPolicyValidationResult policyValidation);
            ActionInterruptRequest request = new ActionInterruptRequest(
                1,
                ActionRequestType.Attack,
                new ActionStateId(TestCounterAction),
                50,
                0,
                0);

            ActionInterruptDecision accepted = ActionInterruptArbiter.Arbitrate(
                CounterContext(CounterFact, 0),
                new[] { request },
                policies);
            ActionInterruptDecision missingFact = ActionInterruptArbiter.Arbitrate(
                CounterContext(string.Empty, 0),
                new[] { request },
                policies);
            ActionInterruptDecision blockedByResistance = ActionInterruptArbiter.Arbitrate(
                CounterContext(CounterFact, 80),
                new[] { request },
                policies);

            Assert.False(validation.HasErrors, validation.DescribeErrors());
            Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));
            Assert.False(policyValidation.HasErrors, policyValidation.DescribeErrors());
            Assert.AreEqual(1, policies.Count);
            Assert.True(accepted.Accepted);
            Assert.AreEqual(TestCounterAction, accepted.TargetState.Value);
            Assert.False(missingFact.Accepted);
            Assert.False(blockedByResistance.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.BlockedByResistance, blockedByResistance.RejectReason);

            CommittedActionBranchOutcome counterOutcome = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(branch, 0, 7));
            Assert.AreEqual(CounterMainKey, counterOutcome.TimelineOutcome.AnimationKey.Value);

            Object.DestroyImmediate(counter);
        }

        [Test]
        public void TestHoldEndCompletionExitsThroughLifecycle()
        {
            CharacterActionDefinitionSO hold = CreateActionDefinition(
                TestHoldAction,
                ActionRequestType.Custom,
                InputRequestKind.Interact,
                TestHoldBranch());
            CharacterActionDefinition definition = hold.ToDefinition(CompileContext());
            CommittedActionRuntimeModule module = new CommittedActionRuntimeModule();
            CharacterActionCatalog catalog = new CharacterActionCatalog(new[] { definition });
            CharacterResolvedAction action = ResolvedAction(
                TestHoldAction,
                ActionRequestType.Custom,
                InputRequestKind.Interact,
                HoldStartKey,
                20);

            module.TickActionLifecycle(in action, in catalog, 0.02f, 20);
            Assert.True(definition.CommittedActionBranch.TryGetNode(
                new CommittedActionNodeId("timeline.testhold.end"),
                out CommittedActionNodeDefinition endNode));
            CommittedActionBranchEvaluationContext endCompleteContext = new CommittedActionBranchEvaluationContext(
                32,
                action,
                action.RequestFact,
                default,
                default,
                CharacterRuntimeBlackboardSnapshot.Empty)
                .WithActiveTimeline(endNode, endNode.TimelineNode.Timeline.DurationTicks);
            ActionMotionResolveResult completed = new ActionMotionResolveResult(
                action.MotionSpec,
                default,
                false,
                true,
                false,
                32,
                "testhold-end-complete");

            module.CompleteActionLifecycle(in completed, false);
            ActionLifecycleFrame next = module.TickActionLifecycle(default, in catalog, 0.02f, 33);

            Assert.True(CommittedActionConditionEvaluator.Evaluate(
                CommittedActionConditionDefinition.TimelineComplete(),
                in endCompleteContext));
            Assert.True(next.ExitedThisFrame);
            Assert.False(next.HasAction);
            Assert.False(next.HasCommittedActionBranchOutcome);

            Object.DestroyImmediate(hold);
        }

        [Test]
        public void GoldenPathFullBodyClaimMapsToSlotContract()
        {
            CharacterActionDefinitionSO hold = CreateActionDefinition(
                TestHoldAction,
                ActionRequestType.Custom,
                InputRequestKind.Interact,
                TestHoldBranch());
            CharacterActionDefinition definition = hold.ToDefinition(CompileContext());
            Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));

            CommittedActionBranchOutcome outcome = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(branch, 0, 8));
            CharacterFrameArbitrationInput input = new CharacterFrameArbitrationInput(
                outcome.BodyClaim,
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.Locomotion, 8),
                outcome.Candidate,
                CharacterFrameCandidateOutput.UpperBody(false, true, 8),
                8);
            CharacterFramePlan plan = new CharacterFramePlan(DefaultBodyArbiter.Instance.Decide(in input));

            Assert.AreEqual(BodyOccupancyKind.FullBody, outcome.BodyClaim.Kind);
            Assert.AreEqual(CharacterBodyDomain.CommittedAction, plan.BaseSlotOwner);
            Assert.AreNotEqual(CharacterBodyDomain.UpperBody, plan.BaseSlotOwner);
            Assert.True(plan.UpperBodySlotSuppressed);

            Object.DestroyImmediate(hold);
        }

        [Test]
        public void GoldenPathRuntimeHasNoTestActionSpecificBranches()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts/Character");
            string runtime = string.Join("\n", Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories)
                .Select(path => File.ReadAllText(path, Encoding.UTF8)));
            string configRoot = Path.Combine(Application.dataPath, "Configs/3C");
            string configs = Directory.Exists(configRoot)
                ? string.Join("\n", Directory.GetFiles(configRoot, "*.*", SearchOption.AllDirectories)
                    .Select(path => File.ReadAllText(path, Encoding.UTF8)))
                : string.Empty;

            Assert.That(runtime, Does.Not.Contain("PlayerTestActionController"));
            Assert.That(runtime, Does.Not.Contain("PlayerTestHoldController"));
            Assert.That(runtime, Does.Not.Contain(TestHoldAction));
            Assert.That(runtime, Does.Not.Contain(TestCounterAction));
            Assert.That(runtime, Does.Not.Contain("TestActionMotionExecutor"));
            Assert.That(runtime, Does.Not.Contain("TestActionAnimationPresenter"));
            Assert.That(runtime, Does.Not.Contain("BaseLayerOwner"));
            Assert.That(configs, Does.Not.Contain(TestHoldAction));
            Assert.That(configs, Does.Not.Contain(TestCounterAction));
        }

        static CharacterActionDefinitionSO CreateActionDefinition(
            string actionId,
            ActionRequestType requestType,
            InputRequestKind inputKind,
            CommittedActionBranchAuthoring branch)
        {
            CharacterActionDefinitionSO asset = ScriptableObject.CreateInstance<CharacterActionDefinitionSO>();
            asset.name = actionId;
            SetField(asset, "actionStateId", actionId);
            SetField(asset, "requestType", requestType);
            SetField(asset, "sourceInputKind", inputKind);
            SetField(asset, "motionSourceStateId", actionId);
            SetField(asset, "priority", 50);
            SetField(asset, "resistance", 0);
            SetField(asset, "committedActionBranch", branch);
            return asset;
        }

        static CommittedActionBranchAuthoring TestHoldBranch()
        {
            return new CommittedActionBranchAuthoring(
                1,
                true,
                "branch.testhold",
                "branch.root.testhold",
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Animation,
                new[]
                {
                    CommittedActionBranchNodeAuthoring.Root(
                        "branch.root.testhold",
                        "timeline.testhold.start",
                        Vector2.zero),
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.testhold.start",
                        Timeline(TestHoldAction, HoldStartKey, 0.2f),
                        new[] { "condition.testhold.start.complete" },
                        Vector2.right),
                    CommittedActionBranchNodeAuthoring.ConditionNode(
                        "condition.testhold.start.complete",
                        new CommittedActionBranchConditionAuthoring(CommittedActionConditionKind.TimelineComplete, CharacterStateVariant.None, true),
                        new[] { "timeline.testhold.loop" },
                        Vector2.right),
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.testhold.loop",
                        Timeline(TestHoldAction, HoldLoopKey, 0.4f, CounterFact, 0.1f, 0.3f),
                        new[] { "condition.testhold.loop.held", "condition.testhold.loop.released" },
                        Vector2.up),
                    CommittedActionBranchNodeAuthoring.ConditionNode(
                        "condition.testhold.loop.held",
                        new CommittedActionBranchConditionAuthoring(
                            CommittedActionConditionKind.RequestHeld,
                            CharacterStateVariant.None,
                            true,
                            InputRequestKind.Interact,
                            string.Empty),
                        new[] { "timeline.testhold.loop" },
                        Vector2.one),
                    CommittedActionBranchNodeAuthoring.ConditionNode(
                        "condition.testhold.loop.released",
                        new CommittedActionBranchConditionAuthoring(
                            CommittedActionConditionKind.RequestReleased,
                            CharacterStateVariant.None,
                            true,
                            InputRequestKind.Interact,
                            string.Empty),
                        new[] { "timeline.testhold.end" },
                        new Vector2(1f, 2f)),
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.testhold.end",
                        Timeline(TestHoldAction, HoldEndKey, 0.2f),
                        Vector2.down)
                });
        }

        static CommittedActionBranchAuthoring TestCounterBranch()
        {
            return new CommittedActionBranchAuthoring(
                1,
                true,
                "branch.testcounter",
                "branch.root.testcounter",
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Animation,
                new[]
                {
                    CommittedActionBranchNodeAuthoring.Root(
                        "branch.root.testcounter",
                        "timeline.testcounter.main",
                        Vector2.zero),
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.testcounter.main",
                        Timeline(TestCounterAction, CounterMainKey, 0.2f),
                        Vector2.right)
                });
        }

        static CommittedActionBranchTimelineAuthoring Timeline(
            string actionId,
            string animationKey,
            float durationSeconds,
            string factId = "",
            float factStart = 0f,
            float factEnd = 0f)
        {
            ActionTimelineTrackAuthoring[] tracks = string.IsNullOrWhiteSpace(factId)
                ? new[]
                {
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipAuthoring(
                                ActionTimelineClipKind.AnimationKey,
                                0f,
                                durationSeconds,
                                ActionTimelineClipPayloadAuthoring.Animation(animationKey))
                        })
                }
                : new[]
                {
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipAuthoring(
                                ActionTimelineClipKind.AnimationKey,
                                0f,
                                durationSeconds,
                                ActionTimelineClipPayloadAuthoring.Animation(animationKey))
                        }),
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Cancel,
                        new[]
                        {
                            new ActionTimelineClipAuthoring(
                                ActionTimelineClipKind.CancelWindow,
                                factStart,
                                factEnd,
                                ActionTimelineClipPayloadAuthoring.Fact(factId))
                        })
                };

            return new CommittedActionBranchTimelineAuthoring(
                true,
                actionId,
                actionId,
                durationSeconds,
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Animation,
                tracks);
        }

        static CommittedActionBranchOutcome EvaluateActive(
            CommittedActionBranchDefinition branch,
            string nodeId,
            int localTick,
            int sourceStep,
            CommittedActionRequestFactSet requestFacts)
        {
            CommittedActionBranchEvaluationContext context = new CommittedActionBranchEvaluationContext(
                sourceStep,
                default,
                CharacterInputRequestFact.None(InputRequestKind.Interact),
                default,
                default,
                CharacterRuntimeBlackboardSnapshot.Empty,
                new CommittedActionNodeId(nodeId),
                localTick,
                0,
                requestFacts,
                ActionFactSet.Empty,
                StateTimelineWindowFacts.None(default),
                1);

            return CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(branch, localTick, sourceStep, context));
        }

        static CommittedActionRequestFactSet RequestFacts(bool held, bool released, int sourceStep)
        {
            if (held && released)
            {
                return new CommittedActionRequestFactSet(new[]
                {
                    CommittedActionRequestFact.HeldFact(InputRequestKind.Interact, sourceStep),
                    CommittedActionRequestFact.ReleasedFact(InputRequestKind.Interact, sourceStep)
                });
            }

            if (held)
            {
                return new CommittedActionRequestFactSet(new[]
                {
                    CommittedActionRequestFact.HeldFact(InputRequestKind.Interact, sourceStep)
                });
            }

            if (released)
            {
                return new CommittedActionRequestFactSet(new[]
                {
                    CommittedActionRequestFact.ReleasedFact(InputRequestKind.Interact, sourceStep)
                });
            }

            return CommittedActionRequestFactSet.Empty;
        }

        static ActionInterruptContext CounterContext(string requestFactId, int resistance)
        {
            StateTimelineWindowFacts facts = new StateTimelineWindowFacts(
                default,
                0f,
                false,
                0f,
                false,
                false,
                false,
                false,
                0,
                resistance,
                0,
                false,
                string.Empty,
                string.Empty,
                string.Empty,
                requestFactId);
            return new ActionInterruptContext(new ActionStateId(TestHoldAction), 0f, resistance, 0, facts);
        }

        static CharacterResolvedAction ResolvedAction(
            string actionId,
            ActionRequestType requestType,
            InputRequestKind inputKind,
            string animationKey,
            int sourceStep)
        {
            ActionStateId actionState = new ActionStateId(actionId);
            ActionMotionSpec motionSpec = new ActionMotionSpec(
                actionState,
                new CharacterStateId(actionId),
                CharacterStateVariant.None,
                0.2f,
                0f,
                false,
                false,
                Vector3.zero,
                0f,
                sourceStep);
            CharacterActionRequest request = new CharacterActionRequest(
                CharacterFrameRequestProviderId.External,
                requestType,
                inputKind,
                sourceStep,
                sourceStep + 10,
                50,
                0,
                CharacterStateVariant.None,
                Vector3.zero);
            CharacterInputRequestFact requestFact = new CharacterInputRequestFact(
                true,
                inputKind,
                sourceStep,
                sourceStep + 10,
                50,
                CharacterStateVariant.None,
                Vector3.zero);
            ActionInterruptRequest interruptRequest = new ActionInterruptRequest(
                sourceStep,
                requestType,
                actionState,
                50,
                0,
                sourceStep,
                sourceStep + 10);
            return new CharacterResolvedAction(
                CharacterFrameRequestProviderId.External,
                request,
                requestFact,
                interruptRequest,
                new ActionInterruptContext(ActionStateIds.None, 0f, 0, sourceStep),
                new ActionAnimationKey(animationKey),
                motionSpec);
        }

        static ActionFactCompileContext FactContext(params string[] factIds)
        {
            ActionFactDeclaration[] declarations = new ActionFactDeclaration[factIds.Length];
            for (int i = 0; i < factIds.Length; i++)
            {
                declarations[i] = new ActionFactDeclaration(
                    new TimelineFactId(factIds[i]),
                    ActionFactSourceKind.TimelineWindow,
                    true);
            }

            return new ActionFactCompileContext(declarations);
        }

        static ActionTimelineCompileContext CompileContext()
        {
            return new ActionTimelineCompileContext(1f / 60f);
        }

        static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, fieldName);
            field.SetValue(target, value);
        }
    }
}
