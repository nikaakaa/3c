using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ThirdPersonSimulation;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class CharacterAuthoringGraphParameter
    {
        internal CharacterAuthoringGraphParameter(string declarationId, string name, Type valueType)
        {
            DeclarationId = declarationId ?? string.Empty;
            Name = name ?? string.Empty;
            ValueType = valueType;
        }

        public string DeclarationId { get; }
        public string Name { get; }
        public Type ValueType { get; }
    }

    public sealed class CharacterAuthoringGraphSignature
    {
        readonly ReadOnlyCollection<CharacterAuthoringGraphParameter> m_Parameters;
        readonly IReadOnlyDictionary<string, CharacterAuthoringGraphParameter> m_ByName;

        internal CharacterAuthoringGraphSignature(IEnumerable<BaseExposedProperty> declarations)
        {
            CharacterAuthoringGraphParameter[] parameters = (declarations ?? Array.Empty<BaseExposedProperty>())
                .Where(value => value != null)
                .OrderBy(value => value.Name, StringComparer.Ordinal)
                .ThenBy(value => value.DeclarationId, StringComparer.Ordinal)
                .Select(value => new CharacterAuthoringGraphParameter(value.DeclarationId, value.Name, value.ValueType))
                .ToArray();
            m_Parameters = Array.AsReadOnly(parameters);
            var byName = new Dictionary<string, CharacterAuthoringGraphParameter>(StringComparer.Ordinal);
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!byName.ContainsKey(parameters[i].Name))
                    byName.Add(parameters[i].Name, parameters[i]);
            }
            m_ByName = new ReadOnlyDictionary<string, CharacterAuthoringGraphParameter>(byName);
        }

        public IReadOnlyList<CharacterAuthoringGraphParameter> Parameters => m_Parameters;

        public bool TryGetParameter(string name, out CharacterAuthoringGraphParameter parameter)
        {
            return m_ByName.TryGetValue(name ?? string.Empty, out parameter);
        }
    }

    public sealed class CharacterAuthoringGraphParameterBinding
    {
        internal CharacterAuthoringGraphParameterBinding(
            CharacterAuthoringGraphParameterDirection direction,
            string parameterName,
            string declarationId,
            string portId,
            Type valueType)
        {
            Direction = direction;
            ParameterName = parameterName ?? string.Empty;
            DeclarationId = declarationId ?? string.Empty;
            PortId = portId ?? string.Empty;
            ValueType = valueType;
        }

        public CharacterAuthoringGraphParameterDirection Direction { get; }
        public string ParameterName { get; }
        public string DeclarationId { get; }
        public string PortId { get; }
        public Type ValueType { get; }
    }

    public enum CharacterAuthoringGraphParameterDirection : byte
    {
        Input = 1,
        Output = 2
    }

    public sealed class CharacterAuthoringGraphCallFrame
    {
        readonly ReadOnlyCollection<CharacterAuthoringGraphParameterBinding> m_Inputs;
        readonly ReadOnlyCollection<CharacterAuthoringGraphParameterBinding> m_Outputs;

        internal CharacterAuthoringGraphCallFrame(
            string identity,
            string referenceKey,
            string childGraphAuthoringId,
            IEnumerable<CharacterAuthoringGraphParameterBinding> inputs,
            IEnumerable<CharacterAuthoringGraphParameterBinding> outputs)
        {
            Identity = identity ?? string.Empty;
            ReferenceKey = referenceKey ?? string.Empty;
            ChildGraphAuthoringId = childGraphAuthoringId ?? string.Empty;
            m_Inputs = Array.AsReadOnly((inputs ?? Array.Empty<CharacterAuthoringGraphParameterBinding>()).ToArray());
            m_Outputs = Array.AsReadOnly((outputs ?? Array.Empty<CharacterAuthoringGraphParameterBinding>()).ToArray());
        }

        public string Identity { get; }
        public string ReferenceKey { get; }
        public string ChildGraphAuthoringId { get; }
        public IReadOnlyList<CharacterAuthoringGraphParameterBinding> Inputs => m_Inputs;
        public IReadOnlyList<CharacterAuthoringGraphParameterBinding> Outputs => m_Outputs;
    }

    internal static class CharacterAuthoringGraphCallFrameFactory
    {
        public static CharacterAuthoringGraphCallFrame Create(
            BaseNode owner,
            NodeGraphReference reference,
            CharacterAuthoringGraphOccurrence child,
            string route,
            CharacterSimulationCompileReport report)
        {
            return CreateCore(owner, reference, child.Graph, child.Signature, route, report);
        }

        internal static CharacterAuthoringGraphCallFrame CreateForExport(
            BaseNode owner,
            NodeGraphReference reference,
            BaseTree child,
            string route)
        {
            return CreateCore(
                owner,
                reference,
                child,
                new CharacterAuthoringGraphSignature(child?.ExposedProperties),
                route,
                null);
        }

        static CharacterAuthoringGraphCallFrame CreateCore(
            BaseNode owner,
            NodeGraphReference reference,
            BaseTree child,
            CharacterAuthoringGraphSignature signature,
            string route,
            CharacterSimulationCompileReport report)
        {
            var inputs = new List<CharacterAuthoringGraphParameterBinding>();
            var outputs = new List<CharacterAuthoringGraphParameterBinding>();
            if (owner is SubTreeNode subTreeNode &&
                string.Equals(reference.Key, "m_SubTree", StringComparison.Ordinal) &&
                child is SubTree)
            {
                BindPorts(
                    subTreeNode.InputPropertyPorts,
                    "_Input",
                    CharacterAuthoringGraphParameterDirection.Input,
                    signature,
                    inputs,
                    route,
                    report);
                BindPorts(
                    subTreeNode.OutputPropertyPorts,
                    "_Output",
                    CharacterAuthoringGraphParameterDirection.Output,
                    signature,
                    outputs,
                    route,
                    report);
            }
            return new CharacterAuthoringGraphCallFrame(
                route + "/call",
                reference.Key,
                child.GraphAuthoringId,
                inputs,
                outputs);
        }

        static void BindPorts(
            IReadOnlyList<PropertyPort> ports,
            string suffix,
            CharacterAuthoringGraphParameterDirection direction,
            CharacterAuthoringGraphSignature signature,
            List<CharacterAuthoringGraphParameterBinding> bindings,
            string route,
            CharacterSimulationCompileReport report)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < ports.Count; i++)
            {
                PropertyPort port = ports[i];
                if (port == null)
                {
                    report?.DiscoveryError("graph_call_port_missing", route, "SubTree call frame contains a missing property port.");
                    continue;
                }
                string portName = port.Name ?? string.Empty;
                if (!portName.EndsWith(suffix, StringComparison.Ordinal))
                {
                    report?.DiscoveryError("graph_call_port_invalid", route + "/port:" + portName, $"SubTree call frame port must end with '{suffix}'.");
                    continue;
                }
                string parameterName = portName.Substring(0, portName.Length - suffix.Length);
                if (!names.Add(parameterName))
                {
                    report?.DiscoveryError("graph_call_parameter_duplicate", route + "/port:" + portName, $"SubTree call frame binds parameter '{parameterName}' more than once.");
                    continue;
                }
                if (!signature.TryGetParameter(parameterName, out CharacterAuthoringGraphParameter parameter))
                {
                    report?.DiscoveryError("graph_call_parameter_missing", route + "/port:" + portName, $"SubTree signature has no declaration named '{parameterName}'.");
                    continue;
                }
                if (port.ValueType != parameter.ValueType)
                {
                    report?.DiscoveryError("graph_call_parameter_type_mismatch", route + "/port:" + portName, $"SubTree parameter '{parameterName}' expects '{parameter.ValueType?.FullName}', but call port is '{port.ValueType?.FullName}'.");
                    continue;
                }
                bindings.Add(new CharacterAuthoringGraphParameterBinding(
                    direction,
                    parameterName,
                    parameter.DeclarationId,
                    port.PortId,
                    parameter.ValueType));
            }
        }
    }
}
