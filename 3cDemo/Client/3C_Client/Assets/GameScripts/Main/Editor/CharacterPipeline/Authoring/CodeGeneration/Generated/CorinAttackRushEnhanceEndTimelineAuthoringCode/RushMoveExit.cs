using System;
using BTSMTL.Authoring.Blackboard;
using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceEndTimelineAuthoringCode
    {
        static RushMoveExitParts BuildRushMoveExit(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RushMoveExitParts();
            parts.graph2 = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "54b8d3e3-65aa-8330-13cb-5842b26fd9c4", "RushMoveExit", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph3 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph2, "3b0e92c0-e932-d8de-d77e-e05e5adb5de3", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "窗口结束");
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillRootFlowNode), "0d264a6f-3ca4-5651-1dff-da1d099fa635", "技能入口", new Vector2(0f, 0f));
            var node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "50210f64-a973-d4fa-0723-cf91e018091d", "结束片段", new Vector2(400f, -80f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineEnableFlowNode), "5446bb15-7882-47d9-9e70-5c7ee2bd4bfb", null, new Vector2(120f, 60f));
            var node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDestroyFlowNode), "61a1ec0c-bd25-442d-a95e-ff9a3a7b3568", null, new Vector2(120f, 660f));
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillSelectorFlowNode), "7b831b01-625d-cb9b-3c1b-169801483309", "窗口执行或结束", new Vector2(200f, 0f));
            var node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDisableFlowNode), "d4373416-a1af-4fad-8220-ef97a1b0f5b2", null, new Vector2(120f, 460f));
            var node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillBlackboardSetFlowNode), "e50c656c-a24d-4a8d-635f-587aa0ad5bee", "RushMoveExit", new Vector2(400f, 80f));
            var node18 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "3bd314ef-0d4a-7cc0-8d3a-5600b06cdf5c", "结束时间", new Vector2(200f, 0f));
            var node19 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillConditionResultFlowNode), "851346b9-712d-34eb-d22e-7c2223ffcc26", "条件结果", new Vector2(400f, 0f));
            var node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineTimeFlowNode), "b62ee8d7-d24a-8528-11a3-f075e05bb7c6", "Timeline时间", new Vector2(0f, 0f));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph2, "71b7fb59-94b0-f237-3fbc-f3ee2474661f", "RushMoveExit", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Rush/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RushMoveExit", "RushMoveExit", 8103UL));
            BtsmtlSkillAuthoringContract.Apply(node12, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph3, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringContract.Apply(node14, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "71b7fb59-94b0-f237-3fbc-f3ee2474661f"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "54b8d3e3-65aa-8330-13cb-5842b26fd9c4"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node14, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(node18, "b", 1.16666663f);
            var edge5 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node11, "Output", node12, "Input", "16b96421-edf3-46d6-34a3-c28ffdf9bb0e");
            var edge7 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node12, "body", node14, "Input", "524c11b5-c32c-d5d4-c53a-326e25751e46");
            var edge6 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node12, "end", node13, "Input", "61557c61-bf29-e974-4881-4d0e5800ee1a");
            var edge8 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node17, "m_Output", node18, "a", "73307dca-92ed-3c2a-883f-97fc7bc1116e");
            var edge9 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node18, "Value", node19, "m_Result", "0415922d-1613-b129-b886-5838a63db5f7");
            return parts;
        }

        sealed class RushMoveExitParts
        {
            internal BtsmtlSkillFlowGraph graph2;
            internal BtsmtlSkillFlowGraph graph3;
        }
    }
}
