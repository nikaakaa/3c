using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class CameraEffectEvaluator
    {
        readonly CharacterCameraProjectionPayload m_Projection;
        readonly CameraEffectRuntimeStateStore m_States =
            new CameraEffectRuntimeStateStore();
        readonly ICameraEffectOwner[] m_Owners;
        readonly List<CameraEffectContribution> m_Contributions =
            new List<CameraEffectContribution>();
        readonly List<PendingRetirement> m_PendingRetirements =
            new List<PendingRetirement>();
        readonly List<CameraEffectRuntimeState> m_VisibleStates =
            new List<CameraEffectRuntimeState>();

        public CameraEffectEvaluator(CharacterCameraProjectionPayload projection)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            m_Projection = projection;
            m_Owners = new ICameraEffectOwner[]
            {
                new CameraOverrideEffectEvaluator(projection),
                new CameraZoomEffectEvaluator(projection),
                new CameraStretchEffectEvaluator(projection),
                new CameraShakeEffectEvaluator(projection),
                new CameraShotEffectEvaluator(projection)
            };
        }

        public IReadOnlyList<CameraEffectContribution> Contributions => m_Contributions;

        public void Reset()
        {
            m_States.Reset();
            m_Contributions.Clear();
            m_PendingRetirements.Clear();
            m_VisibleStates.Clear();
        }

        public void StopScope(CameraPresentationScopeKey scope)
        {
            m_States.ClearScope(scope);
            for (int i = m_PendingRetirements.Count - 1; i >= 0; i--)
            {
                PendingRetirement pending = m_PendingRetirements[i];
                if (new CameraPresentationScopeKey(
                        pending.SourceId,
                        pending.Generation,
                        pending.SourceActionInstanceId).Equals(scope))
                    m_PendingRetirements.RemoveAt(i);
            }
        }

        public void Retire(
            string eventId,
            ulong generation,
            string sourceId,
            ulong sourceActionInstanceId,
            int cycle,
            CameraPresentationStopReason reason)
        {
            m_States.Retire(
                eventId,
                generation,
                sourceId,
                sourceActionInstanceId,
                cycle,
                reason);
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
            Advance(in input);
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

        void Advance(in CameraFrameInput input)
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
                    m_States.RemoveAt(i);
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
                    string.Equals(pending.EventId, request.EventId, StringComparison.Ordinal))
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
                string eventId,
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

            public string EventId { get; }
            public ulong Generation { get; }
            public string SourceId { get; }
            public ulong SourceActionInstanceId { get; }
            public int Cycle { get; }
            public CameraPresentationStopReason Reason { get; }
        }
    }
}
