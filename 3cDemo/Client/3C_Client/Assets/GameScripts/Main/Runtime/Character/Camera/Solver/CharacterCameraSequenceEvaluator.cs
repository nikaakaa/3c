using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class CharacterCameraSequenceEvaluator
    {
        readonly CharacterCameraProjectionPayload m_Projection;
        CameraFramePlan m_LastPlan;
        CameraFramePlan m_BlendFrom;
        string m_CurrentSequenceId = string.Empty;
        float m_TransitionElapsed;
        float m_TransitionDuration;
        float m_Yaw;
        float m_Pitch;
        bool m_Initialized;

        public CharacterCameraSequenceEvaluator(CharacterCameraProjectionPayload projection)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Projection.RequireValid();
        }

        public void Reset()
        {
            m_LastPlan = default;
            m_BlendFrom = default;
            m_CurrentSequenceId = string.Empty;
            m_TransitionElapsed = 0f;
            m_TransitionDuration = 0f;
            m_Yaw = 0f;
            m_Pitch = m_Projection.DefaultElevationAngle;
            m_Initialized = false;
        }

        public CameraFramePlan Evaluate(
            in CameraFrameInput input,
            in CameraSequenceRequest request,
            in CameraResponseRequest response)
        {
            if (input.ResetHistory || !m_Initialized)
            {
                m_Yaw = input.BodyRotation.eulerAngles.y;
                m_Pitch = m_Projection.DefaultElevationAngle;
                m_Initialized = true;
                m_LastPlan = default;
                m_BlendFrom = default;
                m_CurrentSequenceId = string.Empty;
                m_TransitionElapsed = 0f;
                m_TransitionDuration = 0f;
            }

            Vector2 look = response.Apply(input.LookInput);
            float lookDelta = input.Delta(CameraTimeDomain.PresentationScaled);
            m_Yaw = Mathf.Repeat(m_Yaw + look.x * m_Projection.Input.Sensitivity.x, 360f);
            m_Pitch = Mathf.Clamp(
                m_Pitch - look.y * m_Projection.Input.Sensitivity.y,
                m_Projection.Input.PitchLimit.x,
                m_Projection.Input.PitchLimit.y);

            string sequenceId = request.Active ? request.SequenceId : m_Projection.DefaultSequence.SequenceId;
            CameraSequencePayload sequence = ResolveSequence(sequenceId);
            CameraSequenceStagePayload stage = ResolveStage(sequence);
            CameraFramePlan target = BuildTargetPlan(
                input,
                request,
                sequence,
                stage,
                look,
                lookDelta);

            if (!string.Equals(sequence.SequenceId, m_CurrentSequenceId, StringComparison.Ordinal))
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
                m_TransitionElapsed = 0f;
            }
            else
            {
                m_TransitionElapsed += input.Delta(CameraTimeDomain.PresentationScaled);
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

        static CameraSequenceStagePayload ResolveStage(CameraSequencePayload sequence)
        {
            if (sequence == null || sequence.Stages.Count == 0)
                throw new InvalidOperationException("Camera Sequence has no stages.");
            return sequence.Stages[sequence.Stages.Count - 1];
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

        CameraFramePlan BuildTargetPlan(
            in CameraFrameInput input,
            in CameraSequenceRequest request,
            CameraSequencePayload sequence,
            CameraSequenceStagePayload stage,
            Vector2 look,
            float lookDelta)
        {
            Vector3 aim = input.BodyPosition + input.BodyRotation * (Vector3.up * m_Projection.DefaultSphere.Height);
            float radius = m_Projection.DefaultSphere.Radius;
            float fieldOfView = m_Projection.DefaultFieldOfView;
            Vector2 compositionOffset = Vector2.zero;
            switch (stage.Kind)
            {
                case CameraSequenceStageKind.FrameOnePointByHeight:
                    aim = input.BodyPosition + input.BodyRotation *
                        (Vector3.up * (stage.EntityHeight * stage.HeightRatio));
                    fieldOfView = stage.FieldOfView;
                    compositionOffset = stage.ScreenOffset;
                    break;
                case CameraSequenceStageKind.FrameOnePointByScreenOffset:
                    radius = stage.Radius;
                    fieldOfView = stage.FieldOfView;
                    compositionOffset = stage.ScreenOffset;
                    break;
                case CameraSequenceStageKind.FrameOnePointByTrack:
                    if (stage.CameraOrbits.Count == 0)
                        throw new InvalidOperationException($"Camera Sequence stage '{stage.StageId}' has no orbit data.");
                    int orbitIndex = Mathf.Clamp(
                        Mathf.RoundToInt(stage.ElevationRatio * (stage.CameraOrbits.Count - 1)),
                        0,
                        stage.CameraOrbits.Count - 1);
                    CameraOrbitPayload orbit = stage.CameraOrbits[orbitIndex];
                    radius = orbit.Radius;
                    aim += input.BodyRotation * (Vector3.up * orbit.Height);
                    fieldOfView = stage.FieldOfView;
                    compositionOffset = stage.ScreenOffset;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Camera Sequence stage '{stage.StageId}' kind '{stage.Kind}' has no closed evaluator.");
            }

            Quaternion orbitRotation = Quaternion.Euler(m_Pitch, m_Yaw, 0f);
            Vector3 follow = aim + orbitRotation * (Vector3.back * radius);
            follow += orbitRotation * new Vector3(compositionOffset.x, compositionOffset.y, 0f);
            return new CameraFramePlan(
                follow,
                aim,
                fieldOfView,
                m_Projection.NearClipPlane,
                m_Projection.FarClipPlane,
                look,
                m_Yaw,
                m_Pitch,
                radius,
                sequence.SequenceId,
                request.SourceId,
                request.SourceActionInstanceId,
                request.Active ? request.Weight : 1f,
                input.ResetHistory,
                true,
                m_Projection.DefaultSphere.Radius <= 0f
                    ? 1f
                    : radius / m_Projection.DefaultSphere.Radius);
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
                Mathf.LerpUnclamped(from.OrbitRadius, to.OrbitRadius, t),
                to.SequenceId,
                to.SourceId,
                to.SourceActionInstanceId,
                t,
                to.ResetHistory,
                to.Valid,
                Mathf.LerpUnclamped(from.RadiusScale, to.RadiusScale, t),
                Vector3.LerpUnclamped(from.CameraOffset, to.CameraOffset, t),
                Mathf.LerpUnclamped(from.RollDegrees, to.RollDegrees, t));
        }
    }
}
