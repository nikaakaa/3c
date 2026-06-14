using System;

namespace ThirdPersonSimulation
{
    public readonly struct LocalRollbackSoakConfig
    {
        public LocalRollbackSoakConfig(int seed, int tickCount, int rollbackFrames, bool stopOnFailure)
        {
            Seed = seed;
            TickCount = tickCount < 0 ? 0 : tickCount;
            RollbackFrames = rollbackFrames < 1 ? 1 : rollbackFrames;
            StopOnFailure = stopOnFailure;
        }

        public int Seed { get; }
        public int TickCount { get; }
        public int RollbackFrames { get; }
        public bool StopOnFailure { get; }
    }

    public readonly struct LocalRollbackSoakResult
    {
        public LocalRollbackSoakResult(
            bool success,
            int seed,
            int tickCount,
            int rollbackFrames,
            int checkedWindows,
            string failureReason,
            in LocalRollbackSynctestResult firstFailure)
            : this(
                success,
                seed,
                tickCount,
                rollbackFrames,
                checkedWindows,
                failureReason,
                in firstFailure,
                false,
                default)
        {
        }

        public LocalRollbackSoakResult(
            bool success,
            int seed,
            int tickCount,
            int rollbackFrames,
            int checkedWindows,
            string failureReason,
            in LocalRollbackSynctestResult firstFailure,
            bool hasPresentationDrift,
            in LocalRollbackSynctestResult firstPresentationDrift)
        {
            Success = success;
            Seed = seed;
            TickCount = tickCount;
            RollbackFrames = rollbackFrames;
            CheckedWindows = checkedWindows;
            FailureReason = failureReason ?? string.Empty;
            FirstFailure = firstFailure;
            HasPresentationDrift = hasPresentationDrift;
            FirstPresentationDrift = firstPresentationDrift;
        }

        public bool Success { get; }
        public int Seed { get; }
        public int TickCount { get; }
        public int RollbackFrames { get; }
        public int CheckedWindows { get; }
        public string FailureReason { get; }
        public LocalRollbackSynctestResult FirstFailure { get; }
        public bool HasPresentationDrift { get; }
        public LocalRollbackSynctestResult FirstPresentationDrift { get; }
    }

    public sealed class LocalRollbackSoakRunner
    {
        public LocalRollbackSoakRunner(
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

        public LocalRollbackSoakResult Run(in LocalRollbackSoakConfig config, in CharacterSimulationSnapshotTolerance tolerance)
        {
            if (config.TickCount < config.RollbackFrames)
            {
                return new LocalRollbackSoakResult(
                    false,
                    config.Seed,
                    config.TickCount,
                    config.RollbackFrames,
                    0,
                    "tick count smaller than rollback window",
                    default);
            }

            LocalRollbackSynctestRunner synctest = new LocalRollbackSynctestRunner(InputHistory, SnapshotHistory, Simulation);
            LocalRollbackSynctestResult firstFailure = default;
            LocalRollbackSynctestResult firstPresentationDrift = default;
            int checkedWindows = 0;
            bool hasFailure = false;
            bool hasPresentationDrift = false;

            for (int end = config.RollbackFrames; end <= config.TickCount; end++)
            {
                SimulationTick endTick = new SimulationTick(end);
                SimulationTick restoreTick = endTick.Subtract(config.RollbackFrames);
                LocalRollbackSynctestResult result = synctest.Run(
                    SimulationTick.Zero,
                    endTick,
                    restoreTick,
                    in tolerance);
                checkedWindows++;

                if (!hasPresentationDrift && result.FirstMismatch.HasPresentationDrift)
                {
                    firstPresentationDrift = result;
                    hasPresentationDrift = true;
                }

                if (result.Success)
                    continue;

                if (!hasFailure)
                {
                    firstFailure = result;
                    hasFailure = true;
                }

                if (config.StopOnFailure)
                    break;
            }

            bool success = !hasFailure;
            return new LocalRollbackSoakResult(
                success,
                config.Seed,
                config.TickCount,
                config.RollbackFrames,
                checkedWindows,
                success ? string.Empty : firstFailure.FailureReason,
                in firstFailure,
                hasPresentationDrift,
                in firstPresentationDrift);
        }
    }
}
