using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;

namespace ThirdPersonAnimation
{
    public interface IActionAnimationPresenter
    {
        ActionAnimationKey CurrentKey { get; }
        float CurrentNormalizedTime { get; }
        bool HasValidPlayback { get; }
        ActionAnimationPlaybackProgress CurrentPlaybackProgress { get; }
        string CurrentAnimationName { get; }
        bool Present(in CharacterStateAnimationRequest request);
        void Clear();
    }

    public interface IActionAnimationPlaybackProgressController
    {
        bool RestorePlaybackProgress(in ActionAnimationPlaybackProgress progress, string animationName);
    }
}
