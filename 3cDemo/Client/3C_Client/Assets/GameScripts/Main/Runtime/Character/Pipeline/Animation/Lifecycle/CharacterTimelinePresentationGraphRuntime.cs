using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal sealed class CharacterTimelinePresentationGraphRuntime : IFloat32PresentationGraphOutput
    {
        readonly List<Float32PresentationGraphRuntime> m_PresentationGraphs = new();
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
                m_DependencyResolver.InstallGraphSources(data.SourceMap);
                m_PresentationGraphs.Add(runtime);
                for (int index = 0; index < data.SourceMap.Count; index++)
                    if (data.SourceMap[index].InvocationCallerKind == ProgramInvocationCallerKind.PresentationMarker ||
                        data.SourceMap[index].InvocationCallerKind == ProgramInvocationCallerKind.PresentationTreeClip)
                        m_DependencyResolver.InstallPresentationGraph(data.SourceMap[index].GraphId);
            }
        }

        internal void Execute(in RuntimeTimelinePlaybackProvenance provenance,
            in TimelineRuntimePresentationFrame frame, in Float32PresentationGraphFacts facts,
            TimelineRuntimeCompositionHost host)
        {
            m_GraphFrame = frame;
            m_PresentationFacts = facts;
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
            }
        }

        bool ExecutePresentationGraph(in RuntimeTimelinePlaybackProvenance provenance, string graphId, string revision,
            ProgramInvocationCallerKind kind, AbilityTreeClipHook hook)
        {
            Float32PresentationGraphRuntime matched = null;
            int binding = -1;
            for (int index = 0; index < m_PresentationGraphs.Count; index++)
            {
                Float32PresentationGraphRuntime runtime = m_PresentationGraphs[index];
                if (!runtime.TryBind(provenance.SourceInvocationPath, provenance.SourceNodeAuthoringId,
                    m_GraphCaller, graphId, revision, kind, hook, out int candidate))
                    continue;
                if (matched != null)
                    throw new InvalidOperationException("Timeline Presentation graph invocation is ambiguous.");
                matched = runtime;
                binding = candidate;
            }
            if (matched == null)
                throw new InvalidOperationException($"Timeline Presentation graph '{m_GraphCaller}' has no compiled {hook} entry. parent={provenance.SourceInvocationPath};timeline={provenance.SourceNodeAuthoringId};graph={graphId};revision={revision}");
            return matched.Evaluate(binding, m_PresentationFacts, m_GraphFrame.Generation, this);
        }

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
