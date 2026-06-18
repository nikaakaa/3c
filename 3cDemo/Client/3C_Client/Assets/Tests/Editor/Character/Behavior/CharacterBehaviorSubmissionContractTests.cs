using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterBehavior;
using ThirdPersonCharacterStateMachine;
using UnityEngine;

namespace Tests.Editor.Character.Behavior
{
    public sealed class CharacterBehaviorSubmissionContractTests
    {
        [Test]
        public void RequestAndOutputPayloadsAreTypedAndPassBounded()
        {
            CharacterBehaviorSubmissionAudit audit = new CharacterBehaviorSubmissionAudit();

            audit.RequirePassBoundary(CharacterBehaviorEvaluationPass.RequestPass, CharacterBehaviorSubmissionKind.Output);
            audit.RequirePassBoundary(CharacterBehaviorEvaluationPass.OutputPass, CharacterBehaviorSubmissionKind.Request);
            audit.RequirePassBoundary(CharacterBehaviorEvaluationPass.RequestPass, CharacterBehaviorSubmissionKind.Request);
            audit.RequirePassBoundary(CharacterBehaviorEvaluationPass.OutputPass, CharacterBehaviorSubmissionKind.Output);

            CollectionAssert.Contains(audit.Diagnostics, "pass-payload-not-allowed:RequestPass:Output");
            CollectionAssert.Contains(audit.Diagnostics, "pass-payload-not-allowed:OutputPass:Request");
            Assert.AreEqual(2, audit.Diagnostics.Count);
        }

        [Test]
        public void SourceIdSanitizesDefaultsAndSortsByStepOrderThenId()
        {
            CharacterBehaviorSubmissionSource later = Source("locomotion", CharacterBehaviorSourceKind.Locomotion, 2, 0);
            CharacterBehaviorSubmissionSource earlier = Source("action", CharacterBehaviorSourceKind.CommittedAction, 1, 1);
            CharacterBehaviorSubmissionSource sameStepEarlierOrder = Source("root", CharacterBehaviorSourceKind.Root, 1, 0);
            CharacterBehaviorSubmissionSource invalid = Source(" ", CharacterBehaviorSourceKind.Locomotion, -1, -5);

            List<CharacterBehaviorSubmissionSource> sources = new List<CharacterBehaviorSubmissionSource>
            {
                later,
                earlier,
                sameStepEarlierOrder
            };
            sources.Sort();

            Assert.False(invalid.IsValid);
            Assert.AreEqual(0, invalid.SourceStep);
            Assert.AreEqual(0, invalid.SourceOrder);
            Assert.AreEqual("root", sources[0].NodeId.Value);
            Assert.AreEqual("action", sources[1].NodeId.Value);
            Assert.AreEqual("locomotion", sources[2].NodeId.Value);
        }

