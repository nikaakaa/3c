using System;
using System.Collections.Generic;
using System.IO;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static partial class CharacterSimulationBuildOrchestrator
    {
        public static CharacterSimulationBuildResult Build(CharacterPipelineDefinition definition)
        {
            return Build(new CharacterSimulationBuildRequest(
                definition,
                CharacterSimulationBuildPublicationMode.Publish,
                CharacterSimulationTargetCatalog.DefaultEditor(definition)));
        }

        public static CharacterSimulationBuildResult Build(
            CharacterPipelineDefinition definition,
            IReadOnlyList<ICharacterSimulationTargetBuildAdapter> targets)
        {
            return Build(new CharacterSimulationBuildRequest(
                definition,
                CharacterSimulationBuildPublicationMode.Publish,
                targets));
        }

        public static CharacterSimulationBuildResult Build(CharacterSimulationBuildRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var timingWatch = System.Diagnostics.Stopwatch.StartNew();
            bool publish = request.PublicationMode == CharacterSimulationBuildPublicationMode.Publish;
            CharacterSimulationBuildResult result = Execute(
                request,
                out ValidatedSemanticIrArtifact semanticArtifact);
            long compileMs = timingWatch.ElapsedMilliseconds;
            if (!result.IsValid)
                return result;
            if (!publish)
            {
                Debug.Log($"[计时] 重建总览(未发布) 编译 {compileMs}ms | 总 {timingWatch.ElapsedMilliseconds}ms");
                return result;
            }
            var stages = new List<ICharacterSimulationTargetPublishStage>();
            CharacterSemanticIrArtifactPublishTransaction semanticStage = null;
            try
            {
                var phaseWatch = System.Diagnostics.Stopwatch.StartNew();
                string definitionGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(request.Definition));
                semanticStage = CharacterSemanticIrArtifactStore.Stage(definitionGuid, semanticArtifact);
                stages.Add(new CharacterAclAnimationArtifactPublishStage(
                    definitionGuid,
                    result.AnimationCatalog));
                for (int i = 0; i < request.Targets.Count; i++)
                    stages.Add(request.Targets[i].Stage(definitionGuid, result.TargetProducts[i]));
                phaseWatch.Stop();
                long stageMs = phaseWatch.ElapsedMilliseconds;
                phaseWatch.Restart();
                CharacterPresentationProjection publishedProjection = Publish(
                    request.Definition,
                    semanticStage,
                    stages,
                    result.PresentationProjection,
                    result.TargetProducts[0].Contract);
                phaseWatch.Stop();
                Debug.Log(
                    $"[计时] 发布相位 Stage构造 {stageMs}ms | 投影发布 {phaseWatch.ElapsedMilliseconds}ms");
                result = new CharacterSimulationBuildResult(
                    result.Artifact,
                    result.TargetProducts,
                    publishedProjection,
                    result.Report,
                    result.AnimationCatalog);
            }
            catch (Exception exception)
            {
                for (int i = stages.Count - 1; i >= 0; i--)
                    stages[i].Dispose();
                semanticStage?.Dispose();
                result.Report.ArtifactError("artifact_group_publish_failed", AssetDatabase.GetAssetPath(request.Definition), exception.Message);
                return Failed(result.Report);
            }
            FinalizePublication(
                request.Definition,
                stages,
                result.Report);
            for (int i = 0; i < stages.Count; i++)
                stages[i].Dispose();
            semanticStage.Dispose();
            Debug.Log(
                $"[计时] 重建总览 编译 {compileMs}ms | 发布+收尾 {timingWatch.ElapsedMilliseconds - compileMs}ms | 总 {timingWatch.ElapsedMilliseconds}ms");
            return result;
        }

        static void FinalizePublication(
            CharacterPipelineDefinition definition,
            IReadOnlyList<ICharacterSimulationTargetPublishStage> stages,
            CharacterSimulationCompileReport report)
        {
            string sourceIdentity = AssetDatabase.GetAssetPath(definition);
            for (int i = 0; i < stages.Count; i++)
            {
                if (!(stages[i] is ICharacterSimulationTargetPublishFinalizer finalizer))
                    continue;
                try
                {
                    if (finalizer.TryFinalizePublication(out string warning))
                        continue;
                    Debug.LogWarning(
                        $"Character Simulation publication cleanup deferred for '{sourceIdentity}': {warning}");
                    report.Warning(
                        "target_publish_cleanup_deferred",
                        sourceIdentity,
                        warning);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        $"Character Simulation publication cleanup deferred for '{sourceIdentity}': {exception.Message}");
                    report.Warning(
                        "target_publish_cleanup_deferred",
                        sourceIdentity,
                        exception.Message);
                }
            }
        }

        public static TimelineSimulationBuildResult Build(TimelineSimulationBuildRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            bool publish = request.PublicationMode == CharacterSimulationBuildPublicationMode.Publish;
            TimelineSimulationBuildResult result = ExecuteTimeline(
                request,
                out ValidatedSemanticIrArtifact semanticArtifact);
            if (!result.IsValid || !publish)
                return result;
            var stages = new List<ICharacterSimulationTargetPublishStage>();
            CharacterSemanticIrArtifactPublishTransaction semanticStage = null;
            try
            {
                semanticStage = CharacterSemanticIrArtifactStore.Stage(result.RootGuid, semanticArtifact);
                for (int i = 0; i < request.Targets.Count; i++)
                    stages.Add(request.Targets[i].Stage(result.RootGuid, result.TargetProducts[i]));
                semanticStage.Commit();
                for (int i = 0; i < stages.Count; i++)
                    stages[i].Commit();
                for (int i = 0; i < stages.Count; i++)
                    stages[i].Complete();
                semanticStage.Complete();
            }
            catch (Exception exception)
            {
                for (int i = stages.Count - 1; i >= 0; i--)
                    stages[i].Dispose();
                semanticStage?.Dispose();
                result.Report.ArtifactError(
                    "timeline_artifact_group_publish_failed",
                    AssetDatabase.GetAssetPath(request.Timeline),
                    exception.Message);
                return FailedTimeline(result.Report);
            }
            for (int i = 0; i < stages.Count; i++)
                stages[i].Dispose();
            semanticStage.Dispose();
            return result;
        }

        public static CharacterSimulationBuildResult DryRun(CharacterPipelineDefinition definition)
        {
            return Build(new CharacterSimulationBuildRequest(
                definition,
                CharacterSimulationBuildPublicationMode.DryRun,
                CharacterSimulationTargetCatalog.DefaultEditor(definition)));
        }

        public static CharacterSimulationBuildResult DryRun(
            CharacterPipelineDefinition definition,
            IReadOnlyList<ICharacterSimulationTargetBuildAdapter> targets)
        {
            return Build(new CharacterSimulationBuildRequest(
                definition,
                CharacterSimulationBuildPublicationMode.DryRun,
                targets));
        }

        public static CharacterSemanticFrontendResult CompileSemanticIr(CharacterPipelineDefinition definition, bool persistCache)
        {
            CharacterSemanticFrontendResult result = CharacterSemanticFrontendCompiler.Compile(definition);
            if (!result.IsValid || !persistCache)
                return result;
            ValidatedSemanticIrArtifact persisted = CharacterSemanticIrArtifactStore.Write(
                result.CompilationModel.DefinitionGuid,
                result.Artifact);
            return new CharacterSemanticFrontendResult(persisted, result.CompilationModel, result.Report);
        }

        internal static CharacterPresentationProjectionCompileResult CompilePoseOnly(
            CharacterPipelineDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            return CompilePoseOnly(
                definition.AnimationPresentationProfile ??
                throw new InvalidOperationException("Animation Presentation Profile is missing."),
                definition);
        }

        internal static CharacterPresentationProjectionCompileResult CompilePoseOnly(
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definition)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            if (definition && definition.AnimationPresentationProfile != profile)
                throw new InvalidOperationException("Character Definition does not own the Animation Presentation Profile.");
            string profilePath = AssetDatabase.GetAssetPath(profile);
            string ownerGuid = AssetDatabase.AssetPathToGUID(profilePath);
            if (string.IsNullOrEmpty(ownerGuid))
                throw new InvalidOperationException("Pose-only compilation requires a persisted Animation Presentation Profile.");
            return CompilePoseOnly(
                profile,
                definition,
                CreateAnimationBuildInput(ownerGuid, profile),
                false);
        }

        internal static CharacterPresentationProjectionCompileResult CompilePoseOnly(
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definition,
            CharacterAnimationBuildInput animationBuildInput)
        {
            return CompilePoseOnly(
                profile,
                definition,
                animationBuildInput,
                false);
        }

        internal static CharacterPresentationProjectionCompileResult CompilePoseOnly(
            CharacterAnimationPresentationProfile profile,
            CharacterPipelineDefinition definition,
            CharacterAnimationBuildInput animationBuildInput,
            bool generateMissingOrStaleArtifacts)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            if (definition && definition.AnimationPresentationProfile != profile)
                throw new InvalidOperationException("Character Definition does not own the Animation Presentation Profile.");
            if (animationBuildInput == null || animationBuildInput.Profile != profile)
                throw new InvalidOperationException("Pose-only compilation received an unrelated Animation Build Input.");
            string profilePath = AssetDatabase.GetAssetPath(profile);
            string ownerGuid = AssetDatabase.AssetPathToGUID(profilePath);
            if (string.IsNullOrEmpty(ownerGuid))
                throw new InvalidOperationException("Pose-only compilation requires a persisted Animation Presentation Profile.");
            var errors = new List<string>();
            var diagnostics = new List<CharacterFootAnalysisArtifactDiagnostic>();
            CharacterAnimationInputContract animationInputContract =
                CharacterAnimationInputContract.Create(profile);
            CharacterFootPlacementAnalysisCompilation footAnalysis =
                CharacterProjectionFootAnalysisResolver.ResolvePoseOnly(
                    profile,
                    generateMissingOrStaleArtifacts,
                    diagnostics,
                    errors);
            if (errors.Count > 0 || footAnalysis == null)
                throw new InvalidOperationException(string.Join("\n", errors));
            CharacterPresentationProjectionCompileContext context =
                CreatePoseOnlyContext(ownerGuid, profile, animationInputContract);
            return CharacterPresentationProjectionCompiler.Compile(
                new CharacterPresentationProjectionCompileRequest(
                    context,
                    footAnalysis,
                    animationBuildInput.CreatePoseOnlyInput(),
                    null,
                    false));
        }

        static CharacterPresentationProjectionCompileContext CreatePoseOnlyContext(
            string ownerGuid,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationInputContract animationInputContract)
        {
            StableHash semanticHash = StableHash.Compute(
                "character-presentation-pose-only",
                ownerGuid,
                profile.PoseGraph.Graph.GraphId.Value,
                profile.PoseGraph.Graph.ContentRevision);
            var contract = new CharacterPresentationSemanticContract(
                new ProgramId($"pose:{ownerGuid}"),
                new ProgramRevision($"pose:{semanticHash}"),
                new SemanticHash(semanticHash),
                Array.Empty<ProgramProducer>());
            return new CharacterPresentationProjectionCompileContext(
                ownerGuid,
                profile,
                null,
                null,
                null,
                string.Empty,
                animationInputContract,
                contract,
                Array.Empty<CharacterPresentationProducerEntry>(),
                false);
        }

        public static TimelineSemanticFrontendResult CompileTimelineSemanticIr(TimelineAsset timeline, bool persistCache)
        {
            TimelineSemanticFrontendResult result = TimelineSemanticFrontendCompiler.Compile(timeline);
            if (!result.IsValid || !persistCache)
                return result;
            ValidatedSemanticIrArtifact persisted = CharacterSemanticIrArtifactStore.Write(
                result.RootGuid,
                result.Artifact);
            return new TimelineSemanticFrontendResult(
                persisted,
                result.Content,
                result.RootGuid,
                result.Report);
        }

        static CharacterSimulationBuildResult Execute(
            CharacterSimulationBuildRequest request,
            out ValidatedSemanticIrArtifact semanticArtifact)
        {
            semanticArtifact = null;
            CharacterPipelineDefinition definition = request.Definition;
            string definitionGuid = AssetDatabase.AssetPathToGUID(
                AssetDatabase.GetAssetPath(definition));
            var phaseWatch = System.Diagnostics.Stopwatch.StartNew();
            CharacterAnimationBuildInput animationBuildInput = null;
            CharacterPresentationProjectionCompileResult poseResult = null;
            string poseError = null;
            try
            {
                animationBuildInput = CreateAnimationBuildInput(
                    definitionGuid,
                    definition.AnimationPresentationProfile);
                poseResult = CompilePoseOnly(
                    definition.AnimationPresentationProfile,
                    definition,
                    animationBuildInput,
                    false);
            }
            catch (Exception exception)
            {
                poseError = exception.Message;
            }
            long animationInputMs = phaseWatch.ElapsedMilliseconds;
            phaseWatch.Restart();
            CharacterSemanticFrontendResult frontend = CharacterSemanticFrontendCompiler.Compile(definition);
            phaseWatch.Stop();
            long frontendMs = phaseWatch.ElapsedMilliseconds;
            CharacterSimulationCompileReport report = frontend.Report;
            if (!string.IsNullOrEmpty(poseError))
            {
                report.PresentationError(
                    "pose_compilation_failed",
                    definitionGuid,
                    poseError);
            }
            if (poseResult != null && !poseResult.IsValid)
            {
                for (int i = 0; i < poseResult.Diagnostics.Count; i++)
                {
                    CharacterPresentationProjectionDiagnostic diagnostic = poseResult.Diagnostics[i];
                    report.PresentationError(
                        diagnostic.Code,
                        definitionGuid,
                        diagnostic.Message);
                }
            }
            if (!frontend.IsValid || poseResult == null || !poseResult.IsValid)
                return Failed(report);

            ValidatedSemanticIrArtifact artifact;
            string artifactPath = CharacterSemanticIrArtifactStore.GetPath(frontend.CompilationModel.DefinitionGuid);
            try
            {
                phaseWatch.Restart();
                artifact = CharacterSemanticIrArtifactStore.RoundTrip(frontend.Artifact);
                phaseWatch.Stop();
            }
            catch (Exception exception)
            {
                report.ArtifactError("semantic_ir_validation_failed", artifactPath, exception.Message);
                return Failed(report);
            }
            long roundTripMs = phaseWatch.ElapsedMilliseconds;
            if (!artifact.Header.Root.IsCharacter ||
                !string.Equals(artifact.Header.Root.RootIdentity, frontend.CompilationModel.DefinitionGuid, StringComparison.Ordinal))
            {
                report.ArtifactError("character_root_identity_invalid", artifactPath, "Character Semantic IR root does not match the Definition cache identity.");
                return Failed(report);
            }
            semanticArtifact = artifact;

            CharacterPresentationProjection projection = CompileProjection(
                frontend.CompilationModel,
                artifact,
                animationBuildInput,
                poseResult,
                false,
                report,
                out CharacterPresentationSemanticContract frontendContract,
                out CharacterAnimationBuildCatalog animationCatalog);
            if (projection == null || !projection.IsValid || frontendContract == null || !report.IsValid)
                return Failed(report);
            var targetProducts = new List<CharacterSimulationTargetBuildProduct>(request.Targets.Count);
            phaseWatch.Restart();
            for (int i = 0; i < request.Targets.Count; i++)
            {
                ICharacterSimulationTargetBuildAdapter adapter = request.Targets[i];
                CharacterSimulationTargetBuildProduct product = adapter.Compile(
                    artifact,
                    report);
                if (product == null || !report.IsValid)
                    return Failed(report);
                if (!product.NumericProfileId.Equals(adapter.NumericProfileId))
                {
                    report.TargetError(
                        "target_product_identity_mismatch",
                        adapter.NumericProfileId.Value,
                        $"Target Adapter returned product '{product.NumericProfileId}' instead of '{adapter.NumericProfileId}'.");
                    return Failed(report);
                }
                try
                {
                    projection.RequireContract(product.Contract);
                    if (!product.Contract.ContractHash.Equals(frontendContract.ContractHash))
                        throw new InvalidDataException("Target Presentation Contract differs from the Frontend contract.");
                }
                catch (Exception exception)
                {
                    report.TargetError(
                        "target_presentation_contract_mismatch",
                        adapter.NumericProfileId.Value,
                        exception.Message);
                    return Failed(report);
                }
                targetProducts.Add(product);
            }
            phaseWatch.Stop();
            Debug.Log(
                $"[计时] 编译相位 前端 {frontendMs}ms | IR往返 {roundTripMs}ms | " +
                $"动画输入 {animationInputMs}ms | 目标产物 {phaseWatch.ElapsedMilliseconds}ms");
            var descriptor = new CharacterSemanticIrArtifactDescriptor(artifactPath, artifact.Header);
            return new CharacterSimulationBuildResult(
                descriptor,
                targetProducts,
                projection,
                report,
                animationCatalog);
        }

        static TimelineSimulationBuildResult ExecuteTimeline(
            TimelineSimulationBuildRequest request,
            out ValidatedSemanticIrArtifact semanticArtifact)
        {
            semanticArtifact = null;
            TimelineSemanticFrontendResult frontend = TimelineSemanticFrontendCompiler.Compile(request.Timeline);
            CharacterSimulationCompileReport report = frontend.Report;
            if (!frontend.IsValid)
                return FailedTimeline(report);
            string artifactPath = CharacterSemanticIrArtifactStore.GetPath(frontend.RootGuid);
            ValidatedSemanticIrArtifact artifact;
            try
            {
                artifact = CharacterSemanticIrArtifactStore.RoundTrip(frontend.Artifact);
            }
            catch (Exception exception)
            {
                report.ArtifactError("semantic_ir_validation_failed", artifactPath, exception.Message);
                return FailedTimeline(report);
            }
            if (!artifact.Header.Root.IsTimeline ||
                !string.Equals(artifact.Header.Root.RootIdentity, frontend.RootGuid, StringComparison.Ordinal) ||
                !string.Equals(artifact.Header.Root.EntryIdentity, $"timeline:{frontend.Content.Timeline.AuthoringId}", StringComparison.Ordinal) ||
                !string.Equals(artifact.Header.Root.ContentIdentity, frontend.Content.ContentUnit.ContentHash, StringComparison.Ordinal))
            {
                report.ArtifactError("timeline_root_identity_invalid", artifactPath, "Timeline Semantic IR root does not match the persisted content, entry, or cache identity.");
                return FailedTimeline(report);
            }
            semanticArtifact = artifact;
            var targetProducts = new List<CharacterSimulationTargetBuildProduct>(request.Targets.Count);
            for (int i = 0; i < request.Targets.Count; i++)
            {
                ICharacterSimulationTargetBuildAdapter adapter = request.Targets[i];
                CharacterSimulationTargetBuildProduct product = adapter.Compile(artifact, report);
                if (product == null || !report.IsValid)
                    return FailedTimeline(report);
                if (!product.NumericProfileId.Equals(adapter.NumericProfileId))
                {
                    report.TargetError(
                        "target_product_identity_mismatch",
                        adapter.NumericProfileId.Value,
                        $"Target Adapter returned product '{product.NumericProfileId}' instead of '{adapter.NumericProfileId}'.");
                    return FailedTimeline(report);
                }
                if (!ProductMatchesRoot(product, artifact.Header.Root))
                {
                    report.TargetError(
                        "target_root_identity_mismatch",
                        adapter.NumericProfileId.Value,
                        "Target Program root does not match the Timeline Semantic IR root.");
                    return FailedTimeline(report);
                }
                targetProducts.Add(product);
            }
            return new TimelineSimulationBuildResult(
                new CharacterSemanticIrArtifactDescriptor(artifactPath, artifact.Header),
                frontend.RootGuid,
                targetProducts,
                report);
        }

        static bool ProductMatchesRoot(
            CharacterSimulationTargetBuildProduct product,
            SimulationProgramRootDescriptor root)
        {
            return product switch
            {
                Float32CharacterSimulationTargetBuildProduct float32 => float32.Program.Manifest.Root == root,
                FixedCharacterSimulationTargetBuildProduct fixedTarget => fixedTarget.Program.Manifest.Root == root,
                _ => false
            };
        }

        static CharacterPresentationProjection CompileProjection(
            CharacterAuthoringCompilationModel model,
            ValidatedSemanticIrArtifact artifact,
            CharacterAnimationBuildInput animationBuildInput,
            CharacterPresentationProjectionCompileResult poseResult,
            bool generateMissingOrStaleArtifacts,
            CharacterSimulationCompileReport report,
            out CharacterPresentationSemanticContract contract,
            out CharacterAnimationBuildCatalog animationCatalog)
        {
            contract = null;
            animationCatalog = null;
            var errors = new List<string>();
            var footAnalysisDiagnostics = new List<CharacterFootAnalysisArtifactDiagnostic>();
            var phaseWatch = System.Diagnostics.Stopwatch.StartNew();
            CharacterFootPlacementAnalysisCompilation footAnalysis =
                CharacterProjectionFootAnalysisResolver.Resolve(
                    model.AnimationPresentationProfile,
                    model.Timelines,
                    generateMissingOrStaleArtifacts,
                    footAnalysisDiagnostics,
                    errors);
            phaseWatch.Stop();
            long footAnalysisMs = phaseWatch.ElapsedMilliseconds;
            for (int i = 0; i < footAnalysisDiagnostics.Count; i++)
            {
                CharacterFootAnalysisArtifactDiagnostic diagnostic = footAnalysisDiagnostics[i];
                report.PresentationError(
                    ArtifactDiagnosticCode(diagnostic.Status),
                    diagnostic.BindingKey,
                    diagnostic.Message);
            }
            if (footAnalysis == null)
            {
                for (int i = 0; i < errors.Count; i++)
                    report.PresentationError("presentation_projection_invalid", artifact.Header.ProgramId.Value, errors[i]);
                return null;
            }
            for (int i = 0; i < errors.Count; i++)
                report.PresentationError("presentation_projection_invalid", artifact.Header.ProgramId.Value, errors[i]);
            if (!report.IsValid)
                return null;
            phaseWatch.Restart();
            CharacterPresentationSemanticReader reader = new CharacterPresentationSemanticReader(artifact);
            CharacterAnimationInputContract animationInputContract =
                CharacterAnimationInputContract.Create(model.AnimationPresentationProfile);
            IReadOnlyDictionary<string, IReadOnlyList<CharacterPresentationTimelineCallSite>> timelineCallSites =
                CharacterPresentationProducerCompiler.CollectTimelineCallSites(model);
            var producerErrors = new List<string>();
            IReadOnlyList<CharacterPresentationProducerEntry> producerEntries =
                CharacterPresentationProducerCompiler.CompileEntries(
                    reader,
                    model.AnimationPresentationProfile,
                    footAnalysis,
                    model.Timelines,
                    timelineCallSites,
                    animationBuildInput,
                    producerErrors);
            for (int i = 0; i < producerErrors.Count; i++)
                report.PresentationError(
                    "presentation_producer_invalid",
                    artifact.Header.ProgramId.Value,
                    producerErrors[i]);
            if (!report.IsValid)
                return null;
            CharacterPresentationProjectionCompileContext context =
                new CharacterPresentationProjectionCompileContext(
                    model.DefinitionGuid,
                    model.AnimationPresentationProfile,
                    model.Definition.CameraProfile,
                    model.Definition.EquipmentProfile,
                    model.Definition.EquipmentPresentationProfile,
                    model.Definition.ControlModuleId,
                    animationInputContract,
                    reader.Contract,
                    producerEntries,
                    true);
            CharacterPresentationProjectionCompileResult compileResult =
                CharacterPresentationProjectionCompiler.Compile(
                    new CharacterPresentationProjectionCompileRequest(
                        context,
                        footAnalysis,
                        animationBuildInput,
                        poseResult));
            phaseWatch.Stop();
            Debug.Log(
                $"[计时] 投影编译 足部分析 {footAnalysisMs}ms | 投影 {phaseWatch.ElapsedMilliseconds}ms");
            contract = compileResult.Contract;
            animationCatalog = compileResult.AnimationCatalog;
            for (int i = 0; i < compileResult.Diagnostics.Count; i++)
            {
                CharacterPresentationProjectionDiagnostic diagnostic = compileResult.Diagnostics[i];
                report.PresentationError(diagnostic.Code, diagnostic.Identity, diagnostic.Message);
            }
            CharacterPresentationProjection projection = compileResult.Projection;
            if (!report.IsValid)
                return projection;
            return projection;
        }

        static CharacterAnimationBuildInput CreateAnimationBuildInput(
            string definitionGuid,
            CharacterAnimationPresentationProfile profile)
        {
            CharacterAnimationInputContract animationInputContract =
                CharacterAnimationInputContract.Create(profile);
            return CreateAnimationBuildInput(
                definitionGuid,
                profile,
                profile?.SourceResourceBindings ??
                throw new InvalidOperationException("Animation Presentation Profile is missing."),
                CreatePoseSourceRig(profile),
                animationInputContract);
        }

        static CharacterAnimationBuildInput CreateAnimationBuildInput(
            string definitionGuid,
            CharacterAnimationPresentationProfile profile,
            IReadOnlyList<CharacterAnimationSourceResourceBinding> sourceResourceBindings,
            CharacterAnimationSourceRig sourceRig,
            CharacterAnimationInputContract animationInputContract)
        {
            if (string.IsNullOrEmpty(definitionGuid))
                throw new ArgumentException("Definition identity is required.", nameof(definitionGuid));
            profile = profile ??
                throw new InvalidOperationException("Animation Presentation Profile is missing.");
            animationInputContract = animationInputContract ??
                throw new ArgumentNullException(nameof(animationInputContract));
            if (!animationInputContract.Matches(profile))
                throw new InvalidOperationException("Animation Input Contract does not match the Animation Presentation Profile.");
            CharacterAnimationRigPayload rig = new CharacterAnimationRigPayload(
                profile.RigDefinition ??
                throw new InvalidOperationException("Animation Rig Definition is missing."));
            CharacterAnimationParameterLayout layout =
                CharacterAnimationParameterLayoutCompiler.Build(animationInputContract);
            return new CharacterAnimationBuildInput(
                definitionGuid,
                CharacterAnimationBuildInput.RequireNativeArtifactIdentity(),
                profile,
                sourceRig,
                layout,
                profile.AnimationCompression ??
                throw new InvalidOperationException("Animation compression settings are missing."),
                sourceResourceBindings);
        }

        static CharacterAnimationSourceRig CreatePoseSourceRig(
            CharacterAnimationPresentationProfile profile)
        {
            string sourcePath = AssetDatabase.GUIDToAssetPath(
                profile.FootPlacementAnalysisSourceAssetGuid);
            CharacterFootPlacementAnalysisSource source =
                AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(sourcePath);
            if (!source)
                throw new InvalidOperationException("Pose-only compilation requires a persisted Foot Analysis Source.");
            source.RequireCalibrationAuthoringInput();
            string rigPath = AssetDatabase.GUIDToAssetPath(source.SamplingRigAssetGuid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            if (!prefab)
                throw new InvalidOperationException("Pose-only compilation requires a persisted sampling Rig Prefab.");
            CharacterAnimationRigPayload rig = new CharacterAnimationRigPayload(
                profile.RigDefinition ??
                throw new InvalidOperationException("Animation Rig Definition is missing."));
            CharacterAnimationRigBinding binding =
                prefab.GetComponentInChildren<CharacterAnimationRigBinding>(true);
            if (!binding)
                throw new InvalidOperationException("Pose-only sampling Rig Prefab has no Animation Rig Binding.");
            return CharacterAnimationSourceRig.FromRuntimeRig(rig, binding);
        }

        static string ArtifactDiagnosticCode(AnimationFootAnalysisArtifactStatus status)
        {
            return status switch
            {
                AnimationFootAnalysisArtifactStatus.Missing => "foot_analysis_artifact_missing",
                AnimationFootAnalysisArtifactStatus.Stale => "foot_analysis_artifact_stale",
                AnimationFootAnalysisArtifactStatus.Corrupt => "foot_analysis_artifact_corrupt",
                _ => "foot_analysis_artifact_invalid"
            };
        }

        static CharacterPresentationProjection Publish(
            CharacterPipelineDefinition definition,
            CharacterSemanticIrArtifactPublishTransaction semanticStage,
            IReadOnlyList<ICharacterSimulationTargetPublishStage> stages,
            CharacterPresentationProjection projection,
            CharacterPresentationSemanticContract contract)
        {
            if (semanticStage == null)
                throw new ArgumentNullException(nameof(semanticStage));
            if (stages == null || stages.Count == 0)
                throw new ArgumentException("Character Simulation publication requires Target stages.", nameof(stages));
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            string projectionPath = ProjectionAssetPath(definition);
            EnsureFolder(Path.GetDirectoryName(projectionPath)?.Replace('\\', '/'));
            CharacterPresentationProjectionAsset projectionAsset =
                AssetDatabase.LoadAssetAtPath<CharacterPresentationProjectionAsset>(projectionPath);
            bool createProjection = !projectionAsset;
            string definitionBackup = EditorJsonUtility.ToJson(definition);
            string projectionBackup = createProjection ? string.Empty : EditorJsonUtility.ToJson(projectionAsset);
            if (createProjection)
            {
                projectionAsset = ScriptableObject.CreateInstance<CharacterPresentationProjectionAsset>();
                projectionAsset.name = $"{definition.name} Presentation Projection";
            }
            try
            {
                semanticStage.Commit();
                CharacterSimulationProgramAsset float32ProgramAsset = null;
                for (int i = 0; i < stages.Count; i++)
                {
                    stages[i].Commit();
                    if (stages[i].Wrapper is CharacterSimulationProgramAsset programAsset)
                        float32ProgramAsset = programAsset;
                }
                projectionAsset.SetCompiledProjection(projection);
                if (createProjection)
                    AssetDatabase.CreateAsset(projectionAsset, projectionPath);
                EditorUtility.SetDirty(projectionAsset);
                AssetDatabase.SaveAssetIfDirty(projectionAsset);
                if (float32ProgramAsset && definition.SimulationProgram != float32ProgramAsset)
                    definition.SetSimulationProgram(float32ProgramAsset);
                if (definition.PresentationProjection != projectionAsset)
                    definition.SetPresentationProjection(projectionAsset);
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssetIfDirty(definition);
                CharacterPresentationProjection publishedProjection = projectionAsset.Load(contract);
                for (int i = 0; i < stages.Count; i++)
                    stages[i].Complete();
                semanticStage.Complete();
                return publishedProjection;
            }
            catch
            {
                if (createProjection && AssetDatabase.LoadAssetAtPath<CharacterPresentationProjectionAsset>(projectionPath))
                    AssetDatabase.DeleteAsset(projectionPath);
                else if (!createProjection)
                {
                    EditorJsonUtility.FromJsonOverwrite(projectionBackup, projectionAsset);
                    EditorUtility.SetDirty(projectionAsset);
                    AssetDatabase.SaveAssetIfDirty(projectionAsset);
                }
                EditorJsonUtility.FromJsonOverwrite(definitionBackup, definition);
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssetIfDirty(definition);
                for (int i = stages.Count - 1; i >= 0; i--)
                    stages[i].Rollback();
                semanticStage.Rollback();
                throw;
            }
        }

        static CharacterSimulationBuildResult Failed(CharacterSimulationCompileReport report)
        {
            return new CharacterSimulationBuildResult(
                null,
                Array.Empty<CharacterSimulationTargetBuildProduct>(),
                null,
                report);
        }

        static TimelineSimulationBuildResult FailedTimeline(CharacterSimulationCompileReport report)
        {
            return new TimelineSimulationBuildResult(
                null,
                string.Empty,
                Array.Empty<CharacterSimulationTargetBuildProduct>(),
                report);
        }

        static string ProjectionAssetPath(CharacterPipelineDefinition definition)
        {
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string directory = Path.GetDirectoryName(definitionPath)?.Replace('\\', '/') ?? "Assets";
            return $"{directory}/Generated/{definition.name}.PresentationProjection.asset";
        }

        static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
