using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentNodeEmitterRegistry
    {
        readonly Dictionary<string, Type> m_NodeTypes = new Dictionary<string, Type>(StringComparer.Ordinal);

        public AgentNodeEmitterRegistry()
        {
            Register("StateMachineNode", typeof(StateMachineNode));
            Register("StateNode", typeof(StateNode));
            Register("SequenceNode", typeof(SequenceNode));
            Register("Sequence", typeof(SequenceNode));
            Register("SelectorNode", typeof(SelectorNode));
            Register("TimelineNode", typeof(TimelineNode));
            Register("ActivateActionInstanceNode", typeof(ActivateActionInstanceNode));
            Register("SubmitActionLifecycleTransitionNode", typeof(SubmitActionLifecycleTransitionNode));
            Register("CharacterActionRequestInfoNode", typeof(CharacterActionRequestInfoNode));
            Register("CharacterInputBoolInfoNode", typeof(CharacterInputBoolInfoNode));
            Register("CharacterInputFloatInfoNode", typeof(CharacterInputFloatInfoNode));
            Register("CharacterInputVector2InfoNode", typeof(CharacterInputVector2InfoNode));
            Register("CharacterInputVector2MagnitudeInfoNode", typeof(CharacterInputVector2MagnitudeInfoNode));
            Register("CharacterMoveFacingAngleInfoNode", typeof(CharacterMoveFacingAngleInfoNode));
            Register("PipelineBlackboardBoolInfoNode", typeof(PipelineBlackboardBoolInfoNode));
            Register("StateRootCompletedNode", typeof(StateRootCompletedNode));
            Register("StateExitCauseInfoNode", typeof(StateExitCauseInfoNode));
            Register("ActionContextActiveInfoNode", typeof(ActionContextActiveInfoNode));
            Register("ActionWindowActiveInfoNode", typeof(ActionWindowActiveInfoNode));
            Register("CanActivateActionInfoNode", typeof(CanActivateActionInfoNode));
            Register("SucceedNode", typeof(SucceedNode));
            Register("AndNode", typeof(AndNode));
            Register("NotNode", typeof(NotNode));
        }

        public void Register(string key, Type nodeType)
        {
            if (string.IsNullOrEmpty(key) || nodeType == null || !typeof(BaseNode).IsAssignableFrom(nodeType))
                return;

            m_NodeTypes[key] = nodeType;
            m_NodeTypes[nodeType.Name] = nodeType;
            m_NodeTypes[nodeType.FullName] = nodeType;
        }

        public bool TryResolveNodeType(string key, out Type nodeType)
        {
            nodeType = null;
            return !string.IsNullOrEmpty(key) && m_NodeTypes.TryGetValue(key, out nodeType);
        }

        public bool TryCreateNode(BaseGraph graph, string nodeTypeKey, string displayName, Vector2 position, out BaseNode node, AgentCompileReport report, string path)
        {
            node = null;
            if (graph == null)
            {
                report?.Error(path, "missing_graph", "目标 graph 缺失。");
                return false;
            }

            if (!TryResolveNodeType(nodeTypeKey, out Type nodeType))
            {
                report?.Error(path, "unknown_node_type", $"节点类型未登记：{nodeTypeKey}", "把节点类型加入 NodeEmitterRegistry 白名单。");
                return false;
            }

            if (!graph.CanCreateNodeType(nodeType))
            {
                report?.Error(path, "node_type_rejected", $"{graph.GetType().Name} 不能创建 {nodeType.Name}。", "检查目标 graph 层级，例如 TimelineNode 应放在状态行为图。");
                return false;
            }

            node = graph.CreateNode(nodeType);
            node.DisplayName = displayName;
            if (position != Vector2.zero)
                node.Position = position;
            return true;
        }

        public bool ConfigureTimelineNode(
            TimelineNode node,
            AgentTimelineOwnership ownership,
            TimelineAsset timelineAsset,
            ActionContextSlot actionContext,
            AgentCompileReport report,
            string path)
        {
            if (!node)
            {
                report?.Error(path, "missing_timeline_node", "TimelineNode 缺失。");
                return false;
            }

            if (ownership == AgentTimelineOwnership.Shared)
            {
                if (!timelineAsset)
                {
                    report?.Error(path, "missing_shared_timeline_asset", "Shared TimelineNode 必须显式解析 TimelineAsset。", "提供 timelineAssetPath 或 timelineAssetGuid。");
                    return false;
                }
                node.ConfigureSharedAuthoring(timelineAsset, actionContext);
                return true;
            }

            TimelineData inlineTimeline = timelineAsset
                ? timelineAsset.Data.Clone()
                : TimelineData.CreateDefault(node.DisplayName);
            node.ConfigureAuthoring(inlineTimeline, actionContext);
            return true;
        }

        public bool ConfigureActionActivationNode(
            ActivateActionInstanceNode node,
            ActionProfile actionProfile,
            string sourceInputRequestId,
            bool consumeSourceInputRequest,
            ActionContextSlot actionContext,
            string targetKey,
            PipelineBlackboardVariableReference targetSnapshotVariable,
            AgentCompileReport report,
            string path)
        {
            if (!node)
            {
                report?.Error(path, "missing_action_activation_node", "ActivateActionInstanceNode 缺失。");
                return false;
            }

            if (!actionProfile)
            {
                report?.Error(path, "missing_action_profile", "ActionProfile 无法解析。", "ActionProfile 必须来自当前 CharacterPipelineDefinition.ActionProfiles。");
                return false;
            }

            node.ConfigureAuthoring(actionProfile, sourceInputRequestId, consumeSourceInputRequest, actionContext, targetKey, targetSnapshotVariable);
            return true;
        }

        public bool ConfigureLifecycleNode(
            SubmitActionLifecycleTransitionNode node,
            ActionContextSlot actionContext,
            ActionLifecycleTransitionType transitionType,
            string reason,
            AgentCompileReport report,
            string path)
        {
            if (!node)
            {
                report?.Error(path, "missing_lifecycle_node", "SubmitActionLifecycleTransitionNode 缺失。");
                return false;
            }

            node.ConfigureAuthoring(actionContext, transitionType, reason);
            return true;
        }

        public bool ConfigureInputNode(BaseNode node, string inputId, string inputValueType, AgentCompileReport report, string path)
        {
            if (node is CharacterActionRequestInfoNode requestNode)
            {
                requestNode.BindActionRequest(inputId);
                return true;
            }

            if (node is CharacterInputValueInfoNode inputValueNode)
            {
                inputValueNode.BindInputValue(inputId);
                return true;
            }

            report?.Error(path, "unsupported_input_node", $"节点 {node?.GetType().Name ?? "null"} 不是受支持的输入节点。");
            return false;
        }

        public static string ResolveInputNodeType(CharacterInputValueType valueType)
        {
            switch (valueType)
            {
                case CharacterInputValueType.Bool:
                    return "CharacterInputBoolInfoNode";
                case CharacterInputValueType.Float:
                    return "CharacterInputFloatInfoNode";
                case CharacterInputValueType.Vector2:
                    return "CharacterInputVector2InfoNode";
                default:
                    return string.Empty;
            }
        }
    }
}
