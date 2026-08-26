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
        Description = "Record canonical character input per Fixed simulation Tick, list saved JSON traces, or replay one trace while keeping camera controls live. Replay restarts the recorded Gameplay Lab variant, owns character input, automatically captures Foot Landing samples, and publishes samples.csv, facts.json, and diagnoses/ at completion.",
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
            [ToolParameter("Action: record_start, record_stop, replay_last, replay_start, list_traces, status, or stop. Defaults to status.", Required = false)]
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
                    case "list_traces":
                        return Success("Canonical Fixed input traces listed.", true);
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
                    pending = CharacterFixedInputTraceWorkflow.IsPending,
                    pending_operation = CharacterFixedInputTraceWorkflow.PendingOperation,
                    mode = status.Mode.ToString(),
                    trace_id = status.TraceId,
                    actor_id = status.ActorId,
                    frame_count = status.FrameCount,
                    replayed_frame_count = status.ReplayedFrameCount,
                    trace_status = status.Message,
                    workflow_status = CharacterFixedInputTraceWorkflow.LastStatus,
                    failure = CharacterFixedInputTraceWorkflow.LastFailure,
                    camera_control_enabled = true,
                    trace_directory = CharacterFixedInputTraceWorkflow.TraceDirectory,
                    last_trace_id = CharacterFixedInputTraceWorkflow.LastTraceId,
                    last_trace_path = CharacterFixedInputTraceWorkflow.LastTracePath,
                    foot_sampling = CharacterFootLandingPredictionSampler.IsCapturing,
                    foot_sampling_finalizing = CharacterFootLandingPredictionSampler.IsFinalizing,
                    samples_path = CharacterFootLandingPredictionSampler.LastSavedPath,
                    facts_path = CharacterFootLandingPredictionSampler.LastSavedFactsPath,
                    diagnoses_directory = CharacterFootLandingPredictionSampler.LastSavedDiagnosisDirectory,
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
