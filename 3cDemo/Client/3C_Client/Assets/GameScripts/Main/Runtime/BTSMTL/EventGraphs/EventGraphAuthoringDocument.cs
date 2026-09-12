using System;
using System.Collections.Generic;

namespace BTSMTL.EventGraphs
{
    public sealed class EventGraphAuthoringDocument
    {
        public EventGraphAuthoringDocument(
            string graphId,
            string contentRevision,
            IReadOnlyList<EventGraphAuthoringVariable> variables,
            IReadOnlyList<EventGraphAuthoringNode> nodes,
            IReadOnlyList<EventGraphAuthoringEdge> edges)
        {
            GraphId = string.IsNullOrWhiteSpace(graphId)
                ? throw new ArgumentException("Event graph identity is missing.", nameof(graphId))
                : graphId.Trim();
            ContentRevision = string.IsNullOrWhiteSpace(contentRevision)
                ? throw new ArgumentException("Event graph revision is missing.", nameof(contentRevision))
                : contentRevision.Trim();
            Variables = variables ?? throw new ArgumentNullException(nameof(variables));
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            Edges = edges ?? throw new ArgumentNullException(nameof(edges));
        }

        public string GraphId { get; }
        public string ContentRevision { get; }
        public IReadOnlyList<EventGraphAuthoringVariable> Variables { get; }
        public IReadOnlyList<EventGraphAuthoringNode> Nodes { get; }
        public IReadOnlyList<EventGraphAuthoringEdge> Edges { get; }
    }

    public sealed class EventGraphAuthoringVariable
    {
        public EventGraphAuthoringVariable(
            string id,
            string name,
            Type valueType,
            object defaultValue)
        {
            Id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException("Event graph variable identity is missing.", nameof(id))
                : id.Trim();
            Name = string.IsNullOrWhiteSpace(name) ? Id : name.Trim();
            if (!EventGraphValueKinds.IsSupported(valueType))
                throw new ArgumentException("Event graph variable type is unsupported.", nameof(valueType));
            ValueType = valueType;
            DefaultValue = EventGraphValue.FromObject(defaultValue);
        }

        public string Id { get; }
        public string Name { get; }
        public Type ValueType { get; }
        public EventGraphValue DefaultValue { get; }
    }

    public sealed class EventGraphAuthoringNode
    {
        public string Id { get; set; }
        public Type AuthoringType { get; set; }
        public string Name { get; set; }
        public string InputId { get; set; }
        public string VariableId { get; set; }
        public MacroReference Macro { get; set; }
        public string Operation { get; set; }
        public bool PerSecond { get; set; }
        public int PortCount { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
    }

    public sealed class MacroReference
    {
        public MacroReference(FlowCanvas.Macros.Macro macro)
        {
            Value = macro ?? throw new ArgumentNullException(nameof(macro));
        }

        public FlowCanvas.Macros.Macro Value { get; }
    }

    public sealed class EventGraphAuthoringEdge
    {
        public EventGraphAuthoringEdge(
            string id,
            string sourceNodeId,
            string sourcePortId,
            string targetNodeId,
            string targetPortId)
        {
            Id = Require(id, nameof(id));
            SourceNodeId = Require(sourceNodeId, nameof(sourceNodeId));
            SourcePortId = Require(sourcePortId, nameof(sourcePortId));
            TargetNodeId = Require(targetNodeId, nameof(targetNodeId));
            TargetPortId = Require(targetPortId, nameof(targetPortId));
        }

        public string Id { get; }
        public string SourceNodeId { get; }
        public string SourcePortId { get; }
        public string TargetNodeId { get; }
        public string TargetPortId { get; }

        static string Require(string value, string parameterName) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Event graph edge identity is missing.", parameterName)
                : value.Trim();
    }
}
