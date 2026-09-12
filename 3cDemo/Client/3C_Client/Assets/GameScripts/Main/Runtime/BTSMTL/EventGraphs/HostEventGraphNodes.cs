using FlowCanvas;
using ParadoxNotion.Design;
using UnityEngine;

namespace BTSMTL.EventGraphs
{
    public abstract class EventGraphHostInputNode<T> : FlowScriptNode, EventGraphHostInputNodeMarker
    {
        [SerializeField] string m_InputId = string.Empty;

        public string InputId => m_InputId ?? string.Empty;

        protected override void RegisterPorts()
        {
            AddValueOutput<T>("Value", ReadValue);
        }

        public void ConfigureInput(string inputId)
        {
            if (string.IsNullOrWhiteSpace(inputId))
                throw new System.ArgumentException("Event graph input identity is missing.", nameof(inputId));
            m_InputId = inputId.Trim();
        }

        T ReadValue()
        {
            if (graph is not HostEventGraph host)
                throw new System.InvalidOperationException("Event graph host input node is outside a HostEventGraph.");
            return host.ReadInput<T>(m_InputId);
        }
    }

    [Name("Host Float Input")]
    [Category("Host/Input")]
    [Description("Reads a declared host float input.")]
    public sealed class EventGraphFloatInputNode : EventGraphHostInputNode<float>
    {
    }

    [Name("Host Int Input")]
    [Category("Host/Input")]
    [Description("Reads a declared host integer input.")]
    public sealed class EventGraphIntInputNode : EventGraphHostInputNode<int>
    {
    }

    [Name("Host Bool Input")]
    [Category("Host/Input")]
    [Description("Reads a declared host boolean input.")]
    public sealed class EventGraphBoolInputNode : EventGraphHostInputNode<bool>
    {
    }

    [Name("Host Vector2 Input")]
    [Category("Host/Input")]
    [Description("Reads a declared host Vector2 input.")]
    public sealed class EventGraphVector2InputNode : EventGraphHostInputNode<Vector2>
    {
    }

    [Name("Host Vector3 Input")]
    [Category("Host/Input")]
    [Description("Reads a declared host Vector3 input.")]
    public sealed class EventGraphVector3InputNode : EventGraphHostInputNode<Vector3>
    {
    }

    [Name("Host Delta Seconds")]
    [Category("Host/Input")]
    [Description("Reads the delta seconds supplied by the host.")]
    public sealed class EventGraphDeltaNode : FlowScriptNode
    {
        protected override void RegisterPorts()
        {
            AddValueOutput<float>(
                "Delta",
                () =>
                {
                    if (graph is not HostEventGraph host)
                        throw new System.InvalidOperationException(
                            "Event graph delta node is outside a HostEventGraph.");
                    return host.ReadInput<float>(
                        EventGraphHostInputIds.DeltaSeconds);
                });
        }
    }

    public static class EventGraphHostInputIds
    {
        public const string DeltaSeconds = "host.delta-seconds";
    }
}
