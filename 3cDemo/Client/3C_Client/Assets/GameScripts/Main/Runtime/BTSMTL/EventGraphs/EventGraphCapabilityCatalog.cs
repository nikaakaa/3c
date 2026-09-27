using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas.Macros;
using FlowCanvas.Nodes;

namespace BTSMTL.EventGraphs
{
    public enum EventGraphCapabilityKind : byte
    {
        InitializationEvent = 1,
        UpdateEvent = 2,
        VariableGet = 3,
        VariableSet = 4,
        PureValue = 5,
        Switch = 6,
        InstantSplit = 7,
        Sequence = 8,
        Macro = 9,
        HostInput = 10
    }

    public sealed class EventGraphCapabilityDescriptor
    {
        internal EventGraphCapabilityDescriptor(
            Type authoringType,
            EventGraphCapabilityKind kind,
            string identity)
        {
            AuthoringType = authoringType ?? throw new ArgumentNullException(nameof(authoringType));
            Kind = kind;
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }

        public Type AuthoringType { get; }
        public EventGraphCapabilityKind Kind { get; }
        public string Identity { get; }
    }

    public static class EventGraphCapabilityCatalog
    {
        static readonly Dictionary<Type, string> s_PureIdentities =
            new Dictionary<Type, string>
            {
                { typeof(EventGraphStringEqualNode), "value.string.equal" },
                { typeof(EventGraphFloatAbsNode), "value.float.abs" },
                { typeof(EventGraphFloatClampNode), "value.float.clamp" },
                { typeof(EventGraphFloatMoveTowardsNode), "value.float.move-towards" },
                { typeof(EventGraphFloatSelectNode), "value.float.select" },
                { typeof(EventGraphQuaternionAngleAxisNode), "value.quaternion.angle-axis" },
                { typeof(FloatAdd), "value.float.add" },
                { typeof(FloatSubtract), "value.float.subtract" },
                { typeof(FloatMultiply), "value.float.multiply" },
                { typeof(FloatDivide), "value.float.divide" },
                { typeof(FloatModulo), "value.float.modulo" },
                { typeof(FloatGreaterThan), "value.float.greater-than" },
                { typeof(FloatGreaterEqualThan), "value.float.greater-equal-than" },
                { typeof(FloatLessThan), "value.float.less-than" },
                { typeof(FloatLessEqualThan), "value.float.less-equal-than" },
                { typeof(FloatEqual), "value.float.equal" },
                { typeof(FloatNotEqual), "value.float.not-equal" },
                { typeof(FloatInvert), "value.float.invert" },
                { typeof(FloatSnap), "value.float.snap" },
                { typeof(IntegerAdd), "value.int.add" },
                { typeof(IntegerSubtract), "value.int.subtract" },
                { typeof(IntegerMultiply), "value.int.multiply" },
                { typeof(IntegerDivide), "value.int.divide" },
                { typeof(IntegerModulo), "value.int.modulo" },
                { typeof(IntegerGreaterThan), "value.int.greater-than" },
                { typeof(IntegerGreaterEqualThan), "value.int.greater-equal-than" },
                { typeof(IntegerLessThan), "value.int.less-than" },
                { typeof(IntegerLessEqualThan), "value.int.less-equal-than" },
                { typeof(IntegerEqual), "value.int.equal" },
                { typeof(IntegerNotEqual), "value.int.not-equal" },
                { typeof(IntegerInvert), "value.int.invert" },
                { typeof(IntegerSnap), "value.int.snap" },
                { typeof(BooleanEqual), "value.bool.equal" },
                { typeof(BooleanNotEqual), "value.bool.not-equal" },
                { typeof(AND), "value.bool.and" },
                { typeof(OR), "value.bool.or" },
                { typeof(XOR), "value.bool.xor" },
                { typeof(NOT), "value.bool.not" },
                { typeof(Vector3Equal), "value.vector3.equal" },
                { typeof(Vector3NotEqual), "value.vector3.not-equal" },
                { typeof(Vector3Add), "value.vector3.add" },
                { typeof(Vector3Subtract), "value.vector3.subtract" },
                { typeof(Vector3Multiply), "value.vector3.multiply" },
                { typeof(Vector3Divide), "value.vector3.divide" },
                { typeof(Vector3Invert), "value.vector3.invert" },
                { typeof(LerpFloat), "value.float.lerp" },
                { typeof(LerpVector3), "value.vector3.lerp" },
                { typeof(RemapFloat), "value.float.remap" },
                { typeof(RemapVector3), "value.vector3.remap" },
                { typeof(DampFloat), "value.float.damp" },
                { typeof(DampVector3), "value.vector3.damp" },
                { typeof(EventGraphVector3PlanarNode), "value.vector3.planar" },
                { typeof(EventGraphVector3YNode), "value.vector3.y" },
                { typeof(EventGraphVector2MagnitudeNode), "value.vector2.magnitude" },
                { typeof(EventGraphVector2NormalizeNode), "value.vector2.normalize" },
                { typeof(EventGraphVector2SubtractNode), "value.vector2.subtract" },
                { typeof(EventGraphVector2SignedAngleNode), "value.vector2.signed-angle" },
                { typeof(EventGraphQuaternionForwardNode), "value.quaternion.forward" }
            };

