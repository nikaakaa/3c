using System;
using System.Collections.Generic;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterAnimationPresentationAuthoringService
    {
        public static void ConfigureProducerBinding(
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definitionContext,
            AnimationProducerId producerId,
            TransitionAssetBase transition,
            Easing.Function easing)
        {
            RequireContext(profile, definitionContext);
            RequireProducer(definitionContext, producerId);
            if (!transition || !transition.IsValid)
                throw new ArgumentException("A valid Animancer transition asset is required.", nameof(transition));
            if (!Enum.IsDefined(typeof(Easing.Function), easing))
                throw new ArgumentOutOfRangeException(nameof(easing));
            if (!profile.TransitionLibrary || profile.TransitionLibrary.Library == null ||
                !profile.TransitionLibrary.Library.TryGetTransition(transition.Key, out _))
                throw new InvalidOperationException("The transition must be registered in the configured Animancer TransitionLibrary.");

            Undo.RecordObject(profile, "Configure Animation Producer Binding");
            AnimationProducerPresentationBinding binding = profile.FindProducerBinding(producerId);
            if (binding == null)
            {
                binding = new AnimationProducerPresentationBinding();
                var bindings = new List<AnimationProducerPresentationBinding>(profile.ProducerBindings) { binding };
                profile.SetProducerBindings(bindings.ToArray());
            }
            binding.Configure(producerId, transition, easing);
            EditorUtility.SetDirty(profile);
        }

        public static void RemoveProducerBinding(
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definitionContext,
            AnimationProducerId producerId)
        {
            RequireContext(profile, definitionContext);
            RequireProducer(definitionContext, producerId);
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

        static void RequireProducer(CharacterPipelineDefinition definition, AnimationProducerId producerId)
        {
            if (!producerId.IsValid)
                throw new ArgumentException("A valid animation producer id is required.", nameof(producerId));

            if (!definition.SimulationProgram || !definition.PresentationProjection)
                throw new InvalidOperationException($"CharacterPipelineDefinition '{definition.name}' has no compiled Program and Presentation Projection pair.");
            CharacterPresentationProjection projection = definition.PresentationProjection.Load(
                definition.SimulationProgram.Load());
            for (int i = 0; i < projection.AnimationProducers.Count; i++)
            {
                if (projection.AnimationProducers[i].ProducerId.Equals(producerId))
                    return;
            }
            throw new InvalidOperationException($"Animation producer '{producerId}' is not part of '{definition.name}'.");
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
