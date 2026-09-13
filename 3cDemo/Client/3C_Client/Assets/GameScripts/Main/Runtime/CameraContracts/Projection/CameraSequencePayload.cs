using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraSequencePayload
    {
        [SerializeField] string m_SequenceId = string.Empty;
        [SerializeField] CameraTimeDomain m_TimeDomain;
        [SerializeReference] CameraSequenceStagePayload[] m_Stages = Array.Empty<CameraSequenceStagePayload>();

        public CameraSequencePayload(string sequenceId, CameraTimeDomain timeDomain, CameraSequenceStagePayload[] stages)
        {
            m_SequenceId = sequenceId ?? string.Empty;
            m_TimeDomain = timeDomain;
            m_Stages = stages ?? Array.Empty<CameraSequenceStagePayload>();
        }

        public string SequenceId => m_SequenceId ?? string.Empty;
        public CameraTimeDomain TimeDomain => m_TimeDomain;
        public IReadOnlyList<CameraSequenceStagePayload> Stages => m_Stages ?? Array.Empty<CameraSequenceStagePayload>();

        public void RequireValid(string source)
        {
            if (string.IsNullOrWhiteSpace(SequenceId) || !Enum.IsDefined(typeof(CameraTimeDomain), TimeDomain) ||
                Stages.Count == 0)
                throw new InvalidOperationException($"{source} contains an invalid Camera Sequence payload.");
            for (int i = 0; i < Stages.Count; i++)
            {
                CameraSequenceStagePayload stage = Stages[i];
                if (stage == null || string.IsNullOrWhiteSpace(stage.StageId) ||
                    !Enum.IsDefined(typeof(CameraSequenceStageKind), stage.Kind))
                    throw new InvalidOperationException($"{source}.Stages[{i}] is invalid.");
                RequireStage(stage, $"{source}.Stages[{i}]");
            }
        }

        static void RequireStage(CameraSequenceStagePayload stage, string source)
        {
            switch (stage)
            {
                case CameraFrameOnePointByHeightPayload value:
                    if (!float.IsFinite(value.EntityHeight) || value.EntityHeight <= 0f ||
                        !float.IsFinite(value.HeightRatio) || value.HeightRatio <= 0f ||
                        !float.IsFinite(value.FieldOfView) || value.FieldOfView <= 0f ||
                        !Finite(value.ScreenOffset))
                        throw new InvalidOperationException($"{source} contains invalid height framing.");
                    break;
                case CameraFrameOnePointByScreenOffsetPayload value:
                    if (!float.IsFinite(value.AspectRatio) || value.AspectRatio <= 0f ||
                        !float.IsFinite(value.FieldOfView) || value.FieldOfView <= 0f ||
                        !float.IsFinite(value.Radius) || value.Radius <= 0f || !Finite(value.ScreenOffset))
                        throw new InvalidOperationException($"{source} contains invalid screen framing.");
                    break;
                case CameraFrameOnePointByTrackPayload value:
                    if (value.CameraOrbits.Count != 3 || value.ScreenOffsets.Count != value.CameraOrbits.Count ||
                        !float.IsFinite(value.AspectRatio) || value.AspectRatio <= 0f ||
                        !float.IsFinite(value.FieldOfView) || value.FieldOfView <= 0f ||
                        !float.IsFinite(value.ElevationRatio) || !float.IsFinite(value.PolarAngle))
                        throw new InvalidOperationException($"{source} contains invalid track framing.");
                    for (int i = 0; i < value.CameraOrbits.Count; i++)
                    {
                        value.CameraOrbits[i]?.RequireValid($"{source}.CameraOrbits[{i}]");
                        if (value.CameraOrbits[i] == null || !Finite(value.ScreenOffsets[i]))
                            throw new InvalidOperationException($"{source} contains invalid track framing.");
                    }
                    break;
                case CameraFrameTwoPointsPayload value:
                    if (!float.IsFinite(value.AspectRatio) || value.AspectRatio <= 0f ||
                        !float.IsFinite(value.HeightRatio) || value.HeightRatio <= 0f ||
                        !float.IsFinite(value.MinPlayerHeightRatio) || !float.IsFinite(value.MaxPlayerHeightRatio) ||
                        value.MinPlayerHeightRatio > value.MaxPlayerHeightRatio ||
                        !float.IsFinite(value.FieldOfView) || value.FieldOfView <= 0f ||
                        !float.IsFinite(value.Pitch) || !Finite(value.MainHorizontalOffset) ||
                        !Finite(value.SubHorizontalOffset) || !float.IsFinite(value.MainVerticalOffset) ||
                        !Finite(value.TargetVerticalOffset) || !Finite(value.PitchRange) ||
                        value.PitchRange.x >= value.PitchRange.y || !float.IsFinite(value.PlayerHeight) ||
                        value.PlayerHeight <= 0f || string.IsNullOrWhiteSpace(value.BeginCameraDataId))
                        throw new InvalidOperationException($"{source} contains invalid two-point framing.");
                    break;
                case CameraFrameMultiplePointsPayload value:
                    if (!float.IsFinite(value.Radius) || value.Radius <= 0f || !float.IsFinite(value.HeightOffset) ||
                        !float.IsFinite(value.HeightRatio) || value.HeightRatio <= 0f || !float.IsFinite(value.PlayerHeight) ||
                        value.PlayerHeight <= 0f || !Finite(value.AngleRange) || value.AngleRange.x >= value.AngleRange.y ||
                        !float.IsFinite(value.FieldOfView) || value.FieldOfView <= 0f ||
                        string.IsNullOrWhiteSpace(value.BeginCameraDataId) || value.DeltaHeightToPitch == null ||
                        value.FallbackTwoPoints == null)
                        throw new InvalidOperationException($"{source} contains invalid multiple-point framing.");
                    value.DeltaHeightToPitch.RequireValid(source + ".DeltaHeightToPitch");
                    value.FallbackTwoPoints.RequireValid(source + ".FallbackTwoPoints");
                    break;
                case CameraEntityFramePayload value:
                    if (string.IsNullOrWhiteSpace(value.MainTargetSlotId))
                        throw new InvalidOperationException($"{source} contains invalid entity framing.");
                    for (int i = 0; i < value.SubTargetSlotIds.Count; i++)
                        if (string.IsNullOrWhiteSpace(value.SubTargetSlotIds[i]))
                            throw new InvalidOperationException($"{source}.SubTargetSlotIds[{i}] is invalid.");
                    break;
                case CameraFixedInCorePayload value:
                    if (string.IsNullOrWhiteSpace(value.FixedPolicyId) || string.IsNullOrWhiteSpace(value.ActiveChannel))
                        throw new InvalidOperationException($"{source} contains invalid fixed-space framing.");
                    break;
                case CameraHandleVolumePayload value:
                    if (string.IsNullOrWhiteSpace(value.CollisionDataId) || !float.IsFinite(value.NearClipPlane) ||
                        value.NearClipPlane < 0f)
                        throw new InvalidOperationException($"{source} contains invalid volume handling.");
                    break;
                case CameraRotationEulerOffsetPayload value:
                    if (!Finite(value.Offset) || value.FlipForward)
                        throw new InvalidOperationException($"{source} contains invalid Euler rotation offsets.");
                    break;
                case CameraRotationLastPayload value:
                    if (!Finite(value.OverrideRotation) || !Finite(value.Rotation) ||
                        string.IsNullOrWhiteSpace(value.LastCameraDataId))
                        throw new InvalidOperationException($"{source} contains invalid last-rotation settings.");
                    break;
                default:
                    throw new InvalidOperationException($"{source} has unsupported stage payload '{stage.GetType().Name}'.");
            }
        }

        static bool Finite(Vector2 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y);

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
