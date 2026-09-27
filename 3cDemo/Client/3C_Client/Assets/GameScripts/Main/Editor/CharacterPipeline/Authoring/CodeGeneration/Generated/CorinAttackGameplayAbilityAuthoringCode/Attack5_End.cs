using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode
    {
        static Attack5_EndParts BuildAttack5_End(AttackParts attack, RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack5_EndParts();
            parts.graph69 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "b67c56ac-a560-55d6-965d-80641f9975b0", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To End Condition");
            parts.graph79 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "3d3c1c9d-4f1f-536f-9ec1-8c8bb2eb0d20", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 End To Exit Condition");
            parts.graph80 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "bc0790c3-34fd-44ef-90cf-4c12f801c6ff", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 End To Attack1 Condition");
            parts.graph82 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph78, "a0b1c2d3-e4f5-4678-90ab-c1d2e3f4a5b6", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5End ComboAccept");
            parts.graph83 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph82, "b1c2d3e4-f5a6-4789-0abc-d2e3f4a5b6c7", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            parts.graph84 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph78, "6ea21a0a-7b3d-4f35-97a8-6357ec498c0c", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5End RecoveryLate");
            parts.graph85 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph84, "2e384ba0-9c3d-4bd8-859d-80871596dfca", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
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
            var moveWindow = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillActionWindowActiveFlowNode), "7da3ef23-5658-43ee-a3da-b894a722c33f", "Window RecoveryLate", new Vector2(-240f, 280f));
            var moveExit = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "92df36ad-dd12-4f96-a411-4032e7358a4d", "AND", new Vector2(0f, 200f));
            var exitOr = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillNativeNodeWrapper<OR>), "883689c1-6ff9-4988-b1c9-963d3a844374", "OR", new Vector2(200f, 80f));
            var restartRequest = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph80, typeof(BtsmtlSkillActionRequestFlowNode), "3c207e0e-1958-4dd9-9136-5fe33128b229", "Has Attack Request", new Vector2(-360f, 0f));
            var restartWindow = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph80, typeof(BtsmtlSkillActionWindowActiveFlowNode), "c2db97c2-02f1-49cb-9d99-561392b68ddb", "Window ComboAccept", new Vector2(-360f, 70f));
            var restartAdmission = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph80, typeof(BtsmtlSkillCanActivateActionFlowNode), "808d9dff-89d8-4f2b-9ad1-aabea3ab5e88", "Can Activate Attack", new Vector2(-360f, 140f));
            var restartInputAnd = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph80, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "d0e1f2a3-b4c5-4678-90d1-e2f3a4b5c6d7", "AND", new Vector2(-100f, 35f));
            var restartAnd = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph80, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "84dfc816-96b4-4306-91a9-025ccc49a527", "AND", new Vector2(100f, 70f));
            var restartResult = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph80, typeof(BtsmtlSkillConditionResultFlowNode), "22edfc2d-098a-4920-89b8-8a14c9494342", "条件结果", new Vector2(600f, 180f));
            var node383 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph82, typeof(BtsmtlSkillTimelineEnableFlowNode), "c0d1e2f3-a4b5-4c6d-8e7f-901a2b3c4d5e", "片段启用", new Vector2(120f, 60f));
            var node384 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph82, typeof(BtsmtlSkillRootFlowNode), "d1e2f3a4-b5c6-4d7e-8f90-1a2b3c4d5e6f", "技能入口", new Vector2(120f, 260f));
            var node385 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph82, typeof(BtsmtlSkillSelectorFlowNode), "e2f3a4b5-c6d7-4e8f-901a-2b3c4d5e6f70", "窗口执行或结束", new Vector2(280f, 260f));
            var node386 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph82, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "f3a4b5c6-d7e8-4f90-a1b2-3c4d5e6f7081", "结束片段", new Vector2(520f, 260f));
            var node387 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph82, typeof(BtsmtlSkillBlackboardSetFlowNode), "a4b5c6d7-e8f9-401a-b2c3-4d5e6f708192", "Set ComboAccept", new Vector2(320f, 0f));
            var node388 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph82, typeof(BtsmtlSkillTimelineDisableFlowNode), "b5c6d7e8-f9a0-412b-c3d4-5e6f708192a3", "片段停用", new Vector2(120f, 460f));
            var node389 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph82, typeof(BtsmtlSkillTimelineDestroyFlowNode), "c6d7e8f9-a0b1-423c-d4e5-6f708192a3b4", "片段销毁", new Vector2(120f, 660f));
            var node390 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph83, typeof(BtsmtlSkillTimelineTimeFlowNode), "d7e8f9a0-b1c2-434d-e5f6-708192a3b4c5", "Timeline时间", new Vector2(-360f, 0f));
            var node391 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph83, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "e8f9a0b1-c2d3-445e-f607-8192a3b4c5d6", "到达结束时间", new Vector2(-100f, 0f));
            var node392 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph83, typeof(BtsmtlSkillConditionResultFlowNode), "f9a0b1c2-d3e4-456f-0781-92a3b4c5d6e7", null, new Vector2(600f, 180f));
            var recoveryEnable = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph84, typeof(BtsmtlSkillTimelineEnableFlowNode), "d50b74c8-219f-4c84-a0bf-301971d7c56a", "片段启用", new Vector2(120f, 60f));
            var recoveryRoot = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph84, typeof(BtsmtlSkillRootFlowNode), "88e09063-6d44-4ce7-8365-a97c31aee6f6", "技能入口", new Vector2(120f, 260f));
            var recoverySelector = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph84, typeof(BtsmtlSkillSelectorFlowNode), "183f3ef9-74f0-4df0-ae38-e3548639535e", "窗口执行或结束", new Vector2(280f, 260f));
            var recoveryExit = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph84, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "aa003de9-7c8d-4b62-8f07-76438976724a", "结束片段", new Vector2(520f, 260f));
            var recoverySet = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph84, typeof(BtsmtlSkillBlackboardSetFlowNode), "72255436-dcce-4b0c-b180-35740da61b64", "Set RecoveryLate", new Vector2(320f, 0f));
            var recoveryDisable = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph84, typeof(BtsmtlSkillTimelineDisableFlowNode), "27980712-074e-48b1-ae59-b2a930113bdf", "片段停用", new Vector2(120f, 460f));
            var recoveryDestroy = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph84, typeof(BtsmtlSkillTimelineDestroyFlowNode), "6663a695-f9f8-4e9a-9cb9-e176bcde4910", "片段销毁", new Vector2(120f, 660f));
            var recoveryTime = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph85, typeof(BtsmtlSkillTimelineTimeFlowNode), "c1e88c2d-5e49-4b26-8754-9245d947360e", "Timeline时间", new Vector2(-360f, 0f));
            var recoveryEnd = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph85, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "9d72f768-af50-420a-9880-329aa9e6a6c2", "到达结束时间", new Vector2(-100f, 0f));
            var recoveryResult = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph85, typeof(BtsmtlSkillConditionResultFlowNode), "0981e23a-1f05-4d4e-9063-bbcad84b7374", null, new Vector2(600f, 180f));
            BtsmtlSkillAuthoringCode.PruneFlowGraph(parts.graph69, new[] { "c493a942-5634-56c6-a222-a1e50daf6747", "f4c7bb30-1ea2-4f81-a302-5cbb9cc95241", "963a8c27-8799-51a5-84b6-47da9490f275", "e45652d9-a7af-5814-9ed2-b083c6a02d53", "695bf217-15b0-5d96-8f98-6511027930f7", "f4c7bb30-1ea2-4f81-a302-5cbb9cc95241" }, new string[0]);
            BtsmtlSkillAuthoringCode.PruneFlowGraph(parts.graph80, new[] { "3c207e0e-1958-4dd9-9136-5fe33128b229", "c2db97c2-02f1-49cb-9d99-561392b68ddb", "808d9dff-89d8-4f2b-9ad1-aabea3ab5e88", "d0e1f2a3-b4c5-4678-90d1-e2f3a4b5c6d7", "84dfc816-96b4-4306-91a9-025ccc49a527", "22edfc2d-098a-4920-89b8-8a14c9494342" }, new string[0]);
            BtsmtlSkillAuthoringCode.PruneFlowGraph(parts.graph82, new[] { "c0d1e2f3-a4b5-4c6d-8e7f-901a2b3c4d5e", "d1e2f3a4-b5c6-4d7e-8f90-1a2b3c4d5e6f", "e2f3a4b5-c6d7-4e8f-901a-2b3c4d5e6f70", "f3a4b5c6-d7e8-4f90-a1b2-3c4d5e6f7081", "a4b5c6d7-e8f9-401a-b2c3-4d5e6f708192", "b5c6d7e8-f9a0-412b-c3d4-5e6f708192a3", "c6d7e8f9-a0b1-423c-d4e5-6f708192a3b4" }, new string[0]);
            BtsmtlSkillAuthoringCode.PruneFlowGraph(parts.graph83, new[] { "d7e8f9a0-b1c2-434d-e5f6-708192a3b4c5", "e8f9a0b1-c2d3-445e-f607-8192a3b4c5d6", "f9a0b1c2-d3e4-456f-0781-92a3b4c5d6e7" }, new string[0]);
            BtsmtlSkillAuthoringContract.Apply(node331, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "19735807-6d5f-5e7f-91c9-b841ccf1f71e"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(node336, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "6225401bc79441dca5eaab16bdbc0644"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(moveInput, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "MoveAxis"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(moveThreshold, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "1edc27e65f454837b415895f4b808048"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(moveWindow, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RecoveryLate") });
            BtsmtlSkillAuthoringContract.Apply(restartRequest, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "Attack"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(restartWindow, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "ComboAccept") });
            BtsmtlSkillAuthoringContract.Apply(restartAdmission, new[] { new BtsmtlSkillAuthoringFieldValue("admissionProfile", rootParts.asset20), new BtsmtlSkillAuthoringFieldValue("targetSnapshot", new BtsmtlSkillTargetSnapshotReference("b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933")) });
            BtsmtlSkillAuthoringContract.Apply(node387, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "f4a6b8c0-d2e4-4618-a0b2-c4d6e8f0a2b4"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "f6145a5e-1e4e-51a4-aee5-9c71a99ac525"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node387, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(node391, "b", 121f / 60f);
            BtsmtlSkillAuthoringContract.Apply(recoverySelector, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph85, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringContract.Apply(recoverySet, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "9e109c07-120e-43b3-a05b-ce4370830cab"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "f6145a5e-1e4e-51a4-aee5-9c71a99ac525"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(recoverySet, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(recoveryEnd, "b", 121f / 60f);
            var edge191 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph69, node336, "m_Output", node335, "value", "a24f5206-8852-5bd8-a293-2b3e18c43683");
            var edge192 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph69, node331, "m_Output", node333, "a", "11c22c5a-2794-5cf8-a311-95a5530e6a29");
            var edge194 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph69, node335, "Value", node333, "b", "0f2a0964-87d8-52f7-8a6a-11d386d0215e");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, moveInput, "m_Output", moveGreater, "a", "974b25f0-f7ee-4183-aa94-94fb43b91b24");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, moveThreshold, "m_Output", moveGreater, "b", "7c82a228-013d-4680-aa14-275d36b2911c");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, moveWindow, "m_Output", moveExit, "b", "8ef97460-a139-4d5a-a111-7dc34889eb5b");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, moveGreater, "Value", moveExit, "a", "ceb87808-e3bc-4fe6-b80e-6e8a0da79169");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, moveExit, "Value", exitOr, "a", "53c8b7d4-585d-421e-85d6-b0cccc35fecc");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, node376, "m_Output", exitOr, "b", "7e4c7c21-f1cc-410d-b865-259e2caf76f2");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, exitOr, "Value", node377, "m_Result", "9181a9d0-8f52-5928-9542-84c91a34b1e4");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph80, restartRequest, "m_Output", restartInputAnd, "a", "fa100514-10a1-402a-a2af-4dfd7199d101");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph80, restartWindow, "m_Output", restartInputAnd, "b", "d0e1f2a3-b4c5-4678-90d1-e2f3a4b5c6d8");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph80, restartInputAnd, "Value", restartAnd, "a", "d0e1f2a3-b4c5-4678-90d1-e2f3a4b5c6d9");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph80, restartAdmission, "m_Output", restartAnd, "b", "8466b626-c8e4-4cee-8d27-10be32d66fe3");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph80, restartAnd, "Value", restartResult, "m_Result", "26663c4d-1fda-45f6-ac70-b59619b6bdcd");
            BtsmtlSkillAuthoringContract.Apply(node385, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph83, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph82, node384, "Output", node385, "Input", "a0b1c2d3-e4f5-4678-90ab-c1d2e3f4a5b7");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph82, node385, "end", node386, "Input", "a0b1c2d3-e4f5-4678-90ab-c1d2e3f4a5b8");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph82, node385, "body", node387, "Input", "a0b1c2d3-e4f5-4678-90ab-c1d2e3f4a5b9");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph83, node390, "m_Output", node391, "a", "b0c1d2e3-f4a5-4789-0b1c-d2e3f4a5b6c8");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph83, node391, "Value", node392, "m_Result", "b0c1d2e3-f4a5-4789-0b1c-d2e3f4a5b6c9");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph69, node333, "Value", node334, "m_Result", "752c37c8-2fb8-538e-8298-e68436a6a302");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph84, recoveryRoot, "Output", recoverySelector, "Input", "883b1029-9949-4ebf-84c1-139a6f82870f");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph84, recoverySelector, "end", recoveryExit, "Input", "b214c48c-b29c-498b-bff0-212319a08617");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph84, recoverySelector, "body", recoverySet, "Input", "e019a94b-fd06-4908-a746-9053679b2714");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph85, recoveryTime, "m_Output", recoveryEnd, "a", "013a84ed-b065-4834-a9b6-ae9f4d10f130");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph85, recoveryEnd, "Value", recoveryResult, "m_Result", "83dca6a1-4cef-44d6-9769-a7f3163af4e1");
            return parts;
        }

        sealed class Attack5_EndParts
        {
            internal BtsmtlSkillFlowGraph graph69;
            internal BtsmtlSkillFlowGraph graph79;
            internal BtsmtlSkillFlowGraph graph80;
            internal BtsmtlSkillFlowGraph graph82;
            internal BtsmtlSkillFlowGraph graph83;
            internal BtsmtlSkillFlowGraph graph84;
            internal BtsmtlSkillFlowGraph graph85;
        }
    }
}
