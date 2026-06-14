using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ThirdPersonRendering
{
    [ExecuteAlways]
    public sealed class BlockImpactVfxController : MonoBehaviour
    {
        static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        static readonly int SoftnessId = Shader.PropertyToID("_Softness");

        [SerializeField] BlockImpactVfxProfile profile;
        [SerializeField] Renderer flashRenderer;
        [SerializeField] ParticleSystem sparkParticles;
        [SerializeField] ParticleSystemRenderer sparkRenderer;
        [SerializeField] bool playOnEnable;
        [SerializeField] bool useMainCameraBillboard = true;

        MaterialPropertyBlock propertyBlock;
        BlockImpactVfxRequest activeRequest;
        float elapsed;
        bool playing;
#if UNITY_EDITOR
        double lastEditorTime;
        bool editorUpdateRegistered;
#endif

        public BlockImpactVfxProfile Profile
        {
            get => profile;
            set => profile = value;
        }

        public bool IsPlaying => playing;

        public bool PlayOnEnable
        {
            get => playOnEnable;
            set => playOnEnable = value;
        }

        void OnEnable()
        {
            ResolveRuntimeReferences();
            if (playOnEnable)
                PlayDefault();
        }

        void Update()
        {
            if (!Application.isPlaying)
                return;

            Tick(Time.deltaTime);
        }

        void LateUpdate()
        {
            LateBillboard();
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            StopEditorPlayback();
#endif
        }

        void Tick(float deltaTime)
        {
            if (!playing)
                return;

            elapsed += Mathf.Max(0f, deltaTime);
            float duration = Mathf.Max(activeRequest.Duration, BlockImpactVfxRequest.MinDuration);
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float remaining = 1f - normalizedTime;
            ApplyFlash(remaining * remaining);

            if (elapsed >= duration)
                Stop();
        }

        void LateBillboard()
        {
            if (!playing || !useMainCameraBillboard)
                return;

            Camera camera = Camera.main;
            if (camera == null)
                return;

            Billboard(flashRenderer, camera);
        }

        [ContextMenu("Play Default")]
        public void PlayDefault()
        {
            Play(BlockImpactVfxRequest.Default.WithWorldHitPoint(transform.position));
        }

        public void Play(BlockImpactVfxRequest request)
        {
            ResolveRuntimeReferences();
            if (profile == null)
            {
                Debug.LogError("BlockImpactVfxController 缺少 BlockImpactVfxProfile", this);
                return;
            }

            if (!profile.ValidateRequiredTextures(out string message))
            {
                Debug.LogError(message, profile);
                return;
            }

            activeRequest = request;
            elapsed = 0f;
            playing = request.Intensity > 0f;
            transform.SetPositionAndRotation(request.WorldHitPoint, Quaternion.identity);

            SetLayerActive(flashRenderer, playing && request.FlashEnabled && profile.CoreFlashEnabled, profile.FlashScale);
            ConfigureAndEmitSparks(request);
            SubmitScreenPulse(request);

            if (playing)
            {
#if UNITY_EDITOR
                StartEditorPlayback();
#endif
            }

            Tick(0f);
        }

        public void Stop()
        {
#if UNITY_EDITOR
            StopEditorPlayback();
#endif
            playing = false;
            SetLayerActive(flashRenderer, false, Vector2.one);

            if (sparkParticles != null)
            {
                sparkParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                sparkParticles.gameObject.SetActive(false);
            }
        }

        void ApplyFlash(float alpha)
        {
            if (flashRenderer == null || !flashRenderer.gameObject.activeSelf)
                return;

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            float intensity = profile != null ? profile.HdrIntensity * activeRequest.Intensity : activeRequest.Intensity;
            flashRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(TintColorId, profile != null ? profile.FlashColor : Color.white);
            propertyBlock.SetFloat(IntensityId, intensity);
            propertyBlock.SetFloat(AlphaId, Mathf.Clamp01(alpha));
            propertyBlock.SetFloat(SoftnessId, profile != null ? profile.FlashSoftness : 0.32f);
            flashRenderer.SetPropertyBlock(propertyBlock);
        }

        void SubmitScreenPulse(BlockImpactVfxRequest request)
        {
            if (!playing || profile == null)
                return;

            float streakWeight = request.StreakEnabled && profile.ScreenStreakEnabled ? 1f : 0f;
            float pulseWeight = request.ScreenImpactEnabled && profile.ScreenPulseEnabled ? 1f : 0f;
            if (streakWeight <= 0f && pulseWeight <= 0f)
                return;

            BlockImpactPostProcessPulse.Submit(
                request.ScreenCenter,
                request.Intensity * profile.ScreenImpactStrength,
                Mathf.Min(request.Duration, profile.Duration),
                profile.ScreenStreakLength,
                profile.ScreenStreakThickness,
                profile.ScreenStreakSoftness,
                profile.StreakColor,
                pulseWeight * profile.ScreenFlashWeight,
                pulseWeight * profile.ScreenRadialWeight,
                streakWeight * profile.ScreenStreakWeight,
                pulseWeight * profile.ScreenChromaticWeight);
        }

        void ConfigureAndEmitSparks(BlockImpactVfxRequest request)
        {
            if (sparkParticles == null)
                return;

            if (!request.SparksEnabled || !playing || profile == null || !profile.SparksEnabled || profile.SparkCount <= 0)
            {
                sparkParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                sparkParticles.gameObject.SetActive(false);
                return;
            }

            sparkParticles.gameObject.SetActive(true);
            ParticleSystem.MainModule main = sparkParticles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = profile.SparkLifetime;
            main.startSpeed = profile.SparkSpeed * Mathf.Max(0.01f, request.Intensity);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.08f);
            main.startColor = profile.SparkColor;
            main.gravityModifier = profile.SparkGravityModifier;
            main.duration = Mathf.Max(profile.SparkLifetime, request.Duration);

            ParticleSystem.EmissionModule emission = sparkParticles.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = sparkParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = profile.SparkConeAngle;
            shape.radius = 0.035f;
            shape.randomDirectionAmount = 0.12f;

            ParticleSystem.TrailModule trails = sparkParticles.trails;
            trails.enabled = true;
            trails.lifetime = profile.SparkTrailLifetime;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(profile.SparkTrailWidth);

            ParticleSystem.LimitVelocityOverLifetimeModule limitVelocity = sparkParticles.limitVelocityOverLifetime;
            limitVelocity.enabled = profile.SparkVelocityDampen > 0f;
            limitVelocity.dampen = profile.SparkVelocityDampen;

            if (sparkRenderer != null)
            {
                sparkRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                sparkRenderer.velocityScale = profile.SparkVelocityScale;
                sparkRenderer.lengthScale = profile.SparkLengthScale;
                sparkRenderer.rotateWithStretchDirection = true;
            }

            Vector3 up = Mathf.Abs(Vector3.Dot(-request.AttackDirection, Vector3.up)) > 0.96f ? Vector3.right : Vector3.up;
            sparkParticles.transform.SetPositionAndRotation(request.WorldHitPoint, Quaternion.LookRotation(-request.AttackDirection, up));
            sparkParticles.useAutoRandomSeed = request.RandomSeed == 0;
            if (request.RandomSeed != 0)
                sparkParticles.randomSeed = (uint)request.RandomSeed;

            sparkParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sparkParticles.Emit(Mathf.RoundToInt(profile.SparkCount * request.Intensity));
        }

        void ResolveRuntimeReferences()
        {
            if (sparkRenderer == null && sparkParticles != null)
                sparkRenderer = sparkParticles.GetComponent<ParticleSystemRenderer>();
        }

        static void SetLayerActive(Renderer target, bool active, Vector2 scale)
        {
            if (target == null)
                return;

            target.gameObject.SetActive(active);
            target.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        static void Billboard(Renderer target, Camera camera)
        {
            if (target == null || !target.gameObject.activeSelf)
                return;

            Transform targetTransform = target.transform;
            targetTransform.rotation = Quaternion.LookRotation(targetTransform.position - camera.transform.position, camera.transform.up);
        }

#if UNITY_EDITOR
        void StartEditorPlayback()
        {
            if (Application.isPlaying || editorUpdateRegistered)
                return;

            lastEditorTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += EditorTick;
            editorUpdateRegistered = true;
            EditorApplication.QueuePlayerLoopUpdate();
        }

        void StopEditorPlayback()
        {
            if (!editorUpdateRegistered)
                return;

            EditorApplication.update -= EditorTick;
            editorUpdateRegistered = false;
        }

        void EditorTick()
        {
            if (this == null || Application.isPlaying)
            {
                StopEditorPlayback();
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float deltaTime = Mathf.Min(0.05f, Mathf.Max(0f, (float)(now - lastEditorTime)));
            lastEditorTime = now;
            Tick(deltaTime);
            LateBillboard();
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }
#endif
    }
}
