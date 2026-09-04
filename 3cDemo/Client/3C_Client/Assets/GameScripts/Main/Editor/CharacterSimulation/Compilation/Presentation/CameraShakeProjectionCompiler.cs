using System;

namespace ThirdPersonCamera
{
    internal static class CameraShakeProjectionCompiler
    {
        public static CameraShakePayload Compile(
            CameraShakeAsset asset,
            CameraProjectionCompilationContext context)
        {
            asset.RequireValid();
            throw new InvalidOperationException(
                $"Camera Shake '{asset.ShakeId}' cannot be compiled before its ZZZ consumer semantics are closed.");
        }
    }
}
