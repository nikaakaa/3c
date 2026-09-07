using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterMovementModeStateIdentities
    {
        public static IReadOnlyList<string> FromControlContract(
            CharacterControlModuleContract contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            string[] identities = contract.States
                .Select(state => string.Concat(
                    CharacterPresentationTrajectoryIntent.MovementModeStatePrefix,
                    state.Id.Value))
                .ToArray();
            Array.Sort(identities, StringComparer.Ordinal);
            return identities;
        }
    }
}
