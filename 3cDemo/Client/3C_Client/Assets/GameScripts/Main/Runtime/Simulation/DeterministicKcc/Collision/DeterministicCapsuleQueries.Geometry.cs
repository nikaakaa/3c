using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    internal sealed partial class DeterministicCapsuleQueries
    {
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
    }
}
