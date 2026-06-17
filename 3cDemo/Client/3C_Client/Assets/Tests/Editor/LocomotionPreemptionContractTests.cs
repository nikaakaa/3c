using System.IO;
using System.Linq;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class LocomotionPreemptionContractTests
    {
        const string LocomotionStateGraphAssetPath = "Assets/Configs/3C/StateMachine/Locomotion/Corin/CorinLocomotionStateGraph.asset";

        [Test]
        public void LocomotionPreemptionFactDefaultsToNoneAndCarriesSourceFields()
        {
            LocomotionPreemptionFact none = LocomotionPreemptionFact.None;
            LocomotionPreemptionFact fact = LocomotionPreemptionFact.FullBodyActionStarted(
                CharacterStateIds.TurnBack,
                ActionStateIds.Dodge,
                42);

            Assert.False(none.HasPreemption);
            Assert.True(fact.HasPreemption);
            Assert.True(fact.MatchesSource(CharacterStateIds.TurnBack));
            Assert.False(fact.MatchesSource(CharacterStateIds.MoveLoop));
            Assert.AreEqual(CharacterStateIds.TurnBack, fact.SourceLocomotionState);
            Assert.AreEqual(ActionStateIds.Dodge, fact.SourceActionId);
            Assert.AreEqual(42, fact.SourceStep);
            Assert.AreEqual(LocomotionPreemptionReason.FullBodyActionStarted, fact.Reason);
        }

        [Test]
        public void LocomotionPreemptionFactDoesNotReferenceRuntimeObjects()
        {
            string source = ReadProjectFile("Assets/Scripts/Character/StateMachine/Model/CharacterRuntimeBlackboard.cs");
            string factSource = ExtractType(source, "LocomotionPreemptionFact");

            Assert.That(factSource, Does.Not.Contain("UnityEngine.Object"));
            Assert.That(factSource, Does.Not.Contain("MonoBehaviour"));
            Assert.That(factSource, Does.Not.Contain("Animancer"));
            Assert.That(factSource, Does.Not.Contain("InputAction"));
            Assert.That(factSource, Does.Not.Contain("Executor"));
        }

        [Test]
        public void FramePlanAndOutputCarryPreemptionOnlyAfterFullBodyClaimAccepted()
        {
            LocomotionPreemptionFact fact = LocomotionPreemptionFact.FullBodyActionStarted(
                CharacterStateIds.TurnBack,
                ActionStateIds.Dodge,
                7);
            CharacterFrameSubmission acceptedSubmission = CreateSubmission(
                BodyOccupancyClaim.FullBodyAction(7),
                fact,
                7);
            CharacterFrameSubmission rejectedSubmission = CreateSubmission(
                BodyOccupancyClaim.None(8),
                fact,
                8);

            CharacterFramePlan acceptedPlan = DefaultBodyArbiter.Instance.CreatePlan(in acceptedSubmission);
            CharacterFramePlan rejectedPlan = DefaultBodyArbiter.Instance.CreatePlan(in rejectedSubmission);
            CharacterFrameOutput output = new CharacterFrameOutput(acceptedSubmission, acceptedPlan);

            Assert.True(acceptedPlan.LocomotionPreemption.HasPreemption);
            Assert.False(rejectedPlan.LocomotionPreemption.HasPreemption);
            Assert.AreEqual(fact.SourceActionId, output.LocomotionPreemption.SourceActionId);
            Assert.True(output.RuntimeFacts.WriteLocomotionPreemption);
        }

        [Test]
        public void RuntimeBlackboardSnapshotAndRestorePreservePreemptionFact()
        {
            CharacterRuntimeBlackboard blackboard = new CharacterRuntimeBlackboard();
            LocomotionPreemptionFact fact = LocomotionPreemptionFact.FullBodyActionStarted(
                CharacterStateIds.TurnBack,
                ActionStateIds.Dodge,
                11);

            blackboard.WriteLocomotionPreemptionFact(in fact);
            CharacterRuntimeBlackboardRestoreState restoreState = blackboard.CaptureRestoreState();
            blackboard.WriteLocomotionPreemptionFact(LocomotionPreemptionFact.None);
            blackboard.Restore(in restoreState);

            Assert.True(blackboard.Snapshot.LocomotionPreemption.HasPreemption);
            Assert.AreEqual(ActionStateIds.Dodge, blackboard.Snapshot.LocomotionPreemption.SourceActionId);
            Assert.AreEqual(11, blackboard.Snapshot.LocomotionPreemption.SourceStep);
        }

        [Test]
        public void LocomotionPreemptionConditionReadsOnlyContextFacts()
        {
            LocomotionPreemptionFact fact = LocomotionPreemptionFact.FullBodyActionStarted(
                CharacterStateIds.TurnBack,
                ActionStateIds.Dodge,
                12);
            CharacterStateTransitionCondition condition = CharacterStateTransitionCondition.LocomotionPreemptionPending();
            CharacterStateTransitionConditionEvaluationResult accepted = EvaluateCondition(
                condition,
                Context(true, runtimeBlackboard: BlackboardWithPreemption(fact), currentStep: 12),
                CharacterStateIds.TurnBack);
            CharacterStateTransitionConditionEvaluationResult rejected = EvaluateCondition(
                condition,
                Context(true, runtimeBlackboard: BlackboardWithPreemption(fact), currentStep: 12),
                CharacterStateIds.MoveLoop);

            Assert.True(accepted.Passed);
            Assert.False(rejected.Passed);
            Assert.That(accepted.Trace.Context, Does.Contain("sourceAction=Action.Dodge"));
        }

        [Test]
        public void ConfiguredTurnBackPreemptionTransitionsAreHigherPriorityThanNaturalExit()
        {
            CharacterStateMachineDefinition definition = LoadConfiguredLocomotionStateGraphDefinition();
            CharacterStateTransitionDefinition[] preemptionTransitions = definition.Transitions
                .Where(transition =>
                    transition.FromStateId == CharacterStateIds.TurnBack.Value &&
                    transition.Conditions.Any(condition => condition.Kind == CharacterStateTransitionConditionKind.LocomotionPreemptionPending))
                .ToArray();
            CharacterStateTransitionDefinition naturalMoveLoop = definition.Transitions.First(transition =>
                transition.FromStateId == CharacterStateIds.TurnBack.Value &&
                transition.ToStateId == CharacterStateIds.MoveLoop &&
                transition.Conditions.Any(condition => condition.Kind == CharacterStateTransitionConditionKind.LocomotionAnimationCanExit));
            CharacterStateTransitionDefinition naturalIdle = definition.Transitions.First(transition =>
                transition.FromStateId == CharacterStateIds.TurnBack.Value &&
                transition.ToStateId == CharacterStateIds.Idle &&
                transition.Conditions.Any(condition => condition.Kind == CharacterStateTransitionConditionKind.LocomotionAnimationCanExit));

            Assert.AreEqual(2, preemptionTransitions.Length);
            Assert.True(preemptionTransitions.Any(transition => transition.ToStateId == CharacterStateIds.MoveLoop));
            Assert.True(preemptionTransitions.Any(transition => transition.ToStateId == CharacterStateIds.Idle));
            Assert.True(preemptionTransitions.All(transition => transition.Priority > naturalMoveLoop.Priority));
            Assert.True(preemptionTransitions.All(transition => transition.Priority > naturalIdle.Priority));
            Assert.False(definition.Nodes.Any(node => node.StateId == CharacterStateIds.Dodge));
        }

        [Test]
        public void TurnBackPreemptionWithMoveInputExitsToMoveLoopWithoutAnimationExit()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            EnterTurnBack(runner);
            LocomotionPreemptionFact fact = LocomotionPreemptionFact.FullBodyActionStarted(
                CharacterStateIds.TurnBack,
                ActionStateIds.Dodge,
                20);

            CharacterStateMachineFrame frame = runner.Tick(Context(
                true,
                runHeld: true,
                runtimeBlackboard: BlackboardWithPreemption(fact),
                currentStep: 20));

            Assert.AreEqual(CharacterStateIds.MoveLoop, frame.Snapshot.ActiveState);
            Assert.True(frame.ConditionTraces.Any(trace =>
                trace.ConditionKind == CharacterStateTransitionConditionKind.LocomotionPreemptionPending &&
                trace.Passed));
        }

        [Test]
        public void TurnBackPreemptionWithoutMoveInputExitsToIdleAndDoesNotRestoreTurnBack()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            EnterTurnBack(runner);
            LocomotionPreemptionFact fact = LocomotionPreemptionFact.FullBodyActionStarted(
                CharacterStateIds.TurnBack,
                ActionStateIds.Dodge,
                24);

            CharacterStateMachineFrame preempted = runner.Tick(Context(
                false,
                runtimeBlackboard: BlackboardWithPreemption(fact),
                currentStep: 24));
            CharacterStateMachineFrame next = runner.Tick(Context(false, currentStep: 25));

            Assert.AreEqual(CharacterStateIds.Idle, preempted.Snapshot.ActiveState);
            Assert.AreEqual(CharacterStateIds.Idle, next.Snapshot.ActiveState);
            Assert.False(next.ConditionTraces.Any(trace =>
                trace.ConditionKind == CharacterStateTransitionConditionKind.LocomotionPreemptionPending &&
                trace.Passed));
        }

        [Test]
        public void LocomotionFrameBuilderMarksPreemptionConsumedAndClearsPendingIntent()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            EnterTurnBack(runner);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero, true);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            MovementInputIntent intent = MovementInputIntent.FromRaw(input.Move, settings.InputDeadZone, true);
            LocomotionPreemptionFact preemption = LocomotionPreemptionFact.FullBodyActionStarted(
                CharacterStateIds.TurnBack,
                ActionStateIds.Dodge,
                31);
            LocomotionDecisionFacts facts = new LocomotionDecisionFacts(
                intent,
                BasicMovementGait.Run,
                BasicMovementPhaseFacts.None,
                new LocomotionSpatialFacts(Vector3.forward, Vector3.forward, Vector3.forward, Vector3.right),
                LocomotionTurnBackIntent.None);
            LocomotionDecisionFrame decisionFrame = new LocomotionDecisionFrame(
                input,
                settings,
                intent,
                facts,
                BasicMovementGait.Run);
            LocomotionFrameRuntimeState runtimeState = new LocomotionFrameRuntimeState(
                intent,
                BasicMovementGait.Run,
                false,
                BasicMovementGait.Walk,
                true,
                Vector3.forward,
                TurnBackIntent(29, 33, 180f, Vector3.back, Vector3.forward));
            LocomotionFrameBuilderInput builderInput = new LocomotionFrameBuilderInput(
                input,
                31,
                BasicMovementPhase.TurnBack,
                runner.Snapshot.StateTime,
                settings,
                CharacterInputRequestFact.None(InputRequestKind.Dodge),
                StateTimelineWindowFacts.None(CharacterStateIds.TurnBack),
                BlackboardWithPreemption(preemption),
                runtimeState,
                CharacterStateIds.TurnBack.Value);
            LocomotionFrameBuilder builder = new LocomotionFrameBuilder();

            bool evaluated = builder.TryEvaluatePreparedGameplayDecision(
                in decisionFrame,
                runner,
                in builderInput,
                out LocomotionStateDecisionFrame stateDecision,
                out LocomotionFrameBuilderResult result);

            Assert.True(evaluated);
            Assert.True(stateDecision.ConsumedLocomotionPreemption);
            Assert.AreEqual(CharacterStateIds.MoveLoop, stateDecision.StateFrame.Snapshot.ActiveState);
            Assert.False(result.RuntimeState.PendingTurnBackIntent.IsValid);
        }

        [Test]
        public void RuntimeConsumptionClearsFactAndTurnBackResidueInsideLocomotionRuntime()
        {
            string runtime = ReadProjectFile("Assets/Scripts/Character/Movement/Runtime/LocomotionFrameRuntime.cs");
            string stateStore = ReadProjectFile("Assets/Scripts/Character/Movement/Runtime/LocomotionRuntimeStateStore.cs");

            Assert.That(runtime, Does.Contain("stateDecision.ConsumedLocomotionPreemption"));
            Assert.That(runtime, Does.Contain("stateStore.ClearTurnBackPreemptionResidue()"));
            Assert.That(runtime, Does.Contain("LocomotionPreemptionFact none = LocomotionPreemptionFact.None"));
            Assert.That(runtime, Does.Contain("host.WriteLocomotionPreemptionFact(in none)"));
            Assert.That(stateStore, Does.Contain("PendingTurnBackIntent = LocomotionTurnBackIntent.None"));
            Assert.That(stateStore, Does.Contain("ResetMotionPlaybackWindow(BasicMovementPhase.TurnBack)"));
        }

        [Test]
        public void PreemptionBoundariesAvoidDodgeNodesFallbackAndDirectCleanup()
        {
            string graphAsset = ReadProjectFile(LocomotionStateGraphAssetPath);
            string submitter = ReadProjectFile("Assets/Scripts/Character/Action/Runtime/FullBodyActionFrameSubmitter.cs");
            string resolver = ReadProjectFile("Assets/Scripts/Character/Movement/Solver/TurnBack/TurnBackMotionResolver.cs");

            Assert.That(graphAsset, Does.Not.Contain("Action.Dodge"));
            Assert.That(graphAsset, Does.Not.Contain("fallback"));
            Assert.That(submitter, Does.Not.Contain("ClearTurnBackPreemptionResidue"));
            Assert.That(submitter, Does.Not.Contain("ResetMotionPlaybackWindow"));
            Assert.That(submitter, Does.Not.Contain("PendingTurnBackIntent ="));
            Assert.That(resolver, Does.Not.Contain("ActionLifecycle"));
            Assert.That(resolver, Does.Not.Contain("Action.Dodge"));
            Assert.That(resolver, Does.Not.Contain("Dodge"));
        }

        static CharacterFrameSubmission CreateSubmission(
            BodyOccupancyClaim claim,
            LocomotionPreemptionFact preemption,
            int step)
        {
            CharacterFrameArbitrationInput arbitrationInput = new CharacterFrameArbitrationInput(
                claim,
                CharacterFrameCandidateOutput.Locomotion(true, true, step),
                CharacterFrameCandidateOutput.FullBodyAction(true, true, step),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.UpperBody, step),
                step);
            CharacterStateMachineSnapshot snapshot = new CharacterStateMachineSnapshot(
                CharacterStateIds.TurnBack,
                0.25f,
                CharacterStateVariant.None,
                string.Empty,
                new[] { CharacterStateTag.Locomotion, CharacterStateTag.Movement });
            CharacterStateMachineFrame stateFrame = new CharacterStateMachineFrame(
                snapshot,
                true,
                true,
                false,
                InputRequestKind.Dodge,
                false,
                false,
                ActionMotionSpec.None(step),
                default,
                false,
                CharacterStatePayload.Empty);

            return new CharacterFrameSubmission(
                CharacterFrameSubmissionSource.CharacterRuntimeGraph,
                step,
                default,
                default,
                default,
                stateFrame,
                ActionMotionResolveResult.None(step),
                CharacterInputRequestFact.None(InputRequestKind.Dodge),
                ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoRequest),
                StateTimelineFactsTrace.None,
                CharacterStateMachineSnapshot.Inactive,
                false,
                CharacterFrameActionOutputSubmission.None(step),
                arbitrationInput,
                preemption);
        }

        static CharacterStateTransitionConditionEvaluationResult EvaluateCondition(
            CharacterStateTransitionCondition condition,
            in CharacterStateMachineContext context,
            CharacterStateId currentState)
        {
            CharacterStateTransitionDefinition transition = new CharacterStateTransitionDefinition(
                currentState.Value,
                CharacterStateIds.MoveLoop,
                0,
                condition);
            CharacterStateTransitionConditionEvaluationInput input = new CharacterStateTransitionConditionEvaluationInput(
                condition,
                in context,
                null,
                CharacterStateVariant.None,
                currentState,
                transition,
                0f,
                0.1f);
            return CharacterStateTransitionEvaluatorCollection.Default.Evaluate(in input);
        }

        static void EnterTurnBack(CharacterStateMachineRunner runner)
        {
            runner.Tick(Context(true, runHeld: true, currentStep: 1));
            runner.Tick(Context(true, canExit: true, runHeld: true, currentStep: 2));
            runner.Tick(Context(
                true,
                runHeld: true,
                request: TurnBackRequest(3, 5, Vector3.back),
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                turnBackIntent: TurnBackIntent(3, 5, 180f, Vector3.back, Vector3.forward),
                currentStep: 3));
            Assert.AreEqual(CharacterStateIds.TurnBack, runner.Snapshot.ActiveState);
        }

        static CharacterStateMachineContext Context(
            bool move,
            float deltaTime = 0.1f,
            bool canExit = false,
            bool runHeld = false,
            CharacterInputRequestFact request = default,
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard = default,
            Vector3 worldDirection = default,
            Vector3 facingForward = default,
            LocomotionTurnBackIntent turnBackIntent = default,
            int currentStep = 1)
        {
            Vector2 moveInput = move ? Vector2.up : Vector2.zero;
            MovementInputIntent intent = MovementInputIntent.FromRaw(moveInput, 0.1f, runHeld);
            Vector3 resolvedWorldDirection = move
                ? worldDirection.sqrMagnitude > 0.000001f ? worldDirection.normalized : Vector3.forward
                : Vector3.zero;
            Vector3 resolvedFacingForward = facingForward.sqrMagnitude > 0.000001f ? facingForward.normalized : Vector3.forward;
            CharacterInputRequestFact resolvedRequest = request.HasRequest ? request : CharacterInputRequestFact.None(InputRequestKind.Dodge);
            LocomotionDecisionFacts facts = new LocomotionDecisionFacts(
                intent,
                intent.HasMoveIntent ? intent.Gait : BasicMovementGait.Walk,
                new BasicMovementPhaseFacts(canExit),
                new LocomotionSpatialFacts(resolvedWorldDirection, resolvedFacingForward, Vector3.forward, Vector3.right),
                turnBackIntent);
            return new CharacterStateMachineContext(
                deltaTime,
                currentStep,
                in facts,
                resolvedRequest,
                runtimeBlackboard.LocomotionPreemption.HasPreemption ? runtimeBlackboard : CharacterRuntimeBlackboardSnapshot.Empty,
                StateTimelineWindowFacts.None(CharacterStateIds.TurnBack));
        }

        static CharacterRuntimeBlackboardSnapshot BlackboardWithPreemption(LocomotionPreemptionFact fact)
        {
            return new CharacterRuntimeBlackboardSnapshot(
                CharacterRuntimeLocomotionFacts.Default,
                CharacterRuntimeActionFacts.Default,
                CharacterRuntimeAnimationFacts.Default,
                fact,
                CharacterRuntimeDebugFacts.Record("LocomotionPreemption", fact.SourceStep));
        }

        static LocomotionTurnBackIntent TurnBackIntent(
            int originStep,
            int expireStep,
            float angle,
            Vector3 worldDirection,
            Vector3 facingForward)
        {
            return new LocomotionTurnBackIntent(
                true,
                originStep,
                expireStep,
                angle,
                120f,
                worldDirection,
                facingForward);
        }

        static CharacterInputRequestFact TurnBackRequest(int originStep, int expireStep, Vector3 worldDirection)
        {
            return new CharacterInputRequestFact(
                true,
                InputRequestKind.TurnBack,
                originStep,
                expireStep,
                20,
                CharacterStateVariant.Directional,
                worldDirection);
        }

        static CharacterStateMachineRunner CreateRunner()
        {
            return new CharacterStateMachineRunner(LoadConfiguredLocomotionStateGraphDefinition());
        }

        static CharacterStateMachineDefinition LoadConfiguredLocomotionStateGraphDefinition()
        {
            CharacterStateMachineDefinitionSO asset = AssetDatabase.LoadAssetAtPath<CharacterStateMachineDefinitionSO>(
                LocomotionStateGraphAssetPath);
            Assert.NotNull(asset);
            return asset.ToDefinition();
        }

        static string ReadProjectFile(string relativePath)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return File.ReadAllText(Path.Combine(root, relativePath));
        }

        static string ExtractType(string source, string typeName)
        {
            string marker = $"struct {typeName}";
            int name = source.IndexOf(marker, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(name, 0, typeName);
            int start = source.LastIndexOf("public", name, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, typeName);
            int brace = source.IndexOf('{', name);
            Assert.GreaterOrEqual(brace, 0, typeName);

            int depth = 0;
            for (int i = brace; i < source.Length; i++)
            {
                if (source[i] == '{')
                    depth++;
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return source.Substring(start, i - start + 1);
                }
            }

            Assert.Fail($"Could not extract {typeName}.");
            return string.Empty;
        }
    }
}
