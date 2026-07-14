using ThirdPersonCamera;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public static class CharacterLocomotionDirectionResolver
    {
        public static Vector3 Resolve(Vector2 input, bool cameraRelative, CameraBasisSnapshot basis)
        {
            Vector2 clamped = input.sqrMagnitude > 1f ? input.normalized : input;
            if (!cameraRelative || !basis.Valid)
                return new Vector3(clamped.x, 0f, clamped.y);

            Vector3 forward = FlattenNormalized(basis.PlanarForward, Vector3.forward);
            Vector3 right = FlattenNormalized(basis.PlanarRight, Vector3.right);
            Vector3 direction = right * clamped.x + forward * clamped.y;
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        static Vector3 FlattenNormalized(Vector3 value, Vector3 fallback)
        {
            value.y = 0f;
            if (value.sqrMagnitude <= 0.000001f)
                value = fallback;
            return value.normalized;
        }
    }
}
