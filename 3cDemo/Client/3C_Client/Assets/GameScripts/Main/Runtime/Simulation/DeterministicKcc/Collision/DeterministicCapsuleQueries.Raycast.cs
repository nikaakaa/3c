using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    internal sealed partial class DeterministicCapsuleQueries
    {
        public bool Raycast(
            FixedVector3 origin,
            FixedVector3 direction,
            FixedScalar distance,
            out DeterministicKccRayHit closest,
            out DeterministicKccQuerySummary summary)
        {
            m_RayHitCount = 0;
            closest = default;
            if (direction.SqrMagnitude == FixedScalar.Zero || distance <= FixedScalar.Zero)
            {
                summary = default;
                return false;
            }
            FixedVector3 normalized = direction.Normalized;
            FixedVector3 end = origin + Scale(normalized, distance);
            int candidateCount = m_Index.Query(SegmentBounds(origin, end, m_Configuration.QueryTolerance), m_Candidates);
            for (int i = 0; i < candidateCount; i++)
            {
                DeterministicCollisionPrimitive primitive = m_World.Primitives[m_Candidates[i]];
                if (TryRaycastPrimitive(origin, normalized, distance, primitive, out DeterministicKccRayHit hit))
                    AddRayHit(hit);
            }
            Array.Sort(m_RayHits, 0, m_RayHitCount, RayHitComparer.Instance);
            summary = new DeterministicKccQuerySummary(1, candidateCount, m_RayHitCount, 0);
            if (m_RayHitCount == 0)
                return false;
            closest = m_RayHits[0];
            return true;
        }

        bool TryRaycastPrimitive(
            FixedVector3 origin,
            FixedVector3 direction,
            FixedScalar maximumDistance,
            DeterministicCollisionPrimitive primitive,
            out DeterministicKccRayHit hit)
        {
            return primitive.Kind switch
            {
                DeterministicCollisionPrimitiveKind.Plane => TryRaycastPlane(origin, direction, maximumDistance, primitive, out hit),
                DeterministicCollisionPrimitiveKind.Triangle => TryRaycastTriangle(origin, direction, maximumDistance, primitive, out hit),
                DeterministicCollisionPrimitiveKind.Box => TryRaycastBox(origin, direction, maximumDistance, primitive, out hit),
                _ => throw new DeterministicKccQueryException(
                    DeterministicKccQueryStage.Raycast,
                    $"Primitive kind '{primitive.Kind}' is unsupported.",
                    primitive.Id)
            };
        }

        bool TryRaycastPlane(
            FixedVector3 origin,
            FixedVector3 direction,
            FixedScalar maximumDistance,
            DeterministicCollisionPrimitive primitive,
            out DeterministicKccRayHit hit)
        {
            FixedScalar denominator = FixedVector3.Dot(direction, primitive.Normal);
            if (FixedScalar.Abs(denominator) <= m_Configuration.QueryTolerance)
            {
                hit = default;
                return false;
            }
            FixedScalar distance = (primitive.Distance - FixedVector3.Dot(origin, primitive.Normal)) / denominator;
            FixedVector3 point = origin + Scale(direction, distance);
            if (distance < FixedScalar.Zero || distance > maximumDistance || !ContainsBounds(primitive.Bounds, point))
            {
                hit = default;
                return false;
            }
            FixedVector3 normal = denominator < FixedScalar.Zero ? primitive.Normal : -primitive.Normal;
            hit = new DeterministicKccRayHit(
                primitive.SurfaceId,
                primitive.Id,
                new DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind.PlaneFace, 0),
                distance,
                point,
                normal);
            return true;
        }

        bool TryRaycastTriangle(
            FixedVector3 origin,
            FixedVector3 direction,
            FixedScalar maximumDistance,
            DeterministicCollisionPrimitive triangle,
            out DeterministicKccRayHit hit)
        {
            FixedScalar denominator = FixedVector3.Dot(direction, triangle.Normal);
            if (denominator >= -m_Configuration.QueryTolerance)
            {
                hit = default;
                return false;
            }
            FixedScalar distance = (triangle.Distance - FixedVector3.Dot(origin, triangle.Normal)) / denominator;
            if (distance < FixedScalar.Zero || distance > maximumDistance)
            {
                hit = default;
                return false;
            }
            FixedVector3 point = origin + Scale(direction, distance);
            FixedVector3 closest = ClosestPointOnTriangle(point, triangle, out DeterministicCollisionFeatureId feature);
            FixedScalar toleranceSquared = m_Configuration.QueryTolerance * m_Configuration.QueryTolerance;
            if ((point - closest).SqrMagnitude > toleranceSquared)
            {
                hit = default;
                return false;
            }
            hit = new DeterministicKccRayHit(
                triangle.SurfaceId,
                triangle.Id,
                feature,
                distance,
                point,
                triangle.Normal);
            return true;
        }

        bool TryRaycastBox(
            FixedVector3 origin,
            FixedVector3 direction,
            FixedScalar maximumDistance,
            DeterministicCollisionPrimitive box,
            out DeterministicKccRayHit hit)
        {
            FixedScalar enter = FixedScalar.FromInt64(-1);
            FixedScalar exit = maximumDistance;
            int enterFace = -1;
            int exitFace = -1;
            if (!UpdateRaySlab(origin.X, direction.X, box.Bounds.Minimum.X, box.Bounds.Maximum.X, 0, 1,
                    ref enter, ref exit, ref enterFace, ref exitFace) ||
                !UpdateRaySlab(origin.Y, direction.Y, box.Bounds.Minimum.Y, box.Bounds.Maximum.Y, 2, 3,
                    ref enter, ref exit, ref enterFace, ref exitFace) ||
                !UpdateRaySlab(origin.Z, direction.Z, box.Bounds.Minimum.Z, box.Bounds.Maximum.Z, 4, 5,
                    ref enter, ref exit, ref enterFace, ref exitFace))
            {
                hit = default;
                return false;
            }

            FixedScalar distance = enter >= FixedScalar.Zero ? enter : exit;
            int face = enter >= FixedScalar.Zero ? enterFace : exitFace;
            if (distance < FixedScalar.Zero || distance > maximumDistance || face < 0)
            {
                hit = default;
                return false;
            }
            hit = new DeterministicKccRayHit(
                box.SurfaceId,
                box.Id,
                new DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind.BoxFace, face),
                distance,
                origin + Scale(direction, distance),
                BoxFaceNormal(face));
            return true;
        }

        bool UpdateRaySlab(
            FixedScalar origin,
            FixedScalar direction,
            FixedScalar minimum,
            FixedScalar maximum,
            int minimumFace,
            int maximumFace,
            ref FixedScalar enter,
            ref FixedScalar exit,
            ref int enterFace,
            ref int exitFace)
        {
            if (FixedScalar.Abs(direction) <= m_Configuration.QueryTolerance)
                return origin >= minimum && origin <= maximum;
            FixedScalar first = (minimum - origin) / direction;
            FixedScalar second = (maximum - origin) / direction;
            int firstFace = minimumFace;
            int secondFace = maximumFace;
            if (first > second)
            {
                (first, second) = (second, first);
                (firstFace, secondFace) = (secondFace, firstFace);
            }
            if (first > enter || first == enter && (enterFace < 0 || firstFace < enterFace))
            {
                enter = first;
                enterFace = firstFace;
            }
            if (second < exit || second == exit && (exitFace < 0 || secondFace < exitFace))
            {
                exit = second;
                exitFace = secondFace;
            }
            return enter <= exit;
        }

        void AddRayHit(DeterministicKccRayHit hit)
        {
            if (m_RayHitCount >= m_RayHits.Length)
            {
                throw new DeterministicKccQueryException(
                    DeterministicKccQueryStage.Raycast,
                    "Canonical ray hit buffer capacity was exceeded.",
                    hit.PrimitiveId,
                    m_RayHitCount + 1,
                    m_RayHits.Length);
            }
            m_RayHits[m_RayHitCount++] = hit;
        }

        static DeterministicCollisionBounds SegmentBounds(
            FixedVector3 start,
            FixedVector3 end,
            FixedScalar expansion)
        {
            return new DeterministicCollisionBounds(
                new FixedVector3(
                    FixedScalar.Min(start.X, end.X) - expansion,
                    FixedScalar.Min(start.Y, end.Y) - expansion,
                    FixedScalar.Min(start.Z, end.Z) - expansion),
                new FixedVector3(
                    FixedScalar.Max(start.X, end.X) + expansion,
                    FixedScalar.Max(start.Y, end.Y) + expansion,
                    FixedScalar.Max(start.Z, end.Z) + expansion));
        }

        bool ContainsBounds(DeterministicCollisionBounds bounds, FixedVector3 point)
        {
            FixedScalar tolerance = m_Configuration.QueryTolerance;
            return point.X >= bounds.Minimum.X - tolerance && point.X <= bounds.Maximum.X + tolerance &&
                   point.Y >= bounds.Minimum.Y - tolerance && point.Y <= bounds.Maximum.Y + tolerance &&
                   point.Z >= bounds.Minimum.Z - tolerance && point.Z <= bounds.Maximum.Z + tolerance;
        }

        static FixedVector3 BoxFaceNormal(int face)
        {
            return face switch
            {
                0 => new FixedVector3(-FixedScalar.One, FixedScalar.Zero, FixedScalar.Zero),
                1 => new FixedVector3(FixedScalar.One, FixedScalar.Zero, FixedScalar.Zero),
                2 => new FixedVector3(FixedScalar.Zero, -FixedScalar.One, FixedScalar.Zero),
                3 => new FixedVector3(FixedScalar.Zero, FixedScalar.One, FixedScalar.Zero),
                4 => new FixedVector3(FixedScalar.Zero, FixedScalar.Zero, -FixedScalar.One),
                5 => new FixedVector3(FixedScalar.Zero, FixedScalar.Zero, FixedScalar.One),
                _ => throw new ArgumentOutOfRangeException(nameof(face))
            };
        }

        sealed class RayHitComparer : IComparer<DeterministicKccRayHit>
        {
            public static RayHitComparer Instance { get; } = new RayHitComparer();

            public int Compare(DeterministicKccRayHit left, DeterministicKccRayHit right)
            {
                int distance = left.Distance.CompareTo(right.Distance);
                if (distance != 0)
                    return distance;
                int surface = left.SurfaceId.CompareTo(right.SurfaceId);
                if (surface != 0)
                    return surface;
                int primitive = left.PrimitiveId.CompareTo(right.PrimitiveId);
                if (primitive != 0)
                    return primitive;
                int feature = left.FeatureId.CompareTo(right.FeatureId);
                if (feature != 0)
                    return feature;
                int x = left.Point.X.Raw.CompareTo(right.Point.X.Raw);
                if (x != 0)
                    return x;
                int y = left.Point.Y.Raw.CompareTo(right.Point.Y.Raw);
                return y != 0 ? y : left.Point.Z.Raw.CompareTo(right.Point.Z.Raw);
            }
        }
    }
}
