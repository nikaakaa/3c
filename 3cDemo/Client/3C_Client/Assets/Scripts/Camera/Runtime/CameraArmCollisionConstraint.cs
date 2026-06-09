using Cinemachine;
using UnityEngine;

namespace ThirdPersonCamera
{
    [DisallowMultipleComponent]
    public sealed class CameraArmCollisionConstraint : CinemachineExtension
    {
        [SerializeField] Transform anchor;
        [SerializeField] LayerMask collisionMask = 513;
        [SerializeField, Min(0.01f)] float radius = 0.2f;
        [SerializeField, Min(0f)] float collisionSkin = 0.03f;
        [SerializeField, Range(1, 8)] int overlapResolveIterations = 5;
        [SerializeField, Min(0f)] float minDistance = 0.45f;
        [SerializeField, Min(0f)] float shrinkSmoothTime = 0.04f;
        [SerializeField, Min(0f)] float recoverSmoothTime = 0.18f;
        [SerializeField] Vector3 anchorOffset;
        [SerializeField] QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField] bool drawDebug;
        [SerializeField] bool debugLog = true;
        [SerializeField, Min(0f)] float debugLogInterval = 0.1f;
        [SerializeField] float debugCurrentDistance;
        [SerializeField] float debugTargetDistance;
        [SerializeField] bool debugHasHit;
        [SerializeField] string debugHitName;

        readonly RaycastHit[] sphereCastHits = new RaycastHit[32];
        readonly Collider[] overlapHits = new Collider[32];
        float currentDistance;
        float distanceVelocity;
        float nextDebugLogTime;
        bool hasCurrentDistance;
        bool wasConstrained;
        CameraArmCollisionState state;

        public Transform Anchor { get => anchor; set => anchor = value; }
        public LayerMask CollisionMask { get => collisionMask; set => collisionMask = value; }
        public float Radius { get => radius; set => radius = Mathf.Max(0.01f, value); }
        public float CollisionSkin { get => collisionSkin; set => collisionSkin = Mathf.Max(0f, value); }
        public float MinDistance { get => minDistance; set => minDistance = Mathf.Max(0f, value); }
        public float ShrinkSmoothTime { get => shrinkSmoothTime; set => shrinkSmoothTime = Mathf.Max(0f, value); }
        public float RecoverSmoothTime { get => recoverSmoothTime; set => recoverSmoothTime = Mathf.Max(0f, value); }
        public Vector3 AnchorOffset { get => anchorOffset; set => anchorOffset = value; }
        public bool DebugLog { get => debugLog; set => debugLog = value; }
        public CameraArmCollisionState State => state;

        void Reset()
        {
            anchor = ResolveDefaultAnchor();
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState cameraState,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize)
                return;

            Transform resolvedAnchor = anchor != null ? anchor : ResolveDefaultAnchor();
            if (resolvedAnchor == null)
                return;

            Vector3 anchorPosition = resolvedAnchor.TransformPoint(anchorOffset);
            Vector3 desiredPosition = cameraState.RawPosition;
            Vector3 toDesired = desiredPosition - anchorPosition;
            float desiredDistance = toDesired.magnitude;
            if (desiredDistance <= 0.0001f)
                return;

            Vector3 direction = toDesired / desiredDistance;
            float probeRadius = radius + collisionSkin;
            Transform ignoredRoot = ResolveIgnoredRoot();
            bool hasHit = TrySphereCast(anchorPosition, probeRadius, direction, desiredDistance, ignoredRoot, resolvedAnchor, out RaycastHit hit, out int ignoredHits);
            float hitDistance = hasHit ? hit.distance : desiredDistance;
            float targetDistance = CameraArmCollisionSolver.CalculateTargetDistance(desiredDistance, hasHit, hitDistance, minDistance);
            float beforeOverlapDistance = targetDistance;
            targetDistance = ResolveOverlappedDistance(anchorPosition, direction, targetDistance, minDistance, probeRadius, ignoredRoot, resolvedAnchor, out int ignoredOverlaps, out Collider overlapCollider);

            bool constrainedByHit = hasHit && hitDistance < desiredDistance - 0.001f;
            bool constrainedByOverlap = targetDistance < beforeOverlapDistance - 0.001f;
            bool isConstrained = constrainedByHit || constrainedByOverlap;
            bool recovering = wasConstrained && !isConstrained;

