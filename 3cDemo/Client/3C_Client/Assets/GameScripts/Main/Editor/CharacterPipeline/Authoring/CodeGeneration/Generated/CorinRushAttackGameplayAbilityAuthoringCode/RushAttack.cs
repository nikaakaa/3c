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
            var asset7 = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceTimeline.asset", 11400000L);
            var asset8 = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceLoopTimeline.asset", 11400000L);
            var asset9 = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceExplodeTimeline.asset", 11400000L);
            var asset10 = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceExplodeEndTimeline.asset", 11400000L);
            var asset11 = context.ResolveExternalAsset<TimelineAsset>("Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceEndTimeline.asset", 11400000L);
            parts.graph3 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "27452470-d64a-4302-2883-e591d208fdca", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Attack_Rush State Body");
            parts.graph6 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "b215492a-8abe-1c88-8508-349db54e5b51", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Attack_Rush_Explode State Body");
            parts.graph8 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "7ac0154f-bf12-b3f4-0a68-11664a6292ed", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Attack_Rush_End State Body");
            parts.graph10 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "749a1d8e-5dfb-ac35-8eaf-377c261ee5bc", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Attack_Rush_Enhance State Body");
            parts.graph12 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "6fe93f8b-9abc-09d5-0471-ad04ab77fce5", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Attack_Rush_Enhance_Loop State Body");
            parts.graph15 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "881bd1f9-9606-8b50-1488-4cf07e5c0ae4", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Attack_Rush_Enhance_Explode State Body");
            parts.graph17 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "2a683b1a-254e-b224-9766-f3a11fc1a640", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Attack_Rush_Enhance_Explode_End State Body");
            parts.graph19 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "f2e97280-b30b-59a0-7ee2-e0cba8f4a321", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Attack_Rush_Enhance_End State Body");
            var node8 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillRootFlowNode), "2632c6ba-e2dc-f9df-7bd3-71336f057a46", "技能入口", new Vector2(120f, 260f));
            var node10 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillStateOnExitFlowNode), "3ec211ee-05cc-4ef4-a5fb-9024caae0dd6", null, new Vector2(120f, 460f));
            var node7 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillStateOnEnterFlowNode), "446e389c-c5c9-4f32-8214-84ef547b42d4", null, new Vector2(120f, 60f));
            var node9 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph3, typeof(BtsmtlSkillTimelineFlowNode), "55b9f2ca-8bd1-2e45-b52f-33fed7e4aaaa", "Play Attack_Rush Timeline", new Vector2(360f, 0f));
            var node19 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillStateOnEnterFlowNode), "28e77a59-a8b1-470f-941e-7e5bca6cc467", null, new Vector2(120f, 60f));
            var node22 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillStateOnExitFlowNode), "2e807f11-7902-4560-8083-9ce15e6494f2", null, new Vector2(120f, 460f));
            var node20 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillRootFlowNode), "97c6a0c1-70cc-3aa3-8d48-90753d5ed1e6", "技能入口", new Vector2(120f, 260f));
            var node21 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph6, typeof(BtsmtlSkillTimelineFlowNode), "f9efcde8-1717-51f8-d479-c52979defca8", "Play Attack_Rush_Explode Timeline", new Vector2(360f, 0f));
            var node25 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillStateOnEnterFlowNode), "720a1072-f4c3-446d-950c-3a37b8cf21c9", null, new Vector2(120f, 60f));
            var node26 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillRootFlowNode), "8770dc67-3b72-74af-def8-085cbf69871f", "技能入口", new Vector2(120f, 260f));
            var node28 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillStateOnExitFlowNode), "b0672745-af95-43e3-853b-cf5d62102d97", null, new Vector2(120f, 460f));
            var node27 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph8, typeof(BtsmtlSkillTimelineFlowNode), "e21d43cf-55fc-293d-27e4-07ca61de6d99", "Play Attack_Rush_End Timeline", new Vector2(360f, 0f));
            var node33 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph10, typeof(BtsmtlSkillTimelineFlowNode), "003a1591-39c8-19a7-d890-316afb0632d8", "Play Attack_Rush_Enhance Timeline", new Vector2(360f, 0f));
            var node32 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph10, typeof(BtsmtlSkillRootFlowNode), "912756a8-c75a-d57e-4273-e85519e5de87", "技能入口", new Vector2(120f, 260f));
            var node34 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph10, typeof(BtsmtlSkillStateOnExitFlowNode), "d6218a38-e97a-441d-89ca-fca6f0f160a0", null, new Vector2(120f, 460f));
            var node31 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph10, typeof(BtsmtlSkillStateOnEnterFlowNode), "ff816c21-8c1a-4cb6-80ff-87f349aee315", null, new Vector2(120f, 60f));
            var node40 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph12, typeof(BtsmtlSkillStateOnExitFlowNode), "23993e8e-a387-4988-8ce4-7ed38c1cccce", null, new Vector2(120f, 460f));
            var node38 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph12, typeof(BtsmtlSkillRootFlowNode), "7212ba26-f915-8db3-3c3e-b4c5fa4c0c66", "技能入口", new Vector2(120f, 260f));
            var node37 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph12, typeof(BtsmtlSkillStateOnEnterFlowNode), "75a54ebb-66cd-493f-a4dd-5103fbba3cba", null, new Vector2(120f, 60f));
            var node39 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph12, typeof(BtsmtlSkillTimelineFlowNode), "f95825eb-a126-dad4-6d31-5157e5fb8278", "Play Attack_Rush_Enhance_Loop Timeline", new Vector2(360f, 0f));
            var node46 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph15, typeof(BtsmtlSkillStateOnEnterFlowNode), "85b0a724-c1ff-4f87-887f-0ed5d8f76045", null, new Vector2(120f, 60f));
            var node47 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph15, typeof(BtsmtlSkillRootFlowNode), "b69dbedb-2fd9-4eaf-174d-862cb9d43dd3", "技能入口", new Vector2(120f, 260f));
            var node48 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph15, typeof(BtsmtlSkillTimelineFlowNode), "dad3a6e3-da0b-a040-d0e2-c62cd44f7474", "Play Attack_Rush_Enhance_Explode Timeline", new Vector2(360f, 0f));
            var node49 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph15, typeof(BtsmtlSkillStateOnExitFlowNode), "f83a19e1-d20c-4547-811d-68d9a81567e8", null, new Vector2(120f, 460f));
            var node52 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph17, typeof(BtsmtlSkillStateOnEnterFlowNode), "179f6491-edd5-4d97-9a25-8b8ade5f5fd5", null, new Vector2(120f, 60f));
            var node55 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph17, typeof(BtsmtlSkillStateOnExitFlowNode), "3a56d26b-4da8-49d3-a7d4-6b3282c3b9aa", null, new Vector2(120f, 460f));
            var node54 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph17, typeof(BtsmtlSkillTimelineFlowNode), "4c1c23cf-e4ea-ab46-1fc6-d9f98482e9b1", "Play Attack_Rush_Enhance_Explode_End Timeline", new Vector2(360f, 0f));
            var node53 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph17, typeof(BtsmtlSkillRootFlowNode), "b28cd39b-0d43-49ad-7627-021f500e953e", "技能入口", new Vector2(120f, 260f));
            var node58 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph19, typeof(BtsmtlSkillStateOnEnterFlowNode), "3b959670-4572-4249-9e5a-6826000f3528", null, new Vector2(120f, 60f));
            var node59 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph19, typeof(BtsmtlSkillRootFlowNode), "4a622d0c-608c-9358-00ce-857b572b5141", "技能入口", new Vector2(120f, 260f));
            var node60 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph19, typeof(BtsmtlSkillTimelineFlowNode), "9a7b4266-72bb-d1d6-1ef4-1bb5297c8e26", "Play Attack_Rush_Enhance_End Timeline", new Vector2(360f, 0f));
            var node61 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph19, typeof(BtsmtlSkillStateOnExitFlowNode), "d64dcab8-25f3-4577-b4f1-6b8afcdfe0b0", null, new Vector2(120f, 460f));
            BtsmtlSkillAuthoringContract.Apply(node9, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", asset4), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            BtsmtlSkillAuthoringContract.Apply(node21, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", asset5), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            BtsmtlSkillAuthoringContract.Apply(node27, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", asset6), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            BtsmtlSkillAuthoringContract.Apply(node33, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", asset7), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            BtsmtlSkillAuthoringContract.Apply(node39, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("playbackMode", TimelinePlaybackMode.Loop), new BtsmtlSkillAuthoringFieldValue("timelineId", asset8), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            BtsmtlSkillAuthoringContract.Apply(node48, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", asset9), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            BtsmtlSkillAuthoringContract.Apply(node54, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", asset10), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            BtsmtlSkillAuthoringContract.Apply(node60, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", asset11), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Shared) });
            var edge4 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph3, node8, "Output", node9, "Input", "a63a8eab-4b5b-6407-3239-83270dbf9e41");
            var edge11 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph6, node20, "Output", node21, "Input", "e95cd5b2-a90a-6a1e-365b-6abbb3a3002b");
            var edge13 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph8, node26, "Output", node27, "Input", "b437e3fb-1bb1-3337-a2f9-a09206630264");
            var edge15 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph10, node32, "Output", node33, "Input", "07fa5309-0d3f-232b-c55c-09ac82532597");
            var edge17 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph12, node38, "Output", node39, "Input", "fb4091d1-a81e-57f7-4df9-35b383adc91d");
            var edge21 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph15, node47, "Output", node48, "Input", "356d988e-8a1f-d5f4-5881-57982bc37e1a");
            var edge23 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph17, node53, "Output", node54, "Input", "5e678128-1b8e-db0d-9f81-f08d6e58fa53");
            var edge25 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph19, node59, "Output", node60, "Input", "6ecf6584-df68-ef7a-861d-6a7a7e2f3785");
            return parts;
        }

        sealed class RushAttackParts
        {
            internal BtsmtlSkillFlowGraph graph3;
            internal BtsmtlSkillFlowGraph graph6;
            internal BtsmtlSkillFlowGraph graph8;
            internal BtsmtlSkillFlowGraph graph10;
            internal BtsmtlSkillFlowGraph graph12;
            internal BtsmtlSkillFlowGraph graph15;
            internal BtsmtlSkillFlowGraph graph17;
            internal BtsmtlSkillFlowGraph graph19;
        }
    }
}
