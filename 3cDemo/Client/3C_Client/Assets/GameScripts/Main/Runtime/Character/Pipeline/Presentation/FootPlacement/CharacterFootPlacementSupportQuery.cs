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
            FootPlacementSurface surface,
            float soleDistance,
            float swingClearance,
            int queryCount,
            int candidateCount,
            int rejectedCount)
        {
            Surface = surface;
            SoleDistance = soleDistance;
            SwingClearance = swingClearance;
            QueryCount = queryCount;
            CandidateCount = candidateCount;
            RejectedCount = rejectedCount;
        }

        public FootPlacementSurface Surface { get; }
        public float SoleDistance { get; }
        public float SwingClearance { get; }
        public int QueryCount { get; }
        public int CandidateCount { get; }
        public int RejectedCount { get; }
        public bool HasSupport => Surface.IsValid;
    }

    internal sealed class CharacterFootPlacementSupportQuery
    {
        readonly PhysicsScene m_PhysicsScene;
        readonly CharacterFootPlacementRigBinding m_Rig;
        readonly FootPlacementTraceRuntimeSettings m_Settings;
        readonly RaycastHit[] m_Hits;
        readonly SupportCandidate[] m_Candidates;
        int m_CandidateCount;
        int m_RejectedCount;
        int m_QueryCount;
        float m_SwingClearance;

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
            Vector3 currentSole = (pose.HeelPosition + pose.ToePosition) * 0.5f;
            QueryPoint(pose.HeelPosition, 0f, pose.HipPosition, legLength, currentSole.y);
            QueryPoint(pose.ToePosition, 0f, pose.HipPosition, legLength, currentSole.y);
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
            int continuousCount = FilterHeightContinuity();
            if (continuousCount == 0)
                return new FootPlacementSupportResult(
                    default,
                    float.PositiveInfinity,
                    0f,
                    m_QueryCount,
                    0,
                    m_RejectedCount);

            SupportCandidate selected = m_Candidates[0];
            SupportCandidate current = default;
            bool hasCurrent = false;
            float envelopeClearance = 0f;
            for (int i = 0; i < continuousCount; i++)
            {
                SupportCandidate candidate = m_Candidates[i];
                float sampledSoleHeight = Mathf.Lerp(currentSole.y, predictedSole.y, candidate.PathFraction);
                envelopeClearance = Mathf.Max(envelopeClearance, candidate.Point.y - sampledSoleHeight);
                if (candidate.PathFraction <= 0.0001f &&
                    (!hasCurrent || candidate.Distance < current.Distance))
                {
                    current = candidate;
                    hasCurrent = true;
                }
                if (candidate.PathFraction > selected.PathFraction ||
                    Mathf.Approximately(candidate.PathFraction, selected.PathFraction) &&
                    candidate.Distance < selected.Distance)
                    selected = candidate;
            }
            float clearance = Mathf.Clamp(
                Mathf.Max(envelopeClearance, m_SwingClearance),
                0f,
                m_Settings.MaximumSwingClearance);
            return new FootPlacementSupportResult(
                new FootPlacementSurface(selected.Collider, selected.Point, selected.Normal),
                hasCurrent
                    ? Mathf.Abs(currentSole.y - current.Point.y)
                    : float.PositiveInfinity,
                clearance,
                m_QueryCount,
                continuousCount,
                m_RejectedCount);
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
                if (!Accept(hit, hip, legLength, currentSoleHeight))
                {
                    m_RejectedCount++;
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

        bool Accept(RaycastHit hit, Vector3 hip, float legLength, float currentSoleHeight)
        {
            if (!hit.collider || m_Rig.IsSelfCollider(hit.collider))
                return false;
            if (!IsFinite(hit.point) || !IsFinite(hit.normal) || hit.normal.sqrMagnitude <= 0.0001f)
                return false;
            if (Vector3.Angle(Vector3.up, hit.normal) > m_Settings.MaximumSlopeDegrees)
                return false;
            float heightDelta = hit.point.y - currentSoleHeight;
            if (heightDelta > m_Settings.MaximumStepUp || heightDelta < -m_Settings.MaximumStepDown)
                return false;
            return Vector3.Distance(hip, hit.point) <= legLength * 1.05f;
        }

        int FilterHeightContinuity()
        {
            if (m_CandidateCount <= 1)
                return m_CandidateCount;
            int write = 1;
            float previousHeight = m_Candidates[0].Point.y;
            for (int i = 1; i < m_CandidateCount; i++)
            {
                SupportCandidate candidate = m_Candidates[i];
                if (Mathf.Abs(candidate.Point.y - previousHeight) > m_Settings.MaximumHeightDiscontinuity)
                {
                    m_RejectedCount++;
                    continue;
                }
                m_Candidates[write++] = candidate;
                previousHeight = candidate.Point.y;
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
