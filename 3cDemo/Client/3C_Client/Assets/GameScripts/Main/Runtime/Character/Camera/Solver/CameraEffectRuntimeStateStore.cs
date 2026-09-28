using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonCamera
{
    internal sealed class CameraEffectRuntimeStateStore
    {
        readonly List<CameraEffectRuntimeState> m_Active;
        readonly CameraEffectRuntimeState[] m_Pool;

        public CameraEffectRuntimeStateStore(int capacity)
        {
            m_Active = new List<CameraEffectRuntimeState>(capacity);
            m_Pool = new CameraEffectRuntimeState[capacity];
            for (int index = 0; index < capacity; index++)
                m_Pool[index] = new CameraEffectRuntimeState(default, string.Empty);
        }

        public IReadOnlyList<CameraEffectRuntimeState> Active => m_Active;

        public void Reset()
        {
            for (int index = 0; index < m_Active.Count; index++)
                m_Active[index].Reset(default, string.Empty);
            m_Active.Clear();
        }

        public CameraEffectRuntimeState Add(CameraEffectRequest request, string tag = null)
        {
            if (m_Active.Count == m_Pool.Length)
                throw new InvalidOperationException("Camera active effects and retiring tails exceed RequestCapacity.");
            CameraEffectRuntimeState state = m_Pool[m_Active.Count];
            state.Reset(request, tag);
            m_Active.Add(state);
            return state;
        }

        public void RemoveAt(int index)
        {
            CameraEffectRuntimeState released = m_Pool[index];
            int last = m_Active.Count - 1;
            for (int slot = index; slot < last; slot++)
                m_Pool[slot] = m_Pool[slot + 1];
            m_Pool[last] = released;
            m_Active.RemoveAt(index);
            released.Reset(default, string.Empty);
        }

        public void ClearScope(CameraPresentationScopeKey scope)
        {
            for (int i = m_Active.Count - 1; i >= 0; i--)
                if (m_Active[i].Request.Scope.Equals(scope))
                    RemoveAt(i);
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
                    RemoveAt(i);
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
                    active.EventId.Equals(request.EventId))
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
            EventId eventId,
            ulong generation,
            string sourceId,
            ulong sourceActionInstanceId,
            int cycle,
            CameraPresentationStopReason reason)
        {
            bool hasEventId = eventId.IsValid;
            bool hasSourceId = !string.IsNullOrEmpty(sourceId);
            for (int i = m_Active.Count - 1; i >= 0; i--)
            {
                CameraEffectRuntimeState effect = m_Active[i];
                bool matchesEvent = hasEventId &&
                    effect.Request.EventId.Equals(eventId);
                bool matchesSource = hasSourceId &&
                    string.Equals(effect.Request.SourceId, sourceId, StringComparison.Ordinal);
                bool matchesScope = reason == CameraPresentationStopReason.EventRevoked
                    ? matchesEvent
                    : matchesSource;
                if (effect.Request.Generation != generation ||
                    effect.Request.SourceActionInstanceId != sourceActionInstanceId ||
                    effect.Request.Cycle != cycle ||
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
            return right.EventId.CompareTo(left.EventId);
        }
    }
}
