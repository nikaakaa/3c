using System;
using Cinemachine.Utility;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal static class CameraSequenceProjectionCompiler
    {
        public static CameraSequencePayload Compile(
            CameraSequenceAsset asset,
            CameraProjectionCompilationContext context)
        {
            asset.RequireValid();
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (asset.TimeDomain == CameraTimeDomain.OwnerScaled ||
                asset.TimeDomain == CameraTimeDomain.LocalAvatarScaled)
                throw new InvalidOperationException(
                    $"Camera Sequence '{asset.SequenceId}' uses '{asset.TimeDomain}', but the current Presentation Tick has no formal source for that time domain.");
            var stages = new CameraSequenceStagePayload[asset.Stages.Count];
            for (int i = 0; i < stages.Length; i++)
                stages[i] = CompileStage(asset.Stages[i], context);
            return new CameraSequencePayload(asset.SequenceId, asset.TimeDomain, stages);
        }

        static CameraSequenceStagePayload CompileStage(
            CameraSequenceStage stage,
            CameraProjectionCompilationContext context)
        {
            stage.RequireValid(stage.GetType().Name);
            switch (stage)
            {
                case CameraFrameOnePointByHeightStage byHeight:
                    return new CameraFrameOnePointByHeightPayload(
                        byHeight.StageId,
                        byHeight.EntityHeight,
                        byHeight.HeightRatio,
                        byHeight.FieldOfView,
                        byHeight.ScreenOffset);
                case CameraFrameOnePointByScreenOffsetStage byScreen:
                    return new CameraFrameOnePointByScreenOffsetPayload(
                        byScreen.StageId,
                        byScreen.AspectRatio,
                        byScreen.FieldOfView,
                        byScreen.ScreenOffset,
                        byScreen.Radius);
                case CameraFrameOnePointByTrackStage byTrack:
                    var cameraOrbits = new CameraTrackOrbitPayload[byTrack.CameraOrbits.Count];
                    var screenOffsets = new Vector2[byTrack.ScreenOffsets.Count];
                    var knots = new Vector4[cameraOrbits.Length];
                    var control1 = new Vector4[cameraOrbits.Length];
                    var control2 = new Vector4[cameraOrbits.Length];
                    for (int i = 0; i < cameraOrbits.Length; i++)
                    {
                        CameraTrackOrbitDescriptor orbit = byTrack.CameraOrbits[cameraOrbits.Length - 1 - i];
                        cameraOrbits[i] = new CameraTrackOrbitPayload(orbit.Height, orbit.Radius);
                        screenOffsets[i] = byTrack.ScreenOffsets[i];
                        knots[i] = new Vector4(orbit.Height, orbit.Radius, screenOffsets[i].x, screenOffsets[i].y);
                    }
                    SplineHelpers.ComputeSmoothControlPoints(ref knots, ref control1, ref control2);
                    return new CameraFrameOnePointByTrackPayload(
                        byTrack.StageId,
                        cameraOrbits,
                        byTrack.AspectRatio,
                        byTrack.FieldOfView,
                        screenOffsets,
                        control1,
                        control2,
                        byTrack.ElevationRatio,
                        byTrack.PolarAngle,
                        byTrack.CameraLocateRatio,
                        new CameraTrackOrbitPayload(byTrack.TopOrbit.Height, byTrack.TopOrbit.Radius),
                        byTrack.TopCurvature,
                        byTrack.FollowOffset,
                        byTrack.AimOffset);
                case CameraFrameTwoPointsStage twoPoints:
                    return CompileTwoPoints(twoPoints);
                case CameraFrameMultiplePointsStage multiplePoints:
                    return new CameraFrameMultiplePointsPayload(
                        multiplePoints.StageId,
                        multiplePoints.Radius,
                        multiplePoints.HeightOffset,
                        multiplePoints.HeightRatio,
                        multiplePoints.PlayerHeight,
                        multiplePoints.AngleRange,
                        multiplePoints.FieldOfView,
                        context.CompileCurve(context.RequireCurve(multiplePoints.DeltaHeightToPitch)),
                        CompileTwoPoints(multiplePoints.FallbackTwoPoints));
                case CameraTwoEntitiesFrameStage twoEntities:
                    return CompileEntityFrame(twoEntities);
                case CameraMultipleEntitiesFrameStage multipleEntities:
                    return CompileEntityFrame(multipleEntities);
                case CameraEntityFrameStage entity:
                    return CompileEntityFrame(entity);
                case CameraFixedInCoreStage fixedInCore:
                    throw new InvalidOperationException(
                        $"Camera FixedInCore stage '{fixedInCore.StageId}' requires a formal core-space context consumer.");
                case CameraHandleVolumeStage volume:
                    throw new InvalidOperationException(
                        $"Camera HandleVolume stage '{volume.StageId}' requires the formal environment volume contract.");
                case CameraRotationEulerOffsetStage euler:
                    return new CameraRotationEulerOffsetPayload(
                        euler.StageId,
                        euler.Offset,
                        euler.FlipForward);
                case CameraRotationLastStage last:
                    return new CameraRotationLastPayload(
                        last.StageId,
                        last.OverrideRotation,
                        last.Rotation,
                        last.UseRelativeYaw,
                        last.LastCameraDataId);
                default:
                    throw new InvalidOperationException(
                        $"Camera Sequence stage '{stage.StageId}' kind '{stage.Kind}' has no closed source evaluator.");
            }
        }

        static CameraFrameTwoPointsPayload CompileTwoPoints(CameraFrameTwoPointsStage stage)
        {
            stage.RequireValid(stage.GetType().Name);
            return new CameraFrameTwoPointsPayload(
                stage.StageId,
                stage.AspectRatio,
                stage.HeightRatio,
                stage.MinPlayerHeightRatio,
                stage.MaxPlayerHeightRatio,
                stage.FieldOfView,
                stage.Pitch,
                stage.MainHorizontalOffset,
                stage.SubHorizontalOffset,
                stage.MainVerticalOffset,
                stage.TargetVerticalOffset,
                stage.PitchRange,
                stage.PlayerHeight);
        }

        static CameraEntityFramePayload CompileEntityFrame(CameraEntityFrameStage stage)
        {
            stage.RequireValid(stage.GetType().Name);
            var subTargetSlotIds = new string[stage.SubTargetSlotIds.Count];
            for (int i = 0; i < subTargetSlotIds.Length; i++)
                subTargetSlotIds[i] = stage.SubTargetSlotIds[i] ?? string.Empty;
            return new CameraEntityFramePayload(
                stage.StageId,
                stage.Kind,
                stage.MainTargetSlotId,
                subTargetSlotIds);
        }
    }
}
