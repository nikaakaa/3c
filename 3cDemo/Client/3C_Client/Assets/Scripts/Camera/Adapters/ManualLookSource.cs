using UnityEngine;
using UnityEngine.InputSystem;

namespace ThirdPersonCamera
{
    public static class ManualLookSource
    {
        public static CameraLookIntent Read(InputActionReference lookAction)
        {
            return new CameraLookIntent(lookAction != null ? lookAction.action.ReadValue<Vector2>() : Vector2.zero);
        }

        public static void Enable(InputActionReference lookAction)
        {
            if (lookAction != null) lookAction.action.Enable();
        }

        public static void Disable(InputActionReference lookAction)
        {
            if (lookAction != null) lookAction.action.Disable();
        }
    }
}
