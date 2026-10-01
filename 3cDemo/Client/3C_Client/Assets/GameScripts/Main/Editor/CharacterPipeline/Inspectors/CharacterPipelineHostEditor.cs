using System;
using System.Collections.Generic;
using System.IO;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline.Editor;
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

    sealed class CharacterRuntimeDiagnosticsInspector
    {
        RuntimeDiagnosticsCaptureDetail m_CaptureDetail =
            RuntimeDiagnosticsCaptureDetail.Evaluation;

        sealed class ChannelEvents
        {
            internal RuntimeDebugViewModel View;
            internal long Revision = -1;
            internal readonly List<RuntimeDebugEventView> Values = new();
        }

        readonly Dictionary<RuntimeTraceChannel, ChannelEvents> m_Channels = new()
        {
            { RuntimeTraceChannel.Graph, new ChannelEvents() },
            { RuntimeTraceChannel.StateMachine, new ChannelEvents() },
            { RuntimeTraceChannel.Network, new ChannelEvents() },
            { RuntimeTraceChannel.Blackboard, new ChannelEvents() },
            { RuntimeTraceChannel.Equipment, new ChannelEvents() },
            { RuntimeTraceChannel.Motion, new ChannelEvents() },
            { RuntimeTraceChannel.Animation, new ChannelEvents() },
            { RuntimeTraceChannel.FootPlacement, new ChannelEvents() }
        };
        readonly List<RuntimeDebugEventView> m_SectionEvents = new();
        static readonly Comparison<RuntimeDebugEventView> s_NewestFirst =
            (left, right) => right.Event.Sequence.CompareTo(left.Event.Sequence);
        static readonly RuntimeTraceEventKind[] s_SimulationTickKinds = { RuntimeTraceEventKind.SimulationTick, RuntimeTraceEventKind.SimulationRestore, RuntimeTraceEventKind.SimulationEvaluate, RuntimeTraceEventKind.SimulationFinalize, RuntimeTraceEventKind.SimulationStatePublished, RuntimeTraceEventKind.SimulationCommit, RuntimeTraceEventKind.SimulationFailure };
        static readonly RuntimeTraceEventKind[] s_SimulationNetworkModelKinds = { RuntimeTraceEventKind.SimulationNetworkModel };
        static readonly RuntimeTraceEventKind[] s_NodeEnteredKinds = { RuntimeTraceEventKind.NodeEntered, RuntimeTraceEventKind.NodeStatus, RuntimeTraceEventKind.NodeCompleted, RuntimeTraceEventKind.NodeStopRequested, RuntimeTraceEventKind.NodeStopping, RuntimeTraceEventKind.NodeStopped, RuntimeTraceEventKind.NodeForceStopped, RuntimeTraceEventKind.EdgeEvaluated, RuntimeTraceEventKind.EdgeSelected, RuntimeTraceEventKind.GraphCreated, RuntimeTraceEventKind.GraphDestroyed, RuntimeTraceEventKind.NodeRunning, RuntimeTraceEventKind.NodeWaiting, RuntimeTraceEventKind.TraceSamplingLimited };
        static readonly RuntimeTraceEventKind[] s_StateTransitionEvaluatedKinds = { RuntimeTraceEventKind.StateTransitionEvaluated, RuntimeTraceEventKind.StateTransitionSelected, RuntimeTraceEventKind.StateScopeEntered, RuntimeTraceEventKind.StateScopeExited, RuntimeTraceEventKind.StateExitStarted, RuntimeTraceEventKind.StateExitWaiting };
        static readonly RuntimeTraceEventKind[] s_ActionSnapshotKinds = { RuntimeTraceEventKind.ActionSnapshot, RuntimeTraceEventKind.ActionActivationRequested, RuntimeTraceEventKind.ActionLifecycleTransitioned, RuntimeTraceEventKind.ActionWindowSampled, RuntimeTraceEventKind.ActionResultSubmitted };
        static readonly RuntimeTraceEventKind[] s_CameraSnapshotKinds = { RuntimeTraceEventKind.CameraSnapshot, RuntimeTraceEventKind.CameraRequest, RuntimeTraceEventKind.CameraShakeRequest };
        static readonly RuntimeTraceEventKind[] s_FootPlacementSnapshotKinds = { RuntimeTraceEventKind.FootPlacementSnapshot };
        static readonly RuntimeTraceEventKind[] s_SelectionKinds = { RuntimeTraceEventKind.AnimationSelectionSubmitted };
        static readonly RuntimeTraceEventKind[] s_TimelineSamplesKinds = { RuntimeTraceEventKind.AnimationProducerSampled, RuntimeTraceEventKind.TimelineVisualTime };
        static readonly RuntimeTraceEventKind[] s_MotionMatchingKinds = { RuntimeTraceEventKind.MotionMatchingQuery, RuntimeTraceEventKind.MotionMatchingTrajectory, RuntimeTraceEventKind.MotionMatchingPoseHistory, RuntimeTraceEventKind.MotionMatchingAdmission, RuntimeTraceEventKind.MotionMatchingCandidateRejected, RuntimeTraceEventKind.MotionMatchingSearchTraversal, RuntimeTraceEventKind.MotionMatchingTopK, RuntimeTraceEventKind.MotionMatchingPlan, RuntimeTraceEventKind.MotionMatchingSelection, RuntimeTraceEventKind.MotionMatchingPoseSource, RuntimeTraceEventKind.MotionMatchingReset, RuntimeTraceEventKind.MotionMatchingFrame };
        static readonly RuntimeTraceEventKind[] s_PlaybackLifecycleKinds = { RuntimeTraceEventKind.AnimationPlaybackPending, RuntimeTraceEventKind.AnimationPlaybackSelected, RuntimeTraceEventKind.AnimationPlaybackRetained, RuntimeTraceEventKind.AnimationPlaybackRetired, RuntimeTraceEventKind.AnimationPlaybackCompleted, RuntimeTraceEventKind.AnimationPlaybackReleased };
        static readonly RuntimeTraceEventKind[] s_PresentationKinds = { RuntimeTraceEventKind.PresentationInterpolated };

        IReadOnlyList<RuntimeDebugEventView> Events(RuntimeDebugViewModel view, RuntimeTraceChannel channel)
        {
            ChannelEvents cached = m_Channels[channel];
            if (!ReferenceEquals(cached.View, view) || cached.Revision != view.Revision)
            {
                view.CopyCurrentEvents(channel, cached.Values);
                cached.Values.Sort(s_NewestFirst);
                cached.View = view;
                cached.Revision = view.Revision;
            }
            return cached.Values;
        }

        internal void Clear()
        {
            foreach (ChannelEvents cached in m_Channels.Values)
            {
                cached.View = null;
                cached.Revision = -1;
                cached.Values.Clear();
            }
            m_SectionEvents.Clear();
        }

        internal void DrawRuntimeDiagnostics(
            object interestOwner,
            int hostInstanceId,
            CharacterPipelineDefinition definition,
            CharacterRuntimeDiagnosticsInspectorMode mode)
        {
            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            RuntimeDebugViewModel view = session.ViewModel;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Runtime Diagnostics", EditorStyles.boldLabel);
            if (GUILayout.Button("选择技能图／执行实例"))
                BtsmtlSkillHostEntry.ShowHostMenu(hostInstanceId);
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
                ? RuntimeTraceChannel.FootPlacement | RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine
                : RuntimeTraceChannel.All & ~RuntimeTraceChannel.Values;
            if (session.CanControlLiveTarget)
                session.EnsureLiveInterest(interestOwner, liveChannels);

            DrawSessionControls(
                session,
                view,
                mode == CharacterRuntimeDiagnosticsInspectorMode.Complete);
            if (session.AttachmentState == RuntimeDebugAttachmentState.Ended)
                EditorGUILayout.HelpBox("Target ended. The inspector is showing its final live state or the active capture.", MessageType.Info);
            DrawFootPlacement(view);
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

        void DrawSimulation(RuntimeDebugViewModel view)
        {
            DrawEventSection("Simulation Session", Filter(view, RuntimeTraceChannel.Graph, s_SimulationTickKinds), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                return $"{payload.Name} | {payload.Status} | actor {payload.OwnerId} | {payload.Detail}";
            });
        }

        void DrawNetwork(RuntimeDebugViewModel view)
        {
            DrawEventSection("Network Model", Filter(view, RuntimeTraceChannel.Network, s_SimulationNetworkModelKinds), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                return $"{payload.Cause} | {payload.Name} | {payload.Status} | actor {payload.OwnerId} | {payload.RelatedElementId} | input {payload.Value.DisplayValue()} | queue {payload.Priority} | replay {payload.Cycle} | {payload.Detail}";
            });
        }

        void DrawGraphLifecycle(RuntimeDebugViewModel view)
        {
            DrawEventSection("Graph Lifecycle", Filter(view, RuntimeTraceChannel.Graph, s_NodeEnteredKinds), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                return $"{eventView.Event.Kind} | {payload.Status} | {payload.Cause} | parent/source {payload.OwnerId} | target/path {payload.RelatedElementId} | {payload.Detail}";
            });
        }

        void DrawStateMachine(RuntimeDebugViewModel view)
        {
            DrawEventSection("State Machine", Filter(view, RuntimeTraceChannel.StateMachine, s_StateTransitionEvaluatedKinds), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                return $"{eventView.Event.Kind} | {payload.Status} | {payload.Cause} | state {payload.OwnerId} | target {payload.RelatedElementId} | {payload.Detail}";
            });
        }

        void DrawSessionControls(
            RuntimeDebugSession session,
            RuntimeDebugViewModel view,
            bool showGeneralCaptureControls)
        {
            EditorGUILayout.LabelField("State", session.AttachmentState.ToString());
            EditorGUILayout.LabelField("Target", view.Target.DisplayName);
            EditorGUILayout.LabelField("Character", view.Target.CharacterRuntimeId.ToString("D"));
            EditorGUILayout.LabelField("Session", view.Target.SessionId.ToString("D"));
            EditorGUILayout.LabelField("Runtime", view.Target.Revision.RuntimeId);
            EditorGUILayout.LabelField("Source Revision", view.Target.Revision.SourceRevision);
            EditorGUILayout.LabelField("Content Hash", view.Target.Revision.ContentHash);
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
                m_CaptureDetail = (RuntimeDiagnosticsCaptureDetail)EditorGUILayout.EnumPopup("Capture Detail", m_CaptureDetail);
                using (new EditorGUI.DisabledScope(!session.CanStartCapture))
                {
                    if (GUILayout.Button("Start Capture"))
                        session.BeginCapture(RuntimeTraceChannel.All, m_CaptureDetail);
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

        void DrawAction(RuntimeDebugViewModel view)
        {
            DrawEventSection("Action", Filter(view, RuntimeTraceChannel.StateMachine, s_ActionSnapshotKinds), FormatAction);
        }

        void DrawBlackboard(RuntimeDebugViewModel view)
        {
            DrawEventSection("Blackboard", Events(view, RuntimeTraceChannel.Blackboard), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                return $"{eventView.SourceName} | {eventView.Event.Kind} | {payload.Value.DisplayValue()} | {payload.Status} {payload.Cause}";
            });
        }

        void DrawEquipment(RuntimeDebugViewModel view)
        {
            DrawEventSection("Equipment", Events(view, RuntimeTraceChannel.Equipment), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                return $"{eventView.Event.Kind} | {payload.Status} | slot {payload.OwnerId} | {payload.Name} | {payload.RelatedElementId} | {payload.Cause} | {payload.Detail}";
            });
        }

        void DrawMotion(RuntimeDebugViewModel view)
        {
            DrawEventSection("Motion", Events(view, RuntimeTraceChannel.Motion), eventView =>
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

        void DrawCamera(RuntimeDebugViewModel view)
        {
            DrawEventSection("Camera", Filter(view, RuntimeTraceChannel.Animation, s_CameraSnapshotKinds), eventView =>
            {
                RuntimeTracePayload payload = eventView.Event.Payload;
                return $"{eventView.Event.Kind} | {payload.Name} | {payload.Status} | owner {payload.OwnerId} | P{payload.Priority} w{payload.Weight:0.###} | {payload.Value.DisplayValue()}";
            });
        }

        void DrawPresentation(CharacterPipelineDefinition definition, RuntimeDebugViewModel view)
        {
            IReadOnlyList<RuntimeDebugEventView> events = Events(view, RuntimeTraceChannel.Animation);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Animation Presentation", EditorStyles.boldLabel);
            DrawAnimationGroup("Selection", events, s_SelectionKinds);
            DrawAnimationGroup("Timeline Samples", events, s_TimelineSamplesKinds);
            DrawAnimationGroup("Motion Matching", events, s_MotionMatchingKinds);
            DrawAnimationGroup("Playback Lifecycle", events, s_PlaybackLifecycleKinds);
            DrawAnimationGroup("Presentation", events, s_PresentationKinds);
        }

        void DrawFootPlacement(RuntimeDebugViewModel view)
        {
            IReadOnlyList<RuntimeDebugEventView> events = Filter(view, RuntimeTraceChannel.FootPlacement, s_FootPlacementSnapshotKinds);
            DrawEventSection(
                "Foot Placement",
                events,
                eventView =>
                {
                    RuntimeTracePayload payload = eventView.Event.Payload;
                    RuntimeFootPlacementTraceSnapshot snapshot =
                        payload.FootPlacement;
                    if (!snapshot.IsAvailable)
                        return $"{payload.Name} | {payload.Status} | {payload.Detail}";
                    return $"{payload.Status} | frame {snapshot.FrameSequence} | " +
                           $"{FormatFootPlacementFoot(snapshot.Left)} | " +
                           $"{FormatFootPlacementFoot(snapshot.Right)}";
                });
        }

        static string FormatFootPlacementFoot(
            RuntimeFootPlacementFootTraceSnapshot foot)
        {
            string query = foot.QueryAvailable
                ? $"{foot.QueryShape} origin {foot.QueryOrigin} radius {foot.QueryRadius:0.###} distance {foot.QueryMaximumDistance:0.###}"
                : "query unavailable";
            string landing = foot.Accepted
                ? $"accepted surface {foot.SurfaceIdentity} point {foot.LandingPoint} normal {foot.LandingNormal}"
                : $"rejected {foot.RejectReason}";
            return $"{foot.Side} {foot.State} step {foot.StepSource}/{foot.LandingEventIdentity} " +
                   $"sole {foot.CurrentAnimatedSole} raw {foot.RawLanding} {query} {landing}";
        }

        void DrawAnimationGroup(string title, IReadOnlyList<RuntimeDebugEventView> events, IReadOnlyList<RuntimeTraceEventKind> kinds)
        {
            List<RuntimeDebugEventView> matches = m_SectionEvents;
            matches.Clear();
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

        IReadOnlyList<RuntimeDebugEventView> Filter(RuntimeDebugViewModel view, RuntimeTraceChannel channel, IReadOnlyList<RuntimeTraceEventKind> kinds)
        {
            IReadOnlyList<RuntimeDebugEventView> source = Events(view, channel);
            List<RuntimeDebugEventView> result = m_SectionEvents;
            result.Clear();
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
            if (GUILayout.Button("Open", GUILayout.Width(48f)) && !RuntimeDebugSourceNavigator.Open(eventView))
                Debug.LogError($"Runtime debug source could not be resolved by exact authoring identity: {identity}");
            EditorGUILayout.EndHorizontal();
        }
    }

    [CustomEditor(typeof(FixedCharacterHost))]
    public sealed class FixedCharacterHostEditor : UnityEditor.Editor
    {
        readonly CharacterRuntimeDiagnosticsInspector m_RuntimeDiagnostics = new();

        void OnEnable()
        {
            RuntimeDebugSession.Shared.Changed += OnRuntimeDebugChanged;
            TimelineWorkspaceModeBridge.RuntimeDebugEnabledChanged += OnRuntimeDebugEnabledChanged;
        }

        void OnDisable()
        {
            RuntimeDebugSession.Shared.Changed -= OnRuntimeDebugChanged;
            TimelineWorkspaceModeBridge.RuntimeDebugEnabledChanged -= OnRuntimeDebugEnabledChanged;
            RuntimeDebugSession.Shared.ReleaseLiveInterest(this);
            m_RuntimeDiagnostics.Clear();
        }

        void OnRuntimeDebugChanged()
        {
            if (TimelineWorkspaceModeBridge.RuntimeDebugEnabled)
                Repaint();
        }

        void OnRuntimeDebugEnabledChanged()
        {
            if (!TimelineWorkspaceModeBridge.RuntimeDebugEnabled)
            {
                RuntimeDebugSession.Shared.ReleaseLiveInterest(this);
                m_RuntimeDiagnostics.Clear();
            }
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!TimelineWorkspaceModeBridge.RuntimeDebugEnabled)
                return;
            FixedCharacterHost host = target as FixedCharacterHost;
            if (host == null)
                return;
            m_RuntimeDiagnostics.DrawRuntimeDiagnostics(
                this,
                host.GetInstanceID(),
                null,
                CharacterRuntimeDiagnosticsInspectorMode.FootPlacement);
        }
    }
}

