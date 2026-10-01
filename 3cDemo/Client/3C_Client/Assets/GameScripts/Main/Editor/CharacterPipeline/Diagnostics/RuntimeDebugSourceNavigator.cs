using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using FlowCanvas;
using NodeCanvas.Editor;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    static class RuntimeDebugSourceNavigator
    {
        static readonly BtsmtlSkillGraphClosureIndex s_Sources = new();
        static readonly List<RuntimeInstanceKey> s_Instances = new();
        static CharacterPipelineDefinition s_Definition;
        static Guid s_SessionId;
        static RuntimeContentRevision s_Revision;

        static RuntimeDebugSourceNavigator()
        {
            EditorApplication.projectChanged += InvalidateSources;
            Undo.undoRedoPerformed += InvalidateSources;
            ObjectChangeEvents.changesPublished += OnObjectChanges;
        }

        static void InvalidateSources() => s_Definition = null;

        static void OnObjectChanges(ref ObjectChangeEventStream changes)
        {
            for (int i = 0; i < changes.length; i++)
            {
                if (changes.GetEventType(i) != ObjectChangeKind.ChangeAssetObjectProperties)
                    continue;
                InvalidateSources();
                return;
            }
        }

        static BtsmtlSkillGraphClosureIndex GetSources(CharacterPipelineDefinition definition)
        {
            RuntimeDebugTargetInfo target = RuntimeDebugSession.Shared.ViewModel.Target;
            if (!ReferenceEquals(s_Definition, definition) || s_SessionId != target.SessionId ||
                !s_Revision.Equals(target.Revision))
            {
                s_Sources.Build(definition);
                s_Definition = definition;
                s_SessionId = target.SessionId;
                s_Revision = target.Revision;
            }
            return s_Sources;
        }

        internal static FlowGraph ResolveGraph(CharacterPipelineDefinition definition, string graphAuthoringId) =>
            GetSources(definition).Graphs[graphAuthoringId];

        internal static NodeCanvas.Framework.IGraphElement ResolveElement(CharacterPipelineDefinition definition, RuntimeSourceElementKey source) =>
            GetSources(definition).Elements[source];

        public static bool Open(RuntimeDebugEventView eventView, bool pin = false, bool preserveFocus = false)
        {
            EditorWindow previousFocus = preserveFocus ? EditorWindow.focusedWindow : null;
            try
            {
                RuntimeTraceEvent trace = eventView.Event;
                RuntimeInstanceKey instance = trace.RuntimeInstance;
                RuntimeDebugSession session = RuntimeDebugSession.Shared;
                bool historical = session.AttachmentState is RuntimeDebugAttachmentState.CaptureHistory or RuntimeDebugAttachmentState.Ended;
                if (!historical)
                    session.AttachToTarget(instance.CharacterRuntimeId);
                RuntimeDebugTargetInfo targetInfo = session.ViewModel.Target;
                CharacterPipelineDefinition definition = BtsmtlSkillHostEntry.ResolveDefinition(EditorUtility.InstanceIDToObject(targetInfo.HostInstanceId));
                if (eventView.Source.Kind is RuntimeSourceElementKind.Timeline or RuntimeSourceElementKind.Track or
                    RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip)
                {
                    return OpenTimelineSource(definition, instance, trace.Payload.TimelinePlayback, pin);
                }
                return Open(definition, eventView.Source, instance, default, pin);
            }
            finally
            {
                previousFocus?.Focus();
            }
        }

        public static bool Open(CharacterPipelineDefinition definition, RuntimeSourceElementKey source, RuntimeInstanceKey instance = default)
            => Open(definition, source, instance, default, false);

        static bool Open(CharacterPipelineDefinition definition, RuntimeSourceElementKey source,
            RuntimeInstanceKey instance, RuntimeInstanceKey playback, bool pin)
        {
            FlowGraph graph = GetSources(definition).Graphs[source.GraphAuthoringId];
            return OpenSkillGraph(definition, graph, source, instance, playback, pin);
        }

        static bool OpenTimelineSource(
            CharacterPipelineDefinition definition,
            RuntimeInstanceKey instance,
            RuntimeTimelinePlaybackProvenance provenance,
            bool pin)
        {
            if (!TryResolveTimelineGraphInstance(
                    RuntimeDebugSession.Shared.ViewModel,
                    instance,
                    provenance,
                    s_Instances,
                    out RuntimeInstanceKey graphInstance))
                return false;
            return Open(
                definition,
                RuntimeSourceElementKey.Node(
                    provenance.SourceGraphAuthoringId,
                    provenance.SourceNodeAuthoringId),
                graphInstance,
                instance,
                pin);
        }

        internal static bool TryResolveTimelineGraphInstance(
            RuntimeDebugViewModel view,
            RuntimeInstanceKey playback,
            RuntimeTimelinePlaybackProvenance provenance,
            List<RuntimeInstanceKey> instances,
            out RuntimeInstanceKey graphInstance)
        {
            graphInstance = default;
            view.CopyGraphInstances(provenance.SourceGraphAuthoringId, instances);
            for (int i = 0; i < instances.Count; i++)
            {
                RuntimeInstanceKey candidate = instances[i];
                if (candidate.Kind != RuntimeInstanceKind.SkillExecution ||
                    candidate.CharacterRuntimeId != playback.CharacterRuntimeId ||
                    candidate.ActionInstanceId != playback.ActionInstanceId ||
                    (provenance.SourceGraphRuntimeId != Guid.Empty &&
                     candidate.GraphRuntimeId != provenance.SourceGraphRuntimeId) ||
                    candidate.ActivationGeneration != provenance.SkillExecutionGeneration ||
                    candidate.InvocationGeneration != provenance.SourceActivationGeneration ||
                    !string.Equals(candidate.CallSiteId, provenance.SourceInvocationPath, StringComparison.Ordinal))
                    continue;
                graphInstance = candidate;
                return true;
            }
            return false;
        }

        static bool OpenSkillGraph(CharacterPipelineDefinition definition, FlowGraph graph, RuntimeSourceElementKey source,
            RuntimeInstanceKey instance, RuntimeInstanceKey playback, bool pin)
        {
            NodeCanvas.Framework.IGraphElement element = null;
            if (source.Kind is RuntimeSourceElementKind.Node or RuntimeSourceElementKind.Port)
                element = ResolveElement(definition,
                    RuntimeSourceElementKey.Node(source.GraphAuthoringId, source.ElementAuthoringId));
            else if (source.Kind == RuntimeSourceElementKind.Edge)
                element = ResolveElement(definition, source);
            else if (source.Kind is not RuntimeSourceElementKind.Graph and not RuntimeSourceElementKind.BlackboardDeclaration)
                return false;
            bool graphAlreadyOpen = GraphEditor.current != null &&
                                    ReferenceEquals(GraphEditor.rootGraph, graph) &&
                                    ReferenceEquals(GraphEditor.currentGraph, graph);
            bool observationAlreadyOpen = graphAlreadyOpen &&
                                          BtsmtlSkillObservationSession.IsObserving(definition, graph, instance);
            if (!observationAlreadyOpen)
            {
                BtsmtlSkillObservationSession.Close();
                if (graph.editorObservation is BtsmtlSkillFlowObservation existing)
                    existing.Dispose();
            }
            if (!graphAlreadyOpen)
            {
                if (GraphEditor.currentGraph?.editorObservation is BtsmtlSkillFlowObservation previous)
                    previous.Dispose();
                graph.SetCurrentChildGraphAssignable(null);
                GraphEditor window = GraphEditor.OpenWindow(graph);
                window.Show();
                window.Focus();
            }
            if (element != null)
                GraphEditor.FocusElement(element, true);
            if (!observationAlreadyOpen &&
                instance.Kind == RuntimeInstanceKind.SkillExecution)
                BtsmtlSkillObservationSession.Open(definition, graph, RuntimeDebugSession.Shared, instance);
            if (element is BtsmtlSkillTimelineFlowNode timelineNode)
            {
                TimelineEditorWindow timelineWindow = TimelineEditorWindow.FindOpen(timelineNode.TimelineAsset);
                if (timelineWindow == null ||
                    !string.Equals(timelineWindow.SourceGraphAuthoringId, ((IBtsmtlSkillFlowGraph)graph).AuthoringId, StringComparison.Ordinal) ||
                    !string.Equals(timelineWindow.SourceNodeAuthoringId, timelineNode.UID, StringComparison.Ordinal))
                {
                    BtsmtlSkillObservationSession.ExpectTimelineOpening(timelineNode);
                    timelineWindow = TimelineEditorWindow.Open(
                        timelineNode.TimelineAsset,
                        ((IBtsmtlSkillFlowGraph)graph).AuthoringId,
                        timelineNode.UID);
                    BtsmtlSkillObservationSession.ExpectTimelineOpening(null);
                }
                else
                    BtsmtlSkillObservationSession.BindOpenTimeline(timelineNode);
                if (timelineWindow != null && (instance.IsValid || playback.IsValid))
                {
                    timelineWindow.SetRuntimeObservationReadOnly(true);
                    RuntimeInstanceKey selected = playback.IsValid ? playback : ResolveTimelinePlayback(instance);
                    if (selected.IsValid)
                    {
                        timelineWindow.SelectRuntimeObservationPlayback(selected, pin);
                        TimelineRuntimeObservationBridge.RefreshWindow(timelineWindow);
                    }
                }
                if (timelineWindow == null)
                    return false;
                if (source.Kind is RuntimeSourceElementKind.Track or RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip)
                    timelineWindow.FocusSource(
                        source.TrackAuthoringId,
                        source.Kind is RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip ? source.ClipAuthoringId : string.Empty);
                return true;
            }
            return true;
        }

        static RuntimeInstanceKey ResolveTimelinePlayback(RuntimeInstanceKey instance)
        {
            if (instance.Kind == RuntimeInstanceKind.TimelinePlayback)
                return instance;
            return instance.Kind == RuntimeInstanceKind.TreeClip
                ? RuntimeInstanceKey.Timeline(
                    instance.CharacterRuntimeId,
                    instance.SourceOperationIndex,
                    instance.TimelinePlaybackId,
                    instance.ActionInstanceId)
                : default;
        }
    }
}
