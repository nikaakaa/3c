using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinDodgeBackGameplayAbilityAuthoringCode
    {
        static DodgeBackParts BuildDodgeBack(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new DodgeBackParts();
            var asset1 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_DodgeBack_Inplace.anim", 7400000L);
            var asset2 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinDodgeBackGameplayAbilityDefinition/CorinDodgeBackTimeline/DodgeBack.asset", 11400000L);
            parts.graph1 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "93e14509-b248-44cc-a263-78c434016564", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "DodgeBack State Body");
            parts.graph2 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph1, "fa5685f170b175b6f28866f79fa0a083", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision DodgeBackIFrame");
            parts.graph3 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph1, "32d315d4d581f2c38d90f506beb95c02", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryOpen");
            parts.graph4 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "921c1fd2-8a23-431a-a49b-39a98e771e2f", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "DodgeBack State Body/StateRootCompleted");
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineFlowNode), "61586c6f-c735-4125-8875-41f1ff5f93c7", "Play DodgeBack Timeline", new Vector2(324f, 225.3333f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillStateOnEnterFlowNode), "e3379cf4-a2a0-4288-b190-d9952dcd30ed", "进入状态", new Vector2(120f, 60f));
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillStateOnExitFlowNode), "e6673554-94b0-4f98-a52e-01158c43b92c", "退出状态", new Vector2(120f, 460f));
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillRootFlowNode), "f02da1e1-1177-4116-b3ed-766e3251db81", "技能入口", new Vector2(120f, 260f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineEnableFlowNode), "3d9b2d59-0fee-42f8-8e53-246b538ecf33", "片段启用", new Vector2(120f, 60f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDestroyFlowNode), "b3e23c3c-2686-43af-b85b-066bfed036c3", "片段销毁", new Vector2(120f, 660f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillBlackboardSetFlowNode), "c46a2985-824e-4035-8374-efd6ef392c96", "Set DodgeBackIFrame", new Vector2(320f, 0f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillRootFlowNode), "f46d6c90-8f68-45ff-baab-be044b379d59", "技能入口", new Vector2(120f, 260f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDisableFlowNode), "fb5e08cb-e551-4c3d-9b1b-2a36e5b32b39", "片段停用", new Vector2(120f, 460f));
            var node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillBlackboardSetFlowNode), "14ae0b1b-d797-423a-ae5f-4e48a32b5ff1", "Set RecoveryOpen", new Vector2(398.7067f, 223.4471f));
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillRootFlowNode), "2c4bb2f9-dc4a-4d9e-98b5-835b1f42c0e5", "技能入口", new Vector2(120f, 260f));
            var node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineDisableFlowNode), "bac2516e-ac8f-45a4-8e6d-da36c659c790", "片段停用", new Vector2(120f, 460f));
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineEnableFlowNode), "d52c4af5-4427-4a97-8234-518f2c15d57a", "片段启用", new Vector2(120f, 60f));
            var node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineDestroyFlowNode), "f6e4ecf3-e1b7-4e61-95da-c0abb154fa94", "片段销毁", new Vector2(120f, 660f));
            var node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillConditionResultFlowNode), "12d56e7c-5437-4a5f-bed0-c0c5c22ccb75", "条件结果", new Vector2(600f, 180f));
            var node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillStateRootCompletedFlowNode), "b4b30de0-f2a9-4145-8a74-376a9fcb4ca8", "状态主体已完成", new Vector2(-360f, 0f));
            var timeline = BtsmtlSkillAuthoringCode.EnsureTimeline(parts.graph1, "fdf10e49-c270-46d9-bd2e-5e45e13c7a97", "CorinDodgeBackTimeline");
            parts.timelineData = timeline.Data;
            var timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(AnimationTrack), "82f04395-f39f-487a-8112-e45882a37deb", "Animation");
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track, "b0ee4319-922b-410e-8c0c-b4fe70eb7504", 0m, asset1);
            var track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(MotionCurveTrack), "8f2a9050-893a-41e1-be05-57312ab21153", "Motion Curve");
            var clip1 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track1, "ca9b83e7-d1c4-4224-b62d-a51b4a4b9e0d", 0m, asset2, 2.350000000093132257461547852m, 0, 0, 0);
            var track2 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(TreeTrack), "a636f440-208c-47db-88c1-afe9792423df", "Decision");
            var clip2 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track2, "180ec5ff-8aca-43b2-93aa-32d2354b4851", 0.1000000000931322574615478516m, parts.graph2, 0.75m, 0, 0, 0);
            var clip3 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track2, "b88aeaf5-d4a4-4a9d-a706-d6da6cb8072a", 0.75m, parts.graph3, 2.350000000093132257461547852m, 0, 0, 0);
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph1, "066d593b6cd64cdb9de608744b830ca3", "RecoveryOpen", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Dodge/RecoveryOpen", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryOpen", "DodgeRecoveryCancel", 7002UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph1, "131816a77d7344d7b03d11917cb9c75d", "DodgeBackIFrame", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "IFrame", "DodgeBackIFrame", 1UL));
            BtsmtlSkillAuthoringContract.Apply(node4, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", timeline) });
            BtsmtlSkillAuthoringContract.Apply(node8, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "131816a77d7344d7b03d11917cb9c75d"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "93e14509-b248-44cc-a263-78c434016564"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node13, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "066d593b6cd64cdb9de608744b830ca3"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "93e14509-b248-44cc-a263-78c434016564"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip1, new[] { new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "DodgeBack"), new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, asset2), new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, 2.35f) });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip2, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, parts.graph2) });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip3, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, parts.graph3) });
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node3, "Output", node4, "Input", "1d4bfe96-f332-4168-a910-fecb32dd4a43");
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node7, "Output", node8, "Input", "51e60cb8-d436-465c-b8cf-4ca33e8ee920");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node12, "Output", node13, "Input", "8f725471-ff0c-4f15-a97e-4874a314760d");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node16, "m_Output", node17, "m_Result", "797b1212-00ef-4290-9fb4-17258e039be7");
            return parts;
        }

        sealed class DodgeBackParts
        {
            internal BtsmtlSkillFlowGraph graph1;
            internal BtsmtlSkillFlowGraph graph2;
            internal BtsmtlSkillFlowGraph graph3;
            internal BtsmtlSkillFlowGraph graph4;
            internal TimelineData timelineData;
        }
    }
}
