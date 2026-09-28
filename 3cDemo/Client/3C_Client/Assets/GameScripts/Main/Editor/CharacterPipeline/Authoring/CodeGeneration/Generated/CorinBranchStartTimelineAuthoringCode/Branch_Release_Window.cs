using System;
using BTSMTL.Authoring.Blackboard;
using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchStartTimelineAuthoringCode
    {
        static Branch_Release_WindowParts BuildBranch_Release_Window(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Branch_Release_WindowParts();
            parts.graph = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "bb481e4b-436e-3343-2edb-50137d4bb1e2", "Branch Release Window", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph1 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph, "082ca2e5-2cdf-9ac2-9839-d10962564a01", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Release Window End");
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "3ad7a2c9-813f-c445-7041-89a55df20ded", "结束片段", new Vector2(400f, -80f));
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineEnableFlowNode), "42cab1d2-1c08-4f0e-a1d7-87fae9e1c9a8", null, new Vector2(120f, 60f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillBlackboardSetFlowNode), "4a7f4ef0-e213-83ca-e368-356a789aa8f5", "Open BranchRelease", new Vector2(400f, 80f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillSelectorFlowNode), "5a054e09-4e58-7bb2-05d7-3eab16ed528f", "窗口执行或结束", new Vector2(200f, 0f));
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDisableFlowNode), "646a00f5-3b57-4c5b-8239-3a163e429ff5", null, new Vector2(120f, 460f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDestroyFlowNode), "65677a73-3269-4d58-bbee-19358e887343", null, new Vector2(120f, 660f));
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "dc7b3393-ff63-4759-271b-4130d0af5642", "技能入口", new Vector2(0f, 0f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineTimeFlowNode), "522ee7fa-5aed-53d1-5b3a-720d09f3d847", "Timeline时间", new Vector2(0f, 0f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "8f5667e3-92d3-0007-eab3-57ad3f28297f", "结束时间", new Vector2(200f, 0f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillConditionResultFlowNode), "cd129aa0-f07c-6a18-1b00-a73c7fc47eb7", "条件结果", new Vector2(400f, 0f));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph, "8253d70c-2187-afdf-2057-8805682fd389", "BranchRelease", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Branch/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "BranchRelease", "BranchRelease", 9001UL));
            BtsmtlSkillAuthoringContract.Apply(node4, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "8253d70c-2187-afdf-2057-8805682fd389"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "bb481e4b-436e-3343-2edb-50137d4bb1e2"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node4, "m_Value", true);
            BtsmtlSkillAuthoringContract.Apply(node2, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph1, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringCode.SetValue(node8, "b", 1.0999999f);
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node1, "Output", node2, "Input", "646715ea-fd58-5c23-8e58-2bafc854f101");
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "end", node3, "Input", "0d43a83a-2ea0-f922-6631-e71fdf3ef065");
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "body", node4, "Input", "e095dc9c-49a3-8c0a-f56b-5c0b67bc6162");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node7, "m_Output", node8, "a", "d7679108-7a17-f7c2-3fa7-7d83ea48215c");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node8, "Value", node9, "m_Result", "9943d62c-5a44-394b-2e62-7ac8d79cadfb");
            return parts;
        }

        sealed class Branch_Release_WindowParts
        {
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillFlowGraph graph1;
        }
    }
}
