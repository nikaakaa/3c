using System;
using System.Collections.Generic;
using System.Globalization;
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
        public long evicted_event_count;
        public bool stream_complete;
        public bool succeeded;
        public string failure;
        public string evidence_hash;
    }

    internal sealed class CharacterFixedInputRuntimeTraceCapture : IDisposable
    {
        const string Schema = "character-fixed-input-runtime-trace-evidence/1";
        const float TimeTolerance = 0.0001f;
        static readonly byte[] s_FieldSeparator = { 0 };

        readonly RuntimeDiagnosticsStore m_Store;
        readonly Guid m_CaptureId;
        readonly List<RuntimeTraceEvent> m_Events = new List<RuntimeTraceEvent>(16384);
        long m_Cursor;
        bool m_Finished;

        CharacterFixedInputRuntimeTraceCapture(
            RuntimeDiagnosticsStore store,
            Guid captureId)
        {
            m_Store = store;
            m_CaptureId = captureId;
        }

        internal static CharacterFixedInputRuntimeTraceCapture Start(
            FixedCharacterHost host)
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
            RuntimeTraceChannel channels =
                RuntimeTraceChannel.Animation |
                RuntimeTraceChannel.Timeline;
            if (!store.BeginCapture(
                    channels,
                    RuntimeDiagnosticsCaptureDetail.Continuous,
                    out Guid captureId))
            {
                throw new InvalidOperationException(
                    "Fixed input replay Runtime Diagnostics capture did not start.");
            }
            return new CharacterFixedInputRuntimeTraceCapture(store, captureId);
        }

        internal void Poll()
        {
            if (m_Finished)
                return;
            RuntimeCaptureRead read = m_Store.ReadCaptureSince(m_Cursor);
            if (read.RequiresFullSync)
            {
                throw new InvalidOperationException(
                    "Fixed input replay Runtime Diagnostics stream exceeded its unread capacity.");
            }
            for (int i = 0; i < read.Changes.Count; i++)
                m_Events.Add(read.Changes[i].TraceEvent);
            m_Cursor = read.Version;
        }

        internal CharacterFixedInputRuntimeTraceEvidence Complete()
        {
            if (m_Finished)
                throw new InvalidOperationException(
                    "Fixed input replay Runtime Diagnostics capture is already complete.");
            Poll();
            RuntimeCaptureSnapshot snapshot = m_Store.EndCapture();
            m_Finished = true;
            if (snapshot == null || snapshot.CaptureId != m_CaptureId)
            {
                throw new InvalidOperationException(
                    "Fixed input replay Runtime Diagnostics capture ownership changed.");
            }
            return Analyze(m_CaptureId, snapshot.EvictedEvents, m_Events);
        }

        public void Dispose()
        {
            if (m_Finished)
                return;
            RuntimeCaptureSnapshot snapshot = m_Store.FreezeActiveCapture();
            if (snapshot != null && snapshot.CaptureId == m_CaptureId)
                m_Store.EndCapture();
            m_Finished = true;
        }

        static CharacterFixedInputRuntimeTraceEvidence Analyze(
            Guid captureId,
            long evictedEvents,
            IReadOnlyList<RuntimeTraceEvent> events)
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
            string failure = string.Empty;
            using IncrementalHash hash =
                IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            for (int i = 0; i < events.Count; i++)
            {
                RuntimeTraceEvent trace = events[i];
                AppendEventHash(hash, in trace);
                RuntimeTracePayload payload = trace.Payload;
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
                    string key = $"{payload.OwnerId}|{payload.Status}";
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

            if (timelineSamples == 0)
                SetFailure(ref failure, "Replay produced no Timeline visual samples.");
            if (animationSelections == 0 || animationSamples == 0)
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
