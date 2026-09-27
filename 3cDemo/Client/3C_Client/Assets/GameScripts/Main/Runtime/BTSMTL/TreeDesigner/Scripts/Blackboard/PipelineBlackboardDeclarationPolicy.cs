using System;
using BTSMTL.Authoring.Blackboard;

namespace TreeDesigner
{
    public static class PipelineBlackboardDeclarationPolicy
    {
        public static bool TryValidateInputBinding(BaseExposedProperty declaration, out string error)
        {
            error = string.Empty;
            if (declaration == null)
            {
                error = "Blackboard declaration is missing.";
                return false;
            }
            if (declaration.InputBinding == null)
                return true;
            if (string.IsNullOrWhiteSpace(declaration.InputBinding.InputValueId))
                error = "Blackboard Input Binding requires a stable InputValueId.";
            else if (declaration.BlackboardScope != PipelineBlackboardVariableScope.Character)
                error = "Blackboard Input Binding requires Character scope.";
            else if (declaration.BlackboardLifetime != PipelineBlackboardVariableLifetime.Spawn)
                error = "Blackboard Input Binding requires Spawn lifetime.";
            return string.IsNullOrEmpty(error);
        }

        public static bool TryValidateFactProjection(BaseExposedProperty declaration, out string error)
        {
            error = string.Empty;
            if (declaration == null || declaration.FactProjection == null)
                return true;

            if (declaration.FactProjection.Kind != PipelineBlackboardFactProjectionKind.ActionWindow)
            {
                error = $"Unsupported fact projection '{declaration.FactProjection.Kind}'.";
                return false;
            }

            if (declaration.ValueType != typeof(bool))
                error = "ActionWindow projection requires a Bool declaration.";
            else if (declaration.BlackboardScope != PipelineBlackboardVariableScope.Frame ||
                     declaration.BlackboardLifetime != PipelineBlackboardVariableLifetime.Frame)
                error = "ActionWindow projection requires Frame scope and Frame lifetime.";
            else if (string.IsNullOrWhiteSpace(declaration.FactProjection.ActionWindowType))
                error = "ActionWindow projection requires WindowType.";
            else if (string.IsNullOrWhiteSpace(declaration.FactProjection.ActionWindowId))
                error = "ActionWindow projection requires WindowId.";

            return string.IsNullOrEmpty(error);
        }
    }
}
