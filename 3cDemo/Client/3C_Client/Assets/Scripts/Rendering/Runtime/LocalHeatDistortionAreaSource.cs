using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonRendering
{
    public sealed class LocalHeatDistortionAreaSource : MonoBehaviour
    {
        static readonly List<LocalHeatDistortionAreaSource> ActiveSources = new List<LocalHeatDistortionAreaSource>();

        [SerializeField] LocalHeatDistortionAreaShape shape = LocalHeatDistortionAreaShape.ScreenEllipse;
        [SerializeField, Min(LocalHeatDistortionAreaSettings.MinRadius)] float radius = 1.5f;
        [SerializeField] float aspect = 1f;
        [SerializeField] bool overrideMode;
        [SerializeField] LocalHeatDistortionMode mode = LocalHeatDistortionMode.HeatHaze;
        [SerializeField] bool particlesVisible = true;
        [SerializeField] ParticleSystem[] particleSystems = new ParticleSystem[0];

        public LocalHeatDistortionAreaShape Shape
        {
            get => shape;
            set => shape = value;
        }

        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(LocalHeatDistortionAreaSettings.MinRadius, value);
        }

        public float Aspect
        {
            get => aspect;
            set => aspect = Mathf.Clamp(value, LocalHeatDistortionAreaSettings.MinAspect, LocalHeatDistortionAreaSettings.MaxAspect);
        }

        public bool OverrideMode
        {
            get => overrideMode;
            set => overrideMode = value;
        }

        public LocalHeatDistortionMode Mode
        {
            get => mode;
            set => mode = value;
        }

        public bool ParticlesVisible
        {
            get => particlesVisible;
            set
            {
                particlesVisible = value;
                ApplyParticleVisibility();
            }
        }

        public IReadOnlyList<ParticleSystem> ParticleSystems => particleSystems;
        public bool HasParticlePreview => particleSystems != null && particleSystems.Length > 0;
        public static int ActiveSourceCount => ActiveSources.Count;

        public static bool TryResolveArea(Camera camera, float softness, out LocalHeatDistortionAreaSettings areaSettings, out LocalHeatDistortionAreaSource source)
        {
            for (int i = ActiveSources.Count - 1; i >= 0; i--)
            {
                LocalHeatDistortionAreaSource candidate = ActiveSources[i];
                if (candidate == null)
                {
                    ActiveSources.RemoveAt(i);
                    continue;
                }

                if (candidate.TryBuildAreaSettings(camera, softness, out areaSettings))
                {
                    source = candidate;
                    return true;
                }
            }

            source = null;
            areaSettings = LocalHeatDistortionAreaSettings.Invalid;
            return false;
        }

        public bool TryBuildAreaSettings(Camera camera, float softness, out LocalHeatDistortionAreaSettings areaSettings)
        {
            areaSettings = LocalHeatDistortionAreaSettings.Invalid;
            if (!isActiveAndEnabled || camera == null || radius < LocalHeatDistortionAreaSettings.MinRadius)
                return false;

            Vector3 center = camera.WorldToViewportPoint(transform.position);
            if (center.z <= 0f || center.x < -1f || center.x > 2f || center.y < -1f || center.y > 2f)
                return false;

            Vector3 right = camera.WorldToViewportPoint(transform.position + transform.right * radius);
            Vector3 up = camera.WorldToViewportPoint(transform.position + transform.up * radius / Mathf.Max(Aspect, 0.0001f));
            float screenRadius = Mathf.Max(
                Vector2.Distance(new Vector2(center.x, center.y), new Vector2(right.x, right.y)),
                Vector2.Distance(new Vector2(center.x, center.y), new Vector2(up.x, up.y)));

            if (screenRadius < LocalHeatDistortionAreaSettings.MinRadius)
                return false;

            float rotation = -transform.eulerAngles.z * Mathf.Deg2Rad;
            areaSettings = new LocalHeatDistortionAreaSettings(
                new Vector2(center.x, center.y),
                screenRadius,
                Aspect,
                rotation,
                softness,
                center.z,
                shape);
            return areaSettings.IsValid;
        }

        void OnEnable()
        {
            if (!ActiveSources.Contains(this))
                ActiveSources.Add(this);

            ApplyParticleVisibility();
        }

        void OnDisable()
        {
            ActiveSources.Remove(this);
        }

        void OnValidate()
        {
            Radius = radius;
            Aspect = aspect;
            if (particleSystems == null || particleSystems.Length == 0)
                particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            ApplyParticleVisibility();
        }

        void ApplyParticleVisibility()
        {
            if (particleSystems == null)
                return;

            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particle = particleSystems[i];
                if (particle == null)
                    continue;

                ParticleSystem.EmissionModule emission = particle.emission;
                emission.enabled = particlesVisible;
                ParticleSystemRenderer renderer = particle.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                    renderer.enabled = particlesVisible;
            }
        }
    }
}
