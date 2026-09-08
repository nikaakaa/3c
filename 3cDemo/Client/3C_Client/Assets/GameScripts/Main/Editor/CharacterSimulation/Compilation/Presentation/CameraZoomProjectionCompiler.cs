namespace ThirdPersonCamera
{
    internal static class CameraZoomProjectionCompiler
    {
        public static CameraZoomPayload Compile(
            CameraZoomAsset asset,
            CameraProjectionCompilationContext context)
        {
            asset.RequireValid();
            return new CameraZoomPayload(
                asset.ZoomId,
                context.CompileCurve(context.RequireCurve(asset.StartCurve)),
                context.CompileCurve(context.RequireCurve(asset.EndCurve)),
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
    }
}
