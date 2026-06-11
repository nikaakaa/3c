using UnityEngine;

namespace ThirdPersonMovement
{
    public readonly struct BasicLocomotionInputSnapshot
    {
        public BasicLocomotionInputSnapshot(float deltaTime, Vector2 move, Vector2 look)
            : this(deltaTime, move, look, false)
        {
        }

        public BasicLocomotionInputSnapshot(float deltaTime, Vector2 move, Vector2 look, bool runHeld)
        {
            DeltaTime = deltaTime < 0f ? 0f : deltaTime;
            Move = move;
            Look = look;
            RunHeld = runHeld;
        }

        public float DeltaTime { get; }
        public Vector2 Move { get; }
        public Vector2 Look { get; }
        public bool RunHeld { get; }
    }
}
