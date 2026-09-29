using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CharacterCameraFramePlanner
    {
        const float ReferenceAspectRatio = 16f / 9f;

        readonly CharacterCameraProjectionPayload m_Projection;
        readonly List<CameraTargetSnapshot> m_FramingTargets =
            new List<CameraTargetSnapshot>();
        float m_InitialYawOffset;
        float m_InitialPitchOffset;
        float m_YawOffset;
        float m_PitchOffset;

        public CharacterCameraFramePlanner(CharacterCameraProjectionPayload projection)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
        }

        public void Reset()
        {
            m_YawOffset = Mathf.Repeat(m_InitialYawOffset, 360f);
            m_PitchOffset = Mathf.Clamp(
                m_InitialPitchOffset,
                m_Projection.Input.PitchLimit.x,
                m_Projection.Input.PitchLimit.y);
        }

        public void SetInitialState(in CameraInitialState state)
        {
            m_InitialYawOffset = state.Yaw;
            m_InitialPitchOffset = state.Pitch;
            Reset();
        }

        public Vector2 ResolveLook(
            Vector2 lookInput,
            in CameraResponseRequest response)
        {
            Vector2 look = response.Apply(lookInput);
            m_YawOffset = Mathf.Repeat(m_YawOffset + look.x * m_Projection.Input.Sensitivity.x, 360f);
            m_PitchOffset = Mathf.Clamp(
                m_PitchOffset - look.y * m_Projection.Input.Sensitivity.y,
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
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));
            CameraTargetSnapshot body = FindTarget(
                input.Targets,
                CameraTargetBindingKeys.Body,
                out CameraTargetSnapshot bodyTarget)
                ? bodyTarget
                : new CameraTargetSnapshot(
                    CameraTargetBindingKeys.Body,
                    input.BodyPosition,
                    input.BodyPosition,
                    Vector3.zero,
                    true,
                    false);
            CameraTargetSnapshot selected = body;
            if (!string.IsNullOrEmpty(request.TargetKey) &&
                !FindTarget(input.Targets, request.TargetKey, out selected))
            {
                throw new InvalidOperationException(
                    $"Camera Sequence '{sequence.SequenceId}' requires target '{request.TargetKey}', but the frame has no live target snapshot.");
            }

            Vector3 anchor = selected.AnchorPoint;
            Vector3 pivot = selected.AimPoint;
            bool pivotIsExplicit = selected.AimPointIsExplicit;
            float radius = m_Projection.DefaultSphere.Radius;
            float cameraLocateRatio = 1f;
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
                        offset = ResolveScreenOffset(byHeight.ScreenOffset, ReferenceAspectRatio);
                        break;
                    case CameraFrameOnePointByScreenOffsetPayload byScreen:
                        cameraLocateRatio = 1f;
                        radius = byScreen.Radius;
                        fieldOfView = byScreen.FieldOfView;
                        offset = ResolveScreenOffset(byScreen.ScreenOffset, byScreen.AspectRatio);
                        break;
                    case CameraFrameOnePointByTrackPayload byTrack:
                        Vector4 track = SampleTrack(byTrack, byTrack.ElevationRatio);
                        offset = ResolveScreenOffset(
                            new Vector2(track.z, track.w),
                            byTrack.AspectRatio);
                        cameraLocateRatio = byTrack.CameraLocateRatio;
                        radius = Mathf.Sqrt(track.x * track.x + track.y * track.y) * cameraLocateRatio;
                        evaluatedPitch = Mathf.Clamp(
                            Mathf.Atan2(track.x, track.y) * Mathf.Rad2Deg + m_PitchOffset,
                            m_Projection.Input.PitchLimit.x,
                            m_Projection.Input.PitchLimit.y);
                        evaluatedYaw = Mathf.Repeat(byTrack.PolarAngle + m_YawOffset, 360f);
                        evaluatedRoll = 0f;
                        fieldOfView = byTrack.FieldOfView;
                        break;
                    case CameraFrameTwoPointsPayload twoPoints:
                        cameraLocateRatio = 1f;
                        ResolveTwoPointTargets(
                            input.Targets,
                            selected,
                            out CameraTargetSnapshot secondary);
                        ApplyTwoPointFrame(
                            twoPoints,
                            selected,
                            secondary,
                            ref pivot,
                            ref radius,
                            ref offset,
                            ref evaluatedPitch,
                            ref fieldOfView);
                        pivotIsExplicit = true;
                        break;
                    case CameraFrameMultiplePointsPayload multiplePoints:
                        cameraLocateRatio = 1f;
                        BuildMultiplePointTargets(input.Targets, selected);
                        if (m_FramingTargets.Count < 2)
                        {
                            ResolveTwoPointTargets(
                                input.Targets,
                                selected,
                                out CameraTargetSnapshot fallbackSecondary);
                            ApplyTwoPointFrame(
                                multiplePoints.FallbackTwoPoints,
                                selected,
                                fallbackSecondary,
                                ref pivot,
                                ref radius,
                                ref offset,
                                ref evaluatedPitch,
                                ref fieldOfView);
                        }
                        else
                        {
                            ApplyMultiplePointFrame(
                                multiplePoints,
                                ref pivot,
                                ref radius,
                                ref evaluatedPitch,
                                ref fieldOfView);
                            offset = Vector2.zero;
                        }
                        pivotIsExplicit = true;
                        break;
                    case CameraEntityFramePayload entity:
                        ApplyEntityFrame(
                            input.Targets,
                            entity,
                            ref pivot,
                            ref radius,
                            ref cameraLocateRatio,
                            ref evaluatedPitch,
                            ref fieldOfView);
                        pivotIsExplicit = true;
                        break;
                    case CameraRotationEulerOffsetPayload euler:
                        if (euler.FlipForward)
                            throw new InvalidOperationException(
                                $"Camera Sequence stage '{euler.StageId}' uses unsupported FlipForward semantics.");
                        evaluatedPitch += euler.Offset.x;
                        evaluatedYaw = Mathf.Repeat(evaluatedYaw + euler.Offset.y, 360f);
                        evaluatedRoll += euler.Offset.z;
                        break;
                    case CameraRotationLastPayload last:
                        Vector3 rotation = last.OverrideRotation.sqrMagnitude > 0.000001f
                            ? last.OverrideRotation
                            : last.Rotation;
                        evaluatedPitch = rotation.x;
                        evaluatedYaw = last.UseRelativeYaw
                            ? Mathf.Repeat(rotation.y + m_YawOffset, 360f)
                            : Mathf.Repeat(rotation.y, 360f);
                        evaluatedRoll = rotation.z;
                        break;
                    case CameraFixedInCorePayload fixedInCore:
                        throw new InvalidOperationException(
                            $"Camera FixedInCore stage '{fixedInCore.StageId}' requires a formal core-space context consumer.");
                    case CameraHandleVolumePayload handleVolume:
                        throw new InvalidOperationException(
                            $"Camera HandleVolume stage '{handleVolume.StageId}' requires the formal environment volume contract.");
                    default:
                        throw new InvalidOperationException(
                            $"Camera Sequence stage '{stage.StageId}' kind '{stage.Kind}' has no closed evaluator.");
                }
            }

            Quaternion finalRotation = Quaternion.Euler(evaluatedPitch, evaluatedYaw, evaluatedRoll);
            return new CameraFramePlan(
                new CameraWorldBasicData(pivot, finalRotation, radius, offset, fieldOfView),
                new CameraLensPlan(m_Projection.NearClipPlane, m_Projection.FarClipPlane),
                look,
                sequence.SequenceId,
                request.SourceId,
                request.SourceActionInstanceId,
                request.IsDefault ? 1f : request.Weight,
                input.ResetHistory,
                true,
                cameraLocateRatio);
        }

        void ApplyEntityFrame(
            IReadOnlyList<CameraTargetSnapshot> targets,
            CameraEntityFramePayload stage,
            ref Vector3 pivot,
            ref float radius,
            ref float cameraLocateRatio,
            ref float evaluatedPitch,
            ref float fieldOfView)
        {
            if (!FindTarget(targets, stage.MainTargetSlotId, out CameraTargetSnapshot main))
                throw new InvalidOperationException(
                    $"Camera entity frame '{stage.StageId}' requires target slot '{stage.MainTargetSlotId}'.");
            fieldOfView = m_Projection.DefaultFieldOfView;
            if (stage.Kind == CameraSequenceStageKind.FrameOneEntity)
            {
                pivot = main.AimPoint;
                return;
            }

            m_FramingTargets.Clear();
            m_FramingTargets.Add(main);
            for (int i = 0; i < stage.SubTargetSlotIds.Count; i++)
            {
                string slotId = stage.SubTargetSlotIds[i];
                if (!FindTarget(targets, slotId, out CameraTargetSnapshot secondary))
                    throw new InvalidOperationException(
                        $"Camera entity frame '{stage.StageId}' requires target slot '{slotId}'.");
                AddUniqueTarget(secondary);
            }
            if (m_FramingTargets.Count < 2)
                throw new InvalidOperationException(
                    $"Camera entity frame '{stage.StageId}' requires at least two live target slots.");
            ApplyEntityPointFrame(
                ref pivot,
                ref radius,
                ref evaluatedPitch,
                ref fieldOfView);
            cameraLocateRatio = 1f;
        }

        void ApplyEntityPointFrame(
            ref Vector3 pivot,
            ref float radius,
            ref float evaluatedPitch,
            ref float fieldOfView)
        {
            Vector3 minimum = m_FramingTargets[0].AimPoint;
            Vector3 maximum = minimum;
            for (int i = 1; i < m_FramingTargets.Count; i++)
            {
                Vector3 point = m_FramingTargets[i].AimPoint;
                minimum = Vector3.Min(minimum, point);
                maximum = Vector3.Max(maximum, point);
            }
            pivot = (minimum + maximum) * 0.5f;
            float spread = Vector3.Distance(minimum, maximum);
            radius = Mathf.Max(m_Projection.CameraLocateRadius, spread * 0.5f);
            evaluatedPitch = Mathf.Clamp(
                m_PitchOffset,
                m_Projection.Input.PitchLimit.x,
                m_Projection.Input.PitchLimit.y);
            fieldOfView = m_Projection.DefaultFieldOfView;
        }

        void ApplyTwoPointFrame(
            CameraFrameTwoPointsPayload stage,
            CameraTargetSnapshot main,
            CameraTargetSnapshot secondary,
            ref Vector3 pivot,
            ref float radius,
            ref Vector2 offset,
            ref float evaluatedPitch,
            ref float fieldOfView)
        {
            float targetRatio = Mathf.Clamp01(stage.HeightRatio);
            Vector3 mainPoint = main.AimPoint + Vector3.up * stage.MainVerticalOffset;
            Vector3 secondaryPoint = secondary.AimPoint +
                Vector3.up * Mathf.Lerp(stage.TargetVerticalOffset.x, stage.TargetVerticalOffset.y, targetRatio);
            pivot = Vector3.Lerp(mainPoint, secondaryPoint, targetRatio);
            Vector3 separation = secondaryPoint - mainPoint;
            float verticalHalfAngle = Mathf.Tan(stage.FieldOfView * Mathf.Deg2Rad * 0.5f);
            float horizontalHalfAngle = verticalHalfAngle * Mathf.Max(0.0001f, stage.AspectRatio);
            float horizontalSpread = Vector3.ProjectOnPlane(separation, Vector3.up).magnitude;
            float verticalSpread = Mathf.Abs(separation.y);
            float fitRadius = Mathf.Max(
                horizontalSpread / Mathf.Max(0.0001f, 2f * horizontalHalfAngle),
                verticalSpread / Mathf.Max(0.0001f, 2f * verticalHalfAngle));
            radius = Mathf.Max(m_Projection.CameraLocateRadius, fitRadius);
            offset = Vector2.Lerp(
                ResolveScreenOffset(stage.MainHorizontalOffset, stage.AspectRatio),
                ResolveScreenOffset(stage.SubHorizontalOffset, stage.AspectRatio),
                targetRatio);
            evaluatedPitch = Mathf.Clamp(
                stage.Pitch + m_PitchOffset,
                Mathf.Max(stage.PitchRange.x, m_Projection.Input.PitchLimit.x),
                Mathf.Min(stage.PitchRange.y, m_Projection.Input.PitchLimit.y));
            fieldOfView = stage.FieldOfView;
        }

        void ApplyMultiplePointFrame(
            CameraFrameMultiplePointsPayload stage,
            ref Vector3 pivot,
            ref float radius,
            ref float evaluatedPitch,
            ref float fieldOfView)
        {
            Vector3 minimum = m_FramingTargets[0].AimPoint;
            Vector3 maximum = minimum;
            for (int i = 1; i < m_FramingTargets.Count; i++)
            {
                Vector3 point = m_FramingTargets[i].AimPoint;
                minimum = Vector3.Min(minimum, point);
                maximum = Vector3.Max(maximum, point);
            }
            float heightRatio = Mathf.Clamp01(stage.HeightRatio);
            pivot = Vector3.Lerp(minimum, maximum, 0.5f) +
                Vector3.up * (stage.HeightOffset + stage.PlayerHeight * heightRatio);
            float horizontalSpread = Vector3.ProjectOnPlane(maximum - minimum, Vector3.up).magnitude;
            float verticalSpread = Mathf.Abs(maximum.y - minimum.y);
            float verticalHalfAngle = Mathf.Tan(stage.FieldOfView * Mathf.Deg2Rad * 0.5f);
            float horizontalHalfAngle = verticalHalfAngle * Mathf.Max(0.0001f, 16f / 9f);
            float fitRadius = Mathf.Max(
                horizontalSpread / Mathf.Max(0.0001f, 2f * horizontalHalfAngle),
                verticalSpread / Mathf.Max(0.0001f, 2f * verticalHalfAngle));
            radius = Mathf.Max(stage.Radius, fitRadius);
            float normalizedHeight = Mathf.Clamp01(verticalSpread / Mathf.Max(0.0001f, stage.PlayerHeight));
            float pitchDelta = stage.DeltaHeightToPitch?.Evaluate(normalizedHeight) ?? 0f;
            evaluatedPitch = Mathf.Clamp(
                m_PitchOffset + pitchDelta,
                Mathf.Max(stage.AngleRange.x, m_Projection.Input.PitchLimit.x),
                Mathf.Min(stage.AngleRange.y, m_Projection.Input.PitchLimit.y));
            fieldOfView = stage.FieldOfView;
        }

        void ResolveTwoPointTargets(
            IReadOnlyList<CameraTargetSnapshot> targets,
            CameraTargetSnapshot selected,
            out CameraTargetSnapshot secondary)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                CameraTargetSnapshot candidate = targets[i];
                if (candidate.Valid && !string.Equals(candidate.Key, selected.Key, StringComparison.Ordinal))
                {
                    secondary = candidate;
                    return;
                }
            }
            throw new InvalidOperationException(
                "Camera two-point framing requires a second live target snapshot.");
        }

        void BuildMultiplePointTargets(
            IReadOnlyList<CameraTargetSnapshot> targets,
            CameraTargetSnapshot selected)
        {
            m_FramingTargets.Clear();
            AddUniqueTarget(selected);
            for (int i = 0; i < targets.Count; i++)
            {
                CameraTargetSnapshot candidate = targets[i];
                if (candidate.Valid)
                    AddUniqueTarget(candidate);
            }
        }

        void AddUniqueTarget(CameraTargetSnapshot target)
        {
            for (int i = 0; i < m_FramingTargets.Count; i++)
                if (string.Equals(m_FramingTargets[i].Key, target.Key, StringComparison.Ordinal))
                    return;
            m_FramingTargets.Add(target);
        }

        static bool FindTarget(
            IReadOnlyList<CameraTargetSnapshot> targets,
            string key,
            out CameraTargetSnapshot target)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                CameraTargetSnapshot candidate = targets[i];
                if (candidate.Valid && string.Equals(candidate.Key, key, StringComparison.Ordinal))
                {
                    target = candidate;
                    return true;
                }
            }
            target = default;
            return false;
        }

        static Vector2 ResolveScreenOffset(Vector2 offset, float aspectRatio)
        {
            if (!float.IsFinite(aspectRatio) || aspectRatio <= 0f)
                throw new InvalidOperationException("Camera screen offset requires a positive finite aspect ratio.");
            return new Vector2(offset.x * ReferenceAspectRatio / aspectRatio, offset.y);
        }

        static Vector4 SampleTrack(
            CameraFrameOnePointByTrackPayload track,
            float elevationRatio)
        {
            float position = Mathf.Clamp01(elevationRatio) * (track.CameraOrbits.Count - 1);
            int index = Mathf.Min(Mathf.FloorToInt(position), track.CameraOrbits.Count - 2);
            float t = position - index;
            float d = 1f - t;
            CameraTrackOrbitPayload left = track.CameraOrbits[index];
            CameraTrackOrbitPayload right = track.CameraOrbits[index + 1];
            Vector2 leftOffset = track.ScreenOffsets[index];
            Vector2 rightOffset = track.ScreenOffsets[index + 1];
            Vector4 p0 = new Vector4(left.Height, left.Radius, leftOffset.x, leftOffset.y);
            Vector4 p3 = new Vector4(right.Height, right.Radius, rightOffset.x, rightOffset.y);
            return d * d * d * p0 + 3f * d * d * t * track.TrackControl1[index]
                + 3f * d * t * t * track.TrackControl2[index] + t * t * t * p3;
        }
    }
}
