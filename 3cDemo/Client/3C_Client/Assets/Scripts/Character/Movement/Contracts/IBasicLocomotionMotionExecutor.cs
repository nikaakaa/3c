using UnityEngine;

namespace ThirdPersonMovement
{
    public interface IBasicLocomotionMotionExecutor
    {
        float CurrentSpeed { get; }
        Vector3 LastWorldDirection { get; }
        void ExecuteBasicMovement(in MovementCommand command);
    }
}
