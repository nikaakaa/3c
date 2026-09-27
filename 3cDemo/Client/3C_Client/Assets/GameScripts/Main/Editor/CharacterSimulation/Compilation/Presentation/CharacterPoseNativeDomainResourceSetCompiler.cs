using System;
using System.Collections.Generic;
using System.Globalization;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation;
using ThirdPersonCharacter.Pipeline;
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
        const string CorinDefinitionPath =
            "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset";

        [MenuItem("3C/Character/Animation/Compile Animation Domain Resources")]
        public static void CompileSelectedDefinitionResourceSet()
        {
            CharacterPipelineDefinition definition =
                Selection.activeObject as CharacterPipelineDefinition;
            if (!definition)
                throw new InvalidOperationException(
                    "Select a Character Pipeline Definition to compile its Animation Domain Resources.");
            Compile(definition);
        }

        [MenuItem("3C/Character/Animation/Corin/Compile Animation Domain Resources")]
        public static void CompileCorinResourceSet()
        {
            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(
                    CorinDefinitionPath);
            if (!definition)
                throw new InvalidOperationException(
                    $"Corin Character Pipeline Definition is missing at '{CorinDefinitionPath}'.");
            Compile(definition);
        }

        internal static void Compile(CharacterPipelineDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            CharacterAnimationPresentationProfile profile =
                definition.AnimationPresentationProfile;
            if (!profile)
                throw new InvalidOperationException(
                    $"Character Definition '{definition.name}' has no Animation Presentation Profile.");
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
            CharacterAnimationBuildInput buildInput =
                CharacterAnimationBuildInputFactory.Create(definition);
            var errors = new List<string>();
            Dictionary<AnimationClip, CharacterAnimationBuildCatalogEntry>
                catalogEntries =
                    CharacterAnimationBuildInputFactory
                        .RegisterDeclaredSources(buildInput, errors);
            if (errors.Count != 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            CharacterAnimationBuildCatalog animationCatalog =
                buildInput.AnimationCatalog.Complete(errors);
            if (errors.Count != 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
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
                plans.Add(CompileClipPlan(
                    entry.SourceIndex,
                    clipBinding,
                    rig,
                    analysisSource,
                    profile.FindLocomotionSyncGroup(clipBinding.Clip),
                    catalogEntries));
                slots.Add(clipBinding.Slot);
            }
            List<CharacterActionAnimationSourcePlan> actionPlans =
                CompileActionPlans(profile, analysisSource, catalogEntries);
            resourceSet.ReplaceAnimationSources(
                plans,
                slots,
                actionPlans,
                animationCatalog.AnimationResources);
            EditorUtility.SetDirty(resourceSet);
            AssetDatabase.SaveAssetIfDirty(resourceSet);
            Debug.Log(
                $"Animation Domain Resource Set '{resourceSet.name}' compiled " +
                $"{plans.Count} Pose sources and {actionPlans.Count} Action sources from profile '{profile.name}'.");
        }

        static void CompileLocomotionPhasePlan(
            AnimationClip clip,
            CharacterLocomotionSyncGroup group,
            AnimationFootAnalysisArtifact artifact,
            CharacterPresentationPoseSourcePlan plan)
        {
            if (group == null)
                return;
            plan.SetPhase(group.GroupId,
                CharacterLocomotionPhaseAuthoringService.CompilePhasePlan(clip, artifact));
        }

        static List<CharacterActionAnimationSourcePlan> CompileActionPlans(
            CharacterAnimationPresentationProfile profile,
            CharacterFootPlacementAnalysisSource analysisSource,
            IReadOnlyDictionary<AnimationClip, CharacterAnimationBuildCatalogEntry>
                catalogEntries)
        {
            var result = new List<CharacterActionAnimationSourcePlan>(
                profile.SourceResourceBindings.Count);
            for (int i = 0; i < profile.SourceResourceBindings.Count; i++)
            {
                CharacterAnimationSourceResourceBinding binding =
                    profile.SourceResourceBindings[i];
                CharacterAnimationBuildCatalogEntry entry =
                    catalogEntries[binding.AuthoringClip];
                CharacterAnimationClipContentIdentity identity =
                    CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(
                        binding.AuthoringClip);
                result.Add(new CharacterActionAnimationSourcePlan(
                    binding.AuthoringClip,
                    $"{identity.AssetGuid}/{identity.LocalFileId.ToString(CultureInfo.InvariantCulture)}",
                    identity.FullDependencyHash,
                    entry.Backend,
                    identity.SourceDurationSeconds,
                    identity.Loop,
                    entry.ResourceCatalogIndex,
                    entry.GroupClipIndex,
                    CharacterPresentationFootEventCompiler.CompileFootStepObservation(
                        binding.AuthoringClip,
                        identity.SourceDurationSeconds,
                        RequireAnalysisArtifact(binding.AuthoringClip, analysisSource).MotionData)));
            }
            return result;
        }

        static CharacterPresentationPoseSourcePlan CompileClipPlan(
            PresentationPoseSourceIndex sourceIndex,
            CharacterClipPoseSourceBinding binding,
            CharacterAnimationRigDefinition rig,
            CharacterFootPlacementAnalysisSource analysisSource,
            CharacterLocomotionSyncGroup syncGroup,
            IReadOnlyDictionary<AnimationClip, CharacterAnimationBuildCatalogEntry>
                catalogEntries)
        {
            binding.RequireValid(rig);
            AnimationClip clip = binding.Clip;
            CharacterAnimationClipContentIdentity identity =
                CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
            if (!catalogEntries.TryGetValue(
                    clip,
                    out CharacterAnimationBuildCatalogEntry catalogEntry))
            {
                throw new InvalidOperationException(
                    $"Pose source Clip '{clip.name}' has no declared Animation Source Resource Binding.");
            }
            CharacterAnimationClipRegisteredCurveCatalog.ValidateFootMotionGroupRequired(clip);
            AnimationCurve weightCurve =
                CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                    clip,
                    CharacterAnimationClipRegisteredCurveChannels.FootPlacementWeight);
            AnimationFootAnalysisArtifact artifact =
                RequireAnalysisArtifact(clip, analysisSource);
            AnimationFootStepObservationCurvePair footStepObservation =
                CharacterPresentationFootEventCompiler.CompileFootStepObservation(
                    clip,
                    identity.SourceDurationSeconds,
                    artifact.MotionData);
            string identityKey = $"{identity.AssetGuid}:{identity.LocalFileId}";
            var plan = new CharacterPresentationPoseSourcePlan(
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
                catalogEntry.Backend,
                catalogEntry.NativeScalarPage,
                catalogEntry.ResourceCatalogIndex,
                catalogEntry.GroupClipIndex);
            CompileLocomotionPhasePlan(clip, syncGroup, artifact, plan);
            return plan;
        }

        static AnimationFootAnalysisArtifact RequireAnalysisArtifact(
            AnimationClip clip,
            CharacterFootPlacementAnalysisSource source)
        {
            AnimationFootAnalysisArtifactInspection inspection =
                AnimationFootAnalysisArtifactBuilder.Inspect(clip, source);
            if (inspection.Status != AnimationFootAnalysisArtifactStatus.Ready)
                throw new InvalidOperationException(
                    $"Clip '{clip.name}' Foot Analysis artifact is {inspection.Status}: {inspection.Error}. Rebuild its analysis before compiling animation resources.");
            return inspection.Artifact;
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
