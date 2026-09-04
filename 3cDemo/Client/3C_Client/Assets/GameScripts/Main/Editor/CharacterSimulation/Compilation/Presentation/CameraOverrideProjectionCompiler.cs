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
            return new CameraOverrideTrackPayload(
                asset.TrackId,
                CloneSettings(asset.Settings),
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

        static CameraOverrideTrackSettings CloneSettings(CameraOverrideTrackSettings source)
        {
            var orbits = new CameraOrbitDescriptor[source.Orbits.Count];
            for (int i = 0; i < orbits.Length; i++)
            {
                CameraOrbitDescriptor orbit = source.Orbits[i];
                orbits[i] = new CameraOrbitDescriptor(orbit.Height, orbit.Radius, orbit.ScreenY);
            }
            return new CameraOverrideTrackSettings(
                new CameraOrbitDescriptor(source.TopOrbit.Height, source.TopOrbit.Radius, source.TopOrbit.ScreenY),
                orbits,
                source.ScreenY.ToArray(),
                source.FollowOffset,
                source.AimOffset,
                source.FieldOfView);
        }
    }
}
