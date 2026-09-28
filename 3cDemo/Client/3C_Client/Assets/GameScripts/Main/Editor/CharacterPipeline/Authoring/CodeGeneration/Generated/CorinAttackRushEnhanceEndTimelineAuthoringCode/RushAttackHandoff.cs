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
        static RushAttackHandoffParts BuildRushAttackHandoff(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RushAttackHandoffParts();
            parts.graph = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "fb64bd0f-d550-0939-8fe6-c65a84ad747d", "RushAttackHandoff", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph1 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph, "132e8045-7c03-6588-ee86-8130ea5a8573", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "窗口结束");
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillSelectorFlowNode), "34dae8cb-d042-465c-77f7-2b07d7507ee1", "窗口执行或结束", new Vector2(200f, 0f));
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDisableFlowNode), "75018d06-9729-47e7-adb1-b66bbfdcb500", null, new Vector2(120f, 460f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillBlackboardSetFlowNode), "83661d24-1ff4-1f1c-90c1-b45328a13085", "RushAttackHandoff", new Vector2(400f, 80f));
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "9904e3fb-99d7-929b-a468-8cc773453365", "技能入口", new Vector2(0f, 0f));
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "9e572b24-266d-9908-1ee1-b49ab75f8f67", "结束片段", new Vector2(400f, -80f));
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineEnableFlowNode), "ccacfe56-a344-4ac2-8968-005110cbd57c", null, new Vector2(120f, 60f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDestroyFlowNode), "da417f10-4e44-44c6-a3b1-376b0f39ac48", null, new Vector2(120f, 660f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "36a37aa2-c7f8-f4a9-0bc2-c61b7968b458", "结束时间", new Vector2(200f, 0f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineTimeFlowNode), "56fab701-120d-0628-4304-65997d9e8849", "Timeline时间", new Vector2(0f, 0f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillConditionResultFlowNode), "efd26db6-4ced-c06e-62f8-16989f1a6791", "条件结果", new Vector2(400f, 0f));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph, "9ac1b3d7-1e9f-2633-808f-a4634d3f22f2", "RushAttackHandoff", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Rush/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RushAttackHandoff", "RushAttackHandoff", 8102UL));
            BtsmtlSkillAuthoringContract.Apply(node2, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph1, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringContract.Apply(node4, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "9ac1b3d7-1e9f-2633-808f-a4634d3f22f2"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "fb64bd0f-d550-0939-8fe6-c65a84ad747d"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node4, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(node8, "b", 0.6666667f);
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node1, "Output", node2, "Input", "4f2341ca-54c0-622e-a6f0-05f2c792363d");
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "end", node3, "Input", "55e72816-4ea4-73c2-aa38-7d2e5ff5971d");
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "body", node4, "Input", "feec0baf-b6da-24a1-7947-a56185c3972b");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node7, "m_Output", node8, "a", "1fd12e9a-fa60-81c3-8845-2d7027c6c94e");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node8, "Value", node9, "m_Result", "6e85fe77-dd92-19f3-ce1e-d3c5b3c6fa0f");
            return parts;
        }

        sealed class RushAttackHandoffParts
        {
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillFlowGraph graph1;
        }
    }
}
