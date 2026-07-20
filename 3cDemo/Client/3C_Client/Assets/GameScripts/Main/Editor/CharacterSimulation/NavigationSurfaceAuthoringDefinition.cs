using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation.DotRecast;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    [CreateAssetMenu(fileName = "NavigationSurfaceAuthoring", menuName = "3C/Simulation/Navigation Surface Authoring")]
    public sealed class NavigationSurfaceAuthoringDefinition : ScriptableObject
    {
        [SerializeField] string m_MapId = string.Empty;
        [SerializeField] string m_WorldRevision = string.Empty;
        [SerializeField] GameObject m_GeometryPrefab;
        [SerializeField] LayerMask m_IncludedLayers;
        [SerializeField] bool m_RequireStatic;
        [SerializeField] string m_OutputAssetPath = string.Empty;
        [SerializeField] double m_CellSize;
        [SerializeField] double m_CellHeight;
        [SerializeField] double m_AgentHeight;
        [SerializeField] double m_AgentRadius;
        [SerializeField] double m_AgentMaxClimb;
        [SerializeField] double m_AgentMaxSlope;
        [SerializeField] int m_RegionMinSize;
        [SerializeField] int m_RegionMergeSize;
        [SerializeField] double m_EdgeMaxLength;
        [SerializeField] double m_EdgeMaxError;
        [SerializeField] int m_VerticesPerPolygon;
        [SerializeField] double m_DetailSampleDistance;
        [SerializeField] double m_DetailSampleMaxError;
        [SerializeField] double m_NearestExtentX;
        [SerializeField] double m_NearestExtentY;
        [SerializeField] double m_NearestExtentZ;
        [SerializeField] double m_ProjectionTolerance;
        [SerializeField] double m_HeightTolerance;
        [SerializeField] double m_MaximumDisplacement;
        [SerializeField] double m_BoundaryInset;
        [SerializeField] int m_MaximumVisitedPolygons;
        [SerializeField] int m_IncludeFlags;

        public string MapId => Require(m_MapId, "Map ID");
        public string WorldRevision => Require(m_WorldRevision, "World Revision");
        public GameObject GeometryPrefab => m_GeometryPrefab ? m_GeometryPrefab : throw new InvalidOperationException("Navigation authoring requires an explicit geometry prefab.");
        public LayerMask IncludedLayers => m_IncludedLayers.value != 0 ? m_IncludedLayers : throw new InvalidOperationException("Navigation authoring requires explicit included layers.");
        public bool RequireStatic => m_RequireStatic;
        public string OutputAssetPath => RequireAssetPath(m_OutputAssetPath);

        public DotRecastBuildProfile BuildProfile => new DotRecastBuildProfile(
            m_CellSize, m_CellHeight, m_AgentHeight, m_AgentRadius, m_AgentMaxClimb, m_AgentMaxSlope,
            m_RegionMinSize, m_RegionMergeSize, m_EdgeMaxLength, m_EdgeMaxError, m_VerticesPerPolygon,
            m_DetailSampleDistance, m_DetailSampleMaxError);

        public DotRecastQueryProfile QueryProfile => new DotRecastQueryProfile(
            m_NearestExtentX, m_NearestExtentY, m_NearestExtentZ, m_ProjectionTolerance,
            m_HeightTolerance, m_MaximumDisplacement, m_BoundaryInset, m_MaximumVisitedPolygons,
            m_IncludeFlags, 0, new[] { 1d });

        public void RequireValid()
        {
            _ = MapId;
            _ = WorldRevision;
            string geometryPath = AssetDatabase.GetAssetPath(GeometryPrefab);
            if (!geometryPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Navigation geometry must be a prefab asset.");
            _ = IncludedLayers;
            _ = OutputAssetPath;
            _ = BuildProfile;
            _ = QueryProfile;
        }

        static string Require(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Navigation authoring requires {label}.");
            return value.Trim();
        }

        static string RequireAssetPath(string value)
        {
            string path = Require(value, "an output asset path").Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Navigation output asset path must be an Assets/... .asset path.");
            return path;
        }
    }

    [CustomEditor(typeof(NavigationSurfaceAuthoringDefinition))]
    public sealed class NavigationSurfaceAuthoringDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(6f);
            if (GUILayout.Button("Build And Publish Navigation Surface"))
                NavigationSurfaceBuildService.BuildAndPublish((NavigationSurfaceAuthoringDefinition)target);
        }
    }

    public static class NavigationSurfaceBuildMenus
    {
        [MenuItem("Tools/3C/Simulation/Navigation/Build All Surfaces")]
        public static void BuildAllSurfaces()
        {
            string[] guids = AssetDatabase.FindAssets("t:NavigationSurfaceAuthoringDefinition");
            if (guids.Length == 0)
                throw new InvalidOperationException("No Navigation Surface authoring definitions were found.");
            var paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            Array.Sort(paths, StringComparer.Ordinal);
            for (int i = 0; i < paths.Length; i++)
            {
                NavigationSurfaceAuthoringDefinition definition =
                    AssetDatabase.LoadAssetAtPath<NavigationSurfaceAuthoringDefinition>(paths[i]);
                if (!definition)
                    throw new InvalidOperationException($"Navigation Surface authoring definition is missing: {paths[i]}.");
                NavigationSurfaceBuildService.BuildAndPublish(definition);
            }
            UnityEngine.Debug.Log($"Built and published {paths.Length} Navigation Surface asset(s).");
        }
    }

    public static class NavigationSurfaceBuildService
    {
        const string CoordinateProfile = "unity-x-right-y-up-z-forward-meters";

        public static NavigationSurfaceAsset BuildAndPublish(NavigationSurfaceAuthoringDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            definition.RequireValid();
            string geometryPrefabPath = AssetDatabase.GetAssetPath(definition.GeometryPrefab);
            string sourceRevision = AssetDatabase.GetAssetDependencyHash(geometryPrefabPath).ToString();
            NavigationGeometryArtifact geometry = ExportGeometry(definition, geometryPrefabPath, sourceRevision);
            string artifactDirectory = Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? throw new InvalidOperationException(), "Library", "NavigationArtifacts");
            Directory.CreateDirectory(artifactDirectory);
            string fileStem = FileSafe(definition.MapId) + "." + sourceRevision;
            string geometryPath = Path.Combine(artifactDirectory, fileStem + ".navgeom");
            string surfacePath = Path.Combine(artifactDirectory, fileStem + ".navsurface");
            byte[] geometryBytes = NavigationGeometryArtifactCodec.Write(geometry);
            File.WriteAllBytes(geometryPath, geometryBytes);
            NavigationGeometryArtifact verifiedGeometry = NavigationGeometryArtifactCodec.Read(File.ReadAllBytes(geometryPath));
            if (verifiedGeometry.GeometryHash != NavigationGeometryArtifactCodec.Read(geometryBytes).GeometryHash)
                throw new InvalidOperationException("Navigation geometry write verification failed.");
            RunBuildTool(definition, geometryPath, surfacePath);
            byte[] surfaceBytes = File.ReadAllBytes(surfacePath);
            NavigationSurfaceArtifact surface = NavigationSurfaceArtifactCodec.Read(surfaceBytes);
            if (!string.Equals(surface.MapId, definition.MapId, StringComparison.Ordinal) ||
                !string.Equals(surface.WorldRevision, definition.WorldRevision, StringComparison.Ordinal) ||
                surface.GeometryHash != verifiedGeometry.GeometryHash)
                throw new InvalidOperationException("Navigation Surface identity does not match its authoring source.");
            return NavigationSurfacePublishService.Publish(surfaceBytes, definition.OutputAssetPath);
        }

        static NavigationGeometryArtifact ExportGeometry(NavigationSurfaceAuthoringDefinition definition, string prefabPath, string sourceRevision)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var filters = new List<MeshFilter>(root.GetComponentsInChildren<MeshFilter>(true));
                filters.Sort((left, right) => string.CompareOrdinal(
                    GlobalObjectId.GetGlobalObjectIdSlow(left).ToString(),
                    GlobalObjectId.GetGlobalObjectIdSlow(right).ToString()));
                var sources = new List<NavigationGeometrySource>();
                for (int i = 0; i < filters.Count; i++)
                {
                    MeshFilter filter = filters[i];
                    if (!filter || !filter.sharedMesh || (definition.IncludedLayers.value & (1 << filter.gameObject.layer)) == 0)
                        continue;
                    if (definition.RequireStatic && !filter.gameObject.isStatic)
                        continue;
                    Mesh mesh = filter.sharedMesh;
                    Vector3[] vertices = mesh.vertices;
                    int[] triangles = mesh.triangles;
                    var values = new double[vertices.Length * 3];
                    for (int vertex = 0; vertex < vertices.Length; vertex++)
                    {
                        values[vertex * 3] = vertices[vertex].x;
                        values[vertex * 3 + 1] = vertices[vertex].y;
                        values[vertex * 3 + 2] = vertices[vertex].z;
                    }
                    Matrix4x4 matrix = filter.transform.localToWorldMatrix;
                    var transform = new double[16];
                    for (int index = 0; index < transform.Length; index++)
                        transform[index] = matrix[index];
                    sources.Add(new NavigationGeometrySource(
                        GlobalObjectId.GetGlobalObjectIdSlow(filter).ToString(),
                        0,
                        values,
                        triangles,
                        transform));
                }
                if (sources.Count == 0)
                    throw new InvalidOperationException("Navigation authoring selection contains no explicit MeshFilter geometry.");
                return NavigationGeometryArtifactCodec.Read(NavigationGeometryArtifactCodec.Write(
                    new NavigationGeometryArtifact(definition.MapId, definition.WorldRevision, sourceRevision, CoordinateProfile, sources)));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void RunBuildTool(NavigationSurfaceAuthoringDefinition definition, string geometryPath, string surfacePath)
        {
            string projectPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "../../../../Tools/ThirdPersonSimulation.Portable/ThirdPersonSimulation.NavigationBuildTool/ThirdPersonSimulation.NavigationBuildTool.csproj"));
            if (!File.Exists(projectPath))
                throw new FileNotFoundException("Navigation BuildTool project is missing.", projectPath);
            string workingDirectory = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            try
            {
                RunDotNet(
                    new[] { "build", projectPath, "--disable-build-servers", "/nr:false", "/p:UseSharedCompilation=false" },
                    workingDirectory,
                    "Navigation BuildTool compilation failed.");
            }
            finally
            {
                RunDotNet(new[] { "build-server", "shutdown" }, workingDirectory, ".NET build server shutdown failed.");
            }
            string toolPath = Path.Combine(
                Path.GetDirectoryName(projectPath) ?? throw new InvalidOperationException("Navigation BuildTool directory is missing."),
                "bin", "Debug", "net8.0", "ThirdPersonSimulation.NavigationBuildTool.dll");
            if (!File.Exists(toolPath))
                throw new FileNotFoundException("Compiled Navigation BuildTool is missing.", toolPath);
            DotRecastBuildProfile build = definition.BuildProfile;
            DotRecastQueryProfile query = definition.QueryProfile;
            var arguments = new List<string>
            {
                toolPath,
                "build", geometryPath, surfacePath,
                Invariant(build.CellSize), Invariant(build.CellHeight), Invariant(build.AgentHeight), Invariant(build.AgentRadius),
                Invariant(build.AgentMaxClimb), Invariant(build.AgentMaxSlope), build.RegionMinSize.ToString(CultureInfo.InvariantCulture),
                build.RegionMergeSize.ToString(CultureInfo.InvariantCulture), Invariant(build.EdgeMaxLength), Invariant(build.EdgeMaxError),
                build.VerticesPerPolygon.ToString(CultureInfo.InvariantCulture), Invariant(build.DetailSampleDistance), Invariant(build.DetailSampleMaxError),
                Invariant(query.NearestExtentX), Invariant(query.NearestExtentY), Invariant(query.NearestExtentZ),
                Invariant(query.ProjectionTolerance), Invariant(query.HeightTolerance), Invariant(query.MaximumDisplacement),
                Invariant(query.BoundaryInset), query.MaximumVisitedPolygons.ToString(CultureInfo.InvariantCulture),
                query.IncludeFlags.ToString(CultureInfo.InvariantCulture)
            };
            RunDotNet(arguments, workingDirectory, "Navigation BuildTool failed.");
        }

        static void RunDotNet(IReadOnlyList<string> arguments, string workingDirectory, string failureMessage)
        {
            var start = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = JoinArguments(arguments),
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using Process process = Process.Start(start) ?? throw new InvalidOperationException("Failed to start Navigation BuildTool.");
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            string output = outputTask.GetAwaiter().GetResult();
            string error = errorTask.GetAwaiter().GetResult();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"{failureMessage}\n{output}\n{error}");
        }

        static string JoinArguments(IReadOnlyList<string> values)
        {
            var result = new StringBuilder();
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                    result.Append(' ');
                result.Append('"').Append(values[i].Replace("\"", "\\\"")).Append('"');
            }
            return result.ToString();
        }

        static string Invariant(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        static string FileSafe(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value;
        }
    }

    public static class NavigationSurfacePublishService
    {
        public static NavigationSurfaceAsset Publish(byte[] bytes, string assetPath)
        {
            NavigationSurfaceArtifact expected = NavigationSurfaceArtifactCodec.Read(bytes);
            string directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("Navigation Surface asset directory is missing.");
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? throw new InvalidOperationException(), directory));
            NavigationSurfaceAsset asset = AssetDatabase.LoadAssetAtPath<NavigationSurfaceAsset>(assetPath);
            if (!asset)
            {
                asset = ScriptableObject.CreateInstance<NavigationSurfaceAsset>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            asset.SetCanonicalArtifact(bytes);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            NavigationSurfaceAsset reloaded = AssetDatabase.LoadAssetAtPath<NavigationSurfaceAsset>(assetPath) ??
                throw new InvalidOperationException("Published Navigation Surface asset could not be reloaded.");
            NavigationSurfaceArtifact actual = reloaded.Load();
            if (actual.ContentHash != expected.ContentHash || !BytesEqual(bytes, reloaded.CopyCanonicalArtifact()))
                throw new InvalidOperationException("Published Navigation Surface wrapper does not preserve exact artifact bytes.");
            return reloaded;
        }

        static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }
    }
}
