using System;
using System.Collections.Generic;
using System.Text;
using Animancer;
using Cinemachine;
using ThirdPersonCamera;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonGameplay.Lab;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ThirdPersonGameplay.Editor.Lab
{
    internal static class GameplayLabAssetBuilder
    {
        const string ConfigDirectory = "Assets/Configs/Simulation/GameplayLab";
        const string PrefabDirectory = "Assets/Prefabs/GameplayLab";
        const string SceneDirectory = "Assets/Scenes/GameplayLab";
        const string FloatCompositionPath = "Assets/Configs/Character/Corin/Pipeline/Definition/CorinLocalSimulationSessionComposition.asset";
        const string FixedProgramPath = "Assets/Configs/Simulation/DeterministicRollback/CorinFixedProgram.asset";
        const string FixedRuntimePath = "Assets/Configs/Simulation/DeterministicRollback/CorinFixedProgramRuntime.asset";
        const string FixedBackendPath = "Assets/Configs/Simulation/DeterministicRollback/CorinFixedPassBackend.asset";
        const string FixedSolverPath = "Assets/Configs/Simulation/DeterministicRollback/CorinDeterministicKcc.asset";
        const string CharacterDefinitionPath = "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset";
        const string PlayerPrefabPath = "Assets/Prefabs/Characters/RuntimeProfiles/CorinStandalonePlayer.prefab";
        const string TargetPrefabPath = "Assets/Prefabs/Characters/RuntimeProfiles/CorinStandaloneTrainingEnemy.prefab";
        const string EnvironmentPrefabPath = "Assets/Scenes/Shared/CharacterMovementTestEnvironment.prefab";
        const string FixedPipelinePath = ConfigDirectory + "/StandardFixedLocalSimulationPipeline.asset";
        const string FixedSourcePath = ConfigDirectory + "/LocalFixedSimulationSessionSource.asset";
        const string FixedCompositionPath = ConfigDirectory + "/CorinGameplayLabFixedComposition.asset";
        const string FloatRootPath = PrefabDirectory + "/GameplayLabLocalFloat32.prefab";
        const string FixedRootPath = PrefabDirectory + "/GameplayLabLocalFixed.prefab";
        const string FloatVariantPath = ConfigDirectory + "/GameplayLabLocalFloat32Variant.asset";
        const string FixedVariantPath = ConfigDirectory + "/GameplayLabLocalFixedVariant.asset";
        const string PlayerActorId = "gameplay-lab-player";
        const string TargetActorId = "gameplay-lab-target";
        const string ActionTargetInputId = "ActionTarget";
        const string CameraLookInputId = "LookAxis";

        static readonly Vector3 s_PlayerPosition = new Vector3(2.96f, 0f, -5.27f);
        static readonly Vector3 s_TargetPosition = new Vector3(2.96f, 0f, -1.7f);

        public static void Rebuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Gameplay Lab assets cannot be rebuilt in Play Mode.");
            EnsureFolders();
            CharacterPipelineDefinition definition = LoadRequired<CharacterPipelineDefinition>(CharacterDefinitionPath);
            CharacterSimulationBuildResult build = CharacterSimulationBuildOrchestrator.Build(
                new CharacterSimulationBuildRequest(
                    definition,
                    CharacterSimulationBuildPublicationMode.Publish,
                    new ICharacterSimulationTargetBuildAdapter[]
                    {
                        CharacterSimulationTargetCatalog.Float32(definition),
                        new FixedCharacterSimulationTargetBuildAdapter(FixedProgramPath)
                    }));
            if (!build.IsValid)
                throw new InvalidOperationException(BuildFailureMessage(build));
            SimulationSessionCompositionDefinition fixedComposition = BuildFixedComposition();
            GameObject floatRoot = BuildFloatRuntimeRoot();
            GameObject fixedRoot = BuildFixedRuntimeRoot(fixedComposition);
            GameplayLabSessionVariantDefinition floatVariant = BuildVariant(
                FloatVariantPath,
                "gameplay-lab.local-float32",
                floatRoot,
                LoadRequired<SimulationSessionCompositionDefinition>(FloatCompositionPath));
            GameplayLabSessionVariantDefinition fixedVariant = BuildVariant(
                FixedVariantPath,
                "gameplay-lab.local-fixed-q32.32",
                fixedRoot,
                fixedComposition);
            BuildScene(floatVariant, fixedVariant);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GameplayLabEditorLauncher.Validate();
            Debug.Log("Gameplay Lab assets rebuilt: Local Float32 and Local Fixed Q32.32.");
        }

        static string BuildFailureMessage(CharacterSimulationBuildResult build)
        {
            if (build == null)
                return "Gameplay Lab Character Simulation build returned no result.";
            var message = new StringBuilder("Gameplay Lab Character Simulation targets failed to build.");
            for (int i = 0; i < build.Report.Messages.Count; i++)
            {
                CharacterSimulationCompileMessage diagnostic = build.Report.Messages[i];
                if (diagnostic.Severity != CharacterSimulationCompileSeverity.Error)
                    continue;
                message.AppendLine();
                message.Append(diagnostic);
            }
            message.AppendLine();
            message.Append("Artifact=").Append(build.Artifact != null ? "ready" : "missing");
            message.Append(" Targets=").Append(build.TargetProducts.Count);
            message.Append(" Projection=").Append(build.PresentationProjection != null ? "ready" : "missing");
            return message.ToString();
        }

        static SimulationSessionCompositionDefinition BuildFixedComposition()
        {
            FixedCharacterSimulationProgramAsset fixedProgram = LoadRequired<FixedCharacterSimulationProgramAsset>(FixedProgramPath);
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program = fixedProgram.Load();
            FixedProgramRuntimeDefinition runtime = LoadRequired<FixedProgramRuntimeDefinition>(FixedRuntimePath);
            FixedPassExecutionBackendDefinition backend = LoadRequired<FixedPassExecutionBackendDefinition>(FixedBackendPath);
            DeterministicKccWorldSolverDefinition solver = LoadRequired<DeterministicKccWorldSolverDefinition>(FixedSolverPath);
            StandardFixedLocalSimulationPipelineDefinition pipeline = CreateOrLoad<StandardFixedLocalSimulationPipelineDefinition>(FixedPipelinePath);
            LocalFixedSimulationSessionSourceDefinition source = CreateOrLoad<LocalFixedSimulationSessionSourceDefinition>(FixedSourcePath);
            SimulationSessionCompositionDefinition composition = CreateOrLoad<SimulationSessionCompositionDefinition>(FixedCompositionPath);
            string mapId = solver.LoadCollisionWorld().MapId;
            composition.SetAuthoring(
                "corin-gameplay-lab-fixed-local",
                "corin-gameplay-lab-world",
                mapId,
                "corin-gameplay-lab-world-v1",
                "corin-gameplay-lab-fixed-local-logic",
                program.Manifest.TickRate,
                runtime,
                backend,
                pipeline,
                source,
                solver,
                WorldFeature.Ground |
                WorldFeature.Slope |
                WorldFeature.Step |
                WorldFeature.WallSlide |
                WorldFeature.ActorCollision);
            EditorUtility.SetDirty(pipeline);
            EditorUtility.SetDirty(source);
            EditorUtility.SetDirty(composition);
            composition.RequireComplete();
            return composition;
        }

        static GameObject BuildFloatRuntimeRoot()
        {
            SimulationSessionCompositionDefinition composition =
                LoadRequired<SimulationSessionCompositionDefinition>(FloatCompositionPath);
            Scene previous = SceneManager.GetActiveScene();
            Scene workspace = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(workspace);
                var root = new GameObject("GameplayLabLocalFloat32");
                SimulationSessionHost sessionHost = root.AddComponent<SimulationSessionHost>();
                sessionHost.BindComposition(composition);
                ThirdPersonCameraController cameraRig = CreateCameraRig(root.transform);
                CharacterPipelineHost player = InstantiateFloatActor(
                    PlayerPrefabPath,
                    root.transform,
                    "Gameplay Lab Player",
                    PlayerActorId,
                    s_PlayerPosition,
                    Quaternion.identity,
                    sessionHost,
                    CharacterPresentationRole.LocalOwner,
                    cameraRig);
                CharacterPipelineHost target = InstantiateFloatActor(
                    TargetPrefabPath,
                    root.transform,
                    "Gameplay Lab Target",
                    TargetActorId,
                    s_TargetPosition,
                    Quaternion.Euler(0f, 180f, 0f),
                    sessionHost,
                    CharacterPresentationRole.SimulatedActor,
                    null);
                ReplaceWithNeutralControl(target);
                SessionActorActionTargetInputProvider provider =
                    player.GetComponent<SessionActorActionTargetInputProvider>();
                if (!provider)
                    throw new InvalidOperationException("Gameplay Lab Float player requires the formal Session Actor target provider.");
                provider.SetAuthoring(target);
                GameObject saved = SavePrefab(root, FloatRootPath);
                Object.DestroyImmediate(root);
                return saved;
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(workspace, true);
            }
        }

        static GameObject BuildFixedRuntimeRoot(SimulationSessionCompositionDefinition composition)
        {
            CharacterPipelineDefinition definition = LoadRequired<CharacterPipelineDefinition>(CharacterDefinitionPath);
            FixedCharacterSimulationProgramAsset fixedProgram = LoadRequired<FixedCharacterSimulationProgramAsset>(FixedProgramPath);
            Scene previous = SceneManager.GetActiveScene();
            Scene workspace = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(workspace);
                var root = new GameObject("GameplayLabLocalFixed");
                SimulationSessionHost sessionHost = root.AddComponent<SimulationSessionHost>();
                sessionHost.BindComposition(composition);
                ThirdPersonCameraController cameraRig = CreateCameraRig(root.transform);
                FixedCharacterHost player = InstantiateFixedActor(
                    PlayerPrefabPath,
                    root.transform,
                    "Gameplay Lab Fixed Player",
                    new ActorId(PlayerActorId),
                    s_PlayerPosition,
                    Quaternion.identity,
                    sessionHost,
                    fixedProgram,
                    definition,
                    CharacterPresentationRole.LocalOwner,
                    cameraRig,
                    true);
                FixedCharacterHost target = InstantiateFixedActor(
                    TargetPrefabPath,
                    root.transform,
                    "Gameplay Lab Fixed Target",
                    new ActorId(TargetActorId),
                    s_TargetPosition,
                    Quaternion.Euler(0f, 180f, 0f),
                    sessionHost,
                    fixedProgram,
                    definition,
                    CharacterPresentationRole.SimulatedActor,
                    null,
                    false);
                SessionActorActionTargetInputProvider provider =
                    player.GetComponent<SessionActorActionTargetInputProvider>();
                if (!provider)
                    throw new InvalidOperationException("Gameplay Lab Fixed player requires the formal Session Actor target provider.");
                provider.SetAuthoring(target);
                GameObject saved = SavePrefab(root, FixedRootPath);
                Object.DestroyImmediate(root);
                return saved;
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(workspace, true);
            }
        }

        static CharacterPipelineHost InstantiateFloatActor(
            string prefabPath,
            Transform parent,
            string objectName,
            string actorId,
            Vector3 position,
            Quaternion rotation,
            SimulationSessionHost sessionHost,
            CharacterPresentationRole role,
            ThirdPersonCameraController cameraRig)
        {
            GameObject instance = InstantiatePrefab(prefabPath, parent.gameObject.scene);
            instance.name = objectName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            CharacterPipelineHost host = instance.GetComponent<CharacterPipelineHost>() ??
                throw new InvalidOperationException($"Character Prefab '{prefabPath}' has no CharacterPipelineHost.");
            host.BindSessionActor(sessionHost, new ActorId(actorId));
            host.SetRuntimeAuthoring(host.ControlSource, role, cameraRig);
            return host;
        }

        static FixedCharacterHost InstantiateFixedActor(
            string prefabPath,
            Transform parent,
            string objectName,
            ActorId actorId,
            Vector3 position,
            Quaternion rotation,
            SimulationSessionHost sessionHost,
            FixedCharacterSimulationProgramAsset fixedProgram,
            CharacterPipelineDefinition definition,
            CharacterPresentationRole role,
            ThirdPersonCameraController cameraRig,
            bool playerControlled)
        {
            GameObject instance = InstantiatePrefab(prefabPath, parent.gameObject.scene);
            instance.name = objectName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            CharacterPipelineHost floatHost = instance.GetComponent<CharacterPipelineHost>() ??
                throw new InvalidOperationException($"Character Prefab '{prefabPath}' has no CharacterPipelineHost.");
            Float32WorldBodyBinding floatBody = floatHost.WorldBodyBinding;
            string bindingId = floatBody ? floatBody.BindingId :
                throw new InvalidOperationException($"Character Prefab '{prefabPath}' has no World Body Binding.");
            Transform visualRoot = floatHost.VisualRoot;
            CharacterBodyPresentationProfile bodyPresentation = floatHost.BodyPresentationProfile;
            CharacterFootPlacementComposition footPlacement = floatHost.FootPlacement;
            CharacterEquipmentRigBindingCatalog equipment = floatHost.EquipmentRigBindings;
            AnimancerComponent animancer = floatHost.Animancer;
            CharacterAnimationRigBinding animationRigBinding = floatHost.AnimationRigBinding;
            Transform cameraFollowAnchor = floatHost.CameraFollowAnchor;
            Transform cameraAimAnchor = floatHost.CameraAimAnchor;
            IReadOnlyList<CameraTargetBinding> cameraBindings = floatHost.CameraTargetBindings;
            RemoveComponents<CharacterControlSource>(instance);
            Object.DestroyImmediate(floatBody, true);
            Object.DestroyImmediate(floatHost, true);
            CharacterController characterController = instance.GetComponent<CharacterController>();
            if (characterController)
                Object.DestroyImmediate(characterController, true);
            FixedCharacterControlSource controlSource;
            if (playerControlled)
            {
                SessionActorActionTargetInputProvider provider =
                    instance.GetComponent<SessionActorActionTargetInputProvider>() ??
                    instance.AddComponent<SessionActorActionTargetInputProvider>();
                var playerSource = instance.AddComponent<FixedPlayerCharacterControlSource>();
                playerSource.SetAuthoring(definition.InputProfile, ActionTargetInputId, provider);
                controlSource = playerSource;
            }
            else
            {
                RemoveComponents<CharacterActionTargetInputProvider>(instance);
                controlSource = instance.AddComponent<FixedNeutralCharacterControlSource>();
            }
            FixedCharacterHost fixedHost = instance.AddComponent<FixedCharacterHost>();
            fixedHost.SetAuthoring(
                sessionHost,
                fixedProgram,
                definition.PresentationProjection,
                controlSource,
                role,
                actorId,
                bindingId,
                instance.transform,
                visualRoot,
                bodyPresentation,
                footPlacement,
                equipment,
                animancer,
                animationRigBinding,
                cameraRig,
                cameraFollowAnchor,
                cameraAimAnchor,
                cameraBindings,
                role == CharacterPresentationRole.LocalOwner ? CameraLookInputId : string.Empty,
                128);
            return fixedHost;
        }

        static void ReplaceWithNeutralControl(CharacterPipelineHost host)
        {
            RemoveComponents<CharacterControlSource>(host.gameObject);
            RemoveComponents<CharacterActionTargetInputProvider>(host.gameObject);
            NeutralCharacterControlSource neutral = host.gameObject.AddComponent<NeutralCharacterControlSource>();
            host.SetRuntimeAuthoring(neutral, CharacterPresentationRole.SimulatedActor, null);
        }

        static ThirdPersonCameraController CreateCameraRig(Transform parent)
        {
            var targets = new GameObject("Gameplay Camera Targets");
            targets.transform.SetParent(parent, false);
            Transform follow = new GameObject("Camera Follow Target").transform;
            follow.SetParent(targets.transform, false);
            Transform aim = new GameObject("Camera Aim Target").transform;
            aim.SetParent(targets.transform, false);

            var freeLookObject = new GameObject("Character Camera FreeLook");
            freeLookObject.transform.SetParent(parent, false);
            CinemachineFreeLook freeLook = freeLookObject.AddComponent<CinemachineFreeLook>();
            freeLook.Follow = follow;
            freeLook.LookAt = aim;
            freeLook.m_Priority = 10;
            freeLook.m_XAxis.Value = 0f;
            freeLook.m_XAxis.m_InputAxisName = string.Empty;
            freeLook.m_YAxis.Value = 0.5f;
            freeLook.m_YAxis.m_InputAxisName = string.Empty;
            freeLook.m_Orbits = new[]
            {
                new CinemachineFreeLook.Orbit(3.2f, 3.2f),
                new CinemachineFreeLook.Orbit(1.45f, 4f),
                new CinemachineFreeLook.Orbit(-0.6f, 3f)
            };
            CinemachineCollider collider = freeLookObject.AddComponent<CinemachineCollider>();
            collider.m_CollideAgainst = 1;
            collider.m_MinimumDistanceFromTarget = 0.3f;
            collider.m_CameraRadius = 0.25f;
            collider.m_SmoothingTime = 0.05f;
            collider.m_Damping = 0.15f;
            collider.m_DampingWhenOccluded = 0.05f;

            var cameraObject = new GameObject("Gameplay Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent, false);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            CinemachineBrain brain = cameraObject.AddComponent<CinemachineBrain>();
            brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.ManualUpdate;
            brain.m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.LateUpdate;
            var controller = cameraObject.AddComponent<ThirdPersonCameraController>();
            controller.FreeLook = freeLook;
            controller.Brain = brain;
            controller.CameraFollowTarget = follow;
            controller.CameraAimTarget = aim;
            controller.BindFreeLookToResolvedTargets = true;
            controller.Sensitivity = new Vector2(0.12f, 0.0025f);
            return controller;
        }

        static GameplayLabSessionVariantDefinition BuildVariant(
            string path,
            string variantId,
            GameObject rootPrefab,
            SimulationSessionCompositionDefinition composition)
        {
            GameplayLabSessionVariantDefinition variant = CreateOrLoad<GameplayLabSessionVariantDefinition>(path);
            SimulationProgramRuntimeDescriptor program = composition.ProgramRuntime.BuildDescriptor();
            SimulationSessionSourceDescriptor source = composition.SessionSource.BuildAuthoringDescriptor().Source;
            SimulationPipelineDescriptor pipeline = composition.Pipeline.BuildPortableDescriptor();
            SimulationWorldSolverDefinitionDescriptor solver = composition.WorldSolver.BuildDescriptor(composition.TickRate);
            variant.SetAuthoring(
                variantId,
                rootPrefab,
                program.NumericProfileId.Value,
                program.TargetAbiVersion.Value,
                source.Identity.ComponentId,
                pipeline.PipelineId.Value,
                solver.Identity.ComponentId);
            variant.ValidateComposition(composition);
            EditorUtility.SetDirty(variant);
            return variant;
        }

        static void BuildScene(
            GameplayLabSessionVariantDefinition floatVariant,
            GameplayLabSessionVariantDefinition fixedVariant)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                GameObject environment = InstantiatePrefab(EnvironmentPrefabPath, scene);
                environment.name = "Character Movement Test Environment";
                var lightObject = new GameObject("Gameplay Lab Directional Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                var bootstrapObject = new GameObject("Gameplay Lab Bootstrap");
                GameplayLabBootstrap bootstrap = bootstrapObject.AddComponent<GameplayLabBootstrap>();
                bootstrap.SetVariants(0, floatVariant, fixedVariant);
                Scene loadedScene = SceneManager.GetSceneByPath(GameplayLabEditorLauncher.ScenePath);
                bool targetIsOpen = loadedScene.IsValid() && loadedScene.isLoaded;
                if (targetIsOpen)
                {
                    ReplaceSceneContents(scene, loadedScene);
                    if (!EditorSceneManager.SaveScene(loadedScene))
                        throw new InvalidOperationException("Gameplay Lab scene could not be saved.");
                }
                else if (!EditorSceneManager.SaveScene(scene, GameplayLabEditorLauncher.ScenePath, true))
                    throw new InvalidOperationException("Gameplay Lab scene could not be saved.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void ReplaceSceneContents(Scene source, Scene destination)
        {
            GameObject[] destinationRoots = destination.GetRootGameObjects();
            for (int i = 0; i < destinationRoots.Length; i++)
                Object.DestroyImmediate(destinationRoots[i]);
            GameObject[] sourceRoots = source.GetRootGameObjects();
            for (int i = 0; i < sourceRoots.Length; i++)
                SceneManager.MoveGameObjectToScene(sourceRoots[i], destination);
            EditorSceneManager.MarkSceneDirty(destination);
        }

        static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success || !saved)
                throw new InvalidOperationException($"Gameplay Lab Prefab could not be saved: {path}");
            return saved;
        }

        static GameObject InstantiatePrefab(string path, Scene scene)
        {
            GameObject prefab = LoadRequired<GameObject>(path);
            return PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject ??
                throw new InvalidOperationException($"Prefab could not be instantiated: {path}");
        }

        static void RemoveComponents<T>(GameObject gameObject) where T : Component
        {
            T[] components = gameObject.GetComponents<T>();
            for (int i = 0; i < components.Length; i++)
                Object.DestroyImmediate(components[i], true);
        }

        static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static T LoadRequired<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset ? asset : throw new InvalidOperationException($"Required Gameplay Lab asset is missing: {path}");
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/Configs/Simulation", "GameplayLab");
            EnsureFolder("Assets/Prefabs", "GameplayLab");
            EnsureFolder("Assets/Scenes", "GameplayLab");
        }

        static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
