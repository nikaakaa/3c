using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonGameplay.Lab;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicRollback;
using ThirdPersonSimulation.Fixed;
using ThirdPersonPerformance.Instrumentation;
using Stopwatch = System.Diagnostics.Stopwatch;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace ThirdPersonPerformance.Runtime
{
    [DefaultExecutionOrder(-32000)]
    public sealed class ThirdPersonPerformanceCaptureAgent : MonoBehaviour
    {
        const string RequestArgument = "--third-person-performance-request=";
        const string TransportPortArgument = "--third-person-performance-port=";

        sealed class RecorderEntry
        {
            public PerformanceMetricDefinition Metric;
            public ProfilerRecorder Recorder;
            public int Capacity;
        }

        sealed class LogicTickSample
        {
            public PerformanceMetricDefinition Metric;
            public ulong LogicTick;
            public ulong RenderFrame;
            public long DurationTicks;
            public int Count;
        }

        [Serializable]
        sealed class InputTraceFrameDocument
        {
            public ulong simulation_tick = 0UL;
            public string input_payload_base64 = string.Empty;
        }

        [Serializable]
        sealed class InputTraceDocument
        {
            public string schema = string.Empty;
            public string trace_id = string.Empty;
            public string actor_id = string.Empty;
            public int tick_rate = 0;
            public int frame_count = 0;
            public InputTraceFrameDocument[] frames = Array.Empty<InputTraceFrameDocument>();
        }

        [Serializable]
        sealed class InstrumentationManifestDocument
        {
            public string schema = string.Empty;
            public string mode = string.Empty;
            public string catalog_revision = string.Empty;
            public string identity = string.Empty;
            public string weaver_version = string.Empty;
            public int span_layout_revision;
        }

        readonly List<RecorderEntry> m_Recorders = new List<RecorderEntry>();

        PerformanceRunRequestDocument m_Request;
        PerformancePlayerManifestDocument m_PlayerManifest;
        PerformanceScenarioDocument m_Scenario;
        PerformanceCaptureProfileDocument m_Profile;
        PerformanceCameraTraceDocument m_CameraTrace;
        PerformanceLoopbackClient m_Transport;
        PerformanceInstrumentationMode m_InstrumentationMode;
        string m_InstrumentationIdentity = string.Empty;
        PerformanceSpanBuffer m_InstrumentationBuffer;
        int m_TransportPort;
        PerformanceCaptureStatus m_Status = PerformanceCaptureStatus.Pending;
        string m_StartedUtc = string.Empty;
        string m_CurrentStage = "startup";
        string m_Message = string.Empty;
        int m_CaptureStartFrame;
        int m_DroppedLogicTicksAtStart;
        int m_LastCameraFrame;
        float m_CaptureStartedRealtime;
        bool m_ReadySent;
        bool m_WarmupBoundarySent;
        bool m_CompletionSent;
        bool m_ShuttingDown;
        int m_PendingExitCode;
        Exception m_RuntimeFailure;
        LogType m_RuntimeFailureType;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            string requestPath = ReadArgument(Environment.GetCommandLineArgs(), RequestArgument);
            if (string.IsNullOrEmpty(requestPath))
                return;
            var root = new GameObject(nameof(ThirdPersonPerformanceCaptureAgent));
            DontDestroyOnLoad(root);
            root.AddComponent<ThirdPersonPerformanceCaptureAgent>();
        }

        void Awake()
        {
            try
            {
                LoadRequest();
                Application.logMessageReceived += OnRuntimeLog;
                ConfigureInstrumentation();
                ConfigureRuntime();
                if (!IsOperation(PerformanceOperationKinds.Smoke))
                    PrepareReplay();
                StartTransport();
                m_Status = PerformanceCaptureStatus.WaitingForRuntime;
                m_CurrentStage = "waiting-for-runtime";
            }
            catch (Exception exception)
            {
                Fault("startup", exception);
            }
        }

        void Update()
        {
            if (m_ShuttingDown)
            {
                TryCompleteShutdown();
                return;
            }
            if (m_Status == PerformanceCaptureStatus.Faulted)
                return;
            if (m_RuntimeFailure != null)
            {
                Fault(m_CurrentStage, m_RuntimeFailure);
                return;
            }
            try
            {
                DrainCommands();
                UpdateCameraFrame();
                switch (m_Status)
                {
                    case PerformanceCaptureStatus.WaitingForRuntime:
                        UpdateReadiness();
                        break;
                    case PerformanceCaptureStatus.WarmingUp:
                        UpdateWarmup();
                        break;
                    case PerformanceCaptureStatus.Recording:
                        UpdateRecording();
                        break;
                    case PerformanceCaptureStatus.Replaying:
                        UpdateReplay();
                        break;
                }
            }
            catch (Exception exception)
            {
                Fault(m_CurrentStage, exception);
            }
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= OnRuntimeLog;
            PerformanceCameraInputOverride.Clear();
            PerformanceInstrumentationSpanRuntime.CancelCapture();
            DisposeRecorders();
            m_Transport?.Dispose();
            m_Transport = null;
        }

        void OnRuntimeLog(string message, string stackTrace, LogType type)
        {
            if (m_ShuttingDown || m_Status == PerformanceCaptureStatus.Faulted ||
                (type != LogType.Error && type != LogType.Exception && type != LogType.Assert))
                return;
            if (m_RuntimeFailure == null || type == LogType.Exception && m_RuntimeFailureType != LogType.Exception)
            {
                m_RuntimeFailure = new InvalidOperationException(message + "\n" + stackTrace);
                m_RuntimeFailureType = type;
            }
        }

        void LoadRequest()
        {
            string requestPath = ReadArgument(Environment.GetCommandLineArgs(), RequestArgument);
            m_Request = ReadJson<PerformanceRunRequestDocument>(requestPath);
            RequireSchema(m_Request.schema, PerformanceCaptureSchemas.Request, "request");
            m_PlayerManifest = ReadJson<PerformancePlayerManifestDocument>(m_Request.player_manifest_path);
            RequireSchema(m_PlayerManifest.schema, PerformanceCaptureSchemas.Player, "player manifest");
            RequireText(m_Request.instrumentation_identity, "Instrumentation identity");
            if (!string.Equals(m_Request.instrumentation_identity, m_PlayerManifest.instrumentation_identity, StringComparison.Ordinal))
                throw new InvalidDataException("Performance Request and Player instrumentation identities do not match.");
            if (!string.Equals(m_Request.instrumentation_mode, m_PlayerManifest.instrumentation_mode, StringComparison.Ordinal))
                throw new InvalidDataException("Performance Request and Player instrumentation modes do not match.");
            RequireText(m_PlayerManifest.instrumentation_identity, "Player instrumentation identity");
            if (!Enum.TryParse(m_PlayerManifest.instrumentation_mode, true, out m_InstrumentationMode) ||
                !Enum.IsDefined(typeof(PerformanceInstrumentationMode), m_InstrumentationMode) ||
                m_InstrumentationMode == PerformanceInstrumentationMode.Disabled)
            {
                throw new InvalidDataException("Performance Player instrumentation mode must be MarkerOnly or Span.");
            }
            if (m_PlayerManifest.instrumentation_span_layout_revision != PerformanceInstrumentationIdentity.SpanLayoutRevision)
                throw new InvalidDataException("Performance Player instrumentation Span layout is unsupported.");
            string playerRoot = Path.GetDirectoryName(Path.GetFullPath(m_Request.player_manifest_path));
            RequireText(m_PlayerManifest.instrumentation_manifest_path, "Player instrumentation manifest path");
            string instrumentationManifestPath = ResolvePath(playerRoot, m_PlayerManifest.instrumentation_manifest_path);
            if (!File.Exists(instrumentationManifestPath))
                throw new FileNotFoundException("Performance Player instrumentation manifest is missing.", instrumentationManifestPath);
            m_InstrumentationIdentity = m_PlayerManifest.instrumentation_identity;
            InstrumentationManifestDocument instrumentationManifest = ReadJson<InstrumentationManifestDocument>(instrumentationManifestPath);
            if (!string.Equals(instrumentationManifest.schema, PerformanceInstrumentationIdentity.ManifestSchema, StringComparison.Ordinal) ||
                !string.Equals(instrumentationManifest.mode, m_PlayerManifest.instrumentation_mode, StringComparison.Ordinal) ||
                !string.Equals(instrumentationManifest.identity, m_InstrumentationIdentity, StringComparison.Ordinal) ||
                !string.Equals(instrumentationManifest.catalog_revision, ComputeCatalogRevision(), StringComparison.Ordinal) ||
                !string.Equals(instrumentationManifest.weaver_version, PerformanceInstrumentationIdentity.WeaverVersion, StringComparison.Ordinal) ||
                instrumentationManifest.span_layout_revision != PerformanceInstrumentationIdentity.SpanLayoutRevision)
            {
                throw new InvalidDataException("Performance Player instrumentation manifest identity is invalid.");
            }
            if (!string.Equals(Path.GetFullPath(m_Request.request_document_path), Path.GetFullPath(requestPath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Performance Request document path identity is invalid.");
            if (!IsOperation(PerformanceOperationKinds.Smoke) &&
                !IsOperation(PerformanceOperationKinds.Replay) &&
                !IsOperation(PerformanceOperationKinds.Capture))
            {
                throw new InvalidDataException($"Performance operation '{m_Request.operation}' is unsupported.");
            }
            RequireText(m_Request.run_id, "RunId");
            if (!string.Equals(m_Request.transport, PerformanceCaptureSchemas.TransportId, StringComparison.Ordinal))
                throw new InvalidDataException("Performance transport identity is unsupported.");
            string portValue = ReadArgument(Environment.GetCommandLineArgs(), TransportPortArgument);
            if (!int.TryParse(portValue, NumberStyles.None, CultureInfo.InvariantCulture, out m_TransportPort) ||
                m_TransportPort <= 0 || m_TransportPort > ushort.MaxValue)
            {
                throw new InvalidDataException("Performance loopback port is invalid.");
            }
            RequireDirectory(m_Request.staging_root, "StagingRoot");
            m_Scenario = ReadJson<PerformanceScenarioDocument>(m_Request.scenario_path);
            RequireSchema(m_Scenario.schema, PerformanceCaptureSchemas.Scenario, "scenario");
            m_Profile = ReadJson<PerformanceCaptureProfileDocument>(m_Request.profile_path);
            RequireSchema(m_Profile.schema, PerformanceCaptureSchemas.Profile, "profile");
            RequireHash(m_Request.scenario_path, m_Request.scenario_hash, "scenario");
            RequireHash(m_Request.profile_path, m_Request.profile_hash, "profile");
            if (!string.Equals(m_Scenario.content_hash, PerformanceCaptureIdentity.Scenario(m_Scenario), StringComparison.Ordinal) ||
                !string.Equals(m_Profile.content_hash, PerformanceCaptureIdentity.Profile(m_Profile), StringComparison.Ordinal))
            {
                throw new InvalidDataException("Performance scenario or profile content identity is invalid.");
            }
            if (!string.Equals(m_Scenario.ready_condition, "gameplay-lab.session-active+locked-roster+fixed-start-body+metric-catalog-registered", StringComparison.Ordinal) ||
                !string.Equals(m_Scenario.capture_start_boundary, "after-fixed-input-warmup", StringComparison.Ordinal) ||
                !string.Equals(m_Scenario.capture_end_boundary, "fixed-input-replay-completed", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Performance Scenario Ready or Capture boundary contract is unsupported.");
            }
            if (m_Scenario.capture_logic_ticks <= 0 || m_Scenario.warmup_logic_ticks <= 0 ||
                m_Profile.maximum_presentation_fps <= 0 || m_Profile.logic_tick_rate <= 0 ||
                m_Profile.sample_capacity_margin_percent < 0)
            {
                throw new InvalidDataException("Performance scenario or profile timing is invalid.");
            }
            string scenarioDirectory = Path.GetDirectoryName(Path.GetFullPath(m_Request.scenario_path));
            string inputPath = ResolvePath(scenarioDirectory, m_Scenario.input_trace_path);
            string cameraPath = ResolvePath(scenarioDirectory, m_Scenario.camera_trace_path);
            RequireHash(inputPath, m_Scenario.input_trace_hash, "input trace");
            RequireHash(cameraPath, m_Scenario.camera_trace_hash, "camera trace");
            m_Scenario.input_trace_path = inputPath;
            m_Scenario.camera_trace_path = cameraPath;
            m_CameraTrace = ReadJson<PerformanceCameraTraceDocument>(cameraPath);
            RequireSchema(m_CameraTrace.schema, PerformanceCaptureSchemas.CameraTrace, "camera trace");
            if (!string.Equals(m_CameraTrace.content_hash, PerformanceCaptureIdentity.CameraTrace(m_CameraTrace), StringComparison.Ordinal))
                throw new InvalidDataException("Performance camera trace content identity is invalid.");
            if (m_CameraTrace.frame_count != m_Scenario.warmup_logic_ticks + m_Scenario.capture_logic_ticks)
                throw new InvalidDataException("Performance camera trace length does not match scenario logic ticks.");
        }

        void ConfigureInstrumentation()
        {
            if (IsOperation(PerformanceOperationKinds.Capture) &&
                m_InstrumentationMode != PerformanceInstrumentationMode.Span)
            {
                throw new InvalidDataException("Performance Capture requires a Span instrumentation Player.");
            }
            if (m_InstrumentationMode == PerformanceInstrumentationMode.Span)
            {
                if (m_Profile.instrumentation_span_capacity <= 0)
                    throw new InvalidDataException("Performance instrumentation Span capacity is invalid.");
                m_InstrumentationBuffer = new PerformanceSpanBuffer();
                m_InstrumentationBuffer.Begin(m_Profile.instrumentation_span_capacity);
            }
            PerformanceInstrumentationSpanRuntime.Configure(
                m_InstrumentationMode,
                m_InstrumentationBuffer);
        }

        void ConfigureRuntime()
        {
            Application.targetFrameRate = m_Scenario.target_frame_rate;
            QualitySettings.vSyncCount = m_Scenario.v_sync_count;
            if (m_Scenario.quality_level < 0 || m_Scenario.quality_level >= QualitySettings.names.Length)
                throw new InvalidDataException("Performance scenario quality level is invalid.");
            QualitySettings.SetQualityLevel(m_Scenario.quality_level, true);
            if (m_Scenario.width <= 0 || m_Scenario.height <= 0)
                throw new InvalidDataException("Performance scenario resolution is invalid.");
            Screen.SetResolution(m_Scenario.width, m_Scenario.height, false);
            if (!IsOperation(PerformanceOperationKinds.Smoke))
                PerformanceCameraInputOverride.Activate(m_CameraTrace);
        }

        void PrepareReplay()
        {
            InputTraceDocument document = ReadJson<InputTraceDocument>(m_Scenario.input_trace_path);
            if (!string.Equals(document.schema, "character-fixed-input-trace/3", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(document.trace_id) || string.IsNullOrWhiteSpace(document.actor_id) ||
                document.tick_rate != m_Profile.logic_tick_rate ||
                document.frame_count != m_Scenario.warmup_logic_ticks + m_Scenario.capture_logic_ticks ||
                document.frames == null || document.frames.Length != document.frame_count)
            {
                throw new InvalidDataException("Performance input trace is incomplete or incompatible.");
            }
            var actorId = new ActorId(document.actor_id);
            var frames = new FixedCharacterInputTraceFrame[document.frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                InputTraceFrameDocument source = document.frames[i] ??
                    throw new InvalidDataException($"Performance input trace frame {i} is missing.");
                RollbackActorInputFrame frame = RollbackInputCodec.ReadInput(Convert.FromBase64String(source.input_payload_base64));
                if (frame.ActorId != actorId || frame.Provenance != RollbackInputProvenance.LocalExplicit)
                    throw new InvalidDataException($"Performance input trace frame {i} identity is invalid.");
                frames[i] = new FixedCharacterInputTraceFrame(actorId, new SimulationTick(source.simulation_tick), frame.Input);
            }
            FixedCharacterInputTraceModule.PrepareReplay(
                new FixedCharacterInputTrace(document.trace_id, actorId, document.tick_rate, frames),
                m_Scenario.warmup_logic_ticks);
        }

        void StartTransport()
        {
            m_Transport = new PerformanceLoopbackClient(
                m_TransportPort,
                $"HELLO|{m_Request.run_id}|{System.Diagnostics.Process.GetCurrentProcess().Id}",
                checked(m_Profile.runtime_ready_timeout_seconds * 1000));
            m_Transport.Start();
        }

        void DrainCommands()
        {
            while (m_Transport != null && m_Transport.TryRead(out string command))
            {
                if (string.Equals(command, "START", StringComparison.Ordinal))
                {
                    if (IsOperation(PerformanceOperationKinds.Smoke) ||
                        m_Status != PerformanceCaptureStatus.WaitingForRuntime || !RuntimeReadyCore())
                        throw new InvalidOperationException("Performance START arrived before Runtime Ready.");
                    FixedCharacterInputTraceModule.StartReplay();
                    m_Status = PerformanceCaptureStatus.WarmingUp;
                    m_CurrentStage = "warmup";
                    Send("WARMING_UP");
                }
                else if (string.Equals(command, "STOP", StringComparison.Ordinal))
                {
                    if (!IsOperation(PerformanceOperationKinds.Smoke) ||
                        m_Status != PerformanceCaptureStatus.WaitingForRuntime || !m_ReadySent)
                    {
                        throw new InvalidOperationException("Performance STOP is only valid after Smoke READY.");
                    }
                    CompleteSmoke();
                }
                else if (string.Equals(command, "CONTINUE", StringComparison.Ordinal))
                {
                    if (!IsOperation(PerformanceOperationKinds.Replay) ||
                        m_Status != PerformanceCaptureStatus.WarmingUp || !m_WarmupBoundarySent ||
                        FixedCharacterInputTraceModule.Status.Mode != FixedCharacterInputTraceMode.ReplayPaused)
                    {
                        throw new InvalidOperationException("Performance CONTINUE arrived before the exact Replay warmup boundary.");
                    }
                    BeginReplayContinuation();
                }
                else if (string.Equals(command, "RECORD", StringComparison.Ordinal))
                {
                    if (!IsOperation(PerformanceOperationKinds.Capture) ||
                        m_Status != PerformanceCaptureStatus.WarmingUp || !m_WarmupBoundarySent ||
                        FixedCharacterInputTraceModule.Status.Mode != FixedCharacterInputTraceMode.ReplayPaused)
                    {
                        throw new InvalidOperationException("Performance RECORD arrived before the exact warmup boundary.");
                    }
                    BeginRecording();
                }
                else if (command.StartsWith("TRANSPORT_FAULT|", StringComparison.Ordinal))
                {
                    throw new IOException(command.Substring("TRANSPORT_FAULT|".Length));
                }
                else
                {
                    throw new InvalidDataException($"Unknown performance command '{command}'.");
                }
            }
        }

        void UpdateReadiness()
        {
            if (m_ReadySent || !IsTransportConnected())
                return;
            if (!RuntimeReadyCore())
                return;
            Send("READY");
            m_ReadySent = true;
        }

        void ValidateRecorderAvailability()
        {
            IReadOnlyList<PerformanceMetricDefinition> metrics = ThirdPersonRuntimePerformanceMetricCatalog.All;
            for (int i = 0; i < metrics.Count; i++)
            {
                PerformanceMetricDefinition metric = metrics[i];
                if (!UsesRecorder(metric))
                    continue;
                ProfilerRecorder recorder = ProfilerRecorder.StartNew(
                    Category(metric),
                    metric.ProfilerName,
                    1,
                    ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.SumAllSamplesInFrame);
                bool valid = recorder.Valid;
                recorder.Dispose();
                if (!valid)
                    throw new InvalidOperationException($"Required Profiler metric '{metric.ProfilerName}' is unavailable.");
            }
        }

        bool RuntimeReadyCore()
        {
            GameplayLabBootstrap bootstrap = GameplayLabBootstrap.Current;
            bool runtimeReady = GameplayTickSystem.IsInitialized && bootstrap && bootstrap.SessionHost &&
                                bootstrap.SessionHost.LifecycleState == SimulationSessionLifecycleState.Active &&
                                bootstrap.SessionHost.RegistrationCount == 2 && GameplayTickSystem.Current.RenderFrame > 0;
            if (!runtimeReady || IsOperation(PerformanceOperationKinds.Smoke))
                return runtimeReady;
            FixedCharacterInputTraceStatus trace = FixedCharacterInputTraceModule.Status;
            return trace.Mode == FixedCharacterInputTraceMode.PreparingReplay &&
                   !string.IsNullOrEmpty(trace.StartBodyHash);
        }

        void UpdateWarmup()
        {
            if (m_WarmupBoundarySent)
                return;
            FixedCharacterInputTraceStatus trace = FixedCharacterInputTraceModule.Status;
            if (trace.Mode == FixedCharacterInputTraceMode.Faulted)
                throw new InvalidOperationException(trace.Message);
            if (trace.Mode != FixedCharacterInputTraceMode.ReplayPaused)
                return;
            if (trace.ReplayedFrameCount != m_Scenario.warmup_logic_ticks)
                throw new InvalidOperationException("Performance replay paused outside the exact warmup boundary.");
            if (IsOperation(PerformanceOperationKinds.Capture))
                ValidateRecorderAvailability();
            m_WarmupBoundarySent = true;
            Send("WARMUP_COMPLETED");
        }

        void BeginRecording()
        {
            m_CurrentStage = "recording";
            int renderCapacity = Capacity(
                m_Profile.maximum_presentation_fps,
                m_Scenario.capture_logic_ticks,
                m_Profile.logic_tick_rate,
                m_Profile.sample_capacity_margin_percent);
            PerformanceInstrumentationSpanRuntime.BeginCapture();
            StartRecorders(renderCapacity);
            string profilerPath = Path.Combine(m_Request.staging_root, "unity-profiler.raw");
            Profiler.logFile = profilerPath;
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;
            m_StartedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            m_CaptureStartFrame = Time.frameCount;
            m_CaptureStartedRealtime = Time.realtimeSinceStartup;
            m_DroppedLogicTicksAtStart = GameplayTickSystem.Current.DroppedLocalLogicTicks;
            FixedCharacterInputTraceModule.ResumeReplay();
            m_Status = PerformanceCaptureStatus.Recording;
            Send("RECORDING");
        }

        void BeginReplayContinuation()
        {
            m_CurrentStage = "replay";
            m_StartedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            m_CaptureStartFrame = Time.frameCount;
            m_CaptureStartedRealtime = Time.realtimeSinceStartup;
            m_DroppedLogicTicksAtStart = GameplayTickSystem.Current.DroppedLocalLogicTicks;
            FixedCharacterInputTraceModule.ResumeReplay();
            m_Status = PerformanceCaptureStatus.Replaying;
            Send("REPLAYING");
        }

        void StartRecorders(int capacity)
        {
            ProfilerRecorderOptions options = ProfilerRecorderOptions.StartImmediately |
                                              ProfilerRecorderOptions.SumAllSamplesInFrame;
            IReadOnlyList<PerformanceMetricDefinition> metrics = ThirdPersonRuntimePerformanceMetricCatalog.All;
            for (int i = 0; i < metrics.Count; i++)
            {
                PerformanceMetricDefinition metric = metrics[i];
                if (!UsesRecorder(metric))
                    continue;
                ProfilerRecorder recorder = ProfilerRecorder.StartNew(Category(metric), metric.ProfilerName, capacity, options);
                if (!recorder.Valid)
                {
                    recorder.Dispose();
                    throw new InvalidOperationException($"Required Profiler metric '{metric.ProfilerName}' is unavailable.");
                }
                m_Recorders.Add(new RecorderEntry { Metric = metric, Recorder = recorder, Capacity = capacity });
            }
        }

        static bool UsesRecorder(PerformanceMetricDefinition metric) =>
            metric.SampleScope == PerformanceSampleScope.RenderFrame ||
            metric.SampleScope == PerformanceSampleScope.Counter;

        static ProfilerCategory Category(PerformanceMetricDefinition metric) => metric.MetricId switch
        {
            ThirdPersonRuntimePerformanceMetricCatalog.MainThreadMetricId => ProfilerCategory.Internal,
            ThirdPersonRuntimePerformanceMetricCatalog.GcAllocatedMetricId => ProfilerCategory.Memory,
            _ => ProfilerCategory.Scripts
        };

        void UpdateRecording()
        {
            FixedCharacterInputTraceStatus trace = FixedCharacterInputTraceModule.Status;
            if (trace.Mode == FixedCharacterInputTraceMode.Faulted)
                throw new InvalidOperationException(trace.Message);
            if (PerformanceInstrumentationSpanRuntime.Faulted)
            {
                PerformanceSpanBuffer buffer = PerformanceInstrumentationSpanRuntime.SpanBuffer;
                throw new InvalidOperationException(
                    $"Performance instrumentation Span capture faulted. capacity={buffer?.Capacity ?? 0}, records={buffer?.Count ?? 0}, attempted={buffer?.AttemptedCount ?? 0}, last_sequence={buffer?.LastSequence ?? 0}, last_point={(buffer?.LastPointId ?? 0UL):x16}.");
            }
            if (trace.Mode != FixedCharacterInputTraceMode.Completed)
                return;
            if (trace.ReplayedFrameCount != m_Scenario.warmup_logic_ticks + m_Scenario.capture_logic_ticks)
                throw new InvalidOperationException("Performance input replay completed with an unexpected frame count.");
            CompleteRecording();
        }

        void UpdateReplay()
        {
            FixedCharacterInputTraceStatus trace = FixedCharacterInputTraceModule.Status;
            if (trace.Mode == FixedCharacterInputTraceMode.Faulted)
                throw new InvalidOperationException(trace.Message);
            if (trace.Mode != FixedCharacterInputTraceMode.Completed)
                return;
            if (trace.ReplayedFrameCount != m_Scenario.warmup_logic_ticks + m_Scenario.capture_logic_ticks)
                throw new InvalidOperationException("Performance Replay completed with an unexpected frame count.");
            FixedCharacterInputReplayEvidence evidence = FixedCharacterInputTraceModule.CaptureReplayEvidence();
            WriteRuntimeResult(
                "replay-completed",
                "Performance Replay Gate completed.",
                trace.ReplayedFrameCount,
                evidence.StartBodyHash.ToString(),
                evidence.InputSequenceHash.ToString(),
                evidence.BodyTrajectoryHash.ToString());
            m_Status = PerformanceCaptureStatus.Completed;
            m_CurrentStage = "replay-completed";
            m_CompletionSent = true;
            Send("REPLAY_COMPLETED");
            Shutdown(0);
        }

        void CompleteSmoke()
        {
            m_StartedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            WriteRuntimeResult("smoke-completed", "Performance Smoke Gate completed.", 0, string.Empty, string.Empty, string.Empty);
            m_Status = PerformanceCaptureStatus.Completed;
            m_CurrentStage = "smoke-completed";
            m_CompletionSent = true;
            Send("STOPPED");
            Shutdown(0);
        }

        void UpdateCameraFrame()
        {
            if (!PerformanceCameraInputOverride.IsActive)
                return;
            FixedCharacterInputTraceStatus trace = FixedCharacterInputTraceModule.Status;
            int frame = Mathf.Clamp(trace.ReplayedFrameCount, 0, m_CameraTrace.frame_count - 1);
            if (frame == m_LastCameraFrame && PerformanceCameraInputOverride.IsActive)
                return;
            PerformanceCameraInputOverride.SetFrame(frame);
            m_LastCameraFrame = frame;
        }

        void CompleteRecording()
        {
            string completedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            double captureSeconds = Math.Max(double.Epsilon, Time.realtimeSinceStartup - m_CaptureStartedRealtime);
            int presentationFrames = Time.frameCount - m_CaptureStartFrame;
            int dropped = GameplayTickSystem.Current.DroppedLocalLogicTicks - m_DroppedLogicTicksAtStart;
            m_Status = PerformanceCaptureStatus.Finalizing;
            m_CurrentStage = "finalizing";
            if (PerformanceInstrumentationSpanRuntime.Faulted)
                throw new InvalidOperationException("Performance instrumentation Span capture faulted before finalization.");
            PerformanceSpanRecord[] instrumentationSpans = PerformanceInstrumentationSpanRuntime.EndCapture();
            Profiler.enabled = false;
            Profiler.enableBinaryLog = false;
            Profiler.logFile = string.Empty;
            for (int i = 0; i < m_Recorders.Count; i++)
                m_Recorders[i].Recorder.Stop();
            string instrumentationSpanPath = Path.Combine(m_Request.staging_root, "instrumentation-spans.bin");
            PerformanceInstrumentationSpanFile.Write(instrumentationSpanPath, instrumentationSpans);
            string instrumentationSpanHash = Sha256(instrumentationSpanPath);
            string profilerPath = Path.Combine(m_Request.staging_root, "unity-profiler.raw");
            if (!File.Exists(profilerPath) || new FileInfo(profilerPath).Length == 0L)
                throw new InvalidDataException("Unity binary Profiler capture was not published.");
            int logicTicks = CountMetricSamples(instrumentationSpans, "session.logic-tick");
            if (logicTicks != m_Scenario.capture_logic_ticks)
                throw new InvalidDataException("Performance Session LogicTick Span count does not match the capture scenario.");
            WriteMetricSamples(instrumentationSpans);
            string catalogRevision = ComputeCatalogRevision();
            WriteMetricCatalog(catalogRevision);
            var result = new PerformanceRuntimeResultDocument
            {
                operation = m_Request.operation,
                run_id = m_Request.run_id,
                status = PerformanceCaptureStatus.Completed.ToString(),
                stage = "completed",
                message = "Performance Scenario completed.",
                started_utc = m_StartedUtc,
                completed_utc = completedUtc,
                presentation_frames = presentationFrames,
                logic_ticks = logicTicks,
                dropped_logic_ticks = dropped,
                capture_seconds = captureSeconds,
                metric_catalog_revision = catalogRevision,
                instrumentation_identity = m_InstrumentationIdentity,
                instrumentation_mode = m_InstrumentationMode.ToString(),
                instrumentation_span_layout_revision = PerformanceInstrumentationIdentity.SpanLayoutRevision,
                instrumentation_span_path = "instrumentation-spans.bin",
                instrumentation_span_hash = instrumentationSpanHash,
                operating_system = SystemInfo.operatingSystem,
                processor = SystemInfo.processorType,
                graphics_device = SystemInfo.graphicsDeviceName,
                graphics_api = SystemInfo.graphicsDeviceType.ToString()
            };
            WriteJson(Path.Combine(m_Request.staging_root, "runtime-result.json"), result);
            DisposeRecorders();
            m_Status = PerformanceCaptureStatus.Completed;
            m_CurrentStage = "completed";
            m_CompletionSent = true;
            Send("COMPLETED");
            Shutdown(0);
        }

        void WriteMetricSamples(PerformanceSpanRecord[] instrumentationSpans)
        {
            var builder = new StringBuilder(1024 * 1024);
            builder.AppendLine("metric_id,sample_scope,sample_index,identity,render_frame,value,count");
            for (int recorderIndex = 0; recorderIndex < m_Recorders.Count; recorderIndex++)
            {
                RecorderEntry entry = m_Recorders[recorderIndex];
                ProfilerRecorderSample[] samples = entry.Recorder.ToArray();
                if (samples.Length >= entry.Capacity)
                    throw new InvalidOperationException($"Performance metric '{entry.Metric.MetricId}' exhausted its fixed sample capacity.");
                for (int sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
                {
                    if (samples[sampleIndex].Count <= 0)
                        continue;
                    builder.Append(Csv(entry.Metric.MetricId)).Append(',')
                        .Append(entry.Metric.SampleScope).Append(',')
                        .Append(sampleIndex.ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(sampleIndex.ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(sampleIndex.ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(samples[sampleIndex].Value.ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(samples[sampleIndex].Count.ToString(CultureInfo.InvariantCulture)).AppendLine();
                }
            }
            IReadOnlyList<PerformanceMetricDefinition> metrics = ThirdPersonRuntimePerformanceMetricCatalog.All;
            var metricsByHash = new Dictionary<ulong, PerformanceMetricDefinition>();
            for (int i = 0; i < metrics.Count; i++)
            {
                PerformanceMetricDefinition metric = metrics[i];
                ulong metricHash = PerformanceInstrumentationIdentity.Hash64(metric.MetricId);
                if (!metricsByHash.TryAdd(metricHash, metric))
                    throw new InvalidDataException($"Performance metric '{metric.MetricId}' has a duplicate hash.");
            }
            var logicSamples = new Dictionary<(ulong MetricId, ulong LogicTick), LogicTickSample>();
            for (int i = 0; i < instrumentationSpans.Length; i++)
            {
                PerformanceSpanRecord span = instrumentationSpans[i];
                if (!metricsByHash.TryGetValue(span.MetricId, out PerformanceMetricDefinition metric))
                    throw new InvalidDataException($"Performance instrumentation Span metric hash '{span.MetricId:x16}' is not in the catalog.");
                if (UsesRecorder(metric))
                    continue;
                if (metric.SampleScope == PerformanceSampleScope.LogicTick)
                {
                    if ((span.ContextFlags & PerformanceInstrumentationContextFlags.LogicTick) == 0)
                        throw new InvalidDataException($"Performance metric '{metric.MetricId}' has no LogicTick context.");
                    var key = (span.MetricId, span.LogicTick);
                    if (!logicSamples.TryGetValue(key, out LogicTickSample sample))
                    {
                        sample = new LogicTickSample
                        {
                            Metric = metric,
                            LogicTick = span.LogicTick,
                            RenderFrame = span.RenderFrame
                        };
                        logicSamples.Add(key, sample);
                    }
                    sample.DurationTicks = checked(sample.DurationTicks + span.DurationTicks);
                    sample.Count++;
                    continue;
                }
                double nanoseconds = span.DurationTicks * 1000000000d / Stopwatch.Frequency;
                builder.Append(Csv(metric.MetricId)).Append(',')
                    .Append(metric.SampleScope).Append(',')
                    .Append(i.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(span.PointId.ToString("x16", CultureInfo.InvariantCulture)).Append(',')
                    .Append(span.RenderFrame.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(nanoseconds.ToString(CultureInfo.InvariantCulture)).AppendLine(",1");
            }
            int logicSampleIndex = 0;
            foreach (LogicTickSample sample in logicSamples.Values)
            {
                double nanoseconds = sample.DurationTicks * 1000000000d / Stopwatch.Frequency;
                builder.Append(Csv(sample.Metric.MetricId)).Append(',')
                    .Append(sample.Metric.SampleScope).Append(',')
                    .Append((logicSampleIndex++).ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(sample.LogicTick.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(sample.RenderFrame.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(nanoseconds.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(sample.Count.ToString(CultureInfo.InvariantCulture)).AppendLine();
            }
            File.WriteAllText(
                Path.Combine(m_Request.staging_root, "metric-samples.csv"),
                builder.ToString(),
                new UTF8Encoding(false));
        }

        string ComputeCatalogRevision()
        {
            using SHA256 sha = SHA256.Create();
            var builder = new StringBuilder();
            IReadOnlyList<PerformanceMetricDefinition> metrics = ThirdPersonRuntimePerformanceMetricCatalog.All;
            for (int i = 0; i < metrics.Count; i++)
            {
                PerformanceMetricDefinition metric = metrics[i];
                builder.Append(metric.MetricId).Append('|').Append(metric.ProfilerName).Append('|')
                    .Append(metric.ParentId).Append('|').Append(metric.SampleScope).Append('|')
                    .Append(metric.Unit).Append('|').Append(metric.Aggregation).Append('\n');
            }
            return Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString())));
        }

        void WriteMetricCatalog(string revision)
        {
            IReadOnlyList<PerformanceMetricDefinition> metrics = ThirdPersonRuntimePerformanceMetricCatalog.All;
            var values = new PerformanceMetricDefinitionDocument[metrics.Count];
            for (int i = 0; i < metrics.Count; i++)
            {
                PerformanceMetricDefinition metric = metrics[i];
                values[i] = new PerformanceMetricDefinitionDocument
                {
                    metric_id = metric.MetricId,
                    profiler_name = metric.ProfilerName,
                    domain = metric.Domain.ToString(),
                    parent_id = metric.ParentId,
                    sample_scope = metric.SampleScope.ToString(),
                    unit = metric.Unit.ToString(),
                    aggregation = metric.Aggregation.ToString()
                };
            }
            WriteJson(
                Path.Combine(m_Request.staging_root, "metric-catalog.json"),
                new PerformanceMetricCatalogDocument { revision = revision, metrics = values });
        }

        int CountMetricSamples(
            PerformanceSpanRecord[] instrumentationSpans,
            string metricId)
        {
            ulong metricHash = PerformanceInstrumentationIdentity.Hash64(metricId);
            int count = 0;
            for (int i = 0; i < instrumentationSpans.Length; i++)
            {
                if (instrumentationSpans[i].MetricId == metricHash)
                    count++;
            }
            return count;
        }

        void Fault(string stage, Exception exception)
        {
            if (m_Status == PerformanceCaptureStatus.Faulted || m_ShuttingDown)
                return;
            m_Status = PerformanceCaptureStatus.Faulted;
            m_CurrentStage = stage;
            m_Message = exception.Message;
            CleanupCapture();
            try
            {
                if (m_Request != null && !string.IsNullOrEmpty(m_Request.staging_root))
                    WriteFailureResult(stage, exception.ToString(), PerformanceCaptureStatus.Faulted);
                Send($"FAULT|{stage}|{exception.Message.Replace('|', '/')}");
            }
            catch
            {
            }
            Debug.LogException(exception);
            Shutdown(2);
        }

        void WriteFailureResult(string stage, string message, PerformanceCaptureStatus status)
        {
            WriteJson(
                Path.Combine(m_Request.staging_root, "runtime-result.json"),
                new PerformanceRuntimeResultDocument
                {
                    operation = m_Request.operation,
                    run_id = m_Request.run_id,
                    status = status.ToString(),
                    stage = stage,
                    message = message,
                    started_utc = m_StartedUtc,
                    completed_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                    presentation_frames = m_CaptureStartFrame == 0 ? 0 : Time.frameCount - m_CaptureStartFrame,
                    logic_ticks = FixedCharacterInputTraceModule.Status.ReplayedFrameCount,
                    dropped_logic_ticks = m_CaptureStartFrame != 0 && GameplayTickSystem.IsInitialized
                        ? GameplayTickSystem.Current.DroppedLocalLogicTicks - m_DroppedLogicTicksAtStart
                        : 0,
                    capture_seconds = m_CaptureStartedRealtime <= 0f
                        ? 0d
                        : Math.Max(0d, Time.realtimeSinceStartup - m_CaptureStartedRealtime),
                    metric_catalog_revision = ComputeCatalogRevision(),
                    instrumentation_identity = m_InstrumentationIdentity,
                    instrumentation_mode = m_InstrumentationMode.ToString(),
                    instrumentation_span_layout_revision = PerformanceInstrumentationIdentity.SpanLayoutRevision,
                    operating_system = SystemInfo.operatingSystem,
                    processor = SystemInfo.processorType,
                    graphics_device = SystemInfo.graphicsDeviceName,
                    graphics_api = SystemInfo.graphicsDeviceType.ToString()
                });
        }

        void WriteRuntimeResult(
            string stage,
            string message,
            int logicTicks,
            string startBodyHash,
            string inputSequenceHash,
            string bodyTrajectoryHash)
        {
            WriteJson(
                Path.Combine(m_Request.staging_root, "runtime-result.json"),
                new PerformanceRuntimeResultDocument
                {
                    operation = m_Request.operation,
                    run_id = m_Request.run_id,
                    status = PerformanceCaptureStatus.Completed.ToString(),
                    stage = stage,
                    message = message,
                    started_utc = m_StartedUtc,
                    completed_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                    presentation_frames = m_CaptureStartFrame == 0 ? 0 : Time.frameCount - m_CaptureStartFrame,
                    logic_ticks = logicTicks,
                    dropped_logic_ticks = GameplayTickSystem.IsInitialized
                        ? GameplayTickSystem.Current.DroppedLocalLogicTicks - m_DroppedLogicTicksAtStart
                        : 0,
                    capture_seconds = m_CaptureStartedRealtime <= 0f
                        ? 0d
                        : Math.Max(0d, Time.realtimeSinceStartup - m_CaptureStartedRealtime),
                    operating_system = SystemInfo.operatingSystem,
                    processor = SystemInfo.processorType,
                    graphics_device = SystemInfo.graphicsDeviceName,
                    graphics_api = SystemInfo.graphicsDeviceType.ToString(),
                    instrumentation_identity = m_InstrumentationIdentity,
                    instrumentation_mode = m_InstrumentationMode.ToString(),
                    instrumentation_span_layout_revision = PerformanceInstrumentationIdentity.SpanLayoutRevision,
                    start_body_hash = startBodyHash,
                    input_sequence_hash = inputSequenceHash,
                    body_trajectory_hash = bodyTrajectoryHash
                });
        }

        void CleanupCapture()
        {
            Profiler.enabled = false;
            Profiler.enableBinaryLog = false;
            Profiler.logFile = string.Empty;
            PerformanceInstrumentationSpanRuntime.CancelCapture();
            DisposeRecorders();
            PerformanceCameraInputOverride.Clear();
            FixedCharacterInputTraceModule.Stop();
        }

        void DisposeRecorders()
        {
            for (int i = 0; i < m_Recorders.Count; i++)
                m_Recorders[i].Recorder.Dispose();
            m_Recorders.Clear();
        }

        void Send(string message)
        {
            m_Transport?.Send(message);
        }

        bool IsTransportConnected() => m_Transport != null && m_Transport.IsConnected;

        bool IsOperation(string operation) =>
            string.Equals(m_Request?.operation, operation, StringComparison.Ordinal);

        void Shutdown(int exitCode)
        {
            if (m_ShuttingDown)
                return;
            m_ShuttingDown = true;
            m_PendingExitCode = exitCode;
            if (!m_CompletionSent && exitCode == 0)
                Send("COMPLETED");
            TryCompleteShutdown();
        }

        void TryCompleteShutdown()
        {
            if (m_Transport == null || m_Transport.IsSendIdle)
                Application.Quit(m_PendingExitCode);
        }

        static int Capacity(int rate, int captureLogicTicks, int logicRate, int marginPercent)
        {
            int baseCount = checked((int)Math.Ceiling((double)captureLogicTicks * rate / logicRate));
            int margin = checked((int)Math.Ceiling(baseCount * marginPercent / 100d));
            return checked(baseCount + Math.Max(16, margin));
        }

        static T ReadJson<T>(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException("Performance document is missing.", path);
            T value = JsonUtility.FromJson<T>(File.ReadAllText(path, Encoding.UTF8));
            return value == null ? throw new InvalidDataException($"Performance document '{path}' is invalid.") : value;
        }

        static void WriteJson<T>(string path, T value) =>
            File.WriteAllText(path, JsonUtility.ToJson(value, true), new UTF8Encoding(false));

        static void RequireSchema(string actual, string expected, string name)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidDataException($"Performance {name} schema is invalid.");
        }

        static void RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException($"Performance {name} is missing.");
        }

        static void RequireDirectory(string path, string name)
        {
            RequireText(path, name);
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Performance {name} does not exist: {path}");
        }

        static string ResolvePath(string root, string path) =>
            Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(root, path));

        static void RequireHash(string path, string expected, string name)
        {
            if (!File.Exists(path) || string.IsNullOrWhiteSpace(expected))
                throw new InvalidDataException($"Performance {name} identity is incomplete.");
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            string actual = Hex(sha.ComputeHash(stream));
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidDataException($"Performance {name} hash mismatch.");
        }

        static string Hex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        static string Sha256(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return Hex(sha.ComputeHash(stream));
        }

        static string Csv(string value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";

        static string ReadArgument(string[] arguments, string prefix)
        {
            if (arguments == null)
                return string.Empty;
            string value = string.Empty;
            for (int i = 0; i < arguments.Length; i++)
            {
                if (!arguments[i].StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                if (!string.IsNullOrEmpty(value))
                    throw new InvalidOperationException($"Command line argument '{prefix}' is duplicated.");
                value = arguments[i].Substring(prefix.Length).Trim('"');
            }
            return value;
        }
    }
}
