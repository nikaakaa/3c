using Cinemachine;
using UnityEngine;

namespace ThirdPersonCamera
{
    [DefaultExecutionOrder(-50)]
    public sealed class ThirdPersonCameraController : MonoBehaviour, ICameraMovementBasisProvider, ICameraPitchProvider, ICameraInfluenceSink
    {
        [SerializeField] CinemachineFreeLook freeLook;
        [SerializeField] Transform followAnchorSource;
        [SerializeField] Transform cameraFollowTarget;
        [SerializeField] Transform cameraAimTarget;
        [SerializeField] bool bindFreeLookToResolvedTargets = true;
        [SerializeField] Vector2 sensitivity = new Vector2(0.12f, 0.12f);
        [SerializeField] Vector2 pitchLimits = new Vector2(-40f, 70f);

        YawPitchState localState;
        CameraResolveResult resolveResult;
        readonly CameraInfluenceStack influenceStack = new CameraInfluenceStack();
        CameraInfluenceRequest currentInfluence;
        CinemachineResolvedTargetAdapter targetAdapter;
        Vector2 currentLookInput;
        int currentLookInputFrame = -1;

        public CinemachineFreeLook FreeLook { get => freeLook; set => freeLook = value; }
        public Transform FollowAnchorSource { get => followAnchorSource; set => followAnchorSource = value; }
        public Transform CameraFollowTarget { get => cameraFollowTarget; set { cameraFollowTarget = value; targetAdapter = null; } }
        public Transform CameraAimTarget { get => cameraAimTarget; set { cameraAimTarget = value; targetAdapter = null; } }
        public bool BindFreeLookToResolvedTargets { get => bindFreeLookToResolvedTargets; set => bindFreeLookToResolvedTargets = value; }
        public Vector2 Sensitivity { get => sensitivity; set => sensitivity = value; }
        public Vector2 PitchLimits { get => NormalizePitchLimits(pitchLimits); set => pitchLimits = NormalizePitchLimits(value); }
        public float Yaw => ResolveYaw();
        public float Pitch => ResolvePitch();
        public Vector3 CameraPlanarForward => resolveResult.CameraPlanarForward;
        public Vector3 CameraPlanarRight => resolveResult.CameraPlanarRight;
        public Vector3 LookDirection => resolveResult.LookDirection;
        public Vector3 AimPoint => resolveResult.AimPoint;
        public CameraInfluenceRequest CurrentInfluence => currentInfluence;
        public int InfluenceSourceCount => influenceStack.Count;

        void Awake()
        {
            ResolveDefaultReferences();
            ResolveTargetAdapter();
            CaptureFollowAnchorSourceFromFreeLook();
            targetAdapter.EnsureTargets();
            if (bindFreeLookToResolvedTargets)
                targetAdapter.BindFreeLook();
            pitchLimits = NormalizePitchLimits(pitchLimits);
            Output(ReadFollowAnchor());
        }

        void Reset()
        {
            ResolveDefaultReferences();
            ResolveTargetAdapter();
            pitchLimits = NormalizePitchLimits(pitchLimits);
        }

        void OnValidate()
        {
            pitchLimits = NormalizePitchLimits(pitchLimits);
        }
        void LateUpdate()
        {
            Resolve();
        }

        public void Tick(CameraLookIntent intent)
        {
            ApplyLook(intent);
            Output(ReadFollowAnchor());
        }

        public void Tick(CameraLookIntent intent, CameraFollowAnchor followAnchor)
        {
            ApplyLook(intent);
            Output(followAnchor);
        }

        public void Tick(Vector2 lookDelta) { Tick(new CameraLookIntent(lookDelta)); }

        public void Tick(Vector2 lookDelta, Vector3 followPosition)
        {
            Tick(new CameraLookIntent(lookDelta), new CameraFollowAnchor(followPosition));
        }

        public void ResetState(float yawValue, float pitchValue)
        {
            Vector2 limits = PitchLimits;
            float yaw = Mathf.Repeat(yawValue, 360f);
            float pitch = Mathf.Clamp(pitchValue, limits.x, limits.y);
            localState.Reset(yaw, pitch, limits);
            if (freeLook != null)
            {
                freeLook.m_XAxis.Value = yaw;
                freeLook.m_YAxis.Value = Mathf.InverseLerp(limits.x, limits.y, pitch);
                freeLook.PreviousStateIsValid = false;
            }

            Output(ReadFollowAnchor());
        }

        public CameraInfluenceHandle CreateInfluenceHandle(CameraInfluenceRequest initialRequest)
        {
            CameraInfluenceHandle handle = influenceStack.CreateHandle(initialRequest);
            Output(ReadFollowAnchor());
            return handle;
        }

        public void RegisterInfluenceSource(ICameraInfluenceSource source)
        {
            influenceStack.Register(source);
            Output(ReadFollowAnchor());
        }

