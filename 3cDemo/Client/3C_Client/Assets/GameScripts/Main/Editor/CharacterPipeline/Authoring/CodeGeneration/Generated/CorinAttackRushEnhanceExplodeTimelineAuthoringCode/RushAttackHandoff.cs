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
        static RushAttackHandoffParts BuildRushAttackHandoff(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RushAttackHandoffParts();
            parts.graph = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "ea5e29ec-95d8-0e8f-bac4-c3f79eb2cfc7", "RushAttackHandoff", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph1 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph, "f34a4b1d-ce5a-b888-1040-43821df6e956", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "窗口结束");
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDisableFlowNode), "0e62c8a6-bb81-4207-88bc-4ccb6714632e", null, new Vector2(120f, 460f));
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "4d8615bd-0330-2aab-79b8-8e2f6e53a853", "结束片段", new Vector2(400f, -80f));
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineEnableFlowNode), "61ed466c-1298-45d3-b8d1-b7eff176a382", null, new Vector2(120f, 60f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillBlackboardSetFlowNode), "75082ff4-d534-1242-e8f6-c8f590e7021f", "RushAttackHandoff", new Vector2(400f, 80f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillSelectorFlowNode), "7c625660-113f-5c02-1e42-8556dd67478b", "窗口执行或结束", new Vector2(200f, 0f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDestroyFlowNode), "93ecb0b2-4cf1-4fc6-a1eb-8958c2ba1313", null, new Vector2(120f, 660f));
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "b7865d97-eda7-34be-4019-7cb85a57acfe", "技能入口", new Vector2(0f, 0f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillConditionResultFlowNode), "1cdbf314-9f26-3b55-78f0-3f74282c67b5", "条件结果", new Vector2(400f, 0f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineTimeFlowNode), "1d3fb0e0-0dd5-03fd-f120-d8f156f96c1f", "Timeline时间", new Vector2(0f, 0f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "d1d4ee2e-29e2-a6ee-9ff3-94a693721c41", "结束时间", new Vector2(200f, 0f));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph, "c07839bc-5804-2762-ddb8-7942f2d9c46a", "RushAttackHandoff", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Rush/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RushAttackHandoff", "RushAttackHandoff", 8102UL));
            BtsmtlSkillAuthoringContract.Apply(node4, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "c07839bc-5804-2762-ddb8-7942f2d9c46a"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "ea5e29ec-95d8-0e8f-bac4-c3f79eb2cfc7"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node4, "m_Value", true);
            BtsmtlSkillAuthoringContract.Apply(node2, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph1, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringCode.SetValue(node8, "b", 0.733333349f);
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node1, "Output", node2, "Input", "96579de8-20ab-5d67-e8e7-10e7ee94ca8f");
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "body", node4, "Input", "7557fed9-6d74-ae3f-9d98-52d5e0c9ce80");
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "end", node3, "Input", "8455162c-2a42-3a79-1bf5-5645d86a2fbc");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node7, "m_Output", node8, "a", "c2c6b1e0-53b4-1ea1-778b-669db6796389");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node8, "Value", node9, "m_Result", "1d13e6ad-fd23-5a1f-4d92-c711efc3c58e");
            return parts;
        }

        sealed class RushAttackHandoffParts
        {
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillFlowGraph graph1;
        }
    }
}
