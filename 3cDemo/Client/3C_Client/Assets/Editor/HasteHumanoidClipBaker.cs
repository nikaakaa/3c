using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class HasteHumanoidClipBaker
{
    const string Root = "Assets/Art/Animation/Haste/HasteMainCharacter";
    const string SourcePrefabPath = Root + "/GameObject/Courier_Retake.prefab";
    const string AvatarPath = Root + "/HUMANOID/HasteCourier_HumanoidAvatar.asset";
    const string ClipFolder = Root + "/AnimationClip";
    const string OutputClipFolder = Root + "/HUMANOID/HumanoidClips";
    const string OutputControllerFolder = Root + "/HUMANOID/HumanoidPreviewControllers";
    const string OutputPrefabFolder = Root + "/HUMANOID/HumanoidPreviewPrefabs";

    static readonly string[] PreviewClipPaths =
    {
        ClipFolder + "/New_Courier_Idle.anim",
        ClipFolder + "/New_Courier_Board_Fly_0.anim",
        ClipFolder + "/New_Courier_Board_JumpType1.anim",
    };

    [MenuItem("Tools/Haste/Bake Preview Humanoid Clips")]
    public static void BakePreviewHumanoidClips()
    {
        BakeClipSet(PreviewClipPaths, true);
    }

    [MenuItem("Tools/Haste/Bake All Courier Humanoid Clips")]
    public static void BakeAllCourierHumanoidClips()
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { ClipFolder });
        var paths = new List<string>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
                paths.Add(path);
        }

        paths.Sort(StringComparer.OrdinalIgnoreCase);
        BakeClipSet(paths.ToArray(), false);
    }

    static void BakeClipSet(string[] clipPaths, bool createPreviewPrefabs)
    {
        EnsureFolder(OutputClipFolder);
        EnsureFolder(OutputControllerFolder);
        EnsureFolder(OutputPrefabFolder);

        Avatar avatar = LoadAvatar();
        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        if (sourcePrefab == null)
            throw new FileNotFoundException(SourcePrefabPath);

        var report = new List<string>
        {
            "Haste humanoid clip bake report",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            $"Avatar: {AvatarPath}",
            string.Empty
        };

        int baked = 0;
        foreach (string clipPath in clipPaths)
        {
            AnimationClip sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (sourceClip == null)
            {
                report.Add($"Missing clip: {clipPath}");
                continue;
            }

            AnimationClip humanoidClip = BakeClip(sourcePrefab, avatar, sourceClip);
            string outputClipPath = $"{OutputClipFolder}/Humanoid_{sourceClip.name}.anim";
            SaveAsset(humanoidClip, outputClipPath);

            if (createPreviewPrefabs)
            {
                string controllerPath = $"{OutputControllerFolder}/Preview_Humanoid_{sourceClip.name}.controller";
                RuntimeAnimatorController controller = CreateSingleClipController(controllerPath, humanoidClip);
                SavePreviewPrefab(sourcePrefab, avatar, controller, sourceClip.name);
            }

            baked++;
            report.Add($"{sourceClip.name} -> {outputClipPath}");
        }

        File.WriteAllLines(ToAbsolutePath(Root + "/HUMANOID/HumanoidBakeReport.txt"), report);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[HasteHumanoidClipBaker] Baked {baked} Humanoid clips.");
    }

    static Avatar LoadAvatar()
    {
        Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
        {
            HasteHumanoidAvatarBuilder.BuildCourierHumanoidAvatar();
            avatar = AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);
        }

        if (avatar == null || !avatar.isValid || !avatar.isHuman)
            throw new InvalidOperationException("Haste Humanoid Avatar is missing or invalid.");

        return avatar;
    }

    public static AnimationClip BakeClip(GameObject sourcePrefab, Avatar avatar, AnimationClip sourceClip)
    {
        GameObject instance = null;
        try
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
            if (instance == null)
                instance = UnityEngine.Object.Instantiate(sourcePrefab);

            Transform courier = FindRequired(instance.transform, "Courier");
            SanitizeHumanoidHierarchy(courier);
            RemoveMissingScripts(instance);
            RemoveAnimator(courier.gameObject);

            var handler = new HumanPoseHandler(avatar, courier);
            int sampleRate = Mathf.Clamp(Mathf.RoundToInt(sourceClip.frameRate > 0 ? sourceClip.frameRate : 30), 1, 60);
            int steps = Mathf.Max(1, Mathf.CeilToInt(sourceClip.length * sampleRate));
            var pose = new HumanPose();
            var muscleCurves = new AnimationCurve[HumanTrait.MuscleCount];
            for (int i = 0; i < muscleCurves.Length; i++)
                muscleCurves[i] = new AnimationCurve();

            AnimationCurve rootTX = new AnimationCurve();
            AnimationCurve rootTY = new AnimationCurve();
            AnimationCurve rootTZ = new AnimationCurve();
            AnimationCurve rootQX = new AnimationCurve();
            AnimationCurve rootQY = new AnimationCurve();
            AnimationCurve rootQZ = new AnimationCurve();
            AnimationCurve rootQW = new AnimationCurve();
            Quaternion previousRoot = Quaternion.identity;
            bool hasPreviousRoot = false;

            for (int i = 0; i <= steps; i++)
            {
                float time = Mathf.Min(sourceClip.length, i / (float)sampleRate);
                sourceClip.SampleAnimation(courier.gameObject, time);
                handler.GetHumanPose(ref pose);

                for (int muscle = 0; muscle < muscleCurves.Length; muscle++)
                    muscleCurves[muscle].AddKey(time, pose.muscles[muscle]);

                Quaternion bodyRotation = pose.bodyRotation;
                if (hasPreviousRoot && Quaternion.Dot(previousRoot, bodyRotation) < 0)
                    bodyRotation = new Quaternion(-bodyRotation.x, -bodyRotation.y, -bodyRotation.z, -bodyRotation.w);

                previousRoot = bodyRotation;
                hasPreviousRoot = true;

                rootTX.AddKey(time, pose.bodyPosition.x);
                rootTY.AddKey(time, pose.bodyPosition.y);
                rootTZ.AddKey(time, pose.bodyPosition.z);
                rootQX.AddKey(time, bodyRotation.x);
                rootQY.AddKey(time, bodyRotation.y);
                rootQZ.AddKey(time, bodyRotation.z);
                rootQW.AddKey(time, bodyRotation.w);
            }

            var output = new AnimationClip
            {
                name = "Humanoid_" + sourceClip.name,
                frameRate = sampleRate,
                legacy = false,
                wrapMode = sourceClip.wrapMode
            };

            AnimationUtility.SetAnimationClipSettings(output, AnimationUtility.GetAnimationClipSettings(sourceClip));
            SetAnimatorCurve(output, "RootT.x", rootTX);
            SetAnimatorCurve(output, "RootT.y", rootTY);
            SetAnimatorCurve(output, "RootT.z", rootTZ);
            SetAnimatorCurve(output, "RootQ.x", rootQX);
            SetAnimatorCurve(output, "RootQ.y", rootQY);
            SetAnimatorCurve(output, "RootQ.z", rootQZ);
            SetAnimatorCurve(output, "RootQ.w", rootQW);

            for (int muscle = 0; muscle < muscleCurves.Length; muscle++)
                SetAnimatorCurve(output, HumanTrait.MuscleName[muscle], muscleCurves[muscle]);

            return output;
        }
        finally
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    static void SetAnimatorCurve(AnimationClip clip, string propertyName, AnimationCurve curve)
    {
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), propertyName), curve);
    }

    static RuntimeAnimatorController CreateSingleClipController(string path, AnimationClip clip)
    {
        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path) != null)
            AssetDatabase.DeleteAsset(path);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorState state = controller.layers[0].stateMachine.AddState(clip.name);
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        return controller;
    }

    static void SavePreviewPrefab(GameObject sourcePrefab, Avatar avatar, RuntimeAnimatorController controller, string sourceClipName)
    {
        GameObject instance = null;
        try
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
            if (instance == null)
                instance = UnityEngine.Object.Instantiate(sourcePrefab);

            instance.name = "Preview_Humanoid_" + sourceClipName;
            Transform courier = FindRequired(instance.transform, "Courier");
            SanitizeHumanoidHierarchy(courier);
            RemoveMissingScripts(instance);
            Animator animator = courier.GetComponent<Animator>();
            if (animator == null)
                animator = courier.gameObject.AddComponent<Animator>();

            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            if (sourceClipName.Contains("Board"))
            {
                Transform board = FindTransform(courier, "Board");
                if (board != null)
                    board.gameObject.SetActive(true);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, $"{OutputPrefabFolder}/Preview_Humanoid_{sourceClipName}.prefab");
        }
        finally
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    static void SaveAsset(UnityEngine.Object asset, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            AssetDatabase.DeleteAsset(path);

        AssetDatabase.CreateAsset(asset, path);
    }

    static Transform FindRequired(Transform root, string name)
    {
        Transform result = FindTransform(root, name);
        if (result == null)
            throw new InvalidOperationException($"Missing transform: {name}");

        return result;
    }

    static void SanitizeHumanoidHierarchy(Transform root)
    {
        var protectedNames = new HashSet<string>
        {
            "Hip", "Spine_1", "Spine_2", "Spine_3", "Neck", "Head",
            "Hip_L", "Leg_L", "Knee_L", "Foot_L",
            "Hip_R", "Leg_R", "Knee_R", "Foot_R",
            "Shoulder_L", "Arm_L", "Elbow_L", "Hand_L",
            "Shoulder_R", "Arm_R", "Elbow_R", "Hand_R"
        };

        RenameNonBoneDuplicates(root, protectedNames, false);
    }

    static void RemoveMissingScripts(GameObject root)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
    }

    static void RemoveAnimator(GameObject target)
    {
        Animator animator = target.GetComponent<Animator>();
        if (animator != null)
            UnityEngine.Object.DestroyImmediate(animator);
    }

    static void RenameNonBoneDuplicates(Transform transform, HashSet<string> protectedNames, bool underMeshes)
    {
        bool isUnderMeshes = underMeshes || transform.name == "Meshes";
        bool isRenderable = transform.GetComponent<Renderer>() != null
            || transform.GetComponent<MeshFilter>() != null
            || transform.GetComponent<ParticleSystem>() != null;

        if ((isUnderMeshes || isRenderable) && protectedNames.Contains(transform.name))
            transform.name += "_Mesh";

        for (int i = 0; i < transform.childCount; i++)
            RenameNonBoneDuplicates(transform.GetChild(i), protectedNames, isUnderMeshes);
    }

    static Transform FindTransform(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = FindTransform(root.GetChild(i), name);
            if (child != null)
                return child;
        }

        return null;
    }

    static void EnsureFolder(string assetFolder)
    {
        string[] parts = assetFolder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static string ToAbsolutePath(string assetPath)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
    }
}
