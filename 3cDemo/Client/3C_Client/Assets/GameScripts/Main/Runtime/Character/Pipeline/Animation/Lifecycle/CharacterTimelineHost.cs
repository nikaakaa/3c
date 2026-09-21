using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonGameplay.Tick;
using TreeDesigner;
using TimelinePlaybackStatus = BTSMTL.Timeline.TimelinePlaybackStatus;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal sealed class CharacterTimelineDomainBindingResolver : ITimelineDomainBindingResolver
    {
        readonly HashSet<string> m_SupportedTargets;
        int m_NextHandle = 1;
        internal CharacterTimelineDomainBindingResolver(IEnumerable<string> supportedTargets)
        {
            m_SupportedTargets = new HashSet<string>(supportedTargets ?? Array.Empty<string>(), StringComparer.Ordinal);
        }

        public bool TryResolve(
            TimelineBindingDeclaration declaration,
            TimelineBindingValue callValue,
            out TimelineBindingHandle handle,
            out string error)
        {
            handle = default;
            error = string.Empty;
            if (callValue.ValueKind == TimelineBindingValueKind.Target &&
                !string.IsNullOrEmpty(callValue.TargetIdentity) &&
                !m_SupportedTargets.Contains(callValue.TargetIdentity))
            {
                error = $"Timeline target '{callValue.TargetIdentity}' is not supported by this character.";
                return false;
            }
            handle = new TimelineBindingHandle(m_NextHandle++);
            return true;
        }
    }

    internal sealed class CharacterTimelineDependencyResolver : ITimelineRuntimeDependencyResolver
    {
        readonly Dictionary<string, string> m_GraphRevisions = new(StringComparer.Ordinal);
        readonly HashSet<string> m_PresentationGraphs = new(StringComparer.Ordinal);

        internal void InstallPresentationGraph(string graphId) => m_PresentationGraphs.Add("tree:" + graphId);
        readonly Dictionary<string, string> m_CurveRevisions = new(StringComparer.Ordinal);
        readonly Dictionary<string, TimelineRuntimeDependencyHandle> m_Handles = new(StringComparer.Ordinal);
        TimelineRuntimeNumericTarget m_NumericTarget;

        internal void InstallGraphSources(IReadOnlyList<ProgramSourceMapEntry> sources)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                ProgramSourceMapEntry source = sources[i];
                if (string.IsNullOrEmpty(source.GraphId) || string.IsNullOrEmpty(source.ContentHash))
                    continue;
                string identity = $"tree:{source.GraphId}";
                if (m_GraphRevisions.TryGetValue(identity, out string revision) && revision != source.ContentHash)
                    throw new InvalidOperationException($"Compiled Timeline graph '{identity}' has conflicting revisions.");
                m_GraphRevisions[identity] = source.ContentHash;
            }
        }

        internal void InstallContent(IEnumerable<TimelineData> timelines, TimelineRuntimeNumericTarget numericTarget)
        {
            m_NumericTarget = numericTarget;
            m_CurveRevisions.Clear();
            m_Handles.Clear();
            foreach (TimelineData timeline in timelines)
            {
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    Track track = timeline.Tracks[trackIndex];
                    if (track.ExecutionDomain == TimelineExecutionDomain.Presentation)
                        for (int markerIndex = 0; markerIndex < track.Markers.Count; markerIndex++)
                        {
                            if (track.Markers[markerIndex].Graph is not ITimelineTreeGraphAsset graph ||
                                !m_PresentationGraphs.Contains("tree:" + graph.AuthoringId))
                                throw new InvalidOperationException($"Timeline '{timeline.AuthoringId}' marker '{track.Markers[markerIndex].AuthoringId}' has no installed Presentation graph.");
                        }
                    for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                    {
                        if (track.Clips[clipIndex] is not MotionCurveClip motion)
                            continue;
                        if (!motion.SourceCurve || !motion.SourceCurve.TryValidate(out string error))
                            throw new InvalidOperationException($"Timeline motion source '{motion.AuthoringId}' is invalid.");
                        string revision = SourceContentHasher.Hash(JsonUtility.ToJson(motion.SourceCurve));
                        m_CurveRevisions[$"motion-curve:{revision}"] = revision;
                    }
                }
            }
            foreach (string identity in m_GraphRevisions.Keys)
                m_Handles.Add(identity, new TimelineRuntimeDependencyHandle(m_Handles.Count + 1));
            foreach (string identity in m_CurveRevisions.Keys)
                m_Handles.Add(identity, new TimelineRuntimeDependencyHandle(m_Handles.Count + 1));
        }

        public bool TryResolve(
            TimelineContentDependency dependency,
            TimelineRuntimeNumericTarget numericTarget,
            out TimelineRuntimeDependencyHandle handle,
            out string error)
        {
            handle = TimelineRuntimeDependencyHandle.Invalid;
            Dictionary<string, string> revisions = dependency.Kind switch
            {
                "timeline.tree" => m_GraphRevisions,
                "timeline.motion-curve" => m_CurveRevisions,
                _ => null
            };
            if (numericTarget != m_NumericTarget || revisions == null ||
                !revisions.TryGetValue(dependency.Identity, out string revision) ||
                !string.Equals(revision, dependency.ContentHash, StringComparison.Ordinal) ||
                !m_Handles.TryGetValue(dependency.Identity, out handle))
            {
                error = $"Timeline dependency '{dependency.Identity}' has no installed {numericTarget} resource matching revision '{dependency.ContentHash}'.";
                return false;
            }
            error = string.Empty;
            return true;
        }
    }

    internal sealed class CharacterTimelineTreeClipService : ITimelineRuntimeTreeClipService
    {
        readonly CharacterTimelineHost m_Host;
        readonly Dictionary<ulong, List<AbilityTimelineTreeClipState>> m_ActiveClips = new();

        internal CharacterTimelineTreeClipService(CharacterTimelineHost host)
        {
            m_Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        internal IReadOnlyList<AbilityTimelineTreeClipState> Capture(ulong handle) =>
            m_ActiveClips.TryGetValue(handle, out List<AbilityTimelineTreeClipState> clips)
                ? clips : Array.Empty<AbilityTimelineTreeClipState>();

        internal void Restore(ulong handle, TimelineSnapshotItems<AbilityTimelineTreeClipState> restored)
        {
            if (restored.Count == 0)
            {
                m_ActiveClips.Remove(handle);
                return;
            }
            if (!m_ActiveClips.TryGetValue(handle, out List<AbilityTimelineTreeClipState> clips))
            {
                clips = new List<AbilityTimelineTreeClipState>(restored.Count);
                m_ActiveClips.Add(handle, clips);
            }
            clips.Clear();
            for (int index = 0; index < restored.Count; index++)
                clips.Add(restored[index]);
        }

        public bool RequestTreeClipExit(TimelinePlaybackHandle handle, string clipAuthoringId)
        {
            if (!m_Host.IsInitialized)
                return false;
            return m_Host.Host.Service.RequestTreeClipExit(new TimelineRuntimePlaybackHandle(handle.Value), clipAuthoringId);
        }

        public bool Consume(TimelineRuntimeTreeClipRequest request, TimelineRuntimeStepContext context)
        {
            if (!m_Host.IsAbilityRuntimePlayback(context.Playback.Handle))
                return true;
            IAbilityTreeClipInvoker invoker = m_Host.m_ActiveTreeClipInvoker
                ?? throw new InvalidOperationException($"Timeline TreeClip '{request.ClipAuthoringId}' requires an active Ability invoker.");
            if (request.EventKind == TimelineRuntimeTreeClipEventKind.Update && !IsActive(request, context))
                return true;
            var invocation = new AbilityTreeClipInvocation(
                request.ClipAuthoringId,
                request.TreeGraphId,
                request.EventKind switch
                {
                    TimelineRuntimeTreeClipEventKind.Enter => AbilityTreeClipHook.OnEnable,
                    TimelineRuntimeTreeClipEventKind.Update => AbilityTreeClipHook.Root,
                    TimelineRuntimeTreeClipEventKind.Exit => AbilityTreeClipHook.OnDisable,
                    _ => throw new ArgumentOutOfRangeException(nameof(request))
                },
                request.Cycle,
                m_Host.RequireAbilityPlaybackActionInstanceId(context.Playback.Handle),
                checked((int)context.Playback.Handle.Value));
            return invoker.InvokeTreeClip(invocation);
        }

        bool IsActive(TimelineRuntimeTreeClipRequest request, TimelineRuntimeStepContext context)
        {
            TimelineRuntimeSampleView<TimelineRuntimeTreeClipRequest> requests = context.Advance.Evaluation.TreeClips;
            for (int index = requests.Count - 1; index >= 0; index--)
            {
                TimelineRuntimeTreeClipRequest candidate = requests[index];
                if (candidate.EventKind != TimelineRuntimeTreeClipEventKind.Update &&
                    candidate.ClipAuthoringId == request.ClipAuthoringId && candidate.Cycle == request.Cycle)
                    return candidate.EventKind == TimelineRuntimeTreeClipEventKind.Enter;
            }
            if (m_ActiveClips.TryGetValue(context.Playback.Handle.Value, out List<AbilityTimelineTreeClipState> active))
                for (int index = 0; index < active.Count; index++)
                    if (active[index].ClipAuthoringId == request.ClipAuthoringId && active[index].Cycle == request.Cycle)
                        return true;
            return false;
        }

        public void Discard(TimelineRuntimeTreeClipRequest request, TimelineRuntimeStepContext context) { }

        public void Commit(TimelineRuntimeStepContext context)
        {
            if (!m_Host.IsAbilityRuntimePlayback(context.Playback.Handle))
                return;
            TimelineRuntimeSampleView<TimelineRuntimeTreeClipRequest> requests = context.Advance.Evaluation.TreeClips;
            ulong handle = context.Playback.Handle.Value;
            for (int index = 0; index < requests.Count; index++)
            {
                TimelineRuntimeTreeClipRequest request = requests[index];
                if (request.EventKind == TimelineRuntimeTreeClipEventKind.Update)
                    continue;
                RemoveClip(handle, request.ClipAuthoringId, request.Cycle);
                if (request.EventKind != TimelineRuntimeTreeClipEventKind.Enter)
                    continue;
                if (!m_ActiveClips.TryGetValue(handle, out List<AbilityTimelineTreeClipState> clips))
                {
                    clips = new List<AbilityTimelineTreeClipState>(context.Playback.Content.Clips.Count);
                    m_ActiveClips.Add(handle, clips);
                }
                clips.Add(new AbilityTimelineTreeClipState(request.ClipAuthoringId, request.TreeGraphId, request.Cycle));
            }
            if (context.Advance.Completes)
                m_ActiveClips.Remove(handle);
        }

        public void DiscardStep(TimelineRuntimeStepContext context) { }
        public bool ConsumeStop(TimelineRuntimeStopRequest request)
        {
            if (!m_ActiveClips.TryGetValue(request.Handle.Value, out List<AbilityTimelineTreeClipState> clips) || clips.Count == 0)
                return true;
            IAbilityTreeClipInvoker invoker = m_Host.m_ActiveTreeClipInvoker
                ?? throw new InvalidOperationException("Timeline stop requires the current Ability invoker.");
            foreach (AbilityTimelineTreeClipState clip in clips)
            {
                var invocation = new AbilityTreeClipInvocation(
                    clip.ClipAuthoringId,
                    clip.TreeGraphId,
                    AbilityTreeClipHook.OnDestroy,
                    clip.Cycle,
                    m_Host.RequireAbilityPlaybackActionInstanceId(request.Handle),
                    checked((int)request.Handle.Value));
                invoker.InvokeTreeClip(invocation);
            }
            return true;
        }

        public void CommitStop(TimelineRuntimeStopRequest request) =>
            m_ActiveClips.Remove(request.Handle.Value);

        public void DiscardStop(TimelineRuntimeStopRequest request) { }

        void RemoveClip(ulong handle, string clipAuthoringId, int cycle)
        {
            if (!m_ActiveClips.TryGetValue(handle, out List<AbilityTimelineTreeClipState> clips))
                return;
            for (int index = clips.Count - 1; index >= 0; index--)
                if (clips[index].ClipAuthoringId == clipAuthoringId && clips[index].Cycle == cycle)
                    clips.RemoveAt(index);
        }
    }

    public enum CharacterTimelinePlaybackSourceKind : byte
    {
        None = 0,
        AbilityRuntime = 1
    }
    public readonly struct CharacterTimelinePlaybackObservation
    {
        internal CharacterTimelinePlaybackObservation(
            TimelinePlaybackHandle handle,
            TimelinePlaybackStatus status,
            TimelineData timeline,
            string sourceName,
            CharacterTimelinePlaybackSourceKind sourceKind,
            float clipTime,
            float normalizedTime,
            float weight,
            string clipAuthoringId)
        {
            Handle = handle;
            Status = status;
            Timeline = timeline;
            SourceName = sourceName ?? string.Empty;
            SourceKind = sourceKind;
            ClipTime = clipTime;
            NormalizedTime = normalizedTime;
            Weight = weight;
            ClipAuthoringId = clipAuthoringId ?? string.Empty;
        }

        public TimelinePlaybackHandle Handle { get; }
        public TimelinePlaybackStatus Status { get; }
        public TimelineData Timeline { get; }
        public string SourceName { get; }
        public CharacterTimelinePlaybackSourceKind SourceKind { get; }
        public float ClipTime { get; }
        public float NormalizedTime { get; }
        public float Weight { get; }
        public string ClipAuthoringId { get; }
    }


    internal sealed class CharacterTimelineMarkerService : ITimelineRuntimeMarkerService
    {
        readonly CharacterTimelineHost m_Host;

        internal CharacterTimelineMarkerService(CharacterTimelineHost host)
        {
            m_Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public bool Consume(TimelineRuntimeMarkerRequest request, TimelineRuntimeStepContext context)
        {
            if (!m_Host.IsAbilityRuntimePlayback(context.Playback.Handle))
                return true;
            IAbilityTreeClipInvoker invoker = m_Host.m_ActiveTreeClipInvoker
                ?? throw new InvalidOperationException($"Timeline Marker '{request.MarkerAuthoringId}' requires an active Ability invoker.");
            var invocation = new AbilityTreeClipInvocation(
                request.MarkerAuthoringId,
                request.GraphId,
                AbilityTreeClipHook.OnEnable,
                request.Cycle,
                m_Host.RequireAbilityPlaybackActionInstanceId(context.Playback.Handle),
                checked((int)context.Playback.Handle.Value));
            return invoker.InvokeTreeClip(invocation);
        }

        public void Discard(TimelineRuntimeMarkerRequest request, TimelineRuntimeStepContext context)
        {
        }

        public void Commit(TimelineRuntimeStepContext context)
        {
        }

        public void DiscardStep(TimelineRuntimeStepContext context)
        {
        }

        public bool ConsumeStop(TimelineRuntimeStopRequest request) => true;

        public void CommitStop(TimelineRuntimeStopRequest request)
        {
        }

        public void DiscardStop(TimelineRuntimeStopRequest request)
        {
        }
    }

    public readonly struct CharacterTimelinePendingAdvance
    {
        internal CharacterTimelinePendingAdvance(
            TimelinePlaybackHandle handle,
            int runtimeHandle,
            TimelineRuntimeAdvanceResult result,
            AbilityTimelineRuntimeStatus status,
            ulong sequence)
        {
            Handle = handle;
            Result = result;
            Status = status;
            AbilityTimelineProgress progress = !result.IsValid ? default : new AbilityTimelineProgress(
                result.ContentIdentity,
                result.ContentRevision,
                result.Generation,
                result.LogicTick,
                result.Duration,
                result.PreviousTime,
                result.Time,
                result.PreviousCycle,
                result.Cycle,
                result.PlaybackMode == TimelinePlaybackMode.Loop,
                result.Completes ? AbilityTimelineProgressState.Completed : AbilityTimelineProgressState.Active, result.Control);
            Pending = result.IsValid ? new AbilityTimelineAdvancePending(runtimeHandle, sequence, progress) : default;
        }

        public TimelinePlaybackHandle Handle { get; }
        public AbilityTimelineRuntimeStatus Status { get; }
        public AbilityTimelineAdvancePending Pending { get; }
        internal TimelineRuntimeAdvanceResult Result { get; }
    }
    public readonly struct CharacterTimelinePendingStop
    {
        internal CharacterTimelinePendingStop(
            TimelinePlaybackHandle handle,
            int runtimeHandle,
            TimelineRuntimeStopRequest request,
            AbilityTimelineRuntimeStatus status,
            ulong sequence)
        {
            Handle = handle;
            Request = request;
            Status = status;
            TimelineRuntimePlayback playback = request.Playback;
            AbilityTimelineProgress progress = playback == null || request.Reason.LocalLogicTick == 0 ? default : new AbilityTimelineProgress(
                playback.Content.Identity, playback.ContentRevision, playback.Generation, request.Reason.LocalLogicTick,
                playback.Content.Duration, playback.CursorTime, playback.CursorTime, playback.Cycle, playback.Cycle,
                playback.PlaybackMode == TimelinePlaybackMode.Loop, AbilityTimelineProgressState.Stopped, playback.Control);
            Pending = playback != null ? new AbilityTimelineStopPending(runtimeHandle, sequence, progress) : default;
        }

        public TimelinePlaybackHandle Handle { get; }
        public AbilityTimelineStopPending Pending { get; }
        internal TimelineRuntimeStopRequest Request { get; }
        public AbilityTimelineRuntimeStatus Status { get; }
    }
    internal readonly struct TimelinePresentationExecutionContext
    {
        public TimelinePresentationExecutionContext(
            ulong actionInstanceId,
            SimulationTick tick,
            ActivationId activation)
        {
            ActionInstanceId = actionInstanceId;
            Tick = tick;
            Activation = activation;
        }

        public ulong ActionInstanceId { get; }
        public SimulationTick Tick { get; }
        public ActivationId Activation { get; }
    }

    public sealed class CharacterTimelineHost : ITimelinePlaybackService, IDisposable
    {
        struct ActivePlayback
        {
            internal TimelinePlaybackHandle Handle;
            internal ulong Generation;
            internal TimelineData Timeline;
            internal string SourceName;
            internal CharacterTimelinePlaybackSourceKind SourceKind;
            internal bool CoreDriven;
            internal TimelinePlaybackActionContext ActionContext;
            internal ulong ActionInstanceId;
            internal RuntimeInstanceKey RuntimeInstance;
            internal RuntimeTimelinePlaybackProvenance Provenance;
            internal bool TerminalPublished;
            internal bool PresentationWithdrawn;
            internal bool LogicOwnerReleased;
            internal bool RestoredReleasedOwner;
            internal ulong LogicReleaseTick;
            internal bool PresentationReleased;
            internal bool CreatedDiagnosticSnapshot;
            internal TimelineRuntimePresentationSample LocalPresentationSample;
        }

        TimelineRuntimeCompositionHost m_Host;
        CharacterTimelineTreeClipService m_TreeClipService;
        readonly List<ActivePlayback> m_ActivePlaybacks = new List<ActivePlayback>();
        readonly List<TimelineRuntimePresentationFrame> m_PresentationCandidates = new List<TimelineRuntimePresentationFrame>(64);
        readonly List<(ActivePlayback Playback, TimelinePresentationSampleReason Reason, bool RetainForCorrection)> m_PresentationEndCandidates = new(64);
        readonly Dictionary<string, TimelineData> m_TimelineContent = new Dictionary<string, TimelineData>(StringComparer.Ordinal);
        readonly Guid m_ContentSessionIdentity = Guid.NewGuid();
        static long s_NextPendingSequence;
        readonly Dictionary<ulong, CharacterTimelinePendingAdvance> m_PendingAdvances =
            new Dictionary<ulong, CharacterTimelinePendingAdvance>();
        readonly Dictionary<ulong, CharacterTimelinePendingStop> m_PendingStops =
            new Dictionary<ulong, CharacterTimelinePendingStop>();
        readonly string m_SourceName;
        readonly List<Float32PresentationGraphRuntime> m_PresentationGraphs = new();
        Float32PresentationGraphFacts m_PresentationFacts;
        readonly CharacterTimelineDependencyResolver m_DependencyResolver = new();
        TimelineRuntimeNumericTarget m_NumericTarget;
        TimelineAsset[] m_AuthoringTimelineContent = Array.Empty<TimelineAsset>();
        internal ThirdPersonSimulation.IAbilityTreeClipInvoker m_ActiveTreeClipInvoker;
        RuntimeDiagnosticsContext m_Diagnostics;
        int m_TickRate;
        ulong m_ContentGeneration;
        bool m_Initialized;

        public CharacterTimelineHost(string sourceName)
        {
            m_SourceName = string.IsNullOrWhiteSpace(sourceName)
                ? "character-timeline"
                : sourceName.Trim();
        }

        public void PushTreeClipInvoker(ThirdPersonSimulation.IAbilityTreeClipInvoker invoker)
        {
            if (invoker == null)
                throw new ArgumentNullException(nameof(invoker));
            if (m_ActiveTreeClipInvoker != null)
                throw new InvalidOperationException("Timeline TreeClip invoker is already active.");
            m_ActiveTreeClipInvoker = invoker;
        }

        public void PopTreeClipInvoker() => m_ActiveTreeClipInvoker = null;

        public void InstallAbilitySources(IReadOnlyList<ProgramSourceMapEntry> sources) =>
            m_DependencyResolver.InstallGraphSources(sources);

        public void InstallPresentationPrograms(IReadOnlyList<Float32GameplayAbilityExecutionData> programs)
        {
            if (m_Initialized)
                throw new InvalidOperationException("Presentation graphs must be installed before Timeline preparation.");
            for (int programIndex = 0; programIndex < programs.Count; programIndex++)
            {
                Float32GameplayAbilityExecutionData data = programs[programIndex];
                bool hasMarkers = false;
                for (int index = 0; index < data.SourceMap.Count; index++)
                    hasMarkers |= data.SourceMap[index].TargetKind == ProgramSourceTargetKind.GraphInvocation &&
                        data.SourceMap[index].InvocationCallerKind == ProgramInvocationCallerKind.PresentationMarker;
                if (!hasMarkers)
                    continue;
                var runtime = new Float32PresentationGraphRuntime(data);
                m_DependencyResolver.InstallGraphSources(data.SourceMap);
                m_PresentationGraphs.Add(runtime);
                for (int index = 0; index < data.SourceMap.Count; index++)
                    if (data.SourceMap[index].InvocationCallerKind == ProgramInvocationCallerKind.PresentationMarker)
                        m_DependencyResolver.InstallPresentationGraph(data.SourceMap[index].GraphId);
            }
        }

        internal TimelineRuntimeCompositionHost Host => m_Host;
        public bool IsInitialized => m_Initialized && m_Host != null;
        public string AuthoringContentRevision { get; private set; } = string.Empty;
        public string ContentRevision { get; private set; } = string.Empty;
        public ulong ContentGeneration => m_ContentGeneration;
        internal event Action<TimelineRuntimePresentationFrame> PresentationFramePrepared;
        public event Action<TimelineRuntimePresentationFrame> PresentationFrameProduced;
        internal event Action<TimelineRuntimePlaybackHandle, ulong, TimelinePresentationSampleReason, bool> PresentationPlaybackEndPrepared;
        public event Action<TimelineRuntimePlaybackHandle, ulong, TimelinePresentationSampleReason> PresentationPlaybackEnded;

        public void AttachRuntimeDiagnostics(RuntimeDiagnosticsContext diagnostics)
        {
            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));
            if (m_Diagnostics != null && !ReferenceEquals(m_Diagnostics, diagnostics))
                throw new InvalidOperationException("CharacterTimelineHost already belongs to another RuntimeDiagnosticsContext.");
            m_Diagnostics = diagnostics;
        }

        internal void Initialize(TimelineRuntimeNumericTarget numericTarget, int tickRate)
        {
            if (m_Initialized)
                return;
            if (!Enum.IsDefined(typeof(TimelineRuntimeNumericTarget), numericTarget))
                throw new ArgumentOutOfRangeException(nameof(numericTarget));
            m_NumericTarget = numericTarget;
            m_TickRate = tickRate;

            TimelineContractCatalog contractCatalog = TimelineTreeContractComposition.Create();
            var callBindingSource = new TimelineRuntimeCallBindingSource(
                m_SourceName, "character-timeline", default);
            var domainResolver = new CharacterTimelineDomainBindingResolver(
                new[] { "self", "camera", "main" });
            m_TreeClipService = new CharacterTimelineTreeClipService(this);
            var markerService = new CharacterTimelineMarkerService(this);

            m_Host = new TimelineRuntimeCompositionHost(
                contractCatalog,
                m_NumericTarget,
                callBindingSource,
                domainResolver,
                m_DependencyResolver,
                Array.Empty<ITimelineRuntimeEvaluationSink>(),
                m_TreeClipService,
                markerService,
                tickRate);
            m_Host.CommittedEvaluation += OnCommittedTimelineEvaluation;
            m_Host.StopCommitted += OnTimelineStopCommitted;
            m_Initialized = true;
        }

        public bool RequestTimelinePlayback(
            TimelineData timeline,
            string sourceId,
            string sourceName,
            TimelinePlaybackActionContext actionContext,
            TimelinePlaybackMode playbackMode,
            TreeExecutionActivationScope sourceActivation,
            BaseGraph sourceRuntimeGraph,
            out TimelinePlaybackHandle handle)
        {
            return RequestTimelinePlayback(
                timeline,
                sourceId,
                sourceName,
                actionContext,
                playbackMode,
                sourceActivation,
                sourceRuntimeGraph,
                CharacterTimelinePlaybackSourceKind.None,
                -1,
                CreatePlaybackProvenance(sourceId, sourceName, sourceActivation, sourceRuntimeGraph),
                out handle);
        }

        bool RequestTimelinePlayback(
            TimelineData timeline,
            string sourceId,
            string sourceName,
            TimelinePlaybackActionContext actionContext,
            TimelinePlaybackMode playbackMode,
            TreeExecutionActivationScope sourceActivation,
            BaseGraph sourceRuntimeGraph,
            CharacterTimelinePlaybackSourceKind sourceKind,
            int sourceOperationIndex,
            RuntimeTimelinePlaybackProvenance provenance,
            out TimelinePlaybackHandle handle)
        {
            if (!m_Initialized || m_Host == null)
            {
                handle = TimelinePlaybackHandle.Invalid;
                return false;
            }
            TimelineData playbackTimeline = timeline?.Clone();
            if (!m_Host.RequestTimelinePlayback(
                playbackTimeline, sourceId, sourceName, actionContext, playbackMode,
                sourceActivation, sourceRuntimeGraph, out handle))
                return false;
            var runtimeHandle = new TimelineRuntimePlaybackHandle(handle.Value);
            bool accepted = false;
            try
            {
                if (!m_Host.Service.TryGetDescriptor(handle, out TimelineRuntimePlaybackDescriptor descriptor))
                    throw new InvalidOperationException("Started Timeline playback has no descriptor.");
                var active = new ActivePlayback
                {
                    Handle = handle,
                    Generation = descriptor.Generation,
                    Timeline = playbackTimeline,
                    SourceName = sourceName ?? string.Empty,
                    SourceKind = sourceKind,
                    CoreDriven = false,
                    ActionContext = actionContext,
                    ActionInstanceId = actionContext.ActionInstanceId,
                    RuntimeInstance = CreateRuntimeInstance(handle, actionContext.ActionInstanceId, sourceOperationIndex),
                    Provenance = provenance
                };
                m_ActivePlaybacks.Add(active);
                PublishPlaybackSnapshot(active);
                PublishTimelineLifecycle(active, RuntimeTraceEventKind.TimelineRequested, "Requested", string.Empty);
                PublishTimelineLifecycle(active, RuntimeTraceEventKind.TimelineStarted, "Running", string.Empty);
                accepted = true;
                return true;
            }
            finally
            {
                if (!accepted)
                {
                    handle = TimelinePlaybackHandle.Invalid;
                    for (int index = m_ActivePlaybacks.Count - 1; index >= 0; index--)
                    {
                        if (m_ActivePlaybacks[index].Handle.Value != runtimeHandle.Value)
                            continue;
                        if (m_ActivePlaybacks[index].CreatedDiagnosticSnapshot)
                            TimelineRuntimePlaybackSnapshotRegistry.Remove(m_ActivePlaybacks[index].RuntimeInstance);
                        m_ActivePlaybacks.RemoveAt(index);
                    }
                    m_Host.ReleasePlayback(runtimeHandle);
                }
            }
        }

        public void SetTimelineContent(IReadOnlyList<TimelineAsset> timelines)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline content requires an initialized CharacterTimelineHost.");
            if (timelines == null)
                throw new ArgumentNullException(nameof(timelines));
            if (!TryComputeContentRevisions(timelines, out string authoringRevision, out string contentRevision, out List<string> errors))
                throw new InvalidOperationException(string.Join(" | ", errors));
            if (!TryFreezeContent(timelines, out List<CharacterTimelineContentSnapshot> snapshots, out errors))
                throw new InvalidOperationException(string.Join(" | ", errors));
            m_AuthoringTimelineContent = timelines.ToArray();
            InstallTimelineContent(snapshots, authoringRevision, contentRevision);
        }

        void InstallTimelineContent(
            IReadOnlyList<CharacterTimelineContentSnapshot> snapshots,
            string authoringRevision,
            string contentRevision)
        {
            if (snapshots == null || snapshots.Count == 0)
                throw new ArgumentException("Timeline content snapshots are required.", nameof(snapshots));
            m_TimelineContent.Clear();
            for (int i = 0; i < snapshots.Count; i++)
            {
                CharacterTimelineContentSnapshot snapshot = snapshots[i];
                TimelineData timeline = snapshot?.CloneData();
                if (timeline == null)
                    throw new InvalidOperationException("Timeline content snapshot is invalid.");
                if (!m_TimelineContent.TryAdd(timeline.AuthoringId, timeline))
                    throw new InvalidOperationException($"Timeline content identity '{timeline.AuthoringId}' is duplicated.");
            }
            AuthoringContentRevision = authoringRevision ?? string.Empty;
            m_DependencyResolver.InstallContent(m_TimelineContent.Values, m_NumericTarget);
            ContentRevision = contentRevision ?? string.Empty;
            m_ContentGeneration = checked(m_ContentGeneration + 1);
        }

        public bool TryExportContent(
            out CharacterTimelineContentExport export,
            out string error)
        {
            export = null;
            error = string.Empty;
            if (!IsInitialized)
            {
                error = "Timeline content export requires an initialized CharacterTimelineHost.";
                return false;
            }
            if (!TryGetCurrentContentRevisions(
                    out string authoringRevision,
                    out string contentRevision,
                    out List<string> errors))
            {
                error = string.Join(" | ", errors);
                return false;
            }
            if (!TryFreezeContent(m_AuthoringTimelineContent, out List<CharacterTimelineContentSnapshot> snapshots, out errors))
            {
                error = string.Join(" | ", errors);
                return false;
            }
            export = new CharacterTimelineContentExport(snapshots, authoringRevision, contentRevision);
            return true;
        }

        public bool TryPrepareContentAdoption(
            CharacterTimelineContentExport export,
            out CharacterTimelineContentAdoptionPlan plan,
            out string error)
        {
            plan = null;
            error = string.Empty;
            if (!IsInitialized)
            {
                error = "Timeline content adoption requires an initialized CharacterTimelineHost.";
                return false;
            }
            if (!TryValidateCurrentExport(export, out error))
                return false;
            if (!HasCompatibleContentTopology(export.Snapshots))
            {
                error = "Timeline content topology or contract changed; a new Session is required.";
                return false;
            }
            plan = new CharacterTimelineContentAdoptionPlan(
                export,
                m_ContentSessionIdentity,
                m_ContentGeneration,
                true,
                string.Equals(ContentRevision, export.ContentRevision, StringComparison.Ordinal)
                    ? "Timeline content is already adopted."
                    : "Timeline content is prepared for adoption at the next formal playback boundary.");
            return true;
        }

        public bool TryAdoptContent(
            CharacterTimelineContentPublication publication,
            out CharacterTimelineContentAdoptionReport report)
        {
            CharacterTimelineContentAdoptionPlan plan = publication?.Plan;
            if (!IsInitialized)
            {
                report = new CharacterTimelineContentAdoptionReport(
                    CharacterTimelineContentAdoptionState.Rejected,
                    publication?.AuthoringRevision,
                    publication?.ContentRevision,
                    "Timeline content adoption requires an initialized CharacterTimelineHost.");
                return false;
            }
            if (publication == null || !publication.IsValid)
            {
                report = new CharacterTimelineContentAdoptionReport(
                    CharacterTimelineContentAdoptionState.Rejected,
                    publication?.AuthoringRevision,
                    publication?.ContentRevision,
                    "Timeline content publication is invalid.");
                return false;
            }
            if (plan == null || !plan.IsValid || !plan.IsCompatible)
            {
                report = new CharacterTimelineContentAdoptionReport(
                    CharacterTimelineContentAdoptionState.Rejected,
                    plan?.AuthoringRevision,
                    plan?.ContentRevision,
                    "Timeline content adoption plan is invalid or incompatible.");
                return false;
            }
            if (!TryValidateCurrentPlan(plan, out string validationError))
            {
                report = new CharacterTimelineContentAdoptionReport(
                    CharacterTimelineContentAdoptionState.Rejected,
                    plan.AuthoringRevision,
                    plan.ContentRevision,
                    validationError);
                return false;
            }
            if (!HasCompatibleContentTopology(plan.Snapshots))
            {
                report = new CharacterTimelineContentAdoptionReport(
                    CharacterTimelineContentAdoptionState.Rejected,
                    plan.AuthoringRevision,
                    plan.ContentRevision,
                    "Timeline content topology or contract changed after preparation; a new Session is required.");
                return false;
            }
            InstallTimelineContent(plan.Snapshots, plan.AuthoringRevision, plan.ContentRevision);
            report = new CharacterTimelineContentAdoptionReport(
                CharacterTimelineContentAdoptionState.Adopted,
                AuthoringContentRevision,
                ContentRevision,
                "Timeline content adopted for future formal playback activations.");
            return true;
        }

        public bool TryPublishContentAdoption(
            CharacterTimelineContentAdoptionPlan plan,
            out CharacterTimelineContentPublication publication,
            out string error)
        {
            publication = null;
            error = string.Empty;
            if (!IsInitialized)
            {
                error = "Timeline content publication requires an initialized CharacterTimelineHost.";
                return false;
            }
            if (plan == null || !plan.IsValid || !plan.IsCompatible)
            {
                error = "Timeline content adoption plan is invalid or incompatible.";
                return false;
            }
            if (!TryValidateCurrentPlan(plan, out error))
                return false;
            if (!HasCompatibleContentTopology(plan.Snapshots))
            {
                error = "Timeline content topology or contract changed; a new Session is required.";
                return false;
            }
            publication = new CharacterTimelineContentPublication(
                plan,
                "Timeline content publication is sealed for the next formal adoption boundary.");
            return true;
        }

        bool TryValidateCurrentPlan(
            CharacterTimelineContentAdoptionPlan plan,
            out string error)
        {
            if (plan == null || !plan.IsValid || !plan.IsCompatible)
            {
                error = "Timeline content adoption plan is invalid or incompatible.";
                return false;
            }
            if (plan.SessionIdentity != m_ContentSessionIdentity)
            {
                error = "Timeline content adoption plan belongs to another Session.";
                return false;
            }
            if (plan.SessionContentGeneration != m_ContentGeneration)
            {
                error = "Timeline content changed after preparation; export again before adoption.";
                return false;
            }
            return TryValidateCurrentExport(plan.Export, out error);
        }

        bool TryValidateCurrentExport(
            CharacterTimelineContentExport export,
            out string error)
        {
            if (export == null || !export.IsValid)
            {
                error = "Timeline content export is invalid.";
                return false;
            }
            if (!TryGetCurrentContentRevisions(
                    out string authoringRevision,
                    out string contentRevision,
                    out List<string> errors))
            {
                error = string.Join(" | ", errors);
                return false;
            }
            if (!string.Equals(export.AuthoringRevision, authoringRevision, StringComparison.Ordinal) ||
                !string.Equals(export.ContentRevision, contentRevision, StringComparison.Ordinal))
            {
                error = "Timeline authoring changed after export; export again before preparing or adopting.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        bool HasCompatibleContentTopology(IReadOnlyList<CharacterTimelineContentSnapshot> snapshots)
        {
            if (snapshots == null || snapshots.Count != m_TimelineContent.Count)
                return false;
            for (int i = 0; i < snapshots.Count; i++)
            {
                CharacterTimelineContentSnapshot snapshot = snapshots[i];
                if (snapshot == null ||
                    !m_TimelineContent.TryGetValue(snapshot.TimelineAuthoringId, out TimelineData installed) ||
                    !HasSameTopology(installed, snapshot.CloneData()))
                    return false;
            }
            return true;
        }

        static bool HasSameTopology(TimelineData installed, TimelineData candidate)
        {
            if (installed == null || candidate == null || installed.Tracks.Count != candidate.Tracks.Count)
                return false;
            for (int trackIndex = 0; trackIndex < installed.Tracks.Count; trackIndex++)
            {
                Track installedTrack = installed.Tracks[trackIndex];
                Track candidateTrack = candidate.Tracks[trackIndex];
                if (installedTrack == null || candidateTrack == null ||
                    !string.Equals(installedTrack.AuthoringId, candidateTrack.AuthoringId, StringComparison.Ordinal) ||
                    !string.Equals(installedTrack.ContractKind, candidateTrack.ContractKind, StringComparison.Ordinal) ||
                    installedTrack.Clips.Count != candidateTrack.Clips.Count)
                    return false;
                for (int clipIndex = 0; clipIndex < installedTrack.Clips.Count; clipIndex++)
                {
                    Clip installedClip = installedTrack.Clips[clipIndex];
                    Clip candidateClip = candidateTrack.Clips[clipIndex];
                    if (installedClip == null || candidateClip == null ||
                        !string.Equals(installedClip.AuthoringId, candidateClip.AuthoringId, StringComparison.Ordinal) ||
                        !string.Equals(installedClip.ContractKind, candidateClip.ContractKind, StringComparison.Ordinal))
                        return false;
                }
            }
            return true;
        }

        public bool TryGetCurrentAuthoringContentRevision(
            out string authoringRevision,
            out string error)
        {
            if (TryGetCurrentContentRevisions(out authoringRevision, out _, out List<string> errors))
            {
                error = string.Empty;
                return true;
            }
            error = string.Join(" | ", errors);
            return false;
        }

        bool TryGetCurrentContentRevisions(
            out string authoringRevision,
            out string contentRevision,
            out List<string> errors)
        {
            return TryComputeContentRevisions(
                m_AuthoringTimelineContent,
                out authoringRevision,
                out contentRevision,
                out errors);
        }

        static bool TryFreezeContent(
            IReadOnlyList<TimelineAsset> timelines,
            out List<CharacterTimelineContentSnapshot> snapshots,
            out List<string> errors)
        {
            snapshots = new List<CharacterTimelineContentSnapshot>();
            errors = new List<string>();
            if (timelines == null || timelines.Count == 0)
            {
                errors.Add("Timeline content list is empty.");
                return false;
            }
            var identities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < timelines.Count; i++)
            {
                TimelineAsset asset = timelines[i];
                if (!asset || asset.Data == null)
                {
                    errors.Add($"Timeline content #{i} is missing.");
                    continue;
                }
                if (!identities.Add(asset.Data.AuthoringId))
                {
                    errors.Add($"Timeline content identity '{asset.Data.AuthoringId}' is duplicated.");
                    continue;
                }
                snapshots.Add(new CharacterTimelineContentSnapshot(asset.Data));
            }
            return errors.Count == 0;
        }

        static bool TryComputeContentRevisions(
            IReadOnlyList<TimelineAsset> timelines,
            out string authoringRevision,
            out string contentRevision,
            out List<string> errors)
        {
            authoringRevision = string.Empty;
            contentRevision = string.Empty;
            errors = new List<string>();
            if (timelines == null || timelines.Count == 0)
            {
                errors.Add("Timeline content list is empty.");
                return false;
            }
            TimelineContractCatalog catalog = TimelineTreeContractComposition.Create();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var authoringParts = new List<string>(timelines.Count);
            var contentParts = new List<string>(timelines.Count);
            for (int i = 0; i < timelines.Count; i++)
            {
                TimelineAsset asset = timelines[i];
                if (!asset || asset.Data == null)
                {
                    errors.Add($"Timeline content #{i} is missing.");
                    continue;
                }
                if (!identities.Add(asset.Data.AuthoringId))
                {
                    errors.Add($"Timeline content identity '{asset.Data.AuthoringId}' is duplicated.");
                    continue;
                }
                TimelineContentDiscoveryResult discovery = TimelineContentDiscovery.Discover(asset, catalog);
                if (!discovery.IsValid)
                {
                    for (int errorIndex = 0; errorIndex < discovery.Errors.Count; errorIndex++)
                        errors.Add($"{asset.name}: {discovery.Errors[errorIndex]}");
                    continue;
                }
                authoringParts.Add($"{asset.Data.AuthoringId}:{TimelineAuthoringFingerprint.Compute(asset.Data)}");
                contentParts.Add($"{asset.Data.AuthoringId}:{discovery.Content.ContentHash}");
            }
            if (errors.Count != 0)
                return false;
            authoringRevision = SourceContentHasher.Hash(authoringParts.ToArray());
            contentRevision = SourceContentHasher.Hash(contentParts.ToArray());
            return true;
        }

        public bool RequestAbilityTimelinePlayback(
            string timelineId,
            TimelineActionContextIdentity actionContext,
            bool loop,
            AbilityTimelineInvocationSource invocationSource,
            ulong inputSequence,
            SimulationTick tick,
            out TimelinePlaybackHandle handle)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Ability Timeline requires an initialized CharacterTimelineHost.");
            if (!actionContext.IsValid || !actionContext.HasSkillExecution || actionContext.SkillExecutionGeneration == 0)
                throw new ArgumentException("Ability Timeline Action context is invalid.", nameof(actionContext));
            if (!invocationSource.IsValid)
                throw new ArgumentException("Ability Timeline invocation source is invalid.", nameof(invocationSource));
            if (!tick.IsValid)
                throw new ArgumentOutOfRangeException(nameof(tick));
            if (!m_TimelineContent.TryGetValue(timelineId, out TimelineData timeline))
                throw new KeyNotFoundException($"Ability Timeline content '{timelineId}' is not installed.");
            var playbackActionContext = new TimelinePlaybackActionContext(
                actionContext.InstanceId,
                actionContext.ActionId,
                actionContext.PredictionKey,
                inputSequence,
                tick.Value);
            bool requested = RequestTimelinePlayback(
                timeline,
                actionContext.ActionId,
                timeline.Name,
                playbackActionContext,
                loop ? TimelinePlaybackMode.Loop : TimelinePlaybackMode.Once,
                default,
                null,
                CharacterTimelinePlaybackSourceKind.AbilityRuntime,
                invocationSource.OperationIndex,
                CreateAbilityPlaybackProvenance(invocationSource, actionContext, timeline.Name),
                out handle);
            if (!requested)
                throw new InvalidOperationException($"Ability Timeline '{timelineId}' could not start: {m_Host.Service.LastFailure}");
            if (requested)
            {
                for (int i = 0; i < m_ActivePlaybacks.Count; i++)
                {
                    if (m_ActivePlaybacks[i].Handle.Value == handle.Value)
                    {
                        ActivePlayback updated = m_ActivePlaybacks[i];
                        updated.SourceKind = CharacterTimelinePlaybackSourceKind.AbilityRuntime;
                        updated.CoreDriven = true;
                        m_ActivePlaybacks[i] = updated;
                    }
                }
            }
            return requested;
        }

        public CharacterTimelinePendingAdvance AdvanceTimelinePlayback(
            TimelinePlaybackHandle handle,
            ulong logicTick,
            int tickCount,
            AbilityTimelinePlaybackControl control)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline advancement requires an initialized CharacterTimelineHost.");
            TimelinePlaybackStatus status = m_Host.Service.GetTimelinePlaybackStatus(handle);
            if (status != TimelinePlaybackStatus.Requested && status != TimelinePlaybackStatus.Running)
                return new CharacterTimelinePendingAdvance(handle, 0, default, MapTerminalStatus(status), 0);
            TimelineRuntimeAdvanceResult advance = m_Host.Advance(new TimelineRuntimePlaybackHandle(handle.Value), logicTick, tickCount, control);
            var pending = new CharacterTimelinePendingAdvance(handle, (int)handle.Value, advance, AbilityTimelineRuntimeStatus.Running, checked((ulong)System.Threading.Interlocked.Increment(ref s_NextPendingSequence)));
            m_PendingAdvances[handle.Value] = pending;
            return pending;
        }

        public void CommitTimelinePlayback(AbilityTimelineAdvancePending pending)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline commit requires an initialized CharacterTimelineHost.");
            if (!pending.IsValid || !m_PendingAdvances.TryGetValue((ulong)pending.RuntimeHandle, out CharacterTimelinePendingAdvance current) ||
                current.Pending.Sequence != pending.Sequence)
                return;
            m_PendingAdvances.Remove((ulong)pending.RuntimeHandle);
            if (!m_Host.CommitAdvance(new TimelineRuntimePlaybackHandle(current.Handle.Value), current.Result))
                throw new InvalidOperationException($"Timeline advance '{current.Handle.Value}' could not be committed.");
        }

        public void DiscardTimelinePlayback(AbilityTimelineAdvancePending pending)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline discard requires an initialized CharacterTimelineHost.");
            if (!pending.IsValid || !m_PendingAdvances.TryGetValue((ulong)pending.RuntimeHandle, out CharacterTimelinePendingAdvance current) ||
                current.Pending.Sequence != pending.Sequence)
                return;
            m_PendingAdvances.Remove((ulong)pending.RuntimeHandle);
            if (!m_Host.DiscardAdvance(new TimelineRuntimePlaybackHandle(current.Handle.Value), current.Result))
                throw new InvalidOperationException($"Timeline advance '{current.Handle.Value}' could not be discarded.");
        }


        internal void CopyPendingTimelineMotion(int runtimeHandle, List<AbilityTimelineLogicMotion> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));
            results.Clear();
            if (!m_PendingAdvances.TryGetValue((ulong)runtimeHandle, out CharacterTimelinePendingAdvance pending) ||
                !pending.Result.IsValid)
                return;
            ActivePlayback active = default;
            for (int index = 0; index < m_ActivePlaybacks.Count; index++)
            {
                if (m_ActivePlaybacks[index].Handle.Value == pending.Handle.Value)
                {
                    active = m_ActivePlaybacks[index];
                    break;
                }
            }
            if (active.Handle.Value != pending.Handle.Value || !active.Provenance.HasProgramInvocation)
                throw new InvalidOperationException($"Timeline motion '{pending.Handle.Value}' has no Ability invocation provenance.");
            var source = SimulationExecutionSource.FromSkillOperation(
                new OperationHandle(active.Provenance.SourceOperationIndex),
                active.Provenance.SourceInvocationPath);
            TimelineRuntimeSampleView<TimelineMotionCurveContribution> contributions =
                pending.Result.Evaluation.MotionContributions;
            for (int index = 0; index < contributions.Count; index++)
            {
                TimelineMotionCurveContribution contribution = contributions[index];
                results.Add(new AbilityTimelineLogicMotion(
                    source,
                    new CharacterSkillId(active.ActionContext.ActionId),
                    FixedScalar.FromSingle(contribution.Displacement.x),
                    FixedScalar.FromSingle(contribution.Displacement.y),
                    FixedScalar.FromSingle(contribution.Displacement.z),
                    FixedScalar.FromSingle(contribution.YawDegrees),
                    FixedScalar.FromSingle(contribution.Weight),
                    contribution.Priority,
                    MapMotionSpace(contribution.Space),
                    MapMotionChannel(contribution.Channel),
                    MapMotionBlendMode(contribution.BlendMode),
                    contribution.ConsumeLowerChannels));
            }
        }

        static AbilityTimelineMotionChannel MapMotionChannel(TimelineMotionChannel channel)
        {
            return channel switch
            {
                TimelineMotionChannel.Action => AbilityTimelineMotionChannel.Action,
                TimelineMotionChannel.GameplayResult => AbilityTimelineMotionChannel.GameplayResult,
                _ => throw new InvalidOperationException($"Timeline locomotion output cannot enter the Ability motion domain: {channel}.")
            };
        }

        static AbilityTimelineMotionSpace MapMotionSpace(TimelineMotionContributionSpace space)
        {
            return space switch
            {
                TimelineMotionContributionSpace.Local => AbilityTimelineMotionSpace.ActorLocal,
                TimelineMotionContributionSpace.World => AbilityTimelineMotionSpace.World,
                _ => throw new InvalidOperationException($"Timeline motion space '{space}' is not supported.")
            };
        }

        static AbilityTimelineMotionBlendMode MapMotionBlendMode(TimelineMotionBlendMode mode) =>
            (AbilityTimelineMotionBlendMode)mode;

        internal bool IsAbilityRuntimePlayback(TimelineRuntimePlaybackHandle handle)
        {
            for (int i = 0; i < m_ActivePlaybacks.Count; i++)
            {
                if (m_ActivePlaybacks[i].Handle.Value == handle.Value &&
                    m_ActivePlaybacks[i].SourceKind == CharacterTimelinePlaybackSourceKind.AbilityRuntime)
                    return true;
            }
            return false;
        }

        internal ulong RequireAbilityPlaybackActionInstanceId(TimelineRuntimePlaybackHandle handle)
        {
            for (int i = 0; i < m_ActivePlaybacks.Count; i++)
            {
                if (m_ActivePlaybacks[i].Handle.Value != handle.Value)
                    continue;
                if (m_ActivePlaybacks[i].ActionInstanceId != 0)
                    return m_ActivePlaybacks[i].ActionInstanceId;
                break;
            }
            throw new InvalidOperationException($"Timeline playback '{handle.Value}' has no Action instance identity.");
        }

        internal bool TryGetPlaybackActionContext(
            TimelineRuntimePlaybackHandle handle,
            out TimelinePlaybackActionContext actionContext)
        {
            for (int i = 0; i < m_ActivePlaybacks.Count; i++)
            {
                ActivePlayback active = m_ActivePlaybacks[i];
                if (active.Handle.Value == handle.Value && active.ActionContext.IsValid)
                {
                    actionContext = active.ActionContext;
                    return true;
                }
            }
            actionContext = default;
            return false;
        }

        internal bool TryGetPresentationExecutionContext(
            TimelineRuntimePlaybackHandle handle,
            out TimelinePresentationExecutionContext context)
        {
            for (int index = 0; index < m_ActivePlaybacks.Count; index++)
            {
                ActivePlayback active = m_ActivePlaybacks[index];
                if (active.Handle.Value != handle.Value ||
                    active.SourceKind != CharacterTimelinePlaybackSourceKind.AbilityRuntime ||
                    !active.Provenance.HasProgramInvocation)
                {
                    continue;
                }

                ulong tick = active.ActionContext.StartLocalLogicTick;
                if (tick == 0)
                {
                    context = default;
                    return false;
                }

                var source = SimulationExecutionSource.FromSkillOperation(
                    new OperationHandle(active.Provenance.SourceOperationIndex),
                    active.Provenance.SourceInvocationPath);
                context = new TimelinePresentationExecutionContext(
                    active.ActionContext.ActionInstanceId,
                    new SimulationTick(tick),
                    new ActivationId(source, active.Provenance.SkillExecutionGeneration));
                return true;
            }

            context = default;
            return false;
        }

        public TimelinePlaybackStatus GetTimelinePlaybackStatus(TimelinePlaybackHandle handle)
        {
            if (!m_Initialized || m_Host == null || !handle.IsValid)
                return TimelinePlaybackStatus.None;
            return m_Host.Service.GetTimelinePlaybackStatus(handle);
        }

        public CharacterTimelinePendingStop RequestStopTimelinePlayback(
            TimelinePlaybackHandle handle,
            TimelinePlaybackStopContext stopContext)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline stop requires an initialized CharacterTimelineHost.");
            TimelinePlaybackStatus status = m_Host.Service.GetTimelinePlaybackStatus(handle);
            if (status != TimelinePlaybackStatus.Requested && status != TimelinePlaybackStatus.Running)
                return new CharacterTimelinePendingStop(handle, 0, default, MapTerminalStatus(status), 0);
            if (!m_Host.Service.RequestStopTimelinePlayback(
                    handle,
                    stopContext,
                    out TimelineRuntimeStopRequest request))
                throw new InvalidOperationException($"Timeline stop '{handle.Value}' was rejected.");
            var pending = new CharacterTimelinePendingStop(handle, (int)handle.Value, request, AbilityTimelineRuntimeStatus.Running,
                checked((ulong)System.Threading.Interlocked.Increment(ref s_NextPendingSequence)));
            m_PendingStops[handle.Value] = pending;
            return pending;
        }

        public void CommitStopTimelinePlayback(AbilityTimelineStopPending pending)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline stop commit requires an initialized CharacterTimelineHost.");
            if (!pending.IsValid || !m_PendingStops.TryGetValue((ulong)pending.RuntimeHandle, out CharacterTimelinePendingStop current) ||
                current.Pending.Sequence != pending.Sequence)
                return;
            m_PendingStops.Remove((ulong)pending.RuntimeHandle);
            if (!m_Host.Service.CommitStopTimelinePlayback(current.Handle, current.Request))
                throw new InvalidOperationException($"Timeline stop '{current.Handle.Value}' could not be committed.");
        }

        public void DiscardStopTimelinePlayback(AbilityTimelineStopPending pending)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline stop discard requires an initialized CharacterTimelineHost.");
            if (!pending.IsValid || !m_PendingStops.TryGetValue((ulong)pending.RuntimeHandle, out CharacterTimelinePendingStop current) ||
                current.Pending.Sequence != pending.Sequence)
                return;
            m_PendingStops.Remove((ulong)pending.RuntimeHandle);
            if (!m_Host.Service.DiscardStopTimelinePlayback(current.Handle, current.Request))
                throw new InvalidOperationException($"Timeline stop '{current.Handle.Value}' could not be discarded.");
        }
        public bool RequestAbilityTreeClipExit(int runtimeHandle, string clipAuthoringId)
        {
            if (!IsInitialized)
                return false;
            return m_Host.Service.RequestTreeClipExit(
                new TimelineRuntimePlaybackHandle((ulong)runtimeHandle),
                clipAuthoringId);
        }

        public void DiscardUnpublishedTimelinePlayback(TimelinePlaybackHandle playbackHandle)
        {
            var handle = new TimelineRuntimePlaybackHandle(playbackHandle.Value);
            for (int index = 0; index < m_ActivePlaybacks.Count; index++)
            {
                ActivePlayback active = m_ActivePlaybacks[index];
                if (active.Handle.Value != handle.Value)
                    continue;
                if (active.RestoredReleasedOwner)
                {
                    m_Host.DiscardEvaluation(handle);
                    m_TreeClipService.Restore(handle.Value, default);
                    active.LogicOwnerReleased = true;
                    active.RestoredReleasedOwner = false;
                    m_ActivePlaybacks[index] = active;
                    return;
                }
                m_Host.ReleasePlayback(handle);
                m_Host.ReleasePresentationPlayback(handle, active.Generation);
                m_TreeClipService.Restore(handle.Value, default);
                if (active.CreatedDiagnosticSnapshot)
                    TimelineRuntimePlaybackSnapshotRegistry.Remove(active.RuntimeInstance);
                m_ActivePlaybacks.RemoveAt(index);
                return;
            }
            throw new InvalidOperationException("Unpublished Timeline discard has no registered playback.");
        }

        public void PublishAbilityTimelineOwner(int runtimeHandle)
        {
            for (int index = 0; index < m_ActivePlaybacks.Count; index++)
            {
                ActivePlayback active = m_ActivePlaybacks[index];
                if (active.Handle.Value != (ulong)runtimeHandle)
                    continue;
                active.RestoredReleasedOwner = false;
                m_ActivePlaybacks[index] = active;
                return;
            }
            throw new InvalidOperationException("Published Timeline owner has no registered playback.");
        }

        public void ReleaseAbilityTimelineOwner(int runtimeHandle, ulong committedTick)
        {
            var handle = new TimelineRuntimePlaybackHandle(checked((ulong)runtimeHandle));
            for (int index = 0; index < m_ActivePlaybacks.Count; index++)
            {
                ActivePlayback active = m_ActivePlaybacks[index];
                if (active.Handle.Value != handle.Value)
                    continue;
                m_Host.DiscardEvaluation(handle);
                m_TreeClipService.Restore(handle.Value, default);
                active.LogicOwnerReleased = true;
                active.LogicReleaseTick = committedTick;
                m_ActivePlaybacks[index] = active;
                return;
            }
            throw new InvalidOperationException("Timeline logical owner release has no registered playback.");
        }

        void ReleaseConfirmedPlaybacks(ulong confirmedTick)
        {
            for (int index = m_ActivePlaybacks.Count - 1; index >= 0; index--)
            {
                ActivePlayback active = m_ActivePlaybacks[index];
                if (!active.CoreDriven || !active.LogicOwnerReleased || !active.PresentationReleased ||
                    active.LogicReleaseTick > confirmedTick)
                    continue;
                m_Host.ReleasePlayback(new TimelineRuntimePlaybackHandle(active.Handle.Value));
                m_ActivePlaybacks.RemoveAt(index);
            }
        }

        public AbilityTimelineRuntimeSnapshot CaptureAbilityTimelinePlayback(
            TimelinePlaybackHandle handle,
            AbilityTimelineStartRequest request)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline capture requires an initialized CharacterTimelineHost.");
            TimelineRuntimePlaybackSnapshot native = m_Host.Capture(new TimelineRuntimePlaybackHandle(handle.Value));
            return new AbilityTimelineRuntimeSnapshot(
                (int)native.Handle.Value,
                native.Generation,
                native.RequestId,
                native.ExecutionIdentity.OwnerIdentity,
                native.ExecutionIdentity.CallIdentity,
                native.ExecutionIdentity.InstanceId,
                MapSnapshotMode(native.PlaybackMode),
                native.ContentRevision,
                MapSnapshotState(native.State),
                native.CursorTime,
                native.Cycle,
                native.TimeCarry,
                native.Control,
                native.TreeDecisionExits,
                native.PendingTreeDecisionExits,
                native.SectionId,
                native.ActiveClipIds,
                TimelineSnapshotItems<AbilityTimelineTreeClipState>.CopyFrom(m_TreeClipService.Capture(handle.Value)),
                native.HasStopContext,
                MapSnapshotStopCause(native.StopContext.Cause),
                native.StopContext.LocalLogicTick,
                native.InitialBoundaryPending,
                request.TimelineId,
                request.Loop,
                request.ActionContext,
                request.InvocationSource,
                request.InputSequence,
                request.Tick);
        }

        public int ApplyAbilityTimelineSnapshot(AbilityTimelineRuntimeSnapshot snapshot)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline restore requires an initialized CharacterTimelineHost.");
            if (!snapshot.IsValid)
                throw new ArgumentException("Ability Timeline snapshot is invalid.", nameof(snapshot));
            if (!m_TimelineContent.TryGetValue(snapshot.TimelineId, out TimelineData timeline))
                throw new KeyNotFoundException($"Ability Timeline content '{snapshot.TimelineId}' is not installed.");
            var executionIdentity = new TimelineExecutionIdentity(
                snapshot.OwnerIdentity,
                snapshot.CallIdentity,
                snapshot.ExecutionInstanceId);
            if (!m_Host.Service.TryGetPreparation(new TimelineRuntimePlaybackHandle((ulong)snapshot.RuntimeHandle),
                    out TimelineRuntimePreparationResult preparation))
            {
                preparation = m_Host.Prepare(
                    snapshot.RequestId,
                    timeline.Clone(),
                    executionIdentity,
                    MapPlaybackMode(snapshot.PlaybackMode),
                    Array.Empty<TimelineCallBinding>());
                if (!preparation.IsReady)
                    throw new InvalidOperationException($"Ability Timeline restore '{snapshot.RuntimeHandle}' failed preparation: {string.Join(" | ", preparation.Errors)}");
            }
            TimelineData playbackTimeline = preparation.SourceTimeline;
            var stopCause = MapPlaybackStopCause(snapshot.StopCause);
            var native = new TimelineRuntimePlaybackSnapshot(
                new TimelineRuntimePlaybackHandle((ulong)snapshot.RuntimeHandle),
                snapshot.Generation,
                snapshot.RequestId,
                executionIdentity,
                preparation.PlaybackMode,
                m_NumericTarget,
                snapshot.ContentRevision,
                MapPlaybackState(snapshot.State),
                snapshot.CursorTime,
                snapshot.Cycle,
                snapshot.SectionId,
                snapshot.ActiveClipIds,
                snapshot.HasStopContext,
                new TimelinePlaybackStopContext(stopCause, snapshot.StopLocalLogicTick),
                snapshot.InitialBoundaryPending,
                snapshot.TimeCarry,
                snapshot.Control,
                snapshot.TreeDecisionExits,
                snapshot.PendingTreeDecisionExits,
                m_TickRate);
            TimelineRuntimeRestoreCandidate candidate = m_Host.PrepareRestore(native, preparation);
            TimelineRuntimePlaybackHandle restored = m_Host.ApplyRestore(candidate);
            m_TreeClipService.Restore(restored.Value, snapshot.ActiveTreeClips);
            var handle = new TimelinePlaybackHandle(restored.Value);
            for (int index = 0; index < m_ActivePlaybacks.Count; index++)
            {
                ActivePlayback current = m_ActivePlaybacks[index];
                if (current.Handle.Value != restored.Value)
                    continue;
                current.RestoredReleasedOwner |= current.LogicOwnerReleased;
                current.LogicOwnerReleased = false;
                m_ActivePlaybacks[index] = current;
                return checked((int)restored.Value);
            }
            var active = new ActivePlayback
            {
                Handle = handle,
                Generation = snapshot.Generation,
                Timeline = playbackTimeline,
                SourceName = playbackTimeline.Name,
                SourceKind = CharacterTimelinePlaybackSourceKind.AbilityRuntime,
                CoreDriven = true,
                ActionContext = new TimelinePlaybackActionContext(
                    snapshot.ActionContext.InstanceId,
                    snapshot.ActionContext.ActionId,
                    snapshot.ActionContext.PredictionKey,
                    snapshot.InputSequence,
                    snapshot.StartTick.Value),
                ActionInstanceId = snapshot.ActionContext.InstanceId,
                RuntimeInstance = CreateRuntimeInstance(
                    handle,
                    snapshot.ActionContext.InstanceId,
                    snapshot.InvocationSource.OperationIndex),
                Provenance = CreateAbilityPlaybackProvenance(
                    snapshot.InvocationSource,
                    snapshot.ActionContext,
                    playbackTimeline.Name)
            };
            m_ActivePlaybacks.Add(active);
            PublishPlaybackSnapshot(active);
            PublishTimelineLifecycle(active, RuntimeTraceEventKind.TimelineStarted, "Restored", string.Empty);
            return checked((int)restored.Value);
        }

        static AbilityTimelineSnapshotMode MapSnapshotMode(TimelinePlaybackMode mode) => mode switch
        {
            TimelinePlaybackMode.Loop => AbilityTimelineSnapshotMode.Loop,
            _ => AbilityTimelineSnapshotMode.Once
        };

        static TimelinePlaybackMode MapPlaybackMode(AbilityTimelineSnapshotMode mode) => mode switch
        {
            AbilityTimelineSnapshotMode.Loop => TimelinePlaybackMode.Loop,
            _ => TimelinePlaybackMode.Once
        };

        static AbilityTimelineSnapshotState MapSnapshotState(TimelineRuntimePlaybackState state) => state switch
        {
            TimelineRuntimePlaybackState.Prepared => AbilityTimelineSnapshotState.Prepared,
            TimelineRuntimePlaybackState.Running => AbilityTimelineSnapshotState.Running,
            TimelineRuntimePlaybackState.Stopping => AbilityTimelineSnapshotState.Stopping,
            TimelineRuntimePlaybackState.Completed => AbilityTimelineSnapshotState.Completed,
            TimelineRuntimePlaybackState.Stopped => AbilityTimelineSnapshotState.Stopped,
            TimelineRuntimePlaybackState.Failed => AbilityTimelineSnapshotState.Failed,
            TimelineRuntimePlaybackState.Disposed => AbilityTimelineSnapshotState.Disposed,
            _ => throw new InvalidOperationException("Timeline playback state is invalid.")
        };

        static TimelineRuntimePlaybackState MapPlaybackState(AbilityTimelineSnapshotState state) => state switch
        {
            AbilityTimelineSnapshotState.Prepared => TimelineRuntimePlaybackState.Prepared,
            AbilityTimelineSnapshotState.Running => TimelineRuntimePlaybackState.Running,
            AbilityTimelineSnapshotState.Stopping => TimelineRuntimePlaybackState.Stopping,
            AbilityTimelineSnapshotState.Completed => TimelineRuntimePlaybackState.Completed,
            AbilityTimelineSnapshotState.Stopped => TimelineRuntimePlaybackState.Stopped,
            AbilityTimelineSnapshotState.Failed => TimelineRuntimePlaybackState.Failed,
            AbilityTimelineSnapshotState.Disposed => TimelineRuntimePlaybackState.Disposed,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

        static AbilityTimelineSnapshotStopCause MapSnapshotStopCause(TimelinePlaybackStopCause cause) => cause switch
        {
            TimelinePlaybackStopCause.SelfAbort => AbilityTimelineSnapshotStopCause.SelfAbort,
            TimelinePlaybackStopCause.LowerPriorityAbort => AbilityTimelineSnapshotStopCause.LowerPriorityAbort,
            TimelinePlaybackStopCause.ExplicitParentStop => AbilityTimelineSnapshotStopCause.ExplicitParentStop,
            TimelinePlaybackStopCause.StateTransition => AbilityTimelineSnapshotStopCause.StateTransition,
            TimelinePlaybackStopCause.Reset => AbilityTimelineSnapshotStopCause.Reset,
            TimelinePlaybackStopCause.Shutdown => AbilityTimelineSnapshotStopCause.Shutdown,
            _ => AbilityTimelineSnapshotStopCause.None
        };

        static TimelinePlaybackStopCause MapPlaybackStopCause(AbilityTimelineSnapshotStopCause cause) => cause switch
        {
            AbilityTimelineSnapshotStopCause.SelfAbort => TimelinePlaybackStopCause.SelfAbort,
            AbilityTimelineSnapshotStopCause.LowerPriorityAbort => TimelinePlaybackStopCause.LowerPriorityAbort,
            AbilityTimelineSnapshotStopCause.ExplicitParentStop => TimelinePlaybackStopCause.ExplicitParentStop,
            AbilityTimelineSnapshotStopCause.StateTransition => TimelinePlaybackStopCause.StateTransition,
            AbilityTimelineSnapshotStopCause.Reset => TimelinePlaybackStopCause.Reset,
            AbilityTimelineSnapshotStopCause.Shutdown => TimelinePlaybackStopCause.Shutdown,
            _ => TimelinePlaybackStopCause.SelfAbort
        };
        public void CancelTimelinePlayback(
            TimelinePlaybackHandle handle,
            TimelinePlaybackStopContext stopContext)
        {
            if (!m_Initialized || m_Host == null || !handle.IsValid)
                return;
            if (m_PendingStops.TryGetValue(handle.Value, out CharacterTimelinePendingStop pendingStop))
                CommitStopTimelinePlayback(pendingStop.Pending);
            if (m_PendingAdvances.TryGetValue(handle.Value, out CharacterTimelinePendingAdvance pending))
                DiscardTimelinePlayback(pending.Pending);
            m_Host.Service.CancelTimelinePlayback(handle, stopContext);
        }

        RuntimeInstanceKey CreateRuntimeInstance(
            TimelinePlaybackHandle handle,
            ulong actionInstanceId,
            int sourceOperationIndex)
        {
            return m_Diagnostics == null
                ? default
                : RuntimeInstanceKey.Timeline(
                    m_Diagnostics.CharacterRuntimeId,
                    sourceOperationIndex,
                    handle.Value,
                    actionInstanceId);
        }

        static RuntimeTimelinePlaybackProvenance CreatePlaybackProvenance(
            string sourceId,
            string sourceName,
            TreeExecutionActivationScope sourceActivation,
            BaseGraph sourceRuntimeGraph)
        {
            if (sourceRuntimeGraph == null)
                return default;
            if (!sourceActivation.IsValid)
                throw new InvalidOperationException("Timeline playback source activation is invalid.");
            StateMachineExecutionScope state = sourceActivation.StateMachineExecutionPath.Leaf;
            return new RuntimeTimelinePlaybackProvenance(
                sourceRuntimeGraph.GraphAuthoringId,
                sourceId,
                sourceRuntimeGraph.RuntimeId,
                sourceActivation.ActivationId.Generation,
                state.StateMachineGraphOwnerId,
                state.StateId,
                state.StateMachineGraphRuntimeId,
                state.ActivationGeneration,
                sourceName);
        }

        static RuntimeTimelinePlaybackProvenance CreateAbilityPlaybackProvenance(
            AbilityTimelineInvocationSource invocationSource,
            TimelineActionContextIdentity actionContext,
            string sourceName)
        {
            if (!invocationSource.IsValid)
                throw new ArgumentException("Ability Timeline invocation source is invalid.", nameof(invocationSource));
            if (!actionContext.IsValid || !actionContext.HasSkillExecution || actionContext.SkillExecutionGeneration == 0)
                throw new ArgumentException("Ability Timeline Action context is invalid.", nameof(actionContext));
            return new RuntimeTimelinePlaybackProvenance(
                invocationSource.GraphAuthoringId,
                invocationSource.NodeAuthoringId,
                Guid.Empty,
                invocationSource.InvocationGeneration,
                string.Empty,
                string.Empty,
                Guid.Empty,
                0,
                sourceName,
                invocationSource.GraphInvocationPath,
                invocationSource.OperationIndex,
                actionContext.SkillExecutionGeneration);
        }

        void PublishPlaybackSnapshot(ActivePlayback active)
        {
            if (!active.RuntimeInstance.IsValid || active.Timeline == null)
                return;
            if (!TimelineRuntimePlaybackSnapshotRegistry.Publish(active.RuntimeInstance, active.Timeline))
                return;
            for (int index = 0; index < m_ActivePlaybacks.Count; index++)
            {
                ActivePlayback registered = m_ActivePlaybacks[index];
                if (registered.Handle.Value != active.Handle.Value)
                    continue;
                registered.CreatedDiagnosticSnapshot = true;
                m_ActivePlaybacks[index] = registered;
                break;
            }
        }

        void OnCommittedTimelineEvaluation(TimelineRuntimeCommittedEvaluation evaluation)
        {
            if (!TryGetActivePlayback(evaluation.Handle, out ActivePlayback active))
                return;
            if (!active.CoreDriven)
            {
                active.LocalPresentationSample = new TimelineRuntimePresentationSample(evaluation.Generation,
                    evaluation.LogicTick, evaluation.ContentRevision, evaluation.Time, evaluation.Cycle,
                    evaluation.Completes ? TimelinePresentationSampleReason.Completed :
                    evaluation.Control.IsPaused ? TimelinePresentationSampleReason.Paused : TimelinePresentationSampleReason.Advance, false);
                for (int i = 0; i < m_ActivePlaybacks.Count; i++)
                    if (m_ActivePlaybacks[i].Handle.Value == active.Handle.Value)
                    {
                        m_ActivePlaybacks[i] = active;
                        break;
                    }
            }
            float time = evaluation.Time.ToSingle();
            PublishTimelineEvent(
                active,
                RuntimeTraceDomain.Logic,
                RuntimeTraceEventKind.TimelineLogicTime,
                RuntimeSourceElementKey.Timeline(active.Timeline.AuthoringId),
                "Running",
                string.Empty,
                time,
                evaluation.Cycle);
            PublishActiveTimelineElements(active, evaluation, time);
            PublishTreeClipEvents(active, evaluation, time);
            TimelinePlaybackStatus status = m_Host.Service.GetTimelinePlaybackStatus(
                new TimelinePlaybackHandle(evaluation.Handle.Value));
            if (status == TimelinePlaybackStatus.Succeeded)
                PublishTerminal(active.Handle, RuntimeTraceEventKind.TimelineCompleted, "Completed", string.Empty);
            else if (status == TimelinePlaybackStatus.Failed)
                PublishTerminal(active.Handle, RuntimeTraceEventKind.TimelineCancelled, "Failed", "RuntimeFailure");
        }

        void OnTimelineStopCommitted(TimelineRuntimeStopRequest request)
        {
            PublishTerminal(
                new TimelinePlaybackHandle(request.Handle.Value),
                RuntimeTraceEventKind.TimelineStopped,
                "Stopped",
                request.Reason.Cause.ToString());
        }

        void PublishActiveTimelineElements(
            ActivePlayback active,
            TimelineRuntimeCommittedEvaluation evaluation,
            float time)
        {
            if (active.Timeline == null || m_Diagnostics == null)
                return;
            bool publishTracks = m_Diagnostics.ShouldPublish(RuntimeTraceChannel.Timeline, RuntimeTraceEventKind.TrackActive);
            bool publishClips = m_Diagnostics.ShouldPublish(RuntimeTraceChannel.Timeline, RuntimeTraceEventKind.ClipActive);
            if (!publishTracks && !publishClips)
                return;
            TimelineRuntimeSampleView<string> activeClipIds = evaluation.ActiveClipIds;
            for (int trackIndex = 0; trackIndex < active.Timeline.Tracks.Count; trackIndex++)
            {
                Track track = active.Timeline.Tracks[trackIndex];
                if (track == null)
                    continue;
                bool trackPublished = false;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip == null)
                        continue;
                    bool isActive = false;
                    for (int activeIndex = 0; activeIndex < activeClipIds.Count; activeIndex++)
                    {
                        if (!string.Equals(activeClipIds[activeIndex], clip.AuthoringId, StringComparison.Ordinal))
                            continue;
                        isActive = true;
                        break;
                    }
                    if (!isActive)
                        continue;
                    if (publishTracks && !trackPublished)
                    {
                        PublishTimelineEvent(active, RuntimeTraceDomain.Logic, RuntimeTraceEventKind.TrackActive,
                            RuntimeSourceElementKey.Track(active.Timeline.AuthoringId, track.AuthoringId),
                            "Active", string.Empty, time, evaluation.Cycle);
                        trackPublished = true;
                    }
                    if (publishClips)
                        PublishTimelineEvent(active, RuntimeTraceDomain.Logic, RuntimeTraceEventKind.ClipActive,
                            RuntimeSourceElementKey.Clip(active.Timeline.AuthoringId, track.AuthoringId,
                                clip.AuthoringId, clip is TreeClip), "Active", string.Empty, time, evaluation.Cycle);
                }
            }
        }

        void PublishTreeClipEvents(
            ActivePlayback active,
            TimelineRuntimeCommittedEvaluation evaluation,
            float time)
        {
            TimelineRuntimeSampleView<TimelineRuntimeTreeClipRequest> requests = evaluation.Evaluation.TreeClips;
            for (int index = 0; index < requests.Count; index++)
            {
                TimelineRuntimeTreeClipRequest request = requests[index];
                RuntimeTraceEventKind kind = request.EventKind switch
                {
                    TimelineRuntimeTreeClipEventKind.Enter => RuntimeTraceEventKind.TreeClipEntered,
                    TimelineRuntimeTreeClipEventKind.Update => RuntimeTraceEventKind.TreeClipUpdated,
                    TimelineRuntimeTreeClipEventKind.Exit => RuntimeTraceEventKind.TreeClipExited,
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (m_Diagnostics == null || !m_Diagnostics.ShouldPublish(RuntimeTraceChannel.Timeline, kind))
                    continue;
                RuntimeInstanceKey treeClip = RuntimeInstanceKey.TreeClip(
                    active.RuntimeInstance.CharacterRuntimeId,
                    active.Provenance.SourceGraphRuntimeId,
                    active.RuntimeInstance.SourceOperationIndex,
                    active.Handle.Value,
                    request.Cycle,
                    active.ActionInstanceId);
                PublishTimelineEvent(
                    active,
                    RuntimeTraceDomain.Logic,
                    kind,
                    RuntimeSourceElementKey.Clip(
                        active.Timeline.AuthoringId,
                        request.TrackAuthoringId,
                        request.ClipAuthoringId,
                        true),
                    request.EventKind switch
                    {
                        TimelineRuntimeTreeClipEventKind.Enter => "Enter",
                        TimelineRuntimeTreeClipEventKind.Update => "Update",
                        TimelineRuntimeTreeClipEventKind.Exit => "Exit",
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    string.Empty,
                    time,
                    request.Cycle,
                    treeClip,
                    request.TreeGraphId);
            }
        }

        void PublishTimelineVisualTime(ActivePlayback active, TimelineRuntimePresentationFrame frame)
        {
            PublishTimelineEvent(
                active,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.TimelineVisualTime,
                RuntimeSourceElementKey.Timeline(active.Timeline.AuthoringId),
                "Presented",
                string.Empty,
                frame.Time.ToSingle(),
                frame.Cycle,
                default,
                string.Empty);
        }

        void PublishTerminal(
            TimelinePlaybackHandle handle,
            RuntimeTraceEventKind kind,
            string status,
            string cause)
        {
            for (int index = 0; index < m_ActivePlaybacks.Count; index++)
            {
                ActivePlayback active = m_ActivePlaybacks[index];
                if (active.Handle.Value != handle.Value || active.TerminalPublished)
                    continue;
                active.TerminalPublished = true;
                m_ActivePlaybacks[index] = active;
                PublishTimelineLifecycle(active, kind, status, cause);
                return;
            }
        }

        void PublishTimelineLifecycle(
            ActivePlayback active,
            RuntimeTraceEventKind kind,
            string status,
            string cause)
        {
            if (active.Timeline == null)
                return;
            PublishTimelineEvent(
                active,
                RuntimeTraceDomain.Lifecycle,
                kind,
                RuntimeSourceElementKey.Timeline(active.Timeline.AuthoringId),
                status,
                cause,
                0f,
                0);
        }

        void PublishTimelineEvent(
            ActivePlayback active,
            RuntimeTraceDomain domain,
            RuntimeTraceEventKind kind,
            RuntimeSourceElementKey source,
            string status,
            string cause,
            float time,
            int cycle,
            RuntimeInstanceKey runtimeInstance = default,
            string relatedElementId = "")
        {
            if (m_Diagnostics == null || !active.RuntimeInstance.IsValid ||
                !m_Diagnostics.ShouldPublish(RuntimeTraceChannel.Timeline, kind))
                return;
            m_Diagnostics.Publish(
                RuntimeTraceChannel.Timeline,
                domain,
                kind,
                source,
                runtimeInstance.IsValid ? runtimeInstance : active.RuntimeInstance,
                new RuntimeTracePayload
                {
                    Name = active.Timeline.Name,
                    Status = status,
                    Cause = cause,
                    RelatedElementId = relatedElementId,
                    ActionInstanceId = active.ActionInstanceId,
                    ActivationGeneration = active.Provenance.SourceActivationGeneration,
                    Time = time,
                    Cycle = cycle,
                    TimelinePlayback = active.Provenance
                });
        }

        bool TryGetActivePlayback(TimelineRuntimePlaybackHandle handle, out ActivePlayback active)
        {
            for (int index = 0; index < m_ActivePlaybacks.Count; index++)
            {
                active = m_ActivePlaybacks[index];
                if (active.Handle.Value == handle.Value)
                    return true;
            }
            active = default;
            return false;
        }

        static AbilityTimelineRuntimeStatus MapTerminalStatus(TimelinePlaybackStatus status)
        {
            return status switch
            {
                TimelinePlaybackStatus.Succeeded => AbilityTimelineRuntimeStatus.Succeeded,
                TimelinePlaybackStatus.Failed => AbilityTimelineRuntimeStatus.Failed,
                TimelinePlaybackStatus.Cancelled => AbilityTimelineRuntimeStatus.Cancelled,
                _ => AbilityTimelineRuntimeStatus.Running
            };
        }

        public void CollectActivePlaybacks(List<CharacterTimelinePlaybackObservation> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));
            results.Clear();
            if (!m_Initialized || m_Host == null)
                return;
            for (int i = 0; i < m_ActivePlaybacks.Count; i++)
            {
                ActivePlayback active = m_ActivePlaybacks[i];
                ReadLatestSample(
                    active.Handle,
                    out float clipTime,
                    out float normalizedTime,
                    out float weight,
                    out string clipAuthoringId);
                results.Add(new CharacterTimelinePlaybackObservation(
                    active.Handle,
                    m_Host.Service.GetTimelinePlaybackStatus(active.Handle),
                    active.Timeline,
                    active.SourceName,
                    active.SourceKind,
                    clipTime,
                    normalizedTime,
                    weight,
                    clipAuthoringId));
            }
        }

        void ReadLatestSample(
            TimelinePlaybackHandle handle,
            out float clipTime,
            out float normalizedTime,
            out float weight,
            out string clipAuthoringId)
        {
            clipTime = 0f;
            normalizedTime = 0f;
            weight = 0f;
            clipAuthoringId = string.Empty;
            if (!m_Host.TryGetCommittedEvaluationResult(handle, out TimelineRuntimeEvaluationResult result) ||
                result.AnimationContributions.Count == 0)
                return;
            for (int i = 0; i < result.AnimationContributions.Count; i++)
            {
                TimelineAnimationContribution contribution = result.AnimationContributions[i];
                if (contribution.Weight <= weight)
                    continue;
                weight = contribution.Weight;
                clipTime = contribution.ClipTime;
                normalizedTime = contribution.NormalizedTime;
                clipAuthoringId = contribution.ClipAuthoringId;
            }
        }

        internal void Present(in GameplayPresentationFrameContext context, IActionPresentationClockCoordinator clock,
            in Float32PresentationGraphFacts facts)
        {
            if (facts.RenderFrame != context.RenderFrame)
                throw new InvalidOperationException("Timeline presentation requires facts from the same render frame.");
            m_PresentationFacts = facts;
            if (!m_Initialized || m_Host == null || m_ActivePlaybacks.Count == 0)
                return;
            for (int index = m_ActivePlaybacks.Count - 1; index >= 0; index--)
            {
                ActivePlayback active = m_ActivePlaybacks[index];
                if (active.PresentationReleased)
                    continue;
                TimelineRuntimePresentationSample sample = active.LocalPresentationSample;
                bool hasSample = sample.IsValid;
                if (active.CoreDriven)
                {
                    if (clock == null)
                        throw new InvalidOperationException("Ability Timeline presentation requires its Action clock coordinator.");
                    hasSample = clock.TrySampleTimeline(active.ActionInstanceId, active.Provenance.SourceOperationIndex,
                        active.Provenance.SourceInvocationPath, active.Timeline.AuthoringId, active.Generation,
                        context.RenderFrame, context.LocalLogicTick, context.InterpolationAlpha, out sample);
                }
                if (active.PresentationWithdrawn && hasSample && sample.Reason == TimelinePresentationSampleReason.Withdrawn && sample.RetainForCorrection)
                    continue;
                TimelinePlaybackStatus status = m_Host.Service.GetTimelinePlaybackStatus(active.Handle);
                bool locallyStopped = !active.CoreDriven && status != TimelinePlaybackStatus.Requested &&
                    status != TimelinePlaybackStatus.Running && status != TimelinePlaybackStatus.Succeeded;
                TimelineRuntimePresentationFrame frame = default;
                bool presented = hasSample && !locallyStopped && m_Host.TryPresent(
                        active.Handle,
                        in sample,
                        context.RenderFrame,
                        context.PresentationDeltaSeconds,
                        context.InterpolationAlpha,
                        out frame);
                if (presented)
                {
                    m_PresentationCandidates.Add(frame);
                    PresentationFramePrepared?.Invoke(frame);
                }
                bool ended = active.CoreDriven
                    ? hasSample && sample.EndsPlayback
                    : status != TimelinePlaybackStatus.Requested && status != TimelinePlaybackStatus.Running;
                if (ended && !presented)
                {
                    TimelinePresentationSampleReason reason = active.CoreDriven ? sample.Reason : TimelinePresentationSampleReason.Stopped;
                    bool retainForCorrection = active.CoreDriven && sample.RetainForCorrection;
                    m_PresentationEndCandidates.Add((active, reason, retainForCorrection));
                    if (!active.PresentationWithdrawn || !retainForCorrection)
                        PresentationPlaybackEndPrepared?.Invoke(new TimelineRuntimePlaybackHandle(active.Handle.Value), active.Generation, reason, retainForCorrection);
                }
            }
        }

        internal void CommitPresentationFrame(ulong frame, IActionPresentationClockCoordinator clock)
        {
            for (int i = 0; i < m_PresentationCandidates.Count; i++)
            {
                TimelineRuntimePresentationFrame candidate = m_PresentationCandidates[i];
                PresentationFrameProduced?.Invoke(candidate);
                if (TryGetActivePlayback(candidate.Handle, out ActivePlayback active))
                    PublishTimelineVisualTime(active, candidate);
            }
            m_Host?.CommitPresentationFrame(frame);
            for (int i = 0; i < m_PresentationCandidates.Count; i++)
            {
                TimelineRuntimePresentationFrame candidate = m_PresentationCandidates[i];
                for (int index = 0; index < m_ActivePlaybacks.Count; index++)
                {
                    ActivePlayback active = m_ActivePlaybacks[index];
                    if (active.Handle.Value != candidate.Handle.Value || active.Generation != candidate.Generation)
                        continue;
                    active.PresentationWithdrawn = false;
                    m_ActivePlaybacks[index] = active;
                    break;
                }
            }
            for (int i = 0; i < m_PresentationEndCandidates.Count; i++)
            {
                ActivePlayback active = m_PresentationEndCandidates[i].Playback;
                bool retainForCorrection = m_PresentationEndCandidates[i].RetainForCorrection;
                if (active.CoreDriven && !retainForCorrection)
                    clock.ReleaseTimeline(active.ActionInstanceId, active.Provenance.SourceOperationIndex,
                        active.Provenance.SourceInvocationPath, active.Timeline.AuthoringId, active.Generation);
                var handle = new TimelineRuntimePlaybackHandle(active.Handle.Value);
                if (!active.PresentationWithdrawn)
                    PresentationPlaybackEnded?.Invoke(handle, active.Generation, m_PresentationEndCandidates[i].Reason);
                if (retainForCorrection)
                    m_Host.SuspendPresentationPlayback(handle, active.Generation);
                else
                {
                    m_Host.ReleasePresentationPlayback(handle, active.Generation);
                    active.PresentationReleased = true;
                }
                for (int index = m_ActivePlaybacks.Count - 1; index >= 0; index--)
                {
                    if (m_ActivePlaybacks[index].Handle.Value != handle.Value || m_ActivePlaybacks[index].Generation != active.Generation)
                        continue;
                    if (retainForCorrection)
                    {
                        active.PresentationWithdrawn = true;
                        m_ActivePlaybacks[index] = active;
                    }
                    else if (active.CoreDriven)
                        m_ActivePlaybacks[index] = active;
                    else
                        m_ActivePlaybacks.RemoveAt(index);
                }
            }
            m_PresentationCandidates.Clear();
            m_PresentationEndCandidates.Clear();
            if (clock != null)
                ReleaseConfirmedPlaybacks(clock.ConfirmedTimelineTick);
        }

        internal void DiscardPresentationFrame(ulong frame)
        {
            m_PresentationFacts = default;
            m_Host?.DiscardPresentationFrame(frame);
            m_PresentationCandidates.Clear();
            m_PresentationEndCandidates.Clear();
        }

        public void Dispose()
        {
            if (m_Host == null)
                return;
            m_Host.CommittedEvaluation -= OnCommittedTimelineEvaluation;
            m_Host.StopCommitted -= OnTimelineStopCommitted;
            m_Host.Dispose();
            m_Host = null;
            m_TreeClipService = null;
            m_Initialized = false;
            m_ActivePlaybacks.Clear();
            m_PendingAdvances.Clear();
            m_PendingStops.Clear();
            m_ActiveTreeClipInvoker = null;
            m_Diagnostics = null;
        }
    }
}
