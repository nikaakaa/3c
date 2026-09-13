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
    public sealed partial class CorinDodgeForwardGameplayAbilityAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var generation = new GenerationState();
            LoadRootResources(generation, context);
            LoadTimelines_CorinDodgeForwardTimelineResources(generation, context);
            BuildCreateRoot0(generation, context);
            BuildCreateStages_DodgeForward1(generation, context);
            BuildCreateConditions_DodgeForward_State_Body_StateRootCompleted2(generation, context);
            BuildCreateRoot3(generation, context);
            BuildCreateStages_DodgeForward4(generation, context);
            BuildCreateConditions_DodgeForward_State_Body_StateRootCompleted5(generation, context);
            BuildCreateRoot6(generation, context);
            BuildCreateTimelines_CorinDodgeForwardTimeline7(generation, context);
            BuildCreateRoot8(generation, context);
            BuildCreateTimelines_CorinDodgeForwardTimeline9(generation, context);
            BuildConfigureRoot0(generation, context);
            BuildConfigureStages_DodgeForward1(generation, context);
            BuildConfigureTimelines_CorinDodgeForwardTimeline2(generation, context);
            BuildBindRoot0(generation, context);
            BuildBindTimelines_CorinDodgeForwardTimeline1(generation, context);
            BuildBindStages_DodgeForward2(generation, context);
            BuildConnectRoot0(generation, context);
            BuildConnectStages_DodgeForward1(generation, context);
            BuildConnectConditions_DodgeForward_State_Body_StateRootCompleted2(generation, context);
            BuildConnectRoot3(generation, context);
            BuildRootBindingRoot0(generation, context);
            BuildRootBindingStages_DodgeForward1(generation, context);
            BuildRootBindingConditions_DodgeForward_State_Body_StateRootCompleted2(generation, context);
            BuildRootBindingRoot3(generation, context);
            BuildRootBindingStages_DodgeForward4(generation, context);
            BuildRootBindingConditions_DodgeForward_State_Body_StateRootCompleted5(generation, context);
            BuildRootBindingTimelines_CorinDodgeForwardTimeline6(generation, context);
            BuildRootBindingRoot7(generation, context);
            return context.Complete(generation.graph);
        }

        static void LoadRootResources(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.asset = context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>("Assets/Configs/Character/Corin/Pipeline/Actions/Dodge/CorinDodgeActionProfile.asset", 11400000L);
        }

        static void BuildCreateRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph = BtsmtlSkillAuthoringCode.EnsureAbilityRoot(context, "DodgeForward", "12fd389b09dc017facc9250c3f9a4ff3", "DodgeForward");
        }

        static void BuildCreateRoot3(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph, typeof(BtsmtlSkillStateMachineFlowNode), "47098806-a9db-43fb-894a-783c38491e8c", "DodgeForward StateMachine", new Vector2(260f, 84f));
            generation.node = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph, typeof(BtsmtlSkillRootFlowNode), "a7c0040e-35e8-44d0-9de6-2df949cd76a2", "技能入口", new Vector2(-220.3514f, 84.09288f));
        }

        static void BuildCreateRoot6(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.stateMachine = BtsmtlSkillAuthoringCode.EnsureStateMachine(generation.graph, "dc99afc8-813d-4957-a166-df4b28dc46a4", "DodgeForward StateMachine", "12fd389b09dc017facc9250c3f9a4ff3", "47098806-a9db-43fb-894a-783c38491e8c");
        }

        static void BuildCreateRoot8(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.state2 = BtsmtlSkillAuthoringCode.EnsureNativeState(generation.stateMachine, typeof(BtsmtlSkillNativeExitState), "09a76e8e-93d6-4a57-8d21-77a635b07acd", "状态机出口", new Vector2(460f, 0f));
            generation.state3 = BtsmtlSkillAuthoringCode.EnsureNativeState(generation.stateMachine, typeof(BtsmtlSkillNativeAnyState), "0c87a3ad-304a-4230-9f8f-9bce249746bc", "任意状态", new Vector2(0f, 240f));
            generation.state1 = BtsmtlSkillAuthoringCode.EnsureNativeState(generation.stateMachine, typeof(BtsmtlSkillNativeState), "c41e85f4-dacc-49e3-b1b0-bb33b502eba6", "DodgeForward", new Vector2(80f, 0f));
            generation.state = BtsmtlSkillAuthoringCode.EnsureNativeState(generation.stateMachine, typeof(BtsmtlSkillNativeEntryState), "fc180d6b-1f14-486d-9502-e2311d4fc23d", "技能入口", new Vector2(-260f, 0f));
        }

        static void BuildConfigureRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            var endRule = new GameplayAbilityEndRule();
            endRule.Configure(GameplayAbilityEndTrigger.AbortRequested, ActionLifecycleTransitionType.Abort, "", "TreeAbort");
            var endRule1 = new GameplayAbilityEndRule();
            endRule1.Configure(GameplayAbilityEndTrigger.ExecutionCompleted, ActionLifecycleTransitionType.Complete, "", "TimelineCompleted");
            var endRule2 = new GameplayAbilityEndRule();
            endRule2.Configure(GameplayAbilityEndTrigger.InterruptRequested, ActionLifecycleTransitionType.Interrupt, "", "TreeInterrupt");
            var endRule3 = new GameplayAbilityEndRule();
            endRule3.Configure(GameplayAbilityEndTrigger.ActionWindowClosed, ActionLifecycleTransitionType.Cancel, "RecoveryOpen", "RecoveryCancel");
            BtsmtlSkillAuthoringCode.ConfigureAbility(context, "", Array.Empty<GameplayTagId>(), generation.asset, Array.Empty<GameplayEffectDefinition>(), new[] { endRule, endRule1, endRule2, endRule3 }, Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>(), Array.Empty<string>(), "Dodge", true, "", "", false);
        }

        static void BuildBindRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillStateMachineFlowNode)generation.node1).SetStateMachine(generation.stateMachine);
        }

        static void BuildConnectRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph, generation.node, "Output", generation.node1, "Input", "14cb740f-de1e-4bef-8bd5-ec827da247a9");
        }

        static void BuildConnectRoot3(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.stateEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(generation.stateMachine, (BtsmtlSkillNativeState)generation.state, (BtsmtlSkillNativeState)generation.state1, "ad7bfdb0-de13-46c0-8078-2e82fcc8a267");
            generation.stateEdge1 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(generation.stateMachine, (BtsmtlSkillNativeState)generation.state1, (BtsmtlSkillNativeState)generation.state2, "520afb0a-2306-43b7-9a5a-ae41126a5370");
            BtsmtlSkillAuthoringCode.ConfigureNativeConnection((BtsmtlSkillNativeConnection)generation.stateEdge1, generation.graph4, 0, ProgramAbortPolicy.None, 0);
        }

        static void BuildRootBindingRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph, new[] { "a7c0040e-35e8-44d0-9de6-2df949cd76a2", "47098806-a9db-43fb-894a-783c38491e8c" }, new[] { "14cb740f-de1e-4bef-8bd5-ec827da247a9" });
        }

        static void BuildRootBindingRoot3(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneNativeStateMachine(generation.stateMachine, new[] { "c41e85f4-dacc-49e3-b1b0-bb33b502eba6" }, new[] { "ad7bfdb0-de13-46c0-8078-2e82fcc8a267", "520afb0a-2306-43b7-9a5a-ae41126a5370" });
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
            internal Clip clip1;
            internal Track track1;
            internal Clip clip2;
            internal Track track2;
            internal Clip clip3;
            internal GameplayAbilityAdmissionProfile asset;
            internal UnityAnimationClip asset1;
            internal RootMotionCurveAsset asset2;
        }
    }
}
