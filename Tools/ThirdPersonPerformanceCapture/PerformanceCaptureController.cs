using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using ThirdPersonPerformance;
using ThirdPersonPerformance.Instrumentation;

namespace ThirdPersonPerformanceCapture.Controller;

internal sealed class PerformanceCaptureController
{
    const string RequestArgument = "--request=";

    sealed class PerformanceCaptureCancelledException : Exception
    {
    }

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true,
        PropertyNameCaseInsensitive = false
    };

    readonly PerformanceRunRequestDocument _request;
    PerformanceToolchainDocument _toolchain;
    PerformanceScenarioDocument _scenario;
    PerformanceCaptureProfileDocument _profile;
    PerformanceBudgetDocument _budget;
    PerformancePlayerManifestDocument _playerManifest;
    readonly string _requestPath;
    readonly string _requestHash;
    string _playerManifestHash = string.Empty;
    string _toolchainHash = string.Empty;
    readonly string _wprInstance;
    readonly string _startMarker;
    readonly string _stopMarker;
    readonly StringBuilder _log = new();

    Process _player;
    bool _wprOwned;
    string _stage = "preflight";
    PerformanceCaptureStatus _status = PerformanceCaptureStatus.Pending;
    string _statusStage = "preflight";
    string _statusMessage = "Performance Controller is starting.";
    DateTime _lastStatusWriteUtc = DateTime.MinValue;

    PerformanceCaptureController(string requestPath)
    {
        _requestPath = Path.GetFullPath(requestPath);
        _requestHash = PerformanceFileUtility.Sha256(_requestPath);
        _request = ReadJson<PerformanceRunRequestDocument>(_requestPath);
        _wprInstance = $"ThirdPerson-{_request.run_id}";
        _startMarker = $"ThirdPersonCaptureStart:{_request.run_id}";
        _stopMarker = $"ThirdPersonCaptureStop:{_request.run_id}";
    }

    public static async Task<int> RunAsync(string[] args)
    {
        string requestPath = ReadArgument(args, RequestArgument);
        if (string.IsNullOrWhiteSpace(requestPath))
        {
            Console.Error.WriteLine("ThirdPerson Performance Controller requires --request=<path>.");
            return 2;
        }
        PerformanceCaptureController controller = null;
        try
        {
            controller = new PerformanceCaptureController(requestPath);
            return await controller.ExecuteAsync();
        }
        catch (PerformanceCaptureCancelledException)
        {
            return controller?.PublishCancelled() ?? 3;
        }
        catch (Exception exception)
        {
            if (controller != null)
                return controller.PublishFault(exception);
            Console.Error.WriteLine(exception);
            return 2;
        }
        finally
        {
            controller?._player?.Dispose();
        }
    }

    async Task<int> ExecuteAsync()
    {
        LoadDependencies();
        Preflight();
        WriteStatus(PerformanceCaptureStatus.Pending, "preflight", "Performance preflight completed.");
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start(1);
        int transportPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        try
        {
            _stage = "player-start";
            StartPlayer(transportPort);
            WriteStatus(PerformanceCaptureStatus.WaitingForRuntime, "waiting-for-runtime", "Waiting for Performance Player Runtime Ready.");
            _stage = "transport-connect";
            using TcpClient client = await WaitForConnectionAsync(listener, _profile.runtime_ready_timeout_seconds);
            client.NoDelay = true;
            using NetworkStream stream = client.GetStream();
            using var reader = new StreamReader(stream, new UTF8Encoding(false), false, 1024, true);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true) { AutoFlush = true };
            string hello = await ReadLineAsync(reader, _profile.runtime_ready_timeout_seconds);
            RequireHello(hello);
            string ready = await ReadUntilAsync(reader, _profile.runtime_ready_timeout_seconds, "READY");
            if (!string.Equals(ready, "READY", StringComparison.Ordinal))
                throw new InvalidDataException("Performance Player did not publish READY.");
            if (IsOperation(PerformanceOperationKinds.Smoke))
            {
                _stage = "smoke-stop";
                writer.WriteLine("STOP");
                await ReadUntilAsync(reader, _profile.runtime_ready_timeout_seconds, "STOPPED");
                WaitForPlayerSuccess();
                WriteStatus(PerformanceCaptureStatus.Completed, "completed", "Performance Smoke Gate completed.");
                PerformanceCapturePublisher.PublishGateCompleted(
                    _request,
                    _scenario,
                    _profile,
                    _playerManifest,
                    _playerManifestHash,
                    _player.Id,
                    _player.ExitCode,
                    Environment.ProcessId,
                    _log.ToString());
                return 0;
            }
            writer.WriteLine("START");
            await ReadUntilAsync(reader, _profile.runtime_ready_timeout_seconds, "WARMING_UP");
            WriteStatus(PerformanceCaptureStatus.WarmingUp, "warmup", "Performance Scenario warmup is running.");
            await ReadUntilAsync(reader, _profile.runtime_ready_timeout_seconds, "WARMUP_COMPLETED");
            if (IsOperation(PerformanceOperationKinds.Replay))
            {
                _stage = "replay";
                writer.WriteLine("CONTINUE");
                await ReadUntilAsync(reader, _profile.runtime_ready_timeout_seconds, "REPLAYING");
                WriteStatus(PerformanceCaptureStatus.Replaying, "replay", "Performance Replay Gate is running.");
                await ReadUntilAsync(reader, _profile.capture_timeout_seconds, "REPLAY_COMPLETED");
                WaitForPlayerSuccess();
                WriteStatus(PerformanceCaptureStatus.Completed, "completed", "Performance Replay Gate completed.");
                PerformanceCapturePublisher.PublishGateCompleted(
                    _request,
                    _scenario,
                    _profile,
                    _playerManifest,
                    _playerManifestHash,
                    _player.Id,
                    _player.ExitCode,
                    Environment.ProcessId,
                    _log.ToString());
                return 0;
            }
            _stage = "wpr-start";
            StartWpr();
            _stage = "capture-start";
            RunWpr("-marker", Quote(_startMarker), "-instancename", _wprInstance);
            writer.WriteLine("RECORD");
            await ReadUntilAsync(reader, _profile.runtime_ready_timeout_seconds, "RECORDING");
            _stage = "recording";
            WriteStatus(PerformanceCaptureStatus.Recording, "recording", "Performance Scenario recording is running.");
            string completed = await ReadTerminalAsync(reader, _profile.capture_timeout_seconds);
            if (!string.Equals(completed, "COMPLETED", StringComparison.Ordinal))
                throw new InvalidOperationException(completed);
            _stage = "capture-stop";
            RunWpr("-marker", Quote(_stopMarker), "-instancename", _wprInstance);
            StopWpr();
            WaitForPlayerSuccess();
            _stage = "wpa-export";
            WriteStatus(PerformanceCaptureStatus.Finalizing, "wpa-export", "Exporting WPA performance tables.");
            ExportWpa();
            _stage = "analysis";
            WriteStatus(PerformanceCaptureStatus.Finalizing, "analysis", "Building performance summary and comparison.");
            WriteStatus(PerformanceCaptureStatus.Completed, "completed", "Performance Capture completed.");
            PerformanceCapturePublisher.PublishCompleted(
                _request,
                _scenario,
                _profile,
                _budget,
                _playerManifest,
                _toolchain,
                _requestHash,
                _playerManifestHash,
                _toolchainHash,
                _player.Id,
                _player.ExitCode,
                Environment.ProcessId,
                _wprInstance,
                _log.ToString());
            return 0;
        }
        finally
        {
            listener.Stop();
            if (_wprOwned)
                TryStopWpr();
            if (_player != null && !_player.HasExited)
            {
                _player.Kill(true);
                _player.WaitForExit(10000);
            }
        }
    }

    void LoadDependencies()
    {
        _playerManifestHash = PerformanceFileUtility.Sha256(_request.player_manifest_path);
        _toolchainHash = PerformanceFileUtility.Sha256(_request.toolchain_path);
        _toolchain = ReadJson<PerformanceToolchainDocument>(_request.toolchain_path);
        _scenario = ReadJson<PerformanceScenarioDocument>(_request.scenario_path);
        _profile = ReadJson<PerformanceCaptureProfileDocument>(_request.profile_path);
        _budget = ReadJson<PerformanceBudgetDocument>(_request.budget_path);
        _playerManifest = ReadJson<PerformancePlayerManifestDocument>(_request.player_manifest_path);
    }

    void Preflight()
    {
        RequireSchema(_request.schema, PerformanceCaptureSchemas.Request, "request");
        if (!string.Equals(_request.transport, PerformanceCaptureSchemas.TransportId, StringComparison.Ordinal))
            throw new InvalidDataException("Performance transport identity is unsupported.");
        if (!IsOperation(PerformanceOperationKinds.Smoke) &&
            !IsOperation(PerformanceOperationKinds.Replay) &&
            !IsOperation(PerformanceOperationKinds.Capture))
        {
            throw new InvalidDataException($"Performance operation '{_request.operation}' is unsupported.");
        }
        if (string.IsNullOrWhiteSpace(_request.run_id))
            throw new InvalidDataException("Performance RunId is missing.");
        if (!string.Equals(Path.GetFullPath(_request.request_document_path), _requestPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Performance Request document path identity is invalid.");
        RequireSchema(_toolchain.schema, PerformanceCaptureSchemas.Toolchain, "toolchain");
        RequireSchema(_scenario.schema, PerformanceCaptureSchemas.Scenario, "scenario");
        RequireSchema(_profile.schema, PerformanceCaptureSchemas.Profile, "profile");
        RequireSchema(_budget.schema, PerformanceCaptureSchemas.Budget, "budget");
        RequireSchema(_playerManifest.schema, PerformanceCaptureSchemas.Player, "player manifest");
        RequireDirectory(_request.staging_root, "run staging");
        if (!Environment.Is64BitOperatingSystem || !Environment.Is64BitProcess)
            throw new PlatformNotSupportedException("Performance Controller requires a 64-bit Windows process and operating system.");
        string cancelPath = Path.GetFullPath(_request.cancel_path);
        string statusPath = Path.GetFullPath(_request.status_path);
        string stagingRoot = Path.GetFullPath(_request.staging_root).TrimEnd(Path.DirectorySeparatorChar);
        string resultRoot = Path.GetFullPath(_request.result_root).TrimEnd(Path.DirectorySeparatorChar);
        string stagingPrefix = stagingRoot + Path.DirectorySeparatorChar;
        if (!cancelPath.StartsWith(stagingPrefix, StringComparison.OrdinalIgnoreCase) ||
            !statusPath.StartsWith(stagingPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Performance cancel and status paths must belong to this run staging directory.");
        if (!string.Equals(Path.GetFileName(stagingRoot), _request.run_id, StringComparison.Ordinal) ||
            !string.Equals(Path.GetFileName(resultRoot), _request.run_id, StringComparison.Ordinal) ||
            !string.Equals(
                Path.GetDirectoryName(Path.GetDirectoryName(stagingRoot)!),
                Path.GetDirectoryName(resultRoot),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Performance staging and result roots do not share the exact RunId boundary.");
        }
        string driveRoot = Path.GetPathRoot(stagingPrefix) ?? throw new InvalidDataException("Performance staging drive is invalid.");
        long requiredFreeSpace = IsOperation(PerformanceOperationKinds.Capture)
            ? 10L * 1024L * 1024L * 1024L
            : 1024L * 1024L * 1024L;
        if (new DriveInfo(driveRoot).AvailableFreeSpace < requiredFreeSpace)
            throw new IOException($"Performance { _request.operation } requires more free staging space.");
        if (Directory.Exists(_request.result_root))
            throw new IOException($"Performance run destination already exists: {_request.result_root}");
        ValidatePlayerManifest();
        RequireFile(_request.scenario_path, "Scenario");
        RequireFile(_request.budget_path, "Budget");
        RequireFile(_request.profile_path, "Profile");
        if (!string.Equals(PerformanceFileUtility.Sha256(_request.scenario_path), _request.scenario_hash, StringComparison.Ordinal) ||
            !string.Equals(PerformanceFileUtility.Sha256(_request.budget_path), _request.budget_hash, StringComparison.Ordinal) ||
            !string.Equals(PerformanceFileUtility.Sha256(_request.profile_path), _request.profile_hash, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Performance Request scenario, budget or profile hash is invalid.");
        }
        if (!string.Equals(_scenario.content_hash, PerformanceCaptureIdentity.Scenario(_scenario), StringComparison.Ordinal) ||
            !string.Equals(_profile.content_hash, PerformanceCaptureIdentity.Profile(_profile), StringComparison.Ordinal) ||
            !string.Equals(_budget.content_hash, PerformanceCaptureIdentity.Budget(_budget), StringComparison.Ordinal))
        {
            throw new InvalidDataException("Performance scenario, profile or budget content identity is invalid.");
        }
        ValidateScenarioClosure();
        string expectedScene = "Assets/Scenes/GameplayLab/GameplayLab.unity";
        if (!string.Equals(_scenario.scene_path, expectedScene, StringComparison.Ordinal) ||
            !string.Equals(_playerManifest.scene_path, expectedScene, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Performance Player and Scenario must use Gameplay Lab.");
        }
        if (_profile.runtime_ready_timeout_seconds <= 0 || _profile.capture_timeout_seconds <= 0 ||
            _scenario.warmup_logic_ticks <= 0 || _scenario.capture_logic_ticks <= 0 ||
            _scenario.width <= 0 || _scenario.height <= 0)
        {
            throw new InvalidDataException("Performance timing or display configuration is invalid.");
        }
        if (Enum.TryParse(_playerManifest.instrumentation_mode, true, out PerformanceInstrumentationMode instrumentationMode) &&
            instrumentationMode == PerformanceInstrumentationMode.Span &&
            _profile.instrumentation_span_capacity <= 0)
        {
            throw new InvalidDataException("Performance instrumentation Span capacity is invalid.");
        }
        if (IsOperation(PerformanceOperationKinds.Replay))
        {
            ValidateGate(_request.smoke_manifest_path, _request.smoke_manifest_hash, PerformanceOperationKinds.Smoke);
        }
        else if (IsOperation(PerformanceOperationKinds.Capture))
        {
            ValidateCaptureToolchain();
            ValidateGate(_request.smoke_manifest_path, _request.smoke_manifest_hash, PerformanceOperationKinds.Smoke);
            ValidateGate(_request.replay_manifest_path, _request.replay_manifest_hash, PerformanceOperationKinds.Replay);
        }
        Log("Preflight completed.");
    }

    void ValidateCaptureToolchain()
    {
        if (!string.Equals(_toolchain.collector_id, PerformanceCaptureSchemas.CollectorId, StringComparison.Ordinal))
            throw new InvalidDataException("Performance collector identity is unsupported.");
        RequireFile(_toolchain.wpr_path, "WPR");
        RequireFile(_toolchain.xperf_path, "Xperf");
        RequireFile(_toolchain.wpa_exporter_path, "WPA Exporter");
        RequireFile(_toolchain.wpa_path, "WPA");
        RequireFile(_toolchain.wpr_profile_path, "WPR profile");
        RequireFile(_toolchain.wpa_profile_path, "WPA profile");
        ValidateProfileHashes();
        ValidateToolVersion(_toolchain.wpr_path, _toolchain.wpr_version, "WPR");
        ValidateToolVersion(_toolchain.xperf_path, _toolchain.xperf_version, "Xperf");
        ValidateToolVersion(_toolchain.wpa_exporter_path, _toolchain.wpa_exporter_version, "WPA Exporter");
        ValidateToolVersion(_toolchain.wpa_path, _toolchain.wpa_version, "WPA");
        ValidateToolVersion(_toolchain.unity_profiler_path, _toolchain.unity_version, "Unity Profiler");
        RequireAmd64(_toolchain.wpr_path, "WPR");
        RequireAmd64(_toolchain.xperf_path, "Xperf");
        RequireAmd64(_toolchain.wpa_exporter_path, "WPA Exporter");
        RequireAmd64(_toolchain.wpa_path, "WPA");
        RequireAmd64(_toolchain.unity_profiler_path, "Unity Profiler");
        if (!string.Equals(_toolchain.unity_profiler_open_mode, "current-editor-load-profile", StringComparison.Ordinal))
            throw new InvalidDataException("Performance Unity Profiler open mode is unsupported.");
        RequireDirectory(_toolchain.symbol_cache_path, "Symbol cache");
        if (!IsAdministrator())
            throw new UnauthorizedAccessException("Performance Capture must run elevated for WPR CPU sampling.");
    }

    void ValidateGate(string manifestPath, string expectedHash, string operation)
    {
        RequireFile(manifestPath, $"{operation} Gate manifest");
        if (string.IsNullOrWhiteSpace(expectedHash) ||
            !string.Equals(PerformanceFileUtility.Sha256(manifestPath), expectedHash, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Performance {operation} Gate manifest hash is invalid.");
        }
        PerformanceGateManifestDocument gate = ReadJson<PerformanceGateManifestDocument>(manifestPath);
        if (!string.Equals(gate.schema, PerformanceCaptureSchemas.Gate, StringComparison.Ordinal) ||
            !string.Equals(gate.operation, operation, StringComparison.Ordinal) ||
            !string.Equals(gate.status, PerformanceCaptureStatus.Completed.ToString(), StringComparison.Ordinal) ||
            !string.Equals(gate.scenario_hash, _scenario.content_hash, StringComparison.Ordinal) ||
            !string.Equals(gate.build_id, _playerManifest.build_id, StringComparison.Ordinal) ||
            !string.Equals(gate.instrumentation_identity, _playerManifest.instrumentation_identity, StringComparison.Ordinal) ||
            !string.Equals(gate.instrumentation_mode, _playerManifest.instrumentation_mode, StringComparison.Ordinal) ||
            gate.instrumentation_span_layout_revision != _playerManifest.instrumentation_span_layout_revision ||
            !string.Equals(gate.player_manifest_hash, _playerManifestHash, StringComparison.Ordinal) ||
            !string.Equals(gate.profile_hash, _request.profile_hash, StringComparison.Ordinal) ||
            !string.Equals(gate.input_trace_hash, _scenario.input_trace_hash, StringComparison.Ordinal) ||
            !string.Equals(gate.camera_trace_hash, _scenario.camera_trace_hash, StringComparison.Ordinal) ||
            gate.warmup_logic_ticks != _scenario.warmup_logic_ticks ||
            gate.capture_logic_ticks != _scenario.capture_logic_ticks)
        {
            throw new InvalidDataException($"Performance {operation} Gate identity does not match this Capture.");
        }
        if (operation == PerformanceOperationKinds.Replay &&
            !string.Equals(gate.predecessor_gate_hash, _request.smoke_manifest_hash, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Performance Replay Gate was not produced from the selected Smoke Gate.");
        }
        if (gate.files == null || gate.files.Length == 0)
            throw new InvalidDataException($"Performance {operation} Gate closure is empty.");
        string root = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
        string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        for (int i = 0; i < gate.files.Length; i++)
        {
            PerformanceFileDocument file = gate.files[i] ??
                throw new InvalidDataException($"Performance {operation} Gate contains a null file.");
            string path = Path.GetFullPath(Path.Combine(root, file.path));
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(path) ||
                new FileInfo(path).Length != file.size ||
                !string.Equals(PerformanceFileUtility.Sha256(path), file.sha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Performance {operation} Gate file '{file.path}' identity mismatch.");
            }
        }
    }

    void ValidateScenarioClosure()
    {
        string root = Path.GetDirectoryName(Path.GetFullPath(_request.scenario_path))!;
        string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string input = ResolvePath(root, _scenario.input_trace_path);
        string camera = ResolvePath(root, _scenario.camera_trace_path);
        if (!input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            !camera.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Performance Scenario traces must belong to the immutable Scenario artifact.");
        }
        RequireFile(input, "Fixed Input Trace");
        RequireFile(camera, "Camera Trace");
        if (!string.Equals(PerformanceFileUtility.Sha256(input), _scenario.input_trace_hash, StringComparison.Ordinal) ||
            !string.Equals(PerformanceFileUtility.Sha256(camera), _scenario.camera_trace_hash, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Performance Scenario trace hash is invalid.");
        }
        PerformanceCameraTraceDocument cameraTrace = ReadJson<PerformanceCameraTraceDocument>(camera);
        if (!string.Equals(cameraTrace.schema, PerformanceCaptureSchemas.CameraTrace, StringComparison.Ordinal) ||
            cameraTrace.revision <= 0 || cameraTrace.tick_rate != _profile.logic_tick_rate ||
            cameraTrace.frame_count != _scenario.warmup_logic_ticks + _scenario.capture_logic_ticks ||
            string.IsNullOrWhiteSpace(cameraTrace.input_id) ||
            !string.Equals(cameraTrace.content_hash, PerformanceCaptureIdentity.CameraTrace(cameraTrace), StringComparison.Ordinal))
        {
            throw new InvalidDataException("Performance Camera Trace identity is invalid.");
        }
        if (!string.Equals(_scenario.ready_condition, "gameplay-lab.session-active+locked-roster+fixed-start-body+metric-catalog-registered", StringComparison.Ordinal) ||
            !string.Equals(_scenario.capture_start_boundary, "after-fixed-input-warmup", StringComparison.Ordinal) ||
            !string.Equals(_scenario.capture_end_boundary, "fixed-input-replay-completed", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Performance Scenario Ready or Capture boundary contract is unsupported.");
        }
    }

    void ValidatePlayerManifest()
    {
        string manifestRoot = Path.GetDirectoryName(Path.GetFullPath(_request.player_manifest_path))!;
        if (string.IsNullOrWhiteSpace(_request.instrumentation_identity) ||
            !string.Equals(_request.instrumentation_identity, _playerManifest.instrumentation_identity, StringComparison.Ordinal) ||
            !string.Equals(_request.instrumentation_mode, _playerManifest.instrumentation_mode, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(_playerManifest.instrumentation_identity) ||
            !Enum.TryParse(_playerManifest.instrumentation_mode, true, out PerformanceInstrumentationMode instrumentationMode) ||
            !Enum.IsDefined(typeof(PerformanceInstrumentationMode), instrumentationMode) ||
            _playerManifest.instrumentation_span_layout_revision != PerformanceInstrumentationIdentity.SpanLayoutRevision)
        {
            throw new InvalidDataException("Performance Player instrumentation identity or mode is invalid.");
        }
        if (string.IsNullOrWhiteSpace(_playerManifest.instrumentation_manifest_path))
            throw new InvalidDataException("Performance Player instrumentation manifest path is missing.");
        RequireFile(
            ResolvePath(manifestRoot, _playerManifest.instrumentation_manifest_path),
            "Performance Player instrumentation manifest");
        if (!string.Equals(_playerManifest.build_target, "StandaloneWindows64", StringComparison.Ordinal) ||
            !string.Equals(_playerManifest.scripting_backend, "IL2CPP", StringComparison.Ordinal) ||
            !string.Equals(_playerManifest.build_mode, "Development", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Performance Player target, scripting backend or build mode is invalid.");
        }
        string executable = ResolvePath(manifestRoot, _playerManifest.executable_path);
        RequireFile(executable, "Performance Player executable");
        RequireAmd64(executable, "Performance Player");
        string catalog = ResolvePath(manifestRoot, _playerManifest.scenario_catalog_path);
        RequireFile(catalog, "Performance Player Scenario catalog");
        if (!string.Equals(PerformanceFileUtility.Sha256(catalog), PerformanceFileUtility.Sha256(_request.scenario_path), StringComparison.Ordinal))
            throw new InvalidDataException("Performance Player Scenario catalog does not match the Capture request.");
        if (_playerManifest.files == null || _playerManifest.files.Length == 0)
            throw new InvalidDataException("Performance Player file closure is empty.");
        if (!_playerManifest.files.Any(value => string.Equals(value?.role, "native-symbol", StringComparison.Ordinal)) ||
            !_playerManifest.files.Any(value => string.Equals(value?.role, "burst-symbol", StringComparison.Ordinal)))
        {
            throw new InvalidDataException("Performance Player file closure is missing native or Burst symbols.");
        }
        for (int i = 0; i < _playerManifest.files.Length; i++)
        {
            PerformanceFileDocument file = _playerManifest.files[i] ??
                throw new InvalidDataException("Performance Player manifest contains a null file.");
            string path = ResolvePath(manifestRoot, file.path);
            RequireFile(path, file.role);
            if (new FileInfo(path).Length != file.size ||
                !string.Equals(PerformanceFileUtility.Sha256(path), file.sha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Performance Player file '{file.path}' identity mismatch.");
            }
        }
        string closureIdentity = string.Join("\n", _playerManifest.files.Select(value =>
            $"{value.role}|{value.path}|{value.size.ToString(CultureInfo.InvariantCulture)}|{value.sha256}"));
        string buildSeed = string.Join("|", new[]
        {
            _playerManifest.unity_version,
            _playerManifest.build_target,
            _playerManifest.scripting_backend,
            _playerManifest.build_mode,
            _scenario.content_hash,
            _playerManifest.content_identity,
            _playerManifest.pipeline_identity,
            _playerManifest.pose_graph_revision,
            _playerManifest.solver_identity,
            _playerManifest.instrumentation_identity,
            _playerManifest.instrumentation_mode,
            closureIdentity
        });
        string buildId = PerformanceFileUtility.Sha256(Encoding.UTF8.GetBytes(buildSeed)).Substring(0, 24);
        if (!string.Equals(buildId, _playerManifest.build_id, StringComparison.Ordinal))
            throw new InvalidDataException("Performance Player BuildIdentity does not match its exact closure.");
    }

    void StartWpr()
    {
        string temp = Path.Combine(_request.staging_root, "wpr-temp");
        Directory.CreateDirectory(temp);
        RunWpr(
            "-start",
            Quote($"{Path.GetFullPath(_toolchain.wpr_profile_path)}!ThirdPersonCpu"),
            "-filemode",
            "-recordtempto",
            Quote(temp),
            "-instancename",
            _wprInstance);
        _wprOwned = true;
        Log($"WPR started: {_wprInstance}");
    }

    void WaitForPlayerSuccess()
    {
        if (!_player.WaitForExit(30000))
            throw new TimeoutException("Performance Player did not exit after run completion.");
        if (_player.ExitCode != 0)
            throw new InvalidOperationException($"Performance Player exited with code {_player.ExitCode}.");
    }

    void StopWpr()
    {
        string etl = Path.Combine(_request.staging_root, "windows-cpu.etl");
        RunWpr("-stop", Quote(etl), "-instancename", _wprInstance);
        _wprOwned = false;
        RequireFile(etl, "WPR ETL");
        Log("WPR stopped.");
    }

    void TryStopWpr()
    {
        try
        {
            string etl = Path.Combine(_request.staging_root, "windows-cpu-fault.etl");
            RunWpr("-stop", Quote(etl), "-instancename", _wprInstance);
        }
        catch (Exception exception)
        {
            Log($"Owned WPR cleanup failed: {exception.Message}");
        }
        _wprOwned = false;
    }

    void RunWpr(params string[] arguments)
    {
        ProcessResult result = PerformanceProcessRunner.Run(
            _toolchain.wpr_path,
            string.Join(' ', arguments),
            _request.staging_root,
            null,
            TimeSpan.FromMinutes(2),
            Heartbeat);
        Log(result.Output);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"WPR failed with code {result.ExitCode}: {result.Output}");
    }

    void StartPlayer(int transportPort)
    {
        string manifestRoot = Path.GetDirectoryName(Path.GetFullPath(_request.player_manifest_path))!;
        string executable = ResolvePath(manifestRoot, _playerManifest.executable_path);
        string playerLog = Path.Combine(_request.staging_root, "player.log");
        var start = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = manifestRoot,
            UseShellExecute = false,
            CreateNoWindow = false,
            Arguments =
                $"--third-person-performance-request={Quote(_requestPath)} " +
                $"--third-person-performance-port={transportPort.ToString(CultureInfo.InvariantCulture)} " +
                $"-logFile {Quote(playerLog)}"
        };
        _player = Process.Start(start) ?? throw new InvalidOperationException("Performance Player failed to start.");
        Log($"Player started: PID={_player.Id}");
    }

    void ExportWpa()
    {
        ValidateProfileHashes();
        string etl = Path.Combine(_request.staging_root, "windows-cpu.etl");
        string exportRoot = Path.Combine(_request.staging_root, "wpa-export");
        Directory.CreateDirectory(exportRoot);
        string configPath = Path.Combine(_request.staging_root, "wpa-exporter.json");
        PerformanceFileUtility.WriteJson(configPath, new
        {
            InputFileNames = new[]
            {
                new { Key = "capture", Value = etl }
            },
            OutputOptions = new
            {
                OutputFolder = exportRoot,
                OutputFormat = "CSV",
                Delimiter = ",",
                Prefix = "ThirdPerson"
            },
            Profiles = new[]
            {
                new
                {
                    Path = _toolchain.wpa_profile_path,
                    InputFiles = new[]
                    {
                        new
                        {
                            Name = "capture",
                            Marks = new { Start = _startMarker, End = _stopMarker }
                        }
                    }
                }
            }
        }, JsonOptions);
        ProcessResult result = PerformanceProcessRunner.Run(
            _toolchain.wpa_exporter_path,
            $"-exporterconfig {Quote(configPath)}",
            _request.staging_root,
            null,
            TimeSpan.FromMinutes(5),
            Heartbeat);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"WPA Exporter failed with code {result.ExitCode}: {result.Output}");
        Log("WPA Context Switch export completed.");
        PerformanceFileUtility.PublishWpaContextSwitches(exportRoot, _request.staging_root, _player.Id);

        ProcessResult marks = PerformanceProcessRunner.Run(
            _toolchain.xperf_path,
            $"-i {Quote(etl)} -target machine -a marks",
            _request.staging_root,
            null,
            TimeSpan.FromMinutes(1),
            Heartbeat);
        if (marks.ExitCode != 0)
            throw new InvalidOperationException($"Xperf marker export failed with code {marks.ExitCode}: {marks.Output}");
        PerformanceMarkerRange markerRange = PerformanceFileUtility.PublishXperfMarkers(
            marks.Output,
            _request.staging_root,
            _startMarker,
            _stopMarker);

        string playerRoot = Path.GetDirectoryName(Path.GetFullPath(_request.player_manifest_path));
        var environment = new Dictionary<string, string>
        {
            ["_NT_SYMBOL_PATH"] = playerRoot,
            ["_NT_SYMCACHE_PATH"] = Path.GetFullPath(_toolchain.symbol_cache_path)
        };
        string stackArguments =
            $"-i {Quote(etl)} -symbols -target machine -quiet -a stack -butterfly 1 " +
            $"-pid {_player.Id.ToString(CultureInfo.InvariantCulture)} -event PROFILE " +
            $"-range {markerRange.Start.ToString(CultureInfo.InvariantCulture)} {markerRange.End.ToString(CultureInfo.InvariantCulture)}";
        ProcessResult stacks = PerformanceProcessRunner.Run(
            _toolchain.xperf_path,
            stackArguments,
            _request.staging_root,
            environment,
            TimeSpan.FromMinutes(5),
            Heartbeat);
        if (stacks.ExitCode != 0)
            throw new InvalidOperationException($"Xperf stack export failed with code {stacks.ExitCode}: {stacks.Output}");
        PerformanceFileUtility.PublishXperfStackReport(stacks.Output, _request.staging_root, _player.Id);
        Log("Xperf Sampled Profile stack export completed.");
    }

    int PublishCancelled()
    {
        try
        {
            WriteStatus(PerformanceCaptureStatus.Cancelled, "cancelled", $"Performance {_request.operation} was cancelled by its owner.");
            if (_wprOwned)
                TryStopWpr();
            if (_player != null && !_player.HasExited)
            {
                _player.Kill(true);
                _player.WaitForExit(10000);
            }
            Log($"Performance {_request.operation} cancellation completed.");
            if (IsOperation(PerformanceOperationKinds.Capture))
            {
                PerformanceCapturePublisher.PublishCancelled(
                    _request,
                    _scenario,
                    _playerManifest,
                    _toolchain,
                    _requestHash,
                    _playerManifestHash,
                    _toolchainHash,
                    _player?.Id ?? 0,
                    _player != null && _player.HasExited ? _player.ExitCode : 3,
                    Environment.ProcessId,
                    _wprInstance,
                    _log.ToString());
            }
            else
            {
                PerformanceCapturePublisher.PublishGateTerminal(
                    _request,
                    _scenario,
                    _profile,
                    _playerManifest,
                    _playerManifestHash,
                    PerformanceCaptureStatus.Cancelled,
                    "cancelled",
                    $"Performance {_request.operation} was cancelled.",
                    _player?.Id ?? 0,
                    _player != null && _player.HasExited ? _player.ExitCode : 3,
                    Environment.ProcessId,
                    _log.ToString());
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 2;
        }
        return 3;
    }

    int PublishFault(Exception exception)
    {
        try
        {
            _stage = string.IsNullOrWhiteSpace(_stage) ? "controller" : _stage;
            WriteStatus(PerformanceCaptureStatus.Faulted, _stage, exception.Message);
            if (_wprOwned)
                TryStopWpr();
            if (_player != null && !_player.HasExited)
            {
                _player.Kill(true);
                _player.WaitForExit(10000);
            }
            Log(exception.ToString());
            if (IsOperation(PerformanceOperationKinds.Capture))
            {
                PerformanceCapturePublisher.PublishFaulted(
                    _request,
                    _scenario,
                    _playerManifest,
                    _toolchain,
                    _requestHash,
                    _playerManifestHash,
                    _toolchainHash,
                    _stage,
                    exception.Message,
                    _player?.Id ?? 0,
                    _player != null && _player.HasExited ? _player.ExitCode : -1,
                    Environment.ProcessId,
                    _wprInstance,
                    _log.ToString());
            }
            else
            {
                PerformanceCapturePublisher.PublishGateTerminal(
                    _request,
                    _scenario,
                    _profile,
                    _playerManifest,
                    _playerManifestHash,
                    PerformanceCaptureStatus.Faulted,
                    _stage,
                    exception.Message,
                    _player?.Id ?? 0,
                    _player != null && _player.HasExited ? _player.ExitCode : -1,
                    Environment.ProcessId,
                    _log.ToString());
            }
        }
        catch (Exception publishException)
        {
            Console.Error.WriteLine(exception);
            Console.Error.WriteLine(publishException);
        }
        return 2;
    }

    void RequireHello(string line)
    {
        string[] parts = line?.Split('|') ?? Array.Empty<string>();
        if (parts.Length != 3 || !string.Equals(parts[0], "HELLO", StringComparison.Ordinal) ||
            !string.Equals(parts[1], _request.run_id, StringComparison.Ordinal) ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int pid) ||
            _player == null || pid != _player.Id)
        {
            throw new InvalidDataException("Performance Player HELLO identity is invalid.");
        }
    }

    async Task<TcpClient> WaitForConnectionAsync(TcpListener listener, int timeoutSeconds)
    {
        Task<TcpClient> connection = listener.AcceptTcpClientAsync();
        DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (!connection.IsCompleted && DateTime.UtcNow < deadline)
        {
            ThrowIfCancelled();
            Heartbeat();
            await Task.WhenAny(connection, Task.Delay(250));
        }
        if (!connection.IsCompleted)
            throw new TimeoutException("Performance Player did not connect to the Controller loopback transport.");
        TcpClient client = await connection;
        if (client.Client.RemoteEndPoint is not IPEndPoint remote || !IPAddress.IsLoopback(remote.Address))
        {
            client.Dispose();
            throw new InvalidDataException("Performance transport accepted a non-loopback client.");
        }
        return client;
    }

    async Task<string> ReadUntilAsync(StreamReader reader, int timeoutSeconds, string expected)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            string line = await ReadLineAsync(reader, Math.Max(1, (int)(deadline - DateTime.UtcNow).TotalSeconds));
            if (string.Equals(line, expected, StringComparison.Ordinal))
                return line;
            if (line.StartsWith("FAULT|", StringComparison.Ordinal))
                throw new InvalidOperationException(line);
        }
        throw new TimeoutException($"Performance Player did not publish '{expected}'.");
    }

    async Task<string> ReadTerminalAsync(StreamReader reader, int timeoutSeconds)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            string line = await ReadLineAsync(reader, Math.Max(1, (int)(deadline - DateTime.UtcNow).TotalSeconds));
            if (string.Equals(line, "COMPLETED", StringComparison.Ordinal) ||
                line.StartsWith("FAULT|", StringComparison.Ordinal))
            {
                return line;
            }
        }
        throw new TimeoutException("Performance Player did not publish a terminal result.");
    }

    async Task<string> ReadLineAsync(StreamReader reader, int timeoutSeconds)
    {
        Task<string> read = reader.ReadLineAsync();
        DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (!read.IsCompleted && DateTime.UtcNow < deadline)
        {
            ThrowIfCancelled();
            Heartbeat();
            await Task.WhenAny(read, Task.Delay(250));
        }
        if (!read.IsCompleted)
            throw new TimeoutException("Performance Player transport response timed out.");
        string line = await read;
        return line ?? throw new EndOfStreamException("Performance Player transport closed.");
    }

    void ThrowIfCancelled()
    {
        if (File.Exists(_request.cancel_path))
            throw new PerformanceCaptureCancelledException();
    }

    void Log(string value)
    {
        string line = $"{DateTime.UtcNow:O} {value}";
        _log.AppendLine(line);
        Console.WriteLine(line);
    }

    void WriteStatus(PerformanceCaptureStatus status, string stage, string message)
    {
        _status = status;
        _statusStage = stage;
        _statusMessage = message;
        _lastStatusWriteUtc = DateTime.UtcNow;
        var document = new PerformanceRunStatusDocument
        {
            operation = _request.operation,
            run_id = _request.run_id,
            status = status.ToString(),
            stage = stage,
            message = message,
            updated_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            controller_process_id = Environment.ProcessId,
            player_process_id = _player?.Id ?? 0
        };
        string temporary = _request.status_path + ".tmp";
        PerformanceFileUtility.WriteJson(temporary, document, JsonOptions);
        File.Move(temporary, _request.status_path, true);
    }

    void Heartbeat()
    {
        if (DateTime.UtcNow - _lastStatusWriteUtc < TimeSpan.FromSeconds(1))
            return;
        WriteStatus(_status, _statusStage, _statusMessage);
    }

    static T ReadJson<T>(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new FileNotFoundException("Performance document is missing.", path);
        T value = JsonSerializer.Deserialize<T>(File.ReadAllText(path, Encoding.UTF8), JsonOptions);
        return value == null ? throw new InvalidDataException($"Performance document '{path}' is invalid.") : value;
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

    static void RequireDirectory(string path, string name)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            throw new DirectoryNotFoundException($"Performance {name} is missing: {path}");
    }

    static void ValidateToolVersion(string path, string expected, string name)
    {
        RequireFile(path, name);
        string actual = FileVersionInfo.GetVersionInfo(path).FileVersion ?? string.Empty;
        if (string.IsNullOrWhiteSpace(expected) || !string.Equals(actual, expected, StringComparison.Ordinal))
            throw new InvalidDataException($"Performance {name} version mismatch. Expected='{expected}', Actual='{actual}'.");
    }

    void ValidateProfileHashes()
    {
        if (!string.Equals(PerformanceFileUtility.Sha256(_toolchain.wpr_profile_path), _toolchain.wpr_profile_hash, StringComparison.Ordinal) ||
            !string.Equals(PerformanceFileUtility.Sha256(_toolchain.wpa_profile_path), _toolchain.wpa_profile_hash, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Performance WPR or WPA profile hash does not match the Toolchain Definition.");
        }
    }

    static void RequireAmd64(string path, string name)
    {
        using FileStream stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        stream.Position = 0x3c;
        int peOffset = reader.ReadInt32();
        stream.Position = peOffset;
        if (reader.ReadUInt32() != 0x00004550 || reader.ReadUInt16() != 0x8664)
            throw new PlatformNotSupportedException($"Performance {name} must be an AMD64 executable.");
    }

    static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    bool IsOperation(string operation) =>
        string.Equals(_request.operation, operation, StringComparison.Ordinal);

    static string ResolvePath(string root, string path) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(root, path));

    static string ReadArgument(string[] args, string prefix)
    {
        string value = string.Empty;
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith(prefix, StringComparison.Ordinal))
                continue;
            if (!string.IsNullOrEmpty(value))
                throw new InvalidOperationException($"Argument '{prefix}' is duplicated.");
            value = args[i].Substring(prefix.Length).Trim('"');
        }
        return value;
    }

    static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";
}
