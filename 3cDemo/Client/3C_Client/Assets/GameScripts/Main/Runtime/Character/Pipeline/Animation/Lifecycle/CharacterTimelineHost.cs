using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;
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
                    if (track.ExecutionDomain == TimelineExecutionDomain.Presentation && track.Markers.Count != 0)
                        throw new InvalidOperationException($"Timeline '{timeline.AuthoringId}' track '{track.AuthoringId}' requires a presentation graph executor; Simulation graph resources cannot execute its Markers.");
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
        sealed class ActiveTreeClip
        {
            public string ClipAuthoringId;
            public string TreeGraphId;
            public int Cycle;
        }

        readonly CharacterTimelineHost m_Host;
        readonly Dictionary<ulong, List<ActiveTreeClip>> m_ActiveClips = new();

        internal CharacterTimelineTreeClipService(CharacterTimelineHost host)
        {
            m_Host = host ?? throw new ArgumentNullException(nameof(host));
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
            if (request.EventKind == TimelineRuntimeTreeClipEventKind.Update)
            {
                if (!m_ActiveClips.TryGetValue(context.Playback.Handle.Value, out List<ActiveTreeClip> activeClips) ||
                    activeClips.Count == 0)
                    return true;
                ulong actionInstanceId = m_Host.RequireAbilityPlaybackActionInstanceId(context.Playback.Handle);
                foreach (ActiveTreeClip clip in activeClips)
                {
                    if (clip.ClipAuthoringId != request.ClipAuthoringId || clip.Cycle != request.Cycle)
                        continue;
                    var updateInvocation = new AbilityTreeClipInvocation(
                        clip.ClipAuthoringId,
                        clip.TreeGraphId,
                        AbilityTreeClipHook.Root,
                        clip.Cycle,
                        actionInstanceId,
                        checked((int)context.Playback.Handle.Value));
                    return invoker.InvokeTreeClip(updateInvocation);
                }
                return true;
            }
            if (request.EventKind == TimelineRuntimeTreeClipEventKind.Enter)
            {
                if (!m_ActiveClips.TryGetValue(context.Playback.Handle.Value, out List<ActiveTreeClip> clips))
                {
                    clips = new List<ActiveTreeClip>();
                    m_ActiveClips[context.Playback.Handle.Value] = clips;
                }
                clips.RemoveAll(value => value.ClipAuthoringId == request.ClipAuthoringId && value.Cycle == request.Cycle);
                clips.Add(new ActiveTreeClip
                {
                    ClipAuthoringId = request.ClipAuthoringId,
                    TreeGraphId = request.TreeGraphId,
                    Cycle = request.Cycle
                });
            }
            else if (request.EventKind == TimelineRuntimeTreeClipEventKind.Exit)
            {
                RemoveClip(context.Playback.Handle.Value, request.ClipAuthoringId, request.Cycle);
            }
            var invocation = new AbilityTreeClipInvocation(
                request.ClipAuthoringId,
                request.TreeGraphId,
                request.EventKind == TimelineRuntimeTreeClipEventKind.Enter
                    ? AbilityTreeClipHook.OnEnable
                    : AbilityTreeClipHook.OnDisable,
                request.Cycle,
                m_Host.RequireAbilityPlaybackActionInstanceId(context.Playback.Handle),
                checked((int)context.Playback.Handle.Value));
            return invoker.InvokeTreeClip(invocation);
        }

        public void Discard(TimelineRuntimeTreeClipRequest request, TimelineRuntimeStepContext context)
        {
            if (request.EventKind == TimelineRuntimeTreeClipEventKind.Enter)
                RemoveClip(context.Playback.Handle.Value, request.ClipAuthoringId, request.Cycle);
        }

        public void Commit(TimelineRuntimeStepContext context) { }
        public void DiscardStep(TimelineRuntimeStepContext context) { }
        public bool ConsumeStop(TimelineRuntimeStopRequest request)
        {
            if (!m_ActiveClips.TryGetValue(request.Handle.Value, out List<ActiveTreeClip> clips))
                return true;
            IAbilityTreeClipInvoker invoker = m_Host.m_ActiveTreeClipInvoker
                ?? throw new InvalidOperationException("Timeline stop requires the current Ability invoker.");
            foreach (ActiveTreeClip clip in clips)
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
            if (!m_ActiveClips.TryGetValue(handle, out List<ActiveTreeClip> clips))
                return;
            clips.RemoveAll(value => value.ClipAuthoringId == clipAuthoringId && value.Cycle == cycle);
            if (clips.Count == 0)
                m_ActiveClips.Remove(handle);
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

    public sealed class CharacterTimelinePendingAdvance : IAbilityTimelinePending
    {
        internal CharacterTimelinePendingAdvance(
            TimelinePlaybackHandle handle,
            int runtimeHandle,
            TimelineRuntimeAdvanceResult result,
            AbilityTimelineRuntimeStatus status)
        {
            Handle = handle;
            RuntimeHandle = runtimeHandle;
            Result = result;
            Status = status;
        }

        public TimelinePlaybackHandle Handle { get; }
        public int RuntimeHandle { get; }
        public AbilityTimelineRuntimeStatus Status { get; }
        internal TimelineRuntimeAdvanceResult Result { get; }
    }
    public sealed class CharacterTimelinePendingStop : IAbilityTimelineStopPending
    {
        internal CharacterTimelinePendingStop(
            TimelinePlaybackHandle handle,
            int runtimeHandle,
            TimelineRuntimeStopRequest request,
            AbilityTimelineRuntimeStatus status)
        {
            Handle = handle;
            RuntimeHandle = runtimeHandle;
            Request = request;
            Status = status;
        }

        public TimelinePlaybackHandle Handle { get; }
        public int RuntimeHandle { get; }
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

    public readonly struct TimelineActionCueEvent
    {
        public TimelineActionCueEvent(
            EventId eventId,
            string eventName,
            string cueId,
            string stateId,
            int localFrame,
            string branchId,
            TimelineRuntimePlaybackHandle handle,
            ulong generation,
            ulong logicTick,
            int frame,
            int cycle,
            TimelineExecutionIdentity executionIdentity,
            string contentRevision,
            string sourceId,
            string trackAuthoringId,
            string clipAuthoringId)
        {
            if (!eventId.IsValid || string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(cueId) ||
                !handle.IsValid || generation == 0 || logicTick == 0 || frame < 0 || cycle < 0 ||
                localFrame < 0 ||
                !executionIdentity.IsValid || string.IsNullOrWhiteSpace(contentRevision) ||
                string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(trackAuthoringId) ||
                string.IsNullOrWhiteSpace(clipAuthoringId))
            {
                throw new ArgumentException("Timeline ActionCue event is incomplete.");
            }

            EventId = eventId;
            EventName = eventName.Trim();
            CueId = cueId.Trim();
            StateId = stateId?.Trim() ?? string.Empty;
            LocalFrame = localFrame;
            BranchId = branchId?.Trim() ?? string.Empty;
            Handle = handle;
            Generation = generation;
            LogicTick = logicTick;
            Frame = frame;
            Cycle = cycle;
            ExecutionIdentity = executionIdentity;
            ContentRevision = contentRevision.Trim();
            SourceId = sourceId.Trim();
            TrackAuthoringId = trackAuthoringId.Trim();
            ClipAuthoringId = clipAuthoringId.Trim();
        }

        public EventId EventId { get; }
        public string EventName { get; }
        public string CueId { get; }
        public string StateId { get; }
        public int LocalFrame { get; }
        public string BranchId { get; }
        public TimelineRuntimePlaybackHandle Handle { get; }
        public ulong Generation { get; }
        public ulong LogicTick { get; }
        public int Frame { get; }
        public int Cycle { get; }
        public TimelineExecutionIdentity ExecutionIdentity { get; }
        public string ContentRevision { get; }
        public string SourceId { get; }
        public string TrackAuthoringId { get; }
        public string ClipAuthoringId { get; }
    }

    public sealed class CharacterTimelineHost : ITimelinePlaybackService, IDisposable
    {
        struct ActivePlayback
        {
            internal TimelinePlaybackHandle Handle;
            internal TimelineData Timeline;
            internal string SourceName;
            internal CharacterTimelinePlaybackSourceKind SourceKind;
            internal bool CoreDriven;
            internal TimelinePlaybackActionContext ActionContext;
            internal ulong ActionInstanceId;
            internal RuntimeInstanceKey RuntimeInstance;
            internal RuntimeTimelinePlaybackProvenance Provenance;
            internal bool TerminalPublished;
        }

        TimelineRuntimeCompositionHost m_Host;
        readonly List<ActivePlayback> m_ActivePlaybacks = new List<ActivePlayback>();
        readonly List<ActivePlayback> m_PlaybackScan = new List<ActivePlayback>();
        readonly Dictionary<string, TimelineData> m_TimelineContent = new Dictionary<string, TimelineData>(StringComparer.Ordinal);
        readonly Guid m_ContentSessionIdentity = Guid.NewGuid();
        readonly Dictionary<ulong, CharacterTimelinePendingAdvance> m_PendingAdvances =
            new Dictionary<ulong, CharacterTimelinePendingAdvance>();
        readonly Dictionary<ulong, CharacterTimelinePendingStop> m_PendingStops =
            new Dictionary<ulong, CharacterTimelinePendingStop>();
        readonly string m_SourceName;
        readonly CharacterTimelineDependencyResolver m_DependencyResolver = new();
        ulong m_TickCounter;
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

        internal TimelineRuntimeCompositionHost Host => m_Host;
        public bool IsInitialized => m_Initialized && m_Host != null;
        public string AuthoringContentRevision { get; private set; } = string.Empty;
        public string ContentRevision { get; private set; } = string.Empty;
        public ulong ContentGeneration => m_ContentGeneration;
        public event Action<TimelineRuntimePresentationFrame> PresentationFrameProduced;
        public event Action<TimelineRuntimePlaybackHandle> PresentationPlaybackEnded;
        public event Action<TimelineActionCueEvent> ActionCueCommitted;

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
            var treeClipService = new CharacterTimelineTreeClipService(this);
            var markerService = new CharacterTimelineMarkerService(this);

            m_Host = new TimelineRuntimeCompositionHost(
                contractCatalog,
                m_NumericTarget,
                callBindingSource,
                domainResolver,
                m_DependencyResolver,
                Array.Empty<ITimelineRuntimeEvaluationSink>(),
                treeClipService,
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
            var active = new ActivePlayback
            {
                Handle = handle,
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
            return true;
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
            int tickCount)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline advancement requires an initialized CharacterTimelineHost.");
            TimelinePlaybackStatus status = m_Host.Service.GetTimelinePlaybackStatus(handle);
            if (status != TimelinePlaybackStatus.Requested && status != TimelinePlaybackStatus.Running)
                return new CharacterTimelinePendingAdvance(handle, 0, null, MapTerminalStatus(status));
            TimelineRuntimeAdvanceResult advance = m_Host.Advance(new TimelineRuntimePlaybackHandle(handle.Value), logicTick, tickCount);
            var pending = new CharacterTimelinePendingAdvance(handle, (int)handle.Value, advance, AbilityTimelineRuntimeStatus.Running);
            m_PendingAdvances[handle.Value] = pending;
            return pending;
        }

        public void CommitTimelinePlayback(CharacterTimelinePendingAdvance pending)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline commit requires an initialized CharacterTimelineHost.");
            if (pending == null || !m_PendingAdvances.Remove(pending.Handle.Value))
                return;
            if (pending.Result != null && !m_Host.CommitAdvance(new TimelineRuntimePlaybackHandle(pending.Handle.Value), pending.Result))
                throw new InvalidOperationException($"Timeline advance '{pending.Handle.Value}' could not be committed.");
        }

        public void DiscardTimelinePlayback(CharacterTimelinePendingAdvance pending)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline discard requires an initialized CharacterTimelineHost.");
            if (pending == null || !m_PendingAdvances.Remove(pending.Handle.Value))
                return;
            if (pending.Result != null && !m_Host.DiscardAdvance(new TimelineRuntimePlaybackHandle(pending.Handle.Value), pending.Result))
                throw new InvalidOperationException($"Timeline advance '{pending.Handle.Value}' could not be discarded.");
        }


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
                return new CharacterTimelinePendingStop(handle, 0, default, MapTerminalStatus(status));
            if (!m_Host.Service.RequestStopTimelinePlayback(
                    handle,
                    stopContext,
                    out TimelineRuntimeStopRequest request))
                throw new InvalidOperationException($"Timeline stop '{handle.Value}' was rejected.");
            var pending = new CharacterTimelinePendingStop(handle, (int)handle.Value, request, AbilityTimelineRuntimeStatus.Running);
            m_PendingStops[handle.Value] = pending;
            return pending;
        }

        public void CommitStopTimelinePlayback(CharacterTimelinePendingStop pending)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline stop commit requires an initialized CharacterTimelineHost.");
            if (pending == null || !m_PendingStops.Remove(pending.Handle.Value))
                return;
            if (pending.Request.Playback != null && !m_Host.Service.CommitStopTimelinePlayback(pending.Handle, pending.Request))
                throw new InvalidOperationException($"Timeline stop '{pending.Handle.Value}' could not be committed.");
        }

        public void DiscardStopTimelinePlayback(CharacterTimelinePendingStop pending)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline stop discard requires an initialized CharacterTimelineHost.");
            if (pending == null || !m_PendingStops.Remove(pending.Handle.Value))
                return;
            if (pending.Request.Playback != null && !m_Host.Service.DiscardStopTimelinePlayback(pending.Handle, pending.Request))
                throw new InvalidOperationException($"Timeline stop '{pending.Handle.Value}' could not be discarded.");
        }
        public bool RequestAbilityTreeClipExit(int runtimeHandle, string clipAuthoringId)
        {
            if (!IsInitialized)
                return false;
            return m_Host.Service.RequestTreeClipExit(
                new TimelineRuntimePlaybackHandle((ulong)runtimeHandle),
                clipAuthoringId);
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
                native.TreeDecisionExits,
                native.PendingTreeDecisionExits,
                native.SectionId,
                native.ActiveClipIds,
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
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (!m_TimelineContent.TryGetValue(snapshot.TimelineId, out TimelineData timeline))
                throw new KeyNotFoundException($"Ability Timeline content '{snapshot.TimelineId}' is not installed.");
            var executionIdentity = new TimelineExecutionIdentity(
                snapshot.OwnerIdentity,
                snapshot.CallIdentity,
                snapshot.ExecutionInstanceId);
            TimelineData playbackTimeline = timeline.Clone();
            TimelineRuntimePreparationResult preparation = m_Host.Prepare(
                snapshot.RequestId,
                playbackTimeline,
                executionIdentity,
                MapPlaybackMode(snapshot.PlaybackMode),
                Array.Empty<TimelineCallBinding>());
            if (!preparation.IsReady)
                throw new InvalidOperationException($"Ability Timeline restore '{snapshot.RuntimeHandle}' failed preparation: {string.Join(" | ", preparation.Errors)}");
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
                Array.Empty<TimelineRuntimeTreeClipAssociation>(),
                snapshot.HasStopContext,
                new TimelinePlaybackStopContext(stopCause, snapshot.StopLocalLogicTick),
                snapshot.InitialBoundaryPending,
                snapshot.TimeCarry,
                snapshot.TreeDecisionExits,
                snapshot.PendingTreeDecisionExits,
                m_TickRate);
            TimelineRuntimeRestoreCandidate candidate = m_Host.PrepareRestore(native, preparation);
            TimelineRuntimePlaybackHandle restored = m_Host.ApplyRestore(candidate);
            var handle = new TimelinePlaybackHandle(restored.Value);
            var active = new ActivePlayback
            {
                Handle = handle,
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
                CommitStopTimelinePlayback(pendingStop);
            if (m_PendingAdvances.TryGetValue(handle.Value, out CharacterTimelinePendingAdvance pending))
                DiscardTimelinePlayback(pending);
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
            TimelineRuntimePlaybackSnapshotRegistry.Publish(active.RuntimeInstance, active.Timeline);
        }

        void OnCommittedTimelineEvaluation(TimelineRuntimeCommittedEvaluation evaluation)
        {
            if (!TryGetActivePlayback(evaluation.Handle, out ActivePlayback active))
                return;
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
            PublishActionCueEvents(active, evaluation, time);
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

        void PublishActionCueEvents(
            ActivePlayback active,
            TimelineRuntimeCommittedEvaluation evaluation,
            float time)
        {
            IReadOnlyList<TimelineActionCueSample> cues = evaluation.Evaluation?.ActionCues;
            if (cues == null)
                return;
            for (int index = 0; index < cues.Count; index++)
            {
                TimelineActionCueSample cue = cues[index];
                var committed = new TimelineActionCueEvent(
                    new EventId(StableHash.Compute(
                        "timeline.action-cue.committed",
                        evaluation.Handle.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        evaluation.Generation.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        evaluation.LogicTick.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        evaluation.Cycle.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        cue.ClipAuthoringId,
                        cue.CueId,
                        cue.CueType,
                        cue.StateId,
                        cue.LocalFrame.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        cue.BranchId)),
                    cue.CueType,
                    cue.CueId,
                    cue.StateId,
                    cue.LocalFrame,
                    cue.BranchId,
                    evaluation.Handle,
                    evaluation.Generation,
                    evaluation.LogicTick,
                    TimelineTimeGrid.NearestIndex(evaluation.Time, TimelineUtility.FrameRate),
                    evaluation.Cycle,
                    evaluation.ExecutionIdentity,
                    evaluation.ContentRevision,
                    cue.SourceId,
                    cue.TrackName,
                    cue.ClipAuthoringId);
                ActionCueCommitted?.Invoke(committed);
                PublishTimelineEvent(
                    active,
                    RuntimeTraceDomain.Logic,
                    RuntimeTraceEventKind.ActionCueSubmitted,
                    RuntimeSourceElementKey.Timeline(cue.SourceId),
                    cue.CueType,
                    cue.TrackName,
                    time,
                    evaluation.Cycle,
                    default,
                    cue.CueId);
            }
        }

        void PublishActiveTimelineElements(
            ActivePlayback active,
            TimelineRuntimeCommittedEvaluation evaluation,
            float time)
        {
            if (active.Timeline == null)
                return;
            var activeTracks = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<string> activeClipIds = evaluation.Evaluation?.ClipSamples != null
                ? evaluation.Evaluation.ClipSamples.Select(value => value.ClipAuthoringId).ToArray()
                : Array.Empty<string>();
            if (m_Host.TryGetPlaybackDescriptor(active.Handle, out TimelineRuntimePlaybackDescriptor descriptor))
                activeClipIds = descriptor.ActiveClipIds;
            for (int index = 0; index < activeClipIds.Count; index++)
            {
                if (!TryFindClip(active.Timeline, activeClipIds[index], out Track track, out Clip clip))
                    continue;
                if (activeTracks.Add(track.AuthoringId))
                {
                    PublishTimelineEvent(
                        active,
                        RuntimeTraceDomain.Logic,
                        RuntimeTraceEventKind.TrackActive,
                        RuntimeSourceElementKey.Track(active.Timeline.AuthoringId, track.AuthoringId),
                        "Active",
                        string.Empty,
                        time,
                        evaluation.Cycle);
                }
                PublishTimelineEvent(
                    active,
                    RuntimeTraceDomain.Logic,
                    RuntimeTraceEventKind.ClipActive,
                    RuntimeSourceElementKey.Clip(
                        active.Timeline.AuthoringId,
                        track.AuthoringId,
                        clip.AuthoringId,
                        clip is TreeClip),
                    "Active",
                    string.Empty,
                    time,
                    evaluation.Cycle);
            }
        }

        void PublishTreeClipEvents(
            ActivePlayback active,
            TimelineRuntimeCommittedEvaluation evaluation,
            float time)
        {
            IReadOnlyList<TimelineRuntimeTreeClipRequest> requests = evaluation.Evaluation?.TreeClips;
            if (requests == null)
                return;
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
                    request.EventKind.ToString(),
                    string.Empty,
                    time,
                    request.Cycle,
                    treeClip,
                    request.TreeGraphId);
            }
        }

        void PublishTimelineVisualTime(ActivePlayback active, TimelineRuntimePresentationFrame frame)
        {
            if (!m_Host.TryGetPlaybackDescriptor(active.Handle, out TimelineRuntimePlaybackDescriptor descriptor))
                return;
            PublishTimelineEvent(
                active,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.TimelineVisualTime,
                RuntimeSourceElementKey.Timeline(active.Timeline.AuthoringId),
                "Presented",
                string.Empty,
                descriptor.CursorTime.ToSingle(),
                descriptor.Cycle,
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
            if (m_Diagnostics == null || !active.RuntimeInstance.IsValid)
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

        static bool TryFindClip(TimelineData timeline, string clipAuthoringId, out Track track, out Clip clip)
        {
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                track = timeline.Tracks[trackIndex];
                if (track == null)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    clip = track.Clips[clipIndex];
                    if (clip != null && string.Equals(clip.AuthoringId, clipAuthoringId, StringComparison.Ordinal))
                        return true;
                }
            }
            track = null;
            clip = null;
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
                result == null || result.AnimationContributions.Count == 0)
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

        public void Update(float deltaTime)
        {
            if (!m_Initialized || m_Host == null || m_ActivePlaybacks.Count == 0)
                return;
            m_TickCounter++;
            int deltaFrames = Math.Max(1, (int)MathF.Round(deltaTime * 60f));
            m_PlaybackScan.Clear();
            m_PlaybackScan.AddRange(m_ActivePlaybacks);
            for (int i = 0; i < m_PlaybackScan.Count; i++)
            {
                ActivePlayback active = m_PlaybackScan[i];
                TimelinePlaybackStatus status = m_Host.Service.GetTimelinePlaybackStatus(active.Handle);
                if (status != TimelinePlaybackStatus.Requested && status != TimelinePlaybackStatus.Running)
                    continue;
                if (active.CoreDriven)
                    continue;
                m_Host.Service.Step(active.Handle, m_TickCounter, deltaFrames);
            }
        }

        public void Present(in GameplayPresentationFrameContext context)
        {
            if (!m_Initialized || m_Host == null || m_ActivePlaybacks.Count == 0)
                return;
            for (int index = m_ActivePlaybacks.Count - 1; index >= 0; index--)
            {
                ActivePlayback active = m_ActivePlaybacks[index];
                bool presented = m_Host.TryPresent(
                        active.Handle,
                        context.RenderFrame,
                        context.PresentationDeltaSeconds,
                        context.InterpolationAlpha,
                        out TimelineRuntimePresentationFrame frame);
                if (presented)
                {
                    if (frame.Events.Count != 0)
                        throw new InvalidOperationException($"Timeline playback '{frame.Handle.Value}' has Presentation Markers without an installed presentation graph executor.");
                    PresentationFrameProduced?.Invoke(frame);
                    PublishTimelineVisualTime(active, frame);
                }
                TimelinePlaybackStatus status = m_Host.Service.GetTimelinePlaybackStatus(active.Handle);
                if (status != TimelinePlaybackStatus.Requested &&
                    status != TimelinePlaybackStatus.Running &&
                    !presented)
                {
                    PresentationPlaybackEnded?.Invoke(new TimelineRuntimePlaybackHandle(active.Handle.Value));
                    m_ActivePlaybacks.RemoveAt(index);
                }
            }
        }

        public void Dispose()
        {
            if (m_Host == null)
                return;
            m_Host.CommittedEvaluation -= OnCommittedTimelineEvaluation;
            m_Host.StopCommitted -= OnTimelineStopCommitted;
            m_Host.Dispose();
            m_Host = null;
            m_Initialized = false;
            m_ActivePlaybacks.Clear();
            m_PendingAdvances.Clear();
            m_PendingStops.Clear();
            m_ActiveTreeClipInvoker = null;
            m_Diagnostics = null;
        }
    }
}
