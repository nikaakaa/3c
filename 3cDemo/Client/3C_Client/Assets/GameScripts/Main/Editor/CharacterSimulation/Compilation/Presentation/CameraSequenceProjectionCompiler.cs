using System;
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
            if (stage.MakeContextDependent)
                throw new InvalidOperationException(
                    $"Camera Sequence stage '{stage.StageId}' requires a formal context provider.");
            if (stage.PlayLength >= 0f)
                throw new InvalidOperationException(
                    $"Camera Sequence stage '{stage.StageId}' uses finite PlayLength without a formal stage clock consumer.");
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
                    var cameraOrbits = new CameraTrackOrbitPayload[byTrack.CameraOrbits.Count];
                    for (int i = 0; i < cameraOrbits.Length; i++)
                    {
                        CameraTrackOrbitDescriptor orbit = byTrack.CameraOrbits[i];
                        cameraOrbits[i] = new CameraTrackOrbitPayload(orbit.Height, orbit.Radius);
                    }
                    var screenOffsets = new Vector2[byTrack.ScreenOffsets.Count];
                    for (int i = 0; i < screenOffsets.Length; i++)
                        screenOffsets[i] = byTrack.ScreenOffsets[i];
                    return new CameraFrameOnePointByTrackPayload(
                        byTrack.StageId,
                        byTrack.MakeContextDependent,
                        byTrack.PlayLength,
                        cameraOrbits,
                        byTrack.AspectRatio,
                        byTrack.FieldOfView,
                        screenOffsets,
                        byTrack.ElevationRatio,
                        byTrack.PolarAngle);
                case CameraFrameTwoPointsStage twoPoints:
                    return CompileTwoPoints(twoPoints);
                case CameraFrameMultiplePointsStage multiplePoints:
                    return new CameraFrameMultiplePointsPayload(
                        multiplePoints.StageId,
                        multiplePoints.MakeContextDependent,
                        multiplePoints.PlayLength,
                        multiplePoints.Radius,
                        multiplePoints.HeightOffset,
                        multiplePoints.HeightRatio,
                        multiplePoints.PlayerHeight,
                        multiplePoints.AngleRange,
                        multiplePoints.FieldOfView,
                        multiplePoints.LayerMask,
                        multiplePoints.BeginCameraDataId,
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
                        euler.MakeContextDependent,
                        euler.PlayLength,
                        euler.Offset,
                        euler.FlipForward);
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
                stage.MakeContextDependent,
                stage.PlayLength,
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
                stage.PlayerHeight,
                stage.BeginCameraDataId);
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
                stage.MakeContextDependent,
                stage.PlayLength,
                stage.MainTargetSlotId,
                subTargetSlotIds,
                stage.FramePolicyId,
                stage.RotationPolicyId);
        }
    }
}
