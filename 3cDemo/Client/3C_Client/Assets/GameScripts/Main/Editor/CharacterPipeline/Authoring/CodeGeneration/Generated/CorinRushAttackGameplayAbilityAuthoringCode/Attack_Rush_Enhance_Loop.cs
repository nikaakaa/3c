using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {
        static Attack_Rush_Enhance_LoopParts BuildAttack_Rush_Enhance_Loop(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack_Rush_Enhance_LoopParts();
            parts.graph11 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "09706159-9152-caed-5251-06ee9a1f77b4", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到指定时间");
            var node36 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph11, typeof(BtsmtlSkillConditionResultFlowNode), "0cb833c2-eeb7-d987-2fb0-80485d40f69a", "条件结果", new Vector2(600f, 180f));
            var node35 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph11, typeof(BtsmtlSkillStateRootCompletedFlowNode), "6610fe2f-917f-a764-3dd1-be40634024dc", "状态主体已完成", new Vector2(-360f, 0f));
            var edge16 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph11, node35, "m_Output", node36, "m_Result", "1683a105-e09d-bdd2-93c2-02896fece37d");
            return parts;
        }

        sealed class Attack_Rush_Enhance_LoopParts
        {
            internal BtsmtlSkillFlowGraph graph11;
        }
    }
}
