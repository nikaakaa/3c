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

        sealed class PoseSourceCompilationEntry
        {
            public PoseSourceCompilationEntry(
                PresentationPoseSourceIndex sourceIndex,
                CharacterPresentationPoseSourceSlot slot,
                CharacterPresentationPoseSourceBinding binding)
            {
                SourceIndex = sourceIndex;
                Slot = slot;
                Binding = binding;
            }

            public PresentationPoseSourceIndex SourceIndex { get; }
            public CharacterPresentationPoseSourceSlot Slot { get; }
            public CharacterPresentationPoseSourceBinding Binding { get; }
        }

        sealed class PoseSourceCompilationCatalog
        {
            readonly Dictionary<CharacterPresentationPoseSourceSlot, PoseSourceCompilationEntry> m_BySlot;

            public PoseSourceCompilationCatalog(PoseSourceCompilationEntry[] entries)
            {
                Entries = entries ?? Array.Empty<PoseSourceCompilationEntry>();
                m_BySlot = Entries.ToDictionary(value => value.Slot);
            }

            public IReadOnlyList<PoseSourceCompilationEntry> Entries { get; }
            public IReadOnlyDictionary<CharacterPresentationPoseSourceSlot, PresentationPoseSourceIndex> SourceIndices =>
                m_BySlot.ToDictionary(value => value.Key, value => value.Value.SourceIndex);

            public bool TryGet(
                CharacterPresentationPoseSourceSlot slot,
                out PoseSourceCompilationEntry entry) =>
                m_BySlot.TryGetValue(slot, out entry);
        }

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
            PoseSourceCompilationCatalog sourceCatalog = CompilePoseSourceCatalog(
                model.AnimationPresentationProfile,
                errors);
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

        static PoseSourceCompilationCatalog CompilePoseSourceCatalog(
            CharacterAnimationPresentationProfile profile,
            List<string> errors)
        {
            if (!profile || !profile.PoseGraph)
            {
                errors?.Add("Presentation Profile has no Pose Graph for Pose Source compilation.");
                return new PoseSourceCompilationCatalog(Array.Empty<PoseSourceCompilationEntry>());
            }

            string profilePath = AssetDatabase.GetAssetPath(profile);
            var ownedSlots = new HashSet<CharacterPresentationPoseSourceSlot>();
            CharacterPresentationPoseGraphAsset[] graphOwners = EnumeratePoseGraphOwners(profile)
                .Distinct()
                .OrderBy(CharacterPresentationAssetObjectIdentity.Require, StringComparer.Ordinal)
                .ToArray();
            for (int ownerIndex = 0; ownerIndex < graphOwners.Length; ownerIndex++)
            {
                CharacterPresentationPoseGraphAsset graphOwner = graphOwners[ownerIndex];
                string graphPath = AssetDatabase.GetAssetPath(graphOwner);
                for (int slotIndex = 0; slotIndex < graphOwner.SourceSlots.Count; slotIndex++)
                {
                    CharacterPresentationPoseSourceSlot slot = graphOwner.SourceSlots[slotIndex];
                    if (!slot || !ownedSlots.Add(slot) ||
                        !string.Equals(AssetDatabase.GetAssetPath(slot), graphPath, StringComparison.Ordinal))
                    {
                        errors?.Add($"Pose Graph '{graphOwner.name}' Source Slot #{slotIndex} is missing, duplicated or not owned by that asset.");
                        continue;
                    }
                    try
                    {
                        slot.RequireValid();
                    }
                    catch (Exception exception)
                    {
                        errors?.Add($"Pose Graph Source Slot '{slot.name}' is invalid: {exception.Message}");
                    }
                }
            }

            CharacterPresentationPoseSourceSlot[] reachable = EnumerateReachablePoseGraphs(profile)
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .Where(value => value != null && value.PresentationPoseSourceSlot)
                .Select(value => value.PresentationPoseSourceSlot)
                .Distinct()
                .ToArray();
            Array.Sort(reachable, (left, right) =>
                string.CompareOrdinal(
                    CharacterPresentationAssetObjectIdentity.Require(left),
                    CharacterPresentationAssetObjectIdentity.Require(right)));

            var bindingsBySlot = new Dictionary<CharacterPresentationPoseSourceSlot, CharacterPresentationPoseSourceBinding>();
            for (int i = 0; i < profile.PoseSourceBindings.Count; i++)
            {
                CharacterPresentationPoseSourceBinding binding = profile.PoseSourceBindings[i];
                if (!binding || !binding.Slot ||
                    !string.Equals(AssetDatabase.GetAssetPath(binding), profilePath, StringComparison.Ordinal) ||
                    !bindingsBySlot.TryAdd(binding.Slot, binding))
                {
                    errors?.Add($"Presentation Profile Pose Source binding #{i} is missing, duplicated or not owned by the Profile asset.");
                    continue;
                }
                if (!ownedSlots.Contains(binding.Slot) || !binding.Slot.Accepts(binding))
                    errors?.Add($"Pose Source binding '{binding.name}' references a foreign or type-incompatible Slot.");
                try
                {
                    binding.RequireValid(profile.RigDefinition);
                }
                catch (Exception exception)
                {
                    errors?.Add($"Pose Source binding '{binding.name}' is invalid: {exception.Message}");
                }
            }

            var entries = new PoseSourceCompilationEntry[reachable.Length];
            for (int i = 0; i < reachable.Length; i++)
            {
                CharacterPresentationPoseSourceSlot slot = reachable[i];
                if (!ownedSlots.Contains(slot))
                    errors?.Add($"Pose Player references Source Slot '{slot.name}' outside the Pose Graph owner.");
                if (!bindingsBySlot.TryGetValue(slot, out CharacterPresentationPoseSourceBinding binding))
                    errors?.Add($"Pose Source Slot '{slot.name}' has no Profile binding.");
                entries[i] = new PoseSourceCompilationEntry(
                    new PresentationPoseSourceIndex(i),
                    slot,
                    binding);
            }

            foreach (KeyValuePair<CharacterPresentationPoseSourceSlot, CharacterPresentationPoseSourceBinding> pair in bindingsBySlot)
            {
                if (!reachable.Contains(pair.Key))
                    errors?.Add($"Pose Source binding '{pair.Value.name}' is orphaned from every reachable Pose Player.");
            }
            return new PoseSourceCompilationCatalog(entries);
        }

        static IEnumerable<CharacterPresentationPoseGraphAsset> EnumeratePoseGraphOwners(
            CharacterAnimationPresentationProfile profile)
        {
            yield return profile.PoseGraph;
            for (int implementationIndex = 0; implementationIndex < profile.LinkedPoseImplementations.Count; implementationIndex++)
            {
                CharacterLinkedPoseImplementationAsset implementation = profile.LinkedPoseImplementations[implementationIndex];
                if (!implementation)
                    continue;
                for (int entryIndex = 0; entryIndex < implementation.Entries.Count; entryIndex++)
                {
                    CharacterPresentationPoseGraphAsset graphOwner = implementation.Entries[entryIndex]?.GraphOwner;
                    if (graphOwner)
                        yield return graphOwner;
                }
            }
        }

        static IEnumerable<CharacterTypedPoseGraph> EnumerateReachablePoseGraphs(
            CharacterAnimationPresentationProfile profile)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterTypedPoseGraph graph in EnumerateReachablePoseGraphs(profile.PoseGraph, profile.PoseGraph.Graph, visited))
                yield return graph;
            for (int implementationIndex = 0; implementationIndex < profile.LinkedPoseImplementations.Count; implementationIndex++)
            {
                CharacterLinkedPoseImplementationAsset implementation = profile.LinkedPoseImplementations[implementationIndex];
                if (!implementation)
                    continue;
                for (int entryIndex = 0; entryIndex < implementation.Entries.Count; entryIndex++)
                {
                    CharacterLinkedPoseImplementationEntryBinding entry = implementation.Entries[entryIndex];
                    if (entry == null || !entry.GraphOwner)
                        continue;
                    CharacterTypedPoseGraph entryGraph = entry.GraphOwner.RequireGraph(entry.GraphId);
                    foreach (CharacterTypedPoseGraph graph in EnumerateReachablePoseGraphs(entry.GraphOwner, entryGraph, visited))
                        yield return graph;
                }
            }
        }

        static IEnumerable<CharacterTypedPoseGraph> EnumerateReachablePoseGraphs(
            CharacterPresentationPoseGraphAsset owner,
            CharacterTypedPoseGraph graph,
            HashSet<string> visited)
        {
            string key = CharacterPresentationAssetObjectIdentity.Require(owner) + "\0" + graph.GraphId.Value;
            if (!visited.Add(key))
                yield break;
            yield return graph;
            for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
            {
                CharacterTypedPoseNode node = graph.Nodes[nodeIndex];
                if (node?.Payload is CharacterPoseSubgraphPayload subgraph && subgraph.Subgraph != null && subgraph.Subgraph.PoseGraphId.IsValid)
                {
                    foreach (CharacterTypedPoseGraph child in EnumerateReachablePoseGraphs(owner, owner.RequireGraph(subgraph.Subgraph.PoseGraphId), visited))
                        yield return child;
                    continue;
                }
                if (node?.Payload is not CharacterPoseStateMachineNodePayload stateMachine || stateMachine.StateMachine == null)
                    continue;
                for (int stateIndex = 0; stateIndex < stateMachine.StateMachine.States.Count; stateIndex++)
                {
                    CharacterPoseStateDefinition state = stateMachine.StateMachine.States[stateIndex];
                    if (state == null || !state.PoseGraphId.IsValid)
                        continue;
                    foreach (CharacterTypedPoseGraph child in EnumerateReachablePoseGraphs(owner, owner.RequireGraph(state.PoseGraphId), visited))
                        yield return child;
                }
            }
        }

        static CharacterPresentationProjection CompileCore(
            CharacterPresentationSemanticReader reader,
            CharacterAnimationPresentationProfile profile,
            CharacterEquipmentProfile equipmentProfile,
            CharacterEquipmentPresentationProfile equipmentPresentationProfile,
            string projectionRevision,
            CharacterFootPlacementAnalysisCompilation footAnalysisCompilation,
            MotionMatchingProjectionPayload motionMatching,
            PoseSourceCompilationCatalog sourceCatalog,
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
            AnimationBlendCatalogCompilation blendCatalogs = CompileBlendCatalogs(
                profile.PoseGraph,
                profile.RigDefinition,
                errors);
            AnimationBlendNodePayload[] blendNodes = blendCatalogs == null
                ? Array.Empty<AnimationBlendNodePayload>()
                : CompileBlendNodes(
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
            CharacterPresentationPosePlan poseProgram = CharacterPresentationPoseGraphCompiler.Compile(
                profile.PoseGraph,
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
                footAnalysisCompilation,
                errors);
            if (poseProgram != null && blendCatalogs != null)
            {
                poseProgram = CharacterPresentationInertializationPlanCompiler.Compile(
                    poseProgram,
                    profile.PoseGraph,
                    profile.RigDefinition,
                    blendCatalogs.CurveIndices,
                    blendCatalogs.ProfileIndicesByIdentity,
                    errors);
            }
            if (poseProgram != null && blendCatalogs != null)
            {
                try
                {
                    CharacterMotionMatchingPosePlanCompiler.Compile(
                        poseProgram,
                        profile.PoseGraph,
                        profile.RigDefinition,
                        motionMatching,
                        blendCatalogs.CurveIndices,
                        blendCatalogs.ProfileIndicesByIdentity);
                }
                catch (Exception exception)
                {
                    errors?.Add(exception.Message);
                }
            }
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
            PoseSourceCompilationCatalog sourceCatalog,
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
                PoseSourceCompilationEntry entry = sourceCatalog.Entries[bindingIndex];
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
            PoseSourceCompilationCatalog sourceCatalog,
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
                PoseSourceCompilationEntry entry = sourceCatalog.Entries[i];
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
                            CharacterAnimationClipRegisteredCurveChannels.LeftStepTime),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.LeftStepDistance),
                        sourceDurationSeconds),
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
                        clip.isLooping,
                        sourceDurationSeconds,
                        $"{clip.name}/Left")),
                new AnimationFootStepObservationCurveSet(
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightStepTime),
                        sourceDurationSeconds),
                    NormalizeRegisteredCurve(
                        CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                            clip,
                            CharacterAnimationClipRegisteredCurveChannels.RightStepDistance),
                        sourceDurationSeconds),
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
                        clip.isLooping,
                        sourceDurationSeconds,
                        $"{clip.name}/Right")));

        static AnimationFootStepLandingEventTable CompileLandingEvents(
            AnimationFootMotionDataDescriptor motionData,
            AnimationFootMotionFootPage foot,
            bool looping,
            float sourceDurationSeconds,
            string sourceLabel)
        {
            if (motionData == null || foot == null ||
                !float.IsFinite(sourceDurationSeconds) ||
                sourceDurationSeconds <= 0f ||
                Mathf.Abs(
                    motionData.Raw.DurationSeconds -
                    sourceDurationSeconds) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Foot Step Landing Event source timing is invalid.");
            }
            var result = new List<AnimationFootStepLandingEvent>();
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
                if (!step.Available || step.LandingOrdinal != footEvent.Ordinal)
                {
                    throw new InvalidOperationException(
                        "Foot Step Landing Event has no matching Step evidence.");
                }
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
            CharacterPresentationPosePlan posePlan,
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
            CharacterPresentationPosePlan posePlan,
            IReadOnlyList<CharacterAnimationBlendSpacePlan> blendSpaces,
            IReadOnlyDictionary<PresentationPoseSourceIndex, int> blendSpacePlanBySource,
            List<string> errors)
        {
            var result = new List<CharacterAnimationBlendSpacePlayerPlan>();
            for (int operationIndex = 0; operationIndex < posePlan.Operations.Count; operationIndex++)
            {
                CharacterPresentationPoseOperation operation = posePlan.Operations[operationIndex];
                if (operation.Code != CharacterPoseOperationCode.BlendSpacePlayer)
                    continue;
                if (!operation.PresentationPoseSourceIndex.IsValid ||
                    operation.ParameterIndex < 0 || operation.ParameterIndex >= posePlan.Parameters.Count)
                {
                    errors?.Add($"Blend Space Player '{operation.NodeId}' has incomplete compiled inputs.");
                    continue;
                }
                if (!blendSpacePlanBySource.TryGetValue(
                        operation.PresentationPoseSourceIndex,
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
                    operation.BlendSpaceInputRangePolicy != CharacterAnimationBlendSpaceInputRangePolicy.Clamp)
                    consistent = false;
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
                }
                if (!consistent)
                {
                    errors?.Add($"Blend Space Player '{operation.NodeId}' producer assets and typed axis inputs do not share one ParameterId/type/unit contract.");
                    continue;
                }
                result.Add(new CharacterAnimationBlendSpacePlayerPlan(
                    operation.NodeId,
                    operation.PresentationPoseSourceIndex,
                    operation.Index,
                    operation.PlayerIndex,
                    operation.ParameterIndex,
                    operation.ParameterIndexB,
                    operation.BlendSpaceInputRangePolicy,
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

        readonly struct BlendSourceEndpoint
        {
            public BlendSourceEndpoint(int sourceOwnerIndex, string identity)
            {
                if (sourceOwnerIndex < 0 ||
                    string.IsNullOrWhiteSpace(identity))
                    throw new ArgumentException(
                        "Blend source endpoint is invalid.");
                SourceOwnerIndex = sourceOwnerIndex;
                Identity = identity.Trim();
            }

            public int SourceOwnerIndex { get; }
            public string Identity { get; }
        }

        sealed class CompiledBlendAuthoringNode
        {
            public CompiledBlendAuthoringNode(PoseNodeId nodeId, CharacterTypedPoseNode node, SelectionEndpoint selection)
            {
                NodeId = nodeId;
                Node = node;
                Selection = selection;
            }

            public PoseNodeId NodeId { get; }
            public CharacterTypedPoseNode Node { get; }
            public SelectionEndpoint Selection { get; }
        }

        static AnimationBlendCatalogCompilation CompileBlendCatalogs(
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterAnimationRigDefinition rig,
            List<string> errors)
        {
            if (!graphAsset || graphAsset.Graph == null || !rig)
                return null;
            CharacterTypedPoseGraph graph = graphAsset.Graph;
            var curves = new SortedDictionary<string, AnimationBlendCurvePayload>(StringComparer.Ordinal);
            var profiles = new SortedDictionary<string, AnimationBlendProfilePayload>(StringComparer.Ordinal);
            var profileIdentityKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            List<CompiledBlendAuthoringNode> blendNodes =
                CollectBlendAuthoringNodes(graphAsset);
            for (int i = 0; i < blendNodes.Count; i++)
            {
                CharacterAnimationBlendPolicy policy = blendNodes[i].Node.BlendPolicy;
                if (!policy)
                    continue;
                CollectBlendRule(policy.DefaultTransition, rig, curves, profiles, profileIdentityKeys, errors);
                for (int overrideIndex = 0; overrideIndex < policy.Overrides.Count; overrideIndex++)
                    CollectBlendRule(policy.Overrides[overrideIndex]?.Rule, rig, curves, profiles, profileIdentityKeys, errors);
            }
            foreach (CharacterTypedPoseGraph authoredGraph in graphAsset.EnumerateGraphs())
            {
                for (int nodeIndex = 0; nodeIndex < authoredGraph.Nodes.Count; nodeIndex++)
                {
                    if (authoredGraph.Nodes[nodeIndex]?.Payload is not CharacterMotionMatchingPosePayload motionMatching ||
                        !motionMatching.JumpBlendPolicy)
                    {
                        continue;
                    }
                    CharacterAnimationBlendPolicy policy = motionMatching.JumpBlendPolicy;
                    try
                    {
                        policy.RequireValid(rig);
                        RequireStandardBlendOnly(policy.DefaultTransition, authoredGraph.Nodes[nodeIndex].NodeId);
                        if (policy.StackPolicy.StoredPosePolicy != AnimationStoredPosePolicy.CompressOldest ||
                            policy.Overrides.Count != 0)
                        {
                            throw new InvalidOperationException(
                                $"Motion Matching Pose '{authoredGraph.Nodes[nodeIndex].NodeId}' requires CompressOldest and one default Jump transition without owner overrides.");
                        }
                        CollectBlendRule(
                            policy.DefaultTransition,
                            rig,
                            curves,
                            profiles,
                            profileIdentityKeys,
                            errors);
                    }
                    catch (Exception exception)
                    {
                        errors?.Add(exception.Message);
                    }
                }
            }
            foreach (CharacterTypedPoseGraph authoredGraph in graphAsset.EnumerateGraphs())
            {
                for (int nodeIndex = 0; nodeIndex < authoredGraph.Nodes.Count; nodeIndex++)
                {
                    CharacterPoseStateMachineDefinition stateMachine =
                        authoredGraph.Nodes[nodeIndex]?.PoseStateMachine;
                    if (stateMachine == null)
                        continue;
                    for (int transitionIndex = 0;
                         transitionIndex < stateMachine.Transitions.Count;
                         transitionIndex++)
                    {
                        CollectPoseTransition(
                            stateMachine,
                            stateMachine.Transitions[transitionIndex],
                            rig,
                            curves,
                            profiles,
                            profileIdentityKeys,
                            errors);
                    }
                }
            }
            List<CharacterPoseInertializationPolicy> inertialPolicies =
                CollectInertializationPolicies(graphAsset);
            for (int i = 0; i < inertialPolicies.Count; i++)
            {
                CharacterPoseInertializationPolicy policy = inertialPolicies[i];
                if (!policy || policy.DirectPlayerRule == null)
                    continue;
                CollectInertialRule(
                    policy.DirectPlayerRule,
                    rig,
                    curves,
                    profiles,
                    profileIdentityKeys,
                    errors);
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
                profileCatalog.RequireValid(rig.PoseBoneCount, rig.RigId, rig.Revision);
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
                AnimationBlendCurvePayload curve = rule.CompileCurve();
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
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterAnimationRigDefinition rig,
            IReadOnlyList<CharacterPresentationProducerEntry> producers,
            AnimationBlendCatalogCompilation catalogs,
            List<string> errors)
        {
            if (!graphAsset || graphAsset.Graph == null || !rig || catalogs == null)
                return Array.Empty<AnimationBlendNodePayload>();
            List<CompiledBlendAuthoringNode> authoredNodes =
                CollectBlendAuthoringNodes(graphAsset)
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
                    if (authored.Node.Kind != CharacterPoseNodeKind.AnimationSlot)
                    {
                        RequireStandardBlendOnly(policy.DefaultTransition, authored.NodeId);
                        for (int overrideIndex = 0; overrideIndex < policy.Overrides.Count; overrideIndex++)
                        {
                            CharacterAnimationBlendTransitionOverride transition = policy.Overrides[overrideIndex];
                            if (transition != null)
                                RequireStandardBlendOnly(transition.Rule, authored.NodeId);
                        }
                    }
                }
                catch (Exception exception)
                {
                    errors?.Add(exception.Message);
                    continue;
                }

                BlendSourceEndpoint[] nodeSources;
                if (authored.Node.Kind ==
                    CharacterPoseNodeKind.AnimationSlot)
                {
                    nodeSources = producers
                        .Where(value =>
                            value != null &&
                            value.Kind ==
                            CharacterPresentationProducerKind
                                .Animation &&
                            value.AnimationChannelId ==
                            authored.Selection.ChannelId)
                        .OrderBy(value =>
                            value.ProgramProducerIndex)
                        .Select(value =>
                            new BlendSourceEndpoint(
                                value.ProgramProducerIndex,
                                value.ProgramProducerIdentity))
                        .ToArray();
                }
                else
                {
                    nodeSources = new[]
                    {
                        new BlendSourceEndpoint(
                            0,
                            CharacterPresentationAssetObjectIdentity.Require(
                                authored.Node.PresentationPoseSourceSlot))
                    };
                }
                var identities = new Dictionary<string, int>(StringComparer.Ordinal);
                AnimationSelectionAvailabilityPolicy outputPolicy =
                    authored.Node.Kind ==
                    CharacterPoseNodeKind.AnimationSlot
                        ? authored.Selection.Availability
                        : AnimationSelectionAvailabilityPolicy
                            .RequireSelection;
                for (int i = 0; i < nodeSources.Length; i++)
                {
                    BlendSourceEndpoint source = nodeSources[i];
                    if (!identities.TryAdd(
                            source.Identity,
                            source.SourceOwnerIndex))
                    {
                        errors?.Add(
                            $"Animation transition owner '{authored.NodeId}' duplicates source identity '{source.Identity}'.");
                    }
                }
                if (nodeSources.Length == 0)
                    errors?.Add($"Animation transition owner '{authored.NodeId}' has no reachable producer on Animation Channel '{authored.Selection.ChannelId}'.");
                ValidateTransitionOverrides(policy, authored, identities, errors);

                var transitions = new List<AnimationBlendTransitionPayload>();
                AnimationBlendTransitionEndpointKind initialEndpointKind =
                    authored.Node.Kind == CharacterPoseNodeKind.AnimationSlot
                        ? AnimationBlendTransitionEndpointKind.SourcePose
                        : AnimationBlendTransitionEndpointKind.NoPose;
                if (authored.Node.Kind == CharacterPoseNodeKind.AnimationSlot)
                {
                    CharacterAnimationBlendTransitionRule rule = policy.DefaultTransition;
                    string curveKey = AnimationBlendCanonicalPayload.CurveKey(rule.CompileCurve());
                    transitions.Add(new AnimationBlendTransitionPayload(
                        -1,
                        AnimationBlendTransitionEndpointKind.SourcePose,
                        string.Empty,
                        -1,
                        AnimationBlendTransitionEndpointKind.SourcePose,
                        string.Empty,
                        AnimationTransitionBlendLogic.StandardBlend,
                        0f,
                        catalogs.CurveIndices[curveKey],
                        catalogs.ProfileIndicesByIdentity[rule.BlendProfile.ProfileId]));
                }
                for (int target = 0; target < nodeSources.Length; target++)
                {
                    BlendSourceEndpoint targetSource =
                        nodeSources[target];
                    transitions.Add(CompileTransition(
                        policy,
                        -1,
                        initialEndpointKind,
                        string.Empty,
                        targetSource.SourceOwnerIndex,
                        AnimationBlendTransitionEndpointKind.SourceOwner,
                        targetSource.Identity,
                        outputPolicy,
                        catalogs));
                }
                for (int source = 0; source < nodeSources.Length; source++)
                {
                    BlendSourceEndpoint sourceEndpoint =
                        nodeSources[source];
                    for (int target = 0; target < nodeSources.Length; target++)
                    {
                        BlendSourceEndpoint targetEndpoint =
                            nodeSources[target];
                        transitions.Add(CompileTransition(
                            policy,
                            sourceEndpoint.SourceOwnerIndex,
                            AnimationBlendTransitionEndpointKind.SourceOwner,
                            sourceEndpoint.Identity,
                            targetEndpoint.SourceOwnerIndex,
                            AnimationBlendTransitionEndpointKind.SourceOwner,
                            targetEndpoint.Identity,
                            outputPolicy,
                            catalogs));
                    }
                    if (outputPolicy ==
                        AnimationSelectionAvailabilityPolicy
                            .AllowEmpty)
                    {
                        transitions.Add(CompileTransition(
                            policy,
                            sourceEndpoint.SourceOwnerIndex,
                            AnimationBlendTransitionEndpointKind.SourceOwner,
                            sourceEndpoint.Identity,
                            -1,
                            AnimationBlendTransitionEndpointKind.SourcePose,
                            string.Empty,
                            outputPolicy,
                            catalogs));
                    }
                }

                AnimationBlendTransitionPayload[] compiledTransitions =
                    transitions.ToArray();
                CompiledTransitionRoutingPlanPayload routingPlan =
                    authored.Node.Kind ==
                    CharacterPoseNodeKind.AnimationSlot
                        ? null
                        : CompileBlendRoutingPlan(
                            authored.NodeId,
                            policy.PolicyId,
                            policy.Revision,
                            compiledTransitions);
                result[nodeIndex] = new AnimationBlendNodePayload(
                    authored.NodeId,
                    policy.PolicyId,
                    policy.Revision,
                    new AnimationBlendStackPolicyPayload(policy.StackPolicy),
                    compiledTransitions,
                    routingPlan);
            }
            return result;
        }

        static void CollectPoseTransition(
            CharacterPoseStateMachineDefinition stateMachine,
            CharacterPoseStateTransition transition,
            CharacterAnimationRigDefinition rig,
            SortedDictionary<string, AnimationBlendCurvePayload> curves,
            SortedDictionary<string, AnimationBlendProfilePayload> profiles,
            Dictionary<string, string> profileIdentityKeys,
            List<string> errors)
        {
            try
            {
                if (transition == null)
                    throw new InvalidOperationException("Pose State Transition is missing.");
                CharacterPoseStateTransition.RequireBlendSettings(
                    transition.BlendLogic,
                    transition.DurationSeconds,
                    transition.BlendMode,
                    transition.CustomBlendCurve,
                    transition.BlendProfile);
                AnimationBlendCurvePayload curve =
                    CharacterAnimationBlendCurveCompiler.Compile(
                        transition.BlendMode,
                        transition.CustomBlendCurve);
                string curveKey = AnimationBlendCanonicalPayload.CurveKey(curve);
                if (!curves.ContainsKey(curveKey))
                    curves.Add(curveKey, curve);
                if (!transition.BlendProfile)
                    return;
                var profile = new AnimationBlendProfilePayload(
                    transition.BlendProfile,
                    rig);
                string profileKey = AnimationBlendCanonicalPayload.ProfileKey(profile);
                if (profileIdentityKeys.TryGetValue(profile.ProfileId, out string existingKey) &&
                    !string.Equals(existingKey, profileKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Animation Blend Profile identity '{profile.ProfileId}' resolves to multiple canonical payloads.");
                }
                profileIdentityKeys[profile.ProfileId] = profileKey;
                if (!profiles.ContainsKey(profileKey))
                    profiles.Add(profileKey, profile);
            }
            catch (Exception exception)
            {
                errors?.Add(
                    $"Pose StateMachine '{stateMachine?.StateMachineId}' Transition '{transition?.TransitionId}': {exception.Message}");
            }
        }

        static CompiledTransitionRoutingPlanPayload
            CompileBlendRoutingPlan(
                PoseNodeId nodeId,
                string policyId,
                string policyRevision,
                IReadOnlyList<AnimationBlendTransitionPayload>
                    transitions)
        {
            var identities = new Dictionary<int, string>();
            for (int i = 0; i < transitions.Count; i++)
            {
                AnimationBlendTransitionPayload transition =
                    transitions[i] ??
                    throw new InvalidOperationException(
                        $"Animation Blend Stack '{nodeId}' has a missing transition.");
                CollectBlendRoutingIdentity(
                    transition.SourceOwnerIndex,
                    transition.SourceEndpointKind,
                    transition.SourceOwnerIdentity,
                    identities);
                CollectBlendRoutingIdentity(
                    transition.TargetOwnerIndex,
                    transition.TargetEndpointKind,
                    transition.TargetOwnerIdentity,
                    identities);
            }
            var endpoints = new List<TransitionEndpointId>();
            if (transitions.Any(value =>
                    value.SourceEndpointKind == AnimationBlendTransitionEndpointKind.SourcePose ||
                    value.TargetEndpointKind == AnimationBlendTransitionEndpointKind.SourcePose))
            {
                endpoints.Add(TransitionEndpointId.SourcePose);
            }
            if (transitions.Any(value =>
                    value.SourceEndpointKind == AnimationBlendTransitionEndpointKind.NoPose ||
                    value.TargetEndpointKind == AnimationBlendTransitionEndpointKind.NoPose))
            {
                endpoints.Add(TransitionEndpointId.NoPose);
            }
            var endpointsByOwner =
                new Dictionary<int, TransitionEndpointId>();
            foreach (KeyValuePair<int, string> owner in
                     identities.OrderBy(
                          value => value.Value,
                          StringComparer.Ordinal))
            {
                var endpoint = new TransitionEndpointId(
                    $"animation-blend/{nodeId}/owner/{owner.Value}");
                endpoints.Add(endpoint);
                endpointsByOwner.Add(
                    owner.Key,
                    endpoint);
            }
            var rules =
                new AnimationTransitionRule[transitions.Count];
            for (int i = 0; i < rules.Length; i++)
            {
                AnimationBlendTransitionPayload transition =
                    transitions[i];
                TransitionEndpointId source = ResolveBlendRoutingEndpoint(
                    transition.SourceOwnerIndex,
                    transition.SourceEndpointKind,
                    endpointsByOwner);
                TransitionEndpointId target = ResolveBlendRoutingEndpoint(
                    transition.TargetOwnerIndex,
                    transition.TargetEndpointKind,
                    endpointsByOwner);
                rules[i] = new AnimationTransitionRule(
                    new TransitionRuleId(
                        $"animation-blend/{nodeId}/route/{StableHash.Compute(source.Value, target.Value)}"),
                    source,
                    target,
                    transition.BlendLogic,
                    transition.DurationSeconds,
                    new TransitionBlendCurveId(
                        $"curve/{transition.CurveIndex}"),
                    new TransitionBlendProfileId(
                        $"profile/{transition.BlendProfileIndex}"));
            }
            var revision = new TransitionDefinitionRevision(
                StableHash.Compute(
                    policyId,
                    policyRevision,
                    nodeId.Value).ToString());
            TransitionRoutingCompileResult result =
                TransitionRoutingCompiler.Compile(
                    new TransitionRoutingDefinition(
                        TransitionRoutingCompiler.CurrentSchemaVersion,
                        revision,
                        TransitionRoutingCoveragePolicy.DeclaredRules,
                        endpoints,
                        rules));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Animation Blend Stack '{nodeId}' Transition Routing compile failed: " +
                    string.Join(
                        " | ",
                        result.Diagnostics.Select(
                            value =>
                                $"[{value.Code}] {value.Message}")));
            }
            return new CompiledTransitionRoutingPlanPayload(
                result.Plan);
        }

        static void CollectBlendRoutingIdentity(
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind endpointKind,
            string sourceOwnerIdentity,
            Dictionary<int, string> identities)
        {
            if (endpointKind != AnimationBlendTransitionEndpointKind.SourceOwner)
                return;
            if (sourceOwnerIndex < 0 ||
                string.IsNullOrWhiteSpace(sourceOwnerIdentity))
            {
                throw new InvalidOperationException(
                    "Animation Blend Stack transition source owner endpoint is invalid.");
            }
            if (identities.TryGetValue(
                    sourceOwnerIndex,
                    out string existing) &&
                !string.Equals(
                    existing,
                    sourceOwnerIdentity,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Animation Blend Stack source owner index '{sourceOwnerIndex}' resolves to multiple identities.");
            }
            identities[sourceOwnerIndex] = sourceOwnerIdentity;
        }

        static TransitionEndpointId ResolveBlendRoutingEndpoint(
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind endpointKind,
            IReadOnlyDictionary<int, TransitionEndpointId> endpointsByOwner) =>
            endpointKind switch
            {
                AnimationBlendTransitionEndpointKind.SourceOwner =>
                    endpointsByOwner[sourceOwnerIndex],
                AnimationBlendTransitionEndpointKind.SourcePose =>
                    TransitionEndpointId.SourcePose,
                AnimationBlendTransitionEndpointKind.NoPose =>
                    TransitionEndpointId.NoPose,
                _ => throw new InvalidOperationException(
                    "Animation Blend transition endpoint kind is invalid.")
            };

        static void RequireStandardBlendOnly(
            CharacterAnimationBlendTransitionRule rule,
            PoseNodeId nodeId)
        {
            if (rule == null || rule.BlendLogic != AnimationTransitionBlendLogic.StandardBlend)
            {
                throw new InvalidOperationException(
                    $"Blend Stack '{nodeId}' cannot own branch-local Inertialization; use PoseStateMachine or AnimationSlot.");
            }
        }

        static void CollectInertialRule(
            CharacterPoseDirectInertializationRule rule,
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
                if (rule.Mode == PoseInertializationMode.HardCut)
                    return;
                AnimationBlendCurvePayload curve = rule.CompileCurve();
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

        static List<CharacterPoseInertializationPolicy> CollectInertializationPolicies(
            CharacterPresentationPoseGraphAsset graphAsset)
        {
            var result = new List<CharacterPoseInertializationPolicy>();
            CollectInertializationPolicies(
                graphAsset,
                graphAsset.Graph,
                result);
            return result;
        }

        static void CollectInertializationPolicies(
            CharacterPresentationPoseGraphAsset owner,
            CharacterTypedPoseGraph graph,
            List<CharacterPoseInertializationPolicy> result)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterTypedPoseNode node = graph.Nodes[i];
                if (node.Kind == CharacterPoseNodeKind.Inertialization && node.InertializationPolicy)
                    result.Add(node.InertializationPolicy);
                if (node.Kind != CharacterPoseNodeKind.PoseSubgraph ||
                    node.Subgraph == null ||
                    !node.Subgraph.PoseGraphId.IsValid)
                    continue;
                CharacterTypedPoseGraph child =
                    owner.RequireGraph(node.Subgraph.PoseGraphId);
                CollectInertializationPolicies(owner, child, result);
            }
        }

        static void ValidateTransitionOverrides(
            CharacterAnimationBlendPolicy policy,
            CompiledBlendAuthoringNode authored,
            IReadOnlyDictionary<string, int> sourceOwnerIdentities,
            List<string> errors)
        {
            AnimationBlendTransitionEndpointKind initialEndpointKind =
                authored.Node.Kind == CharacterPoseNodeKind.AnimationSlot
                    ? AnimationBlendTransitionEndpointKind.SourcePose
                    : AnimationBlendTransitionEndpointKind.NoPose;
            AnimationSelectionAvailabilityPolicy outputPolicy =
                authored.Node.Kind == CharacterPoseNodeKind.AnimationSlot
                    ? authored.Selection.Availability
                    : AnimationSelectionAvailabilityPolicy.RequireSelection;
            for (int i = 0; i < policy.Overrides.Count; i++)
            {
                CharacterAnimationBlendTransitionOverride transition = policy.Overrides[i];
                if (transition == null)
                    continue;
                if (transition.SourceEndpointKind != AnimationBlendTransitionEndpointKind.SourceOwner &&
                    transition.SourceEndpointKind != initialEndpointKind)
                {
                    errors?.Add(
                        $"Animation transition owner '{authored.NodeId}' transition override #{i} uses endpoint '{transition.SourceEndpointKind}' outside this node type.");
                    continue;
                }
                if (transition.SourceEndpointKind == transition.TargetEndpointKind &&
                    transition.SourceEndpointKind != AnimationBlendTransitionEndpointKind.SourceOwner)
                {
                    errors?.Add(
                        $"Animation transition owner '{authored.NodeId}' transition override #{i} cannot override the unchanged '{transition.SourceEndpointKind}' route.");
                    continue;
                }
                if (transition.SourceEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner &&
                    !sourceOwnerIdentities.ContainsKey(transition.SourceOwnerIdentity))
                    errors?.Add($"Animation transition owner '{authored.NodeId}' transition override #{i} references source owner '{transition.SourceOwnerIdentity}' outside its endpoint set.");
                if (transition.TargetEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner &&
                    !sourceOwnerIdentities.ContainsKey(transition.TargetOwnerIdentity))
                    errors?.Add($"Animation transition owner '{authored.NodeId}' transition override #{i} references target owner '{transition.TargetOwnerIdentity}' outside its endpoint set.");
                if (transition.TargetEndpointKind != AnimationBlendTransitionEndpointKind.SourceOwner &&
                    (transition.TargetEndpointKind != AnimationBlendTransitionEndpointKind.SourcePose ||
                     initialEndpointKind != AnimationBlendTransitionEndpointKind.SourcePose ||
                     outputPolicy != AnimationSelectionAvailabilityPolicy.AllowEmpty))
                {
                    errors?.Add(
                        $"Animation transition owner '{authored.NodeId}' transition override #{i} targets endpoint '{transition.TargetEndpointKind}' outside this node's output contract.");
                }
            }
        }

        static AnimationBlendTransitionPayload CompileTransition(
            CharacterAnimationBlendPolicy policy,
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind sourceEndpointKind,
            string sourceOwnerIdentity,
            int targetOwnerIndex,
            AnimationBlendTransitionEndpointKind targetEndpointKind,
            string targetOwnerIdentity,
            AnimationSelectionAvailabilityPolicy outputPolicy,
            AnimationBlendCatalogCompilation catalogs)
        {
            CharacterAnimationBlendTransitionRule rule = policy.DefaultTransition;
            for (int i = 0; i < policy.Overrides.Count; i++)
            {
                CharacterAnimationBlendTransitionOverride candidate = policy.Overrides[i];
                if (candidate != null &&
                    candidate.SourceEndpointKind == sourceEndpointKind &&
                    candidate.TargetEndpointKind == targetEndpointKind &&
                    string.Equals(candidate.SourceOwnerIdentity, sourceOwnerIdentity, StringComparison.Ordinal) &&
                    string.Equals(candidate.TargetOwnerIdentity, targetOwnerIdentity, StringComparison.Ordinal))
                {
                    rule = candidate.Rule;
                    break;
                }
            }
            string curveKey = AnimationBlendCanonicalPayload.CurveKey(rule.CompileCurve());
            float durationSeconds =
                outputPolicy == AnimationSelectionAvailabilityPolicy.RequireSelection &&
                sourceEndpointKind == AnimationBlendTransitionEndpointKind.NoPose &&
                targetEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner
                ? 0f
                : rule.DurationSeconds;
            return new AnimationBlendTransitionPayload(
                sourceOwnerIndex,
                sourceEndpointKind,
                sourceOwnerIdentity,
                targetOwnerIndex,
                targetEndpointKind,
                targetOwnerIdentity,
                rule.BlendLogic,
                durationSeconds,
                catalogs.CurveIndices[curveKey],
                catalogs.ProfileIndicesByIdentity[rule.BlendProfile.ProfileId]);
        }

        static List<CompiledBlendAuthoringNode> CollectBlendAuthoringNodes(
            CharacterPresentationPoseGraphAsset owner)
        {
            var result = new List<CompiledBlendAuthoringNode>();
            CollectBlendAuthoringNodes(
                owner,
                owner.Graph,
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
            CharacterPresentationPoseGraphAsset owner,
            CharacterTypedPoseGraph graph,
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
            List<CharacterTypedPoseNode> ordered = TopologicalPoseNodes(graph);
            for (int nodeIndex = 0; nodeIndex < ordered.Count; nodeIndex++)
            {
                CharacterTypedPoseNode node = ordered[nodeIndex];
                if (node.Kind ==
                    CharacterPoseNodeKind.ActionPlaybackInput)
                {
                    ICharacterPoseCompilerHandler handler =
                        CharacterPoseCompilerHandlerRegistry.Shared
                            .Require(node.Kind);
                    var endpoint = new SelectionEndpoint(
                        handler.Channel(node.Payload),
                        string.Empty,
                        handler.Availability(node.Payload, false));
                    BindSelectionOutputs(node, scope, endpoint, values);
                    continue;
                }
                if (node.Kind == CharacterPoseNodeKind.GraphInput)
                {
                    IReadOnlyList<CharacterPosePortDefinition> ports =
                        CharacterPoseAuthoringPortProjection.Get(node);
                    for (int i = 0; i < ports.Count; i++)
                    {
                        CharacterPosePortDefinition port = ports[i];
                        if (port != null && IsSelectionPort(port.Kind) &&
                            port.Direction == CharacterPosePortDirection.Output && imports.TryGetValue(port.InterfacePortId, out SelectionEndpoint endpoint))
                            values.Add(ScopedEndpoint(node.NodeId, port.PortId, scope), endpoint);
                    }
                    continue;
                }
                if (node.Kind == CharacterPoseNodeKind.GraphOutput)
                {
                    IReadOnlyList<CharacterPosePortDefinition> ports =
                        CharacterPoseAuthoringPortProjection.Get(node);
                    for (int i = 0; i < ports.Count; i++)
                    {
                        CharacterPosePortDefinition port = ports[i];
                        if (port != null && IsSelectionPort(port.Kind) &&
                            port.Direction == CharacterPosePortDirection.Input &&
                            TryResolveSelection(node, port, incoming, scope, values, out SelectionEndpoint endpoint))
                            exports.Add(port.InterfacePortId, endpoint);
                    }
                    continue;
                }
                if (node.Kind == CharacterPoseNodeKind.PoseSubgraph)
                {
                    var childImports = new Dictionary<PoseInterfacePortId, SelectionEndpoint>();
                    IReadOnlyList<CharacterPosePortDefinition> ports =
                        CharacterPoseAuthoringPortProjection.Get(node);
                    for (int i = 0; i < ports.Count; i++)
                    {
                        CharacterPosePortDefinition port = ports[i];
                        if (port != null && IsSelectionPort(port.Kind) &&
                            port.Direction == CharacterPosePortDirection.Input &&
                            TryResolveSelection(node, port, incoming, scope, values, out SelectionEndpoint endpoint))
                            childImports.Add(port.InterfacePortId, endpoint);
                    }
                    CharacterTypedPoseGraph child =
                        owner.RequireGraph(node.Subgraph.PoseGraphId);
                    PoseNodeId callSite = ScopePoseNodeId(node.NodeId, scope);
                    string childScope = callSite.Value + "/" + child.GraphId;
                    Dictionary<PoseInterfacePortId, SelectionEndpoint> childExports =
                        CollectBlendAuthoringNodes(
                            owner,
                            child,
                            childScope,
                            childImports,
                            result);
                    for (int i = 0; i < ports.Count; i++)
                    {
                        CharacterPosePortDefinition port = ports[i];
                        if (port != null && IsSelectionPort(port.Kind) &&
                            port.Direction == CharacterPosePortDirection.Output && childExports.TryGetValue(port.InterfacePortId, out SelectionEndpoint endpoint))
                            values.Add(ScopedEndpoint(node.NodeId, port.PortId, scope), endpoint);
                    }
                    continue;
                }
                if (node.Kind ==
                        CharacterPoseNodeKind.PoseStateMachine &&
                    node.PoseStateMachine != null)
                {
                    PoseNodeId stateMachineNodeId =
                        ScopePoseNodeId(node.NodeId, scope);
                    for (int stateIndex = 0;
                         stateIndex <
                         node.PoseStateMachine.States.Count;
                         stateIndex++)
                    {
                        CharacterPoseStateDefinition state =
                            node.PoseStateMachine.States[
                                stateIndex];
                        if (state == null ||
                            !state.PoseGraphId.IsValid)
                            continue;
                        CollectBlendAuthoringNodes(
                            owner,
                            owner.RequireGraph(
                                state.PoseGraphId),
                            stateMachineNodeId.Value +
                            "/state/" +
                            state.StateId.Value,
                            new Dictionary<
                                PoseInterfacePortId,
                                SelectionEndpoint>(),
                            result);
                    }
                    continue;
                }
                if (node.Kind != CharacterPoseNodeKind.BlendStack &&
                    node.Kind != CharacterPoseNodeKind.AnimationSlot)
                    continue;
                if (node.Kind == CharacterPoseNodeKind.BlendStack)
                {
                    if (!node.PresentationPoseSourceSlot)
                    {
                        throw new InvalidOperationException(
                            $"Pose State Blend Stack '{node.NodeId}' has no Source Slot.");
                    }
                    result.Add(new CompiledBlendAuthoringNode(
                        ScopePoseNodeId(node.NodeId, scope),
                        node,
                        default));
                    continue;
                }
                CharacterPosePortDefinition selectionPort =
                    CharacterPoseAuthoringPortProjection.Get(node).Single(port =>
                    port.Kind == CharacterPosePortKind.ActionPlayback &&
                    port.Direction == CharacterPosePortDirection.Input);
                if (!TryResolveSelection(node, selectionPort, incoming, scope, values, out SelectionEndpoint selection))
                    throw new InvalidOperationException($"Animation transition owner '{node.NodeId}' has no resolvable selection endpoint.");
                if (node.AnimationChannelId != selection.ChannelId)
                {
                    throw new InvalidOperationException(
                        $"Animation Slot '{node.NodeId}' channel '{node.AnimationChannelId}' does not match Action Playback channel '{selection.ChannelId}'.");
                }
                result.Add(new CompiledBlendAuthoringNode(ScopePoseNodeId(node.NodeId, scope), node, selection));
            }
            return exports;
        }

        static void BindSelectionOutputs(
            CharacterTypedPoseNode node,
            string scope,
            SelectionEndpoint endpoint,
            Dictionary<string, SelectionEndpoint> values)
        {
            IReadOnlyList<CharacterPosePortDefinition> ports =
                CharacterPoseAuthoringPortProjection.Get(node);
            for (int i = 0; i < ports.Count; i++)
            {
                CharacterPosePortDefinition port = ports[i];
                if (port != null && IsSelectionPort(port.Kind) && port.Direction == CharacterPosePortDirection.Output)
                    values.Add(ScopedEndpoint(node.NodeId, port.PortId, scope), endpoint);
            }
        }

        static bool IsSelectionPort(CharacterPosePortKind kind) =>
            kind == CharacterPosePortKind.ActionPlayback;

        static bool TryResolveSelection(
            CharacterTypedPoseNode node,
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

        static List<CharacterTypedPoseNode> TopologicalPoseNodes(CharacterTypedPoseGraph graph)
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
            var result = new List<CharacterTypedPoseNode>(nodes.Count);
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
            PoseSourceCompilationCatalog sourceCatalog,
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
