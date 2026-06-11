using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CorinAnimationSplitter
{
    const string SourceRoot = "Assets/Art/Animation/ZZZ/可琳";
    const string OutputRoot = "Assets/Art/Animation/MyDemoNeed/Corin";
    const string GenericRootmotionFolder = OutputRoot + "/Rootmotion";
    const string GenericInplaceFolder = OutputRoot + "/Inplace";
    const string GenericWithWeaponRootmotionFolder = OutputRoot + "/WithWeaponRootmotion";
    const string GenericWithWeaponInplaceFolder = OutputRoot + "/WithWeaponInplace";
    const string GenericWeaponFolder = OutputRoot + "/CorinWeapon";
    const string HumanoidOutputRoot = OutputRoot + "/Humanoid";
    const string HumanoidRootmotionFolder = HumanoidOutputRoot + "/Rootmotion";
    const string HumanoidInplaceFolder = HumanoidOutputRoot + "/Inplace";
    const string RequestFile = ".corin_animation_split_request";

    static readonly string[] WeaponPathTokens =
    {
        "Bip001 Prop1",
        "Weapon_",
        "Weapon_saw",
        "Corin_Weapon"
    };

    static readonly string[] NonHumanoidPathTokens =
    {
        "Bip001 Prop1",
        "Weapon_",
        "Weapon_saw",
        "Corin_Weapon",
        "Hair_",
        "Skirt_",
        "Spring_",
        "S_Chain",
        "Kuma_",
        "Corin_face",
        "Chest_",
        "Etc_",
        "Bn_",
        "Pelvis_L",
        "Pelvis_R"
    };

    const string SourceClipPrefix = "Avatar_Female_Size01_Corin_Ani_";

    [InitializeOnLoadMethod]
    static void RunQueuedRequest()
    {
        string requestPath = GetRequestPath();
        if (!File.Exists(requestPath))
            return;

        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(requestPath))
                return;

            try
            {
                SplitAllGenericAndHumanoidAnimations();
                File.Delete(requestPath);
                AssetDatabase.Refresh();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        };
    }

    [MenuItem("Tools/BBB/Corin/Split All Generic And Humanoid Animations")]
    public static void SplitAllGenericAndHumanoidAnimations()
    {
        EnsureFolders();
        ClipSpec[] clips = FindSourceClips();

        CleanAnimationAssets(
            GenericRootmotionFolder,
            GenericInplaceFolder,
            GenericWithWeaponRootmotionFolder,
            GenericWithWeaponInplaceFolder,
            GenericWeaponFolder,
            HumanoidRootmotionFolder,
            HumanoidInplaceFolder);

        SplitGeneric(clips);
        SplitHumanoid(clips);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CorinAnimationSplitter] Split {clips.Length} Corin generic and humanoid animations.");
    }

    [MenuItem("Tools/BBB/Corin/Split All Humanoid Animations")]
    public static void SplitAllHumanoidOnly()
    {
        EnsureFolders();
        ClipSpec[] clips = FindSourceClips();
        CleanAnimationAssets(HumanoidRootmotionFolder, HumanoidInplaceFolder);
        SplitHumanoid(clips);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CorinAnimationSplitter] Split {clips.Length} Corin humanoid animations.");
    }

    public static bool IsHumanoidBindingForTest(string path)
    {
        return IsHumanoidPath(path ?? string.Empty);
    }

    public static bool IsWeaponBindingForTest(string path)
    {
        return IsWeaponPath(path ?? string.Empty);
    }

    public static bool IsGenericWithoutWeaponBindingForTest(string path)
    {
        return !IsWeaponPath(path ?? string.Empty);
    }

    static void SplitGeneric(ClipSpec[] clips)
    {
        var report = new StringBuilder();
        report.AppendLine("Corin generic animation split report");
        report.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        report.AppendLine("Source: " + SourceRoot);
        report.AppendLine("Output: " + OutputRoot);
        report.AppendLine("Scope: full generic skeleton; Rootmotion/Inplace exclude weapon, WithWeaponRootmotion/WithWeaponInplace keep weapon, CorinWeapon contains weapon curves only.");
        report.AppendLine("clipCount: " + clips.Length);

        foreach (ClipSpec spec in clips)
        {
            AnimationClip source = LoadClip(spec);
            AnimationClip rootmotion = BuildClip(source, IsGenericWithoutWeaponBinding, false, true);
            AnimationClip inplace = BuildClip(source, IsGenericWithoutWeaponBinding, true, true);
            AnimationClip withWeaponRootmotion = BuildClip(source, AlwaysIncludeBinding, false, true);
            AnimationClip withWeaponInplace = BuildClip(source, AlwaysIncludeBinding, true, true);
            AnimationClip weapon = BuildClip(source, IsWeaponBinding, false, false);

            SaveClip(rootmotion, $"{GenericRootmotionFolder}/{spec.OutputName}_Rootmotion.anim");
            SaveClip(inplace, $"{GenericInplaceFolder}/{spec.OutputName}_Inplace.anim");
            SaveClip(withWeaponRootmotion, $"{GenericWithWeaponRootmotionFolder}/{spec.OutputName}_WithWeaponRootmotion.anim");
            SaveClip(withWeaponInplace, $"{GenericWithWeaponInplaceFolder}/{spec.OutputName}_WithWeaponInplace.anim");
            SaveClip(weapon, $"{GenericWeaponFolder}/{spec.OutputName}_Weapon.anim");

            int total = AnimationUtility.GetCurveBindings(source).Length;
            int rootmotionCount = AnimationUtility.GetCurveBindings(rootmotion).Length;
            int inplaceCount = AnimationUtility.GetCurveBindings(inplace).Length;
            int withWeaponRootmotionCount = AnimationUtility.GetCurveBindings(withWeaponRootmotion).Length;
            int withWeaponInplaceCount = AnimationUtility.GetCurveBindings(withWeaponInplace).Length;
            int weaponCount = AnimationUtility.GetCurveBindings(weapon).Length;

            report.AppendLine();
            report.AppendLine(spec.OutputName);
            report.AppendLine($"  source: {spec.SourcePath} :: {spec.ClipName}");
            report.AppendLine($"  sourceCurves: {total}");
            report.AppendLine($"  rootmotionCurves: {rootmotionCount}");
            report.AppendLine($"  inplaceCurves: {inplaceCount}");
            report.AppendLine($"  withWeaponRootmotionCurves: {withWeaponRootmotionCount}");
            report.AppendLine($"  withWeaponInplaceCurves: {withWeaponInplaceCount}");
            report.AppendLine($"  weaponCurves: {weaponCount}");
        }

        File.WriteAllText(ToAbsoluteAssetPath($"{OutputRoot}/CorinGenericSplitReport.txt"), report.ToString(), Encoding.UTF8);
    }

    static void SplitHumanoid(ClipSpec[] clips)
    {
        var report = new StringBuilder();
        report.AppendLine("Corin humanoid animation split report");
        report.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        report.AppendLine("Source: " + SourceRoot);
        report.AppendLine("Output: " + HumanoidOutputRoot);
        report.AppendLine("Scope: humanoid skeleton only; skirt, hair/accessory chains, face attachment, and weapon curves are excluded.");
        report.AppendLine("clipCount: " + clips.Length);

        foreach (ClipSpec spec in clips)
        {
            AnimationClip source = LoadClip(spec);
            AnimationClip rootmotion = BuildClip(source, IsHumanoidBinding, false, true);
            AnimationClip inplace = BuildClip(source, IsHumanoidBinding, true, true);

            SaveClip(rootmotion, $"{HumanoidRootmotionFolder}/{spec.OutputName}_Rootmotion.anim");
            SaveClip(inplace, $"{HumanoidInplaceFolder}/{spec.OutputName}_Inplace.anim");

            int total = AnimationUtility.GetCurveBindings(source).Length;
            int rootmotionCount = AnimationUtility.GetCurveBindings(rootmotion).Length;
            int inplaceCount = AnimationUtility.GetCurveBindings(inplace).Length;

            report.AppendLine();
            report.AppendLine(spec.OutputName);
            report.AppendLine($"  source: {spec.SourcePath} :: {spec.ClipName}");
            report.AppendLine($"  sourceCurves: {total}");
            report.AppendLine($"  rootmotionCurves: {rootmotionCount}");
            report.AppendLine($"  inplaceCurves: {inplaceCount}");
            report.AppendLine($"  excludedCurves: {Math.Max(0, total - rootmotionCount)}");
        }

        File.WriteAllText(ToAbsoluteAssetPath($"{HumanoidOutputRoot}/CorinHumanoidSplitReport.txt"), report.ToString(), Encoding.UTF8);
    }

    static AnimationClip BuildClip(AnimationClip source, Func<EditorCurveBinding, bool> includeCurve, bool neutralizeRootXZ, bool copyEvents)
    {
        var clip = new AnimationClip
        {
            name = source.name,
            frameRate = source.frameRate,
            wrapMode = source.wrapMode,
            legacy = source.legacy,
        };

        AnimationUtility.SetAnimationClipSettings(clip, AnimationUtility.GetAnimationClipSettings(source));

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
        {
            if (!includeCurve(binding))
                continue;

            AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
            if (neutralizeRootXZ && IsRootPositionXZ(binding))
                curve = MakeConstantCurve(source, curve);

            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
        {
            if (!includeCurve(binding))
                continue;

            ObjectReferenceKeyframe[] curve = AnimationUtility.GetObjectReferenceCurve(source, binding);
            AnimationUtility.SetObjectReferenceCurve(clip, binding, curve);
        }

        if (copyEvents)
            AnimationUtility.SetAnimationEvents(clip, AnimationUtility.GetAnimationEvents(source));

        return clip;
    }

    static AnimationClip LoadClip(ClipSpec spec)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(spec.SourcePath)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate => candidate.name == spec.ClipName);

        if (clip == null)
            throw new InvalidOperationException($"Clip not found: {spec.SourcePath} :: {spec.ClipName}");

        return clip;
    }

    static ClipSpec[] FindSourceClips()
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { SourceRoot });
        var byOutputName = new Dictionary<string, ClipSpec>(StringComparer.OrdinalIgnoreCase);

        foreach (string guid in guids)
        {
            string sourcePath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(sourcePath))
                continue;

            foreach (AnimationClip clip in AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>())
            {
                if (clip == null || string.IsNullOrEmpty(clip.name) || clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                    continue;

                string outputName = ToOutputName(clip.name);
                var spec = new ClipSpec(outputName, sourcePath, clip.name);
                if (!byOutputName.TryGetValue(outputName, out ClipSpec existing) || GetSourcePriority(spec.SourcePath) > GetSourcePriority(existing.SourcePath))
                    byOutputName[outputName] = spec;
            }
        }

        return byOutputName.Values
            .OrderBy(spec => spec.OutputName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    static string ToOutputName(string clipName)
    {
        if (clipName == "Avatar_Female_Size01_Corin_Ani_Run_Start")
            return "Corin_RunStart";

        if (clipName == "Avatar_Female_Size01_Corin_Ani_Run")
            return "Corin_RunLoop";

        if (clipName == "Avatar_Female_Size01_Corin_Ani_Run_Start_End")
            return "Corin_RunEnd";

        string suffix = clipName.StartsWith(SourceClipPrefix, StringComparison.Ordinal)
            ? clipName.Substring(SourceClipPrefix.Length)
            : clipName;

        return "Corin_" + SanitizeFileName(suffix);
    }

    static string SanitizeFileName(string value)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            builder.Append(invalidChars.Contains(character) ? '_' : character);
        }

        return builder.ToString();
    }

    static int GetSourcePriority(string sourcePath)
    {
        if (sourcePath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
            return 50;

        if (sourcePath.EndsWith("可琳补充.fbx", StringComparison.OrdinalIgnoreCase))
            return 40;

        if (sourcePath.EndsWith("可琳（攻击动作）.fbx", StringComparison.OrdinalIgnoreCase))
            return 30;

        if (sourcePath.EndsWith("可琳技能.fbx", StringComparison.OrdinalIgnoreCase))
            return 20;

        if (sourcePath.EndsWith("可琳（基本动作）.fbx", StringComparison.OrdinalIgnoreCase))
            return 10;

        return 0;
    }

    static void SaveClip(AnimationClip clip, string assetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath) != null)
            AssetDatabase.DeleteAsset(assetPath);

        AssetDatabase.CreateAsset(clip, assetPath);
    }

    static void CleanAnimationAssets(params string[] folders)
    {
        foreach (string folder in folders.Distinct())
        {
            if (!AssetDatabase.IsValidFolder(folder))
                continue;

            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith(folder + "/", StringComparison.Ordinal) && path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
                    AssetDatabase.DeleteAsset(path);
            }
        }
    }

    static void EnsureFolders()
    {
        Directory.CreateDirectory(ToAbsoluteAssetPath(GenericRootmotionFolder));
        Directory.CreateDirectory(ToAbsoluteAssetPath(GenericInplaceFolder));
        Directory.CreateDirectory(ToAbsoluteAssetPath(GenericWithWeaponRootmotionFolder));
        Directory.CreateDirectory(ToAbsoluteAssetPath(GenericWithWeaponInplaceFolder));
        Directory.CreateDirectory(ToAbsoluteAssetPath(GenericWeaponFolder));
        Directory.CreateDirectory(ToAbsoluteAssetPath(HumanoidRootmotionFolder));
        Directory.CreateDirectory(ToAbsoluteAssetPath(HumanoidInplaceFolder));
    }

    static bool AlwaysIncludeBinding(EditorCurveBinding binding)
    {
        return true;
    }

    static bool IsGenericWithoutWeaponBinding(EditorCurveBinding binding)
    {
        return !IsWeaponPath(binding.path ?? string.Empty);
    }

    static bool IsWeaponBinding(EditorCurveBinding binding)
    {
        return IsWeaponPath(binding.path ?? string.Empty);
    }

    static bool IsHumanoidBinding(EditorCurveBinding binding)
    {
        return IsHumanoidPath(binding.path ?? string.Empty);
    }

    static bool IsWeaponPath(string path)
    {
        foreach (string token in WeaponPathTokens)
        {
            if (path.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    static bool IsHumanoidPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return true;

        if (path != "Root" && path != "Bip001" && !path.StartsWith("Bip001/", StringComparison.Ordinal))
            return false;

        foreach (string token in NonHumanoidPathTokens)
        {
            if (path.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
        }

        return true;
    }

    static bool IsRootPositionXZ(EditorCurveBinding binding)
    {
        string path = binding.path ?? string.Empty;
        string property = binding.propertyName ?? string.Empty;

        if (property == "RootT.x" || property == "RootT.z")
            return true;

        bool isRoot = path.Length == 0 || path == "Root" || path == "Bip001" || path.EndsWith("/Bip001", StringComparison.Ordinal);
        bool isPosition = property.IndexOf("LocalPosition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          property.IndexOf("Position", StringComparison.OrdinalIgnoreCase) >= 0;
        bool isXZ = property.EndsWith(".x", StringComparison.Ordinal) || property.EndsWith(".z", StringComparison.Ordinal);

        return isRoot && isPosition && isXZ;
    }

    static AnimationCurve MakeConstantCurve(AnimationClip source, AnimationCurve curve)
    {
        float value = curve != null && curve.length > 0 ? curve.keys[0].value : 0f;
        float duration = Mathf.Max(source.length, 1f / Mathf.Max(1f, source.frameRate));
        return AnimationCurve.Constant(0f, duration, value);
    }

    static string ToAbsoluteAssetPath(string assetPath)
    {
        string relative = assetPath.StartsWith("Assets/", StringComparison.Ordinal)
            ? assetPath.Substring("Assets/".Length)
            : assetPath;

        return Path.Combine(Application.dataPath, relative);
    }

    static string GetRequestPath()
    {
        return ToAbsoluteAssetPath($"{OutputRoot}/{RequestFile}");
    }

    readonly struct ClipSpec
    {
        public readonly string OutputName;
        public readonly string SourcePath;
        public readonly string ClipName;

        public ClipSpec(string outputName, string sourcePath, string clipName)
        {
            OutputName = outputName;
            SourcePath = sourcePath;
            ClipName = clipName;
        }
    }
}
