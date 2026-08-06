using System;
using System.IO;
using Animancer;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class TrainingEnemyRuntimeSceneBuilder
    {
        const string DefinitionPath = "Assets/Configs/Character/TrainingEnemy/Pipeline/Definition/TrainingEnemyCharacterPipelineDefinition.asset";
        const string AiDefinitionPath = "Assets/Configs/Character/TrainingEnemy/Pipeline/AI/Authoring/TrainingEnemyAIControllerDefinition.asset";
        const string PresentationPrefabPath = "Assets/Prefabs/Characters/Presentation/AI/TrainingEnemyMonsterPresentation.prefab";
        const string RuntimePrefabPath = "Assets/Prefabs/Characters/RuntimeProfiles/AI/TrainingEnemyMonster.prefab";
        const string BodyPresentationPath = "Assets/Configs/Character/TrainingEnemy/Presentation/Profile/TrainingEnemyObservedBodyPresentationProfile.asset";
        const string ActorId = "training-enemy-monster";

        [MenuItem("Tools/3C/Characters/Build Training Enemy Runtime Prefab")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Training Enemy runtime authoring cannot run in Play Mode.");
            CharacterPipelineDefinition definition = RequireAsset<CharacterPipelineDefinition>(DefinitionPath);
            AIControllerDefinition ai = RequireAsset<AIControllerDefinition>(AiDefinitionPath);
            if (!definition.SimulationProgram || !definition.PresentationProjection || !ai.IntentProgram)
                throw new InvalidOperationException("Training Enemy Character and AI generated products must be published first.");
            CharacterBodyPresentationProfile bodyPresentation = BuildBodyPresentationProfile();
            BuildRuntimePrefab(definition, ai, bodyPresentation);
            AssetDatabase.SaveAssets();
            Debug.Log("Training Enemy runtime Prefab created for formal Gameplay Lab variants.");
        }

        static CharacterBodyPresentationProfile BuildBodyPresentationProfile()
        {
            EnsureFolder(Path.GetDirectoryName(BodyPresentationPath)?.Replace('\\', '/'));
            CharacterBodyPresentationProfile profile = AssetDatabase.LoadAssetAtPath<CharacterBodyPresentationProfile>(BodyPresentationPath);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<CharacterBodyPresentationProfile>();
                profile.name = "Training Enemy Observed Body Presentation Profile";
                AssetDatabase.CreateAsset(profile, BodyPresentationPath);
            }
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("m_TrajectoryMode").intValue = (int)CharacterVisualTrajectoryMode.BoundedCorrection;
            serialized.FindProperty("m_PositionHalfLifeSeconds").floatValue = 0.06f;
            serialized.FindProperty("m_MaximumHorizontalErrorMeters").floatValue = 0.25f;
            serialized.FindProperty("m_PositionSettleDistanceMeters").floatValue = 0.005f;
            serialized.FindProperty("m_YawHalfLifeSeconds").floatValue = 0.05f;
            serialized.FindProperty("m_MaximumYawErrorDegrees").floatValue = 18f;
            serialized.FindProperty("m_YawSettleDegrees").floatValue = 0.25f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static void BuildRuntimePrefab(
            CharacterPipelineDefinition definition,
            AIControllerDefinition ai,
            CharacterBodyPresentationProfile bodyPresentation)
        {
            EnsureFolder(Path.GetDirectoryName(RuntimePrefabPath)?.Replace('\\', '/'));
            GameObject presentation = RequireAsset<GameObject>(PresentationPrefabPath);
            Scene previousActive = SceneManager.GetActiveScene();
            Scene temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var instance = new GameObject("TrainingEnemyMonster")
            {
                layer = RequireCharacterLayer()
            };
            SceneManager.MoveGameObjectToScene(instance, temporary);
            try
            {
                CharacterController characterController = instance.AddComponent<CharacterController>();
                UnityCharacterControllerWorldBodyBinding worldBody = instance.AddComponent<UnityCharacterControllerWorldBodyBinding>();
                AICharacterControlSource control = instance.AddComponent<AICharacterControlSource>();
                CharacterPipelineHost host = instance.AddComponent<CharacterPipelineHost>();

                var visualRootObject = new GameObject("EnemyVisualRoot");
                visualRootObject.layer = instance.layer;
                Transform visualRoot = visualRootObject.transform;
                visualRoot.SetParent(instance.transform, false);
                GameObject presentationInstance = PrefabUtility.InstantiatePrefab(presentation, visualRoot) as GameObject ??
                    throw new InvalidOperationException("Training Enemy Presentation Prefab could not be instantiated.");
                presentationInstance.name = "MonsterPresentation";
                presentationInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                presentationInstance.transform.localScale = Vector3.one * 0.5f;
                SetLayerRecursively(presentationInstance, instance.layer);

                Animator animator = presentationInstance.GetComponentInChildren<Animator>(true) ??
                    throw new InvalidOperationException("Training Enemy Presentation has no Animator.");
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                CharacterAnimationRigBinding rigBinding = animator.GetComponent<CharacterAnimationRigBinding>() ??
                    throw new InvalidOperationException("Training Enemy Presentation has no CharacterAnimationRigBinding.");
                AnimancerComponent animancer = animator.GetComponent<AnimancerComponent>() ??
                    animator.gameObject.AddComponent<AnimancerComponent>();
                var animancerSerialized = new SerializedObject(animancer);
                animancerSerialized.FindProperty("_Animator").objectReferenceValue = animator;
                animancerSerialized.ApplyModifiedPropertiesWithoutUndo();

                var controlSerialized = new SerializedObject(control);
                controlSerialized.FindProperty("m_Controller").objectReferenceValue = ai;
                controlSerialized.ApplyModifiedPropertiesWithoutUndo();

                ConfigureCharacterController(instance.transform, characterController, presentationInstance);
                worldBody.ConfigurePreview("TrainingEnemy.MonsterBody", new ActorId(ActorId), characterController, instance.transform);
                CharacterWorldAwarePresentationBinding worldAware = instance.AddComponent<CharacterWorldAwarePresentationBinding>();
                worldAware.Configure(visualRoot, instance.transform);

                var hostSerialized = new SerializedObject(host);
                hostSerialized.FindProperty("m_Definition").objectReferenceValue = definition;
                hostSerialized.FindProperty("m_ActorId").stringValue = ActorId;
                hostSerialized.FindProperty("m_ControlSource").objectReferenceValue = control;
                hostSerialized.FindProperty("m_PresentationRole").intValue = (int)CharacterPresentationRole.SimulatedActor;
                hostSerialized.FindProperty("m_Animancer").objectReferenceValue = animancer;
                hostSerialized.FindProperty("m_AnimationRigBinding").objectReferenceValue = rigBinding;
                hostSerialized.FindProperty("m_WorldBodyBinding").objectReferenceValue = worldBody;
                hostSerialized.FindProperty("m_VisualRoot").objectReferenceValue = visualRoot;
                hostSerialized.FindProperty("m_EquipmentRigBindings").objectReferenceValue = null;
                hostSerialized.FindProperty("m_EquipmentPreviewFixture").objectReferenceValue = null;
                hostSerialized.FindProperty("m_BodyPresentationProfile").objectReferenceValue = bodyPresentation;
                hostSerialized.FindProperty("m_WorldAwarePresentation").objectReferenceValue = worldAware;
                hostSerialized.FindProperty("m_CameraRig").objectReferenceValue = null;
                hostSerialized.FindProperty("m_CameraFollowAnchor").objectReferenceValue = null;
                hostSerialized.FindProperty("m_CameraAimAnchor").objectReferenceValue = null;
                hostSerialized.FindProperty("m_CameraTargetBindings").arraySize = 0;
                hostSerialized.FindProperty("m_CameraLookInputValueId").stringValue = string.Empty;
                hostSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(host);
                EditorUtility.SetDirty(worldBody);
                EditorUtility.SetDirty(worldAware);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, RuntimePrefabPath) ??
                    throw new InvalidOperationException("Training Enemy runtime Prefab could not be saved.");
                CharacterPipelineHost savedHost = prefab.GetComponent<CharacterPipelineHost>();
                if (!savedHost || savedHost.Definition != definition || savedHost.ActorId != ActorId || savedHost.ControlSource is not AICharacterControlSource)
                    throw new InvalidOperationException("Training Enemy runtime Prefab did not retain its formal Character and AI bindings.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
                EditorSceneManager.CloseScene(temporary, true);
                if (previousActive.IsValid() && previousActive.isLoaded)
                    SceneManager.SetActiveScene(previousActive);
            }
        }

        static void ConfigureCharacterController(Transform root, CharacterController controller, GameObject presentation)
        {
            Renderer[] renderers = presentation.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Training Enemy Presentation has no Renderer bounds for CharacterController authoring.");
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            Vector3 localMin = root.InverseTransformPoint(bounds.min);
            Vector3 localMax = root.InverseTransformPoint(bounds.max);
            Vector3 size = localMax - localMin;
            float height = Mathf.Max(1f, size.y);
            float radius = Mathf.Max(0.2f, Mathf.Min(Mathf.Max(size.x, size.z) * 0.5f, height * 0.45f));
            controller.height = Mathf.Max(height, radius * 2f + 0.02f);
            controller.radius = radius;
            controller.center = new Vector3(
                (localMin.x + localMax.x) * 0.5f,
                localMin.y + controller.height * 0.5f,
                (localMin.z + localMax.z) * 0.5f);
            controller.slopeLimit = 50f;
            controller.stepOffset = Mathf.Min(0.4f, controller.height * 0.2f);
            controller.skinWidth = 0.05f;
            EditorUtility.SetDirty(controller);
        }

        static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                transform.gameObject.layer = layer;
        }

        static int RequireCharacterLayer()
        {
            int layer = LayerMask.NameToLayer("Player");
            return layer >= 0
                ? layer
                : throw new InvalidOperationException("Training Enemy requires the formal Player character collision layer.");
        }

        static T RequireAsset<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException($"Required asset is missing: {path}");

        static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException($"Invalid asset folder '{path}'.");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
