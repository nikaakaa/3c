using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Graph;
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
                bool semanticValid = result.Artifact != null && result.Report.IsValid;
                report.metrics.semanticValidCount = semanticValid ? 1 : 0;
                report.metrics.semanticInvalidCount = semanticValid ? 0 : 1;
                report.metrics.compileSuccessCount = result.IsValid ? 1 : 0;
                report.metrics.compileFailureCount = result.IsValid ? 0 : 1;
            }
            report.success = !report.HasErrors();
            return report;
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
