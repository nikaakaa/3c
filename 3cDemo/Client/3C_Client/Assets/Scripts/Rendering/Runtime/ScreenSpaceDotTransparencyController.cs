using UnityEngine;

namespace ThirdPersonRendering
{
    [ExecuteAlways]
    public sealed class ScreenSpaceDotTransparencyController : MonoBehaviour
    {
        static readonly int EnabledId = Shader.PropertyToID("_ScreenDotTransparencyEnabled");
        static readonly int CoverageId = Shader.PropertyToID("_ScreenDotCoverage");
        static readonly int SpacingPixelsId = Shader.PropertyToID("_ScreenDotSpacingPixels");
        static readonly int RadiusId = Shader.PropertyToID("_ScreenDotRadius");
        static readonly int HardnessId = Shader.PropertyToID("_ScreenDotHardness");
        static readonly int OffsetPixelsId = Shader.PropertyToID("_ScreenDotOffsetPixels");

        [SerializeField] ScreenSpaceDotTransparencyProfile profile;
        [SerializeField] Renderer[] targetRenderers;
        [SerializeField] bool applyOnEnable = true;
        [SerializeField] bool clearOnDisable = true;

        MaterialPropertyBlock propertyBlock;

        public ScreenSpaceDotTransparencyProfile Profile
        {
            get => profile;
            set => profile = value;
        }

        public Renderer[] TargetRenderers
        {
            get => targetRenderers;
            set => targetRenderers = value;
        }

        public bool ApplyOnEnable
        {
            get => applyOnEnable;
            set => applyOnEnable = value;
        }

        public bool ClearOnDisable
        {
            get => clearOnDisable;
            set => clearOnDisable = value;
        }

        public bool HasProfile => profile != null;
        public bool HasTargets => targetRenderers != null && targetRenderers.Length > 0;

        void OnEnable()
        {
            if (applyOnEnable)
                TryApplyProfile();
        }

        void OnDisable()
        {
            if (clearOnDisable && HasTargets)
                TryApplySettings(ScreenSpaceDotTransparencySettings.Disabled);
        }

        [ContextMenu("Apply Screen Dot Transparency")]
        public void ApplyProfile()
        {
            TryApplyProfile();
        }

        [ContextMenu("Clear Screen Dot Transparency")]
        public void Clear()
        {
            TryApplySettings(ScreenSpaceDotTransparencySettings.Disabled);
        }

        public bool TryApplyProfile()
        {
            if (profile == null)
            {
                Debug.LogError("ScreenSpaceDotTransparencyController 缺少 ScreenSpaceDotTransparencyProfile", this);
                if (HasTargets)
                    TryApplySettings(ScreenSpaceDotTransparencySettings.Disabled);
                return false;
            }

            return TryApplySettings(profile.NormalizedSettings);
        }

        public bool TryApplySettings(ScreenSpaceDotTransparencySettings settings)
        {
            if (!HasTargets)
            {
                Debug.LogError("ScreenSpaceDotTransparencyController 缺少目标 Renderer", this);
                return false;
            }

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            bool applied = false;
            for (int i = 0; i < targetRenderers.Length; i++)
                applied |= ApplyToRenderer(targetRenderers[i], settings, propertyBlock);

            return applied;
        }

        public static bool ApplyToRenderer(Renderer target, ScreenSpaceDotTransparencySettings settings, MaterialPropertyBlock propertyBlock)
        {
            if (target == null || propertyBlock == null)
                return false;

            target.GetPropertyBlock(propertyBlock);
            target.SetPropertyBlock(BuildPropertyBlock(propertyBlock, settings));
            return true;
        }

        public static MaterialPropertyBlock BuildPropertyBlock(MaterialPropertyBlock propertyBlock, ScreenSpaceDotTransparencySettings settings)
        {
            propertyBlock.SetFloat(EnabledId, settings.IsActive ? 1f : 0f);
            propertyBlock.SetFloat(CoverageId, settings.Coverage);
            propertyBlock.SetFloat(SpacingPixelsId, settings.SpacingPixels);
            propertyBlock.SetFloat(RadiusId, settings.Radius);
            propertyBlock.SetFloat(HardnessId, settings.Hardness);
            propertyBlock.SetVector(OffsetPixelsId, new Vector4(settings.OffsetPixels.x, settings.OffsetPixels.y, 0f, 0f));
            return propertyBlock;
        }
    }
}
