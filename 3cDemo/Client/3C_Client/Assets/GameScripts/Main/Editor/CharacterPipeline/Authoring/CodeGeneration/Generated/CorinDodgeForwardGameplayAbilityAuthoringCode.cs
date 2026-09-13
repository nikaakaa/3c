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
    public sealed class CorinDodgeForwardGameplayAbilityAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var asset = context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>("Assets/Configs/Character/Corin/Pipeline/Actions/Dodge/CorinDodgeActionProfile.asset", 11400000L);
            var asset1 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_DodgeForward_Inplace.anim", 7400000L);
            var asset2 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinDodgeForwardGameplayAbilityDefinition/CorinDodgeForwardTimeline/DodgeForward.asset", 11400000L);

            var graph = BtsmtlSkillAuthoringCode.EnsureAbilityRoot(context, "DodgeForward", "12fd389b09dc017facc9250c3f9a4ff3", "DodgeForward");
            var graph1 = BtsmtlSkillAuthoringCode.EnsureChildGraph(graph, "01974466-06fd-4295-a76e-74a85e118390", BtsmtlSkillFlowGraphRole.StateBody, "DodgeForward State Body");
            var graph2 = BtsmtlSkillAuthoringCode.EnsureChildGraph(graph1, "df8fd04f1815742f5c253fd8f4ce3e77", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision DodgeForwardIFrame");
            var graph3 = BtsmtlSkillAuthoringCode.EnsureChildGraph(graph1, "49c3ae7850d26f945fc7ef1aed38c456", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryOpen");
            var graph4 = BtsmtlSkillAuthoringCode.EnsureChildGraph(graph, "21b0165d-bd54-40f4-8610-9f8852be4e1a", BtsmtlSkillFlowGraphRole.ConditionRule, "DodgeForward State Body/StateRootCompleted");
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillStateMachineFlowNode), "47098806-a9db-43fb-894a-783c38491e8c", "DodgeForward StateMachine", new Vector2(260f, 84f));
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillRootFlowNode), "a7c0040e-35e8-44d0-9de6-2df949cd76a2", "技能入口", new Vector2(-220.3514f, 84.09288f));
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph1, typeof(BtsmtlSkillStateOnExitFlowNode), "4a1648fa-c157-43c8-a5a0-272ee43a87aa", "退出状态", new Vector2(120f, 460f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph1, typeof(BtsmtlSkillStateOnEnterFlowNode), "555d035e-3251-417d-83ac-c0b9c5d72314", "进入状态", new Vector2(120f, 60f));
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph1, typeof(BtsmtlSkillRootFlowNode), "a757928f-ae8b-489d-9f8e-bf586374e90a", "技能入口", new Vector2(120f, 260f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph1, typeof(BtsmtlSkillTimelineFlowNode), "c78616f5-3e17-4b1d-b6ec-7a200ebec941", "Play DodgeForward Timeline", new Vector2(300f, 0f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph2, typeof(BtsmtlSkillTimelineEnableFlowNode), "8c4daf5a-133b-49d7-bd36-04729d9346c7", "片段启用", new Vector2(120f, 60f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph2, typeof(BtsmtlSkillTimelineDisableFlowNode), "a5909657-eed5-4c74-9172-25f6ded0b8b7", "片段停用", new Vector2(114.7562f, 387.6369f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph2, typeof(BtsmtlSkillBlackboardSetFlowNode), "adeb7e88-3e33-4201-bd0e-92a00001eaf2", "Set DodgeForwardIFrame", new Vector2(320f, 0f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph2, typeof(BtsmtlSkillTimelineDestroyFlowNode), "b0d239f7-57a2-426f-96d1-62a85034900f", "片段销毁", new Vector2(109.5126f, 526.8099f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph2, typeof(BtsmtlSkillRootFlowNode), "ee704955-b263-4e08-ac4f-d79c65aa2f19", "技能入口", new Vector2(101.1227f, 241.1227f));
            var node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph3, typeof(BtsmtlSkillTimelineDisableFlowNode), "0b17a643-0e7a-45d5-8d97-238ace861568", "片段停用", new Vector2(120f, 460f));
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph3, typeof(BtsmtlSkillTimelineEnableFlowNode), "50a3ad51-d102-46c6-8f59-3556f8101df7", "片段启用", new Vector2(120f, 60f));
            var node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph3, typeof(BtsmtlSkillTimelineDestroyFlowNode), "8855643c-4b97-411e-bca9-e19ae9542912", "片段销毁", new Vector2(120f, 660f));
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph3, typeof(BtsmtlSkillRootFlowNode), "b2376875-f4a0-4c2a-adaf-a6e4c0278a82", "技能入口", new Vector2(120f, 260f));
            var node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph3, typeof(BtsmtlSkillBlackboardSetFlowNode), "b2552514-5758-45ae-9201-f7293a7478c2", "Set RecoveryOpen", new Vector2(320f, 0f));
            var node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph4, typeof(BtsmtlSkillStateRootCompletedFlowNode), "2eaafbfa-a850-4809-9fdd-e22b00780718", "状态主体已完成", new Vector2(-360f, 0f));
            var node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph4, typeof(BtsmtlSkillConditionResultFlowNode), "3f7f717a-3ccb-4fca-a0b1-f2011e236a0a", "条件结果", new Vector2(600f, 180f));
            var stateMachine = BtsmtlSkillAuthoringCode.EnsureStateMachine(graph, "dc99afc8-813d-4957-a166-df4b28dc46a4", "DodgeForward StateMachine", "12fd389b09dc017facc9250c3f9a4ff3", "47098806-a9db-43fb-894a-783c38491e8c");
            var timeline = BtsmtlSkillAuthoringCode.EnsureTimeline(graph1, "b871bfc9-f182-473b-8c7f-be176b620394", "CorinDodgeForwardTimeline");
            var state2 = BtsmtlSkillAuthoringCode.EnsureNativeState(stateMachine, typeof(BtsmtlSkillNativeExitState), "09a76e8e-93d6-4a57-8d21-77a635b07acd", "状态机出口", new Vector2(460f, 0f));
            var state3 = BtsmtlSkillAuthoringCode.EnsureNativeState(stateMachine, typeof(BtsmtlSkillNativeAnyState), "0c87a3ad-304a-4230-9f8f-9bce249746bc", "任意状态", new Vector2(0f, 240f));
            var state1 = BtsmtlSkillAuthoringCode.EnsureNativeState(stateMachine, typeof(BtsmtlSkillNativeState), "c41e85f4-dacc-49e3-b1b0-bb33b502eba6", "DodgeForward", new Vector2(80f, 0f));
            var state = BtsmtlSkillAuthoringCode.EnsureNativeState(stateMachine, typeof(BtsmtlSkillNativeEntryState), "fc180d6b-1f14-486d-9502-e2311d4fc23d", "技能入口", new Vector2(-260f, 0f));
            var timelineData = timeline.Data;
            var timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(timelineData, timelineCatalog, typeof(TreeTrack), "1fabe64a-9df6-4bb3-a395-ebfa32d9874b", "Decision");
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(timelineData, timelineCatalog, track, "9080e5fb-1e6c-414a-bd9f-94cab1cf7c5c", 6, graph2, 45, 0, 0, 0);
            var clip1 = BtsmtlSkillAuthoringCode.EnsureClip(timelineData, timelineCatalog, track, "913539bd-df57-4f13-b965-feafda823f36", 46, graph3, 142, 0, 0, 0);
            var track1 = BtsmtlSkillAuthoringCode.EnsureTrack(timelineData, timelineCatalog, typeof(AnimationTrack), "8e4b0f0d-829b-4818-9a8b-39d9246447ea", "Animation");
            var clip2 = BtsmtlSkillAuthoringCode.EnsureClip(timelineData, timelineCatalog, track1, "e9d429f1-595e-475a-b647-444a6e069c92", 0, asset1);
            var track2 = BtsmtlSkillAuthoringCode.EnsureTrack(timelineData, timelineCatalog, typeof(MotionCurveTrack), "a0c961a6-0179-4f4d-8b75-76fa3a8bcd0a", "Motion Curve");
            var clip3 = BtsmtlSkillAuthoringCode.EnsureClip(timelineData, timelineCatalog, track2, "ea9a052b-5407-4a22-b1c2-f5bbce6e31bc", 0, asset2, 141, 0, 0, 0);

            var endRule = new GameplayAbilityEndRule();
            endRule.Configure(GameplayAbilityEndTrigger.AbortRequested, ActionLifecycleTransitionType.Abort, "", "TreeAbort");
            var endRule1 = new GameplayAbilityEndRule();
            endRule1.Configure(GameplayAbilityEndTrigger.ExecutionCompleted, ActionLifecycleTransitionType.Complete, "", "TimelineCompleted");
            var endRule2 = new GameplayAbilityEndRule();
            endRule2.Configure(GameplayAbilityEndTrigger.InterruptRequested, ActionLifecycleTransitionType.Interrupt, "", "TreeInterrupt");
            var endRule3 = new GameplayAbilityEndRule();
            endRule3.Configure(GameplayAbilityEndTrigger.ActionWindowClosed, ActionLifecycleTransitionType.Cancel, "RecoveryOpen", "RecoveryCancel");
            BtsmtlSkillAuthoringCode.ConfigureAbility(context, "", Array.Empty<GameplayTagId>(), asset, Array.Empty<GameplayEffectDefinition>(), new[] { endRule, endRule1, endRule2, endRule3 }, Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>(), Array.Empty<string>(), "Dodge", true, "", "", false);
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(graph1, "4a7a79cd9fc14a8f807d3d9580548cc1", "RecoveryOpen", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Dodge/RecoveryOpen", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryOpen", "DodgeForwardRecoveryOpen", 7003UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(graph1, "d9ecf57b06514746a1eed7a5a7217a6b", "DodgeForwardIFrame", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "IFrame", "DodgeForwardIFrame", 2UL));
            ((BtsmtlSkillBlackboardAccessFlowNode)node8).Configure(new BtsmtlSkillBlackboardReference("d9ecf57b06514746a1eed7a5a7217a6b", "01974466-06fd-4295-a76e-74a85e118390"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)node13).Configure(new BtsmtlSkillBlackboardReference("4a7a79cd9fc14a8f807d3d9580548cc1", "01974466-06fd-4295-a76e-74a85e118390"), BtsmtlSkillBlackboardValueType.Boolean, null);
            timelineData.ConfigureAuthoringIdentity("b871bfc9-f182-473b-8c7f-be176b620394");
            ((TreeClip)clip).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)clip1).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((AnimationTrack)track1).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track1).SetAnimationSlotId("corin.full-body-action");
            ((TimelineAnimationClip)clip2).Clip = asset1;
            ((TimelineAnimationClip)clip2).ExtraPolationMode = ExtraPolationMode.Hold;
            ((TimelineAnimationClip)clip2).BlendProfileId = "corin.animation-rig.action-blend-profile";
            BtsmtlSkillAuthoringCode.ConfigureMotionCurveSource((MotionCurveClip)clip3, asset2, 0f, 2.35f);
            ((MotionCurveClip)clip3).CurveId = "DodgeForward";

            ((BtsmtlSkillStateMachineFlowNode)node1).SetStateMachine(stateMachine);
            ((BtsmtlSkillTimelineFlowNode)node4).Configure(timeline, BtsmtlSkillTimelineOwnership.Private, null, TimelinePlaybackMode.Once);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)state1, graph1);

            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, node, "Output", node1, "Input", "14cb740f-de1e-4bef-8bd5-ec827da247a9");
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph1, node3, "Output", node4, "Input", "1823df30-39a2-4088-9b6d-6e4f2a55cf53");
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph2, node7, "Output", node8, "Input", "41bd0710-f8cf-48f3-bad7-29c7416b6098");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph3, node12, "Output", node13, "Input", "124777d1-9bbf-4292-9ca7-4b89326a8ed4");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph4, node16, "m_Output", node17, "m_Result", "308a15ae-b5f5-49ab-ac56-3c070dea8130");
            var stateEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(stateMachine, (BtsmtlSkillNativeState)state, (BtsmtlSkillNativeState)state1, "ad7bfdb0-de13-46c0-8078-2e82fcc8a267");
            var stateEdge1 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(stateMachine, (BtsmtlSkillNativeState)state1, (BtsmtlSkillNativeState)state2, "520afb0a-2306-43b7-9a5a-ae41126a5370");
            BtsmtlSkillAuthoringCode.ConfigureNativeConnection((BtsmtlSkillNativeConnection)stateEdge1, graph4, 0, ProgramAbortPolicy.None, 0);

            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph, new[] { "a7c0040e-35e8-44d0-9de6-2df949cd76a2", "47098806-a9db-43fb-894a-783c38491e8c" }, new[] { "14cb740f-de1e-4bef-8bd5-ec827da247a9" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph1, new[] { "555d035e-3251-417d-83ac-c0b9c5d72314", "a757928f-ae8b-489d-9f8e-bf586374e90a", "c78616f5-3e17-4b1d-b6ec-7a200ebec941", "4a1648fa-c157-43c8-a5a0-272ee43a87aa" }, new[] { "1823df30-39a2-4088-9b6d-6e4f2a55cf53" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph2, new[] { "8c4daf5a-133b-49d7-bd36-04729d9346c7", "ee704955-b263-4e08-ac4f-d79c65aa2f19", "adeb7e88-3e33-4201-bd0e-92a00001eaf2", "a5909657-eed5-4c74-9172-25f6ded0b8b7", "b0d239f7-57a2-426f-96d1-62a85034900f" }, new[] { "41bd0710-f8cf-48f3-bad7-29c7416b6098" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph3, new[] { "50a3ad51-d102-46c6-8f59-3556f8101df7", "b2376875-f4a0-4c2a-adaf-a6e4c0278a82", "b2552514-5758-45ae-9201-f7293a7478c2", "0b17a643-0e7a-45d5-8d97-238ace861568", "8855643c-4b97-411e-bca9-e19ae9542912" }, new[] { "124777d1-9bbf-4292-9ca7-4b89326a8ed4" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph4, new[] { "2eaafbfa-a850-4809-9fdd-e22b00780718", "3f7f717a-3ccb-4fca-a0b1-f2011e236a0a" }, new[] { "308a15ae-b5f5-49ab-ac56-3c070dea8130" });
            BtsmtlSkillAuthoringCode.PruneNativeStateMachine(stateMachine, new[] { "c41e85f4-dacc-49e3-b1b0-bb33b502eba6" }, new[] { "ad7bfdb0-de13-46c0-8078-2e82fcc8a267", "520afb0a-2306-43b7-9a5a-ae41126a5370" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(graph, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(graph1, new[] { "4a7a79cd9fc14a8f807d3d9580548cc1", "d9ecf57b06514746a1eed7a5a7217a6b" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(graph2, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(graph3, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(graph4, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(timelineData, new[] { "1fabe64a-9df6-4bb3-a395-ebfa32d9874b", "8e4b0f0d-829b-4818-9a8b-39d9246447ea", "a0c961a6-0179-4f4d-8b75-76fa3a8bcd0a" }, new[] { "9080e5fb-1e6c-414a-bd9f-94cab1cf7c5c", "913539bd-df57-4f13-b965-feafda823f36", "e9d429f1-595e-475a-b647-444a6e069c92", "ea9a052b-5407-4a22-b1c2-f5bbce6e31bc" }, Array.Empty<string>(), Array.Empty<string>());
            BtsmtlSkillAuthoringCode.BindAbilityRoot(context, graph);
            return context.Complete(graph);
        }
    }
}
