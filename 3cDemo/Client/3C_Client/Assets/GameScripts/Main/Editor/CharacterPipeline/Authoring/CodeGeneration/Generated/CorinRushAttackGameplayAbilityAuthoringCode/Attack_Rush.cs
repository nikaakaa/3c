using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {
        static Attack_RushParts BuildAttack_Rush(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack_RushParts();
            parts.graph1 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "fe5fc3c8-f546-5019-7154-bd7ee4dc6275", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Entry To Attack_Rush");
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillConditionResultFlowNode), "2d98c02f-cc3a-7814-4bc1-7b199da0119d", "条件结果", new Vector2(600f, 180f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillActionRequestFlowNode), "b939103e-8e47-3d2e-ad8f-39f34479975d", "Has Rush Request", new Vector2(-520f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node2, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "Rush"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node2, "m_Output", node3, "m_Result", "32a058e4-3e89-1366-edf0-31e3ff555bbd");
            return parts;
        }

        sealed class Attack_RushParts
        {
            internal BtsmtlSkillFlowGraph graph1;
        }
    }
}
