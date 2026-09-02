using System;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseMotionMatchingCoordinator
    {
        readonly CharacterPoseProgramRuntime m_Program;
        readonly CharacterPoseSourceModule m_Source;
        readonly CharacterPoseFrameCoordinator m_Frame;

        internal CharacterPoseMotionMatchingCoordinator(
            CharacterPoseProgramRuntime program,
            CharacterPoseSourceModule source,
            CharacterPoseFrameCoordinator frame)
        {
            m_Program = program ?? throw new ArgumentNullException(nameof(program));
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        CharacterPoseMotionMatchingSourceRuntime Runtime =>
            m_Source.MotionMatching;
        internal bool Enabled => Runtime?.Enabled == true;
        internal bool AcceptsTrajectoryIntent =>
            Runtime?.AcceptsTrajectoryIntent == true;

        internal bool BeginFrame(ulong frameIdentity)
        {
            if (Runtime == null)
                return false;
            Runtime.BeginFrame(frameIdentity);
            return true;
        }

        internal void CommitFrame(CharacterPoseProgramFrameLease lease) =>
            Runtime?.CommitFrame(lease.FrameIdentity);

        internal void DiscardFrame(ulong frameIdentity) =>
            Runtime?.DiscardFrame(frameIdentity);

        internal MotionMatchingFrameResolution Resolve(
            ulong presentationFrame,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            out bool hasResolution)
        {
            m_Frame.RequireOpenMutation();
            m_Program.ClearMotionMatchingSelections(m_Frame.ActiveLease);
            hasResolution = false;
            if (Runtime == null)
                return default;
            MotionMatchingPoseStateDemandBatch demands =
                m_Program.BuildMotionMatchingDemandBatch(
                    m_Frame.ActiveLease,
                    presentationFrame,
                    bodyFrame.ResetSequence);
            if (!Runtime.HasFrameWork(
                    m_Frame.ActiveLease.FrameIdentity,
                    in demands))
            {
                return default;
            }
            MotionMatchingFrameResolution resolution = Runtime.ResolveFrame(
                m_Frame.ActiveLease.FrameIdentity,
                presentationFrame,
                presentationDeltaSeconds,
                in bodyFrame,
                in demands);
            m_Program.ApplyMotionMatchingSelections(
                m_Frame.ActiveLease,
                in resolution);
            hasResolution = true;
            return resolution;
        }

        internal void PrepareCompletion(
            in MotionMatchingFrameResolution resolution,
            ulong poseCompletionIdentity)
        {
            m_Frame.RequireOpenMutation();
            m_Program.PrepareMotionMatchingPosePlanCompletion(
                m_Frame.ActiveLease,
                in resolution,
                poseCompletionIdentity);
            RequireRuntime().PrepareFrameCompletion(
                m_Frame.ActiveLease.FrameIdentity,
                in resolution,
                poseCompletionIdentity);
        }

        internal void CompleteFrame()
        {
            m_Frame.RequireOpenMutation();
            if (m_Frame.PendingOutcome !=
                AnimationPresentationFrameOutcome.Committed)
            {
                throw new InvalidOperationException(
                    "Motion Matching Pose Plan completion does not match the evaluated frame.");
            }
            MotionMatchingPosePlanCompletion completion =
                m_Program.BuildMotionMatchingPosePlanCompletion(
                    m_Frame.ActiveLease);
            RequireRuntime().CompleteFrame(
                m_Frame.ActiveLease.FrameIdentity,
                in completion);
        }

        internal bool TryCaptureSearchReplay(
            string providerId,
            out MotionMatchingSearchReplayArtifact artifact)
        {
            artifact = null;
            return Runtime != null &&
                Runtime.TryCaptureSearchReplay(providerId, out artifact);
        }

        internal void CaptureTrajectoryIntent(
            CharacterPresentationTrajectoryIntent intent) =>
            RequireRuntime().CaptureTrajectoryIntent(intent);

        internal void CapturePreviewQuery(
            string providerId,
            MotionMatchingSearchReplayArtifact query) =>
            RequireRuntime().CapturePreviewQuery(providerId, query);

        internal void PublishCommittedFrameDiagnostics(
            RuntimeDiagnosticsContext diagnostics,
            in MotionMatchingFrameResolution resolution) =>
            RequireRuntime().PublishCommittedFrameDiagnostics(
                diagnostics,
                in resolution);

        internal void Reset(
            ulong resetSequence,
            MotionMatchingPresentationResetReason reason) =>
            Runtime?.Reset(resetSequence, reason);

        CharacterPoseMotionMatchingSourceRuntime RequireRuntime() =>
            Runtime ?? throw new InvalidOperationException(
                "Presentation has no Motion Matching module.");
    }
}
