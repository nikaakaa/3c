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
    public sealed partial class CorinDodgeBackGameplayAbilityAuthoringCode
    {
        static DodgeBackParts BuildDodgeBack(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new DodgeBackParts();
            var asset1 = context.ResolveExternalAsset<UnityAnimationClip>("Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_DodgeBack_Inplace.anim", 7400000L);
            var asset2 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinDodgeBackGameplayAbilityDefinition/CorinDodgeBackTimeline/DodgeBack.asset", 11400000L);
            parts.graph1 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "93e14509-b248-44cc-a263-78c434016564", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "DodgeBack State Body");
            parts.graph2 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph1, "fa5685f170b175b6f28866f79fa0a083", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision DodgeBackIFrame");
            parts.graph3 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph2, "8e944bdc49df4f6d936d70a56e44609e", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            parts.graph4 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph1, "32d315d4d581f2c38d90f506beb95c02", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryOpen");
            parts.graph5 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph4, "e219e65dcbd34e7da425abb704ac2a51", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            parts.graph6 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "921c1fd2-8a23-431a-a49b-39a98e771e2f", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "DodgeBack State Body/StateRootCompleted");
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineFlowNode), "61586c6f-c735-4125-8875-41f1ff5f93c7", "Play DodgeBack Timeline", new Vector2(324f, 225.3333f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillStateOnEnterFlowNode), "e3379cf4-a2a0-4288-b190-d9952dcd30ed", "进入状态", new Vector2(120f, 60f));
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillStateOnExitFlowNode), "e6673554-94b0-4f98-a52e-01158c43b92c", "退出状态", new Vector2(120f, 460f));
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillRootFlowNode), "f02da1e1-1177-4116-b3ed-766e3251db81", "技能入口", new Vector2(120f, 260f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillSelectorFlowNode), "36ff032035f44c24b0eeb0cd150df97b", "窗口执行或结束", new Vector2(280f, 260f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineEnableFlowNode), "3d9b2d59-0fee-42f8-8e53-246b538ecf33", "片段启用", new Vector2(120f, 60f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "8b9ccd1a341349e1997872baf0b8d7a2", "结束片段", new Vector2(520f, 260f));
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDestroyFlowNode), "b3e23c3c-2686-43af-b85b-066bfed036c3", "片段销毁", new Vector2(120f, 660f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillBlackboardSetFlowNode), "c46a2985-824e-4035-8374-efd6ef392c96", "Set DodgeBackIFrame", new Vector2(320f, 0f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillRootFlowNode), "f46d6c90-8f68-45ff-baab-be044b379d59", "技能入口", new Vector2(120f, 260f));
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDisableFlowNode), "fb5e08cb-e551-4c3d-9b1b-2a36e5b32b39", "片段停用", new Vector2(120f, 460f));
            var node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "0fd8f67fe6e64fc7a2a41061a9ff3566", "到达结束时间", new Vector2(-100f, 0f));
            var node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillConditionResultFlowNode), "10eda577-904b-4576-8ef2-ae219cfb7c77", null, new Vector2(600f, 180f));
            var node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineTimeFlowNode), "c8f912842c6447dbb541455157263359", "Timeline时间", new Vector2(-360f, 0f));
            var node18 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillSelectorFlowNode), "0ca724f697c44a02895828446c945231", "窗口执行或结束", new Vector2(280f, 260f));
            var node20 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillBlackboardSetFlowNode), "14ae0b1b-d797-423a-ae5f-4e48a32b5ff1", "Set RecoveryOpen", new Vector2(398.7067f, 223.4471f));
            var node19 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "248adaf0e46e48b88f0f5bbbde349b7b", "结束片段", new Vector2(520f, 260f));
            var node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillRootFlowNode), "2c4bb2f9-dc4a-4d9e-98b5-835b1f42c0e5", "技能入口", new Vector2(120f, 260f));
            var node21 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineDisableFlowNode), "bac2516e-ac8f-45a4-8e6d-da36c659c790", "片段停用", new Vector2(120f, 460f));
            var node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineEnableFlowNode), "d52c4af5-4427-4a97-8234-518f2c15d57a", "片段启用", new Vector2(120f, 60f));
            var node22 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineDestroyFlowNode), "f6e4ecf3-e1b7-4e61-95da-c0abb154fa94", "片段销毁", new Vector2(120f, 660f));
            var node24 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "76adbe4f8ec9495b9e70f9260703dc9d", "到达结束时间", new Vector2(-100f, 0f));
            var node25 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillConditionResultFlowNode), "b7268f50-070b-4f6c-b1fc-8a598449acf8", null, new Vector2(600f, 180f));
            var node23 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillTimelineTimeFlowNode), "de79ea08c1224ee1a9f46276271b9b45", "Timeline时间", new Vector2(-360f, 0f));
            var node27 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillConditionResultFlowNode), "12d56e7c-5437-4a5f-bed0-c0c5c22ccb75", "条件结果", new Vector2(600f, 180f));
            var node26 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillStateRootCompletedFlowNode), "b4b30de0-f2a9-4145-8a74-376a9fcb4ca8", "状态主体已完成", new Vector2(-360f, 0f));
            var timeline = BtsmtlSkillAuthoringCode.EnsureTimeline(parts.graph1, "fdf10e49-c270-46d9-bd2e-5e45e13c7a97", "CorinDodgeBackTimeline");
            parts.timelineData = timeline.Data;
            var timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(AnimationTrack), "82f04395-f39f-487a-8112-e45882a37deb", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track, "b0ee4319-922b-410e-8c0c-b4fe70eb7504", 0m, asset1);
            var track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(MotionCurveTrack), "8f2a9050-893a-41e1-be05-57312ab21153", "Motion Curve", TimelineExecutionDomain.Logic);
            var clip1 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track1, "ca9b83e7-d1c4-4224-b62d-a51b4a4b9e0d", 0m, asset2, 2.3500000000931322574615478516m, 0m, 0m, 0m);
            var track2 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(TreeTrack), "a636f440-208c-47db-88c1-afe9792423df", "Decision", TimelineExecutionDomain.Logic);
            var clip2 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track2, "180ec5ff-8aca-43b2-93aa-32d2354b4851", 0.1000000000931322574615478516m, parts.graph2);
            var track3 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(TreeTrack), "00890b6c-bfe3-bf86-4efa-bf6596ca43bd", "Logic / Decision / Decision RecoveryOpen", TimelineExecutionDomain.Logic);
            var clip3 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track3, "b88aeaf5-d4a4-4a9d-a706-d6da6cb8072a", 0.9333333333488553762435913086m, parts.graph4);
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph1, "066d593b6cd64cdb9de608744b830ca3", "RecoveryOpen", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Dodge/RecoveryOpen", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryOpen", "DodgeRecoveryCancel", 7002UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph1, "131816a77d7344d7b03d11917cb9c75d", "DodgeBackIFrame", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "IFrame", "DodgeBackIFrame", 1UL));
            BtsmtlSkillAuthoringContract.Apply(node4, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", timeline) });
            BtsmtlSkillAuthoringContract.Apply(node8, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph3, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringContract.Apply(node10, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "131816a77d7344d7b03d11917cb9c75d"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "93e14509-b248-44cc-a263-78c434016564"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node10, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(node14, "b", 2.35000014f);
            BtsmtlSkillAuthoringContract.Apply(node18, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph5, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringContract.Apply(node20, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "066d593b6cd64cdb9de608744b830ca3"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "93e14509-b248-44cc-a263-78c434016564"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node20, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(node24, "b", 2.35000014f);
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip1, new[] { new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "DodgeBack"), new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, asset2), new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, 2.35f) });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip2, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, parts.graph2) });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip3, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, parts.graph4) });
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node3, "Output", node4, "Input", "1d4bfe96-f332-4168-a910-fecb32dd4a43");
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node7, "Output", node8, "Input", "7c05230e2e2b47b2ad1ef2bee52f0d25");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node8, "end", node9, "Input", "55e1311bb3a24f49b89941cae34c0345");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node8, "body", node10, "Input", "bad5919ff7dd4a3fb7a637f9ac66366a");
            var edge5 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node13, "m_Output", node14, "a", "9c5982c2ca594fc3b0c934965063fa90");
            var edge6 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node14, "Value", node15, "m_Result", "c60900a6987c44ec8d93e99055f805a3");
            var edge7 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node17, "Output", node18, "Input", "8c6c026b56c04e76a67ff587d998ba15");
            var edge8 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node18, "end", node19, "Input", "08ec4e53cdfb4163b2ccbded8ac1dfdc");
            var edge9 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node18, "body", node20, "Input", "2b02e13ce02a4414aa4df24f710e46e7");
            var edge10 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph5, node23, "m_Output", node24, "a", "b5194d47191c4061b679e60da1b24f0e");
            var edge11 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph5, node24, "Value", node25, "m_Result", "ce6b54d82409456f8b3b4ba10dc76675");
            var edge12 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph6, node26, "m_Output", node27, "m_Result", "797b1212-00ef-4290-9fb4-17258e039be7");
            return parts;
        }

        sealed class DodgeBackParts
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
