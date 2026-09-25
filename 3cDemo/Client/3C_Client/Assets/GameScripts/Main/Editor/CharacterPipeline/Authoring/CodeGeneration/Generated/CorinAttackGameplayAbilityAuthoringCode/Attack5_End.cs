using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode
    {
        static Attack5_EndParts BuildAttack5_End(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack5_EndParts();
            parts.graph69 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "b67c56ac-a560-55d6-965d-80641f9975b0", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To End Condition");
            parts.graph79 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "3d3c1c9d-4f1f-536f-9ec1-8c8bb2eb0d20", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 End To Exit Condition");
            parts.graph80 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "bc0790c3-34fd-44ef-90cf-4c12f801c6ff", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 End To Attack1 Condition");
            var node335 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph69, typeof(BtsmtlSkillNativeNodeWrapper<NOT>), "695bf217-15b0-5d96-8f98-6511027930f7", "NOT", new Vector2(-240f, 100f));
            var node333 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph69, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "963a8c27-8799-51a5-84b6-47da9490f275", "AND", new Vector2(220f, 70f));
            var node331 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph69, typeof(BtsmtlSkillBlackboardBooleanFlowNode), "c493a942-5634-56c6-a222-a1e50daf6747", "Attack5EndBoundary", new Vector2(-520f, 0f));
            var node336 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph69, typeof(BtsmtlSkillBlackboardBooleanFlowNode), "f4c7bb30-1ea2-4f81-a302-5cbb9cc95241", "Attack5Hit", new Vector2(-520f, 100f));
            var node334 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph69, typeof(BtsmtlSkillConditionResultFlowNode), "e45652d9-a7af-5814-9ed2-b083c6a02d53", "条件结果", new Vector2(600f, 180f));
            var node377 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillConditionResultFlowNode), "44f4b2cb-7234-5f01-95f3-f33985178fc3", "条件结果", new Vector2(600f, 180f));
            var node376 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillStateRootCompletedFlowNode), "7660be45-c0c4-5f38-94de-6644dbaf579d", "状态主体已完成", new Vector2(-360f, 0f));
            var moveInput = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillInputMagnitudeFlowNode), "1ba5bdc5-e84c-4df8-8e8b-c3d147b13cfa", "MoveAxis Magnitude", new Vector2(-520f, 120f));
            var moveThreshold = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillBlackboardScalarFlowNode), "9c954001-01d8-4c25-b19f-bee54ef26be8", "StopThreshold", new Vector2(-520f, 200f));
            var moveGreater = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "c34e1ccf-e104-4fff-aab1-aa5594a2350c", ">", new Vector2(-200f, 160f));
            var timelineTime = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillTimelineTimeFlowNode), "152a0fda-5ecb-41e2-87cb-e728c2fac6cb", "Timeline时间", new Vector2(-520f, 280f));
            var afterMoveWindow = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "3f5578da-eecf-425e-aed8-c2dc067d3224", "到达移动退出帧", new Vector2(-240f, 280f));
            var moveExit = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "92df36ad-dd12-4f96-a411-4032e7358a4d", "AND", new Vector2(0f, 200f));
            var exitOr = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillNativeNodeWrapper<OR>), "883689c1-6ff9-4988-b1c9-963d3a844374", "OR", new Vector2(200f, 80f));
            var restartRequest = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph80, typeof(BtsmtlSkillActionRequestFlowNode), "3c207e0e-1958-4dd9-9136-5fe33128b229", "Has Attack Request", new Vector2(-360f, 0f));
            var restartAdmission = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph80, typeof(BtsmtlSkillCanActivateActionFlowNode), "808d9dff-89d8-4f2b-9ad1-aabea3ab5e88", "Can Activate Attack", new Vector2(-360f, 140f));
            var restartAnd = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph80, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "84dfc816-96b4-4306-91a9-025ccc49a527", "AND", new Vector2(100f, 70f));
            var restartResult = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph80, typeof(BtsmtlSkillConditionResultFlowNode), "22edfc2d-098a-4920-89b8-8a14c9494342", "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringContract.Apply(node331, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "19735807-6d5f-5e7f-91c9-b841ccf1f71e"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(node336, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "6225401bc79441dca5eaab16bdbc0644"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(moveInput, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "MoveAxis"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(moveThreshold, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "1edc27e65f454837b415895f4b808048"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringCode.SetValue(afterMoveWindow, "b", 37f / 60f);
            BtsmtlSkillAuthoringContract.Apply(restartRequest, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "Attack"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(restartAdmission, new[] { new BtsmtlSkillAuthoringFieldValue("admissionProfile", rootParts.asset20), new BtsmtlSkillAuthoringFieldValue("targetSnapshot", new BtsmtlSkillTargetSnapshotReference("b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933")) });
            var edge191 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph69, node336, "m_Output", node335, "value", "a24f5206-8852-5bd8-a293-2b3e18c43683");
            var edge192 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph69, node331, "m_Output", node333, "a", "11c22c5a-2794-5cf8-a311-95a5530e6a29");
            var edge193 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph69, node333, "Value", node334, "m_Result", "752c37c8-2fb8-538e-8298-e68436a6a302");
            var edge194 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph69, node335, "Value", node333, "b", "0f2a0964-87d8-52f7-8a6a-11d386d0215e");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, moveInput, "m_Output", moveGreater, "a", "974b25f0-f7ee-4183-aa94-94fb43b91b24");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, moveThreshold, "m_Output", moveGreater, "b", "7c82a228-013d-4680-aa14-275d36b2911c");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, timelineTime, "m_Output", afterMoveWindow, "a", "e8d662f9-ea5d-4731-a25c-c246a2c0cf0a");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, afterMoveWindow, "Value", moveExit, "b", "8ef97460-a139-4d5a-a111-7dc34889eb5b");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, moveGreater, "Value", moveExit, "a", "ceb87808-e3bc-4fe6-b80e-6e8a0da79169");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, moveExit, "Value", exitOr, "a", "53c8b7d4-585d-421e-85d6-b0cccc35fecc");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, node376, "m_Output", exitOr, "b", "7e4c7c21-f1cc-410d-b865-259e2caf76f2");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, exitOr, "Value", node377, "m_Result", "9181a9d0-8f52-5928-9542-84c91a34b1e4");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph80, restartRequest, "m_Output", restartAnd, "a", "fa100514-10a1-402a-a2af-4dfd7199d100");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph80, restartAdmission, "m_Output", restartAnd, "b", "8466b626-c8e4-4cee-8d27-10be32d66fe3");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph80, restartAnd, "Value", restartResult, "m_Result", "26663c4d-1fda-45f6-ac70-b59619b6bdcd");
            return parts;
        }

        sealed class Attack5_EndParts
        {
            internal BtsmtlSkillFlowGraph graph69;
            internal BtsmtlSkillFlowGraph graph79;
            internal BtsmtlSkillFlowGraph graph80;
        }
    }
}
