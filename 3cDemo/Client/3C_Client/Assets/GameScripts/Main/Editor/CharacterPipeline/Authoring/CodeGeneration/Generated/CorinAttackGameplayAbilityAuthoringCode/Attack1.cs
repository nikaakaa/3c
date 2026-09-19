using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode
    {
        static Attack1Parts BuildAttack1(AttackParts attack, RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack1Parts();
            parts.graph2 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph1, "1eac26e4ad67ccfd6cfe9342d2ea92d7", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision Attack1Hit");
            parts.graph3 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph1, "361211b72ded30e65d37533b1fb982da", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision ComboAccept");
            parts.graph4 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph1, "375b9282861f7b1674da6d39b8077a5a", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryLate");
            parts.graph5 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph1, "646760b2aa9725210b1232e71fb5a514", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, "Decision RecoveryEarly");
            parts.graph6 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(attack.graph1, "3d20d1d4c26a496393bb52f724252f24", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineTrigger, "tree Marker 25");
            parts.graph7 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "66a99a1ba00258473739f5c053c3d9c3", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack1 To Exit Condition");
            parts.graph9 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "280eceb11630ff24f9986186bc0698e6", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack1 To Exit Condition");
            parts.graph10 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "cebaa6f70788d586cbd02645609fd567", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack1 To Exit Condition");
            var node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(RequestCameraEffectNode), "0decd134-e335-4240-9d37-a40365be4f21", "Corin_Attack_Normal_01_Shake_Node", new Vector2(360f, 460f));
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillParallelFlowNode), "0e21a562-15a3-4d02-960f-1f5fa94ef22f", "相机与命中分支", new Vector2(240f, 260f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillBlackboardSetFlowNode), "10981503-e757-4c4d-a227-a5ed0b5dec44", "Set Attack1Hit", new Vector2(0f, 0f));
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDisableFlowNode), "42753d81-94dc-43b1-9ce5-b4ad64542f0a", "片段停用", new Vector2(120f, 460f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillRootFlowNode), "a7d4ffcf-87d0-4a0a-9f21-1b6983eb4a2a", "技能入口", new Vector2(120f, 260f));
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineDestroyFlowNode), "a937f675-27a7-4a9c-86ee-217b6ecdb158", "片段销毁", new Vector2(120f, 660f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineEnableFlowNode), "b43ece1c-87fe-419b-ab79-80bc31b2f31c", "片段启用", new Vector2(120f, 60f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(RequestCameraEffectNode), "b55d75dd-5b56-4cd3-90aa-95ff0ad4dd22", "Corin_Attack_Normal_01_Shake_Node", new Vector2(360f, 460f));
            var node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineEnableFlowNode), "0b641453-748d-4a90-a3ec-67c1d1c07d4a", "片段启用", new Vector2(120f, 60f));
            var node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillBlackboardSetFlowNode), "7552468a-1e85-40be-ba1a-19d41eaee472", "Set ComboAccept", new Vector2(0f, 0f));
            var node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineDisableFlowNode), "780c1509-e156-4e33-a73b-a42eb1357e4c", "片段停用", new Vector2(120f, 460f));
            var node18 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineDestroyFlowNode), "a61fe269-326f-42e5-9239-975c5c597289", "片段销毁", new Vector2(120f, 660f));
            var node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillRootFlowNode), "beaa704c-1db9-436e-bb89-63466eeb9723", "技能入口", new Vector2(120f, 260f));
            var node20 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillRootFlowNode), "0d4144d5-4595-47c5-bc8c-3680096d8ec9", "技能入口", new Vector2(16.78264f, 281.6265f));
            var node22 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineDisableFlowNode), "7ec0de2b-27b1-4837-b9a3-4d06246a82a6", "片段停用", new Vector2(120f, 460f));
            var node21 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillBlackboardSetFlowNode), "a5ae3b82-5ca9-4c50-b9ab-dab5442b0d15", "Set RecoveryLate", new Vector2(0f, 0f));
            var node19 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineEnableFlowNode), "aa0accd2-9c79-448b-8d45-7675ee2b5903", "片段启用", new Vector2(263.5213f, 48.20374f));
            var node23 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineDestroyFlowNode), "b7214047-3315-41ea-8e09-3cc48ef9699c", "片段销毁", new Vector2(120f, 660f));
            var node25 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillRootFlowNode), "216e7006-20c5-48b2-8ab2-68473a3e3ab1", "技能入口", new Vector2(120f, 260f));
            var node26 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillBlackboardSetFlowNode), "2461c960-3965-474e-828b-3c7ceeb23068", "Set RecoveryEarly", new Vector2(0f, 0f));
            var node28 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillTimelineDestroyFlowNode), "4563a302-8930-4300-83f1-d642d9d7f298", "片段销毁", new Vector2(120f, 660f));
            var node27 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillTimelineDisableFlowNode), "931d2f2f-06b4-4afc-a8ec-9a16374145b9", "片段停用", new Vector2(120f, 460f));
            var node24 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillTimelineEnableFlowNode), "f41b28cf-3809-4a26-abed-6eaed8d64981", "片段启用", new Vector2(120f, 60f));
            var node29 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillTimelineEnableFlowNode), "47d4a0b2-1793-4d94-8922-21958436187e", null, new Vector2(120f, 60f));
            var node30 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph7, typeof(BtsmtlSkillCanActivateActionFlowNode), "00986f00-673c-477f-89ac-f719e03fed7c", "Can Activate Dodge", new Vector2(-360f, 200f));
            var node33 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph7, typeof(BtsmtlSkillActionWindowActiveFlowNode), "311d0d5f-758b-4bbc-9f93-4a2a19d91d30", "Window RecoveryEarly", new Vector2(-360f, 100f));
            var node35 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph7, typeof(BtsmtlSkillActionRequestFlowNode), "567e0c7d-abb8-4eb5-9875-17057535d21f", "Has Dodge Request", new Vector2(-360f, 0f));
            var node32 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph7, typeof(BtsmtlSkillConditionResultFlowNode), "5aef9aa1-6d89-4a8c-b90a-28c2a5734b3c", "条件结果", new Vector2(600f, 180f));
            var node34 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph7, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "757d4fdd-a7da-4cd9-8f27-59320968f9ef", "AND", new Vector2(40f, 35f));
            var node31 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph7, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "8aa240eb-da75-466c-a708-7219269a1a12", "AND", new Vector2(220f, 70f));
            var node42 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph9, typeof(BtsmtlSkillStateRootCompletedFlowNode), "72c0eb28-54cc-4ad3-91b1-77a3bc941b76", "状态主体已完成", new Vector2(-360f, 0f));
            var node43 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph9, typeof(BtsmtlSkillConditionResultFlowNode), "9a7131f1-b4ac-461e-9729-6c292eed9e13", "条件结果", new Vector2(600f, 180f));
            var node45 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph10, typeof(BtsmtlSkillNativeNodeWrapper<FloatGreaterThan>), "66ce9414-6ae1-44bc-878c-495f3922f470", ">", new Vector2(-240f, 20f));
            var node46 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph10, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "74dd4a18-f979-4db1-9d51-40ae18bf0c07", "AND", new Vector2(40f, 35f));
            var node47 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph10, typeof(BtsmtlSkillConditionResultFlowNode), "8eb0bf71-446b-42a5-9f1a-738a45aac793", "条件结果", new Vector2(600f, 180f));
            var node44 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph10, typeof(BtsmtlSkillInputMagnitudeFlowNode), "9a152d3b-09ec-4f85-8340-b3e717e6a291", "MoveAxis Magnitude", new Vector2(-520f, 0f));
            var node48 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph10, typeof(BtsmtlSkillActionWindowActiveFlowNode), "d30a5fa3-668f-4f3d-a5fc-e94874c656d1", "Window RecoveryLate", new Vector2(-360f, 100f));
            var node49 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph10, typeof(BtsmtlSkillBlackboardScalarFlowNode), "e40ea606-b000-48c7-9dc4-d37c48ff114b", "StopThreshold", new Vector2(-520f, 45f));
            BtsmtlSkillAuthoringContract.Apply(node13, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Normal_01_CamShake_A_01") });
            BtsmtlSkillAuthoringContract.Apply(node8, new[] { new BtsmtlSkillAuthoringFieldValue("steps", new[] { BtsmtlSkillAuthoringContract.CreateStep("camera", "相机", null, 0, ProgramAbortPolicy.None), BtsmtlSkillAuthoringContract.CreateStep("hit", "命中", null, 0, ProgramAbortPolicy.None) }) });
            BtsmtlSkillAuthoringContract.Apply(node10, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "423b4949895d4fd38d25ee70e4b70602"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node10, "m_Value", true);
            BtsmtlSkillAuthoringContract.Apply(node9, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("resourceId", "Corin_Attack_Normal_01_CamShake_A_01") });
            BtsmtlSkillAuthoringContract.Apply(node16, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "86e50267d98f4ae8976bd806cb96a2d7"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "894a4cd14e8db8f49003b0660b7660ed"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node16, "m_Value", true);
            BtsmtlSkillAuthoringContract.Apply(node21, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "cfd05565ebbd49129a54cf1e838ceb45"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "894a4cd14e8db8f49003b0660b7660ed"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node21, "m_Value", true);
            BtsmtlSkillAuthoringContract.Apply(node26, new[] { new BtsmtlSkillAuthoringFieldValue("accessMode", "set"), new BtsmtlSkillAuthoringFieldValue("declarationId", "7862fb9d08504eb5803abe60861e6b79"), new BtsmtlSkillAuthoringFieldValue("factContext", null), new BtsmtlSkillAuthoringFieldValue("ownerId", "894a4cd14e8db8f49003b0660b7660ed"), new BtsmtlSkillAuthoringFieldValue("valueType", "bool") });
            BtsmtlSkillAuthoringCode.SetValue(node26, "m_Value", true);
            BtsmtlSkillAuthoringContract.Apply(node30, new[] { new BtsmtlSkillAuthoringFieldValue("admissionProfile", rootParts.asset42), new BtsmtlSkillAuthoringFieldValue("targetSnapshot", new BtsmtlSkillTargetSnapshotReference("b33c8e0cff9e4fd1a23ffc15768d7e43", "00ec42f6d5ede195dcf13e4e27fe7933")) });
            BtsmtlSkillAuthoringContract.Apply(node33, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RecoveryEarly") });
            BtsmtlSkillAuthoringContract.Apply(node35, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "Dodge"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node44, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "MoveAxis"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node48, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RecoveryLate") });
            BtsmtlSkillAuthoringContract.Apply(node49, new[] { new BtsmtlSkillAuthoringFieldValue("declarationId", "1edc27e65f454837b415895f4b808048"), new BtsmtlSkillAuthoringFieldValue("ownerId", "00ec42f6d5ede195dcf13e4e27fe7933") });
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node7, "Output", node8, "Input", "1b6fa94a-33d5-4fbd-96af-4f52f1c1f421");
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node8, "hit", node10, "Input", "d4c2aeb7-5842-448e-9c7f-16558d9dfc53");
            var edge3 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node8, "camera", node9, "Input", "e7c927bb-0706-4106-8d7e-d752fc9df427");
            var edge5 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node15, "Output", node16, "Input", "008c42ae-bb04-45ff-bb4a-6bbce70fbf97");
            var edge6 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node20, "Output", node21, "Input", "4d54f90c-0518-431c-aad3-1131f966578b");
            var edge7 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph5, node25, "Output", node26, "Input", "d537834a-88a2-4a76-a45e-9596f597a6f4");
            var edge8 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph7, node30, "m_Output", node31, "b", "bc048d43-0fe0-40a9-b087-3398300ce954");
            var edge9 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph7, node31, "Value", node32, "m_Result", "40ff98a9-0bcc-4091-9c5e-5de89e455a2e");
            var edge10 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph7, node33, "m_Output", node34, "b", "78c6fb2c-7321-4444-8762-dca53185b78b");
            var edge11 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph7, node34, "Value", node31, "a", "3cff8c0c-1139-4b63-8d84-615812601e74");
            var edge12 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph7, node35, "m_Output", node34, "a", "dbe695b0-31e4-402c-b2e3-93e4c7babbfa");
            var edge18 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph9, node42, "m_Output", node43, "m_Result", "ce30532f-dbc0-45f6-8481-b6cb87ddf9f8");
            var edge19 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph10, node44, "m_Output", node45, "a", "36b1412a-6fc1-4962-a7d2-b57307105cd1");
            var edge20 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph10, node45, "Value", node46, "a", "d63fdb49-6563-4457-a674-d95f9b558410");
            var edge21 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph10, node46, "Value", node47, "m_Result", "7c292eeb-a5fa-49cc-8160-d8cf0c231d8f");
            var edge22 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph10, node48, "m_Output", node46, "b", "c0764b82-f3eb-4f5e-a18d-fc93a6bb7139");
            var edge23 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph10, node49, "m_Output", node45, "b", "7aba0472-683c-4b37-9732-23871f316ab1");
            return parts;
        }

        sealed class Attack1Parts
        {
            internal BtsmtlSkillFlowGraph graph2;
            internal BtsmtlSkillFlowGraph graph3;
            internal BtsmtlSkillFlowGraph graph4;
            internal BtsmtlSkillFlowGraph graph5;
            internal BtsmtlSkillFlowGraph graph6;
            internal BtsmtlSkillFlowGraph graph7;
            internal BtsmtlSkillFlowGraph graph9;
            internal BtsmtlSkillFlowGraph graph10;
        }
    }
}
