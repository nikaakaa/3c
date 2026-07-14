using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public sealed class MotionWarpModifier : IMotionModifier
    {
        public MotionIntent Modify(MotionIntent intent, MotionModifierContext context)
        {
            IReadOnlyList<MotionWarpWindow> windows = context.MotionWarpWindows;
            if (windows == null || windows.Count == 0)
                return intent;

            Vector3 displacement = intent.Displacement;
            float yaw = intent.YawDegrees;
            bool modified = intent.HasMotion;

            for (int i = 0; i < windows.Count; i++)
            {
                MotionWarpWindow window = windows[i];
                if ((!window.HasPositionCorrection && !window.HasYawCorrection) ||
                    !context.TryGetMotionWarpTarget(window.ActionInstanceId, window.TargetKey, out MotionWarpTarget target))
                    continue;

                if (window.HasPositionCorrection)
                {
                    Vector3 desiredDisplacement = target.Position - context.ActorPosition;
                    Vector3 correction = (desiredDisplacement - displacement) * window.PositionWeight * window.Weight;
                    if (correction.sqrMagnitude > window.MaxPositionCorrection * window.MaxPositionCorrection)
                        correction = correction.normalized * window.MaxPositionCorrection;
                    displacement += correction;
                    modified = true;
                }

                if (window.HasYawCorrection &&
                    TryResolveDesiredYaw(context.ActorPosition, context.ActorRotation, target, out float desiredYaw))
                {
                    float correctionYaw = Mathf.DeltaAngle(yaw, desiredYaw) * window.YawWeight * window.Weight;
                    correctionYaw = Mathf.Clamp(correctionYaw, -window.MaxYawCorrectionDegrees, window.MaxYawCorrectionDegrees);
                    yaw += correctionYaw;
                    modified = true;
                }
            }

            if (!modified)
                return default;

            Vector3 velocity = context.DeltaTime > 0f ? displacement / context.DeltaTime : Vector3.zero;
            return new MotionIntent(displacement, velocity, yaw);
        }

        static bool TryResolveDesiredYaw(
            Vector3 actorPosition,
            Quaternion actorRotation,
            MotionWarpTarget target,
            out float desiredYaw)
        {
            if (target.HasYaw)
            {
                desiredYaw = Mathf.DeltaAngle(actorRotation.eulerAngles.y, target.YawDegrees);
                return true;
            }

            Vector3 direction = target.Position - actorPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0000001f)
            {
                desiredYaw = 0f;
                return false;
            }

            desiredYaw = Vector3.SignedAngle(actorRotation * Vector3.forward, direction.normalized, Vector3.up);
            return true;
        }
    }
}
