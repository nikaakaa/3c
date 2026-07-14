using UnityEngine;

namespace ThirdPersonRendering
{
    [CreateAssetMenu(menuName = "3C/Rendering/Black White Flash Profile", fileName = "BlackWhiteFlashProfile")]
    public sealed class BlackWhiteFlashProfile : ScriptableObject
    {
        public const float MinDuration = 0.02f;
        public const float MaxDuration = 2f;

        [SerializeField] BlackWhiteFlashMode mode = BlackWhiteFlashMode.RadialImpact;
        [Range(MinDuration, MaxDuration)]
        [SerializeField] float duration = 0.12f;
        [Range(BlackWhiteFlashSettings.MinIntensity, BlackWhiteFlashSettings.MaxIntensity)]
        [SerializeField] float intensityMultiplier = 1f;
        [SerializeField] Vector2 center = new Vector2(0.5f, 0.5f);
        [SerializeField] AnimationCurve intensityCurve = DefaultIntensityCurve();
        [SerializeField] AnimationCurve radiusCurve = DefaultRadiusCurve();
        [SerializeField] AnimationCurve invertCurve = DefaultInvertCurve();
        [Range(BlackWhiteFlashSettings.MinThreshold, BlackWhiteFlashSettings.MaxThreshold)]
        [SerializeField] float threshold = 0.5f;
        [Range(BlackWhiteFlashSettings.MinContrast, BlackWhiteFlashSettings.MaxContrast)]
        [SerializeField] float contrast = 12f;
        [Range(BlackWhiteFlashSettings.MinWhiteBoost, BlackWhiteFlashSettings.MaxWhiteBoost)]
        [SerializeField] float whiteBoost = 1.15f;
        [Range(BlackWhiteFlashSettings.MinBlackCrush, BlackWhiteFlashSettings.MaxBlackCrush)]
        [SerializeField] float blackCrush = 0.65f;
        [Range(BlackWhiteFlashSettings.MinInvertAmount, BlackWhiteFlashSettings.MaxInvertAmount)]
        [SerializeField] float invertAmount = 0.35f;
        [Range(BlackWhiteFlashSettings.MinRadius, BlackWhiteFlashSettings.MaxRadius)]
        [SerializeField] float baseRadius = 0.2f;
        [Range(BlackWhiteFlashSettings.MinRadius, BlackWhiteFlashSettings.MaxRadius)]
        [SerializeField] float peakRadius = 0.85f;
        [Range(BlackWhiteFlashSettings.MinSoftness, BlackWhiteFlashSettings.MaxSoftness)]
        [SerializeField] float softness = 0.22f;

        public BlackWhiteFlashMode Mode => NormalizeMode(mode);
        public float Duration => Mathf.Clamp(duration, MinDuration, MaxDuration);
        public float IntensityMultiplier => Mathf.Clamp(intensityMultiplier, BlackWhiteFlashSettings.MinIntensity, BlackWhiteFlashSettings.MaxIntensity);
        public Vector2 Center => ClampCenter(center);
        public AnimationCurve IntensityCurve => intensityCurve;
        public AnimationCurve RadiusCurve => radiusCurve;
        public AnimationCurve InvertCurve => invertCurve;
        public float Threshold => Mathf.Clamp(threshold, BlackWhiteFlashSettings.MinThreshold, BlackWhiteFlashSettings.MaxThreshold);
        public float Contrast => Mathf.Clamp(contrast, BlackWhiteFlashSettings.MinContrast, BlackWhiteFlashSettings.MaxContrast);
        public float WhiteBoost => Mathf.Clamp(whiteBoost, BlackWhiteFlashSettings.MinWhiteBoost, BlackWhiteFlashSettings.MaxWhiteBoost);
        public float BlackCrush => Mathf.Clamp(blackCrush, BlackWhiteFlashSettings.MinBlackCrush, BlackWhiteFlashSettings.MaxBlackCrush);
        public float InvertAmount => Mathf.Clamp(invertAmount, BlackWhiteFlashSettings.MinInvertAmount, BlackWhiteFlashSettings.MaxInvertAmount);
        public float BaseRadius => Mathf.Clamp(baseRadius, BlackWhiteFlashSettings.MinRadius, BlackWhiteFlashSettings.MaxRadius);
        public float PeakRadius => Mathf.Clamp(peakRadius, BlackWhiteFlashSettings.MinRadius, BlackWhiteFlashSettings.MaxRadius);
        public float Softness => Mathf.Clamp(softness, BlackWhiteFlashSettings.MinSoftness, BlackWhiteFlashSettings.MaxSoftness);
        public bool HasValidCurves => HasCurve(intensityCurve) && HasCurve(radiusCurve) && HasCurve(invertCurve);

        public BlackWhiteFlashSettings Evaluate(float normalizedTime)
        {
            return Evaluate(Center, normalizedTime, 1f);
        }

        public BlackWhiteFlashSettings Evaluate(Vector2 screenCenter, float normalizedTime, float intensityScale)
        {
            EnsureCurves();

            float time = Mathf.Clamp01(normalizedTime);
            float intensity = IntensityMultiplier * Mathf.Max(0f, intensityScale) * Clamp01(intensityCurve.Evaluate(time));
            float radius = Mathf.Lerp(BaseRadius, PeakRadius, Clamp01(radiusCurve.Evaluate(time)));
            float sampledInvert = InvertAmount * Clamp01(invertCurve.Evaluate(time));

            return new BlackWhiteFlashSettings(
                Mode,
                intensity,
                Threshold,
                Contrast,
                WhiteBoost,
                BlackCrush,
                sampledInvert,
                screenCenter,
                radius,
                Softness);
        }

        public void EnsureCurves()
        {
            if (!HasCurve(intensityCurve))
                intensityCurve = DefaultIntensityCurve();

            if (!HasCurve(radiusCurve))
                radiusCurve = DefaultRadiusCurve();

            if (!HasCurve(invertCurve))
                invertCurve = DefaultInvertCurve();
        }

        void OnValidate()
        {
            mode = NormalizeMode(mode);
            duration = Duration;
            intensityMultiplier = IntensityMultiplier;
            center = Center;
            threshold = Threshold;
            contrast = Contrast;
            whiteBoost = WhiteBoost;
            blackCrush = BlackCrush;
            invertAmount = InvertAmount;
            baseRadius = BaseRadius;
            peakRadius = PeakRadius;
            softness = Softness;
            EnsureCurves();
        }

        static BlackWhiteFlashMode NormalizeMode(BlackWhiteFlashMode value)
        {
            return value >= BlackWhiteFlashMode.FullScreen && value <= BlackWhiteFlashMode.RadialImpact
                ? value
                : BlackWhiteFlashMode.FullScreen;
        }

        static Vector2 ClampCenter(Vector2 value)
        {
            return new Vector2(Mathf.Clamp01(value.x), Mathf.Clamp01(value.y));
        }

        static float Clamp01(float value)
        {
            return Mathf.Clamp01(float.IsNaN(value) ? 0f : value);
        }

        static bool HasCurve(AnimationCurve curve)
        {
            return curve != null && curve.length > 0;
        }

        static AnimationCurve DefaultIntensityCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.16f, 1f),
                new Keyframe(1f, 0f));
        }

        static AnimationCurve DefaultRadiusCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(1f, 1f));
        }

        static AnimationCurve DefaultInvertCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.22f, 0f),
                new Keyframe(1f, 0f));
        }
    }
}
