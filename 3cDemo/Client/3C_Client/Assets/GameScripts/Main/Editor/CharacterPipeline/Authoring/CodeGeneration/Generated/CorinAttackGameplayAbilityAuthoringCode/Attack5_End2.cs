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
            parts.graph73 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "44049a03-c5d1-5f0c-996f-8b30eac669d3", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To End2 Condition");
            parts.graph83 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph82, "5f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5End2 ComboAccept");
            parts.graph84 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph83, "84b73b6e3d174c628e900074929eb369", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            parts.graph85 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph82, "6a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5End2 RecoveryEarly");
            parts.graph86 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph85, "5f6e2527702d46cba140c5187a3c5f2b", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            parts.graph87 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph82, "7b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5End2 RecoveryLate");
            parts.graph88 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(parts.graph87, "0dce24c0c4294d40aeee4c8e7f11bd69", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Timeline达到终止边界");
            parts.graph89 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph82, "1f3150b2-7fa0-97e6-c7b3-769249fca694", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Corin_Attack_Normal_05_CamShake_E_03");
            parts.graph90 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "a565bcba-7161-538b-b2d8-2a13f64a13ec", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 End2 To Exit Condition");
            var node348 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph73, typeof(BtsmtlSkillBlackboardBooleanFlowNode), "1febc74b-4ede-5666-aee4-ad303b94795d", "Attack5EndBoundary", new Vector2(-520f, 0f));
            var node349 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph73, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "99ca0387-6f32-5706-951e-a6a54dbe8cfb", "AND", new Vector2(40f, 35f));
            var node350 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph73, typeof(BtsmtlSkillConditionResultFlowNode), "dbb12885-8f8e-5529-905b-0ba8c42d76b9", "条件结果", new Vector2(600f, 180f));
            var node351 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph73, typeof(BtsmtlSkillBlackboardBooleanFlowNode), "e8d7d0cc-49ba-5fc7-9e09-3d571a25534f", "Attack5Hit", new Vector2(-520f, 100f));
            var node402 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph83, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "5f8f743dda6d45f18ab84fb5527ed263", "结束片段", new Vector2(520f, 260f));
            var node405 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph83, typeof(BtsmtlSkillTimelineDestroyFlowNode), "a4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9", "片段销毁", new Vector2(120f, 660f));
            var node401 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph83, typeof(BtsmtlSkillSelectorFlowNode), "aaf2f083e5f14604bab3a4b40f8840ca", "窗口执行或结束", new Vector2(280f, 260f));
            var node400 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph83, typeof(BtsmtlSkillRootFlowNode), "c0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5", "技能入口", new Vector2(120f, 260f));
            var node403 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph83, typeof(BtsmtlSkillBlackboardSetFlowNode), "d1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6", "Set ComboAccept", new Vector2(320f, 0f));
            var node399 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph83, typeof(BtsmtlSkillTimelineEnableFlowNode), "e2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7", "片段启用", new Vector2(120f, 60f));
            var node404 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph83, typeof(BtsmtlSkillTimelineDisableFlowNode), "f3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8", "片段停用", new Vector2(120f, 460f));
            var node408 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph84, typeof(BtsmtlSkillConditionResultFlowNode), "1a87caf6-941a-400f-9af3-8f23b543082e", null, new Vector2(600f, 180f));
            var node406 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph84, typeof(BtsmtlSkillTimelineTimeFlowNode), "5ef7f3980cc14da79f9b75f0414999ab", "Timeline时间", new Vector2(-360f, 0f));
            var node407 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph84, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "aa1c2d71f54e48d7a82dac966c7bb178", "到达结束时间", new Vector2(-100f, 0f));
            var node411 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph85, typeof(BtsmtlSkillSelectorFlowNode), "7f73e7bfaf5e45798b43c7648eb3557e", "窗口执行或结束", new Vector2(280f, 260f));
            var node412 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph85, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "aafa8e03cc484e709fa94096a82493ce", "结束片段", new Vector2(520f, 260f));
            var node410 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph85, typeof(BtsmtlSkillRootFlowNode), "b5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0", "技能入口", new Vector2(120f, 260f));
            var node413 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph85, typeof(BtsmtlSkillBlackboardSetFlowNode), "c6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1", "Set RecoveryEarly", new Vector2(320f, 0f));
            var node409 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph85, typeof(BtsmtlSkillTimelineEnableFlowNode), "d7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2", "片段启用", new Vector2(120f, 60f));
            var node414 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph85, typeof(BtsmtlSkillTimelineDisableFlowNode), "e8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3", "片段停用", new Vector2(120f, 460f));
            var node415 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph85, typeof(BtsmtlSkillTimelineDestroyFlowNode), "f9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4", "片段销毁", new Vector2(120f, 660f));
            var node418 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph86, typeof(BtsmtlSkillConditionResultFlowNode), "0215b1e7-dd64-4b5b-b9a2-0c2469ccd0ae", null, new Vector2(600f, 180f));
            var node416 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph86, typeof(BtsmtlSkillTimelineTimeFlowNode), "6a9a4053c5ad4c559e260984847afbfa", "Timeline时间", new Vector2(-360f, 0f));
            var node417 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph86, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "fea94c3a77fe4b8bad9b22d2db132aa5", "到达结束时间", new Vector2(-100f, 0f));
            var node422 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph87, typeof(BtsmtlSkillTimelineExitRequestFlowNode), "1933f010c86846f49cdbfffcb984da7a", "结束片段", new Vector2(520f, 260f));
            var node420 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph87, typeof(BtsmtlSkillRootFlowNode), "aae1f2a3b4c5d6e7f8a9b0c1d2e3f4a5", "技能入口", new Vector2(120f, 260f));
            var node421 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph87, typeof(BtsmtlSkillSelectorFlowNode), "aee82ba84ddd4078b56fb85cea0be53d", "窗口执行或结束", new Vector2(280f, 260f));
            var node423 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph87, typeof(BtsmtlSkillBlackboardSetFlowNode), "bbf2a3b4c5d6e7f8a9b0c1d2e3f4a5b6", "Set RecoveryLate", new Vector2(320f, 0f));
            var node419 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph87, typeof(BtsmtlSkillTimelineEnableFlowNode), "cca3b4c5d6e7f8a9b0c1d2e3f4a5b6c7", "片段启用", new Vector2(120f, 60f));
            var node424 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph87, typeof(BtsmtlSkillTimelineDisableFlowNode), "ddb4c5d6e7f8a9b0c1d2e3f4a5b6c7d8", "片段停用", new Vector2(120f, 460f));
            var node425 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph87, typeof(BtsmtlSkillTimelineDestroyFlowNode), "eec5d6e7f8a9b0c1d2e3f4a5b6c7d8e9", "片段销毁", new Vector2(120f, 660f));
            var node427 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph88, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterEqualThan>), "18178d88a0cb4c81a70980d7f1de8b23", "到达结束时间", new Vector2(-100f, 0f));
            var node426 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph88, typeof(BtsmtlSkillTimelineTimeFlowNode), "63b990aacef143ec80787ec5c3d42d44", "Timeline时间", new Vector2(-360f, 0f));
            var node428 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph88, typeof(BtsmtlSkillConditionResultFlowNode), "c600b5af-6a10-4887-baa9-2e797700268d", null, new Vector2(600f, 180f));
            var node430 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph89, typeof(RequestCameraEffectNode), "2e53bdce-1d1e-2e93-b85f-4479dd97a77e", "Corin_Attack_Normal_05_CamShake_E_03", new Vector2(220f, 0f));
            var node432 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph89, typeof(BtsmtlSkillTimelineDisableFlowNode), "52184061-40e2-480f-b502-cfee60cb9da6", null, new Vector2(120f, 460f));
            var node433 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph89, typeof(BtsmtlSkillTimelineDestroyFlowNode), "6d4464d6-3d61-4c3c-ad11-ed5b85493470", null, new Vector2(120f, 660f));
            var node429 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph89, typeof(BtsmtlSkillTimelineEnableFlowNode), "7efdc5e4-6eb5-05a2-98a9-faa31316b1ff", "片段启用", new Vector2(0f, 100f));
            var node431 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph89, typeof(BtsmtlSkillRootFlowNode), "93ffe130-86e5-c4e2-8f36-fc2ca242d682", "持续执行", new Vector2(0f, 0f));
            var node435 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph90, typeof(BtsmtlSkillNativeNodeWrapper<OR>), "4c002849-30fe-4447-b21b-8eb6603da677", "OR", new Vector2(300f, 100f));
            var node438 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph90, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "5b8cf103-72f3-4946-8965-9925d4960391", ">", new Vector2(-200f, 160f));
            var node441 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph90, typeof(BtsmtlSkillActionWindowActiveFlowNode), "64f0de78-17ce-464b-b9f3-99dcd8db0645", "Window RecoveryLate", new Vector2(-360f, 300f));
            var node439 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph90, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "ae10927f-8155-4759-8ebf-0c451d79f538", "AND", new Vector2(80f, 200f));
            var node437 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph90, typeof(BtsmtlSkillInputMagnitudeFlowNode), "b0755ba9-ed5a-4279-990c-21b01d9adc1e", "MoveAxis Magnitude", new Vector2(-520f, 120f));
            var node436 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph90, typeof(BtsmtlSkillConditionResultFlowNode), "bcb254f6-5401-5b88-9a2b-774ba8280d70", "条件结果", new Vector2(600f, 180f));
            var node440 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph90, typeof(BtsmtlSkillBlackboardScalarFlowNode), "c6d9d3f0-b186-4f42-b2ff-1e1dcef150ba", "StopThreshold", new Vector2(-520f, 200f));
            var node434 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph90, typeof(BtsmtlSkillStateRootCompletedFlowNode), "ff33b21c-6845-5b31-b4a7-84e6f00da514", "状态主体已完成", new Vector2(-360f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node348, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "19735807-6d5f-5e7f-91c9-b841ccf1f71e"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(node351, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "6225401bc79441dca5eaab16bdbc0644"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(node401, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph84, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringContract.Apply(node403, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "e4c9e1f6a3d0578c2e6f9a0b1c2d3e4f"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "e78e2907-7a90-52e7-920c-c13d40a350ba"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node403, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(node407, "b", 3.08333349f);
            BtsmtlSkillAuthoringContract.Apply(node411, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph86, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringContract.Apply(node413, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "e2a7c9d4f1b8356a0c4d7e8f9a0b1c2d"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "e78e2907-7a90-52e7-920c-c13d40a350ba"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node413, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(node417, "b", 3.08333349f);
            BtsmtlSkillAuthoringContract.Apply(node421, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("end", "结束", parts.graph88, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("body", "执行", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringContract.Apply(node423, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "e3b8d0e5f2c9467b1d5e8f9a0b1c2d3"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "e78e2907-7a90-52e7-920c-c13d40a350ba"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node423, "m_Value", true);
            BtsmtlSkillAuthoringCode.SetValue(node427, "b", 3.08333349f);
            BtsmtlSkillAuthoringContract.Apply(node430, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("requestId", "852f9df220cc4571b4ce70745d4b8343"), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Normal_05_CamShake_E_03") });
            BtsmtlSkillAuthoringContract.Apply(node441, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RecoveryLate") });
            BtsmtlSkillAuthoringContract.Apply(node437, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "MoveAxis"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node440, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "1edc27e65f454837b415895f4b808048"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            var edge189 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph73, node348, "m_Output", node349, "a", "37458f6e-7c14-5b56-a363-0431f6ef1ef9");
            var edge190 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph73, node349, "Value", node350, "m_Result", "5320d02d-70cf-5cd7-9a0d-42e20ea12b41");
            var edge191 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph73, node351, "m_Output", node349, "b", "e45db7d5-5b0c-52a2-9f22-39225b128b9e");
            var edge220 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph83, node400, "Output", node401, "Input", "b74161aeed3d4334b9b82348da30ed25");
            var edge221 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph83, node401, "end", node402, "Input", "76dd5d190a98496aba3a933c25561ccc");
            var edge222 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph83, node401, "body", node403, "Input", "a7cb0e8dc35e4cf0876229e9412922ae");
            var edge223 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph84, node406, "m_Output", node407, "a", "d75805bcf16a42279608b1d39d40318f");
            var edge224 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph84, node407, "Value", node408, "m_Result", "c41bb2f446a04d88a866ea8e6ce4e651");
            var edge225 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph85, node410, "Output", node411, "Input", "43a2aee0c3ff4bc69e74dbd9408b258f");
            var edge226 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph85, node411, "end", node412, "Input", "391a6a898a1c41b09361fa0e81f71a0f");
            var edge227 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph85, node411, "body", node413, "Input", "5222ed654ddf454aa28cc5a2cbba4b3d");
            var edge228 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph86, node416, "m_Output", node417, "a", "d61f51d7dfc1485ea968197774cd10d8");
            var edge229 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph86, node417, "Value", node418, "m_Result", "e904967c61214501846dafb7ae2c9b7b");
            var edge230 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph87, node420, "Output", node421, "Input", "2f2a66fc79144f2c87c6ee90b3c35d39");
            var edge232 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph87, node421, "body", node423, "Input", "551803754bcc49c49436f52a4bb95c27");
            var edge231 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph87, node421, "end", node422, "Input", "a52534411de94301842ed592a88436c7");
            var edge233 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph88, node426, "m_Output", node427, "a", "9e2679686f8348b8b21e070997941fe7");
            var edge234 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph88, node427, "Value", node428, "m_Result", "91a80aee6ac040eeaa3d5bd2d13c1723");
            var edge235 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph89, node429, "Output", node430, "Input", "5d38a2fc-7e64-8f80-3080-3940e6ce1491");
            var edge236 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph90, node434, "m_Output", node435, "b", "058d8598-dfc5-4441-b1d8-59746d06a3ae");
            var edge237 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph90, node435, "Value", node436, "m_Result", "ae717d21-6482-58da-bb1e-8f139e9a71a6");
            var edge238 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph90, node437, "m_Output", node438, "a", "a9c5da9c-0844-4552-9e9f-15ef945e5698");
            var edge239 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph90, node438, "Value", node439, "a", "9867dd6b-627f-4b87-b1bc-e549ba979a42");
            var edge240 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph90, node439, "Value", node435, "a", "8ab6c3ed-2ff2-4124-a260-c517ee3631ee");
            var edge241 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph90, node440, "m_Output", node438, "b", "69a5be98-3737-4b4c-bc94-187c33d7a984");
            var edge242 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph90, node441, "m_Output", node439, "b", "c0453ccc-ad7e-4c22-b219-834bd4f89e21");
            return parts;
        }

        sealed class Attack5_End2Parts
        {
            internal BtsmtlSkillFlowGraph graph73;
            internal BtsmtlSkillFlowGraph graph83;
            internal BtsmtlSkillFlowGraph graph84;
            internal BtsmtlSkillFlowGraph graph85;
            internal BtsmtlSkillFlowGraph graph86;
            internal BtsmtlSkillFlowGraph graph87;
            internal BtsmtlSkillFlowGraph graph88;
            internal BtsmtlSkillFlowGraph graph89;
            internal BtsmtlSkillFlowGraph graph90;
        }
    }
}
