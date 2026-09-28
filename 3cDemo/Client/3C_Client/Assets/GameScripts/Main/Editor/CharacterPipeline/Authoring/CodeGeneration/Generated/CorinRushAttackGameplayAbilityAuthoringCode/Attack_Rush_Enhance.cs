using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {
        static Attack_Rush_EnhanceParts BuildAttack_Rush_Enhance(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack_Rush_EnhanceParts();
            parts.graph2 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "0f1eecd3-fbb5-3a0d-37dc-ac15517f9c3f", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Entry Badge_S01");
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillConditionResultFlowNode), "f60686e3-816c-599b-aa44-19ef7445b059", "条件结果", new Vector2(600f, 180f));
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillGameplayTagFlowNode), "f9328591-ddce-9d80-372b-9517ce860503", "Badge_S01", new Vector2(-520f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node5, new[] { new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:4a39b2d9de6248bc99baf9561c18716d"), new BtsmtlSkillAuthoringFieldValue("tagId", "Badge_S01") });
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node5, "m_Result", node6, "m_Result", "dd8316df-30a6-dfff-5f45-eba5d84d7d1c");
            return parts;
        }

        sealed class Attack_Rush_EnhanceParts
        {
            internal BtsmtlSkillFlowGraph graph2;
        }
    }
}
