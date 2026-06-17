using System.Text;
using ThirdPersonCamera;
using ThirdPersonDiagnostics;
using ThirdPersonPresentation;
using UnityEngine;

namespace ThirdPersonSimulation
{
    [DisallowMultipleComponent]
    public sealed class LocalRollbackSoakDebugRunner : MonoBehaviour
    {
        [SerializeField] MonoBehaviour simulationBehaviour;
        [SerializeField, Min(1)] int seed = 12345;
        [SerializeField, Min(1)] int tickCount = 600;
        [SerializeField, Min(1)] int rollbackFrames = 8;
        [SerializeField] bool stopOnFailure = true;
        [SerializeField] KeyCode triggerKey = KeyCode.F8;
        [SerializeField] bool runOnKeyDown = true;
        [SerializeField] bool logSuccess = true;
        [SerializeField] bool applyReplayResultToScene;
        [SerializeField] PresentationTransformInterpolator presentationInterpolator;
        [SerializeField] ThirdPersonCameraController cameraController;
        [SerializeField, Min(0f)] float visualCorrectionSeconds = 0.12f;

        ILocalRollbackSynctestSimulation simulation;

        public MonoBehaviour SimulationBehaviour { get => simulationBehaviour; set { simulationBehaviour = value; simulation = value as ILocalRollbackSynctestSimulation; } }
        public ILocalRollbackSynctestSimulation Simulation => simulation;
        public int Seed { get => seed; set => seed = value; }
        public int TickCount { get => tickCount; set => tickCount = Mathf.Max(1, value); }
        public int RollbackFrames { get => rollbackFrames; set => rollbackFrames = Mathf.Max(1, value); }
        public bool StopOnFailure { get => stopOnFailure; set => stopOnFailure = value; }
        public KeyCode TriggerKey { get => triggerKey; set => triggerKey = value; }
        public bool RunOnKeyDown { get => runOnKeyDown; set => runOnKeyDown = value; }
        public bool LogSuccess { get => logSuccess; set => logSuccess = value; }
        public bool ApplyReplayResultToScene { get => applyReplayResultToScene; set => applyReplayResultToScene = value; }
        public PresentationTransformInterpolator PresentationInterpolator { get => presentationInterpolator; set => presentationInterpolator = value; }
        public ThirdPersonCameraController CameraController { get => cameraController; set => cameraController = value; }
        public float VisualCorrectionSeconds { get => visualCorrectionSeconds; set => visualCorrectionSeconds = Mathf.Max(0f, value); }
        public LocalRollbackSoakResult LastResult { get; private set; }
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
                RunSoak();
        }

        public bool RunSoak()
        {
            ResolveReferences();
            if (simulation == null)
                return Fail("missing simulation");
            if (tickCount < rollbackFrames)
                return Fail($"tickCount smaller than rollbackFrames tickCount={tickCount} rollbackFrames={rollbackFrames}");

            int capacity = Mathf.Max(tickCount + 1, rollbackFrames + 1);
            PredictionInputHistory inputHistory = new PredictionInputHistory(capacity);
            PredictionSnapshotHistory snapshotHistory = new PredictionSnapshotHistory(capacity);
            CharacterSimulationSnapshot startSnapshot = simulation.CaptureSnapshot(SimulationTick.Zero);
            PresentationDebugRestoreCapture presentationCapture = PresentationDebugRestoreGuard.Capture(presentationInterpolator);
            CharacterSimulationSnapshot replaySnapshot = startSnapshot;
            SoakRestoreProbe restoreProbe = default;

            try
            {
                LocalRollbackSoakInputConfig inputConfig = new LocalRollbackSoakInputConfig(seed, tickCount);
                LocalRollbackSoakInputGenerator.Populate(in inputConfig, inputHistory, snapshotHistory, simulation);
                replaySnapshot = simulation.CaptureSnapshot(new SimulationTick(tickCount));

                LocalRollbackSoakRunner runner = new LocalRollbackSoakRunner(inputHistory, snapshotHistory, simulation);
                LocalRollbackSoakConfig config = new LocalRollbackSoakConfig(seed, tickCount, rollbackFrames, stopOnFailure);
                LastResult = runner.Run(in config, CharacterSimulationSnapshotTolerance.Default);
            }
            finally
            {
                CharacterSimulationSnapshot finalSnapshot = applyReplayResultToScene ? replaySnapshot : startSnapshot;
                simulation.Restore(in finalSnapshot);
                CompleteDebugRestore();
                PresentationDebugRestoreGuard.Restore(
                    presentationInterpolator,
                    applyReplayResultToScene,
                    visualCorrectionSeconds,
                    in presentationCapture);
                restoreProbe = CaptureRestoreProbe(in finalSnapshot, in presentationCapture);
            }

            HasResult = true;
            LocalRollbackSoakResult result = LastResult;
            LogResult(in result, in restoreProbe);
            LogFirstMismatch(in result);
            return LastResult.Success;
        }

