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
        static void BuildCreateStages_DodgeForward1(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph1 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "01974466-06fd-4295-a76e-74a85e118390", BtsmtlSkillFlowGraphRole.StateBody, "DodgeForward State Body");
            generation.graph2 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph1, "df8fd04f1815742f5c253fd8f4ce3e77", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision DodgeForwardIFrame");
            generation.graph3 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph1, "49c3ae7850d26f945fc7ef1aed38c456", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryOpen");
        }

        static void BuildCreateStages_DodgeForward4(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph1, typeof(BtsmtlSkillStateOnExitFlowNode), "4a1648fa-c157-43c8-a5a0-272ee43a87aa", "退出状态", new Vector2(120f, 460f));
            generation.node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph1, typeof(BtsmtlSkillStateOnEnterFlowNode), "555d035e-3251-417d-83ac-c0b9c5d72314", "进入状态", new Vector2(120f, 60f));
            generation.node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph1, typeof(BtsmtlSkillRootFlowNode), "a757928f-ae8b-489d-9f8e-bf586374e90a", "技能入口", new Vector2(120f, 260f));
            generation.node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph1, typeof(BtsmtlSkillTimelineFlowNode), "c78616f5-3e17-4b1d-b6ec-7a200ebec941", "Play DodgeForward Timeline", new Vector2(369.3334f, 14f));
            generation.node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph2, typeof(BtsmtlSkillTimelineEnableFlowNode), "8c4daf5a-133b-49d7-bd36-04729d9346c7", "片段启用", new Vector2(120f, 60f));
            generation.node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph2, typeof(BtsmtlSkillTimelineDisableFlowNode), "a5909657-eed5-4c74-9172-25f6ded0b8b7", "片段停用", new Vector2(114.7562f, 387.6369f));
            generation.node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph2, typeof(BtsmtlSkillBlackboardSetFlowNode), "adeb7e88-3e33-4201-bd0e-92a00001eaf2", "Set DodgeForwardIFrame", new Vector2(320f, 0f));
            generation.node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph2, typeof(BtsmtlSkillTimelineDestroyFlowNode), "b0d239f7-57a2-426f-96d1-62a85034900f", "片段销毁", new Vector2(109.5126f, 526.8099f));
            generation.node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph2, typeof(BtsmtlSkillRootFlowNode), "ee704955-b263-4e08-ac4f-d79c65aa2f19", "技能入口", new Vector2(101.1227f, 241.1227f));
            generation.node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillTimelineDisableFlowNode), "0b17a643-0e7a-45d5-8d97-238ace861568", "片段停用", new Vector2(120f, 460f));
            generation.node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillTimelineEnableFlowNode), "50a3ad51-d102-46c6-8f59-3556f8101df7", "片段启用", new Vector2(120f, 60f));
            generation.node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillTimelineDestroyFlowNode), "8855643c-4b97-411e-bca9-e19ae9542912", "片段销毁", new Vector2(120f, 660f));
            generation.node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillRootFlowNode), "b2376875-f4a0-4c2a-adaf-a6e4c0278a82", "技能入口", new Vector2(120f, 260f));
            generation.node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillBlackboardSetFlowNode), "b2552514-5758-45ae-9201-f7293a7478c2", "Set RecoveryOpen", new Vector2(320f, 0f));
        }

        static void BuildConfigureStages_DodgeForward1(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph1, "4a7a79cd9fc14a8f807d3d9580548cc1", "RecoveryOpen", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Dodge/RecoveryOpen", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryOpen", "DodgeForwardRecoveryOpen", 7003UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph1, "d9ecf57b06514746a1eed7a5a7217a6b", "DodgeForwardIFrame", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "IFrame", "DodgeForwardIFrame", 2UL));
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node8).Configure(new BtsmtlSkillBlackboardReference("d9ecf57b06514746a1eed7a5a7217a6b", "01974466-06fd-4295-a76e-74a85e118390"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node13).Configure(new BtsmtlSkillBlackboardReference("4a7a79cd9fc14a8f807d3d9580548cc1", "01974466-06fd-4295-a76e-74a85e118390"), BtsmtlSkillBlackboardValueType.Boolean, null);
        }

        static void BuildBindStages_DodgeForward2(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)generation.state1, generation.graph1);
        }

        static void BuildConnectStages_DodgeForward1(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph1, generation.node3, "Output", generation.node4, "Input", "1823df30-39a2-4088-9b6d-6e4f2a55cf53");
            generation.edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph2, generation.node7, "Output", generation.node8, "Input", "41bd0710-f8cf-48f3-bad7-29c7416b6098");
            generation.edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph3, generation.node12, "Output", generation.node13, "Input", "124777d1-9bbf-4292-9ca7-4b89326a8ed4");
        }

        static void BuildRootBindingStages_DodgeForward1(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph1, new[] { "555d035e-3251-417d-83ac-c0b9c5d72314", "a757928f-ae8b-489d-9f8e-bf586374e90a", "c78616f5-3e17-4b1d-b6ec-7a200ebec941", "4a1648fa-c157-43c8-a5a0-272ee43a87aa" }, new[] { "1823df30-39a2-4088-9b6d-6e4f2a55cf53" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph2, new[] { "8c4daf5a-133b-49d7-bd36-04729d9346c7", "ee704955-b263-4e08-ac4f-d79c65aa2f19", "adeb7e88-3e33-4201-bd0e-92a00001eaf2", "a5909657-eed5-4c74-9172-25f6ded0b8b7", "b0d239f7-57a2-426f-96d1-62a85034900f" }, new[] { "41bd0710-f8cf-48f3-bad7-29c7416b6098" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph3, new[] { "50a3ad51-d102-46c6-8f59-3556f8101df7", "b2376875-f4a0-4c2a-adaf-a6e4c0278a82", "b2552514-5758-45ae-9201-f7293a7478c2", "0b17a643-0e7a-45d5-8d97-238ace861568", "8855643c-4b97-411e-bca9-e19ae9542912" }, new[] { "124777d1-9bbf-4292-9ca7-4b89326a8ed4" });
        }

        static void BuildRootBindingStages_DodgeForward4(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph1, new[] { "4a7a79cd9fc14a8f807d3d9580548cc1", "d9ecf57b06514746a1eed7a5a7217a6b" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph2, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph3, Array.Empty<string>());
        }
    }
}
