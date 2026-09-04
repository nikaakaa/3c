using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

namespace ThirdPersonCamera
{
    [DefaultExecutionOrder(-50)]
    public sealed class CinemachineCameraRigAdapter : MonoBehaviour, ICameraMovementBasisProvider, ICameraPitchProvider, ICameraRigAdapter
    {
        [SerializeField] CinemachineFreeLook freeLook;
        [SerializeField] CinemachineBrain brain;
        [SerializeField] Transform cameraFollowTarget;
        [SerializeField] Transform cameraAimTarget;
        [SerializeField] bool bindFreeLookToResolvedTargets = true;

        CameraBasisSnapshot basisSnapshot;
        CameraRigResult result;
        CinemachineFreeLook.Orbit[] formalOrbitGroup;
        float[] formalScreenY;
        bool missingFreeLookReported;
        bool missingTargetReported;
        bool invalidBrainReported;

        public CinemachineFreeLook FreeLook { get => freeLook; set => freeLook = value; }
        public CinemachineBrain Brain { get => brain; set => brain = value; }
        public Transform CameraFollowTarget { get => cameraFollowTarget; set => cameraFollowTarget = value; }
        public Transform CameraAimTarget { get => cameraAimTarget; set => cameraAimTarget = value; }
        public bool BindFreeLookToResolvedTargets { get => bindFreeLookToResolvedTargets; set => bindFreeLookToResolvedTargets = value; }
        public float VerticalOrbitValue => freeLook != null ? Mathf.Clamp01(freeLook.m_YAxis.Value) : 0f;
        public float Yaw => ResolveYaw();
        public float Pitch => basisSnapshot.Valid ? basisSnapshot.Pitch : ResolveCurrentPitch();
        public Vector3 CameraPlanarForward => basisSnapshot.Valid ? basisSnapshot.PlanarForward : Vector3.zero;
        public Vector3 CameraPlanarRight => basisSnapshot.Valid ? basisSnapshot.PlanarRight : Vector3.zero;
        public Vector3 LookDirection => basisSnapshot.Valid ? basisSnapshot.LookDirection : Vector3.zero;
        public Vector3 AimPoint => basisSnapshot.AimPoint;
        public CameraBasisSnapshot BasisSnapshot => basisSnapshot;
        public CameraRigResult Result => result;

        void Awake()
        {
            if (freeLook == null || brain == null)
                ResetComponentReferences();
            ReportMissingFreeLook();
            ReportInvalidBrain();
            ReportMissingTargets();
            if (freeLook == null || !HasValidBrain() || !HasTargets())
            {
                basisSnapshot = CameraBasisSnapshot.Invalid;
                result = default;
                return;
            }

            ClearFreeLookInput();
            BindFreeLookTargets();
            RefreshBasisSnapshot();
        }

        void ResetComponentReferences()
        {
            freeLook = GetComponentInChildren<CinemachineFreeLook>(true);
            brain = GetComponent<CinemachineBrain>();
        }

        void OnValidate()
        {
            if (!freeLook)
                freeLook = GetComponentInChildren<CinemachineFreeLook>(true);
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

            ApplyTargets(plan.FollowPoint + plan.CameraOffset, plan.AimPoint + plan.CameraOffset);
            ApplyOrbit(plan.OrbitYaw, plan.OrbitPitch);
            ApplyOrbitGroup(plan);
            ApplyLens(plan.FieldOfView, plan.NearClipPlane, plan.FarClipPlane);
            ApplyDutch(plan.RollDegrees);
            if (plan.ResetHistory)
                freeLook.PreviousStateIsValid = false;
            UpdateBrain();
            RefreshBasisSnapshot(plan.AimPoint + plan.CameraOffset);
        }

        public void Reset()
        {
            ClearFreeLookInput();
            basisSnapshot = CameraBasisSnapshot.Invalid;
            result = default;
            if (freeLook != null)
            {
                freeLook.PreviousStateIsValid = false;
                RestoreFormalOrbitGroup();
            }
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
                result = default;
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
                result = default;
                return false;
            }

            ClearFreeLookInput();
            BindFreeLookTargets();
            return true;
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

        void ApplyLens(float fieldOfView, float nearClipPlane, float farClipPlane)
        {
            LensSettings lens = freeLook.m_Lens;
            lens.FieldOfView = Mathf.Max(1f, fieldOfView);
            lens.NearClipPlane = Mathf.Max(0f, nearClipPlane);
            lens.FarClipPlane = Mathf.Max(lens.NearClipPlane + 0.001f, farClipPlane);
            freeLook.m_Lens = lens;
        }

        void ApplyDutch(float rollDegrees)
        {
            LensSettings lens = freeLook.m_Lens;
            lens.Dutch = rollDegrees;
            freeLook.m_Lens = lens;
        }

