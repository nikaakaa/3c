using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class CharacterCameraSequenceEvaluator
    {
        readonly CharacterCameraProjectionPayload m_Projection;
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
            m_CurrentSourceId = string.Empty;
            m_CurrentGeneration = 0;
            m_CurrentIsDefault = false;
            m_TransitionElapsed = 0f;
            m_TransitionDuration = 0f;
            m_RetireFrom = default;
            m_RetireElapsed = 0f;
            m_RetireDuration = 0f;
            m_Retiring = false;
            m_Yaw = 0f;
            m_Pitch = m_Projection.DefaultElevationAngle;
            m_Initialized = false;
        }

        public void Retire(string sourceId, ulong generation, float blendOutSeconds)
        {
            if (!m_Initialized || !m_LastPlan.Valid ||
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

            Vector2 look = response.Apply(input.LookInput);
            m_Yaw = Mathf.Repeat(m_Yaw + look.x * m_Projection.Input.Sensitivity.x, 360f);
            m_Pitch = Mathf.Clamp(
                m_Pitch - look.y * m_Projection.Input.Sensitivity.y,
                m_Projection.Input.PitchLimit.x,
                m_Projection.Input.PitchLimit.y);

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
                    CameraSequenceRequest defaultRequest = request;
                    CameraFramePlan retireTarget = BuildTargetPlan(
                        input,
                        defaultRequest,
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
                        m_CurrentSourceId = defaultRequest.SourceId;
                        m_CurrentGeneration = defaultRequest.Generation;
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
            CameraFramePlan target = BuildTargetPlan(
                input,
                request,
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

        CameraFramePlan BuildTargetPlan(
            in CameraFrameInput input,
            in CameraSequenceRequest request,
            CameraSequencePayload sequence,
            Vector2 look)
        {
            Vector3 anchor = input.BodyPosition;
            Vector3 aim = input.BodyPosition + input.BodyRotation *
                (Vector3.up * m_Projection.DefaultSphere.Height);
            bool aimPointIsExplicit = false;
            for (int i = 0; i < input.Targets.Count; i++)
            {
                CameraTargetSnapshot target = input.Targets[i];
                bool matchesDefaultBody = string.IsNullOrEmpty(request.TargetKey) &&
                    string.Equals(target.Key, CameraTargetBindingKeys.Body, StringComparison.Ordinal);
                bool matchesRequestedTarget = !string.IsNullOrEmpty(request.TargetKey) &&
                    string.Equals(target.Key, request.TargetKey, StringComparison.Ordinal);
                if (!target.Valid || !matchesDefaultBody && !matchesRequestedTarget)
                    continue;
                anchor = target.AnchorPoint;
                aim = target.AimPoint;
                aimPointIsExplicit = target.AimPointIsExplicit;
                break;
            }
            float radius = m_Projection.DefaultSphere.Radius;
            float fieldOfView = m_Projection.DefaultFieldOfView;
            Vector2 compositionOffset = Vector2.zero;
            IReadOnlyList<CameraOrbitPayload> orbitGroup = m_Projection.DefaultOrbitGroup;
            bool orbitGroupUsesAbsoluteRadius = false;
            float evaluatedYaw = m_Yaw;
            float evaluatedPitch = m_Pitch;
            for (int stageIndex = 0; stageIndex < sequence.Stages.Count; stageIndex++)
            {
                CameraSequenceStagePayload stage = sequence.Stages[stageIndex];
                switch (stage)
                {
                    case CameraFrameOnePointByHeightPayload byHeight:
                        if (!aimPointIsExplicit)
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
                        orbitGroup = byTrack.CameraOrbits;
                        orbitGroupUsesAbsoluteRadius = true;
                        radius = orbit.Radius;
                        if (!aimPointIsExplicit)
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
                request.IsDefault ? 1f : request.Weight,
                input.ResetHistory,
                true,
                radiusScale: 1f,
                orbitGroup: orbitGroup,
                orbitGroupUsesAbsoluteRadius: orbitGroupUsesAbsoluteRadius);
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
