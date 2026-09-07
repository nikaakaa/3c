using System;
using ThirdPersonSimulation;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterPresentationEquipmentCompilationResult
    {
        public CharacterPresentationEquipmentCompilationResult(
            EquipmentVisualProjectionBinding[] visualBindings,
            IReadOnlyList<string> diagnostics)
        {
            VisualBindings = visualBindings ??
                Array.Empty<EquipmentVisualProjectionBinding>();
            Diagnostics = diagnostics ?? Array.Empty<string>();
        }

        public IReadOnlyList<EquipmentVisualProjectionBinding> VisualBindings { get; }
        public IReadOnlyList<string> Diagnostics { get; }
    }

    internal static class CharacterPresentationEquipmentCompiler
    {
        public static CharacterPresentationEquipmentCompilationResult Compile(
            CharacterEquipmentProfile gameplayProfile,
            CharacterEquipmentPresentationProfile presentationProfile)
        {
            var diagnostics = new List<string>();
            var visualBindings = Array.Empty<EquipmentVisualProjectionBinding>();
            if (!gameplayProfile && !presentationProfile)
            {
                return new CharacterPresentationEquipmentCompilationResult(
                    visualBindings,
                    diagnostics);
            }
            if (!gameplayProfile || !presentationProfile)
            {
                diagnostics.Add(
                    "Equipment Projection requires both Gameplay and Presentation Profiles.");
                return new CharacterPresentationEquipmentCompilationResult(
                    visualBindings,
                    diagnostics);
            }
            presentationProfile.CollectConfigurationErrors(
                gameplayProfile,
                diagnostics);
            visualBindings = presentationProfile.VisualBindings
                .Where(value => value != null)
                .OrderBy(value => value.VisualBindingId.Value, StringComparer.Ordinal)
                .Select(value => new EquipmentVisualProjectionBinding(value))
                .ToArray();
            var bindingIds = new HashSet<EquipmentVisualBindingId>();
            for (int i = 0; i < visualBindings.Length; i++)
            {
                if (!visualBindings[i].VisualBindingId.IsValid ||
                    !bindingIds.Add(visualBindings[i].VisualBindingId))
                {
                    diagnostics.Add(
                        $"Equipment Projection visual binding #{i} is invalid or duplicated.");
                }
            }
            for (int i = 0; i < gameplayProfile.Equipment.Count; i++)
            {
                EquipmentDefinition item = gameplayProfile.Equipment[i];
                if (item && !bindingIds.Contains(item.VisualBindingId))
                {
                    diagnostics.Add(
                        $"Equipment '{item.EquipmentIdValue}' references unresolved visual binding '{item.VisualBindingIdValue}'.");
                }
            }
            return new CharacterPresentationEquipmentCompilationResult(
                visualBindings,
                diagnostics);
        }
    }
}
