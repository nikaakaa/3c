using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {
        static Attack_Rush_Enhance_ExplodeParts BuildAttack_Rush_Enhance_Explode(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack_Rush_Enhance_ExplodeParts();
            parts.graph13 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "95f76c3e-8fa0-d8d2-10bf-9cc746cda136", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "SawExplode");
            var node42 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph13, typeof(BtsmtlSkillConditionResultFlowNode), "40af559c-8a92-e7e9-2b08-48b0a64667ac", "条件结果", new Vector2(600f, 180f));
            var node41 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph13, typeof(BtsmtlSkillActionEventReceivedFlowNode), "62699490-8e09-2d2b-9784-8385a84d553b", "SawExplode", new Vector2(-360f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node41, new[] { new BtsmtlSkillAuthoringFieldValue("eventId", "SawExplode") });
            var edge18 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph13, node41, "m_Output", node42, "m_Result", "6ed1e803-9f89-3ca3-1353-2db4a28d0b18");
            return parts;
        }

        sealed class Attack_Rush_Enhance_ExplodeParts
        {
            internal BtsmtlSkillFlowGraph graph13;
        }
    }
}
