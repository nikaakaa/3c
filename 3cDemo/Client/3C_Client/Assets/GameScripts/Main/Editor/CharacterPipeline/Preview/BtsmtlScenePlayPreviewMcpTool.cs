using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonGameplay.ScenePlay;
using ThirdPersonSimulation;
using UnityEditor;

namespace ThirdPersonCharacter.Editor.CharacterPipeline.Preview
{
    [McpForUnityTool(
        "btsmtl.scene_play",
        Description = "Control one explicit BTSMTL Scene Play request through the formal preview Coordinator and observe its Session, Skill, Build and restore state.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = false,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = false,
        IdempotentHint = false,
        OpenWorldHint = false)]
    public static class BtsmtlScenePlayPreviewMcpTool
    {
        static readonly object s_InterestOwner = new object();

        public sealed class Parameters
        {
            [ToolParameter("Action: start, status, attach, capture_start, capture_stop, timeline, history, pause, resume, reset, stop, request_skill, build, record_start, record_stop, restore or replay.", Required = false)]
            public string action { get; set; }

            [ToolParameter("Exact saved Assets/... Scene path for start.", Required = false)]
            public string scene_path { get; set; }

            [ToolParameter("Exact Scene Play ContextId for start.", Required = false)]
            public string context_id { get; set; }

            [ToolParameter("Exact ActorId for request_skill or build.", Required = false)]
            public string actor_id { get; set; }

            [ToolParameter("Exact SkillId for request_skill.", Required = false)]
            public string skill_id { get; set; }

            [ToolParameter("Target Tick for restore; the formal Session chooses the nearest checkpoint at or before it.", Required = false)]
            public string tick { get; set; }

            [ToolParameter("Replay start Tick.", Required = false)]
            public string from_tick { get; set; }

            [ToolParameter("Replay end Tick.", Required = false)]
            public string to_tick { get; set; }

            [ToolParameter("Start the Scene Play request paused.", Required = false)]
            public bool start_paused { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            string action = @params?["action"]?.Value<string>()?.Trim().ToLowerInvariant() ?? "status";
            IBtsmtlScenePlayPreviewOperations operations =
                BtsmtlScenePlayPreviewOperationsRegistry.Current;
            if (operations == null)
                return Failure("scene_play_operations_missing", action, "Scene Play preview Coordinator is unavailable.");

            try
            {
                switch (action)
                {
                    case "status":
                        return new
                        {
                            success = true,
                            message = "Scene Play preview status.",
                            data = Describe(operations)
                        };
                    case "attach":
                        return Attach(operations, RequiredString(@params, "actor_id"));
                    case "capture_start":
                        return CaptureStart();
                    case "capture_stop":
                        return CaptureStop();
                    case "timeline":
                        return Timeline();
                    case "history":
                        return History();
                    case "start":
                        return Command(
                            operations.Start(
                                new BtsmtlScenePlayRequest(
                                    RequiredString(@params, "scene_path"),
                                    RequiredString(@params, "context_id"),
                                    startPaused: @params?["start_paused"]?.Value<bool>() ?? false)));
                    case "pause":
                        return Command(operations.Pause());
                    case "resume":
                        return Command(operations.Resume());
                    case "reset":
                        return Command(operations.Reset());
                    case "stop":
                        return Command(operations.Stop());
                    case "record_start":
                        return Command(operations.StartInputRecording());
                    case "record_stop":
                        return Command(operations.StopInputRecording());
                    case "request_skill":
                        return Skill(
                            operations.RequestSkill(
                                RequiredString(@params, "actor_id"),
                                RequiredString(@params, "skill_id")));
                    case "restore":
                        return Command(
                            operations.ResumeFromTick(
                                RequiredTick(@params, "tick")));
                    case "replay":
                        return Command(
                            operations.ReplayInputRange(
                                RequiredTick(@params, "from_tick"),
                                RequiredTick(@params, "to_tick")));
                    default:
                        return Failure("scene_play_invalid_action", action, "Scene Play action is unsupported.");
                }
            }
            catch (Exception exception)
            {
                return Failure("scene_play_command_failed", action, exception.Message);
            }
        }

