using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Control.Rules;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentControlDocumentMapper
    {
        public static AgentPackageControllerFile CreateController(
            AgentDocumentEditable editable)
        {
            AgentDocumentControlConfiguration control = editable?.control ??
                new AgentDocumentControlConfiguration();
            return new AgentPackageControllerFile
            {
                controlModuleId = control.moduleId,
                controlSemanticVersion = control.semanticVersion,
                controlParameters = (control.parameters ?? new List<AgentSnapshotControlParameter>())
                    .Where(value => value != null)
                    .OrderBy(value => value.id, StringComparer.Ordinal)
                    .Select(value => new AgentPackageControlParameter
                    {
                        id = value.id,
                        valueType = value.valueType,
                        numericValue = value.numericValue
                    })
                    .ToList()
            };
        }

        public static AgentDocumentControlConfiguration ReadController(
            AgentPackageControllerFile controller,
            AgentCompileReport report)
        {
            var result = new AgentDocumentControlConfiguration
            {
                moduleId = controller?.controlModuleId,
                semanticVersion = controller?.controlSemanticVersion ?? 0,
                parameters = (controller?.controlParameters ?? new List<AgentPackageControlParameter>())
                    .Select(value => value == null
                        ? null
                        : new AgentSnapshotControlParameter
                        {
                            id = value.id,
                            valueType = value.valueType,
                            numericValue = value.numericValue
                        })
                    .ToList()
            };
            Validate(result, report);
            return result;
        }

        public static bool Validate(
            AgentDocumentControlConfiguration control,
            AgentCompileReport report)
        {
            if (control == null)
            {
                report.Error("editable/controller.json", "control_configuration_missing", "CharacterController文档缺少控制配置。");
                return false;
            }
            bool valid = true;
            if (string.IsNullOrWhiteSpace(control.moduleId) || control.moduleId.Any(char.IsWhiteSpace))
            {
                report.Error("editable/controller.json.controlModuleId", "control_module_identity_invalid", "控制模块binding必须提供非空稳定identity。");
                valid = false;
            }
            if (control.semanticVersion < 0)
            {
                report.Error("editable/controller.json.controlSemanticVersion", "control_module_version_invalid", "控制模块版本不能为负数。");
                valid = false;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < (control.parameters?.Count ?? 0); i++)
            {
                AgentSnapshotControlParameter parameter = control.parameters[i];
                string path = $"editable/controller.json.controlParameters[{i}]";
                if (parameter == null ||
                    string.IsNullOrWhiteSpace(parameter.id) ||
                    parameter.id.Any(char.IsWhiteSpace) ||
                    !ids.Add(parameter.id) ||
                    string.IsNullOrWhiteSpace(parameter.valueType) ||
                    double.IsNaN(parameter.numericValue) ||
                    double.IsInfinity(parameter.numericValue))
                {
                    report.Error(path, "control_parameter_invalid", "控制参数必须具有唯一identity、值类型和有限数值。");
                    valid = false;
                    continue;
                }
                if (!Enum.TryParse(parameter.valueType, false, out SemanticValueKind _))
                {
                    report.Error(path + ".valueType", "control_parameter_type_invalid", $"控制参数值类型无效：{parameter.valueType}");
                    valid = false;
                }
            }
            if (valid)
            {
                try
                {
                    ICharacterControlModule module =
                        CorinCharacterControlModuleCatalog.Create().Require(
                            new CharacterControlModuleId(control.moduleId));
                    if (control.semanticVersion != module.Contract.SemanticVersion)
                    {
                        report.Error(
                            "editable/controller.json.controlSemanticVersion",
                            "control_module_version_mismatch",
                            $"控制模块版本{control.semanticVersion}与已登记模块版本{module.Contract.SemanticVersion}不一致。");
                        valid = false;
                    }
                    var values = new List<CharacterControlParameterValue>();
                    for (int i = 0; i < (control.parameters?.Count ?? 0); i++)
                    {
                        AgentSnapshotControlParameter parameter = control.parameters[i];
                        if (parameter == null ||
                            !Enum.TryParse(parameter.valueType, false, out SemanticValueKind valueKind))
                            continue;
                        values.Add(new CharacterControlParameterValue(
                            new CharacterControlParameterId(parameter.id),
                            valueKind,
                            parameter.numericValue));
                    }
                    if (!module.Contract.TryResolveParameterSet(values, out _, out IReadOnlyList<string> errors))
                    {
                        foreach (string error in errors)
                            report.Error("editable/controller.json.controlParameters", "control_parameter_contract_invalid", error);
                        valid = false;
                    }
                }
                catch (Exception exception)
                {
                    report.Error(
                        "editable/controller.json.controlModuleId",
                        "control_module_unavailable",
                        exception.Message);
                    valid = false;
                }
            }
            return valid;
        }

        public static bool SemanticEquals(
            AgentDocumentControlConfiguration left,
            AgentDocumentControlConfiguration right)
        {
            if (left == null || right == null)
                return left == right;
            if (!string.Equals(left.moduleId, right.moduleId, StringComparison.Ordinal) ||
                left.semanticVersion != right.semanticVersion)
                return false;
            var leftParameters = (left.parameters ?? new List<AgentSnapshotControlParameter>())
                .OrderBy(value => value?.id, StringComparer.Ordinal)
                .ToList();
            var rightParameters = (right.parameters ?? new List<AgentSnapshotControlParameter>())
                .OrderBy(value => value?.id, StringComparer.Ordinal)
                .ToList();
            if (leftParameters.Count != rightParameters.Count)
                return false;
            for (int i = 0; i < leftParameters.Count; i++)
            {
                AgentSnapshotControlParameter l = leftParameters[i];
                AgentSnapshotControlParameter r = rightParameters[i];
                if (l == null || r == null ||
                    !string.Equals(l.id, r.id, StringComparison.Ordinal) ||
                    !string.Equals(l.valueType, r.valueType, StringComparison.Ordinal) ||
                    l.numericValue != r.numericValue)
                    return false;
            }
            return true;
        }

        public static JToken ToToken(AgentDocumentEditable editable)
        {
            return AgentAuthoringDocumentCodec.ToToken(CreateController(editable));
        }
    }
}
