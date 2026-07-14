using UnityEngine;

namespace ThirdPersonRendering
{
    [CreateAssetMenu(menuName = "3C/Rendering/Block Impact VFX Profile", fileName = "BlockImpactVfxProfile")]
    public sealed class BlockImpactVfxProfile : ScriptableObject
    {
        public const float MinDuration = 0.02f;
        public const float MaxDuration = 2f;
        public const float MinHdrIntensity = 0f;
        public const float MaxHdrIntensity = 30f;
        public const int MinSparkCount = 0;
        public const int MaxSparkCount = 256;
        public const float MinSparkSpeed = 0f;
        public const float MaxSparkSpeed = 45f;
        public const float MinSparkLifetime = 0.02f;
        public const float MaxSparkLifetime = 2f;
        public const float MinSparkAngle = 0f;
        public const float MaxSparkAngle = 180f;
        public const float MinSparkStretch = 0f;
        public const float MaxSparkStretch = 12f;
        public const float MinTrailWidth = 0.001f;
        public const float MaxTrailWidth = 1f;
        public const float MinSparkGravity = -4f;
        public const float MaxSparkGravity = 4f;
        public const float MinSparkDampen = 0f;
        public const float MaxSparkDampen = 1f;
        public const float MinLayerScale = 0.01f;
        public const float MaxLayerScale = 20f;
        public const float MinScreenStreakLength = 0.05f;
        public const float MaxScreenStreakLength = 2f;
        public const float MinScreenStreakThickness = 0.001f;
        public const float MaxScreenStreakThickness = 0.2f;
        public const float MinScreenStreakSoftness = 0f;
        public const float MaxScreenStreakSoftness = 1f;

        [Header("Textures")]
        [SerializeField] Texture2D flashTexture;
        [SerializeField] Texture2D sparkTexture;
        [SerializeField] Texture2D sparkTrailTexture;
        [SerializeField] Texture2D streakTexture;
        [SerializeField] Texture2D noiseTexture;

        [Header("Layers")]
        [SerializeField] bool coreFlashEnabled = true;
        [SerializeField] bool screenStreakEnabled = true;
        [SerializeField] bool sparksEnabled = true;
        [SerializeField] bool screenPulseEnabled = true;

        [Header("Colors")]
        [ColorUsage(true, true)]
        [SerializeField] Color flashColor = new Color(1f, 0.76f, 0.32f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] Color sparkColor = new Color(1f, 0.55f, 0.12f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] Color streakColor = new Color(1f, 0.42f, 0.34f, 1f);

        [Header("Timing")]
        [Range(MinDuration, MaxDuration)]
        [SerializeField] float duration = 0.28f;

        [Header("Core Flash")]
        [Range(MinHdrIntensity, MaxHdrIntensity)]
        [SerializeField] float hdrIntensity = 12f;
        [Range(0.001f, 1f)]
        [SerializeField] float flashSoftness = 0.32f;
        [SerializeField] Vector2 flashScale = new Vector2(1.45f, 1.45f);

        [Header("Sparks")]
        [Range(MinSparkCount, MaxSparkCount)]
        [SerializeField] int sparkCount = 54;
        [Range(MinSparkSpeed, MaxSparkSpeed)]
        [SerializeField] float sparkSpeed = 13f;
        [Range(MinSparkLifetime, MaxSparkLifetime)]
        [SerializeField] float sparkLifetime = 0.26f;
        [Range(MinSparkAngle, MaxSparkAngle)]
        [SerializeField] float sparkConeAngle = 42f;
        [Range(MinSparkStretch, MaxSparkStretch)]
        [SerializeField] float sparkVelocityScale = 0.18f;
        [Range(MinSparkStretch, MaxSparkStretch)]
        [SerializeField] float sparkLengthScale = 5.2f;
        [Range(MinSparkLifetime, MaxSparkLifetime)]
        [SerializeField] float sparkTrailLifetime = 0.12f;
        [Range(MinTrailWidth, MaxTrailWidth)]
        [SerializeField] float sparkTrailWidth = 0.045f;
        [Range(MinSparkGravity, MaxSparkGravity)]
        [SerializeField] float sparkGravityModifier = 0.35f;
        [Range(MinSparkDampen, MaxSparkDampen)]
        [SerializeField] float sparkVelocityDampen = 0.42f;

        [Header("Screen Streak")]
        [Range(MinScreenStreakLength, MaxScreenStreakLength)]
        [SerializeField] float screenStreakLength = 1.2f;
        [Range(MinScreenStreakThickness, MaxScreenStreakThickness)]
        [SerializeField] float screenStreakThickness = 0.018f;
        [Range(MinScreenStreakSoftness, MaxScreenStreakSoftness)]
        [SerializeField] float screenStreakSoftness = 0.7f;
        [Range(0f, 3f)]
        [SerializeField] float screenFlashWeight = 1f;
        [Range(0f, 3f)]
        [SerializeField] float screenRadialWeight = 1f;
        [Range(0f, 3f)]
        [SerializeField] float screenStreakWeight = 1f;
        [Range(0f, 3f)]
        [SerializeField] float screenChromaticWeight = 1f;
        [Range(0f, 3f)]
        [SerializeField] float screenImpactStrength = 1f;

