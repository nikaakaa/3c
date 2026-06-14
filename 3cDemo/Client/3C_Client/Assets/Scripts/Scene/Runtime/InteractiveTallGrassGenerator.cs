using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonScene
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class InteractiveTallGrassGenerator : MonoBehaviour
    {
        const string GeneratedMeshName = "InteractiveTallGrassGeneratedMesh";

        [SerializeField] InteractiveTallGrassProfile profile;
        [SerializeField] Material grassMaterial;

        Mesh generatedMesh;

        public InteractiveTallGrassProfile Profile
        {
            get => profile;
            set => profile = value;
        }

        public Material GrassMaterial
        {
            get => grassMaterial;
            set => grassMaterial = value;
        }

        public InteractiveTallGrassSettings CurrentSettings => profile != null
            ? profile.NormalizedSettings
            : InteractiveTallGrassSettings.Default;

        public Mesh GeneratedMesh => generatedMesh;

        public static IReadOnlyList<InteractiveTallGrassBlade> GenerateBlades(InteractiveTallGrassSettings settings)
        {
            InteractiveTallGrassBlade[] blades = new InteractiveTallGrassBlade[settings.BladeCount];
            System.Random random = new System.Random(settings.RandomSeed);

            for (int i = 0; i < blades.Length; i++)
            {
                float x = LerpRandom(random, -settings.AreaSize.x * 0.5f, settings.AreaSize.x * 0.5f);
                float z = LerpRandom(random, -settings.AreaSize.y * 0.5f, settings.AreaSize.y * 0.5f);
                float height = LerpRandom(random, settings.MinHeight, settings.MaxHeight);
                float width = LerpRandom(random, settings.MinWidth, settings.MaxWidth);
                float yaw = LerpRandom(random, 0f, 180f);
                blades[i] = new InteractiveTallGrassBlade(new Vector3(x, 0f, z), height, width, yaw);
            }

            return blades;
        }

        public static Mesh BuildMesh(IReadOnlyList<InteractiveTallGrassBlade> blades)
        {
            Mesh mesh = new Mesh
            {
                name = GeneratedMeshName
            };

            Vector3[] vertices = new Vector3[blades.Count * 8];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[blades.Count * 12];
            Color[] colors = new Color[vertices.Length];

            for (int i = 0; i < blades.Count; i++)
            {
                WriteCrossBlade(blades[i], i * 8, i * 12, vertices, uvs, triangles, colors);
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        public void Rebuild()
        {
            EnsureComponents();
            ReplaceGeneratedMesh(BuildMesh(GenerateBlades(CurrentSettings)));
            ApplyMaterialSettings();
        }

        public void ApplyMaterialSettings()
        {
            if (grassMaterial == null)
                return;

            InteractiveTallGrassSettings settings = CurrentSettings;
            grassMaterial.SetColor("_BaseColor", settings.BaseColor);
            grassMaterial.SetColor("_TopColor", settings.TopColor);
            grassMaterial.SetFloat("_ToonStrength", settings.ToonStrength);
            grassMaterial.SetFloat("_WindStrength", settings.WindStrength);
            grassMaterial.SetFloat("_WindFrequency", settings.WindFrequency);
            grassMaterial.SetVector("_WindDirection", new Vector4(settings.WindDirection.x, settings.WindDirection.y, 0f, 0f));
            grassMaterial.SetFloat("_InteractionRadius", settings.InteractionRadius);
            grassMaterial.SetFloat("_BendStrength", settings.BendStrength);
        }

        void OnEnable()
        {
            Rebuild();
        }

        void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= RebuildIfValid;
            UnityEditor.EditorApplication.delayCall += RebuildIfValid;
#endif
        }

        void OnDestroy()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= RebuildIfValid;
#endif
            if (generatedMesh == null)
                return;

            if (Application.isPlaying)
                Destroy(generatedMesh);
            else
                DestroyImmediate(generatedMesh);
        }

#if UNITY_EDITOR
        void RebuildIfValid()
        {
            if (this == null)
                return;

            Rebuild();
        }
#endif

        void EnsureComponents()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();

            if (grassMaterial != null)
                meshRenderer.sharedMaterial = grassMaterial;
        }

        void ReplaceGeneratedMesh(Mesh mesh)
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
                return;

            if (generatedMesh != null)
            {
                if (Application.isPlaying)
                    Destroy(generatedMesh);
                else
                    DestroyImmediate(generatedMesh);
            }

            generatedMesh = mesh;
            meshFilter.sharedMesh = generatedMesh;
        }

        static float LerpRandom(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        static void WriteCrossBlade(
            InteractiveTallGrassBlade blade,
            int vertexStart,
            int triangleStart,
            Vector3[] vertices,
            Vector2[] uvs,
            int[] triangles,
            Color[] colors)
        {
            WriteBladePlane(blade, blade.YawDegrees, vertexStart, triangleStart, vertices, uvs, triangles, colors);
            WriteBladePlane(blade, blade.YawDegrees + 90f, vertexStart + 4, triangleStart + 6, vertices, uvs, triangles, colors);
        }

        static void WriteBladePlane(
            InteractiveTallGrassBlade blade,
            float yawDegrees,
            int vertexStart,
            int triangleStart,
            Vector3[] vertices,
            Vector2[] uvs,
            int[] triangles,
            Color[] colors)
        {
            Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            Vector3 right = rotation * Vector3.right * blade.Width * 0.5f;
            Vector3 bottom = blade.Position;
            Vector3 top = blade.Position + Vector3.up * blade.Height;

            vertices[vertexStart] = bottom - right;
            vertices[vertexStart + 1] = bottom + right;
            vertices[vertexStart + 2] = top - right * 0.22f;
            vertices[vertexStart + 3] = top + right * 0.22f;

            uvs[vertexStart] = new Vector2(0f, 0f);
            uvs[vertexStart + 1] = new Vector2(1f, 0f);
            uvs[vertexStart + 2] = new Vector2(0f, 1f);
            uvs[vertexStart + 3] = new Vector2(1f, 1f);

            colors[vertexStart] = new Color(blade.Width, blade.Height, 0f, 1f);
            colors[vertexStart + 1] = colors[vertexStart];
            colors[vertexStart + 2] = new Color(blade.Width, blade.Height, 1f, 1f);
            colors[vertexStart + 3] = colors[vertexStart + 2];

            triangles[triangleStart] = vertexStart;
            triangles[triangleStart + 1] = vertexStart + 2;
            triangles[triangleStart + 2] = vertexStart + 1;
            triangles[triangleStart + 3] = vertexStart + 1;
            triangles[triangleStart + 4] = vertexStart + 2;
            triangles[triangleStart + 5] = vertexStart + 3;
        }
    }
}
