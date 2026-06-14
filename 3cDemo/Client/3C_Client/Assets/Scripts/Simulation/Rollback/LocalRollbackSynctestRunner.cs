using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public interface ILocalRollbackSynctestSimulation
    {
        CharacterSimulationSnapshot CaptureSnapshot(SimulationTick tick);
        void Restore(in CharacterSimulationSnapshot snapshot);
        void Advance(in PredictionInputFrame input);
    }

    public interface ILocalRollbackDebugRestoreCleanup
    {
        void CompleteDebugRestore();
    }

    public readonly struct LocalRollbackSynctestResult
    {
        public LocalRollbackSynctestResult(
            bool success,
            SimulationTick startTick,
            SimulationTick endTick,
            SimulationTick restoreTick,
            string failureReason,
            CharacterSimulationSnapshotComparison comparison)
            : this(
                success,
                startTick,
                endTick,
                restoreTick,
                failureReason,
                comparison,
                LocalRollbackSynctestFirstMismatch.None)
        {
        }

        public LocalRollbackSynctestResult(
            bool success,
            SimulationTick startTick,
            SimulationTick endTick,
            SimulationTick restoreTick,
            string failureReason,
            CharacterSimulationSnapshotComparison comparison,
            in LocalRollbackSynctestFirstMismatch firstMismatch)
        {
            Success = success;
            StartTick = startTick;
            EndTick = endTick;
            RestoreTick = restoreTick;
            FailureReason = failureReason ?? string.Empty;
            Comparison = comparison;
            FirstMismatch = firstMismatch;
        }

        public bool Success { get; }
        public SimulationTick StartTick { get; }
        public SimulationTick EndTick { get; }
        public SimulationTick RestoreTick { get; }
        public string FailureReason { get; }
        public CharacterSimulationSnapshotComparison Comparison { get; }
        public LocalRollbackSynctestFirstMismatch FirstMismatch { get; }
    }

    public enum LocalRollbackSynctestMismatchStage
    {
        None,
        Restore,
        Replay
    }

    public readonly struct LocalRollbackSynctestFirstMismatch
    {
        public LocalRollbackSynctestFirstMismatch(
            LocalRollbackSynctestMismatchStage stage,
            SimulationTick tick,
            bool hasInput,
            PredictionInputFrame input,
            CharacterSimulationSnapshot expected,
            CharacterSimulationSnapshot actual,
            CharacterSimulationSnapshotComparison comparison)
        {
            Stage = stage;
            Tick = tick;
            HasInput = hasInput;
            Input = input;
            Expected = expected;
            Actual = actual;
            Comparison = comparison;
            HasMismatch = stage != LocalRollbackSynctestMismatchStage.None && !comparison.Matches;
            HasPresentationDrift = stage != LocalRollbackSynctestMismatchStage.None && comparison.HasPresentationDifferences;
        }

        public bool HasMismatch { get; }
        public bool HasPresentationDrift { get; }
        public bool HasAnyDifference => HasMismatch || HasPresentationDrift;
        public LocalRollbackSynctestMismatchStage Stage { get; }
        public SimulationTick Tick { get; }
        public bool HasInput { get; }
        public PredictionInputFrame Input { get; }
        public CharacterSimulationSnapshot Expected { get; }
        public CharacterSimulationSnapshot Actual { get; }
        public CharacterSimulationSnapshotComparison Comparison { get; }

        public static LocalRollbackSynctestFirstMismatch None => new LocalRollbackSynctestFirstMismatch(
            LocalRollbackSynctestMismatchStage.None,
            SimulationTick.Zero,
            false,
            default,
            default,
            default,
            new CharacterSimulationSnapshotComparison(true, System.Array.Empty<string>()));
    }

    public sealed class LocalRollbackSynctestRunner
    {
        readonly List<PredictionInputFrame> replayInputs = new List<PredictionInputFrame>();

        public LocalRollbackSynctestRunner(
            PredictionInputHistory inputHistory,
            PredictionSnapshotHistory snapshotHistory,
            ILocalRollbackSynctestSimulation simulation)
        {
            InputHistory = inputHistory ?? throw new ArgumentNullException(nameof(inputHistory));
            SnapshotHistory = snapshotHistory ?? throw new ArgumentNullException(nameof(snapshotHistory));
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        }

        public PredictionInputHistory InputHistory { get; }
        public PredictionSnapshotHistory SnapshotHistory { get; }
        public ILocalRollbackSynctestSimulation Simulation { get; }

        public LocalRollbackSynctestResult Run(
            SimulationTick startTick,
            SimulationTick endTick,
            SimulationTick restoreTick,
            in CharacterSimulationSnapshotTolerance tolerance)
        {
            if (endTick < startTick)
                return Fail(startTick, endTick, restoreTick, "end tick before start tick");
            if (restoreTick < startTick || restoreTick > endTick)
                return Fail(startTick, endTick, restoreTick, "restore tick outside range");
            if (!SnapshotHistory.TryGet(restoreTick, out CharacterSimulationSnapshot restoreSnapshot))
                return Fail(startTick, endTick, restoreTick, $"missing snapshot {restoreTick.Value}");
            if (!SnapshotHistory.TryGet(endTick, out CharacterSimulationSnapshot expectedSnapshot))
                return Fail(startTick, endTick, restoreTick, $"missing snapshot {endTick.Value}");

            PredictionHistoryQueryResult inputResult = InputHistory.TryReadRange(restoreTick.Next, endTick, replayInputs);
            if (!inputResult.Success)
                return Fail(startTick, endTick, restoreTick, $"missing input {inputResult.Tick.Value}");

            LocalRollbackSynctestFirstMismatch firstMismatch = LocalRollbackSynctestFirstMismatch.None;

            Simulation.Restore(in restoreSnapshot);
            CharacterSimulationSnapshot restoredSnapshot = Simulation.CaptureSnapshot(restoreTick);
            CharacterSimulationSnapshotComparison restoreComparison =
                CharacterSimulationSnapshotComparer.Compare(in restoreSnapshot, in restoredSnapshot, in tolerance);
            if (!restoreComparison.Matches)
            {
                firstMismatch = new LocalRollbackSynctestFirstMismatch(
                    LocalRollbackSynctestMismatchStage.Restore,
                    restoreTick,
                    false,
                    default,
                    restoreSnapshot,
                    restoredSnapshot,
                    restoreComparison);
            }
            else if (restoreComparison.HasPresentationDifferences)
            {
                firstMismatch = new LocalRollbackSynctestFirstMismatch(
                    LocalRollbackSynctestMismatchStage.Restore,
                    restoreTick,
                    false,
                    default,
                    restoreSnapshot,
                    restoredSnapshot,
                    restoreComparison);
            }

            for (int i = 0; i < replayInputs.Count; i++)
            {
                Simulation.Advance(replayInputs[i]);

                if (firstMismatch.HasMismatch)
                    continue;

                PredictionInputFrame input = replayInputs[i];
                if (!SnapshotHistory.TryGet(input.Tick, out CharacterSimulationSnapshot expectedStepSnapshot))
                    continue;

                CharacterSimulationSnapshot actualStepSnapshot = Simulation.CaptureSnapshot(input.Tick);
                CharacterSimulationSnapshotComparison stepComparison =
                    CharacterSimulationSnapshotComparer.Compare(in expectedStepSnapshot, in actualStepSnapshot, in tolerance);
                if (!stepComparison.Matches)
                {
                    firstMismatch = new LocalRollbackSynctestFirstMismatch(
                        LocalRollbackSynctestMismatchStage.Replay,
                        input.Tick,
                        true,
                        input,
                        expectedStepSnapshot,
                        actualStepSnapshot,
                        stepComparison);
                }
                else if (!firstMismatch.HasAnyDifference && stepComparison.HasPresentationDifferences)
                {
                    firstMismatch = new LocalRollbackSynctestFirstMismatch(
                        LocalRollbackSynctestMismatchStage.Replay,
                        input.Tick,
                        true,
                        input,
                        expectedStepSnapshot,
                        actualStepSnapshot,
                        stepComparison);
                }
            }

            CharacterSimulationSnapshot actualSnapshot = Simulation.CaptureSnapshot(endTick);
            CharacterSimulationSnapshotComparison comparison =
                CharacterSimulationSnapshotComparer.Compare(in expectedSnapshot, in actualSnapshot, in tolerance);
            bool success = comparison.Matches && !firstMismatch.HasMismatch;

            return new LocalRollbackSynctestResult(
                success,
                startTick,
                endTick,
                restoreTick,
                ResolveFailureReason(comparison.Matches, in firstMismatch),
                comparison,
                in firstMismatch);
        }

        static string ResolveFailureReason(bool finalMatches, in LocalRollbackSynctestFirstMismatch firstMismatch)
        {
            if (!firstMismatch.HasMismatch)
                return finalMatches ? string.Empty : "snapshot mismatch";

            return finalMatches ? "first mismatch" : "first mismatch and snapshot mismatch";
        }

        static LocalRollbackSynctestResult Fail(SimulationTick startTick, SimulationTick endTick, SimulationTick restoreTick, string reason)
        {
            return new LocalRollbackSynctestResult(
                false,
                startTick,
                endTick,
                restoreTick,
                reason,
                new CharacterSimulationSnapshotComparison(false, new[] { reason }));
        }
    }
}
