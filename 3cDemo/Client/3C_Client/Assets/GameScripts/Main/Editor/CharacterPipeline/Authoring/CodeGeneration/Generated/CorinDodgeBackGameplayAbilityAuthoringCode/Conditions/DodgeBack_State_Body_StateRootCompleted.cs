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
    public sealed partial class CorinDodgeBackGameplayAbilityAuthoringCode
    {
        static void BuildCreateConditions_DodgeBack_State_Body_StateRootCompleted2(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph4 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "921c1fd2-8a23-431a-a49b-39a98e771e2f", BtsmtlSkillFlowGraphRole.ConditionRule, "DodgeBack State Body/StateRootCompleted");
        }

        static void BuildCreateConditions_DodgeBack_State_Body_StateRootCompleted5(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph4, typeof(BtsmtlSkillConditionResultFlowNode), "12d56e7c-5437-4a5f-bed0-c0c5c22ccb75", "条件结果", new Vector2(600f, 180f));
            generation.node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph4, typeof(BtsmtlSkillStateRootCompletedFlowNode), "b4b30de0-f2a9-4145-8a74-376a9fcb4ca8", "状态主体已完成", new Vector2(-360f, 0f));
        }

        static void BuildConnectConditions_DodgeBack_State_Body_StateRootCompleted2(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph4, generation.node16, "m_Output", generation.node17, "m_Result", "797b1212-00ef-4290-9fb4-17258e039be7");
        }

        static void BuildRootBindingConditions_DodgeBack_State_Body_StateRootCompleted2(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph4, new[] { "b4b30de0-f2a9-4145-8a74-376a9fcb4ca8", "12d56e7c-5437-4a5f-bed0-c0c5c22ccb75" }, new[] { "797b1212-00ef-4290-9fb4-17258e039be7" });
        }

        static void BuildRootBindingConditions_DodgeBack_State_Body_StateRootCompleted5(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph4, Array.Empty<string>());
        }
    }
}
