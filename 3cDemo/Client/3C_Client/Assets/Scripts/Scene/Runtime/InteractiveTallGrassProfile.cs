using UnityEngine;

namespace ThirdPersonScene
{
    [CreateAssetMenu(menuName = "3C/Scene/Interactive Tall Grass Profile", fileName = "InteractiveTallGrassProfile")]
    public sealed class InteractiveTallGrassProfile : ScriptableObject
    {
        [SerializeField] Vector2 areaSize = new Vector2(5f, 4f);
        [SerializeField] int bladeCount = 96;
        [SerializeField] int randomSeed = 3107;
        [SerializeField] Vector2 heightRange = new Vector2(1.1f, 1.8f);
        [SerializeField] Vector2 widthRange = new Vector2(0.12f, 0.24f);
        [SerializeField] Color baseColor = new Color(0.16f, 0.34f, 0.12f, 1f);
        [SerializeField] Color topColor = new Color(0.45f, 0.75f, 0.28f, 1f);
        [SerializeField] float toonStrength = 0.65f;
        [SerializeField] float windStrength = 0.22f;
        [SerializeField] float windFrequency = 1.8f;
        [SerializeField] Vector2 windDirection = new Vector2(1f, 0.35f);
        [SerializeField] float interactionRadius = 1.1f;
        [SerializeField] float bendStrength = 0.75f;

        public InteractiveTallGrassSettings NormalizedSettings => new InteractiveTallGrassSettings(
            areaSize,
            bladeCount,
            randomSeed,
            heightRange.x,
            heightRange.y,
            widthRange.x,
            widthRange.y,
            baseColor,
            topColor,
            toonStrength,
            windStrength,
            windFrequency,
            windDirection,
            interactionRadius,
            bendStrength);
    }
}