        static object Describe(IBtsmtlScenePlayPreviewOperations operations)
        {
            BtsmtlScenePlayStatus status = operations.Status;
            return new
            {
                state = status.State.ToString(),
                operation = status.Operation.ToString(),
                scene_generation = status.SceneGeneration,
                failure_stage = status.FailureStage.ToString(),
                failure_code = status.FailureCode,
                failure_message = status.FailureMessage,
                identity = new
                {
                    request_id = status.Identity.RequestId.ToString("N"),
                    scene_path = status.Identity.ScenePath,
                    context_id = status.Identity.ContextId
                },
                skills = operations.SkillOptions.Select(option => new
                {
                    actor_id = option.ActorId,
                    skill_id = option.SkillId,
                    entry_graph_authoring_id = option.EntryGraphAuthoringId,
                    action_profile_id = option.ActionProfileId,
                    source_input_request_id = option.SourceInputRequestId
                }).ToArray(),
                input_replay = new
                {
                    supported = operations.SupportsInputReplay,
                    recording = operations.IsInputRecording
                },
                presentation_restore = operations.SupportsPresentationCheckpointRestore,
                session_history = DescribeSessionHistory(status)
            };
        }

        static object DescribeSessionHistory(BtsmtlScenePlayStatus status)
        {
            if (!status.IsActive ||
                !BtsmtlScenePlayContextRegistry.TryGet(
                    status.Identity.ScenePath,
                    status.Identity.ContextId,
                    out BtsmtlScenePlayContext context) ||
                !context.TryDescribe(
                    out BtsmtlScenePlayContextDescriptor descriptor,
                    out _))
            {
                return null;
            }
            if (!descriptor.HasCharacterRuntime)
            {
                return new
                {
                    available = false,
                    message = "The current Scene Play context has no Character Session."
                };
            }

            SimulationSessionHost session = descriptor.SessionHost;
            SimulationProgramEpoch epoch = session.ProgramEpoch;
            return new
            {
                available = true,
                program_epoch = epoch.Value,
                program_revision = epoch.SourceRevision.Value,
                execution_branch_id = session.ExecutionBranchId.ToString("N"),
                parent_execution_branch_id = session.ParentExecutionBranchId.ToString("N"),
                execution_branch_base_tick = session.ExecutionBranchBaseTick,
                presentation_checkpoint_restore_supported = session.SupportsPresentationCheckpointRestore,
                checkpoint_count = session.CheckpointCount,
                oldest_checkpoint_tick = session.OldestCheckpointTick,
                latest_checkpoint_tick = session.LatestCheckpointTick,
                last_checkpoint_failure = session.LastCheckpointFailure
            };
        }

        static object Command(BtsmtlScenePlayCommandResult result) => new
        {
            success = result.Accepted,
            code = result.Code.ToString(),
            operation = result.Operation.ToString(),
            message = result.Message,
            status = DescribeStatus(result.Status)
        };


        static object Attach(
            IBtsmtlScenePlayPreviewOperations operations,
            string actorId)
        {
            BtsmtlScenePlayStatus status = operations.Status;
            if (!status.IsActive)
                return Failure("scene_play_not_active", "attach", "Scene Play is not active.");
            if (!BtsmtlScenePlayContextRegistry.TryGet(
                    status.Identity.ScenePath,
                    status.Identity.ContextId,
                    out BtsmtlScenePlayContext context))
                return Failure(
                    "scene_play_context_unavailable",
                    "attach",
                    "Scene Play Context is unavailable.");
            if (!context.TryDescribe(
                    out BtsmtlScenePlayContextDescriptor descriptor,
                    out _))
            {
                return Failure(
                    "scene_play_context_unavailable",
                    "attach",
                    "Scene Play Context is invalid or unavailable.");
            }
            for (int i = 0; i < descriptor.Actors.Count; i++)
            {
                BtsmtlScenePlayActorDescriptor actor = descriptor.Actors[i];
                if (!string.Equals(actor.ActorId.Value, actorId, StringComparison.Ordinal))
                    continue;
                if (!RuntimeDebugSession.Shared.AttachToHost(actor.Host.GetInstanceID()))
                    return Failure("scene_play_target_missing", "attach", "The selected Scene Play Actor has no Runtime Diagnostics target.");
                RuntimeDebugSession.Shared.EnsureLiveInterest(
                    s_InterestOwner,
                    RuntimeTraceChannel.All);
                return new
                {
                    success = true,
                    message = "Runtime Diagnostics attached to the selected Scene Play Actor.",
                    data = DescribeDebugSession()
                };
            }
            return Failure("scene_play_actor_missing", "attach", $"Scene Play Actor '{actorId}' is not declared by the current Context.");
        }

