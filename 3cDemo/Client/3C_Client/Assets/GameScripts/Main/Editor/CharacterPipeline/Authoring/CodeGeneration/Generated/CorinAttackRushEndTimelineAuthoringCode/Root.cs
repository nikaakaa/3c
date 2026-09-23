using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEndTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<UnityAnimationClip>("Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_End_FootMotionTarget.anim", 7400000L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "8c852140-8ed0-ae2b-b3b3-654743e1e6ee", "CorinAttackRushEndTimeline");
            parts.timelineData = parts.timeline.Data;
            var timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(AnimationTrack), "9f5299c4-bc66-a805-1449-1268f10a3fbf", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track, "a9427025-b32a-0fb5-88bd-8040c5b63fdd", 0m, asset, 1.333333333255723118782043457m, 0m, 0m, 0m);
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "bcd0c997-0211-5653-03de-2e1f13f88e29", "Attack_Rush_End", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "9f5299c4-bc66-a805-1449-1268f10a3fbf" }, new[] { "a9427025-b32a-0fb5-88bd-8040c5b63fdd" }, new[] { "bcd0c997-0211-5653-03de-2e1f13f88e29" }, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimelineMarkers(rootParts.timelineData, Array.Empty<string>());
        }

        sealed class RootParts
        {
            internal TimelineAsset timeline;
            internal TimelineData timelineData;
        }
    }
}
