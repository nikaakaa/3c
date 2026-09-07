using System;
using ThirdPersonCharacter.Pipeline.Editor;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterPresentationPoseSourcePlanCompilationResult
    {
        public CharacterPresentationPoseSourcePlanCompilationResult(
            IReadOnlyList<CharacterAnimationBlendSpacePlan> blendSpaces,
            IReadOnlyDictionary<PresentationPoseSourceIndex, int> blendSpacePlanBySource,
            CharacterPresentationPoseSourcePlan[] poseSources,
            IReadOnlyList<string> diagnostics)
        {
            BlendSpaces = blendSpaces ?? Array.Empty<CharacterAnimationBlendSpacePlan>();
            BlendSpacePlanBySource = blendSpacePlanBySource ??
                new Dictionary<PresentationPoseSourceIndex, int>();
            PoseSources = poseSources ?? Array.Empty<CharacterPresentationPoseSourcePlan>();
            Diagnostics = diagnostics ?? Array.Empty<string>();
        }

        public IReadOnlyList<CharacterAnimationBlendSpacePlan> BlendSpaces { get; }
        public IReadOnlyDictionary<PresentationPoseSourceIndex, int> BlendSpacePlanBySource { get; }
        public CharacterPresentationPoseSourcePlan[] PoseSources { get; }
        public IReadOnlyList<string> Diagnostics { get; }
    }

    internal static class CharacterPresentationPoseSourcePlanCompiler
    {
        internal static CharacterPresentationPoseSourcePlanCompilationResult Compile(
            CharacterPresentationPoseSourceCompilationCatalog sourceCatalog,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationRigDefinition rig,
            CharacterFootPlacementAnalysisCompilation footAnalysisCompilation,
            CharacterAnimationBuildInput animationBuildInput)
        {
            var diagnostics = new List<string>();
            var blendSpaces = new List<CharacterAnimationBlendSpacePlan>();
            var blendSpaceIndices = new Dictionary<CharacterAnimationBlendSpaceAsset, int>();
            Dictionary<PresentationPoseSourceIndex, int> blendSpacePlanBySource =
                CompileBlendSpacePoseSources(
                    sourceCatalog,
                    profile,
                    rig,
                    footAnalysisCompilation,
                    animationBuildInput,
                    blendSpaces,
                    blendSpaceIndices,
                    diagnostics);
            CharacterPresentationPoseSourcePlan[] poseSources = CompilePoseSources(
                sourceCatalog,
                profile,
                rig,
                footAnalysisCompilation,
                animationBuildInput,
                diagnostics);
            return new CharacterPresentationPoseSourcePlanCompilationResult(
                blendSpaces,
                blendSpacePlanBySource,
                poseSources,
                diagnostics);
        }

        static Dictionary<PresentationPoseSourceIndex, int> CompileBlendSpacePoseSources(
            CharacterPresentationPoseSourceCompilationCatalog sourceCatalog,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationRigDefinition rig,
            CharacterFootPlacementAnalysisCompilation footAnalysisCompilation,
            CharacterAnimationBuildInput animationBuildInput,
            List<CharacterAnimationBlendSpacePlan> blendSpaces,
            Dictionary<CharacterAnimationBlendSpaceAsset, int> blendSpaceIndices,
            List<string> errors)
        {
            var result = new Dictionary<PresentationPoseSourceIndex, int>();
            AnimationFootAnalysisProjectionBuildData footAnalysis = footAnalysisCompilation?.BuildData;
            for (int bindingIndex = 0; bindingIndex < sourceCatalog.Entries.Count; bindingIndex++)
            {
                CharacterPresentationPoseSourceCompilationEntry entry =
                    sourceCatalog.Entries[bindingIndex];
                if (!(entry.Binding is CharacterBlendSpacePoseSourceBinding binding))
                    continue;
                try
                {
                    binding.RequireValid(rig);
                    CharacterAnimationBlendSpaceAsset blendSpace = binding.BlendSpace;
                    if (!blendSpaceIndices.TryGetValue(blendSpace, out int planIndex))
                    {
                        var samples = new CharacterAnimationBlendSpaceSamplePlan[blendSpace.Samples.Count];
                        for (int sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
                        {
                            CharacterAnimationBlendSpaceSample sample = blendSpace.Samples[sampleIndex];
                            AnimationFootFeaturePair features = default;
                            if (footAnalysis != null &&
                                (
                                 !footAnalysis.TryGetBlendSpace(
                                     blendSpace.BlendSpaceId,
                                     sample.SampleId,
                                     out features)))
                            {
                                throw new InvalidOperationException(
                                    $"Sample '{sample.SampleId}' has no generated Foot Analysis features.");
                            }
                            CharacterAnimationClipContentIdentity clipIdentity =
                                CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(sample.Clip);
                            CharacterAnimationSamplingBackendKind backend = CharacterAnimationSamplingBackendKind.NativeClip;
                            CharacterAnimationScalarCurvePage scalarPage = null;
                            CharacterAnimationSourceResourceBinding resourceBinding =
                                profile.FindSourceResourceBinding(sample.Clip);
                            if (resourceBinding != null)
                            {
                                resourceBinding.RequireValid();
                                backend = resourceBinding.Backend;
                                if (backend == CharacterAnimationSamplingBackendKind.NativeClip)
                                    scalarPage = animationBuildInput.AnimationCatalog.BuildNativeScalarPage(
                                        sample.Clip,
                                        errors);
                            }
                            else
                            {
                                scalarPage = animationBuildInput.AnimationCatalog.BuildNativeScalarPage(
                                    sample.Clip,
                                    errors);
                            }
                            CharacterAnimationBuildCatalogEntry catalogEntry =
                                animationBuildInput.AnimationCatalog.RegisterReference(
                                sample.Clip,
                                backend,
                                scalarPage,
                                $"blend-space:{blendSpace.BlendSpaceId}:{sample.SampleId}");
                            CharacterAnimationClipRegisteredCurveCatalog.ValidateFootMotionGroupRequired(sample.Clip);
                            AnimationCurve footWeight = CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                                sample.Clip,
                                CharacterAnimationClipRegisteredCurveChannels.FootPlacementWeight);
                            samples[sampleIndex] = new CharacterAnimationBlendSpaceSamplePlan(
                                sample,
                                $"{clipIdentity.AssetGuid}:{clipIdentity.LocalFileId}",
                                clipIdentity.FullDependencyHash,
                                clipIdentity.AnalysisInputHash,
                                clipIdentity.RegisteredCurveHash,
                                clipIdentity.SourceDurationSeconds,
                                CharacterPresentationFootEventCompiler.NormalizeRegisteredCurve(footWeight, clipIdentity.SourceDurationSeconds),
                                features,
                                backend,
                                scalarPage,
                                catalogEntry.ResourceCatalogIndex,
                                catalogEntry.GroupClipIndex);
                        }
                        var plan = new CharacterAnimationBlendSpacePlan(
                            blendSpace,
                            samples);
                        plan.RequireValid(footAnalysis != null);
                        planIndex = blendSpaces.Count;
                        blendSpaces.Add(plan);
                        blendSpaceIndices.Add(blendSpace, planIndex);
                    }
                    if (!result.TryAdd(entry.SourceIndex, planIndex))
                        throw new InvalidOperationException("source index is duplicated.");
                }
                catch (Exception exception)
                {
                    errors?.Add(
                        $"Blend Space Presentation Pose source binding #{bindingIndex} failed to compile: {exception.Message}");
                }
            }
            return result;
        }

        static CharacterPresentationPoseSourcePlan[] CompilePoseSources(
            CharacterPresentationPoseSourceCompilationCatalog sourceCatalog,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationRigDefinition rig,
            CharacterFootPlacementAnalysisCompilation footAnalysisCompilation,
            CharacterAnimationBuildInput animationBuildInput,
            List<string> errors)
        {
            AnimationFootAnalysisProjectionBuildData footAnalysis =
                footAnalysisCompilation?.BuildData;
            var result = new List<CharacterPresentationPoseSourcePlan>();
            var sourceIndices = new HashSet<PresentationPoseSourceIndex>();
            for (int i = 0; i < sourceCatalog.Entries.Count; i++)
            {
                CharacterPresentationPoseSourceCompilationEntry entry =
                    sourceCatalog.Entries[i];
                CharacterPresentationPoseSourceBinding binding = entry.Binding;
                try
                {
                    if (!binding || !sourceIndices.Add(entry.SourceIndex))
                        throw new InvalidOperationException("binding or source index is missing or duplicated.");
                    binding.RequireValid(rig);
                    if (binding is CharacterClipPoseSourceBinding directClip)
                    {
                        string bindingIdentity = CharacterPresentationAssetObjectIdentity.Require(directClip);
                        if (footAnalysis == null ||
                            !footAnalysis.TryGetPoseSource(bindingIdentity, out AnimationFootFeaturePair directFeatures))
                        {
                            throw new InvalidOperationException("Foot Analysis artifact binding is missing.");
                        }
                        AnimationFootAnalysisArtifact directArtifact =
                            footAnalysisCompilation.RequireArtifact(
                                AnimationFootAnalysisProjectionBuildData
                                    .PoseSourceBindingKey(bindingIdentity));
                        CharacterAnimationClipContentIdentity clipIdentity =
                            CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(directClip.Clip);
                        CharacterAnimationSourceResourceBinding resourceBinding =
                            profile.FindSourceResourceBinding(directClip.Clip);
                        CharacterAnimationSamplingBackendKind backend =
                            resourceBinding?.Backend ?? CharacterAnimationSamplingBackendKind.NativeClip;
                        resourceBinding?.RequireValid();
                        CharacterAnimationScalarCurvePage scalarPage = null;
                        if (backend == CharacterAnimationSamplingBackendKind.NativeClip)
                            scalarPage = animationBuildInput.AnimationCatalog.BuildNativeScalarPage(
                                directClip.Clip,
                                errors);
                        CharacterAnimationBuildCatalogEntry catalogEntry =
                            animationBuildInput.AnimationCatalog.RegisterReference(
                            directClip.Clip,
                            backend,
                            scalarPage,
                            $"pose-source:{bindingIdentity}:{clipIdentity.AssetGuid}");
                        CharacterAnimationClipRegisteredCurveCatalog.ValidateFootMotionGroupRequired(directClip.Clip);
                        AnimationCurve secondsCurve = CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            directClip.Clip,
                            CharacterAnimationClipRegisteredCurveChannels.FootPlacementWeight);
                        result.Add(new CharacterPresentationPoseSourcePlan(
                            entry.SourceIndex,
                            bindingIdentity,
                            directClip,
                            rig,
                            footAnalysis.Identity.AnalysisSourceId,
                            $"{clipIdentity.AssetGuid}:{clipIdentity.LocalFileId}",
                            clipIdentity.FullDependencyHash,
                            clipIdentity.AnalysisInputHash,
                            clipIdentity.RegisteredCurveHash,
                            clipIdentity.SourceDurationSeconds,
                            CharacterPresentationFootEventCompiler.NormalizeRegisteredCurve(secondsCurve, clipIdentity.SourceDurationSeconds),
                            CharacterPresentationFootEventCompiler.CompileFootStepObservation(
                                directClip.Clip,
                                clipIdentity.SourceDurationSeconds,
                                directArtifact.MotionData),
                            directFeatures,
                            backend,
                            scalarPage,
                            catalogEntry.ResourceCatalogIndex,
                            catalogEntry.GroupClipIndex));
                        continue;
                    }
                }
                catch (Exception exception)
                {
                    errors?.Add($"Presentation Pose source binding #{i} failed to compile: {exception.Message}");
                }
            }
            return result.ToArray();
        }

        internal static void ValidateClipPlayers(
            CharacterPoseProgramImage posePlan,
            IReadOnlyList<CharacterPresentationPoseSourcePlan> poseSources,
            CharacterAnimationRigDefinition rig,
            List<string> errors)
        {
            if (posePlan == null)
                return;
            var sourceByIndex = new Dictionary<PresentationPoseSourceIndex, CharacterPresentationPoseSourcePlan>();
            for (int i = 0; i < poseSources.Count; i++)
            {
                CharacterPresentationPoseSourcePlan source = poseSources[i];
                if (source != null)
                    sourceByIndex[source.SourceIndex] = source;
            }
            for (int i = 0; i < posePlan.ClipPlayers.Count; i++)
            {
                CharacterPresentationClipPlayerDescriptor descriptor = posePlan.ClipPlayers[i];
                try
                {
                    descriptor?.RequireValid();
                    if (descriptor == null ||
                        !sourceByIndex.TryGetValue(descriptor.PresentationPoseSourceIndex, out CharacterPresentationPoseSourcePlan source))
                    {
                        throw new InvalidOperationException("Presentation Pose source binding is missing.");
                    }
                    source.RequireValid();
                    if (!string.Equals(source.RigId, rig.RigId, StringComparison.Ordinal) ||
                        !string.Equals(source.RigRevision, rig.Revision, StringComparison.Ordinal) ||
                        descriptor.InitialTime > source.SourceDurationSeconds)
                    {
                        throw new InvalidOperationException("source Rig or initial time does not match the compiled binding.");
                    }
                }
                catch (Exception exception)
                {
                    errors?.Add($"Clip Player #{i} failed to compile: {exception.Message}");
                }
            }
        }

        internal static CharacterAnimationBlendSpacePlayerPlan[] CompileBlendSpacePlayers(
            CharacterPoseProgramImage posePlan,
            IReadOnlyList<CharacterAnimationBlendSpacePlan> blendSpaces,
            IReadOnlyDictionary<PresentationPoseSourceIndex, int> blendSpacePlanBySource,
            List<string> errors)
        {
            var result = new List<CharacterAnimationBlendSpacePlayerPlan>();
            for (int operationIndex = 0;
                 operationIndex < posePlan.OperationHeaders.Count;
                 operationIndex++)
            {
                CharacterPoseOperationHeader operation =
                    posePlan.OperationHeaders[operationIndex];
                if (operation.Code != CharacterPoseOperationCode.BlendSpacePlayer)
                    continue;
                CharacterPosePlayerOperationPayload player =
                    (CharacterPosePlayerOperationPayload)
                    posePlan.OperationPages.RequirePayload(operation);
                int parameterIndex = posePlan.OperationPages.FindInputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.Parameter);
                int parameterIndexB = posePlan.OperationPages.FindInputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.Parameter,
                    1);
                if (!player.SourceIndex.IsValid ||
                    parameterIndex < 0 || parameterIndex >= posePlan.Parameters.Count)
                {
                    errors?.Add($"Blend Space Player '{operation.NodeId}' has incomplete compiled inputs.");
                    continue;
                }
                if (!blendSpacePlanBySource.TryGetValue(
                        player.SourceIndex,
                        out int planIndex) ||
                    planIndex < 0 || planIndex >= blendSpaces.Count)
                {
                    errors?.Add(
                        $"Blend Space Player '{operation.NodeId}' has no exact Presentation Pose source plan.");
                    continue;
                }
                CharacterAnimationBlendSpacePlan reference = blendSpaces[planIndex];
                if (!CharacterPresentationProgramParameterFrame.Supports(reference.XAxis.ParameterId) ||
                    reference.AxisCount == 2 &&
                    !CharacterPresentationProgramParameterFrame.Supports(reference.YAxis.ParameterId))
                {
                    errors?.Add(
                        $"Blend Space Player '{operation.NodeId}' references an axis ParameterId without a formal Presentation Program Parameter provider.");
                    continue;
                }
                bool consistent = true;
                if (reference.Mode == CharacterAnimationBlendSpaceMode.Linear1D &&
                    player.InputRangePolicy != CharacterAnimationBlendSpaceInputRangePolicy.Clamp)
                    consistent = false;
                CharacterPresentationPoseParameterEntry xParameter =
                    posePlan.Parameters[parameterIndex];
                consistent &= SameParameterContract(xParameter, reference.XAxis);
                if (reference.AxisCount == 1)
                    consistent &= parameterIndexB == -1;
                else if (parameterIndexB < 0 || parameterIndexB >= posePlan.Parameters.Count)
                    consistent = false;
                else
                    consistent &= SameParameterContract(posePlan.Parameters[parameterIndexB], reference.YAxis);
                for (int otherParameterIndex = 0;
                     otherParameterIndex < posePlan.Parameters.Count;
                     otherParameterIndex++)
                {
                    if (otherParameterIndex == parameterIndex ||
                        otherParameterIndex == parameterIndexB)
                        continue;
                    PoseParameterId parameterId =
                        posePlan.Parameters[otherParameterIndex].ParameterId;
                    if (!reference.TryGetParameterPolicy(parameterId, out CharacterAnimationBlendSpaceParameterPolicy policy))
                    {
                        consistent = false;
                        break;
                    }
                }
                if (!consistent)
                {
                    errors?.Add($"Blend Space Player '{operation.NodeId}' producer assets and typed axis inputs do not share one ParameterId/type/unit contract.");
                    continue;
                }
                result.Add(new CharacterAnimationBlendSpacePlayerPlan(
                    operation.NodeId,
                    player.SourceIndex,
                    operation.Index,
                    player.PlayerIndex,
                    parameterIndex,
                    parameterIndexB,
                    player.InputRangePolicy,
                    planIndex));
            }
            return result.ToArray();
        }

        static bool SameAxisContract(
            CharacterAnimationBlendSpacePlan left,
            CharacterAnimationBlendSpacePlan right)
        {
            if (left == null || right == null || left.AxisCount != right.AxisCount || left.Mode != right.Mode ||
                !SameAxis(left.XAxis, right.XAxis))
                return false;
            return left.AxisCount == 1 || SameAxis(left.YAxis, right.YAxis);
        }

        static bool SameAxis(CharacterAnimationBlendSpaceAxisPlan left, CharacterAnimationBlendSpaceAxisPlan right) =>
            left != null && right != null && left.ParameterId.Equals(right.ParameterId) &&
            left.ValueType == right.ValueType && string.Equals(left.Unit, right.Unit, StringComparison.Ordinal) &&
            left.Minimum.Equals(right.Minimum) && left.Maximum.Equals(right.Maximum);

        static bool SameParameterContract(
            CharacterPresentationPoseParameterEntry parameter,
            CharacterAnimationBlendSpaceAxisPlan axis) =>
            parameter != null && axis != null && parameter.ParameterId.Equals(axis.ParameterId) &&
            parameter.ValueType == axis.ValueType && string.Equals(parameter.Unit, axis.Unit, StringComparison.Ordinal);
    }
}
