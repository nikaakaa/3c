using System;
using System.Collections.Generic;
using Animancer;
using Animancer.TransitionLibraries;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class AnimationProducerPresentationBinding
    {
        [SerializeField] string m_TimelineAuthoringId;
        [SerializeField] string m_TrackAuthoringId;
        [SerializeField] TransitionAssetBase m_Transition;
        [SerializeField] Animancer.Easing.Function m_Easing = Animancer.Easing.Function.CubicInOut;

        public AnimationProducerId ProducerId => new AnimationProducerId(m_TimelineAuthoringId, m_TrackAuthoringId);
        public TransitionAssetBase Transition => m_Transition;
        public Easing.Function Easing => m_Easing;

        public void Configure(
            AnimationProducerId producerId,
            TransitionAssetBase transition,
            Easing.Function easing)
        {
            if (!producerId.IsValid)
                throw new ArgumentException("Animation producer id is invalid.", nameof(producerId));
            if (!transition || !transition.IsValid)
                throw new ArgumentException("Animancer transition is invalid.", nameof(transition));
            if (!Enum.IsDefined(typeof(Easing.Function), easing))
                throw new ArgumentOutOfRangeException(nameof(easing));

            m_TimelineAuthoringId = producerId.TimelineAuthoringId;
            m_TrackAuthoringId = producerId.TrackAuthoringId;
            m_Transition = transition;
            m_Easing = easing;
        }
    }

    [CreateAssetMenu(
        fileName = "CharacterAnimationPresentationProfile",
        menuName = "3C/Character/Animation Presentation Profile")]
    public sealed class CharacterAnimationPresentationProfile : ScriptableObject
    {
        [SerializeField] CharacterAnimationLayerDefinition[] m_Layers = Array.Empty<CharacterAnimationLayerDefinition>();
        [SerializeField] TransitionLibraryAsset m_TransitionLibrary;
        [SerializeField] AnimationProducerPresentationBinding[] m_ProducerBindings =
            Array.Empty<AnimationProducerPresentationBinding>();

        public IReadOnlyList<CharacterAnimationLayerDefinition> Layers =>
            m_Layers ?? Array.Empty<CharacterAnimationLayerDefinition>();
        public TransitionLibraryAsset TransitionLibrary => m_TransitionLibrary;
        public IReadOnlyList<AnimationProducerPresentationBinding> ProducerBindings =>
            m_ProducerBindings ?? Array.Empty<AnimationProducerPresentationBinding>();

        public AnimationProducerPresentationBinding FindProducerBinding(AnimationProducerId producerId)
        {
            for (int i = 0; i < ProducerBindings.Count; i++)
            {
                AnimationProducerPresentationBinding binding = ProducerBindings[i];
                if (binding != null && binding.ProducerId.Equals(producerId))
                    return binding;
            }
            return null;
        }

        public void SetProducerBindings(AnimationProducerPresentationBinding[] bindings)
        {
            m_ProducerBindings = bindings ?? Array.Empty<AnimationProducerPresentationBinding>();
        }

        public bool CollectConfigurationErrors(List<string> errors)
        {
            bool valid = true;
            if (!m_TransitionLibrary)
            {
                errors?.Add($"{name}: Animation Presentation Transition Library is missing.");
                valid = false;
            }

            IReadOnlyList<CharacterAnimationLayerDefinition> layers = Layers;
            if (layers.Count == 0)
            {
                errors?.Add($"{name}: Animation Presentation layer catalog is empty.");
                valid = false;
            }

            var layerIds = new HashSet<string>(StringComparer.Ordinal);
            var layerIndices = new HashSet<int>();
            for (int i = 0; i < layers.Count; i++)
            {
                CharacterAnimationLayerDefinition layer = layers[i];
                if (layer == null || string.IsNullOrEmpty(layer.Id) ||
                    !layerIds.Add(layer.Id) || layer.AnimancerLayerIndex < 0 ||
                    !layerIndices.Add(layer.AnimancerLayerIndex) ||
                    layer.OutputPolicy == AnimationLayerOutputPolicy.Unspecified)
                {
                    errors?.Add($"{name}: Animation Presentation layer #{i} is invalid or duplicated.");
                    valid = false;
                }
            }

            var producerIds = new HashSet<AnimationProducerId>();
            IReadOnlyList<AnimationProducerPresentationBinding> bindings = ProducerBindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                AnimationProducerPresentationBinding binding = bindings[i];
                if (binding == null || !binding.ProducerId.IsValid ||
                    !producerIds.Add(binding.ProducerId) ||
                    !binding.Transition || !binding.Transition.IsValid ||
                    !Enum.IsDefined(typeof(Easing.Function), binding.Easing))
                {
                    errors?.Add($"{name}: Animation producer binding #{i} is invalid or duplicated.");
                    valid = false;
                }
            }

            return valid;
        }
    }
}
