using System;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchAdmissionProfileAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            parts.admissionProfile = BtsmtlSkillAuthoringCode.EnsureAdmissionProfileRoot(context, "BranchAttack", "BranchAttack", "Local", new[] { new GameplayTagId("Branch") }, new GameplayTagQuery(Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>()), new GameplayTagQuery(Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>()), new GameplayTagQuery(Array.Empty<GameplayTagId>(), new[] { new GameplayTagId("Attack"), new GameplayTagId("Dodge"), new GameplayTagId("Rush"), new GameplayTagId("Branch") }, Array.Empty<GameplayTagId>()), ActionTargetRequirement.OptionalSnapshot, 1, "Assets/Configs/Character/Corin/Pipeline/Abilities/AdmissionProfiles/CorinBranchAdmissionProfile.asset");
            return parts;
        }

        sealed class RootParts
        {
            internal GameplayAbilityAdmissionProfile admissionProfile;
        }
    }
}
