using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Simulation;
using TreeDesigner;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public readonly struct AnimationProducerSourceClipAuthoringEntry
    {
        public AnimationProducerSourceClipAuthoringEntry(
            string timelineAuthoringId,
            string trackAuthoringId,
            string clipAuthoringId,
            UnityEngine.AnimationClip clip)
        {
            TimelineAuthoringId = timelineAuthoringId;
            TrackAuthoringId = trackAuthoringId;
            ClipAuthoringId = clipAuthoringId;
            Clip = clip;
        }

        public string TimelineAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public string ClipAuthoringId { get; }
        public UnityEngine.AnimationClip Clip { get; }
        public string StableIdentity => $"{TimelineAuthoringId}/{TrackAuthoringId}/{ClipAuthoringId}";
    }

    public sealed class AnimationProducerAuthoringEntry
    {
        public AnimationProducerAuthoringEntry(
            AnimationProducerId producerId,
            ThirdPersonSimulation.AnimationChannelId animationChannelId,
            PoseSlotId poseSlotId,
            CharacterAuthoringTimelineEntry timeline,
            AnimationTrack track,
            AnimationProducerSourceClipAuthoringEntry[] sourceClips)
        {
            ProducerId = producerId;
            AnimationChannelId = animationChannelId;
            PoseSlotId = poseSlotId;
            Timeline = timeline;
            Track = track;
            SourceClips = sourceClips ?? Array.Empty<AnimationProducerSourceClipAuthoringEntry>();
        }

        public AnimationProducerId ProducerId { get; }
        public string ProgramProducerIdentity => ProducerId.ProgramProducerIdentity;
        public ThirdPersonSimulation.AnimationChannelId AnimationChannelId { get; }
        public PoseSlotId PoseSlotId { get; }
        public CharacterAuthoringTimelineEntry Timeline { get; }
        public AnimationTrack Track { get; }
        public IReadOnlyList<AnimationProducerSourceClipAuthoringEntry> SourceClips { get; }
        public string DisplayName => $"{Timeline.Graph.name} / {Timeline.Timeline.Name} / {Track.Name}";
    }

    public static class CharacterAnimationPresentationAuthoringService
    {
        public static IReadOnlyList<AnimationProducerAuthoringEntry> DiscoverProducers(
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definitionContext)
        {
            RequireContext(profile, definitionContext);
            var topologyErrors = new List<string>();
            CharacterAuthoringTopologyProjection topology = CharacterAuthoringTopologyProjection.Build(
                CollectCompositionRoots(definitionContext),
                topologyErrors);
            if (!topology.IsValid)
                throw new InvalidOperationException(string.Join("\n", topologyErrors));
            if (!profile.PoseGraph || profile.PoseGraph.Graph == null)
                throw new InvalidOperationException($"Animation Presentation Profile '{profile.name}' requires a Pose Graph before producer bootstrap.");

            var poseSlots = new Dictionary<ThirdPersonSimulation.AnimationChannelId, PoseSlotId>();
            for (int i = 0; i < profile.PoseGraph.Graph.PoseSlots.Count; i++)
            {
                CharacterPoseSlotDeclaration slot = profile.PoseGraph.Graph.PoseSlots[i];
                if (slot == null || !slot.AnimationChannelId.IsValid || !slot.PoseSlotId.IsValid ||
                    !poseSlots.TryAdd(slot.AnimationChannelId, slot.PoseSlotId))
                    throw new InvalidOperationException($"Animation Presentation Pose Graph has an invalid or duplicated Pose Slot declaration at index {i}.");
            }

            var entries = new Dictionary<AnimationProducerId, AnimationProducerAuthoringEntry>();
            for (int timelineIndex = 0; timelineIndex < topology.Timelines.Count; timelineIndex++)
            {
                CharacterAuthoringTimelineEntry timeline = topology.Timelines[timelineIndex];
                for (int trackIndex = 0; trackIndex < timeline.Timeline.Tracks.Count; trackIndex++)
                {
                    if (timeline.Timeline.Tracks[trackIndex] is not AnimationTrack track)
                        continue;
                    var producerId = new AnimationProducerId(timeline.Timeline.AuthoringId, track.AuthoringId);
                    if (!producerId.IsValid || !track.AnimationChannelId.IsValid)
                        throw new InvalidOperationException($"Animation Track at '{timeline.Route}' has no stable producer or Animation Channel identity.");
                    if (!poseSlots.TryGetValue(track.AnimationChannelId, out PoseSlotId poseSlotId))
                        throw new InvalidOperationException($"Animation producer '{producerId}' channel '{track.AnimationChannelId}' has no Pose Slot declaration.");
                    AnimationProducerSourceClipAuthoringEntry[] clips = CollectSourceClips(producerId, track);
                    var entry = new AnimationProducerAuthoringEntry(
                        producerId,
                        track.AnimationChannelId,
                        poseSlotId,
                        timeline,
                        track,
                        clips);
                    if (entries.TryGetValue(producerId, out AnimationProducerAuthoringEntry existing))
                    {
                        if (!ReferenceEquals(existing.Timeline.Timeline, timeline.Timeline) || !ReferenceEquals(existing.Track, track))
                            throw new InvalidOperationException($"Animation producer identity '{producerId}' resolves to multiple Timeline Track owners.");
                        continue;
                    }
                    entries.Add(producerId, entry);
                }
            }

            var result = new List<AnimationProducerAuthoringEntry>(entries.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.ProgramProducerIdentity, right.ProgramProducerIdentity));
            return result;
        }

        public static void ConfigureTimelineProducerBinding(
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definitionContext,
            AnimationProducerId producerId,
            TransitionAssetBase source)
        {
            RequireProducer(profile, definitionContext, producerId);
            if (!source || !source.IsValid)
                throw new ArgumentException("A valid Animancer source asset is required.", nameof(source));
            Undo.RecordObject(profile, "Configure Timeline Animation Producer Binding");
            AnimationProducerPresentationBinding binding = RequireBinding(profile, producerId);
            binding.ConfigureTimeline(producerId, source);
            EditorUtility.SetDirty(profile);
        }

        public static void ConfigureMotionMatchingProducerBinding(
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definitionContext,
            AnimationProducerId producerId)
        {
            RequireProducer(profile, definitionContext, producerId);
            Undo.RecordObject(profile, "Configure Motion Matching Animation Producer Binding");
            AnimationProducerPresentationBinding binding = RequireBinding(profile, producerId);
            binding.ConfigureMotionMatching(producerId);
            EditorUtility.SetDirty(profile);
        }

        public static void RemoveProducerBinding(
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definitionContext,
            AnimationProducerId producerId)
        {
            RequireProducer(profile, definitionContext, producerId);
            Undo.RecordObject(profile, "Remove Animation Producer Binding");
            var retained = new List<AnimationProducerPresentationBinding>();
            for (int i = 0; i < profile.ProducerBindings.Count; i++)
            {
                AnimationProducerPresentationBinding binding = profile.ProducerBindings[i];
                if (binding != null && !binding.ProducerId.Equals(producerId))
                    retained.Add(binding);
            }
            profile.SetProducerBindings(retained.ToArray());
            EditorUtility.SetDirty(profile);
        }

        static AnimationProducerPresentationBinding RequireBinding(
            CharacterAnimationPresentationProfile profile,
            AnimationProducerId producerId)
        {
            AnimationProducerPresentationBinding binding = profile.FindProducerBinding(producerId);
            if (binding != null)
                return binding;
            binding = new AnimationProducerPresentationBinding();
            var bindings = new List<AnimationProducerPresentationBinding>(profile.ProducerBindings) { binding };
            profile.SetProducerBindings(bindings.ToArray());
            return binding;
        }

        static AnimationProducerAuthoringEntry RequireProducer(
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definition,
            AnimationProducerId producerId)
        {
            if (!producerId.IsValid)
                throw new ArgumentException("A valid animation producer id is required.", nameof(producerId));
            IReadOnlyList<AnimationProducerAuthoringEntry> producers = DiscoverProducers(profile, definition);
            for (int i = 0; i < producers.Count; i++)
            {
                if (producers[i].ProducerId.Equals(producerId))
                    return producers[i];
            }
            throw new InvalidOperationException($"Animation producer '{producerId}' is not part of '{definition.name}'.");
        }

        static AnimationProducerSourceClipAuthoringEntry[] CollectSourceClips(
            AnimationProducerId producerId,
            AnimationTrack track)
        {
            var clips = new List<AnimationProducerSourceClipAuthoringEntry>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < track.Clips.Count; i++)
            {
                if (track.Clips[i] is not BTSMTL.Timeline.AnimationClip clip)
                    continue;
                if (string.IsNullOrWhiteSpace(clip.AuthoringId) || !identities.Add(clip.AuthoringId))
                    throw new InvalidOperationException($"Animation producer '{producerId}' has a missing or duplicated source clip identity.");
                clips.Add(new AnimationProducerSourceClipAuthoringEntry(
                    producerId.TimelineAuthoringId,
                    producerId.TrackAuthoringId,
                    clip.AuthoringId,
                    clip.Clip));
            }
            return clips.ToArray();
        }

        static IReadOnlyList<BaseTree> CollectCompositionRoots(CharacterPipelineDefinition definition)
        {
            var roots = new List<BaseTree>();
            if (definition.RootTreeAsset && definition.RootTreeAsset.Tree)
                roots.Add(definition.RootTreeAsset.Tree);
            if (!definition.EquipmentCapabilityEnabled || !definition.EquipmentProfile)
                return roots;
            for (int featureIndex = 0; featureIndex < definition.EquipmentProfile.Features.Count; featureIndex++)
            {
                CharacterEquipmentFeatureDefinition feature = definition.EquipmentProfile.Features[featureIndex];
                if (!feature)
                    continue;
                if (feature.PersistentGraph)
                    roots.Add(feature.PersistentGraph);
                for (int routeIndex = 0; routeIndex < feature.RouteImplementations.Count; routeIndex++)
                {
                    EquipmentFeatureRouteImplementation route = feature.RouteImplementations[routeIndex];
                    if (route?.InlineGraph)
                        roots.Add(route.InlineGraph);
                }
            }
            return roots;
        }

        static void RequireContext(
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definitionContext)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            if (!definitionContext)
                throw new ArgumentNullException(nameof(definitionContext));
            if (definitionContext.AnimationPresentationProfile != profile)
                throw new InvalidOperationException(
                    $"CharacterPipelineDefinition '{definitionContext.name}' does not reference Animation Presentation Profile '{profile.name}'.");
        }
    }
}
