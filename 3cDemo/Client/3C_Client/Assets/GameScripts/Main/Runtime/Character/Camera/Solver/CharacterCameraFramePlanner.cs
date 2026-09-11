using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CharacterCameraFramePlanner
    {
        readonly CharacterCameraProjectionPayload m_Projection;
        float m_YawOffset;
        float m_PitchOffset;

        public CharacterCameraFramePlanner(CharacterCameraProjectionPayload projection)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
        }

        public void Reset()
        {
            m_YawOffset = 0f;
            m_PitchOffset = 0f;
        }

        public Vector2 ResolveLook(
            Vector2 lookInput,
            in CameraResponseRequest response)
        {
            Vector2 look = response.Apply(lookInput);
            m_YawOffset = Mathf.Repeat(m_YawOffset + look.x * m_Projection.Input.Sensitivity.x, 360f);
            m_PitchOffset = Mathf.Clamp(
                m_PitchOffset + look.y * m_Projection.Input.Sensitivity.y,
                m_Projection.Input.PitchLimit.x,
                m_Projection.Input.PitchLimit.y);
            return look;
        }

        public CameraFramePlan BuildTargetPlan(
            in CameraFrameInput input,
            in CameraSequenceRequest request,
            CameraSequencePayload sequence,
            Vector2 look)
        {
            Vector3 anchor = input.BodyPosition;
            Vector3 pivot = input.BodyPosition + input.BodyRotation *
                (Vector3.up * m_Projection.DefaultSphere.Height);
            bool pivotIsExplicit = false;
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
                pivot = target.AimPoint;
                pivotIsExplicit = target.AimPointIsExplicit;
                break;
            }
            float radius = m_Projection.DefaultSphere.Radius;
            float fieldOfView = m_Projection.DefaultFieldOfView;
            Vector2 offset = Vector2.zero;
            float evaluatedYaw = m_YawOffset;
            float evaluatedPitch = m_PitchOffset;
            float evaluatedRoll = 0f;
            for (int stageIndex = 0; stageIndex < sequence.Stages.Count; stageIndex++)
            {
                CameraSequenceStagePayload stage = sequence.Stages[stageIndex];
                switch (stage)
                {
                    case CameraFrameOnePointByHeightPayload byHeight:
                        if (!pivotIsExplicit)
                            pivot = anchor + input.BodyRotation *
                                (Vector3.up * (byHeight.EntityHeight * byHeight.HeightRatio));
                        fieldOfView = byHeight.FieldOfView;
                        offset = byHeight.ScreenOffset;
                        break;
                    case CameraFrameOnePointByScreenOffsetPayload byScreen:
                        radius = byScreen.Radius;
                        fieldOfView = byScreen.FieldOfView;
                        offset = byScreen.ScreenOffset;
                        break;
                    case CameraFrameOnePointByTrackPayload byTrack:
                        CameraTrackOrbitPayload orbit = SampleTrack(byTrack.CameraOrbits, byTrack.ElevationRatio);
                        offset = SampleTrack(byTrack.ScreenOffsets, byTrack.ElevationRatio);
                        radius = Mathf.Sqrt(orbit.Height * orbit.Height + orbit.Radius * orbit.Radius);
                        evaluatedPitch = Mathf.Clamp(
                            Mathf.Atan2(orbit.Height, orbit.Radius) * Mathf.Rad2Deg + m_PitchOffset,
                            m_Projection.Input.PitchLimit.x,
                            m_Projection.Input.PitchLimit.y);
                        evaluatedYaw = Mathf.Repeat(byTrack.PolarAngle + m_YawOffset, 360f);
                        evaluatedRoll = 0f;
                        fieldOfView = byTrack.FieldOfView;
                        break;
                    case CameraRotationEulerOffsetPayload euler:
                        if (euler.FlipForward)
                            throw new InvalidOperationException(
                                $"Camera Sequence stage '{euler.StageId}' uses unsupported FlipForward semantics.");
                        evaluatedPitch += euler.Offset.x;
                        evaluatedYaw = Mathf.Repeat(evaluatedYaw + euler.Offset.y, 360f);
                        evaluatedRoll += euler.Offset.z;
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Camera Sequence stage '{stage.StageId}' kind '{stage.Kind}' has no closed evaluator.");
                }
            }

            Quaternion rotation = Quaternion.Euler(evaluatedPitch, evaluatedYaw, evaluatedRoll);
            return new CameraFramePlan(
                new CameraWorldBasicData(pivot, rotation, radius, offset, fieldOfView),
                new CameraLensPlan(m_Projection.NearClipPlane, m_Projection.FarClipPlane),
                look,
                sequence.SequenceId,
                request.SourceId,
                request.SourceActionInstanceId,
                request.IsDefault ? 1f : request.Weight,
                input.ResetHistory,
                true);
        }

        static CameraTrackOrbitPayload SampleTrack(
            IReadOnlyList<CameraTrackOrbitPayload> orbits,
            float elevationRatio)
        {
            int index = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(elevationRatio) * 2f), 0, 2);
            return orbits[index];
        }

        static Vector2 SampleTrack(IReadOnlyList<Vector2> offsets, float elevationRatio)
        {
            int index = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(elevationRatio) * 2f), 0, 2);
            return offsets[index];
        }

    }
}
