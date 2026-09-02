using System;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation.Sources
{
    internal sealed class CharacterPoseMotionMatchingSourceRuntime :
        IDisposable
    {
        readonly CharacterMotionMatchingPresentationModule m_Module;
        MotionMatchingFrameMutationLease m_Frame;
        bool m_Disposed;

        internal CharacterPoseMotionMatchingSourceRuntime(
            CharacterMotionMatchingPresentationModule module)
        {
            m_Module = module ??
                throw new ArgumentNullException(nameof(module));
        }

        internal bool Enabled => m_Module.Enabled;
        internal bool AcceptsTrajectoryIntent =>
            m_Module.AcceptsTrajectoryIntent;

        internal bool TryCaptureSearchReplay(
            string providerId,
            out MotionMatchingSearchReplayArtifact artifact)
        {
            RequireAlive();
            return m_Module.TryCaptureSearchReplay(
                providerId,
                out artifact);
        }

        internal void CaptureTrajectoryIntent(
            CharacterPresentationTrajectoryIntent intent)
        {
            RequireAlive();
            m_Module.CaptureTrajectoryIntent(intent);
        }

        internal void CapturePreviewQuery(
            string providerId,
            MotionMatchingSearchReplayArtifact query)
        {
            RequireAlive();
            m_Module.CapturePreviewQuery(providerId, query);
        }

        internal void BeginFrame(ulong frameIdentity)
        {
            RequireAlive();
            if (m_Frame.IsValid)
            {
                throw new InvalidOperationException(
                    "Motion Matching source frame is already open.");
            }
            m_Frame = m_Module.BeginPendingFrame(frameIdentity);
        }

        internal bool HasFrameWork(
            ulong frameIdentity,
            in MotionMatchingPoseStateDemandBatch demands)
        {
            RequireFrame(frameIdentity);
            return m_Module.HasFrameWork(in demands);
        }

        internal MotionMatchingFrameResolution ResolveFrame(
            ulong frameIdentity,
            ulong presentationFrame,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            in MotionMatchingPoseStateDemandBatch demands)
        {
            RequireFrame(frameIdentity);
            return m_Module.ResolveFrame(
                presentationFrame,
                presentationDeltaSeconds,
                in bodyFrame,
                in demands,
                null);
        }

        internal void PrepareFrameCompletion(
            ulong frameIdentity,
            in MotionMatchingFrameResolution resolution,
            ulong poseCompletionIdentity)
        {
            RequireFrame(frameIdentity);
            m_Module.PrepareFrameCompletion(
                in resolution,
                poseCompletionIdentity);
        }

        internal void CompleteFrame(
            ulong frameIdentity,
            in MotionMatchingPosePlanCompletion completion)
        {
            RequireFrame(frameIdentity);
            m_Module.CompleteFrame(in completion);
        }

        internal void CommitFrame(ulong frameIdentity)
        {
            RequireFrame(frameIdentity);
            m_Module.SealFrame(m_Frame);
            m_Frame = default;
        }

        internal void DiscardFrame(ulong frameIdentity)
        {
            RequireFrame(frameIdentity);
            m_Module.DiscardFrame(m_Frame);
            m_Frame = default;
        }

        internal void PublishCommittedFrameDiagnostics(
            RuntimeDiagnosticsContext diagnostics,
            in MotionMatchingFrameResolution resolution)
        {
            RequireAlive();
            m_Module.PublishCommittedFrameDiagnostics(
                diagnostics,
                in resolution);
        }

        internal void Reset(
            ulong resetSequence,
            MotionMatchingPresentationResetReason reason)
        {
            RequireAlive();
            if (m_Frame.IsValid)
            {
                throw new InvalidOperationException(
                    "Motion Matching source cannot reset during a frame.");
            }
            m_Module.Reset(resetSequence, reason);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Frame = default;
            m_Module.Dispose();
        }

        void RequireFrame(ulong frameIdentity)
        {
            RequireAlive();
            if (frameIdentity == 0 ||
                !m_Frame.IsValid ||
                m_Frame.FrameIdentity != frameIdentity)
            {
                throw new InvalidOperationException(
                    "Motion Matching source frame is stale.");
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(
                    nameof(CharacterPoseMotionMatchingSourceRuntime));
            }
        }
    }
}
