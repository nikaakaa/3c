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
        static void BuildCreateConditions_Enter_To_State_Rule1(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph1 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "5ee51b65f135f7809601ec732f478ef1", BtsmtlSkillFlowGraphRole.ConditionRule, "Enter_To_State_Rule");
        }

        static void BuildCreateConditions_Enter_To_State_Rule20(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph1, typeof(BtsmtlSkillConditionResultFlowNode), "4376cf41-0cac-494a-8891-ceab7dd9054a", "条件结果", new Vector2(600f, 180f));
        }

        static void BuildRootBindingConditions_Enter_To_State_Rule1(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph1, new[] { "4376cf41-0cac-494a-8891-ceab7dd9054a" }, Array.Empty<string>());
        }

        static void BuildRootBindingConditions_Enter_To_State_Rule20(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph1, Array.Empty<string>());
        }
    }
}
