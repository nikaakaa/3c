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
        static void BuildCreateConditions_Attack4_To_Exit_Condition14(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph34 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "4185a6457a17f3050b7d0b4dd893f48a", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack4 To Exit Condition");
        }

        static void BuildCreateConditions_Attack4_To_Exit_Condition16(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph36 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "7e767bd1bb7375a348c04eb31a0fa6ee", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack4 To Exit Condition");
            generation.graph37 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "4e24360bcf0b4484b81303d56fa2b957", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack4 To Exit Condition");
        }

        static void BuildCreateConditions_Attack4_To_Exit_Condition33(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node160 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph34, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "0bb4a135-464a-478b-8c65-5a8034c0aa55", "AND", new Vector2(40f, 35f));
            generation.node159 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph34, typeof(BtsmtlSkillActionWindowActiveFlowNode), "105d943c-b30c-461d-b83a-f3a52d491570", "Window RecoveryLate", new Vector2(-360f, 100f));
            generation.node162 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph34, typeof(BtsmtlSkillInputMagnitudeFlowNode), "1a2ea1b0-fd91-440c-adf5-5e58e8cfddd8", "MoveAxis Magnitude", new Vector2(-520f, 0f));
            generation.node164 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph34, typeof(BtsmtlSkillBlackboardScalarFlowNode), "393134fd-cd4d-4255-8cfc-ed6dfe49ec83", "StopThreshold", new Vector2(-520f, 45f));
            generation.node161 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph34, typeof(BtsmtlSkillConditionResultFlowNode), "439638e8-eed2-4a24-a297-7ae7c1237bd3", "条件结果", new Vector2(600f, 180f));
            generation.node163 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph34, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "ddabdfc2-6be2-48b8-a9c7-4eb093abd4f7", ">", new Vector2(-240f, 20f));
        }

        static void BuildCreateConditions_Attack4_To_Exit_Condition35(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node171 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph36, typeof(BtsmtlSkillStateRootCompletedFlowNode), "3b9d9658-3de3-44f7-9a89-b5c845bb30e3", "状态主体已完成", new Vector2(-360f, 0f));
            generation.node172 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph36, typeof(BtsmtlSkillConditionResultFlowNode), "ef9cf994-bec4-46f8-9424-b20709fa5b3b", "条件结果", new Vector2(600f, 180f));
            generation.node174 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph37, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "47b79351-c34f-48dc-849b-754cd986d262", "AND", new Vector2(220f, 70f));
            generation.node173 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph37, typeof(BtsmtlSkillCanActivateActionFlowNode), "48028d59-7529-4abb-afa9-010584cd1e9b", "Can Activate Dodge", new Vector2(-360f, 200f));
            generation.node176 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph37, typeof(BtsmtlSkillActionWindowActiveFlowNode), "4ed50c22-f68b-4d9a-ba57-6b325c91ffac", "Window RecoveryEarly", new Vector2(-360f, 100f));
            generation.node177 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph37, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "84af6e94-af5c-4d7c-9c15-5f2a203d236b", "AND", new Vector2(40f, 35f));
            generation.node175 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph37, typeof(BtsmtlSkillConditionResultFlowNode), "cee06b3f-d862-4d69-9a7d-3079f0e8322b", "条件结果", new Vector2(600f, 180f));
            generation.node178 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph37, typeof(BtsmtlSkillActionRequestFlowNode), "d142330f-66ab-46c0-a74d-bf4e5db6cfea", "Has Dodge Request", new Vector2(-360f, 0f));
        }

        static void BuildConfigureConditions_Attack4_To_Exit_Condition12(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillActionWindowActiveFlowNode)generation.node159).SetWindowType("RecoveryLate");
            ((IBtsmtlSkillInputNode)generation.node162).SetInputId("MoveAxis", "asset:be650df85b1e49ab9d1cefc91c6cc809");
            ((BtsmtlSkillBlackboardScalarFlowNode)generation.node164).SetVariable(new BtsmtlSkillBlackboardReference("1edc27e65f454837b415895f4b808048", "00ec42f6d5ede195dcf13e4e27fe7933"));
        }

        static void BuildConfigureConditions_Attack4_To_Exit_Condition14(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillCanActivateActionFlowNode)generation.node173).Configure(null, "", "");
            ((BtsmtlSkillActionWindowActiveFlowNode)generation.node176).SetWindowType("RecoveryEarly");
            ((IBtsmtlSkillInputNode)generation.node178).SetInputId("Dodge", "asset:be650df85b1e49ab9d1cefc91c6cc809");
        }

        static void BuildConnectConditions_Attack4_To_Exit_Condition13(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge69 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph34, generation.node159, "m_Output", generation.node160, "b", "276b291a-9bbf-4cd6-b92f-ab03afc90902");
            generation.edge70 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph34, generation.node160, "Value", generation.node161, "m_Result", "219ceff6-e2c2-438c-8e60-9bde8fc8f5a2");
            generation.edge71 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph34, generation.node162, "m_Output", generation.node163, "a", "c87d6108-e481-4839-9923-b1522e293251");
            generation.edge72 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph34, generation.node163, "Value", generation.node160, "a", "878c30d8-c91f-4d39-9627-a96c0ca5b597");
            generation.edge73 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph34, generation.node164, "m_Output", generation.node163, "b", "be8d10b4-1242-4325-bc72-dc40f8d98d79");
        }

        static void BuildConnectConditions_Attack4_To_Exit_Condition15(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge79 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph36, generation.node171, "m_Output", generation.node172, "m_Result", "7cd969bc-bb24-4083-b928-a318732f4237");
            generation.edge80 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph37, generation.node173, "m_Output", generation.node174, "b", "d386e344-d2b6-47c5-bfe9-95dc605aafa5");
            generation.edge81 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph37, generation.node174, "Value", generation.node175, "m_Result", "53e91a1c-9ab1-4e8a-96d7-3ee9c9771e9b");
            generation.edge82 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph37, generation.node176, "m_Output", generation.node177, "b", "703b8e26-53ba-4411-9ef1-b8df9dd9995e");
            generation.edge83 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph37, generation.node177, "Value", generation.node174, "a", "4ee0e06a-d010-4d5c-9ef5-8f9547bbf1f6");
            generation.edge84 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph37, generation.node178, "m_Output", generation.node177, "a", "1b03c73c-199d-446b-ba6c-2b16fda43039");
        }

        static void BuildRootBindingConditions_Attack4_To_Exit_Condition14(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph34, new[] { "105d943c-b30c-461d-b83a-f3a52d491570", "0bb4a135-464a-478b-8c65-5a8034c0aa55", "439638e8-eed2-4a24-a297-7ae7c1237bd3", "1a2ea1b0-fd91-440c-adf5-5e58e8cfddd8", "ddabdfc2-6be2-48b8-a9c7-4eb093abd4f7", "393134fd-cd4d-4255-8cfc-ed6dfe49ec83" }, new[] { "276b291a-9bbf-4cd6-b92f-ab03afc90902", "219ceff6-e2c2-438c-8e60-9bde8fc8f5a2", "c87d6108-e481-4839-9923-b1522e293251", "878c30d8-c91f-4d39-9627-a96c0ca5b597", "be8d10b4-1242-4325-bc72-dc40f8d98d79" });
        }

        static void BuildRootBindingConditions_Attack4_To_Exit_Condition16(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph36, new[] { "3b9d9658-3de3-44f7-9a89-b5c845bb30e3", "ef9cf994-bec4-46f8-9424-b20709fa5b3b" }, new[] { "7cd969bc-bb24-4083-b928-a318732f4237" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph37, new[] { "48028d59-7529-4abb-afa9-010584cd1e9b", "47b79351-c34f-48dc-849b-754cd986d262", "cee06b3f-d862-4d69-9a7d-3079f0e8322b", "4ed50c22-f68b-4d9a-ba57-6b325c91ffac", "84af6e94-af5c-4d7c-9c15-5f2a203d236b", "d142330f-66ab-46c0-a74d-bf4e5db6cfea" }, new[] { "d386e344-d2b6-47c5-bfe9-95dc605aafa5", "53e91a1c-9ab1-4e8a-96d7-3ee9c9771e9b", "703b8e26-53ba-4411-9ef1-b8df9dd9995e", "4ee0e06a-d010-4d5c-9ef5-8f9547bbf1f6", "1b03c73c-199d-446b-ba6c-2b16fda43039" });
        }

        static void BuildRootBindingConditions_Attack4_To_Exit_Condition33(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph34, Array.Empty<string>());
        }

        static void BuildRootBindingConditions_Attack4_To_Exit_Condition35(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph36, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph37, Array.Empty<string>());
        }
    }
}
