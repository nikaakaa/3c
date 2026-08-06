using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation.DeterministicKcc;
using ThirdPersonSimulation.Fixed;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    [CustomEditor(typeof(DeterministicCollisionWorldAuthoring))]
    public sealed class DeterministicCollisionWorldAuthoringEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            using (new EditorGUI.DisabledScope(!CanBake((DeterministicCollisionWorldAuthoring)target)))
            {
                if (GUILayout.Button("Bake Collision Artifact"))
                    DeterministicCollisionWorldBaker.Bake((DeterministicCollisionWorldAuthoring)target);
            }
        }

        static bool CanBake(DeterministicCollisionWorldAuthoring authoring) =>
            authoring != null && authoring.Output != null;
    }

    public static class DeterministicCollisionWorldBaker
    {
        [MenuItem("Tools/3C/Simulation/Bake Deterministic Collision World")]
        public static void BakeSelected()
        {
            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            DeterministicCollisionWorldAuthoring[] authorings = UnityEngine.Object
                .FindObjectsOfType<DeterministicCollisionWorldAuthoring>(true)
                .Where(value => value != null && value.gameObject.scene == activeScene)
                .ToArray();
            if (authorings.Length != 1)
                throw new InvalidOperationException($"Active scene '{activeScene.path}' requires exactly one DeterministicCollisionWorldAuthoring, found {authorings.Length}.");
            Bake(authorings[0]);
        }

        public static void Bake(DeterministicCollisionWorldAuthoring authoring)
        {
            if (authoring == null)
                throw new ArgumentNullException(nameof(authoring));
            if (authoring.Output == null)
                throw new InvalidOperationException("Deterministic Collision World authoring has no output asset.");
            DeterministicCollisionWorldArtifact artifact = Build(authoring);
            Undo.RecordObject(authoring.Output, "Bake Deterministic Collision World");
            authoring.Output.Replace(artifact);
            EditorUtility.SetDirty(authoring.Output);
            AssetDatabase.SaveAssets();
        }

        public static DeterministicCollisionWorldArtifact Build(DeterministicCollisionWorldAuthoring authoring)
        {
            int units = authoring.QuantizationUnitsPerMeter;
            if (units <= 0 || string.IsNullOrWhiteSpace(authoring.MapId))
                throw new InvalidDataException("Deterministic Collision World authoring identity is invalid.");
            StairTraversalWorldValidationReport stairReport = StairTraversalSurfaceValidator.ValidateWorld(authoring);
            if (stairReport.HasErrors)
                throw new InvalidDataException($"Deterministic Collision World stair authoring validation failed:{Environment.NewLine}{stairReport.FormatErrors()}");
            var surfaces = new List<DeterministicCollisionSurface>();
            var surfaceIds = new Dictionary<string, int>(StringComparer.Ordinal);
            var vertices = new List<FixedVector3>();
            var vertexIds = new Dictionary<VertexKey, int>();
            var primitives = new List<DeterministicCollisionPrimitive>();
            var colliders = new HashSet<Collider>();
            DeterministicCollisionSurfaceAuthoring[] sources = authoring
                .GetComponentsInChildren<DeterministicCollisionSurfaceAuthoring>(true)
                .Where(value => value != null && value.isActiveAndEnabled)
                .OrderBy(value => HierarchyPath(authoring.transform, value.transform), StringComparer.Ordinal)
                .ToArray();
            if (sources.Length == 0)
                throw new InvalidDataException("Deterministic Collision World authoring has no surface sources.");
            var records = new List<ColliderSourceRecord>();
            var walkableBoxes = new List<DeterministicWalkableBoxSource>();
            for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
            {
                DeterministicCollisionSurfaceAuthoring source = sources[sourceIndex];
                if (string.IsNullOrWhiteSpace(source.SurfaceIdentity) || string.IsNullOrWhiteSpace(source.MaterialIdentity))
                    throw new InvalidDataException("Deterministic collision surface identity is invalid.");
                Collider[] sourceColliders = source
                    .GetComponentsInChildren<Collider>(true)
                    .Where(value => value != null && value.enabled && value.gameObject.activeInHierarchy)
                    .OrderBy(value => ColliderIdentity(source.transform, value), StringComparer.Ordinal)
                    .ToArray();
                if (sourceColliders.Length == 0)
                    throw new InvalidDataException($"Collision surface '{source.SurfaceIdentity}' has no active colliders.");
                for (int colliderIndex = 0; colliderIndex < sourceColliders.Length; colliderIndex++)
                {
                    Collider collider = sourceColliders[colliderIndex];
                    if (!colliders.Add(collider))
                        throw new InvalidDataException($"Collider '{collider.name}' belongs to multiple deterministic surfaces.");
                    if (collider.isTrigger)
                        throw new InvalidDataException($"Collider '{collider.name}' is a trigger and cannot enter the deterministic collision world.");
                    ValidateColliderType(collider);
                    string colliderIdentity = ColliderIdentity(source.transform, collider);
                    records.Add(new ColliderSourceRecord(source, collider));
                    if (source.Walkable && collider is BoxCollider box)
                    {
                        walkableBoxes.Add(new DeterministicWalkableBoxSource(
                            colliderIdentity,
                            source.SurfaceIdentity.Trim(),
                            BoxWorldVertices(box, units)));
                    }
                }
            }
            DeterministicWalkableBoxOverlapValidator.Validate(walkableBoxes, units);
            for (int recordIndex = 0; recordIndex < records.Count; recordIndex++)
            {
                ColliderSourceRecord record = records[recordIndex];
                int surfaceId = ResolveSurface(record.Source, record.Collider, surfaces, surfaceIds);
                switch (record.Collider)
                {
                    case BoxCollider box:
                        AddBox(box, surfaceId, units, vertices, vertexIds, primitives);
                        break;
                    case MeshCollider mesh:
                        AddMesh(mesh, surfaceId, units, vertices, vertexIds, primitives);
                        break;
                    case TerrainCollider terrain:
                        AddTerrain(terrain, surfaceId, units, vertices, vertexIds, primitives);
                        break;
                }
            }
            Bounds worldBounds = authoring.WorldBounds;
            if (worldBounds.size.x <= 0f || worldBounds.size.y <= 0f || worldBounds.size.z <= 0f)
                throw new InvalidDataException("Deterministic Collision World bounds must be positive.");
            var bounds = new DeterministicCollisionBounds(
                Quantize(worldBounds.min, units),
                Quantize(worldBounds.max, units));
            var artifact = new DeterministicCollisionWorldArtifact(
                authoring.MapId.Trim(),
                units,
                bounds,
                surfaces,
                vertices,
                primitives);
            return DeterministicCollisionWorldCodec.Read(DeterministicCollisionWorldCodec.Write(artifact));
        }

        static int ResolveSurface(
            DeterministicCollisionSurfaceAuthoring source,
            Collider collider,
            List<DeterministicCollisionSurface> surfaces,
            Dictionary<string, int> surfaceIds)
        {
            if (string.IsNullOrWhiteSpace(source.SurfaceIdentity) || string.IsNullOrWhiteSpace(source.MaterialIdentity))
                throw new InvalidDataException("Deterministic collision surface identity is invalid.");
            string identity = $"{source.SurfaceIdentity.Trim()}@{ColliderIdentity(source.transform, collider)}";
            string material = source.MaterialIdentity.Trim();
            string key = $"{identity}|{material}|{source.Walkable}";
            if (surfaceIds.TryGetValue(key, out int current))
                return current;
            int id = surfaces.Count;
            surfaces.Add(new DeterministicCollisionSurface(id, identity, material, source.Walkable));
            surfaceIds.Add(key, id);
            return id;
        }

        static void ValidateColliderType(Collider collider)
        {
            if (collider is BoxCollider || collider is MeshCollider || collider is TerrainCollider)
                return;
            throw new InvalidDataException(
                $"Collider '{collider.name}' type '{collider.GetType().Name}' is not supported by the deterministic artifact.");
        }

        static void AddBox(
            BoxCollider collider,
            int surfaceId,
            int units,
            List<FixedVector3> vertices,
            Dictionary<VertexKey, int> vertexIds,
            List<DeterministicCollisionPrimitive> primitives)
        {
            if (Quaternion.Angle(collider.transform.rotation, Quaternion.identity) <= 0.001f)
            {
                Bounds bounds = collider.bounds;
                primitives.Add(DeterministicCollisionPrimitive.Box(
                    primitives.Count,
                    surfaceId,
                    Quantize(bounds.min, units),
                    Quantize(bounds.max, units)));
                return;
            }

            FixedVector3[] world = BoxWorldVertices(collider, units);
            var triangles = new[]
            {
                new TriangleInput(world[0], world[1], world[2]),
                new TriangleInput(world[0], world[2], world[3]),
                new TriangleInput(world[4], world[6], world[5]),
                new TriangleInput(world[4], world[7], world[6]),
                new TriangleInput(world[0], world[5], world[1]),
                new TriangleInput(world[0], world[4], world[5]),
                new TriangleInput(world[3], world[2], world[6]),
                new TriangleInput(world[3], world[6], world[7]),
                new TriangleInput(world[0], world[3], world[7]),
                new TriangleInput(world[0], world[7], world[4]),
                new TriangleInput(world[1], world[6], world[2]),
                new TriangleInput(world[1], world[5], world[6])
            };
            AddTriangleSurface(collider.name, surfaceId, triangles, vertices, vertexIds, primitives);
        }

        static FixedVector3[] BoxWorldVertices(BoxCollider collider, int units)
        {
            Vector3 center = collider.center;
            Vector3 half = collider.size * 0.5f;
            Vector3[] local =
            {
                center + new Vector3(-half.x, -half.y, -half.z),
                center + new Vector3(half.x, -half.y, -half.z),
                center + new Vector3(half.x, -half.y, half.z),
                center + new Vector3(-half.x, -half.y, half.z),
                center + new Vector3(-half.x, half.y, -half.z),
                center + new Vector3(half.x, half.y, -half.z),
                center + new Vector3(half.x, half.y, half.z),
                center + new Vector3(-half.x, half.y, half.z)
            };
            var world = new FixedVector3[local.Length];
            for (int i = 0; i < local.Length; i++)
                world[i] = Quantize(collider.transform.TransformPoint(local[i]), units);
            return world;
        }

        static string ColliderIdentity(Transform surfaceRoot, Collider collider)
        {
            Collider[] values = collider.GetComponents<Collider>();
            int componentIndex = Array.IndexOf(values, collider);
            return $"{HierarchyPath(surfaceRoot, collider.transform)}|{collider.GetType().FullName}|{componentIndex}";
        }

        static string HierarchyPath(Transform root, Transform value)
        {
            var names = new Stack<string>();
            Transform current = value;
            while (current != null)
            {
                names.Push(current.name);
                if (current == root)
                    return string.Join("/", names);
                current = current.parent;
            }
            throw new InvalidDataException($"Transform '{value.name}' is outside deterministic authoring root '{root.name}'.");
        }

        static void AddMesh(
            MeshCollider collider,
            int surfaceId,
            int units,
            List<FixedVector3> canonicalVertices,
            Dictionary<VertexKey, int> vertexIds,
            List<DeterministicCollisionPrimitive> primitives)
        {
            Mesh mesh = collider.sharedMesh;
            if (mesh == null)
                throw new InvalidDataException($"MeshCollider '{collider.name}' has no shared Mesh.");
            Vector3[] meshVertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            if (triangles.Length == 0 || triangles.Length % 3 != 0)
                throw new InvalidDataException($"MeshCollider '{collider.name}' has no canonical triangle list.");
            var values = new List<TriangleInput>(triangles.Length / 3);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (triangles[i] < 0 || triangles[i] >= meshVertices.Length ||
                    triangles[i + 1] < 0 || triangles[i + 1] >= meshVertices.Length ||
                    triangles[i + 2] < 0 || triangles[i + 2] >= meshVertices.Length)
                {
                    throw new InvalidDataException($"MeshCollider '{collider.name}' has an out-of-range triangle index at '{i / 3}'.");
                }
                FixedVector3 a = Quantize(collider.transform.TransformPoint(meshVertices[triangles[i]]), units);
                FixedVector3 b = Quantize(collider.transform.TransformPoint(meshVertices[triangles[i + 1]]), units);
                FixedVector3 c = Quantize(collider.transform.TransformPoint(meshVertices[triangles[i + 2]]), units);
                values.Add(new TriangleInput(a, b, c));
            }
            AddTriangleSurface(collider.name, surfaceId, values, canonicalVertices, vertexIds, primitives);
        }

        static void AddTerrain(
            TerrainCollider collider,
            int surfaceId,
            int units,
            List<FixedVector3> vertices,
            Dictionary<VertexKey, int> vertexIds,
            List<DeterministicCollisionPrimitive> primitives)
        {
            TerrainData data = collider.terrainData;
            if (data == null)
                throw new InvalidDataException($"TerrainCollider '{collider.name}' has no TerrainData.");
            int resolution = data.heightmapResolution;
            if (resolution < 2)
                throw new InvalidDataException($"TerrainCollider '{collider.name}' heightmap resolution is invalid.");
            float[,] heights = data.GetHeights(0, 0, resolution, resolution);
            Vector3 size = data.size;
            var values = new List<TriangleInput>(checked((resolution - 1) * (resolution - 1) * 2));
            for (int z = 0; z < resolution - 1; z++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    FixedVector3 a = TerrainVertex(collider, heights, size, resolution, x, z, units);
                    FixedVector3 b = TerrainVertex(collider, heights, size, resolution, x, z + 1, units);
                    FixedVector3 c = TerrainVertex(collider, heights, size, resolution, x + 1, z, units);
                    FixedVector3 d = TerrainVertex(collider, heights, size, resolution, x + 1, z + 1, units);
                    values.Add(new TriangleInput(a, b, c));
                    values.Add(new TriangleInput(c, b, d));
                }
            }
            AddTriangleSurface(collider.name, surfaceId, values, vertices, vertexIds, primitives);
        }

        static FixedVector3 TerrainVertex(
            TerrainCollider collider,
            float[,] heights,
            Vector3 size,
            int resolution,
            int x,
            int z,
            int units)
        {
            float denominator = resolution - 1;
            var local = new Vector3(
                x / denominator * size.x,
                heights[z, x] * size.y,
                z / denominator * size.z);
            return Quantize(collider.transform.TransformPoint(local), units);
        }

        static void AddTriangleSurface(
            string sourceName,
            int surfaceId,
            IReadOnlyList<TriangleInput> inputs,
            List<FixedVector3> vertices,
            Dictionary<VertexKey, int> vertexIds,
            List<DeterministicCollisionPrimitive> primitives)
        {
            int primitiveBase = primitives.Count;
            var records = new TriangleRecord[inputs.Count];
            var edges = new Dictionary<EdgeKey, EdgeReference>();
            for (int i = 0; i < inputs.Count; i++)
            {
                TriangleInput input = inputs[i];
                int vertex0 = ResolveVertex(input.A, vertices, vertexIds);
                int vertex1 = ResolveVertex(input.B, vertices, vertexIds);
                int vertex2 = ResolveVertex(input.C, vertices, vertexIds);
                if (vertex0 == vertex1 || vertex1 == vertex2 || vertex2 == vertex0 ||
                    FixedVector3.Cross(input.B - input.A, input.C - input.A).SqrMagnitude == FixedScalar.Zero)
                {
                    throw new InvalidDataException($"Collision source '{sourceName}' triangle '{i}' degenerates after Fixed quantization.");
                }
                records[i] = new TriangleRecord(primitiveBase + i, vertex0, vertex1, vertex2, input);
                LinkEdge(sourceName, records, edges, i, 0, vertex0, vertex1);
                LinkEdge(sourceName, records, edges, i, 1, vertex1, vertex2);
                LinkEdge(sourceName, records, edges, i, 2, vertex2, vertex0);
            }
            for (int i = 0; i < records.Length; i++)
            {
                TriangleRecord value = records[i];
                primitives.Add(DeterministicCollisionPrimitive.Triangle(
                    value.PrimitiveId,
                    surfaceId,
                    value.Vertex0,
                    value.Vertex1,
                    value.Vertex2,
                    value.Input.A,
                    value.Input.B,
                    value.Input.C,
                    value.Adjacent0,
                    value.Adjacent1,
                    value.Adjacent2));
            }
        }

        static int ResolveVertex(
            FixedVector3 value,
            List<FixedVector3> vertices,
            Dictionary<VertexKey, int> vertexIds)
        {
            var key = new VertexKey(value);
            if (vertexIds.TryGetValue(key, out int existing))
                return existing;
            int id = vertices.Count;
            vertices.Add(value);
            vertexIds.Add(key, id);
            return id;
        }

        static void LinkEdge(
            string sourceName,
            IReadOnlyList<TriangleRecord> records,
            Dictionary<EdgeKey, EdgeReference> edges,
            int triangleIndex,
            int edgeIndex,
            int vertexA,
            int vertexB)
        {
            var key = new EdgeKey(vertexA, vertexB);
            if (!edges.TryGetValue(key, out EdgeReference existing))
            {
                edges.Add(key, new EdgeReference(triangleIndex, edgeIndex));
                return;
            }
            TriangleRecord previous = records[existing.TriangleIndex];
            if (previous.AdjacentAt(existing.EdgeIndex) >= 0)
                throw new InvalidDataException($"Collision source '{sourceName}' has a non-manifold edge '{vertexA}:{vertexB}'.");
            TriangleRecord current = records[triangleIndex];
            previous.SetAdjacent(existing.EdgeIndex, current.PrimitiveId);
            current.SetAdjacent(edgeIndex, previous.PrimitiveId);
        }

        static FixedVector3 Quantize(Vector3 value, int units)
        {
            return new FixedVector3(
                Quantize(value.x, units),
                Quantize(value.y, units),
                Quantize(value.z, units));
        }

        static FixedScalar Quantize(float value, int units)
        {
            long quantized = checked((long)Math.Round((double)value * units, MidpointRounding.AwayFromZero));
            return FixedScalar.FromRatio(quantized, units);
        }

        readonly struct TriangleInput
        {
            public TriangleInput(FixedVector3 a, FixedVector3 b, FixedVector3 c)
            {
                A = a;
                B = b;
                C = c;
            }

            public FixedVector3 A { get; }
            public FixedVector3 B { get; }
            public FixedVector3 C { get; }
        }

        readonly struct ColliderSourceRecord
        {
            public ColliderSourceRecord(DeterministicCollisionSurfaceAuthoring source, Collider collider)
            {
                Source = source;
                Collider = collider;
            }

            public DeterministicCollisionSurfaceAuthoring Source { get; }
            public Collider Collider { get; }
        }

        sealed class TriangleRecord
        {
            readonly int[] m_Adjacent = { -1, -1, -1 };

            public TriangleRecord(int primitiveId, int vertex0, int vertex1, int vertex2, TriangleInput input)
            {
                PrimitiveId = primitiveId;
                Vertex0 = vertex0;
                Vertex1 = vertex1;
                Vertex2 = vertex2;
                Input = input;
            }

            public int PrimitiveId { get; }
            public int Vertex0 { get; }
            public int Vertex1 { get; }
            public int Vertex2 { get; }
            public TriangleInput Input { get; }
            public int Adjacent0 => m_Adjacent[0];
            public int Adjacent1 => m_Adjacent[1];
            public int Adjacent2 => m_Adjacent[2];
            public int AdjacentAt(int edgeIndex) => m_Adjacent[edgeIndex];
            public void SetAdjacent(int edgeIndex, int primitiveId) => m_Adjacent[edgeIndex] = primitiveId;
        }

        readonly struct EdgeReference
        {
            public EdgeReference(int triangleIndex, int edgeIndex)
            {
                TriangleIndex = triangleIndex;
                EdgeIndex = edgeIndex;
            }

            public int TriangleIndex { get; }
            public int EdgeIndex { get; }
        }

        readonly struct EdgeKey : IEquatable<EdgeKey>
        {
            public EdgeKey(int left, int right)
            {
                if (left <= right)
                {
                    Left = left;
                    Right = right;
                }
                else
                {
                    Left = right;
                    Right = left;
                }
            }

            public int Left { get; }
            public int Right { get; }
            public bool Equals(EdgeKey other) => Left == other.Left && Right == other.Right;
            public override bool Equals(object obj) => obj is EdgeKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Left, Right);
        }

        readonly struct VertexKey : IEquatable<VertexKey>
        {
            public VertexKey(FixedVector3 value)
            {
                X = value.X.Raw;
                Y = value.Y.Raw;
                Z = value.Z.Raw;
            }

            public long X { get; }
            public long Y { get; }
            public long Z { get; }
            public bool Equals(VertexKey other) => X == other.X && Y == other.Y && Z == other.Z;
            public override bool Equals(object obj) => obj is VertexKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        }
    }
}
