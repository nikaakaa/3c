using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    internal sealed partial class DeterministicCapsuleQueries
    {
        readonly DeterministicCollisionWorldArtifact m_World;
        readonly DeterministicKccConfiguration m_Configuration;
        readonly DeterministicCollisionWorldIndex m_Index;
        readonly int[] m_Candidates;
        readonly DeterministicKccContact[] m_OverlapContacts;
        readonly DeterministicKccContact[] m_CastContacts;
        readonly DeterministicKccContact[] m_AllCastContacts;

        int m_OverlapContactCount;
        int m_CastContactCount;
        int m_AllCastContactCount;
        int m_RayHitCount;
        DeterministicKccRayHit m_ClosestRayHit;
        bool m_HasClosestRayHit;

        public DeterministicCapsuleQueries(
            DeterministicCollisionWorldArtifact world,
            DeterministicKccConfiguration configuration)
        {
            m_World = world ?? throw new ArgumentNullException(nameof(world));
            m_Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            m_Index = new DeterministicCollisionWorldIndex(world);
            m_Candidates = new int[configuration.MaximumCandidates];
            m_OverlapContacts = new DeterministicKccContact[configuration.MaximumContacts];
            m_CastContacts = new DeterministicKccContact[configuration.MaximumContacts];
            m_AllCastContacts = new DeterministicKccContact[configuration.MaximumContacts];
        }

        public int Overlap(FixedVector3 position, out DeterministicKccQuerySummary summary)
        {
            m_OverlapContactCount = 0;
            int candidateCount = m_Index.Query(CapsuleBounds(position, m_Configuration.CollisionOffset), m_Candidates);
            for (int i = 0; i < candidateCount; i++)
            {
                DeterministicCollisionPrimitive primitive = m_World.Primitives[m_Candidates[i]];
                if (!TryEvaluateDistance(position, primitive, out DeterministicKccContact contact) ||
                    contact.Separation > m_Configuration.CollisionOffset)
                {
                    continue;
                }
                AddContact(m_OverlapContacts, ref m_OverlapContactCount, contact, DeterministicKccQueryStage.Overlap);
            }
            Array.Sort(m_OverlapContacts, 0, m_OverlapContactCount, ContactComparer.Instance);
            summary = new DeterministicKccQuerySummary(1, candidateCount, m_OverlapContactCount, 0);
            return m_OverlapContactCount;
        }

        public DeterministicKccContact OverlapContactAt(int index)
        {
            if (index < 0 || index >= m_OverlapContactCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_OverlapContacts[index];
        }

        public bool Cast(
            FixedVector3 position,
            FixedVector3 displacement,
            out FixedVector3 safePosition,
            out int contactCount,
            out DeterministicKccQuerySummary summary)
        {
            m_CastContactCount = 0;
            safePosition = position + displacement;
            contactCount = 0;
            summary = default;
            FixedScalar speed = displacement.Magnitude;
            if (speed <= m_Configuration.MinimumMovementDistance)
                return false;
            RequireCastDistance(speed);

            int candidateCount = m_Index.Query(
                SweptCapsuleBounds(position, displacement, m_Configuration.CollisionOffset),
                m_Candidates);
            bool found = false;
            FixedScalar earliest = FixedScalar.One;
            int iterations = 0;
            for (int i = 0; i < candidateCount; i++)
            {
                DeterministicCollisionPrimitive primitive = m_World.Primitives[m_Candidates[i]];
                if (!TryCastPrimitive(position, displacement, primitive, out DeterministicKccContact hit, out int primitiveIterations))
                {
                    iterations = checked(iterations + primitiveIterations);
                    continue;
                }
                iterations = checked(iterations + primitiveIterations);
                if (!found || hit.TimeOfImpact < earliest)
                {
                    earliest = hit.TimeOfImpact;
                    found = true;
                }
            }
            if (!found)
            {
                summary = new DeterministicKccQuerySummary(1, candidateCount, 0, iterations);
                return false;
            }

            FixedVector3 hitPosition = position + Scale(displacement, earliest);
            for (int i = 0; i < candidateCount; i++)
            {
                DeterministicCollisionPrimitive primitive = m_World.Primitives[m_Candidates[i]];
                if (!TryEvaluateDistance(hitPosition, primitive, out DeterministicKccContact contact))
                    continue;
                if (contact.Separation > m_Configuration.CollisionOffset + m_Configuration.QueryTolerance)
                    continue;
                if (FixedVector3.Dot(displacement, contact.Normal) >= -m_Configuration.MinimumMovementDistance &&
                    contact.Separation >= -m_Configuration.QueryTolerance)
                {
                    continue;
                }
                AddContact(
                    m_CastContacts,
                    ref m_CastContactCount,
                    contact.WithTimeOfImpact(earliest),
                    DeterministicKccQueryStage.ShapeCast);
            }
            if (m_CastContactCount == 0)
            {
                throw new DeterministicKccQueryException(
                    DeterministicKccQueryStage.ShapeCast,
                    "The earliest conservative TOI produced no canonical blocking contact.");
            }
            Array.Sort(m_CastContacts, 0, m_CastContactCount, ContactComparer.Instance);
            safePosition = hitPosition;
            contactCount = m_CastContactCount;
            summary = new DeterministicKccQuerySummary(1, candidateCount, contactCount, iterations);
            return true;
        }

        public DeterministicKccContact CastContactAt(int index)
        {
            if (index < 0 || index >= m_CastContactCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_CastContacts[index];
        }

        public int CastAll(
            FixedVector3 position,
            FixedVector3 displacement,
            out DeterministicKccQuerySummary summary)
        {
            m_AllCastContactCount = 0;
            FixedScalar speed = displacement.Magnitude;
            if (speed <= m_Configuration.MinimumMovementDistance)
            {
                summary = default;
                return 0;
            }
            RequireCastDistance(speed);
            int candidateCount = m_Index.Query(
                SweptCapsuleBounds(position, displacement, m_Configuration.CollisionOffset),
                m_Candidates);
            int iterations = 0;
            for (int i = 0; i < candidateCount; i++)
            {
                DeterministicCollisionPrimitive primitive = m_World.Primitives[m_Candidates[i]];
                if (!TryCastPrimitive(position, displacement, primitive, out DeterministicKccContact hit, out int primitiveIterations))
                {
                    iterations = checked(iterations + primitiveIterations);
                    continue;
                }
                iterations = checked(iterations + primitiveIterations);
                AddContact(m_AllCastContacts, ref m_AllCastContactCount, hit, DeterministicKccQueryStage.ShapeCast);
            }
            Array.Sort(m_AllCastContacts, 0, m_AllCastContactCount, ContactComparer.Instance);
            summary = new DeterministicKccQuerySummary(1, candidateCount, m_AllCastContactCount, iterations);
            return m_AllCastContactCount;
        }

        public DeterministicKccContact AllCastContactAt(int index)
        {
            if (index < 0 || index >= m_AllCastContactCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_AllCastContacts[index];
        }

        void RequireCastDistance(FixedScalar distance)
        {
            if (distance > m_Configuration.MaximumMovementDistance)
            {
                throw new DeterministicKccQueryException(
                    DeterministicKccQueryStage.ShapeCast,
                    $"Requested displacement '{distance.Raw}' exceeds the locked movement distance '{m_Configuration.MaximumMovementDistance.Raw}'.");
            }
        }

        bool TryCastPrimitive(
            FixedVector3 position,
            FixedVector3 displacement,
            DeterministicCollisionPrimitive primitive,
            out DeterministicKccContact hit,
            out int iterationCount)
        {
            FixedScalar time = FixedScalar.Zero;
            iterationCount = 0;
            hit = default;
            for (int iteration = 0; iteration < m_Configuration.MaximumSweepIterations; iteration++)
            {
                iterationCount++;
                FixedVector3 samplePosition = position + Scale(displacement, time);
                if (!TryEvaluateDistance(samplePosition, primitive, out DeterministicKccContact sample))
                    return false;
                FixedScalar gap = sample.Separation - m_Configuration.CollisionOffset;
                FixedScalar closing = FixedVector3.Dot(displacement, sample.Normal);
                if (gap <= m_Configuration.QueryTolerance)
                {
                    if (closing < -m_Configuration.MinimumMovementDistance || sample.Separation < -m_Configuration.QueryTolerance)
                    {
                        hit = sample.WithTimeOfImpact(time);
                        return true;
                    }
                    return false;
                }
                FixedScalar closingRate = -closing;
                if (closingRate <= m_Configuration.MinimumMovementDistance)
                    return false;
                time += gap / closingRate;
                if (time > FixedScalar.One)
                    return false;
            }
            throw new DeterministicKccQueryException(
                DeterministicKccQueryStage.ShapeCast,
                $"Conservative advancement did not converge after '{m_Configuration.MaximumSweepIterations}' iterations.",
                primitive.Id);
        }

        bool TryEvaluateDistance(
            FixedVector3 position,
            DeterministicCollisionPrimitive primitive,
            out DeterministicKccContact contact)
        {
            return primitive.Kind switch
            {
                DeterministicCollisionPrimitiveKind.Plane => TryPlaneDistance(position, primitive, out contact),
                DeterministicCollisionPrimitiveKind.Triangle => TryTriangleDistance(position, primitive, out contact),
                DeterministicCollisionPrimitiveKind.Box => TryBoxDistance(position, primitive, out contact),
                _ => throw new DeterministicKccQueryException(
                    DeterministicKccQueryStage.Distance,
                    $"Primitive kind '{primitive.Kind}' is unsupported.",
                    primitive.Id)
            };
        }

        void AddContact(
            DeterministicKccContact[] target,
            ref int count,
            DeterministicKccContact contact,
            DeterministicKccQueryStage stage)
        {
            for (int i = 0; i < count; i++)
            {
                DeterministicKccContact current = target[i];
                if (current.PrimitiveId == contact.PrimitiveId && current.FeatureId == contact.FeatureId)
                {
                    if (contact.Separation < current.Separation)
                        target[i] = contact;
                    return;
                }
            }
            if (count >= target.Length)
            {
                throw new DeterministicKccQueryException(
                    stage,
                    "Canonical contact buffer capacity was exceeded.",
                    contact.PrimitiveId,
                    count + 1,
                    target.Length);
            }
            target[count++] = contact;
        }

        void CapsuleSegment(FixedVector3 position, out FixedVector3 start, out FixedVector3 end)
        {
            start = new FixedVector3(position.X, position.Y + m_Configuration.Radius, position.Z);
            end = new FixedVector3(position.X, position.Y + m_Configuration.Height - m_Configuration.Radius, position.Z);
        }

        DeterministicCollisionBounds CapsuleBounds(FixedVector3 position, FixedScalar expansion)
        {
            FixedScalar radius = m_Configuration.Radius + expansion;
            return new DeterministicCollisionBounds(
                new FixedVector3(position.X - radius, position.Y - expansion, position.Z - radius),
                new FixedVector3(position.X + radius, position.Y + m_Configuration.Height + expansion, position.Z + radius));
        }

        DeterministicCollisionBounds SweptCapsuleBounds(
            FixedVector3 position,
            FixedVector3 displacement,
            FixedScalar expansion)
        {
            DeterministicCollisionBounds start = CapsuleBounds(position, expansion);
            DeterministicCollisionBounds end = CapsuleBounds(position + displacement, expansion);
            return new DeterministicCollisionBounds(
                new FixedVector3(
                    FixedScalar.Min(start.Minimum.X, end.Minimum.X),
                    FixedScalar.Min(start.Minimum.Y, end.Minimum.Y),
                    FixedScalar.Min(start.Minimum.Z, end.Minimum.Z)),
                new FixedVector3(
                    FixedScalar.Max(start.Maximum.X, end.Maximum.X),
                    FixedScalar.Max(start.Maximum.Y, end.Maximum.Y),
                    FixedScalar.Max(start.Maximum.Z, end.Maximum.Z)));
        }

        static FixedVector3 Scale(FixedVector3 value, FixedScalar scale) =>
            new FixedVector3(value.X * scale, value.Y * scale, value.Z * scale);

        sealed class ContactComparer : IComparer<DeterministicKccContact>
        {
            public static ContactComparer Instance { get; } = new ContactComparer();

            public int Compare(DeterministicKccContact left, DeterministicKccContact right)
            {
                int time = left.TimeOfImpact.CompareTo(right.TimeOfImpact);
                if (time != 0)
                    return time;
                int surface = left.SurfaceId.CompareTo(right.SurfaceId);
                if (surface != 0)
                    return surface;
                int primitive = left.PrimitiveId.CompareTo(right.PrimitiveId);
                if (primitive != 0)
                    return primitive;
                int feature = left.FeatureId.CompareTo(right.FeatureId);
                if (feature != 0)
                    return feature;
                int x = left.WorldPoint.X.Raw.CompareTo(right.WorldPoint.X.Raw);
                if (x != 0)
                    return x;
                int y = left.WorldPoint.Y.Raw.CompareTo(right.WorldPoint.Y.Raw);
                return y != 0 ? y : left.WorldPoint.Z.Raw.CompareTo(right.WorldPoint.Z.Raw);
            }
        }
    }
}
