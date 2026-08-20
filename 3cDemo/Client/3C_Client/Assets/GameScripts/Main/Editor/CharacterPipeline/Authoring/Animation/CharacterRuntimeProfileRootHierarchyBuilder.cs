using System;
using System.Collections.Generic;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback;
using ThirdPersonGameplay.Networking.ServerAuthoritative;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterRuntimeProfileRootHierarchyBuilder
    {
        const string LocalCorinPath = "Assets/Prefabs/Characters/RuntimeProfiles/Local/CorinStandalonePlayer.prefab";
        const string RollbackCorinPath = "Assets/Prefabs/Characters/RuntimeProfiles/Rollback/CorinDeterministicRollback.prefab";
        const string TrainingEnemyPath = "Assets/Prefabs/Characters/RuntimeProfiles/AI/TrainingEnemyMonster.prefab";
        const string UnityAuthorityCorinPath = "Assets/Prefabs/Characters/RuntimeProfiles/ServerAuthoritative/UnityAuthority/CorinServerAuthoritativeUnityClient.prefab";
        const string DotRecastCorinPath = "Assets/Prefabs/Characters/RuntimeProfiles/ServerAuthoritative/DotRecast/CorinServerAuthoritativeDotRecastClient.prefab";
        const string UnityAuthorityClientScenePath = "Assets/Scenes/ServerAuthoritative/ServerAuthoritativeClient.unity";
        const string DotRecastClientScenePath = "Assets/Scenes/ServerAuthoritative/DotRecastAuthorityClient.unity";

        static readonly string[] PipelineProfiles =
        {
            LocalCorinPath,
            TrainingEnemyPath,
            UnityAuthorityCorinPath,
            DotRecastCorinPath
        };

        [MenuItem("Tools/3C/Characters/Synchronize Runtime Root Hierarchies")]
        public static void Synchronize()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Character Runtime Profile roots cannot be synchronized in Play Mode.");
            SynchronizePipelineProfile(LocalCorinPath, null);
            GameObject templateRoot = PrefabUtility.LoadPrefabContents(LocalCorinPath);
            try
            {
                CharacterPipelineHost templateHost = RequirePipelineHost(templateRoot, LocalCorinPath);
                CharacterAnimationRigBinding templateRig = templateHost.AnimationRigBinding
                    ? templateHost.AnimationRigBinding
                    : throw new InvalidOperationException("Local Corin Runtime Profile has no Animation Rig Binding.");
                for (int i = 1; i < PipelineProfiles.Length; i++)
                    SynchronizePipelineProfile(PipelineProfiles[i], templateRig);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(templateRoot);
            }
            SynchronizeRollbackProfile();
            SynchronizeRemoteTemplateScene(UnityAuthorityClientScenePath, UnityAuthorityCorinPath);
            SynchronizeRemoteTemplateScene(DotRecastClientScenePath, DotRecastCorinPath);
            AssetDatabase.SaveAssets();
        }

        static void SynchronizePipelineProfile(
            string path,
            CharacterAnimationRigBinding templateRig)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                CharacterPipelineHost host = RequirePipelineHost(root, path);
                if (!host.WorldBodyBinding || !host.Animancer || !host.Animancer.Animator || !host.WorldAwarePresentation)
                    throw new InvalidOperationException($"Character Runtime Profile '{path}' has incomplete root inputs.");
                Transform logicRoot = host.WorldBodyBinding is UnityCharacterControllerWorldBodyBinding ccBinding
                    ? ccBinding.LogicRoot
                    : root.transform;
                Transform poseRoot = host.Animancer.Animator.transform;
                Transform visualRoot = host.WorldAwarePresentation.PresentationRoot;
                CharacterWorldAwarePresentationBinding worldAware = host.WorldAwarePresentation;
                if (poseRoot == visualRoot)
                {
                    visualRoot = InsertVisualRoot(logicRoot, poseRoot);
                    worldAware = MoveWorldAwareBinding(worldAware, visualRoot, logicRoot);
                }
                else if (poseRoot.parent != visualRoot || visualRoot.parent != logicRoot)
                {
                    throw new InvalidOperationException($"Character Runtime Profile '{path}' has a non-canonical root hierarchy.");
                }
                visualRoot.name = "VisualRoot";
                poseRoot.name = "PoseRoot";
                CharacterAnimationRigBinding rigBinding = EnsureRigBinding(
                    host,
                    poseRoot,
                    templateRig);
                CharacterRootHierarchyBinding hierarchy =
                    root.GetComponent<CharacterRootHierarchyBinding>() ??
                    root.AddComponent<CharacterRootHierarchyBinding>();
                hierarchy.Configure(logicRoot, visualRoot, poseRoot);
                Transform aimAnchor = host.CameraAimAnchor;
                if (aimAnchor)
                    aimAnchor.SetParent(visualRoot, false);
                var serialized = new SerializedObject(host);
                serialized.FindProperty("m_RootHierarchy").objectReferenceValue = hierarchy;
                serialized.FindProperty("m_AnimationRigBinding").objectReferenceValue = rigBinding;
                serialized.FindProperty("m_WorldAwarePresentation").objectReferenceValue = worldAware;
                if (host.PresentationRole == CharacterPresentationRole.LocalOwner)
                {
                    serialized.FindProperty("m_CameraFollowAnchor").objectReferenceValue = visualRoot;
                    serialized.FindProperty("m_CameraAimAnchor").objectReferenceValue = aimAnchor;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void SynchronizeRollbackProfile()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(RollbackCorinPath);
            try
            {
                DeterministicRollbackCharacterHost host =
                    root.GetComponent<DeterministicRollbackCharacterHost>() ??
                    throw new InvalidOperationException("Rollback Corin Runtime Profile has no Rollback Host.");
                CharacterRootHierarchyBinding hierarchy = host.RootHierarchy
                    ? host.RootHierarchy
                    : root.GetComponent<CharacterRootHierarchyBinding>();
                if (!hierarchy)
                    throw new InvalidOperationException("Rollback Corin Runtime Profile has no Root Hierarchy Binding.");
                hierarchy.RequireValid();
                if (!host.AnimationRigBinding || host.AnimationRigBinding.transform != hierarchy.PoseRoot)
                    throw new InvalidOperationException("Rollback Corin Animation Rig Binding must belong to PoseRoot.");
                Transform aimAnchor = host.CameraAimAnchor;
                if (aimAnchor)
                    aimAnchor.SetParent(hierarchy.VisualRoot, false);
                var serialized = new SerializedObject(host);
                serialized.FindProperty("m_RootHierarchy").objectReferenceValue = hierarchy;
                serialized.FindProperty("m_CameraFollowAnchor").objectReferenceValue = hierarchy.VisualRoot;
                serialized.FindProperty("m_CameraAimAnchor").objectReferenceValue = aimAnchor;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, RollbackCorinPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void SynchronizeRemoteTemplateScene(string scenePath, string prefabPath)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                ServerAuthoritativeRemotePresentationSite site = null;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    ServerAuthoritativeRemotePresentationSite[] candidates =
                        roots[i].GetComponentsInChildren<ServerAuthoritativeRemotePresentationSite>(true);
                    for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                    {
                        if (site)
                            throw new InvalidOperationException($"ServerAuthoritative Scene '{scenePath}' has multiple Remote Presentation Sites.");
                        site = candidates[candidateIndex];
                    }
                }
                if (!site)
                    throw new InvalidOperationException($"ServerAuthoritative Scene '{scenePath}' has no Remote Presentation Site.");
                GameObject characterTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) ??
                    throw new InvalidOperationException($"Remote Character Template is missing: {prefabPath}");
                var serialized = new SerializedObject(site);
                serialized.FindProperty("m_CharacterTemplate").objectReferenceValue = characterTemplate;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException($"ServerAuthoritative Scene could not be saved: {scenePath}");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
            }
        }

        static Transform InsertVisualRoot(Transform logicRoot, Transform poseRoot)
        {
            if (poseRoot.parent != logicRoot)
                throw new InvalidOperationException("Pose root can only be migrated when it is a direct child of LogicRoot.");
            var visualObject = new GameObject("VisualRoot")
            {
                layer = poseRoot.gameObject.layer
            };
            Transform visualRoot = visualObject.transform;
            visualRoot.SetParent(logicRoot, false);
            poseRoot.SetParent(visualRoot, false);
            return visualRoot;
        }

        static CharacterWorldAwarePresentationBinding MoveWorldAwareBinding(
            CharacterWorldAwarePresentationBinding current,
            Transform visualRoot,
            Transform logicRoot)
        {
            CharacterWorldAwarePresentationBinding replacement =
                visualRoot.gameObject.AddComponent<CharacterWorldAwarePresentationBinding>();
            replacement.Configure(visualRoot, logicRoot);
            Object.DestroyImmediate(current, true);
            return replacement;
        }

        static CharacterAnimationRigBinding EnsureRigBinding(
            CharacterPipelineHost host,
            Transform poseRoot,
            CharacterAnimationRigBinding templateRig)
        {
            CharacterAnimationRigDefinition rigDefinition = host.Definition && host.Definition.AnimationPresentationProfile
                ? host.Definition.AnimationPresentationProfile.RigDefinition
                : throw new InvalidOperationException($"Character Runtime Profile '{host.name}' has no Animation Rig Definition.");
            var payload = new CharacterAnimationRigPayload(rigDefinition);
            CharacterAnimationRigBinding current = host.AnimationRigBinding;
            if (current && current.transform == poseRoot)
            {
                current.RequireValid(payload);
                return current;
            }
            Transform[] physicalBones;
            if (current)
            {
                physicalBones = new Transform[current.PhysicalBones.Count];
                for (int i = 0; i < physicalBones.Length; i++)
                    physicalBones[i] = current.PhysicalBones[i];
                Object.DestroyImmediate(current, true);
            }
            else
            {
                if (!templateRig)
                    throw new InvalidOperationException($"Character Runtime Profile '{host.name}' has no Rig Binding template.");
                physicalBones = MapPhysicalBones(templateRig, poseRoot);
            }
            CharacterAnimationRigBinding binding = poseRoot.gameObject.AddComponent<CharacterAnimationRigBinding>();
            binding.Configure(host.Animancer.Animator, payload, physicalBones);
            host.ConfigureAnimationRigBinding(binding);
            return binding;
        }

        static Transform[] MapPhysicalBones(
            CharacterAnimationRigBinding template,
            Transform targetPoseRoot)
        {
            Transform sourcePoseRoot = template.Animator.transform;
            var result = new Transform[template.PhysicalBones.Count];
            for (int i = 0; i < result.Length; i++)
            {
                string path = AnimationUtility.CalculateTransformPath(template.PhysicalBones[i], sourcePoseRoot);
                Transform target = string.IsNullOrEmpty(path)
                    ? targetPoseRoot
                    : targetPoseRoot.Find(path);
                if (!target)
                    throw new InvalidOperationException($"Character PoseRoot is missing Rig Bone path '{path}'.");
                result[i] = target;
            }
            return result;
        }

        static CharacterPipelineHost RequirePipelineHost(GameObject root, string path) =>
            root.GetComponent<CharacterPipelineHost>() ??
            throw new InvalidOperationException($"Character Runtime Profile '{path}' has no CharacterPipelineHost.");
    }
}
