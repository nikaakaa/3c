using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterFootMotionDataMigrationWorkflow
    {
        const string MenuPath = "Tools/3C/Character/Build Corin Foot Motion Curves";
        const string ValidateMenuPath = "Tools/3C/Character/Validate Corin Foot Motion Curves";
        const string ClipFolder = "Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace";
        const string MotionFolder = "Assets/AssetArt/Animation/MyDemoNeed/Corin/WithWeaponRootmotion";
        const string SourcePath = "Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/CorinFootPlacementAnalysisSource.asset";

        static CharacterFootPlacementAnalysisSource s_Source;
        static AnimationClip[] s_Clips;
        static List<CharacterFootMotionBakePlan> s_Plans;
        static int s_Index;
        static bool s_Applying;
        static bool s_ReplaceExisting;
        static int s_AppliedCount;

        [MenuItem(MenuPath)]
        public static void BuildCorin()
        {
            if (s_Clips != null)
                throw new InvalidOperationException("Corin Foot Motion migration is already running.");
            s_Source =
                AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(SourcePath);
            if (!s_Source)
                throw new InvalidOperationException($"Corin Foot Analysis Source is missing at '{SourcePath}'.");
            ConfigureCorinMotionReferences(s_Source);
            s_Source.RequireValid();
            s_Clips = ResolveClips();
            if (s_Clips.Length == 0)
            {
                Clear();
                throw new InvalidOperationException("Corin Foot Motion migration found no native AnimationClip.");
            }
            CharacterFootPlacementRigCalibrationAuthoringSession.RebuildGeometryValidation(s_Source);
            s_Plans = new List<CharacterFootMotionBakePlan>(s_Clips.Length);
            s_Index = 0;
            s_Applying = false;
            s_ReplaceExisting = false;
            s_AppliedCount = 0;
            EditorApplication.update += Tick;
        }

        [MenuItem(ValidateMenuPath)]
        public static void ValidateCorin()
        {
            CharacterFootPlacementAnalysisSource source =
                AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(SourcePath);
            if (!source)
                throw new InvalidOperationException($"Corin Foot Analysis Source is missing at '{SourcePath}'.");
            source.RequireValid();
            AnimationClip[] clips = ResolveClips();
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
                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"Corin Foot Motion Data analyzed {count} AnimationClips and applied {s_AppliedCount}; TrainingEnemy was not touched.");
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
            s_Plans = null;
            s_Index = 0;
            s_Applying = false;
            s_ReplaceExisting = false;
            s_AppliedCount = 0;
        }

        static AnimationClip[] ResolveClips() =>
            AssetDatabase.FindAssets("t:AnimationClip", new[] { ClipFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
                .Select(AssetDatabase.LoadAssetAtPath<AnimationClip>)
                .Where(clip => clip)
                .OrderBy(clip => clip.name, StringComparer.Ordinal)
                .ToArray();

        static void ConfigureCorinMotionReferences(CharacterFootPlacementAnalysisSource source)
        {
            CharacterFootMotionReferenceBinding[] bindings =
            {
                Pair("Corin_Pipeline_Attack1_End_Inplace.anim", "Corin_Attack_Normal_01_End_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_Attack1_Inplace.anim", "Corin_Attack_Normal_01_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_Attack2_End_Inplace.anim", "Corin_Attack_Normal_02_End_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_Attack2_Inplace.anim", "Corin_Attack_Normal_02_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_Attack3_End_Inplace.anim", "Corin_Attack_Normal_03_End_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_Attack3_Inplace.anim", "Corin_Attack_Normal_03_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_Attack4_End_Inplace.anim", "Corin_Attack_Normal_04_End_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_Attack4_Inplace.anim", "Corin_Attack_Normal_04_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_Attack5_End_Inplace.anim", "Corin_Attack_Normal_05_End_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_Attack5_Inplace.anim", "Corin_Attack_Normal_05_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_DodgeBack_Inplace.anim", "Corin_Evade_Back_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_DodgeForward_Inplace.anim", "Corin_Evade_Front_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_Idle_Inplace.anim", "Corin_Idle_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_MovingTurn_Inplace.anim", "Corin_TurnBack_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_RunEnd_Inplace.anim", "Corin_RunEnd_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_RunLoop_Inplace.anim", "Corin_RunLoop_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_RunStart_Inplace.anim", "Corin_RunStart_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_RushAttack_End_Inplace.anim", "Corin_Attack_Rush_End_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_RushAttack_Inplace.anim", "Corin_Attack_Rush_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_WalkLoop_Inplace.anim", "Corin_Walk_WithWeaponRootmotion.anim"),
                Pair("Corin_Pipeline_WalkStart_Inplace.anim", "Corin_Walk_Start_WithWeaponRootmotion.anim")
            };
            Undo.RecordObject(source, "Configure Corin Foot Motion References");
            source.ConfigureMotionReferences(
                new AnimationBoneId("animation-bone/Bip001"),
                bindings);
            EditorUtility.SetDirty(source);
            AssetDatabase.SaveAssetIfDirty(source);
        }

        static CharacterFootMotionReferenceBinding Pair(
            string targetName,
            string motionName,
            string motionFolder = MotionFolder)
        {
            AnimationClip target = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipFolder}/{targetName}");
            AnimationClip motion = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{motionFolder}/{motionName}");
            if (!target || !motion)
                throw new InvalidOperationException($"Corin Foot Motion pair '{targetName}'/'{motionName}' is missing.");
            return CharacterFootMotionReferenceBinding.Create(target, motion);
        }
    }
}
