using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchExplodeTimelineAuthoringCode
    {
        static Corin_Attack_Branch_02_CamShake_E_03Parts BuildCorin_Attack_Branch_02_CamShake_E_03(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Corin_Attack_Branch_02_CamShake_E_03Parts();
            parts.graph1 = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "73bb138d-e801-bcdc-8f05-b300e6d434e2", "Corin_Attack_Branch_02_CamShake_E_03", BtsmtlSkillFlowGraphRole.TimelineBody);
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineDestroyFlowNode), "58febc79-d50c-43a4-a59a-29640d5ec3f8", null, new Vector2(120f, 660f));
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineEnableFlowNode), "6d9c1772-7f84-632b-07e1-d2a3a420f827", "片段启用", new Vector2(0f, 100f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillRootFlowNode), "a3643331-1c55-cd09-8f6c-744993e9201d", "持续执行", new Vector2(0f, 0f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineDisableFlowNode), "da7b8f33-2c3b-470b-b9a6-0100b01ea066", null, new Vector2(120f, 460f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(RequestCameraEffectNode), "fe2380c6-da19-4686-3b60-50c00d49c54b", "Corin_Attack_Branch_02_CamShake_E_03", new Vector2(220f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node6, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "054e9e7550fc459c9397abc4a23c2b39"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Branch_02_CamShake_E_03") });
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node5, "Output", node6, "Input", "fb056843-ce40-79ba-f2a6-a2b0730da52c");
            return parts;
        }

        sealed class Corin_Attack_Branch_02_CamShake_E_03Parts
        {
            internal BtsmtlSkillFlowGraph graph1;
        }
    }
}
