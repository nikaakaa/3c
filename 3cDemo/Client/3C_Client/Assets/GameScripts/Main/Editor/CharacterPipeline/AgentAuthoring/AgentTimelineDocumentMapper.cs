using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
using TreeDesigner;
using UnityEngine;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentPackageMappingSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentTimelineDocumentMapper
    {
        internal static bool ValidateTimelineRelationships(
            AgentDocumentEditable editable,
            AgentCompileReport report)
        {
            var timelines = (editable.timelines ?? new List<AgentSnapshotTimeline>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.timelineAuthoringId))
                .GroupBy(value => value.timelineAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var treeClips = new Dictionary<string, AgentSnapshotTimelineTreeClip>(StringComparer.Ordinal);
            var declarations = new HashSet<string>(
                (editable.blackboardDeclarations ?? new List<AgentSnapshotBlackboardDeclaration>())
                    .Where(value => value != null)
                    .Select(value => value.declarationId),
                StringComparer.Ordinal);
            bool valid = true;
            foreach (AgentSnapshotTimelineTreeClip treeClip in editable.timelineTreeClips ?? new List<AgentSnapshotTimelineTreeClip>())
            {
                string path = $"editable.controller.timelineTreeClips[{treeClip?.clipAuthoringId}]";
                if (treeClip == null ||
                    !IsIdentity(treeClip.clipAuthoringId) ||
                    !treeClips.TryAdd(treeClip.clipAuthoringId, treeClip) ||
                    !timelines.TryGetValue(treeClip.timelineAuthoringId ?? string.Empty, out AgentSnapshotTimeline timeline))
                {
                    report.Error(path, "timeline_tree_clip_relationship_invalid", "TreeClip identity重复、Timeline缺失或引用非法。");
                    valid = false;
                    continue;
                }
                if (!string.Equals(treeClip.ownership, TimelineTreeOwnership.Inline.ToString(), StringComparison.Ordinal) ||
                    !Enum.TryParse(treeClip.phase, false, out TimelineTreeExecutionPhase _) ||
                    treeClip.writes == null ||
                    treeClip.writes.Count > 1 ||
                    treeClip.writes.Any(write =>
                        write == null ||
                        !IsIdentity(write.declarationId) ||
                        !declarations.Contains(write.declarationId)))
                {
                    report.Error(path, "timeline_tree_clip_configuration_invalid", "TreeClip必须是Inline、使用合法phase，并且最多引用一个现有Blackboard declaration。");
                    valid = false;
                }
                AgentSnapshotTimelineTrack track = (timeline.tracks ?? new List<AgentSnapshotTimelineTrack>())
                    .FirstOrDefault(value => string.Equals(value.trackAuthoringId, treeClip.trackAuthoringId, StringComparison.Ordinal));
                AgentSnapshotTimelineClip clip = track?.clips?.FirstOrDefault(value =>
                    string.Equals(value.clipAuthoringId, treeClip.clipAuthoringId, StringComparison.Ordinal));
                if (track == null ||
                    clip == null ||
                    clip.typeName?.EndsWith("TreeClip", StringComparison.Ordinal) != true ||
                    clip.startFrame != treeClip.startFrame ||
                    clip.endFrame != treeClip.endFrame)
                {
                    report.Error(path, "timeline_tree_clip_projection_mismatch", "Controller TreeClip必须与timeline分片中的Timeline、Track、Clip和frame范围一致。");
                    valid = false;
                }
            }

            foreach (AgentSnapshotTimeline timeline in editable.timelines ?? new List<AgentSnapshotTimeline>())
            {
                foreach (AgentSnapshotTimelineTrack track in timeline?.tracks ?? new List<AgentSnapshotTimelineTrack>())
                {
                    foreach (AgentSnapshotTimelineClip clip in track?.clips ?? new List<AgentSnapshotTimelineClip>())
                    {
                        if (clip?.typeName?.EndsWith("TreeClip", StringComparison.Ordinal) == true &&
                            !treeClips.ContainsKey(clip.clipAuthoringId))
                        {
                            report.Error(
                                $"editable.timelines[{timeline.timelineAuthoringId}].tracks[{track.trackAuthoringId}].clips[{clip.clipAuthoringId}]",
                                "timeline_tree_clip_summary_missing",
                                "Timeline TreeClip缺少controller分片中的目标状态。");
                            valid = false;
                        }
                    }
                }
            }
            return valid;
        }

        internal static void ToTimelineFiles(
            AgentSnapshotTimeline source,
            out AgentPackageTimelineFile timeline,
            out AgentPackageCurvesFile curves)
        {
            timeline = new AgentPackageTimelineFile
            {
                id = source.timelineAuthoringId,
                name = source.name,
                callSites = AgentAuthoringDocumentCodec.Clone(source.callSites) ?? new List<AgentSnapshotTimelineCallSite>(),
                sections = AgentAuthoringDocumentCodec.Clone(source.sections) ?? new List<AgentSnapshotTimelineSection>(),
                tracks = AgentAuthoringDocumentCodec.Clone(source.tracks) ?? new List<AgentSnapshotTimelineTrack>()
            };
            curves = new AgentPackageCurvesFile { timelineId = source.timelineAuthoringId };
            foreach (AgentSnapshotTimelineTrack track in timeline.tracks)
            {
                foreach (AgentSnapshotTimelineClip clip in track.clips ?? new List<AgentSnapshotTimelineClip>())
                {
                    foreach (AgentSnapshotTimelineCurveChannel channel in clip.curveChannels ?? new List<AgentSnapshotTimelineCurveChannel>())
                    {
                        curves.curves.Add(new AgentPackageCurve
                        {
                            clipId = clip.clipAuthoringId,
                            channelId = channel.channelId,
                            timeDomain = channel.timeDomain,
                            bounded = channel.bounded,
                            minimum = channel.minimum,
                            maximum = channel.maximum,
                            zero = channel.zero,
                            unit = channel.unit,
                            preWrapMode = channel.preWrapMode,
                            postWrapMode = channel.postWrapMode,
                            keys = channel.keys
                        });
                    }
                    clip.curveChannels = new List<AgentSnapshotTimelineCurveChannel>();
                }
            }
        }

        internal static bool TryFromTimelineFiles(
            string path,
            AgentPackageTimelineFile timeline,
            AgentPackageCurvesFile curves,
            AgentSnapshotTimeline current,
            AgentCompileReport report,
            out AgentSnapshotTimeline result)
        {
            result = new AgentSnapshotTimeline
            {
                timelineAuthoringId = timeline.id,
                name = timeline.name,
                callSites = timeline.callSites ?? new List<AgentSnapshotTimelineCallSite>(),
                sections = timeline.sections ?? new List<AgentSnapshotTimelineSection>(),
                tracks = timeline.tracks ?? new List<AgentSnapshotTimelineTrack>()
            };
            if (!IsIdentity(timeline.id) ||
                timeline.sections == null ||
                timeline.sections.Any(section =>
                    section == null ||
                    !IsIdentity(section.sectionAuthoringId) ||
                    string.IsNullOrWhiteSpace(section.name) ||
                    !string.Equals(section.name, section.name.Trim(), StringComparison.Ordinal) ||
                    section.frame < 0) ||
                timeline.tracks == null ||
                timeline.tracks.Any(track =>
                    track == null ||
                    !IsIdentity(track.trackAuthoringId) ||
                    track.clips == null ||
                    track.clips.Any(clip => clip == null ||
                        !IsIdentity(clip.clipAuthoringId) ||
                        clip.clipInFrame < 0 ||
                        clip.typeName?.EndsWith("AnimationClip", StringComparison.Ordinal) == true &&
                        (!IsAssetReference(clip.animationClip) ||
                         !Enum.TryParse(clip.extraPolationMode, false, out ExtraPolationMode _)))))
            {
                report.Error(path, "timeline_structure_invalid", "Timeline、Section、Track或Clip缺少合法identity与必需集合。");
                return false;
            }
            if (result.sections.GroupBy(section => section.sectionAuthoringId, StringComparer.Ordinal).Any(group => group.Count() > 1) ||
                result.sections.GroupBy(section => section.name, StringComparer.Ordinal).Any(group => group.Count() > 1))
            {
                report.Error(path + ".sections", "timeline_section_duplicate", "Timeline内Section identity或名称重复。");
                return false;
            }
            var sectionIds = result.sections
                .Select(section => section.sectionAuthoringId)
                .ToHashSet(StringComparer.Ordinal);
            foreach (AgentSnapshotTimelineSection section in result.sections)
            {
                if (!string.IsNullOrEmpty(section.nextSectionId) &&
                    !sectionIds.Contains(section.nextSectionId))
                {
                    report.Error(
                        path + $".sections[{section.sectionAuthoringId}].nextSectionId",
                        "timeline_section_next_not_found",
                        "Timeline Section nextSectionId必须引用同一Timeline内存在的Section。");
                    return false;
                }
            }
            List<AgentSnapshotTimelineClip> allClips = result.tracks
                .SelectMany(track => track.clips ?? new List<AgentSnapshotTimelineClip>())
                .Where(clip => clip != null && !string.IsNullOrEmpty(clip.clipAuthoringId))
                .ToList();
            if (allClips.GroupBy(clip => clip.clipAuthoringId, StringComparer.Ordinal).Any(group => group.Count() > 1))
            {
                report.Error(path + ".tracks.clips", "timeline_clip_identity_duplicate", "Timeline内Clip identity重复。");
                return false;
            }
            var clips = allClips.ToDictionary(clip => clip.clipAuthoringId, clip => clip, StringComparer.Ordinal);
            var currentChannels = (current?.tracks ?? new List<AgentSnapshotTimelineTrack>())
                .SelectMany(track => track.clips ?? new List<AgentSnapshotTimelineClip>())
                .SelectMany(clip => (clip.curveChannels ?? new List<AgentSnapshotTimelineCurveChannel>())
                    .Select(channel => new
                    {
                        clip.clipAuthoringId,
                        channel
                    }))
                .GroupBy(value => value.clipAuthoringId + "\0" + value.channel.channelId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().channel, StringComparer.Ordinal);
            var curveIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageCurve curve in curves.curves ?? new List<AgentPackageCurve>())
            {
                string curvePath = $"{path}.curves[{curve?.clipId}:{curve?.channelId}]";
                string identity = (curve?.clipId ?? string.Empty) + "\0" + (curve?.channelId ?? string.Empty);
                if (curve == null ||
                    !curveIds.Add(identity) ||
                    !clips.TryGetValue(curve.clipId ?? string.Empty, out AgentSnapshotTimelineClip clip) ||
                    !ValidateCurve(curve, curvePath, report))
                    return false;
                currentChannels.TryGetValue(identity, out AgentSnapshotTimelineCurveChannel existing);
                TimelineCurveChannelCatalog.TryGet(curve.channelId, out TimelineCurveChannelDescriptor descriptor);
                AgentSnapshotTimelineCurveChannel channel = existing != null
                    ? AgentAuthoringDocumentCodec.Clone(existing)
                    : new AgentSnapshotTimelineCurveChannel
                    {
                        channelId = curve.channelId,
                        displayName = descriptor.DisplayName,
                        timeDomain = descriptor.TimeDomain.ToString(),
                        bounded = descriptor.ValueDomain.IsBounded,
                        minimum = descriptor.ValueDomain.Minimum,
                        maximum = descriptor.ValueDomain.Maximum,
                        zero = descriptor.ValueDomain.Zero,
                        unit = descriptor.ValueDomain.Unit
                    };
                channel.preWrapMode = curve.preWrapMode;
                channel.postWrapMode = curve.postWrapMode;
                channel.keys = curve.keys ?? new List<AgentAnimationCurveKey>();
                clip.curveChannels.Add(channel);
            }
            return true;
        }

        static bool ValidateCurve(AgentPackageCurve curve, string path, AgentCompileReport report)
        {
            if (string.IsNullOrWhiteSpace(curve.channelId) ||
                !TimelineCurveChannelCatalog.TryGet(curve.channelId, out TimelineCurveChannelDescriptor descriptor) ||
                !string.Equals(curve.timeDomain, descriptor.TimeDomain.ToString(), StringComparison.Ordinal) ||
                curve.bounded != descriptor.ValueDomain.IsBounded ||
                curve.minimum != descriptor.ValueDomain.Minimum ||
                curve.maximum != descriptor.ValueDomain.Maximum ||
                curve.zero != descriptor.ValueDomain.Zero ||
                !string.Equals(curve.unit ?? string.Empty, descriptor.ValueDomain.Unit, StringComparison.Ordinal) ||
                !Enum.TryParse(curve.preWrapMode, true, out UnityEngine.WrapMode preWrap) ||
                !Enum.IsDefined(typeof(UnityEngine.WrapMode), preWrap) ||
                !Enum.TryParse(curve.postWrapMode, true, out UnityEngine.WrapMode postWrap) ||
                !Enum.IsDefined(typeof(UnityEngine.WrapMode), postWrap) ||
                curve.keys == null ||
                curve.keys.Count == 0)
            {
                report.Error(path, "timeline_curve_invalid", "Curve缺少registered channel、wrap mode或keys。");
                return false;
            }
            float previous = -1f;
            for (int i = 0; i < curve.keys.Count; i++)
            {
                AgentAnimationCurveKey key = curve.keys[i];
                if (key == null ||
                    !Enum.TryParse(key.weightedMode, true, out UnityEngine.WeightedMode weightedMode) ||
                    !Enum.IsDefined(typeof(UnityEngine.WeightedMode), weightedMode) ||
                    !Finite(key.time) ||
                    key.time < 0f ||
                    key.time > 1f ||
                    key.time <= previous ||
                    !Finite(key.value) ||
                    !Finite(key.inTangent) ||
                    !Finite(key.outTangent) ||
                    !Finite(key.inWeight) ||
                    !Finite(key.outWeight))
                {
                    report.Error($"{path}.keys[{i}]", "timeline_curve_key_invalid", "Curve key必须按normalized time严格递增，并只包含有限数值与合法weightedMode。");
                    return false;
                }
                previous = key.time;
            }
            return true;
        }

        static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
