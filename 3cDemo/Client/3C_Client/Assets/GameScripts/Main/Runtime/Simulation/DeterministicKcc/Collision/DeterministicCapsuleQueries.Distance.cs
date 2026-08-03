using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    internal sealed partial class DeterministicCapsuleQueries
    {
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

        static bool ContainsTrianglePoint(DeterministicCollisionPrimitive triangle, FixedVector3 point)
        {
            FixedScalar first = FixedVector3.Dot(FixedVector3.Cross(triangle.B - triangle.A, point - triangle.A), triangle.Normal);
            FixedScalar second = FixedVector3.Dot(FixedVector3.Cross(triangle.C - triangle.B, point - triangle.B), triangle.Normal);
            FixedScalar third = FixedVector3.Dot(FixedVector3.Cross(triangle.A - triangle.C, point - triangle.C), triangle.Normal);
            return first >= FixedScalar.Zero && second >= FixedScalar.Zero && third >= FixedScalar.Zero;
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
    }
}
