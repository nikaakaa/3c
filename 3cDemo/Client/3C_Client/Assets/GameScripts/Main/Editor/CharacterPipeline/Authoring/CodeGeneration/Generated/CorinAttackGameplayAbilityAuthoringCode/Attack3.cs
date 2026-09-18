using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode
    {
        static Attack3Parts BuildAttack3(AttackParts attack, RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack3Parts();
            parts.graph16 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "1bf78cba3df9605848cdacbff01046f6", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack2 To Attack3 Condition");
            parts.graph21 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph20, "8a14fcb8765abbc619bfd21d1a558e49", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryEarly");
            parts.graph22 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph20, "c1fda7608ebd58824cf60b93566ccd3a", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryLate");
            parts.graph23 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph20, "bed856871c2e9c2d2e326da4a80aba66", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack3Hit");
            parts.graph24 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph20, "d6af200a24afd284f06690675a70f03e", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision ComboAccept");
            parts.graph25 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "2dddeb8cba87a66073266017f18cd6fb", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack3 To Exit Condition");
            parts.graph26 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "693f7315434b7feac2e8a99ca03bb49c", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack3 To Exit Condition");
            parts.graph28 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "94f949e8ec8af7786658e591e921bd42", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack3 To Exit Condition");
            var node75 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph16, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "12bb0dae-0864-462a-a9cd-7c2dfbe7755d", "AND", new Vector2(220f, 70f));
            var node74 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph16, typeof(BtsmtlSkillCanActivateActionFlowNode), "5f279952-a577-450e-ba55-489986edf6ba", "Can Activate Attack", new Vector2(-360f, 200f));
            var node77 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph16, typeof(BtsmtlSkillActionRequestFlowNode), "afd5ac96-3572-4761-b4e9-73df6d4e5c5a", "Has Attack Request", new Vector2(-360f, 0f));
            var node78 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph16, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "d244aa12-6fb0-48b4-88e7-35da5b15c7e9", "AND", new Vector2(40f, 35f));
            var node79 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph16, typeof(BtsmtlSkillActionWindowActiveFlowNode), "e4a10606-4019-482e-9691-5013a43a1778", "Window ComboAccept", new Vector2(-360f, 100f));
            var node76 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph16, typeof(BtsmtlSkillConditionResultFlowNode), "f827b6b4-9110-4e9a-99d1-d9d7c496dac9", "条件结果", new Vector2(600f, 180f));
            var node100 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph21, typeof(BtsmtlSkillBlackboardSetFlowNode), "0710cf99-ac01-4c66-859b-f94e59c8ec63", "Set RecoveryEarly", new Vector2(320f, 0f));
            var node102 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph21, typeof(BtsmtlSkillTimelineDestroyFlowNode), "1d5fb466-0f59-4427-8a5e-9e0fca15f283", "片段销毁", new Vector2(120f, 660f));
            var node101 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph21, typeof(BtsmtlSkillTimelineDisableFlowNode), "6a2bdc3d-5dfe-4f4c-ab71-f6f6bacfe0df", "片段停用", new Vector2(120f, 460f));
            var node98 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph21, typeof(BtsmtlSkillTimelineEnableFlowNode), "ae7064ec-66a6-42c0-948f-d35656c8917f", "片段启用", new Vector2(120f, 60f));
            var node99 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph21, typeof(BtsmtlSkillRootFlowNode), "efd8fc6c-f974-487e-8805-4e93ccb268b7", "技能入口", new Vector2(120f, 260f));
            var node105 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph22, typeof(BtsmtlSkillBlackboardSetFlowNode), "43cdde6d-ea89-4810-b94d-53917023c7ed", "Set RecoveryLate", new Vector2(320f, 0f));
            var node103 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph22, typeof(BtsmtlSkillTimelineEnableFlowNode), "8f734e26-9719-4520-bcfb-eaab4e162ba4", "片段启用", new Vector2(120f, 60f));
            var node104 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph22, typeof(BtsmtlSkillRootFlowNode), "ade25018-4120-4d07-86a0-550b4fa6301d", "技能入口", new Vector2(120f, 260f));
            var node107 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph22, typeof(BtsmtlSkillTimelineDestroyFlowNode), "b4152a99-84f5-4559-ad2b-c93dbf433acf", "片段销毁", new Vector2(120f, 660f));
            var node106 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph22, typeof(BtsmtlSkillTimelineDisableFlowNode), "d12bba92-ea64-46eb-a734-7874fb65f571", "片段停用", new Vector2(120f, 460f));
            var node109 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph23, typeof(BtsmtlSkillRootFlowNode), "00b8c14c-6ed8-42d9-b583-de2c8b7e3af1", "技能入口", new Vector2(120f, 260f));
            var node111 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph23, typeof(BtsmtlSkillTimelineDisableFlowNode), "1189562e-2142-426b-8587-fea255d6186a", "片段停用", new Vector2(120f, 460f));
            var node110 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph23, typeof(BtsmtlSkillBlackboardSetFlowNode), "3f4289fd-d8a3-4965-afa0-c751f12277ca", "Set Attack2Hit", new Vector2(320f, 0f));
            var node108 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph23, typeof(BtsmtlSkillTimelineEnableFlowNode), "57fdd827-1fc7-421d-8c26-d5f8c75ce52b", "片段启用", new Vector2(120f, 60f));
            var node112 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph23, typeof(BtsmtlSkillTimelineDestroyFlowNode), "8378c59f-092a-497e-8ab2-a876ad741819", "片段销毁", new Vector2(120f, 660f));
            var node116 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph24, typeof(BtsmtlSkillTimelineDisableFlowNode), "03407a2d-89d5-412d-b170-4e0621d7bf00", "片段停用", new Vector2(120f, 460f));
            var node117 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph24, typeof(BtsmtlSkillTimelineDestroyFlowNode), "06364eed-3aee-409a-b1e0-aa1422235270", "片段销毁", new Vector2(120f, 660f));
            var node113 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph24, typeof(BtsmtlSkillTimelineEnableFlowNode), "1e0cab18-9869-4abf-b659-905c504e89da", "片段启用", new Vector2(120f, 60f));
            var node115 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph24, typeof(BtsmtlSkillBlackboardSetFlowNode), "596b7aab-3f96-4f8e-8f25-66fefb1c6985", "Set ComboAccept", new Vector2(320f, 0f));
            var node114 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph24, typeof(BtsmtlSkillRootFlowNode), "75ba637e-76e0-41e1-b70f-ad81ecd4a3fe", "技能入口", new Vector2(120f, 260f));
            var node120 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph25, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "09f20d2e-1bcb-4c35-ad7f-1d3ed340cb2f", "AND", new Vector2(40f, 35f));
            var node118 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph25, typeof(BtsmtlSkillBlackboardScalarFlowNode), "632b5311-a285-42f8-9031-f989992c9457", "StopThreshold", new Vector2(-520f, 45f));
            var node122 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph25, typeof(BtsmtlSkillInputMagnitudeFlowNode), "7a2a0909-bc44-4121-b74d-f97ac2751532", "MoveAxis Magnitude", new Vector2(-520f, 0f));
            var node121 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph25, typeof(BtsmtlSkillConditionResultFlowNode), "7ee21a20-3bba-4c00-9b61-581dfa7329d6", "条件结果", new Vector2(600f, 180f));
            var node123 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph25, typeof(BtsmtlSkillActionWindowActiveFlowNode), "8aee43d9-549c-4c8a-b3f7-5713d4391835", "Window RecoveryLate", new Vector2(-360f, 100f));
            var node119 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph25, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "d3d24db4-3787-4570-9e46-d58b1c9f1ace", ">", new Vector2(-240f, 20f));
            var node124 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph26, typeof(BtsmtlSkillCanActivateActionFlowNode), "50df135c-2c6a-4a2b-ba42-16d25682651c", "Can Activate Dodge", new Vector2(-360f, 200f));
            var node127 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph26, typeof(BtsmtlSkillActionWindowActiveFlowNode), "90294fa9-e9b4-4d15-bbae-80211f1eb19a", "Window RecoveryEarly", new Vector2(-360f, 100f));
            var node125 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph26, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "9a0586d3-4d80-4840-a393-817f27e9fdad", "AND", new Vector2(220f, 70f));
            var node129 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph26, typeof(BtsmtlSkillActionRequestFlowNode), "9bc74e3d-1550-46f5-9e0c-64a298a5e713", "Has Dodge Request", new Vector2(-360f, 0f));
            var node128 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph26, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "d1ac3489-a406-40c2-af50-f7e3893d14c6", "AND", new Vector2(40f, 35f));
            var node126 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph26, typeof(BtsmtlSkillConditionResultFlowNode), "dfb7ccaa-985d-4db7-8efa-d00bb4905f76", "条件结果", new Vector2(600f, 180f));
            var node136 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph28, typeof(BtsmtlSkillStateRootCompletedFlowNode), "444f1a0c-bed1-4726-8a97-566d086abee0", "状态主体已完成", new Vector2(-360f, 0f));
            var node137 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph28, typeof(BtsmtlSkillConditionResultFlowNode), "708bd73e-c165-4313-ba02-d225268a6cb1", "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringContract.Apply(node74, new[] { new BtsmtlSkillAuthoringFieldValue("admissionProfile", rootParts.asset20), new BtsmtlSkillAuthoringFieldValue("targetSnapshot", new BtsmtlSkillTargetSnapshotReference("b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933")) });
            BtsmtlSkillAuthoringContract.Apply(node77, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "Attack"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node79, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "ComboAccept") });
            BtsmtlSkillAuthoringContract.Apply(node100, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "ed22b7318b054a84a89a769fc8ec9fef"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "ab4f75a3dbba67da55dbf4a46872eadd"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node105, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "a586c674815f46359af6a0ff35156394"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "ab4f75a3dbba67da55dbf4a46872eadd"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node110, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "361015344dc440ee81193dd42bca2251"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node115, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "6315ad83888944e0bbad7595097b60f9"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "ab4f75a3dbba67da55dbf4a46872eadd"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node118, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "1edc27e65f454837b415895f4b808048"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(node122, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "MoveAxis"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node123, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RecoveryLate") });
            BtsmtlSkillAuthoringContract.Apply(node124, new[] { new BtsmtlSkillAuthoringFieldValue("admissionProfile", rootParts.asset41), new BtsmtlSkillAuthoringFieldValue("targetSnapshot", new BtsmtlSkillTargetSnapshotReference("b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933")) });
            BtsmtlSkillAuthoringContract.Apply(node127, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RecoveryEarly") });
            BtsmtlSkillAuthoringContract.Apply(node129, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "Dodge"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            var edge29 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph16, node74, "m_Output", node75, "b", "191d8aa7-6bf9-494b-a4e3-a4e190a8e668");
            var edge30 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph16, node75, "Value", node76, "m_Result", "a5773886-2ad6-4d78-bdb1-8e444e2ebe48");
            var edge31 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph16, node77, "m_Output", node78, "a", "7ff43a2e-1512-4e8d-a03f-7048611cd383");
            var edge32 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph16, node78, "Value", node75, "a", "9a23f8f5-4295-43b0-9bc9-d1f2e55e833b");
            var edge33 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph16, node79, "m_Output", node78, "b", "0f754649-8416-4201-98dd-38cb8da63705");
            var edge46 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph21, node99, "Output", node100, "Input", "fbdf54c3-2c3d-427c-a1fa-398e34d773ec");
            var edge47 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph22, node104, "Output", node105, "Input", "83b924a0-1741-4854-b2e3-476ca8eed014");
            var edge48 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph23, node109, "Output", node110, "Input", "343b44e4-ff07-4496-bf13-85328f1aca0c");
            var edge49 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph24, node114, "Output", node115, "Input", "03f826d5-8a6d-4aa0-8e56-1d5c915385da");
            var edge50 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph25, node118, "m_Output", node119, "b", "868040c2-156b-4f12-8069-98b85d8dbdba");
            var edge51 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph25, node119, "Value", node120, "a", "708879dd-df80-4e7d-8c57-c3d7c95a58af");
            var edge52 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph25, node120, "Value", node121, "m_Result", "7fb9685d-3f5b-4202-a968-8060221a3ed7");
            var edge53 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph25, node122, "m_Output", node119, "a", "4fbd628a-6b59-4e5f-9631-89bff90ffdce");
            var edge54 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph25, node123, "m_Output", node120, "b", "a8d9e857-84a0-4417-b731-849fb95a570f");
            var edge55 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph26, node124, "m_Output", node125, "b", "78e49128-df91-4d2c-85fe-297701eb4a71");
            var edge56 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph26, node125, "Value", node126, "m_Result", "0f1c1971-4526-4249-9bf9-5622d6046f41");
            var edge57 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph26, node127, "m_Output", node128, "b", "00d9120e-ccd3-43b9-8899-35a8fcaee625");
            var edge58 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph26, node128, "Value", node125, "a", "e7c0b095-003c-49b0-b317-67ced4c2ffb8");
            var edge59 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph26, node129, "m_Output", node128, "a", "8f289fc6-012b-42ca-874e-c415ab995637");
            var edge65 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph28, node136, "m_Output", node137, "m_Result", "d5045f4a-2b3f-4d94-b24f-114138970155");
            return parts;
        }

        sealed class Attack3Parts
        {
            internal BtsmtlSkillFlowGraph graph16;
            internal BtsmtlSkillFlowGraph graph21;
            internal BtsmtlSkillFlowGraph graph22;
            internal BtsmtlSkillFlowGraph graph23;
            internal BtsmtlSkillFlowGraph graph24;
            internal BtsmtlSkillFlowGraph graph25;
            internal BtsmtlSkillFlowGraph graph26;
            internal BtsmtlSkillFlowGraph graph28;
        }
    }
}