        void ApplyOrbitGroup(in CameraFramePlan plan)
        {
            if (freeLook == null || freeLook.m_Orbits == null || plan.Orbit == null || plan.OrbitGroup.Count == 0)
                throw new InvalidOperationException("Camera Frame Plan has no formal orbit group for the Cinemachine FreeLook.");
            if (plan.OrbitGroup.Count != freeLook.m_Orbits.Length)
                throw new InvalidOperationException("Camera Frame Plan orbit group does not match the Cinemachine FreeLook orbit capacity.");
            for (int i = 0; i < freeLook.m_Orbits.Length; i++)
            {
                CameraOrbitPayload orbit = plan.OrbitGroup[i];
                if (orbit == null)
                    throw new InvalidOperationException($"Camera Frame Plan orbit #{i} is missing.");
                freeLook.m_Orbits[i] = new CinemachineFreeLook.Orbit(
                    orbit.Height,
                    orbit.Radius);
            }
            ApplyScreenY(plan.OrbitGroup);
            if (formalOrbitGroup == null || formalOrbitGroup.Length != freeLook.m_Orbits.Length)
            {
                formalOrbitGroup = new CinemachineFreeLook.Orbit[freeLook.m_Orbits.Length];
                Array.Copy(freeLook.m_Orbits, formalOrbitGroup, freeLook.m_Orbits.Length);
                formalScreenY = new float[plan.OrbitGroup.Count];
                for (int i = 0; i < formalScreenY.Length; i++)
                    formalScreenY[i] = plan.OrbitGroup[i].ScreenY;
            }
        }

        void ApplyScreenY(IReadOnlyList<CameraOrbitPayload> orbitGroup)
        {
            for (int i = 0; i < orbitGroup.Count; i++)
            {
                CinemachineVirtualCamera rig = freeLook.GetRig(i);
                CinemachineComposer composer = rig != null
                    ? rig.GetCinemachineComponent<CinemachineComposer>()
                    : null;
                if (composer == null)
                    throw new InvalidOperationException($"Camera Frame Plan orbit #{i} has no Cinemachine Composer.");
                composer.m_ScreenY = orbitGroup[i].ScreenY;
            }
        }

        void RestoreFormalOrbitGroup()
        {
            if (freeLook == null || freeLook.m_Orbits == null || formalOrbitGroup == null ||
                formalOrbitGroup.Length != freeLook.m_Orbits.Length || formalScreenY == null ||
                formalScreenY.Length != formalOrbitGroup.Length)
                return;
            Array.Copy(formalOrbitGroup, freeLook.m_Orbits, formalOrbitGroup.Length);
            ApplyFormalScreenY();
        }

        void ApplyFormalScreenY()
        {
            for (int i = 0; i < formalScreenY.Length; i++)
            {
                CinemachineVirtualCamera rig = freeLook.GetRig(i);
                CinemachineComposer composer = rig != null
                    ? rig.GetCinemachineComponent<CinemachineComposer>()
                    : null;
                if (composer == null)
                    throw new InvalidOperationException($"Formal camera orbit #{i} has no Cinemachine Composer.");
                composer.m_ScreenY = formalScreenY[i];
            }
        }

        void ApplyOrbit(float yaw, float pitch)
        {
            freeLook.m_XAxis.Value = ResolveAxisValue(
                yaw,
                freeLook.m_XAxis.m_MinValue,
                freeLook.m_XAxis.m_MaxValue,
                freeLook.m_XAxis.m_Wrap);
            freeLook.m_YAxis.Value = Mathf.InverseLerp(-70f, 70f, pitch);
            ClearFreeLookInput();
        }

        void RefreshBasisSnapshot()
        {
            RefreshBasisSnapshot(cameraAimTarget != null ? cameraAimTarget.position : Vector3.zero);
        }

        void RefreshBasisSnapshot(Vector3 aimPoint)
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
                Yaw,
                ResolvePitch(lookDirection),
                planarForward.sqrMagnitude > 0.000001f && planarRight.sqrMagnitude > 0.000001f);
            result = new CameraRigResult(
                basisSnapshot,
                state.FinalPosition,
                rotation,
                state.Lens.FieldOfView,
                basisSnapshot.Valid);
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
                result = default;
                return;
            }

            brain.ManualUpdate();
        }

        void ReportMissingFreeLook()
        {
            if (freeLook != null || missingFreeLookReported)
                return;

            missingFreeLookReported = true;
            Debug.LogError("CinemachineCameraRigAdapter requires an explicit CinemachineFreeLook.", this);
        }

        void ReportMissingTargets()
        {
            if (HasTargets() || missingTargetReported)
                return;

            missingTargetReported = true;
            Debug.LogError("CinemachineCameraRigAdapter requires explicit camera follow and aim targets.", this);
        }

        void ReportInvalidBrain()
        {
            if (HasValidBrain() || invalidBrainReported)
                return;

            invalidBrainReported = true;
            Debug.LogError("CinemachineCameraRigAdapter requires an explicit CinemachineBrain with Update Method set to Manual Update.", this);
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
