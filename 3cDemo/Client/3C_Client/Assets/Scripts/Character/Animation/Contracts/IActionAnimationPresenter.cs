using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;

namespace ThirdPersonAnimation
{
    public interface IActionAnimationPresenter
    {
        ActionAnimationKey CurrentKey { get; }
        float CurrentNormalizedTime { get; }
        bool HasValidPlayback { get; }
        string CurrentAnimationName { get; }
        bool Present(in CharacterStateAnimationRequest request);
        void Clear();
    }
}
