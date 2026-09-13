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
        static void BuildCreateStages_DodgeBack1(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph1 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "93e14509-b248-44cc-a263-78c434016564", BtsmtlSkillFlowGraphRole.StateBody, "DodgeBack State Body");
            generation.graph2 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph1, "fa5685f170b175b6f28866f79fa0a083", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision DodgeBackIFrame");
            generation.graph3 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph1, "32d315d4d581f2c38d90f506beb95c02", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryOpen");
        }

        static void BuildCreateStages_DodgeBack4(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph1, typeof(BtsmtlSkillTimelineFlowNode), "61586c6f-c735-4125-8875-41f1ff5f93c7", "Play DodgeBack Timeline", new Vector2(324f, 225.3333f));
            generation.node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph1, typeof(BtsmtlSkillStateOnEnterFlowNode), "e3379cf4-a2a0-4288-b190-d9952dcd30ed", "进入状态", new Vector2(120f, 60f));
            generation.node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph1, typeof(BtsmtlSkillStateOnExitFlowNode), "e6673554-94b0-4f98-a52e-01158c43b92c", "退出状态", new Vector2(120f, 460f));
            generation.node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph1, typeof(BtsmtlSkillRootFlowNode), "f02da1e1-1177-4116-b3ed-766e3251db81", "技能入口", new Vector2(120f, 260f));
            generation.node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph2, typeof(BtsmtlSkillTimelineEnableFlowNode), "3d9b2d59-0fee-42f8-8e53-246b538ecf33", "片段启用", new Vector2(120f, 60f));
            generation.node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph2, typeof(BtsmtlSkillTimelineDestroyFlowNode), "b3e23c3c-2686-43af-b85b-066bfed036c3", "片段销毁", new Vector2(120f, 660f));
            generation.node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph2, typeof(BtsmtlSkillBlackboardSetFlowNode), "c46a2985-824e-4035-8374-efd6ef392c96", "Set DodgeBackIFrame", new Vector2(320f, 0f));
            generation.node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph2, typeof(BtsmtlSkillRootFlowNode), "f46d6c90-8f68-45ff-baab-be044b379d59", "技能入口", new Vector2(120f, 260f));
            generation.node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph2, typeof(BtsmtlSkillTimelineDisableFlowNode), "fb5e08cb-e551-4c3d-9b1b-2a36e5b32b39", "片段停用", new Vector2(120f, 460f));
            generation.node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillBlackboardSetFlowNode), "14ae0b1b-d797-423a-ae5f-4e48a32b5ff1", "Set RecoveryOpen", new Vector2(398.7067f, 223.4471f));
            generation.node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillRootFlowNode), "2c4bb2f9-dc4a-4d9e-98b5-835b1f42c0e5", "技能入口", new Vector2(120f, 260f));
            generation.node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillTimelineDisableFlowNode), "bac2516e-ac8f-45a4-8e6d-da36c659c790", "片段停用", new Vector2(120f, 460f));
            generation.node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillTimelineEnableFlowNode), "d52c4af5-4427-4a97-8234-518f2c15d57a", "片段启用", new Vector2(120f, 60f));
            generation.node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillTimelineDestroyFlowNode), "f6e4ecf3-e1b7-4e61-95da-c0abb154fa94", "片段销毁", new Vector2(120f, 660f));
        }

        static void BuildConfigureStages_DodgeBack1(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph1, "066d593b6cd64cdb9de608744b830ca3", "RecoveryOpen", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Dodge/RecoveryOpen", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryOpen", "DodgeRecoveryCancel", 7002UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph1, "131816a77d7344d7b03d11917cb9c75d", "DodgeBackIFrame", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Windows", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "IFrame", "DodgeBackIFrame", 1UL));
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node8).Configure(new BtsmtlSkillBlackboardReference("131816a77d7344d7b03d11917cb9c75d", "93e14509-b248-44cc-a263-78c434016564"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node13).Configure(new BtsmtlSkillBlackboardReference("066d593b6cd64cdb9de608744b830ca3", "93e14509-b248-44cc-a263-78c434016564"), BtsmtlSkillBlackboardValueType.Boolean, null);
        }

        static void BuildBindStages_DodgeBack2(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)generation.state1, generation.graph1);
        }

        static void BuildConnectStages_DodgeBack1(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph1, generation.node3, "Output", generation.node4, "Input", "1d4bfe96-f332-4168-a910-fecb32dd4a43");
            generation.edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph2, generation.node7, "Output", generation.node8, "Input", "51e60cb8-d436-465c-b8cf-4ca33e8ee920");
            generation.edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph3, generation.node12, "Output", generation.node13, "Input", "8f725471-ff0c-4f15-a97e-4874a314760d");
        }

        static void BuildRootBindingStages_DodgeBack1(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph1, new[] { "e3379cf4-a2a0-4288-b190-d9952dcd30ed", "f02da1e1-1177-4116-b3ed-766e3251db81", "61586c6f-c735-4125-8875-41f1ff5f93c7", "e6673554-94b0-4f98-a52e-01158c43b92c" }, new[] { "1d4bfe96-f332-4168-a910-fecb32dd4a43" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph2, new[] { "3d9b2d59-0fee-42f8-8e53-246b538ecf33", "f46d6c90-8f68-45ff-baab-be044b379d59", "c46a2985-824e-4035-8374-efd6ef392c96", "fb5e08cb-e551-4c3d-9b1b-2a36e5b32b39", "b3e23c3c-2686-43af-b85b-066bfed036c3" }, new[] { "51e60cb8-d436-465c-b8cf-4ca33e8ee920" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph3, new[] { "d52c4af5-4427-4a97-8234-518f2c15d57a", "2c4bb2f9-dc4a-4d9e-98b5-835b1f42c0e5", "14ae0b1b-d797-423a-ae5f-4e48a32b5ff1", "bac2516e-ac8f-45a4-8e6d-da36c659c790", "f6e4ecf3-e1b7-4e61-95da-c0abb154fa94" }, new[] { "8f725471-ff0c-4f15-a97e-4874a314760d" });
        }

        static void BuildRootBindingStages_DodgeBack4(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph1, new[] { "066d593b6cd64cdb9de608744b830ca3", "131816a77d7344d7b03d11917cb9c75d" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph2, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph3, Array.Empty<string>());
        }
    }
}
