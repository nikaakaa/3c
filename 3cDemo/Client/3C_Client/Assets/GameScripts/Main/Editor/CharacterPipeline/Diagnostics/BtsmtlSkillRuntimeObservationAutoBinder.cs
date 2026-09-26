using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline.Editor;
using FlowCanvas;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    static class BtsmtlSkillRuntimeObservationAutoBinder
    {
        static readonly object s_InterestOwner = new object();
        static bool s_Dirty = true;
        static bool s_InterestActive;
        static bool s_Opening;
        static double s_NextResolveTime;
        static FlowGraph s_Graph;
        static RuntimeDebugTargetRequest s_Request;
        static readonly BtsmtlRuntimeFocusResolver s_Focus = new BtsmtlRuntimeFocusResolver();
        static readonly List<RuntimeInstanceKey> s_Instances = new List<RuntimeInstanceKey>();

        static BtsmtlSkillRuntimeObservationAutoBinder()
        {
            GraphEditor.onCurrentGraphChanged += OnCurrentGraphChanged;
            GraphEditor.onEditorClosed += OnEditorClosed;
            RuntimeDiagnosticsTargetRegistry.TargetRegistered += OnTargetChanged;
            RuntimeDiagnosticsTargetRegistry.TargetUnregistered += OnTargetChanged;
            RuntimeDebugSession.Shared.Changed += MarkDirty;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += Update;
        }

        static void OnCurrentGraphChanged(NodeCanvas.Framework.Graph graph)
        {
            s_NextResolveTime = 0d;
            MarkDirty();
        }

        static void OnEditorClosed()
        {
            ReleaseInterest();
            s_Graph = null;
            s_Request = default;
        }

        static void OnTargetChanged(RuntimeDiagnosticsTarget target)
        {
            s_NextResolveTime = 0d;
            MarkDirty();
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                ReleaseInterest();
            MarkDirty();
        }

        static void MarkDirty() => s_Dirty = true;

        static void Update()
        {
            if (!s_Dirty || s_Opening)
                return;
            s_Dirty = false;
            if (TimelineWorkspaceModeBridge.ActiveMode != TimelineWorkspaceMode.RuntimeDebug ||
                !Application.isPlaying || GraphEditor.current == null ||
                GraphEditor.rootGraph is not BtsmtlSkillFlowGraph graph ||
                GraphEditor.currentGraph != graph ||
                graph.Role != BtsmtlSkillFlowGraphRole.Skill)
            {
                ReleaseInterest();
                s_Graph = null;
                s_Request = default;
                return;
            }

            var authoring = (IBtsmtlSkillFlowGraph)graph;

            if (!ReferenceEquals(s_Graph, graph))
            {
                ReleaseInterest();
                s_Graph = graph;
                s_Request = new RuntimeDebugTargetRequest(
                    RuntimeSourceElementKey.Graph(authoring.AuthoringId),
                    new BtsmtlSkillGraphFingerprint().Compute(graph));
            }

            double now = EditorApplication.timeSinceStartup;
            if (now < s_NextResolveTime)
            {
                s_Dirty = true;
                return;
            }
            s_NextResolveTime = now + 0.1d;

            IGraphEditorObservation currentObservation = GraphEditor.currentGraph?.editorObservation;
            if (!s_Request.IsValid || currentObservation != null &&
                currentObservation is not BtsmtlSkillFlowObservation)
            {
                ReleaseInterest();
                return;
            }

            RuntimeDebugSession session = RuntimeDebugSession.Shared;
            RuntimeDebugTargetResolution resolution = session.ResolveTarget(s_Request);
            if (!resolution.CanReadSnapshot || !session.ViewModel.Valid)
            {
                ReleaseInterest();
                return;
            }

            if (currentObservation != null)
                ReleaseInterest();
            else if (!s_InterestActive)
            {
                session.EnsureLiveInterest(
                    s_InterestOwner,
                    RuntimeTraceChannel.Graph | RuntimeTraceChannel.StateMachine);
                s_InterestActive = true;
            }

            RuntimeDebugTargetInfo target = session.ViewModel.Target;
            CharacterPipelineDefinition definition = BtsmtlSkillHostEntry.ResolveDefinition(
                EditorUtility.InstanceIDToObject(target.HostInstanceId));
            if (!definition)
                return;

            RuntimeDebugViewModel view = session.ViewModel;
            s_Focus.Refresh(view);
            RuntimeInstanceKey instance = default;
            int matchCount = 0;
            for (int i = 0; i < s_Focus.ActiveCandidateCount; i++)
            {
                RuntimeDebugEventView focus = s_Focus.Candidates[i];
                RuntimeInstanceKey candidate = focus.Event.RuntimeInstance;
                if (candidate.CharacterRuntimeId != target.CharacterRuntimeId)
                    continue;
                if (candidate.Kind == RuntimeInstanceKind.SkillExecution)
                {
                    if (!string.Equals(focus.Source.GraphAuthoringId, authoring.AuthoringId, StringComparison.Ordinal))
                        continue;
                }
                else if (candidate.Kind == RuntimeInstanceKind.TimelinePlayback)
                {
                    RuntimeTimelinePlaybackProvenance provenance = focus.Event.Payload.TimelinePlayback;
                    if (!string.Equals(provenance.SourceGraphAuthoringId, authoring.AuthoringId, StringComparison.Ordinal) ||
                        !RuntimeDebugSourceNavigator.TryResolveTimelineGraphInstance(
                            view, candidate, provenance, s_Instances, out RuntimeInstanceKey graphInstance))
                        continue;
                    candidate = graphInstance;
                }
                else
                    continue;
                if (instance.Equals(candidate))
                    continue;
                instance = candidate;
                matchCount++;
            }

            if (matchCount != 1)
                return;
            if (BtsmtlSkillObservationSession.IsObserving(definition, graph, instance))
                return;

            s_Opening = true;
            try
            {
                ReleaseInterest();
                if (currentObservation is BtsmtlSkillFlowObservation previous)
                    previous.Dispose();
                BtsmtlSkillObservationSession.Open(definition, graph, session, instance);
            }
            finally
            {
                s_Opening = false;
            }
        }

        static void ReleaseInterest()
        {
            if (!s_InterestActive)
                return;
            RuntimeDebugSession.Shared.ReleaseLiveInterest(s_InterestOwner);
            s_InterestActive = false;
        }
    }
}
