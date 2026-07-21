using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterPipelineHost))]
    public sealed class CharacterPipelineHostEditor : UnityEditor.Editor
    {
        static RuntimeDiagnosticsCaptureDetail s_CaptureDetail = RuntimeDiagnosticsCaptureDetail.Evaluation;

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

            DrawFootPlacementConfiguration(host);
            DrawEquipmentConfiguration(host);
            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            RuntimeDebugViewModel view = session.ViewModel;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Runtime Diagnostics", EditorStyles.boldLabel);
            if (!view.Attached || view.Target.HostInstanceId != host.GetInstanceID())
            {
                session.ReleaseLiveInterest(this);
                if (GUILayout.Button("Attach Debug Session"))
                    session.AttachToHost(host.GetInstanceID());
                EditorGUILayout.LabelField("State", session.AttachmentState.ToString());
                if (view.Attached)
                    EditorGUILayout.LabelField("Current Target", view.Target.DisplayName);
                return;
            }

            if (session.CanControlLiveTarget)
                session.EnsureLiveInterest(this, RuntimeTraceChannel.All);

            DrawSessionControls(session, view);
            if (session.AttachmentState == RuntimeDebugAttachmentState.Ended)
                EditorGUILayout.HelpBox("Target ended. The inspector is showing its final live state or the active capture.", MessageType.Info);
            if (!view.Valid)
            {
                EditorGUILayout.HelpBox(!string.IsNullOrEmpty(view.Error) ? view.Error : "Runtime diagnostics are unavailable.", MessageType.Error);
                return;
            }

            DrawSimulation(view);
            DrawNetwork(view);
            DrawGraphLifecycle(view);
            DrawStateMachine(view);
            DrawAction(view);
            DrawEquipment(view);
            DrawBlackboard(view);
            DrawMotion(view);
            DrawCamera(view);
            DrawPresentation(view);
            DrawFootPlacement(view);
        }

        static void DrawFootPlacementConfiguration(CharacterPipelineHost host)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Foot Placement", EditorStyles.boldLabel);
            CharacterFootPlacementComposition composition = host.FootPlacement;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Composition", composition, typeof(CharacterFootPlacementComposition), true);
                EditorGUILayout.ObjectField("Profile", composition ? composition.Profile : null, typeof(CharacterFootPlacementProfile), false);
                EditorGUILayout.ObjectField("Rig", composition ? composition.Rig : null, typeof(CharacterFootPlacementRig), true);
                EditorGUILayout.ObjectField("Solver", composition ? composition.SolverAdapter : null, typeof(MonoBehaviour), true);
            }
            if (!composition)
            {
                EditorGUILayout.HelpBox("Character host requires an explicit Foot Placement Composition.", MessageType.Error);
                return;
            }
            try
            {
                CharacterFootPlacementRigBinding rig = composition.Rig
                    ? composition.Rig.BuildBinding()
                    : throw new InvalidOperationException("Foot Placement Composition requires a Rig.");
                CharacterFootPlacementProfile profile = composition.Profile
                    ? composition.Profile
                    : throw new InvalidOperationException("Foot Placement Composition requires a Profile.");
                profile.RequireConfiguration(rig);
                ICharacterFootPlacementSolver solver = composition.RequireSolver(host.VisualRoot);
                solver.RequireValid(rig);
                EditorGUILayout.HelpBox("Foot Placement bindings are valid.", MessageType.Info);
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

        static void DrawSessionControls(RuntimeDebugSession session, RuntimeDebugViewModel view)
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
            else
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

        static void DrawPresentation(RuntimeDebugViewModel view)
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
                "Playback Lifecycle",
                events,
                RuntimeTraceEventKind.AnimationPlaybackPending,
                RuntimeTraceEventKind.AnimationPlaybackCurrent,
                RuntimeTraceEventKind.AnimationPlaybackOutgoing,
                RuntimeTraceEventKind.AnimationPlaybackRetired,
                RuntimeTraceEventKind.AnimationPlaybackCompleted,
                RuntimeTraceEventKind.AnimationPlaybackReleased);
            DrawAnimationGroup("Animancer Fade", events, RuntimeTraceEventKind.AnimationFade);
            DrawAnimationGroup("Presentation", events, RuntimeTraceEventKind.PresentationInterpolated);
        }

        static void DrawFootPlacement(RuntimeDebugViewModel view)
        {
            DrawEventSection(
                "Foot Placement",
                Filter(
                    view,
                    RuntimeTraceChannel.FootPlacement,
                    RuntimeTraceEventKind.FootPlacementSnapshot),
                eventView =>
                {
                    RuntimeTracePayload payload = eventView.Event.Payload;
                    return $"{payload.Name} | {payload.Status} | layer {payload.LayerId} | left {payload.Weight:0.###} | right {payload.FinalWeight:0.###} | pelvis {payload.SecondaryTime:0.###} | {payload.Detail}";
                });
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawFootPlacementGizmo(CharacterPipelineHost host, GizmoType gizmoType)
        {
            if (!Application.isPlaying || host?.Registration?.PresentationRuntime == null)
                return;
            CharacterFootPlacementFrameSnapshot snapshot =
                host.Registration.PresentationRuntime.CaptureDiagnostics().FootPlacement;
            if (!snapshot.IsValid)
                return;
            DrawFoot(snapshot.Left, new Color(0.2f, 0.75f, 1f));
            DrawFoot(snapshot.Right, new Color(1f, 0.35f, 0.65f));
        }

        static void DrawFoot(ThirdPersonCharacter.Pipeline.Presentation.FootPlacementFootFrameSnapshot foot, Color color)
        {
            Handles.color = color;
            Handles.DrawWireDisc(foot.PredictedFootprint, Vector3.up, 0.045f);
            Handles.DrawLine(foot.PredictedFootprint, foot.TargetPosition);
            Handles.SphereHandleCap(0, foot.TargetPosition, Quaternion.identity, 0.055f, EventType.Repaint);
        }

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
            return $"{payload.Status} layer {payload.LayerId} playback {payload.OwnerId} time {payload.Time:0.###} weight {payload.Weight:0.###} fade {payload.NormalizedTime:0.###} {payload.Detail}";
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
}
