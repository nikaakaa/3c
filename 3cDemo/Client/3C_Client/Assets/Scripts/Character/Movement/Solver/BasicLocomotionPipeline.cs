using ThirdPersonCamera;
using UnityEngine;

namespace ThirdPersonMovement
{
    public sealed class BasicLocomotionPipeline
    {
        public BasicLocomotionFrame Tick(
            in BasicLocomotionInputSnapshot input,
            in BasicMovementSettings settings,
            ICameraMovementBasisProvider cameraBasisProvider,
            BasicLocomotionStateMachine stateMachine)
        {
            MovementInputIntent intent = MovementInputIntent.FromRaw(input.Move, settings.InputDeadZone);
            Vector3 worldDirection = CameraRelativeMovementResolver.Resolve(intent, cameraBasisProvider);
            BasicMovementPhase phase = stateMachine.Tick(intent.HasMoveIntent, input.DeltaTime, settings);
            MovementCommand command = MovementCommandBuilder.Build(worldDirection, intent, phase, input.DeltaTime, settings);

            return new BasicLocomotionFrame(input, settings, intent, worldDirection, phase, command);
        }
    }
}
