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
                CharacterSimulationBuildResult result = CharacterSimulationBuildOrchestrator.DryRun(definition);
                AppendFormalCompileReport(report, result);
                bool semanticValid = result.Artifact != null && result.Report.IsValid;
                report.metrics.semanticValidCount = semanticValid ? 1 : 0;
                report.metrics.semanticInvalidCount = semanticValid ? 0 : 1;
                report.metrics.compileSuccessCount = result.IsValid ? 1 : 0;
                report.metrics.compileFailureCount = result.IsValid ? 0 : 1;
            }
            report.success = !report.HasErrors();
            return report;
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

