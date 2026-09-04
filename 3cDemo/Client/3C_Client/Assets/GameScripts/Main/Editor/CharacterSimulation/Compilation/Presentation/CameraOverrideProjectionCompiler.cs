using System;

namespace ThirdPersonCamera
{
    internal static class CameraOverrideProjectionCompiler
    {
        public static CameraOverrideTrackPayload Compile(
            CameraOverrideTrackAsset asset,
            CameraProjectionCompilationContext context)
        {
            asset.RequireValid();
            throw new InvalidOperationException(
                $"Camera Override Track '{asset.TrackId}' cannot be compiled before its ZZZ track and tag consumer semantics are closed.");
        }
    }
}
