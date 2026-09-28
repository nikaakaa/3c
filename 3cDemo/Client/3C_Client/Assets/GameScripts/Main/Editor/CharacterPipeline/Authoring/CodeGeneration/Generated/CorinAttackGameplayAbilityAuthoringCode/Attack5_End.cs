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
            parts.graph71 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "9c3889ef21ffe74ea03e03a8c95d1f1e", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To Exit Condition");
            parts.graph74 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "b67c56ac-a560-55d6-965d-80641f9975b0", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To End Condition");
            parts.graph77 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph76, "a0b1c2d3-e4f5-4678-90ab-c1d2e3f4a5b6", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5End ComboAccept");
            parts.graph78 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph77, "b1c2d3e4-f5a6-4789-0abc-d2e3f4a5b6c7", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            parts.graph79 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph76, "6ea21a0a-7b3d-4f35-97a8-6357ec498c0c", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5End RecoveryLate");
            parts.graph80 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph79, "2e384ba0-9c3d-4bd8-859d-80871596dfca", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            parts.graph81 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "3d3c1c9d-4f1f-536f-9ec1-8c8bb2eb0d20", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 End To Exit Condition");
            var node341 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph71, typeof(BtsmtlSkillConditionResultFlowNode), "b34b38c9-c654-47b8-8556-d295201d0e29", "条件结果", new Vector2(600f, 180f));
            var node340 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph71, typeof(BtsmtlSkillStateRootCompletedFlowNode), "c66123e9-2ff9-453f-8a8a-7f05d3788926", "状态主体已完成", new Vector2(-360f, 0f));
            var node356 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph74, typeof(BtsmtlSkillNativeNodeWrapper<NOT>), "695bf217-15b0-5d96-8f98-6511027930f7", "NOT", new Vector2(-240f, 100f));
            var node353 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph74, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "963a8c27-8799-51a5-84b6-47da9490f275", "AND", new Vector2(220f, 70f));
            var node352 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph74, typeof(BtsmtlSkillBlackboardBooleanFlowNode), "c493a942-5634-56c6-a222-a1e50daf6747", "Attack5EndBoundary", new Vector2(-520f, 0f));
            var node354 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph74, typeof(BtsmtlSkillConditionResultFlowNode), "e45652d9-a7af-5814-9ed2-b083c6a02d53", "条件结果", new Vector2(600f, 180f));
            var node355 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph74, typeof(BtsmtlSkillBlackboardBooleanFlowNode), "f4c7bb30-1ea2-4f81-a302-5cbb9cc95241", "Attack5Hit", new Vector2(-520f, 100f));
            var node371 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillBlackboardSetFlowNode), "a4b5c6d7-e8f9-401a-b2c3-4d5e6f708192", "Set ComboAccept", new Vector2(320f, 0f));
            var node372 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillTimelineDisableFlowNode), "b5c6d7e8-f9a0-412b-c3d4-5e6f708192a3", "片段停用", new Vector2(120f, 460f));
            var node367 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillTimelineEnableFlowNode), "c0d1e2f3-a4b5-4c6d-8e7f-901a2b3c4d5e", "片段启用", new Vector2(120f, 60f));
            var node373 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillTimelineDestroyFlowNode), "c6d7e8f9-a0b1-423c-d4e5-6f708192a3b4", "片段销毁", new Vector2(120f, 660f));
            var node368 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillRootFlowNode), "d1e2f3a4-b5c6-4d7e-8f90-1a2b3c4d5e6f", "技能入口", new Vector2(120f, 260f));
            var node369 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillSelectorFlowNode), "e2f3a4b5-c6d7-4e8f-901a-2b3c4d5e6f70", "窗口执行或结束", new Vector2(280f, 260f));
            var node370 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "f3a4b5c6-d7e8-4f90-a1b2-3c4d5e6f7081", "结束片段", new Vector2(520f, 260f));
            var node374 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph78, typeof(BtsmtlSkillTimelineTimeFlowNode), "d7e8f9a0-b1c2-434d-e5f6-708192a3b4c5", "Timeline时间", new Vector2(-360f, 0f));
            var node375 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph78, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "e8f9a0b1-c2d3-445e-f607-8192a3b4c5d6", "到达结束时间", new Vector2(-100f, 0f));
            var node376 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph78, typeof(BtsmtlSkillConditionResultFlowNode), "f9a0b1c2-d3e4-456f-0781-92a3b4c5d6e7", null, new Vector2(600f, 180f));
            var node379 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillSelectorFlowNode), "183f3ef9-74f0-4df0-ae38-e3548639535e", "窗口执行或结束", new Vector2(280f, 260f));
            var node382 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillTimelineDisableFlowNode), "27980712-074e-48b1-ae59-b2a930113bdf", "片段停用", new Vector2(120f, 460f));
            var node383 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillTimelineDestroyFlowNode), "6663a695-f9f8-4e9a-9cb9-e176bcde4910", "片段销毁", new Vector2(120f, 660f));
            var node381 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillBlackboardSetFlowNode), "72255436-dcce-4b0c-b180-35740da61b64", "Set RecoveryLate", new Vector2(320f, 0f));
            var node378 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillRootFlowNode), "88e09063-6d44-4ce7-8365-a97c31aee6f6", "技能入口", new Vector2(120f, 260f));
            var node380 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "aa003de9-7c8d-4b62-8f07-76438976724a", "结束片段", new Vector2(520f, 260f));
            var node377 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph79, typeof(BtsmtlSkillTimelineEnableFlowNode), "d50b74c8-219f-4c84-a0bf-301971d7c56a", "片段启用", new Vector2(120f, 60f));
            var node386 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph80, typeof(BtsmtlSkillConditionResultFlowNode), "0981e23a-1f05-4d4e-9063-bbcad84b7374", null, new Vector2(600f, 180f));
            var node385 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph80, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "9d72f768-af50-420a-9880-329aa9e6a6c2", "到达结束时间", new Vector2(-100f, 0f));
            var node384 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph80, typeof(BtsmtlSkillTimelineTimeFlowNode), "c1e88c2d-5e49-4b26-8754-9245d947360e", "Timeline时间", new Vector2(-360f, 0f));
            var node390 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph81, typeof(BtsmtlSkillInputMagnitudeFlowNode), "1ba5bdc5-e84c-4df8-8e8b-c3d147b13cfa", "MoveAxis Magnitude", new Vector2(-520f, 120f));
            var node389 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph81, typeof(BtsmtlSkillConditionResultFlowNode), "44f4b2cb-7234-5f01-95f3-f33985178fc3", "条件结果", new Vector2(600f, 180f));
            var node387 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph81, typeof(BtsmtlSkillStateRootCompletedFlowNode), "7660be45-c0c4-5f38-94de-6644dbaf579d", "状态主体已完成", new Vector2(-360f, 0f));
            var node394 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph81, typeof(BtsmtlSkillActionWindowActiveFlowNode), "7da3ef23-5658-43ee-a3da-b894a722c33f", "Window RecoveryLate", new Vector2(-240f, 280f));
            var node388 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph81, typeof(BtsmtlSkillNativeNodeWrapper<OR>), "883689c1-6ff9-4988-b1c9-963d3a844374", "OR", new Vector2(200f, 80f));
            var node392 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph81, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "92df36ad-dd12-4f96-a411-4032e7358a4d", "AND", new Vector2(0f, 200f));
            var node393 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph81, typeof(BtsmtlSkillBlackboardScalarFlowNode), "9c954001-01d8-4c25-b19f-bee54ef26be8", "StopThreshold", new Vector2(-520f, 200f));
            var node391 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph81, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "c34e1ccf-e104-4fff-aab1-aa5594a2350c", ">", new Vector2(-200f, 160f));
            BtsmtlSkillAuthoringContract.Apply(node352, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "19735807-6d5f-5e7f-91c9-b841ccf1f71e"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(node355, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "6225401bc79441dca5eaab16bdbc0644"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(node371, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "f4a6b8c0-d2e4-4618-a0b2-c4d6e8f0a2b4"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "f6145a5e-1e4e-51a4-aee5-9c71a99ac525"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node371, "m_Value", true);
            BtsmtlSkillAuthoringContract.Apply(node369, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph78, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringCode.SetValue(node375, "b", 2.01666665f);
            BtsmtlSkillAuthoringContract.Apply(node379, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph80, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringContract.Apply(node381, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "9e109c07-120e-43b3-a05b-ce4370830cab"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "f6145a5e-1e4e-51a4-aee5-9c71a99ac525"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node381, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(node385, "b", 2.01666665f);
            BtsmtlSkillAuthoringContract.Apply(node390, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "MoveAxis"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node394, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RecoveryLate") });
            BtsmtlSkillAuthoringContract.Apply(node393, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "1edc27e65f454837b415895f4b808048"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            var edge183 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph71, node340, "m_Output", node341, "m_Result", "d4923ad8-fb95-4a2b-9e78-b3ad6a2913b4");
            var edge192 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph74, node352, "m_Output", node353, "a", "11c22c5a-2794-5cf8-a311-95a5530e6a29");
            var edge193 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph74, node353, "Value", node354, "m_Result", "752c37c8-2fb8-538e-8298-e68436a6a302");
            var edge194 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph74, node355, "m_Output", node356, "value", "a24f5206-8852-5bd8-a293-2b3e18c43683");
            var edge195 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph74, node356, "Value", node353, "b", "0f2a0964-87d8-52f7-8a6a-11d386d0215e");
            var edge202 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph77, node368, "Output", node369, "Input", "a0b1c2d3-e4f5-4678-90ab-c1d2e3f4a5b7");
            var edge203 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph77, node369, "end", node370, "Input", "a0b1c2d3-e4f5-4678-90ab-c1d2e3f4a5b8");
            var edge204 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph77, node369, "body", node371, "Input", "a0b1c2d3-e4f5-4678-90ab-c1d2e3f4a5b9");
            var edge205 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph78, node374, "m_Output", node375, "a", "b0c1d2e3-f4a5-4789-0b1c-d2e3f4a5b6c8");
            var edge206 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph78, node375, "Value", node376, "m_Result", "b0c1d2e3-f4a5-4789-0b1c-d2e3f4a5b6c9");
            var edge207 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, node378, "Output", node379, "Input", "883b1029-9949-4ebf-84c1-139a6f82870f");
            var edge208 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, node379, "end", node380, "Input", "b214c48c-b29c-498b-bff0-212319a08617");
            var edge209 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph79, node379, "body", node381, "Input", "e019a94b-fd06-4908-a746-9053679b2714");
            var edge210 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph80, node384, "m_Output", node385, "a", "013a84ed-b065-4834-a9b6-ae9f4d10f130");
            var edge211 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph80, node385, "Value", node386, "m_Result", "83dca6a1-4cef-44d6-9769-a7f3163af4e1");
            var edge212 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph81, node387, "m_Output", node388, "b", "7e4c7c21-f1cc-410d-b865-259e2caf76f2");
            var edge213 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph81, node388, "Value", node389, "m_Result", "9181a9d0-8f52-5928-9542-84c91a34b1e4");
            var edge214 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph81, node390, "m_Output", node391, "a", "974b25f0-f7ee-4183-aa94-94fb43b91b24");
            var edge215 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph81, node391, "Value", node392, "a", "ceb87808-e3bc-4fe6-b80e-6e8a0da79169");
            var edge216 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph81, node392, "Value", node388, "a", "53c8b7d4-585d-421e-85d6-b0cccc35fecc");
            var edge217 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph81, node393, "m_Output", node391, "b", "7c82a228-013d-4680-aa14-275d36b2911c");
            var edge218 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph81, node394, "m_Output", node392, "b", "8ef97460-a139-4d5a-a111-7dc34889eb5b");
            return parts;
        }

        sealed class Attack5_EndParts
        {
            internal BtsmtlSkillFlowGraph graph71;
            internal BtsmtlSkillFlowGraph graph74;
            internal BtsmtlSkillFlowGraph graph77;
            internal BtsmtlSkillFlowGraph graph78;
            internal BtsmtlSkillFlowGraph graph79;
            internal BtsmtlSkillFlowGraph graph80;
            internal BtsmtlSkillFlowGraph graph81;
        }
    }
}
