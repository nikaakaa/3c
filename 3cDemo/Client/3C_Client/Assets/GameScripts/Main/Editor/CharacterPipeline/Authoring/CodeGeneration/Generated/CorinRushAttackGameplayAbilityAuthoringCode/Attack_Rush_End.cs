using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {
        static Attack_Rush_EndParts BuildAttack_Rush_End(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack_Rush_EndParts();
            parts.graph6 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "a09e5629-83c6-3ec2-2367-af2aa4048b73", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack_Rush_Explode Terminal");
            parts.graph8 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "94689eb0-04b3-3710-e511-6fc143d31c39", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack_Rush_End Terminal");
            var node20 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillConditionResultFlowNode), "09177d2e-dc99-76e4-b7f7-9aedad98e9c9", "条件结果", new Vector2(600f, 180f));
            var node19 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillStateRootCompletedFlowNode), "790d6d88-47aa-183e-3b5b-2aea6eeb5528", "状态主体已完成", new Vector2(-360f, 0f));
            var node25 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillStateRootCompletedFlowNode), "988af7b5-38ee-e0d0-0196-1811845991c9", "状态主体已完成", new Vector2(-360f, 0f));
            var node26 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillConditionResultFlowNode), "df2e016c-4ec6-02b6-c354-49e0386285b5", "条件结果", new Vector2(600f, 180f));
            var edge9 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph6, node19, "m_Output", node20, "m_Result", "55db4864-553c-d3a1-74f7-721401df4dac");
            var edge11 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph8, node25, "m_Output", node26, "m_Result", "df7a33bd-8f2d-9a7f-fcd4-13a9a2247d11");
            return parts;
        }

        sealed class Attack_Rush_EndParts
        {
            internal BtsmtlSkillFlowGraph graph6;
            internal BtsmtlSkillFlowGraph graph8;
        }
    }
}
