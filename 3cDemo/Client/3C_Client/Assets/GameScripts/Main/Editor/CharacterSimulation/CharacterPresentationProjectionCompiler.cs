using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Editor.MotionMatching;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonSimulation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
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
        sealed class AnimationBlendCatalogCompilation
        {
            public AnimationBlendCatalogCompilation(
                AnimationBlendCurveCatalogPayload curveCatalog,
                AnimationBlendProfileCatalogPayload profileCatalog,
                Dictionary<string, int> curveIndices,
                Dictionary<string, int> profileIndices,
                Dictionary<string, int> profileIndicesByIdentity)
            {
                CurveCatalog = curveCatalog;
                ProfileCatalog = profileCatalog;
                CurveIndices = curveIndices;
                ProfileIndices = profileIndices;
                ProfileIndicesByIdentity = profileIndicesByIdentity;
            }

            public AnimationBlendCurveCatalogPayload CurveCatalog { get; }
            public AnimationBlendProfileCatalogPayload ProfileCatalog { get; }
            public Dictionary<string, int> CurveIndices { get; }
            public Dictionary<string, int> ProfileIndices { get; }
            public Dictionary<string, int> ProfileIndicesByIdentity { get; }
        }

        public static CharacterPresentationProjectionCompileResult Compile(
            CharacterPresentationProjectionCompileRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var errors = new List<string>();
            CharacterAuthoringCompilationModel model = request.Model;
            var reader = new CharacterPresentationSemanticReader(request.Artifact);
            MotionMatchingProjectionPayload motionMatching = CompileMotionMatchingPayload(
                model.AnimationPresentationProfile,
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
                request.FootAnalysis.BuildData,
                motionMatching,
                model.Timelines,
                CollectAnimationMarkerSyncCallSites(model.Root),
                errors);
            var diagnostics = new CharacterPresentationProjectionDiagnostic[errors.Count];
            for (int i = 0; i < errors.Count; i++)
            {
                diagnostics[i] = new CharacterPresentationProjectionDiagnostic(
                    "presentation_projection_invalid",
                    request.Artifact.Header.ProgramId.Value,
                    errors[i]);
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
            AnimationFootAnalysisProjectionBuildData footAnalysis,
            MotionMatchingProjectionPayload motionMatching,
            IReadOnlyDictionary<string, TimelineData> timelines,
            IReadOnlyDictionary<string, IReadOnlyList<AnimationMarkerSyncCallSite>> markerSyncCallSites,
            List<string> errors)
        {
            if (reader == null || profile == null || timelines == null || markerSyncCallSites == null)
            {
                errors?.Add("Character Presentation Projection build input is incomplete.");
                return null;
            }

            profile.CollectConfigurationErrors(errors);
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
            if (!ValidateMarkerSyncAuthoring(reader.Producers, profile, timelines, markerSyncCallSites, errors))
                return null;

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
                    footAnalysis,
                    blendSpaces,
                    blendSpaceIndices,
                    timelines,
                    markerSyncCallSites,
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
            AnimationBlendCatalogCompilation blendCatalogs = CompileBlendCatalogs(
                profile.PoseGraph.Graph,
                profile.RigDefinition,
                errors);
            AnimationBlendNodePayload[] blendNodes = blendCatalogs == null
                ? Array.Empty<AnimationBlendNodePayload>()
                : CompileBlendNodes(
                    profile.PoseGraph.Graph,
                    profile.RigDefinition,
                    entries,
                    blendCatalogs,
                    errors);
            CharacterPresentationPosePlan poseProgram = CharacterPresentationPoseGraphCompiler.Compile(
                profile.PoseGraph,
                profile.RigDefinition,
                animationChannels,
                blendNodes,
                errors);
            if (poseProgram != null && blendCatalogs != null)
            {
                poseProgram = CharacterPresentationInertializationPlanCompiler.Compile(
                    poseProgram,
                    profile.PoseGraph.Graph,
                    profile.RigDefinition,
                    entries,
                    blendCatalogs.CurveIndices,
                    blendCatalogs.ProfileIndicesByIdentity,
                    errors);
            }
            CharacterAnimationBlendSpacePlayerPlan[] blendSpacePlayers = poseProgram == null
                ? Array.Empty<CharacterAnimationBlendSpacePlayerPlan>()
                : CompileBlendSpacePlayers(poseProgram, entries, blendSpaces, errors);
            CharacterAnimationRigPayload rig = poseProgram == null
                ? null
                : new CharacterAnimationRigPayload(profile.RigDefinition);
            CompileEquipmentProjection(
                equipmentProfile,
                equipmentPresentationProfile,
                errors,
                out EquipmentVisualProjectionBinding[] visualBindings);
            if (errors.Count > 0)
                return null;

            return CharacterPresentationProjection.Create(
                reader.Contract,
                poseProgram,
                blendCatalogs.CurveCatalog,
                blendCatalogs.ProfileCatalog,
                rig,
                motionMatching,
                blendSpaces.ToArray(),
                blendSpacePlayers,
                entries.ToArray(),
                footIdentity,
                projectionRevision,
                visualBindings);
        }

        static CharacterAnimationBlendSpacePlayerPlan[] CompileBlendSpacePlayers(
            CharacterPresentationPosePlan posePlan,
            IReadOnlyList<CharacterPresentationProducerEntry> producers,
            IReadOnlyList<CharacterAnimationBlendSpacePlan> blendSpaces,
            List<string> errors)
        {
            var result = new List<CharacterAnimationBlendSpacePlayerPlan>();
            for (int operationIndex = 0; operationIndex < posePlan.Operations.Count; operationIndex++)
            {
                CharacterPresentationPoseOperation operation = posePlan.Operations[operationIndex];
                if (operation.Code != CharacterPoseOperationCode.BlendSpacePlayer)
                    continue;
                if (operation.SelectionInputIndex < 0 || operation.SelectionInputIndex >= posePlan.SelectionInputs.Count ||
                    operation.ParameterIndex < 0 || operation.ParameterIndex >= posePlan.Parameters.Count)
                {
                    errors?.Add($"Blend Space Player '{operation.NodeId}' has incomplete compiled inputs.");
                    continue;
                }
                AnimationChannelId channelId = posePlan.SelectionInputs[operation.SelectionInputIndex].AnimationChannelId;
                CharacterPresentationProducerEntry[] reachable = producers
                    .Where(value => value != null && value.Kind == CharacterPresentationProducerKind.Animation &&
                                    value.AnimationChannelId == channelId)
                    .OrderBy(value => value.ProgramProducerIndex)
                    .ToArray();
                if (reachable.Length == 0 || reachable.Any(value => value.AnimationSourceKind != AnimationPoseSourceKind.BlendSpace))
                {
                    errors?.Add($"Blend Space Player '{operation.NodeId}' channel '{channelId}' has a missing or non-Blend Space producer endpoint.");
                    continue;
                }
                int[] planIndices = reachable
                    .Select(value => value.BlendSpacePlanIndex)
                    .Distinct()
                    .OrderBy(value => value)
                    .ToArray();
                if (planIndices.Any(value => value < 0 || value >= blendSpaces.Count))
                {
                    errors?.Add($"Blend Space Player '{operation.NodeId}' has an invalid Projection plan source.");
                    continue;
                }
                CharacterAnimationBlendSpacePlan reference = blendSpaces[planIndices[0]];
                bool consistent = true;
                if (reference.Mode == CharacterAnimationBlendSpaceMode.Linear1D &&
                    operation.BlendSpaceInputRangePolicy != CharacterAnimationBlendSpaceInputRangePolicy.Clamp)
                    consistent = false;
                for (int i = 1; i < planIndices.Length; i++)
                    consistent &= SameAxisContract(reference, blendSpaces[planIndices[i]]);
                CharacterPresentationPoseParameterEntry xParameter = posePlan.Parameters[operation.ParameterIndex];
                consistent &= SameParameterContract(xParameter, reference.XAxis);
                if (reference.AxisCount == 1)
                    consistent &= operation.ParameterIndexB == -1;
                else if (operation.ParameterIndexB < 0 || operation.ParameterIndexB >= posePlan.Parameters.Count)
                    consistent = false;
                else
                    consistent &= SameParameterContract(posePlan.Parameters[operation.ParameterIndexB], reference.YAxis);
                for (int parameterIndex = 0; parameterIndex < posePlan.Parameters.Count; parameterIndex++)
                {
                    if (parameterIndex == operation.ParameterIndex || parameterIndex == operation.ParameterIndexB)
                        continue;
                    PoseParameterId parameterId = posePlan.Parameters[parameterIndex].ParameterId;
                    if (!reference.TryGetParameterPolicy(parameterId, out CharacterAnimationBlendSpaceParameterPolicy policy))
                    {
                        consistent = false;
                        break;
                    }
                    for (int planIndex = 1; planIndex < planIndices.Length; planIndex++)
                    {
                        if (!blendSpaces[planIndices[planIndex]].TryGetParameterPolicy(parameterId, out CharacterAnimationBlendSpaceParameterPolicy candidate) ||
                            candidate != policy)
                        {
                            consistent = false;
                            break;
                        }
                    }
                }
                if (!consistent)
                {
                    errors?.Add($"Blend Space Player '{operation.NodeId}' producer assets and typed axis inputs do not share one ParameterId/type/unit contract.");
                    continue;
                }
                result.Add(new CharacterAnimationBlendSpacePlayerPlan(
                    operation.NodeId,
                    operation.Index,
                    operation.PlayerIndex,
                    operation.SelectionInputIndex,
                    operation.ParameterIndex,
                    operation.ParameterIndexB,
                    operation.BlendSpaceInputRangePolicy,
                    planIndices));
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
            AnimationFootAnalysisProjectionBuildData footAnalysis,
            List<CharacterAnimationBlendSpacePlan> blendSpaces,
            Dictionary<CharacterAnimationBlendSpaceAsset, int> blendSpaceIndices,
            IReadOnlyDictionary<string, TimelineData> timelines,
            IReadOnlyDictionary<string, IReadOnlyList<AnimationMarkerSyncCallSite>> timelineCallSites,
            List<string> errors)
        {
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
                    default,
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
            if (authoringBinding.SourceKind == AnimationPoseSourceKind.MotionMatching)
            {
                return new CharacterPresentationProducerEntry(
                    producer.Index,
                    producer.Identity,
                    producer.SourceIdentity,
                    producer.ChannelKind,
                    kind.Value,
                    AnimationPoseSourceKind.MotionMatching,
                    playbackMode,
                    producerId.TimelineAuthoringId,
                    producerId.TrackAuthoringId,
                    producer.AnimationChannelId,
                    source.GraphId,
                    source.NodeId,
                    source.TimelineId,
                    producerId.TrackAuthoringId,
                    source.DisplayPath,
                    null,
                    null,
                    null);
            }
            if (authoringBinding.SourceKind == AnimationPoseSourceKind.BlendSpace)
            {
                CharacterAnimationBlendSpaceAsset blendSpace = authoringBinding.BlendSpaceSource;
                CharacterAnimationBlendSpaceValidationReport report = CharacterAnimationBlendSpaceValidator.Validate(blendSpace);
                if (!report.IsValid)
                {
                    report.CopyMessagesTo(errors);
                    return null;
                }
                if (!blendSpaceIndices.TryGetValue(blendSpace, out int planIndex))
                {
                    var samples = new CharacterAnimationBlendSpaceSamplePlan[blendSpace.Samples.Count];
                    for (int i = 0; i < samples.Length; i++)
                    {
                        CharacterAnimationBlendSpaceSample sample = blendSpace.Samples[i];
                        AnimationFootFeaturePair features = default;
                        if (profile.FootPlacementAnalysisMode == CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures &&
                            (footAnalysis == null || !footAnalysis.TryGetBlendSpace(
                                blendSpace.BlendSpaceId,
                                sample.SampleId,
                                out features)))
                        {
                            errors?.Add($"Blend Space '{blendSpace.BlendSpaceId}' Sample '{sample.SampleId}' has no generated Foot Analysis features.");
                            continue;
                        }
                        samples[i] = new CharacterAnimationBlendSpaceSamplePlan(sample, features);
                    }
                    if (errors.Count > 0)
                        return null;
                    try
                    {
                        var plan = new CharacterAnimationBlendSpacePlan(blendSpace, samples);
                        plan.RequireValid(profile.FootPlacementAnalysisMode == CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures);
                        planIndex = blendSpaces.Count;
                        blendSpaces.Add(plan);
                        blendSpaceIndices.Add(blendSpace, planIndex);
                    }
                    catch (Exception exception)
                    {
                        errors?.Add($"Blend Space '{blendSpace.name}' Projection compile failed: {exception.Message}");
                        return null;
                    }
                }
                return new CharacterPresentationProducerEntry(
                    producer.Index,
                    producer.Identity,
                    producer.SourceIdentity,
                    producer.ChannelKind,
                    kind.Value,
                    AnimationPoseSourceKind.BlendSpace,
                    playbackMode,
                    producerId.TimelineAuthoringId,
                    producerId.TrackAuthoringId,
                    producer.AnimationChannelId,
                    source.GraphId,
                    source.NodeId,
                    source.TimelineId,
                    producerId.TrackAuthoringId,
                    source.DisplayPath,
                    null,
                    null,
                    null,
                    planIndex,
                    blendSpace.Samples.Count,
                    blendSpaces[planIndex].ClockDurationSeconds,
                    blendSpaces[planIndex].MarkerSync);
            }
            if (authoringBinding.SourceKind != AnimationPoseSourceKind.Timeline ||
                !authoringBinding.Source || !authoringBinding.Source.IsValid)
            {
                errors?.Add($"Animation producer '{producerId}' has no valid Timeline Animancer source binding.");
                return null;
            }

            var clips = new List<CharacterPresentationAnimationClipBinding>();
            for (int i = 0; i < track.Clips.Count; i++)
            {
                if (track.Clips[i] is not BTSMTL.Timeline.AnimationClip clip)
                    continue;
                if (!clip.Clip)
                {
                    errors?.Add($"Animation producer '{producerId}' clip '{clip.AuthoringId}' has no AnimationClip resource.");
                    continue;
                }
                clip.RequireFootPlacementWeightCurve();
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
                clips.Add(new CharacterPresentationAnimationClipBinding(
                    clip.AuthoringId,
                    clip.Clip,
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
                    clip.FootPlacementCurve,
                    features));
            }
            if (clips.Count == 0)
            {
                errors?.Add($"Animation producer '{producerId}' has no compiled AnimationClip binding.");
                return null;
            }
            var animation = new CharacterPresentationAnimationBinding(
                authoringBinding.Source,
                track.Name,
                timeline.Duration,
                clips.ToArray(),
                CompileMarkerSync(track, timeline),
                new AnimationMarkerBindingId($"marker-binding:{producer.Identity}"));
            return new CharacterPresentationProducerEntry(
                producer.Index,
                producer.Identity,
                producer.SourceIdentity,
                producer.ChannelKind,
                kind.Value,
                AnimationPoseSourceKind.Timeline,
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

        static bool TryResolvePlaybackMode(
            string timelineAuthoringId,
            IReadOnlyDictionary<string, IReadOnlyList<AnimationMarkerSyncCallSite>> timelineCallSites,
            List<string> errors,
            out TimelinePlaybackMode playbackMode)
        {
            playbackMode = default;
            if (timelineCallSites == null ||
                !timelineCallSites.TryGetValue(timelineAuthoringId, out IReadOnlyList<AnimationMarkerSyncCallSite> callSites) ||
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

        readonly struct SelectionEndpoint
        {
            public SelectionEndpoint(
                AnimationChannelId channelId,
                string programProducerId,
                AnimationSelectionAvailabilityPolicy availability)
            {
                ChannelId = channelId;
                ProgramProducerId = programProducerId ?? string.Empty;
                Availability = availability;
            }

            public AnimationChannelId ChannelId { get; }
            public string ProgramProducerId { get; }
            public AnimationSelectionAvailabilityPolicy Availability { get; }
        }

        sealed class CompiledBlendAuthoringNode
        {
            public CompiledBlendAuthoringNode(PoseNodeId nodeId, CharacterPoseNodeDefinition node, SelectionEndpoint selection)
            {
                NodeId = nodeId;
                Node = node;
                Selection = selection;
            }

            public PoseNodeId NodeId { get; }
            public CharacterPoseNodeDefinition Node { get; }
            public SelectionEndpoint Selection { get; }
        }

        static AnimationBlendCatalogCompilation CompileBlendCatalogs(
            CharacterPoseGraphData graph,
            CharacterAnimationRigDefinition rig,
            List<string> errors)
        {
            if (graph == null || !rig)
                return null;
            var curves = new SortedDictionary<string, AnimationBlendCurvePayload>(StringComparer.Ordinal);
            var profiles = new SortedDictionary<string, AnimationBlendProfilePayload>(StringComparer.Ordinal);
            var profileIdentityKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            List<CompiledBlendAuthoringNode> blendNodes = CollectBlendAuthoringNodes(graph);
            for (int i = 0; i < blendNodes.Count; i++)
            {
                CharacterAnimationBlendPolicy policy = blendNodes[i].Node.BlendPolicy;
                if (!policy)
                    continue;
                CollectBlendRule(policy.DefaultTransition, rig, curves, profiles, profileIdentityKeys, errors);
                for (int overrideIndex = 0; overrideIndex < policy.Overrides.Count; overrideIndex++)
                    CollectBlendRule(policy.Overrides[overrideIndex]?.Rule, rig, curves, profiles, profileIdentityKeys, errors);
            }
            List<CharacterPoseInertializationPolicy> inertialPolicies = CollectInertializationPolicies(graph);
            for (int i = 0; i < inertialPolicies.Count; i++)
            {
                CharacterPoseInertializationPolicy policy = inertialPolicies[i];
                if (!policy)
                    continue;
                CollectInertialRule(policy.DefaultRule, rig, curves, profiles, profileIdentityKeys, errors);
                for (int overrideIndex = 0; overrideIndex < policy.Overrides.Count; overrideIndex++)
                    CollectInertialRule(policy.Overrides[overrideIndex]?.Rule, rig, curves, profiles, profileIdentityKeys, errors);
            }
            if (curves.Count == 0 || profiles.Count == 0)
            {
                errors?.Add("Animation Blend catalogs cannot be empty.");
                return null;
            }

            var curveEntries = new AnimationBlendCurveCatalogEntry[curves.Count];
            var curveIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            int curveIndex = 0;
            foreach (KeyValuePair<string, AnimationBlendCurvePayload> pair in curves)
            {
                curveEntries[curveIndex] = new AnimationBlendCurveCatalogEntry(curveIndex, pair.Value);
                curveIndices.Add(pair.Key, curveIndex);
                curveIndex++;
            }
            var profileEntries = new AnimationBlendProfileCatalogEntry[profiles.Count];
            var profileIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            var profileIndicesByIdentity = new Dictionary<string, int>(StringComparer.Ordinal);
            int profileIndex = 0;
            foreach (KeyValuePair<string, AnimationBlendProfilePayload> pair in profiles)
            {
                profileEntries[profileIndex] = new AnimationBlendProfileCatalogEntry(profileIndex, pair.Value);
                profileIndices.Add(pair.Key, profileIndex);
                profileIndicesByIdentity.Add(pair.Value.ProfileId, profileIndex);
                profileIndex++;
            }
            try
            {
                var curveCatalog = new AnimationBlendCurveCatalogPayload(curveEntries);
                var profileCatalog = new AnimationBlendProfileCatalogPayload(profileEntries);
                profileCatalog.RequireValid(rig.Bones.Count, rig.RigId, rig.Revision);
                return new AnimationBlendCatalogCompilation(
                    curveCatalog,
                    profileCatalog,
                    curveIndices,
                    profileIndices,
                    profileIndicesByIdentity);
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
                return null;
            }
        }

        static void CollectBlendRule(
            CharacterAnimationBlendTransitionRule rule,
            CharacterAnimationRigDefinition rig,
            SortedDictionary<string, AnimationBlendCurvePayload> curves,
            SortedDictionary<string, AnimationBlendProfilePayload> profiles,
            Dictionary<string, string> profileIdentityKeys,
            List<string> errors)
        {
            if (rule == null)
                return;
            try
            {
                rule.RequireValid(rig);
                AnimationBlendCurvePayload curve = rule.Curve.Compile();
                string curveKey = AnimationBlendCanonicalPayload.CurveKey(curve);
                if (!curves.ContainsKey(curveKey))
                    curves.Add(curveKey, curve);

                var profile = new AnimationBlendProfilePayload(rule.BlendProfile, rig);
                string profileKey = AnimationBlendCanonicalPayload.ProfileKey(profile);
                if (profileIdentityKeys.TryGetValue(profile.ProfileId, out string existingKey) &&
                    !string.Equals(existingKey, profileKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Animation Blend Profile identity '{profile.ProfileId}' resolves to multiple canonical payloads.");
                }
                profileIdentityKeys[profile.ProfileId] = profileKey;
                if (!profiles.ContainsKey(profileKey))
                    profiles.Add(profileKey, profile);
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
            }
        }

        static AnimationBlendNodePayload[] CompileBlendNodes(
            CharacterPoseGraphData graph,
            CharacterAnimationRigDefinition rig,
            IReadOnlyList<CharacterPresentationProducerEntry> producers,
            AnimationBlendCatalogCompilation catalogs,
            List<string> errors)
        {
            if (graph == null || !rig || catalogs == null)
                return Array.Empty<AnimationBlendNodePayload>();
            List<CompiledBlendAuthoringNode> authoredNodes = CollectBlendAuthoringNodes(graph)
                .OrderBy(value => value.NodeId)
                .ToList();
            var result = new AnimationBlendNodePayload[authoredNodes.Count];
            for (int nodeIndex = 0; nodeIndex < authoredNodes.Count; nodeIndex++)
            {
                CompiledBlendAuthoringNode authored = authoredNodes[nodeIndex];
                CharacterAnimationBlendPolicy policy = authored.Node.BlendPolicy;
                try
                {
                    policy.RequireValid(rig);
                }
                catch (Exception exception)
                {
                    errors?.Add(exception.Message);
                    continue;
                }

                CharacterPresentationProducerEntry[] nodeProducers = producers
                    .Where(value => value != null && value.Kind == CharacterPresentationProducerKind.Animation &&
                                    value.AnimationChannelId == authored.Selection.ChannelId &&
                                    (string.IsNullOrEmpty(authored.Selection.ProgramProducerId) ||
                                     string.Equals(value.ProgramProducerIdentity, authored.Selection.ProgramProducerId, StringComparison.Ordinal)))
                    .OrderBy(value => value.ProgramProducerIndex)
                    .ToArray();
                var identities = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < nodeProducers.Length; i++)
                {
                    CharacterPresentationProducerEntry producer = nodeProducers[i];
                    if (!identities.TryAdd(producer.ProgramProducerIdentity, producer.ProgramProducerIndex))
                        errors?.Add($"Blend Stack '{authored.NodeId}' duplicates producer identity '{producer.ProgramProducerIdentity}'.");
                }
                if (nodeProducers.Length == 0)
                    errors?.Add($"Blend Stack '{authored.NodeId}' has no reachable producer on Animation Channel '{authored.Selection.ChannelId}'.");
                ValidateTransitionOverrides(policy, authored, identities, errors);

                var transitions = new List<AnimationBlendTransitionPayload>();
                for (int target = 0; target < nodeProducers.Length; target++)
                {
                    CharacterPresentationProducerEntry targetProducer = nodeProducers[target];
                    transitions.Add(CompileTransition(
                        policy,
                        -1,
                        true,
                        string.Empty,
                        targetProducer.ProgramProducerIndex,
                        false,
                        targetProducer.ProgramProducerIdentity,
                        authored.Selection.Availability,
                        catalogs));
                }
                for (int source = 0; source < nodeProducers.Length; source++)
                {
                    CharacterPresentationProducerEntry sourceProducer = nodeProducers[source];
                    for (int target = 0; target < nodeProducers.Length; target++)
                    {
                        CharacterPresentationProducerEntry targetProducer = nodeProducers[target];
                        transitions.Add(CompileTransition(
                            policy,
                            sourceProducer.ProgramProducerIndex,
                            false,
                            sourceProducer.ProgramProducerIdentity,
                            targetProducer.ProgramProducerIndex,
                            false,
                            targetProducer.ProgramProducerIdentity,
                            authored.Selection.Availability,
                            catalogs));
                    }
                    if (authored.Selection.Availability == AnimationSelectionAvailabilityPolicy.AllowEmpty)
                    {
                        transitions.Add(CompileTransition(
                            policy,
                            sourceProducer.ProgramProducerIndex,
                            false,
                            sourceProducer.ProgramProducerIdentity,
                            -1,
                            true,
                            string.Empty,
                            authored.Selection.Availability,
                            catalogs));
                    }
                }

                result[nodeIndex] = new AnimationBlendNodePayload(
                    authored.NodeId,
                    policy.PolicyId,
                    policy.Revision,
                    new AnimationBlendStackPolicyPayload(policy.StackPolicy),
                    transitions.ToArray());
            }
            return result;
        }

        static void CollectInertialRule(
            CharacterPoseInertializationRule rule,
            CharacterAnimationRigDefinition rig,
            SortedDictionary<string, AnimationBlendCurvePayload> curves,
            SortedDictionary<string, AnimationBlendProfilePayload> profiles,
            Dictionary<string, string> profileIdentityKeys,
            List<string> errors)
        {
            if (rule == null || rule.Mode == PoseInertializationMode.HardCut)
                return;
            try
            {
                rule.RequireValid(rig);
                AnimationBlendCurvePayload curve = rule.Curve.Compile();
                string curveKey = AnimationBlendCanonicalPayload.CurveKey(curve);
                if (!curves.ContainsKey(curveKey))
                    curves.Add(curveKey, curve);
                var profile = new AnimationBlendProfilePayload(rule.BlendProfile, rig);
                string profileKey = AnimationBlendCanonicalPayload.ProfileKey(profile);
                if (profileIdentityKeys.TryGetValue(profile.ProfileId, out string existingKey) &&
                    !string.Equals(existingKey, profileKey, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Animation Blend Profile identity '{profile.ProfileId}' resolves to multiple canonical payloads.");
                profileIdentityKeys[profile.ProfileId] = profileKey;
                if (!profiles.ContainsKey(profileKey))
                    profiles.Add(profileKey, profile);
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
            }
        }

        static List<CharacterPoseInertializationPolicy> CollectInertializationPolicies(CharacterPoseGraphData graph)
        {
            var result = new List<CharacterPoseInertializationPolicy>();
            CollectInertializationPolicies(graph, result);
            return result;
        }

        static void CollectInertializationPolicies(
            CharacterPoseGraphData graph,
            List<CharacterPoseInertializationPolicy> result)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterPoseNodeDefinition node = graph.Nodes[i];
                if (node.Kind == CharacterPoseNodeKind.Inertialization && node.InertializationPolicy)
                    result.Add(node.InertializationPolicy);
                if (node.Kind != CharacterPoseNodeKind.PoseSubgraph || node.Subgraph == null || !node.Subgraph.IsExclusive)
                    continue;
                CharacterPoseGraphData child = node.Subgraph.HasInline ? node.Subgraph.Inline : node.Subgraph.Shared.Graph;
                CollectInertializationPolicies(child, result);
            }
        }

        static void ValidateTransitionOverrides(
            CharacterAnimationBlendPolicy policy,
            CompiledBlendAuthoringNode authored,
            IReadOnlyDictionary<string, int> producerIdentities,
            List<string> errors)
        {
            for (int i = 0; i < policy.Overrides.Count; i++)
            {
                CharacterAnimationBlendTransitionOverride transition = policy.Overrides[i];
                if (transition == null)
                    continue;
                if (transition.SourceEmpty && transition.TargetEmpty)
                {
                    errors?.Add($"Blend Stack '{authored.NodeId}' transition override #{i} cannot target Empty from Empty.");
                    continue;
                }
                if (!transition.SourceEmpty && !producerIdentities.ContainsKey(transition.SourceProducerIdentity))
                    errors?.Add($"Blend Stack '{authored.NodeId}' transition override #{i} references source producer '{transition.SourceProducerIdentity}' outside its Selection endpoint.");
                if (!transition.TargetEmpty && !producerIdentities.ContainsKey(transition.TargetProducerIdentity))
                    errors?.Add($"Blend Stack '{authored.NodeId}' transition override #{i} references target producer '{transition.TargetProducerIdentity}' outside its Selection endpoint.");
                if (transition.TargetEmpty && authored.Selection.Availability != AnimationSelectionAvailabilityPolicy.AllowEmpty)
                    errors?.Add($"Blend Stack '{authored.NodeId}' transition override #{i} targets Empty while Selection is required.");
            }
        }

        static AnimationBlendTransitionPayload CompileTransition(
            CharacterAnimationBlendPolicy policy,
            int sourceProducerIndex,
            bool sourceEmpty,
            string sourceProducerIdentity,
            int targetProducerIndex,
            bool targetEmpty,
            string targetProducerIdentity,
            AnimationSelectionAvailabilityPolicy outputPolicy,
            AnimationBlendCatalogCompilation catalogs)
        {
            CharacterAnimationBlendTransitionRule rule = policy.DefaultTransition;
            for (int i = 0; i < policy.Overrides.Count; i++)
            {
                CharacterAnimationBlendTransitionOverride candidate = policy.Overrides[i];
                if (candidate != null && candidate.SourceEmpty == sourceEmpty && candidate.TargetEmpty == targetEmpty &&
                    string.Equals(candidate.SourceProducerIdentity, sourceProducerIdentity, StringComparison.Ordinal) &&
                    string.Equals(candidate.TargetProducerIdentity, targetProducerIdentity, StringComparison.Ordinal))
                {
                    rule = candidate.Rule;
                    break;
                }
            }
            string curveKey = AnimationBlendCanonicalPayload.CurveKey(rule.Curve.Compile());
            float durationSeconds = outputPolicy == AnimationSelectionAvailabilityPolicy.RequireSelection && sourceEmpty && !targetEmpty
                ? 0f
                : rule.DurationSeconds;
            return new AnimationBlendTransitionPayload(
                sourceProducerIndex,
                sourceEmpty,
                targetProducerIndex,
                targetEmpty,
                durationSeconds,
                catalogs.CurveIndices[curveKey],
                catalogs.ProfileIndicesByIdentity[rule.BlendProfile.ProfileId]);
        }

        static List<CompiledBlendAuthoringNode> CollectBlendAuthoringNodes(CharacterPoseGraphData root)
        {
            var result = new List<CompiledBlendAuthoringNode>();
            CollectBlendAuthoringNodes(
                root,
                string.Empty,
                new Dictionary<PoseInterfacePortId, SelectionEndpoint>(),
                result);
            var identities = new HashSet<PoseNodeId>();
            for (int i = 0; i < result.Count; i++)
            {
                if (!identities.Add(result[i].NodeId))
                    throw new InvalidOperationException($"Pose Graph duplicates compiled Blend Stack '{result[i].NodeId}'.");
            }
            return result;
        }

        static Dictionary<PoseInterfacePortId, SelectionEndpoint> CollectBlendAuthoringNodes(
            CharacterPoseGraphData graph,
            string scope,
            IReadOnlyDictionary<PoseInterfacePortId, SelectionEndpoint> imports,
            List<CompiledBlendAuthoringNode> result)
        {
            Dictionary<string, CharacterPoseEdge> incoming = graph.Edges.ToDictionary(
                edge => edge.TargetNodeId.Value + "\0" + edge.TargetPortId.Value,
                edge => edge,
                StringComparer.Ordinal);
            var values = new Dictionary<string, SelectionEndpoint>(StringComparer.Ordinal);
            var exports = new Dictionary<PoseInterfacePortId, SelectionEndpoint>();
            List<CharacterPoseNodeDefinition> ordered = TopologicalPoseNodes(graph);
            for (int nodeIndex = 0; nodeIndex < ordered.Count; nodeIndex++)
            {
                CharacterPoseNodeDefinition node = ordered[nodeIndex];
                if (node.Kind == CharacterPoseNodeKind.AnimationSelectionInput ||
                    node.Kind == CharacterPoseNodeKind.MotionMatchingSelectionInput)
                {
                    var endpoint = new SelectionEndpoint(
                        node.AnimationChannelId,
                        node.Kind == CharacterPoseNodeKind.MotionMatchingSelectionInput ? node.ProgramProducerId : string.Empty,
                        node.SelectionAvailability);
                    BindSelectionOutputs(node, scope, endpoint, values);
                    continue;
                }
                if (node.Kind == CharacterPoseNodeKind.GraphInput)
                {
                    for (int i = 0; i < node.Ports.Count; i++)
                    {
                        CharacterPosePortDefinition port = node.Ports[i];
                        if (port != null && port.Kind == CharacterPosePortKind.AnimationSelection &&
                            port.Direction == CharacterPosePortDirection.Output && imports.TryGetValue(port.InterfacePortId, out SelectionEndpoint endpoint))
                            values.Add(ScopedEndpoint(node.NodeId, port.PortId, scope), endpoint);
                    }
                    continue;
                }
                if (node.Kind == CharacterPoseNodeKind.GraphOutput)
                {
                    for (int i = 0; i < node.Ports.Count; i++)
                    {
                        CharacterPosePortDefinition port = node.Ports[i];
                        if (port != null && port.Kind == CharacterPosePortKind.AnimationSelection &&
                            port.Direction == CharacterPosePortDirection.Input &&
                            TryResolveSelection(node, port, incoming, scope, values, out SelectionEndpoint endpoint))
                            exports.Add(port.InterfacePortId, endpoint);
                    }
                    continue;
                }
                if (node.Kind == CharacterPoseNodeKind.PoseSubgraph)
                {
                    var childImports = new Dictionary<PoseInterfacePortId, SelectionEndpoint>();
                    for (int i = 0; i < node.Ports.Count; i++)
                    {
                        CharacterPosePortDefinition port = node.Ports[i];
                        if (port != null && port.Kind == CharacterPosePortKind.AnimationSelection &&
                            port.Direction == CharacterPosePortDirection.Input &&
                            TryResolveSelection(node, port, incoming, scope, values, out SelectionEndpoint endpoint))
                            childImports.Add(port.InterfacePortId, endpoint);
                    }
                    CharacterPoseGraphData child = node.Subgraph.HasInline ? node.Subgraph.Inline : node.Subgraph.Shared.Graph;
                    PoseNodeId callSite = ScopePoseNodeId(node.NodeId, scope);
                    string childScope = callSite.Value + "/" + child.GraphId;
                    Dictionary<PoseInterfacePortId, SelectionEndpoint> childExports =
                        CollectBlendAuthoringNodes(child, childScope, childImports, result);
                    for (int i = 0; i < node.Ports.Count; i++)
                    {
                        CharacterPosePortDefinition port = node.Ports[i];
                        if (port != null && port.Kind == CharacterPosePortKind.AnimationSelection &&
                            port.Direction == CharacterPosePortDirection.Output && childExports.TryGetValue(port.InterfacePortId, out SelectionEndpoint endpoint))
                            values.Add(ScopedEndpoint(node.NodeId, port.PortId, scope), endpoint);
                    }
                    continue;
                }
                if (node.Kind != CharacterPoseNodeKind.BlendStack)
                    continue;
                CharacterPosePortDefinition selectionPort = node.Ports.Single(port =>
                    port.Kind == CharacterPosePortKind.AnimationSelection && port.Direction == CharacterPosePortDirection.Input);
                if (!TryResolveSelection(node, selectionPort, incoming, scope, values, out SelectionEndpoint selection))
                    throw new InvalidOperationException($"Blend Stack '{node.NodeId}' has no resolvable Selection endpoint.");
                result.Add(new CompiledBlendAuthoringNode(ScopePoseNodeId(node.NodeId, scope), node, selection));
            }
            return exports;
        }

        static void BindSelectionOutputs(
            CharacterPoseNodeDefinition node,
            string scope,
            SelectionEndpoint endpoint,
            Dictionary<string, SelectionEndpoint> values)
        {
            for (int i = 0; i < node.Ports.Count; i++)
            {
                CharacterPosePortDefinition port = node.Ports[i];
                if (port != null && port.Kind == CharacterPosePortKind.AnimationSelection && port.Direction == CharacterPosePortDirection.Output)
                    values.Add(ScopedEndpoint(node.NodeId, port.PortId, scope), endpoint);
            }
        }

        static bool TryResolveSelection(
            CharacterPoseNodeDefinition node,
            CharacterPosePortDefinition port,
            IReadOnlyDictionary<string, CharacterPoseEdge> incoming,
            string scope,
            IReadOnlyDictionary<string, SelectionEndpoint> values,
            out SelectionEndpoint endpoint)
        {
            endpoint = default;
            return incoming.TryGetValue(node.NodeId.Value + "\0" + port.PortId.Value, out CharacterPoseEdge edge) &&
                   values.TryGetValue(ScopedEndpoint(edge.SourceNodeId, edge.SourcePortId, scope), out endpoint);
        }

        static List<CharacterPoseNodeDefinition> TopologicalPoseNodes(CharacterPoseGraphData graph)
        {
            var nodes = graph.Nodes.ToDictionary(node => node.NodeId);
            var indegree = nodes.Keys.ToDictionary(node => node, _ => 0);
            var outgoing = new Dictionary<PoseNodeId, List<PoseNodeId>>();
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                CharacterPoseEdge edge = graph.Edges[i];
                indegree[edge.TargetNodeId]++;
                if (!outgoing.TryGetValue(edge.SourceNodeId, out List<PoseNodeId> targets))
                {
                    targets = new List<PoseNodeId>();
                    outgoing.Add(edge.SourceNodeId, targets);
                }
                targets.Add(edge.TargetNodeId);
            }
            var ready = new SortedSet<PoseNodeId>(indegree.Where(pair => pair.Value == 0).Select(pair => pair.Key));
            var result = new List<CharacterPoseNodeDefinition>(nodes.Count);
            while (ready.Count > 0)
            {
                PoseNodeId current = ready.Min;
                ready.Remove(current);
                result.Add(nodes[current]);
                if (!outgoing.TryGetValue(current, out List<PoseNodeId> targets))
                    continue;
                targets.Sort();
                for (int i = 0; i < targets.Count; i++)
                {
                    if (--indegree[targets[i]] == 0)
                        ready.Add(targets[i]);
                }
            }
            if (result.Count != nodes.Count)
                throw new InvalidOperationException($"Pose Graph '{graph.GraphId}' contains a cycle.");
            return result;
        }

        static string ScopedEndpoint(PoseNodeId nodeId, PosePortId portId, string scope) =>
            ScopePoseNodeId(nodeId, scope).Value + "\0" +
            (string.IsNullOrEmpty(scope) ? portId.Value : scope + "/" + portId.Value);

        static PoseNodeId ScopePoseNodeId(PoseNodeId nodeId, string scope) =>
            string.IsNullOrEmpty(scope) ? nodeId : new PoseNodeId(scope + "/" + nodeId.Value);

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

        static bool ValidateMarkerSyncAuthoring(
            IReadOnlyList<ProgramProducer> producers,
            CharacterAnimationPresentationProfile profile,
            IReadOnlyDictionary<string, TimelineData> timelines,
            IReadOnlyDictionary<string, IReadOnlyList<AnimationMarkerSyncCallSite>> callSites,
            List<string> errors)
        {
            var inputs = new List<AnimationMarkerSyncAuthoringInput>();
            for (int producerIndex = 0; producerIndex < producers.Count; producerIndex++)
            {
                ProgramProducer producer = producers[producerIndex];
                if (!TryParseAnimationSource(producer.SourceIdentity, out AnimationProducerId producerId) ||
                    !timelines.TryGetValue(producerId.TimelineAuthoringId, out TimelineData timeline))
                    continue;
                AnimationProducerPresentationBinding presentationBinding = profile.FindProducerBinding(producerId);
                if (presentationBinding == null || presentationBinding.SourceKind != AnimationPoseSourceKind.Timeline)
                    continue;
                AnimationTrack track = null;
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    if (timeline.Tracks[trackIndex] is AnimationTrack candidate &&
                        string.Equals(candidate.AuthoringId, producerId.TrackAuthoringId, StringComparison.Ordinal))
                    {
                        track = candidate;
                        break;
                    }
                }
                if (track == null)
                    continue;
                callSites.TryGetValue(producerId.TimelineAuthoringId, out IReadOnlyList<AnimationMarkerSyncCallSite> producerCallSites);
                inputs.Add(new AnimationMarkerSyncAuthoringInput(
                    producer.Identity,
                    timeline,
                    track,
                    producerCallSites ?? Array.Empty<AnimationMarkerSyncCallSite>()));
            }
            var issues = new List<AnimationMarkerSyncAuthoringIssue>();
            AnimationMarkerSyncAuthoring.Validate(inputs, issues);
            for (int i = 0; i < issues.Count; i++)
            {
                AnimationMarkerSyncAuthoringIssue issue = issues[i];
                errors?.Add($"{issue.Code} [{issue.AuthoringPath}]: {issue.Message}");
            }
            return issues.Count == 0;
        }

        static AnimationMarkerSyncBinding CompileMarkerSync(AnimationTrack track, TimelineData timeline)
        {
            if (track.SyncMode == AnimationSyncMode.None)
                return new AnimationMarkerSyncBinding();
            if (track.SyncMode != AnimationSyncMode.MarkerGroup)
                throw new InvalidOperationException($"AnimationTrack '{track.AuthoringId}' has not been migrated to a publishable sync mode.");
            var markers = new AnimationMarkerSyncMarkerBinding[track.SyncMarkers.Count];
            for (int i = 0; i < track.SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = track.SyncMarkers[i];
                markers[i] = new AnimationMarkerSyncMarkerBinding(
                    marker.AuthoringId,
                    AnimationMarkerSyncAuthoring.NormalizeId(marker.MarkerId),
                    marker.Frame,
                    marker.Frame / (float)TimelineUtility.FrameRate);
            }
            int segmentCount = Math.Max(0, markers.Length - 1) +
                               (track.SequenceTopology == AnimationMarkerSequenceTopology.Cyclic ? 1 : 0);
            var segments = new AnimationMarkerSyncSegmentOccurrence[segmentCount];
            int segmentIndex = 0;
            for (int i = 1; i < markers.Length; i++)
            {
                AnimationMarkerSyncMarkerBinding previous = markers[i - 1];
                AnimationMarkerSyncMarkerBinding next = markers[i];
                segments[segmentIndex] = new AnimationMarkerSyncSegmentOccurrence(
                    segmentIndex,
                    i - 1,
                    i,
                    previous.MarkerId,
                    next.MarkerId,
                    previous.TimeSeconds,
                    next.TimeSeconds,
                    false);
                segmentIndex++;
            }
            if (track.SequenceTopology == AnimationMarkerSequenceTopology.Cyclic)
            {
                AnimationMarkerSyncMarkerBinding previous = markers[markers.Length - 1];
                AnimationMarkerSyncMarkerBinding next = markers[0];
                segments[segmentIndex] = new AnimationMarkerSyncSegmentOccurrence(
                    segmentIndex,
                    markers.Length - 1,
                    0,
                    previous.MarkerId,
                    next.MarkerId,
                    previous.TimeSeconds,
                    timeline.Duration + next.TimeSeconds,
                    true);
            }
            return new AnimationMarkerSyncBinding(
                AnimationSyncMode.MarkerGroup,
                AnimationMarkerSyncAuthoring.NormalizeId(track.SyncGroupId),
                track.SequenceTopology,
                track.SyncRole,
                timeline.MaxFrame,
                timeline.Duration,
                markers,
                segments);
        }

        static IReadOnlyDictionary<string, IReadOnlyList<AnimationMarkerSyncCallSite>> CollectAnimationMarkerSyncCallSites(
            CharacterAuthoringGraphOccurrence root)
        {
            var mutable = new Dictionary<string, List<AnimationMarkerSyncCallSite>>(StringComparer.Ordinal);
            CollectAnimationMarkerSyncCallSites(root, mutable);
            var result = new Dictionary<string, IReadOnlyList<AnimationMarkerSyncCallSite>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<AnimationMarkerSyncCallSite>> pair in mutable)
                result.Add(pair.Key, pair.Value.ToArray());
            return result;
        }

        static void CollectAnimationMarkerSyncCallSites(
            CharacterAuthoringGraphOccurrence occurrence,
            Dictionary<string, List<AnimationMarkerSyncCallSite>> result)
        {
            if (occurrence == null)
                return;
            for (int i = 0; i < occurrence.Timelines.Count; i++)
            {
                CharacterAuthoringTimelineRecord timeline = occurrence.Timelines[i];
                string timelineId = timeline.Timeline.AuthoringId;
                if (!result.TryGetValue(timelineId, out List<AnimationMarkerSyncCallSite> values))
                {
                    values = new List<AnimationMarkerSyncCallSite>();
                    result.Add(timelineId, values);
                }
                values.Add(new AnimationMarkerSyncCallSite(timeline.Route, timeline.Node.PlaybackMode));
            }
            for (int i = 0; i < occurrence.GraphReferences.Count; i++)
                CollectAnimationMarkerSyncCallSites(occurrence.GraphReferences[i].Child, result);
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
                "character-presentation-projection/v5",
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
            List<string> errors)
        {
            if (!profile || !profile.MotionMatchingProfile)
                return null;
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
                    profile.MotionMatchingProfile,
                    profile.PoseGraph.Graph,
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
