using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicRollback;
using ThirdPersonSimulation.Fixed;
using UnityEditor;
using UnityEngine;
using FixedWorldBodyState = ThirdPersonSimulation.Fixed.WorldBodyState;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public readonly struct CharacterFixedInputTraceSummary
    {
        public CharacterFixedInputTraceSummary(
            string traceId,
            string path,
            DateTime createdUtc,
            int frameCount,
            int tickRate)
        {
            TraceId = traceId;
            Path = path;
            CreatedUtc = createdUtc;
            FrameCount = frameCount;
            TickRate = tickRate;
        }

        public string TraceId { get; }
        public string Path { get; }
        public DateTime CreatedUtc { get; }
        public int FrameCount { get; }
        public int TickRate { get; }
    }

    [InitializeOnLoad]
    public static class CharacterFixedInputTraceWorkflow
    {
        const string Schema = "character-fixed-input-trace/2";
        const string ReplayProofSchema = "character-fixed-input-replay-proof/2";
        const string ReplayTickDriveMode = "one-fixed-tick-per-presentation-frame";
        const string ReplayPresentationClockMode = "logic-locked";
        const string StandardReplayOperation = "replay";
        const string ScheduleCaptureOperation = "schedule-record";
        const string ScheduleReplayOperation = "schedule-replay";
        const string PlayerActorId = "gameplay-lab-player";
        const string PendingOperationKey = "ThirdPerson.CharacterInputTrace.PendingOperation.v1";
        const string PendingTraceIdKey = "ThirdPerson.CharacterInputTrace.PendingTraceId.v2";
        const string PendingVariantKey = "ThirdPerson.CharacterInputTrace.PendingVariant.v1";
        const string PendingDeadlineKey = "ThirdPerson.CharacterInputTrace.PendingDeadline.v1";
        const string PendingLaunchPhaseKey = "ThirdPerson.CharacterInputTrace.PendingLaunchPhase.v1";
        const double PendingSeconds = 60d;
        const float PositionTolerance = 0.1f;
        const float YawTolerance = 2f;

        enum PendingLaunchPhase
        {
            AwaitingEditMode = 1,
            ReadyToPlay = 2,
            AwaitingPlayMode = 3,
            Running = 4
        }

        static int s_RecordingVariantIndex;
        static bool s_ReplayOwnsSampling;
        static bool s_ReplayWaitingForSampling;
        static bool s_ReplayFinalizing;
        static bool s_ReplayOwnsTickDrive;
        static int s_ReplayIssuedTickCount;
        static string s_ActiveReplayOperation = StandardReplayOperation;
        static CharacterFixedInputPresentationScheduleRun s_PresentationScheduleRun;
        static CharacterFixedInputPresentationSchedule s_ActivePresentationSchedule;
        static IReadOnlyList<GameplayPresentationScheduleFrame> s_LastPresentationScheduleFrames;
        static string s_LastPresentationSchedulePath = string.Empty;
        static TraceDocument s_PendingReplayDocument;
        static TraceDocument s_ActiveReplayDocument;
        static FixedCharacterInputReplayEvidence s_LastReplayEvidence;
        static string s_LastReplayProofPath = string.Empty;
        static string s_LastReplayComparison = string.Empty;
        static string s_LastTracePath = string.Empty;
        static string s_LastTraceId = string.Empty;
        static string s_LastStatus = string.Empty;
        static string s_LastFailure = string.Empty;

        static CharacterFixedInputTraceWorkflow()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            s_LastTracePath = FindLatestTracePath();
            s_LastTraceId = TraceIdFromPath(s_LastTracePath);
            if (IsPending)
            {
                try
                {
                    EnsurePendingTracePreparation();
                    if (EditorApplication.isPlaying &&
                        ReadPendingLaunchPhase() == PendingLaunchPhase.AwaitingPlayMode)
                    {
                        WritePendingLaunchPhase(PendingLaunchPhase.Running);
                        ResetPendingDeadline();
                    }
                }
                catch (Exception exception)
                {
                    AbortPendingInitialization(exception);
                }
            }
        }

        public static bool IsRecording =>
            FixedCharacterInputTraceModule.Status.Mode == FixedCharacterInputTraceMode.Recording;
        public static bool IsReplaying =>
            FixedCharacterInputTraceModule.Status.Mode == FixedCharacterInputTraceMode.Replaying ||
            FixedCharacterInputTraceModule.Status.Mode == FixedCharacterInputTraceMode.Completed ||
            s_ReplayFinalizing;
        public static bool IsPending => !string.IsNullOrEmpty(EditorPrefs.GetString(PendingOperationKey, string.Empty));
        public static string PendingOperation => EditorPrefs.GetString(PendingOperationKey, string.Empty);
        public static string LastTracePath => s_LastTracePath;
        public static string LastTraceId => s_LastTraceId;
        public static string LastStatus => s_LastStatus;
        public static string LastFailure => s_LastFailure;
        public static string LastReplayProofPath => s_LastReplayProofPath;
        public static string LastReplayComparison => s_LastReplayComparison;
        public static string LastPresentationSchedulePath =>
            s_LastPresentationSchedulePath;
        public static string TraceDirectory => ResolveTraceDirectory();

        public static void StartRecording()
        {
            RequireAvailable();
            FixedCharacterInputTraceModule.PrepareRecording(
                new ActorId(PlayerActorId));
            s_LastFailure = string.Empty;
            IGameplayLabLauncherOperations operations = RequireLauncher();
            GameplayLabLauncherState state = operations.ReadState();
            ArmPending("record", string.Empty, state.SelectedVariantIndex);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.ExitPlaymode();
                s_LastStatus = "Restarting Gameplay Lab before canonical Fixed input recording.";
                return;
            }
            StartPendingPlayMode();
            s_LastStatus = "Starting Gameplay Lab before canonical Fixed input recording.";
        }

        public static string StopAndSaveRecording()
        {
            if (!IsRecording)
                throw new InvalidOperationException("Canonical Fixed input recording is not active.");
            FixedCharacterInputTrace trace = FixedCharacterInputTraceModule.StopRecording();
            TraceDocument document = CreateDocument(
                trace,
                s_RecordingVariantIndex);
            string path = SaveDocument(document);
            s_LastTracePath = path;
            s_LastTraceId = trace.TraceId;
            s_LastStatus = $"Saved {trace.Frames.Count} canonical Fixed input frames.";
            Debug.Log($"Canonical Fixed input trace saved. Trace={trace.TraceId}, Frames={trace.Frames.Count}, Path={path}");
            return path;
        }

        public static void ReplayLast()
        {
            string path = FindLatestTracePath();
            if (string.IsNullOrEmpty(path))
                throw new FileNotFoundException("No canonical Fixed input trace has been recorded.");
            ReplayPath(path, StandardReplayOperation);
        }

        public static void ReplayTrace(string traceId)
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                ReplayLast();
                return;
            }
            string path = Directory.Exists(ResolveTraceDirectory())
                ? Directory.EnumerateFiles(ResolveTraceDirectory(), "*.json", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(value => string.Equals(
                        Path.GetFileNameWithoutExtension(value).Split('-').LastOrDefault(),
                        traceId.Trim(),
                        StringComparison.Ordinal))
                : string.Empty;
            if (string.IsNullOrEmpty(path))
                throw new FileNotFoundException($"Canonical Fixed input trace '{traceId}' was not found.");
            ReplayPath(path, StandardReplayOperation);
        }

        public static void RecordPresentationSchedule(string traceId)
        {
            ReplayPath(
                ResolveTracePath(traceId),
                ScheduleCaptureOperation);
        }

        public static void ReplayWithPresentationSchedule(string traceId)
        {
            ReplayPath(
                ResolveTracePath(traceId),
                ScheduleReplayOperation);
        }

        static string ResolveTracePath(string traceId)
        {
            string path;
            if (string.IsNullOrWhiteSpace(traceId))
            {
                path = FindLatestTracePath();
            }
            else
            {
                path = Directory.Exists(ResolveTraceDirectory())
                    ? Directory.EnumerateFiles(
                            ResolveTraceDirectory(),
                            "*.json",
                            SearchOption.TopDirectoryOnly)
                        .FirstOrDefault(value => string.Equals(
                            System.IO.Path.GetFileNameWithoutExtension(value)
                                .Split('-').LastOrDefault(),
                            traceId.Trim(),
                            StringComparison.Ordinal))
                    : string.Empty;
            }
            return !string.IsNullOrEmpty(path)
                ? path
                : throw new FileNotFoundException(
                    $"Canonical Fixed input trace '{traceId}' was not found.");
        }

        public static void Stop()
        {
            if (IsRecording)
                throw new InvalidOperationException("Use Stop and Save Input to finish an active recording.");
            ClearPending();
            s_ReplayWaitingForSampling = false;
            StopPresentationScheduleRun();
            CloseReplaySamplingWindow();
            ReleaseReplayTickDrive();
            s_PendingReplayDocument = null;
            s_ActiveReplayDocument = null;
            s_LastReplayEvidence = null;
            if (s_ReplayOwnsSampling &&
                (CharacterFootLandingPredictionSampler.IsCapturing ||
                 CharacterFootLandingPredictionSampler.IsStartPending))
            {
                CharacterFootLandingPredictionSampler.StopAndSaveSampling();
            }
            ClearReplayOwnership();
            FixedCharacterInputTraceModule.Stop();
            EditorApplication.isPaused = false;
            s_LastStatus = "Canonical Fixed input trace operation stopped.";
        }

        public static IReadOnlyList<CharacterFixedInputTraceSummary> ListTraces()
        {
            string directory = ResolveTraceDirectory();
            if (!Directory.Exists(directory))
                return Array.Empty<CharacterFixedInputTraceSummary>();
            var values = new List<CharacterFixedInputTraceSummary>();
            foreach (string path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
            {
                TraceDocument document = ReadDocument(path, false);
                values.Add(new CharacterFixedInputTraceSummary(
                    document.trace_id,
                    path,
                    DateTime.Parse(document.created_utc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    document.frame_count,
                    document.tick_rate));
            }
            values.Sort((left, right) => right.CreatedUtc.CompareTo(left.CreatedUtc));
            return values;
        }

        public static void RevealTraceDirectory()
        {
            Directory.CreateDirectory(ResolveTraceDirectory());
            EditorUtility.RevealInFinder(ResolveTraceDirectory());
        }

        static void ReplayPath(string path, string operation)
        {
            RequireAvailable();
            if (operation != StandardReplayOperation &&
                operation != ScheduleCaptureOperation &&
                operation != ScheduleReplayOperation)
            {
                throw new ArgumentOutOfRangeException(nameof(operation));
            }
            TraceDocument document = ReadDocument(path, true);
            FixedCharacterInputTrace trace = ToRuntimeTrace(document);
            s_ActiveReplayDocument = document;
            FixedCharacterInputTraceModule.PrepareReplay(trace);
            s_PendingReplayDocument = document;
            s_LastFailure = string.Empty;
            s_LastTracePath = path;
            s_LastTraceId = document.trace_id;
            ArmPending(
                operation,
                document.trace_id,
                document.launcher_variant_index);
            s_LastStatus =
                $"Restarting Gameplay Lab for {operation} trace {document.trace_id}.";
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.ExitPlaymode();
                return;
            }
            StartPendingPlayMode();
        }

        static void BeginRecording()
        {
            if (!TryResolvePlayerStartState(
                    out FixedCharacterHost host,
                    out FixedWorldBodyState initialBody,
                    out PoseRecord pose))
            {
                return;
            }
            RequirePoseMatchesBody(pose, initialBody);
            s_RecordingVariantIndex = EditorPrefs.GetInt(PendingVariantKey, -1);
            if (s_RecordingVariantIndex < 0)
                throw new InvalidOperationException("Canonical Fixed input recording has no Gameplay Lab variant identity.");
            if (host.SessionHost.LifecycleState != SimulationSessionLifecycleState.Active)
                return;
            FixedCharacterInputTraceModule.StartRecording();
            ClearPending();
            EditorApplication.isPaused = false;
            s_LastStatus = "Recording canonical character input per Fixed simulation Tick. Camera input remains live.";
        }

        static void BeginReplay(TraceDocument document)
        {
            s_ActiveReplayDocument ??= document;
            FixedCharacterInputTrace trace = ToRuntimeTrace(document);
            if (!s_ReplayWaitingForSampling)
            {
                if (!TryResolvePlayerStartState(
                        out FixedCharacterHost host,
                        out FixedWorldBodyState initialBody,
                        out PoseRecord current) ||
                    host.SessionHost.LifecycleState !=
                    SimulationSessionLifecycleState.Active)
                {
                    return;
                }
                RequireBodyEquals(
                    initialBody,
                    trace.StartBody,
                    "Replay Session InitialBody does not match the trace canonical start body.");
                float positionError = Vector3.Distance(
                    current.Position,
                    document.start_pose.Position);
                float yawError = Mathf.Abs(Mathf.DeltaAngle(current.yaw_degrees, document.start_pose.yaw_degrees));
                if (positionError > PositionTolerance || yawError > YawTolerance)
                {
                    throw new InvalidOperationException(
                        $"Replay start state does not match the recording. PositionError={positionError:0.###}, YawError={yawError:0.###}.");
                }
                ResetPendingDeadline();
                CharacterFootLandingPredictionSampler.StartControlledSampling();
                s_ReplayOwnsSampling = true;
                s_ReplayWaitingForSampling = true;
                EditorApplication.isPaused = false;
                s_LastStatus =
                    $"Canonical start body restored. Waiting for Foot Landing sampling before replaying {trace.Frames.Count} Fixed input frames.";
            }
            if (CharacterFootLandingPredictionSampler.IsStartPending)
                return;
            if (!CharacterFootLandingPredictionSampler.IsCapturing)
                throw new InvalidOperationException(
                    string.IsNullOrEmpty(CharacterFootLandingPredictionSampler.LastStartFailure)
                        ? "Foot Landing sampling did not start."
                        : CharacterFootLandingPredictionSampler.LastStartFailure);
            string operation = PendingOperation;
            FixedCharacterInputTraceModule.StartReplay();
            CharacterFootLandingPredictionSampler.OpenControlledCaptureWindow();
            s_ActiveReplayOperation = operation;
            if (operation == StandardReplayOperation)
            {
                BeginReplayTickDrive(trace.Frames.Count);
            }
            else if (operation == ScheduleCaptureOperation)
            {
                s_PresentationScheduleRun =
                    CharacterFixedInputPresentationScheduleRun.StartCapture(
                        CloseReplaySamplingWindow);
                s_ActivePresentationSchedule = null;
            }
            else
            {
                CharacterFixedInputPresentationScheduleBinding binding =
                    BuildPresentationScheduleBinding(document);
                string schedulePath =
                    CharacterFixedInputPresentationSchedule.FindLatestPath(
                        document.trace_id);
                s_ActivePresentationSchedule =
                    CharacterFixedInputPresentationSchedule.Load(
                        schedulePath,
                        in binding);
                s_LastPresentationSchedulePath = schedulePath;
                s_PresentationScheduleRun =
                    CharacterFixedInputPresentationScheduleRun.StartReplay(
                        s_ActivePresentationSchedule,
                        CloseReplaySamplingWindow);
            }
            s_ReplayWaitingForSampling = false;
            ClearPending();
            FixedCharacterInputTraceStatus status =
                FixedCharacterInputTraceModule.Status;
            s_LastStatus =
                $"{operation} started for {status.FrameCount} canonical Fixed input frames with Foot Landing sampling active.";
        }

        static void Tick()
        {
            try
            {
                if (IsPending)
                    TickPending();
                TickActiveReplay();
                TickReplayFinalization();
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        static void TickPending()
        {
            if (DateTime.UtcNow.Ticks > ReadPendingDeadline())
                throw new TimeoutException($"Canonical Fixed input {PendingOperation} timed out while starting Gameplay Lab.");
            PendingLaunchPhase phase = ReadPendingLaunchPhase();
            if (phase == PendingLaunchPhase.ReadyToPlay)
            {
                StartPendingPlayMode();
                return;
            }
            if (phase != PendingLaunchPhase.Running || !EditorApplication.isPlaying)
                return;
            EnsurePendingTracePreparation();
            string operation = PendingOperation;
            if (string.Equals(operation, "record", StringComparison.Ordinal))
            {
                BeginRecording();
                return;
            }
            if (operation != StandardReplayOperation &&
                operation != ScheduleCaptureOperation &&
                operation != ScheduleReplayOperation)
            {
                throw new InvalidOperationException(
                    $"Canonical Fixed input pending operation '{operation}' is invalid.");
            }
            s_PendingReplayDocument ??= ReadPendingReplayDocument();
            BeginReplay(s_PendingReplayDocument);
        }

        static void TickActiveReplay()
        {
            FixedCharacterInputTraceStatus status =
                FixedCharacterInputTraceModule.Status;
            if (status.Mode == FixedCharacterInputTraceMode.Faulted)
            {
                StopPresentationScheduleRun();
                CloseReplaySamplingWindow();
                ReleaseReplayTickDrive();
                throw new InvalidOperationException(status.Message);
            }
            bool scheduled =
                s_ActiveReplayOperation == ScheduleCaptureOperation ||
                s_ActiveReplayOperation == ScheduleReplayOperation;
            if (scheduled)
            {
                if (status.Mode != FixedCharacterInputTraceMode.Replaying &&
                    status.Mode != FixedCharacterInputTraceMode.Completed)
                {
                    return;
                }
                CharacterFixedInputPresentationScheduleRun scheduleRun =
                    s_PresentationScheduleRun ??
                    throw new InvalidOperationException(
                        "Fixed replay has no active Presentation Schedule run.");
                if (!string.IsNullOrEmpty(scheduleRun.Failure))
                    throw new InvalidDataException(scheduleRun.Failure);
                if (status.Mode == FixedCharacterInputTraceMode.Replaying)
                    return;
                if (status.Mode != FixedCharacterInputTraceMode.Completed ||
                    !scheduleRun.Completed ||
                    !scheduleRun.DriveRestored)
                {
                    return;
                }
                s_LastPresentationScheduleFrames =
                    scheduleRun.ObservedFrames.ToArray();
                scheduleRun.Dispose();
                s_PresentationScheduleRun = null;
            }
            else
            {
                if (status.Mode == FixedCharacterInputTraceMode.Replaying)
                {
                    AdvanceReplayTickDrive(status);
                    return;
                }
                if (status.Mode != FixedCharacterInputTraceMode.Completed)
                    return;
                CloseReplaySamplingWindow();
                ReleaseReplayTickDrive();
            }

            s_LastReplayEvidence =
                FixedCharacterInputTraceModule.CaptureReplayEvidence();
            if (s_ReplayOwnsSampling &&
                (CharacterFootLandingPredictionSampler.IsCapturing ||
                 CharacterFootLandingPredictionSampler.IsStartPending))
            {
                CharacterFootLandingPredictionSampler.StopAndSaveSampling();
            }
            s_ReplayOwnsSampling = false;
            s_ReplayWaitingForSampling = false;
            ClearPending();
            FixedCharacterInputTraceModule.Stop();
            if (CharacterFootLandingPredictionSampler.IsFinalizing)
            {
                s_ReplayFinalizing = true;
                s_LastStatus =
                    "Canonical Fixed input replay completed. Foot Landing diagnostics are finalizing.";
                return;
            }
            PublishReplayCompletion();
        }

        static void TickReplayFinalization()
        {
            if (!s_ReplayFinalizing || CharacterFootLandingPredictionSampler.IsFinalizing)
                return;
            s_ReplayFinalizing = false;
            if (!string.IsNullOrEmpty(CharacterFootLandingPredictionSampler.LastFinalizationFailure))
            {
                s_LastFailure = CharacterFootLandingPredictionSampler.LastFinalizationFailure;
                s_LastStatus = $"Foot Landing finalization failed: {s_LastFailure}";
                Debug.LogError(s_LastStatus);
                return;
            }
            PublishReplayCompletion();
        }

        static void PublishReplayCompletion()
        {
            if (s_ActiveReplayOperation == ScheduleCaptureOperation)
            {
                PublishPresentationSchedule();
            }
            else if (s_ActiveReplayOperation == ScheduleReplayOperation)
            {
                PublishScheduledReplayProof();
            }
            else
            {
                PublishReplayProof();
            }
            s_LastStatus =
                $"{s_ActiveReplayOperation} completed. " +
                $"Schedule={s_LastPresentationSchedulePath}, " +
                $"Samples={CharacterFootLandingPredictionSampler.LastSavedPath}, " +
                $"Facts={CharacterFootLandingPredictionSampler.LastSavedFactsPath}, " +
                $"Diagnoses={CharacterFootLandingPredictionSampler.LastSavedDiagnosisDirectory}.";
            s_ActiveReplayOperation = StandardReplayOperation;
            Debug.Log(s_LastStatus);
        }

        static void PublishPresentationSchedule()
        {
            TraceDocument trace = s_ActiveReplayDocument ??
                throw new InvalidOperationException(
                    "Presentation Schedule capture has no active Trace.");
            if (s_LastReplayEvidence == null ||
                s_LastPresentationScheduleFrames == null)
            {
                throw new InvalidOperationException(
                    "Presentation Schedule capture evidence is incomplete.");
            }
            var representative =
                CharacterFixedInputPresentationScheduleEvidenceAnalyzer.Analyze(
                    CharacterFootLandingPredictionSampler.LastSavedPath,
                    CharacterFootLandingPredictionSampler.LastSavedGeometryPath);
            CharacterFixedInputPresentationScheduleBinding binding =
                BuildPresentationScheduleBinding(trace);
            CharacterFixedInputPresentationSchedule schedule =
                CharacterFixedInputPresentationSchedule.Create(
                    in binding,
                    s_LastPresentationScheduleFrames,
                    in representative);
            s_LastPresentationSchedulePath = schedule.Save();
            s_ActivePresentationSchedule = schedule;
            s_ActiveReplayDocument = null;
            s_LastReplayEvidence = null;
            s_LastPresentationScheduleFrames = null;
        }



        static void PublishScheduledReplayProof()
        {
            TraceDocument trace = s_ActiveReplayDocument ??
                throw new InvalidOperationException(
                    "Scheduled replay completion has no active Trace.");
            FixedCharacterInputReplayEvidence evidence =
                s_LastReplayEvidence ??
                throw new InvalidOperationException(
                    "Scheduled replay completion has no Fixed evidence.");
            CharacterFixedInputPresentationSchedule schedule =
                s_ActivePresentationSchedule ??
                throw new InvalidOperationException(
                    "Scheduled replay completion has no Presentation Schedule.");
            IReadOnlyList<GameplayPresentationScheduleFrame> scheduleFrames =
                s_LastPresentationScheduleFrames ??
                throw new InvalidOperationException(
                    "Scheduled replay completion has no Schedule frame evidence.");
            var coverage =
                CharacterFixedInputPresentationScheduleEvidenceAnalyzer
                    .AnalyzeCoverage(
                        CharacterFootLandingPredictionSampler.LastSavedPath,
                        scheduleFrames);
            CharacterFixedInputPresentationScheduleBinding binding =
                BuildPresentationScheduleBinding(trace);
            CharacterFixedInputScheduledReplayProofResult result =
                CharacterFixedInputScheduledReplayProof.Publish(
                    in binding,
                    schedule,
                    evidence,
                    scheduleFrames,
                    in coverage,
                    CharacterFootLandingPredictionSampler.LastSavedPath,
                    CharacterFootLandingPredictionSampler.LastSavedFactsPath);
            s_LastReplayProofPath = result.Path;
            s_LastReplayComparison = result.Summary;
            s_ActiveReplayDocument = null;
            s_LastReplayEvidence = null;
            s_LastPresentationScheduleFrames = null;
            if (!result.Matched)
                throw new InvalidDataException(result.Summary);
        }

        static void PublishReplayProof()
        {
            TraceDocument trace = s_ActiveReplayDocument ??
                throw new InvalidOperationException(
                    "Fixed input replay completion has no active Trace document.");
            FixedCharacterInputReplayEvidence evidence = s_LastReplayEvidence ??
                throw new InvalidOperationException(
                    "Fixed input replay completion has no runtime evidence.");
            if (!string.Equals(
                    trace.trace_id,
                    evidence.TraceId,
                    StringComparison.Ordinal) ||
                trace.frame_count != evidence.Frames.Count)
            {
                throw new InvalidDataException(
                    "Fixed input replay evidence does not match the active Trace.");
            }
            ReplayFootSampleDocument sample = ReadReplayFootSample();
            var document = new ReplayProofDocument
            {
                schema = ReplayProofSchema,
                run_id = Guid.NewGuid().ToString("N"),
                created_utc = DateTime.UtcNow.ToString(
                    "O",
                    CultureInfo.InvariantCulture),
                trace_id = trace.trace_id,
                trace_content_hash = trace.content_hash,
                start_body_hash = evidence.StartBodyHash.ToString(),
                replay_start_tick = evidence.ReplayStartTick,
                frame_count = evidence.Frames.Count,
                tick_drive_mode = ReplayTickDriveMode,
                presentation_clock_mode = ReplayPresentationClockMode,
                input_sequence_hash = evidence.InputSequenceHash.ToString(),
                body_trajectory_hash = evidence.BodyTrajectoryHash.ToString(),
                foot_sample = sample,
                frames = BuildReplayProofFrames(evidence)
            };
            string directory = ResolveReplayProofDirectory(trace.trace_id);
            Directory.CreateDirectory(directory);
            string baselinePath = FindLatestReplayProofPath(directory);
            if (!string.IsNullOrEmpty(baselinePath))
            {
                ReplayProofDocument baseline = ReadReplayProof(baselinePath);
                document.comparison = CompareReplayProofs(
                    baseline,
                    document,
                    baselinePath);
                s_LastReplayComparison = document.comparison.matched
                    ? $"matched:{document.frame_count}:{baselinePath}"
                    : $"mismatch:{document.comparison.divergent_frame_count}:{baselinePath}";
            }
            else
            {
                document.comparison = new ReplayComparisonDocument
                {
                    baseline_available = false,
                    matched = true,
                    baseline_path = string.Empty,
                    compared_frame_count = 0,
                    aggregate_mismatches = Array.Empty<ReplayMismatchDocument>(),
                    divergent_frame_count = 0,
                    first_divergent_relative_frame = -1,
                    first_frame_mismatches = Array.Empty<ReplayMismatchDocument>()
                };
                s_LastReplayComparison =
                    $"baseline-created:{document.frame_count}";
            }
            document.proof_hash = ComputeReplayProofHash(document);
            string path = Path.Combine(
                directory,
                $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{document.run_id}.json");
            string partPath = path + ".part";
            try
            {
                File.WriteAllText(
                    partPath,
                    JsonConvert.SerializeObject(document, Formatting.Indented),
                    new UTF8Encoding(false));
                File.Move(partPath, path);
            }
            catch
            {
                if (File.Exists(partPath))
                    File.Delete(partPath);
                throw;
            }
            s_LastReplayProofPath = path;
            if (!document.comparison.matched)
                throw new InvalidDataException(
                    DescribeReplayComparisonFailure(document.comparison, path));
            s_ActiveReplayDocument = null;
            s_LastReplayEvidence = null;
        }

        static ReplayFootSampleDocument ReadReplayFootSample()
        {
            string factsPath =
                CharacterFootLandingPredictionSampler.LastSavedFactsPath;
            string samplesPath =
                CharacterFootLandingPredictionSampler.LastSavedPath;
            if (string.IsNullOrWhiteSpace(factsPath) ||
                string.IsNullOrWhiteSpace(samplesPath) ||
                !File.Exists(factsPath) ||
                !File.Exists(samplesPath))
            {
                throw new InvalidDataException(
                    "Fixed input replay Foot sample artifacts are unavailable.");
            }
            JObject facts = JObject.Parse(
                File.ReadAllText(factsPath, Encoding.UTF8));
            JObject sample = facts["sample"] as JObject ??
                throw new InvalidDataException(
                    "Fixed input replay Foot facts sample is unavailable.");
            int frameCount = sample.Value<int?>("frameCount") ?? 0;
            if (frameCount <= 0)
                throw new InvalidDataException(
                    "Fixed input replay Foot sample frame count is invalid.");
            return new ReplayFootSampleDocument
            {
                sample_identity =
                    sample.Value<string>("identity") ?? string.Empty,
                samples_path = samplesPath,
                facts_path = factsPath,
                samples_sha256 =
                    sample.Value<string>("sha256") ?? string.Empty,
                sampling_relative_frame_count = frameCount
            };
        }

        static ReplayProofFrameDocument[] BuildReplayProofFrames(
            FixedCharacterInputReplayEvidence evidence)
        {
            var frames = new ReplayProofFrameDocument[evidence.Frames.Count];
            for (int i = 0; i < frames.Length; i++)
            {
                FixedCharacterInputReplayFrameEvidence source =
                    evidence.Frames[i];
                FixedWorldBodyState body = source.Body;
                frames[i] = new ReplayProofFrameDocument
                {
                    relative_frame = source.RelativeFrame,
                    recorded_tick = source.RecordedTick,
                    replay_tick = source.ReplayTick,
                    input_hash = source.InputHash.ToString(),
                    body_hash = source.BodyHash.ToString(),
                    body_position_x_raw = body.Position.X.Raw,
                    body_position_y_raw = body.Position.Y.Raw,
                    body_position_z_raw = body.Position.Z.Raw,
                    body_yaw_raw = body.Yaw.Degrees.Raw,
                    body_velocity_x_raw = body.Velocity.X.Raw,
                    body_velocity_y_raw = body.Velocity.Y.Raw,
                    body_velocity_z_raw = body.Velocity.Z.Raw,
                    body_vertical_velocity_raw =
                        body.VerticalVelocity.Raw,
                    body_grounded = body.Grounded,
                    body_collision = (byte)body.Collision
                };
            }
            return frames;
        }

        static ReplayComparisonDocument CompareReplayProofs(
            ReplayProofDocument baseline,
            ReplayProofDocument current,
            string baselinePath)
        {
            var aggregate = new List<ReplayMismatchDocument>();
            AddReplayMismatch(
                aggregate,
                "trace_id",
                baseline.trace_id,
                current.trace_id);
            AddReplayMismatch(
                aggregate,
                "trace_content_hash",
                baseline.trace_content_hash,
                current.trace_content_hash);
            AddReplayMismatch(
                aggregate,
                "start_body_hash",
                baseline.start_body_hash,
                current.start_body_hash);
            AddReplayMismatch(
                aggregate,
                "frame_count",
                baseline.frame_count.ToString(CultureInfo.InvariantCulture),
                current.frame_count.ToString(CultureInfo.InvariantCulture));
            AddReplayMismatch(
                aggregate,
                "tick_drive_mode",
                baseline.tick_drive_mode,
                current.tick_drive_mode);
            AddReplayMismatch(
                aggregate,
                "presentation_clock_mode",
                baseline.presentation_clock_mode,
                current.presentation_clock_mode);
            AddReplayMismatch(
                aggregate,
                "input_sequence_hash",
                baseline.input_sequence_hash,
                current.input_sequence_hash);
            AddReplayMismatch(
                aggregate,
                "body_trajectory_hash",
                baseline.body_trajectory_hash,
                current.body_trajectory_hash);
            AddReplayMismatch(
                aggregate,
                "sampling_relative_frame_count",
                baseline.foot_sample.sampling_relative_frame_count.ToString(
                    CultureInfo.InvariantCulture),
                current.foot_sample.sampling_relative_frame_count.ToString(
                    CultureInfo.InvariantCulture));
            AddReplayMismatch(
                aggregate,
                "proof_frame_count",
                baseline.frames.Length.ToString(CultureInfo.InvariantCulture),
                current.frames.Length.ToString(CultureInfo.InvariantCulture));

            int comparedFrameCount = Math.Min(
                baseline.frames.Length,
                current.frames.Length);
            int divergentFrameCount = 0;
            int firstDivergentRelativeFrame = -1;
            var firstFrameMismatches = new List<ReplayMismatchDocument>();
            for (int i = 0; i < comparedFrameCount; i++)
            {
                ReplayProofFrameDocument left = baseline.frames[i];
                ReplayProofFrameDocument right = current.frames[i];
                bool differs =
                    left.relative_frame != right.relative_frame ||
                    left.recorded_tick != right.recorded_tick ||
                    !string.Equals(
                        left.input_hash,
                        right.input_hash,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        left.body_hash,
                        right.body_hash,
                        StringComparison.Ordinal);
                if (!differs)
                    continue;
                divergentFrameCount++;
                if (firstDivergentRelativeFrame >= 0)
                    continue;
                firstDivergentRelativeFrame = right.relative_frame;
                AddReplayMismatch(
                    firstFrameMismatches,
                    "relative_frame",
                    left.relative_frame.ToString(CultureInfo.InvariantCulture),
                    right.relative_frame.ToString(CultureInfo.InvariantCulture));
                AddReplayMismatch(
                    firstFrameMismatches,
                    "recorded_tick",
                    left.recorded_tick.ToString(CultureInfo.InvariantCulture),
                    right.recorded_tick.ToString(CultureInfo.InvariantCulture));
                AddReplayMismatch(
                    firstFrameMismatches,
                    "input_hash",
                    left.input_hash,
                    right.input_hash);
                AddReplayMismatch(
                    firstFrameMismatches,
                    "body_hash",
                    left.body_hash,
                    right.body_hash);
            }
            return new ReplayComparisonDocument
            {
                baseline_available = true,
                matched = aggregate.Count == 0 &&
                          divergentFrameCount == 0,
                baseline_path = baselinePath,
                compared_frame_count = comparedFrameCount,
                aggregate_mismatches = aggregate.ToArray(),
                divergent_frame_count = divergentFrameCount,
                first_divergent_relative_frame =
                    firstDivergentRelativeFrame,
                first_frame_mismatches =
                    firstFrameMismatches.ToArray()
            };
        }

        static void AddReplayMismatch(
            ICollection<ReplayMismatchDocument> mismatches,
            string field,
            string baseline,
            string candidate)
        {
            if (string.Equals(
                    baseline,
                    candidate,
                    StringComparison.Ordinal))
            {
                return;
            }
            mismatches.Add(new ReplayMismatchDocument
            {
                field = field,
                baseline = baseline ?? string.Empty,
                candidate = candidate ?? string.Empty
            });
        }

        static string DescribeReplayComparisonFailure(
            ReplayComparisonDocument comparison,
            string candidatePath)
        {
            string aggregateFields = string.Join(
                ",",
                comparison.aggregate_mismatches.Select(value => value.field));
            string firstFrameFields = string.Join(
                ",",
                comparison.first_frame_mismatches.Select(value => value.field));
            return
                $"Fixed input replay proof mismatch. Candidate={candidatePath}, " +
                $"AggregateFields=[{aggregateFields}], " +
                $"DivergentFrameCount={comparison.divergent_frame_count}, " +
                $"FirstDivergentRelativeFrame={comparison.first_divergent_relative_frame}, " +
                $"FirstFrameFields=[{firstFrameFields}].";
        }

        static ReplayProofDocument ReadReplayProof(string path)
        {
            ReplayProofDocument document =
                JsonConvert.DeserializeObject<ReplayProofDocument>(
                    File.ReadAllText(path, Encoding.UTF8));
            if (document == null ||
                document.schema != ReplayProofSchema ||
                document.frame_count <= 0 ||
                document.frames == null ||
                document.frames.Length != document.frame_count ||
                document.foot_sample == null)
            {
                throw new InvalidDataException(
                    "Fixed input replay proof document is incomplete.");
            }
            return document;
        }

        static string ComputeReplayProofHash(
            ReplayProofDocument document)
        {
            using IncrementalHash hash =
                IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            AppendHash(hash, document.schema);
            AppendHash(hash, document.run_id);
            AppendHash(hash, document.created_utc);
            AppendHash(hash, document.trace_id);
            AppendHash(hash, document.trace_content_hash);
            AppendHash(hash, document.start_body_hash);
            AppendHash(
                hash,
                document.replay_start_tick.ToString(
                    CultureInfo.InvariantCulture));
            AppendHash(
                hash,
                document.frame_count.ToString(
                    CultureInfo.InvariantCulture));
            AppendHash(hash, document.tick_drive_mode);
            AppendHash(hash, document.presentation_clock_mode);
            AppendHash(hash, document.input_sequence_hash);
            AppendHash(hash, document.body_trajectory_hash);
            AppendHash(
                hash,
                document.foot_sample.sampling_relative_frame_count.ToString(
                    CultureInfo.InvariantCulture));
            AppendHash(hash, document.foot_sample.samples_sha256);
            for (int i = 0; i < document.frames.Length; i++)
            {
                ReplayProofFrameDocument frame = document.frames[i];
                AppendHash(
                    hash,
                    frame.relative_frame.ToString(
                        CultureInfo.InvariantCulture));
                AppendHash(
                    hash,
                    frame.recorded_tick.ToString(
                        CultureInfo.InvariantCulture));
                AppendHash(hash, frame.input_hash);
                AppendHash(hash, frame.body_hash);
            }
            byte[] bytes = hash.GetHashAndReset();
            var builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString(
                    "x2",
                    CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        static string FindLatestReplayProofPath(string directory) =>
            Directory.EnumerateFiles(
                    directory,
                    "*.json",
                    SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault() ?? string.Empty;

        static string ResolveReplayProofDirectory(string traceId) =>
            Path.Combine(
                Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    "..",
                    "Temp",
                    "CharacterInputReplayProofs",
                    "v2")),
                traceId);

        static void StartPendingPlayMode()
        {
            if (!IsPending ||
                ReadPendingLaunchPhase() != PendingLaunchPhase.ReadyToPlay ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            EnsurePendingTracePreparation();
            IGameplayLabLauncherOperations operations = RequireLauncher();
            int variantIndex = EditorPrefs.GetInt(PendingVariantKey, -1);
            ResetPendingDeadline();
            WritePendingLaunchPhase(PendingLaunchPhase.AwaitingPlayMode);
            operations.Play(variantIndex);
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && IsPending)
            {
                if (ReadPendingLaunchPhase() != PendingLaunchPhase.AwaitingPlayMode)
                    return;
                WritePendingLaunchPhase(PendingLaunchPhase.Running);
                ResetPendingDeadline();
                EditorApplication.isPaused = false;
                return;
            }
            if (state == PlayModeStateChange.EnteredEditMode && IsPending)
            {
                if (ReadPendingLaunchPhase() != PendingLaunchPhase.AwaitingEditMode)
                    return;
                WritePendingLaunchPhase(PendingLaunchPhase.ReadyToPlay);
                ResetPendingDeadline();
                EditorApplication.delayCall += StartPendingPlayMode;
                return;
            }
            if (state != PlayModeStateChange.ExitingPlayMode)
                return;
            if (IsRecording)
            {
                s_LastFailure = "Play Mode ended before canonical Fixed input recording was saved.";
                FixedCharacterInputTraceModule.Stop();
            }
            if (IsReplaying)
            {
                StopPresentationScheduleRun();
                AbandonReplayTickDrive();
                if (s_ReplayOwnsSampling &&
                    (CharacterFootLandingPredictionSampler.IsCapturing ||
                     CharacterFootLandingPredictionSampler.IsStartPending))
                {
                    CharacterFootLandingPredictionSampler.StopAndSaveSampling();
                }
                ClearReplayOwnership();
                FixedCharacterInputTraceModule.Stop();
            }
        }

        static void OnBeforeAssemblyReload()
        {
            if (IsRecording)
                s_LastFailure = "Script reload interrupted canonical Fixed input recording before it was saved.";
            if (s_ReplayOwnsSampling &&
                (CharacterFootLandingPredictionSampler.IsCapturing ||
                 CharacterFootLandingPredictionSampler.IsStartPending))
            {
                CharacterFootLandingPredictionSampler.StopAndSaveSampling();
            }
            StopPresentationScheduleRun();
            AbandonReplayTickDrive();
            ClearReplayOwnership();
            FixedCharacterInputTraceModule.Stop();
        }

        static void Fail(Exception exception)
        {
            s_LastFailure = exception.Message;
            s_LastStatus = $"Canonical Fixed input trace failed: {exception.Message}";
            ClearPending();
            s_ReplayWaitingForSampling = false;
            s_PendingReplayDocument = null;
            s_ActiveReplayDocument = null;
            s_LastReplayEvidence = null;
            s_LastPresentationScheduleFrames = null;
            StopPresentationScheduleRun();
            CloseReplaySamplingWindow();
            ReleaseReplayTickDrive();
            if (s_ReplayOwnsSampling &&
                (CharacterFootLandingPredictionSampler.IsCapturing ||
                 CharacterFootLandingPredictionSampler.IsStartPending))
            {
                CharacterFootLandingPredictionSampler.StopAndSaveSampling();
            }
            ClearReplayOwnership();
            FixedCharacterInputTraceModule.Stop();
            EditorApplication.isPaused = false;
            Debug.LogException(exception);
        }

        static void ClearReplayOwnership()
        {
            s_ReplayOwnsSampling = false;
            s_ReplayWaitingForSampling = false;
            s_ReplayFinalizing = false;
            s_ActiveReplayOperation = StandardReplayOperation;
            s_PendingReplayDocument = null;
            s_ActiveReplayDocument = null;
            s_LastReplayEvidence = null;
            s_LastPresentationScheduleFrames = null;
            s_ActivePresentationSchedule = null;
        }

        static void RequireAvailable()
        {
            if (EditorApplication.isCompiling)
                throw new InvalidOperationException("Canonical Fixed input trace is unavailable while scripts are compiling.");
            if (IsPending || IsRecording || IsReplaying || s_ReplayWaitingForSampling ||
                CharacterFootLandingPredictionSampler.IsFinalizing)
                throw new InvalidOperationException("Another canonical Fixed input trace operation is already active.");
            if (CharacterFootLandingPredictionSampler.IsCapturing || CharacterFootLandingPredictionSampler.IsStartPending)
                throw new InvalidOperationException("Foot Landing sampling is already active.");
        }

        static IGameplayLabLauncherOperations RequireLauncher() =>
            GameplayLabLauncherRegistry.Operations ??
            throw new InvalidOperationException("Gameplay Lab launcher operations are not registered.");

        static bool TryResolvePlayerStartState(
            out FixedCharacterHost selectedHost,
            out FixedWorldBodyState initialBody,
            out PoseRecord pose)
        {
            FixedCharacterHost[] hosts = UnityEngine.Object.FindObjectsByType<FixedCharacterHost>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < hosts.Length; i++)
            {
                FixedCharacterHost host = hosts[i];
                if (host == null || host.RootHierarchy == null ||
                    host.RootHierarchy.LogicRoot == null ||
                    !string.Equals(
                        host.ActorId.Value,
                        PlayerActorId,
                        StringComparison.Ordinal) ||
                    !host.TryGetInitialBody(out initialBody))
                {
                    continue;
                }
                Transform root = host.RootHierarchy.LogicRoot;
                selectedHost = host;
                pose = new PoseRecord
                {
                    x = root.position.x,
                    y = root.position.y,
                    z = root.position.z,
                    yaw_degrees = root.eulerAngles.y
                };
                return true;
            }
            selectedHost = null;
            initialBody = default;
            pose = null;
            return false;
        }

        static PoseRecord PoseFromBody(FixedWorldBodyState body) => new PoseRecord
        {
            x = body.Position.X.ToSingle(),
            y = body.Position.Y.ToSingle(),
            z = body.Position.Z.ToSingle(),
            yaw_degrees = body.Yaw.Degrees.ToSingle()
        };

        static void RequirePoseMatchesBody(PoseRecord pose, FixedWorldBodyState body)
        {
            PoseRecord expected = PoseFromBody(body);
            float positionError = Vector3.Distance(
                pose.Position,
                expected.Position);
            float yawError = Mathf.Abs(Mathf.DeltaAngle(
                pose.yaw_degrees,
                expected.yaw_degrees));
            if (positionError > PositionTolerance || yawError > YawTolerance)
            {
                throw new InvalidOperationException(
                    $"Gameplay Lab LogicRoot does not match its formal InitialBody. " +
                    $"PositionError={positionError:0.###}, YawError={yawError:0.###}.");
            }
        }

        static void RequireBodyEquals(
            FixedWorldBodyState actual,
            FixedWorldBodyState expected,
            string message)
        {
            if (actual.ActorId != expected.ActorId ||
                actual.Position != expected.Position ||
                actual.Yaw != expected.Yaw ||
                actual.Velocity != expected.Velocity ||
                actual.VerticalVelocity != expected.VerticalVelocity ||
                actual.Grounded != expected.Grounded ||
                actual.Collision != expected.Collision)
            {
                throw new InvalidOperationException(message);
            }
        }

        static TraceDocument CreateDocument(
            FixedCharacterInputTrace trace,
            int launcherVariantIndex)
        {
            var document = new TraceDocument
            {
                schema = Schema,
                trace_id = trace.TraceId,
                created_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                actor_id = trace.ActorId.Value,
                program_hash = trace.ProgramHash.ToString(),
                tick_rate = trace.TickRate,
                first_tick = trace.Frames[0].Tick.Value,
                frame_count = trace.Frames.Count,
                launcher_variant_index = launcherVariantIndex,
                start_pose = PoseFromBody(trace.StartBody),
                frames = new TraceFrameDocument[trace.Frames.Count]
            };
            for (int i = 0; i < trace.Frames.Count; i++)
            {
                FixedCharacterInputTraceFrame frame = trace.Frames[i];
                var rollback = new RollbackActorInputFrame(
                    frame.ActorId,
                    new SimulationTick(frame.Input.TickSource.SourceTick),
                    frame.Input.Sequence,
                    frame.Input,
                    RollbackInputProvenance.LocalExplicit);
                document.frames[i] = new TraceFrameDocument
                {
                    simulation_tick = frame.Tick.Value,
                    input_payload_base64 = Convert.ToBase64String(
                        RollbackInputCodec.WriteInput(rollback))
                };
            }
            document.content_hash = ComputeContentHash(document);
            return document;
        }

        static FixedCharacterInputTrace ToRuntimeTrace(TraceDocument document)
        {
            var frames = new FixedCharacterInputTraceFrame[document.frames.Length];
            var actorId = new ActorId(document.actor_id);
            for (int i = 0; i < frames.Length; i++)
            {
                TraceFrameDocument source = document.frames[i];
                RollbackActorInputFrame frame = RollbackInputCodec.ReadInput(
                    Convert.FromBase64String(source.input_payload_base64));
                if (frame.Provenance != RollbackInputProvenance.LocalExplicit || frame.ActorId != actorId)
                    throw new InvalidDataException($"Canonical Fixed input trace frame {i} has an invalid identity.");
                frames[i] = new FixedCharacterInputTraceFrame(
                    frame.ActorId,
                    new SimulationTick(source.simulation_tick),
                    frame.Input);
            }
            return new FixedCharacterInputTrace(
                document.trace_id,
                actorId,
                new ProgramHash(new StableHash(document.program_hash)),
                document.tick_rate,
                BuildStartBody(document, actorId),
                frames);
        }

        static FixedWorldBodyState BuildStartBody(
            TraceDocument document,
            ActorId actorId) =>
            new FixedWorldBodyState(
                actorId,
                new FixedVector3(
                    FixedScalar.FromSingle(document.start_pose.x),
                    FixedScalar.FromSingle(document.start_pose.y),
                    FixedScalar.FromSingle(document.start_pose.z)),
                new FixedYaw(FixedScalar.FromSingle(
                    document.start_pose.yaw_degrees)),
                FixedVector3.Zero,
                FixedScalar.Zero,
                true,
                ThirdPersonSimulation.Fixed.WorldCollisionSummary.Below);

        static string SaveDocument(TraceDocument document)
        {
            string directory = ResolveTraceDirectory();
            Directory.CreateDirectory(directory);
            string timestamp = DateTime.Parse(
                document.created_utc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind).ToLocalTime().ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
            string path = Path.Combine(directory, $"{timestamp}-{document.trace_id}.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(document, Formatting.Indented), new UTF8Encoding(false));
            return path;
        }

        static TraceDocument ReadDocument(string path, bool validatePayload)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException("Canonical Fixed input trace file is unavailable.", path);
            TraceDocument document = JsonConvert.DeserializeObject<TraceDocument>(File.ReadAllText(path, Encoding.UTF8));
            if (document == null || !string.Equals(document.schema, Schema, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(document.trace_id) || string.IsNullOrWhiteSpace(document.actor_id) ||
                string.IsNullOrWhiteSpace(document.program_hash) || document.tick_rate <= 0 ||
                document.frame_count <= 0 || document.frames == null ||
                document.frames.Length != document.frame_count || document.start_pose == null)
            {
                throw new InvalidDataException("Canonical Fixed input trace document is incomplete.");
            }
            if (document.launcher_variant_index < 0)
                throw new InvalidDataException("Canonical Fixed input trace Gameplay Lab variant is invalid.");
            for (int i = 0; i < document.frames.Length; i++)
            {
                TraceFrameDocument frame = document.frames[i];
                if (frame == null || frame.simulation_tick == 0 ||
                    string.IsNullOrWhiteSpace(frame.input_payload_base64))
                {
                    throw new InvalidDataException($"Canonical Fixed input trace frame {i} is incomplete.");
                }
            }
            string actualHash = ComputeContentHash(document);
            if (!string.Equals(document.content_hash, actualHash, StringComparison.Ordinal))
                throw new InvalidDataException("Canonical Fixed input trace content hash is invalid.");
            if (validatePayload)
            {
                FixedCharacterInputTrace trace = ToRuntimeTrace(document);
                if (trace.Frames[0].Tick.Value != document.first_tick)
                    throw new InvalidDataException("Canonical Fixed input trace first Tick is invalid.");
            }
            return document;
        }

        static string ComputeContentHash(TraceDocument document)
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            AppendHash(hash, document.schema);
            AppendHash(hash, document.trace_id);
            AppendHash(hash, document.created_utc);
            AppendHash(hash, document.actor_id);
            AppendHash(hash, document.program_hash);
            AppendHash(hash, document.tick_rate.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, document.first_tick.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, document.frame_count.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, document.launcher_variant_index.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, document.start_pose.x.ToString("R", CultureInfo.InvariantCulture));
            AppendHash(hash, document.start_pose.y.ToString("R", CultureInfo.InvariantCulture));
            AppendHash(hash, document.start_pose.z.ToString("R", CultureInfo.InvariantCulture));
            AppendHash(hash, document.start_pose.yaw_degrees.ToString("R", CultureInfo.InvariantCulture));
            for (int i = 0; i < document.frames.Length; i++)
            {
                AppendHash(
                    hash,
                    document.frames[i].simulation_tick.ToString(CultureInfo.InvariantCulture));
                AppendHash(hash, document.frames[i].input_payload_base64);
            }
            byte[] bytes = hash.GetHashAndReset();
            var builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        static void AppendHash(IncrementalHash hash, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            hash.AppendData(bytes);
            hash.AppendData(new byte[] { 0x1f });
        }

        static void EnsurePendingTracePreparation()
        {
            if (!IsPending ||
                FixedCharacterInputTraceModule.Status.Mode !=
                FixedCharacterInputTraceMode.Idle)
            {
                return;
            }
            string operation = PendingOperation;
            if (string.Equals(operation, "record", StringComparison.Ordinal))
            {
                FixedCharacterInputTraceModule.PrepareRecording(
                    new ActorId(PlayerActorId));
                return;
            }
            if (operation != StandardReplayOperation &&
                operation != ScheduleCaptureOperation &&
                operation != ScheduleReplayOperation)
            {
                throw new InvalidOperationException(
                    $"Canonical Fixed input pending operation '{operation}' is invalid.");
            }
            s_PendingReplayDocument ??= ReadPendingReplayDocument();
            FixedCharacterInputTraceModule.PrepareReplay(
                ToRuntimeTrace(s_PendingReplayDocument));
        }

        static TraceDocument ReadPendingReplayDocument()
        {
            string traceId = EditorPrefs.GetString(
                PendingTraceIdKey,
                string.Empty);
            if (string.IsNullOrWhiteSpace(traceId))
            {
                throw new InvalidDataException(
                    "Canonical Fixed input pending TraceId is unavailable.");
            }
            return ReadDocument(ResolveTracePath(traceId), true);
        }

        static void ArmPending(string operation, string traceId, int variantIndex)
        {
            EditorPrefs.SetString(PendingOperationKey, operation);
            EditorPrefs.SetString(PendingTraceIdKey, traceId ?? string.Empty);
            EditorPrefs.SetInt(PendingVariantKey, variantIndex);
            WritePendingLaunchPhase(
                EditorApplication.isPlayingOrWillChangePlaymode
                    ? PendingLaunchPhase.AwaitingEditMode
                    : PendingLaunchPhase.ReadyToPlay);
            ResetPendingDeadline();
        }

        static CharacterFixedInputPresentationScheduleBinding
            BuildPresentationScheduleBinding(TraceDocument document) =>
            new CharacterFixedInputPresentationScheduleBinding(
                document.trace_id,
                document.content_hash,
                document.actor_id,
                document.program_hash,
                document.tick_rate,
                document.frame_count,
                document.launcher_variant_index);

        static void BeginReplayTickDrive(int frameCount)
        {
            if (frameCount <= 0 || s_ReplayOwnsTickDrive ||
                !GameplayTickSystem.IsInitialized)
            {
                throw new InvalidOperationException(
                    "Canonical Fixed replay Tick drive is unavailable.");
            }
            RequireTickDriveCommand(GameplayTickDriveCommand.SetPresentationClock(
                GameplayPresentationDebugClockMode.LogicLockedPresentation));
            RequireTickDriveCommand(GameplayTickDriveCommand.Step(1));
            s_ReplayOwnsTickDrive = true;
            s_ReplayIssuedTickCount = 1;
        }

        static void AdvanceReplayTickDrive(FixedCharacterInputTraceStatus status)
        {
            if (!s_ReplayOwnsTickDrive ||
                status.ReplayedFrameCount > s_ReplayIssuedTickCount)
            {
                throw new InvalidOperationException(
                    "Canonical Fixed replay Tick drive lost its frame boundary.");
            }
            if (status.ReplayedFrameCount < s_ReplayIssuedTickCount ||
                s_ReplayIssuedTickCount >= status.FrameCount)
            {
                return;
            }
            RequireTickDriveCommand(GameplayTickDriveCommand.Step(1));
            s_ReplayIssuedTickCount++;
        }

        static void ReleaseReplayTickDrive()
        {
            if (!s_ReplayOwnsTickDrive)
                return;
            if (!GameplayTickSystem.IsInitialized)
            {
                AbandonReplayTickDrive();
                return;
            }
            try
            {
                RequireTickDriveCommand(GameplayTickDriveCommand.SetPresentationClock(
                    GameplayPresentationDebugClockMode.LivePresentation));
                RequireTickDriveCommand(GameplayTickDriveCommand.SetRealtime());
            }
            finally
            {
                AbandonReplayTickDrive();
            }
        }

        static void AbandonReplayTickDrive()
        {
            s_ReplayOwnsTickDrive = false;
            s_ReplayIssuedTickCount = 0;
        }

        static void RequireTickDriveCommand(GameplayTickDriveCommand command)
        {
            if (!GameplayTickSystem.EnqueueDriveCommand(command))
                throw new InvalidOperationException(
                    "Gameplay Tick System rejected the canonical Fixed replay drive command.");
        }

        static void StopPresentationScheduleRun()
        {
            if (s_PresentationScheduleRun == null)
                return;
            s_PresentationScheduleRun.Stop();
            s_PresentationScheduleRun.Dispose();
            s_PresentationScheduleRun = null;
        }

        static void CloseReplaySamplingWindow()
        {
            if (CharacterFootLandingPredictionSampler.IsControlledCaptureWindow &&
                CharacterFootLandingPredictionSampler.IsCaptureWindowOpen)
            {
                CharacterFootLandingPredictionSampler.CloseControlledCaptureWindow();
            }
        }

        static PendingLaunchPhase ReadPendingLaunchPhase()
        {
            var phase = (PendingLaunchPhase)EditorPrefs.GetInt(
                PendingLaunchPhaseKey,
                0);
            if (phase < PendingLaunchPhase.AwaitingEditMode ||
                phase > PendingLaunchPhase.Running)
            {
                throw new InvalidOperationException(
                    "Canonical Fixed input pending launch phase is invalid.");
            }
            return phase;
        }

        static void WritePendingLaunchPhase(PendingLaunchPhase phase) =>
            EditorPrefs.SetInt(PendingLaunchPhaseKey, (int)phase);

        static void ResetPendingDeadline() => EditorPrefs.SetString(
            PendingDeadlineKey,
            DateTime.UtcNow.AddSeconds(PendingSeconds).Ticks.ToString(CultureInfo.InvariantCulture));

        static long ReadPendingDeadline()
        {
            string value = EditorPrefs.GetString(PendingDeadlineKey, string.Empty);
            return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out long ticks)
                ? ticks
                : 0L;
        }

        static void ClearPending()
        {
            EditorPrefs.DeleteKey(PendingOperationKey);
            EditorPrefs.DeleteKey(PendingTraceIdKey);
            EditorPrefs.DeleteKey(
                "ThirdPerson.CharacterInputTrace.PendingTracePath.v1");
            EditorPrefs.DeleteKey(PendingVariantKey);
            EditorPrefs.DeleteKey(PendingDeadlineKey);
            EditorPrefs.DeleteKey(PendingLaunchPhaseKey);
            s_PendingReplayDocument = null;
        }

        static void AbortPendingInitialization(Exception exception)
        {
            s_LastFailure = exception.Message;
            s_LastStatus =
                $"Canonical Fixed input pending operation was aborted: {exception.Message}";
            ClearPending();
            FixedCharacterInputTraceModule.Stop();
            Debug.LogException(exception);
        }

        static string FindLatestTracePath()
        {
            string directory = ResolveTraceDirectory();
            return Directory.Exists(directory)
                ? Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault() ?? string.Empty
                : string.Empty;
        }

        static string TraceIdFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            string name = Path.GetFileNameWithoutExtension(path);
            int separator = name.LastIndexOf('-');
            return separator >= 0 && separator + 1 < name.Length
                ? name.Substring(separator + 1)
                : string.Empty;
        }

        static string ResolveTraceDirectory() => Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "..",
            "Diagnostics",
            "CharacterInputTraces"));

        [Serializable]
        sealed class ReplayProofDocument
        {
            public string schema;
            public string run_id;
            public string created_utc;
            public string trace_id;
            public string trace_content_hash;
            public string start_body_hash;
            public ulong replay_start_tick;
            public int frame_count;
            public string tick_drive_mode;
            public string presentation_clock_mode;
            public string input_sequence_hash;
            public string body_trajectory_hash;
            public ReplayFootSampleDocument foot_sample;
            public ReplayProofFrameDocument[] frames;
            public ReplayComparisonDocument comparison;
            public string proof_hash;
        }

        [Serializable]
        sealed class ReplayFootSampleDocument
        {
            public string sample_identity;
            public string samples_path;
            public string facts_path;
            public string samples_sha256;
            public int sampling_relative_frame_count;
        }

        [Serializable]
        sealed class ReplayProofFrameDocument
        {
            public int relative_frame;
            public ulong recorded_tick;
            public ulong replay_tick;
            public string input_hash;
            public string body_hash;
            public long body_position_x_raw;
            public long body_position_y_raw;
            public long body_position_z_raw;
            public long body_yaw_raw;
            public long body_velocity_x_raw;
            public long body_velocity_y_raw;
            public long body_velocity_z_raw;
            public long body_vertical_velocity_raw;
            public bool body_grounded;
            public byte body_collision;
        }

        [Serializable]
        sealed class ReplayComparisonDocument
        {
            public bool baseline_available;
            public bool matched;
            public string baseline_path;
            public int compared_frame_count;
            public ReplayMismatchDocument[] aggregate_mismatches;
            public int divergent_frame_count;
            public int first_divergent_relative_frame;
            public ReplayMismatchDocument[] first_frame_mismatches;
        }

        [Serializable]
        sealed class ReplayMismatchDocument
        {
            public string field;
            public string baseline;
            public string candidate;
        }

        [Serializable]
        sealed class PoseRecord
        {
            public float x;
            public float y;
            public float z;
            public float yaw_degrees;

            [JsonIgnore]
            public Vector3 Position => new Vector3(x, y, z);
        }

        [Serializable]
        sealed class TraceDocument
        {
            public string schema;
            public string trace_id;
            public string created_utc;
            public string actor_id;
            public string program_hash;
            public int tick_rate;
            public ulong first_tick;
            public int frame_count;
            public int launcher_variant_index;
            public PoseRecord start_pose;
            public TraceFrameDocument[] frames;
            public string content_hash;
        }

        [Serializable]
        sealed class TraceFrameDocument
        {
            public ulong simulation_tick;
            public string input_payload_base64;
        }
    }
}