        [Test]
        public void PayloadDefaultsAreEmptyAndConsumerRulesAreExplicit()
        {
            CharacterBehaviorSubmissionSource requestSource = Source("action", CharacterBehaviorSourceKind.CommittedAction, 4, 1);
            BehaviorRequestSubmission request = BehaviorRequestSubmission.None(requestSource);
            BehaviorOutputSubmission output = BehaviorOutputSubmission.None(requestSource);
            BehaviorCueSubmission cue = default;
            BehaviorMotionChannelSubmission motion = default;
            BehaviorAnimationChannelSubmission animation = default;
            BehaviorWindowFactsChannelSubmission windowFacts = default;
            BehaviorClaimSubmission claim = default;
            BehaviorDiagnosticSubmission diagnostic = default;
            BehaviorStateWriteSubmission stateWrite = default;

            Assert.False(request.HasRequest);
            Assert.False(output.HasOutput);
            Assert.False(cue.HasCue);
            Assert.False(motion.HasMotion);
            Assert.False(animation.HasAnimation);
            Assert.False(windowFacts.HasFacts);
            Assert.False(claim.HasClaim);
            Assert.False(diagnostic.HasDiagnostic);
            Assert.False(stateWrite.HasWrite);
            Assert.True(CharacterBehaviorSubmissionRules.CanConsume(CharacterBehaviorSubmissionKind.Request, CharacterBehaviorSubmissionConsumer.RequestArbiter));
            Assert.True(CharacterBehaviorSubmissionRules.CanConsume(CharacterBehaviorSubmissionKind.Output, CharacterBehaviorSubmissionConsumer.BehaviorSubmissionComposer));
            Assert.True(CharacterBehaviorSubmissionRules.CanConsume(CharacterBehaviorSubmissionKind.MotionChannel, CharacterBehaviorSubmissionConsumer.FramePlanInput));
            Assert.True(CharacterBehaviorSubmissionRules.CanConsume(CharacterBehaviorSubmissionKind.AnimationChannel, CharacterBehaviorSubmissionConsumer.FramePlanInput));
            Assert.True(CharacterBehaviorSubmissionRules.CanConsume(CharacterBehaviorSubmissionKind.WindowFactsChannel, CharacterBehaviorSubmissionConsumer.FramePlanInput));
            Assert.True(CharacterBehaviorSubmissionRules.CanConsume(CharacterBehaviorSubmissionKind.Claim, CharacterBehaviorSubmissionConsumer.FramePlanInput));
            Assert.False(CharacterBehaviorSubmissionRules.CanConsume(CharacterBehaviorSubmissionKind.Output, CharacterBehaviorSubmissionConsumer.RequestArbiter));
        }

        [Test]
        public void IllegalConsumerAndUnconsumedRequiredPayloadsProduceDiagnostics()
        {
            CharacterBehaviorSubmissionAudit audit = new CharacterBehaviorSubmissionAudit();

            audit.RequireAllowedConsumer(CharacterBehaviorSubmissionKind.Output, CharacterBehaviorSubmissionConsumer.RequestArbiter);
            audit.RequireConsumed(CharacterBehaviorSubmissionKind.Output, false);
            audit.RequireConsumed(CharacterBehaviorSubmissionKind.MotionChannel, false);
            audit.RequireConsumed(CharacterBehaviorSubmissionKind.AnimationChannel, false);
            audit.RequireConsumed(CharacterBehaviorSubmissionKind.WindowFactsChannel, false);
            audit.RequireConsumed(CharacterBehaviorSubmissionKind.Claim, false);
            audit.RequireConsumed(CharacterBehaviorSubmissionKind.Diagnostic, false);

            CollectionAssert.Contains(audit.Diagnostics, "consumer-not-allowed:Output:RequestArbiter");
            CollectionAssert.Contains(audit.Diagnostics, "submission-unconsumed:Output");
            CollectionAssert.Contains(audit.Diagnostics, "submission-unconsumed:MotionChannel");
            CollectionAssert.Contains(audit.Diagnostics, "submission-unconsumed:AnimationChannel");
            CollectionAssert.Contains(audit.Diagnostics, "submission-unconsumed:WindowFactsChannel");
            CollectionAssert.Contains(audit.Diagnostics, "submission-unconsumed:Claim");
            Assert.AreEqual(6, audit.Diagnostics.Count);
        }

