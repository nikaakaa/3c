using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CorinAnimationSplitter
{
    const string OutputRoot = "Assets/Animation/MyDemoNeed/Corin";
    const string RootmotionFolder = OutputRoot + "/Rootmotion";
    const string InplaceFolder = OutputRoot + "/Inplace";
    const string WeaponFolder = OutputRoot + "/CorinWeapon";
    const string RequestFile = ".corin_split_request";

    static readonly ClipSpec[] Clips =
    {
        new ClipSpec("Corin_Idle", "Assets/Animation/可琳/可琳（基本动作）.fbx", "Avatar_Female_Size01_Corin_Ani_Idle"),
        new ClipSpec("Corin_RunStart", "Assets/Animation/可琳/可琳补充.fbx", "Avatar_Female_Size01_Corin_Ani_Run_Start"),
        new ClipSpec("Corin_RunLoop", "Assets/Animation/可琳/可琳（基本动作）.fbx", "Avatar_Female_Size01_Corin_Ani_Run"),
        new ClipSpec("Corin_RunEnd", "Assets/Animation/可琳/可琳（基本动作）.fbx", "Avatar_Female_Size01_Corin_Ani_Run_Start_End"),
    };

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
                SplitCurrentFour();
                File.Delete(requestPath);
                AssetDatabase.Refresh();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        };
    }

    [MenuItem("Tools/BBB/Corin/Split Current 4 Animations")]
    public static void SplitCurrentFour()
    {
        EnsureFolders();

        var report = new StringBuilder();
        report.AppendLine("Corin animation split report");
        report.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        foreach (ClipSpec spec in Clips)
        {
            AnimationClip source = LoadClip(spec);
            AnimationClip rootmotion = BuildClip(source, binding => true, false, true);
            AnimationClip inplace = BuildClip(source, binding => !IsWeaponBinding(binding), true, true);
            AnimationClip weapon = BuildClip(source, IsWeaponBinding, false, false);

            SaveClip(rootmotion, $"{RootmotionFolder}/{spec.OutputName}_Rootmotion.anim");
            SaveClip(inplace, $"{InplaceFolder}/{spec.OutputName}_Inplace.anim");
            SaveClip(weapon, $"{WeaponFolder}/{spec.OutputName}_Weapon.anim");

            int total = AnimationUtility.GetCurveBindings(source).Length;
            int weaponCount = AnimationUtility.GetCurveBindings(weapon).Length;
            int inplaceCount = AnimationUtility.GetCurveBindings(inplace).Length;

            report.AppendLine();
            report.AppendLine(spec.OutputName);
            report.AppendLine($"  source: {spec.SourcePath} :: {spec.ClipName}");
            report.AppendLine($"  sourceCurves: {total}");
            report.AppendLine($"  inplaceCurves: {inplaceCount}");
            report.AppendLine($"  weaponCurves: {weaponCount}");
        }

        File.WriteAllText(ToAbsoluteAssetPath($"{OutputRoot}/CorinSplitReport.txt"), report.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log("[CorinAnimationSplitter] Split current 4 Corin animations.");
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

    static void SaveClip(AnimationClip clip, string assetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath) != null)
            AssetDatabase.DeleteAsset(assetPath);

        AssetDatabase.CreateAsset(clip, assetPath);
    }

    static void EnsureFolders()
    {
        Directory.CreateDirectory(ToAbsoluteAssetPath(RootmotionFolder));
        Directory.CreateDirectory(ToAbsoluteAssetPath(InplaceFolder));
        Directory.CreateDirectory(ToAbsoluteAssetPath(WeaponFolder));
    }

    static bool IsWeaponBinding(EditorCurveBinding binding)
    {
        string path = binding.path ?? string.Empty;
        return path.Contains("Bip001 Prop1") ||
               path.Contains("Weapon_") ||
               path.Contains("Weapon_saw") ||
               path.Contains("Corin_Weapon");
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
