using ThirdPersonSimulation;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchAttackGameplayAbilityAuthoringCode
    {
        static EndParts BuildEnd(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new EndParts();
            parts.graph5 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "a4b4718d-7de3-3f0a-2503-a5e11098eb21", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "ExplodeComplete Condition");
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge3, parts.graph5, 0, ProgramAbortPolicy.None, 0);
            parts.graph7 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "bfd4da16-db48-e661-9a5a-fa1f4a1471cc", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "EndComplete Condition");
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge4, parts.graph7, 0, ProgramAbortPolicy.None, 0);
            var node18 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillConditionResultFlowNode), "8b6167f3-dba9-4215-33a6-12a02c05cc15", "条件结果", new Vector2(600f, 180f));
            var node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillStateRootCompletedFlowNode), "c14c347d-5d46-9afa-8328-042449d0ffac", "状态主体已完成", new Vector2(-360f, 0f));
            var node24 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph7, typeof(BtsmtlSkillConditionResultFlowNode), "39f055ab-f09a-4d45-18d1-bb92b0bfedcf", "条件结果", new Vector2(600f, 180f));
            var node23 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph7, typeof(BtsmtlSkillStateRootCompletedFlowNode), "960931a1-c06d-088f-8e6f-4c1d53cca96c", "状态主体已完成", new Vector2(-360f, 0f));
            var edge8 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph5, node17, "m_Output", node18, "m_Result", "554c1966-3b23-5c06-85a1-d93e8b55c886");
            var edge10 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph7, node23, "m_Output", node24, "m_Result", "141066df-9138-91bd-3c5b-c2441039b12e");
            return parts;
        }

        sealed class EndParts
        {
            internal BtsmtlSkillFlowGraph graph5;
            internal BtsmtlSkillFlowGraph graph7;
        }
    }
}
