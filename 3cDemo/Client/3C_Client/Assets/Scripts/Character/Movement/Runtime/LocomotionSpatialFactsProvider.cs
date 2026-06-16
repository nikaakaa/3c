using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonMovement
{
    internal interface ILocomotionSpatialFactsProviderHost
    {
        RollbackCameraBasisProvider RollbackCameraBasisProvider { get; }
        bool HasCameraController { get; }
        Vector2 CameraLookSensitivity { get; }
        void ApplyCameraLook(Vector2 lookInput);
        void SyncRollbackCameraBasisFromCamera();
        void SyncRollbackCameraBasisWithoutCamera();
        Vector3 ResolveFacingForward();
        void LogCameraInput(Vector2 moveInput, Vector2 lookInput);
    }

    internal sealed class LocomotionSpatialFactsProvider
    {
        readonly ILocomotionSpatialFactsProviderHost host;

        public LocomotionSpatialFactsProvider(ILocomotionSpatialFactsProviderHost host)
        {
            this.host = host;
        }

        public LocomotionSpatialFacts Resolve(
            in BasicLocomotionInputSnapshot input,
            in MovementInputIntent intent)
        {
            RollbackCameraBasisProvider cameraBasis = host.RollbackCameraBasisProvider;
            if (cameraBasis.UsingOverride)
            {
                cameraBasis.ApplyLook(input.Look, host.CameraLookSensitivity);
            }
            else if (host.HasCameraController)
            {
                host.ApplyCameraLook(input.Look);
                host.SyncRollbackCameraBasisFromCamera();
            }
            else
            {
                host.SyncRollbackCameraBasisWithoutCamera();
            }

            host.LogCameraInput(input.Move, input.Look);

            return LocomotionFactsBuilder.BuildSpatialFacts(
                in intent,
                CameraRelativeMovementResolver.Resolve(intent, cameraBasis),
                host.ResolveFacingForward(),
                cameraBasis.CameraPlanarForward,
                cameraBasis.CameraPlanarRight);
        }
    }
}
