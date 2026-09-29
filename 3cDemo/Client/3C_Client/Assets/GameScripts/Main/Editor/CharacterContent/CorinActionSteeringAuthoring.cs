using System;
using BTSMTL.Timeline;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    internal static class CorinActionSteeringAuthoring
    {
        internal static string AddMotion(BtsmtlAuthoringGenerationContext context, TimelineData data,
            string state, string sourcePath, decimal duration)
        {
            var source = context.ResolveExternalAsset<RootMotionCurveAsset>(sourcePath, 11400000L);
            var catalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(data, catalog, typeof(MotionCurveTrack),
                Id(state + ":motion-track"), "Motion Curve", TimelineExecutionDomain.Logic);
            string id = Id(state + ":motion-clip");
            var clip = BtsmtlSkillAuthoringCode.EnsureClip(data, catalog, track, id, 0m, source, duration, 0m, 0m, 0m);
            float end = (float)duration;
            if ((double)end > (double)duration)
                end = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(end) - 1);
            TimelineAuthoringPropertyContract.Apply(data, clip, new[]
            {
                new TimelineAuthoringPropertyValue("curveId", TimelineAuthoringPropertyKind.Text, state),
                new TimelineAuthoringPropertyValue("sourceCurve", TimelineAuthoringPropertyKind.Object, source),
                new TimelineAuthoringPropertyValue("sourceEndTime", TimelineAuthoringPropertyKind.Float, end)
            });
            return id;
        }

        internal static void Apply(TimelineData data, string sourceId, string warpId,
            AnimationCurve targetFrameResponse, AnimationCurve inputFrameResponse)
        {
            if (!MotionWarpAuthoring.TryResolveSource(data, sourceId, out MotionCurveClip source))
                throw new InvalidOperationException("Corin steering source is missing: " + sourceId);
            var catalog = TimelineTreeContractComposition.Create();
            MotionWarpClip warp = null;
            foreach (var track in data.Tracks)
                foreach (var clip in track.Clips)
                    if (clip is MotionWarpClip candidate && candidate.AuthoringId == warpId)
                        warp = candidate;
            if (warp == null)
            {
                var track = BtsmtlSkillAuthoringCode.EnsureTrack(data, catalog, typeof(MotionWarpTrack),
                    Id(warpId + ":track"), "Action Steering", TimelineExecutionDomain.Logic);
                warp = (MotionWarpClip)BtsmtlSkillAuthoringCode.EnsureClip(data, catalog, track,
                    warpId, Seconds(source.StartTime), null, Seconds(source.CurveEndTime), 0m, 0m, 0m);
            }
            else
            {
                BtsmtlSkillAuthoringCode.EnsureClip(data, catalog, warp.Track, warpId,
                    Seconds(source.StartTime), null, Seconds(source.CurveEndTime), 0m, 0m, 0m);
            }
            TimelineAuthoringPropertyContract.Apply(data, warp, new[]
            {
                new TimelineAuthoringPropertyValue("sourceMotionClipId", TimelineAuthoringPropertyKind.Text, sourceId),
                new TimelineAuthoringPropertyValue("translationMode", TimelineAuthoringPropertyKind.Enum, MotionWarpTranslationMode.Disabled),
                new TimelineAuthoringPropertyValue("rotationMethod", TimelineAuthoringPropertyKind.Enum, MotionWarpRotationMethod.TargetResponse),
                new TimelineAuthoringPropertyValue("rotationMode", TimelineAuthoringPropertyKind.Enum, MotionWarpRotationMode.FaceTarget),
                new TimelineAuthoringPropertyValue("maxTotalYawCorrectionDegrees", TimelineAuthoringPropertyKind.Float, 180f),
                new TimelineAuthoringPropertyValue("steeringInputId", TimelineAuthoringPropertyKind.Text, "MoveAxis")
            });
            float frames = (source.CurveEndTime - source.StartTime).ToSingle() * 60f;
            TimelineCurveChannelCatalog.Require("motion-warp.yaw-response").Replace(warp, Normalize(targetFrameResponse, frames));
            TimelineCurveChannelCatalog.Require("motion-warp.input-yaw-response").Replace(warp, Normalize(inputFrameResponse, frames));
        }

        static AnimationCurve Normalize(AnimationCurve curve, float frames)
        {
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].time /= frames;
                keys[i].inTangent *= frames;
                keys[i].outTangent *= frames;
            }
            return new AnimationCurve(keys) { preWrapMode = curve.preWrapMode, postWrapMode = curve.postWrapMode };
        }

        static decimal Seconds(FixedScalar value) => (decimal)value.Raw / FixedScalar.OneRaw;
        internal static string Id(string seed) => new Guid(BtsmtlSkillGraphAssetFactory.StableIdentity(seed)).ToString("D");
    }
}
