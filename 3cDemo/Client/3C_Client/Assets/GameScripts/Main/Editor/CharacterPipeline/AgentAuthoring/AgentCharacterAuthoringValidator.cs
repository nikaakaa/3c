using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentCharacterAuthoringValidator
    {
        public AgentCompileReport Validate(
            CharacterPipelineDefinition definition,
            bool includeExactCompile = true)
        {
            return Validate(definition, null, includeExactCompile);
        }

        public AgentCompileReport Validate(
            CharacterPipelineDefinition definition,
            SimulationSessionCompositionDefinition composition,
            bool includeExactCompile = true)
        {
            string definitionPath = definition ? AssetDatabase.GetAssetPath(definition) : string.Empty;
            var report = new AgentCompileReport
            {
                success = true,
                domain = AgentAuthoringSchema.CharacterControllerDomain,
                rootIdentity = AssetDatabase.AssetPathToGUID(definitionPath)
            };
            if (!definition)
            {
                report.Error("definition", "missing_definition", "CharacterPipelineDefinition 缺失。");
                return report;
            }

            if (!definition.AnimationPresentationProfile)
                report.Error(
                    "definition.animationPresentationProfile",
                    "missing_animation_presentation_profile",
                    "CharacterAnimationPresentationProfile 缺失。");
            if (!definition.BodyMotionProfile)
                report.Error("definition.bodyMotionProfile", "missing_body_motion_profile", "CharacterBodyMotionProfile 缺失。");
            else
            {
                var bodyMotionErrors = new List<string>();
                definition.BodyMotionProfile.CollectConfigurationErrors(bodyMotionErrors);
                for (int i = 0; i < bodyMotionErrors.Count; i++)
                    report.Error("definition.bodyMotionProfile", "body_motion_profile_invalid", bodyMotionErrors[i]);
            }
            if (!definition.InputProfile)
                report.Error("definition.inputProfile", "missing_input_profile", "CharacterInputProfile 缺失。");
            else
            {
                var inputErrors = new List<string>();
                definition.InputProfile.CollectConfigurationErrors(inputErrors);
                for (int i = 0; i < inputErrors.Count; i++)
                    report.Error("definition.inputProfile", "input_profile_invalid", inputErrors[i]);
            }

            if (definition.SkillDefinitions.Count > 0 || (definition.SkillGraphs?.Count ?? 0) > 0)
                AgentSkillFlowDocumentMapper.ValidateDefinition(definition, report);
            if (includeExactCompile)
            {
                CharacterSimulationBuildResult result = CharacterSimulationBuildOrchestrator.DryRun(
                    definition,
                    CharacterSimulationTargetCatalog.DefaultValidation(definition));
                AppendFormalCompileReport(report, result);
                AppendTargetStateLayoutReport(report, result);
                AppendTargetStateCodecReport(report, result);
                bool semanticValid = result.Artifact != null && result.Report.IsValid;
                report.metrics.semanticValidCount = semanticValid ? 1 : 0;
                report.metrics.semanticInvalidCount = semanticValid ? 0 : 1;
                report.metrics.compileSuccessCount = result.IsValid ? 1 : 0;
                report.metrics.compileFailureCount = result.IsValid ? 0 : 1;
            }
            if (composition)
                AppendCompositionCompatibilityReport(report, composition);
            report.success = !report.HasErrors();
            return report;
        }

        static void AppendCompositionCompatibilityReport(
            AgentCompileReport report,
            SimulationSessionCompositionDefinition composition)
        {
            string path = AssetDatabase.GetAssetPath(composition);
            try
            {
                SimulationSessionCompositionCompatibilityReport compatibility =
                    SimulationSessionCompositionCompatibility.Evaluate(composition);
                for (int i = 0; i < compatibility.Issues.Count; i++)
                {
                    SimulationSessionCompatibilityIssue issue = compatibility.Issues[i];
                    report.Error(
                        $"composition/{path}/{issue.Code}",
                        issue.Code,
                        issue.Message);
                }
                if (compatibility.Compilation == null)
                {
                    if (compatibility.Issues.Count == 0)
                        report.Error(
                            "composition/" + path,
                            "session_pipeline_compile_missing",
                            "Session Composition兼容检查没有生成正式Pipeline编译结果。");
                    return;
                }
                for (int i = 0; i < compatibility.Compilation.Errors.Count; i++)
                {
                    SimulationPipelineCompileError error = compatibility.Compilation.Errors[i];
                    report.Error(
                        $"composition/{path}/pipeline/{error.Code}",
                        error.Code.ToString(),
                        $"{error.Message} Component={error.ComponentIdentity} Pass={error.PassId} Product={error.ProductId}。");
                }
                if (!compatibility.IsValid)
                    return;
                SimulationSessionSourceDescriptor source = compatibility.Source.Source;
                report.Info(
                    "composition/" + path,
                    "session_composition_compatible",
                    $"ProgramRuntime={compatibility.ProgramRuntime.Identity}; Backend={compatibility.Backend.Identity}; Pipeline={compatibility.PipelineIdentity}; PlanHash={compatibility.PlanHash}; Source={source.Identity}; Solver={compatibility.Solver.Identity}; RequiredPasses={source.RequiredPipelinePasses.Count}; SourcePorts={source.RequiredPipelineSourcePorts.Count}; NetworkModel={(source.Model.HasValue ? source.Model.Value.ToString() : "Local")}.");
            }
            catch (Exception exception)
            {
                report.Error(
                    "composition/" + path,
                    "session_composition_validation_failed",
                    exception.Message);
            }
        }

        static void AppendTargetStateLayoutReport(
            AgentCompileReport report,
            CharacterSimulationBuildResult result)
        {
            if (result?.TargetProducts == null || result.TargetProducts.Count == 0)
                return;

            IReadOnlyList<ProgramStateSlot> baseline = StateSlots(result.TargetProducts[0]);
            for (int targetIndex = 0; targetIndex < result.TargetProducts.Count; targetIndex++)
            {
                CharacterSimulationTargetBuildProduct target = result.TargetProducts[targetIndex];
                IReadOnlyList<ProgramStateSlot> slots = StateSlots(target);
                report.Info(
                    $"compiler/TargetLowering/{target.NumericProfileId.Value}",
                    "state_layout_identity",
                    $"NumericProfile={target.NumericProfileId.Value} StateSlots={slots.Count} ProgramHash={ProgramHash(target)} LayoutHash={LayoutHash(target)}.");
                if (slots.Count != baseline.Count)
                {
                    report.Error(
                        $"compiler/TargetLowering/{target.NumericProfileId.Value}/StateSlots",
                        "state_layout_count_mismatch",
                        $"Target state slot count '{slots.Count}' does not match '{baseline.Count}'.");
                    continue;
                }
                for (int slotIndex = 0; slotIndex < baseline.Count; slotIndex++)
                {
                    ProgramStateSlot expected = baseline[slotIndex];
                    ProgramStateSlot actual = slots[slotIndex];
                    if (expected.Index != actual.Index ||
                        !string.Equals(expected.Identity, actual.Identity, StringComparison.Ordinal) ||
                        expected.ValueKind != actual.ValueKind ||
                        expected.OwnerKind != actual.OwnerKind ||
                        expected.Semantic != actual.Semantic ||
                        !string.Equals(expected.OwnerIdentity, actual.OwnerIdentity, StringComparison.Ordinal))
                    {
                        report.Error(
                            $"compiler/TargetLowering/{target.NumericProfileId.Value}/StateSlots[{slotIndex}]",
                            "state_layout_identity_mismatch",
                            $"Target state slot identity differs from '{baseline[slotIndex].Identity}'.");
                        break;
                    }
                }
            }
        }

        static IReadOnlyList<ProgramStateSlot> StateSlots(CharacterSimulationTargetBuildProduct target)
        {
            return target switch
            {
                Float32CharacterSimulationTargetBuildProduct float32 => float32.Program.StateSlots,
                FixedCharacterSimulationTargetBuildProduct fixedTarget => fixedTarget.Program.StateSlots,
                _ => Array.Empty<ProgramStateSlot>()
            };
        }

        static string ProgramHash(CharacterSimulationTargetBuildProduct target)
        {
            return target switch
            {
                Float32CharacterSimulationTargetBuildProduct float32 => float32.Program.ProgramHash.ToString(),
                FixedCharacterSimulationTargetBuildProduct fixedTarget => fixedTarget.Program.ProgramHash.ToString(),
                _ => string.Empty
            };
        }

        static string LayoutHash(CharacterSimulationTargetBuildProduct target)
        {
            return target switch
            {
                Float32CharacterSimulationTargetBuildProduct float32 => float32.Program.LayoutHash.ToString(),
                FixedCharacterSimulationTargetBuildProduct fixedTarget => fixedTarget.Program.LayoutHash.ToString(),
                _ => string.Empty
            };
        }

        static void AppendTargetStateCodecReport(
            AgentCompileReport report,
            CharacterSimulationBuildResult result)
        {
            if (result?.TargetProducts == null)
                return;
            for (int targetIndex = 0; targetIndex < result.TargetProducts.Count; targetIndex++)
            {
                CharacterSimulationTargetBuildProduct target = result.TargetProducts[targetIndex];
                try
                {
                    string stateHash;
                    string snapshotHash;
                    if (target is Float32CharacterSimulationTargetBuildProduct float32)
                        ValidateFloat32StateCodecs(float32.Program, out stateHash, out snapshotHash);
                    else if (target is FixedCharacterSimulationTargetBuildProduct fixedTarget)
                        ValidateFixedStateCodecs(fixedTarget.Program, out stateHash, out snapshotHash);
                    else
                        continue;
                    report.Info(
                        $"compiler/TargetLowering/{target.NumericProfileId.Value}",
                        "state_codec_round_trip",
                        $"StateHash={stateHash} WorldSnapshotHash={snapshotHash}.");
                }
                catch (Exception exception)
                {
                    report.Error(
                        $"compiler/TargetLowering/{target.NumericProfileId.Value}",
                        "state_codec_round_trip_failed",
                        exception.Message);
                }
            }
        }

        static void ValidateFloat32StateCodecs(
            ThirdPersonSimulation.CharacterSimulationProgram program,
            out string stateHash,
            out string snapshotHash)
        {
            ThirdPersonSimulation.ActorId actorId = new ThirdPersonSimulation.ActorId("validation-actor");
            ThirdPersonSimulation.CharacterSimulationState state =
                ThirdPersonSimulation.CharacterSimulationState.CreateInitial(program);
            byte[] stateBytes = ThirdPersonSimulation.CharacterSimulationStateCodec.Write(state);
            ThirdPersonSimulation.CharacterSimulationState restoredState =
                ThirdPersonSimulation.CharacterSimulationStateCodec.Read(stateBytes, program);
            ThirdPersonSimulation.CharacterStateHash hash =
                ThirdPersonSimulation.CharacterSimulationStateCodec.ComputeHash(restoredState);
            if (!hash.Equals(ThirdPersonSimulation.CharacterSimulationStateCodec.ComputeHash(state)))
                throw new InvalidOperationException("Float32 Character State hash changed after canonical round-trip.");
            stateHash = hash.ToString();

            ThirdPersonSimulation.WorldSimulationState world =
                new ThirdPersonSimulation.WorldSimulationState(
                    program.Manifest.NumericProfile,
                    new ThirdPersonSimulation.SolverImplementationId("validation.solver"),
                    "1",
                    new ThirdPersonSimulation.WorldRevision("validation.world"),
                    ThirdPersonSimulation.WorldStatePersistenceMode.Snapshot,
                    new[]
                    {
                        new ThirdPersonSimulation.WorldBodyState(
                            actorId,
                            ThirdPersonSimulation.Float32Vector3.Zero,
                            ThirdPersonSimulation.Float32Yaw.Zero,
                            ThirdPersonSimulation.Float32Vector3.Zero,
                            ThirdPersonSimulation.Float32Scalar.Zero,
                            false,
                            ThirdPersonSimulation.WorldCollisionSummary.None)
                    },
                    Array.Empty<byte>());
            var catalog = new ThirdPersonSimulation.SimulationProgramCatalog(new[] { program });
            ThirdPersonSimulation.SimulationWorldSnapshot snapshot =
                ThirdPersonSimulation.SimulationWorldSnapshotFactory.Capture(
                    catalog,
                    new ThirdPersonSimulation.SimulationTick(1),
                    new[]
                    {
                        new ThirdPersonSimulation.SimulationActorState(actorId, state)
                    },
                    world,
                    ThirdPersonSimulation.WorldCapability.None);
            byte[] snapshotBytes = ThirdPersonSimulation.SimulationWorldSnapshotCodec.Write(snapshot);
            ThirdPersonSimulation.SimulationWorldSnapshot restoredSnapshot =
                ThirdPersonSimulation.SimulationWorldSnapshotCodec.Read(snapshotBytes);
            _ = restoredSnapshot.Actors[0].Decode(program);
            if (!restoredSnapshot.WorldHash.Equals(snapshot.WorldHash))
                throw new InvalidOperationException("Float32 World Snapshot hash changed after canonical round-trip.");
            snapshotHash = restoredSnapshot.WorldHash.ToString();
        }

        static void ValidateFixedStateCodecs(
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program,
            out string stateHash,
            out string snapshotHash)
        {
            ThirdPersonSimulation.ActorId actorId = new ThirdPersonSimulation.ActorId("validation-actor");
            ThirdPersonSimulation.Fixed.CharacterSimulationState state =
                ThirdPersonSimulation.Fixed.CharacterSimulationState.CreateInitial(program);
            byte[] stateBytes = ThirdPersonSimulation.Fixed.CharacterSimulationStateCodec.Write(state);
            ThirdPersonSimulation.Fixed.CharacterSimulationState restoredState =
                ThirdPersonSimulation.Fixed.CharacterSimulationStateCodec.Read(stateBytes, program);
            ThirdPersonSimulation.CharacterStateHash hash =
                ThirdPersonSimulation.Fixed.CharacterSimulationStateCodec.ComputeHash(restoredState);
            if (!hash.Equals(ThirdPersonSimulation.Fixed.CharacterSimulationStateCodec.ComputeHash(state)))
                throw new InvalidOperationException("Fixed Character State hash changed after canonical round-trip.");
            stateHash = hash.ToString();

            ThirdPersonSimulation.Fixed.WorldSimulationState world =
                new ThirdPersonSimulation.Fixed.WorldSimulationState(
                    program.Manifest.NumericProfile,
                    new ThirdPersonSimulation.SolverImplementationId("validation.solver"),
                    "1",
                    new ThirdPersonSimulation.WorldRevision("validation.world"),
                    ThirdPersonSimulation.Fixed.WorldStatePersistenceMode.Snapshot,
                    new[]
                    {
                        new ThirdPersonSimulation.Fixed.WorldBodyState(
                            actorId,
                            ThirdPersonSimulation.Fixed.FixedVector3.Zero,
                            ThirdPersonSimulation.Fixed.FixedYaw.Zero,
                            ThirdPersonSimulation.Fixed.FixedVector3.Zero,
                            ThirdPersonSimulation.Fixed.FixedScalar.Zero,
                            false,
                            ThirdPersonSimulation.Fixed.WorldCollisionSummary.None)
                    },
                    Array.Empty<byte>());
            var catalog = new ThirdPersonSimulation.Fixed.SimulationProgramCatalog(new[] { program });
            ThirdPersonSimulation.Fixed.SimulationWorldSnapshot snapshot =
                ThirdPersonSimulation.Fixed.SimulationWorldSnapshotFactory.Capture(
                    catalog,
                    new ThirdPersonSimulation.SimulationTick(1),
                    new[]
                    {
                        new ThirdPersonSimulation.Fixed.SimulationActorState(actorId, state)
                    },
                    world,
                    ThirdPersonSimulation.WorldCapability.None);
            byte[] snapshotBytes = ThirdPersonSimulation.Fixed.SimulationWorldSnapshotCodec.Write(snapshot);
            ThirdPersonSimulation.Fixed.SimulationWorldSnapshot restoredSnapshot =
                ThirdPersonSimulation.Fixed.SimulationWorldSnapshotCodec.Read(snapshotBytes);
            _ = restoredSnapshot.Actors[0].Decode(program);
            if (!restoredSnapshot.WorldHash.Equals(snapshot.WorldHash))
                throw new InvalidOperationException("Fixed World Snapshot hash changed after canonical round-trip.");
            snapshotHash = restoredSnapshot.WorldHash.ToString();
        }

        static void AppendFormalCompileReport(AgentCompileReport report, CharacterSimulationBuildResult result)
        {
            if (result?.Report == null)
            {
                report.Error("compiler", "formal_compile_report_missing", "正式 Character Simulation Compiler 没有返回报告。");
                return;
            }
            for (int i = 0; i < result.Report.Messages.Count; i++)
            {
                CharacterSimulationCompileMessage message = result.Report.Messages[i];
                string path = $"compiler/{message.Stage}/{message.SourceIdentity}";
                switch (message.Severity)
                {
                    case CharacterSimulationCompileSeverity.Information:
                        report.Info(path, message.Code, message.Message);
                        break;
                    case CharacterSimulationCompileSeverity.Warning:
                        report.Warning(path, message.Code, message.Message);
                        break;
                    case CharacterSimulationCompileSeverity.Error:
                        report.Error(path, message.Code, message.Message);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

}
