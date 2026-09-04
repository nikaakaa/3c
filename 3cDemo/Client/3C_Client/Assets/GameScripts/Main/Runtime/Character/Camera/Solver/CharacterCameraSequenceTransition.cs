using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CharacterCameraSequenceTransition
    {
        readonly CharacterCameraProjectionPayload m_Projection;
        readonly CharacterCameraFramePlanner m_Planner;
        CameraFramePlan m_LastPlan;
        CameraFramePlan m_BlendFrom;
        string m_CurrentSequenceId = string.Empty;
        string m_CurrentSourceId = string.Empty;
        ulong m_CurrentGeneration;
        bool m_CurrentIsDefault;
        float m_TransitionElapsed;
        float m_TransitionDuration;
        CameraFramePlan m_RetireFrom;
        float m_RetireElapsed;
        float m_RetireDuration;
        bool m_Retiring;

        public CharacterCameraSequenceTransition(
            CharacterCameraProjectionPayload projection,
            CharacterCameraFramePlanner planner)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Planner = planner ?? throw new ArgumentNullException(nameof(planner));
        }

        public void Reset()
        {
            m_LastPlan = default;
            m_BlendFrom = default;
            m_CurrentSequenceId = string.Empty;
            m_CurrentSourceId = string.Empty;
            m_CurrentGeneration = 0;
            m_CurrentIsDefault = false;
            m_TransitionElapsed = 0f;
            m_TransitionDuration = 0f;
            m_RetireFrom = default;
            m_RetireElapsed = 0f;
            m_RetireDuration = 0f;
            m_Retiring = false;
        }

        public void Retire(string sourceId, ulong generation, float blendOutSeconds)
        {
            if (!m_LastPlan.Valid ||
                m_CurrentGeneration != generation ||
                !string.Equals(m_CurrentSourceId, sourceId, StringComparison.Ordinal))
                return;
            m_RetireFrom = m_LastPlan;
            m_RetireElapsed = 0f;
            m_RetireDuration = Mathf.Max(0f, blendOutSeconds);
            m_Retiring = true;
        }

        public CameraFramePlan Evaluate(
            in CameraFrameInput input,
            in CameraSequenceRequest request,
            Vector2 look)
        {
            if (m_Retiring)
            {
                if (!request.IsDefault)
                {
                    m_Retiring = false;
                    m_RetireFrom = default;
                    m_RetireElapsed = 0f;
                    m_RetireDuration = 0f;
                    m_CurrentSequenceId = string.Empty;
                    m_BlendFrom = default;
                    m_TransitionElapsed = 0f;
                    m_TransitionDuration = 0f;
                }
                else
                {
                    CameraFramePlan retireTarget = m_Planner.BuildTargetPlan(
                        in input,
                        in request,
                        m_Projection.DefaultSequence,
                        look);
                    float retireProgress = m_RetireDuration <= 0f
                        ? 1f
                        : Mathf.Clamp01(m_RetireElapsed / m_RetireDuration);
                    CameraFramePlan retiredResult = retireProgress >= 1f
                        ? retireTarget
                        : Blend(m_RetireFrom, retireTarget, retireProgress);
                    m_LastPlan = retiredResult;
                    m_RetireElapsed += ResolveSequenceDelta(
                        m_Projection.DefaultSequence,
                        in input);
                    if (retireProgress >= 1f || m_RetireElapsed >= m_RetireDuration)
                    {
                        m_Retiring = false;
                        m_RetireFrom = default;
                        m_RetireElapsed = 0f;
                        m_RetireDuration = 0f;
                        m_CurrentSequenceId = m_Projection.DefaultSequence.SequenceId;
                        m_CurrentSourceId = request.SourceId;
                        m_CurrentGeneration = request.Generation;
                        m_CurrentIsDefault = true;
                        m_BlendFrom = default;
                        m_TransitionElapsed = 0f;
                        m_TransitionDuration = 0f;
                    }
                    return retiredResult;
                }
            }

            string sequenceId = request.IsDefault
                ? m_Projection.DefaultSequence.SequenceId
                : request.SequenceId;
            CameraSequencePayload sequence = ResolveSequence(sequenceId);
            CameraFramePlan target = m_Planner.BuildTargetPlan(
                in input,
                in request,
                sequence,
                look);

            bool sequenceChanged = !string.Equals(sequence.SequenceId, m_CurrentSequenceId, StringComparison.Ordinal) ||
                request.IsDefault != m_CurrentIsDefault ||
                request.Generation != m_CurrentGeneration ||
                !string.Equals(request.SourceId, m_CurrentSourceId, StringComparison.Ordinal);
            if (sequenceChanged)
            {
                if (request.InterruptPolicy == CameraSequenceInterruptPolicy.Cut || !m_LastPlan.Valid)
                {
                    m_BlendFrom = default;
                    m_TransitionDuration = 0f;
                }
                else
                {
                    m_BlendFrom = m_LastPlan;
                    m_TransitionDuration = request.BlendInSeconds;
                }
                m_CurrentSequenceId = sequence.SequenceId;
                m_CurrentSourceId = request.SourceId;
                m_CurrentGeneration = request.Generation;
                m_CurrentIsDefault = request.IsDefault;
                m_TransitionElapsed = 0f;
            }
            else
            {
                m_TransitionElapsed += ResolveSequenceDelta(sequence, in input);
                m_CurrentSourceId = request.SourceId;
                m_CurrentGeneration = request.Generation;
                m_CurrentIsDefault = request.IsDefault;
            }

            float progress = m_TransitionDuration <= 0f
                ? 1f
                : Mathf.Clamp01(m_TransitionElapsed / m_TransitionDuration);
            CameraFramePlan result = progress >= 1f || !m_BlendFrom.Valid
                ? target
                : Blend(m_BlendFrom, target, progress);
            m_LastPlan = result;
            return result;
        }

        CameraSequencePayload ResolveSequence(string sequenceId)
        {
            if (string.Equals(sequenceId, m_Projection.DefaultSequence.SequenceId, StringComparison.Ordinal))
                return m_Projection.DefaultSequence;
            for (int i = 0; i < m_Projection.Sequences.Count; i++)
            {
                CameraSequencePayload sequence = m_Projection.Sequences[i];
                if (sequence != null && string.Equals(sequence.SequenceId, sequenceId, StringComparison.Ordinal))
                    return sequence;
            }
            throw new InvalidOperationException($"Camera Sequence '{sequenceId}' is not present in the Projection.");
        }

        static float ResolveSequenceDelta(
            CameraSequencePayload sequence,
            in CameraFrameInput input)
        {
            if (!Enum.IsDefined(typeof(CameraTimeDomain), sequence.TimeDomain))
                throw new InvalidOperationException($"Camera Sequence '{sequence.SequenceId}' has an invalid time domain.");
            return input.Delta(sequence.TimeDomain);
        }

        static CameraFramePlan Blend(CameraFramePlan from, CameraFramePlan to, float progress)
        {
            float t = Mathf.Clamp01(progress);
            return new CameraFramePlan(
                Vector3.LerpUnclamped(from.FollowPoint, to.FollowPoint, t),
                Vector3.LerpUnclamped(from.AimPoint, to.AimPoint, t),
                Mathf.LerpUnclamped(from.FieldOfView, to.FieldOfView, t),
                Mathf.LerpUnclamped(from.NearClipPlane, to.NearClipPlane, t),
                Mathf.LerpUnclamped(from.FarClipPlane, to.FarClipPlane, t),
                Vector2.LerpUnclamped(from.LookDelta, to.LookDelta, t),
                Mathf.LerpAngle(from.OrbitYaw, to.OrbitYaw, t),
                Mathf.LerpUnclamped(from.OrbitPitch, to.OrbitPitch, t),
                Mathf.LerpUnclamped(
                    ResolveEffectiveOrbitRadius(from),
                    ResolveEffectiveOrbitRadius(to),
                    t),
                to.SequenceId,
                to.SourceId,
                to.SourceActionInstanceId,
                t,
                to.ResetHistory,
                to.Valid,
                1f,
                Vector3.LerpUnclamped(from.CameraOffset, to.CameraOffset, t),
                Mathf.LerpUnclamped(from.RollDegrees, to.RollDegrees, t),
                BlendOrbitGroups(from, to, t),
                true);
        }

        static IReadOnlyList<CameraOrbitPayload> BlendOrbitGroups(
            CameraFramePlan from,
            CameraFramePlan to,
            float progress)
        {
            if (from.OrbitGroup.Count != to.OrbitGroup.Count)
                throw new InvalidOperationException("Camera Sequence transition changed orbit group capacity.");
            float fromScale = ResolveOrbitRadiusScale(from);
            float toScale = ResolveOrbitRadiusScale(to);
            if (ReferenceEquals(from.OrbitGroup, to.OrbitGroup) &&
                Mathf.Approximately(fromScale, 1f) && Mathf.Approximately(toScale, 1f))
                return to.OrbitGroup;
            var result = new CameraOrbitPayload[to.OrbitGroup.Count];
            for (int i = 0; i < result.Length; i++)
            {
                CameraOrbitPayload fromOrbit = from.OrbitGroup[i];
                CameraOrbitPayload toOrbit = to.OrbitGroup[i];
                result[i] = new CameraOrbitPayload(
                    Mathf.LerpUnclamped(fromOrbit.Height, toOrbit.Height, progress),
                    Mathf.LerpUnclamped(fromOrbit.Radius * fromScale, toOrbit.Radius * toScale, progress),
                    Mathf.LerpUnclamped(fromOrbit.ScreenY, toOrbit.ScreenY, progress));
            }
            return result;
        }

        static float ResolveOrbitRadiusScale(CameraFramePlan plan)
        {
            if (plan.OrbitGroup.Count == 0)
                throw new InvalidOperationException("Camera Sequence plan has no orbit group for blending.");
            CameraOrbitPayload centerOrbit = plan.OrbitGroup[plan.OrbitGroup.Count / 2];
            if (centerOrbit == null || centerOrbit.Radius <= 0f || plan.OrbitRadius <= 0f)
                throw new InvalidOperationException("Camera Sequence plan has an invalid orbit radius reference.");
            return plan.RadiusScale * (plan.OrbitGroupUsesAbsoluteRadius
                ? 1f
                : plan.OrbitRadius / centerOrbit.Radius);
        }

        static float ResolveEffectiveOrbitRadius(CameraFramePlan plan)
        {
            int centerIndex = plan.OrbitGroup.Count / 2;
            return plan.OrbitGroup[centerIndex].Radius * ResolveOrbitRadiusScale(plan);
        }
    }
}
