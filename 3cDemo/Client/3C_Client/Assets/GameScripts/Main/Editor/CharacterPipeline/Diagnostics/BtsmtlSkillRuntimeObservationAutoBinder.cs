using System;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using FlowCanvas;
using NodeCanvas.Editor;
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
        static FlowGraph s_Graph;
        static RuntimeDebugTargetRequest s_Request;

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

        static void OnCurrentGraphChanged(Graph graph) => MarkDirty();

        static void OnEditorClosed()
        {
            ReleaseInterest();
            s_Graph = null;
            s_Request = default;
        }

        static void OnTargetChanged(RuntimeDiagnosticsTarget target) => MarkDirty();

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
            if (!Application.isPlaying || GraphEditor.current == null ||
                GraphEditor.rootGraph is not BtsmtlSkillFlowGraph graph ||
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

            if (graph.editorObservation != null || !s_Request.IsValid)
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

            if (!s_InterestActive)
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

            RuntimeInstanceKey[] instances = session.ViewModel
                .GetGraphInstances(authoring.AuthoringId)
                .Where(instance => instance.Kind == RuntimeInstanceKind.SkillExecution &&
                    instance.CharacterRuntimeId == target.CharacterRuntimeId)
                .ToArray();
            if (instances.Length != 1)
                return;

            s_Opening = true;
            try
            {
                ReleaseInterest();
                BtsmtlSkillObservationSession.Open(definition, graph, session, instances[0]);
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
