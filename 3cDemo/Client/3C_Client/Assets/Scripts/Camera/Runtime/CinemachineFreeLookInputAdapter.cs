using Cinemachine;
using UnityEngine;

namespace ThirdPersonCamera
{
    [DisallowMultipleComponent]
    public sealed class CinemachineFreeLookInputAdapter : MonoBehaviour, AxisState.IInputAxisProvider
    {
        [SerializeField] ThirdPersonCameraController cameraController;

        public ThirdPersonCameraController CameraController { get => cameraController; set => cameraController = value; }

        void Reset()
        {
            ResolveController();
        }

        void OnEnable()
        {
            ResolveController();
            if (TryGetComponent(out CinemachineFreeLook freeLook))
                freeLook.UpdateInputAxisProvider();
        }

        void OnValidate()
        {
            ResolveController();
        }

        public float GetAxisValue(int axis)
        {
            return cameraController != null ? cameraController.GetLookAxisValue(axis) : 0f;
        }

        void ResolveController()
        {
            if (cameraController == null)
                cameraController = GetComponentInParent<ThirdPersonCameraController>(true);
        }
    }
}
