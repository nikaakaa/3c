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
    public sealed class CorinDodgeBackGameplayAbilityAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var asset = context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>("Assets/Configs/Character/Corin/Pipeline/Actions/Dodge/CorinDodgeActionProfile.asset", 11400000L);
            var asset1 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_DodgeBack_Inplace.anim", 7400000L);
            var asset2 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinDodgeBackGameplayAbilityDefinition/CorinDodgeBackTimeline/DodgeBack.asset", 11400000L);

            var graph = BtsmtlSkillAuthoringCode.EnsureAbilityRoot(context, "DodgeBack", "5d6c5b40e613e36a79c647152961ea33", "DodgeBack");
            var graph1 = BtsmtlSkillAuthoringCode.EnsureChildGraph(graph, "93e14509-b248-44cc-a263-78c434016564", BtsmtlSkillFlowGraphRole.StateBody, "DodgeBack State Body");
            var graph2 = BtsmtlSkillAuthoringCode.EnsureChildGraph(graph1, "fa5685f170b175b6f28866f79fa0a083", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision DodgeBackIFrame");
            var graph3 = BtsmtlSkillAuthoringCode.EnsureChildGraph(graph1, "32d315d4d581f2c38d90f506beb95c02", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryOpen");
            var graph4 = BtsmtlSkillAuthoringCode.EnsureChildGraph(graph, "921c1fd2-8a23-431a-a49b-39a98e771e2f", BtsmtlSkillFlowGraphRole.ConditionRule, "DodgeBack State Body/StateRootCompleted");
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillRootFlowNode), "37772f70-8c7f-4126-ac64-e32fcca47890", "技能入口", new Vector2(-354.6667f, 6f));
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillStateMachineFlowNode), "c5122005-2d41-4e69-93e9-e6b33f6afc61", "DodgeBack StateMachine", new Vector2(260f, 6f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph1, typeof(BtsmtlSkillTimelineFlowNode), "61586c6f-c735-4125-8875-41f1ff5f93c7", "Play DodgeBack Timeline", new Vector2(324f, 225.3333f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph1, typeof(BtsmtlSkillStateOnEnterFlowNode), "e3379cf4-a2a0-4288-b190-d9952dcd30ed", "进入状态", new Vector2(120f, 60f));
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph1, typeof(BtsmtlSkillStateOnExitFlowNode), "e6673554-94b0-4f98-a52e-01158c43b92c", "退出状态", new Vector2(120f, 460f));
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph1, typeof(BtsmtlSkillRootFlowNode), "f02da1e1-1177-4116-b3ed-766e3251db81", "技能入口", new Vector2(120f, 260f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph2, typeof(BtsmtlSkillTimelineEnableFlowNode), "3d9b2d59-0fee-42f8-8e53-246b538ecf33", "片段启用", new Vector2(120f, 60f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph2, typeof(BtsmtlSkillTimelineDestroyFlowNode), "b3e23c3c-2686-43af-b85b-066bfed036c3", "片段销毁", new Vector2(120f, 660f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph2, typeof(BtsmtlSkillBlackboardSetFlowNode), "c46a2985-824e-4035-8374-efd6ef392c96", "Set DodgeBackIFrame", new Vector2(320f, 0f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph2, typeof(BtsmtlSkillRootFlowNode), "f46d6c90-8f68-45ff-baab-be044b379d59", "技能入口", new Vector2(120f, 260f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph2, typeof(BtsmtlSkillTimelineDisableFlowNode), "fb5e08cb-e551-4c3d-9b1b-2a36e5b32b39", "片段停用", new Vector2(120f, 460f));
            var node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph3, typeof(BtsmtlSkillBlackboardSetFlowNode), "14ae0b1b-d797-423a-ae5f-4e48a32b5ff1", "Set RecoveryOpen", new Vector2(398.7067f, 223.4471f));
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph3, typeof(BtsmtlSkillRootFlowNode), "2c4bb2f9-dc4a-4d9e-98b5-835b1f42c0e5", "技能入口", new Vector2(120f, 260f));
            var node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph3, typeof(BtsmtlSkillTimelineDisableFlowNode), "bac2516e-ac8f-45a4-8e6d-da36c659c790", "片段停用", new Vector2(120f, 460f));
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph3, typeof(BtsmtlSkillTimelineEnableFlowNode), "d52c4af5-4427-4a97-8234-518f2c15d57a", "片段启用", new Vector2(120f, 60f));
            var node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph3, typeof(BtsmtlSkillTimelineDestroyFlowNode), "f6e4ecf3-e1b7-4e61-95da-c0abb154fa94", "片段销毁", new Vector2(120f, 660f));
            var node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph4, typeof(BtsmtlSkillConditionResultFlowNode), "12d56e7c-5437-4a5f-bed0-c0c5c22ccb75", "条件结果", new Vector2(600f, 180f));
            var node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph4, typeof(BtsmtlSkillStateRootCompletedFlowNode), "b4b30de0-f2a9-4145-8a74-376a9fcb4ca8", "状态主体已完成", new Vector2(-360f, 0f));
            var stateMachine = BtsmtlSkillAuthoringCode.EnsureStateMachine(graph, "8d9a3eed-127b-4405-9e67-6f33d5d3d3bf", "DodgeBack StateMachine", "5d6c5b40e613e36a79c647152961ea33", "c5122005-2d41-4e69-93e9-e6b33f6afc61");
            var timeline = BtsmtlSkillAuthoringCode.EnsureTimeline(graph1, "fdf10e49-c270-46d9-bd2e-5e45e13c7a97", "CorinDodgeBackTimeline");
            var state2 = BtsmtlSkillAuthoringCode.EnsureNativeState(stateMachine, typeof(BtsmtlSkillNativeExitState), "00b9a83c-948d-4896-ac17-2a2404189e07", "状态机出口", new Vector2(521.3334f, 124.0001f));
            var state = BtsmtlSkillAuthoringCode.EnsureNativeState(stateMachine, typeof(BtsmtlSkillNativeEntryState), "1fa0f321-eba5-4e94-aa38-822d84de055e", "技能入口", new Vector2(-260f, 0f));
            var state3 = BtsmtlSkillAuthoringCode.EnsureNativeState(stateMachine, typeof(BtsmtlSkillNativeAnyState), "6d7e8b1d-4e9c-4c84-a1e0-8d7f0e4c04c5", "任意状态", new Vector2(0f, 240f));
            var state1 = BtsmtlSkillAuthoringCode.EnsureNativeState(stateMachine, typeof(BtsmtlSkillNativeState), "e376c542-fd18-4a79-bd6c-df249bc626e0", "DodgeBack", new Vector2(41.49386f, 35.21167f));
            var timelineData = timeline.Data;
            var timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(timelineData, timelineCatalog, typeof(AnimationTrack), "82f04395-f39f-487a-8112-e45882a37deb", "Animation");
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(timelineData, timelineCatalog, track, "b0ee4319-922b-410e-8c0c-b4fe70eb7504", 0, asset1);
            var track1 = BtsmtlSkillAuthoringCode.EnsureTrack(timelineData, timelineCatalog, typeof(MotionCurveTrack), "8f2a9050-893a-41e1-be05-57312ab21153", "Motion Curve");
            var clip1 = BtsmtlSkillAuthoringCode.EnsureClip(timelineData, timelineCatalog, track1, "ca9b83e7-d1c4-4224-b62d-a51b4a4b9e0d", 0, asset2, 141, 0, 0, 0);
            var track2 = BtsmtlSkillAuthoringCode.EnsureTrack(timelineData, timelineCatalog, typeof(TreeTrack), "a636f440-208c-47db-88c1-afe9792423df", "Decision");
            var clip2 = BtsmtlSkillAuthoringCode.EnsureClip(timelineData, timelineCatalog, track2, "180ec5ff-8aca-43b2-93aa-32d2354b4851", 6, graph2, 45, 0, 0, 0);
            var clip3 = BtsmtlSkillAuthoringCode.EnsureClip(timelineData, timelineCatalog, track2, "b88aeaf5-d4a4-4a9d-a706-d6da6cb8072a", 45, graph3, 141, 0, 0, 0);

            var endRule = new GameplayAbilityEndRule();
            endRule.Configure(GameplayAbilityEndTrigger.AbortRequested, ActionLifecycleTransitionType.Abort, "", "TreeAbort");
            var endRule1 = new GameplayAbilityEndRule();
            endRule1.Configure(GameplayAbilityEndTrigger.InterruptRequested, ActionLifecycleTransitionType.Interrupt, "", "TreeInterrupt");
            var endRule2 = new GameplayAbilityEndRule();
            endRule2.Configure(GameplayAbilityEndTrigger.ActionWindowClosed, ActionLifecycleTransitionType.Cancel, "RecoveryOpen", "RecoveryCancel");
            var endRule3 = new GameplayAbilityEndRule();
            endRule3.Configure(GameplayAbilityEndTrigger.ExecutionCompleted, ActionLifecycleTransitionType.Complete, "", "TimelineCompleted");
            BtsmtlSkillAuthoringCode.ConfigureAbility(context, "", Array.Empty<GameplayTagId>(), asset, Array.Empty<GameplayEffectDefinition>(), new[] { endRule, endRule1, endRule2, endRule3 }, Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>(), Array.Empty<string>(), "Dodge", true, "", "", false);
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(graph1, "066d593b6cd64cdb9de608744b830ca3", "RecoveryOpen", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Dodge/RecoveryOpen", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryOpen", "DodgeRecoveryCancel", 7002UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(graph1, "131816a77d7344d7b03d11917cb9c75d", "DodgeBackIFrame", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "IFrame", "DodgeBackIFrame", 1UL));
            ((BtsmtlSkillBlackboardAccessFlowNode)node8).Configure(new BtsmtlSkillBlackboardReference("131816a77d7344d7b03d11917cb9c75d", "93e14509-b248-44cc-a263-78c434016564"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)node13).Configure(new BtsmtlSkillBlackboardReference("066d593b6cd64cdb9de608744b830ca3", "93e14509-b248-44cc-a263-78c434016564"), BtsmtlSkillBlackboardValueType.Boolean, null);
            timelineData.ConfigureAuthoringIdentity("fdf10e49-c270-46d9-bd2e-5e45e13c7a97");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            ((TimelineAnimationClip)clip).Clip = asset1;
            ((TimelineAnimationClip)clip).BlendProfileId = "corin.animation-rig.action-blend-profile";
            BtsmtlSkillAuthoringCode.ConfigureMotionCurveSource((MotionCurveClip)clip1, asset2, 0f, 2.35f);
            ((MotionCurveClip)clip1).CurveId = "DodgeBack";
            ((TreeClip)clip2).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);
            ((TreeClip)clip3).SetExecutionPhase(TimelineTreeExecutionPhase.Decision);

            ((BtsmtlSkillStateMachineFlowNode)node1).SetStateMachine(stateMachine);
            ((BtsmtlSkillTimelineFlowNode)node4).Configure(timeline, BtsmtlSkillTimelineOwnership.Private, null, TimelinePlaybackMode.Once);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)state1, graph1);

            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, node, "Output", node1, "Input", "9e42a23e-40ab-4c4f-9680-92be8536ccfa");
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph1, node3, "Output", node4, "Input", "1d4bfe96-f332-4168-a910-fecb32dd4a43");
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph2, node7, "Output", node8, "Input", "51e60cb8-d436-465c-b8cf-4ca33e8ee920");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph3, node12, "Output", node13, "Input", "8f725471-ff0c-4f15-a97e-4874a314760d");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph4, node16, "m_Output", node17, "m_Result", "797b1212-00ef-4290-9fb4-17258e039be7");
            var stateEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(stateMachine, (BtsmtlSkillNativeState)state, (BtsmtlSkillNativeState)state1, "027effc7-ec6b-453c-90a5-cedb9418730a");
            var stateEdge1 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(stateMachine, (BtsmtlSkillNativeState)state1, (BtsmtlSkillNativeState)state2, "56b68cd3-0249-4f8f-9af2-06c451584b84");
            BtsmtlSkillAuthoringCode.ConfigureNativeConnection((BtsmtlSkillNativeConnection)stateEdge1, graph4, 0, ProgramAbortPolicy.None, 0);

            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph, new[] { "37772f70-8c7f-4126-ac64-e32fcca47890", "c5122005-2d41-4e69-93e9-e6b33f6afc61" }, new[] { "9e42a23e-40ab-4c4f-9680-92be8536ccfa" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph1, new[] { "e3379cf4-a2a0-4288-b190-d9952dcd30ed", "f02da1e1-1177-4116-b3ed-766e3251db81", "61586c6f-c735-4125-8875-41f1ff5f93c7", "e6673554-94b0-4f98-a52e-01158c43b92c" }, new[] { "1d4bfe96-f332-4168-a910-fecb32dd4a43" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph2, new[] { "3d9b2d59-0fee-42f8-8e53-246b538ecf33", "f46d6c90-8f68-45ff-baab-be044b379d59", "c46a2985-824e-4035-8374-efd6ef392c96", "fb5e08cb-e551-4c3d-9b1b-2a36e5b32b39", "b3e23c3c-2686-43af-b85b-066bfed036c3" }, new[] { "51e60cb8-d436-465c-b8cf-4ca33e8ee920" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph3, new[] { "d52c4af5-4427-4a97-8234-518f2c15d57a", "2c4bb2f9-dc4a-4d9e-98b5-835b1f42c0e5", "14ae0b1b-d797-423a-ae5f-4e48a32b5ff1", "bac2516e-ac8f-45a4-8e6d-da36c659c790", "f6e4ecf3-e1b7-4e61-95da-c0abb154fa94" }, new[] { "8f725471-ff0c-4f15-a97e-4874a314760d" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph4, new[] { "b4b30de0-f2a9-4145-8a74-376a9fcb4ca8", "12d56e7c-5437-4a5f-bed0-c0c5c22ccb75" }, new[] { "797b1212-00ef-4290-9fb4-17258e039be7" });
            BtsmtlSkillAuthoringCode.PruneNativeStateMachine(stateMachine, new[] { "e376c542-fd18-4a79-bd6c-df249bc626e0" }, new[] { "027effc7-ec6b-453c-90a5-cedb9418730a", "56b68cd3-0249-4f8f-9af2-06c451584b84" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(graph, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(graph1, new[] { "066d593b6cd64cdb9de608744b830ca3", "131816a77d7344d7b03d11917cb9c75d" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(graph2, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(graph3, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(graph4, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(timelineData, new[] { "82f04395-f39f-487a-8112-e45882a37deb", "8f2a9050-893a-41e1-be05-57312ab21153", "a636f440-208c-47db-88c1-afe9792423df" }, new[] { "b0ee4319-922b-410e-8c0c-b4fe70eb7504", "ca9b83e7-d1c4-4224-b62d-a51b4a4b9e0d", "180ec5ff-8aca-43b2-93aa-32d2354b4851", "b88aeaf5-d4a4-4a9d-a706-d6da6cb8072a" }, Array.Empty<string>(), Array.Empty<string>());
            BtsmtlSkillAuthoringCode.BindAbilityRoot(context, graph);
            return context.Complete(graph);
        }
    }
}
