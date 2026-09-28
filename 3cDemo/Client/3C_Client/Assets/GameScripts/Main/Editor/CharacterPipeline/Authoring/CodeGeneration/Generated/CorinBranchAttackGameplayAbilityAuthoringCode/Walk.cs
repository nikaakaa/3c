using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchAttackGameplayAbilityAuthoringCode
    {
        static WalkParts BuildWalk(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new WalkParts();
            parts.graph10 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "8d679b99-05ed-69d1-cb44-e913b1796b2a", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Moving Condition");
            var node32 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph10, typeof(BtsmtlSkillInputMagnitudeFlowNode), "607f4707-fa09-ede1-34fd-cb0673e8a8b2", "MoveAxis Magnitude", new Vector2(-520f, 0f));
            var node33 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph10, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "cc505aaa-fa75-6c4c-8a19-800ea845d82e", ">", new Vector2(-240f, 0f));
            var node34 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph10, typeof(BtsmtlSkillConditionResultFlowNode), "e9a363c8-c22e-203f-01a7-391d4eb19d1e", "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringContract.Apply(node32, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "MoveAxis"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringCode.SetValue(node33, "b", 0.05f);
            var edge14 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph10, node32, "m_Output", node33, "a", "a5f6bcf4-d262-841e-17e8-98013080011a");
            var edge15 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph10, node33, "Value", node34, "m_Result", "7fd3af54-12fc-4222-dc62-3a6929531082");
            return parts;
        }

        sealed class WalkParts
        {
            internal BtsmtlSkillFlowGraph graph10;
        }
    }
}
