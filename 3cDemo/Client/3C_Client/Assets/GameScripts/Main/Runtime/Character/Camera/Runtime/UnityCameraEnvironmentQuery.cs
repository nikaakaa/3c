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
            bool startOverlapped = HasOverlap(
                request.PreviousLocation,
                request.Radius,
                request.LayerMask,
                trigger);
            bool hasDesiredSegment = TryResolveSegment(
                request.PivotLocation,
                request.DesiredLocation,
                request.Radius,
                request.NearClipPlane,
                request.LayerMask,
                trigger,
                out Vector3 pivotSafe,
                out Vector3 pivotNormal,
                out float pivotDistance,
                out int pivotColliderInstanceId);
            if (!hasDesiredSegment)
            {
                if (HasOverlap(request.DesiredLocation, request.Radius, request.LayerMask, trigger))
                    return new CameraEnvironmentQueryResult(
                        CameraCollisionStatus.NoLegalSpace,
                        request.DesiredLocation,
                        Vector3.zero,
                        0f,
                        0);
                return CameraEnvironmentQueryResult.Clear(request.DesiredLocation);
            }

            Vector3 safeLocation = pivotSafe;
            Vector3 normal = pivotNormal;
            float distance = pivotDistance;
            int colliderInstanceId = pivotColliderInstanceId;
            if (!request.Reset && !startOverlapped && TryResolveSegment(
                    request.PreviousLocation,
                    request.DesiredLocation,
                    request.Radius,
                    0f,
                    request.LayerMask,
                    trigger,
                    out Vector3 movementSafe,
                    out Vector3 movementNormal,
                    out float movementDistance,
                    out int movementColliderInstanceId) &&
                Vector3.SqrMagnitude(movementSafe - request.PivotLocation) <
                Vector3.SqrMagnitude(safeLocation - request.PivotLocation))
            {
                safeLocation = movementSafe;
                normal = movementNormal;
                distance = movementDistance;
                colliderInstanceId = movementColliderInstanceId;
            }

            CameraCollisionStatus status = HasOverlap(
                safeLocation, request.Radius, request.LayerMask, trigger)
                ? CameraCollisionStatus.NoLegalSpace
                : startOverlapped
                    ? CameraCollisionStatus.StartOverlapped
                    : CameraCollisionStatus.Corrected;
            return new CameraEnvironmentQueryResult(
                status,
                safeLocation,
                normal,
                distance,
                colliderInstanceId);
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

        bool TryResolveSegment(
            Vector3 origin,
            Vector3 destination,
            float radius,
            float minimumDistance,
            int layerMask,
            QueryTriggerInteraction trigger,
            out Vector3 safeLocation,
            out Vector3 hitNormal,
            out float hitDistance,
            out int colliderInstanceId)
        {
            Vector3 delta = destination - origin;
            float length = delta.magnitude;
            if (length <= 0.000001f)
            {
                safeLocation = destination;
                hitNormal = Vector3.zero;
                hitDistance = 0f;
                colliderInstanceId = 0;
                return false;
            }
            Vector3 direction = delta / length;
            if (!TryGetNearestHit(origin, direction, length, radius, layerMask, trigger, out RaycastHit hit))
            {
                safeLocation = destination;
                hitNormal = Vector3.zero;
                hitDistance = 0f;
                colliderInstanceId = 0;
                return false;
            }
            float centerDistance = Mathf.Max(minimumDistance, hit.distance - ContactOffset);
            safeLocation = origin + direction * centerDistance;
            hitNormal = hit.normal;
            hitDistance = hit.distance;
            colliderInstanceId = hit.collider.GetInstanceID();
            return true;
        }

        bool TryGetNearestHit(
            Vector3 origin,
            Vector3 direction,
            float distance,
            float radius,
            int layerMask,
            QueryTriggerInteraction trigger,
            out RaycastHit nearest)
        {
            int count = m_PhysicsScene.SphereCast(
                origin,
                radius,
                direction,
                m_HitResults,
                distance,
                layerMask,
                trigger);
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
