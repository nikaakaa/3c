using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct FootPlacementSurface
    {
        public FootPlacementSurface(Collider collider, Vector3 point, Vector3 normal)
        {
            Collider = collider;
            Transform = collider ? collider.transform : null;
            Point = point;
            Normal = normal;
            LocalPoint = Transform ? Transform.InverseTransformPoint(point) : Vector3.zero;
            LocalNormal = Transform ? Transform.InverseTransformDirection(normal).normalized : Vector3.up;
            Identity = collider ? collider.GetInstanceID() : 0;
        }

        public Collider Collider { get; }
        public Transform Transform { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public Vector3 LocalPoint { get; }
        public Vector3 LocalNormal { get; }
        public int Identity { get; }
        public bool IsValid => Collider && Transform && Identity != 0;

        public FootPlacementSurface Rebuild()
        {
            return IsValid
                ? new FootPlacementSurface(
                    Collider,
                    Transform.TransformPoint(LocalPoint),
                    Transform.TransformDirection(LocalNormal).normalized)
                : default;
        }
    }

    internal readonly struct FootPlacementSupportResult
    {
        public FootPlacementSupportResult(
            FootPlacementSurface heelSupport,
            FootPlacementSurface toeSupport,
            FootPlacementSurface currentSupport,
            FootPlacementSurface futureLandingSupport,
            FootPlacementGroundEnvelope groundEnvelope,
            float soleDistance,
            float swingClearance,
            int queryCount,
            int candidateCount,
            int rejectedCount)
        {
            HeelSupport = heelSupport;
            ToeSupport = toeSupport;
            CurrentSupport = currentSupport;
            FutureLandingSupport = futureLandingSupport;
            GroundEnvelope = groundEnvelope;
            SoleDistance = soleDistance;
            SwingClearance = swingClearance;
            QueryCount = queryCount;
            CandidateCount = candidateCount;
            RejectedCount = rejectedCount;
        }

        public FootPlacementSurface HeelSupport { get; }
        public FootPlacementSurface ToeSupport { get; }
        public FootPlacementSurface CurrentSupport { get; }
        public FootPlacementSurface FutureLandingSupport { get; }
        public FootPlacementGroundEnvelope GroundEnvelope { get; }
        public FootPlacementSurface Surface => CurrentSupport;
        public float SoleDistance { get; }
        public float SwingClearance { get; }
        public int QueryCount { get; }
        public int CandidateCount { get; }
        public int RejectedCount { get; }
        public bool HasSupport => CurrentSupport.IsValid;
        public bool HasFutureLandingSupport => FutureLandingSupport.IsValid;
    }

    internal sealed class CharacterFootPlacementSupportQuery
    {
        readonly PhysicsScene m_PhysicsScene;
        readonly CharacterFootPlacementRigBinding m_Rig;
        readonly FootPlacementTraceRuntimeSettings m_Settings;
        readonly RaycastHit[] m_Hits;
        readonly SupportCandidate[] m_Candidates;
        readonly FootPlacementGroundEnvelopeSegment[] m_Segments;
        int m_CandidateCount;
        int m_RejectedCount;
        int m_QueryCount;
        float m_SwingClearance;
        FootPlacementGroundEnvelopeRejectReason m_EnvelopeRejectReason;

        public CharacterFootPlacementSupportQuery(
            PhysicsScene physicsScene,
            CharacterFootPlacementRigBinding rig,
            FootPlacementTraceRuntimeSettings settings)
        {
            if (!physicsScene.IsValid())
                throw new ArgumentException("Foot Placement requires a valid PhysicsScene.", nameof(physicsScene));
            m_PhysicsScene = physicsScene;
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_Settings = settings;
            m_Hits = new RaycastHit[settings.HitCapacity];
            m_Candidates = new SupportCandidate[settings.CandidateCapacity];
            m_Segments = new FootPlacementGroundEnvelopeSegment[settings.CandidateCapacity];
        }

        public FootPlacementSupportResult Query(
            CharacterFootPlacementAnimatedFootPose pose,
            Vector3 predictedSole,
            float legLength)
        {
            m_CandidateCount = 0;
            m_RejectedCount = 0;
            m_QueryCount = 0;
            m_SwingClearance = 0f;
            m_EnvelopeRejectReason = FootPlacementGroundEnvelopeRejectReason.None;
            Vector3 currentSole = (pose.HeelPosition + pose.ToePosition) * 0.5f;
            FootPlacementSurface heelSupport = QueryCurrentSupport(pose.HeelPosition, pose.HipPosition, legLength, currentSole.y, out float heelDistance);
            FootPlacementSurface toeSupport = QueryCurrentSupport(pose.ToePosition, pose.HipPosition, legLength, currentSole.y, out float toeDistance);
            FootPlacementSurface currentSupport = BuildCurrentSupport(pose, heelSupport, toeSupport);
            float soleDistance = ResolveSoleDistance(heelDistance, toeDistance);
            QueryPoint(currentSole, 0f, pose.HipPosition, legLength, currentSole.y);
            int pathSamples = m_Settings.PathSampleCount;
            for (int i = 1; i <= pathSamples; i++)
            {
                float fraction = i / (float)(pathSamples + 1);
                QueryPoint(
                    Vector3.Lerp(currentSole, predictedSole, fraction),
                    fraction,
                    pose.HipPosition,
                    legLength,
                    currentSole.y);
            }
            QueryPoint(predictedSole, 1f, pose.HipPosition, legLength, currentSole.y);
            QueryCapsulePath(currentSole, predictedSole);
            SortCandidates();
            CollapseSampleCandidates();
            int continuousCount = FilterEnvelopeContinuity();
            if (continuousCount == 0)
                return new FootPlacementSupportResult(
                    heelSupport,
                    toeSupport,
                    currentSupport,
                    default,
                    new FootPlacementGroundEnvelope(
                        m_Segments,
                        0,
                        m_EnvelopeRejectReason == FootPlacementGroundEnvelopeRejectReason.None
                            ? FootPlacementGroundEnvelopeRejectReason.NoCandidate
                            : m_EnvelopeRejectReason),
                    soleDistance,
                    0f,
                    m_QueryCount,
                    0,
                    m_RejectedCount);

            SupportCandidate future = default;
            bool hasFuture = false;
            float envelopeClearance = 0f;
            for (int i = 0; i < continuousCount; i++)
            {
                SupportCandidate candidate = m_Candidates[i];
                float sampledSoleHeight = Mathf.Lerp(currentSole.y, predictedSole.y, candidate.PathFraction);
                envelopeClearance = Mathf.Max(envelopeClearance, candidate.Point.y - sampledSoleHeight);
                if (candidate.PathFraction >= 0.9999f &&
                    (!hasFuture || candidate.Distance < future.Distance))
                {
                    future = candidate;
                    hasFuture = true;
                }
            }
            int segmentCount = BuildEnvelopeSegments(continuousCount);
            float clearance = Mathf.Clamp(
                Mathf.Max(envelopeClearance, m_SwingClearance),
                0f,
                m_Settings.MaximumSwingClearance);
            return new FootPlacementSupportResult(
                heelSupport,
                toeSupport,
                currentSupport,
                hasFuture
                    ? new FootPlacementSurface(future.Collider, future.Point, future.Normal)
                    : default,
                new FootPlacementGroundEnvelope(
                    m_Segments,
                    segmentCount,
                    segmentCount > 0
                        ? m_EnvelopeRejectReason
                        : FootPlacementGroundEnvelopeRejectReason.SurfaceDiscontinuity),
                soleDistance,
                clearance,
                m_QueryCount,
                continuousCount,
                m_RejectedCount);
        }

        FootPlacementSurface QueryCurrentSupport(
            Vector3 solePoint,
            Vector3 hip,
            float legLength,
            float currentSoleHeight,
            out float soleDistance)
        {
            Vector3 origin = solePoint + Vector3.up * m_Settings.CastAbove;
            float castDistance = m_Settings.CastAbove + m_Settings.CastBelow;
            FootPlacementSurface selected = default;
            soleDistance = float.PositiveInfinity;

            m_QueryCount++;
            int rayCount = m_PhysicsScene.Raycast(
                origin,
                Vector3.down,
                m_Hits,
                castDistance,
                m_Settings.GroundLayerMask,
                QueryTriggerInteraction.Ignore);
            SelectCurrentSupport(
                rayCount,
                solePoint,
                hip,
                legLength,
                currentSoleHeight,
                ref selected,
                ref soleDistance);

            m_QueryCount++;
            int sphereCount = m_PhysicsScene.SphereCast(
                origin,
                m_Settings.SphereRadius,
                Vector3.down,
                m_Hits,
                castDistance,
                m_Settings.GroundLayerMask,
                QueryTriggerInteraction.Ignore);
            SelectCurrentSupport(
                sphereCount,
                solePoint,
                hip,
                legLength,
                currentSoleHeight,
                ref selected,
                ref soleDistance);
            return selected;
        }

        void SelectCurrentSupport(
            int hitCount,
            Vector3 solePoint,
            Vector3 hip,
            float legLength,
            float currentSoleHeight,
            ref FootPlacementSurface selected,
            ref float soleDistance)
        {
            int count = Mathf.Min(hitCount, m_Hits.Length);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = m_Hits[i];
                if (!Accept(hit, hip, legLength, currentSoleHeight, out FootPlacementGroundEnvelopeRejectReason rejectReason))
                {
                    m_RejectedCount++;
                    RecordRejectReason(rejectReason);
                    continue;
                }

                float candidateDistance = Mathf.Abs(solePoint.y - hit.point.y);
                int candidateIdentity = hit.collider.GetInstanceID();
                if (candidateDistance > soleDistance ||
                    Mathf.Approximately(candidateDistance, soleDistance) &&
                    selected.IsValid &&
                    candidateIdentity >= selected.Identity)
                    continue;

                selected = new FootPlacementSurface(hit.collider, hit.point, hit.normal.normalized);
                soleDistance = candidateDistance;
            }
        }

        static FootPlacementSurface BuildCurrentSupport(
            CharacterFootPlacementAnimatedFootPose pose,
            FootPlacementSurface heelSupport,
            FootPlacementSurface toeSupport)
        {
            if (!heelSupport.IsValid && !toeSupport.IsValid)
                return default;
            Vector3 sole = (pose.HeelPosition + pose.ToePosition) * 0.5f;
            if (!heelSupport.IsValid || !toeSupport.IsValid)
            {
                FootPlacementSurface support = heelSupport.IsValid ? heelSupport : toeSupport;
                Vector3 probe = heelSupport.IsValid ? pose.HeelPosition : pose.ToePosition;
                Vector3 projectedCenter = support.Point + Vector3.ProjectOnPlane(sole - probe, support.Normal);
                return new FootPlacementSurface(support.Collider, projectedCenter, support.Normal);
            }

            FootPlacementSurface owner;
            if (heelSupport.Point.y > toeSupport.Point.y + 0.0001f)
                owner = heelSupport;
            else if (toeSupport.Point.y > heelSupport.Point.y + 0.0001f)
                owner = toeSupport;
            else
                owner = heelSupport.Identity <= toeSupport.Identity ? heelSupport : toeSupport;

            Vector3 normal = (heelSupport.Normal + toeSupport.Normal).normalized;
            Vector3 soleTangent = toeSupport.Point - heelSupport.Point;
            if (soleTangent.sqrMagnitude > 0.000001f)
            {
                Vector3 fittedNormal = Vector3.ProjectOnPlane(normal, soleTangent.normalized).normalized;
                if (fittedNormal.sqrMagnitude > 0.000001f)
                    normal = fittedNormal.y >= 0f ? fittedNormal : -fittedNormal;
            }
            if (normal.sqrMagnitude <= 0.000001f)
                normal = Vector3.up;
            return new FootPlacementSurface(
                owner.Collider,
                (heelSupport.Point + toeSupport.Point) * 0.5f,
                normal);
        }

        static float ResolveSoleDistance(float heelDistance, float toeDistance)
        {
            if (!IsFinite(heelDistance))
                return toeDistance;
            if (!IsFinite(toeDistance))
                return heelDistance;
            return Mathf.Min(heelDistance, toeDistance);
        }

        int BuildEnvelopeSegments(int candidateCount)
        {
            int count = 0;
            for (int i = 0; i < candidateCount; i++)
            {
                SupportCandidate current = m_Candidates[i];
                float start = i == 0
                    ? 0f
                    : (m_Candidates[i - 1].PathFraction + current.PathFraction) * 0.5f;
                float end = i == candidateCount - 1
                    ? 1f
                    : (current.PathFraction + m_Candidates[i + 1].PathFraction) * 0.5f;
                Vector3 edgeStart = i > 0
                    ? Vector3.Lerp(m_Candidates[i - 1].Point, current.Point, 0.5f)
                    : current.Point;
                Vector3 edgeEnd = i + 1 < candidateCount
                    ? Vector3.Lerp(current.Point, m_Candidates[i + 1].Point, 0.5f)
                    : current.Point;
                m_Segments[count++] = new FootPlacementGroundEnvelopeSegment(
                    start,
                    end,
                    new FootPlacementSurface(current.Collider, current.Point, current.Normal),
                    edgeStart,
                    edgeEnd,
                    Mathf.Max(current.Point.y, edgeStart.y, edgeEnd.y),
                    end - start > 0.0001f);
            }
            return count;
        }

        void QueryPoint(
            Vector3 point,
            float pathFraction,
            Vector3 hip,
            float legLength,
            float currentSoleHeight)
        {
            Vector3 origin = point + Vector3.up * m_Settings.CastAbove;
            float distance = m_Settings.CastAbove + m_Settings.CastBelow;
            m_QueryCount++;
            int rayCount = m_PhysicsScene.Raycast(
                origin,
                Vector3.down,
                m_Hits,
                distance,
                m_Settings.GroundLayerMask,
                QueryTriggerInteraction.Ignore);
            CollectHits(rayCount, pathFraction, hip, legLength, currentSoleHeight);
            m_QueryCount++;
            int sphereCount = m_PhysicsScene.SphereCast(
                origin,
                m_Settings.SphereRadius,
                Vector3.down,
                m_Hits,
                distance,
                m_Settings.GroundLayerMask,
                QueryTriggerInteraction.Ignore);
            CollectHits(sphereCount, pathFraction, hip, legLength, currentSoleHeight);
        }

        void QueryCapsulePath(
            Vector3 start,
            Vector3 end)
        {
            Vector3 delta = end - start;
            float distance = delta.magnitude;
            if (distance <= 0.0001f)
                return;
            Vector3 origin = start + Vector3.up * (m_Settings.CastAbove * 0.5f);
            m_QueryCount++;
            int count = m_PhysicsScene.CapsuleCast(
                origin,
                origin + Vector3.up * (m_Settings.CapsuleRadius * 2f),
                m_Settings.CapsuleRadius,
                delta / distance,
                m_Hits,
                distance,
                m_Settings.GroundLayerMask,
                QueryTriggerInteraction.Ignore);
            int hitCount = Mathf.Min(count, m_Hits.Length);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = m_Hits[i];
                if (!hit.collider || m_Rig.IsSelfCollider(hit.collider) || !IsFinite(hit.point))
                {
                    m_RejectedCount++;
                    continue;
                }
                float fraction = Mathf.Clamp01(hit.distance / distance);
                float sampledSoleHeight = Mathf.Lerp(start.y, end.y, fraction);
                float clearance = hit.point.y + m_Settings.CapsuleRadius - sampledSoleHeight;
                m_SwingClearance = Mathf.Max(m_SwingClearance, clearance);
            }
        }

        void CollectHits(
            int hitCount,
            float pathFraction,
            Vector3 hip,
            float legLength,
            float currentSoleHeight)
        {
            int count = Mathf.Min(hitCount, m_Hits.Length);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = m_Hits[i];
                if (!Accept(hit, hip, legLength, currentSoleHeight, out FootPlacementGroundEnvelopeRejectReason rejectReason))
                {
                    m_RejectedCount++;
                    RecordRejectReason(rejectReason);
                    continue;
                }
                if (m_CandidateCount >= m_Candidates.Length)
                {
                    m_RejectedCount++;
                    continue;
                }
                m_Candidates[m_CandidateCount++] = new SupportCandidate(
                    hit.collider,
                    hit.point,
                    hit.normal.normalized,
                    Mathf.Clamp01(pathFraction),
                    Mathf.Max(0f, hit.distance));
            }
        }

        bool Accept(
            RaycastHit hit,
            Vector3 hip,
            float legLength,
            float currentSoleHeight,
            out FootPlacementGroundEnvelopeRejectReason rejectReason)
        {
            if (!hit.collider || m_Rig.IsSelfCollider(hit.collider))
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.InvalidCandidate;
                return false;
            }
            if (!IsFinite(hit.point) || !IsFinite(hit.normal) || hit.normal.sqrMagnitude <= 0.0001f)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.InvalidCandidate;
                return false;
            }
            if (Vector3.Angle(Vector3.up, hit.normal) > m_Settings.MaximumSlopeDegrees)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.SlopeExceeded;
                return false;
            }
            float heightDelta = hit.point.y - currentSoleHeight;
            if (heightDelta > m_Settings.MaximumStepUp || heightDelta < -m_Settings.MaximumStepDown)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.StepExceeded;
                return false;
            }
            if (Vector3.Distance(hip, hit.point) > legLength * 1.05f)
            {
                rejectReason = FootPlacementGroundEnvelopeRejectReason.ReachExceeded;
                return false;
            }
            rejectReason = FootPlacementGroundEnvelopeRejectReason.None;
            return true;
        }

        void RecordRejectReason(FootPlacementGroundEnvelopeRejectReason reason)
        {
            if (reason != FootPlacementGroundEnvelopeRejectReason.None &&
                m_EnvelopeRejectReason == FootPlacementGroundEnvelopeRejectReason.None)
                m_EnvelopeRejectReason = reason;
        }

        int FilterEnvelopeContinuity()
        {
            if (m_CandidateCount == 0)
            {
                m_EnvelopeRejectReason = FootPlacementGroundEnvelopeRejectReason.NoCandidate;
                return 0;
            }
            if (m_CandidateCount == 1)
                return m_CandidateCount;
            int write = 1;
            SupportCandidate previous = m_Candidates[0];
            for (int i = 1; i < m_CandidateCount; i++)
            {
                SupportCandidate candidate = m_Candidates[i];
                float heightDelta = candidate.Point.y - previous.Point.y;
                if (Mathf.Abs(heightDelta) > m_Settings.MaximumHeightDiscontinuity ||
                    heightDelta > m_Settings.MaximumStepUp ||
                    heightDelta < -m_Settings.MaximumStepDown)
                {
                    m_EnvelopeRejectReason = FootPlacementGroundEnvelopeRejectReason.HeightDiscontinuity;
                    m_RejectedCount += m_CandidateCount - i;
                    break;
                }
                Vector2 previousPlanar = new Vector2(previous.Point.x, previous.Point.z);
                Vector2 candidatePlanar = new Vector2(candidate.Point.x, candidate.Point.z);
                if (Vector2.Distance(previousPlanar, candidatePlanar) > m_Settings.MaximumEdgeGap)
                {
                    m_EnvelopeRejectReason = FootPlacementGroundEnvelopeRejectReason.EdgeGap;
                    m_RejectedCount += m_CandidateCount - i;
                    break;
                }
                m_Candidates[write++] = candidate;
                previous = candidate;
            }
            return write;
        }

        void CollapseSampleCandidates()
        {
            if (m_CandidateCount <= 1)
                return;
            int write = 0;
            int read = 0;
            while (read < m_CandidateCount)
            {
                SupportCandidate selected = m_Candidates[read++];
                while (read < m_CandidateCount &&
                       Mathf.Approximately(m_Candidates[read].PathFraction, selected.PathFraction))
                    read++;
                m_Candidates[write++] = selected;
            }
            m_CandidateCount = write;
        }

        void SortCandidates()
        {
            for (int i = 1; i < m_CandidateCount; i++)
            {
                SupportCandidate value = m_Candidates[i];
                int j = i - 1;
                while (j >= 0 && Compare(value, m_Candidates[j]) < 0)
                {
                    m_Candidates[j + 1] = m_Candidates[j];
                    j--;
                }
                m_Candidates[j + 1] = value;
            }
        }

        static int Compare(SupportCandidate left, SupportCandidate right)
        {
            int path = left.PathFraction.CompareTo(right.PathFraction);
            if (path != 0)
                return path;
            int distance = left.Distance.CompareTo(right.Distance);
            return distance != 0 ? distance : left.SurfaceIdentity.CompareTo(right.SurfaceIdentity);
        }

        static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        readonly struct SupportCandidate
        {
            public SupportCandidate(Collider collider, Vector3 point, Vector3 normal, float pathFraction, float distance)
            {
                Collider = collider;
                Point = point;
                Normal = normal;
                PathFraction = pathFraction;
                Distance = distance;
                SurfaceIdentity = collider ? collider.GetInstanceID() : 0;
            }

            public Collider Collider { get; }
            public Vector3 Point { get; }
            public Vector3 Normal { get; }
            public float PathFraction { get; }
            public float Distance { get; }
            public int SurfaceIdentity { get; }
        }
    }
}
