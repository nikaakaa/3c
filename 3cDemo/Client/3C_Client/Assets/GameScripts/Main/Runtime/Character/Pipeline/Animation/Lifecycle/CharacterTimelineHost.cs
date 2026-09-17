using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;
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

        public bool TryResolve(
            TimelineContentDependency dependency,
            TimelineRuntimeNumericTarget numericTarget,
            out TimelineRuntimeDependencyHandle handle,
            out string error)
        {
            handle = TimelineRuntimeDependencyHandle.Invalid;
            error = $"Timeline runtime dependency '{dependency.Identity}' requires a composed dependency service."; return false;
        }
    }

    internal sealed class CharacterTimelineTreeClipService : ITimelineRuntimeTreeClipService
    {
        sealed class ActiveTreeClip
        {
            public string ClipAuthoringId;
            public string TreeGraphId;
            public int Cycle;
            public IAbilityTreeClipInvoker Invoker;
        }

        readonly CharacterTimelineHost m_Host;
        readonly Dictionary<ulong, List<ActiveTreeClip>> m_ActiveClips = new();

        internal CharacterTimelineTreeClipService(CharacterTimelineHost host)
        {
            m_Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public bool Consume(TimelineRuntimeTreeClipRequest request, TimelineRuntimeStepContext context)
        {
            if (request.EventKind == TimelineRuntimeTreeClipEventKind.Update)
                return true;
            if (!m_Host.IsAbilityRuntimePlayback(context.Playback.Handle))
                return true;
            IAbilityTreeClipInvoker invoker = m_Host.m_ActiveTreeClipInvoker
                ?? throw new InvalidOperationException($"Timeline TreeClip '{request.ClipAuthoringId}' requires an active Ability invoker.");
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
                    Cycle = request.Cycle,
                    Invoker = invoker
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
                    : AbilityTreeClipHook.OnDisable);
            return invoker.InvokeTreeClip(invocation);
        }

        public void Discard(TimelineRuntimeTreeClipRequest request, TimelineRuntimeStepContext context)
        {
            if (request.EventKind == TimelineRuntimeTreeClipEventKind.Enter)
                RemoveClip(context.Playback.Handle.Value, request.ClipAuthoringId, request.Cycle);
        }

        public void Commit(TimelineRuntimeStepContext context) { }
        public void DiscardStep(TimelineRuntimeStepContext context) { }
        public bool ConsumeStop(TimelineRuntimeStopRequest request) => true;

        public void CommitStop(TimelineRuntimeStopRequest request)
        {
            if (!m_ActiveClips.TryGetValue(request.Handle.Value, out List<ActiveTreeClip> clips))
                return;
            m_ActiveClips.Remove(request.Handle.Value);
            foreach (ActiveTreeClip clip in clips)
            {
                var invocation = new AbilityTreeClipInvocation(
                    clip.ClipAuthoringId,
                    clip.TreeGraphId,
                    AbilityTreeClipHook.OnDestroy);
                if (!clip.Invoker.InvokeTreeClip(invocation))
                    continue;
            }
        }

        public void DiscardStop(TimelineRuntimeStopRequest request) =>
            m_ActiveClips.Remove(request.Handle.Value);

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
        FixedPreview = 1,
        AbilityRuntime = 2
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
    [DisallowMultipleComponent]
    public sealed class CharacterTimelineHost : MonoBehaviour, ITimelinePlaybackService
    {
        struct ActivePlayback
        {
            internal TimelinePlaybackHandle Handle;
            internal TimelineData Timeline;
            internal string SourceName;
            internal CharacterTimelinePlaybackSourceKind SourceKind;
            internal bool CoreDriven;
        }

        TimelineRuntimeCompositionHost m_Host;
        readonly List<ActivePlayback> m_ActivePlaybacks = new List<ActivePlayback>();
        readonly List<ActivePlayback> m_PlaybackScan = new List<ActivePlayback>();
        readonly Dictionary<string, TimelineData> m_TimelineContent = new Dictionary<string, TimelineData>(StringComparer.Ordinal);
        readonly Dictionary<ulong, CharacterTimelinePendingAdvance> m_PendingAdvances =
            new Dictionary<ulong, CharacterTimelinePendingAdvance>();
        readonly Dictionary<ulong, CharacterTimelinePendingStop> m_PendingStops =
            new Dictionary<ulong, CharacterTimelinePendingStop>();
        ulong m_TickCounter;
        TimelinePlaybackHandle m_PreviewHandle;
        TimelineRuntimeNumericTarget m_NumericTarget;
        internal ThirdPersonSimulation.IAbilityTreeClipInvoker m_ActiveTreeClipInvoker;
        bool m_Initialized;

        public void PushTreeClipInvoker(ThirdPersonSimulation.IAbilityTreeClipInvoker invoker)
        {
            if (invoker == null)
                throw new ArgumentNullException(nameof(invoker));
            if (m_ActiveTreeClipInvoker != null)
                throw new InvalidOperationException("Timeline TreeClip invoker is already active.");
            m_ActiveTreeClipInvoker = invoker;
        }

        public void PopTreeClipInvoker() => m_ActiveTreeClipInvoker = null;

        internal TimelineRuntimeCompositionHost Host => m_Host;
        public bool IsInitialized => m_Initialized && m_Host != null;

        internal void Initialize(TimelineRuntimeNumericTarget numericTarget)
        {
            if (m_Initialized)
                return;
            if (!Enum.IsDefined(typeof(TimelineRuntimeNumericTarget), numericTarget))
                throw new ArgumentOutOfRangeException(nameof(numericTarget));
            m_NumericTarget = numericTarget;

            var contractCatalog = new TimelineContractCatalog(Array.Empty<ITimelineContractProvider>());
            var callBindingSource = new TimelineRuntimeCallBindingSource(
                name, "character-timeline", default);
            var domainResolver = new CharacterTimelineDomainBindingResolver(
                new[] { "self", "camera", "main" });
            var dependencyResolver = new CharacterTimelineDependencyResolver();
            var treeClipService = new CharacterTimelineTreeClipService(this);

            m_Host = new TimelineRuntimeCompositionHost(
                contractCatalog,
                m_NumericTarget,
                callBindingSource,
                domainResolver,
                dependencyResolver,
                Array.Empty<ITimelineRuntimeEvaluationSink>(),
                treeClipService);
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
            out TimelinePlaybackHandle handle)
        {
            if (!m_Initialized || m_Host == null)
            {
                handle = TimelinePlaybackHandle.Invalid;
                return false;
            }
            if (!m_Host.RequestTimelinePlayback(
                timeline, sourceId, sourceName, actionContext, playbackMode,
                sourceActivation, sourceRuntimeGraph, out handle))
                return false;
            m_ActivePlaybacks.Add(new ActivePlayback
            {
                Handle = handle,
                Timeline = timeline,
                SourceName = sourceName ?? string.Empty,
                SourceKind = sourceKind,
                CoreDriven = false
            });
            return true;
        }

        public void SetTimelineContent(IReadOnlyList<TimelineAsset> timelines)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline content requires an initialized CharacterTimelineHost.");
            if (timelines == null)
                throw new ArgumentNullException(nameof(timelines));
            m_TimelineContent.Clear();
            for (int i = 0; i < timelines.Count; i++)
            {
                TimelineAsset asset = timelines[i];
                if (!asset || asset.Data == null)
                    throw new InvalidOperationException("Timeline content list contains an invalid Timeline asset.");
                if (!m_TimelineContent.TryAdd(asset.Data.AuthoringId, asset.Data))
                    throw new InvalidOperationException($"Timeline content identity '{asset.Data.AuthoringId}' is duplicated.");
            }
        }

        public bool RequestAbilityTimelinePlayback(
            string timelineId,
            TimelinePlaybackActionContext actionContext,
            bool loop,
            out TimelinePlaybackHandle handle)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Ability Timeline requires an initialized CharacterTimelineHost.");
            if (!actionContext.IsValid)
                throw new ArgumentException("Ability Timeline Action context is invalid.", nameof(actionContext));
            if (!m_TimelineContent.TryGetValue(timelineId, out TimelineData timeline))
                throw new KeyNotFoundException($"Ability Timeline content '{timelineId}' is not installed.");
            bool requested = RequestTimelinePlayback(
                timeline,
                actionContext.ActionId,
                timeline.Name,
                actionContext,
                loop ? TimelinePlaybackMode.Loop : TimelinePlaybackMode.Once,
                default,
                null,
                CharacterTimelinePlaybackSourceKind.AbilityRuntime,
                out handle);
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
            int deltaFrames)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline advancement requires an initialized CharacterTimelineHost.");
            TimelinePlaybackStatus status = m_Host.Service.GetTimelinePlaybackStatus(handle);
            if (status != TimelinePlaybackStatus.Requested && status != TimelinePlaybackStatus.Running)
                return new CharacterTimelinePendingAdvance(handle, 0, null, MapTerminalStatus(status));
            TimelineRuntimeAdvanceResult advance = m_Host.Advance(new TimelineRuntimePlaybackHandle(handle.Value), logicTick, deltaFrames);
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


        public bool RequestPreviewTimelinePlayback(
            TimelineData timeline,
            string sourceName,
            TimelinePlaybackMode playbackMode,
            out TimelinePlaybackHandle handle)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Timeline preview requires an initialized CharacterTimelineHost.");
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (m_PreviewHandle.IsValid)
                throw new InvalidOperationException("Timeline preview already has an active playback.");
            bool requested = RequestTimelinePlayback(
                timeline,
                "sceneplay.preview",
                sourceName ?? timeline.Name,
                default,
                playbackMode,
                default,
                null,
                CharacterTimelinePlaybackSourceKind.FixedPreview,
                out handle);
            if (requested)
                m_PreviewHandle = handle;
            return requested;
        }

        public bool CancelPreviewTimelinePlayback()
        {
            if (!IsInitialized || !m_PreviewHandle.IsValid)
                return false;
            m_Host.Service.CancelTimelinePlayback(
                m_PreviewHandle,
                new TimelinePlaybackStopContext(TimelinePlaybackStopCause.SelfAbort, 0));
            m_PreviewHandle = TimelinePlaybackHandle.Invalid;
            return true;
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

        public bool PreviewIsActive
        {
            get
            {
                if (!m_PreviewHandle.IsValid || m_Host == null)
                    return false;
                TimelinePlaybackStatus status = m_Host.Service.GetTimelinePlaybackStatus(m_PreviewHandle);
                return status == TimelinePlaybackStatus.Requested || status == TimelinePlaybackStatus.Running;
            }
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
                native.CursorFrame,
                native.Cycle,
                native.SectionId,
                native.ActiveClipIds,
                native.HasStopContext,
                MapSnapshotStopCause(native.StopContext.Cause),
                native.StopContext.LocalLogicTick,
                native.InitialBoundaryPending,
                request.TimelineId,
                request.Loop,
                request.ActionContext,
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
            TimelineRuntimePreparationResult preparation = m_Host.Prepare(
                snapshot.RequestId,
                timeline,
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
                snapshot.CursorFrame,
                snapshot.Cycle,
                snapshot.SectionId,
                snapshot.ActiveClipIds,
                Array.Empty<TimelineRuntimeTreeClipAssociation>(),
                snapshot.HasStopContext,
                new TimelinePlaybackStopContext(stopCause, snapshot.StopLocalLogicTick),
                snapshot.InitialBoundaryPending);
            TimelineRuntimeRestoreCandidate candidate = m_Host.PrepareRestore(native, preparation);
            TimelineRuntimePlaybackHandle restored = m_Host.ApplyRestore(candidate);
            var handle = new TimelinePlaybackHandle(restored.Value);
            m_ActivePlaybacks.Add(new ActivePlayback
            {
                Handle = handle,
                Timeline = timeline,
                SourceName = timeline.Name,
                SourceKind = CharacterTimelinePlaybackSourceKind.AbilityRuntime,
                CoreDriven = true
            });
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

        void Update()
        {
            if (!m_Initialized || m_Host == null || m_ActivePlaybacks.Count == 0)
                return;
            m_TickCounter++;
            int deltaFrames = Mathf.Max(1, Mathf.RoundToInt(Time.deltaTime * 60f));
            m_PlaybackScan.Clear();
            m_PlaybackScan.AddRange(m_ActivePlaybacks);
            for (int i = 0; i < m_PlaybackScan.Count; i++)
            {
                ActivePlayback active = m_PlaybackScan[i];
                TimelinePlaybackStatus status = m_Host.Service.GetTimelinePlaybackStatus(active.Handle);
                if (status != TimelinePlaybackStatus.Requested && status != TimelinePlaybackStatus.Running)
                {
                    m_ActivePlaybacks.Remove(active);
                    continue;
                }
                if (active.CoreDriven)
                    continue;
                m_Host.Service.Step(active.Handle, m_TickCounter, deltaFrames);
            }
        }

        void OnDestroy()
        {
            m_Host?.Dispose();
        }
    }
}
