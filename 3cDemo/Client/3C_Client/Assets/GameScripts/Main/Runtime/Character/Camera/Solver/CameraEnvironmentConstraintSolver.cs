using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class CameraEnvironmentConstraintSolver
    {
        const float PlaneTolerance = 0.005f;
        const float DampingEpsilon = 0.0001f;
        const float LogNegligibleResidual = -4.605170186f;

        readonly CharacterCameraProjectionPayload m_Projection;
        readonly ICameraEnvironmentQuery m_Query;
        Vector3 m_PreviousOcclusionPoint;
        Vector3 m_PreviousOcclusionNormal;
        float m_PreviousDistance = -1f;
        float m_CorrectionDistance;

        public CameraEnvironmentConstraintSolver(
            CharacterCameraProjectionPayload projection,
            ICameraEnvironmentQuery query)
        {
            m_Projection = projection;
            m_Query = query;
        }

        public void Reset()
        {
            m_PreviousOcclusionPoint = default;
            m_PreviousOcclusionNormal = default;
            m_PreviousDistance = -1f;
            m_CorrectionDistance = 0f;
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
            float halfHeight = Mathf.Tan(plan.FieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfWidth = halfHeight * input.PixelWidth / input.PixelHeight;
            float protectionRadius = plan.NearClipPlane * Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight);
            float centerDistance = plan.NearClipPlane;
            if (plan.NearClipPlane > 0f && protectionRadius <= plan.NearClipPlane)
            {
                protectionRadius = (plan.NearClipPlane * plan.NearClipPlane + protectionRadius * protectionRadius) /
                    (2f * plan.NearClipPlane);
                centerDistance = protectionRadius;
            }
            Vector3 protectionCenterOffset = plan.Rotation * Vector3.forward * centerDistance;
            var request = new CameraEnvironmentQueryRequest(
                desired,
                plan.PivotLocation,
                protectionCenterOffset,
                protectionRadius,
                settings.MinimumDistance,
                settings.DistanceLimit,
                settings.CameraRadius,
                settings.LayerMask.value,
                settings.TriggerMode);
            CameraEnvironmentQueryResult queryResult = m_Query.Resolve(in request);
            Vector3 occlusionPoint = queryResult.OcclusionPoint;
            Vector3 occlusionNormal = queryResult.OcclusionNormal;
            float safeDistance = queryResult.SafeDistance;
            if (queryResult.Status != CameraCollisionStatus.NoLegalSpace)
            {
                if (input.ResetHistory || m_PreviousDistance < 0f)
                {
                    m_CorrectionDistance = 0f;
                }
                else if (delta > 0f)
                {
                    bool hasPlane = occlusionNormal != Vector3.zero;
                    bool hadPlane = m_PreviousOcclusionNormal != Vector3.zero;
                    bool samePlane = hasPlane && hadPlane && IsSamePlane(occlusionPoint, occlusionNormal);
                    if (hasPlane && !hadPlane || samePlane && safeDistance < m_PreviousDistance)
                        m_CorrectionDistance = 0f;
                    else if (hadPlane && !samePlane)
                        m_CorrectionDistance = Mathf.Max(0f, safeDistance - m_PreviousDistance);
                    m_CorrectionDistance = RemainingCorrection(m_CorrectionDistance, settings.Damping, delta);
                }
                float recoveryDistance = Mathf.Max(settings.MinimumDistance, safeDistance - m_CorrectionDistance);
                if (recoveryDistance < safeDistance)
                {
                    Vector3 recoveryLocation = plan.PivotLocation +
                        (queryResult.SafeLocation - plan.PivotLocation) * (recoveryDistance / safeDistance);
                    var recoveryRequest = new CameraEnvironmentQueryRequest(
                        recoveryLocation,
                        plan.PivotLocation,
                        protectionCenterOffset,
                        protectionRadius,
                        settings.MinimumDistance,
                        settings.DistanceLimit,
                        settings.CameraRadius,
                        settings.LayerMask.value,
                        settings.TriggerMode);
                    queryResult = m_Query.Resolve(in recoveryRequest);
                }
            }

            Vector3 constrained = queryResult.SafeLocation;
            var collision = new CameraCollisionResult(queryResult.Status,
                desired, constrained, queryResult.HitNormal,
                Vector3.Distance(desired, constrained), queryResult.ColliderInstanceId);
            if (queryResult.Status == CameraCollisionStatus.NoLegalSpace)
            {
                Reset();
                return plan.WithCollision(collision).WithValidity(false);
            }

            m_PreviousDistance = queryResult.SafeDistance;
            m_CorrectionDistance = safeDistance - m_PreviousDistance;
            m_PreviousOcclusionPoint = occlusionPoint;
            m_PreviousOcclusionNormal = occlusionNormal;
            CameraWorldBasicData constrainedData = plan.WorldBasicData.WithLocation(constrained);
            return plan
                .WithWorldBasicData(constrainedData)
                .WithCollision(collision);
        }

        bool IsSamePlane(Vector3 point, Vector3 normal)
        {
            Vector3 difference = normal - m_PreviousOcclusionNormal;
            return Mathf.Abs(difference.x) < PlaneTolerance &&
                Mathf.Abs(difference.y) < PlaneTolerance &&
                Mathf.Abs(difference.z) < PlaneTolerance &&
                Mathf.Abs(Vector3.Dot(point, normal) -
                    Vector3.Dot(m_PreviousOcclusionPoint, m_PreviousOcclusionNormal)) < PlaneTolerance;
        }

        static float RemainingCorrection(float correction, float damping, float delta)
        {
            if (damping < DampingEpsilon || correction < DampingEpsilon)
                return 0f;
            if (delta < DampingEpsilon)
                return correction;
            float decay = Mathf.Exp(LogNegligibleResidual / damping * delta);
            return correction - correction * (1f - decay);
        }
    }
}
