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

        public CameraEffectRuntimeState Add(CameraEffectRequest request, string tag = null)
        {
            var state = new CameraEffectRuntimeState(request, tag);
            m_Active.Add(state);
            return state;
        }

        public void RemoveAt(int index) => m_Active.RemoveAt(index);

        public void ClearScope(CameraPresentationScopeKey scope)
        {
            for (int i = m_Active.Count - 1; i >= 0; i--)
                if (m_Active[i].Request.Scope.Equals(scope))
                    m_Active.RemoveAt(i);
        }

        public void ClearOverrideTracks(bool clearTracks, IReadOnlyList<string> tags)
        {
            if (!clearTracks && (tags == null || tags.Count == 0))
                return;
            for (int i = m_Active.Count - 1; i >= 0; i--)
            {
                CameraEffectRuntimeState state = m_Active[i];
                if (state.Request.Kind != CameraEffectKind.Override)
                    continue;
                if (clearTracks || Contains(tags, state.Tag))
                    m_Active.RemoveAt(i);
            }
        }

        public CameraEffectRuntimeState FindEvent(CameraEffectRequest request)
        {
            for (int i = 0; i < m_Active.Count; i++)
            {
                CameraEffectRequest active = m_Active[i].Request;
                if (active.Kind == request.Kind &&
                    active.Generation == request.Generation &&
                    active.SourceActionInstanceId == request.SourceActionInstanceId &&
                    active.Cycle == request.Cycle &&
                    string.Equals(active.SourceId, request.SourceId, StringComparison.Ordinal) &&
                    string.Equals(active.EventId, request.EventId, StringComparison.Ordinal))
                    return m_Active[i];
            }
            return null;
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
                bool matchesIdentity = effect.Request.Generation == generation &&
                    effect.Request.SourceActionInstanceId == sourceActionInstanceId;
                bool matchesScope = reason == CameraPresentationStopReason.EventRevoked
                    ? matchesEvent
                    : matchesSource;
                if (effect.Request.Generation != generation ||
                    effect.Request.SourceActionInstanceId != sourceActionInstanceId ||
                    effect.Request.Cycle != cycle ||
                    !matchesIdentity ||
                    !matchesScope)
                    continue;
                if (effect.Retired)
                    continue;
                effect.Retired = true;
                effect.RetireElapsed = 0f;
                effect.RetireStartElapsed = effect.Elapsed;
                effect.RetireReason = reason;
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
                    if (Compare(candidate.Request, selected.Request) <= 0)
                        continue;
                }
                selected = candidate;
            }
            return selected;
        }

        static bool Contains(IReadOnlyList<string> values, string value)
        {
            for (int i = 0; i < values.Count; i++)
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                    return true;
            return false;
        }

        static int Compare(CameraEffectRequest left, CameraEffectRequest right)
        {
            int result = left.Priority.CompareTo(right.Priority);
            if (result != 0)
                return result;
            result = left.Weight.CompareTo(right.Weight);
            if (result != 0)
                return result;
            result = left.Generation.CompareTo(right.Generation);
            if (result != 0)
                return result;
            result = left.SourceActionInstanceId.CompareTo(right.SourceActionInstanceId);
            if (result != 0)
                return result;
            result = left.Cycle.CompareTo(right.Cycle);
            if (result != 0)
                return result;
            result = string.CompareOrdinal(right.SourceId, left.SourceId);
            if (result != 0)
                return result;
            return string.CompareOrdinal(right.EventId, left.EventId);
        }
    }
}
