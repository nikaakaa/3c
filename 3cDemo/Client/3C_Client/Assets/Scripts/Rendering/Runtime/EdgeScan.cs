using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ThirdPersonRendering
{
    [Serializable]
    [VolumeComponentMenuForRenderPipeline("Post-processing/3C/Edge Scan", typeof(UniversalRenderPipeline))]
    public sealed class EdgeScan : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, EdgeScanSettings.MinIntensity, EdgeScanSettings.MaxIntensity);
        public Vector3Parameter origin = new Vector3Parameter(Vector3.zero);
        public ClampedFloatParameter radius = new ClampedFloatParameter(0f, EdgeScanSettings.MinRadius, EdgeScanSettings.MaxRadius);
        public ClampedFloatParameter width = new ClampedFloatParameter(2f, EdgeScanSettings.MinWidth, EdgeScanSettings.MaxWidth);
        public ColorParameter color = new ColorParameter(new Color(0.2f, 0.85f, 1f, 1f), true, true, true);
        public ClampedFloatParameter depthThreshold = new ClampedFloatParameter(0.08f, EdgeScanSettings.MinDepthThreshold, EdgeScanSettings.MaxDepthThreshold);
        public ClampedFloatParameter normalThreshold = new ClampedFloatParameter(0.25f, EdgeScanSettings.MinNormalThreshold, EdgeScanSettings.MaxNormalThreshold);
        public ClampedFloatParameter edgeStrength = new ClampedFloatParameter(1.5f, EdgeScanSettings.MinEdgeStrength, EdgeScanSettings.MaxEdgeStrength);
        public ClampedFloatParameter distanceFade = new ClampedFloatParameter(80f, EdgeScanSettings.MinDistanceFade, EdgeScanSettings.MaxDistanceFade);
        public Vector3Parameter direction = new Vector3Parameter(Vector3.forward);
        public ClampedFloatParameter arcAngle = new ClampedFloatParameter(120f, EdgeScanSettings.MinArcAngle, EdgeScanSettings.MaxArcAngle);
        public ClampedFloatParameter scanLineSpacing = new ClampedFloatParameter(1.15f, EdgeScanSettings.MinScanLineSpacing, EdgeScanSettings.MaxScanLineSpacing);
        public ClampedFloatParameter scanLineWidth = new ClampedFloatParameter(0.08f, EdgeScanSettings.MinScanLineWidth, EdgeScanSettings.MaxScanLineWidth);
        public ClampedFloatParameter scanLineStrength = new ClampedFloatParameter(1.2f, EdgeScanSettings.MinScanLineStrength, EdgeScanSettings.MaxScanLineStrength);
        public ClampedFloatParameter frontGlowStrength = new ClampedFloatParameter(1.8f, EdgeScanSettings.MinFrontGlowStrength, EdgeScanSettings.MaxFrontGlowStrength);
        public ClampedFloatParameter darkenStrength = new ClampedFloatParameter(0.22f, EdgeScanSettings.MinDarkenStrength, EdgeScanSettings.MaxDarkenStrength);

        public EdgeScanSettings NormalizedSettings => new EdgeScanSettings(
            intensity.value,
            origin.value,
            radius.value,
            width.value,
            color.value,
            depthThreshold.value,
            normalThreshold.value,
            edgeStrength.value,
            distanceFade.value,
            direction.value,
            arcAngle.value,
            scanLineSpacing.value,
            scanLineWidth.value,
            scanLineStrength.value,
            frontGlowStrength.value,
            darkenStrength.value);

        public bool IsActive()
        {
            return active && NormalizedSettings.IsActive;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
