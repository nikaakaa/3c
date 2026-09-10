using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Editor.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation
{
    internal static class CharacterMotionMatchingResourceProjectionCompiler
    {
        internal static MotionMatchingProjectionPayload Compile(
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationBuildInput animationBuildInput,
            CharacterPresentationPoseResourceCompilationCatalog resources,
            List<string> errors)
        {
            if (!profile || animationBuildInput == null || resources == null)
                return null;
            CharacterPoseResourceSlot[] bindingSlots = profile.PoseGraph.EnumerateGraphs()
                .SelectMany(value => value.Nodes)
                .Select(value => (value?.Payload as CharacterMotionMatchingPosePayload)?.BindingSlot)
                .Where(value => value)
                .Distinct()
                .ToArray();
            if (bindingSlots.Length == 0)
                return null;
            CharacterMotionMatchingBinding[] bindings;
            try
            {
                bindings = bindingSlots
                    .Select(value => resources.MotionMatchingBinding(value, "Motion Matching Pose"))
                    .Distinct()
                    .ToArray();
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
                return null;
            }
            CharacterMotionMatchingProfile[] profiles = bindings
                .Select(value => value.Profile)
                .Where(value => value)
                .Distinct()
                .ToArray();
            if (profiles.Length != 1)
            {
                errors?.Add("Motion Matching Pose nodes must resolve one exact Motion Matching Profile.");
                return null;
            }
            if (profile.FootPlacementAnalysisMode != CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures ||
                !CharacterFootPlacementAnalysisSource.IsAssetGuid(profile.FootPlacementAnalysisSourceAssetGuid))
            {
                errors?.Add("Motion Matching Projection requires the Presentation Profile generated Foot Analysis Source.");
                return null;
            }
            string path = AssetDatabase.GUIDToAssetPath(profile.FootPlacementAnalysisSourceAssetGuid);
            CharacterFootPlacementAnalysisSource analysisSource =
                AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(path);
            if (!analysisSource)
            {
                errors?.Add("Motion Matching Projection Foot Analysis Source is missing.");
                return null;
            }
            try
            {
                MotionMatchingProjectionPayload compiled = MotionMatchingProjectionPayloadCompiler.Compile(
                    profiles[0],
                    profile.PoseGraph,
                    profile.RigDefinition,
                    analysisSource,
                    AnimationClipMotionMatchingParameterCurveResolver.Instance,
                    resources);
                var databases = new MotionMatchingDatabasePayload[compiled.DatabaseCount];
                for (int databaseIndex = 0; databaseIndex < databases.Length; databaseIndex++)
                {
                    MotionMatchingDatabasePayload database = compiled.GetDatabase(databaseIndex);
                    var compiledClipBindings = new MotionMatchingClipBindingPayload[database.ClipBindingCount];
                    for (int clipIndex = 0; clipIndex < compiledClipBindings.Length; clipIndex++)
                    {
                        MotionMatchingClipBindingPayload binding = database.GetClipBinding(clipIndex);
                        CharacterAnimationSourceResourceBinding resourceBinding =
                            animationBuildInput.SourceResourceBindings.SingleOrDefault(
                                value => value?.AuthoringClip == binding.Clip);
                        CharacterAnimationSamplingBackendKind backend =
                            resourceBinding?.Backend ?? CharacterAnimationSamplingBackendKind.NativeClip;
                        resourceBinding?.RequireValid();
                        if (backend != CharacterAnimationSamplingBackendKind.NativeClip &&
                            backend != CharacterAnimationSamplingBackendKind.Acl)
                            throw new InvalidOperationException("Motion Matching Clip binding backend is unsupported.");
                        CharacterAnimationScalarCurvePage scalarPage =
                            backend == CharacterAnimationSamplingBackendKind.NativeClip
                                ? animationBuildInput.AnimationCatalog.BuildNativeScalarPage(binding.Clip, errors)
                                : null;
                        CharacterAnimationBuildCatalogEntry catalogEntry =
                            animationBuildInput.AnimationCatalog.RegisterReference(
                                binding.Clip,
                                backend,
                                scalarPage,
                                $"motion-matching:{databaseIndex}:{clipIndex}:{binding.SourceClipId}");
                        compiledClipBindings[clipIndex] =
                            backend == CharacterAnimationSamplingBackendKind.NativeClip
                                ? MotionMatchingClipBindingPayload.CreateNative(
                                    binding.SourceClipId,
                                    binding.AssetGuid,
                                    binding.LocalFileId,
                                    binding.Clip,
                                    binding.RootLocked,
                                    binding.FootPlacementWeightCurve,
                                    scalarPage)
                                : MotionMatchingClipBindingPayload.CreateAcl(
                                    binding.SourceClipId,
                                    binding.AssetGuid,
                                    binding.LocalFileId,
                                    binding.RootLocked,
                                    binding.FootPlacementWeightCurve,
                                    binding.DurationSeconds,
                                    binding.IsLooping,
                                    catalogEntry.ResourceCatalogIndex,
                                    catalogEntry.GroupClipIndex);
                    }
                    databases[databaseIndex] = database.WithClipBindings(compiledClipBindings);
                }
                return compiled.WithDatabases(databases);
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
                return null;
            }
        }
    }
}
