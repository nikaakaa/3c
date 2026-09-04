namespace ThirdPersonCamera
{
    internal static class CameraShotProjectionCompiler
    {
        public static CameraShotPayload Compile(
            CameraShotAsset asset,
            CameraProjectionCompilationContext context)
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
                CompileBlend(asset.BlendIn, context),
                CompileBlend(asset.BlendOut, context),
                asset.BlendWithIgnoreLookAtTarget,
                asset.Priority,
                asset.Tag);
        }

        static CameraShotBlendPayload CompileBlend(
            CameraShotBlendSettings source,
            CameraProjectionCompilationContext context)
        {
            source.RequireValid("Camera Shot blend");
            return new CameraShotBlendPayload(
                source.Duration,
                context.CompileCurve(context.RequireCurve(source.Curve)),
                source.UseCoreSpace,
                source.UseDelta);
        }
    }
}
