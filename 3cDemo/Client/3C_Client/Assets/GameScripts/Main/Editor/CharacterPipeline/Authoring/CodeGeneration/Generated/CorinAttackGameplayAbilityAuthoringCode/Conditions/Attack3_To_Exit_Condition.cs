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
        static void BuildCreateConditions_Attack3_To_Exit_Condition10(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph25 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "2dddeb8cba87a66073266017f18cd6fb", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack3 To Exit Condition");
            generation.graph26 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "693f7315434b7feac2e8a99ca03bb49c", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack3 To Exit Condition");
        }

        static void BuildCreateConditions_Attack3_To_Exit_Condition12(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.graph28 = BtsmtlSkillAuthoringCode.EnsureChildGraph(generation.graph, "94f949e8ec8af7786658e591e921bd42", BtsmtlSkillFlowGraphRole.ConditionRule, "Attack3 To Exit Condition");
        }

        static void BuildCreateConditions_Attack3_To_Exit_Condition29(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node117 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph25, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "09f20d2e-1bcb-4c35-ad7f-1d3ed340cb2f", "AND", new Vector2(40f, 35f));
            generation.node115 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph25, typeof(BtsmtlSkillBlackboardScalarFlowNode), "632b5311-a285-42f8-9031-f989992c9457", "StopThreshold", new Vector2(-520f, 45f));
            generation.node119 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph25, typeof(BtsmtlSkillInputMagnitudeFlowNode), "7a2a0909-bc44-4121-b74d-f97ac2751532", "MoveAxis Magnitude", new Vector2(-520f, 0f));
            generation.node118 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph25, typeof(BtsmtlSkillConditionResultFlowNode), "7ee21a20-3bba-4c00-9b61-581dfa7329d6", "条件结果", new Vector2(600f, 180f));
            generation.node120 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph25, typeof(BtsmtlSkillActionWindowActiveFlowNode), "8aee43d9-549c-4c8a-b3f7-5713d4391835", "Window RecoveryLate", new Vector2(-360f, 100f));
            generation.node116 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph25, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "d3d24db4-3787-4570-9e46-d58b1c9f1ace", ">", new Vector2(-240f, 20f));
            generation.node121 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph26, typeof(BtsmtlSkillCanActivateActionFlowNode), "50df135c-2c6a-4a2b-ba42-16d25682651c", "Can Activate Dodge", new Vector2(-360f, 200f));
            generation.node124 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph26, typeof(BtsmtlSkillActionWindowActiveFlowNode), "90294fa9-e9b4-4d15-bbae-80211f1eb19a", "Window RecoveryEarly", new Vector2(-360f, 100f));
            generation.node122 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph26, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "9a0586d3-4d80-4840-a393-817f27e9fdad", "AND", new Vector2(220f, 70f));
            generation.node126 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph26, typeof(BtsmtlSkillActionRequestFlowNode), "9bc74e3d-1550-46f5-9e0c-64a298a5e713", "Has Dodge Request", new Vector2(-360f, 0f));
            generation.node125 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph26, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "d1ac3489-a406-40c2-af50-f7e3893d14c6", "AND", new Vector2(40f, 35f));
            generation.node123 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph26, typeof(BtsmtlSkillConditionResultFlowNode), "dfb7ccaa-985d-4db7-8efa-d00bb4905f76", "条件结果", new Vector2(600f, 180f));
        }

        static void BuildCreateConditions_Attack3_To_Exit_Condition31(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.node133 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph28, typeof(BtsmtlSkillStateRootCompletedFlowNode), "444f1a0c-bed1-4726-8a97-566d086abee0", "状态主体已完成", new Vector2(-360f, 0f));
            generation.node134 = BtsmtlSkillAuthoringCode.EnsureFlowNode(generation.graph28, typeof(BtsmtlSkillConditionResultFlowNode), "708bd73e-c165-4313-ba02-d225268a6cb1", "条件结果", new Vector2(600f, 180f));
        }

        static void BuildConfigureConditions_Attack3_To_Exit_Condition9(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            ((BtsmtlSkillBlackboardScalarFlowNode)generation.node115).SetVariable(new BtsmtlSkillBlackboardReference("1edc27e65f454837b415895f4b808048", "00ec42f6d5ede195dcf13e4e27fe7933"));
            ((IBtsmtlSkillInputNode)generation.node119).SetInputId("MoveAxis", "asset:be650df85b1e49ab9d1cefc91c6cc809");
            ((BtsmtlSkillActionWindowActiveFlowNode)generation.node120).SetWindowType("RecoveryLate");
            ((BtsmtlSkillCanActivateActionFlowNode)generation.node121).Configure(null, "", "");
            ((BtsmtlSkillActionWindowActiveFlowNode)generation.node124).SetWindowType("RecoveryEarly");
            ((IBtsmtlSkillInputNode)generation.node126).SetInputId("Dodge", "asset:be650df85b1e49ab9d1cefc91c6cc809");
        }

        static void BuildConnectConditions_Attack3_To_Exit_Condition9(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge48 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph25, generation.node115, "m_Output", generation.node116, "b", "868040c2-156b-4f12-8069-98b85d8dbdba");
            generation.edge49 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph25, generation.node116, "Value", generation.node117, "a", "708879dd-df80-4e7d-8c57-c3d7c95a58af");
            generation.edge50 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph25, generation.node117, "Value", generation.node118, "m_Result", "7fb9685d-3f5b-4202-a968-8060221a3ed7");
            generation.edge51 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph25, generation.node119, "m_Output", generation.node116, "a", "4fbd628a-6b59-4e5f-9631-89bff90ffdce");
            generation.edge52 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph25, generation.node120, "m_Output", generation.node117, "b", "a8d9e857-84a0-4417-b731-849fb95a570f");
            generation.edge53 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph26, generation.node121, "m_Output", generation.node122, "b", "78e49128-df91-4d2c-85fe-297701eb4a71");
            generation.edge54 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph26, generation.node122, "Value", generation.node123, "m_Result", "0f1c1971-4526-4249-9bf9-5622d6046f41");
            generation.edge55 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph26, generation.node124, "m_Output", generation.node125, "b", "00d9120e-ccd3-43b9-8899-35a8fcaee625");
            generation.edge56 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph26, generation.node125, "Value", generation.node122, "a", "e7c0b095-003c-49b0-b317-67ced4c2ffb8");
            generation.edge57 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph26, generation.node126, "m_Output", generation.node125, "a", "8f289fc6-012b-42ca-874e-c415ab995637");
        }

        static void BuildConnectConditions_Attack3_To_Exit_Condition11(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.edge63 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(generation.graph28, generation.node133, "m_Output", generation.node134, "m_Result", "d5045f4a-2b3f-4d94-b24f-114138970155");
        }

        static void BuildRootBindingConditions_Attack3_To_Exit_Condition10(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph25, new[] { "632b5311-a285-42f8-9031-f989992c9457", "d3d24db4-3787-4570-9e46-d58b1c9f1ace", "09f20d2e-1bcb-4c35-ad7f-1d3ed340cb2f", "7ee21a20-3bba-4c00-9b61-581dfa7329d6", "7a2a0909-bc44-4121-b74d-f97ac2751532", "8aee43d9-549c-4c8a-b3f7-5713d4391835" }, new[] { "868040c2-156b-4f12-8069-98b85d8dbdba", "708879dd-df80-4e7d-8c57-c3d7c95a58af", "7fb9685d-3f5b-4202-a968-8060221a3ed7", "4fbd628a-6b59-4e5f-9631-89bff90ffdce", "a8d9e857-84a0-4417-b731-849fb95a570f" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph26, new[] { "50df135c-2c6a-4a2b-ba42-16d25682651c", "9a0586d3-4d80-4840-a393-817f27e9fdad", "dfb7ccaa-985d-4db7-8efa-d00bb4905f76", "90294fa9-e9b4-4d15-bbae-80211f1eb19a", "d1ac3489-a406-40c2-af50-f7e3893d14c6", "9bc74e3d-1550-46f5-9e0c-64a298a5e713" }, new[] { "78e49128-df91-4d2c-85fe-297701eb4a71", "0f1c1971-4526-4249-9bf9-5622d6046f41", "00d9120e-ccd3-43b9-8899-35a8fcaee625", "e7c0b095-003c-49b0-b317-67ced4c2ffb8", "8f289fc6-012b-42ca-874e-c415ab995637" });
        }

        static void BuildRootBindingConditions_Attack3_To_Exit_Condition12(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneFlowGraph(generation.graph28, new[] { "444f1a0c-bed1-4726-8a97-566d086abee0", "708bd73e-c165-4313-ba02-d225268a6cb1" }, new[] { "d5045f4a-2b3f-4d94-b24f-114138970155" });
        }

        static void BuildRootBindingConditions_Attack3_To_Exit_Condition29(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph25, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph26, Array.Empty<string>());
        }

        static void BuildRootBindingConditions_Attack3_To_Exit_Condition31(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneBlackboard(generation.graph28, Array.Empty<string>());
        }
    }
}
