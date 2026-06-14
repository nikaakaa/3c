using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonAnimation;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonSimulation.Tests
{
    public sealed class LocalLatencyReconciliationTests
    {
        [Test]
        public void LatencySimulatorZeroDelayArrivesImmediately()
        {
            LatencySimulator sim = new LatencySimulator(8);
            PredictionInputFrame frame = Input(1, Vector2.right);
            sim.Write(in frame, 0);

            Assert.True(sim.HasArrived(new SimulationTick(1), new SimulationTick(1)));
            Assert.True(sim.TryGet(new SimulationTick(1), new SimulationTick(1), out PredictionInputFrame retrieved));
            Assert.AreEqual(new SimulationTick(1), retrieved.Tick);
            Assert.AreEqual(Vector2.right, retrieved.Move);
        }

        [Test]
        public void LatencySimulatorDelayedFrameArrivesAfterDelay()
        {
            LatencySimulator sim = new LatencySimulator(8);
            PredictionInputFrame frame = Input(5, Vector2.up, true, true);
            sim.Write(in frame, 3);

            Assert.False(sim.HasArrived(new SimulationTick(5), new SimulationTick(6)));
            Assert.False(sim.HasArrived(new SimulationTick(5), new SimulationTick(7)));
            Assert.True(sim.HasArrived(new SimulationTick(5), new SimulationTick(8)));
            Assert.True(sim.TryGet(new SimulationTick(5), new SimulationTick(8), out PredictionInputFrame retrieved));
            Assert.AreEqual(new SimulationTick(5), retrieved.Tick);
            Assert.True(retrieved.RunHeld);
            Assert.True(retrieved.Dodge.Pressed);
        }

        [Test]
        public void LatencySimulatorMissingTickReturnsFalse()
        {
            LatencySimulator sim = new LatencySimulator(8);
            Assert.False(sim.TryGet(new SimulationTick(0), new SimulationTick(5), out _));
            Assert.False(sim.HasArrived(new SimulationTick(0), new SimulationTick(5)));
        }

        [Test]
        public void LatencySimulatorTrimsToCapacity()
        {
            LatencySimulator sim = new LatencySimulator(3);
            sim.Write(Input(1, Vector2.zero), 0);
            sim.Write(Input(2, Vector2.zero), 0);
            sim.Write(Input(3, Vector2.zero), 0);
            sim.Write(Input(4, Vector2.zero), 0);

            Assert.False(sim.TryGet(new SimulationTick(1), new SimulationTick(5), out _));
            Assert.AreEqual(3, sim.Count);
        }

        [Test]
        public void LatencySimulatorTrimConfirmedBefore()
        {
            LatencySimulator sim = new LatencySimulator(8);
            sim.Write(Input(1, Vector2.zero), 0);
            sim.Write(Input(2, Vector2.zero), 0);
            sim.Write(Input(3, Vector2.zero), 0);

            sim.TrimConfirmedBefore(new SimulationTick(2));
            Assert.False(sim.TryGet(new SimulationTick(1), new SimulationTick(5), out _));
            Assert.True(sim.TryGet(new SimulationTick(2), new SimulationTick(5), out _));
        }

        [Test]
        public void RepeatLastFrameStrategyCopiesMoveAndLook()
        {
            RepeatLastFramePredictionStrategy strategy = new RepeatLastFramePredictionStrategy();
            strategy.RecordFrame(Input(1, Vector2.right, false));

            Assert.True(strategy.TryPredict(new SimulationTick(2), out PredictionInputFrame predicted));
            Assert.AreEqual(new SimulationTick(2), predicted.Tick);
            Assert.AreEqual(Vector2.right, predicted.Move);
            Assert.AreEqual(Vector2.zero, predicted.Look);
            Assert.False(predicted.RunHeld);
        }

        [Test]
        public void RepeatLastFrameStrategyHeldPersistsPressedReleasedDoNot()
        {
            RepeatLastFramePredictionStrategy strategy = new RepeatLastFramePredictionStrategy();
            PredictionInputFrame original = new PredictionInputFrame(
                new SimulationTick(1),
                Vector2.zero,
                Vector2.zero,
                true,
                new PredictionButtonFrame(true, true, false),
                new PredictionButtonFrame(false, true, false),
                new PredictionButtonFrame(false, false, true),
                PredictionButtonFrame.None);
            strategy.RecordFrame(in original);

            Assert.True(strategy.TryPredict(new SimulationTick(2), out PredictionInputFrame predicted));
            // pressed: no longer pressed in prediction
            Assert.False(predicted.Dodge.Pressed);
            // held: still held
            Assert.True(predicted.Dodge.Held);
            Assert.True(predicted.Attack.Held);
            // released: not repeated
            Assert.False(predicted.Jump.Released);
            Assert.False(predicted.Jump.Pressed);
            // RunHeld: kept
            Assert.True(predicted.RunHeld);
        }

        [Test]
        public void RepeatLastFrameStrategyFailsWithoutPriorFrame()
        {
            RepeatLastFramePredictionStrategy strategy = new RepeatLastFramePredictionStrategy();
            Assert.False(strategy.TryPredict(new SimulationTick(0), out _));
        }

        [Test]
        public void PredictionStrategyDoesNotWriteToRealHistory()
        {
            PredictionInputHistory history = new PredictionInputHistory(8);
            RepeatLastFramePredictionStrategy strategy = new RepeatLastFramePredictionStrategy();
            strategy.RecordFrame(Input(1, Vector2.right));
            strategy.TryPredict(new SimulationTick(2), out _);

            Assert.False(history.TryGet(new SimulationTick(2), out _));
        }

        [Test]
        public void ReconciledInputResolverPrefersRealOverPrediction()
        {
            LatencySimulator remote = new LatencySimulator(8);
            RepeatLastFramePredictionStrategy strategy = new RepeatLastFramePredictionStrategy();

            PredictionInputFrame real = Input(2, Vector2.up, true);
            remote.Write(in real, 0);
            strategy.RecordFrame(Input(1, Vector2.right));

            ReconciledInputFrame resolved = ReconciledInputResolver.Resolve(
                new SimulationTick(2), new SimulationTick(10), remote, strategy);

            Assert.False(resolved.IsPrediction);
            Assert.AreEqual(Vector2.up, resolved.Frame.Move);
            Assert.True(resolved.Frame.RunHeld);
        }

        [Test]
        public void ReconciledInputResolverFallsBackToPrediction()
        {
            LatencySimulator remote = new LatencySimulator(8);
            RepeatLastFramePredictionStrategy strategy = new RepeatLastFramePredictionStrategy();
            strategy.RecordFrame(Input(1, Vector2.right));

            ReconciledInputFrame resolved = ReconciledInputResolver.Resolve(
                new SimulationTick(5), new SimulationTick(5), remote, strategy);

            Assert.True(resolved.IsPrediction);
            Assert.AreEqual(Vector2.right, resolved.Frame.Move);
        }

        [Test]
        public void ReconciliationNoRollbackWhenInputsMatch()
        {
            LatencySimulator remote = new LatencySimulator(8);
            PredictionInputHistory localInputs = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshots = new PredictionSnapshotHistory(8);
            FakeRollbackSimulation simulation = new FakeRollbackSimulation();

            // Simulate 4 ticks with zero-delay remote input (both sides identical)
            snapshots.Write(simulation.CaptureSnapshot(SimulationTick.Zero));
            for (int tick = 1; tick <= 4; tick++)
            {
                PredictionInputFrame frame = Input(tick, Vector2.right);
                localInputs.Write(frame);
                remote.Write(in frame, 0);
                simulation.Advance(frame);
                CharacterSimulationSnapshot snapshot = simulation.CaptureSnapshot(new SimulationTick(tick));
                snapshots.Write(in snapshot);
            }

            LocalLatencyReconciliationRunner runner = new LocalLatencyReconciliationRunner(
                localInputs, remote, snapshots, simulation);

            LocalLatencyReconciliationResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(4),
                CharacterSimulationSnapshotTolerance.Default);

            Assert.True(result.Success);
            Assert.False(result.FirstIncorrectTick.HasValue);
            Assert.AreEqual(0, result.ReplayFrameCount);
        }

        [Test]
        public void ReconciliationDetectsFirstIncorrectTickOnDifferentInput()
        {
            LatencySimulator remote = new LatencySimulator(8);
            PredictionInputHistory localInputs = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshots = new PredictionSnapshotHistory(8);
            FakeRollbackSimulation simulation = new FakeRollbackSimulation();

            // Run locally with one input sequence
            snapshots.Write(simulation.CaptureSnapshot(SimulationTick.Zero));
            for (int tick = 1; tick <= 4; tick++)
            {
                PredictionInputFrame frame = Input(tick, Vector2.right);
                localInputs.Write(frame);
                simulation.Advance(frame);
                CharacterSimulationSnapshot snapshot = simulation.CaptureSnapshot(new SimulationTick(tick));
                snapshots.Write(in snapshot);
            }

            // Remote has different input at tick 3
            for (int tick = 1; tick <= 4; tick++)
            {
                Vector2 move = tick == 3 ? Vector2.left : Vector2.right;
                remote.Write(Input(tick, move), 0);
            }

            LocalLatencyReconciliationRunner runner = new LocalLatencyReconciliationRunner(
                localInputs, remote, snapshots, simulation);

            LocalLatencyReconciliationResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(4),
                CharacterSimulationSnapshotTolerance.Default);

            Assert.True(result.Success);
            Assert.AreEqual(LocalLatencyReconciliationOutcome.PredictionCorrection, result.Outcome);
            Assert.True(result.FirstIncorrectTick.HasValue);
            Assert.AreEqual(3, result.FirstIncorrectTick.Value.Value);
            Assert.True(result.PredictionDifference.HasDifference);
            CollectionAssert.Contains(result.PredictionDifference.Differences.ToArray(), "move");
        }

        [Test]
        public void ReconciliationRollbackReplaysFromFirstIncorrectTick()
        {
            LatencySimulator remote = new LatencySimulator(8);
            PredictionInputHistory localInputs = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshots = new PredictionSnapshotHistory(8);
            FakeRollbackSimulation simulation = new FakeRollbackSimulation();

            snapshots.Write(simulation.CaptureSnapshot(SimulationTick.Zero));
            for (int tick = 1; tick <= 4; tick++)
            {
                PredictionInputFrame frame = Input(tick, Vector2.right);
                localInputs.Write(frame);
                simulation.Advance(frame);
                CharacterSimulationSnapshot snapshot = simulation.CaptureSnapshot(new SimulationTick(tick));
                snapshots.Write(in snapshot);
            }

            // Remote has different input at tick 3 only, correct on others
            for (int tick = 1; tick <= 4; tick++)
            {
                Vector2 move = tick == 3 ? Vector2.left : Vector2.right;
                remote.Write(Input(tick, move), 0);
            }

            LocalLatencyReconciliationRunner runner = new LocalLatencyReconciliationRunner(
                localInputs, remote, snapshots, simulation);

            LocalLatencyReconciliationResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(4),
                CharacterSimulationSnapshotTolerance.Default);

            Assert.True(result.Success);
            Assert.AreEqual(LocalLatencyReconciliationOutcome.PredictionCorrection, result.Outcome);
            Assert.AreEqual(3, result.FirstIncorrectTick.Value.Value);
            Assert.AreEqual(2, result.RestoreTick.Value);
            Assert.AreEqual(2, result.ReplayFrameCount);
            Assert.False(result.ReplayFirstMismatch.HasMismatch);
        }

        [Test]
        public void ReconciliationFailsWhenRestoreSnapshotMissing()
        {
            LatencySimulator remote = new LatencySimulator(8);
            PredictionInputHistory localInputs = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshots = new PredictionSnapshotHistory(8);
            FakeRollbackSimulation simulation = new FakeRollbackSimulation();

            // Only write snapshot at tick 0, not at tick 1
            snapshots.Write(simulation.CaptureSnapshot(SimulationTick.Zero));
            for (int tick = 1; tick <= 2; tick++)
            {
                PredictionInputFrame frame = Input(tick, Vector2.right);
                localInputs.Write(frame);
                remote.Write(Input(tick, Vector2.left), 0);
                simulation.Advance(frame);
            }

            LocalLatencyReconciliationRunner runner = new LocalLatencyReconciliationRunner(
                localInputs, remote, snapshots, simulation);

            LocalLatencyReconciliationResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(2),
                CharacterSimulationSnapshotTolerance.Default);

            Assert.False(result.Success);
        }

        [Test]
        public void ReconciliationDoesNotModifyInputHistory()
        {
            LatencySimulator remote = new LatencySimulator(8);
            PredictionInputHistory localInputs = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshots = new PredictionSnapshotHistory(8);
            FakeRollbackSimulation simulation = new FakeRollbackSimulation();

            snapshots.Write(simulation.CaptureSnapshot(SimulationTick.Zero));
            for (int tick = 1; tick <= 2; tick++)
            {
                PredictionInputFrame frame = Input(tick, Vector2.right);
                localInputs.Write(frame);
                simulation.Advance(frame);
                CharacterSimulationSnapshot snapshot = simulation.CaptureSnapshot(new SimulationTick(tick));
                snapshots.Write(in snapshot);
            }

            remote.Write(Input(1, Vector2.right), 0);
            remote.Write(Input(2, Vector2.left), 0);

            int countBefore = localInputs.Count;

            LocalLatencyReconciliationRunner runner = new LocalLatencyReconciliationRunner(
                localInputs, remote, snapshots, simulation);
            runner.Run(SimulationTick.Zero, new SimulationTick(2), CharacterSimulationSnapshotTolerance.Default);

            Assert.AreEqual(countBefore, localInputs.Count);
            Assert.True(localInputs.TryGet(new SimulationTick(1), out PredictionInputFrame f1));
            Assert.AreEqual(Vector2.right, f1.Move);
        }

        [Test]
        public void ReconciliationPredictionFallbackAndConvergence()
        {
            LatencySimulator remote = new LatencySimulator(8);
            PredictionInputHistory localInputs = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshots = new PredictionSnapshotHistory(8);
            FakeRollbackSimulation simulation = new FakeRollbackSimulation();

            snapshots.Write(simulation.CaptureSnapshot(SimulationTick.Zero));
            for (int tick = 1; tick <= 4; tick++)
            {
                PredictionInputFrame frame = Input(tick, Vector2.right * 0.25f * tick);
                localInputs.Write(frame);
                simulation.Advance(frame);
                CharacterSimulationSnapshot snapshot = simulation.CaptureSnapshot(new SimulationTick(tick));
                snapshots.Write(in snapshot);
            }

            // Remote: only write tick 1 real, let 2-4 be predicted
            remote.Write(Input(1, Vector2.right * 0.25f), 0);

            // Tick 2-4 only have tick 1 as seed, so predicted = tick 1 repeat (Vector2.right * 0.25f)
            // But local snapshots at tick 2-4 have moves = Vector2.right * 0.5f, * 0.75f, * 1.0f
            // So they WILL diverge — first incorrect at tick 2

            LocalLatencyReconciliationRunner runner = new LocalLatencyReconciliationRunner(
                localInputs, remote, snapshots, simulation);

            LocalLatencyReconciliationResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(4),
                CharacterSimulationSnapshotTolerance.Default);

            Assert.True(result.Success);
            Assert.AreEqual(LocalLatencyReconciliationOutcome.PredictionCorrection, result.Outcome);
            Assert.True(result.FirstIncorrectTick.HasValue);
            Assert.AreEqual(2, result.FirstIncorrectTick.Value.Value);
        }

        [Test]
        public void ReconciliationReportsReplayNondeterminismSeparatelyFromPredictionDifference()
        {
            LatencySimulator remote = new LatencySimulator(8);
            PredictionInputHistory localInputs = new PredictionInputHistory(8);
            PredictionSnapshotHistory snapshots = new PredictionSnapshotHistory(8);
            NondeterministicReplaySimulation simulation = new NondeterministicReplaySimulation();

            snapshots.Write(simulation.CaptureSnapshot(SimulationTick.Zero));
            for (int tick = 1; tick <= 3; tick++)
            {
                PredictionInputFrame frame = Input(tick, Vector2.right);
                localInputs.Write(frame);
                simulation.Advance(frame);
                CharacterSimulationSnapshot snapshot = simulation.CaptureSnapshot(new SimulationTick(tick));
                snapshots.Write(in snapshot);
            }

            remote.Write(Input(1, Vector2.right), 0);
            remote.Write(Input(2, Vector2.left), 0);
            remote.Write(Input(3, Vector2.right), 0);

            LocalLatencyReconciliationRunner runner = new LocalLatencyReconciliationRunner(
                localInputs, remote, snapshots, simulation);

            LocalLatencyReconciliationResult result = runner.Run(
                SimulationTick.Zero,
                new SimulationTick(3),
                CharacterSimulationSnapshotTolerance.Default);

            Assert.False(result.Success);
            Assert.AreEqual(LocalLatencyReconciliationOutcome.ReplayNondeterminism, result.Outcome);
            Assert.True(result.PredictionDifference.HasDifference);
            Assert.True(result.ReplayFirstMismatch.HasMismatch);
            Assert.AreEqual(LocalRollbackSynctestMismatchStage.Replay, result.ReplayFirstMismatch.Stage);
        }

        [Test]
        public void LatencyRecoveryCoreDoesNotReferenceForbiddenTypes()
        {
            string root = Path.Combine(Application.dataPath, "Scripts/Simulation/Rollback");
            string combined = string.Join("\n", Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

            Assert.That(combined, Does.Not.Contain("Fantasy"));
            Assert.That(combined, Does.Not.Contain(".proto"));
            Assert.That(combined, Does.Not.Contain("Animancer"));
            Assert.That(combined, Does.Not.Contain("Cinemachine"));
            Assert.That(combined, Does.Not.Contain("UnityEngine.InputSystem"));
            Assert.That(combined, Does.Not.Contain("CharacterController"));
            Assert.That(combined, Does.Not.Contain("BasicLocomotionPipeline"));
        }

        static PredictionInputFrame Input(int tick, Vector2 move, bool runHeld = false, bool dodgePressed = false)
        {
            return new PredictionInputFrame(
                new SimulationTick(tick),
                move,
                Vector2.zero,
                runHeld,
                dodgePressed ? new PredictionButtonFrame(true, true, false) : PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None,
                PredictionButtonFrame.None);
        }

        sealed class FakeRollbackSimulation : ILocalRollbackSynctestSimulation
        {
            Vector3 position;

            public CharacterSimulationSnapshot CaptureSnapshot(SimulationTick tick)
            {
                return new CharacterSimulationSnapshot(
                    tick,
                    position,
                    0f,
                    new CharacterStateMachineSnapshot(
                        CharacterStateIds.Idle,
                        0f,
                        CharacterStateVariant.None,
                        string.Empty,
                        Array.Empty<CharacterStateTag>()),
                    false,
                    BasicMovementGait.Walk,
                    Vector3.forward,
                    BasicMovementPhase.Idle,
                    BasicMovementGait.Walk,
                    string.Empty,
                    0f);
            }

            public void Restore(in CharacterSimulationSnapshot snapshot)
            {
                position = snapshot.Position;
            }

            public void Advance(in PredictionInputFrame input)
            {
                position += new Vector3(input.Move.x, 0f, input.Move.y);
            }
        }

        sealed class NondeterministicReplaySimulation : ILocalRollbackSynctestSimulation
        {
            Vector3 position;
            int restoreCount;

            public CharacterSimulationSnapshot CaptureSnapshot(SimulationTick tick)
            {
                return new CharacterSimulationSnapshot(
                    tick,
                    position,
                    0f,
                    new CharacterStateMachineSnapshot(
                        CharacterStateIds.Idle,
                        0f,
                        CharacterStateVariant.None,
                        string.Empty,
                        Array.Empty<CharacterStateTag>()),
                    false,
                    BasicMovementGait.Walk,
                    Vector3.forward,
                    BasicMovementPhase.Idle,
                    BasicMovementGait.Walk,
                    string.Empty,
                    0f);
            }

            public void Restore(in CharacterSimulationSnapshot snapshot)
            {
                position = snapshot.Position;
                restoreCount++;
            }

            public void Advance(in PredictionInputFrame input)
            {
                Vector3 delta = new Vector3(input.Move.x, 0f, input.Move.y);
                if (restoreCount >= 4)
                    delta += Vector3.forward * 0.5f;

                position += delta;
            }
        }
    }
}
