using System.Text;
using ThirdPersonCamera;
using ThirdPersonPresentation;
using UnityEngine;
using ThirdPersonDiagnostics;

namespace ThirdPersonSimulation
{
    [DisallowMultipleComponent]
    public sealed class LocalRollbackSynctestDebugRunner : MonoBehaviour
    {
        [SerializeField] PredictionInputHistoryTickRecorder inputRecorder;
        [SerializeField] LocomotionSnapshotHistoryRecorder snapshotRecorder;
        [SerializeField] MonoBehaviour simulationBehaviour;
        [SerializeField, Min(1)] int rollbackFrames = 8;
        [SerializeField] KeyCode triggerKey = KeyCode.F6;
        [SerializeField] bool runOnKeyDown = true;
        [SerializeField] bool logSuccess = true;
        [SerializeField] bool applyReplayResultToScene;
        [SerializeField] PresentationTransformInterpolator presentationInterpolator;
        [SerializeField] ThirdPersonCameraController cameraController;
        [SerializeField, Min(0f)] float visualCorrectionSeconds = 0.12f;

        ILocalRollbackSynctestSimulation simulation;

        public PredictionInputHistoryTickRecorder InputRecorder { get => inputRecorder; set => inputRecorder = value; }
        public LocomotionSnapshotHistoryRecorder SnapshotRecorder { get => snapshotRecorder; set => snapshotRecorder = value; }
        public MonoBehaviour SimulationBehaviour { get => simulationBehaviour; set { simulationBehaviour = value; simulation = value as ILocalRollbackSynctestSimulation; } }
        public ILocalRollbackSynctestSimulation Simulation => simulation;
        public int RollbackFrames { get => rollbackFrames; set => rollbackFrames = Mathf.Max(1, value); }
        public KeyCode TriggerKey { get => triggerKey; set => triggerKey = value; }
        public bool RunOnKeyDown { get => runOnKeyDown; set => runOnKeyDown = value; }
        public bool ApplyReplayResultToScene { get => applyReplayResultToScene; set => applyReplayResultToScene = value; }
        public PresentationTransformInterpolator PresentationInterpolator { get => presentationInterpolator; set => presentationInterpolator = value; }
        public ThirdPersonCameraController CameraController { get => cameraController; set => cameraController = value; }
        public float VisualCorrectionSeconds { get => visualCorrectionSeconds; set => visualCorrectionSeconds = Mathf.Max(0f, value); }
        public LocalRollbackSynctestResult LastResult { get; private set; }
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
                RunDebugSynctest();
        }

        public bool RunDebugSynctest()
        {
            ResolveReferences();

            if (inputRecorder == null || snapshotRecorder == null || simulation == null)
                return Fail("missing recorder or simulation");
            if (!snapshotRecorder.History.TryGetLatestRecoverableTick(out SimulationTick endTick))
                return Fail("missing latest snapshot");
            if (endTick.Value <= rollbackFrames)
                return Fail($"not enough history end={endTick.Value} rollbackFrames={rollbackFrames}");

            SimulationTick startTick = SimulationTick.Zero;
            SimulationTick restoreTick = endTick.Subtract(rollbackFrames);
            if (!snapshotRecorder.History.TryGet(endTick, out CharacterSimulationSnapshot liveSnapshot))
                return Fail($"missing snapshot {endTick.Value}");
            CharacterSimulationSnapshot replaySnapshot = liveSnapshot;
            PresentationDebugRestoreCapture presentationCapture = PresentationDebugRestoreGuard.Capture(presentationInterpolator);
            bool hasTimingProbeStart = TryCaptureTimingProbePose(out RollbackTimingProbePose timingProbeStart);
            RollbackTimingProbePose timingProbeAfterReplay = default;
            RollbackTimingProbePose timingProbeFinal = default;
            LocalRollbackSynctestRunner runner = new LocalRollbackSynctestRunner(
                inputRecorder.History,
                snapshotRecorder.History,
                simulation);

            try
            {
                LastResult = runner.Run(startTick, endTick, restoreTick, CharacterSimulationSnapshotTolerance.Default);
                replaySnapshot = simulation.CaptureSnapshot(endTick);
                TryCaptureTimingProbePose(out timingProbeAfterReplay);
            }
            finally
            {
                CharacterSimulationSnapshot finalSnapshot = applyReplayResultToScene ? replaySnapshot : liveSnapshot;
                simulation.Restore(in finalSnapshot);
                CompleteDebugRestore();
                PresentationDebugRestoreGuard.Restore(
                    presentationInterpolator,
                    applyReplayResultToScene,
                    visualCorrectionSeconds,
                    in presentationCapture);
                TryCaptureTimingProbePose(out timingProbeFinal);
            }

            HasResult = true;
            LogResult(LastResult);
            LogFirstMismatch(LastResult);
            LogTimingProbe(
                LastResult,
                presentationCapture.HasVisualStartPose,
                presentationCapture.HasRestoreState,
                hasTimingProbeStart,
                timingProbeStart,
                timingProbeAfterReplay,
                timingProbeFinal);
            return LastResult.Success;
        }

