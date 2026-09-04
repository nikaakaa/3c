using Cinemachine;
using UnityEngine;

namespace ThirdPersonCamera
{
    [DefaultExecutionOrder(-50)]
    public sealed class CinemachineCameraRigAdapter : MonoBehaviour, ICameraMovementBasisProvider, ICameraPitchProvider, ICameraRigAdapter
    {
        [SerializeField] CinemachineVirtualCamera virtualCamera;
        [SerializeField] CinemachineBrain brain;

        CameraBasisSnapshot basisSnapshot;
        CameraRigResult result;
        bool missingVirtualCameraReported;
        bool invalidBrainReported;

        public CinemachineVirtualCamera VirtualCamera { get => virtualCamera; set => virtualCamera = value; }
        public CinemachineBrain Brain { get => brain; set => brain = value; }
        public float Yaw => basisSnapshot.Valid ? basisSnapshot.Yaw : ResolveCurrentYaw();
        public float Pitch => basisSnapshot.Valid ? basisSnapshot.Pitch : ResolveCurrentPitch();
        public Vector3 CameraPlanarForward => basisSnapshot.Valid ? basisSnapshot.PlanarForward : Vector3.zero;
        public Vector3 CameraPlanarRight => basisSnapshot.Valid ? basisSnapshot.PlanarRight : Vector3.zero;
        public Vector3 LookDirection => basisSnapshot.Valid ? basisSnapshot.LookDirection : Vector3.zero;
        public Vector3 AimPoint => basisSnapshot.AimPoint;
        public CameraBasisSnapshot BasisSnapshot => basisSnapshot;
        public CameraRigResult Result => result;

        void Awake()
        {
            if (virtualCamera == null || brain == null)
                ResetComponentReferences();
            ReportMissingVirtualCamera();
            ReportInvalidBrain();
            if (virtualCamera == null || !HasValidBrain())
            {
                basisSnapshot = CameraBasisSnapshot.Invalid;
                result = default;
                return;
            }

            virtualCamera.PreviousStateIsValid = false;
            RefreshBasisSnapshot();
        }

        void ResetComponentReferences()
        {
            virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>(true);
            brain = GetComponent<CinemachineBrain>();
        }

        void OnValidate()
        {
            if (!virtualCamera)
                virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>(true);
            if (!brain)
                brain = GetComponent<CinemachineBrain>();
        }

        public void Apply(in CameraFramePlan plan)
        {
            if (!plan.Valid)
            {
                basisSnapshot = CameraBasisSnapshot.Invalid;
                result = default;
                return;
            }

            if (!CanApply())
                return;

            ApplyLens(plan.FieldOfView, plan.NearClipPlane, plan.FarClipPlane);
            virtualCamera.ForceCameraPosition(plan.Location, plan.Rotation);
            if (plan.ResetHistory)
                virtualCamera.PreviousStateIsValid = false;
            UpdateBrain();
            RefreshBasisSnapshot(plan.AimPoint);
        }

        public void Reset()
        {
            basisSnapshot = CameraBasisSnapshot.Invalid;
            result = default;
            if (virtualCamera != null)
                virtualCamera.PreviousStateIsValid = false;
        }

        bool CanApply()
        {
            ReportMissingVirtualCamera();
            ReportInvalidBrain();
            if (virtualCamera == null || !HasValidBrain())
            {
                basisSnapshot = CameraBasisSnapshot.Invalid;
                result = default;
                return false;
            }

            return true;
        }

        void ApplyLens(float fieldOfView, float nearClipPlane, float farClipPlane)
        {
            LensSettings lens = virtualCamera.m_Lens;
            lens.FieldOfView = Mathf.Clamp(fieldOfView, 5f, 170f);
            lens.NearClipPlane = Mathf.Max(0f, nearClipPlane);
            lens.FarClipPlane = Mathf.Max(lens.NearClipPlane + 0.001f, farClipPlane);
            virtualCamera.m_Lens = lens;
        }

        void RefreshBasisSnapshot(Vector3 aimPoint = default)
        {
            if (brain == null || brain.ActiveVirtualCamera == null)
            {
                basisSnapshot = CameraBasisSnapshot.Invalid;
                result = default;
                return;
            }

            CameraState state = brain.CurrentCameraState;
            Quaternion rotation = state.FinalOrientation;
            Vector3 lookDirection = (rotation * Vector3.forward).normalized;
            if (lookDirection.sqrMagnitude <= 0.000001f)
            {
                basisSnapshot = CameraBasisSnapshot.Invalid;
                result = default;
                return;
            }

            CameraBasisResolver.ResolvePlanarBasis(rotation, out Vector3 planarForward, out Vector3 planarRight);
            basisSnapshot = new CameraBasisSnapshot(
                planarForward,
                planarRight,
                lookDirection,
                aimPoint,
                ResolveYaw(lookDirection),
                ResolvePitch(lookDirection),
                planarForward.sqrMagnitude > 0.000001f && planarRight.sqrMagnitude > 0.000001f);
            result = new CameraRigResult(
                basisSnapshot,
                state.FinalPosition,
                rotation,
                state.Lens.FieldOfView,
                basisSnapshot.Valid);
        }

        bool HasValidBrain() => brain != null && brain.m_UpdateMethod == CinemachineBrain.UpdateMethod.ManualUpdate;

        void UpdateBrain()
        {
            if (!HasValidBrain())
            {
                ReportInvalidBrain();
                basisSnapshot = CameraBasisSnapshot.Invalid;
                result = default;
                return;
            }

            brain.ManualUpdate();
        }

        void ReportMissingVirtualCamera()
        {
            if (virtualCamera != null || missingVirtualCameraReported)
                return;

            missingVirtualCameraReported = true;
            Debug.LogError("CinemachineCameraRigAdapter requires an explicit CinemachineVirtualCamera.", this);
        }

        void ReportInvalidBrain()
        {
            if (HasValidBrain() || invalidBrainReported)
                return;

            invalidBrainReported = true;
            Debug.LogError("CinemachineCameraRigAdapter requires an explicit CinemachineBrain with Update Method set to Manual Update.", this);
        }

        float ResolveCurrentYaw()
        {
            if (brain == null || brain.ActiveVirtualCamera == null)
                return 0f;
            return ResolveYaw(brain.CurrentCameraState.FinalOrientation * Vector3.forward);
        }

        float ResolveCurrentPitch()
        {
            if (brain == null || brain.ActiveVirtualCamera == null)
                return 0f;
            return ResolvePitch(brain.CurrentCameraState.FinalOrientation * Vector3.forward);
        }

        static float ResolveYaw(Vector3 lookDirection)
        {
            if (lookDirection.sqrMagnitude <= 0.000001f)
                return 0f;
            Vector3 planar = Vector3.ProjectOnPlane(lookDirection.normalized, Vector3.up);
            if (planar.sqrMagnitude <= 0.000001f)
                return 0f;
            return Mathf.Atan2(planar.x, planar.z) * Mathf.Rad2Deg;
        }

        static float ResolvePitch(Vector3 lookDirection)
        {
            if (lookDirection.sqrMagnitude <= 0.000001f)
                return 0f;

            return Mathf.Asin(Mathf.Clamp(lookDirection.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
        }
    }
}
