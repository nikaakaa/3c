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
        static void BuildCreateConditions_Attack3_To_Attack4_Condition11(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph27 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "db9f983b0414a04fb72784e5b2b59838", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack3 To Attack4 Condition");
        }

        static void BuildCreateConditions_Attack3_To_Attack4_Condition30(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node128 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph27, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "0537bd26-1cf3-45c2-92c5-1d896d70d4bd", "AND", new Vector2(40f, 35f));
            generation.node130 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph27, typeof(BtsmtlSkillConditionResultFlowNode), "360f4fad-44ba-4cb3-bbe8-7728a781f890", "条件结果", new Vector2(600f, 180f));
            generation.node127 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph27, typeof(BtsmtlSkillActionRequestFlowNode), "36138109-865b-4384-995e-56076a4c7410", "Has Attack Request", new Vector2(-360f, 0f));
            generation.node131 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph27, typeof(BtsmtlSkillCanActivateActionFlowNode), "5d6fe35f-531a-46b7-a97a-7daf4f9b8097", "Can Activate Attack", new Vector2(-360f, 200f));
            generation.node129 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph27, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "63866ef1-87b6-4291-9606-ce1230478c83", "AND", new Vector2(220f, 70f));
            generation.node132 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph27, typeof(BtsmtlSkillActionWindowActiveFlowNode), "cdd15f3c-f01d-4319-94e8-7fb783690ba7", "Window ComboAccept", new Vector2(-360f, 100f));
        }

        static void BuildConfigureConditions_Attack3_To_Attack4_Condition10(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((IBtsmtlSkillInputNode)generation.node127).SetInputId("Attack", "asset:be650df85b1e49ab9d1cefc91c6cc809");
            ((BtsmtlSkillCanActivateActionFlowNode)generation.node131).Configure(null, "b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933");
            ((BtsmtlSkillActionWindowActiveFlowNode)generation.node132).SetWindowType("ComboAccept");
        }

        static void BuildConnectConditions_Attack3_To_Attack4_Condition10(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge58 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph27, generation.node127, "m_Output", generation.node128, "a", "eba41a97-036b-4806-98f5-fd20ac4a7332");
            generation.edge59 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph27, generation.node128, "Value", generation.node129, "a", "0e2a5f6d-e40f-4d76-9691-1dad5b79504e");
            generation.edge60 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph27, generation.node129, "Value", generation.node130, "m_Result", "55779d5e-fb1d-4291-8f8b-7f62d53014f7");
            generation.edge61 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph27, generation.node131, "m_Output", generation.node129, "b", "c61cf27b-5662-476d-b88f-04de80775e3a");
            generation.edge62 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph27, generation.node132, "m_Output", generation.node128, "b", "00e9955c-b35d-4457-b141-c914e94a8b63");
        }

        static void BuildRootBindingConditions_Attack3_To_Attack4_Condition11(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph27, new[] { "36138109-865b-4384-995e-56076a4c7410", "0537bd26-1cf3-45c2-92c5-1d896d70d4bd", "63866ef1-87b6-4291-9606-ce1230478c83", "360f4fad-44ba-4cb3-bbe8-7728a781f890", "5d6fe35f-531a-46b7-a97a-7daf4f9b8097", "cdd15f3c-f01d-4319-94e8-7fb783690ba7" }, new[] { "eba41a97-036b-4806-98f5-fd20ac4a7332", "0e2a5f6d-e40f-4d76-9691-1dad5b79504e", "55779d5e-fb1d-4291-8f8b-7f62d53014f7", "c61cf27b-5662-476d-b88f-04de80775e3a", "00e9955c-b35d-4457-b141-c914e94a8b63" });
        }

        static void BuildRootBindingConditions_Attack3_To_Attack4_Condition30(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph27, Array.Empty<string>());
        }
    }
}
