using UnityEngine;

namespace ThirdPersonRendering
{
    public readonly struct GlitchSettings
    {
        public const float MinIntensity = 0f;
        public const float MaxIntensity = 1f;
        public const float MinBlockSize = 8f;
        public const float MaxBlockSize = 160f;
        public const float MinHorizontalJitter = 0f;
        public const float MaxHorizontalJitter = 0.08f;
        public const float MinRgbSplit = 0f;
        public const float MaxRgbSplit = 0.04f;
        public const float MinScanLineIntensity = 0f;
        public const float MaxScanLineIntensity = 1f;
        public const float MinSpeed = 0f;
        public const float MaxSpeed = 60f;
        public const float MinMaskInfluence = 0f;
        public const float MaxMaskInfluence = 1f;
        public const float MinMaskExpansion = 0f;
        public const float MaxMaskExpansion = 0.12f;
        public const float MinSlashSliceDensity = 32f;
        public const float MaxSlashSliceDensity = 720f;
        public const float MinSlashSmearWidth = 0f;
        public const float MaxSlashSmearWidth = 0.12f;
        public const float MinSlashHighlightStretch = 0f;
        public const float MaxSlashHighlightStretch = 2f;
        public const float MinSlashDirection = -1f;
        public const float MaxSlashDirection = 1f;
        public const float MinSlashBlend = 0f;
        public const float MaxSlashBlend = 1f;
        public const float ActivationThreshold = 0.0001f;

        public static readonly GlitchSettings Disabled = new GlitchSettings(
            GlitchMode.DigitalGlitch,
            0f,
            48f,
            0.02f,
            0.01f,
            0.35f,
            24f,
            false,
            1f,
            0.04f,
            360f,
            0.04f,
            0.8f,
            1f,
            1f);

        public GlitchSettings(
            float intensity,
            float blockSize,
            float horizontalJitter,
            float rgbSplit,
            float scanLineIntensity,
            float speed,
            bool useTargetMask,
            float maskInfluence,
            float maskExpansion)
            : this(
                GlitchMode.DigitalGlitch,
                intensity,
                blockSize,
                horizontalJitter,
                rgbSplit,
                scanLineIntensity,
                speed,
                useTargetMask,
                maskInfluence,
                maskExpansion,
                360f,
                0.04f,
                0.8f,
                1f,
                1f)
        {
        }

        public GlitchSettings(
            GlitchMode mode,
            float intensity,
            float blockSize,
            float horizontalJitter,
            float rgbSplit,
            float scanLineIntensity,
            float speed,
            bool useTargetMask,
            float maskInfluence,
            float maskExpansion,
            float slashSliceDensity,
            float slashSmearWidth,
            float slashHighlightStretch,
            float slashDirection,
            float slashBlend)
        {
            Mode = NormalizeMode(mode);
            Intensity = Mathf.Clamp(intensity, MinIntensity, MaxIntensity);
            BlockSize = Mathf.Clamp(blockSize, MinBlockSize, MaxBlockSize);
            HorizontalJitter = Mathf.Clamp(horizontalJitter, MinHorizontalJitter, MaxHorizontalJitter);
            RgbSplit = Mathf.Clamp(rgbSplit, MinRgbSplit, MaxRgbSplit);
            ScanLineIntensity = Mathf.Clamp(scanLineIntensity, MinScanLineIntensity, MaxScanLineIntensity);
            Speed = Mathf.Clamp(speed, MinSpeed, MaxSpeed);
            UseTargetMask = useTargetMask;
            MaskInfluence = Mathf.Clamp(maskInfluence, MinMaskInfluence, MaxMaskInfluence);
            MaskExpansion = Mathf.Clamp(maskExpansion, MinMaskExpansion, MaxMaskExpansion);
            SlashSliceDensity = Mathf.Clamp(slashSliceDensity, MinSlashSliceDensity, MaxSlashSliceDensity);
            SlashSmearWidth = Mathf.Clamp(slashSmearWidth, MinSlashSmearWidth, MaxSlashSmearWidth);
            SlashHighlightStretch = Mathf.Clamp(slashHighlightStretch, MinSlashHighlightStretch, MaxSlashHighlightStretch);
            SlashDirection = Mathf.Clamp(slashDirection, MinSlashDirection, MaxSlashDirection);
            SlashBlend = Mathf.Clamp(slashBlend, MinSlashBlend, MaxSlashBlend);
        }

        public GlitchMode Mode { get; }
        public float Intensity { get; }
        public float BlockSize { get; }
        public float HorizontalJitter { get; }
        public float RgbSplit { get; }
        public float ScanLineIntensity { get; }
        public float Speed { get; }
        public bool UseTargetMask { get; }
        public float MaskInfluence { get; }
        public float MaskExpansion { get; }
        public float SlashSliceDensity { get; }
        public float SlashSmearWidth { get; }
        public float SlashHighlightStretch { get; }
        public float SlashDirection { get; }
        public float SlashBlend { get; }
        public bool IsActive => Intensity > ActivationThreshold;

        public Vector4 PrimaryParams => new Vector4(Intensity, BlockSize, HorizontalJitter, RgbSplit);
        public Vector4 SecondaryParams => new Vector4(ScanLineIntensity, Speed, UseTargetMask ? 1f : 0f, MaskInfluence);
        public Vector4 MaskParams => new Vector4(MaskExpansion, 0f, 0f, 0f);
        public Vector4 SlashParams => new Vector4(SlashSliceDensity, SlashSmearWidth, SlashHighlightStretch, SlashDirection);
        public Vector4 ModeParams => new Vector4((float)Mode, SlashBlend, 0f, 0f);

        static GlitchMode NormalizeMode(GlitchMode mode)
        {
            return mode >= GlitchMode.DigitalGlitch && mode <= GlitchMode.SlashTear
                ? mode
                : GlitchMode.DigitalGlitch;
        }
    }
}
