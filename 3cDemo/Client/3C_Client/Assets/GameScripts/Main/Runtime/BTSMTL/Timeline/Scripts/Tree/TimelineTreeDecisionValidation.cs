using System;
using System.Collections.Generic;
using TreeDesigner;

namespace BTSMTL.Timeline
{
    public static class TimelineTreeDecisionValidation
    {
        public static bool Validate(
            TimelineRunningTree tree,
            List<string> errors,
            Func<PipelineBlackboardVariableReference, BaseExposedProperty> declarationResolver = null)
        {
            if (tree == null)
            {
                errors?.Add("Decision TreeClip is missing TimelineRunningTree.");
                return false;
            }

            bool valid = true;
            for (int i = 0; i < tree.Nodes.Count; i++)
            {
                BaseNode node = tree.Nodes[i];
                if (node == null)
                    continue;

                if (!IsDecisionNode(node))
                {
                    errors?.Add($"Decision TreeClip contains unsupported node capability: {node.GetType().Name} ({node.GUID}).");
                    valid = false;
                    continue;
                }

                if (node is ExposedPropertyNode exposedNode &&
                    exposedNode.NodeType == ExposedPropertyNodeType.Set &&
                    !ValidateDecisionOutput(tree, exposedNode, errors, declarationResolver))
                    valid = false;
            }

            return valid;
        }

        static bool IsDecisionNode(BaseNode node)
        {
            if (node is TimelineActionNode)
                return false;
            if (node is ActionNode && !(node is ExposedPropertyNode))
                return false;

            return node is RootNode ||
                   node is TimelineEnterNode ||
                   node is CompositeNode ||
                   node is SucceedNode ||
                   node is ExposedPropertyNode ||
                   node is ValueNode;
        }

        static bool ValidateDecisionOutput(
            TimelineRunningTree tree,
            ExposedPropertyNode node,
            List<string> errors,
            Func<PipelineBlackboardVariableReference, BaseExposedProperty> declarationResolver)
        {
            BaseExposedProperty variable = node.ExposedProperty;
            PipelineBlackboardVariableReference reference = node.BlackboardVariable;
            if (!variable && reference.IsValid)
            {
                for (int i = 0; i < tree.ExposedProperties.Count; i++)
                {
                    BaseExposedProperty declaration = tree.ExposedProperties[i];
                    if (declaration != null &&
                        declaration.DeclarationId == reference.DeclarationId &&
                        declaration.DeclarationOwnerId == reference.DeclarationOwnerId)
                    {
                        variable = declaration;
                        break;
                    }
                }
            }

            if (!variable && reference.IsValid)
                variable = declarationResolver?.Invoke(reference);

            if (!variable)
            {
                errors?.Add($"Decision blackboard setter has a missing declaration: {node.GUID}.");
                return false;
            }

            if (!node.BlackboardVariable.IsValid)
            {
                errors?.Add($"Decision blackboard setter has an invalid declaration reference: {node.GUID}.");
                return false;
            }

            if (variable.BlackboardScope != PipelineBlackboardVariableScope.Frame ||
                variable.BlackboardLifetime != PipelineBlackboardVariableLifetime.Frame)
            {
                errors?.Add($"Decision output '{variable.BlackboardKey}' must use Frame scope and Frame lifetime.");
                return false;
            }

            bool localGate = variable.FactProjection == null;
            bool projectedWindow = variable.FactProjection?.Kind == PipelineBlackboardFactProjectionKind.ActionWindow &&
                                   PipelineBlackboardFactProjectionPolicy.TryValidate(variable, out _);
            if (!localGate && !projectedWindow)
            {
                errors?.Add($"Decision output '{variable.BlackboardKey}' must be a local gate or a valid ActionWindow projection.");
                return false;
            }

            for (int i = 0; i < tree.ExposedProperties.Count; i++)
            {
                BaseExposedProperty local = tree.ExposedProperties[i];
                if (local == null ||
                    local.DeclarationId == variable.DeclarationId ||
                    local.BlackboardKey != variable.BlackboardKey)
                    continue;

                if (local.ValueType != variable.ValueType ||
                    local.BlackboardScope != variable.BlackboardScope ||
                    local.BlackboardLifetime != variable.BlackboardLifetime ||
                    local.InputValueId != variable.InputValueId ||
                    local.FactProjection?.Kind != variable.FactProjection?.Kind ||
                    variable.FactProjection?.Kind == PipelineBlackboardFactProjectionKind.ActionWindow &&
                    (local.FactProjection?.ActionWindowType != variable.FactProjection.ActionWindowType ||
                     local.FactProjection?.ActionWindowId != variable.FactProjection.ActionWindowId ||
                     local.FactProjection?.ActionWindowDigest != variable.FactProjection.ActionWindowDigest))
                {
                    errors?.Add($"Decision blackboard declaration conflicts with '{variable.BlackboardKey}'.");
                    return false;
                }
            }

            return true;
        }
    }
}
