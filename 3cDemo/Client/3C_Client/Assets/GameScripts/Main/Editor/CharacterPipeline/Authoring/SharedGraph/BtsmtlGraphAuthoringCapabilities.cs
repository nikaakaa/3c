using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class BtsmtlGraphAuthoringCapabilities
    {
        sealed class NodeDescriptor
        {
            public string Kind;
            public Type Type;
            public bool SystemOwned;
            public string Anchor;
            public string[] Properties;
            public List<string> GraphKinds;
            public List<AgentPackagePortDescriptor> FlowPorts;
            public List<AgentPackagePortDescriptor> PropertyPorts;
            public Func<BaseNode, JObject, bool> ConfigureFlowPorts;
            public IReadOnlyList<GraphAuthoringCommandDescriptor> Commands;
            public bool CanCreate;
            public bool CanConfigure;
            public bool CanDelete;
        }

        readonly List<NodeDescriptor> m_RegistrationDescriptors =
            new List<NodeDescriptor>();
        static readonly Dictionary<
            Type,
            Func<BaseNode, JObject, bool>>
            s_DynamicFlowPortConfigurations =
                new Dictionary<
                    Type,
                    Func<BaseNode, JObject, bool>>();
        static bool s_SharedRegistered;

        public static readonly GraphAuthoringDomainId SharedDomain = new GraphAuthoringDomainId("btsmtl");

        public BtsmtlGraphAuthoringCapabilities()
        {
            if (s_SharedRegistered)
                return;
            RegisterSystem<RootNode>("@root");
            RegisterSystem<StateMachineEnterNode>("@enter");
            RegisterSystem<StateMachineExitNode>("@exit");
            RegisterSystem<StateMachineAnyStateNode>("@any");
            RegisterSystem<StateOnEnterNode>("@onEnter");
            RegisterSystem<StateOnExitNode>("@onExit");
            RegisterSystem<TimelineEnterNode>("@timelineEnter");
            RegisterSystem<ConditionRuleResultNode>("@result");

            Register<StateMachineNode>("state-machine", "graphReferences");
            Register<StateNode>("state", "graphReferences");
            Register<SequenceNode>("sequence");
            Register<SelectorNode>("selector");
            Register<ParallelNode>("parallel");
            Register<LoopNode>("loop", "loopStopType");
            Register<SucceedNode>("succeed");
            Register<TimelineNode>(
                "timeline",
                TimelineCommands(),
                "graphReferences",
                "assetReferences");
            Register<ActivateActionInstanceNode>(
                "activate-action-instance",
                ActionAnimationWorkspaceCommand(),
                "assetReferences");
            Register<SubmitActionLifecycleTransitionNode>("submit-action-lifecycle", "assetReferences");
            Register<CharacterActionRequestInfoNode>("character-action-request", "requestId");
            Register<CharacterInputBoolInfoNode>("character-input-bool", "inputId");
            Register<CharacterInputFloatInfoNode>("character-input-float", "inputId");
            Register<CharacterInputVector2InfoNode>("character-input-vector2", "inputId");
            Register<CharacterInputVector2MagnitudeInfoNode>("character-input-vector2-magnitude", "inputId");
            Register<CharacterMoveFacingAngleInfoNode>("character-move-facing-angle");
            Register<PipelineBlackboardBoolInfoNode>("pipeline-blackboard-bool", "blackboardDeclarationId");
            Register<PipelineBlackboardFloatInfoNode>("pipeline-blackboard-float", "blackboardDeclarationId");
            Register<StateRootCompletedNode>("state-root-completed");
            Register<StateExitCauseInfoNode>("state-exit-cause", "stateExitCause");
            Register<ActionContextActiveInfoNode>("action-context-active", "actionContextId");
            Register<ActionWindowActiveInfoNode>("action-window-active", "windowType");
            Register<CanActivateActionInfoNode>("can-activate-action", "actionProfileId", "targetSnapshotBlackboardDeclarationId");
            Register<LocomotionInputMotionNode>(
                "locomotion-input-motion",
                "moveSpeed",
                "turnSpeedDegrees",
                "cameraRelative",
                "continuous");
            Register<AndNode>("and");
            Register<OrNode>("or");
            Register<NotNode>("not");
            Register<CompareNode>("compare", "compareType");
            Register<ExposedPropertyNode>("exposed-property", ConfigureExposedPropertyFlowPorts, "exposedProperty");
            Register<ReadSelfObservationNode>("ai-read-self");
            Register<EnumerateConfiguredCandidatesNode>("ai-enumerate-candidates");
            Register<SelectNearestCandidateNode>("ai-select-nearest-candidate");
            Register<ReadTargetDistanceNode>("ai-read-target-distance");
            Register<ReadTargetDirectionNode>("ai-read-target-direction");
            Register<ReadSelectedTargetSnapshotNode>("ai-read-target-snapshot");
            Register<ReadAIMemoryNode>("ai-read-memory");
            Register<WriteAIMemoryNode>("ai-write-memory");
            Register<WriteContinuousInputNode>("ai-write-continuous-input");
            Register<WriteActionTargetSnapshotNode>("ai-write-action-target");
            Register<SubmitActionRequestNode>("ai-submit-action-request");
            Register<AIWaitTicksNode>("ai-wait-ticks");
            EnsureSharedRegistered(m_RegistrationDescriptors);
            m_RegistrationDescriptors.Clear();
        }

        public GraphAuthoringCapabilityCatalog SharedCatalog => GraphAuthoringCapabilityRegistrationRoot.Catalog;

        public bool TryGetKind(string typeName, out string kind)
        {
            kind = null;
            if (!TryResolveDescriptor(
                    typeName,
                    out GraphAuthoringCapabilityDescriptor
                        descriptor))
                return false;
            kind = descriptor.ExternalKind;
            return true;
        }

        public bool TryGetTypeName(string kind, out string typeName)
        {
            typeName = null;
            if (!SharedCatalog.TryGetByExternalKind(
                    SharedDomain,
                    kind,
                    out GraphAuthoringCapabilityDescriptor
                        descriptor) ||
                descriptor.SystemOwned ||
                descriptor.AuthoringType == null)
                return false;
            typeName = descriptor.AuthoringType.FullName;
            return true;
        }

        public bool TryResolveNodeType(string kindOrTypeName, out Type type)
        {
            type = null;
            if (!TryResolveDescriptor(
                    kindOrTypeName,
                    out GraphAuthoringCapabilityDescriptor
                        descriptor) ||
                descriptor.SystemOwned)
                return false;
            type = descriptor.AuthoringType;
            return type != null;
        }

        public bool TryGetSharedCapability(
            BaseNode node,
            out GraphAuthoringCapabilityId capabilityId)
        {
            capabilityId = default;
            if (node == null ||
                !SharedCatalog.TryGetByAuthoringType(
                    SharedDomain,
                    node.GetType(),
                    out GraphAuthoringCapabilityDescriptor
                        descriptor))
                return false;
            capabilityId = descriptor.CapabilityId;
            return true;
        }

        public bool TryResolveSharedCapability(
            GraphAuthoringCapabilityId capabilityId,
            out Type type)
        {
            type = null;
            if (!capabilityId.IsValid)
                return false;
            GraphAuthoringCapabilityDescriptor descriptor;
            try
            {
                descriptor =
                    SharedCatalog.Require(capabilityId);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            if (!descriptor.DomainId.Equals(SharedDomain) ||
                descriptor.SystemOwned)
                return false;
            type = descriptor.AuthoringType;
            return type != null;
        }

        public bool TryGetAnchor(string typeName, out string anchor)
        {
            anchor = null;
            if (!TryResolveDescriptor(
                    typeName,
                    out GraphAuthoringCapabilityDescriptor
                        descriptor) ||
                !descriptor.SystemOwned)
                return false;
            anchor = descriptor.AnchorId;
            return true;
        }

        public bool IsSystemKind(string kind)
        {
            return
                SharedCatalog.TryGetByExternalKind(
                    SharedDomain,
                    kind,
                    out GraphAuthoringCapabilityDescriptor
                        descriptor) &&
                descriptor.SystemOwned;
        }

        public bool SupportsGenericMutation(string kindOrTypeName)
        {
            if (!TryResolveDescriptor(
                    kindOrTypeName,
                    out GraphAuthoringCapabilityDescriptor
                        descriptor))
                return false;
            return !descriptor.SystemOwned &&
                   descriptor.Fields.All(field =>
                       string.Equals(
                           field.FieldId.Value,
                           "loopStopType",
                           StringComparison.Ordinal) ||
                       string.Equals(
                           field.FieldId.Value,
                           "compareType",
                           StringComparison.Ordinal) ||
                       string.Equals(field.FieldId.Value, "moveSpeed", StringComparison.Ordinal) ||
                       string.Equals(field.FieldId.Value, "turnSpeedDegrees", StringComparison.Ordinal) ||
                       string.Equals(field.FieldId.Value, "cameraRelative", StringComparison.Ordinal) ||
                       string.Equals(field.FieldId.Value, "continuous", StringComparison.Ordinal));
        }

        public bool CanEditProperty(string kindOrTypeName, string property)
        {
            if (!TryResolveDescriptor(
                    kindOrTypeName,
                    out GraphAuthoringCapabilityDescriptor
                        descriptor))
                return false;
            if (descriptor.SystemOwned)
                return false;
            return descriptor.TryGetField(new GraphAuthoringFieldId(property), out GraphAuthoringFieldDescriptor field) &&
                   field.AuthoringWritable;
        }

        public bool IsFullyRoundTrippable(string kindOrTypeName)
        {
            if (!TryResolveDescriptor(
                    kindOrTypeName,
                    out GraphAuthoringCapabilityDescriptor
                        descriptor))
                return false;
            return !descriptor.SystemOwned;
        }

        public bool ValidateProperties(
            string kind,
            JObject properties,
            AgentCompileReport report,
            string path,
            bool allowExistingEmptyActionContext = false)
        {
            if (!TryDescribe(
                    kind,
                    out NodeDescriptor descriptor) ||
                descriptor.SystemOwned)
            {
                report.Error(path + ".kind", "unknown_node_kind", $"Node kind未登记：{kind}");
                return false;
            }
            if (properties == null)
                properties = new JObject();
            GraphAuthoringCapabilityDescriptor capability = SharedCatalog.Require(SharedCapabilityId(descriptor.Kind));
            HashSet<string> allowed = descriptor.CanConfigure
                ? new HashSet<string>(capability.Fields.Where(value => value.AuthoringWritable).Select(value => value.FieldId.Value), StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
            foreach (Newtonsoft.Json.Linq.JProperty property in properties.Properties())
            {
                if (allowed.Contains(property.Name))
                {
                    if (!ValidatePropertyValue(
                            property,
                            report,
                            path + ".properties." + property.Name,
                            allowExistingEmptyActionContext))
                        return false;
                    continue;
                }
                report.Error(path + ".properties." + property.Name, "unknown_node_property", $"{kind}不支持property：{property.Name}");
                return false;
            }
            foreach (string required in descriptor.Properties.Where(IsRequiredProperty))
            {
                if (allowExistingEmptyActionContext &&
                    string.Equals(required, "actionContextId", StringComparison.Ordinal))
                    continue;
                if (HasRequiredPropertyValue(properties[required]))
                    continue;
                report.Error(path + ".properties." + required, "node_property_required", $"{kind}必须声明有效{required}。");
                return false;
            }
            return true;
        }

        static bool HasRequiredPropertyValue(JToken value)
        {
            if (value == null || value.Type == JTokenType.Null)
                return false;
            return value.Type != JTokenType.String ||
                   !string.IsNullOrWhiteSpace(value.Value<string>());
        }

        static bool IsRequiredProperty(string property)
        {
            return IsRequiredStringProperty(property) ||
                   string.Equals(property, "moveSpeed", StringComparison.Ordinal) ||
                   string.Equals(property, "turnSpeedDegrees", StringComparison.Ordinal) ||
                   string.Equals(property, "cameraRelative", StringComparison.Ordinal) ||
                   string.Equals(property, "continuous", StringComparison.Ordinal);
        }

        static bool IsRequiredStringProperty(string property)
        {
            return string.Equals(property, "inputId", StringComparison.Ordinal) ||
                   string.Equals(property, "requestId", StringComparison.Ordinal) ||
                   string.Equals(property, "blackboardDeclarationId", StringComparison.Ordinal) ||
                   string.Equals(property, "stateExitCause", StringComparison.Ordinal) ||
                   string.Equals(property, "actionContextId", StringComparison.Ordinal) ||
                   string.Equals(property, "windowType", StringComparison.Ordinal) ||
                   string.Equals(property, "actionProfileId", StringComparison.Ordinal);
        }

        static bool ValidatePropertyValue(
            JProperty property,
            AgentCompileReport report,
            string path,
            bool allowExistingEmptyActionContext)
        {
            if (IsRequiredStringProperty(property.Name))
            {
                if (allowExistingEmptyActionContext &&
                    string.Equals(property.Name, "actionContextId", StringComparison.Ordinal) &&
                    property.Value.Type == JTokenType.String &&
                    string.IsNullOrEmpty(property.Value.Value<string>()))
                    return true;
                if (property.Value.Type != JTokenType.String ||
                    string.IsNullOrWhiteSpace(property.Value.Value<string>()))
                    return FailProperty(report, path, $"{property.Name}必须是非空String。");
                if (string.Equals(property.Name, "stateExitCause", StringComparison.Ordinal) &&
                    !Enum.TryParse(property.Value.Value<string>(), false, out StateExitCause _))
                    return FailProperty(report, path, "stateExitCause不是已登记枚举值。");
                return true;
            }
            if (string.Equals(property.Name, "targetSnapshotBlackboardDeclarationId", StringComparison.Ordinal))
            {
                return property.Value.Type == JTokenType.String ||
                       FailProperty(report, path, "targetSnapshotBlackboardDeclarationId必须是String。");
            }
            if (string.Equals(property.Name, "graphReferences", StringComparison.Ordinal))
            {
                return ValidateObjectArray(
                    property.Value,
                    new[] { "key", "graphId", "ownership", "sharedAssetPath" },
                    path,
                    report,
                    value =>
                        value["key"]?.Type == JTokenType.String &&
                        !string.IsNullOrWhiteSpace(value.Value<string>("key")) &&
                        value["graphId"]?.Type == JTokenType.String &&
                        !string.IsNullOrWhiteSpace(value.Value<string>("graphId")) &&
                        (value["sharedAssetPath"] == null || value["sharedAssetPath"].Type == JTokenType.String) &&
                        Enum.TryParse(value.Value<string>("ownership"), false, out AgentGraphOwnership ownership) &&
                        ownership != AgentGraphOwnership.Unknown);
            }
            if (string.Equals(property.Name, "assetReferences", StringComparison.Ordinal))
            {
                return ValidateObjectArray(
                    property.Value,
                    new[] { "key", "assetPath", "assetGuid" },
                    path,
                    report,
                    value =>
                        value["key"]?.Type == JTokenType.String &&
                        !string.IsNullOrWhiteSpace(value.Value<string>("key")) &&
                        (value["assetPath"] == null || value["assetPath"].Type == JTokenType.String) &&
                        (value["assetGuid"] == null || value["assetGuid"].Type == JTokenType.String));
            }
            if (string.Equals(property.Name, "exposedProperty", StringComparison.Ordinal))
            {
                if (property.Value is not JObject exposed ||
                    exposed.Properties().Any(value =>
                        value.Name != "mode" &&
                        value.Name != "declarationId" &&
                        value.Name != "valueType" &&
                        value.Name != "value") ||
                    exposed["mode"]?.Type != JTokenType.String ||
                    !Enum.TryParse(exposed.Value<string>("mode"), false, out ExposedPropertyNodeType mode) ||
                    exposed["declarationId"]?.Type != JTokenType.String ||
                    string.IsNullOrWhiteSpace(exposed.Value<string>("declarationId")) ||
                    exposed["valueType"]?.Type != JTokenType.String ||
                    string.IsNullOrWhiteSpace(exposed.Value<string>("valueType")))
                    return FailProperty(report, path, "exposedProperty必须声明有效mode、declarationId与valueType。");
                if (mode == ExposedPropertyNodeType.Get)
                    return exposed["value"] == null || exposed["value"].Type == JTokenType.Null ||
                           FailProperty(report, path + ".value", "Get节点不保存运行时value。");
                return exposed["value"] != null && exposed["value"].Type != JTokenType.Null ||
                       FailProperty(report, path + ".value", "Set节点必须声明value。");
            }
            if (string.Equals(property.Name, "loopStopType", StringComparison.Ordinal))
            {
                return property.Value.Type == JTokenType.String &&
                       Enum.TryParse(property.Value.Value<string>(), false, out LoopNode.StopType _) ||
                       FailProperty(report, path, "loopStopType不是已登记枚举值。");
            }
            if (string.Equals(property.Name, "compareType", StringComparison.Ordinal))
            {
                return property.Value.Type == JTokenType.String &&
                       Enum.TryParse(property.Value.Value<string>(), false, out CompareNode.CompareType _) ||
                       FailProperty(report, path, "compareType不是已登记枚举值。");
            }
            if (string.Equals(property.Name, "moveSpeed", StringComparison.Ordinal))
            {
                return TryReadFiniteFloat(property.Value, out float moveSpeed) && moveSpeed >= 0f ||
                       FailProperty(report, path, "moveSpeed必须是大于等于0的有限Float。");
            }
            if (string.Equals(property.Name, "turnSpeedDegrees", StringComparison.Ordinal))
            {
                return TryReadFiniteFloat(property.Value, out float turnSpeedDegrees) && turnSpeedDegrees > 0f ||
                       FailProperty(report, path, "turnSpeedDegrees必须是大于0的有限Float。");
            }
            if (string.Equals(property.Name, "cameraRelative", StringComparison.Ordinal) ||
                string.Equals(property.Name, "continuous", StringComparison.Ordinal))
            {
                return property.Value.Type == JTokenType.Boolean ||
                       FailProperty(report, path, $"{property.Name}必须是Boolean。");
            }
            return true;
        }

        static bool TryReadFiniteFloat(JToken value, out float result)
        {
            result = 0f;
            if (value == null ||
                value.Type != JTokenType.Float &&
                value.Type != JTokenType.Integer)
                return false;
            result = value.Value<float>();
            return !float.IsNaN(result) && !float.IsInfinity(result);
        }

        static bool ValidateObjectArray(
            JToken token,
            IEnumerable<string> allowedFields,
            string path,
            AgentCompileReport report,
            Func<JObject, bool> validate)
        {
            if (token is not JArray array)
                return FailProperty(report, path, "property必须是Array。");
            var allowed = new HashSet<string>(allowedFields, StringComparer.Ordinal);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < array.Count; i++)
            {
                if (array[i] is JObject value &&
                    value.Properties().All(property => allowed.Contains(property.Name)) &&
                    validate(value) &&
                    (value["key"] == null || keys.Add(value.Value<string>("key"))))
                    continue;
                return FailProperty(report, $"{path}[{i}]", "property对象包含未知字段、缺失字段或字段类型错误。");
            }
            return true;
        }

        static bool FailProperty(AgentCompileReport report, string path, string message)
        {
            report.Error(path, "node_property_invalid", message);
            return false;
        }

        public IReadOnlyList<AgentPackageNodeKindDescriptor> ExportNodeKinds(
            IReadOnlyList<AgentSnapshotGraph> graphs,
            string domain)
        {
            var portsByKind = new Dictionary<string, Dictionary<string, AgentPackagePortDescriptor>>(StringComparer.Ordinal);
            foreach (AgentSnapshotGraph graph in graphs ?? Array.Empty<AgentSnapshotGraph>())
            {
                foreach (AgentSnapshotNode node in graph.nodes ?? new List<AgentSnapshotNode>())
                {
                    if (!TryGetKind(node.typeName, out string kind) || IsSystemKind(kind))
                        continue;
                    if (!portsByKind.TryGetValue(kind, out Dictionary<string, AgentPackagePortDescriptor> ports))
                    {
                        ports = new Dictionary<string, AgentPackagePortDescriptor>(StringComparer.Ordinal);
                        portsByKind.Add(kind, ports);
                    }
                    foreach (AgentSnapshotPropertyPort port in node.propertyPorts ?? new List<AgentSnapshotPropertyPort>())
                    {
                        if (port != null && !string.IsNullOrEmpty(port.portId))
                            ports[port.portId] = new AgentPackagePortDescriptor
                            {
                                key = port.portId,
                                direction = port.direction,
                                valueType = StableValueType(port.valueType)
                            };
                    }
                }
            }

            return Descriptors()
                .Where(value =>
                    !value.SystemOwned &&
                    value.CanCreate &&
                    value.CanConfigure &&
                    value.CanDelete &&
                    IsNodeTypeAllowed(value.Type, domain))
                .OrderBy(value => value.Kind, StringComparer.Ordinal)
                .Select(value => new AgentPackageNodeKindDescriptor
                {
                    kind = value.Kind,
                    graphKinds = value.GraphKinds
                        .Where(graphKind => IsGraphKindAllowed(graphKind, domain))
                        .OrderBy(graphKind => graphKind, StringComparer.Ordinal)
                        .ToList(),
                    properties = value.CanConfigure ? value.Properties.ToList() : new List<string>(),
                    defaults = Defaults(value.Kind),
                    flowPorts = value.FlowPorts,
                    propertyPorts = MergePorts(value.PropertyPorts, portsByKind.TryGetValue(value.Kind, out Dictionary<string, AgentPackagePortDescriptor> ports)
                        ? ports.Values
                        : Array.Empty<AgentPackagePortDescriptor>()),
                    canCreate = value.CanCreate,
                    canConfigure = value.CanConfigure,
                    canDelete = value.CanDelete
                })
                .ToList();
        }

        public IReadOnlyList<AgentPackageGraphKindDescriptor> ExportGraphKinds(string domain)
        {
            return Enum.GetNames(typeof(AgentGraphKind))
                .Where(value => !string.Equals(value, AgentGraphKind.Unknown.ToString(), StringComparison.Ordinal))
                .Where(value => IsGraphKindAllowed(value, domain))
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(value => new AgentPackageGraphKindDescriptor
                {
                    kind = value,
                    ownerSlot = ResolveOwnerSlot(value),
                    nodeKinds = Descriptors()
                        .Where(descriptor =>
                            !descriptor.SystemOwned &&
                            descriptor.CanCreate &&
                            descriptor.CanConfigure &&
                            descriptor.CanDelete &&
                            descriptor.GraphKinds.Contains(value) &&
                            IsNodeTypeAllowed(descriptor.Type, domain))
                        .Select(descriptor => descriptor.Kind)
                        .OrderBy(kind => kind, StringComparer.Ordinal)
                        .ToList(),
                    anchors = ResolveAnchors(value, domain)
                })
                .ToList();
        }

        public bool TryCreateNode(
            BaseGraph graph,
            string nodeKind,
            string displayName,
            Vector2 position,
            out BaseNode node,
            AgentCompileReport report,
            string path)
        {
            node = null;
            if (graph == null)
            {
                report?.Error(path, "missing_graph", "目标Graph缺失。");
                return false;
            }
            if (!TryDescribe(
                    nodeKind,
                    out NodeDescriptor descriptor) ||
                descriptor.SystemOwned)
            {
                report?.Error(path, "unknown_node_kind", $"Node kind未登记：{nodeKind}");
                return false;
            }
            if (!graph.CanCreateNodeType(descriptor.Type))
            {
                report?.Error(path, "node_kind_rejected", $"{graph.GetType().Name}不能创建{nodeKind}。");
                return false;
            }
            node = graph.CreateNode(descriptor.Type);
            node.DisplayName = displayName;
            if (position != Vector2.zero)
                node.Position = position;
            return true;
        }

        public bool TryCreateNodeByTypeName(
            BaseGraph graph,
            string typeName,
            string displayName,
            Vector2 position,
            out BaseNode node,
            AgentCompileReport report,
            string path)
        {
            node = null;
            if (!TryGetKind(typeName, out string kind))
            {
                report?.Error(path, "unknown_node_type", $"Node类型未登记：{typeName}");
                return false;
            }
            return TryCreateNode(graph, kind, displayName, position, out node, report, path);
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
                report?.Error(path, "missing_timeline_node", "TimelineNode缺失。");
                return false;
            }
            if (ownership == AgentTimelineOwnership.Shared)
            {
                if (!timelineAsset)
                {
                    report?.Error(path, "missing_shared_timeline_asset", "Shared TimelineNode必须显式解析TimelineAsset。");
                    return false;
                }
                node.ConfigureSharedAuthoring(timelineAsset, actionContext);
                return true;
            }
            TimelineData inlineTimeline = timelineAsset ? timelineAsset.Data.Clone() : TimelineData.CreateDefault(node.DisplayName);
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
            if (!node || !actionProfile)
            {
                report?.Error(path, "action_activation_configuration_invalid", "Action activation Node或ActionProfile缺失。");
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
                report?.Error(path, "missing_lifecycle_node", "Lifecycle Node缺失。");
                return false;
            }
            node.ConfigureAuthoring(actionContext, transitionType, reason);
            return true;
        }

        public bool ConfigureInputNode(BaseNode node, string inputId, AgentCompileReport report, string path)
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
            report?.Error(path, "unsupported_input_node", $"Node {node?.GetType().Name ?? "null"}不支持Input binding。");
            return false;
        }

        public bool TryResolveInputValueCapability(
            CharacterInputValueType valueType,
            out GraphAuthoringCapabilityDescriptor descriptor)
        {
            return SharedCatalog.TryGetByExternalKind(
                SharedDomain,
                ResolveInputNodeKind(valueType),
                out descriptor);
        }

        public bool TryResolveActionRequestCapability(
            out GraphAuthoringCapabilityDescriptor descriptor)
        {
            return SharedCatalog.TryGetByExternalKind(
                SharedDomain,
                "character-action-request",
                out descriptor);
        }

        public bool ConfigureConditionValueNode(
            BaseNode node,
            AgentConditionValueNodeConfigurationKind configuration,
            BaseExposedProperty declaration,
            StateExitCause stateExitCause,
            ActionContextSlot actionContext,
            string windowType,
            ActionProfile actionProfile,
            PipelineBlackboardVariableReference targetSnapshot,
            AgentCompileReport report,
            string path)
        {
            switch (configuration)
            {
                case AgentConditionValueNodeConfigurationKind.None:
                    return true;
                case AgentConditionValueNodeConfigurationKind.BlackboardDeclaration when node is PipelineBlackboardValueInfoNode blackboard:
                    blackboard.ConfigureAuthoring(declaration);
                    return true;
                case AgentConditionValueNodeConfigurationKind.StateExitCause when node is StateExitCauseInfoNode exitCause:
                    exitCause.ConfigureAuthoring(stateExitCause);
                    return true;
                case AgentConditionValueNodeConfigurationKind.ActionContext when node is ActionContextActiveInfoNode context:
                    context.ConfigureAuthoring(actionContext);
                    return true;
                case AgentConditionValueNodeConfigurationKind.ActionWindow when node is ActionWindowActiveInfoNode window:
                    window.ConfigureAuthoring(windowType);
                    return true;
                case AgentConditionValueNodeConfigurationKind.ActionAdmission when node is CanActivateActionInfoNode admission:
                    admission.ConfigureAuthoring(actionProfile, targetSnapshot);
                    return true;
                default:
                    report?.Error(path, "condition_value_configuration_mismatch", $"{node?.GetType().Name ?? "null"}与{configuration}配置不匹配。");
                    return false;
            }
        }

        public static string ResolveInputNodeType(CharacterInputValueType valueType)
        {
            BtsmtlGraphAuthoringCapabilities capabilities =
                new BtsmtlGraphAuthoringCapabilities();
            return capabilities.TryResolveInputValueCapability(
                       valueType,
                       out GraphAuthoringCapabilityDescriptor descriptor)
                ? descriptor.AuthoringType?.FullName ?? string.Empty
                : string.Empty;
        }

        static string ResolveInputNodeKind(CharacterInputValueType valueType)
        {
            switch (valueType)
            {
                case CharacterInputValueType.Bool:
                    return "character-input-bool";
                case CharacterInputValueType.Float:
                    return "character-input-float";
                case CharacterInputValueType.Vector2:
                    return "character-input-vector2";
                default:
                    return string.Empty;
            }
        }

        void Register<T>(string kind, params string[] properties) where T : BaseNode
        {
            Register(
                kind,
                typeof(T),
                false,
                null,
                properties,
                null,
                Array.Empty<GraphAuthoringCommandDescriptor>());
        }

        void Register<T>(
            string kind,
            IReadOnlyList<GraphAuthoringCommandDescriptor> commands,
            params string[] properties)
            where T : BaseNode
        {
            Register(
                kind,
                typeof(T),
                false,
                null,
                properties,
                null,
                commands);
        }

        void Register<T>(
            string kind,
            Func<BaseNode, JObject, bool> configureFlowPorts,
            params string[] properties)
            where T : BaseNode
        {
            Register(
                kind,
                typeof(T),
                false,
                null,
                properties,
                configureFlowPorts,
                Array.Empty<GraphAuthoringCommandDescriptor>());
        }

        void RegisterSystem<T>(string anchor) where T : BaseNode
        {
            Register(
                anchor,
                typeof(T),
                true,
                anchor,
                Array.Empty<string>(),
                null,
                Array.Empty<GraphAuthoringCommandDescriptor>());
        }

        void Register(
            string kind,
            Type type,
            bool systemOwned,
            string anchor,
            string[] properties,
            Func<BaseNode, JObject, bool> configureFlowPorts,
            IReadOnlyList<GraphAuthoringCommandDescriptor> commands)
        {
            CreatePorts(type, out List<AgentPackagePortDescriptor> flowPorts, out List<AgentPackagePortDescriptor> propertyPorts);
            var descriptor = new NodeDescriptor
            {
                Kind = kind,
                Type = type,
                SystemOwned = systemOwned,
                Anchor = anchor,
                Properties = properties ?? Array.Empty<string>(),
                GraphKinds = ResolveGraphKinds(type),
                FlowPorts = flowPorts,
                PropertyPorts = propertyPorts,
                ConfigureFlowPorts = configureFlowPorts,
                Commands = commands ??
                    Array.Empty<GraphAuthoringCommandDescriptor>(),
                CanCreate = !systemOwned && SupportsCreate(kind),
                CanConfigure = !systemOwned && SupportsConfigure(kind),
                CanDelete = !systemOwned
            };
            if (m_RegistrationDescriptors.Any(value =>
                    string.Equals(
                        value.Kind,
                        kind,
                        StringComparison.Ordinal) ||
                    value.Type == type))
            {
                throw new InvalidOperationException(
                    $"BTSMTL capability '{kind}' or authoring type '{type.FullName}' is duplicated.");
            }
            m_RegistrationDescriptors.Add(descriptor);
            if (configureFlowPorts != null)
            {
                s_DynamicFlowPortConfigurations[type] =
                    configureFlowPorts;
            }
        }

        static void EnsureSharedRegistered(IEnumerable<NodeDescriptor> descriptors)
        {
            if (s_SharedRegistered)
                return;
            NodeDescriptor[] values = descriptors.OrderBy(value => value.Kind, StringComparer.Ordinal).ToArray();
            GraphAuthoringCapabilityRegistrationRoot.RegisterDomain("btsmtl.graph", catalog =>
            {
                for (int i = 0; i < values.Length; i++)
                    catalog.Register(ToSharedDescriptor(values[i]));
            });
            s_SharedRegistered = true;
        }

        static GraphAuthoringCapabilityDescriptor ToSharedDescriptor(NodeDescriptor descriptor)
        {
            GraphAuthoringDocumentRoleId[] roles = descriptor.GraphKinds
                .Select(SharedRoleId)
                .Distinct()
                .ToArray();
            var fields = new List<GraphAuthoringFieldDescriptor>();
            for (int i = 0; i < descriptor.Properties.Length; i++)
            {
                string property = descriptor.Properties[i];
                bool referenceOnly =
                    property.EndsWith("References", StringComparison.Ordinal);
                fields.Add(new GraphAuthoringFieldDescriptor(
                    new GraphAuthoringFieldId(property),
                    SplitDisplayName(property),
                    SharedFieldKind(property),
                    referenceOnly
                        ? GraphAuthoringFieldAccess.ReferenceRead
                        : descriptor.CanConfigure
                        ? GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite
                        : GraphAuthoringFieldAccess.ReferenceRead,
                    constraint: SharedFieldConstraint(property),
                    pickerKind: SharedPickerKind(property)));
            }
            var ports = new List<GraphAuthoringPortDescriptor>();
            if (descriptor.ConfigureFlowPorts == null)
            {
                AddSharedPorts(
                    ports,
                    descriptor.FlowPorts,
                    "flow",
                    descriptor.Type);
            }
            AddSharedPorts(
                ports,
                descriptor.PropertyPorts,
                "property",
                descriptor.Type);
            GraphAuthoringDynamicPortPolicy dynamicPortPolicy = descriptor.ConfigureFlowPorts == null
                ? GraphAuthoringDynamicPortPolicy.None
                : GraphAuthoringDynamicPortPolicy.OrderedBidirectional;
            return new GraphAuthoringCapabilityDescriptor(
                SharedCapabilityId(descriptor.Kind),
                SharedDomain,
                roles,
                descriptor.SystemOwned ? descriptor.Anchor : SplitDisplayName(descriptor.Kind),
                SharedCategory(descriptor.Type, descriptor.SystemOwned),
                SharedColor(descriptor.Type, descriptor.SystemOwned),
                fields,
                ports,
                dynamicPortPolicy,
                commands: descriptor.Commands,
                presentationKind: SharedPresentationKind(descriptor.Type),
                mutationBindingId: descriptor.SystemOwned ? string.Empty : "btsmtl.node",
                validationBindingId: "btsmtl.node",
                compilerBindingId: "btsmtl.node." + descriptor.Kind,
                documentCodecId: "btsmtl.graph-node",
                authoringType: descriptor.Type,
                externalKind: descriptor.Kind,
                systemOwned: descriptor.SystemOwned,
                anchorId: descriptor.Anchor);
        }

        static IReadOnlyList<GraphAuthoringCommandDescriptor>
            ActionAnimationWorkspaceCommand() =>
            new[]
            {
                new GraphAuthoringCommandDescriptor(
                    ActionAnimationWorkspaceCommands.Open,
                    "Open Action Animation Workspace",
                    false)
            };

        static IReadOnlyList<GraphAuthoringCommandDescriptor>
            TimelineCommands() =>
            new[]
            {
                new GraphAuthoringCommandDescriptor(
                    ActionAnimationWorkspaceCommands.Open,
                    "Open Action Animation Workspace",
                    false),
                new GraphAuthoringCommandDescriptor(
                    TimelineAuthoringCommands.UseInline,
                    "Use Inline Timeline",
                    false,
                    GraphAuthoringCommandPresentationKind.Custom),
                new GraphAuthoringCommandDescriptor(
                    TimelineAuthoringCommands.UseShared,
                    "Use Shared Timeline",
                    false,
                    GraphAuthoringCommandPresentationKind.Custom)
            };

        static void AddSharedPorts(
            ICollection<GraphAuthoringPortDescriptor> target,
            IReadOnlyList<AgentPackagePortDescriptor> source,
            string family,
            Type authoringType)
        {
            for (int i = 0; i < source.Count; i++)
            {
                AgentPackagePortDescriptor port = source[i];
                bool input = string.Equals(port.direction, "input", StringComparison.OrdinalIgnoreCase);
                target.Add(new GraphAuthoringPortDescriptor(
                    new GraphAuthoringPortId(family + ":" + port.key),
                    SplitDisplayName(port.key),
                    "btsmtl." + family,
                    input ? GraphAuthoringPortDirection.Input : GraphAuthoringPortDirection.Output,
                    SharedCapacity(
                        authoringType,
                        family,
                        port.key,
                        input),
                    input,
                    i));
            }
        }

        static GraphAuthoringPortCapacity SharedCapacity(
            Type authoringType,
            string family,
            string portName,
            bool input)
        {
            if (string.Equals(
                    family,
                    "property",
                    StringComparison.Ordinal))
            {
                return input
                    ? GraphAuthoringPortCapacity.Single
                    : GraphAuthoringPortCapacity.Multiple;
            }
            BaseNode node =
                (BaseNode)Activator.CreateInstance(authoringType);
            FlowPortDeclaration declaration =
                node.GetSupportedFlowPortDeclarations(null)
                    .Single(value =>
                        value.Direction ==
                            (input
                                ? PortDirection.Input
                                : PortDirection.Output) &&
                        string.Equals(
                            value.Name,
                            portName,
                            StringComparison.Ordinal));
            return BtsmtlSharedGraphPort.Capacity(
                declaration.Capacity);
        }

        public static GraphAuthoringCapabilityId SharedCapabilityId(string kind) =>
            new GraphAuthoringCapabilityId(kind.StartsWith("@", StringComparison.Ordinal)
                ? "btsmtl.anchor." + kind.Substring(1)
                : "btsmtl." + kind);

        bool TryResolveDescriptor(
            string kindOrTypeName,
            out GraphAuthoringCapabilityDescriptor descriptor)
        {
            descriptor = null;
            if (string.IsNullOrWhiteSpace(kindOrTypeName))
                return false;
            if (SharedCatalog.TryGetByExternalKind(
                    SharedDomain,
                    kindOrTypeName,
                    out descriptor))
                return true;
            descriptor = SharedCatalog.GetDomain(SharedDomain)
                .SingleOrDefault(value =>
                    value.AuthoringType != null &&
                    (string.Equals(
                         value.AuthoringType.FullName,
                         kindOrTypeName,
                         StringComparison.Ordinal) ||
                     string.Equals(
                         value.AuthoringType.Name,
                         kindOrTypeName,
                         StringComparison.Ordinal)));
            return descriptor != null;
        }

        IEnumerable<NodeDescriptor> Descriptors() =>
            SharedCatalog.GetDomain(SharedDomain)
                .Select(Describe);

        bool TryDescribe(
            string kindOrTypeName,
            out NodeDescriptor descriptor)
        {
            descriptor = null;
            if (!TryResolveDescriptor(
                    kindOrTypeName,
                    out GraphAuthoringCapabilityDescriptor
                        capability))
                return false;
            descriptor = Describe(capability);
            return true;
        }

        bool TryDescribe(
            Type type,
            out NodeDescriptor descriptor)
        {
            descriptor = null;
            if (type == null ||
                !SharedCatalog.TryGetByAuthoringType(
                    SharedDomain,
                    type,
                    out GraphAuthoringCapabilityDescriptor
                        capability))
                return false;
            descriptor = Describe(capability);
            return true;
        }

        static NodeDescriptor Describe(
            GraphAuthoringCapabilityDescriptor capability)
        {
            if (capability?.AuthoringType == null ||
                string.IsNullOrWhiteSpace(
                    capability.ExternalKind))
            {
                throw new InvalidOperationException(
                    $"BTSMTL capability '{capability?.CapabilityId}' has no authoring type or external kind.");
            }
            CreatePorts(
                capability.AuthoringType,
                out List<AgentPackagePortDescriptor> flowPorts,
                out List<AgentPackagePortDescriptor> propertyPorts);
            s_DynamicFlowPortConfigurations.TryGetValue(
                capability.AuthoringType,
                out Func<BaseNode, JObject, bool>
                    configureFlowPorts);
            return new NodeDescriptor
            {
                Kind = capability.ExternalKind,
                Type = capability.AuthoringType,
                SystemOwned = capability.SystemOwned,
                Anchor = capability.AnchorId,
                Properties = capability.Fields
                    .Select(value => value.FieldId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray(),
                GraphKinds = Enum.GetNames(
                        typeof(AgentGraphKind))
                    .Where(value =>
                        !string.Equals(
                            value,
                            AgentGraphKind.Unknown.ToString(),
                            StringComparison.Ordinal) &&
                        capability.Allows(
                            SharedRoleId(value)))
                    .ToList(),
                FlowPorts = flowPorts,
                PropertyPorts = propertyPorts,
                ConfigureFlowPorts = configureFlowPorts,
                Commands = capability.Commands,
                CanCreate = !capability.SystemOwned,
                CanConfigure = !capability.SystemOwned,
                CanDelete = !capability.SystemOwned
            };
        }

        public static GraphAuthoringDocumentRoleId SharedRoleId(string graphKind) =>
            new GraphAuthoringDocumentRoleId("btsmtl." + ToKebabCase(graphKind));

        public static GraphAuthoringDocumentRoleId SharedRoleId(BaseGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            if (graph is ConditionRuleGraph)
                return SharedRoleId(AgentGraphKind.ConditionRuleGraph.ToString());
            if (graph is StateMachineGraph)
                return SharedRoleId(AgentGraphKind.StateMachineGraph.ToString());
            if (graph is StateBehaviorSubTree)
                return SharedRoleId(AgentGraphKind.StateBehaviorSubTree.ToString());
            if (graph is SubTree)
                return SharedRoleId(AgentGraphKind.SubTree.ToString());
            if (graph is RunnableTree)
                return SharedRoleId(AgentGraphKind.RunnableTree.ToString());
            if (graph is BaseTree)
                return SharedRoleId(AgentGraphKind.BaseTree.ToString());
            throw new InvalidOperationException(
                $"BTSMTL Graph type '{graph.GetType().FullName}' has no registered authoring role.");
        }

        static GraphAuthoringFieldValueKind SharedFieldKind(string property)
        {
            if (property.EndsWith("References", StringComparison.Ordinal))
                return GraphAuthoringFieldValueKind.Object;
            if (string.Equals(property, "moveSpeed", StringComparison.Ordinal) ||
                string.Equals(property, "turnSpeedDegrees", StringComparison.Ordinal))
                return GraphAuthoringFieldValueKind.Float;
            if (string.Equals(property, "cameraRelative", StringComparison.Ordinal) ||
                string.Equals(property, "continuous", StringComparison.Ordinal))
                return GraphAuthoringFieldValueKind.Boolean;
            if (string.Equals(property, "loopStopType", StringComparison.Ordinal) ||
                string.Equals(property, "compareType", StringComparison.Ordinal) ||
                string.Equals(property, "stateExitCause", StringComparison.Ordinal) ||
                string.Equals(property, "windowType", StringComparison.Ordinal))
                return GraphAuthoringFieldValueKind.Enum;
            if (property.EndsWith("Id", StringComparison.Ordinal))
                return GraphAuthoringFieldValueKind.IdentityReference;
            return GraphAuthoringFieldValueKind.Object;
        }

        static GraphAuthoringFieldConstraint SharedFieldConstraint(string property)
        {
            if (string.Equals(property, "loopStopType", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(allowedValues: Enum.GetNames(typeof(LoopNode.StopType)));
            if (string.Equals(property, "compareType", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(allowedValues: Enum.GetNames(typeof(CompareNode.CompareType)));
            if (string.Equals(property, "stateExitCause", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(nonEmpty: true);
            if (string.Equals(property, "windowType", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(nonEmpty: true);
            if (string.Equals(property, "moveSpeed", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(minimum: 0d, finite: true);
            if (string.Equals(property, "turnSpeedDegrees", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(minimum: 0.000001d, finite: true);
            return new GraphAuthoringFieldConstraint(nonEmpty: IsRequiredProperty(property));
        }

        static string SharedPickerKind(string property)
        {
            if (property.EndsWith("Id", StringComparison.Ordinal))
                return ToKebabCase(property.Substring(0, property.Length - 2));
            if (string.Equals(property, "assetReferences", StringComparison.Ordinal))
                return "asset";
            if (string.Equals(property, "graphReferences", StringComparison.Ordinal))
                return "graph";
            return string.Empty;
        }

        static GraphAuthoringNodePresentationKind SharedPresentationKind(Type type)
        {
            if (type == typeof(StateMachineEnterNode))
                return GraphAuthoringNodePresentationKind.StateMachineEntry;
            if (type == typeof(StateNode))
                return GraphAuthoringNodePresentationKind.State;
            return GraphAuthoringNodePresentationKind.Standard;
        }

        static string SharedCategory(Type type, bool systemOwned)
        {
            if (systemOwned)
                return "System";
            if (typeof(ValueNode).IsAssignableFrom(type))
                return "Values";
            if (type == typeof(StateMachineNode) || type == typeof(StateNode))
                return "State Machine";
            if (type.Namespace != null && type.Namespace.IndexOf("AI", StringComparison.OrdinalIgnoreCase) >= 0)
                return "AI";
            return "Gameplay";
        }

        static Color SharedColor(Type type, bool systemOwned)
        {
            var nodeColor =
                Attribute.GetCustomAttribute(
                    type,
                    typeof(NodeColorAttribute),
                    true) as NodeColorAttribute;
            if (nodeColor != null)
                return nodeColor.Color / 255f;
            if (systemOwned)
                return new Color32(74, 82, 96, 255);
            if (typeof(ValueNode).IsAssignableFrom(type))
                return new Color32(76, 98, 132, 255);
            if (type == typeof(StateMachineNode) || type == typeof(StateNode))
                return new Color32(91, 76, 132, 255);
            return new Color32(65, 105, 91, 255);
        }

        static string SplitDisplayName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            string normalized = value.Replace('-', ' ').Replace('_', ' ');
            var result = new List<char>(normalized.Length + 8);
            for (int i = 0; i < normalized.Length; i++)
            {
                char current = normalized[i];
                if (i > 0 && char.IsUpper(current) && normalized[i - 1] != ' ')
                    result.Add(' ');
                result.Add(i == 0 ? char.ToUpperInvariant(current) : current);
            }
            return new string(result.ToArray());
        }

        static string ToKebabCase(string value)
        {
            var result = new List<char>(value?.Length ?? 0);
            for (int i = 0; i < (value?.Length ?? 0); i++)
            {
                char current = value[i];
                if (i > 0 && char.IsUpper(current))
                    result.Add('-');
                result.Add(char.ToLowerInvariant(current));
            }
            return new string(result.ToArray());
        }

        static bool SupportsCreate(string kind)
        {
            switch (kind)
            {
                case "state-machine":
                case "state":
                case "sequence":
                case "selector":
                case "parallel":
                case "loop":
                case "succeed":
                case "timeline":
                case "activate-action-instance":
                case "submit-action-lifecycle":
                case "state-root-completed":
                case "locomotion-input-motion":
                case "character-move-facing-angle":
                case "character-action-request":
                case "character-input-bool":
                case "character-input-float":
                case "character-input-vector2":
                case "character-input-vector2-magnitude":
                case "pipeline-blackboard-bool":
                case "pipeline-blackboard-float":
                case "state-exit-cause":
                case "action-context-active":
                case "action-window-active":
                case "can-activate-action":
                case "and":
                case "or":
                case "not":
                case "compare":
                case "exposed-property":
                case "ai-read-self":
                case "ai-enumerate-candidates":
                case "ai-select-nearest-candidate":
                case "ai-read-target-distance":
                case "ai-read-target-direction":
                case "ai-read-target-snapshot":
                case "ai-read-memory":
                case "ai-write-memory":
                case "ai-write-continuous-input":
                case "ai-write-action-target":
                case "ai-submit-action-request":
                case "ai-wait-ticks":
                    return true;
                default:
                    return false;
            }
        }

        static bool SupportsConfigure(string kind)
        {
            return SupportsCreate(kind);
        }

        static List<string> ResolveGraphKinds(Type type)
        {
            if (type == typeof(LocomotionInputMotionNode))
                return new List<string> { AgentGraphKind.StateBehaviorSubTree.ToString() };
            if (typeof(StateNode).IsAssignableFrom(type) ||
                type == typeof(StateMachineEnterNode) ||
                type == typeof(StateMachineExitNode) ||
                type == typeof(StateMachineAnyStateNode))
                return new List<string> { AgentGraphKind.StateMachineGraph.ToString() };
            if (typeof(ValueNode).IsAssignableFrom(type))
            {
                return new List<string>
                {
                    AgentGraphKind.ConditionRuleGraph.ToString(),
                    AgentGraphKind.StateBehaviorSubTree.ToString(),
                    AgentGraphKind.BaseTree.ToString(),
                    AgentGraphKind.RunnableTree.ToString(),
                    AgentGraphKind.SubTree.ToString()
                };
            }
            return new List<string> { AgentGraphKind.BaseTree.ToString(), AgentGraphKind.RunnableTree.ToString(), AgentGraphKind.SubTree.ToString(), AgentGraphKind.StateBehaviorSubTree.ToString() };
        }

        static string ResolveOwnerSlot(string graphKind)
        {
            if (string.Equals(graphKind, AgentGraphKind.StateMachineGraph.ToString(), StringComparison.Ordinal))
                return "stateMachine";
            if (string.Equals(graphKind, AgentGraphKind.StateBehaviorSubTree.ToString(), StringComparison.Ordinal))
                return "body";
            if (string.Equals(graphKind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal))
                return "condition";
            return "root";
        }

        static bool IsGraphKindKnown(string graphKind)
        {
            return Enum.TryParse(graphKind, false, out AgentGraphKind parsed) && parsed != AgentGraphKind.Unknown;
        }

        public bool IsGraphKindAllowed(string graphKind, string domain)
        {
            if (!IsGraphKindKnown(graphKind))
                return false;
            if (string.Equals(domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal))
                return true;
            if (!string.Equals(domain, AgentAuthoringSchema.AIControllerDomain, StringComparison.Ordinal))
                return false;
            return string.Equals(graphKind, AgentGraphKind.BaseTree.ToString(), StringComparison.Ordinal) ||
                   string.Equals(graphKind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal);
        }

        public bool IsOwnerSlotAllowed(string graphKind, string slot)
        {
            return string.Equals(ResolveOwnerSlot(graphKind), slot, StringComparison.Ordinal);
        }

        public bool IsNodeAllowed(string kind, string graphKind, string domain)
        {
            return TryDescribe(kind, out NodeDescriptor descriptor) &&
                   !descriptor.SystemOwned &&
                   descriptor.GraphKinds.Contains(graphKind) &&
                   IsNodeTypeAllowed(descriptor.Type, domain);
        }

        public bool IsNodeTypeAllowed(Type type, string domain)
        {
            if (!TryDescribe(
                    type,
                    out NodeDescriptor descriptor) ||
                !IsNodeTypeAllowed(descriptor, domain))
                return false;
            return true;
        }

        public bool IsNodeTypeAllowed(string kindOrTypeName, string domain)
        {
            if (!TryDescribe(
                    kindOrTypeName,
                    out NodeDescriptor descriptor))
                return false;
            return IsNodeTypeAllowed(descriptor, domain);
        }

        static bool IsNodeTypeAllowed(NodeDescriptor descriptor, string domain)
        {
            if (descriptor == null)
                return false;
            if (string.Equals(domain, AgentAuthoringSchema.AIControllerDomain, StringComparison.Ordinal))
                return NodeAuthoringCapabilityPolicy.TryGetCapability(descriptor.Type, out NodeAuthoringCapability aiCapability) &&
                       NodeAuthoringCapabilityPolicy.Allows(GraphAuthoringRole.AIController, aiCapability);
            if (string.Equals(domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal))
                return !NodeAuthoringCapabilityPolicy.TryGetCapability(descriptor.Type, out NodeAuthoringCapability characterCapability) ||
                       NodeAuthoringCapabilityPolicy.Allows(GraphAuthoringRole.Character, characterCapability);
            return false;
        }

        public bool IsPortAllowed(
            string kind,
            JObject properties,
            string port,
            string direction,
            bool property)
        {
            if (!TryDescribe(
                    kind,
                    out NodeDescriptor descriptor))
                return false;
            List<AgentPackagePortDescriptor> ports;
            if (property || descriptor.ConfigureFlowPorts == null)
            {
                ports = property ? descriptor.PropertyPorts : descriptor.FlowPorts;
            }
            else
            {
                try
                {
                    BaseNode node = (BaseNode)Activator.CreateInstance(descriptor.Type);
                    if (!descriptor.ConfigureFlowPorts(node, properties))
                        return false;
                    ports = ToFlowPortDescriptors(node.GetFlowPortDeclarations(null));
                }
                catch
                {
                    return false;
                }
            }
            return ports.Any(value =>
                string.Equals(value.key, port, StringComparison.Ordinal) &&
                string.Equals(value.direction, direction, StringComparison.Ordinal));
        }

        public bool IsAnchorPortAllowed(string graphKind, string anchor, string port, string direction, bool property, string domain)
        {
            AgentPackageAnchorDescriptor descriptor = ResolveAnchors(graphKind, domain)
                .FirstOrDefault(value => string.Equals(value.anchor, anchor, StringComparison.Ordinal));
            List<AgentPackagePortDescriptor> ports = property ? descriptor?.propertyPorts : descriptor?.flowPorts;
            return ports?.Any(value =>
                string.Equals(value.key, port, StringComparison.Ordinal) &&
                string.Equals(value.direction, direction, StringComparison.Ordinal)) == true;
        }

        static List<AgentPackageAnchorDescriptor> ResolveAnchors(string graphKind, string domain)
        {
            var anchors = new List<string>();
            if (string.Equals(graphKind, AgentGraphKind.StateMachineGraph.ToString(), StringComparison.Ordinal))
                anchors.AddRange(new[] { "@enter", "@exit", "@any" });
            else if (string.Equals(graphKind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal))
                anchors.Add("@result");
            else if (string.Equals(graphKind, AgentGraphKind.StateBehaviorSubTree.ToString(), StringComparison.Ordinal))
                anchors.AddRange(new[] { "@root", "@onEnter", "@onExit" });
            else if (string.Equals(domain, AgentAuthoringSchema.AIControllerDomain, StringComparison.Ordinal))
                anchors.Add("@root");
            else
                anchors.AddRange(new[] { "@root", "@timelineEnter" });
            var catalog =
                new BtsmtlGraphAuthoringCapabilities();
            return anchors.Select(anchor =>
            {
                if (!catalog.TryDescribe(
                        anchor,
                        out NodeDescriptor node))
                {
                    throw new InvalidOperationException(
                        $"BTSMTL anchor capability '{anchor}' is missing.");
                }
                return new AgentPackageAnchorDescriptor
                {
                    anchor = anchor,
                    flowPorts = node.FlowPorts,
                    propertyPorts = node.PropertyPorts
                };
            }).ToList();
        }

        static void CreatePorts(
            Type type,
            out List<AgentPackagePortDescriptor> flowPorts,
            out List<AgentPackagePortDescriptor> propertyPorts)
        {
            flowPorts = new List<AgentPackagePortDescriptor>();
            propertyPorts = new List<AgentPackagePortDescriptor>();
            try
            {
                BaseNode flowNode = (BaseNode)Activator.CreateInstance(type);
                flowPorts = ToFlowPortDescriptors(flowNode.GetSupportedFlowPortDeclarations(null));
            }
            catch
            {
                flowPorts.Clear();
            }
            try
            {
                BaseNode propertyNode = (BaseNode)Activator.CreateInstance(type);
                propertyNode.BeforeInit();
                propertyPorts = propertyNode.PropertyPortMap.Values
                    .Select(port => new AgentPackagePortDescriptor
                    {
                        key = port.PortId,
                        direction = port.Direction.ToString(),
                        valueType = StableValueType(port.ValueType)
                    })
                    .OrderBy(port => port.key, StringComparer.Ordinal)
                    .ToList();
            }
            catch
            {
                propertyPorts.Clear();
            }
        }

        static List<AgentPackagePortDescriptor> ToFlowPortDescriptors(
            IEnumerable<FlowPortDeclaration> declarations)
        {
            return declarations
                .Select(port => new AgentPackagePortDescriptor
                {
                    key = port.Name,
                    direction = port.Direction.ToString()
                })
                .GroupBy(port => port.key + "\0" + port.direction, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(port => port.key, StringComparer.Ordinal)
                .ToList();
        }

        static bool ConfigureExposedPropertyFlowPorts(BaseNode node, JObject properties)
        {
            if (node is not ExposedPropertyNode exposedProperty ||
                properties?["exposedProperty"]?["mode"]?.Type != JTokenType.String ||
                !Enum.TryParse(
                    properties["exposedProperty"].Value<string>("mode"),
                    false,
                    out ExposedPropertyNodeType mode))
                return false;
            exposedProperty.SetNodeType(mode);
            return true;
        }

        static List<AgentPackagePortDescriptor> MergePorts(
            IEnumerable<AgentPackagePortDescriptor> first,
            IEnumerable<AgentPackagePortDescriptor> second)
        {
            return (first ?? Array.Empty<AgentPackagePortDescriptor>())
                .Concat(second ?? Array.Empty<AgentPackagePortDescriptor>())
                .Where(port => port != null && !string.IsNullOrEmpty(port.key))
                .GroupBy(port => port.key + "\0" + port.direction, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(port => port.key, StringComparer.Ordinal)
                .ToList();
        }

        static string StableValueType(Type type)
        {
            if (type == null) return string.Empty;
            if (type == typeof(bool)) return "bool";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(string)) return "string";
            if (type == typeof(Vector2)) return "vector2";
            if (type == typeof(Vector3)) return "vector3";
            return type.Name;
        }

        static string StableValueType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return string.Empty;
            int separator = typeName.LastIndexOf('.');
            string name = separator >= 0 ? typeName.Substring(separator + 1) : typeName;
            if (name == nameof(Boolean)) return "bool";
            if (name == nameof(Int32)) return "int";
            if (name == nameof(Single)) return "float";
            if (name == nameof(String)) return "string";
            if (name == nameof(Vector2)) return "vector2";
            if (name == nameof(Vector3)) return "vector3";
            return name;
        }

        static JObject Defaults(string kind)
        {
            if (string.Equals(kind, "loop", StringComparison.Ordinal))
                return new JObject { ["loopStopType"] = LoopNode.StopType.None.ToString() };
            if (string.Equals(kind, "compare", StringComparison.Ordinal))
                return new JObject { ["compareType"] = CompareNode.CompareType.Equal.ToString() };
            if (string.Equals(kind, "locomotion-input-motion", StringComparison.Ordinal))
                return new JObject
                {
                    ["moveSpeed"] = 4f,
                    ["turnSpeedDegrees"] = 720f,
                    ["cameraRelative"] = true,
                    ["continuous"] = false
                };
            return null;
        }
    }
}
