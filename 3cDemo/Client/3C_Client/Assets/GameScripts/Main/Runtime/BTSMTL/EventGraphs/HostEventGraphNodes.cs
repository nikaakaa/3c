using System;
using FlowCanvas;
using FlowCanvas.Nodes;
using ParadoxNotion.Design;
using UnityEngine;

namespace BTSMTL.EventGraphs
{
    public abstract class EventGraphHostInputNode<T> : FlowScriptNode, EventGraphHostInputNodeMarker
    {
        [SerializeField] string m_InputId = string.Empty;
        [NonSerialized] EventGraphInputDescriptor m_Descriptor;

        public string InputId => m_InputId ?? string.Empty;

        public Type ValueType => typeof(T);

        public override string name =>
            string.IsNullOrEmpty(InputId)
                ? base.name
                : EventGraphHostInputDisplayNames.For(InputId);

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

        public void BindInput(EventGraphInputDescriptor descriptor) => m_Descriptor = descriptor;

        T ReadValue()
        {
            if (graph is not HostEventGraph host)
                throw new System.InvalidOperationException("Event graph host input node is outside a HostEventGraph.");
            return host.ReadInput(m_Descriptor).As<T>();
        }
    }

    internal static class EventGraphHostInputDisplayNames
    {
        internal static string For(string inputId) => inputId switch
        {
            "presentation.velocity" => "Read Velocity",
            "presentation.rotation" => "Read Rotation",
            "presentation.grounded" => "Read Grounded",
            "presentation.desired-planar-velocity" => "Read Desired Planar Velocity",
            "presentation.desired-facing" => "Read Desired Facing",
            "presentation.has-motion" => "Read Has Motion",
            "presentation.locomotion-planar-basis" => "Read Locomotion Planar Basis",
            _ => "Read " + inputId.Replace(".", " ").Replace("-", " ")
        };
    }

    [Name("Read Host Float")]
    [Category("Host/Input")]
    [Description("Reads a declared host float input.")]
    public sealed class EventGraphFloatInputNode : EventGraphHostInputNode<float>
    {
    }

    [Name("Read Host Int")]
    [Category("Host/Input")]
    [Description("Reads a declared host integer input.")]
    public sealed class EventGraphIntInputNode : EventGraphHostInputNode<int>
    {
    }

    [Name("Read Host Bool")]
    [Category("Host/Input")]
    [Description("Reads a declared host boolean input.")]
    public sealed class EventGraphBoolInputNode : EventGraphHostInputNode<bool>
    {
    }

    [Name("Read Host Vector2")]
    [Category("Host/Input")]
    [Description("Reads a declared host Vector2 input.")]
    public sealed class EventGraphVector2InputNode : EventGraphHostInputNode<Vector2>
    {
    }

    [Name("Read Host Vector3")]
    [Category("Host/Input")]
    [Description("Reads a declared host Vector3 input.")]
    public sealed class EventGraphVector3InputNode : EventGraphHostInputNode<Vector3>
    {
    }

    [Name("Read Host Quaternion")]
    [Category("Host/Input")]
    public sealed class EventGraphQuaternionInputNode : EventGraphHostInputNode<Quaternion>
    {
    }

    [Name("Read Delta Seconds")]
    [Category("Host/Input")]
    [Description("Reads the delta seconds supplied by the host.")]
    public sealed class EventGraphDeltaNode : FlowScriptNode, EventGraphHostInputNodeMarker
    {
        [NonSerialized] EventGraphInputDescriptor m_Descriptor;

        protected override void RegisterPorts()
        {
            AddValueOutput<float>(
                "Delta",
                () =>
                {
                    if (graph is not HostEventGraph host)
                        throw new System.InvalidOperationException(
                            "Event graph delta node is outside a HostEventGraph.");
                    return host.ReadInput(m_Descriptor).As<float>();
                });
        }

        public string InputId => EventGraphHostInputIds.DeltaSeconds;

        public Type ValueType => typeof(float);

        public void BindInput(EventGraphInputDescriptor descriptor) => m_Descriptor = descriptor;
    }

    public static class EventGraphHostInputIds
    {
        public const string DeltaSeconds = "host.delta-seconds";
    }

