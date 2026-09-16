using System;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;
using FlowCanvas.Nodes;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode
    {
        static Attack4Parts BuildAttack4(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack4Parts();
            parts.graph27 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "db9f983b0414a04fb72784e5b2b59838", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack3 To Attack4 Condition");
            parts.graph34 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "4185a6457a17f3050b7d0b4dd893f48a", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack4 To Exit Condition");
            parts.graph36 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "7e767bd1bb7375a348c04eb31a0fa6ee", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack4 To Exit Condition");
            parts.graph37 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "4e24360bcf0b4484b81303d56fa2b957", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack4 To Exit Condition");
            var node128 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph27, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "0537bd26-1cf3-45c2-92c5-1d896d70d4bd", "AND", new Vector2(40f, 35f));
            var node130 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph27, typeof(BtsmtlSkillConditionResultFlowNode), "360f4fad-44ba-4cb3-bbe8-7728a781f890", "条件结果", new Vector2(600f, 180f));
            var node127 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph27, typeof(BtsmtlSkillActionRequestFlowNode), "36138109-865b-4384-995e-56076a4c7410", "Has Attack Request", new Vector2(-360f, 0f));
            var node131 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph27, typeof(BtsmtlSkillCanActivateActionFlowNode), "5d6fe35f-531a-46b7-a97a-7daf4f9b8097", "Can Activate Attack", new Vector2(-360f, 200f));
            var node129 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph27, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "63866ef1-87b6-4291-9606-ce1230478c83", "AND", new Vector2(220f, 70f));
            var node132 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph27, typeof(BtsmtlSkillActionWindowActiveFlowNode), "cdd15f3c-f01d-4319-94e8-7fb783690ba7", "Window ComboAccept", new Vector2(-360f, 100f));
            var node143 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph30, typeof(BtsmtlSkillTimelineDestroyFlowNode), "180b1f83-5851-4817-a2c5-87123927cb97", "片段销毁", new Vector2(120f, 660f));
            var node141 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph30, typeof(BtsmtlSkillBlackboardSetFlowNode), "2367bbe3-3d42-4a6a-92fc-99dec3f5e314", "Set Attack2Hit", new Vector2(320f, 0f));
            var node142 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph30, typeof(BtsmtlSkillTimelineDisableFlowNode), "2db3e4f7-a02b-40a5-b43f-e89e466bef08", "片段停用", new Vector2(120f, 460f));
            var node140 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph30, typeof(BtsmtlSkillRootFlowNode), "48cdc479-d57e-4da2-89fe-83fbaa37f7fb", "技能入口", new Vector2(120f, 260f));
            var node139 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph30, typeof(BtsmtlSkillTimelineEnableFlowNode), "c36a7c0e-7ad8-4dc1-85ec-ca5820ac75e3", "片段启用", new Vector2(120f, 60f));
            var node145 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph31, typeof(BtsmtlSkillRootFlowNode), "2bd9d21a-6a19-4b5b-becb-7787b68d9e25", "技能入口", new Vector2(120f, 260f));
            var node144 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph31, typeof(BtsmtlSkillTimelineEnableFlowNode), "68d76ee6-8062-4fde-9c8b-a377369614ec", "片段启用", new Vector2(120f, 60f));
            var node147 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph31, typeof(BtsmtlSkillTimelineDisableFlowNode), "a929c5ad-d2b4-401c-95be-e406880b3c8a", "片段停用", new Vector2(120f, 460f));
            var node146 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph31, typeof(BtsmtlSkillBlackboardSetFlowNode), "c54c9d37-f042-409b-b7d3-102af84ddf01", "Set RecoveryLate", new Vector2(320f, 0f));
            var node148 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph31, typeof(BtsmtlSkillTimelineDestroyFlowNode), "c9930916-a8b5-4ac5-bcd4-d34a14a30f37", "片段销毁", new Vector2(120f, 660f));
            var node149 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph32, typeof(BtsmtlSkillTimelineEnableFlowNode), "2be81e47-2da0-49c0-b4a9-c0a8d3db86fd", "片段启用", new Vector2(120f, 60f));
            var node153 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph32, typeof(BtsmtlSkillTimelineDestroyFlowNode), "36a3c728-f042-4427-97eb-f1bdd831974b", "片段销毁", new Vector2(120f, 660f));
            var node151 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph32, typeof(BtsmtlSkillBlackboardSetFlowNode), "91d67a18-db16-41d5-bae2-b6acac59389d", "Set RecoveryEarly", new Vector2(320f, 0f));
            var node152 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph32, typeof(BtsmtlSkillTimelineDisableFlowNode), "af29316e-f707-40b7-aa79-bdc941f736ff", "片段停用", new Vector2(120f, 460f));
            var node150 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph32, typeof(BtsmtlSkillRootFlowNode), "aff306c6-aa1b-4635-b85a-5e56b8d9d9b0", "技能入口", new Vector2(120f, 260f));
            var node156 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph33, typeof(BtsmtlSkillBlackboardSetFlowNode), "468b164f-c02b-4986-a333-834fd1bb8b45", "Set ComboAccept", new Vector2(320f, 0f));
            var node158 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph33, typeof(BtsmtlSkillTimelineDestroyFlowNode), "5fe03763-9315-4fb3-9dc4-b865776a0d42", "片段销毁", new Vector2(120f, 660f));
            var node155 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph33, typeof(BtsmtlSkillRootFlowNode), "6b8224d2-bfbe-483f-a97c-a5a786118293", "技能入口", new Vector2(120f, 260f));
            var node157 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph33, typeof(BtsmtlSkillTimelineDisableFlowNode), "c2200340-5255-4ae8-af56-a5cdda3c3564", "片段停用", new Vector2(120f, 460f));
            var node154 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph33, typeof(BtsmtlSkillTimelineEnableFlowNode), "c6c1c2eb-5dbf-4f06-be7d-64f21c33501c", "片段启用", new Vector2(120f, 60f));
            var node160 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph34, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "0bb4a135-464a-478b-8c65-5a8034c0aa55", "AND", new Vector2(40f, 35f));
            var node159 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph34, typeof(BtsmtlSkillActionWindowActiveFlowNode), "105d943c-b30c-461d-b83a-f3a52d491570", "Window RecoveryLate", new Vector2(-360f, 100f));
            var node162 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph34, typeof(BtsmtlSkillInputMagnitudeFlowNode), "1a2ea1b0-fd91-440c-adf5-5e58e8cfddd8", "MoveAxis Magnitude", new Vector2(-520f, 0f));
            var node164 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph34, typeof(BtsmtlSkillBlackboardScalarFlowNode), "393134fd-cd4d-4255-8cfc-ed6dfe49ec83", "StopThreshold", new Vector2(-520f, 45f));
            var node161 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph34, typeof(BtsmtlSkillConditionResultFlowNode), "439638e8-eed2-4a24-a297-7ae7c1237bd3", "条件结果", new Vector2(600f, 180f));
            var node163 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph34, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "ddabdfc2-6be2-48b8-a9c7-4eb093abd4f7", ">", new Vector2(-240f, 20f));
            var node171 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph36, typeof(BtsmtlSkillStateRootCompletedFlowNode), "3b9d9658-3de3-44f7-9a89-b5c845bb30e3", "状态主体已完成", new Vector2(-360f, 0f));
            var node172 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph36, typeof(BtsmtlSkillConditionResultFlowNode), "ef9cf994-bec4-46f8-9424-b20709fa5b3b", "条件结果", new Vector2(600f, 180f));
            var node174 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph37, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "47b79351-c34f-48dc-849b-754cd986d262", "AND", new Vector2(220f, 70f));
            var node173 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph37, typeof(BtsmtlSkillCanActivateActionFlowNode), "48028d59-7529-4abb-afa9-010584cd1e9b", "Can Activate Dodge", new Vector2(-360f, 200f));
            var node176 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph37, typeof(BtsmtlSkillActionWindowActiveFlowNode), "4ed50c22-f68b-4d9a-ba57-6b325c91ffac", "Window RecoveryEarly", new Vector2(-360f, 100f));
            var node177 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph37, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "84af6e94-af5c-4d7c-9c15-5f2a203d236b", "AND", new Vector2(40f, 35f));
            var node175 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph37, typeof(BtsmtlSkillConditionResultFlowNode), "cee06b3f-d862-4d69-9a7d-3079f0e8322b", "条件结果", new Vector2(600f, 180f));
            var node178 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph37, typeof(BtsmtlSkillActionRequestFlowNode), "d142330f-66ab-46c0-a74d-bf4e5db6cfea", "Has Dodge Request", new Vector2(-360f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node127, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "Attack"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node131, new[] { new BtsmtlSkillAuthoringFieldValue("admissionProfile", context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>("Assets/Configs/Character/Corin/Pipeline/Actions/Attack/CorinAttackActionProfile.asset", 11400000L)), new BtsmtlSkillAuthoringFieldValue("targetSnapshot", new BtsmtlSkillTargetSnapshotReference("b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933")) });
            BtsmtlSkillAuthoringContract.Apply(node132, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "ComboAccept") });
            BtsmtlSkillAuthoringContract.Apply(node141, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "bf611f54e2cb477297162996345d8a34"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node146, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "f2d1e42a70ee492784a4dbe30a1e014f"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "b2fa5183fe82ae10d37b83f3d03c931c"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node151, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "b96e0a2121e14eb68a1fb6aeff18fd37"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "b2fa5183fe82ae10d37b83f3d03c931c"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node156, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "16015282780a4579817a0de9a10ebe4f"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "b2fa5183fe82ae10d37b83f3d03c931c"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node159, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RecoveryLate") });
            BtsmtlSkillAuthoringContract.Apply(node162, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "MoveAxis"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node164, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "1edc27e65f454837b415895f4b808048"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(node173, new[] { new BtsmtlSkillAuthoringFieldValue("admissionProfile", context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>("Assets/Configs/Character/Corin/Pipeline/Actions/Dodge/CorinDodgeActionProfile.asset", 11400000L)), new BtsmtlSkillAuthoringFieldValue("targetSnapshot", new BtsmtlSkillTargetSnapshotReference("b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933")) });
            BtsmtlSkillAuthoringContract.Apply(node176, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RecoveryEarly") });
            BtsmtlSkillAuthoringContract.Apply(node178, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "Dodge"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            var edge58 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph27, node127, "m_Output", node128, "a", "eba41a97-036b-4806-98f5-fd20ac4a7332");
            var edge59 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph27, node128, "Value", node129, "a", "0e2a5f6d-e40f-4d76-9691-1dad5b79504e");
            var edge60 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph27, node129, "Value", node130, "m_Result", "55779d5e-fb1d-4291-8f8b-7f62d53014f7");
            var edge61 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph27, node131, "m_Output", node129, "b", "c61cf27b-5662-476d-b88f-04de80775e3a");
            var edge62 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph27, node132, "m_Output", node128, "b", "00e9955c-b35d-4457-b141-c914e94a8b63");
            var edge65 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph30, node140, "Output", node141, "Input", "bbfaa6a1-5523-4e32-8347-2a2beaf3bd9a");
            var edge66 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph31, node145, "Output", node146, "Input", "fbda6ca0-0644-4c38-826c-1bdd2966daa0");
            var edge67 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph32, node150, "Output", node151, "Input", "a631a939-688e-40f8-9d17-b0b1c21bf58f");
            var edge68 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph33, node155, "Output", node156, "Input", "457a5a36-3e6b-4c9b-bd8d-ca7cd60a3906");
            var edge69 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph34, node159, "m_Output", node160, "b", "276b291a-9bbf-4cd6-b92f-ab03afc90902");
            var edge70 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph34, node160, "Value", node161, "m_Result", "219ceff6-e2c2-438c-8e60-9bde8fc8f5a2");
            var edge71 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph34, node162, "m_Output", node163, "a", "c87d6108-e481-4839-9923-b1522e293251");
            var edge72 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph34, node163, "Value", node160, "a", "878c30d8-c91f-4d39-9627-a96c0ca5b597");
            var edge73 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph34, node164, "m_Output", node163, "b", "be8d10b4-1242-4325-bc72-dc40f8d98d79");
            var edge79 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph36, node171, "m_Output", node172, "m_Result", "7cd969bc-bb24-4083-b928-a318732f4237");
            var edge80 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph37, node173, "m_Output", node174, "b", "d386e344-d2b6-47c5-bfe9-95dc605aafa5");
            var edge81 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph37, node174, "Value", node175, "m_Result", "53e91a1c-9ab1-4e8a-96d7-3ee9c9771e9b");
            var edge82 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph37, node176, "m_Output", node177, "b", "703b8e26-53ba-4411-9ef1-b8df9dd9995e");
            var edge83 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph37, node177, "Value", node174, "a", "4ee0e06a-d010-4d5c-9ef5-8f9547bbf1f6");
            var edge84 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph37, node178, "m_Output", node177, "a", "1b03c73c-199d-446b-ba6c-2b16fda43039");
            return parts;
        }

        sealed class Attack4Parts
        {
            internal BtsmtlSkillFlowGraph graph27;
            internal BtsmtlSkillFlowGraph graph30;
            internal BtsmtlSkillFlowGraph graph31;
            internal BtsmtlSkillFlowGraph graph32;
            internal BtsmtlSkillFlowGraph graph33;
            internal BtsmtlSkillFlowGraph graph34;
            internal BtsmtlSkillFlowGraph graph36;
            internal BtsmtlSkillFlowGraph graph37;
        }
    }
}
