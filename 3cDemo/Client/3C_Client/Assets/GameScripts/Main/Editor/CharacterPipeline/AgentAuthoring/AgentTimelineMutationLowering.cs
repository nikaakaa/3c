using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentMutationLoweringSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentTimelineMutationLowering
    {
        static bool IsNormalized(float value) => value >= 0f && value <= 1f;

        internal static AgentMutation LowerEnsureTimelineNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            if (!Enum.TryParse(operation.timelineOwnership, true, out AgentTimelineOwnership ownership) || !Enum.IsDefined(typeof(AgentTimelineOwnership), ownership))
            {
                context.Error("timelineOwnership", "timeline_ownership_invalid", $"Timeline ownership 无效：{operation.timelineOwnership}", "使用 Inline 或 Shared。");
                ownership = AgentTimelineOwnership.Inline;
            }
            string displayName = First(operation.displayName, First(operation.timeline, "Timeline"));
            var timelineAsset = new AgentAssetReference(operation.timeline, operation.timelineAssetPath, operation.timelineAssetGuid);
            var timelineTarget = new AgentTimelineTargetReference(operation.timelineAuthoringId, operation.trackAuthoringId, operation.clipAuthoringId);
            return context.IsValid
                ? new AgentEnsureTimelineNodeMutation(
                    operation.id,
                    context.Path,
                    target,
                    existing,
                    displayName,
                    First(operation.lifecycleSlot, "Root"),
                    ownership,
                    timelineAsset,
                    ReadActionContext(operation),
                    timelineTarget,
                    operation.position)
                : null;
        }

        internal static AgentMutation LowerEnsureInlineTimeline(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentElementTargetReference timelineNode = context.RequiredElement(
                operation.targetElementAuthoringId,
                operation.targetPlannedIdentity,
                "timelineNode");
            string displayName = context.RequiredText(
                operation.displayName,
                operation.timeline,
                "displayName",
                "ensure_inline_timeline 缺少 Timeline 名称。");
            return context.IsValid
                ? new AgentEnsureInlineTimelineMutation(operation.id, context.Path, timelineNode, displayName)
                : null;
        }

        internal static AgentMutation LowerEnsureTimelineTreeClip(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "ensure_timeline_tree_clip 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            if (operation.endFrame <= operation.startFrame)
                context.Error("endFrame", "timeline_clip_range_invalid", "TreeClip endFrame 必须大于 startFrame。");
            AgentPlannedIdentityReference output = context.OptionalPlannedIdentity(operation.clipPlannedIdentity, "clipPlannedIdentity", AgentMutationOutputKind.TimelineClip);
            var target = new AgentTimelineTargetReference(timeline, track, clip, output);
            return context.IsValid ? new AgentEnsureTimelineTreeClipMutation(operation.id, context.Path, target, operation.startFrame, operation.endFrame, First(operation.timelinePhase, "Decision")) : null;
        }

        internal static AgentMutation LowerEnsureMotionCurveTrack(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            ReadTimelineReference(context, operation, "ensure_motion_curve_track", out string timeline, out AgentPlannedIdentityReference timelineOutput);
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            var target = new AgentTimelineTargetReference(timeline, timelineOutput, track, default, string.Empty, default);
            return context.IsValid
                ? new AgentEnsureMotionCurveTrackMutation(operation.id, context.Path, target, First(operation.displayName, "Motion Curve"))
                : null;
        }

        internal static AgentMutation LowerEnsureMotionCurveClip(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            ReadTimelineReference(context, operation, "ensure_motion_curve_clip", out string timeline, out AgentPlannedIdentityReference timelineOutput);
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            AgentPlannedIdentityReference trackOutput = context.OptionalPlannedIdentity(operation.trackPlannedIdentity, "trackPlannedIdentity", AgentMutationOutputKind.TimelineTrack);
            if (string.IsNullOrEmpty(track) == !trackOutput.IsValid)
                context.Error("track", "motion_curve_track_reference_invalid", "ensure_motion_curve_clip 必须且只能提供 trackAuthoringId 或 trackPlannedIdentity。");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            if (operation.endFrame <= operation.startFrame)
                context.Error("endFrame", "motion_curve_clip_range_invalid", "MotionCurveClip endFrame 必须大于 startFrame。");
            var target = new AgentTimelineTargetReference(timeline, timelineOutput, track, trackOutput, clip, default);
            return context.IsValid
                ? new AgentEnsureMotionCurveClipMutation(operation.id, context.Path, target, operation.startFrame, operation.endFrame)
                : null;
        }

        internal static AgentMutation LowerConfigureMotionCurveClip(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            ReadTimelineReference(context, operation, "configure_motion_curve_clip", out string timeline, out AgentPlannedIdentityReference timelineOutput);
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            AgentPlannedIdentityReference clipOutput = context.OptionalPlannedIdentity(operation.clipPlannedIdentity, "clipPlannedIdentity", AgentMutationOutputKind.TimelineClip);
            if (string.IsNullOrEmpty(clip) == !clipOutput.IsValid)
                context.Error("clip", "motion_curve_clip_reference_invalid", "configure_motion_curve_clip 必须且只能提供 clipAuthoringId 或 clipPlannedIdentity。");
            string curveId = context.RequiredText(operation.curveId, string.Empty, "curveId", "configure_motion_curve_clip 缺少 curveId。");
            if (operation.curveEndFrame <= operation.startFrame || operation.curveEndFrame > operation.endFrame)
                context.Error("curveEndFrame", "motion_curve_end_frame_invalid", "MotionCurveClip 必须满足 startFrame < curveEndFrame <= endFrame。");
            if (!Enum.TryParse(operation.motionSpace, true, out TimelineMotionContributionSpace space) || !Enum.IsDefined(typeof(TimelineMotionContributionSpace), space))
                context.Error("motionSpace", "motion_curve_space_invalid", $"MotionCurve space 无效：{operation.motionSpace}");
            if (!Enum.TryParse(operation.motionChannel, true, out TimelineMotionChannel channel) || !Enum.IsDefined(typeof(TimelineMotionChannel), channel))
                context.Error("motionChannel", "motion_curve_channel_invalid", $"MotionCurve channel 无效：{operation.motionChannel}");
            if (!Enum.TryParse(operation.motionBlendMode, true, out TimelineMotionBlendMode blendMode) || !Enum.IsDefined(typeof(TimelineMotionBlendMode), blendMode))
                context.Error("motionBlendMode", "motion_curve_blend_mode_invalid", $"MotionCurve blend mode 无效：{operation.motionBlendMode}");
            var target = new AgentTimelineTargetReference(timeline, timelineOutput, track, default, clip, clipOutput);
            return context.IsValid
                ? new AgentConfigureMotionCurveClipMutation(
                    operation.id,
                    context.Path,
                    target,
                    curveId,
                    operation.curveEndFrame,
                    space,
                    channel,
                    blendMode,
                    operation.motionPriority,
                    operation.consumeLowerChannels)
                : null;
        }

        internal static void ReadTimelineReference(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation,
            string operationName,
            out string timeline,
            out AgentPlannedIdentityReference timelineOutput)
        {
            timeline = context.OptionalAuthoringId(operation.timelineAuthoringId, "timelineAuthoringId");
            timelineOutput = context.OptionalPlannedIdentity(operation.timelinePlannedIdentity, "timelinePlannedIdentity", AgentMutationOutputKind.Timeline);
            if (string.IsNullOrEmpty(timeline) == !timelineOutput.IsValid)
                context.Error("timeline", "timeline_reference_invalid", $"{operationName} 必须且只能提供 timelineAuthoringId 或 timelinePlannedIdentity。");
        }

        internal static AgentMutation LowerEnsureMotionWarpTrack(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "ensure_motion_warp_track 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            return context.IsValid
                ? new AgentEnsureMotionWarpTrackMutation(operation.id, context.Path, timeline, track, First(operation.displayName, "Motion Warp"))
                : null;
        }

        internal static AgentMutation LowerEnsureMotionWarpClip(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "ensure_motion_warp_clip 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            AgentPlannedIdentityReference trackOutput = context.OptionalPlannedIdentity(operation.trackPlannedIdentity, "trackPlannedIdentity", AgentMutationOutputKind.TimelineTrack);
            if (string.IsNullOrEmpty(track) == !trackOutput.IsValid)
                context.Error("track", "motion_warp_track_reference_invalid", "ensure_motion_warp_clip 必须且只能提供 trackAuthoringId 或 trackPlannedIdentity。", "引用已有 MotionWarpTrack 或前序 ensure_motion_warp_track output。");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            var target = new AgentTimelineTargetReference(timeline, track, trackOutput, clip, default);
            return context.IsValid ? new AgentEnsureMotionWarpClipMutation(operation.id, context.Path, target, operation.startFrame, operation.endFrame) : null;
        }

        internal static AgentMutation LowerConfigureMotionWarpSource(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentTimelineTargetReference target = LowerMotionWarpClipTarget(context, operation);
            string source = context.OptionalAuthoringId(
                context.RequiredText(operation.sourceMotionClipAuthoringId, string.Empty, "sourceMotionClipAuthoringId", "configure_motion_warp_source 缺少 source MotionCurve identity。"),
                "sourceMotionClipAuthoringId");
            return context.IsValid ? new AgentConfigureMotionWarpSourceMutation(operation.id, context.Path, target, source) : null;
        }

        internal static AgentMutation LowerConfigureMotionWarpParameters(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentTimelineTargetReference target = LowerMotionWarpClipTarget(context, operation);
            if (!Enum.TryParse(operation.translationMode, true, out MotionWarpTranslationMode translationMode) || !Enum.IsDefined(typeof(MotionWarpTranslationMode), translationMode))
                context.Error("translationMode", "motion_warp_translation_mode_invalid", $"MotionWarp translation mode 无效：{operation.translationMode}");
            if (!Enum.TryParse(operation.targetOffsetSpace, true, out MotionWarpTargetOffsetSpace targetOffsetSpace) || !Enum.IsDefined(typeof(MotionWarpTargetOffsetSpace), targetOffsetSpace))
                context.Error("targetOffsetSpace", "motion_warp_target_offset_space_invalid", $"MotionWarp target offset space 无效：{operation.targetOffsetSpace}");
            if (!Enum.TryParse(operation.rotationMode, true, out MotionWarpRotationMode rotationMode) || !Enum.IsDefined(typeof(MotionWarpRotationMode), rotationMode))
                context.Error("rotationMode", "motion_warp_rotation_mode_invalid", $"MotionWarp rotation mode 无效：{operation.rotationMode}");
            if (!Enum.TryParse(operation.rotationMethod, true, out MotionWarpRotationMethod rotationMethod) || !Enum.IsDefined(typeof(MotionWarpRotationMethod), rotationMethod))
                context.Error("rotationMethod", "motion_warp_rotation_method_invalid", $"MotionWarp rotation method 无效：{operation.rotationMethod}");
            if (!Enum.TryParse(operation.limitPolicy, true, out MotionWarpLimitPolicy limitPolicy) || !Enum.IsDefined(typeof(MotionWarpLimitPolicy), limitPolicy))
                context.Error("limitPolicy", "motion_warp_limit_policy_invalid", $"MotionWarp limit policy 无效：{operation.limitPolicy}");
            AnimationCurve positionCurve = LowerAnimationCurve(context, operation.positionProgressCurve, "positionProgressCurve", 2, false);
            AnimationCurve yawCurve = LowerAnimationCurve(context, operation.yawProgressCurve, "yawProgressCurve", 2, false);
            return context.IsValid
                ? new AgentConfigureMotionWarpParametersMutation(
                    operation.id,
                    context.Path,
                    target,
                    translationMode,
                    targetOffsetSpace,
                    rotationMode,
                    rotationMethod,
                    operation.targetPlanarOffset,
                    operation.targetYawOffsetDegrees,
                    operation.maxTotalPositionCorrection,
                    operation.maxTotalYawCorrectionDegrees,
                    operation.maximumYawRateDegreesPerSecond,
                    limitPolicy,
                    positionCurve,
                    yawCurve)
                : null;
        }

        internal static AgentTimelineTargetReference LowerMotionWarpClipTarget(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", $"{operation.kind} 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            AgentPlannedIdentityReference clipOutput = context.OptionalPlannedIdentity(operation.clipPlannedIdentity, "clipPlannedIdentity", AgentMutationOutputKind.TimelineClip);
            if (string.IsNullOrEmpty(clip) == !clipOutput.IsValid)
                context.Error("clip", "motion_warp_clip_reference_invalid", $"{operation.kind} 必须且只能提供 clipAuthoringId 或 clipPlannedIdentity。");
            return new AgentTimelineTargetReference(timeline, track, default, clip, clipOutput);
        }

        internal static AnimationCurve LowerAnimationCurve(
            AgentMutationPlanningContext context,
            List<AgentAnimationCurveKey> source,
            string field,
            int minimumKeyCount,
            bool requireNormalized)
        {
            if (source == null || source.Count < minimumKeyCount)
            {
                context.Error(field, "animation_curve_missing", $"{field} 至少需要 {minimumKeyCount} 个 key。");
                return null;
            }
            var keys = new Keyframe[source.Count];
            float previousTime = -1f;
            for (int i = 0; i < source.Count; i++)
            {
                AgentAnimationCurveKey value = source[i];
                if (value == null || !Enum.TryParse(value.weightedMode, true, out WeightedMode weightedMode) || !Enum.IsDefined(typeof(WeightedMode), weightedMode))
                {
                    context.Error($"{field}[{i}]", "animation_curve_key_invalid", $"{field}[{i}] 缺失或 weightedMode 无效。");
                    continue;
                }
                if (requireNormalized && (!IsNormalized(value.time) || !IsNormalized(value.value) ||
                                          value.time < previousTime))
                {
                    context.Error($"{field}[{i}]", "animation_curve_not_normalized", $"{field}[{i}] 必须按时间有序，且time/value位于[0,1]。");
                    continue;
                }
                previousTime = value.time;
                keys[i] = new Keyframe(value.time, value.value, value.inTangent, value.outTangent, value.inWeight, value.outWeight)
                {
                    weightedMode = weightedMode
                };
            }
            return context.IsValid ? new AnimationCurve(keys) : null;
        }

        internal static AgentMutation LowerDeleteTimelineClip(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "delete_timeline_clip 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(context.RequiredText(operation.clipAuthoringId, string.Empty, "clipAuthoringId", "delete_timeline_clip 缺少 Clip identity。"), "clipAuthoringId");
            return context.IsValid ? new AgentDeleteTimelineClipMutation(operation.id, context.Path, new AgentTimelineTargetReference(timeline, track, clip)) : null;
        }

        internal static AgentMutation LowerDeleteTimelineTrack(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "delete_timeline_track 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(context.RequiredText(operation.trackAuthoringId, string.Empty, "trackAuthoringId", "delete_timeline_track 缺少 Track identity。"), "trackAuthoringId");
            return context.IsValid ? new AgentDeleteTimelineTrackMutation(operation.id, context.Path, timeline, track) : null;
        }

        internal static AgentMutation LowerEnsureTimelineSection(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            ReadTimelineReference(context, operation, "ensure_timeline_section", out string timeline, out AgentPlannedIdentityReference timelineOutput);
            string section = context.OptionalAuthoringId(operation.sectionAuthoringId, "sectionAuthoringId");
            string displayName = context.RequiredText(operation.displayName, string.Empty, "displayName", "ensure_timeline_section 缺少Section名称。");
            if (!string.Equals(displayName, displayName.Trim(), StringComparison.Ordinal))
                context.Error("displayName", "timeline_section_name_invalid", "Timeline Section名称不能包含首尾空白。");
            if (operation.startFrame < 0)
                context.Error("startFrame", "timeline_section_frame_invalid", "Timeline Section frame不能小于0。");
            var target = new AgentTimelineTargetReference(timeline, timelineOutput, string.Empty, default, string.Empty, default);
            return context.IsValid
                ? new AgentEnsureTimelineSectionMutation(operation.id, context.Path, target, section, displayName, operation.startFrame)
                : null;
        }

        internal static AgentMutation LowerDeleteTimelineSection(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            ReadTimelineReference(context, operation, "delete_timeline_section", out string timeline, out AgentPlannedIdentityReference timelineOutput);
            string section = context.OptionalAuthoringId(
                context.RequiredText(operation.sectionAuthoringId, string.Empty, "sectionAuthoringId", "delete_timeline_section 缺少Section identity。"),
                "sectionAuthoringId");
            var target = new AgentTimelineTargetReference(timeline, timelineOutput, string.Empty, default, string.Empty, default);
            return context.IsValid
                ? new AgentDeleteTimelineSectionMutation(operation.id, context.Path, target, section)
                : null;
        }

        internal static AgentMutation LowerMoveTimelineClip(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "move_timeline_clip 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(context.RequiredText(operation.trackAuthoringId, string.Empty, "trackAuthoringId", "move_timeline_clip 缺少 Track identity。"), "trackAuthoringId");
            string clip = context.OptionalAuthoringId(context.RequiredText(operation.clipAuthoringId, string.Empty, "clipAuthoringId", "move_timeline_clip 缺少 Clip identity。"), "clipAuthoringId");
            if (operation.frameOffset == 0)
                context.Error("frameOffset", "timeline_clip_offset_zero", "move_timeline_clip 的 frameOffset 不能为 0。");
            return context.IsValid
                ? new AgentMoveTimelineClipMutation(operation.id, context.Path, new AgentTimelineTargetReference(timeline, track, clip), operation.frameOffset)
                : null;
        }

        internal static AgentMutation LowerConfigureTimelineClipEase(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            ReadTimelineReference(context, operation, "configure_timeline_clip_ease", out string timeline, out AgentPlannedIdentityReference timelineOutput);
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            AgentPlannedIdentityReference clipOutput = context.OptionalPlannedIdentity(operation.clipPlannedIdentity, "clipPlannedIdentity", AgentMutationOutputKind.TimelineClip);
            if (string.IsNullOrEmpty(clip) == !clipOutput.IsValid)
                context.Error("clip", "timeline_clip_reference_invalid", "configure_timeline_clip_ease 必须且只能提供 clipAuthoringId 或 clipPlannedIdentity。");
            if (operation.selfEaseInFrame < 0)
                context.Error("selfEaseInFrame", "timeline_clip_ease_negative", "selfEaseInFrame 不能小于 0。");
            if (operation.selfEaseOutFrame < 0)
                context.Error("selfEaseOutFrame", "timeline_clip_ease_negative", "selfEaseOutFrame 不能小于 0。");
            return context.IsValid
                ? new AgentConfigureTimelineClipEaseMutation(
                    operation.id,
                    context.Path,
                    new AgentTimelineTargetReference(timeline, timelineOutput, track, default, clip, clipOutput),
                    operation.selfEaseInFrame,
                    operation.selfEaseOutFrame)
                : null;
        }

        internal static AgentMutation LowerConfigureTimelineCurveChannel(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            ReadTimelineReference(context, operation, "configure_timeline_curve_channel", out string timeline, out AgentPlannedIdentityReference timelineOutput);
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            AgentPlannedIdentityReference clipOutput = context.OptionalPlannedIdentity(operation.clipPlannedIdentity, "clipPlannedIdentity", AgentMutationOutputKind.TimelineClip);
            if (string.IsNullOrEmpty(clip) == !clipOutput.IsValid)
                context.Error("clip", "timeline_clip_reference_invalid", "configure_timeline_curve_channel 必须且只能提供 clipAuthoringId 或 clipPlannedIdentity。");
            string channelId = context.RequiredText(operation.curveChannelId, string.Empty, "curveChannelId", "configure_timeline_curve_channel 缺少registered ChannelId。");
            if (!TimelineCurveChannelCatalog.TryGet(channelId, out TimelineCurveChannelDescriptor descriptor))
                context.Error("curveChannelId", "timeline_curve_channel_unknown", $"未知 Timeline Curve ChannelId：{channelId}");
            AnimationCurve curve = LowerTimelineCurvePayload(context, operation.curve, "curve");
            return context.IsValid
                ? new AgentConfigureTimelineCurveChannelMutation(
                    operation.id,
                    context.Path,
                    new AgentTimelineTargetReference(timeline, timelineOutput, track, default, clip, clipOutput),
                    descriptor.ChannelId,
                    curve)
                : null;
        }

        internal static AnimationCurve LowerTimelineCurvePayload(
            AgentMutationPlanningContext context,
            AgentAnimationCurvePayload payload,
            string field)
        {
            if (payload == null)
            {
                context.Error(field, "timeline_curve_payload_missing", "Timeline curve payload不能为空。");
                return null;
            }
            if (!Enum.TryParse(payload.preWrapMode, true, out WrapMode preWrapMode) ||
                !Enum.IsDefined(typeof(WrapMode), preWrapMode))
                context.Error($"{field}.preWrapMode", "timeline_curve_wrap_mode_invalid", $"无效preWrapMode：{payload.preWrapMode}");
            if (!Enum.TryParse(payload.postWrapMode, true, out WrapMode postWrapMode) ||
                !Enum.IsDefined(typeof(WrapMode), postWrapMode))
                context.Error($"{field}.postWrapMode", "timeline_curve_wrap_mode_invalid", $"无效postWrapMode：{payload.postWrapMode}");
            if (payload.keys == null || payload.keys.Count == 0)
            {
                context.Error($"{field}.keys", "timeline_curve_keys_missing", "Timeline curve至少需要一个key。");
                return null;
            }
            var keys = new Keyframe[payload.keys.Count];
            float previousTime = -1f;
            for (int i = 0; i < payload.keys.Count; i++)
            {
                AgentAnimationCurveKey value = payload.keys[i];
                if (value == null ||
                    !Enum.TryParse(value.weightedMode, true, out WeightedMode weightedMode) ||
                    !Enum.IsDefined(typeof(WeightedMode), weightedMode))
                {
                    context.Error($"{field}.keys[{i}]", "timeline_curve_key_invalid", "Curve key缺失或weightedMode无效。");
                    continue;
                }
                if (!IsNormalized(value.time) || value.time <= previousTime ||
                    float.IsNaN(value.value) || float.IsInfinity(value.value) ||
                    float.IsNaN(value.inWeight) || float.IsInfinity(value.inWeight) ||
                    float.IsNaN(value.outWeight) || float.IsInfinity(value.outWeight))
                {
                    context.Error($"{field}.keys[{i}]", "timeline_curve_key_payload_invalid", "Curve key必须按normalized time严格递增，value与weight必须是有限数值。");
                    continue;
                }
                previousTime = value.time;
                keys[i] = new Keyframe(value.time, value.value, value.inTangent, value.outTangent, value.inWeight, value.outWeight)
                {
                    weightedMode = weightedMode
                };
            }
            if (!context.IsValid)
                return null;
            return new AnimationCurve(keys) { preWrapMode = preWrapMode, postWrapMode = postWrapMode };
        }

        internal static AgentMutation LowerConfigureAnimationTrackChannel(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            AgentTimelineTargetReference target = LowerAnimationTrackTarget(context, operation);
            string value = context.RequiredText(
                operation.animationChannelId,
                string.Empty,
                "animationChannelId",
                "configure_animation_track_channel 缺少 AnimationChannelId。");
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                context.Error("animationChannelId", "animation_channel_id_invalid", "AnimationChannelId 不能包含首尾空白。");
            var animationChannelId = new AnimationChannelId(value);
            if (!animationChannelId.IsValid)
                context.Error("animationChannelId", "animation_channel_id_invalid", "AnimationChannelId 必须是非空稳定 identity。");
            return context.IsValid
                ? new AgentConfigureAnimationTrackChannelMutation(operation.id, context.Path, target, animationChannelId)
                : null;
        }

        internal static AgentMutation LowerEnsureAnimationClipSegment(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            ReadTimelineReference(context, operation, "ensure_animation_clip_segment", out string timeline, out AgentPlannedIdentityReference timelineOutput);
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            AgentPlannedIdentityReference clipOutput = context.OptionalPlannedIdentity(operation.clipPlannedIdentity, "clipPlannedIdentity", AgentMutationOutputKind.TimelineClip);
            if (string.IsNullOrEmpty(clip) == !clipOutput.IsValid)
                context.Error("clip", "animation_clip_segment_reference_invalid", "AnimationClip Segment必须且只能提供clipAuthoringId或clipPlannedIdentity。");
            AgentPackageObjectReference clipReference = operation.animationClip;
            bool externalClip = clipReference != null &&
                                string.IsNullOrWhiteSpace(clipReference.localId) &&
                                !string.IsNullOrWhiteSpace(clipReference.assetPath) &&
                                !string.IsNullOrWhiteSpace(clipReference.assetGuid) &&
                                clipReference.localFileId != 0;
            if (!externalClip)
                context.Error("animationClip", "animation_clip_reference_invalid", "AnimationClip Segment必须提供现有原生Clip的结构化引用。");
            if (operation.startFrame < 0 || operation.endFrame <= operation.startFrame || operation.clipInFrame < 0)
                context.Error("frames", "animation_clip_segment_frames_invalid", "AnimationClip Segment必须满足0 <= Start < End且ClipIn >= 0。");
            if (!Enum.TryParse(operation.extraPolationMode, false, out ExtraPolationMode extraPolationMode))
                context.Error("extraPolationMode", "animation_clip_segment_extrapolation_invalid", $"AnimationClip Segment Extrapolation无效：{operation.extraPolationMode}");
            return context.IsValid
                ? new AgentEnsureAnimationClipSegmentMutation(
                    operation.id,
                    context.Path,
                    new AgentTimelineTargetReference(timeline, timelineOutput, track, default, clip, clipOutput),
                    clipReference,
                    operation.startFrame,
                    operation.endFrame,
                    operation.clipInFrame,
                    extraPolationMode)
                : null;
        }

        internal static AgentTimelineTargetReference LowerAnimationTrackTarget(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(
                context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", $"{operation.kind} 缺少 Timeline identity。"),
                "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            AgentPlannedIdentityReference trackOutput = context.OptionalPlannedIdentity(operation.trackPlannedIdentity, "trackPlannedIdentity", AgentMutationOutputKind.TimelineTrack);
            if (string.IsNullOrEmpty(track) == !trackOutput.IsValid)
                context.Error("track", "animation_track_reference_invalid", $"{operation.kind} 必须且只能提供 trackAuthoringId 或 trackPlannedIdentity。");
            return new AgentTimelineTargetReference(timeline, track, trackOutput, string.Empty, default);
        }

        internal static AgentMutation LowerEnsureTreeClipBlackboardWrite(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string timeline = context.OptionalAuthoringId(context.RequiredText(operation.timelineAuthoringId, string.Empty, "timelineAuthoringId", "ensure_tree_clip_blackboard_write 缺少 Timeline identity。"), "timelineAuthoringId");
            string track = context.OptionalAuthoringId(operation.trackAuthoringId, "trackAuthoringId");
            string clip = context.OptionalAuthoringId(operation.clipAuthoringId, "clipAuthoringId");
            AgentAuthoringReference declaration = context.RequiredDeclaration(operation.declarationAuthoringId, operation.declarationPlannedIdentity, "declaration");
            AgentPlannedIdentityReference output = context.OptionalPlannedIdentity(operation.clipPlannedIdentity, "clipPlannedIdentity", AgentMutationOutputKind.TimelineClip);
            if (string.IsNullOrEmpty(operation.clipAuthoringId) && !output.IsValid)
                context.Error("clipAuthoringId", "clip_identity_missing", "ensure_tree_clip_blackboard_write 必须使用 stable Clip identity 或前序 TimelineClip output。");
            var target = new AgentTimelineTargetReference(timeline, track, clip, output);
            return context.IsValid ? new AgentEnsureTreeClipBlackboardWriteMutation(operation.id, context.Path, target, declaration) : null;
        }
    }
}
