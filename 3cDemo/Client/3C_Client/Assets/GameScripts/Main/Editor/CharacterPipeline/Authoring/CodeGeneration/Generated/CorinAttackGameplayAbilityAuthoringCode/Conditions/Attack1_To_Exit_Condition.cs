using System;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;
using TimelineAnimationClip = BTSMTL.Timeline.AnimationClip;
using UnityObject = UnityEngine.Object;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;
using ThirdPersonCamera;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode
    {
        static void BuildCreateConditions_Attack1_To_Exit_Condition3(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph7 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "66a99a1ba00258473739f5c053c3d9c3", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack1 To Exit Condition");
        }

        static void BuildCreateConditions_Attack1_To_Exit_Condition5(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph9 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "280eceb11630ff24f9986186bc0698e6", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack1 To Exit Condition");
            generation.graph10 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "cebaa6f70788d586cbd02645609fd567", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack1 To Exit Condition");
        }

        static void BuildCreateConditions_Attack1_To_Exit_Condition22(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node27 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph7, typeof(BtsmtlSkillCanActivateActionFlowNode), "00986f00-673c-477f-89ac-f719e03fed7c", "Can Activate Dodge", new Vector2(-360f, 200f));
            generation.node30 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph7, typeof(BtsmtlSkillActionWindowActiveFlowNode), "311d0d5f-758b-4bbc-9f93-4a2a19d91d30", "Window RecoveryEarly", new Vector2(-360f, 100f));
            generation.node32 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph7, typeof(BtsmtlSkillActionRequestFlowNode), "567e0c7d-abb8-4eb5-9875-17057535d21f", "Has Dodge Request", new Vector2(-360f, 0f));
            generation.node29 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph7, typeof(BtsmtlSkillConditionResultFlowNode), "5aef9aa1-6d89-4a8c-b90a-28c2a5734b3c", "条件结果", new Vector2(600f, 180f));
            generation.node31 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph7, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "757d4fdd-a7da-4cd9-8f27-59320968f9ef", "AND", new Vector2(40f, 35f));
            generation.node28 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph7, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "8aa240eb-da75-466c-a708-7219269a1a12", "AND", new Vector2(220f, 70f));
        }

        static void BuildCreateConditions_Attack1_To_Exit_Condition24(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node39 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph9, typeof(BtsmtlSkillStateRootCompletedFlowNode), "72c0eb28-54cc-4ad3-91b1-77a3bc941b76", "状态主体已完成", new Vector2(-360f, 0f));
            generation.node40 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph9, typeof(BtsmtlSkillConditionResultFlowNode), "9a7131f1-b4ac-461e-9729-6c292eed9e13", "条件结果", new Vector2(600f, 180f));
            generation.node42 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph10, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "66ce9414-6ae1-44bc-878c-495f3922f470", ">", new Vector2(-240f, 20f));
            generation.node43 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph10, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "74dd4a18-f979-4db1-9d51-40ae18bf0c07", "AND", new Vector2(40f, 35f));
            generation.node44 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph10, typeof(BtsmtlSkillConditionResultFlowNode), "8eb0bf71-446b-42a5-9f1a-738a45aac793", "条件结果", new Vector2(600f, 180f));
            generation.node41 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph10, typeof(BtsmtlSkillInputMagnitudeFlowNode), "9a152d3b-09ec-4f85-8340-b3e717e6a291", "MoveAxis Magnitude", new Vector2(-520f, 0f));
            generation.node45 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph10, typeof(BtsmtlSkillActionWindowActiveFlowNode), "d30a5fa3-668f-4f3d-a5fc-e94874c656d1", "Window RecoveryLate", new Vector2(-360f, 100f));
            generation.node46 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph10, typeof(BtsmtlSkillBlackboardScalarFlowNode), "e40ea606-b000-48c7-9dc4-d37c48ff114b", "StopThreshold", new Vector2(-520f, 45f));
        }

        static void BuildConfigureConditions_Attack1_To_Exit_Condition2(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillCanActivateActionFlowNode)generation.node27).Configure(null, "", "");
            ((BtsmtlSkillActionWindowActiveFlowNode)generation.node30).SetWindowType("RecoveryEarly");
            ((IBtsmtlSkillInputNode)generation.node32).SetInputId("Dodge", "asset:be650df85b1e49ab9d1cefc91c6cc809");
        }

        static void BuildConfigureConditions_Attack1_To_Exit_Condition4(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((IBtsmtlSkillInputNode)generation.node41).SetInputId("MoveAxis", "asset:be650df85b1e49ab9d1cefc91c6cc809");
            ((BtsmtlSkillActionWindowActiveFlowNode)generation.node45).SetWindowType("RecoveryLate");
            ((BtsmtlSkillBlackboardScalarFlowNode)generation.node46).SetVariable(new BtsmtlSkillBlackboardReference("1edc27e65f454837b415895f4b808048", "00ec42f6d5ede195dcf13e4e27fe7933"));
        }

        static void BuildConnectConditions_Attack1_To_Exit_Condition2(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge6 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph7, generation.node27, "m_Output", generation.node28, "b", "bc048d43-0fe0-40a9-b087-3398300ce954");
            generation.edge7 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph7, generation.node28, "Value", generation.node29, "m_Result", "40ff98a9-0bcc-4091-9c5e-5de89e455a2e");
            generation.edge8 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph7, generation.node30, "m_Output", generation.node31, "b", "78c6fb2c-7321-4444-8762-dca53185b78b");
            generation.edge9 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph7, generation.node31, "Value", generation.node28, "a", "3cff8c0c-1139-4b63-8d84-615812601e74");
            generation.edge10 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph7, generation.node32, "m_Output", generation.node31, "a", "dbe695b0-31e4-402c-b2e3-93e4c7babbfa");
        }

        static void BuildConnectConditions_Attack1_To_Exit_Condition4(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge16 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph9, generation.node39, "m_Output", generation.node40, "m_Result", "ce30532f-dbc0-45f6-8481-b6cb87ddf9f8");
            generation.edge17 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph10, generation.node41, "m_Output", generation.node42, "a", "36b1412a-6fc1-4962-a7d2-b57307105cd1");
            generation.edge18 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph10, generation.node42, "Value", generation.node43, "a", "d63fdb49-6563-4457-a674-d95f9b558410");
            generation.edge19 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph10, generation.node43, "Value", generation.node44, "m_Result", "7c292eeb-a5fa-49cc-8160-d8cf0c231d8f");
            generation.edge20 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph10, generation.node45, "m_Output", generation.node43, "b", "c0764b82-f3eb-4f5e-a18d-fc93a6bb7139");
            generation.edge21 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph10, generation.node46, "m_Output", generation.node42, "b", "7aba0472-683c-4b37-9732-23871f316ab1");
        }

        static void BuildRootBindingConditions_Attack1_To_Exit_Condition3(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph7, new[] { "00986f00-673c-477f-89ac-f719e03fed7c", "8aa240eb-da75-466c-a708-7219269a1a12", "5aef9aa1-6d89-4a8c-b90a-28c2a5734b3c", "311d0d5f-758b-4bbc-9f93-4a2a19d91d30", "757d4fdd-a7da-4cd9-8f27-59320968f9ef", "567e0c7d-abb8-4eb5-9875-17057535d21f" }, new[] { "bc048d43-0fe0-40a9-b087-3398300ce954", "40ff98a9-0bcc-4091-9c5e-5de89e455a2e", "78c6fb2c-7321-4444-8762-dca53185b78b", "3cff8c0c-1139-4b63-8d84-615812601e74", "dbe695b0-31e4-402c-b2e3-93e4c7babbfa" });
        }

        static void BuildRootBindingConditions_Attack1_To_Exit_Condition5(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph9, new[] { "72c0eb28-54cc-4ad3-91b1-77a3bc941b76", "9a7131f1-b4ac-461e-9729-6c292eed9e13" }, new[] { "ce30532f-dbc0-45f6-8481-b6cb87ddf9f8" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph10, new[] { "9a152d3b-09ec-4f85-8340-b3e717e6a291", "66ce9414-6ae1-44bc-878c-495f3922f470", "74dd4a18-f979-4db1-9d51-40ae18bf0c07", "8eb0bf71-446b-42a5-9f1a-738a45aac793", "d30a5fa3-668f-4f3d-a5fc-e94874c656d1", "e40ea606-b000-48c7-9dc4-d37c48ff114b" }, new[] { "36b1412a-6fc1-4962-a7d2-b57307105cd1", "d63fdb49-6563-4457-a674-d95f9b558410", "7c292eeb-a5fa-49cc-8160-d8cf0c231d8f", "c0764b82-f3eb-4f5e-a18d-fc93a6bb7139", "7aba0472-683c-4b37-9732-23871f316ab1" });
        }

        static void BuildRootBindingConditions_Attack1_To_Exit_Condition22(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph7, Array.Empty<string>());
        }

        static void BuildRootBindingConditions_Attack1_To_Exit_Condition24(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph9, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph10, Array.Empty<string>());
        }
    }
}
