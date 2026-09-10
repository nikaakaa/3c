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
            var graphs = new HashSet<string>(
                presentation.poseGraphs.Select(value => value.id),
                StringComparer.Ordinal);
            var sourceCapabilities = new HashSet<string>(
                CharacterPoseNodeDefinitionModule.Shared.All
                    .Where(value => value.UsesPoseSourceSlot)
                    .Select(value =>
                        value.CapabilityIdentity),
                StringComparer.Ordinal);
            var channelCapabilities = new HashSet<string>(
                CharacterPoseNodeDefinitionModule.Shared.All
                    .Where(value => value.UsesAnimationChannel)
                    .Select(value =>
                        value.CapabilityIdentity),
                StringComparer.Ordinal);
            var subgraphCapabilities = new HashSet<string>(
                CharacterPoseNodeDefinitionModule.Shared.All
                    .Where(value =>
                        value.NativeRole ==
                        CharacterPoseNativeNodeRole.Subgraph)
                    .Select(value =>
                        value.CapabilityIdentity),
                StringComparer.Ordinal);
            foreach (AgentPackagePoseGraphFile graph in
                     presentation.poseGraphs)
            {
                foreach (AgentPackagePoseNode node in graph.nodes)
                {
                    string path =
                        GraphPath(graph.id) + $".nodes[{node.id}]";
                    if (sourceCapabilities.Contains(node.capability) &&
                        !sources.Contains(
                            AgentAuthoringPresentationPackageValidator.ReferenceIdentity(
                                node.properties["pose-source-slot"]
                                    ?.ToObject<AgentPackageObjectReference>())))
                    {
                        report.Error(
                            path + ".properties.pose-source-slot",
                            "presentation_pose_source_unresolved",
                            "Pose节点引用的Source Slot不在Profile目标状态中。");
                    }
                    if (channelCapabilities.Contains(
                            node.capability) &&
                        !channels.Contains(
                            node.properties["animation-channel-id"]
                                ?.Value<string>() ??
                            string.Empty))
                    {
                        report.Error(
                            path + ".properties.animation-channel-id",
                            "presentation_animation_channel_unresolved",
                            "Pose节点引用的Animation Channel不在Timeline目标状态中。");
                    }
                    if (subgraphCapabilities.Contains(
                            node.capability) &&
                        !graphs.Contains(
                            node.properties["graph-id"]?.Value<string>() ??
                            string.Empty))
                    {
                        report.Error(
                            path + ".properties.graph-id",
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
    }
}
