using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchExplodeTimelineAuthoringCode
    {
        static Corin_Attack_Branch_02_CamShake_E_02Parts BuildCorin_Attack_Branch_02_CamShake_E_02(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Corin_Attack_Branch_02_CamShake_E_02Parts();
            parts.graph = BtsmtlSkillAuthoringCode.EnsureTimelineGraph(rootParts.timeline, "81bafe67-d9c4-41ca-c4db-af0f7a44bee7", "Corin_Attack_Branch_02_CamShake_E_02", BtsmtlSkillFlowGraphRole.TimelineBody);
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineEnableFlowNode), "2e40c8da-9101-e6ec-9ddc-507e42a4aefe", "片段启用", new Vector2(0f, 100f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDestroyFlowNode), "63d7a4c3-c967-41ad-81ca-88dd1d6d5ca3", null, new Vector2(120f, 660f));
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(RequestCameraEffectNode), "b8e45e73-2f21-cbc4-5c5d-c00636ffe918", "Corin_Attack_Branch_02_CamShake_E_02", new Vector2(220f, 0f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "bf92bf54-bc51-6704-7421-7d7c65b9fe58", "持续执行", new Vector2(0f, 0f));
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillTimelineDisableFlowNode), "c1e5b641-ef9c-4a49-933f-aace124b6579", null, new Vector2(120f, 460f));
            BtsmtlSkillAuthoringContract.Apply(node1, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "4ba9ccdb0a8e450c8f7d29718db00688"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Branch_02_CamShake_E_02") });
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node, "Output", node1, "Input", "abc6accb-3c31-edf8-f36a-bc9bafd6769c");
            return parts;
        }

        sealed class Corin_Attack_Branch_02_CamShake_E_02Parts
        {
            internal BtsmtlSkillFlowGraph graph;
        }
    }
}
