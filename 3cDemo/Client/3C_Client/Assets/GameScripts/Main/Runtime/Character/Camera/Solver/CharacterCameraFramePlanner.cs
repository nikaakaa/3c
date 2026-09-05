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

        public State CaptureState() => new State(m_Yaw, m_Pitch);

        public void RestoreState(State state)
        {
            m_Yaw = state.Yaw;
            m_Pitch = state.Pitch;
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
            float evaluatedYaw = m_Yaw;
            float evaluatedPitch = m_Pitch;
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
                        evaluatedPitch = Mathf.Atan2(orbit.Height, orbit.Radius) * Mathf.Rad2Deg;
                        evaluatedYaw = byTrack.PolarAngle;
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

        public readonly struct State
        {
            public State(float yaw, float pitch)
            {
                Yaw = yaw;
                Pitch = pitch;
            }

            public float Yaw { get; }
            public float Pitch { get; }
        }

    }
}