        [Test]
        public void SubmissionSetQueriesByPassAndSourceAndKeepsStableOrder()
        {
            CharacterBehaviorSubmissionSet set = new CharacterBehaviorSubmissionSet();
            CharacterBehaviorSubmissionSource action = Source("action", CharacterBehaviorSourceKind.CommittedAction, 3, 1);
            CharacterBehaviorSubmissionSource locomotion = Source("locomotion", CharacterBehaviorSourceKind.Locomotion, 3, 0);

            set.Add(new BehaviorDiagnosticSubmission(action, "action", string.Empty, false));
            set.Add(new BehaviorDiagnosticSubmission(locomotion, "locomotion", string.Empty, false));
            set.Add(new BehaviorCueSubmission(
                new CharacterBehaviorSubmissionSource(action.NodeId, action.SourceKind, CharacterBehaviorEvaluationPass.OutputPass, action.SourceStep, action.SourceOrder),
                "dodge-cue",
                0));
            CharacterBehaviorSubmissionSource actionOutput = CharacterBehaviorSubmissionSource.Create(
                "action",
                CharacterBehaviorSourceKind.CommittedAction,
                CharacterBehaviorEvaluationPass.OutputPass,
                3,
                1);
            set.Add(new BehaviorMotionChannelSubmission(actionOutput, new ActionMotionSpec(
                ActionStateIds.Dodge,
                CharacterStateIds.Dodge,
                CharacterStateVariant.Directional,
                0.2f,
                4f,
                true,
                true,
                Vector3.forward,
                0f,
                3)));
            set.Add(new BehaviorAnimationChannelSubmission(actionOutput, ActionAnimationKeys.DodgeDirectional));
            set.Add(new BehaviorWindowFactsChannelSubmission(actionOutput, new[] { "window.action.dodge" }));
            set.Add(new BehaviorClaimSubmission(actionOutput, BodyOccupancyClaim.CommittedActionFullBody(3)));

            CharacterBehaviorSubmissionSet outputPass = set.QueryByPass(CharacterBehaviorEvaluationPass.OutputPass);
            CharacterBehaviorSubmissionSet actionOnly = set.QueryBySource(action.NodeId);

            Assert.False(CharacterBehaviorSubmissionSet.Empty.IsEmpty == false);
            Assert.AreEqual("locomotion", set.Diagnostics[0].Code);
            Assert.AreEqual("action", set.Diagnostics[1].Code);
            Assert.AreEqual(1, outputPass.Cues.Count);
            Assert.AreEqual(1, outputPass.MotionChannels.Count);
            Assert.AreEqual(1, outputPass.AnimationChannels.Count);
            Assert.AreEqual(1, outputPass.WindowFactsChannels.Count);
            Assert.AreEqual(1, outputPass.Claims.Count);
            Assert.AreEqual(ActionAnimationKeys.DodgeDirectional, actionOnly.AnimationChannels[0].AnimationKey);
            CollectionAssert.Contains(actionOnly.WindowFactsChannels[0].FactIds, "window.action.dodge");
            Assert.True(actionOnly.Claims[0].Claim.ClaimsFullBody);
            Assert.AreEqual(6, actionOnly.Diagnostics.Count + actionOnly.Cues.Count + actionOnly.MotionChannels.Count + actionOnly.AnimationChannels.Count + actionOnly.WindowFactsChannels.Count + actionOnly.Claims.Count);
        }

        [Test]
        public void ChannelPayloadsAreOutputPassOnly()
        {
            CharacterBehaviorSubmissionAudit audit = new CharacterBehaviorSubmissionAudit();

            audit.RequirePassBoundary(CharacterBehaviorEvaluationPass.RequestPass, CharacterBehaviorSubmissionKind.MotionChannel);
            audit.RequirePassBoundary(CharacterBehaviorEvaluationPass.RequestPass, CharacterBehaviorSubmissionKind.AnimationChannel);
            audit.RequirePassBoundary(CharacterBehaviorEvaluationPass.RequestPass, CharacterBehaviorSubmissionKind.WindowFactsChannel);
            audit.RequirePassBoundary(CharacterBehaviorEvaluationPass.RequestPass, CharacterBehaviorSubmissionKind.Claim);
            audit.RequirePassBoundary(CharacterBehaviorEvaluationPass.OutputPass, CharacterBehaviorSubmissionKind.MotionChannel);
            audit.RequirePassBoundary(CharacterBehaviorEvaluationPass.OutputPass, CharacterBehaviorSubmissionKind.AnimationChannel);
            audit.RequirePassBoundary(CharacterBehaviorEvaluationPass.OutputPass, CharacterBehaviorSubmissionKind.WindowFactsChannel);
            audit.RequirePassBoundary(CharacterBehaviorEvaluationPass.OutputPass, CharacterBehaviorSubmissionKind.Claim);

            CollectionAssert.Contains(audit.Diagnostics, "pass-payload-not-allowed:RequestPass:MotionChannel");
            CollectionAssert.Contains(audit.Diagnostics, "pass-payload-not-allowed:RequestPass:AnimationChannel");
            CollectionAssert.Contains(audit.Diagnostics, "pass-payload-not-allowed:RequestPass:WindowFactsChannel");
            CollectionAssert.Contains(audit.Diagnostics, "pass-payload-not-allowed:RequestPass:Claim");
            Assert.AreEqual(4, audit.Diagnostics.Count);
        }

