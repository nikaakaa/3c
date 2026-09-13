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
        static void BuildCreateConditions_Attack4_To_Attack5_Condition15(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph35 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "181848fe552cf5d8dea571420ba4d097", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack4 To Attack5 Condition");
        }

        static void BuildCreateConditions_Attack4_To_Attack5_Condition34(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node165 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph35, typeof(BtsmtlSkillCanActivateActionFlowNode), "79a3da65-a63c-4cc5-8fb0-92f1b2b2a2b0", "Can Activate Attack", new Vector2(-360f, 200f));
            generation.node169 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph35, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "af016b33-7d67-48a2-a359-1ed676825ffd", "AND", new Vector2(40f, 35f));
            generation.node168 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph35, typeof(BtsmtlSkillActionWindowActiveFlowNode), "c83151bc-009a-4d34-b38c-f99e2b45b51e", "Window ComboAccept", new Vector2(-360f, 100f));
            generation.node167 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph35, typeof(BtsmtlSkillConditionResultFlowNode), "d6bc5a5d-7649-4be1-ae8e-ff1a92e7bb94", "条件结果", new Vector2(600f, 180f));
            generation.node166 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph35, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "de106c4b-d5f8-434c-9652-816402d20945", "AND", new Vector2(220f, 70f));
            generation.node170 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph35, typeof(BtsmtlSkillActionRequestFlowNode), "e88fb30b-48ed-49c6-883b-6027beee4801", "Has Attack Request", new Vector2(-360f, 0f));
        }

        static void BuildConfigureConditions_Attack4_To_Attack5_Condition13(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillCanActivateActionFlowNode)generation.node165).Configure(null, "b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933");
            ((BtsmtlSkillActionWindowActiveFlowNode)generation.node168).SetWindowType("ComboAccept");
            ((IBtsmtlSkillInputNode)generation.node170).SetInputId("Attack", "asset:be650df85b1e49ab9d1cefc91c6cc809");
        }

        static void BuildConnectConditions_Attack4_To_Attack5_Condition14(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge74 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph35, generation.node165, "m_Output", generation.node166, "b", "aa0ce478-92a7-42fd-ad00-5ce1a029396b");
            generation.edge75 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph35, generation.node166, "Value", generation.node167, "m_Result", "99e605d7-d252-4199-b71e-0c80491a198e");
            generation.edge76 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph35, generation.node168, "m_Output", generation.node169, "b", "13087de2-b46e-4197-a24f-5465b21f7df0");
            generation.edge77 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph35, generation.node169, "Value", generation.node166, "a", "968f5392-0920-40b4-8fad-eddfc18d3653");
            generation.edge78 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph35, generation.node170, "m_Output", generation.node169, "a", "a6d53455-e3d8-4f2a-9b61-bb41a5001146");
        }

        static void BuildRootBindingConditions_Attack4_To_Attack5_Condition15(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph35, new[] { "79a3da65-a63c-4cc5-8fb0-92f1b2b2a2b0", "de106c4b-d5f8-434c-9652-816402d20945", "d6bc5a5d-7649-4be1-ae8e-ff1a92e7bb94", "c83151bc-009a-4d34-b38c-f99e2b45b51e", "af016b33-7d67-48a2-a359-1ed676825ffd", "e88fb30b-48ed-49c6-883b-6027beee4801" }, new[] { "aa0ce478-92a7-42fd-ad00-5ce1a029396b", "99e605d7-d252-4199-b71e-0c80491a198e", "13087de2-b46e-4197-a24f-5465b21f7df0", "968f5392-0920-40b4-8fad-eddfc18d3653", "a6d53455-e3d8-4f2a-9b61-bb41a5001146" });
        }

        static void BuildRootBindingConditions_Attack4_To_Attack5_Condition34(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph35, Array.Empty<string>());
        }
    }
}
