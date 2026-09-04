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
            m_Yaw = Mathf.Repeat(m_Yaw + look.x * m_Projection.Input.Sensitivity.x, 360f);
            m_Pitch = Mathf.Clamp(
                m_Pitch - look.y * m_Projection.Input.Sensitivity.y,
                m_Projection.Input.PitchLimit.x,
                m_Projection.Input.PitchLimit.y);

            string sequenceId = request.Active ? request.SequenceId : m_Projection.DefaultSequence.SequenceId;
            CameraSequencePayload sequence = ResolveSequence(sequenceId);
            CameraFramePlan target = BuildTargetPlan(
                input,
                request,
                sequence,
                look);

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
            Vector2 look)
        {
            Vector3 anchor = input.BodyPosition;
            Vector3 aim = input.BodyPosition + input.BodyRotation *
                (Vector3.up * m_Projection.DefaultSphere.Height);
            for (int i = 0; i < input.Targets.Count; i++)
            {
                CameraTargetSnapshot target = input.Targets[i];
                if (!target.Valid || !string.Equals(target.Key, request.TargetKey, StringComparison.Ordinal))
                    continue;
                anchor = target.AnchorPoint;
                aim = target.AimPoint;
                break;
            }
            float radius = m_Projection.DefaultSphere.Radius;
            float fieldOfView = m_Projection.DefaultFieldOfView;
            Vector2 compositionOffset = Vector2.zero;
            float evaluatedYaw = m_Yaw;
            float evaluatedPitch = m_Pitch;
            for (int stageIndex = 0; stageIndex < sequence.Stages.Count; stageIndex++)
            {
                CameraSequenceStagePayload stage = sequence.Stages[stageIndex];
                switch (stage)
                {
                    case CameraFrameOnePointByHeightPayload byHeight:
                        aim = anchor + input.BodyRotation *
                            (Vector3.up * (byHeight.EntityHeight * byHeight.HeightRatio));
                        fieldOfView = byHeight.FieldOfView;
                        compositionOffset = byHeight.ScreenOffset;
                        break;
                    case CameraFrameOnePointByScreenOffsetPayload byScreen:
                        radius = byScreen.Radius;
                        fieldOfView = byScreen.FieldOfView;
                        compositionOffset = byScreen.ScreenOffset;
                        break;
                    case CameraFrameOnePointByTrackPayload byTrack:
                        if (byTrack.CameraOrbits.Count == 0)
                            throw new InvalidOperationException($"Camera Sequence stage '{byTrack.StageId}' has no orbit data.");
                        int orbitIndex = Mathf.Clamp(
                            Mathf.RoundToInt(byTrack.ElevationRatio * (byTrack.CameraOrbits.Count - 1)),
                            0,
                            byTrack.CameraOrbits.Count - 1);
                        CameraOrbitPayload orbit = byTrack.CameraOrbits[orbitIndex];
                        radius = orbit.Radius;
                        aim = anchor + input.BodyRotation * (Vector3.up * orbit.Height);
                        fieldOfView = byTrack.FieldOfView;
                        compositionOffset = byTrack.ScreenOffset;
                        break;
                    case CameraRotationEulerOffsetPayload euler:
                        evaluatedPitch += euler.Offset.x;
                        evaluatedYaw = Mathf.Repeat(evaluatedYaw + euler.Offset.y, 360f);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Camera Sequence stage '{stage.StageId}' kind '{stage.Kind}' has no closed evaluator.");
                }
            }

            Quaternion orbitRotation = Quaternion.Euler(evaluatedPitch, evaluatedYaw, 0f);
            aim += orbitRotation * new Vector3(compositionOffset.x, compositionOffset.y, 0f);
            return new CameraFramePlan(
                anchor,
                aim,
                fieldOfView,
                m_Projection.NearClipPlane,
                m_Projection.FarClipPlane,
                look,
                evaluatedYaw,
                evaluatedPitch,
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
