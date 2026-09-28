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
                asset.DissipationMode == 5 ? context.CompileCurve(asset.CustomCurveKey) : null,
                asset.FadeInDuration,
                asset.FadeInCurve ? context.CompileCurve(asset.FadeInCurve) : null,
                asset.FadeOutDuration,
                asset.FadeOutCurve ? context.CompileCurve(asset.FadeOutCurve) : null,
                asset.Curve ? context.CompileCurve(asset.Curve) : null,
                asset.IgnoreTimeScale,
                asset.PlayStackingType,
                asset.PlayPriority,
                asset.DataPriority,
                asset.StandardConfigKey);
        }
    }
}
