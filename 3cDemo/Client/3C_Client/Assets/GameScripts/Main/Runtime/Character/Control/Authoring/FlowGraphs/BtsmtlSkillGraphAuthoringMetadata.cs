#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using FlowCanvas.Macros;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonSimulation;
using TreeDesigner.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillGraphAuthoringMetadata
    {
        public static readonly GraphAuthoringDomainId Domain =
            new GraphAuthoringDomainId("btsmtl-skill");

        static readonly IReadOnlyDictionary<BtsmtlSkillFlowGraphRole, GraphAuthoringDocumentRoleId>
            s_Roles = Enum.GetValues(typeof(BtsmtlSkillFlowGraphRole))
                .Cast<BtsmtlSkillFlowGraphRole>()
                .ToDictionary(
                    value => value,
                    value => new GraphAuthoringDocumentRoleId(value.ToString()));

        static bool s_Registered;

        public static GraphAuthoringCapabilityCatalog Catalog
        {
            get
            {
                EnsureRegistered();
                return GraphAuthoringCapabilityRegistrationRoot.Catalog;
            }
        }

        public static GraphAuthoringDocumentRoleId Role(
            BtsmtlSkillFlowGraphRole role)
        {
            return s_Roles[role];
        }

        public static IReadOnlyList<string> RequiredAnchors(
            BtsmtlSkillFlowGraphRole role)
        {
            return role switch
            {
                BtsmtlSkillFlowGraphRole.Skill => new[] { "@root" },
                BtsmtlSkillFlowGraphRole.Subgraph => new[] { "@input", "@output" },
                BtsmtlSkillFlowGraphRole.StateMachine => new[] { "@enter", "@any", "@exit" },
                BtsmtlSkillFlowGraphRole.ConditionRule => new[] { "@result" },
                BtsmtlSkillFlowGraphRole.StateBody => new[] { "@onEnter", "@root", "@onExit" },
                BtsmtlSkillFlowGraphRole.TimelineBody => new[] { "@timelineEnable", "@root", "@timelineDisable", "@timelineDestroy" },
                _ => Array.Empty<string>()
            };
        }

        public static bool IsAnchorAllowed(
            string kind,
            BtsmtlSkillFlowGraphRole role) =>
            RequiredAnchors(role).Contains(kind, StringComparer.Ordinal);

        public static GraphAuthoringCapabilityDescriptor Require(string kind)
        {
            EnsureRegistered();
            return Catalog.Require(
                new GraphAuthoringCapabilityId("btsmtl.skill." + kind));
        }

        public static bool TryGetKind(Type type, out string kind)
        {
            return BtsmtlSkillCapabilityCatalog.TryGetKind(type, out kind);
        }

        public static bool TryGetKind(FlowNode node, out string kind)
        {
            return BtsmtlSkillCapabilityCatalog.TryGetKind(node, out kind);
        }

        public static bool TryResolveType(string kind, out Type type)
        {
            return BtsmtlSkillCapabilityCatalog.TryResolveType(kind, out type);
        }

        public static bool TryResolveType(
            string kind,
            string fieldId,
            string value,
            out Type type)
        {
            return BtsmtlSkillCapabilityCatalog.TryResolveType(
                kind,
                fieldId,
                value,
                out type);
        }

        public static bool IsAnchor(string kind) =>
            BtsmtlSkillCapabilityCatalog.IsAnchor(kind);

        public static bool HasOrderedStepPorts(string kind) =>
            BtsmtlSkillCapabilityCatalog.TryResolveType(kind, out Type type) &&
            DynamicPortSource(type) == GraphAuthoringDynamicPortSource.OrderedSteps;

        public static IReadOnlyList<BtsmtlSkillGraphReferenceAttribute> GraphReferences(string kind)
        {
            if (BtsmtlSkillCapabilityCatalog.TryResolveType(
                    kind,
                    out Type type) &&
                type == typeof(MacroNodeWrapper))
                return new[]
                {
                    new BtsmtlSkillGraphReferenceAttribute(
                        "graphId",
                        BtsmtlSkillFlowGraphRole.Subgraph)
                };
            return type != null
                ? BtsmtlSkillCapabilityCatalog.GraphReferences(type)
                : Array.Empty<BtsmtlSkillGraphReferenceAttribute>();
        }

        public static string ValueType(Type type)
        {
            if (type == typeof(Flow))
                return "flow";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(int))
                return "int";
            if (type == typeof(float))
                return "float";
            if (type == typeof(string))
                return "string";
            if (type == typeof(uint))
                return "uint";
            if (type == typeof(ulong))
                return "ulong";
            if (type == typeof(Vector2))
                return "vector2";
            if (type == typeof(Vector3))
                return "vector3";
            if (type == typeof(ActionTargetSnapshot))
                return "action-target-snapshot";
            return string.Empty;
        }

        public static bool TryResolveValueType(string value, out Type type)
        {
            type = value?.Trim().ToLowerInvariant() switch
            {
                "flow" => typeof(Flow),
                "bool" => typeof(bool),
                "int" => typeof(int),
                "float" => typeof(float),
                "string" => typeof(string),
                "uint" => typeof(uint),
                "ulong" => typeof(ulong),
                "vector2" => typeof(Vector2),
                "vector3" => typeof(Vector3),
                "action-target-snapshot" => typeof(ActionTargetSnapshot),
                _ => null
            };
            return type != null;
        }

        public static IReadOnlyList<GraphAuthoringDynamicPortProjection>
            ProjectBlackboardPorts(string valueType, string accessMode)
        {
            if (!TryResolveValueType(valueType, out Type type))
                return Array.Empty<GraphAuthoringDynamicPortProjection>();
            string canonicalValueType = ValueType(type);
            if (accessMode == "set")
                return new[]
                {
                    new GraphAuthoringDynamicPortProjection(
                        new GraphAuthoringPortId("Input"),
                        "Input",
                        "flow",
                        GraphAuthoringPortDirection.Input,
                        GraphAuthoringPortCapacity.Single,
                        true,
                        0),
                    new GraphAuthoringDynamicPortProjection(
                        new GraphAuthoringPortId("m_Value"),
                        "Input",
                        canonicalValueType,
                        GraphAuthoringPortDirection.Input,
                        GraphAuthoringPortCapacity.Single,
                        true,
                        1)
                };
            if (accessMode == "get")
                return new[]
                {
                    new GraphAuthoringDynamicPortProjection(
                        new GraphAuthoringPortId("m_Output"),
                        "Output",
                        canonicalValueType,
                        GraphAuthoringPortDirection.Output,
                        GraphAuthoringPortCapacity.Multiple,
                        false,
                        0)
                };
            return Array.Empty<GraphAuthoringDynamicPortProjection>();
        }

        public static GraphAuthoringDynamicPortProjection ProjectCompositeStepPort(
            string kind,
            string id,
            string name,
            int order)
        {
            if (!BtsmtlSkillCapabilityCatalog.TryResolveType(
                    kind,
                    out Type type) ||
                !typeof(BtsmtlSkillCompositeFlowNode).IsAssignableFrom(type))
            {
                throw new InvalidOperationException(
                    $"Skill node kind '{kind}' does not declare ordered step ports.");
            }
            return new GraphAuthoringDynamicPortProjection(
                new GraphAuthoringPortId(id),
                name ?? id,
                "flow",
                GraphAuthoringPortDirection.Output,
                GraphAuthoringPortCapacity.Single,
                false,
                order);
        }

        public static GraphAuthoringDynamicPortProjection ProjectAnchorStepPort(
            string kind,
            string id,
            string name,
            int order)
        {
            return ProjectCompositeStepPort(kind, id, name, order);
        }

        public static GraphAuthoringDynamicPortProjection ProjectMacroParameterPort(
            string id,
            string name,
            string valueType,
            bool input,
            int order)
        {
            string canonicalValueType = ResolveCanonicalValueType(valueType);
            return new GraphAuthoringDynamicPortProjection(
                new GraphAuthoringPortId(id),
                name ?? id,
                canonicalValueType,
                input
                    ? GraphAuthoringPortDirection.Input
                    : GraphAuthoringPortDirection.Output,
                input
                    ? GraphAuthoringPortCapacity.Single
                    : GraphAuthoringPortCapacity.Multiple,
                false,
                order);
        }

        public static GraphAuthoringDynamicPortProjection ProjectMacroInterfacePort(
            string kind,
            string id,
            string name,
            string valueType,
            int order)
        {
            if (!BtsmtlSkillCapabilityCatalog.TryResolveType(
                    kind,
                    out Type type))
                throw new InvalidOperationException(
                    $"Skill anchor kind '{kind}' does not declare macro interface ports.");
            GraphAuthoringDynamicPortSource source = DynamicPortSource(type);
            if (source != GraphAuthoringDynamicPortSource.MacroInputs &&
                source != GraphAuthoringDynamicPortSource.MacroOutputs)
                throw new InvalidOperationException(
                    $"Skill anchor kind '{kind}' does not declare macro interface ports.");
            string canonicalValueType = ResolveCanonicalValueType(valueType);
            bool input = source == GraphAuthoringDynamicPortSource.MacroOutputs;
            bool flow = canonicalValueType == "flow";
            return new GraphAuthoringDynamicPortProjection(
                new GraphAuthoringPortId(id),
                name ?? id,
                canonicalValueType,
                input
                    ? GraphAuthoringPortDirection.Input
                    : GraphAuthoringPortDirection.Output,
                input == flow
                    ? GraphAuthoringPortCapacity.Multiple
                    : GraphAuthoringPortCapacity.Single,
                false,
                order);
        }

        static string ResolveCanonicalValueType(string value)
        {
            if (!TryResolveValueType(value, out Type type))
                throw new InvalidOperationException(
                    $"Skill value type '{value}'无法解析。");
            return ValueType(type);
        }

        public static IReadOnlyList<GraphAuthoringDynamicPortProjection>
            ProjectPorts(
                string kind,
                IReadOnlyList<GraphAuthoringDynamicPortProjection> dynamicPorts = null)
        {
            GraphAuthoringCapabilityDescriptor capability = Require(kind);
            return GraphAuthoringNodePortShapeProjector.ProjectComplete(
                capability,
                Array.Empty<GraphAuthoringTypedPropertyValue>(),
                dynamicPorts);
        }

        public static IReadOnlyList<GraphAuthoringDynamicPortProjection>
            ProjectPorts(
                string kind,
                IReadOnlyList<GraphAuthoringTypedPropertyValue> properties,
                IReadOnlyList<GraphAuthoringDynamicPortProjection> dynamicPorts = null)
        {
            return GraphAuthoringNodePortShapeProjector.ProjectComplete(
                Require(kind),
                properties ?? Array.Empty<GraphAuthoringTypedPropertyValue>(),
                dynamicPorts);
        }

        public static bool IsAllowed(
            string kind,
            BtsmtlSkillFlowGraphRole role)
        {
            if (!TryResolveType(kind, out Type type))
                return false;
            return BtsmtlSkillFlowGraphRules.Allows(
                type,
                role,
                role == BtsmtlSkillFlowGraphRole.Subgraph);
        }

        public static IReadOnlyList<GraphAuthoringCapabilityDescriptor>
            GetDomainCapabilities()
        {
            EnsureRegistered();
            return Catalog.GetDomain(Domain);
        }

        public static void EnsureRegistered()
        {
            if (s_Registered)
                return;
            GraphAuthoringCapabilityRegistrationRoot.RegisterDomain(
                "btsmtl.skill",
                catalog =>
                {
                    var kinds = new HashSet<string>(StringComparer.Ordinal);
                    foreach (Type type in BtsmtlSkillCapabilityCatalog.All)
                    {
                        if (!BtsmtlSkillCapabilityCatalog.TryGetKind(
                                type,
                                out string kind) ||
                            !kinds.Add(kind))
                            continue;
                        catalog.Register(CreateDescriptor(type, kind));
                    }
                });
            s_Registered = true;
        }

        static GraphAuthoringCapabilityDescriptor CreateDescriptor(
            Type type,
            string kind)
        {
            bool systemOwned = BtsmtlSkillCapabilityCatalog.IsAnchor(type);
            List<GraphAuthoringDocumentRoleId> roles = Enum.GetValues(
                    typeof(BtsmtlSkillFlowGraphRole))
                .Cast<BtsmtlSkillFlowGraphRole>()
                .Where(role => BtsmtlSkillFlowGraphRules.Allows(
                    type,
                    role,
                    role == BtsmtlSkillFlowGraphRole.Subgraph))
                .Select(Role)
                .Distinct()
                .ToList();
            if (roles.Count == 0)
                roles.Add(Role(BtsmtlSkillFlowGraphRole.Skill));

            IReadOnlyList<GraphAuthoringFieldDescriptor> fields =
                BtsmtlSkillCapabilityCatalog.Fields(type);
            GraphAuthoringDynamicPortSource dynamicPortSource =
                DynamicPortSource(type);
            var ports = dynamicPortSource == GraphAuthoringDynamicPortSource.MacroInputs ||
                        dynamicPortSource == GraphAuthoringDynamicPortSource.MacroOutputs ||
                        dynamicPortSource == GraphAuthoringDynamicPortSource.BlackboardValue ||
                        type == typeof(MacroNodeWrapper)
                ? Array.Empty<GraphAuthoringPortDescriptor>()
                : CreateFixedPorts(type);
            return new GraphAuthoringCapabilityDescriptor(
                new GraphAuthoringCapabilityId("btsmtl.skill." + kind),
                Domain,
                roles,
                kind,
                "BTSMTL Skill",
                Color.white,
                fields,
                ports,
                DynamicPortPolicy(type),
                mutationBindingId: systemOwned ? string.Empty : "btsmtl.skill.node",
                validationBindingId: "btsmtl.skill.node",
                compilerBindingId: "btsmtl.skill." + kind,
                documentCodecId: "btsmtl.skill-node",
                authoringType: type,
                externalKind: kind,
                systemOwned: systemOwned,
                anchorId: systemOwned ? kind : string.Empty,
                executionDomainId: "btsmtl-skill",
                portVariants: PortVariants(type),
                dynamicPortSource: dynamicPortSource);
        }

        static IReadOnlyList<GraphAuthoringPortDescriptor> CreateFixedPorts(
            Type type)
        {
            GraphAuthoringDynamicPortSource source = DynamicPortSource(type);
            if (type == typeof(MacroNodeWrapper) ||
                source == GraphAuthoringDynamicPortSource.MacroInputs ||
                source == GraphAuthoringDynamicPortSource.MacroOutputs ||
                source == GraphAuthoringDynamicPortSource.BlackboardValue)
                return Array.Empty<GraphAuthoringPortDescriptor>();
            FlowNode node = (FlowNode)Activator.CreateInstance(type);
            if (node is BtsmtlSkillCompositeFlowNode composite)
                composite.SetSteps(Array.Empty<BtsmtlSkillStepPort>());
            node.GatherPorts();
            var result = new List<GraphAuthoringPortDescriptor>();
            foreach (BtsmtlSkillPortShape shape in
                     BtsmtlSkillCapabilityCatalog.ProjectPorts(node))
            {
                result.Add(new GraphAuthoringPortDescriptor(
                    new GraphAuthoringPortId(shape.Id),
                    shape.Id,
                    ValueType(shape.ValueType),
                    shape.IsInput
                        ? GraphAuthoringPortDirection.Input
                        : GraphAuthoringPortDirection.Output,
                    shape.Multiple
                        ? GraphAuthoringPortCapacity.Multiple
                        : GraphAuthoringPortCapacity.Single,
                    shape.Required,
                    result.Count));
            }
            return result;
        }

        static GraphAuthoringDynamicPortPolicy DynamicPortPolicy(Type type)
        {
            return DynamicPortSource(type) switch
            {
                GraphAuthoringDynamicPortSource.OrderedSteps =>
                    GraphAuthoringDynamicPortPolicy.OrderedOutputs,
                GraphAuthoringDynamicPortSource.MacroInputs =>
                    GraphAuthoringDynamicPortPolicy.OrderedOutputs,
                GraphAuthoringDynamicPortSource.MacroOutputs =>
                    GraphAuthoringDynamicPortPolicy.OrderedInputs,
                GraphAuthoringDynamicPortSource.MacroParameters =>
                    GraphAuthoringDynamicPortPolicy.OrderedBidirectional,
                _ => GraphAuthoringDynamicPortPolicy.None
            };
        }

        static GraphAuthoringDynamicPortSource DynamicPortSource(Type type)
        {
            return type == typeof(MacroNodeWrapper)
                ? GraphAuthoringDynamicPortSource.MacroParameters
                : typeof(IBtsmtlSkillBlackboardAccessNode).IsAssignableFrom(type) &&
                  type.GetCustomAttributes(
                      typeof(BtsmtlSkillNodeVariantAttribute),
                      true).Length > 0
                    ? GraphAuthoringDynamicPortSource.BlackboardValue
                : typeof(MacroInputNode).IsAssignableFrom(type)
                    ? GraphAuthoringDynamicPortSource.MacroInputs
                    : typeof(MacroOutputNode).IsAssignableFrom(type)
                        ? GraphAuthoringDynamicPortSource.MacroOutputs
                        : typeof(BtsmtlSkillCompositeFlowNode).IsAssignableFrom(type)
                            ? GraphAuthoringDynamicPortSource.OrderedSteps
                            : GraphAuthoringDynamicPortSource.None;
        }

        static IReadOnlyList<GraphAuthoringPortVariantDescriptor>
            PortVariants(Type type)
        {
            if (!typeof(IBtsmtlSkillBlackboardAccessNode).IsAssignableFrom(type) ||
                type.GetCustomAttributes(
                    typeof(BtsmtlSkillNodeVariantAttribute),
                    true).Length == 0)
                return Array.Empty<GraphAuthoringPortVariantDescriptor>();
            var discriminator = new GraphAuthoringFieldId("accessMode");
            return new[]
            {
                new GraphAuthoringPortVariantDescriptor(
                    "get",
                    new GraphAuthoringPortVariantCondition(
                        discriminator,
                        GraphAuthoringFieldValueKind.Enum,
                        "get"),
                    new[]
                    {
                        new GraphAuthoringPortDescriptor(
                            new GraphAuthoringPortId("m_Output"),
                            "m_Output",
                            "object",
                            GraphAuthoringPortDirection.Output,
                            GraphAuthoringPortCapacity.Multiple,
                            false,
                            0)
                    }),
                new GraphAuthoringPortVariantDescriptor(
                    "set",
                    new GraphAuthoringPortVariantCondition(
                        discriminator,
                        GraphAuthoringFieldValueKind.Enum,
                        "set"),
                    new[]
                    {
                        new GraphAuthoringPortDescriptor(
                            new GraphAuthoringPortId("Input"),
                            "Input",
                            "flow",
                            GraphAuthoringPortDirection.Input,
                            GraphAuthoringPortCapacity.Single,
                            true,
                            0),
                        new GraphAuthoringPortDescriptor(
                            new GraphAuthoringPortId("m_Value"),
                            "m_Value",
                            "object",
                            GraphAuthoringPortDirection.Input,
                            GraphAuthoringPortCapacity.Single,
                            true,
                            1)
                    })
            };
        }
    }
}
#endif
