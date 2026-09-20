using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterPoseNativeDomainResourceSetCompiler
    {
        const string CorinProfilePath =
            "Assets/Configs/Character/Corin/Pipeline/Presentation/Profiles/CorinAnimationPresentationProfile.asset";

        [MenuItem("3C/Character/Animation/Compile Pose Domain Resource Set")]
        public static void CompileSelectedProfileResourceSet()
        {
            CharacterAnimationPresentationProfile profile =
                Selection.activeObject as CharacterAnimationPresentationProfile;
            if (!profile)
                throw new InvalidOperationException(
                    "Select a Character Animation Presentation Profile to compile its Pose Domain Resource Set.");
            Compile(profile);
        }

        [MenuItem("3C/Character/Animation/Corin/Compile Pose Domain Resource Set")]
        public static void CompileCorinResourceSet()
        {
            CharacterAnimationPresentationProfile profile =
                AssetDatabase.LoadAssetAtPath<CharacterAnimationPresentationProfile>(CorinProfilePath);
            if (!profile)
                throw new InvalidOperationException(
                    $"Corin Character Animation Presentation Profile is missing at '{CorinProfilePath}'.");
            Compile(profile);
        }

        internal static void Compile(CharacterAnimationPresentationProfile profile)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            CharacterAnimationRigDefinition rig = profile.RigDefinition;
            if (!rig)
                throw new InvalidOperationException(
                    $"Presentation Profile '{profile.name}' has no Rig Definition.");
            CharacterPoseNativeDomainResourceSet resourceSet = profile.PoseNativeDomainResources;
            if (!resourceSet)
                throw new InvalidOperationException(
                    $"Presentation Profile '{profile.name}' has no Pose Native Domain Resource Set.");
            CharacterFootPlacementAnalysisSource analysisSource =
                ResolveAnalysisSource(profile);
            CharacterPresentationPoseSourceCompilationResult compilation =
                CharacterPresentationPoseSourceCompiler.Compile(profile);
            RequireNoDiagnostics(profile, compilation.Diagnostics);
            var plans = new List<CharacterPresentationPoseSourcePlan>(
                compilation.Catalog.Entries.Count);
            var slots = new List<CharacterPresentationPoseSourceSlot>(
                compilation.Catalog.Entries.Count);
            var seenIndices = new HashSet<PresentationPoseSourceIndex>();
            foreach (CharacterPresentationPoseSourceCompilationEntry entry in
                     compilation.Catalog.Entries)
            {
                if (!entry.SourceIndex.IsValid || !seenIndices.Add(entry.SourceIndex))
                    throw new InvalidOperationException(
                        "Pose source compilation produced a missing or duplicated source index.");
                if (entry.Binding is not CharacterClipPoseSourceBinding clipBinding)
                    throw new InvalidOperationException(
                        $"Pose source binding kind " +
                        $"{entry.Binding?.GetType().Name ?? "missing"} is not supported by the Pose Domain Resource Set compiler.");
                plans.Add(CompileClipPlan(entry.SourceIndex, clipBinding, rig, analysisSource));
                slots.Add(clipBinding.Slot);
            }
            resourceSet.ReplaceSourcePlans(plans, slots);
            EditorUtility.SetDirty(resourceSet);
            AssetDatabase.SaveAssetIfDirty(resourceSet);
            Debug.Log(
                $"Pose Domain Resource Set '{resourceSet.name}' compiled " +
                $"{plans.Count} source plans from profile '{profile.name}'.");
        }

        static CharacterPresentationPoseSourcePlan CompileClipPlan(
            PresentationPoseSourceIndex sourceIndex,
            CharacterClipPoseSourceBinding binding,
            CharacterAnimationRigDefinition rig,
            CharacterFootPlacementAnalysisSource analysisSource)
        {
            binding.RequireValid(rig);
            AnimationClip clip = binding.Clip;
            CharacterAnimationClipContentIdentity identity =
                CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
            CharacterAnimationClipRegisteredCurveCatalog.ValidateFootMotionGroupRequired(clip);
            AnimationCurve weightCurve =
                CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                    clip,
                    CharacterAnimationClipRegisteredCurveChannels.FootPlacementWeight);
            AnimationFootAnalysisArtifact artifact =
                AnimationFootAnalysisArtifactBuilder.Build(clip, analysisSource);
            AnimationFootStepObservationCurvePair footStepObservation =
                CharacterPresentationFootEventCompiler.CompileFootStepObservation(
                    clip,
                    identity.SourceDurationSeconds,
                    artifact.MotionData);
            string identityKey = $"{identity.AssetGuid}:{identity.LocalFileId}";
            return new CharacterPresentationPoseSourcePlan(
                sourceIndex,
                $"clip:{identityKey}",
                clip,
                rig,
                analysisSource.AnalysisSourceId.Value,
                identityKey,
                identity.FullDependencyHash,
                identity.AnalysisInputHash,
                identity.RegisteredCurveHash,
                identity.SourceDurationSeconds,
                CharacterPresentationFootEventCompiler.NormalizeRegisteredCurve(
                    weightCurve,
                    identity.SourceDurationSeconds),
                footStepObservation,
                artifact.Features,
                CharacterAnimationSamplingBackendKind.NativeClip);
        }

        static CharacterFootPlacementAnalysisSource ResolveAnalysisSource(
            CharacterAnimationPresentationProfile profile)
        {
            string analysisSourceGuid = profile.FootPlacementAnalysisSourceAssetGuid;
            if (string.IsNullOrWhiteSpace(analysisSourceGuid))
                throw new InvalidOperationException(
                    $"Presentation Profile '{profile.name}' has no Foot Placement Analysis Source.");
            string analysisSourcePath = AssetDatabase.GUIDToAssetPath(analysisSourceGuid);
            CharacterFootPlacementAnalysisSource analysisSource =
                string.IsNullOrEmpty(analysisSourcePath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(
                        analysisSourcePath);
            if (!analysisSource)
                throw new InvalidOperationException(
                    $"Foot Placement Analysis Source GUID '{analysisSourceGuid}' does not resolve on profile '{profile.name}'.");
            analysisSource.RequireValid();
            return analysisSource;
        }

        static void RequireNoDiagnostics(
            CharacterAnimationPresentationProfile profile,
            IReadOnlyList<string> diagnostics)
        {
            if (diagnostics == null || diagnostics.Count == 0)
                return;
            throw new InvalidOperationException(
                $"Pose source compilation for profile '{profile.name}' failed:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, diagnostics));
        }
    }
}
