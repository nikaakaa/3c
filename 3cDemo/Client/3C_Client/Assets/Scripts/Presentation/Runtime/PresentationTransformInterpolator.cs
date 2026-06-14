using ThirdPersonSimulation;
using UnityEngine;
using UnityEngine.Serialization;

namespace ThirdPersonPresentation
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class PresentationTransformInterpolator : MonoBehaviour
    {
        [SerializeField] Transform source;
        [FormerlySerializedAs("visualAnchor")]
        [SerializeField] Transform visualTarget;
        [SerializeField] UnitySimulationTickDriver tickDriver;
        [SerializeField, Min(0f)] float snapDistance = 3f;
        [SerializeField, Min(0f)] float correctionBlendSeconds = 0.12f;

        PresentationPose previousTickPose;
        PresentationPose currentTickPose;
        PresentationPose correctionStartPose;
        float correctionDurationSeconds;
        float correctionElapsedSeconds;
        bool hasCurrentTickPose;
        bool hasPreviousTickPose;
        bool correctionActive;

        public Transform Source { get => source; set { source = value; ResetSamples(); } }
        public Transform VisualTarget { get => ResolveVisualTarget(); set => visualTarget = value; }
        public UnitySimulationTickDriver TickDriver { get => tickDriver; set => SetTickDriver(value); }
        public float SnapDistance { get => snapDistance; set => snapDistance = Mathf.Max(0f, value); }
        public float CorrectionBlendSeconds { get => correctionBlendSeconds; set => correctionBlendSeconds = Mathf.Max(0f, value); }
        public bool HasCurrentTickPose => hasCurrentTickPose;
        public bool HasPreviousTickPose => hasPreviousTickPose;
        public bool IsCorrectionActive => correctionActive;

        void Reset()
        {
            visualTarget = transform;
        }

        void Awake()
        {
            if (visualTarget == null)
                visualTarget = transform;

            CaptureSourceAsInitialSample();
        }

        void OnEnable()
        {
            Subscribe(tickDriver);
            CaptureSourceAsInitialSample();
            UpdateVisualTarget();
        }

        void OnDisable()
        {
            Unsubscribe(tickDriver);
        }

        void LateUpdate()
        {
            AdvanceCorrection(Time.unscaledDeltaTime);
            UpdateVisualTarget();
        }

        public void ResetSamples()
        {
            hasCurrentTickPose = false;
            hasPreviousTickPose = false;
            previousTickPose = default;
            currentTickPose = default;
            correctionStartPose = default;
            correctionDurationSeconds = 0f;
            correctionElapsedSeconds = 0f;
            correctionActive = false;
        }

        public PresentationDebugRestoreState CaptureDebugRestoreState()
        {
            return new PresentationDebugRestoreState(
                previousTickPose,
                currentTickPose,
                correctionStartPose,
                correctionDurationSeconds,
                correctionElapsedSeconds,
                hasPreviousTickPose,
                hasCurrentTickPose,
                correctionActive);
        }

        public void RestoreDebugRestoreState(in PresentationDebugRestoreState state)
        {
            previousTickPose = state.PreviousTickPose;
            currentTickPose = state.CurrentTickPose;
            correctionStartPose = state.CorrectionStartPose;
            correctionDurationSeconds = Mathf.Max(0f, state.CorrectionDurationSeconds);
            correctionElapsedSeconds = Mathf.Max(0f, state.CorrectionElapsedSeconds);
            hasPreviousTickPose = state.HasPreviousTickPose;
            hasCurrentTickPose = state.HasCurrentTickPose;
            correctionActive = state.CorrectionActive;
        }

        public void CaptureSourceSample()
        {
            if (source == null)
                return;

            if (!hasCurrentTickPose)
            {
                currentTickPose = PresentationPose.FromTransform(source);
                previousTickPose = currentTickPose;
                hasCurrentTickPose = true;
                hasPreviousTickPose = false;
                return;
            }

            previousTickPose = currentTickPose;
            currentTickPose = PresentationPose.FromTransform(source);
            hasPreviousTickPose = true;
        }

        public PresentationPose ResolvePose(float interpolationAlpha)
        {
            if (source == null)
            {
                Transform target = ResolveVisualTarget();
                return new PresentationPose(target.position, target.rotation);
            }

            if (correctionActive)
            {
                float alpha = correctionDurationSeconds <= 0f
                    ? 1f
                    : Mathf.Clamp01(correctionElapsedSeconds / correctionDurationSeconds);
                return PresentationTransformResolver.Resolve(
                    correctionStartPose,
                    PresentationPose.FromTransform(source),
                    alpha,
                    true,
                    0f);
            }

            if (!hasCurrentTickPose || tickDriver == null)
                return PresentationPose.FromTransform(source);

            return PresentationTransformResolver.Resolve(
                previousTickPose,
                currentTickPose,
                interpolationAlpha,
                hasPreviousTickPose,
                snapDistance);
        }

        public void UpdateVisualTarget()
        {
            Transform target = ResolveVisualTarget();
            if (target == null || target == source)
                return;

            PresentationPose pose = ResolvePose(tickDriver != null ? tickDriver.InterpolationAlpha : 1f);
            target.SetPositionAndRotation(pose.Position, pose.Rotation);
        }

        public void BeginCorrectionFromCurrentVisual()
        {
            BeginCorrectionFromCurrentVisual(correctionBlendSeconds);
        }

        public void BeginCorrectionFromCurrentVisual(float durationSeconds)
        {
            Transform target = ResolveVisualTarget();
            if (target == null)
                return;

            BeginCorrection(PresentationPose.FromTransform(target), durationSeconds);
        }

        public void BeginCorrection(PresentationPose visualStartPose, float durationSeconds)
        {
            if (source == null)
                return;

            CaptureSourceAsInitialSample();
            correctionStartPose = visualStartPose;
            correctionDurationSeconds = Mathf.Max(0f, durationSeconds);
            correctionElapsedSeconds = 0f;
            correctionActive = correctionDurationSeconds > 0f;
        }

        public void AdvanceCorrection(float deltaSeconds)
        {
            if (!correctionActive)
                return;

            correctionElapsedSeconds += Mathf.Max(0f, deltaSeconds);
            if (correctionElapsedSeconds < correctionDurationSeconds)
                return;

            correctionElapsedSeconds = correctionDurationSeconds;
            correctionActive = false;
            CaptureSourceAsInitialSample();
        }

        void OnTickProduced(SimulationTickContext context)
        {
            CaptureSourceSample();
        }

        void SetTickDriver(UnitySimulationTickDriver value)
        {
            if (tickDriver == value)
                return;

            if (isActiveAndEnabled)
                Unsubscribe(tickDriver);

            tickDriver = value;

            if (isActiveAndEnabled)
                Subscribe(tickDriver);
        }

        void CaptureSourceAsInitialSample()
        {
            if (source == null)
                return;

            currentTickPose = PresentationPose.FromTransform(source);
            previousTickPose = currentTickPose;
            hasCurrentTickPose = true;
            hasPreviousTickPose = false;
        }

        Transform ResolveVisualTarget()
        {
            if (visualTarget == null)
                visualTarget = transform;

            return visualTarget;
        }

        void Subscribe(UnitySimulationTickDriver driver)
        {
            if (driver != null)
                driver.TickProduced += OnTickProduced;
        }

        void Unsubscribe(UnitySimulationTickDriver driver)
        {
            if (driver != null)
                driver.TickProduced -= OnTickProduced;
        }
    }
}
