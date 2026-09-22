using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode
    {
        static Attack5_End2Parts BuildAttack5_End2(AttackParts attack, RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack5_End2Parts();
            parts.graph46 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "44049a03-c5d1-5f0c-996f-8b30eac669d3", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 To End2 Condition");
            parts.graph49 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph48, "5f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5End2 ComboAccept");
            parts.graph50 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph48, "6a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5End2 RecoveryEarly");
            parts.graph51 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph48, "7b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack5End2 RecoveryLate");
            parts.graph52 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "a565bcba-7161-538b-b2d8-2a13f64a13ec", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack5 End2 To Exit Condition");
            var node222 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph46, typeof(BtsmtlSkillBlackboardBooleanFlowNode), "1febc74b-4ede-5666-aee4-ad303b94795d", "Attack5EndBoundary", new Vector2(-520f, 0f));
            var node223 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph46, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "99ca0387-6f32-5706-951e-a6a54dbe8cfb", "AND", new Vector2(40f, 35f));
            var node224 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph46, typeof(BtsmtlSkillConditionResultFlowNode), "dbb12885-8f8e-5529-905b-0ba8c42d76b9", "条件结果", new Vector2(600f, 180f));
            var node225 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph46, typeof(BtsmtlSkillBlackboardBooleanFlowNode), "e8d7d0cc-49ba-5fc7-9e09-3d571a25534f", "Attack5Hit", new Vector2(-520f, 100f));
            var node239 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph49, typeof(BtsmtlSkillTimelineDestroyFlowNode), "a4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9", "片段销毁", new Vector2(120f, 660f));
            var node236 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph49, typeof(BtsmtlSkillRootFlowNode), "c0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5", "技能入口", new Vector2(120f, 260f));
            var node237 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph49, typeof(BtsmtlSkillBlackboardSetFlowNode), "d1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6", "Set ComboAccept", new Vector2(320f, 0f));
            var node235 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph49, typeof(BtsmtlSkillTimelineEnableFlowNode), "e2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7", "片段启用", new Vector2(120f, 60f));
            var node238 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph49, typeof(BtsmtlSkillTimelineDisableFlowNode), "f3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8", "片段停用", new Vector2(120f, 460f));
            var node241 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph50, typeof(BtsmtlSkillRootFlowNode), "b5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0", "技能入口", new Vector2(120f, 260f));
            var node242 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph50, typeof(BtsmtlSkillBlackboardSetFlowNode), "c6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1", "Set RecoveryEarly", new Vector2(320f, 0f));
            var node240 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph50, typeof(BtsmtlSkillTimelineEnableFlowNode), "d7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2", "片段启用", new Vector2(120f, 60f));
            var node243 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph50, typeof(BtsmtlSkillTimelineDisableFlowNode), "e8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3", "片段停用", new Vector2(120f, 460f));
            var node244 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph50, typeof(BtsmtlSkillTimelineDestroyFlowNode), "f9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4", "片段销毁", new Vector2(120f, 660f));
            var node246 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph51, typeof(BtsmtlSkillRootFlowNode), "aae1f2a3b4c5d6e7f8a9b0c1d2e3f4a5", "技能入口", new Vector2(120f, 260f));
            var node247 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph51, typeof(BtsmtlSkillBlackboardSetFlowNode), "bbf2a3b4c5d6e7f8a9b0c1d2e3f4a5b6", "Set RecoveryLate", new Vector2(320f, 0f));
            var node245 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph51, typeof(BtsmtlSkillTimelineEnableFlowNode), "cca3b4c5d6e7f8a9b0c1d2e3f4a5b6c7", "片段启用", new Vector2(120f, 60f));
            var node248 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph51, typeof(BtsmtlSkillTimelineDisableFlowNode), "ddb4c5d6e7f8a9b0c1d2e3f4a5b6c7d8", "片段停用", new Vector2(120f, 460f));
            var node249 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph51, typeof(BtsmtlSkillTimelineDestroyFlowNode), "eec5d6e7f8a9b0c1d2e3f4a5b6c7d8e9", "片段销毁", new Vector2(120f, 660f));
            var node251 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph52, typeof(BtsmtlSkillConditionResultFlowNode), "bcb254f6-5401-5b88-9a2b-774ba8280d70", "条件结果", new Vector2(600f, 180f));
            var node250 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph52, typeof(BtsmtlSkillStateRootCompletedFlowNode), "ff33b21c-6845-5b31-b4a7-84e6f00da514", "状态主体已完成", new Vector2(-360f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node222, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "19735807-6d5f-5e7f-91c9-b841ccf1f71e"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(node225, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "6225401bc79441dca5eaab16bdbc0644"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            BtsmtlSkillAuthoringContract.Apply(node237, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "e4c9e1f6a3d0578c2e6f9a0b1c2d3e4f"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "e78e29077a9052e7920cc13d40a350ba"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node242, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "e2a7c9d4f1b8356a0c4d7e8f9a0b1c2d"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "e78e29077a9052e7920cc13d40a350ba"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringContract.Apply(node247, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "e3b8d0e5f2c9467b1d5e8f9a0b1c2d3"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "e78e29077a9052e7920cc13d40a350ba"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            var edge104 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph46, node222, "m_Output", node223, "a", "37458f6e-7c14-5b56-a363-0431f6ef1ef9");
            var edge105 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph46, node223, "Value", node224, "m_Result", "5320d02d-70cf-5cd7-9a0d-42e20ea12b41");
            var edge106 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph46, node225, "m_Output", node223, "b", "e45db7d5-5b0c-52a2-9f22-39225b128b9e");
            var edge113 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph49, node236, "Output", node237, "Input", "ff0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c");
            var edge114 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph50, node241, "Output", node242, "Input", "d34e5f6a7b8c9d0e1f2a3b4c5d6e7f8a");
            var edge115 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph51, node246, "Output", node247, "Input", "b78c9d0e1f2a3b4c5d6e7f8a9b0c1d2e");
            var edge116 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph52, node250, "m_Output", node251, "m_Result", "ae717d21-6482-58da-bb1e-8f139e9a71a6");
            return parts;
        }

        sealed class Attack5_End2Parts
        {
            internal BtsmtlSkillFlowGraph graph46;
            internal BtsmtlSkillFlowGraph graph49;
            internal BtsmtlSkillFlowGraph graph50;
            internal BtsmtlSkillFlowGraph graph51;
            internal BtsmtlSkillFlowGraph graph52;
        }
    }
}
