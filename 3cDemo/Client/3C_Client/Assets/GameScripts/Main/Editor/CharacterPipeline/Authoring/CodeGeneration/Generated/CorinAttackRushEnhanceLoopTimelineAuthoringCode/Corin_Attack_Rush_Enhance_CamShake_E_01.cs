using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceLoopTimelineAuthoringCode
    {
        static Corin_Attack_Rush_Enhance_CamShake_E_01Parts BuildCorin_Attack_Rush_Enhance_CamShake_E_01(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Corin_Attack_Rush_Enhance_CamShake_E_01Parts();
            parts.graph = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "d5a8b42d-31ae-628d-8630-b39978dfdbba", "Corin_Attack_Rush_Enhance_CamShake_E_01", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph1 = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "cb69dcd7-d2ab-08f4-00bb-8752ab90d5f5", "Corin_Attack_Rush_Enhance_CamShake_E_01", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph2 = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "8d345aeb-f419-aa38-a46d-aca37dc1f255", "Corin_Attack_Rush_Enhance_CamShake_E_01", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph3 = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "eed547cb-3541-1ceb-e61a-a2a7cf8a27ba", "Corin_Attack_Rush_Enhance_CamShake_E_01", BtsmtlSkillFlowGraphRole.TimelineBody);
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDisableFlowNode), "05d6f449-bcf8-4ebf-8f8e-14efec5f50c4", null, new Vector2(120f, 460f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "3f341caf-01ae-bcc8-eb05-3b45318dade4", "持续执行", new Vector2(0f, 0f));
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineEnableFlowNode), "4c025e43-35ef-990e-5041-91fd84e0b637", "片段启用", new Vector2(0f, 100f));
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(RequestCameraEffectNode), "c5f0e07e-b894-421b-7a59-88a62803793a", "Corin_Attack_Rush_Enhance_CamShake_E_01", new Vector2(220f, 0f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDestroyFlowNode), "d014cb1d-d999-4d0c-ad53-8100ff1e4b01", null, new Vector2(120f, 660f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineDestroyFlowNode), "450e58b4-d897-45c4-a320-5e15a50a416b", null, new Vector2(120f, 660f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillRootFlowNode), "9d001bbc-3a15-5aff-7c37-e5b1b9caf469", "持续执行", new Vector2(0f, 0f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(RequestCameraEffectNode), "a4f77c6d-9402-d6be-aefb-4ff781e7acc1", "Corin_Attack_Rush_Enhance_CamShake_E_01", new Vector2(220f, 0f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineDisableFlowNode), "ab774f7d-1213-4939-aebd-c1f71b71e630", null, new Vector2(120f, 460f));
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineEnableFlowNode), "f391756e-d006-9b7c-28fb-b7dac82ba941", "片段启用", new Vector2(0f, 100f));
            var node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDisableFlowNode), "33200d86-10f4-4364-bc16-b7792b21a882", null, new Vector2(120f, 460f));
            var node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDestroyFlowNode), "50af946c-8f88-4e55-ad7c-7a2c216e60d6", null, new Vector2(120f, 660f));
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillRootFlowNode), "56e2cf30-7f6a-2d7a-1542-43cd309b6a5f", "持续执行", new Vector2(0f, 0f));
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(RequestCameraEffectNode), "76aaa465-8f79-13de-b8ed-faa35df57e50", "Corin_Attack_Rush_Enhance_CamShake_E_01", new Vector2(220f, 0f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineEnableFlowNode), "88898e67-f32a-e4a4-c8f1-74e6170f2803", "片段启用", new Vector2(0f, 100f));
            var node18 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineDisableFlowNode), "5f80c5a8-2949-4050-8f59-16fd1367a7ce", null, new Vector2(120f, 460f));
            var node19 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineDestroyFlowNode), "7b25388d-07ef-4174-a52c-ae2c064ee310", null, new Vector2(120f, 660f));
            var node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(RequestCameraEffectNode), "e5bf0b68-ab10-71ff-b4b3-bb7bd12f85df", "Corin_Attack_Rush_Enhance_CamShake_E_01", new Vector2(220f, 0f));
            var node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillRootFlowNode), "f3b1efbd-4dcb-277a-d68a-ae71d40be2f4", "持续执行", new Vector2(0f, 0f));
            var node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineEnableFlowNode), "f8407dca-bce9-c703-940d-dc5c10db0478", "片段启用", new Vector2(0f, 100f));
            BtsmtlSkillAuthoringContract.Apply(node1, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "6389062f28824031838f413c9f9e8c23"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Rush_Enhance_CamShake_E_01") });
            BtsmtlSkillAuthoringContract.Apply(node6, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "76e6a15bd10c4cbdb954d651c47525f2"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Rush_Enhance_CamShake_E_01") });
            BtsmtlSkillAuthoringContract.Apply(node11, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "295794ddc2da490aa137ce264a1df980"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Rush_Enhance_CamShake_E_01") });
            BtsmtlSkillAuthoringContract.Apply(node16, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "3a98dd480a6343e79b15f0a4e6cfcc8d"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Rush_Enhance_CamShake_E_01") });
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node, "Output", node1, "Input", "572b764c-0313-883a-b567-a1e10ad2853e");
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node5, "Output", node6, "Input", "9afc422a-11e0-f55e-dcf0-575043c7c410");
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node10, "Output", node11, "Input", "a499caea-d41c-2e75-02eb-729eed54b488");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node15, "Output", node16, "Input", "d735e91e-d2d0-d1f5-0501-32089c8bb854");
            return parts;
        }

        sealed class Corin_Attack_Rush_Enhance_CamShake_E_01Parts
        {
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillFlowGraph graph1;
            internal BtsmtlSkillFlowGraph graph2;
            internal BtsmtlSkillFlowGraph graph3;
        }
    }
}