        bool Fail(string reason)
        {
            LastResult = new LocalRollbackSynctestResult(
                false,
                SimulationTick.Zero,
                SimulationTick.Zero,
                SimulationTick.Zero,
                reason,
                new CharacterSimulationSnapshotComparison(false, new[] { reason }));
            HasResult = true;
             RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                 RuntimeDiagnosticLogCategory.Simulation,
                 RuntimeDiagnosticLogLevel.Warning,
                 "synctest-fail",
                 "",
                 "",
                 0,
                 Time.frameCount,
                 $"[rollback-synctest] FAIL {reason}"));
            return false;
        }

        void LogResult(in LocalRollbackSynctestResult result)
        {
            if (result.Success)
            {
                if (logSuccess)
                     RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                         RuntimeDiagnosticLogCategory.Simulation,
                         RuntimeDiagnosticLogLevel.Info,
                         "synctest-pass",
                         "",
                         "",
                         0,
                         Time.frameCount,
                         LocalRollbackSynctestLogFormatter.FormatPass(in result)));
                return;
            }

             RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                 RuntimeDiagnosticLogCategory.Simulation,
                 RuntimeDiagnosticLogLevel.Warning,
                 "synctest-fail-detail",
                 "",
                 "",
                 0,
                 Time.frameCount,
                 LocalRollbackSynctestLogFormatter.FormatFail(in result)));
        }

        void LogFirstMismatch(in LocalRollbackSynctestResult result)
        {
            if (!result.FirstMismatch.HasAnyDifference)
                return;

             RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                 RuntimeDiagnosticLogCategory.Simulation,
                 result.FirstMismatch.HasMismatch ? RuntimeDiagnosticLogLevel.Warning : RuntimeDiagnosticLogLevel.Info,
                 result.FirstMismatch.HasMismatch ? "synctest-first-mismatch" : "synctest-first-presentation-drift",
                 "",
                 "",
                 result.FirstMismatch.Tick.Value,
                 Time.frameCount,
                 LocalRollbackSynctestLogFormatter.FormatFirstMismatch(in result)));
        }

        bool TryCaptureTimingProbePose(out RollbackTimingProbePose pose)
        {
            if (presentationInterpolator == null)
                ResolveReferences();

            return RollbackTimingProbe.TryCapture(
                presentationInterpolator,
                ResolveCameraForProbe(),
                transform,
                out pose);
        }

        void LogTimingProbe(
            in LocalRollbackSynctestResult result,
            bool hasVisualStartPose,
            bool hasPresentationState,
            bool hasTimingProbeStart,
            in RollbackTimingProbePose startPose,
            in RollbackTimingProbePose replayPose,
            in RollbackTimingProbePose finalPose)
        {
             RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                 RuntimeDiagnosticLogCategory.Simulation,
                 RuntimeDiagnosticLogLevel.Warning,
                 "rollback-timing-probe",
                 "",
                 "",
                 result.EndTick.Value,
                 Time.frameCount,
                 RollbackTimingProbe.Format(
                     in result,
                     applyReplayResultToScene,
                     hasVisualStartPose,
                     hasPresentationState,
                     hasTimingProbeStart,
                     in startPose,
                     in replayPose,
                     in finalPose),
                 "Simulation.rollback-timing-probe"));
        }

        void CompleteDebugRestore()
        {
            if (simulation is ILocalRollbackDebugRestoreCleanup cleanup)
                cleanup.CompleteDebugRestore();
        }

        ThirdPersonCameraController ResolveCameraForProbe()
        {
            if (cameraController == null)
                ResolveCameraFromSimulation();
            if (cameraController == null)
                cameraController = GetComponent<ThirdPersonCameraController>();

            return cameraController;
        }

        void ResolveCameraFromSimulation()
        {
            if (simulation is FullBodyRollbackSimulation fullBodySimulation &&
                fullBodySimulation.RuntimeController != null)
            {
                cameraController = fullBodySimulation.RuntimeController.CameraController;
                if (cameraController != null)
                    return;
            }

            if (simulation is LocomotionRollbackSimulation locomotionSimulation &&
                locomotionSimulation.RuntimeController != null)
            {
                cameraController = locomotionSimulation.RuntimeController.CameraController;
            }
        }

        void ResolveReferences()
        {
            if (inputRecorder == null)
                inputRecorder = GetComponent<PredictionInputHistoryTickRecorder>();

            if (snapshotRecorder == null)
                snapshotRecorder = GetComponent<LocomotionSnapshotHistoryRecorder>();

            if (simulation == null)
                ResolveSimulationFromBehaviour();
            if (simulation == null)
                ResolveSimulationFromComponents(GetComponents<MonoBehaviour>());

            if (presentationInterpolator == null)
                presentationInterpolator = GetComponent<PresentationTransformInterpolator>();

            if (cameraController == null)
                ResolveCameraFromSimulation();
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
