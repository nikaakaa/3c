using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Network;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public interface IMotionModifier
    {
        MotionIntent Modify(MotionIntent intent, MotionModifierContext context);
    }

    public interface ICharacterMotionContext : IRuntimeDiagnosticsContextSource
    {
        bool TryGetActionInstanceHandle(ulong actionInstanceId, out ActionInstanceHandle handle);
    }

    public readonly struct MotionModifierContext
    {
        public MotionModifierContext(
            Vector3 actorPosition,
            Quaternion actorRotation,
            float deltaTime,
            ICharacterMotionContext motionContext,
            IReadOnlyList<MotionWarpWindow> motionWarpWindows)
        {
            ActorPosition = actorPosition;
            ActorRotation = actorRotation;
            DeltaTime = deltaTime;
            MotionContext = motionContext;
            MotionWarpWindows = motionWarpWindows;
        }

        public Vector3 ActorPosition { get; }
        public Quaternion ActorRotation { get; }
        public float DeltaTime { get; }
        public ICharacterMotionContext MotionContext { get; }
        public IReadOnlyList<MotionWarpWindow> MotionWarpWindows { get; }

        public bool TryGetMotionWarpTarget(ulong actionInstanceId, string targetKey, out MotionWarpTarget target)
        {
            target = default;
            if (MotionContext == null ||
                !MotionContext.TryGetActionInstanceHandle(actionInstanceId, out ActionInstanceHandle handle) ||
                !handle.TargetSnapshot.HasTarget)
                return false;

            if (!string.IsNullOrEmpty(targetKey) &&
                !string.IsNullOrEmpty(handle.TargetKey) &&
                !string.Equals(targetKey, handle.TargetKey, System.StringComparison.Ordinal))
                return false;

            Vector3 direction = handle.TargetSnapshot.Rotation * Vector3.forward;
            target = direction.sqrMagnitude > 0.0001f
                ? new MotionWarpTarget(handle.TargetSnapshot.Position, Quaternion.LookRotation(direction, Vector3.up).eulerAngles.y)
                : new MotionWarpTarget(handle.TargetSnapshot.Position);
            return true;
        }
    }
}
