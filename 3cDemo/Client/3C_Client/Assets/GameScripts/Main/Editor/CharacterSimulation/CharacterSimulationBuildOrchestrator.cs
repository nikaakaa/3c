using System;
using System.Collections.Generic;
using System.IO;
using BTSMTL.Timeline;
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
            CharacterSimulationBuildResult result = Execute(definition, true);
            if (!result.IsValid)
                return result;
            try
            {
                string definitionGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition));
                using CharacterTargetProgramArtifactPublishTransaction transaction =
                    CharacterTargetProgramArtifactStore.Stage(definitionGuid, result.Program);
                Publish(definition, transaction, result.PresentationProjection);
            }
            catch (Exception exception)
            {
                result.Report.ArtifactError("target_program_publish_failed", AssetDatabase.GetAssetPath(definition), exception.Message);
                return Failed(result.Report);
            }
            return result;
        }

        public static CharacterSimulationBuildResult DryRun(CharacterPipelineDefinition definition)
        {
            return Execute(definition, false);
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

        static CharacterSimulationBuildResult Execute(CharacterPipelineDefinition definition, bool persistCache)
        {
            CharacterSemanticFrontendResult frontend = CharacterSemanticFrontendCompiler.Compile(definition);
            CharacterSimulationCompileReport report = frontend.Report;
            if (!frontend.IsValid)
                return Failed(report);

            ValidatedSemanticIrArtifact artifact = frontend.Artifact;
            string artifactPath = CharacterSemanticIrArtifactStore.GetPath(frontend.CompilationModel.DefinitionGuid);
            if (persistCache)
            {
                try
                {
                    artifact = CharacterSemanticIrArtifactStore.Write(frontend.CompilationModel.DefinitionGuid, artifact);
                }
                catch (Exception exception)
                {
                    report.ArtifactError("semantic_ir_store_failed", artifactPath, exception.Message);
                    return Failed(report);
                }
            }

            CharacterSimulationProgram program = CompileFloat32(artifact, report);
            if (program == null || !report.IsValid)
                return Failed(report);
            CharacterPresentationProjection projection = CompileProjection(frontend.CompilationModel, artifact, program, report);
            if (projection == null || !projection.IsValid || !report.IsValid)
                return Failed(report);
            var descriptor = new CharacterSemanticIrArtifactDescriptor(artifactPath, artifact.Header);
            return new CharacterSimulationBuildResult(descriptor, program, projection, report);
        }

        static CharacterSimulationProgram CompileFloat32(ValidatedSemanticIrArtifact artifact, CharacterSimulationCompileReport report)
        {
            try
            {
                Float32ProgramLoweringResult result = Float32CharacterSimulationTargetCompiler.Compile(artifact);
                for (int i = 0; i < result.Conversions.Count; i++)
                {
                    Float32ScalarConversion conversion = result.Conversions[i];
                    if (conversion.WasRounded)
                        report.TargetInformation("float32_literal_rounded", conversion.SourceIdentity, $"{conversion.SourceValue:R} -> {conversion.Value} error={conversion.AbsoluteError:R}.");
                }
                byte[] bytes = CharacterSimulationProgramCodec.WriteArtifact(result.Program);
                CharacterSimulationProgram roundTrip = CharacterSimulationProgramCodec.ReadArtifact(
                    bytes,
                    new ProgramLoadExpectation(
                        artifact.Header.CompilerVersion,
                        artifact.Header.OperationSetVersion,
                        artifact.Header.SourceRevision,
                        artifact.Header.SemanticHash,
                        Float32SimulationNumericProfile.Value));
                if (!roundTrip.ProgramHash.Equals(result.Program.ProgramHash) || !roundTrip.LayoutHash.Equals(result.Program.LayoutHash))
                    throw new InvalidDataException("Float32 Program round-trip identity mismatch.");
                var partitions = new HashSet<ProgramStateValueKind>();
                for (int i = 0; i < result.Program.StateSlots.Count; i++)
                    partitions.Add(result.Program.StateSlots[i].ValueKind);
                report.TargetInformation(
                    "float32_state_abi",
                    result.Program.Manifest.ProgramId.Value,
                    $"ABI={result.Program.Manifest.NumericProfile.AbiVersion.Value} Codec={CharacterSimulationStateCodec.CodecIdentity} StateSlots={result.Program.StateSlots.Count} TypedPartitions={partitions.Count} MotionTransientSlots=0 GameplayEffectAggregateSlots=1.");
                return result.Program;
            }
            catch (SimulationNumericConversionException exception)
            {
                report.TargetError("float32_numeric_conversion_failed", exception.SourceIdentity, exception.Message);
                return null;
            }
            catch (Exception exception)
            {
                report.TargetError("float32_target_failed", artifact.Header.ProgramId.Value, exception.Message);
                return null;
            }
        }

        static CharacterPresentationProjection CompileProjection(
            CharacterAuthoringCompilationModel model,
            ValidatedSemanticIrArtifact artifact,
            CharacterSimulationProgram program,
            CharacterSimulationCompileReport report)
        {
            var errors = new List<string>();
            CharacterPresentationProjection projection = CharacterPresentationProjection.Build(
                program,
                model.AnimationPresentationProfile,
                model.Timelines,
                CollectAnimationMarkerSyncCallSites(model.Root),
                errors);
            for (int i = 0; i < errors.Count; i++)
                report.PresentationError("presentation_projection_invalid", artifact.Header.ProgramId.Value, errors[i]);
            if (!report.IsValid)
                return projection;
            if (!string.Equals(projection.ProgramId, artifact.Header.ProgramId.Value, StringComparison.Ordinal) ||
                !string.Equals(projection.SourceRevision, artifact.Header.SourceRevision.Value, StringComparison.Ordinal) ||
                !string.Equals(projection.SemanticHash, artifact.Header.SemanticHash.ToString(), StringComparison.Ordinal) ||
                !string.Equals(projection.ProgramHash, program.ProgramHash.ToString(), StringComparison.Ordinal))
            {
                report.PresentationError("presentation_identity_mismatch", artifact.Header.ProgramId.Value, "Projection identity does not match the Semantic IR artifact and Float32 Program.");
                return projection;
            }
            if (projection.Producers.Count != artifact.SemanticIr.Producers.Count || projection.Producers.Count != program.Producers.Count)
            {
                report.PresentationError("presentation_producer_set_mismatch", artifact.Header.ProgramId.Value, "Projection producer count does not match the Semantic IR artifact and Float32 Program.");
                return projection;
            }
            for (int i = 0; i < projection.Producers.Count; i++)
            {
                CharacterPresentationProducerEntry projected = projection.Producers[i];
                ProgramProducer semantic = artifact.SemanticIr.Producers[i];
                ProgramProducer lowered = program.Producers[i];
                if (projected.ProgramProducerIndex != i || semantic.Index != i || lowered.Index != i ||
                    !string.Equals(projected.ProgramProducerIdentity, semantic.Identity, StringComparison.Ordinal) ||
                    !string.Equals(projected.ProgramProducerIdentity, lowered.Identity, StringComparison.Ordinal))
                {
                    report.PresentationError("presentation_producer_identity_mismatch", projected.ProgramProducerIdentity, $"Producer index {i} is not identical across Semantic IR, Program and Projection.");
                }
            }
            return projection;
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

        static void Publish(
            CharacterPipelineDefinition definition,
            CharacterTargetProgramArtifactPublishTransaction transaction,
            CharacterPresentationProjection projection)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));
            string programPath = ProgramAssetPath(definition);
            string projectionPath = ProjectionAssetPath(definition);
            EnsureFolder(Path.GetDirectoryName(programPath)?.Replace('\\', '/'));
            CharacterSimulationProgramAsset programAsset = AssetDatabase.LoadAssetAtPath<CharacterSimulationProgramAsset>(programPath);
            CharacterPresentationProjectionAsset projectionAsset = AssetDatabase.LoadAssetAtPath<CharacterPresentationProjectionAsset>(projectionPath);
            bool createProgram = !programAsset;
            bool createProjection = !projectionAsset;
            string definitionBackup = EditorJsonUtility.ToJson(definition);
            string programBackup = createProgram ? string.Empty : EditorJsonUtility.ToJson(programAsset);
            string projectionBackup = createProjection ? string.Empty : EditorJsonUtility.ToJson(projectionAsset);
            if (createProgram)
            {
                programAsset = ScriptableObject.CreateInstance<CharacterSimulationProgramAsset>();
                programAsset.name = $"{definition.name} Simulation Program";
            }
            if (createProjection)
            {
                projectionAsset = ScriptableObject.CreateInstance<CharacterPresentationProjectionAsset>();
                projectionAsset.name = $"{definition.name} Presentation Projection";
            }
            try
            {
                LoadedCharacterTargetProgramArtifact artifact = transaction.Commit();
                programAsset.SetCompiledArtifact(artifact);
                projectionAsset.SetCompiledProjection(projection);
                if (createProgram)
                    AssetDatabase.CreateAsset(programAsset, programPath);
                if (createProjection)
                    AssetDatabase.CreateAsset(projectionAsset, projectionPath);
                EditorUtility.SetDirty(programAsset);
                EditorUtility.SetDirty(projectionAsset);
                AssetDatabase.SaveAssetIfDirty(programAsset);
                AssetDatabase.SaveAssetIfDirty(projectionAsset);
                if (definition.SimulationProgram != programAsset || definition.PresentationProjection != projectionAsset)
                {
                    definition.SetSimulationProgram(programAsset);
                    definition.SetPresentationProjection(projectionAsset);
                    EditorUtility.SetDirty(definition);
                    AssetDatabase.SaveAssetIfDirty(definition);
                }
                transaction.Complete();
            }
            catch
            {
                if (createProgram && AssetDatabase.LoadAssetAtPath<CharacterSimulationProgramAsset>(programPath))
                    AssetDatabase.DeleteAsset(programPath);
                else if (!createProgram)
                {
                    EditorJsonUtility.FromJsonOverwrite(programBackup, programAsset);
                    EditorUtility.SetDirty(programAsset);
                    AssetDatabase.SaveAssetIfDirty(programAsset);
                }
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
                throw;
            }
        }

        static CharacterSimulationBuildResult Failed(CharacterSimulationCompileReport report)
        {
            return new CharacterSimulationBuildResult(null, null, null, report);
        }

        static string ProgramAssetPath(CharacterPipelineDefinition definition)
        {
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string directory = Path.GetDirectoryName(definitionPath)?.Replace('\\', '/') ?? "Assets";
            return $"{directory}/Generated/{definition.name}.SimulationProgram.asset";
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
