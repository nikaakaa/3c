using System;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Animation.Resources
{
    internal sealed class CharacterAnimationResourceScope : IDisposable
    {
        enum ScopeState : byte
        {
            Open = 1,
            Closing = 2,
            Closed = 3
        }

        readonly CharacterAclResourceStore m_Store;
        ScopeState m_State = ScopeState.Open;

        internal CharacterAnimationResourceScope(
            ICharacterAnimationAssetLoader loader,
            CharacterAnimationResourceSettings settings)
        {
            m_Store = new CharacterAclResourceStore(
                loader ?? throw new ArgumentNullException(nameof(loader)),
                settings);
        }

        internal CharacterAclResourceStore Store
        {
            get
            {
                RequireOpenOrClosing();
                return m_Store;
            }
        }

        internal bool IsClosing => m_State != ScopeState.Open;

        internal int Register(CharacterAnimationCompiledResourceDescriptor descriptor)
        {
            RequireOpen();
            return m_Store.Register(descriptor);
        }

        internal void Request(int resourceIndex)
        {
            RequireOpen();
            m_Store.Request(resourceIndex);
        }

        internal CharacterAclResourceReadinessResult GetReadiness(int resourceIndex)
        {
            RequireOpenOrClosing();
            return m_Store.GetReadiness(resourceIndex);
        }

        internal void AdvancePreparation()
        {
            RequireOpen();
            m_Store.AdvancePreparation();
        }

        internal void BeginClose()
        {
            if (m_State == ScopeState.Open)
            {
                m_State = ScopeState.Closing;
                m_Store.BeginClose();
            }
        }

        public void Dispose()
        {
            if (m_State == ScopeState.Closed)
                return;
            BeginClose();
            m_Store.Dispose();
            m_State = ScopeState.Closed;
        }

        void RequireOpen()
        {
            if (m_State != ScopeState.Open)
                throw new ObjectDisposedException(nameof(CharacterAnimationResourceScope));
        }

        void RequireOpenOrClosing()
        {
            if (m_State == ScopeState.Closed)
                throw new ObjectDisposedException(nameof(CharacterAnimationResourceScope));
        }
    }
}
