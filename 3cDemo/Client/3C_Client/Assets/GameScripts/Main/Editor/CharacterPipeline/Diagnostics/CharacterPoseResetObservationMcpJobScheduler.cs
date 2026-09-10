using System;
using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using System.Linq;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    static class CharacterPoseResetObservationMcpJobScheduler
    {
        enum Phase : byte
        {
            WaitingTarget = 1,
            WaitingPaused = 2,
            WaitingInitialFrame = 3,
            WaitingCreatedFrame = 4,
            WaitingHistoryFrame = 5,
            WaitingResetFrame = 6
        }

        sealed class Job
        {
            internal string Id;
            internal string ActorId;
            internal string ContextId;
            internal string ScenePath;
            internal bool Started;
            internal bool Completed;
            internal object FinalResponse;
            internal Phase CurrentPhase = Phase.WaitingTarget;
            internal AnimationPresentationRuntimeTarget Target;
            internal string DebugTargetKey = string.Empty;
            internal GameplayTickDriveMode InitialDriveMode;
            internal bool PauseSubmitted;
            internal bool InitialStepSubmitted;
            internal Guid InterestOwner;
            internal AnimationPoseWatchIdentity[] Watches = Array.Empty<AnimationPoseWatchIdentity>();
            internal ulong LastCompletion;
            internal CharacterPoseResetObservationMcpTool.Observation Created;
            internal CharacterPoseResetObservationMcpTool.Observation Used;
            internal CharacterPoseResetObservationMcpTool.Observation Reset;
            internal double Deadline;

            internal void Schedule()
            {
                EditorApplication.update += Update;
            }

            void Update()
            {
                if (Completed)
                    return;
                Started = true;
                try
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException(
                            "Pose reset observation requires an active formal Scene Play session.");
                    if (EditorApplication.timeSinceStartup > Deadline)
                        throw new TimeoutException(
                            $"Scene Play Actor '{ActorId}' did not produce the required committed Pose frames.");
                    switch (CurrentPhase)
                    {
                        case Phase.WaitingTarget:
                            UpdateWaitingTarget();
                            break;
                        case Phase.WaitingPaused:
                            UpdateWaitingPaused();
                            break;
                        case Phase.WaitingInitialFrame:
                            UpdateWaitingInitialFrame();
                            break;
                        case Phase.WaitingCreatedFrame:
                            UpdateWaitingCreatedFrame();
                            break;
                        case Phase.WaitingHistoryFrame:
                            UpdateWaitingHistoryFrame();
                            break;
                        case Phase.WaitingResetFrame:
                            UpdateWaitingResetFrame();
                            break;
                        default:
                            throw new InvalidOperationException("Pose reset observation phase is invalid.");
                    }
                }
                catch (Exception exception)
                {
                    Fail(exception);
                }
            }

            void UpdateWaitingTarget()
            {
                if (!TryBindTarget())
                    return;
                if (InitialDriveMode != GameplayTickDriveMode.Paused)
                {
                    Submit(SimulationSessionDebugCommand.Pause(DebugTargetKey));
                    PauseSubmitted = true;
                }
                CurrentPhase = Phase.WaitingPaused;
            }

            void UpdateWaitingPaused()
            {
                if (!TryGetDebugStatus(out SimulationSessionDebugStatusSnapshot status) ||
                    status.DriveStatus.Mode != GameplayTickDriveMode.Paused)
                    return;
                if (!TryGetSnapshot(out AnimationPresentationRuntimeSnapshot snapshot))
                {
                    if (InitialStepSubmitted)
                        return;
                    InitialStepSubmitted = true;
                    Submit(SimulationSessionDebugCommand.Step(DebugTargetKey, 1));
                    CurrentPhase = Phase.WaitingInitialFrame;
                    return;
                }
                BeginCreated(snapshot);
            }

            void UpdateWaitingInitialFrame()
            {
                if (!TryGetNewSnapshot(out AnimationPresentationRuntimeSnapshot snapshot))
                    return;
                BeginCreated(snapshot);
            }

            void UpdateWaitingCreatedFrame()
            {
                if (!TryGetNewSnapshot(out AnimationPresentationRuntimeSnapshot snapshot))
                    return;
                Created = CharacterPoseResetObservationMcpTool.Capture(snapshot);
                LastCompletion = snapshot.CompletionIdentity;
                Submit(SimulationSessionDebugCommand.Step(DebugTargetKey, 1));
                CurrentPhase = Phase.WaitingHistoryFrame;
            }

            void UpdateWaitingHistoryFrame()
            {
                if (!TryGetNewSnapshot(out AnimationPresentationRuntimeSnapshot snapshot))
                    return;
                Used = CharacterPoseResetObservationMcpTool.Capture(snapshot);
                LastCompletion = snapshot.CompletionIdentity;
                Target.Reset();
                Submit(SimulationSessionDebugCommand.Step(DebugTargetKey, 1));
                CurrentPhase = Phase.WaitingResetFrame;
            }

            void UpdateWaitingResetFrame()
            {
                if (!TryGetNewSnapshot(out AnimationPresentationRuntimeSnapshot snapshot))
                    return;
                Reset = CharacterPoseResetObservationMcpTool.Capture(snapshot);
                CharacterPoseResetObservationMcpTool.RequireEquivalent(
                    Created,
                    Used,
                    Reset);
                Complete();
            }

            void BeginCreated(AnimationPresentationRuntimeSnapshot snapshot)
            {
                PrepareWatches(snapshot);
                Target.Reset();
                LastCompletion = snapshot.CompletionIdentity;
                Submit(SimulationSessionDebugCommand.Step(DebugTargetKey, 1));
                CurrentPhase = Phase.WaitingCreatedFrame;
            }

            void PrepareWatches(AnimationPresentationRuntimeSnapshot snapshot)
            {
                if (Watches.Length != 0)
                    return;
                IReadOnlyList<AnimationPoseWatchIdentity> watches =
                    CharacterFootPoseWatchDiscovery.Build(snapshot);
                if (watches.Count == 0)
                    throw new InvalidOperationException(
                        "Formal Scene Play Pose contains no Foot Placement or Full Body IK watches.");
                Watches = new AnimationPoseWatchIdentity[watches.Count];
                for (int i = 0; i < watches.Count; i++)
                    Watches[i] = watches[i];
                InterestOwner = Guid.NewGuid();
                Target.SetDiagnosticsInterest(
                    InterestOwner,
                    AnimationPresentationDiagnosticsInterest.LiveState |
                    AnimationPresentationDiagnosticsInterest.OperationDetail |
                    AnimationPresentationDiagnosticsInterest.FinalPoseDetail |
                    AnimationPresentationDiagnosticsInterest.PoseWatch);
                Target.SetPoseWatchInterests(InterestOwner, Watches);
            }

            bool TryBindTarget()
            {
                if (!AnimationPresentationRuntimeTargetRegistry.TryGet(
                        new ActorId(ActorId),
                        out AnimationPresentationRuntimeTarget target))
                    return false;
                UnityEngine.Object owner = EditorUtility.InstanceIDToObject(target.HostInstanceId);
                if (owner is not Component component ||
                    !string.Equals(component.gameObject.scene.path, ScenePath, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Scene Play Actor '{ActorId}' is not owned by scene '{ScenePath}'.");
                }
                if (!target.CanReset)
                    throw new InvalidOperationException(
                        $"Scene Play Actor '{ActorId}' has no formal Pose reset controller.");
                SimulationSessionDebugStatusSnapshot[] statuses =
                    LocalSimulationDebugControlService.CaptureStatusSnapshots()
                        .Where(value => value.LifecycleState == SimulationSessionLifecycleState.Active)
                        .ToArray();
                SimulationSessionDebugStatusSnapshot matched = default;
                int matches = 0;
                for (int i = 0; i < statuses.Length; i++)
                {
                    UnityEngine.Object sessionOwner = EditorUtility.InstanceIDToObject(
                        statuses[i].Identity.HostInstanceId);
                    if (sessionOwner is not Component sessionComponent ||
                        !string.Equals(
                            sessionComponent.gameObject.scene.path,
                            ScenePath,
                            StringComparison.Ordinal))
                        continue;
                    matched = statuses[i];
                    matches++;
                }
                if (matches != 1)
                    return false;
                Target = target;
                DebugTargetKey = matched.Identity.TargetKey;
                InitialDriveMode = matched.DriveStatus.Mode;
                return true;
            }

            bool TryGetDebugStatus(out SimulationSessionDebugStatusSnapshot status)
            {
                IReadOnlyList<SimulationSessionDebugStatusSnapshot> statuses =
                    LocalSimulationDebugControlService.CaptureStatusSnapshots();
                for (int i = 0; i < statuses.Count; i++)
                {
                    if (!string.Equals(
                            statuses[i].Identity.TargetKey,
                            DebugTargetKey,
                            StringComparison.Ordinal))
                        continue;
                    status = statuses[i];
                    return true;
                }
                status = default;
                return false;
            }

            bool TryGetSnapshot(out AnimationPresentationRuntimeSnapshot snapshot)
            {
                snapshot = default;
                if (Target == null || !Target.TryGetDebugView(out AnimationPresentationDebugView debugView))
                    return false;
                snapshot = debugView.PosePlan;
                return true;
            }

            bool TryGetNewSnapshot(out AnimationPresentationRuntimeSnapshot snapshot)
            {
                if (!TryGetSnapshot(out snapshot) ||
                    snapshot.CompletionIdentity <= LastCompletion)
                {
                    snapshot = default;
                    return false;
                }
                return true;
            }

            void Submit(SimulationSessionDebugCommand command)
            {
                if (!LocalSimulationDebugControlService.TrySubmit(
                        DebugTargetKey,
                        command,
                        out SimulationSessionDebugCommandResult result) ||
                    !result.Accepted)
                {
                    throw new InvalidOperationException(
                        result.Message ?? "Formal Scene Play debug command was rejected.");
                }
            }

            void Complete()
            {
                string targetName = Target.DisplayName;
                Guid runtimeId = Target.RuntimeInstanceId;
                object response = new
                {
                    success = true,
                    message = "Formal Scene Play Pose reset observation completed.",
                    data = new
                    {
                        actor_id = ActorId,
                        context_id = ContextId,
                        scene_path = ScenePath,
                        target = targetName,
                        runtime_instance_id = runtimeId.ToString("N"),
                        watch_count = Watches.Length,
                        pose_value_count = Created.PoseValueCount,
                        goal_count = Created.GoalCount,
                        applied_goal_count = Created.AppliedGoalCount,
                        effector_count = Created.EffectorCount,
                        pose_hash = Created.PoseHash,
                        goal_hash = Created.GoalHash,
                        output_hash = Created.OutputHash,
                        created = Created.ToResult(),
                        used = Used.ToResult(),
                        reset = Reset.ToResult(),
                        pose_equal = Created.PoseHash == Reset.PoseHash,
                        goals_equal = Created.GoalHash == Reset.GoalHash,
                        output_equal = Created.OutputHash == Reset.OutputHash
                    }
                };
                Finish(response);
            }

            void Fail(Exception exception)
            {
                Finish(new ErrorResponse(
                    "pose_reset_observation_failed",
                    new
                    {
                        job_id = Id,
                        actor_id = ActorId,
                        context_id = ContextId,
                        scene_path = ScenePath,
                        phase = CurrentPhase.ToString(),
                        message = exception.ToString()
                    }));
            }

            void Finish(object response)
            {
                ReleaseRuntimeInterest();
                if (PauseSubmitted && InitialDriveMode != GameplayTickDriveMode.Paused)
                {
                    try
                    {
                        Submit(SimulationSessionDebugCommand.SetRealtime(DebugTargetKey));
                    }
                    catch
                    {
                    }
                }
                FinalResponse = response;
                Completed = true;
                EditorApplication.update -= Update;
            }

            void ReleaseRuntimeInterest()
            {
                if (Target == null || InterestOwner == Guid.Empty)
                    return;
                try
                {
                    Target.RemovePoseWatchInterests(InterestOwner);
                    Target.RemoveDiagnosticsInterest(InterestOwner);
                }
                catch
                {
                }
            }
        }

        const string DefaultActorId = "gameplay-lab-player";
        const string DefaultScenePath = "Assets/Scenes/GameplayLab/GameplayLab.unity";
        static readonly HashSet<string> AllowedParameters = new HashSet<string>(StringComparer.Ordinal)
        {
            "action",
            "actor_id",
            "context_id",
            "job_id",
            "scene_path"
        };
        static readonly Dictionary<string, Job> Jobs =
            new Dictionary<string, Job>(StringComparer.Ordinal);
        static readonly object JobsLock = new object();

        internal static object Handle(JObject parameters)
        {
            if (parameters == null)
                return new ErrorResponse("request_missing");
            foreach (JProperty property in parameters.Properties())
            {
                if (!AllowedParameters.Contains(property.Name))
                    return new ErrorResponse("unknown_parameter", new { parameter = property.Name });
                if (property.Value.Type != JTokenType.String)
                    return new ErrorResponse("parameter_invalid", new { parameter = property.Name });
            }
            string action = StringValue(
                parameters,
                "action",
                parameters["job_id"] != null ? "status" : "start");
            if (string.Equals(action, "status", StringComparison.OrdinalIgnoreCase))
                return Status(parameters);
            if (!string.Equals(action, "start", StringComparison.OrdinalIgnoreCase))
                return new ErrorResponse(
                    "poll_action_unsupported",
                    new { action, supported = new[] { "start", "status" } });
            if (parameters["job_id"] != null)
                return new ErrorResponse("job_id_not_allowed_for_start", new { action });
            if (!EditorApplication.isPlaying)
                return new ErrorResponse(
                    "scene_play_required",
                    new { actor_id = StringValue(parameters, "actor_id", DefaultActorId) });

            string actorId = StringValue(parameters, "actor_id", DefaultActorId);
            string contextId = StringValue(parameters, "context_id", string.Empty);
            string scenePath = StringValue(parameters, "scene_path", DefaultScenePath);
            if (string.IsNullOrWhiteSpace(actorId))
                return new ErrorResponse("actor_id_required");
            if (!scenePath.StartsWith("Assets/", StringComparison.Ordinal))
                return new ErrorResponse("scene_path_invalid", new { scene_path = scenePath });
            Job job;
            lock (JobsLock)
            {
                foreach (Job active in Jobs.Values)
                {
                    if (active.Completed ||
                        !string.Equals(active.ActorId, actorId, StringComparison.Ordinal) ||
                        !string.Equals(active.ScenePath, scenePath, StringComparison.Ordinal))
                        continue;
                    return Pending(active, "Pose reset observation job is already running.");
                }
                job = new Job
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ActorId = actorId,
                    ContextId = contextId,
                    ScenePath = scenePath,
                    Deadline = EditorApplication.timeSinceStartup + 120d
                };
                Jobs.Add(job.Id, job);
            }
            job.Schedule();
            return Pending(job, "Formal Scene Play Pose reset observation job scheduled.");
        }

        static object Status(JObject parameters)
        {
            string jobId = StringValue(parameters, "job_id", string.Empty);
            if (string.IsNullOrWhiteSpace(jobId))
                return new ErrorResponse("job_id_required", new { action = "status" });
            lock (JobsLock)
            {
                if (!Jobs.TryGetValue(jobId, out Job job))
                    return new ErrorResponse("job_lost", new { job_id = jobId });
                if (!job.Completed)
                    return Pending(job, "Formal Scene Play Pose reset observation job is running.");
                return job.FinalResponse ?? new ErrorResponse("job_response_missing", JobState(job));
            }
        }

        static PendingResponse Pending(Job job, string message) =>
            new PendingResponse(message, 1.0, JobState(job));

        static object JobState(Job job) => new
        {
            job_id = job.Id,
            actor_id = job.ActorId,
            context_id = job.ContextId,
            scene_path = job.ScenePath,
            started = job.Started,
            completed = job.Completed,
            phase = job.CurrentPhase.ToString(),
            completion_identity = job.LastCompletion,
            target = job.Target?.DisplayName ?? string.Empty
        };

        static string StringValue(JObject parameters, string key, string defaultValue)
        {
            string value = parameters[key]?.Value<string>();
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }
    }
}
