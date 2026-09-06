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
        readonly struct AnimationTimelineCallSite
        {
            public AnimationTimelineCallSite(string identity, TimelinePlaybackMode playbackMode)
            {
                Identity = identity ?? string.Empty;
                PlaybackMode = playbackMode;
            }

            public string Identity { get; }
            public TimelinePlaybackMode PlaybackMode { get; }
        }

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
            MotionMatchingProjectionPayload motionMatching = CompileMotionMatchingPayload(
                model.AnimationPresentationProfile,
                sourceCatalog,
                errors);
            string projectionRevision = ComputeProjectionRevision(
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
                CollectTimelineCallSites(model.Root),
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
            revision = ComputeProjectionRevision(
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
            IReadOnlyDictionary<string, IReadOnlyList<AnimationTimelineCallSite>> timelineCallSites,
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
                CharacterPresentationProducerEntry entry = BuildProducer(
                    reader,
                    reader.Producers[i],
                    profile,
                    footAnalysisCompilation,
                    timelines,
                    timelineCallSites,
                    errors);
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
            CompileEquipmentProjection(
                equipmentProfile,
                equipmentPresentationProfile,
                errors,
                out EquipmentVisualProjectionBinding[] visualBindings);
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
                                NormalizeRegisteredCurve(footWeight, clipIdentity.SourceDurationSeconds),
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
                            NormalizeRegisteredCurve(secondsCurve, clipIdentity.SourceDurationSeconds),
                            CompileFootStepObservation(
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

        static AnimationCurve NormalizeRegisteredCurve(AnimationCurve source, float sourceDurationSeconds)
        {
            Keyframe[] keys = source.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                key.time /= sourceDurationSeconds;
                key.inTangent *= sourceDurationSeconds;
                key.outTangent *= sourceDurationSeconds;
                keys[i] = key;
            }
            return new AnimationCurve(keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        static AnimationFootStepObservationCurvePair CompileFootStepObservation(
            UnityEngine.AnimationClip clip,
            float sourceDurationSeconds,
            AnimationFootMotionDataDescriptor motionData) =>
            new AnimationFootStepObservationCurvePair(
                new AnimationFootStepObservationCurveSet(
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftFootHeight),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftToeHeight),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftToeSpeed),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftPositionError),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftRotationError),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftContact),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftLockMode),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftLockWeight),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftSupport),
                        sourceDurationSeconds),
                    CompileLandingEvents(
                        motionData,
                        motionData?.Left,
                        motionData?.Raw.Left,
                        clip.isLooping,
                        sourceDurationSeconds,
                        $"{clip.name}/Left")),
                new AnimationFootStepObservationCurveSet(
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightFootHeight),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightToeHeight),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightToeSpeed),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightPositionError),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightRotationError),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightContact),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightLockMode),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightLockWeight),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightSupport),
                        sourceDurationSeconds),
                    CompileLandingEvents(
                        motionData,
                        motionData?.Right,
                        motionData?.Raw.Right,
                        clip.isLooping,
                        sourceDurationSeconds,
                        $"{clip.name}/Right")));

        static AnimationFootStepLandingEventTable CompileLandingEvents(
            AnimationFootMotionDataDescriptor motionData,
            AnimationFootMotionFootPage foot,
            AnimationFootMotionRawFootPage rawFoot,
            bool looping,
            float sourceDurationSeconds,
            string sourceLabel)
        {
            if (motionData == null || foot == null || rawFoot == null ||
                !float.IsFinite(sourceDurationSeconds) ||
                sourceDurationSeconds <= 0f ||
                rawFoot.Samples.Count != foot.Samples.Count ||
                Mathf.Abs(
                    motionData.Raw.DurationSeconds -
                    sourceDurationSeconds) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Foot Step Landing Event source timing is invalid.");
            }
            var result = new List<AnimationFootStepLandingEvent>();
            int landingOrdinal = 0;
            for (int i = 0; i < foot.Events.Count; i++)
            {
                AnimationFootMotionEvent footEvent = foot.Events[i];
                if (footEvent.Kind != AnimationFootMotionEventKind.Landing)
                    continue;
                if ((uint)footEvent.SampleIndex >=
                    (uint)motionData.Raw.RootSamples.Count ||
                    (uint)footEvent.SampleIndex >=
                    (uint)foot.Samples.Count)
                {
                    throw new InvalidOperationException(
                        "Foot Step Landing Event sample is outside its artifact.");
                }
                float normalizedTime =
                    motionData.Raw.RootSamples[footEvent.SampleIndex].TimeSeconds /
                    sourceDurationSeconds;
                AnimationFootMotionStepEvidence step =
                    foot.Samples[footEvent.SampleIndex].Step;
                if (!step.Available ||
                    step.LandingOrdinal != footEvent.Ordinal ||
                    footEvent.Ordinal != ++landingOrdinal)
                {
                    throw new InvalidOperationException(
                        "Foot Step Landing Event has no matching Step evidence.");
                }
                RequireLandingStepDistanceConsistency(
                    motionData.Raw,
                    rawFoot,
                    foot,
                    i,
                    in footEvent,
                    in step,
                    looping,
                    sourceLabel);
                bool hasSwingBoundaries = ResolveLandingEventPhaseLeads(
                    motionData,
                    foot,
                    in footEvent,
                    looping,
                    sourceDurationSeconds,
                    sourceLabel,
                    out float preSwingLeadSeconds,
                    out float swingLeadSeconds,
                    out float approachContactLeadSeconds);
                result.Add(new AnimationFootStepLandingEvent(
                    normalizedTime,
                    footEvent.Ordinal,
                    footEvent.CycleOffset,
                    step.Distance,
                    footEvent.RootLocalSolePosition,
                    hasSwingBoundaries,
                    preSwingLeadSeconds,
                    swingLeadSeconds,
                    approachContactLeadSeconds));
            }
            return new AnimationFootStepLandingEventTable(result.ToArray());
        }

        static void RequireLandingStepDistanceConsistency(
            AnimationFootMotionRawPage raw,
            AnimationFootMotionRawFootPage rawFoot,
            AnimationFootMotionFootPage foot,
            int eventIndex,
            in AnimationFootMotionEvent landing,
            in AnimationFootMotionStepEvidence step,
            bool looping,
            string sourceLabel)
        {
            const float geometryToleranceMeters = 0.0001f;
            const float timeToleranceSeconds = 0.0001f;
            Vector3 currentPosition =
                rawFoot.Samples[landing.SampleIndex].Sole.MotionPosition;
            if (Vector3.Distance(currentPosition, landing.MotionSolePosition) >
                    geometryToleranceMeters ||
                step.TimeSeconds > timeToleranceSeconds)
            {
                throw new InvalidOperationException(
                    $"Foot Motion '{sourceLabel}' Landing #{landing.Ordinal} " +
                    "does not match its canonical motion sample or time boundary.");
            }
            int previousEventIndex = -1;
            for (int i = eventIndex - 1; i >= 0; i--)
            {
                if (foot.Events[i].Kind != AnimationFootMotionEventKind.Landing)
                    continue;
                previousEventIndex = i;
                break;
            }
            bool previousCycle = previousEventIndex < 0 && looping;
            if (previousCycle)
            {
                for (int i = foot.Events.Count - 1; i >= 0; i--)
                {
                    if (foot.Events[i].Kind != AnimationFootMotionEventKind.Landing)
                        continue;
                    previousEventIndex = i;
                    break;
                }
            }
            Vector3 previousPosition = previousEventIndex >= 0
                ? foot.Events[previousEventIndex].MotionSolePosition
                : rawFoot.Samples[0].Sole.MotionPosition;
            if (previousCycle)
            {
                AnimationFootMotionRootSample first = raw.RootSamples[0];
                AnimationFootMotionRootSample last =
                    raw.RootSamples[raw.RootSamples.Count - 1];
                Quaternion cycleRotation =
                    (last.Rotation * Quaternion.Inverse(first.Rotation)).normalized;
                Vector3 cycleTranslation =
                    last.Position - cycleRotation * first.Position;
                previousPosition = Quaternion.Inverse(cycleRotation) *
                                   (previousPosition - cycleTranslation);
            }
            float expectedDistance = Vector3.ProjectOnPlane(
                currentPosition - previousPosition,
                Vector3.up).magnitude;
            if (!float.IsFinite(expectedDistance) ||
                Mathf.Abs(step.Distance - expectedDistance) >
                    geometryToleranceMeters)
            {
                throw new InvalidOperationException(
                    $"Foot Motion '{sourceLabel}' Landing #{landing.Ordinal} " +
                    $"Step Distance mismatch: expected={expectedDistance:R}, " +
                    $"actual={step.Distance:R}.");
            }
        }

        static bool ResolveLandingEventPhaseLeads(
            AnimationFootMotionDataDescriptor motionData,
            AnimationFootMotionFootPage foot,
            in AnimationFootMotionEvent footEvent,
            bool looping,
            float sourceDurationSeconds,
            string sourceLabel,
            out float preSwingLeadSeconds,
            out float swingLeadSeconds,
            out float approachContactLeadSeconds)
        {
            int sampleCount = motionData.Raw.RootSamples.Count;
            int activeSampleCount = looping ? sampleCount - 1 : sampleCount;
            int landingSample = footEvent.SampleIndex;
            if (activeSampleCount <= 0 ||
                landingSample < 0 ||
                landingSample >= activeSampleCount)
            {
                throw new InvalidOperationException(
                    $"Foot Step Landing Event #{footEvent.Ordinal} sample is invalid.");
            }
            int previousLandingSample = FindPreviousEventSample(
                foot,
                AnimationFootMotionEventKind.Landing,
                landingSample,
                activeSampleCount,
                looping);
            int liftOffSample = FindPreviousEventSample(
                foot,
                AnimationFootMotionEventKind.LiftOff,
                landingSample,
                activeSampleCount,
                looping);
            if (liftOffSample < 0)
            {
                if (!looping && landingSample == 0)
                {
                    preSwingLeadSeconds = 0f;
                    swingLeadSeconds = 0f;
                    approachContactLeadSeconds = 0f;
                    return false;
                }
                const float contactEpsilon = 0.0001f;
                if (!looping &&
                    foot.Samples[0].Filter.Contact <= contactEpsilon)
                {
                    liftOffSample = 0;
                }
                else if (foot.Samples[0].Filter.Contact > contactEpsilon)
                {
                    preSwingLeadSeconds = 0f;
                    swingLeadSeconds = 0f;
                    approachContactLeadSeconds = 0f;
                    return false;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Foot Step Landing Event {sourceLabel} #{footEvent.Ordinal} has no preceding LiftOff boundary. LandingSample={landingSample}, InitialContact={foot.Samples[0].Filter.Contact}.");
                }
            }
            preSwingLeadSeconds = previousLandingSample >= 0
                ? SecondsBetweenSamples(
                    motionData,
                    previousLandingSample,
                    landingSample,
                    looping,
                    sourceDurationSeconds)
                : motionData.Raw.RootSamples[landingSample].TimeSeconds;
            swingLeadSeconds = SecondsBetweenSamples(
                motionData,
                liftOffSample,
                landingSample,
                looping,
                sourceDurationSeconds);
            int approachContactSample = FindApproachContactSample(
                foot,
                liftOffSample,
                landingSample,
                activeSampleCount,
                looping);
            if (approachContactSample < 0)
            {
                throw new InvalidOperationException(
                    $"Foot Step Landing Event {sourceLabel} #{footEvent.Ordinal} has no Approach Contact boundary. LiftOffSample={liftOffSample}, LandingSample={landingSample}.");
            }
            approachContactLeadSeconds = SecondsBetweenSamples(
                motionData,
                approachContactSample,
                landingSample,
                looping,
                sourceDurationSeconds);
            if (!float.IsFinite(preSwingLeadSeconds) ||
                !float.IsFinite(swingLeadSeconds) ||
                !float.IsFinite(approachContactLeadSeconds) ||
                preSwingLeadSeconds < 0f ||
                swingLeadSeconds < 0f ||
                swingLeadSeconds > preSwingLeadSeconds ||
                approachContactLeadSeconds < 0f ||
                approachContactLeadSeconds > swingLeadSeconds)
            {
                throw new InvalidOperationException(
                    $"Foot Step Landing Event #{footEvent.Ordinal} phase boundaries are invalid.");
            }
            return true;
        }

        static int FindPreviousEventSample(
            AnimationFootMotionFootPage foot,
            AnimationFootMotionEventKind kind,
            int targetSample,
            int activeSampleCount,
            bool looping)
        {
            int selectedSample = -1;
            int selectedDistance = int.MaxValue;
            for (int i = 0; i < foot.Events.Count; i++)
            {
                AnimationFootMotionEvent candidate = foot.Events[i];
                if (candidate.Kind != kind ||
                    candidate.SampleIndex < 0 ||
                    candidate.SampleIndex >= activeSampleCount)
                {
                    continue;
                }
                int distance = targetSample - candidate.SampleIndex;
                if (distance <= 0)
                {
                    if (!looping)
                        continue;
                    distance += activeSampleCount;
                }
                if (distance >= selectedDistance)
                    continue;
                selectedSample = candidate.SampleIndex;
                selectedDistance = distance;
            }
            return selectedSample;
        }

        static int FindApproachContactSample(
            AnimationFootMotionFootPage foot,
            int liftOffSample,
            int landingSample,
            int activeSampleCount,
            bool looping)
        {
            int distance = BackwardSampleDistance(
                liftOffSample,
                landingSample,
                activeSampleCount,
                looping);
            if (distance <= 0)
                return -1;
            const float contactEpsilon = 0.0001f;
            for (int offset = 1; offset <= distance; offset++)
            {
                int sample = landingSample - offset;
                if (looping)
                    sample = Mod(sample, activeSampleCount);
                int next = sample + 1;
                if (looping)
                    next %= activeSampleCount;
                if (foot.Samples[sample].Filter.Contact <= contactEpsilon &&
                    foot.Samples[next].Filter.Contact > contactEpsilon)
                {
                    return next;
                }
            }
            return -1;
        }

        static int BackwardSampleDistance(
            int fromSample,
            int toSample,
            int activeSampleCount,
            bool looping)
        {
            int distance = toSample - fromSample;
            if (distance < 0 && looping)
                distance += activeSampleCount;
            return distance;
        }

        static float SecondsBetweenSamples(
            AnimationFootMotionDataDescriptor motionData,
            int fromSample,
            int toSample,
            bool looping,
            float sourceDurationSeconds)
        {
            float seconds =
                motionData.Raw.RootSamples[toSample].TimeSeconds -
                motionData.Raw.RootSamples[fromSample].TimeSeconds;
            if (seconds <= 0f && looping)
                seconds += sourceDurationSeconds;
            if (!float.IsFinite(seconds) || seconds < 0f)
                throw new InvalidOperationException("Formal Foot Step Event interval is invalid.");
            return seconds;
        }

        static int Mod(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
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

        static CharacterPresentationProducerEntry BuildProducer(
            CharacterPresentationSemanticReader reader,
            ProgramProducer producer,
            CharacterAnimationPresentationProfile profile,
            CharacterFootPlacementAnalysisCompilation footAnalysisCompilation,
            IReadOnlyDictionary<string, TimelineData> timelines,
            IReadOnlyDictionary<string, IReadOnlyList<AnimationTimelineCallSite>> timelineCallSites,
            List<string> errors)
        {
            AnimationFootAnalysisProjectionBuildData footAnalysis =
                footAnalysisCompilation?.BuildData;
            CharacterPresentationProducerKind? kind = reader.ResolveKind(producer, errors);
            ProgramSourceMapEntry source = reader.ResolveSource(producer, errors);
            if (!kind.HasValue || source == null)
                return null;

            if (kind.Value != CharacterPresentationProducerKind.Animation)
            {
                CharacterPresentationCameraBinding camera = kind.Value == CharacterPresentationProducerKind.Camera
                    ? BuildCameraBinding(reader, producer, source, timelines, errors)
                    : null;
                CharacterPresentationCueBinding cue = kind.Value == CharacterPresentationProducerKind.Cue
                    ? BuildCueBinding(producer, source, timelines, errors)
                    : null;
                if (kind.Value == CharacterPresentationProducerKind.Camera && camera == null ||
                    kind.Value == CharacterPresentationProducerKind.Cue && cue == null)
                    return null;
                return new CharacterPresentationProducerEntry(
                    producer.Index,
                    producer.Identity,
                    producer.SourceIdentity,
                    producer.ChannelKind,
                    kind.Value,
                    TimelinePlaybackMode.Once,
                    string.Empty,
                    string.Empty,
                    producer.AnimationChannelId,
                    source.GraphId,
                    source.NodeId,
                    source.TimelineId,
                    ParseTrackId(producer.SourceIdentity),
                    source.DisplayPath,
                    null,
                    camera,
                    cue);
            }

            if (!TryParseAnimationSource(producer.SourceIdentity, out AnimationProducerId producerId) ||
                !string.Equals(source.TimelineId, producerId.TimelineAuthoringId, StringComparison.Ordinal))
            {
                errors?.Add($"Animation producer '{producer.Identity}' has an invalid source identity.");
                return null;
            }
            if (!timelines.TryGetValue(producerId.TimelineAuthoringId, out TimelineData timeline))
            {
                errors?.Add($"Animation producer '{producer.Identity}' Timeline source is absent from the compiler inventory.");
                return null;
            }
            if (!TryResolvePlaybackMode(
                    producerId.TimelineAuthoringId,
                    timelineCallSites,
                    errors,
                    out TimelinePlaybackMode playbackMode))
                return null;
            AnimationTrack track = null;
            for (int i = 0; i < timeline.Tracks.Count; i++)
            {
                if (timeline.Tracks[i] is AnimationTrack candidate &&
                    string.Equals(candidate.AuthoringId, producerId.TrackAuthoringId, StringComparison.Ordinal))
                {
                    track = candidate;
                    break;
                }
            }
            if (track == null || track.AnimationChannelId != producer.AnimationChannelId)
            {
                errors?.Add($"Animation producer '{producer.Identity}' Track source or Animation Channel binding is invalid.");
                return null;
            }
            AnimationProducerPresentationBinding authoringBinding = profile.FindProducerBinding(producerId);
            if (authoringBinding == null)
            {
                errors?.Add($"Animation producer '{producerId}' has no Presentation source binding.");
                return null;
            }
            if (footAnalysis == null)
            {
                errors?.Add($"Animation producer '{producerId}' has no compiled Profile Foot Analysis source.");
                return null;
            }
            if (!authoringBinding.Source || !authoringBinding.Source.IsValid)
            {
                errors?.Add(
                    $"Animation producer '{producerId}' is not a finite Timeline Action source. Continuous Pose sources must be bound through a PoseState provider.");
                return null;
            }

            var clips = new List<CharacterPresentationAnimationClipBinding>();
            for (int i = 0; i < track.Clips.Count; i++)
            {
                if (track.Clips[i] is not BTSMTL.Timeline.AnimationClip clip)
                    continue;
                UnityEngine.AnimationClip sourceClip = clip.Clip;
                if (!sourceClip)
                {
                    errors?.Add($"Animation producer '{producerId}' segment '{clip.AuthoringId}' has no AnimationClip.");
                    continue;
                }
                try
                {
                    _ = CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(sourceClip);
                }
                catch (Exception exception)
                {
                    errors?.Add($"Animation producer '{producerId}' Clip '{sourceClip.name}' is invalid: {exception.Message}");
                    continue;
                }
                AnimationFootFeaturePair features = default;
                if (profile.FootPlacementAnalysisMode == CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures &&
                    (footAnalysis == null || !footAnalysis.TryGet(
                        producerId.TimelineAuthoringId,
                        producerId.TrackAuthoringId,
                        clip.AuthoringId,
                        out features)))
                {
                    errors?.Add($"Animation producer '{producerId}' clip '{clip.AuthoringId}' has no generated Foot Analysis features.");
                    continue;
                }
                CharacterAnimationClipContentIdentity clipIdentity =
                    CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(sourceClip);
                CharacterAnimationClipRegisteredCurveCatalog.ValidateFootMotionGroupRequired(sourceClip);
                AnimationCurve footWeight = CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                    sourceClip,
                    CharacterAnimationClipRegisteredCurveChannels.FootPlacementWeight);
                AnimationFootAnalysisArtifact artifact =
                    footAnalysisCompilation.RequireArtifact(
                        AnimationFootAnalysisProjectionBuildData.BindingKey(
                            producerId.TimelineAuthoringId,
                            producerId.TrackAuthoringId,
                            clip.AuthoringId));
                clips.Add(new CharacterPresentationAnimationClipBinding(
                    clip.AuthoringId,
                    $"{clipIdentity.AssetGuid}:{clipIdentity.LocalFileId}",
                    clipIdentity.FullDependencyHash,
                    clipIdentity.AnalysisInputHash,
                    clipIdentity.RegisteredCurveHash,
                    sourceClip,
                    clipIdentity.SourceDurationSeconds,
                    clip.StartTime,
                    clip.EndTime,
                    clip.ClipInTime,
                    clip.DurationTime,
                    clip.EaseInTime,
                    clip.EaseOutTime,
                    clip.ExtraPolationMode,
                    clip.WeightCurve,
                    clip.EaseInCurve,
                    clip.EaseOutCurve,
                    NormalizeRegisteredCurve(footWeight, clipIdentity.SourceDurationSeconds),
                    features,
                    CompileFootStepObservation(
                        sourceClip,
                        clipIdentity.SourceDurationSeconds,
                        artifact.MotionData)));
            }
            if (clips.Count == 0)
            {
                errors?.Add($"Animation producer '{producerId}' has no compiled AnimationClip binding.");
                return null;
            }
            if (!TryCompileLastSampleTime(
                    track,
                    timeline.Duration,
                    producerId,
                    errors,
                    out float lastSampleTimeSeconds))
            {
                return null;
            }
            var animation = new CharacterPresentationAnimationBinding(
                authoringBinding.Source,
                track.Name,
                timeline.Duration,
                lastSampleTimeSeconds,
                clips.ToArray());
            return new CharacterPresentationProducerEntry(
                producer.Index,
                producer.Identity,
                producer.SourceIdentity,
                producer.ChannelKind,
                kind.Value,
                playbackMode,
                producerId.TimelineAuthoringId,
                producerId.TrackAuthoringId,
                producer.AnimationChannelId,
                source.GraphId,
                source.NodeId,
                source.TimelineId,
                producerId.TrackAuthoringId,
                source.DisplayPath,
                animation,
                null,
                null);
        }

        static bool TryCompileLastSampleTime(
            AnimationTrack track,
            float timelineDuration,
            AnimationProducerId producerId,
            List<string> errors,
            out float lastSampleTimeSeconds)
        {
            lastSampleTimeSeconds = 0f;
            if (track == null ||
                !float.IsFinite(timelineDuration) ||
                timelineDuration <= 0f)
            {
                errors?.Add(
                    $"Animation producer '{producerId}' has an invalid Timeline duration.");
                return false;
            }

            BTSMTL.Timeline.AnimationClip[] clips =
                track.Clips
                    .OfType<BTSMTL.Timeline.AnimationClip>()
                    .Where(value => value != null && value.Clip)
                    .OrderBy(value => value.StartTime)
                    .ThenBy(value => value.EndTime)
                    .ToArray();
            if (clips.Length == 0)
            {
                errors?.Add(
                    $"Animation producer '{producerId}' has no sampleable AnimationClip coverage.");
                return false;
            }

            const float tolerance = 0.00001f;
            float coverageEnd = 0f;
            bool held = false;
            for (int i = 0; i < clips.Length; i++)
            {
                BTSMTL.Timeline.AnimationClip clip = clips[i];
                if (clip.StartTime > coverageEnd + tolerance)
                {
                    errors?.Add(
                        $"Animation producer '{producerId}' has an AnimationClip coverage gap at {coverageEnd:R}-{clip.StartTime:R} seconds.");
                    return false;
                }
                coverageEnd = Math.Max(coverageEnd, clip.EndTime);
                if (clip.ExtraPolationMode == ExtraPolationMode.Hold)
                {
                    held = true;
                    coverageEnd = timelineDuration;
                    break;
                }
            }

            float boundedEnd = Math.Min(coverageEnd, timelineDuration);
            lastSampleTimeSeconds = held
                ? boundedEnd
                : Math.Max(0f, boundedEnd - 1f / 60000f);
            if (!float.IsFinite(lastSampleTimeSeconds) ||
                lastSampleTimeSeconds <= 0f)
            {
                errors?.Add(
                    $"Animation producer '{producerId}' has no positive finite sample coverage.");
                return false;
            }
            return true;
        }


        static bool TryResolvePlaybackMode(
            string timelineAuthoringId,
            IReadOnlyDictionary<string, IReadOnlyList<AnimationTimelineCallSite>> timelineCallSites,
            List<string> errors,
            out TimelinePlaybackMode playbackMode)
        {
            playbackMode = default;
            if (timelineCallSites == null ||
                !timelineCallSites.TryGetValue(timelineAuthoringId, out IReadOnlyList<AnimationTimelineCallSite> callSites) ||
                callSites == null ||
                callSites.Count == 0)
            {
                errors?.Add($"Animation Timeline '{timelineAuthoringId}' has no playback call site.");
                return false;
            }

            playbackMode = callSites[0].PlaybackMode;
            for (int i = 1; i < callSites.Count; i++)
            {
                if (callSites[i].PlaybackMode == playbackMode)
                    continue;
                errors?.Add(
                    $"Animation Timeline '{timelineAuthoringId}' is called with both {playbackMode} and {callSites[i].PlaybackMode}; " +
                    "one Presentation producer cannot own conflicting playback modes.");
                return false;
            }
            return true;
        }

        static IReadOnlyDictionary<string, IReadOnlyList<AnimationTimelineCallSite>> CollectTimelineCallSites(
            CharacterAuthoringGraphOccurrence root)
        {
            var result = new Dictionary<string, List<AnimationTimelineCallSite>>(StringComparer.Ordinal);
            CollectTimelineCallSites(root, result);
            return result.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<AnimationTimelineCallSite>)pair.Value.ToArray(),
                StringComparer.Ordinal);
        }

        static void CollectTimelineCallSites(
            CharacterAuthoringGraphOccurrence occurrence,
            Dictionary<string, List<AnimationTimelineCallSite>> result)
        {
            if (occurrence == null)
                return;
            for (int i = 0; i < occurrence.Timelines.Count; i++)
            {
                CharacterAuthoringTimelineRecord timeline = occurrence.Timelines[i];
                string timelineId = timeline.Timeline.AuthoringId;
                if (!result.TryGetValue(timelineId, out List<AnimationTimelineCallSite> values))
                {
                    values = new List<AnimationTimelineCallSite>();
                    result.Add(timelineId, values);
                }
                values.Add(new AnimationTimelineCallSite(timeline.Route, timeline.Node.PlaybackMode));
            }
            for (int i = 0; i < occurrence.GraphReferences.Count; i++)
                CollectTimelineCallSites(occurrence.GraphReferences[i].Child, result);
        }

        static CharacterPresentationCameraBinding BuildCameraBinding(
            CharacterPresentationSemanticReader reader,
            ProgramProducer producer,
            ProgramSourceMapEntry source,
            IReadOnlyDictionary<string, TimelineData> timelines,
            List<string> errors)
        {
            if (TryFindSourceClip(source, timelines, out Clip clip))
            {
                if (clip is CameraStateClip state)
                {
                    return CharacterPresentationCameraBinding.State(
                        state.Mode,
                        state.Priority,
                        state.BlendInSeconds,
                        state.BlendOutSeconds,
                        state.TargetKey,
                        state.InterruptPolicy);
                }
                if (clip is CameraCueClip cue)
                {
                    return CharacterPresentationCameraBinding.Cue(
                        cue.CueId,
                        cue.CueKind,
                        cue.CueType,
                        cue.DurationSeconds,
                        cue.Priority);
                }
                if (clip is CameraResponseClip response)
                {
                    return CharacterPresentationCameraBinding.Response(
                        response.LookResponse,
                        response.ManualOrbitWeight,
                        response.PitchResponseWeight,
                        response.YawResponseWeight,
                        response.Priority);
                }
                errors?.Add($"Camera producer '{producer.Identity}' source clip type '{clip.GetType().Name}' is unsupported.");
                return null;
            }

            try
            {
                SemanticOperation operation = reader.RequireProducerOperation(producer);
                if (operation.Integer0 != CameraProgramOperationSchema.PayloadVersion)
                    throw new InvalidOperationException($"payload version '{operation.Integer0}' is unsupported");
                return operation.Code switch
                {
                    SimulationOperationCode.CameraStateRequest => CharacterPresentationCameraBinding.State(
                        (TimelineCameraMode)operation.Integer1,
                        reader.RequireInt32(operation, "Priority"),
                        reader.RequireScalar(operation, "BlendInSeconds"),
                        reader.RequireScalar(operation, "BlendOutSeconds"),
                        reader.RequireString(operation, "TargetKey"),
                        (TimelineCameraInterruptPolicy)operation.Flags),
                    SimulationOperationCode.CameraCue => CharacterPresentationCameraBinding.Cue(
                        reader.RequireString(operation, "CueId"),
                        (TimelineCameraCueKind)operation.Integer1,
                        reader.RequireString(operation, "CueType"),
                        reader.RequireScalar(operation, "DurationSeconds"),
                        reader.RequireInt32(operation, "Priority")),
                    SimulationOperationCode.CameraResponse => CharacterPresentationCameraBinding.Response(
                        (TimelineCameraLookResponseMode)operation.Integer1,
                        reader.RequireScalar(operation, "ManualOrbitWeight"),
                        reader.RequireScalar(operation, "PitchResponseWeight"),
                        reader.RequireScalar(operation, "YawResponseWeight"),
                        reader.RequireInt32(operation, "Priority")),
                    SimulationOperationCode.CameraTarget => CharacterPresentationCameraBinding.Target(
                        reader.RequireString(operation, "TargetKey"),
                        reader.RequireString(operation, "AnchorKey"),
                        reader.RequireString(operation, "AimPointKey"),
                        reader.RequireString(operation, "PreferredBoneKey"),
                        reader.RequireInt32(operation, "Priority")),
                    _ => throw new InvalidOperationException($"operation '{operation.Code}' is unsupported")
                };
            }
            catch (Exception exception)
            {
                errors?.Add($"Camera producer '{producer.Identity}' Graph payload is invalid: {exception.Message}.");
                return null;
            }
        }

        static CharacterPresentationCueBinding BuildCueBinding(
            ProgramProducer producer,
            ProgramSourceMapEntry source,
            IReadOnlyDictionary<string, TimelineData> timelines,
            List<string> errors)
        {
            if (TryFindSourceClip(source, timelines, out Clip clip))
            {
                if (clip is ActionCueClip cue)
                    return new CharacterPresentationCueBinding(cue.CueId, cue.CueType);
                errors?.Add($"Cue producer '{producer.Identity}' source clip type '{clip.GetType().Name}' is unsupported.");
                return null;
            }
            const string cueMarker = ":cue:";
            int marker = producer.Identity.LastIndexOf(cueMarker, StringComparison.Ordinal);
            if (marker >= 0)
            {
                string suffix = producer.Identity.Substring(marker + cueMarker.Length);
                int separator = suffix.IndexOf(':');
                string cueId = separator >= 0 ? suffix.Substring(separator + 1) : suffix;
                if (!string.IsNullOrEmpty(cueId))
                    return new CharacterPresentationCueBinding(cueId, "GameplayEffect");
            }
            errors?.Add($"Cue producer '{producer.Identity}' has no resolvable authoring payload.");
            return null;
        }

       static bool TryFindSourceClip(
            ProgramSourceMapEntry source,
            IReadOnlyDictionary<string, TimelineData> timelines,
            out Clip clip)
        {
            clip = null;
            if (source == null || string.IsNullOrEmpty(source.TimelineId) || string.IsNullOrEmpty(source.ClipId) ||
                !timelines.TryGetValue(source.TimelineId, out TimelineData timeline))
                return false;
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (string.Equals(track.Clips[clipIndex].AuthoringId, source.ClipId, StringComparison.Ordinal))
                    {
                        clip = track.Clips[clipIndex];
                        return true;
                    }
                }
            }
            return false;
        }

        static bool TryParseAnimationSource(string sourceIdentity, out AnimationProducerId producerId)
        {
            producerId = default;
            const string timelinePrefix = "timeline:";
            const string trackSeparator = "/track:";
            if (string.IsNullOrEmpty(sourceIdentity) || !sourceIdentity.StartsWith(timelinePrefix, StringComparison.Ordinal))
                return false;
            int separator = sourceIdentity.IndexOf(trackSeparator, timelinePrefix.Length, StringComparison.Ordinal);
            if (separator < 0)
                return false;
            producerId = new AnimationProducerId(
                sourceIdentity.Substring(timelinePrefix.Length, separator - timelinePrefix.Length),
                sourceIdentity.Substring(separator + trackSeparator.Length));
            return producerId.IsValid;
        }

        static string ParseTrackId(string sourceIdentity)
        {
            const string trackSeparator = "/track:";
            int separator = string.IsNullOrEmpty(sourceIdentity)
                ? -1
                : sourceIdentity.IndexOf(trackSeparator, StringComparison.Ordinal);
            return separator < 0 ? string.Empty : sourceIdentity.Substring(separator + trackSeparator.Length);
        }

        internal static string ComputeProjectionRevision(
            CharacterAnimationPresentationProfile animationProfile,
            UnityEngine.Object equipmentPresentationProfile,
            StableHash contractHash,
            IReadOnlyList<string> footAnalysisTokens,
            MotionMatchingProjectionPayload motionMatching)
        {
            var values = new List<string>
            {
                CharacterPresentationProjection.CurrentAbiVersion,
                contractHash.ToString()
            };
            AddProjectionAssetRevision(animationProfile, values);
            AddProjectionAssetRevision(equipmentPresentationProfile, values);
            AddMotionMatchingRevision(motionMatching, values);
            if (footAnalysisTokens != null)
            {
                for (int i = 0; i < footAnalysisTokens.Count; i++)
                    values.Add(footAnalysisTokens[i]);
            }
            return StableHash.Compute(values.ToArray()).ToString();
        }

        static MotionMatchingProjectionPayload CompileMotionMatchingPayload(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseSourceCompilationCatalog sourceCatalog,
            List<string> errors)
        {
            if (!profile || sourceCatalog == null)
                return null;
            CharacterMotionMatchingBinding[] bindings = profile.PoseGraph.EnumerateGraphs()
                .SelectMany(value => value.Nodes)
                .Select(value => (value?.Payload as CharacterMotionMatchingPosePayload)?.Binding)
                .Where(value => value)
                .Distinct()
                .ToArray();
            if (bindings.Length == 0)
                return null;
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
                return MotionMatchingProjectionPayloadCompiler.Compile(
                    profiles[0],
                    profile.PoseGraph,
                    profile.RigDefinition,
                    analysisSource,
                    AnimationClipMotionMatchingParameterCurveResolver.Instance);
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
                return null;
            }
        }

        static void AddMotionMatchingRevision(MotionMatchingProjectionPayload payload, List<string> values)
        {
            if (payload == null)
            {
                values.Add("motion-matching:none");
                return;
            }
            values.Add($"motion-matching:{payload.ProfileId.Value}:{payload.ProfileRevision}");
            for (int i = 0; i < payload.DatabaseCount; i++)
            {
                CharacterMotionMatchingDatabaseArtifactIdentity identity = payload.GetDatabase(i).ArtifactIdentity;
                values.Add($"{identity.DatabaseId.Value}:{identity.DatabaseRevision}:{identity.AnalysisInputHash}:{identity.OrderedClipDependencyHash}:{identity.ContentHash}");
            }
        }

        static void AddProjectionAssetRevision(UnityEngine.Object root, List<string> values)
        {
            if (!root)
            {
                values.Add("none");
                return;
            }
            string rootPath = AssetDatabase.GetAssetPath(root);
            string[] dependencies = AssetDatabase.GetDependencies(rootPath, true)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            values.Add(rootPath);
            for (int i = 0; i < dependencies.Length; i++)
            {
                string path = dependencies[i];
                values.Add(path);
                values.Add(AssetDatabase.AssetPathToGUID(path));
                values.Add(AssetDatabase.GetAssetDependencyHash(path).ToString());
            }
        }

        static void CompileEquipmentProjection(
            CharacterEquipmentProfile gameplayProfile,
            CharacterEquipmentPresentationProfile presentationProfile,
            List<string> errors,
            out EquipmentVisualProjectionBinding[] visualBindings)
        {
            visualBindings = Array.Empty<EquipmentVisualProjectionBinding>();
            if (!gameplayProfile && !presentationProfile)
                return;
            if (!gameplayProfile || !presentationProfile)
            {
                errors?.Add("Equipment Projection requires both Gameplay and Presentation Profiles.");
                return;
            }
            presentationProfile.CollectConfigurationErrors(gameplayProfile, errors);
            visualBindings = presentationProfile.VisualBindings
                .Where(value => value != null)
                .OrderBy(value => value.VisualBindingId.Value, StringComparer.Ordinal)
                .Select(value => new EquipmentVisualProjectionBinding(value))
                .ToArray();
            var bindingIds = new HashSet<EquipmentVisualBindingId>();
            for (int i = 0; i < visualBindings.Length; i++)
            {
                if (!visualBindings[i].VisualBindingId.IsValid || !bindingIds.Add(visualBindings[i].VisualBindingId))
                    errors?.Add($"Equipment Projection visual binding #{i} is invalid or duplicated.");
            }
            for (int i = 0; i < gameplayProfile.Equipment.Count; i++)
            {
                EquipmentDefinition item = gameplayProfile.Equipment[i];
                if (item && !bindingIds.Contains(item.VisualBindingId))
                    errors?.Add($"Equipment '{item.EquipmentIdValue}' references unresolved visual binding '{item.VisualBindingIdValue}'.");
            }
        }
    }
}
