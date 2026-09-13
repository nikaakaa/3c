using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline
{
    public sealed partial class CharacterPipelineDefinition
    {
        public CharacterBodyMotionBinding BuildBodyMotionRuntimeBinding()
        {
            CharacterBodyMotionProfile profile = BodyMotionProfile
                ? BodyMotionProfile
                : throw new InvalidOperationException("Character Pipeline Definition Body Motion profile is missing.");
            string sourceIdentity = SimulationIdentityValue(profile.name);
            StableHash contentRevision = StableHash.Compute(
                $"character-body-motion-profile/{CharacterBodyMotionProfile.SemanticVersion}",
                sourceIdentity,
                profile.GravityAcceleration.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                profile.MaximumFallSpeed.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            return new CharacterBodyMotionBinding(
                sourceIdentity,
                contentRevision,
                CharacterBodyMotionProfile.SemanticVersion,
                profile.GravityAcceleration,
                profile.MaximumFallSpeed);
        }

        static string SimulationIdentityValue(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                throw new InvalidOperationException("Character Body Motion profile name is missing.");
            return $"body-motion:{profileName.Trim()}";
        }
    }
}
