using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using ThirdPersonDiagnostics;

public static class CorinHumanoidMuscleClipBaker
{
    const string SourceRoot = "Assets/Art/Animation/ZZZ/可琳";
    public const string HumanoidModelPath = "Assets/Art/Model/ZZZ/可琳/可琳Humanoid.fbx";
    public const string OutputRoot = "Assets/Art/Animation/MyDemoNeed/Corin/HumanoidMuscle";
    public const string OutputRootmotionFolder = OutputRoot + "/Rootmotion";
    public const string OutputInplaceFolder = OutputRoot + "/Inplace";
    const string SourceClipPrefix = "Avatar_Female_Size01_Corin_Ani_";

    static readonly string[] PresentationOutputNames =
    {
        "Corin_RunStart",
        "Corin_RunLoop",
        "Corin_Run_End",
        "Corin_Idle",
        "Corin_Walk_Start",
        "Corin_Walk",
        "Corin_Evade_Front",
        "Corin_Evade_Back",
        "Corin_TurnBack",
    };

    [MenuItem("Tools/3C/Corin/Bake Presentation Humanoid Muscle Clips")]
    public static void BakePresentationClips()
    {
        BakeClipSet(PresentationOutputNames, false);
    }

    [MenuItem("Tools/3C/Corin/Bake All Humanoid Muscle Clips")]
    public static void BakeAllHumanoidMuscleClips()
    {
        BakeClipSet(null, true);
    }

