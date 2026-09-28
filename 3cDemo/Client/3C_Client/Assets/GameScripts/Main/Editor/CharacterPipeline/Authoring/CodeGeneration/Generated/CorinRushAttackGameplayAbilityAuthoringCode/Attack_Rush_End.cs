using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {
        static Attack_Rush_EndParts BuildAttack_Rush_End(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack_Rush_EndParts();
            parts.graph7 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "a09e5629-83c6-3ec2-2367-af2aa4048b73", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack_Rush_Explode Terminal");
            parts.graph9 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "94689eb0-04b3-3710-e511-6fc143d31c39", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack_Rush_End Terminal");
            parts.graph20 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "4e352d81-3c0a-2ee7-dc2c-9b6e910f71f1", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "状态主体已完成");
            var node24 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph7, typeof(BtsmtlSkillConditionResultFlowNode), "09177d2e-dc99-76e4-b7f7-9aedad98e9c9", "条件结果", new Vector2(600f, 180f));
            var node23 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph7, typeof(BtsmtlSkillStateRootCompletedFlowNode), "790d6d88-47aa-183e-3b5b-2aea6eeb5528", "状态主体已完成", new Vector2(-360f, 0f));
            var node29 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph9, typeof(BtsmtlSkillStateRootCompletedFlowNode), "988af7b5-38ee-e0d0-0196-1811845991c9", "状态主体已完成", new Vector2(-360f, 0f));
            var node30 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph9, typeof(BtsmtlSkillConditionResultFlowNode), "df2e016c-4ec6-02b6-c354-49e0386285b5", "条件结果", new Vector2(600f, 180f));
            var node62 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph20, typeof(BtsmtlSkillStateRootCompletedFlowNode), "42c9da54-a38d-ab50-b580-66fc78a7bd54", "状态主体已完成", new Vector2(-360f, 0f));
            var node63 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph20, typeof(BtsmtlSkillConditionResultFlowNode), "748cef4a-7de4-dd3a-9cd9-2bcecb1b29ec", "条件结果", new Vector2(600f, 180f));
            var edge12 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph7, node23, "m_Output", node24, "m_Result", "55db4864-553c-d3a1-74f7-721401df4dac");
            var edge14 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph9, node29, "m_Output", node30, "m_Result", "df7a33bd-8f2d-9a7f-fcd4-13a9a2247d11");
            var edge26 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph20, node62, "m_Output", node63, "m_Result", "e0c5afd0-5c15-75c6-4dae-e8993ffddc94");
            return parts;
        }

        sealed class Attack_Rush_EndParts
        {
            internal BtsmtlSkillFlowGraph graph7;
            internal BtsmtlSkillFlowGraph graph9;
            internal BtsmtlSkillFlowGraph graph20;
        }
    }
}
