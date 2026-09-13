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
        static void BuildCreateConditions_Attack2_To_Exit_Condition8(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph17 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "18da04dc0b1711110ef19f05b5146bee", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack2 To Exit Condition");
            generation.graph18 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "b6ef910563ebce087609b38a48a54faa", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack2 To Exit Condition");
            generation.graph19 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "6b627c62b78b273dab72cf76c0b62086", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack2 To Exit Condition");
        }

        static void BuildCreateConditions_Attack2_To_Exit_Condition27(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node77 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph17, typeof(BtsmtlSkillInputMagnitudeFlowNode), "13a54652-1edf-495b-998a-1f7f96c9b2b8", "MoveAxis Magnitude", new Vector2(-520f, 0f));
            generation.node78 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph17, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "52d6e59f-93d7-4c15-93cd-e8b250f4bca3", ">", new Vector2(-240f, 20f));
            generation.node81 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph17, typeof(BtsmtlSkillBlackboardScalarFlowNode), "6d48f07b-3aca-41b5-aa29-a664e317426b", "StopThreshold", new Vector2(-520f, 45f));
            generation.node80 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph17, typeof(BtsmtlSkillConditionResultFlowNode), "9296adc6-5edf-4cb7-81ac-1b83adbfaa50", "条件结果", new Vector2(600f, 180f));
            generation.node79 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph17, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "ed675b3a-a2bb-4378-b89e-937fd25d57a5", "AND", new Vector2(40f, 35f));
            generation.node82 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph17, typeof(BtsmtlSkillActionWindowActiveFlowNode), "ff3387f2-e728-442c-9351-de8217f5dde7", "Window RecoveryLate", new Vector2(-360f, 100f));
            generation.node86 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph18, typeof(BtsmtlSkillConditionResultFlowNode), "04a71ac0-abf9-40fa-aa7d-2a67f4e67c3e", "条件结果", new Vector2(600f, 180f));
            generation.node85 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph18, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "330848fb-7efd-4c3e-ba8b-91a17ddc45c2", "AND", new Vector2(220f, 70f));
            generation.node84 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph18, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "4f62e295-cea3-4d67-9212-d07a4ebc2cf1", "AND", new Vector2(40f, 35f));
            generation.node83 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph18, typeof(BtsmtlSkillActionRequestFlowNode), "b174a173-98d1-4673-96c3-fd7a0aadb3fa", "Has Dodge Request", new Vector2(-360f, 0f));
            generation.node87 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph18, typeof(BtsmtlSkillActionWindowActiveFlowNode), "b6a9c363-16f1-4f91-bb9e-5125820e2e50", "Window RecoveryEarly", new Vector2(-360f, 100f));
            generation.node88 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph18, typeof(BtsmtlSkillCanActivateActionFlowNode), "e48ce2f5-a4e9-4e5e-ace0-accac91de4ca", "Can Activate Dodge", new Vector2(-360f, 200f));
            generation.node89 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph19, typeof(BtsmtlSkillStateRootCompletedFlowNode), "5b612365-a42e-4374-a830-6bea3f4af3a7", "状态主体已完成", new Vector2(-360f, 0f));
            generation.node90 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph19, typeof(BtsmtlSkillConditionResultFlowNode), "d5b8e174-74e5-417c-9685-1f262a71af1c", "条件结果", new Vector2(600f, 180f));
        }

        static void BuildConfigureConditions_Attack2_To_Exit_Condition7(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((IBtsmtlSkillInputNode)generation.node77).SetInputId("MoveAxis", "asset:be650df85b1e49ab9d1cefc91c6cc809");
            ((BtsmtlSkillBlackboardScalarFlowNode)generation.node81).SetVariable(new BtsmtlSkillBlackboardReference("1edc27e65f454837b415895f4b808048", "00ec42f6d5ede195dcf13e4e27fe7933"));
            ((BtsmtlSkillActionWindowActiveFlowNode)generation.node82).SetWindowType("RecoveryLate");
            ((IBtsmtlSkillInputNode)generation.node83).SetInputId("Dodge", "asset:be650df85b1e49ab9d1cefc91c6cc809");
            ((BtsmtlSkillActionWindowActiveFlowNode)generation.node87).SetWindowType("RecoveryEarly");
            ((BtsmtlSkillCanActivateActionFlowNode)generation.node88).Configure(null, "", "");
        }

        static void BuildConnectConditions_Attack2_To_Exit_Condition7(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge32 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph17, generation.node77, "m_Output", generation.node78, "a", "0c57f93a-f02e-4823-8c0a-13c6deca0a6f");
            generation.edge33 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph17, generation.node78, "Value", generation.node79, "a", "18990e82-9bb3-46a6-a8f4-bf06f1956f39");
            generation.edge34 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph17, generation.node79, "Value", generation.node80, "m_Result", "8cf687a0-b8ae-42d8-87f6-9ab48b6354f1");
            generation.edge35 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph17, generation.node81, "m_Output", generation.node78, "b", "ab7dcfc3-d27b-4c85-8cd7-752224e40be9");
            generation.edge36 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph17, generation.node82, "m_Output", generation.node79, "b", "869f8419-548e-4146-b906-3f6065c3f838");
            generation.edge37 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph18, generation.node83, "m_Output", generation.node84, "a", "8ec34cc3-32f5-4feb-9fad-8268b86da0d7");
            generation.edge38 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph18, generation.node84, "Value", generation.node85, "a", "698731f4-a462-4c2e-9f84-9db77be79daf");
            generation.edge39 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph18, generation.node85, "Value", generation.node86, "m_Result", "3bf13cbe-7c52-43b8-9338-56fcb8f53aa2");
            generation.edge40 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph18, generation.node87, "m_Output", generation.node84, "b", "5789cfe7-ee9e-442d-a65c-815f4beb0ed5");
            generation.edge41 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph18, generation.node88, "m_Output", generation.node85, "b", "90540feb-0e11-410a-8240-5671d1f1f8ad");
            generation.edge42 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph19, generation.node89, "m_Output", generation.node90, "m_Result", "420c3d01-a9b5-4a74-af0a-8d7adc7de53a");
        }

        static void BuildRootBindingConditions_Attack2_To_Exit_Condition8(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph17, new[] { "13a54652-1edf-495b-998a-1f7f96c9b2b8", "52d6e59f-93d7-4c15-93cd-e8b250f4bca3", "ed675b3a-a2bb-4378-b89e-937fd25d57a5", "9296adc6-5edf-4cb7-81ac-1b83adbfaa50", "6d48f07b-3aca-41b5-aa29-a664e317426b", "ff3387f2-e728-442c-9351-de8217f5dde7" }, new[] { "0c57f93a-f02e-4823-8c0a-13c6deca0a6f", "18990e82-9bb3-46a6-a8f4-bf06f1956f39", "8cf687a0-b8ae-42d8-87f6-9ab48b6354f1", "ab7dcfc3-d27b-4c85-8cd7-752224e40be9", "869f8419-548e-4146-b906-3f6065c3f838" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph18, new[] { "b174a173-98d1-4673-96c3-fd7a0aadb3fa", "4f62e295-cea3-4d67-9212-d07a4ebc2cf1", "330848fb-7efd-4c3e-ba8b-91a17ddc45c2", "04a71ac0-abf9-40fa-aa7d-2a67f4e67c3e", "b6a9c363-16f1-4f91-bb9e-5125820e2e50", "e48ce2f5-a4e9-4e5e-ace0-accac91de4ca" }, new[] { "8ec34cc3-32f5-4feb-9fad-8268b86da0d7", "698731f4-a462-4c2e-9f84-9db77be79daf", "3bf13cbe-7c52-43b8-9338-56fcb8f53aa2", "5789cfe7-ee9e-442d-a65c-815f4beb0ed5", "90540feb-0e11-410a-8240-5671d1f1f8ad" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph19, new[] { "5b612365-a42e-4374-a830-6bea3f4af3a7", "d5b8e174-74e5-417c-9685-1f262a71af1c" }, new[] { "420c3d01-a9b5-4a74-af0a-8d7adc7de53a" });
        }

        static void BuildRootBindingConditions_Attack2_To_Exit_Condition27(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph17, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph18, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph19, Array.Empty<string>());
        }
    }
}
