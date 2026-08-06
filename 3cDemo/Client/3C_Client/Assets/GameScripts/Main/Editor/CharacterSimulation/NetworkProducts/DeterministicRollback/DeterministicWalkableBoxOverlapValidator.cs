using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    readonly struct DeterministicWalkableBoxSource
    {
        public DeterministicWalkableBoxSource(
            string colliderIdentity,
            string surfaceIdentity,
            FixedVector3[] vertices)
        {
            ColliderIdentity = colliderIdentity ?? throw new ArgumentNullException(nameof(colliderIdentity));
            SurfaceIdentity = surfaceIdentity ?? throw new ArgumentNullException(nameof(surfaceIdentity));
            Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
            if (vertices.Length != 8)
                throw new ArgumentException("Walkable Box validation requires eight vertices.", nameof(vertices));
        }

        public string ColliderIdentity { get; }
        public string SurfaceIdentity { get; }
        public FixedVector3[] Vertices { get; }
    }

    static class DeterministicWalkableBoxOverlapValidator
    {
        public static void Validate(
            IReadOnlyList<DeterministicWalkableBoxSource> sources,
            int quantizationUnitsPerMeter)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));
            if (quantizationUnitsPerMeter <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantizationUnitsPerMeter));
            FixedScalar tolerance = FixedScalar.FromRatio(1, quantizationUnitsPerMeter);
            var overlaps = new List<string>();
            for (int leftIndex = 0; leftIndex < sources.Count; leftIndex++)
            {
                for (int rightIndex = leftIndex + 1; rightIndex < sources.Count; rightIndex++)
                {
                    DeterministicWalkableBoxSource left = sources[leftIndex];
                    DeterministicWalkableBoxSource right = sources[rightIndex];
                    if (!TryFindPositiveVolumeOverlap(left, right, tolerance, out FixedVector3 axis, out FixedScalar depth))
                        continue;
                    overlaps.Add(
                        $"left='{left.ColliderIdentity}' surface='{left.SurfaceIdentity}', right='{right.ColliderIdentity}' surface='{right.SurfaceIdentity}', axisRaw={axis.X.Raw},{axis.Y.Raw},{axis.Z.Raw}, depthRaw={depth.Raw}");
                }
            }
            if (overlaps.Count > 0)
                throw new InvalidDataException(
                    $"Walkable Box colliders overlap with positive volume ({overlaps.Count} pair(s)):{Environment.NewLine}{string.Join(Environment.NewLine, overlaps)}");
        }

        static bool TryFindPositiveVolumeOverlap(
            DeterministicWalkableBoxSource left,
            DeterministicWalkableBoxSource right,
            FixedScalar tolerance,
            out FixedVector3 minimumAxis,
            out FixedScalar minimumDepth)
        {
            FixedVector3 leftX = left.Vertices[1] - left.Vertices[0];
            FixedVector3 leftY = left.Vertices[4] - left.Vertices[0];
            FixedVector3 leftZ = left.Vertices[3] - left.Vertices[0];
            FixedVector3 rightX = right.Vertices[1] - right.Vertices[0];
            FixedVector3 rightY = right.Vertices[4] - right.Vertices[0];
            FixedVector3 rightZ = right.Vertices[3] - right.Vertices[0];
            if (!HaveDivergentSupportAxes(leftY, rightY, tolerance) ||
                !HavePositiveHorizontalTopOverlap(left.Vertices, right.Vertices, tolerance))
            {
                minimumAxis = FixedVector3.Zero;
                minimumDepth = FixedScalar.Zero;
                return false;
            }
            var leftAxes = new[] { leftX, leftY, leftZ };
            var rightAxes = new[] { rightX, rightY, rightZ };
            minimumAxis = FixedVector3.Zero;
            minimumDepth = FixedScalar.Zero;
            bool foundAxis = false;

            for (int i = 0; i < leftAxes.Length; i++)
            {
                if (!EvaluateAxis(left, right, leftAxes[i], tolerance, ref foundAxis, ref minimumAxis, ref minimumDepth))
                    return false;
            }
            for (int i = 0; i < rightAxes.Length; i++)
            {
                if (!EvaluateAxis(left, right, rightAxes[i], tolerance, ref foundAxis, ref minimumAxis, ref minimumDepth))
                    return false;
            }
            for (int i = 0; i < leftAxes.Length; i++)
            {
                for (int j = 0; j < rightAxes.Length; j++)
                {
                    FixedVector3 axis = FixedVector3.Cross(leftAxes[i], rightAxes[j]);
                    if (!EvaluateAxis(left, right, axis, tolerance, ref foundAxis, ref minimumAxis, ref minimumDepth))
                        return false;
                }
            }
            return foundAxis;
        }

        static bool HaveDivergentSupportAxes(
            FixedVector3 leftAxis,
            FixedVector3 rightAxis,
            FixedScalar tolerance)
        {
            FixedScalar leftLength = leftAxis.Magnitude;
            FixedScalar rightLength = rightAxis.Magnitude;
            if (leftLength <= tolerance || rightLength <= tolerance)
                return false;
            FixedVector3 leftNormal = Scale(leftAxis, FixedScalar.One / leftLength);
            FixedVector3 rightNormal = Scale(rightAxis, FixedScalar.One / rightLength);
            return FixedVector3.Cross(leftNormal, rightNormal).Magnitude > tolerance;
        }

        static bool HavePositiveHorizontalTopOverlap(
            IReadOnlyList<FixedVector3> leftVertices,
            IReadOnlyList<FixedVector3> rightVertices,
            FixedScalar tolerance)
        {
            bool foundAxis = false;
            for (int polygonIndex = 0; polygonIndex < 2; polygonIndex++)
            {
                IReadOnlyList<FixedVector3> vertices = polygonIndex == 0 ? leftVertices : rightVertices;
                for (int i = 4; i < 8; i++)
                {
                    int next = i == 7 ? 4 : i + 1;
                    FixedVector3 edge = vertices[next] - vertices[i];
                    FixedVector3 axis = new FixedVector3(edge.Z, FixedScalar.Zero, -edge.X);
                    FixedScalar length = axis.Magnitude;
                    if (length <= tolerance)
                        continue;
                    foundAxis = true;
                    FixedVector3 normalized = Scale(axis, FixedScalar.One / length);
                    ProjectTop(leftVertices, normalized, out FixedScalar leftMinimum, out FixedScalar leftMaximum);
                    ProjectTop(rightVertices, normalized, out FixedScalar rightMinimum, out FixedScalar rightMaximum);
                    FixedScalar overlap = FixedScalar.Min(leftMaximum, rightMaximum) -
                                          FixedScalar.Max(leftMinimum, rightMinimum);
                    if (overlap <= tolerance)
                        return false;
                }
            }
            return foundAxis;
        }

        static void ProjectTop(
            IReadOnlyList<FixedVector3> vertices,
            FixedVector3 axis,
            out FixedScalar minimum,
            out FixedScalar maximum)
        {
            minimum = FixedVector3.Dot(vertices[4], axis);
            maximum = minimum;
            for (int i = 5; i < 8; i++)
            {
                FixedScalar value = FixedVector3.Dot(vertices[i], axis);
                minimum = FixedScalar.Min(minimum, value);
                maximum = FixedScalar.Max(maximum, value);
            }
        }

        static bool EvaluateAxis(
            DeterministicWalkableBoxSource left,
            DeterministicWalkableBoxSource right,
            FixedVector3 axis,
            FixedScalar tolerance,
            ref bool foundAxis,
            ref FixedVector3 minimumAxis,
            ref FixedScalar minimumDepth)
        {
            FixedScalar length = axis.Magnitude;
            if (length <= tolerance)
                return true;
            FixedVector3 normalized = Scale(axis, FixedScalar.One / length);
            Project(left.Vertices, normalized, out FixedScalar leftMinimum, out FixedScalar leftMaximum);
            Project(right.Vertices, normalized, out FixedScalar rightMinimum, out FixedScalar rightMaximum);
            FixedScalar overlap = FixedScalar.Min(leftMaximum, rightMaximum) -
                                  FixedScalar.Max(leftMinimum, rightMinimum);
            if (overlap <= tolerance)
                return false;
            if (!foundAxis || overlap < minimumDepth)
            {
                foundAxis = true;
                minimumAxis = normalized;
                minimumDepth = overlap;
            }
            return true;
        }

        static void Project(
            IReadOnlyList<FixedVector3> vertices,
            FixedVector3 axis,
            out FixedScalar minimum,
            out FixedScalar maximum)
        {
            minimum = FixedVector3.Dot(vertices[0], axis);
            maximum = minimum;
            for (int i = 1; i < vertices.Count; i++)
            {
                FixedScalar value = FixedVector3.Dot(vertices[i], axis);
                minimum = FixedScalar.Min(minimum, value);
                maximum = FixedScalar.Max(maximum, value);
            }
        }

        static FixedVector3 Scale(FixedVector3 value, FixedScalar scalar) =>
            new FixedVector3(value.X * scalar, value.Y * scalar, value.Z * scalar);
    }
}
