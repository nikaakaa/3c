namespace ThirdPersonCamera
{
    internal static class CameraStretchProjectionCompiler
    {
        public static CameraStretchPayload Compile(
            CameraStretchAsset asset,
            CameraProjectionCompilationContext context)
        {
            asset.RequireValid();
            return new CameraStretchPayload(
                asset.StretchId,
                context.CompileCurve(context.RequireCurve(asset.StartCurve)),
                context.CompileCurve(context.RequireCurve(asset.EndCurve)),
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
    }
}
