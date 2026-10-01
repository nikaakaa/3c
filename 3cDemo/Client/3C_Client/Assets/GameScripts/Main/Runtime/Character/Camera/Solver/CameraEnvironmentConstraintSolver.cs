using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class CameraEnvironmentConstraintSolver
    {
        readonly CharacterCameraProjectionPayload m_Projection;
        readonly ICameraEnvironmentQuery m_Query;
        Vector3 m_CurrentLocation;
        float m_CorrectionRatio;
        float m_CorrectionVelocity;
        bool m_HasLocation;

        public CameraEnvironmentConstraintSolver(
            CharacterCameraProjectionPayload projection,
            ICameraEnvironmentQuery query)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Query = query;
            if (projection.Collision.Enabled && query == null)
                throw new ArgumentNullException(nameof(query));
        }

        public void Reset()
        {
            m_CurrentLocation = default;
            m_CorrectionRatio = 0f;
            m_CorrectionVelocity = 0f;
            m_HasLocation = false;
        }

        public CameraFramePlan Apply(CameraFramePlan plan, in CameraFrameInput input)
        {
            if (!plan.Valid)
                return plan;
            CameraCollisionSettings settings = m_Projection.Collision;
            if (!settings.Enabled || plan.IgnoreCollision)
                return plan.WithCollision(CameraCollisionResult.NotEvaluated(plan.Location));

            float delta = input.Delta(settings.TimeDomain);
            Vector3 desired = plan.Location;
            Vector3 previous = m_HasLocation ? m_CurrentLocation : desired;
            var request = new CameraEnvironmentQueryRequest(
                previous,
                desired,
                plan.PivotLocation,
                settings.Radius + settings.NearClipPlane,
                settings.NearClipPlane,
                settings.LayerMask.value,
                settings.TriggerMode,
                delta,
                input.ResetHistory || !m_HasLocation);
            CameraEnvironmentQueryResult queryResult = m_Query.Resolve(in request);
            if (queryResult.Status == CameraCollisionStatus.NoLegalSpace)
            {
                m_HasLocation = false;
                return plan.WithCollision(new CameraCollisionResult(
                        CameraCollisionStatus.NoLegalSpace,
                        desired,
                        queryResult.SafeLocation,
                        queryResult.HitNormal,
                        Vector3.Distance(desired, queryResult.SafeLocation),
                        queryResult.ColliderInstanceId))
                    .WithValidity(false);
            }

            Vector3 constrained = queryResult.SafeLocation;
            Vector3 pivotToCamera = desired - plan.PivotLocation;
            float distance = pivotToCamera.magnitude;
            CameraCollisionStatus status = queryResult.Status;
            Vector3 normal = queryResult.HitNormal;
            int colliderInstanceId = queryResult.ColliderInstanceId;
            if (queryResult.Status == CameraCollisionStatus.Clear &&
                m_HasLocation && !input.ResetHistory && m_CorrectionRatio > 0f && settings.SmoothTime > 0f)
            {
                float correctionRatio = delta > 0f
                    ? Mathf.SmoothDamp(m_CorrectionRatio, 0f, ref m_CorrectionVelocity,
                        settings.SmoothTime, Mathf.Infinity, delta)
                    : m_CorrectionRatio;
                constrained = plan.PivotLocation + pivotToCamera * (1f - correctionRatio);
                var recoveryRequest = new CameraEnvironmentQueryRequest(
                    m_CurrentLocation,
                    constrained,
                    plan.PivotLocation,
                    settings.Radius + settings.NearClipPlane,
                    settings.NearClipPlane,
                    settings.LayerMask.value,
                    settings.TriggerMode,
                    delta,
                    false);
                CameraEnvironmentQueryResult recoveryResult = m_Query.Resolve(in recoveryRequest);
                if (recoveryResult.Status == CameraCollisionStatus.NoLegalSpace)
                {
                    m_HasLocation = false;
                    return plan.WithCollision(new CameraCollisionResult(
                            CameraCollisionStatus.NoLegalSpace,
                            desired,
                            recoveryResult.SafeLocation,
                            recoveryResult.HitNormal,
                            Vector3.Distance(desired, recoveryResult.SafeLocation),
                            recoveryResult.ColliderInstanceId))
                        .WithValidity(false);
                }
                constrained = recoveryResult.SafeLocation;
                status = recoveryResult.Status;
                normal = recoveryResult.HitNormal;
                colliderInstanceId = recoveryResult.ColliderInstanceId;
            }
            else
            {
                m_CorrectionVelocity = 0f;
            }

            m_CurrentLocation = constrained;
            m_CorrectionRatio = 1f - Vector3.Distance(plan.PivotLocation, constrained) / distance;
            m_HasLocation = true;
            CameraWorldBasicData constrainedData = plan.WorldBasicData.WithLocation(constrained);
            return plan
                .WithWorldBasicData(constrainedData)
                .WithCollision(new CameraCollisionResult(
                    status,
                    desired,
                    constrained,
                    normal,
                    Vector3.Distance(desired, constrained),
                    colliderInstanceId));
        }
    }
}
