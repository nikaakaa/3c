using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Animancer;
using Animancer.TransitionLibraries;
using ThirdPersonAction;
using ThirdPersonAnimation;
using ThirdPersonAnimation.EditorTools;
using ThirdPersonMovement;
using UnityEditor;
using UnityEngine;
using ThirdPersonDiagnostics;

public static class CorinHumanoidPresentationAssembler
{
    const string SourcePrefabPath = "Assets/Prefabs/Character/可琳.prefab";
    const string OutputPrefabPath = "Assets/Prefabs/Character/可琳_Humanoid.prefab";
    const string HumanoidModelPath = "Assets/Art/Model/ZZZ/可琳/可琳Humanoid.fbx";
    const string HumanoidConfigRoot = "Assets/Configs/3C/Animation/Corin/Animancer/Reference/Humanoid";
    const string TransitionFolder = HumanoidConfigRoot + "/TransitionAsset";
    const string LibraryPath = HumanoidConfigRoot + "/CorinHumanoid_TransitionLib.asset";
    const string AliasFolder = "Assets/Configs/3C/Animation/Corin/Animancer/Parameters";
    const string RunOnceMarkerPath = "Assets/Editor/CorinHumanoidPresentationAssembler.runonce";

    static readonly TransitionSpec[] TransitionSpecs =
    {
        new("CorinHumanoid_RunStart", "Corin_RunStart_Inplace.anim", 0.25f),
        new("CorinHumanoid_RunLoop", "Corin_RunLoop_Inplace.anim", 0.25f),
        new("CorinHumanoid_RunEnd", "Corin_Run_End_Inplace.anim", 0.08f),
        new("CorinHumanoid_Idle", "Corin_Idle_Inplace.anim", 0.25f),
        new("CorinHumanoid_WalkStart", "Corin_Walk_Start_Inplace.anim", 0.25f),
        new("CorinHumanoid_WalkLoop", "Corin_Walk_Inplace.anim", 0.25f),
        new("CorinHumanoid_DodgeDirectional", "Corin_Evade_Front_Inplace.anim", 0.25f),
        new("CorinHumanoid_DodgeBackstep", "Corin_Evade_Back_Inplace.anim", 0.25f),
        new("CorinHumanoid_TurnBack", "Corin_TurnBack_Inplace.anim", 0.25f),
    };

    static readonly AliasSpec[] AliasSpecs =
    {
        new("RunStart", 0),
        new("RunLoop", 1),
        new("RunEnd", 2),
        new("Idle", 3),
        new("WalkEnd", 3),
        new("WalkStart", 4),
        new("WalkLoop", 5),
        new("Action.Dodge.Directional", 6),
        new("Action.Dodge.Backstep", 7),
        new("Locomotion.Turn.Back", 8),
    };

    static readonly TransitionModifierDefinition[] Modifiers =
    {
        new(2, 0, 0f),
        new(2, 3, 0f),
        new(0, 5, 0.25f),
        new(1, 5, 0.25f),
        new(2, 5, 0.25f),
        new(3, 5, 0.25f),
        new(4, 5, 0.25f),
        new(5, 5, 0.25f),
        new(0, 4, 0.25f),
        new(1, 4, 0.25f),
        new(2, 4, 0.25f),
        new(3, 4, 0.25f),
        new(4, 4, 0.25f),
        new(5, 4, 0.25f),
    };

