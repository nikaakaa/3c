using ThirdPersonSimulation;
using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchAttackGameplayAbilityAuthoringCode
    {
        static ExplodeParts BuildExplode(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new ExplodeParts();
            parts.graph2 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "df4ce173-d799-01fd-09d6-3ccf61ee49f1", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "StartRelease Condition");
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge1, parts.graph2, 100, ProgramAbortPolicy.None, 0);
            parts.graph9 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "14146bd4-eb66-78cb-9321-8b3f39bccad8", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Release Condition");
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge5, parts.graph9, 100, ProgramAbortPolicy.None, 0);
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillBooleanInputFlowNode), "03f3919f-1ae9-11dd-3bb6-1b317fd53755", "BranchHeld", new Vector2(-520f, 0f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "7a4e9072-0daf-449c-3623-85a81e4fa46a", "AND", new Vector2(200f, 100f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillNativeNodeWrapper<NOT>), "86908c72-bc7f-6759-9380-6045b8e17c75", "NOT", new Vector2(-240f, 0f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillConditionResultFlowNode), "dda5eda1-c952-a2b3-fbe7-81f7889baf57", "条件结果", new Vector2(600f, 180f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillActionWindowActiveFlowNode), "e56fe5a0-3a42-074c-0550-8aa11971acb1", "Branch Release Window", new Vector2(-240f, 160f));
            var node31 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph9, typeof(BtsmtlSkillConditionResultFlowNode), "14e58aaa-f7f4-de6e-ee9d-3b9808fe8f30", "条件结果", new Vector2(600f, 180f));
            var node30 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph9, typeof(BtsmtlSkillNativeNodeWrapper<NOT>), "d22eecb4-b437-7576-0b21-4bb28ad5c613", "NOT", new Vector2(-240f, 0f));
            var node29 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph9, typeof(BtsmtlSkillBooleanInputFlowNode), "e903d461-7f42-01ae-0822-70eb70f8a75d", "BranchHeld", new Vector2(-520f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node6, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "BranchHeld"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node10, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "BranchRelease") });
            BtsmtlSkillAuthoringContract.Apply(node29, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "BranchHeld"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node6, "m_Output", node7, "value", "6e0ae88a-496b-8b2d-d2ae-01c205807825");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node7, "Value", node8, "b", "14444ac7-47df-6187-7608-33c85ed67cc6");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node8, "Value", node9, "m_Result", "ce0dfb9c-179c-6fda-bf67-0da72c42f1cb");
            var edge5 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node10, "m_Output", node8, "a", "5167cf1e-05c8-d34e-28d8-3c6eddf0f1ab");
            var edge12 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph9, node29, "m_Output", node30, "value", "d2f8057e-c7f8-afa5-d25a-f6648f40fc09");
            var edge13 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph9, node30, "Value", node31, "m_Result", "d2610220-29d4-7f04-841e-4500f5164d19");
            return parts;
        }

        sealed class ExplodeParts
        {
            internal BtsmtlSkillFlowGraph graph2;
            internal BtsmtlSkillFlowGraph graph9;
        }
    }
}
