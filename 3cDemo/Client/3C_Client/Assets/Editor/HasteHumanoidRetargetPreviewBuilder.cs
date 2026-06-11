using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class HasteHumanoidRetargetPreviewBuilder
{
    const string Root = "Assets/Art/Animation/Haste/HasteMainCharacter";
    const string DefaultOutputRoot = Root + "/HUMANOID/RetargetPreviews";

    [MenuItem("Tools/Haste/Prepare Selected Target As Humanoid")]
    public static void PrepareSelectedTargetAsHumanoid()
    {
        try
        {
            TargetHumanoidPrepareResult result = PrepareTargetAsHumanoid(GetSelectedTarget());
            string state = result.AvatarValid ? "valid" : "invalid";
            Debug.Log($"[HasteHumanoidRetargetPreviewBuilder] Prepared {result.ModelPath}. Avatar is {state}.");
            EditorUtility.DisplayDialog("Haste Target Humanoid", $"Model: {result.ModelPath}\nAvatar: {state}", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError("[HasteHumanoidRetargetPreviewBuilder] " + ex.Message);
            EditorUtility.DisplayDialog("Haste Target Humanoid", ex.Message, "OK");
        }
    }

    [MenuItem("Tools/Haste/Prepare Selected Target As Humanoid", true)]
    static bool CanPrepareSelectedTargetAsHumanoid()
    {
        foreach (UnityEngine.Object selected in Selection.objects)
        {
            if (selected is GameObject)
                return true;
        }

        return false;
    }

    [MenuItem("Tools/Haste/Prepare Target And Create Retarget Preview From Selection")]
    public static void PrepareTargetAndCreatePreviewFromSelection()
    {
        try
        {
            GetSelection(out GameObject target, out AnimationClip clip);
            string targetPath = AssetDatabase.GetAssetPath(target);
            TargetHumanoidPrepareResult prepare = PrepareTargetAsHumanoid(target);
            if (!prepare.AvatarValid)
                throw new InvalidOperationException($"Prepared {prepare.ModelPath}, but Unity still could not build a valid Humanoid Avatar. Open Configure Avatar and fix the mapping.");

            if (!string.IsNullOrEmpty(targetPath))
                target = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) ?? target;

            RetargetPreviewResult result = CreatePreview(target, clip, DefaultOutputRoot);
            Selection.activeObject = result.PreviewPrefab;
            EditorGUIUtility.PingObject(result.PreviewPrefab);
            Debug.Log($"[HasteHumanoidRetargetPreviewBuilder] Prepared target and created {result.PreviewPrefabPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError("[HasteHumanoidRetargetPreviewBuilder] " + ex.Message);
            EditorUtility.DisplayDialog("Haste Retarget Preview", ex.Message, "OK");
        }
    }

    [MenuItem("Tools/Haste/Prepare Target And Create Retarget Preview From Selection", true)]
    static bool CanPrepareTargetAndCreatePreviewFromSelection()
    {
        return CanCreatePreviewFromSelection();
    }

    [MenuItem("Tools/Haste/Create Retarget Preview From Selection")]
    public static void CreatePreviewFromSelection()
    {
        try
        {
            GetSelection(out GameObject target, out AnimationClip clip);
            RetargetPreviewResult result = CreatePreview(target, clip, DefaultOutputRoot);
            Selection.activeObject = result.PreviewPrefab;
            EditorGUIUtility.PingObject(result.PreviewPrefab);
            Debug.Log($"[HasteHumanoidRetargetPreviewBuilder] Created {result.PreviewPrefabPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError("[HasteHumanoidRetargetPreviewBuilder] " + ex.Message);
            EditorUtility.DisplayDialog("Haste Retarget Preview", ex.Message, "OK");
        }
    }

    [MenuItem("Tools/Haste/Create Retarget Preview From Selection", true)]
    static bool CanCreatePreviewFromSelection()
    {
        bool hasTarget = false;
        bool hasClip = false;
        foreach (UnityEngine.Object selected in Selection.objects)
        {
            hasTarget |= selected is GameObject;
            hasClip |= selected is AnimationClip;
        }

        return hasTarget && hasClip;
    }

    public static TargetHumanoidPrepareResult PrepareTargetAsHumanoid(GameObject target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (!TryResolveTargetModelImporter(target, out string modelPath, out ModelImporter importer))
            throw new InvalidOperationException($"{target.name} does not resolve to a model importer.");

        bool changed = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            changed = true;
        }

        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            changed = true;
        }

        if (!importer.autoGenerateAvatarMappingIfUnspecified)
        {
            importer.autoGenerateAvatarMappingIfUnspecified = true;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
        else
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);

        Avatar avatar = FindAvatarAtPath(modelPath);
        bool avatarValid = avatar != null && avatar.isValid && avatar.isHuman;
        return new TargetHumanoidPrepareResult(modelPath, changed, avatarValid, avatar);
    }

    public static RetargetPreviewResult CreatePreview(GameObject target, AnimationClip clip, string outputRoot)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (clip == null)
            throw new ArgumentNullException(nameof(clip));

        if (!clip.humanMotion)
            throw new InvalidOperationException($"{clip.name} is not a Humanoid clip.");

        if (string.IsNullOrWhiteSpace(outputRoot))
            throw new ArgumentException("Output root is empty.", nameof(outputRoot));

        Animator targetAnimator = FindAnimator(target);
        Avatar targetAvatar = targetAnimator.avatar;
        if (targetAvatar == null || !targetAvatar.isValid || !targetAvatar.isHuman)
            throw new InvalidOperationException(CreateAvatarFailureMessage(target, targetAvatar));

        string targetName = SanitizeName(target.name);
        string clipName = SanitizeName(clip.name);
        string outputFolder = $"{outputRoot}/{targetName}";
        EnsureFolder(outputFolder);

        string controllerPath = $"{outputFolder}/Preview_{targetName}_{clipName}.controller";
        string prefabPath = $"{outputFolder}/Preview_{targetName}_{clipName}.prefab";
        string reportPath = $"{outputFolder}/RetargetPreviewReport.txt";

        AnimatorController controller = CreateSingleClipController(controllerPath, clip);
        GameObject instance = null;
        try
        {
            instance = InstantiateTarget(target);
            instance.name = $"Preview_{targetName}_{clipName}";

            Animator animator = FindAnimator(instance);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                AssetDatabase.DeleteAsset(prefabPath);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            WriteReport(reportPath, target, targetAvatar, clip, controllerPath, prefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new RetargetPreviewResult(prefabPath, controllerPath, prefab, controller);
        }
        finally
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    public static bool TryResolveTargetModelImporter(GameObject target, out string modelPath, out ModelImporter importer)
    {
        modelPath = null;
        importer = null;
        if (target == null)
            return false;

        if (TryGetModelImporter(AssetDatabase.GetAssetPath(target), out modelPath, out importer))
            return true;

        Animator animator = target.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.avatar != null && TryGetModelImporter(AssetDatabase.GetAssetPath(animator.avatar), out modelPath, out importer))
            return true;

        foreach (SkinnedMeshRenderer renderer in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.sharedMesh != null && TryGetModelImporter(AssetDatabase.GetAssetPath(renderer.sharedMesh), out modelPath, out importer))
                return true;
        }

        return false;
    }

    static void GetSelection(out GameObject target, out AnimationClip clip)
    {
        target = null;
        clip = null;
        foreach (UnityEngine.Object selected in Selection.objects)
        {
            if (target == null && selected is GameObject selectedTarget)
                target = selectedTarget;

            if (clip == null && selected is AnimationClip selectedClip)
                clip = selectedClip;
        }

        if (target == null || clip == null)
            throw new InvalidOperationException("Select one Humanoid target prefab or scene object and one Haste Humanoid clip.");
    }

    static GameObject GetSelectedTarget()
    {
        foreach (UnityEngine.Object selected in Selection.objects)
        {
            if (selected is GameObject selectedTarget)
                return selectedTarget;
        }

        throw new InvalidOperationException("Select one target prefab, model, or scene object.");
    }

    static GameObject InstantiateTarget(GameObject target)
    {
        string path = AssetDatabase.GetAssetPath(target);
        GameObject instance = null;
        if (!string.IsNullOrEmpty(path))
            instance = PrefabUtility.InstantiatePrefab(target) as GameObject;

        return instance != null ? instance : UnityEngine.Object.Instantiate(target);
    }

    static Animator FindAnimator(GameObject target)
    {
        Animator animator = target.GetComponentInChildren<Animator>(true);
        if (animator == null)
            throw new InvalidOperationException($"{target.name} does not have an Animator.");

        return animator;
    }

    static string CreateAvatarFailureMessage(GameObject target, Avatar avatar)
    {
        string message = $"{target.name} does not have a valid Humanoid Avatar.";
        if (avatar == null)
            return message + " Assign a Humanoid Avatar to the target Animator first.";

        string avatarPath = AssetDatabase.GetAssetPath(avatar);
        if (string.IsNullOrEmpty(avatarPath))
            return message + $" Current Avatar: {avatar.name}.";

        var importer = AssetImporter.GetAtPath(avatarPath) as ModelImporter;
        if (importer == null)
            return message + $" Current Avatar: {avatar.name}. Avatar asset: {avatarPath}.";

        return message +
            $" Current Avatar: {avatar.name}. Model: {avatarPath}. Rig: {importer.animationType}. " +
            "Set the model Rig Animation Type to Humanoid, Apply, then configure a valid Avatar.";
    }

    static bool TryGetModelImporter(string assetPath, out string modelPath, out ModelImporter importer)
    {
        modelPath = null;
        importer = null;
        if (string.IsNullOrEmpty(assetPath))
            return false;

        importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
            return false;

        modelPath = assetPath;
        return true;
    }

    static Avatar FindAvatarAtPath(string modelPath)
    {
        Avatar fallback = null;
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
        {
            Avatar avatar = asset as Avatar;
            if (avatar == null)
                continue;

            if (avatar.isValid && avatar.isHuman)
                return avatar;

            if (fallback == null)
                fallback = avatar;
        }

        return fallback;
    }

    static AnimatorController CreateSingleClipController(string path, AnimationClip clip)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            AssetDatabase.DeleteAsset(path);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorState state = controller.layers[0].stateMachine.AddState(clip.name);
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        return controller;
    }

    static void WriteReport(string path, GameObject target, Avatar avatar, AnimationClip clip, string controllerPath, string prefabPath)
    {
        File.WriteAllText(
            ToAbsolutePath(path),
            "Haste retarget preview report\n" +
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
            $"Target: {target.name}\n" +
            $"Target Asset: {AssetDatabase.GetAssetPath(target)}\n" +
            $"Avatar: {avatar.name}\n" +
            $"Clip: {clip.name}\n" +
            $"Clip Asset: {AssetDatabase.GetAssetPath(clip)}\n" +
            $"Controller: {controllerPath}\n" +
            $"Preview Prefab: {prefabPath}\n");
    }

    static string SanitizeName(string name)
    {
        string result = Regex.Replace(name, @"[^\w\u4e00-\u9fa5]+", "_");
        return string.IsNullOrWhiteSpace(result) ? "Target" : result.Trim('_');
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

    public readonly struct RetargetPreviewResult
    {
        public RetargetPreviewResult(string previewPrefabPath, string controllerPath, GameObject previewPrefab, RuntimeAnimatorController controller)
        {
            PreviewPrefabPath = previewPrefabPath;
            ControllerPath = controllerPath;
            PreviewPrefab = previewPrefab;
            Controller = controller;
        }

        public string PreviewPrefabPath { get; }
        public string ControllerPath { get; }
        public GameObject PreviewPrefab { get; }
        public RuntimeAnimatorController Controller { get; }
    }

    public readonly struct TargetHumanoidPrepareResult
    {
        public TargetHumanoidPrepareResult(string modelPath, bool changedImporter, bool avatarValid, Avatar avatar)
        {
            ModelPath = modelPath;
            ChangedImporter = changedImporter;
            AvatarValid = avatarValid;
            Avatar = avatar;
        }

        public string ModelPath { get; }
        public bool ChangedImporter { get; }
        public bool AvatarValid { get; }
        public Avatar Avatar { get; }
    }
}
