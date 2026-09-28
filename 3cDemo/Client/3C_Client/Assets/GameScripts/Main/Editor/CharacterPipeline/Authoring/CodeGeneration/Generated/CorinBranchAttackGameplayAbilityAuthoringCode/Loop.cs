using ThirdPersonSimulation;
using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchAttackGameplayAbilityAuthoringCode
    {
        static LoopParts BuildLoop(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new LoopParts();
            parts.graph3 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "e8f0f092-9c10-9e40-4b44-9689d6730599", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "StartComplete Condition");
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge2, parts.graph3, 0, ProgramAbortPolicy.None, 1);
            parts.graph12 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "8da8fb68-9dc1-3646-a3d1-d3a4162f959f", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Stopped Condition");
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge8, parts.graph12, 0, ProgramAbortPolicy.None, 1);
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillStateRootCompletedFlowNode), "9bb194e4-b643-fee7-b32d-246d6202d865", "状态主体已完成", new Vector2(-360f, 0f));
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillConditionResultFlowNode), "dfb451d9-5b14-938f-0253-2df754df8b65", "条件结果", new Vector2(600f, 180f));
            var node39 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph12, typeof(BtsmtlSkillInputMagnitudeFlowNode), "401922e5-0787-ebd2-ae51-88acca8b7a29", "MoveAxis Magnitude", new Vector2(-520f, 0f));
            var node40 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph12, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "4f2908f7-a69e-c374-6ff5-294175de1bb5", ">", new Vector2(-240f, 0f));
            var node41 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph12, typeof(BtsmtlSkillNativeNodeWrapper<NOT>), "69f6f4f6-df03-7193-5979-e4ed08a292ae", "NOT", new Vector2(40f, 0f));
            var node42 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph12, typeof(BtsmtlSkillConditionResultFlowNode), "8762b35e-8d26-6af4-0a0a-b48d7a2e409f", "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringContract.Apply(node39, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "MoveAxis"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringCode.SetValue(node40, "b", 0.05f);
            var edge6 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node11, "m_Output", node12, "m_Result", "98b2b443-0615-8c6a-6d3a-f5b81c1d78d3");
            var edge17 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph12, node39, "m_Output", node40, "a", "3673d22e-103c-634b-1402-d7a81895f71a");
            var edge18 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph12, node40, "Value", node41, "value", "a6224092-96f3-deb3-90b1-db663311e63b");
            var edge19 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph12, node41, "Value", node42, "m_Result", "31760954-157f-7fb3-a6cc-e8822eb6776d");
            return parts;
        }

        sealed class LoopParts
        {
            internal BtsmtlSkillFlowGraph graph3;
            internal BtsmtlSkillFlowGraph graph12;
        }
    }
}
