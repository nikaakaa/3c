using System;
using BTSMTL.Authoring.Blackboard;
using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceExplodeEndTimelineAuthoringCode
    {
        static RushMoveExitParts BuildRushMoveExit(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RushMoveExitParts();
            parts.graph = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "2d7a8392-226f-cff3-ce0e-50090b2119ca", "RushMoveExit", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph1 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph, "464a152c-33d4-d6da-3f5f-1582995752cf", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "窗口结束");
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineEnableFlowNode), "27c44ecf-3fcc-4150-a8ca-9ef935c9af40", null, new Vector2(120f, 60f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillBlackboardSetFlowNode), "4d7298fe-c064-c062-62e6-4ac12997a124", "RushMoveExit", new Vector2(400f, 80f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillSelectorFlowNode), "57893ebb-db34-e079-7028-b985721e563e", "窗口执行或结束", new Vector2(200f, 0f));
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDisableFlowNode), "6080099d-5ee6-468f-aece-b4d4394a61fd", null, new Vector2(120f, 460f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDestroyFlowNode), "9e474e2b-551f-45c1-841f-c0d5d259a568", null, new Vector2(120f, 660f));
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "aa7a31c7-1f5b-d0af-9a8d-0b702d651701", "结束片段", new Vector2(400f, -80f));
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "cc02cac5-0392-4883-5cc7-56ae982b885f", "技能入口", new Vector2(0f, 0f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillConditionResultFlowNode), "231d8e07-9f0b-22eb-6477-057851b5a331", "条件结果", new Vector2(400f, 0f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "7227283a-2a7b-3dd1-5eab-a90464d732c4", "结束时间", new Vector2(200f, 0f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineTimeFlowNode), "f148ecc5-61cf-f1e5-ab8e-e86fc9a31aa9", "Timeline时间", new Vector2(0f, 0f));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph, "7530d1f8-64fb-efdc-2966-bc1b42dd8b32", "RushMoveExit", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Rush/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RushMoveExit", "RushMoveExit", 8103UL));
            BtsmtlSkillAuthoringContract.Apply(node4, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "7530d1f8-64fb-efdc-2966-bc1b42dd8b32"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "2d7a8392-226f-cff3-ce0e-50090b2119ca"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node4, "m_Value", true);
            BtsmtlSkillAuthoringContract.Apply(node2, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph1, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringCode.SetValue(node8, "b", 1.9666667f);
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node1, "Output", node2, "Input", "8c986a81-2585-9a44-d96e-653678e4a997");
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "body", node4, "Input", "7df0fc7d-2734-b148-f4a3-f630eb21fb8f");
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "end", node3, "Input", "e03d854d-b8f6-d963-c2bc-2fcbf17fd3ba");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node7, "m_Output", node8, "a", "8b83013d-28ab-19d6-a675-dc84548d739d");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node8, "Value", node9, "m_Result", "427d635a-4259-3445-862b-ab66bb940a5f");
            return parts;
        }

        sealed class RushMoveExitParts
        {
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillFlowGraph graph1;
        }
    }
}
