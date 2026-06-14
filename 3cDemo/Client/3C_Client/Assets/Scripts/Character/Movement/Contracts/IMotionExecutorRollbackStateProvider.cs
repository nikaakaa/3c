using ThirdPersonSimulation;

namespace ThirdPersonMovement
{
    public interface IMotionExecutorRollbackStateProvider
    {
        MotionExecutorRollbackState CaptureRollbackState();
        void RestoreRollbackState(in MotionExecutorRollbackState state);
    }
}
