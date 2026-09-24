using System;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {
        static RushAttackParts BuildRushAttack(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RushAttackParts();
            var asset4 = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushTimeline.asset", 11400000L);
            var asset5 = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushExplodeTimeline.asset", 11400000L);
            var asset6 = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEndTimeline.asset", 11400000L);
            parts.graph2 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "27452470-d64a-4302-2883-e591d208fdca", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Attack_Rush State Body");
            parts.graph5 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "b215492a-8abe-1c88-8508-349db54e5b51", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Attack_Rush_Explode State Body");
            parts.graph7 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "7ac0154f-bf12-b3f4-0a68-11664a6292ed", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Attack_Rush_End State Body");
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillRootFlowNode), "2632c6ba-e2dc-f9df-7bd3-71336f057a46", "技能入口", new Vector2(120f, 260f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillStateOnExitFlowNode), "3ec211ee-05cc-4ef4-a5fb-9024caae0dd6", null, new Vector2(120f, 460f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillStateOnEnterFlowNode), "446e389c-c5c9-4f32-8214-84ef547b42d4", null, new Vector2(120f, 60f));
            var node6 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph2, typeof(BtsmtlSkillTimelineFlowNode), "55b9f2ca-8bd1-2e45-b52f-33fed7e4aaaa", "Play Attack_Rush Timeline", new Vector2(360f, 0f));
            var node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillStateOnEnterFlowNode), "28e77a59-a8b1-470f-941e-7e5bca6cc467", null, new Vector2(120f, 60f));
            var node18 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillStateOnExitFlowNode), "2e807f11-7902-4560-8083-9ce15e6494f2", null, new Vector2(120f, 460f));
            var node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillRootFlowNode), "97c6a0c1-70cc-3aa3-8d48-90753d5ed1e6", "技能入口", new Vector2(120f, 260f));
            var node17 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph5, typeof(BtsmtlSkillTimelineFlowNode), "f9efcde8-1717-51f8-d479-c52979defca8", "Play Attack_Rush_Explode Timeline", new Vector2(360f, 0f));
            var node21 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph7, typeof(BtsmtlSkillStateOnEnterFlowNode), "720a1072-f4c3-446d-950c-3a37b8cf21c9", null, new Vector2(120f, 60f));
            var node22 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph7, typeof(BtsmtlSkillRootFlowNode), "8770dc67-3b72-74af-def8-085cbf69871f", "技能入口", new Vector2(120f, 260f));
            var node24 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph7, typeof(BtsmtlSkillStateOnExitFlowNode), "b0672745-af95-43e3-853b-cf5d62102d97", null, new Vector2(120f, 460f));
            var node23 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph7, typeof(BtsmtlSkillTimelineFlowNode), "e21d43cf-55fc-293d-27e4-07ca61de6d99", "Play Attack_Rush_End Timeline", new Vector2(360f, 0f));
            BtsmtlSkillAuthoringContract.Apply(node6, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", asset4), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            BtsmtlSkillAuthoringContract.Apply(node17, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", asset5), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            BtsmtlSkillAuthoringContract.Apply(node23, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", asset6), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            var edge2 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph2, node5, "Output", node6, "Input", "a63a8eab-4b5b-6407-3239-83270dbf9e41");
            var edge8 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph5, node16, "Output", node17, "Input", "e95cd5b2-a90a-6a1e-365b-6abbb3a3002b");
            var edge10 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph7, node22, "Output", node23, "Input", "b437e3fb-1bb1-3337-a2f9-a09206630264");
            return parts;
        }

        sealed class RushAttackParts
        {
            internal BtsmtlSkillFlowGraph graph2;
            internal BtsmtlSkillFlowGraph graph5;
            internal BtsmtlSkillFlowGraph graph7;
        }
    }
}
