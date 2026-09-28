using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceTimelineAuthoringCode
    {
        static Corin_Attack_Rush_Enhance_CamShake_E_01Parts BuildCorin_Attack_Rush_Enhance_CamShake_E_01(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Corin_Attack_Rush_Enhance_CamShake_E_01Parts();
            parts.graph = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "fa272cac-4cee-42cb-2ca3-89fbcd15a620", "Corin_Attack_Rush_Enhance_CamShake_E_01", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph1 = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "e2b45588-4831-d074-3a31-6ec048655d1f", "Corin_Attack_Rush_Enhance_CamShake_E_01", BtsmtlSkillFlowGraphRole.TimelineBody);
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDestroyFlowNode), "547a26ef-0aea-4d9d-96af-89ff5eaade12", null, new Vector2(120f, 660f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "660bff09-9dff-fd22-5525-21455130d664", "持续执行", new Vector2(0f, 0f));
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDisableFlowNode), "9b80940e-7eda-447c-9572-ed252a5e8b94", null, new Vector2(120f, 460f));
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineEnableFlowNode), "ad37b95a-86b8-9e6b-caf0-21d4ded4e374", "片段启用", new Vector2(0f, 100f));
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(RequestCameraEffectNode), "cd02877f-f12c-5dbc-c919-28984ce188ef", "Corin_Attack_Rush_Enhance_CamShake_E_01", new Vector2(220f, 0f));
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineEnableFlowNode), "38a41c46-4cde-ebe8-6b88-d0bbb6466de3", "片段启用", new Vector2(0f, 100f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineDisableFlowNode), "abd27f6f-106e-4073-9d37-b56ecd7f2e0b", null, new Vector2(120f, 460f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillRootFlowNode), "b42ba62f-2d92-37a1-a283-7900511c0e2d", "持续执行", new Vector2(0f, 0f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineDestroyFlowNode), "bebfeec7-98b7-4c10-bffe-379abea946f1", null, new Vector2(120f, 660f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(RequestCameraEffectNode), "d23a26bc-4224-171f-087c-70a91c84c51f", "Corin_Attack_Rush_Enhance_CamShake_E_01", new Vector2(220f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node1, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "c499d8c7db1140bd8575fba4b2755b2f"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Rush_Enhance_CamShake_E_01") });
            BtsmtlSkillAuthoringContract.Apply(node6, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "99f66ecc95ad45f9a442d8281c5be9e0"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Rush_Enhance_CamShake_E_01") });
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node, "Output", node1, "Input", "c937d290-0a85-92f5-cb85-eafb0edeebc7");
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node5, "Output", node6, "Input", "8a9e9ba6-e2b0-5727-945f-6fdce673ee58");
            return parts;
        }

        sealed class Corin_Attack_Rush_Enhance_CamShake_E_01Parts
        {
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillFlowGraph graph1;
        }
    }
}
