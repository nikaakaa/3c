using Cinemachine;
using UnityEngine;

namespace ThirdPersonCamera
{
    [DefaultExecutionOrder(-50)]
    public sealed class ThirdPersonCameraController : MonoBehaviour, ICameraMovementBasisProvider, ICameraPitchProvider, ICameraRigAdapter
    {
        [SerializeField] CinemachineFreeLook freeLook;
        [SerializeField] CinemachineBrain brain;
        [SerializeField] Transform cameraFollowTarget;
        [SerializeField] Transform cameraAimTarget;
        [SerializeField] bool bindFreeLookToResolvedTargets = true;
        [SerializeField] Vector2 sensitivity = new Vector2(0.12f, 0.0025f);

        CameraBasisSnapshot basisSnapshot;
        bool missingFreeLookReported;
        bool missingTargetReported;
        bool invalidBrainReported;

        public CinemachineFreeLook FreeLook { get => freeLook; set => freeLook = value; }
        public CinemachineBrain Brain { get => brain; set => brain = value; }
        public Transform CameraFollowTarget { get => cameraFollowTarget; set => cameraFollowTarget = value; }
        public Transform CameraAimTarget { get => cameraAimTarget; set => cameraAimTarget = value; }
        public bool BindFreeLookToResolvedTargets { get => bindFreeLookToResolvedTargets; set => bindFreeLookToResolvedTargets = value; }
        public Vector2 Sensitivity { get => sensitivity; set => sensitivity = value; }
        public float VerticalOrbitValue => freeLook != null ? Mathf.Clamp01(freeLook.m_YAxis.Value) : 0f;
        public float Yaw => ResolveYaw();
        public float Pitch => basisSnapshot.Valid ? basisSnapshot.Pitch : ResolveCurrentPitch();
        public Vector3 CameraPlanarForward => basisSnapshot.Valid ? basisSnapshot.PlanarForward : Vector3.zero;
        public Vector3 CameraPlanarRight => basisSnapshot.Valid ? basisSnapshot.PlanarRight : Vector3.zero;
        public Vector3 LookDirection => basisSnapshot.Valid ? basisSnapshot.LookDirection : Vector3.zero;
        public Vector3 AimPoint => basisSnapshot.AimPoint;
        public CameraBasisSnapshot BasisSnapshot => basisSnapshot;

        void Awake()
        {
            ReportMissingFreeLook();
            ReportInvalidBrain();
            ReportMissingTargets();
            if (freeLook == null || !HasValidBrain() || !HasTargets())
            {
                basisSnapshot = CameraBasisSnapshot.Invalid;
                return;
            }

            ClearFreeLookInput();
            BindFreeLookTargets();
            RefreshBasisSnapshot();
        }

        void Reset()
        {
            freeLook = GetComponentInChildren<CinemachineFreeLook>(true);
            brain = GetComponent<CinemachineBrain>();
        }

        public void Apply(CameraPosePlan plan)
        {
            if (!plan.Valid)
            {
                basisSnapshot = CameraBasisSnapshot.Invalid;
                return;
            }

            if (!CanApply())
                return;

            ApplyLookDelta(plan.LookDelta);
            ApplyTargets(plan.FollowPoint, plan.AimPoint);
            ApplyLens(plan.FieldOfView);
            UpdateBrain();
            RefreshBasisSnapshot(plan.AimPoint);
        }

        public void SnapTargets(Vector3 followPoint, Vector3 aimPoint)
        {
            ReportMissingTargets();
            if (!HasTargets())
                return;

            ApplyTargets(followPoint, aimPoint);
            BindFreeLookTargets();
        }

        public void ResetOrbitState(float yawValue, float verticalOrbitValue)
        {
            if (freeLook == null)
            {
                ReportMissingFreeLook();
                basisSnapshot = CameraBasisSnapshot.Invalid;
                return;
            }

            freeLook.m_XAxis.Value = ResolveAxisValue(
                yawValue,
                freeLook.m_XAxis.m_MinValue,
                freeLook.m_XAxis.m_MaxValue,
                freeLook.m_XAxis.m_Wrap);
            freeLook.m_YAxis.Value = Mathf.Clamp01(verticalOrbitValue);
            freeLook.PreviousStateIsValid = false;
            ClearFreeLookInput();
            RefreshBasisSnapshot();
        }

        bool CanApply()
        {
            ReportMissingFreeLook();
            ReportInvalidBrain();
            ReportMissingTargets();
            if (freeLook == null || !HasValidBrain() || !HasTargets())
            {
                basisSnapshot = CameraBasisSnapshot.Invalid;
                return false;
            }

            ClearFreeLookInput();
            BindFreeLookTargets();
            return true;
        }

