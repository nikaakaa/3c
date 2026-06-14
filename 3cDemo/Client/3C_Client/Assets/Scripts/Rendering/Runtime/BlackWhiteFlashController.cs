using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ThirdPersonRendering
{
    [ExecuteAlways]
    public sealed class BlackWhiteFlashController : MonoBehaviour
    {
        [SerializeField] Volume targetVolume;
        [SerializeField] BlackWhiteFlashProfile profile;
        [SerializeField] bool playOnEnable;
        [SerializeField] bool restoreIntensityOnStop = true;
        [SerializeField] bool useUnscaledTime = true;

        Vector2 activeCenter;
        float activeIntensityScale = 1f;
        float elapsed;
        bool playing;
#if UNITY_EDITOR
        double lastEditorTime;
        bool editorUpdateRegistered;
#endif

        public Volume TargetVolume
        {
            get => targetVolume;
            set => targetVolume = value;
        }

        public BlackWhiteFlashProfile Profile
        {
            get => profile;
            set => profile = value;
        }

        public bool PlayOnEnable
        {
            get => playOnEnable;
            set => playOnEnable = value;
        }

        public bool RestoreIntensityOnStop
        {
            get => restoreIntensityOnStop;
            set => restoreIntensityOnStop = value;
        }

        public bool UseUnscaledTime
        {
            get => useUnscaledTime;
            set => useUnscaledTime = value;
        }

        public bool IsPlaying => playing;
        public float Elapsed => elapsed;

        void Reset()
        {
            targetVolume = GetComponent<Volume>();
        }

        void OnEnable()
        {
            ResolveTargetVolume();

            if (playOnEnable)
                PlayDefault();
        }

        void Update()
        {
            if (!Application.isPlaying)
                return;

            Tick(useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            StopEditorPlayback();
#endif
            if (restoreIntensityOnStop)
                ApplyDisabled();
        }

        [ContextMenu("Play Default")]
        public void PlayDefault()
        {
            if (profile == null)
            {
                Debug.LogError("BlackWhiteFlashController 缺少 BlackWhiteFlashProfile", this);
                return;
            }

            Play(profile.Center, 1f);
        }

        public void Play(Vector2 screenCenter)
        {
            Play(screenCenter, 1f);
        }

        public void Play(Vector2 screenCenter, float intensityScale)
        {
            if (!CanWriteVolume())
                return;

            activeCenter = new Vector2(Mathf.Clamp01(screenCenter.x), Mathf.Clamp01(screenCenter.y));
            activeIntensityScale = Mathf.Max(0f, intensityScale);
            elapsed = 0f;
            playing = activeIntensityScale > 0f;

            if (!playing)
            {
                ApplyDisabled();
                return;
            }

#if UNITY_EDITOR
            StartEditorPlayback();
#endif
            ApplyAtNormalizedTime(0f);
        }

        public void Tick(float deltaTime)
        {
            if (!playing)
                return;

            elapsed += Mathf.Max(0f, deltaTime);
            float duration = profile != null ? profile.Duration : BlackWhiteFlashProfile.MinDuration;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            ApplyAtNormalizedTime(normalizedTime);

            if (elapsed >= duration)
                Stop();
        }

        public void Stop()
        {
            playing = false;
            elapsed = 0f;
#if UNITY_EDITOR
            StopEditorPlayback();
#endif
            if (restoreIntensityOnStop)
                ApplyDisabled();
        }

        bool CanWriteVolume()
        {
            ResolveTargetVolume();

            if (profile == null)
            {
                Debug.LogError("BlackWhiteFlashController 缺少 BlackWhiteFlashProfile", this);
                return false;
            }

            if (!TryGetBlackWhiteFlash(out _))
            {
                Debug.LogError("BlackWhiteFlashController 目标 Volume 缺少 Black White Flash 组件", targetVolume);
                return false;
            }

            return true;
        }

        void ResolveTargetVolume()
        {
            if (targetVolume == null)
                targetVolume = GetComponent<Volume>();
        }

        void ApplyAtNormalizedTime(float normalizedTime)
        {
            if (!TryGetBlackWhiteFlash(out BlackWhiteFlash blackWhiteFlash))
                return;

            BlackWhiteFlashSettings settings = profile.Evaluate(activeCenter, normalizedTime, activeIntensityScale);
            ApplySettings(blackWhiteFlash, settings);
        }

        void ApplyDisabled()
        {
            if (!TryGetBlackWhiteFlash(out BlackWhiteFlash blackWhiteFlash))
                return;

            ApplySettings(blackWhiteFlash, BlackWhiteFlashSettings.Disabled);
        }

        bool TryGetBlackWhiteFlash(out BlackWhiteFlash blackWhiteFlash)
        {
            blackWhiteFlash = null;
            if (targetVolume == null)
                return false;

            VolumeProfile volumeProfile = targetVolume.profile;
            return volumeProfile != null && volumeProfile.TryGet(out blackWhiteFlash);
        }

        static void ApplySettings(BlackWhiteFlash blackWhiteFlash, BlackWhiteFlashSettings settings)
        {
            blackWhiteFlash.active = settings.IsActive;
            blackWhiteFlash.mode.overrideState = true;
            blackWhiteFlash.intensity.overrideState = true;
            blackWhiteFlash.threshold.overrideState = true;
            blackWhiteFlash.contrast.overrideState = true;
            blackWhiteFlash.whiteBoost.overrideState = true;
            blackWhiteFlash.blackCrush.overrideState = true;
            blackWhiteFlash.invertAmount.overrideState = true;
            blackWhiteFlash.center.overrideState = true;
            blackWhiteFlash.radius.overrideState = true;
            blackWhiteFlash.softness.overrideState = true;
            blackWhiteFlash.mode.value = settings.Mode;
            blackWhiteFlash.intensity.value = settings.Intensity;
            blackWhiteFlash.threshold.value = settings.Threshold;
            blackWhiteFlash.contrast.value = settings.Contrast;
            blackWhiteFlash.whiteBoost.value = settings.WhiteBoost;
            blackWhiteFlash.blackCrush.value = settings.BlackCrush;
            blackWhiteFlash.invertAmount.value = settings.InvertAmount;
            blackWhiteFlash.center.value = settings.Center;
            blackWhiteFlash.radius.value = settings.Radius;
            blackWhiteFlash.softness.value = settings.Softness;
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
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }
#endif
    }
}
