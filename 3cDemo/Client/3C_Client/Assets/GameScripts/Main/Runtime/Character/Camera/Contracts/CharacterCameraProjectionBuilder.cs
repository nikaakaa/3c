using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThirdPersonCamera
{
    public static class CharacterCameraProjectionBuilder
    {
        public static CharacterCameraProjectionPayload Build(CharacterCameraProfile profile)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            profile.RequireValid();
            var curves = new Dictionary<int, CameraCurveAsset>();
            for (int i = 0; i < profile.Curves.Count; i++)
            {
                CameraCurveAsset curve = profile.Curves[i];
                if (!curves.TryAdd(curve.GetInstanceID(), curve))
                    throw new InvalidOperationException($"Camera Profile '{profile.name}' contains a duplicated Curve reference.");
            }

            var sequenceMap = new Dictionary<string, CameraSequencePayload>(StringComparer.Ordinal);
            CameraSequencePayload defaultSequence = BuildSequence(profile.DefaultSequence, curves);
            sequenceMap.Add(defaultSequence.SequenceId, defaultSequence);
            for (int i = 0; i < profile.Sequences.Count; i++)
            {
                CameraSequencePayload sequence = BuildSequence(profile.Sequences[i], curves);
                if (!sequenceMap.TryAdd(sequence.SequenceId, sequence))
                    throw new InvalidOperationException($"Camera Profile '{profile.name}' contains duplicated Sequence identity '{sequence.SequenceId}'.");
            }

            var targetSlots = new CameraTargetSlotPayload[profile.TargetSlots.Count];
            for (int i = 0; i < targetSlots.Length; i++)
                targetSlots[i] = new CameraTargetSlotPayload(profile.TargetSlots[i]);

            var orbits = profile.DefaultOrbitGroup
                .Select(value => new CameraOrbitPayload(value.Height, value.Radius, value.ScreenY))
                .ToArray();
            return new CharacterCameraProjectionPayload(
                profile,
                defaultSequence,
                sequenceMap.Values.OrderBy(value => value.SequenceId, StringComparer.Ordinal).ToArray(),
                profile.OverrideTracks.Select(value => BuildOverride(value, curves)).ToArray(),
                profile.Zooms.Select(value => BuildZoom(value, curves)).ToArray(),
                profile.Stretches.Select(value => BuildStretch(value, curves)).ToArray(),
                profile.Shakes.Select(value => BuildShake(value, curves)).ToArray(),
                profile.Shots.Select(value => BuildShot(value, curves)).ToArray(),
                profile.Curves.Select(BuildCurve).ToArray(),
                new CameraOrbitPayload(profile.DefaultSphere.Height, profile.DefaultSphere.Radius, profile.DefaultSphere.ScreenY),
                orbits,
                targetSlots);
        }

        static CameraSequencePayload BuildSequence(
            CameraSequenceAsset asset,
            IReadOnlyDictionary<int, CameraCurveAsset> curves)
        {
            asset.RequireValid();
            var stages = new CameraSequenceStagePayload[asset.Stages.Count];
            for (int i = 0; i < stages.Length; i++)
                stages[i] = BuildStage(asset.Stages[i], curves);
            return new CameraSequencePayload(asset.SequenceId, asset.TimeDomain, stages);
        }

        static CameraSequenceStagePayload BuildStage(
            CameraSequenceStage stage,
            IReadOnlyDictionary<int, CameraCurveAsset> curves)
        {
            stage.RequireValid(stage.GetType().Name);
            float entityHeight = 0f;
            float heightRatio = 0f;
            float fieldOfView = 0f;
            Vector2 screenOffset = default;
            float aspectRatio = 0f;
            float radius = 0f;
            CameraOrbitPayload[] cameraOrbits = Array.Empty<CameraOrbitPayload>();
            float elevationRatio = 0f;
            float polarAngle = 0f;
            float minPlayerHeightRatio = 0f;
            float maxPlayerHeightRatio = 0f;
            float pitch = 0f;
            Vector2 mainHorizontalOffset = default;
            Vector2 subHorizontalOffset = default;
            float mainVerticalOffset = 0f;
            Vector2 targetVerticalOffset = default;
            Vector2 pitchRange = default;
            float playerHeight = 0f;
            string beginCameraDataId = string.Empty;
            float heightOffset = 0f;
            Vector2 angleRange = default;
            LayerMask layerMask = default;
            string deltaHeightToPitchCurveId = string.Empty;
            string fallbackStageId = string.Empty;
            string mainTargetSlotId = string.Empty;
            string[] subTargetSlotIds = Array.Empty<string>();
            string framePolicyId = string.Empty;
            string rotationPolicyId = string.Empty;
            string fixedPolicyId = string.Empty;
            string activeChannel = string.Empty;
            string collisionDataId = string.Empty;
            bool handleLineOfSightCollision = false;
            float nearClipPlane = 0f;
            Vector3 rotationOffset = default;
            bool flipForward = false;
            Vector3 overrideRotation = default;
            Vector3 rotation = default;
            bool useRelativeYaw = false;
            string lastCameraDataId = string.Empty;

            switch (stage)
            {
                case CameraFrameOnePointByHeightStage byHeight:
                    entityHeight = byHeight.EntityHeight;
                    heightRatio = byHeight.HeightRatio;
                    fieldOfView = byHeight.FieldOfView;
                    screenOffset = byHeight.ScreenOffset;
                    break;
                case CameraFrameOnePointByScreenOffsetStage byScreen:
                    aspectRatio = byScreen.AspectRatio;
                    fieldOfView = byScreen.FieldOfView;
                    screenOffset = byScreen.ScreenOffset;
                    radius = byScreen.Radius;
                    break;
                case CameraFrameOnePointByTrackStage byTrack:
                    cameraOrbits = byTrack.CameraOrbits
                        .Select(value => new CameraOrbitPayload(value.Height, value.Radius, value.ScreenY))
                        .ToArray();
                    aspectRatio = byTrack.AspectRatio;
                    fieldOfView = byTrack.FieldOfView;
                    screenOffset = byTrack.ScreenOffset;
                    elevationRatio = byTrack.ElevationRatio;
                    polarAngle = byTrack.PolarAngle;
                    break;
                case CameraFrameTwoPointsStage twoPoints:
                    aspectRatio = twoPoints.AspectRatio;
                    heightRatio = twoPoints.HeightRatio;
                    minPlayerHeightRatio = twoPoints.MinPlayerHeightRatio;
                    maxPlayerHeightRatio = twoPoints.MaxPlayerHeightRatio;
                    fieldOfView = twoPoints.FieldOfView;
                    pitch = twoPoints.Pitch;
                    mainHorizontalOffset = twoPoints.MainHorizontalOffset;
                    subHorizontalOffset = twoPoints.SubHorizontalOffset;
                    mainVerticalOffset = twoPoints.MainVerticalOffset;
                    targetVerticalOffset = twoPoints.TargetVerticalOffset;
                    pitchRange = twoPoints.PitchRange;
                    playerHeight = twoPoints.PlayerHeight;
                    beginCameraDataId = twoPoints.BeginCameraDataId;
                    break;
                case CameraFrameMultiplePointsStage multiplePoints:
                    radius = multiplePoints.Radius;
                    heightOffset = multiplePoints.HeightOffset;
                    heightRatio = multiplePoints.HeightRatio;
                    playerHeight = multiplePoints.PlayerHeight;
                    angleRange = multiplePoints.AngleRange;
                    fieldOfView = multiplePoints.FieldOfView;
                    layerMask = multiplePoints.LayerMask;
                    beginCameraDataId = multiplePoints.BeginCameraDataId;
                    deltaHeightToPitchCurveId = RequireCurve(multiplePoints.DeltaHeightToPitch, curves).CurveId;
                    fallbackStageId = multiplePoints.FallbackTwoPoints.StageId;
                    break;
                case CameraEntityFrameStage entity:
                    mainTargetSlotId = entity.MainTargetSlotId;
                    subTargetSlotIds = entity.SubTargetSlotIds.ToArray();
                    framePolicyId = entity.FramePolicyId;
                    rotationPolicyId = entity.RotationPolicyId;
                    break;
                case CameraFixedInCoreStage fixedInCore:
                    fixedPolicyId = fixedInCore.FixedPolicyId;
                    activeChannel = fixedInCore.ActiveChannel;
                    break;
                case CameraHandleVolumeStage volume:
                    collisionDataId = volume.CollisionDataId;
                    handleLineOfSightCollision = volume.HandleLineOfSightCollision;
                    nearClipPlane = volume.NearClipPlane;
                    break;
                case CameraRotationEulerOffsetStage euler:
                    rotationOffset = euler.Offset;
                    flipForward = euler.FlipForward;
                    break;
                case CameraRotationLastStage last:
                    overrideRotation = last.OverrideRotation;
                    rotation = last.Rotation;
                    useRelativeYaw = last.UseRelativeYaw;
                    lastCameraDataId = last.LastCameraDataId;
                    break;
                default:
                    throw new InvalidOperationException($"Camera Sequence stage type '{stage.GetType().Name}' is not supported.");
            }

            return new CameraSequenceStagePayload(
                stage.StageId,
                stage.Kind,
                stage.MakeContextDependent,
                stage.PlayLength,
                entityHeight,
                heightRatio,
                fieldOfView,
                screenOffset,
                aspectRatio,
                radius,
                cameraOrbits,
                elevationRatio,
                polarAngle,
                minPlayerHeightRatio,
                maxPlayerHeightRatio,
                pitch,
                mainHorizontalOffset,
                subHorizontalOffset,
                mainVerticalOffset,
                targetVerticalOffset,
                pitchRange,
                playerHeight,
                beginCameraDataId,
                heightOffset,
                angleRange,
                layerMask,
                deltaHeightToPitchCurveId,
                fallbackStageId,
                mainTargetSlotId,
                subTargetSlotIds,
                framePolicyId,
                rotationPolicyId,
                fixedPolicyId,
                activeChannel,
                collisionDataId,
                handleLineOfSightCollision,
                nearClipPlane,
                rotationOffset,
                flipForward,
                overrideRotation,
                rotation,
                useRelativeYaw,
                lastCameraDataId);
        }

        static CameraOverrideTrackPayload BuildOverride(
            CameraOverrideTrackAsset asset,
            IReadOnlyDictionary<int, CameraCurveAsset> curves)
        {
            asset.RequireValid();
            CameraCurveAsset blendIn = RequireCurve(asset.BlendInCurve, curves);
            CameraCurveAsset blendOut = RequireCurve(asset.BlendOutCurve, curves);
            return new CameraOverrideTrackPayload(
                asset.TrackId,
                CloneSettings(asset.Settings),
                asset.Priority,
                asset.Tag,
                asset.ClearTracks,
                asset.ClearTags.ToArray(),
                asset.Duration,
                asset.TimeDomain,
                asset.IgnoreLocalAvatar,
                asset.IgnoreOwnerTimeScale,
                asset.IgnoreWorldTimeScale,
                asset.BlendInSeconds,
                asset.BlendOutSeconds,
                BuildCurve(blendIn),
                BuildCurve(blendOut));
        }

        static CameraZoomPayload BuildZoom(CameraZoomAsset asset, IReadOnlyDictionary<int, CameraCurveAsset> curves)
        {
            asset.RequireValid();
            return new CameraZoomPayload(
                asset.ZoomId,
                BuildCurve(RequireCurve(asset.StartCurve, curves)),
                BuildCurve(RequireCurve(asset.EndCurve, curves)),
                asset.DataPriority,
                asset.IgnorePriorityInEndTime,
                asset.IgnoreWorldTimeScale,
                asset.IgnoreLocalAvatar,
                asset.IgnoreOwnerTimeScale,
                asset.LastTime,
                asset.StartTime,
                asset.StackingType,
                asset.FieldOfView,
                asset.FovVariationType,
                asset.DelayTime,
                asset.EndTime,
                asset.PlayStackingType);
        }

        static CameraStretchPayload BuildStretch(CameraStretchAsset asset, IReadOnlyDictionary<int, CameraCurveAsset> curves)
        {
            asset.RequireValid();
            return new CameraStretchPayload(
                asset.StretchId,
                BuildCurve(RequireCurve(asset.StartCurve, curves)),
                BuildCurve(RequireCurve(asset.EndCurve, curves)),
                asset.RuntimeCamFollowYPoints,
                asset.RotationZ,
                asset.IgnoreLocalAvatar,
                asset.IsAppliedElevationRatio,
                asset.IsAppliedEndElevationAngle,
                asset.PlayStackingType,
                asset.RuntimeCamFollowYOffsetRatio,
                asset.IsElevationAngleAbsolute,
                asset.IgnorePriorityInEndTime,
                asset.IgnoreWorldTimeScale,
                asset.IsEndElevationAngleAbsolute,
                asset.ElevationAngleMin,
                asset.RecoilTime,
                asset.DataPriority,
                asset.EndElevationAngleMax,
                asset.ApplyAimPointsCameraFollowYOffset,
                asset.ApplyRuntimeCamFollowYOffset,
                asset.CamOffsetSpace,
                asset.IgnoreOwnerTimeScale,
                asset.ElevationAngleMax,
                asset.HoldTime,
                asset.DelayTime,
                asset.CamOffset,
                asset.EndElevationAngleMin,
                asset.RadiusRatio,
                asset.StretchTime,
                asset.FovVariationType);
        }

        static CameraShakePayload BuildShake(CameraShakeAsset asset, IReadOnlyDictionary<int, CameraCurveAsset> curves)
        {
            asset.RequireValid();
            return new CameraShakePayload(
                asset.ShakeId,
                asset.ShakeType,
                asset.CameraShakePropertyConfig,
                asset.AngleVertical,
                asset.NoiseAngle,
                asset.RadiusLength,
                asset.DistanceToPlane,
                asset.NoiseRatio,
                asset.ShakeTotalTime,
                asset.Frequency,
                asset.RollAmplitude,
                asset.PitchAmplitude,
                asset.YawAmplitude,
                asset.ShakeCenterSpace,
                asset.RealtimeVibration,
                asset.DissipationMode,
                asset.ImpactRadius,
                asset.DissipationDistance,
                asset.CustomCurveKey,
                asset.FadeInDuration,
                BuildCurve(RequireCurve(asset.FadeInCurve, curves)),
                asset.FadeOutDuration,
                BuildCurve(RequireCurve(asset.FadeOutCurve, curves)),
                BuildCurve(RequireCurve(asset.Curve, curves)),
                asset.IgnoreTimeScale,
                asset.PlayStackingType,
                asset.PlayPriority,
                asset.DataPriority,
                asset.StandardConfigKey);
        }

        static CameraShotPayload BuildShot(CameraShotAsset asset, IReadOnlyDictionary<int, CameraCurveAsset> curves)
        {
            asset.RequireValid();
            return new CameraShotPayload(
                asset.ShotId,
                asset.CinePrefabPath,
                asset.FollowTargetSlotId,
                asset.LookAtTargetSlotId,
                asset.NearClipPlane,
                asset.FarClipPlane,
                asset.Duration,
                asset.TimeDomain,
                asset.IgnoreCameraCollision,
                asset.ApplyEntityTimeScale,
                asset.FollowOffset,
                asset.LookAtOffset,
                asset.OffsetRotation,
                asset.FieldOfView,
                BuildBlend(asset.BlendIn, curves),
                BuildBlend(asset.BlendOut, curves),
                asset.BlendWithIgnoreLookAtTarget,
                asset.Priority,
                asset.Tag);
        }

        static CameraShotBlendPayload BuildBlend(
            CameraShotBlendSettings source,
            IReadOnlyDictionary<int, CameraCurveAsset> curves)
        {
            source.RequireValid("Camera Shot blend");
            return new CameraShotBlendPayload(
                source.Duration,
                BuildCurve(RequireCurve(source.Curve, curves)),
                source.UseCoreSpace,
                source.UseDelta);
        }

        static CameraOverrideTrackSettings CloneSettings(CameraOverrideTrackSettings source)
        {
            var orbits = new CameraOrbitDescriptor[source.Orbits.Count];
            for (int i = 0; i < orbits.Length; i++)
            {
                CameraOrbitDescriptor orbit = source.Orbits[i];
                orbits[i] = new CameraOrbitDescriptor(orbit.Height, orbit.Radius, orbit.ScreenY);
            }
            return new CameraOverrideTrackSettings(
                new CameraOrbitDescriptor(source.TopOrbit.Height, source.TopOrbit.Radius, source.TopOrbit.ScreenY),
                orbits,
                source.ScreenY.ToArray(),
                source.FollowOffset,
                source.AimOffset,
                source.FieldOfView);
        }

        static CameraCurvePayload BuildCurve(CameraCurveAsset curve)
        {
            curve.RequireValid();
            return curve.Compile();
        }

        static CameraCurveAsset RequireCurve(
            CameraCurveAsset curve,
            IReadOnlyDictionary<int, CameraCurveAsset> curves)
        {
            if (!curve || !curves.ContainsKey(curve.GetInstanceID()))
                throw new InvalidOperationException($"Camera Curve '{curve?.name ?? "Missing"}' is not registered by its Profile.");
            return curve;
        }
    }
}
