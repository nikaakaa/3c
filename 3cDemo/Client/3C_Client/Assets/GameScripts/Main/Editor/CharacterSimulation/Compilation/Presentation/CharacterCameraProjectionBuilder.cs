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
                stages[i] = BuildStage(asset.Stages[i]);
            return new CameraSequencePayload(asset.SequenceId, asset.TimeDomain, stages);
        }

        static CameraSequenceStagePayload BuildStage(CameraSequenceStage stage)
        {
            stage.RequireValid(stage.GetType().Name);
            switch (stage)
            {
                case CameraFrameOnePointByHeightStage byHeight:
                    return new CameraFrameOnePointByHeightPayload(
                        byHeight.StageId,
                        byHeight.MakeContextDependent,
                        byHeight.PlayLength,
                        byHeight.EntityHeight,
                        byHeight.HeightRatio,
                        byHeight.FieldOfView,
                        byHeight.ScreenOffset);
                case CameraFrameOnePointByScreenOffsetStage byScreen:
                    return new CameraFrameOnePointByScreenOffsetPayload(
                        byScreen.StageId,
                        byScreen.MakeContextDependent,
                        byScreen.PlayLength,
                        byScreen.AspectRatio,
                        byScreen.FieldOfView,
                        byScreen.ScreenOffset,
                        byScreen.Radius);
                case CameraFrameOnePointByTrackStage byTrack:
                    return new CameraFrameOnePointByTrackPayload(
                        byTrack.StageId,
                        byTrack.MakeContextDependent,
                        byTrack.PlayLength,
                        byTrack.CameraOrbits
                            .Select(value => new CameraOrbitPayload(value.Height, value.Radius, value.ScreenY))
                            .ToArray(),
                        byTrack.AspectRatio,
                        byTrack.FieldOfView,
                        byTrack.ScreenOffset,
                        byTrack.ElevationRatio,
                        byTrack.PolarAngle);
                case CameraRotationEulerOffsetStage euler:
                    return new CameraRotationEulerOffsetPayload(
                        euler.StageId,
                        euler.MakeContextDependent,
                        euler.PlayLength,
                        euler.Offset,
                        euler.FlipForward);
                default:
                    throw new InvalidOperationException(
                        $"Camera Sequence stage '{stage.StageId}' kind '{stage.Kind}' has no closed source evaluator.");
            }
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
