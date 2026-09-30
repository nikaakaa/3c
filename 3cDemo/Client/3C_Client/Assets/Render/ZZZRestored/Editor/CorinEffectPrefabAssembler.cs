using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZZZ.Rendering.Restored.Editor
{
    public static class CorinEffectPrefabAssembler
    {
        const string HierarchyPath =
            @"D:\Unity_Project_1\3C\Tools\Rendering\CorinRenderData\corin-prefab-hierarchy.json";
        const string ParticleDataPath =
            @"D:\ZZZ_Dump\output\corin_replication\corin-particle-system-unity.json";
        const string RendererMaterialMapPath =
            @"D:\ZZZ_Dump\output\corin_replication\corin-renderer-material-map.json";
        const string RendererMeshMapPath =
            @"D:\ZZZ_Dump\output\corin_replication\corin-renderer-mesh-map-v2.json";
        const string RendererDataPath =
            @"D:\ZZZ_Dump\output\corin_replication\corin-particle-renderer-unity.json";
        const string PrefabRoot = "Assets/AssetArt/Effect/ZZZ/Corin/Prefabs";
        const string MeshRoot = "Assets/AssetArt/Effect/ZZZ/Corin/Meshes";
        const string MaterialSetPath = "Assets/AssetArt/Effect/ZZZ/Corin/CorinEffectMaterialSet.asset";

        sealed class RendererMeshSlot
        {
            public string BlockId;
            public string Field;
            public string MeshName;
        }

        [MenuItem("Tools/ZZZ/Restored/Rebuild Corin Effect Prefab Hierarchy")]
        public static void Rebuild()
        {
            EnsureFolder(PrefabRoot);
            EnsureFolder(MeshRoot);
            ImportMeshes();
            var document = JObject.Parse(File.ReadAllText(HierarchyPath, Encoding.UTF8));
            if ((string)document["schema"] != "zzz-corin-prefab-hierarchy/v1")
                throw new InvalidOperationException("特效 Prefab 层级来源合同版本不匹配。");
            var particleLookup = new Dictionary<string, JObject>(StringComparer.Ordinal);
            if (File.Exists(ParticleDataPath))
            {
                var particleData = JObject.Parse(File.ReadAllText(ParticleDataPath, Encoding.UTF8));
                foreach (var record in (JArray)particleData["records"])
                {
                    var key = (string)record["cab"] + ":" + (long)record["pathId"];
                    particleLookup.Add(key, (JObject)record);
                }
            }
            var materialSet = AssetDatabase.LoadAssetAtPath<CorinEffectMaterialSet>(MaterialSetPath);
            var rendererMaterialMap = new Dictionary<string, string[]>(StringComparer.Ordinal);
            if (File.Exists(RendererMaterialMapPath))
            {
                var mapData = JObject.Parse(File.ReadAllText(RendererMaterialMapPath, Encoding.UTF8));
                foreach (var property in (JObject)mapData["entries"])
                {
                    if (property.Value is JArray sourceKeys)
                        rendererMaterialMap[property.Key] = sourceKeys.Values<string>().ToArray();
                }
            }
            var rendererLookup = new Dictionary<string, JObject>(StringComparer.Ordinal);
            if (File.Exists(RendererDataPath))
            {
                var rendererData = JObject.Parse(File.ReadAllText(RendererDataPath, Encoding.UTF8));
                foreach (var record in (JArray)rendererData["records"])
                {
                    var key = (string)record["cab"] + ":" + (long)record["pathId"];
                    rendererLookup.Add(key, (JObject)record);
                }
            }
            var meshLookup = new Dictionary<(string BlockId, string MeshName), UnityEngine.Mesh>();
            foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { MeshRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mesh = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(path);
                var blockId = Path.GetFileName(Path.GetDirectoryName(path));
                if (mesh != null && blockId != Path.GetFileName(MeshRoot))
                    meshLookup[(blockId, SourceMeshName(mesh.name))] = mesh;
            }
            var rendererMeshMap = new Dictionary<string, List<RendererMeshSlot>>(StringComparer.Ordinal);
            if (File.Exists(RendererMeshMapPath))
            {
                var meshMapData = JObject.Parse(File.ReadAllText(RendererMeshMapPath, Encoding.UTF8));
                if ((string)meshMapData["schema"] != "zzz-corin-renderer-mesh-map/v2")
                    throw new InvalidOperationException("特效 Mesh 槽位来源合同版本不匹配。");
                foreach (var property in (JObject)meshMapData["entries"])
                {
                    var slots = new List<RendererMeshSlot>();
                    foreach (var slotProperty in (JObject)property.Value["slots"])
                    {
                        var slot = (JObject)slotProperty.Value;
                        if ((string)slot["status"] != "external")
                            continue;
                        slots.Add(new RendererMeshSlot
                        {
                            BlockId = (string)slot["blockId"],
                            Field = slotProperty.Key,
                            MeshName = (string)slot["meshName"]
                        });
                    }
                    rendererMeshMap[property.Key] = slots;
                }
            }
            var created = 0;
            var particleCount = 0;
            var boundMeshCount = 0;
            foreach (var record in (JArray)document["records"])
            {
                var source = (JObject)record["source"];
                var rootName = (string)source["name"];
                var rootCab = (string)source["cab"];
                var prefabPath = PrefabRoot + "/" + rootName + ".prefab";
                var root = BuildNode((JObject)record["root"], rootCab, particleLookup,
                    rendererLookup, rendererMeshMap, meshLookup,
                    rendererMaterialMap, materialSet, ref particleCount, ref boundMeshCount);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
                created++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"可琳特效 Prefab 层级已重建：{created} 个 Prefab、{particleCount} 个 ParticleSystem、{boundMeshCount} 个 Mesh 引用 -> {PrefabRoot}");
        }

        static GameObject BuildNode(JObject node, string rootCab,
            Dictionary<string, JObject> particleLookup,
            Dictionary<string, JObject> rendererLookup,
            Dictionary<string, List<RendererMeshSlot>> rendererMeshMap,
            Dictionary<(string BlockId, string MeshName), UnityEngine.Mesh> meshLookup,
            Dictionary<string, string[]> rendererMaterialMap, CorinEffectMaterialSet materialSet,
            ref int particleCount, ref int boundMeshCount)
        {
            var name = (string)node["name"];
            var go = new GameObject(name);
            go.layer = (int)node["layer"];
            go.SetActive((bool)node["isActive"]);
            var transform = go.transform;
            var t = (JObject)node["transform"];
            if (t != null)
            {
                transform.localPosition = Vector3Value((JObject)t["localPosition"]);
                transform.localRotation = QuaternionValue((JObject)t["localRotation"]);
                transform.localScale = Vector3Value((JObject)t["localScale"]);
            }
            foreach (var component in (JArray)node["components"])
            {
                var type = (string)component["type"];
                var key = rootCab + ":" + (long)component["pathId"];
                if (type == "ParticleSystem")
                {
                    if (particleLookup.TryGetValue(key, out var data))
                    {
                        ApplyParticleSystem(go, data);
                        var hasRenderer = ((JArray)node["components"]).Any(item =>
                            (string)item["type"] == "ParticleSystemRenderer");
                        if (!hasRenderer)
                            Object.DestroyImmediate(go.GetComponent<ParticleSystemRenderer>());
                        particleCount++;
                    }
                }
                else if (type == "ParticleSystemRenderer")
                {
                    var renderer = go.GetComponent<ParticleSystemRenderer>();
                    if (renderer == null)
                        renderer = go.AddComponent<ParticleSystemRenderer>();
                    if (rendererLookup.TryGetValue(key, out var rd))
                        ApplyRendererProperties(renderer, rd);
                    if (rendererMeshMap.TryGetValue(key, out var meshSlots))
                    {
                        foreach (var meshSlot in meshSlots)
                        {
                            var serializedRenderer = new SerializedObject(renderer);
                            var meshProperty = serializedRenderer.FindProperty(MeshPropertyField(meshSlot.Field));
                            var mesh = meshLookup[(meshSlot.BlockId, meshSlot.MeshName)];
                            meshProperty.objectReferenceValue = mesh;
                            serializedRenderer.ApplyModifiedProperties();
                            boundMeshCount++;
                        }
                    }
                    if (rendererMaterialMap.TryGetValue(key, out var sourceKeys) && materialSet != null)
                    {
                        var materials = sourceKeys
                            .Select(sourceKey => materialSet.Find(sourceKey))
                            .Where(material => material != null)
                            .ToArray();
                        if (materials.Length > 0)
                            renderer.sharedMaterials = materials;
                    }
                }
            }
            foreach (var childToken in (JArray)node["children"])
            {
                var child = BuildNode((JObject)childToken, rootCab, particleLookup,
                    rendererLookup, rendererMeshMap, meshLookup,
                    rendererMaterialMap, materialSet, ref particleCount, ref boundMeshCount);
                child.transform.SetParent(transform, false);
            }
            return go;
        }

        static void ImportMeshes()
        {
            var sourceRoot = @"D:\ZZZ_Dump\work\corin_render_dependencies_v1\Mesh";
            foreach (var blockDir in Directory.GetDirectories(sourceRoot))
            {
                var meshDir = Path.Combine(blockDir, "Mesh");
                if (!Directory.Exists(meshDir))
                    continue;
                foreach (var objFile in Directory.GetFiles(meshDir, "*.obj"))
                {
                    var meshName = Path.GetFileNameWithoutExtension(objFile);
                    var destPath = MeshRoot + "/" + Path.GetFileName(blockDir) + "/" + meshName + ".obj";
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                    if (!File.Exists(destPath))
                        File.Copy(objFile, destPath);
                }
            }
            AssetDatabase.Refresh();
        }

        static void ApplyParticleSystem(GameObject go, JObject data)
        {
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            var m = (JObject)data["main"];
            main.duration = (float)m["duration"];
            main.loop = (bool)m["looping"];
            main.prewarm = (bool)m["prewarm"];
            ApplyMinMaxCurve(m["startDelay"], value => main.startDelay = value);
            ApplyMinMaxCurve(m["startLifetime"], value => main.startLifetime = value);
            ApplyMinMaxCurve(m["startSpeed"], value => main.startSpeed = value);
            main.startSize3D = (bool)m["startSize3D"];
            ApplyMinMaxCurve(m["startSize"], value => main.startSize = value);
            ApplyMinMaxCurve(m["startSizeY"], value => main.startSizeY = value);
            ApplyMinMaxCurve(m["startSizeZ"], value => main.startSizeZ = value);
            main.startRotation3D = (bool)m["startRotation3D"];
            ApplyMinMaxCurve(m["startRotation"], value => main.startRotation = value);
            ApplyMinMaxCurve(m["startRotationX"], value => main.startRotationX = value);
            ApplyMinMaxCurve(m["startRotationY"], value => main.startRotationY = value);
            ApplyMinMaxCurve(m["startRotationZ"], value => main.startRotationZ = value);
            main.flipRotation = (float)m["randomizeRotationDirection"];
            ApplyMinMaxGradient(m["startColor"], value => main.startColor = value);
            ApplyMinMaxCurve(m["gravityModifier"], value => main.gravityModifier = value);
            main.maxParticles = (int)m["maxParticles"];
            main.simulationSpeed = (float)m["simulationSpeed"];
            main.scalingMode = (ParticleSystemScalingMode)(int)m["scalingMode"];
            main.playOnAwake = (bool)m["playOnAwake"];
            ps.useAutoRandomSeed = (bool)m["autoRandomSeed"];
            if (!(bool)m["autoRandomSeed"])
                ps.randomSeed = (uint)(int)m["randomSeed"];

            var emission = ps.emission;
            var e = (JObject)data["emission"];
            emission.enabled = (bool)e["enabled"];
            ApplyMinMaxCurve(e["rateOverTime"], value => emission.rateOverTime = value);
            ApplyMinMaxCurve(e["rateOverDistance"], value => emission.rateOverDistance = value);
            var bursts = (JArray)e["bursts"];
            emission.burstCount = bursts.Count;
            for (var i = 0; i < bursts.Count; i++)
            {
                var burst = (JObject)bursts[i];
                var uBurst = new ParticleSystem.Burst((float)burst["time"], 1)
                {
                    probability = (float)burst["probability"]
                };
                uBurst.count = CurveFromJson(burst["count"]);
                emission.SetBurst(i, uBurst);
            }

            var shape = ps.shape;
            var s = (JObject)data["shape"];
            shape.enabled = (bool)s["enabled"];
            shape.shapeType = (ParticleSystemShapeType)(int)s["shapeType"];
            shape.radius = (float)s["radius"];
            shape.angle = (float)s["angle"];
            shape.arc = (float)s["arc"];
            shape.radiusThickness = (float)s["radiusThickness"];
            shape.randomDirectionAmount = (float)s["randomDirectionAmount"];
            shape.sphericalDirectionAmount = (float)s["sphericalDirectionAmount"];
            shape.alignToDirection = (bool)s["alignToDirection"];

            var colorOverLifetime = ps.colorOverLifetime;
            var col = (JObject)data["colorOverLifetime"];
            colorOverLifetime.enabled = (bool)col["enabled"];
            ApplyMinMaxGradient(col["color"], value => colorOverLifetime.color = value);

            var sizeOverLifetime = ps.sizeOverLifetime;
            var sol = (JObject)data["sizeOverLifetime"];
            sizeOverLifetime.enabled = (bool)sol["enabled"];
            sizeOverLifetime.separateAxes = (bool)sol["separateAxes"];
            ApplyMinMaxCurve(sol["size"], value => sizeOverLifetime.size = value);
            ApplyMinMaxCurve(sol["y"], value => sizeOverLifetime.y = value);
            ApplyMinMaxCurve(sol["z"], value => sizeOverLifetime.z = value);

            var rotationOverLifetime = ps.rotationOverLifetime;
            var rol = (JObject)data["rotationOverLifetime"];
            rotationOverLifetime.enabled = (bool)rol["enabled"];
            rotationOverLifetime.separateAxes = (bool)rol["separateAxes"];
            ApplyMinMaxCurve(rol["z"], value => rotationOverLifetime.z = value);
            ApplyMinMaxCurve(rol["x"], value => rotationOverLifetime.x = value);
            ApplyMinMaxCurve(rol["y"], value => rotationOverLifetime.y = value);

            var velocityOverLifetime = ps.velocityOverLifetime;
            var vol = (JObject)data["velocityOverLifetime"];
            velocityOverLifetime.enabled = (bool)vol["enabled"];
            velocityOverLifetime.space = (ParticleSystemSimulationSpace)(int)vol["space"];
            ApplyMinMaxCurve(vol["x"], value => velocityOverLifetime.x = value);
            ApplyMinMaxCurve(vol["y"], value => velocityOverLifetime.y = value);
            ApplyMinMaxCurve(vol["z"], value => velocityOverLifetime.z = value);

            var limitVelocity = ps.limitVelocityOverLifetime;
            var lvm = (JObject)data["limitVelocityOverLifetime"];
            limitVelocity.enabled = (bool)lvm["enabled"];
            limitVelocity.separateAxes = (bool)lvm["separateAxes"];
            limitVelocity.space = (ParticleSystemSimulationSpace)(int)lvm["space"];
            ApplyMinMaxCurve(lvm["limit"], value => limitVelocity.limit = value);
            ApplyMinMaxCurve(lvm["limitX"], value => limitVelocity.limitX = value);
            ApplyMinMaxCurve(lvm["limitY"], value => limitVelocity.limitY = value);
            ApplyMinMaxCurve(lvm["limitZ"], value => limitVelocity.limitZ = value);
            limitVelocity.dampen = (float)lvm["dampen"];
            limitVelocity.drag = (float)lvm["drag"];

            var forceOverLifetime = ps.forceOverLifetime;
            var fom = (JObject)data["forceOverLifetime"];
            forceOverLifetime.enabled = (bool)fom["enabled"];
            forceOverLifetime.space = (ParticleSystemSimulationSpace)(int)fom["space"];
            forceOverLifetime.randomized = (bool)fom["randomized"];
            ApplyMinMaxCurve(fom["x"], value => forceOverLifetime.x = value);
            ApplyMinMaxCurve(fom["y"], value => forceOverLifetime.y = value);
            ApplyMinMaxCurve(fom["z"], value => forceOverLifetime.z = value);

            var inheritVelocity = ps.inheritVelocity;
            var ivm = (JObject)data["inheritVelocity"];
            inheritVelocity.enabled = (bool)ivm["enabled"];
            inheritVelocity.mode = (ParticleSystemInheritVelocityMode)(int)ivm["mode"];
            ApplyMinMaxCurve(ivm["curve"], value => inheritVelocity.curve = value);

            var noise = ps.noise;
            var nm = (JObject)data["noise"];
            noise.enabled = (bool)nm["enabled"];
            noise.separateAxes = (bool)nm["separateAxes"];
            noise.damping = (bool)nm["damping"];
            noise.frequency = (float)nm["frequency"];
            ApplyMinMaxCurve(nm["strength"], value => noise.strength = value);
            ApplyMinMaxCurve(nm["strengthY"], value => noise.strengthY = value);
            ApplyMinMaxCurve(nm["strengthZ"], value => noise.strengthZ = value);
            ApplyMinMaxCurve(nm["scrollSpeed"], value => noise.scrollSpeed = value);

            var textureSheet = ps.textureSheetAnimation;
            var tsa = (JObject)data["textureSheetAnimation"];
            textureSheet.enabled = (bool)tsa["enabled"];
            textureSheet.numTilesX = (int)tsa["numTilesX"];
            textureSheet.numTilesY = (int)tsa["numTilesY"];
            textureSheet.rowIndex = (int)tsa["rowIndex"];
            ApplyMinMaxCurve(tsa["frameOverTime"], value => textureSheet.frameOverTime = value);
            ApplyMinMaxCurve(tsa["startFrame"], value => textureSheet.startFrame = value);

            var colorBySpeed = ps.colorBySpeed;
            var cbs = (JObject)data["colorBySpeed"];
            colorBySpeed.enabled = (bool)cbs["enabled"];
            ApplyMinMaxGradient(cbs["color"], value => colorBySpeed.color = value);
            var cbsRange = (JArray)cbs["range"];
            colorBySpeed.range = new Vector2((float)cbsRange[0], (float)cbsRange[1]);

            var sizeBySpeed = ps.sizeBySpeed;
            var sbs = (JObject)data["sizeBySpeed"];
            sizeBySpeed.enabled = (bool)sbs["enabled"];
            sizeBySpeed.separateAxes = (bool)sbs["separateAxes"];
            ApplyMinMaxCurve(sbs["size"], value => sizeBySpeed.size = value);
            ApplyMinMaxCurve(sbs["y"], value => sizeBySpeed.y = value);
            ApplyMinMaxCurve(sbs["z"], value => sizeBySpeed.z = value);
            var sbsRange = (JArray)sbs["range"];
            sizeBySpeed.range = new Vector2((float)sbsRange[0], (float)sbsRange[1]);

            var rotationBySpeed = ps.rotationBySpeed;
            var rbs = (JObject)data["rotationBySpeed"];
            rotationBySpeed.enabled = (bool)rbs["enabled"];
            rotationBySpeed.separateAxes = (bool)rbs["separateAxes"];
            ApplyMinMaxCurve(rbs["curve"], value => rotationBySpeed.z = value);
            var rbsRange = (JArray)rbs["range"];
            rotationBySpeed.range = new Vector2((float)rbsRange[0], (float)rbsRange[1]);

            var trails = ps.trails;
            var tm = (JObject)data["trails"];
            trails.enabled = (bool)tm["enabled"];
            trails.ratio = (float)tm["ratio"];
            ApplyMinMaxCurve(tm["lifetime"], value => trails.lifetime = value);
            trails.minVertexDistance = (float)tm["minVertexDistance"];
            trails.textureMode = (ParticleSystemTrailTextureMode)(int)tm["textureMode"];
            ApplyMinMaxCurve(tm["widthOverTrail"], value => trails.widthOverTrail = value);
            ApplyMinMaxGradient(tm["colorOverTrail"], value => trails.colorOverTrail = value);
            trails.dieWithParticles = (bool)tm["dieWithParticles"];
            trails.sizeAffectsWidth = (bool)tm["sizeAffectsWidth"];
            trails.inheritParticleColor = (bool)tm["inheritParticleColor"];
        }

        static void ApplyRendererProperties(ParticleSystemRenderer renderer, JObject data)
        {
            renderer.enabled = (bool)data["enabled"];
            renderer.renderMode = (ParticleSystemRenderMode)(int)data["renderMode"];
            renderer.sortMode = (ParticleSystemSortMode)(int)data["sortMode"];
            renderer.lengthScale = (float)data["lengthScale"];
            renderer.velocityScale = (float)data["velocityScale"];
            renderer.cameraVelocityScale = (float)data["cameraVelocityScale"];
            renderer.normalDirection = (float)data["normalDirection"];
            renderer.sortingFudge = (float)data["sortingFudge"];
            renderer.minParticleSize = (float)data["minParticleSize"];
            renderer.maxParticleSize = (float)data["maxParticleSize"];
            renderer.shadowBias = (float)data["shadowBias"];
            renderer.alignment = (ParticleSystemRenderSpace)(int)data["renderAlignment"];
            var pivot = (JArray)data["pivot"];
            renderer.pivot = new Vector3((float)pivot[0], (float)pivot[1], (float)pivot[2]);
            var flip = (JArray)data["flip"];
            renderer.flip = new Vector3((float)flip[0], (float)flip[1], (float)flip[2]);
            renderer.allowRoll = (bool)data["allowRoll"];
            var vertexStreams = (JObject)data["vertexStreams"];
            var serializedRenderer = new SerializedObject(renderer);
            serializedRenderer.FindProperty("m_UseCustomVertexStreams").boolValue =
                (bool)data["useVertexStreams"];
            var streamProperty = serializedRenderer.FindProperty("m_VertexStreams");
            streamProperty.arraySize = (int)vertexStreams["size"];
            var streamData = (string)vertexStreams["data"];
            for (var i = 0; i < streamProperty.arraySize; i++)
                streamProperty.GetArrayElementAtIndex(i).intValue =
                    Convert.ToByte(streamData.Substring(i * 2, 2), 16);
            serializedRenderer.ApplyModifiedProperties();
        }

        static string MeshPropertyField(string slotName)
        {
            if (slotName == "mesh")
                return "m_Mesh";
            return "m_" + char.ToUpperInvariant(slotName[0]) + slotName.Substring(1);
        }

        static string SourceMeshName(string importedMeshName)
        {
            const string objImporterSuffix = "_0";
            return importedMeshName.EndsWith(objImporterSuffix, StringComparison.Ordinal)
                ? importedMeshName[..^objImporterSuffix.Length]
                : importedMeshName;
        }

        static void ApplyMinMaxCurve(JToken token, Action<ParticleSystem.MinMaxCurve> setter)
        {
            if (token == null || token.Type == JTokenType.Null)
                return;
            setter(CurveFromJson(token));
        }

        static ParticleSystem.MinMaxCurve CurveFromJson(JToken token)
        {
            var obj = (JObject)token;
            var mode = (int)obj["mode"];
            var scalar = (float)obj["scalar"];
            var minScalar = (float)obj["minScalar"];
            switch (mode)
            {
                case 0:
                    return new ParticleSystem.MinMaxCurve(scalar);
                case 1:
                    return new ParticleSystem.MinMaxCurve(scalar, new AnimationCurve(BuildKeyframeCurve(obj["maxCurve"])));
                case 2:
                    return new ParticleSystem.MinMaxCurve(scalar,
                        new AnimationCurve(BuildKeyframeCurve(obj["minCurve"])),
                        new AnimationCurve(BuildKeyframeCurve(obj["maxCurve"])));
                case 3:
                    return new ParticleSystem.MinMaxCurve(minScalar, scalar);
                default:
                    return new ParticleSystem.MinMaxCurve(scalar);
            }
        }

        static Keyframe[] BuildKeyframeCurve(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return new[] { new Keyframe(0, 0) };
            return ((JArray)token).Select(k => new Keyframe(
                (float)k["time"], (float)k["value"], (float)k["inTangent"], (float)k["outTangent"])).ToArray();
        }

        static void ApplyMinMaxGradient(JToken token, Action<ParticleSystem.MinMaxGradient> setter)
        {
            if (token == null || token.Type == JTokenType.Null)
                return;
            var obj = (JObject)token;
            var mode = (int)obj["mode"];
            switch (mode)
            {
                case 0:
                    setter(new ParticleSystem.MinMaxGradient(ColorFromJson(obj["maxColor"])));
                    break;
                case 1:
                    setter(new ParticleSystem.MinMaxGradient(BuildGradient(obj["maxGradient"])));
                    break;
                case 2:
                    setter(new ParticleSystem.MinMaxGradient(
                        BuildGradient(obj["minGradient"]), BuildGradient(obj["maxGradient"])));
                    break;
                case 3:
                    setter(new ParticleSystem.MinMaxGradient(
                        ColorFromJson(obj["minColor"]), ColorFromJson(obj["maxColor"])));
                    break;
                default:
                    setter(new ParticleSystem.MinMaxGradient(Color.white));
                    break;
            }
        }

        static Gradient BuildGradient(JToken token)
        {
            var gradient = new Gradient();
            if (token == null || token.Type == JTokenType.Null)
                return gradient;
            var obj = (JObject)token;
            var colorKeys = ((JArray)obj["colorKeys"]).Select(k => new GradientColorKey(
                ColorFromJson(k["color"]), (float)k["time"])).ToArray();
            var alphaKeys = ((JArray)obj["alphaKeys"]).Select(k => new GradientAlphaKey(
                (float)k["alpha"], (float)k["time"])).ToArray();
            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        static Color ColorFromJson(JToken token)
        {
            if (token is JArray arr)
                return new Color((float)arr[0], (float)arr[1], (float)arr[2], (float)arr[3]);
            return Color.white;
        }

        static Vector3 Vector3Value(JObject v) => new((float)v["x"], (float)v["y"], (float)v["z"]);

        static Quaternion QuaternionValue(JObject v) => new((float)v["x"], (float)v["y"], (float)v["z"], (float)v["w"]);

        static void EnsureFolder(string path)
        {
            var current = "Assets";
            foreach (var segment in path.Split('/').Skip(1))
            {
                var next = current + "/" + segment;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segment);
                current = next;
            }
        }
    }
}
