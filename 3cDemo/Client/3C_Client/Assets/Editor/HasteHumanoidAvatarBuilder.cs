using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HasteHumanoidAvatarBuilder
{
    const string Root = "Assets/Art/Animation/Haste/HasteMainCharacter";
    const string SourcePrefabPath = Root + "/GameObject/Courier_Retake.prefab";
    const string OutputFolder = Root + "/HUMANOID";
    const string AvatarPath = OutputFolder + "/HasteCourier_HumanoidAvatar.asset";
    const string BaseHumanoidPrefabPath = OutputFolder + "/HasteCourier_Humanoid.prefab";

    static readonly PreviewSpec[] PreviewSpecs =
    {
        new PreviewSpec("Idle", Root + "/AnimatorController/Preview_Courier_Idle.controller", OutputFolder + "/Preview_Humanoid_Courier_Idle.prefab", false),
        new PreviewSpec("Board_Fly_0", Root + "/AnimatorController/Preview_Courier_Board_Fly_0.controller", OutputFolder + "/Preview_Humanoid_Courier_Board_Fly_0.prefab", true),
        new PreviewSpec("Board_JumpType1", Root + "/AnimatorController/Preview_Courier_Board_JumpType1.controller", OutputFolder + "/Preview_Humanoid_Courier_Board_JumpType1.prefab", true),
    };

    [MenuItem("Tools/Haste/Build Courier Humanoid Avatar")]
    public static void BuildCourierHumanoidAvatar()
    {
        EnsureFolder(OutputFolder);

        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        if (sourcePrefab == null)
            throw new FileNotFoundException(SourcePrefabPath);

        GameObject instance = null;
        try
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
            if (instance == null)
                instance = UnityEngine.Object.Instantiate(sourcePrefab);

            Transform courier = FindRequired(instance.transform, "Courier");
            SanitizeHumanoidHierarchy(courier);
            RemoveMissingScripts(instance);
            Animator animator = EnsureAnimator(courier.gameObject);
            Avatar avatar = BuildFirstValidAvatar(courier);
            SaveAvatar(avatar);

            animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);
            animator.applyRootMotion = false;
            PrefabUtility.SaveAsPrefabAsset(instance, BaseHumanoidPrefabPath);

            foreach (PreviewSpec spec in PreviewSpecs)
                SavePreviewPrefab(sourcePrefab, avatar, spec);

            WriteReport();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HasteHumanoidAvatarBuilder] Built Humanoid avatar and preview prefabs.");
        }
        finally
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    static Avatar BuildFirstValidAvatar(Transform courier)
    {
        string[] labels = { "LegKneeFoot", "HipLegFoot", "HipLegKnee" };
        Dictionary<HumanBodyBones, string>[] candidates =
        {
            CreateHumanMap("Leg_L", "Knee_L", "Foot_L", "Leg_R", "Knee_R", "Foot_R"),
            CreateHumanMap("Hip_L", "Leg_L", "Foot_L", "Hip_R", "Leg_R", "Foot_R"),
            CreateHumanMap("Hip_L", "Leg_L", "Knee_L", "Hip_R", "Leg_R", "Knee_R"),
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            HumanDescription description = CreateHumanDescription(courier, candidates[i]);
            Avatar avatar = AvatarBuilder.BuildHumanAvatar(courier.gameObject, description);
            avatar.name = "HasteCourier_Humanoid";

            if (avatar.isValid && avatar.isHuman)
            {
                Debug.Log($"[HasteHumanoidAvatarBuilder] Humanoid mapping: {labels[i]}");
                return avatar;
            }

            UnityEngine.Object.DestroyImmediate(avatar);
        }

        throw new InvalidOperationException("Could not build a valid Humanoid avatar for Courier_Retake.");
    }

    static Dictionary<HumanBodyBones, string> CreateHumanMap(
        string leftUpperLeg,
        string leftLowerLeg,
        string leftFoot,
        string rightUpperLeg,
        string rightLowerLeg,
        string rightFoot)
    {
        return new Dictionary<HumanBodyBones, string>
        {
            { HumanBodyBones.Hips, "Hip" },
            { HumanBodyBones.Spine, "Spine_1" },
            { HumanBodyBones.Chest, "Spine_2" },
            { HumanBodyBones.UpperChest, "Spine_3" },
            { HumanBodyBones.Neck, "Neck" },
            { HumanBodyBones.Head, "Head" },
            { HumanBodyBones.LeftUpperLeg, leftUpperLeg },
            { HumanBodyBones.LeftLowerLeg, leftLowerLeg },
            { HumanBodyBones.LeftFoot, leftFoot },
            { HumanBodyBones.RightUpperLeg, rightUpperLeg },
            { HumanBodyBones.RightLowerLeg, rightLowerLeg },
            { HumanBodyBones.RightFoot, rightFoot },
            { HumanBodyBones.LeftShoulder, "Shoulder_L" },
            { HumanBodyBones.LeftUpperArm, "Arm_L" },
            { HumanBodyBones.LeftLowerArm, "Elbow_L" },
            { HumanBodyBones.LeftHand, "Hand_L" },
            { HumanBodyBones.RightShoulder, "Shoulder_R" },
            { HumanBodyBones.RightUpperArm, "Arm_R" },
            { HumanBodyBones.RightLowerArm, "Elbow_R" },
            { HumanBodyBones.RightHand, "Hand_R" },
        };
    }

    static HumanDescription CreateHumanDescription(Transform courier, Dictionary<HumanBodyBones, string> map)
    {
        var humanBones = new List<HumanBone>();
        foreach (KeyValuePair<HumanBodyBones, string> pair in map)
        {
            if (FindInSkeleton(courier, pair.Value) == null)
                throw new InvalidOperationException($"Missing bone: {pair.Value}");

            humanBones.Add(new HumanBone
            {
                humanName = HumanTrait.BoneName[(int)pair.Key],
                boneName = pair.Value,
                limit = new HumanLimit { useDefaultValues = true }
            });
        }

        return new HumanDescription
        {
            human = humanBones.ToArray(),
            skeleton = BuildSkeleton(courier).ToArray(),
            upperArmTwist = 0.5f,
            lowerArmTwist = 0.5f,
            upperLegTwist = 0.5f,
            lowerLegTwist = 0.5f,
            armStretch = 0.05f,
            legStretch = 0.05f,
            feetSpacing = 0,
            hasTranslationDoF = false
        };
    }

    static List<SkeletonBone> BuildSkeleton(Transform root)
    {
        var bones = new List<SkeletonBone>();
        var usedNames = new HashSet<string>();
        AddSkeletonBone(root, bones, usedNames);
        return bones;
    }

    static void AddSkeletonBone(Transform transform, List<SkeletonBone> bones, HashSet<string> usedNames)
    {
        if (ShouldSkipSkeletonNode(transform))
            return;

        if (usedNames.Add(transform.name))
        {
            bones.Add(new SkeletonBone
            {
                name = transform.name,
                position = transform.localPosition,
                rotation = transform.localRotation,
                scale = transform.localScale
            });
        }

        for (int i = 0; i < transform.childCount; i++)
            AddSkeletonBone(transform.GetChild(i), bones, usedNames);
    }

    static bool ShouldSkipSkeletonNode(Transform transform)
    {
        if (transform.name == "Meshes")
            return true;

        return transform.GetComponent<Renderer>() != null
            || transform.GetComponent<MeshFilter>() != null
            || transform.GetComponent<ParticleSystem>() != null;
    }

    static void SaveAvatar(Avatar avatar)
    {
        if (AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath) != null)
            AssetDatabase.DeleteAsset(AvatarPath);

        AssetDatabase.CreateAsset(avatar, AvatarPath);
    }

    static void SavePreviewPrefab(GameObject sourcePrefab, Avatar avatar, PreviewSpec spec)
    {
        GameObject instance = null;
        try
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
            if (instance == null)
                instance = UnityEngine.Object.Instantiate(sourcePrefab);

            instance.name = Path.GetFileNameWithoutExtension(spec.OutputPath);
            Transform courier = FindRequired(instance.transform, "Courier");
            SanitizeHumanoidHierarchy(courier);
            RemoveMissingScripts(instance);
            Animator animator = EnsureAnimator(courier.gameObject);
            animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath) ?? avatar;
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(spec.ControllerPath);
            animator.applyRootMotion = false;

            if (spec.EnableBoard)
            {
                Transform board = FindInSkeleton(courier, "Board");
                if (board != null)
                    board.gameObject.SetActive(true);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, spec.OutputPath);
        }
        finally
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    static Animator EnsureAnimator(GameObject target)
    {
        Animator animator = target.GetComponent<Animator>();
        if (animator == null)
            animator = target.AddComponent<Animator>();

        return animator;
    }

    static Transform FindRequired(Transform root, string name)
    {
        Transform result = FindInSkeleton(root, name);
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

    static Transform FindInSkeleton(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = FindInSkeleton(root.GetChild(i), name);
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

    static void WriteReport()
    {
        File.WriteAllText(
            ToAbsolutePath(OutputFolder + "/HasteHumanoidReadme.txt"),
            "菜单：Tools/Haste/Build Courier Humanoid Avatar\n" +
            "输出 Avatar：HUMANOID/HasteCourier_HumanoidAvatar.asset\n" +
            "输出 Prefab：HUMANOID/HasteCourier_Humanoid.prefab\n" +
            "预览 Prefab：HUMANOID/Preview_Humanoid_Courier_*.prefab\n" +
            "说明：这一步把 Courier 模型做成 Unity Humanoid Avatar。Haste 原始 .anim 仍是 Generic Transform 曲线，如需给其他人形角色复用，还要继续烘焙成 Humanoid muscle 动作。\n");
    }

    static string ToAbsolutePath(string assetPath)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
    }

    readonly struct PreviewSpec
    {
        public PreviewSpec(string name, string controllerPath, string outputPath, bool enableBoard)
        {
            Name = name;
            ControllerPath = controllerPath;
            OutputPath = outputPath;
            EnableBoard = enableBoard;
        }

        public string Name { get; }
        public string ControllerPath { get; }
        public string OutputPath { get; }
        public bool EnableBoard { get; }
    }
}
