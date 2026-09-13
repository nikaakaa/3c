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
        static void BuildCreateStages_Attack517(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph38 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "d0c0d504787f8e6989ba2bae6aa1a49a", BtsmtlSkillFlowGraphRole.StateBody, "Attack5 State Body");
            generation.graph39 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph38, "47990a9445e2bdec6a74d1f4522332ff", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5Hit");
            generation.graph40 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph38, "9fd63abef3169807a4ac031a16341e66", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryEarly");
            generation.graph41 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph38, "f73db1a04c83dfd8b64ece524d072146", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryLate");
        }

        static void BuildCreateStages_Attack536(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node182 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph38, typeof(BtsmtlSkillStateOnExitFlowNode), "1eebebc7-5016-4843-9274-36fcf0abe727", "退出状态", new Vector2(120f, 460f));
            generation.node181 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph38, typeof(BtsmtlSkillTimelineFlowNode), "4332be4e-00aa-4ac8-8d3c-784566321004", "Play Attack5 Timeline", new Vector2(340f, 0f));
            generation.node180 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph38, typeof(BtsmtlSkillRootFlowNode), "5364b12d-3098-4c39-808f-544bc834c216", "技能入口", new Vector2(120f, 260f));
            generation.node179 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph38, typeof(BtsmtlSkillStateOnEnterFlowNode), "5ab4b41e-4ca0-43cf-956c-f372a0293d0d", "进入状态", new Vector2(120f, 60f));
            generation.node183 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph39, typeof(BtsmtlSkillTimelineEnableFlowNode), "0f1fd664-33df-44bf-8e83-e82a3caf6822", "片段启用", new Vector2(120f, 60f));
            generation.node187 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph39, typeof(BtsmtlSkillTimelineDestroyFlowNode), "2378333f-ce0c-4f60-bf69-f01bcc591053", "片段销毁", new Vector2(120f, 660f));
            generation.node186 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph39, typeof(BtsmtlSkillTimelineDisableFlowNode), "409b788f-7b4a-4e03-ae38-0664b0ed7b4d", "片段停用", new Vector2(120f, 460f));
            generation.node184 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph39, typeof(BtsmtlSkillRootFlowNode), "f2d16ae4-4acb-41c9-92c1-b1afcae1bb36", "技能入口", new Vector2(120f, 260f));
            generation.node185 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph39, typeof(BtsmtlSkillBlackboardSetFlowNode), "fad0f316-daa7-47fa-9284-56f6144623ef", "Set Attack2Hit", new Vector2(320f, 0f));
            generation.node191 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph40, typeof(BtsmtlSkillTimelineDisableFlowNode), "25f06ce9-cf99-4be6-8c8c-41000dd6c5a6", "片段停用", new Vector2(120f, 460f));
            generation.node189 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph40, typeof(BtsmtlSkillRootFlowNode), "36ab6c98-6b04-40e6-aaf1-3712dffda347", "技能入口", new Vector2(120f, 260f));
            generation.node188 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph40, typeof(BtsmtlSkillTimelineEnableFlowNode), "7d36691a-7950-4114-bd30-528b660f2b77", "片段启用", new Vector2(120f, 60f));
            generation.node192 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph40, typeof(BtsmtlSkillTimelineDestroyFlowNode), "a1bfb095-96f8-419d-b537-4729a27224e5", "片段销毁", new Vector2(120f, 660f));
            generation.node190 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph40, typeof(BtsmtlSkillBlackboardSetFlowNode), "fe3ee5fe-a9fd-4d7e-8949-a151fca7aa22", "Set RecoveryEarly", new Vector2(320f, 0f));
            generation.node196 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph41, typeof(BtsmtlSkillTimelineDisableFlowNode), "369ce7a8-b06a-4af1-9d16-01cd5e201eee", "片段停用", new Vector2(120f, 460f));
            generation.node193 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph41, typeof(BtsmtlSkillTimelineEnableFlowNode), "7efaa326-d89a-480f-8509-a501136e0c73", "片段启用", new Vector2(120f, 60f));
            generation.node194 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph41, typeof(BtsmtlSkillRootFlowNode), "92969358-7eef-4928-8bc3-f58eb568d069", "技能入口", new Vector2(120f, 260f));
            generation.node195 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph41, typeof(BtsmtlSkillBlackboardSetFlowNode), "93b0eb27-adbf-4da9-ad9c-23d24f774f28", "Set RecoveryLate", new Vector2(320f, 0f));
            generation.node197 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph41, typeof(BtsmtlSkillTimelineDestroyFlowNode), "dae97261-bbf7-4c7c-85b9-023990514765", "片段销毁", new Vector2(120f, 660f));
        }

        static void BuildConfigureStages_Attack515(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph38, "f3f45c6944c247539e29c5b37ba6dede", "RecoveryEarly", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Attack/RecoveryEarly", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryEarly", "Attack5RecoveryEarly", 5004UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph38, "fd926414f6864445b2c3c2050a158d04", "RecoveryLate", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Attack/RecoveryLate", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryLate", "Attack5MoveCancel", 5003UL));
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node185).Configure(new BtsmtlSkillBlackboardReference("6225401bc79441dca5eaab16bdbc0644", "00ec42f6d5ede195dcf13e4e27fe7933"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node190).Configure(new BtsmtlSkillBlackboardReference("f3f45c6944c247539e29c5b37ba6dede", "d0c0d504787f8e6989ba2bae6aa1a49a"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node195).Configure(new BtsmtlSkillBlackboardReference("fd926414f6864445b2c3c2050a158d04", "d0c0d504787f8e6989ba2bae6aa1a49a"), BtsmtlSkillBlackboardValueType.Boolean, null);
        }

        static void BuildBindStages_Attack510(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)generation.state6, generation.graph38);
        }

        static void BuildConnectStages_Attack516(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge85 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph38, generation.node180, "Output", generation.node181, "Input", "f0437050-1c9f-4d8d-a669-2d75581a1841");
            generation.edge86 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph39, generation.node184, "Output", generation.node185, "Input", "c4931a91-9791-48df-9571-b7a0f9cb13e8");
            generation.edge87 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph40, generation.node189, "Output", generation.node190, "Input", "5f3e4157-bb2b-4619-aab9-2c6d04200890");
            generation.edge88 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph41, generation.node194, "Output", generation.node195, "Input", "422f100d-3716-425c-abd2-595b3cfe8768");
        }

        static void BuildRootBindingStages_Attack517(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph38, new[] { "5ab4b41e-4ca0-43cf-956c-f372a0293d0d", "5364b12d-3098-4c39-808f-544bc834c216", "4332be4e-00aa-4ac8-8d3c-784566321004", "1eebebc7-5016-4843-9274-36fcf0abe727" }, new[] { "f0437050-1c9f-4d8d-a669-2d75581a1841" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph39, new[] { "0f1fd664-33df-44bf-8e83-e82a3caf6822", "f2d16ae4-4acb-41c9-92c1-b1afcae1bb36", "fad0f316-daa7-47fa-9284-56f6144623ef", "409b788f-7b4a-4e03-ae38-0664b0ed7b4d", "2378333f-ce0c-4f60-bf69-f01bcc591053" }, new[] { "c4931a91-9791-48df-9571-b7a0f9cb13e8" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph40, new[] { "7d36691a-7950-4114-bd30-528b660f2b77", "36ab6c98-6b04-40e6-aaf1-3712dffda347", "fe3ee5fe-a9fd-4d7e-8949-a151fca7aa22", "25f06ce9-cf99-4be6-8c8c-41000dd6c5a6", "a1bfb095-96f8-419d-b537-4729a27224e5" }, new[] { "5f3e4157-bb2b-4619-aab9-2c6d04200890" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph41, new[] { "7efaa326-d89a-480f-8509-a501136e0c73", "92969358-7eef-4928-8bc3-f58eb568d069", "93b0eb27-adbf-4da9-ad9c-23d24f774f28", "369ce7a8-b06a-4af1-9d16-01cd5e201eee", "dae97261-bbf7-4c7c-85b9-023990514765" }, new[] { "422f100d-3716-425c-abd2-595b3cfe8768" });
        }

        static void BuildRootBindingStages_Attack536(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph38, new[] { "f3f45c6944c247539e29c5b37ba6dede", "fd926414f6864445b2c3c2050a158d04" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph39, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph40, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph41, Array.Empty<string>());
        }
    }
}
