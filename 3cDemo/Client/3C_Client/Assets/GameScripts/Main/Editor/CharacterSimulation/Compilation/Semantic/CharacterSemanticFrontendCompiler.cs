using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonCharacter.Control.Rules;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class CharacterSemanticFrontendCompiler
    {
        public const string CompilerVersion = "character-simulation-compiler/29";
        public static readonly OperationSetVersion OperationSetVersion = CharacterGameplayOperationSet.Version;

        static readonly Dictionary<string, string> s_VerifiedSourceRevisions =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public static CharacterSemanticFrontendResult Compile(CharacterPipelineDefinition definition)
        {
            var report = new CharacterSimulationCompileReport();
            if (!TryResolveRoot(definition, report, out string definitionPath, out string definitionGuid))
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

            CharacterAuthoringCompilationModel firstModel = Discover(definition, definitionPath, definitionGuid, sourceRevision, report);
            CharacterGameplaySemanticIr firstIr = Emit(firstModel, report);
            ValidatedSemanticIrArtifact firstArtifact = ValidateArtifact(firstIr, report, definitionPath);
            if (firstModel == null || firstArtifact == null || !report.IsValid)
                return CharacterSemanticFrontendResult.Failed(report);

            if (s_VerifiedSourceRevisions.TryGetValue(definitionGuid, out string verifiedRevision) &&
                string.Equals(verifiedRevision, sourceRevision.Value, StringComparison.Ordinal))
                return new CharacterSemanticFrontendResult(firstArtifact, firstModel, report);

            var verificationReport = new CharacterSimulationCompileReport();
            CharacterAuthoringCompilationModel secondModel = Discover(definition, definitionPath, definitionGuid, sourceRevision, verificationReport);
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
            s_VerifiedSourceRevisions[definitionGuid] = sourceRevision.Value;
            return new CharacterSemanticFrontendResult(firstArtifact, firstModel, report);
        }

        static CharacterAuthoringCompilationModel Discover(
            CharacterPipelineDefinition definition,
            string definitionPath,
            string definitionGuid,
            ProgramRevision sourceRevision,
            CharacterSimulationCompileReport report)
        {
            try
            {
                return new CharacterAuthoringDiscovery(report).Discover(definition, definitionPath, definitionGuid, sourceRevision);
            }
            catch (Exception exception)
            {
                report.DiscoveryError("authoring_discovery_failed", definitionPath, exception.ToString());
                return null;
            }
        }

        static CharacterGameplaySemanticIr Emit(CharacterAuthoringCompilationModel model, CharacterSimulationCompileReport report)
        {
            if (model == null || !report.IsValid)
                return null;
            try
            {
                var root = new SimulationProgramRootDescriptor(
                    SimulationProgramRootKind.Character,
                    model.DefinitionGuid,
                    $"control:{model.Definition.ControlModuleId}",
                    model.SourceRevision.Value);
                var builder = new CharacterSimulationProgramBuilder(
                    model.ProgramId,
                    CompilerVersion,
                    OperationSetVersion,
                    model.TickRate,
                    model.SourceRevision,
                    report,
                    root);
                builder.SetBodyMotion(
                    new CharacterBodyMotionBinding(
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
                if (string.IsNullOrEmpty(model.Definition.ControlModuleId))
                {
                    report.Error("control_module_missing", model.DefinitionPath, "Character Pipeline requires an installed control module.");
                    return null;
                }
                ICharacterControlModule controlModule = ResolveControlModule(model.Definition.ControlModuleId, report, model.DefinitionPath);
                if (controlModule == null)
                    return null;
                if (!string.Equals(controlModule.Contract.ModuleId.Value, model.Definition.ControlModuleId, StringComparison.Ordinal))
                {
                    report.Error(
                        "control_module_identity_mismatch",
                        model.DefinitionPath,
                        $"Resolved control module '{controlModule.Contract.ModuleId.Value}' does not match Definition entry '{model.Definition.ControlModuleId}'.");
                    return null;
                }
                CharacterSimulationSourceLocation controlSource = new CharacterSimulationSourceLocation(
                    typeof(ICharacterControlModule).FullName,
                    $"control:{controlModule.Contract.ModuleId.Value}",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    $"control:{controlModule.Contract.ModuleId.Value}",
                    contentHash: controlModule.Contract.SemanticVersion.ToString());
                if (!TryResolveControlParameters(model.Definition.ControlParameters, controlModule.Contract, report, model.DefinitionPath, out CharacterControlParameterSet controlParameters))
                    return null;
                controlSource = new CharacterSimulationSourceLocation(
                    controlSource.SourceType,
                    controlSource.GraphId,
                    controlSource.NodeId,
                    controlSource.EdgeId,
                    controlSource.TimelineId,
                    controlSource.ClipId,
                    controlSource.DisplayPath,
                    trackId: controlSource.TrackId,
                    declarationId: controlSource.DeclarationId,
                    portId: controlSource.PortId,
                    contentHash: controlParameters.ContentHash.ToString());
                new CharacterSemanticControlModuleEmitter(builder).Emit(controlModule.Contract, controlSource);
                IReadOnlyList<CharacterControlMotionCompilationRecord> motions = CharacterControlMotionCompilationDiscovery.Discover(
                    controlModule.Contract.Motions,
                    model.Roots,
                    model.Definition.ControlMotionTimelines,
                    report);
                if (!report.IsValid)
                    return null;
                OperationHandle controlRoot = emitter.EmitControlAbilityPrograms(controlModule.Contract, controlSource, motions);
                if (!controlRoot.IsValid)
                    return null;
                builder.DeclareReference(
                    "program:root-operation",
                    OperationHandle.Invalid,
                    ProgramReferenceKind.Operation,
                    controlRoot.Value,
                    controlSource.DisplayPath,
                    controlSource);
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
                        semanticIr.SemanticHash,
                        semanticIr.Manifest.Root));
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

        static readonly string s_SourceRevisionSessionPrefix = "3C.sourceRevision.fingerprint.";

        static ProgramRevision ComputeSourceRevision(string definitionPath)
        {
            string[] dependencies = AssetDatabase.GetDependencies(definitionPath, true)
                .Append(definitionPath)
                .Distinct(StringComparer.Ordinal)
                .Where(IsSourceDependency)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            string sessionKey;
            using (var fingerprintWriter = new CanonicalWriter())
            {
                fingerprintWriter.WriteString(CompilerVersion);
                fingerprintWriter.WriteInt32(dependencies.Length);
                for (int i = 0; i < dependencies.Length; i++)
                {
                    string path = dependencies[i].Replace('\\', '/');
                    string absolute = Path.GetFullPath(path);
                    if (!File.Exists(absolute))
                        throw new FileNotFoundException($"Authoring dependency '{path}' does not exist.", absolute);
                    var file = new FileInfo(absolute);
                    fingerprintWriter.WriteString(path);
                    fingerprintWriter.WriteString(AssetDatabase.AssetPathToGUID(path));
                    fingerprintWriter.WriteString(
                        file.LastWriteTimeUtc.Ticks.ToString(
                            System.Globalization.CultureInfo.InvariantCulture));
                    fingerprintWriter.WriteInt64(file.Length);
                }
                sessionKey = s_SourceRevisionSessionPrefix + fingerprintWriter.ComputeHash().Value;
                string cachedRevision = SessionState.GetString(sessionKey, string.Empty);
                if (cachedRevision.Length > 0)
                    return new ProgramRevision(cachedRevision);
            }
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
            string revision = writer.ComputeHash().Value;
            SessionState.SetString(sessionKey, revision);
            return new ProgramRevision(revision);
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

        static ICharacterControlModule ResolveControlModule(
            string moduleId,
            CharacterSimulationCompileReport report,
            string source)
        {
            try
            {
                return CorinCharacterControlModuleCatalog.Create().Require(new CharacterControlModuleId(moduleId));
            }
            catch (Exception exception)
            {
                report.Error("control_module_uninstalled", source, exception.Message);
                return null;
            }
        }

        static bool TryResolveControlParameters(
            IReadOnlyList<CharacterControlParameterConfiguration> configurations,
            CharacterControlModuleContract contract,
            CharacterSimulationCompileReport report,
            string source,
            out CharacterControlParameterSet parameters)
        {
            var values = new List<CharacterControlParameterValue>();
            for (int i = 0; i < configurations.Count; i++)
            {
                CharacterControlParameterConfiguration configuration = configurations[i];
                if (configuration == null || string.IsNullOrWhiteSpace(configuration.ParameterId))
                {
                    report.Error("control_parameter_configuration_invalid", source, $"Control parameter configuration #{i} has no identity.");
                    continue;
                }
                try
                {
                    values.Add(new CharacterControlParameterValue(
                        new CharacterControlParameterId(configuration.ParameterId.Trim()),
                        configuration.ValueKind,
                        configuration.NumericValue));
                }
                catch (Exception exception)
                {
                    report.Error("control_parameter_configuration_invalid", source, exception.Message);
                }
            }
            if (!report.IsValid)
            {
                parameters = null;
                return false;
            }
            if (!contract.TryResolveParameterSet(values, out parameters, out IReadOnlyList<string> errors))
            {
                for (int i = 0; i < errors.Count; i++)
                    report.Error("control_parameter_configuration_invalid", source, errors[i]);
                parameters = null;
                return false;
            }
            return true;
        }

        static bool TryResolveRoot(
            CharacterPipelineDefinition definition,
            CharacterSimulationCompileReport report,
            out string definitionPath,
            out string definitionGuid)
        {
            definitionPath = string.Empty;
            definitionGuid = string.Empty;
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
            return true;
        }
    }
}
