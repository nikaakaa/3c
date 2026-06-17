using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThirdPersonAction;
using UnityEngine;

namespace ThirdPersonAction.Tests
{
    public sealed class ActionRuntimeStateTrackerTests
    {
        static readonly ActionStateId Attack01 = new ActionStateId("Action.Attack01");
        static readonly ActionStateId Dodge = new ActionStateId("Action.Dodge");

        [Test]
        public void DefaultStateIsActionNone()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();

            Assert.AreEqual(ActionRuntimeStateTracker.NoneState, tracker.CurrentState);
            Assert.AreEqual(0f, tracker.ElapsedSeconds);
            Assert.AreEqual(0, tracker.CurrentResistance);
            Assert.AreEqual(0, tracker.CurrentTick);
        }

        [Test]
        public void EnterStateSetsStateAndResistance()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();

            tracker.EnterState(Attack01, 30);

            Assert.AreEqual(Attack01, tracker.CurrentState);
            Assert.AreEqual(30, tracker.CurrentResistance);
            Assert.AreEqual(0f, tracker.ElapsedSeconds);
        }

        [Test]
        public void EnterStateResetsElapsedSeconds()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();
            tracker.EnterState(Attack01, 30);
            tracker.Tick(0.25f, 2);

            tracker.EnterState(Dodge, 40);

            Assert.AreEqual(Dodge, tracker.CurrentState);
            Assert.AreEqual(0f, tracker.ElapsedSeconds);
            Assert.AreEqual(2, tracker.CurrentTick);
        }

        [Test]
        public void EnterStateClampsNegativeResistance()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();

            tracker.EnterState(Attack01, -10);

            Assert.AreEqual(0, tracker.CurrentResistance);
        }

        [Test]
        public void EnterStateFallsBackWhenStateIsInvalid()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();

            tracker.EnterState(ActionStateId.Empty, 10);

            Assert.AreEqual(ActionRuntimeStateTracker.NoneState, tracker.CurrentState);
            Assert.AreEqual(10, tracker.CurrentResistance);
        }

        [Test]
        public void TickIncreasesElapsedSecondsAndUpdatesTick()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();
            tracker.EnterState(Attack01, 30);

            tracker.Tick(0.2f, 4);

            Assert.AreEqual(0.2f, tracker.ElapsedSeconds);
            Assert.AreEqual(4, tracker.CurrentTick);
        }

        [Test]
        public void TickDoesNotReduceElapsedSecondsWithNegativeDelta()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();
            tracker.EnterState(Attack01, 30);
            tracker.Tick(0.2f, 4);

            tracker.Tick(-0.1f, 5);

            Assert.AreEqual(0.2f, tracker.ElapsedSeconds);
            Assert.AreEqual(5, tracker.CurrentTick);
        }

        [Test]
        public void TickClampsNegativeTick()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();

            tracker.Tick(0.1f, -1);

            Assert.AreEqual(0, tracker.CurrentTick);
        }

        [Test]
        public void SnapshotOutputsCurrentFacts()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();
            tracker.EnterState(Attack01, 30);
            tracker.Tick(0.2f, 12);

            ActionRuntimeStateSnapshot snapshot = tracker.Snapshot;

            Assert.AreEqual(Attack01, snapshot.CurrentState);
            Assert.AreEqual(0.2f, snapshot.ElapsedSeconds);
            Assert.AreEqual(30, snapshot.CurrentResistance);
            Assert.AreEqual(12, snapshot.CurrentTick);
        }

        [Test]
        public void SnapshotClampsNegativeValues()
        {
            ActionRuntimeStateSnapshot snapshot = new ActionRuntimeStateSnapshot(Attack01, -0.1f, -2, -3);

            Assert.AreEqual(0f, snapshot.ElapsedSeconds);
            Assert.AreEqual(0, snapshot.CurrentResistance);
            Assert.AreEqual(0, snapshot.CurrentTick);
        }

        [Test]
        public void CreateInterruptContextOutputsCurrentFacts()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();
            tracker.EnterState(Attack01, 30);
            tracker.Tick(0.2f, 12);

            ActionInterruptContext context = tracker.CreateInterruptContext();

            Assert.AreEqual(Attack01, context.CurrentState);
            Assert.AreEqual(0.2f, context.CurrentStateElapsedSeconds);
            Assert.AreEqual(30, context.CurrentStateResistance);
            Assert.AreEqual(12, context.CurrentTick);
        }

        [Test]
        public void AcceptedDecisionUpdatesStateAndResistance()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();
            tracker.EnterState(Attack01, 30);
            tracker.Tick(0.2f, 4);

            tracker.ApplyDecision(Accepted(Dodge, 50), targetResistance: 40);

            Assert.AreEqual(Dodge, tracker.CurrentState);
            Assert.AreEqual(0f, tracker.ElapsedSeconds);
            Assert.AreEqual(40, tracker.CurrentResistance);
            Assert.AreEqual(4, tracker.CurrentTick);
        }

        [Test]
        public void AcceptedDecisionClampsTargetResistance()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();

            tracker.ApplyDecision(Accepted(Dodge, 50), targetResistance: -1);

            Assert.AreEqual(Dodge, tracker.CurrentState);
            Assert.AreEqual(0, tracker.CurrentResistance);
        }

        [Test]
        public void AcceptedDecisionWithInvalidTargetFallsBackToActionNone()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();

            tracker.ApplyDecision(Accepted(ActionStateId.Empty, 50), targetResistance: 10);

            Assert.AreEqual(ActionRuntimeStateTracker.NoneState, tracker.CurrentState);
            Assert.AreEqual(10, tracker.CurrentResistance);
        }

        [Test]
        public void RejectedDecisionDoesNotChangeFacts()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();
            tracker.EnterState(Attack01, 30);
            tracker.Tick(0.2f, 4);

            tracker.ApplyDecision(ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoPolicy), targetResistance: 99);

            Assert.AreEqual(Attack01, tracker.CurrentState);
            Assert.AreEqual(0.2f, tracker.ElapsedSeconds);
            Assert.AreEqual(30, tracker.CurrentResistance);
            Assert.AreEqual(4, tracker.CurrentTick);
        }

        [Test]
        public void TrackerDoesNotAutoExit()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();
            tracker.EnterState(Attack01, 30);

            tracker.Tick(100f, 100);

            Assert.AreEqual(Attack01, tracker.CurrentState);
            Assert.AreEqual(100f, tracker.ElapsedSeconds);
        }

        [Test]
        public void ArbiterAcceptedDecisionCanDriveTracker()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();
            tracker.EnterState(Attack01, 10);
            ActionInterruptRequest request = Request(Dodge, 50);
            ActionInterruptPolicy policy = new ActionInterruptPolicy(Attack01, Dodge, 1);

            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                tracker.CreateInterruptContext(),
                new[] { request },
                new[] { policy });
            tracker.ApplyDecision(decision, targetResistance: 40);

            Assert.True(decision.Accepted);
            Assert.AreEqual(Dodge, tracker.CurrentState);
            Assert.AreEqual(40, tracker.CurrentResistance);
        }

        [Test]
        public void ArbiterRejectedDecisionDoesNotDriveTracker()
        {
            ActionRuntimeStateTracker tracker = new ActionRuntimeStateTracker();
            tracker.EnterState(Attack01, 10);
            ActionInterruptRequest request = Request(Dodge, 1);
            ActionInterruptPolicy policy = new ActionInterruptPolicy(Attack01, Dodge, 10);

            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                tracker.CreateInterruptContext(),
                new[] { request },
                new[] { policy });
            tracker.ApplyDecision(decision, targetResistance: 40);

            Assert.False(decision.Accepted);
            Assert.AreEqual(Attack01, tracker.CurrentState);
            Assert.AreEqual(10, tracker.CurrentResistance);
        }

        [Test]
        public void LocomotionRuntimeDoesNotDependOnActionRuntimeStateTracker()
        {
            string movementRoot = Path.Combine(Application.dataPath, "Scripts/Character/Movement");
            string stateMachineRoot = Path.Combine(Application.dataPath, "Scripts/Character/StateMachine");
            string stateMachine = string.Join("\n", Directory.GetFiles(stateMachineRoot, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
            string locomotionRuntime = File.ReadAllText(Path.Combine(movementRoot, "Runtime/LocomotionRuntimeModule.cs"));
            string presenter = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Character/Animation/Runtime/CharacterAnimancerPresenter.cs"));

            Assert.That(stateMachine, Does.Not.Contain("ActionRuntimeStateTracker"));
            Assert.That(locomotionRuntime, Does.Not.Contain("ActionRuntimeStateTracker"));
            Assert.That(presenter, Does.Not.Contain("ActionRuntimeStateTracker"));
        }

        [Test]
        public void ActionRuntimeStateTrackerDoesNotNeedUnitySceneObjects()
        {
            AssertNoUnityObjectFields(typeof(ActionRuntimeStateSnapshot));
            AssertNoUnityObjectFields(typeof(ActionRuntimeStateTracker));
        }

        [Test]
        public void ActionRuntimeStateTrackerDoesNotReferenceForbiddenRuntimeTypes()
        {
            string actionRoot = Path.Combine(Application.dataPath, "Scripts/Character/Action");
            string combined = string.Join("\n", Directory.GetFiles(actionRoot, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

            Assert.That(combined, Does.Not.Contain("Animancer"));
            Assert.That(combined, Does.Not.Contain("AnimationClip"));
            Assert.That(combined, Does.Not.Contain("Animator"));
            Assert.That(combined, Does.Not.Contain("CharacterController"));
            Assert.That(combined, Does.Not.Contain("Cinemachine"));
            Assert.That(combined, Does.Not.Contain("UnityEngine.InputSystem"));
            Assert.That(combined, Does.Not.Contain("BBBNexus"));
        }

        static ActionInterruptDecision Accepted(ActionStateId target, int priority)
        {
            return ActionInterruptDecision.Accept(Request(target, priority));
        }

        static ActionInterruptRequest Request(ActionStateId target, int priority)
        {
            return new ActionInterruptRequest(
                requestId: priority + target.GetHashCode(),
                requestType: ActionRequestType.Dodge,
                targetState: target,
                priority: priority,
                sourceOrder: 0,
                originTick: 0);
        }

        static void AssertNoUnityObjectFields(System.Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
                Assert.False(typeof(Object).IsAssignableFrom(fields[i].FieldType), $"{type.Name}.{fields[i].Name}");
        }
    }
}
