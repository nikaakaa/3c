using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchStartTimelineAuthoringCode
    {
        static Corin_Attack_Branch_02_CamShake_E_01Parts BuildCorin_Attack_Branch_02_CamShake_E_01(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Corin_Attack_Branch_02_CamShake_E_01Parts();
            parts.graph2 = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "c818dc0f-ad73-ca26-f90a-36eb2bcee749", "Corin_Attack_Branch_02_CamShake_E_01", BtsmtlSkillFlowGraphRole.TimelineBody);
            var node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDestroyFlowNode), "569a98b3-4b47-42a7-bbca-0dd2e8d53644", null, new Vector2(120f, 660f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineEnableFlowNode), "754bdd42-9f10-022d-f593-70850d200138", "片段启用", new Vector2(0f, 100f));
            var node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDisableFlowNode), "7daf819d-5aa6-43d7-9861-dd3bde0ae040", null, new Vector2(120f, 460f));
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(RequestCameraEffectNode), "a478bbcd-93fd-ff4e-6ee6-763c51e4db85", "Corin_Attack_Branch_02_CamShake_E_01", new Vector2(220f, 0f));
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillRootFlowNode), "b69eea7e-172a-55d4-61cd-b9fba87c0b88", "持续执行", new Vector2(0f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node11, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "0402de0f1aab47a5a388bc1f809d66bc"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Branch_02_CamShake_E_01") });
            var edge5 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node10, "Output", node11, "Input", "5a56b4d4-cc67-633f-08ee-4be3c958fb30");
            return parts;
        }

        sealed class Corin_Attack_Branch_02_CamShake_E_01Parts
        {
            internal BtsmtlSkillFlowGraph graph2;
        }
    }
}
