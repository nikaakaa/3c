using System;
using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushExplodeTimelineAuthoringCode
    {
        static Open_RushAttackHandoff__14Parts BuildOpen_RushAttackHandoff__14(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Open_RushAttackHandoff__14Parts();
            parts.graph = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "0e892811-e868-5a83-6173-273343bdf627", "Open RushAttackHandoff @14", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph1 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph, "a078b961a242aa314433311bd9931aec", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            parts.graph2 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph, "370e0d968c4006a590a53affcf493e5e", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Rush接招超过14帧");
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineEnableFlowNode), "01307e8c-ef8a-490d-b4f2-03023b2ddbdc", null, new Vector2(120f, 60f));
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDisableFlowNode), "09842c59-cca9-4782-99a1-e95a511dfa0e", null, new Vector2(120f, 460f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDestroyFlowNode), "693dee71-0043-496d-8c5c-6487fa76de05", null, new Vector2(120f, 660f));
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "a911a1cd-7774-74ab-3e3c-c94e914a1999", "技能入口", new Vector2(120f, 260f));
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "cf83d0cba3943dc1a138e29294eb593a", "结束片段", new Vector2(520f, 260f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillBlackboardSetFlowNode), "dc66d8e7-ec9e-52af-a696-cb6cc9b315db", "Open RushAttackHandoff", new Vector2(360f, 260f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillSelectorFlowNode), "e002f9fb5afaa9b3e28e91dc1c442c3b", "窗口执行或结束", new Vector2(280f, 260f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineTimeFlowNode), "08543e2e115a83fb8d1e3ef4b8a6c8ac", "Timeline时间", new Vector2(-360f, 0f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "3a8a532cfb6d7ed1ea5e92c196ae49a7", "到达结束时间", new Vector2(-100f, 0f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillConditionResultFlowNode), "e189c371-2e75-4a25-acc6-ab53aca233a6", null, new Vector2(600f, 180f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineTimeFlowNode), "45e9152be85a59345493b1ed7b52851e", "Timeline时间", new Vector2(-360f, 0f));
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillConditionResultFlowNode), "a66eae92-e6a7-48c9-933a-633ed0e43805", null, new Vector2(600f, 180f));
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "c4afb853800757616c10be85a48d91e5", "超过14帧", new Vector2(-100f, 0f));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph, "c0b6e090-4d93-9e2d-9659-95b8aede20b0", "RushAttackHandoffOpen", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Rush/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RushAttackHandoff", "RushAttackHandoffOpen", 8102UL));
            BtsmtlSkillAuthoringContract.Apply(node4, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "c0b6e090-4d93-9e2d-9659-95b8aede20b0"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "0e892811-e868-5a83-6173-273343bdf627"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node4, "m_Value", true);
            BtsmtlSkillAuthoringContract.Apply(node2, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph1, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", parts.graph2, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringCode.SetValue(node8, "b", 0.733333349f);
            BtsmtlSkillAuthoringCode.SetValue(node11, "b", 0.233333334f);
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node1, "Output", node2, "Input", "2eac3e9583703345c834bbccace2ddef");
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "body", node4, "Input", "4e848d73d047062d9ecd44457dbb1f33");
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "end", node3, "Input", "831173c8ee886e923338c98128fd9a83");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node7, "m_Output", node8, "a", "48eefc86ba1579843b8d2b775362c15c");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node8, "Value", node9, "m_Result", "d9ad97a1dcbec462b9b627cce5367067");
            var edge5 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node10, "m_Output", node11, "a", "ed5769636cc13d88a75123a84dd6ebad");
            var edge6 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node11, "Value", node12, "m_Result", "72750e03bc4a56b84372005a40b46274");
            return parts;
        }

        sealed class Open_RushAttackHandoff__14Parts
        {
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillFlowGraph graph1;
            internal BtsmtlSkillFlowGraph graph2;
        }
    }
}
