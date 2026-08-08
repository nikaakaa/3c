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
                   $"solver {snapshot.SolverFailure} | alpha/body {snapshot.PlacementAlpha:0.###}/{snapshot.BodyGrounded} | modifier {snapshot.ModifierSelectedSide} | " +
                   $"L hit {left.DidCurrentTraceHit} offset {left.TargetOffset:0.###}+{left.SoleClearanceTarget:0.###}->{left.UnconstrainedOffset:0.###}+constraint {left.SoleConstraintOffset:0.###}={left.CurrentOffset:0.###} continuous {left.ContinuousSoleContact} contact {left.ContactState} anchor {left.AnchorBlendWeight:0.###} soleResidual {left.ResidualSolePenetration:0.###} rewrite {left.SelectedForPredictiveRewrite}/{left.PredictiveRewritten}/{left.PredictionRejectReason} residual {left.PositionResidual:0.###} | " +
                   $"R hit {right.DidCurrentTraceHit} offset {right.TargetOffset:0.###}+{right.SoleClearanceTarget:0.###}->{right.UnconstrainedOffset:0.###}+constraint {right.SoleConstraintOffset:0.###}={right.CurrentOffset:0.###} continuous {right.ContinuousSoleContact} contact {right.ContactState} anchor {right.AnchorBlendWeight:0.###} soleResidual {right.ResidualSolePenetration:0.###} rewrite {right.SelectedForPredictiveRewrite}/{right.PredictiveRewritten}/{right.PredictionRejectReason} residual {right.PositionResidual:0.###} | " +
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
                           $"L {footIk.Left.ContactState} offset {footIk.Left.TargetOffset:0.###}+{footIk.Left.SoleClearanceTarget:0.###}->{footIk.Left.UnconstrainedOffset:0.###}+constraint {footIk.Left.SoleConstraintOffset:0.###}={footIk.Left.CurrentOffset:0.###} continuous {footIk.Left.ContinuousSoleContact} anchor {footIk.Left.AnchorBlendWeight:0.###} soleResidual {footIk.Left.ResidualSolePenetration:0.###} rewrite {footIk.Left.PredictiveRewritten} residual {footIk.Left.PositionResidual:0.###} | " +
                           $"R {footIk.Right.ContactState} offset {footIk.Right.TargetOffset:0.###}+{footIk.Right.SoleClearanceTarget:0.###}->{footIk.Right.UnconstrainedOffset:0.###}+constraint {footIk.Right.SoleConstraintOffset:0.###}={footIk.Right.CurrentOffset:0.###} continuous {footIk.Right.ContinuousSoleContact} anchor {footIk.Right.AnchorBlendWeight:0.###} soleResidual {footIk.Right.ResidualSolePenetration:0.###} rewrite {footIk.Right.PredictiveRewritten} residual {footIk.Right.PositionResidual:0.###} | " +
                           $"pelvis {footIk.PelvisLyraTargetOffset:0.###}->{footIk.PelvisResolvedTargetOffset:0.###}->{footIk.CurrentPelvisOffset:0.###} | modifier {footIk.ModifierSelectedSide}";
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
            var values = new List<string>
            {
                "presentation_position", "trace_sequence", "frame_sequence", "reset_sequence",
                "grounding_completion", "modifier_completion", "solver_completion", "has_modifier",
                "solver_backend", "solver_failure", "node_executed", "body_grounded", "placement_alpha", "presentation_delta_seconds", "pose_root_vertical_delta",
                "lyra_source_identity", "spring_identity", "rig_id", "rig_revision", "profile_id", "profile_revision",
                "pose_plan_hash", "calibration_id", "calibration_revision", "physics_scene_identity", "self_filter_identity",
                "pelvis_lyra_target", "pelvis_resolved_target", "pelvis_current", "pelvis_spring_velocity",
                "pelvis_previous_target", "pelvis_spring_initialized", "pelvis_translation_x", "pelvis_translation_y",
                "pelvis_translation_z", "pelvis_goal_weight", "pelvis_goal_application", "pelvis_goal_source",
                "modifier_selected_side", "baseline_producer_operation", "baseline_producer_call_site",
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
                snapshot.LyraSourceIdentity, snapshot.SpringIdentity, snapshot.RigId, snapshot.RigRevision, snapshot.ProfileId, snapshot.ProfileRevision,
                snapshot.PosePlanHash, snapshot.CalibrationId, snapshot.CalibrationRevision, Number(snapshot.PhysicsSceneIdentity), Number(snapshot.SelfFilterIdentity),
                Number(snapshot.PelvisLyraTargetOffset), Number(snapshot.PelvisResolvedTargetOffset), Number(snapshot.CurrentPelvisOffset), Number(snapshot.PelvisSpringVelocity),
                Number(snapshot.PreviousPelvisTarget), Bool(snapshot.PelvisSpringInitialized), Number(snapshot.PelvisPreSolveTranslation.x), Number(snapshot.PelvisPreSolveTranslation.y),
                Number(snapshot.PelvisPreSolveTranslation.z), Number(snapshot.PelvisGoalPositionWeight), snapshot.PelvisGoalApplication, snapshot.PelvisGoalSourceKind,
                snapshot.ModifierSelectedSide, Number(snapshot.BaselineProducerOperationIndex), Number(snapshot.BaselineProducerCallSiteIndex),
                Number(snapshot.BaselineGoalOffset), Number(snapshot.BaselineGoalCount), snapshot.BaselineRigId, snapshot.BaselineRigRevision
            };
            AppendFootIkLegValues(values, left);
            AppendFootIkLegValues(values, right);
            AppendCsvRow(builder, values.ToArray());
        }

        static void AppendFootIkLegHeader(List<string> values, string prefix)
        {
            string[] names =
            {
                "hit", "surface", "query_shape", "query_purpose", "query_foot_index",
                "query_origin_x", "query_origin_y", "query_origin_z", "query_capsule_end_x", "query_capsule_end_y", "query_capsule_end_z",
                "query_direction_x", "query_direction_y", "query_direction_z", "query_radius", "query_maximum_distance", "query_layer_mask", "query_minimum_ground_normal_dot",
                "hit_location_x", "hit_location_y", "hit_location_z", "impact_point_x", "impact_point_y", "impact_point_z",
                "hit_normal_x", "hit_normal_y", "hit_normal_z", "hit_distance",
                "contact", "transition", "has_anchor", "anchor_local_x", "anchor_local_y", "anchor_local_z",
                "anchor_local_rotation_x", "anchor_local_rotation_y", "anchor_local_rotation_z", "anchor_local_rotation_w",
                "anchor_world_x", "anchor_world_y", "anchor_world_z", "anchor_world_rotation_x", "anchor_world_rotation_y", "anchor_world_rotation_z", "anchor_world_rotation_w", "anchor_blend",
                "swing_eligible", "selected_for_rewrite", "rewritten", "prediction_reject", "future_surface",
                "future_point_x", "future_point_y", "future_point_z", "future_normal_x", "future_normal_y", "future_normal_z",
                "ground_envelope_count", "ground_envelope_reject", "predictive_query_count", "predictive_rejected_query_count", "prediction_horizon", "swing_clearance",
                "baseline_application", "final_source", "solver_result_available", "plant_confidence", "plant_contact", "sole_height",
                "placement_weight", "animation_foot_speed", "surface_distance",
                "sole_support_surface", "sole_support_point_x", "sole_support_point_y", "sole_support_point_z",
                "sole_support_normal_x", "sole_support_normal_y", "sole_support_normal_z",
                "sole_clearance_target", "sole_clearance_target_x", "sole_clearance_target_y", "sole_clearance_target_z",
                "sole_ankle_x", "sole_ankle_y", "sole_ankle_z",
                "sole_heel_x", "sole_heel_y", "sole_heel_z",
                "sole_toe_x", "sole_toe_y", "sole_toe_z",
                "sole_heel_plane_distance", "sole_toe_plane_distance", "residual_sole_penetration",
                "animated_ankle_component_y", "has_previous_sole_sample", "previous_sole_surface", "previous_sole_heel_plane_distance", "previous_sole_toe_plane_distance", "continuous_sole_contact",
                "baseline_position_weight", "baseline_rotation_weight",
                "final_position_weight", "final_rotation_weight", "target_offset", "offset_target", "unconstrained_offset", "sole_constraint_offset", "current_offset", "offset_spring_velocity", "previous_offset_target", "offset_spring_initialized",
                "target_normal_x", "target_normal_y", "target_normal_z", "current_normal_x", "current_normal_y", "current_normal_z",
                "normal_spring_velocity_x", "normal_spring_velocity_y", "normal_spring_velocity_z",
                "previous_normal_target_x", "previous_normal_target_y", "previous_normal_target_z", "normal_spring_initialized",
                "current_grounding_x", "current_grounding_y", "current_grounding_z", "baseline_x", "baseline_y", "baseline_z", "final_x", "final_y", "final_z",
                "solved_x", "solved_y", "solved_z", "position_residual", "rotation_residual_degrees"
            };
            for (int i = 0; i < names.Length; i++)
                values.Add($"{prefix}_{names[i]}");
        }

        static void AppendFootIkLegValues(List<string> values, RuntimeFootIkLegTraceSnapshot leg)
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
                leg.ContactState, leg.TransitionReason, Bool(leg.HasSurfaceAnchor),
                Number(leg.SurfaceLocalAnchor.x), Number(leg.SurfaceLocalAnchor.y), Number(leg.SurfaceLocalAnchor.z),
                Number(leg.SurfaceLocalRotation.x), Number(leg.SurfaceLocalRotation.y), Number(leg.SurfaceLocalRotation.z), Number(leg.SurfaceLocalRotation.w),
                Number(leg.AnchorWorldPosition.x), Number(leg.AnchorWorldPosition.y), Number(leg.AnchorWorldPosition.z),
                Number(leg.AnchorWorldRotation.x), Number(leg.AnchorWorldRotation.y), Number(leg.AnchorWorldRotation.z), Number(leg.AnchorWorldRotation.w), Number(leg.AnchorBlendWeight),
                Bool(leg.SwingEligible), Bool(leg.SelectedForPredictiveRewrite), Bool(leg.PredictiveRewritten), leg.PredictionRejectReason, Number(leg.FutureSurfaceIdentity),
                Number(leg.FutureSupportPoint.x), Number(leg.FutureSupportPoint.y), Number(leg.FutureSupportPoint.z),
                Number(leg.FutureSupportNormal.x), Number(leg.FutureSupportNormal.y), Number(leg.FutureSupportNormal.z),
                Number(leg.GroundEnvelopeSegmentCount), leg.GroundEnvelopeRejectReason, Number(leg.PredictiveQueryCount), Number(leg.PredictiveRejectedQueryCount),
                Number(leg.PredictionHorizon), Number(leg.SwingClearance), leg.BaselineGoalApplication, leg.FinalGoalSourceKind, Bool(leg.SolverResultAvailable),
                Number(leg.PlantConfidence), Bool(leg.PlantContact), Number(leg.SoleHeight), Number(leg.PlacementWeight), Number(leg.AnimationFootSpeed), Number(leg.SurfaceDistance),
                Number(leg.SoleSupportSurfaceIdentity),
                Number(leg.SoleSupportPoint.x), Number(leg.SoleSupportPoint.y), Number(leg.SoleSupportPoint.z),
                Number(leg.SoleSupportNormal.x), Number(leg.SoleSupportNormal.y), Number(leg.SoleSupportNormal.z),
                Number(leg.SoleClearanceTarget),
                Number(leg.SoleClearanceTargetTranslation.x), Number(leg.SoleClearanceTargetTranslation.y), Number(leg.SoleClearanceTargetTranslation.z),
                Number(leg.SoleAnklePosition.x), Number(leg.SoleAnklePosition.y), Number(leg.SoleAnklePosition.z),
                Number(leg.SoleHeelPosition.x), Number(leg.SoleHeelPosition.y), Number(leg.SoleHeelPosition.z),
                Number(leg.SoleToePosition.x), Number(leg.SoleToePosition.y), Number(leg.SoleToePosition.z),
                Number(leg.SoleHeelPlaneDistance), Number(leg.SoleToePlaneDistance), Number(leg.ResidualSolePenetration),
                Number(leg.AnimatedAnkleComponentY), Bool(leg.HasPreviousSoleSample), Number(leg.PreviousSoleSurfaceIdentity), Number(leg.PreviousSoleHeelPlaneDistance), Number(leg.PreviousSoleToePlaneDistance), Bool(leg.ContinuousSoleContact),
                Number(leg.BaselineGoalPositionWeight), Number(leg.BaselineGoalRotationWeight), Number(leg.FinalGoalPositionWeight), Number(leg.FinalGoalRotationWeight),
                Number(leg.TargetOffset), Number(leg.OffsetTarget), Number(leg.UnconstrainedOffset), Number(leg.SoleConstraintOffset), Number(leg.CurrentOffset), Number(leg.OffsetSpringVelocity), Number(leg.PreviousOffsetTarget), Bool(leg.OffsetSpringInitialized),
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
