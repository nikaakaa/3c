using System;
using System.Collections.Generic;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterAnimationPresentationAuthoringService
    {
        public static void ConfigureProducerBinding(
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definitionContext,
            AnimationProducerId producerId,
            TransitionAssetBase source)
        {
            RequireContext(profile, definitionContext);
            RequireProducer(definitionContext, producerId);
            if (!source || !source.IsValid)
                throw new ArgumentException("A valid Animancer source asset is required.", nameof(source));

            Undo.RecordObject(profile, "Configure Animation Producer Binding");
            AnimationProducerPresentationBinding binding = profile.FindProducerBinding(producerId);
            if (binding == null)
            {
                binding = new AnimationProducerPresentationBinding();
                var bindings = new List<AnimationProducerPresentationBinding>(profile.ProducerBindings) { binding };
                profile.SetProducerBindings(bindings.ToArray());
            }
            binding.Configure(producerId, source);
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
                Float32CharacterPresentationContractAdapter.Create(definition.SimulationProgram.Load()));
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
