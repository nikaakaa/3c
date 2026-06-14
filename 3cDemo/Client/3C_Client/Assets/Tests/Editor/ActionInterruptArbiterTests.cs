using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonDiagnostics;
using UnityEngine;

namespace ThirdPersonAction.Tests
{
    public sealed class ActionInterruptArbiterTests
    {
        static readonly ActionStateId Idle = new ActionStateId("Locomotion.Idle");
        static readonly ActionStateId MoveStop = new ActionStateId("Locomotion.MoveStop");
        static readonly ActionStateId Attack01 = new ActionStateId("Action.Attack01");
        static readonly ActionStateId Dodge = new ActionStateId("Action.Dodge");
        static readonly ActionStateId HitReact = new ActionStateId("Action.HitReact");
        static readonly ActionStateId Death = new ActionStateId("Action.Death");

        [TearDown]
        public void TearDown()
        {
            RuntimeDiagnosticLog.Reset();
        }

        [Test]
        public void NoRequestReturnsNoRequest()
        {
            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Idle),
                new ActionInterruptRequest[0],
                new[] { Always(Idle, Dodge, 1) });

            Assert.False(decision.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.NoRequest, decision.RejectReason);
        }

        [Test]
        public void NoMatchingPolicyReturnsNoPolicy()
        {
            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Idle),
                new[] { Request(Dodge, 10) },
                new[] { Always(Attack01, Dodge, 1) });

            Assert.False(decision.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.NoPolicy, decision.RejectReason);
        }

        [Test]
        public void ExpiredRequestIsRejected()
        {
            ActionInterruptRequest request = Request(Dodge, 10, sourceOrder: 0, expireTick: 3);

            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Idle, currentTick: 4),
                new[] { request },
                new[] { Always(Idle, Dodge, 1) });

            Assert.False(decision.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.Expired, decision.RejectReason);
        }

        [Test]
        public void AlwaysPolicyAcceptsRequest()
        {
            ActionInterruptRequest request = Request(Dodge, 10);

            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Idle),
                new[] { request },
                new[] { Always(Idle, Dodge, 1) });

            Assert.True(decision.Accepted);
            Assert.AreEqual(Dodge, decision.TargetState);
            Assert.AreEqual(request.RequestId, decision.SelectedRequest.RequestId);
        }

        [Test]
        public void AcceptedRequestWritesDetailedDiagnosticLog()
        {
            List<RuntimeDiagnosticLogEvent> events = new List<RuntimeDiagnosticLogEvent>();
            ActionInterruptRequest request = Request(Dodge, 10, sourceOrder: 2);

            using (RuntimeDiagnosticLog.Capture(events.Add))
            {
                ActionInterruptArbiter.Arbitrate(
                    Context(Idle, elapsed: 0.25f, resistance: 3, currentTick: 8),
                    new[] { request },
                    new[] { During(Idle, Dodge, 5, 0.2f, 0.4f) });
            }

            RuntimeDiagnosticLogEvent accepted = FindEvent(events, "interrupt-request-accepted");
            RuntimeDiagnosticLogEvent decision = FindEvent(events, "interrupt-decision-accepted");

            Assert.AreEqual(RuntimeDiagnosticLogCategory.Action, accepted.Category);
            Assert.AreEqual("Locomotion.Idle", accepted.PreviousStatePath);
            Assert.AreEqual("Action.Dodge", accepted.StatePath);
            Assert.AreEqual(8, accepted.Step);
            StringAssert.Contains("request=Dodge", accepted.Context);
            StringAssert.Contains("policyIndex=0", accepted.Context);
            StringAssert.Contains("timing=DuringElapsedTimeWindow", accepted.Context);
            StringAssert.Contains("elapsed=0.250", accepted.Context);
            StringAssert.Contains("resistance=3", decision.Context);
            StringAssert.Contains("accepted=True", decision.Context);
        }

        [Test]
        public void PriorityBelowMinimumIsRejected()
        {
            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Idle),
                new[] { Request(Dodge, 4) },
                new[] { Always(Idle, Dodge, 5) });

            Assert.False(decision.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.PriorityTooLow, decision.RejectReason);
        }

        [Test]
        public void RejectedRequestWritesDetailedDiagnosticLog()
        {
            List<RuntimeDiagnosticLogEvent> events = new List<RuntimeDiagnosticLogEvent>();

            using (RuntimeDiagnosticLog.Capture(events.Add))
            {
                ActionInterruptArbiter.Arbitrate(
                    Context(Idle, elapsed: 0.1f, currentTick: 4),
                    new[] { Request(Dodge, 4) },
                    new[] { Always(Idle, Dodge, 5) });
            }

            RuntimeDiagnosticLogEvent rejected = FindEvent(events, "interrupt-request-rejected");
            RuntimeDiagnosticLogEvent decision = FindEvent(events, "interrupt-decision-rejected");

            StringAssert.Contains("reason=PriorityTooLow", rejected.Context);
            StringAssert.Contains("priority=4", rejected.Context);
            StringAssert.Contains("minPriority=5", rejected.Context);
            StringAssert.Contains("reject=PriorityTooLow", decision.Context);
        }

        [Test]
        public void CurrentResistanceBlocksRequest()
        {
            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Attack01, resistance: 80),
                new[] { Request(Dodge, 80) },
                new[] { Always(Attack01, Dodge, 1) });

            Assert.False(decision.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.BlockedByResistance, decision.RejectReason);
        }

        [Test]
        public void ForcePolicyBypassesCurrentResistance()
        {
            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Attack01, resistance: 100),
                new[] { Request(Death, 10, requestType: ActionRequestType.Death) },
                new[] { new ActionInterruptPolicy(Attack01, Death, 1, force: true) });

            Assert.True(decision.Accepted);
            Assert.AreEqual(Death, decision.TargetState);
        }

        [Test]
        public void NaturalExitWindowDoesNotSatisfyTimelinePolicyWindow()
        {
            ThirdPersonCharacterStateMachine.StateTimelineWindowFacts exitFacts =
                new ThirdPersonCharacterStateMachine.StateTimelineWindowFacts(
                    new ThirdPersonCharacterStateMachine.CharacterStateId("Action.Attack01"),
                    1f,
                    true,
                    0.5f,
                    false,
                    false,
                    false,
                    true,
                    0,
                    0,
                    0,
                    false,
                    "attack-exit");

            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Attack01, timelineFacts: exitFacts),
                new[] { Request(Dodge, 50) },
                new[] { new ActionInterruptPolicy(Attack01, Dodge, 1, windowId: "attack-exit") });

            Assert.False(decision.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.TimingNotSatisfied, decision.RejectReason);
        }

        [Test]
        public void RequestWindowSatisfiesTimelinePolicyWindow()
        {
            ThirdPersonCharacterStateMachine.StateTimelineWindowFacts cancelFacts =
                new ThirdPersonCharacterStateMachine.StateTimelineWindowFacts(
                    new ThirdPersonCharacterStateMachine.CharacterStateId("Action.Attack01"),
                    0.5f,
                    true,
                    0.25f,
                    false,
                    false,
                    true,
                    false,
                    0,
                    0,
                    0,
                    false,
                    "attack-dodge-cancel",
                    "attack-dodge-cancel");

            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Attack01, timelineFacts: cancelFacts),
                new[] { Request(Dodge, 50) },
                new[] { new ActionInterruptPolicy(Attack01, Dodge, 1, windowId: "attack-dodge-cancel") });

            Assert.True(decision.Accepted);
            Assert.AreEqual(Dodge, decision.TargetState);
        }

        [Test]
        public void AfterElapsedTimeRejectsBeforeThreshold()
        {
            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Attack01, elapsed: 0.19f),
                new[] { Request(Dodge, 20) },
                new[] { After(Attack01, Dodge, 1, 0.2f) });

            Assert.False(decision.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.TimingNotSatisfied, decision.RejectReason);
        }

        [Test]
        public void AfterElapsedTimeAcceptsAtThreshold()
        {
            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Attack01, elapsed: 0.2f),
                new[] { Request(Dodge, 20) },
                new[] { After(Attack01, Dodge, 1, 0.2f) });

            Assert.True(decision.Accepted);
            Assert.AreEqual(Dodge, decision.TargetState);
        }

        [Test]
        public void DuringElapsedTimeWindowRejectsBeforeWindow()
        {
            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Attack01, elapsed: 0.24f),
                new[] { Request(Dodge, 20) },
                new[] { During(Attack01, Dodge, 1, 0.25f, 0.45f) });

            Assert.False(decision.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.TimingNotSatisfied, decision.RejectReason);
        }

        [Test]
        public void DuringElapsedTimeWindowAcceptsInsideWindow()
        {
            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Attack01, elapsed: 0.3f),
                new[] { Request(Dodge, 20) },
                new[] { During(Attack01, Dodge, 1, 0.25f, 0.45f) });

            Assert.True(decision.Accepted);
            Assert.AreEqual(Dodge, decision.TargetState);
        }

        [Test]
        public void DuringElapsedTimeWindowRejectsAfterWindow()
        {
            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Attack01, elapsed: 0.46f),
                new[] { Request(Dodge, 20) },
                new[] { During(Attack01, Dodge, 1, 0.25f, 0.45f) });

            Assert.False(decision.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.TimingNotSatisfied, decision.RejectReason);
        }

        [Test]
        public void HighestPriorityAcceptableRequestWins()
        {
            ActionInterruptRequest dodge = Request(Dodge, 20, sourceOrder: 0);
            ActionInterruptRequest hitReact = Request(HitReact, 50, sourceOrder: 1, requestType: ActionRequestType.HitReact);

            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Attack01),
                new[] { dodge, hitReact },
                new[] { Always(Attack01, Dodge, 1), Always(Attack01, HitReact, 1) });

            Assert.True(decision.Accepted);
            Assert.AreEqual(HitReact, decision.TargetState);
            Assert.AreEqual(hitReact.RequestId, decision.SelectedRequest.RequestId);
        }

        [Test]
        public void SamePriorityUsesStableSourceOrder()
        {
            ActionInterruptRequest later = Request(HitReact, 50, sourceOrder: 2, requestType: ActionRequestType.HitReact);
            ActionInterruptRequest earlier = Request(Dodge, 50, sourceOrder: 1);

            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Attack01),
                new[] { later, earlier },
                new[] { Always(Attack01, Dodge, 1), Always(Attack01, HitReact, 1) });

            Assert.True(decision.Accepted);
            Assert.AreEqual(Dodge, decision.TargetState);
            Assert.AreEqual(earlier.RequestId, decision.SelectedRequest.RequestId);
        }

        [Test]
        public void SamePriorityAndSourceOrderUsesSubmissionOrder()
        {
            ActionInterruptRequest first = Request(Dodge, 50, sourceOrder: 1);
            ActionInterruptRequest second = Request(HitReact, 50, sourceOrder: 1, requestType: ActionRequestType.HitReact);

            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Attack01),
                new[] { first, second },
                new[] { Always(Attack01, Dodge, 1), Always(Attack01, HitReact, 1) });

            Assert.True(decision.Accepted);
            Assert.AreEqual(Dodge, decision.TargetState);
            Assert.AreEqual(first.RequestId, decision.SelectedRequest.RequestId);
        }

        [Test]
        public void InvalidPolicyIsReportedByValidator()
        {
            ActionInterruptPolicy policy = During(Attack01, Dodge, 1, 0.5f, 0.2f);

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(new[] { policy });

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("window end"));
        }

        [Test]
        public void EmptyPoliciesAreValidButAcceptNothing()
        {
            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(new ActionInterruptPolicy[0]);
            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Idle),
                new[] { Request(Dodge, 10) },
                new ActionInterruptPolicy[0]);

            Assert.False(result.HasErrors);
            Assert.False(decision.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.NoPolicy, decision.RejectReason);
        }

        [Test]
        public void ValidatorReportsNegativeMinPriorityAndInvalidTarget()
        {
            ActionInterruptPolicy policy = Always(Idle, ActionStateId.Empty, -1);

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(new[] { policy });

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("target state"));
            Assert.That(result.DescribeErrors(), Does.Contain("min priority"));
        }

        [Test]
        public void ValidatorWarnsAboutDuplicatePolicies()
        {
            ActionInterruptPolicy policy = Always(Idle, Dodge, 1);

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(new[] { policy, policy });

            Assert.False(result.HasErrors);
            Assert.That(result.Warnings, Is.Not.Empty);
        }

        [Test]
        public void RequestDoesNotNeedUnitySceneObjects()
        {
            ActionInterruptRequest request = Request(Dodge, 10);

            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                Context(Idle),
                new[] { request },
                new[] { Always(Idle, Dodge, 1) });

            Assert.True(decision.Accepted);
            AssertNoUnityObjectFields(typeof(ActionInterruptRequest));
            AssertNoUnityObjectFields(typeof(ActionInterruptContext));
            AssertNoUnityObjectFields(typeof(ActionInterruptPolicy));
        }

        [Test]
        public void ActionModuleDoesNotReferenceForbiddenRuntimeTypes()
        {
            string actionRoot = Path.Combine(Application.dataPath, "Scripts/Character/Action");
            string combined = string.Join("\n", Directory.GetFiles(actionRoot, "*.cs", SearchOption.AllDirectories)
                .Select(path => File.ReadAllText(path)));

            Assert.That(combined, Does.Not.Contain("Animancer"));
            Assert.That(combined, Does.Not.Contain("AnimationClip"));
            Assert.That(combined, Does.Not.Contain("Animator"));
            Assert.That(combined, Does.Not.Contain("CharacterController"));
            Assert.That(combined, Does.Not.Contain("Cinemachine"));
            Assert.That(combined, Does.Not.Contain("UnityEngine.InputSystem"));
            Assert.That(combined, Does.Not.Contain("BBBNexus"));
        }

        [Test]
        public void LocomotionMoveStopTransitionsDoNotDependOnActionArbiter()
        {
            string stateMachineRoot = Path.Combine(Application.dataPath, "Scripts/Character/StateMachine");
            string stateMachine = string.Join("\n", Directory.GetFiles(stateMachineRoot, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
            string presenter = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs"));

            Assert.That(stateMachine, Does.Not.Contain("ActionInterruptArbiter"));
            Assert.That(presenter, Does.Not.Contain("ActionInterruptArbiter"));
            Assert.That(stateMachine, Does.Contain("MoveStop"));
            Assert.That(stateMachine, Does.Contain("MoveStart"));
        }

        static ActionInterruptContext Context(
            ActionStateId state,
            float elapsed = 0f,
            int resistance = 0,
            int currentTick = 0,
            ThirdPersonCharacterStateMachine.StateTimelineWindowFacts timelineFacts = default)
        {
            return new ActionInterruptContext(state, elapsed, resistance, currentTick, timelineFacts);
        }

        static ActionInterruptRequest Request(
            ActionStateId target,
            int priority,
            int sourceOrder = 0,
            int expireTick = ActionInterruptRequest.NeverExpires,
            ActionRequestType requestType = ActionRequestType.Dodge)
        {
            return new ActionInterruptRequest(
                requestId: sourceOrder + priority + target.GetHashCode(),
                requestType: requestType,
                targetState: target,
                priority: priority,
                sourceOrder: sourceOrder,
                originTick: 0,
                expireTick: expireTick);
        }

        static ActionInterruptPolicy Always(ActionStateId from, ActionStateId target, int minPriority)
        {
            return new ActionInterruptPolicy(from, target, minPriority);
        }

        static ActionInterruptPolicy After(ActionStateId from, ActionStateId target, int minPriority, float elapsedTime)
        {
            return new ActionInterruptPolicy(from, target, minPriority, ActionInterruptTimingRule.AfterElapsedTime, elapsedTime);
        }

        static ActionInterruptPolicy During(ActionStateId from, ActionStateId target, int minPriority, float start, float end)
        {
            return new ActionInterruptPolicy(from, target, minPriority, ActionInterruptTimingRule.DuringElapsedTimeWindow, start, end);
        }

        static void AssertNoUnityObjectFields(System.Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                Assert.False(typeof(Object).IsAssignableFrom(fields[i].FieldType), $"{type.Name}.{fields[i].Name}");
                Assert.That(fields[i].FieldType.FullName, Does.Not.Contain("Animancer"));
            }
        }

        static RuntimeDiagnosticLogEvent FindEvent(List<RuntimeDiagnosticLogEvent> events, string message)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Message == message)
                    return events[i];
            }

            Assert.Fail($"Missing diagnostic event {message}");
            return default;
        }
    }
}
