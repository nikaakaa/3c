using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    public enum DeterministicCollisionPrimitiveKind : byte
    {
        Plane = 1,
        Triangle = 2,
        Box = 3
    }

    public enum DeterministicCollisionFeatureKind : byte
    {
        PlaneFace = 1,
        TriangleVertex = 2,
        TriangleEdge = 3,
        TriangleFace = 4,
        BoxFace = 5
    }

    public readonly struct DeterministicCollisionFeatureId : IEquatable<DeterministicCollisionFeatureId>, IComparable<DeterministicCollisionFeatureId>
    {
        public DeterministicCollisionFeatureId(DeterministicCollisionFeatureKind kind, int index)
        {
            if (!Enum.IsDefined(typeof(DeterministicCollisionFeatureKind), kind) || index < 0)
                throw new ArgumentException("Collision feature identity is invalid.");
            Kind = kind;
            Index = index;
        }

        public DeterministicCollisionFeatureKind Kind { get; }
        public int Index { get; }
        public bool IsValid => Index >= 0 && Enum.IsDefined(typeof(DeterministicCollisionFeatureKind), Kind);
        public static DeterministicCollisionFeatureId Invalid => default;

        public int CompareTo(DeterministicCollisionFeatureId other)
        {
            int kind = ((byte)Kind).CompareTo((byte)other.Kind);
            return kind != 0 ? kind : Index.CompareTo(other.Index);
        }

        public bool Equals(DeterministicCollisionFeatureId other) => Kind == other.Kind && Index == other.Index;
        public override bool Equals(object obj) => obj is DeterministicCollisionFeatureId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine((byte)Kind, Index);
        public static bool operator ==(DeterministicCollisionFeatureId left, DeterministicCollisionFeatureId right) => left.Equals(right);
        public static bool operator !=(DeterministicCollisionFeatureId left, DeterministicCollisionFeatureId right) => !left.Equals(right);
    }

    public readonly struct DeterministicCollisionBounds
    {
        public DeterministicCollisionBounds(FixedVector3 minimum, FixedVector3 maximum)
        {
            if (minimum.X > maximum.X || minimum.Y > maximum.Y || minimum.Z > maximum.Z)
                throw new ArgumentException("Collision bounds minimum exceeds maximum.");
            Minimum = minimum;
            Maximum = maximum;
        }

        public FixedVector3 Minimum { get; }
        public FixedVector3 Maximum { get; }
    }

    public sealed class DeterministicCollisionSurface
    {
        public DeterministicCollisionSurface(int id, string identity, string materialIdentity, bool walkable)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id));
            Id = id;
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            MaterialIdentity = SimulationIdentity.Require(materialIdentity, nameof(materialIdentity));
            Walkable = walkable;
        }

        public int Id { get; }
        public string Identity { get; }
        public string MaterialIdentity { get; }
        public bool Walkable { get; }
    }

    public sealed class DeterministicCollisionPrimitive
    {
        DeterministicCollisionPrimitive(
            int id,
            DeterministicCollisionPrimitiveKind kind,
            int surfaceId,
            FixedVector3 a,
            FixedVector3 b,
            FixedVector3 c,
            FixedVector3 normal,
            FixedScalar distance,
            DeterministicCollisionBounds bounds,
            int vertex0,
            int vertex1,
            int vertex2,
            int adjacentPrimitive0,
            int adjacentPrimitive1,
            int adjacentPrimitive2)
        {
            if (id < 0 || surfaceId < 0 || !Enum.IsDefined(typeof(DeterministicCollisionPrimitiveKind), kind))
                throw new ArgumentOutOfRangeException();
            Id = id;
            Kind = kind;
            SurfaceId = surfaceId;
            A = a;
            B = b;
            C = c;
            Normal = normal;
            Distance = distance;
            Bounds = bounds;
            Vertex0 = vertex0;
            Vertex1 = vertex1;
            Vertex2 = vertex2;
            AdjacentPrimitive0 = adjacentPrimitive0;
            AdjacentPrimitive1 = adjacentPrimitive1;
            AdjacentPrimitive2 = adjacentPrimitive2;
        }

        public int Id { get; }
        public DeterministicCollisionPrimitiveKind Kind { get; }
        public int SurfaceId { get; }
        public FixedVector3 A { get; }
        public FixedVector3 B { get; }
        public FixedVector3 C { get; }
        public FixedVector3 Normal { get; }
        public FixedScalar Distance { get; }
        public DeterministicCollisionBounds Bounds { get; }
        public int Vertex0 { get; }
        public int Vertex1 { get; }
        public int Vertex2 { get; }
        public int AdjacentPrimitive0 { get; }
        public int AdjacentPrimitive1 { get; }
        public int AdjacentPrimitive2 { get; }

        public static DeterministicCollisionPrimitive Plane(
            int id,
            int surfaceId,
            FixedVector3 normal,
            FixedScalar distance,
            DeterministicCollisionBounds bounds)
        {
            FixedVector3 normalized = RequireNormal(normal);
            return new DeterministicCollisionPrimitive(
                id,
                DeterministicCollisionPrimitiveKind.Plane,
                surfaceId,
                FixedVector3.Zero,
                FixedVector3.Zero,
                FixedVector3.Zero,
                normalized,
                distance,
                bounds,
                -1,
                -1,
                -1,
                -1,
                -1,
                -1);
        }

        public static DeterministicCollisionPrimitive Triangle(
            int id,
            int surfaceId,
            int vertex0,
            int vertex1,
            int vertex2,
            FixedVector3 a,
            FixedVector3 b,
            FixedVector3 c,
            int adjacentPrimitive0,
            int adjacentPrimitive1,
            int adjacentPrimitive2)
        {
            FixedVector3 normal = RequireNormal(FixedVector3.Cross(b - a, c - a));
            FixedScalar distance = FixedVector3.Dot(normal, a);
            var bounds = new DeterministicCollisionBounds(
                new FixedVector3(
                    FixedScalar.Min(a.X, FixedScalar.Min(b.X, c.X)),
                    FixedScalar.Min(a.Y, FixedScalar.Min(b.Y, c.Y)),
                    FixedScalar.Min(a.Z, FixedScalar.Min(b.Z, c.Z))),
                new FixedVector3(
                    FixedScalar.Max(a.X, FixedScalar.Max(b.X, c.X)),
                    FixedScalar.Max(a.Y, FixedScalar.Max(b.Y, c.Y)),
                    FixedScalar.Max(a.Z, FixedScalar.Max(b.Z, c.Z))));
            return new DeterministicCollisionPrimitive(
                id,
                DeterministicCollisionPrimitiveKind.Triangle,
                surfaceId,
                a,
                b,
                c,
                normal,
                distance,
                bounds,
                vertex0,
                vertex1,
                vertex2,
                adjacentPrimitive0,
                adjacentPrimitive1,
                adjacentPrimitive2);
        }

        public static DeterministicCollisionPrimitive Box(
            int id,
            int surfaceId,
            FixedVector3 minimum,
            FixedVector3 maximum)
        {
            var bounds = new DeterministicCollisionBounds(minimum, maximum);
            return new DeterministicCollisionPrimitive(
                id,
                DeterministicCollisionPrimitiveKind.Box,
                surfaceId,
                minimum,
                maximum,
                FixedVector3.Zero,
                FixedVector3.Zero,
                FixedScalar.Zero,
                bounds,
                -1,
                -1,
                -1,
                -1,
                -1,
                -1);
        }

        public int AdjacentPrimitiveAt(int edgeIndex)
        {
            if (Kind != DeterministicCollisionPrimitiveKind.Triangle || edgeIndex < 0 || edgeIndex > 2)
                throw new ArgumentOutOfRangeException(nameof(edgeIndex));
            return edgeIndex == 0 ? AdjacentPrimitive0 : edgeIndex == 1 ? AdjacentPrimitive1 : AdjacentPrimitive2;
        }

        static FixedVector3 RequireNormal(FixedVector3 value)
        {
            FixedScalar magnitude = value.Magnitude;
            if (magnitude == FixedScalar.Zero)
                throw new ArgumentException("Collision primitive normal is zero.");
            return new FixedVector3(value.X / magnitude, value.Y / magnitude, value.Z / magnitude);
        }
    }

    public sealed class DeterministicCollisionWorldArtifact
    {
        public const int ArtifactVersion = 2;
        public const string ArtifactSchema = "deterministic-collision-world/2";
        readonly ReadOnlyCollection<DeterministicCollisionSurface> m_Surfaces;
        readonly ReadOnlyCollection<FixedVector3> m_Vertices;
        readonly ReadOnlyCollection<DeterministicCollisionPrimitive> m_Primitives;

        public DeterministicCollisionWorldArtifact(
            string mapId,
            int quantizationUnitsPerMeter,
            DeterministicCollisionBounds bounds,
            IEnumerable<DeterministicCollisionSurface> surfaces,
            IEnumerable<FixedVector3> vertices,
            IEnumerable<DeterministicCollisionPrimitive> primitives)
        {
            MapId = SimulationIdentity.Require(mapId, nameof(mapId));
            if (quantizationUnitsPerMeter <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantizationUnitsPerMeter));
            QuantizationUnitsPerMeter = quantizationUnitsPerMeter;
            Bounds = bounds;
            var surfaceValues = new List<DeterministicCollisionSurface>(surfaces ?? throw new ArgumentNullException(nameof(surfaces)));
            surfaceValues.Sort((left, right) => left.Id.CompareTo(right.Id));
            if (surfaceValues.Count == 0)
                throw new ArgumentException("Collision world requires a surface catalog.", nameof(surfaces));
            for (int i = 0; i < surfaceValues.Count; i++)
            {
                if (surfaceValues[i].Id != i)
                    throw new ArgumentException("Collision surface ids must be dense and canonical.", nameof(surfaces));
            }
            var vertexValues = new List<FixedVector3>(vertices ?? throw new ArgumentNullException(nameof(vertices)));
            var uniqueVertices = new HashSet<FixedVector3>();
            for (int i = 0; i < vertexValues.Count; i++)
            {
                if (!uniqueVertices.Add(vertexValues[i]))
                    throw new ArgumentException($"Collision vertex '{i}' duplicates an earlier canonical vertex.", nameof(vertices));
            }
            var primitiveValues = new List<DeterministicCollisionPrimitive>(primitives ?? throw new ArgumentNullException(nameof(primitives)));
            primitiveValues.Sort((left, right) => left.Id.CompareTo(right.Id));
            for (int i = 0; i < primitiveValues.Count; i++)
            {
                if (primitiveValues[i].Id != i || primitiveValues[i].SurfaceId >= surfaceValues.Count)
                    throw new ArgumentException("Collision primitive ids or surface references are invalid.", nameof(primitives));
                ValidateVertices(primitiveValues[i], vertexValues);
                ValidateAdjacency(primitiveValues[i], primitiveValues);
            }
            m_Surfaces = surfaceValues.AsReadOnly();
            m_Vertices = vertexValues.AsReadOnly();
            m_Primitives = primitiveValues.AsReadOnly();
            ContentHash = DeterministicCollisionWorldCodec.ComputeContentHash(this);
        }

        public string MapId { get; }
        public int QuantizationUnitsPerMeter { get; }
        public DeterministicCollisionBounds Bounds { get; }
        public IReadOnlyList<DeterministicCollisionSurface> Surfaces => m_Surfaces;
        public IReadOnlyList<FixedVector3> Vertices => m_Vertices;
        public IReadOnlyList<DeterministicCollisionPrimitive> Primitives => m_Primitives;
        public StableHash ContentHash { get; }

        public bool AreAdjacent(int leftPrimitiveId, int rightPrimitiveId)
        {
            if (leftPrimitiveId < 0 || rightPrimitiveId < 0 || leftPrimitiveId >= m_Primitives.Count || rightPrimitiveId >= m_Primitives.Count)
                return false;
            DeterministicCollisionPrimitive left = m_Primitives[leftPrimitiveId];
            if (left.Kind != DeterministicCollisionPrimitiveKind.Triangle)
                return false;
            return left.AdjacentPrimitive0 == rightPrimitiveId ||
                   left.AdjacentPrimitive1 == rightPrimitiveId ||
                   left.AdjacentPrimitive2 == rightPrimitiveId;
        }

        static void ValidateAdjacency(
            DeterministicCollisionPrimitive primitive,
            IReadOnlyList<DeterministicCollisionPrimitive> primitives)
        {
            if (primitive.Kind != DeterministicCollisionPrimitiveKind.Triangle)
            {
                if (primitive.AdjacentPrimitive0 != -1 || primitive.AdjacentPrimitive1 != -1 || primitive.AdjacentPrimitive2 != -1)
                    throw new ArgumentException("Only triangle primitives may declare adjacency.");
                return;
            }
            for (int edge = 0; edge < 3; edge++)
            {
                int adjacent = primitive.AdjacentPrimitiveAt(edge);
                if (adjacent < -1 || adjacent == primitive.Id || adjacent >= primitives.Count)
                    throw new ArgumentException($"Triangle '{primitive.Id}' edge '{edge}' has invalid adjacency '{adjacent}'.");
                if (adjacent >= 0 && primitives[adjacent].Kind != DeterministicCollisionPrimitiveKind.Triangle)
                    throw new ArgumentException($"Triangle '{primitive.Id}' edge '{edge}' references a non-triangle primitive.");
                if (adjacent >= 0 && !HasMatchingAdjacentEdge(primitive, edge, primitives[adjacent]))
                    throw new ArgumentException($"Triangle '{primitive.Id}' edge '{edge}' adjacency '{adjacent}' is not symmetric or does not share the same canonical edge.");
            }
        }

        static bool HasMatchingAdjacentEdge(
            DeterministicCollisionPrimitive primitive,
            int edgeIndex,
            DeterministicCollisionPrimitive adjacent)
        {
            EdgeVertices(primitive, edgeIndex, out int left, out int right);
            for (int edge = 0; edge < 3; edge++)
            {
                if (adjacent.AdjacentPrimitiveAt(edge) != primitive.Id)
                    continue;
                EdgeVertices(adjacent, edge, out int adjacentLeft, out int adjacentRight);
                if (left == adjacentRight && right == adjacentLeft || left == adjacentLeft && right == adjacentRight)
                    return true;
            }
            return false;
        }

        static void EdgeVertices(
            DeterministicCollisionPrimitive primitive,
            int edgeIndex,
            out int left,
            out int right)
        {
            if (edgeIndex == 0)
            {
                left = primitive.Vertex0;
                right = primitive.Vertex1;
            }
            else if (edgeIndex == 1)
            {
                left = primitive.Vertex1;
                right = primitive.Vertex2;
            }
            else
            {
                left = primitive.Vertex2;
                right = primitive.Vertex0;
            }
        }

        static void ValidateVertices(
            DeterministicCollisionPrimitive primitive,
            IReadOnlyList<FixedVector3> vertices)
        {
            if (primitive.Kind != DeterministicCollisionPrimitiveKind.Triangle)
            {
                if (primitive.Vertex0 != -1 || primitive.Vertex1 != -1 || primitive.Vertex2 != -1)
                    throw new ArgumentException("Only triangle primitives may reference canonical vertices.");
                return;
            }
            if (primitive.Vertex0 < 0 || primitive.Vertex1 < 0 || primitive.Vertex2 < 0 ||
                primitive.Vertex0 >= vertices.Count || primitive.Vertex1 >= vertices.Count || primitive.Vertex2 >= vertices.Count ||
                primitive.Vertex0 == primitive.Vertex1 || primitive.Vertex1 == primitive.Vertex2 || primitive.Vertex2 == primitive.Vertex0 ||
                primitive.A != vertices[primitive.Vertex0] || primitive.B != vertices[primitive.Vertex1] || primitive.C != vertices[primitive.Vertex2])
            {
                throw new ArgumentException($"Triangle '{primitive.Id}' has invalid canonical vertex references.");
            }
        }
    }
}
