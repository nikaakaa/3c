using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Motion;
using TreeDesigner;
using UnityEngine;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentDocumentMutationSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentTimelineDocumentMutationModule
    {
        internal static void BuildTimelineMutations(
            AgentGraphSnapshot current,
            AgentDocumentEditable target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            const string timelinesPath = "document.editable.timelines";
            var oldTimelines = Index(current.timelines, value => value.timelineAuthoringId, timelinesPath, report);
            var newTimelines = Index(target.timelines, value => value.timelineAuthoringId, timelinesPath, report);
            var targetNodeIds = new HashSet<string>(
                (target.graphs ?? new List<AgentSnapshotGraph>())
                    .SelectMany(graph => graph.nodes ?? new List<AgentSnapshotNode>())
                    .Select(node => node.elementAuthoringId),
                StringComparer.Ordinal);
            var currentTreeClips = Index(
                current.timelineTreeClips,
                value => value.clipAuthoringId,
                "current.timelineTreeClips",
                report);
            var targetTreeClips = Index(
                target.timelineTreeClips,
                value => value.clipAuthoringId,
                "document.editable.timelineTreeClips",
                report);
            var removedTimelineIds = new HashSet<string>(oldTimelines.Keys.Except(newTimelines.Keys, StringComparer.Ordinal), StringComparer.Ordinal);
            var removedTrackKeys = new HashSet<string>(StringComparer.Ordinal);
            var localTimelineIdentities = new Dictionary<string, string>(StringComparer.Ordinal);
            var localTrackIdentities = new Dictionary<string, string>(StringComparer.Ordinal);
            var localClipIdentities = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (AgentSnapshotTimeline timeline in current.timelines ?? new List<AgentSnapshotTimeline>())
            {
                if (!removedTimelineIds.Contains(timeline.timelineAuthoringId))
                    continue;
                string path = $"{timelinesPath}[{Escape(timeline.timelineAuthoringId)}]";
                if (timeline.callSites == null || timeline.callSites.Count == 0 ||
                    timeline.callSites.Any(callSite => targetNodeIds.Contains(callSite.nodeAuthoringId)))
                    report.Error(path, "timeline_delete_requires_owner_node", "Timeline只能通过删除全部拥有它的Timeline节点级联删除。");
            }

            foreach (AgentSnapshotTimeline timeline in target.timelines ?? new List<AgentSnapshotTimeline>())
            {
                string path = $"{timelinesPath}[{Escape(timeline.timelineAuthoringId)}]";
                if (!oldTimelines.TryGetValue(timeline.timelineAuthoringId, out AgentSnapshotTimeline oldTimeline))
                {
                    AgentSnapshotTimelineCallSite callSite = timeline.callSites?.Count == 1
                        ? timeline.callSites[0]
                        : null;
                    List<AgentSnapshotTimelineBindingSummary> bindings = (target.stateMachines ?? new List<AgentSnapshotStateMachineSummary>())
                        .SelectMany(machine => machine.states ?? new List<AgentSnapshotStateSummary>())
                        .SelectMany(state => state.timelines ?? new List<AgentSnapshotTimelineBindingSummary>())
                        .Where(binding =>
                            callSite != null &&
                            string.Equals(binding.nodeAuthoringId, callSite.nodeAuthoringId, StringComparison.Ordinal) &&
                            string.Equals(binding.timelineAuthoringId, timeline.timelineAuthoringId, StringComparison.Ordinal))
                        .ToList();
                    if (!IsLocal(timeline.timelineAuthoringId) ||
                        callSite == null ||
                        !IsLocal(callSite.nodeAuthoringId) ||
                        !targetNodeIds.Contains(callSite.nodeAuthoringId) ||
                        bindings.Count != 1 ||
                        !string.Equals(bindings[0].ownership, AgentTimelineOwnership.Inline.ToString(), StringComparison.Ordinal))
                    {
                        report.Error(path, "timeline_create_requires_inline_node", "新增Timeline必须由同一事务中的唯一local Inline TimelineNode拥有，并在controller、graph与callSite中一致声明。");
                        continue;
                    }
                    string plannedIdentity = LocalIdentity(timeline.timelineAuthoringId);
                    localTimelineIdentities[timeline.timelineAuthoringId] = plannedIdentity;
                    Add(mutations, path, AgentMutationKind.EnsureInlineTimeline, operation =>
                    {
                        operation.id = plannedIdentity;
                        operation.targetPlannedIdentity = LocalIdentity(callSite.nodeAuthoringId);
                        operation.displayName = timeline.name;
                    });
                    oldTimeline = new AgentSnapshotTimeline
                    {
                        timelineAuthoringId = timeline.timelineAuthoringId,
                        name = timeline.name,
                        callSites = AgentAuthoringDocumentCodec.Clone(timeline.callSites),
                        sections = new List<AgentSnapshotTimelineSection>(),
                        tracks = new List<AgentSnapshotTimelineTrack>()
                    };
                }
                if (!string.Equals(oldTimeline.name, timeline.name, StringComparison.Ordinal) ||
                    !Same(oldTimeline.callSites, timeline.callSites))
                {
                    report.Error(path, "timeline_metadata_modified", "Timeline名称与调用点只能通过拥有它的Timeline节点修改。");
                    continue;
                }

                var oldSections = Index(oldTimeline.sections, value => value.sectionAuthoringId, path + ".sections", report);
                var newSections = Index(timeline.sections, value => value.sectionAuthoringId, path + ".sections", report);
                foreach (AgentSnapshotTimelineSection section in oldTimeline.sections ?? new List<AgentSnapshotTimelineSection>())
                {
                    if (newSections.ContainsKey(section.sectionAuthoringId))
                        continue;
                    Add(mutations, $"{path}.sections[{Escape(section.sectionAuthoringId)}]", AgentMutationKind.DeleteTimelineSection, operation =>
                    {
                        SetTimelineReference(operation, timeline.timelineAuthoringId, localTimelineIdentities);
                        operation.sectionAuthoringId = section.sectionAuthoringId;
                    });
                }
                foreach (AgentSnapshotTimelineSection section in timeline.sections ?? new List<AgentSnapshotTimelineSection>())
                {
                    string sectionPath = $"{path}.sections[{Escape(section.sectionAuthoringId)}]";
                    oldSections.TryGetValue(section.sectionAuthoringId, out AgentSnapshotTimelineSection oldSection);
                    if (oldSection == null && !IsLocal(section.sectionAuthoringId))
                    {
                        report.Error(sectionPath, "timeline_section_create_requires_local_identity", "新增Timeline Section必须使用local identity。");
                        continue;
                    }
                    if (oldSection != null &&
                        string.Equals(oldSection.name, section.name, StringComparison.Ordinal) &&
                        oldSection.frame == section.frame)
                        continue;
                    Add(mutations, sectionPath, AgentMutationKind.EnsureTimelineSection, operation =>
                    {
                        if (oldSection == null)
                            operation.id = LocalIdentity(section.sectionAuthoringId);
                        else
                            operation.sectionAuthoringId = section.sectionAuthoringId;
                        SetTimelineReference(operation, timeline.timelineAuthoringId, localTimelineIdentities);
                        operation.displayName = section.name;
                        operation.startFrame = section.frame;
                    });
                }

                var oldTracks = Index(oldTimeline.tracks, value => value.trackAuthoringId, path + ".tracks", report);
                var newTracks = Index(timeline.tracks, value => value.trackAuthoringId, path + ".tracks", report);
                var removedTracks = (oldTimeline.tracks ?? new List<AgentSnapshotTimelineTrack>())
                    .Where(track => !newTracks.ContainsKey(track.trackAuthoringId))
                    .ToList();
                foreach (AgentSnapshotTimelineTrack track in removedTracks)
                {
                    removedTrackKeys.Add(timeline.timelineAuthoringId + "\0" + track.trackAuthoringId);
                    Add(mutations, $"{path}.tracks[{Escape(track.trackAuthoringId)}]", AgentMutationKind.DeleteTimelineTrack, operation =>
                    {
                        operation.timelineAuthoringId = timeline.timelineAuthoringId;
                        operation.trackAuthoringId = track.trackAuthoringId;
                    });
                }

                foreach (AgentSnapshotTimelineTrack track in timeline.tracks ?? new List<AgentSnapshotTimelineTrack>())
                {
                    string trackPath = $"{path}.tracks[{Escape(track.trackAuthoringId)}]";
                    if (!oldTracks.TryGetValue(track.trackAuthoringId, out AgentSnapshotTimelineTrack oldTrack))
                    {
                        bool motionCurveTrack = track.typeName?.EndsWith("MotionCurveTrack", StringComparison.Ordinal) == true;
                        if ((!track.motionWarpTrack && !motionCurveTrack) || !IsLocal(track.trackAuthoringId))
                        {
                            report.Error(trackPath, "timeline_track_create_unsupported", "当前正式Document能力只允许创建local MotionCurve Track或MotionWarp Track；其它Track必须先补齐对应的typed capability。");
                            continue;
                        }
                        string plannedIdentity = LocalIdentity(track.trackAuthoringId);
                        localTrackIdentities[track.trackAuthoringId] = plannedIdentity;
                        BuildTimelineTrackMutations(
                            timeline.timelineAuthoringId,
                            null,
                            track,
                            currentTreeClips,
                            targetTreeClips,
                            localTimelineIdentities,
                            localTrackIdentities,
                            localClipIdentities,
                            mutations,
                            report,
                            trackPath);
                        continue;
                    }
                    int expectedIndex = oldTrack.index - removedTracks.Count(value => value.index < oldTrack.index);
                    if (track.index != expectedIndex)
                    {
                        report.Error(trackPath + ".index", "timeline_track_reorder_unsupported", "Track顺序只能由删除前序Track自然收拢，当前正式API不支持任意重排。");
                        continue;
                    }
                    BuildTimelineTrackMutations(
                        timeline.timelineAuthoringId,
                        oldTrack,
                        track,
                        currentTreeClips,
                        targetTreeClips,
                        localTimelineIdentities,
                        localTrackIdentities,
                        localClipIdentities,
                        mutations,
                        report,
                        trackPath);
                }
            }

        }

        static void BuildTimelineTrackMutations(
            string timelineId,
            AgentSnapshotTimelineTrack current,
            AgentSnapshotTimelineTrack target,
            IReadOnlyDictionary<string, AgentSnapshotTimelineTreeClip> currentTreeClips,
            IReadOnlyDictionary<string, AgentSnapshotTimelineTreeClip> targetTreeClips,
            IReadOnlyDictionary<string, string> localTimelineIdentities,
            IReadOnlyDictionary<string, string> localTrackIdentities,
            IDictionary<string, string> localClipIdentities,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            if (current != null &&
                (!string.Equals(current.typeName, target.typeName, StringComparison.Ordinal) ||
                 current.motionWarpTrack != target.motionWarpTrack))
            {
                report.Error(path, "timeline_track_kind_changed", "Track kind不能原地改变；请删除旧Track并创建受支持的新Track。");
                return;
            }

            bool motionCurveTrack = target.typeName?.EndsWith("MotionCurveTrack", StringComparison.Ordinal) == true;
            if (motionCurveTrack &&
                (current == null || !string.Equals(current.name, target.name, StringComparison.Ordinal)))
            {
                Add(mutations, path, AgentMutationKind.EnsureMotionCurveTrack, operation =>
                {
                    if (current == null && IsLocal(target.trackAuthoringId))
                        operation.id = localTrackIdentities.TryGetValue(target.trackAuthoringId, out string plannedIdentity)
                            ? plannedIdentity
                            : LocalIdentity(target.trackAuthoringId);
                    SetTimelineReference(operation, timelineId, localTimelineIdentities);
                    if (current != null && !IsLocal(target.trackAuthoringId))
                        operation.trackAuthoringId = target.trackAuthoringId;
                    operation.displayName = target.name;
                });
            }
            else if (target.motionWarpTrack &&
                (current == null || !string.Equals(current.name, target.name, StringComparison.Ordinal)))
            {
                Add(mutations, path, AgentMutationKind.EnsureMotionWarpTrack, operation =>
                {
                    if (current == null && IsLocal(target.trackAuthoringId))
                        operation.id = localTrackIdentities.TryGetValue(target.trackAuthoringId, out string plannedIdentity)
                            ? plannedIdentity
                            : LocalIdentity(target.trackAuthoringId);
                    SetTimelineReference(operation, timelineId, localTimelineIdentities);
                    if (current != null && !IsLocal(target.trackAuthoringId))
                        operation.trackAuthoringId = target.trackAuthoringId;
                    operation.displayName = target.name;
                });
            }
            else if (current != null && !string.Equals(current.name, target.name, StringComparison.Ordinal))
            {
                report.Error(path + ".name", "timeline_track_name_unsupported", "当前Track类型没有正式rename Mutation。");
            }

            bool isAnimationTrack = !string.IsNullOrEmpty(target.animationChannelId) ||
                                    target.typeName?.EndsWith("AnimationTrack", StringComparison.Ordinal) == true;
            if (isAnimationTrack)
            {
                if (current == null || !string.Equals(current.animationChannelId, target.animationChannelId, StringComparison.Ordinal))
                {
                    Add(mutations, path + ".animationChannelId", AgentMutationKind.ConfigureAnimationTrackChannel, operation =>
                    {
                        operation.timelineAuthoringId = timelineId;
                        SetTimelineTrackReference(operation, target.trackAuthoringId, localTrackIdentities);
                        operation.animationChannelId = target.animationChannelId;
                    });
                }
            }
            else if (current != null &&
                     !SameOptionalText(current.animationChannelId, target.animationChannelId))
            {
                report.Error(path, "timeline_track_animation_fields_invalid", "非AnimationTrack不能携带Animation channel配置。");
            }

            var oldClips = Index(current?.clips, value => value.clipAuthoringId, path + ".clips", report);
            var newClips = Index(target.clips, value => value.clipAuthoringId, path + ".clips", report);
            foreach (AgentSnapshotTimelineClip removed in current?.clips ?? new List<AgentSnapshotTimelineClip>())
            {
                if (newClips.ContainsKey(removed.clipAuthoringId))
                    continue;
                Add(mutations, $"{path}.clips[{Escape(removed.clipAuthoringId)}]", AgentMutationKind.DeleteTimelineClip, operation =>
                {
                    operation.timelineAuthoringId = timelineId;
                    operation.trackAuthoringId = target.trackAuthoringId;
                    operation.clipAuthoringId = removed.clipAuthoringId;
                });
            }

            foreach (AgentSnapshotTimelineClip clip in target.clips ?? new List<AgentSnapshotTimelineClip>())
            {
                oldClips.TryGetValue(clip.clipAuthoringId, out AgentSnapshotTimelineClip oldClip);
                currentTreeClips.TryGetValue(clip.clipAuthoringId, out AgentSnapshotTimelineTreeClip oldTreeClip);
                targetTreeClips.TryGetValue(clip.clipAuthoringId, out AgentSnapshotTimelineTreeClip treeClip);
                BuildTimelineClipMutations(
                    timelineId,
                    target,
                    oldClip,
                    clip,
                    oldTreeClip,
                    treeClip,
                    localTimelineIdentities,
                    localTrackIdentities,
                    localClipIdentities,
                    mutations,
                    report,
                    $"{path}.clips[{Escape(clip.clipAuthoringId)}]");
            }
        }

        static void BuildTimelineClipMutations(
            string timelineId,
            AgentSnapshotTimelineTrack track,
            AgentSnapshotTimelineClip current,
            AgentSnapshotTimelineClip target,
            AgentSnapshotTimelineTreeClip currentTreeClip,
            AgentSnapshotTimelineTreeClip targetTreeClip,
            IReadOnlyDictionary<string, string> localTimelineIdentities,
            IReadOnlyDictionary<string, string> localTrackIdentities,
            IDictionary<string, string> localClipIdentities,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            if (current != null &&
                (!string.Equals(current.typeName, target.typeName, StringComparison.Ordinal) ||
                 current.motionWarpClip != target.motionWarpClip))
            {
                report.Error(path, "timeline_clip_kind_changed", "Clip kind不能原地改变；请删除旧Clip并创建受支持的新Clip。");
                return;
            }
            if (current == null && !IsLocal(target.clipAuthoringId))
            {
                report.Error(path, "timeline_clip_identity_unknown", "新增Clip必须使用local: identity。");
                return;
            }

            bool treeClip = targetTreeClip != null || target.typeName?.EndsWith("TreeClip", StringComparison.Ordinal) == true;
            bool motionCurveClip = target.typeName?.EndsWith("MotionCurveClip", StringComparison.Ordinal) == true;
            bool animationSegment = target.typeName?.EndsWith("AnimationClip", StringComparison.Ordinal) == true;
            if (treeClip)
            {
                if (targetTreeClip == null)
                {
                    report.Error(path, "timeline_tree_clip_summary_missing", "TreeClip必须在controller分片提供对应的目标状态。");
                    return;
                }
                if (!string.Equals(targetTreeClip.ownership, "Inline", StringComparison.OrdinalIgnoreCase))
                {
                    report.Error(path + ".ownership", "timeline_tree_clip_inline_required", "当前正式TreeClip authoring只接受Inline ownership。");
                    return;
                }
                bool ensure = current == null ||
                              current.startFrame != target.startFrame ||
                              current.endFrame != target.endFrame ||
                              currentTreeClip == null ||
                              !string.Equals(currentTreeClip.phase, targetTreeClip.phase, StringComparison.Ordinal) ||
                              !string.Equals(currentTreeClip.ownership, targetTreeClip.ownership, StringComparison.Ordinal);
                if (ensure)
                {
                    Add(mutations, path, AgentMutationKind.EnsureTimelineTreeClip, operation =>
                    {
                        if (IsLocal(target.clipAuthoringId))
                        {
                            operation.id = LocalIdentity(target.clipAuthoringId);
                            localClipIdentities[target.clipAuthoringId] = operation.id;
                        }
                        operation.timelineAuthoringId = timelineId;
                        if (!IsLocal(track.trackAuthoringId))
                            operation.trackAuthoringId = track.trackAuthoringId;
                        operation.clipAuthoringId = IsLocal(target.clipAuthoringId) ? string.Empty : target.clipAuthoringId;
                        operation.startFrame = target.startFrame;
                        operation.endFrame = target.endFrame;
                        operation.timelinePhase = targetTreeClip.phase;
                    });
                }
                BuildTreeClipWriteMutations(
                    timelineId,
                    track.trackAuthoringId,
                    target.clipAuthoringId,
                    currentTreeClip,
                    targetTreeClip,
                    localClipIdentities,
                    mutations,
                    report,
                    path);
            }
            else if (motionCurveClip)
            {
                BuildMotionCurveClipMutations(
                    timelineId,
                    track.trackAuthoringId,
                    current,
                    target,
                    localTimelineIdentities,
                    localTrackIdentities,
                    localClipIdentities,
                    mutations,
                    path);
            }
            else if (target.motionWarpClip)
            {
                BuildMotionWarpClipMutations(
                    timelineId,
                    track.trackAuthoringId,
                    current,
                    target,
                    localTrackIdentities,
                    localClipIdentities,
                    mutations,
                    report,
                    path);
            }
            else if (animationSegment)
            {
                bool ensure = current == null ||
                              current.startFrame != target.startFrame ||
                              current.endFrame != target.endFrame ||
                              current.clipInFrame != target.clipInFrame ||
                              !SameOptionalText(current.extraPolationMode, target.extraPolationMode) ||
                              !Same(current.animationClip, target.animationClip);
                if (ensure)
                {
                    Add(mutations, path, AgentMutationKind.EnsureAnimationClipSegment, operation =>
                    {
                        if (IsLocal(target.clipAuthoringId))
                        {
                            operation.id = LocalIdentity(target.clipAuthoringId);
                            localClipIdentities[target.clipAuthoringId] = operation.id;
                        }
                        SetTimelineReference(operation, timelineId, localTimelineIdentities);
                        SetTimelineTrackReference(operation, track.trackAuthoringId, localTrackIdentities);
                        if (!IsLocal(target.clipAuthoringId))
                            operation.clipAuthoringId = target.clipAuthoringId;
                        operation.animationClip = target.animationClip;
                        operation.startFrame = target.startFrame;
                        operation.endFrame = target.endFrame;
                        operation.clipInFrame = target.clipInFrame;
                        operation.extraPolationMode = target.extraPolationMode;
                    });
                }
            }
            else if (current == null)
            {
                report.Error(path, "timeline_clip_create_unsupported", "当前Clip类型没有正式create capability。");
            }
            else
            {
                if (!SameGenericClipConfiguration(current, target))
                {
                    report.Error(path, "timeline_clip_configuration_unsupported", "该Clip的资产引用或typed配置发生变化，但当前类型没有对应的正式Mutation。");
                    return;
                }
                int currentDuration = current.endFrame - current.startFrame;
                int targetDuration = target.endFrame - target.startFrame;
                if ((current.startFrame != target.startFrame || current.endFrame != target.endFrame) &&
                    currentDuration == targetDuration)
                {
                    Add(mutations, path, AgentMutationKind.MoveTimelineClip, operation =>
                    {
                        operation.timelineAuthoringId = timelineId;
                        operation.trackAuthoringId = track.trackAuthoringId;
                        operation.clipAuthoringId = target.clipAuthoringId;
                        operation.frameOffset = target.startFrame - current.startFrame;
                    });
                }
                else if (currentDuration != targetDuration)
                {
                    report.Error(path, "timeline_clip_resize_unsupported", "当前Clip类型没有正式resize Mutation。");
                }
            }

            if ((current == null && (target.selfEaseInFrame != 0 || target.selfEaseOutFrame != 0)) ||
                current != null &&
                (current.selfEaseInFrame != target.selfEaseInFrame ||
                 current.selfEaseOutFrame != target.selfEaseOutFrame))
            {
                Add(mutations, path + ".ease", AgentMutationKind.ConfigureTimelineClipEase, operation =>
                {
                    SetTimelineReference(operation, timelineId, localTimelineIdentities);
                    SetTimelineClipReference(operation, track.trackAuthoringId, target.clipAuthoringId, localClipIdentities);
                    operation.selfEaseInFrame = target.selfEaseInFrame;
                    operation.selfEaseOutFrame = target.selfEaseOutFrame;
                });
            }

            BuildTimelineCurveMutations(
                timelineId,
                track.trackAuthoringId,
                current,
                target,
                localTimelineIdentities,
                localClipIdentities,
                mutations,
                report,
                path);
        }

        static bool SameGenericClipConfiguration(
            AgentSnapshotTimelineClip current,
            AgentSnapshotTimelineClip target)
        {
            return Same(current.animationClip, target.animationClip) &&
                   current.clipInFrame == target.clipInFrame &&
                   SameOptionalText(current.extraPolationMode, target.extraPolationMode) &&
                   SameOptionalText(current.curveId, target.curveId) &&
                   current.curveEndFrame == target.curveEndFrame &&
                   SameOptionalText(current.motionSpace, target.motionSpace) &&
                   SameOptionalText(current.motionChannel, target.motionChannel) &&
                   SameOptionalText(current.motionBlendMode, target.motionBlendMode) &&
                   current.motionPriority == target.motionPriority &&
                   current.consumeLowerChannels == target.consumeLowerChannels &&
                   SameOptionalText(current.sourceMotionClipAuthoringId, target.sourceMotionClipAuthoringId) &&
                   SameOptionalText(current.sourceMotionClipPath, target.sourceMotionClipPath) &&
                   SameOptionalText(current.translationMode, target.translationMode) &&
                   SameOptionalText(current.targetOffsetSpace, target.targetOffsetSpace) &&
                   SameOptionalText(current.rotationMode, target.rotationMode) &&
                   SameOptionalText(current.rotationMethod, target.rotationMethod) &&
                   Same(current.targetPlanarOffset, target.targetPlanarOffset) &&
                   current.targetYawOffsetDegrees.Equals(target.targetYawOffsetDegrees) &&
                   current.maxTotalPositionCorrection.Equals(target.maxTotalPositionCorrection) &&
                   current.maxTotalYawCorrectionDegrees.Equals(target.maxTotalYawCorrectionDegrees) &&
                   current.maximumYawRateDegreesPerSecond.Equals(target.maximumYawRateDegreesPerSecond) &&
                   SameOptionalText(current.limitPolicy, target.limitPolicy);
        }

        static void BuildTreeClipWriteMutations(
            string timelineId,
            string trackId,
            string clipId,
            AgentSnapshotTimelineTreeClip current,
            AgentSnapshotTimelineTreeClip target,
            IDictionary<string, string> localClipIdentities,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            List<AgentSnapshotTreeClipWrite> oldWrites = current?.writes ?? new List<AgentSnapshotTreeClipWrite>();
            List<AgentSnapshotTreeClipWrite> writes = target?.writes ?? new List<AgentSnapshotTreeClipWrite>();
            if (writes.Count > 1)
            {
                report.Error(path + ".writes", "timeline_tree_clip_writes_unsupported", "当前正式TreeClip只支持一个Bool Blackboard write。");
                return;
            }
            if (writes.Count == 0)
            {
                if (oldWrites.Count > 0)
                    report.Error(path + ".writes", "timeline_tree_clip_write_delete_unsupported", "当前正式API不支持单独删除TreeClip write。");
                return;
            }
            if (oldWrites.Count == 1 && Same(oldWrites[0], writes[0]))
                return;
            AgentSnapshotTreeClipWrite write = writes[0];
            Add(mutations, path + ".writes[0]", AgentMutationKind.EnsureTreeClipBlackboardWrite, operation =>
            {
                operation.timelineAuthoringId = timelineId;
                if (!IsLocal(trackId))
                    operation.trackAuthoringId = trackId;
                if (IsLocal(clipId))
                    operation.clipPlannedIdentity = localClipIdentities.TryGetValue(clipId, out string plannedIdentity)
                        ? plannedIdentity
                        : LocalIdentity(clipId);
                else
                    operation.clipAuthoringId = clipId;
                if (IsLocal(write.declarationId))
                    operation.declarationPlannedIdentity = LocalIdentity(write.declarationId);
                else
                    operation.declarationAuthoringId = write.declarationId;
            });
        }

        static void BuildMotionCurveClipMutations(
            string timelineId,
            string trackId,
            AgentSnapshotTimelineClip current,
            AgentSnapshotTimelineClip target,
            IReadOnlyDictionary<string, string> localTimelineIdentities,
            IReadOnlyDictionary<string, string> localTrackIdentities,
            IDictionary<string, string> localClipIdentities,
            AgentMutationDraftSet mutations,
            string path)
        {
            if (current == null || current.startFrame != target.startFrame || current.endFrame != target.endFrame)
            {
                Add(mutations, path, AgentMutationKind.EnsureMotionCurveClip, operation =>
                {
                    if (IsLocal(target.clipAuthoringId))
                    {
                        operation.id = LocalIdentity(target.clipAuthoringId);
                        localClipIdentities[target.clipAuthoringId] = operation.id;
                    }
                    SetTimelineReference(operation, timelineId, localTimelineIdentities);
                    SetTimelineTrackReference(operation, trackId, localTrackIdentities);
                    operation.clipAuthoringId = IsLocal(target.clipAuthoringId) ? string.Empty : target.clipAuthoringId;
                    operation.startFrame = target.startFrame;
                    operation.endFrame = target.endFrame;
                });
            }
            if (current == null || !SameMotionCurveConfiguration(current, target))
            {
                Add(mutations, path + ".motion", AgentMutationKind.ConfigureMotionCurveClip, operation =>
                {
                    SetTimelineReference(operation, timelineId, localTimelineIdentities);
                    SetTimelineClipReference(operation, trackId, target.clipAuthoringId, localClipIdentities);
                    operation.startFrame = target.startFrame;
                    operation.endFrame = target.endFrame;
                    operation.curveId = target.curveId;
                    operation.curveEndFrame = target.curveEndFrame;
                    operation.motionSpace = target.motionSpace;
                    operation.motionChannel = target.motionChannel;
                    operation.motionBlendMode = target.motionBlendMode;
                    operation.motionPriority = target.motionPriority;
                    operation.consumeLowerChannels = target.consumeLowerChannels;
                });
            }
        }

        static bool SameMotionCurveConfiguration(AgentSnapshotTimelineClip left, AgentSnapshotTimelineClip right)
        {
            return string.Equals(left.curveId, right.curveId, StringComparison.Ordinal) &&
                   left.curveEndFrame == right.curveEndFrame &&
                   string.Equals(left.motionSpace, right.motionSpace, StringComparison.Ordinal) &&
                   string.Equals(left.motionChannel, right.motionChannel, StringComparison.Ordinal) &&
                   string.Equals(left.motionBlendMode, right.motionBlendMode, StringComparison.Ordinal) &&
                   left.motionPriority == right.motionPriority &&
                   left.consumeLowerChannels == right.consumeLowerChannels;
        }

        static void BuildMotionWarpClipMutations(
            string timelineId,
            string trackId,
            AgentSnapshotTimelineClip current,
            AgentSnapshotTimelineClip target,
            IReadOnlyDictionary<string, string> localTrackIdentities,
            IDictionary<string, string> localClipIdentities,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            if (current == null || current.startFrame != target.startFrame || current.endFrame != target.endFrame)
            {
                Add(mutations, path, AgentMutationKind.EnsureMotionWarpClip, operation =>
                {
                    if (IsLocal(target.clipAuthoringId))
                    {
                        operation.id = LocalIdentity(target.clipAuthoringId);
                        localClipIdentities[target.clipAuthoringId] = operation.id;
                    }
                    operation.timelineAuthoringId = timelineId;
                    SetTimelineTrackReference(operation, trackId, localTrackIdentities);
                    operation.clipAuthoringId = IsLocal(target.clipAuthoringId) ? string.Empty : target.clipAuthoringId;
                    operation.startFrame = target.startFrame;
                    operation.endFrame = target.endFrame;
                });
            }
            if (current == null ||
                !string.Equals(current.sourceMotionClipAuthoringId, target.sourceMotionClipAuthoringId, StringComparison.Ordinal))
            {
                Add(mutations, path + ".source", AgentMutationKind.ConfigureMotionWarpSource, operation =>
                {
                    operation.timelineAuthoringId = timelineId;
                    SetTimelineClipReference(operation, trackId, target.clipAuthoringId, localClipIdentities);
                    operation.sourceMotionClipAuthoringId = target.sourceMotionClipAuthoringId;
                });
            }
            if (current == null || !SameMotionWarpParameters(current, target))
            {
                Add(mutations, path + ".parameters", AgentMutationKind.ConfigureMotionWarpParameters, operation =>
                {
                    operation.timelineAuthoringId = timelineId;
                    SetTimelineClipReference(operation, trackId, target.clipAuthoringId, localClipIdentities);
                    operation.translationMode = target.translationMode;
                    operation.targetOffsetSpace = target.targetOffsetSpace;
                    operation.rotationMode = target.rotationMode;
                    operation.rotationMethod = target.rotationMethod;
                    operation.targetPlanarOffset = ToVector(target.targetPlanarOffset);
                    operation.targetYawOffsetDegrees = target.targetYawOffsetDegrees;
                    operation.maxTotalPositionCorrection = target.maxTotalPositionCorrection;
                    operation.maxTotalYawCorrectionDegrees = target.maxTotalYawCorrectionDegrees;
                    operation.maximumYawRateDegreesPerSecond = target.maximumYawRateDegreesPerSecond;
                    operation.limitPolicy = target.limitPolicy;
                    operation.positionProgressCurve = CurveKeys(target, "motion-warp.position-progress");
                    operation.yawProgressCurve = CurveKeys(target, "motion-warp.yaw-progress");
                });
            }
        }

        static bool SameMotionWarpParameters(AgentSnapshotTimelineClip left, AgentSnapshotTimelineClip right)
        {
            return string.Equals(left.translationMode, right.translationMode, StringComparison.Ordinal) &&
                   string.Equals(left.targetOffsetSpace, right.targetOffsetSpace, StringComparison.Ordinal) &&
                   string.Equals(left.rotationMode, right.rotationMode, StringComparison.Ordinal) &&
                   string.Equals(left.rotationMethod, right.rotationMethod, StringComparison.Ordinal) &&
                   Same(left.targetPlanarOffset, right.targetPlanarOffset) &&
                   left.targetYawOffsetDegrees.Equals(right.targetYawOffsetDegrees) &&
                   left.maxTotalPositionCorrection.Equals(right.maxTotalPositionCorrection) &&
                   left.maxTotalYawCorrectionDegrees.Equals(right.maxTotalYawCorrectionDegrees) &&
                   left.maximumYawRateDegreesPerSecond.Equals(right.maximumYawRateDegreesPerSecond) &&
                   string.Equals(left.limitPolicy, right.limitPolicy, StringComparison.Ordinal);
        }

        static List<AgentAnimationCurveKey> CurveKeys(AgentSnapshotTimelineClip clip, string channelId)
        {
            AgentSnapshotTimelineCurveChannel channel = (clip.curveChannels ?? new List<AgentSnapshotTimelineCurveChannel>())
                .FirstOrDefault(value => string.Equals(value.channelId, channelId, StringComparison.Ordinal));
            if (channel?.keys != null && channel.keys.Count >= 2)
                return channel.keys;
            return new List<AgentAnimationCurveKey>
            {
                new AgentAnimationCurveKey { time = 0f, value = 0f },
                new AgentAnimationCurveKey { time = 1f, value = 1f }
            };
        }

        static void SetTimelineTrackReference(
            AgentMutationDraft operation,
            string trackId,
            IReadOnlyDictionary<string, string> localTrackIdentities)
        {
            if (IsLocal(trackId))
                operation.trackPlannedIdentity = localTrackIdentities.TryGetValue(trackId, out string plannedIdentity)
                    ? plannedIdentity
                    : LocalIdentity(trackId);
            else
                operation.trackAuthoringId = trackId;
        }

        static void SetTimelineReference(
            AgentMutationDraft operation,
            string timelineId,
            IReadOnlyDictionary<string, string> localTimelineIdentities)
        {
            if (IsLocal(timelineId))
                operation.timelinePlannedIdentity = localTimelineIdentities.TryGetValue(timelineId, out string plannedIdentity)
                    ? plannedIdentity
                    : LocalIdentity(timelineId);
            else
                operation.timelineAuthoringId = timelineId;
        }

        static void SetTimelineClipReference(
            AgentMutationDraft operation,
            string trackId,
            string clipId,
            IDictionary<string, string> localClipIdentities)
        {
            if (!IsLocal(trackId))
                operation.trackAuthoringId = trackId;
            if (IsLocal(clipId))
                operation.clipPlannedIdentity = localClipIdentities.TryGetValue(clipId, out string plannedIdentity)
                    ? plannedIdentity
                    : LocalIdentity(clipId);
            else
                operation.clipAuthoringId = clipId;
        }

        static void BuildTimelineCurveMutations(
            string timelineId,
            string trackId,
            AgentSnapshotTimelineClip current,
            AgentSnapshotTimelineClip target,
            IReadOnlyDictionary<string, string> localTimelineIdentities,
            IDictionary<string, string> localClipIdentities,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            string path)
        {
            var oldChannels = Index(current?.curveChannels, value => value.channelId, path + ".curves", report);
            var newChannels = Index(target.curveChannels, value => value.channelId, path + ".curves", report);
            foreach (AgentSnapshotTimelineCurveChannel channel in target.curveChannels ?? new List<AgentSnapshotTimelineCurveChannel>())
            {
                if (oldChannels.TryGetValue(channel.channelId, out AgentSnapshotTimelineCurveChannel oldChannel) &&
                    Same(oldChannel, channel))
                    continue;
                Add(mutations, $"{path}.curves[{Escape(channel.channelId)}]", AgentMutationKind.ConfigureTimelineCurveChannel, operation =>
                {
                    SetTimelineReference(operation, timelineId, localTimelineIdentities);
                    operation.trackAuthoringId = IsLocal(trackId) ? string.Empty : trackId;
                    if (IsLocal(target.clipAuthoringId))
                    {
                        operation.clipPlannedIdentity = localClipIdentities.TryGetValue(target.clipAuthoringId, out string plannedIdentity)
                            ? plannedIdentity
                            : LocalIdentity(target.clipAuthoringId);
                    }
                    else
                        operation.clipAuthoringId = target.clipAuthoringId;
                    operation.curveChannelId = channel.channelId;
                    operation.curve = new AgentAnimationCurvePayload
                    {
                        preWrapMode = channel.preWrapMode,
                        postWrapMode = channel.postWrapMode,
                        keys = channel.keys
                    };
                });
            }
            foreach (string removed in oldChannels.Keys.Except(newChannels.Keys, StringComparer.Ordinal))
                report.Error($"{path}.curves[{Escape(removed)}]", "timeline_curve_delete_unsupported", "Registered Curve Channel不能从Document删除，只能完整替换payload。");
        }
    }
}
