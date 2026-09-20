using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor.RootMotion;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CorinFootMotionAuthoringWorkflow
    {
        const string MenuPath = "3C/Character/Animation/Corin/Build Foot Motion Curves";
        const string ValidateMenuPath = "3C/Character/Animation/Corin/Validate Foot Motion Curves";
        const string ConfigureMenuPath = "3C/Character/Animation/Corin/Configure Foot Motion References";
        const string ProfilePath = "Assets/Configs/Character/Corin/Pipeline/Presentation/Profiles/CorinAnimationPresentationProfile.asset";
        const string GeneratedFolder = "Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated";
        const string MotionTargetFolder = GeneratedFolder + "/InPlaceTargets";
        const string MotionReferenceFolder = GeneratedFolder + "/MotionReferences";
        const string RuntimeCurveSourceFolder = "Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace";
        const string CombatAnimationFolder = "Assets/AssetArt/Animation/ZZZ/可琳/dump";
        const string SourcePath = "Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/CorinFootPlacementAnalysisSource.asset";

        static CharacterFootPlacementAnalysisSource s_Source;
        static AnimationClip[] s_Clips;
        static CorinFootMotionPair[] s_Pairs;
        static List<CharacterFootMotionBakePlan> s_Plans;
        static int s_Index;
        static bool s_Applying;
        static bool s_ReplaceExisting;
        static int s_AppliedCount;

        [MenuItem(MenuPath)]
        public static void Build()
        {
            if (s_Clips != null)
                throw new InvalidOperationException("Corin Foot Motion authoring is already running.");
            s_Source =
                AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(SourcePath);
            if (!s_Source)
                throw new InvalidOperationException($"Corin Foot Analysis Source is missing at '{SourcePath}'.");
            s_Pairs = ResolvePairs(s_Source);
            s_Clips = s_Pairs.Select(value => value.Target).ToArray();
            if (s_Clips.Length == 0)
            {
                Clear();
                throw new InvalidOperationException("Corin Foot Motion authoring found no native AnimationClip.");
            }
            ConfigureCorinMotionReferences(s_Source, s_Pairs);
            s_Source.RequireValid();
            CharacterFootPlacementRigCalibrationAuthoringSession.RebuildGeometryValidation(s_Source);
            s_Plans = new List<CharacterFootMotionBakePlan>(s_Clips.Length);
            s_Index = 0;
            s_Applying = false;
            s_ReplaceExisting = false;
            s_AppliedCount = 0;
            EditorApplication.update += Tick;
        }

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            CharacterFootPlacementAnalysisSource source =
                AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(SourcePath);
            if (!source)
                throw new InvalidOperationException($"Corin Foot Analysis Source is missing at '{SourcePath}'.");
            source.RequireValid();
            CorinFootMotionPair[] pairs = ResolvePairs(source);
            AnimationClip[] clips = pairs.Select(value => value.Target).ToArray();
            for (int i = 0; i < clips.Length; i++)
            {
                CharacterFootMotionReference motionReference = source.RequireMotionReference(clips[i]);
                _ = CharacterFootMotionReferencePairValidator.RequireCompatible(
                    in motionReference,
                    source.RigDefinition,
                    source.MotionRootBoneId);
                _ = CharacterAnimationClipRegisteredCurveCatalog.TryRead(
                    clips[i],
                    CharacterAnimationClipRegisteredCurveChannels.FootPlacementWeight,
                    out _);
                _ = CharacterAnimationClipRegisteredCurveCatalog.TryRead(
                    clips[i],
                    CharacterAnimationClipRegisteredCurveChannels.LocomotionPhase,
                    out _);
                CharacterAnimationClipRegisteredCurveCatalog.ValidateFootMotionGroupRequired(clips[i]);
                AnimationFootAnalysisArtifactIdentity expected =
                    AnimationFootAnalysisArtifactBuilder.GetExpectedIdentity(clips[i], source);
                AnimationFootAnalysisArtifactInspection inspection =
                    AnimationFootAnalysisArtifactStore.Inspect(expected);
                if (inspection.Status != AnimationFootAnalysisArtifactStatus.Ready)
                    throw new InvalidOperationException(
                        $"Corin Foot Motion Artifact '{clips[i].name}' is {inspection.Status}: {inspection.Error}");
                CharacterFootMotionBakePlan plan =
                    CharacterFootMotionBakeService.BuildPlanFromReadyArtifact(source, clips[i]);
                if (!plan.IsNoChange)
                    throw new InvalidOperationException(
                        $"Corin Foot Motion Curve group '{clips[i].name}' differs from its Artifact Candidate: {plan.State}.");
                AnimationFootAnalysisArtifactStore.PruneTargetArtifacts(expected);
            }
            Debug.Log($"Validated complete Foot Motion Curve groups on {clips.Length} Corin AnimationClips.");
        }

        [MenuItem(ConfigureMenuPath)]
        public static void Configure()
        {
            CharacterFootPlacementAnalysisSource source =
                AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(SourcePath);
            if (!source)
                throw new InvalidOperationException($"Corin Foot Analysis Source is missing at '{SourcePath}'.");
            CorinFootMotionPair[] pairs = ResolvePairs(source);
            AnimationClip[] clips = pairs.Select(value => value.Target).ToArray();
            if (clips.Length == 0)
                throw new InvalidOperationException("Corin Foot Motion configuration found no native AnimationClip.");
            ConfigureCorinMotionReferences(source, pairs);
            source.RequireValid();
            CharacterFootPlacementRigCalibrationAuthoringSession.RebuildGeometryValidation(source);
            Debug.Log($"Configured {clips.Length} Corin Foot Motion References.");
        }

        static void Tick()
        {
            try
            {
                if (!s_Applying)
                {
                    AnimationClip clip = s_Clips[s_Index];
                    s_Plans.Add(CharacterFootMotionBakeService.Analyze(s_Source, clip));
                    s_Index++;
                    EditorUtility.DisplayProgressBar(
                        "Corin Foot Motion Data",
                        $"Analyze {s_Index}/{s_Clips.Length}",
                        s_Index / (float)(s_Clips.Length * 2));
                    if (s_Index < s_Clips.Length)
                        return;
                    CharacterFootMotionBakePlan[] replacements =
                        s_Plans.Where(value => value.RequiresReplace).ToArray();
                    if (replacements.Length > 0)
                    {
                        string targets = string.Join(
                            "\n",
                            replacements.Select(value =>
                                $"{value.TargetClip.name}: {value.ChangedChannels.Count} changed channels"));
                        if (!EditorUtility.DisplayDialog(
                                "Replace Corin Foot Motion Curves",
                                $"The following {replacements.Length} clips differ from their new Candidates:\n\n{targets}",
                                "Replace Existing Curves",
                                "Cancel"))
                        {
                            Clear();
                            return;
                        }
                        s_ReplaceExisting = true;
                    }
                    s_Applying = true;
                    s_Index = 0;
                }
                CharacterFootMotionBakePlan plan = s_Plans[s_Index];
                CharacterFootMotionBakeApplyResult result = CharacterFootMotionBakeService.Apply(
                    plan,
                    plan.PlanHash,
                    s_ReplaceExisting);
                if (result.Applied)
                    s_AppliedCount++;
                s_Index++;
                EditorUtility.DisplayProgressBar(
                    "Corin Foot Motion Data",
                    $"Apply {s_Index}/{s_Clips.Length}",
                    (s_Clips.Length + s_Index) / (float)(s_Clips.Length * 2));
                if (s_Index < s_Clips.Length)
                    return;
                int count = s_Clips.Length;
                ConfigureRuntimeCurves(s_Pairs);
                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"Corin Foot Motion Data analyzed {count} AnimationClips and applied {s_AppliedCount}.");
                Clear();
            }
            catch (Exception exception)
            {
                string clipName = s_Clips != null && s_Index >= 0 && s_Index < s_Clips.Length
                    ? s_Clips[s_Index].name
                    : "unknown";
                Clear();
                Debug.LogException(new InvalidOperationException(
                    $"Corin Foot Motion Data failed for '{clipName}': {exception.Message}",
                    exception));
            }
        }

        static void Clear()
        {
            EditorApplication.update -= Tick;
            EditorUtility.ClearProgressBar();
            s_Source = null;
            s_Clips = null;
            s_Pairs = null;
            s_Plans = null;
            s_Index = 0;
            s_Applying = false;
            s_ReplaceExisting = false;
            s_AppliedCount = 0;
        }

        static CorinFootMotionPair[] ResolvePairs(CharacterFootPlacementAnalysisSource source)
        {
            CharacterAnimationPresentationProfile profile =
                AssetDatabase.LoadAssetAtPath<CharacterAnimationPresentationProfile>(ProfilePath);
            if (!profile)
                throw new InvalidOperationException($"Corin Animation Presentation Profile is missing at '{ProfilePath}'.");
            var pairs = new List<CorinFootMotionPair>(profile.PoseSourceBindings.Count);
            var seen = new HashSet<AnimationClip>();
            for (int i = 0; i < profile.PoseSourceBindings.Count; i++)
            {
                CharacterPresentationPoseSourceBinding binding = profile.PoseSourceBindings[i];
                if (binding is not CharacterClipPoseSourceBinding clipBinding || !clipBinding.Clip)
                    throw new InvalidOperationException(
                        $"Corin Pose Source '{binding?.name ?? "missing"}' is not a persisted AnimationClip binding.");
                if (!seen.Add(clipBinding.Clip))
                    throw new InvalidOperationException(
                        $"Corin Pose Profile contains duplicate AnimationClip '{clipBinding.Clip.name}'.");
                string motion = clipBinding.Slot.name switch
                {
                    "Corin_Pipeline_Idle_Inplace Source Binding" => "Idle",
                    "Corin_Pipeline_RunStart_Inplace Source Binding" => "Run_Start",
                    "Corin_Pipeline_RunLoop_Inplace Source Binding" => "Run",
                    "Corin_Pipeline_RunEnd_Inplace Source Binding" => "Run_End",
                    "Corin_Pipeline_WalkStart_Inplace Source Binding" => "Walk_Start",
                    "Corin_Pipeline_WalkLoop_Inplace Source Binding" => "Walk",
                    "Corin_Pipeline_MovingTurn_Inplace Source Binding" => "TurnBack",
                    _ => throw new InvalidOperationException($"Corin combat locomotion Slot '{clipBinding.Slot.name}' is not configured.")
                };
                AnimationClip combatClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    $"{CombatAnimationFolder}/Avatar_Female_Size01_Corin_Ani_{motion}.anim");
                if (!combatClip)
                    throw new InvalidOperationException($"Corin combat animation '{motion}' is missing.");
                AnimationClip target = EnsureMotionTarget(profile, clipBinding, combatClip);
                pairs.Add(new CorinFootMotionPair(target, EnsureMotionReference(combatClip)));
            }
            AssetDatabase.SaveAssets();
            return pairs.ToArray();
        }

        static void ConfigureCorinMotionReferences(
            CharacterFootPlacementAnalysisSource source,
            IReadOnlyList<CorinFootMotionPair> pairs)
        {
            CharacterFootMotionReferenceBinding[] bindings = pairs
                .Select(value => Pair(value.Target, value.MotionReference))
                .ToArray();
            AnimationClip calibrationPreview = pairs
                .SingleOrDefault(value => string.Equals(
                    value.MotionReference.name,
                    "Avatar_Female_Size01_Corin_Ani_Idle_FootMotionReference",
                    StringComparison.Ordinal))
                .Target;
            if (!calibrationPreview)
                throw new InvalidOperationException("Corin Pose Profile has no combat Idle clip for Foot Analysis calibration.");
            Undo.RecordObject(source, "Configure Corin Foot Motion References");
            source.Configure(
                source.AnalysisSourceId,
                source.AnalysisVersion,
                source.SamplingRigAssetGuid,
                source.RigDefinition,
                source.RigCalibration,
                calibrationPreview,
                source.CalibrationPreviewNormalizedTime);
            source.ConfigureMotionReferences(
                new AnimationBoneId("animation-bone/Bip001"),
                bindings);
            EditorUtility.SetDirty(source);
            AssetDatabase.SaveAssets();
        }

        static void ConfigureRuntimeCurves(IReadOnlyList<CorinFootMotionPair> pairs)
        {
            int configured = 0;
            for (int i = 0; i < pairs.Count; i++)
            {
                AnimationClip target = pairs[i].Target;
                AnimationClip donor = LoadRuntimeCurveSource(target);
                Undo.RecordObject(target, "Configure Corin Runtime Curves");
                if (!CharacterAnimationClipRegisteredCurveCatalog.TryRead(
                        target,
                        CharacterAnimationClipRegisteredCurveChannels.FootPlacementWeight,
                        out _))
                {
                    CopyRetimedCurve(
                        donor,
                        target,
                        CharacterAnimationClipRegisteredCurveChannels.FootPlacementWeight);
                    configured++;
                }
                if (!CharacterAnimationClipRegisteredCurveCatalog.TryRead(
                        target,
                        CharacterAnimationClipRegisteredCurveChannels.LocomotionPhase,
                        out _) &&
                    CharacterAnimationClipRegisteredCurveCatalog.TryRead(
                        donor,
                        CharacterAnimationClipRegisteredCurveChannels.LocomotionPhase,
                        out _))
                {
                    CopyRetimedCurve(
                        donor,
                        target,
                        CharacterAnimationClipRegisteredCurveChannels.LocomotionPhase);
                    configured++;
                }
                EditorUtility.SetDirty(target);
            }
            if (configured > 0)
                Debug.Log($"Configured {configured} Corin runtime AnimationClip Curves.");
        }

        static AnimationClip LoadRuntimeCurveSource(AnimationClip target)
        {
            string sourceName = target.name.EndsWith("_FootMotionTarget", StringComparison.Ordinal)
                ? target.name.Substring(0, target.name.Length - "_FootMotionTarget".Length)
                : target.name;
            string donorName = sourceName switch
            {
                "Avatar_Female_Size01_Corin_Ani_Walk" => "Corin_Pipeline_WalkLoop_Inplace",
                "Avatar_Female_Size01_Corin_Ani_Run_Start" => "Corin_Pipeline_RunStart_Inplace",
                "Avatar_Female_Size01_Corin_Ani_Walk_Start" => "Corin_Pipeline_WalkStart_Inplace",
                "Avatar_Female_Size01_Corin_Ani_Run_End" => "Corin_Pipeline_RunEnd_Inplace",
                "Avatar_Female_Size01_Corin_Ani_Idle" => "Corin_Pipeline_Idle_Inplace",
                "Avatar_Female_Size01_Corin_Ani_Run" => "Corin_Pipeline_RunLoop_Inplace",
                "Avatar_Female_Size01_Corin_Ani_TurnBack" => "Corin_Pipeline_MovingTurn_Inplace",
                _ => throw new InvalidOperationException(
                    $"Corin runtime Curve migration has no donor mapping for '{target.name}'.")
            };
            AnimationClip donor = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                $"{RuntimeCurveSourceFolder}/{donorName}.anim");
            if (!donor)
                throw new InvalidOperationException(
                    $"Corin runtime Curve migration donor is missing for '{target.name}'.");
            return donor;
        }

        static void CopyRetimedCurve(
            AnimationClip donor,
            AnimationClip target,
            string channelId)
        {
            if (!CharacterAnimationClipRegisteredCurveCatalog.TryRead(donor, channelId, out AnimationCurve source))
                throw new InvalidOperationException(
                    $"Corin runtime Curve donor '{donor.name}' is missing '{channelId}'.");
            float donorDuration = CharacterAnimationClipRegisteredCurveCatalog.ResolveSourceDurationSeconds(donor);
            float targetDuration = CharacterAnimationClipRegisteredCurveCatalog.ResolveSourceDurationSeconds(target);
            if (donorDuration <= 0f || targetDuration <= 0f)
                throw new InvalidOperationException(
                    $"Corin runtime Curve migration requires positive durations for '{donor.name}' and '{target.name}'.");
            AnimationCurve retimed = RetimedCurve(source, donorDuration, targetDuration);
            CharacterAnimationClipRegisteredCurveCatalog.Validate(target, channelId, retimed);
            CharacterAnimationClipRegisteredCurveCatalog.Replace(target, channelId, retimed);
        }

        static AnimationCurve RetimedCurve(
            AnimationCurve source,
            float sourceDuration,
            float targetDuration)
        {
            float timeScale = targetDuration / sourceDuration;
            float slopeScale = sourceDuration / targetDuration;
            Keyframe[] keys = source.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                key.time *= timeScale;
                key.inTangent *= slopeScale;
                key.outTangent *= slopeScale;
                keys[i] = key;
            }
            return new AnimationCurve(keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        static CharacterFootMotionReferenceBinding Pair(
            AnimationClip target,
            AnimationClip motionReference)
        {
            return CharacterFootMotionReferenceBinding.Create(target, EnsureMotionReference(motionReference));
        }

        static AnimationClip EnsureMotionTarget(
            CharacterAnimationPresentationProfile profile,
            CharacterClipPoseSourceBinding binding,
            AnimationClip source)
        {
            EnsureFolder(MotionTargetFolder);
            string destinationPath =
                $"{MotionTargetFolder}/{source.name}_FootMotionTarget.anim";
            AnimationClip target = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
            if (!target)
            {
                target = AnimationClipNormalizationAuthoringService.CreateRootOffsetNormalizedCopy(
                    source,
                    destinationPath);
                AssetDatabase.SaveAssets();
            }
            if (binding.Clip != target)
            {
                Undo.RecordObject(binding, "Bind Corin Foot Motion Target");
                binding.Configure(binding.Slot as CharacterClipPoseSourceSlot, target);
                EditorUtility.SetDirty(binding);
                EditorUtility.SetDirty(profile);
            }
            return target;
        }

        static AnimationClip EnsureMotionReference(AnimationClip source)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (sourcePath.StartsWith(MotionReferenceFolder + "/", StringComparison.Ordinal))
                return source;
            EnsureFolder(MotionReferenceFolder);
            string destinationPath =
                $"{MotionReferenceFolder}/{source.name}_FootMotionReference.anim";
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
            if (existing)
                return existing;
            if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
                throw new InvalidOperationException(
                    $"Could not create Foot Motion Reference for '{source.name}'.");
            AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceUpdate);
            AnimationClip created = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
            if (!created)
                throw new InvalidOperationException(
                    $"Created Foot Motion Reference '{destinationPath}' cannot be loaded.");
            return created;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            const string parent = "Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement";
            if (!AssetDatabase.IsValidFolder(parent))
                throw new InvalidOperationException("Corin Foot Placement folder is missing.");
            string relativePath = folder.Substring(parent.Length + 1);
            string current = parent;
            foreach (string segment in relativePath.Split('/'))
            {
                string next = $"{current}/{segment}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segment);
                current = next;
            }
        }

        readonly struct CorinFootMotionPair
        {
            public CorinFootMotionPair(AnimationClip target, AnimationClip motionReference)
            {
                Target = target;
                MotionReference = motionReference;
            }

            public AnimationClip Target { get; }
            public AnimationClip MotionReference { get; }
        }
    }
}
