using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
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
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track, "977432d8-2f56-cac2-8f6b-f01f8c03c53c", 0m, asset, 1.166666666744276881217956543m, 0m, 0m, 0m);
            parts.track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "1bf4b743-1d2b-a12e-4bcf-f5fb5a4d2ca6", "Open RushAttackHandoff", TimelineExecutionDomain.Logic);
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "866672da-8a24-aa7c-d040-bc8f3df58e6a", "Attack_Rush_Explode", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, Open_RushAttackHandoff__14Parts open_RushAttackHandoff__14, BtsmtlAuthoringGenerationContext context)
        {
            rootParts.clip1 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track1, "e1786aa0-8844-0d5b-f435-599b31ebef46", 0.2166666665580123662948608398m, open_RushAttackHandoff__14.graph);
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip1, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, open_RushAttackHandoff__14.graph) });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(open_RushAttackHandoff__14.graph, new[] { "01307e8c-ef8a-490d-b4f2-03023b2ddbdc", "a911a1cd-7774-74ab-3e3c-c94e914a1999", "e002f9fb5afaa9b3e28e91dc1c442c3b", "cf83d0cba3943dc1a138e29294eb593a", "dc66d8e7-ec9e-52af-a696-cb6cc9b315db", "09842c59-cca9-4782-99a1-e95a511dfa0e", "693dee71-0043-496d-8c5c-6487fa76de05" }, new[] { "2eac3e9583703345c834bbccace2ddef", "831173c8ee886e923338c98128fd9a83", "4e848d73d047062d9ecd44457dbb1f33" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(open_RushAttackHandoff__14.graph1, new[] { "08543e2e115a83fb8d1e3ef4b8a6c8ac", "3a8a532cfb6d7ed1ea5e92c196ae49a7", "e189c371-2e75-4a25-acc6-ab53aca233a6" }, new[] { "48eefc86ba1579843b8d2b775362c15c", "d9ad97a1dcbec462b9b627cce5367067" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(open_RushAttackHandoff__14.graph, new[] { "c0b6e090-4d93-9e2d-9659-95b8aede20b0" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(open_RushAttackHandoff__14.graph1, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "28aa1f10-a087-95bd-2af1-8963b056c918", "1bf4b743-1d2b-a12e-4bcf-f5fb5a4d2ca6" }, new[] { "977432d8-2f56-cac2-8f6b-f01f8c03c53c", "e1786aa0-8844-0d5b-f435-599b31ebef46" }, new[] { "866672da-8a24-aa7c-d040-bc8f3df58e6a" }, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimelineMarkers(rootParts.timelineData, Array.Empty<string>());
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