            if (!isConstrained && !recovering)
            {
                currentDistance = targetDistance;
                distanceVelocity = 0f;
                hasCurrentDistance = true;
                wasConstrained = false;
                WriteState(hasHit, currentDistance, targetDistance, anchorPosition, desiredPosition, desiredPosition, hasHit ? hit.collider : null);
                LogConstraint(vcam, desiredDistance, hitDistance, constrainedByHit, constrainedByOverlap, recovering, overlapCollider, ignoredRoot, ignoredHits, ignoredOverlaps, deltaTime);
                return;
            }

            if (!hasCurrentDistance || deltaTime < 0f)
            {
                currentDistance = targetDistance;
                distanceVelocity = 0f;
                hasCurrentDistance = true;
            }
            else
            {
                currentDistance = CameraArmCollisionSolver.SmoothDistance(currentDistance, targetDistance, shrinkSmoothTime, recoverSmoothTime, deltaTime, ref distanceVelocity);
            }

            Vector3 resolvedPosition = anchorPosition + direction * currentDistance;
            cameraState.RawPosition = resolvedPosition;
            wasConstrained = isConstrained || Mathf.Abs(currentDistance - targetDistance) > 0.001f;
            WriteState(hasHit, currentDistance, targetDistance, anchorPosition, desiredPosition, resolvedPosition, hasHit ? hit.collider : null);
            LogConstraint(vcam, desiredDistance, hitDistance, constrainedByHit, constrainedByOverlap, recovering, overlapCollider, ignoredRoot, ignoredHits, ignoredOverlaps, deltaTime);
            if (drawDebug)
                DrawDebug(state);
        }

        bool TrySphereCast(
            Vector3 anchorPosition,
            float probeRadius,
            Vector3 direction,
            float desiredDistance,
            Transform ignoredRoot,
            Transform resolvedAnchor,
            out RaycastHit nearestHit,
            out int ignoredHitCount)
        {
            nearestHit = default;
            ignoredHitCount = 0;
            int hitCount = Physics.SphereCastNonAlloc(anchorPosition, probeRadius, direction, sphereCastHits, desiredDistance, collisionMask, triggerInteraction);
            bool hasHit = false;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = sphereCastHits[i];
                if (candidate.collider == null)
                    continue;

                if (ShouldIgnoreCollider(candidate.collider, ignoredRoot, resolvedAnchor))
                {
                    ignoredHitCount++;
                    continue;
                }

                if (candidate.distance >= nearestDistance)
                    continue;

                nearestDistance = candidate.distance;
                nearestHit = candidate;
                hasHit = true;
            }

            return hasHit;
        }

        float ResolveOverlappedDistance(
            Vector3 anchorPosition,
            Vector3 direction,
            float targetDistance,
            float safeMinDistance,
            float probeRadius,
            Transform ignoredRoot,
            Transform resolvedAnchor,
            out int ignoredOverlapCount,
            out Collider overlapCollider)
        {
            ignoredOverlapCount = 0;
            overlapCollider = null;
            if (!OverlapsCollision(anchorPosition + direction * targetDistance, probeRadius, ignoredRoot, resolvedAnchor, ref ignoredOverlapCount, ref overlapCollider))
                return targetDistance;

            float safeDistance = Mathf.Max(0f, safeMinDistance);
            if (OverlapsCollision(anchorPosition + direction * safeDistance, probeRadius, ignoredRoot, resolvedAnchor, ref ignoredOverlapCount, ref overlapCollider))
                return safeDistance;

            float blockedDistance = targetDistance;
            int iterations = Mathf.Clamp(overlapResolveIterations, 1, 8);
            for (int i = 0; i < iterations; i++)
            {
                float testDistance = (safeDistance + blockedDistance) * 0.5f;
                if (OverlapsCollision(anchorPosition + direction * testDistance, probeRadius, ignoredRoot, resolvedAnchor, ref ignoredOverlapCount, ref overlapCollider))
                    blockedDistance = testDistance;
                else
                    safeDistance = testDistance;
            }

            return safeDistance;
        }

        bool OverlapsCollision(Vector3 position, float probeRadius, Transform ignoredRoot, Transform resolvedAnchor, ref int ignoredOverlapCount, ref Collider overlapCollider)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(position, probeRadius, overlapHits, collisionMask, triggerInteraction);
            for (int i = 0; i < hitCount; i++)
            {
                Collider candidate = overlapHits[i];
                if (candidate == null)
                    continue;

                if (ShouldIgnoreCollider(candidate, ignoredRoot, resolvedAnchor))
                {
                    ignoredOverlapCount++;
                    continue;
                }

                overlapCollider = candidate;
                return true;
            }

            return false;
        }

        bool ShouldIgnoreCollider(Collider candidate, Transform ignoredRoot, Transform resolvedAnchor)
        {
            Transform candidateTransform = candidate.transform;
            return IsRelated(candidateTransform, ignoredRoot)
                || IsRelated(candidateTransform, resolvedAnchor)
                || IsRelated(candidateTransform, transform.root);
        }

        Transform ResolveIgnoredRoot()
        {
            ThirdPersonCameraController controller = GetComponentInParent<ThirdPersonCameraController>(true);
            return controller != null ? controller.FollowAnchorSource : null;
        }

        Transform ResolveDefaultAnchor()
        {
            CinemachineVirtualCameraBase vcam = VirtualCamera;
            if (vcam != null && vcam.Follow != null)
                return vcam.Follow;
            if (vcam != null && vcam.LookAt != null)
                return vcam.LookAt;

            ThirdPersonCameraController controller = GetComponentInParent<ThirdPersonCameraController>(true);
            return controller != null ? controller.FollowAnchorSource : null;
        }

        void WriteState(bool hasHit, float current, float target, Vector3 anchorPosition, Vector3 desiredPosition, Vector3 resolvedPosition, Collider hitCollider)
        {
            state = new CameraArmCollisionState(hasHit, current, target, anchorPosition, desiredPosition, resolvedPosition, hitCollider);
            debugCurrentDistance = state.CurrentDistance;
            debugTargetDistance = state.TargetDistance;
            debugHasHit = state.HasHit;
            debugHitName = state.HitName;
        }

        void LogConstraint(
            CinemachineVirtualCameraBase vcam,
            float desiredDistance,
            float hitDistance,
            bool constrainedByHit,
            bool constrainedByOverlap,
            bool recovering,
            Collider overlapCollider,
            Transform ignoredRoot,
            int ignoredHits,
            int ignoredOverlaps,
            float deltaTime)
        {
            if (!ShouldLog())
                return;

            Debug.Log(
                $"[DEBUG-CAM-CHAIN] arm.constraint frame={Time.frameCount} vcam={TargetName(vcam != null ? vcam.transform : null)} " +
                $"reason={ConstraintReason(constrainedByHit, constrainedByOverlap, recovering)} desiredDistance={desiredDistance:F3} hitDistance={hitDistance:F3} " +
                $"targetDistance={state.TargetDistance:F3} currentDistance={state.CurrentDistance:F3} hit={state.HitName} overlap={(overlapCollider != null ? overlapCollider.name : string.Empty)} " +
                $"ignoredRoot={TargetName(ignoredRoot)} ignoredHits={ignoredHits} ignoredOverlaps={ignoredOverlaps} deltaTime={deltaTime:F4} " +
                $"mask={collisionMask.value} radius={radius:F3} skin={collisionSkin:F3} min={minDistance:F3}");
        }

        bool ShouldLog()
        {
            if (!debugLog)
                return false;

            if (debugLogInterval <= 0f)
                return true;

            float now = Time.unscaledTime;
            if (now < nextDebugLogTime)
                return false;

            nextDebugLogTime = now + debugLogInterval;
            return true;
        }

        static void DrawDebug(CameraArmCollisionState value)
        {
            Debug.DrawLine(value.AnchorPosition, value.DesiredPosition, Color.gray);
            Debug.DrawLine(value.AnchorPosition, value.ResolvedPosition, value.HasHit ? Color.red : Color.green);
        }

        static bool IsRelated(Transform candidate, Transform root)
        {
            if (candidate == null || root == null)
                return false;

            return candidate == root || candidate.IsChildOf(root) || root.IsChildOf(candidate);
        }

        static string ConstraintReason(bool constrainedByHit, bool constrainedByOverlap, bool recovering)
        {
            if (constrainedByHit && constrainedByOverlap)
                return "hit+overlap";
            if (constrainedByHit)
                return "hit";
            if (constrainedByOverlap)
                return "overlap";
            return recovering ? "recover" : "none";
        }

        static string TargetName(Transform target)
        {
            return target != null ? target.name : "null";
        }
    }
}
