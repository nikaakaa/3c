using ThirdPersonCharacter.Pipeline.Presentation;
using System.Runtime.ExceptionServices;
using System;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeDomainInstance : IDisposable
    {
        readonly CharacterPoseNativeDomainSession m_Session;
        readonly CharacterPoseNativeActionCommandSource m_ActionCommandSource;
        bool m_Disposed;

        internal CharacterPoseNativeDomainInstance(
            CharacterPoseNativeDomainSession session,
            CharacterPoseNativeActionCommandSource actionCommandSource)
        {
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_ActionCommandSource = actionCommandSource ?? throw new ArgumentNullException(nameof(actionCommandSource));
        }

        internal CharacterPoseNativeDomainSession Session => m_Session;
        internal CharacterPoseNativeActionCommandSource ActionCommandSource => m_ActionCommandSource;
        internal bool IsAdopted => !m_Disposed;

        internal void BeginFrame(ulong frameIdentity) =>
            m_ActionCommandSource.BeginFrame(frameIdentity);

        internal bool TryGetCommands(
            ActorId actorId,
            ulong frameIdentity,
            out System.Collections.Generic.IReadOnlyList<ActionAnimationPlaybackCommand> commands) =>
            m_ActionCommandSource.TryGetCommands(actorId, frameIdentity, out commands);

        internal void DiscardFrame()
        {
            if (m_ActionCommandSource.HasOpenFrame)
                m_ActionCommandSource.DiscardFrame();
        }

        internal CharacterPoseNativeResetResult Reset(ulong resetGeneration) =>
            m_Session.Reset(resetGeneration);

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Exception failure = null;
            try
            {
                DiscardFrame();
            }
            catch (Exception cleanup)
            {
                failure = cleanup;
            }
            CharacterPresentationCleanup.Dispose(m_Session, ref failure);
            if (failure != null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
