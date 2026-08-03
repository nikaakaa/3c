using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace TreeDesigner.Editor
{
    public interface IBtsmtlSharedClipboardCodec :
        IGraphAuthoringClipboardCodec
    {
        string SerializeElements(
            IGraphAuthoringDocumentProjection document,
            IEnumerable<GraphElement> elements);

        bool CanPasteElements(
            IGraphAuthoringDocumentProjection document,
            string payload);

        void PasteElements(
            IGraphAuthoringDocumentProjection document,
            string operationName,
            string payload,
            Vector2 graphPosition);
    }

    public interface IBtsmtlNodeCreationPayload
    {
        Type NodeType { get; }
        void Configure(BaseNode node);
    }

    public static class BtsmtlTransitionMutation
    {
        public static void Apply(
            BaseGraph graph,
            BaseEdge edge,
            GraphAuthoringFieldId fieldId,
            object value)
        {
            if (graph == null || edge == null)
                throw new ArgumentNullException(
                    graph == null ? nameof(graph) : nameof(edge));
            switch (fieldId.Value)
            {
                case "priority":
                {
                    int priority = Convert.ToInt32(value);
                    edge.TransitionPriority = priority >= 0
                        ? priority
                        : throw new ArgumentOutOfRangeException(
                            nameof(value),
                            "BTSMTL transition priority cannot be negative.");
                    return;
                }
                case "interruption":
                    edge.AbortPolicy =
                        ParseEnum<BTAbortPolicy>(
                            value,
                            fieldId);
                    return;
                case "condition-rule-ownership":
                {
                    ConditionRuleGraphOwnership ownership =
                        ParseEnum<ConditionRuleGraphOwnership>(
                            value,
                            fieldId);
                    if (ownership ==
                        ConditionRuleGraphOwnership.Inline)
                    {
                        if (edge.ConditionRuleGraphOwnership !=
                                ConditionRuleGraphOwnership.Inline ||
                            edge.InlineConditionRuleGraph == null)
                        {
                            edge.SetConditionRuleGraph(
                                ConditionRuleGraph
                                    .CreateDefaultGraph(
                                        $"{edge.StartNode.ResolvedDisplayName}_To_{edge.EndNode.ResolvedDisplayName}_Rule",
                                        graph.AuthoringRole));
                        }
                        return;
                    }
                    if (ownership ==
                            ConditionRuleGraphOwnership.Shared &&
                        edge.SharedConditionRuleGraphAsset)
                    {
                        edge.SetConditionRuleGraphAsset(
                            edge.SharedConditionRuleGraphAsset);
                        return;
                    }
                    throw new InvalidOperationException(
                        "Select an exact shared ConditionRuleGraph asset before changing ownership to Shared.");
                }
                case "inline-condition-rule":
                    edge.SetConditionRuleGraph(
                        value as ConditionRuleGraph ??
                        throw new InvalidOperationException(
                            "Inline Condition Rule requires a ConditionRuleGraph."));
                    return;
                case "shared-condition-rule-asset":
                {
                    BaseTreeAsset asset =
                        value as BaseTreeAsset ??
                        throw new InvalidOperationException(
                            "Shared Condition Rule requires a BaseTreeAsset.");
                    if (!(asset.Tree is ConditionRuleGraph))
                    {
                        throw new InvalidOperationException(
                            $"Asset '{asset.name}' does not contain a ConditionRuleGraph.");
                    }
                    edge.SetConditionRuleGraphAsset(asset);
                    return;
                }
                default:
                    throw new InvalidOperationException(
                        $"BTSMTL Transition does not declare writable field '{fieldId}'.");
            }
        }

        static T ParseEnum<T>(
            object value,
            GraphAuthoringFieldId fieldId)
            where T : struct
        {
            if (value is T typed)
                return typed;
            if (Enum.TryParse(
                    value?.ToString(),
                    false,
                    out T parsed))
                return parsed;
            throw new InvalidOperationException(
                $"BTSMTL field '{fieldId}' has invalid value '{value}'.");
        }
    }

    public sealed class BtsmtlNodeGroupMutationPayload
    {
        public BtsmtlNodeGroupMutationPayload(NodeGroup group)
        {
            Group = group ??
                throw new ArgumentNullException(nameof(group));
        }

        public NodeGroup Group { get; }
    }

    public sealed class BtsmtlStackMutationPayload
    {
        public BtsmtlStackMutationPayload(StackNode stack)
        {
            Stack = stack ??
                throw new ArgumentNullException(nameof(stack));
        }

        public StackNode Stack { get; }
    }

    public static class BtsmtlSharedGraphPort
    {
        public const string FlowValueType = "btsmtl.flow";
        public const string PropertyValueType = "btsmtl.property";
        const string FlowPrefix = "flow:";
        const string PropertyPrefix = "property:";

        public static GraphAuthoringPortId Flow(string name) =>
            new GraphAuthoringPortId(
                FlowPrefix + RequireName(name));

        public static GraphAuthoringPortId Property(string name) =>
            new GraphAuthoringPortId(
                PropertyPrefix + RequireName(name));

        public static bool TryParse(
            GraphAuthoringPortId id,
            out bool property,
            out string name)
        {
            property = false;
            name = string.Empty;
            if (!id.IsValid)
                return false;
            if (id.Value.StartsWith(
                    FlowPrefix,
                    StringComparison.Ordinal))
            {
                name = id.Value.Substring(FlowPrefix.Length);
                return !string.IsNullOrEmpty(name);
            }
            if (id.Value.StartsWith(
                    PropertyPrefix,
                    StringComparison.Ordinal))
            {
                property = true;
                name = id.Value.Substring(
                    PropertyPrefix.Length);
                return !string.IsNullOrEmpty(name);
            }
            return false;
        }

        public static GraphAuthoringPortDirection Direction(
            PortDirection direction) =>
            direction == PortDirection.Input
                ? GraphAuthoringPortDirection.Input
                : GraphAuthoringPortDirection.Output;

        public static GraphAuthoringPortCapacity Capacity(
            PortCapacity capacity) =>
            capacity == PortCapacity.Single
                ? GraphAuthoringPortCapacity.Single
                : GraphAuthoringPortCapacity.Multiple;

        static string RequireName(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException(
                    "BTSMTL port name is missing.")
                : value;
    }

    public sealed class BtsmtlSharedAuthoringWorkspaceBinding
    {
        readonly Action<bool> m_SetReadOnly;

        public BtsmtlSharedAuthoringWorkspaceBinding(
            BaseTree graph,
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringCapabilityCatalog capabilities,
            IGraphAuthoringDomainMutation mutation,
            IGraphAuthoringConnectionPolicy connectionPolicy,
            IGraphAuthoringDetailsDataSource details,
            IGraphAuthoringNavigatorDataSource navigator,
            IGraphAuthoringDomainDiagnostics diagnostics,
            IBtsmtlSharedClipboardCodec clipboard = null,
            Action<bool> setReadOnly = null,
            IGraphAuthoringStateMachineProjection stateMachineDocument = null,
            IGraphAuthoringDomainMutation stateMachineMutation = null,
            IGraphAuthoringStateMachinePolicy stateMachinePolicy = null,
            IGraphAuthoringStateMachineDetailsDataSource stateMachineDetails = null)
        {
            Graph = graph ??
                throw new ArgumentNullException(nameof(graph));
            Document = document ??
                       throw new ArgumentNullException(nameof(document));
            Capabilities = capabilities ??
                           throw new ArgumentNullException(
                               nameof(capabilities));
            Mutation = mutation ??
                       throw new ArgumentNullException(nameof(mutation));
            ConnectionPolicy = connectionPolicy ??
                               throw new ArgumentNullException(
                                   nameof(connectionPolicy));
            Details = details ??
                      throw new ArgumentNullException(nameof(details));
            Navigator = navigator ??
                        throw new ArgumentNullException(nameof(navigator));
            Diagnostics = diagnostics ??
                          throw new ArgumentNullException(nameof(diagnostics));
            Clipboard = clipboard;
            m_SetReadOnly = setReadOnly ??
                throw new ArgumentNullException(nameof(setReadOnly));
            StateMachineDocument = stateMachineDocument;
            StateMachineMutation = stateMachineMutation;
            StateMachinePolicy = stateMachinePolicy;
            StateMachineDetails = stateMachineDetails;
            bool stateMachineComplete =
                stateMachineDocument != null &&
                stateMachineMutation != null &&
                stateMachinePolicy != null &&
                stateMachineDetails != null;
            bool stateMachineEmpty =
                stateMachineDocument == null &&
                stateMachineMutation == null &&
                stateMachinePolicy == null &&
                stateMachineDetails == null;
            if (!stateMachineComplete && !stateMachineEmpty)
            {
                throw new ArgumentException(
                    "BTSMTL StateMachine workspace binding is incomplete.");
            }
        }

        public BaseTree Graph { get; }
        public IGraphAuthoringDocumentProjection Document { get; }
        public GraphAuthoringCapabilityCatalog Capabilities { get; }
        public IGraphAuthoringDomainMutation Mutation { get; }
        public IGraphAuthoringConnectionPolicy ConnectionPolicy { get; }
        public IGraphAuthoringDetailsDataSource Details { get; }
        public IGraphAuthoringNavigatorDataSource Navigator { get; }
        public IGraphAuthoringDomainDiagnostics Diagnostics { get; }
        public IBtsmtlSharedClipboardCodec Clipboard { get; }
        public IGraphAuthoringStateMachineProjection StateMachineDocument
        {
            get;
        }
        public IGraphAuthoringDomainMutation StateMachineMutation { get; }
        public IGraphAuthoringStateMachinePolicy StateMachinePolicy { get; }
        public IGraphAuthoringStateMachineDetailsDataSource StateMachineDetails
        {
            get;
        }
        public bool UsesStateMachineSurface =>
            StateMachineDocument != null;

        public void SetReadOnly(bool readOnly)
        {
            m_SetReadOnly(readOnly);
        }
    }

    public interface IBtsmtlSharedAuthoringWorkspaceFactory
    {
        BtsmtlSharedAuthoringWorkspaceBinding Create(
            BaseTreeWindow window,
            BaseTree graph,
            bool readOnly);
    }

    public static class BtsmtlSharedAuthoringWorkspaceRegistry
    {
        static IBtsmtlSharedAuthoringWorkspaceFactory s_Factory;

        public static void Register(
            IBtsmtlSharedAuthoringWorkspaceFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (s_Factory != null &&
                !ReferenceEquals(s_Factory, factory) &&
                s_Factory.GetType() != factory.GetType())
            {
                throw new InvalidOperationException(
                    $"BTSMTL shared workspace factory is already owned by '{s_Factory.GetType().FullName}'.");
            }
            s_Factory = factory;
        }

        public static BtsmtlSharedAuthoringWorkspaceBinding Create(
            BaseTreeWindow window,
            BaseTree graph,
            bool readOnly)
        {
            if (s_Factory == null)
            {
                throw new InvalidOperationException(
                    "BTSMTL shared workspace factory is not registered.");
            }
            return s_Factory.Create(
                window ??
                throw new ArgumentNullException(nameof(window)),
                graph ??
                throw new ArgumentNullException(nameof(graph)),
                readOnly);
        }
    }
}
