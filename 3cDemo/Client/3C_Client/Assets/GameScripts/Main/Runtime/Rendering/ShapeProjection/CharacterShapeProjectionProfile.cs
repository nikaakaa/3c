using System;
using UnityEngine;

namespace ThirdPersonRendering.ShapeProjection
{
    [CreateAssetMenu(fileName = "CharacterShapeProjectionProfile", menuName = "3C/Rendering/Character Shape Projection Profile")]
    public sealed class CharacterShapeProjectionProfile : ScriptableObject
    {
        [SerializeField] ShapeProjectionProfileId profileId;
        [SerializeField, Min(1)] int revision = 1;
        [SerializeField, Min(1)] int runtimeTuningRevision = 1;
        [SerializeField] Hash128 contentHash;
        [SerializeField, Range(0f, 255f)] float colorClusterThreshold = 64f;
        [SerializeField, Range(0f, 255f)] float smallRegionMergeThreshold = 72f;
        [SerializeField, Min(0)] int smallRegionTriangleLimit = 2;
        [SerializeField, Min(1)] int minimumProjectedRegionTriangles = 4;
        [SerializeField, Min(0.25f)] float maximumSimplifyEpsilonPixels = 4f;
        [SerializeField, Min(0.25f)] float outlineWidthPixels = 1.25f;
        [SerializeField, Min(0f)] float minimumSecondaryLoopAreaPixels = 48f;
        [SerializeField, Min(0f)] float minimumSharedEdgePixels = 2f;
        [SerializeField] Color outlineColor = new Color(0.035f, 0.035f, 0.045f, 1f);
        [SerializeField] ShapeProjectionCapacity capacity = new ShapeProjectionCapacity
        {
            MaxRenderers = 8,
            MaxVertices = 120000,
            MaxTriangles = 180000,
            MaxRegions = 512,
            MaxSharedChains = 2048,
            AtlasWidth = 2048,
            AtlasHeight = 2048,
            MaxContourPoints = 262144,
            MaxLoops = 4096,
            MaxIndirectInstances = 512,
            ReadbackSlots = 3
        };
        [SerializeField] ShapeProjectionSubmeshRule[] submeshRules = Array.Empty<ShapeProjectionSubmeshRule>();

        public ShapeProjectionProfileId ProfileId => profileId;
        public int Revision => revision;
        public int RuntimeTuningRevision => runtimeTuningRevision;
        public Hash128 ContentHash => contentHash;
        public float ColorClusterThreshold => colorClusterThreshold;
        public float SmallRegionMergeThreshold => smallRegionMergeThreshold;
        public int SmallRegionTriangleLimit => smallRegionTriangleLimit;
        public int MinimumProjectedRegionTriangles => minimumProjectedRegionTriangles;
        public float MaximumSimplifyEpsilonPixels => maximumSimplifyEpsilonPixels;
        public float OutlineWidthPixels => outlineWidthPixels;
        public float MinimumSecondaryLoopAreaPixels => minimumSecondaryLoopAreaPixels;
        public float MinimumSharedEdgePixels => minimumSharedEdgePixels;
        public Color OutlineColor => outlineColor;
        public ShapeProjectionCapacity Capacity => capacity;
        public ShapeProjectionSubmeshRule[] SubmeshRules => submeshRules;

        public ShapeProjectionValidationResult ValidateProfile()
        {
            if (!profileId.IsValid)
                return ShapeProjectionValidationResult.Fail("ProfileId为空");
            if (revision < 1)
                return ShapeProjectionValidationResult.Fail("Profile revision必须大于0");
            if (runtimeTuningRevision < 1)
                return ShapeProjectionValidationResult.Fail("Profile runtime tuning revision必须大于0");
            if (!IsFinite(colorClusterThreshold) || !IsFinite(smallRegionMergeThreshold)
                                                 || !IsFinite(maximumSimplifyEpsilonPixels)
                                                 || !IsFinite(outlineWidthPixels)
                                                 || !IsFinite(minimumSecondaryLoopAreaPixels)
                                                 || !IsFinite(minimumSharedEdgePixels))
                return ShapeProjectionValidationResult.Fail("Profile包含非有限数值");
            if (colorClusterThreshold < 0f || smallRegionMergeThreshold < 0f || smallRegionTriangleLimit < 0
                                                || minimumProjectedRegionTriangles < 1)
                return ShapeProjectionValidationResult.Fail("Region分区参数无效");
            if (maximumSimplifyEpsilonPixels <= 0f || outlineWidthPixels <= 0f || minimumSecondaryLoopAreaPixels < 0f || minimumSharedEdgePixels < 0f)
                return ShapeProjectionValidationResult.Fail("轮廓参数无效");
            if (!capacity.Validate(out string capacityError))
                return ShapeProjectionValidationResult.Fail(capacityError);
            if (submeshRules == null || submeshRules.Length == 0)
                return ShapeProjectionValidationResult.Fail("必须显式配置Renderer/Submesh规则");

            for (int i = 0; i < submeshRules.Length; i++)
            {
                ShapeProjectionSubmeshRule rule = submeshRules[i];
                if (string.IsNullOrWhiteSpace(rule.RendererSlotId) || rule.SubmeshIndex < 0 || rule.Material == null)
                    return ShapeProjectionValidationResult.Fail($"Renderer/Submesh规则{i}没有完整identity或Material");
                if (!IsFinite(rule.AlphaThreshold) || rule.AlphaThreshold < 0f || rule.AlphaThreshold > 1f)
                    return ShapeProjectionValidationResult.Fail($"Renderer/Submesh规则{i}的Alpha Threshold无效");
                for (int j = i + 1; j < submeshRules.Length; j++)
                {
                    if (submeshRules[j].RendererSlotId == rule.RendererSlotId
                        && submeshRules[j].SubmeshIndex == rule.SubmeshIndex)
                        return ShapeProjectionValidationResult.Fail($"{rule.RendererSlotId} Submesh {rule.SubmeshIndex}存在重复规则");
                }
            }

            return ShapeProjectionValidationResult.Success;
        }

        public bool TryGetSubmeshRule(string rendererSlotId, int submeshIndex, Material material,
            out ShapeProjectionSubmeshRule rule)
        {
            if (submeshRules != null)
            {
                for (int i = 0; i < submeshRules.Length; i++)
                {
                    if (submeshRules[i].RendererSlotId != rendererSlotId
                        || submeshRules[i].SubmeshIndex != submeshIndex
                        || submeshRules[i].Material != material)
                        continue;
                    rule = submeshRules[i];
                    return true;
                }
            }

            rule = default;
            return false;
        }

#if UNITY_EDITOR
        public void EnsureIdentity()
        {
            if (!profileId.IsValid)
                profileId = new ShapeProjectionProfileId(Guid.NewGuid().ToString("N"));
        }

        public void PublishContentHash(Hash128 hash)
        {
            contentHash = hash;
        }

        public void ReplaceSubmeshRules(ShapeProjectionSubmeshRule[] rules)
        {
            submeshRules = rules ?? Array.Empty<ShapeProjectionSubmeshRule>();
            InvalidatePublishedContent();
        }

        public void ReplaceCapacity(ShapeProjectionCapacity value)
        {
            capacity = value;
            InvalidatePublishedContent();
        }

        public void InvalidatePublishedContent()
        {
            contentHash = default;
            revision++;
            runtimeTuningRevision++;
        }

        public void RecordRuntimeTuningChange()
        {
            runtimeTuningRevision++;
        }
#endif

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
