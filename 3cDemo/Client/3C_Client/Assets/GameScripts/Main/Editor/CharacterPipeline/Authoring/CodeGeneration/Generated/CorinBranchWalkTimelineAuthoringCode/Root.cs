using System;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchWalkTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<UnityAnimationClip>("Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Branch_Walk_FootMotionTarget.anim", 7400000L);
            var asset1 = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/Avatar_Female_Size01_Corin_Ani_Attack_Branch_Walk_FootMotionTarget.asset", 11400000L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "7bf8c586-06eb-00ff-1f88-f4030c3e41c9", "CorinBranchWalkTimeline");
            parts.timelineData = parts.timeline.Data;
            var timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(AnimationTrack), "6c10882e-f0d7-f092-9437-fbc2b1baaf81", "Animation", TimelineExecutionDomain.Presentation);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track, "1a783cd6-3eaa-a04a-5deb-e515f315d005", 0m, asset, 1.0833333700429648160934448242m, 0m, 0m, 0m);
            var track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(MotionCurveTrack), "8b389718-11da-c92f-b2b9-2fefaa22bb95", "Motion Curve", TimelineExecutionDomain.Logic);
            var clip1 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track1, "9b5184d4-2515-4e4b-7cad-dd09418c89e3", 0m, asset1, 1.0833333700429648160934448242m, 0m, 0m, 0m);
            parts.timelineData.Loop = true;
            BtsmtlSkillAuthoringCode.EnsureSection(parts.timelineData, "a07166c9-77b8-e148-44fd-6ae05cb69236", "Attack_Branch_02_Walk", 0m, "");
            ((AnimationTrack)track).SetAnimationChannelId(new AnimationChannelId("FullBodyAction"));
            ((AnimationTrack)track).SetAnimationSlotId("corin.full-body-action");
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("blendProfileId", TimelineAuthoringPropertyKind.Text, "corin.animation-rig.action-blend-profile") });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip1, new[] { new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "BranchWalk"), new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, asset1), new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, 1.08333325f) });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "6c10882e-f0d7-f092-9437-fbc2b1baaf81", "8b389718-11da-c92f-b2b9-2fefaa22bb95" }, new[] { "1a783cd6-3eaa-a04a-5deb-e515f315d005", "9b5184d4-2515-4e4b-7cad-dd09418c89e3" }, new[] { "a07166c9-77b8-e148-44fd-6ae05cb69236" }, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimelineMarkers(rootParts.timelineData, Array.Empty<string>());
        }

        sealed class RootParts
        {
            internal TimelineAsset timeline;
            internal TimelineData timelineData;
        }
    }
}
