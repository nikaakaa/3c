using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFixedInputPresentationScheduleBinding
    {
        internal CharacterFixedInputPresentationScheduleBinding(
            string traceId,
            string traceContentHash,
            string actorId,
            string programHash,
            int tickRate,
            int traceFrameCount,
            int launcherVariantIndex)
        {
            if (string.IsNullOrWhiteSpace(traceId) ||
                string.IsNullOrWhiteSpace(traceContentHash) ||
                string.IsNullOrWhiteSpace(actorId) ||
                string.IsNullOrWhiteSpace(programHash) ||
                tickRate <= 0 || traceFrameCount <= 0 ||
                launcherVariantIndex < 0)
            {
                throw new ArgumentException(
                    "Fixed Input Presentation Schedule binding is incomplete.");
            }
            TraceId = traceId;
            TraceContentHash = traceContentHash;
            ActorId = actorId;
            ProgramHash = programHash;
            TickRate = tickRate;
            TraceFrameCount = traceFrameCount;
            LauncherVariantIndex = launcherVariantIndex;
        }

        internal string TraceId { get; }
        internal string TraceContentHash { get; }
        internal string ActorId { get; }
        internal string ProgramHash { get; }
        internal int TickRate { get; }
        internal int TraceFrameCount { get; }
        internal int LauncherVariantIndex { get; }
    }

    internal readonly struct CharacterFixedInputPresentationScheduleRepresentativeEvidence
    {
        internal CharacterFixedInputPresentationScheduleRepresentativeEvidence(
            int acceptedEnvelopeRowCount,
            int corridorOutsideEnvelopeRowCount,
            int clampAboveTenCentimetersOutsideCorridorCount,
            float maximumOutsideCorridorClampMeters,
            int verticalEndpointEventCount,
            int representativeFrameSequence,
            string representativeSide,
            int landingSurfaceIdentity,
            int verticalSurfaceIdentity,
            float landingHeight,
            float verticalEdgeUpperHeight,
            float verticalSeparationMeters)
        {
            if (acceptedEnvelopeRowCount <= 0 ||
                corridorOutsideEnvelopeRowCount <= 0 ||
                clampAboveTenCentimetersOutsideCorridorCount <= 0 ||
                !float.IsFinite(maximumOutsideCorridorClampMeters) ||
                maximumOutsideCorridorClampMeters <= 0.1f ||
                verticalEndpointEventCount <= 0 ||
                representativeFrameSequence <= 0 ||
                string.IsNullOrWhiteSpace(representativeSide) ||
                landingSurfaceIdentity == 0 || verticalSurfaceIdentity == 0 ||
                !float.IsFinite(landingHeight) ||
                !float.IsFinite(verticalEdgeUpperHeight) ||
                !float.IsFinite(verticalSeparationMeters) ||
                verticalSeparationMeters <= 0.1f)
            {
                throw new ArgumentException(
                    "Presentation Schedule representative Foot evidence is incomplete.");
            }
            AcceptedEnvelopeRowCount = acceptedEnvelopeRowCount;
            CorridorOutsideEnvelopeRowCount = corridorOutsideEnvelopeRowCount;
            ClampAboveTenCentimetersOutsideCorridorCount =
                clampAboveTenCentimetersOutsideCorridorCount;
            MaximumOutsideCorridorClampMeters =
                maximumOutsideCorridorClampMeters;
            VerticalEndpointEventCount = verticalEndpointEventCount;
            RepresentativeFrameSequence = representativeFrameSequence;
            RepresentativeSide = representativeSide.Trim();
            LandingSurfaceIdentity = landingSurfaceIdentity;
            VerticalSurfaceIdentity = verticalSurfaceIdentity;
            LandingHeight = landingHeight;
            VerticalEdgeUpperHeight = verticalEdgeUpperHeight;
            VerticalSeparationMeters = verticalSeparationMeters;
        }

        internal int AcceptedEnvelopeRowCount { get; }
        internal int CorridorOutsideEnvelopeRowCount { get; }
        internal int ClampAboveTenCentimetersOutsideCorridorCount { get; }
        internal float MaximumOutsideCorridorClampMeters { get; }
        internal int VerticalEndpointEventCount { get; }
        internal int RepresentativeFrameSequence { get; }
        internal string RepresentativeSide { get; }
        internal int LandingSurfaceIdentity { get; }
        internal int VerticalSurfaceIdentity { get; }
        internal float LandingHeight { get; }
        internal float VerticalEdgeUpperHeight { get; }
        internal float VerticalSeparationMeters { get; }
    }

    internal sealed class CharacterFixedInputPresentationSchedule
    {
        internal const string Schema =
            "character-fixed-input-presentation-schedule/1";
        readonly ScheduleDocument m_Document;

        CharacterFixedInputPresentationSchedule(ScheduleDocument document)
        {
            m_Document = document ?? throw new ArgumentNullException(nameof(document));
            ValidateDocument(document);
        }

        internal string ScheduleId => m_Document.schedule_id;
        internal string ContentHash => m_Document.content_hash;
        internal string TraceId => m_Document.trace_id;
        internal string TraceContentHash => m_Document.trace_content_hash;
        internal int FrameCount => m_Document.frame_count;
        internal int TraceFrameCount => m_Document.trace_frame_count;
        internal IReadOnlyList<GameplayScriptedPresentationFrame> Frames =>
            m_Document.frames.Select(ToRuntimeFrame).ToArray();
        internal string Path { get; private set; } = string.Empty;

        internal static CharacterFixedInputPresentationSchedule Create(
            in CharacterFixedInputPresentationScheduleBinding binding,
            IReadOnlyList<GameplayPresentationScheduleFrame> frames,
            in CharacterFixedInputPresentationScheduleRepresentativeEvidence evidence)
        {
            if (frames == null || frames.Count == 0)
                throw new ArgumentException("Presentation Schedule has no frames.", nameof(frames));
            var documents = new ScheduleFrameDocument[frames.Count];
            ulong previousRelativeEnd = 0;
            for (int i = 0; i < frames.Count; i++)
            {
                GameplayPresentationScheduleFrame frame = frames[i];
                if (frame.DriveMode !=
                    GameplayTickDriveMode.LivePresentationScheduleCapture ||
                    frame.FrameIndex != i ||
                    frame.RelativeStartLocalLogicTick != previousRelativeEnd)
                {
                    throw new InvalidDataException(
                        $"Live Presentation Schedule frame {i} is discontinuous.");
                }
                documents[i] = FromRuntimeFrame(in frame);
                previousRelativeEnd = frame.RelativeEndLocalLogicTick;
            }
            if (previousRelativeEnd != (ulong)binding.TraceFrameCount)
                throw new InvalidDataException(
                    "Live Presentation Schedule does not close the Fixed Trace frame count.");
            var document = new ScheduleDocument
            {
                schema = Schema,
                schedule_id = Guid.NewGuid().ToString("N"),
                created_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                trace_id = binding.TraceId,
                trace_content_hash = binding.TraceContentHash,
                actor_id = binding.ActorId,
                program_hash = binding.ProgramHash,
                tick_rate = binding.TickRate,
                trace_frame_count = binding.TraceFrameCount,
                launcher_variant_index = binding.LauncherVariantIndex,
                source = "canonical-live-replay",
                start_local_logic_tick = frames[0].StartLocalLogicTick,
                end_local_logic_tick = frames[frames.Count - 1].EndLocalLogicTick,
                frame_count = documents.Length,
                representative = FromEvidence(in evidence),
                frames = documents
            };
            document.content_hash = ComputeHash(document);
            return new CharacterFixedInputPresentationSchedule(document);
        }

        internal static CharacterFixedInputPresentationSchedule Load(
            string path,
            in CharacterFixedInputPresentationScheduleBinding binding)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException(
                    "Fixed Input Presentation Schedule is unavailable.", path);
            ScheduleDocument document = JsonConvert.DeserializeObject<ScheduleDocument>(
                File.ReadAllText(path, Encoding.UTF8));
            var schedule = new CharacterFixedInputPresentationSchedule(document);
            schedule.RequireBinding(in binding);
            schedule.Path = path;
            return schedule;
        }

        internal string Save()
        {
            string directory = ResolveDirectory(TraceId);
            Directory.CreateDirectory(directory);
            string path = System.IO.Path.Combine(
                directory,
                $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{ScheduleId}.json");
            string part = path + ".part";
            File.WriteAllText(
                part,
                JsonConvert.SerializeObject(m_Document, Formatting.Indented),
                new UTF8Encoding(false));
            File.Move(part, path);
            Path = path;
            return path;
        }

        internal static string FindLatestPath(string traceId)
        {
            string directory = ResolveDirectory(traceId);
            return Directory.Exists(directory)
                ? Directory.EnumerateFiles(directory, "*.json")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault() ?? string.Empty
                : string.Empty;
        }

        void RequireBinding(
            in CharacterFixedInputPresentationScheduleBinding binding)
        {
            if (!string.Equals(TraceId, binding.TraceId, StringComparison.Ordinal) ||
                !string.Equals(
                    TraceContentHash,
                    binding.TraceContentHash,
                    StringComparison.Ordinal) ||
                !string.Equals(m_Document.actor_id, binding.ActorId, StringComparison.Ordinal) ||
                !string.Equals(m_Document.program_hash, binding.ProgramHash, StringComparison.Ordinal) ||
                m_Document.tick_rate != binding.TickRate ||
                TraceFrameCount != binding.TraceFrameCount ||
                m_Document.launcher_variant_index != binding.LauncherVariantIndex)
            {
                throw new InvalidDataException(
                    "Presentation Schedule does not match the Fixed Trace binding.");
            }
        }

        static void ValidateDocument(ScheduleDocument document)
        {
            if (document == null || document.schema != Schema ||
                string.IsNullOrWhiteSpace(document.schedule_id) ||
                string.IsNullOrWhiteSpace(document.trace_id) ||
                string.IsNullOrWhiteSpace(document.trace_content_hash) ||
                document.trace_frame_count <= 0 || document.frame_count <= 0 ||
                document.frames == null ||
                document.frames.Length != document.frame_count ||
                document.representative == null ||
                !string.Equals(
                    document.content_hash,
                    ComputeHash(document),
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Fixed Input Presentation Schedule document is invalid.");
            }
            ulong previousEnd = 0;
            for (int i = 0; i < document.frames.Length; i++)
            {
                ScheduleFrameDocument frame = document.frames[i];
                if (frame == null || frame.frame_index != i ||
                    frame.relative_start_local_logic_tick != previousEnd ||
                    frame.relative_end_local_logic_tick < previousEnd ||
                    frame.logic_tick_count !=
                    checked((int)(frame.relative_end_local_logic_tick - previousEnd)))
                {
                    throw new InvalidDataException(
                        $"Presentation Schedule frame {i} is invalid.");
                }
                _ = ToRuntimeFrame(frame);
                previousEnd = frame.relative_end_local_logic_tick;
            }
            if (previousEnd != (ulong)document.trace_frame_count)
                throw new InvalidDataException(
                    "Presentation Schedule Tick closure is invalid.");
        }

        static string ResolveDirectory(string traceId) =>
            System.IO.Path.GetFullPath(System.IO.Path.Combine(
                Application.dataPath,
                "..",
                "Temp",
                "CharacterInputPresentationSchedules",
                traceId));

        static ScheduleFrameDocument FromRuntimeFrame(
            in GameplayPresentationScheduleFrame frame) =>
            new ScheduleFrameDocument
            {
                frame_index = frame.FrameIndex,
                render_frame = frame.RenderFrame,
                start_local_logic_tick = frame.StartLocalLogicTick,
                end_local_logic_tick = frame.EndLocalLogicTick,
                relative_start_local_logic_tick =
                    frame.RelativeStartLocalLogicTick,
                relative_end_local_logic_tick =
                    frame.RelativeEndLocalLogicTick,
                logic_tick_count = frame.LogicTickCount,
                scaled_delta_seconds = frame.ScaledDeltaSeconds,
                unscaled_delta_seconds = frame.UnscaledDeltaSeconds,
                presentation_delta_seconds = frame.PresentationDeltaSeconds,
                interpolation_alpha = frame.InterpolationAlpha,
                presentation_clock_mode = frame.PresentationClockMode.ToString()
            };

        static GameplayScriptedPresentationFrame ToRuntimeFrame(
            ScheduleFrameDocument frame) =>
            new GameplayScriptedPresentationFrame(
                frame.frame_index,
                frame.relative_start_local_logic_tick,
                frame.relative_end_local_logic_tick,
                frame.scaled_delta_seconds,
                frame.unscaled_delta_seconds,
                frame.presentation_delta_seconds,
                frame.interpolation_alpha,
                Enum.Parse<GameplayPresentationDebugClockMode>(
                    frame.presentation_clock_mode));

        static RepresentativeDocument FromEvidence(
            in CharacterFixedInputPresentationScheduleRepresentativeEvidence value) =>
            new RepresentativeDocument
            {
                accepted_envelope_row_count = value.AcceptedEnvelopeRowCount,
                corridor_outside_envelope_row_count =
                    value.CorridorOutsideEnvelopeRowCount,
                clamp_above_ten_centimeters_outside_corridor_count =
                    value.ClampAboveTenCentimetersOutsideCorridorCount,
                maximum_outside_corridor_clamp_meters =
                    value.MaximumOutsideCorridorClampMeters,
                vertical_endpoint_event_count = value.VerticalEndpointEventCount,
                representative_frame_sequence = value.RepresentativeFrameSequence,
                representative_side = value.RepresentativeSide,
                landing_surface_identity = value.LandingSurfaceIdentity,
                vertical_surface_identity = value.VerticalSurfaceIdentity,
                landing_height = value.LandingHeight,
                vertical_edge_upper_height = value.VerticalEdgeUpperHeight,
                vertical_separation_meters = value.VerticalSeparationMeters
            };

        static string ComputeHash(ScheduleDocument document)
        {
            using IncrementalHash hash =
                IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            Add(hash, document.schema);
            Add(hash, document.trace_id);
            Add(hash, document.trace_content_hash);
            Add(hash, document.actor_id);
            Add(hash, document.program_hash);
            Add(hash, document.tick_rate);
            Add(hash, document.trace_frame_count);
            Add(hash, document.launcher_variant_index);
            Add(hash, document.start_local_logic_tick);
            Add(hash, document.end_local_logic_tick);
            RepresentativeDocument representative = document.representative;
            Add(hash, representative.accepted_envelope_row_count);
            Add(hash, representative.corridor_outside_envelope_row_count);
            Add(hash, representative.clamp_above_ten_centimeters_outside_corridor_count);
            Add(hash, representative.maximum_outside_corridor_clamp_meters);
            Add(hash, representative.vertical_endpoint_event_count);
            Add(hash, representative.representative_frame_sequence);
            Add(hash, representative.representative_side);
            Add(hash, representative.landing_surface_identity);
            Add(hash, representative.vertical_surface_identity);
            Add(hash, representative.landing_height);
            Add(hash, representative.vertical_edge_upper_height);
            Add(hash, representative.vertical_separation_meters);
            for (int i = 0; i < document.frames.Length; i++)
            {
                ScheduleFrameDocument frame = document.frames[i];
                Add(hash, frame.frame_index);
                Add(hash, frame.render_frame);
                Add(hash, frame.start_local_logic_tick);
                Add(hash, frame.end_local_logic_tick);
                Add(hash, frame.relative_start_local_logic_tick);
                Add(hash, frame.relative_end_local_logic_tick);
                Add(hash, frame.logic_tick_count);
                Add(hash, frame.scaled_delta_seconds);
                Add(hash, frame.unscaled_delta_seconds);
                Add(hash, frame.presentation_delta_seconds);
                Add(hash, frame.interpolation_alpha);
                Add(hash, frame.presentation_clock_mode);
            }
            byte[] bytes = hash.GetHashAndReset();
            return string.Concat(bytes.Select(value =>
                value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        static void Add(IncrementalHash hash, object value)
        {
            string text = value switch
            {
                float number => number.ToString("R", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value?.ToString() ?? string.Empty
            };
            hash.AppendData(Encoding.UTF8.GetBytes(text));
            hash.AppendData(new byte[] { 0x1f });
        }

        [Serializable]
        sealed class ScheduleDocument
        {
            public string schema;
            public string schedule_id;
            public string created_utc;
            public string trace_id;
            public string trace_content_hash;
            public string actor_id;
            public string program_hash;
            public int tick_rate;
            public int trace_frame_count;
            public int launcher_variant_index;
            public string source;
            public ulong start_local_logic_tick;
            public ulong end_local_logic_tick;
            public int frame_count;
            public RepresentativeDocument representative;
            public ScheduleFrameDocument[] frames;
            public string content_hash;
        }

        [Serializable]
        sealed class ScheduleFrameDocument
        {
            public int frame_index;
            public ulong render_frame;
            public ulong start_local_logic_tick;
            public ulong end_local_logic_tick;
            public ulong relative_start_local_logic_tick;
            public ulong relative_end_local_logic_tick;
            public int logic_tick_count;
            public float scaled_delta_seconds;
            public float unscaled_delta_seconds;
            public float presentation_delta_seconds;
            public float interpolation_alpha;
            public string presentation_clock_mode;
        }

        [Serializable]
        sealed class RepresentativeDocument
        {
            public int accepted_envelope_row_count;
            public int corridor_outside_envelope_row_count;
            public int clamp_above_ten_centimeters_outside_corridor_count;
            public float maximum_outside_corridor_clamp_meters;
            public int vertical_endpoint_event_count;
            public int representative_frame_sequence;
            public string representative_side;
            public int landing_surface_identity;
            public int vertical_surface_identity;
            public float landing_height;
            public float vertical_edge_upper_height;
            public float vertical_separation_meters;
        }
    }

    internal sealed class CharacterFixedInputPresentationScheduleRun :
        IGameplayPresentationScheduleFrameTarget,
        IDisposable
    {
        readonly List<GameplayPresentationScheduleFrame> m_ObservedFrames =
            new List<GameplayPresentationScheduleFrame>();
        readonly IReadOnlyList<GameplayScriptedPresentationFrame> m_ScriptedFrames;
        readonly Action m_FormalWindowComplete;
        readonly bool m_Capturing;
        bool m_Registered;
        bool m_EndQueued;
        bool m_FormalWindowClosed;
        bool m_Disposed;

        CharacterFixedInputPresentationScheduleRun(
            IReadOnlyList<GameplayScriptedPresentationFrame> scriptedFrames,
            Action formalWindowComplete)
        {
            m_ScriptedFrames = scriptedFrames;
            m_FormalWindowComplete = formalWindowComplete ??
                throw new ArgumentNullException(nameof(formalWindowComplete));
            m_Capturing = scriptedFrames == null;
        }

        internal IReadOnlyList<GameplayPresentationScheduleFrame> ObservedFrames =>
            m_ObservedFrames;
        internal bool Completed { get; private set; }
        internal string Failure { get; private set; } = string.Empty;
        internal bool DriveRestored =>
            GameplayTickSystem.IsInitialized &&
            !GameplayTickSystem.Current.DriveStatus.PresentationScheduleDriveActive;

        internal static CharacterFixedInputPresentationScheduleRun StartCapture(
            Action formalWindowComplete)
        {
            var run = new CharacterFixedInputPresentationScheduleRun(
                null,
                formalWindowComplete);
            run.Register();
            Enqueue(
                GameplayTickDriveCommand.BeginLivePresentationScheduleCapture());
            return run;
        }

        internal static CharacterFixedInputPresentationScheduleRun StartReplay(
            CharacterFixedInputPresentationSchedule schedule,
            Action formalWindowComplete)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));
            var run = new CharacterFixedInputPresentationScheduleRun(
                schedule.Frames,
                formalWindowComplete);
            run.Register();
            Enqueue(
                GameplayTickDriveCommand.BeginScriptedPresentationSchedule());
            Enqueue(GameplayTickDriveCommand.ScriptedFrame(run.m_ScriptedFrames[0]));
            return run;
        }

        public void PresentationScheduleFrame(
            GameplayPresentationScheduleFrame frame)
        {
            if (m_Disposed || Completed || !string.IsNullOrEmpty(Failure))
                return;
            try
            {
                if (frame.FrameIndex != m_ObservedFrames.Count)
                    throw new InvalidDataException(
                        "Presentation Schedule frame index is discontinuous.");
                if (m_ObservedFrames.Count != 0 &&
                    frame.RelativeStartLocalLogicTick !=
                    m_ObservedFrames[m_ObservedFrames.Count - 1]
                        .RelativeEndLocalLogicTick)
                {
                    throw new InvalidDataException(
                        "Presentation Schedule relative Logic Tick is discontinuous.");
                }
                m_ObservedFrames.Add(frame);
                FixedCharacterInputTraceStatus trace =
                    FixedCharacterInputTraceModule.Status;
                if (m_Capturing)
                {
                    if (trace.Mode == FixedCharacterInputTraceMode.Completed)
                        Complete(frame.RelativeEndLocalLogicTick, trace.FrameCount);
                    return;
                }
                RequireScriptedFrame(frame, m_ScriptedFrames[frame.FrameIndex]);
                if (frame.FrameIndex + 1 < m_ScriptedFrames.Count)
                {
                    if (trace.Mode == FixedCharacterInputTraceMode.Completed)
                        throw new InvalidDataException(
                            "Fixed Trace completed before the Presentation Schedule.");
                    Enqueue(GameplayTickDriveCommand.ScriptedFrame(
                        m_ScriptedFrames[frame.FrameIndex + 1]));
                    return;
                }
                if (trace.Mode != FixedCharacterInputTraceMode.Completed)
                    throw new InvalidDataException(
                        "Presentation Schedule completed before the Fixed Trace.");
                Complete(frame.RelativeEndLocalLogicTick, trace.FrameCount);
            }
            catch (Exception exception)
            {
                Failure = exception.Message;
                CloseFormalWindow();
                QueueEnd();
            }
        }

        internal void Stop()
        {
            QueueEnd();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            CloseFormalWindow();
            if (m_Registered)
                GameplayTickSystem.UnregisterPresentationScheduleTarget(this);
            if (!m_EndQueued && GameplayTickSystem.IsInitialized &&
                GameplayTickSystem.Current.DriveStatus.PresentationScheduleDriveActive)
            {
                Enqueue(GameplayTickDriveCommand.EndPresentationSchedule());
            }
        }

        void Complete(ulong relativeEnd, int traceFrameCount)
        {
            if (relativeEnd != (ulong)traceFrameCount)
                throw new InvalidDataException(
                    "Presentation Schedule does not close the Fixed Trace.");
            Completed = true;
            CloseFormalWindow();
            QueueEnd();
        }

        void CloseFormalWindow()
        {
            if (m_FormalWindowClosed)
                return;
            m_FormalWindowClosed = true;
            m_FormalWindowComplete();
        }

        void QueueEnd()
        {
            if (m_EndQueued || !GameplayTickSystem.IsInitialized)
                return;
            Enqueue(GameplayTickDriveCommand.EndPresentationSchedule());
            m_EndQueued = true;
        }

        void Register()
        {
            if (!GameplayTickSystem.RegisterPresentationScheduleTarget(this))
                throw new InvalidOperationException(
                    "Gameplay Tick System rejected the Presentation Schedule target.");
            m_Registered = true;
        }

        static void RequireScriptedFrame(
            GameplayPresentationScheduleFrame actual,
            GameplayScriptedPresentationFrame expected)
        {
            if (actual.DriveMode != GameplayTickDriveMode.ScriptedPresentationFrame ||
                actual.FrameIndex != expected.FrameIndex ||
                actual.RelativeStartLocalLogicTick !=
                expected.RelativeStartLocalLogicTick ||
                actual.RelativeEndLocalLogicTick !=
                expected.RelativeEndLocalLogicTick ||
                BitConverter.SingleToInt32Bits(actual.ScaledDeltaSeconds) !=
                BitConverter.SingleToInt32Bits(expected.ScaledDeltaSeconds) ||
                BitConverter.SingleToInt32Bits(actual.UnscaledDeltaSeconds) !=
                BitConverter.SingleToInt32Bits(expected.UnscaledDeltaSeconds) ||
                BitConverter.SingleToInt32Bits(actual.PresentationDeltaSeconds) !=
                BitConverter.SingleToInt32Bits(expected.PresentationDeltaSeconds) ||
                BitConverter.SingleToInt32Bits(actual.InterpolationAlpha) !=
                BitConverter.SingleToInt32Bits(expected.InterpolationAlpha) ||
                actual.PresentationClockMode != expected.PresentationClockMode)
            {
                throw new InvalidDataException(
                    $"Presentation Schedule diverged at frame {expected.FrameIndex}.");
            }
        }

        static void Enqueue(GameplayTickDriveCommand command)
        {
            if (!GameplayTickSystem.EnqueueDriveCommand(command))
                throw new InvalidOperationException(
                    "Gameplay Tick System rejected a Presentation Schedule command.");
        }
    }
}
