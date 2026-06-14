using UnityEngine;

namespace ThirdPersonRendering
{
    public readonly struct LocalHeatDistortionAreaSettings
    {
        public const float MinRadius = 0.01f;
        public const float MaxRadius = 2f;
        public const float MinAspect = 0.2f;
        public const float MaxAspect = 5f;
        public const float MinSoftness = 0.01f;
        public const float MaxSoftness = 0.8f;
        public const float DepthFadeDistance = 0.15f;

        public static readonly LocalHeatDistortionAreaSettings Invalid = new LocalHeatDistortionAreaSettings(
            Vector2.one * 0.5f,
            0f,
            1f,
            0f,
            1f,
            0f,
            LocalHeatDistortionAreaShape.ScreenEllipse);

        public LocalHeatDistortionAreaSettings(
            Vector2 center,
            float radius,
            float aspect,
            float rotationRadians,
            float softness,
            float sourceViewDepth,
            LocalHeatDistortionAreaShape shape)
        {
            Center = new Vector2(Mathf.Clamp01(center.x), Mathf.Clamp01(center.y));
            Radius = Mathf.Clamp(radius, 0f, MaxRadius);
            Aspect = Mathf.Clamp(aspect, MinAspect, MaxAspect);
            RotationRadians = rotationRadians;
            Softness = Mathf.Clamp(softness, MinSoftness, MaxSoftness);
            SourceViewDepth = Mathf.Max(0f, sourceViewDepth);
            Shape = NormalizeShape(shape);
        }

        public Vector2 Center { get; }
        public float Radius { get; }
        public float Aspect { get; }
        public float RotationRadians { get; }
        public float Softness { get; }
        public float SourceViewDepth { get; }
        public LocalHeatDistortionAreaShape Shape { get; }
        public bool IsValid => Radius >= MinRadius;

        public Vector4 AreaParams => new Vector4(Center.x, Center.y, Radius, Softness);
        public Vector4 AreaExtraParams => new Vector4(Aspect, Mathf.Sin(RotationRadians), Mathf.Cos(RotationRadians), (float)Shape);
        public Vector4 AreaDepthParams => new Vector4(SourceViewDepth, DepthFadeDistance, 0f, 0f);

        static LocalHeatDistortionAreaShape NormalizeShape(LocalHeatDistortionAreaShape shape)
        {
            return shape >= LocalHeatDistortionAreaShape.ScreenEllipse && shape <= LocalHeatDistortionAreaShape.CylinderPressure
                ? shape
                : LocalHeatDistortionAreaShape.ScreenEllipse;
        }
    }
}
