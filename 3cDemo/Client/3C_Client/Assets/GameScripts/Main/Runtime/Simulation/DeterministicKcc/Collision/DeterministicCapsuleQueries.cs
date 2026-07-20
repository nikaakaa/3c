using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    internal sealed class DeterministicCapsuleQueries
    {
        readonly DeterministicCollisionWorldArtifact m_World;
        readonly DeterministicKccConfiguration m_Configuration;
        readonly DeterministicCollisionWorldIndex m_Index;
        readonly int[] m_Candidates;
        readonly DeterministicKccContact[] m_OverlapContacts;
        readonly DeterministicKccContact[] m_CastContacts;

        int m_OverlapContactCount;
        int m_CastContactCount;

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
        }

        public int Overlap(FixedVector3 position, out DeterministicKccQuerySummary summary)
        {
            m_OverlapContactCount = 0;
            int candidateCount = m_Index.Query(CapsuleBounds(position, m_Configuration.SkinWidth), m_Candidates);
            for (int i = 0; i < candidateCount; i++)
            {
                DeterministicCollisionPrimitive primitive = m_World.Primitives[m_Candidates[i]];
                if (!TryEvaluateDistance(position, primitive, out DeterministicKccContact contact) ||
                    contact.Separation > m_Configuration.SkinWidth)
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
            if (speed > m_Configuration.MaximumMovementDistance)
            {
                throw new DeterministicKccQueryException(
                    DeterministicKccQueryStage.ShapeCast,
                    $"Requested displacement '{speed.Raw}' exceeds the locked movement distance '{m_Configuration.MaximumMovementDistance.Raw}'.");
            }

            DeterministicCollisionBounds sweptBounds = SweptCapsuleBounds(position, displacement, m_Configuration.SkinWidth);
            int candidateCount = m_Index.Query(sweptBounds, m_Candidates);
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
                if (contact.Separation > m_Configuration.SkinWidth + m_Configuration.QueryTolerance)
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
                FixedScalar gap = sample.Separation - m_Configuration.SkinWidth;
                FixedScalar closing = FixedVector3.Dot(displacement, sample.Normal);
                if (gap <= m_Configuration.QueryTolerance)
                {
                    if (closing < -m_Configuration.MinimumMovementDistance ||
                        sample.Separation < -m_Configuration.QueryTolerance)
                    {
                        hit = sample.WithTimeOfImpact(time);
                        return true;
                    }
                    return false;
                }

                FixedScalar closingRate = -closing;
                if (closingRate <= m_Configuration.MinimumMovementDistance)
                    return false;
                FixedScalar advance = gap / closingRate;
                time += advance;
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

        bool TryPlaneDistance(
            FixedVector3 position,
            DeterministicCollisionPrimitive primitive,
            out DeterministicKccContact contact)
        {
            CapsuleSegment(position, out FixedVector3 segmentStart, out FixedVector3 segmentEnd);
            FixedScalar startDistance = FixedVector3.Dot(segmentStart, primitive.Normal) - primitive.Distance;
            FixedScalar endDistance = FixedVector3.Dot(segmentEnd, primitive.Normal) - primitive.Distance;
            FixedVector3 axisPoint;
            FixedScalar signedDistance;
            if (startDistance <= FixedScalar.Zero && endDistance >= FixedScalar.Zero ||
                endDistance <= FixedScalar.Zero && startDistance >= FixedScalar.Zero)
            {
                FixedScalar denominator = startDistance - endDistance;
                FixedScalar amount = denominator == FixedScalar.Zero
                    ? FixedScalar.Zero
                    : FixedScalar.Clamp(startDistance / denominator, FixedScalar.Zero, FixedScalar.One);
                axisPoint = segmentStart + Scale(segmentEnd - segmentStart, amount);
                signedDistance = FixedVector3.Dot(position, primitive.Normal) - primitive.Distance;
            }
            else if (FixedScalar.Abs(startDistance) <= FixedScalar.Abs(endDistance))
            {
                axisPoint = segmentStart;
                signedDistance = startDistance;
            }
            else
            {
                axisPoint = segmentEnd;
                signedDistance = endDistance;
            }
            FixedVector3 normal = signedDistance < FixedScalar.Zero ? -primitive.Normal : primitive.Normal;
            FixedScalar distance = FixedScalar.Abs(signedDistance);
            FixedVector3 worldPoint = axisPoint - Scale(normal, distance);
            contact = CreateContact(
                primitive,
                new DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind.PlaneFace, 0),
                axisPoint,
                worldPoint,
                normal,
                distance);
            return true;
        }

        bool TryTriangleDistance(
            FixedVector3 position,
            DeterministicCollisionPrimitive triangle,
            out DeterministicKccContact contact)
        {
            CapsuleSegment(position, out FixedVector3 segmentStart, out FixedVector3 segmentEnd);
            FixedScalar startPlaneDistance = FixedVector3.Dot(segmentStart, triangle.Normal) - triangle.Distance;
            FixedScalar endPlaneDistance = FixedVector3.Dot(segmentEnd, triangle.Normal) - triangle.Distance;
            if (FixedScalar.Max(startPlaneDistance, endPlaneDistance) < -m_Configuration.QueryTolerance)
            {
                contact = default;
                return false;
            }

            bool hasCandidate = false;
            FixedScalar bestDistanceSquared = FixedScalar.Zero;
            FixedVector3 bestAxisPoint = FixedVector3.Zero;
            FixedVector3 bestWorldPoint = FixedVector3.Zero;
            DeterministicCollisionFeatureId bestFeature = default;
            FixedScalar denominator = startPlaneDistance - endPlaneDistance;
            if ((startPlaneDistance <= FixedScalar.Zero && endPlaneDistance >= FixedScalar.Zero ||
                 endPlaneDistance <= FixedScalar.Zero && startPlaneDistance >= FixedScalar.Zero) &&
                denominator != FixedScalar.Zero)
            {
                FixedScalar amount = FixedScalar.Clamp(startPlaneDistance / denominator, FixedScalar.Zero, FixedScalar.One);
                FixedVector3 planePoint = segmentStart + Scale(segmentEnd - segmentStart, amount);
                if (ContainsTrianglePoint(triangle, planePoint))
                {
                    ConsiderCandidate(
                        planePoint,
                        planePoint,
                        new DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind.TriangleFace, 0),
                        ref hasCandidate,
                        ref bestDistanceSquared,
                        ref bestAxisPoint,
                        ref bestWorldPoint,
                        ref bestFeature);
                }
            }

            FixedVector3 startClosest = ClosestPointOnTriangle(segmentStart, triangle, out DeterministicCollisionFeatureId startFeature);
            ConsiderCandidate(segmentStart, startClosest, startFeature, ref hasCandidate, ref bestDistanceSquared,
                ref bestAxisPoint, ref bestWorldPoint, ref bestFeature);
            FixedVector3 endClosest = ClosestPointOnTriangle(segmentEnd, triangle, out DeterministicCollisionFeatureId endFeature);
            ConsiderCandidate(segmentEnd, endClosest, endFeature, ref hasCandidate, ref bestDistanceSquared,
                ref bestAxisPoint, ref bestWorldPoint, ref bestFeature);
            ConsiderSegmentPair(segmentStart, segmentEnd, triangle.A, triangle.B, 0,
                ref hasCandidate, ref bestDistanceSquared, ref bestAxisPoint, ref bestWorldPoint, ref bestFeature);
            ConsiderSegmentPair(segmentStart, segmentEnd, triangle.B, triangle.C, 1,
                ref hasCandidate, ref bestDistanceSquared, ref bestAxisPoint, ref bestWorldPoint, ref bestFeature);
            ConsiderSegmentPair(segmentStart, segmentEnd, triangle.C, triangle.A, 2,
                ref hasCandidate, ref bestDistanceSquared, ref bestAxisPoint, ref bestWorldPoint, ref bestFeature);

            if (!hasCandidate)
            {
                contact = default;
                return false;
            }
            FixedScalar distance = FixedScalar.Sqrt(bestDistanceSquared);
            FixedVector3 normal = distance <= m_Configuration.QueryTolerance
                ? triangle.Normal
                : Scale(bestAxisPoint - bestWorldPoint, FixedScalar.One / distance);
            if (FixedVector3.Dot(normal, triangle.Normal) < FixedScalar.Zero)
            {
                contact = default;
                return false;
            }
            contact = CreateContact(triangle, bestFeature, bestAxisPoint, bestWorldPoint, normal, distance);
            return true;
        }

        bool TryBoxDistance(
            FixedVector3 position,
            DeterministicCollisionPrimitive primitive,
            out DeterministicKccContact contact)
        {
            FixedVector3 minimum = primitive.Bounds.Minimum;
            FixedVector3 maximum = primitive.Bounds.Maximum;
            FixedScalar segmentMinimum = position.Y + m_Configuration.Radius;
            FixedScalar segmentMaximum = position.Y + m_Configuration.Height - m_Configuration.Radius;
            FixedScalar axisY;
            FixedScalar boxY;
            if (segmentMaximum < minimum.Y)
            {
                axisY = segmentMaximum;
                boxY = minimum.Y;
            }
            else if (segmentMinimum > maximum.Y)
            {
                axisY = segmentMinimum;
                boxY = maximum.Y;
            }
            else
            {
                axisY = FixedScalar.Clamp(segmentMinimum, minimum.Y, maximum.Y);
                boxY = axisY;
            }
            FixedVector3 axisPoint = new FixedVector3(position.X, axisY, position.Z);
            FixedVector3 worldPoint = new FixedVector3(
                FixedScalar.Clamp(position.X, minimum.X, maximum.X),
                boxY,
                FixedScalar.Clamp(position.Z, minimum.Z, maximum.Z));
            FixedVector3 delta = axisPoint - worldPoint;
            FixedScalar distanceSquared = delta.SqrMagnitude;
            if (distanceSquared > FixedScalar.Zero)
            {
                FixedScalar distance = FixedScalar.Sqrt(distanceSquared);
                FixedVector3 normal = Scale(delta, FixedScalar.One / distance);
                contact = CreateContact(
                    primitive,
                    new DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind.BoxFace, SelectBoxFace(normal)),
                    axisPoint,
                    worldPoint,
                    normal,
                    distance);
                return true;
            }

            SelectInsideBoxContact(
                position,
                segmentMinimum,
                segmentMaximum,
                minimum,
                maximum,
                out FixedVector3 insideNormal,
                out FixedScalar penetration,
                out int faceIndex);
            FixedVector3 insideAxis = new FixedVector3(position.X, FixedScalar.Clamp(axisY, minimum.Y, maximum.Y), position.Z);
            FixedVector3 insideWorld = insideAxis - Scale(insideNormal, -penetration + m_Configuration.Radius);
            contact = new DeterministicKccContact(
                primitive.Id,
                primitive.SurfaceId,
                new DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind.BoxFace, faceIndex),
                insideNormal,
                insideAxis - Scale(insideNormal, m_Configuration.Radius),
                insideWorld,
                -penetration,
                FixedScalar.Zero);
            return true;
        }

        DeterministicKccContact CreateContact(
            DeterministicCollisionPrimitive primitive,
            DeterministicCollisionFeatureId feature,
            FixedVector3 axisPoint,
            FixedVector3 worldPoint,
            FixedVector3 normal,
            FixedScalar axisDistance)
        {
            FixedVector3 normalized = normal.Normalized;
            if (normalized.SqrMagnitude == FixedScalar.Zero)
            {
                throw new DeterministicKccQueryException(
                    DeterministicKccQueryStage.Distance,
                    "Closest-feature query produced a zero normal.",
                    primitive.Id);
            }
            return new DeterministicKccContact(
                primitive.Id,
                primitive.SurfaceId,
                feature,
                normalized,
                axisPoint - Scale(normalized, m_Configuration.Radius),
                worldPoint,
                axisDistance - m_Configuration.Radius,
                FixedScalar.Zero);
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

        void SelectInsideBoxContact(
            FixedVector3 position,
            FixedScalar segmentMinimum,
            FixedScalar segmentMaximum,
            FixedVector3 minimum,
            FixedVector3 maximum,
            out FixedVector3 normal,
            out FixedScalar penetration,
            out int faceIndex)
        {
            FixedScalar radius = m_Configuration.Radius;
            FixedScalar best = position.X - (minimum.X - radius);
            normal = new FixedVector3(-FixedScalar.One, FixedScalar.Zero, FixedScalar.Zero);
            faceIndex = 0;
            CompareBoundary((maximum.X + radius) - position.X,
                new FixedVector3(FixedScalar.One, FixedScalar.Zero, FixedScalar.Zero), 1, ref best, ref normal, ref faceIndex);
            CompareBoundary(segmentMinimum - (minimum.Y - radius),
                new FixedVector3(FixedScalar.Zero, -FixedScalar.One, FixedScalar.Zero), 2, ref best, ref normal, ref faceIndex);
            CompareBoundary((maximum.Y + radius) - segmentMaximum,
                new FixedVector3(FixedScalar.Zero, FixedScalar.One, FixedScalar.Zero), 3, ref best, ref normal, ref faceIndex);
            CompareBoundary(position.Z - (minimum.Z - radius),
                new FixedVector3(FixedScalar.Zero, FixedScalar.Zero, -FixedScalar.One), 4, ref best, ref normal, ref faceIndex);
            CompareBoundary((maximum.Z + radius) - position.Z,
                new FixedVector3(FixedScalar.Zero, FixedScalar.Zero, FixedScalar.One), 5, ref best, ref normal, ref faceIndex);
            penetration = FixedScalar.Max(best, m_Configuration.QueryTolerance);
        }

        static void CompareBoundary(
            FixedScalar candidate,
            FixedVector3 candidateNormal,
            int candidateFace,
            ref FixedScalar best,
            ref FixedVector3 normal,
            ref int faceIndex)
        {
            if (candidate < best)
            {
                best = candidate;
                normal = candidateNormal;
                faceIndex = candidateFace;
            }
        }

        static int SelectBoxFace(FixedVector3 normal)
        {
            FixedScalar x = FixedScalar.Abs(normal.X);
            FixedScalar y = FixedScalar.Abs(normal.Y);
            FixedScalar z = FixedScalar.Abs(normal.Z);
            if (x >= y && x >= z)
                return normal.X < FixedScalar.Zero ? 0 : 1;
            if (y >= z)
                return normal.Y < FixedScalar.Zero ? 2 : 3;
            return normal.Z < FixedScalar.Zero ? 4 : 5;
        }

        static void ConsiderSegmentPair(
            FixedVector3 firstStart,
            FixedVector3 firstEnd,
            FixedVector3 secondStart,
            FixedVector3 secondEnd,
            int edgeIndex,
            ref bool hasCandidate,
            ref FixedScalar bestDistanceSquared,
            ref FixedVector3 bestAxisPoint,
            ref FixedVector3 bestWorldPoint,
            ref DeterministicCollisionFeatureId bestFeature)
        {
            ClosestSegmentPoints(firstStart, firstEnd, secondStart, secondEnd,
                out FixedVector3 firstPoint, out FixedVector3 secondPoint);
            ConsiderCandidate(
                firstPoint,
                secondPoint,
                new DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind.TriangleEdge, edgeIndex),
                ref hasCandidate,
                ref bestDistanceSquared,
                ref bestAxisPoint,
                ref bestWorldPoint,
                ref bestFeature);
        }

        static void ConsiderCandidate(
            FixedVector3 axisPoint,
            FixedVector3 worldPoint,
            DeterministicCollisionFeatureId feature,
            ref bool hasCandidate,
            ref FixedScalar bestDistanceSquared,
            ref FixedVector3 bestAxisPoint,
            ref FixedVector3 bestWorldPoint,
            ref DeterministicCollisionFeatureId bestFeature)
        {
            FixedScalar distanceSquared = (axisPoint - worldPoint).SqrMagnitude;
            if (hasCandidate)
            {
                int distance = distanceSquared.CompareTo(bestDistanceSquared);
                if (distance > 0 || distance == 0 && CompareFeatureWitness(feature, worldPoint, bestFeature, bestWorldPoint) >= 0)
                    return;
            }
            hasCandidate = true;
            bestDistanceSquared = distanceSquared;
            bestAxisPoint = axisPoint;
            bestWorldPoint = worldPoint;
            bestFeature = feature;
        }

        static int CompareFeatureWitness(
            DeterministicCollisionFeatureId leftFeature,
            FixedVector3 leftPoint,
            DeterministicCollisionFeatureId rightFeature,
            FixedVector3 rightPoint)
        {
            int feature = FeatureRank(leftFeature.Kind).CompareTo(FeatureRank(rightFeature.Kind));
            if (feature == 0)
                feature = leftFeature.Index.CompareTo(rightFeature.Index);
            if (feature != 0)
                return feature;
            int x = leftPoint.X.Raw.CompareTo(rightPoint.X.Raw);
            if (x != 0)
                return x;
            int y = leftPoint.Y.Raw.CompareTo(rightPoint.Y.Raw);
            return y != 0 ? y : leftPoint.Z.Raw.CompareTo(rightPoint.Z.Raw);
        }

        static int FeatureRank(DeterministicCollisionFeatureKind kind)
        {
            return kind == DeterministicCollisionFeatureKind.TriangleFace
                ? 0
                : kind == DeterministicCollisionFeatureKind.TriangleEdge
                    ? 1
                    : kind == DeterministicCollisionFeatureKind.TriangleVertex
                        ? 2
                        : 3;
        }

        static FixedVector3 ClosestPointOnTriangle(
            FixedVector3 point,
            DeterministicCollisionPrimitive triangle,
            out DeterministicCollisionFeatureId feature)
        {
            FixedVector3 ab = triangle.B - triangle.A;
            FixedVector3 ac = triangle.C - triangle.A;
            FixedVector3 ap = point - triangle.A;
            FixedScalar d1 = FixedVector3.Dot(ab, ap);
            FixedScalar d2 = FixedVector3.Dot(ac, ap);
            if (d1 <= FixedScalar.Zero && d2 <= FixedScalar.Zero)
            {
                feature = new DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind.TriangleVertex, 0);
                return triangle.A;
            }

            FixedVector3 bp = point - triangle.B;
            FixedScalar d3 = FixedVector3.Dot(ab, bp);
            FixedScalar d4 = FixedVector3.Dot(ac, bp);
            if (d3 >= FixedScalar.Zero && d4 <= d3)
            {
                feature = new DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind.TriangleVertex, 1);
                return triangle.B;
            }

            FixedScalar vc = d1 * d4 - d3 * d2;
            if (vc <= FixedScalar.Zero && d1 >= FixedScalar.Zero && d3 <= FixedScalar.Zero)
            {
                feature = new DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind.TriangleEdge, 0);
                return triangle.A + Scale(ab, d1 / (d1 - d3));
            }

            FixedVector3 cp = point - triangle.C;
            FixedScalar d5 = FixedVector3.Dot(ab, cp);
            FixedScalar d6 = FixedVector3.Dot(ac, cp);
            if (d6 >= FixedScalar.Zero && d5 <= d6)
            {
                feature = new DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind.TriangleVertex, 2);
                return triangle.C;
            }

            FixedScalar vb = d5 * d2 - d1 * d6;
            if (vb <= FixedScalar.Zero && d2 >= FixedScalar.Zero && d6 <= FixedScalar.Zero)
            {
                feature = new DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind.TriangleEdge, 2);
                return triangle.A + Scale(ac, d2 / (d2 - d6));
            }

            FixedScalar va = d3 * d6 - d5 * d4;
            if (va <= FixedScalar.Zero && d4 - d3 >= FixedScalar.Zero && d5 - d6 >= FixedScalar.Zero)
            {
                feature = new DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind.TriangleEdge, 1);
                FixedVector3 edge = triangle.C - triangle.B;
                FixedScalar amount = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return triangle.B + Scale(edge, amount);
            }

            FixedScalar inverse = FixedScalar.One / (va + vb + vc);
            FixedScalar v = vb * inverse;
            FixedScalar w = vc * inverse;
            feature = new DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind.TriangleFace, 0);
            return triangle.A + Scale(ab, v) + Scale(ac, w);
        }

        static void ClosestSegmentPoints(
            FixedVector3 firstStart,
            FixedVector3 firstEnd,
            FixedVector3 secondStart,
            FixedVector3 secondEnd,
            out FixedVector3 firstPoint,
            out FixedVector3 secondPoint)
        {
            FixedVector3 firstDirection = firstEnd - firstStart;
            FixedVector3 secondDirection = secondEnd - secondStart;
            FixedVector3 offset = firstStart - secondStart;
            FixedScalar firstLength = FixedVector3.Dot(firstDirection, firstDirection);
            FixedScalar secondLength = FixedVector3.Dot(secondDirection, secondDirection);
            FixedScalar secondProjection = FixedVector3.Dot(secondDirection, offset);
            FixedScalar firstAmount;
            FixedScalar secondAmount;
            if (firstLength == FixedScalar.Zero && secondLength == FixedScalar.Zero)
            {
                firstAmount = FixedScalar.Zero;
                secondAmount = FixedScalar.Zero;
            }
            else if (firstLength == FixedScalar.Zero)
            {
                firstAmount = FixedScalar.Zero;
                secondAmount = FixedScalar.Clamp(secondProjection / secondLength, FixedScalar.Zero, FixedScalar.One);
            }
            else
            {
                FixedScalar firstProjection = FixedVector3.Dot(firstDirection, offset);
                if (secondLength == FixedScalar.Zero)
                {
                    secondAmount = FixedScalar.Zero;
                    firstAmount = FixedScalar.Clamp(-firstProjection / firstLength, FixedScalar.Zero, FixedScalar.One);
                }
                else
                {
                    FixedScalar directions = FixedVector3.Dot(firstDirection, secondDirection);
                    FixedScalar denominator = firstLength * secondLength - directions * directions;
                    firstAmount = denominator == FixedScalar.Zero
                        ? FixedScalar.Zero
                        : FixedScalar.Clamp(
                            (directions * secondProjection - firstProjection * secondLength) / denominator,
                            FixedScalar.Zero,
                            FixedScalar.One);
                    secondAmount = (directions * firstAmount + secondProjection) / secondLength;
                    if (secondAmount < FixedScalar.Zero)
                    {
                        secondAmount = FixedScalar.Zero;
                        firstAmount = FixedScalar.Clamp(-firstProjection / firstLength, FixedScalar.Zero, FixedScalar.One);
                    }
                    else if (secondAmount > FixedScalar.One)
                    {
                        secondAmount = FixedScalar.One;
                        firstAmount = FixedScalar.Clamp((directions - firstProjection) / firstLength, FixedScalar.Zero, FixedScalar.One);
                    }
                }
            }
            firstPoint = firstStart + Scale(firstDirection, firstAmount);
            secondPoint = secondStart + Scale(secondDirection, secondAmount);
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

        static bool ContainsTrianglePoint(DeterministicCollisionPrimitive triangle, FixedVector3 point)
        {
            FixedScalar first = FixedVector3.Dot(FixedVector3.Cross(triangle.B - triangle.A, point - triangle.A), triangle.Normal);
            FixedScalar second = FixedVector3.Dot(FixedVector3.Cross(triangle.C - triangle.B, point - triangle.B), triangle.Normal);
            FixedScalar third = FixedVector3.Dot(FixedVector3.Cross(triangle.A - triangle.C, point - triangle.C), triangle.Normal);
            return first >= FixedScalar.Zero && second >= FixedScalar.Zero && third >= FixedScalar.Zero;
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
