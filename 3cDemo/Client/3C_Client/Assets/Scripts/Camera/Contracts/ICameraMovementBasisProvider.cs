using UnityEngine;

namespace ThirdPersonCamera
{
    public interface ICameraMovementBasisProvider
    {
        Vector3 CameraPlanarForward { get; }
        Vector3 CameraPlanarRight { get; }
    }

    public interface ICameraPitchProvider
    {
        float Pitch { get; }
    }
}
