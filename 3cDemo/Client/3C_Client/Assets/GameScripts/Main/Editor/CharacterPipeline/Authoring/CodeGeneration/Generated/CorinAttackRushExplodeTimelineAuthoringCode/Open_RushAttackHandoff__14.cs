using BTSMTL.Authoring.Blackboard;
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
            parts.graph3 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph, "b2d84acc-07c2-4e64-8843-1f3f3afcf86e", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Rush接招与移动重叠");
            parts.graph4 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph, "a996a0c9-6b01-4f48-b597-6875024b2b36", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Rush移动退出");
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineEnableFlowNode), "01307e8c-ef8a-490d-b4f2-03023b2ddbdc", null, new Vector2(120f, 60f));
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDisableFlowNode), "09842c59-cca9-4782-99a1-e95a511dfa0e", null, new Vector2(120f, 460f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDestroyFlowNode), "693dee71-0043-496d-8c5c-6487fa76de05", null, new Vector2(120f, 660f));
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "a911a1cd-7774-74ab-3e3c-c94e914a1999", "技能入口", new Vector2(120f, 260f));
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "cf83d0cba3943dc1a138e29294eb593a", "结束片段", new Vector2(520f, 260f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillBlackboardSetFlowNode), "dc66d8e7-ec9e-52af-a696-cb6cc9b315db", "Open RushAttackHandoff", new Vector2(360f, 260f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillSelectorFlowNode), "e002f9fb5afaa9b3e28e91dc1c442c3b", "窗口执行或结束", new Vector2(280f, 260f));
            var moveSet = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillBlackboardSetFlowNode), "02a6c3f2-9a88-49ce-94c6-73c9e1d293d8", "Open RushMoveExit", new Vector2(540f, 360f));
            var both = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillSequenceFlowNode), "a945bf4b-431c-4ee3-8bf1-2e7df6e6deed", "接招与移动窗口", new Vector2(360f, 120f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineTimeFlowNode), "08543e2e115a83fb8d1e3ef4b8a6c8ac", "Timeline时间", new Vector2(-360f, 0f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "3a8a532cfb6d7ed1ea5e92c196ae49a7", "到达结束时间", new Vector2(-100f, 0f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillConditionResultFlowNode), "e189c371-2e75-4a25-acc6-ab53aca233a6", null, new Vector2(600f, 180f));
            var bothTime = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineTimeFlowNode), "46bdd9b2-d2ba-472e-89f5-20438ce04d72", "Timeline时间", new Vector2(-360f, 0f));
            var moveOpen = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "4c5f1f8a-d0fe-45e6-9f01-6f3f1db1fd98", "到达42帧", new Vector2(-100f, -100f));
            var handoffOpen = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillNativeNodeWrapper<FloatLessThan>), "3cd379ba-56b5-45a0-8263-c68ae419031e", "小于44帧", new Vector2(-100f, 100f));
            var overlap = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "6980fcde-3e7d-4206-8f05-7974840ddbeb", "AND", new Vector2(220f, 0f));
            var bothResult = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillConditionResultFlowNode), "e609ad94-75de-4278-a042-e967a78291eb", null, new Vector2(600f, 180f));
            var moveTime = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineTimeFlowNode), "c33a6537-daeb-41f1-b894-97bf215371f0", "Timeline时间", new Vector2(-360f, 0f));
            var afterHandoff = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "0795d68e-33e7-4df0-af33-bb6e4ee033f0", "到达44帧", new Vector2(-100f, 0f));
            var moveResult = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillConditionResultFlowNode), "ae0b6daa-beb8-4fba-aecc-09826020d854", null, new Vector2(600f, 180f));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph, "c0b6e090-4d93-9e2d-9659-95b8aede20b0", "RushAttackHandoffOpen", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Rush/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RushAttackHandoff", "RushAttackHandoffOpen", 8102UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph, "e3771557-bf28-43be-b99a-2d0799c7d220", "RushMoveExitOpen", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Rush/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RushMoveExit", "RushMoveExitOpen", 8103UL));
            BtsmtlSkillAuthoringContract.Apply(node4, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "c0b6e090-4d93-9e2d-9659-95b8aede20b0"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "0e892811-e868-5a83-6173-273343bdf627"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(moveSet, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "e3771557-bf28-43be-b99a-2d0799c7d220"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "0e892811-e868-5a83-6173-273343bdf627"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node4, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(moveSet, "m_Value", true);
            BtsmtlSkillAuthoringContract.Apply(node2, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph1, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("both", "接招与移动", parts.graph3, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("move", "移动", parts.graph4, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "接招", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringContract.Apply(both, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("handoff", "接招", null, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("move", "移动", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringCode.SetValue(node8, "b", 1.05f);
            BtsmtlSkillAuthoringCode.SetValue(moveOpen, "b", 42f / 60f);
            BtsmtlSkillAuthoringCode.SetValue(handoffOpen, "b", 44f / 60f);
            BtsmtlSkillAuthoringCode.SetValue(afterHandoff, "b", 44f / 60f);
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node1, "Output", node2, "Input", "2eac3e9583703345c834bbccace2ddef");
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "body", node4, "Input", "4e848d73d047062d9ecd44457dbb1f33");
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "end", node3, "Input", "831173c8ee886e923338c98128fd9a83");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node7, "m_Output", node8, "a", "48eefc86ba1579843b8d2b775362c15c");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node8, "Value", node9, "m_Result", "d9ad97a1dcbec462b9b627cce5367067");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "both", both, "Input", "fb8a6a0d-d88a-4b36-a5bc-5e6ba935973c");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "move", moveSet, "Input", "fae59432-15b8-405d-bff1-9e83e9551e16");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, both, "handoff", node4, "Input", "daa0f8e5-124b-4d44-8259-c10ead8789e6");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, both, "move", moveSet, "Input", "dc61f57f-b678-4125-83e5-90501e67ecc9");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, bothTime, "m_Output", moveOpen, "a", "ce3e5b90-13b1-454c-8d7e-24872f4dcf6b");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, bothTime, "m_Output", handoffOpen, "a", "0fcb0799-22ca-493c-938f-eaf2278fc266");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, moveOpen, "Value", overlap, "a", "728b134a-e42a-472c-8c42-ad6c6e77b656");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, handoffOpen, "Value", overlap, "b", "f1c3f785-6eb1-4841-a62b-20b012ec43b1");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, overlap, "Value", bothResult, "m_Result", "213f9c77-8c4c-4575-9f70-3b7a6045eb3d");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, moveTime, "m_Output", afterHandoff, "a", "a2daeffc-9bf0-4181-ab37-8fc438b03328");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, afterHandoff, "Value", moveResult, "m_Result", "2a30edbe-9f9a-4002-b9c4-09e2f7116dca");
            return parts;
        }

        sealed class Open_RushAttackHandoff__14Parts
        {
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillFlowGraph graph1;
            internal BtsmtlSkillFlowGraph graph3;
            internal BtsmtlSkillFlowGraph graph4;
        }
    }
}
