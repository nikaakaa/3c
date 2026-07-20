using System;
using ThirdPersonCharacter.Pipeline.Input;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentNodeAssetCommandHandler : IAgentPatchCommandHandler
    {
        readonly AgentNodeEmitterRegistry m_Emitters;

        public AgentNodeAssetCommandHandler(AgentNodeEmitterRegistry emitters)
        {
            m_Emitters = emitters;
        }

        public bool Preflight(AgentPatchCompileSession session, AgentPatchCommand command)
        {
            if (command is not AgentEnsureInputNodeCommand input)
                throw new InvalidOperationException($"Unsupported Node/Asset command: {command.Kind}");
            if (!session.TryResolveGraph(input.Graph, input.Path, out BaseTree graph))
                return false;

            string nodeType = ResolveNodeType(session, input);
            if (string.IsNullOrEmpty(nodeType))
            {
                session.Report.Error(input.Path, "input_not_found", $"输入定义无法解析：{input.InputId}");
                return false;
            }
            if (!m_Emitters.TryResolveNodeType(nodeType, out Type type))
            {
                session.Report.Error(input.Path, "unknown_node_type", $"节点类型未登记：{nodeType}");
                return false;
            }
            if (graph != null)
            {
                if (!session.TryResolveOptionalNode(graph, input.ExistingElement, input.Path, out BaseNode existing))
                    return false;
                if (existing == null && !graph.CanCreateNodeType(type))
                {
                    session.Report.Error(input.Path, "node_type_rejected", $"{graph.GetType().Name} 不能创建 {type.Name}。");
                    return false;
                }
            }
            session.AddPlanned(input, graph, input.DisplayName, "ensure input node");
            return true;
        }

        public void Apply(AgentPatchCompileSession session, AgentPatchCommand command)
        {
            if (command is not AgentEnsureInputNodeCommand input)
                throw new InvalidOperationException($"Unsupported Node/Asset command: {command.Kind}");
            if (!session.TryResolveGraph(input.Graph, input.Path, out BaseTree graph))
                return;
            if (!session.TryResolveOptionalNode(graph, input.ExistingElement, input.Path, out BaseNode existing))
                return;
            if (existing != null)
            {
                session.AddApplied(input, graph, existing, "exists");
                return;
            }

            string nodeType = ResolveNodeType(session, input);
            if (string.IsNullOrEmpty(nodeType))
            {
                session.Report.Error(input.Path, "input_not_found", $"输入定义无法解析：{input.InputId}");
                return;
            }
            if (!m_Emitters.TryCreateNode(graph, nodeType, input.DisplayName, input.Position, out BaseNode node, session.Report, input.Path))
                return;
            if (!m_Emitters.ConfigureInputNode(node, input.InputId, input.InputValueType, session.Report, input.Path))
                return;
            session.AddApplied(input, graph, node, "input");
        }

        static string ResolveNodeType(AgentPatchCompileSession session, AgentEnsureInputNodeCommand command)
        {
            if (!string.IsNullOrEmpty(command.NodeType))
                return command.NodeType;
            if (session.Resolver.TryResolveActionRequest(command.InputId, out _))
                return "CharacterActionRequestInfoNode";
            return session.Resolver.TryResolveInputValue(command.InputId, out CharacterInputValueDefinition value)
                ? AgentNodeEmitterRegistry.ResolveInputNodeType(value.ValueType)
                : string.Empty;
        }
    }
}
