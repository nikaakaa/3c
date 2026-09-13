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
        static void LoadStages_Attack1Resources(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.asset = context.ResolveExternalAsset<BtsmtlSkillFlowGraph>("Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.SharedGraph.1eac26e4ad67ccfd6cfe9342d2ea92d7.asset", 11400000L);
            generation.asset1 = context.ResolveExternalAsset<BtsmtlSkillFlowGraph>("Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.SharedGraph.361211b72ded30e65d37533b1fb982da.asset", 11400000L);
            generation.asset2 = context.ResolveExternalAsset<BtsmtlSkillFlowGraph>("Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.SharedGraph.375b9282861f7b1674da6d39b8077a5a.asset", 11400000L);
            generation.asset3 = context.ResolveExternalAsset<BtsmtlSkillFlowGraph>("Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.SharedGraph.646760b2aa9725210b1232e71fb5a514.asset", 11400000L);
        }

        static void BuildCreateStages_Attack12(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph2 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "894a4cd14e8db8f49003b0660b7660ed", BtsmtlSkillFlowGraphRole.StateBody, "Attack1 State Body");
            generation.graph3 = generation.asset;
            generation.graph4 = generation.asset1;
            generation.graph5 = generation.asset2;
            generation.graph6 = generation.asset3;
        }

        static void BuildCreateStages_Attack121(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph2, typeof(BtsmtlSkillStateOnExitFlowNode), "638d1c23-c4ce-4238-99d3-b710dc55e285", "退出状态", new Vector2(120f, 460f));
            generation.node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph2, typeof(BtsmtlSkillRootFlowNode), "7a3023c1-f97c-4d0f-b4a8-0cc0b0b93315", "技能入口", new Vector2(85.33331f, 201.3333f));
            generation.node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph2, typeof(BtsmtlSkillStateOnEnterFlowNode), "95180922-081d-44d4-91a8-c260c6ad5aec", "进入状态", new Vector2(120f, 60f));
            generation.node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph2, typeof(BtsmtlSkillTimelineFlowNode), "e6624058-5b34-43ff-85f5-2cc491af2ef4", "Play Attack1 Timeline", new Vector2(270.6667f, 200.6667f));
            generation.node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillBlackboardSetFlowNode), "10981503-e757-4c4d-a227-a5ed0b5dec44", "Set Attack1Hit", new Vector2(0f, 0f));
            generation.node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillTimelineDisableFlowNode), "42753d81-94dc-43b1-9ce5-b4ad64542f0a", "片段停用", new Vector2(120f, 460f));
            generation.node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillRootFlowNode), "a7d4ffcf-87d0-4a0a-9f21-1b6983eb4a2a", "技能入口", new Vector2(120f, 260f));
            generation.node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillTimelineDestroyFlowNode), "a937f675-27a7-4a9c-86ee-217b6ecdb158", "片段销毁", new Vector2(120f, 660f));
            generation.node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph3, typeof(BtsmtlSkillTimelineEnableFlowNode), "b43ece1c-87fe-419b-ab79-80bc31b2f31c", "片段启用", new Vector2(120f, 60f));
            generation.node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph4, typeof(BtsmtlSkillTimelineEnableFlowNode), "0b641453-748d-4a90-a3ec-67c1d1c07d4a", "片段启用", new Vector2(120f, 60f));
            generation.node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph4, typeof(BtsmtlSkillBlackboardSetFlowNode), "7552468a-1e85-40be-ba1a-19d41eaee472", "Set ComboAccept", new Vector2(0f, 0f));
            generation.node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph4, typeof(BtsmtlSkillTimelineDisableFlowNode), "780c1509-e156-4e33-a73b-a42eb1357e4c", "片段停用", new Vector2(120f, 460f));
            generation.node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph4, typeof(BtsmtlSkillTimelineDestroyFlowNode), "a61fe269-326f-42e5-9239-975c5c597289", "片段销毁", new Vector2(120f, 660f));
            generation.node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph4, typeof(BtsmtlSkillRootFlowNode), "beaa704c-1db9-436e-bb89-63466eeb9723", "技能入口", new Vector2(120f, 260f));
            generation.node18 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph5, typeof(BtsmtlSkillRootFlowNode), "0d4144d5-4595-47c5-bc8c-3680096d8ec9", "技能入口", new Vector2(16.78264f, 281.6265f));
            generation.node20 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph5, typeof(BtsmtlSkillTimelineDisableFlowNode), "7ec0de2b-27b1-4837-b9a3-4d06246a82a6", "片段停用", new Vector2(120f, 460f));
            generation.node19 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph5, typeof(BtsmtlSkillBlackboardSetFlowNode), "a5ae3b82-5ca9-4c50-b9ab-dab5442b0d15", "Set RecoveryLate", new Vector2(0f, 0f));
            generation.node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph5, typeof(BtsmtlSkillTimelineEnableFlowNode), "aa0accd2-9c79-448b-8d45-7675ee2b5903", "片段启用", new Vector2(263.5213f, 48.20374f));
            generation.node21 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph5, typeof(BtsmtlSkillTimelineDestroyFlowNode), "b7214047-3315-41ea-8e09-3cc48ef9699c", "片段销毁", new Vector2(120f, 660f));
            generation.node23 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph6, typeof(BtsmtlSkillRootFlowNode), "216e7006-20c5-48b2-8ab2-68473a3e3ab1", "技能入口", new Vector2(120f, 260f));
            generation.node24 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph6, typeof(BtsmtlSkillBlackboardSetFlowNode), "2461c960-3965-474e-828b-3c7ceeb23068", "Set RecoveryEarly", new Vector2(0f, 0f));
            generation.node26 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph6, typeof(BtsmtlSkillTimelineDestroyFlowNode), "4563a302-8930-4300-83f1-d642d9d7f298", "片段销毁", new Vector2(120f, 660f));
            generation.node25 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph6, typeof(BtsmtlSkillTimelineDisableFlowNode), "931d2f2f-06b4-4afc-a8ec-9a16374145b9", "片段停用", new Vector2(120f, 460f));
            generation.node22 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph6, typeof(BtsmtlSkillTimelineEnableFlowNode), "f41b28cf-3809-4a26-abed-6eaed8d64981", "片段启用", new Vector2(120f, 60f));
        }

        static void BuildConfigureStages_Attack11(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph2, "7862fb9d08504eb5803abe60861e6b79", "RecoveryEarly", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Attack/RecoveryEarly", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryEarly", "Attack1RecoveryEarly", 1004UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph2, "86e50267d98f4ae8976bd806cb96a2d7", "ComboAccept", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Attack/ComboAccept", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "ComboAccept", "Attack1Cancel", 1002UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph2, "cfd05565ebbd49129a54cf1e838ceb45", "RecoveryLate", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Attack/RecoveryLate", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryLate", "Attack1MoveCancel", 1003UL));
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node9).Configure(new BtsmtlSkillBlackboardReference("423b4949895d4fd38d25ee70e4b70602", "00ec42f6d5ede195dcf13e4e27fe7933"), BtsmtlSkillBlackboardValueType.Boolean, null);
            BtsmtlSkillAuthoringCode.SetValue(generation.node9, "m_Value", true);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node14).Configure(new BtsmtlSkillBlackboardReference("86e50267d98f4ae8976bd806cb96a2d7", "894a4cd14e8db8f49003b0660b7660ed"), BtsmtlSkillBlackboardValueType.Boolean, null);
            BtsmtlSkillAuthoringCode.SetValue(generation.node14, "m_Value", true);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node19).Configure(new BtsmtlSkillBlackboardReference("cfd05565ebbd49129a54cf1e838ceb45", "894a4cd14e8db8f49003b0660b7660ed"), BtsmtlSkillBlackboardValueType.Boolean, null);
            BtsmtlSkillAuthoringCode.SetValue(generation.node19, "m_Value", true);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node24).Configure(new BtsmtlSkillBlackboardReference("7862fb9d08504eb5803abe60861e6b79", "894a4cd14e8db8f49003b0660b7660ed"), BtsmtlSkillBlackboardValueType.Boolean, null);
            BtsmtlSkillAuthoringCode.SetValue(generation.node24, "m_Value", true);
        }

        static void BuildBindStages_Attack16(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)generation.state1, generation.graph2);
        }

        static void BuildConnectStages_Attack11(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph2, generation.node4, "Output", generation.node5, "Input", "b061e857-d80a-4e67-b189-7bdf4efa6c15");
            generation.edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph3, generation.node8, "Output", generation.node9, "Input", "b795c215-b546-49ff-8777-1d0f8e0185bf");
            generation.edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph4, generation.node13, "Output", generation.node14, "Input", "008c42ae-bb04-45ff-bb4a-6bbce70fbf97");
            generation.edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph5, generation.node18, "Output", generation.node19, "Input", "4d54f90c-0518-431c-aad3-1131f966578b");
            generation.edge5 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph6, generation.node23, "Output", generation.node24, "Input", "d537834a-88a2-4a76-a45e-9596f597a6f4");
        }

        static void BuildRootBindingStages_Attack12(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph2, new[] { "95180922-081d-44d4-91a8-c260c6ad5aec", "7a3023c1-f97c-4d0f-b4a8-0cc0b0b93315", "e6624058-5b34-43ff-85f5-2cc491af2ef4", "638d1c23-c4ce-4238-99d3-b710dc55e285" }, new[] { "b061e857-d80a-4e67-b189-7bdf4efa6c15" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph3, new[] { "b43ece1c-87fe-419b-ab79-80bc31b2f31c", "a7d4ffcf-87d0-4a0a-9f21-1b6983eb4a2a", "10981503-e757-4c4d-a227-a5ed0b5dec44", "42753d81-94dc-43b1-9ce5-b4ad64542f0a", "a937f675-27a7-4a9c-86ee-217b6ecdb158" }, new[] { "b795c215-b546-49ff-8777-1d0f8e0185bf" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph4, new[] { "0b641453-748d-4a90-a3ec-67c1d1c07d4a", "beaa704c-1db9-436e-bb89-63466eeb9723", "7552468a-1e85-40be-ba1a-19d41eaee472", "780c1509-e156-4e33-a73b-a42eb1357e4c", "a61fe269-326f-42e5-9239-975c5c597289" }, new[] { "008c42ae-bb04-45ff-bb4a-6bbce70fbf97" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph5, new[] { "aa0accd2-9c79-448b-8d45-7675ee2b5903", "0d4144d5-4595-47c5-bc8c-3680096d8ec9", "a5ae3b82-5ca9-4c50-b9ab-dab5442b0d15", "7ec0de2b-27b1-4837-b9a3-4d06246a82a6", "b7214047-3315-41ea-8e09-3cc48ef9699c" }, new[] { "4d54f90c-0518-431c-aad3-1131f966578b" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph6, new[] { "f41b28cf-3809-4a26-abed-6eaed8d64981", "216e7006-20c5-48b2-8ab2-68473a3e3ab1", "2461c960-3965-474e-828b-3c7ceeb23068", "931d2f2f-06b4-4afc-a8ec-9a16374145b9", "4563a302-8930-4300-83f1-d642d9d7f298" }, new[] { "d537834a-88a2-4a76-a45e-9596f597a6f4" });
        }

        static void BuildRootBindingStages_Attack121(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph2, new[] { "7862fb9d08504eb5803abe60861e6b79", "86e50267d98f4ae8976bd806cb96a2d7", "cfd05565ebbd49129a54cf1e838ceb45" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph3, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph4, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph5, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph6, Array.Empty<string>());
        }
    }
}
