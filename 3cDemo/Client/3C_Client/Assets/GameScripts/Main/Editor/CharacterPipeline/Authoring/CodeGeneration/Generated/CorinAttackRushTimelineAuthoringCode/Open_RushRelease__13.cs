using System;
using BTSMTL.Authoring.Blackboard;
using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushTimelineAuthoringCode
    {
        static Open_RushRelease__13Parts BuildOpen_RushRelease__13(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Open_RushRelease__13Parts();
            parts.graph = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "a36ae29c-e40c-cb73-c4f0-76d48cca7b19", "Open RushRelease @13", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph1 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph, "ff9bb48b0a86fdeb587468c8a64a0135", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "588176fd56347925b53c0e1ce37b96bf", "结束片段", new Vector2(520f, 260f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillBlackboardSetFlowNode), "5ff7e22c-25ab-4ff1-da33-d305fd403eb7", "Open RushRelease", new Vector2(360f, 260f));
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineEnableFlowNode), "67fc193c-1f8a-459f-99be-c2674e0f807d", null, new Vector2(120f, 60f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDestroyFlowNode), "6b4dca86-bbd8-46b4-a5cd-d046ea7c4dc0", null, new Vector2(120f, 660f));
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "824479d8-fe60-97da-b837-7057b4ffb3c4", "技能入口", new Vector2(120f, 260f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillSelectorFlowNode), "a1801a530b3621874f90fcc30c1c2980", "窗口执行或结束", new Vector2(280f, 260f));
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDisableFlowNode), "fdfe95d8-c235-4b5d-9442-5c824dd83988", null, new Vector2(120f, 460f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineTimeFlowNode), "240d2a8b7d752774a688c297ae71d41d", "Timeline时间", new Vector2(-360f, 0f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "9e1c2a215105285fc3e17f3f53cb4541", "到达结束时间", new Vector2(-100f, 0f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillConditionResultFlowNode), "febbbc66-4d44-4b28-85d4-be0def2ef40a", null, new Vector2(600f, 180f));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(parts.graph, "2087927f-c32b-0c89-864a-fbf269fba556", "RushReleaseOpen", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Rush/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RushRelease", "RushReleaseOpen", 8101UL));
            BtsmtlSkillAuthoringContract.Apply(node4, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "2087927f-c32b-0c89-864a-fbf269fba556"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "a36ae29c-e40c-cb73-c4f0-76d48cca7b19"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node4, "m_Value", true);
            BtsmtlSkillAuthoringContract.Apply(node2, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph1, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringCode.SetValue(node8, "b", 1.16666663f);
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node1, "Output", node2, "Input", "6f1a8f7714e164cf8e97702130fb84bd");
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "end", node3, "Input", "a017b7ed23355b5d3f1e44326e2e4c78");
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node2, "body", node4, "Input", "fb4a7a8e524dad90c3ea2318e796e560");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node7, "m_Output", node8, "a", "4c3793fff2fc498c75f8bd420a6e5166");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node8, "Value", node9, "m_Result", "83b8cf6cf59ace8c57bfa762ea002fea");
            return parts;
        }

        sealed class Open_RushRelease__13Parts
        {
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillFlowGraph graph1;
        }
    }
}
