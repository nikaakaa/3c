using System;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonGameplay.Tick;

namespace ThirdPersonCharacter.Pipeline
{
    internal sealed class CharacterPresentationFrameTarget : IGameplayPresentationFrameTarget
    {
        readonly ICharacterPresentationRuntime m_Runtime;

        public CharacterPresentationFrameTarget(ICharacterPresentationRuntime runtime)
        {
            m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public void PresentationFrame(GameplayPresentationFrameContext context)
        {
            m_Runtime.Present(context);
        }
    }
}
