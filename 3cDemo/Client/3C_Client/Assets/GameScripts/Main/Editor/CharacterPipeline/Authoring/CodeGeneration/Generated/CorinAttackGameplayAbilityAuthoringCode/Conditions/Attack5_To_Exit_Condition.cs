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
        static void BuildCreateConditions_Attack5_To_Exit_Condition18(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph42 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "578514b8212731449843673c435635fd", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To Exit Condition");
            generation.graph43 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "9c3889ef21ffe74ea03e03a8c95d1f1e", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To Exit Condition");
            generation.graph44 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "9293934dc033be2e9f406fb511cfddc2", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To Exit Condition");
        }

        static void BuildCreateConditions_Attack5_To_Exit_Condition37(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node202 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph42, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "4abc6e48-d805-4b21-bc75-63de209c1331", "AND", new Vector2(40f, 35f));
            generation.node199 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph42, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "8b225da2-3186-4a89-b8c0-52f04672ee2d", "AND", new Vector2(220f, 70f));
            generation.node198 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph42, typeof(BtsmtlSkillCanActivateActionFlowNode), "924a96f3-9be9-469b-b6eb-b9f4c8233967", "Can Activate Dodge", new Vector2(-360f, 200f));
            generation.node201 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph42, typeof(BtsmtlSkillActionWindowActiveFlowNode), "9cc1864f-a1cb-4fec-8b44-bd9e08927c8d", "Window RecoveryEarly", new Vector2(-360f, 100f));
            generation.node203 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph42, typeof(BtsmtlSkillActionRequestFlowNode), "cae0c3b1-d201-49d0-88b4-5faddaf059b4", "Has Dodge Request", new Vector2(-360f, 0f));
            generation.node200 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph42, typeof(BtsmtlSkillConditionResultFlowNode), "f5943698-e1a0-434a-9ae4-06fe8ae03530", "条件结果", new Vector2(600f, 180f));
            generation.node205 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph43, typeof(BtsmtlSkillConditionResultFlowNode), "b34b38c9-c654-47b8-8556-d295201d0e29", "条件结果", new Vector2(600f, 180f));
            generation.node204 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph43, typeof(BtsmtlSkillStateRootCompletedFlowNode), "c66123e9-2ff9-453f-8a8a-7f05d3788926", "状态主体已完成", new Vector2(-360f, 0f));
            generation.node210 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph44, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "0aa95855-7548-4c10-b18d-fe47a97f0902", ">", new Vector2(-240f, 20f));
            generation.node207 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph44, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "2494ffd1-274d-4172-b511-fb17904bae0c", "AND", new Vector2(40f, 35f));
            generation.node206 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph44, typeof(BtsmtlSkillActionWindowActiveFlowNode), "4226ba2e-1e41-4c26-8ed0-a15fa7c78115", "Window RecoveryLate", new Vector2(-360f, 100f));
            generation.node208 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph44, typeof(BtsmtlSkillConditionResultFlowNode), "80c40d52-a358-4baa-b990-8f4961743d36", "条件结果", new Vector2(600f, 180f));
            generation.node209 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph44, typeof(BtsmtlSkillBlackboardScalarFlowNode), "b6cfc921-c2f8-4f9b-bd12-423cf1e954af", "StopThreshold", new Vector2(-520f, 45f));
            generation.node211 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph44, typeof(BtsmtlSkillInputMagnitudeFlowNode), "c8a6ff82-347d-4a84-993e-5c64a52f5409", "MoveAxis Magnitude", new Vector2(-520f, 0f));
        }

        static void BuildConfigureConditions_Attack5_To_Exit_Condition16(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillCanActivateActionFlowNode)generation.node198).Configure(null, "", "");
            ((BtsmtlSkillActionWindowActiveFlowNode)generation.node201).SetWindowType("RecoveryEarly");
            ((IBtsmtlSkillInputNode)generation.node203).SetInputId("Dodge", "asset:be650df85b1e49ab9d1cefc91c6cc809");
            ((BtsmtlSkillActionWindowActiveFlowNode)generation.node206).SetWindowType("RecoveryLate");
            ((BtsmtlSkillBlackboardScalarFlowNode)generation.node209).SetVariable(new BtsmtlSkillBlackboardReference("1edc27e65f454837b415895f4b808048", "00ec42f6d5ede195dcf13e4e27fe7933"));
            ((IBtsmtlSkillInputNode)generation.node211).SetInputId("MoveAxis", "asset:be650df85b1e49ab9d1cefc91c6cc809");
        }

        static void BuildConnectConditions_Attack5_To_Exit_Condition17(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge89 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph42, generation.node198, "m_Output", generation.node199, "b", "17244c10-2ba2-4c18-ad68-076468427288");
            generation.edge90 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph42, generation.node199, "Value", generation.node200, "m_Result", "de5cb30a-ec1a-461f-8e0d-8aaf8ecf9514");
            generation.edge91 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph42, generation.node201, "m_Output", generation.node202, "b", "07495c4f-6c6d-44ef-8062-abfd4d74b5a1");
            generation.edge92 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph42, generation.node202, "Value", generation.node199, "a", "2e48b774-5ab0-4cd1-8633-1920e2ac012a");
            generation.edge93 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph42, generation.node203, "m_Output", generation.node202, "a", "6194e31b-fa2f-4ab4-9e27-69cbfbd124d0");
            generation.edge94 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph43, generation.node204, "m_Output", generation.node205, "m_Result", "d4923ad8-fb95-4a2b-9e78-b3ad6a2913b4");
            generation.edge95 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph44, generation.node206, "m_Output", generation.node207, "b", "734548a5-edee-48d2-a78b-43c116091164");
            generation.edge96 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph44, generation.node207, "Value", generation.node208, "m_Result", "f5fc9123-4b54-4f98-9573-f4e2d21e917b");
            generation.edge97 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph44, generation.node209, "m_Output", generation.node210, "b", "a19ecf45-7ca5-4d7d-b423-559b2cd2ebfa");
            generation.edge98 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph44, generation.node210, "Value", generation.node207, "a", "7701b272-ec3e-402c-ada8-fe2845eed0e3");
            generation.edge99 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph44, generation.node211, "m_Output", generation.node210, "a", "638878a5-4b43-4937-8d4e-88490bf7ef4e");
        }

        static void BuildRootBindingConditions_Attack5_To_Exit_Condition18(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph42, new[] { "924a96f3-9be9-469b-b6eb-b9f4c8233967", "8b225da2-3186-4a89-b8c0-52f04672ee2d", "f5943698-e1a0-434a-9ae4-06fe8ae03530", "9cc1864f-a1cb-4fec-8b44-bd9e08927c8d", "4abc6e48-d805-4b21-bc75-63de209c1331", "cae0c3b1-d201-49d0-88b4-5faddaf059b4" }, new[] { "17244c10-2ba2-4c18-ad68-076468427288", "de5cb30a-ec1a-461f-8e0d-8aaf8ecf9514", "07495c4f-6c6d-44ef-8062-abfd4d74b5a1", "2e48b774-5ab0-4cd1-8633-1920e2ac012a", "6194e31b-fa2f-4ab4-9e27-69cbfbd124d0" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph43, new[] { "c66123e9-2ff9-453f-8a8a-7f05d3788926", "b34b38c9-c654-47b8-8556-d295201d0e29" }, new[] { "d4923ad8-fb95-4a2b-9e78-b3ad6a2913b4" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph44, new[] { "4226ba2e-1e41-4c26-8ed0-a15fa7c78115", "2494ffd1-274d-4172-b511-fb17904bae0c", "80c40d52-a358-4baa-b990-8f4961743d36", "b6cfc921-c2f8-4f9b-bd12-423cf1e954af", "0aa95855-7548-4c10-b18d-fe47a97f0902", "c8a6ff82-347d-4a84-993e-5c64a52f5409" }, new[] { "734548a5-edee-48d2-a78b-43c116091164", "f5fc9123-4b54-4f98-9573-f4e2d21e917b", "a19ecf45-7ca5-4d7d-b423-559b2cd2ebfa", "7701b272-ec3e-402c-ada8-fe2845eed0e3", "638878a5-4b43-4937-8d4e-88490bf7ef4e" });
        }

        static void BuildRootBindingConditions_Attack5_To_Exit_Condition37(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph42, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph43, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph44, Array.Empty<string>());
        }
    }
}
