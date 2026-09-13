using System;
using System.Globalization;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class UnityCameraEnvironmentQuery : ICameraEnvironmentQuery
    {
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
                out string pivotCollider);
            if (!hasDesiredSegment)
            {
                if (startOverlapped)
                    return new CameraEnvironmentQueryResult(
                        CameraCollisionStatus.NoLegalSpace,
                        request.PreviousLocation,
                        Vector3.zero,
                        0f,
                        string.Empty);
                return CameraEnvironmentQueryResult.Clear(request.DesiredLocation);
            }

            Vector3 safeLocation = pivotSafe;
            Vector3 normal = pivotNormal;
            float distance = pivotDistance;
            string colliderIdentity = pivotCollider;
            if (TryResolveMovementSweep(
                    request.PreviousLocation,
                    request.DesiredLocation,
                    request.Radius,
                    request.LayerMask,
                    trigger,
                    out Vector3 movementSafe,
                    out Vector3 movementNormal,
                    out float movementDistance,
                    out string movementCollider) &&
                Vector3.SqrMagnitude(movementSafe - request.PivotLocation) <
                Vector3.SqrMagnitude(safeLocation - request.PivotLocation))
            {
                safeLocation = movementSafe;
                normal = movementNormal;
                distance = movementDistance;
                colliderIdentity = movementCollider;
            }

            CameraCollisionStatus status = startOverlapped
                ? CameraCollisionStatus.StartOverlapped
                : CameraCollisionStatus.Corrected;
            return new CameraEnvironmentQueryResult(
                status,
                safeLocation,
                normal,
                distance,
                colliderIdentity);
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
                if (collider && !IsSelf(collider))
                    return true;
            }
            return false;
        }

        bool TryResolveSegment(
            Vector3 origin,
            Vector3 destination,
            float radius,
            float nearClipPlane,
            int layerMask,
            QueryTriggerInteraction trigger,
            out Vector3 safeLocation,
            out Vector3 hitNormal,
            out float hitDistance,
            out string colliderIdentity)
        {
            Vector3 delta = destination - origin;
            float length = delta.magnitude;
            if (length <= 0.000001f)
            {
                safeLocation = destination;
                hitNormal = Vector3.zero;
                hitDistance = 0f;
                colliderIdentity = string.Empty;
                return false;
            }
            Vector3 direction = delta / length;
            if (!TryGetNearestHit(origin, direction, length, radius, layerMask, trigger, out RaycastHit hit))
            {
                safeLocation = destination;
                hitNormal = Vector3.zero;
                hitDistance = 0f;
                colliderIdentity = string.Empty;
                return false;
            }
            float centerDistance = hit.distance - radius;
            if (centerDistance <= nearClipPlane)
            {
                safeLocation = origin + direction * nearClipPlane;
                hitNormal = hit.normal;
                hitDistance = Mathf.Max(0f, hit.distance);
                colliderIdentity = Identity(hit.collider);
                return true;
            }
            safeLocation = origin + direction * centerDistance;
            hitNormal = hit.normal;
            hitDistance = Mathf.Max(0f, hit.distance);
            colliderIdentity = Identity(hit.collider);
            return true;
        }

        bool TryResolveMovementSweep(
            Vector3 origin,
            Vector3 destination,
            float radius,
            int layerMask,
            QueryTriggerInteraction trigger,
            out Vector3 safeLocation,
            out Vector3 hitNormal,
            out float hitDistance,
            out string colliderIdentity)
        {
            Vector3 delta = destination - origin;
            float length = delta.magnitude;
            if (length <= 0.000001f)
            {
                safeLocation = destination;
                hitNormal = Vector3.zero;
                hitDistance = 0f;
                colliderIdentity = string.Empty;
                return false;
            }
            Vector3 direction = delta / length;
            if (!TryGetNearestHit(origin, direction, length, radius, layerMask, trigger, out RaycastHit hit))
            {
                safeLocation = destination;
                hitNormal = Vector3.zero;
                hitDistance = 0f;
                colliderIdentity = string.Empty;
                return false;
            }
            float centerDistance = Mathf.Max(0f, hit.distance - radius);
            safeLocation = origin + direction * centerDistance;
            hitNormal = hit.normal;
            hitDistance = Mathf.Max(0f, hit.distance);
            colliderIdentity = Identity(hit.collider);
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
                if (!hit.collider || IsSelf(hit.collider) || found && hit.distance >= nearest.distance)
                    continue;
                nearest = hit;
                found = true;
            }
            return found;
        }

        bool IsSelf(Collider collider) =>
            collider && (collider.transform == m_SelfRoot || collider.transform.IsChildOf(m_SelfRoot));

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

        static string Identity(Collider collider) =>
            collider
                ? collider.GetInstanceID().ToString(CultureInfo.InvariantCulture)
                : string.Empty;
    }
}
