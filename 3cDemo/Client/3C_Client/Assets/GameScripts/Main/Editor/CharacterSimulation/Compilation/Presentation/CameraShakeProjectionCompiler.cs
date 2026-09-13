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
            return new CameraShakePayload(
                asset.ShakeId,
                asset.ShakeType,
                asset.CameraShakePropertyConfig,
                asset.AngleVertical,
                asset.NoiseAngle,
                asset.RadiusLength,
                asset.DistanceToPlane,
                asset.NoiseRatio,
                asset.ShakeTotalTime,
                asset.Frequency,
                asset.RollAmplitude,
                asset.PitchAmplitude,
                asset.YawAmplitude,
                asset.ShakeCenterSpace,
                asset.RealtimeVibration,
                asset.DissipationMode,
                asset.ImpactRadius,
                asset.DissipationDistance,
                asset.CustomCurveKey,
                asset.FadeInDuration,
                context.CompileCurve(context.RequireCurve(asset.FadeInCurve)),
                asset.FadeOutDuration,
                context.CompileCurve(context.RequireCurve(asset.FadeOutCurve)),
                context.CompileCurve(context.RequireCurve(asset.Curve)),
                asset.IgnoreTimeScale,
                asset.PlayStackingType,
                asset.PlayPriority,
                asset.DataPriority,
                asset.StandardConfigKey);
        }
    }
}
