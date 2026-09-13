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
        static void BuildCreateConditions_Attack1_To_Attack2_Condition4(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph8 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "fbe7f0ab5a6848bf28dd2f55f7242282", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack1 To Attack2 Condition");
        }

        static void BuildCreateConditions_Attack1_To_Attack2_Condition23(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node36 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph8, typeof(BtsmtlSkillConditionResultFlowNode), "45786ebc-05a5-44a1-bca6-2888a6bdcf80", "条件结果", new Vector2(600f, 180f));
            generation.node35 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph8, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "5abfc932-3527-4581-8b42-b9c28a6e63fd", "AND", new Vector2(220f, 70f));
            generation.node34 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph8, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "5e4efb9b-de3b-445b-8ae7-d304b413e02b", "AND", new Vector2(40f, 35f));
            generation.node33 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph8, typeof(BtsmtlSkillActionRequestFlowNode), "cce6a5ea-67c8-4a19-9b57-39e771e2364b", "Has Attack Request", new Vector2(-360f, 0f));
            generation.node37 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph8, typeof(BtsmtlSkillActionWindowActiveFlowNode), "ec5dd0ff-4406-41ae-886e-6799be818ffe", "Window ComboAccept", new Vector2(-360f, 100f));
            generation.node38 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph8, typeof(BtsmtlSkillCanActivateActionFlowNode), "f73664b9-e509-4355-85d1-76a35f5cb88a", "Can Activate Attack", new Vector2(-360f, 200f));
        }

        static void BuildConfigureConditions_Attack1_To_Attack2_Condition3(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((IBtsmtlSkillInputNode)generation.node33).SetInputId("Attack", "asset:be650df85b1e49ab9d1cefc91c6cc809");
            ((BtsmtlSkillActionWindowActiveFlowNode)generation.node37).SetWindowType("ComboAccept");
            ((BtsmtlSkillCanActivateActionFlowNode)generation.node38).Configure(null, "b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933");
        }

        static void BuildConnectConditions_Attack1_To_Attack2_Condition3(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge11 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph8, generation.node33, "m_Output", generation.node34, "a", "2ee1a38e-b035-4654-ba09-f480f015a2c7");
            generation.edge12 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph8, generation.node34, "Value", generation.node35, "a", "1e52bba8-3755-4534-a6ae-f2a96385d33f");
            generation.edge13 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph8, generation.node35, "Value", generation.node36, "m_Result", "bda74353-7906-4d63-ad12-fcab657bc1fa");
            generation.edge14 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph8, generation.node37, "m_Output", generation.node34, "b", "b45493c9-2454-446c-beff-1d8886d66f32");
            generation.edge15 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph8, generation.node38, "m_Output", generation.node35, "b", "1c4a064e-760a-4aad-8245-01272b6e2561");
        }

        static void BuildRootBindingConditions_Attack1_To_Attack2_Condition4(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph8, new[] { "cce6a5ea-67c8-4a19-9b57-39e771e2364b", "5e4efb9b-de3b-445b-8ae7-d304b413e02b", "5abfc932-3527-4581-8b42-b9c28a6e63fd", "45786ebc-05a5-44a1-bca6-2888a6bdcf80", "ec5dd0ff-4406-41ae-886e-6799be818ffe", "f73664b9-e509-4355-85d1-76a35f5cb88a" }, new[] { "2ee1a38e-b035-4654-ba09-f480f015a2c7", "1e52bba8-3755-4534-a6ae-f2a96385d33f", "bda74353-7906-4d63-ad12-fcab657bc1fa", "b45493c9-2454-446c-beff-1d8886d66f32", "1c4a064e-760a-4aad-8245-01272b6e2561" });
        }

        static void BuildRootBindingConditions_Attack1_To_Attack2_Condition23(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph8, Array.Empty<string>());
        }
    }
}
