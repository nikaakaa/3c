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

        PresentationPose previousTickPose;
        PresentationPose currentTickPose;
        bool hasCurrentTickPose;
        bool hasPreviousTickPose;

        public Transform Source { get => source; set { source = value; ResetSamples(); } }
        public Transform VisualTarget { get => ResolveVisualTarget(); set => visualTarget = value; }
        public UnitySimulationTickDriver TickDriver { get => tickDriver; set => SetTickDriver(value); }
        public float SnapDistance { get => snapDistance; set => snapDistance = Mathf.Max(0f, value); }
        public bool HasCurrentTickPose => hasCurrentTickPose;
        public bool HasPreviousTickPose => hasPreviousTickPose;

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
            UpdateVisualTarget();
        }

        public void ResetSamples()
        {
            hasCurrentTickPose = false;
            hasPreviousTickPose = false;
            previousTickPose = default;
            currentTickPose = default;
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
