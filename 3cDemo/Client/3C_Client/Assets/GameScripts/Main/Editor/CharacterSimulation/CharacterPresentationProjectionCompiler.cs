using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
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
            string projectionRevision = ComputeProjectionRevision(
                model.AnimationPresentationProfile,
                model.Definition.EquipmentPresentationProfile,
                reader.Contract.ContractHash,
                request.FootAnalysis.RevisionTokens);
            CharacterPresentationProjection projection = CompileCore(
                reader,
                model.AnimationPresentationProfile,
                model.Definition.EquipmentProfile,
                model.Definition.EquipmentPresentationProfile,
                projectionRevision,
                request.FootAnalysis.BuildData,
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
                footAnalysisTokens);
            return true;
        }

        static CharacterPresentationProjection CompileCore(
            CharacterPresentationSemanticReader reader,
            CharacterAnimationPresentationProfile profile,
            CharacterEquipmentProfile equipmentProfile,
            CharacterEquipmentPresentationProfile equipmentPresentationProfile,
            string projectionRevision,
            AnimationFootAnalysisProjectionBuildData footAnalysis,
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
            if (!ValidateMarkerSyncAuthoring(reader.Producers, timelines, markerSyncCallSites, errors))
                return null;

            var entries = new List<CharacterPresentationProducerEntry>();
            var animationIds = new HashSet<AnimationProducerId>();
            for (int i = 0; i < reader.Producers.Count; i++)
            {
                CharacterPresentationProducerEntry entry = BuildProducer(
                    reader,
                    reader.Producers[i],
                    profile,
                    footAnalysis,
                    timelines,
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
            int contributionCapacityPerValue = ComputeContributionCapacity(profile.BlendLibrary);
            CharacterPresentationPoseProgram poseProgram = CharacterPresentationPoseGraphCompiler.Compile(
                profile.PoseGraph,
                profile.RigDefinition,
                animationChannels,
                contributionCapacityPerValue,
                errors);
            AnimationBlendCatalogCompilation blendCatalogs = CompileBlendCatalogs(
                profile.BlendLibrary,
                profile.RigDefinition,
                errors);
            AnimationBlendSlotPayload[] blendSlots = poseProgram == null
                ? Array.Empty<AnimationBlendSlotPayload>()
                : CompileBlendSlots(profile.BlendLibrary, profile.RigDefinition, poseProgram, entries, blendCatalogs, errors);
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
                blendSlots,
                blendCatalogs.CurveCatalog,
                blendCatalogs.ProfileCatalog,
                rig,
                entries.ToArray(),
                footIdentity,
                projectionRevision,
                visualBindings);
        }

        static CharacterPresentationProducerEntry BuildProducer(
            CharacterPresentationSemanticReader reader,
            ProgramProducer producer,
            CharacterAnimationPresentationProfile profile,
            AnimationFootAnalysisProjectionBuildData footAnalysis,
            IReadOnlyDictionary<string, TimelineData> timelines,
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
            if (authoringBinding == null || !authoringBinding.Source || !authoringBinding.Source.IsValid)
            {
                errors?.Add($"Animation producer '{producerId}' has no valid Animancer source binding.");
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
                CompileMarkerSync(track, timeline));
            return new CharacterPresentationProducerEntry(
                producer.Index,
                producer.Identity,
                producer.SourceIdentity,
                producer.ChannelKind,
                kind.Value,
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

        static int ComputeContributionCapacity(CharacterAnimationBlendLibrary library)
        {
            if (!library)
                return 0;
            int capacity = 0;
            for (int i = 0; i < library.Slots.Count; i++)
            {
                CharacterAnimationBlendSlotDefinition slot = library.Slots[i];
                if (slot?.StackPolicy == null)
                    continue;
                capacity = checked(capacity + slot.StackPolicy.MaxActiveSourceEntries);
            }
            return capacity;
        }

        static AnimationBlendCatalogCompilation CompileBlendCatalogs(
            CharacterAnimationBlendLibrary library,
            CharacterAnimationRigDefinition rig,
            List<string> errors)
        {
            if (!library || !rig)
                return null;
            var curves = new SortedDictionary<string, AnimationBlendCurvePayload>(StringComparer.Ordinal);
            var profiles = new SortedDictionary<string, AnimationBlendProfilePayload>(StringComparer.Ordinal);
            var profileIdentityKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int slotIndex = 0; slotIndex < library.Slots.Count; slotIndex++)
            {
                CharacterAnimationBlendSlotDefinition slot = library.Slots[slotIndex];
                if (slot == null)
                    continue;
                CollectBlendRule(slot.DefaultTransition, rig, curves, profiles, profileIdentityKeys, errors);
                for (int transitionIndex = 0; transitionIndex < slot.Overrides.Count; transitionIndex++)
                    CollectBlendRule(slot.Overrides[transitionIndex]?.Rule, rig, curves, profiles, profileIdentityKeys, errors);
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

        static AnimationBlendSlotPayload[] CompileBlendSlots(
            CharacterAnimationBlendLibrary library,
            CharacterAnimationRigDefinition rig,
            CharacterPresentationPoseProgram poseProgram,
            IReadOnlyList<CharacterPresentationProducerEntry> producers,
            AnimationBlendCatalogCompilation catalogs,
            List<string> errors)
        {
            if (!library || !rig || poseProgram == null || catalogs == null)
                return Array.Empty<AnimationBlendSlotPayload>();
            var result = new AnimationBlendSlotPayload[poseProgram.Slots.Count];
            for (int slotIndex = 0; slotIndex < poseProgram.Slots.Count; slotIndex++)
            {
                CharacterPresentationPoseSlotProgramEntry programSlot = poseProgram.Slots[slotIndex];
                CharacterAnimationBlendSlotDefinition authoredSlot;
                try
                {
                    authoredSlot = library.RequireSlot(programSlot.PoseSlotId);
                    authoredSlot.RequireValid(rig);
                }
                catch (Exception exception)
                {
                    errors?.Add(exception.Message);
                    continue;
                }

                CharacterPresentationProducerEntry[] slotProducers = producers
                    .Where(value => value != null && value.Kind == CharacterPresentationProducerKind.Animation &&
                                    value.AnimationChannelId == programSlot.AnimationChannelId)
                    .OrderBy(value => value.ProgramProducerIndex)
                    .ToArray();
                var identities = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < slotProducers.Length; i++)
                {
                    CharacterPresentationProducerEntry producer = slotProducers[i];
                    if (!identities.TryAdd(producer.ProgramProducerIdentity, producer.ProgramProducerIndex))
                    {
                        errors?.Add($"Pose Slot '{programSlot.PoseSlotId}' duplicates producer identity '{producer.ProgramProducerIdentity}'.");
                    }
                }
                if (slotProducers.Length == 0)
                    errors?.Add($"Pose Slot '{programSlot.PoseSlotId}' has no reachable producer on Animation Channel '{programSlot.AnimationChannelId}'.");
                ValidateTransitionOverrides(authoredSlot, programSlot, identities, errors);

                var transitions = new List<AnimationBlendTransitionPayload>();
                for (int target = 0; target < slotProducers.Length; target++)
                {
                    CharacterPresentationProducerEntry targetProducer = slotProducers[target];
                    transitions.Add(CompileTransition(
                        authoredSlot,
                        -1,
                        true,
                        string.Empty,
                        targetProducer.ProgramProducerIndex,
                        false,
                        targetProducer.ProgramProducerIdentity,
                        programSlot.OutputPolicy,
                        catalogs));
                }
                for (int source = 0; source < slotProducers.Length; source++)
                {
                    CharacterPresentationProducerEntry sourceProducer = slotProducers[source];
                    for (int target = 0; target < slotProducers.Length; target++)
                    {
                        CharacterPresentationProducerEntry targetProducer = slotProducers[target];
                        transitions.Add(CompileTransition(
                            authoredSlot,
                            sourceProducer.ProgramProducerIndex,
                            false,
                            sourceProducer.ProgramProducerIdentity,
                            targetProducer.ProgramProducerIndex,
                            false,
                            targetProducer.ProgramProducerIdentity,
                            programSlot.OutputPolicy,
                            catalogs));
                    }
                    if (programSlot.OutputPolicy == PoseSlotOutputPolicy.AllowEmpty)
                    {
                        transitions.Add(CompileTransition(
                            authoredSlot,
                            sourceProducer.ProgramProducerIndex,
                            false,
                            sourceProducer.ProgramProducerIdentity,
                            -1,
                            true,
                            string.Empty,
                            programSlot.OutputPolicy,
                            catalogs));
                    }
                }

                result[slotIndex] = new AnimationBlendSlotPayload(
                    programSlot.PoseSlotId,
                    programSlot.AnimationChannelId,
                    programSlot.OutputPolicy,
                    new AnimationBlendStackPolicyPayload(authoredSlot.StackPolicy),
                    transitions.ToArray());
            }
            return result;
        }

        static void ValidateTransitionOverrides(
            CharacterAnimationBlendSlotDefinition authoredSlot,
            CharacterPresentationPoseSlotProgramEntry programSlot,
            IReadOnlyDictionary<string, int> producerIdentities,
            List<string> errors)
        {
            for (int i = 0; i < authoredSlot.Overrides.Count; i++)
            {
                CharacterAnimationBlendTransitionOverride transition = authoredSlot.Overrides[i];
                if (transition == null)
                    continue;
                if (transition.SourceEmpty && transition.TargetEmpty)
                {
                    errors?.Add($"Pose Slot '{programSlot.PoseSlotId}' transition override #{i} cannot target Empty from Empty.");
                    continue;
                }
                if (!transition.SourceEmpty && !producerIdentities.ContainsKey(transition.SourceProducerIdentity))
                {
                    errors?.Add($"Pose Slot '{programSlot.PoseSlotId}' transition override #{i} references source producer '{transition.SourceProducerIdentity}' outside Animation Channel '{programSlot.AnimationChannelId}'.");
                }
                if (!transition.TargetEmpty && !producerIdentities.ContainsKey(transition.TargetProducerIdentity))
                {
                    errors?.Add($"Pose Slot '{programSlot.PoseSlotId}' transition override #{i} references target producer '{transition.TargetProducerIdentity}' outside Animation Channel '{programSlot.AnimationChannelId}'.");
                }
                if (transition.TargetEmpty && programSlot.OutputPolicy != PoseSlotOutputPolicy.AllowEmpty)
                {
                    errors?.Add($"Pose Slot '{programSlot.PoseSlotId}' transition override #{i} targets Empty while output policy is RequireOutput.");
                }
            }
        }

        static AnimationBlendTransitionPayload CompileTransition(
            CharacterAnimationBlendSlotDefinition slot,
            int sourceProducerIndex,
            bool sourceEmpty,
            string sourceProducerIdentity,
            int targetProducerIndex,
            bool targetEmpty,
            string targetProducerIdentity,
            PoseSlotOutputPolicy outputPolicy,
            AnimationBlendCatalogCompilation catalogs)
        {
            CharacterAnimationBlendTransitionRule rule = slot.DefaultTransition;
            for (int i = 0; i < slot.Overrides.Count; i++)
            {
                CharacterAnimationBlendTransitionOverride candidate = slot.Overrides[i];
                if (candidate != null && candidate.SourceEmpty == sourceEmpty && candidate.TargetEmpty == targetEmpty &&
                    string.Equals(candidate.SourceProducerIdentity, sourceProducerIdentity, StringComparison.Ordinal) &&
                    string.Equals(candidate.TargetProducerIdentity, targetProducerIdentity, StringComparison.Ordinal))
                {
                    rule = candidate.Rule;
                    break;
                }
            }
            string curveKey = AnimationBlendCanonicalPayload.CurveKey(rule.Curve.Compile());
            float durationSeconds = outputPolicy == PoseSlotOutputPolicy.RequireOutput && sourceEmpty && !targetEmpty
                ? 0f
                : rule.DurationSeconds;
            return new AnimationBlendTransitionPayload(
                sourceProducerIndex,
                sourceEmpty,
                targetProducerIndex,
                targetEmpty,
                rule.Technique,
                durationSeconds,
                catalogs.CurveIndices[curveKey],
                catalogs.ProfileIndicesByIdentity[rule.BlendProfile.ProfileId]);
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

        static bool ValidateMarkerSyncAuthoring(
            IReadOnlyList<ProgramProducer> producers,
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
            IReadOnlyList<string> footAnalysisTokens)
        {
            var values = new List<string>
            {
                "character-presentation-projection/v3",
                contractHash.ToString()
            };
            AddProjectionAssetRevision(animationProfile, values);
            AddProjectionAssetRevision(equipmentPresentationProfile, values);
            if (footAnalysisTokens != null)
            {
                for (int i = 0; i < footAnalysisTokens.Count; i++)
                    values.Add(footAnalysisTokens[i]);
            }
            return StableHash.Compute(values.ToArray()).ToString();
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