        public static bool TryGet(
            Type nodeType,
            out EventGraphCapabilityDescriptor descriptor)
        {
            descriptor = null;
            if (nodeType == null)
                return false;

            if (nodeType == typeof(StartEvent))
            {
                descriptor = new EventGraphCapabilityDescriptor(
                    nodeType,
                    EventGraphCapabilityKind.InitializationEvent,
                    "event.start");
                return true;
            }
            if (nodeType == typeof(UpdateEvent))
            {
                descriptor = new EventGraphCapabilityDescriptor(
                    nodeType,
                    EventGraphCapabilityKind.UpdateEvent,
                    "event.update");
                return true;
            }
            if (nodeType == typeof(SwitchBool))
            {
                descriptor = new EventGraphCapabilityDescriptor(
                    nodeType,
                    EventGraphCapabilityKind.Switch,
                    "flow.switch-bool");
                return true;
            }
            if (nodeType == typeof(Split))
            {
                descriptor = new EventGraphCapabilityDescriptor(
                    nodeType,
                    EventGraphCapabilityKind.InstantSplit,
                    "flow.split-instant");
                return true;
            }
            if (nodeType == typeof(Sequence))
            {
                descriptor = new EventGraphCapabilityDescriptor(
                    nodeType,
                    EventGraphCapabilityKind.Sequence,
                    "flow.flip-flop");
                return true;
            }
            if (nodeType == typeof(MacroNodeWrapper))
            {
                descriptor = new EventGraphCapabilityDescriptor(
                    nodeType,
                    EventGraphCapabilityKind.Macro,
                    "graph.macro-call");
                return true;
            }
            if (nodeType == typeof(MacroInputNode) ||
                nodeType == typeof(MacroOutputNode))
            {
                descriptor = new EventGraphCapabilityDescriptor(
                    nodeType,
                    EventGraphCapabilityKind.Macro,
                    "graph.macro-port");
                return true;
            }
            if (nodeType == typeof(EventGraphStringInputNode) ||
                nodeType == typeof(EventGraphFloatInputNode) ||
                nodeType == typeof(EventGraphIntInputNode) ||
                nodeType == typeof(EventGraphBoolInputNode) ||
                 nodeType == typeof(EventGraphVector2InputNode) ||
                 nodeType == typeof(EventGraphVector3InputNode) ||
                 nodeType == typeof(EventGraphQuaternionInputNode) ||
                 nodeType == typeof(EventGraphDeltaNode))
            {
                descriptor = new EventGraphCapabilityDescriptor(
                    nodeType,
                    EventGraphCapabilityKind.HostInput,
                    nodeType == typeof(EventGraphDeltaNode)
                        ? "host.delta-seconds"
                        : "host.typed-input");
                return true;
            }
            if (nodeType.IsGenericType &&
                nodeType.GetGenericTypeDefinition() == typeof(GetVariable<>))
            {
                descriptor = new EventGraphCapabilityDescriptor(
                    nodeType,
                    EventGraphCapabilityKind.VariableGet,
                    "variable.get");
                return true;
            }
            if (nodeType.IsGenericType &&
                nodeType.GetGenericTypeDefinition() == typeof(SetVariable<>))
            {
                descriptor = new EventGraphCapabilityDescriptor(
                    nodeType,
                    EventGraphCapabilityKind.VariableSet,
                    "variable.set");
                return true;
            }
            if (nodeType.IsGenericType &&
                nodeType.GetGenericTypeDefinition() == typeof(SimplexNodeWrapper<>))
            {
                Type simplexType = nodeType.GetGenericArguments()[0];
                if (s_PureIdentities.TryGetValue(simplexType, out string wrappedPureIdentity))
                {
                    descriptor = new EventGraphCapabilityDescriptor(
                        nodeType,
                        EventGraphCapabilityKind.PureValue,
                        wrappedPureIdentity);
                    return true;
                }
            }
            if (s_PureIdentities.TryGetValue(nodeType, out string pureIdentity))
            {
                descriptor = new EventGraphCapabilityDescriptor(
                    nodeType,
                    EventGraphCapabilityKind.PureValue,
                    pureIdentity);
                return true;
            }
            return false;
        }

        public static bool Allows(Type nodeType) => TryGet(nodeType, out _);

        public static bool IsKnownIdentity(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
                return false;
            return identity switch
            {
                "event.start" => true,
                "event.update" => true,
                "flow.switch-bool" => true,
                "flow.split-instant" => true,
                "flow.flip-flop" => true,
                "graph.macro-call" => true,
                "graph.macro-port" => true,
                "host.delta-seconds" => true,
                "host.typed-input" => true,
                "variable.get" => true,
                "variable.set" => true,
                _ => s_PureIdentities.Values.Contains(identity, StringComparer.Ordinal)
            };
        }

        public static bool TryGetAuthoringType(
            string identity,
            out Type authoringType)
        {
            authoringType = null;
            switch (identity)
            {
                case "event.start":
                    authoringType = typeof(StartEvent);
                    return true;
                case "event.update":
                    authoringType = typeof(UpdateEvent);
                    return true;
                case "flow.switch-bool":
                    authoringType = typeof(SwitchBool);
                    return true;
                case "flow.split-instant":
                    authoringType = typeof(Split);
                    return true;
                case "flow.flip-flop":
                    authoringType = typeof(Sequence);
                    return true;
                case "graph.macro-call":
                    authoringType = typeof(MacroNodeWrapper);
                    return true;
                case "graph.macro-port":
                    authoringType = typeof(MacroInputNode);
                    return true;
                case "host.delta-seconds":
                    authoringType = typeof(EventGraphDeltaNode);
                    return true;
                case "variable.get":
                    authoringType = typeof(GetVariable<>);
                    return true;
                case "variable.set":
                    authoringType = typeof(SetVariable<>);
                    return true;
            }
            foreach (KeyValuePair<Type, string> pair in s_PureIdentities)
            {
                if (string.Equals(pair.Value, identity, StringComparison.Ordinal))
                {
                    authoringType = typeof(SimplexNodeWrapper<>).MakeGenericType(pair.Key);
                    return true;
                }
            }
            return false;
        }
    }
}
