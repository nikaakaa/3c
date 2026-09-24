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
            parts.graph69 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "b67c56ac-a560-55d6-965d-80641f9975b0", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To End Condition");
            parts.graph79 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "3d3c1c9d-4f1f-536f-9ec1-8c8bb2eb0d20", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 End To Exit Condition");
            var node332 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph69, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "0c465468-7d09-5aee-aa6d-9d4a473c0444", "AND", new Vector2(-40f, 35f));
            var node335 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph69, typeof(BtsmtlSkillNativeNodeWrapper<NOT>), "695bf217-15b0-5d96-8f98-6511027930f7", "NOT", new Vector2(-240f, 100f));
            var node333 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph69, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "963a8c27-8799-51a5-84b6-47da9490f275", "AND", new Vector2(220f, 70f));
            var node331 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph69, typeof(BtsmtlSkillBlackboardBooleanFlowNode), "c493a942-5634-56c6-a222-a1e50daf6747", "Attack5EndBoundary", new Vector2(-520f, 0f));
            var node334 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph69, typeof(BtsmtlSkillConditionResultFlowNode), "e45652d9-a7af-5814-9ed2-b083c6a02d53", "条件结果", new Vector2(600f, 180f));
            var node377 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillConditionResultFlowNode), "44f4b2cb-7234-5f01-95f3-f33985178fc3", "条件结果", new Vector2(600f, 180f));
            var node376 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillStateRootCompletedFlowNode), "7660be45-c0c4-5f38-94de-6644dbaf579d", "状态主体已完成", new Vector2(-360f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node331, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "19735807-6d5f-5e7f-91c9-b841ccf1f71e"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            var edge190 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph69, node331, "m_Output", node332, "a", "63997c25-56e3-585e-bdb3-832605cb7392");
            var edge191 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph69, node331, "m_Output", node335, "value", "a24f5206-8852-5bd8-a293-2b3e18c43683");
            var edge192 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph69, node332, "Value", node333, "a", "11c22c5a-2794-5cf8-a311-95a5530e6a29");
            var edge193 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph69, node333, "Value", node334, "m_Result", "752c37c8-2fb8-538e-8298-e68436a6a302");
            var edge194 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph69, node335, "Value", node333, "b", "0f2a0964-87d8-52f7-8a6a-11d386d0215e");
            var edge213 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, node376, "m_Output", node377, "m_Result", "9181a9d0-8f52-5928-9542-84c91a34b1e4");
            return parts;
        }

        sealed class Attack5_EndParts
        {
            internal BtsmtlSkillFlowGraph graph69;
            internal BtsmtlSkillFlowGraph graph79;
        }
    }
}
