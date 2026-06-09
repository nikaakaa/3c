using UnityEngine;

namespace ThirdPersonMovement
{
    public static class MovementCommandBuilder
    {
        public static MovementCommand Build(Vector3 worldDirection, in MovementInputIntent intent, BasicMovementPhase phase, float deltaTime, in BasicMovementSettings settings)
        {
            float speed = intent.HasMoveIntent ? settings.MaxPlanarSpeed * intent.Strength : 0f;
            return new MovementCommand(worldDirection, speed, settings.RotationSpeed, deltaTime, phase);
        }
    }
}
