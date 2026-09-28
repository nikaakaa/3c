using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchEndTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<UnityAnimationClip>("Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Branch_02_End_FootMotionTarget.anim", 7400000L);
            var asset1 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/Avatar_Female_Size01_Corin_Ani_Attack_Branch_02_End_FootMotionTarget.asset", 11400000L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "6b6bda6e-be52-5ca5-c54a-dccbedf7338e", "CorinBranchEndTimeline");
            parts.timelineData = parts.timeline.Data;
            var timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(AnimationTrack), "39489022-a308-66f6-26f6-f5888b9e3075", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track, "3971d342-54c5-6457-9e99-0210fb447708", 0m, asset, 2.1833333999384194612503051758m, 0m, 0m, 0m);
            var track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(MotionCurveTrack), "83436d42-b99e-1671-1918-69fc7a7256a8", "Motion Curve", TimelineExecutionDomain.Logic);
            var clip1 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track1, "5f985d00-f5d7-2a40-cfa7-7b1b83c8e022", 0m, asset1, 2.1833333999384194612503051758m, 0m, 0m, 0m);
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "4718955e-5eb3-9983-5385-af03b676148d", "Attack_Branch_02_End", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip1, new[] { new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "BranchEnd"), new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, asset1), new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, 2.1833334f) });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "39489022-a308-66f6-26f6-f5888b9e3075", "83436d42-b99e-1671-1918-69fc7a7256a8" }, new[] { "3971d342-54c5-6457-9e99-0210fb447708", "5f985d00-f5d7-2a40-cfa7-7b1b83c8e022" }, new[] { "4718955e-5eb3-9983-5385-af03b676148d" }, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimelineMarkers(rootParts.timelineData, Array.Empty<string>());
        }

        sealed class RootParts
        {
            internal TimelineAsset timeline;
            internal TimelineData timelineData;
        }
    }
}
