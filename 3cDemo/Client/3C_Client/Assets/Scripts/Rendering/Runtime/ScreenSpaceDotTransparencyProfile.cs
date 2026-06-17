using UnityEngine;

namespace ThirdPersonRendering
{
    [CreateAssetMenu(menuName = "3C/Rendering/Screen Space Dot Transparency Profile", fileName = "ScreenSpaceDotTransparencyProfile")]
    public sealed class ScreenSpaceDotTransparencyProfile : ScriptableObject
    {
        [SerializeField] bool enabled;
        [Range(ScreenSpaceDotTransparencySettings.MinCoverage, ScreenSpaceDotTransparencySettings.MaxCoverage)]
        [SerializeField] float coverage;
        [Range(ScreenSpaceDotTransparencySettings.MinSpacingPixels, ScreenSpaceDotTransparencySettings.MaxSpacingPixels)]
        [SerializeField] float spacingPixels = 12f;
        [Range(ScreenSpaceDotTransparencySettings.MinRadius, ScreenSpaceDotTransparencySettings.MaxRadius)]
        [SerializeField] float radius = 0.45f;
        [Range(ScreenSpaceDotTransparencySettings.MinHardness, ScreenSpaceDotTransparencySettings.MaxHardness)]
        [SerializeField] float hardness = 1f;
        [SerializeField] Vector2 offsetPixels;

        public bool Enabled => enabled;
        public float Coverage => NormalizedSettings.Coverage;
        public float SpacingPixels => NormalizedSettings.SpacingPixels;
        public float Radius => NormalizedSettings.Radius;
        public float Hardness => NormalizedSettings.Hardness;
        public Vector2 OffsetPixels => NormalizedSettings.OffsetPixels;
        public ScreenSpaceDotTransparencySettings NormalizedSettings => new ScreenSpaceDotTransparencySettings(
            enabled,
            coverage,
            spacingPixels,
            radius,
            hardness,
            offsetPixels);

        void OnValidate()
        {
            ScreenSpaceDotTransparencySettings settings = NormalizedSettings;
            coverage = settings.Coverage;
            spacingPixels = settings.SpacingPixels;
            radius = settings.Radius;
            hardness = settings.Hardness;
            offsetPixels = settings.OffsetPixels;
        }
    }
}
