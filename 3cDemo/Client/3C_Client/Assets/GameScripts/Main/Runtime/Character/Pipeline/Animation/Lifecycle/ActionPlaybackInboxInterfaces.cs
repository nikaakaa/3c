using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    public interface IActionPlaybackCommandPublisher
    {
        void Publish(ActionAnimationPlaybackCommand command);
        void Replace(
            EventId targetEventId,
            ActionAnimationPlaybackCommand replacement);
        void Retire(ActionAnimationPlaybackCommand terminalCommand);
    }
}
