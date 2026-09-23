using System;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public readonly struct CharacterPoseSamplingTarget
    {
        CharacterPoseSamplingTarget(in CharacterPoseDiagnosticTarget identity, int hostInstanceId)
        {
            Identity = identity;
            HostInstanceId = hostInstanceId;
        }

        public CharacterPoseDiagnosticTarget Identity { get; }
        public Guid RuntimeInstanceId => Identity.RuntimeInstanceId;
        public int HostInstanceId { get; }

        public static CharacterPoseSamplingTarget Require(string actorId)
        {
            foreach (FixedCharacterHost host in UnityEngine.Object.FindObjectsOfType<FixedCharacterHost>())
            {
                if (host.ActorId.Value != actorId)
                    continue;
                if (host.PresentationRuntime == null ||
                    !host.PresentationRuntime.TryGetPoseDiagnosticTarget(out CharacterPoseDiagnosticTarget identity))
                    throw new InvalidOperationException($"Actor '{actorId}' Pose diagnostics are not ready.");
                return new CharacterPoseSamplingTarget(in identity, host.GetInstanceID());
            }
            throw new InvalidOperationException($"Active Fixed Actor '{actorId}' was not found.");
        }
    }
}
