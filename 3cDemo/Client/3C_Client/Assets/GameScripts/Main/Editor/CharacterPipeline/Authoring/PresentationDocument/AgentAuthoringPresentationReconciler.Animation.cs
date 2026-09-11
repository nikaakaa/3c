using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;
using AnimationClip = UnityEngine.AnimationClip;


using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation
{
    public sealed partial class AgentAuthoringPresentationReconciler
    {
        static IReadOnlyList<AgentAnimationClipCurveMutationPlan> BuildAnimationClipCurvePlan(
            AgentDocumentPresentationEditable current,
            AgentDocumentPresentationEditable target,
            PlanBuilder builder,
            AgentCompileReport report)
        {
            var result = new List<AgentAnimationClipCurveMutationPlan>();
            var locomotionMembers = new HashSet<string>(
                (target.profile?.locomotionSyncGroups ??
                 new List<AgentPackageLocomotionSyncGroup>())
                .SelectMany(value => value.members ??
                    new List<AgentPackageObjectReference>())
                .Select(ReferenceIdentity),
                StringComparer.Ordinal);
            Dictionary<string, AgentPackageAnimationClipCurvesFile> currentById =
                (current.animationClips ?? new List<AgentPackageAnimationClipCurvesFile>())
                .ToDictionary(value => value.id, StringComparer.Ordinal);
            Dictionary<string, AgentPackageAnimationClipCurvesFile> targetById =
                (target.animationClips ?? new List<AgentPackageAnimationClipCurvesFile>())
                .ToDictionary(value => value.id, StringComparer.Ordinal);
            if (!currentById.Keys.OrderBy(value => value, StringComparer.Ordinal)
                    .SequenceEqual(targetById.Keys.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal))
            {
                report.Error(
                    "editable/animation-clips",
                    "animation_clip_curve_closure_changed",
                    "Document不能创建、删除或替换Definition闭包中的AnimationClip分片。");
                return result;
            }
            foreach (KeyValuePair<string, AgentPackageAnimationClipCurvesFile> pair in
                     targetById.OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                AgentPackageAnimationClipCurvesFile previous = currentById[pair.Key];
                AgentPackageAnimationClipCurvesFile next = pair.Value;
                AnimationClip clip = Resolve<AnimationClip>(
                    next.clip,
                    $"editable/animation-clips/{pair.Key}/curves.json.clip",
                    report);
                if (!clip)
                    continue;
                CharacterAnimationClipContentIdentity identity;
                try
                {
                    identity = CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
                }
                catch (Exception exception)
                {
                    report.Error(
                        $"editable/animation-clips/{pair.Key}/curves.json",
                        "animation_clip_identity_invalid",
                        exception.Message);
                    continue;
                }
                if (!string.Equals(next.dependencyBaseline, identity.FullDependencyHash, StringComparison.Ordinal) ||
                    !string.Equals(next.analysisInputHash, identity.AnalysisInputHash, StringComparison.Ordinal) ||
                    !string.Equals(next.registeredCurveHash, identity.RegisteredCurveHash, StringComparison.Ordinal))
                {
                    report.Error(
                        $"editable/animation-clips/{pair.Key}/curves.json",
                        "animation_clip_dependency_changed",
                        "AnimationClip dependency baseline或Analysis Input Hash已经变化，必须重新checkout。");
                    continue;
                }
                var channelIds = new HashSet<string>(StringComparer.Ordinal);
                for (int curveIndex = 0; curveIndex < next.curves.Count; curveIndex++)
                {
                    AgentPackageCurve curve = next.curves[curveIndex];
                    try
                    {
                        _ = CharacterAnimationClipRegisteredCurveCatalog.Require(curve.channelId);
                        if (!channelIds.Add(curve.channelId))
                            throw new InvalidOperationException($"AnimationClip Curve channel '{curve.channelId}' is duplicated.");
                        CharacterAnimationClipRegisteredCurveCatalog.Validate(
                            clip,
                            curve.channelId,
                            ConvertCurve(curve));
                    }
                    catch (Exception exception)
                    {
                        report.Error(
                            $"editable/animation-clips/{pair.Key}/curves.json.curves[{curveIndex}]",
                            "animation_clip_curve_channel_invalid",
                        exception.Message);
                    }
                }
                int footMotionCount = CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels.Count(
                    value => channelIds.Contains(value.ChannelId));
                if (footMotionCount != 0 &&
                    footMotionCount != CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels.Count)
                {
                    report.Error(
                        $"editable/animation-clips/{pair.Key}/curves.json",
                        "animation_clip_foot_motion_group_incomplete",
                        "Foot Motion Curve必须以左右脚22条完整数据组提交。");
                }
                if (!channelIds.Contains(
                        CharacterAnimationClipRegisteredCurveChannels.FootPlacementWeight))
                {
                    report.Error(
                        $"editable/animation-clips/{pair.Key}/curves.json",
                        "animation_clip_foot_weight_required",
                        "当前Definition可达AnimationClip必须保留Foot Placement Weight注册Curve。");
                }
                bool hasPhase = channelIds.Contains(
                    CharacterAnimationClipRegisteredCurveChannels.LocomotionPhase);
                bool requiresPhase = locomotionMembers.Contains(pair.Key);
                if (hasPhase != requiresPhase)
                {
                    report.Error(
                        $"editable/animation-clips/{pair.Key}/curves.json",
                        "animation_clip_phase_group_mismatch",
                        requiresPhase
                            ? "Locomotion Sync Group成员必须具有Locomotion Phase注册Curve。"
                            : "非Locomotion Sync Group成员不得保留无消费Locomotion Phase注册Curve。");
                }
                if (!JToken.DeepEquals(
                        AgentAuthoringDocumentCodec.ToToken(previous),
                        AgentAuthoringDocumentCodec.ToToken(next)))
                {
                    result.Add(new AgentAnimationClipCurveMutationPlan(clip, next));
                    builder.Direct(
                        "ReplaceAnimationClipCurves",
                        identity.AssetGuid,
                        $"editable/animation-clips/{pair.Key}/curves.json",
                        string.Join(",", next.curves
                            .Select(value => value.channelId)
                            .OrderBy(value => value, StringComparer.Ordinal)));
                }
            }
            return result;
        }

        static CharacterLocomotionSyncGroup[] BuildLocomotionSyncGroups(
            AgentPackagePresentationProfileFile profile,
            AgentCompileReport report)
        {
            var result = new List<CharacterLocomotionSyncGroup>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var members = new HashSet<AnimationClip>();
            foreach (AgentPackageLocomotionSyncGroup source in
                     profile.locomotionSyncGroups ?? new List<AgentPackageLocomotionSyncGroup>())
            {
                string path = $"editable/presentation/profile.json.locomotionSyncGroups[{source?.groupId}]";
                if (source == null || string.IsNullOrWhiteSpace(source.groupId) || !ids.Add(source.groupId))
                {
                    report.Error(path, "locomotion_sync_group_identity_invalid", "Locomotion Sync Group identity缺失或重复。");
                    continue;
                }
                var clips = new List<AnimationClip>();
                for (int i = 0; i < (source.members?.Count ?? 0); i++)
                {
                    AnimationClip clip = Resolve<AnimationClip>(source.members[i], path + $".members[{i}]", report);
                    if (!clip || !members.Add(clip))
                    {
                        report.Error(path + $".members[{i}]", "locomotion_sync_group_member_invalid", "AnimationClip缺失或属于多个Locomotion Sync Group。");
                        continue;
                    }
                    clips.Add(clip);
                }
                try
                {
                    result.Add(new CharacterLocomotionSyncGroup(source.groupId, clips.ToArray()));
                }
                catch (Exception exception)
                {
                    report.Error(path, "locomotion_sync_group_invalid", exception.Message);
                }
            }
            return result.ToArray();
        }
    }
}
