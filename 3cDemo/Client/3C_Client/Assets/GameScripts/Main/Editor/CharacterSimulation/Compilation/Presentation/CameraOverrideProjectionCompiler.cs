using System;
using System.Linq;

namespace ThirdPersonCamera
{
    internal static class CameraOverrideProjectionCompiler
    {
        public static CameraOverrideTrackPayload Compile(
            CameraOverrideTrackAsset asset,
            CameraProjectionCompilationContext context)
        {
            asset.RequireValid();
            CameraOverrideTrackSettings settings = asset.Settings;
            var orbits = settings.Orbits
                .Select(value => new CameraOrbitPayload(value.Height, value.Radius))
                .ToArray();
            var screenY = settings.ScreenY.ToArray();
            var payloadSettings = new CameraOverrideTrackSettingsPayload(
                new CameraOrbitPayload(settings.TopOrbit.Height, settings.TopOrbit.Radius),
                orbits,
                screenY,
                settings.FollowOffset,
                settings.AimOffset,
                settings.FieldOfView);
            payloadSettings.RequireValid($"Camera Override Track '{asset.TrackId}'.Settings");
            return new CameraOverrideTrackPayload(
                asset.TrackId,
                payloadSettings,
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
                context.CompileCurve(context.RequireCurve(asset.BlendInCurve)),
                context.CompileCurve(context.RequireCurve(asset.BlendOutCurve)));
        }
    }
}
