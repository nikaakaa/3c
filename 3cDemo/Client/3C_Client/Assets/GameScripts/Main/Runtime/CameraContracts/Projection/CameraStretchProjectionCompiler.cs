using System;

namespace ThirdPersonCamera
{
    internal static class CameraStretchProjectionCompiler
    {
        public static CameraStretchPayload Compile(
            CameraStretchAsset asset,
            CameraProjectionCompilationContext context)
        {
            asset.RequireValid();
            if (asset.CamOffsetSpace == CameraSpace.Core)
                throw new InvalidOperationException(
                    $"Camera Stretch '{asset.StretchId}' uses Core offset space without a formal core transform input.");
            if (asset.FovVariationType != CameraFovVariationType.Absolute)
                throw new InvalidOperationException(
                    $"Camera Stretch '{asset.StretchId}' uses FOV variation '{asset.FovVariationType}' without a source FOV delta field.");
            if (asset.ApplyAimPointsCameraFollowYOffset)
                throw new InvalidOperationException(
                    $"Camera Stretch '{asset.StretchId}' requires an aim-point follow-offset input.");
            var followPoints = new string[asset.RuntimeCamFollowYPoints.Count];
            for (int i = 0; i < followPoints.Length; i++)
                followPoints[i] = asset.RuntimeCamFollowYPoints[i];
            return new CameraStretchPayload(
                asset.StretchId,
                context.CompileCurve(context.RequireCurve(asset.StartCurve)),
                context.CompileCurve(context.RequireCurve(asset.EndCurve)),
                followPoints,
                asset.RotationZ,
                asset.IgnoreLocalAvatar,
                asset.IsAppliedElevationRatio,
                asset.IsAppliedEndElevationAngle,
                asset.PlayStackingType,
                asset.StackingType,
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
    }
}
