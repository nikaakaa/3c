using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode
    {
        static Attack5_End2Parts BuildAttack5_End2(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack5_End2Parts();
            parts.graph46 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "44049a03-c5d1-5f0c-996f-8b30eac669d3", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To End2 Condition");
            parts.graph49 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "a565bcba-7161-538b-b2d8-2a13f64a13ec", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 End2 To Exit Condition");
            var node222 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph46, typeof(BtsmtlSkillBlackboardBooleanFlowNode), "1febc74b-4ede-5666-aee4-ad303b94795d", "Attack5EndBoundary", new Vector2(-520f, 0f));
            var node223 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph46, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "99ca0387-6f32-5706-951e-a6a54dbe8cfb", "AND", new Vector2(40f, 35f));
            var node224 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph46, typeof(BtsmtlSkillConditionResultFlowNode), "dbb12885-8f8e-5529-905b-0ba8c42d76b9", "条件结果", new Vector2(600f, 180f));
            var node225 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph46, typeof(BtsmtlSkillBlackboardBooleanFlowNode), "e8d7d0cc-49ba-5fc7-9e09-3d571a25534f", "Attack5Hit", new Vector2(-520f, 100f));
            var node236 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph49, typeof(BtsmtlSkillConditionResultFlowNode), "bcb254f6-5401-5b88-9a2b-774ba8280d70", "条件结果", new Vector2(600f, 180f));
            var node235 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph49, typeof(BtsmtlSkillStateRootCompletedFlowNode), "ff33b21c-6845-5b31-b4a7-84e6f00da514", "状态主体已完成", new Vector2(-360f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node222, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "19735807-6d5f-5e7f-91c9-b841ccf1f71e"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(node225, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "6225401bc79441dca5eaab16bdbc0644"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            var edge105 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph46, node222, "m_Output", node223, "a", "37458f6e-7c14-5b56-a363-0431f6ef1ef9");
            var edge106 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph46, node223, "Value", node224, "m_Result", "5320d02d-70cf-5cd7-9a0d-42e20ea12b41");
            var edge107 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph46, node225, "m_Output", node223, "b", "e45db7d5-5b0c-52a2-9f22-39225b128b9e");
            var edge114 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph49, node235, "m_Output", node236, "m_Result", "ae717d21-6482-58da-bb1e-8f139e9a71a6");
            return parts;
        }

        sealed class Attack5_End2Parts
        {
            internal BtsmtlSkillFlowGraph graph46;
            internal BtsmtlSkillFlowGraph graph49;
        }
    }
}
