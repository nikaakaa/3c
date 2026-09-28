using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchLoopTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<UnityAnimationClip>("Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Branch_Loop_FootMotionTarget.anim", 7400000L);
            var asset1 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/Avatar_Female_Size01_Corin_Ani_Attack_Branch_Loop_FootMotionTarget.asset", 11400000L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "5f5b8561-2490-a6dd-2f8c-195d830242f4", "CorinBranchLoopTimeline");
            parts.timelineData = parts.timeline.Data;
            var timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(AnimationTrack), "f1c4978f-7784-f927-b8ab-a2f871957401", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track, "126138d4-3b40-34dd-c7b6-3947b0d56472", 0m, asset, 1.0333334200549870729446411133m, 0m, 0m, 0m);
            var track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(MotionCurveTrack), "1c5095cf-ed9f-a89f-b931-2c5dff2158fd", "Motion Curve", TimelineExecutionDomain.Logic);
            var clip1 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track1, "911dcf11-c2f4-248e-c0d0-a3bf8551f70c", 0m, asset1, 1.0333334200549870729446411133m, 0m, 0m, 0m);
            parts.timelineData.Loop = true;
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "66f2262e-70ae-d7a6-57bb-8226adbb1fbb", "Attack_Branch_02_Loop", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip1, new[] { new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "BranchLoop"), new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, asset1), new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, 1.0333333f) });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "f1c4978f-7784-f927-b8ab-a2f871957401", "1c5095cf-ed9f-a89f-b931-2c5dff2158fd" }, new[] { "126138d4-3b40-34dd-c7b6-3947b0d56472", "911dcf11-c2f4-248e-c0d0-a3bf8551f70c" }, new[] { "66f2262e-70ae-d7a6-57bb-8226adbb1fbb" }, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimelineMarkers(rootParts.timelineData, Array.Empty<string>());
        }

        sealed class RootParts
        {
            internal TimelineAsset timeline;
            internal TimelineData timelineData;
        }
    }
}
