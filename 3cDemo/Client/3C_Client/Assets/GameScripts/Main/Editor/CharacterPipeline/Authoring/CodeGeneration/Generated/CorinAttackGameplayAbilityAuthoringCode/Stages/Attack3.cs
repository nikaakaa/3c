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
        static void BuildCreateStages_Attack39(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph20 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "ab4f75a3dbba67da55dbf4a46872eadd", BtsmtlSkillFlowGraphRole.StateBody, "Attack3 State Body");
            generation.graph21 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph20, "8a14fcb8765abbc619bfd21d1a558e49", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryEarly");
            generation.graph22 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph20, "c1fda7608ebd58824cf60b93566ccd3a", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryLate");
            generation.graph23 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph20, "bed856871c2e9c2d2e326da4a80aba66", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack3Hit");
            generation.graph24 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph20, "d6af200a24afd284f06690675a70f03e", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision ComboAccept");
        }

        static void BuildCreateStages_Attack328(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node94 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph20, typeof(BtsmtlSkillStateOnExitFlowNode), "6951fc02-1906-4e01-8125-b04cb243a9aa", "退出状态", new Vector2(120f, 460f));
            generation.node93 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph20, typeof(BtsmtlSkillTimelineFlowNode), "6bee7def-7e74-41c3-9dbd-5157947289cc", "Play Attack3 Timeline", new Vector2(340f, 0f));
            generation.node91 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph20, typeof(BtsmtlSkillStateOnEnterFlowNode), "db4c14b7-d2a8-4a75-a4a4-019548b39838", "进入状态", new Vector2(120f, 60f));
            generation.node92 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph20, typeof(BtsmtlSkillRootFlowNode), "e81cf2fd-f1c5-48ca-9993-0c4a2a06eaec", "技能入口", new Vector2(120f, 260f));
            generation.node97 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph21, typeof(BtsmtlSkillBlackboardSetFlowNode), "0710cf99-ac01-4c66-859b-f94e59c8ec63", "Set RecoveryEarly", new Vector2(320f, 0f));
            generation.node99 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph21, typeof(BtsmtlSkillTimelineDestroyFlowNode), "1d5fb466-0f59-4427-8a5e-9e0fca15f283", "片段销毁", new Vector2(120f, 660f));
            generation.node98 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph21, typeof(BtsmtlSkillTimelineDisableFlowNode), "6a2bdc3d-5dfe-4f4c-ab71-f6f6bacfe0df", "片段停用", new Vector2(120f, 460f));
            generation.node95 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph21, typeof(BtsmtlSkillTimelineEnableFlowNode), "ae7064ec-66a6-42c0-948f-d35656c8917f", "片段启用", new Vector2(120f, 60f));
            generation.node96 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph21, typeof(BtsmtlSkillRootFlowNode), "efd8fc6c-f974-487e-8805-4e93ccb268b7", "技能入口", new Vector2(120f, 260f));
            generation.node102 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph22, typeof(BtsmtlSkillBlackboardSetFlowNode), "43cdde6d-ea89-4810-b94d-53917023c7ed", "Set RecoveryLate", new Vector2(320f, 0f));
            generation.node100 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph22, typeof(BtsmtlSkillTimelineEnableFlowNode), "8f734e26-9719-4520-bcfb-eaab4e162ba4", "片段启用", new Vector2(120f, 60f));
            generation.node101 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph22, typeof(BtsmtlSkillRootFlowNode), "ade25018-4120-4d07-86a0-550b4fa6301d", "技能入口", new Vector2(120f, 260f));
            generation.node104 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph22, typeof(BtsmtlSkillTimelineDestroyFlowNode), "b4152a99-84f5-4559-ad2b-c93dbf433acf", "片段销毁", new Vector2(120f, 660f));
            generation.node103 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph22, typeof(BtsmtlSkillTimelineDisableFlowNode), "d12bba92-ea64-46eb-a734-7874fb65f571", "片段停用", new Vector2(120f, 460f));
            generation.node106 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph23, typeof(BtsmtlSkillRootFlowNode), "00b8c14c-6ed8-42d9-b583-de2c8b7e3af1", "技能入口", new Vector2(120f, 260f));
            generation.node108 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph23, typeof(BtsmtlSkillTimelineDisableFlowNode), "1189562e-2142-426b-8587-fea255d6186a", "片段停用", new Vector2(120f, 460f));
            generation.node107 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph23, typeof(BtsmtlSkillBlackboardSetFlowNode), "3f4289fd-d8a3-4965-afa0-c751f12277ca", "Set Attack2Hit", new Vector2(320f, 0f));
            generation.node105 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph23, typeof(BtsmtlSkillTimelineEnableFlowNode), "57fdd827-1fc7-421d-8c26-d5f8c75ce52b", "片段启用", new Vector2(120f, 60f));
            generation.node109 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph23, typeof(BtsmtlSkillTimelineDestroyFlowNode), "8378c59f-092a-497e-8ab2-a876ad741819", "片段销毁", new Vector2(120f, 660f));
            generation.node113 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph24, typeof(BtsmtlSkillTimelineDisableFlowNode), "03407a2d-89d5-412d-b170-4e0621d7bf00", "片段停用", new Vector2(120f, 460f));
            generation.node114 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph24, typeof(BtsmtlSkillTimelineDestroyFlowNode), "06364eed-3aee-409a-b1e0-aa1422235270", "片段销毁", new Vector2(120f, 660f));
            generation.node110 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph24, typeof(BtsmtlSkillTimelineEnableFlowNode), "1e0cab18-9869-4abf-b659-905c504e89da", "片段启用", new Vector2(120f, 60f));
            generation.node112 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph24, typeof(BtsmtlSkillBlackboardSetFlowNode), "596b7aab-3f96-4f8e-8f25-66fefb1c6985", "Set ComboAccept", new Vector2(320f, 0f));
            generation.node111 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph24, typeof(BtsmtlSkillRootFlowNode), "75ba637e-76e0-41e1-b70f-ad81ecd4a3fe", "技能入口", new Vector2(120f, 260f));
        }

        static void BuildConfigureStages_Attack38(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph20, "6315ad83888944e0bbad7595097b60f9", "ComboAccept", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Attack/ComboAccept", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "ComboAccept", "Attack3Cancel", 3002UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph20, "a586c674815f46359af6a0ff35156394", "RecoveryLate", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Attack/RecoveryLate", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryLate", "Attack3MoveCancel", 3003UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph20, "ed22b7318b054a84a89a769fc8ec9fef", "RecoveryEarly", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Attack/RecoveryEarly", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryEarly", "Attack3RecoveryEarly", 3004UL));
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node97).Configure(new BtsmtlSkillBlackboardReference("ed22b7318b054a84a89a769fc8ec9fef", "ab4f75a3dbba67da55dbf4a46872eadd"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node102).Configure(new BtsmtlSkillBlackboardReference("a586c674815f46359af6a0ff35156394", "ab4f75a3dbba67da55dbf4a46872eadd"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node107).Configure(new BtsmtlSkillBlackboardReference("361015344dc440ee81193dd42bca2251", "00ec42f6d5ede195dcf13e4e27fe7933"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node112).Configure(new BtsmtlSkillBlackboardReference("6315ad83888944e0bbad7595097b60f9", "ab4f75a3dbba67da55dbf4a46872eadd"), BtsmtlSkillBlackboardValueType.Boolean, null);
        }

        static void BuildBindStages_Attack38(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)generation.state4, generation.graph20);
        }

        static void BuildConnectStages_Attack38(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge43 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph20, generation.node92, "Output", generation.node93, "Input", "fee2f0f3-0d8a-4de9-9523-c0dbe8ddf430");
            generation.edge44 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph21, generation.node96, "Output", generation.node97, "Input", "fbdf54c3-2c3d-427c-a1fa-398e34d773ec");
            generation.edge45 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph22, generation.node101, "Output", generation.node102, "Input", "83b924a0-1741-4854-b2e3-476ca8eed014");
            generation.edge46 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph23, generation.node106, "Output", generation.node107, "Input", "343b44e4-ff07-4496-bf13-85328f1aca0c");
            generation.edge47 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph24, generation.node111, "Output", generation.node112, "Input", "03f826d5-8a6d-4aa0-8e56-1d5c915385da");
        }

        static void BuildRootBindingStages_Attack39(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph20, new[] { "db4c14b7-d2a8-4a75-a4a4-019548b39838", "e81cf2fd-f1c5-48ca-9993-0c4a2a06eaec", "6bee7def-7e74-41c3-9dbd-5157947289cc", "6951fc02-1906-4e01-8125-b04cb243a9aa" }, new[] { "fee2f0f3-0d8a-4de9-9523-c0dbe8ddf430" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph21, new[] { "ae7064ec-66a6-42c0-948f-d35656c8917f", "efd8fc6c-f974-487e-8805-4e93ccb268b7", "0710cf99-ac01-4c66-859b-f94e59c8ec63", "6a2bdc3d-5dfe-4f4c-ab71-f6f6bacfe0df", "1d5fb466-0f59-4427-8a5e-9e0fca15f283" }, new[] { "fbdf54c3-2c3d-427c-a1fa-398e34d773ec" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph22, new[] { "8f734e26-9719-4520-bcfb-eaab4e162ba4", "ade25018-4120-4d07-86a0-550b4fa6301d", "43cdde6d-ea89-4810-b94d-53917023c7ed", "d12bba92-ea64-46eb-a734-7874fb65f571", "b4152a99-84f5-4559-ad2b-c93dbf433acf" }, new[] { "83b924a0-1741-4854-b2e3-476ca8eed014" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph23, new[] { "57fdd827-1fc7-421d-8c26-d5f8c75ce52b", "00b8c14c-6ed8-42d9-b583-de2c8b7e3af1", "3f4289fd-d8a3-4965-afa0-c751f12277ca", "1189562e-2142-426b-8587-fea255d6186a", "8378c59f-092a-497e-8ab2-a876ad741819" }, new[] { "343b44e4-ff07-4496-bf13-85328f1aca0c" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph24, new[] { "1e0cab18-9869-4abf-b659-905c504e89da", "75ba637e-76e0-41e1-b70f-ad81ecd4a3fe", "596b7aab-3f96-4f8e-8f25-66fefb1c6985", "03407a2d-89d5-412d-b170-4e0621d7bf00", "06364eed-3aee-409a-b1e0-aa1422235270" }, new[] { "03f826d5-8a6d-4aa0-8e56-1d5c915385da" });
        }

        static void BuildRootBindingStages_Attack328(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph20, new[] { "6315ad83888944e0bbad7595097b60f9", "a586c674815f46359af6a0ff35156394", "ed22b7318b054a84a89a769fc8ec9fef" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph21, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph22, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph23, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph24, Array.Empty<string>());
        }
    }
}
