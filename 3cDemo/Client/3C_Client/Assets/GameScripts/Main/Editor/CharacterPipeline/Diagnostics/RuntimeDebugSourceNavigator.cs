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
        static BtsmtlSkillGraphFingerprint s_Fingerprint;
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
                s_Fingerprint = new BtsmtlSkillGraphFingerprint();
                s_Definition = definition;
                s_SessionId = target.SessionId;
                s_Revision = target.Revision;
            }
            return s_Sources;
        }

        internal static FlowGraph ResolveGraph(CharacterPipelineDefinition definition, string graphAuthoringId) =>
            GetSources(definition).Graphs[graphAuthoringId];

        internal static RuntimeDebugTargetRequest CreateTargetRequest(CharacterPipelineDefinition definition, FlowGraph graph)
        {
            GetSources(definition);
            return new RuntimeDebugTargetRequest(
                RuntimeSourceElementKey.Graph(((IBtsmtlSkillFlowGraph)graph).AuthoringId),
                s_Fingerprint.Compute(graph));
        }

        public static bool Open(RuntimeDebugEventView eventView, bool pin = false, bool preserveFocus = false)
        {
            EditorWindow previousFocus = preserveFocus ? EditorWindow.focusedWindow : null;
            try
            {
                RuntimeTraceEvent trace = eventView.Event;
                RuntimeInstanceKey instance = trace.RuntimeInstance;
                RuntimeDebugSession session = RuntimeDebugSession.Shared;
                bool historical = session.AttachmentState is RuntimeDebugAttachmentState.CaptureHistory or RuntimeDebugAttachmentState.Ended;
                if (!instance.IsValid || !eventView.Source.IsValid)
                    return false;
                RuntimeDiagnosticsTarget target = null;
                RuntimeDebugTargetInfo targetInfo = session.ViewModel.Target;
                if (historical)
                {
                    if (!session.TryResolveHistoricalSource(
                            trace.ContentRevision,
                            trace.Source,
                            out RuntimeSourceElementKey historicalSource,
                            out DebugSourceMapEntry historicalEntry) ||
                        !historicalSource.Equals(eventView.Source) ||
                        !historicalEntry.Source.Equals(eventView.Source))
                        return false;
                }
                else
                {
                    if (!RuntimeDiagnosticsTargetRegistry.TryGet(instance.CharacterRuntimeId, out target) ||
                        target.SessionId != trace.SessionId ||
                        !target.Context.TryGetSourceMap(trace.ContentRevision, out IDebugSourceMap sourceMap) ||
                        !sourceMap.TryGet(trace.Source, out DebugSourceMapEntry entry) ||
                        !entry.Source.Equals(eventView.Source))
                        return false;
                    targetInfo = new RuntimeDebugTargetInfo(target);
                }
                if (targetInfo.CharacterRuntimeId != instance.CharacterRuntimeId)
                    return false;
                CharacterPipelineDefinition definition = BtsmtlSkillHostEntry.ResolveDefinition(EditorUtility.InstanceIDToObject(targetInfo.HostInstanceId));
                if (!definition)
                    return false;
                if (!historical &&
                    !session.AttachToTarget(instance.CharacterRuntimeId))
                    return false;
                if (eventView.Source.Kind is RuntimeSourceElementKind.Timeline or RuntimeSourceElementKind.Track or
                    RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip)
                {
                    return OpenTimelineSource(definition, eventView.Source, instance, trace.Payload.TimelinePlayback, pin);
                }
                return Open(definition, eventView.Source, instance, default, string.Empty, pin);
            }
            finally
            {
                previousFocus?.Focus();
            }
        }

        public static bool Open(CharacterPipelineDefinition definition, RuntimeSourceElementKey source, RuntimeInstanceKey instance = default)
            => Open(definition, source, instance, default, string.Empty, false);

        static bool Open(CharacterPipelineDefinition definition, RuntimeSourceElementKey source,
            RuntimeInstanceKey instance, RuntimeInstanceKey playback, string expectedTimelineId, bool pin)
        {
            if (!definition || !source.IsValid)
                return false;

            return !string.IsNullOrEmpty(source.GraphAuthoringId) &&
                   GetSources(definition).Graphs.TryGetValue(source.GraphAuthoringId, out FlowGraph graph) &&
                   OpenSkillGraph(definition, graph, source, instance, playback, expectedTimelineId, pin);
        }

        static bool OpenTimelineSource(
            CharacterPipelineDefinition definition,
            RuntimeSourceElementKey source,
            RuntimeInstanceKey instance,
            RuntimeTimelinePlaybackProvenance provenance,
            bool pin)
        {
            if (!provenance.IsValid || string.IsNullOrEmpty(source.TimelineAuthoringId) ||
                string.IsNullOrEmpty(provenance.SourceGraphAuthoringId) ||
                string.IsNullOrEmpty(provenance.SourceNodeAuthoringId))
            {
                return false;
            }
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
                source.TimelineAuthoringId,
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
                if (graphInstance.IsValid)
                    return false;
                graphInstance = candidate;
            }
            return graphInstance.IsValid;
        }

        static bool OpenSkillGraph(CharacterPipelineDefinition definition, FlowGraph graph, RuntimeSourceElementKey source,
            RuntimeInstanceKey instance, RuntimeInstanceKey playback, string expectedTimelineId, bool pin)
        {
            NodeCanvas.Framework.IGraphElement element = null;
            if (source.Kind is RuntimeSourceElementKind.Node or RuntimeSourceElementKind.Port)
                GetSources(definition).Elements.TryGetValue(
                    RuntimeSourceElementKey.Node(source.GraphAuthoringId, source.ElementAuthoringId), out element);
            else if (source.Kind == RuntimeSourceElementKind.Edge)
                GetSources(definition).Elements.TryGetValue(source, out element);
            else if (source.Kind is RuntimeSourceElementKind.Timeline or RuntimeSourceElementKind.Track or RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip)
            {
                int matches = 0;
                for (int i = 0; i < graph.allNodes.Count; i++)
                {
                    if (graph.allNodes[i] is not BtsmtlSkillTimelineFlowNode timeline || timeline.Timeline == null ||
                        !string.Equals(timeline.Timeline.AuthoringId, source.TimelineAuthoringId, StringComparison.Ordinal))
                        continue;
                    element = timeline;
                    matches++;
                }
                if (matches != 1)
                    return false;
            }
            else if (source.Kind is not RuntimeSourceElementKind.Graph and not RuntimeSourceElementKind.BlackboardDeclaration)
                return false;
            if (source.Kind is not RuntimeSourceElementKind.Graph and not RuntimeSourceElementKind.BlackboardDeclaration && element == null)
                return false;
            if (playback.IsValid &&
                (element is not BtsmtlSkillTimelineFlowNode playbackNode ||
                 !string.Equals(playbackNode.Timeline?.AuthoringId, expectedTimelineId, StringComparison.Ordinal)))
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
                        if (!timelineWindow.SelectRuntimeObservationPlayback(selected, pin))
                            return false;
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
