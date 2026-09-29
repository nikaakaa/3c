using System;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchAttackGameplayAbilityAuthoringCode
    {
        static BranchAttackParts BuildBranchAttack(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new BranchAttackParts();
            var asset7 = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/BranchAttack/CorinBranchStartTimeline.asset", 11400000L);
            var asset8 = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/BranchAttack/CorinBranchExplodeTimeline.asset", 11400000L);
            var asset9 = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/BranchAttack/CorinBranchEndTimeline.asset", 11400000L);
            var asset10 = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/BranchAttack/CorinBranchLoopTimeline.asset", 11400000L);
            var asset11 = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/BranchAttack/CorinBranchWalkTimeline.asset", 11400000L);
            parts.graph1 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "00f20065-267d-9b0f-3b16-3671c867525f", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Start State Body");
            BtsmtlSkillAuthoringCode.ConfigureNativeState(rootParts.state1, parts.graph1);
            parts.graph4 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "dba3170e-4eeb-d46a-75d5-da4bc64fc6b2", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Explode State Body");
            BtsmtlSkillAuthoringCode.ConfigureNativeState(rootParts.state2, parts.graph4);
            parts.graph6 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "5eb55018-0efe-f2bf-fa7d-84438caf8433", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "End State Body");
            BtsmtlSkillAuthoringCode.ConfigureNativeState(rootParts.state3, parts.graph6);
            parts.graph8 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "f919f518-17f1-3b53-9319-8e1edba3f827", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Loop State Body");
            BtsmtlSkillAuthoringCode.ConfigureNativeState(rootParts.state5, parts.graph8);
            parts.graph11 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "d4dc4405-5666-3cd9-7877-d776f1aefe5a", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Walk State Body");
            BtsmtlSkillAuthoringCode.ConfigureNativeState(rootParts.state6, parts.graph11);
            var node5 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillStateOnExitFlowNode), "29fb343e-c67c-2f9c-d218-b1ad2df26835", "退出状态", new Vector2(120f, 460f));
            var node2 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillStateOnEnterFlowNode), "4ab87d0d-e5a7-4e50-e2df-544301e7f0fc", "进入状态", new Vector2(120f, 60f));
            var node3 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillRootFlowNode), "95386920-cebd-2d3c-f221-a2f33bba5f76", "技能入口", new Vector2(120f, 260f));
            var node4 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph1, typeof(BtsmtlSkillTimelineFlowNode), "cf1b257e-1739-5c84-d043-c195b28a2eaa", "Play Start", new Vector2(360f, 260f));
            var node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillStateOnEnterFlowNode), "0951c9ea-e5aa-f6a3-640b-e08fa7a6fac1", "进入状态", new Vector2(120f, 60f));
            var node15 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillTimelineFlowNode), "255985ff-8069-fca6-6605-ae73735ce75e", "Play Explode", new Vector2(360f, 260f));
            var node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillStateOnExitFlowNode), "716e7788-28b8-d0db-77a4-f8955d3f800d", "退出状态", new Vector2(120f, 460f));
            var node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillRootFlowNode), "8646c812-da6e-a522-354b-60f141efb11e", "技能入口", new Vector2(120f, 260f));
            var node22 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillStateOnExitFlowNode), "3cd61447-d6a9-42ac-0a1d-7d15ef647bb5", "退出状态", new Vector2(120f, 460f));
            var node20 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillRootFlowNode), "9308ee5d-14d9-26a2-e137-2f99a1cde280", "技能入口", new Vector2(120f, 260f));
            var node19 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillStateOnEnterFlowNode), "c59593cb-5b5f-9eb0-98aa-bee7b7db48ed", "进入状态", new Vector2(120f, 60f));
            var node21 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillTimelineFlowNode), "f4681b0d-4709-c76f-2e56-b3a98d3c5b11", "Play End", new Vector2(360f, 260f));
            var node28 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillStateOnExitFlowNode), "43ba89d0-ddb2-8b41-95e5-cba7e67c5c0d", "退出状态", new Vector2(120f, 460f));
            var node25 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillStateOnEnterFlowNode), "a138d90c-ee37-801b-a065-4030bfaae9c4", "进入状态", new Vector2(120f, 60f));
            var node27 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillTimelineFlowNode), "c5ad3a8c-a13e-1a37-ea35-97e58edeea2c", "Play Loop", new Vector2(360f, 260f));
            var node26 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillRootFlowNode), "e7154d3b-e27f-f722-e04e-52ad6c85d128", "技能入口", new Vector2(120f, 260f));
            var node36 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph11, typeof(BtsmtlSkillRootFlowNode), "568bbb9f-a3e8-8ac4-1dd2-640635d10c29", "技能入口", new Vector2(120f, 260f));
            var node38 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph11, typeof(BtsmtlSkillStateOnExitFlowNode), "61549d03-9c96-e380-2df9-175723a111ca", "退出状态", new Vector2(120f, 460f));
            var node35 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph11, typeof(BtsmtlSkillStateOnEnterFlowNode), "8d8e9bde-769d-7406-f256-5ce338bb39fe", "进入状态", new Vector2(120f, 60f));
            var node37 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph11, typeof(BtsmtlSkillTimelineFlowNode), "d3f48f1e-be9f-1cc4-7146-452193f95e76", "Play Walk", new Vector2(360f, 260f));
            BtsmtlSkillAuthoringContract.Apply(node4, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", asset7), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            BtsmtlSkillAuthoringContract.Apply(node15, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", asset8), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            BtsmtlSkillAuthoringContract.Apply(node21, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", asset9), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            BtsmtlSkillAuthoringContract.Apply(node27, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("playbackMode", TimelinePlaybackMode.Loop), new BtsmtlSkillAuthoringFieldValue("timelineId", asset10), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            BtsmtlSkillAuthoringContract.Apply(node37, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("playbackMode", TimelinePlaybackMode.Loop), new BtsmtlSkillAuthoringFieldValue("timelineId", asset11), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            var edge1 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph1, node3, "Output", node4, "Input", "226c5af3-0b41-abcc-6ae8-157c0a389bae");
            var edge7 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node14, "Output", node15, "Input", "2c6d744a-d773-05a8-d134-418406e3d4cf");
            var edge9 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph6, node20, "Output", node21, "Input", "7cbcc729-974f-bdd0-556e-e666a072ef20");
            var edge11 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph8, node26, "Output", node27, "Input", "0e634d85-e8ff-0d8b-5278-67310a4df75e");
            var edge16 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph11, node36, "Output", node37, "Input", "d7220402-f48c-b4c1-814d-6fc7fba13aad");
            return parts;
        }

        sealed class BranchAttackParts
        {
            internal BtsmtlSkillFlowGraph graph1;
            internal BtsmtlSkillFlowGraph graph4;
            internal BtsmtlSkillFlowGraph graph6;
            internal BtsmtlSkillFlowGraph graph8;
            internal BtsmtlSkillFlowGraph graph11;
        }
    }
}
