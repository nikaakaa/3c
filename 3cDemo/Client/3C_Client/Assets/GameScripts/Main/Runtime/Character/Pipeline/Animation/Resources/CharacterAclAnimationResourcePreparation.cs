using System;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Animation.Resources
{
    internal sealed class CharacterAclAnimationResourcePreparation : IDisposable
    {
        readonly CharacterAnimationCompiledResourceDescriptor m_ExpectedDescriptor;
        readonly ICharacterAnimationAssetLoader m_Loader;
        CharacterAnimationAssetLoadTicket m_Ticket;
        IntPtr m_Group;
        bool m_Requested;
        bool m_Disposed;

        internal CharacterAclAnimationResourcePreparation(
            CharacterAnimationCompiledResourceDescriptor descriptor,
            ICharacterAnimationAssetLoader loader)
        {
            m_ExpectedDescriptor = descriptor ??
                throw new ArgumentNullException(nameof(descriptor));
            m_ExpectedDescriptor.RequireValid();
            m_Loader = loader ?? throw new ArgumentNullException(nameof(loader));
            Readiness = CharacterAclResourceReadinessResult.Pending();
        }

        internal CharacterAclResourceReadinessResult Readiness { get; private set; }
        internal IntPtr Group => m_Group;
        internal bool IsRequested => m_Requested;
        internal bool IsInProgress => m_Requested && Readiness.IsPending;
        internal bool HasPendingCleanup =>
            (Readiness.IsReady && m_Requested) ||
            (Readiness.IsInvalid && (m_Requested || m_Group != IntPtr.Zero));

        internal void Request()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterAclAnimationResourcePreparation));
            if (m_Requested || Readiness.IsReady || Readiness.IsInvalid)
                return;
            try
            {
                m_Ticket = m_Loader.BeginLoad(
                    new CharacterAnimationResourceAddress(
                        m_ExpectedDescriptor.ResourceAddress));
                m_Requested = true;
            }
            catch (Exception exception)
            {
                Readiness = CharacterAclResourceReadinessResult.Invalid(
                    CharacterAclResourceFailureCode.NativeUnavailable,
                    exception.Message);
            }
        }

        internal void Advance()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterAclAnimationResourcePreparation));
            if (Readiness.IsReady && m_Requested)
            {
                ReleaseTicket();
                return;
            }
            if (Readiness.IsInvalid && (m_Requested || m_Group != IntPtr.Zero))
            {
                RetryInvalidCleanup();
                return;
            }
            if (!m_Requested || !Readiness.IsPending)
                return;
            CharacterAnimationAssetLoadResult loaded = m_Loader.Poll(m_Ticket);
            if (loaded.IsPending)
                return;
            Exception primaryFailure = null;
            Exception cleanupFailure = null;
            try
            {
                if (loaded.IsInvalid || !loaded.Resource ||
                    !loaded.Resource.MatchesDescriptor(m_ExpectedDescriptor))
                {
                    Readiness = CharacterAclResourceReadinessResult.Invalid(
                        CharacterAclResourceFailureCode.SourceIdentityMismatch,
                        loaded.Message.Length == 0
                            ? "Loaded ACL resource does not match the compiled resource descriptor."
                            : loaded.Message);
                }
                else
                {
                    CharacterAclResourceReadinessResult validation = loaded.Resource.ValidateRuntime();
                    if (validation.IsInvalid)
                    {
                        Readiness = validation;
                    }
                    else
                    {
                        IntPtr group = IntPtr.Zero;
                        try
                        {
                            CharacterAclNativeError error = CharacterAclNativeBridge.GroupCreate(
                                loaded.Resource.RequireGroupPayload(CharacterAclDataBlockKind.Transform),
                                loaded.Resource.RequireGroupPayload(CharacterAclDataBlockKind.Scalar),
                                loaded.Resource.RequirePayload(CharacterAclDataBlockKind.DatabaseHeader, 0),
                                loaded.Resource.RequirePayload(CharacterAclDataBlockKind.BulkMedium, 0),
                                loaded.Resource.RequirePayload(CharacterAclDataBlockKind.BulkLow, 0),
                                out group);
                            if (error != CharacterAclNativeError.Success || group == IntPtr.Zero)
                            {
                                primaryFailure = new InvalidOperationException(
                                    $"ACL native group creation failed: {error}.");
                            }
                            else
                            {
                                m_Group = group;
                                group = IntPtr.Zero;
                                Readiness = CharacterAclResourceReadinessResult.Ready();
                            }
                        }
                        catch (Exception exception)
                        {
                            primaryFailure = exception;
                        }
                        if (group != IntPtr.Zero)
                        {
                            m_Group = group;
                            try
                            {
                                ReleaseGroup();
                            }
                            catch (Exception cleanupException)
                            {
                                RecordFailure(ref cleanupFailure, cleanupException);
                            }
                        }
                        if (primaryFailure != null)
                        {
                            Readiness = CharacterAclResourceReadinessResult.Invalid(
                                CharacterAclResourceFailureCode.ContextInvalid,
                                primaryFailure.Message);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                primaryFailure = exception;
                Readiness = CharacterAclResourceReadinessResult.Invalid(
                    CharacterAclResourceFailureCode.ContextInvalid,
                    exception.Message);
            }
            try
            {
                ReleaseTicket();
            }
            catch (Exception cleanupException)
            {
                RecordFailure(ref cleanupFailure, cleanupException);
            }
            if (cleanupFailure != null)
            {
                if (primaryFailure != null)
                    throw new AggregateException(
                        "ACL resource preparation advance cleanup failed.",
                        primaryFailure,
                        cleanupFailure);
                throw new AggregateException(
                    "ACL resource preparation advance cleanup failed.",
                    cleanupFailure);
            }
        }

        void ReleaseTicket()
        {
            if (!m_Requested)
                return;
            m_Loader.Release(m_Ticket);
            m_Ticket = default;
            m_Requested = false;
        }

        void RetryInvalidCleanup()
        {
            Exception failure = null;
            try
            {
                ReleaseGroup();
            }
            catch (Exception exception)
            {
                RecordFailure(ref failure, exception);
            }
            try
            {
                ReleaseTicket();
            }
            catch (Exception exception)
            {
                RecordFailure(ref failure, exception);
            }
            if (failure != null)
                throw new AggregateException(
                    "ACL invalid resource cleanup failed.",
                    failure);
        }

        internal void Reject(
            CharacterAclResourceFailureCode failureCode,
            string message)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterAclAnimationResourcePreparation));
            Readiness = CharacterAclResourceReadinessResult.Invalid(
                failureCode,
                message);
            Exception cleanupFailure = null;
            try
            {
                ReleaseGroup();
            }
            catch (Exception exception)
            {
                RecordFailure(ref cleanupFailure, exception);
            }
            try
            {
                ReleaseTicket();
            }
            catch (Exception exception)
            {
                RecordFailure(ref cleanupFailure, exception);
            }
            if (cleanupFailure != null)
                throw new AggregateException(
                    "ACL resource preparation rejection cleanup failed.",
                    new InvalidOperationException(message),
                    cleanupFailure);
        }

        internal IntPtr TakeGroup()
        {
            if (!Readiness.IsReady || m_Group == IntPtr.Zero)
                throw new InvalidOperationException("ACL resource preparation has no ready group.");
            IntPtr group = m_Group;
            m_Group = IntPtr.Zero;
            return group;
        }

        internal void ReleaseGroup()
        {
            if (m_Group == IntPtr.Zero)
                return;
            CharacterAclNativeError error =
                CharacterAclNativeBridge.GroupRelease(m_Group);
            if (error != CharacterAclNativeError.Success)
                throw new InvalidOperationException(
                    $"ACL resource group release failed: {error}.");
            m_Group = IntPtr.Zero;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Exception failure = null;
            try
            {
                ReleaseTicket();
            }
            catch (Exception exception)
            {
                RecordFailure(ref failure, exception);
            }
            try
            {
                ReleaseGroup();
            }
            catch (Exception exception)
            {
                RecordFailure(ref failure, exception);
            }
            if (failure != null)
                throw new AggregateException(
                    "ACL resource preparation disposal failed.",
                    failure);
            m_Disposed = true;
        }

        static void RecordFailure(ref Exception failure, Exception exception)
        {
            failure = failure == null
                ? exception
                : new AggregateException(failure, exception);
        }
    }
}
