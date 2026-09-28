using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushExplodeTimelineAuthoringCode
    {
        static Corin_Attack_Rush_CamShake_E_02Parts BuildCorin_Attack_Rush_CamShake_E_02(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Corin_Attack_Rush_CamShake_E_02Parts();
            parts.graph4 = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "7db645ac-3ab0-01b0-e794-9dd6303bb909", "Corin_Attack_Rush_CamShake_E_02", BtsmtlSkillFlowGraphRole.TimelineBody);
            var node24 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineDestroyFlowNode), "013df1d3-8ed3-4622-b19d-980dca8859a7", null, new Vector2(120f, 660f));
            var node21 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(RequestCameraEffectNode), "34134f8d-02a2-1506-fd62-9078bbbc9d0f", "Corin_Attack_Rush_CamShake_E_02", new Vector2(220f, 0f));
            var node23 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineDisableFlowNode), "4dede53a-bacb-44cb-8b21-de2f61285431", null, new Vector2(120f, 460f));
            var node22 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillRootFlowNode), "81d12b3e-a3fa-c166-8197-615f6897e991", "持续执行", new Vector2(0f, 0f));
            var node20 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineEnableFlowNode), "e4729273-598d-1483-53e7-af3e4bb563fe", "片段启用", new Vector2(0f, 100f));
            BtsmtlSkillAuthoringContract.Apply(node21, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "47d440cddaae4e0a9857fd8e15f9ef26"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Rush_CamShake_E_02") });
            var edge16 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node20, "Output", node21, "Input", "03c47eb1-e534-82ad-9887-e9f695d53b20");
            return parts;
        }

        sealed class Corin_Attack_Rush_CamShake_E_02Parts
        {
            internal BtsmtlSkillFlowGraph graph4;
        }
    }
}
