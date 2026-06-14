using UnityEngine;

namespace ThirdPersonRendering
{
    public readonly struct EdgeScanSettings
    {
        public const float MinIntensity = 0f;
        public const float MaxIntensity = 3f;
        public const float MinRadius = 0f;
        public const float MaxRadius = 250f;
        public const float MinWidth = 0.02f;
        public const float MaxWidth = 30f;
        public const float MinDepthThreshold = 0.0001f;
        public const float MaxDepthThreshold = 2f;
        public const float MinNormalThreshold = 0.0001f;
        public const float MaxNormalThreshold = 1f;
        public const float MinEdgeStrength = 0f;
        public const float MaxEdgeStrength = 5f;
        public const float MinDistanceFade = 0f;
        public const float MaxDistanceFade = 250f;
        public const float MinArcAngle = 1f;
        public const float MaxArcAngle = 360f;
        public const float MinScanLineSpacing = 0.05f;
        public const float MaxScanLineSpacing = 20f;
        public const float MinScanLineWidth = 0.005f;
        public const float MaxScanLineWidth = 3f;
        public const float MinScanLineStrength = 0f;
        public const float MaxScanLineStrength = 5f;
        public const float MinFrontGlowStrength = 0f;
        public const float MaxFrontGlowStrength = 5f;
        public const float MinDarkenStrength = 0f;
        public const float MaxDarkenStrength = 1f;
        public const float ActivationThreshold = 0.0001f;

        public static readonly EdgeScanSettings Disabled = new EdgeScanSettings(
            0f,
            Vector3.zero,
            0f,
            2f,
            new Color(0.2f, 0.85f, 1f, 1f),
            0.08f,
            0.25f,
            1.5f,
            80f,
            Vector3.forward,
            120f,
            1.15f,
            0.08f,
            1.2f,
            1.8f,
            0.22f);

        public EdgeScanSettings(
            float intensity,
            Vector3 origin,
            float radius,
            float width,
            Color color,
            float depthThreshold,
            float normalThreshold,
            float edgeStrength,
            float distanceFade,
            Vector3 direction,
            float arcAngle,
            float scanLineSpacing,
            float scanLineWidth,
            float scanLineStrength,
            float frontGlowStrength,
            float darkenStrength)
        {
            Intensity = Mathf.Clamp(intensity, MinIntensity, MaxIntensity);
            Origin = origin;
            Radius = Mathf.Clamp(radius, MinRadius, MaxRadius);
            Width = Mathf.Clamp(width, MinWidth, MaxWidth);
            Color = color;
            DepthThreshold = Mathf.Clamp(depthThreshold, MinDepthThreshold, MaxDepthThreshold);
            NormalThreshold = Mathf.Clamp(normalThreshold, MinNormalThreshold, MaxNormalThreshold);
            EdgeStrength = Mathf.Clamp(edgeStrength, MinEdgeStrength, MaxEdgeStrength);
            DistanceFade = Mathf.Clamp(distanceFade, MinDistanceFade, MaxDistanceFade);
            Direction = NormalizeHorizontalDirection(direction);
            ArcAngle = Mathf.Clamp(arcAngle, MinArcAngle, MaxArcAngle);
            ScanLineSpacing = Mathf.Clamp(scanLineSpacing, MinScanLineSpacing, MaxScanLineSpacing);
            ScanLineWidth = Mathf.Clamp(scanLineWidth, MinScanLineWidth, MaxScanLineWidth);
            ScanLineStrength = Mathf.Clamp(scanLineStrength, MinScanLineStrength, MaxScanLineStrength);
            FrontGlowStrength = Mathf.Clamp(frontGlowStrength, MinFrontGlowStrength, MaxFrontGlowStrength);
            DarkenStrength = Mathf.Clamp(darkenStrength, MinDarkenStrength, MaxDarkenStrength);
        }

        public float Intensity { get; }
        public Vector3 Origin { get; }
        public float Radius { get; }
        public float Width { get; }
        public Color Color { get; }
        public float DepthThreshold { get; }
        public float NormalThreshold { get; }
        public float EdgeStrength { get; }
        public float DistanceFade { get; }
        public Vector3 Direction { get; }
        public float ArcAngle { get; }
        public float ScanLineSpacing { get; }
        public float ScanLineWidth { get; }
        public float ScanLineStrength { get; }
        public float FrontGlowStrength { get; }
        public float DarkenStrength { get; }
        public bool IsActive => Intensity > ActivationThreshold && Width > MinWidth - ActivationThreshold;

        public Vector4 OriginRadiusParams => new Vector4(Origin.x, Origin.y, Origin.z, Radius);
        public Vector4 ScanParams => new Vector4(Intensity, Width, EdgeStrength, DistanceFade);
        public Vector4 EdgeParams => new Vector4(DepthThreshold, NormalThreshold, 0f, 0f);
        public Vector4 ColorParams => new Vector4(Color.r, Color.g, Color.b, Color.a);
        public Vector4 DirectionArcParams => new Vector4(Direction.x, Direction.y, Direction.z, Mathf.Cos(ArcAngle * 0.5f * Mathf.Deg2Rad));
        public Vector4 LineParams => new Vector4(ScanLineSpacing, ScanLineWidth, ScanLineStrength, FrontGlowStrength);
        public Vector4 ToneParams => new Vector4(DarkenStrength, 0f, 0f, 0f);

        static Vector3 NormalizeHorizontalDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return Vector3.forward;

            return direction.normalized;
        }
    }
}
