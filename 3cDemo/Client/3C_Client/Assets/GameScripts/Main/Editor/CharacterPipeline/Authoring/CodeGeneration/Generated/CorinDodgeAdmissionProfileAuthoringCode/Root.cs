using System;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinDodgeAdmissionProfileAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            parts.admissionProfile = BtsmtlSkillAuthoringCode.EnsureAdmissionProfileRoot(context, "Dodge", "Dodge", "Local", new[] { new GameplayTagId("Dodge") }, new GameplayTagQuery(Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>()), new GameplayTagQuery(Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>()), new GameplayTagQuery(Array.Empty<GameplayTagId>(), new[] { new GameplayTagId("Attack"), new GameplayTagId("Dodge") }, Array.Empty<GameplayTagId>()), ActionTargetRequirement.None, 1, "Assets/Configs/Character/Corin/Pipeline/Abilities/AdmissionProfiles/CorinDodgeAdmissionProfile.asset");
            return parts;
        }

        sealed class RootParts
        {
            internal GameplayAbilityAdmissionProfile admissionProfile;
        }
    }
}
