using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class CameraStateResolver
    {
        CameraMode m_CurrentMode = CameraMode.FreeLook;
        string m_CurrentSourceId = string.Empty;
        float m_BlendElapsed;
        float m_BlendDuration;

        public void Reset()
        {
            m_CurrentMode = CameraMode.FreeLook;
            m_CurrentSourceId = string.Empty;
            m_BlendElapsed = 0f;
            m_BlendDuration = 0f;
        }

        public CameraStateRequest Resolve(
            IReadOnlyList<CameraStateRequest> requests,
            HashSet<ulong> terminalActionInstances,
            float deltaTime,
            out float blendProgress)
        {
            CameraStateRequest selected = CameraStateRequest.FreeLookBase;

            if (requests != null)
            {
                for (int i = 0; i < requests.Count; i++)
                {
                    CameraStateRequest candidate = requests[i];
                    if (!candidate.Active || IsTerminal(candidate.SourceActionInstanceId, terminalActionInstances))
                        continue;

                    if (ShouldReplace(selected, candidate))
                        selected = candidate;
                }
            }

            if (selected.Mode != m_CurrentMode || selected.SourceId != m_CurrentSourceId)
            {
                m_CurrentMode = selected.Mode;
                m_CurrentSourceId = selected.SourceId;
                m_BlendElapsed = 0f;
                m_BlendDuration = selected.BlendInSeconds;
            }
            else
            {
                m_BlendElapsed += Mathf.Max(0f, deltaTime);
            }

            blendProgress = m_BlendDuration <= 0f ? 1f : Mathf.Clamp01(m_BlendElapsed / m_BlendDuration);
            return selected;
        }

        static bool ShouldReplace(CameraStateRequest selected, CameraStateRequest candidate)
        {
            if (!selected.Active)
                return true;
            if (candidate.Priority != selected.Priority)
                return candidate.Priority > selected.Priority;
            if (!Mathf.Approximately(candidate.Weight, selected.Weight))
                return candidate.Weight > selected.Weight;
            return false;
        }

        static bool IsTerminal(ulong actionInstanceId, HashSet<ulong> terminalActionInstances)
        {
            return actionInstanceId != 0 &&
                   terminalActionInstances != null &&
                   terminalActionInstances.Contains(actionInstanceId);
        }
    }

    public sealed class CameraResponsePolicyResolver
    {
        public CameraResponsePolicy Resolve(
            CameraStateRequest selectedState,
            IReadOnlyList<CameraResponsePolicy> policies,
            HashSet<ulong> terminalActionInstances)
        {
            CameraResponsePolicy selected = DefaultFor(selectedState);
            if (policies == null)
                return selected;

            for (int i = 0; i < policies.Count; i++)
            {
                CameraResponsePolicy candidate = policies[i];
                if (!candidate.Active || IsTerminal(candidate.SourceActionInstanceId, terminalActionInstances))
                    continue;

                if (ShouldReplace(selected, candidate))
                    selected = candidate;
            }

            return selected;
        }

        static CameraResponsePolicy DefaultFor(CameraStateRequest state)
        {
            if (state.Mode == CameraMode.SkillCloseup)
            {
                return new CameraResponsePolicy(
                    CameraLookResponseMode.Suppressed,
                    0f,
                    0f,
                    0f,
                    state.Priority,
                    1f,
                    state.SourceId,
                    state.SourceActionInstanceId);
            }

            return CameraResponsePolicy.Full;
        }

        static bool ShouldReplace(CameraResponsePolicy selected, CameraResponsePolicy candidate)
        {
            if (!selected.Active)
                return true;
            if (candidate.Priority != selected.Priority)
                return candidate.Priority > selected.Priority;
            return candidate.Weight > selected.Weight;
        }

        static bool IsTerminal(ulong actionInstanceId, HashSet<ulong> terminalActionInstances)
        {
            return actionInstanceId != 0 &&
                   terminalActionInstances != null &&
                   terminalActionInstances.Contains(actionInstanceId);
        }
    }

    public sealed class CameraModifierResolver
    {
        static readonly CameraCueKind[] ApplyOrder =
        {
            CameraCueKind.Shake,
            CameraCueKind.FovKick,
            CameraCueKind.Recoil,
            CameraCueKind.CollisionCorrection,
            CameraCueKind.Custom
        };

        readonly List<ActiveCameraCue> m_ActiveCues = new List<ActiveCameraCue>();
        readonly List<CameraCue> m_DebugCues = new List<CameraCue>();

        public IReadOnlyList<CameraCue> DebugCues => m_DebugCues;

        public void Reset()
        {
            m_ActiveCues.Clear();
            m_DebugCues.Clear();
        }

        public void DiscardTerminal(HashSet<ulong> terminalActionInstances)
        {
            if (terminalActionInstances == null || terminalActionInstances.Count == 0)
                return;

            for (int i = m_ActiveCues.Count - 1; i >= 0; i--)
            {
                if (IsTerminal(m_ActiveCues[i].Cue.SourceActionInstanceId, terminalActionInstances))
                    m_ActiveCues.RemoveAt(i);
            }
            RebuildDebugCues();
        }

        public CameraPosePlan Resolve(
            CameraPosePlan basePlan,
            IReadOnlyList<CameraCue> newCues,
            HashSet<ulong> terminalActionInstances,
            float deltaTime)
        {
            AddNewCues(newCues, terminalActionInstances);
            UpdateCues(deltaTime, terminalActionInstances);

            CameraPosePlan plan = basePlan;
            for (int orderIndex = 0; orderIndex < ApplyOrder.Length; orderIndex++)
            {
                CameraCueKind cueKind = ApplyOrder[orderIndex];
                for (int i = 0; i < m_ActiveCues.Count; i++)
                {
                    CameraCue cue = m_ActiveCues[i].Cue;
                    if (cue.CueKind == cueKind)
                        plan = ApplyCue(plan, cue);
                }
            }

            RebuildDebugCues();
            return plan;
        }

        void AddNewCues(IReadOnlyList<CameraCue> cues, HashSet<ulong> terminalActionInstances)
        {
            if (cues == null)
                return;

            for (int i = 0; i < cues.Count; i++)
            {
                CameraCue cue = cues[i];
                if (!cue.Active || IsTerminal(cue.SourceActionInstanceId, terminalActionInstances))
                    continue;

                m_ActiveCues.Add(new ActiveCameraCue(cue));
            }
        }

        void UpdateCues(float deltaTime, HashSet<ulong> terminalActionInstances)
        {
            float dt = Mathf.Max(0f, deltaTime);
            for (int i = m_ActiveCues.Count - 1; i >= 0; i--)
            {
                ActiveCameraCue active = m_ActiveCues[i];
                if (IsTerminal(active.Cue.SourceActionInstanceId, terminalActionInstances))
                {
                    m_ActiveCues.RemoveAt(i);
                    continue;
                }

                active.RemainingSeconds -= dt;
                if (active.RemainingSeconds <= 0f)
                    m_ActiveCues.RemoveAt(i);
            }
        }

        static CameraPosePlan ApplyCue(CameraPosePlan plan, CameraCue cue)
        {
            float intensity = cue.Intensity;
            float fieldOfView = plan.FieldOfView;

            switch (cue.CueKind)
            {
                case CameraCueKind.FovKick:
                    fieldOfView += intensity;
                    break;
            }

            return new CameraPosePlan(
                plan.Mode,
                plan.FollowPoint,
                plan.AimPoint,
                fieldOfView,
                plan.ResponsePolicy,
                plan.LookDelta,
                plan.SourceId,
                plan.SourceActionInstanceId,
                plan.BlendProgress,
                plan.Valid);
        }

        void RebuildDebugCues()
        {
            m_DebugCues.Clear();
            for (int i = 0; i < m_ActiveCues.Count; i++)
                m_DebugCues.Add(m_ActiveCues[i].Cue);
        }

        static bool IsTerminal(ulong actionInstanceId, HashSet<ulong> terminalActionInstances)
        {
            return actionInstanceId != 0 &&
                   terminalActionInstances != null &&
                   terminalActionInstances.Contains(actionInstanceId);
        }

        sealed class ActiveCameraCue
        {
            public ActiveCameraCue(CameraCue cue)
            {
                Cue = cue;
                RemainingSeconds = cue.DurationSeconds > 0f ? cue.DurationSeconds : 0.016f;
            }

            public CameraCue Cue { get; }
            public float RemainingSeconds;
        }
    }
}
