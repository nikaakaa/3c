using System;
using UnityEngine;

namespace ThirdPersonScene
{
    [Serializable]
    public readonly struct InteractiveTallGrassSettings
    {
        public const float MinAreaSize = 0.5f;
        public const float MaxAreaSize = 24f;
        public const int MinBladeCount = 1;
        public const int MaxBladeCount = 512;
        public const float MinBladeHeight = 0.2f;
        public const float MaxBladeHeight = 3.5f;
        public const float MinBladeWidth = 0.03f;
        public const float MaxBladeWidth = 0.8f;
        public const float MinToonStrength = 0f;
        public const float MaxToonStrength = 1f;
        public const float MinWindStrength = 0f;
        public const float MaxWindStrength = 1.2f;
        public const float MinWindFrequency = 0f;
        public const float MaxWindFrequency = 8f;
        public const float MinInteractionRadius = 0f;
        public const float MaxInteractionRadius = 4f;
        public const float MinBendStrength = 0f;
        public const float MaxBendStrength = 1.5f;

        public static readonly InteractiveTallGrassSettings Default = new InteractiveTallGrassSettings(
            new Vector2(5f, 4f),
            96,
            3107,
            1.1f,
            1.8f,
            0.12f,
            0.24f,
            new Color(0.16f, 0.34f, 0.12f, 1f),
            new Color(0.45f, 0.75f, 0.28f, 1f),
            0.65f,
            0.22f,
            1.8f,
            new Vector2(1f, 0.35f),
            1.1f,
            0.75f);

        public InteractiveTallGrassSettings(
            Vector2 areaSize,
            int bladeCount,
            int randomSeed,
            float minHeight,
            float maxHeight,
            float minWidth,
            float maxWidth,
            Color baseColor,
            Color topColor,
            float toonStrength,
            float windStrength,
            float windFrequency,
            Vector2 windDirection,
            float interactionRadius,
            float bendStrength)
        {
            AreaSize = new Vector2(
                Mathf.Clamp(areaSize.x, MinAreaSize, MaxAreaSize),
                Mathf.Clamp(areaSize.y, MinAreaSize, MaxAreaSize));
            BladeCount = Mathf.Clamp(bladeCount, MinBladeCount, MaxBladeCount);
            RandomSeed = randomSeed;
            MinHeight = Mathf.Clamp(Mathf.Min(minHeight, maxHeight), MinBladeHeight, MaxBladeHeight);
            MaxHeight = Mathf.Clamp(Mathf.Max(minHeight, maxHeight), MinBladeHeight, MaxBladeHeight);
            MinWidth = Mathf.Clamp(Mathf.Min(minWidth, maxWidth), MinBladeWidth, MaxBladeWidth);
            MaxWidth = Mathf.Clamp(Mathf.Max(minWidth, maxWidth), MinBladeWidth, MaxBladeWidth);
            BaseColor = ClampColor(baseColor);
            TopColor = ClampColor(topColor);
            ToonStrength = Mathf.Clamp(toonStrength, MinToonStrength, MaxToonStrength);
            WindStrength = Mathf.Clamp(windStrength, MinWindStrength, MaxWindStrength);
            WindFrequency = Mathf.Clamp(windFrequency, MinWindFrequency, MaxWindFrequency);
            WindDirection = NormalizeWindDirection(windDirection);
            InteractionRadius = Mathf.Clamp(interactionRadius, MinInteractionRadius, MaxInteractionRadius);
            BendStrength = Mathf.Clamp(bendStrength, MinBendStrength, MaxBendStrength);
        }

        public Vector2 AreaSize { get; }
        public int BladeCount { get; }
        public int RandomSeed { get; }
        public float MinHeight { get; }
        public float MaxHeight { get; }
        public float MinWidth { get; }
        public float MaxWidth { get; }
        public Color BaseColor { get; }
        public Color TopColor { get; }
        public float ToonStrength { get; }
        public float WindStrength { get; }
        public float WindFrequency { get; }
        public Vector2 WindDirection { get; }
        public float InteractionRadius { get; }
        public float BendStrength { get; }

        static Color ClampColor(Color color)
        {
            return new Color(
                Mathf.Clamp01(color.r),
                Mathf.Clamp01(color.g),
                Mathf.Clamp01(color.b),
                Mathf.Clamp01(color.a));
        }

        static Vector2 NormalizeWindDirection(Vector2 direction)
        {
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        }
    }
}
