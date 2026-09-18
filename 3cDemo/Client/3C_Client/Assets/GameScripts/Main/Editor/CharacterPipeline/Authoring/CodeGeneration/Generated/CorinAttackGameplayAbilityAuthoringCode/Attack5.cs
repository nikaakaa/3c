using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode
    {
        static Attack5Parts BuildAttack5(AttackParts attack, RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack5Parts();
            parts.graph35 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "181848fe552cf5d8dea571420ba4d097", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack4 To Attack5 Condition");
            parts.graph39 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph38, "47990a9445e2bdec6a74d1f4522332ff", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5Hit");
            parts.graph40 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph38, "9fd63abef3169807a4ac031a16341e66", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryEarly");
            parts.graph41 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph38, "f73db1a04c83dfd8b64ece524d072146", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryLate");
            parts.graph42 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "578514b8212731449843673c435635fd", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To Exit Condition");
            parts.graph43 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "9c3889ef21ffe74ea03e03a8c95d1f1e", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To Exit Condition");
            parts.graph44 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "9293934dc033be2e9f406fb511cfddc2", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To Exit Condition");
            var node165 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph35, typeof(BtsmtlSkillCanActivateActionFlowNode), "79a3da65-a63c-4cc5-8fb0-92f1b2b2a2b0", "Can Activate Attack", new Vector2(-360f, 200f));
            var node169 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph35, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "af016b33-7d67-48a2-a359-1ed676825ffd", "AND", new Vector2(40f, 35f));
            var node168 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph35, typeof(BtsmtlSkillActionWindowActiveFlowNode), "c83151bc-009a-4d34-b38c-f99e2b45b51e", "Window ComboAccept", new Vector2(-360f, 100f));
            var node167 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph35, typeof(BtsmtlSkillConditionResultFlowNode), "d6bc5a5d-7649-4be1-ae8e-ff1a92e7bb94", "条件结果", new Vector2(600f, 180f));
            var node166 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph35, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "de106c4b-d5f8-434c-9652-816402d20945", "AND", new Vector2(220f, 70f));
            var node170 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph35, typeof(BtsmtlSkillActionRequestFlowNode), "e88fb30b-48ed-49c6-883b-6027beee4801", "Has Attack Request", new Vector2(-360f, 0f));
            var node183 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph39, typeof(BtsmtlSkillTimelineEnableFlowNode), "0f1fd664-33df-44bf-8e83-e82a3caf6822", "片段启用", new Vector2(120f, 60f));
            var node187 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph39, typeof(BtsmtlSkillTimelineDestroyFlowNode), "2378333f-ce0c-4f60-bf69-f01bcc591053", "片段销毁", new Vector2(120f, 660f));
            var node186 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph39, typeof(BtsmtlSkillTimelineDisableFlowNode), "409b788f-7b4a-4e03-ae38-0664b0ed7b4d", "片段停用", new Vector2(120f, 460f));
            var node184 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph39, typeof(BtsmtlSkillRootFlowNode), "f2d16ae4-4acb-41c9-92c1-b1afcae1bb36", "技能入口", new Vector2(120f, 260f));
            var node185 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph39, typeof(BtsmtlSkillBlackboardSetFlowNode), "fad0f316-daa7-47fa-9284-56f6144623ef", "Set Attack2Hit", new Vector2(320f, 0f));
            var node191 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph40, typeof(BtsmtlSkillTimelineDisableFlowNode), "25f06ce9-cf99-4be6-8c8c-41000dd6c5a6", "片段停用", new Vector2(120f, 460f));
            var node189 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph40, typeof(BtsmtlSkillRootFlowNode), "36ab6c98-6b04-40e6-aaf1-3712dffda347", "技能入口", new Vector2(120f, 260f));
            var node188 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph40, typeof(BtsmtlSkillTimelineEnableFlowNode), "7d36691a-7950-4114-bd30-528b660f2b77", "片段启用", new Vector2(120f, 60f));
            var node192 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph40, typeof(BtsmtlSkillTimelineDestroyFlowNode), "a1bfb095-96f8-419d-b537-4729a27224e5", "片段销毁", new Vector2(120f, 660f));
            var node190 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph40, typeof(BtsmtlSkillBlackboardSetFlowNode), "fe3ee5fe-a9fd-4d7e-8949-a151fca7aa22", "Set RecoveryEarly", new Vector2(320f, 0f));
            var node196 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph41, typeof(BtsmtlSkillTimelineDisableFlowNode), "369ce7a8-b06a-4af1-9d16-01cd5e201eee", "片段停用", new Vector2(120f, 460f));
            var node193 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph41, typeof(BtsmtlSkillTimelineEnableFlowNode), "7efaa326-d89a-480f-8509-a501136e0c73", "片段启用", new Vector2(120f, 60f));
            var node194 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph41, typeof(BtsmtlSkillRootFlowNode), "92969358-7eef-4928-8bc3-f58eb568d069", "技能入口", new Vector2(120f, 260f));
            var node195 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph41, typeof(BtsmtlSkillBlackboardSetFlowNode), "93b0eb27-adbf-4da9-ad9c-23d24f774f28", "Set RecoveryLate", new Vector2(320f, 0f));
            var node197 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph41, typeof(BtsmtlSkillTimelineDestroyFlowNode), "dae97261-bbf7-4c7c-85b9-023990514765", "片段销毁", new Vector2(120f, 660f));
            var node202 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph42, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "4abc6e48-d805-4b21-bc75-63de209c1331", "AND", new Vector2(40f, 35f));
            var node199 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph42, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "8b225da2-3186-4a89-b8c0-52f04672ee2d", "AND", new Vector2(220f, 70f));
            var node198 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph42, typeof(BtsmtlSkillCanActivateActionFlowNode), "924a96f3-9be9-469b-b6eb-b9f4c8233967", "Can Activate Dodge", new Vector2(-360f, 200f));
            var node201 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph42, typeof(BtsmtlSkillActionWindowActiveFlowNode), "9cc1864f-a1cb-4fec-8b44-bd9e08927c8d", "Window RecoveryEarly", new Vector2(-360f, 100f));
            var node203 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph42, typeof(BtsmtlSkillActionRequestFlowNode), "cae0c3b1-d201-49d0-88b4-5faddaf059b4", "Has Dodge Request", new Vector2(-360f, 0f));
            var node200 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph42, typeof(BtsmtlSkillConditionResultFlowNode), "f5943698-e1a0-434a-9ae4-06fe8ae03530", "条件结果", new Vector2(600f, 180f));
            var node205 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph43, typeof(BtsmtlSkillConditionResultFlowNode), "b34b38c9-c654-47b8-8556-d295201d0e29", "条件结果", new Vector2(600f, 180f));
            var node204 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph43, typeof(BtsmtlSkillStateRootCompletedFlowNode), "c66123e9-2ff9-453f-8a8a-7f05d3788926", "状态主体已完成", new Vector2(-360f, 0f));
            var node210 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph44, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "0aa95855-7548-4c10-b18d-fe47a97f0902", ">", new Vector2(-240f, 20f));
            var node207 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph44, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "2494ffd1-274d-4172-b511-fb17904bae0c", "AND", new Vector2(40f, 35f));
            var node206 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph44, typeof(BtsmtlSkillActionWindowActiveFlowNode), "4226ba2e-1e41-4c26-8ed0-a15fa7c78115", "Window RecoveryLate", new Vector2(-360f, 100f));
            var node208 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph44, typeof(BtsmtlSkillConditionResultFlowNode), "80c40d52-a358-4baa-b990-8f4961743d36", "条件结果", new Vector2(600f, 180f));
            var node209 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph44, typeof(BtsmtlSkillBlackboardScalarFlowNode), "b6cfc921-c2f8-4f9b-bd12-423cf1e954af", "StopThreshold", new Vector2(-520f, 45f));
            var node211 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph44, typeof(BtsmtlSkillInputMagnitudeFlowNode), "c8a6ff82-347d-4a84-993e-5c64a52f5409", "MoveAxis Magnitude", new Vector2(-520f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node165, new[] { new BtsmtlSkillAuthoringFieldValue("admissionProfile", rootParts.asset), new BtsmtlSkillAuthoringFieldValue("targetSnapshot", new BtsmtlSkillTargetSnapshotReference("b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933")) });
            BtsmtlSkillAuthoringContract.Apply(node168, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "ComboAccept") });
            BtsmtlSkillAuthoringContract.Apply(node170, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "Attack"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node185, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "6225401bc79441dca5eaab16bdbc0644"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node190, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "f3f45c6944c247539e29c5b37ba6dede"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "d0c0d504787f8e6989ba2bae6aa1a49a"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node195, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "fd926414f6864445b2c3c2050a158d04"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "d0c0d504787f8e6989ba2bae6aa1a49a"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node198, new[] { new BtsmtlSkillAuthoringFieldValue("admissionProfile", rootParts.asset21), new BtsmtlSkillAuthoringFieldValue("targetSnapshot", new BtsmtlSkillTargetSnapshotReference("b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933")) });
            BtsmtlSkillAuthoringContract.Apply(node201, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RecoveryEarly") });
            BtsmtlSkillAuthoringContract.Apply(node203, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "Dodge"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node206, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RecoveryLate") });
            BtsmtlSkillAuthoringContract.Apply(node209, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "1edc27e65f454837b415895f4b808048"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(node211, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "MoveAxis"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            var edge74 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph35, node165, "m_Output", node166, "b", "aa0ce478-92a7-42fd-ad00-5ce1a029396b");
            var edge75 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph35, node166, "Value", node167, "m_Result", "99e605d7-d252-4199-b71e-0c80491a198e");
            var edge76 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph35, node168, "m_Output", node169, "b", "13087de2-b46e-4197-a24f-5465b21f7df0");
            var edge77 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph35, node169, "Value", node166, "a", "968f5392-0920-40b4-8fad-eddfc18d3653");
            var edge78 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph35, node170, "m_Output", node169, "a", "a6d53455-e3d8-4f2a-9b61-bb41a5001146");
            var edge86 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph39, node184, "Output", node185, "Input", "c4931a91-9791-48df-9571-b7a0f9cb13e8");
            var edge87 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph40, node189, "Output", node190, "Input", "5f3e4157-bb2b-4619-aab9-2c6d04200890");
            var edge88 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph41, node194, "Output", node195, "Input", "422f100d-3716-425c-abd2-595b3cfe8768");
            var edge89 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph42, node198, "m_Output", node199, "b", "17244c10-2ba2-4c18-ad68-076468427288");
            var edge90 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph42, node199, "Value", node200, "m_Result", "de5cb30a-ec1a-461f-8e0d-8aaf8ecf9514");
            var edge91 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph42, node201, "m_Output", node202, "b", "07495c4f-6c6d-44ef-8062-abfd4d74b5a1");
            var edge92 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph42, node202, "Value", node199, "a", "2e48b774-5ab0-4cd1-8633-1920e2ac012a");
            var edge93 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph42, node203, "m_Output", node202, "a", "6194e31b-fa2f-4ab4-9e27-69cbfbd124d0");
            var edge94 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph43, node204, "m_Output", node205, "m_Result", "d4923ad8-fb95-4a2b-9e78-b3ad6a2913b4");
            var edge95 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph44, node206, "m_Output", node207, "b", "734548a5-edee-48d2-a78b-43c116091164");
            var edge96 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph44, node207, "Value", node208, "m_Result", "f5fc9123-4b54-4f98-9573-f4e2d21e917b");
            var edge97 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph44, node209, "m_Output", node210, "b", "a19ecf45-7ca5-4d7d-b423-559b2cd2ebfa");
            var edge98 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph44, node210, "Value", node207, "a", "7701b272-ec3e-402c-ada8-fe2845eed0e3");
            var edge99 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph44, node211, "m_Output", node210, "a", "638878a5-4b43-4937-8d4e-88490bf7ef4e");
            return parts;
        }

        sealed class Attack5Parts
        {
            internal BtsmtlSkillFlowGraph graph35;
            internal BtsmtlSkillFlowGraph graph39;
            internal BtsmtlSkillFlowGraph graph40;
            internal BtsmtlSkillFlowGraph graph41;
            internal BtsmtlSkillFlowGraph graph42;
            internal BtsmtlSkillFlowGraph graph43;
            internal BtsmtlSkillFlowGraph graph44;
        }
    }
}
