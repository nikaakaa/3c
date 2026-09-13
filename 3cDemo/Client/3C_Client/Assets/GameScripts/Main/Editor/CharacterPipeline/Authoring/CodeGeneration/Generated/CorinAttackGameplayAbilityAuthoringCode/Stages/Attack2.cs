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
        static void BuildCreateStages_Attack26(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph11 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "ef6ea798d23cbb42daa656723c5263b2", BtsmtlSkillFlowGraphRole.StateBody, "Attack2 State Body");
            generation.graph12 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph11, "5fa11a1cf6f511cbdbd4f3ed5a6e0502", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryEarly");
            generation.graph13 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph11, "ca2fa58353e2116d8c29572093b89a74", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack2Hit");
            generation.graph14 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph11, "8714858c3f851c577134c9293f2f1f84", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision ComboAccept");
            generation.graph15 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph11, "6cdb5dac773bab990391f7c8ccf76dbe", BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryLate");
        }

        static void BuildCreateStages_Attack225(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node49 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph11, typeof(BtsmtlSkillTimelineFlowNode), "01780c25-3f89-4117-9ec0-24dd9c32db70", "Play Attack2 Timeline", new Vector2(340f, 0f));
            generation.node50 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph11, typeof(BtsmtlSkillStateOnExitFlowNode), "12f26908-7735-4a0d-8fc1-9a8b4bc08c57", "退出状态", new Vector2(120f, 460f));
            generation.node47 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph11, typeof(BtsmtlSkillStateOnEnterFlowNode), "8ea76387-27ce-4ab7-a036-47fbcddb9c4c", "进入状态", new Vector2(120f, 60f));
            generation.node48 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph11, typeof(BtsmtlSkillRootFlowNode), "db13fa44-b0d4-4e90-b40a-92b1e6226d0d", "技能入口", new Vector2(120f, 260f));
            generation.node55 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph12, typeof(BtsmtlSkillTimelineDestroyFlowNode), "04197c9c-ea02-414c-8d5e-1ca9e9e8c90b", "片段销毁", new Vector2(120f, 660f));
            generation.node53 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph12, typeof(BtsmtlSkillBlackboardSetFlowNode), "3e573873-16f9-4a38-85b3-aafdaf8f6e82", "Set RecoveryEarly", new Vector2(320f, 0f));
            generation.node54 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph12, typeof(BtsmtlSkillTimelineDisableFlowNode), "49b0ea78-3bde-4a0b-8add-3a1dfe7051c3", "片段停用", new Vector2(120f, 460f));
            generation.node51 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph12, typeof(BtsmtlSkillTimelineEnableFlowNode), "adf26532-b12d-40c7-a999-1999f81af71a", "片段启用", new Vector2(120f, 60f));
            generation.node52 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph12, typeof(BtsmtlSkillRootFlowNode), "c3803fe7-214b-4985-89c7-f6592a0b6b4e", "技能入口", new Vector2(120f, 260f));
            generation.node60 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph13, typeof(BtsmtlSkillTimelineDestroyFlowNode), "09bedfc8-90c8-47aa-bc96-4c096b49f316", "片段销毁", new Vector2(120f, 660f));
            generation.node56 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph13, typeof(BtsmtlSkillTimelineEnableFlowNode), "18f4f01d-a121-488d-9ed4-006524b75aa1", "片段启用", new Vector2(120f, 60f));
            generation.node58 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph13, typeof(BtsmtlSkillBlackboardSetFlowNode), "4efb3df8-df1b-4cc0-8c0b-3c202b23e994", "Set Attack2Hit", new Vector2(320f, 0f));
            generation.node59 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph13, typeof(BtsmtlSkillTimelineDisableFlowNode), "820a8125-2853-493e-9742-cb619211dafa", "片段停用", new Vector2(120f, 460f));
            generation.node57 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph13, typeof(BtsmtlSkillRootFlowNode), "ffd3564c-f03e-4ad1-9b9d-35145b213ed9", "技能入口", new Vector2(120f, 260f));
            generation.node64 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph14, typeof(BtsmtlSkillTimelineDisableFlowNode), "5b4eb23b-58aa-45a5-9621-ffc0aaacdb6c", "片段停用", new Vector2(120f, 460f));
            generation.node65 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph14, typeof(BtsmtlSkillTimelineDestroyFlowNode), "6915eefa-9d42-450d-9eee-d73a847eb47c", "片段销毁", new Vector2(120f, 660f));
            generation.node63 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph14, typeof(BtsmtlSkillBlackboardSetFlowNode), "7cc12057-a4f5-4516-879c-fab602d9a377", "Set ComboAccept", new Vector2(320f, 0f));
            generation.node62 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph14, typeof(BtsmtlSkillRootFlowNode), "81317361-7dcb-46e0-a464-faf7b9344409", "技能入口", new Vector2(120f, 260f));
            generation.node61 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph14, typeof(BtsmtlSkillTimelineEnableFlowNode), "93879655-33c1-42d2-8fec-8a299e1fe4d4", "片段启用", new Vector2(120f, 60f));
            generation.node67 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph15, typeof(BtsmtlSkillRootFlowNode), "2dbbadbb-d768-4c44-82d7-857a1ad0e296", "技能入口", new Vector2(120f, 260f));
            generation.node69 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph15, typeof(BtsmtlSkillTimelineDisableFlowNode), "330817cf-25f5-4d60-885f-fb93d9acc709", "片段停用", new Vector2(120f, 460f));
            generation.node68 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph15, typeof(BtsmtlSkillBlackboardSetFlowNode), "8c0ec107-02fa-430b-b316-80bb649d7df4", "Set RecoveryLate", new Vector2(320f, 0f));
            generation.node66 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph15, typeof(BtsmtlSkillTimelineEnableFlowNode), "9e415cce-14f7-45d1-b1ca-23dcd618cebe", "片段启用", new Vector2(120f, 60f));
            generation.node70 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph15, typeof(BtsmtlSkillTimelineDestroyFlowNode), "c46e9fae-fbb8-4e26-8755-09b445855d2f", "片段销毁", new Vector2(120f, 660f));
        }

        static void BuildConfigureStages_Attack25(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph11, "45097cf16c2e46d395111550dda1cd18", "RecoveryEarly", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Attack/RecoveryEarly", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryEarly", "Attack2RecoveryEarly", 2004UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph11, "c289023d917b45be835f404f75cf2fe3", "ComboAccept", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Attack/ComboAccept", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "ComboAccept", "Attack2Cancel", 2002UL));
            BtsmtlSkillAuthoringCode.EnsureBlackboardDeclaration(generation.graph11, "f4e893c7da714366a7feb74b85faad47", "RecoveryLate", typeof(Boolean), false, PipelineBlackboardVariableScope.Frame, PipelineBlackboardVariableLifetime.Frame, "Action/Attack/RecoveryLate", null, new PipelineBlackboardFactProjection(PipelineBlackboardFactProjectionKind.ActionWindow, "RecoveryLate", "Attack2MoveCancel", 2003UL));
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node53).Configure(new BtsmtlSkillBlackboardReference("45097cf16c2e46d395111550dda1cd18", "ef6ea798d23cbb42daa656723c5263b2"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node58).Configure(new BtsmtlSkillBlackboardReference("676010964678489285e72c0bf7ec64a2", "00ec42f6d5ede195dcf13e4e27fe7933"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node63).Configure(new BtsmtlSkillBlackboardReference("c289023d917b45be835f404f75cf2fe3", "ef6ea798d23cbb42daa656723c5263b2"), BtsmtlSkillBlackboardValueType.Boolean, null);
            ((BtsmtlSkillBlackboardAccessFlowNode)generation.node68).Configure(new BtsmtlSkillBlackboardReference("f4e893c7da714366a7feb74b85faad47", "ef6ea798d23cbb42daa656723c5263b2"), BtsmtlSkillBlackboardValueType.Boolean, null);
        }

        static void BuildBindStages_Attack27(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)generation.state3, generation.graph11);
        }

        static void BuildConnectStages_Attack25(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge22 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph11, generation.node48, "Output", generation.node49, "Input", "801b9575-d19f-4dac-993b-7ded0b48e427");
            generation.edge23 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph12, generation.node52, "Output", generation.node53, "Input", "9f1a253a-f128-4904-a02f-2642c184e9da");
            generation.edge24 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph13, generation.node57, "Output", generation.node58, "Input", "d061fe7a-5fc3-4624-934b-0544302ed348");
            generation.edge25 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph14, generation.node62, "Output", generation.node63, "Input", "5ccb6ca2-713b-4f8b-9ac0-f2d9bb82c0e4");
            generation.edge26 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph15, generation.node67, "Output", generation.node68, "Input", "7804cd0b-5b3d-4b2b-b633-41b0a1dbc6f1");
        }

        static void BuildRootBindingStages_Attack26(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph11, new[] { "8ea76387-27ce-4ab7-a036-47fbcddb9c4c", "db13fa44-b0d4-4e90-b40a-92b1e6226d0d", "01780c25-3f89-4117-9ec0-24dd9c32db70", "12f26908-7735-4a0d-8fc1-9a8b4bc08c57" }, new[] { "801b9575-d19f-4dac-993b-7ded0b48e427" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph12, new[] { "adf26532-b12d-40c7-a999-1999f81af71a", "c3803fe7-214b-4985-89c7-f6592a0b6b4e", "3e573873-16f9-4a38-85b3-aafdaf8f6e82", "49b0ea78-3bde-4a0b-8add-3a1dfe7051c3", "04197c9c-ea02-414c-8d5e-1ca9e9e8c90b" }, new[] { "9f1a253a-f128-4904-a02f-2642c184e9da" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph13, new[] { "18f4f01d-a121-488d-9ed4-006524b75aa1", "ffd3564c-f03e-4ad1-9b9d-35145b213ed9", "4efb3df8-df1b-4cc0-8c0b-3c202b23e994", "820a8125-2853-493e-9742-cb619211dafa", "09bedfc8-90c8-47aa-bc96-4c096b49f316" }, new[] { "d061fe7a-5fc3-4624-934b-0544302ed348" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph14, new[] { "93879655-33c1-42d2-8fec-8a299e1fe4d4", "81317361-7dcb-46e0-a464-faf7b9344409", "7cc12057-a4f5-4516-879c-fab602d9a377", "5b4eb23b-58aa-45a5-9621-ffc0aaacdb6c", "6915eefa-9d42-450d-9eee-d73a847eb47c" }, new[] { "5ccb6ca2-713b-4f8b-9ac0-f2d9bb82c0e4" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph15, new[] { "9e415cce-14f7-45d1-b1ca-23dcd618cebe", "2dbbadbb-d768-4c44-82d7-857a1ad0e296", "8c0ec107-02fa-430b-b316-80bb649d7df4", "330817cf-25f5-4d60-885f-fb93d9acc709", "c46e9fae-fbb8-4e26-8755-09b445855d2f" }, new[] { "7804cd0b-5b3d-4b2b-b633-41b0a1dbc6f1" });
        }

        static void BuildRootBindingStages_Attack225(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph11, new[] { "45097cf16c2e46d395111550dda1cd18", "c289023d917b45be835f404f75cf2fe3", "f4e893c7da714366a7feb74b85faad47" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph12, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph13, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph14, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph15, Array.Empty<string>());
        }
    }
}