    [MenuItem("Tools/3C/Corin/Validate Humanoid Muscle Clips")]
    public static void ValidateOutputClips()
    {
        foreach (string outputName in PresentationOutputNames)
        {
            ValidateClip($"{OutputInplaceFolder}/{outputName}_Inplace.anim");
            ValidateClip($"{OutputRootmotionFolder}/{outputName}_Rootmotion.anim");
        }

         RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(RuntimeDiagnosticLogCategory.Editor, RuntimeDiagnosticLogLevel.Info, "humanoid-muscle-validation-passed", "", "", 0, Time.frameCount, "[CorinHumanoidMuscleClipBaker] Humanoid muscle clip validation passed."));
    }

    public static AnimationClip BakeClip(GameObject humanoidModelPrefab, Avatar avatar, AnimationClip sourceClip, bool neutralizeRootXZ)
    {
        if (humanoidModelPrefab == null)
            throw new ArgumentNullException(nameof(humanoidModelPrefab));

        if (avatar == null || !avatar.isValid || !avatar.isHuman)
            throw new ArgumentException("Avatar must be a valid Humanoid avatar.", nameof(avatar));

        if (sourceClip == null)
            throw new ArgumentNullException(nameof(sourceClip));

        GameObject instance = null;
        HumanPoseHandler handler = null;
        try
        {
            instance = PrefabUtility.InstantiatePrefab(humanoidModelPrefab) as GameObject;
            if (instance == null)
                instance = UnityEngine.Object.Instantiate(humanoidModelPrefab);

            Transform root = instance.transform;
            RemoveAnimator(root.gameObject);

            handler = new HumanPoseHandler(avatar, root);
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
                sourceClip.SampleAnimation(root.gameObject, time);
                handler.GetHumanPose(ref pose);

                for (int muscle = 0; muscle < muscleCurves.Length; muscle++)
                    muscleCurves[muscle].AddKey(time, pose.muscles[muscle]);

                Quaternion bodyRotation = pose.bodyRotation;
                if (hasPreviousRoot && Quaternion.Dot(previousRoot, bodyRotation) < 0f)
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

            float duration = Mathf.Max(sourceClip.length, 1f / sampleRate);
            if (neutralizeRootXZ)
            {
                rootTX = MakeConstantCurve(rootTX, duration);
                rootTZ = MakeConstantCurve(rootTZ, duration);
            }

            var output = new AnimationClip
            {
                name = sourceClip.name,
                frameRate = sampleRate,
                legacy = false,
                wrapMode = sourceClip.wrapMode,
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

            AnimationUtility.SetAnimationEvents(output, AnimationUtility.GetAnimationEvents(sourceClip));
            return output;
        }
        finally
        {
            handler?.Dispose();
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    public static bool IsHumanoidMuscleClip(AnimationClip clip)
    {
        if (clip == null || !clip.humanMotion)
            return false;

        bool hasRootT = false;
        bool hasRootQ = false;
        bool hasMuscle = false;

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (binding.type != typeof(Animator) || !string.IsNullOrEmpty(binding.path))
                return false;

            if (binding.propertyName.StartsWith("RootT.", StringComparison.Ordinal))
                hasRootT = true;
            else if (binding.propertyName.StartsWith("RootQ.", StringComparison.Ordinal))
                hasRootQ = true;
            else if (IsMuscleProperty(binding.propertyName))
                hasMuscle = true;
        }

        return hasRootT && hasRootQ && hasMuscle;
    }

    public static AnimationClip LoadSourceClipForTest(string outputName)
    {
        ClipSpec spec = FindSourceClips().First(candidate => candidate.OutputName == outputName);
        return LoadClip(spec);
    }

    static void BakeClipSet(IReadOnlyCollection<string> outputNames, bool cleanOutputFolders)
    {
        EnsureFolders();
        if (cleanOutputFolders)
            CleanAnimationAssets(OutputRootmotionFolder, OutputInplaceFolder);

        Avatar avatar = FindHumanoidAvatar();
        GameObject model = LoadRequired<GameObject>(HumanoidModelPath);
        HashSet<string> filter = outputNames != null ? new HashSet<string>(outputNames, StringComparer.OrdinalIgnoreCase) : null;
        ClipSpec[] clips = FindSourceClips()
            .Where(spec => filter == null || filter.Contains(spec.OutputName))
            .OrderBy(spec => spec.OutputName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (filter != null && clips.Length != filter.Count)
        {
            string found = string.Join(", ", clips.Select(spec => spec.OutputName));
            throw new InvalidOperationException($"Missing Corin source clips. Expected {filter.Count}, found {clips.Length}: {found}");
        }

        var report = new StringBuilder();
        report.AppendLine("Corin humanoid muscle clip bake report");
        report.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        report.AppendLine("Source: " + SourceRoot);
        report.AppendLine("Avatar: " + HumanoidModelPath);
        report.AppendLine("Output: " + OutputRoot);
        report.AppendLine("Scope: Unity Humanoid Animator RootT/RootQ/muscle curves. Skirt, hair, weapon, and other non-human chains are not baked into these clips.");
        report.AppendLine("clipCount: " + clips.Length);

        foreach (ClipSpec spec in clips)
        {
            AnimationClip source = LoadClip(spec);
            AnimationClip rootmotion = BakeClip(model, avatar, source, false);
            AnimationClip inplace = BakeClip(model, avatar, source, true);

            rootmotion.name = $"{spec.OutputName}_Rootmotion";
            inplace.name = $"{spec.OutputName}_Inplace";

            string rootmotionPath = $"{OutputRootmotionFolder}/{spec.OutputName}_Rootmotion.anim";
            string inplacePath = $"{OutputInplaceFolder}/{spec.OutputName}_Inplace.anim";
            SaveClip(rootmotion, rootmotionPath);
            SaveClip(inplace, inplacePath);

            report.AppendLine();
            report.AppendLine(spec.OutputName);
            report.AppendLine($"  source: {spec.SourcePath} :: {spec.ClipName}");
            report.AppendLine($"  rootmotion: {rootmotionPath}");
            report.AppendLine($"  inplace: {inplacePath}");
            report.AppendLine($"  humanMotion: {rootmotion.humanMotion && inplace.humanMotion}");
            report.AppendLine($"  rootmotionCurves: {AnimationUtility.GetCurveBindings(rootmotion).Length}");
            report.AppendLine($"  inplaceCurves: {AnimationUtility.GetCurveBindings(inplace).Length}");
        }

        File.WriteAllText(ToAbsoluteAssetPath($"{OutputRoot}/CorinHumanoidMuscleBakeReport.txt"), report.ToString(), Encoding.UTF8);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
         RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(RuntimeDiagnosticLogCategory.Editor, RuntimeDiagnosticLogLevel.Info, "humanoid-muscle-baked", "", "", 0, Time.frameCount, $"[CorinHumanoidMuscleClipBaker] Baked {clips.Length} Corin Humanoid muscle clips."));
    }

    static void ValidateClip(string path)
    {
        AnimationClip clip = LoadRequired<AnimationClip>(path);
        if (!IsHumanoidMuscleClip(clip))
            throw new InvalidOperationException($"{path} is not a Unity Humanoid muscle clip.");
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

        return byOutputName.Values.ToArray();
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
            builder.Append(invalidChars.Contains(character) ? '_' : character);

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

    static Avatar FindHumanoidAvatar()
    {
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(HumanoidModelPath)
            .OfType<Avatar>()
            .FirstOrDefault(candidate => candidate != null && candidate.isValid && candidate.isHuman);

        if (avatar == null)
            throw new InvalidOperationException($"{HumanoidModelPath} does not contain a valid Humanoid Avatar.");

        return avatar;
    }

    static void SetAnimatorCurve(AnimationClip clip, string propertyName, AnimationCurve curve)
    {
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), propertyName), curve);
    }

    static AnimationCurve MakeConstantCurve(AnimationCurve source, float duration)
    {
        float value = source != null && source.length > 0 ? source.keys[0].value : 0f;
        return AnimationCurve.Constant(0f, duration, value);
    }

    static bool IsMuscleProperty(string propertyName)
    {
        for (int i = 0; i < HumanTrait.MuscleCount; i++)
            if (HumanTrait.MuscleName[i] == propertyName)
                return true;

        return false;
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
        Directory.CreateDirectory(ToAbsoluteAssetPath(OutputRootmotionFolder));
        Directory.CreateDirectory(ToAbsoluteAssetPath(OutputInplaceFolder));
    }

    static void RemoveAnimator(GameObject target)
    {
        Animator animator = target.GetComponent<Animator>();
        if (animator != null)
            UnityEngine.Object.DestroyImmediate(animator);
    }

    static T LoadRequired<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            throw new InvalidOperationException($"Missing asset: {path}");

        return asset;
    }

    static string ToAbsoluteAssetPath(string assetPath)
    {
        string relative = assetPath.StartsWith("Assets/", StringComparison.Ordinal)
            ? assetPath.Substring("Assets/".Length)
            : assetPath;

        return Path.Combine(Application.dataPath, relative);
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
