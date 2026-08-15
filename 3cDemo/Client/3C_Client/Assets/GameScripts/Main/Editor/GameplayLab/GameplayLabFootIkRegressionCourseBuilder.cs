using System;
using System.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ThirdPersonGameplay.Editor.Lab
{
    internal static class GameplayLabFootIkRegressionCourseBuilder
    {
        const string EnvironmentPrefabPath = "Assets/Scenes/Shared/CharacterMovementTestEnvironment.prefab";
        const string CollisionPath = "Assets/Configs/Simulation/DeterministicRollback/World/CorinDeterministicCollisionWorld.asset";

        [MenuItem("Tools/3C/Gameplay Lab/Sync Foot IK Automatic Regression Course")]
        static void SyncFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("GameplayLab Foot IK regression course cannot be synchronized in Play Mode.");
            SyncEnvironmentPrefab();
            SyncGameplayLabScene();
            Debug.Log("GameplayLab Foot IK automatic regression course synchronized and collision world baked.");
        }

        internal static void SyncEnvironmentPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(EnvironmentPrefabPath);
            try
            {
                Renderer template = root.GetComponentsInChildren<Renderer>(true)
                    .FirstOrDefault(value => string.Equals(value.name, "HighStep_01", StringComparison.Ordinal));
                if (!template)
                    throw new InvalidOperationException("Character Movement Test Environment has no HighStep_01 visual template.");
                Transform existing = FindDirect(root.transform, GameplayLabFootIkRegressionCourse.RootName);
                if (existing)
                    Object.DestroyImmediate(existing.gameObject);
                BuildCourse(root.transform, template.sharedMaterials);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, EnvironmentPrefabPath);
                if (!saved)
                    throw new InvalidOperationException("Character Movement Test Environment could not save the Foot IK regression course.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.ImportAsset(EnvironmentPrefabPath, ImportAssetOptions.ForceUpdate);
        }

        internal static void SyncGameplayLabScene()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(GameplayLabEditorLauncher.ScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(GameplayLabEditorLauncher.ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                RemoveLegacyRootMarker(scene, GameplayLabFootIkRegressionCourse.StartMarkerName);
                RemoveLegacyRootMarker(scene, GameplayLabFootIkRegressionCourse.EndMarkerName);
                ValidateScene(scene);
                DeterministicCollisionWorldAuthoring world = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<DeterministicCollisionWorldAuthoring>(true))
                    .Single();
                DeterministicCollisionWorldAsset collision = AssetDatabase.LoadAssetAtPath<DeterministicCollisionWorldAsset>(CollisionPath);
                if (!collision || world.Output != collision)
                    throw new InvalidOperationException("GameplayLab Foot IK regression course targets another deterministic collision world.");
                DeterministicCollisionWorldBaker.Bake(world);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("GameplayLab scene could not save the Foot IK regression course.");
            }
            finally
            {
                if (openedHere && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
            }
        }

        internal static void ValidateScene(Scene scene)
        {
            GameplayLabFootIkRegressionCourse.Resolve(scene, out _, out _);
            DeterministicCollisionWorldAuthoring[] worlds = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<DeterministicCollisionWorldAuthoring>(true))
                .ToArray();
            if (worlds.Length != 1)
                throw new InvalidOperationException($"GameplayLab requires one deterministic collision world, found {worlds.Length}.");
            StairTraversalWorldValidationReport report = StairTraversalSurfaceValidator.ValidateWorld(worlds[0]);
            if (report.HasErrors)
                throw new InvalidOperationException($"GameplayLab stair validation failed:{Environment.NewLine}{report.FormatErrors()}");
        }

        static void BuildCourse(Transform environment, Material[] materials)
        {
            StairSurfaceLayerResolution layers = StairSurfaceLayerResolver.ResolveRequired();
            GameObject course = CreateObject(GameplayLabFootIkRegressionCourse.RootName, environment, layers.Ground);
            GameObject gameplayRoot = CreateObject("GameplaySurfaces", course.transform, layers.Ground);
            ConfigureSurface(gameplayRoot.AddComponent<DeterministicCollisionSurfaceAuthoring>());
            GameObject footRoot = CreateObject("FootPlacementSurfaces", course.transform, layers.FootPlacement);
            GameObject ascentRoot = CreateObject("AscentTreads", footRoot.transform, layers.FootPlacement);
            GameObject descentRoot = CreateObject("DescentTreads", footRoot.transform, layers.FootPlacement);

            float routeStart = GameplayLabFootIkRegressionCourse.CourseStartZ;
            float flightRun = GameplayLabFootIkRegressionCourse.FlightRun;
            float descentStart = GameplayLabFootIkRegressionCourse.DescentStartZ;
            float courseEnd = GameplayLabFootIkRegressionCourse.CourseEndZ;
            float height = GameplayLabFootIkRegressionCourse.CourseHeight;
            float width = GameplayLabFootIkRegressionCourse.CourseWidth;
            for (int i = 0; i < GameplayLabFootIkRegressionCourse.StepCountPerFlight; i++)
            {
                float top = (i + 1) * GameplayLabFootIkRegressionCourse.StepRise;
                CreateCube(
                    $"{GameplayLabFootIkRegressionCourse.AscentStepPrefix}{i + 1:D2}",
                    ascentRoot.transform,
                    new Vector3(
                        GameplayLabFootIkRegressionCourse.CourseX,
                        top * 0.5f,
                        routeStart + (i + 0.5f) * GameplayLabFootIkRegressionCourse.StepRun),
                    new Vector3(width, top, GameplayLabFootIkRegressionCourse.StepRun + 0.02f),
                    layers.FootPlacement,
                    materials);
                float descentTop = (GameplayLabFootIkRegressionCourse.StepCountPerFlight - i) *
                                     GameplayLabFootIkRegressionCourse.StepRise;
                CreateCube(
                    $"{GameplayLabFootIkRegressionCourse.DescentStepPrefix}{i + 1:D2}",
                    descentRoot.transform,
                    new Vector3(
                        GameplayLabFootIkRegressionCourse.CourseX,
                        descentTop * 0.5f,
                        descentStart + (i + 0.5f) * GameplayLabFootIkRegressionCourse.StepRun),
                    new Vector3(width, descentTop, GameplayLabFootIkRegressionCourse.StepRun + 0.02f),
                    layers.FootPlacement,
                    materials);
            }

            CreateGroundMesh(
                "RegressionApproachStart",
                gameplayRoot.transform,
                GameplayLabFootIkRegressionCourse.CourseStartZ -
                (GameplayLabFootIkRegressionCourse.AlignmentDistance + GameplayLabFootIkRegressionCourse.EndpointMargin + 1f) * 0.5f,
                GameplayLabFootIkRegressionCourse.AlignmentDistance + GameplayLabFootIkRegressionCourse.EndpointMargin + 1f,
                width + 2f,
                layers.Ground,
                materials);
            CreateCube(
                "RegressionTop",
                gameplayRoot.transform,
                new Vector3(
                    GameplayLabFootIkRegressionCourse.CourseX,
                    height * 0.5f,
                    routeStart + flightRun + GameplayLabFootIkRegressionCourse.TopLength * 0.5f),
                new Vector3(width, height, GameplayLabFootIkRegressionCourse.TopLength),
                layers.Ground,
                materials);
            CreateGroundMesh(
                "RegressionApproachEnd",
                gameplayRoot.transform,
                courseEnd +
                (GameplayLabFootIkRegressionCourse.AlignmentDistance + GameplayLabFootIkRegressionCourse.EndpointMargin + 1f) * 0.5f,
                GameplayLabFootIkRegressionCourse.AlignmentDistance + GameplayLabFootIkRegressionCourse.EndpointMargin + 1f,
                width + 2f,
                layers.Ground,
                materials);

            CreateStair(
                GameplayLabFootIkRegressionCourse.AscentIdentity,
                gameplayRoot.transform,
                ascentRoot.transform,
                new Vector3(GameplayLabFootIkRegressionCourse.CourseX, 0f, routeStart),
                new Vector3(GameplayLabFootIkRegressionCourse.CourseX, height, routeStart + flightRun),
                layers.Ground);
            CreateStair(
                GameplayLabFootIkRegressionCourse.DescentIdentity,
                gameplayRoot.transform,
                descentRoot.transform,
                new Vector3(GameplayLabFootIkRegressionCourse.CourseX, 0f, courseEnd),
                new Vector3(GameplayLabFootIkRegressionCourse.CourseX, height, descentStart),
                layers.Ground);
            CreateMarker(GameplayLabFootIkRegressionCourse.StartMarkerName, course.transform, GameplayLabFootIkRegressionCourse.StartPosition);
            CreateMarker(GameplayLabFootIkRegressionCourse.EndMarkerName, course.transform, GameplayLabFootIkRegressionCourse.EndPosition);
        }

        static void CreateStair(
            string identity,
            Transform parent,
            Transform footSurfaceRoot,
            Vector3 lower,
            Vector3 upper,
            int groundLayer)
        {
            GameObject stairObject = CreateObject(identity, parent, groundLayer);
            Transform lowerTransition = CreateMarker("LowerTransition", stairObject.transform, lower);
            Transform upperTransition = CreateMarker("UpperTransition", stairObject.transform, upper);
            StairTraversalSurfaceAuthoring authoring = stairObject.AddComponent<StairTraversalSurfaceAuthoring>();
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("m_StairIdentity").stringValue = identity;
            serialized.FindProperty("m_FootSurfaceRoot").objectReferenceValue = footSurfaceRoot;
            serialized.FindProperty("m_LowerTransition").objectReferenceValue = lowerTransition;
            serialized.FindProperty("m_UpperTransition").objectReferenceValue = upperTransition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            StairTraversalRampEditorOperations.Create(authoring);
        }

        static void ConfigureSurface(DeterministicCollisionSurfaceAuthoring authoring)
        {
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("m_SurfaceIdentity").stringValue = "foot-ik-regression-course";
            serialized.FindProperty("m_MaterialIdentity").stringValue = "default";
            serialized.FindProperty("m_Walkable").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateGroundMesh(
            string name,
            Transform parent,
            float centerZ,
            float length,
            float width,
            int layer,
            Material[] materials)
        {
            GameObject value = CreateCube(
                name,
                parent,
                new Vector3(GameplayLabFootIkRegressionCourse.CourseX, -0.15f, centerZ),
                new Vector3(width, 0.3f, length),
                layer,
                materials);
            BoxCollider box = value.GetComponent<BoxCollider>();
            Object.DestroyImmediate(box);
            MeshCollider mesh = value.AddComponent<MeshCollider>();
            mesh.sharedMesh = value.GetComponent<MeshFilter>().sharedMesh;
        }

        static GameObject CreateCube(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            int layer,
            Material[] materials)
        {
            GameObject value = GameObject.CreatePrimitive(PrimitiveType.Cube);
            MoveAndParent(value, parent);
            value.name = name;
            value.layer = layer;
            value.isStatic = true;
            value.transform.SetPositionAndRotation(position, Quaternion.identity);
            value.transform.localScale = scale;
            Renderer renderer = value.GetComponent<Renderer>();
            renderer.sharedMaterials = materials;
            return value;
        }

        static GameObject CreateObject(string name, Transform parent, int layer)
        {
            var value = new GameObject(name);
            MoveAndParent(value, parent);
            value.layer = layer;
            return value;
        }

        static Transform CreateMarker(string name, Transform parent, Vector3 position)
        {
            GameObject marker = CreateObject(name, parent, parent.gameObject.layer);
            marker.transform.position = position;
            return marker.transform;
        }

        static void MoveAndParent(GameObject value, Transform parent)
        {
            SceneManager.MoveGameObjectToScene(value, parent.gameObject.scene);
            value.transform.SetParent(parent, false);
        }

        static Transform FindDirect(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                    return child;
            }
            return null;
        }

        static void RemoveLegacyRootMarker(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (string.Equals(roots[i].name, name, StringComparison.Ordinal))
                    Object.DestroyImmediate(roots[i]);
            }
        }
    }
}
