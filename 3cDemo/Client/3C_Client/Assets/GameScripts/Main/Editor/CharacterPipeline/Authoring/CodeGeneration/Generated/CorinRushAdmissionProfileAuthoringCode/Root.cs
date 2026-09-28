using System;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAdmissionProfileAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            parts.admissionProfile = BtsmtlSkillAuthoringCode.EnsureAdmissionProfileRoot(context, "RushAttack", "RushAttack", "Local", new[] { new GameplayTagId("Rush") }, new GameplayTagQuery(Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>()), new GameplayTagQuery(Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>()), new GameplayTagQuery(Array.Empty<GameplayTagId>(), new[] { new GameplayTagId("Attack"), new GameplayTagId("Dodge"), new GameplayTagId("Rush") }, Array.Empty<GameplayTagId>()), ActionTargetRequirement.OptionalSnapshot, 1, "Assets/Configs/Character/Corin/Pipeline/Abilities/AdmissionProfiles/CorinRushAdmissionProfile.asset");
            return parts;
        }

        sealed class RootParts
        {
            internal GameplayAbilityAdmissionProfile admissionProfile;
        }
    }
}
