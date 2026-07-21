using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class CharacterSemanticFrontendCompiler
    {
        public const string CompilerVersion = "character-simulation-compiler/21";
        public static readonly OperationSetVersion OperationSetVersion = CharacterGameplayOperationSet.Version;

        public static CharacterSemanticFrontendResult Compile(CharacterPipelineDefinition definition)
        {
            var report = new CharacterSimulationCompileReport();
            if (!TryResolveRoot(definition, report, out string definitionPath, out string definitionGuid, out BaseTree root))
                return CharacterSemanticFrontendResult.Failed(report);
            ProgramRevision sourceRevision;
            try
            {
                sourceRevision = ComputeSourceRevision(definitionPath);
            }
            catch (Exception exception)
            {
                report.DiscoveryError("source_revision_failed", definitionPath, exception.Message);
                return CharacterSemanticFrontendResult.Failed(report);
            }

            CharacterAuthoringCompilationModel firstModel = Discover(definition, definitionPath, definitionGuid, sourceRevision, root, report);
            CharacterGameplaySemanticIr firstIr = Emit(firstModel, report);
            ValidatedSemanticIrArtifact firstArtifact = ValidateArtifact(firstIr, report, definitionPath);
            if (firstModel == null || firstArtifact == null || !report.IsValid)
                return CharacterSemanticFrontendResult.Failed(report);

            var verificationReport = new CharacterSimulationCompileReport();
            CharacterAuthoringCompilationModel secondModel = Discover(definition, definitionPath, definitionGuid, sourceRevision, root, verificationReport);
            CharacterGameplaySemanticIr secondIr = Emit(secondModel, verificationReport);
            ValidatedSemanticIrArtifact secondArtifact = ValidateArtifact(secondIr, verificationReport, definitionPath);
            if (secondModel == null || secondArtifact == null || !verificationReport.IsValid)
            {
                for (int i = 0; i < verificationReport.Messages.Count; i++)
                {
                    CharacterSimulationCompileMessage message = verificationReport.Messages[i];
                    report.ArtifactError("frontend_determinism_recompile_failed", message.SourceIdentity, $"{message.Stage}: {message.Message}");
                }
                return CharacterSemanticFrontendResult.Failed(report);
            }
            if (!firstArtifact.CanonicalBytes.Span.SequenceEqual(secondArtifact.CanonicalBytes.Span))
            {
                report.ArtifactError("semantic_ir_nondeterministic", definitionPath, "Two unchanged Frontend passes produced different canonical Semantic IR bytes.");
                return CharacterSemanticFrontendResult.Failed(report);
            }
            return new CharacterSemanticFrontendResult(firstArtifact, firstModel, report);
        }

        static CharacterAuthoringCompilationModel Discover(
            CharacterPipelineDefinition definition,
            string definitionPath,
            string definitionGuid,
            ProgramRevision sourceRevision,
            BaseTree root,
            CharacterSimulationCompileReport report)
        {
            try
            {
                return new CharacterAuthoringDiscovery(report).Discover(definition, definitionPath, definitionGuid, sourceRevision, root);
            }
            catch (Exception exception)
            {
                report.DiscoveryError("authoring_discovery_failed", definitionPath, exception.Message);
                return null;
            }
        }

        static CharacterGameplaySemanticIr Emit(CharacterAuthoringCompilationModel model, CharacterSimulationCompileReport report)
        {
            if (model == null || !report.IsValid)
                return null;
            try
            {
                var builder = new CharacterSimulationProgramBuilder(
                    model.ProgramId,
                    CompilerVersion,
                    OperationSetVersion,
                    model.TickRate,
                    model.SourceRevision,
                    report);
                builder.SetBodyMotion(
                    new CharacterBodyMotionSemanticDescriptor(
                        model.BodyMotionSourceIdentity,
                        model.BodyMotionContentRevision,
                        CharacterBodyMotionProfile.SemanticVersion,
                        model.BodyMotionProfile.GravityAcceleration,
                        model.BodyMotionProfile.MaximumFallSpeed),
                    new CharacterSimulationSourceLocation(
                        typeof(CharacterBodyMotionProfile).FullName,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        AssetDatabase.GetAssetPath(model.BodyMotionProfile),
                        contentHash: model.BodyMotionContentRevision.ToString()));
                var catalogCompiler = new CharacterSimulationCatalogCompiler(model, builder, report);
                CharacterSimulationCatalogIndex catalog = catalogCompiler.Compile();
                var emitter = new CharacterSemanticEmitter(model, builder, report, catalog);
                IReadOnlyDictionary<string, OperationHandle> rootOperations = emitter.EmitCompositionRoots();
                for (int rootIndex = 0; rootIndex < model.Roots.Count; rootIndex++)
                {
                    CharacterCompositionRoot root = model.Roots[rootIndex];
                    if (!rootOperations.TryGetValue(root.Identity, out OperationHandle rootOperation) || !rootOperation.IsValid)
                        continue;
                    var source = new CharacterSimulationSourceLocation(
                        root.Occurrence.Graph.GetType().FullName,
                        root.Occurrence.Graph.GraphAuthoringId,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        root.SourcePath,
                        contentHash: GraphAuthoringFingerprint.Compute(root.Occurrence.Graph));
                    builder.DeclareReference(
                        $"program:composition-root:{root.Identity}",
                        OperationHandle.Invalid,
                        ProgramReferenceKind.Operation,
                        rootOperation.Value,
                        root.Identity,
                        source);
                    if (root.Role == CharacterCompositionRootRole.Character)
                    {
                        builder.DeclareReference(
                            "program:root-operation",
                            OperationHandle.Invalid,
                            ProgramReferenceKind.Operation,
                            rootOperation.Value,
                            root.Identity,
                            source);
                    }
                }
                return builder.Build();
            }
            catch (Exception exception)
            {
                report.EmissionError("semantic_emission_failed", model.DefinitionPath, exception.Message);
                return null;
            }
        }

        static ValidatedSemanticIrArtifact ValidateArtifact(
            CharacterGameplaySemanticIr semanticIr,
            CharacterSimulationCompileReport report,
            string sourceIdentity)
        {
            if (semanticIr == null || !report.IsValid)
                return null;
            try
            {
                ValidatedSemanticIrArtifact artifact = CharacterGameplaySemanticIrCodec.CreateValidatedArtifact(semanticIr);
                return CharacterGameplaySemanticIrCodec.ReadValidatedArtifact(
                    artifact.ToArray(),
                    new SemanticIrLoadExpectation(
                        semanticIr.Manifest.ProgramId,
                        semanticIr.Manifest.CompilerVersion,
                        semanticIr.Manifest.OperationSetVersion,
                        semanticIr.Manifest.TickRate,
                        semanticIr.Manifest.SourceRevision,
                        semanticIr.SemanticHash));
            }
            catch (Exception exception)
            {
                report.ArtifactError("semantic_ir_validation_failed", sourceIdentity, exception.Message);
                return null;
            }
        }

        public static ProgramRevision ComputeSourceRevision(CharacterPipelineDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            string path = AssetDatabase.GetAssetPath(definition);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("CharacterPipelineDefinition is not a persisted asset.");
            return ComputeSourceRevision(path);
        }

        public static ProgramId ComputeProgramId(CharacterPipelineDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            string path = AssetDatabase.GetAssetPath(definition);
            string guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException("CharacterPipelineDefinition is not a persisted asset with a GUID.");
            return new ProgramId($"character:{guid}");
        }

        static ProgramRevision ComputeSourceRevision(string definitionPath)
        {
            string[] dependencies = AssetDatabase.GetDependencies(definitionPath, true)
                .Append(definitionPath)
                .Distinct(StringComparer.Ordinal)
                .Where(IsSourceDependency)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            using var writer = new CanonicalWriter();
            writer.WriteString(CompilerVersion);
            writer.WriteInt32(dependencies.Length);
            for (int i = 0; i < dependencies.Length; i++)
            {
                string path = dependencies[i].Replace('\\', '/');
                string absolute = Path.GetFullPath(path);
                if (!File.Exists(absolute))
                    throw new FileNotFoundException($"Authoring dependency '{path}' does not exist.", absolute);
                writer.WriteString(path);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid))
                    throw new InvalidOperationException($"Authoring dependency '{path}' has no Unity asset GUID.");
                writer.WriteString(guid);
                writer.WriteBytes(File.ReadAllBytes(absolute));
            }
            return new ProgramRevision(writer.ComputeHash().Value);
        }

        static bool IsSourceDependency(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            string extension = Path.GetExtension(path);
            if (!string.Equals(extension, ".asset", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".inputactions", StringComparison.OrdinalIgnoreCase))
                return false;
            Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
            return type != typeof(CharacterSimulationProgramAsset) &&
                   type != typeof(CharacterPresentationProjectionAsset);
        }

        static bool TryResolveRoot(
            CharacterPipelineDefinition definition,
            CharacterSimulationCompileReport report,
            out string definitionPath,
            out string definitionGuid,
            out BaseTree root)
        {
            definitionPath = string.Empty;
            definitionGuid = string.Empty;
            root = null;
            if (!definition)
            {
                report.DiscoveryError("definition_missing", "CharacterPipelineDefinition", "Frontend root is missing.");
                return false;
            }
            definitionPath = AssetDatabase.GetAssetPath(definition);
            definitionGuid = string.IsNullOrEmpty(definitionPath) ? string.Empty : AssetDatabase.AssetPathToGUID(definitionPath);
            if (string.IsNullOrEmpty(definitionPath) || string.IsNullOrEmpty(definitionGuid))
            {
                report.DiscoveryError("definition_identity_missing", definition.name, "CharacterPipelineDefinition must be a persisted asset with a GUID.");
                return false;
            }
            if (!definition.RootTreeAsset)
            {
                report.DiscoveryError("root_tree_asset_missing", definitionPath, "CharacterPipelineDefinition RootTree asset is missing.");
                return false;
            }
            root = definition.RootTreeAsset.Tree;
            if (root is not RunnableTree)
            {
                report.DiscoveryError("root_tree_type_invalid", definitionPath, $"RootTree type '{root?.GetType().FullName}' is not RunnableTree.");
                return false;
            }
            return true;
        }
    }
}