        [Test]
        public void StateOwnershipRulesCoverAllRequiredOwners()
        {
            AssertOwner("Behavior node private state", CharacterBehaviorStateOwner.BehaviorRuntime);
            AssertOwner("Locomotion runtime state", CharacterBehaviorStateOwner.LocomotionRuntime);
            AssertOwner("Action active action and state time", CharacterBehaviorStateOwner.ActionLifecycleRuntime);
            AssertOwner("Animation playback state", CharacterBehaviorStateOwner.AnimationPresenter);
            AssertOwner("Confirmed blackboard facts", CharacterBehaviorStateOwner.CharacterRuntimeBlackboard);
            AssertOwner("Rollback restore state", CharacterBehaviorStateOwner.RuntimeCaptureRestore);
            AssertOwner("Editor graph state", CharacterBehaviorStateOwner.EditorOnlyAsset);
        }

        [Test]
        public void FakeRunnerCollectsMultipleLeavesWithoutProductionRegistration()
        {
            FakeCharacterBehaviorSubmissionRunner runner = new FakeCharacterBehaviorSubmissionRunner(new[]
            {
                new FakeCharacterBehaviorLeaf(Source("action", CharacterBehaviorSourceKind.TestFake, 5, 1), new FakeCharacterBehaviorLeafEvaluator("action")),
                new FakeCharacterBehaviorLeaf(Source("locomotion", CharacterBehaviorSourceKind.TestFake, 5, 0), new FakeCharacterBehaviorLeafEvaluator("locomotion"))
            });

            CharacterBehaviorSubmissionSet result = runner.Collect(CharacterBehaviorEvaluationPass.RequestPass);

            Assert.AreEqual(2, result.Diagnostics.Count);
            Assert.AreEqual("locomotion", result.Diagnostics[0].Code);
            Assert.AreEqual("action", result.Diagnostics[1].Code);
            Assert.AreEqual(2, result.StateWrites.Count);
            Assert.True(result.StateWrites[0].IsOwnedBySourceNode);
            string runtimeCore = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Character/Pipeline/Runtime/CharacterRuntimeCore.cs"));
            Assert.That(runtimeCore, Does.Not.Contain("FakeCharacterBehaviorSubmissionRunner"));
        }

        [Test]
        public void SubmissionContractsDoNotReferenceUnityEditorRefRunnerOrSideEffectAppliers()
        {
            string root = Path.Combine(Application.dataPath, "Scripts/Character/Behavior");
            string[] banned =
            {
                "MonoBehaviour",
                "Transform",
                "Animator",
                "CharacterController",
                "InputAction",
                "GraphView",
                "TreeRunner",
                "TimelinePlayer",
                "ICharacterAnimationOutputPresenter",
                "CharacterAnimancerPresenter",
                "CharacterFrameOutputApplier",
                "CharacterMotionDriver",
                "CharacterControllerBasicMotionExecutor",
                "WriteLocomotionPreemptionFact",
                "WriteStateFrameActionFacts",
                "UnityEditor"
            };
            List<string> violations = new List<string>();
            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                for (int i = 0; i < banned.Length; i++)
                {
                    if (text.Contains(banned[i]))
                        violations.Add($"{Path.GetFileName(file)}:{banned[i]}");
                }
            }

            Assert.IsEmpty(violations);
        }

        static CharacterBehaviorSubmissionSource Source(
            string id,
            CharacterBehaviorSourceKind kind,
            int step,
            int order)
        {
            return CharacterBehaviorSubmissionSource.Create(
                id,
                kind,
                CharacterBehaviorEvaluationPass.RequestPass,
                step,
                order);
        }

        static void AssertOwner(string stateKind, CharacterBehaviorStateOwner expected)
        {
            Assert.True(CharacterBehaviorStateOwnership.TryGetOwner(stateKind, out CharacterBehaviorStateOwner owner));
            Assert.AreEqual(expected, owner);
        }
    }
}
