using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using BTSMTL.Timeline;
using Cinemachine;
using ThirdPersonCamera;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonGameplay.Lab;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicRollback;
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
        const string CompositionDirectory = ConfigDirectory + "/Compositions";
        const string PipelineDirectory = ConfigDirectory + "/Pipelines";
        const string SourceDirectory = ConfigDirectory + "/Sources";
        const string VariantDirectory = ConfigDirectory + "/Variants";
        const string PrefabDirectory = "Assets/Prefabs/GameplayLab";
        const string FloatCompositionPath = "Assets/Configs/Character/Corin/Pipeline/Simulation/Compositions/CorinLocalSimulationSessionComposition.asset";
        const string FixedProgramPath = "Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgram.asset";
        const string FixedRuntimePath = "Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgramRuntime.asset";
        const string FixedBackendPath = "Assets/Configs/Simulation/DeterministicRollback/Pipelines/CorinFixedPassBackend.asset";
        const string FixedSolverPath = "Assets/Configs/Simulation/DeterministicRollback/World/CorinDeterministicKcc.asset";
        const string RollbackPipelinePath = "Assets/Configs/Simulation/DeterministicRollback/Pipelines/CorinDeterministicRollbackPipeline.asset";
        const string CollisionPath = "Assets/Configs/Simulation/DeterministicRollback/World/CorinDeterministicCollisionWorld.asset";
        const string RollbackEndpointPath = "Assets/Configs/Simulation/DeterministicRollback/Networking/CorinRollbackEndpoint.asset";
        const string RollbackSourcePath = "Assets/Configs/Simulation/DeterministicRollback/Networking/CorinRollbackSessionSource.asset";
        const string RollbackCompositionPath = "Assets/Configs/Simulation/DeterministicRollback/Compositions/CorinRollbackComposition.asset";
        const string CharacterDefinitionPath = "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset";
        const string PlayerPrefabPath = "Assets/Prefabs/Characters/RuntimeProfiles/Local/CorinStandalonePlayer.prefab";
        const string TrainingEnemyPrefabPath = "Assets/Prefabs/Characters/RuntimeProfiles/AI/TrainingEnemyMonster.prefab";
        const string TrainingEnemyAttackTimelinePath = "Assets/Configs/Character/TrainingEnemy/Pipeline/Graphs/Timelines/TrainingEnemyAttackTimeline.asset";
        const string FixedPlayerProfilePrefabPath = "Assets/Prefabs/Characters/RuntimeProfiles/Local/CorinGameplayLabFixedPlayer.prefab";
        const string FixedTargetProfilePrefabPath = "Assets/Prefabs/Characters/RuntimeProfiles/Local/CorinGameplayLabFixedTarget.prefab";
        const string AnimationRigTemplatePrefabPath = "Assets/Prefabs/Characters/RuntimeProfiles/Rollback/CorinDeterministicRollback.prefab";
        const string EnvironmentPrefabPath = "Assets/Scenes/Shared/CharacterMovementTestEnvironment.prefab";
        const string FixedPipelinePath = PipelineDirectory + "/StandardFixedLocalSimulationPipeline.asset";
        const string FixedSourcePath = SourceDirectory + "/LocalFixedSimulationSessionSource.asset";
        const string FixedCompositionPath = CompositionDirectory + "/CorinGameplayLabFixedComposition.asset";
        const string FloatRootPath = PrefabDirectory + "/GameplayLabLocalFloat32.prefab";
        const string FixedRootPath = PrefabDirectory + "/GameplayLabLocalFixed.prefab";
        const string RollbackRootPath = PrefabDirectory + "/GameplayLabDeterministicRollback.prefab";
        const string FloatVariantPath = VariantDirectory + "/GameplayLabLocalFloat32Variant.asset";
        const string FixedVariantPath = VariantDirectory + "/GameplayLabLocalFixedVariant.asset";
        const string RollbackVariantPath = VariantDirectory + "/GameplayLabDeterministicRollbackVariant.asset";
        const string PlayerActorId = "gameplay-lab-player";
        const string TargetActorId = "gameplay-lab-target";
        const string ActionTargetInputId = "ActionTarget";
        const string CameraLookInputId = "LookAxis";
        const string RollbackSessionId = "corin-deterministic-rollback-demo";
        const string RollbackMapId = "deterministic-rollback-demo";
        const string RollbackLaunchArgumentPrefix = "--deterministic-rollback-profile=";
        const string SharedWorldId = "corin-gameplay-lab-world";
        const string SharedWorldRevision = "corin-gameplay-lab-world-v1";

        static readonly Vector3 s_FloatPlayerPosition = new Vector3(2.96f, 0f, -5.27f);
        static readonly Vector3 s_FixedFootIkPlayerPosition = new Vector3(
            GameplayLabFootIkRegressionCourse.CourseX,
            0f,
            GameplayLabFootIkRegressionCourse.CourseStartZ -
            GameplayLabFootIkRegressionCourse.EndpointMargin);
        static readonly Vector3 s_TargetPosition = new Vector3(2.96f, 0f, -1.7f);

        public static void Rebuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Gameplay Lab assets cannot be rebuilt in Play Mode.");
            EnsureFolders();
            NormalizeTrainingEnemyTimeline();
            CharacterRuntimeProfileRootHierarchyBuilder.Synchronize();
            CharacterPipelineDefinition definition = LoadRequired<CharacterPipelineDefinition>(CharacterDefinitionPath);
            FixedCharacterSimulationProgramAsset fixedProgram =
                FixedCharacterSimulationProgramBuildService.Build(definition, FixedProgramPath);
            CharacterPresentationProjectionAsset projection = definition.PresentationProjection
                ? definition.PresentationProjection
                : throw new InvalidOperationException("Gameplay Lab Character Definition has no published Projection.");
            SynchronizeRollbackTemplateProjection(projection);
            DeterministicKccWorldSolverDefinition solver =
                LoadRequired<DeterministicKccWorldSolverDefinition>(FixedSolverPath);
            DeterministicCollisionWorldAsset collision =
                LoadRequired<DeterministicCollisionWorldAsset>(CollisionPath);
            ValidatePublishedProducts(definition, fixedProgram, projection, solver, collision);
            BuildFixedRuntimeProfiles(definition, fixedProgram);
            SimulationSessionCompositionDefinition fixedComposition = BuildFixedComposition();
            SimulationSessionCompositionDefinition rollbackComposition = BuildRollbackComposition();
            GameObject floatRoot = BuildFloatRuntimeRoot();
            GameObject fixedRoot = BuildFixedRuntimeRoot(fixedComposition);
            GameObject rollbackRoot = BuildRollbackRuntimeRoot(rollbackComposition);
            SimulationSessionCompositionDefinition floatComposition =
                LoadRequired<SimulationSessionCompositionDefinition>(FloatCompositionPath);
            GameplayLabSessionVariantDefinition floatVariant = BuildVariant(
                FloatVariantPath,
                "gameplay-lab.local-float32",
                floatRoot,
                floatComposition,
                definition,
                definition.SimulationProgram,
                projection,
                floatComposition.WorldSolver,
                null,
                string.Empty);
            GameplayLabSessionVariantDefinition fixedVariant = BuildVariant(
                FixedVariantPath,
                "gameplay-lab.local-fixed-q32.32",
                fixedRoot,
                fixedComposition,
                definition,
                fixedProgram,
                projection,
                solver,
                collision,
                string.Empty);
            GameplayLabSessionVariantDefinition rollbackVariant = BuildVariant(
                RollbackVariantPath,
                "gameplay-lab.deterministic-rollback",
                rollbackRoot,
                rollbackComposition,
                definition,
                fixedProgram,
                projection,
                solver,
                collision,
                RollbackLaunchArgumentPrefix);
            BuildScene(fixedVariant, floatVariant, rollbackVariant);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GameplayLabEditorLauncher.Validate();
            Debug.Log("Shared Gameplay Lab synchronized: Local Fixed Q32.32, Local Float32 AI and Deterministic Rollback.");
        }

        public static void SyncFloat32EnemyVariant()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Gameplay Lab assets cannot be synchronized in Play Mode.");
            EnsureFolders();
            NormalizeTrainingEnemyTimeline();
            CharacterPipelineDefinition definition = LoadRequired<CharacterPipelineDefinition>(CharacterDefinitionPath);
            if (!definition.SimulationProgram || !definition.PresentationProjection)
                throw new InvalidOperationException("Gameplay Lab Float32 player products are missing.");
            SimulationSessionCompositionDefinition composition =
                LoadRequired<SimulationSessionCompositionDefinition>(FloatCompositionPath);
            GameObject root = BuildFloatRuntimeRoot();
            GameplayLabSessionVariantDefinition floatVariant = BuildVariant(
                FloatVariantPath,
                "gameplay-lab.local-float32",
                root,
                composition,
                definition,
                definition.SimulationProgram,
                definition.PresentationProjection,
                composition.WorldSolver,
                null,
                string.Empty);
            GameplayLabSessionVariantDefinition fixedVariant =
                LoadRequired<GameplayLabSessionVariantDefinition>(FixedVariantPath);
            GameplayLabSessionVariantDefinition rollbackVariant =
                LoadRequired<GameplayLabSessionVariantDefinition>(RollbackVariantPath);
            ScriptableObject fixedProgram = LoadRequired<FixedCharacterSimulationProgramAsset>(FixedProgramPath);
            EnsureVariantProgram(fixedVariant, fixedProgram);
            EnsureVariantProgram(rollbackVariant, fixedProgram);
            SyncSceneVariants(fixedVariant, floatVariant, rollbackVariant);
            AssetDatabase.SaveAssets();
            GameplayLabEditorLauncher.Validate();
            Debug.Log("Gameplay Lab Float32 Training Enemy synchronized without rebuilding Fixed or Rollback products.");
        }

        static void EnsureVariantProgram(
            GameplayLabSessionVariantDefinition variant,
            ScriptableObject program)
        {
            var serialized = new SerializedObject(variant);
            SerializedProperty property = serialized.FindProperty("m_Program");
            if (property.objectReferenceValue)
                return;
            property.objectReferenceValue = program;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(variant);
        }

        static void SyncSceneVariants(params GameplayLabSessionVariantDefinition[] variants)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(GameplayLabEditorLauncher.ScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(GameplayLabEditorLauncher.ScenePath, OpenSceneMode.Additive);
            try
            {
                GameplayLabBootstrap[] bootstraps = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<GameplayLabBootstrap>(true))
                    .ToArray();
                if (bootstraps.Length != 1)
                    throw new InvalidOperationException($"Gameplay Lab scene requires exactly one Bootstrap, found {bootstraps.Length}.");
                GameplayLabBootstrap bootstrap = bootstraps[0];
                string selectedId = bootstrap.Variants[bootstrap.StartupVariantIndex].VariantId;
                int selectedIndex = Array.FindIndex(
                    variants,
                    variant => string.Equals(variant.VariantId, selectedId, StringComparison.Ordinal));
                bootstrap.SetVariants(selectedIndex >= 0 ? selectedIndex : 0, variants);
                EditorUtility.SetDirty(bootstrap);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Gameplay Lab scene could not save its synchronized Variant list.");
            }
            finally
            {
                if (openedHere && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
            }
        }

        static void ValidatePublishedProducts(
            CharacterPipelineDefinition definition,
            FixedCharacterSimulationProgramAsset fixedProgram,
            CharacterPresentationProjectionAsset projection,
            DeterministicKccWorldSolverDefinition solver,
            DeterministicCollisionWorldAsset collision)
        {
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string definitionGuid = AssetDatabase.AssetPathToGUID(definitionPath);
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program = fixedProgram.Load();
            CharacterPresentationSemanticContract contract =
                FixedCharacterPresentationContractAdapter.Create(program);
            CharacterPresentationProjection published = projection.Load(contract);
            ProgramId expectedProgramId = CharacterSemanticFrontendCompiler.ComputeProgramId(definition);
            ProgramRevision expectedRevision = CharacterSemanticFrontendCompiler.ComputeSourceRevision(definition);
            if (!string.Equals(fixedProgram.DefinitionGuid, definitionGuid, StringComparison.Ordinal) ||
                !string.Equals(program.Manifest.ProgramId.Value, expectedProgramId.Value, StringComparison.Ordinal) ||
                !string.Equals(program.Manifest.SourceRevision.Value, expectedRevision.Value, StringComparison.Ordinal) ||
                !string.Equals(published.ProgramId, expectedProgramId.Value, StringComparison.Ordinal) ||
                !string.Equals(published.SourceRevision, expectedRevision.Value, StringComparison.Ordinal) ||
                !string.Equals(published.SemanticHash, program.Manifest.SemanticHash.ToString(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Gameplay Lab requires one current Fixed Program and Projection from the exact Corin Definition.");
            }
            if (solver.CollisionWorld != collision ||
                !string.Equals(solver.LoadCollisionWorld().ContentHash.Value, collision.ContentHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Gameplay Lab KCC and Collision Artifact references are split.");
            }
            _ = solver.BuildKccIdentityHash(program.Manifest.TickRate);
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
                SharedWorldId,
                mapId,
                SharedWorldRevision,
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

        static SimulationSessionCompositionDefinition BuildRollbackComposition()
        {
            FixedCharacterSimulationProgramAsset fixedProgram = LoadRequired<FixedCharacterSimulationProgramAsset>(FixedProgramPath);
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program = fixedProgram.Load();
            FixedProgramRuntimeDefinition runtime = LoadRequired<FixedProgramRuntimeDefinition>(FixedRuntimePath);
            FixedPassExecutionBackendDefinition backend = LoadRequired<FixedPassExecutionBackendDefinition>(FixedBackendPath);
            DeterministicRollbackPipelineDefinition pipeline = CreateOrLoad<DeterministicRollbackPipelineDefinition>(RollbackPipelinePath);
            DeterministicKccWorldSolverDefinition solver = LoadRequired<DeterministicKccWorldSolverDefinition>(FixedSolverPath);
            RollbackEndpointAuthoringDefinition endpoint = CreateOrLoad<RollbackEndpointAuthoringDefinition>(RollbackEndpointPath);
            ConfigureRollbackEndpoint(endpoint);
            DeterministicRollbackSessionSourceDefinition source =
                CreateOrLoad<DeterministicRollbackSessionSourceDefinition>(RollbackSourcePath);
            ConfigureRollbackSource(source, program.Manifest.TickRate, fixedProgram, pipeline, solver, endpoint);
            SimulationSessionCompositionDefinition composition =
                CreateOrLoad<SimulationSessionCompositionDefinition>(RollbackCompositionPath);
            composition.SetAuthoring(
                RollbackSessionId,
                SharedWorldId,
                RollbackMapId,
                SharedWorldRevision,
                "deterministic-rollback-local-logic",
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
            source.BuildModelDefinition();
            return composition;
        }

        static void ConfigureRollbackEndpoint(RollbackEndpointAuthoringDefinition endpoint)
        {
            var serialized = new SerializedObject(endpoint);
            serialized.FindProperty("m_RelayServerAddress").stringValue = "127.0.0.1";
            serialized.FindProperty("m_RelayServerPort").intValue = 24100;
            serialized.FindProperty("m_RelayServerPeerId").stringValue = "rollback-input-relay";
            serialized.FindProperty("m_SessionId").stringValue = RollbackSessionId;
            serialized.FindProperty("m_MaximumDatagramBytes").intValue = 1200;
            serialized.FindProperty("m_MaximumQueuedMessages").intValue = 512;
            serialized.FindProperty("m_MaximumFragmentsPerMessage").intValue = 512;
            serialized.FindProperty("m_ReliableResendMilliseconds").intValue = 50;
            serialized.FindProperty("m_InputRedundancyCount").intValue = 4;
            serialized.FindProperty("m_MaximumPreparationTicks").intValue = 1200;
            SerializedProperty profiles = serialized.FindProperty("m_PeerProfiles");
            profiles.arraySize = 2;
            ConfigureRollbackProfile(
                profiles.GetArrayElementAtIndex(0),
                "peer-a",
                "rollback-peer-a",
                "rollback-player-a",
                "rollback-actor-a",
                24101);
            ConfigureRollbackProfile(
                profiles.GetArrayElementAtIndex(1),
                "peer-b",
                "rollback-peer-b",
                "rollback-player-b",
                "rollback-actor-b",
                24102);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(endpoint);
            endpoint.Build();
            endpoint.BuildRoster();
        }

        static void ConfigureRollbackProfile(
            SerializedProperty profile,
            string profileId,
            string peerId,
            string playerId,
            string actorId,
            int port)
        {
            profile.FindPropertyRelative("m_ProfileId").stringValue = profileId;
            profile.FindPropertyRelative("m_PeerId").stringValue = peerId;
            profile.FindPropertyRelative("m_PlayerId").stringValue = playerId;
            profile.FindPropertyRelative("m_ActorId").stringValue = actorId;
            profile.FindPropertyRelative("m_LocalAddress").stringValue = "127.0.0.1";
            profile.FindPropertyRelative("m_LocalPort").intValue = port;
        }

        static void ConfigureRollbackSource(
            DeterministicRollbackSessionSourceDefinition source,
            int tickRate,
            FixedCharacterSimulationProgramAsset program,
            DeterministicRollbackPipelineDefinition pipeline,
            DeterministicKccWorldSolverDefinition solver,
            RollbackEndpointAuthoringDefinition endpoint)
        {
            var serialized = new SerializedObject(source);
            serialized.FindProperty("m_TickRate").intValue = tickRate;
            serialized.FindProperty("m_FixedProgram").objectReferenceValue = program;
            serialized.FindProperty("m_Pipeline").objectReferenceValue = pipeline;
            serialized.FindProperty("m_WorldSolver").objectReferenceValue = solver;
            serialized.FindProperty("m_Endpoint").objectReferenceValue = endpoint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(source);
        }

        static GameObject BuildFixedRuntimeRoot(
            SimulationSessionCompositionDefinition composition)
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
                    FixedPlayerProfilePrefabPath,
                    root.transform,
                    "Gameplay Lab Fixed Player",
                    s_FixedFootIkPlayerPosition,
                    Quaternion.identity,
                    sessionHost,
                    cameraRig);
                FixedCharacterHost target = InstantiateFixedActor(
                    FixedTargetProfilePrefabPath,
                    root.transform,
                    "Gameplay Lab Fixed Target",
                    s_TargetPosition,
                    Quaternion.Euler(0f, 180f, 0f),
                    sessionHost,
                    null);
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
                    "Gameplay Lab Float Player",
                    PlayerActorId,
                    s_FloatPlayerPosition,
                    Quaternion.identity,
                    sessionHost,
                    CharacterPresentationRole.LocalOwner,
                    cameraRig);
                CharacterPipelineHost enemy = InstantiateFloatActor(
                    TrainingEnemyPrefabPath,
                    root.transform,
                    "Gameplay Lab Training Enemy",
                    TargetActorId,
                    s_TargetPosition,
                    Quaternion.Euler(0f, 180f, 0f),
                    sessionHost,
                    CharacterPresentationRole.SimulatedActor,
                    null);
                if (enemy.ControlSource is not AICharacterControlSource)
                    throw new InvalidOperationException("Gameplay Lab Float enemy requires the formal AI Character Control Source.");
                SessionActorActionTargetInputProvider provider =
                    player.GetComponent<SessionActorActionTargetInputProvider>();
                if (!provider)
                    throw new InvalidOperationException("Gameplay Lab Float player requires the formal Session Actor target provider.");
                provider.SetAuthoring(enemy);
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

        static GameObject BuildRollbackRuntimeRoot(SimulationSessionCompositionDefinition composition)
        {
            CharacterPipelineDefinition definition = LoadRequired<CharacterPipelineDefinition>(CharacterDefinitionPath);
            FixedCharacterSimulationProgramAsset fixedProgram = LoadRequired<FixedCharacterSimulationProgramAsset>(FixedProgramPath);
            RollbackEndpointAuthoringDefinition endpoint = LoadRequired<RollbackEndpointAuthoringDefinition>(RollbackEndpointPath);
            Scene previous = SceneManager.GetActiveScene();
            Scene workspace = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(workspace);
                var root = new GameObject("GameplayLabDeterministicRollback");
                SimulationSessionHost sessionHost = root.AddComponent<SimulationSessionHost>();
                sessionHost.BindComposition(composition);
                ThirdPersonCameraController cameraRig = CreateCameraRig(root.transform);
                DeterministicRollbackCharacterHost actorA = InstantiateRollbackActor(
                    root.transform,
                    "Corin Rollback Actor A",
                    "rollback-actor-a",
                    "rollback-body-a",
                    new Vector3(-2f, 0f, -5f),
                    sessionHost,
                    endpoint,
                    fixedProgram,
                    definition,
                    cameraRig);
                DeterministicRollbackCharacterHost actorB = InstantiateRollbackActor(
                    root.transform,
                    "Corin Rollback Actor B",
                    "rollback-actor-b",
                    "rollback-body-b",
                    new Vector3(2f, 0f, -5f),
                    sessionHost,
                    endpoint,
                    fixedProgram,
                    definition,
                    cameraRig);
                var diagnosticsObject = new GameObject("Deterministic Rollback Diagnostics");
                diagnosticsObject.transform.SetParent(root.transform, false);
                DeterministicRollbackDemoStatusOverlay overlay =
                    diagnosticsObject.AddComponent<DeterministicRollbackDemoStatusOverlay>();
                overlay.SetActors(actorA, actorB);
                GameObject saved = SavePrefab(root, RollbackRootPath);
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

        static DeterministicRollbackCharacterHost InstantiateRollbackActor(
            Transform parent,
            string objectName,
            string actorId,
            string bodyBindingId,
            Vector3 position,
            SimulationSessionHost sessionHost,
            RollbackEndpointAuthoringDefinition endpoint,
            FixedCharacterSimulationProgramAsset fixedProgram,
            CharacterPipelineDefinition definition,
            ThirdPersonCameraController cameraRig)
        {
            GameObject instance = InstantiatePrefab(AnimationRigTemplatePrefabPath, parent.gameObject.scene);
            instance.name = objectName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.identity;
            DeterministicRollbackCharacterHost host =
                instance.GetComponent<DeterministicRollbackCharacterHost>() ??
                throw new InvalidOperationException(
                    $"Rollback Character Prefab '{AnimationRigTemplatePrefabPath}' has no DeterministicRollbackCharacterHost.");
            CharacterRootHierarchyBinding rootHierarchy = host.RootHierarchy
                ? host.RootHierarchy
                : throw new InvalidOperationException(
                    $"Rollback Character Prefab '{AnimationRigTemplatePrefabPath}' has no Root Hierarchy Binding.");
            rootHierarchy.RequireValid();
            host.SetAuthoring(
                sessionHost,
                endpoint,
                fixedProgram,
                definition.PresentationProjection,
                definition.InputProfile,
                actorId,
                bodyBindingId,
                rootHierarchy,
                cameraRig,
                CameraLookInputId);
            return host;
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
            Vector3 position,
            Quaternion rotation,
            SimulationSessionHost sessionHost,
            ThirdPersonCameraController cameraRig)
        {
            GameObject instance = InstantiatePrefab(prefabPath, parent.gameObject.scene);
            instance.name = objectName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            FixedCharacterHost fixedHost = instance.GetComponent<FixedCharacterHost>() ??
                throw new InvalidOperationException($"Fixed Character Prefab '{prefabPath}' has no FixedCharacterHost.");
            fixedHost.SetSceneAuthoring(sessionHost, cameraRig);
            return fixedHost;
        }

        static void NormalizeTrainingEnemyTimeline()
        {
            TimelineAsset timeline = LoadRequired<TimelineAsset>(TrainingEnemyAttackTimelinePath);
            foreach (AnimationTrack track in timeline.Data.Tracks.OfType<AnimationTrack>())
            {
                if (!track.AnimationChannelId.IsValid)
                    throw new InvalidOperationException("Training Enemy Animation Track has no formal Animation Channel identity.");
                track.SetAnimationChannelId(track.AnimationChannelId);
            }
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssetIfDirty(timeline);
        }

        public static void RebuildFixedCharacterPrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Gameplay Lab Fixed Character Prefabs cannot be rebuilt in Play Mode.");
            EnsureFolders();
            CharacterRuntimeProfileRootHierarchyBuilder.Synchronize();
            CharacterPipelineDefinition definition = LoadRequired<CharacterPipelineDefinition>(CharacterDefinitionPath);
            FixedCharacterSimulationProgramAsset fixedProgram =
                LoadRequired<FixedCharacterSimulationProgramAsset>(FixedProgramPath);
            SynchronizeRollbackTemplateProjection(
                definition.PresentationProjection
                    ? definition.PresentationProjection
                    : throw new InvalidOperationException("Gameplay Lab Character Definition has no published Projection."));
            SimulationSessionCompositionDefinition fixedComposition =
                LoadRequired<SimulationSessionCompositionDefinition>(FixedCompositionPath);
            BuildFixedRuntimeProfiles(definition, fixedProgram);
            BuildFixedRuntimeRoot(fixedComposition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void BuildFixedRuntimeProfiles(
            CharacterPipelineDefinition definition,
            FixedCharacterSimulationProgramAsset fixedProgram)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene workspace = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(workspace);
                BuildFixedRuntimeProfile(
                    definition,
                    fixedProgram,
                    FixedPlayerProfilePrefabPath,
                    "CorinGameplayLabFixedPlayer",
                    new ActorId(PlayerActorId),
                    CharacterPresentationRole.LocalOwner,
                    true);
                BuildFixedRuntimeProfile(
                    definition,
                    fixedProgram,
                    FixedTargetProfilePrefabPath,
                    "CorinGameplayLabFixedTarget",
                    new ActorId(TargetActorId),
                    CharacterPresentationRole.SimulatedActor,
                    false);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(workspace, true);
            }
        }

        static void SynchronizeRollbackTemplateProjection(
            CharacterPresentationProjectionAsset projection)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(AnimationRigTemplatePrefabPath);
            try
            {
                DeterministicRollbackCharacterHost host =
                    root.GetComponent<DeterministicRollbackCharacterHost>() ??
                    throw new InvalidOperationException(
                        $"Rollback Character Prefab '{AnimationRigTemplatePrefabPath}' has no DeterministicRollbackCharacterHost.");
                host.SetPresentationProjectionAuthoring(projection);
                EditorUtility.SetDirty(host);
                PrefabUtility.SaveAsPrefabAsset(root, AnimationRigTemplatePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void BuildFixedRuntimeProfile(
            CharacterPipelineDefinition definition,
            FixedCharacterSimulationProgramAsset fixedProgram,
            string outputPath,
            string objectName,
            ActorId actorId,
            CharacterPresentationRole role,
            bool playerControlled)
        {
            GameObject instance = InstantiatePrefab(PlayerPrefabPath, SceneManager.GetActiveScene());
            PrefabUtility.UnpackPrefabInstance(
                instance,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            instance.name = objectName;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            CharacterPipelineHost floatHost = instance.GetComponent<CharacterPipelineHost>() ??
                throw new InvalidOperationException($"Character Prefab '{PlayerPrefabPath}' has no CharacterPipelineHost.");
            RequireRootHierarchy(floatHost, definition);
            Float32WorldBodyBinding floatBody = floatHost.WorldBodyBinding;
            string bindingId = floatBody ? floatBody.BindingId :
                throw new InvalidOperationException($"Character Prefab '{PlayerPrefabPath}' has no World Body Binding.");
            CharacterRootHierarchyBinding rootHierarchy = floatHost.RootHierarchy;
            CharacterBodyPresentationProfile bodyPresentation = floatHost.BodyPresentationProfile;
            CharacterWorldAwarePresentationBinding worldAwarePresentation = floatHost.WorldAwarePresentation;
            CharacterEquipmentRigBindingCatalog equipment = floatHost.EquipmentRigBindings;
            AnimancerComponent animancer = floatHost.Animancer;
            CharacterAnimationRigBinding animationRigBinding = floatHost.AnimationRigBinding;
            Transform inheritedCameraAimAnchor = floatHost.CameraAimAnchor;
            Transform cameraFollowAnchor = playerControlled ? floatHost.CameraFollowAnchor : null;
            Transform cameraAimAnchor = playerControlled ? floatHost.CameraAimAnchor : null;
            IReadOnlyList<CameraTargetBinding> cameraBindings = playerControlled
                ? floatHost.CameraTargetBindings
                : Array.Empty<CameraTargetBinding>();
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
                if (inheritedCameraAimAnchor)
                    Object.DestroyImmediate(inheritedCameraAimAnchor.gameObject, true);
                controlSource = instance.AddComponent<FixedNeutralCharacterControlSource>();
            }
            FixedCharacterHost fixedHost = instance.AddComponent<FixedCharacterHost>();
            fixedHost.SetProfileAuthoring(
                fixedProgram,
                definition.PresentationProjection,
                controlSource,
                role,
                actorId,
                bindingId,
                rootHierarchy,
                bodyPresentation,
                worldAwarePresentation,
                equipment,
                animancer,
                animationRigBinding,
                cameraFollowAnchor,
                cameraAimAnchor,
                cameraBindings,
                playerControlled ? CameraLookInputId : string.Empty,
                128);
            SavePrefab(instance, outputPath);
            Object.DestroyImmediate(instance);
        }

        static void RequireRootHierarchy(
            CharacterPipelineHost host,
            CharacterPipelineDefinition definition)
        {
            CharacterRootHierarchyBinding rootHierarchy = host.RootHierarchy;
            if (!rootHierarchy)
                throw new InvalidOperationException(
                    $"Gameplay Lab Character '{host.name}' has no formal Root Hierarchy Binding.");
            rootHierarchy.RequireValid();

            CharacterAnimationRigDefinition rigDefinition = definition.AnimationPresentationProfile
                ? definition.AnimationPresentationProfile.RigDefinition
                : throw new InvalidOperationException(
                    "Gameplay Lab Character Definition has no Animation Presentation Rig.");
            var payload = new CharacterAnimationRigPayload(rigDefinition);
            CharacterAnimationRigBinding rigBinding = host.AnimationRigBinding;
            AnimancerComponent animancer = host.Animancer;
            if (!animancer || !animancer.Animator || animancer.transform != rootHierarchy.PoseRoot)
                throw new InvalidOperationException($"Gameplay Lab Character '{host.name}' PoseRoot has no formal Animancer Animator.");
            if (!rigBinding || rigBinding.transform != rootHierarchy.PoseRoot)
                throw new InvalidOperationException($"Gameplay Lab Character '{host.name}' PoseRoot has no formal Animation Rig Binding.");
            animancer.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            rigBinding.RequireValid(payload);
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
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0)
                throw new InvalidOperationException("Required layer 'Ground' is not configured.");
            collider.m_CollideAgainst = 1 << groundLayer;
            collider.m_MinimumDistanceFromTarget = 0.3f;
            collider.m_CameraRadius = 0.25f;
            collider.m_Strategy = CinemachineCollider.ResolutionStrategy.PreserveCameraHeight;
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
            SimulationSessionCompositionDefinition composition,
            CharacterPipelineDefinition definition,
            ScriptableObject programArtifact,
            CharacterPresentationProjectionAsset projection,
            SimulationWorldSolverDefinition worldSolver,
            ScriptableObject collision,
            string externalLaunchArgumentPrefix)
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
                solver.Identity.ComponentId,
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition)),
                composition,
                programArtifact,
                projection,
                worldSolver,
                collision,
                externalLaunchArgumentPrefix);
            variant.ValidateComposition(composition);
            EditorUtility.SetDirty(variant);
            return variant;
        }

        static void BuildScene(
            GameplayLabSessionVariantDefinition fixedVariant,
            GameplayLabSessionVariantDefinition floatVariant,
            GameplayLabSessionVariantDefinition rollbackVariant)
        {
            BuildSharedScene(new[] { fixedVariant, floatVariant, rollbackVariant });
        }

        static void BuildSharedScene(GameplayLabSessionVariantDefinition[] variants)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                GameObject environment = InstantiatePrefab(EnvironmentPrefabPath, scene);
                environment.name = "Character Movement Test Environment";
                ConfigureDeterministicWorld(environment);
                var lightObject = new GameObject("Gameplay Lab Directional Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                if (variants.Length != 0)
                {
                    var bootstrapObject = new GameObject("Gameplay Lab Bootstrap");
                    GameplayLabBootstrap bootstrap = bootstrapObject.AddComponent<GameplayLabBootstrap>();
                    bootstrap.SetVariants(0, variants);
                }
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

        static void ConfigureDeterministicWorld(GameObject environment)
        {
            DeterministicCollisionWorldAsset collision = CreateOrLoad<DeterministicCollisionWorldAsset>(CollisionPath);
            DeterministicCollisionWorldAuthoring world =
                environment.GetComponent<DeterministicCollisionWorldAuthoring>() ??
                environment.AddComponent<DeterministicCollisionWorldAuthoring>();
            var worldSerialized = new SerializedObject(world);
            worldSerialized.FindProperty("m_MapId").stringValue = RollbackMapId;
            worldSerialized.FindProperty("m_QuantizationUnitsPerMeter").intValue = 1000;
            worldSerialized.FindProperty("m_WorldBoundsCenter").vector3Value = new Vector3(50f, 8f, 15f);
            worldSerialized.FindProperty("m_WorldBoundsSize").vector3Value = new Vector3(400f, 60f, 240f);
            worldSerialized.FindProperty("m_Output").objectReferenceValue = collision;
            worldSerialized.ApplyModifiedPropertiesWithoutUndo();
            DeterministicCollisionSurfaceAuthoring broadSurface =
                environment.GetComponent<DeterministicCollisionSurfaceAuthoring>();
            if (broadSurface)
                Object.DestroyImmediate(broadSurface);
            if (environment.GetComponentsInChildren<DeterministicCollisionSurfaceAuthoring>(true).Length == 0)
                throw new InvalidOperationException("Gameplay Lab environment Prefab has no explicit deterministic surface authoring roots.");
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
            EnsureFolder(ConfigDirectory, "Compositions");
            EnsureFolder(ConfigDirectory, "Pipelines");
            EnsureFolder(ConfigDirectory, "Sources");
            EnsureFolder(ConfigDirectory, "Variants");
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
