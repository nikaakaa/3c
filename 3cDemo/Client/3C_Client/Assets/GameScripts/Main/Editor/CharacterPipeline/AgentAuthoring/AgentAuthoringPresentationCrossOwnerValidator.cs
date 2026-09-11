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


namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentAuthoringPresentationCrossOwnerValidator
    {
        static string GraphPath(string id) =>
            AgentAuthoringPresentationPackageCodec.GraphDirectory(id) + "/graph.json";

        static string ProducerKey(AgentPackageAnimationProducerBinding value) =>
            value.timelineId + ":" + value.trackId;

        internal static void Validate(
            AgentDocumentEditable editable,
            AgentDocumentContext context,
            AgentCompileReport report)
        {
            AgentDocumentPresentationEditable presentation =
                editable.presentation;
            var channels = new HashSet<string>(StringComparer.Ordinal);
            var skillTimelineTracks = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillTimelineFile timeline in
                     editable.skillTimelines ?? new List<AgentPackageSkillTimelineFile>())
            {
                foreach (AgentPackageSkillTimelineTrack track in
                         timeline?.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                {
                    if (!string.IsNullOrWhiteSpace(track?.animationChannelId))
                        channels.Add(track.animationChannelId);
                    if (timeline != null && track != null)
                        skillTimelineTracks.Add(timeline.id + "\0" + track.id);
                }
            }
            var animationClips = new HashSet<string>(
                presentation.animationClips.Select(value =>
                    AgentAuthoringPresentationPackageValidator.ReferenceIdentity(value.clip)),
                StringComparer.Ordinal);
            foreach (AgentPackageSkillTimelineFile timeline in
                     editable.skillTimelines ?? new List<AgentPackageSkillTimelineFile>())
            {
                foreach (AgentPackageSkillTimelineClip clip in
                         (timeline?.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                             .SelectMany(value => value?.clips ?? new List<AgentPackageSkillTimelineClip>())
                             .Where(value => value?.kind == TimelineContractKinds.AnimationClip))
                {
                    if (!animationClips.Contains(AgentAuthoringPresentationPackageValidator.ReferenceIdentity(clip.animationClip)))
                        report.Error(
                            $"editable/skills/timelines/{timeline.id}/clips/{clip.id}.animationClip",
                            "presentation_animation_clip_unresolved",
                            "Skill Timeline Animation Segment引用不在AnimationClip目标闭包中。");
                }
            }
            var sources = new HashSet<string>(
                presentation.profile.poseSources.Select(value =>
                    AgentAuthoringPresentationPackageValidator.ReferenceIdentity(value.slot)),
                StringComparer.Ordinal);
            var resources = new HashSet<string>(
                presentation.profile.poseResources.Select(value =>
                    AgentAuthoringPresentationPackageValidator.ReferenceIdentity(value.slot)),
                StringComparer.Ordinal);
            ValidateStateMachineResourceBindings(
                presentation,
                context?.presentation,
                report);
            var graphs = new HashSet<string>(
                presentation.poseGraphs.Select(value => value.id),
                StringComparer.Ordinal);
            var sourceFields = CharacterPoseAuthoringMetadata.All
                .Where(value => value.UsesPoseSourceSlot)
                .ToDictionary(
                    value => value.CapabilityIdentity,
                    value => value.Fields.Single(field =>
                        field.PickerKind == "pose-source-slot").FieldId.Value,
                    StringComparer.Ordinal);
            var channelFields = CharacterPoseAuthoringMetadata.All
                .Where(value => value.UsesAnimationChannel)
                .ToDictionary(
                    value => value.CapabilityIdentity,
                    value => value.Fields.Single(field =>
                        field.PickerKind == "animation-channel").FieldId.Value,
                    StringComparer.Ordinal);
            var graphReferenceFields = CharacterPoseAuthoringMetadata.All
                .Where(value => value.NativeRole ==
                    CharacterPoseNativeNodeRole.Subgraph)
                .SelectMany(value => value.Fields
                    .Where(field => field.PickerKind == "pose-graph")
                    .Select(field => new
                    {
                        value.CapabilityIdentity,
                        FieldId = field.FieldId.Value
                    }))
                .ToDictionary(
                    value => value.CapabilityIdentity,
                    value => value.FieldId,
                    StringComparer.Ordinal);
            foreach (AgentPackagePoseGraphFile graph in
                     presentation.poseGraphs)
            {
                foreach (AgentPackagePoseNode node in graph.nodes)
                {
                    string path =
                        GraphPath(graph.id) + $".nodes[{node.id}]";
                    if (sourceFields.TryGetValue(
                            node.capability,
                            out string sourceFieldId) &&
                        !sources.Contains(
                            AgentAuthoringPresentationPackageValidator.ReferenceIdentity(
                                node.properties[sourceFieldId]
                                    ?.ToObject<AgentPackageObjectReference>())))
                    {
                        report.Error(
                            path + ".properties." + sourceFieldId,
                            "presentation_pose_source_unresolved",
                            "Pose节点引用的Source Slot不在Profile目标状态中。");
                    }
                    if (channelFields.TryGetValue(
                            node.capability,
                            out string channelFieldId) &&
                        !channels.Contains(
                            node.properties[channelFieldId]
                                ?.Value<string>() ??
                            string.Empty))
                    {
                        report.Error(
                            path + ".properties." + channelFieldId,
                            "presentation_animation_channel_unresolved",
                            "Pose节点引用的Animation Channel不在Timeline目标状态中。");
                    }
                    if (graphReferenceFields.TryGetValue(
                            node.capability,
                            out string graphFieldId) &&
                        !graphs.Contains(
                            node.properties[graphFieldId]?.Value<string>() ??
                            string.Empty))
                    {
                        report.Error(
                            path + ".properties." + graphFieldId,
                            "presentation_subgraph_unresolved",
                            "Pose Subgraph引用不在root-owned Graph catalog中。");
                    }
                    GraphAuthoringCapabilityDescriptor capability =
                        CharacterPoseGraphAuthoringCapabilities.Catalog.Require(
                            new GraphAuthoringCapabilityId(node.capability),
                            CharacterPoseGraphAuthoringCapabilities.Domain,
                            new GraphAuthoringDocumentRoleId(graph.role));
                    foreach (GraphAuthoringFieldDescriptor field in capability.Fields
                                 .Where(value => value.PickerKind == "pose-resource-slot"))
                    {
                        if (!resources.Contains(
                                AgentAuthoringPresentationPackageValidator.ReferenceIdentity(
                                    node.properties[field.FieldId.Value]
                                        ?.ToObject<AgentPackageObjectReference>())))
                        {
                            report.Error(
                                path + ".properties." + field.FieldId.Value,
                                "presentation_pose_resource_unresolved",
                                "Pose节点引用的Resource Slot不在Profile目标状态中。");
                        }
                    }
                }
            }
            foreach (AgentPackageAnimationProducerBinding producer in
                     presentation.profile.actionProducers)
            {
                string path =
                    "editable/presentation/profile.json.actionProducers[" +
                    ProducerKey(producer) + "]";
                if (producer.timelineId.StartsWith(
                        "local:",
                        StringComparison.Ordinal) ||
                    producer.trackId.StartsWith(
                        "local:",
                        StringComparison.Ordinal))
                {
                    report.Error(
                        path,
                        "presentation_action_producer_local_reference_invalid",
                        "Action producer只允许绑定已存在的Timeline与Animation track；当前正式Timeline Mutation不创建这两类owner。");
                }
                else if (!skillTimelineTracks.Contains(producer.timelineId + "\0" + producer.trackId))
                {
                    report.Error(
                        path,
                        "presentation_action_producer_unresolved",
                        "Action producer必须引用Timeline目标状态中的现有track。");
                }
            }
        }

        static void ValidateStateMachineResourceBindings(
            AgentDocumentPresentationEditable presentation,
            AgentDocumentPresentationContext context,
            AgentCompileReport report)
        {
            IReadOnlyList<AgentPackagePoseResourceBinding> resources =
                presentation?.profile?.poseResources ??
                new List<AgentPackagePoseResourceBinding>();
            foreach (AgentPackagePoseStateMachineFile machine in
                     presentation?.poseStateMachines ??
                     new List<AgentPackagePoseStateMachineFile>())
            {
                foreach (AgentPackagePoseTransition transition in
                         machine?.transitions ??
                         new List<AgentPackagePoseTransition>())
                {
                    string path =
                        AgentAuthoringPresentationPackageCodec.StateMachineDirectory(
                            machine?.id) +
                        "/state-machine.json.transitions[" +
                        transition?.id + "]";
                    ValidateBlendResourceBinding(
                        transition?.blendProfileAssetId,
                        "BlendProfile",
                        context?.blendProfiles,
                        resources,
                        path + ".blendProfileAssetId",
                        report);
                    ValidateBlendResourceBinding(
                        transition?.customBlendCurveAssetId,
                        "BlendCurve",
                        context?.blendCurves,
                        resources,
                        path + ".customBlendCurveAssetId",
                        report);
                }
            }
        }

        static void ValidateBlendResourceBinding(
            string resourceId,
            string expectedKind,
            IReadOnlyList<AgentDocumentBlendAssetContext> catalog,
            IReadOnlyList<AgentPackagePoseResourceBinding> resources,
            string path,
            AgentCompileReport report)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                return;
            AgentDocumentBlendAssetContext asset =
                catalog?.FirstOrDefault(value =>
                    string.Equals(value?.id, resourceId, StringComparison.Ordinal));
            if (asset == null)
            {
                report.Error(
                    path,
                    "presentation_blend_resource_unresolved",
                    $"Blend资源身份'{resourceId}'不在当前Definition的Asset Catalog中。");
                return;
            }
            AgentPackagePoseResourceBinding[] matches =
                resources.Where(value =>
                        string.Equals(
                            value?.resource?.assetGuid,
                            asset.assetGuid,
                            StringComparison.Ordinal))
                    .ToArray();
            if (matches.Length == 0)
            {
                report.Error(
                    path,
                    "presentation_pose_resource_binding_missing",
                    $"Transition引用的Blend资源'{resourceId}'必须绑定一个kind为'{expectedKind}'的Pose Resource Slot。");
                return;
            }
            if (!matches.Any(value =>
                    string.Equals(value.kind, expectedKind, StringComparison.Ordinal)))
            {
                report.Error(
                    path,
                    "presentation_pose_resource_kind_mismatch",
                    $"Transition引用的Blend资源'{resourceId}'没有kind为'{expectedKind}'的Pose Resource Slot。");
            }
        }
    }
}
