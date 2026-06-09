using UnityEngine;

namespace ThirdPersonMovement
{
    public readonly struct MovementInputIntent
    {
        public MovementInputIntent(Vector2 rawInput, Vector2 normalizedInput, float strength, bool hasMoveIntent)
        {
            RawInput = rawInput;
            NormalizedInput = normalizedInput;
            Strength = strength;
            HasMoveIntent = hasMoveIntent;
        }

        public Vector2 RawInput { get; }
        public Vector2 NormalizedInput { get; }
        public float Strength { get; }
        public bool HasMoveIntent { get; }

        public static MovementInputIntent FromRaw(Vector2 rawInput, float deadZone)
        {
            float magnitude = rawInput.magnitude;
            if (magnitude <= Mathf.Clamp01(deadZone))
                return new MovementInputIntent(rawInput, Vector2.zero, 0f, false);

            float strength = Mathf.Min(1f, magnitude);
            Vector2 normalized = magnitude > 1f ? rawInput / magnitude : rawInput;
            return new MovementInputIntent(rawInput, normalized, strength, true);
        }
    }
}
