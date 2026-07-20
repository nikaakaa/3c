using System;
using System.Collections.Generic;
using Animancer;
using Animancer.TransitionLibraries;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationPresentationValidationCode
    {
        PresentationMissing,
        ProgramMissing,
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
        BindingFadeModeUnsupported,
        BindingEasingInvalid,
        BindingMarkerSyncInvalid,
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
        public CharacterPresentationProjection Projection { get; private set; }
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
            CharacterPresentationProjection projection,
            CharacterSimulationProgram program,
            List<string> errors)
        {
            var index = new CharacterAnimationPresentationBindingIndex();
            index.IsValid = index.BuildInternal(
                projection,
                program,
                program == null ? null : CharacterPresentationProgramIdentity.From(program),
                errors);
            return index;
        }

        public static CharacterAnimationPresentationBindingIndex Build(
            CharacterPresentationProjection projection,
            CharacterPresentationProgramIdentity program,
            List<string> errors)
        {
            var index = new CharacterAnimationPresentationBindingIndex();
            index.IsValid = index.BuildInternal(projection, null, program, errors);
            return index;
        }

        bool BuildInternal(
            CharacterPresentationProjection projection,
            CharacterSimulationProgram exactProgram,
            CharacterPresentationProgramIdentity program,
            List<string> errors)
        {
            if (projection == null)
            {
                Report(AnimationPresentationValidationCode.PresentationMissing,
                    "Character Presentation Projection is missing.", errors);
                return false;
            }
            if (program == null)
            {
                Report(AnimationPresentationValidationCode.ProgramMissing,
                    "Animation Presentation validation requires a compiled Character Simulation Program.", errors);
                return false;
            }
            try
            {
                if (exactProgram != null)
                    projection.RequireProgram(exactProgram);
                else
                    projection.RequireSemanticProgram(program);
            }
            catch (Exception exception)
            {
                Report(AnimationPresentationValidationCode.ProjectionInvalid, exception.Message, errors);
                return false;
            }

            Projection = projection;
            bool valid = CollectLayers(projection.Layers, errors);
            TransitionLibrary = projection.TransitionLibrary;
            if (!TransitionLibrary || TransitionLibrary.Library == null)
            {
                Report(AnimationPresentationValidationCode.TransitionLibraryMissing,
                    "Character Presentation Projection requires one Animancer TransitionLibraryAsset.", errors);
                valid = false;
            }

            if (projection.Producers.Count != program.ProducerIdentities.Count)
            {
                Report(AnimationPresentationValidationCode.ProjectionInvalid,
                    "Character Presentation Projection producer count does not match the Program manifest.", errors);
                valid = false;
            }
            var producerIds = new HashSet<AnimationProducerId>();
            var markerPairSets = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            for (int i = 0; i < projection.Producers.Count; i++)
            {
                CharacterPresentationProducerEntry producer = projection.Producers[i];
                if (producer == null || producer.ProgramProducerIndex != i || i >= program.ProducerIdentities.Count ||
                    !string.Equals(producer.ProgramProducerIdentity, program.ProducerIdentities[i], StringComparison.Ordinal))
                {
                    Report(AnimationPresentationValidationCode.ProjectionInvalid,
                        $"Character Presentation Projection producer #{i} does not match the Program manifest.", errors);
                    valid = false;
                    continue;
                }
                if (producer.Kind != CharacterPresentationProducerKind.Animation)
                    continue;
                AnimationProducerId producerId = producer.ProducerId;
                CharacterPresentationAnimationBinding animation = producer.Animation;
                if (!producerId.IsValid || !producerIds.Add(producerId))
                {
                    Report(AnimationPresentationValidationCode.ProducerIdentityInvalid,
                        $"Animation producer '{producer.ProgramProducerIdentity}' has an invalid or duplicate identity.", errors, producerId, producer.LayerId);
                    valid = false;
                    continue;
                }
                if (!m_Layers.ContainsKey(producer.LayerId))
                {
                    Report(AnimationPresentationValidationCode.ProducerLayerUnknown,
                        $"Animation producer '{producerId}' references unknown layer '{producer.LayerId}'.", errors, producerId, producer.LayerId);
                    valid = false;
                    continue;
                }
                if (animation == null || !animation.Transition || !animation.Transition.IsValid ||
                    animation.Transition.Key == null || animation.Clips.Count == 0)
                {
                    Report(AnimationPresentationValidationCode.BindingTransitionInvalid,
                        $"Animation producer '{producerId}' has an invalid compiled resource binding.", errors, producerId, producer.LayerId);
                    valid = false;
                    continue;
                }
                AnimationMarkerSyncBinding markerSync = animation.MarkerSync;
                if (markerSync == null)
                {
                    Report(AnimationPresentationValidationCode.BindingMarkerSyncInvalid,
                        $"Animation producer '{producerId}' has invalid marker sync data: Compiled marker sync binding is missing.", errors, producerId, producer.LayerId);
                    valid = false;
                    continue;
                }
                if (!markerSync.TryValidate(out string markerError))
                {
                    Report(AnimationPresentationValidationCode.BindingMarkerSyncInvalid,
                        $"Animation producer '{producerId}' has invalid marker sync data: {markerError}", errors, producerId, producer.LayerId);
                    valid = false;
                    continue;
                }
                if (markerSync.IsMarkerGroup)
                {
                    string markerGroupKey = producer.LayerId + "\0" + markerSync.CanonicalGroupId;
                    var directedPairs = new HashSet<string>(StringComparer.Ordinal);
                    for (int segmentIndex = 0; segmentIndex < markerSync.Segments.Count; segmentIndex++)
                    {
                        AnimationMarkerSyncSegmentOccurrence segment = markerSync.Segments[segmentIndex];
                        directedPairs.Add(AnimationMarkerSyncAuthoring.PairKey(
                            segment.PreviousMarkerId,
                            segment.NextMarkerId));
                    }
                    if (markerPairSets.TryGetValue(markerGroupKey, out HashSet<string> expectedPairs))
                    {
                        if (!expectedPairs.SetEquals(directedPairs))
                        {
                            Report(AnimationPresentationValidationCode.BindingMarkerSyncInvalid,
                                $"Animation producer '{producerId}' does not match directed marker pairs for layer/group '{producer.LayerId}/{markerSync.CanonicalGroupId}'.",
                                errors,
                                producerId,
                                producer.LayerId);
                            valid = false;
                            continue;
                        }
                    }
                    else
                    {
                        markerPairSets.Add(markerGroupKey, directedPairs);
                    }
                }
                if (!Enum.IsDefined(typeof(Easing.Function), animation.Easing) ||
                    animation.Transition.FadeMode == FadeMode.FromStart ||
                    animation.Transition.FadeMode == FadeMode.NormalizedFromStart ||
                    !TransitionLibrary.Library.TryGetTransition(animation.Transition.Key, out _))
                {
                    Report(AnimationPresentationValidationCode.BindingTransitionInvalid,
                        $"Animation producer '{producerId}' compiled transition policy is invalid.", errors, producerId, producer.LayerId);
                    valid = false;
                    continue;
                }
                m_Bindings.Add(producerId, new ResolvedAnimationProducerBinding(
                    producerId,
                    producer.LayerId,
                    animation.Transition,
                    animation.Easing,
                    animation.Clips.Count));
            }

            return valid;
        }

        bool CollectLayers(IReadOnlyList<CharacterAnimationLayerDefinition> layers, List<string> errors)
        {
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

}
