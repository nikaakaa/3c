using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushExplodeTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<UnityAnimationClip>("Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Explode_FootMotionTarget.anim", 7400000L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "e25bc128-d9c4-0448-b708-cf1a0c8b8830", "CorinAttackRushExplodeTimeline");
            parts.timelineData = parts.timeline.Data;
            parts.timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(AnimationTrack), "28aa1f10-a087-95bd-2af1-8963b056c918", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track, "977432d8-2f56-cac2-8f6b-f01f8c03c53c", 0m, asset, 1.0500000000465661287307739258m, 0m, 0m, 0m);
            parts.track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "1bf4b743-1d2b-a12e-4bcf-f5fb5a4d2ca6", "Open RushAttackHandoff", TimelineExecutionDomain.Logic);
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "866672da-8a24-aa7c-d040-bc8f3df58e6a", "Attack_Rush_Explode", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, Open_RushAttackHandoff__14Parts open_RushAttackHandoff__14, BtsmtlAuthoringGenerationContext context)
        {
            rootParts.clip1 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track1, "e1786aa0-8844-0d5b-f435-599b31ebef46", 14m / 60m, open_RushAttackHandoff__14.graph);
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip1, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, open_RushAttackHandoff__14.graph) });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(open_RushAttackHandoff__14.graph, new[] { "01307e8c-ef8a-490d-b4f2-03023b2ddbdc", "a911a1cd-7774-74ab-3e3c-c94e914a1999", "e002f9fb5afaa9b3e28e91dc1c442c3b", "cf83d0cba3943dc1a138e29294eb593a", "dc66d8e7-ec9e-52af-a696-cb6cc9b315db", "09842c59-cca9-4782-99a1-e95a511dfa0e", "693dee71-0043-496d-8c5c-6487fa76de05", "02a6c3f2-9a88-49ce-94c6-73c9e1d293d8", "a945bf4b-431c-4ee3-8bf1-2e7df6e6deed" }, new[] { "2eac3e9583703345c834bbccace2ddef", "831173c8ee886e923338c98128fd9a83", "4e848d73d047062d9ecd44457dbb1f33", "fb8a6a0d-d88a-4b36-a5bc-5e6ba935973c", "fae59432-15b8-405d-bff1-9e83e9551e16", "daa0f8e5-124b-4d44-8259-c10ead8789e6", "dc61f57f-b678-4125-83e5-90501e67ecc9" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(open_RushAttackHandoff__14.graph1, new[] { "08543e2e115a83fb8d1e3ef4b8a6c8ac", "3a8a532cfb6d7ed1ea5e92c196ae49a7", "e189c371-2e75-4a25-acc6-ab53aca233a6" }, new[] { "48eefc86ba1579843b8d2b775362c15c", "d9ad97a1dcbec462b9b627cce5367067" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(open_RushAttackHandoff__14.graph3, new[] { "46bdd9b2-d2ba-472e-89f5-20438ce04d72", "4c5f1f8a-d0fe-45e6-9f01-6f3f1db1fd98", "260b1692-cc72-4166-9675-dbdf6dcb57f7", "6980fcde-3e7d-4206-8f05-7974840ddbeb", "e609ad94-75de-4278-a042-e967a78291eb" }, new[] { "ce3e5b90-13b1-454c-8d7e-24872f4dcf6b", "0fcb0799-22ca-493c-938f-eaf2278fc266", "728b134a-e42a-472c-8c42-ad6c6e77b656", "f1c3f785-6eb1-4841-a62b-20b012ec43b1", "213f9c77-8c4c-4575-9f70-3b7a6045eb3d" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(open_RushAttackHandoff__14.graph4, new[] { "c33a6537-daeb-41f1-b894-97bf215371f0", "0795d68e-33e7-4df0-af33-bb6e4ee033f0", "ae0b6daa-beb8-4fba-aecc-09826020d854" }, new[] { "a2daeffc-9bf0-4181-ab37-8fc438b03328", "2a30edbe-9f9a-4002-b9c4-09e2f7116dca" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(open_RushAttackHandoff__14.graph, new[] { "c0b6e090-4d93-9e2d-9659-95b8aede20b0", "e3771557-bf28-43be-b99a-2d0799c7d220" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(open_RushAttackHandoff__14.graph1, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(open_RushAttackHandoff__14.graph3, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(open_RushAttackHandoff__14.graph4, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "28aa1f10-a087-95bd-2af1-8963b056c918", "1bf4b743-1d2b-a12e-4bcf-f5fb5a4d2ca6" }, new[] { "977432d8-2f56-cac2-8f6b-f01f8c03c53c", "e1786aa0-8844-0d5b-f435-599b31ebef46" }, new[] { "866672da-8a24-aa7c-d040-bc8f3df58e6a" }, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimelineMarkers(rootParts.timelineData, Array.Empty<string>());
            BtsmtlSkillOwnedAssets.ReleaseOrphaned(rootParts.timeline);
        }

        sealed class RootParts
        {
            internal TimelineAsset timeline;
            internal TimelineData timelineData;
            internal Track track1;
            internal Clip clip1;
            internal TimelineContractCatalog timelineCatalog;
        }
    }
}
