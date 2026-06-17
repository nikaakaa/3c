using ThirdPersonCharacterStateMachine;

namespace ThirdPersonAnimation
{
    public interface ILocomotionAnimationPresenter : ILocomotionAnimationPlaybackProgressController
    {
        string CurrentAnimationName { get; }
        void Present(in MovementAnimationContext context);
    }

    public interface ICharacterAnimationOutputPresenter
    {
        CharacterAnimationPlaybackSnapshot CurrentSnapshot { get; }
        void PresentLocomotion(in MovementAnimationContext context);
        bool PresentAction(in CharacterStateAnimationRequest request);
        void ClearActionPlayback();
    }
}
