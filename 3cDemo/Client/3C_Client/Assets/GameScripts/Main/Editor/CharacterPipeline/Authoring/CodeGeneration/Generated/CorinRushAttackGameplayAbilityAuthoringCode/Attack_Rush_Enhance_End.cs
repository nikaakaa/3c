using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {
        static Attack_Rush_Enhance_EndParts BuildAttack_Rush_Enhance_End(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack_Rush_Enhance_EndParts();
            parts.graph14 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "871f271f-33a8-2b8b-8a88-5a7581150c17", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Enhance Loop Released");
            var node45 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph14, typeof(BtsmtlSkillConditionResultFlowNode), "017bf1ae-b901-cb3d-d981-09176016ef65", "条件结果", new Vector2(600f, 180f));
            var node44 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph14, typeof(BtsmtlSkillNativeNodeWrapper<NOT>), "35a19981-5de3-c3ee-8669-1d81a7b2caff", "NOT", new Vector2(-240f, 180f));
            var node43 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph14, typeof(BtsmtlSkillBooleanInputFlowNode), "80706ada-c9b7-68c1-684e-60d57794aefd", "AttackHeld", new Vector2(-520f, 180f));
            BtsmtlSkillAuthoringContract.Apply(node43, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "AttackHeld"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            var edge19 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph14, node43, "m_Output", node44, "value", "7f7ac07f-fd2b-7445-83b1-5100162ffcce");
            var edge20 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph14, node44, "Value", node45, "m_Result", "b87c1c72-fd18-057c-c366-c5872a6c270b");
            return parts;
        }

        sealed class Attack_Rush_Enhance_EndParts
        {
            internal BtsmtlSkillFlowGraph graph14;
        }
    }
}
