using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class CameraEffectEvaluator
    {
        readonly CharacterCameraProjectionPayload m_Projection;
        readonly CameraEffectRuntimeStateStore m_States;
        readonly ICameraEffectOwner[] m_Owners;
        readonly List<CameraEffectContribution> m_Contributions;
        readonly ReadOnlyCollection<CameraEffectContribution> m_ContributionView;
        readonly List<PendingRetirement> m_PendingRetirements;
        readonly List<CameraEffectRuntimeState> m_VisibleStates;
        readonly HashSet<CameraEffectEventKey> m_CompletedEvents;
        readonly List<CameraEffectEventKey> m_CompletedToRemove;
        readonly int m_Capacity;

        public CameraEffectEvaluator(CharacterCameraProjectionPayload projection, int capacity)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Projection = projection;
            m_Capacity = capacity;
            m_States = new CameraEffectRuntimeStateStore(capacity);
            m_Contributions = new List<CameraEffectContribution>(capacity);
            m_ContributionView = m_Contributions.AsReadOnly();
            m_PendingRetirements = new List<PendingRetirement>(capacity);
            m_VisibleStates = new List<CameraEffectRuntimeState>(capacity);
            m_CompletedEvents = new HashSet<CameraEffectEventKey>(capacity);
            m_CompletedToRemove = new List<CameraEffectEventKey>(capacity);
            m_Owners = new ICameraEffectOwner[]
            {
                new CameraOverrideEffectEvaluator(projection),
                new CameraZoomEffectEvaluator(projection),
                new CameraStretchEffectEvaluator(projection),
                new CameraShakeEffectEvaluator(projection),
                new CameraShotEffectEvaluator(projection)
            };
        }

        public ReadOnlyCollection<CameraEffectContribution> Contributions => m_ContributionView;

        public bool IsComplete(EventId eventId, ulong generation, string sourceId,
            ulong sourceActionInstanceId, int cycle) =>
            m_CompletedEvents.Contains(new CameraEffectEventKey(
                eventId, generation, sourceId, sourceActionInstanceId, cycle));

        public void Reset()
        {
            m_States.Reset();
            m_Contributions.Clear();
            m_PendingRetirements.Clear();
            m_VisibleStates.Clear();
            m_CompletedEvents.Clear();
        }

        public void RetireScope(
            CameraPresentationScopeKey scope,
            CameraPresentationStopReason reason)
        {
            for (int i = 0; i < m_States.Active.Count; i++)
            {
                CameraEffectRuntimeState state = m_States.Active[i];
                if (!state.Request.Scope.Equals(scope))
                    continue;
                m_States.Retire(
                    state.Request.EventId,
                    state.Request.Generation,
                    state.Request.SourceId,
                    state.Request.SourceActionInstanceId,
                    state.Request.Cycle,
                    reason);
            }
        }

        public void Retire(
            EventId eventId,
            ulong generation,
            string sourceId,
            ulong sourceActionInstanceId,
            int cycle,
            CameraPresentationStopReason reason)
        {
            if (m_PendingRetirements.Count == m_Capacity)
                throw new InvalidOperationException("Camera retirement count exceeds RequestCapacity.");
            m_States.Retire(
                eventId,
                generation,
                sourceId,
                sourceActionInstanceId,
                cycle,
                reason);
            m_CompletedEvents.Remove(new CameraEffectEventKey(
                eventId,
                generation,
                sourceId,
                sourceActionInstanceId,
                cycle));
            m_PendingRetirements.Add(new PendingRetirement(
                eventId,
                generation,
                sourceId,
                sourceActionInstanceId,
                cycle,
                reason));
        }

        public CameraFramePlan Resolve(
            CameraFramePlan basePlan,
            IReadOnlyList<CameraEffectRequest> newRequests,
            in CameraFrameInput input)
        {
            if (newRequests != null && newRequests.Count > m_Capacity)
                throw new InvalidOperationException("Camera effect requests exceed RequestCapacity.");
            m_CompletedToRemove.Clear();
            foreach (CameraEffectEventKey key in m_CompletedEvents)
                if (!ContainsEvent(newRequests, key))
                    m_CompletedToRemove.Add(key);
            for (int index = 0; index < m_CompletedToRemove.Count; index++)
                m_CompletedEvents.Remove(m_CompletedToRemove[index]);
            m_CompletedToRemove.Clear();
            AddRequests(newRequests);
            ApplyPendingRetirements();
            m_VisibleStates.Clear();
            for (int i = 0; i < m_States.Active.Count; i++)
                m_VisibleStates.Add(m_States.Active[i]);
            CameraFramePlan plan = basePlan;
            for (int i = 0; i < m_Owners.Length; i++)
                plan = m_Owners[i].Apply(plan, m_VisibleStates, in input);
            m_Contributions.Clear();
            for (int i = 0; i < m_States.Active.Count; i++)
            {
                CameraEffectRuntimeState active = m_States.Active[i];
                ICameraEffectOwner owner = RequireOwner(active.Request.Kind);
                bool visible = m_VisibleStates.Contains(active);
                float remaining = active.Retired
                    ? Mathf.Max(0f, owner.RetireDuration(active) - active.RetireElapsed)
                    : float.PositiveInfinity;
                m_Contributions.Add(new CameraEffectContribution(
                    owner.Stage,
                    active.Request.ResourceId,
                    active.Request.Weight,
                    remaining,
                    active.Request.Priority,
                    visible && active.Request.Active && !active.Retired,
                    active.Request.SourceId,
                    active.Request.Generation,
                    active.Request.SourceActionInstanceId,
                    active.Request.Cycle,
                    active.Request.EventId,
                    active.RetireReason));
            }
            Advance(in input, newRequests);
            return plan;
        }

        void AddRequests(IReadOnlyList<CameraEffectRequest> requests)
        {
            if (requests == null)
                return;
            for (int i = 0; i < requests.Count; i++)
            {
                CameraEffectRequest request = requests[i];
                ICameraEffectOwner owner = RequireOwner(request.Kind);
                if (m_CompletedEvents.Contains(CameraEffectEventKey.From(request)))
                    continue;
                if (owner.UpdatesBySource)
                {
                    CameraEffectRuntimeState existing = m_States.FindSource(request);
                    if (existing != null)
                    {
                        existing.Request = request;
                        existing.Tag = ResolveTag(request);
                        existing.Retired = false;
                        existing.RetireElapsed = 0f;
                        existing.RetireStartElapsed = 0f;
                        existing.RetireReason = CameraPresentationStopReason.NaturalComplete;
                        RemovePendingRetirement(request);
                        continue;
                    }
                }
                if (!request.Active)
                    continue;
                if (!owner.UpdatesBySource)
                {
                    CameraEffectRuntimeState existing = m_States.FindEvent(request);
                    if (existing != null)
                    {
                        existing.Request = request;
                        existing.Tag = ResolveTag(request);
                        existing.Retired = false;
                        existing.RetireElapsed = 0f;
                        existing.RetireStartElapsed = 0f;
                        existing.RetireReason = CameraPresentationStopReason.NaturalComplete;
                        RemovePendingRetirement(request);
                        continue;
                    }
                }
                if (!owner.HasResource(request.ResourceId))
                    throw new InvalidOperationException(
                        $"Camera effect resource '{request.ResourceId}' is not present in the Projection.");
                if (request.Kind == CameraEffectKind.Override &&
                    m_Projection.TryGetOverride(request.ResourceId, out CameraOverrideTrackPayload overridePayload))
                    m_States.ClearOverrideTracks(
                        overridePayload.ClearTracks,
                        overridePayload.ClearTags);
                RemovePendingRetirement(request);
                m_States.Add(request, ResolveTag(request));
            }
        }

        string ResolveTag(CameraEffectRequest request)
        {
            return request.Kind == CameraEffectKind.Override &&
                   m_Projection.TryGetOverride(request.ResourceId, out CameraOverrideTrackPayload overridePayload)
                ? overridePayload.Tag
                : string.Empty;
        }

        static bool ContainsEvent(IReadOnlyList<CameraEffectRequest> requests, CameraEffectEventKey key)
        {
            if (requests == null)
                return false;
            for (int index = 0; index < requests.Count; index++)
                if (CameraEffectEventKey.From(requests[index]).Equals(key))
                    return true;
            return false;
        }

        void Advance(in CameraFrameInput input, IReadOnlyList<CameraEffectRequest> requests)
        {
            for (int i = m_States.Active.Count - 1; i >= 0; i--)
            {
                CameraEffectRuntimeState active = m_States.Active[i];
                ICameraEffectOwner owner = RequireOwner(active.Request.Kind);
                float delta = owner.ResolveDelta(active, in input);
                active.Elapsed += delta;
                if (active.Retired)
                {
                    active.RetireElapsed += delta;
                    if (active.RetireElapsed >= owner.RetireDuration(active))
                        m_States.RemoveAt(i);
                }
                else if (owner.IsExpired(active))
                {
                    CameraEffectEventKey key = CameraEffectEventKey.From(active.Request);
                    if (ContainsEvent(requests, key))
                        m_CompletedEvents.Add(key);
                    m_States.RemoveAt(i);
                }
            }
        }

        void ApplyPendingRetirements()
        {
            for (int i = 0; i < m_PendingRetirements.Count; i++)
            {
                PendingRetirement retirement = m_PendingRetirements[i];
                m_States.Retire(
                    retirement.EventId,
                    retirement.Generation,
                    retirement.SourceId,
                    retirement.SourceActionInstanceId,
                    retirement.Cycle,
                    retirement.Reason);
            }
            m_PendingRetirements.Clear();
        }

        void RemovePendingRetirement(CameraEffectRequest request)
        {
            for (int i = m_PendingRetirements.Count - 1; i >= 0; i--)
            {
                PendingRetirement pending = m_PendingRetirements[i];
                if (pending.Generation == request.Generation &&
                    pending.SourceActionInstanceId == request.SourceActionInstanceId &&
                    pending.Cycle == request.Cycle &&
                    string.Equals(pending.SourceId, request.SourceId, StringComparison.Ordinal) &&
                    pending.EventId.Equals(request.EventId))
                    m_PendingRetirements.RemoveAt(i);
            }
        }

        ICameraEffectOwner RequireOwner(CameraEffectKind kind)
        {
            for (int i = 0; i < m_Owners.Length; i++)
                if (m_Owners[i].Kind == kind)
                    return m_Owners[i];
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "No camera effect owner is registered.");
        }

        readonly struct PendingRetirement
        {
            public PendingRetirement(
                EventId eventId,
                ulong generation,
                string sourceId,
                ulong sourceActionInstanceId,
                int cycle,
                CameraPresentationStopReason reason)
            {
                EventId = eventId;
                Generation = generation;
                SourceId = sourceId;
                SourceActionInstanceId = sourceActionInstanceId;
                Cycle = cycle;
                Reason = reason;
            }

            public EventId EventId { get; }
            public ulong Generation { get; }
            public string SourceId { get; }
            public ulong SourceActionInstanceId { get; }
            public int Cycle { get; }
            public CameraPresentationStopReason Reason { get; }
        }

        readonly struct CameraEffectEventKey : IEquatable<CameraEffectEventKey>
        {
            public CameraEffectEventKey(
                EventId eventId,
                ulong generation,
                string sourceId,
                ulong sourceActionInstanceId,
                int cycle)
            {
                EventId = eventId;
                Generation = generation;
                SourceId = sourceId ?? string.Empty;
                SourceActionInstanceId = sourceActionInstanceId;
                Cycle = cycle;
            }

            public EventId EventId { get; }
            public ulong Generation { get; }
            public string SourceId { get; }
            public ulong SourceActionInstanceId { get; }
            public int Cycle { get; }

            public static CameraEffectEventKey From(CameraEffectRequest request) =>
                new CameraEffectEventKey(
                    request.EventId,
                    request.Generation,
                    request.SourceId,
                    request.SourceActionInstanceId,
                    request.Cycle);

            public bool Equals(CameraEffectEventKey other) =>
                Generation == other.Generation &&
                SourceActionInstanceId == other.SourceActionInstanceId &&
                Cycle == other.Cycle &&
                EventId.Equals(other.EventId) &&
                string.Equals(SourceId, other.SourceId, StringComparison.Ordinal);

            public override bool Equals(object obj) =>
                obj is CameraEffectEventKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + EventId.GetHashCode();
                    hash = hash * 31 + Generation.GetHashCode();
                    hash = hash * 31 + StringComparer.Ordinal.GetHashCode(SourceId);
                    hash = hash * 31 + SourceActionInstanceId.GetHashCode();
                    hash = hash * 31 + Cycle;
                    return hash;
                }
            }
        }
    }
}
