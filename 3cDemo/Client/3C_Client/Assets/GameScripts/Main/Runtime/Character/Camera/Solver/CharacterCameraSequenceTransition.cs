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
        ulong m_CurrentSourceActionInstanceId;
        int m_CurrentCycle;
        bool m_CurrentIsDefault;
        float m_TransitionElapsed;
        float m_TransitionDuration;
        CameraFramePlan m_RetireFrom;
        float m_RetireElapsed;
        float m_RetireDuration;
        CameraTimeDomain m_RetireTimeDomain;
        CameraPresentationStopReason m_RetireReason;
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
            m_CurrentSourceActionInstanceId = 0;
            m_CurrentCycle = 0;
            m_CurrentIsDefault = false;
            m_TransitionElapsed = 0f;
            m_TransitionDuration = 0f;
            m_RetireFrom = default;
            m_RetireElapsed = 0f;
            m_RetireDuration = 0f;
            m_RetireTimeDomain = CameraTimeDomain.PresentationScaled;
            m_RetireReason = CameraPresentationStopReason.NaturalComplete;
            m_Retiring = false;
        }

        public bool IsRetiring => m_Retiring;
        public CameraPresentationStopReason RetireReason => m_RetireReason;

        public bool Retire(
            string sourceId,
            ulong generation,
            ulong sourceActionInstanceId,
            int cycle,
            float blendOutSeconds,
            CameraPresentationStopReason reason)
        {
            if (!m_LastPlan.Valid ||
                m_CurrentGeneration != generation ||
                m_CurrentSourceActionInstanceId != sourceActionInstanceId ||
                m_CurrentCycle != cycle ||
                !string.Equals(m_CurrentSourceId, sourceId, StringComparison.Ordinal))
                return false;
            m_RetireTimeDomain = ResolveSequence(m_CurrentSequenceId).TimeDomain;
            m_RetireFrom = m_LastPlan;
            m_RetireElapsed = 0f;
            m_RetireDuration = blendOutSeconds > 0f
                ? blendOutSeconds
                : m_Projection.RotationTransitionSeconds;
            m_RetireReason = reason;
            m_Retiring = true;
            return true;
        }

        public void ForceTeardown(
            string sourceId,
            ulong generation,
            ulong sourceActionInstanceId)
        {
            if (!m_LastPlan.Valid ||
                m_CurrentGeneration != generation ||
                m_CurrentSourceActionInstanceId != sourceActionInstanceId ||
                !string.Equals(m_CurrentSourceId, sourceId, StringComparison.Ordinal))
                return;
            m_Retiring = false;
            m_RetireFrom = default;
            m_LastPlan = default;
            m_BlendFrom = default;
            m_TransitionElapsed = 0f;
            m_TransitionDuration = 0f;
            m_RetireReason = CameraPresentationStopReason.NaturalComplete;
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
                    m_RetireTimeDomain = CameraTimeDomain.PresentationScaled;
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
                    m_RetireElapsed += ResolveTimeDelta(m_RetireTimeDomain, in input);
                    if (retireProgress >= 1f || m_RetireElapsed >= m_RetireDuration)
                    {
                        m_Retiring = false;
                        m_RetireFrom = default;
                        m_RetireElapsed = 0f;
                        m_RetireDuration = 0f;
                        m_RetireTimeDomain = CameraTimeDomain.PresentationScaled;
                        m_CurrentSequenceId = m_Projection.DefaultSequence.SequenceId;
                        m_CurrentSourceId = request.SourceId;
                        m_CurrentGeneration = request.Generation;
                        m_CurrentSourceActionInstanceId = request.SourceActionInstanceId;
                        m_CurrentCycle = request.Cycle;
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
                request.SourceActionInstanceId != m_CurrentSourceActionInstanceId ||
                request.Cycle != m_CurrentCycle ||
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
                    m_TransitionDuration = request.BlendInSeconds > 0f
                        ? request.BlendInSeconds
                        : m_Projection.RotationTransitionSeconds;
                }
                m_CurrentSequenceId = sequence.SequenceId;
                m_CurrentSourceId = request.SourceId;
                m_CurrentGeneration = request.Generation;
                m_CurrentSourceActionInstanceId = request.SourceActionInstanceId;
                m_CurrentCycle = request.Cycle;
                m_CurrentIsDefault = request.IsDefault;
                m_TransitionElapsed = 0f;
            }
            else
            {
                m_TransitionElapsed += ResolveTimeDelta(sequence.TimeDomain, in input);
                m_CurrentSourceId = request.SourceId;
                m_CurrentGeneration = request.Generation;
                m_CurrentSourceActionInstanceId = request.SourceActionInstanceId;
                m_CurrentCycle = request.Cycle;
                m_CurrentIsDefault = request.IsDefault;
            }

            float progress = m_TransitionDuration <= 0f
                ? 1f
                : Mathf.Clamp01(m_TransitionElapsed / m_TransitionDuration);
            CameraFramePlan result = progress >= 1f || !m_BlendFrom.Valid
                ? target
                : Blend(m_BlendFrom, target, progress);
            if (!request.IsDefault && request.Weight < 1f)
            {
                CameraSequenceRequest defaultRequest = new CameraSequenceRequest(
                    m_Projection.DefaultSequence.SequenceId,
                    int.MinValue,
                    1f,
                    0f,
                    0f,
                    string.Empty,
                    "camera.default.sequence",
                    0,
                    0,
                    CameraSequenceInterruptPolicy.BlendOut,
                    true);
                CameraFramePlan defaultPlan = m_Planner.BuildTargetPlan(
                    in input,
                    in defaultRequest,
                    m_Projection.DefaultSequence,
                    look);
                result = Blend(defaultPlan, result, request.Weight);
            }
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

        static float ResolveTimeDelta(
            CameraTimeDomain timeDomain,
            in CameraFrameInput input)
        {
            if (timeDomain != CameraTimeDomain.PresentationScaled &&
                timeDomain != CameraTimeDomain.PresentationUnscaled &&
                timeDomain != CameraTimeDomain.OwnerScaled &&
                timeDomain != CameraTimeDomain.LocalAvatarScaled)
                throw new InvalidOperationException("Camera Sequence has an invalid time domain.");
            return input.Delta(timeDomain);
        }

        static CameraFramePlan Blend(CameraFramePlan from, CameraFramePlan to, float progress)
        {
            float t = Mathf.Clamp01(progress);
            CameraFramePlan result = new CameraFramePlan(
                CameraWorldBasicData.Lerp(from.WorldBasicData, to.WorldBasicData, t),
                Vector3.LerpUnclamped(from.AimOffset, to.AimOffset, t),
                new CameraLensPlan(
                    Mathf.LerpUnclamped(from.NearClipPlane, to.NearClipPlane, t),
                    Mathf.LerpUnclamped(from.FarClipPlane, to.FarClipPlane, t)),
                Vector2.LerpUnclamped(from.LookDelta, to.LookDelta, t),
                to.SequenceId,
                to.SourceId,
                to.SourceActionInstanceId,
                t,
                to.ResetHistory,
                to.Valid,
                Mathf.LerpUnclamped(from.CameraLocateRatio, to.CameraLocateRatio, t));
            return result
                .WithIgnoreCollision(from.IgnoreCollision || to.IgnoreCollision)
                .WithShotId(to.ShotId);
        }

    }
}
