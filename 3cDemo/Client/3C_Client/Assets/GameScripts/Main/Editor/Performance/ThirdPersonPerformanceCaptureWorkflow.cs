using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ThirdPersonGameplay.Lab;
using ThirdPersonPerformance;
using ThirdPersonPerformance.Editor;
using ThirdPersonPerformance.Instrumentation;
using ThirdPersonPerformance.Instrumentation.Editor;
using ThirdPersonCharacter.Editor.ProductStartup;
using ThirdPersonCharacter.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class ThirdPersonPerformanceCaptureWorkflow
    {
        internal const string FixedVariantId = "gameplay-lab.local-fixed-q32.32";
        const string ScenePath = "Assets/Scenes/GameplayLab/GameplayLab.unity";
        const string CameraLookInputId = "LookAxis";
        const string ToolchainPreference = "ThirdPerson.Performance.ToolchainPath";
        const string ScenarioPreference = "ThirdPerson.Performance.ScenarioPath";
        const string PlayerPreference = "ThirdPerson.Performance.PlayerManifestPath";
        const string BaselinePreference = "ThirdPerson.Performance.BaselineManifestPath";
        const string SmokePreference = "ThirdPerson.Performance.LastSmokeManifestPath";
        const string ReplayPreference = "ThirdPerson.Performance.LastReplayManifestPath";
        const string CapturePreference = "ThirdPerson.Performance.LastCaptureManifestPath";
        const string ActiveRunPreference = "ThirdPerson.Performance.ActiveRunId";
        const string ActiveOperationPreference = "ThirdPerson.Performance.ActiveOperation";
        const string ActiveCancelPreference = "ThirdPerson.Performance.ActiveCancelPath";
        const string ActiveStagingPreference = "ThirdPerson.Performance.ActiveStagingPath";

        [Serializable]
        sealed class FixedInputTraceDocument
        {
            public string schema = string.Empty;
            public string trace_id = string.Empty;
            public string actor_id = string.Empty;
            public int tick_rate = 0;
            public int frame_count = 0;
            public FixedInputTraceFrameDocument[] frames = Array.Empty<FixedInputTraceFrameDocument>();
        }

        [Serializable]
        sealed class FixedInputTraceFrameDocument
        {
        }

        public static string ToolchainPath => ProjectEditorPreferences.GetString(ToolchainPreference, DefaultToolchainPath);
        public static string ScenarioPath => ProjectEditorPreferences.GetString(ScenarioPreference, string.Empty);
        public static string BudgetPath => string.IsNullOrEmpty(ScenarioPath) ? string.Empty : Path.Combine(Path.GetDirectoryName(ScenarioPath), "budget.json");
        public static string CaptureProfilePath => string.IsNullOrEmpty(ScenarioPath) ? string.Empty : Path.Combine(Path.GetDirectoryName(ScenarioPath), "capture-profile.json");
        public static string PlayerManifestPath => ProjectEditorPreferences.GetString(PlayerPreference, string.Empty);
        public static string BaselineManifestPath => ProjectEditorPreferences.GetString(BaselinePreference, string.Empty);
        public static string LastSmokeManifestPath => ProjectEditorPreferences.GetString(SmokePreference, string.Empty);
        public static string LastReplayManifestPath => ProjectEditorPreferences.GetString(ReplayPreference, string.Empty);
        public static string LastCaptureManifestPath => ProjectEditorPreferences.GetString(CapturePreference, string.Empty);
        public static bool SmokeGateReady => IsGateReady(LastSmokeManifestPath, PerformanceOperationKinds.Smoke);
        public static bool ReplayGateReady => IsGateReady(LastReplayManifestPath, PerformanceOperationKinds.Replay);

        public static string Status
        {
            get
            {
                if (IsRunRunning)
                {
                    string activeStatusPath = Path.Combine(ProjectEditorPreferences.GetString(ActiveStagingPreference, string.Empty), "status.json");
                    if (File.Exists(activeStatusPath))
                    {
                        PerformanceRunStatusDocument status = ReadJson<PerformanceRunStatusDocument>(activeStatusPath);
                        return $"{status.operation} {status.status}: {status.run_id} / {status.stage}";
                    }
                    return $"Starting {ProjectEditorPreferences.GetString(ActiveOperationPreference, string.Empty)} {ProjectEditorPreferences.GetString(ActiveRunPreference, string.Empty)}";
                }
                if (File.Exists(LastCaptureManifestPath))
                {
                    PerformanceCaptureManifestDocument manifest = ReadJson<PerformanceCaptureManifestDocument>(LastCaptureManifestPath);
                    return $"{manifest.status}: {manifest.capture_id} / {manifest.stage}";
                }
                if (File.Exists(LastReplayManifestPath))
                {
                    PerformanceGateManifestDocument gate = ReadJson<PerformanceGateManifestDocument>(LastReplayManifestPath);
                    return $"{gate.operation} {gate.status}: {gate.run_id} / {gate.stage}";
                }
                if (File.Exists(LastSmokeManifestPath))
                {
                    PerformanceGateManifestDocument gate = ReadJson<PerformanceGateManifestDocument>(LastSmokeManifestPath);
                    return $"{gate.operation} {gate.status}: {gate.run_id} / {gate.stage}";
                }
                if (!string.IsNullOrEmpty(ProjectEditorPreferences.GetString(ActiveRunPreference, string.Empty)))
                    return "Controller exited before publishing a run manifest.";
                return "Idle";
            }
        }

        public static bool IsRunRunning
        {
            get
            {
                string staging = ProjectEditorPreferences.GetString(ActiveStagingPreference, string.Empty);
                if (string.IsNullOrWhiteSpace(staging) || !Directory.Exists(staging))
                    return false;
                try
                {
                    string statusPath = Path.Combine(staging, "status.json");
                    if (!File.Exists(statusPath))
                        return true;
                    PerformanceRunStatusDocument status = ReadJson<PerformanceRunStatusDocument>(statusPath);
                    if (!IsTerminalStatus(status.status))
                        return true;
                    string manifestPath = ProjectEditorPreferences.GetString(ManifestPreference(status.operation), string.Empty);
                    return !File.Exists(manifestPath);
                }
                catch
                {
                    return false;
                }
            }
        }

        static string ClientRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static string RepositoryRoot => Path.GetFullPath(Path.Combine(ClientRoot, "..", "..", ".."));
        internal static string PerformanceRoot => Path.Combine(ClientRoot, "Library", "Performance");
        internal static string PerformanceBuildWorkspaceRoot =>
            Path.Combine(RepositoryRoot, ".performance-build");
        static string DefaultToolchainPath => Path.Combine(PerformanceRoot, "Toolchain", "windows-wpr-cpu.json");
        static string ControllerProjectPath => Path.Combine(RepositoryRoot, "Tools", "ThirdPersonPerformanceCapture", "ThirdPersonPerformanceCapture.Controller.csproj");
        static string ControllerExecutablePath => Path.Combine(RepositoryRoot, "Tools", "ThirdPersonPerformanceCapture", "bin", "Release", "net8.0-windows", "ThirdPersonPerformanceCapture.Controller.exe");

        public static void ConfigureToolchain()
        {
            string toolkit = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Windows Kits",
                "10",
                "Windows Performance Toolkit");
            string wpr = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "wpr.exe");
            string xperf = Path.Combine(toolkit, "xperf.exe");
            string exporter = Path.Combine(toolkit, "wpaexporter.exe");
            string wpa = Path.Combine(toolkit, "wpa.exe");
            string wprProfile = Path.Combine(RepositoryRoot, "Tools", "ThirdPersonPerformanceCapture", "Profiles", "ThirdPersonCpu.wprp");
            string wpaProfile = Path.Combine(RepositoryRoot, "Tools", "ThirdPersonPerformanceCapture", "Profiles", "ThirdPersonCpu.wpaProfile");
            RequireFile(wpr, "WPR");
            RequireFile(xperf, "Xperf");
            RequireFile(exporter, "WPA Exporter");
            RequireFile(wpa, "WPA");
            RequireFile(EditorApplication.applicationPath, "Unity Profiler host");
            RequireFile(wprProfile, "project WPR profile");
            RequireFile(wpaProfile, "project WPA profile");
            string symbols = Path.Combine(PerformanceRoot, "SymbolCache");
            Directory.CreateDirectory(symbols);
            var toolchain = new PerformanceToolchainDocument
            {
                wpr_path = Path.GetFullPath(wpr),
                xperf_path = Path.GetFullPath(xperf),
                wpa_exporter_path = Path.GetFullPath(exporter),
                wpa_path = Path.GetFullPath(wpa),
                unity_profiler_path = Path.GetFullPath(EditorApplication.applicationPath),
                unity_profiler_open_mode = "current-editor-load-profile",
                symbol_cache_path = Path.GetFullPath(symbols),
                wpr_profile_path = Path.GetFullPath(wprProfile),
                wpa_profile_path = Path.GetFullPath(wpaProfile),
                wpr_profile_hash = Sha256(wprProfile),
                wpa_profile_hash = Sha256(wpaProfile),
                wpr_version = VersionOf(wpr),
                xperf_version = VersionOf(xperf),
                wpa_exporter_version = VersionOf(exporter),
                wpa_version = VersionOf(wpa),
                unity_version = VersionOf(EditorApplication.applicationPath)
            };
            Directory.CreateDirectory(Path.GetDirectoryName(DefaultToolchainPath));
            WriteJson(DefaultToolchainPath, toolchain);
            ProjectEditorPreferences.SetString(ToolchainPreference, DefaultToolchainPath);
            Debug.Log($"Performance Toolchain configured: {DefaultToolchainPath}");
        }

        public static bool TryValidateToolchain(out string message)
        {
            try
            {
                PerformanceToolchainDocument toolchain = ReadJson<PerformanceToolchainDocument>(ToolchainPath);
                RequireSchema(toolchain.schema, PerformanceCaptureSchemas.Toolchain, "toolchain");
                RequireVersion(toolchain.wpr_path, toolchain.wpr_version, "WPR");
                RequireVersion(toolchain.xperf_path, toolchain.xperf_version, "Xperf");
                RequireVersion(toolchain.wpa_exporter_path, toolchain.wpa_exporter_version, "WPA Exporter");
                RequireVersion(toolchain.wpa_path, toolchain.wpa_version, "WPA");
                RequireVersion(toolchain.unity_profiler_path, toolchain.unity_version, "Unity Profiler host");
                if (!string.Equals(toolchain.unity_profiler_open_mode, "current-editor-load-profile", StringComparison.Ordinal))
                    throw new InvalidDataException("Unity Profiler open mode is unsupported.");
                RequireFile(toolchain.wpr_profile_path, "WPR profile");
                RequireFile(toolchain.wpa_profile_path, "WPA profile");
                if (!string.Equals(Sha256(toolchain.wpr_profile_path), toolchain.wpr_profile_hash, StringComparison.Ordinal) ||
                    !string.Equals(Sha256(toolchain.wpa_profile_path), toolchain.wpa_profile_hash, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("WPR or WPA profile hash does not match the applied Toolchain Definition.");
                }
                if (!Directory.Exists(toolchain.symbol_cache_path))
                    throw new DirectoryNotFoundException($"Symbol cache is missing: {toolchain.symbol_cache_path}");
                message = "Configured";
                return true;
            }
            catch (Exception exception)
            {
                message = exception.Message;
                return false;
            }
        }

        public static void PublishScenario(string variantId, string tracePath)
        {
            if (!string.Equals(variantId, FixedVariantId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Performance Scenario currently requires the formal Fixed Variant '{FixedVariantId}'.");
            RequireFile(tracePath, "Fixed Input Trace");
            FixedInputTraceDocument input = ReadJson<FixedInputTraceDocument>(tracePath);
            if (string.IsNullOrWhiteSpace(input.trace_id) || string.IsNullOrWhiteSpace(input.actor_id) ||
                input.tick_rate <= 0 || input.frame_count <= 0 || input.frames == null || input.frames.Length != input.frame_count)
            {
                throw new InvalidDataException("Fixed Input Trace identity or frame closure is invalid.");
            }
            RequireVariant(variantId).RequireComplete();
            const int warmupLogicTicks = 180;
            if (input.frame_count <= warmupLogicTicks)
                throw new InvalidDataException("Fixed Input Trace must contain both warmup and capture LogicTicks.");
            string scenarioId = $"performance.{input.trace_id}.fixed.r3";
            string root = Path.Combine(PerformanceRoot, "Scenarios", scenarioId);
            string scenarioPath = Path.Combine(root, "scenario.json");
            if (Directory.Exists(root))
            {
                PerformanceScenarioDocument existing = ReadJson<PerformanceScenarioDocument>(scenarioPath);
                if (!string.Equals(existing.variant_id, variantId, StringComparison.Ordinal) ||
                    !string.Equals(existing.input_trace_hash, Sha256(tracePath), StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Published Performance Scenario '{scenarioId}' already exists with another identity.");
                }
                ProjectEditorPreferences.SetString(ScenarioPreference, scenarioPath);
                return;
            }
            Directory.CreateDirectory(root);
            string inputPath = Path.Combine(root, "input-trace.json");
            File.Copy(tracePath, inputPath, false);
            var camera = new PerformanceCameraTraceDocument
            {
                trace_id = $"camera.{input.trace_id}.fixed.r2",
                revision = 2,
                input_id = CameraLookInputId,
                tick_rate = input.tick_rate,
                frame_count = input.frame_count,
                segments = new[]
                {
                    new PerformanceCameraTraceSegmentDocument
                    {
                        first_frame = 0,
                        frame_count = input.frame_count,
                        x = 0f,
                        y = 0f
                    }
                }
            };
            camera.content_hash = PerformanceCaptureIdentity.CameraTrace(camera);
            string cameraPath = Path.Combine(root, "camera-trace.json");
            WriteJson(cameraPath, camera);
            var profile = new PerformanceCaptureProfileDocument
            {
                profile_id = "gameplay-cpu-standard",
                revision = 3,
                maximum_presentation_fps = 120,
                logic_tick_rate = input.tick_rate,
                sample_capacity_margin_percent = 25,
                instrumentation_span_capacity = 262144,
                runtime_ready_timeout_seconds = 90,
                capture_timeout_seconds = Math.Max(120, (int)Math.Ceiling((double)input.frame_count / input.tick_rate) + 60)
            };
            profile.content_hash = PerformanceCaptureIdentity.Profile(profile);
            string profilePath = Path.Combine(root, "capture-profile.json");
            WriteJson(profilePath, profile);
            var budget = new PerformanceBudgetDocument
            {
                budget_id = "gameplay-client-60fps",
                revision = 1,
                target_fps = 60d,
                maximum_dropped_logic_ticks = 0,
                metrics = new[]
                {
                    new PerformanceMetricBudgetDocument { metric_id = "unity.main-thread", p95_limit = 16.667d, p99_limit = 25d, unit = "Milliseconds" },
                    new PerformanceMetricBudgetDocument { metric_id = "unity.gc-allocated-in-frame", p95_limit = 32768d, p99_limit = 65536d, unit = "Bytes" },
                    new PerformanceMetricBudgetDocument { metric_id = "session.logic-tick", p95_limit = 8d, p99_limit = 12d, unit = "Milliseconds" },
                    new PerformanceMetricBudgetDocument { metric_id = "gameplay.presentation", p95_limit = 8d, p99_limit = 12d, unit = "Milliseconds" }
                }
            };
            budget.content_hash = PerformanceCaptureIdentity.Budget(budget);
            string budgetPath = Path.Combine(root, "budget.json");
            WriteJson(budgetPath, budget);
            var scenario = new PerformanceScenarioDocument
            {
                scenario_id = scenarioId,
                revision = 1,
                scene_path = ScenePath,
                variant_id = variantId,
                actor_id = input.actor_id,
                roster_identity = "gameplay-lab-player|gameplay-lab-target",
                ready_condition = "gameplay-lab.session-active+locked-roster+fixed-start-body+metric-catalog-registered",
                capture_start_boundary = "after-fixed-input-warmup",
                capture_end_boundary = "fixed-input-replay-completed",
                input_trace_path = Path.GetFileName(inputPath),
                input_trace_hash = Sha256(inputPath),
                camera_trace_path = Path.GetFileName(cameraPath),
                camera_trace_hash = Sha256(cameraPath),
                warmup_logic_ticks = warmupLogicTicks,
                capture_logic_ticks = input.frame_count - warmupLogicTicks,
                width = 1920,
                height = 1080,
                quality_level = QualitySettings.GetQualityLevel(),
                v_sync_count = 0,
                target_frame_rate = 120
            };
            scenario.content_hash = PerformanceCaptureIdentity.Scenario(scenario);
            WriteJson(scenarioPath, scenario);
            ProjectEditorPreferences.SetString(ScenarioPreference, scenarioPath);
            Debug.Log($"Performance Scenario published: {scenarioPath}");
        }

        public static void BuildPlayer(
            string selectedVariantId,
            PerformanceInstrumentationMode instrumentationMode) =>
            BuildPlayer(selectedVariantId, string.Empty, instrumentationMode);

        internal static void BuildPlayer(
            string selectedVariantId,
            string workspaceIdentity,
            PerformanceInstrumentationMode instrumentationMode)
        {
            PerformanceScenarioDocument scenario = RequireScenario();
            if (!string.Equals(scenario.variant_id, selectedVariantId, StringComparison.Ordinal))
                throw new InvalidOperationException("Selected Launcher Variant does not match the published Performance Scenario.");
            GameplayLabSessionVariantDefinition variant = RequireVariant(scenario.variant_id);
            variant.RequireComplete();
            PerformanceInstrumentationBuildInput instrumentationInput =
                ThirdPersonPerformanceInstrumentationCatalog.CreateBuildInput(
                    instrumentationMode,
                    ThirdPersonPerformanceInstrumentationCatalog.AssemblyNames);
            PerformanceBuildIdentity buildIdentity = CaptureBuildIdentity(
                variant,
                instrumentationMode,
                instrumentationInput.Identity);
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
                throw new FileNotFoundException("Gameplay Lab scene is missing.", ScenePath);
            string workspaceId = string.IsNullOrWhiteSpace(workspaceIdentity)
                ? Guid.NewGuid().ToString("N")
                : workspaceIdentity;
            if (!Guid.TryParseExact(workspaceId, "N", out _))
                throw new InvalidDataException("Performance Build workspace identity must be a canonical job id.");
            string buildWorkspaceRoot = PerformanceBuildWorkspaceRoot;
            string candidate = Path.Combine(buildWorkspaceRoot, workspaceId);
            string stageRoot = Path.Combine(PerformanceRoot, "Players", ".staging");
            string stage = Path.Combine(stageRoot, workspaceId);
            string instrumentationRoot = Path.Combine(candidate, "Instrumentation");
            string instrumentationInputPath = Path.Combine(instrumentationRoot, "build-input.txt");
            string instrumentationManifestDirectory = Path.Combine(instrumentationRoot, "Fragments");
            Directory.CreateDirectory(candidate);
            string executable = Path.Combine(candidate, "ThirdPersonPerformancePlayer.exe");
            ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone);
            try
            {
                ThirdPersonPerformanceInstrumentationCatalog.WriteBuildInput(
                    instrumentationInputPath,
                    instrumentationManifestDirectory,
                    instrumentationMode,
                    ThirdPersonPerformanceInstrumentationCatalog.AssemblyNames);
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = executable,
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.Development | BuildOptions.StrictMode,
                    extraScriptingDefines = PerformanceInstrumentationBuildInput.CreateBuildDefines(instrumentationInputPath)
                };
                BuildReport report;
                using (ProductBuildValidationContext.Enter(ProductBuildKind.PerformancePlayer))
                    report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                    throw new InvalidOperationException($"Performance Player build failed: {report.summary.result}");
                RequireFile(executable, "Performance Player executable");
                PublishNativeSymbols(candidate, executable);
                string[] pdbs = Directory.GetFiles(candidate, "*.pdb", SearchOption.AllDirectories);
                if (!pdbs.Any(path => Path.GetFileName(path).Contains("GameAssembly", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException("Performance Player build is missing the native GameAssembly PDB.");
                if (!pdbs.Any(path => path.Contains("burst", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException("Performance Player build is missing Burst debug symbols.");
                string instrumentationManifestPath = Path.Combine(
                    instrumentationRoot,
                    "instrumentation-manifest.json");
                string instrumentationIdentity = PerformanceInstrumentationManifestIndex.Write(
                    instrumentationManifestDirectory,
                    instrumentationManifestPath,
                    instrumentationInput);
                buildIdentity = buildIdentity.WithInstrumentationIdentity(instrumentationIdentity);
                string scenarioRoot = Path.Combine(candidate, "Scenario");
                CopyScenarioClosure(Path.GetDirectoryName(ScenarioPath), scenarioRoot);
                PerformanceFileDocument[] files = BuildClosure(candidate);
                string closureIdentity = string.Join("\n", files.Select(value =>
                    $"{value.role}|{value.path}|{value.size.ToString(CultureInfo.InvariantCulture)}|{value.sha256}"));
                string buildSeed = string.Join("|", new[]
                {
                    Application.unityVersion,
                    BuildTarget.StandaloneWindows64.ToString(),
                    ScriptingImplementation.IL2CPP.ToString(),
                    "Development",
                    scenario.content_hash,
                    buildIdentity.ProgramIdentity,
                    buildIdentity.PipelineIdentity,
                    buildIdentity.ProjectionIdentity,
                    buildIdentity.SolverIdentity,
                    buildIdentity.InstrumentationIdentity,
                    buildIdentity.InstrumentationMode,
                    closureIdentity
                });
                string buildId = Sha256(Encoding.UTF8.GetBytes(buildSeed)).Substring(0, 24);
                string manifestPath = Path.Combine(candidate, "player-manifest.json");
                var manifest = new PerformancePlayerManifestDocument
                {
                    build_id = buildId,
                    unity_version = Application.unityVersion,
                    build_target = BuildTarget.StandaloneWindows64.ToString(),
                    scripting_backend = ScriptingImplementation.IL2CPP.ToString(),
                    build_mode = "Development",
                    scene_path = ScenePath,
                    executable_path = RelativePath(candidate, executable),
                    scenario_catalog_path = RelativePath(candidate, Path.Combine(scenarioRoot, "scenario.json")),
                    program_identity = buildIdentity.ProgramIdentity,
                    pipeline_identity = buildIdentity.PipelineIdentity,
                    projection_identity = buildIdentity.ProjectionIdentity,
                    solver_identity = buildIdentity.SolverIdentity,
                    instrumentation_identity = buildIdentity.InstrumentationIdentity,
                    instrumentation_mode = buildIdentity.InstrumentationMode,
                    instrumentation_span_layout_revision = PerformanceInstrumentationIdentity.SpanLayoutRevision,
                    instrumentation_manifest_path = RelativePath(candidate, instrumentationManifestPath),
                    files = files
                };
                WriteJson(manifestPath, manifest);
                string destination = Path.Combine(PerformanceRoot, "Players", buildId);
                if (Directory.Exists(destination))
                    throw new IOException($"Performance Player '{buildId}' is already published.");
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                Directory.CreateDirectory(stageRoot);
                Directory.Move(candidate, stage);
                Directory.Move(stage, destination);
                string publishedManifest = Path.Combine(destination, "player-manifest.json");
                ProjectEditorPreferences.SetString(PlayerPreference, publishedManifest);
                Debug.Log($"Performance Player published: {publishedManifest}");
            }
            catch
            {
                DeleteOwnedStage(candidate, buildWorkspaceRoot);
                DeleteOwnedStage(stage, stageRoot);
                throw;
            }
            finally
            {
                if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone) != backend)
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, backend);
            }
        }

        public static void StartCapture()
        {
            StartRun(PerformanceOperationKinds.Capture);
        }

        public static void StartSmoke()
        {
            StartRun(PerformanceOperationKinds.Smoke);
        }

        public static void StartReplay()
        {
            StartRun(PerformanceOperationKinds.Replay);
        }

        static void StartRun(string operation)
        {
            if (IsRunRunning)
                throw new InvalidOperationException("A Performance run owned by this Launcher is already active.");
            if (!string.Equals(operation, PerformanceOperationKinds.Smoke, StringComparison.Ordinal) &&
                !string.Equals(operation, PerformanceOperationKinds.Replay, StringComparison.Ordinal) &&
                !string.Equals(operation, PerformanceOperationKinds.Capture, StringComparison.Ordinal))
            {
                throw new ArgumentOutOfRangeException(nameof(operation));
            }
            RequireFile(ToolchainPath, "Performance Toolchain");
            if (string.Equals(operation, PerformanceOperationKinds.Capture, StringComparison.Ordinal) &&
                !TryValidateToolchain(out string toolchainError))
            {
                throw new InvalidOperationException(toolchainError);
            }
            PerformanceScenarioDocument scenario = RequireScenario();
            PerformancePlayerManifestDocument player = ReadJson<PerformancePlayerManifestDocument>(PlayerManifestPath);
            RequireSchema(player.schema, PerformanceCaptureSchemas.Player, "player manifest");
            if (string.IsNullOrWhiteSpace(player.instrumentation_identity) ||
                (string.Equals(operation, PerformanceOperationKinds.Capture, StringComparison.Ordinal) &&
                 !string.Equals(player.instrumentation_mode, PerformanceInstrumentationMode.Span.ToString(), StringComparison.Ordinal)))
            {
                throw new InvalidDataException("Performance Capture requires a Span instrumentation Player.");
            }
            string playerScenario = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(PlayerManifestPath), player.scenario_catalog_path));
            if (!File.Exists(playerScenario) || !string.Equals(Sha256(playerScenario), Sha256(ScenarioPath), StringComparison.Ordinal))
                throw new InvalidDataException("Performance Player Scenario catalog does not match the selected Scenario artifact.");
            BuildController();
            RequireFile(ControllerExecutablePath, "Performance Controller executable");
            string runId = $"{operation}.{DateTime.UtcNow:yyyyMMdd-HHmmss}.{Guid.NewGuid():N}";
            string runRoot = string.Equals(operation, PerformanceOperationKinds.Capture, StringComparison.Ordinal)
                ? Path.Combine(PerformanceRoot, "Captures")
                : Path.Combine(PerformanceRoot, "Gates", operation);
            string staging = Path.Combine(runRoot, ".staging", runId);
            string result = Path.Combine(runRoot, runId);
            Directory.CreateDirectory(staging);
            string cancelPath = Path.Combine(staging, "cancel.request");
            string statusPath = Path.Combine(staging, "status.json");
            string requestPath = Path.Combine(staging, "request.json");
            string smokeManifest = string.Equals(operation, PerformanceOperationKinds.Smoke, StringComparison.Ordinal)
                ? string.Empty
                : RequireGate(LastSmokeManifestPath, PerformanceOperationKinds.Smoke);
            string replayManifest = string.Equals(operation, PerformanceOperationKinds.Capture, StringComparison.Ordinal)
                ? RequireGate(LastReplayManifestPath, PerformanceOperationKinds.Replay)
                : string.Empty;
            var request = new PerformanceRunRequestDocument
            {
                operation = operation,
                run_id = runId,
                request_document_path = Path.GetFullPath(requestPath),
                transport = PerformanceCaptureSchemas.TransportId,
                staging_root = Path.GetFullPath(staging),
                result_root = Path.GetFullPath(result),
                player_manifest_path = Path.GetFullPath(PlayerManifestPath),
                scenario_path = Path.GetFullPath(ScenarioPath),
                scenario_hash = Sha256(ScenarioPath),
                budget_path = BudgetPath,
                budget_hash = Sha256(BudgetPath),
                profile_path = CaptureProfilePath,
                profile_hash = Sha256(CaptureProfilePath),
                toolchain_path = Path.GetFullPath(ToolchainPath),
                smoke_manifest_path = smokeManifest,
                smoke_manifest_hash = string.IsNullOrEmpty(smokeManifest) ? string.Empty : Sha256(smokeManifest),
                replay_manifest_path = replayManifest,
                replay_manifest_hash = string.IsNullOrEmpty(replayManifest) ? string.Empty : Sha256(replayManifest),
                baseline_manifest_path = string.IsNullOrEmpty(BaselineManifestPath) ? string.Empty : Path.GetFullPath(BaselineManifestPath),
                instrumentation_identity = player.instrumentation_identity,
                instrumentation_mode = player.instrumentation_mode,
                cancel_path = Path.GetFullPath(cancelPath),
                status_path = Path.GetFullPath(statusPath)
            };
            WriteJson(requestPath, request);
            WriteJson(statusPath, new PerformanceRunStatusDocument
            {
                operation = operation,
                run_id = runId,
                status = PerformanceCaptureStatus.Pending.ToString(),
                stage = "scheduled",
                message = "Performance run scheduled.",
                updated_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            });
            var start = new ProcessStartInfo
            {
                FileName = ControllerExecutablePath,
                Arguments = $"--request=\"{requestPath}\"",
                WorkingDirectory = RepositoryRoot,
                UseShellExecute = string.Equals(operation, PerformanceOperationKinds.Capture, StringComparison.Ordinal),
                CreateNoWindow = !string.Equals(operation, PerformanceOperationKinds.Capture, StringComparison.Ordinal),
                WindowStyle = ProcessWindowStyle.Hidden
            };
            if (start.UseShellExecute)
                start.Verb = "runas";
            try
            {
                Process process = Process.Start(start) ?? throw new InvalidOperationException("Performance Controller did not start.");
                ProjectEditorPreferences.SetString(ActiveRunPreference, runId);
                ProjectEditorPreferences.SetString(ActiveOperationPreference, operation);
                ProjectEditorPreferences.SetString(ActiveCancelPreference, cancelPath);
                ProjectEditorPreferences.SetString(ActiveStagingPreference, staging);
                ProjectEditorPreferences.SetString(ManifestPreference(operation), Path.Combine(result, "manifest.json"));
                process.Dispose();
                Debug.Log($"Performance {operation} started: {runId}");
            }
            catch
            {
                DeleteOwnedStage(staging, Path.Combine(runRoot, ".staging"));
                throw;
            }
        }

        public static void CancelOwnedRun()
        {
            if (!IsRunRunning)
                throw new InvalidOperationException("The Launcher has no active owned Performance run.");
            string runId = ProjectEditorPreferences.GetString(ActiveRunPreference, string.Empty);
            string cancelPath = ProjectEditorPreferences.GetString(ActiveCancelPreference, string.Empty);
            string staging = ProjectEditorPreferences.GetString(ActiveStagingPreference, string.Empty);
            if (string.IsNullOrEmpty(runId) || string.IsNullOrEmpty(cancelPath) ||
                !string.Equals(Path.GetDirectoryName(Path.GetFullPath(cancelPath)), Path.GetFullPath(staging), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Owned Performance run cancellation identity is invalid.");
            }
            File.WriteAllText(cancelPath, runId, new UTF8Encoding(false));
        }

        public static void SelectBaseline()
        {
            string path = EditorUtility.OpenFilePanel("Select Completed Performance Capture manifest", Path.Combine(PerformanceRoot, "Captures"), "json");
            if (string.IsNullOrEmpty(path))
                return;
            SelectBaseline(path);
        }

        public static void SelectBaseline(string path)
        {
            PerformanceCaptureManifestDocument manifest = ReadJson<PerformanceCaptureManifestDocument>(path);
            RequireSchema(manifest.schema, PerformanceCaptureSchemas.Manifest, "capture manifest");
            if (!string.Equals(manifest.status, PerformanceCaptureStatus.Completed.ToString(), StringComparison.Ordinal))
                throw new InvalidDataException("Baseline Capture must be Completed.");
            ProjectEditorPreferences.SetString(BaselinePreference, Path.GetFullPath(path));
        }

        public static void ClearBaseline()
        {
            ProjectEditorPreferences.DeleteKey(BaselinePreference);
        }

        public static void OpenSummary()
        {
            OpenFile(RequireCaptureFile("summary"));
        }

        public static void OpenComparison()
        {
            OpenFile(RequireCaptureFile("comparison"));
        }

        public static void OpenUnityProfiler()
        {
            _ = RequireCaptureToolchain();
            string path = RequireCaptureFile("unity-profiler");
            if (!ProfilerDriver.LoadProfile(path, false))
                throw new InvalidOperationException("Unity Profiler could not load the raw Capture.");
            EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
        }

        public static void OpenWpa()
        {
            PerformanceToolchainDocument toolchain = RequireCaptureToolchain();
            string etl = RequireCaptureFile("windows-cpu");
            Process.Start(new ProcessStartInfo
            {
                FileName = toolchain.wpa_path,
                Arguments = $"\"{etl}\" -profile \"{toolchain.wpa_profile_path}\"",
                UseShellExecute = true
            })?.Dispose();
        }

        public static void RevealLastCapture()
        {
            EditorUtility.RevealInFinder(RequireLastCaptureRoot());
        }

        static string RequireGate(string manifestPath, string operation)
        {
            RequireFile(manifestPath, $"Performance {operation} Gate manifest");
            PerformanceGateManifestDocument gate = ReadJson<PerformanceGateManifestDocument>(manifestPath);
            PerformanceScenarioDocument scenario = RequireScenario();
            PerformancePlayerManifestDocument player = ReadJson<PerformancePlayerManifestDocument>(PlayerManifestPath);
            if (!string.Equals(gate.schema, PerformanceCaptureSchemas.Gate, StringComparison.Ordinal) ||
                !string.Equals(gate.operation, operation, StringComparison.Ordinal) ||
                !string.Equals(gate.status, PerformanceCaptureStatus.Completed.ToString(), StringComparison.Ordinal) ||
                !string.Equals(gate.scenario_hash, scenario.content_hash, StringComparison.Ordinal) ||
                !string.Equals(gate.build_id, player.build_id, StringComparison.Ordinal) ||
                !string.Equals(gate.instrumentation_identity, player.instrumentation_identity, StringComparison.Ordinal) ||
                !string.Equals(gate.instrumentation_mode, player.instrumentation_mode, StringComparison.Ordinal) ||
                gate.instrumentation_span_layout_revision != player.instrumentation_span_layout_revision ||
                !string.Equals(gate.player_manifest_hash, Sha256(PlayerManifestPath), StringComparison.Ordinal) ||
                !string.Equals(gate.profile_hash, Sha256(CaptureProfilePath), StringComparison.Ordinal) ||
                !string.Equals(gate.input_trace_hash, scenario.input_trace_hash, StringComparison.Ordinal) ||
                !string.Equals(gate.camera_trace_hash, scenario.camera_trace_hash, StringComparison.Ordinal) ||
                gate.warmup_logic_ticks != scenario.warmup_logic_ticks ||
                gate.capture_logic_ticks != scenario.capture_logic_ticks)
            {
                throw new InvalidDataException($"Performance {operation} Gate does not match the selected Scenario and Player.");
            }
            if (operation == PerformanceOperationKinds.Replay &&
                !string.Equals(gate.predecessor_gate_hash, Sha256(LastSmokeManifestPath), StringComparison.Ordinal))
            {
                throw new InvalidDataException("Performance Replay Gate was not produced from the selected Smoke Gate.");
            }
            string root = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
            string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            PerformanceFileDocument[] files = gate.files ?? Array.Empty<PerformanceFileDocument>();
            if (files.Length == 0)
                throw new InvalidDataException($"Performance {operation} Gate closure is empty.");
            for (int i = 0; i < files.Length; i++)
            {
                PerformanceFileDocument file = files[i] ??
                    throw new InvalidDataException($"Performance {operation} Gate contains a null file.");
                string path = Path.GetFullPath(Path.Combine(root, file.path));
                if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(path) ||
                    new FileInfo(path).Length != file.size ||
                    !string.Equals(Sha256(path), file.sha256, StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Performance {operation} Gate file '{file.path}' identity mismatch.");
                }
            }
            return Path.GetFullPath(manifestPath);
        }

        static bool IsGateReady(string manifestPath, string operation)
        {
            try
            {
                _ = RequireGate(manifestPath, operation);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static string ManifestPreference(string operation)
        {
            if (string.Equals(operation, PerformanceOperationKinds.Smoke, StringComparison.Ordinal))
                return SmokePreference;
            if (string.Equals(operation, PerformanceOperationKinds.Replay, StringComparison.Ordinal))
                return ReplayPreference;
            if (string.Equals(operation, PerformanceOperationKinds.Capture, StringComparison.Ordinal))
                return CapturePreference;
            throw new ArgumentOutOfRangeException(nameof(operation));
        }

        static bool IsTerminalStatus(string status) =>
            string.Equals(status, PerformanceCaptureStatus.Completed.ToString(), StringComparison.Ordinal) ||
            string.Equals(status, PerformanceCaptureStatus.Faulted.ToString(), StringComparison.Ordinal) ||
            string.Equals(status, PerformanceCaptureStatus.Cancelled.ToString(), StringComparison.Ordinal);

        static PerformanceScenarioDocument RequireScenario()
        {
            PerformanceScenarioDocument scenario = ReadJson<PerformanceScenarioDocument>(ScenarioPath);
            RequireSchema(scenario.schema, PerformanceCaptureSchemas.Scenario, "scenario");
            if (!string.Equals(scenario.content_hash, PerformanceCaptureIdentity.Scenario(scenario), StringComparison.Ordinal))
                throw new InvalidDataException("Performance Scenario content identity is invalid.");
            return scenario;
        }

        static GameplayLabSessionVariantDefinition RequireVariant(string variantId)
        {
            GameplayLabSessionVariantDefinition[] values = AssetDatabase.FindAssets("t:GameplayLabSessionVariantDefinition")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameplayLabSessionVariantDefinition>)
                .Where(value => value)
                .Where(value => string.Equals(value.VariantId, variantId, StringComparison.Ordinal))
                .ToArray();
            if (values.Length != 1)
                throw new InvalidOperationException($"Performance Scenario requires exactly one Variant '{variantId}', found {values.Length}.");
            return values[0];
        }

        static PerformanceBuildIdentity CaptureBuildIdentity(
            GameplayLabSessionVariantDefinition variant,
            PerformanceInstrumentationMode instrumentationMode,
            string instrumentationIdentity)
        {
            string programPath = AssetDatabase.GetAssetPath(variant.Program);
            return new PerformanceBuildIdentity(
                $"{variant.DefinitionGuid}:{AssetDatabase.GetAssetDependencyHash(programPath)}",
                variant.PipelineId,
                variant.PresentationProjection.ProjectionRevision,
                variant.SolverId,
                instrumentationIdentity,
                instrumentationMode.ToString());
        }

        static void CopyScenarioClosure(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            string[] names = { "scenario.json", "input-trace.json", "camera-trace.json", "capture-profile.json", "budget.json" };
            for (int i = 0; i < names.Length; i++)
            {
                string path = Path.Combine(source, names[i]);
                RequireFile(path, names[i]);
                File.Copy(path, Path.Combine(destination, names[i]), false);
            }
        }

        static void PublishNativeSymbols(string candidate, string executable)
        {
            string playerName = Path.GetFileNameWithoutExtension(executable);
            string backup = Path.Combine(candidate, $"{playerName}_BackUpThisFolder_ButDontShipItWithYourGame");
            string source = Path.Combine(backup, "GameAssembly.pdb");
            RequireFile(source, "GameAssembly PDB");
            string destination = Path.Combine(candidate, "GameAssembly.pdb");
            if (File.Exists(destination))
                throw new IOException($"Performance Player symbol already exists: {destination}");
            File.Move(source, destination);
            DeleteOwnedStage(backup, candidate);
        }

        static PerformanceFileDocument[] BuildClosure(string root) =>
            Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(path => new PerformanceFileDocument
                {
                    role = PlayerFileRole(path),
                    path = RelativePath(root, path),
                    size = new FileInfo(path).Length,
                    sha256 = Sha256(path)
                })
                .ToArray();

        static string PlayerFileRole(string path)
        {
            string extension = Path.GetExtension(path);
            if (string.Equals(extension, ".pdb", StringComparison.OrdinalIgnoreCase))
                return path.Contains("burst", StringComparison.OrdinalIgnoreCase) ? "burst-symbol" : "native-symbol";
            if (string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase))
                return "player-executable";
            if (string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase))
                return "player-binary";
            if (path.Contains($"{Path.DirectorySeparatorChar}Scenario{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                return "scenario-catalog";
            return "player-file";
        }

        static void BuildController()
        {
            RequireFile(ControllerProjectPath, "Performance Controller project");
            string arguments = $"build \"{ControllerProjectPath}\" --configuration Release --disable-build-servers /nr:false /p:UseSharedCompilation=false";
            ProcessResult build;
            ProcessResult shutdown;
            try
            {
                build = RunProcess("dotnet", arguments, RepositoryRoot, 300000);
            }
            finally
            {
                shutdown = RunProcess("dotnet", "build-server shutdown", RepositoryRoot, 60000);
            }
            if (build.ExitCode != 0)
                throw new InvalidOperationException($"Performance Controller build failed.\n{build.Output}\n{shutdown.Output}");
        }

        static ProcessResult RunProcess(string file, string arguments, string workingDirectory, int timeoutMilliseconds)
        {
            var start = new ProcessStartInfo
            {
                FileName = file,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using Process process = Process.Start(start) ?? throw new InvalidOperationException($"Process '{file}' did not start.");
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(timeoutMilliseconds))
            {
                process.Kill();
                throw new TimeoutException($"Process '{file}' timed out.");
            }
            return new ProcessResult(process.ExitCode, output + error);
        }

        static string RequireLastCaptureRoot()
        {
            RequireFile(LastCaptureManifestPath, "last Performance Capture manifest");
            return Path.GetDirectoryName(Path.GetFullPath(LastCaptureManifestPath));
        }

        internal static string RequireCaptureFile(string role) =>
            RequireCaptureFile(LastCaptureManifestPath, role);

        internal static string RequireCaptureFile(string manifestPath, string role)
        {
            RequireFile(manifestPath, "Performance Capture manifest");
            string root = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
            PerformanceCaptureManifestDocument manifest = ReadJson<PerformanceCaptureManifestDocument>(manifestPath);
            RequireSchema(manifest.schema, PerformanceCaptureSchemas.Manifest, "capture manifest");
            PerformanceFileDocument[] files = (manifest.files ?? Array.Empty<PerformanceFileDocument>())
                .Where(value => value != null && string.Equals(value.role, role, StringComparison.Ordinal))
                .ToArray();
            if (files.Length != 1)
                throw new InvalidDataException($"Performance Capture manifest requires exactly one '{role}' file.");
            string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(Path.Combine(root, files[0].path));
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(path) ||
                new FileInfo(path).Length != files[0].size ||
                !string.Equals(Sha256(path), files[0].sha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Performance Capture file '{role}' does not match its manifest.");
            }
            return path;
        }

        static PerformanceToolchainDocument RequireCaptureToolchain()
        {
            PerformanceCaptureManifestDocument manifest = ReadJson<PerformanceCaptureManifestDocument>(LastCaptureManifestPath);
            if (!string.Equals(manifest.toolchain_identity, Sha256(ToolchainPath), StringComparison.Ordinal))
                throw new InvalidDataException("Current Toolchain Definition does not match the selected Capture.");
            PerformanceToolchainDocument toolchain = ReadJson<PerformanceToolchainDocument>(ToolchainPath);
            RequireVersion(toolchain.unity_profiler_path, toolchain.unity_version, "Unity Profiler host");
            RequireVersion(toolchain.wpa_path, toolchain.wpa_version, "WPA");
            if (!string.Equals(Sha256(toolchain.wpr_profile_path), toolchain.wpr_profile_hash, StringComparison.Ordinal) ||
                !string.Equals(Sha256(toolchain.wpa_profile_path), toolchain.wpa_profile_hash, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Current WPR or WPA profile does not match the selected Capture Toolchain.");
            }
            return toolchain;
        }

        static void OpenFile(string path)
        {
            RequireFile(path, Path.GetFileName(path));
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true })?.Dispose();
        }

        static void DeleteOwnedStage(string path, string stagingRoot)
        {
            string root = Path.GetFullPath(stagingRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string target = Path.GetFullPath(path);
            if (target.StartsWith(root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(target))
                Directory.Delete(target, true);
        }

        static string RelativePath(string root, string path)
        {
            string relative = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(path));
            if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                throw new InvalidDataException($"Performance file escapes its closure root: {path}");
            return relative.Replace(Path.DirectorySeparatorChar, '/');
        }

        static string VersionOf(string path) => FileVersionInfo.GetVersionInfo(path).FileVersion ?? string.Empty;

        static void RequireVersion(string path, string expected, string name)
        {
            RequireFile(path, name);
            string actual = VersionOf(path);
            if (string.IsNullOrWhiteSpace(expected) || !string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidDataException($"{name} version mismatch. Expected='{expected}', Actual='{actual}'.");
        }

        static void RequireSchema(string actual, string expected, string name)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidDataException($"Performance {name} schema is invalid.");
        }

        static void RequireFile(string path, string name)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException($"Performance {name} is missing.", path);
        }

        static T ReadJson<T>(string path)
        {
            RequireFile(path, typeof(T).Name);
            T value = JsonUtility.FromJson<T>(File.ReadAllText(path, Encoding.UTF8));
            return value == null ? throw new InvalidDataException($"Performance document '{path}' is invalid.") : value;
        }

        static void WriteJson<T>(string path, T value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path, JsonUtility.ToJson(value, true), new UTF8Encoding(false));
        }

        static string Sha256(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return Hex(sha.ComputeHash(stream));
        }

        static string Sha256(byte[] bytes)
        {
            using SHA256 sha = SHA256.Create();
            return Hex(sha.ComputeHash(bytes));
        }

        static string Hex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        readonly struct ProcessResult
        {
            public ProcessResult(int exitCode, string output)
            {
                ExitCode = exitCode;
                Output = output;
            }

            public int ExitCode { get; }
            public string Output { get; }
        }

        readonly struct PerformanceBuildIdentity
        {
            public PerformanceBuildIdentity(
                string programIdentity,
                string pipelineIdentity,
                string projectionIdentity,
                string solverIdentity,
                string instrumentationIdentity,
                string instrumentationMode)
            {
                ProgramIdentity = programIdentity;
                PipelineIdentity = pipelineIdentity;
                ProjectionIdentity = projectionIdentity;
                SolverIdentity = solverIdentity;
                InstrumentationIdentity = instrumentationIdentity;
                InstrumentationMode = instrumentationMode;
            }

            public string ProgramIdentity { get; }
            public string PipelineIdentity { get; }
            public string ProjectionIdentity { get; }
            public string SolverIdentity { get; }
            public string InstrumentationIdentity { get; }
            public string InstrumentationMode { get; }

            public PerformanceBuildIdentity WithInstrumentationIdentity(string identity) =>
                new PerformanceBuildIdentity(
                    ProgramIdentity,
                    PipelineIdentity,
                    ProjectionIdentity,
                    SolverIdentity,
                    identity,
                    InstrumentationMode);
        }
    }
}
