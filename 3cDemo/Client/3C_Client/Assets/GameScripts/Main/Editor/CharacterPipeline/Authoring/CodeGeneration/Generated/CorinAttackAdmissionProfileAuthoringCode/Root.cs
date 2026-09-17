using System;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackAdmissionProfileAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            parts.admissionProfile = BtsmtlSkillAuthoringCode.EnsureAdmissionProfileRoot(context, "Attack", "Attack", "Local", new[] { new GameplayTagId("Attack") }, new GameplayTagQuery(Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>()), new GameplayTagQuery(Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>()), new GameplayTagQuery(Array.Empty<GameplayTagId>(), new[] { new GameplayTagId("Attack"), new GameplayTagId("Dodge") }, Array.Empty<GameplayTagId>()), ActionTargetRequirement.OptionalSnapshot, 1, "Assets/Configs/Character/Corin/Pipeline/Actions/Attack/CorinAttackActionProfile.asset");
            return parts;
        }

        sealed class RootParts
        {
            internal GameplayAbilityAdmissionProfile admissionProfile;
        }
    }
}