    [Name("Extract Planar XZ")]
    [Category("Values/Vector3")]
    public sealed class EventGraphVector3PlanarNode : PureFunctionNode<Vector2, Vector3>
    {
        public override Vector2 Invoke(Vector3 value) => new Vector2(value.x, value.z);
    }

    [Name("Extract Vertical Speed")]
    [Category("Values/Vector3")]
    public sealed class EventGraphVector3YNode : PureFunctionNode<float, Vector3>
    {
        public override float Invoke(Vector3 value) => value.y;
    }

    [Name("Vector2 Magnitude")]
    [Category("Values/Vector2")]
    public sealed class EventGraphVector2MagnitudeNode : PureFunctionNode<float, Vector2>
    {
        public override float Invoke(Vector2 value) => value.magnitude;
    }

    [Name("Vector2 Normalize By Squared Magnitude")]
    [Category("Values/Vector2")]
    public sealed class EventGraphVector2NormalizeNode : PureFunctionNode<Vector2, Vector2, float>
    {
        public override Vector2 Invoke(Vector2 value, float minimumSquaredMagnitude) =>
            minimumSquaredMagnitude <= 0f
                ? value.normalized
                : value.sqrMagnitude > minimumSquaredMagnitude
                    ? value.normalized
                    : Vector2.zero;
    }

    [Name("Vector2 Subtract")]
    [Category("Values/Vector2")]
    public sealed class EventGraphVector2SubtractNode : PureFunctionNode<Vector2, Vector2, Vector2>
    {
        public override Vector2 Invoke(Vector2 a, Vector2 b) => a - b;
    }

    [Name("Vector2 Signed Angle")]
    [Category("Values/Vector2")]
    public sealed class EventGraphVector2SignedAngleNode : PureFunctionNode<float, Vector2, Vector2>
    {
        public override float Invoke(Vector2 from, Vector2 to) => Vector2.SignedAngle(from, to);
    }

    [Name("Extract Current Forward")]
    [Category("Values/Quaternion")]
    public sealed class EventGraphQuaternionForwardNode : PureFunctionNode<Vector3, Quaternion>
    {
        public override Vector3 Invoke(Quaternion value) => value * Vector3.forward;
    }
    [Name("Read Host String")]
    [Category("Host/Input")]
    public sealed class EventGraphStringInputNode : EventGraphHostInputNode<string> { }

    [Name("String Equal")]
    [Category("Values/String")]
    public sealed class EventGraphStringEqualNode : PureFunctionNode<bool, string, string>
    {
        public override bool Invoke(string a, string b) => string.Equals(a, b, System.StringComparison.Ordinal);
    }

    [Name("Float Absolute")]
    [Category("Values/Float")]
    public sealed class EventGraphFloatAbsNode : PureFunctionNode<float, float>
    {
        public override float Invoke(float value) => Mathf.Abs(value);
    }

    [Name("Float Clamp")]
    [Category("Values/Float")]
    public sealed class EventGraphFloatClampNode : PureFunctionNode<float, float, float, float>
    {
        public override float Invoke(float value, float minimum, float maximum) => Mathf.Clamp(value, minimum, maximum);
    }

    [Name("Float Move Towards")]
    [Category("Values/Float")]
    public sealed class EventGraphFloatMoveTowardsNode : PureFunctionNode<float, float, float, float>
    {
        public override float Invoke(float current, float target, float maxDelta) => Mathf.MoveTowards(current, target, maxDelta);
    }

    [Name("Select Float")]
    [Category("Values/Float")]
    public sealed class EventGraphFloatSelectNode : PureFunctionNode<float, bool, float, float>
    {
        public override float Invoke(bool condition, float whenTrue, float whenFalse) => condition ? whenTrue : whenFalse;
    }

    [Name("Quaternion Angle Axis")]
    [Category("Values/Quaternion")]
    public sealed class EventGraphQuaternionAngleAxisNode : PureFunctionNode<Quaternion, float, Vector3>
    {
        public override Quaternion Invoke(float angle, Vector3 axis) => Quaternion.AngleAxis(angle, axis);
    }

}
