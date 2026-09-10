using System;
using System.Collections.Generic;
using ThirdPersonCamera;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterPresentationProjectionCompileContext
    {
        public CharacterPresentationProjectionCompileContext(
            string ownerIdentity,
            CharacterAnimationPresentationProfile animationPresentationProfile,
            CharacterCameraProfile cameraProfile,
            CharacterEquipmentProfile equipmentProfile,
            CharacterEquipmentPresentationProfile equipmentPresentationProfile,
            string controlModuleId,
            CharacterAnimationInputContract animationInputContract,
            CharacterPresentationSemanticContract semanticContract,
            IReadOnlyList<CharacterPresentationProducerEntry> producers,
            bool hasGameplayProducerContract)
        {
            OwnerIdentity = string.IsNullOrWhiteSpace(ownerIdentity)
                ? throw new ArgumentException("Presentation owner identity is required.", nameof(ownerIdentity))
                : ownerIdentity.Trim();
            AnimationPresentationProfile = animationPresentationProfile ? animationPresentationProfile : throw new ArgumentNullException(nameof(animationPresentationProfile));
            CameraProfile = cameraProfile;
            EquipmentProfile = equipmentProfile;
            EquipmentPresentationProfile = equipmentPresentationProfile;
            ControlModuleId = controlModuleId ?? string.Empty;
            AnimationInputContract = animationInputContract ??
                throw new ArgumentNullException(nameof(animationInputContract));
            SemanticContract = semanticContract ?? throw new ArgumentNullException(nameof(semanticContract));
            Producers = producers ?? throw new ArgumentNullException(nameof(producers));
            HasGameplayProducerContract = hasGameplayProducerContract;
        }

        public string OwnerIdentity { get; }
        public CharacterAnimationPresentationProfile AnimationPresentationProfile { get; }
        public CharacterCameraProfile CameraProfile { get; }
        public CharacterEquipmentProfile EquipmentProfile { get; }
        public CharacterEquipmentPresentationProfile EquipmentPresentationProfile { get; }
        public string ControlModuleId { get; }
        public CharacterAnimationInputContract AnimationInputContract { get; }
        public CharacterPresentationSemanticContract SemanticContract { get; }
        public IReadOnlyList<CharacterPresentationProducerEntry> Producers { get; }
        public bool HasGameplayProducerContract { get; }

    }
}
