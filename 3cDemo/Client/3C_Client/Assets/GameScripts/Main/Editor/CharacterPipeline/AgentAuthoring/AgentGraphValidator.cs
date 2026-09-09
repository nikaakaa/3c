using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.AI.Editor;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentGraphValidator
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

    public sealed class AgentAIControllerValidator
    {
        readonly BtsmtlGraphAuthoringCapabilities m_Catalog =
            new BtsmtlGraphAuthoringCapabilities();

        public AgentCompileReport Validate(AIControllerDefinition definition, bool validateCompiler = true)
        {
            var report = new AgentCompileReport
            {
                success = true,
                domain = AgentAuthoringSchema.AIControllerDomain,
                rootIdentity = definition ? definition.ControllerId : string.Empty
            };
            if (!definition)
            {
                report.Error("definition", "ai_definition_missing", "AIControllerDefinition 缺失。");
                return report;
            }

            var errors = new List<string>();
            if (!definition.CollectConfigurationErrors(errors))
            {
                for (int i = 0; i < errors.Count; i++)
                    report.Error("definition", "ai_definition_invalid", errors[i]);
                report.metrics.semanticInvalidCount += errors.Count;
                return report;
            }

            if (definition.RootTreeAsset.Tree is not AIControllerTree root)
            {
                report.Error("root", "ai_root_tree_invalid", "RootTree 不是 AIControllerTree。");
                return report;
            }
            root.RebindReadOnlyViewReferences();
            if (root.AuthoringRole != GraphAuthoringRole.AIController)
                report.Error("root", "ai_graph_role_invalid", $"AI root graph role 无效：{root.AuthoringRole}");

            var declarationIds = new HashSet<string>(StringComparer.Ordinal);
            var declarationKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < root.ExposedProperties.Count; i++)
            {
                BaseExposedProperty declaration = root.ExposedProperties[i];
                string path = $"root.blackboard[{i}]";
                if (declaration == null || !declarationIds.Add(declaration.DeclarationId) || !declarationKeys.Add(declaration.BlackboardKey))
                {
                    report.Error(path, "ai_blackboard_identity_invalid", "AI Blackboard declaration 缺失或 identity/key 重复。");
                    continue;
                }
                if (declaration.BlackboardScope != PipelineBlackboardVariableScope.AIController &&
                    declaration.BlackboardScope != PipelineBlackboardVariableScope.AITick &&
                    declaration.BlackboardScope != PipelineBlackboardVariableScope.Graph)
                    report.Error(path, "ai_blackboard_scope_invalid", $"AI Blackboard scope 不允许：{declaration.BlackboardScope}");
                if (declaration.BlackboardLifetime != PipelineBlackboardVariablePolicy.DefaultLifetime(declaration.BlackboardScope))
                    report.Error(path, "ai_blackboard_lifetime_invalid", "AI Blackboard lifetime 与 scope 不匹配。");
                if (declaration.InputBinding != null || declaration.FactProjection != null)
                    report.Error(path, "ai_blackboard_character_payload_forbidden", "AI Blackboard 不允许 Character Input Binding 或 Fact Projection。");
            }

            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < root.Nodes.Count; i++)
            {
                BaseNode node = root.Nodes[i];
                string path = $"root.nodes[{i}]";
                if (node == null || !nodeIds.Add(node.GUID))
                {
                    report.Error(path, "ai_node_identity_invalid", "AI node 缺失或 identity 重复。");
                    continue;
                }
                if (!m_Catalog.IsNodeTypeAllowed(node.GetType(), AgentAuthoringSchema.AIControllerDomain))
                    report.Error(path, "ai_node_capability_forbidden", $"AI Graph 禁止节点：{node.GetType().FullName}");
            }

            if (!validateCompiler)
            {
                report.Warning(
                    "compiler",
                    "ai_intent_compile_deferred",
                    "受控 Character Program 已过期；AI authoring 已验证，AIIntentProgram 保持 stale，等待 Character Program 重新发布后再编译。");
                report.metrics.semanticValidCount++;
            }
            else try
            {
                AIIntentProgramBuildService.Validate(definition);
                report.metrics.compileSuccessCount++;
                report.metrics.semanticValidCount++;
            }
            catch (Exception exception)
            {
                report.Error("compiler", "ai_intent_compile_validation_failed", exception.Message);
                report.metrics.compileFailureCount++;
                report.metrics.semanticInvalidCount++;
            }
            report.success = !report.HasErrors();
            return report;
        }
    }
}
