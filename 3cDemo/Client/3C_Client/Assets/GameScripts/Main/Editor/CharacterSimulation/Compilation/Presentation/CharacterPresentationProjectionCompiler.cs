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
            CharacterPresentationPoseSourcePlanCompilationResult sourcePlanCompilation =
                CharacterPresentationPoseSourcePlanCompiler.Compile(
                    sourceCatalog,
                    profile.RigDefinition,
                    footAnalysisCompilation);
            errors.AddRange(sourcePlanCompilation.Diagnostics);
            IReadOnlyList<CharacterAnimationBlendSpacePlan> blendSpaces =
                sourcePlanCompilation.BlendSpaces;
            IReadOnlyDictionary<PresentationPoseSourceIndex, int> blendSpacePlanBySource =
                sourcePlanCompilation.BlendSpacePlanBySource;
            CharacterPresentationPoseSourcePlan[] poseSources =
                sourcePlanCompilation.PoseSources;
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
                : CharacterPresentationPoseSourcePlanCompiler.CompileBlendSpacePlayers(
                    poseProgram,
                    blendSpaces,
                    blendSpacePlanBySource,
                    errors);
            CharacterAnimationRigPayload rig = poseProgram == null
                ? null
                : new CharacterAnimationRigPayload(profile.RigDefinition);
            CharacterPresentationPoseSourcePlanCompiler.ValidateClipPlayers(
                poseProgram,
                poseSources,
                profile.RigDefinition,
                errors);
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

    }
}
