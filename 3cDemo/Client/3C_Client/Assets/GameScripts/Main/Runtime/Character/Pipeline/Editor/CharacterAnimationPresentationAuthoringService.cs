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
            CharacterPipelineDefinition definition,
            AnimationProducerId producerId,
            TransitionAssetBase transition,
            Easing.Function easing)
        {
            CharacterAnimationPresentationDefinition presentation = RequirePresentation(definition);
            RequireProducer(definition, producerId);
            if (!transition || !transition.IsValid)
                throw new ArgumentException("A valid Animancer transition asset is required.", nameof(transition));
            if (!Enum.IsDefined(typeof(Easing.Function), easing))
                throw new ArgumentOutOfRangeException(nameof(easing));
            if (!presentation.TransitionLibrary || presentation.TransitionLibrary.Library == null ||
                !presentation.TransitionLibrary.Library.TryGetTransition(transition.Key, out _))
                throw new InvalidOperationException("The transition must be registered in the configured Animancer TransitionLibrary.");

            Undo.RecordObject(definition, "Configure Animation Producer Binding");
            AnimationProducerPresentationBinding binding = presentation.FindProducerBinding(producerId);
            if (binding == null)
            {
                binding = new AnimationProducerPresentationBinding();
                var bindings = new List<AnimationProducerPresentationBinding>(presentation.ProducerBindings) { binding };
                presentation.SetProducerBindings(bindings.ToArray());
            }
            binding.Configure(producerId, transition, easing);
            EditorUtility.SetDirty(definition);
        }

        public static void RemoveProducerBinding(
            CharacterPipelineDefinition definition,
            AnimationProducerId producerId)
        {
            CharacterAnimationPresentationDefinition presentation = RequirePresentation(definition);
            Undo.RecordObject(definition, "Remove Animation Producer Binding");
            var retained = new List<AnimationProducerPresentationBinding>();
            for (int i = 0; i < presentation.ProducerBindings.Count; i++)
            {
                AnimationProducerPresentationBinding binding = presentation.ProducerBindings[i];
                if (binding != null && !binding.ProducerId.Equals(producerId))
                    retained.Add(binding);
            }
            presentation.SetProducerBindings(retained.ToArray());
            EditorUtility.SetDirty(definition);
        }

        static void RequireProducer(CharacterPipelineDefinition definition, AnimationProducerId producerId)
        {
            if (!producerId.IsValid)
                throw new ArgumentException("A valid animation producer id is required.", nameof(producerId));

            var errors = new List<string>();
            AnimationPresentationProjection projection = AnimationPresentationProjection.Build(definition.RootTree, errors);
            if (!projection.IsValid)
                throw new InvalidOperationException(string.Join("\n", errors));
            for (int i = 0; i < projection.Producers.Count; i++)
            {
                if (projection.Producers[i].ProducerId.Equals(producerId))
                    return;
            }
            throw new InvalidOperationException($"Animation producer '{producerId}' is not part of '{definition.name}'.");
        }

        static CharacterAnimationPresentationDefinition RequirePresentation(CharacterPipelineDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            if (definition.AnimationPresentation == null)
                throw new InvalidOperationException($"CharacterPipelineDefinition '{definition.name}' has no Animation Presentation Definition.");
            return definition.AnimationPresentation;
        }
    }
}