        public void UnregisterInfluenceSource(ICameraInfluenceSource source)
        {
            influenceStack.Unregister(source);
            Output(ReadFollowAnchor());
        }

        public void ApplyLook(Vector2 lookDelta)
        {
            ApplyLook(new CameraLookIntent(lookDelta));
        }

        public void ApplyLook(CameraLookIntent intent)
        {
            currentLookInput = intent.Delta;
            currentLookInputFrame = Time.frameCount;
            if (freeLook == null)
                localState.Apply(intent, sensitivity, PitchLimits);
            CameraFollowAnchor followAnchor = ReadFollowAnchor();
            resolveResult = ResolveData(followAnchor);
            ApplyTargets(resolveResult);
        }

        public void Resolve(Vector3 followPosition)
        {
            Resolve(new CameraFollowAnchor(followPosition));
        }

        public void Resolve()
        {
            Output(ReadFollowAnchor());
        }

        public void Resolve(CameraFollowAnchor followAnchor)
        {
            Output(followAnchor);
        }

        CameraResolveResult ResolveData(CameraFollowAnchor followAnchor)
        {
            Quaternion rotation = ResolveCameraRotation();
            CameraBasisResolver.ResolvePlanarBasis(rotation, out Vector3 planarForward, out Vector3 planarRight);
            CameraInfluenceRequest influence = influenceStack.Resolve(CameraInfluenceRequest.FreeDefault);
            currentInfluence = influence;
            return ThirdPersonCameraResolver.Resolve(
                followAnchor.Position,
                rotation,
                planarForward,
                planarRight,
                influence.AimIntent);
        }

        void Output(CameraFollowAnchor followAnchor)
        {
            resolveResult = ResolveData(followAnchor);
            ApplyTargets(resolveResult);
        }

        CameraFollowAnchor ReadFollowAnchor()
        {
            Transform anchor = ResolveRuntimeAnchor();
            return new CameraFollowAnchor(anchor != null ? anchor.position : transform.position);
        }

        void ResolveDefaultReferences()
        {
            if (freeLook == null)
                freeLook = GetComponentInChildren<CinemachineFreeLook>(true);
        }

        void ResolveTargetAdapter()
        {
            targetAdapter = new CinemachineResolvedTargetAdapter(transform, freeLook, cameraFollowTarget, cameraAimTarget);
        }

        void CaptureFollowAnchorSourceFromFreeLook()
        {
            if (followAnchorSource != null || freeLook == null)
                return;

            if (freeLook.Follow != null && !IsResolvedTarget(freeLook.Follow))
            {
                followAnchorSource = freeLook.Follow;
                return;
            }

            if (freeLook.LookAt != null && !IsResolvedTarget(freeLook.LookAt))
                followAnchorSource = freeLook.LookAt;
        }

        Transform ResolveConfiguredAnchor()
        {
            if (followAnchorSource != null)
                return followAnchorSource;
            if (freeLook != null && freeLook.Follow != null && !IsResolvedTarget(freeLook.Follow))
                return freeLook.Follow;
            if (freeLook != null && freeLook.LookAt != null && !IsResolvedTarget(freeLook.LookAt))
                return freeLook.LookAt;
            return null;
        }

        Transform ResolveRuntimeAnchor()
        {
            Transform anchor = ResolveConfiguredAnchor();
            return anchor != null ? anchor : transform;
        }

        public float GetLookAxisValue(int axis)
        {
            Vector2 look = currentLookInputFrame == Time.frameCount
                ? currentLookInput
                : Vector2.zero;

            if (axis == 0)
                return look.x;
            if (axis == 1)
                return look.y;
            return 0f;
        }

        Quaternion ResolveCameraRotation()
        {
            if (freeLook != null && freeLook.PreviousStateIsValid)
                return freeLook.State.FinalOrientation;

            return Quaternion.Euler(Pitch, Yaw, 0f);
        }

        void ApplyTargets(CameraResolveResult result)
        {
            if (targetAdapter == null)
                ResolveTargetAdapter();

            targetAdapter.Apply(result);
            cameraFollowTarget = targetAdapter.FollowTarget;
            cameraAimTarget = targetAdapter.AimTarget;
        }

        bool IsResolvedTarget(Transform target)
        {
            if (targetAdapter == null)
                ResolveTargetAdapter();

            return targetAdapter.IsOutputTarget(target);
        }

        float ResolveYaw()
        {
            if (freeLook != null)
                return Mathf.Repeat(freeLook.m_XAxis.Value, 360f);
            return localState.Yaw;
        }

        float ResolvePitch()
        {
            if (freeLook != null)
            {
                Vector2 limits = PitchLimits;
                return Mathf.Lerp(limits.x, limits.y, Mathf.Clamp01(freeLook.m_YAxis.Value));
            }

            return localState.Pitch;
        }

        static Vector2 NormalizePitchLimits(Vector2 value)
        {
            return value.x <= value.y ? value : new Vector2(value.y, value.x);
        }
    }
}
