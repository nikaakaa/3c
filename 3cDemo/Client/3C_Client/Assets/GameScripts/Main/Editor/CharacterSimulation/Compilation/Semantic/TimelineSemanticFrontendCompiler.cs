using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonSimulation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class TimelineSemanticFrontendCompiler
    {
        public const string CompilerVersion = "timeline-simulation-compiler/1";
        public static readonly OperationSetVersion OperationSetVersion = CharacterGameplayOperationSet.Version;

        public static TimelineSemanticFrontendResult Compile(TimelineAsset asset)
        {
            var report = new CharacterSimulationCompileReport();
            if (!TryResolveRoot(asset, report, out string path, out string guid))
                return TimelineSemanticFrontendResult.Failed(report);
            ProgramRevision sourceRevision;
            try
            {
                sourceRevision = ComputeSourceRevision(path);
            }
            catch (Exception exception)
            {
                report.DiscoveryError("timeline_source_revision_failed", path, exception.Message);
                return TimelineSemanticFrontendResult.Failed(report);
            }
            TimelineSemanticContentRecord content = TimelineSemanticContentDiscovery.Discover(asset, report);
            if (content == null || !report.IsValid)
                return TimelineSemanticFrontendResult.Failed(report);
            CharacterGameplaySemanticIr first = Emit(content, guid, sourceRevision, report);
            ValidatedSemanticIrArtifact firstArtifact = ValidateArtifact(first, report, path);
            if (firstArtifact == null || !report.IsValid)
                return TimelineSemanticFrontendResult.Failed(report);
            var verificationReport = new CharacterSimulationCompileReport();
            TimelineSemanticContentRecord secondContent = TimelineSemanticContentDiscovery.Discover(asset, verificationReport);
            CharacterGameplaySemanticIr second = secondContent == null
                ? null
                : Emit(secondContent, guid, sourceRevision, verificationReport);
            ValidatedSemanticIrArtifact secondArtifact = ValidateArtifact(second, verificationReport, path);
            if (secondArtifact == null || !verificationReport.IsValid)
            {
                for (int i = 0; i < verificationReport.Messages.Count; i++)
                {
                    CharacterSimulationCompileMessage message = verificationReport.Messages[i];
                    report.ArtifactError(
                        "timeline_frontend_determinism_recompile_failed",
                        message.SourceIdentity,
                        $"{message.Stage}: {message.Message}");
                }
                return TimelineSemanticFrontendResult.Failed(report);
            }
            if (!firstArtifact.CanonicalBytes.Span.SequenceEqual(secondArtifact.CanonicalBytes.Span))
            {
                report.ArtifactError(
                    "timeline_semantic_ir_nondeterministic",
                    path,
                    "Two unchanged Timeline Frontend passes produced different canonical Semantic IR bytes.");
                return TimelineSemanticFrontendResult.Failed(report);
            }
            return new TimelineSemanticFrontendResult(firstArtifact, content, guid, report);
        }

        static CharacterGameplaySemanticIr Emit(
            TimelineSemanticContentRecord content,
            string guid,
            ProgramRevision sourceRevision,
            CharacterSimulationCompileReport report)
        {
            try
            {
                var builder = new CharacterSimulationProgramBuilder(
                    new ProgramId($"timeline:{guid}"),
                    CompilerVersion,
                    OperationSetVersion,
                    TimelineUtility.FrameRate,
                    sourceRevision,
                    report,
                    new SimulationProgramRootDescriptor(
                        SimulationProgramRootKind.Timeline,
                        guid,
                        $"timeline:{content.Timeline.AuthoringId}",
                        content.ContentHash));
                var rootSource = new CharacterSimulationSourceLocation(
                    typeof(TimelineAsset).FullName,
                    $"timeline-root:{guid}",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    $"timeline:{content.Timeline.AuthoringId}/root",
                    contentHash: content.ContentHash);
                OperationHandle root = builder.DeclareOperation(
                    rootSource,
                    SimulationOperationCode.Root,
                    Array.Empty<int>());
                OperationHandle timeline = new TimelineSemanticRootEmitter(
                    CharacterSimulationTimelineEmitterRegistry.CreateDefault(),
                    builder,
                    report).Emit(content);
                builder.DeclareControlFlow(
                    $"timeline:{content.Timeline.AuthoringId}/root-entry",
                    root,
                    timeline,
                    "Entry",
                    "Entry",
                    ProgramControlFlowKind.Child,
                    0,
                    0,
                    ProgramAbortPolicy.None,
                    false,
                    OperationHandle.Invalid,
                    rootSource);
                builder.DeclareReference(
                    "program:root-operation",
                    OperationHandle.Invalid,
                    ProgramReferenceKind.Operation,
                    root.Value,
                    content.Route,
                    rootSource);
                return builder.Build();
            }
            catch (Exception exception)
            {
                report.EmissionError("timeline_semantic_emission_failed", content.Route, exception.Message);
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
                ValidatedSemanticIrArtifact artifact =
                    CharacterGameplaySemanticIrCodec.CreateValidatedArtifact(semanticIr);
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
                report.ArtifactError("timeline_semantic_ir_validation_failed", sourceIdentity, exception.Message);
                return null;
            }
        }

        static bool TryResolveRoot(
            TimelineAsset asset,
            CharacterSimulationCompileReport report,
            out string path,
            out string guid)
        {
            path = string.Empty;
            guid = string.Empty;
            if (!asset)
            {
                report.DiscoveryError("timeline_root_missing", "TimelineAsset", "Timeline build root is missing.");
                return false;
            }
            path = AssetDatabase.GetAssetPath(asset);
            guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(guid))
            {
                report.DiscoveryError("timeline_root_identity_missing", asset.name, "Timeline root must be a persisted asset with a GUID.");
                return false;
            }
            if (asset.Data == null)
            {
                report.DiscoveryError("timeline_data_missing", path, "Timeline asset has no TimelineData.");
                return false;
            }
            return true;
        }

        static ProgramRevision ComputeSourceRevision(string rootPath)
        {
            string[] dependencies = AssetDatabase.GetDependencies(rootPath, true)
                .Append(rootPath)
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
                    throw new FileNotFoundException($"Timeline dependency '{path}' does not exist.", absolute);
                writer.WriteString(path);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid))
                    throw new InvalidOperationException($"Timeline dependency '{path}' has no Unity asset GUID.");
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
            return string.Equals(extension, ".asset", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".inputactions", StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class TimelineSemanticFrontendResult
    {
        internal TimelineSemanticFrontendResult(
            ValidatedSemanticIrArtifact artifact,
            TimelineSemanticContentRecord content,
            string rootGuid,
            CharacterSimulationCompileReport report)
        {
            Artifact = artifact;
            Content = content;
            RootGuid = rootGuid ?? string.Empty;
            Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        TimelineSemanticFrontendResult(CharacterSimulationCompileReport report)
        {
            Report = report ?? throw new ArgumentNullException(nameof(report));
            RootGuid = string.Empty;
        }

        public ValidatedSemanticIrArtifact Artifact { get; }
        public TimelineSemanticContentRecord Content { get; }
        public string RootGuid { get; }
        public CharacterSimulationCompileReport Report { get; }
        public bool IsValid => Artifact != null && Content != null && Report.IsValid;
        internal static TimelineSemanticFrontendResult Failed(CharacterSimulationCompileReport report) =>
            new TimelineSemanticFrontendResult(report);
    }
}
