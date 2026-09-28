using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushTimelineAuthoringCode
    {
        static Corin_Attack_Rush_CamShake_E_01Parts BuildCorin_Attack_Rush_CamShake_E_01(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Corin_Attack_Rush_CamShake_E_01Parts();
            parts.graph2 = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "e6697a88-2b1a-8c0f-a50f-20534592c2d1", "Corin_Attack_Rush_CamShake_E_01", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph3 = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "903fdd6a-8055-02a2-68cc-91d60bddbe65", "Corin_Attack_Rush_CamShake_E_01", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph4 = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "bf5d35a2-8b8e-69c9-84b0-60e7f5b3def1", "Corin_Attack_Rush_CamShake_E_01", BtsmtlSkillFlowGraphRole.TimelineBody);
            parts.graph5 = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "9c65d36d-408c-c5fd-f54c-e64c78f227a9", "Corin_Attack_Rush_CamShake_E_01", BtsmtlSkillFlowGraphRole.TimelineBody);
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillRootFlowNode), "0c2ffdf9-fc1e-2fd6-536b-d485d3fdd56c", "持续执行", new Vector2(0f, 0f));
            var node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDestroyFlowNode), "1e12267f-8472-4690-a488-7f32162b2e8a", null, new Vector2(120f, 660f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineEnableFlowNode), "202b8259-91b1-9d35-d581-68bc3d0fe3c4", "片段启用", new Vector2(0f, 100f));
            var node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDisableFlowNode), "660d3958-8c3b-4645-bddf-ea25abb94522", null, new Vector2(120f, 460f));
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(RequestCameraEffectNode), "cdbffe94-a856-9297-8935-b1bfc2746bf5", "Corin_Attack_Rush_CamShake_E_01", new Vector2(220f, 0f));
            var node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineEnableFlowNode), "22677920-f939-e5c6-70a2-3bb9297a49de", "片段启用", new Vector2(0f, 100f));
            var node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(RequestCameraEffectNode), "7e7372d8-3855-5653-5de1-fe518b1f804e", "Corin_Attack_Rush_CamShake_E_01", new Vector2(220f, 0f));
            var node19 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineDestroyFlowNode), "8beab8de-ef02-4e75-a094-2106adb641cc", null, new Vector2(120f, 660f));
            var node18 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineDisableFlowNode), "eb9dde8a-4d0a-4ac7-b5f0-fc9551d93507", null, new Vector2(120f, 460f));
            var node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillRootFlowNode), "ffea03cf-4f6a-6103-a18d-7edb6b63423e", "持续执行", new Vector2(0f, 0f));
            var node22 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillRootFlowNode), "388d278d-70e2-2549-9a42-992ca9dbf386", "持续执行", new Vector2(0f, 0f));
            var node24 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineDestroyFlowNode), "4267d576-c00f-4f3f-8391-d9a4c752df26", null, new Vector2(120f, 660f));
            var node20 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineEnableFlowNode), "6ce19b42-5e9f-2fbd-fa45-1b2c2f0a4cdd", "片段启用", new Vector2(0f, 100f));
            var node21 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(RequestCameraEffectNode), "9fe45e8c-70fc-b9bf-71d2-ba978d4349d7", "Corin_Attack_Rush_CamShake_E_01", new Vector2(220f, 0f));
            var node23 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineDisableFlowNode), "c3e7f001-44f0-421b-af43-a3e77224b162", null, new Vector2(120f, 460f));
            var node26 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(RequestCameraEffectNode), "118770cc-a637-f401-32c6-db5910133e4e", "Corin_Attack_Rush_CamShake_E_01", new Vector2(220f, 0f));
            var node29 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillTimelineDestroyFlowNode), "18cefd00-a29c-4b7f-ab72-c30bc6ee9bc0", null, new Vector2(120f, 660f));
            var node28 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillTimelineDisableFlowNode), "4f3b565b-be74-4402-be62-7c5e7371edbd", null, new Vector2(120f, 460f));
            var node27 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillRootFlowNode), "b4adb882-a4da-08e6-dcdb-4c116c1321a0", "持续执行", new Vector2(0f, 0f));
            var node25 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillTimelineEnableFlowNode), "f19c7a3e-dbfd-119b-a13b-5c592d87f856", "片段启用", new Vector2(0f, 100f));
            BtsmtlSkillAuthoringContract.Apply(node11, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "c499d8c7db1140bd8575fba4b2755b2f"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Rush_CamShake_E_01") });
            BtsmtlSkillAuthoringContract.Apply(node16, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "99f66ecc95ad45f9a442d8281c5be9e0"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Rush_CamShake_E_01") });
            BtsmtlSkillAuthoringContract.Apply(node21, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "bb271ce3d52c4c8792ec5201ea48d453"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Rush_CamShake_E_01") });
            BtsmtlSkillAuthoringContract.Apply(node26, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "ce1c5c57d28e404db380538d0001fe19"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Rush_CamShake_E_01") });
            var edge5 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node10, "Output", node11, "Input", "a7da3379-ca62-ad46-a61f-b5a4aa15c5f5");
            var edge6 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node15, "Output", node16, "Input", "d11d6392-9d00-927a-e51a-5b4c9e42257b");
            var edge7 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node20, "Output", node21, "Input", "29cfd37c-f349-155b-9866-7df6062b2060");
            var edge8 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph5, node25, "Output", node26, "Input", "9cd95102-5e96-4dd3-c0d4-025df1a9e741");
            return parts;
        }

        sealed class Corin_Attack_Rush_CamShake_E_01Parts
        {
            internal BtsmtlSkillFlowGraph graph2;
            internal BtsmtlSkillFlowGraph graph3;
            internal BtsmtlSkillFlowGraph graph4;
            internal BtsmtlSkillFlowGraph graph5;
        }
    }
}
