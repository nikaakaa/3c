using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal interface ICharacterAnimationAssetLoader : IDisposable
    {
        CharacterAnimationAssetLoadTicket BeginLoad(
            CharacterAnimationResourceAddress address);
        CharacterAnimationAssetLoadResult Poll(
            CharacterAnimationAssetLoadTicket ticket);
        void Release(CharacterAnimationAssetLoadTicket ticket);
    }
}
