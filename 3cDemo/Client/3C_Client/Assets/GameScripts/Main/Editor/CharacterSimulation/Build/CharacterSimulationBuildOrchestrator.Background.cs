using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class CharacterSimulationBackgroundBuildTargetIdentity
    {
        internal CharacterSimulationBackgroundBuildTargetIdentity(
            NumericProfileId numericProfileId,
            ProgramId programId,
            ProgramRevision sourceRevision,
            SemanticHash semanticHash,
            ProgramHash programHash,
            LayoutHash layoutHash,
            int sourceMapEntryCount,
            StableHash presentationContractHash,
            string presentationProjectionRevision)
        {
            if (!numericProfileId.IsValid || !programId.IsValid || string.IsNullOrWhiteSpace(sourceRevision.Value) ||
                !semanticHash.Value.IsValid || !programHash.IsValid || !layoutHash.IsValid ||
                sourceMapEntryCount < 0 || !presentationContractHash.IsValid)
            {
                throw new ArgumentException("Background build target identity is incomplete.");
            }
            NumericProfileId = numericProfileId;
            ProgramId = programId;
            SourceRevision = sourceRevision;
            SemanticHash = semanticHash;
            ProgramHash = programHash;
            LayoutHash = layoutHash;
            SourceMapEntryCount = sourceMapEntryCount;
            PresentationContractHash = presentationContractHash;
            PresentationProjectionRevision = presentationProjectionRevision ?? string.Empty;
        }

        public NumericProfileId NumericProfileId { get; }
        public ProgramId ProgramId { get; }
        public ProgramRevision SourceRevision { get; }
        public SemanticHash SemanticHash { get; }
        public ProgramHash ProgramHash { get; }
        public LayoutHash LayoutHash { get; }
        public int SourceMapEntryCount { get; }
        public StableHash PresentationContractHash { get; }
        public string PresentationProjectionRevision { get; }
    }

    public sealed class CharacterSimulationBackgroundBuildResult
    {
        internal CharacterSimulationBackgroundBuildResult(
            ValidatedSemanticIrArtifact artifact,
            string rootGuid,
            IReadOnlyList<ICharacterSimulationTargetBuildAdapter> targets,
            IReadOnlyList<CharacterSimulationTargetBuildProduct> products,
            CharacterSimulationCompileReport report,
            CharacterPresentationProjection presentationProjection = null,
            CharacterAnimationBuildCatalog animationCatalog = null)
        {
            Artifact = artifact;
            RootGuid = rootGuid ?? string.Empty;
            Targets = targets ?? Array.Empty<ICharacterSimulationTargetBuildAdapter>();
            Products = products ?? Array.Empty<CharacterSimulationTargetBuildProduct>();
            TargetIdentities = BuildTargetIdentities(
                Artifact,
                Products,
                presentationProjection?.ProjectionRevision);
            Report = report ?? throw new ArgumentNullException(nameof(report));
            PresentationProjection = presentationProjection;
            AnimationCatalog = animationCatalog;
        }

        internal ValidatedSemanticIrArtifact Artifact { get; }
        public string RootGuid { get; }
        public IReadOnlyList<ICharacterSimulationTargetBuildAdapter> Targets { get; }
        public IReadOnlyList<CharacterSimulationTargetBuildProduct> Products { get; }
        public IReadOnlyList<CharacterSimulationBackgroundBuildTargetIdentity> TargetIdentities { get; }
        public CharacterSimulationCompileReport Report { get; }
        internal CharacterPresentationProjection PresentationProjection { get; }
        internal CharacterAnimationBuildCatalog AnimationCatalog { get; }
        bool IsCharacterBuild => Artifact != null && Artifact.Header.Root.IsCharacter;
        public bool IsValid => Artifact != null &&
                               !string.IsNullOrEmpty(RootGuid) &&
                               Products.Count == Targets.Count &&
                               TargetIdentities.Count == Products.Count &&
                               Products.Count > 0 &&
                               (!IsCharacterBuild ||
                                PresentationProjection != null &&
                                PresentationProjection.IsValid &&
                                AnimationCatalog != null) &&
                               Report.IsValid;

        static IReadOnlyList<CharacterSimulationBackgroundBuildTargetIdentity> BuildTargetIdentities(
            ValidatedSemanticIrArtifact artifact,
            IReadOnlyList<CharacterSimulationTargetBuildProduct> products,
            string presentationProjectionRevision)
        {
            if (artifact == null || products == null)
                return Array.Empty<CharacterSimulationBackgroundBuildTargetIdentity>();
            var identities = new List<CharacterSimulationBackgroundBuildTargetIdentity>(products.Count);
            for (int i = 0; i < products.Count; i++)
            {
                CharacterSimulationTargetBuildProduct product = products[i];
                if (product is Float32CharacterSimulationTargetBuildProduct floatProduct)
                {
                    identities.Add(new CharacterSimulationBackgroundBuildTargetIdentity(
                        floatProduct.NumericProfileId,
                        floatProduct.Program.Manifest.ProgramId,
                        floatProduct.Program.Manifest.SourceRevision,
                        floatProduct.Program.Manifest.SemanticHash,
                        floatProduct.Program.ProgramHash,
                        floatProduct.Program.LayoutHash,
                        floatProduct.Program.SourceMap.Count,
                        product.Contract.ContractHash,
                        presentationProjectionRevision));
                    continue;
                }
                if (product is FixedCharacterSimulationTargetBuildProduct fixedProduct)
                {
                    identities.Add(new CharacterSimulationBackgroundBuildTargetIdentity(
                        fixedProduct.NumericProfileId,
                        fixedProduct.Program.Manifest.ProgramId,
                        fixedProduct.Program.Manifest.SourceRevision,
                        fixedProduct.Program.Manifest.SemanticHash,
                        fixedProduct.Program.ProgramHash,
                        fixedProduct.Program.LayoutHash,
                        fixedProduct.Program.SourceMap.Count,
                        product.Contract.ContractHash,
                        presentationProjectionRevision));
                    continue;
                }
                throw new InvalidOperationException($"Unsupported Character Simulation Target product '{product?.GetType().FullName}'.");
            }
            return identities.AsReadOnly();
        }
    }

    public static partial class CharacterSimulationBuildOrchestrator
    {
        public static Task<CharacterSimulationBackgroundBuildResult> StartBackgroundCharacter(
            CharacterPipelineDefinition definition,
            IReadOnlyList<ICharacterSimulationTargetBuildAdapter> targets)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            if (targets == null || targets.Count == 0)
                throw new ArgumentException("Background Character build requires target adapters.", nameof(targets));
            CharacterSemanticFrontendResult frontend = CharacterSemanticFrontendCompiler.Compile(definition);
            if (!frontend.IsValid)
            {
                return Task.FromResult(new CharacterSimulationBackgroundBuildResult(
                    null,
                    frontend.CompilationModel?.DefinitionGuid,
                    targets,
                    Array.Empty<CharacterSimulationTargetBuildProduct>(),
                    frontend.Report));
            }
            ValidatedSemanticIrArtifact artifact;
            try
            {
                artifact = CharacterSemanticIrArtifactStore.RoundTrip(frontend.Artifact);
            }
            catch (Exception exception)
            {
                frontend.Report.ArtifactError(
                    "semantic_ir_validation_failed",
                    frontend.CompilationModel.DefinitionPath,
                    exception.Message);
                return Task.FromResult(new CharacterSimulationBackgroundBuildResult(
                    null,
                    frontend.CompilationModel.DefinitionGuid,
                    targets,
                    Array.Empty<CharacterSimulationTargetBuildProduct>(),
                    frontend.Report));
            }
            if (!artifact.Header.Root.IsCharacter ||
                !string.Equals(
                    artifact.Header.Root.RootIdentity,
                    frontend.CompilationModel.DefinitionGuid,
                    StringComparison.Ordinal))
            {
                frontend.Report.ArtifactError(
                    "character_root_identity_invalid",
                    frontend.CompilationModel.DefinitionPath,
                    "Character Semantic IR root does not match the Definition cache identity.");
                return Task.FromResult(new CharacterSimulationBackgroundBuildResult(
                    artifact,
                    frontend.CompilationModel.DefinitionGuid,
                    targets,
                    Array.Empty<CharacterSimulationTargetBuildProduct>(),
                    frontend.Report));
            }
            if (!TryCompileBackgroundPresentation(
                    definition,
                    frontend.CompilationModel,
                    artifact,
                    frontend.Report,
                    out CharacterPresentationProjection presentationProjection,
                    out CharacterAnimationBuildCatalog animationCatalog))
            {
                return Task.FromResult(new CharacterSimulationBackgroundBuildResult(
                    artifact,
                    frontend.CompilationModel.DefinitionGuid,
                    targets,
                    Array.Empty<CharacterSimulationTargetBuildProduct>(),
                    frontend.Report,
                    presentationProjection,
                    animationCatalog));
            }
            return CompileTargetsAsync(
                artifact,
                frontend.CompilationModel.DefinitionGuid,
                targets,
                presentationProjection,
                animationCatalog,
                frontend.Report);
        }

        public static Task<CharacterSimulationBackgroundBuildResult> StartBackgroundTimeline(
            TimelineAsset timeline,
            IReadOnlyList<ICharacterSimulationTargetBuildAdapter> targets)
        {
            if (!timeline)
                throw new ArgumentNullException(nameof(timeline));
            if (targets == null || targets.Count == 0)
                throw new ArgumentException("Background Timeline build requires target adapters.", nameof(targets));
            TimelineSemanticFrontendResult frontend = TimelineSemanticFrontendCompiler.Compile(timeline);
            if (!frontend.IsValid)
            {
                return Task.FromResult(new CharacterSimulationBackgroundBuildResult(
                    null,
                    frontend.RootGuid,
                    targets,
                    Array.Empty<CharacterSimulationTargetBuildProduct>(),
                    frontend.Report));
            }
            return CompileTargetsAsync(
                frontend.Artifact,
                frontend.RootGuid,
                targets,
                null,
                null,
                frontend.Report);
        }

        public static void PublishBackground(
            CharacterSimulationBackgroundBuildResult result,
            CharacterPipelineDefinition definition)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            if (!result.IsValid)
                throw new InvalidOperationException("Background Character Simulation build is invalid and cannot be published.");
            CharacterSemanticIrArtifactPublishTransaction semanticStage = null;
            var stages = new List<ICharacterSimulationTargetPublishStage>(result.Products.Count);
            try
            {
                semanticStage = CharacterSemanticIrArtifactStore.Stage(result.RootGuid, result.Artifact);
                stages.Add(new CharacterAclAnimationArtifactPublishStage(
                    result.RootGuid,
                    result.AnimationCatalog));
                for (int i = 0; i < result.Products.Count; i++)
                    stages.Add(result.Targets[i].Stage(result.RootGuid, result.Products[i]));
                Publish(
                    definition,
                    semanticStage,
                    stages,
                    result.PresentationProjection,
                    result.Products[0].Contract);
                FinalizePublication(definition, stages, result.Report);
            }
            catch
            {
                for (int i = stages.Count - 1; i >= 0; i--)
                    stages[i].Dispose();
                semanticStage?.Dispose();
                throw;
            }
            finally
            {
                for (int i = 0; i < stages.Count; i++)
                    stages[i].Dispose();
                semanticStage?.Dispose();
            }
            AssetDatabase.Refresh();
        }

        public static bool PublishBackgroundAndQueueAdoption(
            CharacterSimulationBackgroundBuildResult result,
            CharacterPipelineHost host,
            out string error)
        {
            error = string.Empty;
            try
            {
                if (host == null)
                {
                    error = "Background Program adoption requires a live Character Pipeline Host.";
                    return false;
                }
                ProgramRevision currentRevision = CharacterSemanticFrontendCompiler.ComputeSourceRevision(host.Definition);
                if (!currentRevision.Equals(result.Artifact.Header.SourceRevision))
                {
                    error = "Authoring SourceRevision changed while the background Program build was running.";
                    return false;
                }
                Float32CharacterSimulationTargetBuildProduct floatProduct = null;
                for (int i = 0; i < result.Products.Count; i++)
                {
                    if (result.Products[i] is Float32CharacterSimulationTargetBuildProduct candidate)
                    {
                        floatProduct = candidate;
                        break;
                    }
                }
                if (floatProduct == null)
                {
                    error = "Background Program adoption requires a Float32 Character Program product.";
                    return false;
                }
                if (!host.TryPrepareCurrentProgramAdoption(
                        floatProduct.Program,
                        result.PresentationProjection,
                        out error))
                    return false;
                try
                {
                    PublishBackground(result, host.Definition);
                }
                catch
                {
                    host.DiscardPreparedProgramAdoption();
                    throw;
                }
                if (!host.TryQueuePreparedProgramAdoption(
                        floatProduct.Program.Manifest.SourceRevision,
                        out error))
                    return false;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        static Task<CharacterSimulationBackgroundBuildResult> CompileTargetsAsync(
            ValidatedSemanticIrArtifact artifact,
            string rootGuid,
            IReadOnlyList<ICharacterSimulationTargetBuildAdapter> targets,
            CharacterPresentationProjection presentationProjection,
            CharacterAnimationBuildCatalog animationCatalog,
            CharacterSimulationCompileReport report)
        {
            var targetCopy = new ICharacterSimulationTargetBuildAdapter[targets.Count];
            for (int i = 0; i < targetCopy.Length; i++)
                targetCopy[i] = targets[i] ?? throw new ArgumentException("Background build target is missing.", nameof(targets));
            return Task.Run(() =>
            {
                report = report ?? new CharacterSimulationCompileReport();
                var products = new List<CharacterSimulationTargetBuildProduct>(targetCopy.Length);
                for (int i = 0; i < targetCopy.Length; i++)
                {
                    CharacterSimulationTargetBuildProduct product = targetCopy[i].Compile(artifact, report);
                    if (product == null || !report.IsValid)
                        return new CharacterSimulationBackgroundBuildResult(
                            artifact,
                            rootGuid,
                            targetCopy,
                            products,
                            report,
                            presentationProjection,
                            animationCatalog);
                    if (!product.NumericProfileId.Equals(targetCopy[i].NumericProfileId))
                    {
                        report.TargetError(
                            "background_target_product_identity_mismatch",
                            targetCopy[i].NumericProfileId.Value,
                            $"Target returned '{product.NumericProfileId}' instead of '{targetCopy[i].NumericProfileId}'.");
                        return new CharacterSimulationBackgroundBuildResult(
                            artifact,
                            rootGuid,
                            targetCopy,
                            products,
                            report,
                            presentationProjection,
                            animationCatalog);
                    }
                    if (presentationProjection != null)
                    {
                        try
                        {
                            presentationProjection.RequireContract(product.Contract);
                        }
                        catch (Exception exception)
                        {
                            report.TargetError(
                                "background_presentation_contract_mismatch",
                                targetCopy[i].NumericProfileId.Value,
                                exception.Message);
                            return new CharacterSimulationBackgroundBuildResult(
                                artifact,
                                rootGuid,
                                targetCopy,
                                products,
                                report,
                                presentationProjection,
                                animationCatalog);
                        }
                    }
                    products.Add(product);
                }
                return new CharacterSimulationBackgroundBuildResult(
                    artifact,
                    rootGuid,
                    targetCopy,
                    products,
                    report,
                    presentationProjection,
                    animationCatalog);
            });
        }

        static bool TryCompileBackgroundPresentation(
            CharacterPipelineDefinition definition,
            CharacterAuthoringCompilationModel model,
            ValidatedSemanticIrArtifact artifact,
            CharacterSimulationCompileReport report,
            out CharacterPresentationProjection projection,
            out CharacterAnimationBuildCatalog animationCatalog)
        {
            projection = null;
            animationCatalog = null;
            string definitionGuid = model.DefinitionGuid;
            try
            {
                CharacterAnimationBuildInput animationBuildInput = CreateAnimationBuildInput(
                    definitionGuid,
                    definition.AnimationPresentationProfile);
                CharacterPresentationProjectionCompileResult poseResult = CompilePoseOnly(
                    definition.AnimationPresentationProfile,
                    definition,
                    animationBuildInput,
                    true);
                if (poseResult == null || !poseResult.IsValid)
                {
                    AppendPresentationDiagnostics(report, definitionGuid, poseResult);
                    return false;
                }
                projection = CompileProjection(
                    model,
                    artifact,
                    animationBuildInput,
                    poseResult,
                    true,
                    report,
                    out CharacterPresentationSemanticContract contract,
                    out animationCatalog);
                return projection != null &&
                       projection.IsValid &&
                       contract != null &&
                       animationCatalog != null &&
                       report.IsValid;
            }
            catch (Exception exception)
            {
                report.PresentationError(
                    "presentation_projection_failed",
                    definitionGuid,
                    exception.Message);
                return false;
            }
        }

        static void AppendPresentationDiagnostics(
            CharacterSimulationCompileReport report,
            string sourceIdentity,
            CharacterPresentationProjectionCompileResult result)
        {
            if (result == null)
                return;
            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                CharacterPresentationProjectionDiagnostic diagnostic = result.Diagnostics[i];
                report.PresentationError(
                    diagnostic.Code,
                    string.IsNullOrEmpty(diagnostic.Identity) ? sourceIdentity : diagnostic.Identity,
                    diagnostic.Message);
            }
        }
    }
}
