using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
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
            CharacterAuthoringTimelineEntry timeline,
            AnimationTrack track,
            AnimationProducerSourceClipAuthoringEntry[] sourceClips)
        {
            ProducerId = producerId;
            AnimationChannelId = animationChannelId;
            Timeline = timeline;
            Track = track;
            SourceClips = sourceClips ?? Array.Empty<AnimationProducerSourceClipAuthoringEntry>();
        }

        public AnimationProducerId ProducerId { get; }
        public string ProgramProducerIdentity => ProducerId.ProgramProducerIdentity;
        public ThirdPersonSimulation.AnimationChannelId AnimationChannelId { get; }
        public CharacterAuthoringTimelineEntry Timeline { get; }
        public AnimationTrack Track { get; }
        public IReadOnlyList<AnimationProducerSourceClipAuthoringEntry> SourceClips { get; }
        public string DisplayName => $"{Timeline.Graph.name} / {Timeline.Timeline.Name} / {Track.Name}";
    }

    public static class CharacterAnimationPresentationAuthoringService
    {
        public static CharacterClipPoseSourceSlot CreateClipPoseSource(
            CharacterAnimationPresentationProfile profile,
            string displayName,
            UnityEngine.AnimationClip clip)
        {
            RequirePoseSourceContext(profile, displayName);
            _ = CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
            var slot = UnityEngine.ScriptableObject.CreateInstance<CharacterClipPoseSourceSlot>();
            slot.name = displayName.Trim();
            var binding = UnityEngine.ScriptableObject.CreateInstance<CharacterClipPoseSourceBinding>();
            binding.name = slot.name + " Binding";
            binding.Configure(slot, clip);
            CreatePoseSource(profile, slot, binding);
            return slot;
        }

        public static CharacterBlendSpacePoseSourceSlot CreateBlendSpacePoseSource(
            CharacterAnimationPresentationProfile profile,
            string displayName,
            CharacterAnimationBlendSpaceAsset blendSpace)
        {
            RequirePoseSourceContext(profile, displayName);
            var slot = UnityEngine.ScriptableObject.CreateInstance<CharacterBlendSpacePoseSourceSlot>();
            slot.name = displayName.Trim();
            var binding = UnityEngine.ScriptableObject.CreateInstance<CharacterBlendSpacePoseSourceBinding>();
            binding.name = slot.name + " Binding";
            binding.Configure(
                slot,
                blendSpace,
                profile.RigDefinition,
                ResolveFootAnalysisIdentity(profile));
            CreatePoseSource(profile, slot, binding);
            return slot;
        }

        public static CharacterMotionMatchingPoseSourceSlot CreateMotionMatchingPoseSource(
            CharacterAnimationPresentationProfile profile,
            string displayName,
            CharacterMotionMatchingProfile motionMatchingProfile,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            CharacterMotionMatchingDatabaseDefinition[] databases)
        {
            RequirePoseSourceContext(profile, displayName);
            var slot = UnityEngine.ScriptableObject.CreateInstance<CharacterMotionMatchingPoseSourceSlot>();
            slot.name = displayName.Trim();
            var binding = UnityEngine.ScriptableObject.CreateInstance<CharacterMotionMatchingPoseSourceBinding>();
            binding.name = slot.name + " Binding";
            binding.Configure(
                slot,
                motionMatchingProfile,
                profile.RigDefinition,
                searchDomainId,
                databases,
                ResolveFootAnalysisIdentity(profile));
            CreatePoseSource(profile, slot, binding);
            return slot;
        }

        public static void ConfigureClipPoseSourceBinding(
            CharacterAnimationPresentationProfile profile,
            CharacterClipPoseSourceSlot slot,
            UnityEngine.AnimationClip clip)
        {
            if (!profile || !slot)
                throw new ArgumentException("Clip Pose source context is incomplete.");
            _ = CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
            CharacterPresentationPoseSourceBinding current = profile.FindPoseSourceBinding(slot);
            Undo.RecordObject(profile, "Configure Clip Pose Source Binding");
            if (current)
                Undo.DestroyObjectImmediate(current);
            var binding = UnityEngine.ScriptableObject.CreateInstance<CharacterClipPoseSourceBinding>();
            binding.name = slot.name + " Binding";
            binding.Configure(slot, clip);
            AssetDatabase.AddObjectToAsset(binding, profile);
            CharacterPresentationPoseSourceBinding[] bindings = profile.PoseSourceBindings
                .Where(value => value && value.Slot != slot)
                .Concat(new CharacterPresentationPoseSourceBinding[] { binding })
                .ToArray();
            profile.SetPoseSourceBindings(bindings);
            EditorUtility.SetDirty(profile);
        }

        public static void ConfigureBlendSpacePoseSourceBinding(
            CharacterAnimationPresentationProfile profile,
            CharacterBlendSpacePoseSourceSlot slot,
            CharacterAnimationBlendSpaceAsset blendSpace)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            if (!slot)
                throw new ArgumentNullException(nameof(slot));
            if (!blendSpace)
                throw new ArgumentNullException(nameof(blendSpace));
            if (!profile.RigDefinition)
                throw new InvalidOperationException($"Animation Presentation Profile '{profile.name}' requires one formal Rig Definition.");
            CharacterPresentationPoseSourceBinding existing = profile.FindPoseSourceBinding(slot);
            var configured = UnityEngine.ScriptableObject.CreateInstance<CharacterBlendSpacePoseSourceBinding>();
            configured.name = existing ? existing.name : slot.name + " Binding";
            configured.Configure(
                slot,
                blendSpace,
                profile.RigDefinition,
                ResolveFootAnalysisIdentity(profile));
            SetPoseSourceBinding(profile, configured);
        }

        public static void ConfigureMotionMatchingPoseSourceBinding(
            CharacterAnimationPresentationProfile profile,
            CharacterMotionMatchingPoseSourceSlot slot,
            CharacterMotionMatchingProfile motionMatchingProfile,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            CharacterMotionMatchingDatabaseDefinition[] databases)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            if (!slot)
                throw new ArgumentNullException(nameof(slot));
            if (!motionMatchingProfile)
                throw new ArgumentNullException(nameof(motionMatchingProfile));
            CharacterPresentationPoseSourceBinding existing = profile.FindPoseSourceBinding(slot);
            var configured = UnityEngine.ScriptableObject.CreateInstance<CharacterMotionMatchingPoseSourceBinding>();
            configured.name = existing ? existing.name : slot.name + " Binding";
            configured.Configure(
                slot,
                motionMatchingProfile,
                profile.RigDefinition,
                searchDomainId,
                databases,
                ResolveFootAnalysisIdentity(profile));
            SetPoseSourceBinding(profile, configured);
        }

        public static void RenamePoseSourceSlot(
            CharacterPresentationPoseGraphAsset graph,
            CharacterPresentationPoseSourceSlot slot,
            string displayName)
        {
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                "Rename Pose Source Slot");
            transaction.Add(new RenamePoseSourceSlotMutation(
                RequireAssetOwnerId(graph),
                slot,
                displayName));
            new CharacterPresentationMutationService().Apply(
                new CharacterPoseGraphAssetMutationOwner(graph),
                transaction);
        }

        public static void RenamePoseSourceBinding(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseSourceBinding binding,
            string displayName)
        {
            string profileId = RequireAssetOwnerId(profile);
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                "Rename Pose Source Binding");
            transaction.Add(new RenameProfileSourceBindingMutation(
                profileId,
                binding,
                displayName));
            new CharacterPresentationMutationService().Apply(
                new CharacterPresentationProfileMutationOwner(profile, profileId),
                transaction);
        }

        public static void DeletePoseSource(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseSourceSlot slot)
        {
            if (!profile || !profile.PoseGraph || !slot)
                throw new ArgumentException("Pose source delete context is incomplete.");
            CharacterPresentationPoseSourceBinding binding = profile.FindPoseSourceBinding(slot);
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Delete Pose Source");
            try
            {
                var graphTransaction = new CharacterPresentationMutationTransaction(
                    Guid.NewGuid().ToString("N"),
                    "Delete Pose Source");
                graphTransaction.Add(new DeletePoseSourceSlotMutation(
                    RequireAssetOwnerId(profile.PoseGraph),
                    slot));
                new CharacterPresentationMutationService().Apply(
                    new CharacterPoseGraphAssetMutationOwner(profile.PoseGraph),
                    graphTransaction);
                if (binding)
                {
                    var profileTransaction = new CharacterPresentationMutationTransaction(
                        Guid.NewGuid().ToString("N"),
                        "Delete Pose Source");
                    profileTransaction.Add(new RemoveProfileSourceBindingMutation(
                        RequireAssetOwnerId(profile),
                        binding));
                    new CharacterPresentationMutationService().Apply(
                        new CharacterPresentationProfileMutationOwner(
                            profile,
                            RequireAssetOwnerId(profile)),
                        profileTransaction);
                }
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        static void SetPoseSourceBinding(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseSourceBinding configured)
        {
            string profileId = RequireAssetOwnerId(profile);
            bool replace = profile.FindPoseSourceBinding(configured.Slot);
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                replace ? "Configure Pose Source Binding" : "Create Pose Source Binding");
            transaction.Add(replace
                ? new SetProfileSourceBindingMutation(profileId, configured)
                : new CreateProfileSourceBindingMutation(profileId, configured));
            try
            {
                new CharacterPresentationMutationService().Apply(
                    new CharacterPresentationProfileMutationOwner(profile, profileId),
                    transaction);
            }
            catch
            {
                if (configured && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(configured)))
                    UnityEngine.Object.DestroyImmediate(configured);
                throw;
            }
        }

        static void CreatePoseSource(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseSourceSlot slot,
            CharacterPresentationPoseSourceBinding binding)
        {
            string graphId = RequireAssetOwnerId(profile.PoseGraph);
            string profileId = RequireAssetOwnerId(profile);
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create Pose Source");
            try
            {
                var graphTransaction = new CharacterPresentationMutationTransaction(
                    Guid.NewGuid().ToString("N"),
                    "Create Pose Source");
                graphTransaction.Add(new CreatePoseSourceSlotMutation(graphId, slot));
                new CharacterPresentationMutationService().Apply(
                    new CharacterPoseGraphAssetMutationOwner(profile.PoseGraph),
                    graphTransaction);
                var profileTransaction = new CharacterPresentationMutationTransaction(
                    Guid.NewGuid().ToString("N"),
                    "Create Pose Source");
                profileTransaction.Add(new CreateProfileSourceBindingMutation(profileId, binding));
                new CharacterPresentationMutationService().Apply(
                    new CharacterPresentationProfileMutationOwner(profile, profileId),
                    profileTransaction);
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                if (slot && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(slot)))
                    UnityEngine.Object.DestroyImmediate(slot);
                if (binding && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(binding)))
                    UnityEngine.Object.DestroyImmediate(binding);
                throw;
            }
        }

        static void RequirePoseSourceContext(
            CharacterAnimationPresentationProfile profile,
            string displayName)
        {
            if (!profile || !profile.PoseGraph || !profile.RigDefinition)
                throw new InvalidOperationException("Pose source requires one Profile, Pose Graph and Rig Definition.");
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Pose Source Slot name is missing.", nameof(displayName));
        }

        static string RequireAssetOwnerId(UnityEngine.Object owner)
        {
            if (!owner)
                throw new ArgumentNullException(nameof(owner));
            string path = AssetDatabase.GetAssetPath(owner);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid))
                throw new InvalidOperationException($"Asset '{owner.name}' must be saved before editing Pose sources.");
            return guid;
        }

        public static IReadOnlyList<AnimationProducerAuthoringEntry> DiscoverProducers(
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definitionContext)
        {
            RequireContext(profile, definitionContext);
            if (!profile.PoseGraph || profile.PoseGraph.Graph == null)
                throw new InvalidOperationException($"Animation Presentation Profile '{profile.name}' requires a Pose Graph before producer bootstrap.");
            return DiscoverProducerTracks(definitionContext, true);
        }

        public static IReadOnlyList<AnimationProducerAuthoringEntry> DiscoverProducerTracks(
            CharacterPipelineDefinition definitionContext)
        {
            if (!definitionContext)
                throw new ArgumentNullException(nameof(definitionContext));
            return DiscoverProducerTracks(definitionContext, false);
        }

        static IReadOnlyList<AnimationProducerAuthoringEntry> DiscoverProducerTracks(
            CharacterPipelineDefinition definitionContext,
            bool requireAnimationChannel)
        {
            var topologyErrors = new List<string>();
            CharacterAuthoringTopologyProjection topology = CharacterAuthoringTopologyProjection.Build(
                CollectCompositionRoots(definitionContext),
                topologyErrors);
            if (!topology.IsValid)
                throw new InvalidOperationException(string.Join("\n", topologyErrors));

            var entries = new Dictionary<AnimationProducerId, AnimationProducerAuthoringEntry>();
            for (int timelineIndex = 0; timelineIndex < topology.Timelines.Count; timelineIndex++)
            {
                CharacterAuthoringTimelineEntry timeline = topology.Timelines[timelineIndex];
                for (int trackIndex = 0; trackIndex < timeline.Timeline.Tracks.Count; trackIndex++)
                {
                    if (timeline.Timeline.Tracks[trackIndex] is not AnimationTrack track)
                        continue;
                    var producerId = new AnimationProducerId(timeline.Timeline.AuthoringId, track.AuthoringId);
                    if (!producerId.IsValid || requireAnimationChannel && !track.AnimationChannelId.IsValid)
                        throw new InvalidOperationException($"Animation Track at '{timeline.Route}' has no stable producer or Animation Channel identity.");
                    AnimationProducerSourceClipAuthoringEntry[] clips = CollectSourceClips(producerId, track);
                    var entry = new AnimationProducerAuthoringEntry(
                        producerId,
                        track.AnimationChannelId,
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

        internal static string ResolveFootAnalysisIdentity(CharacterAnimationPresentationProfile profile)
        {
            if (!profile ||
                profile.FootPlacementAnalysisMode != CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures ||
                !CharacterFootPlacementAnalysisSource.IsAssetGuid(profile.FootPlacementAnalysisSourceAssetGuid))
            {
                throw new InvalidOperationException("Animation producer requires one formal generated Foot Analysis Source.");
            }
            string path = AssetDatabase.GUIDToAssetPath(profile.FootPlacementAnalysisSourceAssetGuid);
            CharacterFootPlacementAnalysisSource source =
                AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(path);
            if (!source)
                throw new InvalidOperationException("Animation producer Foot Analysis Source asset is missing.");
            source.RequireValid();
            return source.AnalysisSourceId.Value;
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
