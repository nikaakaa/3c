using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonSimulation;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class BtsmtlGraphAuthoringCapabilities
    {
        readonly List<GraphAuthoringCapabilityDescriptor>
            m_RegistrationDescriptors =
            new List<GraphAuthoringCapabilityDescriptor>();
        static bool s_SharedRegistered;

        public static readonly GraphAuthoringDomainId SharedDomain = new GraphAuthoringDomainId("btsmtl");

        public BtsmtlGraphAuthoringCapabilities()
        {
            if (s_SharedRegistered)
                return;
            RegisterSystem<RootNode>(
                "@root",
                BtsmtlGraphAuthoringRoles.BaseTree,
                BtsmtlGraphAuthoringRoles.RunnableTree,
                BtsmtlGraphAuthoringRoles.SubTree,
                BtsmtlGraphAuthoringRoles.StateBehaviorSubTree);
            RegisterSystem<StateMachineEnterNode>(
                "@enter",
                BtsmtlGraphAuthoringRoles.StateMachineGraph);
            RegisterSystem<StateMachineExitNode>(
                "@exit",
                BtsmtlGraphAuthoringRoles.StateMachineGraph);
            RegisterSystem<StateMachineAnyStateNode>(
                "@any",
                BtsmtlGraphAuthoringRoles.StateMachineGraph);
            RegisterSystem<StateOnEnterNode>(
                "@onEnter",
                BtsmtlGraphAuthoringRoles.StateBehaviorSubTree);
            RegisterSystem<StateOnExitNode>(
                "@onExit",
                BtsmtlGraphAuthoringRoles.StateBehaviorSubTree);
            RegisterSystem<TimelineEnterNode>(
                "@timelineEnter",
                BtsmtlGraphAuthoringRoles.BaseTree,
                BtsmtlGraphAuthoringRoles.RunnableTree,
                BtsmtlGraphAuthoringRoles.SubTree,
                BtsmtlGraphAuthoringRoles.StateBehaviorSubTree);
            RegisterSystem<ConditionRuleResultNode>(
                "@result",
                BtsmtlGraphAuthoringRoles.ConditionRuleGraph);

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
            Register<CanActivateActionInfoNode>("can-activate-action", "admissionProfileId", "targetSnapshotBlackboardDeclarationId");
            Register<LocomotionInputMotionNode>(
                "locomotion-input-motion",
                "moveSpeed",
                "displacementMode",
                "turnSpeedDegrees",
                "cameraRelative",
                "executionMode",
                "durationSeconds",
                "assetReferences");
            Register<AndNode>("and");
            Register<OrNode>("or");
            Register<NotNode>("not");
            Register<CompareNode>("compare", "compareType");
            RegisterExposedProperty();
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
                        descriptor) ||
                !IsNodeCapability(descriptor))
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
                descriptor.SystemOwned ||
                !IsNodeCapability(descriptor))
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

        public bool TryResolveInputValueCapability(
            CharacterInputValueType valueType,
            out GraphAuthoringCapabilityDescriptor descriptor)
        {
            return SharedCatalog.TryGetByExternalKind(
                SharedDomain,
                ResolveInputNodeKind(valueType),
                out descriptor);
        }

        static string ResolveInputNodeKind(CharacterInputValueType valueType)
        {
            return valueType switch
            {
                CharacterInputValueType.Bool => "character-input-bool",
                CharacterInputValueType.Float => "character-input-float",
                CharacterInputValueType.Vector2 => "character-input-vector2",
                _ => string.Empty
            };
        }

        public bool TryResolveActionRequestCapability(
            out GraphAuthoringCapabilityDescriptor descriptor)
        {
            return SharedCatalog.TryGetByExternalKind(
                SharedDomain,
                "character-action-request",
                out descriptor);
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

        void RegisterExposedProperty()
        {
            Register(
                "exposed-property",
                typeof(ExposedPropertyNode),
                false,
                null,
                new[] { "exposedProperty" },
                CreateExposedPropertyPortVariants(),
                Array.Empty<GraphAuthoringCommandDescriptor>(),
                false);
        }

        void RegisterSystem<T>(
            string anchor,
            params string[] graphKinds) where T : BaseNode
        {
            Register(
                anchor,
                typeof(T),
                true,
                anchor,
                Array.Empty<string>(),
                null,
                Array.Empty<GraphAuthoringCommandDescriptor>(),
                graphKinds: graphKinds);
        }

        void Register(
            string kind,
            Type type,
            bool systemOwned,
            string anchor,
            string[] properties,
            IReadOnlyList<GraphAuthoringPortVariantDescriptor> portVariants,
            IReadOnlyList<GraphAuthoringCommandDescriptor> commands,
            bool inferFixedPorts = true,
            IReadOnlyList<string> graphKinds = null)
        {
            List<GraphAuthoringPortDescriptor> flowPorts;
            List<GraphAuthoringPortDescriptor> propertyPorts;
            if (inferFixedPorts)
                CreatePorts(type, out flowPorts, out propertyPorts);
            else
            {
                flowPorts = new List<GraphAuthoringPortDescriptor>();
                propertyPorts = new List<GraphAuthoringPortDescriptor>();
            }
            IReadOnlyList<string> allowedGraphKinds =
                graphKinds ?? ResolveGraphKinds(type);
            GraphAuthoringCapabilityDescriptor descriptor =
                CreateDescriptor(
                    kind,
                    type,
                    systemOwned,
                    anchor,
                    properties,
                    flowPorts,
                    propertyPorts,
                    portVariants,
                    commands,
                    allowedGraphKinds);
            if (m_RegistrationDescriptors.Any(value =>
                    string.Equals(
                        value.ExternalKind,
                        kind,
                        StringComparison.Ordinal) ||
                    value.AuthoringType == type))
            {
                throw new InvalidOperationException(
                    $"BTSMTL capability '{kind}' or authoring type '{type.FullName}' is duplicated.");
            }
            m_RegistrationDescriptors.Add(descriptor);
        }

        static void EnsureSharedRegistered(
            IEnumerable<GraphAuthoringCapabilityDescriptor> descriptors)
        {
            if (s_SharedRegistered)
                return;
            GraphAuthoringCapabilityDescriptor[] values = descriptors
                .OrderBy(value => value.ExternalKind, StringComparer.Ordinal)
                .ToArray();
            GraphAuthoringCapabilityRegistrationRoot.RegisterDomain("btsmtl.graph", catalog =>
            {
                for (int i = 0; i < values.Length; i++)
                    catalog.Register(values[i]);
            });
            s_SharedRegistered = true;
        }

        static GraphAuthoringCapabilityDescriptor CreateDescriptor(
            string kind,
            Type type,
            bool systemOwned,
            string anchor,
            IReadOnlyList<string> properties,
            IReadOnlyList<GraphAuthoringPortDescriptor> flowPorts,
            IReadOnlyList<GraphAuthoringPortDescriptor> propertyPorts,
            IReadOnlyList<GraphAuthoringPortVariantDescriptor> portVariants,
            IReadOnlyList<GraphAuthoringCommandDescriptor> commands,
            IReadOnlyList<string> graphKinds)
        {
            GraphAuthoringDocumentRoleId[] roles = graphKinds
                .Select(SharedRoleId)
                .Distinct()
                .ToArray();
            var fields = new List<GraphAuthoringFieldDescriptor>();
            for (int i = 0; i < properties.Count; i++)
            {
                string property = properties[i];
                bool referenceOnly =
                    property.EndsWith("References", StringComparison.Ordinal) &&
                    !(type == typeof(LocomotionInputMotionNode) && string.Equals(property, "assetReferences", StringComparison.Ordinal));
                fields.Add(new GraphAuthoringFieldDescriptor(
                    new GraphAuthoringFieldId(property),
                    SplitDisplayName(property),
                    SharedFieldKind(property),
                    referenceOnly
                        ? GraphAuthoringFieldAccess.ReferenceRead
                        : !systemOwned
                        ? GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite
                        : GraphAuthoringFieldAccess.ReferenceRead,
                    defaultValue: SharedFieldDefault(type, property),
                    constraint: SharedFieldConstraint(property),
                    pickerKind: SharedPickerKind(property)));
            }
            var ports = new List<GraphAuthoringPortDescriptor>();
            AddSharedPorts(
                ports,
                flowPorts,
                "flow");
            AddSharedPorts(
                ports,
                propertyPorts,
                "property");
            return new GraphAuthoringCapabilityDescriptor(
                SharedCapabilityId(kind),
                SharedDomain,
                roles,
                systemOwned ? anchor : SplitDisplayName(kind),
                SharedCategory(type, systemOwned),
                SharedColor(type, systemOwned),
                fields,
                ports,
                GraphAuthoringDynamicPortPolicy.None,
                commands: commands ?? Array.Empty<GraphAuthoringCommandDescriptor>(),
                presentationKind: SharedPresentationKind(type),
                mutationBindingId: systemOwned ? string.Empty : "btsmtl.node",
                validationBindingId: "btsmtl.node",
                compilerBindingId: "btsmtl.node." + kind,
                authoringType: type,
                externalKind: kind,
                systemOwned: systemOwned,
                anchorId: anchor,
                portVariants: portVariants ?? Array.Empty<GraphAuthoringPortVariantDescriptor>());
        }

        static IReadOnlyList<GraphAuthoringCommandDescriptor>
            TimelineCommands() =>
            new[]
            {
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
            IReadOnlyList<GraphAuthoringPortDescriptor> source,
            string family)
        {
            for (int i = 0; i < source.Count; i++)
            {
                GraphAuthoringPortDescriptor port = source[i];
                target.Add(new GraphAuthoringPortDescriptor(
                    new GraphAuthoringPortId(family + ":" + port.PortId.Value),
                    SplitDisplayName(port.PortId.Value),
                    "btsmtl." + family,
                    port.Direction,
                    port.Capacity,
                    port.Required,
                    i));
            }
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

        bool TryDescribe(
            string kindOrTypeName,
            out GraphAuthoringCapabilityDescriptor descriptor)
        {
            descriptor = null;
            return TryResolveDescriptor(kindOrTypeName, out descriptor) &&
                   IsNodeCapability(descriptor);
        }

        bool TryDescribe(
            Type type,
            out GraphAuthoringCapabilityDescriptor descriptor)
        {
            descriptor = null;
            if (type == null ||
                !SharedCatalog.TryGetByAuthoringType(
                        SharedDomain,
                    type,
                    out GraphAuthoringCapabilityDescriptor
                        capability) ||
                !IsNodeCapability(capability))
                return false;
            descriptor = capability;
            return true;
        }

        static bool IsNodeCapability(
            GraphAuthoringCapabilityDescriptor capability) =>
            capability?.AuthoringType != null &&
            !string.IsNullOrWhiteSpace(capability.ExternalKind);

        public static GraphAuthoringDocumentRoleId SharedRoleId(string graphKind) =>
            new GraphAuthoringDocumentRoleId("btsmtl." + ToKebabCase(graphKind));

        public static GraphAuthoringDocumentRoleId SharedRoleId(BaseGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            if (graph is ConditionRuleGraph)
                return SharedRoleId(BtsmtlGraphAuthoringRoles.ConditionRuleGraph);
            if (graph is StateMachineGraph)
                return SharedRoleId(BtsmtlGraphAuthoringRoles.StateMachineGraph);
            if (graph is StateBehaviorSubTree)
                return SharedRoleId(BtsmtlGraphAuthoringRoles.StateBehaviorSubTree);
            if (graph is SubTree)
                return SharedRoleId(BtsmtlGraphAuthoringRoles.SubTree);
            if (graph is RunnableTree)
                return SharedRoleId(BtsmtlGraphAuthoringRoles.RunnableTree);
            if (graph is BaseTree)
                return SharedRoleId(BtsmtlGraphAuthoringRoles.BaseTree);
            throw new InvalidOperationException(
                $"BTSMTL Graph type '{graph.GetType().FullName}' has no registered authoring role.");
        }

        static GraphAuthoringFieldValueKind SharedFieldKind(string property)
        {
            if (property.EndsWith("References", StringComparison.Ordinal))
                return GraphAuthoringFieldValueKind.Object;
            if (string.Equals(property, "moveSpeed", StringComparison.Ordinal) ||
                string.Equals(property, "turnSpeedDegrees", StringComparison.Ordinal) ||
                string.Equals(property, "durationSeconds", StringComparison.Ordinal))
                return GraphAuthoringFieldValueKind.Float;
            if (string.Equals(property, "cameraRelative", StringComparison.Ordinal))
                return GraphAuthoringFieldValueKind.Boolean;
            if (string.Equals(property, "loopStopType", StringComparison.Ordinal) ||
                string.Equals(property, "compareType", StringComparison.Ordinal) ||
                string.Equals(property, "stateExitCause", StringComparison.Ordinal) ||
                string.Equals(property, "windowType", StringComparison.Ordinal) ||
                string.Equals(property, "executionMode", StringComparison.Ordinal))
                return GraphAuthoringFieldValueKind.Enum;
            if (string.Equals(property, "displacementMode", StringComparison.Ordinal))
                return GraphAuthoringFieldValueKind.Enum;
            if (property.EndsWith("Id", StringComparison.Ordinal))
                return GraphAuthoringFieldValueKind.IdentityReference;
            return GraphAuthoringFieldValueKind.Object;
        }

        static object SharedFieldDefault(Type type, string property)
        {
            if (type == typeof(LoopNode) && string.Equals(property, "loopStopType", StringComparison.Ordinal))
                return LoopNode.StopType.None.ToString();
            if (type == typeof(CompareNode) && string.Equals(property, "compareType", StringComparison.Ordinal))
                return CompareNode.CompareType.Equal.ToString();
            if (type != typeof(LocomotionInputMotionNode))
                return null;
            if (string.Equals(property, "moveSpeed", StringComparison.Ordinal))
                return LocomotionInputMotionAuthoringRules.DefaultMoveSpeed;
            if (string.Equals(property, "displacementMode", StringComparison.Ordinal))
                return LocomotionInputMotionAuthoringRules.DefaultDisplacementMode.ToString();
            if (string.Equals(property, "turnSpeedDegrees", StringComparison.Ordinal))
                return LocomotionInputMotionAuthoringRules.DefaultTurnSpeedDegrees;
            if (string.Equals(property, "cameraRelative", StringComparison.Ordinal))
                return LocomotionInputMotionAuthoringRules.DefaultCameraRelative;
            if (string.Equals(property, "executionMode", StringComparison.Ordinal))
                return LocomotionInputMotionAuthoringRules.DefaultExecutionMode.ToString();
            if (string.Equals(property, "durationSeconds", StringComparison.Ordinal))
                return LocomotionInputMotionAuthoringRules.DefaultDurationSeconds;
            return null;
        }

        static GraphAuthoringFieldConstraint SharedFieldConstraint(string property)
        {
            if (string.Equals(property, "loopStopType", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(allowedValues: Enum.GetNames(typeof(LoopNode.StopType)));
            if (string.Equals(property, "compareType", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(allowedValues: Enum.GetNames(typeof(CompareNode.CompareType)));
            if (string.Equals(property, "executionMode", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(allowedValues: Enum.GetNames(typeof(LocomotionInputMotionExecutionMode)));
            if (string.Equals(property, "displacementMode", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(allowedValues: Enum.GetNames(typeof(LocomotionInputMotionDisplacementMode)));
            if (string.Equals(property, "stateExitCause", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(nonEmpty: true);
            if (string.Equals(property, "windowType", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(nonEmpty: true);
            if (string.Equals(property, "moveSpeed", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(minimum: 0d, finite: true);
            if (string.Equals(property, "turnSpeedDegrees", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(minimum: 0.000001d, finite: true);
            if (string.Equals(property, "durationSeconds", StringComparison.Ordinal))
                return new GraphAuthoringFieldConstraint(minimum: 0d, finite: true);
            return new GraphAuthoringFieldConstraint(nonEmpty: IsRequiredProperty(property));
        }

        static bool IsRequiredProperty(string property) =>
            string.Equals(property, "inputId", StringComparison.Ordinal) ||
            string.Equals(property, "requestId", StringComparison.Ordinal) ||
            string.Equals(property, "blackboardDeclarationId", StringComparison.Ordinal) ||
            string.Equals(property, "stateExitCause", StringComparison.Ordinal) ||
            string.Equals(property, "actionContextId", StringComparison.Ordinal) ||
            string.Equals(property, "windowType", StringComparison.Ordinal) ||
            string.Equals(property, "admissionProfileId", StringComparison.Ordinal);

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

        static List<string> ResolveGraphKinds(Type type)
        {
            if (type == typeof(LocomotionInputMotionNode))
                return new List<string> { BtsmtlGraphAuthoringRoles.StateBehaviorSubTree };
            if (typeof(StateNode).IsAssignableFrom(type) ||
                type == typeof(StateMachineEnterNode) ||
                type == typeof(StateMachineExitNode) ||
                type == typeof(StateMachineAnyStateNode))
                return new List<string> { BtsmtlGraphAuthoringRoles.StateMachineGraph };
            if (typeof(ValueNode).IsAssignableFrom(type))
            {
                return new List<string>
                {
                    BtsmtlGraphAuthoringRoles.ConditionRuleGraph,
                    BtsmtlGraphAuthoringRoles.StateBehaviorSubTree,
                    BtsmtlGraphAuthoringRoles.BaseTree,
                    BtsmtlGraphAuthoringRoles.RunnableTree,
                    BtsmtlGraphAuthoringRoles.SubTree
                };
            }
            return new List<string>
            {
                BtsmtlGraphAuthoringRoles.BaseTree,
                BtsmtlGraphAuthoringRoles.RunnableTree,
                BtsmtlGraphAuthoringRoles.SubTree,
                BtsmtlGraphAuthoringRoles.StateBehaviorSubTree
            };
        }

        public string OwnerSlot(string graphKind)
        {
            if (string.Equals(graphKind, BtsmtlGraphAuthoringRoles.StateMachineGraph, StringComparison.Ordinal))
                return "stateMachine";
            if (string.Equals(graphKind, BtsmtlGraphAuthoringRoles.StateBehaviorSubTree, StringComparison.Ordinal))
                return "body";
            if (string.Equals(graphKind, BtsmtlGraphAuthoringRoles.ConditionRuleGraph, StringComparison.Ordinal))
                return "condition";
            return "root";
        }

        static bool IsGraphKindKnown(string graphKind)
        {
            return BtsmtlGraphAuthoringRoles.IsKnown(graphKind);
        }

        public bool IsGraphKindAllowed(string graphKind)
        {
            return IsGraphKindKnown(graphKind);
        }

        public bool IsOwnerSlotAllowed(string graphKind, string slot)
        {
            return string.Equals(OwnerSlot(graphKind), slot, StringComparison.Ordinal);
        }

        public bool IsNodeAllowed(string kind, string graphKind)
        {
            return TryDescribe(kind, out GraphAuthoringCapabilityDescriptor descriptor) &&
                   !descriptor.SystemOwned &&
                   descriptor.Allows(SharedRoleId(graphKind)) &&
                   IsNodeTypeAllowed(descriptor.AuthoringType);
        }

        public bool IsNodeTypeAllowed(Type type)
        {
            if (!TryDescribe(
                    type,
                    out GraphAuthoringCapabilityDescriptor descriptor) ||
                !IsNodeTypeAllowed(descriptor))
                return false;
            return true;
        }

        public bool IsNodeTypeAllowed(string kindOrTypeName)
        {
            if (!TryDescribe(
                    kindOrTypeName,
                    out GraphAuthoringCapabilityDescriptor descriptor))
                return false;
            return IsNodeTypeAllowed(descriptor);
        }

        static bool IsNodeTypeAllowed(
            GraphAuthoringCapabilityDescriptor descriptor)
        {
            if (descriptor == null)
                return false;
            return !NodeAuthoringCapabilityPolicy.TryGetCapability(descriptor.AuthoringType, out NodeAuthoringCapability characterCapability) ||
                   NodeAuthoringCapabilityPolicy.Allows(GraphAuthoringRole.Character, characterCapability);
        }


        public IReadOnlyList<GraphAuthoringDynamicPortProjection> ProjectPortShape(
            BaseNode node,
            BaseGraph owner,
            GraphAuthoringCapabilityDescriptor capability)
        {
            return GraphAuthoringNodePortShapeProjector.Project(
                capability,
                ReadNodeTypedProperties(node, capability),
                ProjectAuthoredDynamicPorts(node, owner, capability));
        }

        public IReadOnlyList<GraphAuthoringDynamicPortProjection>
            ProjectCompletePortShape(
                BaseNode node,
                BaseGraph owner,
                GraphAuthoringCapabilityDescriptor capability)
        {
            return GraphAuthoringNodePortShapeProjector.ProjectComplete(
                capability,
                ReadNodeTypedProperties(node, capability),
                ProjectAuthoredDynamicPorts(node, owner, capability));
        }

        public bool IsAnchorPortAllowed(string graphKind, string anchor, string port, string direction, bool property, string domain)
        {
            if (!TryDescribe(anchor, out GraphAuthoringCapabilityDescriptor descriptor) ||
                !descriptor.SystemOwned ||
                !descriptor.Allows(SharedRoleId(graphKind)))
                return false;
            IReadOnlyList<GraphAuthoringPortDescriptor> ports =
                ToNodePortDescriptors(descriptor.FixedPorts, property);
            return ports.Any(value =>
                string.Equals(value.PortId.Value, port, StringComparison.Ordinal) &&
                string.Equals(value.Direction.ToString(), direction, StringComparison.OrdinalIgnoreCase));
        }

        static IReadOnlyList<GraphAuthoringTypedPropertyValue>
            ReadNodeTypedProperties(
                BaseNode node,
                GraphAuthoringCapabilityDescriptor capability)
        {
            var result = new List<GraphAuthoringTypedPropertyValue>();
            foreach (GraphAuthoringPortVariantCondition condition in
                     capability.PortVariants
                         .Select(value => value.When)
                         .GroupBy(value => value.FieldId)
                         .Select(value => value.First()))
            {
                if (node is ExposedPropertyNode exposedProperty &&
                    string.Equals(
                        condition.FieldId.Value,
                        "exposedProperty.mode",
                        StringComparison.Ordinal))
                {
                    result.Add(new GraphAuthoringTypedPropertyValue(
                        condition.FieldId,
                        condition.ValueKind,
                        exposedProperty.NodeType.ToString()));
                    continue;
                }
                throw new GraphAuthoringPortShapeException(
                    "port_shape_discriminator_unknown",
                    $"BTSMTL node '{node?.GUID}' cannot project discriminator '{condition.FieldId}'.");
            }
            return result;
        }

        static IReadOnlyList<GraphAuthoringDynamicPortProjection>
            ProjectAuthoredDynamicPorts(
                BaseNode node,
                BaseGraph owner,
                GraphAuthoringCapabilityDescriptor capability)
        {
            var declared = new HashSet<GraphAuthoringPortId>(
                capability.FixedPorts.Select(value => value.PortId)
                    .Concat(capability.PortVariants.SelectMany(value => value.Ports).Select(value => value.PortId)));
            var result = new List<GraphAuthoringDynamicPortProjection>();
            int order = 1000;
            foreach (FlowPortDeclaration port in node.GetFlowPortDeclarations(owner))
            {
                GraphAuthoringPortId id = BtsmtlSharedGraphPort.Flow(port.Name);
                if (declared.Contains(id))
                    continue;
                result.Add(new GraphAuthoringDynamicPortProjection(
                    id,
                    port.Name,
                    BtsmtlSharedGraphPort.FlowValueType,
                    BtsmtlSharedGraphPort.Direction(port.Direction),
                    BtsmtlSharedGraphPort.Capacity(port.Capacity),
                    port.Direction == PortDirection.Input,
                    order++));
            }
            foreach (PropertyPort port in node.PropertyPortMap.Values
                         .Where(value => value != null)
                         .OrderBy(value => value.Index)
                         .ThenBy(value => value.PortId, StringComparer.Ordinal))
            {
                GraphAuthoringPortId id = BtsmtlSharedGraphPort.Property(port.PortId);
                if (declared.Contains(id))
                    continue;
                result.Add(new GraphAuthoringDynamicPortProjection(
                    id,
                    port.DisplayName,
                    BtsmtlSharedGraphPort.PropertyValueType,
                    BtsmtlSharedGraphPort.Direction(port.Direction),
                    port.Direction == PortDirection.Input
                        ? GraphAuthoringPortCapacity.Single
                        : GraphAuthoringPortCapacity.Multiple,
                    port.Direction == PortDirection.Input,
                    order++));
            }
            return result;
        }

        static void CreatePorts(
            Type type,
            out List<GraphAuthoringPortDescriptor> flowPorts,
            out List<GraphAuthoringPortDescriptor> propertyPorts)
        {
            flowPorts = new List<GraphAuthoringPortDescriptor>();
            propertyPorts = new List<GraphAuthoringPortDescriptor>();
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
                    .Select((port, index) => new GraphAuthoringPortDescriptor(
                        new GraphAuthoringPortId(port.PortId),
                        port.DisplayName,
                        StableValueType(port.ValueType),
                        port.Direction == PortDirection.Input
                            ? GraphAuthoringPortDirection.Input
                            : GraphAuthoringPortDirection.Output,
                        port.Direction == PortDirection.Input
                            ? GraphAuthoringPortCapacity.Single
                            : GraphAuthoringPortCapacity.Multiple,
                        port.Direction == PortDirection.Input,
                        index))
                    .OrderBy(port => port.PortId.Value, StringComparer.Ordinal)
                    .ToList();
            }
            catch
            {
                propertyPorts.Clear();
            }
        }

        static List<GraphAuthoringPortDescriptor> ToFlowPortDescriptors(
            IEnumerable<FlowPortDeclaration> declarations)
        {
            return declarations
                .Select((port, index) => new GraphAuthoringPortDescriptor(
                    new GraphAuthoringPortId(port.Name),
                    port.Name,
                    BtsmtlSharedGraphPort.FlowValueType,
                    port.Direction == PortDirection.Input
                        ? GraphAuthoringPortDirection.Input
                        : GraphAuthoringPortDirection.Output,
                    BtsmtlSharedGraphPort.Capacity(port.Capacity),
                    port.Direction == PortDirection.Input,
                    index))
                .GroupBy(port => port.PortId.Value + "\0" + port.Direction, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(port => port.PortId.Value, StringComparer.Ordinal)
                .ToList();
        }

        static IReadOnlyList<GraphAuthoringPortVariantDescriptor>
            CreateExposedPropertyPortVariants()
        {
            var discriminator = new GraphAuthoringFieldId("exposedProperty.mode");
            return new[]
            {
                new GraphAuthoringPortVariantDescriptor(
                    ExposedPropertyNodeType.Get.ToString(),
                    new GraphAuthoringPortVariantCondition(
                        discriminator,
                        GraphAuthoringFieldValueKind.Enum,
                        ExposedPropertyNodeType.Get.ToString()),
                    new[]
                    {
                        new GraphAuthoringPortDescriptor(
                            BtsmtlSharedGraphPort.Property("m_Value"),
                            "Value",
                            BtsmtlSharedGraphPort.PropertyValueType,
                            GraphAuthoringPortDirection.Output,
                            GraphAuthoringPortCapacity.Multiple,
                            false,
                            100)
                    }),
                new GraphAuthoringPortVariantDescriptor(
                    ExposedPropertyNodeType.Set.ToString(),
                    new GraphAuthoringPortVariantCondition(
                        discriminator,
                        GraphAuthoringFieldValueKind.Enum,
                        ExposedPropertyNodeType.Set.ToString()),
                    new[]
                    {
                        new GraphAuthoringPortDescriptor(
                            BtsmtlSharedGraphPort.Flow(ExposedPropertyNode.FlowInputPortName),
                            ExposedPropertyNode.FlowInputPortName,
                            BtsmtlSharedGraphPort.FlowValueType,
                            GraphAuthoringPortDirection.Input,
                            GraphAuthoringPortCapacity.Single,
                            true,
                            100),
                        new GraphAuthoringPortDescriptor(
                            BtsmtlSharedGraphPort.Property("m_Value"),
                            "Value",
                            BtsmtlSharedGraphPort.PropertyValueType,
                            GraphAuthoringPortDirection.Input,
                            GraphAuthoringPortCapacity.Single,
                            true,
                            101)
                    })
            };
        }

        static string StableValueType(Type type)
        {
            if (type == null) return "object";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(string)) return "string";
            if (type == typeof(Vector2)) return "vector2";
            if (type == typeof(Vector3)) return "vector3";
            return "object";
        }

        static string StableValueType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return "object";
            int separator = typeName.LastIndexOf('.');
            string name = separator >= 0 ? typeName.Substring(separator + 1) : typeName;
            if (name == nameof(Boolean)) return "bool";
            if (name == nameof(Int32)) return "int";
            if (name == nameof(Single)) return "float";
            if (name == nameof(String)) return "string";
            if (name == nameof(Vector2)) return "vector2";
            if (name == nameof(Vector3)) return "vector3";
            return "object";
        }

        static List<GraphAuthoringPortDescriptor> ToNodePortDescriptors(
            IEnumerable<GraphAuthoringPortDescriptor> ports,
            bool property)
        {
            var result = new List<GraphAuthoringPortDescriptor>();
            foreach (GraphAuthoringPortDescriptor descriptor in
                     ports ?? Array.Empty<GraphAuthoringPortDescriptor>())
            {
                if (!BtsmtlSharedGraphPort.TryParse(
                        descriptor.PortId,
                        out bool isProperty,
                        out string name) ||
                    isProperty != property)
                    continue;
                result.Add(new GraphAuthoringPortDescriptor(
                    new GraphAuthoringPortId(name),
                    descriptor.DisplayName,
                    descriptor.ValueTypeId,
                    descriptor.Direction,
                    descriptor.Capacity,
                    descriptor.Required,
                    result.Count));
            }
            return result;
        }

    }
}