        static object CaptureStart()
        {
            bool accepted = RuntimeDebugSession.Shared.BeginCapture(
                RuntimeTraceChannel.All,
                RuntimeDiagnosticsCaptureDetail.Continuous);
            return accepted
                ? new
                {
                    success = true,
                    message = "Runtime Diagnostics capture started.",
                    data = DescribeDebugSession()
                }
                : Failure("scene_play_capture_unavailable", "capture_start", "Runtime Diagnostics capture requires an attached live target.");
        }

        static object CaptureStop()
        {
            if (!RuntimeDebugSession.Shared.EndCapture())
                return Failure("scene_play_capture_not_recording", "capture_stop", "Runtime Diagnostics capture is not recording.");
            return new
            {
                success = true,
                message = "Runtime Diagnostics capture stopped.",
                data = new
                {
                    debug = DescribeDebugSession(),
                    timeline = DescribeTimeline(RuntimeDebugSession.Shared.BuildExecutionTimeline()),
                    history = DescribeHistory(RuntimeDebugSession.Shared.BuildExecutionHistory())
                }
            };
        }

        static object Timeline()
        {
            RuntimeExecutionTimeline timeline = RuntimeDebugSession.Shared.BuildExecutionTimeline();
            return timeline == null
                ? Failure("scene_play_timeline_missing", "timeline", "No Runtime Diagnostics capture is available.")
                : new
                {
                    success = true,
                    message = "Runtime execution timeline built from the captured formal trace.",
                    data = DescribeTimeline(timeline)
                };
        }

        static object History()
        {
            RuntimeExecutionHistory history = RuntimeDebugSession.Shared.BuildExecutionHistory();
            return history == null
                ? Failure("scene_play_history_missing", "history", "No Runtime Diagnostics capture is available.")
                : new
                {
                    success = true,
                    message = "Runtime execution history built from the captured formal trace.",
                    data = DescribeHistory(history)
                };
        }

        static object DescribeDebugSession()
        {
            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            IReadOnlyList<RuntimeDebugEventView> actionResults =
                session.ViewModel.GetCurrentEvents(RuntimeTraceChannel.StateMachine)
                    .Where(value => value.Event.Kind == RuntimeTraceEventKind.ActionResultSubmitted)
                    .ToArray();
            return new
            {
                attachment = session.AttachmentState.ToString(),
                target_count = session.Targets.Count,
                program_epoch = session.ViewModel.Target.ProgramEpoch,
                program_revision = session.ViewModel.Target.Revision.ToString(),
                capture_recording = session.IsCaptureRecording,
                capture_segment_count = session.CaptureSegmentCount,
                history_offset = session.HistoryOffset,
                action_results = actionResults.Select(DescribeActionResult).ToArray()
            };
        }

        static object DescribeActionResult(RuntimeDebugEventView eventView)
        {
            RuntimeTraceEvent traceEvent = eventView.Event;
            RuntimeTracePayload payload = traceEvent.Payload;
            return new
            {
                tick = traceEvent.Position,
                execution_branch_id = traceEvent.ExecutionBranchId.ToString("N"),
                program_epoch = traceEvent.ProgramEpoch,
                program_revision = traceEvent.ProgramRevision.ToString(),
                skill_id = payload.SkillId,
                action_instance_id = payload.ActionInstanceId,
                input_sequence = payload.InputSequence,
                status = payload.Status,
                detail = payload.Detail,
                cause = payload.Cause,
                source = eventView.Source.ToString(),
                runtime_instance = DescribeRuntimeInstance(traceEvent.RuntimeInstance)
            };
        }

