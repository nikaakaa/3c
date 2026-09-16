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
        static Attack2Parts BuildAttack2(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack2Parts();
            parts.graph8 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "fbe7f0ab5a6848bf28dd2f55f7242282", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack1 To Attack2 Condition");
            parts.graph17 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "18da04dc0b1711110ef19f05b5146bee", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack2 To Exit Condition");
            parts.graph18 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "b6ef910563ebce087609b38a48a54faa", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack2 To Exit Condition");
            parts.graph19 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "6b627c62b78b273dab72cf76c0b62086", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack2 To Exit Condition");
            var node36 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillConditionResultFlowNode), "45786ebc-05a5-44a1-bca6-2888a6bdcf80", "条件结果", new Vector2(600f, 180f));
            var node35 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "5abfc932-3527-4581-8b42-b9c28a6e63fd", "AND", new Vector2(220f, 70f));
            var node34 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "5e4efb9b-de3b-445b-8ae7-d304b413e02b", "AND", new Vector2(40f, 35f));
            var node33 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillActionRequestFlowNode), "cce6a5ea-67c8-4a19-9b57-39e771e2364b", "Has Attack Request", new Vector2(-360f, 0f));
            var node37 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillActionWindowActiveFlowNode), "ec5dd0ff-4406-41ae-886e-6799be818ffe", "Window ComboAccept", new Vector2(-360f, 100f));
            var node38 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillCanActivateActionFlowNode), "f73664b9-e509-4355-85d1-76a35f5cb88a", "Can Activate Attack", new Vector2(-360f, 200f));
            var node55 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph12, typeof(BtsmtlSkillTimelineDestroyFlowNode), "04197c9c-ea02-414c-8d5e-1ca9e9e8c90b", "片段销毁", new Vector2(120f, 660f));
            var node53 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph12, typeof(BtsmtlSkillBlackboardSetFlowNode), "3e573873-16f9-4a38-85b3-aafdaf8f6e82", "Set RecoveryEarly", new Vector2(320f, 0f));
            var node54 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph12, typeof(BtsmtlSkillTimelineDisableFlowNode), "49b0ea78-3bde-4a0b-8add-3a1dfe7051c3", "片段停用", new Vector2(120f, 460f));
            var node51 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph12, typeof(BtsmtlSkillTimelineEnableFlowNode), "adf26532-b12d-40c7-a999-1999f81af71a", "片段启用", new Vector2(120f, 60f));
            var node52 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph12, typeof(BtsmtlSkillRootFlowNode), "c3803fe7-214b-4985-89c7-f6592a0b6b4e", "技能入口", new Vector2(120f, 260f));
            var node60 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph13, typeof(BtsmtlSkillTimelineDestroyFlowNode), "09bedfc8-90c8-47aa-bc96-4c096b49f316", "片段销毁", new Vector2(120f, 660f));
            var node56 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph13, typeof(BtsmtlSkillTimelineEnableFlowNode), "18f4f01d-a121-488d-9ed4-006524b75aa1", "片段启用", new Vector2(120f, 60f));
            var node58 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph13, typeof(BtsmtlSkillBlackboardSetFlowNode), "4efb3df8-df1b-4cc0-8c0b-3c202b23e994", "Set Attack2Hit", new Vector2(320f, 0f));
            var node59 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph13, typeof(BtsmtlSkillTimelineDisableFlowNode), "820a8125-2853-493e-9742-cb619211dafa", "片段停用", new Vector2(120f, 460f));
            var node57 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph13, typeof(BtsmtlSkillRootFlowNode), "ffd3564c-f03e-4ad1-9b9d-35145b213ed9", "技能入口", new Vector2(120f, 260f));
            var node64 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph14, typeof(BtsmtlSkillTimelineDisableFlowNode), "5b4eb23b-58aa-45a5-9621-ffc0aaacdb6c", "片段停用", new Vector2(120f, 460f));
            var node65 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph14, typeof(BtsmtlSkillTimelineDestroyFlowNode), "6915eefa-9d42-450d-9eee-d73a847eb47c", "片段销毁", new Vector2(120f, 660f));
            var node63 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph14, typeof(BtsmtlSkillBlackboardSetFlowNode), "7cc12057-a4f5-4516-879c-fab602d9a377", "Set ComboAccept", new Vector2(320f, 0f));
            var node62 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph14, typeof(BtsmtlSkillRootFlowNode), "81317361-7dcb-46e0-a464-faf7b9344409", "技能入口", new Vector2(120f, 260f));
            var node61 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph14, typeof(BtsmtlSkillTimelineEnableFlowNode), "93879655-33c1-42d2-8fec-8a299e1fe4d4", "片段启用", new Vector2(120f, 60f));
            var node67 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph15, typeof(BtsmtlSkillRootFlowNode), "2dbbadbb-d768-4c44-82d7-857a1ad0e296", "技能入口", new Vector2(120f, 260f));
            var node69 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph15, typeof(BtsmtlSkillTimelineDisableFlowNode), "330817cf-25f5-4d60-885f-fb93d9acc709", "片段停用", new Vector2(120f, 460f));
            var node68 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph15, typeof(BtsmtlSkillBlackboardSetFlowNode), "8c0ec107-02fa-430b-b316-80bb649d7df4", "Set RecoveryLate", new Vector2(320f, 0f));
            var node66 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph15, typeof(BtsmtlSkillTimelineEnableFlowNode), "9e415cce-14f7-45d1-b1ca-23dcd618cebe", "片段启用", new Vector2(120f, 60f));
            var node70 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph15, typeof(BtsmtlSkillTimelineDestroyFlowNode), "c46e9fae-fbb8-4e26-8755-09b445855d2f", "片段销毁", new Vector2(120f, 660f));
            var node77 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph17, typeof(BtsmtlSkillInputMagnitudeFlowNode), "13a54652-1edf-495b-998a-1f7f96c9b2b8", "MoveAxis Magnitude", new Vector2(-520f, 0f));
            var node78 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph17, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "52d6e59f-93d7-4c15-93cd-e8b250f4bca3", ">", new Vector2(-240f, 20f));
            var node81 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph17, typeof(BtsmtlSkillBlackboardScalarFlowNode), "6d48f07b-3aca-41b5-aa29-a664e317426b", "StopThreshold", new Vector2(-520f, 45f));
            var node80 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph17, typeof(BtsmtlSkillConditionResultFlowNode), "9296adc6-5edf-4cb7-81ac-1b83adbfaa50", "条件结果", new Vector2(600f, 180f));
            var node79 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph17, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "ed675b3a-a2bb-4378-b89e-937fd25d57a5", "AND", new Vector2(40f, 35f));
            var node82 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph17, typeof(BtsmtlSkillActionWindowActiveFlowNode), "ff3387f2-e728-442c-9351-de8217f5dde7", "Window RecoveryLate", new Vector2(-360f, 100f));
            var node86 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph18, typeof(BtsmtlSkillConditionResultFlowNode), "04a71ac0-abf9-40fa-aa7d-2a67f4e67c3e", "条件结果", new Vector2(600f, 180f));
            var node85 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph18, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "330848fb-7efd-4c3e-ba8b-91a17ddc45c2", "AND", new Vector2(220f, 70f));
            var node84 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph18, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "4f62e295-cea3-4d67-9212-d07a4ebc2cf1", "AND", new Vector2(40f, 35f));
            var node83 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph18, typeof(BtsmtlSkillActionRequestFlowNode), "b174a173-98d1-4673-96c3-fd7a0aadb3fa", "Has Dodge Request", new Vector2(-360f, 0f));
            var node87 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph18, typeof(BtsmtlSkillActionWindowActiveFlowNode), "b6a9c363-16f1-4f91-bb9e-5125820e2e50", "Window RecoveryEarly", new Vector2(-360f, 100f));
            var node88 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph18, typeof(BtsmtlSkillCanActivateActionFlowNode), "e48ce2f5-a4e9-4e5e-ace0-accac91de4ca", "Can Activate Dodge", new Vector2(-360f, 200f));
            var node89 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph19, typeof(BtsmtlSkillStateRootCompletedFlowNode), "5b612365-a42e-4374-a830-6bea3f4af3a7", "状态主体已完成", new Vector2(-360f, 0f));
            var node90 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph19, typeof(BtsmtlSkillConditionResultFlowNode), "d5b8e174-74e5-417c-9685-1f262a71af1c", "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringContract.Apply(node33, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "Attack"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node37, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "ComboAccept") });
            BtsmtlSkillAuthoringContract.Apply(node38, new[] { new BtsmtlSkillAuthoringFieldValue("admissionProfile", context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>("Assets/Configs/Character/Corin/Pipeline/Actions/Attack/CorinAttackActionProfile.asset", 11400000L)), new BtsmtlSkillAuthoringFieldValue("targetSnapshot", new BtsmtlSkillTargetSnapshotReference("b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933")) });
            BtsmtlSkillAuthoringContract.Apply(node53, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "45097cf16c2e46d395111550dda1cd18"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "ef6ea798d23cbb42daa656723c5263b2"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node58, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "676010964678489285e72c0bf7ec64a2"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node63, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "c289023d917b45be835f404f75cf2fe3"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "ef6ea798d23cbb42daa656723c5263b2"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node68, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "f4e893c7da714366a7feb74b85faad47"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "ef6ea798d23cbb42daa656723c5263b2"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node77, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "MoveAxis"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node81, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "1edc27e65f454837b415895f4b808048"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(node82, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RecoveryLate") });
            BtsmtlSkillAuthoringContract.Apply(node83, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "Dodge"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node87, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RecoveryEarly") });
            BtsmtlSkillAuthoringContract.Apply(node88, new[] { new BtsmtlSkillAuthoringFieldValue("admissionProfile", context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>("Assets/Configs/Character/Corin/Pipeline/Actions/Dodge/CorinDodgeActionProfile.asset", 11400000L)), new BtsmtlSkillAuthoringFieldValue("targetSnapshot", new BtsmtlSkillTargetSnapshotReference("b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933")) });
            var edge11 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph8, node33, "m_Output", node34, "a", "2ee1a38e-b035-4654-ba09-f480f015a2c7");
            var edge12 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph8, node34, "Value", node35, "a", "1e52bba8-3755-4534-a6ae-f2a96385d33f");
            var edge13 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph8, node35, "Value", node36, "m_Result", "bda74353-7906-4d63-ad12-fcab657bc1fa");
            var edge14 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph8, node37, "m_Output", node34, "b", "b45493c9-2454-446c-beff-1d8886d66f32");
            var edge15 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph8, node38, "m_Output", node35, "b", "1c4a064e-760a-4aad-8245-01272b6e2561");
            var edge23 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph12, node52, "Output", node53, "Input", "9f1a253a-f128-4904-a02f-2642c184e9da");
            var edge24 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph13, node57, "Output", node58, "Input", "d061fe7a-5fc3-4624-934b-0544302ed348");
            var edge25 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph14, node62, "Output", node63, "Input", "5ccb6ca2-713b-4f8b-9ac0-f2d9bb82c0e4");
            var edge26 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph15, node67, "Output", node68, "Input", "7804cd0b-5b3d-4b2b-b633-41b0a1dbc6f1");
            var edge32 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph17, node77, "m_Output", node78, "a", "0c57f93a-f02e-4823-8c0a-13c6deca0a6f");
            var edge33 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph17, node78, "Value", node79, "a", "18990e82-9bb3-46a6-a8f4-bf06f1956f39");
            var edge34 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph17, node79, "Value", node80, "m_Result", "8cf687a0-b8ae-42d8-87f6-9ab48b6354f1");
            var edge35 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph17, node81, "m_Output", node78, "b", "ab7dcfc3-d27b-4c85-8cd7-752224e40be9");
            var edge36 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph17, node82, "m_Output", node79, "b", "869f8419-548e-4146-b906-3f6065c3f838");
            var edge37 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph18, node83, "m_Output", node84, "a", "8ec34cc3-32f5-4feb-9fad-8268b86da0d7");
            var edge38 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph18, node84, "Value", node85, "a", "698731f4-a462-4c2e-9f84-9db77be79daf");
            var edge39 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph18, node85, "Value", node86, "m_Result", "3bf13cbe-7c52-43b8-9338-56fcb8f53aa2");
            var edge40 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph18, node87, "m_Output", node84, "b", "5789cfe7-ee9e-442d-a65c-815f4beb0ed5");
            var edge41 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph18, node88, "m_Output", node85, "b", "90540feb-0e11-410a-8240-5671d1f1f8ad");
            var edge42 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph19, node89, "m_Output", node90, "m_Result", "420c3d01-a9b5-4a74-af0a-8d7adc7de53a");
            return parts;
        }

        sealed class Attack2Parts
        {
            internal BtsmtlSkillFlowGraph graph8;
            internal BtsmtlSkillFlowGraph graph12;
            internal BtsmtlSkillFlowGraph graph13;
            internal BtsmtlSkillFlowGraph graph14;
            internal BtsmtlSkillFlowGraph graph15;
            internal BtsmtlSkillFlowGraph graph17;
            internal BtsmtlSkillFlowGraph graph18;
            internal BtsmtlSkillFlowGraph graph19;
        }
    }
}