        bool Fail(string reason)
        {
            LastResult = new LocalRollbackSoakResult(
                false,
                seed,
                tickCount,
                rollbackFrames,
                0,
                reason,
                default);
            HasResult = true;
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Simulation,
                RuntimeDiagnosticLogLevel.Warning,
                "rollback-soak-result",
                "",
                "",
                0,
                Time.frameCount,
                $"ROLLBACK_SOAK_RESULT result=FAIL seed={seed} tickCount={tickCount} rollbackFrames={rollbackFrames} checkedWindows=0 reason={reason}",
                "Simulation.rollback-soak-result"));
            return false;
        }

        void LogResult(in LocalRollbackSoakResult result, in SoakRestoreProbe restoreProbe)
        {
            if (result.Success && !logSuccess)
                return;

            RuntimeDiagnosticLogLevel level = result.Success
                ? RuntimeDiagnosticLogLevel.Info
                : RuntimeDiagnosticLogLevel.Warning;
            StringBuilder builder = new StringBuilder(192);
            builder.Append("ROLLBACK_SOAK_RESULT");
            builder.Append(" result=").Append(result.Success ? "PASS" : "FAIL");
            builder.Append(" seed=").Append(result.Seed);
            builder.Append(" tickCount=").Append(result.TickCount);
            builder.Append(" rollbackFrames=").Append(result.RollbackFrames);
            builder.Append(" checkedWindows=").Append(result.CheckedWindows);
            builder.Append(" applyReplay=").Append(applyReplayResultToScene);
            builder.Append(" presentationDrift=").Append(result.HasPresentationDrift);
            if (result.HasPresentationDrift)
            {
                LocalRollbackSynctestResult drift = result.FirstPresentationDrift;
                builder.Append(" firstPresentationRestore=").Append(drift.RestoreTick.Value);
                builder.Append(" firstPresentationEnd=").Append(drift.EndTick.Value);
                if (drift.FirstMismatch.HasPresentationDrift)
                {
                    builder.Append(" firstPresentationStage=").Append(drift.FirstMismatch.Stage);
                    builder.Append(" firstPresentationTick=").Append(drift.FirstMismatch.Tick.Value);
                    if (drift.FirstMismatch.Comparison.PresentationDifferences.Count > 0)
                        builder.Append(" firstPresentationDifferences=").Append(string.Join(",", drift.FirstMismatch.Comparison.PresentationDifferences));
                }
            }
            builder.Append(" sourceRestored=").Append(restoreProbe.SourceRestored);
            builder.Append(" visualRestored=").Append(restoreProbe.VisualRestored);
            builder.Append(" cameraLocalOnly=").Append(restoreProbe.CameraLocalOnly);
            builder.Append(" visualChecked=").Append(restoreProbe.VisualChecked);
            if (!result.Success && !string.IsNullOrWhiteSpace(result.FailureReason))
                builder.Append(" reason=").Append(result.FailureReason);

            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Simulation,
                level,
                "rollback-soak-result",
                "",
                "",
                result.TickCount,
                Time.frameCount,
                builder.ToString(),
                "Simulation.rollback-soak-result"));
        }

        SoakRestoreProbe CaptureRestoreProbe(
            in CharacterSimulationSnapshot expectedSnapshot,
            in PresentationDebugRestoreCapture presentationCapture)
        {
            bool sourceRestored = false;
            if (simulation != null)
            {
                CharacterSimulationSnapshot actualSnapshot = simulation.CaptureSnapshot(expectedSnapshot.Tick);
                CharacterSimulationSnapshotComparison comparison = CharacterSimulationSnapshotComparer.Compare(
                    in expectedSnapshot,
                    in actualSnapshot,
                    CharacterSimulationSnapshotTolerance.Default);
                sourceRestored = comparison.Matches;
            }

            bool visualRestored = !presentationCapture.HasVisualStartPose;
            if (presentationCapture.HasVisualStartPose && presentationInterpolator != null && presentationInterpolator.VisualTarget != null)
                visualRestored = PoseMatches(
                    presentationCapture.VisualStartPose,
                    PresentationPose.FromTransform(presentationInterpolator.VisualTarget));

            return new SoakRestoreProbe(
                sourceRestored,
                visualRestored,
                true,
                presentationCapture.HasVisualStartPose);
        }

        static bool PoseMatches(in PresentationPose expected, in PresentationPose actual)
        {
            return Vector3.Distance(expected.Position, actual.Position) <= 0.001f &&
                   Quaternion.Angle(expected.Rotation, actual.Rotation) <= 0.01f;
        }

        void LogFirstMismatch(in LocalRollbackSoakResult result)
        {
            if (result.Success || string.IsNullOrEmpty(result.FirstFailure.FailureReason))
                return;

            LocalRollbackSynctestResult failure = result.FirstFailure;
            StringBuilder builder = new StringBuilder(384);
            builder.Append("ROLLBACK_SOAK_FIRST_MISMATCH");
            builder.Append(" seed=").Append(result.Seed);
            builder.Append(" restore=").Append(failure.RestoreTick.Value);
            builder.Append(" end=").Append(failure.EndTick.Value);
            builder.Append(" reason=").Append(failure.FailureReason);
            if (failure.FirstMismatch.HasMismatch)
            {
                builder.Append(" stage=").Append(failure.FirstMismatch.Stage);
                builder.Append(" tick=").Append(failure.FirstMismatch.Tick.Value);
                if (failure.FirstMismatch.Comparison.Differences.Count > 0)
                    builder.Append(" firstDifferences=").Append(string.Join(",", failure.FirstMismatch.Comparison.Differences));
                if (failure.FirstMismatch.Comparison.PresentationDifferences.Count > 0)
                    builder.Append(" firstPresentationDifferences=").Append(string.Join(",", failure.FirstMismatch.Comparison.PresentationDifferences));
            }

            if (failure.Comparison.Differences.Count > 0)
                builder.Append(" differences=").Append(string.Join(",", failure.Comparison.Differences));
            if (failure.Comparison.PresentationDifferences.Count > 0)
                builder.Append(" presentationDifferences=").Append(string.Join(",", failure.Comparison.PresentationDifferences));

            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Simulation,
                RuntimeDiagnosticLogLevel.Warning,
                "rollback-soak-first-mismatch",
                "",
                "",
                failure.EndTick.Value,
                Time.frameCount,
                builder.ToString(),
                "Simulation.rollback-soak-first-mismatch"));
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

        readonly struct SoakRestoreProbe
        {
            public SoakRestoreProbe(
                bool sourceRestored,
                bool visualRestored,
                bool cameraLocalOnly,
                bool visualChecked)
            {
                SourceRestored = sourceRestored;
                VisualRestored = visualRestored;
                CameraLocalOnly = cameraLocalOnly;
                VisualChecked = visualChecked;
            }

            public bool SourceRestored { get; }
            public bool VisualRestored { get; }
            public bool CameraLocalOnly { get; }
            public bool VisualChecked { get; }
        }
    }
}
