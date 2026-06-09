using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace ThirdPersonCamera
{
    public sealed class ThirdPersonCameraController : MonoBehaviour, ICameraMovementBasisProvider, ICameraPitchProvider, ICameraInfluenceSink
    {
        [SerializeField] CinemachineFreeLook freeLook;
        [FormerlySerializedAs("followRoot")]
        [SerializeField] Transform followAnchorSource;
        [SerializeField] Transform cameraFollowTarget;
        [SerializeField] Transform cameraAimTarget;
        [SerializeField] bool bindFreeLookToResolvedTargets = true;
        [SerializeField] InputActionReference lookAction;
        [SerializeField] Vector2 sensitivity = new Vector2(0.12f, 0.12f);
        [SerializeField] Vector2 pitchLimits = new Vector2(-40f, 70f);
        [SerializeField] bool enableInputOnEnable = true;
        [SerializeField] bool autoTick = true;
        [SerializeField] bool debugLog = true;
        [SerializeField, Min(0f)] float debugLogInterval = 0.1f;

        YawPitchState fallbackState;
        CameraResolveResult resolveResult;
        readonly CameraInfluenceStack influenceStack = new CameraInfluenceStack();
        CameraInfluenceRequest currentInfluence;
        CameraInfluenceHandle legacyInfluenceHandle;
        CinemachineResolvedTargetAdapter targetAdapter;
        Vector2 currentLookInput;
        int currentLookInputFrame = -1;
        float nextInputDebugLogTime;
        float nextOutputDebugLogTime;

        public CinemachineFreeLook FreeLook { get => freeLook; set => freeLook = value; }
        public Transform FollowAnchorSource { get => followAnchorSource; set => followAnchorSource = value; }
        public Transform CameraFollowTarget { get => cameraFollowTarget; set => cameraFollowTarget = value; }
        public Transform CameraAimTarget { get => cameraAimTarget; set => cameraAimTarget = value; }
        public bool BindFreeLookToResolvedTargets { get => bindFreeLookToResolvedTargets; set => bindFreeLookToResolvedTargets = value; }
        public InputActionReference LookAction { get => lookAction; set => lookAction = value; }
        public Vector2 Sensitivity { get => sensitivity; set => sensitivity = value; }
        public Vector2 PitchLimits { get => NormalizePitchLimits(pitchLimits); set => pitchLimits = NormalizePitchLimits(value); }
        public bool AutoTick { get => autoTick; set => autoTick = value; }
        public bool DebugLog { get => debugLog; set => debugLog = value; }
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
        void OnEnable() { if (enableInputOnEnable) ManualLookSource.Enable(lookAction); }
        void OnDisable()
        {
            if (enableInputOnEnable)
                ManualLookSource.Disable(lookAction);

            if (legacyInfluenceHandle != null)
            {
                legacyInfluenceHandle.Dispose();
                legacyInfluenceHandle = null;
            }
        }
        void LateUpdate() { if (autoTick) Tick(ManualLookSource.Read(lookAction)); }

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
            fallbackState.Reset(yawValue, pitchValue, PitchLimits);
            Output(ReadFollowAnchor());
        }

        public void SetInfluence(CameraInfluenceRequest request)
        {
            if (legacyInfluenceHandle == null)
                legacyInfluenceHandle = influenceStack.CreateHandle(request);
            else
                legacyInfluenceHandle.Set(request);

            Output(ReadFollowAnchor());
        }

        public void ClearInfluence()
        {
            if (legacyInfluenceHandle != null)
            {
                legacyInfluenceHandle.Dispose();
                legacyInfluenceHandle = null;
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
            float previousYaw = Yaw;
            float previousPitch = Pitch;
            currentLookInput = intent.Delta;
            currentLookInputFrame = Time.frameCount;
            if (freeLook == null)
                fallbackState.Apply(intent, sensitivity, PitchLimits);
            CameraFollowAnchor followAnchor = ReadFollowAnchor();
            resolveResult = ResolveData(followAnchor);
            ApplyTargets(resolveResult);
            LogInput(intent.Delta, previousYaw, previousPitch, Yaw, Pitch, followAnchor);
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
            LogOutput(followAnchor, resolveResult);
        }

        CameraFollowAnchor ReadFollowAnchor()
        {
            Transform anchor = ResolveRuntimeAnchor();
            Vector3 fallback = anchor != null ? anchor.position : transform.position;
            return TransformFollowAnchorSource.Read(anchor, fallback);
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
                : ManualLookSource.Read(lookAction).Delta;

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
            return fallbackState.Yaw;
        }

        float ResolvePitch()
        {
            if (freeLook != null)
            {
                Vector2 limits = PitchLimits;
                return Mathf.Lerp(limits.x, limits.y, Mathf.Clamp01(freeLook.m_YAxis.Value));
            }

            return fallbackState.Pitch;
        }

        static Vector2 NormalizePitchLimits(Vector2 value)
        {
            return value.x <= value.y ? value : new Vector2(value.y, value.x);
        }

        void LogInput(
            Vector2 lookDelta,
            float previousYaw,
            float previousPitch,
            float currentYaw,
            float currentPitch,
            CameraFollowAnchor followAnchor)
        {
            if (!ShouldLog(ref nextInputDebugLogTime, lookDelta.sqrMagnitude > 0.000001f))
                return;

            Debug.Log(
                $"[DEBUG-CAM-CHAIN] controller.input frame={Time.frameCount} autoTick={autoTick} " +
                $"look={lookDelta.ToString("F3")} sensitivity={sensitivity.ToString("F3")} pitchLimits={PitchLimits.ToString("F3")} " +
                $"yaw={previousYaw:F3}->{currentYaw:F3} pitch={previousPitch:F3}->{currentPitch:F3} " +
                $"followAnchor={followAnchor.Position.ToString("F3")} followSource={TargetName(followAnchorSource)} " +
                $"freeLook={TargetName(freeLook != null ? freeLook.transform : null)} freeLookFollow={TargetName(freeLook != null ? freeLook.Follow : null)} " +
                $"freeLookLookAt={TargetName(freeLook != null ? freeLook.LookAt : null)}");
        }

        void LogOutput(CameraFollowAnchor followAnchor, CameraResolveResult result)
        {
            if (!ShouldLog(ref nextOutputDebugLogTime, false))
                return;

            Debug.Log(
                $"[DEBUG-CAM-CHAIN] controller.output frame={Time.frameCount} autoTick={autoTick} " +
                $"anchor={followAnchor.Position.ToString("F3")} aimPoint={result.AimPoint.ToString("F3")} lookDir={result.LookDirection.ToString("F3")} " +
                $"planarForward={result.CameraPlanarForward.ToString("F3")} planarRight={result.CameraPlanarRight.ToString("F3")}");
        }

        bool ShouldLog(ref float nextLogTime, bool force)
        {
            if (!debugLog)
                return false;

            if (debugLogInterval <= 0f)
                return true;

            float now = Time.unscaledTime;
            if (!force && now < nextLogTime)
                return false;

            nextLogTime = now + debugLogInterval;
            return true;
        }

        static string TargetName(Transform target)
        {
            return target != null ? target.name : "null";
        }
    }
}
