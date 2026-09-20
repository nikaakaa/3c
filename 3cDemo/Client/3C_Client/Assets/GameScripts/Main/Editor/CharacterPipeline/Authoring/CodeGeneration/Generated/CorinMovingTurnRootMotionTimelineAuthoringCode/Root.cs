using System;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinMovingTurnRootMotionTimelineAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<RootMotionCurveAsset>("Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinMovingTurnRootMotionTimeline/CorinMovingTurnRootMotionTimeline/MovingTurn180.asset", 11400000L);
            var asset1 = context.ResolveExternalAsset<BtsmtlSkillFlowGraph>("Assets/Configs/Character/Corin/Pipeline/Graphs/SharedTimelines/CorinMovingTurnRootMotionTimeline.asset", 133216587555870579L);
            parts.timeline = BtsmtlSkillAuthoringCode.EnsureTimelineRoot(context, "8a6491b4-93fe-4002-a814-2ac6eb75e567", "CorinMovingTurnRootMotionTimeline");
            parts.timelineData = parts.timeline.Data;
            var timelineCatalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(MotionCurveTrack), "9b2e235b-266b-47ef-8ecd-c2fa8a4207fc", "Motion Curve", TimelineExecutionDomain.Logic);
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track, "e04f4e26-be58-4698-8905-36dcef1d5405", 0m, asset, 0.4666666686534881591796875m, 0, 0, 0);
            var track1 = BtsmtlSkillAuthoringCode.EnsureTrack(parts.timelineData, timelineCatalog, typeof(TreeTrack), "cd3bfa4b-0b9d-4c85-a6e8-e1d386bbb770", "tree", TimelineExecutionDomain.Logic);
            var clip1 = BtsmtlSkillAuthoringCode.EnsureClip(parts.timelineData, timelineCatalog, track1, "cdbb3f67-b1ba-4139-9524-851c1e9873d3", 0.1166666666977107524871826172m, asset1);
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip, new[] { new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, "MovingTurn180"), new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, asset), new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, 0.466666669f) });
            TimelineCurveChannelCatalog.Require("motion.ease-in").Replace(clip, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0.9999998f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0.9999998f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineCurveChannelCatalog.Require("motion.ease-out").Replace(clip, new AnimationCurve(new[] { new Keyframe(0f, 0f, 0f, 0.9999998f, 0f, 0f) { weightedMode = WeightedMode.None }, new Keyframe(1f, 1f, 0.9999998f, 0f, 0f, 0f) { weightedMode = WeightedMode.None } }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            TimelineAuthoringPropertyContract.Apply(parts.timelineData, clip1, new[] { new TimelineAuthoringPropertyValue("assetTree", TimelineAuthoringPropertyKind.Object, asset1) });
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.PruneTimeline(rootParts.timelineData, new[] { "9b2e235b-266b-47ef-8ecd-c2fa8a4207fc", "cd3bfa4b-0b9d-4c85-a6e8-e1d386bbb770" }, new[] { "e04f4e26-be58-4698-8905-36dcef1d5405", "cdbb3f67-b1ba-4139-9524-851c1e9873d3" }, Array.Empty<string>(), Array.Empty<string>());
        }

        sealed class RootParts
        {
            internal TimelineAsset timeline;
            internal TimelineData timelineData;
        }
    }
}
