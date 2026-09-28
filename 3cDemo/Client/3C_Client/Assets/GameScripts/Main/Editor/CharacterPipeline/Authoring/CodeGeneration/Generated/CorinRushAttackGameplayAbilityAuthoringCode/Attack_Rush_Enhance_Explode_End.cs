using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {
        static Attack_Rush_Enhance_Explode_EndParts BuildAttack_Rush_Enhance_Explode_End(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack_Rush_Enhance_Explode_EndParts();
            parts.graph16 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "c747d6ff-79b9-facf-e1fd-61e5b6b9c096", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "状态主体已完成");
            parts.graph18 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "9fe64dc8-6b19-f7b6-98f9-8119292ceebd", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "状态主体已完成");
            var node50 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph16, typeof(BtsmtlSkillStateRootCompletedFlowNode), "0f4dbd22-86df-becf-57e2-4eddb3ff9013", "状态主体已完成", new Vector2(-360f, 0f));
            var node51 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph16, typeof(BtsmtlSkillConditionResultFlowNode), "353dd2b8-6fce-6215-e31e-5f8d623a822a", "条件结果", new Vector2(600f, 180f));
            var node56 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph18, typeof(BtsmtlSkillStateRootCompletedFlowNode), "52d972d9-b1e8-3072-aa80-961ffce9036f", "状态主体已完成", new Vector2(-360f, 0f));
            var node57 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph18, typeof(BtsmtlSkillConditionResultFlowNode), "9353e5c4-af3e-452f-7a8e-13d1cb60b1e9", "条件结果", new Vector2(600f, 180f));
            var edge22 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph16, node50, "m_Output", node51, "m_Result", "19e8d9f9-e887-8838-5086-8af6bc2a6f26");
            var edge24 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph18, node56, "m_Output", node57, "m_Result", "bdfc8c70-96f0-ccce-7bfb-db01e0d07c68");
            return parts;
        }

        sealed class Attack_Rush_Enhance_Explode_EndParts
        {
            internal BtsmtlSkillFlowGraph graph16;
            internal BtsmtlSkillFlowGraph graph18;
        }
    }
}
