using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterFootGroundGeometrySource
    {
        readonly struct Face
        {
            internal Face(Vector3 a, Vector3 b, Vector3 c)
            {
                A = a;
                B = b;
                C = c;
                Vector3 minimum = Vector3.Min(a, Vector3.Min(b, c));
                Vector3 maximum = Vector3.Max(a, Vector3.Max(b, c));
                Bounds = new Bounds((minimum + maximum) * 0.5f, maximum - minimum);
            }

            internal Vector3 A { get; }
            internal Vector3 B { get; }
            internal Vector3 C { get; }
            internal Bounds Bounds { get; }
        }

        sealed class Surface
        {
            internal readonly Collider Collider;
            internal readonly Matrix4x4 Matrix;
            internal readonly Face[] Faces;
            internal readonly CharacterFootGroundSurfaceState State;
            internal readonly Bounds Bounds;
            internal readonly Mesh Mesh;
            internal readonly Vector3 BoxCenter;
            internal readonly Vector3 BoxSize;

            internal Surface(Collider collider)
            {
                Collider = collider;
                Matrix = collider.transform.localToWorldMatrix;
                Bounds = collider.bounds;
                Faces = Array.Empty<Face>();
                State = CharacterFootGroundSurfaceState.UnsupportedGeometry;
                if (collider.attachedRigidbody || !ValidTransform(Matrix))
                    return;
                if (collider is BoxCollider box)
                {
                    BoxCenter = box.center;
                    BoxSize = box.size;
                    if (!Finite(BoxCenter) || !Finite(BoxSize) ||
                        BoxSize.x <= 0f || BoxSize.y <= 0f || BoxSize.z <= 0f)
                    {
                        State = CharacterFootGroundSurfaceState.GeometryUnavailable;
                        return;
                    }
                    Faces = BuildBox(Matrix, BoxCenter, BoxSize);
                    Bounds = Enclose(Faces);
                    State = CharacterFootGroundSurfaceState.Ready;
                }
                else if (collider is MeshCollider meshCollider)
                {
                    if (meshCollider.convex || Matrix.determinant <= 0f)
                        return;
                    Mesh = meshCollider.sharedMesh;
                    if (!Mesh || !Mesh.isReadable)
                    {
                        State = CharacterFootGroundSurfaceState.GeometryUnavailable;
                        return;
                    }
                    Vector3[] vertices = Mesh.vertices;
                    int[] indices = Mesh.triangles;
                    if (indices.Length == 0 || indices.Length % 3 != 0)
                    {
                        State = CharacterFootGroundSurfaceState.GeometryUnavailable;
                        return;
                    }
                    var faces = new Face[indices.Length / 3];
                    for (int i = 0; i < faces.Length; i++)
                    {
                        int a = indices[i * 3];
                        int b = indices[i * 3 + 1];
                        int c = indices[i * 3 + 2];
                        if ((uint)a >= (uint)vertices.Length ||
                            (uint)b >= (uint)vertices.Length ||
                            (uint)c >= (uint)vertices.Length)
                        {
                            State = CharacterFootGroundSurfaceState.GeometryUnavailable;
                            return;
                        }
                        Vector3 first = Matrix.MultiplyPoint3x4(vertices[a]);
                        Vector3 second = Matrix.MultiplyPoint3x4(vertices[b]);
                        Vector3 third = Matrix.MultiplyPoint3x4(vertices[c]);
                        if (!Finite(first) || !Finite(second) || !Finite(third))
                        {
                            State = CharacterFootGroundSurfaceState.GeometryUnavailable;
                            return;
                        }
                        faces[i] = new Face(first, second, third);
                    }
                    Faces = faces;
                    Bounds = Enclose(Faces);
                    State = CharacterFootGroundSurfaceState.Ready;
                }
            }

            internal bool IsCurrent =>
                Collider && !Collider.attachedRigidbody &&
                Collider.transform.localToWorldMatrix.Equals(Matrix) &&
                (!(Collider is BoxCollider box) ||
                 box.center.Equals(BoxCenter) && box.size.Equals(BoxSize)) &&
                (!(Collider is MeshCollider meshCollider) ||
                 !meshCollider.convex && meshCollider.sharedMesh == Mesh);

            static Bounds Enclose(Face[] faces)
            {
                Bounds result = faces[0].Bounds;
                for (int i = 1; i < faces.Length; i++)
                    result.Encapsulate(faces[i].Bounds);
                return result;
            }

            static Face[] BuildBox(Matrix4x4 matrix, Vector3 center, Vector3 size)
            {
                var vertices = new Vector3[8];
                Vector3 half = size * 0.5f;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = matrix.MultiplyPoint3x4(center + new Vector3(
                        (i & 1) == 0 ? -half.x : half.x,
                        (i & 2) == 0 ? -half.y : half.y,
                        (i & 4) == 0 ? -half.z : half.z));
                }
                int[] indices =
                {
                    0, 4, 6, 0, 6, 2,
                    1, 3, 7, 1, 7, 5,
                    0, 1, 5, 0, 5, 4,
                    2, 6, 7, 2, 7, 3,
                    0, 2, 3, 0, 3, 1,
                    4, 5, 7, 4, 7, 6
                };
                var result = new Face[12];
                Vector3 worldCenter = matrix.MultiplyPoint3x4(center);
                for (int i = 0; i < result.Length; i++)
                {
                    Vector3 a = vertices[indices[i * 3]];
                    Vector3 b = vertices[indices[i * 3 + 1]];
                    Vector3 c = vertices[indices[i * 3 + 2]];
                    Vector3 normal = Vector3.Cross(b - a, c - a);
                    if (Vector3.Dot(normal, (a + b + c) / 3f - worldCenter) < 0f)
                    {
                        Vector3 swap = b;
                        b = c;
                        c = swap;
                    }
                    result[i] = new Face(a, b, c);
                }
                return result;
            }

            static bool ValidTransform(Matrix4x4 matrix)
            {
                Vector3 x = matrix.MultiplyVector(Vector3.right);
                Vector3 y = matrix.MultiplyVector(Vector3.up);
                Vector3 z = matrix.MultiplyVector(Vector3.forward);
                if (!Finite(matrix.MultiplyPoint3x4(Vector3.zero)) ||
                    !Finite(x) || !Finite(y) || !Finite(z) ||
                    x.sqrMagnitude <= 0.00000001f ||
                    y.sqrMagnitude <= 0.00000001f ||
                    z.sqrMagnitude <= 0.00000001f)
                    return false;
                x.Normalize();
                y.Normalize();
                z.Normalize();
                return Mathf.Abs(Vector3.Dot(x, y)) <= 0.0001f &&
                       Mathf.Abs(Vector3.Dot(x, z)) <= 0.0001f &&
                       Mathf.Abs(Vector3.Dot(y, z)) <= 0.0001f;
            }

            static bool Finite(Vector3 value) =>
                float.IsFinite(value.x) && float.IsFinite(value.y) &&
                float.IsFinite(value.z);
        }

        readonly Dictionary<int, Surface> m_Surfaces = new Dictionary<int, Surface>();
        readonly CharacterFootGroundSurfaceProjector m_Projector =
            new CharacterFootGroundSurfaceProjector();

        internal CharacterFootGroundGeometrySource(
            PhysicsScene physicsScene,
            CharacterFootPlacementPoseRig rig)
        {
            Scene ownerScene = rig.World.gameObject.scene;
            Prepare(ownerScene, physicsScene, rig);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene != ownerScene)
                    Prepare(scene, physicsScene, rig);
            }
        }

        void Prepare(
            Scene scene,
            PhysicsScene physicsScene,
            CharacterFootPlacementPoseRig rig)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                !scene.GetPhysicsScene().Equals(physicsScene))
                return;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Collider[] colliders = roots[i].GetComponentsInChildren<Collider>(true);
                for (int j = 0; j < colliders.Length; j++)
                {
                    Collider collider = colliders[j];
                    if (!collider || collider.isTrigger || rig.IsSelfCollider(collider))
                        continue;
                    int identity = collider.GetInstanceID();
                    if (!m_Surfaces.ContainsKey(identity))
                        m_Surfaces.Add(identity, new Surface(collider));
                }
            }
        }

        internal CharacterFootGroundSurfaceState Validate(int surfaceIdentity)
        {
            if (!m_Surfaces.TryGetValue(surfaceIdentity, out Surface surface))
                return CharacterFootGroundSurfaceState.GeometryUnavailable;
            if (surface.State != CharacterFootGroundSurfaceState.Ready)
                return surface.State;
            return surface.IsCurrent
                ? CharacterFootGroundSurfaceState.Ready
                : CharacterFootGroundSurfaceState.GeometryChanged;
        }

        internal CharacterFootGroundSurfaceState Query(
            in CharacterFootGroundPathQueryRequest query,
            CharacterFootGroundSurfacePage output)
        {
            Vector3 translation = query.Direction.normalized * query.MaximumDistance;
            Vector3 minimum = Vector3.Min(
                Vector3.Min(query.AxisStart, query.AxisEnd),
                Vector3.Min(query.AxisStart + translation, query.AxisEnd + translation));
            Vector3 maximum = Vector3.Max(
                Vector3.Max(query.AxisStart, query.AxisEnd),
                Vector3.Max(query.AxisStart + translation, query.AxisEnd + translation));
            var queryBounds = new Bounds(
                (minimum + maximum) * 0.5f,
                maximum - minimum + Vector3.one * (query.Radius * 2f));
            foreach (KeyValuePair<int, Surface> item in m_Surfaces)
            {
                Surface surface = item.Value;
                Collider collider = surface.Collider;
                if (!collider || !collider.enabled || !collider.gameObject.activeInHierarchy ||
                    collider.isTrigger || (query.LayerMask & (1 << collider.gameObject.layer)) == 0 ||
                    !surface.Bounds.Intersects(queryBounds))
                    continue;
                CharacterFootGroundSurfaceState state = Validate(item.Key);
                if (state != CharacterFootGroundSurfaceState.Ready)
                    return state;
                bool boundedFaces = collider is MeshCollider;
                for (int i = 0; i < surface.Faces.Length; i++)
                {
                    Face face = surface.Faces[i];
                    if (boundedFaces && !face.Bounds.Intersects(queryBounds))
                        continue;
                    if (!m_Projector.TryAppend(
                            item.Key, i, face.A, face.B, face.C,
                            boundedFaces, in query, output))
                    {
                        return output.Count >= output.Capacity
                            ? CharacterFootGroundSurfaceState.CapacityExceeded
                            : CharacterFootGroundSurfaceState.GeometryUnavailable;
                    }
                }
            }
            return CharacterFootGroundSurfaceState.Ready;
        }
    }
}
