using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public enum LocalLatencyReconciliationOutcome
    {
        None,
        NoCorrectionRequired,
        PredictionCorrection,
        ReplayNondeterminism,
        Failure
    }

    public readonly struct PredictionInputFrameDifference
    {
        public PredictionInputFrameDifference(
            bool hasDifference,
            PredictionInputFrame predictedInput,
            PredictionInputFrame resolvedInput,
            string[] differences)
        {
            HasDifference = hasDifference;
            PredictedInput = predictedInput;
            ResolvedInput = resolvedInput;
            Differences = differences ?? System.Array.Empty<string>();
        }

        public bool HasDifference { get; }
        public PredictionInputFrame PredictedInput { get; }
        public PredictionInputFrame ResolvedInput { get; }
        public IReadOnlyList<string> Differences { get; }

        public static PredictionInputFrameDifference None => new PredictionInputFrameDifference(
            false,
            default,
            default,
            System.Array.Empty<string>());

        public static PredictionInputFrameDifference Compare(
            in PredictionInputFrame predicted,
            in PredictionInputFrame resolved)
        {
            List<string> differences = new List<string>();
            if (predicted.Tick != resolved.Tick)
                differences.Add("tick");
            if (UnityEngine.Vector2.Distance(predicted.Move, resolved.Move) > 0.0001f)
                differences.Add("move");
            if (UnityEngine.Vector2.Distance(predicted.Look, resolved.Look) > 0.0001f)
                differences.Add("look");
            if (predicted.RunHeld != resolved.RunHeld)
                differences.Add("run");
            CompareButton("dodge", predicted.Dodge, resolved.Dodge, differences);
            CompareButton("attack", predicted.Attack, resolved.Attack, differences);
            CompareButton("jump", predicted.Jump, resolved.Jump, differences);
            CompareButton("interact", predicted.Interact, resolved.Interact, differences);
            if (predicted.HasCameraBasis != resolved.HasCameraBasis)
                differences.Add("cameraBasis.hasValue");
            if (predicted.HasCameraBasis && resolved.HasCameraBasis)
            {
                if (UnityEngine.Vector3.Distance(predicted.CameraBasisState.PlanarForward, resolved.CameraBasisState.PlanarForward) > 0.0001f)
                    differences.Add("cameraBasis.forward");
                if (UnityEngine.Vector3.Distance(predicted.CameraBasisState.PlanarRight, resolved.CameraBasisState.PlanarRight) > 0.0001f)
                    differences.Add("cameraBasis.right");
                if (UnityEngine.Mathf.Abs(UnityEngine.Mathf.DeltaAngle(predicted.CameraBasisState.Yaw, resolved.CameraBasisState.Yaw)) > 0.001f)
                    differences.Add("cameraBasis.yaw");
            }

            return new PredictionInputFrameDifference(
                differences.Count > 0,
                predicted,
                resolved,
                differences.ToArray());
        }

        static void CompareButton(
            string name,
            in PredictionButtonFrame predicted,
            in PredictionButtonFrame resolved,
            List<string> differences)
        {
            if (predicted.Pressed != resolved.Pressed)
                differences.Add(name + ".pressed");
            if (predicted.Held != resolved.Held)
                differences.Add(name + ".held");
            if (predicted.Released != resolved.Released)
                differences.Add(name + ".released");
        }
    }

    public readonly struct LocalLatencyReconciliationResult
    {
        public LocalLatencyReconciliationResult(
            bool success,
            SimulationTick? firstIncorrectTick,
            SimulationTick restoreTick,
            SimulationTick endTick,
            int replayFrameCount,
            CharacterSimulationSnapshotComparison comparison)
            : this(
                success,
                success
                    ? LocalLatencyReconciliationOutcome.NoCorrectionRequired
                    : LocalLatencyReconciliationOutcome.Failure,
                firstIncorrectTick,
                restoreTick,
                endTick,
                replayFrameCount,
                comparison,
                PredictionInputFrameDifference.None,
                LocalRollbackSynctestFirstMismatch.None)
        {
        }

        public LocalLatencyReconciliationResult(
            bool success,
            LocalLatencyReconciliationOutcome outcome,
            SimulationTick? firstIncorrectTick,
            SimulationTick restoreTick,
            SimulationTick endTick,
            int replayFrameCount,
            CharacterSimulationSnapshotComparison comparison,
            in PredictionInputFrameDifference predictionDifference,
            in LocalRollbackSynctestFirstMismatch replayFirstMismatch)
        {
            Success = success;
            Outcome = outcome;
            FirstIncorrectTick = firstIncorrectTick;
            RestoreTick = restoreTick;
            EndTick = endTick;
            ReplayFrameCount = replayFrameCount;
            Comparison = comparison;
            PredictionDifference = predictionDifference;
            ReplayFirstMismatch = replayFirstMismatch;
        }

        public bool Success { get; }
        public LocalLatencyReconciliationOutcome Outcome { get; }
        public SimulationTick? FirstIncorrectTick { get; }
        public SimulationTick RestoreTick { get; }
        public SimulationTick EndTick { get; }
        public int ReplayFrameCount { get; }
        public CharacterSimulationSnapshotComparison Comparison { get; }
        public PredictionInputFrameDifference PredictionDifference { get; }
        public LocalRollbackSynctestFirstMismatch ReplayFirstMismatch { get; }

        public static LocalLatencyReconciliationResult NoError(
            SimulationTick startTick,
            SimulationTick endTick)
        {
            return new LocalLatencyReconciliationResult(
                true,
                LocalLatencyReconciliationOutcome.NoCorrectionRequired,
                null,
                startTick,
                endTick,
                0,
                new CharacterSimulationSnapshotComparison(true, System.Array.Empty<string>()),
                PredictionInputFrameDifference.None,
                LocalRollbackSynctestFirstMismatch.None);
        }

        public static LocalLatencyReconciliationResult Fail(
            string reason,
            SimulationTick restoreTick,
            SimulationTick endTick)
        {
            return new LocalLatencyReconciliationResult(
                false,
                LocalLatencyReconciliationOutcome.Failure,
                null,
                restoreTick,
                endTick,
                0,
                new CharacterSimulationSnapshotComparison(false, new[] { reason }),
                PredictionInputFrameDifference.None,
                LocalRollbackSynctestFirstMismatch.None);
        }
    }

    public sealed class LocalLatencyReconciliationRunner
    {
        readonly List<PredictionInputFrame> replayInputs = new List<PredictionInputFrame>();
        readonly List<CharacterSimulationSnapshot> resolvedReplaySnapshots = new List<CharacterSimulationSnapshot>();
        readonly PredictionInputHistory localInputHistory;
        readonly LatencySimulator remoteSimulator;
        readonly PredictionSnapshotHistory snapshotHistory;
        readonly ILocalRollbackSynctestSimulation simulation;
        readonly RepeatLastFramePredictionStrategy predictionStrategy;

        public LocalLatencyReconciliationRunner(
            PredictionInputHistory localInputHistory,
            LatencySimulator remoteSimulator,
            PredictionSnapshotHistory snapshotHistory,
            ILocalRollbackSynctestSimulation simulation)
        {
            this.localInputHistory = localInputHistory;
            this.remoteSimulator = remoteSimulator;
            this.snapshotHistory = snapshotHistory;
            this.simulation = simulation;
            predictionStrategy = new RepeatLastFramePredictionStrategy();
        }

        public LocalLatencyReconciliationResult Run(
            SimulationTick confirmedTick,
            SimulationTick currentTick,
            in CharacterSimulationSnapshotTolerance tolerance)
        {
            // CheckSimulation: find first incorrect tick by comparing local snapshots
            // with remote-input replay snapshots tick by tick
            SimulationTick? firstIncorrectTick = null;
            PredictionInputFrameDifference predictionDifference = PredictionInputFrameDifference.None;

            for (SimulationTick tick = confirmedTick.Next; tick <= currentTick; tick = tick.Next)
            {
                if (!snapshotHistory.TryGet(tick, out CharacterSimulationSnapshot localSnapshot))
                    return LocalLatencyReconciliationResult.Fail(
                        $"missing local snapshot {tick.Value}",
                        confirmedTick,
                        currentTick);

                ReconciledInputFrame resolved = ReconciledInputResolver.Resolve(
                    tick, currentTick, remoteSimulator, predictionStrategy);
                PredictionInputFrame resolvedFrame = resolved.Frame;

                if (!snapshotHistory.TryGet(tick.Subtract(1), out CharacterSimulationSnapshot restoreSnap))
                    return LocalLatencyReconciliationResult.Fail(
                        $"missing snapshot for rollback {tick.Subtract(1).Value}",
                        tick.Subtract(1),
                        currentTick);

                simulation.Restore(in restoreSnap);
                simulation.Advance(resolvedFrame);
                CharacterSimulationSnapshot replaySnapshot = simulation.CaptureSnapshot(tick);

                CharacterSimulationSnapshotComparison comparison =
                    CharacterSimulationSnapshotComparer.Compare(in localSnapshot, in replaySnapshot, in tolerance);

                if (!comparison.Matches)
                {
                    firstIncorrectTick = tick;
                    if (localInputHistory.TryGet(tick, out PredictionInputFrame localInput))
                    {
                        predictionDifference = PredictionInputFrameDifference.Compare(
                            in localInput,
                            in resolvedFrame);
                    }
                    else
                    {
                        predictionDifference = new PredictionInputFrameDifference(
                            true,
                            default,
                            resolvedFrame,
                            new[] { "missingLocalInput" });
                    }
                    break;
                }
            }

            if (firstIncorrectTick == null)
            {
                CharacterSimulationSnapshot liveSnapshot = simulation.CaptureSnapshot(currentTick);
                simulation.Restore(in liveSnapshot);
                return LocalLatencyReconciliationResult.NoError(confirmedTick, currentTick);
            }

            // AdjustSimulation: rollback and replay from first incorrect tick
            SimulationTick restoreTick = firstIncorrectTick.Value.Subtract(1);
            if (!snapshotHistory.TryGet(restoreTick, out CharacterSimulationSnapshot rollbackSnapshot))
                return LocalLatencyReconciliationResult.Fail(
                    $"missing snapshot for rollback {restoreTick.Value}",
                    restoreTick,
                    currentTick);

            replayInputs.Clear();
            for (SimulationTick tick = firstIncorrectTick.Value; tick <= currentTick; tick = tick.Next)
            {
                ReconciledInputFrame resolved = ReconciledInputResolver.Resolve(
                    tick, currentTick, remoteSimulator, predictionStrategy);
                PredictionInputFrame resolvedFrame = resolved.Frame;
                replayInputs.Add(resolvedFrame);
            }

            LocalRollbackSynctestFirstMismatch replayFirstMismatch = ReplayResolvedInputsStrict(
                in rollbackSnapshot,
                in tolerance,
                out CharacterSimulationSnapshotComparison finalComparison);
            if (replayFirstMismatch.HasMismatch || !finalComparison.Matches)
            {
                return new LocalLatencyReconciliationResult(
                    false,
                    LocalLatencyReconciliationOutcome.ReplayNondeterminism,
                    firstIncorrectTick,
                    restoreTick,
                    currentTick,
                    replayInputs.Count,
                    finalComparison,
                    in predictionDifference,
                    in replayFirstMismatch);
            }

            return new LocalLatencyReconciliationResult(
                true,
                LocalLatencyReconciliationOutcome.PredictionCorrection,
                firstIncorrectTick,
                restoreTick,
                currentTick,
                replayInputs.Count,
                finalComparison,
                in predictionDifference,
                LocalRollbackSynctestFirstMismatch.None);
        }

        LocalRollbackSynctestFirstMismatch ReplayResolvedInputsStrict(
            in CharacterSimulationSnapshot rollbackSnapshot,
            in CharacterSimulationSnapshotTolerance tolerance,
            out CharacterSimulationSnapshotComparison finalComparison)
        {
            resolvedReplaySnapshots.Clear();
            simulation.Restore(in rollbackSnapshot);
            for (int i = 0; i < replayInputs.Count; i++)
            {
                simulation.Advance(replayInputs[i]);
                resolvedReplaySnapshots.Add(simulation.CaptureSnapshot(replayInputs[i].Tick));
            }

            simulation.Restore(in rollbackSnapshot);
            LocalRollbackSynctestFirstMismatch firstMismatch = LocalRollbackSynctestFirstMismatch.None;
            CharacterSimulationSnapshot actualSnapshot = rollbackSnapshot;
            for (int i = 0; i < replayInputs.Count; i++)
            {
                PredictionInputFrame input = replayInputs[i];
                simulation.Advance(input);
                actualSnapshot = simulation.CaptureSnapshot(input.Tick);
                CharacterSimulationSnapshot expectedSnapshot = resolvedReplaySnapshots[i];
                CharacterSimulationSnapshotComparison stepComparison =
                    CharacterSimulationSnapshotComparer.Compare(in expectedSnapshot, in actualSnapshot, in tolerance);
                if (!stepComparison.Matches && !firstMismatch.HasMismatch)
                {
                    firstMismatch = new LocalRollbackSynctestFirstMismatch(
                        LocalRollbackSynctestMismatchStage.Replay,
                        input.Tick,
                        true,
                        input,
                        expectedSnapshot,
                        actualSnapshot,
                        stepComparison);
                }
            }

            if (resolvedReplaySnapshots.Count > 0)
            {
                CharacterSimulationSnapshot expectedFinalSnapshot = resolvedReplaySnapshots[resolvedReplaySnapshots.Count - 1];
                finalComparison = CharacterSimulationSnapshotComparer.Compare(
                    in expectedFinalSnapshot,
                    in actualSnapshot,
                    in tolerance);
            }
            else
            {
                finalComparison = new CharacterSimulationSnapshotComparison(true, System.Array.Empty<string>());
            }

            return firstMismatch;
        }
    }
}
