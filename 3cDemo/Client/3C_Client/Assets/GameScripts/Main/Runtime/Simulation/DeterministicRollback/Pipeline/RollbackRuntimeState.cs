using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public readonly struct RollbackNetworkDiagnosticsSnapshot
    {
        public RollbackNetworkDiagnosticsSnapshot(
            ulong lastHashTick,
            StableHash lastWorldHash,
            StableHash lastKccHash,
            int localHashCount,
            int remoteHashCount,
            int pendingRecoveryCount,
            ulong recoveryCount,
            long droppedReceivedDatagrams,
            string lastDesyncScope)
        {
            LastHashTick = lastHashTick;
            LastWorldHash = lastWorldHash;
            LastKccHash = lastKccHash;
            LocalHashCount = localHashCount;
            RemoteHashCount = remoteHashCount;
            PendingRecoveryCount = pendingRecoveryCount;
            RecoveryCount = recoveryCount;
            DroppedReceivedDatagrams = droppedReceivedDatagrams;
            LastDesyncScope = lastDesyncScope ?? string.Empty;
        }

        public ulong LastHashTick { get; }
        public StableHash LastWorldHash { get; }
        public StableHash LastKccHash { get; }
        public int LocalHashCount { get; }
        public int RemoteHashCount { get; }
        public int PendingRecoveryCount { get; }
        public ulong RecoveryCount { get; }
        public long DroppedReceivedDatagrams { get; }
        public string LastDesyncScope { get; }
    }

    public interface IRollbackNetworkDiagnosticsSource
    {
        RollbackNetworkDiagnosticsSnapshot CaptureNetworkDiagnostics();
    }

    public readonly struct RollbackPresentationDiagnosticsSnapshot
    {
        public RollbackPresentationDiagnosticsSnapshot(
            ulong bodyBranchReplacementCount,
            ulong animationBranchReplacementCount,
            float followerPositionCorrectionMeters,
            float followerYawCorrectionDegrees)
        {
            BodyBranchReplacementCount = bodyBranchReplacementCount;
            AnimationBranchReplacementCount = animationBranchReplacementCount;
            FollowerPositionCorrectionMeters = followerPositionCorrectionMeters;
            FollowerYawCorrectionDegrees = followerYawCorrectionDegrees;
        }

        public ulong BodyBranchReplacementCount { get; }
        public ulong AnimationBranchReplacementCount { get; }
        public float FollowerPositionCorrectionMeters { get; }
        public float FollowerYawCorrectionDegrees { get; }
    }

    public readonly struct RollbackRuntimeDiagnosticsSnapshot
    {
        public RollbackRuntimeDiagnosticsSnapshot(
            int offensiveRequestDelayTicks,
            int confirmationDelayTicks,
            ulong lastCanonicalTick,
            ulong confirmedTick,
            ulong completedTick,
            ulong lateInputCount,
            ulong lastLateInputTick,
            ulong provenancePromotionCount,
            ulong explicitCorrectionCount,
            ulong lastExplicitAffectedTick,
            StableHash lastPredictedInputHash,
            StableHash lastCanonicalInputHash,
            StableHash lastAppliedInputHash,
            ulong rollbackCount,
            ulong replayedTickCount,
            int lastRollbackDepth,
            ulong recoveryCount,
            ulong lastPublishedHashTick,
            ulong pendingRecoveryTick,
            ulong requiredRecoveryTick,
            string recoveryReason,
            int inputHistoryCount,
            ulong inputHistoryFloor,
            ulong inputHistoryCeiling,
            int snapshotHistoryCount,
            ulong snapshotHistoryFloor,
            ulong snapshotHistoryCeiling,
            RollbackInputSourceDiagnosticsSnapshot inputSource,
            RollbackOutputLifecycleSnapshot output,
            RollbackPresentationDiagnosticsSnapshot presentation,
            RollbackNetworkDiagnosticsSnapshot network)
        {
            OffensiveRequestDelayTicks = offensiveRequestDelayTicks;
            ConfirmationDelayTicks = confirmationDelayTicks;
            LastCanonicalTick = lastCanonicalTick;
            ConfirmedTick = confirmedTick;
            CompletedTick = completedTick;
            LateInputCount = lateInputCount;
            LastLateInputTick = lastLateInputTick;
            ProvenancePromotionCount = provenancePromotionCount;
            ExplicitCorrectionCount = explicitCorrectionCount;
            LastExplicitAffectedTick = lastExplicitAffectedTick;
            LastPredictedInputHash = lastPredictedInputHash;
            LastCanonicalInputHash = lastCanonicalInputHash;
            LastAppliedInputHash = lastAppliedInputHash;
            RollbackCount = rollbackCount;
            ReplayedTickCount = replayedTickCount;
            LastRollbackDepth = lastRollbackDepth;
            RecoveryCount = recoveryCount;
            LastPublishedHashTick = lastPublishedHashTick;
            PendingRecoveryTick = pendingRecoveryTick;
            RequiredRecoveryTick = requiredRecoveryTick;
            RecoveryReason = recoveryReason ?? string.Empty;
            InputHistoryCount = inputHistoryCount;
            InputHistoryFloor = inputHistoryFloor;
            InputHistoryCeiling = inputHistoryCeiling;
            SnapshotHistoryCount = snapshotHistoryCount;
            SnapshotHistoryFloor = snapshotHistoryFloor;
            SnapshotHistoryCeiling = snapshotHistoryCeiling;
            InputSource = inputSource;
            Output = output;
            Presentation = presentation;
            Network = network;
        }

        public int OffensiveRequestDelayTicks { get; }
        public int ConfirmationDelayTicks { get; }
        public ulong LastCanonicalTick { get; }
        public ulong ConfirmedTick { get; }
        public ulong CompletedTick { get; }
        public ulong LateInputCount { get; }
        public ulong LastLateInputTick { get; }
        public ulong ProvenancePromotionCount { get; }
        public ulong ExplicitCorrectionCount { get; }
        public ulong LastExplicitAffectedTick { get; }
        public StableHash LastPredictedInputHash { get; }
        public StableHash LastCanonicalInputHash { get; }
        public StableHash LastAppliedInputHash { get; }
        public ulong RollbackCount { get; }
        public ulong ReplayedTickCount { get; }
        public int LastRollbackDepth { get; }
        public ulong RecoveryCount { get; }
        public ulong LastPublishedHashTick { get; }
        public ulong PendingRecoveryTick { get; }
        public ulong RequiredRecoveryTick { get; }
        public string RecoveryReason { get; }
        public int InputHistoryCount { get; }
        public ulong InputHistoryFloor { get; }
        public ulong InputHistoryCeiling { get; }
        public int SnapshotHistoryCount { get; }
        public ulong SnapshotHistoryFloor { get; }
        public ulong SnapshotHistoryCeiling { get; }
        public RollbackInputSourceDiagnosticsSnapshot InputSource { get; }
        public RollbackOutputLifecycleSnapshot Output { get; }
        public RollbackPresentationDiagnosticsSnapshot Presentation { get; }
        public RollbackNetworkDiagnosticsSnapshot Network { get; }
    }

    public sealed class RollbackRuntimeState
    {
        internal sealed class TransactionCheckpoint
        {
            internal TransactionCheckpoint(
                RollbackRuntimeState owner,
                IReadOnlyList<RollbackInputHistoryEntry> inputs,
                KeyValuePair<ulong, StableHash>[] appliedGameplayHashes,
                IRollbackInputSourceCheckpoint inputSourceCheckpoint)
            {
                Owner = owner;
                Inputs = inputs;
                AppliedGameplayHashes = appliedGameplayHashes;
                InputSourceCheckpoint = inputSourceCheckpoint;
                LastCanonicalContiguousTick = owner.m_LastCanonicalContiguousTick;
                RelayConfirmedTick = owner.m_RelayConfirmedTick;
                ConfirmedTick = owner.m_ConfirmedTick;
                ConfirmedTickAtTransactionStart = owner.m_ConfirmedTickAtTransactionStart;
                LastCompletedTick = owner.m_LastCompletedTick;
                RollbackCount = owner.m_RollbackCount;
                ReplayedTickCount = owner.m_ReplayedTickCount;
                LastRollbackDepth = owner.m_LastRollbackDepth;
                RecoveryCount = owner.m_RecoveryCount;
                LastPublishedHashTick = owner.m_LastPublishedHashTick;
                PendingRecoveryTick = owner.m_PendingRecoveryTick;
                RequiredRecoveryTick = owner.m_RequiredRecoveryTick;
                RecoveryReason = owner.m_RecoveryReason;
                LateInputCount = owner.m_LateInputCount;
                LastLateInputTick = owner.m_LastLateInputTick;
                LastPredictedInputTick = owner.m_LastPredictedInputTick;
                LastPredictedInputHash = owner.m_LastPredictedInputHash;
                LastCanonicalObservedTick = owner.m_LastCanonicalObservedTick;
                LastCanonicalInputHash = owner.m_LastCanonicalInputHash;
                LastAppliedInputTick = owner.m_LastAppliedInputTick;
                LastAppliedInputHash = owner.m_LastAppliedInputHash;
                EarliestExplicitAffectedTick = owner.m_EarliestExplicitAffectedTick;
                ProvenancePromotionCount = owner.m_ProvenancePromotionCount;
                ExplicitCorrectionCount = owner.m_ExplicitCorrectionCount;
                LastExplicitAffectedTick = owner.m_LastExplicitAffectedTick;
            }

            internal RollbackRuntimeState Owner { get; }
            internal IReadOnlyList<RollbackInputHistoryEntry> Inputs { get; }
            internal KeyValuePair<ulong, StableHash>[] AppliedGameplayHashes { get; }
            internal IRollbackInputSourceCheckpoint InputSourceCheckpoint { get; }
            internal ulong LastCanonicalContiguousTick { get; }
            internal ulong RelayConfirmedTick { get; }
            internal ulong ConfirmedTick { get; }
            internal ulong ConfirmedTickAtTransactionStart { get; }
            internal ulong LastCompletedTick { get; }
            internal ulong RollbackCount { get; }
            internal ulong ReplayedTickCount { get; }
            internal int LastRollbackDepth { get; }
            internal ulong RecoveryCount { get; }
            internal ulong LastPublishedHashTick { get; }
            internal ulong PendingRecoveryTick { get; }
            internal ulong RequiredRecoveryTick { get; }
            internal string RecoveryReason { get; }
            internal ulong LateInputCount { get; }
            internal ulong LastLateInputTick { get; }
            internal ulong LastPredictedInputTick { get; }
            internal StableHash LastPredictedInputHash { get; }
            internal ulong LastCanonicalObservedTick { get; }
            internal StableHash LastCanonicalInputHash { get; }
            internal ulong LastAppliedInputTick { get; }
            internal StableHash LastAppliedInputHash { get; }
            internal ulong EarliestExplicitAffectedTick { get; }
            internal ulong ProvenancePromotionCount { get; }
            internal ulong ExplicitCorrectionCount { get; }
            internal ulong LastExplicitAffectedTick { get; }
        }

        readonly DeterministicRollbackModelPolicy m_Policy;
        readonly StableHash m_RosterHash;
        readonly string m_LocalPeerId;
        readonly RollbackInputHistory m_Inputs;
        readonly RollbackSnapshotHistory m_Snapshots;
        readonly SortedDictionary<ulong, StableHash> m_AppliedGameplayHashes =
            new SortedDictionary<ulong, StableHash>();
        ulong m_LastCanonicalContiguousTick;
        ulong m_RelayConfirmedTick;
        ulong m_ConfirmedTick;
        ulong m_ConfirmedTickAtTransactionStart;
        ulong m_LastCompletedTick;
        ulong m_RollbackCount;
        ulong m_ReplayedTickCount;
        int m_LastRollbackDepth;
        ulong m_RecoveryCount;
        ulong m_LastPublishedHashTick;
        ulong m_PendingRecoveryTick;
        ulong m_RequiredRecoveryTick;
        string m_RecoveryReason = string.Empty;
        ulong m_LateInputCount;
        ulong m_LastLateInputTick;
        ulong m_LastPredictedInputTick;
        StableHash m_LastPredictedInputHash;
        ulong m_LastCanonicalObservedTick;
        StableHash m_LastCanonicalInputHash;
        ulong m_LastAppliedInputTick;
        StableHash m_LastAppliedInputHash;
        IRollbackInputSourcePort m_InputSource;
        ulong m_EarliestExplicitAffectedTick;
        ulong m_ProvenancePromotionCount;
        ulong m_ExplicitCorrectionCount;
        ulong m_LastExplicitAffectedTick;

        public RollbackRuntimeState(
            DeterministicRollbackModelPolicy policy,
            StableHash rosterHash,
            string localPeerId)
        {
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            if (!rosterHash.IsValid)
                throw new ArgumentException("Rollback roster hash is invalid.", nameof(rosterHash));
            m_RosterHash = rosterHash;
            m_LocalPeerId = SimulationIdentity.Require(localPeerId, nameof(localPeerId));
            m_Inputs = new RollbackInputHistory(policy.HistoryLengthTicks);
            m_Snapshots = new RollbackSnapshotHistory(policy.MaximumQueuedSnapshots);
        }

        public StableHash RosterHash => m_RosterHash;
        public string LocalPeerId => m_LocalPeerId;
        public ulong LastCanonicalContiguousTick => m_LastCanonicalContiguousTick;
        public ulong RelayConfirmedTick => m_RelayConfirmedTick;
        public ulong ConfirmedTick => m_ConfirmedTick;
        public ulong ConfirmedTickAtTransactionStart => m_ConfirmedTickAtTransactionStart;
        public ulong LastCompletedTick => m_LastCompletedTick;
        public ulong RollbackCount => m_RollbackCount;
        public ulong ReplayedTickCount => m_ReplayedTickCount;
        public int LastRollbackDepth => m_LastRollbackDepth;
        public ulong LastPublishedHashTick => m_LastPublishedHashTick;
        public RollbackInputHistory Inputs => m_Inputs;
        public RollbackSnapshotHistory Snapshots => m_Snapshots;

        public void BindInputSource(IRollbackInputSourcePort inputSource)
        {
            if (inputSource == null)
                throw new ArgumentNullException(nameof(inputSource));
            if (m_InputSource != null)
                throw new InvalidOperationException("Rollback Runtime State input Source is already bound.");
            m_InputSource = inputSource;
        }

        public RollbackRuntimeDiagnosticsSnapshot CaptureDiagnostics(
            RollbackOutputLifecycleSnapshot output,
            RollbackPresentationDiagnosticsSnapshot presentation,
            RollbackNetworkDiagnosticsSnapshot network)
        {
            return new RollbackRuntimeDiagnosticsSnapshot(
                m_Policy.OffensiveRequestDelayTicks,
                m_Policy.ConfirmationDelayTicks,
                m_LastCanonicalContiguousTick,
                m_ConfirmedTick,
                m_LastCompletedTick,
                m_LateInputCount,
                m_LastLateInputTick,
                m_ProvenancePromotionCount,
                m_ExplicitCorrectionCount,
                m_LastExplicitAffectedTick,
                m_LastPredictedInputHash,
                m_LastCanonicalInputHash,
                m_LastAppliedInputHash,
                m_RollbackCount,
                m_ReplayedTickCount,
                m_LastRollbackDepth,
                m_RecoveryCount,
                m_LastPublishedHashTick,
                m_PendingRecoveryTick,
                m_RequiredRecoveryTick,
                m_RecoveryReason,
                m_Inputs.Count,
                m_Inputs.FloorTick,
                m_Inputs.CeilingTick,
                m_Snapshots.Count,
                m_Snapshots.FloorTick,
                m_Snapshots.CeilingTick,
                m_InputSource?.CaptureDiagnostics() ?? default,
                output,
                presentation,
                network);
        }

        public void RecordPredicted(RollbackCanonicalInputBundle bundle)
        {
            m_Inputs.RecordPredicted(bundle);
            if (bundle.Tick.Value >= m_LastPredictedInputTick)
            {
                m_LastPredictedInputTick = bundle.Tick.Value;
                m_LastPredictedInputHash = bundle.GameplayHash;
            }
        }

        public void RecordRelayedExplicit(RollbackActorInputFrame frame)
        {
            if (frame == null || frame.Provenance != RollbackInputProvenance.RelayedExplicit)
                throw new ArgumentException("Rollback Runtime State accepts only relayed explicit input.", nameof(frame));
            RollbackInputHistoryEntry entry;
            try
            {
                entry = m_Inputs.GetRequired(frame.Tick);
            }
            catch (KeyNotFoundException)
            {
                return;
            }
            RollbackCanonicalInputBundle predicted = entry.Predicted;
            if (predicted == null)
                return;
            var actors = new RollbackActorInputFrame[predicted.Actors.Count];
            bool found = false;
            bool gameplayChanged = false;
            for (int i = 0; i < actors.Length; i++)
            {
                RollbackActorInputFrame current = predicted.Actors[i];
                if (!current.ActorId.Equals(frame.ActorId))
                {
                    actors[i] = current;
                    continue;
                }
                found = true;
                gameplayChanged = !current.GameplayHash.Equals(frame.GameplayHash);
                actors[i] = frame;
            }
            if (!found)
                throw new InvalidOperationException($"Rollback relayed Actor '{frame.ActorId}' is absent from predicted Tick '{frame.Tick}'.");
            if (!gameplayChanged)
            {
                m_ProvenancePromotionCount = checked(m_ProvenancePromotionCount + 1);
                return;
            }
            var replacement = new RollbackCanonicalInputBundle(
                frame.Tick,
                checked(predicted.BundleSequence + 1),
                actors);
            m_Inputs.RecordPredicted(replacement);
            if (frame.Tick.Value <= m_LastCompletedTick &&
                m_AppliedGameplayHashes.TryGetValue(frame.Tick.Value, out StableHash applied) &&
                !applied.Equals(replacement.GameplayHash))
            {
                m_ExplicitCorrectionCount = checked(m_ExplicitCorrectionCount + 1);
                if (m_EarliestExplicitAffectedTick == 0 || frame.Tick.Value < m_EarliestExplicitAffectedTick)
                    m_EarliestExplicitAffectedTick = frame.Tick.Value;
                m_LastExplicitAffectedTick = m_EarliestExplicitAffectedTick;
                RecordLateInput(frame.Tick);
            }
        }

        public void BeginOuterTransaction(ulong currentCompletedTick)
        {
            if (currentCompletedTick != m_LastCompletedTick)
            {
                throw new InvalidOperationException(
                    $"Rollback Pipeline completed Tick '{m_LastCompletedTick}' does not match outer transaction Tick '{currentCompletedTick}'.");
            }
            m_ConfirmedTickAtTransactionStart = m_ConfirmedTick;
        }

        public void RecordCanonical(RollbackCanonicalInputBundle bundle)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));
            if (bundle.Tick.Value <= m_ConfirmedTick)
                return;
            if (m_Inputs.FloorTick != 0 && bundle.Tick.Value < m_Inputs.FloorTick)
            {
                RecordLateInput(bundle.Tick);
                RequireSnapshotRecovery(bundle.Tick, "canonical-input-before-history-floor");
                return;
            }
            bool recorded = m_Inputs.RecordCanonical(bundle);
            if (!recorded)
                return;
            if (bundle.Tick.Value >= m_LastCanonicalObservedTick)
            {
                m_LastCanonicalObservedTick = bundle.Tick.Value;
                m_LastCanonicalInputHash = bundle.GameplayHash;
            }
            if (bundle.Tick.Value <= m_LastCompletedTick)
                RecordLateInput(bundle.Tick);
            AdvanceCanonicalHorizon();
        }

        public void RecordCompletedTick(SimulationTick tick)
        {
            if (!tick.IsValid)
                throw new ArgumentException("Rollback completed output Tick is invalid.", nameof(tick));
            ulong expected = checked(m_LastCompletedTick + 1);
            if (tick.Value != expected)
                throw new InvalidOperationException($"Rollback completed Tick '{tick}' is not contiguous after '{m_LastCompletedTick}'.");
            m_LastCompletedTick = tick.Value;
            AdvanceConfirmedHorizon();
        }

        public void RecordRelayConfirmedTick(SimulationTick tick)
        {
            if (!tick.IsValid)
                return;
            if (tick.Value < m_RelayConfirmedTick)
                throw new InvalidOperationException("Rollback Relay confirmed frontier regressed.");
            if (tick.Value > m_LastCanonicalContiguousTick)
                throw new InvalidOperationException("Rollback Relay confirmed frontier exceeds local canonical history.");
            m_RelayConfirmedTick = tick.Value;
            AdvanceConfirmedHorizon();
        }

        public bool TryFindEarliestMismatch(out SimulationTick tick)
        {
            if (m_EarliestExplicitAffectedTick != 0)
            {
                tick = new SimulationTick(m_EarliestExplicitAffectedTick);
                return true;
            }
            IReadOnlyList<RollbackInputHistoryEntry> entries = m_Inputs.CaptureEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                RollbackInputHistoryEntry entry = entries[i];
                if (entry.Canonical == null ||
                    !m_AppliedGameplayHashes.TryGetValue(entry.Tick.Value, out StableHash applied) ||
                    applied.Equals(entry.Canonical.GameplayHash))
                {
                    continue;
                }
                tick = entry.Tick;
                return true;
            }
            tick = default;
            return false;
        }

        public void BeginRollback(SimulationTick firstAffectedTick, ulong currentCompletedTick)
        {
            if (!firstAffectedTick.IsValid || firstAffectedTick.Value > currentCompletedTick)
                throw new ArgumentException("Rollback affected Tick range is invalid.");
            if (firstAffectedTick.Value <= m_ConfirmedTick)
                throw new InvalidOperationException($"Rollback cannot revise confirmed Tick '{firstAffectedTick}'.");
            int depth = checked((int)(currentCompletedTick - firstAffectedTick.Value + 1));
            if (depth > m_Policy.MaximumRollbackDepthTicks)
                throw new InvalidOperationException($"Rollback depth '{depth}' exceeds configured maximum '{m_Policy.MaximumRollbackDepthTicks}'.");
            m_EarliestExplicitAffectedTick = 0;
            RecordRollback(depth, depth);
        }

        public void BeginDeepRecoveryReplay(SimulationTick firstAffectedTick, ulong currentCompletedTick)
        {
            if (!firstAffectedTick.IsValid || firstAffectedTick.Value > currentCompletedTick ||
                firstAffectedTick.Value <= m_ConfirmedTick)
            {
                throw new ArgumentException("Rollback deep recovery range is invalid.");
            }
            int depth = checked((int)(currentCompletedTick - firstAffectedTick.Value + 1));
            if (depth <= m_Policy.MaximumRollbackDepthTicks)
                throw new InvalidOperationException("Rollback deep recovery must exceed the normal rollback depth limit.");
            m_EarliestExplicitAffectedTick = 0;
            m_RecoveryCount = checked(m_RecoveryCount + 1);
            RecordRollback(depth, depth);
        }

        public void RecordAppliedInput(SimulationTick tick)
        {
            RollbackInputHistoryEntry entry = m_Inputs.GetRequired(tick);
            RollbackCanonicalInputBundle applied = entry.Canonical ?? entry.Predicted ??
                throw new InvalidOperationException($"Rollback Tick '{tick}' has no applied input bundle.");
            m_AppliedGameplayHashes[tick.Value] = applied.GameplayHash;
            if (tick.Value >= m_LastAppliedInputTick)
            {
                m_LastAppliedInputTick = tick.Value;
                m_LastAppliedInputHash = applied.GameplayHash;
            }
        }

        void RecordLateInput(SimulationTick tick)
        {
            m_LateInputCount = checked(m_LateInputCount + 1);
            m_LastLateInputTick = tick.Value;
        }

        public void CaptureCommittedSnapshot(FixedSimulationSessionSnapshot snapshot, bool replaceExisting)
        {
            m_Snapshots.Capture(snapshot, replaceExisting);
        }

        public void RecordRollback(int depth, int replayedTicks)
        {
            if (depth <= 0 || replayedTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(depth));
            m_RollbackCount = checked(m_RollbackCount + 1);
            m_ReplayedTickCount = checked(m_ReplayedTickCount + (ulong)replayedTicks);
            m_LastRollbackDepth = depth;
        }

        public bool TryReserveNextHashTick(int cadenceTicks, out SimulationTick tick)
        {
            if (cadenceTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(cadenceTicks));
            ulong next = m_LastPublishedHashTick == 0
                ? (ulong)cadenceTicks
                : checked(m_LastPublishedHashTick + (ulong)cadenceTicks);
            if (next > m_ConfirmedTick)
            {
                tick = default;
                return false;
            }
            m_LastPublishedHashTick = next;
            tick = new SimulationTick(next);
            return true;
        }

        public void InstallRecoverySnapshot(FixedSimulationSessionSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.Tick.Value > m_LastCompletedTick)
                throw new InvalidOperationException("Rollback recovery snapshot is newer than the local completed horizon.");
            if (m_RequiredRecoveryTick != 0 && snapshot.Tick.Value < m_RequiredRecoveryTick)
                throw new InvalidOperationException("Rollback recovery snapshot does not cover the first affected Tick.");
            m_Snapshots.Capture(snapshot, true);
            if (m_PendingRecoveryTick != 0 && m_PendingRecoveryTick != snapshot.Tick.Value)
                throw new InvalidOperationException("Rollback already has a different pending recovery snapshot.");
            m_PendingRecoveryTick = snapshot.Tick.Value;
            m_RequiredRecoveryTick = 0;
            m_RecoveryReason = string.Empty;
            m_RecoveryCount = checked(m_RecoveryCount + 1);
        }

        public void RequireSnapshotRecovery(SimulationTick firstAffectedTick, string reason)
        {
            if (!firstAffectedTick.IsValid || firstAffectedTick.Value > m_LastCompletedTick)
                throw new ArgumentException("Rollback snapshot recovery Tick is invalid.", nameof(firstAffectedTick));
            reason = SimulationIdentity.Require(reason, nameof(reason));
            if (m_RequiredRecoveryTick == 0 || firstAffectedTick.Value < m_RequiredRecoveryTick)
            {
                m_RequiredRecoveryTick = firstAffectedTick.Value;
                m_RecoveryReason = reason;
            }
        }

        public bool TryGetRequiredRecovery(out SimulationTick tick, out string reason)
        {
            tick = m_RequiredRecoveryTick == 0 ? default : new SimulationTick(m_RequiredRecoveryTick);
            reason = m_RecoveryReason;
            return tick.IsValid;
        }

        public bool TryGetPendingRecovery(out SimulationTick tick)
        {
            tick = m_PendingRecoveryTick == 0 ? default : new SimulationTick(m_PendingRecoveryTick);
            return tick.IsValid;
        }

        public void MarkRecoveryScheduled(SimulationTick tick)
        {
            if (!tick.IsValid || tick.Value != m_PendingRecoveryTick)
                throw new InvalidOperationException("Rollback recovery schedule does not match the pending snapshot.");
            m_PendingRecoveryTick = 0;
        }

        internal TransactionCheckpoint CaptureTransactionCheckpoint()
        {
            var applied = new KeyValuePair<ulong, StableHash>[m_AppliedGameplayHashes.Count];
            int index = 0;
            foreach (KeyValuePair<ulong, StableHash> pair in m_AppliedGameplayHashes)
                applied[index++] = pair;
            if (m_InputSource == null)
                throw new InvalidOperationException("Rollback Runtime State has no bound input Source.");
            return new TransactionCheckpoint(
                this,
                m_Inputs.CaptureEntries(),
                applied,
                m_InputSource.CaptureCheckpoint());
        }

        internal void RestoreTransactionCheckpoint(TransactionCheckpoint checkpoint)
        {
            if (checkpoint == null || !ReferenceEquals(checkpoint.Owner, this))
                throw new ArgumentException("Rollback transaction checkpoint belongs to another runtime.", nameof(checkpoint));
            if (checkpoint.LastCanonicalContiguousTick < checkpoint.RelayConfirmedTick ||
                checkpoint.RelayConfirmedTick < checkpoint.ConfirmedTick ||
                checkpoint.LastCompletedTick < checkpoint.ConfirmedTick ||
                checkpoint.LastPublishedHashTick > checkpoint.ConfirmedTick ||
                checkpoint.PendingRecoveryTick > checkpoint.LastCompletedTick ||
                checkpoint.RequiredRecoveryTick > checkpoint.LastCompletedTick ||
                checkpoint.RequiredRecoveryTick == 0 && !string.IsNullOrEmpty(checkpoint.RecoveryReason) ||
                checkpoint.RequiredRecoveryTick != 0 && string.IsNullOrEmpty(checkpoint.RecoveryReason))
                throw new InvalidDataException("Rollback transactional state predates its confirmed horizon.");
            m_Inputs.RestoreEntries(checkpoint.Inputs);
            if (m_InputSource == null)
                throw new InvalidOperationException("Rollback Runtime State has no bound input Source.");
            m_InputSource.RestoreCheckpoint(checkpoint.InputSourceCheckpoint);
            m_AppliedGameplayHashes.Clear();
            foreach (KeyValuePair<ulong, StableHash> applied in checkpoint.AppliedGameplayHashes)
                m_AppliedGameplayHashes.Add(applied.Key, applied.Value);
            m_LastCanonicalContiguousTick = checkpoint.LastCanonicalContiguousTick;
            m_RelayConfirmedTick = checkpoint.RelayConfirmedTick;
            m_ConfirmedTick = checkpoint.ConfirmedTick;
            m_ConfirmedTickAtTransactionStart = checkpoint.ConfirmedTickAtTransactionStart;
            m_LastCompletedTick = checkpoint.LastCompletedTick;
            m_RollbackCount = checkpoint.RollbackCount;
            m_ReplayedTickCount = checkpoint.ReplayedTickCount;
            m_LastRollbackDepth = checkpoint.LastRollbackDepth;
            m_RecoveryCount = checkpoint.RecoveryCount;
            m_LastPublishedHashTick = checkpoint.LastPublishedHashTick;
            m_PendingRecoveryTick = checkpoint.PendingRecoveryTick;
            m_RequiredRecoveryTick = checkpoint.RequiredRecoveryTick;
            m_RecoveryReason = checkpoint.RecoveryReason;
            m_LateInputCount = checkpoint.LateInputCount;
            m_LastLateInputTick = checkpoint.LastLateInputTick;
            m_LastPredictedInputTick = checkpoint.LastPredictedInputTick;
            m_LastPredictedInputHash = checkpoint.LastPredictedInputHash;
            m_LastCanonicalObservedTick = checkpoint.LastCanonicalObservedTick;
            m_LastCanonicalInputHash = checkpoint.LastCanonicalInputHash;
            m_LastAppliedInputTick = checkpoint.LastAppliedInputTick;
            m_LastAppliedInputHash = checkpoint.LastAppliedInputHash;
            m_EarliestExplicitAffectedTick = checkpoint.EarliestExplicitAffectedTick;
            m_ProvenancePromotionCount = checkpoint.ProvenancePromotionCount;
            m_ExplicitCorrectionCount = checkpoint.ExplicitCorrectionCount;
            m_LastExplicitAffectedTick = checkpoint.LastExplicitAffectedTick;
        }

        public byte[] CaptureSimulationProjection()
        {
            if (m_AppliedGameplayHashes.Count > m_Policy.HistoryLengthTicks)
            {
                throw new InvalidOperationException(
                    $"Rollback applied-input history count '{m_AppliedGameplayHashes.Count}' exceeds configured capacity '{m_Policy.HistoryLengthTicks}' before snapshot capture.");
            }
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(0x50524244);
            writer.WriteInt32(1);
            writer.WriteString(m_Policy.ConfigurationHash.Value);
            writer.WriteString(m_RosterHash.Value);
            writer.WriteUInt64(m_LastCompletedTick);
            writer.WriteInt32(m_AppliedGameplayHashes.Count);
            foreach (KeyValuePair<ulong, StableHash> pair in m_AppliedGameplayHashes)
            {
                writer.WriteUInt64(pair.Key);
                writer.WriteString(pair.Value.Value);
            }
            return writer.ToArray();
        }

        public void RestoreSimulationProjection(byte[] payload)
        {
            var reader = new CanonicalReader(payload ?? throw new ArgumentNullException(nameof(payload)));
            if (reader.ReadUInt32() != 0x50524244 || reader.ReadInt32() != 1 ||
                !string.Equals(reader.ReadString(), m_Policy.ConfigurationHash.Value, StringComparison.Ordinal) ||
                !string.Equals(reader.ReadString(), m_RosterHash.Value, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Rollback simulation projection identity is invalid.");
            }
            ulong lastCompleted = reader.ReadUInt64();
            int appliedCount = ReadCount(reader, m_Policy.HistoryLengthTicks);
            var applied = new SortedDictionary<ulong, StableHash>();
            for (int i = 0; i < appliedCount; i++)
                applied.Add(reader.ReadUInt64(), new StableHash(reader.ReadString()));
            reader.RequireComplete();
            if (lastCompleted < m_ConfirmedTick)
                throw new InvalidDataException("Rollback simulation projection predates the confirmed horizon.");
            m_AppliedGameplayHashes.Clear();
            foreach (KeyValuePair<ulong, StableHash> pair in applied)
                m_AppliedGameplayHashes.Add(pair.Key, pair.Value);
            m_LastCompletedTick = lastCompleted;
        }

        void AdvanceCanonicalHorizon()
        {
            ulong next = m_LastCanonicalContiguousTick == 0 ? m_Inputs.FloorTick : checked(m_LastCanonicalContiguousTick + 1);
            if (next == 0)
                return;
            while (true)
            {
                try
                {
                    RollbackInputHistoryEntry entry = m_Inputs.GetRequired(new SimulationTick(next));
                    if (entry.Canonical == null)
                        break;
                }
                catch (KeyNotFoundException)
                {
                    break;
                }
                m_LastCanonicalContiguousTick = next;
                next = checked(next + 1);
            }
            AdvanceConfirmedHorizon();
        }

        void AdvanceConfirmedHorizon()
        {
            ulong candidate = Math.Min(m_RelayConfirmedTick, m_LastCompletedTick);
            for (ulong tick = checked(m_ConfirmedTick + 1); tick <= candidate; tick++)
            {
                RollbackInputHistoryEntry entry;
                try
                {
                    entry = m_Inputs.GetRequired(new SimulationTick(tick));
                }
                catch (KeyNotFoundException)
                {
                    candidate = tick - 1;
                    break;
                }
                if (entry.Canonical == null ||
                    !m_AppliedGameplayHashes.TryGetValue(tick, out StableHash applied) ||
                    !applied.Equals(entry.Canonical.GameplayHash) ||
                    !HasExplicitInputForEveryActor(entry.Canonical))
                {
                    candidate = tick - 1;
                    break;
                }
                if (tick == ulong.MaxValue)
                    break;
            }
            if (candidate > m_ConfirmedTick)
                m_ConfirmedTick = candidate;
            m_Inputs.DiscardThrough(m_ConfirmedTick);
            RollbackInputHistory.RemoveThrough(m_AppliedGameplayHashes, m_ConfirmedTick);
        }

        static bool HasExplicitInputForEveryActor(RollbackCanonicalInputBundle bundle)
        {
            for (int i = 0; i < bundle.Actors.Count; i++)
            {
                if (bundle.Actors[i].Provenance != RollbackInputProvenance.CanonicalExplicit)
                    return false;
            }
            return true;
        }

        static int ReadCount(CanonicalReader reader, int maximum)
        {
            int value = reader.ReadInt32();
            if (value < 0 || value > maximum)
                throw new InvalidDataException($"Rollback Pipeline state count '{value}' is invalid.");
            return value;
        }
    }
}
