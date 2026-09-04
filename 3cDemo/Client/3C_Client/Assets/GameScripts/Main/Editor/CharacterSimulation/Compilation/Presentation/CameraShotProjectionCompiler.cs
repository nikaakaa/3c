using System;

namespace ThirdPersonCamera
{
    internal static class CameraShotProjectionCompiler
    {
        public static CameraShotPayload Compile(
            CameraShotAsset asset,
            CameraProjectionCompilationContext context)
        {
            asset.RequireValid();
            throw new InvalidOperationException(
                $"Camera Shot '{asset.ShotId}' cannot be compiled before its CinePrefab and virtual-camera consumer semantics are closed.");
        }
    }
}
