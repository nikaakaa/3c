using System;
using System.Collections.Generic;

namespace ThirdPersonCamera
{
    internal sealed class CameraEffectRuntimeStateStore
    {
        readonly List<CameraEffectRuntimeState> m_Active =
            new List<CameraEffectRuntimeState>();

        public IReadOnlyList<CameraEffectRuntimeState> Active => m_Active;

        public void Reset() => m_Active.Clear();

        public void Add(CameraEffectRequest request) =>
            m_Active.Add(new CameraEffectRuntimeState(request));

        public void RemoveAt(int index) => m_Active.RemoveAt(index);

        public bool ContainsEvent(CameraEffectRequest request)
        {
            if (request.Kind != CameraEffectKind.Shake)
                return false;
            for (int i = 0; i < m_Active.Count; i++)
            {
                CameraEffectRequest active = m_Active[i].Request;
                if (active.Generation == request.Generation &&
                    active.SourceActionInstanceId == request.SourceActionInstanceId &&
                    active.Cycle == request.Cycle &&
                    string.Equals(active.EventId, request.EventId, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public CameraEffectRuntimeState FindSource(CameraEffectRequest request)
        {
            for (int i = 0; i < m_Active.Count; i++)
            {
                CameraEffectRuntimeState active = m_Active[i];
                if (active.Request.Kind == request.Kind &&
                    active.Request.Generation == request.Generation &&
                    active.Request.SourceActionInstanceId == request.SourceActionInstanceId &&
                    active.Request.Cycle == request.Cycle &&
                    string.Equals(active.Request.SourceId, request.SourceId, StringComparison.Ordinal))
                    return active;
            }
            return null;
        }

        public void Retire(
            string eventId,
            ulong generation,
            string sourceId,
            ulong sourceActionInstanceId,
            int cycle,
            CameraPresentationStopReason reason)
        {
            bool hasEventId = !string.IsNullOrEmpty(eventId);
            bool hasSourceId = !string.IsNullOrEmpty(sourceId);
            for (int i = m_Active.Count - 1; i >= 0; i--)
            {
                CameraEffectRuntimeState effect = m_Active[i];
                bool matchesEvent = hasEventId &&
                    string.Equals(effect.Request.EventId, eventId, StringComparison.Ordinal);
                bool matchesSource = hasSourceId &&
                    string.Equals(effect.Request.SourceId, sourceId, StringComparison.Ordinal);
                if (effect.Request.Generation != generation ||
                    effect.Request.SourceActionInstanceId != sourceActionInstanceId ||
                    reason != CameraPresentationStopReason.ForceTeardown &&
                    effect.Request.Cycle != cycle ||
                    !matchesEvent && !matchesSource)
                    continue;
                if (reason == CameraPresentationStopReason.ForceTeardown)
                {
                    m_Active.RemoveAt(i);
                    continue;
                }
                if (effect.Retired)
                    continue;
                effect.Retired = true;
                effect.RetireElapsed = 0f;
                effect.RetireStartElapsed = effect.Elapsed;
            }
        }

        public static CameraEffectRuntimeState Select(
            IReadOnlyList<CameraEffectRuntimeState> active,
            CameraEffectKind kind)
        {
            CameraEffectRuntimeState selected = null;
            for (int i = 0; i < active.Count; i++)
            {
                CameraEffectRuntimeState candidate = active[i];
                if (candidate.Request.Kind != kind)
                    continue;
                if (!candidate.Retired && !candidate.Request.Active)
                    continue;
                if (selected != null)
                {
                    if (selected.Retired != candidate.Retired)
                    {
                        if (selected.Retired)
                            selected = candidate;
                        continue;
                    }
                    if (candidate.Request.Priority <= selected.Request.Priority)
                        continue;
                }
                selected = candidate;
            }
            return selected;
        }
    }
}
