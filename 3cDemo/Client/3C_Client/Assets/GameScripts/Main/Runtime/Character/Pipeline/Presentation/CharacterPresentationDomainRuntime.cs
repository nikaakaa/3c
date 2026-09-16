using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterPresentationDomainRuntime :
        IDisposable,
        ICharacterPresentationDomainRuntime
    {
        readonly ActorId m_ActorId;
        readonly CharacterBodyPresentationRuntime m_Body;
        readonly List<CharacterPresentationCommand> m_PendingCommands = new();
        readonly List<CharacterPresentationCommand> m_ActiveCommands = new();
        readonly List<CharacterPresentationTrajectoryIntent> m_PendingTrajectories = new();
        readonly List<IReadOnlyList<EquipmentVisualSelection>> m_PendingEquipment = new();
        bool m_Disposed;

        internal CharacterPresentationDomainRuntime(
            ActorId actorId,
            CharacterBodyPresentationRuntime body)
        {
            m_ActorId = actorId;
            m_Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public bool AcceptsTrajectoryIntent => true;
        public ulong BodyResetSequence => m_Body.ResetSequence;
        public bool SupportsCheckpointCapture => false;
        public bool SupportsCheckpointRestore => false;

        public bool TryGetLatestBody(out CharacterPresentationBodyState body) =>
            m_Body.TryGetLatestBody(out body);

        public void CaptureBodyTransaction(IReadOnlyList<CharacterPresentationBodyInterval> intervals) =>
            m_Body.CaptureTransaction(intervals);

        public void CaptureTrajectoryIntent(CharacterPresentationTrajectoryIntent intent)
        {
            if (intent.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation trajectory targets another Actor.");
            m_PendingTrajectories.Add(intent);
        }

        public void CaptureEquipmentSelections(IReadOnlyList<EquipmentVisualSelection> selections)
        {
            if (selections == null || selections.Count == 0)
                return;
            for (int i = 0; i < selections.Count; i++)
            {
                if (selections[i].ActorId != m_ActorId)
                    throw new InvalidOperationException("Presentation equipment selection targets another Actor.");
            }
            m_PendingEquipment.Add(selections);
        }

        public void Publish(CharacterPresentationCommand command)
        {
            RequireCommandActor(command);
            m_PendingCommands.Add(command);
        }

        public void Replace(CharacterPresentationCommand current, CharacterPresentationCommand replacement)
        {
            RequireCommandActor(current);
            RequireCommandActor(replacement);
            int index = m_PendingCommands.FindIndex(command => command.Header.EventId.Equals(current.Header.EventId));
            if (index < 0)
                throw new InvalidOperationException($"Presentation replacement target '{current.Header.EventId}' is not pending.");
            m_PendingCommands[index] = replacement;
        }

        public void Retire(CharacterPresentationCommand command)
        {
            RequireCommandActor(command);
            m_PendingCommands.RemoveAll(candidate => candidate.Header.EventId.Equals(command.Header.EventId));
        }

        public void Reset()
        {
            m_Body.Reset();
            m_PendingCommands.Clear();
            m_ActiveCommands.Clear();
            m_PendingTrajectories.Clear();
            m_PendingEquipment.Clear();
        }

        public CharacterPresentationDomainDiagnosticsSnapshot CaptureDiagnostics() =>
            new(
                0,
                0,
                m_Body.FollowerPositionCorrectionMeters,
                m_Body.FollowerYawCorrectionDegrees);

        public bool TryCaptureCheckpoint(SimulationSessionCheckpoint checkpoint, out string error)
        {
            checkpoint = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint));
            error = "Character Presentation checkpoint capture is not composed.";
            return false;
        }

        public bool TryRestoreCheckpoint(SimulationSessionCheckpoint checkpoint, out string error)
        {
            checkpoint = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint));
            error = "Character Presentation checkpoint restore is not composed.";
            return false;
        }

        public void PresentationFrame(GameplayPresentationFrameContext context)
        {
            CharacterBodyPresentationFrame bodyFrame = m_Body.Present(context);
            if (!bodyFrame.IsValid)
                return;
            ApplyCommandTransaction();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Body.Dispose();
        }

        void ApplyCommandTransaction()
        {
            m_ActiveCommands.AddRange(m_PendingCommands);
            m_PendingCommands.Clear();
        }

        void RequireCommandActor(CharacterPresentationCommand command)
        {
            if (command.Header.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation command targets another Actor.");
        }
    }
}



