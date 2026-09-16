using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterPresentationDomainRuntime :
        IDisposable,
        ICharacterPresentationDomainRuntime
    {
        readonly ActorId m_ActorId;
        readonly CharacterBodyPresentationRuntime m_Body;
        bool m_Disposed;

        internal CharacterPresentationDomainRuntime(
            ActorId actorId,
            CharacterBodyPresentationRuntime body)
        {
            m_ActorId = actorId;
            m_Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public bool AcceptsTrajectoryIntent => false;
        public ulong BodyResetSequence => m_Body.ResetSequence;
        public bool SupportsCheckpointCapture => false;
        public bool SupportsCheckpointRestore => false;

        public bool TryGetLatestBody(out CharacterPresentationBodyState body) =>
            m_Body.TryGetLatestBody(out body);

        public void CaptureBodyTransaction(IReadOnlyList<CharacterPresentationBodyInterval> intervals) =>
            m_Body.CaptureTransaction(intervals);

        public void CaptureTrajectoryIntent(CharacterPresentationTrajectoryIntent intent)
        {
            RequireCommandActor(intent.ActorId);
            throw new InvalidOperationException(
                "Trajectory capture requires a composed Pose runtime.");
        }

        public void CaptureEquipmentSelections(IReadOnlyList<EquipmentVisualSelection> selections)
        {
            if (selections != null)
            {
                for (int i = 0; i < selections.Count; i++)
                    RequireCommandActor(selections[i].ActorId);
            }
            throw new InvalidOperationException(
                "Equipment presentation requires a composed Equipment runtime.");
        }

        public void Publish(CharacterPresentationCommand command)
        {
            RequireCommandActor(command.Header.ActorId);
            throw new InvalidOperationException(
                "Presentation commands require composed Action, Timeline, and Pose runtimes.");
        }

        public void Replace(CharacterPresentationCommand current, CharacterPresentationCommand replacement)
        {
            RequireCommandActor(current.Header.ActorId);
            RequireCommandActor(replacement.Header.ActorId);
            throw new InvalidOperationException(
                "Presentation replacement requires composed Action, Timeline, and Pose runtimes.");
        }

        public void Retire(CharacterPresentationCommand command)
        {
            RequireCommandActor(command.Header.ActorId);
            throw new InvalidOperationException(
                "Presentation retirement requires composed Action, Timeline, and Pose runtimes.");
        }

        public void Reset() => m_Body.Reset();

        public CharacterPresentationDomainDiagnosticsSnapshot CaptureDiagnostics() =>
            new(
                0,
                0,
                m_Body.FollowerPositionCorrectionMeters,
                m_Body.FollowerYawCorrectionDegrees);

        public bool TryCaptureCheckpoint(SimulationSessionCheckpoint checkpoint, out string error)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            error = "Character Presentation checkpoint capture is not composed.";
            return false;
        }

        public bool TryRestoreCheckpoint(SimulationSessionCheckpoint checkpoint, out string error)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            error = "Character Presentation checkpoint restore is not composed.";
            return false;
        }

        public void PresentationFrame(GameplayPresentationFrameContext context) =>
            m_Body.Present(context);

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Body.Dispose();
        }

        void RequireCommandActor(ActorId actorId)
        {
            if (actorId != m_ActorId)
                throw new InvalidOperationException("Presentation input targets another Actor.");
        }
    }
}

