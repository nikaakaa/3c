using System.Text;
using ThirdPersonPresentation;
using UnityEngine;
using ThirdPersonDiagnostics;

namespace ThirdPersonSimulation
{
    [DisallowMultipleComponent]
    public sealed class LocalLatencyReconciliationDebugRunner : MonoBehaviour
    {
        [SerializeField] PredictionInputHistoryTickRecorder inputRecorder;
        [SerializeField] LocomotionSnapshotHistoryRecorder snapshotRecorder;
        [SerializeField] MonoBehaviour simulationBehaviour;
        [SerializeField, Min(0)] int latencyTicks = 3;
        [SerializeField, Min(1)] int rollbackCapacity = 16;
        [SerializeField] KeyCode triggerKey = KeyCode.F7;
        [SerializeField] bool runOnKeyDown = true;
        [SerializeField] bool logSuccess = true;
        [SerializeField] bool applyReplayResultToScene;
        [SerializeField] PresentationTransformInterpolator presentationInterpolator;
        [SerializeField, Min(0f)] float visualCorrectionSeconds = 0.12f;

        ILocalRollbackSynctestSimulation simulation;
        LatencySimulator remoteSimulator;
        RepeatLastFramePredictionStrategy predictionStrategy;

        public PredictionInputHistoryTickRecorder InputRecorder { get => inputRecorder; set => inputRecorder = value; }
        public LocomotionSnapshotHistoryRecorder SnapshotRecorder { get => snapshotRecorder; set => snapshotRecorder = value; }
        public MonoBehaviour SimulationBehaviour { get => simulationBehaviour; set { simulationBehaviour = value; simulation = value as ILocalRollbackSynctestSimulation; } }
        public ILocalRollbackSynctestSimulation Simulation => simulation;
        public int LatencyTicks { get => latencyTicks; set => latencyTicks = Mathf.Max(0, value); }
        public int RollbackCapacity { get => rollbackCapacity; set => rollbackCapacity = Mathf.Max(1, value); }
        public KeyCode TriggerKey { get => triggerKey; set => triggerKey = value; }
        public bool RunOnKeyDown { get => runOnKeyDown; set => runOnKeyDown = value; }
        public bool ApplyReplayResultToScene { get => applyReplayResultToScene; set => applyReplayResultToScene = value; }
        public PresentationTransformInterpolator PresentationInterpolator { get => presentationInterpolator; set => presentationInterpolator = value; }
        public float VisualCorrectionSeconds { get => visualCorrectionSeconds; set => visualCorrectionSeconds = Mathf.Max(0f, value); }
        public LocalLatencyReconciliationResult LastResult { get; private set; }
        public bool HasResult { get; private set; }

        void Reset()
        {
            ResolveReferences();
        }

        void Awake()
        {
            ResolveReferences();
        }

        void Update()
        {
            if (runOnKeyDown && Input.GetKeyDown(triggerKey))
                RunReconciliation();
        }

        public bool RunReconciliation()
        {
            ResolveReferences();

            if (inputRecorder == null || snapshotRecorder == null || simulation == null)
                return Fail("missing recorder or simulation");
            if (!snapshotRecorder.History.TryGetLatestRecoverableTick(out SimulationTick currentTick))
                return Fail("missing latest snapshot");
            if (currentTick.Value <= latencyTicks)
                return Fail($"not enough history current={currentTick.Value} latency={latencyTicks}");

            EnsureRemoteSimulator();
            predictionStrategy = predictionStrategy ?? new RepeatLastFramePredictionStrategy();

            SimulationTick confirmedTick = SimulationTick.Zero;
            SimulationTick earliest = currentTick.Subtract(rollbackCapacity);
            if (earliest.Value > 0)
                confirmedTick = earliest;
            CharacterSimulationSnapshot liveSnapshot = simulation.CaptureSnapshot(currentTick);
            PresentationDebugRestoreCapture presentationCapture = PresentationDebugRestoreGuard.Capture(presentationInterpolator);

            LocalLatencyReconciliationRunner runner = new LocalLatencyReconciliationRunner(
                inputRecorder.History,
                remoteSimulator,
                snapshotRecorder.History,
                simulation);

            LastResult = runner.Run(confirmedTick, currentTick, CharacterSimulationSnapshotTolerance.Default);

            if (!applyReplayResultToScene)
            {
                simulation.Restore(in liveSnapshot);
                CompleteDebugRestore();
                PresentationDebugRestoreGuard.Restore(
                    presentationInterpolator,
                    applyReplayResultToScene,
                    visualCorrectionSeconds,
                    in presentationCapture);
            }

            HasResult = true;
            LogResult(LastResult);
            return LastResult.Success;
        }

