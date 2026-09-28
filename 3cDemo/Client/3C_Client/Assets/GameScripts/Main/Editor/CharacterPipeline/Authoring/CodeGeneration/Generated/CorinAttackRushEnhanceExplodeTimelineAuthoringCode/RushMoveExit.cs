using System;
using BTSMTL.Authoring.Blackboard;
using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceExplodeTimelineAuthoringCode
    {
        static RushMoveExitParts BuildRushMoveExit(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RushMoveExitParts();
            parts.graph2 = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "f88ddc9d-8914-49c7-4bd4-c9023b43d79f", "RushMoveExit", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph3 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph2, "6184189f-4dfc-eeb2-4683-27983d563fa9", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "窗口结束");
            var node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDestroyFlowNode), "3fe012fe-04cd-4867-925f-a48ede90dddf", null, new Vector2(120f, 660f));
            var node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillBlackboardSetFlowNode), "8dcb6041-2fe0-b234-069f-8670ce68e1f9", "RushMoveExit", new Vector2(400f, 80f));
            var node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "91d2300b-7ff7-cb37-a5f2-66df24446028", "结束片段", new Vector2(400f, -80f));
            var node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDisableFlowNode), "adf69153-ee9a-4829-a756-a1e0b1785d16", null, new Vector2(120f, 460f));
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillSelectorFlowNode), "af0b877d-cae8-7a45-b039-62873ed41e01", "窗口执行或结束", new Vector2(200f, 0f));
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillRootFlowNode), "e89aef47-d825-e250-549b-a869f9b012e9", "技能入口", new Vector2(0f, 0f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineEnableFlowNode), "eca3e9bc-8c2a-46ac-acee-96679e56a65b", null, new Vector2(120f, 60f));
            var node19 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillConditionResultFlowNode), "6fa2bc67-2dd0-f758-f9c3-934d24c73d7d", "条件结果", new Vector2(400f, 0f));
            var node18 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "70000659-506d-20a5-140b-57ce82caecd7", "结束时间", new Vector2(200f, 0f));
            var node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineTimeFlowNode), "7a45ffcb-c5c4-b406-c28e-0a449396e24d", "Timeline时间", new Vector2(0f, 0f));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph2, "30e756a5-027f-7657-193c-233abd586e6b", "RushMoveExit", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Rush/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RushMoveExit", "RushMoveExit", 8103UL));
            BtsmtlSkillAuthoringContract.Apply(node14, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "30e756a5-027f-7657-193c-233abd586e6b"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "f88ddc9d-8914-49c7-4bd4-c9023b43d79f"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node14, "m_Value", true);
            BtsmtlSkillAuthoringContract.Apply(node12, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph3, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringCode.SetValue(node18, "b", 1.05f);
            var edge5 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node11, "Output", node12, "Input", "a53f7e04-6620-c7e4-1765-468b70a76891");
            var edge7 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node12, "body", node14, "Input", "19411d22-c9cd-0fc0-bf84-e2ed27f8116c");
            var edge6 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node12, "end", node13, "Input", "416d23f3-79cd-2a36-a500-b94d04f8568e");
            var edge8 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node17, "m_Output", node18, "a", "22baf6d0-926e-c1ee-5e89-c30a10960f56");
            var edge9 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node18, "Value", node19, "m_Result", "c2dc942b-b08e-7351-6be6-550b2f7d0755");
            return parts;
        }

        sealed class RushMoveExitParts
        {
            internal BtsmtlSkillFlowGraph graph2;
            internal BtsmtlSkillFlowGraph graph3;
        }
    }
}
