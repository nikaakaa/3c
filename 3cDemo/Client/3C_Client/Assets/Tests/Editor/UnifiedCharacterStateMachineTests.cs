using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Animancer;
using Animancer.TransitionLibraries;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonAnimation;
using ThirdPersonAnimation.EditorTools;
using ThirdPersonCamera;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonDiagnostics;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class UnifiedCharacterStateMachineTests
    {
        const string LocomotionStateGraphAssetPath = "Assets/Configs/3C/StateMachine/Locomotion/Corin/CorinLocomotionStateGraph.asset";

        [Test]
        public void ConfiguredStateMachineInitialStateIsLocomotionIdle()
        {
            CharacterStateMachineRunner runner = CreateRunner();

            Assert.AreEqual(CharacterStateIds.Idle, runner.Snapshot.ActiveState);
            Assert.AreEqual("Locomotion.Idle", runner.Snapshot.ActivePath);
            CharacterStateMachineSnapshot snapshot = runner.Snapshot;
            Assert.AreEqual(BasicMovementPhase.Idle, CharacterStateDomainView.FromSnapshot(in snapshot).LocomotionPhase);
        }

        [Test]
        public void ConfiguredLocomotionStateGraphContainsOnlyLocomotionStates()
        {
            CharacterStateMachineDefinition definition = LoadConfiguredLocomotionStateGraphDefinition();
            string[] ids = definition.Nodes.Select(node => node.StateId.Value).ToArray();

            CollectionAssert.AreEquivalent(
                new[]
                {
                    "Locomotion.Idle",
                    "Locomotion.MoveStart",
                    "Locomotion.MoveLoop",
                    "Locomotion.MoveStop",
                    "Locomotion.TurnBack"
                },
                ids);
            CollectionAssert.DoesNotContain(ids, "Locomotion.TurnInPlace");
            Assert.False(ids.Any(id => id.StartsWith("Action.", System.StringComparison.Ordinal)));
        }

        [Test]
        public void LocomotionDecisionFactsDefaultIsPureEmptyData()
        {
            LocomotionDecisionFacts facts = LocomotionDecisionFacts.Empty;

            Assert.False(facts.HasMoveIntent);
            Assert.AreEqual(BasicMovementGait.Walk, facts.GaitCandidate);
            Assert.False(facts.TurnBackIntent.IsValid);
            Assert.AreEqual(Vector3.zero, facts.SpatialFacts.WorldMoveDirection);
            Assert.AreEqual(Vector3.zero, facts.SpatialFacts.FacingForward);
        }

        [Test]
        public void LocomotionSpatialFactsNormalizePlanarDirections()
        {
            LocomotionSpatialFacts facts = new LocomotionSpatialFacts(
                new Vector3(0f, 4f, -3f),
                new Vector3(2f, 5f, 0f),
                new Vector3(0f, 9f, 4f),
                new Vector3(3f, -8f, 0f));

            Assert.AreEqual(Vector3.back, facts.WorldMoveDirection);
            Assert.AreEqual(Vector3.right, facts.FacingForward);
            Assert.AreEqual(Vector3.forward, facts.CameraPlanarForward);
            Assert.AreEqual(Vector3.right, facts.CameraPlanarRight);
        }

        [Test]
        public void LocomotionTurnBackIntentHonorsStepWindow()
        {
            LocomotionTurnBackIntent intent = LocomotionTurnBackIntent.Capture(
                10,
                2,
                180f,
                120f,
                Vector3.back,
                Vector3.forward);

            Assert.True(intent.IsValidAt(10));
            Assert.True(intent.IsValidAt(11));
            Assert.True(intent.IsValidAt(12));
            Assert.False(intent.IsValidAt(13));
            Assert.AreEqual(Vector3.back, intent.WorldMoveDirection);
            Assert.AreEqual(Vector3.forward, intent.FacingForward);
        }

        [Test]
        public void TurnBackMotionPolicyDefaultDeclaresRunLoopBakedMotionProfile()
        {
            TurnBackMotionPolicy policy = TurnBackMotionPolicy.Default;

            Assert.True(policy.IsEnabled);
            Assert.AreEqual(TurnBackMotionPolicy.DefaultAliasKey, policy.AliasKey);
            Assert.AreEqual(BasicMovementPhase.MoveLoop, policy.EntryPhase);
            Assert.AreEqual(BasicMovementGait.Run, policy.EntryGait);
            Assert.AreEqual(TurnBackMotionYawSource.BakedMotionProfile, policy.YawSource);
            Assert.AreEqual(TurnBackMotionTranslationSource.BakedMotionProfile, policy.TranslationSource);
            Assert.True(policy.SuppressInputRotation);
            Assert.True(policy.SuppressInputPlanarMovement);
            Assert.AreEqual(TurnBackMotionPolicy.DefaultTurnCompleteNormalizedTime, policy.TurnCompleteNormalizedTime);
            Assert.AreEqual(0.08f, policy.EnterFadeDuration);
            Assert.AreEqual(0f, policy.StartNormalizedTime);
            Assert.AreEqual(TurnBackMotionPolicy.DefaultTurnCompleteNormalizedTime, policy.LockInputNormalizedTime);
            Assert.AreEqual(TurnBackMotionPolicy.DefaultTurnCompleteNormalizedTime, policy.ExitNormalizedTime);
            Assert.True(policy.HasBakedMotionProfile);
            Assert.AreEqual(TurnBackMotionPolicy.DefaultBakedMotionProfileId, policy.BakedMotionProfileId);
        }

        [Test]
        public void TurnBackMotionPolicyCarriesBakedProfileIdAsPureData()
        {
            TurnBackMotionPolicy policy = new TurnBackMotionPolicy(
                "Locomotion.Turn.Back",
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                TurnBackMotionYawSource.BakedMotionProfile,
                TurnBackMotionTranslationSource.BakedMotionProfile,
                true,
                true,
                0.5f,
                0.1f,
                0.05f,
                0.4f,
                0.5f,
                "Configs/3C/Animation/Locomotion/Corin/Bake/Generic/TurnBack");

            Assert.True(policy.HasBakedMotionProfile);
            Assert.AreEqual("Configs/3C/Animation/Locomotion/Corin/Bake/Generic/TurnBack", policy.BakedMotionProfileId);
            Assert.AreEqual(TurnBackMotionYawSource.BakedMotionProfile, policy.YawSource);
            Assert.AreEqual(TurnBackMotionTranslationSource.BakedMotionProfile, policy.TranslationSource);
        }

        [Test]
        public void TurnBackMotionPolicyClampsInvalidTimingValues()
        {
            TurnBackMotionPolicy policy = new TurnBackMotionPolicy(
                "Locomotion.Turn.Back",
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                TurnBackMotionYawSource.BakedMotionProfile,
                TurnBackMotionTranslationSource.None,
                true,
                true,
                2f,
                -1f,
                -0.5f,
                2f,
                3f,
                string.Empty);

            Assert.AreEqual(1f, policy.TurnCompleteNormalizedTime);
            Assert.AreEqual(0f, policy.EnterFadeDuration);
            Assert.AreEqual(0f, policy.StartNormalizedTime);
            Assert.AreEqual(1f, policy.LockInputNormalizedTime);
            Assert.AreEqual(1f, policy.ExitNormalizedTime);
        }

        [Test]
        public void StateTimelineSamplerSeparatesExitAndRequestWindows()
        {
            StateTimelinePolicyDefinition policy = new StateTimelinePolicyDefinition(
                "Action.Attack01",
                0,
                0,
                new[]
                {
                    new StateTimelineWindowDefinition(
                        "attack-exit",
                        StateTimelineWindowKind.Exit,
                        StateTimelineTimeDomain.Normalized,
                        0.8f,
                        1f,
                        factId: TimelineFactIds.NaturalExitReady.Value),
                    new StateTimelineWindowDefinition(
                        "attack-dodge-cancel",
                        StateTimelineWindowKind.Cancel,
                        StateTimelineTimeDomain.Normalized,
                        0.2f,
                        0.6f,
                        minPriority: 30,
                        requestType: ActionRequestType.Dodge,
                        factId: TimelineFactIds.CancelableToDodge.Value)
                });

            StateTimelineWindowFacts exitFacts = StateTimelineSampler.Sample(
                in policy,
                0.9f,
                true,
                0.9f,
                ActionRequestType.Dodge);
            StateTimelineWindowFacts cancelFacts = StateTimelineSampler.Sample(
                in policy,
                0.4f,
                true,
                0.4f,
                ActionRequestType.Dodge);
            StateTimelineWindowFacts currentFacts = StateTimelineSampler.Sample(
                in policy,
                0.4f,
                true,
                0.4f,
                ActionRequestType.None);

            Assert.True(exitFacts.ExitWindowActive);
            Assert.False(exitFacts.InterruptWindowActive);
            Assert.AreEqual("attack-exit", exitFacts.ActiveWindowIds);
            Assert.AreEqual(TimelineFactIds.NaturalExitReady.Value, exitFacts.ActiveFactIds);
            Assert.True(exitFacts.Contains(TimelineFactIds.NaturalExitReady));
            Assert.False(exitFacts.ContainsRequestFact(TimelineFactIds.NaturalExitReady));
            Assert.IsEmpty(exitFacts.RequestWindowIds);
            Assert.False(exitFacts.HasRequestWindow);

            Assert.False(cancelFacts.ExitWindowActive);
            Assert.True(cancelFacts.InterruptWindowActive);
            Assert.AreEqual("attack-dodge-cancel", cancelFacts.ActiveWindowIds);
            Assert.AreEqual("attack-dodge-cancel", cancelFacts.RequestWindowIds);
            Assert.AreEqual(TimelineFactIds.CancelableToDodge.Value, cancelFacts.ActiveFactIds);
            Assert.AreEqual(TimelineFactIds.CancelableToDodge.Value, cancelFacts.RequestFactIds);
            Assert.True(cancelFacts.Contains(TimelineFactIds.CancelableToDodge));
            Assert.True(cancelFacts.ContainsRequestFact(TimelineFactIds.CancelableToDodge));
            CollectionAssert.AreEqual(
                new[] { TimelineFactIds.CancelableToDodge },
                cancelFacts.EnumerateActiveFacts().ToArray());
            Assert.True(cancelFacts.HasRequestWindow);
            Assert.AreEqual(30, cancelFacts.MinPriority);

            Assert.AreEqual("attack-dodge-cancel", currentFacts.ActiveWindowIds);
            Assert.AreEqual("attack-dodge-cancel", currentFacts.RequestWindowIds);
            Assert.AreEqual(TimelineFactIds.CancelableToDodge.Value, currentFacts.RequestFactIds);
            Assert.True(currentFacts.ContainsRequestFact(TimelineFactIds.CancelableToDodge));
        }

        [Test]
        public void StateTimelineSamplerCoversBoundariesAndSecondsDomain()
        {
            StateTimelinePolicyDefinition normalizedPolicy = new StateTimelinePolicyDefinition(
                "Action.Attack01",
                1,
                2,
                new[]
                {
                    new StateTimelineWindowDefinition(
                        "attack-motion",
                        StateTimelineWindowKind.Motion,
                        StateTimelineTimeDomain.Normalized,
                        0.25f,
                        0.5f,
                        priority: 3,
                        resistance: 4,
                        factId: TimelineFactIds.MotionActive.Value)
                });
            StateTimelinePolicyDefinition secondsPolicy = new StateTimelinePolicyDefinition(
                "Action.Attack01",
                0,
                0,
                new[]
                {
                    new StateTimelineWindowDefinition(
                        "input-lock",
                        StateTimelineWindowKind.InputLock,
                        StateTimelineTimeDomain.Seconds,
                        0.1f,
                        0.2f,
                        factId: TimelineFactIds.InputLocked.Value),
                    new StateTimelineWindowDefinition(
                        "dodge-cancel",
                        StateTimelineWindowKind.Cancel,
                        StateTimelineTimeDomain.Seconds,
                        0.1f,
                        0.2f,
                        minPriority: 8,
                        force: true,
                        requestType: ActionRequestType.Dodge,
                        factId: TimelineFactIds.CancelableToDodge.Value)
                });

            Assert.False(StateTimelineSampler.Sample(in normalizedPolicy, 0.249f, true, 0f).MotionWindowActive);
            Assert.True(StateTimelineSampler.Sample(in normalizedPolicy, 0.25f, true, 0f).MotionWindowActive);
            Assert.True(StateTimelineSampler.Sample(in normalizedPolicy, 0.4f, true, 0f).MotionWindowActive);
            Assert.True(StateTimelineSampler.Sample(in normalizedPolicy, 0.5f, true, 0f).MotionWindowActive);
            Assert.False(StateTimelineSampler.Sample(in normalizedPolicy, 0.501f, true, 0f).MotionWindowActive);

            StateTimelineWindowFacts facts = StateTimelineSampler.Sample(
                in secondsPolicy,
                0f,
                false,
                0.15f,
                ActionRequestType.Dodge);

            Assert.True(facts.InputLockWindowActive);
            Assert.True(facts.InterruptWindowActive);
            Assert.True(facts.Contains(TimelineFactIds.InputLocked));
            Assert.True(facts.ContainsRequestFact(TimelineFactIds.CancelableToDodge));
            Assert.AreEqual(8, facts.MinPriority);
            Assert.True(facts.Force);
        }

        [Test]
        public void StateTimelineValidatorRequiresRequestTypeForInterruptWindows()
        {
            StateTimelinePolicyDefinition policy = new StateTimelinePolicyDefinition(
                "Action.Attack01",
                0,
                0,
                new[]
                {
                    new StateTimelineWindowDefinition(
                        "attack-cancel",
                        StateTimelineWindowKind.Cancel,
                        StateTimelineTimeDomain.Normalized,
                        0.2f,
                        0.6f,
                        factId: TimelineFactIds.CancelableToDodge.Value)
                });

            StateTimelinePolicyValidationResult result = StateTimelinePolicyValidator.Validate(policy);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("request type"));
        }

        [Test]
        public void StateTimelineValidatorRejectsInvalidKindAndTimeDomain()
        {
            StateTimelinePolicyDefinition policy = new StateTimelinePolicyDefinition(
                "Action.Attack01",
                0,
                0,
                new[]
                {
                    new StateTimelineWindowDefinition(
                        "bad-window",
                        (StateTimelineWindowKind)999,
                        (StateTimelineTimeDomain)999,
                        0f,
                        1f,
                        factId: TimelineFactIds.MotionActive.Value)
                });

            StateTimelinePolicyValidationResult result = StateTimelinePolicyValidator.Validate(policy);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("kind"));
            Assert.That(result.DescribeErrors(), Does.Contain("time domain"));
        }

        [Test]
        public void BasicLocomotionPipelineUsesDecisionFactsWithoutRecomputingInputOrCamera()
        {
            BasicLocomotionPipeline pipeline = new BasicLocomotionPipeline();
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero, false);
            MovementInputIntent intent = new MovementInputIntent(Vector2.left, Vector2.left, 1f, true, BasicMovementGait.Run);
            LocomotionDecisionFacts facts = new LocomotionDecisionFacts(
                intent,
                BasicMovementGait.Run,
                BasicMovementPhaseFacts.None,
                new LocomotionSpatialFacts(Vector3.right, Vector3.forward, Vector3.forward, Vector3.right),
                LocomotionTurnBackIntent.None);

            BasicLocomotionFrame frame = pipeline.Tick(
                in input,
                BasicMovementSettings.FromConfig(null),
                in facts,
                BasicMovementPhase.MoveLoop,
                BasicMovementMotionFacts.None(BasicMovementPhase.MoveLoop),
                BasicMovementGait.Run);

            Assert.AreEqual(Vector2.left, frame.Intent.RawInput);
            Assert.AreEqual(Vector3.right, frame.WorldDirection);
            Assert.AreEqual(Vector3.right, frame.Command.WorldDirection);
            Assert.AreEqual(BasicMovementGait.Run, frame.Command.Gait);
        }

        [Test]
        public void MovementInputEntersMoveStart()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            CharacterStateMachineFrame frame = runner.Tick(Context(move: true));

            Assert.AreEqual(CharacterStateIds.MoveStart, frame.Snapshot.ActiveState);
            Assert.True(frame.ExecuteBasicMovement);
        }

        [Test]
        public void MoveStartCanExitToMoveLoop()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true));

            CharacterStateMachineFrame frame = runner.Tick(Context(move: true, canExit: true));

            Assert.AreEqual(CharacterStateIds.MoveLoop, frame.Snapshot.ActiveState);
            Assert.AreEqual(BasicMovementPhase.MoveLoop, frame.LocomotionPhase);
        }

        [Test]
        public void NoMoveInputEntersMoveStopAndThenIdle()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true));
            runner.Tick(Context(move: true, canExit: true));

            CharacterStateMachineFrame stop = runner.Tick(Context(move: false));
            CharacterStateMachineFrame idle = runner.Tick(Context(move: false, canExit: true));

            Assert.AreEqual(CharacterStateIds.MoveStop, stop.Snapshot.ActiveState);
            Assert.AreEqual(CharacterStateIds.Idle, idle.Snapshot.ActiveState);
            Assert.True(idle.ResetRunLatch);
        }

        [Test]
        public void MoveStopCanReEnterMoveStart()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true));
            runner.Tick(Context(move: true, canExit: true));
            runner.Tick(Context(move: false));

            CharacterStateMachineFrame frame = runner.Tick(Context(move: true));

            Assert.AreEqual(CharacterStateIds.MoveStart, frame.Snapshot.ActiveState);
        }

        [Test]
        public void ConfiguredStateMachineRoutesMoveStartAndMoveLoopToTurnBackOnly()
        {
            CharacterStateMachineDefinition definition = LoadConfiguredLocomotionStateGraphDefinition();
            string[] sources = definition.Transitions
                .Where(transition => transition.ToStateId == CharacterStateIds.TurnBack)
                .Select(transition => transition.FromStateId)
                .ToArray();

            CollectionAssert.Contains(sources, CharacterStateIds.MoveStart.Value);
            CollectionAssert.Contains(sources, CharacterStateIds.MoveLoop.Value);
            CollectionAssert.DoesNotContain(sources, CharacterStateIds.MoveStop.Value);
            CollectionAssert.DoesNotContain(sources, CharacterStateIds.Idle.Value);
        }

        [Test]
        public void ConfiguredStateMachineAssetUsesFormalTurnBackState()
        {
            CharacterStateMachineDefinitionSO asset = AssetDatabase.LoadAssetAtPath<CharacterStateMachineDefinitionSO>(
                LocomotionStateGraphAssetPath);
            Assert.NotNull(asset);

            CharacterStateMachineDefinition definition = asset.ToDefinition();
            Assert.True(definition.TryGetNode(CharacterStateIds.TurnBack, out CharacterStateNodeDefinition turnBack));
            Assert.True(turnBack.HasModule(CharacterStateModuleType.LocomotionAnimationAlias));
            Assert.True(turnBack.HasModule(CharacterStateModuleType.TurnBackMotionPolicy));
            Assert.True(turnBack.TryResolveAnimationBinding(
                CharacterStateVariant.None,
                out CharacterStateAnimationBinding binding,
                out CharacterStatePlaybackFactSource playbackFactSource));
            Assert.AreEqual(CharacterStatePlaybackFactSource.Locomotion, playbackFactSource);
            Assert.AreEqual("Locomotion.Turn.Back", binding.KeyValue);
            Assert.True(turnBack.TryGetTurnBackMotionPolicy(out TurnBackMotionPolicy policy));
            Assert.AreEqual("Locomotion.Turn.Back", policy.AliasKey);
            Assert.AreEqual(TurnBackMotionYawSource.BakedMotionProfile, policy.YawSource);
            Assert.AreEqual(TurnBackMotionTranslationSource.BakedMotionProfile, policy.TranslationSource);
            Assert.True(policy.SuppressInputRotation);
            Assert.True(policy.SuppressInputPlanarMovement);
            Assert.True(policy.HasBakedMotionProfile);
            Assert.AreEqual(TurnBackMotionPolicy.DefaultBakedMotionProfileId, policy.BakedMotionProfileId);

            string[] sources = definition.Transitions
                .Where(transition => transition.ToStateId == CharacterStateIds.TurnBack)
                .Select(transition => transition.FromStateId)
                .ToArray();
            CollectionAssert.AreEquivalent(new[] { CharacterStateIds.MoveStart.Value, CharacterStateIds.MoveLoop.Value }, sources);
        }

        [Test]
        public void ConfiguredLocomotionStateGraphNodesExposeExplicitCapabilityModules()
        {
            CharacterStateMachineDefinition definition = LoadConfiguredLocomotionStateGraphDefinition();

            Assert.True(definition.TryGetNode(CharacterStateIds.MoveLoop, out CharacterStateNodeDefinition moveLoop));
            Assert.True(moveLoop.HasModule(CharacterStateModuleType.LocomotionPhase));
            Assert.True(moveLoop.HasModule(CharacterStateModuleType.InputDrivenMotion));
            Assert.False(moveLoop.HasModule(CharacterStateModuleType.ActionAnimation));

            Assert.True(definition.TryGetNode(CharacterStateIds.TurnBack, out CharacterStateNodeDefinition turnBack));
            Assert.True(turnBack.HasModule(CharacterStateModuleType.LocomotionAnimationAlias));
            Assert.True(turnBack.HasModule(CharacterStateModuleType.TurnBackMotionPolicy));
            Assert.True(turnBack.TryResolveAnimationBinding(
                CharacterStateVariant.None,
                out CharacterStateAnimationBinding turnBackBinding,
                out CharacterStatePlaybackFactSource turnBackSource));
            Assert.AreEqual(CharacterStatePlaybackFactSource.Locomotion, turnBackSource);
            Assert.AreEqual("Locomotion.Turn.Back", turnBackBinding.TimelineBindingKey);

            Assert.False(definition.TryGetNode(CharacterStateIds.Dodge, out _));
            Assert.False(definition.Nodes.Any(node => node.HasModule(CharacterStateModuleType.ConfiguredActionMotion)));
            Assert.False(definition.Nodes.Any(node => node.HasModule(CharacterStateModuleType.ActionAnimation)));
        }

        [Test]
        public void ConfiguredLocomotionGraphProjectsToGraphAndMetadata()
        {
            CharacterStateMachineDefinition definition = LoadConfiguredLocomotionStateGraphDefinition();
            StateGraphDefinition graph = definition.Graph;
            CharacterStateMetadataSet metadata = definition.CharacterMetadata;

            Assert.AreEqual(new StateGraphNodeId(definition.InitialState.Value), graph.InitialNodeId);
            Assert.AreEqual(definition.Nodes.Count, graph.Nodes.Count);
            Assert.AreEqual(definition.Transitions.Count, graph.Transitions.Count);

            Assert.True(graph.TryGetNode(new StateGraphNodeId(CharacterStateIds.MoveLoop.Value), out StateGraphNode moveLoopNode));
            Assert.AreEqual(new StateGraphNodeId(string.Empty), moveLoopNode.ParentId);
            Assert.AreEqual("MoveLoop", moveLoopNode.PathSegment);

            Assert.True(metadata.TryGetNode(new StateGraphNodeId(CharacterStateIds.MoveLoop.Value), out CharacterStateNodeMetadata moveLoopMetadata));
            Assert.True(moveLoopMetadata.HasCapability(CharacterStateModuleType.LocomotionPhase));
            Assert.True(moveLoopMetadata.HasCapability(CharacterStateModuleType.InputDrivenMotion));
            Assert.AreEqual(BasicMovementPhase.MoveLoop, moveLoopMetadata.LocomotionPhase);

            Assert.False(metadata.TryGetNode(new StateGraphNodeId(CharacterStateIds.Dodge.Value), out _));
        }

        [Test]
        public void StateGraphProjectionPreservesDefaultTransitionEdges()
        {
            CharacterStateMachineDefinition definition = LoadConfiguredStateMachineDefinition();
            string[] legacyEdges = definition.Transitions
                .Select(transition => $"{transition.FromStateId}->{transition.ToStateId.Value}:{transition.Priority}:{string.Join(",", transition.Conditions.Select(condition => condition.Kind.ToString()))}")
                .OrderBy(edge => edge)
                .ToArray();
            string[] graphEdges = definition.Graph.Transitions
                .Select(transition => $"{transition.FromNodeId}->{transition.ToNodeId.Value}:{transition.Priority}:{string.Join(",", transition.Conditions.Select(condition => condition.Key))}")
                .OrderBy(edge => edge)
                .ToArray();

            CollectionAssert.AreEqual(legacyEdges, graphEdges);
        }

        [Test]
        public void CharacterStateDomainViewCanBeDerivedFromCharacterMetadata()
        {
            CharacterStateMachineDefinition definition = LoadConfiguredStateMachineDefinition();
            Assert.True(definition.TryGetNode(CharacterStateIds.MoveLoop, out CharacterStateNodeDefinition moveLoopNode));
            CharacterStateMachineSnapshot snapshot = new CharacterStateMachineSnapshot(
                CharacterStateIds.MoveLoop,
                0.1f,
                CharacterStateVariant.None,
                string.Empty,
                new[] { CharacterStateTag.Locomotion, CharacterStateTag.Movement });
            CharacterStateDomainView legacyView = CharacterStateDomainView.FromSnapshotAndNode(in snapshot, moveLoopNode);

            Assert.True(definition.CharacterMetadata.TryDeriveStateDomainView(in snapshot, out CharacterStateDomainView view));

            Assert.AreEqual(legacyView.IsAction, view.IsAction);
            Assert.AreEqual(legacyView.IsLocomotion, view.IsLocomotion);
            Assert.AreEqual(legacyView.Owner.Kind, view.Owner.Kind);
            Assert.AreEqual(legacyView.ActionState, view.ActionState);
            Assert.AreEqual(legacyView.LocomotionPhase, view.LocomotionPhase);
        }

        [Test]
        public void GenericGraphModelDoesNotReferenceCharacterBusinessTypes()
        {
            string graphRoot = Path.Combine(Application.dataPath, "Scripts/Character/StateMachine/Model/Graph");
            string graphSources = string.Join("\n", Directory.GetFiles(graphRoot, "*.cs", SearchOption.AllDirectories)
                .Select(path => File.ReadAllText(path, System.Text.Encoding.UTF8)));

            Assert.That(graphSources, Does.Not.Contain("Dodge"));
            Assert.That(graphSources, Does.Not.Contain("TurnBack"));
            Assert.That(graphSources, Does.Not.Contain("RunLatch"));
            Assert.That(graphSources, Does.Not.Contain("BasicMovementGait"));
            Assert.That(graphSources, Does.Not.Contain("ActionMovementCommand"));
            Assert.That(graphSources, Does.Not.Contain("CharacterStateOwner"));
            Assert.That(graphSources, Does.Not.Contain("ActionStateId"));
            Assert.That(graphSources, Does.Not.Contain("CharacterStateModuleType"));
            Assert.That(graphSources, Does.Not.Contain("ThirdPersonAction"));
            Assert.That(graphSources, Does.Not.Contain("ThirdPersonMovement"));
            Assert.That(graphSources, Does.Not.Contain("UnityEngine"));
            Assert.That(graphSources, Does.Not.Contain("Transform"));
            Assert.That(graphSources, Does.Not.Contain("CharacterController"));
            Assert.That(graphSources, Does.Not.Contain("Animancer"));
            Assert.That(graphSources, Does.Not.Contain("InputAction"));
        }

        [Test]
        public void StateGraphSnapshotKeepsCharacterInterpretationOut()
        {
            string snapshot = File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "Scripts/Character/StateMachine/Model/Graph/StateGraphSnapshot.cs"),
                System.Text.Encoding.UTF8);

            Assert.That(snapshot, Does.Not.Contain("CharacterStateOwner"));
            Assert.That(snapshot, Does.Not.Contain("LocomotionPhase"));
            Assert.That(snapshot, Does.Not.Contain("ActionState"));
            Assert.That(snapshot, Does.Not.Contain("BasicMovementPhase"));
            Assert.That(snapshot, Does.Not.Contain("ActionStateId"));
        }

        [Test]
        public void RunnerConsumesStateGraphFacadeForNodeAndTransitionLookup()
        {
            string runner = File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "Scripts/Character/StateMachine/Solver/Runtime/CharacterStateMachineRunner.cs"),
                System.Text.Encoding.UTF8);

            Assert.That(runner, Does.Contain("definition.Graph.TryGetNode"));
            Assert.That(runner, Does.Contain("definition.Graph.Transitions"));
            Assert.That(runner, Does.Not.Contain("new CharacterStateMachineRunner("));
            Assert.That(runner, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(runner, Does.Not.Contain("IActionMovementExecutor"));
            Assert.That(runner, Does.Not.Contain("IActionAnimationPresenter"));
        }

        [Test]
        public void TurnBackStateWithoutTimelinePolicyFailsValidation()
        {
            CharacterStateMachineDefinition defaults = LoadConfiguredStateMachineDefinition();
            CharacterStateMachineDefinition definition = new CharacterStateMachineDefinition(
                defaults.InitialState,
                defaults.Nodes.ToArray(),
                defaults.Transitions.ToArray(),
                defaults.TimelinePolicies.Where(policy => policy.StateId != CharacterStateIds.TurnBack).ToArray());

            CharacterStateMachineValidationResult validation = definition.Validate();

            Assert.True(validation.HasErrors);
            Assert.That(validation.DescribeErrors(), Does.Contain("TurnBack timeline policy is missing"));
        }

        [Test]
        public void CharacterStateMachineValidationPropagatesTimelineWarnings()
        {
            CharacterStateNodeDefinition idle = new CharacterStateNodeDefinition(
                CharacterStateIds.Idle,
                default,
                "Idle",
                new[] { CharacterStateTag.Character, CharacterStateTag.Locomotion },
                new CharacterStateModuleDefinition[0]);
            StateTimelinePolicyDefinition policy = new StateTimelinePolicyDefinition(
                CharacterStateIds.Idle.Value,
                0,
                0,
                new[]
                {
                    new StateTimelineWindowDefinition(
                        "duplicate-window",
                        StateTimelineWindowKind.Motion,
                        StateTimelineTimeDomain.Normalized,
                        0f,
                        0.5f,
                        factId: TimelineFactIds.MotionActive.Value),
                    new StateTimelineWindowDefinition(
                        "duplicate-window",
                        StateTimelineWindowKind.InputLock,
                        StateTimelineTimeDomain.Normalized,
                        0.5f,
                        1f,
                        factId: TimelineFactIds.InputLocked.Value)
                });
            CharacterStateMachineDefinition definition = new CharacterStateMachineDefinition(
                CharacterStateIds.Idle,
                new[] { idle },
                new CharacterStateTransitionDefinition[0],
                new[] { policy });

            CharacterStateMachineValidationResult validation = definition.Validate();

            Assert.False(validation.HasErrors);
            Assert.That(validation.Warnings, Is.Not.Empty);
            Assert.That(validation.DescribeWarnings(), Does.Contain("duplicate-window"));
        }

        [Test]
        public void DomainWildcardTransitionSourcePassesValidation()
        {
            CharacterStateMachineDefinition definition = LoadConfiguredLocomotionStateGraphDefinition();

            CharacterStateMachineValidationResult validation = definition.Validate();

            Assert.False(validation.DescribeErrors().Contains("source 'Locomotion.*' is not declared"));
        }

        [Test]
        public void ConditionAdapterHasMoveIntentBehaviorMatchesLegacy()
        {
            Assert.True(EvaluateCondition(CharacterStateTransitionCondition.HasMoveIntent(), Context(move: true)).Passed);
            Assert.False(EvaluateCondition(CharacterStateTransitionCondition.HasMoveIntent(), Context(move: false)).Passed);
        }

        [Test]
        public void ConditionAdapterNoMoveIntentBehaviorMatchesLegacy()
        {
            Assert.True(EvaluateCondition(CharacterStateTransitionCondition.NoMoveIntent(), Context(move: false)).Passed);
            Assert.False(EvaluateCondition(CharacterStateTransitionCondition.NoMoveIntent(), Context(move: true)).Passed);
        }

        [Test]
        public void ConditionAdapterStateCanExitBehaviorMatchesLegacy()
        {
            Assert.True(EvaluateCondition(CharacterStateTransitionCondition.StateCanExit(), Context(move: true, canExit: true)).Passed);
            Assert.False(EvaluateCondition(CharacterStateTransitionCondition.StateCanExit(), Context(move: true, canExit: false)).Passed);
        }

        [Test]
        public void ConditionAdapterHasInputRequestBehaviorMatchesLegacy()
        {
            CharacterInputRequestFact dodge = DodgeRequest(CharacterStateVariant.Directional, Vector3.forward);
            CharacterStateTransitionCondition condition = CharacterStateTransitionCondition.HasInputRequest(InputRequestKind.Dodge);

            Assert.True(EvaluateCondition(condition, Context(move: true, request: dodge)).Passed);
            Assert.False(EvaluateCondition(condition, Context(move: true, request: TurnBackRequest(1, 3, Vector3.back))).Passed);
            Assert.False(EvaluateCondition(condition, Context(move: true, request: new CharacterInputRequestFact(true, InputRequestKind.Dodge, 1, 3, 30, CharacterStateVariant.Directional, Vector3.zero))).Passed);
        }

        [Test]
        public void ConditionAdapterStateElapsedAtLeastBehaviorMatchesLegacy()
        {
            CharacterStateTransitionCondition condition = CharacterStateTransitionCondition.StateElapsedAtLeast(0.25f);

            Assert.True(EvaluateCondition(condition, Context(move: true), projectedStateTime: 0.25f).Passed);
            Assert.False(EvaluateCondition(condition, Context(move: true), projectedStateTime: 0.1f).Passed);
        }

        [Test]
        public void ConditionAdapterMoveTurnBackRequestedBehaviorMatchesLegacy()
        {
            CharacterStateTransitionCondition condition = CharacterStateTransitionCondition.MoveTurnBackRequested(120f);
            CharacterStateMachineContext accepted = Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward));
            CharacterStateMachineContext rejected = Context(
                move: true,
                runHeld: false,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward));

            CharacterStateTransitionConditionEvaluationResult acceptedResult = EvaluateCondition(condition, accepted);

            Assert.True(acceptedResult.Passed);
            Assert.True(acceptedResult.Trace.EmitDiagnostic);
            Assert.False(EvaluateCondition(condition, rejected).Passed);
        }

        [Test]
        public void ConditionAdapterLocomotionAnimationCanExitBehaviorMatchesLegacy()
        {
            CharacterStateMachineDefinition definition = LoadConfiguredStateMachineDefinition();
            Assert.True(definition.TryGetNode(CharacterStateIds.TurnBack, out CharacterStateNodeDefinition turnBack));
            CharacterStateTransitionCondition condition = CharacterStateTransitionCondition.LocomotionAnimationCanExit();

            CharacterStateTransitionConditionEvaluationResult notEnded = EvaluateCondition(
                condition,
                Context(
                    move: true,
                    runtimeBlackboard: BlackboardWithLocomotionProgress("Locomotion.Turn.Back", 0.25f, false)),
                turnBack);
            CharacterStateTransitionConditionEvaluationResult ended = EvaluateCondition(
                condition,
                Context(
                    move: true,
                    runtimeBlackboard: BlackboardWithLocomotionProgress("Locomotion.Turn.Back", 1f, true)),
                turnBack);

            Assert.False(notEnded.Passed);
            Assert.True(ended.Passed);
        }

        [Test]
        public void ConditionAdapterActionCanExitBehaviorMatchesLegacy()
        {
            CharacterStateNodeDefinition dodge = new CharacterStateNodeDefinition(
                CharacterStateIds.Dodge,
                CharacterStateIds.Action,
                "Dodge",
                new[] { CharacterStateTag.Character, CharacterStateTag.Action, CharacterStateTag.Dodge },
                new[]
                {
                    CharacterStateModuleDefinition.ConfiguredActionMotion(
                        new CharacterActionMovementDefinition(CharacterStateVariant.Directional, 0.35f, 4f, true, true)),
                    CharacterStateModuleDefinition.ActionAnimation(
                        CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeDirectional.Value, ActionAnimationKeys.DodgeDirectional.Value))
                });
            CharacterStateTransitionCondition condition = CharacterStateTransitionCondition.ActionCanExit();

            CharacterStateTransitionConditionEvaluationResult notEnded = EvaluateCondition(
                condition,
                Context(
                    move: true,
                    runtimeBlackboard: BlackboardWithActionProgress(ActionAnimationKeys.DodgeDirectional, 0.5f, false)),
                dodge,
                CharacterStateVariant.Directional);
            CharacterStateTransitionConditionEvaluationResult ended = EvaluateCondition(
                condition,
                Context(
                    move: true,
                    runtimeBlackboard: BlackboardWithActionProgress(ActionAnimationKeys.DodgeDirectional, 1f, true)),
                dodge,
                CharacterStateVariant.Directional);

            Assert.False(notEnded.Passed);
            Assert.True(ended.Passed);
        }

        [Test]
        public void ConditionEvaluatorCoverageMissingKeyFailsValidation()
        {
            CharacterStateMachineDefinition defaults = LoadConfiguredStateMachineDefinition();
            CharacterStateMachineDefinition definition = new CharacterStateMachineDefinition(
                defaults.InitialState,
                defaults.Nodes.ToArray(),
                new[]
                {
                    new CharacterStateTransitionDefinition(
                        CharacterStateIds.Idle.Value,
                        CharacterStateIds.MoveLoop,
                        0,
                        CharacterStateTransitionCondition.ActionCanExit())
                },
                defaults.TimelinePolicies.ToArray());

            CharacterStateMachineValidationResult validation = CharacterStateMachineValidator.Validate(
                definition,
                new CharacterStateCoreConditionEvaluator());

            Assert.True(validation.HasErrors);
            Assert.That(validation.DescribeErrors(), Does.Contain("has no evaluator"));
        }

        [Test]
        public void DuplicateConditionEvaluatorKeyFailsValidation()
        {
            CharacterStateMachineValidationResult validation = CharacterStateMachineValidator.Validate(
                LoadConfiguredStateMachineDefinition(),
                new CharacterStateCoreConditionEvaluator(),
                new DuplicateConditionEvaluator(CharacterStateTransitionConditionKind.HasMoveIntent));

            Assert.True(validation.HasErrors);
            Assert.That(validation.DescribeErrors(), Does.Contain("duplicated"));
        }

        [Test]
        public void EvaluatorCollectionRejectsDuplicateConditionKey()
        {
            System.InvalidOperationException exception = Assert.Throws<System.InvalidOperationException>(() =>
                new CharacterStateTransitionEvaluatorCollection(
                    new CharacterStateCoreConditionEvaluator(),
                    new DuplicateConditionEvaluator(CharacterStateTransitionConditionKind.HasMoveIntent)));

            Assert.That(exception.Message, Does.Contain("duplicated"));
        }

        [Test]
        public void DefaultEvaluatorCollectionKeepsStableOrderAndCoversConditionEnum()
        {
            CharacterStateTransitionEvaluatorCollection collection = CharacterStateTransitionEvaluatorCollection.Default;

            CollectionAssert.AreEqual(
                new[] { "Core", "Locomotion", "Animation", "Action" },
                collection.Evaluators.Select(evaluator => evaluator.Name).ToArray());
            CollectionAssert.IsEmpty(
                System.Enum.GetValues(typeof(CharacterStateTransitionConditionKind))
                    .Cast<CharacterStateTransitionConditionKind>()
                    .Where(kind => !collection.Supports(kind))
                    .ToArray());
        }

        [Test]
        public void RunnerTransitionSelectionDelegatesBusinessConditionsToCollection()
        {
            string runner = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/StateMachine/Solver/Runtime/CharacterStateMachineRunner.cs"));
            string transitionSelection = ExtractMethodBody(runner, "TryResolveTransition") +
                                         ExtractMethodBody(runner, "AllConditionsPass");

            Assert.That(transitionSelection, Does.Contain("transitionEvaluators.Evaluate"));
            Assert.That(transitionSelection, Does.Not.Contain("MoveTurnBackRequested"));
            Assert.That(transitionSelection, Does.Not.Contain("ActionCanExit"));
            Assert.That(transitionSelection, Does.Not.Contain("Attack"));
            Assert.That(transitionSelection, Does.Not.Contain("Jump"));
            Assert.That(transitionSelection, Does.Not.Contain("HitReact"));
        }

        [Test]
        public void RunnerDoesNotSubmitConditionDiagnosticsDirectly()
        {
            string runner = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/StateMachine/Solver/Runtime/CharacterStateMachineRunner.cs"));

            Assert.That(runner, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(runner, Does.Not.Contain("LogTurnBackConditionProbe"));
        }

        [Test]
        public void TransitionConditionEvaluatorsAvoidForbiddenRuntimeObjects()
        {
            string transitionRoot = Path.Combine(Application.dataPath, "Scripts/Character/StateMachine/Solver/Transition");
            string combined = string.Join("\n", Directory.GetFiles(transitionRoot, "*.cs", SearchOption.TopDirectoryOnly).Select(File.ReadAllText));

            Assert.That(combined, Does.Not.Contain("Animancer"));
            Assert.That(combined, Does.Not.Contain("Animator"));
            Assert.That(combined, Does.Not.Contain("CharacterController"));
            Assert.That(combined, Does.Not.Contain("UnityEngine.InputSystem"));
            Assert.That(combined, Does.Not.Contain("InputAction"));
            Assert.That(combined, Does.Not.Contain("Transform"));
            Assert.That(combined, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
        }

        [Test]
        public void ConditionDefinitionDoesNotStoreRuntimeEvaluatorReferences()
        {
            string model = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/StateMachine/Model/CharacterStateMachineTypes.cs"));
            string conditionDefinition = ExtractSourceBlock(
                model,
                "public struct CharacterStateTransitionCondition",
                "public sealed class CharacterStateTransitionDefinition");

            Assert.That(conditionDefinition, Does.Not.Contain("MonoBehaviour"));
            Assert.That(conditionDefinition, Does.Not.Contain("ScriptableObject"));
            Assert.That(conditionDefinition, Does.Not.Contain("ICharacterStateTransitionConditionEvaluator"));
            Assert.That(conditionDefinition, Does.Not.Contain("Func<"));
            Assert.That(conditionDefinition, Does.Not.Contain("Action<"));
        }

        [Test]
        public void ConditionTraceCanBeSubmittedByDiagnosticAdapter()
        {
            FakeCharacterDiagnosticSink sink = new FakeCharacterDiagnosticSink();
            CharacterFrameDiagnosticAdapter adapter = new CharacterFrameDiagnosticAdapter(sink);
            CharacterStateTransitionConditionTrace trace = new CharacterStateTransitionConditionTrace(
                CharacterStateTransitionConditionKind.HasMoveIntent,
                CharacterStateIds.Idle.Value,
                CharacterStateIds.MoveStart.Value,
                12,
                false,
                "missing-move",
                "hasMove=False",
                true,
                "transition-condition-test");

            adapter.LogTransitionConditionTraces(new[] { trace });

            Assert.True(sink.Events.Any(item =>
                item.Message == "transition-condition-test" &&
                item.StatePath == CharacterStateIds.Idle.Value &&
                item.PreviousStatePath == CharacterStateIds.MoveStart.Value &&
                item.Step == 12 &&
                item.Context.Contains("condition=HasMoveIntent") &&
                item.Context.Contains("reason=missing-move")));
        }

        [Test]
        public void TurnBackConditionProbeLogFieldsAreGeneratedFromTrace()
        {
            FakeCharacterDiagnosticSink sink = new FakeCharacterDiagnosticSink();
            CharacterFrameDiagnosticAdapter adapter = new CharacterFrameDiagnosticAdapter(sink);
            CharacterStateTransitionConditionEvaluationResult result = EvaluateCondition(
                CharacterStateTransitionCondition.MoveTurnBackRequested(120f),
                Context(
                    move: true,
                    runHeld: true,
                    currentStep: 3,
                    worldDirection: Vector3.back,
                    facingForward: Vector3.forward,
                    turnBackIntent: TurnBackIntent(3, 7, 180f, Vector3.back, Vector3.forward)),
                projectedStateTime: 0.3f);

            adapter.LogTransitionConditionTraces(new[] { result.Trace });

            Assert.True(result.Trace.EmitDiagnostic);
            Assert.True(sink.Events.Any(item =>
                item.Message == "locomotion-turnback-condition" &&
                item.Context.Contains("condition=MoveTurnBackRequested") &&
                item.Context.Contains("angle=180.000") &&
                item.Context.Contains("threshold=120.000") &&
                item.Context.Contains("passed=True")));
        }

        [Test]
        public void CharacterDiagnosticAdaptersSupportFakeSinkEventObservation()
        {
            FakeCharacterDiagnosticSink sink = new FakeCharacterDiagnosticSink();
            CharacterFrameDiagnosticAdapter fullBody = new CharacterFrameDiagnosticAdapter(sink);
            ActionInterruptDiagnosticAdapter action = new ActionInterruptDiagnosticAdapter(sink);
            LocomotionDiagnosticAdapter locomotion = new LocomotionDiagnosticAdapter(sink);
            ActionInterruptContext context = new ActionInterruptContext(ActionStateIds.None, 0.25f, 0, 9);
            ActionInterruptRequest request = new ActionInterruptRequest(3, ActionRequestType.Dodge, ActionStateIds.Dodge, 30, originTick: 9);
            ActionInterruptPolicy policy = new ActionInterruptPolicy(ActionStateIds.None, ActionStateIds.Dodge, 30);

            fullBody.LogPipelineSnapshot("Locomotion.Idle", 9, "summary");
            fullBody.LogTimelineFactsTrace(StateTimelineFactsTrace.Current(StateTimelineWindowFacts.None(default), 9, ActionRequestType.None));
            action.LogRequestAccepted(in context, request, 0, policy);
            action.LogRequestRejected(in context, request, ActionInterruptRejectReason.PriorityTooLow, 0, policy);
            locomotion.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Trace,
                "turnback-frame-summary"));

            Assert.True(sink.Events.Any(item => item.Message == "character-frame-pipeline"));
            Assert.True(sink.Events.Any(item => item.Message == "state-timeline-window-facts"));
            Assert.True(sink.Events.Any(item => item.Message == "interrupt-request-accepted"));
            Assert.True(sink.Events.Any(item => item.Message == "interrupt-request-rejected"));
            Assert.True(sink.Events.Any(item => item.Message == "turnback-frame-summary"));
        }

        [Test]
        public void RejectedActionRequestDoesNotEnterTransitionConditionContext()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 5, 4);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero);
            LocomotionDecisionFacts facts = DecisionFacts(in input, in settings, Vector3.forward, Vector3.forward);
            DodgeActionTuning config = TestDodgeTuning();
            CharacterInputRequestFact rejected = CommittedActionInterruptRequestFactory.BuildDodgeRequestFact(
                buffer,
                5,
                CharacterStateMachineSnapshot.Inactive,
                in input,
                false,
                in facts,
                in config,
                0,
                new[] { new ActionInterruptPolicy(ActionStateIds.None, ActionStateIds.Dodge, 31) },
                out ActionInterruptDecision decision);

            CharacterStateTransitionConditionEvaluationResult result = EvaluateCondition(
                CharacterStateTransitionCondition.HasInputRequest(InputRequestKind.Dodge),
                Context(move: true, request: rejected));

            Assert.False(decision.Accepted);
            Assert.False(rejected.HasRequest);
            Assert.False(result.Passed);
            Assert.That(result.Trace.Context, Does.Contain("hasRequest=False"));
            Assert.That(result.Trace.Context, Does.Not.Contain("ActionInterruptPolicy"));
        }

        [Test]
        public void ConfiguredRunLocomotionEnablesTurnBackBakedProfileAndKeepsRunEndProfile()
        {
            RunLocomotionAnimationConfigSO asset = AssetDatabase.LoadAssetAtPath<RunLocomotionAnimationConfigSO>(
                "Assets/Configs/3C/Animation/Corin/Locomotion/CorinLocomotionAnimationConfig.asset");
            Assert.NotNull(asset);

            RunLocomotionAnimationConfigValidationResult validation = asset.Validate();
            Assert.False(validation.HasErrors, validation.DescribeErrors());
            LocomotionMotionProfileSO turnBackProfile = asset.ResolveMotionProfile(
                BasicMovementPhase.TurnBack,
                BasicMovementGait.Run,
                "Locomotion.Turn.Back");
            Assert.NotNull(turnBackProfile);
            Assert.AreEqual(BasicMovementPhase.TurnBack, turnBackProfile.Phase);
            Assert.AreEqual(BasicMovementGait.Run, turnBackProfile.Gait);
            Assert.AreEqual("Locomotion.Turn.Back", turnBackProfile.AliasKey);
            Assert.NotNull(asset.ResolveMotionProfile(
                BasicMovementPhase.MoveStop,
                BasicMovementGait.Run,
                "RunEnd"));
        }

        [Test]
        public void ConfiguredTurnBackEntryOnlyConsumesAcceptedTurnBackInputFact()
        {
            CharacterStateMachineDefinition definition = LoadConfiguredLocomotionStateGraphDefinition();
            CharacterStateTransitionDefinition[] transitions = definition.Transitions
                .Where(transition => transition.ToStateId == CharacterStateIds.TurnBack)
                .ToArray();

            Assert.AreEqual(2, transitions.Length);
            CollectionAssert.AreEquivalent(
                new[] { CharacterStateIds.MoveStart.Value, CharacterStateIds.MoveLoop.Value },
                transitions.Select(transition => transition.FromStateId).ToArray());
            foreach (CharacterStateTransitionDefinition transition in transitions)
            {
                Assert.AreEqual(1, transition.Conditions.Count);
                Assert.AreEqual(CharacterStateTransitionConditionKind.HasInputRequest, transition.Conditions[0].Kind);
                Assert.AreEqual(InputRequestKind.TurnBack, transition.Conditions[0].RequestKind);
                Assert.False(transition.Conditions.Any(condition => condition.Kind == CharacterStateTransitionConditionKind.MoveTurnBackRequested));
            }

            string asset = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Configs/3C/StateMachine/Locomotion/Corin/CorinLocomotionStateGraph.asset"));
            Assert.That(asset, Does.Not.Contain("kind: 7"));
        }

        [Test]
        public void MoveLoopAcceptedTurnBackRequestEntersTurnBack()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true));
            runner.Tick(Context(move: true, canExit: true));

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                request: TurnBackRequest(1, 3, Vector3.back),
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward)));

            Assert.AreEqual(CharacterStateIds.TurnBack, frame.Snapshot.ActiveState);
            Assert.AreEqual(BasicMovementPhase.TurnBack, frame.LocomotionPhase);
            Assert.True(frame.ExecuteBasicMovement);
            Assert.True(frame.PresentLocomotionAnimation);
            Assert.AreEqual(ActionStateIds.None, frame.ActionState);
            Assert.True(frame.HasTurnBackMotionPolicy);
            Assert.AreEqual(TurnBackMotionPolicy.DefaultAliasKey, frame.TurnBackMotionPolicy.AliasKey);
            Assert.AreEqual(Vector3.back, frame.TurnBackWorldDirection);
        }

        [Test]
        public void MoveLoopIntentOnlyDoesNotEnterTurnBack()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true));
            runner.Tick(Context(move: true, canExit: true));

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward)));

            Assert.AreEqual(CharacterStateIds.MoveLoop, frame.Snapshot.ActiveState);
        }

        [Test]
        public void MoveStartAcceptedTurnBackRequestEntersTurnBack()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true, runHeld: true));

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                request: TurnBackRequest(1, 3, Vector3.back),
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward)));

            Assert.AreEqual(CharacterStateIds.TurnBack, frame.Snapshot.ActiveState);
            Assert.AreEqual(BasicMovementPhase.TurnBack, frame.LocomotionPhase);
            Assert.AreEqual(Vector3.back, frame.TurnBackWorldDirection);
        }

        [Test]
        public void RejectedTurnBackRequestDoesNotEnterTurnBack()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true));
            runner.Tick(Context(move: true, canExit: true));
            LocomotionTurnBackIntent intent = TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward);
            LocomotionDecisionFacts facts = new LocomotionDecisionFacts(
                MovementInputIntent.FromRaw(Vector2.up, 0.1f, true),
                BasicMovementGait.Run,
                BasicMovementPhaseFacts.None,
                new LocomotionSpatialFacts(Vector3.back, Vector3.forward, Vector3.forward, Vector3.right),
                intent);
            CharacterInputRequestFact rejectedFact = CommittedActionInterruptRequestFactory.BuildTurnBackRequestFact(
                1,
                runner.Snapshot,
                in facts,
                StateTimelineWindowFacts.None(CharacterStateIds.MoveLoop),
                new[] { new ActionInterruptPolicy(new ActionStateId(CharacterStateIds.MoveLoop.Value), new ActionStateId(CharacterStateIds.TurnBack.Value), 99) },
                out ActionInterruptDecision decision);

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                request: rejectedFact,
                turnBackIntent: intent));

            Assert.False(decision.Accepted);
            Assert.False(rejectedFact.HasRequest);
            Assert.AreEqual(CharacterStateIds.MoveLoop, frame.Snapshot.ActiveState);
        }

        [Test]
        public void MoveLoopTurnBackUsesAcceptedRequestDirectionInsteadOfPreviousMoveDirection()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true));
            runner.Tick(Context(move: true, canExit: true));

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                request: TurnBackRequest(1, 3, Vector3.back),
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward),
                runtimeBlackboard: BlackboardWithLocomotionDirection(Vector3.back)));

            Assert.AreEqual(CharacterStateIds.TurnBack, frame.Snapshot.ActiveState);
            Assert.AreEqual(Vector3.back, frame.TurnBackWorldDirection);
            Assert.AreEqual(Vector3.forward, frame.TurnBackEntryBasisForward);
            Assert.AreNotEqual(frame.TurnBackWorldDirection, frame.TurnBackEntryBasisForward);
        }

        [Test]
        public void TurnBackLockedDirectionSurvivesSubsequentInputBasisChanges()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true));
            runner.Tick(Context(move: true, canExit: true));

            CharacterStateMachineFrame entered = runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                request: TurnBackRequest(1, 3, Vector3.back),
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward)));
            CharacterStateMachineFrame held = runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.right,
                facingForward: Vector3.right,
                runtimeBlackboard: BlackboardWithLocomotionProgress("Locomotion.Turn.Back", 0.25f, false)));

            Assert.AreEqual(CharacterStateIds.TurnBack, entered.Snapshot.ActiveState);
            Assert.AreEqual(CharacterStateIds.TurnBack, held.Snapshot.ActiveState);
            Assert.AreEqual(Vector3.back, held.TurnBackWorldDirection);
            Assert.AreEqual(Vector3.forward, held.TurnBackEntryBasisForward);
        }

        [Test]
        public void TurnBackEntryBasisSurvivesRestore()
        {
            CharacterStateMachineRunner original = CreateRunner();
            original.Tick(Context(move: true));
            original.Tick(Context(move: true, canExit: true));
            CharacterStateMachineFrame entered = original.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                request: TurnBackRequest(1, 3, Vector3.back),
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward)));
            CharacterStateMachineRestoreState restoreState = original.CaptureRestoreState();

            CharacterStateMachineRunner restored = CreateRunner();
            Assert.True(restored.Restore(in restoreState));
            CharacterStateMachineFrame restoredFrame = restored.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.right,
                facingForward: Vector3.right,
                runtimeBlackboard: BlackboardWithLocomotionProgress("Locomotion.Turn.Back", 0.25f, false)));

            Assert.AreEqual(CharacterStateIds.TurnBack, entered.Snapshot.ActiveState);
            Assert.AreEqual(CharacterStateIds.TurnBack, restoredFrame.Snapshot.ActiveState);
            Assert.AreEqual(Vector3.back, restoredFrame.TurnBackWorldDirection);
            Assert.AreEqual(Vector3.forward, restoredFrame.TurnBackEntryBasisForward);
        }

        [Test]
        public void MoveLoopPreviousOpposedDirectionDoesNotTriggerTurnBackWhenFacingMatchesInput()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true));
            runner.Tick(Context(move: true, canExit: true));

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: true,
                worldDirection: Vector3.forward,
                facingForward: Vector3.forward,
                runtimeBlackboard: BlackboardWithLocomotionDirection(Vector3.back)));

            Assert.AreEqual(CharacterStateIds.MoveLoop, frame.Snapshot.ActiveState);
        }

        [Test]
        public void MoveLoopReverseInputWithoutDerivedIntentDoesNotEnterTurnBack()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true));
            runner.Tick(Context(move: true, canExit: true));

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward));

            Assert.AreEqual(CharacterStateIds.MoveLoop, frame.Snapshot.ActiveState);
        }

        [Test]
        public void MoveStartIntentOnlyDoesNotEnterTurnBack()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true, runHeld: true));

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward)));

            Assert.AreEqual(CharacterStateIds.MoveStart, frame.Snapshot.ActiveState);
        }

        [Test]
        public void MoveStopDoesNotConsumeTurnBackIntent()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true, runHeld: true));
            runner.Tick(Context(move: true, runHeld: true, canExit: true));
            runner.Tick(Context(move: false));

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward)));

            Assert.AreEqual(CharacterStateIds.MoveStart, frame.Snapshot.ActiveState);
        }

        [Test]
        public void IdleDoesNotConsumeTurnBackIntentDirectly()
        {
            CharacterStateMachineRunner runner = CreateRunner();

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward)));

            Assert.AreEqual(CharacterStateIds.MoveStart, frame.Snapshot.ActiveState);
        }

        [Test]
        public void TurnBackExitsToMoveLoopAfterLocomotionAnimationEnds()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true, runHeld: true));
            runner.Tick(Context(move: true, runHeld: true, canExit: true));
            runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                request: TurnBackRequest(1, 3, Vector3.back),
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward),
                runtimeBlackboard: BlackboardWithLocomotionDirection(Vector3.back)));

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                runtimeBlackboard: BlackboardWithLocomotionProgress("Locomotion.Turn.Back", 1f, true)));

            Assert.AreEqual(CharacterStateIds.MoveLoop, frame.Snapshot.ActiveState);
            Assert.AreEqual(BasicMovementPhase.MoveLoop, frame.LocomotionPhase);
        }

        [Test]
        public void TurnBackDoesNotExitBeforeTurnCompleteTime()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true, runHeld: true));
            runner.Tick(Context(move: true, runHeld: true, canExit: true));
            runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                request: TurnBackRequest(1, 3, Vector3.back),
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward),
                runtimeBlackboard: BlackboardWithLocomotionDirection(Vector3.back)));

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                runtimeBlackboard: BlackboardWithLocomotionProgress(
                    "Locomotion.Turn.Back",
                    TurnBackMotionPolicy.DefaultTurnCompleteNormalizedTime - 0.01f,
                    false)));

            Assert.AreEqual(CharacterStateIds.TurnBack, frame.Snapshot.ActiveState);
            Assert.AreEqual(BasicMovementPhase.TurnBack, frame.LocomotionPhase);
        }

        [Test]
        public void TurnBackExitsToMoveLoopAtTurnCompleteTime()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true, runHeld: true));
            runner.Tick(Context(move: true, runHeld: true, canExit: true));
            runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                request: TurnBackRequest(1, 3, Vector3.back),
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward),
                runtimeBlackboard: BlackboardWithLocomotionDirection(Vector3.back)));

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                runtimeBlackboard: BlackboardWithLocomotionProgress(
                    "Locomotion.Turn.Back",
                    TurnBackMotionPolicy.DefaultTurnCompleteNormalizedTime,
                    false)));

            Assert.AreEqual(CharacterStateIds.MoveLoop, frame.Snapshot.ActiveState);
            Assert.AreEqual(BasicMovementPhase.MoveLoop, frame.LocomotionPhase);
        }

        [Test]
        public void TurnBackExitsToIdleAtTurnCompleteTimeWithoutMoveInput()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true, runHeld: true));
            runner.Tick(Context(move: true, runHeld: true, canExit: true));
            runner.Tick(Context(
                move: true,
                runHeld: true,
                worldDirection: Vector3.back,
                facingForward: Vector3.forward,
                request: TurnBackRequest(1, 3, Vector3.back),
                turnBackIntent: TurnBackIntent(1, 3, 180f, Vector3.back, Vector3.forward),
                runtimeBlackboard: BlackboardWithLocomotionDirection(Vector3.back)));

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: false,
                runHeld: true,
                runtimeBlackboard: BlackboardWithLocomotionProgress(
                    "Locomotion.Turn.Back",
                    TurnBackMotionPolicy.DefaultTurnCompleteNormalizedTime,
                    false)));

            Assert.AreEqual(CharacterStateIds.Idle, frame.Snapshot.ActiveState);
            Assert.AreEqual(BasicMovementPhase.Idle, frame.LocomotionPhase);
        }

        [Test]
        public void CorinTurnBackTransitionLibrariesUseRootMotionClips()
        {
            AssertTurnBackLibraryUsesInPlaceVisualClip("Assets/Configs/3C/Animation/Corin/Animancer/Reference/Humanoid/CorinHumanoid_TransitionLib.asset");
            AssertTurnBackLibraryUsesInPlaceVisualClip("Assets/Configs/3C/Animation/Corin/Animancer/RigVariants/Generic/CorinGenericAnimancerTransitionLibrary.asset");
        }

        [Test]
        public void MissingTurnBackTransitionReportsDiagnosticAndDoesNotPlay()
        {
            RuntimeDiagnosticLog.Reset();
            List<RuntimeDiagnosticLogEvent> events = new List<RuntimeDiagnosticLogEvent>();
            GameObject gameObject = new GameObject("missing-turnback-transition-test");
            try
            {
                gameObject.AddComponent<Animator>();
                AnimancerComponent animancer = gameObject.AddComponent<AnimancerComponent>();
                CharacterAnimancerPresenter presenter = gameObject.AddComponent<CharacterAnimancerPresenter>();
                TransitionLibrary library = new TransitionLibrary();
                library.AddTransition(StringReference.Get("Idle"), CreateClipTransition(CreateClip("Idle")));
                animancer.Graph.Transitions = library;

                using (RuntimeDiagnosticLog.Capture(events.Add))
                {
                    presenter.Present(new MovementAnimationContext(
                        BasicMovementPhase.TurnBack,
                        BasicMovementGait.Run,
                        true,
                        1f,
                        Vector3.back,
                        0f,
                        TurnBackMotionPolicy.Default,
                        true));
                }

                Assert.IsEmpty(presenter.CurrentAnimationName);
                Assert.True(events.Any(item => item.Message == "locomotion-animation-missing-transition" && item.Context.Contains("Locomotion.Turn.Back")));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CorinPrefabAnimatorKeepsRootMotionEnabledForManualOnAnimatorMove()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Character/可琳.prefab");

            Assert.NotNull(prefab);
            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            Assert.NotNull(animator);
            Assert.True(animator.applyRootMotion);
        }
        [Test]
        public void BackstepDodgeReturnToIdleReplaysLocomotionAfterActionInterrupt()
        {
            CreateAnimationPresenterRig(
                out GameObject gameObject,
                out CharacterAnimancerPresenter locomotionPresenter,
                out CharacterAnimancerPresenter actionPresenter,
                out AnimancerComponent animancer,
                out AnimationClip idleClip,
                out AnimationClip runClip,
                out AnimationClip dodgeClip);

            try
            {
                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.Idle, BasicMovementGait.Walk, false, 0f, Vector3.zero, 0f));
                Assert.AreSame(idleClip, animancer.Graph.Layers[0].CurrentState.MainObject);

                actionPresenter.Present(new CharacterStateAnimationRequest(CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeBackstep.Value, "Dodge Backstep"), 1, new ActionAnimationPlaybackIntent(1)));
                Assert.AreSame(dodgeClip, animancer.Graph.Layers[0].CurrentState.MainObject);

                actionPresenter.Clear();
                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.Idle, BasicMovementGait.Walk, false, 0f, Vector3.zero, 0f));

                Assert.AreSame(idleClip, animancer.Graph.Layers[0].CurrentState.MainObject);
                Assert.False(actionPresenter.HasValidPlayback);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(runClip);
                Object.DestroyImmediate(dodgeClip);
            }
        }

        [Test]
        public void DirectionalDodgeReturnToRunLoopReplaysLocomotionAfterActionInterrupt()
        {
            CreateAnimationPresenterRig(
                out GameObject gameObject,
                out CharacterAnimancerPresenter locomotionPresenter,
                out CharacterAnimancerPresenter actionPresenter,
                out AnimancerComponent animancer,
                out AnimationClip idleClip,
                out AnimationClip runClip,
                out AnimationClip dodgeClip);

            try
            {
                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.MoveLoop, BasicMovementGait.Run, true, 1f, Vector3.forward, 5f));
                Assert.AreSame(runClip, animancer.Graph.Layers[0].CurrentState.MainObject);

                actionPresenter.Present(new CharacterStateAnimationRequest(CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeDirectional.Value, "Dodge Directional"), 1, new ActionAnimationPlaybackIntent(1)));
                Assert.AreSame(dodgeClip, animancer.Graph.Layers[0].CurrentState.MainObject);

                actionPresenter.Clear();
                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.MoveLoop, BasicMovementGait.Run, true, 1f, Vector3.forward, 5f));

                Assert.AreSame(runClip, animancer.Graph.Layers[0].CurrentState.MainObject);
                Assert.False(actionPresenter.HasValidPlayback);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(runClip);
                Object.DestroyImmediate(dodgeClip);
            }
        }

        [Test]
        public void TurnBackLocomotionKeepsAnimatorRootMotionDisabledAfterAction()
        {
            CreateAnimationPresenterRig(
                out GameObject gameObject,
                out CharacterAnimancerPresenter locomotionPresenter,
                out CharacterAnimancerPresenter actionPresenter,
                out AnimancerComponent animancer,
                out AnimationClip idleClip,
                out AnimationClip runClip,
                out AnimationClip dodgeClip);

            try
            {
                AnimatorRootMotionController rootMotionController = animancer.GetComponent<AnimatorRootMotionController>();
                actionPresenter.Present(new CharacterStateAnimationRequest(CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeDirectional.Value, "Dodge Directional"), 1, new ActionAnimationPlaybackIntent(1)));
                Assert.False(rootMotionController.ManualRootMotionActive);
                Assert.False(animancer.Animator.applyRootMotion);

                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.TurnBack, BasicMovementGait.Run, true, 1f, Vector3.back, 5f));

                Assert.False(rootMotionController.ManualRootMotionActive);
                Assert.False(animancer.Animator.applyRootMotion);
                Assert.AreSame(runClip, animancer.Graph.Layers[0].CurrentState.MainObject);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(runClip);
                Object.DestroyImmediate(dodgeClip);
            }
        }

        [Test]
        public void RootMotionPolicyKeepsAnimatorApplyRootMotionDisabledForTurnBack()
        {
            CreateAnimationPresenterRig(
                out GameObject gameObject,
                out CharacterAnimancerPresenter locomotionPresenter,
                out CharacterAnimancerPresenter actionPresenter,
                out AnimancerComponent animancer,
                out AnimationClip idleClip,
                out AnimationClip runClip,
                out AnimationClip dodgeClip);

            try
            {
                AnimatorRootMotionController rootMotionController = animancer.GetComponent<AnimatorRootMotionController>();
                animancer.Animator.applyRootMotion = false;

                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.TurnBack, BasicMovementGait.Run, true, 1f, Vector3.back, 5f));

                Assert.False(animancer.Animator.applyRootMotion);
                Assert.False(rootMotionController.ManualRootMotionActive);

                animancer.Animator.applyRootMotion = true;
                actionPresenter.Present(new CharacterStateAnimationRequest(CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeDirectional.Value, "Dodge Directional"), 1, new ActionAnimationPlaybackIntent(1)));

                Assert.False(animancer.Animator.applyRootMotion);
                Assert.False(rootMotionController.ManualRootMotionActive);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(runClip);
                Object.DestroyImmediate(dodgeClip);
            }
        }

        [Test]
        public void NonTurnBackLocomotionKeepsAnimatorRootMotionDisabledByDefault()
        {
            CreateAnimationPresenterRig(
                out GameObject gameObject,
                out CharacterAnimancerPresenter locomotionPresenter,
                out _,
                out AnimancerComponent animancer,
                out AnimationClip idleClip,
                out AnimationClip runClip,
                out AnimationClip dodgeClip);

            try
            {
                AnimatorRootMotionController rootMotionController = animancer.GetComponent<AnimatorRootMotionController>();
                animancer.Animator.applyRootMotion = true;

                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.MoveLoop, BasicMovementGait.Run, true, 1f, Vector3.forward, 5f));

                Assert.False(animancer.Animator.applyRootMotion);
                Assert.False(rootMotionController.ManualRootMotionActive);
                Assert.AreSame(runClip, animancer.Graph.Layers[0].CurrentState.MainObject);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(runClip);
                Object.DestroyImmediate(dodgeClip);
            }
        }

        [Test]
        public void LocomotionPresenterDoesNotKeepPendingRuntimeRootMotionDelta()
        {
            CreateAnimationPresenterRig(
                out GameObject gameObject,
                out CharacterAnimancerPresenter locomotionPresenter,
                out _,
                out _,
                out AnimationClip idleClip,
                out AnimationClip runClip,
                out AnimationClip dodgeClip);

            try
            {
                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.MoveLoop, BasicMovementGait.Run, true, 1f, Vector3.forward, 5f));

                string presenter = File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "Scripts/Character/Animation/Runtime/CharacterAnimancerPresenter.cs"));
                Assert.That(presenter, Does.Not.Contain("pendingRootMotionDelta"));
                Assert.That(presenter, Does.Not.Contain("ConsumeRootMotionDelta"));
                Assert.That(presenter, Does.Not.Contain("ILocomotionRootMotionSource"));
                Assert.That(presenter, Does.Not.Contain("ILocomotionRootMotionRollbackStateProvider"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(runClip);
                Object.DestroyImmediate(dodgeClip);
            }
        }


        [Test]
        public void LocomotionPlaybackProgressAdvancesFromSimulationTick()
        {
            CreateAnimationPresenterRig(
                out GameObject gameObject,
                out CharacterAnimancerPresenter locomotionPresenter,
                out _,
                out AnimancerComponent animancer,
                out AnimationClip idleClip,
                out AnimationClip runClip,
                out AnimationClip dodgeClip);

            try
            {
                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.TurnBack, BasicMovementGait.Run, true, 1f, Vector3.back, 5f));
                AnimancerState state = animancer.Graph.Layers[0].CurrentState;
                locomotionPresenter.RestorePlaybackProgress(new AnimationPhasePlaybackProgress(
                    BasicMovementPhase.TurnBack,
                    "Locomotion.Turn.Back",
                    0.25f,
                    true,
                    false));

                AnimationPhasePlaybackProgress before = locomotionPresenter.CurrentPlaybackProgress;
                AnimationPhasePlaybackProgress after = locomotionPresenter.AdvancePlayback(0.5f);

                Assert.AreEqual(0.25f, before.NormalizedTime, 0.0001f);
                Assert.AreEqual(0.75f, after.NormalizedTime, 0.0001f);
                Assert.AreEqual(0.75f, state.NormalizedTime, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(runClip);
                Object.DestroyImmediate(dodgeClip);
            }
        }

        [Test]
        public void TurnBackRestorePlaybackProgressResumesSameAliasWithoutRestart()
        {
            CreateAnimationPresenterRig(
                out GameObject gameObject,
                out CharacterAnimancerPresenter locomotionPresenter,
                out CharacterAnimancerPresenter actionPresenter,
                out AnimancerComponent animancer,
                out AnimationClip idleClip,
                out AnimationClip runClip,
                out AnimationClip dodgeClip);

            try
            {
                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.TurnBack, BasicMovementGait.Run, true, 1f, Vector3.back, 5f));
                Assert.AreSame(runClip, animancer.Graph.Layers[0].CurrentState.MainObject);

                locomotionPresenter.RestorePlaybackProgress(
                    new AnimationPhasePlaybackProgress(
                        BasicMovementPhase.TurnBack,
                        "Locomotion.Turn.Back",
                        0.35f,
                        true,
                        false),
                    BasicMovementGait.Run);
                actionPresenter.Present(new CharacterStateAnimationRequest(
                    CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeDirectional.Value, "Dodge Directional"),
                    1,
                    new ActionAnimationPlaybackIntent(1)));
                Assert.AreSame(dodgeClip, animancer.Graph.Layers[0].CurrentState.MainObject);

                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.TurnBack, BasicMovementGait.Run, true, 1f, Vector3.back, 5f));

                Assert.AreSame(runClip, animancer.Graph.Layers[0].CurrentState.MainObject);
                Assert.AreEqual(0.35f, animancer.Graph.Layers[0].CurrentState.NormalizedTime, 0.0001f);
                Assert.AreEqual(0.35f, locomotionPresenter.CurrentPlaybackProgress.NormalizedTime, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(runClip);
                Object.DestroyImmediate(dodgeClip);
            }
        }

        [Test]
        public void TurnBackAnimationRestartsWhenReenteredAfterCompletion()
        {
            CreateAnimationPresenterRig(
                out GameObject gameObject,
                out CharacterAnimancerPresenter locomotionPresenter,
                out _,
                out AnimancerComponent animancer,
                out AnimationClip idleClip,
                out AnimationClip runClip,
                out AnimationClip dodgeClip);

            try
            {
                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.TurnBack, BasicMovementGait.Run, true, 1f, Vector3.back, 0f));
                AnimancerState turnBackState = animancer.Graph.Layers[0].CurrentState;
                turnBackState.NormalizedTime = 1.024f;

                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.MoveLoop, BasicMovementGait.Run, true, 1f, Vector3.forward, 6f));
                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.TurnBack, BasicMovementGait.Run, true, 1f, Vector3.back, 0f));

                Assert.AreEqual(0f, animancer.Graph.Layers[0].CurrentState.NormalizedTime, 0.0001f);
                Assert.AreEqual(0f, locomotionPresenter.CurrentPlaybackProgress.NormalizedTime, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(runClip);
                Object.DestroyImmediate(dodgeClip);
            }
        }

        [Test]
        public void ActionAnimationKeepsRootMotionDisabledAfterTurnBack()
        {
            CreateAnimationPresenterRig(
                out GameObject gameObject,
                out CharacterAnimancerPresenter locomotionPresenter,
                out CharacterAnimancerPresenter actionPresenter,
                out AnimancerComponent animancer,
                out AnimationClip idleClip,
                out AnimationClip runClip,
                out AnimationClip dodgeClip);

            try
            {
                AnimatorRootMotionController rootMotionController = animancer.GetComponent<AnimatorRootMotionController>();
                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.TurnBack, BasicMovementGait.Run, true, 1f, Vector3.back, 5f));
                Assert.False(rootMotionController.ManualRootMotionActive);
                Assert.False(animancer.Animator.applyRootMotion);

                actionPresenter.Present(new CharacterStateAnimationRequest(CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeDirectional.Value, "Dodge Directional"), 1, new ActionAnimationPlaybackIntent(1)));

                Assert.False(rootMotionController.ManualRootMotionActive);
                Assert.False(animancer.Animator.applyRootMotion);
                Assert.AreSame(dodgeClip, animancer.Graph.Layers[0].CurrentState.MainObject);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(runClip);
                Object.DestroyImmediate(dodgeClip);
            }
        }

        [Test]
        public void ActionAnimationKeepsRootMotionDisabledAfterRepeatedTurnBackPolicy()
        {
            CreateAnimationPresenterRig(
                out GameObject gameObject,
                out CharacterAnimancerPresenter locomotionPresenter,
                out CharacterAnimancerPresenter actionPresenter,
                out AnimancerComponent animancer,
                out AnimationClip idleClip,
                out AnimationClip runClip,
                out AnimationClip dodgeClip);

            try
            {
                AnimatorRootMotionController rootMotionController = animancer.GetComponent<AnimatorRootMotionController>();
                actionPresenter.Present(new CharacterStateAnimationRequest(CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeDirectional.Value, "Dodge Directional"), 1, new ActionAnimationPlaybackIntent(1)));
                Assert.False(rootMotionController.ManualRootMotionActive);
                Assert.False(animancer.Animator.applyRootMotion);

                locomotionPresenter.Present(new MovementAnimationContext(BasicMovementPhase.TurnBack, BasicMovementGait.Run, true, 1f, Vector3.back, 5f));
                Assert.False(rootMotionController.ManualRootMotionActive);
                Assert.False(animancer.Animator.applyRootMotion);

                actionPresenter.Present(new CharacterStateAnimationRequest(CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeDirectional.Value, "Dodge Directional"), 2, new ActionAnimationPlaybackIntent(2)));

                Assert.False(rootMotionController.ManualRootMotionActive);
                Assert.False(animancer.Animator.applyRootMotion);
                Assert.AreSame(dodgeClip, animancer.Graph.Layers[0].CurrentState.MainObject);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(runClip);
                Object.DestroyImmediate(dodgeClip);
            }
        }

        [Test]
        public void UnifiedActionAnimationKeepsActiveSameKeyPlaybackWhenPresentedAgain()
        {
            CreateAnimationPresenterRig(
                out GameObject gameObject,
                out CharacterAnimancerPresenter locomotionPresenter,
                out CharacterAnimancerPresenter actionPresenter,
                out AnimancerComponent animancer,
                out AnimationClip idleClip,
                out AnimationClip runClip,
                out AnimationClip dodgeClip);

            try
            {
                CharacterStateAnimationRequest request = new CharacterStateAnimationRequest(
                    CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeDirectional.Value, "Dodge Directional"),
                    1,
                    new ActionAnimationPlaybackIntent(1));
                Assert.True(actionPresenter.Present(in request));
                animancer.Graph.Layers[0].CurrentState.NormalizedTime = 0.75f;

                CharacterStateAnimationRequest chainedRequest = new CharacterStateAnimationRequest(
                    CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeDirectional.Value, "Dodge Directional"),
                    2,
                    new ActionAnimationPlaybackIntent(1));
                Assert.True(actionPresenter.Present(in chainedRequest));

                Assert.AreEqual(0.75f, actionPresenter.CurrentNormalizedTime, 0.0001f);
                Assert.AreSame(dodgeClip, animancer.Graph.Layers[0].CurrentState.MainObject);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
                Object.DestroyImmediate(idleClip);
                Object.DestroyImmediate(runClip);
                Object.DestroyImmediate(dodgeClip);
            }
        }

        [Test]
        public void MissingActionMovementVariantAnimationFailsValidation()
        {
            CharacterStateNodeDefinition dodge = new CharacterStateNodeDefinition(
                CharacterStateIds.Dodge,
                CharacterStateIds.Action,
                "Dodge",
                new[] { CharacterStateTag.Character, CharacterStateTag.Action, CharacterStateTag.Dodge },
                new[]
                {
                    CharacterStateModuleDefinition.ConfiguredActionMotion(
                        new CharacterActionMovementDefinition(CharacterStateVariant.Directional, 0.35f, 4f, true, true),
                        new CharacterActionMovementDefinition(CharacterStateVariant.Backstep, 0.35f, 3f, false, false)),
                    CharacterStateModuleDefinition.ActionAnimation(
                        default,
                        new[]
                        {
                            new CharacterStateVariantDefinition(CharacterStateVariant.Directional, default),
                            new CharacterStateVariantDefinition(CharacterStateVariant.Backstep, CharacterStateAnimationBinding.FromLibraryKey(ActionAnimationKeys.DodgeBackstep.Value, "Backstep"))
                        })
                });
            CharacterStateMachineDefinition definition = new CharacterStateMachineDefinition(
                CharacterStateIds.Idle,
                LoadConfiguredStateMachineDefinition().Nodes.Where(node => node.StateId != CharacterStateIds.Dodge).Concat(new[] { dodge }).ToArray(),
                LoadConfiguredStateMachineDefinition().Transitions.ToArray(),
                LoadConfiguredStateMachineDefinition().TimelinePolicies.ToArray());

            CharacterStateMachineValidationResult validation = definition.Validate();

            Assert.True(validation.HasErrors);
            Assert.That(validation.DescribeErrors(), Does.Contain("Directional"));
        }

        [Test]
        public void GenericActionMovementNodeCanPassValidationWithoutDodgeStateId()
        {
            CharacterStateNodeDefinition roll = new CharacterStateNodeDefinition(
                new CharacterStateId("Action.Roll"),
                CharacterStateIds.Action,
                "Roll",
                new[] { CharacterStateTag.Character, CharacterStateTag.Action },
                new[]
                {
                    CharacterStateModuleDefinition.ConfiguredActionMotion(
                        new CharacterActionMovementDefinition(CharacterStateVariant.None, 0.25f, 2f, true, false)),
                    CharacterStateModuleDefinition.InputConsume(InputRequestKind.Dodge),
                    CharacterStateModuleDefinition.ActionAnimation(
                        CharacterStateAnimationBinding.FromLibraryKey("Action.Roll", "Roll"))
                });
            CharacterStateMachineDefinition defaults = LoadConfiguredStateMachineDefinition();
            CharacterStateMachineDefinition definition = new CharacterStateMachineDefinition(
                CharacterStateIds.Idle,
                defaults.Nodes.Concat(new[] { roll }).ToArray(),
                defaults.Transitions.ToArray(),
                defaults.TimelinePolicies.ToArray());

            CharacterStateMachineValidationResult validation = definition.Validate();

            Assert.False(validation.HasErrors, validation.DescribeErrors());
        }

        [Test]
        public void DuplicateMotionAuthorityModulesFailValidation()
        {
            CharacterStateMachineDefinition defaults = LoadConfiguredStateMachineDefinition();
            CharacterStateNodeDefinition node = new CharacterStateNodeDefinition(
                new CharacterStateId("Locomotion.DuplicateMotion"),
                CharacterStateIds.Locomotion,
                "DuplicateMotion",
                new[] { CharacterStateTag.Character, CharacterStateTag.Locomotion },
                new[]
                {
                    CharacterStateModuleDefinition.InputDrivenMotion(),
                    CharacterStateModuleDefinition.ConfiguredActionMotion(
                        new CharacterActionMovementDefinition(CharacterStateVariant.None, 0.1f, 1f, true, false))
                });
            CharacterStateMachineDefinition definition = new CharacterStateMachineDefinition(
                defaults.InitialState,
                defaults.Nodes.Concat(new[] { node }).ToArray(),
                defaults.Transitions.ToArray(),
                defaults.TimelinePolicies.ToArray());

            CharacterStateMachineValidationResult validation = definition.Validate();

            Assert.True(validation.HasErrors);
            Assert.That(validation.DescribeErrors(), Does.Contain("duplicate motion authority"));
        }

        [Test]
        public void DuplicateAnimationAuthorityModulesFailValidation()
        {
            CharacterStateMachineDefinition defaults = LoadConfiguredStateMachineDefinition();
            CharacterStateNodeDefinition node = new CharacterStateNodeDefinition(
                new CharacterStateId("Action.DuplicateAnimation"),
                CharacterStateIds.Action,
                "DuplicateAnimation",
                new[] { CharacterStateTag.Character, CharacterStateTag.Action },
                new[]
                {
                    CharacterStateModuleDefinition.ConfiguredActionMotion(
                        new CharacterActionMovementDefinition(CharacterStateVariant.None, 0.1f, 1f, true, false)),
                    CharacterStateModuleDefinition.ActionAnimation(
                        CharacterStateAnimationBinding.FromLibraryKey("Action.Roll", "Roll")),
                    CharacterStateModuleDefinition.LocomotionAnimationAlias(
                        CharacterStateAnimationBinding.FromLibraryKey("Locomotion.Turn.Back", "Turn Back"))
                });
            CharacterStateMachineDefinition definition = new CharacterStateMachineDefinition(
                defaults.InitialState,
                defaults.Nodes.Concat(new[] { node }).ToArray(),
                defaults.Transitions.ToArray(),
                defaults.TimelinePolicies.ToArray());

            CharacterStateMachineValidationResult validation = definition.Validate();

            Assert.True(validation.HasErrors);
            Assert.That(validation.DescribeErrors(), Does.Contain("duplicate animation authority"));
        }

        [Test]
        public void CommittedActionInputRequestBuilderBuildsDirectionalDodgeRequest()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 2, 4);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero);
            LocomotionDecisionFacts facts = DecisionFacts(in input, in settings, Vector3.forward, Vector3.forward);
            DodgeActionTuning config = TestDodgeTuning();

            bool built = CommittedActionInputRequestBuilder.TryBuildDodgeRequest(
                buffer,
                2,
                in input,
                false,
                in facts,
                in config,
                out DodgeActionRequest request);

            Assert.True(built);
            Assert.AreEqual(DodgeActionVariant.Directional, request.Variant);
            Assert.AreEqual(config.Priority, request.Priority);
            Assert.AreEqual(2, request.OriginStep);
            Assert.AreEqual(6, request.ExpireStep);
            Assert.AreEqual(ActionStateIds.Dodge, request.TargetState);
            Assert.AreEqual(Vector3.forward, request.WorldDirection);
        }

        [Test]
        public void CommittedActionInputRequestBuilderUsesLocomotionDecisionFactsDirection()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 2, 4);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.left, Vector2.zero);
            LocomotionDecisionFacts facts = DecisionFacts(in input, in settings, Vector3.right, Vector3.forward);
            DodgeActionTuning config = TestDodgeTuning();

            bool built = CommittedActionInputRequestBuilder.TryBuildDodgeRequest(
                buffer,
                2,
                in input,
                false,
                in facts,
                in config,
                out DodgeActionRequest request);

            Assert.True(built);
            Assert.AreEqual(DodgeActionVariant.Directional, request.Variant);
            Assert.AreEqual(Vector3.right, request.WorldDirection);
        }

        [Test]
        public void CommittedActionInputRequestBuilderBuildsBackstepDodgeRequest()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 3, 4);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.zero, Vector2.zero);
            LocomotionDecisionFacts facts = DecisionFacts(in input, in settings, Vector3.zero, Vector3.forward);
            DodgeActionTuning config = TestDodgeTuning();

            bool built = CommittedActionInputRequestBuilder.TryBuildDodgeRequest(
                buffer,
                3,
                in input,
                false,
                in facts,
                in config,
                out DodgeActionRequest request);

            Assert.True(built);
            Assert.AreEqual(DodgeActionVariant.Backstep, request.Variant);
            Assert.AreEqual(Vector3.back, request.WorldDirection);
        }

        [Test]
        public void CommittedActionInterruptRequestFactoryAcceptedDecisionBuildsDodgeFact()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 4, 4);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero);
            LocomotionDecisionFacts facts = DecisionFacts(in input, in settings, Vector3.forward, Vector3.forward);
            DodgeActionTuning config = TestDodgeTuning();

            CharacterInputRequestFact fact = CommittedActionInterruptRequestFactory.BuildDodgeRequestFact(
                buffer,
                4,
                CharacterStateMachineSnapshot.Inactive,
                in input,
                false,
                in facts,
                in config,
                0,
                new[] { new ActionInterruptPolicy(ActionStateIds.None, ActionStateIds.Dodge, 30) },
                out ActionInterruptDecision decision);

            Assert.True(decision.Accepted);
            Assert.True(fact.HasRequest);
            Assert.AreEqual(CharacterStateVariant.Directional, fact.Variant);
            Assert.AreEqual(config.Priority, fact.Priority);
            Assert.AreEqual(Vector3.forward, fact.WorldDirection);
        }

        [Test]
        public void CommittedActionInterruptRequestFactoryRejectedDecisionDoesNotBuildFact()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 5, 4);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero);
            LocomotionDecisionFacts facts = DecisionFacts(in input, in settings, Vector3.forward, Vector3.forward);
            DodgeActionTuning config = TestDodgeTuning();

            CharacterInputRequestFact fact = CommittedActionInterruptRequestFactory.BuildDodgeRequestFact(
                buffer,
                5,
                CharacterStateMachineSnapshot.Inactive,
                in input,
                false,
                in facts,
                in config,
                0,
                new[] { new ActionInterruptPolicy(ActionStateIds.None, ActionStateIds.Dodge, 31) },
                out ActionInterruptDecision decision);

            Assert.False(decision.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.PriorityTooLow, decision.RejectReason);
            Assert.False(fact.HasRequest);
            Assert.True(buffer.TryPeek(InputRequestKind.Dodge, 5, out _));
        }

        [Test]
        public void CommittedActionInterruptRequestFactoryResistanceRejectsDodgeAndKeepsBufferRequest()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 6, 4);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero);
            LocomotionDecisionFacts facts = DecisionFacts(in input, in settings, Vector3.forward, Vector3.forward);
            DodgeActionTuning config = TestDodgeTuning();

            CharacterInputRequestFact fact = CommittedActionInterruptRequestFactory.BuildDodgeRequestFact(
                buffer,
                6,
                CharacterStateMachineSnapshot.Inactive,
                in input,
                false,
                in facts,
                in config,
                30,
                new[] { new ActionInterruptPolicy(ActionStateIds.None, ActionStateIds.Dodge, 30) },
                out ActionInterruptDecision decision);

            Assert.False(decision.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.BlockedByResistance, decision.RejectReason);
            Assert.False(fact.HasRequest);
            Assert.True(buffer.TryPeek(InputRequestKind.Dodge, 6, out _));
        }

        [Test]
        public void CommittedActionInterruptRequestFactoryForcePolicyBypassesResistance()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 7, 4);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero);
            LocomotionDecisionFacts facts = DecisionFacts(in input, in settings, Vector3.forward, Vector3.forward);
            DodgeActionTuning config = TestDodgeTuning();

            CharacterInputRequestFact fact = CommittedActionInterruptRequestFactory.BuildDodgeRequestFact(
                buffer,
                7,
                CharacterStateMachineSnapshot.Inactive,
                in input,
                false,
                in facts,
                in config,
                100,
                new[] { new ActionInterruptPolicy(ActionStateIds.None, ActionStateIds.Dodge, 30, force: true) },
                out ActionInterruptDecision decision);

            Assert.True(decision.Accepted);
            Assert.True(fact.HasRequest);
        }

        [Test]
        public void CommittedActionInterruptRequestFactoryUsesDodgeTimelineWindow()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 7, 4);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero);
            LocomotionDecisionFacts facts = DecisionFacts(in input, in settings, Vector3.forward, Vector3.forward);
            DodgeActionTuning config = TestDodgeTuning();
            StateTimelineWindowFacts dodgeWindow = new StateTimelineWindowFacts(
                CharacterStateIds.Dodge,
                0.5f,
                true,
                0.2f,
                false,
                false,
                true,
                false,
                0,
                0,
                30,
                false,
                "dodge-chain-cancel",
                "dodge-chain-cancel");

            CharacterInputRequestFact rejected = CommittedActionInterruptRequestFactory.BuildDodgeRequestFact(
                buffer,
                7,
                CharacterStateMachineSnapshot.Inactive,
                in input,
                false,
                in facts,
                in config,
                0,
                new[] { new ActionInterruptPolicy(ActionStateIds.None, ActionStateIds.Dodge, 30, windowId: "dodge-chain-cancel") },
                default,
                out ActionInterruptDecision rejectedDecision);
            CharacterInputRequestFact accepted = CommittedActionInterruptRequestFactory.BuildDodgeRequestFact(
                buffer,
                7,
                CharacterStateMachineSnapshot.Inactive,
                in input,
                false,
                in facts,
                in config,
                0,
                new[] { new ActionInterruptPolicy(ActionStateIds.None, ActionStateIds.Dodge, 30, windowId: "dodge-chain-cancel") },
                dodgeWindow,
                out ActionInterruptDecision acceptedDecision);

            Assert.False(rejected.HasRequest);
            Assert.AreEqual(ActionInterruptRejectReason.TimingNotSatisfied, rejectedDecision.RejectReason);
            Assert.True(acceptedDecision.Accepted);
            Assert.True(accepted.HasRequest);
        }

        [Test]
        public void CommittedActionInterruptRequestFactoryRejectsDodgeToDodgeWhenPriorityDoesNotBeatResistance()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 8, 4);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero);
            CharacterStateMachineSnapshot snapshot = DodgeSnapshot(0.1f);
            DodgeActionTuning config = new DodgeActionTuning(0.35f, 4f, 0.35f, 3f, 30, 40, true, false);
            LocomotionDecisionFacts facts = DecisionFacts(in input, in settings, Vector3.forward, Vector3.forward);

            CharacterInputRequestFact fact = CommittedActionInterruptRequestFactory.BuildDodgeRequestFact(
                buffer,
                8,
                in snapshot,
                in input,
                false,
                in facts,
                in config,
                ResolveActionResistance(ActionStateIds.Dodge, in config),
                new[] { new ActionInterruptPolicy(ActionStateIds.Dodge, ActionStateIds.Dodge, 30) },
                out ActionInterruptDecision decision);

            Assert.False(decision.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.BlockedByResistance, decision.RejectReason);
            Assert.False(fact.HasRequest);
            Assert.True(buffer.TryPeek(InputRequestKind.Dodge, 8, out _));
        }

        [Test]
        public void CommittedActionInterruptRequestFactoryForceDodgeToDodgeBypassesCurrentResistance()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 9, 4);
            BasicMovementSettings settings = BasicMovementSettings.FromConfig(null);
            BasicLocomotionInputSnapshot input = new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero);
            CharacterStateMachineSnapshot snapshot = DodgeSnapshot(0.1f);
            DodgeActionTuning config = new DodgeActionTuning(0.35f, 4f, 0.35f, 3f, 30, 40, true, false);
            LocomotionDecisionFacts facts = DecisionFacts(in input, in settings, Vector3.forward, Vector3.forward);

            CharacterInputRequestFact fact = CommittedActionInterruptRequestFactory.BuildDodgeRequestFact(
                buffer,
                9,
                in snapshot,
                in input,
                false,
                in facts,
                in config,
                ResolveActionResistance(ActionStateIds.Dodge, in config),
                new[] { new ActionInterruptPolicy(ActionStateIds.Dodge, ActionStateIds.Dodge, 30, force: true) },
                out ActionInterruptDecision decision);

            Assert.True(decision.Accepted);
            Assert.True(fact.HasRequest);
            Assert.AreEqual(30, decision.SelectedRequest.Priority);
        }

        [Test]
        public void RuntimeBlackboardDefaultsAndWritesTypedFacts()
        {
            CharacterRuntimeBlackboard blackboard = new CharacterRuntimeBlackboard();
            CharacterRuntimeBlackboardSnapshot initial = blackboard.Snapshot;

            Assert.AreEqual(BasicMovementPhase.Idle, initial.Locomotion.Phase);
            Assert.AreEqual(BasicMovementGait.Walk, initial.Locomotion.LastMovingGait);
            Assert.False(initial.Action.Active);
            Assert.False(initial.Animation.ActionHasValidPlayback);
            Assert.False(initial.Animation.ActionIsEnded);
            Assert.AreEqual(string.Empty, initial.Debug.LastWriter);

            blackboard.WriteLocomotionFacts(new CharacterRuntimeLocomotionFacts(
                BasicMovementPhase.MoveStop,
                BasicMovementGait.Run,
                BasicMovementGait.Run,
                true,
                BasicMovementGait.Run,
                true,
                new Vector3(0f, 2f, 5f),
                false,
                0f,
                12));
            blackboard.WriteActionFacts(new CharacterRuntimeActionFacts(
                true,
                ActionStateIds.Dodge,
                false,
                false,
                true,
                Vector3.forward,
                0.3f,
                true,
                13));
            blackboard.WriteAnimationFacts(new CharacterRuntimeAnimationFacts(
                new AnimationPhasePlaybackProgress(BasicMovementPhase.MoveLoop, "RunLoop", 0.4f, true, false),
                "RunLoop",
                ActionAnimationKeys.DodgeDirectional,
                0.25f,
                true,
                true,
                "Dodge Directional",
                14));

            CharacterRuntimeBlackboardSnapshot snapshot = blackboard.Snapshot;
            Assert.AreEqual(BasicMovementPhase.MoveStop, snapshot.Locomotion.Phase);
            Assert.AreEqual(BasicMovementGait.Run, snapshot.Locomotion.MoveStopEntryGait);
            Assert.AreEqual(ActionStateIds.Dodge, snapshot.Action.State);
            Assert.AreEqual(ActionAnimationKeys.DodgeDirectional, snapshot.Animation.ActionKey);
            Assert.True(snapshot.Animation.ActionIsEnded);
            Assert.AreEqual("Animation", snapshot.Debug.LastWriter);
            Assert.AreEqual(14, snapshot.Debug.LastWriteStep);
        }

        [Test]
        public void RuntimeBlackboardRestoreIsIdempotent()
        {
            CharacterRuntimeBlackboard blackboard = new CharacterRuntimeBlackboard();
            blackboard.WriteActionFacts(new CharacterRuntimeActionFacts(
                true,
                ActionStateIds.Dodge,
                true,
                true,
                false,
                Vector3.zero,
                0f,
                false,
                20));
            CharacterRuntimeBlackboardRestoreState restoreState = blackboard.CaptureRestoreState();

            blackboard.Reset();
            blackboard.Restore(in restoreState);
            CharacterRuntimeBlackboardSnapshot once = blackboard.Snapshot;
            blackboard.Restore(in restoreState);
            CharacterRuntimeBlackboardSnapshot twice = blackboard.Snapshot;

            Assert.True(once.Action.Active);
            Assert.True(twice.Action.Active);
            Assert.AreEqual(once.Action.State, twice.Action.State);
            Assert.AreEqual(once.Action.Completed, twice.Action.Completed);
            Assert.AreEqual(once.Debug.LastWriter, twice.Debug.LastWriter);
            Assert.AreEqual(once.Debug.LastWriteStep, twice.Debug.LastWriteStep);
        }

        [Test]
        public void CharacterStateMachineContextCarriesRuntimeBlackboardSnapshot()
        {
            CharacterRuntimeBlackboard blackboard = new CharacterRuntimeBlackboard();
            blackboard.WriteLocomotionFacts(new CharacterRuntimeLocomotionFacts(
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                BasicMovementGait.Run,
                false,
                BasicMovementGait.Walk,
                true,
                Vector3.forward,
                true,
                1f,
                30));

            CharacterStateMachineContext context = Context(
                move: true,
                runtimeBlackboard: blackboard.Snapshot);

            Assert.AreEqual(BasicMovementGait.Run, context.RuntimeBlackboard.Locomotion.LastMovingGait);
            Assert.True(context.RuntimeBlackboard.Locomotion.RunLatchActive);
            Assert.AreEqual("Locomotion", context.RuntimeBlackboard.Debug.LastWriter);
        }

        [Test]
        public void CharacterControllerExecutorAppliesTurnBackAnimationMotionWithoutInputMotion()
        {
            GameObject gameObject = new GameObject("turnback-motion-executor-test");

            try
            {
                CharacterController characterController = gameObject.AddComponent<CharacterController>();
                CharacterControllerBasicMotionExecutor executor = new CharacterControllerBasicMotionExecutor(
                    characterController,
                    gameObject.transform,
                    false,
                    -20f,
                    -2f);
                MovementCommand command = new MovementCommand(
                    Vector3.right,
                    10f,
                    720f,
                    0.1f,
                    BasicMovementPhase.TurnBack,
                    BasicMovementGait.Run,
                    new BasicMovementMotionFacts(
                        true,
                        new Vector3(0f, 0f, 1f),
                        90f,
                        BasicMovementPhase.TurnBack,
                        "Locomotion.Turn.Back",
                        true,
                        true));

                executor.ExecuteBasicMovement(in command);

                Assert.AreEqual(0f, gameObject.transform.position.x, 0.001f);
                Assert.AreEqual(1f, gameObject.transform.position.z, 0.001f);
                Assert.AreEqual(90f, gameObject.transform.eulerAngles.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CharacterControllerExecutorDoesNotRotateWorldRootMotionDeltaTwice()
        {
            GameObject gameObject = new GameObject("turnback-world-root-motion-executor-test");

            try
            {
                gameObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                CharacterController characterController = gameObject.AddComponent<CharacterController>();
                CharacterControllerBasicMotionExecutor executor = new CharacterControllerBasicMotionExecutor(
                    characterController,
                    gameObject.transform,
                    false,
                    -20f,
                    -2f);
                MovementCommand command = new MovementCommand(
                    Vector3.back,
                    10f,
                    720f,
                    0.1f,
                    BasicMovementPhase.TurnBack,
                    BasicMovementGait.Run,
                    new BasicMovementMotionFacts(
                        true,
                        new Vector3(0f, 0f, 1f),
                        0f,
                        BasicMovementPhase.TurnBack,
                        "Locomotion.Turn.Back",
                        true,
                        true,
                        BasicMovementPlanarDeltaSpace.World));

                executor.ExecuteBasicMovement(in command);

                Assert.AreEqual(0f, gameObject.transform.position.x, 0.001f);
                Assert.AreEqual(1f, gameObject.transform.position.z, 0.001f);
                Assert.AreEqual(90f, gameObject.transform.eulerAngles.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CharacterControllerExecutorUsesEntryLocalBasisInsteadOfCurrentRootYaw()
        {
            GameObject gameObject = new GameObject("turnback-entry-local-motion-executor-test");

            try
            {
                gameObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                CharacterController characterController = gameObject.AddComponent<CharacterController>();
                CharacterControllerBasicMotionExecutor executor = new CharacterControllerBasicMotionExecutor(
                    characterController,
                    gameObject.transform,
                    false,
                    -20f,
                    -2f);
                MovementCommand command = new MovementCommand(
                    Vector3.zero,
                    0f,
                    0f,
                    0.1f,
                    BasicMovementPhase.TurnBack,
                    BasicMovementGait.Run,
                    new BasicMovementMotionFacts(
                        true,
                        new Vector3(1f, 0f, 0f),
                        0f,
                        BasicMovementPhase.TurnBack,
                        "Locomotion.Turn.Back",
                        true,
                        true,
                        BasicMovementPlanarDeltaSpace.EntryLocal,
                        TurnBackMotionPolicy.Default,
                        Vector3.forward));

                executor.ExecuteBasicMovement(in command);

                Assert.AreEqual(1f, gameObject.transform.position.x, 0.001f);
                Assert.AreEqual(0f, gameObject.transform.position.z, 0.001f);
                Assert.AreEqual(90f, gameObject.transform.eulerAngles.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CharacterControllerExecutorDoesNotFallbackEntryLocalToCurrentRootYawWithoutBasis()
        {
            GameObject gameObject = new GameObject("turnback-entry-local-missing-basis-test");

            try
            {
                gameObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                CharacterController characterController = gameObject.AddComponent<CharacterController>();
                CharacterControllerBasicMotionExecutor executor = new CharacterControllerBasicMotionExecutor(
                    characterController,
                    gameObject.transform,
                    false,
                    -20f,
                    -2f);
                MovementCommand command = new MovementCommand(
                    Vector3.zero,
                    0f,
                    0f,
                    0.1f,
                    BasicMovementPhase.TurnBack,
                    BasicMovementGait.Run,
                    new BasicMovementMotionFacts(
                        true,
                        new Vector3(0f, 0f, 1f),
                        0f,
                        BasicMovementPhase.TurnBack,
                        "Locomotion.Turn.Back",
                        true,
                        true,
                        BasicMovementPlanarDeltaSpace.EntryLocal,
                        TurnBackMotionPolicy.Default));

                executor.ExecuteBasicMovement(in command);

                Assert.AreEqual(0f, gameObject.transform.position.x, 0.001f);
                Assert.AreEqual(0f, gameObject.transform.position.z, 0.001f);
                Assert.AreEqual(90f, gameObject.transform.eulerAngles.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CharacterControllerExecutorStillRotatesLocalBakedMotionDelta()
        {
            GameObject gameObject = new GameObject("local-baked-motion-executor-test");

            try
            {
                gameObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                CharacterController characterController = gameObject.AddComponent<CharacterController>();
                CharacterControllerBasicMotionExecutor executor = new CharacterControllerBasicMotionExecutor(
                    characterController,
                    gameObject.transform,
                    false,
                    -20f,
                    -2f);
                MovementCommand command = new MovementCommand(
                    Vector3.zero,
                    0f,
                    0f,
                    0.1f,
                    BasicMovementPhase.MoveStart,
                    BasicMovementGait.Run,
                    new BasicMovementMotionFacts(
                        true,
                        new Vector3(0f, 0f, 1f),
                        0f,
                        BasicMovementPhase.MoveStart,
                        "RunStart"));

                executor.ExecuteBasicMovement(in command);

                Assert.AreEqual(1f, gameObject.transform.position.x, 0.001f);
                Assert.AreEqual(0f, gameObject.transform.position.z, 0.001f);
                Assert.AreEqual(90f, gameObject.transform.eulerAngles.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CharacterControllerExecutorRollbackStateRestoresRootPose()
        {
            GameObject gameObject = new GameObject("motion-executor-root-pose-restore-test");

            try
            {
                CharacterController characterController = gameObject.AddComponent<CharacterController>();
                CharacterControllerBasicMotionExecutor executor = new CharacterControllerBasicMotionExecutor(
                    characterController,
                    gameObject.transform,
                    false,
                    -20f,
                    -2f);

                gameObject.transform.SetPositionAndRotation(new Vector3(3f, 0f, 4f), Quaternion.Euler(0f, 37f, 0f));
                MotionExecutorRollbackState state = executor.CaptureRollbackState();

                gameObject.transform.SetPositionAndRotation(new Vector3(-8f, 0f, 9f), Quaternion.Euler(0f, 220f, 0f));
                executor.RestoreRollbackState(in state);

                Assert.AreEqual(3f, gameObject.transform.position.x, 0.001f);
                Assert.AreEqual(4f, gameObject.transform.position.z, 0.001f);
                Assert.AreEqual(37f, gameObject.transform.eulerAngles.y, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FullBodySubmissionTurnBackRequestDoesNotRequireDodgeConfig()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true, runHeld: true));
            runner.Tick(Context(move: true, canExit: true, runHeld: true));
            LocomotionTurnBackIntent intent = TurnBackIntent(4, 6, 180f, Vector3.back, Vector3.forward);
            MovementInputIntent moveIntent = MovementInputIntent.FromRaw(Vector2.down, 0.1f, true);
            LocomotionDecisionFacts facts = new LocomotionDecisionFacts(
                moveIntent,
                BasicMovementGait.Run,
                BasicMovementPhaseFacts.None,
                new LocomotionSpatialFacts(Vector3.back, Vector3.forward, Vector3.forward, Vector3.right),
                intent);
            ActionInterruptPolicy[] policies =
            {
                new ActionInterruptPolicy(
                    new ActionStateId(CharacterStateIds.MoveLoop.Value),
                    new ActionStateId(CharacterStateIds.TurnBack.Value),
                    20,
                    requiredFactId: TimelineFactIds.TurnBackEnterOpen.Value)
            };
            CommittedActionRequestSubmissionResolverInput input = new CommittedActionRequestSubmissionResolverInput(
                null,
                4,
                0.1f,
                runner.Snapshot,
                new BasicLocomotionInputSnapshot(0.1f, Vector2.down, Vector2.zero, true),
                true,
                facts,
                new StateTimelineWindowFacts(
                    runner.Snapshot.ActiveState,
                    0f,
                    false,
                    runner.Snapshot.StateTime,
                    false,
                    false,
                    true,
                    false,
                    0,
                    0,
                    20,
                    false,
                    "turnback-enter",
                    "turnback-enter",
                    TimelineFactIds.TurnBackEnterOpen.Value,
                    TimelineFactIds.TurnBackEnterOpen.Value),
                false,
                CharacterActionCatalog.Empty,
                0,
                policies);

            CharacterActionRequestSubmissionResult result = CommittedActionRequestSubmissionResolver.Resolve(in input);

            Assert.True(result.Accepted);
            Assert.AreEqual(InputRequestKind.TurnBack, result.Request.RequestKind);
            Assert.AreEqual(CharacterStateIds.TurnBack.Value, result.Decision.TargetState.Value);
        }

        [Test]
        public void FullBodySubmissionTurnBackRequestWithoutCurrentTimelineFactsIsRejected()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true, runHeld: true));
            runner.Tick(Context(move: true, canExit: true, runHeld: true));
            LocomotionTurnBackIntent intent = TurnBackIntent(4, 6, 180f, Vector3.back, Vector3.forward);
            MovementInputIntent moveIntent = MovementInputIntent.FromRaw(Vector2.down, 0.1f, true);
            LocomotionDecisionFacts facts = new LocomotionDecisionFacts(
                moveIntent,
                BasicMovementGait.Run,
                BasicMovementPhaseFacts.None,
                new LocomotionSpatialFacts(Vector3.back, Vector3.forward, Vector3.forward, Vector3.right),
                intent);
            ActionInterruptPolicy[] policies =
            {
                new ActionInterruptPolicy(
                    new ActionStateId(CharacterStateIds.MoveLoop.Value),
                    new ActionStateId(CharacterStateIds.TurnBack.Value),
                    20,
                    requiredFactId: TimelineFactIds.TurnBackEnterOpen.Value)
            };
            CommittedActionRequestSubmissionResolverInput input = new CommittedActionRequestSubmissionResolverInput(
                null,
                4,
                0.1f,
                runner.Snapshot,
                new BasicLocomotionInputSnapshot(0.1f, Vector2.down, Vector2.zero, true),
                true,
                facts,
                StateTimelineWindowFacts.None(runner.Snapshot.ActiveState),
                false,
                CharacterActionCatalog.Empty,
                0,
                policies);

            CharacterActionRequestSubmissionResult result = CommittedActionRequestSubmissionResolver.Resolve(in input);

            Assert.False(result.Accepted);
            Assert.AreEqual(ActionInterruptRejectReason.TimingNotSatisfied, result.Decision.RejectReason);
        }

        [Test]
        public void ResolveCurrentActionResistanceReturnsZeroForNoAction()
        {
            DodgeActionTuning config = new DodgeActionTuning(0.35f, 4f, 0.35f, 3f, 30, 55, true, false);

            int resistance = ResolveActionResistance(ActionStateIds.None, in config);

            Assert.AreEqual(0, resistance);
        }

        [Test]
        public void ResolveCurrentActionResistanceReturnsDodgeConfigResistanceForDodge()
        {
            DodgeActionTuning config = new DodgeActionTuning(0.35f, 4f, 0.35f, 3f, 30, 55, true, false);

            int resistance = ResolveActionResistance(ActionStateIds.Dodge, in config);

            Assert.AreEqual(55, resistance);
        }

        [Test]
        public void ConfiguredLocomotionStateGraphDoesNotContainDodgeEntryTransitions()
        {
            CharacterStateMachineDefinition definition = LoadConfiguredLocomotionStateGraphDefinition();

            Assert.False(definition.Transitions.Any(transition =>
                transition.FromStateId.StartsWith("Action.", System.StringComparison.Ordinal) ||
                transition.ToStateId.Value.StartsWith("Action.", System.StringComparison.Ordinal)));

            string policyAsset = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Configs/3C/Action/Corin/InterruptPolicy/CorinActionInterruptPolicySet.asset"));
            Assert.That(policyAsset, Does.Contain("targetStateId: Action.Dodge"));
            Assert.That(policyAsset, Does.Contain("fromStateId: Action.Dodge"));
        }

        [Test]
        public void TimelineSamplerUsesModulePlaybackFactSource()
        {
            CharacterStateMachineDefinition definition = LoadConfiguredStateMachineDefinition();
            Assert.True(definition.TryGetNode(CharacterStateIds.TurnBack, out CharacterStateNodeDefinition turnBack));

            CharacterStateMachineContext context = Context(
                move: true,
                runtimeBlackboard: BlackboardWithLocomotionProgress("Locomotion.Turn.Back", 0.5f, false));

            StateTimelineWindowFacts facts = CharacterStateTimelineFactSampler.SampleCurrent(
                definition,
                turnBack,
                CharacterStateIds.TurnBack,
                CharacterStateVariant.None,
                in context,
                0.2f,
                ActionRequestType.Locomotion);

            Assert.True(facts.HasValidNormalizedTime);
            Assert.AreEqual(0.5f, facts.NormalizedTime, 0.0001f);
        }

        [Test]
        public void StateFrameAnimationPresentationUsesAnimationOutputSource()
        {
            string outputRuntime = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Action/Runtime/CharacterFrameOutputRuntime.cs"));
            string presenter = ExtractSourceBlock(
                outputRuntime,
                "internal sealed class CharacterAnimationOutputPresenter",
                "internal sealed class CharacterFrameRuntimeFactsWriter");

            Assert.That(presenter, Does.Contain("animationRequest.IsActionAnimation"));
            Assert.That(presenter, Does.Not.Contain("stateFrame.Owner.IsAction"));
        }

        [Test]
        public void FullBodyExitToLocomotionUsesStateModulesInsteadOfOwner()
        {
            string locomotionSubmitter = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Movement/Runtime/LocomotionFrameSubmitter.cs"));
            string contextTypes = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Pipeline/Model/CharacterFrameContext.cs"));
            string gameplayDecision = ExtractMethodBody(locomotionSubmitter, "TrySubmitFrameOutput");
            string setStateDecision = ExtractMethodBody(contextTypes, "SetStateDecision");

            Assert.That(gameplayDecision, Does.Contain("previousNode.IsActionCapabilityState"));
            Assert.That(setStateDecision, Does.Contain("previousActionCapabilityState"));
            Assert.That(setStateDecision, Does.Not.Contain("previousSnapshot.Owner.IsAction"));
            Assert.That(setStateDecision, Does.Not.Contain("decision.StateFrame.Owner.IsAction"));
        }

        [Test]
        public void StateNodeInspectorOnlyDrawsCoreFieldsAndModules()
        {
            string drawer = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Editor/Character/StateMachine/CharacterStateMachineDefinitionDrawers.cs"));
            string classBody = drawer.Substring(drawer.IndexOf("CharacterStateNodeDefinitionDrawer", System.StringComparison.Ordinal));
            string body = ExtractMethodBody(classBody, "OnGUI");

            Assert.That(body, Does.Contain("stateId"));
            Assert.That(body, Does.Contain("parentStateId"));
            Assert.That(body, Does.Contain("pathSegment"));
            Assert.That(body, Does.Contain("tags"));
            Assert.That(body, Does.Contain("modules"));
            Assert.That(body, Does.Not.Contain("\"output\""));
            Assert.That(body, Does.Not.Contain("\"animation\""));
            Assert.That(body, Does.Not.Contain("\"variants\""));
        }

        [Test]
        public void StateNodeDefinitionDoesNotKeepLegacyUniversalFields()
        {
            string model = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/StateMachine/Model/CharacterStateMachineTypes.cs"));

            Assert.That(model, Does.Not.Contain("[SerializeField, HideInInspector] CharacterStateVariantDefinition[] variants"));
            Assert.That(model, Does.Not.Contain("[SerializeField, HideInInspector] CharacterStateOutputDefinition output"));
            Assert.That(model, Does.Not.Contain("[SerializeField, HideInInspector] CharacterStateAnimationBinding animation"));
            Assert.That(model, Does.Not.Contain("BuildModulesFromLegacy"));
        }

        [Test]
        public void StateModuleInspectorDrawsPayloadByModuleType()
        {
            string drawer = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Editor/Character/StateMachine/CharacterStateMachineDefinitionDrawers.cs"));
            string body = ExtractMethodBody(drawer, "DrawPayload");

            Assert.That(body, Does.Contain("CharacterStateModuleType.LocomotionPhase"));
            Assert.That(body, Does.Contain("CharacterStateModuleType.ConfiguredActionMotion"));
            Assert.That(body, Does.Contain("CharacterStateModuleType.ActionAnimation"));
            Assert.That(body, Does.Contain("CharacterStateModuleType.LocomotionAnimationAlias"));
            Assert.That(body, Does.Contain("CharacterStateModuleType.TurnBackMotionPolicy"));
            Assert.That(body, Does.Contain("CharacterStateModuleType.InputConsume"));
            Assert.That(body, Does.Contain("CharacterStateModuleType.RunLatch"));
            Assert.That(body, Does.Contain("CharacterStateModuleType.TimelineWindow"));
        }

        [Test]
        public void RunnerAndEvaluatorDoNotReferenceForbiddenRuntimeObjects()
        {
            string root = Path.Combine(Application.dataPath, "Scripts/Character/StateMachine/Solver");
            string combined = string.Join("\n", new[]
            {
                Path.Combine(root, "Runtime", "CharacterStateMachineRunner.cs"),
                Path.Combine(root, "Runtime", "CharacterStateLifecycle.cs"),
                Path.Combine(root, "Timeline", "StateTimelineSampler.cs"),
                Path.Combine(root, "Timeline", "CharacterStateTimelineFactSampler.cs"),
                Path.Combine(root, "Transition", "CharacterStateTransitionEvaluator.cs"),
                Path.Combine(root, "Output", "CharacterStateOutputResolver.cs"),
                Path.Combine(root, "Validation", "CharacterStateMachineValidator.cs")
            }.Select(File.ReadAllText));

            Assert.That(combined, Does.Not.Contain("UnityHFSM"));
            Assert.That(combined, Does.Not.Contain("Animancer"));
            Assert.That(combined, Does.Not.Contain("Animator"));
            Assert.That(combined, Does.Not.Contain("CharacterController"));
            Assert.That(combined, Does.Not.Contain("UnityEngine.InputSystem"));
            Assert.That(combined, Does.Not.Contain("InputAction"));
            Assert.That(combined, Does.Not.Contain("Transform"));
            Assert.That(combined, Does.Not.Contain("Cinemachine"));
            Assert.That(combined, Does.Not.Contain("DodgeActionTuning"));
            Assert.That(combined, Does.Not.Contain("CharacterStateIds.Dodge"));
            Assert.That(combined, Does.Not.Contain("ActionInterruptArbiter"));
            Assert.That(combined, Does.Not.Contain("ActionInterruptPolicySetSO"));
            Assert.That(combined, Does.Not.Contain(".Move("));
            Assert.That(combined, Does.Not.Contain(".Present("));
        }

        [Test]
        public void RunnerDelegatesTimelineLifecycleAndOutputResolution()
        {
            string root = Path.Combine(Application.dataPath, "Scripts/Character/StateMachine/Solver");
            string runner = File.ReadAllText(Path.Combine(root, "Runtime", "CharacterStateMachineRunner.cs"));
            string resolver = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Action/Solver/CommittedActionRequestSubmissionResolver.cs"));

            Assert.That(runner, Does.Contain("ICharacterStateLifecycle"));
            Assert.That(runner, Does.Contain("CharacterStateTimelineFactSampler.SampleCurrent"));
            Assert.That(runner, Does.Contain("CharacterStateOutputResolver.Resolve"));
            Assert.That(runner, Does.Contain("WithProjectedTimelineFacts"));
            Assert.That(runner, Does.Contain("WithTargetTimelineFacts"));
            Assert.That(resolver, Does.Not.Contain("CharacterStateMachineRunner"));
            Assert.That(resolver, Does.Not.Contain("CharacterStateMachineDefinition"));
            Assert.That(resolver, Does.Not.Contain("CharacterStateTimelineFactSampler.SampleCurrent"));
            Assert.That(resolver, Does.Contain("CurrentTimelineFacts"));
        }

        [Test]
        public void CharacterStateOutputResolverEmitsActionMotionSpecWithoutCommandMath()
        {
            string resolver = File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "Scripts/Character/StateMachine/Solver/Output/CharacterStateOutputResolver.cs"),
                System.Text.Encoding.UTF8);

            Assert.That(resolver, Does.Contain("ActionMotionSpec"));
            Assert.That(resolver, Does.Not.Contain("new ActionMovementCommand"));
            Assert.That(resolver, Does.Not.Contain("frameDistance"));
            Assert.That(resolver, Does.Not.Contain("ActionCompleted"));
        }

        [Test]
        public void ActionMotionResolverWritesRunLatchOnlyWhenCompletedWithMoveIntent()
        {
            ActionMotionSpec movingSpec = new ActionMotionSpec(
                ActionStateIds.Dodge,
                CharacterStateIds.Dodge,
                CharacterStateVariant.Directional,
                0.35f,
                4f,
                true,
                true,
                Vector3.forward,
                0.1f,
                42);
            ActionMotionResolveInput movingInput = new ActionMotionResolveInput(
                movingSpec,
                0.1f,
                StateTimelineWindowFacts.None(default),
                CharacterRuntimeActionFacts.Default,
                true);

            ActionMotionResolveResult moving = ActionMotionResolver.Resolve(in movingInput);

            Assert.True(moving.HasActionMovement);
            Assert.False(moving.ActionCompleted);
            Assert.False(moving.SetRunLatch);
            Assert.AreEqual(4f * 0.1f / 0.35f, moving.MovementCommand.PlanarDistance, 0.0001f);
            Assert.AreEqual(42, moving.SourceStep);

            ActionMotionSpec completedSpec = new ActionMotionSpec(
                ActionStateIds.Dodge,
                CharacterStateIds.Dodge,
                CharacterStateVariant.Directional,
                0.35f,
                4f,
                true,
                true,
                Vector3.forward,
                0.35f,
                43);
            ActionMotionResolveInput completedInput = new ActionMotionResolveInput(
                completedSpec,
                0.25f,
                StateTimelineWindowFacts.None(default),
                CharacterRuntimeActionFacts.Default,
                true);

            ActionMotionResolveResult completed = ActionMotionResolver.Resolve(in completedInput);

            Assert.True(completed.ActionCompleted);
            Assert.True(completed.SetRunLatch);
            Assert.AreEqual(43, completed.SourceStep);

            ActionMotionResolveInput completedWithoutMoveInput = new ActionMotionResolveInput(
                completedSpec,
                0.25f,
                StateTimelineWindowFacts.None(default),
                CharacterRuntimeActionFacts.Default,
                false);
            ActionMotionResolveResult completedWithoutMove = ActionMotionResolver.Resolve(in completedWithoutMoveInput);

            Assert.True(completedWithoutMove.ActionCompleted);
            Assert.False(completedWithoutMove.SetRunLatch);
        }

        [Test]
        public void ActionMotionResolverDoesNotReferenceSceneAnimationOrInputObjects()
        {
            string actionRoot = Path.Combine(Application.dataPath, "Scripts/Character/Action");
            string combined = string.Join("\n", new[]
            {
                Path.Combine(actionRoot, "Model/ActionMotionTypes.cs"),
                Path.Combine(actionRoot, "Solver/ActionMotionResolver.cs")
            }.Select(path => File.ReadAllText(path, System.Text.Encoding.UTF8)));

            Assert.That(combined, Does.Not.Contain("CharacterController"));
            Assert.That(combined, Does.Not.Contain("Animancer"));
            Assert.That(combined, Does.Not.Contain("Animator"));
            Assert.That(combined, Does.Not.Contain("InputAction"));
            Assert.That(combined, Does.Not.Contain("Transform"));
            Assert.That(combined, Does.Not.Contain("UnityEngine.Object"));
            Assert.That(combined, Does.Not.Contain("AnimationClip"));
            Assert.That(combined, Does.Not.Contain("DodgeActionTuning"));
            Assert.That(combined, Does.Not.Contain("ActionStateIds.Dodge"));
        }

        [Test]
        public void CommittedActionOutputWritesCompletedDirectionalRunLatchToLocomotionRuntime()
        {
            string outputPort = File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "Scripts/Character/Movement/Contracts/ILocomotionOutputRuntimePort.cs"),
                System.Text.Encoding.UTF8);
            string characterFrameOutput = File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "Scripts/Character/Action/Runtime/CharacterFrameOutputRuntime.cs"),
                System.Text.Encoding.UTF8);
            string locomotionOutput = File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "Scripts/Character/Movement/Runtime/LocomotionOutputRuntime.cs"),
                System.Text.Encoding.UTF8);

            Assert.That(outputPort, Does.Contain("void SetRunLatchActive(bool active);"));
            Assert.That(characterFrameOutput, Does.Contain("if (actionMotionResult.SetRunLatch)"));
            Assert.That(characterFrameOutput, Does.Contain("locomotionOutputRuntime.SetRunLatchActive(true);"));
            Assert.That(locomotionOutput, Does.Contain("public void SetRunLatchActive(bool active)"));
            Assert.That(locomotionOutput, Does.Contain("dependencies.StateStore.SetRunLatchActive(active);"));
        }

        [Test]
        public void CharacterActionRequestSubmissionArbiterUsesSubmissionProviders()
        {
            string arbiter = File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "Scripts/Character/Action/Solver/CharacterActionRequestSubmissionArbiter.cs"),
                System.Text.Encoding.UTF8);

            Assert.That(arbiter, Does.Contain("CommittedActionRequestSubmissionProviderCollection.Default"));
            Assert.That(arbiter, Does.Contain("ActionInterruptArbiter.Arbitrate"));
            Assert.That(arbiter, Does.Not.Contain("BuildDodgeRequestFact"));
            Assert.That(arbiter, Does.Not.Contain("BuildTurnBackRequestFact"));
            Assert.That(arbiter, Does.Not.Contain("InputRequestKind.Dodge"));
            Assert.That(arbiter, Does.Not.Contain("InputRequestKind.TurnBack"));
        }

        [Test]
        public void RunnerStoresStatePayloadWithoutTurnBackSpecialFields()
        {
            string runner = File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "Scripts/Character/StateMachine/Solver/Runtime/CharacterStateMachineRunner.cs"),
                System.Text.Encoding.UTF8);

            Assert.That(runner, Does.Contain("CharacterStatePayload"));
            Assert.That(runner, Does.Not.Contain("actionWorldDirection"));
            Assert.That(runner, Does.Not.Contain("turnBackWorldDirection"));
            Assert.That(runner, Does.Not.Contain("turnBackEntryBasisForward"));
            Assert.That(runner, Does.Not.Contain("CharacterStateIds.TurnBack"));
        }

        [Test]
        public void CharacterStateMachineSnapshotKeepsFullBodyInterpretationOut()
        {
            string runtimeTypes = File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "Scripts/Character/StateMachine/Model/CharacterStateMachineRuntimeTypes.cs"),
                System.Text.Encoding.UTF8);
            string snapshotBody = ExtractSourceBlock(
                runtimeTypes,
                "public readonly struct CharacterStateMachineSnapshot",
                "public readonly struct CharacterStateDomainView");

            Assert.That(runtimeTypes, Does.Contain("public readonly struct CharacterStateDomainView"));
            Assert.That(snapshotBody, Does.Not.Contain("IsAction"));
            Assert.That(snapshotBody, Does.Not.Contain("IsLocomotion"));
            Assert.That(snapshotBody, Does.Not.Contain("LocomotionPhase"));
            Assert.That(snapshotBody, Does.Not.Contain("ActionState"));
            Assert.That(snapshotBody, Does.Not.Contain("CharacterStateOwner"));
        }

        [Test]
        public void ActionMotionSpecAndResolveResultStayCopyablePureData()
        {
            ActionMotionSpec spec = new ActionMotionSpec(
                new ActionStateId("Action.LightAttack"),
                new CharacterStateId("Action.LightAttack"),
                CharacterStateVariant.None,
                0.2f,
                1.5f,
                false,
                false,
                Vector3.right,
                0.1f,
                9);
            ActionMotionResolveInput input = new ActionMotionResolveInput(
                spec,
                0.1f,
                StateTimelineWindowFacts.None(default),
                CharacterRuntimeActionFacts.Default,
                false);

            ActionMotionResolveResult original = ActionMotionResolver.Resolve(in input);
            ActionMotionResolveResult copied = original;

            Assert.True(copied.HasSpec);
            Assert.AreEqual("Action.LightAttack", copied.Spec.ActionState.Value);
            Assert.AreEqual(original.MovementCommand.PlanarDistance, copied.MovementCommand.PlanarDistance, 0.0001f);
            Assert.False(copied.SetRunLatch);
        }

        [Test]
        public void CharacterRuntimeActionFactsUseResolverResultSource()
        {
            ActionMotionSpec spec = new ActionMotionSpec(
                ActionStateIds.Dodge,
                CharacterStateIds.Dodge,
                CharacterStateVariant.Directional,
                0.35f,
                4f,
                true,
                true,
                Vector3.forward,
                0.1f,
                99);
            ActionMotionResolveResult motion = ActionMotionResolver.Resolve(new ActionMotionResolveInput(
                spec,
                0.1f,
                StateTimelineWindowFacts.None(CharacterStateIds.Dodge),
                CharacterRuntimeActionFacts.Default,
                true));
            CharacterStateMachineSnapshot snapshot = new CharacterStateMachineSnapshot(
                CharacterStateIds.Dodge,
                0.1f,
                CharacterStateVariant.Directional,
                "Action.Dodge",
                new[] { CharacterStateTag.Character, CharacterStateTag.Action, CharacterStateTag.Dodge });
            CharacterStateMachineFrame frame = new CharacterStateMachineFrame(
                snapshot,
                false,
                false,
                false,
                InputRequestKind.Dodge,
                false,
                false,
                spec,
                default,
                false,
                CharacterStatePayload.Empty);

            CharacterRuntimeActionFacts facts = CharacterRuntimeActionFacts.FromStateFrame(
                in frame,
                in motion,
                false,
                99);

            Assert.True(facts.Active);
            Assert.True(facts.HasMovement);
            Assert.AreEqual(motion.ActionCompleted, facts.Completed);
            Assert.AreEqual(motion.MovementCommand.WorldDirection, facts.WorldDirection);
            Assert.AreEqual(motion.MovementCommand.PlanarDistance, facts.PlanarDistance, 0.0001f);
            Assert.AreEqual(motion.MovementCommand.RotateToDirection, facts.RotateToDirection);
            Assert.AreEqual(motion.SourceStep, facts.SourceStep);
        }

        [Test]
        public void FutureActionMotionSpecDoesNotRequireOutputResolverMathBranch()
        {
            ActionMotionSpec spec = new ActionMotionSpec(
                new ActionStateId("Action.LightAttack"),
                new CharacterStateId("Action.LightAttack"),
                CharacterStateVariant.None,
                0.3f,
                2f,
                false,
                false,
                Vector3.forward,
                0.15f,
                11);
            ActionMotionResolveInput input = new ActionMotionResolveInput(
                spec,
                0.1f,
                StateTimelineWindowFacts.None(default),
                CharacterRuntimeActionFacts.Default,
                false);

            ActionMotionResolveResult result = ActionMotionResolver.Resolve(in input);
            string outputResolver = File.ReadAllText(Path.Combine(
                    Application.dataPath,
                    "Scripts/Character/StateMachine/Solver/Output/CharacterStateOutputResolver.cs"),
                System.Text.Encoding.UTF8);

            Assert.True(result.HasActionMovement);
            Assert.That(outputResolver, Does.Not.Contain("LightAttack"));
            Assert.That(outputResolver, Does.Not.Contain("frameDistance"));
        }

        [Test]
        public void TimelineFactsAuthorityHasStaticBoundaries()
        {
            string stateMachineRoot = Path.Combine(Application.dataPath, "Scripts/Character/StateMachine");
            string actionRoot = Path.Combine(Application.dataPath, "Scripts/Character/Action");
            string runner = File.ReadAllText(Path.Combine(stateMachineRoot, "Solver/Runtime/CharacterStateMachineRunner.cs"));
            string sampler = File.ReadAllText(Path.Combine(stateMachineRoot, "Solver/Timeline/CharacterStateTimelineFactSampler.cs"));
            string resolver = File.ReadAllText(Path.Combine(actionRoot, "Solver/CommittedActionRequestSubmissionResolver.cs"));
            string output = File.ReadAllText(Path.Combine(stateMachineRoot, "Solver/Output/CharacterStateOutputResolver.cs"));
            string diagnostics = File.ReadAllText(Path.Combine(actionRoot, "Diagnostics/CharacterFrameDiagnosticAdapter.cs"));

            Assert.That(resolver, Does.Not.Contain("SampleCurrent"));
            Assert.That(resolver, Does.Not.Contain("StateMachineDefinition"));
            Assert.That(output, Does.Not.Contain("SampleCurrent"));
            Assert.That(runner, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(sampler, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(diagnostics, Does.Contain("state-timeline-window-facts"));
            Assert.That(diagnostics, Does.Contain("source=current"));
            Assert.That(diagnostics, Does.Contain("source=projected"));
            Assert.That(diagnostics, Does.Contain("source=target"));
        }

        [Test]
        public void RunnerTimelineTraceKeepsCurrentProjectedAndTargetFactsDistinct()
        {
            CharacterStateMachineRunner runner = CreateRunner();
            runner.Tick(Context(move: true, runHeld: true));
            runner.Tick(Context(move: true, canExit: true, runHeld: true));
            float stateTimeBeforeTick = runner.StateTime;
            StateTimelineWindowFacts currentFacts = new StateTimelineWindowFacts(
                runner.Snapshot.ActiveState,
                0f,
                false,
                stateTimeBeforeTick,
                false,
                false,
                false,
                false,
                0,
                0,
                0,
                false,
                string.Empty);

            CharacterStateMachineFrame frame = runner.Tick(Context(
                move: false,
                deltaTime: 0.1f,
                timelineFacts: currentFacts));

            Assert.AreEqual(StateTimelineFactsSource.Current, frame.CurrentTimelineFactsTrace.Source);
            Assert.AreEqual(StateTimelineFactsSource.Projected, frame.ProjectedTimelineFactsTrace.Source);
            Assert.AreEqual(StateTimelineFactsSource.Target, frame.TargetTimelineFactsTrace.Source);
            Assert.AreEqual(currentFacts.ElapsedSeconds, frame.CurrentTimelineFactsTrace.Facts.ElapsedSeconds, 0.0001f);
            Assert.AreEqual(stateTimeBeforeTick + 0.1f, frame.ProjectedTimelineFactsTrace.Facts.ElapsedSeconds, 0.0001f);
            Assert.AreEqual(CharacterStateIds.MoveStop, frame.TargetTimelineFactsTrace.Facts.StateId);
            Assert.AreNotEqual(frame.CurrentTimelineFactsTrace.FactsId, frame.ProjectedTimelineFactsTrace.FactsId);
        }

        [Test]
        public void StateMachineAnimationBindingStoresOnlyStableKeys()
        {
            string model = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/StateMachine/Model/CharacterStateMachineTypes.cs"));
            string asset = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Configs/3C/StateMachine/Locomotion/Corin/CorinLocomotionStateGraph.asset"));

            Assert.That(model, Does.Contain("animationKey"));
            Assert.That(model, Does.Contain("timelineBindingKey"));
            Assert.That(model, Does.Not.Contain("AnimationClip"));
            Assert.That(model, Does.Not.Contain("transitionAsset"));
            Assert.That(model, Does.Not.Contain("TransitionAsset"));
            Assert.That(model, Does.Not.Contain("transitionLibraryKey"));
            Assert.That(model, Does.Not.Contain("TransitionLibraryKey"));
            Assert.That(model, Does.Not.Contain("fadeDuration"));
            Assert.That(model, Does.Not.Contain("startTime"));
            Assert.That(asset, Does.Not.Contain("timelineBindingKey: Action.Dodge.Directional"));
            Assert.That(asset, Does.Not.Contain("timelineBindingKey: Action.Dodge.Backstep"));
            Assert.That(asset, Does.Not.Contain("clip: {fileID:"));
            Assert.That(asset, Does.Not.Contain("transitionAsset:"));
            Assert.That(asset, Does.Not.Contain("transitionLibraryKey:"));
            Assert.That(asset, Does.Not.Contain("fadeDuration:"));
            Assert.That(asset, Does.Not.Contain("startTime:"));
        }

        [Test]
        public void ActionRequestSubmissionDoesNotReferencePresentationOrMotionRuntimeObjects()
        {
            string actionRoot = Path.Combine(Application.dataPath, "Scripts/Character/Action");
            string combined = string.Join("\n", new[]
            {
                Path.Combine(actionRoot, "Solver/CommittedActionInterruptRequestFactory.cs"),
                Path.Combine(actionRoot, "Solver/CommittedActionInputRequestBuilder.cs"),
                Path.Combine(actionRoot, "Solver/DodgeActionPlanner.cs"),
                Path.Combine(actionRoot, "Solver/DodgeActionDirectionResolver.cs")
            }.Select(File.ReadAllText));

            Assert.That(combined, Does.Not.Contain("Animancer"));
            Assert.That(combined, Does.Not.Contain("Animator"));
            Assert.That(combined, Does.Not.Contain("CharacterController"));
            Assert.That(combined, Does.Not.Contain("Cinemachine"));
            Assert.That(combined, Does.Not.Contain("UnityEngine.InputSystem"));
            Assert.That(combined, Does.Not.Contain("BBBNexus"));
            Assert.That(combined, Does.Not.Contain("ICameraMovementBasisProvider"));
            Assert.That(combined, Does.Not.Contain("CameraRelativeMovementResolver"));
            Assert.That(combined, Does.Not.Contain("MovementInputIntent.FromRaw"));
        }

        [Test]
        public void LocomotionDecisionStagesDoNotCallMotionOrAnimationAdapters()
        {
            string movementRoot = Path.Combine(Application.dataPath, "Scripts/Character/Movement");
            string spatialProvider = File.ReadAllText(Path.Combine(
                movementRoot,
                "Runtime/LocomotionSpatialFactsProvider.cs"));
            string pipeline = File.ReadAllText(Path.Combine(
                movementRoot,
                "Solver/LocomotionFrameBuilder.cs"));

            AssertMethodBodyDoesNotContain(pipeline, "ResolvePrepareFacts", "ExecuteBasicMovement");
            AssertMethodBodyDoesNotContain(pipeline, "ResolvePrepareFacts", "Present(");
            AssertMethodBodyDoesNotContain(pipeline, "TryPrepareDecisionFrame", "ExecuteBasicMovement");
            AssertMethodBodyDoesNotContain(pipeline, "TryPrepareDecisionFrame", "Present(");
            AssertMethodBodyDoesNotContain(spatialProvider, "Resolve", "ExecuteBasicMovement");
            AssertMethodBodyDoesNotContain(spatialProvider, "Resolve", "Present(");
            AssertMethodBodyDoesNotContain(pipeline, "TryEvaluatePreparedGameplayDecision", "ExecuteBasicMovement");
            AssertMethodBodyDoesNotContain(pipeline, "TryEvaluatePreparedGameplayDecision", "Present(");
            AssertMethodBodyDoesNotContain(pipeline, "TryBuildMotionFromStateDecision", "ExecuteBasicMovement");
            AssertMethodBodyDoesNotContain(pipeline, "TryBuildMotionFromStateDecision", "Present(");
        }

        [Test]
        public void LocomotionAdapterModulesDoNotReferenceRuntimeDrivers()
        {
            string movementRoot = Path.Combine(Application.dataPath, "Scripts/Character/Movement");
            string combined = string.Join("\n", new[]
            {
                Path.Combine(movementRoot, "Solver/Facts/LocomotionFactsBuilder.cs"),
                Path.Combine(movementRoot, "Solver/TurnBack/TurnBackIntentResolver.cs"),
                Path.Combine(movementRoot, "Solver/TurnBack/TurnBackMotionResolver.cs"),
                Path.Combine(movementRoot, "Solver/Motion/LocomotionStateMotionBuilder.cs"),
                Path.Combine(movementRoot, "Solver/Snapshot/LocomotionSnapshotAdapter.cs"),
                Path.Combine(movementRoot, "Diagnostics/LocomotionDiagnostics.cs")
            }.Select(File.ReadAllText));

            Assert.That(combined, Does.Not.Contain("MonoBehaviour"));
            Assert.That(combined, Does.Not.Contain("CharacterController"));
            Assert.That(combined, Does.Not.Contain("Animancer"));
            Assert.That(combined, Does.Not.Contain("UnityEngine.InputSystem"));
            Assert.That(combined, Does.Not.Contain("InputAction"));
            Assert.That(combined, Does.Not.Contain("ISimulationTick"));
            Assert.That(combined, Does.Not.Contain("RegisterTick"));
            Assert.That(combined, Does.Not.Contain("new CharacterStateMachineRunner"));
            Assert.That(combined, Does.Not.Contain(".ExecuteBasicMovement("));
            Assert.That(combined, Does.Not.Contain(".Present("));
        }

        [Test]
        public void LocomotionFrameBuilderOwnsMainlineWithoutRuntimeAdapters()
        {
            string builder = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Movement/Solver/LocomotionFrameBuilder.cs"));

            Assert.That(builder, Does.Not.Contain("MonoBehaviour"));
            Assert.That(builder, Does.Not.Contain("Transform"));
            Assert.That(builder, Does.Not.Contain("CharacterController"));
            Assert.That(builder, Does.Not.Contain("UnityEngine.InputSystem"));
            Assert.That(builder, Does.Not.Contain("InputAction"));
            Assert.That(builder, Does.Not.Contain("Animancer"));
            Assert.That(builder, Does.Not.Contain("new CharacterStateMachineRunner"));
            Assert.That(builder, Does.Not.Contain("RegisterTick"));
            Assert.That(builder, Does.Not.Contain(".Move("));
            Assert.That(builder, Does.Not.Contain(".Present("));
            Assert.That(builder, Does.Not.Contain("RestorePlaybackProgress"));
            Assert.That(builder, Does.Not.Contain("ResetMotionPlaybackWindow"));
        }

        [Test]
        public void LocomotionFrameRuntimeModulesOwnFramePreparation()
        {
            string movementRoot = Path.Combine(Application.dataPath, "Scripts/Character/Movement");
            string module = File.ReadAllText(Path.Combine(
                movementRoot,
                "Runtime/LocomotionRuntimeModule.cs"));
            string runtime = File.ReadAllText(Path.Combine(
                movementRoot,
                "Runtime/LocomotionFrameRuntime.cs"));
            string stateStore = File.ReadAllText(Path.Combine(
                movementRoot,
                "Runtime/LocomotionRuntimeStateStore.cs"));
            string prepareProvider = File.ReadAllText(Path.Combine(
                movementRoot,
                "Runtime/LocomotionPrepareFactsProvider.cs"));
            string spatialProvider = File.ReadAllText(Path.Combine(
                movementRoot,
                "Runtime/LocomotionSpatialFactsProvider.cs"));
            string motionProvider = File.ReadAllText(Path.Combine(
                movementRoot,
                "Runtime/LocomotionMotionFactsProvider.cs"));
            string combined = string.Join("\n", runtime, stateStore, prepareProvider, spatialProvider, motionProvider);

            Assert.That(module, Does.Contain("LocomotionFrameRuntimeAdapter"));
            Assert.That(module, Does.Contain("LocomotionRuntimeStateStore"));
            Assert.That(stateStore, Does.Contain("CaptureRollbackState"));
            Assert.That(stateStore, Does.Contain("RestoreRollbackState"));
            Assert.That(prepareProvider, Does.Contain("TryBuildPreparationContext"));
            Assert.That(spatialProvider, Does.Contain("LocomotionFactsBuilder.BuildSpatialFacts"));
            Assert.That(motionProvider, Does.Contain("TurnBackMotionResolver.Resolve"));
            Assert.That(combined, Does.Not.Contain("ExecuteBasicMovement"));
            Assert.That(combined, Does.Not.Contain(".Present("));
            Assert.That(combined, Does.Not.Contain("new CharacterStateMachineRunner"));
            Assert.That(runtime, Does.Not.Contain("CharacterFramePipeline"));
        }

        [Test]
        public void LocomotionDiagnosticsKeepsKnownEventIds()
        {
            string movementRoot = Path.Combine(Application.dataPath, "Scripts/Character/Movement");
            string combined = string.Join("\n", new[]
            {
                Path.Combine(movementRoot, "Diagnostics/LocomotionDiagnostics.cs"),
                Path.Combine(movementRoot, "Runtime/LocomotionPrepareFactsProvider.cs"),
                Path.Combine(Application.dataPath, "Scripts/Character/Pipeline/Runtime/CharacterFrameRuntimeController.cs")
            }.Select(File.ReadAllText));

            Assert.That(combined, Does.Contain("locomotion-state-machine-output-probe"));
            Assert.That(combined, Does.Contain("locomotion-decision-pipeline"));
            Assert.That(combined, Does.Contain("locomotion-turnback-intent"));
            Assert.That(combined, Does.Contain("turnback-root-motion-consumed"));
            Assert.That(combined, Does.Contain("locomotion-turnback-state-policy"));
            Assert.That(combined, Does.Contain("turnback-entry-basis-missing"));
            Assert.That(combined, Does.Contain("turnback-frame-summary"));
            Assert.That(combined, Does.Contain("movement-config-missing"));
            Assert.That(combined, Does.Contain("input-source-missing"));
            Assert.That(combined, Does.Contain("motion-executor-missing"));
            Assert.That(combined, Does.Contain("locomotion-tick-snapshot"));
            Assert.That(combined, Does.Contain("locomotion-run-latch-reset-after-idle"));
            Assert.That(combined, Does.Contain("movement-camera-input"));
        }

        [Test]
        public void LocomotionDecisionPreparationOwnsAnimationProgressAdvance()
        {
            string prepareProvider = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Movement/Runtime/LocomotionPrepareFactsProvider.cs"));
            string frameRuntime = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Movement/Runtime/LocomotionFrameRuntime.cs"));

            Assert.That(prepareProvider, Does.Contain("AdvanceAnimationPlaybackProgress"));
            Assert.That(frameRuntime, Does.Not.Contain("AdvanceAnimationPlaybackProgress"));
        }

        [Test]
        public void RuntimeStateMachineRunnerHasSingleCharacterStateOwner()
        {
            string runtimeController = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Pipeline/Runtime/CharacterFrameRuntimeController.cs"));
            string committedActionModule = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Action/Runtime/CommittedActionRuntimeModule.cs"));
            string stateMachineRuntime = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Action/Runtime/CharacterStateMachineRuntime.cs"));

            Assert.That(runtimeController, Does.Not.Contain("new CharacterStateMachineRunner"));
            Assert.That(committedActionModule, Does.Contain("CharacterStateMachineRuntime"));
            Assert.AreEqual(1, CountOccurrences(stateMachineRuntime, "new CharacterStateMachineRunner"));
        }

        [Test]
        public void CharacterFramePipelineOwnsOutputOrder()
        {
            string pipelineRoot = Path.Combine(Application.dataPath, "Scripts/Character/Pipeline");
            string host = File.ReadAllText(Path.Combine(
                pipelineRoot,
                "Runtime/CharacterFramePipelineHost.cs"));
            string pipeline = File.ReadAllText(Path.Combine(
                pipelineRoot,
                "Runtime/CharacterFramePipeline.cs"));
            string actionSubmitter = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Action/Runtime/CommittedActionFrameSubmitter.cs"));

            Assert.That(host, Does.Contain("new CharacterFramePipeline("));
            Assert.That(pipeline, Does.Contain("SimulationTickPhaseOrder.Phases"));
            Assert.That(pipeline, Does.Contain("RunExecuteMotion"));
            Assert.That(pipeline, Does.Contain("RunPresentationBridge"));
            Assert.That(actionSubmitter, Does.Not.Contain("RunExecuteMotion"));
            Assert.That(actionSubmitter, Does.Not.Contain("RunPresentationBridge"));
        }

        [Test]
        public void CharacterRuntimePortsKeepFramePipelineOffConcreteControllers()
        {
            string actionRoot = Path.Combine(Application.dataPath, "Scripts/Character/Action");
            string pipelineRoot = Path.Combine(Application.dataPath, "Scripts/Character/Pipeline");
            string movementRoot = Path.Combine(Application.dataPath, "Scripts/Character/Movement");
            string pipeline = File.ReadAllText(Path.Combine(pipelineRoot, "Runtime/CharacterFramePipeline.cs"));
            string host = File.ReadAllText(Path.Combine(pipelineRoot, "Runtime/CharacterFramePipelineHost.cs"));
            string runtimeController = File.ReadAllText(Path.Combine(pipelineRoot, "Runtime/CharacterFrameRuntimeController.cs"));
            string runtimeCore = File.ReadAllText(Path.Combine(pipelineRoot, "Runtime/CharacterRuntimeCore.cs"));
            string runtimeTickAdapter = File.ReadAllText(Path.Combine(pipelineRoot, "Runtime/CharacterFrameRuntimeTickAdapter.cs"));
            string actionSubmitter = File.ReadAllText(Path.Combine(actionRoot, "Runtime/CommittedActionFrameSubmitter.cs"));
            string locomotionSubmitter = File.ReadAllText(Path.Combine(movementRoot, "Runtime/LocomotionFrameSubmitter.cs"));
            string characterPort = File.ReadAllText(Path.Combine(pipelineRoot, "Contracts/ICharacterFrameRuntimePort.cs"));
            string requestSubmitter = File.ReadAllText(Path.Combine(pipelineRoot, "Contracts/ICharacterFrameRequestSubmitter.cs"));
            string outputSubmitter = File.ReadAllText(Path.Combine(pipelineRoot, "Contracts/ICharacterFrameOutputSubmitter.cs"));
            string legacyActionRuntime = "FullBody" + "ActionRuntime";
            string legacySubmissionBuilder = "FullBody" + "SubmissionBuilder";
            string legacyIntegratedAdapter = "FullBody" + "IntegratedFrameAdapter";
            string legacySubmissionPort = "I" + "FullBody" + "SubmissionRuntimePort";
            string legacyOutputPort = "I" + "FullBody" + "OutputRuntimePort";
            string fullBodyPortsPath = Path.Combine(actionRoot, "Contracts/" + "FullBody" + "RuntimePorts.cs");
            string fullBodyAdapterPath = Path.Combine(actionRoot, "Runtime/" + "FullBody" + "RuntimePortAdapter.cs");
            string fullBodyBuilderPath = Path.Combine(actionRoot, "Runtime/" + legacySubmissionBuilder + ".cs");
            string adapter = File.ReadAllText(Path.Combine(pipelineRoot, "Runtime/CharacterFrameRuntimePortAdapter.cs"));
            string locomotionFramePort = File.ReadAllText(Path.Combine(movementRoot, "Contracts/ILocomotionFrameRuntimePort.cs"));
            string locomotionOutputPort = File.ReadAllText(Path.Combine(movementRoot, "Contracts/ILocomotionOutputRuntimePort.cs"));
            string allPorts = characterPort + "\n" + requestSubmitter + "\n" + outputSubmitter + "\n" + locomotionFramePort + "\n" + locomotionOutputPort;

            Assert.That(pipeline, Does.Not.Contain(legacyActionRuntime));
            Assert.That(pipeline, Does.Not.Contain(legacySubmissionBuilder));
            Assert.That(pipeline, Does.Contain("ICharacterFrameRequestSubmitter"));
            Assert.That(pipeline, Does.Contain("ICharacterFrameOutputSubmitter"));
            Assert.That(host, Does.Contain("new CharacterFramePipeline("));
            Assert.That(host, Does.Not.Contain(legacySubmissionBuilder));
            Assert.That(runtimeController, Does.Contain("CharacterRuntimeCore"));
            Assert.That(runtimeController, Does.Not.Contain("CharacterFrameRuntimeHost"));
            Assert.That(runtimeController, Does.Not.Contain("new CharacterFrameRuntimeHost("));
            Assert.That(runtimeCore, Does.Contain("CharacterFrameRuntimeHost"));
            Assert.That(runtimeCore, Does.Contain("new CharacterFrameRuntimeHost("));
            Assert.That(runtimeTickAdapter, Does.Contain("CharacterFrameRuntimeController"));
            Assert.That(actionSubmitter, Does.Not.Contain(legacyActionRuntime));
            Assert.That(actionSubmitter, Does.Not.Contain("PlayerLocomotionController"));
            Assert.That(actionSubmitter, Does.Not.Contain(legacyIntegratedAdapter));
            Assert.That(locomotionSubmitter, Does.Not.Contain(legacySubmissionBuilder));
            Assert.False(File.Exists(Path.Combine(pipelineRoot, "Runtime/CharacterFrameSubmitterChain.cs")));
            Assert.That(actionSubmitter, Does.Contain("ICharacterFrameRequestSubmitter"));
            Assert.That(actionSubmitter, Does.Contain("ICharacterFrameOutputSubmitter"));
            Assert.That(actionSubmitter, Does.Not.Contain("TryEvaluatePreparedGameplayDecision"));
            Assert.That(actionSubmitter, Does.Not.Contain("TryBuildMotionFromStateDecision"));
            Assert.That(locomotionSubmitter, Does.Contain("ICharacterFrameOutputSubmitter"));
            Assert.That(locomotionSubmitter, Does.Contain("TryEvaluatePreparedGameplayDecision"));
            Assert.That(locomotionSubmitter, Does.Contain("TryBuildMotionFromStateDecision"));
            Assert.That(pipeline, Does.Contain("ICharacterFrameRuntimePort"));
            Assert.That(actionSubmitter, Does.Contain("ICharacterFrameSubmissionRuntimePort"));
            Assert.That(actionSubmitter, Does.Contain("ILocomotionFrameRuntimePort"));
            Assert.That(adapter, Does.Contain("CharacterRuntimeCore"));
            Assert.That(adapter, Does.Not.Contain(legacyActionRuntime));
            Assert.That(adapter, Does.Not.Contain("PlayerLocomotionController"));
            Assert.That(adapter, Does.Not.Contain("CharacterFrameRuntimeController"));
            Assert.That(adapter, Does.Contain("ICharacterFrameRuntimePort"));
            Assert.That(characterPort, Does.Contain("ICharacterFrameRuntimePort"));
            Assert.That(characterPort, Does.Not.Contain(legacySubmissionPort));
            Assert.That(characterPort, Does.Not.Contain(legacyOutputPort));
            Assert.False(File.Exists(fullBodyPortsPath));
            Assert.False(File.Exists(fullBodyAdapterPath));
            Assert.False(File.Exists(fullBodyBuilderPath));
            Assert.That(allPorts, Does.Not.Contain("MonoBehaviour"));
            Assert.That(allPorts, Does.Not.Contain("Transform"));
            Assert.That(allPorts, Does.Not.Contain("CharacterController"));
            Assert.That(allPorts, Does.Not.Contain("Animancer"));
            Assert.That(allPorts, Does.Not.Contain("InputAction"));
            Assert.That(allPorts, Does.Not.Contain(legacyActionRuntime));
            Assert.That(allPorts, Does.Not.Contain("PlayerLocomotionController"));
            Assert.That(locomotionFramePort, Does.Not.Contain("ExecuteLocomotionMotion"));
            Assert.That(locomotionFramePort, Does.Not.Contain("PresentLocomotionAnimation"));
            Assert.That(locomotionOutputPort, Does.Not.Contain("TryPrepareDecisionFrame"));
            Assert.That(locomotionOutputPort, Does.Not.Contain("TryEvaluatePreparedGameplayDecision"));
            Assert.That(locomotionOutputPort, Does.Not.Contain("TryBuildMotionFromStateDecision"));
        }

        [Test]
        public void CharacterFrameOutputRuntimeModulesOwnOutputSideEffects()
        {
            string actionRoot = Path.Combine(Application.dataPath, "Scripts/Character/Action");
            string pipelineRoot = Path.Combine(Application.dataPath, "Scripts/Character/Pipeline");
            string adapter = File.ReadAllText(Path.Combine(pipelineRoot, "Runtime/CharacterFrameRuntimePortAdapter.cs"));
            string module = File.ReadAllText(Path.Combine(actionRoot, "Runtime/CommittedActionRuntimeModule.cs"));
            string outputRuntime = File.ReadAllText(Path.Combine(actionRoot, "Runtime/CharacterFrameOutputRuntime.cs"));
            string outputHost = File.ReadAllText(Path.Combine(actionRoot, "Runtime/CharacterFrameOutputRuntimeHost.cs"));

            Assert.That(adapter, Does.Contain("OutputRuntime"));
            Assert.That(adapter, Does.Not.Contain("SetLastFrameOutputsForPipeline"));
            Assert.That(adapter, Does.Not.Contain("ConsumeStateFrameInputRequestForPipeline"));
            Assert.That(adapter, Does.Not.Contain("ExecuteStateFrameMotionForPipeline"));
            Assert.That(adapter, Does.Not.Contain("PresentStateFrameAnimationForPipeline"));
            Assert.That(adapter, Does.Not.Contain("WriteStateFrameActionFactsForPipeline"));
            Assert.That(adapter, Does.Not.Contain("UpdateStateSnapshotForPipeline"));
            Assert.That(adapter, Does.Not.Contain("WriteAnimationRuntimeFactsForPipeline"));
            Assert.That(adapter, Does.Not.Contain("CompleteLocomotionTickForPipeline"));
            Assert.That(adapter, Does.Not.Contain("LogDiagnosticTickSnapshotsForPipeline"));

            Assert.That(module, Does.Contain("CharacterFrameOutputRuntimeHost"));
            Assert.That(outputHost, Does.Contain("internal sealed class CharacterFrameOutputRuntimeHost"));

            Assert.That(outputRuntime, Does.Contain("internal sealed class CharacterFrameOutputRuntime"));
            Assert.That(outputRuntime, Does.Contain("internal sealed class CharacterFrameOutputCacheWriter"));
            Assert.That(outputRuntime, Does.Contain("internal sealed class CharacterFrameInputRequestConsumer"));
            Assert.That(outputRuntime, Does.Contain("internal sealed class CharacterFrameMotionOutputApplier"));
            Assert.That(outputRuntime, Does.Contain("internal sealed class CharacterAnimationOutputPresenter"));
            Assert.That(outputRuntime, Does.Contain("internal sealed class CharacterFrameRuntimeFactsWriter"));
            Assert.That(outputRuntime, Does.Contain("internal sealed class CharacterFrameSnapshotWriter"));
            Assert.That(outputRuntime, Does.Contain("internal sealed class CharacterFrameDiagnosticSubmitter"));
            Assert.That(outputRuntime, Does.Contain("ExecuteActionMovement"));
            Assert.That(outputRuntime, Does.Contain("PresentLocomotionAnimation"));
            Assert.That(outputRuntime, Does.Contain("WriteActionFacts"));
            Assert.That(outputRuntime, Does.Contain("WriteAnimationFacts"));
            Assert.That(outputRuntime, Does.Not.Contain("FullBody" + "ActionRuntime"));
            Assert.That(outputRuntime, Does.Not.Contain("CharacterController.Move"));
            Assert.That(outputRuntime, Does.Not.Contain("Animancer"));
            Assert.That(outputRuntime, Does.Not.Contain("CharacterStateMachineDefinition"));
            Assert.That(outputRuntime, Does.Not.Contain("TransitionEvaluator"));
            Assert.That(outputRuntime, Does.Not.Contain("CommittedActionRequestSubmissionResolver"));
            Assert.That(outputRuntime, Does.Not.Contain("CharacterActionRequestSubmissionArbiter"));
        }

        [Test]
        public void LocomotionOutputRuntimeModulesOwnOutputSideEffects()
        {
            string movementRoot = Path.Combine(Application.dataPath, "Scripts/Character/Movement");
            string actionRoot = Path.Combine(Application.dataPath, "Scripts/Character/Action");
            string module = File.ReadAllText(Path.Combine(
                movementRoot,
                "Runtime/LocomotionRuntimeModule.cs"));
            string outputRuntime = File.ReadAllText(Path.Combine(
                movementRoot,
                "Runtime/LocomotionOutputRuntime.cs"));
            string characterFrameOutput = File.ReadAllText(Path.Combine(
                actionRoot,
                "Runtime/CharacterFrameOutputRuntime.cs"));

            Assert.That(module, Does.Contain("LocomotionOutputRuntimeHost"));

            Assert.That(outputRuntime, Does.Contain("internal sealed class LocomotionOutputRuntime"));
            Assert.That(outputRuntime, Does.Contain("internal sealed class LocomotionOutputRuntimeAdapter"));
            Assert.That(outputRuntime, Does.Contain("internal sealed class LocomotionMotionOutputApplier"));
            Assert.That(outputRuntime, Does.Contain("internal sealed class LocomotionAnimationOutputPresenter"));
            Assert.That(outputRuntime, Does.Contain("internal sealed class LocomotionRuntimeBlackboardWriter"));
            Assert.That(outputRuntime, Does.Contain("internal sealed class LocomotionOutputCompletion"));
            Assert.That(outputRuntime, Does.Contain("ExecuteBasicMovement"));
            Assert.That(outputRuntime, Does.Contain("PresentAnimation"));
            Assert.That(outputRuntime, Does.Contain("WriteActionFactsToBlackboard"));
            Assert.That(outputRuntime, Does.Contain("WriteAnimationFactsToBlackboard"));
            Assert.That(outputRuntime, Does.Not.Contain("TryPrepareDecisionFrame"));
            Assert.That(outputRuntime, Does.Not.Contain("TryEvaluatePreparedGameplayDecision"));
            Assert.That(outputRuntime, Does.Not.Contain("TryBuildMotionFromStateDecision"));
            Assert.That(outputRuntime, Does.Not.Contain("CharacterController.Move"));
            Assert.That(outputRuntime, Does.Not.Contain("Transform.position"));
            Assert.That(outputRuntime, Does.Not.Contain("InputAction"));
            Assert.That(outputRuntime, Does.Not.Contain("new CharacterStateMachineRunner"));
            Assert.That(outputRuntime, Does.Not.Contain("CharacterFramePipeline"));
            Assert.That(outputRuntime, Does.Not.Contain("PlayerLocomotionController"));
            Assert.That(characterFrameOutput, Does.Contain("ILocomotionOutputRuntimePort"));
            Assert.That(characterFrameOutput, Does.Not.Contain("PlayerLocomotionController"));
        }

        [Test]
        public void CharacterRuntimePortRefactorDoesNotMoveSideEffectsIntoModelOrBuilderTypes()
        {
            string frameTypes = ReadCharacterFrameModelSources();
            string stateMachineTypes = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/StateMachine/Model/CharacterStateMachineTypes.cs"));
            string locomotionBuilder = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Movement/Solver/LocomotionFrameBuilder.cs"));

            Assert.That(frameTypes, Does.Not.Contain("IActionMovementExecutor"));
            Assert.That(frameTypes, Does.Not.Contain("IActionAnimationPresenter"));
            Assert.That(frameTypes, Does.Not.Contain("MonoBehaviour"));
            Assert.That(frameTypes, Does.Not.Contain("Transform"));
            Assert.That(frameTypes, Does.Not.Contain("CharacterController"));
            Assert.That(frameTypes, Does.Not.Contain("InputAction"));
            Assert.That(stateMachineTypes, Does.Not.Contain("FullBodyRuntimePort"));
            Assert.That(stateMachineTypes, Does.Not.Contain("ICharacterFrameRuntimePort"));
            Assert.That(stateMachineTypes, Does.Not.Contain("ILocomotionFrameRuntimePort"));
            Assert.That(locomotionBuilder, Does.Not.Contain("ExecuteLocomotionMotion"));
            Assert.That(locomotionBuilder, Does.Not.Contain("PresentLocomotionAnimation"));
            Assert.That(locomotionBuilder, Does.Not.Contain(".Move("));
            Assert.That(locomotionBuilder, Does.Not.Contain(".Present("));
        }

        [Test]
        public void CharacterFramePipelineDelegatesActionRequestAndDiagnostics()
        {
            string pipeline = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Pipeline/Runtime/CharacterFramePipeline.cs"));
            string actionSubmitter = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Action/Runtime/CommittedActionFrameSubmitter.cs"));
            string legacySubmissionBuilder = "FullBody" + "SubmissionBuilder";
            string legacyIntegratedAdapter = "FullBody" + "IntegratedFrameAdapter";

            Assert.That(pipeline, Does.Contain("ICharacterFrameRequestSubmitter"));
            Assert.That(pipeline, Does.Contain("ICharacterFrameOutputSubmitter"));
            Assert.That(pipeline, Does.Not.Contain(legacySubmissionBuilder));
            Assert.That(actionSubmitter, Does.Contain("CommittedActionRequestSubmissionResolver.Resolve"));
            Assert.That(actionSubmitter, Does.Not.Contain(legacyIntegratedAdapter));
            Assert.That(pipeline, Does.Contain("CharacterFrameDiagnostics.LogPipelineSnapshot"));
            Assert.That(pipeline, Does.Not.Contain("CharacterActionRequestSubmissionInput submissionInput"));
            Assert.That(actionSubmitter, Does.Not.Contain("CharacterActionRequestSubmissionInput submissionInput"));
            Assert.That(pipeline, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
        }

        [Test]
        public void CharacterFrameSubmissionDoesNotMixRequestArbitration()
        {
            string types = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Pipeline/Model/CharacterFrameSubmission.cs"));
            string submission = ExtractTypeBlock(
                types,
                "public readonly struct CharacterFrameSubmission");

            Assert.That(submission, Does.Not.Contain("ActionInterruptRequest"));
            Assert.That(submission, Does.Not.Contain("ActionInterruptContext"));
            Assert.That(submission, Does.Not.Contain("CharacterFrameRequestSubmission"));
            Assert.That(submission, Does.Not.Contain("MonoBehaviour"));
            Assert.That(submission, Does.Not.Contain("Transform"));
            Assert.That(submission, Does.Not.Contain("CharacterController"));
            Assert.That(submission, Does.Not.Contain("InputAction"));
        }

        [Test]
        public void CharacterFrameSubmissionCarriesPureOutputSubmissions()
        {
            string types = ReadCharacterFrameModelSources();
            string submissionTypes = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Pipeline/Model/CharacterFrameSubmission.cs"));
            string submission = ExtractTypeBlock(
                submissionTypes,
                "public readonly struct CharacterFrameSubmission");

            Assert.That(types, Does.Contain("public readonly struct CharacterFrameMovementSubmission"));
            Assert.That(types, Does.Contain("public readonly struct CharacterFrameAnimationSubmission"));
            Assert.That(types, Does.Contain("public readonly struct CharacterFrameInputConsumeSubmission"));
            Assert.That(types, Does.Contain("public readonly struct CharacterFrameRuntimeFactsSubmission"));
            Assert.That(types, Does.Contain("public readonly struct CharacterFrameDiagnosticsSubmission"));
            Assert.That(types, Does.Contain("public readonly struct CharacterFrameSnapshotEventsSubmission"));
            Assert.That(submission, Does.Contain("public CharacterFrameMovementSubmission Movement"));
            Assert.That(submission, Does.Contain("public CharacterFrameAnimationSubmission Animation"));
            Assert.That(submission, Does.Contain("public CharacterFrameInputConsumeSubmission InputConsume"));
            Assert.That(submission, Does.Contain("public CharacterFrameRuntimeFactsSubmission RuntimeFacts"));
            Assert.That(submission, Does.Contain("public CharacterFrameDiagnosticsSubmission Diagnostics"));
            Assert.That(submission, Does.Contain("public CharacterFrameSnapshotEventsSubmission SnapshotEvents"));
            Assert.That(types, Does.Not.Contain("IActionMovementExecutor"));
            Assert.That(types, Does.Not.Contain("IActionAnimationPresenter"));
            Assert.That(types, Does.Not.Contain("MonoBehaviour"));
            Assert.That(types, Does.Not.Contain("Transform"));
            Assert.That(types, Does.Not.Contain("CharacterController"));
            Assert.That(types, Does.Not.Contain("InputAction"));
        }

        [Test]
        public void CharacterFrameDataContractsAreSplitIntoFocusedModelFiles()
        {
            string modelRoot = Path.Combine(Application.dataPath, "Scripts/Character/Pipeline/Model");
            string actionRoot = Path.Combine(Application.dataPath, "Scripts/Character/Action");

            Assert.True(File.Exists(Path.Combine(modelRoot, "CharacterFramePipelineStep.cs")));
            Assert.True(File.Exists(Path.Combine(modelRoot, "CharacterFrameInput.cs")));
            Assert.True(File.Exists(Path.Combine(modelRoot, "CharacterFrameContext.cs")));
            Assert.True(File.Exists(Path.Combine(modelRoot, "CharacterFrameSubmission.cs")));
            Assert.True(File.Exists(Path.Combine(modelRoot, "CharacterFrameOutput.cs")));
            Assert.True(File.Exists(Path.Combine(modelRoot, "CharacterFrameResult.cs")));
            Assert.True(File.Exists(Path.Combine(modelRoot, "CharacterFrameDiagnosticsSummary.cs")));
            Assert.False(File.Exists(Path.Combine(modelRoot, "CharacterFramePipelineTypes.cs")));
            Assert.False(File.Exists(Path.Combine(actionRoot, "Model/CharacterFramePipelineTypes.cs")));
        }

        [Test]
        public void RequestProvidersDoNotExecuteOutputSideEffects()
        {
            string providers = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Action/Solver/CommittedActionRequestSubmissionProviders.cs"));
            string arbiter = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Action/Solver/CharacterActionRequestSubmissionArbiter.cs"));
            string combined = providers + "\n" + arbiter;

            Assert.That(combined, Does.Contain("DodgeActionRequestSubmissionProvider"));
            Assert.That(combined, Does.Contain("TurnBackActionRequestSubmissionProvider"));
            Assert.That(combined, Does.Contain("ActionInterruptArbiter.Arbitrate"));
            Assert.That(combined, Does.Not.Contain("ExecuteActionMovement"));
            Assert.That(combined, Does.Not.Contain("ExecuteLocomotionMotion"));
            Assert.That(combined, Does.Not.Contain(".Present("));
            Assert.That(combined, Does.Not.Contain("runner.Tick"));
        }

        [Test]
        public void FrameSubmittersDoNotApplyOutputSideEffects()
        {
            string actionSubmitter = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Action/Runtime/CommittedActionFrameSubmitter.cs"));
            string locomotionSubmitter = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Movement/Runtime/LocomotionFrameSubmitter.cs"));
            string submitters = actionSubmitter + "\n" + locomotionSubmitter;
            string pipeline = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Pipeline/Runtime/CharacterFramePipeline.cs"));

            Assert.That(actionSubmitter, Does.Contain("CharacterFrameSubmission"));
            Assert.That(submitters, Does.Not.Contain("ExecuteActionMovement"));
            Assert.That(submitters, Does.Not.Contain("ExecuteLocomotionMotion"));
            Assert.That(submitters, Does.Not.Contain("PresentStateFrameAnimationForPipeline"));
            Assert.That(submitters, Does.Not.Contain("WriteStateFrameActionFactsForPipeline"));
            Assert.That(pipeline, Does.Contain("CharacterFrameOutputApplier"));
        }

        [Test]
        public void OldLocalPipelineClassNamesAreNotFormalRuntimePaths()
        {
            string actionRoot = Path.Combine(Application.dataPath, "Scripts/Character/Action");
            string movementRoot = Path.Combine(Application.dataPath, "Scripts/Character/Movement");

            Assert.False(File.Exists(Path.Combine(actionRoot, "Runtime/FullBodyFramePipeline.cs")));
            Assert.False(File.Exists(Path.Combine(actionRoot, "Model/FullBodyFramePipelineTypes.cs")));
            Assert.False(File.Exists(Path.Combine(actionRoot, "Runtime/CharacterFramePipeline.cs")));
            Assert.False(File.Exists(Path.Combine(actionRoot, "Model/CharacterFramePipelineTypes.cs")));
            Assert.False(File.Exists(Path.Combine(actionRoot, "Contracts/ICharacterFrameRuntimePort.cs")));
            Assert.False(File.Exists(Path.Combine(actionRoot, "Solver/FullBody" + "ActionRequestGate.cs")));
            Assert.False(File.Exists(Path.Combine(actionRoot, "Solver/FullBodyPipelineActionRequestResolver.cs")));
            Assert.False(File.Exists(Path.Combine(actionRoot, "Solver/FullBody" + "ActionRequestCandidates.cs")));
            Assert.False(File.Exists(Path.Combine(actionRoot, "Solver/FullBody" + "ActionInterruptGate.cs")));
            Assert.False(File.Exists(Path.Combine(movementRoot, "Solver/LocomotionFramePipeline.cs")));
            Assert.False(File.Exists(Path.Combine(movementRoot, "Model/LocomotionFramePipelineInput.cs")));
            Assert.False(File.Exists(Path.Combine(movementRoot, "Model/LocomotionFramePipelineResult.cs")));
        }

        [Test]
        public void CharacterRuntimeLayerSolversAvoidRuntimeAdapterReferences()
        {
            string animationSolver = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Animation/Solver/LocomotionAnimationAliasResolver.cs"));
            string movementSolver = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Movement/Solver/Motion/AnimationPlanarDeltaResolver.cs"));
            string actionSolver = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Action/Solver/CommittedActionRequestSubmissionResolver.cs"));

            Assert.That(animationSolver, Does.Not.Contain("Animancer"));
            Assert.That(animationSolver, Does.Not.Contain("Animator"));
            Assert.That(animationSolver, Does.Not.Contain("AnimationClip"));
            Assert.That(animationSolver, Does.Not.Contain("CharacterController"));
            Assert.That(animationSolver, Does.Not.Contain("InputAction"));

            Assert.That(movementSolver, Does.Not.Contain("CharacterController"));
            Assert.That(movementSolver, Does.Not.Contain("Transform"));
            Assert.That(movementSolver, Does.Not.Contain(".Move("));
            Assert.That(movementSolver, Does.Not.Contain("transform."));

            Assert.That(actionSolver, Does.Not.Contain("new CharacterStateMachineRunner"));
            Assert.That(actionSolver, Does.Not.Contain("RegisterTick"));
            Assert.That(actionSolver, Does.Not.Contain("CharacterController"));
            Assert.That(actionSolver, Does.Not.Contain("Animancer"));
            Assert.That(actionSolver, Does.Not.Contain("InputAction"));
            Assert.That(actionSolver, Does.Not.Contain("Camera.main"));
            Assert.That(actionSolver, Does.Not.Contain(".Move("));
            Assert.That(actionSolver, Does.Not.Contain(".Present("));
            Assert.That(actionSolver, Does.Not.Contain("Resources.Load"));
        }

        [Test]
        public void CharacterFrameDiagnosticsUsesUnifiedLogOnly()
        {
            string diagnostics = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Action/Diagnostics/CharacterFrameDiagnostics.cs"));
            string adapter = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Action/Diagnostics/CharacterFrameDiagnosticAdapter.cs"));
            string sink = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Diagnostics/RuntimeDiagnosticLogCharacterSink.cs"));

            Assert.That(diagnostics, Does.Contain("CharacterFrameDiagnosticAdapter"));
            Assert.That(diagnostics, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(adapter, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(sink, Does.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(diagnostics, Does.Not.Contain("Debug.Log"));
            Assert.That(diagnostics, Does.Not.Contain("new CharacterStateMachineRunner"));
            Assert.That(diagnostics, Does.Not.Contain(".Move("));
            Assert.That(diagnostics, Does.Not.Contain(".Present("));
            Assert.That(adapter, Does.Contain("character-frame-path-changed"));
            Assert.That(adapter, Does.Contain("character-frame-pending-transition-changed"));
            Assert.That(adapter, Does.Contain("locomotion-phase-changed"));
            Assert.That(adapter, Does.Contain("action-accepted"));
            Assert.That(adapter, Does.Contain("character-frame-tick-snapshot"));
            Assert.That(adapter, Does.Contain("animation-tick-snapshot"));
            Assert.That(adapter, Does.Contain("state-machine-definition-invalid"));
        }

        [Test]
        public void CharacterDiagnosticAdapterBoundaryOwnsRuntimeLogSubmission()
        {
            string characterRoot = Path.Combine(Application.dataPath, "Scripts/Character");
            string runner = File.ReadAllText(Path.Combine(characterRoot, "StateMachine/Solver/Runtime/CharacterStateMachineRunner.cs"));
            string transition = string.Join("\n", Directory.GetFiles(Path.Combine(characterRoot, "StateMachine/Solver/Transition"), "*.cs").Select(File.ReadAllText));
            string sampler = File.ReadAllText(Path.Combine(characterRoot, "StateMachine/Solver/Timeline/CharacterStateTimelineFactSampler.cs"));
            string pipeline = File.ReadAllText(Path.Combine(characterRoot, "Pipeline/Runtime/CharacterFramePipeline.cs"));
            string characterFrameOutput = File.ReadAllText(Path.Combine(characterRoot, "Action/Runtime/CharacterFrameOutputRuntime.cs"));
            string locomotionOutput = File.ReadAllText(Path.Combine(characterRoot, "Movement/Runtime/LocomotionOutputRuntime.cs"));
            string actionArbiter = File.ReadAllText(Path.Combine(characterRoot, "Action/Solver/ActionInterruptArbiter.cs"));
            string fullBodyDiagnostics = File.ReadAllText(Path.Combine(characterRoot, "Action/Diagnostics/CharacterFrameDiagnostics.cs"));
            string locomotionDiagnostics = File.ReadAllText(Path.Combine(characterRoot, "Movement/Diagnostics/LocomotionDiagnostics.cs"));

            Assert.That(runner, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(transition, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(sampler, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(pipeline, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(characterFrameOutput, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(locomotionOutput, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(actionArbiter, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(fullBodyDiagnostics, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
            Assert.That(locomotionDiagnostics, Does.Not.Contain("RuntimeDiagnosticLog.Submit"));
        }

        [Test]
        public void LocomotionAnimationAliasResolverKeepsFallbackAliases()
        {
            Assert.AreEqual(
                "Idle",
                LocomotionAnimationAliasResolver.ResolveAliasKey(null, BasicMovementPhase.Idle, BasicMovementGait.Walk));
            Assert.AreEqual(
                "WalkStart",
                LocomotionAnimationAliasResolver.ResolveAliasKey(null, BasicMovementPhase.MoveStart, BasicMovementGait.Walk));
            Assert.AreEqual(
                "RunLoop",
                LocomotionAnimationAliasResolver.ResolveAliasKey(null, BasicMovementPhase.MoveLoop, BasicMovementGait.Run));
            Assert.AreEqual(
                "RunEnd",
                LocomotionAnimationAliasResolver.ResolveAliasKey(null, BasicMovementPhase.MoveStop, BasicMovementGait.Run));
            Assert.AreEqual(
                "Locomotion.Turn.Back",
                LocomotionAnimationAliasResolver.ResolveAliasKey(null, BasicMovementPhase.TurnBack, BasicMovementGait.Run));
        }

        [Test]
        public void AnimationPlanarDeltaResolverMapsMotionWithoutRuntimeAdapters()
        {
            BasicMovementMotionFacts localFacts = new BasicMovementMotionFacts(
                true,
                new Vector3(0f, 0f, 2f),
                0f,
                BasicMovementPhase.TurnBack,
                "Locomotion.Turn.Back",
                planarDeltaSpace: BasicMovementPlanarDeltaSpace.Local);
            MovementCommand localCommand = new MovementCommand(
                Vector3.forward,
                0f,
                0f,
                0.02f,
                BasicMovementPhase.TurnBack,
                BasicMovementGait.Run,
                localFacts);
            Vector3 localWorldDelta = AnimationPlanarDeltaResolver.ResolveWorldDelta(
                in localCommand,
                Quaternion.Euler(0f, 90f, 0f));

            Assert.That(localWorldDelta.x, Is.EqualTo(2f).Within(0.001f));
            Assert.That(localWorldDelta.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(localWorldDelta.z, Is.EqualTo(0f).Within(0.001f));

            BasicMovementMotionFacts entryFacts = new BasicMovementMotionFacts(
                true,
                new Vector3(1f, 0f, 2f),
                0f,
                BasicMovementPhase.TurnBack,
                "Locomotion.Turn.Back",
                planarDeltaSpace: BasicMovementPlanarDeltaSpace.EntryLocal,
                entryPlanarBasisForward: Vector3.forward);
            MovementCommand entryCommand = new MovementCommand(
                Vector3.zero,
                0f,
                0f,
                0.02f,
                BasicMovementPhase.TurnBack,
                BasicMovementGait.Run,
                entryFacts);
            Vector3 entryWorldDelta = AnimationPlanarDeltaResolver.ResolveWorldDelta(in entryCommand, Quaternion.identity);

            Assert.That(entryWorldDelta.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(entryWorldDelta.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(entryWorldDelta.z, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void RuntimeDodgeConfigDoesNotFallbackToDefault()
        {
            string module = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Action/Runtime/CommittedActionRuntimeModule.cs"));
            string actionSubmitter = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Action/Runtime/CommittedActionFrameSubmitter.cs"));

            Assert.That(module, Does.Not.Contain("DodgeActionTuning.Default"));
            Assert.That(actionSubmitter, Does.Not.Contain("DodgeActionTuning.Default"));
            Assert.That(actionSubmitter, Does.Contain("TryResolveActionCatalog"));
            Assert.That(actionSubmitter, Does.Not.Contain("TryResolveDodgeActionTuning"));
        }

        [Test]
        public void TurnBackRootMotionDoesNotExposeAnimatorDeltaPendingBuffer()
        {
            string provider = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Movement/Runtime/LocomotionMotionFactsProvider.cs"));
            string resolver = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Movement/Solver/TurnBack/TurnBackMotionResolver.cs"));

            string body = ExtractMethodBody(provider, "ResolveTurnBackRootMotionFacts", "TurnBackMotionPolicy policy");
            string resolverBody = ExtractMethodBody(resolver, "Resolve", "AnimationMotionProfileSample bakedSample");
            string bakedGuard = ExtractMethodBody(resolver, "RequiresBakedMotion");
            string planarResolver = ExtractMethodBody(resolver, "ResolvePlanarDelta");
            string yawResolver = ExtractMethodBody(resolver, "ResolveYawDelta");

            Assert.That(body, Does.Contain("TurnBackMotionResolver.Resolve"));
            Assert.That(resolverBody, Does.Contain("motionWindowActive"));
            Assert.That(body, Does.Not.Contain("RequiresTurnBackRuntimeRootDelta"));
            Assert.That(resolverBody, Does.Not.Contain("RequiresTurnBackRuntimeRootDelta"));
            Assert.That(body, Does.Not.Contain("RuntimeRootDelta"));
            Assert.That(resolverBody, Does.Not.Contain("RuntimeRootDelta"));
            Assert.That(body, Does.Not.Contain("ConsumeRootMotionDelta"));
            Assert.That(resolverBody, Does.Not.Contain("ConsumeRootMotionDelta"));
            Assert.That(body, Does.Not.Contain("TryResolveTurnBackAuthoredRootMotionDelta"));
            Assert.That(body, Does.Not.Contain("ResolveAuthoredRootMotionSource"));
            Assert.That(provider, Does.Not.Contain("ILocomotionAuthoredRootMotionSource"));
            Assert.That(provider, Does.Not.Contain("AnimationClipRootMotionSampler"));
            Assert.That(body, Does.Contain("? ResolveTurnBackBakedMotionSample"));
            Assert.That(body, Does.Contain("TurnBackMotionResolver.RequiresBakedMotion(in policy)"));
            Assert.That(bakedGuard, Does.Contain("TurnBackMotionYawSource.BakedMotionProfile"));
            Assert.That(bakedGuard, Does.Contain("TurnBackMotionTranslationSource.BakedMotionProfile"));
            Assert.That(planarResolver, Does.Not.Contain("RuntimeRootDelta"));
            Assert.That(planarResolver, Does.Not.Contain("rawDelta"));
            Assert.That(yawResolver, Does.Not.Contain("RuntimeRootDelta"));
            Assert.That(yawResolver, Does.Not.Contain("rawDelta"));
            Assert.That(yawResolver, Does.Contain("bakedSample.HasMotionContribution ? bakedSample.YawDelta : 0f"));
            Assert.That(provider, Does.Contain("ResolveMotionProfile"));
            Assert.That(provider, Does.Contain("AnimationMotionProfileSampler"));
        }

        [Test]
        public void LocomotionPresenterDoesNotApplyCharacterRootMotion()
        {
            string presenter = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Animation/Runtime/CharacterAnimancerPresenter.cs"));

            Assert.That(presenter, Does.Not.Contain("CharacterController"));
            Assert.That(presenter, Does.Not.Contain(".Move("));
            Assert.That(presenter, Does.Not.Contain("transform.position ="));
            Assert.That(presenter, Does.Not.Contain("transform.rotation ="));
            Assert.That(presenter, Does.Not.Contain("transform.Translate"));
            Assert.That(presenter, Does.Not.Contain("transform.Rotate"));
            Assert.That(presenter, Does.Not.Contain("pendingRootMotionDelta"));
            Assert.That(presenter, Does.Not.Contain("ConsumeRootMotionDelta"));
            Assert.That(presenter, Does.Not.Contain("ILocomotionRootMotionSource"));
            Assert.That(presenter, Does.Not.Contain("ILocomotionRootMotionRollbackStateProvider"));
            Assert.That(presenter, Does.Not.Contain("ILocomotionAuthoredRootMotionSource"));
            Assert.That(presenter, Does.Not.Contain("TrySampleAuthoredRootMotion"));
            Assert.That(presenter, Does.Not.Contain("AnimationClipRootMotionSampler"));
        }

        [Test]
        public void BasicLocomotionPipelineDoesNotRecomputeDecisionFacts()
        {
            string pipeline = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/Movement/Solver/BasicLocomotionPipeline.cs"));

            Assert.That(pipeline, Does.Not.Contain("MovementInputIntent.FromRaw"));
            Assert.That(pipeline, Does.Not.Contain("CameraRelativeMovementResolver.Resolve"));
            Assert.That(pipeline, Does.Not.Contain("ICameraMovementBasisProvider"));
        }

        [Test]
        public void RuntimeBlackboardDoesNotReferenceForbiddenRuntimeObjects()
        {
            string blackboard = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Character/StateMachine/Model/CharacterRuntimeBlackboard.cs"));
            string animationRuntime = string.Join("\n", new[]
            {
                Path.Combine(Application.dataPath, "Scripts/Character/Animation/Runtime/CharacterAnimancerPresenter.cs")
            }.Select(File.ReadAllText));

            Assert.That(blackboard, Does.Not.Contain("Animancer"));
            Assert.That(blackboard, Does.Not.Contain("Transform"));
            Assert.That(blackboard, Does.Not.Contain("Camera"));
            Assert.That(blackboard, Does.Not.Contain("CharacterController"));
            Assert.That(blackboard, Does.Not.Contain("InputAction"));
            Assert.That(blackboard, Does.Not.Contain("UnityEngine.Object"));
            Assert.That(blackboard, Does.Not.Contain("AnimationClip"));
            Assert.That(blackboard, Does.Not.Contain("TransitionAsset"));
            Assert.That(blackboard, Does.Not.Contain("TransitionLibrary"));
            Assert.That(blackboard, Does.Not.Contain("Dictionary<"));
            Assert.That(animationRuntime, Does.Not.Contain("RuntimeBlackboard"));
            Assert.That(animationRuntime, Does.Not.Contain("WriteActionFacts"));
            Assert.That(animationRuntime, Does.Not.Contain("WriteAnimationFacts"));
        }

        [Test]
        public void StateMachineModelDoesNotReferenceAnimationRuntimeObjects()
        {
            string modelRoot = Path.Combine(Application.dataPath, "Scripts/Character/StateMachine/Model");
            string combined = string.Join("\n", Directory.GetFiles(modelRoot, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

            Assert.That(combined, Does.Not.Contain("Animancer"));
            Assert.That(combined, Does.Not.Contain("AnimationClip"));
            Assert.That(combined, Does.Not.Contain("TransitionAsset"));
            Assert.That(combined, Does.Not.Contain("TransitionLibrary"));
            Assert.That(combined, Does.Not.Contain("Animator"));
            Assert.That(combined, Does.Not.Contain("CharacterController"));
            Assert.That(combined, Does.Not.Contain("InputAction"));
        }

        [Test]
        public void DefaultLocomotionStateGraphAssetDoesNotDependOnUnityHfsmRuntime()
        {
            string asset = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Configs/3C/StateMachine/Locomotion/Corin/CorinLocomotionStateGraph.asset"));
            string runtime = string.Join("\n", Directory.GetFiles(
                Path.Combine(Application.dataPath, "Scripts/Character/StateMachine"),
                "*.cs",
                SearchOption.AllDirectories).Select(File.ReadAllText));

            Assert.That(asset, Does.Not.Contain("UnityHFSM"));
            Assert.That(asset, Does.Not.Contain("UnityHFSM.StateMachine"));
            Assert.That(runtime, Does.Not.Contain("UnityHFSM"));
        }

        [Test]
        public void RuntimeCodeNoLongerReferencesOldSplitPathTypes()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string combined = string.Join("\n", Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

            Assert.That(combined, Does.Not.Contain("BasicLocomotionStateMachine"));
            Assert.That(combined, Does.Not.Contain("LocomotionStateGraphConfigSO"));
            Assert.That(combined, Does.Not.Contain("FullBodyHfsmStateTreeBuilder"));
            Assert.That(combined, Does.Not.Contain("FullBodyHfsmStateTreeDriver"));
            Assert.That(combined, Does.Not.Contain("DodgeActionRuntime"));
            Assert.That(combined, Does.Not.Contain("DodgeFullBody" + "ActionModule"));
            Assert.That(combined, Does.Not.Contain("FullBody" + "ActionSetSO"));
            Assert.That(combined, Does.Not.Contain("FullBody" + "ActionAnimationSetSO"));
            Assert.That(combined, Does.Not.Contain("ActionAnimationProfileSO"));
        }

        [Test]
        public void RuntimeCodeDoesNotIntroduceTurnBackSpecificControllerOrStateMachine()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string[] turnBackControllers = Directory.GetFiles(scriptsRoot, "*TurnBack*Controller*.cs", SearchOption.AllDirectories);
            string[] turnBackStateMachines = Directory.GetFiles(scriptsRoot, "*TurnBack*StateMachine*.cs", SearchOption.AllDirectories);

            Assert.IsEmpty(turnBackControllers);
            Assert.IsEmpty(turnBackStateMachines);
        }

        sealed class DuplicateConditionEvaluator : ICharacterStateTransitionConditionEvaluator
        {
            readonly CharacterStateTransitionConditionKind[] supported;

            public DuplicateConditionEvaluator(CharacterStateTransitionConditionKind conditionKind)
            {
                supported = new[] { conditionKind };
            }

            public string Name => "Duplicate";
            public IReadOnlyList<CharacterStateTransitionConditionKind> SupportedConditions => supported;

            public CharacterStateTransitionConditionEvaluationResult Evaluate(in CharacterStateTransitionConditionEvaluationInput input)
            {
                return CharacterStateTransitionConditionEvaluationResult.From(in input, true, "duplicate-test");
            }
        }

        static CharacterStateTransitionConditionEvaluationResult EvaluateCondition(
            CharacterStateTransitionCondition condition,
            in CharacterStateMachineContext context,
            CharacterStateNodeDefinition currentNode = null,
            CharacterStateVariant currentVariant = CharacterStateVariant.None,
            float stateTime = 0f,
            float projectedStateTime = 0.1f)
        {
            CharacterStateId source = currentNode != null ? currentNode.StateId : CharacterStateIds.Idle;
            CharacterStateTransitionDefinition transition = new CharacterStateTransitionDefinition(
                source.Value,
                CharacterStateIds.MoveLoop,
                0,
                condition);
            CharacterStateTransitionConditionEvaluationInput input = new CharacterStateTransitionConditionEvaluationInput(
                condition,
                in context,
                currentNode,
                currentVariant,
                source,
                transition,
                stateTime,
                projectedStateTime);

            return CharacterStateTransitionEvaluatorCollection.Default.Evaluate(in input);
        }

        static CharacterStateMachineRunner CreateRunner()
        {
            return new CharacterStateMachineRunner(LoadConfiguredStateMachineDefinition());
        }

        static CharacterStateMachineDefinition LoadConfiguredStateMachineDefinition()
        {
            return LoadConfiguredStateMachineDefinitionAsset().ToDefinition();
        }

        static CharacterStateMachineDefinition LoadConfiguredLocomotionStateGraphDefinition()
        {
            return LoadConfiguredLocomotionStateGraphAsset().ToDefinition();
        }

        static CharacterStateMachineDefinitionSO LoadConfiguredStateMachineDefinitionAsset()
        {
            CharacterStateMachineDefinitionSO asset = AssetDatabase.LoadAssetAtPath<CharacterStateMachineDefinitionSO>(
                LocomotionStateGraphAssetPath);
            Assert.NotNull(asset);
            return asset;
        }

        static CharacterStateMachineDefinitionSO LoadConfiguredLocomotionStateGraphAsset()
        {
            CharacterStateMachineDefinitionSO asset = AssetDatabase.LoadAssetAtPath<CharacterStateMachineDefinitionSO>(
                LocomotionStateGraphAssetPath);
            Assert.NotNull(asset);
            return asset;
        }

        static CharacterConfigSO LoadConfiguredCharacterConfigAsset()
        {
            CharacterConfigSO asset = AssetDatabase.LoadAssetAtPath<CharacterConfigSO>(
                "Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset");
            Assert.NotNull(asset);
            Assert.NotNull(asset.StateMachine);
            Assert.NotNull(asset.Movement);
            return asset;
        }

        static CharacterConfigSO CreateCharacterConfig(RunLocomotionAnimationConfigSO locomotionAnimation)
        {
            CharacterConfigSO baseConfig = LoadConfiguredCharacterConfigAsset();
            CharacterConfigSO config = ScriptableObject.CreateInstance<CharacterConfigSO>();
            SetPrivateField(config, "stateMachine", baseConfig.StateMachine);
            SetPrivateField(config, "movement", baseConfig.Movement);
            SetPrivateField(config, "locomotionAnimation", locomotionAnimation != null ? locomotionAnimation : baseConfig.LocomotionAnimation);
            SetPrivateField(config, "actionInterruptPolicy", baseConfig.ActionInterruptPolicy);
            SetPrivateField(config, "bodyClaimPolicy", baseConfig.BodyClaimPolicy);
            SetPrivateField(config, "actionCatalog", baseConfig.ActionCatalog);
            SetPrivateField(config, "behaviorRuntimeDefinition", baseConfig.BehaviorRuntimeDefinition);
            SetPrivateField(config, "inputActions", baseConfig.InputActions);
            SetPrivateField(config, "moveAction", baseConfig.MoveAction);
            SetPrivateField(config, "runAction", baseConfig.RunAction);
            SetPrivateField(config, "lookAction", baseConfig.LookAction);
            SetPrivateField(config, "cameraConfig", baseConfig.CameraConfig);
            return config;
        }

        static bool TickRuntime(
            CharacterFrameRuntimeController runtime,
            int step,
            float deltaTime,
            Vector2 move,
            bool runHeld = false,
            bool dodgePressed = false)
        {
            PredictionButtonFrame dodge = dodgePressed
                ? new PredictionButtonFrame(true, true, false)
                : PredictionButtonFrame.None;
            CharacterFrameInput input = new CharacterFrameInput(
                step,
                new BasicLocomotionInputSnapshot(deltaTime, move, Vector2.zero, runHeld),
                dodgePressed,
                dodge,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None);
            return runtime.Tick(in input);
        }

        static ActionMotionResolveResult ResolveActionMotion(in CharacterStateMachineFrame frame, float deltaTime)
        {
            CharacterActionCatalogSO actionCatalog = LoadConfiguredCharacterConfigAsset().ActionCatalog;
            ActionTimelineCompileContext compileContext = ActionTimelineCompileContext.FromTickRate(SimulationTickRate.Default);
            CharacterActionCatalog catalog = actionCatalog != null ? actionCatalog.ToCatalog(in compileContext) : CharacterActionCatalog.Empty;
            DodgeActionTuning dodgeTuning = default;
            bool hasDodgeAction = catalog.TryGetDodgeDefinition(out CharacterActionDefinition definition) &&
                                  definition.TryGetDodgeTuning(out dodgeTuning);
            ActionMotionSpec spec = DodgeActionMotionSpecAdapter.Resolve(
                frame.ActionMotionSpec,
                hasDodgeAction,
                in dodgeTuning);
            ActionMotionResolveInput input = new ActionMotionResolveInput(
                spec,
                deltaTime,
                frame.TimelineFacts,
                CharacterRuntimeActionFacts.Default,
                true);
            return ActionMotionResolver.Resolve(in input);
        }

        static int CountOccurrences(string source, string text)
        {
            int count = 0;
            int index = 0;
            while ((index = source.IndexOf(text, index)) >= 0)
            {
                count++;
                index += text.Length;
            }

            return count;
        }

        sealed class FakeCharacterDiagnosticSink : ICharacterDiagnosticSink
        {
            readonly List<RuntimeDiagnosticLogEvent> events = new List<RuntimeDiagnosticLogEvent>();

            public IReadOnlyList<RuntimeDiagnosticLogEvent> Events => events;

            public void Submit(in RuntimeDiagnosticLogEvent diagnosticEvent)
            {
                events.Add(diagnosticEvent);
            }
        }

        static void AssertMethodBodyDoesNotContain(string source, string methodName, string text)
        {
            string body = ExtractMethodBody(source, methodName);
            Assert.That(body, Does.Not.Contain(text));
        }

        static string ExtractSourceBlock(string source, string startMarker, string endMarker)
        {
            int start = source.IndexOf(startMarker, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, $"Start marker {startMarker} was not found.");
            int end = source.IndexOf(endMarker, start, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(end, 0, $"End marker {endMarker} was not found.");
            return source.Substring(start, end - start);
        }

        static string ExtractTypeBlock(string source, string typeMarker)
        {
            int start = source.IndexOf(typeMarker, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, $"Type marker {typeMarker} was not found.");
            int openBrace = source.IndexOf('{', start);
            Assert.GreaterOrEqual(openBrace, 0, $"Type marker {typeMarker} body was not found.");

            int depth = 0;
            for (int i = openBrace; i < source.Length; i++)
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

            Assert.Fail($"Type marker {typeMarker} body was not closed.");
            return string.Empty;
        }

        static string ReadCharacterFrameModelSources()
        {
            string modelRoot = Path.Combine(Application.dataPath, "Scripts/Character/Pipeline/Model");
            string[] files =
            {
                "CharacterFramePipelineStep.cs",
                "CharacterFrameInput.cs",
                "CharacterFrameContext.cs",
                "CharacterFrameSubmission.cs",
                "CharacterFrameOutput.cs",
                "CharacterFrameResult.cs",
                "CharacterFrameDiagnosticsSummary.cs"
            };
            return string.Join("\n", files.Select(file => File.ReadAllText(Path.Combine(modelRoot, file), System.Text.Encoding.UTF8)));
        }

        static string ExtractMethodBody(string source, string methodName)
        {
            return ExtractMethodBody(source, methodName, string.Empty);
        }

        static string ExtractMethodBody(string source, string methodName, string signatureMustContain)
        {
            int openBrace = FindMethodBodyOpenBrace(source, methodName, signatureMustContain);
            Assert.GreaterOrEqual(openBrace, 0, $"Method {methodName} was not found.");

            int depth = 0;
            for (int i = openBrace; i < source.Length; i++)
            {
                if (source[i] == '{')
                    depth++;
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return source.Substring(openBrace, i - openBrace + 1);
                }
            }

            Assert.Fail($"Method {methodName} body was not closed.");
            return string.Empty;
        }

        static int FindMethodBodyOpenBrace(string source, string methodName)
        {
            return FindMethodBodyOpenBrace(source, methodName, string.Empty);
        }

        static int FindMethodBodyOpenBrace(string source, string methodName, string signatureMustContain)
        {
            int searchIndex = 0;
            while (searchIndex < source.Length)
            {
                int nameIndex = source.IndexOf(methodName, searchIndex, System.StringComparison.Ordinal);
                if (nameIndex < 0)
                    return -1;

                int afterName = nameIndex + methodName.Length;
                if (!IsIdentifierBoundary(source, nameIndex - 1) ||
                    !IsIdentifierBoundary(source, afterName) ||
                    afterName >= source.Length ||
                    source[afterName] != '(')
                {
                    searchIndex = afterName;
                    continue;
                }

                int closeParen = FindMatchingParen(source, afterName);
                if (closeParen < 0)
                    return -1;

                if (!string.IsNullOrEmpty(signatureMustContain))
                {
                    string signature = source.Substring(nameIndex, closeParen - nameIndex + 1);
                    if (!signature.Contains(signatureMustContain))
                    {
                        searchIndex = closeParen + 1;
                        continue;
                    }
                }

                int next = NextNonWhitespace(source, closeParen + 1);
                if (next >= 0 && source[next] == '{')
                    return next;

                searchIndex = afterName;
            }

            return -1;
        }

        static int FindMatchingParen(string source, int openParen)
        {
            int depth = 0;
            for (int i = openParen; i < source.Length; i++)
            {
                if (source[i] == '(')
                    depth++;
                else if (source[i] == ')')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }

            return -1;
        }

        static int NextNonWhitespace(string source, int start)
        {
            for (int i = start; i < source.Length; i++)
            {
                if (!char.IsWhiteSpace(source[i]))
                    return i;
            }

            return -1;
        }

        static bool IsIdentifierBoundary(string source, int index)
        {
            if (index < 0 || index >= source.Length)
                return true;

            char value = source[index];
            return !char.IsLetterOrDigit(value) && value != '_';
        }

        static CharacterRuntimeBlackboardSnapshot BlackboardWithActionProgress(
            ActionAnimationKey key,
            float normalizedTime,
            bool isEnded)
        {
            return new CharacterRuntimeBlackboardSnapshot(
                CharacterRuntimeLocomotionFacts.Default,
                CharacterRuntimeActionFacts.Default,
                new CharacterRuntimeAnimationFacts(
                    AnimationPhasePlaybackProgress.Invalid(BasicMovementPhase.Idle),
                    string.Empty,
                    key,
                    normalizedTime,
                    true,
                    isEnded,
                    key.Value,
                    1),
                CharacterRuntimeDebugFacts.Record("Animation", 1));
        }

        static CharacterRuntimeBlackboardSnapshot BlackboardWithLocomotionDirection(Vector3 worldDirection)
        {
            return new CharacterRuntimeBlackboardSnapshot(
                new CharacterRuntimeLocomotionFacts(
                    BasicMovementPhase.MoveLoop,
                    BasicMovementGait.Run,
                    BasicMovementGait.Run,
                    false,
                    BasicMovementGait.Walk,
                    true,
                    worldDirection,
                    true,
                    1f,
                    1),
                CharacterRuntimeActionFacts.Default,
                CharacterRuntimeAnimationFacts.Default,
                CharacterRuntimeDebugFacts.Record("Locomotion", 1));
        }

        static CharacterRuntimeBlackboardSnapshot BlackboardWithLocomotionProgress(
            string aliasKey,
            float normalizedTime,
            bool isEnded)
        {
            return new CharacterRuntimeBlackboardSnapshot(
                CharacterRuntimeLocomotionFacts.Default,
                CharacterRuntimeActionFacts.Default,
                new CharacterRuntimeAnimationFacts(
                    new AnimationPhasePlaybackProgress(BasicMovementPhase.TurnBack, aliasKey, normalizedTime, true, isEnded),
                    aliasKey,
                    ActionAnimationPlaybackProgress.Invalid,
                    string.Empty,
                    1),
                CharacterRuntimeDebugFacts.Record("Animation", 1));
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
            StateTimelineWindowFacts timelineFacts = default,
            int currentStep = 1)
        {
            Vector2 moveInput = move ? Vector2.up : Vector2.zero;
            MovementInputIntent intent = MovementInputIntent.FromRaw(moveInput, 0.1f, runHeld);
            Vector3 resolvedWorldDirection = move
                ? worldDirection.sqrMagnitude > 0.000001f ? worldDirection.normalized : Vector3.forward
                : Vector3.zero;
            Vector3 resolvedFacingForward = facingForward.sqrMagnitude > 0.000001f ? facingForward.normalized : Vector3.forward;
            CharacterInputRequestFact resolvedRequest = request.HasRequest ? request : CharacterInputRequestFact.None(InputRequestKind.Dodge);
            CharacterRuntimeBlackboardSnapshot resolvedBlackboard = !string.IsNullOrEmpty(runtimeBlackboard.Debug.LastWriter)
                ? runtimeBlackboard
                : CharacterRuntimeBlackboardSnapshot.Empty;
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
                resolvedBlackboard,
                timelineFacts);
        }

        static LocomotionDecisionFacts DecisionFacts(
            in BasicLocomotionInputSnapshot input,
            in BasicMovementSettings settings,
            Vector3 worldDirection,
            Vector3 facingForward)
        {
            MovementInputIntent intent = MovementInputIntent.FromRaw(input.Move, settings.InputDeadZone, input.RunHeld);
            return new LocomotionDecisionFacts(
                intent,
                intent.HasMoveIntent ? intent.Gait : BasicMovementGait.Walk,
                BasicMovementPhaseFacts.None,
                new LocomotionSpatialFacts(worldDirection, facingForward, Vector3.forward, Vector3.right),
                LocomotionTurnBackIntent.None);
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
                CharacterStateVariant.None,
                worldDirection);
        }

        static CharacterInputRequestFact DodgeRequest(CharacterStateVariant variant, Vector3 direction)
        {
            return new CharacterInputRequestFact(
                true,
                InputRequestKind.Dodge,
                1,
                4,
                TestDodgeTuning().Priority,
                variant,
                direction);
        }

        static DodgeActionTuning TestDodgeTuning()
        {
            return new DodgeActionTuning(0.35f, 4f, 0.35f, 3f, 30, 20, true, false);
        }

        static ActionInterruptPolicySetSO CreateDodgePolicyAsset(int minPriority)
        {
            ActionInterruptPolicySetSO asset = ScriptableObject.CreateInstance<ActionInterruptPolicySetSO>();
            FieldInfo field = typeof(ActionInterruptPolicySetSO).GetField("policies", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(asset, new[]
            {
                new ActionInterruptPolicyDefinition(ActionStateIds.None.Value, ActionStateIds.Dodge.Value, minPriority),
                new ActionInterruptPolicyDefinition(ActionStateIds.Dodge.Value, ActionStateIds.Dodge.Value, minPriority)
            });
            return asset;
        }

        static CharacterStateMachineSnapshot DodgeSnapshot(float stateTime)
        {
            return new CharacterStateMachineSnapshot(
                CharacterStateIds.Dodge,
                stateTime,
                CharacterStateVariant.Directional,
                string.Empty,
                new[] { CharacterStateTag.Character, CharacterStateTag.Action, CharacterStateTag.Dodge });
        }

        static int ResolveActionResistance(ActionStateId activeActionState, in DodgeActionTuning config)
        {
            return activeActionState == ActionStateIds.Dodge ? config.Resistance : 0;
        }

        static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, fieldName);
            field.SetValue(target, value);
        }

        static void CreateAnimationPresenterRig(
            out GameObject gameObject,
            out CharacterAnimancerPresenter locomotionPresenter,
            out CharacterAnimancerPresenter actionPresenter,
            out AnimancerComponent animancer,
            out AnimationClip idleClip,
            out AnimationClip runClip,
            out AnimationClip dodgeClip)
        {
            gameObject = new GameObject("animation-presenter-rig");
            gameObject.AddComponent<Animator>();
            animancer = gameObject.AddComponent<AnimancerComponent>();
            locomotionPresenter = gameObject.AddComponent<CharacterAnimancerPresenter>();
            actionPresenter = locomotionPresenter;
            AnimatorRootMotionController.Resolve(animancer);
            idleClip = CreateClip("Idle");
            runClip = CreateClip("RunLoop");
            dodgeClip = CreateClip("Action.Dodge");

            TransitionLibrary library = new TransitionLibrary();
            library.AddTransition(StringReference.Get("Idle"), CreateClipTransition(idleClip));
            library.AddTransition(StringReference.Get("RunLoop"), CreateClipTransition(runClip));
            library.AddTransition(StringReference.Get("Locomotion.Turn.Back"), CreateClipTransition(runClip));
            library.AddTransition(StringReference.Get(ActionAnimationKeys.DodgeBackstep.Value), CreateClipTransition(dodgeClip));
            library.AddTransition(StringReference.Get(ActionAnimationKeys.DodgeDirectional.Value), CreateClipTransition(dodgeClip));
            animancer.Graph.Transitions = library;
        }

        static void AssertTurnBackLibraryUsesInPlaceVisualClip(string libraryPath)
        {
            TransitionLibraryAsset asset = AssetDatabase.LoadAssetAtPath<TransitionLibraryAsset>(libraryPath);

            Assert.NotNull(asset, libraryPath);
            Assert.NotNull(asset.Library, libraryPath);
            Assert.True(
                asset.Library.TryGetTransition(StringReference.Get("Locomotion.Turn.Back"), out TransitionModifierGroup group),
                libraryPath);

            ITransition transition = group.Transition;
            if (transition is TransitionAssetBase transitionAsset)
                transition = transitionAsset.GetTransition();

            ClipTransition clipTransition = transition as ClipTransition;
            Assert.NotNull(clipTransition, libraryPath);
            Assert.True(clipTransition.IsValid, libraryPath);
            Assert.NotNull(clipTransition.Clip, libraryPath);
            StringAssert.Contains("TurnBack", clipTransition.Clip.name, libraryPath);
            StringAssert.Contains("Inplace", clipTransition.Clip.name, libraryPath);
            Assert.That(clipTransition.Clip.name, Does.Not.Contain("Rootmotion"), libraryPath);
        }

        static AnimationClip CreateClip(string name)
        {
            AnimationClip clip = new AnimationClip { name = name };
            clip.legacy = false;
            return clip;
        }

        static void EnsureTestFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrWhiteSpace(parent))
                EnsureTestFolder(parent);

            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(string.IsNullOrWhiteSpace(parent) ? "Assets" : parent, name);
        }

        static ClipTransition CreateClipTransition(AnimationClip clip)
        {
            return new ClipTransition
            {
                Clip = clip,
                FadeDuration = 0.08f
            };
        }

        sealed class TestCameraBasisProvider : ICameraMovementBasisProvider
        {
            public TestCameraBasisProvider(Vector3 forward, Vector3 right)
            {
                CameraPlanarForward = forward;
                CameraPlanarRight = right;
            }

            public Vector3 CameraPlanarForward { get; }
            public Vector3 CameraPlanarRight { get; }
        }

        sealed class TestFacingDirectionProvider : IFacingDirectionProvider
        {
            public TestFacingDirectionProvider(Vector3 facingForward)
            {
                FacingForward = facingForward;
            }

            public Vector3 FacingForward { get; }
        }

        sealed class TestFacingDirectionProviderComponent : MonoBehaviour, IFacingDirectionProvider
        {
            public Vector3 FacingForward => Vector3.forward;
        }

        sealed class TestAnimationPlaybackProgressSource : MonoBehaviour, IAnimationPhasePlaybackProgressSource
        {
            AnimationPhasePlaybackProgress playbackProgress = AnimationPhasePlaybackProgress.Invalid(BasicMovementPhase.Idle);

            public AnimationPhasePlaybackProgress CurrentPlaybackProgress => playbackProgress;

            public void SetPlaybackProgress(AnimationPhasePlaybackProgress progress)
            {
                playbackProgress = progress;
            }
        }

        sealed class TestActionAnimationPresenter : MonoBehaviour, ICharacterAnimationOutputPresenter, IActionAnimationPlaybackProgressController
        {
            public int PresentCount { get; private set; }
            public int ClearCount { get; private set; }
            public ActionAnimationKey CurrentKey { get; private set; }
            public ActionAnimationPlaybackIntent PlaybackIntent { get; private set; }
            public int SourceStep { get; private set; }
            public float NormalizedTime { get; set; }
            public bool PlaybackEnded { get; set; }
            public float CurrentNormalizedTime => NormalizedTime;
            public bool HasValidPlayback => CurrentKey.IsValid;
            public ActionAnimationPlaybackProgress CurrentPlaybackProgress => new ActionAnimationPlaybackProgress(CurrentKey, CurrentNormalizedTime, HasValidPlayback, PlaybackEnded, PlaybackIntent);
            public string CurrentAnimationName => CurrentKey.Value;
            public CharacterAnimationPlaybackSnapshot CurrentSnapshot => new CharacterAnimationPlaybackSnapshot(
                HasValidPlayback ? CharacterAnimationPlaybackDomain.Action : CharacterAnimationPlaybackDomain.None,
                CurrentKey.Value,
                AnimationPhasePlaybackProgress.Invalid(BasicMovementPhase.Idle),
                string.Empty,
                CurrentPlaybackProgress,
                CurrentAnimationName,
                SourceStep);

            public void PresentLocomotion(in MovementAnimationContext context)
            {
            }

            public bool PresentAction(in CharacterStateAnimationRequest request)
            {
                return Present(in request);
            }

            public void ClearActionPlayback()
            {
                Clear();
            }

            public bool Present(in CharacterStateAnimationRequest request)
            {
                PresentCount++;
                CurrentKey = request.Key;
                PlaybackIntent = request.ActionPlaybackIntent;
                SourceStep = request.SourceStep;
                return true;
            }

            public void Clear()
            {
                ClearCount++;
                CurrentKey = default;
                PlaybackIntent = ActionAnimationPlaybackIntent.Invalid;
                SourceStep = 0;
                NormalizedTime = 0f;
                PlaybackEnded = false;
            }

            public bool RestorePlaybackProgress(in ActionAnimationPlaybackProgress progress, string animationName)
            {
                if (!progress.HasValidPlayback)
                {
                    Clear();
                    return true;
                }

                CurrentKey = progress.Key;
                PlaybackIntent = progress.PlaybackIntent;
                NormalizedTime = progress.NormalizedTime;
                PlaybackEnded = progress.IsEnded;
                return true;
            }
        }
    }
}

