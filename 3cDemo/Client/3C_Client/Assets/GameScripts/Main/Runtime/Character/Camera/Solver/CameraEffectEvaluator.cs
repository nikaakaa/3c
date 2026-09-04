using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class CameraEffectEvaluator
    {
        readonly CameraEffectRuntimeStateStore m_States =
            new CameraEffectRuntimeStateStore();
        readonly ICameraEffectOwner[] m_Owners;
        readonly List<CameraEffectContribution> m_Contributions =
            new List<CameraEffectContribution>();

        public CameraEffectEvaluator(CharacterCameraProjectionPayload projection)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
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
        }

        public void Retire(string eventId, ulong generation, string sourceId = null)
        {
            m_States.Retire(eventId, generation, sourceId);
        }

        public CameraFramePlan Resolve(
            CameraFramePlan basePlan,
            IReadOnlyList<CameraEffectRequest> newRequests,
            in CameraFrameInput input)
        {
            AddRequests(newRequests);
            CameraFramePlan plan = basePlan;
            for (int i = 0; i < m_Owners.Length; i++)
                plan = m_Owners[i].Apply(plan, m_States.Active, in input);
            m_Contributions.Clear();
            for (int i = 0; i < m_States.Active.Count; i++)
            {
                CameraEffectRuntimeState active = m_States.Active[i];
                ICameraEffectOwner owner = RequireOwner(active.Request.Kind);
                float remaining = active.Retired
                    ? Mathf.Max(0f, owner.RetireDuration(active) - active.RetireElapsed)
                    : float.PositiveInfinity;
                m_Contributions.Add(new CameraEffectContribution(
                    owner.Stage,
                    active.Request.ResourceId,
                    active.Request.Weight,
                    remaining,
                    active.Request.Priority,
                    true));
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
                if (!request.Active)
                    continue;
                ICameraEffectOwner owner = RequireOwner(request.Kind);
                if (!owner.UpdatesBySource && m_States.ContainsEvent(request))
                    continue;
                if (!owner.HasResource(request.ResourceId))
                    throw new InvalidOperationException(
                        $"Camera effect resource '{request.ResourceId}' is not present in the Projection.");
                if (owner.UpdatesBySource)
                {
                    CameraEffectRuntimeState existing = m_States.FindSource(
                        request.SourceId,
                        request.Generation);
                    if (existing != null)
                    {
                        existing.Request = request;
                        existing.Retired = false;
                        existing.RetireElapsed = 0f;
                        continue;
                    }
                }
                m_States.Add(request);
            }
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
                    active.RetireElapsed += delta;
                if (owner.IsExpired(active))
                    m_States.RemoveAt(i);
            }
        }

        ICameraEffectOwner RequireOwner(CameraEffectKind kind)
        {
            for (int i = 0; i < m_Owners.Length; i++)
                if (m_Owners[i].Kind == kind)
                    return m_Owners[i];
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "No camera effect owner is registered.");
        }
    }
}
