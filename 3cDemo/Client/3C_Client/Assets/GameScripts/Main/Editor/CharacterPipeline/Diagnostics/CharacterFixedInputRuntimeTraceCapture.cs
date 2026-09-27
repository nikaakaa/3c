using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [Serializable]
    internal sealed class CharacterFixedInputRuntimeTraceEvidence
    {
        public string schema;
        public string capture_id;
        public string trace_id;
        public string events_path;
        public string source_map_path;
        public string summary_path;
        public bool replay_completed;
        public string capture_kind;
        public bool capture_completed;
        public string input_trace_path;
        public string startup_path;
        public string foot_sample_identity;
        public string foot_capture_directory;
        public string presentation_sample_identity;
        public string presentation_capture_directory;
        public CharacterRuntimeCaptureError[] errors;
        public int event_count;
        public int timeline_visual_sample_count;
        public int timeline_visual_frame_count;
        public int timeline_time_regression_count;
        public int animation_selection_count;
        public int animation_sample_count;
        public int animation_terminal_count;
        public int animation_frame_count;
        public int animation_same_frame_selection_conflict_count;
        public int animation_sample_time_regression_count;
        public int camera_snapshot_count;
        public int camera_frame_count;
        public int camera_invalid_snapshot_count;
        public int camera_reset_count;
        public int camera_request_count;
        public int value_sample_count;
        public int value_sampling_limit_count;
        public int unsupported_value_sample_count;
        public int action_window_sample_count;
        public int action_result_count;
        public int gameplay_effect_event_count;
        public long evicted_event_count;
        public bool stream_complete;
        public bool succeeded;
        public string failure;
        public string evidence_hash;
    }

    [Serializable]
    internal sealed class CharacterRuntimeCaptureError
    {
        public int render_frame;
        public string type;
        public string message;
        public string stack_trace;
    }

    internal sealed class CharacterFixedInputRuntimeTraceCapture : IDisposable
    {
        internal const string Schema = "character-fixed-input-runtime-trace-evidence/2";
        const float TimeTolerance = 0.0001f;
        static readonly byte[] s_FieldSeparator = { 0 };

        readonly RuntimeDiagnosticsStore m_Store;
        readonly Guid m_CaptureId;
        readonly string m_TraceId;
        readonly IDebugSourceMap m_SourceMap;
        readonly string m_CaptureKind;
        string m_InputTracePath;
        readonly string m_StartupPath;
        string m_FootIdentity = string.Empty;
        string m_FootDirectory = string.Empty;
        string m_PresentationIdentity = string.Empty;
        string m_PresentationDirectory = string.Empty;
        bool m_StreamComplete = true;
        readonly List<CharacterRuntimeCaptureError> m_Errors = new List<CharacterRuntimeCaptureError>();
        readonly List<RuntimeTraceEvent> m_Events = new List<RuntimeTraceEvent>(16384);
        long m_Cursor;
        bool m_Finished;

        CharacterFixedInputRuntimeTraceCapture(
            RuntimeDiagnosticsStore store,
            Guid captureId, string traceId, IDebugSourceMap sourceMap, string captureKind, string inputTracePath)
        {
            m_Store = store;
            m_CaptureId = captureId;
            m_TraceId = traceId;
            m_SourceMap = sourceMap;
            m_CaptureKind = captureKind;
            m_InputTracePath = inputTracePath;
            m_StartupPath = CharacterInputStartupCapture.Path;
            Application.logMessageReceived += OnLog;
        }

        internal static CharacterFixedInputRuntimeTraceCapture Start(
            FixedCharacterHost host, string traceId, string captureKind, string inputTracePath)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (!RuntimeDiagnosticsTargetRegistry.TryGetByHost(
                    host.GetInstanceID(),
                    out RuntimeDiagnosticsTarget target))
            {
                throw new InvalidOperationException(
                    "Fixed input replay could not resolve its Runtime Diagnostics target.");
            }
            RuntimeDiagnosticsStore store = target.Store;
            if (store.IsCaptureRecording)
            {
                throw new InvalidOperationException(
                    "Fixed input replay requires exclusive Runtime Diagnostics capture ownership.");
            }
            RuntimeTraceChannel channels = RuntimeTraceChannel.StateMachine | RuntimeTraceChannel.Timeline | RuntimeTraceChannel.Blackboard |
                RuntimeTraceChannel.Animation | RuntimeTraceChannel.Motion | RuntimeTraceChannel.GameplayEffect |
                RuntimeTraceChannel.FootPlacement | RuntimeTraceChannel.Equipment | RuntimeTraceChannel.Values;
            if (!store.BeginCapture(
                    channels,
                    RuntimeDiagnosticsCaptureDetail.Continuous,
                    out Guid captureId))
            {
                throw new InvalidOperationException(
                    "Fixed input replay Runtime Diagnostics capture did not start.");
            }
            return new CharacterFixedInputRuntimeTraceCapture(store, captureId, traceId, target.SourceMap, captureKind, inputTracePath);
        }

        internal void BindSampling()
        {
            m_FootIdentity = CharacterFootDiagnosticSampling.CurrentSampleIdentity;
            m_FootDirectory = DiagnosticSamplingWorkflowRegistry.Require(CharacterFootDiagnosticSampling.CapabilityId).CurrentCaptureDirectory;
            var presentation = CharacterGameplayDiagnosticCapture.Presentation;
            m_PresentationIdentity = presentation.CurrentSampleIdentity;
            m_PresentationDirectory = presentation.CurrentCaptureDirectory;
        }

        internal void BindInputPath(string path) => m_InputTracePath = path;

        void OnLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;
            m_Errors.Add(new CharacterRuntimeCaptureError
            {
                render_frame = Time.frameCount, type = type.ToString(), message = message, stack_trace = stackTrace
            });
        }

        internal void Poll()
        {
            if (m_Finished)
                return;
            RuntimeCaptureRead read = m_Store.ReadCaptureSince(m_Cursor);
            if (read.RequiresFullSync)
                m_StreamComplete = false;
            for (int i = 0; i < read.Changes.Count; i++)
                if (read.Changes[i].Revision > m_Cursor)
                    m_Events.Add(read.Changes[i].TraceEvent);
            m_Cursor = read.Version;
        }

        internal CharacterFixedInputRuntimeTraceEvidence Complete() => Finish(true, string.Empty);

        internal CharacterFixedInputRuntimeTraceEvidence Abort(string reason) =>
            Finish(false, string.IsNullOrEmpty(reason) ? "Capture interrupted before replay completion." : reason);

        CharacterFixedInputRuntimeTraceEvidence Finish(bool completed, string reason)
        {
            if (m_Finished)
                throw new InvalidOperationException(
                    "Fixed input replay Runtime Diagnostics capture is already complete.");
            Poll();
            RuntimeCaptureSnapshot snapshot = m_Store.EndCapture();
            m_Finished = true;
            Application.logMessageReceived -= OnLog;
            if (snapshot == null || snapshot.CaptureId != m_CaptureId)
            {
                throw new InvalidOperationException(
                    "Fixed input replay Runtime Diagnostics capture ownership changed.");
            }
            CharacterFixedInputRuntimeTraceEvidence evidence = Analyze(m_CaptureId, snapshot.EvictedEvents, m_Events, m_CaptureKind != "record");
            evidence.trace_id = m_TraceId;
            evidence.replay_completed = completed && m_CaptureKind != "record";
            evidence.capture_kind = m_CaptureKind;
            evidence.capture_completed = completed;
            evidence.input_trace_path = m_InputTracePath;
            evidence.startup_path = m_StartupPath;
            evidence.foot_sample_identity = m_FootIdentity;
            evidence.foot_capture_directory = m_FootDirectory;
            evidence.presentation_sample_identity = m_PresentationIdentity;
            evidence.presentation_capture_directory = m_PresentationDirectory;
            evidence.errors = m_Errors.ToArray();
            if (m_Errors.Count > 0)
            {
                evidence.succeeded = false;
                evidence.failure = m_Errors[0].message;
            }
            evidence.stream_complete = m_StreamComplete && evidence.value_sampling_limit_count == 0;
            if (!evidence.stream_complete)
            {
                evidence.succeeded = false;
                evidence.failure = "Runtime Diagnostics capture exceeded a stream or value sampling limit; retained events are incomplete.";
            }
            if (!completed)
            {
                evidence.succeeded = false;
                evidence.failure = reason;
            }
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Diagnostics",
                "CharacterRuntimeTraces", m_TraceId, m_CaptureId.ToString("N")));
            Directory.CreateDirectory(directory);
            evidence.events_path = Path.Combine(directory, "events.jsonl");
            evidence.source_map_path = Path.Combine(directory, "source-map.json");
            evidence.summary_path = Path.Combine(directory, "summary.json");
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new TraceVectorConverter());
            using (var writer = new StreamWriter(evidence.events_path, false, new UTF8Encoding(false)))
                for (int index = 0; index < m_Events.Count; index++)
                    writer.WriteLine(JsonConvert.SerializeObject(m_Events[index], settings));
            File.WriteAllText(evidence.source_map_path, JsonConvert.SerializeObject(new
            {
                m_SourceMap.Revision, m_SourceMap.Entries, m_SourceMap.GraphInvocations
            }, Formatting.Indented, settings), new UTF8Encoding(false));
            File.WriteAllText(evidence.summary_path, JsonConvert.SerializeObject(evidence, Formatting.Indented), new UTF8Encoding(false));
            return evidence;
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLog;
            if (m_Finished)
                return;
            RuntimeCaptureSnapshot snapshot = m_Store.FreezeActiveCapture();
            if (snapshot != null && snapshot.CaptureId == m_CaptureId)
                m_Store.EndCapture();
            m_Finished = true;
        }

        sealed class TraceVectorConverter : JsonConverter
        {
            public override bool CanRead => false;
            public override bool CanConvert(Type type) => type == typeof(Vector2) || type == typeof(Vector3) ||
                type == typeof(Vector4) || type == typeof(Quaternion);
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                Vector4 vector = value switch
                {
                    Vector2 v => new Vector4(v.x, v.y, 0f, 0f),
                    Vector3 v => new Vector4(v.x, v.y, v.z, 0f),
                    Vector4 v => v,
                    Quaternion q => new Vector4(q.x, q.y, q.z, q.w),
                    _ => throw new ArgumentException("Unsupported trace vector.", nameof(value))
                };
                writer.WriteStartArray();
                writer.WriteValue(vector.x);
                writer.WriteValue(vector.y);
                if (!(value is Vector2)) writer.WriteValue(vector.z);
                if (value is Vector4 || value is Quaternion) writer.WriteValue(vector.w);
                writer.WriteEndArray();
            }
            public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer) =>
                throw new NotSupportedException();
        }

        static CharacterFixedInputRuntimeTraceEvidence Analyze(
            Guid captureId,
            long evictedEvents,
            IReadOnlyList<RuntimeTraceEvent> events,
            bool requireReplayCoverage)
        {
            var timelineCursors = new Dictionary<string, SampleCursor>(StringComparer.Ordinal);
            var animationCursors = new Dictionary<string, SampleCursor>(StringComparer.Ordinal);
            var frameSelections = new Dictionary<SelectionSlot, string>();
            var timelineFrames = new HashSet<ulong>();
            var animationFrames = new HashSet<ulong>();
            var cameraFrames = new HashSet<ulong>();
            int timelineSamples = 0;
            int timelineRegressions = 0;
            int animationSelections = 0;
            int animationSamples = 0;
            int animationTerminals = 0;
            int animationConflicts = 0;
            int animationRegressions = 0;
            int cameraSnapshots = 0;
            int cameraInvalidSnapshots = 0;
            int cameraResets = 0;
            int cameraRequests = 0;
            int valueSamples = 0;
            int valueLimits = 0;
            int unsupportedValues = 0;
            int actionWindows = 0;
            int actionResults = 0;
            int effectEvents = 0;
            string failure = string.Empty;
            using IncrementalHash hash =
                IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            for (int i = 0; i < events.Count; i++)
            {
                RuntimeTraceEvent trace = events[i];
                AppendEventHash(hash, in trace);
                RuntimeTracePayload payload = trace.Payload;
                if (trace.Kind == RuntimeTraceEventKind.ValueSampled)
                {
                    valueSamples++;
                    if (payload.Value.Kind == DebugValueKind.TypeOnly) unsupportedValues++;
                }
                if (trace.Kind == RuntimeTraceEventKind.ValueSamplingLimited) valueLimits++;
                if (trace.Kind == RuntimeTraceEventKind.ActionWindowSampled) actionWindows++;
                if (trace.Kind == RuntimeTraceEventKind.ActionResultSubmitted) actionResults++;
                if (trace.Channel == RuntimeTraceChannel.GameplayEffect) effectEvents++;
                if (trace.Kind == RuntimeTraceEventKind.TimelineVisualTime)
                {
                    timelineSamples++;
                    timelineFrames.Add(trace.Position);
                    string key = BuildTimelineKey(in trace);
                    if (timelineCursors.TryGetValue(key, out SampleCursor previous) &&
                        Regressed(previous, payload.Cycle, payload.Time))
                    {
                        timelineRegressions++;
                        SetFailure(
                            ref failure,
                            $"Timeline visual time regressed at frame {trace.Position} for '{key}'.");
                    }
                    timelineCursors[key] = new SampleCursor(payload.Cycle, payload.Time);
                    continue;
                }
                if (trace.Kind == RuntimeTraceEventKind.AnimationSelectionSubmitted)
                {
                    animationSelections++;
                    animationFrames.Add(trace.Position);
                    var slot = new SelectionSlot(
                        trace.Position,
                        payload.AnimationChannelId);
                    if (frameSelections.TryGetValue(slot, out string selected) &&
                        !string.Equals(selected, payload.OwnerId, StringComparison.Ordinal))
                    {
                        animationConflicts++;
                        SetFailure(
                            ref failure,
                            $"Animation channel '{payload.AnimationChannelId}' selected multiple playbacks at frame {trace.Position}.");
                    }
                    frameSelections[slot] = payload.OwnerId ?? string.Empty;
                    continue;
                }
                if (trace.Kind == RuntimeTraceEventKind.AnimationProducerSampled)
                {
                    animationSamples++;
                    animationFrames.Add(trace.Position);
                    string key = $"{payload.OwnerId}|{payload.Status}|{payload.RelatedElementId}";
                    if (animationCursors.TryGetValue(key, out SampleCursor previous) &&
                        Regressed(previous, payload.Cycle, payload.SecondaryTime))
                    {
                        animationRegressions++;
                        SetFailure(
                            ref failure,
                            $"Animation sample time regressed at frame {trace.Position} for '{key}'.");
                    }
                    animationCursors[key] =
                        new SampleCursor(payload.Cycle, payload.SecondaryTime);
                    continue;
                }
                if (trace.Kind is RuntimeTraceEventKind.AnimationPlaybackCompleted or
                    RuntimeTraceEventKind.AnimationPlaybackReleased or
                    RuntimeTraceEventKind.AnimationPlaybackRetired)
                {
                    animationTerminals++;
                    var slot = new SelectionSlot(
                        trace.Position,
                        payload.AnimationChannelId);
                    if (frameSelections.TryGetValue(slot, out string selected) &&
                        string.Equals(selected, payload.OwnerId, StringComparison.Ordinal))
                    {
                        frameSelections.Remove(slot);
                    }
                    continue;
                }
                if (trace.Kind == RuntimeTraceEventKind.CameraSnapshot)
                {
                    cameraSnapshots++;
                    cameraFrames.Add(trace.Position);
                    if (!string.Equals(payload.Status, "Ready", StringComparison.Ordinal))
                    {
                        cameraInvalidSnapshots++;
                        SetFailure(
                            ref failure,
                            $"Camera snapshot was invalid at frame {trace.Position}.");
                    }
                    if (payload.Flag)
                        cameraResets++;
                    continue;
                }
                if (trace.Kind == RuntimeTraceEventKind.CameraRequest)
                    cameraRequests++;
            }

            if (requireReplayCoverage && timelineSamples == 0)
                SetFailure(ref failure, "Replay produced no Timeline visual samples.");
            if (requireReplayCoverage && (animationSelections == 0 || animationSamples == 0))
                SetFailure(ref failure, "Replay produced incomplete Animation selection or sample evidence.");
            if (cameraSnapshots == 0)
                SetFailure(ref failure, "Replay produced no Camera snapshots.");

            return new CharacterFixedInputRuntimeTraceEvidence
            {
                schema = Schema,
                capture_id = captureId.ToString("N"),
                event_count = events.Count,
                timeline_visual_sample_count = timelineSamples,
                timeline_visual_frame_count = timelineFrames.Count,
                timeline_time_regression_count = timelineRegressions,
                animation_selection_count = animationSelections,
                animation_sample_count = animationSamples,
                animation_terminal_count = animationTerminals,
                animation_frame_count = animationFrames.Count,
                animation_same_frame_selection_conflict_count = animationConflicts,
                animation_sample_time_regression_count = animationRegressions,
                camera_snapshot_count = cameraSnapshots,
                camera_frame_count = cameraFrames.Count,
                camera_invalid_snapshot_count = cameraInvalidSnapshots,
                camera_reset_count = cameraResets,
                camera_request_count = cameraRequests,
                value_sample_count = valueSamples,
                value_sampling_limit_count = valueLimits,
                unsupported_value_sample_count = unsupportedValues,
                action_window_sample_count = actionWindows,
                action_result_count = actionResults,
                gameplay_effect_event_count = effectEvents,
                evicted_event_count = evictedEvents,
                stream_complete = true,
                succeeded = string.IsNullOrEmpty(failure),
                failure = failure,
                evidence_hash = ToHex(hash.GetHashAndReset())
            };
        }

        static bool Regressed(SampleCursor previous, int cycle, float time) =>
            cycle < previous.Cycle ||
            cycle == previous.Cycle && time + TimeTolerance < previous.Time;

        static string BuildTimelineKey(in RuntimeTraceEvent trace)
        {
            RuntimeTracePayload payload = trace.Payload;
            RuntimeTimelinePlaybackProvenance source = payload.TimelinePlayback;
            return $"{trace.RuntimeInstance.TimelinePlaybackId}/{source.SourceGraphAuthoringId}/{source.SourceNodeAuthoringId}/{source.SourceInvocationPath}/{source.SourceOperationIndex}/{payload.ActionInstanceId}/{payload.Name}";
        }

        static void SetFailure(ref string failure, string value)
        {
            if (string.IsNullOrEmpty(failure))
                failure = value;
        }

        static void AppendEventHash(
            IncrementalHash hash,
            in RuntimeTraceEvent trace)
        {
            RuntimeTracePayload payload = trace.Payload;
            AppendHash(hash, trace.Domain.ToString());
            AppendHash(hash, trace.Channel.ToString());
            AppendHash(hash, trace.Position.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, trace.Kind.ToString());
            AppendHash(hash, payload.Status);
            AppendHash(hash, payload.Name);
            AppendHash(hash, payload.Cause);
            AppendHash(hash, payload.AnimationChannelId);
            AppendHash(hash, payload.OwnerId);
            AppendHash(hash, payload.RelatedElementId);
            AppendHash(hash, payload.ActionInstanceId.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.ActivationGeneration.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.InputSequence.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.Time.ToString("R", CultureInfo.InvariantCulture));
            AppendHash(hash, payload.SecondaryTime.ToString("R", CultureInfo.InvariantCulture));
            AppendHash(hash, payload.Weight.ToString("R", CultureInfo.InvariantCulture));
            AppendHash(hash, payload.Priority.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.Cycle.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.Flag.ToString());
            AppendHash(hash, payload.Detail);
            AppendHash(hash, payload.StartTick.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.EndTick.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.Revision.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.ActionPhase.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.ActionState.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.ActionResult.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.LifecycleOperation.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.StackCount.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.Value.Kind.ToString());
            AppendHash(hash, payload.Value.Boolean.ToString());
            AppendHash(hash, payload.Value.Signed.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.Value.Unsigned.ToString(CultureInfo.InvariantCulture));
            AppendHash(hash, payload.Value.Number.ToString("R", CultureInfo.InvariantCulture));
            AppendHash(hash, payload.Value.Text);
            for (int index = 0; index < 4; index++)
                AppendHash(hash, payload.Value.Vector[index].ToString("R", CultureInfo.InvariantCulture));
        }

        static void AppendHash(IncrementalHash hash, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            hash.AppendData(bytes);
            hash.AppendData(s_FieldSeparator);
        }

        static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        readonly struct SampleCursor
        {
            internal SampleCursor(int cycle, float time)
            {
                Cycle = cycle;
                Time = time;
            }

            internal int Cycle { get; }
            internal float Time { get; }
        }

        readonly struct SelectionSlot : IEquatable<SelectionSlot>
        {
            internal SelectionSlot(ulong frame, string channel)
            {
                Frame = frame;
                Channel = channel ?? string.Empty;
            }

            readonly ulong Frame;
            readonly string Channel;

            public bool Equals(SelectionSlot other) =>
                Frame == other.Frame &&
                string.Equals(Channel, other.Channel, StringComparison.Ordinal);

            public override bool Equals(object obj) =>
                obj is SelectionSlot other && Equals(other);

            public override int GetHashCode() =>
                unchecked((Frame.GetHashCode() * 397) ^ Channel.GetHashCode());
        }
    }
}
