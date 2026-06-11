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
            BasicMovementPhase phase)
        {
            MovementInputIntent intent = MovementInputIntent.FromRaw(input.Move, settings.InputDeadZone, input.RunHeld);
            Vector3 worldDirection = CameraRelativeMovementResolver.Resolve(intent, cameraBasisProvider);
            MovementCommand command = MovementCommandBuilder.Build(worldDirection, intent, phase, input.DeltaTime, settings);

            return new BasicLocomotionFrame(input, settings, intent, worldDirection, phase, command);
        }

        public BasicLocomotionFrame Tick(
            in BasicLocomotionInputSnapshot input,
            in BasicMovementSettings settings,
            ICameraMovementBasisProvider cameraBasisProvider,
            BasicMovementPhase phase,
            BasicMovementPhaseFacts phaseFacts)
        {
            MovementInputIntent intent = MovementInputIntent.FromRaw(input.Move, settings.InputDeadZone, input.RunHeld);
            Vector3 worldDirection = CameraRelativeMovementResolver.Resolve(intent, cameraBasisProvider);
            MovementCommand command = MovementCommandBuilder.Build(worldDirection, intent, phase, input.DeltaTime, settings);

            return new BasicLocomotionFrame(input, settings, intent, worldDirection, phase, command);
        }

        public BasicLocomotionFrame Tick(
            in BasicLocomotionInputSnapshot input,
            in BasicMovementSettings settings,
            ICameraMovementBasisProvider cameraBasisProvider,
            BasicMovementPhase phase,
            BasicMovementPhaseFacts phaseFacts,
            BasicMovementMotionFacts motionFacts)
        {
            MovementInputIntent intent = MovementInputIntent.FromRaw(input.Move, settings.InputDeadZone, input.RunHeld);
            BasicMovementGait frameGait = intent.HasMoveIntent ? intent.Gait : BasicMovementGait.Walk;
            return Tick(in input, in settings, cameraBasisProvider, phase, phaseFacts, motionFacts, frameGait);
        }

        public BasicLocomotionFrame Tick(
            in BasicLocomotionInputSnapshot input,
            in BasicMovementSettings settings,
            ICameraMovementBasisProvider cameraBasisProvider,
            BasicMovementPhase phase,
            BasicMovementPhaseFacts phaseFacts,
            BasicMovementMotionFacts motionFacts,
            BasicMovementGait frameGait)
        {
            MovementInputIntent intent = MovementInputIntent.FromRaw(input.Move, settings.InputDeadZone, input.RunHeld);
            Vector3 worldDirection = CameraRelativeMovementResolver.Resolve(intent, cameraBasisProvider);
            BasicMovementGait commandGait = intent.HasMoveIntent ? intent.Gait : frameGait;
            MovementCommand command = MovementCommandBuilder.Build(worldDirection, intent, phase, input.DeltaTime, settings, motionFacts, commandGait);

            return new BasicLocomotionFrame(input, settings, intent, worldDirection, phase, command);
        }
    }
}
