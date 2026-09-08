using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public interface ILocomotionInputMotionAuthoring
    {
        float MoveSpeed { get; }
        LocomotionInputMotionDisplacementMode DisplacementMode { get; }
        RootMotionCurveAsset ActionMotionCurve { get; }
        float TurnSpeedDegrees { get; }
        bool CameraRelative { get; }
        LocomotionInputMotionExecutionMode ExecutionMode { get; }
        float DurationSeconds { get; }
    }
}
