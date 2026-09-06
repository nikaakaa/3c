using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Editor.MotionMatching;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterPresentationAssetObjectIdentity
    {
        public static string Require(UnityEngine.Object value)
        {
            if (!value ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    value,
                    out string guid,
                    out long localFileId) ||
                string.IsNullOrWhiteSpace(guid) ||
                localFileId == 0)
            {
                throw new InvalidOperationException(
                    $"Asset object '{value?.name ?? "Missing"}' has no stable GUID/local file id.");
            }
            return string.Concat(guid, ":", localFileId.ToString("D20"));
        }
    }

    internal sealed class CharacterPresentationProjectionCompileRequest
    {
        public CharacterPresentationProjectionCompileRequest(
            ValidatedSemanticIrArtifact artifact,
            CharacterAuthoringCompilationModel model,
            CharacterFootPlacementAnalysisCompilation footAnalysis)
        {
            Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
            Model = model ?? throw new ArgumentNullException(nameof(model));
            FootAnalysis = footAnalysis ?? throw new ArgumentNullException(nameof(footAnalysis));
        }

        public ValidatedSemanticIrArtifact Artifact { get; }
        public CharacterAuthoringCompilationModel Model { get; }
        public CharacterFootPlacementAnalysisCompilation FootAnalysis { get; }
    }

    internal sealed class CharacterPresentationProjectionDiagnostic
    {
        public CharacterPresentationProjectionDiagnostic(string code, string identity, string message)
        {
            Code = code ?? string.Empty;
            Identity = identity ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string Code { get; }
        public string Identity { get; }
        public string Message { get; }
    }

    internal sealed class CharacterPresentationProjectionCompileResult
    {
        public CharacterPresentationProjectionCompileResult(
            CharacterPresentationProjection projection,
            CharacterPresentationSemanticContract contract,
            string projectionRevision,
            IReadOnlyList<CharacterPresentationProjectionDiagnostic> diagnostics)
        {
            Projection = projection;
            Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            ProjectionRevision = string.IsNullOrWhiteSpace(projectionRevision)
                ? throw new ArgumentException("Projection revision is required.", nameof(projectionRevision))
                : projectionRevision;
            Diagnostics = diagnostics ?? Array.Empty<CharacterPresentationProjectionDiagnostic>();
        }

        public CharacterPresentationProjection Projection { get; }
        public CharacterPresentationSemanticContract Contract { get; }
        public string ProjectionRevision { get; }
        public IReadOnlyList<CharacterPresentationProjectionDiagnostic> Diagnostics { get; }
        public bool IsValid => Projection != null && Projection.IsValid && Diagnostics.Count == 0;
    }

    internal static class CharacterPresentationProjectionCompiler
    {
        public static CharacterPresentationProjectionCompileResult Compile(
            CharacterPresentationProjectionCompileRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var errors = new List<string>();
            CharacterAuthoringCompilationModel model = request.Model;
            var reader = new CharacterPresentationSemanticReader(request.Artifact);
            CharacterPresentationPoseSourceCompilationResult sourceCompilation =
                CharacterPresentationPoseSourceCompiler.Compile(
                    model.AnimationPresentationProfile);
            errors.AddRange(sourceCompilation.Diagnostics);
            CharacterPresentationPoseSourceCompilationCatalog sourceCatalog =
                sourceCompilation.Catalog;
            CharacterPresentationMotionMatchingCompilationResult motionMatchingCompilation =
                CharacterPresentationMotionMatchingCompiler.Compile(
                    model.AnimationPresentationProfile);
            errors.AddRange(motionMatchingCompilation.Diagnostics);
            MotionMatchingProjectionPayload motionMatching =
                motionMatchingCompilation.Payload;
            string projectionRevision = CharacterPresentationProjectionRevisionCompiler.Compute(
                model.AnimationPresentationProfile,
                model.Definition.EquipmentPresentationProfile,
                reader.Contract.ContractHash,
                request.FootAnalysis.RevisionTokens,
                motionMatching);
            CharacterPresentationProjection projection = CompileCore(
                reader,
                model.AnimationPresentationProfile,
                model.Definition.EquipmentProfile,
                model.Definition.EquipmentPresentationProfile,
                projectionRevision,
                request.FootAnalysis,
                motionMatching,
                sourceCatalog,
                model.Timelines,
                CharacterPresentationProducerCompiler.CollectTimelineCallSites(model.Root),
                errors);
            var diagnostics = new CharacterPresentationProjectionDiagnostic[errors.Count];
            for (int i = 0; i < errors.Count; i++)
            {
                string message = errors[i] ?? string.Empty;
                string code = "presentation_projection_invalid";
                if (message.StartsWith("[animation_phase_quality_", StringComparison.Ordinal))
                {
                    int end = message.IndexOf(']');
                    if (end > 1)
                    {
                        code = message.Substring(1, end - 1);
                        message = message.Substring(end + 1).TrimStart();
                    }
                }
                diagnostics[i] = new CharacterPresentationProjectionDiagnostic(
                    code,
                    request.Artifact.Header.ProgramId.Value,
                    message);
            }
            return new CharacterPresentationProjectionCompileResult(
                projection,
                reader.Contract,
                projectionRevision,
                diagnostics);
        }

        public static bool TryComputePublishedRevision(
            CharacterPipelineDefinition definition,
            CharacterPresentationSemanticContract contract,
            CharacterPresentationProjection projection,
            out string revision)
        {
            revision = string.Empty;
            if (!definition || contract == null || projection == null || !projection.IsValid)
                return false;
            var errors = new List<string>();
            if (!CharacterProjectionFootAnalysisResolver.TryBuildPublishedRevisionTokens(
                    definition.AnimationPresentationProfile,
                    projection,
                    errors,
                    out string[] footAnalysisTokens) ||
                errors.Count > 0)
                return false;
            revision = CharacterPresentationProjectionRevisionCompiler.Compute(
                definition.AnimationPresentationProfile,
                definition.EquipmentPresentationProfile,
                contract.ContractHash,
                footAnalysisTokens,
                projection.MotionMatching);
            return true;
        }

       static CharacterPresentationProjection CompileCore(
            CharacterPresentationSemanticReader reader,
            CharacterAnimationPresentationProfile profile,
            CharacterEquipmentProfile equipmentProfile,
            CharacterEquipmentPresentationProfile equipmentPresentationProfile,
            string projectionRevision,
            CharacterFootPlacementAnalysisCompilation footAnalysisCompilation,
            MotionMatchingProjectionPayload motionMatching,
            CharacterPresentationPoseSourceCompilationCatalog sourceCatalog,
            IReadOnlyDictionary<string, TimelineData> timelines,
            IReadOnlyDictionary<string, IReadOnlyList<CharacterPresentationTimelineCallSite>> timelineCallSites,
            List<string> errors)
        {
            if (reader == null || profile == null || sourceCatalog == null || timelines == null || timelineCallSites == null)
            {
                errors?.Add("Character Presentation Projection build input is incomplete.");
                return null;
            }

            profile.CollectConfigurationErrors(errors);
            AnimationFootAnalysisProjectionBuildData footAnalysis =
                footAnalysisCompilation?.BuildData;
            AnimationFootAnalysisProjectionIdentity footIdentity = default;
            if (profile.FootPlacementAnalysisMode == CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures)
            {
                if (footAnalysis == null)
                {
                    errors?.Add("Character Presentation Projection requires generated Foot Analysis build data.");
                    return null;
                }
                footAnalysis.Identity.RequireValid();
                footIdentity = footAnalysis.Identity;
            }
            else if (footAnalysis != null)
            {
                errors?.Add("Disabled Foot Analysis cannot receive generated build data.");
                return null;
            }
            var entries = new List<CharacterPresentationProducerEntry>();
            var blendSpaces = new List<CharacterAnimationBlendSpacePlan>();
            var blendSpaceIndices = new Dictionary<CharacterAnimationBlendSpaceAsset, int>();
            var animationIds = new HashSet<AnimationProducerId>();
            for (int i = 0; i < reader.Producers.Count; i++)
            {
                CharacterPresentationProducerCompilationResult producerCompilation =
                    CharacterPresentationProducerCompiler.Compile(
                    reader,
                    reader.Producers[i],
                    profile,
                    footAnalysisCompilation,
                    timelines,
                    timelineCallSites);
                errors.AddRange(producerCompilation.Diagnostics);
                CharacterPresentationProducerEntry entry = producerCompilation.Entry;
                if (entry == null)
                    continue;
                entries.Add(entry);
                if (entry.Kind == CharacterPresentationProducerKind.Animation)
                    animationIds.Add(entry.ProducerId);
            }
            for (int i = 0; i < profile.ProducerBindings.Count; i++)
            {
                AnimationProducerPresentationBinding binding = profile.ProducerBindings[i];
                if (binding != null && binding.ProducerId.IsValid && !animationIds.Contains(binding.ProducerId))
                    errors?.Add($"Animation producer binding '{binding.ProducerId}' is orphaned from the Semantic IR.");
            }
            entries.Sort((left, right) => left.ProgramProducerIndex.CompareTo(right.ProgramProducerIndex));
            AnimationChannelId[] animationChannels = entries
                .Where(value => value.Kind == CharacterPresentationProducerKind.Animation)
                .Select(value => value.AnimationChannelId)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            CharacterPresentationAnimationBlendCompiler.Compilation blendCatalogs =
                CharacterPresentationAnimationBlendCompiler.CompileCatalog(
                    profile.PoseGraph,
                    profile.RigDefinition,
                    errors);
            AnimationBlendNodePayload[] blendNodes = blendCatalogs == null
                ? Array.Empty<AnimationBlendNodePayload>()
                : CharacterPresentationAnimationBlendCompiler.CompileNodes(
                    profile.PoseGraph,
                    profile.RigDefinition,
                    entries,
                    blendCatalogs,
                    errors);
            Dictionary<PresentationPoseSourceIndex, int> blendSpacePlanBySource =
                CompileBlendSpacePoseSources(
                    sourceCatalog,
                    profile.RigDefinition,
                    footAnalysisCompilation,
                    blendSpaces,
                    blendSpaceIndices,
                    errors);
            CharacterPresentationPoseSourcePlan[] poseSources = CompilePoseSources(
                sourceCatalog,
                profile.RigDefinition,
                footAnalysisCompilation,
                errors);
            AnimationClipPhasePlan[] clipPhasePlans = Array.Empty<AnimationClipPhasePlan>();
            AnimationSourcePhasePlan[] sourcePhasePlans = Array.Empty<AnimationSourcePhasePlan>();
            AnimationFootPhaseValidationDescriptor[] clipPhaseValidations =
                Array.Empty<AnimationFootPhaseValidationDescriptor>();
            try
            {
                AnimationPhasePlanCompiler.CompileSources(
                    profile,
                    poseSources,
                    blendSpaces,
                    blendSpacePlanBySource,
                    footAnalysisCompilation,
                    out clipPhasePlans,
                    out sourcePhasePlans,
                    out clipPhaseValidations);
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
            }
            CharacterLinkedPoseProjectionPayload linkedPose =
                CharacterLinkedPoseProjectionCompiler.Compile(
                    profile,
                    equipmentProfile,
                    errors);
            var poseRequest = new CharacterPoseCompilationRequest(
                new CharacterPoseCanvasAuthoringView(profile.PoseGraph),
                profile.RigDefinition,
                animationChannels,
                blendNodes,
                poseSources,
                clipPhasePlans,
                sourcePhasePlans,
                clipPhaseValidations,
                sourceCatalog.SourceIndices,
                blendCatalogs?.CurveIndices,
                blendCatalogs?.ProfileIndicesByIdentity,
                profile,
                linkedPose,
                motionMatching,
                footAnalysisCompilation);
            CharacterPoseCompilationResult poseCompilation =
                CharacterPoseCompilerModule.Compile(poseRequest);
            poseCompilation.CopyMessagesTo(errors);
            CharacterPoseProgramImage poseProgram = poseCompilation.ProgramImage;
            CharacterAnimationBlendSpacePlayerPlan[] blendSpacePlayers = poseProgram == null
                ? Array.Empty<CharacterAnimationBlendSpacePlayerPlan>()
                : CompileBlendSpacePlayers(
                    poseProgram,
                    blendSpaces,
                    blendSpacePlanBySource,
                    errors);
            CharacterAnimationRigPayload rig = poseProgram == null
                ? null
                : new CharacterAnimationRigPayload(profile.RigDefinition);
            ValidateClipPlayers(poseProgram, poseSources, profile.RigDefinition, errors);
            CharacterPresentationEquipmentCompilationResult equipmentCompilation =
                CharacterPresentationEquipmentCompiler.Compile(
                    equipmentProfile,
                    equipmentPresentationProfile);
            errors.AddRange(equipmentCompilation.Diagnostics);
            EquipmentVisualProjectionBinding[] visualBindings =
                equipmentCompilation.VisualBindings.ToArray();
            if (errors.Count > 0)
                return null;

            CharacterPresentationProjection projection = CharacterPresentationProjection.Create(
                reader.Contract,
                poseProgram,
                blendCatalogs.CurveCatalog,
                blendCatalogs.ProfileCatalog,
                rig,
                motionMatching,
                poseSources,
                blendSpaces.ToArray(),
                blendSpacePlayers,
                clipPhasePlans,
                sourcePhasePlans,
                entries.ToArray(),
                footIdentity,
                projectionRevision,
                visualBindings,
                linkedPose);
            CharacterPoseTuningCompilationResult tuning =
                CharacterPoseTuningLayoutCompiler.Compile(
                    reader.Contract.ProgramId.Value,
                    projection);
            projection.SetTuningPayload(
                tuning.Layout,
                tuning.DefaultBlock,
                tuning.PublishedParameterRevision);
            return projection;
        }

        static Dictionary<PresentationPoseSourceIndex, int> CompileBlendSpacePoseSources(
            CharacterPresentationPoseSourceCompilationCatalog sourceCatalog,
            CharacterAnimationRigDefinition rig,
            CharacterFootPlacementAnalysisCompilation footAnalysisCompilation,
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
                                features);
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
            CharacterAnimationRigDefinition rig,
            CharacterFootPlacementAnalysisCompilation footAnalysisCompilation,
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
                            directFeatures));
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

       static void ValidateClipPlayers(
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

        static CharacterAnimationBlendSpacePlayerPlan[] CompileBlendSpacePlayers(
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
