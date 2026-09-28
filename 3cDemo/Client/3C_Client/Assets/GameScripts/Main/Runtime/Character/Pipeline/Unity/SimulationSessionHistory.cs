using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline
{
    internal sealed class SimulationSessionHistory
    {
        readonly SortedDictionary<ulong, SimulationSessionCheckpoint> m_Checkpoints =
            new SortedDictionary<ulong, SimulationSessionCheckpoint>();
        const int MaxCheckpointCount = 32;
        const ulong CheckpointInterval = 30;
        readonly List<ulong> m_CheckpointRemovalBuffer = new List<ulong>(MaxCheckpointCount);
        string m_LastCheckpointFailure = string.Empty;
        ulong m_LastLogicTick;
        Guid m_ExecutionBranchId = Guid.NewGuid();
        Guid m_ParentExecutionBranchId;
        ulong m_ExecutionBranchBaseTick;
        ISimulationSessionRuntimeHandle m_Runtime;
        IReadOnlyList<ISimulationActorRegistration> m_Registrations;

        internal ulong LatestCheckpointTick => GetLastCheckpointTick();
        internal ulong OldestCheckpointTick => GetFirstCheckpointTick();
        internal int CheckpointCount => m_Checkpoints.Count;
        internal string LastCheckpointFailure => m_LastCheckpointFailure;
        internal Guid ExecutionBranchId => m_ExecutionBranchId;
        internal Guid ParentExecutionBranchId => m_ParentExecutionBranchId;
        internal ulong ExecutionBranchBaseTick => m_ExecutionBranchBaseTick;

        internal void BindRuntime(ISimulationSessionRuntimeHandle runtime, IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            m_Runtime = runtime;
            m_Registrations = registrations;
        }

        internal void DetachRuntime()
        {
            m_Runtime = null;
            m_Registrations = null;
        }

        internal void Reset()
        {
            m_Checkpoints.Clear();
            m_LastCheckpointFailure = string.Empty;
            m_ExecutionBranchId = Guid.NewGuid();
            m_ParentExecutionBranchId = Guid.Empty;
            m_ExecutionBranchBaseTick = 0;
            m_LastLogicTick = 0;
        }

        internal static bool SupportsPresentationCheckpointRestore(IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            if (registrations.Count == 0)
                return false;
            for (int i = 0; i < registrations.Count; i++)
            {
                if (registrations[i] is not ISimulationPresentationCheckpointRuntime checkpoint ||
                    !checkpoint.SupportsPresentationCheckpointCapture ||
                    !checkpoint.SupportsPresentationCheckpointRestore)
                {
                    return false;
                }
            }
            return true;
        }

        internal bool TryCaptureCheckpointNow(out ulong checkpointTick, out string error)
        {
            checkpointTick = 0;
            error = string.Empty;
            if (m_LastLogicTick == 0)
            {
                error = "Simulation Session has not completed a Logic Tick for a skill checkpoint.";
                return false;
            }
            if (!TryCaptureCheckpoint(out error))
                return false;
            checkpointTick = m_LastLogicTick;
            return true;
        }

        internal bool TryRestoreCheckpoint(ulong tick, out string error)
        {
            error = string.Empty;
            if (!(m_Runtime is ISimulationSessionCheckpointRuntime checkpointRuntime))
            {
                error = "Active Session runtime does not expose checkpoint restore.";
                return false;
            }
            if (!m_Checkpoints.TryGetValue(tick, out SimulationSessionCheckpoint checkpoint))
            {
                error = $"Simulation Session has no checkpoint at Tick '{tick}'.";
                return false;
            }
            if (!IsCheckpointCompatible(checkpoint, out error))
                return false;
            return TryRestorePreparedCheckpoint(checkpointRuntime, checkpoint, out error);
        }

        bool TryRestorePreparedCheckpoint(
            ISimulationSessionCheckpointRuntime checkpointRuntime,
            SimulationSessionCheckpoint checkpoint,
            out string error)
        {
            if (!TryPreparePresentationCheckpointRestore(out error))
                return false;
            SimulationSessionCheckpoint rollbackCheckpoint = null;
            try
            {
                rollbackCheckpoint = checkpointRuntime.CaptureCheckpoint();
                if (!TryCapturePresentationCheckpoint(rollbackCheckpoint, out error))
                    return false;
                checkpointRuntime.RestoreCheckpoint(checkpoint);
                if (!TryRestorePresentationCheckpoint(checkpoint, out error))
                {
                    string restoreError = error;
                    TryRollbackSimulationRestore(
                        checkpointRuntime,
                        rollbackCheckpoint,
                        restoreError,
                        out error);
                    TryRollbackPresentationRestore(rollbackCheckpoint, error, out error);
                    return false;
                }
                CommitRestoreBranch(checkpoint);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                if (rollbackCheckpoint != null)
                {
                    TryRollbackSimulationRestore(
                        checkpointRuntime,
                        rollbackCheckpoint,
                        error,
                        out error);
                    TryRollbackPresentationRestore(rollbackCheckpoint, error, out error);
                }
                return false;
            }
        }

        internal bool TryRestoreToTick(ulong targetTick, out ulong restoredCheckpointTick, out string error)
        {
            restoredCheckpointTick = 0;
            error = string.Empty;
            SimulationSessionCheckpoint checkpoint = FindCheckpointAtOrBefore(targetTick);
            if (checkpoint == null)
            {
                error = $"Simulation Session has no checkpoint at or before Tick '{targetTick}'.";
                return false;
            }
            if (!IsCheckpointCompatible(checkpoint, out error))
                return false;
            restoredCheckpointTick = checkpoint.Tick.Value;
            if (restoredCheckpointTick == targetTick)
            {
                if (!(m_Runtime is ISimulationSessionCheckpointRuntime exactRuntime))
                {
                    error = "Active Session runtime does not expose checkpoint restore.";
                    return false;
                }
                return TryRestorePreparedCheckpoint(exactRuntime, checkpoint, out error);
            }
            if (!(m_Runtime is ISimulationSessionInputReplayRuntime replay))
            {
                error = "Active Session runtime does not expose input replay for a non-checkpoint historical Tick.";
                return false;
            }
            if (!(m_Runtime is ISimulationSessionCheckpointRuntime checkpointRuntime))
            {
                error = "Active Session runtime does not expose checkpoint rollback for historical input replay.";
                return false;
            }
            if (!TryPreparePresentationCheckpointRestore(out error))
                return false;
            return TryReplayPreparedCheckpoint(replay, checkpointRuntime, checkpoint, targetTick, out error);
        }

        internal bool TryReplayInputRange(ulong fromTick, ulong toTick, out string error)
        {
            if (fromTick >= toTick)
            {
                error = "Simulation Session input replay range is invalid.";
                return false;
            }
            if (!(m_Runtime is ISimulationSessionInputReplayRuntime replay))
            {
                error = "Active Session runtime does not expose input replay.";
                return false;
            }
            if (!m_Checkpoints.TryGetValue(fromTick, out SimulationSessionCheckpoint checkpoint))
            {
                error = $"Simulation Session has no checkpoint at Tick '{fromTick}'.";
                return false;
            }
            if (!IsCheckpointCompatible(checkpoint, out error))
                return false;
            if (!TryPreparePresentationCheckpointRestore(out error))
                return false;
            if (!(m_Runtime is ISimulationSessionCheckpointRuntime checkpointRuntime))
            {
                error = "Active Session runtime does not expose checkpoint rollback for input replay.";
                return false;
            }
            return TryReplayPreparedCheckpoint(replay, checkpointRuntime, checkpoint, toTick, out error);
        }

        bool TryReplayPreparedCheckpoint(
            ISimulationSessionInputReplayRuntime replay,
            ISimulationSessionCheckpointRuntime checkpointRuntime,
            SimulationSessionCheckpoint checkpoint,
            ulong toTick,
            out string error)
        {
            SimulationSessionCheckpoint rollbackCheckpoint;
            try
            {
                rollbackCheckpoint = checkpointRuntime.CaptureCheckpoint();
                if (!TryCapturePresentationCheckpoint(rollbackCheckpoint, out error))
                    return false;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
            if (!replay.TryReplayInputRange(checkpoint, checkpoint.Tick.Value, toTick, out error))
                return false;
            if (!TryRestorePresentationCheckpoint(checkpoint, out error))
            {
                string restoreError = error;
                TryRollbackInputReplay(
                    replay,
                    checkpointRuntime,
                    rollbackCheckpoint,
                    restoreError,
                    out error);
                TryRollbackPresentationRestore(rollbackCheckpoint, error, out error);
                return false;
            }
            CommitRestoreBranch(checkpoint);
            return true;
        }

        internal void CompleteLogicTick(ulong outerTick)
        {
            m_LastLogicTick = outerTick;
            if (outerTick == 0 || outerTick % CheckpointInterval != 0 && m_Checkpoints.Count != 0)
            {
                return;
            }
            TryCaptureCheckpoint(out _);
        }

        bool TryCaptureCheckpoint(out string error)
        {
            error = string.Empty;
            if (!(m_Runtime is ISimulationSessionCheckpointRuntime checkpointRuntime))
            {
                error = "Active Session runtime does not expose checkpoint capture.";
                return false;
            }
            try
            {
                SimulationSessionCheckpoint checkpoint = checkpointRuntime.CaptureCheckpoint();
                if (SupportsPresentationCheckpointRestore(m_Registrations) &&
                    !TryCapturePresentationCheckpoint(checkpoint, out string presentationError))
                {
                    throw new InvalidOperationException(presentationError);
                }
                for (int i = 0; i < m_Registrations.Count; i++)
                    m_Registrations[i].PublishCheckpoint(
                        checkpoint.Tick.Value,
                        checkpoint.SnapshotId,
                        checkpoint.SnapshotHash);
                m_Checkpoints[checkpoint.Tick.Value] = checkpoint;
                while (m_Checkpoints.Count > MaxCheckpointCount)
                    m_Checkpoints.Remove(GetFirstCheckpointTick());
                m_LastCheckpointFailure = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                m_LastCheckpointFailure = exception.Message;
                error = exception.Message;
                return false;
            }
        }

        ulong GetLastCheckpointTick()
        {
            ulong value = 0;
            foreach (ulong tick in m_Checkpoints.Keys)
                value = tick;
            return value;
        }

        ulong GetFirstCheckpointTick()
        {
            foreach (ulong tick in m_Checkpoints.Keys)
                return tick;
            return 0;
        }

        SimulationSessionCheckpoint FindCheckpointAtOrBefore(ulong targetTick)
        {
            SimulationSessionCheckpoint selected = null;
            foreach (KeyValuePair<ulong, SimulationSessionCheckpoint> pair in m_Checkpoints)
            {
                if (pair.Key > targetTick)
                    break;
                selected = pair.Value;
            }
            return selected;
        }

        bool IsCheckpointCompatible(SimulationSessionCheckpoint checkpoint, out string error)
        {
            SimulationSessionCompositionDescriptor descriptor = m_Runtime.Descriptor;
            if (!checkpoint.SessionId.Equals(descriptor.SessionId))
            {
                error = $"Checkpoint Tick '{checkpoint.Tick.Value}' belongs to Session '{checkpoint.SessionId}', but the active Session is '{descriptor.SessionId}'.";
                return false;
            }
            if (!checkpoint.GameplayContentHash.Equals(descriptor.GameplayContentHash))
            {
                error = $"Checkpoint Tick '{checkpoint.Tick.Value}' belongs to Gameplay Content '{checkpoint.GameplayContentHash}', but the active Session Content is '{descriptor.GameplayContentHash}'.";
                return false;
            }
            if (!checkpoint.PipelineHash.Equals(descriptor.Pipeline.Hash))
            {
                error = $"Checkpoint Tick '{checkpoint.Tick.Value}' belongs to Pipeline '{checkpoint.PipelineHash}', but the active Session Pipeline is '{descriptor.Pipeline.Hash}'.";
                return false;
            }
            if (!string.Equals(
                    checkpoint.BackendId,
                    descriptor.ExecutionBackend.ComponentId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    checkpoint.BackendSemanticVersion,
                    descriptor.ExecutionBackend.SemanticVersion,
                    StringComparison.Ordinal))
            {
                error = $"Checkpoint Tick '{checkpoint.Tick.Value}' belongs to Backend '{checkpoint.BackendId}@{checkpoint.BackendSemanticVersion}', but the active Session Backend is '{descriptor.ExecutionBackend.ComponentId}@{descriptor.ExecutionBackend.SemanticVersion}'.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        bool TryPreparePresentationCheckpointRestore(
            out string error)
        {
            for (int i = 0; i < m_Registrations.Count; i++)
            {
                if (m_Registrations[i] is ISimulationPresentationCheckpointRuntime checkpoint &&
                    checkpoint.SupportsPresentationCheckpointRestore)
                    continue;
                error = $"Actor '{m_Registrations[i].ActorId}' does not expose Presentation checkpoint restore.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        bool TryRestorePresentationCheckpoint(
            SimulationSessionCheckpoint checkpoint,
            out string error)
        {
            for (int i = 0; i < m_Registrations.Count; i++)
            {
                if (!((ISimulationPresentationCheckpointRuntime)m_Registrations[i])
                        .TryRestorePresentationCheckpoint(checkpoint, out error))
                    return false;
            }
            error = string.Empty;
            return true;
        }

        bool TryCapturePresentationCheckpoint(
            SimulationSessionCheckpoint checkpoint,
            out string error)
        {
            for (int i = 0; i < m_Registrations.Count; i++)
            {
                if (!((ISimulationPresentationCheckpointRuntime)m_Registrations[i])
                        .TryCapturePresentationCheckpoint(checkpoint, out error))
                    return false;
            }
            error = string.Empty;
            return true;
        }

        static void TryRollbackSimulationRestore(
            ISimulationSessionCheckpointRuntime checkpointRuntime,
            SimulationSessionCheckpoint rollbackCheckpoint,
            string failure,
            out string error)
        {
            try
            {
                checkpointRuntime.RestoreCheckpoint(rollbackCheckpoint);
                error = failure;
            }
            catch (Exception rollbackException)
            {
                error = $"{failure} Simulation rollback failed: {rollbackException.Message}";
            }
        }

        static void TryRollbackInputReplay(
            ISimulationSessionInputReplayRuntime replay,
            ISimulationSessionCheckpointRuntime checkpointRuntime,
            SimulationSessionCheckpoint rollbackCheckpoint,
            string failure,
            out string error)
        {
            string cancelError = string.Empty;
            bool canceled;
            try
            {
                canceled = replay.TryCancelInputReplay(out cancelError);
            }
            catch (Exception exception)
            {
                canceled = false;
                cancelError = exception.Message;
            }
            string rollbackError = string.Empty;
            bool restored;
            try
            {
                checkpointRuntime.RestoreCheckpoint(rollbackCheckpoint);
                restored = true;
            }
            catch (Exception exception)
            {
                restored = false;
                rollbackError = exception.Message;
            }
            if (canceled && restored)
            {
                error = failure;
                return;
            }
            string details = !canceled
                ? $"Input replay cancel failed: {cancelError}"
                : string.Empty;
            if (!restored)
                details = string.IsNullOrEmpty(details)
                    ? $"Simulation rollback failed: {rollbackError}"
                    : $"{details} Simulation rollback failed: {rollbackError}";
            error = $"{failure} Replay rollback failed: {details}";
        }

        void TryRollbackPresentationRestore(
            SimulationSessionCheckpoint rollbackCheckpoint,
            string failure,
            out string error)
        {
            try
            {
                if (TryRestorePresentationCheckpoint(rollbackCheckpoint, out string presentationError))
                {
                    error = failure;
                    return;
                }
                error = string.IsNullOrEmpty(presentationError)
                    ? $"{failure} Presentation rollback failed."
                    : $"{failure} Presentation rollback failed: {presentationError}";
            }
            catch (Exception exception)
            {
                error = $"{failure} Presentation rollback failed: {exception.Message}";
            }
        }

        void CommitRestoreBranch(SimulationSessionCheckpoint checkpoint)
        {
            m_CheckpointRemovalBuffer.Clear();
            foreach (ulong futureTick in m_Checkpoints.Keys)
                if (futureTick > checkpoint.Tick.Value)
                    m_CheckpointRemovalBuffer.Add(futureTick);
            for (int i = 0; i < m_CheckpointRemovalBuffer.Count; i++)
                m_Checkpoints.Remove(m_CheckpointRemovalBuffer[i]);
            m_CheckpointRemovalBuffer.Clear();
            m_ParentExecutionBranchId = m_ExecutionBranchId;
            m_ExecutionBranchId = Guid.NewGuid();
            m_ExecutionBranchBaseTick = checkpoint.Tick.Value;
            m_LastLogicTick = checkpoint.Tick.Value;
            for (int i = 0; i < m_Registrations.Count; i++)
                m_Registrations[i].BindExecutionBranch(m_ExecutionBranchId);
        }

    }
}
