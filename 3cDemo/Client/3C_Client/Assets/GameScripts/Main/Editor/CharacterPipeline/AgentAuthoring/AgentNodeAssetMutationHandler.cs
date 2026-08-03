using System;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentNodeAssetMutationHandler : IAgentMutationHandler
    {
        readonly BtsmtlGraphAuthoringCapabilities m_Catalog;

        public AgentNodeAssetMutationHandler(
            BtsmtlGraphAuthoringCapabilities catalog)
        {
            m_Catalog = catalog;
        }

        public bool Preflight(AgentMutationSession session, AgentMutation command)
        {
            if (command is AgentConfigureActionAdmissionMutation admission)
                return PreflightAdmission(session, admission);
            if (command is AgentEnsureConditionValueNodeMutation conditionValue)
                return PreflightConditionValue(session, conditionValue);
            if (command is not AgentEnsureInputNodeMutation input)
                throw new InvalidOperationException($"Unsupported Node/Asset command: {command.Kind}");
            if (!session.TryResolveGraph(input.Graph, input.Path, out BaseTree graph))
                return false;

            string nodeType = ResolveNodeType(session, input);
            if (string.IsNullOrEmpty(nodeType))
            {
                session.Report.Error(input.Path, "input_not_found", $"输入定义无法解析：{input.InputId}");
                return false;
            }
            if (!m_Catalog.TryResolveNodeType(nodeType, out Type type))
            {
                session.Report.Error(input.Path, "unknown_node_type", $"节点类型未登记：{nodeType}");
                return false;
            }
            if (!ValidateBinding(session, input, type))
                return false;
            if (graph != null)
            {
                if (!session.TryResolveOptionalNode(graph, input.ExistingElement, input.Path, out BaseNode existing))
                    return false;
                if (existing != null && existing.GetType() != type)
                {
                    session.Report.Error(input.Path, "input_node_type_changed", "Input Node kind不能原地改变。");
                    return false;
                }
                if (existing == null && !graph.CanCreateNodeType(type))
                {
                    session.Report.Error(input.Path, "node_type_rejected", $"{graph.GetType().Name} 不能创建 {type.Name}。");
                    return false;
                }
            }
            session.AddPlanned(input, graph, input.DisplayName, "ensure input node");
            return true;
        }

        public void Apply(AgentMutationSession session, AgentMutation command)
        {
            if (command is AgentConfigureActionAdmissionMutation admission)
            {
                ApplyAdmission(session, admission);
                return;
            }
            if (command is AgentEnsureConditionValueNodeMutation conditionValue)
            {
                ApplyConditionValue(session, conditionValue);
                return;
            }
            if (command is not AgentEnsureInputNodeMutation input)
                throw new InvalidOperationException($"Unsupported Node/Asset command: {command.Kind}");
            if (!session.TryResolveGraph(input.Graph, input.Path, out BaseTree graph))
                return;
            if (!session.TryResolveOptionalNode(graph, input.ExistingElement, input.Path, out BaseNode existing))
                return;

            string nodeType = ResolveNodeType(session, input);
            if (string.IsNullOrEmpty(nodeType))
            {
                session.Report.Error(input.Path, "input_not_found", $"输入定义无法解析：{input.InputId}");
                return;
            }
            BaseNode node = existing;
            if (node == null &&
                !m_Catalog.TryCreateNode(graph, nodeType, input.DisplayName, input.Position, out node, session.Report, input.Path))
                return;
            node.DisplayName = input.DisplayName;
            node.Position = input.Position;
            if (!m_Catalog.ConfigureInputNode(node, input.InputId, session.Report, input.Path))
                return;
            session.AddApplied(input, graph, node, existing == null ? "input created" : "input configured");
        }

        static bool PreflightAdmission(AgentMutationSession session, AgentConfigureActionAdmissionMutation command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph) ||
                !session.TryResolveNode(graph, command.Element, command.Path, out BaseNode node))
                return false;
            if (node is not CanActivateActionInfoNode)
            {
                session.Report.Error(command.Path, "action_admission_node_type_invalid", "目标节点不是CanActivateActionInfoNode。");
                return false;
            }
            if (!session.Resolver.TryResolveActionProfile(command.ActionProfile.LogicalId, out _))
            {
                session.Report.Error(command.Path, "action_profile_not_found", $"ActionProfile未在当前Definition中找到：{command.ActionProfile.LogicalId}");
                return false;
            }
            session.AddPlanned(command, graph, node.ResolvedDisplayName, $"bind {command.ActionProfile.LogicalId}");
            return true;
        }

        static void ApplyAdmission(AgentMutationSession session, AgentConfigureActionAdmissionMutation command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph) ||
                !session.TryResolveNode(graph, command.Element, command.Path, out BaseNode node))
                return;
            if (node is not CanActivateActionInfoNode admission)
            {
                session.Report.Error(command.Path, "action_admission_node_type_invalid", "目标节点不是CanActivateActionInfoNode。");
                return;
            }
            if (!session.Resolver.TryResolveActionProfile(command.ActionProfile.LogicalId, out ActionProfile profile))
            {
                session.Report.Error(command.Path, "action_profile_not_found", $"ActionProfile未在当前Definition中找到：{command.ActionProfile.LogicalId}");
                return;
            }
            admission.ConfigureAuthoring(profile, admission.TargetSnapshotVariable);
            session.AddAppliedWithoutIdentity(command, graph, admission.ResolvedDisplayName, $"bound {command.ActionProfile.LogicalId}");
        }

        static string ResolveNodeType(AgentMutationSession session, AgentEnsureInputNodeMutation command)
        {
            if (!string.IsNullOrEmpty(command.NodeType))
                return command.NodeType;
            if (session.Resolver.TryResolveActionRequest(command.InputId, out _))
                return "CharacterActionRequestInfoNode";
            return session.Resolver.TryResolveInputValue(command.InputId, out CharacterInputValueDefinition value)
                ? BtsmtlGraphAuthoringCapabilities
                    .ResolveInputNodeType(value.ValueType)
                : string.Empty;
        }

        static bool ValidateBinding(AgentMutationSession session, AgentEnsureInputNodeMutation command, Type nodeType)
        {
            if (nodeType == typeof(CharacterActionRequestInfoNode))
            {
                if (session.Resolver.TryResolveActionRequest(command.InputId, out _))
                    return true;
                session.Report.Error(command.Path, "action_request_not_found", $"Action Request不属于当前Definition：{command.InputId}");
                return false;
            }
            if (!typeof(CharacterInputValueInfoNode).IsAssignableFrom(nodeType))
            {
                session.Report.Error(command.Path, "input_node_type_invalid", $"节点不是正式Character Input节点：{nodeType.Name}");
                return false;
            }
            if (!session.Resolver.TryResolveInputValue(command.InputId, out CharacterInputValueDefinition value))
            {
                session.Report.Error(command.Path, "input_value_not_found", $"Input Value不属于当前Definition：{command.InputId}");
                return false;
            }
            string expected =
                BtsmtlGraphAuthoringCapabilities
                    .ResolveInputNodeType(value.ValueType);
            bool magnitude = nodeType == typeof(CharacterInputVector2MagnitudeInfoNode) &&
                             value.ValueType == CharacterInputValueType.Vector2;
            if (string.Equals(expected, nodeType.FullName, StringComparison.Ordinal) || magnitude)
                return true;
            session.Report.Error(command.Path, "input_value_type_mismatch", $"{command.InputId}不能绑定到{nodeType.Name}。");
            return false;
        }

        bool PreflightConditionValue(AgentMutationSession session, AgentEnsureConditionValueNodeMutation command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph))
                return false;
            if (!m_Catalog.TryResolveNodeType(command.NodeType, out Type nodeType))
            {
                session.Report.Error(command.Path, "unknown_node_type", $"节点类型未登记：{command.NodeType}");
                return false;
            }
            if (!session.TryResolveOptionalNode(graph, command.ExistingElement, command.Path, out BaseNode existing))
                return false;
            if (existing != null && existing.GetType() != nodeType)
            {
                session.Report.Error(command.Path, "condition_value_node_type_changed", "Condition Value Node kind不能原地改变。");
                return false;
            }
            if (existing == null && !graph.CanCreateNodeType(nodeType))
            {
                session.Report.Error(command.Path, "node_type_rejected", $"{graph.GetType().Name}不能创建{nodeType.Name}。");
                return false;
            }
            if (!ValidateConditionValueConfiguration(session, command, nodeType))
                return false;
            session.AddPlanned(command, graph, command.DisplayName, existing == null ? "condition value created" : "condition value configured");
            return true;
        }

        void ApplyConditionValue(AgentMutationSession session, AgentEnsureConditionValueNodeMutation command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph) ||
                !m_Catalog.TryResolveNodeType(command.NodeType, out Type nodeType) ||
                !session.TryResolveOptionalNode(graph, command.ExistingElement, command.Path, out BaseNode existing))
                return;
            BaseNode node = existing;
            if (node == null &&
                !m_Catalog.TryCreateNode(graph, command.NodeType, command.DisplayName, command.Position, out node, session.Report, command.Path))
                return;

            BaseExposedProperty declaration = null;
            ActionContextSlot actionContext = null;
            ActionProfile actionProfile = null;
            PipelineBlackboardVariableReference targetSnapshot = default;
            if (command.ConfigurationKind == AgentConditionValueNodeConfigurationKind.BlackboardDeclaration &&
                !session.TryResolveDeclaration(session.RootTree, command.BlackboardDeclaration, command.Path, out declaration))
                return;
            if (command.ConfigurationKind == AgentConditionValueNodeConfigurationKind.ActionContext &&
                !session.Resolver.TryResolveActionContext(command.ActionContext, out actionContext))
            {
                session.Report.Error(command.Path, "action_context_not_found", $"Action Context无法解析：{command.ActionContext.LogicalId}");
                return;
            }
            if (command.ConfigurationKind == AgentConditionValueNodeConfigurationKind.ActionAdmission)
            {
                if (!session.Resolver.TryResolveActionProfile(command.ActionProfile.LogicalId, out actionProfile))
                {
                    session.Report.Error(command.Path, "action_profile_not_found", $"ActionProfile未在当前Definition中找到：{command.ActionProfile.LogicalId}");
                    return;
                }
                if (command.TargetSnapshotDeclaration.IsValid)
                {
                    if (!session.TryResolveDeclaration(session.RootTree, command.TargetSnapshotDeclaration, command.Path, out BaseExposedProperty targetDeclaration))
                        return;
                    if (targetDeclaration.ValueType != typeof(ActionTargetSnapshot))
                    {
                        session.Report.Error(command.Path, "blackboard_type_mismatch", "Action Admission目标快照必须引用ActionTargetSnapshot declaration。");
                        return;
                    }
                    targetSnapshot = targetDeclaration.CreateBlackboardReference();
                }
            }

            node.DisplayName = command.DisplayName;
            node.Position = command.Position;
            if (!m_Catalog.ConfigureConditionValueNode(
                    node,
                    command.ConfigurationKind,
                    declaration,
                    command.StateExitCause,
                    actionContext,
                    command.WindowType,
                    actionProfile,
                    targetSnapshot,
                    session.Report,
                    command.Path))
                return;
            session.AddApplied(command, graph, node, existing == null ? "condition value created" : "condition value configured");
        }

        static bool ValidateConditionValueConfiguration(
            AgentMutationSession session,
            AgentEnsureConditionValueNodeMutation command,
            Type nodeType)
        {
            switch (command.ConfigurationKind)
            {
                case AgentConditionValueNodeConfigurationKind.None:
                    return nodeType == typeof(CharacterMoveFacingAngleInfoNode);
                case AgentConditionValueNodeConfigurationKind.BlackboardDeclaration:
                    if (!typeof(PipelineBlackboardValueInfoNode).IsAssignableFrom(nodeType))
                        return FailConfiguration(session, command, nodeType);
                    if (!session.TryResolveDeclaration(session.RootTree, command.BlackboardDeclaration, command.Path, out BaseExposedProperty declaration))
                        return false;
                    if (declaration != null)
                    {
                        Type expected = nodeType == typeof(PipelineBlackboardBoolInfoNode) ? typeof(bool) : typeof(float);
                        if (declaration.ValueType != expected)
                        {
                            session.Report.Error(command.Path, "blackboard_type_mismatch", $"{nodeType.Name}需要{expected.Name} declaration。");
                            return false;
                        }
                    }
                    return true;
                case AgentConditionValueNodeConfigurationKind.StateExitCause:
                    return nodeType == typeof(StateExitCauseInfoNode) || FailConfiguration(session, command, nodeType);
                case AgentConditionValueNodeConfigurationKind.ActionContext:
                    if (nodeType != typeof(ActionContextActiveInfoNode))
                        return FailConfiguration(session, command, nodeType);
                    if (session.Resolver.TryResolveActionContext(command.ActionContext, out _))
                        return true;
                    session.Report.Error(command.Path, "action_context_not_found", $"Action Context无法解析：{command.ActionContext.LogicalId}");
                    return false;
                case AgentConditionValueNodeConfigurationKind.ActionWindow:
                    return nodeType == typeof(ActionWindowActiveInfoNode) || FailConfiguration(session, command, nodeType);
                case AgentConditionValueNodeConfigurationKind.ActionAdmission:
                    if (nodeType != typeof(CanActivateActionInfoNode))
                        return FailConfiguration(session, command, nodeType);
                    if (!session.Resolver.TryResolveActionProfile(command.ActionProfile.LogicalId, out _))
                    {
                        session.Report.Error(command.Path, "action_profile_not_found", $"ActionProfile未在当前Definition中找到：{command.ActionProfile.LogicalId}");
                        return false;
                    }
                    if (!command.TargetSnapshotDeclaration.IsValid)
                        return true;
                    if (!session.TryResolveDeclaration(session.RootTree, command.TargetSnapshotDeclaration, command.Path, out BaseExposedProperty target))
                        return false;
                    if (target == null || target.ValueType == typeof(ActionTargetSnapshot))
                        return true;
                    session.Report.Error(command.Path, "blackboard_type_mismatch", "Action Admission目标快照必须引用ActionTargetSnapshot declaration。");
                    return false;
                default:
                    return FailConfiguration(session, command, nodeType);
            }
        }

        static bool FailConfiguration(
            AgentMutationSession session,
            AgentEnsureConditionValueNodeMutation command,
            Type nodeType)
        {
            session.Report.Error(command.Path, "condition_value_configuration_mismatch", $"{nodeType.Name}与{command.ConfigurationKind}配置不匹配。");
            return false;
        }
    }
}
