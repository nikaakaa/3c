using System;
using System.Collections.Generic;
using Animancer;
using Animancer.TransitionLibraries;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationPresentationValidationCode
    {
        PresentationMissing,
        RootTreeMissing,
        LayerMissing,
        LayerDuplicate,
        LayerInvalid,
        TransitionLibraryMissing,
        ProducerIdentityInvalid,
        ProducerDuplicate,
        ProducerLayerUnknown,
        BindingMissing,
        BindingDuplicate,
        BindingOrphan,
        BindingTransitionMissing,
        BindingTransitionInvalid,
        BindingTransitionNotInLibrary,
        BindingTransitionClipMismatch,
        BindingFadeModeUnsupported,
        BindingEasingInvalid,
        ProducerClipMissing,
        ProjectionInvalid
    }

    public readonly struct AnimationPresentationValidationIssue
    {
        public AnimationPresentationValidationIssue(
            AnimationPresentationValidationCode code,
            string message,
            AnimationProducerId producerId = default,
            string layerId = "")
        {
            Code = code;
            Message = message ?? string.Empty;
            ProducerId = producerId;
            LayerId = layerId ?? string.Empty;
        }

        public AnimationPresentationValidationCode Code { get; }
        public string Message { get; }
        public AnimationProducerId ProducerId { get; }
        public string LayerId { get; }
    }

    public readonly struct ResolvedAnimationProducerBinding
    {
        public ResolvedAnimationProducerBinding(
            AnimationProducerId producerId,
            string layerId,
            TransitionAssetBase transition,
            Easing.Function easing,
            int authoredClipCount)
        {
            ProducerId = producerId;
            LayerId = layerId ?? string.Empty;
            Transition = transition;
            Easing = easing;
            AuthoredClipCount = authoredClipCount;
        }

        public AnimationProducerId ProducerId { get; }
        public string LayerId { get; }
        public TransitionAssetBase Transition { get; }
        public Easing.Function Easing { get; }
        public int AuthoredClipCount { get; }
        public bool UsesMixer => AuthoredClipCount > 1;
        public bool IsValid => ProducerId.IsValid &&
                               !string.IsNullOrEmpty(LayerId) &&
                               Transition &&
                               Transition.IsValid &&
                               AuthoredClipCount > 0 &&
                               Enum.IsDefined(typeof(Easing.Function), Easing);
    }

    public sealed class CharacterAnimationPresentationBindingIndex
    {
        readonly Dictionary<string, ResolvedAnimationLayer> m_Layers =
            new Dictionary<string, ResolvedAnimationLayer>(StringComparer.Ordinal);
        readonly Dictionary<AnimationProducerId, ResolvedAnimationProducerBinding> m_Bindings =
            new Dictionary<AnimationProducerId, ResolvedAnimationProducerBinding>();
        readonly List<AnimationPresentationValidationIssue> m_Issues =
            new List<AnimationPresentationValidationIssue>();

        public bool IsValid { get; private set; }
        public TransitionLibraryAsset TransitionLibrary { get; private set; }
        public AnimationPresentationProjection Projection { get; private set; }
        public IReadOnlyDictionary<string, ResolvedAnimationLayer> Layers => m_Layers;
        public IReadOnlyDictionary<AnimationProducerId, ResolvedAnimationProducerBinding> Bindings => m_Bindings;
        public IReadOnlyList<AnimationPresentationValidationIssue> Issues => m_Issues;

        public bool TryGetLayer(string layerId, out ResolvedAnimationLayer layer)
        {
            return m_Layers.TryGetValue(layerId ?? string.Empty, out layer);
        }

        public bool TryGetBinding(AnimationProducerId producerId, out ResolvedAnimationProducerBinding binding)
        {
            return m_Bindings.TryGetValue(producerId, out binding);
        }

        public static CharacterAnimationPresentationBindingIndex Build(
            CharacterAnimationPresentationDefinition definition,
            BaseTree rootTree,
            List<string> errors)
        {
            var index = new CharacterAnimationPresentationBindingIndex();
            index.IsValid = index.BuildInternal(definition, rootTree, errors);
            return index;
        }

        bool BuildInternal(
            CharacterAnimationPresentationDefinition definition,
            BaseTree rootTree,
            List<string> errors)
        {
            if (definition == null)
            {
                Report(AnimationPresentationValidationCode.PresentationMissing,
                    "Animation Presentation Definition is missing.", errors);
                return false;
            }

            bool valid = CollectLayers(definition, errors);
            TransitionLibrary = definition.TransitionLibrary;
            if (!TransitionLibrary)
            {
                Report(AnimationPresentationValidationCode.TransitionLibraryMissing,
                    "Animation Presentation requires one Animancer TransitionLibraryAsset.", errors);
                valid = false;
            }

            if (rootTree == null)
            {
                Report(AnimationPresentationValidationCode.RootTreeMissing,
                    "Animation Presentation validation requires a RootTree.", errors);
                return false;
            }

            var projectionErrors = new List<string>();
            Projection = AnimationPresentationProjection.Build(rootTree, projectionErrors);
            for (int i = 0; i < projectionErrors.Count; i++)
                Report(AnimationPresentationValidationCode.ProjectionInvalid, projectionErrors[i], errors);
            valid &= Projection.IsValid;

            var projectedProducers = new Dictionary<AnimationProducerId, AnimationPresentationProducerEntry>();
            for (int i = 0; i < Projection.Producers.Count; i++)
            {
                AnimationPresentationProducerEntry producer = Projection.Producers[i];
                if (!producer.ProducerId.IsValid)
                {
                    Report(AnimationPresentationValidationCode.ProducerIdentityInvalid,
                        $"Timeline '{producer.Timeline?.Name}' contains an AnimationTrack without stable authoring identity.",
                        errors,
                        producer.ProducerId,
                        producer.LayerId);
                    valid = false;
                    continue;
                }
                if (!m_Layers.ContainsKey(producer.LayerId))
                {
                    Report(AnimationPresentationValidationCode.ProducerLayerUnknown,
                        $"Animation producer '{producer.ProducerId}' references unknown layer '{producer.LayerId}'.",
                        errors,
                        producer.ProducerId,
                        producer.LayerId);
                    valid = false;
                }
                if (projectedProducers.TryGetValue(producer.ProducerId, out AnimationPresentationProducerEntry existing))
                {
                    if (!ReferenceEquals(existing.Timeline, producer.Timeline) || !ReferenceEquals(existing.Track, producer.Track))
                    {
                        Report(AnimationPresentationValidationCode.ProducerDuplicate,
                            $"Animation producer identity '{producer.ProducerId}' is used by multiple Timeline tracks.",
                            errors,
                            producer.ProducerId,
                            producer.LayerId);
                        valid = false;
                    }
                    continue;
                }
                projectedProducers.Add(producer.ProducerId, producer);
            }

            IReadOnlyList<AnimationProducerPresentationBinding> bindings = definition.ProducerBindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                AnimationProducerPresentationBinding binding = bindings[i];
                if (binding == null || !binding.ProducerId.IsValid)
                {
                    Report(AnimationPresentationValidationCode.ProducerIdentityInvalid,
                        $"Animation producer binding #{i} has an invalid producer identity.", errors);
                    valid = false;
                    continue;
                }
                if (!projectedProducers.TryGetValue(binding.ProducerId, out AnimationPresentationProducerEntry producer))
                {
                    Report(AnimationPresentationValidationCode.BindingOrphan,
                        $"Animation producer binding '{binding.ProducerId}' does not resolve to a Timeline AnimationTrack.",
                        errors,
                        binding.ProducerId);
                    valid = false;
                    continue;
                }
                if (m_Bindings.ContainsKey(binding.ProducerId))
                {
                    Report(AnimationPresentationValidationCode.BindingDuplicate,
                        $"Animation producer '{binding.ProducerId}' has multiple presentation bindings.",
                        errors,
                        binding.ProducerId,
                        producer.LayerId);
                    valid = false;
                    continue;
                }
                if (!binding.Transition)
                {
                    Report(AnimationPresentationValidationCode.BindingTransitionMissing,
                        $"Animation producer '{binding.ProducerId}' has no Animancer transition source.",
                        errors,
                        binding.ProducerId,
                        producer.LayerId);
                    valid = false;
                    continue;
                }
                if (!binding.Transition.IsValid || binding.Transition.Key == null)
                {
                    Report(AnimationPresentationValidationCode.BindingTransitionInvalid,
                        $"Animation producer '{binding.ProducerId}' has an invalid Animancer transition source.",
                        errors,
                        binding.ProducerId,
                        producer.LayerId);
                    valid = false;
                    continue;
                }
                if (!Enum.IsDefined(typeof(Easing.Function), binding.Easing))
                {
                    Report(AnimationPresentationValidationCode.BindingEasingInvalid,
                        $"Animation producer '{binding.ProducerId}' has an invalid Animancer easing.",
                        errors,
                        binding.ProducerId,
                        producer.LayerId);
                    valid = false;
                    continue;
                }
                if (binding.Transition.FadeMode == FadeMode.FromStart ||
                    binding.Transition.FadeMode == FadeMode.NormalizedFromStart)
                {
                    Report(AnimationPresentationValidationCode.BindingFadeModeUnsupported,
                        $"Animation producer '{binding.ProducerId}' cannot use {binding.Transition.FadeMode} because Timeline owns playback time.",
                        errors,
                        binding.ProducerId,
                        producer.LayerId);
                    valid = false;
                    continue;
                }
                if (TransitionLibrary &&
                    (TransitionLibrary.Library == null ||
                     !TransitionLibrary.Library.TryGetTransition(binding.Transition.Key, out _)))
                {
                    Report(AnimationPresentationValidationCode.BindingTransitionNotInLibrary,
                        $"Animation producer '{binding.ProducerId}' transition is not registered in the configured Animancer TransitionLibrary.",
                        errors,
                        binding.ProducerId,
                        producer.LayerId);
                    valid = false;
                    continue;
                }

                int authoredClipCount = CountAnimationClips(producer.Track);
                if (authoredClipCount == 0)
                {
                    Report(AnimationPresentationValidationCode.ProducerClipMissing,
                        $"Animation producer '{binding.ProducerId}' contains no valid animation clips.",
                        errors,
                        binding.ProducerId,
                        producer.LayerId);
                    valid = false;
                    continue;
                }
                if (!TransitionMatchesTrack(binding.Transition, producer.Track))
                {
                    Report(AnimationPresentationValidationCode.BindingTransitionClipMismatch,
                        $"Animation producer '{binding.ProducerId}' transition clips do not match its Timeline clips.",
                        errors,
                        binding.ProducerId,
                        producer.LayerId);
                    valid = false;
                    continue;
                }

                m_Bindings.Add(binding.ProducerId, new ResolvedAnimationProducerBinding(
                    binding.ProducerId,
                    producer.LayerId,
                    binding.Transition,
                    binding.Easing,
                    authoredClipCount));
            }

            foreach (KeyValuePair<AnimationProducerId, AnimationPresentationProducerEntry> pair in projectedProducers)
            {
                if (m_Bindings.ContainsKey(pair.Key))
                    continue;
                Report(AnimationPresentationValidationCode.BindingMissing,
                    $"Animation producer '{pair.Key}' requires one Animancer transition binding.",
                    errors,
                    pair.Key,
                    pair.Value.LayerId);
                valid = false;
            }

            return valid;
        }

        static int CountAnimationClips(AnimationTrack track)
        {
            int count = 0;
            if (track == null)
                return count;
            for (int i = 0; i < track.Clips.Count; i++)
            {
                if (track.Clips[i] is BTSMTL.Timeline.AnimationClip clip && clip.Clip)
                    count++;
            }
            return count;
        }

        static bool TransitionMatchesTrack(TransitionAssetBase transition, AnimationTrack track)
        {
            var transitionClips = new List<UnityEngine.AnimationClip>();
            transition.GetAnimationClips(transitionClips);
            var expected = new HashSet<UnityEngine.AnimationClip>();
            for (int i = 0; i < track.Clips.Count; i++)
            {
                if (track.Clips[i] is BTSMTL.Timeline.AnimationClip clip && clip.Clip)
                    expected.Add(clip.Clip);
            }
            var actual = new HashSet<UnityEngine.AnimationClip>();
            for (int i = 0; i < transitionClips.Count; i++)
            {
                if (transitionClips[i])
                    actual.Add(transitionClips[i]);
            }
            return expected.SetEquals(actual);
        }

        bool CollectLayers(CharacterAnimationPresentationDefinition definition, List<string> errors)
        {
            IReadOnlyList<CharacterAnimationLayerDefinition> layers = definition.Layers;
            if (layers.Count == 0)
            {
                Report(AnimationPresentationValidationCode.LayerMissing,
                    "Animation Presentation requires at least one layer.", errors);
                return false;
            }

            bool valid = true;
            var animancerIndices = new HashSet<int>();
            for (int i = 0; i < layers.Count; i++)
            {
                CharacterAnimationLayerDefinition layer = layers[i];
                if (layer == null || string.IsNullOrEmpty(layer.Id))
                {
                    Report(AnimationPresentationValidationCode.LayerMissing,
                        $"Animation Presentation layer #{i} is missing or has no LayerId.", errors);
                    valid = false;
                    continue;
                }
                if (m_Layers.ContainsKey(layer.Id))
                {
                    Report(AnimationPresentationValidationCode.LayerDuplicate,
                        $"Animation Presentation contains duplicate LayerId '{layer.Id}'.", errors, layerId: layer.Id);
                    valid = false;
                    continue;
                }
                if (layer.AnimancerLayerIndex < 0 ||
                    !animancerIndices.Add(layer.AnimancerLayerIndex) ||
                    !Enum.IsDefined(typeof(AnimationBlendMode), layer.BlendMode) ||
                    !Enum.IsDefined(typeof(AnimationLayerOutputPolicy), layer.OutputPolicy) ||
                    layer.OutputPolicy == AnimationLayerOutputPolicy.Unspecified)
                {
                    Report(AnimationPresentationValidationCode.LayerInvalid,
                        $"Animation layer '{layer.Id}' has invalid runtime configuration.", errors, layerId: layer.Id);
                    valid = false;
                    continue;
                }

                m_Layers.Add(layer.Id, new ResolvedAnimationLayer(
                    layer.Id,
                    layer.AnimancerLayerIndex,
                    layer.AvatarMask,
                    layer.BlendMode,
                    layer.OutputPolicy,
                    i));
            }
            return valid;
        }

        void Report(
            AnimationPresentationValidationCode code,
            string message,
            List<string> errors,
            AnimationProducerId producerId = default,
            string layerId = "")
        {
            var issue = new AnimationPresentationValidationIssue(code, message, producerId, layerId);
            m_Issues.Add(issue);
            errors?.Add(issue.Message);
        }
    }

    public sealed class AnimationPresentationProjection
    {
        readonly List<AnimationPresentationProducerEntry> m_Producers = new List<AnimationPresentationProducerEntry>();

        public bool IsValid { get; private set; }
        public CharacterAuthoringTopologyProjection Topology { get; private set; }
        public IReadOnlyList<AnimationPresentationProducerEntry> Producers => m_Producers;

        public static AnimationPresentationProjection Build(BaseTree rootTree, List<string> errors)
        {
            var projection = new AnimationPresentationProjection();
            projection.Topology = CharacterAuthoringTopologyProjection.Build(rootTree, errors);
            projection.IsValid = projection.Topology.IsValid;
            if (!projection.IsValid)
                return projection;

            for (int i = 0; i < projection.Topology.Timelines.Count; i++)
                projection.CollectAnimationProducers(projection.Topology.Timelines[i]);
            return projection;
        }

        void CollectAnimationProducers(CharacterAuthoringTimelineEntry source)
        {
            for (int trackIndex = 0; trackIndex < source.Timeline.Tracks.Count; trackIndex++)
            {
                if (source.Timeline.Tracks[trackIndex] is not AnimationTrack track)
                    continue;
                m_Producers.Add(new AnimationPresentationProducerEntry(
                    source.Route,
                    source.Graph,
                    source.Node,
                    source.Timeline,
                    track,
                    new AnimationProducerId(source.Timeline.AuthoringId, track.AuthoringId),
                    track.LayerId));
            }
        }
    }

    public readonly struct AnimationPresentationProducerEntry
    {
        public AnimationPresentationProducerEntry(
            TreeAuthoringRouteId route,
            BaseGraph graph,
            TimelineNode node,
            TimelineData timeline,
            AnimationTrack track,
            AnimationProducerId producerId,
            string layerId)
        {
            Route = route;
            Graph = graph;
            Node = node;
            Timeline = timeline;
            Track = track;
            ProducerId = producerId;
            LayerId = layerId ?? string.Empty;
        }

        public TreeAuthoringRouteId Route { get; }
        public BaseGraph Graph { get; }
        public TimelineNode Node { get; }
        public TimelineData Timeline { get; }
        public AnimationTrack Track { get; }
        public AnimationProducerId ProducerId { get; }
        public string LayerId { get; }
    }

}
