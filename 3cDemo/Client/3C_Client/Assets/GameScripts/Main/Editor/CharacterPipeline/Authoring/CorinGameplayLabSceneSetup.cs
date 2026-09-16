using System;
using TEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonCharacter.Pipeline.ThirdPersonCamera;
using Cinemachine;

public static class CorinGameplayLabSceneSetup
{
    const string CompositionGuid = "e381b64b19ae421aa92161f8c0cbd5d4";
    const string PrefabPath = "Assets/Prefabs/Characters/RuntimeProfiles/Local/CorinGameplayLabFixedPlayer.prefab";

    [MenuItem("3C/Setup/Corin GameplayLab Fixed Scene", priority = 0)]
    public static void SetupScene()
    {
        var composition = LoadAssetByGuid<SimulationSessionCompositionDefinition>(CompositionGuid);
        if (composition == null)
            throw new InvalidOperationException("CorinGameplayLabFixedComposition asset not found.");

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            throw new InvalidOperationException($"CorinGameplayLabFixedPlayer prefab not found at '{PrefabPath}'.");

        EnsureGround();
        EnsureMainCamera();

        var sessionGo = new GameObject("SimulationSession");
        var sessionHost = sessionGo.AddComponent<SimulationSessionHost>();
        sessionHost.BindComposition(composition);

        var characterGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        characterGo.name = "Corin";
        characterGo.transform.position = new Vector3(0f, 0.1f, 0f);
        var fixedHost = characterGo.GetComponent<FixedCharacterHost>();
        if (fixedHost == null)
            throw new InvalidOperationException("Corin prefab is missing FixedCharacterHost.");

        var cameraRig = EnsureCameraRig(characterGo.transform);
        var so = new SerializedObject(fixedHost);
        so.FindProperty("m_SessionHost").objectReferenceValue = sessionHost;
        so.FindProperty("m_CameraRig").objectReferenceValue = cameraRig;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(fixedHost);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = characterGo;
        Debug.Log("Corin GameplayLab setup complete. Press Play to test.");
    }

    [MenuItem("3C/Setup/Initialize YooAsset Editor Mode", priority = 1)]
    public static void InitializeYooAsset()
    {
        IResourceModule resourceModule = ModuleSystem.GetModule<IResourceModule>();
        if (!YooAssets.Initialized)
            resourceModule.Initialize();
        Debug.Log($"YooAsset initialized. DefaultPackage: {resourceModule.DefaultPackageName}, PlayMode: {resourceModule.PlayMode}");
    }

    static CinemachineCameraRigAdapter EnsureCameraRig(Transform characterRoot)
    {
        var existing = FindObjectOfType<CinemachineCameraRigAdapter>();
        if (existing != null)
            return existing;

        var cameraGo = new GameObject("Main Camera");
        var camera = cameraGo.AddComponent<Camera>();
        camera.tag = "MainCamera";
        cameraGo.transform.position = new Vector3(0f, 3f, -5f);
        cameraGo.AddComponent<CinemachineBrain>();

        var vcamGo = new GameObject("CM_VirtualCamera");
        vcamGo.transform.SetParent(cameraGo.transform);
        var vcam = vcamGo.AddComponent<CinemachineVirtualCamera>();
        vcam.Priority = 10;

        var rigGo = new GameObject("CameraRig");
        var adapter = rigGo.AddComponent<CinemachineCameraRigAdapter>();
        adapter.VirtualCamera = vcam;
        adapter.Brain = cameraGo.GetComponent<CinemachineBrain>();

        return adapter;
    }

    static void EnsureMainCamera()
    {
        if (Camera.main != null)
            return;
    }

    static void EnsureGround()
    {
        if (GameObject.Find("Ground") != null)
            return;
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(10f, 1f, 10f);
    }

    static T LoadAssetByGuid<T>(string guid) where T : UnityEngine.Object
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
            return null;
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
