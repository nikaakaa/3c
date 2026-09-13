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
        static void BuildCreateStages_Attack413(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph29 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "b2fa5183fe82ae10d37b83f3d03c931c", BtsmtlSkillFlowGraphRole.StateBody, "Attack4 State Body");
            generation.graph30 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph29, "8a91fee1d77efbcc4fa651519191735d", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack4Hit");
            generation.graph31 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph29, "5bc2802ca36ef90431c4b88514935ae8", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryLate");
            generation.graph32 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph29, "0daa704707f9392cb85cf4807d735109", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryEarly");
            generation.graph33 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph29, "be290500eb027394c9d4dc930afd57e7", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision ComboAccept");
        }

        static void BuildCreateStages_Attack432(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node138 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph29, typeof(BtsmtlSkillStateOnExitFlowNode), "24a343a3-fee4-4515-976a-88277e9788a9", "退出状态", new Vector2(120f, 460f));
            generation.node136 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph29, typeof(BtsmtlSkillRootFlowNode), "6fb52eac-5a67-4b5d-8ec8-dc461d311a9e", "技能入口", new Vector2(120f, 260f));
            generation.node137 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph29, typeof(BtsmtlSkillTimelineFlowNode), "9a7317fb-0b6e-47b8-8fdc-31e4d1134e99", "Play Attack4 Timeline", new Vector2(340f, 0f));
            generation.node135 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph29, typeof(BtsmtlSkillStateOnEnterFlowNode), "b18d9def-e8f6-47b8-83ce-60ee9f0a7eaf", "进入状态", new Vector2(120f, 60f));
            generation.node143 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph30, typeof(BtsmtlSkillTimelineDestroyFlowNode), "180b1f83-5851-4817-a2c5-87123927cb97", "片段销毁", new Vector2(120f, 660f));
            generation.node141 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph30, typeof(BtsmtlSkillBlackboardSetFlowNode), "2367bbe3-3d42-4a6a-92fc-99dec3f5e314", "Set Attack2Hit", new Vector2(320f, 0f));
            generation.node142 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph30, typeof(BtsmtlSkillTimelineDisableFlowNode), "2db3e4f7-a02b-40a5-b43f-e89e466bef08", "片段停用", new Vector2(120f, 460f));
            generation.node140 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph30, typeof(BtsmtlSkillRootFlowNode), "48cdc479-d57e-4da2-89fe-83fbaa37f7fb", "技能入口", new Vector2(120f, 260f));
            generation.node139 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph30, typeof(BtsmtlSkillTimelineEnableFlowNode), "c36a7c0e-7ad8-4dc1-85ec-ca5820ac75e3", "片段启用", new Vector2(120f, 60f));
            generation.node145 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph31, typeof(BtsmtlSkillRootFlowNode), "2bd9d21a-6a19-4b5b-becb-7787b68d9e25", "技能入口", new Vector2(120f, 260f));
            generation.node144 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph31, typeof(BtsmtlSkillTimelineEnableFlowNode), "68d76ee6-8062-4fde-9c8b-a377369614ec", "片段启用", new Vector2(120f, 60f));
            generation.node147 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph31, typeof(BtsmtlSkillTimelineDisableFlowNode), "a929c5ad-d2b4-401c-95be-e406880b3c8a", "片段停用", new Vector2(120f, 460f));
            generation.node146 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph31, typeof(BtsmtlSkillBlackboardSetFlowNode), "c54c9d37-f042-409b-b7d3-102af84ddf01", "Set RecoveryLate", new Vector2(320f, 0f));
            generation.node148 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph31, typeof(BtsmtlSkillTimelineDestroyFlowNode), "c9930916-a8b5-4ac5-bcd4-d34a14a30f37", "片段销毁", new Vector2(120f, 660f));
            generation.node149 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph32, typeof(BtsmtlSkillTimelineEnableFlowNode), "2be81e47-2da0-49c0-b4a9-c0a8d3db86fd", "片段启用", new Vector2(120f, 60f));
            generation.node153 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph32, typeof(BtsmtlSkillTimelineDestroyFlowNode), "36a3c728-f042-4427-97eb-f1bdd831974b", "片段销毁", new Vector2(120f, 660f));
            generation.node151 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph32, typeof(BtsmtlSkillBlackboardSetFlowNode), "91d67a18-db16-41d5-bae2-b6acac59389d", "Set RecoveryEarly", new Vector2(320f, 0f));
            generation.node152 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph32, typeof(BtsmtlSkillTimelineDisableFlowNode), "af29316e-f707-40b7-aa79-bdc941f736ff", "片段停用", new Vector2(120f, 460f));
            generation.node150 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph32, typeof(BtsmtlSkillRootFlowNode), "aff306c6-aa1b-4635-b85a-5e56b8d9d9b0", "技能入口", new Vector2(120f, 260f));
            generation.node156 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph33, typeof(BtsmtlSkillBlackboardSetFlowNode), "468b164f-c02b-4986-a333-834fd1bb8b45", "Set ComboAccept", new Vector2(320f, 0f));
            generation.node158 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph33, typeof(BtsmtlSkillTimelineDestroyFlowNode), "5fe03763-9315-4fb3-9dc4-b865776a0d42", "片段销毁", new Vector2(120f, 660f));
            generation.node155 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph33, typeof(BtsmtlSkillRootFlowNode), "6b8224d2-bfbe-483f-a97c-a5a786118293", "技能入口", new Vector2(120f, 260f));
            generation.node157 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph33, typeof(BtsmtlSkillTimelineDisableFlowNode), "c2200340-5255-4ae8-af56-a5cdda3c3564", "片段停用", new Vector2(120f, 460f));
            generation.node154 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph33, typeof(BtsmtlSkillTimelineEnableFlowNode), "c6c1c2eb-5dbf-4f06-be7d-64f21c33501c", "片段启用", new Vector2(120f, 60f));
        }

        static void BuildConfigureStages_Attack411(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph29, "16015282780a4579817a0de9a10ebe4f", "ComboAccept", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Attack/ComboAccept", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "ComboAccept", "Attack4Cancel", 4002UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph29, "b96e0a2121e14eb68a1fb6aeff18fd37", "RecoveryEarly", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Attack/RecoveryEarly", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryEarly", "Attack4RecoveryEarly", 4004UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph29, "f2d1e42a70ee492784a4dbe30a1e014f", "RecoveryLate", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Attack/RecoveryLate", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryLate", "Attack4MoveCancel", 4003UL));
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node141).Configure(new BtsmtlSkillBlackboardReference("bf611f54e2cb477297162996345d8a34", "00ec42f6d5ede195dcf13e4e27fe7933"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node146).Configure(new BtsmtlSkillBlackboardReference("f2d1e42a70ee492784a4dbe30a1e014f", "b2fa5183fe82ae10d37b83f3d03c931c"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node151).Configure(new BtsmtlSkillBlackboardReference("b96e0a2121e14eb68a1fb6aeff18fd37", "b2fa5183fe82ae10d37b83f3d03c931c"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node156).Configure(new BtsmtlSkillBlackboardReference("16015282780a4579817a0de9a10ebe4f", "b2fa5183fe82ae10d37b83f3d03c931c"), BtsmtlSkillBlackboardValueType.Boolean, null);
        }

        static void BuildBindStages_Attack49(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)generation.state5, generation.graph29);
        }

        static void BuildConnectStages_Attack412(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge64 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph29, generation.node136, "Output", generation.node137, "Input", "db4912ce-3e00-4068-a9ee-89d44324e4b6");
            generation.edge65 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph30, generation.node140, "Output", generation.node141, "Input", "bbfaa6a1-5523-4e32-8347-2a2beaf3bd9a");
            generation.edge66 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph31, generation.node145, "Output", generation.node146, "Input", "fbda6ca0-0644-4c38-826c-1bdd2966daa0");
            generation.edge67 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph32, generation.node150, "Output", generation.node151, "Input", "a631a939-688e-40f8-9d17-b0b1c21bf58f");
            generation.edge68 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph33, generation.node155, "Output", generation.node156, "Input", "457a5a36-3e6b-4c9b-bd8d-ca7cd60a3906");
        }

        static void BuildRootBindingStages_Attack413(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph29, new[] { "b18d9def-e8f6-47b8-83ce-60ee9f0a7eaf", "6fb52eac-5a67-4b5d-8ec8-dc461d311a9e", "9a7317fb-0b6e-47b8-8fdc-31e4d1134e99", "24a343a3-fee4-4515-976a-88277e9788a9" }, new[] { "db4912ce-3e00-4068-a9ee-89d44324e4b6" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph30, new[] { "c36a7c0e-7ad8-4dc1-85ec-ca5820ac75e3", "48cdc479-d57e-4da2-89fe-83fbaa37f7fb", "2367bbe3-3d42-4a6a-92fc-99dec3f5e314", "2db3e4f7-a02b-40a5-b43f-e89e466bef08", "180b1f83-5851-4817-a2c5-87123927cb97" }, new[] { "bbfaa6a1-5523-4e32-8347-2a2beaf3bd9a" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph31, new[] { "68d76ee6-8062-4fde-9c8b-a377369614ec", "2bd9d21a-6a19-4b5b-becb-7787b68d9e25", "c54c9d37-f042-409b-b7d3-102af84ddf01", "a929c5ad-d2b4-401c-95be-e406880b3c8a", "c9930916-a8b5-4ac5-bcd4-d34a14a30f37" }, new[] { "fbda6ca0-0644-4c38-826c-1bdd2966daa0" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph32, new[] { "2be81e47-2da0-49c0-b4a9-c0a8d3db86fd", "aff306c6-aa1b-4635-b85a-5e56b8d9d9b0", "91d67a18-db16-41d5-bae2-b6acac59389d", "af29316e-f707-40b7-aa79-bdc941f736ff", "36a3c728-f042-4427-97eb-f1bdd831974b" }, new[] { "a631a939-688e-40f8-9d17-b0b1c21bf58f" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph33, new[] { "c6c1c2eb-5dbf-4f06-be7d-64f21c33501c", "6b8224d2-bfbe-483f-a97c-a5a786118293", "468b164f-c02b-4986-a333-834fd1bb8b45", "c2200340-5255-4ae8-af56-a5cdda3c3564", "5fe03763-9315-4fb3-9dc4-b865776a0d42" }, new[] { "457a5a36-3e6b-4c9b-bd8d-ca7cd60a3906" });
        }

        static void BuildRootBindingStages_Attack432(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph29, new[] { "16015282780a4579817a0de9a10ebe4f", "b96e0a2121e14eb68a1fb6aeff18fd37", "f2d1e42a70ee492784a4dbe30a1e014f" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph30, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph31, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph32, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph33, Array.Empty<string>());
        }
    }
}
