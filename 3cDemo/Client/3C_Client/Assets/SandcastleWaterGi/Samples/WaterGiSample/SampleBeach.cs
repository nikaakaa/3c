using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SandcastleWaterGi.Samples
{
    /// <summary>
    /// One-click procedural beach. With autoBuild enabled (default) the
    /// scene builds itself as soon as the shaders are compiled, so the
    /// bundled Beach.unity shows sand + water + GI right after import.
    /// </summary>
    [ExecuteAlways]
    public class SampleBeach : MonoBehaviour
    {
        public int resolution = 256;
        public float worldSize = 60f;
        public float tideLevel = 0.35f;

        public Color drySand = new Color(0.82f, 0.76f, 0.63f);
        public Color wetSand = new Color(0.55f, 0.48f, 0.36f);
        public Color underwaterSand = new Color(0.40f, 0.52f, 0.48f);

        public Vector3 sunDirection = new Vector3(0.5f, 1f, 0.3f);
        public bool autoBuild = true;

        public Vector2Int waterResolution = new Vector2Int(128, 128);

#if UNITY_EDITOR
        void OnEnable()
        {
            if (autoBuild && !Application.isPlaying)
                EditorApplication.delayCall += TryAutoBuild;
        }

        void TryAutoBuild()
        {
            if (this == null) return;
            if (transform.Find("Terrain") != null && transform.Find("Water") != null) return;
            if (Shader.Find("SandcastleWaterGi/GiTerrain") == null || Shader.Find("Lit/Water") == null)
            {
                // shaders still importing; retry on the next editor tick
                EditorApplication.delayCall += TryAutoBuild;
                return;
            }
            Build();
        }
#endif

        [ContextMenu("Build Beach")]
        public void Build()
        {
            var heightmap = GenerateHeightmap();

            // ---- GI ----
            var giGo = GetOrCreate("GiLighting");
            var gi = giGo.GetComponent<GiLighting>();
            if (gi == null) gi = giGo.AddComponent<GiLighting>();
            gi.heightmap = heightmap;
            gi.worldSize = new Vector2(worldSize, worldSize);
            gi.originOffset = new Vector2(-worldSize * 0.5f, -worldSize * 0.5f);
            gi.sunDirection = sunDirection;

            // ---- water simulation ----
            var simGo = GetOrCreate("WaterSimulation");
            var sim = simGo.GetComponent<WaterSimulation>();
            if (sim == null) sim = simGo.AddComponent<WaterSimulation>();
            sim.heightmap = heightmap;
            sim.resolution = waterResolution;
            sim.worldSize = new Vector2(worldSize, worldSize);
            sim.originOffset = new Vector2(-worldSize * 0.5f, -worldSize * 0.5f);
            sim.waterLevel = tideLevel;

            // ---- sand pile simulation (Ground: collapse + wetness) ----
            var groundGo = GetOrCreate("GroundSimulation");
            var ground = groundGo.GetComponent<GroundSimulation>();
            if (ground == null) ground = groundGo.AddComponent<GroundSimulation>();
            ground.heightmap = heightmap;
            ground.resolution = new Vector2Int(resolution, resolution);
            ground.worldSize = new Vector2(worldSize, worldSize);
            ground.originOffset = new Vector2(-worldSize * 0.5f, -worldSize * 0.5f);
            ground.waterLevel = tideLevel;

#if UNITY_EDITOR
            if (gi.compute == null || sim.compute == null || ground.compute == null)
            {
                string[] giGuids = AssetDatabase.FindAssets("GiLighting t:ComputeShader");
                if (giGuids.Length > 0 && gi.compute == null)
                    gi.compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(giGuids[0]));
                string[] waterGuids = AssetDatabase.FindAssets("WaterSimulation t:ComputeShader");
                if (waterGuids.Length > 0 && sim.compute == null)
                    sim.compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(waterGuids[0]));
                string[] groundGuids = AssetDatabase.FindAssets("GroundSimulation t:ComputeShader");
                if (groundGuids.Length > 0 && ground.compute == null)
                    ground.compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(groundGuids[0]));
                if (gi.compute == null || sim.compute == null || ground.compute == null)
                    Debug.LogWarning("请把 GiLighting/WaterSimulation/GroundSimulation.compute 拖到对应组件的 Compute 槽");
            }
#endif

            // cross-coupling: water erodes sand, sand shapes the water floor
            ground.waterSimulation = sim;
            sim.groundSimulation = ground;

            // ---- terrain (rebuilt Lit/Sand shader + original textures) ----
            var terrain = GetOrCreate("Terrain", typeof(MeshFilter), typeof(MeshRenderer));
            SetupPlane(terrain, resolution);
            // pick the shader set for the active render pipeline
            bool urp = GraphicsSettings.renderPipelineAsset != null;
            var sandShader = Shader.Find(urp ? "SandcastleWaterGi/LitSandURP" : "SandcastleWaterGi/LitSand");
            if (sandShader == null) sandShader = Shader.Find(urp ? "SandcastleWaterGi/GiTerrainURP" : "SandcastleWaterGi/GiTerrain");
            var terrainMat = new Material(sandShader);
#if UNITY_EDITOR
            Texture2D sandTex = FindTexture("Sand");
            Texture2D causticsTex = FindTexture("Caustics");
            if (sandTex != null) terrainMat.SetTexture("sandMap", sandTex);
            if (causticsTex != null) terrainMat.SetTexture("causticsMap", causticsTex);
