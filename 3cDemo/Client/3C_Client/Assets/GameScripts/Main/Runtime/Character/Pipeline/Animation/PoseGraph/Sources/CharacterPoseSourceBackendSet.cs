using System;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;

namespace ThirdPersonCharacter.Pipeline.Animation.Sources
{
    internal sealed class CharacterPoseSourceBackendSet : IDisposable
    {
        readonly IAnimationPoseSamplingBackend m_NativeClip;
        readonly IAnimationPoseSamplingBackend m_Acl;
        bool m_Disposed;

        internal CharacterPoseSourceBackendSet(
            IAnimationPoseSamplingBackend nativeClip,
            IAnimationPoseSamplingBackend acl)
        {
            m_NativeClip = nativeClip ?? throw new ArgumentNullException(nameof(nativeClip));
            m_Acl = acl;
        }

        internal IAnimationPoseSamplingBackend NativeClip => m_NativeClip;

        internal IAnimationPoseSamplingBackend Require(
            CharacterAnimationSamplingBackendKind backend) =>
            backend switch
            {
                CharacterAnimationSamplingBackendKind.NativeClip => m_NativeClip,
                CharacterAnimationSamplingBackendKind.Acl => m_Acl ??
                    throw new InvalidOperationException(
                        "ACL backend is absent from the compiled source closure."),
                _ => throw new ArgumentOutOfRangeException(nameof(backend))
            };

        internal void BeginFrame(in CharacterPoseSourceFrameLease lease)
        {
            m_NativeClip.BeginFrame(lease);
            try
            {
                m_Acl?.BeginFrame(lease);
            }
            catch
            {
                m_NativeClip.DiscardFrame(lease);
                throw;
            }
        }

        internal void RequireOpenFrame(in CharacterPoseSourceFrameLease lease)
        {
            m_NativeClip.RequireOpenFrame(lease);
            m_Acl?.RequireOpenFrame(lease);
        }

        internal void ValidateFrame(in CharacterPoseSourceFrameLease lease)
        {
            m_NativeClip.ValidateFrame(lease);
            m_Acl?.ValidateFrame(lease);
        }

        internal void EnterEvaluateBarrier(in CharacterPoseSourceFrameLease lease)
        {
            m_NativeClip.EnterEvaluateBarrier(lease);
            m_Acl?.EnterEvaluateBarrier(lease);
        }

        internal void CommitFrame(in CharacterPoseSourceFrameLease lease)
        {
            bool nativeOpen = m_NativeClip.HasOpenFrame;
            bool aclOpen = m_Acl?.HasOpenFrame == true;
            try
            {
                m_NativeClip.ApplyFrame(lease);
                m_Acl?.ApplyFrame(lease);
                m_NativeClip.ValidateAppliedFrame(lease);
                m_Acl?.ValidateAppliedFrame(lease);
            }
            catch (Exception exception)
            {
                Exception failure = exception;
                if (m_Acl != null && (aclOpen || m_Acl.HasOpenFrame))
                    RollbackStep(() => m_Acl.RollbackAppliedFrame(lease), ref failure);
                if (nativeOpen || m_NativeClip.HasOpenFrame)
                    RollbackStep(() => m_NativeClip.RollbackAppliedFrame(lease), ref failure);
                throw failure;
            }
            m_NativeClip.FinalizeAppliedFrame(lease);
            m_Acl?.FinalizeAppliedFrame(lease);
        }

        internal void DiscardFrame(CharacterPoseSourceFrameLease lease)
        {
            Exception failure = null;
            if (m_Acl?.HasOpenFrame == true)
                DiscardStep(() => m_Acl.DiscardFrame(lease), ref failure);
            if (m_NativeClip.HasOpenFrame)
                DiscardStep(() => m_NativeClip.DiscardFrame(lease), ref failure);
            if (failure != null)
                throw failure;
        }

        internal void ExecuteDeferredReleases()
        {
            m_NativeClip.ExecuteDeferredReleases();
            m_Acl?.ExecuteDeferredReleases();
        }

        internal void Clear()
        {
            m_NativeClip.Clear();
            m_Acl?.Clear();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Exception failure = null;
            DisposeStep(() => m_Acl?.Dispose(), ref failure);
            DisposeStep(m_NativeClip.Dispose, ref failure);
            m_Disposed = true;
            if (failure != null)
                throw failure;
        }

        static void DiscardStep(Action action, ref Exception failure)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
        }

        static void RollbackStep(Action action, ref Exception failure)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = new AggregateException(failure, exception);
            }
        }

        static void DisposeStep(Action action, ref Exception failure)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
        }
    }
}
