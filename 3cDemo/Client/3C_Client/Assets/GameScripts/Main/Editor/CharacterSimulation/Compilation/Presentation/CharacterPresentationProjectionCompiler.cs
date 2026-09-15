using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Control.Rules;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL;
using ThirdPersonCharacter.Editor.MotionMatching;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCamera;
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
            CharacterPresentationProjectionCompileContext context,
            CharacterFootPlacementAnalysisCompilation footAnalysis,
            CharacterAnimationBuildInput animationBuildInput,
            CharacterPresentationProjectionCompileResult reusedPoseResult = null,
            bool bindPoseProjectionIdentity = true)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            FootAnalysis = footAnalysis ?? throw new ArgumentNullException(nameof(footAnalysis));
            AnimationBuildInput = animationBuildInput ?? throw new ArgumentNullException(nameof(animationBuildInput));
            ReusedPoseResult = reusedPoseResult;
            BindPoseProjectionIdentity = bindPoseProjectionIdentity;
        }

        public CharacterPresentationProjectionCompileContext Context { get; }
        public CharacterFootPlacementAnalysisCompilation FootAnalysis { get; }
        public CharacterAnimationBuildInput AnimationBuildInput { get; }
        public CharacterPresentationProjectionCompileResult ReusedPoseResult { get; }
        public bool BindPoseProjectionIdentity { get; }
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
            IReadOnlyList<CharacterPresentationProjectionDiagnostic> diagnostics,
            CharacterAnimationBuildCatalog animationCatalog)
        {
            Projection = projection;
            Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            ProjectionRevision = projectionRevision ?? string.Empty;
            Diagnostics = diagnostics ?? Array.Empty<CharacterPresentationProjectionDiagnostic>();
            AnimationCatalog = animationCatalog ?? throw new ArgumentNullException(nameof(animationCatalog));
        }

        public CharacterPresentationProjection Projection { get; }
        public CharacterPresentationSemanticContract Contract { get; }
        public string ProjectionRevision { get; }
        public IReadOnlyList<CharacterPresentationProjectionDiagnostic> Diagnostics { get; }
        internal CharacterAnimationBuildCatalog AnimationCatalog { get; }
        internal IReadOnlyList<CharacterAclAnimationGroupArtifact> AnimationArtifacts =>
            AnimationCatalog.AnimationArtifacts;
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
            CharacterPresentationProjectionCompileContext context = request.Context;
            if (request.AnimationBuildInput.Profile != context.AnimationPresentationProfile)
                throw new InvalidOperationException("Animation build input Profile does not match the compilation model.");
            if (!context.AnimationInputContract.Matches(context.AnimationPresentationProfile))
                throw new InvalidOperationException("Animation Input Contract does not match the Animation Presentation Profile.");
            bool reusePoseResult = request.ReusedPoseResult != null &&
                                   !context.HasGameplayProducerContract;
            var phaseWatch = System.Diagnostics.Stopwatch.StartNew();
            long poseSourceMs = 0;
            long motionMatchingMs = 0;
            CharacterPresentationPoseSourceCompilationCatalog sourceCatalog = null;
            CharacterPresentationPoseResourceCompilationCatalog resourceCatalog = null;
            MotionMatchingProjectionPayload motionMatching = null;
            if (!reusePoseResult)
            {
                var resourceDiagnostics = new List<string>();
                resourceCatalog = CharacterPresentationPoseResourceCompiler.Compile(
                    context.AnimationPresentationProfile,
                    resourceDiagnostics);
                errors.AddRange(resourceDiagnostics);
                CharacterPresentationPoseSourceCompilationResult sourceCompilation =
                    CharacterPresentationPoseSourceCompiler.Compile(
                        context.AnimationPresentationProfile);
                phaseWatch.Stop();
                poseSourceMs = phaseWatch.ElapsedMilliseconds;
                errors.AddRange(sourceCompilation.Diagnostics);
                sourceCatalog = sourceCompilation.Catalog;
                phaseWatch.Restart();
                CharacterPresentationMotionMatchingCompilationResult motionMatchingCompilation =
                    CharacterPresentationMotionMatchingCompiler.Compile(
                        context.AnimationPresentationProfile,
                        request.AnimationBuildInput,
                        resourceCatalog);
                phaseWatch.Stop();
                motionMatchingMs = phaseWatch.ElapsedMilliseconds;
                errors.AddRange(motionMatchingCompilation.Diagnostics);
                motionMatching = motionMatchingCompilation.Payload;
            }
            else
            {
                if (!request.ReusedPoseResult.IsValid || request.ReusedPoseResult.Projection == null)
                    errors.Add("Character assembly received an invalid independent Pose compilation result.");
                else
                {
                    motionMatching = request.ReusedPoseResult.Projection.MotionMatching;
                    RegisterReusedPoseAnimationCatalog(
                        request.ReusedPoseResult,
                        request.AnimationBuildInput);
                }
            }
            long coreMs = 0;
            long catalogMs = 0;
            IReadOnlyList<string> movementModeStateIdentities =
                ResolveMovementModeStateIdentities(context, errors);
            CharacterPresentationProjection projection = null;
            string projectionRevision = string.Empty;
            CharacterAnimationBuildCatalog animationCatalog = null;
            if (errors.Count == 0)
            {
                phaseWatch.Restart();
                projection = CompileCore(
                    context,
                    context.AnimationPresentationProfile,
                    context.CameraProfile,
                    context.EquipmentProfile,
                        context.EquipmentPresentationProfile,
                        request.FootAnalysis,
                        motionMatching,
                        sourceCatalog,
                        resourceCatalog,
                        request.AnimationBuildInput,
                        movementModeStateIdentities,
                        errors,
                        reusePoseResult ? request.ReusedPoseResult : null);
                phaseWatch.Stop();
                coreMs = phaseWatch.ElapsedMilliseconds;
                phaseWatch.Restart();
                animationCatalog = request.AnimationBuildInput.AnimationCatalog.Complete(errors);
                if (projection != null && animationCatalog != null && errors.Count == 0)
                {
                    projectionRevision = CharacterPresentationProjectionRevisionCompiler.Compute(
                        context.AnimationPresentationProfile,
                        request.AnimationBuildInput.SourceResourceBindings,
                        context.EquipmentPresentationProfile,
                        context.SemanticContract.ContractHash,
                        request.FootAnalysis.RevisionTokens,
                        motionMatching,
                        animationCatalog.AnimationResources);
                    projection.SetAnimationResources(
                        animationCatalog.AnimationResources.ToArray(),
                        projectionRevision);
                    projection.RequireContract(context.SemanticContract);
                    projection.RequirePosePayload(request.BindPoseProjectionIdentity);
                    CharacterPoseTuningCompilationResult tuning =
                        CharacterPoseTuningLayoutCompiler.Compile(
                            context.SemanticContract.ProgramId.Value,
                            projection);
                    projection.SetTuningPayload(
                        tuning.Layout,
                        tuning.DefaultBlock,
                        tuning.PublishedParameterRevision);
                }
                phaseWatch.Stop();
                catalogMs = phaseWatch.ElapsedMilliseconds;
            }
            if (animationCatalog == null)
                animationCatalog = request.AnimationBuildInput.AnimationCatalog.Complete(errors);
            phaseWatch.Restart();
            CharacterPresentationProjectionValidationResult validation =
                CharacterPoseGraphProjectionValidator.Validate(
                    projection,
                    context.SemanticContract);
            phaseWatch.Stop();
            UnityEngine.Debug.Log(
                $"[计时] 投影细分 PoseSource {poseSourceMs}ms | MM {motionMatchingMs}ms | " +
                $"Core {coreMs}ms | 目录+修订 {catalogMs}ms | 校验 {phaseWatch.ElapsedMilliseconds}ms");
            errors.AddRange(validation.Diagnostics);
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
                    context.SemanticContract.ProgramId.Value,
                    message);
            }
            return new CharacterPresentationProjectionCompileResult(
                projection,
                context.SemanticContract,
                projectionRevision,
                diagnostics,
                animationCatalog);
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
                definition.AnimationPresentationProfile.SourceResourceBindings,
                definition.EquipmentPresentationProfile,
                contract.ContractHash,
                footAnalysisTokens,
                projection.MotionMatching,
                projection.AnimationResources);
            return true;
        }

        static IReadOnlyList<string> ResolveMovementModeStateIdentities(
            CharacterPresentationProjectionCompileContext context,
            List<string> errors)
        {
            if (!context.HasGameplayProducerContract)
                return context.AnimationInputContract.MovementModeStateIdentities;
            string moduleId = context.ControlModuleId;
            if (string.IsNullOrEmpty(moduleId))
            {
                errors?.Add("Presentation Projection requires a Definition Control Module identity for Movement Mode state validation.");
                return Array.Empty<string>();
            }
            ICharacterControlModule controlModule =
                CorinCharacterControlModuleCatalog.Create().Require(new CharacterControlModuleId(moduleId));
            IReadOnlyList<string> identities =
                CharacterMovementModeStateIdentities.FromControlContract(controlModule.Contract);
            for (int i = 0; i < context.AnimationInputContract.MovementModeStateIdentities.Count; i++)
            {
                string required = context.AnimationInputContract.MovementModeStateIdentities[i];
                if (!identities.Contains(required, StringComparer.Ordinal))
                    errors?.Add($"Animation Input Contract Movement Mode state '{required}' is not declared by the Control Module.");
            }
            return identities;
        }

        static void RegisterReusedPoseAnimationCatalog(
            CharacterPresentationProjectionCompileResult poseResult,
            CharacterAnimationBuildInput animationBuildInput)
        {
            for (int i = 0; i < poseResult.AnimationCatalog.Entries.Count; i++)
            {
                CharacterAnimationBuildCatalogEntry entry =
                    poseResult.AnimationCatalog.Entries[i];
                animationBuildInput.AnimationCatalog.RegisterReference(
                    entry.AuthoringClip,
                    entry.Backend,
                    entry.NativeScalarPage,
                    "pose-reused:" + entry.StableIdentity);
            }
        }

        static CharacterPresentationAnimationPropertyBinding[] CompileAnimationProperties(
            CharacterAnimationPresentationProfile profile,
            CharacterPoseProgramImage poseProgram,
            List<string> errors)
        {
            if (!profile || poseProgram == null)
                return Array.Empty<CharacterPresentationAnimationPropertyBinding>();
            var result = new List<CharacterPresentationAnimationPropertyBinding>();
            for (int i = 0; i < profile.AnimationPropertyBindings.Count; i++)
            {
                CharacterAnimationPropertyAuthoringBinding source = profile.AnimationPropertyBindings[i];
                try
                {
                    source.RequireValid();
                    int parameterIndex = poseProgram.RequireParameterIndex(source.ParameterId);
                    CharacterPresentationPoseParameterEntry parameter = poseProgram.Parameters[parameterIndex];
                    result.Add(new CharacterPresentationAnimationPropertyBinding(
                        source.RendererBindingId + ":" + source.BlendShapeName,
                        source.ParameterId,
                        parameterIndex,
                        parameter.Unit,
                        parameter.DefaultValue,
                        source.RendererBindingId,
                        source.ExpectedMesh,
                        source.MeshContentHash,
                        source.BlendShapeName,
                        source.BlendShapeIndex));
                }
                catch (Exception exception)
                {
                    errors?.Add($"Animation property binding #{i} failed to compile: {exception.Message}");
                }
            }
            result.Sort((left, right) => string.CompareOrdinal(left.BindingId, right.BindingId));
            return result.ToArray();
        }

        static CharacterPresentationProjection CompileCore(
            CharacterPresentationProjectionCompileContext context,
            CharacterAnimationPresentationProfile profile,
            CharacterCameraProfile cameraProfile,
            CharacterEquipmentProfile equipmentProfile,
            CharacterEquipmentPresentationProfile equipmentPresentationProfile,
            CharacterFootPlacementAnalysisCompilation footAnalysisCompilation,
            MotionMatchingProjectionPayload motionMatching,
            CharacterPresentationPoseSourceCompilationCatalog sourceCatalog,
            CharacterPresentationPoseResourceCompilationCatalog resourceCatalog,
            CharacterAnimationBuildInput animationBuildInput,
            IReadOnlyList<string> movementModeStateIdentities,
            List<string> errors,
            CharacterPresentationProjectionCompileResult reusedPoseResult = null)
        {
            if (context == null || profile == null ||
                (reusedPoseResult == null && (sourceCatalog == null || resourceCatalog == null)) ||
                animationBuildInput == null)
            {
                errors?.Add("Character Presentation Projection build input is incomplete.");
                return null;
            }

            if (context.HasGameplayProducerContract)
                profile.CollectConfigurationErrors(errors);
            else
                profile.CollectPoseConfigurationErrors(errors);
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
            var entries = new List<CharacterPresentationProducerEntry>(context.Producers.Count);
            var animationIds = new HashSet<AnimationProducerId>();
            for (int i = 0; i < context.Producers.Count; i++)
            {
                CharacterPresentationProducerEntry entry = context.Producers[i];
                if (entry == null)
                {
                    errors?.Add($"Presentation producer entry #{i} is missing.");
                    continue;
                }
                entries.Add(entry);
                if (entry.Kind == CharacterPresentationProducerKind.Animation)
                    animationIds.Add(entry.ProducerId);
            }
            if (context.HasGameplayProducerContract)
                for (int i = 0; i < profile.ProducerBindings.Count; i++)
                {
                    AnimationProducerPresentationBinding binding = profile.ProducerBindings[i];
                    if (binding != null && binding.ProducerId.IsValid && !animationIds.Contains(binding.ProducerId))
                        errors?.Add($"Animation producer binding '{binding.ProducerId}' is orphaned from the Semantic IR.");
            }
            entries.Sort((left, right) => left.ProgramProducerIndex.CompareTo(right.ProgramProducerIndex));
            if (reusedPoseResult != null)
            {
                return CompileAssembledCore(
                    context,
                    profile,
                    cameraProfile,
                    equipmentProfile,
                    equipmentPresentationProfile,
                    footAnalysisCompilation,
                    motionMatching,
                    entries.ToArray(),
                    errors,
                    reusedPoseResult.Projection);
            }
            CharacterPresentationAnimationBlendCompiler.Compilation blendCatalogs =
                CharacterPresentationAnimationBlendCompiler.CompileCatalog(
                    profile.PoseGraph,
                    profile.RigDefinition,
                    resourceCatalog,
                    errors);
            AnimationBlendNodePayload[] blendNodes = blendCatalogs == null
                ? Array.Empty<AnimationBlendNodePayload>()
                : CharacterPresentationAnimationBlendCompiler.CompileNodes(
                    profile.PoseGraph,
                    profile.RigDefinition,
                    resourceCatalog,
                    entries,
                    blendCatalogs,
                    errors,
                    context.HasGameplayProducerContract);
            CharacterPresentationPoseSourcePlanCompilationResult sourcePlanCompilation =
                CharacterPresentationPoseSourcePlanCompiler.Compile(
                    sourceCatalog,
                    profile,
                    profile.RigDefinition,
                    footAnalysisCompilation,
                    animationBuildInput);
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
                context.AnimationInputContract,
                profile.RigDefinition,
                blendNodes,
                poseSources,
                clipPhasePlans,
                sourcePhasePlans,
                clipPhaseValidations,
                sourceCatalog.SourceIndices,
                blendCatalogs?.CurveIndices,
                blendCatalogs?.ProfileIndicesByIdentity,
                resourceCatalog,
                profile,
                linkedPose,
                motionMatching,
                footAnalysisCompilation,
                movementModeStateIdentities);
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
            CharacterPresentationAnimationPropertyBinding[] animationProperties =
                CompileAnimationProperties(profile, poseProgram, errors);
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

            CharacterCameraProjectionPayload cameraPayload =
                cameraProfile ? CharacterCameraProjectionBuilder.Build(cameraProfile) : null;
            CharacterPresentationProjection projection = CharacterPresentationProjection.Create(
                context.SemanticContract,
                poseProgram,
                blendCatalogs.CurveCatalog,
                blendCatalogs.ProfileCatalog,
                rig,
                Array.Empty<CharacterAnimationCompiledResourceDescriptor>(),
                motionMatching,
                poseSources,
                blendSpaces.ToArray(),
                blendSpacePlayers,
                clipPhasePlans,
                sourcePhasePlans,
                entries.ToArray(),
                footIdentity,
                string.Empty,
                visualBindings,
                linkedPose,
                cameraPayload,
                null,
                null,
                string.Empty,
                animationProperties);
            return projection;
        }

        static CharacterPresentationProjection CompileAssembledCore(
            CharacterPresentationProjectionCompileContext context,
            CharacterAnimationPresentationProfile profile,
            CharacterCameraProfile cameraProfile,
            CharacterEquipmentProfile equipmentProfile,
            CharacterEquipmentPresentationProfile equipmentPresentationProfile,
            CharacterFootPlacementAnalysisCompilation footAnalysisCompilation,
            MotionMatchingProjectionPayload motionMatching,
            CharacterPresentationProducerEntry[] producers,
            List<string> errors,
            CharacterPresentationProjection poseProjection)
        {
            if (poseProjection == null || !poseProjection.IsValid)
            {
                errors?.Add("Character assembly received an invalid Pose Projection.");
                return null;
            }
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
            CharacterLinkedPoseProjectionPayload linkedPose =
                CharacterLinkedPoseProjectionCompiler.Compile(
                    profile,
                    equipmentProfile,
                    errors);
            CharacterPresentationAnimationPropertyBinding[] animationProperties =
                CompileAnimationProperties(profile, poseProjection.PosePlan, errors);
            CharacterPresentationPoseSourcePlanCompiler.ValidateClipPlayers(
                poseProjection.PosePlan,
                poseProjection.PoseSources,
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
            CharacterCameraProjectionPayload cameraPayload =
                cameraProfile ? CharacterCameraProjectionBuilder.Build(cameraProfile) : null;
            return CharacterPresentationProjection.Create(
                context.SemanticContract,
                poseProjection.PosePlan,
                poseProjection.BlendCurveCatalog,
                poseProjection.BlendProfileCatalog,
                poseProjection.Rig,
                Array.Empty<CharacterAnimationCompiledResourceDescriptor>(),
                motionMatching,
                poseProjection.PoseSources.ToArray(),
                poseProjection.BlendSpaces.ToArray(),
                poseProjection.BlendSpacePlayers.ToArray(),
                poseProjection.ClipPhasePlans.ToArray(),
                poseProjection.SourcePhasePlans.ToArray(),
                producers,
                footIdentity,
                string.Empty,
                visualBindings,
                linkedPose,
                cameraPayload,
                null,
                null,
                string.Empty,
                animationProperties);
        }

    }
}
