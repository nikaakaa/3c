using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {
        static Attack_RushParts BuildAttack_Rush(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack_RushParts();
            parts.graph1 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "b29b2648-176f-3872-2498-d612a3980c4e", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Entry Without Badge_S01");
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillNativeNodeWrapper<NOT>), "942775c7-e01c-23e8-d556-ec49028886cf", "NOT", new Vector2(-240f, 0f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillConditionResultFlowNode), "9e6027b3-7a1e-c33b-a472-a74aff3b3f36", "条件结果", new Vector2(600f, 180f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillGameplayTagFlowNode), "daf2ebca-8e14-4635-4420-1942458dd100", "Badge_S01", new Vector2(-520f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node2, new[] { new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:4a39b2d9de6248bc99baf9561c18716d"), new BtsmtlSkillAuthoringFieldValue("tagId", "Badge_S01") });
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node2, "m_Result", node3, "value", "2b118f75-e8ec-dd4d-10dc-a0e672bbb19b");
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node3, "Value", node4, "m_Result", "c9389271-d008-f330-4b2a-8d841cf84326");
            return parts;
        }

        sealed class Attack_RushParts
        {
            internal BtsmtlSkillFlowGraph graph1;
        }
    }
}
