using UnityEngine;

namespace ThirdPersonMovement
{
    public readonly struct BasicLocomotionFrame
    {
        public BasicLocomotionFrame(
            BasicLocomotionInputSnapshot input,
            BasicMovementSettings settings,
            MovementInputIntent intent,
            Vector3 worldDirection,
            BasicMovementPhase phase,
            MovementCommand command)
        {
            Input = input;
            Settings = settings;
            Intent = intent;
            WorldDirection = worldDirection;
            Phase = phase;
            Command = command;
        }

        public BasicLocomotionInputSnapshot Input { get; }
        public BasicMovementSettings Settings { get; }
        public MovementInputIntent Intent { get; }
        public Vector3 WorldDirection { get; }
        public BasicMovementPhase Phase { get; }
        public MovementCommand Command { get; }
    }
}
