using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Animancer;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.AI.Editor;
using ThirdPersonCharacter.Behavior;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityAnimationClip = UnityEngine.AnimationClip;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class TrainingEnemyClosedLoopBuilder
    {
        const string RootPath = "Assets/Configs/Character/TrainingEnemy/Pipeline";
        const string DefinitionPath = RootPath + "/Definition/TrainingEnemyCharacterPipelineDefinition.asset";
        const string CharacterTreePath = RootPath + "/Graphs/TrainingEnemyCharacterRootTree.asset";
        const string InputProfilePath = RootPath + "/Input/TrainingEnemyInputProfile.asset";
        const string MoveReferencePath = RootPath + "/Input/References/TrainingEnemyMoveInputActionReference.asset";
        const string AttackReferencePath = RootPath + "/Input/References/TrainingEnemyAttackInputActionReference.asset";
        const string GameplayEffectProfilePath = RootPath + "/GameplayEffect/TrainingEnemyGameplayEffectProfile.asset";
        const string BodyMotionProfilePath = RootPath + "/Motion/TrainingEnemyBodyMotionProfile.asset";
        const string MoveBehaviorPath = RootPath + "/Behavior/TrainingEnemyLocomotionMoveBehavior.asset";
        const string AttackActionPath = RootPath + "/Actions/Attack/TrainingEnemyAttackActionProfile.asset";
        const string AttackContextPath = RootPath + "/Actions/Attack/TrainingEnemyAttackActionContext.asset";
        const string AttackTimelinePath = RootPath + "/Graphs/Timelines/TrainingEnemyAttackTimeline.asset";
        const string AttackSequencePath = "Assets/Configs/Character/TrainingEnemy/Presentation/Sequences/TrainingEnemyMonsterAttackSequence.asset";
        const string AttackSourcePath = "Assets/Configs/Character/TrainingEnemy/Presentation/Sources/TrainingEnemyAttackAnimationSource.asset";
        const string AnimationRigPath = "Assets/Configs/Character/TrainingEnemy/Presentation/Rig/TrainingEnemyMonsterAnimationRig.asset";
        const string FootAnalysisSourcePath = "Assets/Configs/Character/TrainingEnemy/Presentation/FootPlacement/TrainingEnemyMonsterFootPlacementAnalysisSource.asset";
        const string AnimationProfilePath = "Assets/Configs/Character/TrainingEnemy/Presentation/Profile/TrainingEnemyMonsterAnimationPresentationProfile.asset";
        const string MonsterClipRootPath = "Assets/Configs/Character/TrainingEnemy/Presentation/Clips";
        const string SourceInputProfilePath = "Assets/Configs/Character/Corin/Pipeline/Input/CorinCharacterInputProfile.asset";
        const string SourceGameplayEffectProfilePath = "Assets/Configs/Character/Corin/Pipeline/GameplayEffect/CorinCharacterGameplayEffectProfile.asset";
        const string AiRootPath = RootPath + "/AI/Authoring/TrainingEnemyAIController.AIRootTree.asset";
        const string AiPerceptionPath = RootPath + "/AI/Authoring/TrainingEnemyAIPerceptionProfile.asset";
        const string AiDefinitionPath = RootPath + "/AI/Authoring/TrainingEnemyAIControllerDefinition.asset";
        const string AnimationChannel = "training-enemy.monster.animation.full-body";
        const string FootAnalysisIdentity = "TrainingEnemy.Monster.FootPlacementAnalysis";

        [MenuItem("Tools/3C/Characters/Build Training Enemy Closed Loop Authoring")]
        public static void BuildAuthoring()
        {
            RequireEditMode();
            TrainingEnemyAnimationPresentationBuilder.Build();
            EnsureFolders();

            CharacterInputProfile sourceInput = RequireAsset<CharacterInputProfile>(SourceInputProfilePath);
            InputActionReference moveReference = BuildInputReference(
                MoveReferencePath,
                "TrainingEnemyMoveInputActionReference",
                sourceInput.InputValues.Single(value => value.InputValueId == "MoveAxis").SourceAction.action);
            InputActionReference attackReference = BuildInputReference(
                AttackReferencePath,
                "TrainingEnemyAttackInputActionReference",
                sourceInput.ActionRequests.Single(value => value.RequestId == "Attack").SourceAction.action);
            CharacterInputProfile inputProfile = BuildInputProfile(sourceInput.SourceAsset, moveReference, attackReference);
            CharacterGameplayEffectProfile gameplayEffectProfile = BuildGameplayEffectProfile();
            CharacterBodyMotionProfile bodyMotionProfile = LoadOrCreate<CharacterBodyMotionProfile>(BodyMotionProfilePath, "Training Enemy Body Motion Profile");
            GameplayBehaviorProfile moveBehavior = BuildMoveBehavior();
            ActionProfile attackAction = BuildAttackAction();
            ActionContextSlot attackContext = LoadOrCreate<ActionContextSlot>(AttackContextPath, "Training Enemy Attack Action Context");
            UnityAnimationClip attackClip = LoadMonsterClips()["Goblin_Ani_Attack_01"];
            CharacterAnimationSequenceAsset attackSequence = BuildAttackSequence(attackClip);
            TimelineAsset attackTimeline = BuildAttackTimeline(attackSequence);
            TransitionAsset attackSource = BuildAttackSource(attackClip);
            BindAttackAnimation(attackTimeline, attackSource);
            BaseTreeAsset characterTree = BuildCharacterTree(attackAction, attackContext, attackTimeline);
            CharacterPipelineDefinition definition = BuildDefinition(
                characterTree,
                inputProfile,
                gameplayEffectProfile,
                bodyMotionProfile,
                attackAction,
                moveBehavior);
            BuildAiAuthoring(definition);

            var errors = new List<string>();
            if (!definition.CollectConfigurationErrors(errors))
                throw new InvalidOperationException(string.Join("\n", errors));
            AssetDatabase.SaveAssets();
            Debug.Log("Training Enemy independent Character and AI authoring closed loop built. Publish Character Float32 products before compiling the AI Program.");
        }

        [MenuItem("Tools/3C/Characters/Compile Training Enemy AI Program")]
        public static void CompileAi()
        {
            RequireEditMode();
            AIControllerDefinition controller = RequireAsset<AIControllerDefinition>(AiDefinitionPath);
            if (!controller.ControlledCharacter.SimulationProgram || !controller.ControlledCharacter.PresentationProjection)
                throw new InvalidOperationException("Training Enemy Character Float32 products must be published before compiling AI.");
            AIIntentProgramBuildService.CompileAndPublish(controller);
            AIIntentProgramBuildService.Validate(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("Training Enemy AI Intent Program compiled and published.");
        }

        [MenuItem("Tools/3C/Characters/Build Training Enemy AI Authoring")]
        public static void BuildAiAuthoring()
        {
            RequireEditMode();
            EnsureFolders();
            BuildAiAuthoring(RequireAsset<CharacterPipelineDefinition>(DefinitionPath));
            AssetDatabase.SaveAssets();
            Debug.Log("Training Enemy independent AI authoring built.");
        }

        static CharacterInputProfile BuildInputProfile(
            InputActionAsset sourceAsset,
            InputActionReference moveReference,
            InputActionReference attackReference)
        {
            CharacterInputProfile profile = LoadOrCreate<CharacterInputProfile>(InputProfilePath, "Training Enemy Input Profile");
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("m_SourceAsset").objectReferenceValue = sourceAsset;
            SerializedProperty values = serialized.FindProperty("m_InputValues");
            values.arraySize = 1;
            SerializedProperty move = values.GetArrayElementAtIndex(0);
            move.FindPropertyRelative("m_InputValueId").stringValue = "MoveAxis";
            move.FindPropertyRelative("m_ValueType").intValue = (int)CharacterInputValueType.Vector2;
            move.FindPropertyRelative("m_Vector2ConflictPolicy").intValue = (int)CharacterVector2ConflictPolicy.LatestActuatedCardinal;
            move.FindPropertyRelative("m_SourceAction").objectReferenceValue = moveReference;
            SerializedProperty requests = serialized.FindProperty("m_ActionRequests");
            requests.arraySize = 1;
            SerializedProperty attack = requests.GetArrayElementAtIndex(0);
            attack.FindPropertyRelative("m_RequestId").stringValue = "Attack";
            attack.FindPropertyRelative("m_SourceAction").objectReferenceValue = attackReference;
            attack.FindPropertyRelative("m_BufferSeconds").floatValue = 0.2f;
            attack.FindPropertyRelative("m_Priority").intValue = 0;
            attack.FindPropertyRelative("m_TimingClass").intValue = (int)CharacterActionRequestTimingClass.Offensive;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static CharacterGameplayEffectProfile BuildGameplayEffectProfile()
        {
            CharacterGameplayEffectProfile source = RequireAsset<CharacterGameplayEffectProfile>(SourceGameplayEffectProfilePath);
            CharacterGameplayEffectProfile profile = LoadOrCreate<CharacterGameplayEffectProfile>(
                GameplayEffectProfilePath,
                "Training Enemy Gameplay Effect Profile");
            EditorUtility.CopySerialized(source, profile);
            profile.name = "TrainingEnemyGameplayEffectProfile";
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static GameplayBehaviorProfile BuildMoveBehavior()
        {
            GameplayBehaviorProfile profile = LoadOrCreate<GameplayBehaviorProfile>(
                MoveBehaviorPath,
                "Training Enemy Locomotion Move Behavior");
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("m_BehaviorId").stringValue = "Movement.Locomotion.Move";
            serialized.FindProperty("m_BehaviorKind").intValue = 1;
            serialized.FindProperty("m_DisplayName").stringValue = "Monster Locomotion Move";
            serialized.FindProperty("m_DebugCategory").stringValue = "MonsterMotion";
            serialized.FindProperty("m_Tags").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static ActionProfile BuildAttackAction()
        {
            ActionProfile profile = LoadOrCreate<ActionProfile>(AttackActionPath, "Training Enemy Attack Action");
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("m_ActionId").stringValue = "Attack";
            serialized.FindProperty("m_DisplayName").stringValue = "Monster Attack";
            serialized.FindProperty("m_DebugCategory").stringValue = "MonsterAction";
            serialized.FindProperty("m_Tags").arraySize = 0;
            ClearTagQuery(serialized.FindProperty("m_RequiredTags"));
            ClearTagQuery(serialized.FindProperty("m_BlockTags"));
            ClearTagQuery(serialized.FindProperty("m_CancelTags"));
            serialized.FindProperty("m_TargetRequirement").intValue = (int)ActionTargetRequirement.OptionalSnapshot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static void ClearTagQuery(SerializedProperty query)
        {
            query.FindPropertyRelative("m_All").arraySize = 0;
            query.FindPropertyRelative("m_Any").arraySize = 0;
            query.FindPropertyRelative("m_None").arraySize = 0;
        }

        static CharacterAnimationSequenceAsset BuildAttackSequence(UnityAnimationClip attackClip)
        {
            CharacterAnimationSequenceAsset sequence = LoadOrCreate<CharacterAnimationSequenceAsset>(
                AttackSequencePath,
                "Training Enemy Monster Attack Sequence");
            sequence.Configure(
                AuthoringIdentity.IsValid(sequence.AuthoringId) ? sequence.AuthoringId : AuthoringIdentity.Create(),
                attackClip,
                RequireAsset<CharacterAnimationRigDefinition>(AnimationRigPath),
                false,
                1f,
                RequireAsset<CharacterFootPlacementAnalysisSource>(FootAnalysisSourcePath),
                FootAnalysisIdentity,
                AnimationCurve.Linear(0f, 1f, 1f, 1f));
            EditorUtility.SetDirty(sequence);
            return sequence;
        }

        static TimelineAsset BuildAttackTimeline(CharacterAnimationSequenceAsset attackSequence)
        {
            TimelineAsset asset = LoadOrCreate<TimelineAsset>(AttackTimelinePath, "Training Enemy Attack Timeline");
            TimelineData timeline = TimelineData.CreateDefault("Training Enemy Attack");
            timeline.AddTrack(typeof(AnimationTrack));
            AnimationTrack track = timeline.Tracks.OfType<AnimationTrack>().Single();
            track.Name = "Full Body Attack";
            track.SetAnimationChannelId(new AnimationChannelId(AnimationChannel));
            BTSMTL.Timeline.AnimationClip clip = timeline.AddClip(attackSequence, track, 0) as BTSMTL.Timeline.AnimationClip ??
                throw new InvalidOperationException("Training Enemy Attack Timeline could not create an Animation Clip.");
            clip.EndFrame = Math.Max(1, Mathf.CeilToInt(attackSequence.Clip.length * TimelineUtility.FrameRate));
            timeline.Init();
            asset.SetData(timeline);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static TransitionAsset BuildAttackSource(UnityAnimationClip attackClip)
        {
            TransitionAsset source = LoadOrCreate<TransitionAsset>(AttackSourcePath, "Training Enemy Attack Animation Source");
            source.Transition = new ClipTransition { Clip = attackClip };
            if (!source.IsValid)
                throw new InvalidOperationException("Training Enemy Attack Animancer source is invalid.");
            EditorUtility.SetDirty(source);
            return source;
        }

        static void BindAttackAnimation(TimelineAsset timeline, TransitionAsset source)
        {
            CharacterAnimationPresentationProfile profile = RequireAsset<CharacterAnimationPresentationProfile>(AnimationProfilePath);
            AnimationTrack track = timeline.Data.Tracks.OfType<AnimationTrack>().Single();
            var binding = new AnimationProducerPresentationBinding();
            binding.ConfigureTimeline(
                new AnimationProducerId(timeline.Data.AuthoringId, track.AuthoringId),
                source,
                FootAnalysisIdentity);
            profile.SetProducerBindings(new[] { binding });
            var errors = new List<string>();
            if (!profile.CollectConfigurationErrors(errors))
                throw new InvalidOperationException(string.Join("\n", errors));
            EditorUtility.SetDirty(profile);
        }

        static BaseTreeAsset BuildCharacterTree(
            ActionProfile attackAction,
            ActionContextSlot attackContext,
            TimelineAsset attackTimeline)
        {
            var tree = new OneRootTree { name = "Training Enemy Character Root Tree" };
            RootNode root = CreateNode<RootNode>(tree, "Root", new Vector2(-640f, 0f));
            tree.RootGUID = root.GUID;
            ActionTargetSnapshotExposedProperty actionTarget = tree.CreateExposedProperty(typeof(ActionTargetSnapshotExposedProperty)) as ActionTargetSnapshotExposedProperty ??
                throw new InvalidOperationException("Training Enemy ActionTarget Blackboard declaration could not be created.");
            actionTarget.Name = "ActionTarget";
            actionTarget.ConfigureDeclaration(
                "ActionTarget",
                PipelineBlackboardVariableScope.Character,
                PipelineBlackboardVariableLifetime.Spawn,
                "MonsterCombat/Targeting");
            actionTarget.ConfigureInputBinding("ActionTarget");
            actionTarget.ShowOutside = true;
            actionTarget.CanEdit = false;
            PipelineBlackboardVariableReference actionTargetReference = actionTarget.CreateBlackboardReference();

            LoopNode loop = CreateNode<LoopNode>(tree, "Character Loop", new Vector2(-420f, 0f));
            loop.ConfigureAuthoring(LoopNode.StopType.None);
            ParallelNode parallel = CreateNode<ParallelNode>(tree, "Locomotion and Action", new Vector2(-180f, 0f));
            StateMachineNode locomotionMachine = CreateNode<StateMachineNode>(tree, "Monster Locomotion", new Vector2(100f, -120f));
            StateMachineNode actionMachine = CreateNode<StateMachineNode>(tree, "Monster Action", new Vector2(100f, 120f));
            tree.Link(root, loop, "Output", "Input");
            tree.Link(loop, parallel, "Output", "Input");
            BaseEdge locomotionEdge = tree.Link(parallel, locomotionMachine, "Output", "Input");
            BaseEdge actionEdge = tree.Link(parallel, actionMachine, "Output", "Input");
            locomotionEdge.SetConditionRuleGraph(BuildTrueRule("Run Monster Locomotion"));
            actionEdge.SetConditionRuleGraph(BuildTrueRule("Run Monster Action"));
            BuildLocomotionStateMachine(locomotionMachine);
            BuildActionStateMachine(actionMachine, attackAction, attackContext, attackTimeline, actionTargetReference);

            BaseTreeAsset asset = LoadOrCreate<BaseTreeAsset>(CharacterTreePath, "Training Enemy Character Root Tree");
            asset.SetTree(tree);
            InitializeGraphOwnership(tree);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static void BuildLocomotionStateMachine(StateMachineNode machine)
        {
            StateMachineGraph graph = machine.Graph;
            graph.name = "Training Enemy Locomotion State Machine";
            StateNode state = graph.StateNodes.Single();
            state.DisplayName = "Locomotion";
            StateBehaviorSubTree behavior = RequireStateBehavior(state);
            behavior.name = "Training Enemy Locomotion";
            RootNode root = behavior.Nodes.OfType<RootNode>().Single();
            CharacterInputVector2InfoNode input = CreateNode<CharacterInputVector2InfoNode>(behavior, "MoveAxis", new Vector2(0f, -100f));
            input.BindInputValue("MoveAxis");
            LocomotionInputMotionNode motion = CreateNode<LocomotionInputMotionNode>(behavior, "Monster Locomotion Motion", new Vector2(260f, 0f));
            motion.ConfigureAuthoring(
                4.5f,
                LocomotionInputMotionDisplacementMode.ConstantSpeed,
                null,
                720f,
                false,
                LocomotionInputMotionExecutionMode.Continuous,
                0f);
            behavior.LinkProperty(input, motion, input.PropertyPortMap["m_Output"], motion.PropertyPortMap["m_MoveInput"]);
            behavior.Link(root, motion, "Output", "Input");
        }

        static void BuildActionStateMachine(
            StateMachineNode machine,
            ActionProfile attackAction,
            ActionContextSlot attackContext,
            TimelineAsset attackTimeline,
            PipelineBlackboardVariableReference actionTarget)
        {
            StateMachineGraph graph = machine.Graph;
            graph.name = "Training Enemy Action State Machine";
            StateNode idle = graph.StateNodes.Single();
            idle.DisplayName = "No Action";
            StateNode attack = CreateNode<StateNode>(graph, "Attack", new Vector2(180f, 0f));
            BaseEdge enterAttack = graph.Link(idle, attack, StateMachinePorts.StateOut, StateMachinePorts.StateIn);
            BaseEdge exitAttack = graph.Link(attack, idle, StateMachinePorts.StateOut, StateMachinePorts.StateIn);
            enterAttack.SetConditionRuleGraph(BuildAttackEntryRule(attackAction, actionTarget));
            exitAttack.SetConditionRuleGraph(BuildStateCompletedRule());
            BuildAttackState(attack, attackAction, attackContext, attackTimeline, actionTarget);
        }

        static ConditionRuleGraph BuildAttackEntryRule(
            ActionProfile attackAction,
            PipelineBlackboardVariableReference actionTarget)
        {
            ConditionRuleGraph graph = ConditionRuleGraph.CreateDefaultGraph("Enter Monster Attack");
            CharacterActionRequestInfoNode request = CreateNode<CharacterActionRequestInfoNode>(graph, "Has Attack Request", new Vector2(-420f, -60f));
            request.BindActionRequest("Attack");
            CanActivateActionInfoNode canActivate = CreateNode<CanActivateActionInfoNode>(graph, "Can Activate Attack", new Vector2(-420f, 60f));
            canActivate.ConfigureAuthoring(attackAction, actionTarget);
            AndNode and = CreateNode<AndNode>(graph, "Attack Requested and Allowed", new Vector2(-120f, 0f));
            graph.LinkProperty(request, and, request.PropertyPortMap["m_Output"], and.PropertyPortMap["m_Input1"]);
            graph.LinkProperty(canActivate, and, canActivate.PropertyPortMap["m_Output"], and.PropertyPortMap["m_Input2"]);
            ConnectConditionResult(graph, and, "m_Output");
            return graph;
        }

        static ConditionRuleGraph BuildStateCompletedRule()
        {
            ConditionRuleGraph graph = ConditionRuleGraph.CreateDefaultGraph("Monster Attack Completed");
            StateRootCompletedNode completed = CreateNode<StateRootCompletedNode>(graph, "Attack Root Completed", new Vector2(-180f, 0f));
            ConnectConditionResult(graph, completed, "m_Output");
            return graph;
        }

        static void BuildAttackState(
            StateNode state,
            ActionProfile attackAction,
            ActionContextSlot attackContext,
            TimelineAsset attackTimeline,
            PipelineBlackboardVariableReference actionTarget)
        {
            StateBehaviorSubTree behavior = RequireStateBehavior(state);
            behavior.name = "Training Enemy Attack State";
            RootNode root = behavior.Nodes.OfType<RootNode>().Single();
            SequenceNode sequence = CreateNode<SequenceNode>(behavior, "Attack Playback", new Vector2(220f, -100f));
            ActivateActionInstanceNode activate = CreateNode<ActivateActionInstanceNode>(behavior, "Activate Monster Attack", new Vector2(500f, -220f));
            activate.ConfigureAuthoring(attackAction, "Attack", true, attackContext, string.Empty, actionTarget);
            TimelineNode timeline = CreateNode<TimelineNode>(behavior, "Play Monster Attack", new Vector2(500f, -100f));
            timeline.ConfigureSharedAuthoring(attackTimeline, attackContext, TimelinePlaybackMode.Once);
            behavior.Link(root, sequence, "Output", "Input");
            BaseEdge activateEdge = behavior.Link(sequence, activate, "Output", "Input");
            BaseEdge timelineEdge = behavior.Link(sequence, timeline, "Output", "Input");
            activateEdge.SetConditionRuleGraph(BuildTrueRule("Activate Monster Attack"));
            timelineEdge.SetConditionRuleGraph(BuildTrueRule("Play Monster Attack Timeline"));
            BuildActionExitLifecycle(behavior, attackContext);
        }

        static void BuildActionExitLifecycle(StateBehaviorSubTree behavior, ActionContextSlot context)
        {
            StateOnExitNode onExit = behavior.Nodes.OfType<StateOnExitNode>().Single();
            SelectorNode selector = CreateNode<SelectorNode>(behavior, "Action Exit", new Vector2(220f, 180f));
            SubmitActionLifecycleTransitionNode cancel = BuildLifecycleNode(behavior, context, ActionLifecycleTransitionType.Cancel, "WindowCancel", new Vector2(520f, 20f));
            SubmitActionLifecycleTransitionNode interrupt = BuildLifecycleNode(behavior, context, ActionLifecycleTransitionType.Interrupt, "TreeInterrupt", new Vector2(520f, 100f));
            SubmitActionLifecycleTransitionNode abort = BuildLifecycleNode(behavior, context, ActionLifecycleTransitionType.Abort, "TreeAbort", new Vector2(520f, 180f));
            SubmitActionLifecycleTransitionNode complete = BuildLifecycleNode(behavior, context, ActionLifecycleTransitionType.Complete, "TimelineCompleted", new Vector2(520f, 260f));
            SucceedNode succeed = CreateNode<SucceedNode>(behavior, "Succeed", new Vector2(520f, 340f));
            behavior.Link(onExit, selector, "Output", "Input");
            BaseEdge cancelEdge = behavior.Link(selector, cancel, "Output", "Input");
            BaseEdge interruptEdge = behavior.Link(selector, interrupt, "Output", "Input");
            BaseEdge abortEdge = behavior.Link(selector, abort, "Output", "Input");
            BaseEdge completeEdge = behavior.Link(selector, complete, "Output", "Input");
            BaseEdge succeedEdge = behavior.Link(selector, succeed, "Output", "Input");
            cancelEdge.SetConditionRuleGraph(BuildFalseRule("Monster Attack Cancel"));
            interruptEdge.SetConditionRuleGraph(BuildExitCauseRule("Monster Attack Interrupt", context, StateExitCause.TreeSelfAbort, StateExitCause.TreeLowerPriorityAbort));
            abortEdge.SetConditionRuleGraph(BuildExitCauseRule("Monster Attack Abort", context, StateExitCause.TreeParentStop));
            completeEdge.SetConditionRuleGraph(BuildExitCauseRule("Monster Attack Complete", context, StateExitCause.StateTransition));
            succeedEdge.SetConditionRuleGraph(BuildTrueRule("Finish Monster Attack Exit"));
            selector.OrderChildren();
        }

        static SubmitActionLifecycleTransitionNode BuildLifecycleNode(
            StateBehaviorSubTree graph,
            ActionContextSlot context,
            ActionLifecycleTransitionType type,
            string reason,
            Vector2 position)
        {
            SubmitActionLifecycleTransitionNode node = CreateNode<SubmitActionLifecycleTransitionNode>(graph, "Submit " + type, position);
            node.ConfigureAuthoring(context, type, reason);
            return node;
        }

        static ConditionRuleGraph BuildFalseRule(string name)
        {
            ConditionRuleGraph graph = ConditionRuleGraph.CreateDefaultGraph(name);
            graph.ResultNode.SetDefaultResult(false);
            return graph;
        }

        static ConditionRuleGraph BuildTrueRule(string name)
        {
            ConditionRuleGraph graph = ConditionRuleGraph.CreateDefaultGraph(name);
            graph.ResultNode.SetDefaultResult(true);
            return graph;
        }

        static ConditionRuleGraph BuildExitCauseRule(
            string name,
            ActionContextSlot context,
            params StateExitCause[] causes)
        {
            ConditionRuleGraph graph = ConditionRuleGraph.CreateDefaultGraph(name);
            ActionContextActiveInfoNode active = CreateNode<ActionContextActiveInfoNode>(graph, "Action Context Active", new Vector2(-480f, -80f));
            active.ConfigureAuthoring(context);
            BaseNode causeOutput;
            string causePort;
            if (causes.Length == 1)
            {
                StateExitCauseInfoNode cause = CreateNode<StateExitCauseInfoNode>(graph, causes[0].ToString(), new Vector2(-480f, 80f));
                cause.ConfigureAuthoring(causes[0]);
                causeOutput = cause;
                causePort = "m_Output";
            }
            else
            {
                StateExitCauseInfoNode first = CreateNode<StateExitCauseInfoNode>(graph, causes[0].ToString(), new Vector2(-520f, 40f));
                StateExitCauseInfoNode second = CreateNode<StateExitCauseInfoNode>(graph, causes[1].ToString(), new Vector2(-520f, 120f));
                first.ConfigureAuthoring(causes[0]);
                second.ConfigureAuthoring(causes[1]);
                OrNode or = CreateNode<OrNode>(graph, "Interrupt Exit Cause", new Vector2(-280f, 80f));
                graph.LinkProperty(first, or, first.PropertyPortMap["m_Output"], or.PropertyPortMap["m_Input1"]);
                graph.LinkProperty(second, or, second.PropertyPortMap["m_Output"], or.PropertyPortMap["m_Input2"]);
                causeOutput = or;
                causePort = "m_Output";
            }
            AndNode and = CreateNode<AndNode>(graph, "Active Action Exit", new Vector2(0f, 0f));
            graph.LinkProperty(active, and, active.PropertyPortMap["m_Output"], and.PropertyPortMap["m_Input1"]);
            graph.LinkProperty(causeOutput, and, causeOutput.PropertyPortMap[causePort], and.PropertyPortMap["m_Input2"]);
            ConnectConditionResult(graph, and, "m_Output");
            return graph;
        }

        static void ConnectConditionResult(ConditionRuleGraph graph, BaseNode source, string sourcePort)
        {
            graph.ResultNode.SetDefaultResult(false);
            graph.LinkProperty(
                source,
                graph.ResultNode,
                source.PropertyPortMap[sourcePort],
                graph.ResultNode.PropertyPortMap["m_Result"]);
        }

        static CharacterPipelineDefinition BuildDefinition(
            BaseTreeAsset tree,
            CharacterInputProfile input,
            CharacterGameplayEffectProfile gameplayEffects,
            CharacterBodyMotionProfile bodyMotion,
            ActionProfile attack,
            GameplayBehaviorProfile moveBehavior)
        {
            CharacterPipelineDefinition definition = LoadOrCreate<CharacterPipelineDefinition>(
                DefinitionPath,
                "Training Enemy Character Pipeline Definition");
            CharacterAnimationPresentationProfile animationProfile = RequireAsset<CharacterAnimationPresentationProfile>(AnimationProfilePath);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("m_RootTreeAsset").objectReferenceValue = tree;
            serialized.FindProperty("m_SimulationTickRate").intValue = 60;
            serialized.FindProperty("m_InputProfile").objectReferenceValue = input;
            serialized.FindProperty("m_GameplayEffectProfile").objectReferenceValue = gameplayEffects;
            serialized.FindProperty("m_BodyMotionProfile").objectReferenceValue = bodyMotion;
            serialized.FindProperty("m_AnimationPresentationProfile").objectReferenceValue = animationProfile;
            serialized.FindProperty("m_EquipmentCapabilityEnabled").boolValue = false;
            serialized.FindProperty("m_EquipmentProfile").objectReferenceValue = null;
            serialized.FindProperty("m_EquipmentPresentationProfile").objectReferenceValue = null;
            SerializedProperty actions = serialized.FindProperty("m_ActionProfiles");
            actions.arraySize = 1;
            actions.GetArrayElementAtIndex(0).objectReferenceValue = attack;
            SerializedProperty behaviors = serialized.FindProperty("m_BehaviorProfiles");
            behaviors.arraySize = 1;
            behaviors.GetArrayElementAtIndex(0).objectReferenceValue = moveBehavior;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        static void BuildAiAuthoring(CharacterPipelineDefinition definition)
        {
            AIControllerTree tree = BuildAiTree();
            BaseTreeAsset treeAsset = LoadOrCreate<BaseTreeAsset>(AiRootPath, "Training Enemy AI Root Tree");
            treeAsset.SetTree(tree);
            InitializeGraphOwnership(tree);
            EditorUtility.SetDirty(treeAsset);

            AIPerceptionProfile perception = LoadOrCreate<AIPerceptionProfile>(AiPerceptionPath, "Training Enemy AI Perception");
            perception.ConfigureAuthoring(new[] { "gameplay-lab-player" }, AICandidateOrdering.DistanceThenActorId);
            EditorUtility.SetDirty(perception);
            AIControllerDefinition controller = LoadOrCreate<AIControllerDefinition>(AiDefinitionPath, "Training Enemy AI Controller Definition");
            controller.ConfigureAuthoring("TrainingEnemy.MonsterAI", treeAsset, definition, perception);
            EditorUtility.SetDirty(controller);
        }

        static AIControllerTree BuildAiTree()
        {
            var tree = new AIControllerTree { name = "Training Enemy Monster AI" };
            tree.CheckInit();
            RootNode root = tree.Nodes.OfType<RootNode>().Single();
            AIActionTargetSnapshotExposedProperty currentTarget = CreateAiBlackboard<AIActionTargetSnapshotExposedProperty>(
                tree,
                "CurrentTarget",
                PipelineBlackboardVariableScope.AIController,
                default(AIActionTargetSnapshotValue));
            FloatExposedProperty attackRange = CreateAiBlackboard<FloatExposedProperty>(
                tree,
                "AttackRange",
                PipelineBlackboardVariableScope.AIController,
                2.5f);
            IntExposedProperty attackCooldown = CreateAiBlackboard<IntExposedProperty>(
                tree,
                "AttackCooldownTicks",
                PipelineBlackboardVariableScope.AIController,
                36);
            Vector2ExposedProperty stopMove = CreateAiBlackboard<Vector2ExposedProperty>(
                tree,
                "StopMove",
                PipelineBlackboardVariableScope.Graph,
                Vector2.zero);

            LoopNode loop = CreateNode<LoopNode>(tree, "Monster Decision Loop", new Vector2(-760f, 0f));
            loop.ConfigureAuthoring(LoopNode.StopType.None);
            SequenceNode observe = CreateNode<SequenceNode>(tree, "Observe Local Actor", new Vector2(-520f, 0f));
            SelectNearestCandidateNode selectTarget = CreateNode<SelectNearestCandidateNode>(tree, "Select Nearest Local Actor", new Vector2(-260f, -180f));
            ReadSelectedTargetSnapshotNode selectedTarget = CreateNode<ReadSelectedTargetSnapshotNode>(tree, "Read Selected Target", new Vector2(-260f, 40f));
            WriteAIMemoryNode rememberTarget = CreateNode<WriteAIMemoryNode>(tree, "Remember Current Target", new Vector2(0f, -100f));
            rememberTarget.ConfigureAuthoring(currentTarget, AIMemoryValueKind.ActionTargetSnapshot);
            WriteActionTargetSnapshotNode writeActionTarget = CreateNode<WriteActionTargetSnapshotNode>(tree, "Write Character Action Target", new Vector2(240f, -100f));
            writeActionTarget.ConfigureInput("ActionTarget");
            SelectorNode decide = CreateNode<SelectorNode>(tree, "Chase or Attack", new Vector2(500f, 0f));

            SequenceNode chase = CreateNode<SequenceNode>(tree, "Chase Target", new Vector2(760f, -140f));
            ReadTargetDirectionNode targetDirection = CreateNode<ReadTargetDirectionNode>(tree, "Read Target Direction", new Vector2(760f, -340f));
            WriteContinuousInputNode writeMove = CreateNode<WriteContinuousInputNode>(tree, "Write Monster Move", new Vector2(1020f, -140f));
            writeMove.ConfigureInput("MoveAxis", typeof(Vector2PropertyPort));

            SequenceNode attack = CreateNode<SequenceNode>(tree, "Attack Target", new Vector2(760f, 160f));
            ReadAIMemoryNode readStopMove = CreateNode<ReadAIMemoryNode>(tree, "Read Stop Move", new Vector2(760f, 360f));
            readStopMove.ConfigureAuthoring(stopMove, AIMemoryValueKind.Vector2);
            WriteContinuousInputNode stopMovement = CreateNode<WriteContinuousInputNode>(tree, "Stop Before Attack", new Vector2(1020f, 80f));
            stopMovement.ConfigureInput("MoveAxis", typeof(Vector2PropertyPort));
            SubmitActionRequestNode submitAttack = CreateNode<SubmitActionRequestNode>(tree, "Submit Monster Attack", new Vector2(1020f, 180f));
            submitAttack.ConfigureRequest("Attack", 0.2f, 0, AIRequestRepeatPolicy.OncePerActivation);
            ReadAIMemoryNode readCooldown = CreateNode<ReadAIMemoryNode>(tree, "Read Attack Cooldown", new Vector2(760f, 460f));
            readCooldown.ConfigureAuthoring(attackCooldown, AIMemoryValueKind.Integer);
            AIWaitTicksNode waitCooldown = CreateNode<AIWaitTicksNode>(tree, "Wait Attack Cooldown", new Vector2(1020f, 280f));

            tree.Link(root, loop, "Output", "Input");
            tree.Link(loop, observe, "Output", "Input");
            LinkCompositeChild(tree, observe, selectTarget, 0);
            LinkCompositeChild(tree, observe, rememberTarget, 1);
            LinkCompositeChild(tree, observe, writeActionTarget, 2);
            LinkCompositeChild(tree, observe, decide, 3);
            BaseEdge chaseEdge = LinkCompositeChild(tree, decide, chase, 0);
            chaseEdge.AbortPolicy = BTAbortPolicy.Both;
            chaseEdge.SetConditionRuleGraph(BuildTargetRangeRule("Target Outside Attack Range", attackRange, CompareNode.CompareType.Greater));
            BaseEdge attackEdge = LinkCompositeChild(tree, decide, attack, 1);
            attackEdge.AbortPolicy = BTAbortPolicy.Self;
            attackEdge.SetConditionRuleGraph(BuildTargetRangeRule("Target Inside Attack Range", attackRange, CompareNode.CompareType.LessEqual));
            LinkCompositeChild(tree, chase, writeMove, 0);
            LinkCompositeChild(tree, attack, stopMovement, 0);
            LinkCompositeChild(tree, attack, submitAttack, 1);
            LinkCompositeChild(tree, attack, waitCooldown, 2);

            tree.LinkProperty(selectedTarget, rememberTarget, selectedTarget.PropertyPortMap["m_Target"], rememberTarget.PropertyPortMap["m_Value"]);
            tree.LinkProperty(targetDirection, writeMove, targetDirection.PropertyPortMap["m_Direction"], writeMove.PropertyPortMap["m_Value"]);
            tree.LinkProperty(readStopMove, stopMovement, readStopMove.PropertyPortMap["m_Value"], stopMovement.PropertyPortMap["m_Value"]);
            tree.LinkProperty(readCooldown, waitCooldown, readCooldown.PropertyPortMap["m_Value"], waitCooldown.PropertyPortMap["m_Ticks"]);
            observe.OrderChildren();
            decide.OrderChildren();
            chase.OrderChildren();
            attack.OrderChildren();
            return tree;
        }

        static T CreateAiBlackboard<T>(
            AIControllerTree tree,
            string key,
            PipelineBlackboardVariableScope scope,
            object value)
            where T : BaseExposedProperty
        {
            T declaration = tree.CreateExposedProperty(typeof(T)) as T ??
                throw new InvalidOperationException($"Training Enemy AI Blackboard declaration '{key}' could not be created.");
            declaration.Name = key;
            declaration.ConfigureDeclaration(
                key,
                scope,
                PipelineBlackboardVariablePolicy.DefaultLifetime(scope),
                "MonsterAI");
            declaration.SetValue(value);
            return declaration;
        }

        static BaseEdge LinkCompositeChild(BaseTree tree, CompositeNode parent, RunnableNode child, int order)
        {
            BaseEdge edge = tree.Link(parent, child, "Output", "Input") ??
                throw new InvalidOperationException($"Training Enemy AI could not link '{parent.ResolvedDisplayName}' to '{child.ResolvedDisplayName}'.");
            edge.FlowOrder = order;
            edge.ConditionRuleGraph.ResultNode.SetDefaultResult(true);
            return edge;
        }

        static ConditionRuleGraph BuildTargetRangeRule(
            string name,
            FloatExposedProperty attackRange,
            CompareNode.CompareType comparison)
        {
            ConditionRuleGraph graph = ConditionRuleGraph.CreateDefaultGraph(name, GraphAuthoringRole.AIController);
            ReadTargetDistanceNode distance = CreateNode<ReadTargetDistanceNode>(graph, "Target Distance", new Vector2(-440f, -80f));
            ReadAIMemoryNode range = CreateNode<ReadAIMemoryNode>(graph, "Attack Range", new Vector2(-440f, 80f));
            range.ConfigureAuthoring(attackRange, AIMemoryValueKind.Scalar);
            CompareNode compare = CreateNode<CompareNode>(graph, "Compare Target Distance", new Vector2(-140f, 0f));
            compare.ConfigureAuthoring(comparison);
            compare.SetPropertyPort("m_InputValue1", typeof(FloatPropertyPort), PortDirection.Input);
            compare.SetPropertyPort("m_InputValue2", typeof(FloatPropertyPort), PortDirection.Input);
            graph.LinkProperty(distance, compare, distance.PropertyPortMap["m_Distance"], compare.PropertyPortMap["m_InputValue1"]);
            graph.LinkProperty(range, compare, range.PropertyPortMap["m_Value"], compare.PropertyPortMap["m_InputValue2"]);
            ConnectConditionResult(graph, compare, "m_Result");
            return graph;
        }

        static InputActionReference BuildInputReference(string path, string name, InputAction action)
        {
            if (action == null)
                throw new InvalidOperationException($"Input Action for '{name}' is missing.");
            InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
            if (!reference)
            {
                reference = ScriptableObject.CreateInstance<InputActionReference>();
                reference.name = name;
                AssetDatabase.CreateAsset(reference, path);
            }
            reference.Set(action);
            reference.name = name;
            EditorUtility.SetDirty(reference);
            return reference;
        }

        static Dictionary<string, UnityAnimationClip> LoadMonsterClips()
        {
            var clips = new Dictionary<string, UnityAnimationClip>(StringComparer.Ordinal);
            foreach (string clipName in TrainingEnemyAnimationAssetAuthoring.RequiredClipNames)
            {
                string path = MonsterClipRootPath + "/" + clipName + ".anim";
                UnityAnimationClip clip = AssetDatabase.LoadAssetAtPath<UnityAnimationClip>(path) ??
                    throw new InvalidOperationException($"Training Enemy published AnimationClip is missing: {path}");
                clips.Add(clipName, clip);
            }
            return clips;
        }

        static StateBehaviorSubTree RequireStateBehavior(StateNode state) =>
            state.SubTree as StateBehaviorSubTree ??
            throw new InvalidOperationException($"State '{state.ResolvedDisplayName}' has no StateBehaviorSubTree.");

        static void InitializeGraphOwnership(BaseTree root)
        {
            var visited = new HashSet<BaseTree>();
            InitializeGraphOwnership(root, visited);
        }

        static void InitializeGraphOwnership(BaseTree graph, HashSet<BaseTree> visited)
        {
            if (graph == null || !visited.Add(graph))
                return;
            graph.CheckInit();
            foreach (BaseNode node in graph.Nodes.Where(value => value != null))
            {
                foreach (NodeGraphReference reference in node.GetGraphReferences())
                    InitializeGraphOwnership(reference.Tree, visited);
            }
            foreach (BaseEdge edge in graph.Edges.Where(value => value != null))
                InitializeGraphOwnership(edge.ConditionRuleGraph, visited);
        }

        static T CreateNode<T>(BaseGraph graph, string displayName, Vector2 position) where T : BaseNode
        {
            T node = graph.CreateNode(typeof(T)) as T ??
                throw new InvalidOperationException($"{graph.name} could not create {typeof(T).Name}.");
            node.DisplayName = displayName;
            node.Position = position;
            return node;
        }

        static T RequireAsset<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException($"Required asset is missing: {path}");

        static T LoadOrCreate<T>(string path, string name) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Training Enemy authoring cannot run in Play Mode.");
        }

        static void EnsureFolders()
        {
            foreach (string path in new[]
                     {
                         RootPath,
                         RootPath + "/Definition",
                         RootPath + "/Graphs",
                         RootPath + "/Graphs/Timelines",
                         RootPath + "/Input",
                         RootPath + "/Input/References",
                         RootPath + "/GameplayEffect",
                         RootPath + "/Motion",
                         RootPath + "/Behavior",
                         RootPath + "/Actions",
                         RootPath + "/Actions/Attack",
                         RootPath + "/AI",
                         RootPath + "/AI/Authoring",
                         "Assets/Configs/Character/TrainingEnemy/Presentation/Sources"
                     })
                EnsureFolder(path);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException($"Invalid asset folder '{path}'.");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
