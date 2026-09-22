using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode
    {
        static Attack5_EndParts BuildAttack5_End(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack5_EndParts();
            parts.graph47 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "b67c56ac-a560-55d6-965d-80641f9975b0", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To End Condition");
            parts.graph54 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "3d3c1c9d-4f1f-536f-9ec1-8c8bb2eb0d20", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 End To Exit Condition");
            var node227 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph47, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "0c465468-7d09-5aee-aa6d-9d4a473c0444", "AND", new Vector2(-40f, 35f));
            var node230 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph47, typeof(BtsmtlSkillNativeNodeWrapper<NOT>), "695bf217-15b0-5d96-8f98-6511027930f7", "NOT", new Vector2(-240f, 100f));
            var node228 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph47, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "963a8c27-8799-51a5-84b6-47da9490f275", "AND", new Vector2(220f, 70f));
            var node226 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph47, typeof(BtsmtlSkillBlackboardBooleanFlowNode), "c493a942-5634-56c6-a222-a1e50daf6747", "Attack5EndBoundary", new Vector2(-520f, 0f));
            var node229 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph47, typeof(BtsmtlSkillConditionResultFlowNode), "e45652d9-a7af-5814-9ed2-b083c6a02d53", "条件结果", new Vector2(600f, 180f));
            var node257 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph54, typeof(BtsmtlSkillConditionResultFlowNode), "44f4b2cb-7234-5f01-95f3-f33985178fc3", "条件结果", new Vector2(600f, 180f));
            var node256 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph54, typeof(BtsmtlSkillStateRootCompletedFlowNode), "7660be45-c0c4-5f38-94de-6644dbaf579d", "状态主体已完成", new Vector2(-360f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node226, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "19735807-6d5f-5e7f-91c9-b841ccf1f71e"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            var edge107 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph47, node226, "m_Output", node227, "a", "63997c25-56e3-585e-bdb3-832605cb7392");
            var edge108 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph47, node226, "m_Output", node230, "value", "a24f5206-8852-5bd8-a293-2b3e18c43683");
            var edge109 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph47, node227, "Value", node228, "a", "11c22c5a-2794-5cf8-a311-95a5530e6a29");
            var edge110 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph47, node228, "Value", node229, "m_Result", "752c37c8-2fb8-538e-8298-e68436a6a302");
            var edge111 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph47, node230, "Value", node228, "b", "0f2a0964-87d8-52f7-8a6a-11d386d0215e");
            var edge118 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph54, node256, "m_Output", node257, "m_Result", "9181a9d0-8f52-5928-9542-84c91a34b1e4");
            return parts;
        }

        sealed class Attack5_EndParts
        {
            internal BtsmtlSkillFlowGraph graph47;
            internal BtsmtlSkillFlowGraph graph54;
        }
    }
}
