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
    public sealed partial class CorinDodgeForwardGameplayAbilityAuthoringCode
    {
        static void BuildCreateConditions_DodgeForward_State_Body_StateRootCompleted2(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph4 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "21b0165d-bd54-40f4-8610-9f8852be4e1a", BtsmtlSkillFlowGraphRole.ConditionRule, "DodgeForward State Body/StateRootCompleted");
        }

        static void BuildCreateConditions_DodgeForward_State_Body_StateRootCompleted5(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph4, typeof(BtsmtlSkillStateRootCompletedFlowNode), "2eaafbfa-a850-4809-9fdd-e22b00780718", "状态主体已完成", new Vector2(-360f, 0f));
            generation.node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph4, typeof(BtsmtlSkillConditionResultFlowNode), "3f7f717a-3ccb-4fca-a0b1-f2011e236a0a", "条件结果", new Vector2(600f, 180f));
        }

        static void BuildConnectConditions_DodgeForward_State_Body_StateRootCompleted2(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph4, generation.node16, "m_Output", generation.node17, "m_Result", "308a15ae-b5f5-49ab-ac56-3c070dea8130");
        }

        static void BuildRootBindingConditions_DodgeForward_State_Body_StateRootCompleted2(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph4, new[] { "2eaafbfa-a850-4809-9fdd-e22b00780718", "3f7f717a-3ccb-4fca-a0b1-f2011e236a0a" }, new[] { "308a15ae-b5f5-49ab-ac56-3c070dea8130" });
        }

        static void BuildRootBindingConditions_DodgeForward_State_Body_StateRootCompleted5(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph4, Array.Empty<string>());
        }
    }
}
