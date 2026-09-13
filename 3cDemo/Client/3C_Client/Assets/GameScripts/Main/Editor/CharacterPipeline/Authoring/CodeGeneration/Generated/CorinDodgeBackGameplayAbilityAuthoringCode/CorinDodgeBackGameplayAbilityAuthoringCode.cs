using System;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;
using TimelineAnimationClip = BTSMTL.Timeline.AnimationClip;
using UnityObject = UnityEngine.Object;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;
using ThirdPersonCamera;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinDodgeBackGameplayAbilityAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var generation = new GenerationState();
            LoadRootResources(generation, context);
            LoadTimelines_CorinDodgeBackTimelineResources(generation, context);
            BuildCreateRoot0(generation, context);
            BuildCreateStages_DodgeBack1(generation, context);
            BuildCreateConditions_DodgeBack_State_Body_StateRootCompleted2(generation, context);
            BuildCreateRoot3(generation, context);
            BuildCreateStages_DodgeBack4(generation, context);
            BuildCreateConditions_DodgeBack_State_Body_StateRootCompleted5(generation, context);
            BuildCreateRoot6(generation, context);
            BuildCreateTimelines_CorinDodgeBackTimeline7(generation, context);
            BuildCreateRoot8(generation, context);
            BuildCreateTimelines_CorinDodgeBackTimeline9(generation, context);
            BuildConfigureRoot0(generation, context);
            BuildConfigureStages_DodgeBack1(generation, context);
            BuildConfigureTimelines_CorinDodgeBackTimeline2(generation, context);
            BuildBindRoot0(generation, context);
            BuildBindTimelines_CorinDodgeBackTimeline1(generation, context);
            BuildBindStages_DodgeBack2(generation, context);
            BuildConnectRoot0(generation, context);
            BuildConnectStages_DodgeBack1(generation, context);
            BuildConnectConditions_DodgeBack_State_Body_StateRootCompleted2(generation, context);
            BuildConnectRoot3(generation, context);
            BuildRootBindingRoot0(generation, context);
            BuildRootBindingStages_DodgeBack1(generation, context);
            BuildRootBindingConditions_DodgeBack_State_Body_StateRootCompleted2(generation, context);
            BuildRootBindingRoot3(generation, context);
            BuildRootBindingStages_DodgeBack4(generation, context);
            BuildRootBindingConditions_DodgeBack_State_Body_StateRootCompleted5(generation, context);
            BuildRootBindingTimelines_CorinDodgeBackTimeline6(generation, context);
            BuildRootBindingRoot7(generation, context);
            return context.Complete(generation.graph);
        }

        static void LoadRootResources(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.asset = context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>("Assets/Configs/Character/Corin/Pipeline/Actions/Dodge/CorinDodgeActionProfile.asset", 11400000L);
        }

        static void BuildCreateRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph = BtsmtlSkillAuthoringCode.EnsureAbilityRoot(context, "DodgeBack", "5d6c5b40e613e36a79c647152961ea33", "DodgeBack");
        }

        static void BuildCreateRoot3(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph, typeof(BtsmtlSkillRootFlowNode), "37772f70-8c7f-4126-ac64-e32fcca47890", "技能入口", new Vector2(-354.6667f, 6f));
            generation.node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph, typeof(BtsmtlSkillStateMachineFlowNode), "c5122005-2d41-4e69-93e9-e6b33f6afc61", "DodgeBack StateMachine", new Vector2(260f, 6f));
        }

        static void BuildCreateRoot6(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.stateMachine = BtsmtlSkillAuthoringCode.EnsureStateMachine(generation.graph, "8d9a3eed-127b-4405-9e67-6f33d5d3d3bf", "DodgeBack StateMachine", "5d6c5b40e613e36a79c647152961ea33", "c5122005-2d41-4e69-93e9-e6b33f6afc61");
        }

        static void BuildCreateRoot8(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.state2 = BtsmtlSkillAuthoringCode.EnsureNativeState(generation.stateMachine, typeof(BtsmtlSkillNativeExitState), "00b9a83c-948d-4896-ac17-2a2404189e07", "状态机出口", new Vector2(521.3334f, 124.0001f));
            generation.state = BtsmtlSkillAuthoringCode.EnsureNativeState(generation.stateMachine, typeof(BtsmtlSkillNativeEntryState), "1fa0f321-eba5-4e94-aa38-822d84de055e", "技能入口", new Vector2(-260f, 0f));
            generation.state3 = BtsmtlSkillAuthoringCode.EnsureNativeState(generation.stateMachine, typeof(BtsmtlSkillNativeAnyState), "6d7e8b1d-4e9c-4c84-a1e0-8d7f0e4c04c5", "任意状态", new Vector2(0f, 240f));
            generation.state1 = BtsmtlSkillAuthoringCode.EnsureNativeState(generation.stateMachine, typeof(BtsmtlSkillNativeState), "e376c542-fd18-4a79-bd6c-df249bc626e0", "DodgeBack", new Vector2(41.49386f, 35.21167f));
        }

        static void BuildConfigureRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            var endRule = new GameplayAbilityEndRule();
            endRule.Configure(GameplayAbilityEndTrigger.AbortRequested, ActionLifecycleTransitionType.Abort, "", "TreeAbort");
            var endRule1 = new GameplayAbilityEndRule();
            endRule1.Configure(GameplayAbilityEndTrigger.InterruptRequested, ActionLifecycleTransitionType.Interrupt, "", "TreeInterrupt");
            var endRule2 = new GameplayAbilityEndRule();
            endRule2.Configure(GameplayAbilityEndTrigger.ActionWindowClosed, ActionLifecycleTransitionType.Cancel, "RecoveryOpen", "RecoveryCancel");
            var endRule3 = new GameplayAbilityEndRule();
            endRule3.Configure(GameplayAbilityEndTrigger.ExecutionCompleted, ActionLifecycleTransitionType.Complete, "", "TimelineCompleted");
            BtsmtlSkillAuthoringCode.ConfigureAbility(context, "", Array.Empty<GameplayTagId>(), generation.asset, Array.Empty<GameplayEffectDefinition>(), new[] { endRule, endRule1, endRule2, endRule3 }, Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>(), Array.Empty<string>(), "Dodge", true, "", "", false);
        }

        static void BuildBindRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillStateMachineFlowNode)generation.node1).SetStateMachine(generation.stateMachine);
        }

        static void BuildConnectRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph, generation.node, "Output", generation.node1, "Input", "9e42a23e-40ab-4c4f-9680-92be8536ccfa");
        }

        static void BuildConnectRoot3(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.stateEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(generation.stateMachine, (BtsmtlSkillNativeState)generation.state, (BtsmtlSkillNativeState)generation.state1, "027effc7-ec6b-453c-90a5-cedb9418730a");
            generation.stateEdge1 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(generation.stateMachine, (BtsmtlSkillNativeState)generation.state1, (BtsmtlSkillNativeState)generation.state2, "56b68cd3-0249-4f8f-9af2-06c451584b84");
            BtsmtlSkillAuthoringCode.ConfigureNativeConnection((BtsmtlSkillNativeConnection)generation.stateEdge1, generation.graph4, 0, ProgramAbortPolicy.None, 0);
        }

        static void BuildRootBindingRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph, new[] { "37772f70-8c7f-4126-ac64-e32fcca47890", "c5122005-2d41-4e69-93e9-e6b33f6afc61" }, new[] { "9e42a23e-40ab-4c4f-9680-92be8536ccfa" });
        }

        static void BuildRootBindingRoot3(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneNativeStateMachine(generation.stateMachine, new[] { "e376c542-fd18-4a79-bd6c-df249bc626e0" }, new[] { "027effc7-ec6b-453c-90a5-cedb9418730a", "56b68cd3-0249-4f8f-9af2-06c451584b84" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph, Array.Empty<string>());
        }

        static void BuildRootBindingRoot7(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.BindAbilityRoot(context, generation.graph);
        }

        sealed class GenerationState
        {
            internal BtsmtlSkillFlowGraph graph;
            internal FlowNode node;
            internal BinderConnection edge;
            internal FlowNode node1;
            internal BtsmtlSkillFlowGraph graph1;
            internal FlowNode node2;
            internal FlowNode node3;
            internal BinderConnection edge1;
            internal FlowNode node4;
            internal FlowNode node5;
            internal BtsmtlSkillFlowGraph graph2;
            internal FlowNode node6;
            internal FlowNode node7;
            internal BinderConnection edge2;
            internal FlowNode node8;
            internal FlowNode node9;
            internal FlowNode node10;
            internal BtsmtlSkillFlowGraph graph3;
            internal FlowNode node11;
            internal FlowNode node12;
            internal BinderConnection edge3;
            internal FlowNode node13;
            internal FlowNode node14;
            internal FlowNode node15;
            internal BtsmtlSkillFlowGraph graph4;
            internal FlowNode node16;
            internal BinderConnection edge4;
            internal FlowNode node17;
            internal BtsmtlSkillNativeStateMachine stateMachine;
            internal BtsmtlSkillNativeState state;
            internal BtsmtlSkillNativeConnection stateEdge;
            internal BtsmtlSkillNativeState state1;
            internal BtsmtlSkillNativeConnection stateEdge1;
            internal BtsmtlSkillNativeState state2;
            internal BtsmtlSkillNativeState state3;
            internal TimelineAsset timeline;
            internal TimelineData timelineData;
            internal Track track;
            internal Clip clip;
            internal Track track1;
            internal Clip clip1;
            internal Track track2;
            internal Clip clip2;
            internal Clip clip3;
            internal GameplayAbilityAdmissionProfile asset;
            internal UnityAnimationClip asset1;
            internal RootMotionCurveAsset asset2;
        }
    }
}
