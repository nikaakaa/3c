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
        static void BuildCreateConditions_Attack2_To_Attack3_Condition7(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph16 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "1bf78cba3df9605848cdacbff01046f6", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack2 To Attack3 Condition");
        }

        static void BuildCreateConditions_Attack2_To_Attack3_Condition26(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node72 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph16, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "12bb0dae-0864-462a-a9cd-7c2dfbe7755d", "AND", new Vector2(220f, 70f));
            generation.node71 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph16, typeof(BtsmtlSkillCanActivateActionFlowNode), "5f279952-a577-450e-ba55-489986edf6ba", "Can Activate Attack", new Vector2(-360f, 200f));
            generation.node74 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph16, typeof(BtsmtlSkillActionRequestFlowNode), "afd5ac96-3572-4761-b4e9-73df6d4e5c5a", "Has Attack Request", new Vector2(-360f, 0f));
            generation.node75 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph16, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "d244aa12-6fb0-48b4-88e7-35da5b15c7e9", "AND", new Vector2(40f, 35f));
            generation.node76 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph16, typeof(BtsmtlSkillActionWindowActiveFlowNode), "e4a10606-4019-482e-9691-5013a43a1778", "Window ComboAccept", new Vector2(-360f, 100f));
            generation.node73 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph16, typeof(BtsmtlSkillConditionResultFlowNode), "f827b6b4-9110-4e9a-99d1-d9d7c496dac9", "条件结果", new Vector2(600f, 180f));
        }

        static void BuildConfigureConditions_Attack2_To_Attack3_Condition6(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillCanActivateActionFlowNode)generation.node71).Configure(null, "b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933");
            ((IBtsmtlSkillInputNode)generation.node74).SetInputId("Attack", "asset:be650df85b1e49ab9d1cefc91c6cc809");
            ((BtsmtlSkillActionWindowActiveFlowNode)generation.node76).SetWindowType("ComboAccept");
        }

        static void BuildConnectConditions_Attack2_To_Attack3_Condition6(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge27 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph16, generation.node71, "m_Output", generation.node72, "b", "191d8aa7-6bf9-494b-a4e3-a4e190a8e668");
            generation.edge28 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph16, generation.node72, "Value", generation.node73, "m_Result", "a5773886-2ad6-4d78-bdb1-8e444e2ebe48");
            generation.edge29 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph16, generation.node74, "m_Output", generation.node75, "a", "7ff43a2e-1512-4e8d-a03f-7048611cd383");
            generation.edge30 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph16, generation.node75, "Value", generation.node72, "a", "9a23f8f5-4295-43b0-9bc9-d1f2e55e833b");
            generation.edge31 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph16, generation.node76, "m_Output", generation.node75, "b", "0f754649-8416-4201-98dd-38cb8da63705");
        }

        static void BuildRootBindingConditions_Attack2_To_Attack3_Condition7(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph16, new[] { "5f279952-a577-450e-ba55-489986edf6ba", "12bb0dae-0864-462a-a9cd-7c2dfbe7755d", "f827b6b4-9110-4e9a-99d1-d9d7c496dac9", "afd5ac96-3572-4761-b4e9-73df6d4e5c5a", "d244aa12-6fb0-48b4-88e7-35da5b15c7e9", "e4a10606-4019-482e-9691-5013a43a1778" }, new[] { "191d8aa7-6bf9-494b-a4e3-a4e190a8e668", "a5773886-2ad6-4d78-bdb1-8e444e2ebe48", "7ff43a2e-1512-4e8d-a03f-7048611cd383", "9a23f8f5-4295-43b0-9bc9-d1f2e55e833b", "0f754649-8416-4201-98dd-38cb8da63705" });
        }

        static void BuildRootBindingConditions_Attack2_To_Attack3_Condition26(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph16, Array.Empty<string>());
        }
    }
}
