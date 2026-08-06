using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    enum CharacterRuntimeDiagnosticsInspectorMode
    {
        Complete,
        FootPlacement
    }

    static class CharacterRuntimeDiagnosticsInspector
    {
        const int FootIkCaptureSegmentLimit = 240;

        static RuntimeDiagnosticsCaptureDetail s_CaptureDetail = RuntimeDiagnosticsCaptureDetail.Evaluation;

        internal static void DrawCharacterPipelineConfiguration(CharacterPipelineHost host)
        {
            DrawFootPlacementConfiguration(host);
            DrawEquipmentConfiguration(host);
        }

        internal static void DrawRuntimeDiagnostics(
            object interestOwner,
            int hostInstanceId,
            CharacterPipelineDefinition definition,
            CharacterRuntimeDiagnosticsInspectorMode mode)
        {
            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            RuntimeDebugViewModel view = session.ViewModel;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Runtime Diagnostics", EditorStyles.boldLabel);
            if (!view.Attached || view.Target.HostInstanceId != hostInstanceId)
            {
                session.ReleaseLiveInterest(interestOwner);
                if (GUILayout.Button("Attach Debug Session"))
                    session.AttachToHost(hostInstanceId);
                EditorGUILayout.LabelField("State", session.AttachmentState.ToString());
                if (view.Attached)
                    EditorGUILayout.LabelField("Current Target", view.Target.DisplayName);
                return;
            }

            RuntimeTraceChannel liveChannels = mode == CharacterRuntimeDiagnosticsInspectorMode.FootPlacement
                ? RuntimeTraceChannel.FootPlacement
                : RuntimeTraceChannel.All;
            if (session.CanControlLiveTarget)
                session.EnsureLiveInterest(interestOwner, liveChannels);

            DrawSessionControls(
                session,
                view,
                mode == CharacterRuntimeDiagnosticsInspectorMode.Complete);
            if (session.AttachmentState == RuntimeDebugAttachmentState.Ended)
                EditorGUILayout.HelpBox("Target ended. The inspector is showing its final live state or the active capture.", MessageType.Info);
            if (!view.Valid)
            {
                EditorGUILayout.HelpBox(!string.IsNullOrEmpty(view.Error) ? view.Error : "Runtime diagnostics are unavailable.", MessageType.Error);
                return;
            }

            DrawFootPlacement(session, view);
            if (mode == CharacterRuntimeDiagnosticsInspectorMode.FootPlacement)
                return;
            DrawSimulation(view);
            DrawNetwork(view);
            DrawGraphLifecycle(view);
            DrawStateMachine(view);
            DrawAction(view);
            DrawEquipment(view);
            DrawBlackboard(view);
            DrawMotion(view);
            DrawCamera(view);
            DrawPresentation(definition, view);
        }

        static void DrawFootPlacementConfiguration(CharacterPipelineHost host)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("World-Aware Presentation", EditorStyles.boldLabel);
            CharacterWorldAwarePresentationBinding binding = host.WorldAwarePresentation;
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Binding", binding, typeof(CharacterWorldAwarePresentationBinding), true);
            if (!binding)
            {
                EditorGUILayout.HelpBox("Character host requires an explicit World-Aware Presentation Binding.", MessageType.Error);
                return;
            }
            try
            {
                binding.RequireValid();
                if (binding.PresentationRoot != host.VisualRoot)
                    throw new InvalidOperationException("World-Aware Presentation Root must match the Host Visual Root.");
                EditorGUILayout.HelpBox("World-Aware Presentation Binding is valid. Foot Placement Profile and Calibration are owned by the Pose Graph node.", MessageType.Info);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
            }
        }

        static void DrawEquipmentConfiguration(CharacterPipelineHost host)
        {
            if (!host.Definition || !host.Definition.EquipmentCapabilityEnabled)
                return;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Equipment", EditorStyles.boldLabel);
            CharacterEquipmentRigBindingCatalog catalog = host.EquipmentRigBindings;
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Rig Bindings", catalog, typeof(CharacterEquipmentRigBindingCatalog), true);
            if (!catalog)
            {
                EditorGUILayout.HelpBox("Equipment-enabled Character Host requires an explicit Rig Binding Catalog.", MessageType.Error);
                return;
            }
            var errors = new List<string>();
            if (catalog.CollectConfigurationErrors(errors))
            {
                EditorGUILayout.HelpBox("Equipment Rig and Socket bindings are valid.", MessageType.Info);
                return;
            }
            for (int i = 0; i < errors.Count; i++)
                EditorGUILayout.HelpBox(errors[i], MessageType.Error);
        }

        static void DrawSimulation(RuntimeDebugViewModel view)
        {
            DrawEventSection("Simulation Session", Filter(view, RuntimeTraceChannel.Graph,
                RuntimeTraceEventKind.SimulationTick,
                RuntimeTraceEventKind.SimulationRestore,
                RuntimeTraceEventKind.SimulationEvaluate,
                RuntimeTraceEventKind.SimulationFinalize,
                RuntimeTraceEventKind.SimulationStatePublished,
                RuntimeTraceEventKind.SimulationCommit,
                RuntimeTraceEventKind.SimulationFailure), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                return $"{payload.Name} | {payload.Status} | actor {payload.OwnerId} | {payload.Detail}";
            });
        }

        static void DrawNetwork(RuntimeDebugViewModel view)
        {
            DrawEventSection("Network Model", Filter(view, RuntimeTraceChannel.Network,
                RuntimeTraceEventKind.SimulationNetworkModel), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                return $"{payload.Cause} | {payload.Name} | {payload.Status} | actor {payload.OwnerId} | {payload.RelatedElementId} | input {payload.Value.DisplayValue()} | queue {payload.Priority} | replay {payload.Cycle} | {payload.Detail}";
            });
        }

        static void DrawGraphLifecycle(RuntimeDebugViewModel view)
        {
            DrawEventSection("Graph Lifecycle", Filter(view, RuntimeTraceChannel.Graph,
                RuntimeTraceEventKind.NodeEntered,
                RuntimeTraceEventKind.NodeStatus,
                RuntimeTraceEventKind.NodeCompleted,
                RuntimeTraceEventKind.NodeStopRequested,
                RuntimeTraceEventKind.NodeStopping,
                RuntimeTraceEventKind.NodeStopped,
                RuntimeTraceEventKind.NodeForceStopped,
                RuntimeTraceEventKind.EdgeEvaluated,
                RuntimeTraceEventKind.EdgeSelected), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                return $"{eventView.Event.Kind} | {payload.Status} | {payload.Cause} | parent/source {payload.OwnerId} | target/path {payload.RelatedElementId} | {payload.Detail}";
            });
        }

        static void DrawStateMachine(RuntimeDebugViewModel view)
        {
            DrawEventSection("State Machine", Filter(view, RuntimeTraceChannel.StateMachine,
                RuntimeTraceEventKind.StateTransitionEvaluated,
                RuntimeTraceEventKind.StateTransitionSelected,
                RuntimeTraceEventKind.StateScopeEntered,
                RuntimeTraceEventKind.StateScopeExited,
                RuntimeTraceEventKind.StateExitStarted,
                RuntimeTraceEventKind.StateExitWaiting), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                return $"{eventView.Event.Kind} | {payload.Status} | {payload.Cause} | state {payload.OwnerId} | target {payload.RelatedElementId} | {payload.Detail}";
            });
        }

        static void DrawSessionControls(
            RuntimeDebugSession session,
            RuntimeDebugViewModel view,
            bool showGeneralCaptureControls)
        {
            EditorGUILayout.LabelField("State", session.AttachmentState.ToString());
            EditorGUILayout.LabelField("Target", view.Target.DisplayName);
            EditorGUILayout.LabelField("Character", view.Target.CharacterRuntimeId.ToString("D"));
            EditorGUILayout.LabelField("Session", view.Target.SessionId.ToString("D"));
            EditorGUILayout.LabelField("Program", view.Target.Revision.ProgramId);
            EditorGUILayout.LabelField("Source Revision", view.Target.Revision.SourceRevision);
            EditorGUILayout.LabelField("Program Hash", view.Target.Revision.ProgramHash);
            EditorGUILayout.LabelField("Live Channels", view.Channels.ToString());
            EditorGUILayout.LabelField("Position", $"logic {view.LatestLogicTick}, presentation {view.LatestPresentationFrame}");

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!session.CanControlLiveTarget && !session.CanResumeLiveTarget))
            {
                if (GUILayout.Button(session.CanControlLiveTarget ? "Freeze Live" : "Resume Live"))
                {
                    if (session.CanControlLiveTarget)
                        session.FreezeLive();
                    else
                        session.ResumeLive();
                }
            }
            if (GUILayout.Button("Clear Debug Session"))
                session.ClearTarget();
            EditorGUILayout.EndHorizontal();

            if (session.IsCaptureRecording)
            {
                EditorGUILayout.LabelField("Capture", $"Recording {session.CaptureSegmentCount}/{session.CaptureSegmentCapacity} segments");
                if (GUILayout.Button("Stop Capture"))
                    session.EndCapture();
            }
            else if (showGeneralCaptureControls)
            {
                s_CaptureDetail = (RuntimeDiagnosticsCaptureDetail)EditorGUILayout.EnumPopup("Capture Detail", s_CaptureDetail);
                using (new EditorGUI.DisabledScope(!session.CanStartCapture))
                {
                    if (GUILayout.Button("Start Capture"))
                        session.BeginCapture(RuntimeTraceChannel.All, s_CaptureDetail);
                }
            }

            if (session.HasCaptureHistory)
            {
                int maxHistory = Math.Max(0, session.CaptureSnapshot.SegmentCount - 1);
                int history = EditorGUILayout.IntSlider("Capture History", Math.Min(session.HistoryOffset, maxHistory), 0, maxHistory);
                if (history != session.HistoryOffset)
                    session.SetHistoryOffset(history);
            }
        }

        static void DrawAction(RuntimeDebugViewModel view)
        {
            DrawEventSection("Action", Filter(view, RuntimeTraceChannel.StateMachine,
                RuntimeTraceEventKind.ActionSnapshot,
                RuntimeTraceEventKind.ActionActivationRequested,
                RuntimeTraceEventKind.ActionLifecycleTransitioned,
                RuntimeTraceEventKind.ActionWindowSampled,
                RuntimeTraceEventKind.ActionCueSubmitted,
                RuntimeTraceEventKind.ActionResultSubmitted), FormatAction);
        }

        static void DrawBlackboard(RuntimeDebugViewModel view)
        {
            DrawEventSection("Blackboard", view.GetCurrentEvents(RuntimeTraceChannel.Blackboard), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                return $"{eventView.SourceName} | {eventView.Event.Kind} | {payload.Value.DisplayValue()} | {payload.Status} {payload.Cause}";
            });
        }

        static void DrawEquipment(RuntimeDebugViewModel view)
        {
            DrawEventSection("Equipment", view.GetCurrentEvents(RuntimeTraceChannel.Equipment), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                return $"{eventView.Event.Kind} | {payload.Status} | slot {payload.OwnerId} | {payload.Name} | {payload.RelatedElementId} | {payload.Cause} | {payload.Detail}";
            });
        }

        static void DrawMotion(RuntimeDebugViewModel view)
        {
            DrawEventSection("Motion", view.GetCurrentEvents(RuntimeTraceChannel.Motion), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                if (eventView.Event.Kind == RuntimeTraceEventKind.SimulationWorldBatch)
                    return $"{payload.Name} | {payload.Status} | {payload.Detail}";
                if (eventView.Event.Kind == RuntimeTraceEventKind.MotionContribution)
                    return $"{payload.Name} | {payload.Status} | P{payload.Priority} w{payload.Weight:0.###} | {payload.Value.DisplayValue()}";
                if (string.Equals(payload.Name, "world_result_applied", StringComparison.Ordinal))
                    return $"{payload.Name} | {payload.Status} | actor {payload.OwnerId} | {payload.Detail}";
                return $"Resolved | {payload.Status} | {payload.Value.DisplayValue()} | yaw {payload.Time:0.###}/{payload.SecondaryTime:0.###}";
            });
        }

        static void DrawCamera(RuntimeDebugViewModel view)
        {
            DrawEventSection("Camera", Filter(view, RuntimeTraceChannel.Animation,
                RuntimeTraceEventKind.CameraSnapshot,
                RuntimeTraceEventKind.CameraRequest,
                RuntimeTraceEventKind.CameraCue), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                return $"{eventView.Event.Kind} | {payload.Name} | {payload.Status} | owner {payload.OwnerId} | P{payload.Priority} w{payload.Weight:0.###} | {payload.Value.DisplayValue()}";
            });
        }

        static void DrawPresentation(CharacterPipelineDefinition definition, RuntimeDebugViewModel view)
        {
            IReadOnlyList<RuntimeDebugEventView> source = view.GetCurrentEvents(RuntimeTraceChannel.Animation);
            var events = new List<RuntimeDebugEventView>();
            for (int i = 0; i < source.Count; i++)
            {
                RuntimeTraceEventKind kind = source[i].Event.Kind;
                if (kind is RuntimeTraceEventKind.CameraSnapshot or RuntimeTraceEventKind.CameraRequest or RuntimeTraceEventKind.CameraCue)
                    continue;
                events.Add(source[i]);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Animation Presentation", EditorStyles.boldLabel);
            DrawAnimationGroup("Selection", events, RuntimeTraceEventKind.AnimationSelectionSubmitted);
            DrawAnimationGroup("Timeline Samples", events, RuntimeTraceEventKind.AnimationProducerSampled, RuntimeTraceEventKind.TimelineVisualTime);
            DrawAnimationGroup(
                "Motion Matching",
                events,
                RuntimeTraceEventKind.MotionMatchingQuery,
                RuntimeTraceEventKind.MotionMatchingTrajectory,
                RuntimeTraceEventKind.MotionMatchingPoseHistory,
                RuntimeTraceEventKind.MotionMatchingAdmission,
                RuntimeTraceEventKind.MotionMatchingCandidateRejected,
                RuntimeTraceEventKind.MotionMatchingSearchTraversal,
                RuntimeTraceEventKind.MotionMatchingTopK,
                RuntimeTraceEventKind.MotionMatchingPlan,
                RuntimeTraceEventKind.MotionMatchingSelection,
                RuntimeTraceEventKind.MotionMatchingPoseSource,
                RuntimeTraceEventKind.MotionMatchingReset,
                RuntimeTraceEventKind.MotionMatchingFrame);
            DrawMotionMatchingReplayCapture(definition, view, events);
            DrawAnimationGroup(
                "Playback Lifecycle",
                events,
                RuntimeTraceEventKind.AnimationPlaybackPending,
                RuntimeTraceEventKind.AnimationPlaybackSelected,
                RuntimeTraceEventKind.AnimationPlaybackRetained,
                RuntimeTraceEventKind.AnimationPlaybackRetired,
                RuntimeTraceEventKind.AnimationPlaybackCompleted,
                RuntimeTraceEventKind.AnimationPlaybackReleased);
            DrawAnimationGroup("Presentation", events, RuntimeTraceEventKind.PresentationInterpolated);
        }

        static void DrawMotionMatchingReplayCapture(
            CharacterPipelineDefinition definition,
            RuntimeDebugViewModel view,
            IReadOnlyList<RuntimeDebugEventView> events)
        {
            EditorGUILayout.LabelField("Motion Matching Capability", "Available in project");
            CharacterAnimationPresentationProfile profile = definition ? definition.AnimationPresentationProfile : null;
            CharacterPresentationProjectionAsset projection = definition ? definition.PresentationProjection : null;
            EditorGUILayout.LabelField("Definition Identity", AssetIdentity(definition));
            EditorGUILayout.LabelField("Profile Identity", AssetIdentity(profile));
            EditorGUILayout.LabelField("Projection Asset Identity", AssetIdentity(projection));
            if (!AnimationPresentationRuntimeTargetRegistry.TryGet(
                    view.Target.CharacterRuntimeId,
                    out AnimationPresentationRuntimeTarget target))
            {
                EditorGUILayout.LabelField("Current Definition", "Runtime target unavailable");
                return;
            }
            EditorGUILayout.LabelField(
                "Current Definition",
                target.MotionMatchingRuntimeEnabled ? "Enabled" : "Disabled");
            if (!target.MotionMatchingRuntimeEnabled)
                return;
            string providerId = string.Empty;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                RuntimeTraceEventKind kind = events[i].Event.Kind;
                if (kind is not RuntimeTraceEventKind.MotionMatchingQuery and
                    not RuntimeTraceEventKind.MotionMatchingSelection)
                {
                    continue;
                }
                providerId = events[i].Event.Payload.OwnerId;
                if (!string.IsNullOrWhiteSpace(providerId))
                    break;
            }
            EditorGUILayout.LabelField("Active Provider", string.IsNullOrEmpty(providerId) ? "No searchable frame" : providerId);
            if (!string.IsNullOrEmpty(providerId) &&
                target.TryCaptureMotionMatchingSearchReplay(providerId, out MotionMatchingSearchReplayArtifact artifact))
            {
                EditorGUILayout.LabelField("MM Profile", artifact.ProfileId.Value);
                EditorGUILayout.LabelField("Database", artifact.DatabaseIdentity.DatabaseId.Value);
                EditorGUILayout.LabelField("Database Artifact", artifact.DatabaseIdentity.ContentHash.Value);
                EditorGUILayout.LabelField("Runtime Projection", artifact.ProjectionIdentity);
            }
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(providerId)))
            {
                if (GUILayout.Button("Capture Motion Matching Search Replay"))
                    CaptureMotionMatchingSearchReplay(target, providerId);
            }
        }

        static string AssetIdentity(UnityEngine.Object asset)
        {
            if (!asset)
                return "Missing";
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            return string.IsNullOrEmpty(guid) ? asset.name : $"{asset.name} [{guid}]";
        }

        static void CaptureMotionMatchingSearchReplay(
            AnimationPresentationRuntimeTarget target,
            string providerId)
        {
            try
            {
                if (!target.TryCaptureMotionMatchingSearchReplay(providerId, out MotionMatchingSearchReplayArtifact artifact))
                    throw new InvalidOperationException("The active Motion Matching provider has no completed Search to capture.");
                string path = EditorUtility.SaveFilePanelInProject(
                    "Capture Motion Matching Search Replay",
                    $"{providerId}-search-replay",
                    "bytes",
                    "Choose the Search Replay Artifact path.");
                if (string.IsNullOrEmpty(path))
                    return;
                File.WriteAllBytes(Path.GetFullPath(path), MotionMatchingSearchReplayArtifactCodec.Encode(artifact));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Motion Matching Search Replay Capture Failed", exception.Message, "OK");
            }
        }

        static void DrawFootPlacement(
            RuntimeDebugSession session,
            RuntimeDebugViewModel view)
        {
            IReadOnlyList<RuntimeDebugEventView> events = Filter(
                view,
                RuntimeTraceChannel.FootPlacement,
                RuntimeTraceEventKind.FootPlacementSnapshot);
            DrawEventSection(
                "Foot Placement",
                events,
                eventView =>
                {
                    RuntimeTracePayload payload = eventView.Event.Payload;
                    RuntimeFootIkTraceSnapshot footIk = payload.FootIk;
                    if (!footIk.IsAvailable)
                        return $"{payload.Name} | {payload.Status} | {payload.Detail}";
                    return $"{payload.Status} | frame {footIk.FrameSequence} | " +
                           $"L confidence {footIk.Left.PlantConfidence:0.###} -> plant {footIk.Left.PlantWeight:0.###} -> goal {footIk.Left.GoalPositionWeight:0.###} -> residual {footIk.Left.PositionResidual:0.###} | " +
                           $"R confidence {footIk.Right.PlantConfidence:0.###} -> plant {footIk.Right.PlantWeight:0.###} -> goal {footIk.Right.GoalPositionWeight:0.###} -> residual {footIk.Right.PositionResidual:0.###} | " +
                           $"pelvis {footIk.PelvisTargetOffset:0.###}->{footIk.PelvisResolvedOffset:0.###} reject L/R {footIk.RejectLeftGoal}/{footIk.RejectRightGoal}";
                });
            if (session.IsCaptureRecording)
            {
                EditorGUILayout.HelpBox(
                    "Foot IK records every Presentation Frame. Inspector preview is throttled without dropping captured frames.",
                    MessageType.Info);
                if (GUILayout.Button("Stop Active Capture"))
                    session.EndCapture();
            }
            else
            {
                using (new EditorGUI.DisabledScope(!session.CanStartCapture))
                {
                    if (GUILayout.Button($"Capture {FootIkCaptureSegmentLimit} Foot IK Frames"))
                    {
                        session.BeginBoundedCapture(
                            RuntimeTraceChannel.FootPlacement,
                            RuntimeDiagnosticsCaptureDetail.Continuous,
                            FootIkCaptureSegmentLimit);
                    }
                }
            }
            using (new EditorGUI.DisabledScope(!session.HasCaptureHistory))
            {
                if (GUILayout.Button("Export Foot IK Capture CSV"))
                    ExportFootIkCapture(session.CaptureSnapshot);
            }
        }

        static void ExportFootIkCapture(RuntimeCaptureSnapshot capture)
        {
            try
            {
                if (capture == null)
                    throw new InvalidOperationException("There is no completed Runtime Diagnostics capture.");
                string path = EditorUtility.SaveFilePanel(
                    "Export Foot IK Capture",
                    string.Empty,
                    $"foot-ik-{capture.CaptureId:N}.csv",
                    "csv");
                if (string.IsNullOrEmpty(path))
                    return;
                var builder = new StringBuilder(32768);
                AppendFootIkHeader(builder);
                int rowCount = 0;
                for (int segmentIndex = 0; segmentIndex < capture.Segments.Count; segmentIndex++)
                {
                    RuntimeCaptureSegmentSnapshot segment = capture.Segments[segmentIndex];
                    for (int eventIndex = 0; eventIndex < segment.Events.Count; eventIndex++)
                    {
                        RuntimeTraceEvent traceEvent = segment.Events[eventIndex];
                        if (traceEvent.Channel != RuntimeTraceChannel.FootPlacement ||
                            traceEvent.Kind != RuntimeTraceEventKind.FootPlacementSnapshot ||
                            !traceEvent.Payload.FootIk.IsAvailable)
                        {
                            continue;
                        }
                        AppendFootIkRow(builder, traceEvent);
                        rowCount++;
                    }
                }
                if (rowCount == 0)
                    throw new InvalidOperationException("The capture contains no Continuous Foot IK frames.");
                File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
                EditorUtility.RevealInFinder(path);
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Foot IK Capture Export Failed", exception.Message, "OK");
            }
        }

        static void AppendFootIkHeader(StringBuilder builder)
        {
            AppendCsvRow(builder,
                "presentation_position", "trace_sequence", "frame_sequence", "goal_completion", "solver_completion",
                "grounding_backend", "solver_backend", "solver_failure", "body_grounded", "root_hit", "root_surface",
                "pelvis_target", "pelvis_resolved", "reject_left", "reject_right", "pelvis_height_mode", "movement_compensation_mode",
                "left_grounded", "left_hit", "left_surface", "left_plant_confidence", "left_sole_height", "left_placement_weight",
                "left_plant_weight", "left_contact_weight", "left_goal_position_weight", "left_goal_rotation_weight", "left_constraint",
                "left_transition", "left_lock", "left_prediction_reject", "left_goal_application", "left_goal_source", "left_solver_result_available",
                "left_leg_extension", "left_ankle_twist", "left_query_count",
                "left_rejected_query_count", "left_grounding_x", "left_grounding_y", "left_grounding_z", "left_goal_x", "left_goal_y",
                "left_goal_z", "left_solved_x", "left_solved_y", "left_solved_z", "left_position_residual", "left_rotation_residual_degrees",
                "right_grounded", "right_hit", "right_surface", "right_plant_confidence", "right_sole_height", "right_placement_weight",
                "right_plant_weight", "right_contact_weight", "right_goal_position_weight", "right_goal_rotation_weight", "right_constraint",
                "right_transition", "right_lock", "right_prediction_reject", "right_goal_application", "right_goal_source", "right_solver_result_available",
                "right_leg_extension", "right_ankle_twist", "right_query_count",
                "right_rejected_query_count", "right_grounding_x", "right_grounding_y", "right_grounding_z", "right_goal_x", "right_goal_y",
                "right_goal_z", "right_solved_x", "right_solved_y", "right_solved_z", "right_position_residual", "right_rotation_residual_degrees");
        }

        static void AppendFootIkRow(StringBuilder builder, RuntimeTraceEvent traceEvent)
        {
            RuntimeFootIkTraceSnapshot snapshot = traceEvent.Payload.FootIk;
            RuntimeFootIkLegTraceSnapshot left = snapshot.Left;
            RuntimeFootIkLegTraceSnapshot right = snapshot.Right;
            AppendCsvRow(builder,
                Number(traceEvent.Position), Number(traceEvent.Sequence), Number(snapshot.FrameSequence),
                Number(snapshot.GoalCompletionIdentity), Number(snapshot.SolverCompletionIdentity),
                snapshot.GroundingBackendIdentity, snapshot.SolverBackendIdentity, snapshot.SolverFailure,
                Bool(snapshot.BodyGrounded), Bool(snapshot.RootHit), Number(snapshot.RootSurfaceIdentity),
                Number(snapshot.PelvisTargetOffset), Number(snapshot.PelvisResolvedOffset), Bool(snapshot.RejectLeftGoal),
                Bool(snapshot.RejectRightGoal), snapshot.PelvisHeightMode, snapshot.MovementCompensationMode,
                Bool(left.Grounded), Bool(left.CurrentGroundingHit), Number(left.SurfaceIdentity), Number(left.PlantConfidence),
                Number(left.SoleHeight), Number(left.PlacementWeight), Number(left.PlantWeight), Number(left.ContactWeight),
                Number(left.GoalPositionWeight), Number(left.GoalRotationWeight), left.ConstraintState, left.TransitionReason, left.LockType,
                left.PredictionRejectReason, left.GoalApplication, left.GoalSourceKind, Bool(left.SolverResultAvailable),
                Number(left.LegExtensionRatio), Number(left.AnkleTwistDegrees), Number(left.QueryCount),
                Number(left.RejectedQueryCount), Number(left.GroundingComponentPosition.x), Number(left.GroundingComponentPosition.y),
                Number(left.GroundingComponentPosition.z), Number(left.GoalComponentPosition.x), Number(left.GoalComponentPosition.y),
                Number(left.GoalComponentPosition.z), Number(left.SolvedComponentPosition.x), Number(left.SolvedComponentPosition.y),
                Number(left.SolvedComponentPosition.z), Number(left.PositionResidual), Number(left.RotationResidualDegrees),
                Bool(right.Grounded), Bool(right.CurrentGroundingHit), Number(right.SurfaceIdentity), Number(right.PlantConfidence),
                Number(right.SoleHeight), Number(right.PlacementWeight), Number(right.PlantWeight), Number(right.ContactWeight),
                Number(right.GoalPositionWeight), Number(right.GoalRotationWeight), right.ConstraintState, right.TransitionReason, right.LockType,
                right.PredictionRejectReason, right.GoalApplication, right.GoalSourceKind, Bool(right.SolverResultAvailable),
                Number(right.LegExtensionRatio), Number(right.AnkleTwistDegrees), Number(right.QueryCount),
                Number(right.RejectedQueryCount), Number(right.GroundingComponentPosition.x), Number(right.GroundingComponentPosition.y),
                Number(right.GroundingComponentPosition.z), Number(right.GoalComponentPosition.x), Number(right.GoalComponentPosition.y),
                Number(right.GoalComponentPosition.z), Number(right.SolvedComponentPosition.x), Number(right.SolvedComponentPosition.y),
                Number(right.SolvedComponentPosition.z), Number(right.PositionResidual), Number(right.RotationResidualDegrees));
        }

        static void AppendCsvRow(StringBuilder builder, params string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                    builder.Append(',');
                string value = values[i] ?? string.Empty;
                if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
                {
                    builder.Append(value);
                    continue;
                }
                builder.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
            }
            builder.AppendLine();
        }

        static string Number<T>(T value) where T : IFormattable =>
            value.ToString(null, CultureInfo.InvariantCulture);

        static string Bool(bool value) => value ? "true" : "false";

        static void DrawAnimationGroup(string title, IReadOnlyList<RuntimeDebugEventView> events, params RuntimeTraceEventKind[] kinds)
        {
            var matches = new List<RuntimeDebugEventView>();
            for (int i = 0; i < events.Count; i++)
            {
                if (ContainsKind(kinds, events[i].Event.Kind))
                    matches.Add(events[i]);
            }

            EditorGUILayout.LabelField(title, matches.Count.ToString());
            for (int i = 0; i < matches.Count; i++)
            {
                RuntimeDebugEventView eventView = matches[i];
                RuntimeTracePayload payload = eventView.Event.Payload;
                string detail = FormatAnimationEvent(eventView.Event.Kind, payload);
                EditorGUILayout.LabelField($"{eventView.Event.Kind} {payload.Name}", detail);
                DrawSourceIdentity(eventView);
            }
        }

        static IReadOnlyList<RuntimeDebugEventView> Filter(RuntimeDebugViewModel view, RuntimeTraceChannel channel, params RuntimeTraceEventKind[] kinds)
        {
            IReadOnlyList<RuntimeDebugEventView> source = view.GetCurrentEvents(channel);
            var result = new List<RuntimeDebugEventView>();
            for (int i = 0; i < source.Count; i++)
            {
                if (ContainsKind(kinds, source[i].Event.Kind))
                    result.Add(source[i]);
            }
            return result;
        }

        static bool ContainsKind(IReadOnlyList<RuntimeTraceEventKind> kinds, RuntimeTraceEventKind value)
        {
            for (int i = 0; i < kinds.Count; i++)
            {
                if (kinds[i] == value)
                    return true;
            }
            return false;
        }

        static string FormatAnimationEvent(RuntimeTraceEventKind kind, RuntimeTracePayload payload)
        {
            return $"{payload.Status} channel {payload.AnimationChannelId} slot {payload.Name} playback {payload.OwnerId} source {payload.RelatedElementId} time {payload.Time:0.###} weight {payload.Weight:0.###} {payload.Detail}";
        }

        static void DrawEventSection(string title, IReadOnlyList<RuntimeDebugEventView> events, Func<RuntimeDebugEventView, string> formatter)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Events", events.Count.ToString());
            for (int i = 0; i < events.Count; i++)
            {
                RuntimeDebugEventView eventView = events[i];
                EditorGUILayout.LabelField(formatter(eventView), EditorStyles.wordWrappedLabel);
                DrawSourceIdentity(eventView);
            }
        }

        static string FormatAction(RuntimeDebugEventView eventView)
        {
            RuntimeTracePayload payload = eventView.Event.Payload;
            return $"{eventView.Event.Kind} | {payload.Name} | {payload.Status} | {payload.Cause} | owner {payload.OwnerId} | {payload.Detail}";
        }

        static void DrawSourceIdentity(RuntimeDebugEventView eventView)
        {
            if (!eventView.Source.IsValid)
                return;
            string identity = eventView.Source.GraphAuthoringId.Length > 0
                ? $"{eventView.Source.GraphAuthoringId}/{eventView.Source.ElementAuthoringId}"
                : $"{eventView.Source.TimelineAuthoringId}/{eventView.Source.TrackAuthoringId}/{eventView.Source.ClipAuthoringId}";
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Source", identity);
            if (GUILayout.Button("Open", GUILayout.Width(48f)) && !RuntimeDebugSourceNavigator.Open(eventView.Source))
                Debug.LogError($"Runtime debug source could not be resolved by exact authoring identity: {identity}");
            EditorGUILayout.EndHorizontal();
        }
    }

    [CustomEditor(typeof(CharacterPipelineHost))]
    public sealed class CharacterPipelineHostEditor : UnityEditor.Editor
    {
        void OnEnable()
        {
            RuntimeDebugSession.Shared.Changed += Repaint;
        }

        void OnDisable()
        {
            RuntimeDebugSession.Shared.Changed -= Repaint;
            RuntimeDebugSession.Shared.ReleaseLiveInterest(this);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            CharacterPipelineHost host = target as CharacterPipelineHost;
            if (host == null)
                return;
            CharacterRuntimeDiagnosticsInspector.DrawCharacterPipelineConfiguration(host);
            CharacterRuntimeDiagnosticsInspector.DrawRuntimeDiagnostics(
                this,
                host.GetInstanceID(),
                host.Definition,
                CharacterRuntimeDiagnosticsInspectorMode.Complete);
        }
    }

    [CustomEditor(typeof(FixedCharacterHost))]
    public sealed class FixedCharacterHostEditor : UnityEditor.Editor
    {
        void OnEnable()
        {
            RuntimeDebugSession.Shared.Changed += Repaint;
        }

        void OnDisable()
        {
            RuntimeDebugSession.Shared.Changed -= Repaint;
            RuntimeDebugSession.Shared.ReleaseLiveInterest(this);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            FixedCharacterHost host = target as FixedCharacterHost;
            if (host == null)
                return;
            CharacterRuntimeDiagnosticsInspector.DrawRuntimeDiagnostics(
                this,
                host.GetInstanceID(),
                null,
                CharacterRuntimeDiagnosticsInspectorMode.FootPlacement);
        }
    }
}