        void ApplyLookDelta(Vector2 lookDelta)
        {
            if (freeLook == null || lookDelta.sqrMagnitude <= 0f)
                return;

            freeLook.m_XAxis.Value = ResolveAxisValue(
                freeLook.m_XAxis.Value + lookDelta.x * sensitivity.x,
                freeLook.m_XAxis.m_MinValue,
                freeLook.m_XAxis.m_MaxValue,
                freeLook.m_XAxis.m_Wrap);
            freeLook.m_YAxis.Value = Mathf.Clamp01(freeLook.m_YAxis.Value - lookDelta.y * sensitivity.y);
            ClearFreeLookInput();
        }

        void ApplyTargets(Vector3 followPoint, Vector3 aimPoint)
        {
            cameraFollowTarget.position = followPoint;
            cameraAimTarget.position = aimPoint;
        }

        void BindFreeLookTargets()
        {
            if (!bindFreeLookToResolvedTargets || freeLook == null || !HasTargets())
                return;

            freeLook.Follow = cameraFollowTarget;
            freeLook.LookAt = cameraAimTarget;
        }

        void ClearFreeLookInput()
        {
            if (freeLook == null)
                return;

            freeLook.m_XAxis.m_InputAxisName = string.Empty;
            freeLook.m_YAxis.m_InputAxisName = string.Empty;
            freeLook.m_XAxis.m_InputAxisValue = 0f;
            freeLook.m_YAxis.m_InputAxisValue = 0f;
            freeLook.m_XAxis.SetInputAxisProvider(0, null);
            freeLook.m_YAxis.SetInputAxisProvider(1, null);
        }

        void ApplyLens(float fieldOfView)
        {
            LensSettings lens = freeLook.m_Lens;
            lens.FieldOfView = Mathf.Max(1f, fieldOfView);
            freeLook.m_Lens = lens;
        }

        void RefreshBasisSnapshot()
        {
            RefreshBasisSnapshot(cameraAimTarget != null ? cameraAimTarget.position : Vector3.zero);
        }

        void RefreshBasisSnapshot(Vector3 aimPoint)
        {
            if (freeLook == null || !freeLook.PreviousStateIsValid)
            {
                basisSnapshot = CameraBasisSnapshot.Invalid;
                return;
            }

            Quaternion rotation = freeLook.State.FinalOrientation;
            Vector3 lookDirection = (rotation * Vector3.forward).normalized;
            if (lookDirection.sqrMagnitude <= 0.000001f)
            {
                basisSnapshot = CameraBasisSnapshot.Invalid;
                return;
            }

            CameraBasisResolver.ResolvePlanarBasis(rotation, out Vector3 planarForward, out Vector3 planarRight);
            basisSnapshot = new CameraBasisSnapshot(
                planarForward,
                planarRight,
                lookDirection,
                aimPoint,
                Yaw,
                ResolvePitch(lookDirection),
                planarForward.sqrMagnitude > 0.000001f && planarRight.sqrMagnitude > 0.000001f);
        }

        bool HasTargets()
        {
            return cameraFollowTarget != null && cameraAimTarget != null;
        }

        bool HasValidBrain()
        {
            return brain != null && brain.m_UpdateMethod == CinemachineBrain.UpdateMethod.ManualUpdate;
        }

        void UpdateBrain()
        {
            if (!HasValidBrain())
            {
                ReportInvalidBrain();
                basisSnapshot = CameraBasisSnapshot.Invalid;
                return;
            }

            brain.ManualUpdate();
        }

        void ReportMissingFreeLook()
        {
            if (freeLook != null || missingFreeLookReported)
                return;

            missingFreeLookReported = true;
            Debug.LogError("ThirdPersonCameraController requires an explicit CinemachineFreeLook.", this);
        }

        void ReportMissingTargets()
        {
            if (HasTargets() || missingTargetReported)
                return;

            missingTargetReported = true;
            Debug.LogError("ThirdPersonCameraController requires explicit camera follow and aim targets.", this);
        }

        void ReportInvalidBrain()
        {
            if (HasValidBrain() || invalidBrainReported)
                return;

            invalidBrainReported = true;
            Debug.LogError("ThirdPersonCameraController requires an explicit CinemachineBrain with Update Method set to Manual Update.", this);
        }

        float ResolveYaw()
        {
            return freeLook != null ? Mathf.Repeat(freeLook.m_XAxis.Value, 360f) : 0f;
        }

        float ResolveCurrentPitch()
        {
            if (freeLook == null || !freeLook.PreviousStateIsValid)
                return 0f;

            return ResolvePitch(freeLook.State.FinalOrientation * Vector3.forward);
        }

        static float ResolvePitch(Vector3 lookDirection)
        {
            if (lookDirection.sqrMagnitude <= 0.000001f)
                return 0f;

            return Mathf.Asin(Mathf.Clamp(lookDirection.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        static float ResolveAxisValue(float value, float min, float max, bool wrap)
        {
            if (!wrap)
                return Mathf.Clamp(value, min, max);

            float range = max - min;
            if (range <= 0.0001f)
                return min;

            return Mathf.Repeat(value - min, range) + min;
        }
    }
}
