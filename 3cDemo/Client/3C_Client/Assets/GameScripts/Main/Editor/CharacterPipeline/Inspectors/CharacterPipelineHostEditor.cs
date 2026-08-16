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
        const int FootIkSnapshotPollLimit = 120;

        static RuntimeDiagnosticsCaptureDetail s_CaptureDetail = RuntimeDiagnosticsCaptureDetail.Evaluation;
        static readonly object s_FootIkSnapshotInterestOwner = new object();
        static readonly List<RuntimeDebugTargetInfo> s_FootIkSnapshotTargets = new List<RuntimeDebugTargetInfo>();
        static int s_FootIkSnapshotHostInstanceId;
        static int s_FootIkSnapshotPollCount;
        static int s_FootIkSnapshotTargetIndex;

        [MenuItem("Tools/3C/Internal/Dump Foot IK Live Snapshots")]
        static void DumpFootIkLiveSnapshots()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("Foot IK live snapshot requires Play Mode.");
                return;
            }

            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            s_FootIkSnapshotTargets.Clear();
            if (TryGetSelectedHostInstanceId(out int selectedHostInstanceId))
            {
                IReadOnlyList<RuntimeDebugTargetInfo> targets = session.Targets;
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i].HostInstanceId == selectedHostInstanceId)
                    {
                        s_FootIkSnapshotTargets.Add(targets[i]);
                        break;
                    }
                }
            }
            else
            {
                s_FootIkSnapshotTargets.AddRange(session.Targets);
            }

            if (s_FootIkSnapshotTargets.Count == 0)
            {
                Debug.LogError("No runtime diagnostics target is available for Foot IK snapshot.");
                return;
            }

            EditorApplication.update -= PollFootIkSnapshot;
            session.ReleaseLiveInterest(s_FootIkSnapshotInterestOwner);
            s_FootIkSnapshotTargetIndex = 0;
            EditorApplication.update += PollFootIkSnapshot;
            BeginCurrentFootIkSnapshotTarget();
        }

        static bool TryGetSelectedHostInstanceId(out int hostInstanceId)
        {
            GameObject selected = Selection.activeGameObject;
            if (selected)
            {
                FixedCharacterHost fixedHost = selected.GetComponentInParent<FixedCharacterHost>(true);
                if (fixedHost)
                {
                    hostInstanceId = fixedHost.GetInstanceID();
                    return true;
                }

                CharacterPipelineHost host = selected.GetComponentInParent<CharacterPipelineHost>(true);
                if (host)
                {
                    hostInstanceId = host.GetInstanceID();
                    return true;
                }
            }

            hostInstanceId = 0;
            return false;
        }

        static void PollFootIkSnapshot()
        {
            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            if (!EditorApplication.isPlaying ||
                !session.ViewModel.Attached ||
                session.ViewModel.Target.HostInstanceId != s_FootIkSnapshotHostInstanceId)
            {
                if (++s_FootIkSnapshotPollCount < FootIkSnapshotPollLimit)
                    return;
                Debug.LogError($"Foot IK live snapshot timed out before target {CurrentFootIkSnapshotTargetName} became readable.");
                AdvanceFootIkSnapshotTarget();
                return;
            }

            IReadOnlyList<RuntimeDebugEventView> events = Filter(
                session.ViewModel,
                RuntimeTraceChannel.FootPlacement,
                RuntimeTraceEventKind.FootPlacementSnapshot);
            if (events.Count == 0)
            {
                if (++s_FootIkSnapshotPollCount < FootIkSnapshotPollLimit)
                    return;
                Debug.LogError($"Foot IK live snapshot timed out before target {CurrentFootIkSnapshotTargetName} published a FootPlacementSnapshot.");
                AdvanceFootIkSnapshotTarget();
                return;
            }

            RuntimeFootIkTraceSnapshot snapshot = events[0].Event.Payload.FootIk;
            Debug.Log($"{CurrentFootIkSnapshotTargetName} | position {ResolveCurrentFootIkTargetPosition()} | {FormatFootIkLiveSnapshot(snapshot)}");
            AdvanceFootIkSnapshotTarget();
        }

        static string ResolveCurrentFootIkTargetPosition()
        {
            UnityEngine.Object target = EditorUtility.InstanceIDToObject(s_FootIkSnapshotHostInstanceId);
            if (target is not Component component)
                return "unavailable";

            Vector3 position = component switch
            {
                FixedCharacterHost fixedHost => fixedHost.VisualPosition,
                CharacterPipelineHost host when host.VisualRoot => host.VisualRoot.position,
                _ => component.transform.position
            };
            return $"{position.x:0.###}/{position.y:0.###}/{position.z:0.###}";
        }

        static string CurrentFootIkSnapshotTargetName =>
            s_FootIkSnapshotTargetIndex >= 0 && s_FootIkSnapshotTargetIndex < s_FootIkSnapshotTargets.Count
                ? s_FootIkSnapshotTargets[s_FootIkSnapshotTargetIndex].DisplayName
                : "Unknown Target";

        static void BeginCurrentFootIkSnapshotTarget()
        {
            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            RuntimeDebugTargetInfo target = s_FootIkSnapshotTargets[s_FootIkSnapshotTargetIndex];
            if (!session.AttachToTarget(target.CharacterRuntimeId))
            {
                Debug.LogError($"Foot IK live snapshot could not attach target {target.DisplayName}.");
                AdvanceFootIkSnapshotTarget();
                return;
            }

            session.EnsureLiveInterest(s_FootIkSnapshotInterestOwner, RuntimeTraceChannel.FootPlacement);
            s_FootIkSnapshotHostInstanceId = target.HostInstanceId;
            s_FootIkSnapshotPollCount = 0;
        }

        static void AdvanceFootIkSnapshotTarget()
        {
            RuntimeDebugSession.Shared.ReleaseLiveInterest(s_FootIkSnapshotInterestOwner);
            s_FootIkSnapshotTargetIndex++;
            if (s_FootIkSnapshotTargetIndex < s_FootIkSnapshotTargets.Count)
            {
                BeginCurrentFootIkSnapshotTarget();
                return;
            }

            CompleteFootIkSnapshotPoll();
        }

        static void CompleteFootIkSnapshotPoll()
        {
            EditorApplication.update -= PollFootIkSnapshot;
            RuntimeDebugSession.Shared.ReleaseLiveInterest(s_FootIkSnapshotInterestOwner);
            s_FootIkSnapshotTargets.Clear();
            s_FootIkSnapshotHostInstanceId = 0;
            s_FootIkSnapshotPollCount = 0;
            s_FootIkSnapshotTargetIndex = 0;
        }

        static string FormatFootIkLiveSnapshot(RuntimeFootIkTraceSnapshot snapshot)
        {
            RuntimeFootIkLegTraceSnapshot left = snapshot.Left;
            RuntimeFootIkLegTraceSnapshot right = snapshot.Right;
            return $"Foot IK Live | frame {snapshot.FrameSequence} | completion {snapshot.GroundingCompletionIdentity}/{snapshot.ModifierCompletionIdentity}->{snapshot.SolverCompletionIdentity} | " +
                   $"solver {snapshot.SolverFailure} | alpha/body {snapshot.PlacementAlpha:0.###}/{snapshot.BodyGrounded} | " +
                   $"L hit {left.DidCurrentTraceHit} offset {left.TargetOffset:0.###}+{left.SoleClearanceTarget:0.###}->{left.CurrentOffset:0.###} contact {left.ContactState}/{left.ContactDecision} anchor {left.AnchorBlendWeight:0.###} finalPhysical {left.FinalPhysicalResidualPenetration:0.###} event {left.LandingEventIdentity}/{left.LandingEventIdentityValid} plan {left.PlanLandingEventIdentity}/{left.PredictivePlanState}@{left.PlanElapsedSeconds:0.###} progress {left.PredictiveExecutionProgress:0.###} rewrite {left.PredictiveRewritten}/{left.PredictionRejectReason} lift {left.AppliedLift:0.###} residual {left.PositionResidual:0.###} | " +
                   $"R hit {right.DidCurrentTraceHit} offset {right.TargetOffset:0.###}+{right.SoleClearanceTarget:0.###}->{right.CurrentOffset:0.###} contact {right.ContactState}/{right.ContactDecision} anchor {right.AnchorBlendWeight:0.###} finalPhysical {right.FinalPhysicalResidualPenetration:0.###} event {right.LandingEventIdentity}/{right.LandingEventIdentityValid} plan {right.PlanLandingEventIdentity}/{right.PredictivePlanState}@{right.PlanElapsedSeconds:0.###} progress {right.PredictiveExecutionProgress:0.###} rewrite {right.PredictiveRewritten}/{right.PredictionRejectReason} lift {right.AppliedLift:0.###} residual {right.PositionResidual:0.###} | " +
                   $"pelvis {snapshot.PelvisLyraTargetOffset:0.###}->{snapshot.PelvisResolvedTargetOffset:0.###}->{snapshot.CurrentPelvisOffset:0.###} velocity {snapshot.PelvisSpringVelocity:0.###}";
        }

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
                           $"L {footIk.Left.ContactState}/{footIk.Left.ContactDecision} offset {footIk.Left.TargetOffset:0.###}+{footIk.Left.SoleClearanceTarget:0.###}->{footIk.Left.CurrentOffset:0.###} event {footIk.Left.LandingEventIdentity}/{footIk.Left.LandingEventIdentityValid} plan {footIk.Left.PlanLandingEventIdentity}/{footIk.Left.PredictivePlanState}@{footIk.Left.PlanElapsedSeconds:0.###} progress {footIk.Left.PredictiveExecutionProgress:0.###} anchor {footIk.Left.AnchorBlendWeight:0.###} finalPhysical {footIk.Left.FinalPhysicalResidualPenetration:0.###} rewrite {footIk.Left.PredictiveRewritten} residual {footIk.Left.PositionResidual:0.###} | " +
                           $"R {footIk.Right.ContactState}/{footIk.Right.ContactDecision} offset {footIk.Right.TargetOffset:0.###}+{footIk.Right.SoleClearanceTarget:0.###}->{footIk.Right.CurrentOffset:0.###} event {footIk.Right.LandingEventIdentity}/{footIk.Right.LandingEventIdentityValid} plan {footIk.Right.PlanLandingEventIdentity}/{footIk.Right.PredictivePlanState}@{footIk.Right.PlanElapsedSeconds:0.###} progress {footIk.Right.PredictiveExecutionProgress:0.###} anchor {footIk.Right.AnchorBlendWeight:0.###} finalPhysical {footIk.Right.FinalPhysicalResidualPenetration:0.###} rewrite {footIk.Right.PredictiveRewritten} residual {footIk.Right.PositionResidual:0.###} | " +
                           $"pelvis {footIk.PelvisLyraTargetOffset:0.###}->{footIk.PelvisResolvedTargetOffset:0.###}->{footIk.CurrentPelvisOffset:0.###}";
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

        internal static void AppendFootIkHeader(StringBuilder builder)
        {
            var values = new List<string>
            {
                "presentation_position", "trace_sequence", "frame_sequence", "reset_sequence",
                "grounding_completion", "modifier_completion", "solver_completion", "has_modifier",
                "solver_backend", "solver_failure", "node_executed", "body_grounded", "placement_alpha", "presentation_delta_seconds", "pose_root_vertical_delta",
                "pose_root_world_x", "pose_root_world_y", "pose_root_world_z", "pose_root_world_rotation_x", "pose_root_world_rotation_y", "pose_root_world_rotation_z", "pose_root_world_rotation_w",
                "lyra_source_identity", "spring_identity", "rig_id", "rig_revision", "profile_id", "profile_revision",
                "pose_plan_hash", "calibration_id", "calibration_revision", "physics_scene_identity", "self_filter_identity",
                "pelvis_lyra_target", "pelvis_resolved_target", "pelvis_current", "pelvis_spring_velocity",
                "pelvis_previous_target", "pelvis_spring_initialized", "pelvis_translation_x", "pelvis_translation_y",
                "pelvis_translation_z", "pelvis_goal_weight", "pelvis_goal_application", "pelvis_goal_source",
                "pelvis_support_available", "pelvis_support_side", "pelvis_support_switched", "pelvis_support_plan_sequence",
                "pelvis_current_support_target", "pelvis_selected_support_target",
                "left_pelvis_has_action_constraint", "left_pelvis_constraint_mode", "left_pelvis_support_phase",
                "left_pelvis_body_pivot_mode", "left_pelvis_candidate", "left_pelvis_plan_sequence", "left_pelvis_displacement",
                "right_pelvis_has_action_constraint", "right_pelvis_constraint_mode", "right_pelvis_support_phase",
                "right_pelvis_body_pivot_mode", "right_pelvis_candidate", "right_pelvis_plan_sequence", "right_pelvis_displacement",
                "baseline_producer_operation", "baseline_producer_call_site",
                "baseline_goal_offset", "baseline_goal_count", "baseline_rig_id", "baseline_rig_revision"
            };
            AppendFootIkLegHeader(values, "left");
            AppendFootIkLegHeader(values, "right");
            AppendCsvRow(builder, values.ToArray());
        }

        static void AppendFootIkRow(StringBuilder builder, RuntimeTraceEvent traceEvent)
        {
            RuntimeFootIkTraceSnapshot snapshot = traceEvent.Payload.FootIk;
            RuntimeFootIkLegTraceSnapshot left = snapshot.Left;
            RuntimeFootIkLegTraceSnapshot right = snapshot.Right;
            var values = new List<string>
            {
                Number(traceEvent.Position), Number(traceEvent.Sequence), Number(snapshot.FrameSequence), Number(snapshot.ResetSequence),
                Number(snapshot.GroundingCompletionIdentity), Number(snapshot.ModifierCompletionIdentity), Number(snapshot.SolverCompletionIdentity), Bool(snapshot.HasPredictiveModifier),
                snapshot.SolverBackendIdentity, snapshot.SolverFailure, Bool(snapshot.NodeExecuted), Bool(snapshot.BodyGrounded), Number(snapshot.PlacementAlpha), Number(snapshot.PresentationDeltaSeconds), Number(snapshot.PoseRootVerticalDelta),
                Number(snapshot.PoseRootWorldPosition.x), Number(snapshot.PoseRootWorldPosition.y), Number(snapshot.PoseRootWorldPosition.z),
                Number(snapshot.PoseRootWorldRotation.x), Number(snapshot.PoseRootWorldRotation.y), Number(snapshot.PoseRootWorldRotation.z), Number(snapshot.PoseRootWorldRotation.w),
                snapshot.LyraSourceIdentity, snapshot.SpringIdentity, snapshot.RigId, snapshot.RigRevision, snapshot.ProfileId, snapshot.ProfileRevision,
                snapshot.PosePlanHash, snapshot.CalibrationId, snapshot.CalibrationRevision, Number(snapshot.PhysicsSceneIdentity), Number(snapshot.SelfFilterIdentity),
                Number(snapshot.PelvisLyraTargetOffset), Number(snapshot.PelvisResolvedTargetOffset), Number(snapshot.CurrentPelvisOffset), Number(snapshot.PelvisSpringVelocity),
                Number(snapshot.PreviousPelvisTarget), Bool(snapshot.PelvisSpringInitialized), Number(snapshot.PelvisPreSolveTranslation.x), Number(snapshot.PelvisPreSolveTranslation.y),
                Number(snapshot.PelvisPreSolveTranslation.z), Number(snapshot.PelvisGoalPositionWeight), snapshot.PelvisGoalApplication, snapshot.PelvisGoalSourceKind,
                Bool(snapshot.PelvisSupportAvailable), snapshot.PelvisSupportSide, Bool(snapshot.PelvisSupportSwitched), Number(snapshot.PelvisSupportPlanSequence),
                Number(snapshot.PelvisCurrentSupportTarget), Number(snapshot.PelvisSelectedSupportTarget),
                Bool(snapshot.LeftPelvisHasActionConstraint), snapshot.LeftPelvisConstraintMode, snapshot.LeftPelvisSupportPhase,
                snapshot.LeftPelvisBodyPivotMode, Bool(snapshot.LeftPelvisCandidate), Number(snapshot.LeftPelvisPlanSequence), Number(snapshot.LeftPelvisDisplacement),
                Bool(snapshot.RightPelvisHasActionConstraint), snapshot.RightPelvisConstraintMode, snapshot.RightPelvisSupportPhase,
                snapshot.RightPelvisBodyPivotMode, Bool(snapshot.RightPelvisCandidate), Number(snapshot.RightPelvisPlanSequence), Number(snapshot.RightPelvisDisplacement),
                Number(snapshot.BaselineProducerOperationIndex), Number(snapshot.BaselineProducerCallSiteIndex),
                Number(snapshot.BaselineGoalOffset), Number(snapshot.BaselineGoalCount), snapshot.BaselineRigId, snapshot.BaselineRigRevision
            };
            AppendFootIkLegValues(values, left);
            AppendFootIkLegValues(values, right);
            AppendCsvRow(builder, values.ToArray());
        }

        static void AppendFootIkLegHeader(List<string> values, string prefix)
        {
            string[] beforePath =
            {
                "hit", "surface", "query_shape", "query_purpose", "query_foot_index",
                "query_origin_x", "query_origin_y", "query_origin_z", "query_capsule_end_x", "query_capsule_end_y", "query_capsule_end_z",
                "query_direction_x", "query_direction_y", "query_direction_z", "query_radius", "query_maximum_distance", "query_layer_mask", "query_minimum_ground_normal_dot",
                "hit_location_x", "hit_location_y", "hit_location_z", "impact_point_x", "impact_point_y", "impact_point_z",
                "hit_normal_x", "hit_normal_y", "hit_normal_z", "hit_distance",
                "contact", "transition", "contact_decision", "contact_surface_valid", "contact_surface_distance_accepted",
                "contact_capture_speed_accepted", "contact_retention_speed_accepted", "contact_confidence_accepted",
                "maximum_contact_surface_distance", "plant_speed_threshold", "unalignment_speed_threshold",
                "plant_confidence_enter", "plant_confidence_exit", "anchor_distance", "anchor_distance_accepted",
                "maximum_anchor_distance", "anchor_blend_speed", "has_anchor", "anchor_local_x", "anchor_local_y", "anchor_local_z",
                "anchor_local_rotation_x", "anchor_local_rotation_y", "anchor_local_rotation_z", "anchor_local_rotation_w",
                "anchor_world_x", "anchor_world_y", "anchor_world_z", "anchor_world_rotation_x", "anchor_world_rotation_y", "anchor_world_rotation_z", "anchor_world_rotation_w", "anchor_blend",
                "foot_feature_valid", "predicted_step_valid", "predicted_step_has_landing_event",
                "predicted_step_source_bound", "has_authoritative_landing_event", "expected_landing_event_identity",
                "landing_event_identity_valid", "current_event_is_pre_swing", "current_event_is_swing",
                "rewritten", "prediction_reject", "future_surface",
                "future_point_x", "future_point_y", "future_point_z", "future_normal_x", "future_normal_y", "future_normal_z",
                "ground_envelope_count", "ground_envelope_reject", "predictive_query_count", "predictive_raw_hit_count", "predictive_rejected_query_count",
                "predictive_reject_no_candidate_count", "predictive_reject_height_discontinuity_count", "predictive_reject_edge_gap_count",
                "predictive_reject_surface_discontinuity_count", "predictive_reject_reach_exceeded_count", "predictive_reject_slope_exceeded_count",
                "predictive_reject_step_exceeded_count", "predictive_reject_invalid_candidate_count", "predictive_reject_unsupported_center_count",
                "future_landing_query_available", "future_landing_query_shape", "future_landing_query_purpose",
                "future_landing_query_origin_x", "future_landing_query_origin_y", "future_landing_query_origin_z",
                "future_landing_query_direction_x", "future_landing_query_direction_y", "future_landing_query_direction_z",
                "future_landing_query_radius", "future_landing_query_maximum_distance", "future_landing_query_minimum_ground_normal_dot", "prediction_horizon",
                "landing_event_identity", "source_sample_identity", "source_sample_cycle", "event_ordinal", "contribution_continuity_identity",
                "current_event_foot_pose_weight", "plan_prediction_blend", "authoritative_prediction_blend",
                "has_plan_revision", "revision_plan_sequence", "plan_revision_blend_weight", "plan_transition_kind",
                "plan_attempt_available", "plan_attempt_kind", "plan_attempt_sequence", "plan_attempt_generated_frame", "plan_attempt_landing_event_identity",
                "plan_attempt_state", "plan_attempt_reject", "plan_attempt_ground_envelope_reject", "plan_attempt_query_count", "plan_attempt_raw_hit_count", "plan_attempt_rejected_query_count",
                "plan_attempt_origin_kind", "plan_attempt_origin_plan_sequence", "plan_attempt_origin_landing_event_identity",
                "plan_attempt_origin_sole_x", "plan_attempt_origin_sole_y", "plan_attempt_origin_sole_z",
                "plan_attempt_origin_ground_path_x", "plan_attempt_origin_ground_path_y", "plan_attempt_origin_ground_path_z",
                "plan_attempt_origin_support_surface", "plan_attempt_origin_support_point_x", "plan_attempt_origin_support_point_y", "plan_attempt_origin_support_point_z",
                "plan_attempt_origin_support_normal_x", "plan_attempt_origin_support_normal_y", "plan_attempt_origin_support_normal_z", "plan_attempt_origin_sole_height_above_support",
                "plan_attempt_request_source_sample_identity", "plan_attempt_request_source_sample_cycle", "plan_attempt_request_event_ordinal",
                "plan_attempt_request_event_phase", "plan_attempt_request_time_to_landing_seconds",
                "plan_attempt_request_motion_generation", "plan_attempt_request_motion_authority_tick",
                "plan_attempt_request_motion_current_velocity_x", "plan_attempt_request_motion_current_velocity_z",
                "plan_attempt_request_motion_continuation_velocity_x", "plan_attempt_request_motion_continuation_velocity_z",
                "plan_attempt_request_motion_yaw_velocity_degrees_per_second",
                "plan_attempt_request_root_start_x", "plan_attempt_request_root_start_y", "plan_attempt_request_root_start_z",
                "plan_attempt_request_root_start_rotation_x", "plan_attempt_request_root_start_rotation_y",
                "plan_attempt_request_root_start_rotation_z", "plan_attempt_request_root_start_rotation_w",
                "plan_attempt_request_presented_body_start_x", "plan_attempt_request_presented_body_start_y", "plan_attempt_request_presented_body_start_z",
                "plan_attempt_request_committed_body_velocity_x", "plan_attempt_request_committed_body_velocity_y", "plan_attempt_request_committed_body_velocity_z",
                "plan_attempt_request_trajectory_curvature_degrees_per_second", "plan_attempt_request_trajectory_curvature_available",
                "plan_attempt_request_movement_playback_time",
                "plan_attempt_request_up_x", "plan_attempt_request_up_y", "plan_attempt_request_up_z",
                "plan_attempt_request_sole_support_radius", "plan_attempt_request_leg_length",
                "plan_fading_out", "plan_retention_weight",
                "intent_landing_displacement_error", "intent_landing_displacement_threshold",
                "landing_confidence", "authored_landing_delay", "landing_event_phase", "landing_lift_off_phase",
                "root_local_landing_x", "root_local_landing_y", "root_local_landing_z",
                "root_local_route_0_x", "root_local_route_0_y", "root_local_route_0_z",
                "root_local_route_1_x", "root_local_route_1_y", "root_local_route_1_z",
                "root_local_route_2_x", "root_local_route_2_y", "root_local_route_2_z",
                "root_local_route_3_x", "root_local_route_3_y", "root_local_route_3_z",
                "root_local_route_4_x", "root_local_route_4_y", "root_local_route_4_z",
                "root_local_route_5_x", "root_local_route_5_y", "root_local_route_5_z",
                "root_local_route_6_x", "root_local_route_6_y", "root_local_route_6_z",
                "root_local_route_7_x", "root_local_route_7_y", "root_local_route_7_z",
                "root_local_route_8_x", "root_local_route_8_y", "root_local_route_8_z",
                "root_local_route_9_x", "root_local_route_9_y", "root_local_route_9_z",
                "root_local_route_10_x", "root_local_route_10_y", "root_local_route_10_z",
                "root_local_route_11_x", "root_local_route_11_y", "root_local_route_11_z",
                "root_local_route_12_x", "root_local_route_12_y", "root_local_route_12_z",
                "root_local_route_13_x", "root_local_route_13_y", "root_local_route_13_z",
                "root_local_route_14_x", "root_local_route_14_y", "root_local_route_14_z",
                "root_local_route_15_x", "root_local_route_15_y", "root_local_route_15_z",
                "root_local_route_16_x", "root_local_route_16_y", "root_local_route_16_z",
                "root_local_route_17_x", "root_local_route_17_y", "root_local_route_17_z",
                "root_local_route_18_x", "root_local_route_18_y", "root_local_route_18_z",
                "root_local_route_19_x", "root_local_route_19_y", "root_local_route_19_z",
                "root_local_route_20_x", "root_local_route_20_y", "root_local_route_20_z",
                "root_local_route_21_x", "root_local_route_21_y", "root_local_route_21_z",
                "root_local_route_22_x", "root_local_route_22_y", "root_local_route_22_z",
                "root_local_route_23_x", "root_local_route_23_y", "root_local_route_23_z",
                "root_local_route_24_x", "root_local_route_24_y", "root_local_route_24_z",
                "authored_foot_route_start_x", "authored_foot_route_start_y", "authored_foot_route_start_z",
                "authored_foot_route_landing_x", "authored_foot_route_landing_y", "authored_foot_route_landing_z",
                "prediction_distance", "predictive_plan_sequence", "predictive_plan_generated_frame", "plan_generation_phase",
                "incoming_predicted_step_valid", "incoming_landing_event_identity_valid", "incoming_landing_event_identity",
                "incoming_event_phase", "incoming_lift_off_phase",
                "predictive_plan_state", "predictive_plan_transition", "predictive_plan_end_reason", "predictive_execution_progress",
                "plan_landing_event_identity", "plan_source_sample_identity", "plan_source_sample_cycle", "plan_event_ordinal",
                "plan_contribution_continuity_identity", "plan_elapsed_seconds", "plan_seconds_to_lift_off", "plan_swing_duration",
                "plan_has_path_geometry", "plan_has_executable_path",
                "frozen_planar_velocity_x", "frozen_planar_velocity_y", "frozen_planar_velocity_z",
                "trajectory_curvature_degrees_per_second", "trajectory_curvature_available",
                "frozen_trajectory_curvature_degrees_per_second", "frozen_trajectory_curvature_available",
                "frozen_yaw_velocity_degrees_per_second", "frozen_maximum_yaw_velocity_degrees_per_second",
                "motion_linear_landing_error", "motion_angular_landing_error",
                "motion_landing_error", "motion_landing_tolerance",
                "current_sole_world_x", "current_sole_world_y", "current_sole_world_z",
                "fixed_path_start_world_x", "fixed_path_start_world_y", "fixed_path_start_world_z",
                "fixed_landing_world_x", "fixed_landing_world_y", "fixed_landing_world_z",
                "current_path_world_x", "current_path_world_y", "current_path_world_z",
                "current_path_root_world_x", "current_path_root_world_y", "current_path_root_world_z",
                "current_path_hip_world_x", "current_path_hip_world_y", "current_path_hip_world_z",
                "predicted_hip_world_x", "predicted_hip_world_y", "predicted_hip_world_z",
                "frozen_root_start_world_x", "frozen_root_start_world_y", "frozen_root_start_world_z",
                "frozen_root_start_rotation_x", "frozen_root_start_rotation_y", "frozen_root_start_rotation_z", "frozen_root_start_rotation_w",
                "frozen_root_landing_world_x", "frozen_root_landing_world_y", "frozen_root_landing_world_z",
                "frozen_root_landing_rotation_x", "frozen_root_landing_rotation_y", "frozen_root_landing_rotation_z", "frozen_root_landing_rotation_w",
                "prediction_up_x", "prediction_up_y", "prediction_up_z",
                "minimum_landing_confidence", "maximum_prediction_reach_ratio", "prediction_reach_ratio",
                "prediction_cast_above", "prediction_cast_below", "prediction_route_sample_count", "prediction_accepted_hit_count", "prediction_edge_plane_candidate_count", "prediction_accepted_edge_plane_count", "path_sphere_radius", "swing_capsule_radius", "sole_support_radius",
                "current_path_surface", "current_path_support_x", "current_path_support_y", "current_path_support_z",
                "current_path_normal_x", "current_path_normal_y", "current_path_normal_z",
                "pre_clearance_heel_path_distance", "pre_clearance_toe_path_distance",
                "post_clearance_heel_path_distance", "post_clearance_toe_path_distance",
                "predictive_clearance_evaluated", "predictive_residual_penetration", "planned_foot_route_world_sample_count",
                "planned_foot_route_world_0_x", "planned_foot_route_world_0_y", "planned_foot_route_world_0_z",
                "planned_foot_route_world_1_x", "planned_foot_route_world_1_y", "planned_foot_route_world_1_z",
                "planned_foot_route_world_2_x", "planned_foot_route_world_2_y", "planned_foot_route_world_2_z",
                "planned_foot_route_world_3_x", "planned_foot_route_world_3_y", "planned_foot_route_world_3_z",
                "planned_foot_route_world_4_x", "planned_foot_route_world_4_y", "planned_foot_route_world_4_z",
                "planned_foot_route_world_5_x", "planned_foot_route_world_5_y", "planned_foot_route_world_5_z",
                "planned_foot_route_world_6_x", "planned_foot_route_world_6_y", "planned_foot_route_world_6_z",
                "path_diagnostic_sample_count"
            };
            for (int i = 0; i < beforePath.Length; i++)
                values.Add($"{prefix}_{beforePath[i]}");
            for (int i = 0; i < 8; i++)
                AppendFootIkPathSampleHeader(values, prefix, i);
            string[] afterPath =
            {
                "authored_animation_clearance", "animation_clearance_continuity_offset",
                "animation_clearance_continuity_contribution", "reach_clearance", "composite_animation_clearance",
                "required_lift", "applied_lift",
                "baseline_goal_world_x", "baseline_goal_world_y", "baseline_goal_world_z",
                "final_goal_world_x", "final_goal_world_y", "final_goal_world_z",
                "baseline_application", "final_source", "solver_result_available", "plant_confidence", "plant_contact", "sole_height",
                "placement_weight", "animation_foot_speed", "surface_distance",
                "sole_support_surface", "sole_support_point_x", "sole_support_point_y", "sole_support_point_z",
                "sole_support_normal_x", "sole_support_normal_y", "sole_support_normal_z",
                "sole_clearance_target", "baseline_sole_pose_seq", "final_goal_sole_pose_seq", "final_solved_sole_pose_seq",
                "final_physical_support_kind", "final_physical_support_surface",
                "final_physical_support_point_x", "final_physical_support_point_y", "final_physical_support_point_z",
                "final_physical_support_normal_x", "final_physical_support_normal_y", "final_physical_support_normal_z",
                "final_physical_heel_plane_distance", "final_physical_toe_plane_distance", "final_physical_residual_penetration", "final_physical_evaluated",
                "animated_ankle_component_y",
                "baseline_position_weight", "baseline_rotation_weight",
                "final_position_weight", "final_rotation_weight", "target_offset", "offset_target", "current_offset", "offset_spring_velocity", "previous_offset_target", "offset_spring_initialized",
                "target_normal_x", "target_normal_y", "target_normal_z", "current_normal_x", "current_normal_y", "current_normal_z",
                "normal_spring_velocity_x", "normal_spring_velocity_y", "normal_spring_velocity_z",
                "previous_normal_target_x", "previous_normal_target_y", "previous_normal_target_z", "normal_spring_initialized",
                "current_grounding_x", "current_grounding_y", "current_grounding_z", "baseline_x", "baseline_y", "baseline_z", "final_x", "final_y", "final_z",
                "solved_x", "solved_y", "solved_z", "position_residual", "rotation_residual_degrees"
            };
            for (int i = 0; i < afterPath.Length; i++)
                values.Add($"{prefix}_{afterPath[i]}");
        }

        static void AppendFootIkPathSampleHeader(List<string> values, string prefix, int index)
        {
            string sample = $"{prefix}_path_{index}";
            values.Add($"{sample}_fraction");
            values.Add($"{sample}_position_x");
            values.Add($"{sample}_position_y");
            values.Add($"{sample}_position_z");
            values.Add($"{sample}_normal_x");
            values.Add($"{sample}_normal_y");
            values.Add($"{sample}_normal_z");
            values.Add($"{sample}_surface");
            values.Add($"{sample}_root_x");
            values.Add($"{sample}_root_y");
            values.Add($"{sample}_root_z");
            values.Add($"{sample}_hip_x");
            values.Add($"{sample}_hip_y");
            values.Add($"{sample}_hip_z");
        }

        internal static void AppendFootIkLegValues(List<string> values, RuntimeFootIkLegTraceSnapshot leg)
        {
            values.AddRange(new[]
            {
                Bool(leg.DidCurrentTraceHit), Number(leg.CurrentSurfaceIdentity), leg.CurrentQueryShape, leg.CurrentQueryPurpose, Number(leg.CurrentQueryFootIndex),
                Number(leg.CurrentQueryOrigin.x), Number(leg.CurrentQueryOrigin.y), Number(leg.CurrentQueryOrigin.z),
                Number(leg.CurrentQueryCapsuleEnd.x), Number(leg.CurrentQueryCapsuleEnd.y), Number(leg.CurrentQueryCapsuleEnd.z),
                Number(leg.CurrentQueryDirection.x), Number(leg.CurrentQueryDirection.y), Number(leg.CurrentQueryDirection.z),
                Number(leg.CurrentQueryRadius), Number(leg.CurrentQueryMaximumDistance), Number(leg.CurrentQueryLayerMask), Number(leg.CurrentQueryMinimumGroundNormalDot),
                Number(leg.CurrentHitLocation.x), Number(leg.CurrentHitLocation.y), Number(leg.CurrentHitLocation.z),
                Number(leg.CurrentImpactPoint.x), Number(leg.CurrentImpactPoint.y), Number(leg.CurrentImpactPoint.z),
                Number(leg.CurrentHitNormal.x), Number(leg.CurrentHitNormal.y), Number(leg.CurrentHitNormal.z), Number(leg.CurrentHitDistance),
                leg.ContactState, leg.TransitionReason, leg.ContactDecision, Bool(leg.ContactSurfaceValid), Bool(leg.ContactSurfaceDistanceAccepted),
                Bool(leg.ContactCaptureSpeedAccepted), Bool(leg.ContactRetentionSpeedAccepted), Bool(leg.ContactConfidenceAccepted),
                Number(leg.MaximumContactSurfaceDistance), Number(leg.PlantSpeedThreshold), Number(leg.UnalignmentSpeedThreshold),
                Number(leg.PlantConfidenceEnter), Number(leg.PlantConfidenceExit), Number(leg.AnchorDistance), Bool(leg.AnchorDistanceAccepted),
                Number(leg.MaximumAnchorDistance), Number(leg.AnchorBlendSpeed), Bool(leg.HasSurfaceAnchor),
                Number(leg.SurfaceLocalAnchor.x), Number(leg.SurfaceLocalAnchor.y), Number(leg.SurfaceLocalAnchor.z),
                Number(leg.SurfaceLocalRotation.x), Number(leg.SurfaceLocalRotation.y), Number(leg.SurfaceLocalRotation.z), Number(leg.SurfaceLocalRotation.w),
                Number(leg.AnchorWorldPosition.x), Number(leg.AnchorWorldPosition.y), Number(leg.AnchorWorldPosition.z),
                Number(leg.AnchorWorldRotation.x), Number(leg.AnchorWorldRotation.y), Number(leg.AnchorWorldRotation.z), Number(leg.AnchorWorldRotation.w), Number(leg.AnchorBlendWeight),
                Bool(leg.FootFeatureValid), Bool(leg.PredictedStepValid), Bool(leg.PredictedStepHasLandingEvent),
                Bool(leg.PredictedStepSourceBound), Bool(leg.HasAuthoritativeLandingEvent), Number(leg.ExpectedLandingEventIdentity),
                Bool(leg.LandingEventIdentityValid), Bool(leg.CurrentEventIsPreSwing), Bool(leg.CurrentEventIsSwing),
                Bool(leg.PredictiveRewritten), leg.PredictionRejectReason, Number(leg.FutureSurfaceIdentity),
                Number(leg.FutureSupportPoint.x), Number(leg.FutureSupportPoint.y), Number(leg.FutureSupportPoint.z),
                Number(leg.FutureSupportNormal.x), Number(leg.FutureSupportNormal.y), Number(leg.FutureSupportNormal.z),
                Number(leg.GroundEnvelopeSegmentCount), leg.GroundEnvelopeRejectReason, Number(leg.PredictiveQueryCount), Number(leg.PredictiveRawHitCount), Number(leg.PredictiveRejectedQueryCount),
                Number(leg.PredictiveRejectNoCandidateCount), Number(leg.PredictiveRejectHeightDiscontinuityCount), Number(leg.PredictiveRejectEdgeGapCount),
                Number(leg.PredictiveRejectSurfaceDiscontinuityCount), Number(leg.PredictiveRejectReachExceededCount), Number(leg.PredictiveRejectSlopeExceededCount),
                Number(leg.PredictiveRejectStepExceededCount), Number(leg.PredictiveRejectInvalidCandidateCount), Number(leg.PredictiveRejectUnsupportedCenterCount),
                Bool(leg.FutureLandingQueryAvailable), leg.FutureLandingQueryShape, leg.FutureLandingQueryPurpose,
                Number(leg.FutureLandingQueryOrigin.x), Number(leg.FutureLandingQueryOrigin.y), Number(leg.FutureLandingQueryOrigin.z),
                Number(leg.FutureLandingQueryDirection.x), Number(leg.FutureLandingQueryDirection.y), Number(leg.FutureLandingQueryDirection.z),
                Number(leg.FutureLandingQueryRadius), Number(leg.FutureLandingQueryMaximumDistance), Number(leg.FutureLandingQueryMinimumGroundNormalDot), Number(leg.PredictionHorizon),
                Number(leg.LandingEventIdentity), Number(leg.SourceSampleIdentity), Number(leg.SourceSampleCycle), Number(leg.EventOrdinal), Number(leg.ContributionContinuityIdentity),
                Number(leg.CurrentEventFootPoseWeight), Number(leg.PlanPredictionBlend), Number(leg.AuthoritativePredictionBlend),
                Bool(leg.HasPlanRevision), Number(leg.RevisionPlanSequence), Number(leg.PlanRevisionBlendWeight), leg.PlanTransitionKind,
                Bool(leg.PlanAttemptAvailable), leg.PlanAttemptKind, Number(leg.PlanAttemptSequence), Number(leg.PlanAttemptGeneratedFrame), Number(leg.PlanAttemptLandingEventIdentity),
                leg.PlanAttemptState, leg.PlanAttemptRejectReason, leg.PlanAttemptGroundEnvelopeRejectReason, Number(leg.PlanAttemptQueryCount), Number(leg.PlanAttemptRawHitCount), Number(leg.PlanAttemptRejectedQueryCount),
                leg.PlanAttemptOriginKind, Number(leg.PlanAttemptOriginPlanSequence), Number(leg.PlanAttemptOriginLandingEventIdentity),
                Number(leg.PlanAttemptOriginSole.x), Number(leg.PlanAttemptOriginSole.y), Number(leg.PlanAttemptOriginSole.z),
                Number(leg.PlanAttemptOriginGroundPath.x), Number(leg.PlanAttemptOriginGroundPath.y), Number(leg.PlanAttemptOriginGroundPath.z),
                Number(leg.PlanAttemptOriginSupportSurfaceIdentity), Number(leg.PlanAttemptOriginSupportPoint.x), Number(leg.PlanAttemptOriginSupportPoint.y), Number(leg.PlanAttemptOriginSupportPoint.z),
                Number(leg.PlanAttemptOriginSupportNormal.x), Number(leg.PlanAttemptOriginSupportNormal.y), Number(leg.PlanAttemptOriginSupportNormal.z), Number(leg.PlanAttemptOriginSoleHeightAboveSupport),
                Number(leg.PlanAttemptRequestSourceSampleIdentity), Number(leg.PlanAttemptRequestSourceSampleCycle), Number(leg.PlanAttemptRequestEventOrdinal),
                Number(leg.PlanAttemptRequestEventPhase), Number(leg.PlanAttemptRequestTimeToLandingSeconds),
                Number(leg.PlanAttemptRequestMotionGeneration), Number(leg.PlanAttemptRequestMotionAuthorityTick),
                Number(leg.PlanAttemptRequestMotionCurrentVelocity.x), Number(leg.PlanAttemptRequestMotionCurrentVelocity.y),
                Number(leg.PlanAttemptRequestMotionContinuationVelocity.x), Number(leg.PlanAttemptRequestMotionContinuationVelocity.y),
                Number(leg.PlanAttemptRequestMotionYawVelocityDegreesPerSecond),
                Number(leg.PlanAttemptRequestRootStart.x), Number(leg.PlanAttemptRequestRootStart.y), Number(leg.PlanAttemptRequestRootStart.z),
                Number(leg.PlanAttemptRequestRootStartRotation.x), Number(leg.PlanAttemptRequestRootStartRotation.y),
                Number(leg.PlanAttemptRequestRootStartRotation.z), Number(leg.PlanAttemptRequestRootStartRotation.w),
                Number(leg.PlanAttemptRequestPresentedBodyStartPosition.x), Number(leg.PlanAttemptRequestPresentedBodyStartPosition.y), Number(leg.PlanAttemptRequestPresentedBodyStartPosition.z),
                Number(leg.PlanAttemptRequestCommittedBodyVelocity.x), Number(leg.PlanAttemptRequestCommittedBodyVelocity.y), Number(leg.PlanAttemptRequestCommittedBodyVelocity.z),
                Number(leg.PlanAttemptRequestTrajectoryCurvatureDegreesPerSecond), Bool(leg.PlanAttemptRequestTrajectoryCurvatureAvailable),
                Number(leg.PlanAttemptRequestMovementPlaybackTime),
                Number(leg.PlanAttemptRequestUp.x), Number(leg.PlanAttemptRequestUp.y), Number(leg.PlanAttemptRequestUp.z),
                Number(leg.PlanAttemptRequestSoleSupportRadius), Number(leg.PlanAttemptRequestLegLength),
                Bool(leg.PlanFadingOut), Number(leg.PlanRetentionWeight),
                Number(leg.IntentLandingDisplacementError), Number(leg.IntentLandingDisplacementThreshold),
                Number(leg.LandingConfidence), Number(leg.AuthoredLandingDelaySeconds), Number(leg.LandingEventPhase), Number(leg.LandingLiftOffPhase),
                Number(leg.RootLocalLanding.x), Number(leg.RootLocalLanding.y), Number(leg.RootLocalLanding.z),
                Number(leg.RootLocalRouteSample0.x), Number(leg.RootLocalRouteSample0.y), Number(leg.RootLocalRouteSample0.z),
                Number(leg.RootLocalRouteSample1.x), Number(leg.RootLocalRouteSample1.y), Number(leg.RootLocalRouteSample1.z),
                Number(leg.RootLocalRouteSample2.x), Number(leg.RootLocalRouteSample2.y), Number(leg.RootLocalRouteSample2.z),
                Number(leg.RootLocalRouteSample3.x), Number(leg.RootLocalRouteSample3.y), Number(leg.RootLocalRouteSample3.z),
                Number(leg.RootLocalRouteSample4.x), Number(leg.RootLocalRouteSample4.y), Number(leg.RootLocalRouteSample4.z),
                Number(leg.RootLocalRouteSample5.x), Number(leg.RootLocalRouteSample5.y), Number(leg.RootLocalRouteSample5.z),
                Number(leg.RootLocalRouteSample6.x), Number(leg.RootLocalRouteSample6.y), Number(leg.RootLocalRouteSample6.z),
                Number(leg.RootLocalRouteSample7.x), Number(leg.RootLocalRouteSample7.y), Number(leg.RootLocalRouteSample7.z),
                Number(leg.RootLocalRouteSample8.x), Number(leg.RootLocalRouteSample8.y), Number(leg.RootLocalRouteSample8.z),
                Number(leg.RootLocalRouteSample9.x), Number(leg.RootLocalRouteSample9.y), Number(leg.RootLocalRouteSample9.z),
                Number(leg.RootLocalRouteSample10.x), Number(leg.RootLocalRouteSample10.y), Number(leg.RootLocalRouteSample10.z),
                Number(leg.RootLocalRouteSample11.x), Number(leg.RootLocalRouteSample11.y), Number(leg.RootLocalRouteSample11.z),
                Number(leg.RootLocalRouteSample12.x), Number(leg.RootLocalRouteSample12.y), Number(leg.RootLocalRouteSample12.z),
                Number(leg.RootLocalRouteSample13.x), Number(leg.RootLocalRouteSample13.y), Number(leg.RootLocalRouteSample13.z),
                Number(leg.RootLocalRouteSample14.x), Number(leg.RootLocalRouteSample14.y), Number(leg.RootLocalRouteSample14.z),
                Number(leg.RootLocalRouteSample15.x), Number(leg.RootLocalRouteSample15.y), Number(leg.RootLocalRouteSample15.z),
                Number(leg.RootLocalRouteSample16.x), Number(leg.RootLocalRouteSample16.y), Number(leg.RootLocalRouteSample16.z),
                Number(leg.RootLocalRouteSample17.x), Number(leg.RootLocalRouteSample17.y), Number(leg.RootLocalRouteSample17.z),
                Number(leg.RootLocalRouteSample18.x), Number(leg.RootLocalRouteSample18.y), Number(leg.RootLocalRouteSample18.z),
                Number(leg.RootLocalRouteSample19.x), Number(leg.RootLocalRouteSample19.y), Number(leg.RootLocalRouteSample19.z),
                Number(leg.RootLocalRouteSample20.x), Number(leg.RootLocalRouteSample20.y), Number(leg.RootLocalRouteSample20.z),
                Number(leg.RootLocalRouteSample21.x), Number(leg.RootLocalRouteSample21.y), Number(leg.RootLocalRouteSample21.z),
                Number(leg.RootLocalRouteSample22.x), Number(leg.RootLocalRouteSample22.y), Number(leg.RootLocalRouteSample22.z),
                Number(leg.RootLocalRouteSample23.x), Number(leg.RootLocalRouteSample23.y), Number(leg.RootLocalRouteSample23.z),
                Number(leg.RootLocalRouteSample24.x), Number(leg.RootLocalRouteSample24.y), Number(leg.RootLocalRouteSample24.z),
                Number(leg.AuthoredFootRouteStart.x), Number(leg.AuthoredFootRouteStart.y), Number(leg.AuthoredFootRouteStart.z),
                Number(leg.AuthoredFootRouteLanding.x), Number(leg.AuthoredFootRouteLanding.y), Number(leg.AuthoredFootRouteLanding.z),
                Number(leg.PredictionDistance), Number(leg.PredictivePlanSequence), Number(leg.PredictivePlanGeneratedFrame), Number(leg.PredictivePlanGenerationPhase),
                Bool(leg.IncomingPredictedStepValid), Bool(leg.IncomingLandingEventIdentityValid), Number(leg.IncomingLandingEventIdentity),
                Number(leg.IncomingEventPhase), Number(leg.IncomingLiftOffPhase),
                leg.PredictivePlanState, leg.PredictivePlanTransitionReason, leg.PredictivePlanEndReason,
                Number(leg.PredictiveExecutionProgress),
                Number(leg.PlanLandingEventIdentity), Number(leg.PlanSourceSampleIdentity), Number(leg.PlanSourceSampleCycle), Number(leg.PlanEventOrdinal),
                Number(leg.PlanContributionContinuityIdentity), Number(leg.PlanElapsedSeconds), Number(leg.PlanSecondsToLiftOff), Number(leg.PlanSwingDuration),
                Bool(leg.PlanHasPathGeometry), Bool(leg.PlanHasExecutablePath),
                Number(leg.FrozenPlanarVelocity.x), Number(leg.FrozenPlanarVelocity.y), Number(leg.FrozenPlanarVelocity.z),
                Number(leg.TrajectoryCurvatureDegreesPerSecond), Bool(leg.TrajectoryCurvatureAvailable),
                Number(leg.FrozenTrajectoryCurvatureDegreesPerSecond), Bool(leg.FrozenTrajectoryCurvatureAvailable),
                Number(leg.FrozenYawVelocityDegreesPerSecond), Number(leg.FrozenMaximumYawVelocityDegreesPerSecond),
                Number(leg.MotionLinearLandingError), Number(leg.MotionAngularLandingError),
                Number(leg.MotionLandingError), Number(leg.MotionLandingTolerance),
                Number(leg.CurrentSoleWorldPosition.x), Number(leg.CurrentSoleWorldPosition.y), Number(leg.CurrentSoleWorldPosition.z),
                Number(leg.FixedPathStartWorldPosition.x), Number(leg.FixedPathStartWorldPosition.y), Number(leg.FixedPathStartWorldPosition.z),
                Number(leg.FixedLandingWorldPosition.x), Number(leg.FixedLandingWorldPosition.y), Number(leg.FixedLandingWorldPosition.z),
                Number(leg.CurrentPathWorldPosition.x), Number(leg.CurrentPathWorldPosition.y), Number(leg.CurrentPathWorldPosition.z),
                Number(leg.CurrentPathRootWorldPosition.x), Number(leg.CurrentPathRootWorldPosition.y), Number(leg.CurrentPathRootWorldPosition.z),
                Number(leg.CurrentPathHipWorldPosition.x), Number(leg.CurrentPathHipWorldPosition.y), Number(leg.CurrentPathHipWorldPosition.z),
                Number(leg.PredictedHipWorldPosition.x), Number(leg.PredictedHipWorldPosition.y), Number(leg.PredictedHipWorldPosition.z),
                Number(leg.FrozenRootStartWorldPosition.x), Number(leg.FrozenRootStartWorldPosition.y), Number(leg.FrozenRootStartWorldPosition.z),
                Number(leg.FrozenRootStartWorldRotation.x), Number(leg.FrozenRootStartWorldRotation.y), Number(leg.FrozenRootStartWorldRotation.z), Number(leg.FrozenRootStartWorldRotation.w),
                Number(leg.FrozenRootLandingWorldPosition.x), Number(leg.FrozenRootLandingWorldPosition.y), Number(leg.FrozenRootLandingWorldPosition.z),
                Number(leg.FrozenRootLandingWorldRotation.x), Number(leg.FrozenRootLandingWorldRotation.y), Number(leg.FrozenRootLandingWorldRotation.z), Number(leg.FrozenRootLandingWorldRotation.w),
                Number(leg.PredictionUp.x), Number(leg.PredictionUp.y), Number(leg.PredictionUp.z),
                Number(leg.MinimumLandingConfidence), Number(leg.MaximumPredictionReachRatio), Number(leg.PredictionReachRatio),
                Number(leg.CastAbove), Number(leg.CastBelow), Number(leg.PredictiveRouteSampleCount), Number(leg.PredictiveAcceptedHitCount), Number(leg.PredictiveEdgePlaneCandidateCount), Number(leg.PredictiveAcceptedEdgePlaneCount), Number(leg.PathSphereRadius), Number(leg.SwingCapsuleRadius), Number(leg.SoleSupportRadius),
                Number(leg.CurrentPathSurfaceIdentity),
                Number(leg.CurrentPathSupportPoint.x), Number(leg.CurrentPathSupportPoint.y), Number(leg.CurrentPathSupportPoint.z),
                Number(leg.CurrentPathSupportNormal.x), Number(leg.CurrentPathSupportNormal.y), Number(leg.CurrentPathSupportNormal.z),
                Number(leg.PreClearanceHeelPathDistance), Number(leg.PreClearanceToePathDistance),
                Number(leg.PostClearanceHeelPathDistance), Number(leg.PostClearanceToePathDistance),
                Bool(leg.PredictiveClearanceEvaluated), Number(leg.PredictiveResidualPenetration), Number(leg.PlannedFootRouteWorldSampleCount),
                Number(leg.PlannedFootRouteWorldSample0.x), Number(leg.PlannedFootRouteWorldSample0.y), Number(leg.PlannedFootRouteWorldSample0.z),
                Number(leg.PlannedFootRouteWorldSample1.x), Number(leg.PlannedFootRouteWorldSample1.y), Number(leg.PlannedFootRouteWorldSample1.z),
                Number(leg.PlannedFootRouteWorldSample2.x), Number(leg.PlannedFootRouteWorldSample2.y), Number(leg.PlannedFootRouteWorldSample2.z),
                Number(leg.PlannedFootRouteWorldSample3.x), Number(leg.PlannedFootRouteWorldSample3.y), Number(leg.PlannedFootRouteWorldSample3.z),
                Number(leg.PlannedFootRouteWorldSample4.x), Number(leg.PlannedFootRouteWorldSample4.y), Number(leg.PlannedFootRouteWorldSample4.z),
                Number(leg.PlannedFootRouteWorldSample5.x), Number(leg.PlannedFootRouteWorldSample5.y), Number(leg.PlannedFootRouteWorldSample5.z),
                Number(leg.PlannedFootRouteWorldSample6.x), Number(leg.PlannedFootRouteWorldSample6.y), Number(leg.PlannedFootRouteWorldSample6.z),
                Number(leg.PredictivePathDiagnosticSampleCount)
            });
            for (int i = 0; i < 8; i++)
                AppendFootIkPathSampleValues(values, GetFootIkPathSample(leg, i));
            values.AddRange(new[]
            {
                Number(leg.AuthoredAnimationClearance), Number(leg.AnimationClearanceContinuityOffset),
                Number(leg.AnimationClearanceContinuityContribution), Number(leg.ReachClearance),
                Number(leg.CompositeAnimationClearance),
                Number(leg.RequiredLift), Number(leg.AppliedLift),
                Number(leg.BaselineGoalWorldPosition.x), Number(leg.BaselineGoalWorldPosition.y), Number(leg.BaselineGoalWorldPosition.z),
                Number(leg.FinalGoalWorldPosition.x), Number(leg.FinalGoalWorldPosition.y), Number(leg.FinalGoalWorldPosition.z),
                leg.BaselineGoalApplication, leg.FinalGoalSourceKind, Bool(leg.SolverResultAvailable),
                Number(leg.PlantConfidence), Bool(leg.PlantContact), Number(leg.SoleHeight), Number(leg.PlacementWeight), Number(leg.AnimationFootSpeed), Number(leg.SurfaceDistance),
                Number(leg.SoleSupportSurfaceIdentity),
                Number(leg.SoleSupportPoint.x), Number(leg.SoleSupportPoint.y), Number(leg.SoleSupportPoint.z),
                Number(leg.SoleSupportNormal.x), Number(leg.SoleSupportNormal.y), Number(leg.SoleSupportNormal.z),
                Number(leg.SoleClearanceTarget),
                FootSolePoseSequence(leg.BaselineGoalWorldPosition, leg.SoleHeelPosition, leg.SoleToePosition),
                FootSolePoseSequence(leg.FinalGoalWorldPosition, leg.FinalGoalSoleHeelPosition, leg.FinalGoalSoleToePosition),
                FootSolePoseSequence(leg.SolvedSoleAnklePosition, leg.SolvedSoleHeelPosition, leg.SolvedSoleToePosition),
                leg.FinalPhysicalSupportKind, Number(leg.FinalPhysicalSupportSurfaceIdentity),
                Number(leg.FinalPhysicalSupportPoint.x), Number(leg.FinalPhysicalSupportPoint.y), Number(leg.FinalPhysicalSupportPoint.z),
                Number(leg.FinalPhysicalSupportNormal.x), Number(leg.FinalPhysicalSupportNormal.y), Number(leg.FinalPhysicalSupportNormal.z),
                Number(leg.FinalPhysicalHeelPlaneDistance), Number(leg.FinalPhysicalToePlaneDistance), Number(leg.FinalPhysicalResidualPenetration), Bool(leg.FinalPhysicalEvaluationAvailable),
                Number(leg.AnimatedAnkleComponentY),
                Number(leg.BaselineGoalPositionWeight), Number(leg.BaselineGoalRotationWeight), Number(leg.FinalGoalPositionWeight), Number(leg.FinalGoalRotationWeight),
                Number(leg.TargetOffset), Number(leg.OffsetTarget), Number(leg.CurrentOffset), Number(leg.OffsetSpringVelocity), Number(leg.PreviousOffsetTarget), Bool(leg.OffsetSpringInitialized),
                Number(leg.TargetNormal.x), Number(leg.TargetNormal.y), Number(leg.TargetNormal.z),
                Number(leg.CurrentNormal.x), Number(leg.CurrentNormal.y), Number(leg.CurrentNormal.z),
                Number(leg.NormalSpringVelocity.x), Number(leg.NormalSpringVelocity.y), Number(leg.NormalSpringVelocity.z),
                Number(leg.PreviousNormalTarget.x), Number(leg.PreviousNormalTarget.y), Number(leg.PreviousNormalTarget.z), Bool(leg.NormalSpringInitialized),
                Number(leg.CurrentGroundingComponentPosition.x), Number(leg.CurrentGroundingComponentPosition.y), Number(leg.CurrentGroundingComponentPosition.z),
                Number(leg.BaselineGoalComponentPosition.x), Number(leg.BaselineGoalComponentPosition.y), Number(leg.BaselineGoalComponentPosition.z),
                Number(leg.FinalGoalComponentPosition.x), Number(leg.FinalGoalComponentPosition.y), Number(leg.FinalGoalComponentPosition.z),
                Number(leg.SolvedComponentPosition.x), Number(leg.SolvedComponentPosition.y), Number(leg.SolvedComponentPosition.z),
                Number(leg.PositionResidual), Number(leg.RotationResidualDegrees)
            });
        }

        static RuntimeFootIkPathSampleSnapshot GetFootIkPathSample(
            RuntimeFootIkLegTraceSnapshot leg,
            int index)
        {
            switch (index)
            {
                case 0: return leg.PredictivePathSample0;
                case 1: return leg.PredictivePathSample1;
                case 2: return leg.PredictivePathSample2;
                case 3: return leg.PredictivePathSample3;
                case 4: return leg.PredictivePathSample4;
                case 5: return leg.PredictivePathSample5;
                case 6: return leg.PredictivePathSample6;
                case 7: return leg.PredictivePathSample7;
                default: return default;
            }
        }

        static void AppendFootIkPathSampleValues(
            List<string> values,
            RuntimeFootIkPathSampleSnapshot sample)
        {
            values.Add(Number(sample.Fraction));
            values.Add(Number(sample.Position.x));
            values.Add(Number(sample.Position.y));
            values.Add(Number(sample.Position.z));
            values.Add(Number(sample.Normal.x));
            values.Add(Number(sample.Normal.y));
            values.Add(Number(sample.Normal.z));
            values.Add(Number(sample.SurfaceIdentity));
            values.Add(Number(sample.AnimationRootPosition.x));
            values.Add(Number(sample.AnimationRootPosition.y));
            values.Add(Number(sample.AnimationRootPosition.z));
            values.Add(Number(sample.HipPosition.x));
            values.Add(Number(sample.HipPosition.y));
            values.Add(Number(sample.HipPosition.z));
        }

        internal static void AppendCsvRow(StringBuilder builder, params string[] values)
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

        internal static string Number<T>(T value) where T : IFormattable =>
            value.ToString(null, CultureInfo.InvariantCulture);

        internal static string Bool(bool value) => value ? "true" : "false";

        static string FootSolePoseSequence(Vector3 ankle, Vector3 heel, Vector3 toe) =>
            string.Join(";", new[]
            {
                Number(ankle.x), Number(ankle.y), Number(ankle.z),
                Number(heel.x), Number(heel.y), Number(heel.z),
                Number(toe.x), Number(toe.y), Number(toe.z)
            });

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
