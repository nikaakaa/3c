using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class UnityCameraEnvironmentQuery : ICameraEnvironmentQuery
    {
        const float ContactOffset = 0.0001f;

        readonly PhysicsScene m_PhysicsScene;
        readonly Transform m_SelfRoot;
        readonly Collider[] m_OverlapResults;
        readonly RaycastHit[] m_HitResults;

        public UnityCameraEnvironmentQuery(
            PhysicsScene physicsScene,
            Transform selfRoot,
            int hitCapacity = 32)
        {
            if (!physicsScene.IsValid())
                throw new ArgumentException("Camera collision requires a valid PhysicsScene.", nameof(physicsScene));
            if (!selfRoot)
                throw new ArgumentNullException(nameof(selfRoot));
            if (hitCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(hitCapacity));
            m_PhysicsScene = physicsScene;
            m_SelfRoot = selfRoot;
            m_OverlapResults = new Collider[hitCapacity];
            m_HitResults = new RaycastHit[hitCapacity];
        }

        public CameraEnvironmentQueryResult Resolve(in CameraEnvironmentQueryRequest request)
        {
            QueryTriggerInteraction trigger = ToQueryTrigger(request.TriggerMode);
            Vector3 pivotToCamera = request.DesiredLocation - request.PivotLocation;
            float desiredDistance = pivotToCamera.magnitude;
            Vector3 direction = pivotToCamera / desiredDistance;
            float distance = desiredDistance;
            RaycastHit occlusionHit = default;
            Vector3 hitNormal = Vector3.zero;
            int colliderInstanceId = 0;
            if (distance < request.MinimumDistance + ContactOffset)
            {
                distance = request.MinimumDistance;
            }
            else
            {
                float queryDistance = request.DistanceLimit > ContactOffset
                    ? Mathf.Min(request.DistanceLimit, distance)
                    : distance;
                Vector3 origin = request.DesiredLocation - direction * queryDistance;
                int count = m_PhysicsScene.Raycast(origin, direction, m_HitResults,
                    queryDistance + request.CameraRadius, request.LayerMask, trigger);
                if (TrySelectNearestHit(count, out occlusionHit))
                {
                    distance = Mathf.Clamp(distance - queryDistance + occlusionHit.distance - ContactOffset,
                        request.MinimumDistance, desiredDistance);
                    hitNormal = occlusionHit.normal;
                    colliderInstanceId = occlusionHit.collider.GetInstanceID();
                }
            }

            Vector3 safeLocation = request.PivotLocation + direction * distance;
            Vector3 protectionCenter = safeLocation + request.ProtectionCenterOffset;
            bool safe = !HasOverlap(protectionCenter, request.ProtectionRadius + ContactOffset,
                request.LayerMask, trigger);
            if (!safe)
            {
                float queryDistance = request.DistanceLimit > ContactOffset
                    ? Mathf.Min(request.DistanceLimit, distance)
                    : distance;
                float sweepDistance = queryDistance - request.ProtectionRadius;
                if (sweepDistance > 0f)
                {
                    Vector3 origin = protectionCenter - direction * sweepDistance;
                    int count = m_PhysicsScene.SphereCast(origin,
                        request.ProtectionRadius + ContactOffset, direction, m_HitResults,
                        sweepDistance + request.CameraRadius, request.LayerMask, trigger);
                    if (TrySelectNearestHit(count, out RaycastHit hit))
                    {
                        distance = Mathf.Clamp(request.ProtectionRadius + hit.distance,
                            request.MinimumDistance, distance);
                        safeLocation = request.PivotLocation + direction * distance;
                        hitNormal = hit.normal;
                        colliderInstanceId = hit.collider.GetInstanceID();
                    }
                }
                safe = !HasOverlap(safeLocation + request.ProtectionCenterOffset,
                    request.ProtectionRadius, request.LayerMask, trigger);
            }
            CameraCollisionStatus status = !safe
                ? CameraCollisionStatus.NoLegalSpace
                : distance == desiredDistance ? CameraCollisionStatus.Clear : CameraCollisionStatus.Corrected;
            return new CameraEnvironmentQueryResult(
                status,
                safeLocation,
                distance,
                hitNormal,
                colliderInstanceId,
                occlusionHit.point,
                occlusionHit.normal);
        }

        bool HasOverlap(
            Vector3 location,
            float radius,
            int layerMask,
            QueryTriggerInteraction trigger)
        {
            int count = m_PhysicsScene.OverlapSphere(
                location,
                radius,
                m_OverlapResults,
                layerMask,
                trigger);
            for (int i = 0; i < count; i++)
            {
                Collider collider = m_OverlapResults[i];
                if (!IsSelf(collider))
                    return true;
            }
            return false;
        }

        bool TrySelectNearestHit(int count, out RaycastHit nearest)
        {
            nearest = default;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = m_HitResults[i];
                if (IsSelf(hit.collider) || found && hit.distance >= nearest.distance)
                    continue;
                nearest = hit;
                found = true;
            }
            return found;
        }

        bool IsSelf(Collider collider) =>
            collider.transform == m_SelfRoot || collider.transform.IsChildOf(m_SelfRoot);

        static QueryTriggerInteraction ToQueryTrigger(CameraCollisionTriggerMode mode)
        {
            switch (mode)
            {
                case CameraCollisionTriggerMode.Ignore:
                    return QueryTriggerInteraction.Ignore;
                case CameraCollisionTriggerMode.Collide:
                    return QueryTriggerInteraction.Collide;
                default:
                    return QueryTriggerInteraction.UseGlobal;
            }
        }
    }
}
