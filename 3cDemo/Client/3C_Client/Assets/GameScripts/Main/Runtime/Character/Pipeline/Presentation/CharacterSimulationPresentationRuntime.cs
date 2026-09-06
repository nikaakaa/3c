using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public sealed class CharacterSimulationPresentationRuntime :
        ICharacterPresentationRuntime,
        ISimulationPresentationOutputPort,
        IAnimationPresentationRuntimeSnapshotProvider
    {
        readonly ActorId m_ActorId;
        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterBodyPresentationRuntime m_Body;
        readonly CharacterPresentationFactProjector m_FactProjector;
        readonly CharacterAnimationPresentationRuntime m_Animation;
        readonly CharacterEquipmentVisualRuntime m_Equipment;
        readonly CharacterEquipmentLinkedPoseRuntime m_LinkedPose;
        readonly CharacterCameraPresentationRuntime m_Camera;
        readonly CharacterPoseWorkerPresentationSession
            m_WorkerPresentationSession;
        readonly CharacterPresentationFrameCoordinator m_FrameCoordinator;
        readonly List<CharacterPresentationCommand> m_CurrentFrameSignals =
            new List<CharacterPresentationCommand>();
        bool m_Disposed;

        internal CharacterSimulationPresentationRuntime(
            ActorId actorId,
            CharacterPresentationProjection projection,
            CharacterBodyPresentationRuntime body,
            CharacterAnimationPresentationRuntime animation,
            CharacterEquipmentVisualRuntime equipment,
            CharacterCameraPresentationRuntime camera,
            Transform poseRoot,
            RuntimeDiagnosticsContext diagnostics,
            CharacterPoseWorkerPresentationSession workerPresentationSession)
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
            if (requiresFootPlacement != m_Animation.HasFootPlacement)
                throw new InvalidOperationException("Foot Placement runtime must match the compiled Pose Graph node exactly.");
            m_Camera = camera;
            diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            m_WorkerPresentationSession = workerPresentationSession ??
                throw new ArgumentNullException(nameof(workerPresentationSession));
            m_FrameCoordinator = new CharacterPresentationFrameCoordinator(
                actorId,
                projection,
                m_Body,
                m_FactProjector,
                m_Animation,
                m_Equipment,
                m_LinkedPose.Session,
                m_Camera,
                poseRoot,
                diagnostics);
        }

        internal CharacterPoseWorkerPresentationSession
            WorkerPresentationSession => m_WorkerPresentationSession;

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
        public CharacterPosePlanStageSnapshot PosePlanStages =>
            m_FrameCoordinator.PosePlanStages;

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
            snapshot = m_FrameCoordinator.PosePlanStages;
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
                    m_FrameCoordinator.RecordAnimationBranchReplacement();
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

        internal void BeginPresentationFrame(
            GameplayPresentationFrameContext context)
        {
            RequireAlive();
            try
            {
                m_FrameCoordinator.Begin(context);
            }
            catch
            {
                m_CurrentFrameSignals.Clear();
                throw;
            }
        }

        internal bool TryAdvancePresentationFrame(
            out CharacterPoseWorkerStageLease workerLease)
        {
            RequireAlive();
            return m_FrameCoordinator.TryAdvance(out workerLease);
        }


        internal void CompletePresentationFrame()
        {
            RequireAlive();
            try
            {
                m_FrameCoordinator.Complete();
            }
            finally
            {
                m_CurrentFrameSignals.Clear();
            }
        }

        internal Exception AbortPresentationFrame()
        {
            RequireAlive();
            Exception failure = m_FrameCoordinator.Abort();
            m_CurrentFrameSignals.Clear();
            return failure;
        }

        public CharacterPresentationRuntimeDiagnosticsSnapshot CaptureDiagnostics()
        {
            return m_FrameCoordinator.CaptureDiagnostics();
        }

        public void Reset()
        {
            if (m_Disposed)
                return;
            m_CurrentFrameSignals.Clear();
            m_LinkedPose.Reset();
            m_Equipment.Reset();
            m_Camera?.Reset();
            m_FrameCoordinator.Reset();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_CurrentFrameSignals.Clear();
            m_FrameCoordinator.Dispose();
            CharacterPresentationModuleLifetime.Dispose(m_Camera, m_Equipment, m_Animation, m_Body);
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
