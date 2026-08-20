using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using Unity.Profiling;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public sealed class CharacterSimulationPresentationRuntime :
        ICharacterPresentationRuntime,
        ISimulationPresentationOutputPort,
        IAnimationPresentationRuntimeSnapshotProvider
    {
        static readonly ProfilerMarker AnimationMarker = new ProfilerMarker("ThirdPerson.Presentation.Animation");
        static readonly ProfilerMarker EquipmentMarker = new ProfilerMarker("ThirdPerson.Presentation.Equipment");
        static readonly ProfilerMarker FactProjectionMarker = new ProfilerMarker("ThirdPerson.Presentation.FactProjection");
        static readonly ProfilerMarker FinalPoseMarker = new ProfilerMarker("ThirdPerson.Presentation.FinalPose");
        static readonly ProfilerMarker CameraMarker = new ProfilerMarker("ThirdPerson.Presentation.Camera");

        readonly ActorId m_ActorId;
        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterBodyPresentationRuntime m_Body;
        readonly CharacterPresentationFactProjector m_FactProjector;
        readonly CharacterAnimationPresentationRuntime m_Animation;
        readonly CharacterEquipmentVisualRuntime m_Equipment;
        readonly CharacterEquipmentLinkedPoseRuntime m_LinkedPose;
        readonly CharacterFootPlacementRuntime m_FootPlacement;
        readonly CharacterCameraPresentationRuntime m_Camera;
        readonly Transform m_VisualRoot;
        readonly Transform m_PoseRoot;
        readonly RuntimeDiagnosticsContext m_Diagnostics;
        readonly List<CharacterPresentationCommand> m_CurrentFrameSignals =
            new List<CharacterPresentationCommand>();

        bool m_PoseHasOutput;
        ulong m_LastBodyResetSequence;
        double m_LastAnimationSampleTick;
        bool m_AnimationClockInitialized;
        ulong m_AnimationBranchReplacementCount;
        bool m_ReportedPresentationFailure;
        FinalAnimationPoseFrame m_LastFinalPose;
        CharacterPosePlanStageSnapshot m_PosePlanStages;
        bool m_Disposed;

        internal CharacterSimulationPresentationRuntime(
            ActorId actorId,
            CharacterPresentationProjection projection,
            CharacterBodyPresentationRuntime body,
            CharacterAnimationPresentationRuntime animation,
            CharacterEquipmentVisualRuntime equipment,
            CharacterFootPlacementRuntime footPlacement,
            CharacterCameraPresentationRuntime camera,
            Transform poseRoot,
            RuntimeDiagnosticsContext diagnostics)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Presentation Runtime Actor identity is invalid.", nameof(actorId));
            m_ActorId = actorId;
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Body = body ?? throw new ArgumentNullException(nameof(body));
            m_FactProjector = new CharacterPresentationFactProjector(actorId);
            m_Animation = animation ?? throw new ArgumentNullException(nameof(animation));
            m_Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            m_LinkedPose = new CharacterEquipmentLinkedPoseRuntime(actorId, projection);
            bool requiresFootPlacement = projection.PosePlan.FootPlacements.Count == 1;
            if (requiresFootPlacement != (footPlacement != null))
                throw new InvalidOperationException("Foot Placement runtime must match the compiled Pose Graph node exactly.");
            m_FootPlacement = footPlacement;
            m_Camera = camera;
            m_VisualRoot = m_Body.VisualRoot;
            m_PoseRoot = poseRoot
                ? poseRoot
                : throw new ArgumentNullException(nameof(poseRoot));
            if (m_PoseRoot.parent != m_VisualRoot)
                throw new InvalidOperationException("PoseRoot must be a direct child of the Presentation VisualRoot.");
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public void CaptureBodyInterval(CharacterPresentationBodyInterval interval)
        {
            RequireAlive();
            if (interval.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation Body interval targets another Actor.");
            m_Body.Capture(interval);
            m_FactProjector.CaptureBodyBranch(
                m_Body.ResetSequence,
                m_Body.ResetReason);
        }

        public bool AcceptsTrajectoryIntent => true;
        public bool MotionMatchingRuntimeEnabled => m_Animation.MotionMatchingRuntimeEnabled;
        public AnimationPresentationDiagnosticsInterest DiagnosticsInterest =>
            m_Animation.DiagnosticsInterest;
        internal CharacterPoseTuningLayout TuningLayout =>
            m_Animation.TuningLayout;
        internal CharacterPoseTuningParameterBlock ActiveTuningBlock =>
            m_Animation.ActiveTuningBlock;
        internal CharacterPoseTuningRuntimeState TuningState =>
            m_Animation.TuningState;
        internal bool SubmitTuningCandidate(
            CharacterPoseTuningCandidate candidate,
            out string error) =>
            m_Animation.SubmitTuningCandidate(candidate, out error);
        internal void ClearPendingTuningCandidate() =>
            m_Animation.ClearPendingTuningCandidate();
        public ulong BodyResetSequence => m_Body.ResetSequence;
        public CharacterPosePlanStageSnapshot PosePlanStages => m_PosePlanStages;

        public bool TryGetAnimationPresentationDebugView(
            out AnimationPresentationDebugView debugView)
        {
            if (m_Disposed || !m_Animation.HasDebugView)
            {
                debugView = null;
                return false;
            }
            debugView = m_Animation.DebugView;
            return true;
        }

        public bool TryGetPosePlanStages(out CharacterPosePlanStageSnapshot snapshot)
        {
            snapshot = m_PosePlanStages;
            return !m_Disposed && snapshot.IsValid;
        }

        public bool TryCaptureMotionMatchingSearchReplay(
            string providerId,
            out MotionMatchingSearchReplayArtifact artifact)
        {
            if (m_Disposed)
            {
                artifact = null;
                return false;
            }
            return m_Animation.TryCaptureMotionMatchingSearchReplay(providerId, out artifact);
        }

        public void SetPoseWatchInterests(Guid ownerId, IReadOnlyList<AnimationPoseWatchIdentity> interests)
        {
            RequireAlive();
            m_Animation.SetPoseWatchInterests(ownerId, interests);
        }

        public void RemovePoseWatchInterests(Guid ownerId)
        {
            if (!m_Disposed)
                m_Animation.RemovePoseWatchInterests(ownerId);
        }

        public void SetDiagnosticsInterest(
            Guid ownerId,
            AnimationPresentationDiagnosticsInterest interest)
        {
            RequireAlive();
            m_Animation.SetDiagnosticsInterest(ownerId, interest);
        }

        public void RemoveDiagnosticsInterest(Guid ownerId)
        {
            if (!m_Disposed)
                m_Animation.RemoveDiagnosticsInterest(ownerId);
        }

        public void CaptureEquipmentSelections(IReadOnlyList<EquipmentVisualSelection> selections)
        {
            RequireAlive();
            m_LinkedPose.Capture(selections);
            m_Equipment.Capture(selections);
        }

        public void CaptureTrajectoryIntent(CharacterPresentationTrajectoryIntent intent)
        {
            RequireAlive();
            if (intent.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation Trajectory Intent targets another Actor.");
            m_FactProjector.CaptureIntent(intent);
            if (m_Animation.AcceptsMotionMatchingTrajectoryIntent)
                m_Animation.CaptureMotionMatchingTrajectoryIntent(intent);
        }

        public void CaptureBodyTransaction(IReadOnlyList<CharacterPresentationBodyInterval> intervals)
        {
            RequireAlive();
            m_Body.CaptureTransaction(intervals);
            m_FactProjector.CaptureBodyBranch(
                m_Body.ResetSequence,
                m_Body.ResetReason);
        }

        public void Publish(PresentationCommand command) =>
            Publish(CharacterPresentationCommand.FromFloat32(command));

        public void Publish(CharacterPresentationCommand command)
        {
            RequireAlive();
            if (command.Header.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation command targets another Actor.");
            CharacterPresentationProducerEntry producer = RequireProducer(command.ProducerId);
            switch (command.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                case CharacterPresentationCommandKind.SampleProducer:
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    m_Animation.Publish(command, producer);
                    break;
                case CharacterPresentationCommandKind.Camera:
                    RequireCamera().Publish(command, producer);
                    break;
                case CharacterPresentationCommandKind.Cue:
                    if (producer.Kind != CharacterPresentationProducerKind.Cue || producer.Cue == null)
                    {
                        throw new InvalidOperationException(
                            $"Cue command targets invalid Projection producer '{producer.ProgramProducerIdentity}'.");
                    }
                    m_CurrentFrameSignals.Add(command);
                    break;
                case CharacterPresentationCommandKind.Vfx:
                case CharacterPresentationCommandKind.Ui:
                    m_CurrentFrameSignals.Add(command);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command.Kind), command.Kind, null);
            }
        }

        public void Retire(CharacterPresentationCommand command)
        {
            RequireAlive();
            if (command.Header.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation retirement targets another Actor.");
            CharacterPresentationProducerEntry producer = RequireProducer(command.ProducerId);
            switch (command.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                case CharacterPresentationCommandKind.SampleProducer:
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    m_Animation.Retire(command, producer);
                    break;
                case CharacterPresentationCommandKind.Camera:
                    RequireCamera().Retire(command, producer);
                    break;
                case CharacterPresentationCommandKind.Cue:
                case CharacterPresentationCommandKind.Vfx:
                case CharacterPresentationCommandKind.Ui:
                    RetireSignal(command.Header.EventId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command.Kind), command.Kind, null);
            }
        }

        public void Replace(
            CharacterPresentationCommand current,
            CharacterPresentationCommand replacement)
        {
            RequireAlive();
            if (current.Header.ActorId != m_ActorId || replacement.Header.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation replacement targets another Actor.");
            CharacterPresentationProducerEntry currentProducer = RequireProducer(current.ProducerId);
            CharacterPresentationProducerEntry replacementProducer = RequireProducer(replacement.ProducerId);
            switch (replacement.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                case CharacterPresentationCommandKind.SampleProducer:
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    m_AnimationBranchReplacementCount = checked(m_AnimationBranchReplacementCount + 1);
                    m_Animation.Replace(current, replacement, currentProducer, replacementProducer);
                    break;
                case CharacterPresentationCommandKind.Camera:
                    RequireCamera().Retire(current, currentProducer);
                    RequireCamera().Publish(replacement, replacementProducer);
                    break;
                case CharacterPresentationCommandKind.Cue:
                case CharacterPresentationCommandKind.Vfx:
                case CharacterPresentationCommandKind.Ui:
                    RetireSignal(current.Header.EventId);
                    m_CurrentFrameSignals.Add(replacement);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(replacement.Kind), replacement.Kind, null);
            }
        }

        public void Present(GameplayPresentationFrameContext context)
        {
            RequireAlive();
            m_Diagnostics.BeginPresentationFrame(context.RenderFrame);
            try
            {
                using (EquipmentMarker.Auto())
                    m_Equipment.Present();
                CharacterBodyPresentationFrame bodyFrame = m_Body.Present(context);
                if (!bodyFrame.IsValid)
                {
                    m_AnimationClockInitialized = false;
                    ResetPoseIfNeeded(
                        context.RenderFrame,
                        m_LastBodyResetSequence,
                        CharacterFootPlacementResetReason.MissingAnimationOutput,
                        CharacterBodyPresentationResetReason.Initialization);
                    return;
                }
                if (bodyFrame.ResetSequence != m_LastBodyResetSequence)
                {
                    m_AnimationClockInitialized = false;
                    if (bodyFrame.ResetReason == CharacterBodyPresentationResetReason.CommittedBranchReplacement)
                    {
                        m_Animation.RetargetBodyBranch(bodyFrame.ResetSequence);
                        m_FootPlacement?.RetargetBodyBranch(bodyFrame.ResetSequence);
                    }
                    else
                    {
                        m_Animation.ResetPoseBranch(bodyFrame.ResetSequence);
                        m_FootPlacement?.Reset(new CharacterFootPlacementReset(
                            m_ActorId,
                            context.RenderFrame,
                            bodyFrame.ResetSequence,
                            CharacterFootPlacementResetReason.BodyStreamReset,
                            bodyFrame.ResetReason));
                        m_PoseHasOutput = false;
                    }
                    m_LastBodyResetSequence = bodyFrame.ResetSequence;
                }
                float animationDeltaSeconds = ResolveAnimationDeltaSeconds(in context, in bodyFrame);
                if (animationDeltaSeconds <= 0f)
                {
                    if (m_Camera != null)
                    {
                        using (CameraMarker.Auto())
                            m_Camera.Present(bodyFrame, context.PresentationDeltaSeconds);
                    }
                    return;
                }
                CharacterPresentationFactFrame factFrame;
                using (FactProjectionMarker.Auto())
                {
                    factFrame = m_FactProjector.Project(
                        context.RenderFrame,
                        animationDeltaSeconds,
                        in bodyFrame);
                }
                ComposedAnimationPoseFrame animationPose;
                try
                {
                    animationPose = PresentAnimation(
                        in bodyFrame,
                        in factFrame,
                        context.RenderFrame,
                        animationDeltaSeconds);
                }
                catch (Exception exception)
                {
                    if (!m_ReportedPresentationFailure)
                    {
                        m_ReportedPresentationFailure = true;
                        Debug.LogError(
                            $"Presentation failure Actor={m_ActorId}, Frame={context.RenderFrame}, " +
                            $"BodyTick={bodyFrame.PreviousTick}->{bodyFrame.CurrentTick}@{bodyFrame.SampleAlpha:R}, " +
                            $"Visible={bodyFrame.VisiblePosition:R}, VisualRoot={m_VisualRoot.position:R}, " +
                            $"PoseRoot={m_PoseRoot.position:R}, PoseRootLocal={m_PoseRoot.localPosition:R}, " +
                            $"CameraSkipped={m_Camera != null}, Error={exception.Message}");
                    }
                    throw;
                }
                using (FinalPoseMarker.Auto())
                    CommitFinalPose(bodyFrame, context, in animationPose);
                if (m_Camera != null)
                {
                    using (CameraMarker.Auto())
                        m_Camera.Present(bodyFrame, context.PresentationDeltaSeconds);
                }
            }
            finally
            {
                m_CurrentFrameSignals.Clear();
            }
        }

        public CharacterPresentationRuntimeDiagnosticsSnapshot CaptureDiagnostics()
        {
            return new CharacterPresentationRuntimeDiagnosticsSnapshot(
                m_Body.BranchReplacementCount,
                m_AnimationBranchReplacementCount,
                m_Body.FollowerPositionCorrectionMeters,
                m_Body.FollowerYawCorrectionDegrees,
                m_PosePlanStages,
                m_Animation.HasRuntimeDiagnosticsSnapshot,
                m_Animation.HasRuntimeDiagnosticsSnapshot
                    ? m_Animation.RuntimeDiagnosticsSnapshot
                    : default);
        }

        public void Reset()
        {
            if (m_Disposed)
                return;
            m_CurrentFrameSignals.Clear();
            m_LinkedPose.Reset();
            m_Equipment.Reset();
            m_Camera?.Reset();
            m_FootPlacement?.Reset(new CharacterFootPlacementReset(
                m_ActorId,
                0,
                0,
                CharacterFootPlacementResetReason.PresentationReset,
                CharacterBodyPresentationResetReason.Initialization));
            m_Animation.Reset();
            m_Body.Reset();
            m_FactProjector.Reset();
            m_PoseHasOutput = false;
            m_LastBodyResetSequence = 0;
            m_LastAnimationSampleTick = 0d;
            m_AnimationClockInitialized = false;
            m_AnimationBranchReplacementCount = 0;
            m_LastFinalPose = default;
            m_PosePlanStages = default;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_CurrentFrameSignals.Clear();
            CharacterPresentationModuleLifetime.Dispose(m_Camera, m_FootPlacement, m_Equipment, m_Animation, m_Body);
        }

        ComposedAnimationPoseFrame PresentAnimation(
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            ulong presentationFrame,
            float presentationDeltaSeconds)
        {
            using (AnimationMarker.Auto())
            {
                return m_Animation.Present(
                    presentationFrame,
                    bodyFrame.AnimationSampleTick,
                    bodyFrame.AnimationSampleAlpha,
                    presentationDeltaSeconds,
                    in bodyFrame,
                    in factFrame,
                    m_LinkedPose.Session,
                    m_FootPlacement,
                    m_Diagnostics);
            }
        }

        float ResolveAnimationDeltaSeconds(
            in GameplayPresentationFrameContext context,
            in CharacterBodyPresentationFrame bodyFrame)
        {
            if (m_Body.SourceMode != CharacterBodyPresentationSourceMode.CommittedStream)
                return context.PresentationDeltaSeconds;
            double sampleTick = (double)bodyFrame.PreviousTick +
                                ((double)bodyFrame.CurrentTick - bodyFrame.PreviousTick) *
                                (double)bodyFrame.SampleAlpha;
            if (!m_AnimationClockInitialized)
            {
                m_LastAnimationSampleTick = sampleTick;
                m_AnimationClockInitialized = true;
                return m_Body.TickDurationSeconds;
            }
            double deltaTicks = sampleTick - m_LastAnimationSampleTick;
            if (deltaTicks < -0.000001d)
                throw new InvalidOperationException("Animation presentation sample clock cannot move backward.");
            m_LastAnimationSampleTick = sampleTick;
            double deltaSeconds = Math.Max(0d, deltaTicks) * m_Body.TickDurationSeconds;
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) || deltaSeconds > float.MaxValue)
                throw new InvalidOperationException("Animation logic delta is invalid.");
            return (float)deltaSeconds;
        }

        void CommitFinalPose(
            CharacterBodyPresentationFrame bodyFrame,
            GameplayPresentationFrameContext context,
            in ComposedAnimationPoseFrame animationPose)
        {
            AnimationPoseAvailability availability;
            try
            {
                availability = animationPose.Availability;
            }
            catch (InvalidOperationException)
            {
                m_PosePlanStages = ShouldCapturePosePlanStages
                    ? CharacterPosePlanStageSnapshotFactory.Unavailable(
                        m_Projection.PosePlan,
                        AnimationPoseAvailability.Invalid,
                        CharacterPoseStageUnavailableReason.PoseUnavailable)
                    : default;
                ResetPoseIfNeeded(
                    context.RenderFrame,
                    bodyFrame.ResetSequence,
                    CharacterFootPlacementResetReason.MissingAnimationOutput,
                    bodyFrame.ResetReason);
                return;
            }
            if (availability != AnimationPoseAvailability.Pose)
            {
                m_PosePlanStages = ShouldCapturePosePlanStages
                    ? CharacterPosePlanStageSnapshotFactory.Unavailable(
                        m_Projection.PosePlan,
                        availability,
                        CharacterPoseStageUnavailableReason.PoseUnavailable)
                    : default;
                ResetPoseIfNeeded(
                    context.RenderFrame,
                    bodyFrame.ResetSequence,
                    CharacterFootPlacementResetReason.InvalidPose,
                    bodyFrame.ResetReason);
                return;
            }
            m_LastFinalPose = new FinalAnimationPoseFrame(in animationPose, animationPose.CompletionIdentity);
            m_PosePlanStages = ShouldCapturePosePlanStages
                ? CharacterPosePlanStageSnapshotFactory.Completed(
                    m_Projection.PosePlan,
                    in animationPose)
                : default;
            m_PoseHasOutput = true;
        }

        bool ShouldCapturePosePlanStages =>
            (m_Animation.DiagnosticsInterest &
             (AnimationPresentationDiagnosticsInterest.LiveState |
              AnimationPresentationDiagnosticsInterest.Capture)) != 0;

        void ResetPoseIfNeeded(
            ulong renderFrame,
            ulong resetSequence,
            CharacterFootPlacementResetReason reason,
            CharacterBodyPresentationResetReason bodyReason)
        {
            if (!m_PoseHasOutput)
                return;
            m_FootPlacement?.Reset(new CharacterFootPlacementReset(
                m_ActorId,
                renderFrame,
                resetSequence,
                reason,
                bodyReason));
            m_PoseHasOutput = false;
            m_LastFinalPose = default;
        }

        CharacterPresentationProducerEntry RequireProducer(string producerId)
        {
            if (!m_Projection.TryGetProducer(producerId, out CharacterPresentationProducerEntry producer))
            {
                throw new InvalidOperationException(
                    $"Presentation producer '{producerId}' is absent from the compiled Projection.");
            }
            return producer;
        }

        CharacterCameraPresentationRuntime RequireCamera()
        {
            return m_Camera ?? throw new InvalidOperationException(
                "Camera PresentationCommand targets an Actor without an explicit Camera composition.");
        }

        void RetireSignal(EventId eventId)
        {
            for (int i = m_CurrentFrameSignals.Count - 1; i >= 0; i--)
            {
                if (m_CurrentFrameSignals[i].Header.EventId.Equals(eventId))
                    m_CurrentFrameSignals.RemoveAt(i);
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterSimulationPresentationRuntime));
        }
    }
}
