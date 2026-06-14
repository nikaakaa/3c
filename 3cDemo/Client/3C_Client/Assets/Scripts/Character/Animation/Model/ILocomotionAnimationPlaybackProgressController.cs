using ThirdPersonMovement;

namespace ThirdPersonAnimation
{
    public interface ILocomotionAnimationPlaybackProgressController : IAnimationPhasePlaybackProgressSource
    {
        bool RestorePlaybackProgress(in AnimationPhasePlaybackProgress progress);
        bool RestorePlaybackProgress(in AnimationPhasePlaybackProgress progress, BasicMovementGait gait);
        AnimationPhasePlaybackProgress AdvancePlayback(float deltaTime);
    }
}
