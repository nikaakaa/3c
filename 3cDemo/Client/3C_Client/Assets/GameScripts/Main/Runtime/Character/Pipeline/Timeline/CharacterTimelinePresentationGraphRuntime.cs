using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;
using ThirdPersonCharacter.Pipeline.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal sealed class CharacterTimelinePresentationGraphRuntime : IFloat32PresentationGraphOutput
    {
        readonly List<(Float32PresentationGraphRuntime Runtime, CharacterTimelinePresentationGraphDiagnostics.ProgramSources Sources)> m_PresentationGraphs = new();
        readonly CharacterTimelinePresentationGraphDiagnostics m_Diagnostics = new();
        string m_SkillId;
        ulong m_ActionInstanceId;
        Float32PresentationGraphFacts m_PresentationFacts;
        TimelineRuntimePresentationFrame m_GraphFrame;
        string m_GraphCaller;
        bool m_GraphMarker;
        int m_GraphCycle;
        long m_GraphTime;
        readonly CharacterTimelineDependencyResolver m_DependencyResolver;
        internal event Action<TimelinePresentationGraphCameraOutput> CameraPrepared;

        internal CharacterTimelinePresentationGraphRuntime(CharacterTimelineDependencyResolver dependencyResolver)
        {
            m_DependencyResolver = dependencyResolver;
        }

        internal void InstallPrograms(IReadOnlyList<Float32GameplayAbilityExecutionData> programs)
        {
            for (int programIndex = 0; programIndex < programs.Count; programIndex++)
            {
                Float32GameplayAbilityExecutionData data = programs[programIndex];
                bool hasMarkers = false;
                for (int index = 0; index < data.SourceMap.Count; index++)
                    hasMarkers |= data.SourceMap[index].TargetKind == ProgramSourceTargetKind.GraphInvocation &&
                        (data.SourceMap[index].InvocationCallerKind == ProgramInvocationCallerKind.PresentationMarker ||
                         data.SourceMap[index].InvocationCallerKind == ProgramInvocationCallerKind.PresentationTreeClip);
                if (!hasMarkers)
                    continue;
                var runtime = new Float32PresentationGraphRuntime(data);
                m_DependencyResolver.InstallGraphSources(data.SourceMap, true);
                string contentHash = data.ContentHash.ToString();
                var sourceMap = new DebugSourceMap(new RuntimeContentRevision(
                    $"float32-presentation/{data.AbilityId.Value}", contentHash, contentHash));
                AbilityDebugSourceMapFiller.Fill(sourceMap, 0, data.SourceMap);
                sourceMap.Seal();
                m_PresentationGraphs.Add((runtime, new CharacterTimelinePresentationGraphDiagnostics.ProgramSources(data, runtime, sourceMap)));
            }
        }

        internal void AttachRuntimeDiagnostics(RuntimeDiagnosticsContext diagnostics)
        {
            if (m_PresentationGraphs.Count == 0)
                return;
            m_Diagnostics.Attach(diagnostics);
            for (int i = 0; i < m_PresentationGraphs.Count; i++)
                diagnostics.RegisterSourceMap(m_PresentationGraphs[i].Sources.SourceMap);
        }

        internal void Execute(in RuntimeTimelinePlaybackProvenance provenance, string skillId, ulong actionInstanceId,
            in TimelineRuntimePresentationFrame frame, in Float32PresentationGraphFacts facts,
            TimelineRuntimeCompositionHost host)
        {
            m_GraphFrame = frame;
            m_PresentationFacts = facts;
            m_SkillId = skillId;
            m_ActionInstanceId = actionInstanceId;
            try
            {
                for (int index = 0; index < frame.Operations.TreeClips.Count; index++)
                {
                    TimelineRuntimeTreeClipRequest request = frame.Operations.TreeClips[index];
                    m_GraphCaller = request.ClipAuthoringId;
                    m_GraphMarker = false;
                    m_GraphCycle = request.Cycle;
                    m_GraphTime = request.Time.Raw;
                    AbilityTreeClipHook hook = request.EventKind switch
                    {
                        TimelineRuntimeTreeClipEventKind.Enter => AbilityTreeClipHook.OnEnable,
                        TimelineRuntimeTreeClipEventKind.Update => AbilityTreeClipHook.Root,
                        TimelineRuntimeTreeClipEventKind.Exit => AbilityTreeClipHook.OnDisable,
                        TimelineRuntimeTreeClipEventKind.Destroy => AbilityTreeClipHook.OnDestroy,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    if (ExecutePresentationGraph(in provenance, request.TreeGraphId, request.TreeGraphRevision,
                        ProgramInvocationCallerKind.PresentationTreeClip, hook))
                        host.RequestPresentationTreeClipExit(frame, request);
                }
                for (int index = 0; index < frame.Events.Count; index++)
                {
                    TimelineRuntimePresentationEvent marker = frame.Events[index];
                    m_GraphCaller = marker.MarkerAuthoringId;
                    m_GraphMarker = true;
                    m_GraphCycle = marker.Cycle;
                    m_GraphTime = marker.Time.Raw;
                    ExecutePresentationGraph(in provenance, marker.GraphId, marker.GraphRevision,
                        ProgramInvocationCallerKind.PresentationMarker, AbilityTreeClipHook.OnEnable);
                }
            }
            finally
            {
                m_GraphFrame = default;
                m_PresentationFacts = default;
                m_GraphCaller = null;
                m_SkillId = null;
                m_ActionInstanceId = 0;
            }
        }

        bool ExecutePresentationGraph(in RuntimeTimelinePlaybackProvenance provenance, string graphId, string revision,
            ProgramInvocationCallerKind kind, AbilityTreeClipHook hook)
        {
            Float32PresentationGraphRuntime matched = null;
            CharacterTimelinePresentationGraphDiagnostics.ProgramSources sources = null;
            int binding = -1;
            for (int index = 0; index < m_PresentationGraphs.Count; index++)
            {
                Float32PresentationGraphRuntime runtime = m_PresentationGraphs[index].Runtime;
                if (runtime.AbilityId.Value != m_SkillId ||
                    !runtime.TryBind(provenance.SourceInvocationPath, provenance.SourceNodeAuthoringId,
                    m_GraphCaller, graphId, revision, kind, hook, out int candidate))
                    continue;
                if (matched != null)
                    throw new InvalidOperationException("Timeline Presentation graph invocation is ambiguous.");
                matched = runtime;
                sources = m_PresentationGraphs[index].Sources;
                binding = candidate;
            }
            if (matched == null)
                throw new InvalidOperationException($"Timeline Presentation graph '{m_GraphCaller}' has no compiled {hook} entry. parent={provenance.SourceInvocationPath};timeline={provenance.SourceNodeAuthoringId};graph={graphId};revision={revision}");
            m_Diagnostics.Begin(sources, binding, provenance, m_GraphFrame, m_SkillId, m_ActionInstanceId,
                m_GraphCaller, m_GraphCycle, m_GraphTime);
            try
            {
                return matched.Evaluate(binding, m_PresentationFacts, m_GraphFrame.Generation, this);
            }
            finally
            {
                m_Diagnostics.End();
            }
        }

        internal void CommitDiagnostics(ulong frame) => m_Diagnostics.Commit(frame);
        internal void DiscardDiagnostics() => m_Diagnostics.Discard();
        internal void DetachRuntimeDiagnostics() => m_Diagnostics.Detach();

        bool IFloat32PresentationGraphOutput.CaptureGraphTrace => m_Diagnostics.CaptureGraph;
        bool IFloat32PresentationGraphOutput.CaptureValueTrace => m_Diagnostics.CaptureValues;
        void IFloat32PresentationGraphOutput.TraceOperation(OperationHandle operation, string code, string detail, ulong generation) =>
            m_Diagnostics.TraceOperation(operation, code, detail, generation);
        void IFloat32PresentationGraphOutput.TraceEdge(ProgramControlFlowEdge edge, bool selected, bool passed) =>
            m_Diagnostics.TraceEdge(edge, selected, passed);
        void IFloat32PresentationGraphOutput.TraceValue(OperationHandle operation, string port, AbilityStateValue value, bool input) =>
            m_Diagnostics.TraceValue(operation, port, value, input);

        void IFloat32PresentationGraphOutput.SubmitCamera(in Float32PresentationGraphOutputIdentity identity, string producer,
            in PresentationCameraRequest activation,
            in PresentationCameraRequest retirement, bool retiring)
        {
            if (CameraPrepared == null)
                throw new InvalidOperationException("Timeline Presentation graph has no composed Camera consumer.");
            CameraPrepared(new TimelinePresentationGraphCameraOutput(m_GraphFrame, m_GraphCaller,
                m_GraphMarker, m_GraphCycle, m_GraphTime, identity, producer, activation, retirement, retiring));
        }

    }
}