        public Texture2D FlashTexture => flashTexture;
        public Texture2D SparkTexture => sparkTexture;
        public Texture2D SparkTrailTexture => sparkTrailTexture;
        public Texture2D StreakTexture => streakTexture;
        public Texture2D NoiseTexture => noiseTexture;
        public bool CoreFlashEnabled => coreFlashEnabled;
        public bool ScreenStreakEnabled => screenStreakEnabled;
        public bool SparksEnabled => sparksEnabled;
        public bool ScreenPulseEnabled => screenPulseEnabled;
        public Color FlashColor => flashColor;
        public Color SparkColor => sparkColor;
        public Color StreakColor => streakColor;
        public float HdrIntensity => hdrIntensity;
        public float FlashSoftness => flashSoftness;
        public float Duration => duration;
        public int SparkCount => sparkCount;
        public float SparkSpeed => sparkSpeed;
        public float SparkLifetime => sparkLifetime;
        public float SparkConeAngle => sparkConeAngle;
        public float SparkVelocityScale => sparkVelocityScale;
        public float SparkLengthScale => sparkLengthScale;
        public float SparkTrailLifetime => sparkTrailLifetime;
        public float SparkTrailWidth => sparkTrailWidth;
        public float SparkGravityModifier => sparkGravityModifier;
        public float SparkVelocityDampen => sparkVelocityDampen;
        public Vector2 FlashScale => flashScale;
        public float ScreenStreakLength => screenStreakLength;
        public float ScreenStreakThickness => screenStreakThickness;
        public float ScreenStreakSoftness => screenStreakSoftness;
        public float ScreenFlashWeight => screenFlashWeight;
        public float ScreenRadialWeight => screenRadialWeight;
        public float ScreenStreakWeight => screenStreakWeight;
        public float ScreenChromaticWeight => screenChromaticWeight;
        public float ScreenImpactStrength => screenImpactStrength;

        public bool HasRequiredTextures => flashTexture != null
            && sparkTexture != null
            && sparkTrailTexture != null
            && streakTexture != null;

        public bool ValidateRequiredTextures(out string message)
        {
            if (flashTexture == null)
            {
                message = "BlockImpactVfxProfile 缺少爆闪贴图";
                return false;
            }

            if (sparkTexture == null)
            {
                message = "BlockImpactVfxProfile 缺少火花贴图";
                return false;
            }

            if (sparkTrailTexture == null)
            {
                message = "BlockImpactVfxProfile 缺少火花拖尾贴图";
                return false;
            }

            if (streakTexture == null)
            {
                message = "BlockImpactVfxProfile 缺少横向光带 mask 贴图";
                return false;
            }

            message = string.Empty;
            return true;
        }

        void OnValidate()
        {
            hdrIntensity = Mathf.Clamp(hdrIntensity, MinHdrIntensity, MaxHdrIntensity);
            duration = Mathf.Clamp(duration, MinDuration, MaxDuration);
            sparkCount = Mathf.Clamp(sparkCount, MinSparkCount, MaxSparkCount);
            sparkSpeed = Mathf.Clamp(sparkSpeed, MinSparkSpeed, MaxSparkSpeed);
            sparkLifetime = Mathf.Clamp(sparkLifetime, MinSparkLifetime, MaxSparkLifetime);
            sparkConeAngle = Mathf.Clamp(sparkConeAngle, MinSparkAngle, MaxSparkAngle);
            sparkVelocityScale = Mathf.Clamp(sparkVelocityScale, MinSparkStretch, MaxSparkStretch);
            sparkLengthScale = Mathf.Clamp(sparkLengthScale, MinSparkStretch, MaxSparkStretch);
            sparkTrailLifetime = Mathf.Clamp(sparkTrailLifetime, MinSparkLifetime, MaxSparkLifetime);
            sparkTrailWidth = Mathf.Clamp(sparkTrailWidth, MinTrailWidth, MaxTrailWidth);
            sparkGravityModifier = Mathf.Clamp(sparkGravityModifier, MinSparkGravity, MaxSparkGravity);
            sparkVelocityDampen = Mathf.Clamp(sparkVelocityDampen, MinSparkDampen, MaxSparkDampen);
            flashScale = ClampScale(flashScale);
            flashSoftness = Mathf.Clamp(flashSoftness, 0.001f, 1f);
            screenStreakLength = Mathf.Clamp(screenStreakLength, MinScreenStreakLength, MaxScreenStreakLength);
            screenStreakThickness = Mathf.Clamp(screenStreakThickness, MinScreenStreakThickness, MaxScreenStreakThickness);
            screenStreakSoftness = Mathf.Clamp(screenStreakSoftness, MinScreenStreakSoftness, MaxScreenStreakSoftness);
            screenFlashWeight = Mathf.Clamp(screenFlashWeight, 0f, 3f);
            screenRadialWeight = Mathf.Clamp(screenRadialWeight, 0f, 3f);
            screenStreakWeight = Mathf.Clamp(screenStreakWeight, 0f, 3f);
            screenChromaticWeight = Mathf.Clamp(screenChromaticWeight, 0f, 3f);
            screenImpactStrength = Mathf.Clamp(screenImpactStrength, 0f, 3f);
        }

        static Vector2 ClampScale(Vector2 value)
        {
            return new Vector2(
                Mathf.Clamp(value.x, MinLayerScale, MaxLayerScale),
                Mathf.Clamp(value.y, MinLayerScale, MaxLayerScale));
        }
    }
}
