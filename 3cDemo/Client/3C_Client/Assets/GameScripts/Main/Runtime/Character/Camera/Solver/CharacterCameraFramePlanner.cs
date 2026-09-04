using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CharacterCameraFramePlanner
    {
        readonly CharacterCameraProjectionPayload m_Projection;
        float m_Yaw;
        float m_Pitch;

        public CharacterCameraFramePlanner(CharacterCameraProjectionPayload projection)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
        }

        public void Reset()
        {
            Reset(Quaternion.identity);
        }

        public void Reset(Quaternion bodyRotation)
        {
            m_Yaw = Mathf.Repeat(bodyRotation.eulerAngles.y, 360f);
            m_Pitch = m_Projection.DefaultElevationAngle;
        }

        public Vector2 ResolveLook(
            Vector2 lookInput,
            in CameraResponseRequest response)
        {
            Vector2 look = response.Apply(lookInput);
            m_Yaw = Mathf.Repeat(m_Yaw + look.x * m_Projection.Input.Sensitivity.x, 360f);
            m_Pitch = Mathf.Clamp(
                m_Pitch - look.y * m_Projection.Input.Sensitivity.y,
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
            float evaluatedRoll = 0f;
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

            Quaternion orbitRotation = Quaternion.Euler(evaluatedPitch, evaluatedYaw, 0f);
            aim += orbitRotation * new Vector3(compositionOffset.x, compositionOffset.y, 0f);
            CameraOrbitComposition orbitComposition = CameraOrbitComposition.Create(
                orbitGroup,
                radius,
                orbitGroupUsesAbsoluteRadius,
                evaluatedYaw,
                evaluatedPitch);
            return new CameraFramePlan(
                anchor,
                aim,
                new CameraLensPlan(
                    fieldOfView,
                    m_Projection.NearClipPlane,
                    m_Projection.FarClipPlane),
                look,
                orbitComposition,
                sequence.SequenceId,
                request.SourceId,
                request.SourceActionInstanceId,
                request.IsDefault ? 1f : request.Weight,
                input.ResetHistory,
                true,
                rollDegrees: evaluatedRoll);
        }
    }
}
