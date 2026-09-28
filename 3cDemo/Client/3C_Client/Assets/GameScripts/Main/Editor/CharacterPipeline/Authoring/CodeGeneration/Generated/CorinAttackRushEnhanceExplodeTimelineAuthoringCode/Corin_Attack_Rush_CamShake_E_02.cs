using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceExplodeTimelineAuthoringCode
    {
        static Corin_Attack_Rush_CamShake_E_02Parts BuildCorin_Attack_Rush_CamShake_E_02(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Corin_Attack_Rush_CamShake_E_02Parts();
            parts.graph4 = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "87013632-fccb-4d87-1866-888d882e2be4", "Corin_Attack_Rush_CamShake_E_02", BtsmtlSkillFlowGraphRole.TimelineBody);
            var node21 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(RequestCameraEffectNode), "442164a1-974e-c750-d965-c6e83e26c72f", "Corin_Attack_Rush_CamShake_E_02", new Vector2(220f, 0f));
            var node24 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineDestroyFlowNode), "a7b8bd6d-bbb7-4083-96c3-014717783044", null, new Vector2(120f, 660f));
            var node20 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineEnableFlowNode), "b19ad106-3846-c3fe-1c5e-41a2f82e53f2", "片段启用", new Vector2(0f, 100f));
            var node23 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineDisableFlowNode), "dab66851-2f42-4716-a0ba-ffb1d1447ef0", null, new Vector2(120f, 460f));
            var node22 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillRootFlowNode), "f6a4c3b6-dabd-147e-5241-c6f700162d12", "持续执行", new Vector2(0f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node21, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "47d440cddaae4e0a9857fd8e15f9ef26"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Rush_CamShake_E_02") });
            var edge10 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node20, "Output", node21, "Input", "69ad20a6-7c41-246b-d22d-b0df82c4dda7");
            return parts;
        }

        sealed class Corin_Attack_Rush_CamShake_E_02Parts
        {
            internal BtsmtlSkillFlowGraph graph4;
        }
    }
}
