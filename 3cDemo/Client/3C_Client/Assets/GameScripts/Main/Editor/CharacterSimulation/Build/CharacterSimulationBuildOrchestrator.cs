using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class CharacterSimulationBuildOrchestrator
    {
        public static CharacterSimulationBuildResult Build(CharacterPipelineDefinition definition)
        {
            return Build(new CharacterSimulationBuildRequest(
                definition,
                CharacterSimulationBuildPublicationMode.Publish,
                CharacterSimulationTargetCatalog.DefaultEditor(definition)));
        }

        public static CharacterSimulationBuildResult Build(CharacterSimulationBuildRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            bool publish = request.PublicationMode == CharacterSimulationBuildPublicationMode.Publish;
            CharacterSimulationBuildResult result = Execute(
                request,
                out ValidatedSemanticIrArtifact semanticArtifact);
            if (!result.IsValid)
                return result;
            if (!publish)
                return result;
            var stages = new List<ICharacterSimulationTargetPublishStage>();
            CharacterSemanticIrArtifactPublishTransaction semanticStage = null;
            try
            {
                string definitionGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(request.Definition));
                semanticStage = CharacterSemanticIrArtifactStore.Stage(definitionGuid, semanticArtifact);
                for (int i = 0; i < request.Targets.Count; i++)
                    stages.Add(request.Targets[i].Stage(definitionGuid, result.TargetProducts[i]));
                CharacterPresentationProjection publishedProjection = Publish(
                    request.Definition,
                    semanticStage,
                    stages,
                    result.PresentationProjection,
                    result.TargetProducts[0].Contract);
                result = new CharacterSimulationBuildResult(
                    result.Artifact,
                    result.TargetProducts,
                    publishedProjection,
                    result.Report);
            }
            catch (Exception exception)
            {
                for (int i = stages.Count - 1; i >= 0; i--)
                    stages[i].Dispose();
                semanticStage?.Dispose();
                result.Report.ArtifactError("artifact_group_publish_failed", AssetDatabase.GetAssetPath(request.Definition), exception.Message);
                return Failed(result.Report);
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

        static CharacterSimulationBuildResult Execute(
            CharacterSimulationBuildRequest request,
            out ValidatedSemanticIrArtifact semanticArtifact)
        {
            semanticArtifact = null;
            CharacterPipelineDefinition definition = request.Definition;
            CharacterSemanticFrontendResult frontend = CharacterSemanticFrontendCompiler.Compile(definition);
            CharacterSimulationCompileReport report = frontend.Report;
            if (!frontend.IsValid)
                return Failed(report);

            ValidatedSemanticIrArtifact artifact;
            string artifactPath = CharacterSemanticIrArtifactStore.GetPath(frontend.CompilationModel.DefinitionGuid);
            try
            {
                artifact = CharacterSemanticIrArtifactStore.RoundTrip(frontend.Artifact);
            }
            catch (Exception exception)
            {
                report.ArtifactError("semantic_ir_validation_failed", artifactPath, exception.Message);
                return Failed(report);
            }
            semanticArtifact = artifact;

            CharacterPresentationProjection projection = CompileProjection(
                frontend.CompilationModel,
                artifact,
                request.PublicationMode == CharacterSimulationBuildPublicationMode.Publish,
                report,
                out CharacterPresentationSemanticContract frontendContract);
            if (projection == null || !projection.IsValid || frontendContract == null || !report.IsValid)
                return Failed(report);
            var targetProducts = new List<CharacterSimulationTargetBuildProduct>(request.Targets.Count);
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
            var descriptor = new CharacterSemanticIrArtifactDescriptor(artifactPath, artifact.Header);
            return new CharacterSimulationBuildResult(descriptor, targetProducts, projection, report);
        }

        static CharacterPresentationProjection CompileProjection(
            CharacterAuthoringCompilationModel model,
            ValidatedSemanticIrArtifact artifact,
            bool generateMissingOrStaleArtifacts,
            CharacterSimulationCompileReport report,
            out CharacterPresentationSemanticContract contract)
        {
            contract = null;
            var errors = new List<string>();
            var footAnalysisDiagnostics = new List<CharacterFootAnalysisArtifactDiagnostic>();
            CharacterFootPlacementAnalysisCompilation footAnalysis =
                CharacterProjectionFootAnalysisResolver.Resolve(
                    model.AnimationPresentationProfile,
                    model.Timelines,
                    generateMissingOrStaleArtifacts,
                    footAnalysisDiagnostics,
                    errors);
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
            CharacterPresentationProjectionCompileResult compileResult =
                CharacterPresentationProjectionCompiler.Compile(
                    new CharacterPresentationProjectionCompileRequest(
                        artifact,
                        model,
                        footAnalysis));
            contract = compileResult.Contract;
            for (int i = 0; i < compileResult.Diagnostics.Count; i++)
            {
                CharacterPresentationProjectionDiagnostic diagnostic = compileResult.Diagnostics[i];
                report.PresentationError(diagnostic.Code, diagnostic.Identity, diagnostic.Message);
            }
            CharacterPresentationProjection projection = compileResult.Projection;
            if (!report.IsValid)
                return projection;
            if (!string.Equals(projection.ProgramId, artifact.Header.ProgramId.Value, StringComparison.Ordinal) ||
                !string.Equals(projection.SourceRevision, artifact.Header.SourceRevision.Value, StringComparison.Ordinal) ||
                !string.Equals(projection.SemanticHash, artifact.Header.SemanticHash.ToString(), StringComparison.Ordinal) ||
                !string.Equals(projection.ContractHash, contract.ContractHash.ToString(), StringComparison.Ordinal))
            {
                report.PresentationError("presentation_identity_mismatch", artifact.Header.ProgramId.Value, "Projection identity does not match the Semantic IR presentation contract.");
                return projection;
            }
            if (projection.Producers.Count != artifact.SemanticIr.Producers.Count)
            {
                report.PresentationError("presentation_producer_set_mismatch", artifact.Header.ProgramId.Value, "Projection producer count does not match the Semantic IR artifact.");
                return projection;
            }
            for (int i = 0; i < projection.Producers.Count; i++)
            {
                CharacterPresentationProducerEntry projected = projection.Producers[i];
                ProgramProducer semantic = artifact.SemanticIr.Producers[i];
                if (projected.ProgramProducerIndex != i || semantic.Index != i ||
                    !string.Equals(projected.ProgramProducerIdentity, semantic.Identity, StringComparison.Ordinal))
                {
                    report.PresentationError("presentation_producer_identity_mismatch", projected.ProgramProducerIdentity, $"Producer index {i} is not identical across Semantic IR and Projection.");
                }
            }
            return projection;
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
