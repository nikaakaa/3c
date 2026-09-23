using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<UnityAnimationClip>("Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_FootMotionTarget.anim", 7400000L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "606351d5-2e51-5730-3083-9e61df7814be", "CorinAttackRushTimeline");
            parts.timelineData = parts.timeline.Data;
            parts.timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(AnimationTrack), "96b6f5ae-9018-e3a7-4bb9-06cb7613ce62", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, parts.timelineCatalog, track, "a00919f2-e3a4-c306-f4d8-a6ffaa805aa7", 0m, asset, 1.166666666744276881217956543m, 0m, 0m, 0m);
            parts.track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, parts.timelineCatalog, typeof(TreeTrack), "a1c374d2-f94a-8eeb-0b57-aef78168d0e9", "Open RushRelease", TimelineExecutionDomain.Logic);
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "86b751b3-24d8-fc3c-3f0c-1d6dfa88f07e", "Attack_Rush", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, Open_RushRelease__13Parts open_RushRelease__13, BtsmtlAuthoringGenerationContext context)
        {
            rootParts.clip1 = BtsmtlSkillAuthoringCode.EnsureClip(rootParts.timelineData, rootParts.timelineCatalog, rootParts.track1, "afd9b28a-5319-5f6a-37d7-7d1600dda9da", 0.1999999999534338712692260742m, open_RushRelease__13.graph);
            TimelineAuthoringPropertyContract.Apply(rootParts.timelineData, rootParts.clip1, new[] { new TimelineAuthoringPropertyValue("executionPhase", TimelineAuthoringPropertyKind.Enum, TimelineTreeExecutionPhase.Decision), new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, open_RushRelease__13.graph) });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(open_RushRelease__13.graph, new[] { "67fc193c-1f8a-459f-99be-c2674e0f807d", "824479d8-fe60-97da-b837-7057b4ffb3c4", "a1801a530b3621874f90fcc30c1c2980", "588176fd56347925b53c0e1ce37b96bf", "5ff7e22c-25ab-4ff1-da33-d305fd403eb7", "fdfe95d8-c235-4b5d-9442-5c824dd83988", "6b4dca86-bbd8-46b4-a5cd-d046ea7c4dc0" }, new[] { "6f1a8f7714e164cf8e97702130fb84bd", "a017b7ed23355b5d3f1e44326e2e4c78", "fb4a7a8e524dad90c3ea2318e796e560" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(open_RushRelease__13.graph1, new[] { "240d2a8b7d752774a688c297ae71d41d", "9e1c2a215105285fc3e17f3f53cb4541", "febbbc66-4d44-4b28-85d4-be0def2ef40a" }, new[] { "4c3793fff2fc498c75f8bd420a6e5166", "83b8cf6cf59ace8c57bfa762ea002fea" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(open_RushRelease__13.graph, new[] { "2087927f-c32b-0c89-864a-fbf269fba556" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(open_RushRelease__13.graph1, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "96b6f5ae-9018-e3a7-4bb9-06cb7613ce62", "a1c374d2-f94a-8eeb-0b57-aef78168d0e9" }, new[] { "a00919f2-e3a4-c306-f4d8-a6ffaa805aa7", "afd9b28a-5319-5f6a-37d7-7d1600dda9da" }, new[] { "86b751b3-24d8-fc3c-3f0c-1d6dfa88f07e" }, Array.Empty<string>());
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
