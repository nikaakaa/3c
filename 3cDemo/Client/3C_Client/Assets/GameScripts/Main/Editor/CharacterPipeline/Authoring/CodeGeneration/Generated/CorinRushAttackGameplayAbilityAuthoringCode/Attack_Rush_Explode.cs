using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {
        static Attack_Rush_ExplodeParts BuildAttack_Rush_Explode(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack_Rush_ExplodeParts();
            parts.graph3 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "faba30d8-c53d-b3c2-90d2-a484bb704d92", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack_Rush Release");
            parts.graph4 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "7533340e-1fbe-040f-cd26-bda0a91bdc0c", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack_Rush Terminal");
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillBooleanInputFlowNode), "1914af7e-8470-7b2c-1945-ec3689ec90f1", "RushHeld", new Vector2(-520f, 120f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillActionWindowActiveFlowNode), "b68ce95b-79e9-c12c-17ed-73c7105d4405", "Window RushRelease", new Vector2(-520f, 0f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "bcf27c63-c7dd-6f20-8e3a-6d0ced1f17c8", "AND", new Vector2(120f, 60f));
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillNativeNodeWrapper<NOT>), "ccdc4afe-9a1a-c18a-f109-3c6b1aa8534e", "NOT", new Vector2(-240f, 120f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillConditionResultFlowNode), "f8591ee3-7002-f816-c60e-39f0a20a6c1b", "条件结果", new Vector2(600f, 180f));
            var node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillConditionResultFlowNode), "02412cb7-edc0-80ef-224d-487968aa253f", "条件结果", new Vector2(600f, 180f));
            var node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillStateRootCompletedFlowNode), "fb3bb13e-f4f3-65e2-234c-6848c7709a1c", "状态主体已完成", new Vector2(-360f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node11, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "RushHeld"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node8, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RushRelease") });
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node8, "m_Output", node9, "a", "c6c5c063-cad9-40c7-82dc-a2fd35fbaeda");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node9, "Value", node10, "m_Result", "6a4c65f0-062b-2154-92d8-87d2a68a20bb");
            var edge5 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node11, "m_Output", node12, "value", "039e56d0-1b64-91ed-095b-2019f6cf8c7e");
            var edge6 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node12, "Value", node9, "b", "85c2dfc3-df48-aa7e-8462-c2b1c0bd50c8");
            var edge7 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node13, "m_Output", node14, "m_Result", "6ada550c-6e77-a300-675b-1857c7dd1c6b");
            return parts;
        }

        sealed class Attack_Rush_ExplodeParts
        {
            internal BtsmtlSkillFlowGraph graph3;
            internal BtsmtlSkillFlowGraph graph4;
        }
    }
}
