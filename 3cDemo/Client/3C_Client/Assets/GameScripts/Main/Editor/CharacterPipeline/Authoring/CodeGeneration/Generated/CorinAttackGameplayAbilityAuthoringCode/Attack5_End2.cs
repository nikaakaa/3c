using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode
    {
        static Attack5_End2Parts BuildAttack5_End2(AttackParts attack, RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack5_End2Parts();
            parts.graph68 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "44049a03-c5d1-5f0c-996f-8b30eac669d3", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To End2 Condition");
            parts.graph71 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph70, "5f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5End2 ComboAccept");
            parts.graph72 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph71, "84b73b6e3d174c628e900074929eb369", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            parts.graph73 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph70, "6a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5End2 RecoveryEarly");
            parts.graph74 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph73, "5f6e2527702d46cba140c5187a3c5f2b", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            parts.graph75 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph70, "7b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5End2 RecoveryLate");
            parts.graph76 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph75, "0dce24c0c4294d40aeee4c8e7f11bd69", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            parts.graph77 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "a565bcba-7161-538b-b2d8-2a13f64a13ec", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 End2 To Exit Condition");
            parts.graph81 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "9eec80cd-0f7e-4cbd-9304-da36e994dc6e", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 End2 To Attack1 Condition");
            var node327 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph68, typeof(BtsmtlSkillBlackboardBooleanFlowNode), "1febc74b-4ede-5666-aee4-ad303b94795d", "Attack5EndBoundary", new Vector2(-520f, 0f));
            var node328 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph68, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "99ca0387-6f32-5706-951e-a6a54dbe8cfb", "AND", new Vector2(40f, 35f));
            var node329 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph68, typeof(BtsmtlSkillConditionResultFlowNode), "dbb12885-8f8e-5529-905b-0ba8c42d76b9", "条件结果", new Vector2(600f, 180f));
            var node330 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph68, typeof(BtsmtlSkillBlackboardBooleanFlowNode), "e8d7d0cc-49ba-5fc7-9e09-3d571a25534f", "Attack5Hit", new Vector2(-520f, 100f));
            var node343 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph71, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "5f8f743dda6d45f18ab84fb5527ed263", "结束片段", new Vector2(520f, 260f));
            var node346 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph71, typeof(BtsmtlSkillTimelineDestroyFlowNode), "a4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9", "片段销毁", new Vector2(120f, 660f));
            var node342 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph71, typeof(BtsmtlSkillSelectorFlowNode), "aaf2f083e5f14604bab3a4b40f8840ca", "窗口执行或结束", new Vector2(280f, 260f));
            var node341 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph71, typeof(BtsmtlSkillRootFlowNode), "c0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5", "技能入口", new Vector2(120f, 260f));
            var node344 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph71, typeof(BtsmtlSkillBlackboardSetFlowNode), "d1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6", "Set ComboAccept", new Vector2(320f, 0f));
            var node340 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph71, typeof(BtsmtlSkillTimelineEnableFlowNode), "e2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7", "片段启用", new Vector2(120f, 60f));
            var node345 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph71, typeof(BtsmtlSkillTimelineDisableFlowNode), "f3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8", "片段停用", new Vector2(120f, 460f));
            var node349 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph72, typeof(BtsmtlSkillConditionResultFlowNode), "1a87caf6-941a-400f-9af3-8f23b543082e", null, new Vector2(600f, 180f));
            var node347 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph72, typeof(BtsmtlSkillTimelineTimeFlowNode), "5ef7f3980cc14da79f9b75f0414999ab", "Timeline时间", new Vector2(-360f, 0f));
            var node348 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph72, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "aa1c2d71f54e48d7a82dac966c7bb178", "到达结束时间", new Vector2(-100f, 0f));
            var node352 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph73, typeof(BtsmtlSkillSelectorFlowNode), "7f73e7bfaf5e45798b43c7648eb3557e", "窗口执行或结束", new Vector2(280f, 260f));
            var node353 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph73, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "aafa8e03cc484e709fa94096a82493ce", "结束片段", new Vector2(520f, 260f));
            var node351 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph73, typeof(BtsmtlSkillRootFlowNode), "b5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0", "技能入口", new Vector2(120f, 260f));
            var node354 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph73, typeof(BtsmtlSkillBlackboardSetFlowNode), "c6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1", "Set RecoveryEarly", new Vector2(320f, 0f));
            var node350 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph73, typeof(BtsmtlSkillTimelineEnableFlowNode), "d7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2", "片段启用", new Vector2(120f, 60f));
            var node355 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph73, typeof(BtsmtlSkillTimelineDisableFlowNode), "e8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3", "片段停用", new Vector2(120f, 460f));
            var node356 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph73, typeof(BtsmtlSkillTimelineDestroyFlowNode), "f9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4", "片段销毁", new Vector2(120f, 660f));
            var node359 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph74, typeof(BtsmtlSkillConditionResultFlowNode), "0215b1e7-dd64-4b5b-b9a2-0c2469ccd0ae", null, new Vector2(600f, 180f));
            var node357 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph74, typeof(BtsmtlSkillTimelineTimeFlowNode), "6a9a4053c5ad4c559e260984847afbfa", "Timeline时间", new Vector2(-360f, 0f));
            var node358 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph74, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "fea94c3a77fe4b8bad9b22d2db132aa5", "到达结束时间", new Vector2(-100f, 0f));
            var node363 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph75, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "1933f010c86846f49cdbfffcb984da7a", "结束片段", new Vector2(520f, 260f));
            var node361 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph75, typeof(BtsmtlSkillRootFlowNode), "aae1f2a3b4c5d6e7f8a9b0c1d2e3f4a5", "技能入口", new Vector2(120f, 260f));
            var node362 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph75, typeof(BtsmtlSkillSelectorFlowNode), "aee82ba84ddd4078b56fb85cea0be53d", "窗口执行或结束", new Vector2(280f, 260f));
            var node364 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph75, typeof(BtsmtlSkillBlackboardSetFlowNode), "bbf2a3b4c5d6e7f8a9b0c1d2e3f4a5b6", "Set RecoveryLate", new Vector2(320f, 0f));
            var node360 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph75, typeof(BtsmtlSkillTimelineEnableFlowNode), "cca3b4c5d6e7f8a9b0c1d2e3f4a5b6c7", "片段启用", new Vector2(120f, 60f));
            var node365 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph75, typeof(BtsmtlSkillTimelineDisableFlowNode), "ddb4c5d6e7f8a9b0c1d2e3f4a5b6c7d8", "片段停用", new Vector2(120f, 460f));
            var node366 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph75, typeof(BtsmtlSkillTimelineDestroyFlowNode), "eec5d6e7f8a9b0c1d2e3f4a5b6c7d8e9", "片段销毁", new Vector2(120f, 660f));
            var node368 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph76, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "18178d88a0cb4c81a70980d7f1de8b23", "到达结束时间", new Vector2(-100f, 0f));
            var node367 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph76, typeof(BtsmtlSkillTimelineTimeFlowNode), "63b990aacef143ec80787ec5c3d42d44", "Timeline时间", new Vector2(-360f, 0f));
            var node369 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph76, typeof(BtsmtlSkillConditionResultFlowNode), "c600b5af-6a10-4887-baa9-2e797700268d", null, new Vector2(600f, 180f));
            var node371 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillConditionResultFlowNode), "bcb254f6-5401-5b88-9a2b-774ba8280d70", "条件结果", new Vector2(600f, 180f));
            var node370 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillStateRootCompletedFlowNode), "ff33b21c-6845-5b31-b4a7-84e6f00da514", "状态主体已完成", new Vector2(-360f, 0f));
            var moveInput = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillInputMagnitudeFlowNode), "b0755ba9-ed5a-4279-990c-21b01d9adc1e", "MoveAxis Magnitude", new Vector2(-520f, 120f));
            var moveThreshold = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillBlackboardScalarFlowNode), "c6d9d3f0-b186-4f42-b2ff-1e1dcef150ba", "StopThreshold", new Vector2(-520f, 200f));
            var moveGreater = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "5b8cf103-72f3-4946-8965-9925d4960391", ">", new Vector2(-200f, 160f));
            var moveWindow = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillActionWindowActiveFlowNode), "64f0de78-17ce-464b-b9f3-99dcd8db0645", "Window RecoveryLate", new Vector2(-360f, 300f));
            var moveAnd = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "ae10927f-8155-4759-8ebf-0c451d79f538", "AND", new Vector2(80f, 200f));
            var exitOr = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph77, typeof(BtsmtlSkillNativeNodeWrapper<OR>), "4c002849-30fe-4447-b21b-8eb6603da677", "OR", new Vector2(300f, 100f));
            var restartRequest = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph81, typeof(BtsmtlSkillActionRequestFlowNode), "30d1e56f-37ea-45aa-9396-f976b06405e0", "Has Attack Request", new Vector2(-520f, 0f));
            var restartWindow = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph81, typeof(BtsmtlSkillActionWindowActiveFlowNode), "c2db97c2-02f1-49cb-9d99-561392b68ddb", "Window ComboAccept", new Vector2(-520f, 100f));
            var restartAdmission = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph81, typeof(BtsmtlSkillCanActivateActionFlowNode), "2b79cec4-f48e-4d6b-ab87-90411a48165c", "Can Activate Attack", new Vector2(-520f, 200f));
            var restartInputAnd = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph81, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "959117fa-e0ec-41d0-b58b-b5e8823c24c9", "AND", new Vector2(-120f, 50f));
            var restartAdmissionAnd = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph81, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "b80e083c-be7e-46fa-844c-a23503cd11f6", "AND", new Vector2(200f, 80f));
            var restartResult = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph81, typeof(BtsmtlSkillConditionResultFlowNode), "e9121a2d-8fc6-4679-bf22-f87918506c33", "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringContract.Apply(node327, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "19735807-6d5f-5e7f-91c9-b841ccf1f71e"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(node330, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "6225401bc79441dca5eaab16bdbc0644"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(moveInput, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "MoveAxis"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(moveThreshold, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "1edc27e65f454837b415895f4b808048"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(moveWindow, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RecoveryLate") });
            BtsmtlSkillAuthoringContract.Apply(restartRequest, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "Attack"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(restartWindow, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "ComboAccept") });
            BtsmtlSkillAuthoringContract.Apply(restartAdmission, new[] { new BtsmtlSkillAuthoringFieldValue("admissionProfile", rootParts.asset20), new BtsmtlSkillAuthoringFieldValue("targetSnapshot", new BtsmtlSkillTargetSnapshotReference("b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933")) });
            BtsmtlSkillAuthoringContract.Apply(node342, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph72, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringContract.Apply(node344, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "e4c9e1f6a3d0578c2e6f9a0b1c2d3e4f"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "e78e2907-7a90-52e7-920c-c13d40a350ba"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node344, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(node348, "b", 3.08333349f);
            BtsmtlSkillAuthoringContract.Apply(node352, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph74, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringContract.Apply(node354, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "e2a7c9d4f1b8356a0c4d7e8f9a0b1c2d"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "e78e2907-7a90-52e7-920c-c13d40a350ba"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node354, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(node358, "b", 3.08333349f);
            BtsmtlSkillAuthoringContract.Apply(node362, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph76, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringContract.Apply(node364, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "e3b8d0e5f2c9467b1d5e8f9a0b1c2d3"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "e78e2907-7a90-52e7-920c-c13d40a350ba"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node364, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(node368, "b", 3.08333349f);
            var edge187 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph68, node327, "m_Output", node328, "a", "37458f6e-7c14-5b56-a363-0431f6ef1ef9");
            var edge188 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph68, node328, "Value", node329, "m_Result", "5320d02d-70cf-5cd7-9a0d-42e20ea12b41");
            var edge189 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph68, node330, "m_Output", node328, "b", "e45db7d5-5b0c-52a2-9f22-39225b128b9e");
            var edge196 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph71, node341, "Output", node342, "Input", "b74161aeed3d4334b9b82348da30ed25");
            var edge197 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph71, node342, "end", node343, "Input", "76dd5d190a98496aba3a933c25561ccc");
            var edge198 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph71, node342, "body", node344, "Input", "a7cb0e8dc35e4cf0876229e9412922ae");
            var edge199 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph72, node347, "m_Output", node348, "a", "d75805bcf16a42279608b1d39d40318f");
            var edge200 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph72, node348, "Value", node349, "m_Result", "c41bb2f446a04d88a866ea8e6ce4e651");
            var edge201 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph73, node351, "Output", node352, "Input", "43a2aee0c3ff4bc69e74dbd9408b258f");
            var edge202 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph73, node352, "end", node353, "Input", "391a6a898a1c41b09361fa0e81f71a0f");
            var edge203 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph73, node352, "body", node354, "Input", "5222ed654ddf454aa28cc5a2cbba4b3d");
            var edge204 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph74, node357, "m_Output", node358, "a", "d61f51d7dfc1485ea968197774cd10d8");
            var edge205 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph74, node358, "Value", node359, "m_Result", "e904967c61214501846dafb7ae2c9b7b");
            var edge206 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph75, node361, "Output", node362, "Input", "2f2a66fc79144f2c87c6ee90b3c35d39");
            var edge208 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph75, node362, "body", node364, "Input", "551803754bcc49c49436f52a4bb95c27");
            var edge207 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph75, node362, "end", node363, "Input", "a52534411de94301842ed592a88436c7");
            var edge209 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph76, node367, "m_Output", node368, "a", "9e2679686f8348b8b21e070997941fe7");
            var edge210 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph76, node368, "Value", node369, "m_Result", "91a80aee6ac040eeaa3d5bd2d13c1723");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph77, moveInput, "m_Output", moveGreater, "a", "a9c5da9c-0844-4552-9e9f-15ef945e5698");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph77, moveThreshold, "m_Output", moveGreater, "b", "69a5be98-3737-4b4c-bc94-187c33d7a984");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph77, moveGreater, "Value", moveAnd, "a", "9867dd6b-627f-4b87-b1bc-e549ba979a42");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph77, moveWindow, "m_Output", moveAnd, "b", "c0453ccc-ad7e-4c22-b219-834bd4f89e21");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph77, moveAnd, "Value", exitOr, "a", "8ab6c3ed-2ff2-4124-a260-c517ee3631ee");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph77, node370, "m_Output", exitOr, "b", "058d8598-dfc5-4441-b1d8-59746d06a3ae");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph77, exitOr, "Value", node371, "m_Result", "ae717d21-6482-58da-bb1e-8f139e9a71a6");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph81, restartRequest, "m_Output", restartInputAnd, "a", "e7fe2a06-f499-4b91-b684-13172dee8750");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph81, restartWindow, "m_Output", restartInputAnd, "b", "5de5d1c1-b2da-492e-9bc7-832885895e33");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph81, restartInputAnd, "Value", restartAdmissionAnd, "a", "ea152e25-85ad-4864-9fc8-5c8b7e092456");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph81, restartAdmission, "m_Output", restartAdmissionAnd, "b", "713bab30-153e-4448-9265-171354396ff1");
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph81, restartAdmissionAnd, "Value", restartResult, "m_Result", "5d2f7e36-9468-44ad-b08a-88a5c9c26761");
            return parts;
        }

        sealed class Attack5_End2Parts
        {
            internal BtsmtlSkillFlowGraph graph68;
            internal BtsmtlSkillFlowGraph graph71;
            internal BtsmtlSkillFlowGraph graph72;
            internal BtsmtlSkillFlowGraph graph73;
            internal BtsmtlSkillFlowGraph graph74;
            internal BtsmtlSkillFlowGraph graph75;
            internal BtsmtlSkillFlowGraph graph76;
            internal BtsmtlSkillFlowGraph graph77;
            internal BtsmtlSkillFlowGraph graph81;
        }
    }
}