#endif
            terrainMat.SetFloat("_tideLevel", tideLevel);
            terrain.GetComponent<MeshRenderer>().sharedMaterial = terrainMat;

            // ---- water (dense mesh so the simulated waves show) ----
            var water = GetOrCreate("Water", typeof(MeshFilter), typeof(MeshRenderer));
            SetupWaterMesh(water, waterResolution.x);
            var waterShader = Shader.Find(urp ? "SandcastleWaterGi/WaterURP" : "Lit/Water");
            if (waterShader == null) waterShader = Shader.Find("Lit/Water");
            var waterMat = new Material(waterShader);
            waterMat.SetFloat("_waterLevel", tideLevel);
#if UNITY_EDITOR
            Texture2D waterNormal = FindTexture("WaterNormal2D");
            if (waterNormal != null) waterMat.SetTexture("waterNormalMap", waterNormal);
#endif
            water.GetComponent<MeshRenderer>().sharedMaterial = waterMat;

            // ---- sun light ----
            var lightGo = GetOrCreate("Sun");
            var light = lightGo.GetComponent<Light>();
            if (light == null) light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.transform.rotation = Quaternion.LookRotation(sunDirection.normalized);
        }

        RenderTexture GenerateHeightmap()
        {
            var rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            rt.Create();

            var cols = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float u = (x + 0.5f) / resolution;
                    float v = (y + 0.5f) / resolution;
                    float px = (u - 0.5f) * worldSize;
                    float pz = (v - 0.5f) * worldSize;

                    // oval sandbank with a castle dune, submerged rim
                    float d2 = (px / (worldSize * 0.30f)) * (px / (worldSize * 0.30f))
                             + (pz / (worldSize * 0.22f)) * (pz / (worldSize * 0.22f));
                    float bank = Mathf.Exp(-d2 * 1.7f);
                    float castle = Mathf.Exp(-((px - worldSize * 0.05f) * 0.35f) * ((px - worldSize * 0.05f) * 0.35f)
                                            - (pz * 0.5f) * (pz * 0.5f)) * 0.5f;
                    float h = tideLevel
                            + bank * 0.75f
                            + castle * 0.4f
                            + 0.05f * Mathf.Sin(px * 0.7f + pz * 0.5f)
                            + 0.04f * Mathf.Sin(px * 1.9f) * Mathf.Sin(pz * 1.4f)
                            - 0.05f * Mathf.Sin(px * 0.23f - pz * 0.31f);

                    float depth = Mathf.Max(0f, tideLevel - h);
                    Color c = Color.Lerp(wetSand, drySand, Mathf.Clamp01((h - tideLevel) / 0.5f));
                    c = Color.Lerp(c, underwaterSand, Mathf.Clamp01(depth / 0.3f));
                    c *= 0.85f + 0.3f * Mathf.PerlinNoise(px * 0.05f, pz * 0.05f);

                    cols[y * resolution + x] = new Color(h, c.r, c.g, c.b);
                }
            }

            var tex = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false);
            tex.SetPixels(cols);
            tex.Apply();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(tex, rt);
            RenderTexture.active = prev;
            DestroyImmediate(tex);
            return rt;
        }

#if UNITY_EDITOR
        static Texture2D FindTexture(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Texture2D");
            foreach (string g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (p.Contains("SandcastleWaterGi"))
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            }
            return null;
        }
#endif

        GameObject GetOrCreate(string name, params System.Type[] components)
        {
            var child = transform.Find(name);
            GameObject go;
            if (child != null)
            {
                go = child.gameObject;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(transform, false);
            }
            foreach (var t in components)
                if (go.GetComponent(t) == null)
                    go.AddComponent(t);
            return go;
        }

        void SetupPlane(GameObject go, int res)
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf.sharedMesh == null)
            {
                var prim = GameObject.CreatePrimitive(PrimitiveType.Plane);
                mf.sharedMesh = prim.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(prim);
            }
            go.transform.localScale = new Vector3(worldSize / 10f, 1f, worldSize / 10f);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
        }

        void SetupWaterMesh(GameObject go, int res)
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf.sharedMesh == null)
            {
                // dense grid spanning the world, vertices at y=0
                var mesh = new Mesh { name = "WaterGrid" };
                int n = res + 1;
                var verts = new Vector3[n * n];
                var uv = new Vector2[n * n];
                var tris = new int[res * res * 6];
                for (int z = 0; z < n; z++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        verts[z * n + x] = new Vector3(x / (float)res - 0.5f, 0f, z / (float)res - 0.5f);
                        uv[z * n + x] = new Vector2(x / (float)res, z / (float)res);
                    }
                }
                int t = 0;
                for (int z = 0; z < res; z++)
                {
                    for (int x = 0; x < res; x++)
                    {
                        int a = z * n + x;
                        tris[t++] = a; tris[t++] = a + n; tris[t++] = a + 1;
                        tris[t++] = a + 1; tris[t++] = a + n; tris[t++] = a + n + 1;
                    }
                }
                mesh.vertices = verts;
                mesh.uv = uv;
                mesh.triangles = tris;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mf.sharedMesh = mesh;
            }
            go.transform.localScale = new Vector3(worldSize, 1f, worldSize);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
        }
    }
}