        static object DescribeTimeline(RuntimeExecutionTimeline timeline)
        {
            if (timeline == null)
                return null;
            return new
            {
                capture_id = timeline.CaptureId.ToString("N"),
                complete = timeline.IsComplete,
                evicted_events = timeline.EvictedEvents,
                unmapped_event_count = timeline.UnmappedEventCount,
                span_count = timeline.Spans.Count,
                spans = timeline.Spans.Select(span => new
                {
                    kind = span.Kind.ToString(),
                    completed = span.Completed,
                    has_source = span.HasSource,
                    start_position = span.StartPosition,
                    end_position = span.EndPosition,
                    start_sequence = span.StartSequence,
                    end_sequence = span.EndSequence,
                    program_epoch = span.ProgramEpoch,
                    program_revision = span.ProgramRevision.ToString(),
                    skill_id = span.SkillId,
                    action_instance_id = span.ActionInstanceId,
                    call_site_id = span.CallSiteId,
                    activation_generation = span.ActivationGeneration,
                    graph_invocation_generation = span.GraphInvocationGeneration,
                    parent_invocation_generation = span.ParentInvocationGeneration,
                    cycle = span.Cycle,
                    runtime_instance = DescribeRuntimeInstance(span.Instance),
                    source_handle = new
                    {
                        value = span.SourceHandle.Value,
                        kind = span.SourceHandle.Kind.ToString(),
                        valid = span.SourceHandle.IsValid
                    },
                    source = new
                    {
                        kind = span.Source.Kind.ToString(),
                        graph_authoring_id = span.Source.GraphAuthoringId,
                        element_authoring_id = span.Source.ElementAuthoringId,
                        timeline_authoring_id = span.Source.TimelineAuthoringId,
                        track_authoring_id = span.Source.TrackAuthoringId,
                        clip_authoring_id = span.Source.ClipAuthoringId
                    }
                }).ToArray()
            };
        }

        static object DescribeHistory(RuntimeExecutionHistory history)
        {
            if (history == null)
                return null;
            bool presentationRestoreSupported = SupportsAttachedPresentationRestore();
            return new
            {
                capture_id = history.CaptureId.ToString("N"),
                complete = history.IsComplete,
                evicted_events = history.EvictedEvents,
                unmapped_event_count = history.UnmappedEventCount,
                tick_count = history.Ticks.Count,
                checkpoint_count = history.Checkpoints.Count,
                presentation_frame_count = history.PresentationFrames.Count,
                has_presentation_frames = history.HasPresentationFrames,
                presentation_restore_supported = presentationRestoreSupported,
                replay_supported = presentationRestoreSupported && !history.HasExternalResults,
                external_result_count = history.ExternalResultCount,
                checkpoints = history.Checkpoints.Select(checkpoint => new
                {
                    tick = checkpoint.Tick,
                    execution_branch_id = checkpoint.ExecutionBranchId.ToString("N"),
                    program_epoch = checkpoint.ProgramEpoch,
                    program_revision = checkpoint.Revision.ToString(),
                    snapshot_identity = checkpoint.SnapshotIdentity,
                    history_complete = checkpoint.HistoryComplete,
                    history_can_restore = checkpoint.CanRestore,
                    can_restore = checkpoint.CanRestore && presentationRestoreSupported
                }).ToArray(),
                ticks = history.Ticks.Select(tick => new
                {
                    tick = tick.Tick,
                    execution_branch_id = tick.ExecutionBranchId.ToString("N"),
                    event_count = tick.Events.Count,
                    external_result_count = tick.ExternalResults.Count,
                    source_clocks = tick.SourceClocks.Select(source => new
                    {
                        clock_id = source.ClockId,
                        tick_kind = source.TickKind
                    }).ToArray(),
                    program_epochs = tick.ProgramEpochs.ToArray(),
                    skill_ids = tick.SkillIds.ToArray(),
                    action_instance_ids = tick.ActionInstanceIds.ToArray(),
                    input_sequences = tick.InputSequences.ToArray(),
                    branch_ids = tick.ExecutionBranchIds.Select(value => value.ToString("N")).ToArray(),
                    runtime_instances = tick.RuntimeInstances.Select(DescribeRuntimeInstance).ToArray()
                }).ToArray()
            };
        }

