using System;
using System.Collections.Generic;
using System.Globalization;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation
{
    internal static class CharacterAnimationBuildInputFactory
    {
        internal static CharacterAnimationBuildInput Create(
            CharacterPipelineDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            CharacterAnimationPresentationProfile profile =
                definition.AnimationPresentationProfile ??
                throw new InvalidOperationException(
                    "Character Definition has no Animation Presentation Profile.");
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string definitionGuid = AssetDatabase.AssetPathToGUID(definitionPath);
            if (string.IsNullOrWhiteSpace(definitionGuid))
                throw new InvalidOperationException(
                    "Animation build requires a persisted Character Definition.");
            CharacterAnimationInputContract contract =
                CharacterAnimationInputContract.Create(profile);
            return new CharacterAnimationBuildInput(
                definitionGuid,
                CharacterAnimationBuildInput.RequireNativeArtifactIdentity(),
                profile,
                CreateSourceRig(profile),
                CharacterAnimationParameterLayoutCompiler.Build(contract),
                profile.AnimationCompression,
                profile.SourceResourceBindings);
        }

        internal static Dictionary<AnimationClip,
            CharacterAnimationBuildCatalogEntry> RegisterDeclaredSources(
            CharacterAnimationBuildInput buildInput,
            List<string> errors)
        {
            if (buildInput == null)
                throw new ArgumentNullException(nameof(buildInput));
            var result = new Dictionary<AnimationClip,
                CharacterAnimationBuildCatalogEntry>();
            for (int i = 0;
                 i < buildInput.SourceResourceBindings.Count;
                 i++)
            {
                CharacterAnimationSourceResourceBinding binding =
                    buildInput.SourceResourceBindings[i] ??
                    throw new InvalidOperationException(
                        $"Animation Source Resource Binding #{i} is missing.");
                binding.RequireValid();
                CharacterAnimationScalarCurvePage scalarPage =
                    binding.Backend ==
                    CharacterAnimationSamplingBackendKind.NativeClip
                        ? buildInput.AnimationCatalog.BuildNativeScalarPage(
                            binding.AuthoringClip,
                            errors)
                        : null;
                CharacterAnimationBuildCatalogEntry entry =
                    buildInput.AnimationCatalog.RegisterReference(
                        binding.AuthoringClip,
                        binding.Backend,
                        scalarPage,
                        $"declared-source:{i.ToString(CultureInfo.InvariantCulture)}");
                if (!result.TryAdd(binding.AuthoringClip, entry))
                {
                    throw new InvalidOperationException(
                        $"Animation source Clip '{binding.AuthoringClip.name}' is declared more than once.");
                }
            }
            return result;
        }

        static CharacterAnimationSourceRig CreateSourceRig(
            CharacterAnimationPresentationProfile profile)
        {
            string sourcePath = AssetDatabase.GUIDToAssetPath(
                profile.FootPlacementAnalysisSourceAssetGuid);
            CharacterFootPlacementAnalysisSource source =
                AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(
                    sourcePath);
            if (!source)
                throw new InvalidOperationException(
                    "Animation build requires a persisted Foot Analysis Source.");
            source.RequireCalibrationAuthoringInput();
            string rigPath = AssetDatabase.GUIDToAssetPath(
                source.SamplingRigAssetGuid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            if (!prefab)
                throw new InvalidOperationException(
                    "Animation build requires a persisted sampling Rig Prefab.");
            CharacterAnimationRigPayload rig = new CharacterAnimationRigPayload(
                profile.RigDefinition ??
                throw new InvalidOperationException(
                    "Animation build requires a Rig Definition."));
            CharacterAnimationRigBinding binding =
                prefab.GetComponentInChildren<CharacterAnimationRigBinding>(true);
            if (!binding)
                throw new InvalidOperationException(
                    "Animation sampling Rig Prefab has no Animation Rig Binding.");
            return CharacterAnimationSourceRig.FromRuntimeRig(rig, binding);
        }
    }
}
