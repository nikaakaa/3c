using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using ThirdPersonSimulation.Fixed;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [McpForUnityTool(
        "character.fixed_input_trace",
        Description = "Record canonical character input per Fixed simulation Tick, capture or consume a Live Presentation Schedule, list saved traces, or replay one trace while keeping camera controls live.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = false,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = false,
        IdempotentHint = false,
        OpenWorldHint = false)]
    public static class CharacterFixedInputTraceMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: record_start, record_stop, replay_last, replay_start, diagnostic_replay_start, schedule_record_start, schedule_replay_start, list_traces, inspect_trace, status, or stop. Defaults to status.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Exact trace_id returned by record_stop or list_traces. Used by replay_start; omitted means latest.", Required = false)]
            public string trace_id { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            string action = @params?["action"]?.Value<string>() ?? "status";
            string traceId = @params?["trace_id"]?.Value<string>() ?? string.Empty;
            try
            {
                switch (action.Trim().ToLowerInvariant())
                {
                    case "record_start":
                        CharacterFixedInputTraceWorkflow.StartRecording();
                        return Success("Canonical Fixed input recording requested.", false);
                    case "record_stop":
                        CharacterFixedInputTraceWorkflow.StopAndSaveRecording();
                        return Success("Canonical Fixed input trace saved.", false);
                    case "replay_last":
                        CharacterFixedInputTraceWorkflow.ReplayLast();
                        return Success("Latest canonical Fixed input replay requested.", false);
                    case "replay_start":
                        CharacterFixedInputTraceWorkflow.ReplayTrace(traceId);
                        return Success("Canonical Fixed input replay requested.", false);
                    case "diagnostic_replay_start":
                        if (string.IsNullOrWhiteSpace(traceId))
                            CharacterFixedInputTraceWorkflow
                                .ReplayLastWithDiagnostics();
                        else
                            CharacterFixedInputTraceWorkflow
                                .ReplayTraceWithDiagnostics(traceId);
                        return Success(
                            "Canonical Fixed input replay with Foot diagnostics requested.",
                            false);
                    case "schedule_record_start":
                        CharacterFixedInputTraceWorkflow.RecordPresentationSchedule(
                            traceId);
                        return Success(
                            "Canonical Live Presentation Schedule capture requested.",
                            false);
                    case "schedule_replay_start":
                        CharacterFixedInputTraceWorkflow
                            .ReplayWithPresentationSchedule(traceId);
                        return Success(
                            "Scripted Presentation Schedule replay requested.",
                            false);
                    case "list_traces":
                        return Success("Canonical Fixed input traces listed.", true);
                    case "inspect_trace":
                        return InspectTrace(traceId);
                    case "status":
                        return Success("Canonical Fixed input trace status.", false);
                    case "stop":
                        CharacterFixedInputTraceWorkflow.Stop();
                        return Success("Canonical Fixed input replay or pending operation stopped.", false);
                    default:
                        return new ErrorResponse("invalid_action", new { action });
                }
            }
            catch (Exception exception)
            {
                return new ErrorResponse(
                    "fixed_input_trace_failed",
                    new { action, trace_id = traceId, message = exception.Message });
            }
        }

        static object InspectTrace(string traceId)
        {
            if (string.IsNullOrWhiteSpace(traceId))
                throw new ArgumentException("inspect_trace requires an exact trace_id.");
            FixedCharacterInputTrace trace =
                CharacterFixedInputTraceWorkflow.ReadSavedTrace(traceId);
            return new
            {
                success = true,
                message = "Canonical Fixed input trace coverage decoded.",
                data = new
                {
                    trace_id = trace.TraceId,
                    frame_count = trace.Frames.Count,
                    tick_rate = trace.TickRate,
                    values = trace.Frames.SelectMany(frame => frame.Input.Values)
                        .GroupBy(value => new { value.InputId, value.Kind })
                        .OrderBy(group => group.Key.InputId, StringComparer.Ordinal)
                        .ThenBy(group => group.Key.Kind)
                        .Select(group => new
                        {
                            input_id = group.Key.InputId,
                            kind = group.Key.Kind.ToString(),
                            frame_count = group.Count(),
                            boolean_true_frame_count = group.Key.Kind ==
                                SimulationInputValueKind.Boolean
                                ? (int?)group.Count(value => value.Boolean) : null
                        }).ToArray(),
                    requests = trace.Frames.SelectMany(frame => frame.Input.Requests)
                        .GroupBy(request => request.RequestId)
                        .OrderBy(group => group.Key, StringComparer.Ordinal)
                        .Select(group => new
                        {
                            request_id = group.Key,
                            occurrence_count = group.Count(),
                            distinct_sequence_count = group.Select(request =>
                                request.Sequence).Distinct().Count()
                        }).ToArray()
                }
            };
        }

        static object Success(string message, bool includeTraces)
        {
            FixedCharacterInputTraceStatus status = FixedCharacterInputTraceModule.Status;
            CharacterFixedInputTraceSummary[] traces = includeTraces
                ? CharacterFixedInputTraceWorkflow.ListTraces().ToArray()
                : Array.Empty<CharacterFixedInputTraceSummary>();
            return new
            {
                success = true,
                message,
                data = new
                {
                    playing = EditorApplication.isPlaying,
                    paused = EditorApplication.isPaused,
                    pending = CharacterFixedInputTraceWorkflow.IsPending,
                    pending_operation = CharacterFixedInputTraceWorkflow.PendingOperation,
                    mode = status.Mode.ToString(),
                    trace_id = status.TraceId,
                    actor_id = status.ActorId,
                    frame_count = status.FrameCount,
                    replayed_frame_count = status.ReplayedFrameCount,
                    replay_tick_drive_owned =
                        CharacterFixedInputTraceWorkflow
                            .OwnsReplayTickDrive,
                    replay_issued_tick_count =
                        CharacterFixedInputTraceWorkflow
                            .ReplayIssuedTickCount,
                    trace_status = status.Message,
                    workflow_status = CharacterFixedInputTraceWorkflow.LastStatus,
                    failure = CharacterFixedInputTraceWorkflow.LastFailure,
                    camera_control_enabled = true,
                    trace_directory = CharacterFixedInputTraceWorkflow.TraceDirectory,
                    last_trace_id = CharacterFixedInputTraceWorkflow.LastTraceId,
                    last_trace_path = CharacterFixedInputTraceWorkflow.LastTracePath,
                    presentation_schedule_path =
                        CharacterFixedInputTraceWorkflow
                            .LastPresentationSchedulePath,
                    replay_proof_path =
                        CharacterFixedInputTraceWorkflow.LastReplayProofPath,
                    replay_comparison =
                        CharacterFixedInputTraceWorkflow.LastReplayComparison,
                    foot_sampling_available = CharacterFootDiagnosticSampling.IsAvailable,
                    foot_sampling = CharacterFootDiagnosticSampling.IsCapturing,
                    foot_sampling_finalizing = CharacterFootDiagnosticSampling.IsFinalizing,
                    samples_path = CharacterFootDiagnosticSampling.LastSavedPath,
                    manifest_path = CharacterFootDiagnosticSampling.LastManifestPath,
                    ground_contacts_path = CharacterFootDiagnosticSampling.GetArtifactPath("ground-contacts"),
                    ground_envelope_path = CharacterFootDiagnosticSampling.GetArtifactPath("ground-envelope"),
                    ground_surfaces_path = CharacterFootDiagnosticSampling.GetArtifactPath("ground-surfaces"),
                    traces = traces.Select(value => new
                    {
                        trace_id = value.TraceId,
                        path = value.Path,
                        created_utc = value.CreatedUtc,
                        frame_count = value.FrameCount,
                        tick_rate = value.TickRate
                    }).ToArray()
                }
            };
        }
    }
}
