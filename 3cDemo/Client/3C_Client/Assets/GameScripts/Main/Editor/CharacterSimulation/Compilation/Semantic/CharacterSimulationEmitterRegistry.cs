using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public readonly struct CharacterSimulationNodeEmission
    {
        public CharacterSimulationNodeEmission(SimulationOperationCode code, int integer0 = 0, int integer1 = 0, ulong unsigned0 = 0, string text0 = null, uint flags = 0, IEnumerable<KeyValuePair<string, object>> constants = null)
        {
            Code = code;
            Integer0 = integer0;
            Integer1 = integer1;
            Unsigned0 = unsigned0;
            Text0 = text0 ?? string.Empty;
            Flags = flags;
            Constants = constants == null ? Array.Empty<KeyValuePair<string, object>>() : constants.ToArray();
        }
        public SimulationOperationCode Code { get; }
        public int Integer0 { get; }
        public int Integer1 { get; }
        public ulong Unsigned0 { get; }
        public string Text0 { get; }
        public uint Flags { get; }
        public IReadOnlyList<KeyValuePair<string, object>> Constants { get; }
    }

    public interface ICharacterSimulationNodeEmitter
    {
        Type SourceType { get; }
        OperationHandle Emit(BaseNode node, CharacterSimulationNodeEmitterContext context);
    }

    public sealed class CharacterSimulationNodeEmitterContext
    {
        readonly BaseGraph m_Graph;
        readonly string m_GraphContentHash;
        readonly string m_Route;
        readonly GameplayAbilitySemanticBuilder m_Builder;
        readonly SimulationOperationEmitter m_OperationEmitter;

        public CharacterSimulationNodeEmitterContext(BaseGraph graph, string route, GameplayAbilitySemanticBuilder builder)
        {
            m_Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            m_GraphContentHash = GraphAuthoringFingerprint.Compute(m_Graph);
            m_Route = route ?? string.Empty;
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_OperationEmitter = new SimulationOperationEmitter(builder);
        }

        public GameplayAbilitySemanticBuilder Builder => m_Builder;

        public OperationHandle Emit(BaseNode node, CharacterSimulationNodeEmission emission)
        {
            return Emit(node, emission, string.Empty);
        }

        public OperationHandle Emit(BaseNode node, CharacterSimulationNodeEmission emission, string portId)
        {
            CharacterSimulationSourceLocation source = Source(node, portId);
            List<CapturedValuePort> valuePorts = CaptureValuePorts(node, emission.Code);
            var constantInputs = new List<SimulationConstantInput>();
            CaptureUnconnectedInputConstants(node, valuePorts, constantInputs);
            return m_OperationEmitter.Emit(source, emission, constantInputs);
        }

        public CharacterSimulationSourceLocation Source(BaseNode node, string portId = "")
        {
            string sourcePort = portId ?? string.Empty;
            return new CharacterSimulationSourceLocation(
                node.GetType().FullName,
                m_Graph.GraphAuthoringId,
                node.GUID,
                string.Empty,
                string.Empty,
                string.Empty,
                sourcePort.Length == 0
                    ? $"{m_Route}/node:{node.GUID}"
                    : $"{m_Route}/node:{node.GUID}/port:{sourcePort}",
                portId: sourcePort,
                contentHash: m_GraphContentHash);
        }

        public void RecordOutputPorts(BaseNode node, OperationHandle operation, params string[] portIds)
        {
            if (portIds == null)
                throw new ArgumentNullException(nameof(portIds));
            for (int i = 0; i < portIds.Length; i++)
                m_Builder.DeclareOperationPortSource(operation, Source(node, portIds[i]));
        }

        public static string AssetIdentity(UnityEngine.Object asset)
        {
            if (!asset)
                return string.Empty;
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            return string.IsNullOrEmpty(guid) ? string.Empty : $"asset:{guid}";
        }

        List<CapturedValuePort> CaptureValuePorts(BaseNode node, SimulationOperationCode code)
        {
            OperationValuePortContract contract = GameplayAbilityValuePortContracts.Require(code);
            var result = new List<CapturedValuePort>();
            List<NodeFieldAccessor> accessors = node.GetFieldAccessors().OrderBy(value => value.FieldKey, StringComparer.Ordinal).ToList();
            for (int i = 0; i < accessors.Count; i++)
            {
                NodeFieldAccessor accessor = accessors[i];
                if (!accessor.TryGetPropertyPort(out PropertyPort port) || port == null)
                    continue;
                string portId = string.IsNullOrEmpty(port.PortId) ? accessor.FieldKey : port.PortId;
                if (port.Direction == PortDirection.Output)
                {
                    if (IsPropertyOutputLinked(node.GUID, portId, accessor.FieldKey))
                        contract.RequireSelection(portId);
                    continue;
                }
                OperationValuePortDefinition definition = contract.RequireInput(portId);
                if (IsPropertyInputLinked(node.GUID, portId, accessor.FieldKey))
                    continue;
                SemanticValueKind kind = ResolvePortKind(port, node, portId);
                if (!definition.Accepts(kind))
                    throw new InvalidOperationException($"Node '{node.GetType().Name}' port '{portId}' kind '{kind}' violates operation '{code}' contract '{definition.Constraint}'.");
                result.Add(new CapturedValuePort(accessor.FieldKey, portId, port.Direction, kind, port));
            }
            return result;
        }

        void CaptureUnconnectedInputConstants(
            BaseNode node,
            IReadOnlyList<CapturedValuePort> ports,
            List<SimulationConstantInput> constantInputs)
        {
            for (int i = 0; i < ports.Count; i++)
            {
                CapturedValuePort captured = ports[i];
                if (captured.Direction != PortDirection.Input)
                    continue;
                string portId = captured.PortId;
                if (IsPropertyInputLinked(node.GUID, portId, captured.FieldKey))
                    continue;
                CharacterSimulationSourceLocation source = Source(node, portId);
                constantInputs.Add(new SimulationConstantInput(portId, captured.Kind, captured.Port.GetValue(), source));
            }
        }

        static SemanticValueKind ResolvePortKind(PropertyPort port, BaseNode node, string portId)
        {
            object value = port.GetValue();
            return value switch
            {
                bool => SemanticValueKind.Boolean,
                int => SemanticValueKind.Int32,
                uint => SemanticValueKind.UInt64,
                ulong => SemanticValueKind.UInt64,
                float => SemanticValueKind.Number,
                double => SemanticValueKind.Number,
                Vector2 => SemanticValueKind.Vector2,
                Vector3 => SemanticValueKind.Vector3,
                string => SemanticValueKind.Identity,
                Enum => SemanticValueKind.Int32,
                _ => throw new InvalidOperationException($"Node '{node.GetType().Name}' Value port '{portId}' uses unsupported property type '{value?.GetType().FullName ?? port.GetType().FullName}'.")
            };
        }

        bool IsPropertyInputLinked(string nodeId, string portId, string fieldKey)
        {
            for (int i = 0; i < m_Graph.PropertyEdges.Count; i++)
            {
                PropertyEdge edge = m_Graph.PropertyEdges[i];
                if (edge != null &&
                    string.Equals(edge.EndNodeGUID, nodeId, StringComparison.Ordinal) &&
                    (string.Equals(edge.EndPortName, portId, StringComparison.Ordinal) || string.Equals(edge.EndPortName, fieldKey, StringComparison.Ordinal)))
                    return true;
            }
            return false;
        }

        bool IsPropertyOutputLinked(string nodeId, string portId, string fieldKey)
        {
            for (int i = 0; i < m_Graph.PropertyEdges.Count; i++)
            {
                PropertyEdge edge = m_Graph.PropertyEdges[i];
                if (edge != null &&
                    string.Equals(edge.StartNodeGUID, nodeId, StringComparison.Ordinal) &&
                    (string.Equals(edge.StartPortName, portId, StringComparison.Ordinal) || string.Equals(edge.StartPortName, fieldKey, StringComparison.Ordinal)))
                    return true;
            }
            return false;
        }

        readonly struct CapturedValuePort
        {
            public CapturedValuePort(string fieldKey, string portId, PortDirection direction, SemanticValueKind kind, PropertyPort port)
            {
                FieldKey = fieldKey;
                PortId = portId;
                Direction = direction;
                Kind = kind;
                Port = port;
            }

            public string FieldKey { get; }
            public string PortId { get; }
            public PortDirection Direction { get; }
            public SemanticValueKind Kind { get; }
            public PropertyPort Port { get; }
        }

    }

    public sealed class CharacterSimulationNodeEmitterRegistry
    {
        readonly Dictionary<Type, ICharacterSimulationNodeEmitter> m_Emitters = new Dictionary<Type, ICharacterSimulationNodeEmitter>();

        public void Register(ICharacterSimulationNodeEmitter emitter)
        {
            if (emitter == null)
                throw new ArgumentNullException(nameof(emitter));
            if (!m_Emitters.TryAdd(emitter.SourceType, emitter))
                throw new InvalidOperationException($"Emitter for '{emitter.SourceType.FullName}' is already registered.");
        }

        public bool TryGet(Type sourceType, out ICharacterSimulationNodeEmitter emitter)
        {
            return m_Emitters.TryGetValue(sourceType, out emitter);
        }

        public static CharacterSimulationNodeEmitterRegistry CreateDefault()
        {
            var registry = new CharacterSimulationNodeEmitterRegistry();
            CharacterSimulationCoreNodeEmitterRegistration.Register(registry);
            CharacterSimulationInputNodeEmitterRegistration.Register(registry);
            CharacterSimulationBlackboardNodeEmitterRegistration.Register(registry);
            CharacterSimulationActionNodeEmitterRegistration.Register(registry);
            CharacterSimulationCameraNodeEmitterRegistration.Register(registry);
            CharacterSimulationGameplayNodeEmitterRegistration.Register(registry);
            CharacterSimulationEquipmentNodeEmitterRegistration.Register(registry);
            CharacterSimulationMotionNodeEmitterRegistration.Register(registry);
            return registry;
        }

        internal static ICharacterSimulationNodeEmitter Simple<T>(Func<T, CharacterSimulationNodeEmission> emit) where T : BaseNode
        {
            return new SimpleCharacterSimulationNodeEmitter<T>(emit);
        }

        internal static ICharacterSimulationNodeEmitter Camera<T>(Func<T, CharacterSimulationNodeEmission> emit) where T : BaseNode
        {
            return new CameraCharacterSimulationNodeEmitter<T>(emit);
        }

        internal static KeyValuePair<string, object>[] Fields(params (string Name, object Value)[] values)
        {
            var result = new KeyValuePair<string, object>[values.Length];
            for (int i = 0; i < values.Length; i++)
                result[i] = new KeyValuePair<string, object>(values[i].Name, values[i].Value);
            return result;
        }
    }

    sealed class SimpleCharacterSimulationNodeEmitter<T> : ICharacterSimulationNodeEmitter where T : BaseNode
    {
        readonly Func<T, CharacterSimulationNodeEmission> m_Emit;

        public SimpleCharacterSimulationNodeEmitter(Func<T, CharacterSimulationNodeEmission> emit)
        {
            m_Emit = emit ?? throw new ArgumentNullException(nameof(emit));
        }
        public Type SourceType => typeof(T);
        public OperationHandle Emit(BaseNode node, CharacterSimulationNodeEmitterContext context)
        {
            return context.Emit((T)node, m_Emit((T)node));
        }
    }

    sealed class CameraCharacterSimulationNodeEmitter<T> : ICharacterSimulationNodeEmitter where T : BaseNode
    {
        readonly Func<T, CharacterSimulationNodeEmission> m_Emit;

        public CameraCharacterSimulationNodeEmitter(Func<T, CharacterSimulationNodeEmission> emit)
        {
            m_Emit = emit ?? throw new ArgumentNullException(nameof(emit));
        }

        public Type SourceType => typeof(T);

        public OperationHandle Emit(BaseNode node, CharacterSimulationNodeEmitterContext context)
        {
            CharacterSimulationNodeEmission emission = m_Emit((T)node);
            CharacterSimulationSourceLocation source = context.Source(node, CameraProgramOperationSchema.OutputPortId);
            OperationHandle operation = context.Emit(node, emission, CameraProgramOperationSchema.OutputPortId);
            string producerIdentity = $"camera:{source.TemplateIdentity}";
            int producer = context.Builder.DeclareProducer(
                producerIdentity,
                CameraProgramOperationSchema.ChannelId,
                source.TemplateIdentity,
                ProgramOutputChannelKind.Presentation,
                source);
            if (producer < 0)
                return operation;
            context.Builder.DeclareReference(
                $"{source.TemplateIdentity}/camera-producer",
                operation,
                ProgramReferenceKind.Producer,
                producer,
                producerIdentity,
                source);
            return operation;
        }
    }

    sealed class CameraBasisCharacterSimulationNodeEmitter : ICharacterSimulationNodeEmitter
    {
        public Type SourceType => typeof(ReadCameraBasisNode);

        public OperationHandle Emit(BaseNode node, CharacterSimulationNodeEmitterContext context)
        {
            var cameraBasis = (ReadCameraBasisNode)node;
            OperationHandle operation = context.Emit(
                cameraBasis,
                new CharacterSimulationNodeEmission(
                    SimulationOperationCode.CameraBasisRead,
                    integer0: CameraProgramOperationSchema.PayloadVersion));
            context.RecordOutputPorts(
                cameraBasis,
                operation,
                CameraProgramOperationSchema.BasisValidPortId,
                CameraProgramOperationSchema.BasisPlanarForwardPortId,
                CameraProgramOperationSchema.BasisPlanarRightPortId,
                CameraProgramOperationSchema.BasisLookDirectionPortId,
                CameraProgramOperationSchema.BasisAimPointPortId,
                CameraProgramOperationSchema.BasisYawPortId,
                CameraProgramOperationSchema.BasisPitchPortId);
            return operation;
        }
    }
}
