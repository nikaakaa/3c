using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicRollback;
using ThirdPersonSimulation.Fixed;
using UnityEditor;
using UnityEngine;

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
        const string PlayerActorId = "gameplay-lab-player";
        const string PendingOperationKey = "ThirdPerson.CharacterInputTrace.PendingOperation.v1";
        const string PendingTracePathKey = "ThirdPerson.CharacterInputTrace.PendingTracePath.v1";
        const string PendingVariantKey = "ThirdPerson.CharacterInputTrace.PendingVariant.v1";
        const string PendingDeadlineKey = "ThirdPerson.CharacterInputTrace.PendingDeadline.v1";
        const double PendingSeconds = 60d;
        const float PositionTolerance = 0.1f;
        const float YawTolerance = 2f;

        static PoseRecord s_RecordingStartPose;
        static int s_RecordingVariantIndex;
        static bool s_ReplayOwnsSampling;
        static bool s_ReplayWaitingForSampling;
        static bool s_ReplayFinalizing;
        static TraceDocument s_PendingReplayDocument;
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
            if (IsPending && EditorApplication.isPlaying)
                ResetPendingDeadline();
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
        public static string TraceDirectory => ResolveTraceDirectory();

        public static void StartRecording()
        {
            RequireAvailable();
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
            operations.Play(state.SelectedVariantIndex);
            s_LastStatus = "Starting Gameplay Lab before canonical Fixed input recording.";
        }

        public static string StopAndSaveRecording()
        {
            if (!IsRecording)
                throw new InvalidOperationException("Canonical Fixed input recording is not active.");
            FixedCharacterInputTrace trace = FixedCharacterInputTraceModule.StopRecording();
            TraceDocument document = CreateDocument(trace, s_RecordingStartPose, s_RecordingVariantIndex);
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
            ReplayPath(path);
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
            ReplayPath(path);
        }

        public static void Stop()
        {
            if (IsRecording)
                throw new InvalidOperationException("Use Stop and Save Input to finish an active recording.");
            ClearPending();
            s_ReplayWaitingForSampling = false;
            s_PendingReplayDocument = null;
            if (s_ReplayOwnsSampling &&
                (CharacterFootLandingPredictionSampler.IsCapturing ||
                 CharacterFootLandingPredictionSampler.IsStartPending))
            {
                CharacterFootLandingPredictionSampler.StopAndSaveSampling();
            }
            s_ReplayOwnsSampling = false;
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

        static void ReplayPath(string path)
        {
            RequireAvailable();
            TraceDocument document = ReadDocument(path, true);
            s_PendingReplayDocument = document;
            s_LastFailure = string.Empty;
            s_LastTracePath = path;
            s_LastTraceId = document.trace_id;
            ArmPending("replay", path, document.launcher_variant_index);
            s_LastStatus = $"Restarting Gameplay Lab to replay trace {document.trace_id}.";
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.ExitPlaymode();
                return;
            }
            StartPendingPlayMode();
        }

        static void BeginRecording()
        {
            if (!TryResolvePlayerPose(out PoseRecord pose))
                throw new InvalidOperationException("Gameplay Lab Fixed player is not ready for input recording.");
            s_RecordingStartPose = pose;
            s_RecordingVariantIndex = EditorPrefs.GetInt(PendingVariantKey, -1);
            if (s_RecordingVariantIndex < 0)
                throw new InvalidOperationException("Canonical Fixed input recording has no Gameplay Lab variant identity.");
            FixedCharacterInputTraceModule.StartRecording(new ActorId(PlayerActorId));
            ClearPending();
            EditorApplication.isPaused = false;
            s_LastStatus = "Recording canonical character input per Fixed simulation Tick. Camera input remains live.";
        }

        static void BeginReplay(TraceDocument document)
        {
            if (!s_ReplayWaitingForSampling)
            {
                if (!TryResolvePlayerPose(out PoseRecord current))
                    return;
                float positionError = Vector3.Distance(current.Position, document.start_pose.Position);
                float yawError = Mathf.Abs(Mathf.DeltaAngle(current.yaw_degrees, document.start_pose.yaw_degrees));
                if (positionError > PositionTolerance || yawError > YawTolerance)
                {
                    throw new InvalidOperationException(
                        $"Replay start state does not match the recording. PositionError={positionError:0.###}, YawError={yawError:0.###}.");
                }
                ResetPendingDeadline();
                CharacterFootLandingPredictionSampler.StartSampling();
                s_ReplayOwnsSampling = true;
                s_ReplayWaitingForSampling = true;
                FixedCharacterInputTrace trace = ToRuntimeTrace(document);
                FixedCharacterInputTraceModule.StartReplay(trace);
                EditorApplication.isPaused = false;
                s_LastStatus =
                    $"Replaying {trace.Frames.Count} canonical Fixed input frames while Foot Landing sampling starts.";
            }
            if (CharacterFootLandingPredictionSampler.IsStartPending)
                return;
            if (!CharacterFootLandingPredictionSampler.IsCapturing)
                throw new InvalidOperationException(
                    string.IsNullOrEmpty(CharacterFootLandingPredictionSampler.LastStartFailure)
                        ? "Foot Landing sampling did not start."
                        : CharacterFootLandingPredictionSampler.LastStartFailure);
            s_ReplayWaitingForSampling = false;
            ClearPending();
            FixedCharacterInputTraceStatus status = FixedCharacterInputTraceModule.Status;
            s_LastStatus =
                $"Replaying {status.FrameCount} canonical Fixed input frames with Foot Landing sampling active. Camera input remains live.";
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
            if (!EditorApplication.isPlaying)
            {
                StartPendingPlayMode();
                return;
            }
            string operation = PendingOperation;
            if (string.Equals(operation, "record", StringComparison.Ordinal))
            {
                if (TryResolvePlayerPose(out _))
                    BeginRecording();
                return;
            }
            if (!string.Equals(operation, "replay", StringComparison.Ordinal))
                throw new InvalidOperationException($"Canonical Fixed input pending operation '{operation}' is invalid.");
            s_PendingReplayDocument ??= ReadDocument(
                EditorPrefs.GetString(PendingTracePathKey, string.Empty),
                true);
            BeginReplay(s_PendingReplayDocument);
        }

        static void TickActiveReplay()
        {
            FixedCharacterInputTraceStatus status = FixedCharacterInputTraceModule.Status;
            if (status.Mode == FixedCharacterInputTraceMode.Faulted)
                throw new InvalidOperationException(status.Message);
            if (status.Mode != FixedCharacterInputTraceMode.Completed)
                return;
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
                s_LastStatus = "Canonical Fixed input replay completed. Foot Landing diagnostics are finalizing.";
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
            s_LastStatus =
                $"Replay completed. Samples={CharacterFootLandingPredictionSampler.LastSavedPath}, " +
                $"Facts={CharacterFootLandingPredictionSampler.LastSavedFactsPath}, " +
                $"Diagnoses={CharacterFootLandingPredictionSampler.LastSavedDiagnosisDirectory}.";
            Debug.Log(s_LastStatus);
        }

        static void StartPendingPlayMode()
        {
            if (!IsPending || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            IGameplayLabLauncherOperations operations = RequireLauncher();
            int variantIndex = EditorPrefs.GetInt(PendingVariantKey, -1);
            ResetPendingDeadline();
            operations.Play(variantIndex);
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && IsPending)
            {
                ResetPendingDeadline();
                EditorApplication.isPaused = false;
                return;
            }
            if (state == PlayModeStateChange.EnteredEditMode && IsPending)
            {
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
                if (s_ReplayOwnsSampling &&
                    (CharacterFootLandingPredictionSampler.IsCapturing ||
                     CharacterFootLandingPredictionSampler.IsStartPending))
                {
                    CharacterFootLandingPredictionSampler.StopAndSaveSampling();
                }
                s_ReplayOwnsSampling = false;
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
            s_ReplayOwnsSampling = false;
            s_ReplayFinalizing = false;
            FixedCharacterInputTraceModule.Stop();
        }

        static void Fail(Exception exception)
        {
            s_LastFailure = exception.Message;
            s_LastStatus = $"Canonical Fixed input trace failed: {exception.Message}";
            ClearPending();
            s_ReplayWaitingForSampling = false;
            s_PendingReplayDocument = null;
            if (s_ReplayOwnsSampling &&
                (CharacterFootLandingPredictionSampler.IsCapturing ||
                 CharacterFootLandingPredictionSampler.IsStartPending))
            {
                CharacterFootLandingPredictionSampler.StopAndSaveSampling();
            }
            s_ReplayOwnsSampling = false;
            FixedCharacterInputTraceModule.Stop();
            EditorApplication.isPaused = false;
            Debug.LogException(exception);
        }

        static void RequireAvailable()
        {
            if (EditorApplication.isCompiling)
                throw new InvalidOperationException("Canonical Fixed input trace is unavailable while scripts are compiling.");
            if (IsPending || IsRecording || IsReplaying || s_ReplayWaitingForSampling ||
                CharacterFootLandingPredictionSampler.IsFinalizing)
                throw new InvalidOperationException("Another canonical Fixed input trace operation is already active.");
            if (GameplayLabFootIkKeyboardRouteDriver.IsActive || GameplayLabFootIkKeyboardRouteDriver.IsPending)
                throw new InvalidOperationException("The legacy automatic keyboard route is active and cannot share character input ownership.");
            if (CharacterFootLandingPredictionSampler.IsCapturing || CharacterFootLandingPredictionSampler.IsStartPending)
                throw new InvalidOperationException("Foot Landing sampling is already active.");
        }

        static IGameplayLabLauncherOperations RequireLauncher() =>
            GameplayLabLauncherRegistry.Operations ??
            throw new InvalidOperationException("Gameplay Lab launcher operations are not registered.");

        static bool TryResolvePlayerPose(out PoseRecord pose)
        {
            FixedCharacterHost[] hosts = UnityEngine.Object.FindObjectsByType<FixedCharacterHost>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < hosts.Length; i++)
            {
                FixedCharacterHost host = hosts[i];
                if (host == null || host.RootHierarchy == null || host.RootHierarchy.LogicRoot == null ||
                    !string.Equals(host.ActorId.Value, PlayerActorId, StringComparison.Ordinal))
                {
                    continue;
                }
                Transform root = host.RootHierarchy.LogicRoot;
                pose = new PoseRecord
                {
                    x = root.position.x,
                    y = root.position.y,
                    z = root.position.z,
                    yaw_degrees = root.eulerAngles.y
                };
                return true;
            }
            pose = null;
            return false;
        }

        static TraceDocument CreateDocument(
            FixedCharacterInputTrace trace,
            PoseRecord startPose,
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
                start_pose = startPose,
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
                frames);
        }

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

        static void ArmPending(string operation, string tracePath, int variantIndex)
        {
            EditorPrefs.SetString(PendingOperationKey, operation);
            EditorPrefs.SetString(PendingTracePathKey, tracePath ?? string.Empty);
            EditorPrefs.SetInt(PendingVariantKey, variantIndex);
            ResetPendingDeadline();
        }

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
            EditorPrefs.DeleteKey(PendingTracePathKey);
            EditorPrefs.DeleteKey(PendingVariantKey);
            EditorPrefs.DeleteKey(PendingDeadlineKey);
            s_PendingReplayDocument = null;
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
            "Temp",
            "CharacterInputTraces"));

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