        void EnsureRemoteSimulator()
        {
            int capacity = Mathf.Max(1, rollbackCapacity);
            if (remoteSimulator == null || remoteSimulator.Capacity != capacity)
                remoteSimulator = new LatencySimulator(capacity);

            PopulateRemoteFromLocal();
        }

        void PopulateRemoteFromLocal()
        {
            if (inputRecorder == null || snapshotRecorder == null)
                return;

            if (!snapshotRecorder.History.TryGetLatestRecoverableTick(out SimulationTick latestTick))
                return;

            for (SimulationTick tick = SimulationTick.Zero; tick <= latestTick; tick = tick.Next)
            {
                if (remoteSimulator.HasArrived(tick, latestTick))
                    continue;

                if (inputRecorder.History.TryGet(tick, out PredictionInputFrame frame))
                    remoteSimulator.Write(in frame, latencyTicks);
            }
        }

        bool Fail(string reason)
        {
            LastResult = LocalLatencyReconciliationResult.Fail(reason, SimulationTick.Zero, SimulationTick.Zero);
            HasResult = true;
             RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                 RuntimeDiagnosticLogCategory.Simulation,
                 RuntimeDiagnosticLogLevel.Warning,
                 "reconciliation-fail",
                 "",
                 "",
                 0,
                 Time.frameCount,
                 $"[reconciliation] FAIL {reason}"));
            return false;
        }

        void LogResult(in LocalLatencyReconciliationResult result)
        {
            if (result.Success && !result.FirstIncorrectTick.HasValue)
            {
                if (logSuccess)
                     RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                         RuntimeDiagnosticLogCategory.Simulation,
                         RuntimeDiagnosticLogLevel.Info,
                         "reconciliation-pass-norollback",
                         "",
                         "",
                         0,
                         Time.frameCount,
                         $"[reconciliation] PASS outcome={result.Outcome} end={result.EndTick.Value}"));
                return;
            }

            if (result.Success)
            {
                if (logSuccess)
                {
                     RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                         RuntimeDiagnosticLogCategory.Simulation,
                         RuntimeDiagnosticLogLevel.Info,
                         "reconciliation-pass-withrollback",
                         "",
                         "",
                         0,
                         Time.frameCount,
                         $"[reconciliation] PASS outcome={result.Outcome} firstIncorrect={result.FirstIncorrectTick?.Value} " +
                         $"restore={result.RestoreTick.Value} frames={result.ReplayFrameCount} end={result.EndTick.Value}" +
                         FormatPredictionDifference(in result)));
                }

                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("[reconciliation] FAIL");
            builder.Append(" outcome=").Append(result.Outcome);
            builder.Append(" firstIncorrect=").Append(result.FirstIncorrectTick?.Value ?? -1);
            builder.Append(" restore=").Append(result.RestoreTick.Value);
            builder.Append(" frames=").Append(result.ReplayFrameCount);
            builder.Append(" end=").Append(result.EndTick.Value);
            builder.Append(FormatPredictionDifference(in result));
            if (result.ReplayFirstMismatch.HasMismatch)
            {
                builder.Append(" replayStage=").Append(result.ReplayFirstMismatch.Stage);
                builder.Append(" replayTick=").Append(result.ReplayFirstMismatch.Tick.Value);
                if (result.ReplayFirstMismatch.Comparison.Differences.Count > 0)
                    builder.Append(" replayDifferences=").Append(string.Join(",", result.ReplayFirstMismatch.Comparison.Differences));
            }
            if (result.Comparison.Differences.Count > 0)
                builder.Append(" differences=").Append(string.Join(",", result.Comparison.Differences));

             RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                 RuntimeDiagnosticLogCategory.Simulation,
                 RuntimeDiagnosticLogLevel.Warning,
                 "reconciliation-fail-detail",
                 "",
                 "",
                 0,
                 Time.frameCount,
                 builder.ToString()));
        }

        static string FormatPredictionDifference(in LocalLatencyReconciliationResult result)
        {
            PredictionInputFrameDifference difference = result.PredictionDifference;
            if (!difference.HasDifference)
                return string.Empty;

            StringBuilder builder = new StringBuilder();
            builder.Append(" predictionDiff=").Append(string.Join(",", difference.Differences));
            builder.Append(" predictedMove=").Append(LocalRollbackSynctestLogFormatter.Format(difference.PredictedInput.Move));
            builder.Append(" resolvedMove=").Append(LocalRollbackSynctestLogFormatter.Format(difference.ResolvedInput.Move));
            builder.Append(" predictedRun=").Append(difference.PredictedInput.RunHeld);
            builder.Append(" resolvedRun=").Append(difference.ResolvedInput.RunHeld);
            return builder.ToString();
        }

        void CompleteDebugRestore()
        {
            if (simulation is ILocalRollbackDebugRestoreCleanup cleanup)
                cleanup.CompleteDebugRestore();
        }

        void ResolveReferences()
        {
            if (inputRecorder == null)
                inputRecorder = GetComponent<PredictionInputHistoryTickRecorder>();
            if (inputRecorder == null)
                inputRecorder = GetComponentInParent<PredictionInputHistoryTickRecorder>();
            if (inputRecorder == null)
                inputRecorder = GetComponentInChildren<PredictionInputHistoryTickRecorder>(true);

            if (snapshotRecorder == null)
                snapshotRecorder = GetComponent<LocomotionSnapshotHistoryRecorder>();
            if (snapshotRecorder == null)
                snapshotRecorder = GetComponentInParent<LocomotionSnapshotHistoryRecorder>();
            if (snapshotRecorder == null)
                snapshotRecorder = GetComponentInChildren<LocomotionSnapshotHistoryRecorder>(true);

            if (simulation == null)
                ResolveSimulationFromBehaviour();
            if (simulation == null)
                ResolveSimulationFromComponents(GetComponents<MonoBehaviour>());
            if (simulation == null)
                ResolveSimulationFromComponents(GetComponentsInParent<MonoBehaviour>(true));
            if (simulation == null)
                ResolveSimulationFromComponents(GetComponentsInChildren<MonoBehaviour>(true));

            if (presentationInterpolator == null)
                presentationInterpolator = GetComponent<PresentationTransformInterpolator>();
            if (presentationInterpolator == null)
                presentationInterpolator = GetComponentInParent<PresentationTransformInterpolator>();
            if (presentationInterpolator == null)
                presentationInterpolator = GetComponentInChildren<PresentationTransformInterpolator>(true);
        }

        void ResolveSimulationFromBehaviour()
        {
            if (simulationBehaviour is ILocalRollbackSynctestSimulation resolved)
            {
                simulation = resolved;
                return;
            }

            simulation = null;
        }

        void ResolveSimulationFromComponents(MonoBehaviour[] behaviours)
        {
            if (behaviours == null)
                return;

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (!(behaviours[i] is ILocalRollbackSynctestSimulation candidate))
                    continue;

                simulationBehaviour = behaviours[i];
                simulation = candidate;
                return;
            }
        }
    }
}
