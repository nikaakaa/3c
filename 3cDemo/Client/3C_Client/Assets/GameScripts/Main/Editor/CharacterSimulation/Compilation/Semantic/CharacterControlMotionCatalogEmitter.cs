using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    // 控制模块 SourceCurve 位移的静态曲线载体：把 Definition.ControlMotionTimelines
    // 引用的 TimelineData 声明为 Timeline/TimelineTrack/MotionCurve 三级 Catalog 条目，
    // 运行时 FixedMotionRuntime 按 timeline:{id}/track:{id}/clip:{id} 身份采样 yaw/position。
    // 控制运动不走 Timeline scheduler，本类只在编译期把曲线搬进 Program Catalog。
    internal static class CharacterControlMotionCatalogEmitter
    {
        internal static void Declare(
            CharacterSimulationProgramBuilder builder,
            IReadOnlyList<TimelineData> timelines,
            string definitionPath,
            CharacterSimulationCompileReport report)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (timelines == null || timelines.Count == 0)
                return;
            for (int timelineIndex = 0; timelineIndex < timelines.Count; timelineIndex++)
            {
                TimelineData timeline = timelines[timelineIndex];
                if (timeline == null)
                {
                    report.DiscoveryError(
                        "control_motion_timeline_null",
                        definitionPath,
                        $"Control motion timeline #{timelineIndex} is null.");
                    continue;
                }
                string timelineIdentity = $"timeline:{timeline.AuthoringId}";
                var timelineSource = Source(definitionPath, timeline.Name);
                builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.Timeline,
                    timelineIdentity,
                    0,
                    new[]
                    {
                        builder.ConstantField(timelineSource, "FrameRate", TimelineUtility.FrameRate)
                    },
                    timelineSource);
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    if (timeline.Tracks[trackIndex] is not MotionCurveTrack track)
                        continue;
                    string trackIdentity = $"{timelineIdentity}/track:{track.AuthoringId}";
                    var trackSource = Source(definitionPath, $"{timeline.Name}/{track.Name}");
                    builder.DeclareCatalogEntry(
                        ProgramCatalogEntryKind.TimelineTrack,
                        trackIdentity,
                        0,
                        new[]
                        {
                            builder.IdentityField("Timeline", timelineIdentity)
                        },
                        trackSource);
                    for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                    {
                        if (track.Clips[clipIndex] is not MotionCurveClip clip)
                            continue;
                        string clipIdentity = $"{trackIdentity}/clip:{clip.AuthoringId}";
                        var fields = new List<ProgramCatalogField>
                        {
                            builder.IdentityField("Track", trackIdentity),
                            builder.ConstantField(trackSource, "StartFrame", clip.StartFrame),
                            builder.ConstantField(trackSource, "CurveEndFrame", clip.CurveEndFrame)
                        };
                        TryAddCurveField(builder, fields, trackSource, clip, "WeightCurve", clip.WeightCurve);
                        TryAddCurveField(builder, fields, trackSource, clip, "PositionX", clip.ProgramPositionX);
                        TryAddCurveField(builder, fields, trackSource, clip, "PositionY", clip.ProgramPositionY);
                        TryAddCurveField(builder, fields, trackSource, clip, "PositionZ", clip.ProgramPositionZ);
                        TryAddCurveField(builder, fields, trackSource, clip, "Yaw", clip.ProgramYaw);
                        TryAddCurveField(builder, fields, trackSource, clip, "EaseInCurve", clip.EaseInCurve);
                        TryAddCurveField(builder, fields, trackSource, clip, "EaseOutCurve", clip.EaseOutCurve);
                        builder.DeclareCatalogEntry(
                            ProgramCatalogEntryKind.MotionCurve,
                            clipIdentity,
                            0,
                            fields,
                            trackSource);
                    }
                }
            }
        }

        static void TryAddCurveField(
            CharacterSimulationProgramBuilder builder,
            List<ProgramCatalogField> fields,
            CharacterSimulationSourceLocation source,
            MotionCurveClip clip,
            string fieldName,
            AnimationCurve curve)
        {
            if (curve == null)
            {
                builder.Report.DiscoveryError(
                    "control_motion_curve_missing",
                    source.Identity,
                    $"Motion curve clip '{clip.CurveId}' is missing curve '{fieldName}'.");
                return;
            }
            var writer = new SemanticDataWriter();
            writer.WriteUInt32(0x56525543);
            writer.WriteInt32(1);
            writer.WriteInt32((int)curve.preWrapMode);
            writer.WriteInt32((int)curve.postWrapMode);
            writer.WriteInt32(curve.length);
            for (int i = 0; i < curve.length; i++)
            {
                Keyframe key = curve.keys[i];
                if (key.weightedMode != WeightedMode.None)
                    throw new InvalidOperationException($"Curve '{fieldName}' key #{i} uses unsupported weighted tangents.");
                writer.WriteNumber(key.time, $"{source.Identity}/{fieldName}[{i}].time");
                writer.WriteNumber(key.value, $"{source.Identity}/{fieldName}[{i}].value");
                writer.WriteNumber(key.inTangent, $"{source.Identity}/{fieldName}[{i}].inTangent");
                writer.WriteNumber(key.outTangent, $"{source.Identity}/{fieldName}[{i}].outTangent");
                writer.WriteNumber(key.inWeight, $"{source.Identity}/{fieldName}[{i}].inWeight");
                writer.WriteNumber(key.outWeight, $"{source.Identity}/{fieldName}[{i}].outWeight");
                writer.WriteInt32((int)key.weightedMode);
            }
            fields.Add(builder.ConstantField(source, fieldName, writer.Build()));
        }

        static CharacterSimulationSourceLocation Source(string definitionPath, string name)
        {
            return new CharacterSimulationSourceLocation(
                typeof(CharacterPipelineDefinition).FullName,
                definitionPath,
                string.Empty,
                string.Empty,
                name,
                string.Empty,
                $"control-motion-timeline:{name}");
        }
    }
}