        static bool SupportsAttachedPresentationRestore()
        {
            IBtsmtlScenePlayPreviewOperations operations =
                BtsmtlScenePlayPreviewOperationsRegistry.Current;
            if (operations == null || !operations.SupportsPresentationCheckpointRestore)
                return false;
            BtsmtlScenePlayStatus status = operations.Status;
            if (!status.IsActive ||
                !BtsmtlScenePlayContextRegistry.TryGet(
                    status.Identity.ScenePath,
                    status.Identity.ContextId,
                    out BtsmtlScenePlayContext context) ||
                !context.TryDescribe(
                    out BtsmtlScenePlayContextDescriptor descriptor,
                    out _))
                return false;
            int hostInstanceId = RuntimeDebugSession.Shared.ViewModel.Target.HostInstanceId;
            for (int i = 0; i < descriptor.Actors.Count; i++)
            {
                CharacterPipelineHost host = descriptor.Actors[i].Host;
                if (host && host.GetInstanceID() == hostInstanceId)
                    return true;
            }
            return false;
        }

        static object DescribeRuntimeInstance(RuntimeInstanceKey instance) => new
        {
            kind = instance.Kind.ToString(),
            character_runtime_id = instance.CharacterRuntimeId.ToString("N"),
            graph_runtime_id = instance.GraphRuntimeId.ToString("N"),
            state_id = instance.StateId,
            activation_generation = instance.ActivationGeneration,
            timeline_playback_id = instance.TimelinePlaybackId,
            tree_clip_cycle = instance.TreeClipCycle,
            action_instance_id = instance.ActionInstanceId,
            call_site_id = instance.CallSiteId,
            invocation_generation = instance.InvocationGeneration,
            source_operation_index = instance.SourceOperationIndex,
            tree_clip_operation_index = instance.TreeClipOperationIndex
        };

        static object Skill(BtsmtlScenePlaySkillRequestResult result) => new
        {
            success = result.Accepted,
            code = result.Code.ToString(),
            actor_id = result.ActorId,
            skill_id = result.SkillId,
            input_request_id = result.InputRequestId,
            input_sequence = result.InputSequence,
            scene_generation = result.SceneGeneration,
            program_epoch = result.ProgramEpoch,
            program_revision = result.ProgramRevision,
            execution_branch_id = result.ExecutionBranchId.ToString("N"),
            checkpoint_tick = result.CheckpointTick,
            message = result.Message
        };

        static object Failure(string code, string action, string message) => new
        {
            success = false,
            code,
            action,
            message
        };

        static object DescribeStatus(BtsmtlScenePlayStatus status) => new
        {
            state = status.State.ToString(),
            operation = status.Operation.ToString(),
            scene_generation = status.SceneGeneration,
            failure_stage = status.FailureStage.ToString(),
            failure_code = status.FailureCode,
            failure_message = status.FailureMessage,
            identity = new
            {
                request_id = status.Identity.RequestId.ToString("N"),
                scene_path = status.Identity.ScenePath,
                context_id = status.Identity.ContextId
            }
        };

        static string RequiredString(JObject @params, string name)
        {
            string value = @params?[name]?.Value<string>() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException($"Scene Play parameter '{name}' is required and must be trimmed.", name);
            return value;
        }

        static ulong RequiredTick(JObject @params, string name)
        {
            string value = RequiredString(@params, name);
            if (!ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out ulong tick))
                throw new ArgumentException($"Scene Play parameter '{name}' must be an unsigned Tick.", name);
            return tick;
        }
    }
}