    [MenuItem("Tools/3C/Corin/Build Humanoid Presentation Prefab")]
    public static void Build()
    {
        CorinHumanoidMuscleClipBaker.BakePresentationClips();
        EnsureFolders();

        TransitionAsset[] transitions = BuildTransitions();
        TransitionLibraryAsset library = BuildLibrary(transitions);
        AssetDatabase.SaveAssets();
        GameObject prefab = BuildPrefab(library);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Validate();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
         RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(RuntimeDiagnosticLogCategory.Editor, RuntimeDiagnosticLogLevel.Info, "humanoid-presentation-built", "", "", 0, Time.frameCount, $"[CorinHumanoidPresentationAssembler] Built {OutputPrefabPath} with {LibraryPath}."));
    }

    [InitializeOnLoadMethod]
    static void RunOnceOnLoad()
    {
        if (!File.Exists(AbsoluteRunOnceMarkerPath()))
            return;

        EditorApplication.delayCall -= ExecuteRunOnce;
        EditorApplication.delayCall += ExecuteRunOnce;
    }

    [MenuItem("Tools/3C/Corin/Validate Humanoid Presentation Prefab")]
    public static void ValidateFromMenu()
    {
        Validate();
         RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(RuntimeDiagnosticLogCategory.Editor, RuntimeDiagnosticLogLevel.Info, "humanoid-presentation-validation-passed", "", "", 0, Time.frameCount, "[CorinHumanoidPresentationAssembler] Humanoid presentation prefab validation passed."));
    }

    public static void Validate()
    {
        GameObject prefab = LoadRequired<GameObject>(OutputPrefabPath);
        TransitionLibraryAsset library = LoadRequired<TransitionLibraryAsset>(LibraryPath);
        Avatar avatar = FindHumanoidAvatar();

        Animator animator = prefab.GetComponentsInChildren<Animator>(true).FirstOrDefault();
        if (animator == null)
            throw new InvalidOperationException($"{OutputPrefabPath} has no Animator.");

        if (animator.avatar != avatar)
            throw new InvalidOperationException($"{OutputPrefabPath} does not use {HumanoidModelPath} Avatar.");

        if (animator.runtimeAnimatorController != null)
            throw new InvalidOperationException($"{OutputPrefabPath} still has an Animator Controller.");

        if (animator.transform.Find("Bip001") == null)
            throw new InvalidOperationException($"{OutputPrefabPath} Animator is not on the Humanoid model root.");

        AnimancerComponent animancer = animator.GetComponent<AnimancerComponent>();
        if (animancer == null)
            throw new InvalidOperationException($"{OutputPrefabPath} has no AnimancerComponent.");

        if (animancer.Transitions != library)
            throw new InvalidOperationException($"{OutputPrefabPath} does not use {LibraryPath}.");

        CharacterAnimancerPresenter presenter = animator.GetComponent<CharacterAnimancerPresenter>();
        if (presenter == null)
            throw new InvalidOperationException($"{OutputPrefabPath} has no CharacterAnimancerPresenter on the model root.");

        CharacterFrameRuntimeController runtimeController = prefab.GetComponent<CharacterFrameRuntimeController>();
        if (runtimeController != null &&
            (runtimeController.LocomotionPresenterBehaviour != presenter ||
             runtimeController.AnimationPresenterBehaviour != presenter))
        {
            throw new InvalidOperationException($"{OutputPrefabPath} runtime presenter references are not rewired.");
        }

        if (library.Definition.Transitions.Length != TransitionSpecs.Length)
            throw new InvalidOperationException($"{LibraryPath} transition count is invalid.");

        TransitionAssetBase[] libraryTransitions = library.Definition.Transitions;
        for (int i = 0; i < TransitionSpecs.Length; i++)
        {
            TransitionSpec spec = TransitionSpecs[i];
            AnimationClip clip = LoadHumanoidMuscleClip(spec);

            TransitionAsset transition = LoadRequired<TransitionAsset>($"{TransitionFolder}/{spec.AssetName}.asset");
            if (libraryTransitions[i] != transition)
                throw new InvalidOperationException($"{LibraryPath} transition {i} is not {spec.AssetName}.");

            if (transition.GetTransition() is not ClipTransition clipTransition)
                throw new InvalidOperationException($"{spec.AssetName} is not a ClipTransition.");

            if (clipTransition.Clip != clip)
                throw new InvalidOperationException($"{spec.AssetName} does not use {ClipPath(spec.ClipName)}.");

            if (!Mathf.Approximately(clipTransition.FadeDuration, spec.Fade))
                throw new InvalidOperationException($"{spec.AssetName} fade is invalid.");
        }

        if (library.Definition.Modifiers.Length != Modifiers.Length)
            throw new InvalidOperationException($"{LibraryPath} modifier count is invalid.");

        for (int i = 0; i < Modifiers.Length; i++)
            if (library.Definition.Modifiers[i] != Modifiers[i])
                throw new InvalidOperationException($"{LibraryPath} modifier {i} is invalid.");

        NamedIndex[] aliases = library.Definition.Aliases;
        NamedIndex[] expectedAliases = BuildAliases();
        if (aliases.Length != expectedAliases.Length)
            throw new InvalidOperationException($"{LibraryPath} alias count is invalid.");

        for (int i = 0; i < expectedAliases.Length; i++)
            if (aliases[i] != expectedAliases[i])
                throw new InvalidOperationException($"{LibraryPath} alias {i} is invalid.");

    }

    static void ExecuteRunOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += ExecuteRunOnce;
            return;
        }

        if (!ConsumeRunOnceMarker())
            return;

        try
        {
            Build();
         RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(RuntimeDiagnosticLogCategory.Editor, RuntimeDiagnosticLogLevel.Info, "runonce-build-completed", "", "", 0, Time.frameCount, "[CorinHumanoidPresentationAssembler] Run-once build completed."));
        }
        catch (Exception ex)
        {
         RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(RuntimeDiagnosticLogCategory.Editor, RuntimeDiagnosticLogLevel.Error, "runonce-build-failed", "", "", 0, Time.frameCount, $"[CorinHumanoidPresentationAssembler] Run-once build failed.\n{ex}"));
        }
    }

    static TransitionAsset[] BuildTransitions()
    {
        var result = new TransitionAsset[TransitionSpecs.Length];
        for (int i = 0; i < TransitionSpecs.Length; i++)
        {
            TransitionSpec spec = TransitionSpecs[i];
            string assetPath = $"{TransitionFolder}/{spec.AssetName}.asset";
            AnimationClip clip = LoadHumanoidMuscleClip(spec);

            TransitionAsset asset = AssetDatabase.LoadAssetAtPath<TransitionAsset>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TransitionAsset>();
                asset.name = spec.AssetName;
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.name = spec.AssetName;
            asset.Transition = new ClipTransition
            {
                Clip = clip,
                FadeDuration = spec.Fade,
                Speed = 1f,
                NormalizedStartTime = float.NaN,
            };

            EditorUtility.SetDirty(asset);
            result[i] = asset;
        }

        return result;
    }

    static TransitionLibraryAsset BuildLibrary(TransitionAsset[] transitions)
    {
        TransitionLibraryAsset library = AssetDatabase.LoadAssetAtPath<TransitionLibraryAsset>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<TransitionLibraryAsset>();
            library.name = "CorinHumanoid_TransitionLib";
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        var definition = new TransitionLibraryDefinition
        {
            Transitions = transitions,
            Modifiers = Modifiers,
            Aliases = BuildAliases(),
        };
        definition.AliasAllTransitions = false;
        library.name = "CorinHumanoid_TransitionLib";
        library.Definition = definition;

        EditorUtility.SetDirty(library);
        return library;
    }

    static NamedIndex[] BuildAliases()
    {
        var aliases = new List<NamedIndex>(AliasSpecs.Length);
        foreach (AliasSpec spec in AliasSpecs)
        {
            StringAsset alias = LoadRequired<StringAsset>($"{AliasFolder}/{spec.Name}.asset");
            aliases.Add(new NamedIndex(alias, spec.Index));
        }

        aliases.Sort((a, b) => a.CompareTo(b));
        return aliases.ToArray();
    }

    static GameObject BuildPrefab(TransitionLibraryAsset library)
    {
        GameObject source = LoadRequired<GameObject>(SourcePrefabPath);
        GameObject model = LoadRequired<GameObject>(HumanoidModelPath);
        Avatar avatar = FindHumanoidAvatar();
        GameObject instance = null;

        try
        {
            instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Could not instantiate {SourcePrefabPath}.");

            instance.name = "可琳_Humanoid";
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Transform visualRoot = FindChild(instance.transform, "CharacterVisualRoot");
            if (visualRoot == null)
                throw new InvalidOperationException($"{SourcePrefabPath} has no CharacterVisualRoot.");

            int visualLayer = ResolveVisualLayer(visualRoot);
            RemoveAnimationOutputComponents(visualRoot.gameObject);
            ClearChildren(visualRoot);

            GameObject modelInstance = PrefabUtility.InstantiatePrefab(model, visualRoot) as GameObject;
            if (modelInstance == null)
                throw new InvalidOperationException($"Could not instantiate {HumanoidModelPath}.");

            Transform modelTransform = modelInstance.transform;
            modelInstance.name = "可琳Humanoid_Model";
            modelTransform.localPosition = Vector3.zero;
            modelTransform.localRotation = Quaternion.identity;
            modelTransform.localScale = Vector3.one;
            SetLayerRecursively(modelInstance, visualLayer);

            Animator animator = modelInstance.GetComponent<Animator>();
            if (animator == null)
                animator = modelInstance.AddComponent<Animator>();

            animator.avatar = avatar;
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;

            foreach (Animator nested in modelInstance.GetComponentsInChildren<Animator>(true))
                if (nested != animator)
                    UnityEngine.Object.DestroyImmediate(nested);

            AnimancerComponent animancer = modelInstance.GetComponent<AnimancerComponent>();
            if (animancer == null)
                animancer = modelInstance.AddComponent<AnimancerComponent>();

            animancer.Animator = animator;
            animancer.Transitions = library;

            CharacterAnimancerPresenter presenter = modelInstance.GetComponent<CharacterAnimancerPresenter>();
            if (presenter == null)
                presenter = modelInstance.AddComponent<CharacterAnimancerPresenter>();

            RewireControllers(instance, presenter);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Could not save {OutputPrefabPath}.");

            return prefab;
        }
        finally
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    static Avatar FindHumanoidAvatar()
    {
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(HumanoidModelPath)
            .OfType<Avatar>()
            .FirstOrDefault(x => x != null && x.isHuman && x.isValid);

        if (avatar == null)
            throw new InvalidOperationException($"{HumanoidModelPath} does not contain a valid Humanoid Avatar.");

        return avatar;
    }

    static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    static void RemoveAnimationOutputComponents(GameObject target)
    {
        RemoveComponent<CharacterAnimancerPresenter>(target);
        RemoveComponent<AnimancerComponent>(target);
        RemoveComponent<Animator>(target);
    }

    static void RemoveComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component != null)
            UnityEngine.Object.DestroyImmediate(component);
    }

    static void RewireControllers(
        GameObject root,
        CharacterAnimancerPresenter presenter)
    {
        CharacterFrameRuntimeController runtimeController = root.GetComponent<CharacterFrameRuntimeController>();
        if (runtimeController != null)
        {
            runtimeController.LocomotionPresenterBehaviour = presenter;
            runtimeController.AnimationPresenterBehaviour = presenter;
        }
    }

    static int ResolveVisualLayer(Transform visualRoot)
    {
        Renderer renderer = visualRoot.GetComponentInChildren<Renderer>(true);
        return renderer != null ? renderer.gameObject.layer : visualRoot.gameObject.layer;
    }

    static void SetLayerRecursively(GameObject root, int layer)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }

    static Transform FindChild(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == childName)
                return child;

        return null;
    }

    static T LoadRequired<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            throw new InvalidOperationException($"Missing asset: {path}");

        return asset;
    }

    static string ClipPath(string clipName)
    {
        return $"{CorinHumanoidMuscleClipBaker.OutputInplaceFolder}/{clipName}";
    }

    static AnimationClip LoadHumanoidMuscleClip(TransitionSpec spec)
    {
        string path = ClipPath(spec.ClipName);
        AnimationClip clip = LoadRequired<AnimationClip>(path);
        if (clip.legacy)
            throw new InvalidOperationException($"{path} is a legacy clip.");

        if (!CorinHumanoidMuscleClipBaker.IsHumanoidMuscleClip(clip))
            throw new InvalidOperationException($"{path} is not a Unity Humanoid muscle clip.");

        return clip;
    }

    static bool ConsumeRunOnceMarker()
    {
        string path = AbsoluteRunOnceMarkerPath();
        if (!File.Exists(path))
            return false;

        File.Delete(path);
        string metaPath = path + ".meta";
        if (File.Exists(metaPath))
            File.Delete(metaPath);

        return true;
    }

    static string AbsoluteRunOnceMarkerPath()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            throw new InvalidOperationException("Could not resolve Unity project root.");

        return Path.Combine(projectRoot, RunOnceMarkerPath.Replace('/', Path.DirectorySeparatorChar));
    }

    static void EnsureFolders()
    {
        EnsureFolder(HumanoidConfigRoot);
        EnsureFolder(TransitionFolder);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    readonly struct TransitionSpec
    {
        public readonly string AssetName;
        public readonly string ClipName;
        public readonly float Fade;

        public TransitionSpec(string assetName, string clipName, float fade)
        {
            AssetName = assetName;
            ClipName = clipName;
            Fade = fade;
        }
    }

    readonly struct AliasSpec
    {
        public readonly string Name;
        public readonly int Index;

        public AliasSpec(string name, int index)
        {
            Name = name;
            Index = index;
        }
    }
}
