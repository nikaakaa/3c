using ThirdPersonCamera;
using UnityEngine;

namespace ThirdPersonSimulation
{
    public sealed class RollbackCameraBasisProvider : ICameraMovementBasisProvider
    {
        RollbackCameraBasisState state = RollbackCameraBasisState.Default;
        bool useOverride;

        public Vector3 CameraPlanarForward => state.PlanarForward;
        public Vector3 CameraPlanarRight => state.PlanarRight;
        public float Yaw => state.Yaw;
        public bool UsingOverride => useOverride;

        public void SyncFrom(ICameraMovementBasisProvider provider, float fallbackYaw = 0f)
        {
            if (useOverride || provider == null)
                return;

            Vector3 forward = provider.CameraPlanarForward;
            Vector3 right = provider.CameraPlanarRight;
            float yaw = ResolveYaw(forward, fallbackYaw);
            state = new RollbackCameraBasisState(forward, right, yaw);
        }

        public void Override(in RollbackCameraBasisState overrideState)
        {
            state = overrideState;
            useOverride = true;
        }

        public void ApplyLook(Vector2 lookDelta, Vector2 sensitivity)
        {
            float yaw = Mathf.Repeat(state.Yaw + lookDelta.x * sensitivity.x, 360f);
            state = new RollbackCameraBasisState(
                Quaternion.Euler(0f, yaw, 0f) * Vector3.forward,
                Quaternion.Euler(0f, yaw, 0f) * Vector3.right,
                yaw);
        }

        public void ReleaseOverride()
        {
            useOverride = false;
        }

        static float ResolveYaw(Vector3 forward, float fallbackYaw)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.000001f)
                return fallbackYaw;

            forward.Normalize();
            return Mathf.Repeat(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, 360f);
        }
    }
}
