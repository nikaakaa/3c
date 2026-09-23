using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinDodgeForwardGameplayAbilityAuthoringCode
    {
        static DodgeForwardParts BuildDodgeForward(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new DodgeForwardParts();
            var asset1 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_DodgeForward_Inplace.anim", 7400000L);
            var asset2 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinDodgeForwardGameplayAbilityDefinition/CorinDodgeForwardTimeline/DodgeForward.asset", 11400000L);
            parts.graph1 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "01974466-06fd-4295-a76e-74a85e118390", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "DodgeForward State Body");
            parts.graph2 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph1, "df8fd04f1815742f5c253fd8f4ce3e77", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision DodgeForwardIFrame");
            parts.graph3 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph2, "e4fb0f3dec444c26974190dee25c99ca", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            parts.graph4 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph1, "49c3ae7850d26f945fc7ef1aed38c456", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryOpen");
            parts.graph5 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph4, "d01db78358894d4eb3ddc83d71fb61e8", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            parts.graph6 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "21b0165d-bd54-40f4-8610-9f8852be4e1a", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "DodgeForward State Body/StateRootCompleted");
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillStateOnExitFlowNode), "4a1648fa-c157-43c8-a5a0-272ee43a87aa", "退出状态", new Vector2(120f, 460f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillStateOnEnterFlowNode), "555d035e-3251-417d-83ac-c0b9c5d72314", "进入状态", new Vector2(120f, 60f));
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillRootFlowNode), "a757928f-ae8b-489d-9f8e-bf586374e90a", "技能入口", new Vector2(120f, 260f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineFlowNode), "c78616f5-3e17-4b1d-b6ec-7a200ebec941", "Play DodgeForward Timeline", new Vector2(300f, 0f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "1ffc339cfc80443f99c1b3440e6382dd", "结束片段", new Vector2(520f, 260f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineEnableFlowNode), "8c4daf5a-133b-49d7-bd36-04729d9346c7", "片段启用", new Vector2(120f, 60f));
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDisableFlowNode), "a5909657-eed5-4c74-9172-25f6ded0b8b7", "片段停用", new Vector2(114.7562f, 387.6369f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillBlackboardSetFlowNode), "adeb7e88-3e33-4201-bd0e-92a00001eaf2", "Set DodgeForwardIFrame", new Vector2(320f, 0f));
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDestroyFlowNode), "b0d239f7-57a2-426f-96d1-62a85034900f", "片段销毁", new Vector2(109.5126f, 526.8099f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillSelectorFlowNode), "d2cb42aaf9fa46cda30a8227f9c431eb", "窗口执行或结束", new Vector2(280f, 260f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillRootFlowNode), "ee704955-b263-4e08-ac4f-d79c65aa2f19", "技能入口", new Vector2(101.1227f, 241.1227f));
            var node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "258900970f6c4204b213b8a1676d84ce", "到达结束时间", new Vector2(-100f, 0f));
            var node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillConditionResultFlowNode), "d9785381-0e21-4a4c-a1aa-a918eb99644d", null, new Vector2(600f, 180f));
            var node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineTimeFlowNode), "f8afa4a6bfd24deba6b47ff701ae3127", "Timeline时间", new Vector2(-360f, 0f));
            var node21 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineDisableFlowNode), "0b17a643-0e7a-45d5-8d97-238ace861568", "片段停用", new Vector2(120f, 460f));
            var node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineEnableFlowNode), "50a3ad51-d102-46c6-8f59-3556f8101df7", "片段启用", new Vector2(120f, 60f));
            var node22 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineDestroyFlowNode), "8855643c-4b97-411e-bca9-e19ae9542912", "片段销毁", new Vector2(120f, 660f));
            var node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillRootFlowNode), "b2376875-f4a0-4c2a-adaf-a6e4c0278a82", "技能入口", new Vector2(120f, 260f));
            var node20 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillBlackboardSetFlowNode), "b2552514-5758-45ae-9201-f7293a7478c2", "Set RecoveryOpen", new Vector2(357.1542f, 268.9257f));
            var node18 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillSelectorFlowNode), "ee9ad3b56f7e46cba0a163577c0abd18", "窗口执行或结束", new Vector2(280f, 260f));
            var node19 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "f9386ecb0b2c46d99d90d7bc24801f8f", "结束片段", new Vector2(520f, 260f));
            var node24 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "2c16d1f9d4994b28a1760def26e567f4", "到达结束时间", new Vector2(-100f, 0f));
            var node23 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillTimelineTimeFlowNode), "3f4879384e4c483dac358a69edd5dde7", "Timeline时间", new Vector2(-360f, 0f));
            var node25 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillConditionResultFlowNode), "ae0d291d-cd16-422e-9e2e-d6df6f9a1a4a", null, new Vector2(600f, 180f));
            var node26 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillStateRootCompletedFlowNode), "2eaafbfa-a850-4809-9fdd-e22b00780718", "状态主体已完成", new Vector2(-360f, 0f));
            var node27 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillConditionResultFlowNode), "3f7f717a-3ccb-4fca-a0b1-f2011e236a0a", "条件结果", new Vector2(600f, 180f));
            var timeline = BtsmtlSkillAuthoringCode.EnsureTimeline(parts.graph1, "b871bfc9-f182-473b-8c7f-be176b620394", "CorinDodgeForwardTimeline");
            parts.timelineData = timeline.Data;
            var timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(TreeTrack), "1fabe64a-9df6-4bb3-a395-ebfa32d9874b", "Decision", TimelineExecutionDomain.Logic);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track, "9080e5fb-1e6c-414a-bd9f-94cab1cf7c5c", 0.1000000000931322574615478516m, parts.graph2);
            var track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(TreeTrack), "37d5f20e-931b-c859-1243-f37083cf2954", "Logic / Decision / Decision RecoveryOpen", TimelineExecutionDomain.Logic);
            var clip1 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track1, "913539bd-df57-4f13-b965-feafda823f36", 0.3999999999068677425384521484m, parts.graph4);
            var track2 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(AnimationTrack), "8e4b0f0d-829b-4818-9a8b-39d9246447ea", "Animation", TimelineExecutionDomain.Presentation);
            var clip2 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track2, "e9d429f1-595e-475a-b647-444a6e069c92", 0m, asset1);
            var track3 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(MotionCurveTrack), "a0c961a6-0179-4f4d-8b75-76fa3a8bcd0a", "Motion Curve", TimelineExecutionDomain.Logic);
            var clip3 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track3, "ea9a052b-5407-4a22-b1c2-f5bbce6e31bc", 0m, asset2, 2.3500000000931322574615478516m, 0m, 0m, 0m);
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph1, "4a7a79cd9fc14a8f807d3d9580548cc1", "RecoveryOpen", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Dodge/RecoveryOpen", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryOpen", "DodgeForwardRecoveryOpen", 7003UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph1, "d9ecf57b06514746a1eed7a5a7217a6b", "DodgeForwardIFrame", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "IFrame", "DodgeForwardIFrame", 2UL));
            BtsmtlSkillAuthoringContract.Apply(node4, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", timeline) });
            BtsmtlSkillAuthoringContract.Apply(node10, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "d9ecf57b06514746a1eed7a5a7217a6b"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "01974466-06fd-4295-a76e-74a85e118390"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node10, "m_Value", true);
            BtsmtlSkillAuthoringContract.Apply(node8, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph3, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringCode.SetValue(node14, "b", 2.35000014f);
            BtsmtlSkillAuthoringContract.Apply(node20, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "4a7a79cd9fc14a8f807d3d9580548cc1"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "01974466-06fd-4295-a76e-74a85e118390"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node20, "m_Value", true);
            BtsmtlSkillAuthoringContract.Apply(node18, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph5, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringCode.SetValue(node24, "b", 2.35000014f);
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, parts.graph2) });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip1, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, parts.graph4) });
            ((AnimationTrack)track2).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track2).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip2, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile"), new TimelineAuthoringPropertyValue("extraPolationMode", TimelineAuthoringPropertyKind.Enum, ExtraPolationMode.Hold) });
            TimelineCurveChannelCatalog.Require("animation.weight").Replace(clip2, new AnimationCurve(new[] { new Keyframe(0f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.9999977f, 1f, 0f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("animation.ease-in").Replace(clip2, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 1.00000274f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.9999977f, 1f, 1.00000274f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("animation.ease-out").Replace(clip2, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 1.00000274f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(0.9999977f, 1f, 1.00000274f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip3, new[] { new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "DodgeForward"), new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, asset2), new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, 2.35f) });
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node3, "Output", node4, "Input", "1823df30-39a2-4088-9b6d-6e4f2a55cf53");
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node7, "Output", node8, "Input", "07c227aa25aa434ca1dd5d7b3d8faf81");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node8, "end", node9, "Input", "bed077f00df14bd8b9610cfb9f7b7ca0");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node8, "body", node10, "Input", "ca4977d4fa6e43258d4c92b81127e112");
            var edge5 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node13, "m_Output", node14, "a", "e214c21504e74f598aecf4eb85199d9f");
            var edge6 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node14, "Value", node15, "m_Result", "303077bb97854ee6b8c549061bc99138");
            var edge7 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node17, "Output", node18, "Input", "7890877386624486bedca0ca9b31a258");
            var edge9 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node18, "body", node20, "Input", "a4c0074828e34a248f1dc789c8ba349e");
            var edge8 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node18, "end", node19, "Input", "c20a9591f4ef47cfb454db1b8fb90e97");
            var edge10 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph5, node23, "m_Output", node24, "a", "f5fb71f193504ab8b71932ca274c1bea");
            var edge11 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph5, node24, "Value", node25, "m_Result", "dd2625a662174d599a2e812ea5001f00");
            var edge12 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph6, node26, "m_Output", node27, "m_Result", "308a15ae-b5f5-49ab-ac56-3c070dea8130");
            return parts;
        }

        sealed class DodgeForwardParts
        {
            internal BtsmtlSkillFlowGraph graph1;
            internal BtsmtlSkillFlowGraph graph2;
            internal BtsmtlSkillFlowGraph graph3;
            internal BtsmtlSkillFlowGraph graph4;
            internal BtsmtlSkillFlowGraph graph5;
            internal BtsmtlSkillFlowGraph graph6;
            internal TimelineData timelineData;
        }
    }
}
